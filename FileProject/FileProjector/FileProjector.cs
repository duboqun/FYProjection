using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using OSGeo.GDAL;
using OSGeo.OSR;
using PIE.Meteo.RasterProject;

namespace PIE.Meteo.FileProject
{
    public abstract class FileProjector : IFileProjector
    {
        #region 派生字段

        protected string _name = null;
        protected string _fullname = null;
        protected AbstractWarpDataset _curSession = null;
        protected bool _isBeginSession = false;
        protected RasterProjector _rasterProjector { get; set; } = null;
        protected Band[] _rasterDataBands = null; //用于投影的通道数据集。其数组顺序即为输出目标文件的通道顺序
        protected Band[] _dstDataBands = null; //用于投影的通道数据集。其数组顺序即为输出目标文件的通道顺序

        //protected Action<int, string> progressCallback;
        protected int progress = 0;
        protected int progressCount = 100;
        protected int percent = 0;

        protected PrjEnvelope _maxPrjEnvelope = null;
        protected AbstractWarpDataset _solarZenithCacheRaster { get; set; } = null; //太阳高度角通道
        protected Size _srcLocationSize;

        protected SpatialReference _dstSpatialRef;

        //当前投影范围，需要使用的原始轨道数据最小范围
        protected Block _orbitBlock = null;
        protected double[] _xs = null; //经纬度值或者公里数，存储的实际是计算后的值
        protected double[] _ys = null; //经纬度值或者公里数，存储的实际是计算后的值

        protected bool _isRadRef = false; //亮温订正
        protected bool _isRad = false; //辐射定标
        protected bool _isSolarZenith = false; //太阳天顶角订正

        /// <summary>
        /// 是否对亮温（辐亮度）进行临边变暗订正，公式如下
        /// T = Tb + deltaT
        /// deltaT == (Math.Pow(Math.E,0.00012*theta*theta)-1)*(0.1072*Tb-26.81)  //theta是卫星天顶角
        /// </summary>
        protected bool _isSensorZenith = false;

        //卫星天顶角通道。
        protected AbstractWarpDataset _sensorSenithRaster = null;
        protected Band _sensorSenithBand;
        protected string _outFormat = "GTiff";
        protected string _outfilename = "";
        protected string _dstProj4 = "";
        protected int _dstBandCount = 0;
        protected Size _dstSize;
        protected PrjEnvelope _dstEnvelope = null;
        protected float _outResolutionX = 0;
        protected float _outResolutionY = 0;
        protected float _srcImgResolution = 1.0f;

        protected string[] _supportAngles = new string[]
            {"SensorAzimuth", "SensorZenith", "SolarAzimuth", "SolarZenith"};

        protected Band[] _angleBands = null; //输出的角度文件
        protected AbstractWarpDataset[] _dstAngleRasters = null;
        protected Band[] _dstAngleBands = null;

        protected int _readyProgress = 0;
        protected Band _longitudeBand = null; //用于投影的经纬度通道
        protected Band _latitudeBand = null;
        protected double _geoIntercept = 0d; //地理数据截距
        protected double _geoSlope = 1d; //地理数据斜率

        //左右去除像元个数格式LeftRightInvalid={8,8}
        private Regex _leftRightInvalidArgReg =
            new Regex(@"LeftRightInvalid=\{(?<left>\d+?)\|(?<right>\d+?)\}", RegexOptions.Compiled);

        protected int _left = 0; //读取轨道数据时候左右去除像元个数
        protected int _right = 0;

        protected int _sacnLineWidth = 10; //扫描线宽度,默认为10，MERSI和MODIS的经纬度数据的扫描线。

        //投影扩展通道
        protected string[] _supportExtBandNames = null;
        protected Band[] _extSrcBands = null;
        protected Band[] _extDstBands = null;
        protected AbstractWarpDataset[] _extDstRasters = null;
        protected double? _NODATA_VALUE = null;

        protected Band[] _extSrcBands2 = null; //地形校正，真彩校正需要的波段
        protected AbstractWarpDataset[] _extDstRasters2 = null; //真彩校正需要的波段
        protected Band[] _extDstBands2 = null; //真彩输出波段

        #endregion

        protected FilePrjSettings _prjSettings;

        public string Name
        {
            get { return _name; }
        }

        public string FullName
        {
            get { return _fullname; }
        }

        #region 虚方法

        public abstract bool IsSupport(string fileName);

        public abstract void ComputeDstEnvelope(AbstractWarpDataset srcRaster, SpatialReference dstSpatialRef,
            out RasterProject.PrjEnvelope maxPrjEnvelope, Action<int, string> progressCallback);

        public abstract FilePrjSettings CreateDefaultPrjSettings();

        #endregion

        #region 未实现方法

        public virtual void Project(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings,
            SpatialReference dstSpatial, Action<int, string> progressCallback)
        {
            throw new NotImplementedException();
        }

        public virtual AbstractWarpDataset Project(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings,
            AbstractWarpDataset dstRaster, int beginBandIndex, Action<int, string> progressCallback)
        {
            throw new NotImplementedException();
        }

        public virtual void Project(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings,
            SpatialReference dstSpatialRef, Action<int, string> progressCallback, double weight, float zoom)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Read

        internal void ReadBandData(out float[] bandData, AbstractWarpDataset srcRaster, int bandNo, int xOffset,
            int yOffset, int blockWidth, int blockHeight)
        {
            bandData = new float[blockWidth * blockHeight];
            Band latBand = null;
            try
            {
                latBand = srcRaster.GetRasterBand(bandNo);
                latBand.ReadRaster(xOffset, yOffset, blockWidth, blockHeight, bandData,
                    blockWidth, blockHeight, 0, 0);
            }
            finally
            {
                if (latBand != null)
                    (latBand as IDisposable).Dispose();
            }
        }

        internal void ReadBandData(out float[] bandData, Band band, int xOffset, int yOffset, int blockWidth,
            int blockHeight)
        {
            bandData = null;
            if (band == null)
                return;
            try
            {
                bandData = new float[blockWidth * blockHeight];
                band.ReadRaster(xOffset, yOffset, blockWidth, blockHeight, bandData,
                    blockWidth, blockHeight, 0, 0);
            }
            finally
            {
            }
        }

        internal short[] ReadBandData(Band band, int xOffset, int yOffset, int blockWidth, int blockHeight)
        {
            if (band == null)
                return null;
            try
            {
                short[] bandData = new short[blockWidth * blockHeight];
                band.ReadRaster(xOffset, yOffset, blockWidth, blockHeight, bandData,
                    blockWidth, blockHeight, 0, 0);
                return bandData;
            }
            finally
            {
            }
        }

        internal void ReadBandData(Band band, out float[] bandData, out Size srcSize)
        {
            int width = band.XSize;
            int height = band.YSize;
            srcSize = new Size(width, height);
            bandData = new float[width * height];

            band.ReadRaster(0, 0, width, height, bandData, width, height, 0, 0);
        }

        /// <summary>
        /// 仅用于用于投影的通道数据读取。非角度等数据
        /// </summary>
        /// <param name="bandData"></param>
        /// <param name="srcRaster"></param>
        /// <param name="dstBandIndex"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <param name="blockWidth"></param>
        /// <param name="blockHeight"></param>
        internal void ReadImgBand(out ushort[] bandData, int dstBandIndex, int xOffset, int yOffset, int blockWidth,
            int blockHeight)
        {
            Band latBand = _rasterDataBands[dstBandIndex]; //
            bandData = new ushort[blockWidth * blockHeight];
            bool ok = false;
            try
            {
                CPLErr e = latBand.ReadRaster(xOffset, yOffset, blockWidth, blockHeight, bandData, blockWidth,
                    blockHeight,
                    0, 0);
                if (e != CPLErr.CE_Failure)
                    ok = true;
            }
            finally
            {
                if (!ok)
                {
                    System.Diagnostics.Debug.WriteLine("ReadImgBand  Index:{0}失败", dstBandIndex);
                }

                //latBand.Dispose();//现在貌似还不能释放...
            }
        }

        internal void ReadImgBand<T>(out T[] bandData, DataType dataType, int dstBandIndex, int xOffset,
            int yOffset, int blockWidth, int blockHeight)
        {
            Band latBand = _rasterDataBands[dstBandIndex]; //
            bandData = new T[blockWidth * blockHeight];
            GCHandle h = GCHandle.Alloc(bandData, GCHandleType.Pinned);
            try
            {
                IntPtr bufferPtr = h.AddrOfPinnedObject();
                latBand.ReadRaster(xOffset, yOffset, blockWidth, blockHeight, bufferPtr, blockWidth, blockHeight,
                    dataType, 0, 0);
            }
            finally
            {
                h.Free();
            }
        }

        internal void ReadImgBand(Band band, int xOffset, int yOffset, int xSize, int ySize, Size bufferSize,
            out ushort[] bandData)
        {
            bandData = new ushort[bufferSize.Width * bufferSize.Height];
            band.ReadRaster(xOffset, yOffset, xSize, ySize, bandData, bufferSize.Width, bufferSize.Height, 0, 0);
        }

        internal virtual void ReadBandForPrj<T>(Band band, int xOffset, int yOffset, int xSize, int ySize,
            Size bufferSize, out T[] bandData)
        {
            bandData = new T[bufferSize.Width * bufferSize.Height];
            GCHandle h = GCHandle.Alloc(bandData, GCHandleType.Pinned);
            try
            {
                IntPtr bufferPtr = h.AddrOfPinnedObject();
                band.ReadRaster(xOffset, yOffset, xSize, ySize, bufferPtr, bufferSize.Width, bufferSize.Height,
                    BlockOper.DataTypeHelper.Type2PixelDataType(typeof(T)), 0, 0);
            }
            finally
            {
                h.Free();
            }
        }

        internal virtual void ReadAgileBand(Band band, int xOffset, int yOffset, int xSize, int ySize, Size bufferSize,
            out short[] bandData)
        {
            bandData = new short[bufferSize.Width * bufferSize.Height];

            band.ReadRaster(xOffset, yOffset, xSize, ySize, bandData, bufferSize.Width, bufferSize.Height, 0, 0);
        }

        protected virtual void ReadLocations(AbstractWarpDataset geoRaster, out Band longitudeBand,
            out Band latitudeBand)
        {
            Band[] lonsBands = geoRaster.GetBands("Longitude");
            Band[] latBands = geoRaster.GetBands("Latitude");
            if (lonsBands == null || latBands == null || lonsBands.Length == 0 || latBands.Length == 0 ||
                lonsBands[0] == null || latBands[0] == null)
                throw new Exception("获取经纬度数据集失败");
            longitudeBand = lonsBands[0];
            latitudeBand = latBands[0];
        }

        /// <summary>
        /// 按照指定的偏移量读取
        /// </summary>
        /// <param name="rasterBand"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <param name="blockWidth"></param>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        protected double[] ReadBlockDatas(Band rasterBand, int xOffset, int yOffset, int blockWidth, int blockHeight)
        {
            double[] bandData = new double[blockWidth * blockHeight];

            rasterBand.ReadRaster(xOffset, yOffset, blockWidth, blockHeight, bandData, blockWidth, blockHeight,
                0, 0);
            return bandData;
        }

        /// <summary>
        /// 按照指定的区域采样读取
        /// </summary>
        /// <param name="rasterBand"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <param name="blockWidth"></param>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        internal double[] ReadSampleDatas(Band rasterBand, int xOffset, int yOffset, int blockWidth, int blockHeight)
        {
            double[] bandData = new double[blockWidth * blockHeight];
            rasterBand.ReadRaster(xOffset, yOffset, rasterBand.XSize, rasterBand.YSize, bandData, blockWidth,
                blockHeight, 0, 0);
            return bandData;
        }

        protected abstract void ReadLocations(AbstractWarpDataset srcRaster, out double[] xs, out double[] ys,
            out Size locationSize);

