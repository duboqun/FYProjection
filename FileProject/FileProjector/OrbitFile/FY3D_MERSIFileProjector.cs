using PIE.DataSource;
using PIE.Meteo.RasterProject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using PIE.Meteo.Core;
using PIE.Geometry;

namespace PIE.Meteo.FileProject
{
    public class FY3D_MERSIFileProjector : FileProjector
    {
        List<TbbCoff> mTbbList = new List<TbbCoff>() {
            new TbbCoff(20, "EV_1KM_Emissive", 0, 2631.579, 2634.359, 300, 0.7130, 1.00103, -0.4759),
            new TbbCoff(21, "EV_1KM_Emissive", 1, 2469.136, 2471.654, 300, 1.2818, 1.00085, -0.3139),
            new TbbCoff(22, "EV_1KM_Emissive", 2, 1388.889, 1382.621, 270, 19.8410, 1.00125, -0.2662),
            new TbbCoff(23, "EV_1KM_Emissive", 3, 1169.591, 1168.182, 270, 37.6244, 1.00030, -0.0513),
            new TbbCoff(24, "EV_250_Aggr.1KM_Emissive", 0, 925.926, 933.364, 300, 110.8226, 1.00133, -0.0734),
            new TbbCoff(25, "EV_250_Aggr.1KM_Emissive", 1, 833.333, 836.941, 300, 127.9002, 1.00065, 0.0875),
            new TbbCoff(24, "EV_250_Emissive_b24", 0, 925.926, 933.364, 300, 110.8226, 1.00133, -0.0734),
            new TbbCoff(25, "EV_250_Emissive_b25", 0, 833.333, 836.941, 300, 127.9002, 1.00065, 0.0875),
        };

        protected const double DEG_TO_RAD_P100 = 0.000174532925199432955; // (PI/180)/100;
        //反射通道(可见光/近红外)定标，文件属性
        protected const string VIR_Cal_Coeff = "VIR_Cal_Coeff";
        protected const string SolarZenith = "SolarZenith";       //太阳天顶角数据集
        //发射通道(热红外通道)定标
        protected const double C1 = 1.191042 / 100000;
        protected const double C2 = 1.4387752;
        protected const double V = 875.1379;

        private SpatialReference _srcSpatialRef = null;
        //反射通道，19个通道的三个系数，排列为第一个通道的k0，k1，k2；第二个通道的k0，k1，k2；......
        private float[] _vir_Cal_Coeff = null;
        //太阳高度角文件
        private string _szDataFilename;
        private AbstractWarpDataset _longitudeRaster = null;
        private AbstractWarpDataset _latitudeRaster = null;
        private int _readyProgress = 0;
        private string _dataType = "1KM";              //1KM、QKM
        private FY3_MERSI_PrjSettings _prjSettings;
        //private AbstractWarpDataset _angleDataProvider = null;
        private PrjBand[] _prjBands = null;
        private AbstractWarpDataset _geoDataProvider = null;



        public FY3D_MERSIFileProjector()
            : base()
        {
            _name = "FY3C_MERSI";
            _fullname = "FY3C_MERSI轨道数据投影";
            _rasterProjector = new RasterProjector();
            _srcSpatialRef = SpatialReferenceFactory.CreateSpatialReference(4326);
            _left = 10;
            _right = 10;
            //_NODATA_VALUE = 65535;
            _supportExtBandNames = new string[] { "DEM", "LandCover", "LandSeaMask" };
        }

        public override bool IsSupport(string fileName)
        {
            return false;
        }

        public override void Project(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings, SpatialReference dstSpatialRef, Action<int, string> progressCallback)
        {
            try
            {
                ReadyArgs(srcRaster, prjSettings, dstSpatialRef, progressCallback);
                AbstractWarpDataset outwriter = null;
                try
                {
                    List<string> options = new List<string>();
                    options.Add("INTERLEAVE=BSQ");
                    options.Add("VERSION=LDF");
                    options.Add("WITHHDR=TRUE");
                    options.Add("SPATIALREF=" + _dstSpatialRef.ExportToProj4());
                    options.Add("MAPINFO={" + 1 + "," + 1 + "}:{" + _prjSettings.OutEnvelope.MinX + "," + _prjSettings.OutEnvelope.MaxY + "}:{" + _outResolutionX + "," + _outResolutionY + "}");
                    options.Add("SENSOR=MERSI");
                    if (srcRaster.DataIdentify != null)
                    {
                        string satellite = srcRaster.DataIdentify.Satellite;
                        DateTime dt = srcRaster.DataIdentify.OrbitDateTime;
                        bool asc = srcRaster.DataIdentify.IsAscOrbitDirection;
                        if (!string.IsNullOrWhiteSpace(satellite))
                        {
                            options.Add("SATELLITE=" + satellite);
                        }
                        if (dt != DateTime.MinValue && dt != DateTime.MaxValue)
                            options.Add("DATETIME=" + dt.ToString("yyyy/MM/dd HH:mm"));
                        options.Add("ORBITDIRECTION=" + (asc ? "ASC" : "DESC"));
                    }
                    List<string> op1 = new List<string>(options);
                    op1.Add("BANDNAMES=" + BandNameString(_prjSettings.OutBandNos));
                    outwriter = CreateOutFile(_outfilename, _dstBandCount, _dstSize, op1.ToArray());
                    WriteMetaData(srcRaster, outwriter, _prjSettings);
                    ReadyAngleFiles(_prjSettings.AngleFile, _outfilename, _prjSettings, _dstSize, options.ToArray());
                    ProjectToLDF(srcRaster, outwriter, 0, progressCallback);
                }
                catch (IOException ex)
                {
                    if (ex.Message == "磁盘空间不足。\r\n" && File.Exists(_outfilename))
                        File.Delete(_outfilename);
                    throw ex;
                }
                finally
                {
                    if (outwriter != null)
                    {
                        outwriter.Dispose();
                        outwriter = null;
                    }
                }
            }
            catch
            {
                EndSession();
                TryDeleteCurCatch();
                throw;
            }
            finally
            {
                if (_curSession != null)
                {
                    EndSession();
                    if (prjSettings.IsClearPrjCache)
                        TryDeleteCurCatch();
                }
            }
        }

