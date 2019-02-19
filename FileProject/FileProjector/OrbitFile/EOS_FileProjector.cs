using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using PIE.Meteo.Model;
using PIE.Meteo.RasterProject;

namespace PIE.Meteo.FileProject
{
    public class EOS_FileProjector : FileProjector
    {
        private const double DEG_TO_RAD_P100 = 0.000174532925199432955; // (PI/180)/100;

        private const double PI = 3.14159265358979323e0;

        //准备[辐射定标]参数
        //private float[] _radiance_Scales;
        //private float[] _radiance_Offsets;
        private float[] _v;

        private string _fileAliasName;
        private SpatialReference _srcSpatialRef = null;

        //太阳高度角
        private string _szDataFilename = "";
        private string _fileType = "1000";
        private EOS_MODIS_PrjSettings _prjSettings;
        private AbstractWarpDataset _angleDataProvider = null;
        private PrjBand[] _prjBands = null;

        public EOS_FileProjector()
            : base()
        {
            _name = "EOS";
            _fullname = "EOS轨道文件投影";
            _rasterProjector = new RasterProjector();
            _srcSpatialRef = SpatialReferenceFactory.CreateSpatialReference(4326);
            _supportExtBandNames = new string[] {"Height", "Range", "Land/SeaMask"};
        }

        public override bool IsSupport(string fileName)
        {
            return false;
        }