        /// <summary>
        /// 指定采样大小，读取经纬度数据集数据
        /// </summary>
        /// <param name="longitudeBand"></param>
        /// <param name="latitudeBand"></param>
        /// <param name="maxGeoSize"></param>
        /// <param name="xs"></param>
        /// <param name="ys"></param>
        /// <param name="locationSize"></param>
        protected void ReadLocations(Band longitudeBand, Band latitudeBand, Size maxGeoSize, out double[] xs,
            out double[] ys, out Size locationSize)
        {
            int sampleWidth = longitudeBand.XSize;
            int sampleHeight = longitudeBand.YSize;
            if (maxGeoSize.Width < sampleWidth)
                sampleWidth = maxGeoSize.Width;
            if (maxGeoSize.Height < sampleHeight)
                sampleHeight = maxGeoSize.Height;
            Size sampleSize = new Size(sampleWidth, sampleHeight);
            ReadBandData(longitudeBand, sampleSize, out xs);
            ReadBandData(latitudeBand, sampleSize, out ys);
            locationSize = sampleSize;
        }

        /// <summary>
        /// 按照指定的采样比例大小读取数据
        /// </summary>
        /// <param name="band"></param>
        /// <param name="sampleSize"></param>
        /// <param name="bandData"></param>
        /// <param name="srcSize"></param>
        private void ReadBandData(Band band, Size sampleSize, out double[] bandData)
        {
            int width = band.XSize;
            int height = band.YSize;
            int sampleWidth = sampleSize.Width;
            int sampleHeight = sampleSize.Height;
            bandData = new Double[sampleSize.Width * sampleSize.Height];
            band.ReadRaster(0, 0, width, height, bandData, sampleWidth, sampleHeight, 0, 0);
        }

        #endregion

        #region Ready

        /// <summary>
        /// 准备要投影的四个角度波段
        /// </summary>
        /// <param name="srcAngleRaster"></param>
        /// <param name="mainFilename"></param>
        /// <param name="prjSettings"></param>
        /// <param name="outSize"></param>
        /// <param name="options"></param>
        internal void ReadyAngleFiles(AbstractWarpDataset srcAngleRaster, string mainFilename,
            FilePrjSettings prjSettings, Size outSize, string[] options)
        {
            this._prjSettings = prjSettings;
            _angleBands = null;
            if (prjSettings.ExtArgs == null || prjSettings.ExtArgs.Length == 0)
                return;
            List<Band> srcAngleBands = new List<Band>();
            List<AbstractWarpDataset> dstAngleRasters = new List<AbstractWarpDataset>();
            List<Band> dstAngleBands = new List<Band>();

            foreach (string extarg in prjSettings.ExtArgs)
            {
                if (_supportAngles.Contains(extarg))
                {
                    Band[] band = srcAngleRaster.GetBands(extarg);
                    if (band != null && band.Length != 0)
                    {
                        srcAngleBands.Add(band[0]);
                        string fileName = Path.ChangeExtension(mainFilename, extarg + ".ldf");

                        List<string> opts = new List<string>(options);
                        opts.Add("BANDNAMES=" + extarg);
                        AbstractWarpDataset dstAngleRaster =
                            CreateOutFile(fileName, 1, outSize, DataType.GDT_Int16, opts.ToArray());
                        Band dstband = dstAngleRaster.GetRasterBand(0);
                        dstAngleRasters.Add(dstAngleRaster);
                        dstAngleBands.Add(dstband);
                    }
                }
            }

            _angleBands = srcAngleBands.ToArray();
            _dstAngleRasters = dstAngleRasters.ToArray();
            _dstAngleBands = dstAngleBands.ToArray();
        }

        /// <summary> 
        /// 准备定位信息,计算投影后的值，并计算范围
        /// </summary>
        protected void ReadyLocations(AbstractWarpDataset geoRaster, SpatialReference srcSpatialRef,
            SpatialReference dstSpatialRef, out Size geoSize,
            out double[] xs, out double[] ys, out PrjEnvelope maxPrjEnvelope, Action<int, string> progressCallback)
        {
            ReadLocations(geoRaster, out xs, out ys, out geoSize);
            TryResetLonlatForLeftRightInvalid(xs, ys, geoSize);
            //PrjEnvelope maskEnvelope = new PrjEnvelope(-180d, 180d, -90d, 90d);
            _rasterProjector.ComputeDstEnvelope(srcSpatialRef, xs, ys, geoSize, dstSpatialRef, out maxPrjEnvelope,
                progressCallback);
        }

        internal virtual void ReadyExtBands(AbstractWarpDataset srcRaster, string mainFilename,
            FilePrjSettings prjSettings, Size outSize, string[] options)
        {
            _extSrcBands = null;
            if (prjSettings.ExtArgs == null || prjSettings.ExtArgs.Length == 0)
                return;
            if (_supportExtBandNames == null)
                return;
            string[] extBandNames = TryParseExtBands("ExtBands", prjSettings.ExtArgs);
            if (extBandNames == null || extBandNames.Length == 0)
                return;
            List<Band> srcBands = new List<Band>();
            List<AbstractWarpDataset> dstRasters = new List<AbstractWarpDataset>();
            List<Band> dstBands = new List<Band>();

            foreach (string extBandName in extBandNames)
            {
                if (_supportExtBandNames.Contains(extBandName))
                {
                    Band[] band = srcRaster.GetBands(extBandName);
                    if (band != null && band.Length != 0)
                    {
                        srcBands.Add(band[0]);
                        string fileName = Path.ChangeExtension(mainFilename, extBandName + ".ldf");
                        List<string> opts = new List<string>(options);
                        opts.Add("BANDNAMES=" + extBandName);
                        AbstractWarpDataset dstAngleRaster = CreateOutFile(fileName, 1, outSize,
                            band[0].DataType, opts.ToArray());
                        Band dstband = dstAngleRaster.GetRasterBand(0);
                        dstRasters.Add(dstAngleRaster);
                        dstBands.Add(dstband);
                    }
                }
            }

            _extSrcBands = srcBands.ToArray();
            _extDstRasters = dstRasters.ToArray();
            _extDstBands = dstBands.ToArray();
        }

        protected void ReadyExtBands2(AbstractWarpDataset srcRaster, string mainFilename, FilePrjSettings prjSettings,
            Size outSize, string[] options)
        {
            //OutTrueColor
            _extSrcBands2 = null;
            if (prjSettings.ExtArgs == null || prjSettings.ExtArgs.Length == 0)
                return;
            string[] extBandNames = TryParseExtBands("ExtBands2", prjSettings.ExtArgs);
            if (extBandNames == null || extBandNames.Length == 0)
                return;
            List<Band> srcBands = new List<Band>();

            foreach (string extBandName in extBandNames)
            {
                Band[] band = srcRaster.GetBands(extBandName);
                if (band != null && band.Length != 0)
                {
                    srcBands.Add(band[0]);
                }
            }

            if (srcBands.Count == 5 && prjSettings.ExtArgs.Contains("OutTrueColor"))
            {
                if (prjSettings.OutBandNos.Contains(1) && prjSettings.OutBandNos.Contains(2) &&
                    prjSettings.OutBandNos.Contains(3))
                {
                    List<AbstractWarpDataset> dstRasters = new List<AbstractWarpDataset>();
                    List<Band> dstBands = new List<Band>();
                    string fileName = Path.ChangeExtension(mainFilename, "TrueColor.ldf");
                    AbstractWarpDataset dstRaster = CreateOutFile(fileName, 3, outSize, DataType.GDT_Byte, null);
                    dstRasters.Add(dstRaster);
                    for (int i = 0; i < dstRaster.BandCount; i++)
                    {
                        Band band = dstRaster.GetRasterBand(i);
                        //band.SetNoDataValue(0);
                        dstBands.Add(band);
                    }

                    _extDstRasters2 = dstRasters.ToArray();
                    _extDstBands2 = dstBands.ToArray();
                }
            }

            _extSrcBands2 = srcBands.ToArray();
        }

        #endregion


        public virtual void BeginSession(AbstractWarpDataset srcRaster)
        {
            _curSession = srcRaster;
            _isBeginSession = true;
        }

        public virtual void EndSession()
        {
            _curSession = null;
            _isBeginSession = false;
            if (_rasterDataBands != null && _rasterDataBands.Length != 0)
            {
                for (int i = 0; i < _rasterDataBands.Length; i++)
                {
                    if (_rasterDataBands[i] != null)
                    {
                        (_rasterDataBands[i] as IDisposable).Dispose();
                        _rasterDataBands[i] = null;
                    }
                }

                _rasterDataBands = null;
            }

            if (_dstAngleRasters != null && _dstAngleRasters.Length != 0)
            {
                for (int i = 0; i < _dstAngleRasters.Length; i++)
                {
                    if (_dstAngleRasters[i] != null)
                    {
                        _dstAngleRasters[i].Dispose();
                        _dstAngleRasters[i] = null;
                    }
                }

                _dstAngleRasters = null;
            }

            if (_solarZenithCacheRaster != null)
            {
                _solarZenithCacheRaster.Dispose();
                _solarZenithCacheRaster = null;
            }

            if (_sensorSenithBand != null)
            {
                (_sensorSenithBand as IDisposable).Dispose();
                _sensorSenithBand = null;
            }

            if (_sensorSenithRaster != null)
            {
                _sensorSenithRaster.Dispose();
                _sensorSenithRaster = null;
            }

            if (_extSrcBands != null && _extSrcBands.Length != 0)
            {
                for (int i = 0; i < _extSrcBands.Length; i++)
                {
                    if (_extSrcBands[i] != null)
                    {
                        (_extSrcBands[i] as IDisposable).Dispose();
                        _extSrcBands[i] = null;
                    }
                }

                _extSrcBands = null;
            }

            if (_extDstBands != null && _extDstBands.Length != 0)
            {
                for (int i = 0; i < _extDstBands.Length; i++)
                {
                    if (_extDstBands[i] != null)
                    {
                        (_extDstBands[i] as IDisposable).Dispose();
                        _extDstBands[i] = null;
                    }
                }

                _extDstBands = null;
            }

            if (_extDstBands2 != null && _extDstBands2.Length != 0)
            {
                for (int i = 0; i < _extDstBands2.Length; i++)
                {
                    if (_extDstBands2[i] != null)
                    {
                        (_extDstBands2[i] as IDisposable).Dispose();
                        _extDstBands2[i] = null;
                    }
                }

                _extDstBands = null;
            }

            if (_extDstRasters != null && _extDstRasters.Length != 0)
            {
                for (int i = 0; i < _extDstRasters.Length; i++)
                {
                    if (_extDstRasters[i] != null)
                    {
                        _extDstRasters[i].Dispose();
                        _extDstRasters[i] = null;
                    }
                }

                _extDstRasters = null;
            }

            if (_extDstRasters2 != null && _extDstRasters2.Length != 0)
            {
                for (int i = 0; i < _extDstRasters2.Length; i++)
                {
                    if (_extDstRasters2[i] != null)
                    {
                        _extDstRasters2[i].Dispose();
                        _extDstRasters2[i] = null;
                    }
                }

                _extDstRasters2 = null;
            }
        }

        public virtual void Dispose()
        {
        }