        public override AbstractWarpDataset Project(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings, AbstractWarpDataset dstRaster, int beginBandIndex, Action<int, string> progressCallback)
        {
            if (dstRaster == null)
                return null;
            try
            {
                ReadyArgs(srcRaster, prjSettings, (dstRaster.SpatialRef), progressCallback);
                _prjSettings.OutPathAndFileName = dstRaster.fileName;
                _outfilename = dstRaster.fileName;
                _outResolutionX = dstRaster.ResolutionX;
                _outResolutionY = dstRaster.ResolutionY;
                _dstEnvelope = _prjSettings.OutEnvelope = new PrjEnvelope(
                    dstRaster.GetEnvelope().XMin, dstRaster.GetEnvelope().XMax,
                    dstRaster.GetEnvelope().YMin, dstRaster.GetEnvelope().YMax, _dstSpatialRef);
                try
                {
                    Size outSize = new Size(dstRaster.Width, dstRaster.Height);
                    //角度输出，其中的BANDNAME需要在ReadyAngleFiles()方法中获取
                    string[] angleOptions = new string[] {
                            "INTERLEAVE=BSQ",
                            "VERSION=LDF",
                            "WITHHDR=TRUE",
                            "SPATIALREF=" + _dstSpatialRef.ExportToProj4(),
                            "MAPINFO={" + 1 + "," + 1 + "}:{" + _prjSettings.OutEnvelope.MinX + "," + _prjSettings.OutEnvelope.MaxY + "}:{" + _outResolutionX + "," + _outResolutionY + "}"
                    };
                    ReadyAngleFiles(_prjSettings.AngleFile, _outfilename, _prjSettings, outSize, angleOptions);
                    ReadyExtBands(_prjSettings.AngleFile, _outfilename, _prjSettings, outSize, angleOptions);
                    ReadyExtBands2(_prjSettings.AngleFile, _outfilename, _prjSettings, outSize, angleOptions);
                    UpdateNodataValue(dstRaster);
                    ProjectToLDF(srcRaster, dstRaster, beginBandIndex, progressCallback);
                    return dstRaster;
                }
                catch (IOException ex)
                {
                    if (ex.Message == "磁盘空间不足。\r\n" && File.Exists(_outfilename))
                        File.Delete(_outfilename);
                    throw ex;
                }
                catch (Exception ex)
                {
                    MyLog.Log.Print.Error("FY3D_MERSIFileProjector.Project()", ex);
                    throw ex;
                }
                finally
                {
                    if (dstRaster != null)
                    {
                        dstRaster.Dispose();
                        dstRaster = null;
                    }
                }
            }
            catch (Exception ex)
            {
                EndSession();
                TryDeleteCurCatch();
                throw ex;
            }
            finally
            {
                if (_curSession == null)
                {
                    EndSession();
                    if (prjSettings.IsClearPrjCache)
                        TryDeleteCurCatch();
                }
            }
        }

