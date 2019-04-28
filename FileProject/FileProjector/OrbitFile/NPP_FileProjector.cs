using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIE.Meteo.RasterProject;
using System.IO;
using System.Collections.Concurrent;
using System.Collections;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace PIE.Meteo.FileProject
{
    public class NPP_FileProjector : FileProjector
    {
        AbstractWarpDataset mBandGeoDataset = null;
        AbstractWarpDataset iBandGeoDataset = null;
        Dictionary<int, AbstractWarpDataset> bandDatasetList = new Dictionary<int, AbstractWarpDataset>();
        NPP_PrjSetting _prjSettings = null;
        AbstractWarpDataset _geoDataProvider = null;
        SpatialReference _srcSpatialRef = null;
        string _solarZenithCacheFilename = string.Empty;
        private PrjBand[] _prjBands { get; set; } = null;
        public NPP_FileProjector()
            : base()
        {
            _name = "NPP_VIIRS";
            _fullname = "NPP_VIIRS轨道数据投影";
            _rasterProjector = new RasterProjector();
            _left = 10;
            _right = 10;
            _srcSpatialRef = SpatialReferenceFactory.CreateSpatialReference(4326);
            //_NODATA_VALUE = 65535;
            //_supportExtBandNames = new string[] { "DEM", "LandCover", "LandSeaMask" };
        }
        public override void ComputeDstEnvelope(AbstractWarpDataset srcRaster, SpatialReference dstSpatialRef, out PrjEnvelope maxPrjEnvelope, Action<int, string> progressCallback)
        {
            throw new NotImplementedException();
        }

        public override FilePrjSettings CreateDefaultPrjSettings()
        {
            throw new NotImplementedException();
        }

        public override bool IsSupport(string fileName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 进行Npp条带去除
        /// </summary>
        /// <param name="srcImgRaster"></param>
        /// <param name="i"></param>
        /// <param name="srcBandData"></param>
        /// <param name="solarZenithData"></param>
        /// <param name="srcBlockImgSize"></param>
        /// <param name="angleSize"></param>
        protected override void DoRadiation(AbstractWarpDataset srcImgRaster, int i, ushort[] srcBandData, float[] solarZenithData, Size srcBlockImgSize, Size angleSize)
        {
            //监测0值的上下值，求平均
            int height = srcBlockImgSize.Height;
            int width = srcBlockImgSize.Width;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            if (false)
            {
                Parallel.ForEach(Partitioner.Create(0, srcBlockImgSize.Width), range =>
                {
                    ushort[] temp = new ushort[height];
                    List<ushort> fillValue = new List<ushort>();
                    for (int ii = range.Item1; ii < range.Item2; ii++)
                    {
                        int noValuePixelCount = 0;
                        for (int j = 0; j < height; j++)
                        {
                            temp[j] = srcBandData[j * width + ii];
                            if (temp[j] == 0) noValuePixelCount++;
                        }
                        if (noValuePixelCount == 0)
                            continue;
                        for (int k = 0; k < 4; k++)
                        {
                            for (int t = 0; t < height; t++)
                            {
                                if (temp[t] != 0)
                                    continue;
                                if (t == 0)
                                {
                                    if (temp[t + 1] != 0)
                                        temp[t] = temp[t + 1];
                                }
                                else if (t == height - 1)
                                {
                                    if (temp[t - 1] != 0)
                                        temp[t] = temp[t - 1];
                                }
                                else
                                {
                                    if (temp[t + 1] != 0)
                                    {
                                        temp[t] = temp[t + 1];
                                    }
                                    else if (temp[t - 1] != 0)
                                    {
                                        temp[t] = temp[t - 1];
                                    }
                                }
                            }
                        }
                        //结束修改
                        for (int j = 0; j < height; j++)
                        {
                            srcBandData[j * width + ii] = temp[j];
                        }

                    }
                    temp = null;
                });
            }
            else
            {
                for (int t = 0; t < 4; t++)
                {
                    int validCount = 0;
                    double validSum = 0;
                    for (int ii = 1; ii < height - 1; ii++)
                    {
                        for (int jj = 1; jj < width - 1; jj++)
                        {
                            if (srcBandData[ii * width + jj] == 0)
                            {
                                validCount = 0;
                                validSum = 0;
                                for (int q = -1; q <= 1; q++)
                                {
                                    for (int w = -1; w <= 1; w++)
                                    {
                                        if (q != 0 && w != 0 && srcBandData[(ii + q) * width + jj + w] != 0)
                                        {
                                            validCount++;
                                            validSum += srcBandData[(ii + q) * width + jj + w];
                                        }
                                    }
                                }
                                if (validCount != 0)
                                {
                                    srcBandData[ii * width + jj] = (ushort)(validSum / validCount + 0.5);
                                }
                            }
                        }
                    }
                }
                {
                    int ii = 0;
                    int jj = 0;
                    int validCount = 0;
                    double validSum = 0;
                    //第一行
                    for (jj = 1; jj < width - 1; jj++)
                    {
                        if (srcBandData[ii * width + jj] == 0)
                        {
                            validCount = 0;
                            validSum = 0;
                            for (int q = 0; q <= 1; q++)
                            {
                                for (int w = -1; w <= 1; w++)
                                {
                                    if (q != 0 && w != 0 && srcBandData[(ii + q) * width + jj + w] != 0)
                                    {
                                        validCount++;
                                        validSum += srcBandData[(ii + q) * width + jj + w];
                                    }
                                }
                            }
                            if (validCount != 0)
                            {
                                srcBandData[ii * width + jj] = (ushort)(validSum / validCount + 0.5);
                            }
                        }
                    }
                    ii = height - 1;
                    //最后一行
                    for (jj = 1; jj < width - 1; jj++)
                    {
                        if (srcBandData[ii * width + jj] == 0)
                        {
                            validCount = 0;
                            validSum = 0;
                            for (int q = -1; q <= 0; q++)
                            {
                                for (int w = -1; w <= 1; w++)
                                {
                                    if (q != 0 && w != 0 && srcBandData[(ii + q) * width + jj + w] != 0)
                                    {
                                        validCount++;
                                        validSum += srcBandData[(ii + q) * width + jj + w];
                                    }
                                }
                            }
                            if (validCount != 0)
                            {
                                srcBandData[ii * width + jj] = (ushort)(validSum / validCount + 0.5);
                            }
                        }
                    }
                    jj = 0;
                    //第一列
                    for (ii = 1; ii < height - 1; ii++)
                    {
                        if (srcBandData[ii * width + jj] == 0)
                        {
                            validCount = 0;
                            validSum = 0;
                            for (int q = -1; q <= 1; q++)
                            {
                                for (int w = 0; w <= 1; w++)
                                {
                                    if (q != 0 && w != 0 && srcBandData[(ii + q) * width + jj + w] != 0)
                                    {
                                        validCount++;
                                        validSum += srcBandData[(ii + q) * width + jj + w];
                                    }
                                }
                            }
                            if (validCount != 0)
                            {
                                srcBandData[ii * width + jj] = (ushort)(validSum / validCount + 0.5);
                            }
                        }
                    }
                    //最后一列
                    jj = width-1;
                    for (ii = 1; ii < height - 1; ii++)
                    {
                        if (srcBandData[ii * width + jj] == 0)
                        {
                            validCount = 0;
                            validSum = 0;
                            for (int q = -1; q <= 1; q++)
                            {
                                for (int w = -1; w <= 0; w++)
                                {
                                    if (q != 0 && w != 0 && srcBandData[(ii + q) * width + jj + w] != 0)
                                    {
                                        validCount++;
                                        validSum += srcBandData[(ii + q) * width + jj + w];
                                    }
                                }
                            }
                            if (validCount != 0)
                            {
                                srcBandData[ii * width + jj] = (ushort)(validSum / validCount + 0.5);
                            }
                        }
                    }
                }
            }
            //Console.WriteLine("花费时间" + timer.ElapsedMilliseconds);
        }

        protected override void ReadLocations(AbstractWarpDataset srcRaster, out double[] xs, out double[] ys, out Size locationSize)
        {
            Band longitudeBand = null, latitudeBand = null;
            ReadLocations(srcRaster, out longitudeBand, out latitudeBand);//GetGeoBand
            Size srcLocationSize = new Size(longitudeBand.GetXSize(), longitudeBand.GetYSize());
            ReadLocations(longitudeBand, latitudeBand, srcLocationSize, out xs, out ys, out locationSize);
        }

        public override void BeginSession(AbstractWarpDataset srcRaster)
        {
            base.BeginSession(srcRaster);
        }
        public override void EndSession()
        {
            mBandGeoDataset?.Dispose();
            iBandGeoDataset?.Dispose();
            bandDatasetList.All(t => { t.Value?.Dispose(); return true; });
            base.EndSession();
        }

        public override void Project(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings, SpatialReference dstSpatial, Action<int, string> progressCallback)
        {
            try
            {
                InitInputDataset(srcRaster, prjSettings);
                ReadyArgs(srcRaster, prjSettings, dstSpatial, progressCallback);
                AbstractWarpDataset outwriter = null;
                try
                {
                    outwriter = CreateOutFile(_outfilename, _dstBandCount, _dstSize, null);
                    WriteMetaData(srcRaster, outwriter, _prjSettings);
                    ReadyAngleFiles(_geoDataProvider, _outfilename, _prjSettings, _dstSize, null);
                    ReadyExtBands(_geoDataProvider, _outfilename, _prjSettings, _dstSize, null);
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
            catch (Exception ex)
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
            //base.Project(srcRaster, prjSettings, dstSpatial, progressCallback);
        }

        private void ReadyArgs(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings, SpatialReference dstSpatialRef, Action<int, string> progressCallback)
        {
            if (srcRaster == null)
                throw new ArgumentNullException("srcRaster");
            if (prjSettings == null)
                throw new ArgumentNullException("prjSettings");
            _readyProgress = 0;
            base._prjSettings = prjSettings;
            _prjSettings = prjSettings as NPP_PrjSetting;
            if (dstSpatialRef != null && dstSpatialRef.IsGeographic()==1)
            {
                if (prjSettings.OutResolutionX < 0.0075 && prjSettings.OutResolutionY < 0.0075)
                {
                    _geoDataProvider = iBandGeoDataset;
                }
                else
                {
                    _geoDataProvider = iBandGeoDataset;
                }
            }
            else
            {
                _geoDataProvider = iBandGeoDataset;
            }
            _dstSpatialRef = dstSpatialRef;
            MemoryHelper.MemoryNeed(200, 1536);//剩余200MB,已使用1.2GB
            progressCallback?.Invoke(_readyProgress++, "准备相关参数");

            TryCreateDefaultArgs(srcRaster, _prjSettings, ref dstSpatialRef);
            //这里去除的是读取轨道数据时候的左右像元个数。
            _left = 8;
            _right = 8;
            TrySetLeftRightInvalidPixel(_prjSettings.ExtArgs);
            DoSession(srcRaster, _geoDataProvider, dstSpatialRef, _prjSettings, progressCallback);
            if (prjSettings.OutEnvelope == null || prjSettings.OutEnvelope == PrjEnvelope.Empty)
            {
                prjSettings.OutEnvelope = _maxPrjEnvelope;
                _orbitBlock = new Block { xBegin = 0, yBegin = 0, xEnd = srcRaster.Width - 1, yEnd = srcRaster.Height - 1 };
            }
            else
            {
                GetEnvelope(_xs, _ys, srcRaster.Width, srcRaster.Height, _prjSettings.OutEnvelope, out _orbitBlock);
                if (_orbitBlock == null || _orbitBlock.Width <= 0 || _orbitBlock.Height <= 0)
                    throw new Exception("数据不在目标区间内");
                float invalidPresent = (_orbitBlock.Width * _orbitBlock.Height * 1.0F) / (srcRaster.Width * srcRaster.Height);
                if (invalidPresent < 0.0001f)
                    throw new Exception("数据占轨道数据比例太小,有效率" + invalidPresent * 100 + "%");
                if (invalidPresent > 0.60)
                    _orbitBlock = new Block { xBegin = 0, yBegin = 0, xEnd = srcRaster.Width - 1, yEnd = srcRaster.Height - 1 };
            }
            //if (dstSpatialRef.Type == SpatialReferenceType.GeographicCS && (prjSettings.OutEnvelope.MaxY > 80 || prjSettings.OutEnvelope.MaxY < -80))
            //    throw new Exception(string.Format("高纬度数据，不适合投影为等经纬度数据[{0}]", _maxPrjEnvelope));
            _dstSpatialRef = dstSpatialRef;
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

        private void DoSession(AbstractWarpDataset srcRaster, AbstractWarpDataset geoRaster, SpatialReference dstSpatialRef, NPP_PrjSetting prjSettings, Action<int, string> progressCallback)
        {
            if (_curSession == null || _curSession != srcRaster || _isBeginSession)
            {
                progressCallback?.Invoke(_readyProgress++, "读取及预处理经纬度数据集");
                ReadyLocations(_geoDataProvider, SpatialReferenceFactory.CreateSpatialReference(4326), dstSpatialRef, out _srcLocationSize, out _xs, out _ys, out _maxPrjEnvelope, progressCallback);
                progressCallback?.Invoke(_readyProgress++, "准备其他参数");
                if (prjSettings.IsRadRef || prjSettings.IsRad)
                {
                    //ReadyRadiationArgs(srcRaster);
                }
                if (prjSettings.IsSolarZenith && prjSettings.IsRadRef)
                {
                    _solarZenithCacheFilename = GetSolarZenithCacheFilename(geoRaster.fileName);    //太阳天顶角数据
                    if (!File.Exists(_solarZenithCacheFilename))
                        ReadySolarZenithArgsToFile(geoRaster);
                    else
                    {
                        _solarZenithCacheRaster =WarpDataset.Open(_solarZenithCacheFilename);
                    }
                    if (prjSettings.IsSensorZenith)
                    {
                        ReadySensorZenith(geoRaster);
                    }
                }
                _rasterDataBands = TryCreateRasterDataBands(srcRaster, prjSettings, progressCallback);
                _isBeginSession = false;
            }
        }

        private Band[] TryCreateRasterDataBands(AbstractWarpDataset srcRaster, NPP_PrjSetting prjSettings, Action<int, string> progressCallback)
        {
            List<Band> rasterBands = new List<Band>();
            for (int i = 0; i < _prjBands.Length; i++)
            {
                if (progressCallback != null)
                    progressCallback(_readyProgress++, "准备第" + i + "个输入数据通道");
                PrjBand bandMap = _prjBands[i];
                int index = Convert.ToInt32(bandMap.BandName);
                if (prjSettings.IsRad || prjSettings.IsRadRef)
                {

                    Band[] latBands = bandDatasetList[index].GetBands("BrightnessTemperature");

                    if (latBands == null || latBands.Length == 0)
                    {
                        latBands = bandDatasetList[index].GetBands("Reflectance");
                        Console.WriteLine("Reflectance");
                    }
                    else
                    {
                        Console.WriteLine("BrightnessTemperature");
                    }
                    Band band = latBands[0];
                    rasterBands.Add(band);
                }
                else
                {
                    Band[] latBands = bandDatasetList[index].GetBands("Radiance");
                    Band band = latBands[0];
                    rasterBands.Add(band);
                }
            }
            return rasterBands.ToArray();
        }
        private void ReadySensorZenith(AbstractWarpDataset srcRaster)
        {
            _sensorSenithRaster = srcRaster;
            Band[] bands = srcRaster.GetBands("SatelliteZenith");
            if (bands != null || bands.Length != 1)
                _sensorSenithBand = bands[0];
        }
        private void ReadySolarZenithArgsToFile(AbstractWarpDataset srcRaster)
        {
            if (srcRaster == null)
                throw new ArgumentNullException("srcRaster", "获取太阳天顶角数据失败");
            try
            {
                //IBandProvider srcbandpro = srcRaster.BandProvider as IBandProvider;
                Size srcSize = Size.Empty;
                float[] readSolarZenithData = ReadDataSetToFloat32(srcRaster, "SolarZenithAngle", 0, out srcSize);

                _solarZenithCacheRaster = WriteData(readSolarZenithData, _solarZenithCacheFilename, srcSize.Width, srcSize.Height);
                readSolarZenithData = null;
            }
            catch (Exception ex)
            {
                throw new Exception("获取太阳天顶角数据失败", ex.InnerException);
            }
        }
        private float[] ReadDataSetToFloat32(AbstractWarpDataset srcbandpro, string dataSetName, int bandIndex, out Size srcSize)
        {
            float[] data = null;
            Band rasterBand = srcbandpro.GetBands(dataSetName)[0];
            {
                srcSize = new Size(rasterBand.GetXSize(), rasterBand.GetYSize());
                data = new float[srcSize.Width * srcSize.Height];
                        rasterBand.ReadRaster(0, 0, srcSize.Width, srcSize.Height, data, srcSize.Width, srcSize.Height, 0,0);
            }
            return data;
        }

        private void TryCreateDefaultArgs(AbstractWarpDataset srcRaster, NPP_PrjSetting prjSettings, ref SpatialReference dstSpatialRef)
        {
            if (dstSpatialRef == null)
                dstSpatialRef = _srcSpatialRef;
            if (string.IsNullOrWhiteSpace(prjSettings.OutFormat))
                prjSettings.OutFormat = "LDF";
            if (dstSpatialRef.IsGeographic()==1)
            {
                _srcImgResolution = 0.01F;
            }
            else
            {
                _srcImgResolution = 1000f;
            }
            if (prjSettings.OutResolutionX <= 0 || prjSettings.OutResolutionY <= 0)
            {
                if (dstSpatialRef.IsGeographic()==1)
                {
                    prjSettings.OutResolutionX = 0.0075F;
                    prjSettings.OutResolutionY = 0.0075F;
                }
                else
                {
                    prjSettings.OutResolutionX = 750F;
                    prjSettings.OutResolutionY = 750F;
                }
            }
            if (prjSettings.OutBandNos == null || prjSettings.OutBandNos.Length == 0)
            {
                _prjBands = PrjBand.NPP_Oribt;
            }
            else
            {
                List<PrjBand> bands = new List<PrjBand>();
                PrjBand[] defbands = PrjBand.NPP_Oribt;
                foreach (int bandNo in prjSettings.OutBandNos)
                {
                    bands.Add(defbands[bandNo - 1]);
                }
                _prjBands = bands.ToArray();
            }
        }

        /// <summary>
        /// 初始化输入数据集
        /// </summary>
        /// <param name="srcRaster"></param>
        /// <param name="prjSettings"></param>
        private void InitInputDataset(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings)
        {
            if (iBandGeoDataset == null)
            {
                if ((new int[] { 1, 2, 3, 4, 5 }).Any(t => prjSettings.OutBandNos.Contains(t)))
                {
                    iBandGeoDataset = WarpDataset.Open(FileFinder.TryFindNppIbandGeoFile(srcRaster));
                }
            }
            if (mBandGeoDataset == null)
            {
                mBandGeoDataset = WarpDataset.Open(FileFinder.TryFindNppMbandGeoFile(srcRaster));
            }
            foreach (var item in prjSettings.OutBandNos)
            {
                if (!bandDatasetList.ContainsKey(item))
                {
                    if (item <= 5)
                    {
                        bandDatasetList.Add(item, WarpDataset.Open(FileFinder.TryFindNppIbandFile(srcRaster, item)));
                    }
                    else
                    {
                        bandDatasetList.Add(item, WarpDataset.Open(FileFinder.TryFindNppMbandFile(srcRaster, item - 5)));
                    }
                }
            }
        }

        /// <summary>
        /// Npp卫星数据读取
        /// </summary>
        /// <param name="bandData"></param>
        /// <param name="dstBandIndex"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <param name="blockWidth"></param>
        /// <param name="blockHeight"></param>
        /// <param name="srcImgSize"></param>
        protected override void ReadImgBand(out ushort[] bandData, int dstBandIndex, int xOffset, int yOffset, int blockWidth, int blockHeight, Size? srcImgSize = default(Size?))
        {
            //Npp的波段数据含有多种无效值，并且Radiance与Temperature数据集可能为Uint16也可能为Float
            //65535 65534 65533 65532 65531 65530 65529 65528
            //-999.9 -999.8 -999.7 -999.6 -999.5 -999.4 -999.3 -999.2
            //如果需要判断读取的是反射率还是亮温，可以通过_prjSettings.IsRadRef||_prjSettings.IsRad
            //_prjBands为待投影波段信息

            //以下代码为假设对NPP数据进行亮温与反射率的读取

            Band rasterBand = _rasterDataBands[dstBandIndex];//
            bandData = new ushort[blockWidth * blockHeight];
            bool ok = false;
            try
            {
                double ratioX = 1, ratioY = 1;
                if (srcImgSize.HasValue)
                {
                    ratioX = rasterBand.GetXSize() * 1.0 / srcImgSize.Value.Width;
                    ratioY = rasterBand.GetYSize() * 1.0 / srcImgSize.Value.Height;
                }

                Func<double, int> ToInt = t => Convert.ToInt32(t);
                if (rasterBand.DataType == DataType.GDT_UInt16)
                {
                    ok = rasterBand.ReadRaster(ToInt(xOffset * ratioX), ToInt(yOffset * ratioY),
                        ToInt(blockWidth * ratioX), ToInt(blockHeight * ratioY),
                        bandData, blockWidth, blockHeight, 0,0)== CPLErr.CE_None;
                    //无效值剔除
                    for (int i = 0; i < blockWidth * blockHeight; i++)
                    {
                        if (bandData[i] > 65527)
                            bandData[i] = 0;
                    }
                }
                else if (rasterBand.DataType== DataType.GDT_Float32)
                {
                    float[] tempBuf = new float[blockWidth * blockHeight];
                    ok = rasterBand.ReadRaster(ToInt(xOffset * ratioX), ToInt(yOffset * ratioY),
                            ToInt(blockWidth * ratioX), ToInt(blockHeight * ratioY),
                            tempBuf, blockWidth, blockHeight, 0,0)== CPLErr.CE_None;
                    for (int i = 0; i < blockWidth * blockHeight; i++)
                    {
                        //过滤无效值为0
                        bandData[i] = tempBuf[i] < 0 ? (UInt16)0 : (UInt16)(tempBuf[i] * 10);
                    }
                    tempBuf = null;
                }

                if (!ok)
                    throw new Exception($"波段数据读取失败");
            }
            finally
            {
                if (!ok)
                {
                    System.Diagnostics.Debug.WriteLine("ReadImgBand  Index:{0}失败", dstBandIndex);
                }
            }
        }



        /*
         * 带i的是高分辨率
        gdnbo
        VIIRS-DNB-GEO_All WGS84
        gimgo
        VIIRS-IMG-GEO_All WGS84
        gitco
        VIIRS-IMG-GEO-TC_All 地形校正
        gmodo
        VIIRS-MOD-GEO_All WGS84
        gmtco
        VIIRS-MOD-GEO-TC_All 地形校正
        icdbg
        VIIRS-MOD-UNAGG-GEO_All
        ivcdb
        VIIRS-DualGain-Cal-IP_All
        ivobc
        VIIRS-OBC-IP_All
        svdnb
        VIIRS-DNB-SDR_All
        svi01-05
        VIIRS-I1-SDR_All
        svm01-16
        VIIRS-M1-SDR_All

        i1 Rad uint16 Ref Uint16
        i2 Rad uint16 Ref uint16
        i3 Rad uint16 Ref uint16
        i4 Rad uint16 Tem uint16
        i5 Rad uint16 Tem uint16

        m1 Rad uint16 Ref uint16
        m2 Rad uint16 Ref uint16
        m3 Rad float Ref uint16
        m4 Rad float Ref uint16
        m5 Rad float Ref uint16
        m6 Rad uint16 Ref uint16
        m7 Rad float Ref uint16
        m8 Rad uint16 Ref uint16
        m9 Rad uint16 Ref uint16
        m10 Rad uint16 Ref uint16
        m11 Rad uint16 Ref uint16
        m12 Rad uint16 Tem uint16 
        m13 Rad float Tem float
        m14 Rad uint16 Tem uint16
        m15 Rad uint16 Tem uint16
        m16 Rad uint16 Tem uint16
         */

    }
}