        internal void GetEnvelope(double[] xs, double[] ys, int w, int h, PrjEnvelope envelope, out Block block)
        {
            int length = xs.Length;
            int rOffset;
            int index;
            double x;
            double y;
            int xMin = w;
            int xMax = 0;
            int yMin = h;
            int yMax = 0;
            bool hasContain = false;
            for (int i = 0; i < h; i++)
            {
                rOffset = i * w;
                for (int j = 0; j < w; j++)
                {
                    index = rOffset + j;
                    x = xs[index];
                    y = ys[index];
                    if (envelope.Contains(x, y))
                    {
                        if (!hasContain)
                            hasContain = true;
                        if (xMin > j)
                            xMin = j;
                        if (xMax < j)
                            xMax = j;
                        if (yMin > i)
                            yMin = i;
                        if (yMax < i)
                            yMax = i;
                    }
                }
            }

            if (!hasContain)
            {
                block = Block.Empty;
                return;
            }

            //扩大16个像素，防止投影变形造成边缘像素缺失
            int sc = 16;
            xMin = xMin - sc < 0 ? 0 : xMin - sc;
            xMax = xMax + sc >= w - 1 ? w - 1 : xMax + sc;
            yMin = yMin - sc < 0 ? 0 : yMin - 10;
            yMax = yMax + sc >= h - 1 ? h - 1 : yMax + sc;
            //设置从整个扫描线行开始,为了有效去除条带
            if (_sacnLineWidth > 1)
            {
                int pYMin = yMin % _sacnLineWidth;
                if (pYMin != 0)
                    yMin = yMin - pYMin;
                int pYMax = (yMax + 1) % _sacnLineWidth;
                if (pYMax != 0)
                    yMax = yMax + (_sacnLineWidth - pYMax);
            }

            //防止偏移扫描线偏移后,超过范围
            yMin = yMin < 0 ? 0 : yMin;
            yMax = yMax >= h - 1 ? h - 1 : yMax;
            block = new Block {xBegin = xMin, xEnd = xMax, yBegin = yMin, yEnd = yMax};
        }

        internal void GetBlockDatas(double[] xs, double[] ys, int w, int h, int offBeginx, int offBeginy, int blockW,
            int blockH, out double[] blockXs, out double[] blockYs)
        {
            blockXs = new double[blockW * blockH];
            blockYs = new double[blockW * blockH];
            for (int i = 0; i < blockH; i++)
            {
                for (int j = 0; j < blockW; j++)
                {
                    blockXs[i * blockW + j] = xs[(i + offBeginy) * w + j + offBeginx];
                    blockYs[i * blockW + j] = ys[(i + offBeginy) * w + j + offBeginx];
                }
            }
        }

        internal string GetSolarZenithCacheFilename(string srcFilename)
        {
            return GetCacheFilename(srcFilename, "solarZenith.ldf");
        }

        #region 缓存目录

        protected string _catchDir = null;

        internal string GetCacheFilename(string srcFilename, string dstFilename)
        {
            _catchDir = Path.Combine(Directory.GetCurrentDirectory(), "prjChche", Path.GetFileName(srcFilename));
            string dataFilename = Path.Combine(_catchDir, dstFilename);
            if (!Directory.Exists(_catchDir))
                Directory.CreateDirectory(_catchDir);
            return dataFilename;
        }