        private void ReadyArgs(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings, SpatialReference dstSpatialRef, Action<int, string> progressCallback)
        {
            float resolutionScale = 1f;
            _readyProgress = 0;
            if (progressCallback != null)
                progressCallback(_readyProgress++, "准备相关参数");
            _prjSettings = ArgsCheck(srcRaster, prjSettings);
            _geoDataProvider = _prjSettings.GeoFile;
            CheckIs0250(srcRaster);
            _dstSpatialRef = dstSpatialRef;
            switch (_dataType)
            {
                case "1KM":
                    //整轨投影时候去除左右锯齿，分块投影不需要
                    _left = 10;
                    _right = 10;
                    if (_prjSettings.OutEnvelope == null || _prjSettings.OutEnvelope == PrjEnvelope.Empty)//整轨投影时做限制
                    {
                        MemoryHelper.MemoryNeed(500, 1536);
                    }
                    else
                    {
                        MemoryHelper.MemoryNeed(400, 1536);      //剩余900MB,已使用1.2GB
                    }
                    //_angleDataProvider = srcRaster;       
                    TryCreateDefaultArgs(srcRaster, _prjSettings, ref _dstSpatialRef);
                    TrySetLeftRightInvalidPixel(_prjSettings.ExtArgs);
                    DoSession(srcRaster, _geoDataProvider, _dstSpatialRef, _prjSettings, progressCallback);
                    break;
                case "QKM":
                    resolutionScale = 4f;
                    _left = 20;
                    _right = 20;
                    if (_prjSettings.OutEnvelope == null || _prjSettings.OutEnvelope == PrjEnvelope.Empty)
                    {
                        MemoryHelper.MemoryNeed(800, 1280);     //整幅投影对内存做限制，系统剩余内存不低于A参数MB，应用程序已使用内存不超过B参数MB
                    }
                    else
                    {
                        MemoryHelper.MemoryNeed(600, 1280);     //剩余900MB,最大已使用1.2GB
                    }
                    //_angleDataProvider = _prjSettings.SecondaryOrbitRaster;
                    TryCreate0250DefaultArgs(srcRaster, _prjSettings, ref _dstSpatialRef);
                    TrySetLeftRightInvalidPixel(_prjSettings.ExtArgs);
                    DoSession(srcRaster, _geoDataProvider, _dstSpatialRef, _prjSettings, progressCallback);
                    break;
                default:
                    break;
            }
            if (_prjSettings.OutEnvelope == null || _prjSettings.OutEnvelope == PrjEnvelope.Empty)
            {
                _prjSettings.OutEnvelope = _maxPrjEnvelope;
                _orbitBlock = new Block { xBegin = 0, yBegin = 0, xEnd = _srcLocationSize.Width - 1, yEnd = _srcLocationSize.Height - 1 };
            }
            else
            {
                if (_xs != null && _ys != null)
                {
                    GetEnvelope(_xs, _ys, _srcLocationSize.Width, _srcLocationSize.Height, _prjSettings.OutEnvelope, out _orbitBlock);
                }
                else
                {
                    int bC = 8;
                    int tmpWidth = 0;
                    int tmpHeight = 0;
                    double[] tmpxs = null;
                    double[] tmpys = null;
                    //8024，8000
                    tmpWidth = _srcLocationSize.Width / bC;
                    tmpHeight = _srcLocationSize.Height / bC;
                    tmpxs = ReadSampleDatas(_longitudeBand, 0, 0, tmpWidth, tmpHeight);
                    tmpys = ReadSampleDatas(_latitudeBand, 0, 0, tmpWidth, tmpHeight);
                    _rasterProjector.Transform(SpatialReferenceFactory.CreateSpatialReference(4326), tmpxs, tmpys, _dstSpatialRef);
                    //计算偏移
                    GetEnvelope(tmpxs, tmpys, tmpWidth, tmpHeight, _prjSettings.OutEnvelope, out _orbitBlock);
                    _orbitBlock = _orbitBlock.Zoom(bC, bC);
                    tmpxs = new double[1];
                    tmpys = new double[1];
                }
                if (_orbitBlock == null || _orbitBlock.Width <= 0 || _orbitBlock.Height <= 0)
                    throw new Exception("数据不在目标区间内");
                float invalidPresent = (_orbitBlock.Width * _orbitBlock.Height * resolutionScale) / (_srcLocationSize.Width * _srcLocationSize.Height);
                if (invalidPresent < 0.0001f)
                    throw new Exception("数据占轨道数据比例太小,有效率" + invalidPresent * 100 + "%");
                if (invalidPresent > 0.60)
                    _orbitBlock = new Block { xBegin = 0, yBegin = 0, xEnd = _srcLocationSize.Width - 1, yEnd = _srcLocationSize.Height - 1 };
            }
            //地理坐标投影,下面简单的对地理坐标投影的范围作了限制，不大严谨，仅适合目前的状况。
            //if (_dstSpatialRef.Type == SpatialReferenceType.GeographicCS && (prjSettings.OutEnvelope.MaxY > 80 || prjSettings.OutEnvelope.MaxY < -80))
            //{
            //    throw new Exception(string.Format("高纬度数据，不适合投影为等经纬度数据[{0}]", _maxPrjEnvelope));
            //}
            //以下参数用于投影

            _dstEnvelope = _prjSettings.OutEnvelope;
            if (!_dstEnvelope.IntersectsWith(_maxPrjEnvelope))
                throw new Exception("数据不在目标区间内");
            _outResolutionX = _prjSettings.OutResolutionX;
            _outResolutionY = _prjSettings.OutResolutionY;
            _outFormat = _prjSettings.OutFormat;
            _outfilename = _prjSettings.OutPathAndFileName;
            _dstProj4 = _dstSpatialRef.ExportToProj4();
            _dstBandCount = _prjBands.Length;
            _dstSize = _prjSettings.OutSize;
            _isRadRef = _prjSettings.IsRadRef;
            _isSolarZenith = _prjSettings.IsSolarZenith;
            _isSensorZenith = _prjSettings.IsSensorZenith;
            _isRad = _prjSettings.IsRad;
        }

        private static FY3_MERSI_PrjSettings ArgsCheck(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings)
        {
            if (srcRaster == null)
                throw new ArgumentNullException("srcRaster");
            if (prjSettings == null)
                throw new ArgumentNullException("prjSettings");
            FY3_MERSI_PrjSettings fy3prjSettings = prjSettings as FY3_MERSI_PrjSettings;
            if (fy3prjSettings.GeoFile == null)
                throw new ArgumentNullException("地理坐标文件未设置(GEO)");
            return fy3prjSettings;
        }

        private void CheckIs0250(AbstractWarpDataset srcRaster)
        {
            try
            {
                Dictionary<string, string> filaAttrs = srcRaster.GetAttributes();
                if (filaAttrs == null || !filaAttrs.ContainsKey("File Alias Name"))
                    throw new Exception("不能确认为合法的MERSI轨道数据，尝试获取文件属性File Alias Name的值为空");
                string fileAliasName = filaAttrs["File Alias Name"];
                if (string.IsNullOrWhiteSpace(fileAliasName))
                    throw new Exception("不能确认为合法的MERSI轨道数据，尝试获取文件属性File Alias Name的值为空");
                else if (fileAliasName == "MERSI_1KM_L1")
                    _dataType = "1KM";
                else if (fileAliasName == "MERSI_QKM_L1" || fileAliasName == "MERSI_250M_L1")
                    _dataType = "QKM";
                else
                    throw new Exception("不能确认为合法的MERSI轨道数据，文件属性File Alias Name的值为[" + fileAliasName + "]支持的是MERSI_1KM_L1或者MERSI_QKM_L1");
            }
            catch (Exception ex)
            {
                throw new Exception("不能确认为合法的MERSI轨道数据" + ex.Message, ex.InnerException);
            }
        }