        public override AbstractWarpDataset Project(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings,
            AbstractWarpDataset dstRaster, int beginBandIndex, Action<int, string> progressCallback)
        {
            if (dstRaster == null)
                return null;
            try
            {
                _dstSpatialRef = dstRaster.SpatialRef;
                Envelope coordEnv = dstRaster.GetEnvelope();
                prjSettings.OutEnvelope = PrjEnvelope.CreateByLeftTop(_dstSpatialRef, coordEnv.MinX, coordEnv.MaxY,
                    coordEnv.GetWidth(), coordEnv.GetHeight());
                ReadyArgs(srcRaster as WarpDataset, prjSettings, dstRaster.SpatialRef, progressCallback);
                string outfilename = _prjSettings.OutPathAndFileName;
                try
                {
                    Size outSize = new Size(dstRaster.Width, dstRaster.Height);
                    string[] options = new string[]
                    {
                        "INTERLEAVE=BSQ",
                        "VERSION=LDF",
                        "WITHHDR=TRUE",
                        "SPATIALREF=" + _dstSpatialRef.ExportToProj4(),
                        "MAPINFO={" + 1 + "," + 1 + "}:{" + _prjSettings.OutEnvelope.MinX + "," +
                        _prjSettings.OutEnvelope.MaxY + "}:{" + dstRaster.ResolutionX + "," + dstRaster.ResolutionY +
                        "}"
                    };
                    ReadyAngleFiles(_angleDataProvider, outfilename, _prjSettings, outSize, options);
                    ReadyExtBands(_angleDataProvider, _outfilename, _prjSettings, _dstSize, options);
                    ProjectToLDF(srcRaster, dstRaster, beginBandIndex, progressCallback);
                    return dstRaster;
                }
                catch (IOException ex)
                {
                    if (ex.Message == "磁盘空间不足。\r\n" && File.Exists(outfilename))
                        File.Delete(outfilename);
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
            catch
            {
                TryDeleteCurCatch();
                throw;
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

        public override void Project(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings,
            SpatialReference dstSpatialRef, Action<int, string> progressCallback)
        {
            try
            {
                ReadyArgs(srcRaster, prjSettings, dstSpatialRef, progressCallback);
                AbstractWarpDataset outwriter = null;
                try
                {
                    Size outSize = _prjSettings.OutSize;
                    //string[] options = new string[]{
                    //        "INTERLEAVE=BSQ",
                    //        "VERSION=LDF",
                    //        "WITHHDR=TRUE",
                    //        "SPATIALREF=" + _dstSpatialRef.ToProj4String(),
                    //        "MAPINFO={" + 1 + "," + 1 + "}:{" + _prjSettings.OutEnvelope.MinX + "," + _prjSettings.OutEnvelope.MaxY + "}:{" + _outResolutionX + "," + _outResolutionY + "}"
                    //    };
                    //,"BANDNAMES="+ BandNameString(_prjSettings.OutBandNos)    

                    List<string> options = new List<string>();
                    options.Add("INTERLEAVE=BSQ");
                    options.Add("VERSION=LDF");
                    options.Add("WITHHDR=TRUE");
                    options.Add("SPATIALREF=" + dstSpatialRef.ExportToProj4());
                    options.Add("MAPINFO={" + 1 + "," + 1 + "}:{" + _prjSettings.OutEnvelope.MinX + "," +
                                _prjSettings.OutEnvelope.MaxY + "}:{" + _outResolutionX + "," + _outResolutionY + "}");
                    List<string> op1 = new List<string>(options);
                    op1.Add("BANDNAMES=" + BandNameString(_prjSettings.OutBandNos));

                    outwriter = CreateOutFile(_outfilename, _dstBandCount, outSize, op1.ToArray());
                    ReadyAngleFiles(_angleDataProvider, _outfilename, _prjSettings, outSize, options.ToArray());
                    ReadyExtBands(_angleDataProvider, _outfilename, _prjSettings, _dstSize, options.ToArray());
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
                TryDeleteCurCatch();
                throw;
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

        private void ReadyArgs(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings,
            SpatialReference dstSpatialRef, Action<int, string> progressCallback)
        {
            float resolutionScale = 1f;
            _readyProgress = 0;
            if (progressCallback != null)
                progressCallback(_readyProgress++, "准备相关参数");
            _prjSettings = ArgsCheck(srcRaster, prjSettings, progressCallback);
            base._prjSettings = prjSettings;
            _fileType = CheckFile(srcRaster);
            _angleDataProvider = (prjSettings as EOS_MODIS_PrjSettings).LocationFile;
            switch (_fileType)
            {
                case "1000":
                    if (_prjSettings.OutEnvelope == null || _prjSettings.OutEnvelope == PrjEnvelope.Empty)
                    {
                        MemoryHelper.MemoryNeed(300, 1536);
                    }
                    else
                        MemoryHelper.MemoryNeed(300, 1536); //剩余900MB,已使用1.2GB

                    TryCreateDefaultArgsKM(srcRaster, _prjSettings, ref _dstSpatialRef);
                    break;
                case "0500":
                    resolutionScale = 2f;
                    TryCreateDefaultArgsHKM(srcRaster, _prjSettings, ref _dstSpatialRef);
                    break;
                case "0250":
                    resolutionScale = 4f;
                    TryCreateDefaultArgsQKM(srcRaster, _prjSettings, ref _dstSpatialRef);
                    break;
                default:
                    break;
            }

            _left = 16;
            _right = 16;
            TrySetLeftRightInvalidPixel(_prjSettings.ExtArgs);
            DoSession(srcRaster, _dstSpatialRef, _prjSettings, progressCallback);
            if (_prjSettings.OutEnvelope == null || _prjSettings.OutEnvelope == PrjEnvelope.Empty)
            {
                _prjSettings.OutEnvelope = _maxPrjEnvelope;
                _orbitBlock = new Block
                    {xBegin = 0, yBegin = 0, xEnd = srcRaster.Width - 1, yEnd = srcRaster.Height - 1};
            }
            else
            {
                GetEnvelope(_xs, _ys, _srcLocationSize.Width, _srcLocationSize.Height, _prjSettings.OutEnvelope,
                    out _orbitBlock);
                if (_orbitBlock == null || _orbitBlock.Width <= 0 || _orbitBlock.Height <= 0)
                    throw new Exception("数据不在目标区间内");
                float invalidPresent = (_orbitBlock.Width * _orbitBlock.Height * resolutionScale) /
                                       (srcRaster.Width * srcRaster.Height);
                if (invalidPresent < 0.0001f)
                    throw new Exception("数据占轨道数据比例太小,有效率" + invalidPresent * 100 + "%");
                if (invalidPresent > 0.60f)
                    _orbitBlock = new Block
                        {xBegin = 0, yBegin = 0, xEnd = srcRaster.Width - 1, yEnd = srcRaster.Height - 1};
            }

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
            _isSolarZenith = _prjSettings.IsSolarZenith;
            _isSensorZenith = _prjSettings.IsSensorZenith;

            _isRadRef = _prjSettings.IsRadRef;
            _isRad = _prjSettings.IsRad;
        }

        private EOS_MODIS_PrjSettings ArgsCheck(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings,
            Action<int, string> progressCallback)
        {
            if (srcRaster == null)
                throw new ArgumentNullException("srcRaster");
            if (prjSettings == null)
                throw new ArgumentNullException("prjSettings");
            EOS_MODIS_PrjSettings _prjSettings = prjSettings as EOS_MODIS_PrjSettings;
            if (_prjSettings.LocationFile == null)
                throw new ArgumentNullException("prjSettings.LocationFile", "EOS投影未获得经纬度坐标(MOD03.hdf)文件");
            return _prjSettings;
        }

        public override void EndSession()
        {
            base.EndSession();
            _xs = null;
            _ys = null;
            //_radiance_Scales = null;
            //_radiance_Offsets = null;
            _v = null;
            if (_solarZenithCacheRaster != null)
            {
                _solarZenithCacheRaster.Dispose();
                _solarZenithCacheRaster = null;
            }

            _radiance_Scales_EV_1KM_RefSB = null;
            _radiance_Offsets_EV_1KM_RefSB = null;
            _radiance_Scales_EV_250_Aggr1km_RefSB = null;
            _radiance_Offsets_EV_250_Aggr1km_RefSB = null;
            _radiance_Scales_EV_500_Aggr1km_RefSB = null;
            _radiance_Offsets_EV_500_Aggr1km_RefSB = null;
            _radiance_Scales_EV_1KM_Emissive = null;
            _radiance_Offsets_EV_1KM_Emissive = null;
            _radiance_Scales_EV_250_Aggr500_RefSB = null;
            _radiance_Offsets_EV_250_Aggr500_RefSB = null;
            _radiance_Scales_EV_500_RefSB = null;
            _radiance_Offsets_EV_500_RefSB = null;
            _radiance_Scales_EV_250_RefSB = null;
            _radiance_Offsets_EV_250_RefSB = null;
        }

        private void DoSession(AbstractWarpDataset srcRaster, SpatialReference dstSpatialRef,
            EOS_MODIS_PrjSettings prjSettings, Action<int, string> progressCallback)
        {
            if (_curSession == null || _curSession != srcRaster || _isBeginSession)
            {
                AbstractWarpDataset locationRester = prjSettings.LocationFile;
                ReadyLocations(locationRester, dstSpatialRef, out _xs, out _ys, out _maxPrjEnvelope,
                    out _srcLocationSize, progressCallback);
                if (progressCallback != null)
                    progressCallback(_readyProgress++, "准备其他参数");
                if (prjSettings.IsSolarZenith && prjSettings.IsRadRef)
                {
                    _szDataFilename = GetSolarZenithCacheFilename(locationRester.fileName);
                    if (!File.Exists(_szDataFilename))
                        ReadySolarZenithArgsToFile(locationRester);
                    else
                    {
                        var ds = Gdal.OpenShared(_szDataFilename, Access.GA_ReadOnly);
                        _solarZenithCacheRaster = new WarpDataset(ds, _szDataFilename);
                    }

                    if (prjSettings.IsSensorZenith)
                    {
                        ReadySensorZenith(locationRester);
                    }
                }

                _rasterDataBands = TryCreateRasterDataBands(srcRaster, prjSettings, progressCallback);
                _isBeginSession = false;
            }
        }

        private void ReadySensorZenith(AbstractWarpDataset srcRaster)
        {
            _sensorSenithRaster = srcRaster;
            Band[] bands = srcRaster.GetBands("SensorZenith");
            if (bands != null || bands.Length != 1)
                _sensorSenithBand = bands[0];
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

        /// <summary> 
        /// 准备定位信息,WGS-84经纬度值
        /// 计算投影后的值，并计算范围
        /// </summary>
        private void ReadyLocations(AbstractWarpDataset srcRaster, SpatialReference dstSpatialRef,
            out double[] xs, out double[] ys, out PrjEnvelope maxPrjEnvelope, out Size locationSize,
            Action<int, string> progressCallback)
        {
            if (progressCallback != null)
                progressCallback(_readyProgress++, "读取经纬度数据集");
            ReadLocations(srcRaster, out xs, out ys, out locationSize);
            TryResetLonlatForLeftRightInvalid(xs, ys, locationSize);
            if (progressCallback != null)
                progressCallback(_readyProgress++, "预处理经纬度数据集");
            _rasterProjector.ComputeDstEnvelope(_srcSpatialRef, xs, ys, locationSize, dstSpatialRef, out maxPrjEnvelope,
                progressCallback);
        }

        //读取定位信息(经纬度数据集)
        protected override void ReadLocations(AbstractWarpDataset raster, out double[] longitudes,
            out double[] latitudes, out Size lonSize)
        {
            Band lonsBand = null;
            Band latBand = null;
            ReadModisLocations(raster, out lonsBand, out latBand);

            {
                lonSize = new Size(lonsBand.XSize, lonsBand.YSize);
                longitudes = new Double[lonsBand.XSize * lonsBand.YSize];

                lonsBand.ReadRaster(0, 0, lonsBand.XSize, lonsBand.YSize, longitudes,
                    lonsBand.XSize, lonsBand.YSize, 0, 0);
            }
            (lonsBand as IDisposable).Dispose();
            {
                latitudes = new Double[lonSize.Width * lonSize.Height];

                latBand.ReadRaster(0, 0, latBand.XSize, latBand.YSize, latitudes, latBand.XSize,
                    latBand.YSize, 0, 0);
                (latBand as IDisposable).Dispose();
            }
        }

        private Band[] TryCreateRasterDataBands(AbstractWarpDataset srcRaster, EOS_MODIS_PrjSettings prjSettings,
            Action<int, string> progressCallback)
        {
            List<Band> rasterBands = new List<Band>();
            for (int i = 0; i < _prjBands.Length; i++)
            {
                if (progressCallback != null)
                    progressCallback(_readyProgress++, "准备第" + i + "个输入数据通道");
                PrjBand bandMap = _prjBands[i];
                Band[] latBands = srcRaster.GetBands(bandMap.DataSetName);
                Band band = latBands[bandMap.DataSetIndex];
                rasterBands.Add(band);
            }

            return rasterBands.ToArray();
        }

        private void ReadBandData(Band latBand, UInt16[] bandData, int xOffset, int yOffset, int blockWidth,
            int blockHeight)
        {
            latBand.ReadRaster(xOffset, yOffset, blockWidth, blockHeight, bandData, blockWidth, blockHeight,
                0, 0);
        }

        /// <summary> 辐射值计算</summary>
        private void DoRadiation(ushort[] srcBandData, Size srcDataSize, Size srcLocationSize, string datasetName,
            int bandIndex,
            AbstractWarpDataset imgRaster, bool isRadiation, bool isSolarZenith, float[] solarZenithData)
        {
            if (!isRadiation)
                return;
            ReadyRadiationArgs(imgRaster, datasetName);
            switch (datasetName)
            {
                case "EV_1KM_RefSB":
                {
                    float scale = 1000 * _radiance_Scales_EV_1KM_RefSB[bandIndex];
                    float offSet = _radiance_Offsets_EV_1KM_RefSB[bandIndex];
                    RefSBRadiation(srcBandData, isSolarZenith, scale, offSet, solarZenithData);
                }
                    break;
                case "EV_500_Aggr1km_RefSB":
                {
                    float scale = 1000 * _radiance_Scales_EV_500_Aggr1km_RefSB[bandIndex];
                    float offSet = _radiance_Offsets_EV_500_Aggr1km_RefSB[bandIndex];
                    RefSBRadiation(srcBandData, isSolarZenith, scale, offSet, solarZenithData);
                }
                    break;
                case "EV_250_Aggr1km_RefSB":
                {
                    float scale = 1000 * _radiance_Scales_EV_250_Aggr1km_RefSB[bandIndex];
                    float offSet = _radiance_Offsets_EV_250_Aggr1km_RefSB[bandIndex];
                    RefSBRadiation(srcBandData, isSolarZenith, scale, offSet, solarZenithData);
                }
                    break;
                case "EV_1KM_Emissive":
                {
                    float scale = _radiance_Scales_EV_1KM_Emissive[bandIndex];
                    float offSet = _radiance_Offsets_EV_1KM_Emissive[bandIndex];
                    float v = _v[bandIndex];
                    EmissiveRadistion(srcBandData, scale, offSet, v);
                }
                    break;
                case "EV_250_RefSB":
                {
                    float scale = 1000 * _radiance_Scales_EV_250_RefSB[bandIndex];
                    float offSet = _radiance_Offsets_EV_250_RefSB[bandIndex];
                    RefSBRadiation(srcBandData, isSolarZenith, scale, offSet, solarZenithData, srcDataSize,
                        srcLocationSize);
                }
                    break;
                case "EV_500_RefSB":
                {
                    float scale = 1000 * _radiance_Scales_EV_500_RefSB[bandIndex];
                    float offSet = _radiance_Offsets_EV_500_RefSB[bandIndex];
                    RefSBRadiation(srcBandData, isSolarZenith, scale, offSet, solarZenithData, srcDataSize,
                        srcLocationSize);
                }
                    break;
                case "EV_250_Aggr500_RefSB":
                {
                    float scale = 1000 * _radiance_Scales_EV_250_Aggr500_RefSB[bandIndex];
                    float offSet = _radiance_Offsets_EV_250_Aggr500_RefSB[bandIndex];
                    RefSBRadiation(srcBandData, isSolarZenith, scale, offSet, solarZenithData, srcDataSize,
                        srcLocationSize);
                }
                    break;
                default:
                    break;
            }
        }

        private void EmissiveRadistion(ushort[] srcBandData, float scale, float offSet, float v)
        {
            float ws = v / 1000 / 1000000;
            double wsPow5 = ws * ws * ws * ws * ws; // Math.Pow(ws, 5);
            double c1CwsPow5 = c1 / wsPow5;
            double c2Cws = c2 / ws;

            Parallel.For(0, srcBandData.Length, (index) =>
            {
                double temperatureBB;
                double radiation = 1.0e06 * scale * (srcBandData[index] - offSet);
                //srcBandData[index] = (ushort)planck_m(v / 1000, radiation);//减少计算，改为下面的过程
                temperatureBB = (c2Cws / Math.Log(c1CwsPow5 / radiation + 1));
                if (_isSensorZenith && _sensorZenithData != null) //临边变暗订正
                {
                    double deltaT;
                    double sensorZenith = _sensorZenithData[index] * 0.01d;
                    deltaT = temperatureBB + (Math.Pow(Math.E, 0.00012 * sensorZenith * sensorZenith) - 1) *
                             (0.1072 * temperatureBB - 26.81);
                    srcBandData[index] = (UInt16) (deltaT * 10);
                }
                else
                    srcBandData[index] = (UInt16) (temperatureBB * 10);
            });

            //int xh = srcBandData.Length / 16; //考虑到目前机器还是16核以下的居多
            //Parallel.For(0, 16, (c) =>
            //{
            //    int off = c * xh;
            //    int index;
            //    for (int i = 0; i < xh; i++)
            //    {
            //        index = off + i;
            //        double radiation = 1.0e06 * scale * (srcBandData[index] - offSet);
            //        //srcBandData[index] = (ushort)planck_m(v / 1000, radiation);//减少计算，改为下面的过程
            //        srcBandData[index] = (ushort)(c210Cws / Math.Log(c1CwsPow5 / radiation + 1));
            //    }
            //});
            //int sy = srcBandData.Length % 16;
            //if (sy != 0)
            //{
            //    Parallel.For(srcBandData.Length - sy, srcBandData.Length, (index) =>
            //    {
            //        double radiation = 1.0e06 * scale * (srcBandData[index] - offSet);
            //        //srcBandData[index] = (ushort)planck_m(v / 1000, radiation);//减少计算，改为下面的过程
            //        srcBandData[index] = (ushort)(c210Cws / Math.Log(c1CwsPow5 / radiation + 1));
            //    });
            //}
        }

        const double h = 6.6256e-34; //Planck constant (Joule second)
        const double c = 3e+8; //Speed of light in vacuum (meters per second)

        const double k = 1.38e-23; //Boltzmann constant (Joules per Kelvin)

        //Derived constants
        const double c1 = 2.0 * h * c * c;

        const double c2 = (h * c) / k;

        //private float _srcImgResolution;
        private double planck_m(double w, double t)
        {
            double ws = 1.0e-6 * w; // Convert wavelength to meters
            double tmp = c2 / (ws * Math.Log(c1 / (Math.Pow(ws, 5) * t) + 1));
            return tmp * 10;
        }

        private void RefSBRadiation(ushort[] srcBandData, bool isSolarZenith, float scale, float offSet,
            float[] solarZenithData)
        {
            if (isSolarZenith) //高度角订正
            {
                int xh = srcBandData.Length / 16;
                Parallel.For(0, 16, (c) =>
                {
                    int off = c * xh;
                    int index;
                    for (int i = 0; i < xh; i++)
                    {
                        index = off + i;
                        double radiation = scale * (srcBandData[index] - offSet);
                        srcBandData[index] = (UInt16) (radiation * solarZenithData[index]);
                    }
                });
                int sy = srcBandData.Length % 16;
                if (sy != 0)
                {
                    Parallel.For(srcBandData.Length - sy, srcBandData.Length, (index) =>
                    {
                        double radiation = scale * (srcBandData[index] - offSet);
                        srcBandData[index] = (UInt16) (radiation * solarZenithData[index]);
                    });
                }
            }
            else
            {
                int xh = srcBandData.Length / 16;
                Parallel.For(0, 16, (c) =>
                {
                    int off = c * xh;
                    int index;
                    for (int i = 0; i < xh; i++)
                    {
                        index = off + i;
                        if (srcBandData[index] > 65000) //理论上讲反射率应当是0-100
                            srcBandData[index] = 0;
                        else
                            srcBandData[index] = (UInt16) (scale * (srcBandData[index] - offSet));
                    }
                });
                int sy = srcBandData.Length % 16;
                if (sy != 0)
                {
                    Parallel.For(srcBandData.Length - sy, srcBandData.Length, (index) =>
                    {
                        if (srcBandData[index] > 65000) //理论上讲反射率应当是0-100
                            srcBandData[index] = 0;
                        else
                            srcBandData[index] = (UInt16) (scale * (srcBandData[index] - offSet));
                    });
                }
            }
        }

        private void RefSBRadiation(ushort[] srcBandData, bool isSolarZenith, float scale, float offSet,
            float[] solarZenithData, Size dataSize, Size locationSize)
        {
            float scoreX = (float) dataSize.Width / locationSize.Width;
            float scoreY = (float) dataSize.Height / locationSize.Height;
            int dataHeight = dataSize.Height;
            int dataWidth = dataSize.Width;
            int lcWidth = locationSize.Width;
            if (isSolarZenith) //高度角订正
            {
                Parallel.For(0, dataHeight, (dataRow) =>
                {
                    for (int dataCol = 0; dataCol < dataWidth; dataCol++)
                    {
                        int offset = dataRow * dataWidth;
                        int index = offset + dataCol;
                        ushort curData = srcBandData[index];
                        double radiation = scale * (curData - offSet);
                        if (curData > 65000) //理论上讲反射率应当是0-100
                            srcBandData[index] = 0;
                        else
                        {
                            int szRow = (int) (dataRow / scoreY);
                            int szCol = (int) (dataCol / scoreX);
                            double solarZenith = solarZenithData[szRow * lcWidth + szCol]; //;
                            srcBandData[index] = (UInt16) (radiation * solarZenith);
                        }
                    }
                });
            }
            else
            {
                int xh = srcBandData.Length / 16;
                Parallel.For(0, 16, (c) =>
                {
                    int off = c * xh;
                    int index;
                    for (int i = 0; i < xh; i++)
                    {
                        index = off + i;
                        if (srcBandData[index] > 65000)
                            srcBandData[index] = 0;
                        else
                            srcBandData[index] = (UInt16) (scale * (srcBandData[index] - offSet));
                    }
                });
                int sy = srcBandData.Length % 16;
                if (sy != 0)
                {
                    Parallel.For(srcBandData.Length - sy, srcBandData.Length, (index) =>
                    {
                        if (srcBandData[index] > 65000)
                            srcBandData[index] = 0;
                        else
                            srcBandData[index] = (UInt16) (scale * (srcBandData[index] - offSet));
                    });
                }
            }
        }

        #region 定标参数读取

        private float[] _radiance_Scales_EV_1KM_RefSB = null;
        private float[] _radiance_Offsets_EV_1KM_RefSB = null;
        private float[] _radiance_Scales_EV_250_Aggr1km_RefSB = null;
        private float[] _radiance_Offsets_EV_250_Aggr1km_RefSB = null;
        private float[] _radiance_Scales_EV_500_Aggr1km_RefSB = null;
        private float[] _radiance_Offsets_EV_500_Aggr1km_RefSB = null;
        private float[] _radiance_Scales_EV_1KM_Emissive = null;
        private float[] _radiance_Offsets_EV_1KM_Emissive = null;
        private float[] _radiance_Scales_EV_250_Aggr500_RefSB = null;
        private float[] _radiance_Offsets_EV_250_Aggr500_RefSB = null;
        private float[] _radiance_Scales_EV_500_RefSB = null;
        private float[] _radiance_Offsets_EV_500_RefSB = null;
        private float[] _radiance_Scales_EV_250_RefSB = null;
        private float[] _radiance_Offsets_EV_250_RefSB = null;

        //准备[辐射定标]参数
        private void ReadyRadiationArgs(AbstractWarpDataset srcRaster, string dataSetName)
        {
            if (srcRaster == null)
                throw new ArgumentNullException("srcRaster", "获取亮温转换参数失败：参数srcRaster为空");
            if (dataSetName == null)
                throw new ArgumentNullException("dataSetName", "获取亮温转换参数失败：参数srcRaster为空");
            try
            {
                switch (dataSetName)
                {
                    case "EV_1KM_RefSB":
                        if (_radiance_Scales_EV_1KM_RefSB == null)
                        {
                            _radiance_Scales_EV_1KM_RefSB =
                                ReadDataSetAttrToFloat(srcRaster, "EV_1KM_RefSB", "reflectance_scales", 15);
                            _radiance_Offsets_EV_1KM_RefSB =
                                ReadDataSetAttrToFloat(srcRaster, "EV_1KM_RefSB", "reflectance_offsets", 15);
                        }

                        break;
                    case "EV_250_Aggr1km_RefSB":
                        if (_radiance_Scales_EV_250_Aggr1km_RefSB == null)
                        {
                            _radiance_Scales_EV_250_Aggr1km_RefSB = ReadDataSetAttrToFloat(srcRaster,
                                "EV_250_Aggr1km_RefSB", "reflectance_scales", 2);
                            _radiance_Offsets_EV_250_Aggr1km_RefSB = ReadDataSetAttrToFloat(srcRaster,
                                "EV_250_Aggr1km_RefSB", "reflectance_offsets", 2);
                        }

                        break;
                    case "EV_500_Aggr1km_RefSB":
                        if (_radiance_Scales_EV_500_Aggr1km_RefSB == null)
                        {
                            _radiance_Scales_EV_500_Aggr1km_RefSB = ReadDataSetAttrToFloat(srcRaster,
                                "EV_500_Aggr1km_RefSB", "reflectance_scales", 5);
                            _radiance_Offsets_EV_500_Aggr1km_RefSB = ReadDataSetAttrToFloat(srcRaster,
                                "EV_500_Aggr1km_RefSB", "reflectance_offsets", 5);
                        }

                        break;
                    case "EV_1KM_Emissive": //热红外satellite Terra (EOS AM-1)  Aqua (EOS PM-1)
                        if (_radiance_Scales_EV_1KM_Emissive == null)
                        {
                            _radiance_Scales_EV_1KM_Emissive =
                                ReadDataSetAttrToFloat(srcRaster, "EV_1KM_Emissive", "radiance_scales", 16);
                            _radiance_Offsets_EV_1KM_Emissive =
                                ReadDataSetAttrToFloat(srcRaster, "EV_1KM_Emissive", "radiance_offsets", 16);
                            if (_fileAliasName == "Terra")
                            {
                                _v = new float[]
                                {
                                    3788.3F, 3992.2F, 3972.0F, 4056.7F, 4473.2F, 4545.4F, 6770.5F, 7342.9F, 8528.7F,
                                    9734.1F, 11018.9F, 12032.1F, 13365.0F, 13683.3F, 13925.2F, 14195.6F
                                };
                            }
                            else if (_fileAliasName == "Aqua")
                            {
                                _v = new float[]
                                {
                                    3780.2F, 3981.8F, 3972.0F, 4061.6F, 4448.3F, 4526.3F, 6786.8F, 7349.3F, 8555.3F,
                                    9723.7F, 11026.2F, 12042.3F, 13364.7F, 13685.9F, 13913.2F, 14215.2F
                                };
                            }
                        }

                        break;
                    case "EV_250_Aggr500_RefSB":
                        if (_radiance_Scales_EV_250_Aggr500_RefSB == null)
                        {
                            _radiance_Scales_EV_250_Aggr500_RefSB = ReadDataSetAttrToFloat(srcRaster,
                                "EV_250_Aggr500_RefSB", "reflectance_scales", 2);
                            _radiance_Offsets_EV_250_Aggr500_RefSB = ReadDataSetAttrToFloat(srcRaster,
                                "EV_250_Aggr500_RefSB", "reflectance_offsets", 2);
                        }

                        break;
                    case "EV_500_RefSB":
                        if (_radiance_Scales_EV_500_RefSB == null)
                        {
                            _radiance_Scales_EV_500_RefSB =
                                ReadDataSetAttrToFloat(srcRaster, "EV_500_RefSB", "reflectance_scales", 5);
                            _radiance_Offsets_EV_500_RefSB =
                                ReadDataSetAttrToFloat(srcRaster, "EV_500_RefSB", "reflectance_offsets", 5);
                        }

                        break;
                    case "EV_250_RefSB":
                        if (_radiance_Scales_EV_250_RefSB == null)
                        {
                            _radiance_Scales_EV_250_RefSB =
                                ReadDataSetAttrToFloat(srcRaster, "EV_250_RefSB", "reflectance_scales", 2);
                            _radiance_Offsets_EV_250_RefSB =
                                ReadDataSetAttrToFloat(srcRaster, "EV_250_RefSB", "reflectance_offsets", 2);
                        }

                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("获取亮温转换参数失败:" + ex.Message, ex.InnerException);
            }
        }

        private void ReadySolarZenithArgsToFile(AbstractWarpDataset srcRaster)
        {
            if (srcRaster == null)
                throw new ArgumentNullException("srcRaster", "获取太阳天顶角数据失败");
            try
            {
                Band[] rasterBands = srcRaster.GetBands("SolarZenith");
                Band rasterBand = rasterBands[0];
                Size bandSize = new Size(rasterBand.XSize, rasterBand.YSize);
                short[] readSolarZenithData = ReadBandToInt16(rasterBand);
                int length = readSolarZenithData.Length;
                float[] saveSolarZenithData = new float[length];
                Parallel.For(0, length, index =>
                {
                    if (readSolarZenithData[index] > 0 && readSolarZenithData[index] < 18000)
                        saveSolarZenithData[index] =
                            (float) (1.0f / Math.Cos(readSolarZenithData[index] * DEG_TO_RAD_P100));
                    else
                        saveSolarZenithData[index] = 0;
                });
                _solarZenithCacheRaster =
                    WriteData(saveSolarZenithData, _szDataFilename, bandSize.Width, bandSize.Height);
                saveSolarZenithData = null;
                readSolarZenithData = null;
            }
            catch (Exception ex)
            {
                throw new Exception("获取太阳天顶角数据失败", ex.InnerException);
            }
        }

        private float[] ReadDataSetAttrToFloat(AbstractWarpDataset srcbandpro, string dataSetName, string attrName,
            int length)
        {
            float[] value = new float[length];
            IDictionary<string, string> dsAtts = srcbandpro.GetDatasetAttributes(dataSetName);
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

        private float[] ReadFileAttributeToFloat(AbstractWarpDataset srcbandpro, string AttrName, int length)
        {
            float[] value = new float[length];
            Dictionary<string, string> dsAtts = srcbandpro.GetAttributes();
            string refSbCalStr = dsAtts[AttrName];
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

        private Int16[] ReadDataSetToInt16(AbstractWarpDataset srcbandpro, string dataSetName, int bandIndex)
        {
            Int16[] data = null;
            Band[] rasterBands = srcbandpro.GetBands(dataSetName);
            Band rasterBand = rasterBands[0];
            {
                data = new Int16[rasterBand.XSize * rasterBand.YSize];

                rasterBand.ReadRaster(0, 0, rasterBand.XSize, rasterBand.YSize, data,
                    rasterBand.XSize, rasterBand.YSize, 0, 0);
            }
            return data;
        }

        private Int16[] ReadBandToInt16(Band rasterBand)
        {
            Int16[] data = null;
            {
                data = new Int16[rasterBand.XSize * rasterBand.YSize];
                rasterBand.ReadRaster(0, 0, rasterBand.XSize, rasterBand.YSize, data,
                    rasterBand.XSize, rasterBand.YSize, 0, 0);
            }
            return data;
        }

        private float[] ReadDataSetToSingle(AbstractWarpDataset srcbandpro, Size srcSize, string dataSetName,
            int bandIndex)
        {
            Single[] data = new Single[srcSize.Width * srcSize.Height];
            Band[] rasterBands = srcbandpro.GetBands(dataSetName);
            Band rasterBand = rasterBands[0];
            {
                rasterBand.ReadRaster(0, 0, srcSize.Width, srcSize.Height, data, srcSize.Width, srcSize.Height,
                    0, 0);
            }
            (rasterBand as IDisposable).Dispose();
            return data;
        }

        #endregion

        /// <summary> 读取通道值</summary>
        private void ReadBandData(UInt16[] bandData, WarpDataset srcRaster, string bandName, int bandNumber,
            Size srcSize)
        {
            Band[] latBands = srcRaster.GetBands(bandName);
            Band latBand = latBands[bandNumber];
            {
                latBand.ReadRaster(0, 0, srcSize.Width, srcSize.Height, bandData, srcSize.Width, srcSize.Height,
                    0, 0);
            }
            (latBand as IDisposable).Dispose();
        }

        public override FilePrjSettings CreateDefaultPrjSettings()
        {
            return new FY3_VIRR_PrjSettings();
        }

        private void TryCreateDefaultArgsKM(AbstractWarpDataset srcRaster, EOS_MODIS_PrjSettings prjSettings,
            ref SpatialReference dstSpatialRef)
        {
            if (dstSpatialRef == null)
                dstSpatialRef = _srcSpatialRef;
            if (string.IsNullOrWhiteSpace(prjSettings.OutFormat))
                prjSettings.OutFormat = "LDF";

            if (dstSpatialRef.IsGeographic() == 1)
            {
                _srcImgResolution = 0.01F;
            }
            else
            {
                _srcImgResolution = 1000F;
            }

            if (prjSettings.OutResolutionX == 0 || prjSettings.OutResolutionY == 0)
            {
                if (dstSpatialRef.IsGeographic() == 1)
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

            SetPrjBand(prjSettings, PrjBand.MODIS_1000_Orbit);
        }

        private void SetPrjBand(EOS_MODIS_PrjSettings prjSettings, PrjBand[] defaultPrjBands)
        {
            if (prjSettings.OutBandNos == null || prjSettings.OutBandNos.Length == 0)
            {
                _prjBands = defaultPrjBands;
            }
            else
            {
                List<PrjBand> bands = new List<PrjBand>();
                PrjBand[] defbands = defaultPrjBands;
                foreach (int bandNo in prjSettings.OutBandNos)
                {
                    if (bandNo > defbands.Length || bandNo <= 0)
                        throw new Exception(string.Format("请求的波段号非法，文件波段数为[{0}],请求的波段号为[{1}]", defbands.Length,
                            bandNo));
                    bands.Add(defbands[bandNo - 1]);
                }

                _prjBands = bands.ToArray();
            }
        }

        private void TryCreateDefaultArgsHKM(AbstractWarpDataset srcRaster, EOS_MODIS_PrjSettings prjSettings,
            ref SpatialReference dstSpatialRef)
        {
            if (dstSpatialRef == null)
                dstSpatialRef = _srcSpatialRef;
            if (string.IsNullOrWhiteSpace(prjSettings.OutFormat))
                prjSettings.OutFormat = "LDF";
            if (dstSpatialRef.IsGeographic() == 1)
            {
                _srcImgResolution = 0.005F;
            }
            else
            {
                _srcImgResolution = 500F;
            }

            if (prjSettings.OutResolutionX == 0 || prjSettings.OutResolutionY == 0)
            {
                if (dstSpatialRef.IsGeographic() == 1)
                {
                    prjSettings.OutResolutionX = 0.005F;
                    prjSettings.OutResolutionY = 0.005F;
                }
                else
                {
                    prjSettings.OutResolutionX = 500F;
                    prjSettings.OutResolutionY = 500F;
                }
            }

            SetPrjBand(prjSettings, PrjBand.MODIS_500_Orbit);
        }

        private void TryCreateDefaultArgsQKM(AbstractWarpDataset srcRaster, EOS_MODIS_PrjSettings prjSettings,
            ref SpatialReference dstSpatialRef)
        {
            if (dstSpatialRef == null)
                dstSpatialRef = _srcSpatialRef;
            if (string.IsNullOrWhiteSpace(prjSettings.OutFormat))
                prjSettings.OutFormat = "LDF";
            if (dstSpatialRef.IsGeographic() == 1)
            {
                _srcImgResolution = 0.0025F;
            }
            else
            {
                _srcImgResolution = 250F;
            }

            if (prjSettings.OutResolutionX == 0 || prjSettings.OutResolutionY == 0)
            {
                if (dstSpatialRef.IsGeographic() == 1)
                {
                    prjSettings.OutResolutionX = 0.0025F;
                    prjSettings.OutResolutionY = 0.0025F;
                }
                else
                {
                    prjSettings.OutResolutionX = 250F;
                    prjSettings.OutResolutionY = 250F;
                }
            }

            SetPrjBand(prjSettings, PrjBand.MODIS_250_Orbit);
        }


        private string CheckFile(AbstractWarpDataset srcRaster)
        {
            _fileAliasName = TryGetAliasName(srcRaster);
            if (string.IsNullOrWhiteSpace(_fileAliasName) || (_fileAliasName != "Terra" && _fileAliasName != "Aqua"))
                throw new Exception("无法识别为EOS MODIS轨道数据的卫星标识，不能为其设定参数[" + _fileAliasName + "]");
            else
            {
                string[] datasets = srcRaster.DatasetNames.ToArray();
                if (datasets.Any(t => t.Contains("EV_1KM_RefSB")) &&
                    datasets.Any(t => t.Contains("EV_250_Aggr1km_RefSB")) &&
                    datasets.Any(t => t.Contains("EV_500_Aggr1km_RefSB")))
                    return "1000";
                else if (datasets.Any(t => t.Contains("EV_500_RefSB")) &&
                         datasets.Any(t => t.Contains("EV_250_Aggr500_RefSB")))
                    return "0500";
                else if (datasets.Any(t => t.Contains("EV_250_RefSB")))
                    return "0250";
                else
                    throw new Exception("无法辨认该EOS MODIS数据类型");
            }
        }

        private string TryGetAliasName(AbstractWarpDataset srcDs)
        {
            Dictionary<string, string> filaAttrs = srcDs.GetAttributes();
            var rInfo = mRasterSourceManager.GetInstance().GetRasterDatasetInfo(srcDs.fileName);
            if (filaAttrs != null && filaAttrs.ContainsKey("Satellite"))
            {
                return filaAttrs["Satellite"];
            }
            else if (rInfo != null)
            {
                if (rInfo.SatelliteID == "EOST" || rInfo.SatelliteID == "TERRA")
                    return "Terra";
                else if (rInfo.SatelliteID == "EOSA" || rInfo.SatelliteID == "AQUA")
                    return "Aqua";
            }

            throw new Exception("无法识别为EOS MODIS轨道数据，无法确定是Terra还是Aqua，无法设定参数");
        }

        public override void ComputeDstEnvelope(AbstractWarpDataset loncationRaster, SpatialReference dstSpatialRef,
            out PrjEnvelope maxPrjEnvelope, Action<int, string> progressCallback)
        {
            if (loncationRaster != null)
            {
                Size LoncationSize;
                double[] xs, ys;
                ReadyLocations(loncationRaster, dstSpatialRef, out xs, out ys, out maxPrjEnvelope, out LoncationSize,
                    progressCallback);
            }
            else
            {
                maxPrjEnvelope = PrjEnvelope.Empty;
            }
        }

        private void ReadBandData(out UInt16[] bandData, AbstractWarpDataset srcRaster, string bandName, int bandNumber,
            Size srcSize)
        {
            Band[] latBands = srcRaster.GetBands(bandName);
            Band latBand = latBands[bandNumber];
            {
                bandData = new UInt16[latBand.XSize * latBand.YSize];

                latBand.ReadRaster(0, 0, latBand.XSize, latBand.YSize, bandData, latBand.XSize,
                    latBand.YSize, 0, 0);
            }
            (latBand as IDisposable).Dispose();
        }

        protected override void DoRadiation(AbstractWarpDataset srcImgRaster, int i, ushort[] srcBandData,
            float[] solarZenithData, Size srcBlockImgSize, Size angleSize)
        {
            if (_isRadRef)
            {
                string ds = _prjBands[i].DataSetName;
                int dsIndex = _prjBands[i].DataSetIndex;
                DoRadiation(srcBandData, srcBlockImgSize, angleSize, ds, dsIndex, srcImgRaster, _isRadRef,
                    _isSolarZenith, solarZenithData);
            }
        }

        public override bool HasVaildEnvelope(AbstractWarpDataset geoRaster, PrjEnvelope validEnv,
            SpatialReference dstSpatialRef)
        {
            if (geoRaster == null)
                throw new ArgumentNullException("locationRaster", "参数[经纬度数据文件]不能为空");

            double[] xs = null;
            double[] ys = null;
            Size geoSize;
            Size maxGeoSize = new Size(1024, 1024);
            Band longitudeBand, latitudeBand;
            ReadModisLocations(geoRaster, out longitudeBand, out latitudeBand); //GetGeoBand
            Size srcLocationSize = new Size(longitudeBand.XSize, longitudeBand.YSize);
            ReadLocations(longitudeBand, latitudeBand, maxGeoSize, out xs, out ys, out geoSize);
            return _rasterProjector.HasVaildEnvelope(xs, ys, validEnv, null, dstSpatialRef);
        }

        private void ReadModisLocations(AbstractWarpDataset geoRaster, out Band longitudeBand, out Band latitudeBand)
        {
            //HDF4_EOS:EOS_SWATH:"E:\RSDATA\Modis\L1\EOST_MODIS_L1_20190120_1114_MOD03.HDF":MODIS_Swath_Type_GEO:SensorAzimuth
            //MODIS_SWATH_Type_L1B
            //Longitude Latitude
            longitudeBand = null;
            latitudeBand = null;
            string lonDsName = $"HDF4_EOS:EOS_SWATH:\"{geoRaster.fileName}\":MODIS_Swath_Type_GEO:Longitude";
            string latDsName = $"HDF4_EOS:EOS_SWATH:\"{geoRaster.fileName}\":MODIS_Swath_Type_GEO:Latitude";
            Dataset lonDs = null, latDs = null;
            try
            {
                lonDs = Gdal.Open(lonDsName, Access.GA_ReadOnly);
                latDs = Gdal.Open(latDsName, Access.GA_ReadOnly);
                if (lonDs != null && latDs != null)
                {
                    longitudeBand = lonDs.GetRasterBand(1);
                    latitudeBand = latDs.GetRasterBand(1);
                    return;
                }
            }
            catch (Exception e)
            {
                lonDs = null;
                latDs = null;
            }

            lonDsName = $"HDF4_EOS:EOS_SWATH:\"{geoRaster.fileName}\":MODIS_SWATH_Type_L1B:Longitude";
            latDsName = $"HDF4_EOS:EOS_SWATH:\"{geoRaster.fileName}\":MODIS_SWATH_Type_L1B:Latitude";
            try
            {
                lonDs = Gdal.Open(lonDsName, Access.GA_ReadOnly);
                latDs = Gdal.Open(latDsName, Access.GA_ReadOnly);
                if (lonDs != null && latDs != null)
                {
                    longitudeBand = lonDs.GetRasterBand(1);
                    latitudeBand = latDs.GetRasterBand(1);
                    return;
                }
            }
            catch (Exception e)
            {
                lonDs = null;
                latDs = null;
            }
        }
    }
}