        internal void TryDeleteCurCatch()
        {
            if (!string.IsNullOrWhiteSpace(_catchDir) && Directory.Exists(_catchDir))
            {
                try
                {
                    Directory.Delete(_catchDir, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("删除缓存目录失败：\r\n" + _catchDir + "\r\n错误信息：", ex);
                }
            }
        }

        #endregion


        internal void InvalidLongLat(double[] xs, double[] ys, int width, int height, int left, int right)
        {
            int rightBegin = width - right;
            for (int j = 0; j < height; j++)
            {
                for (int m = 0; m < left; m++)
                {
                    int index = j * width + m;
                    xs[index] = 999d;
                }

                for (int n = rightBegin; n < width; n++)
                {
                    int index = j * width + n;
                    xs[index] = 999d;
                }
            }
        }

        internal AbstractWarpDataset WriteData(float[] data, string fileName, int width, int height)
        {
            string[] _options = new string[] {"header_offset=128"};
            Dataset ds =
                DatasetFactory.CreateRasterDataset(fileName, width, height, 1, DataType.GDT_Float32, "ENVI", null);
            if (ds == null)
                throw new ArgumentException("请检查输出文件路径");
            Band band = ds.GetRasterBand(1);

            band.WriteRaster(0, 0, width, height, data, width, height, 0, 0);


            AbstractWarpDataset cacheWriter = new WarpDataset(ds, fileName);
            return cacheWriter;
        }

        internal AbstractWarpDataset CreateOutFile(string outfilename, int dstBandCount, Size outSize, string[] options)
        {
            CheckAndCreateDir(Path.GetDirectoryName(outfilename));
            string[] _options = new string[] {"header_offset=128"};
            Dataset ds = DatasetFactory.CreateRasterDataset(outfilename, outSize.Width, outSize.Height
                , dstBandCount, DataType.GDT_UInt16, "ENVI", null);
            if (ds == null)
                throw new ArgumentException("请检查输出文件路径");
            var srs = _dstSpatialRef;
            double[] geoTrans = new double[6]
            {
                _dstEnvelope.MinX, Convert.ToDouble(_outResolutionX.ToString("f6")), 0, _dstEnvelope.MaxY, 0,
                -Convert.ToDouble(_outResolutionY.ToString("f6"))
            };
            ds.SetProjection(srs.ExportToWkt());
            ds.SetGeoTransform(geoTrans);
            if (_NODATA_VALUE.HasValue)
                Enumerable.Range(0, dstBandCount).ToList()
                    .ForEach(t => ds.GetRasterBand(t).SetNoDataValue(_NODATA_VALUE.Value));
            return new WarpDataset(ds, outfilename);
        }

        internal void UpdateNodataValue(AbstractWarpDataset ds)
        {
            if (_NODATA_VALUE.HasValue)
                Enumerable.Range(0, ds.BandCount).ToList()
                    .ForEach(t => ds.GetRasterBand(t).SetNoDataValue(_NODATA_VALUE.Value));
        }

        internal AbstractWarpDataset CreateOutFile(string outfilename, int dstBandCount, Size outSize,
            DataType dataType, string[] options)
        {
            CheckAndCreateDir(Path.GetDirectoryName(outfilename));
            string[] _options = new string[] {"header_offset=128"};
            Dataset ds = DatasetFactory.CreateRasterDataset(outfilename, outSize.Width, outSize.Height
                , dstBandCount, dataType, "ENVI", null);

            var srs = _dstSpatialRef;
            double[] geoTrans = new double[6]
            {
                _dstEnvelope.MinX, Convert.ToDouble(_outResolutionX.ToString("f6")), 0, _dstEnvelope.MaxY, 0,
                -Convert.ToDouble(_outResolutionY.ToString("f6"))
            };
            ds.SetProjection(srs.ExportToWkt());
            ds.SetGeoTransform(geoTrans);
            //Enumerable.Range(0, dstBandCount).ToList().ForEach(t => ds.GetRasterBand(t).SetNoDataValue(9999));
            return new WarpDataset(ds, outfilename);
        }

        private string[] TryParseExtBands(string keyName, object[] extArgs)
        {
            Regex _extKeyValues = new Regex(@"(?<key>\w+)\s*=\s*(?<value>[\w|\W]*)\s*", RegexOptions.Compiled);
            if (extArgs != null && extArgs.Length != 0)
            {
                foreach (object extArg in extArgs)
                {
                    if (extArg is string)
                    {
                        string strExtArg = extArg as string;
                        Match match = _extKeyValues.Match(strExtArg);
                        if (match.Success)
                        {
                            string key = match.Groups["key"].Value;
                            string value = match.Groups["value"].Value;
                            if (key == keyName)
                            {
                                return value.Split(';');
                            }
                        }
                    }
                }
            }

            return null;
        }

        internal void ProjectAngle(Size dstbufferSize, Size srcBufferSize, int blockWidth, int blockHeight,
            int blockYIndex, int blockXIndex, Block curOrbitblock, UInt16[] dstRowLookUpTable,
            UInt16[] dstColLookUpTable, Action<int, string> progressCallback)
        {
            if (_angleBands != null && _angleBands.Length != 0 && _dstAngleRasters != null &&
                _dstAngleRasters.Length != 0)
            {
                short[] srcBandData = null;
                short[] dstBandData = new short[dstbufferSize.Width * dstbufferSize.Height];
                for (int i = 0; i < _angleBands.Length; i++)
                {
                    if (progressCallback != null)
                    {
                        progress++;
                        percent = progress * 100 / progressCount;
                        progressCallback(percent, string.Format("投影角度数据{0}%", percent));
                    }

                    Band srcAngleBand = _angleBands[i];
                    ReadAgileBand(srcAngleBand, curOrbitblock.xBegin, curOrbitblock.yBegin, curOrbitblock.Width,
                        curOrbitblock.Height, srcBufferSize, out srcBandData);
                    _rasterProjector.Project<short>(srcBandData, srcBufferSize, dstRowLookUpTable, dstColLookUpTable,
                        dstbufferSize, dstBandData, 0, null);
                    Band dstAngleBand = _dstAngleBands[i];
                    int blockOffsetY = blockYIndex * blockHeight;
                    int blockOffsetX = blockXIndex * blockWidth;
                    dstAngleBand.WriteRaster(blockOffsetX, blockOffsetY, blockWidth, blockHeight, dstBandData,
                        dstbufferSize.Width, dstbufferSize.Height, 0, 0);
                }
            }
        }


        internal void ProjectTrueColor(AbstractWarpDataset srcImgRaster, Size dstbufferSize, Size srcBufferSize,
            int blockWidth, int blockHeight, int blockYNo, int blockXNo, Block curOrbitblock,
            UInt16[] dstRowLookUpTable, UInt16[] dstColLookUpTable, Action<int, string> progressCallback)
        {
            if (_extDstBands2 == null || _extDstBands2.Length != 3)
            {
                return;
            }

            if (_extSrcBands2 != null && _extSrcBands2.Length == 5 && _prjSettings.ExtArgs.Contains("OutTrueColor"))
            {
                if (_prjSettings.OutBandNos.Contains(1) && _prjSettings.OutBandNos.Contains(2) &&
                    _prjSettings.OutBandNos.Contains(3))
                {
                    if (progressCallback != null)
                    {
                        progress++;
                        percent = progress * 100 / progressCount;
                        progressCallback(percent, string.Format("投影真彩色数据{0}%,请耐心等待", percent));
                    }

                    int angle2geoRatioX = _srcLocationSize.Width / _solarZenithCacheRaster.Width;
                    int angle2geoRatioY = _srcLocationSize.Height / _solarZenithCacheRaster.Height;
                    Size angleBlockSize = new Size(curOrbitblock.Width / angle2geoRatioX,
                        curOrbitblock.Height / angle2geoRatioY);
                    int xOffset = curOrbitblock.xBegin / angle2geoRatioX;
                    int yOffset = curOrbitblock.yBegin / angle2geoRatioY;

                    int imgLocationRatioX = srcImgRaster.Width / _srcLocationSize.Width;
                    int imgLocationRatioY = srcImgRaster.Height / _srcLocationSize.Height;
                    int srcBlockImgWidth = curOrbitblock.Width * imgLocationRatioX;
                    int srcBlockImgHeight = curOrbitblock.Height * imgLocationRatioY;
                    Size angleBufferSize = new Size(srcBlockImgWidth, srcBlockImgHeight);

                    float[] refCalData = new float[3 * 19];
                    Band[] rasterBands = srcImgRaster.GetBands("Calibration/VIS_Cal_Coeff");
                    if (rasterBands != null && rasterBands.Length > 0)
                    {
                        rasterBands[0].ReadRaster(0, 0, 3, 19, refCalData, 3, 19, 0, 0);
                    }
                    else
                    {
                        return;
                    }

                    //读取三波段数据
                    //读取四种角度数据和DEM
                    //SensorAzimuth;SensorZenith;SolarAzimuth;SolarZenith;DEM
                    ushort[][] srcBuffer = new ushort[3][];
                    float[] demData = null,
                        sensorAzimuthData = null,
                        sensorZenithData = null,
                        solarAzimuthData = null,
                        solarZenithData = null;
                    ReadImgBand(out srcBuffer[0], 0, curOrbitblock.xBegin * imgLocationRatioX,
                        curOrbitblock.yBegin * imgLocationRatioY, srcBlockImgWidth, srcBlockImgHeight);
                    ReadImgBand(out srcBuffer[1], 1, curOrbitblock.xBegin * imgLocationRatioX,
                        curOrbitblock.yBegin * imgLocationRatioY, srcBlockImgWidth, srcBlockImgHeight);
                    ReadImgBand(out srcBuffer[2], 2, curOrbitblock.xBegin * imgLocationRatioX,
                        curOrbitblock.yBegin * imgLocationRatioY, srcBlockImgWidth, srcBlockImgHeight);
                    ReadBandForPrj(_extSrcBands2[0], xOffset, yOffset, angleBlockSize.Width, angleBlockSize.Height,
                        angleBufferSize, out sensorAzimuthData);
                    ReadBandForPrj(_extSrcBands2[1], xOffset, yOffset, angleBlockSize.Width, angleBlockSize.Height,
                        angleBufferSize, out sensorZenithData);
                    ReadBandForPrj(_extSrcBands2[2], xOffset, yOffset, angleBlockSize.Width, angleBlockSize.Height,
                        angleBufferSize, out solarAzimuthData);
                    ReadBandForPrj(_extSrcBands2[3], xOffset, yOffset, angleBlockSize.Width, angleBlockSize.Height,
                        angleBufferSize, out solarZenithData);
                    ReadBandForPrj(_extSrcBands2[4], xOffset, yOffset, angleBlockSize.Width, angleBlockSize.Height,
                        angleBufferSize, out demData);
                    Parallel.For(0, srcBlockImgWidth * srcBlockImgHeight, i =>
                    {
                        float t0 = solarZenithData[i] * 0.01f; //太阳天顶角
                        float t1 = sensorZenithData[i] * 0.01f; //卫星天顶角
                        float ph0 = solarAzimuthData[i] * 0.01f; //太阳方位角
                        float ph1 = sensorAzimuthData[i] * 0.01f; //卫星方位角

                        float phi = ph0 - ph1; //相对方位角
                        float mus = Convert.ToSingle(Math.Cos(t0 * DEG2RAD));
                        float muv = Convert.ToSingle(Math.Cos(t1 * DEG2RAD));

                        float[] sphalb = new float[3];
                        float[] rhoray = new float[3];
                        float[] TtotraytH2O = new float[3];
                        float[] tOG = new float[3];
                        if (t0 > 89.0)
                        {
                            //dark night 
                            srcBuffer[0][i] = 0;
                            srcBuffer[1][i] = 0;
                            srcBuffer[2][i] = 0;
                        }
                        else
                        {
                            int atmok = getatmvariables(mus, muv, phi, demData[i], sphalb, rhoray, TtotraytH2O, tOG);
                            if (atmok == 0)
                            {
                                //0 1 2
                                //3 4 5
                                //6 7 8
                                //refCalData
                                for (int iband = 0; iband < 3; ++iband)
                                {
                                    double bval = srcBuffer[iband][i]; //dn值
                                    double refl = bval * bval * refCalData[2 + iband * 3] +
                                                  bval * refCalData[1 + iband * 3] + refCalData[iband * 3]; //反射率
                                    refl /= 100.0;
                                    double rtoa = refl / mus; //大气顶层反射率
                                    if (rtoa < 0) rtoa = 0;
                                    if (rtoa > 1.1) rtoa = 1.1;

                                    //返回大气校正后的反射率
                                    double acVal = correctedrefl(rtoa, TtotraytH2O[iband], tOG[iband], rhoray[iband],
                                        sphalb[iband]);

                                    //linear 0-255
                                    ushort newdn = linear255(acVal);

                                    // inlinear 0-255
                                    newdn = jac255(newdn);
                                    srcBuffer[iband][i] = newdn;
                                }
                            }
                            else
                            {
                                srcBuffer[0][i] = 0;
                                srcBuffer[1][i] = 0;
                                srcBuffer[2][i] = 0;
                            }
                        }
                    });
                    //for (int i = 0; i < srcBlockImgWidth * srcBlockImgHeight; i++)
                    //{
                    //}

                    //投影写入
                    for (int i = 0; i < 3; i++)
                    {
                        ushort[] dstBandData = new ushort[dstbufferSize.Width * dstbufferSize.Height];
                        _rasterProjector.Project<ushort>(srcBuffer[2 - i], angleBufferSize, dstRowLookUpTable,
                            dstColLookUpTable, dstbufferSize, dstBandData, 0, null);
                        Band trueColorBand = _extDstBands2[i];
                        int blockOffsetY = blockYNo * blockHeight;
                        int blockOffsetX = blockXNo * blockWidth;
                        trueColorBand.WriteRaster(blockOffsetX, blockOffsetY, blockWidth, blockHeight, dstBandData,
                            dstbufferSize.Width, dstbufferSize.Height, 0, 0);
                    }

                    //Size angleSize = new Size(srcBlockJdWidth, srcBlockJdHeight);
                    //DoRadiation(srcImgRaster, i, srcBandData, solarZenithData, srcBlockImgSize, srcAngleBlockSize);
                }
            }
        }

        internal void ProjectExtBands(Size dstbufferSize, Size srcBufferSize, int blockWidth, int blockHeight,
            int blockYNo, int blockXNo, Block curOrbitblock, UInt16[] dstRowLookUpTable, UInt16[] dstColLookUpTable,
            Action<int, string> progressCallback)
        {
            if (_extSrcBands != null && _extSrcBands.Length != 0 && _extDstRasters != null &&
                _extDstRasters.Length != 0)
            {
                for (int i = 0; i < _extSrcBands.Length; i++)
                {
                    Band srcBand = _extSrcBands[i];
                    Band dstBand = _extDstBands[i];
                    DataType dataType = srcBand.DataType;
                    switch (dataType)
                    {
                        case DataType.GDT_Byte:
                            ProjectExtBands<Byte>(srcBand, dstBand, dstbufferSize, srcBufferSize, blockWidth,
                                blockHeight, blockYNo, blockXNo, curOrbitblock, dstRowLookUpTable, dstColLookUpTable,
                                progressCallback);
                            break;
                        case DataType.GDT_Float64:
                            ProjectExtBands<Double>(srcBand, dstBand, dstbufferSize, srcBufferSize, blockWidth,
                                blockHeight, blockYNo, blockXNo, curOrbitblock, dstRowLookUpTable, dstColLookUpTable,
                                progressCallback);
                            break;
                        case DataType.GDT_Float32:
                            ProjectExtBands<Single>(srcBand, dstBand, dstbufferSize, srcBufferSize, blockWidth,
                                blockHeight, blockYNo, blockXNo, curOrbitblock, dstRowLookUpTable, dstColLookUpTable,
                                progressCallback);
                            break;
                        case DataType.GDT_Int16:
                            ProjectExtBands<short>(srcBand, dstBand, dstbufferSize, srcBufferSize, blockWidth,
                                blockHeight, blockYNo, blockXNo, curOrbitblock, dstRowLookUpTable, dstColLookUpTable,
                                progressCallback);
                            break;
                        case DataType.GDT_Int32:
                            ProjectExtBands<Int32>(srcBand, dstBand, dstbufferSize, srcBufferSize, blockWidth,
                                blockHeight, blockYNo, blockXNo, curOrbitblock, dstRowLookUpTable, dstColLookUpTable,
                                progressCallback);
                            break;
                        case DataType.GDT_UInt16:
                            ProjectExtBands<UInt16>(srcBand, dstBand, dstbufferSize, srcBufferSize, blockWidth,
                                blockHeight, blockYNo, blockXNo, curOrbitblock, dstRowLookUpTable, dstColLookUpTable,
                                progressCallback);
                            break;
                        case DataType.GDT_UInt32:
                            ProjectExtBands<UInt32>(srcBand, dstBand, dstbufferSize, srcBufferSize, blockWidth,
                                blockHeight, blockYNo, blockXNo, curOrbitblock, dstRowLookUpTable, dstColLookUpTable,
                                progressCallback);
                            break;
                        case DataType.GDT_Unknown:
                            Console.WriteLine("不支持Unknow类型的波段数据投影");
                            break;
                        default:
                            break;
                    }

                    if (progressCallback != null)
                    {
                        progress++;
                        percent = progress * 100 / progressCount;
                        progressCallback(percent, string.Format("投影扩展波段{0}数据", i)); //_extSrcBands[i].Description
                    }
                }
            }
        }

        internal void ProjectExtBands<T>(Band srcBand, Band dstBand, Size dstbufferSize, Size srcBufferSize,
            int blockWidth, int blockHeight, int blockYIndex, int blockXIndex, Block curOrbitblock,
            UInt16[] dstRowLookUpTable, UInt16[] dstColLookUpTable, Action<int, string> progressCallback)
        {
            T[] srcBandData = null;
            ReadBandForPrj(srcBand, curOrbitblock.xBegin, curOrbitblock.yBegin, curOrbitblock.Width,
                curOrbitblock.Height, srcBufferSize, out srcBandData);
            T[] dstBandData = new T[dstbufferSize.Width * dstbufferSize.Height];
            GCHandle h = GCHandle.Alloc(dstBandData, GCHandleType.Pinned);
            try
            {
                IntPtr bufferPtr = h.AddrOfPinnedObject();
                _rasterProjector.Project<T>(srcBandData, srcBufferSize, dstRowLookUpTable, dstColLookUpTable,
                    dstbufferSize, dstBandData, default(T), null);
                int blockOffsetY = blockYIndex * blockHeight;
                int blockOffsetX = blockXIndex * blockWidth;
                dstBand.WriteRaster(blockOffsetX, blockOffsetY, blockWidth, blockHeight, bufferPtr, dstbufferSize.Width,
                    dstbufferSize.Height, DataType.GDT_Int16, 0, 0);
            }
            finally
            {
                h.Free();
            }
        }

        protected void ProjectRaster(AbstractWarpDataset srcRaster, AbstractWarpDataset prdWriter, int beginBandIndex,
            Action<int, string> progressCallback)
        {
            switch (srcRaster.DataType)
            {
                case DataType.GDT_Unknown:
                    throw new Exception("未知数据类型");
                case DataType.GDT_Byte:
                    ProjectRaster<byte>(srcRaster, prdWriter, 0, progressCallback);
                    break;
                case DataType.GDT_Float64:
                    ProjectRaster<Double>(srcRaster, prdWriter, 0, progressCallback);
                    break;
                case DataType.GDT_Float32:
                    ProjectRaster<float>(srcRaster, prdWriter, 0, progressCallback);
                    break;
                case DataType.GDT_Int16:
                    ProjectRaster<Int16>(srcRaster, prdWriter, 0, progressCallback);
                    break;
                case DataType.GDT_Int32:
                    ProjectRaster<Int32>(srcRaster, prdWriter, 0, progressCallback);
                    break;
                case DataType.GDT_UInt16:
                    ProjectRaster<UInt16>(srcRaster, prdWriter, 0, progressCallback);
                    break;
                case DataType.GDT_UInt32:
                    ProjectRaster<UInt32>(srcRaster, prdWriter, 0, progressCallback);
                    break;
                default:
                    throw new Exception("未知数据类型");
                    break;
            }
        }

        protected void ProjectRaster(AbstractWarpDataset srcRaster, AbstractWarpDataset prdWriter, int beginBandIndex,
            double invalidValue, Action<int, string> progressCallback)
        {
            switch (srcRaster.DataType)
            {
                case DataType.GDT_Unknown:
                    throw new Exception("不支持未知数据类型");
                //case DataType.Byte:
                //    ProjectRaster<sbyte>(srcRaster, prdWriter, 0, (sbyte)invalidValue, progressCallback);
                //    break;
                case DataType.GDT_Byte:
                    ProjectRaster<byte>(srcRaster, prdWriter, 0, (byte) invalidValue, progressCallback);
                    break;
                case DataType.GDT_Float64:
                    ProjectRaster<Double>(srcRaster, prdWriter, 0, (double) invalidValue, progressCallback);
                    break;
                case DataType.GDT_Float32:
                    ProjectRaster<float>(srcRaster, prdWriter, 0, (float) invalidValue, progressCallback);
                    break;
                case DataType.GDT_Int16:
                    ProjectRaster<Int16>(srcRaster, prdWriter, 0, (Int16) invalidValue, progressCallback);
                    break;
                case DataType.GDT_Int32:
                    ProjectRaster<Int32>(srcRaster, prdWriter, 0, (Int32) invalidValue, progressCallback);
                    break;
                case DataType.GDT_UInt16:
                    ProjectRaster<UInt16>(srcRaster, prdWriter, 0, (UInt16) invalidValue, progressCallback);
                    break;
                case DataType.GDT_UInt32:
                    ProjectRaster<UInt32>(srcRaster, prdWriter, 0, (UInt32) invalidValue, progressCallback);
                    break;
                default:
                    throw new Exception("未知数据类型");
            }
        }

        private void ProjectRaster<T>(AbstractWarpDataset srcImgRaster, AbstractWarpDataset prdWriter,
            int beginBandIndex, Action<int, string> progressCallback)
        {
            ProjectRaster<T>(srcImgRaster, prdWriter, beginBandIndex, default(T), progressCallback);
        }

        private void ProjectRaster<T>(AbstractWarpDataset srcImgRaster, AbstractWarpDataset prdWriter,
            int beginBandIndex, T invalidValue, Action<int, string> progressCallback)
        {
            if (srcImgRaster == null || srcImgRaster.Width == 0 || srcImgRaster.Height == 0)
                throw new Exception("投影数据失败：无法读取源数据,或者源数据高或宽为0。");
            Size srcImgSize = new Size(srcImgRaster.Width, srcImgRaster.Height);
            Size outSize = _dstEnvelope.GetSize(_outResolutionX, _outResolutionY);
            float bufferResolutionX = 0f;
            float bufferResolutionY = 0f;
            float outXScale = _srcImgResolution / _outResolutionX;
            float outYScale = _srcImgResolution / _outResolutionY;
            if (outXScale > 1.5f || outYScale > 1.5f)
            {
                bufferResolutionX = _srcImgResolution;
                bufferResolutionY = _srcImgResolution;
            }
            else
            {
                bufferResolutionX = _outResolutionX;
                bufferResolutionY = _outResolutionY;
            }

            int blockXNum;
            int blockYNum;
            int blockWidth;
            int blockHeight;
            GetBlockNumber(outSize, _srcLocationSize, outXScale, outYScale, out blockXNum, out blockYNum,
                out blockWidth, out blockHeight);
            int imgLocationRatioX = (int) Math.Round(srcImgSize.Width * 1.0 / _srcLocationSize.Width);
            int imgLocationRatioY = (int) Math.Round(srcImgSize.Height * 1.0 / _srcLocationSize.Height);
            int outBandCount = (_dstBandCount + (_angleBands == null ? 0 : _angleBands.Length) +
                                (_extSrcBands == null ? 0 : _extSrcBands.Length));

            progressCount = blockYNum * blockXNum * outBandCount;
            progress = 0;
            percent = 0;
            Size bufferSize;
            for (int blockYIndex = 0; blockYIndex < blockYNum; blockYIndex++)
            {
                for (int blockXIndex = 0; blockXIndex < blockXNum; blockXIndex++)
                {
                    bool useGlobeBlock = false;
                    //起始偏移，结束偏移
                    int beginX = blockWidth * blockXIndex;
                    int beginY = blockHeight * blockYIndex;
                    if (beginX >= outSize.Width || beginY >= outSize.Height)
                        continue;
                    if (beginX + blockWidth > outSize.Width)
                        blockWidth = outSize.Width - beginX;
                    if (beginY + blockHeight > outSize.Height)
                        blockHeight = outSize.Height - beginY;

                    //当前块的四角范围
                    double blockMinX = _dstEnvelope.MinX + beginX * _outResolutionX;
                    double blockMaxX = blockMinX + blockWidth * _outResolutionX;
                    double blockMaxY = _dstEnvelope.MaxY - beginY * _outResolutionY;
                    double blockMinY = blockMaxY - blockHeight * _outResolutionY;
                    PrjEnvelope blockEnvelope =
                        new PrjEnvelope(blockMinX, blockMaxX, blockMinY, blockMaxY, _dstSpatialRef);
                    bufferSize = blockEnvelope.GetSize(bufferResolutionX, bufferResolutionY);
                    //根据当前输出块,反算出对应的源数据块起始行列，为了减小后面需要读取的源数据大小
                    Block curOrbitblock = null; //经纬度数据集，计算轨道数据范围偏移
                    double[] srcBlockXs;
                    double[] srcBlockYs;
                    if (blockYNum == 1 && blockXNum == 1) //没分块的情况
                    {
                        curOrbitblock = _orbitBlock.Clone() as Block;

                        if (curOrbitblock.Width == _srcLocationSize.Width &&
                            curOrbitblock.Height == _srcLocationSize.Height)
                        {
                            useGlobeBlock = true;
                            srcBlockXs = _xs;
                            srcBlockYs = _ys;
                        }
                        else
                        {
                            GetBlockDatas(_xs, _ys, _srcLocationSize.Width, _srcLocationSize.Height,
                                curOrbitblock.xBegin, curOrbitblock.yBegin, curOrbitblock.Width, curOrbitblock.Height,
                                out srcBlockXs, out srcBlockYs);
                        }
                    }
                    else
                    {
                        GetEnvelope(_xs, _ys, _srcLocationSize.Width, _srcLocationSize.Height, blockEnvelope,
                            out curOrbitblock);
                        if (curOrbitblock.Width <= 0 || curOrbitblock.Height <= 0) //当前分块不在图像内部
                        {
                            progress += _dstBandCount;
                            continue;
                        }

                        if (curOrbitblock.xBegin > _left)
                            curOrbitblock.xBegin = _left;
                        if (curOrbitblock.xEnd > _srcLocationSize.Width - 1 - _right)
                            curOrbitblock.xEnd = _srcLocationSize.Width - 1 - _right;
                        GetBlockDatas(_xs, _ys, _srcLocationSize.Width, _srcLocationSize.Height, curOrbitblock.xBegin,
                            curOrbitblock.yBegin, curOrbitblock.Width, curOrbitblock.Height, out srcBlockXs,
                            out srcBlockYs);
                    }

                    int srcBlockJdWidth = curOrbitblock.Width;
                    int srcBlockJdHeight = curOrbitblock.Height;
                    int srcBlockImgWidth = curOrbitblock.Width * imgLocationRatioX;
                    int srcBlockImgHeight = curOrbitblock.Height * imgLocationRatioY;
                    Size srcBlockLocationSize = new Size(srcBlockJdWidth, srcBlockJdHeight);
                    Size srcBlockImgSize = new Size(srcBlockImgWidth, srcBlockImgHeight);

                    //计算当前分块的投影查算表
                    UInt16[] dstRowLookUpTable = new UInt16[bufferSize.Width * bufferSize.Height];
                    UInt16[] dstColLookUpTable = new UInt16[bufferSize.Width * bufferSize.Height];
                    if (imgLocationRatioX == 1)
                        _rasterProjector.ComputeIndexMapTable(srcBlockXs, srcBlockYs, srcBlockImgSize, bufferSize,
                            blockEnvelope, _maxPrjEnvelope,
                            out dstRowLookUpTable, out dstColLookUpTable, null);
                    else
                        _rasterProjector.ComputeIndexMapTable(srcBlockXs, srcBlockYs, srcBlockLocationSize,
                            srcBlockImgSize, bufferSize, blockEnvelope, //_maxPrjEnvelope,
                            out dstRowLookUpTable, out dstColLookUpTable, null, _sacnLineWidth);
                    DataType dataType = srcImgRaster.DataType;
                    //执行投影
                    for (int i = 0; i < _dstBandCount; i++) //读取原始通道值，投影到目标区域
                    {
                        T[] srcBandData = null;
                        T[] dstBandData = new T[bufferSize.Width * bufferSize.Height];
                        if (progressCallback != null)
                        {
                            progress++;
                            percent = (int) (progress * 100 / progressCount);
                            System.Diagnostics.Debug.WriteLine("投影完成{0}%", percent);
                            progressCallback(percent, string.Format("投影完成{0}%", percent));
                        }

                        ReadImgBand(out srcBandData, dataType, i, curOrbitblock.xBegin * imgLocationRatioX,
                            curOrbitblock.yBegin * imgLocationRatioY, srcBlockImgWidth, srcBlockImgHeight);
                        Size angleSize = new Size(srcBlockJdWidth, srcBlockJdHeight);
                        _rasterProjector.Project<T>(srcBandData, srcBlockImgSize, dstRowLookUpTable, dstColLookUpTable,
                            bufferSize, dstBandData, invalidValue, null);
                        srcBandData = null;
                        Band band = prdWriter.GetRasterBand(i);
                        GCHandle h = GCHandle.Alloc(dstBandData, GCHandleType.Pinned);
                        try
                        {
                            IntPtr bufferPtr = h.AddrOfPinnedObject();
                            int blockOffsetY = blockHeight * blockYIndex;
                            int blockOffsetX = blockWidth * blockXIndex;
                            band.WriteRaster(blockOffsetX, blockOffsetY, blockWidth, blockHeight, bufferPtr,
                                bufferSize.Width,
                                bufferSize.Height, dataType, 0, 0);
                        }
                        finally
                        {
                            h.Free();
                        }

                        dstBandData = null;
                    }

                    //ReleaseZenithData();
                    //Size srcBufferSize = new Size(srcBlockImgWidth, srcBlockImgHeight);
                    //ProjectAngle(bufferSize, srcBufferSize, blockWidth, blockHeight, blockYIndex, blockXIndex, curOrbitblock, dstRowLookUpTable, dstColLookUpTable, progressCallback);
                    dstRowLookUpTable = null;
                    dstColLookUpTable = null;
                }
            }
        }

        protected void ProjectToLDF(AbstractWarpDataset srcImgRaster, AbstractWarpDataset dstImgRaster,
            int beginBandIndex, Action<int, string> progressCallback)
        {
            //progressCallback = progressCallback;
            if (srcImgRaster == null || srcImgRaster.Width == 0 || srcImgRaster.Height == 0)
                throw new Exception("投影数据失败：无法读取源数据,或者源数据高或宽为0。");
            Size srcImgSize = new Size(srcImgRaster.Width, srcImgRaster.Height);
            Size outSize = _dstEnvelope.GetSize(_outResolutionX, _outResolutionY);
            float bufferResolutionX = 0f;
            float bufferResolutionY = 0f;
            float outXScale = _srcImgResolution / _outResolutionX;
            float outYScale = _srcImgResolution / _outResolutionY;
            if (outXScale > 1.5f || outYScale > 1.5f)
            {
                bufferResolutionX = _srcImgResolution;
                bufferResolutionY = _srcImgResolution;
            }
            else
            {
                bufferResolutionX = _outResolutionX;
                bufferResolutionY = _outResolutionY;
            }

            int blockXCount, blockYCount, blockWidth, blockHeight;
            //后面投影需要的内存：（double）经纬度数据、（int16）原始通道数据、（int16）投影后通道、（int16）其他（如角度数据等）
            GetBlockNumber(outSize, _srcLocationSize, outXScale, outYScale, out blockXCount, out blockYCount,
                out blockWidth, out blockHeight);
            int imgLocationRatioX = srcImgSize.Width / _srcLocationSize.Width;
            int imgLocationRatioY = srcImgSize.Height / _srcLocationSize.Height;
            int outBandCount = _dstBandCount + (_angleBands == null ? 0 : _angleBands.Length) +
                               (_extSrcBands == null ? 0 : _extSrcBands.Length);
            if (_prjSettings.ExtArgs != null)
                outBandCount += _prjSettings.ExtArgs.Contains("OutTrueColor") ? 1 : 0;
            progressCount = blockYCount * blockXCount * outBandCount;
            progress = 0;
            percent = 0;

            #region 在需要分块的情况下，采样经纬度数据集

            int bC = 1;
            int tmpWidth = 0;
            int tmpHeight = 0;
            double[] tmpxs = null;
            double[] tmpys = null;
            if (blockYCount * blockXCount > 1 && (_xs == null || _ys == null))
            {
                bC = (int) Math.Sqrt(blockXCount * blockYCount) + 1;
                tmpWidth = _srcLocationSize.Width / bC;
                tmpHeight = _srcLocationSize.Height / bC;
                tmpxs = ReadSampleDatas(_longitudeBand, 0, 0, tmpWidth, tmpHeight);
                tmpys = ReadSampleDatas(_latitudeBand, 0, 0, tmpWidth, tmpHeight);
                TryApplyGeoInterceptSlope(tmpxs, tmpys);
                _rasterProjector.Transform(SpatialReferenceFactory.CreateSpatialReference(4326), tmpxs, tmpys,
                    _dstSpatialRef);
            }

            #endregion

            for (int blockXNo = 0; blockXNo < blockXCount; blockXNo++)
            {
                for (int blockYNo = 0; blockYNo < blockYCount; blockYNo++)
                {
                    System.Diagnostics.Debug.WriteLine("分块投影处理，共{0}块,开始处理{1}块", blockXCount * blockYCount,
                        blockYNo + blockXNo * blockYCount);
                    //起始偏移，结束偏移
                    int beginX = blockWidth * blockXNo;
                    int beginY = blockHeight * blockYNo;
                    if (beginX >= outSize.Width || beginY >= outSize.Height)
                        continue;
                    if (beginX + blockWidth > outSize.Width)
                        blockWidth = outSize.Width - beginX;
                    if (beginY + blockHeight > outSize.Height)
                        blockHeight = outSize.Height - beginY;

                    //当前块的四角范围
                    double blockMinX = _dstEnvelope.MinX + beginX * _outResolutionX;
                    double blockMaxX = blockMinX + blockWidth * _outResolutionX;
                    double blockMaxY = _dstEnvelope.MaxY - beginY * _outResolutionY;
                    double blockMinY = blockMaxY - blockHeight * _outResolutionY;
                    PrjEnvelope blockEnvelope =
                        new PrjEnvelope(blockMinX, blockMaxX, blockMinY, blockMaxY, _dstSpatialRef);
                    Size curBufferSize = blockEnvelope.GetSize(bufferResolutionX, bufferResolutionY);
                    //根据当前输出块,反算出对应的源数据块(轨道)起始行列，为了减小后面需要读取的源数据大小
                    Block curOrbitblock = null;
                    //开始获取当前分块的经纬度数据集，计算轨道数据范围偏移
                    double[] srcBlockXs;
                    double[] srcBlockYs;

                    if (blockYCount == 1 && blockXCount == 1) //没分块的情况
                    {
                        #region 没分块的情况

                        curOrbitblock = _orbitBlock.Clone() as Block;
                        if (curOrbitblock.xBegin < _left)
                            curOrbitblock.xBegin = _left;
                        if (curOrbitblock.xEnd > _srcLocationSize.Width - 1 - _right)
                            curOrbitblock.xEnd = _srcLocationSize.Width - 1 - _right;
                        if (curOrbitblock.Width == _srcLocationSize.Width &&
                            curOrbitblock.Height == _srcLocationSize.Height)
                        {
                            if (_xs != null && _ys != null)
                            {
                                srcBlockXs = _xs;
                                srcBlockYs = _ys;
                            }
                            else
                            {
                                srcBlockXs = ReadBlockDatas(_longitudeBand, curOrbitblock.xBegin, curOrbitblock.yBegin,
                                    curOrbitblock.Width, curOrbitblock.Height);
                                srcBlockYs = ReadBlockDatas(_latitudeBand, curOrbitblock.xBegin, curOrbitblock.yBegin,
                                    curOrbitblock.Width, curOrbitblock.Height);

                                TryApplyGeoInterceptSlope(srcBlockXs, srcBlockYs);
                                _rasterProjector.Transform(SpatialReferenceFactory.CreateSpatialReference(4326),
                                    srcBlockXs, srcBlockYs, _dstSpatialRef);
                            }
                        }
                        else
                        {
                            if (_xs != null && _ys != null)
                            {
                                GetBlockDatas(_xs, _ys, _srcLocationSize.Width, _srcLocationSize.Height,
                                    curOrbitblock.xBegin, curOrbitblock.yBegin, curOrbitblock.Width,
                                    curOrbitblock.Height, out srcBlockXs, out srcBlockYs);
                            }
                            else
                            {
                                srcBlockXs = ReadBlockDatas(_longitudeBand, curOrbitblock.xBegin, curOrbitblock.yBegin,
                                    curOrbitblock.Width, curOrbitblock.Height);
                                srcBlockYs = ReadBlockDatas(_latitudeBand, curOrbitblock.xBegin, curOrbitblock.yBegin,
                                    curOrbitblock.Width, curOrbitblock.Height);
                                TryApplyGeoInterceptSlope(srcBlockXs, srcBlockYs);
                                _rasterProjector.Transform(SpatialReferenceFactory.CreateSpatialReference(4326),
                                    srcBlockXs, srcBlockYs, _dstSpatialRef);
                            }
                        }

                        #endregion
                    }
                    else
                    {
                        #region 分块

                        if (_xs != null && _ys != null)
                        {
                            GetEnvelope(_xs, _ys, _srcLocationSize.Width, _srcLocationSize.Height, blockEnvelope,
                                out curOrbitblock);
                        }
                        else
                        {
                            //计算偏移。
                            GetEnvelope(tmpxs, tmpys, tmpWidth, tmpHeight, blockEnvelope, out curOrbitblock);
                            curOrbitblock = curOrbitblock.Zoom(bC, bC);
                        }

                        if (curOrbitblock.Width <= 0 || curOrbitblock.Height <= 0) //当前分块不在图像内部
                        {
                            progress += _dstBandCount;
                            continue;
                        }

                        if (curOrbitblock.xBegin < _left)
                            curOrbitblock.xBegin = _left;
                        if (curOrbitblock.xEnd > _srcLocationSize.Width - 1 - _right)
                            curOrbitblock.xEnd = _srcLocationSize.Width - 1 - _right;
                        if (_xs != null && _ys != null)
                        {
                            GetBlockDatas(_xs, _ys, _srcLocationSize.Width, _srcLocationSize.Height,
                                curOrbitblock.xBegin, curOrbitblock.yBegin, curOrbitblock.Width, curOrbitblock.Height,
                                out srcBlockXs, out srcBlockYs);
                        }
                        else
                        {
                            srcBlockXs = ReadBlockDatas(_longitudeBand, curOrbitblock.xBegin, curOrbitblock.yBegin,
                                curOrbitblock.Width, curOrbitblock.Height);
                            srcBlockYs = ReadBlockDatas(_latitudeBand, curOrbitblock.xBegin, curOrbitblock.yBegin,
                                curOrbitblock.Width, curOrbitblock.Height);
                            TryApplyGeoInterceptSlope(srcBlockXs, srcBlockYs);
                            _rasterProjector.Transform(SpatialReferenceFactory.CreateSpatialReference(4326), srcBlockXs,
                                srcBlockYs, _dstSpatialRef);
                        }

                        #endregion
                    }

                    int srcBlockJdWidth = curOrbitblock.Width;
                    int srcBlockJdHeight = curOrbitblock.Height;
                    int srcBlockImgWidth = curOrbitblock.Width * imgLocationRatioX;
                    int srcBlockImgHeight = curOrbitblock.Height * imgLocationRatioY;
                    Size srcBlockLocationSize = new Size(srcBlockJdWidth, srcBlockJdHeight);
                    Size srcBlockImgSize = new Size(srcBlockImgWidth, srcBlockImgHeight);

                    //亮温订正，天顶角修正：下面获取用到的部分经纬度和太阳高度角修正系数数据,下面修改为从临时文件直接读取。
                    //最新的FY3C，250米数据，可以使用250米地理数据，但是角度数据还是1km的，这时候就需要将角度数据，读取为250米大小。
                    float[] solarZenithData = null;
                    float[] sensorZenithData = null;
                    Size srcAngleBlockSize = Size.Empty;
                    if (_isRadRef && _isSolarZenith)
                    {
                        #region 辐射定标 太阳高度角

                        if (_solarZenithCacheRaster != null) //太阳天顶角数据
                        {
                            int angle2geoRatioX = _srcLocationSize.Width / _solarZenithCacheRaster.Width;
                            int angle2geoRatioY = _srcLocationSize.Height / _solarZenithCacheRaster.Height;
                            srcAngleBlockSize = new Size(curOrbitblock.Width / angle2geoRatioX,
                                curOrbitblock.Height / angle2geoRatioY);
                            int xOffset = curOrbitblock.xBegin / angle2geoRatioX;
                            int yOffset = curOrbitblock.yBegin / angle2geoRatioY;
                            ReadBandData(out solarZenithData, _solarZenithCacheRaster, 0, xOffset, yOffset,
                                srcAngleBlockSize.Width, srcAngleBlockSize.Height);
                            //亮温临边变暗订正,读取卫星天顶角数据。
                            if (_isSensorZenith)
                                ReadBandData(out sensorZenithData, _sensorSenithBand, xOffset, yOffset,
                                    srcAngleBlockSize.Width, srcAngleBlockSize.Height);
                            //TryReadZenithData(curOrbitblock.xBegin / angle2geoRatioX, curOrbitblock.yBegin / angle2geoRatioY, srcAngleBlockSize.Width, srcAngleBlockSize.Height);
                        }
                        else
                            srcAngleBlockSize = new Size(srcBlockJdWidth, srcBlockJdHeight); //认为角度和经纬度数据一致

                        #endregion
                    }

                    //进行地形校正
                    if (_prjSettings.ExtArgs != null && _prjSettings.ExtArgs.Contains("TerrainCorrection"))
                    {
                        //SensorAzimuth;SensorZenith;SolarAzimuth;SolarZenith;DEM
                        float[] demData = null;
                        float[] sAzimuthData = null;

                        int angle2geoRatioX = _srcLocationSize.Width / _solarZenithCacheRaster.Width;
                        int angle2geoRatioY = _srcLocationSize.Height / _solarZenithCacheRaster.Height;
                        Size angleBlockSize = new Size(curOrbitblock.Width / angle2geoRatioX,
                            curOrbitblock.Height / angle2geoRatioY);
                        int xOffset = curOrbitblock.xBegin / angle2geoRatioX;
                        int yOffset = curOrbitblock.yBegin / angle2geoRatioY;
                        if (_extSrcBands2 != null && _extSrcBands2.Length == 5)
                        {
                            ReadBandForPrj(_extSrcBands2[4], xOffset, yOffset, angleBlockSize.Width,
                                angleBlockSize.Height, curOrbitblock.Size, out demData);
                            ReadBandForPrj(_extSrcBands2[0], xOffset, yOffset, angleBlockSize.Width,
                                angleBlockSize.Height, curOrbitblock.Size, out sAzimuthData);
                            ReadBandForPrj(_extSrcBands2[1], xOffset, yOffset, angleBlockSize.Width,
                                angleBlockSize.Height, curOrbitblock.Size, out sensorZenithData);
                        }

                        TerrainCorrection(srcBlockXs, srcBlockYs, sAzimuthData, sensorZenithData, demData);
                    }

                    //计算当前分块的投影查算表
                    UInt16[] dstRowLookUpTable = null;
                    UInt16[] dstColLookUpTable = null;
                    if (imgLocationRatioX == 1)
                        _rasterProjector.ComputeIndexMapTable(srcBlockXs, srcBlockYs, srcBlockImgSize, curBufferSize,
                            blockEnvelope, _maxPrjEnvelope, out dstRowLookUpTable, out dstColLookUpTable, null);
                    else
                        _rasterProjector.ComputeIndexMapTable(srcBlockXs, srcBlockYs, srcBlockLocationSize,
                            srcBlockImgSize, curBufferSize, blockEnvelope, //_maxPrjEnvelope,
                            out dstRowLookUpTable, out dstColLookUpTable, null, _sacnLineWidth);

                    //执行投影
                    UInt16[] srcBandData = null;
                    UInt16[] dstBandData = new UInt16[curBufferSize.Width * curBufferSize.Height];

                    for (int i = 0; i < _dstBandCount; i++) //读取原始通道值，投影到目标区域
                    {
                        if (progressCallback != null)
                        {
                            progress++;
                            percent = (int) (progress * 100 / progressCount);
                            progressCallback(percent, string.Format("投影完成{0}%", percent));
                        }

                        ReadImgBand(out srcBandData, i, curOrbitblock.xBegin * imgLocationRatioX,
                            curOrbitblock.yBegin * imgLocationRatioY, srcBlockImgSize.Width, srcBlockImgSize.Height);
                        //Size angleSize = new Size(srcBlockJdWidth, srcBlockJdHeight);
                        DoRadiation(srcImgRaster, i, srcBandData, solarZenithData, srcBlockImgSize, srcAngleBlockSize);
                        ushort nodata = 0;
                        if (_NODATA_VALUE.HasValue)
                            nodata = (ushort) _NODATA_VALUE;
                        _rasterProjector.Project<UInt16>(srcBandData, srcBlockImgSize, dstRowLookUpTable,
                            dstColLookUpTable, curBufferSize, dstBandData, nodata, null);
                        srcBandData = null;
                        Band band = dstImgRaster.GetRasterBand(i + beginBandIndex);

                        int blockOffsetY = blockHeight * blockYNo;
                        int blockOffsetX = blockWidth * blockXNo;
                        band.WriteRaster(blockOffsetX, blockOffsetY, blockWidth, blockHeight, dstBandData,
                            curBufferSize.Width, curBufferSize.Height, 0, 0);

                        (band as IDisposable).Dispose();
                    }

                    srcBandData = new ushort[1];
                    dstBandData = new ushort[1];
                    srcBandData = null;
                    dstBandData = null;

                    ReleaseZenithData();
                    //投影角度
                    ProjectAngle(curBufferSize, srcBlockImgSize, blockWidth, blockHeight, blockYNo, blockXNo,
                        curOrbitblock, dstRowLookUpTable, dstColLookUpTable, progressCallback);
                    //投影扩展波段
                    ProjectExtBands(curBufferSize, srcBlockImgSize, blockWidth, blockHeight, blockYNo, blockXNo,
                        curOrbitblock, dstRowLookUpTable, dstColLookUpTable, progressCallback);
                    //真彩色投影
                    ProjectTrueColor(srcImgRaster, curBufferSize, srcBlockImgSize, blockWidth, blockHeight, blockYNo,
                        blockXNo, curOrbitblock, dstRowLookUpTable, dstColLookUpTable, progressCallback);
                    dstColLookUpTable = new ushort[1];
                    dstRowLookUpTable = new ushort[1];
                    dstRowLookUpTable = null;
                    dstColLookUpTable = null;
                    solarZenithData = new float[1];
                    solarZenithData = null;

                    GC.Collect();
                    GC.WaitForFullGCComplete();
                    System.Diagnostics.Debug.WriteLine("分块投影处理，共{0}块,处理完成{1}块", blockXCount * blockYCount,
                        blockYNo + blockXNo * blockYCount);
                }
            }
        }

        #region 地形校正

        const double D2G = 3.1415926 / 180.0;
        const double er1 = 6378137;
        const double er2 = 6356752;
        const double er1s = er1 * er1;
        const double er2s = er2 * er2;

        double computeEarthRadius(double lat)
        {
            double cosb = Math.Cos(lat * D2G);
            double sinb = Math.Sin(lat * D2G);
            double p1 = er1s * cosb;
            double p2 = er2s * sinb;
            double p3 = er1 * cosb;
            double p4 = er2 * sinb;
            double R2 = (p1 * p1 + p2 * p2) / (p3 * p3 + p4 * p4);
            double R = Math.Sqrt(R2);
            return R;
        }

        private void TerrainCorrection(double[] srcBlockXs, double[] srcBlockYs, float[] sAzimuthData,
            float[] sensorZenithData, float[] demData)
        {
            if (srcBlockXs == null || srcBlockYs == null || sAzimuthData == null || sensorZenithData == null ||
                demData == null)
            {
                throw new ArgumentNullException("地形校正输入参数为空");
            }

            Parallel.For(0, srcBlockXs.Length, (index) =>
            {
                if (demData[index] < 0) demData[index] = 0;
                double earthR = computeEarthRadius(srcBlockYs[index]); //latval是纬度
                double latCircleR = Math.Cos(srcBlockYs[index] * D2G) * earthR;

                double tanViewZenith = Math.Tan(D2G * sensorZenithData[index] * 0.01);
                double tanDem = tanViewZenith * demData[index];

                double dx = Math.Sin(D2G * sAzimuthData[index] * 0.01) * tanDem;
                double dy = Math.Cos(D2G * sAzimuthData[index] * 0.01) * tanDem;

                double dlon = dx / D2G / latCircleR;
                double dlat = dy / D2G / latCircleR;

                srcBlockXs[index] += dlon;
                srcBlockYs[index] += dlat;
            });
        }

        #endregion

        protected void TryApplyGeoInterceptSlope(double[] xs, double[] ys)
        {
            if (_geoIntercept != 0d || _geoSlope != 1d)
            {
                for (int i = 0; i < xs.Length; i++)
                {
                    xs[i] = xs[i] * _geoSlope + _geoIntercept;
                }

                for (int i = 0; i < ys.Length; i++)
                {
                    ys[i] = ys[i] * _geoSlope + _geoIntercept;
                }
            }
        }


        protected virtual void TryReadZenithData(int xOffset, int yOffset, int blockWidth, int blockHeight)
        {
            return;
        }

        protected virtual void ReleaseZenithData()
        {
            return;
        }

        private void SetAoiIndex(int[] aoiIndex, ushort[] dstRowLookUpTable, ushort value)
        {
            if (aoiIndex != null && aoiIndex.Length != 0)
            {
                foreach (int aoi in dstRowLookUpTable)
                {
                    dstRowLookUpTable[aoi] = value;
                }
            }
        }

        private int[] GetAoiIndex(Size bufferSize, PrjEnvelope blockEnvelope, SpatialReference dstSpatialRef)
        {
            //
            return null;
        }

        protected abstract void DoRadiation(AbstractWarpDataset srcImgRaster, int i, ushort[] srcBandData,
            float[] solarZenithData, Size srcBlockImgSize, Size angleSize);

        /// <summary>
        /// 修改分块判断综合输出数据尺寸和经纬度数据尺寸
        /// </summary>
        /// <param name="size"></param>
        /// <param name="geoSize"></param>
        /// <param name="xScale"></param>
        /// <param name="yScale"></param>
        /// <param name="blockXNum"></param>
        /// <param name="blockYNum"></param>
        /// <param name="blockWidth"></param>
        /// <param name="blockHeight"></param>
        protected virtual void GetBlockNumber(Size size, Size geoSize, float xScale, float yScale, out int blockXNum,
            out int blockYNum, out int blockWidth, out int blockHeight)
        {
            if (size.Width <= 0)
                throw new Exception("指定的投影区域的宽度过窄:" + _dstEnvelope.Width + ",无法满足" + _outResolutionX + "分辨率输出的最小要求！宽度" +
                                    size.Width + "<1");
            if (size.Height <= 0)
                throw new Exception("指定的投影区域的高度度过窄:" + _dstEnvelope.Height + ",无法满足" + _outResolutionY +
                                    "分辨率输出的最小要求！高度" + size.Height + "<1");
            int w = size.Width;
            int h = size.Height;
            blockXNum = 1;
            blockYNum = 1;
            blockWidth = w;
            blockHeight = h;
            int MaxX = 30000;
            int MaxY = 30000;
            if (IntPtr.Size == 4)
            {
                MaxX = 5000;
                MaxY = 5000;
            }

            ulong mem = MemoryHelper.GetAvalidPhyMemory(); //系统剩余内存
            //int byteArrayCount = (2 + 2 + 2 + 2 * 2 + 2 * 8) / 2;
            //原数据一个int16，目标数据一个int16，用于订正的天顶角一个int16，查找表2*UInt16(可能还需要两个经纬度数据集)，不过如果用到，应该在之前已经申请
            //ulong maxByteArray = MemoryHelper.GetMaxArrayLength<UInt16>(byteArrayCount);

            //double canUsemem = mem > maxByteArray ? maxByteArray : mem;
            double canUsemem = mem;

            MaxY = (int) (canUsemem / w * xScale * yScale); //有些geoSize比较大，比如MERSI250M

            if (size.Width * size.Height <= MaxX * MaxY)
                return;
            while (blockWidth > MaxX)
            {
                blockXNum++;
                blockWidth = (int) Math.Floor((double) w / blockXNum);
            }

            while (blockHeight > MaxY)
            {
                blockYNum++;
                blockHeight = (int) Math.Floor((double) h / blockYNum);
            }

            System.Diagnostics.Debug.WriteLine("blockXNum,blockXNum:{0},{1}", blockXNum, blockYNum);
        }

        protected string BandNameString(int[] outBandNos)
        {
            return BandNameString(null, outBandNos);
        }

        protected string BandNameString(AbstractWarpDataset srcRaster, int[] outBandNos)
        {
            if (outBandNos == null || outBandNos.Length == 0)
                return "";
            if (srcRaster != null && srcRaster.IsBandNameRaster())
            {
                int[] bandNams = null;
                if (srcRaster.TryGetBandNameFromBandNos(outBandNos, out bandNams))
                {
                    return string.Join<int>(",", bandNams);
                }
                else
                    return string.Join(",", outBandNos);
            }
            else
            {
                return string.Join(",", outBandNos);
            }
        }

        protected virtual void TrySetLeftRightInvalidPixel(object[] extArgs)
        {
            if (extArgs != null && extArgs.Length != 0)
            {
                foreach (object extArg in extArgs)
                {
                    if (extArg is string)
                    {
                        string strExtArg = extArg as string;
                        Match match = _leftRightInvalidArgReg.Match(strExtArg);
                        if (match.Success)
                        {
                            string left = match.Groups["left"].Value;
                            string right = match.Groups["right"].Value;
                            int.TryParse(left, out _left);
                            int.TryParse(right, out _right);
                        }
                    }
                }
            }
        }

        public virtual bool HasVaildEnvelope(AbstractWarpDataset geoRaster, PrjEnvelope validEnv,
            SpatialReference dstSpatialRef)
        {
            if (geoRaster == null)
                throw new ArgumentNullException("locationRaster", "参数[经纬度数据文件]不能为空");
            //double[] xs, ys;
            //Size locationSize;
            //ReadLocations(locationRaster, out xs, out ys, out locationSize);
            //return _rasterProjector.HasVaildEnvelope(xs, ys, validEnv, null, envSpatialReference);

            double[] xs = null;
            double[] ys = null;
            Size geoSize;
            Size maxGeoSize = new Size(1024, 1024);
            Band longitudeBand = null, latitudeBand = null;
            ReadLocations(geoRaster, out longitudeBand, out latitudeBand); //GetGeoBand
            Size srcLocationSize = new Size(longitudeBand.XSize, longitudeBand.YSize);
            ReadLocations(longitudeBand, latitudeBand, maxGeoSize, out xs, out ys, out geoSize);
            return _rasterProjector.HasVaildEnvelope(xs, ys, validEnv, null, dstSpatialRef);
        }

        public bool ValidEnvelope(AbstractWarpDataset locationRaster, PrjEnvelope validEnv,
            SpatialReference envSpatialReference, out double validRate, out PrjEnvelope outEnv)
        {
            if (locationRaster == null)
                throw new ArgumentNullException("locationRaster", "参数[经纬度数据文件]不能为空");
            Size srcSize = new Size(locationRaster.Width, locationRaster.Height);
            double[] xs, ys;
            Size locationSize;
            ReadLocations(locationRaster, out xs, out ys, out locationSize);
            return _rasterProjector.VaildEnvelope(xs, ys, validEnv,
                SpatialReferenceFactory.CreateSpatialReference(4326), envSpatialReference, out validRate, out outEnv);
        }


        protected void TryResetLonlatForLeftRightInvalid(double[] longitudes, double[] latitudes, Size locationSize,
            Size geoRasterSize)
        {
            int height = locationSize.Height;
            int width = locationSize.Width;
            if (_left + _right >= width)
                return;
            double sample = locationSize.Width * 1d / geoRasterSize.Width;
            if (_left > 0)
            {
                int left = (int) ((_left - 1) * sample);
                for (int i = 0; i < left; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        longitudes[j * width + i] = double.MinValue;
                        latitudes[j * width + i] = double.MinValue;
                    }
                }
            }

            if (_right > 0)
            {
                //int right = _right - 1;
                int right = (int) ((_right - 1) * sample);
                for (int i = width - right; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        longitudes[j * width + i] = double.MinValue;
                        latitudes[j * width + i] = double.MinValue;
                    }
                }
            }
        }