        private void TryCreate0250DefaultArgs(AbstractWarpDataset srcRaster, FY3_MERSI_PrjSettings prjSettings, ref SpatialReference dstSpatialRef)
        {
            if (prjSettings.SecondaryOrbitRaster == null && prjSettings.IsSolarZenith)
                prjSettings.IsSolarZenith = false;  //throw new Exception("无法获取相应1KM轨道数据文件，无法做太阳天顶角订正");
            if (dstSpatialRef == null)
                dstSpatialRef = _srcSpatialRef;
            if (string.IsNullOrWhiteSpace(prjSettings.OutFormat))
                prjSettings.OutFormat = "LDF";
            if (dstSpatialRef.Type == SpatialReferenceType.GeographicCS)
                _srcImgResolution = 0.0025f;
            else
                _srcImgResolution = 250f;
            if (prjSettings.OutResolutionX == 0 || prjSettings.OutResolutionY == 0)
            {
                if (dstSpatialRef.Type == SpatialReferenceType.GeographicCS)
                {
                    prjSettings.OutResolutionX = 0.0025F;//地理坐标系
                    prjSettings.OutResolutionY = 0.0025F;
                }
                else
                {
                    prjSettings.OutResolutionX = 250F;//投影坐标系
                    prjSettings.OutResolutionY = 250F;
                }
            }
            if (prjSettings.OutBandNos == null || prjSettings.OutBandNos.Length == 0)
            {
                _prjBands = BandDefCollection.MERSI2_0250_OrbitDefCollecges();
            }
            else
            {
                List<PrjBand> bands = new List<PrjBand>();
                PrjBand[] defbands = BandDefCollection.MERSI2_0250_OrbitDefCollecges();
                foreach (int bandNo in prjSettings.OutBandNos)
                {
                    bands.Add(defbands[bandNo - 1]);
                }
                _prjBands = bands.ToArray();
            }
        }

        private void TryCreateDefaultArgs(AbstractWarpDataset srcRaster, FY3_MERSI_PrjSettings prjSettings, ref SpatialReference dstSpatialRef)
        {
            if (dstSpatialRef == null)
                dstSpatialRef = _srcSpatialRef;
            if (string.IsNullOrWhiteSpace(prjSettings.OutFormat))
                prjSettings.OutFormat = "LDF";
            if (dstSpatialRef.Type == SpatialReferenceType.GeographicCS)
            {
                _srcImgResolution = 0.01F;
            }
            else
            {
                _srcImgResolution = 1000F;
            }
            if (prjSettings.OutResolutionX == 0 || prjSettings.OutResolutionY == 0)
            {
                if (dstSpatialRef.Type == SpatialReferenceType.GeographicCS)
                {
                    prjSettings.OutResolutionX = 0.01F;
                    prjSettings.OutResolutionY = 0.01F;
                }
                else
                {
                    prjSettings.OutResolutionX = 1000F;
                    prjSettings.OutResolutionY = 1000F;
                }
            }
            if (prjSettings.OutBandNos == null || prjSettings.OutBandNos.Length == 0)
            {
                _prjBands = BandDefCollection.MERSI2_1000_OrbitDefCollecges();
            }
            else
            {
                List<PrjBand> bands = new List<PrjBand>();
                PrjBand[] defbands = BandDefCollection.MERSI2_1000_OrbitDefCollecges();
                foreach (int bandNo in prjSettings.OutBandNos)
                {
                    bands.Add(defbands[bandNo - 1]);
                }
                _prjBands = bands.ToArray();
            }
        }

        private void DoSession(AbstractWarpDataset srcRaster, AbstractWarpDataset geoRaster, SpatialReference dstSpatialRef, FY3_MERSI_PrjSettings prjSettings, Action<int, string> progressCallback)
        {
            if (_curSession == null || _curSession != srcRaster || _isBeginSession)
            {
                Size srcImgSize = new Size(srcRaster.Width, srcRaster.Height);
                ReadyLocations(geoRaster, dstSpatialRef, prjSettings, out _srcLocationSize, out _maxPrjEnvelope, progressCallback);
                if (progressCallback != null)
                    progressCallback(4, "准备其他参数");
                if (prjSettings.IsRadRef)
                    ReadyRadiationArgs(srcRaster);
                if (prjSettings.IsSolarZenith) //&& prjSettings.IsRadiation
                {
                    _szDataFilename = GetSolarZenithCacheFilename(geoRaster.fileName);
                    if (!File.Exists(_szDataFilename))
                        ReadySolarZenithArgsToFile(prjSettings.AngleFile);
                    else
                    {
                        _solarZenithCacheRaster = WarpDataset.Open(_szDataFilename);
                    }

                    if (prjSettings.IsSensorZenith)
                    {
                        ReadySensorZenith(prjSettings.AngleFile);
                    }
                }
                _rasterDataBands = TryCreateRasterDataBands(srcRaster, prjSettings, progressCallback);//待投影的波段
                _isBeginSession = false;
            }
        }

        private void ReadySensorZenith(AbstractWarpDataset srcRaster)
        {
            _sensorSenithRaster = srcRaster;
            IRasterBand[] bands = srcRaster.GetBands("SensorZenith");
            if (bands != null || bands.Length != 1)
                _sensorSenithBand = bands[0];
        }

        public override void EndSession()
        {
            base.EndSession();
            //_cache.Clear();
            _xs = null;
            _ys = null;
            _vir_Cal_Coeff = null;
            if (_solarZenithCacheRaster != null)
            {
                _solarZenithCacheRaster.Dispose();
                _solarZenithCacheRaster = null;
            }
            if (_longitudeBand != null)
            {
                (_longitudeBand as IDisposable).Dispose();
                _longitudeBand = null;
            }
            if (_latitudeBand != null)
            {
                (_latitudeBand as IDisposable).Dispose();
                _latitudeBand = null;
            }
            if (_latitudeRaster != null)
            {
                _latitudeRaster.Dispose();
                _latitudeRaster = null;
            }
            if (_longitudeRaster != null)
            {
                _longitudeRaster.Dispose();
                _longitudeRaster = null;
            }
        }

        private IRasterBand[] TryCreateRasterDataBands(AbstractWarpDataset srcRaster, FY3_MERSI_PrjSettings fy3prjSettings, Action<int, string> progressCallback)
        {
            List<IRasterBand> rasterBands = new List<IRasterBand>();
            for (int i = 0; i < _prjBands.Length; i++)
            {
                if (progressCallback != null)
                    progressCallback(_readyProgress++, $"准备第{i + 1}个输入数据通道");
                PrjBand bandMap = _prjBands[i];
                IRasterBand[] latBands = srcRaster.GetBands(bandMap.DataSetName);
                IRasterBand band = latBands[bandMap.DataSetIndex];
                rasterBands.Add(band);
            }
            return rasterBands.ToArray();
        }

        /// <summary>
        /// 这里修改为获取经纬度IRasterBand，而不是存储到_xs,_ys中
        /// 因为对于MERSI250米分辨率的经纬度数据集数据量太大了，无法一次放入内存中。
        /// </summary>
        /// <param name="geoRaster"></param>
        /// <param name="dstSpatialRef"></param>
        /// <param name="fy3prjSettings"></param>
        /// <param name="srcLocationSize"></param>
        /// <param name="maxPrjEnvelope"></param>
        /// <param name="progressCallback"></param>
        private void ReadyLocations(AbstractWarpDataset geoRaster, SpatialReference dstSpatialRef, FY3_MERSI_PrjSettings fy3prjSettings,
            out Size srcLocationSize, out PrjEnvelope maxPrjEnvelope, Action<int, string> progressCallback)
        {
            if (progressCallback != null)
                progressCallback(_readyProgress++, "读取经纬度数据集");

            double[] xs = null;
            double[] ys = null;
            Size geoSize;
            Size maxGeoSize = new Size(1024, 1024);//采样读取后的最大Size
            ReadLocations(geoRaster, out _longitudeBand, out _latitudeBand);//GetGeoBand
            srcLocationSize = new Size(_longitudeBand.GetXSize(), _longitudeBand.GetYSize());
            ReadLocations(_longitudeBand, _latitudeBand, maxGeoSize, out xs, out ys, out geoSize);
            TryResetLonlatForLeftRightInvalid(xs, ys, geoSize, srcLocationSize);
            _rasterProjector.ComputeDstEnvelope(_srcSpatialRef, xs, ys, geoSize, dstSpatialRef, out maxPrjEnvelope, progressCallback);
            xs = new double[1];
            ys = new double[1];
            GC.Collect();
        }

        protected override void ReadLocations(AbstractWarpDataset geoRaster, out double[] xs, out double[] ys, out Size locationSize)
        {
            xs = null;
            ys = null;
            locationSize = Size.Empty;
        }

        protected override void ReadLocations(AbstractWarpDataset geoRaster, out IRasterBand longitudeBand, out IRasterBand latitudeBand)
        {
            IRasterBand[] lonsBands = geoRaster.GetBands("Longitude");
            IRasterBand[] latBands = geoRaster.GetBands("Latitude");
            if (lonsBands == null || latBands == null || lonsBands.Length == 0 || latBands.Length == 0 || lonsBands[0] == null || latBands[0] == null)
                throw new Exception("获取经纬度数据集失败");
            longitudeBand = lonsBands[0];
            latitudeBand = latBands[0];
        }



        /// <summary>
        /// 获取转换参数
        /// </summary>
        /// <param name="srcImgRaster"></param>
        /// <param name="dsName"></param>
        private void ReadDnIS(AbstractWarpDataset srcImgRaster, string dsName)
        {
            if (srcImgRaster == null)
                throw new ArgumentNullException("srcRaster", "获取亮温转换参数失败：参数srcRaster为空");
            if (dsName == null)
                throw new ArgumentNullException("dataSetName", "获取亮温转换参数失败：参数srcRaster为空");
            try
            {
                int count = srcImgRaster.GetBands(dsName).Length;
                _dsIntercept = ReadDataSetAttrToFloat(srcImgRaster, dsName, "Intercept", count);
                _dsSlope = ReadDataSetAttrToFloat(srcImgRaster, dsName, "Slope", count);
            }
            catch (Exception ex)
            {
                throw new Exception("获取亮温转换参数失败:" + ex.Message, ex.InnerException);
            }
        }

        private float[] ReadDataSetAttrToFloat(AbstractWarpDataset srcbandpro, string dataSetName, string attrName, int length)
        {
            float[] value = new float[length];
            Dictionary<string, string> dsAtts = srcbandpro.GetDatasetAttributes(dataSetName);
            string refSbCalStr = dsAtts[attrName];
            string[] refSbCals = refSbCalStr.Split(',');
            if (refSbCals.Length >= length)
            {
                for (int i = 0; i < length; i++)
                {
                    value[i] = float.Parse(refSbCals[i]);
                }
                return value;
            }
            else
                return null;
        }

        protected short[] _sensorZenithData = null;
        protected override void TryReadZenithData(int xOffset, int yOffset, int blockWidth, int blockHeight)
        {
            //亮温临边变暗订正,读取卫星天顶角数据。
            if (_isSensorZenith && _sensorSenithBand != null)
            {
                _sensorZenithData = ReadBandData(_sensorSenithBand, xOffset, yOffset, blockWidth, blockHeight);
            }
        }

        protected override void ReleaseZenithData()
        {
            _sensorZenithData = null;
        }