        protected void TryResetLonlatForLeftRightInvalid(double[] longitudes, double[] latitudes, Size locationSize)
        {
            int height = locationSize.Height;
            int width = locationSize.Width;
            if (_left + _right >= width)
                return;
            if (_left > 0)
            {
                int left = _left - 1;
                for (int i = 0; i < left; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        longitudes[j * width + i] = double.MinValue;
                        latitudes[j * width + i] = double.MinValue;
                    }
                }
            }

            if (_right > 0)
            {
                int right = _right - 1;
                for (int i = width - right; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        longitudes[j * width + i] = double.MinValue;
                        latitudes[j * width + i] = double.MinValue;
                    }
                }
            }
        }

        protected void CheckAndCreateDir(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        #region 真彩校正

        static double DEG2RAD = 0.0174532925199;
        static double UO3 = 0.319;
        static double UH2O = 2.93;
        static double REFLMIN = -0.01;
        static double REFLMAX = 1.6;

        static double MAXSOLZ = 86.5;
        static double MAXAIRMASS = 18;
        static double SCALEHEIGHT = 8000;
        static int MAXNUMSPHALBVALUES = 3000;
        static double TAUMAX = 0.3;
        static double TAUSTEP4SPHALB = (TAUMAX / MAXNUMSPHALBVALUES);

        static double HDF_DAY = 1;
        static double HDF_NIGHT = 2;
        static double HDF_BAD = 0;
        static float[] aH2O = new float[] {0, 0, -5.60723f};
        static float[] bH2O = new float[] {0, 0, 0.820175f};
        static float[] aO3 = new float[] {0.00743232f, 0.089691f, 0.0715289f};
        static float[] taur0 = new float[] {0.19325f, 0.09536f, 0.05100f};
        static float[] sphalb0 = new float[MAXNUMSPHALBVALUES];
        static bool first_time = true;

        static float[] a = new float[]
        {
            -.57721566f, 0.99999193f, -0.24991055f,
            0.05519968f, -0.00976004f, 0.00107857f
        };

        static float[] as0 = new float[]
        {
            0.33243832f, 0.16285370f, -0.30924818f, -0.10324388f, 0.11493334f,
            -6.777104e-02f, 1.577425e-03f, -1.240906e-02f, 3.241678e-02f, -3.503695e-02f
        };

        static float[] as1 = new float[] {0.19666292f, -5.439061e-02f};
        static float[] as2 = new float[] {0.14545937f, -2.910845e-02f};

        UInt16 jac255(UInt16 val)
        {
            if (val < 30)
            {
                return Convert.ToUInt16(val * 3.667f);
            }
            else if (val < 60)
            {
                return Convert.ToUInt16(val * 1.667f + 60);
            }
            else if (val < 120)
            {
                return Convert.ToUInt16(val * 0.833f + 110);
            }
            else if (val < 190)
            {
                return Convert.ToUInt16(val * 0.4286f + 158.57);
            }
            else
            {
                return Convert.ToUInt16(Math.Min(255, val * 0.2308f + 196.15));
            }
        }

        private ushort linear255(double val)
        {
            if (val < 0) val = 0;
            return Convert.ToUInt16(Math.Min(val * 231.9f, 255));
        }

        private double correctedrefl(double refl, float TtotraytH2O, float tOG, float rhoray, float sphalb)
        {
            double corr_refl = (refl / tOG - rhoray) / TtotraytH2O;
            corr_refl /= (1 + corr_refl * sphalb);
            return corr_refl;
        }

        private unsafe void chand(float phi, float muv, float mus, double[] taur, float[] rhoray, double[] trup,
            double[] trdown)
        {
            const double xfd = 0.958725775;
            const float xbeta2 = 0.5f;
            float* pl = stackalloc float[5];
            double fs01, fs02, fs0, fs1, fs2;
            double phios, xcosf1, xcosf2, xcosf3;
            double xph1, xph2, xph3, xitm1, xitm2;
            double xlntaur, xitot1, xitot2, xitot3;
            int i, ib;

            phios = phi + 180;
            xcosf1 = 1.0f;
            xcosf2 = Math.Cos(phios * DEG2RAD);
            xcosf3 = Math.Cos(2 * phios * DEG2RAD);
            xph1 = 1 + (3 * mus * mus - 1) * (3 * muv * muv - 1) * xfd / 8.0;
            xph2 = -xfd * xbeta2 * 1.5 * mus * muv * Math.Sqrt(1 - mus * mus) * Math.Sqrt(1 - muv * muv);
            xph3 = xfd * xbeta2 * 0.375 * (1 - mus * mus) * (1 - muv * muv);
            pl[0] = 1.0f;
            pl[1] = mus + muv;
            pl[2] = mus * muv;
            pl[3] = mus * mus + muv * muv;
            pl[4] = mus * mus * muv * muv;
            fs01 = fs02 = 0;
            for (i = 0; i < 5; i++) fs01 += pl[i] * as0[i];
            for (i = 0; i < 5; i++) fs02 += pl[i] * as0[5 + i];
            for (ib = 0; ib < 3; ib++)
            {
                xlntaur = Math.Log(taur[ib]);
                fs0 = fs01 + fs02 * xlntaur;
                fs1 = as1[0] + xlntaur * as1[1];
                fs2 = as2[0] + xlntaur * as2[1];
                trdown[ib] = Math.Exp(-taur[ib] / mus);
                trup[ib] = Math.Exp(-taur[ib] / muv);
                xitm1 = (1 - trdown[ib] * trup[ib]) / 4.0 / (mus + muv);
                xitm2 = (1 - trdown[ib]) * (1 - trup[ib]);
                xitot1 = xph1 * (xitm1 + xitm2 * fs0);
                xitot2 = xph2 * (xitm1 + xitm2 * fs1);
                xitot3 = xph3 * (xitm1 + xitm2 * fs2);
                rhoray[ib] = Convert.ToSingle(xitot1 * xcosf1 + xitot2 * xcosf2 * 2 + xitot3 * xcosf3 * 2);
            }
        }

        double fintexp1(double tau)
        {
            double xx, xftau;
            int i;

            xx = a[0];
            xftau = 1.0;
            for (i = 1; i < 6; i++)
            {
                xftau *= tau;
                xx += a[i] * xftau;
            }

            return xx - Math.Log(tau);
        }

        double fintexp3(double tau)
        {
            return (Math.Exp(-tau) * (1.0 - tau) + tau * tau * fintexp1(tau)) / 2.0;
        }

        double csalbr(double tau)
        {
            return (3 * tau - fintexp3(tau) * (4 + 2 * tau) + 2 * Math.Exp(-tau)) / (4 + 3 * tau);
        }

        private int getatmvariables(float mus, float muv, float phi, float height, float[] sphalb, float[] rhoray,
            float[] TtotraytH2O, float[] tOG)
        {
            double m, Ttotrayu, Ttotrayd, tO3, tO2, tH2O, psurfratio;
            int j, ib;
            ///modis const float aH2O[Nbands]={ -5.60723, -5.25251, 0, 0, -6.29824, -7.70944, -3.91877 };
            ///modis const float bH2O[Nbands]={ 0.820175, 0.725159, 0, 0, 0.865732, 0.966947, 0.745342 };
            ///modis const float aO3[Nbands]={ 0.0715289, 0, 0.00743232, 0.089691, 0, 0, 0 };
            ///modis const float taur0[Nbands] = { 0.05100, 0.01631, 0.19325, 0.09536, 0.00366, 0.00123, 0.00043 };

            /// FY3B band1(mod3) band2(mod4) band3(mod1)
            double[] taur = new double[3];
            double[] trup = new double[3];
            double[] trdown = new double[3];

            if (first_time)
            {
                sphalb0[0] = 0;
                for (j = 1;
                    j < MAXNUMSPHALBVALUES;
                    j++) /* taur <= 0.3 for bands 1 to 7 (including safety margin for height<~0) */
                    sphalb0[j] =
                        Convert.ToSingle(
                            csalbr(j * TAUSTEP4SPHALB)); //! compute molcular spherical albedo from tau 0.0 to 0.3.
                first_time = false;
            }

            m = 1 / mus + 1 / muv;
            if (m > MAXAIRMASS) return -1;
            psurfratio = Math.Exp(-height / (float) SCALEHEIGHT);
            for (ib = 0; ib < 3; ib++)
                taur[ib] = taur0[ib] * psurfratio;

            chand(phi, muv, mus, taur, rhoray, trup, trdown);

            for (ib = 0; ib < 3; ib++)
            {
                int tauindex = (int) (taur[ib] / TAUSTEP4SPHALB + 0.5); //bug fixed for tau greater 0.3
                tauindex = Math.Min(MAXNUMSPHALBVALUES - 1, tauindex);
                sphalb[ib] = sphalb0[tauindex];
                Ttotrayu = ((2 / 3.0 + muv) + (2 / 3.0 - muv) * trup[ib]) / (4 / 3.0 + taur[ib]);
                Ttotrayd = ((2 / 3.0 + mus) + (2 / 3.0 - mus) * trdown[ib]) / (4 / 3.0 + taur[ib]);
                tO3 = tO2 = tH2O = 1;
                if (aO3[ib] != 0) tO3 = Math.Exp(-m * UO3 * aO3[ib]);
                if (bH2O[ib] != 0) tH2O = Math.Exp(-Math.Exp(aH2O[ib] + bH2O[ib] * Math.Log(m * UH2O)));
                TtotraytH2O[ib] = Convert.ToSingle(Ttotrayu * Ttotrayd * tH2O);
                tOG[ib] = Convert.ToSingle(tO3 * tO2);
            }

            return 0;
        }

        #endregion

        public static IFileProjector GetFileProjectByName(string name)
        {
            IFileProjector prj = null;
            switch (name)
            {
                case "FY3D_MERSI":
                    prj = new FY3D_MERSIFileProjector();
                    break;
                case "FY3C_VIRR":
                    prj = new FY3C_VIRRFileProjector();
                    break;
                case "FY4A":
                    prj = new FY4A_AGRIFileProjector();
                    break;
                case "FY2":
                    prj = new FY2X_VISSRFileProjector();
                    break;
                case "FY3_MERSI":
                    prj = new FY3_MERSIFileProjector();
                    break;
                case "FY3_VIRR":
                    prj = new FY3_VIRRFileProjector();
                    break;
                case "EOS":
                    prj = new EOS_FileProjector();
                    break;
                default:
                    throw new Exception(string.Format("未找到{0}投影库", name));
                    break;
            }

            return prj;
        }

        public static bool WriteMetaData(AbstractWarpDataset srcRaster, AbstractWarpDataset dstRaster,
            FilePrjSettings prjSettings)
        {
            bool ok = false;
            string originalName = Path.GetFileName(srcRaster.fileName);
            string bandStr = "";
            foreach (var item in prjSettings.OutBandNos)
            {
                bandStr += item + ",";
            }

            if (bandStr.Last() == ',')
                bandStr = bandStr.Substring(0, bandStr.Length - 1);
            Dataset outDs = dstRaster.ds;
            if (outDs != null)
            {
                outDs.SetMetadataItem("OriginalFileName", originalName, "Extend");
                outDs.SetMetadataItem("OutBandNos", bandStr, "Extend");
                ok = true;
            }

            return ok;
        }
    }
}