        #region 辐射定标
        //执行辐射度亮度转换
        private void DoRadiation(AbstractWarpDataset srcImgRaster, ushort[] srcBlockBandData, Size srcBlockDataSize, string dsName, int dsIndex, float[] solarZenithData, Size srcLocationSize)
        {
            if (!_prjSettings.IsRadRef)
                return;
            bool isSolarZenith = _prjSettings.IsSolarZenith;

            switch (dsName)
            {
                case "EV_250_Aggr.1KM_RefSB"://定标系数为VIS_Cal_Coeff 中的前四组,k0，k1，k2
                    {
                        float k0 = _vir_Cal_Coeff[dsIndex * 3];
                        float k1 = _vir_Cal_Coeff[dsIndex * 3 + 1];
                        float k2 = _vir_Cal_Coeff[dsIndex * 3 + 2];

                        ReadDnIS(srcImgRaster, dsName);
                        float intercept = _dsIntercept[dsIndex];
                        float slope = _dsSlope[dsIndex];

                        RefSBRadiation(srcBlockBandData, k0, k1, k2, intercept, slope, solarZenithData, isSolarZenith);
                    }
                    break;
                case "EV_1KM_RefSB":        //定标系数为VIS_Cal_Coeff 中的后15 组
                    {
                        float k0 = _vir_Cal_Coeff[12 + dsIndex * 3];
                        float k1 = _vir_Cal_Coeff[12 + dsIndex * 3 + 1];
                        float k2 = _vir_Cal_Coeff[12 + dsIndex * 3 + 2];

                        ReadDnIS(srcImgRaster, dsName);
                        float intercept = _dsIntercept[dsIndex];
                        float slope = _dsSlope[dsIndex];

                        RefSBRadiation(srcBlockBandData, k0, k1, k2, intercept, slope, solarZenithData, isSolarZenith);
                    }
                    break;
                case "EV_250_RefSB_b1"://_vir_Cal_Coeff
                    {
                        float k0 = _vir_Cal_Coeff[dsIndex * 0];
                        float k1 = _vir_Cal_Coeff[dsIndex * 0 + 1];
                        float k2 = _vir_Cal_Coeff[dsIndex * 0 + 2];

                        ReadDnIS(srcImgRaster, dsName);
                        float intercept = _dsIntercept[dsIndex];
                        float slope = _dsSlope[dsIndex];


                        RefSBRadiation(srcBlockBandData, k0, k1, k2, intercept, slope, solarZenithData, isSolarZenith, srcBlockDataSize, srcLocationSize);
                    }
                    break;
                case "EV_250_RefSB_b2":
                    {
                        float k0 = _vir_Cal_Coeff[dsIndex * 1];
                        float k1 = _vir_Cal_Coeff[dsIndex * 1 + 1];
                        float k2 = _vir_Cal_Coeff[dsIndex * 1 + 2];

                        ReadDnIS(srcImgRaster, dsName);
                        float intercept = _dsIntercept[dsIndex];
                        float slope = _dsSlope[dsIndex];

                        RefSBRadiation(srcBlockBandData, k0, k1, k2, intercept, slope, solarZenithData, isSolarZenith, srcBlockDataSize, srcLocationSize);
                    }
                    break;
                case "EV_250_RefSB_b3":
                    {
                        float k0 = _vir_Cal_Coeff[dsIndex * 2];
                        float k1 = _vir_Cal_Coeff[dsIndex * 2 + 1];
                        float k2 = _vir_Cal_Coeff[dsIndex * 2 + 2];

                        ReadDnIS(srcImgRaster, dsName);
                        float intercept = _dsIntercept[dsIndex];
                        float slope = _dsSlope[dsIndex];

                        RefSBRadiation(srcBlockBandData, k0, k1, k2, intercept, slope, solarZenithData, isSolarZenith, srcBlockDataSize, srcLocationSize);
                    }
                    break;
                case "EV_250_RefSB_b4":
                    {
                        float k0 = _vir_Cal_Coeff[dsIndex * 3];
                        float k1 = _vir_Cal_Coeff[dsIndex * 3 + 1];
                        float k2 = _vir_Cal_Coeff[dsIndex * 3 + 2];

                        ReadDnIS(srcImgRaster, dsName);
                        float intercept = _dsIntercept[dsIndex];
                        float slope = _dsSlope[dsIndex];

                        RefSBRadiation(srcBlockBandData, k0, k1, k2, intercept, slope, solarZenithData, isSolarZenith, srcBlockDataSize, srcLocationSize);
                    }
                    break;
                case "EV_1KM_Emissive":
                case "EV_250_Aggr.1KM_Emissive":    //0~4095
                case "EV_250_Emissive_b24":
                case "EV_250_Emissive_b25":
                    {   //亮温=10*(c2*v/log(1+c1*v*v*v/(观测值/100.0)))
                        ReadDnIS(srcImgRaster, dsName);
                        float intercept = _dsIntercept[dsIndex];
                        float slope = _dsSlope[dsIndex];
                        //fy3d增加了亮温订正
                        RadiationEmissive3D(srcBlockBandData, dsName, dsIndex, intercept, slope);

                        //RadiationEmissive(srcBlockBandData, c2v, c1v3);
                    }
                    break;
                default:
                    break;
            }
        }

        private float[] _dsIntercept;
        private float[] _dsSlope;
        protected override void DoRadiation(AbstractWarpDataset srcImgRaster, int i, ushort[] srcBandData, float[] solarZenithData, Size srcBlockImgSize, Size angleSize)
        {
            if (_isRadRef)
            {
                string ds = _prjBands[i].DataSetName;
                int dsIndex = _prjBands[i].DataSetIndex;
                DoRadiation(srcImgRaster, srcBandData, srcBlockImgSize, ds, dsIndex, solarZenithData, angleSize);
            }
            if (_isRad)
            {
                string dsName = _prjBands[i].DataSetName;
                int dsIndex = _prjBands[i].DataSetIndex;
                ReadDnIS(srcImgRaster, dsName);
                float intercept = _dsIntercept[dsIndex];
                float slope = _dsSlope[dsIndex];

                Parallel.For(0, srcBandData.Length, (index) =>
                {
                    ushort rad16 = 0;
                    double rad = slope * (srcBandData[index] - intercept);
                    if (rad < ushort.MaxValue)
                        rad16 = Convert.ToUInt16(rad);
                    srcBandData[index] = rad16;
                });
            }
        }
        private void RadiationEmissive3D(ushort[] bandData, string dsName, int dsIndex, float intercept, float slope)
        {
            TbbCoff modeltbbcoff = mTbbList.FirstOrDefault(o => o.DataSetName == dsName && o.BandIndex == dsIndex);

            double c2v = C2 * modeltbbcoff.Mersi_EquivMidWn;
            double c1v3 = C1 * modeltbbcoff.Mersi_EquivMidWn * modeltbbcoff.Mersi_EquivMidWn * modeltbbcoff.Mersi_EquivMidWn;

            Parallel.For(0, bandData.Length, (index) =>
            {
                double temperatureBB;
                double sensorZenith;
                double deltaT;
                float dn = slope * (bandData[index] - intercept);   //dn值（原始观测值）调整
                temperatureBB = (c2v / Math.Log(1 + c1v3 / dn));
                temperatureBB = temperatureBB * modeltbbcoff.TbbCorr_Coeff_A + modeltbbcoff.TbbCorr_Coeff_B;
                //添加"临边变暗订正"
                if (_isSensorZenith && _sensorZenithData != null)
                {
                    sensorZenith = _sensorZenithData[index] * 0.01d;//原始数据放大了100倍
                    deltaT = temperatureBB + (Math.Pow(Math.E, 0.00012 * sensorZenith * sensorZenith) - 1) * (0.1072 * temperatureBB - 26.81);
                    bandData[index] = (UInt16)(deltaT * 10);
                }
                else
                    bandData[index] = (UInt16)(temperatureBB * 10);
                if (bandData[index] > 6500)
                    bandData[index] = 0;
            });
        }

        //MERSI 0250M 反射通道定标.高度角数据和数据尺寸不一致,
        private void RefSBRadiation(ushort[] bandData, float k0, float k1, float k2, float intercept, float slope, float[] solarZenithData, bool isSolarZenith, Size dataSize, Size angleSize)
        {
            int height = dataSize.Height;
            int width = dataSize.Width;
            if (isSolarZenith)
            {
                float scoreX = (float)dataSize.Width / angleSize.Width;
                float scoreY = (float)dataSize.Height / angleSize.Height;
                Parallel.For(0, dataSize.Height, (row) =>
                {
                    int rOffset = row * width;
                    for (int col = 0; col < dataSize.Width; col++)
                    {
                        int index = rOffset + col;
                        float dn = slope * (bandData[index] - intercept);   //dn值（原始观测值）调整
                        double radiation = k0 + k1 * dn + k2 * dn * dn;//(定标)计算反射率,理论上讲反射率应当是0-100
                        int szCol = (int)(col / scoreX);
                        int szRow = (int)(row / scoreY);
                        double solarZenith = solarZenithData[szRow * angleSize.Width + szCol];//;

                        bandData[index] = (UInt16)(radiation * solarZenith);
                        if (bandData[index] > 65000)
                            bandData[index] = 0;
                    }
                });
            }
            else
            {
                Parallel.For(0, dataSize.Height, (row) =>
                {
                    for (int col = 0; col < dataSize.Width; col++)
                    {
                        int index = row * width + col;
                        float dn = slope * (bandData[index] - intercept);   //dn值（原始观测值）调整
                        double radiation = k0 + k1 * dn + k2 * dn * dn;//(定标)计算反射率,理论上讲反射率应当是0-100
                        bandData[index] = (UInt16)(10 * radiation);//放大到0-1000
                        if (bandData[index] > 65000)
                            bandData[index] = 0;
                    }
                });
            }
        }

        //可见光反射率计算,角度信息和数据尺寸一致。
        private void RefSBRadiation(ushort[] srcBandData, float k0, float k1, float k2, float intercept, float slope, float[] solarZenithData, bool isSolarZenith)
        {
            if (isSolarZenith)//天顶角订正
            {
                Parallel.For(0, srcBandData.Length, (index) =>
                {
                    float bandData = slope * (srcBandData[index] - intercept);   //dn值（原始观测值）调整回恢复
                    double radiation = k0 + k1 * bandData + k2 * bandData * bandData;
                    double solarZenith = solarZenithData[index];

                    srcBandData[index] = (UInt16)(radiation * solarZenith);
                    if (srcBandData[index] > 65000)
                        srcBandData[index] = 0;
                });
            }
            else
            {
                Parallel.For(0, srcBandData.Length, (index) =>
                {
                    float bandData = slope * (srcBandData[index] - intercept);   //dn值（原始观测值）调整回恢复
                    double radiation = k0 + k1 * bandData + k2 * bandData * bandData;
                    srcBandData[index] = (UInt16)(10 * radiation);      //放大到0-1000
                    if (srcBandData[index] > 65000)
                        srcBandData[index] = 0;
                });
            }
        }
        #endregion

        private void ReadySolarZenithArgsToFile(AbstractWarpDataset srcRaster)
        {

            if (srcRaster == null)
                throw new ArgumentNullException("srcRaster", "获取太阳天顶角数据失败");
            try
            {
                Size srcSize = new System.Drawing.Size(srcRaster.Width, srcRaster.Height);
                short[] readSolarZenithData = ReadDataSetToInt16(srcRaster, srcSize, SolarZenith, 0);
                int length = srcRaster.Width * srcRaster.Height;
                float[] saveSolarZenithData = new float[length];
                Parallel.For(0, length, index =>
                {
                    if (readSolarZenithData[index] > 0 && readSolarZenithData[index] < 18000)
                        saveSolarZenithData[index] = (float)(10.0f / Math.Cos(readSolarZenithData[index] * DEG_TO_RAD_P100));
                    else
                        saveSolarZenithData[index] = 0;
                });
                _solarZenithCacheRaster = WriteData(saveSolarZenithData, _szDataFilename, srcSize.Width, srcSize.Height);
                saveSolarZenithData = null;
                readSolarZenithData = null;

            }
            catch (Exception ex)
            {
                throw new Exception("获取太阳天顶角数据失败", ex.InnerException);
            }
        }

        private void ReadyRadiationArgs(AbstractWarpDataset srcRaster)
        {
            if (srcRaster == null)
                throw new ArgumentNullException("srcRaster", "获取亮温转换参数失败：参数srcRaster为空");
            try
            {
                //这里和之前FY3B的数据有区别，FY3C的此参数存在数据集VIS_Cal_Coeff中，19*3。band_name = 1-4, 6-20
                //_vir_Cal_Coeff = ReadFileAttributeToFloat(srcbandpro, VIR_Cal_Coeff, 57);
                Size visSize = new Size(3, 19);
                _vir_Cal_Coeff = ReadDataSetToSingle(srcRaster, visSize, "Calibration/VIS_Cal_Coeff", 0);
            }
            catch (Exception ex)
            {
                throw new Exception("获取亮温转换参数失败:" + ex.Message, ex.InnerException);
            }
        }

        private Int16[] ReadDataSetToInt16(AbstractWarpDataset srcbandpro, Size srcSize, string dataSetName, int bandIndex)
        {
            Int16[] data = new Int16[srcSize.Width * srcSize.Height];
            IRasterBand[] rasterBands = srcbandpro.GetBands(dataSetName);
            IRasterBand rasterBand = rasterBands[0];
            {
                unsafe
                {
                    fixed (Int16* ptr = data)
                    {
                        IntPtr bufferPtr = new IntPtr(ptr);
                        rasterBand.Read(0, 0, srcSize.Width, srcSize.Height, bufferPtr, srcSize.Width, srcSize.Height, PixelDataType.Int16);
                    }
                }
            }
            return data;
        }

        private float[] ReadDataSetToSingle(AbstractWarpDataset srcbandpro, Size srcSize, string dataSetName, int bandIndex)
        {
            Single[] data = new Single[srcSize.Width * srcSize.Height];
            IRasterBand[] rasterBands = srcbandpro.GetBands(dataSetName);
            IRasterBand rasterBand = rasterBands[0];
            {
                rasterBand.Read(0, 0, srcSize.Width, srcSize.Height, data, srcSize.Width, srcSize.Height, PixelDataType.Float32);
            }
            return data;
        }

        public override FilePrjSettings CreateDefaultPrjSettings()
        {
            return new FY3_VIRR_PrjSettings();
        }

        public override void ComputeDstEnvelope(AbstractWarpDataset geoRaster, SpatialReference dstSpatialRef, out PrjEnvelope maxPrjEnvelope, Action<int, string> progressCallback)
        {
            if (geoRaster != null)
            {
                double[] xs = null;
                double[] ys = null;
                Size geoSize;
                Size maxGeoSize = new Size(1024, 1024);//采样读取后的最大Size
                IRasterBand longitudeBand = null;
                IRasterBand latitudeBand = null;
                ReadLocations(geoRaster, out longitudeBand, out latitudeBand);//GetGeoBand
                ReadLocations(longitudeBand, latitudeBand, maxGeoSize, out xs, out ys, out geoSize);
                Size geoRasterSize = new Size(longitudeBand.GetXSize(), longitudeBand.GetYSize());
                TryResetLonlatForLeftRightInvalid(xs, ys, geoSize, geoRasterSize);
                _rasterProjector.ComputeDstEnvelope(_srcSpatialRef, xs, ys, geoSize, dstSpatialRef, out maxPrjEnvelope, progressCallback);
            }
            else
            {
                maxPrjEnvelope = PrjEnvelope.Empty;
            }
        }


    }
    public class TbbCoff
    {
        //波段号：从1开始
        public int Bandnum { get; set; }
        //数据集名称
        public string DataSetName { get; set; }
        //在数据集中的波段序号
        public int BandIndex { get; set; }
        //中心波长
        public double Required_MidWn { get; set; }
        //实际等效中心波长
        public double Mersi_EquivMidWn { get; set; }
        //典型黑体亮温
        public int T_type { get; set; }
        //典型黑体辐射度
        public double R_type { get; set; }
        //亮温修正系数斜率
        public double TbbCorr_Coeff_A { get; set; }
        //亮温修正系数截距
        public double TbbCorr_Coeff_B { get; set; }
        public TbbCoff(int bandnum, string dataSetName, int bandIndex, double required_MidWn, double mersi_EquivMidWn, int t_type, double r_type, double tbbCorr_Coeff_A, double tbbCorr_Coeff_B)
        {
            this.Bandnum = bandnum;
            this.DataSetName = dataSetName;
            this.BandIndex = bandIndex;
            this.Required_MidWn = required_MidWn;
            this.Mersi_EquivMidWn = mersi_EquivMidWn;
            this.T_type = t_type;
            this.R_type = r_type;
            this.TbbCorr_Coeff_A = tbbCorr_Coeff_A;
            this.TbbCorr_Coeff_B = tbbCorr_Coeff_B;
        }

    }
}