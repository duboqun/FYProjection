using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.IO;
using PIE.Meteo.FileProject;
using PIE.Meteo.RasterProject;
using PIE.Meteo.FileProject.DF.NOAA;
using System.Collections.Concurrent;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace PIE.Meteo.FileProject
{
    public class NOAA_1BDFileProjector : FileProjector
    {
        private static string vrtContent =
            "<VRTDataset rasterXSize=\"51\" rasterYSize=\"{HEIGHT}\">\n  <VRTRasterBand dataType=\"Float64\" band=\"1\">\n    <Description>GEOLOC X</Description>\n    <NoDataValue>-200</NoDataValue>\n    <SimpleSource>\n      <SourceFilename relativeToVRT=\"0\">L1BGCPS:{FILEPATH}</SourceFilename>\n      <SourceBand>1</SourceBand>\n      <SourceProperties RasterXSize=\"51\" RasterYSize=\"{HEIGHT}\" DataType=\"Float64\" BlockXSize=\"51\" BlockYSize=\"1\" />\n      <SrcRect xOff=\"0\" yOff=\"0\" xSize=\"51\" ySize=\"{HEIGHT}\" />\n      <DstRect xOff=\"0\" yOff=\"0\" xSize=\"51\" ySize=\"{HEIGHT}\" />\n    </SimpleSource>\n  </VRTRasterBand>\n  <VRTRasterBand dataType=\"Float64\" band=\"2\">\n    <Description>GEOLOC Y</Description>\n    <NoDataValue>-200</NoDataValue>\n    <SimpleSource>\n      <SourceFilename relativeToVRT=\"0\">L1BGCPS:{FILEPATH}</SourceFilename>\n      <SourceBand>2</SourceBand>\n      <SourceProperties RasterXSize=\"51\" RasterYSize=\"{HEIGHT}\" DataType=\"Float64\" BlockXSize=\"51\" BlockYSize=\"1\" />\n      <SrcRect xOff=\"0\" yOff=\"0\" xSize=\"51\" ySize=\"{HEIGHT}\" />\n      <DstRect xOff=\"0\" yOff=\"0\" xSize=\"51\" ySize=\"{HEIGHT}\" />\n    </SimpleSource>\n  </VRTRasterBand>\n    <VRTRasterBand dataType=\"Float32\" band=\"1\">\n    <Description>Solar zenith angles</Description>\n    <SimpleSource>\n      <SourceFilename relativeToVRT=\"0\">L1B_ANGLES:{FILEPATH}</SourceFilename>\n      <SourceBand>1</SourceBand>\n      <SourceProperties RasterXSize=\"51\" RasterYSize=\"{HEIGHT}\" DataType=\"Float32\" BlockXSize=\"51\" BlockYSize=\"1\" />\n      <SrcRect xOff=\"0\" yOff=\"0\" xSize=\"51\" ySize=\"{HEIGHT}\" />\n      <DstRect xOff=\"0\" yOff=\"0\" xSize=\"51\" ySize=\"{HEIGHT}\" />\n    </SimpleSource>\n  </VRTRasterBand>\n  <VRTRasterBand dataType=\"Float32\" band=\"2\">\n    <Description>Satellite zenith angles</Description>\n    <SimpleSource>\n      <SourceFilename relativeToVRT=\"0\">L1B_ANGLES:{FILEPATH}</SourceFilename>\n      <SourceBand>2</SourceBand>\n      <SourceProperties RasterXSize=\"51\" RasterYSize=\"{HEIGHT}\" DataType=\"Float32\" BlockXSize=\"51\" BlockYSize=\"1\" />\n      <SrcRect xOff=\"0\" yOff=\"0\" xSize=\"51\" ySize=\"{HEIGHT}\" />\n      <DstRect xOff=\"0\" yOff=\"0\" xSize=\"51\" ySize=\"{HEIGHT}\" />\n    </SimpleSource>\n  </VRTRasterBand>\n  <VRTRasterBand dataType=\"Float32\" band=\"3\">\n    <Description>Relative azimuth angles</Description>\n    <SimpleSource>\n      <SourceFilename relativeToVRT=\"0\">L1B_ANGLES:{FILEPATH}</SourceFilename>\n      <SourceBand>3</SourceBand>\n      <SourceProperties RasterXSize=\"51\" RasterYSize=\"{HEIGHT}\" DataType=\"Float32\" BlockXSize=\"51\" BlockYSize=\"1\" />\n      <SrcRect xOff=\"0\" yOff=\"0\" xSize=\"51\" ySize=\"{HEIGHT}\" />\n      <DstRect xOff=\"0\" yOff=\"0\" xSize=\"51\" ySize=\"{HEIGHT}\" />\n    </SimpleSource>\n  </VRTRasterBand>\n</VRTDataset>";

        private const double DEG_TO_RAD_P100 = 0.000174532925199432955; // (PI/180)/100;
        private const double C2 = 1.438833;
        private const double C1 = 1.1910659 / 100000;

        #region 定标变量

        /// <summary>
        /// 反射通道定标系数（业务用，通道1、2、3A），存储为：[行,3*5]//scale1，offset1，scale2，offset2，inflection
        /// band1业务用 斜率1、截距1、斜率2、截距2、交叉点
        /// band1测试用 斜率1、截距1、斜率2、截距2、交叉点
        /// band1发射前 斜率1、截距1、斜率2、截距2、交叉点
        /// band2业务用...
        /// band2测试用...
        /// band2发射前...
        /// 3A   业务用...
        /// </summary>
        private double[,] _refSB_Coeff = null;

        /// <summary>
        /// 发射通道定标系数（业务用，通道3B、4、5），存储为：[行,3*3] scale1，scale2，offset
        /// 3B业务用 scale1，scale2，offset
        /// 3B测试用 scale1，scale2，offset
        /// 3B发射前 scale1，scale2，offset
        /// 4 业务用 scale1，scale2，offset
        /// 4 测试用 scale1，scale2，offset
        /// 4 发射前 scale1，scale2，offset
        /// 5 业务用 scale1，scale2，offset
        /// 5 测试用 scale1，scale2，offset
        /// 5 发射前 scale1，scale2，offset
        /// </summary>
        private double[,] _emissive_Radiance_Coeff = null;

        /// <summary>
        /// 发射通道A、B、V系数
        /// 3B A、B、V
        /// 4  A、B、V
        /// 5  A、B、V
        /// </summary>
        private float[] _emmisive_BT_Coefficients = null;

        private bool _isDay = false; //白天

        #endregion

        SpatialReference _srcSpatialRef = null;

        //private Block _orbitBlock = null;              //当前投影范围，需要使用的原始轨道数据最小范围
        private AbstractWarpDataset _outLdfDriver = null;
        private string _szDataFilename;

        private Dataset _vrtDataset = null;
        //private WarpDataset _solarZenithCacheRaster = null; 
        //protected short[] _sensorZenithData = null;

        //#region Session
        //PrjEnvelope _maxPrjEnvelope = null;
        //double[] _xs = null;    //存储的实际是计算后的值
        //double[] _ys = null;    //存储的实际是计算后的值
        //#endregion

        public NOAA_1BDFileProjector()
            : base()
        {
            _name = "NOAA_1BD";
            _fullname = "NOAA_1BD轨道文件投影";
            _rasterProjector = new RasterProjector();
            _srcSpatialRef = SpatialReferenceFactory.CreateSpatialReference(4326);
            _supportAngles = new string[] {"SolarZenith", "SatelliteZenith", "RelativeAzimuth"};
        }

        public override bool IsSupport(string fileName)
        {
            return false;
        }

        public override void Project(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings,
            SpatialReference dstSpatialRef, Action<int, string> progressCallback)
        {
            if (srcRaster == null)
                throw new ArgumentNullException("srcRaster");
            if (prjSettings == null)
                throw new ArgumentNullException("prjSettings");
            if (progressCallback != null)
                progressCallback(0, "准备相关参数");
            _dstSpatialRef = dstSpatialRef;
            if (prjSettings.OutEnvelope == null || prjSettings.OutEnvelope == PrjEnvelope.Empty)
            {
                MemoryHelper.MemoryNeed(200, 1536);
            }
            else
            {
                MemoryHelper.MemoryNeed(200, 1536); //剩余900MB,已使用1.2GB
            }

            try
            {
                NOAA_PrjSettings noaaPrjSettings = prjSettings as NOAA_PrjSettings;
                TryCreateDefaultArgs(srcRaster, noaaPrjSettings, ref dstSpatialRef);
                _prjSettings = prjSettings;
                _isSensorZenith = noaaPrjSettings.IsSensorZenith;
                DoSession(srcRaster, dstSpatialRef, noaaPrjSettings, progressCallback);
                if (prjSettings.OutEnvelope == null || prjSettings.OutEnvelope == PrjEnvelope.Empty)
                {
                    prjSettings.OutEnvelope = _maxPrjEnvelope;
                    _orbitBlock = new Block
                        {xBegin = 0, yBegin = 0, xEnd = srcRaster.Width - 1, yEnd = srcRaster.Height - 1};
                }
                else
                {
                    GetEnvelope(_xs, _ys, srcRaster.Width, srcRaster.Height, prjSettings.OutEnvelope, out _orbitBlock);
                    if (_orbitBlock == null || _orbitBlock.Width <= 0 || _orbitBlock.Height <= 0)
                        throw new Exception("数据不在目标区间内");
                    float invalidPresent = (_orbitBlock.Width * _orbitBlock.Height * 1.0F) /
                                           (srcRaster.Width * srcRaster.Height);
                    if (invalidPresent < 0.0001f)
                        throw new Exception("数据占轨道数据比例太小，有效率" + invalidPresent * 100 + "%");
                    if (invalidPresent > 0.60)
                        _orbitBlock = new Block
                            {xBegin = 0, yBegin = 0, xEnd = srcRaster.Width - 1, yEnd = srcRaster.Height - 1};
                }

                if (dstSpatialRef.IsGeographic() == 1 && _maxPrjEnvelope.MaxX > 180 && _maxPrjEnvelope.MinX < -180 &&
                    _maxPrjEnvelope.MaxY > 90 && _maxPrjEnvelope.MinY < -90)
                    throw new Exception("读取NOAA 1bd经纬度不在合理范围内[" + _maxPrjEnvelope.ToString() + "]");
                if (dstSpatialRef.IsGeographic() == 1 &&
                    (prjSettings.OutEnvelope.MaxY > 80 || prjSettings.OutEnvelope.MaxY < -80))
                    throw new Exception(string.Format("高纬度数据[>80]，不适合投影为等经纬度数据[{0}]", _maxPrjEnvelope));
                PrjEnvelope envelops = prjSettings.OutEnvelope;
                if (!envelops.IntersectsWith(_maxPrjEnvelope))
                    throw new Exception("数据不在目标区间内");
                float outResolutionX = prjSettings.OutResolutionX;
                float outResolutionY = prjSettings.OutResolutionY;
                int dstBandCount = prjSettings.OutBandNos.Length;
                Size outSize = prjSettings.OutSize;
                string[] angleOptions = new string[]
                {
                    "INTERLEAVE=BSQ",
                    "VERSION=LDF",
                    "WITHHDR=TRUE",
                    "SPATIALREF=" + dstSpatialRef.ExportToProj4(),
                    "MAPINFO={" + 1 + "," + 1 + "}:{" + prjSettings.OutEnvelope.MinX + "," +
                    prjSettings.OutEnvelope.MaxY + "}:{" + outResolutionX + "," + outResolutionY + "}"
                };
                string outfilename = prjSettings.OutPathAndFileName;
                ReadyAngleFiles(srcRaster, outfilename, prjSettings, outSize, angleOptions);
                ProjectLocal(srcRaster, noaaPrjSettings, dstSpatialRef, progressCallback);
            }
            catch
            {
                EndSession();
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

        public override void EndSession()
        {
            base.EndSession();
            _xs = null;
            _ys = null;
            _refSB_Coeff = null;
            _emissive_Radiance_Coeff = null;
            _emmisive_BT_Coefficients = null;
            if (_solarZenithCacheRaster != null)
            {
                _solarZenithCacheRaster.Dispose();
                _solarZenithCacheRaster = null;
            }

            if (_outLdfDriver != null)
            {
                _outLdfDriver.Dispose();
                _outLdfDriver = null;
            }

            if (_vrtDataset != null)
            {
                (_vrtDataset as IDisposable).Dispose();
            }

            if (_prjSettings.IsClearPrjCache)
                TryDeleteCurCatch();
        }

        private void DoSession(AbstractWarpDataset srcRaster, SpatialReference dstSpatialRef,
            NOAA_PrjSettings prjSettings, Action<int, string> progressCallback)
        {
            if (_curSession == null || _curSession != srcRaster || _isBeginSession)
            {
                string vrtPath = GetCacheFilename(srcRaster.fileName, "ext.vrt");
                if (!File.Exists(vrtPath))
                    File.WriteAllText(vrtPath,
                        vrtContent.Replace("{FILEPATH}", srcRaster.fileName)
                            .Replace("{HEIGHT}", srcRaster.Height.ToString()));
                if (_vrtDataset == null)
                    _vrtDataset = Gdal.Open(vrtPath, Access.GA_ReadOnly);
                Size srcSize = new Size(srcRaster.Width, srcRaster.Height);
                ReadyLocations(srcRaster, dstSpatialRef, srcSize, out _xs, out _ys, out _maxPrjEnvelope,
                    progressCallback);
                if (progressCallback != null)
                    progressCallback(4, "准备亮温计算参数");
                if (prjSettings.IsRadiation)
                    ReadyRadiationArgs(srcRaster);
                if (progressCallback != null)
                    progressCallback(5, "准备亮温计算参数");
                if (prjSettings.IsSolarZenith && prjSettings.IsRadiation)
                {
                    _szDataFilename = GetSolarZenithCacheFilename(srcRaster.fileName);
                    if (!File.Exists(_szDataFilename))
                        ReadySolarZenithArgsToFile(srcRaster);
                    else
                        _solarZenithCacheRaster = WarpDataset.Open(_szDataFilename) as WarpDataset;
                    if (prjSettings.IsSensorZenith)
                    {
                    }
                }

                _isBeginSession = false;
            }
        }

        private void ProjectLocal(AbstractWarpDataset srcRaster, NOAA_PrjSettings prjSettings,
            SpatialReference dstSpatialRef, Action<int, string> progressCallback)
        {
            PrjEnvelope envelops = prjSettings.OutEnvelope;
            if (envelops.IntersectsWith(_maxPrjEnvelope))
            {
                ProjectToLDF(srcRaster, prjSettings, dstSpatialRef, progressCallback);
            }
            else
            {
                throw new Exception("数据不在目标区间内");
            }
        }

        /// <summary>
        /// 0、先生成目标文件，以防止目标空间不足。
        /// 1、计算查找表
        /// 2、读取通道数据
        /// 3、计算通道数据亮温
        /// 4、投影通道。
        /// </summary>
        private void ProjectToLDF(AbstractWarpDataset srcRaster, NOAA_PrjSettings prjSettings,
            SpatialReference dstSpatialRef, Action<int, string> progressCallback)
        {
            string outFormat = prjSettings.OutFormat;
            string outfilename = prjSettings.OutPathAndFileName;
            string dstProj4 = dstSpatialRef.ExportToProj4();
            int[] outBandNos = prjSettings.OutBandNos;
            int dstBandCount = outBandNos.Length;
            Size srcSize = new Size(srcRaster.Width, srcRaster.Height);
            Size dstSize = prjSettings.OutSize;
            Size srcJdSize = srcSize;
            {
                PrjEnvelope dstEnvelope = prjSettings.OutEnvelope;

                List<string> opts = new List<string>();
                opts.AddRange(new string[]
                {
                    "INTERLEAVE=BSQ",
                    "VERSION=LDF",
                    "WITHHDR=TRUE",
                    "SPATIALREF=" + dstSpatialRef.ExportToProj4(),
                    "MAPINFO={" + 1 + "," + 1 + "}:{" + dstEnvelope.MinX + "," + dstEnvelope.MaxY + "}:{" +
                    prjSettings.OutResolutionX + "," + prjSettings.OutResolutionY + "}",
                    "BANDNAMES=" + BandNameString(prjSettings.OutBandNos),
                    "SENSOR=AVHRR"
                });
                if (srcRaster.DataIdentify != null)
                {
                    string satellite = srcRaster.DataIdentify.Satellite;
                    DateTime dt = srcRaster.DataIdentify.OrbitDateTime;
                    bool asc = srcRaster.DataIdentify.IsAscOrbitDirection;
                    if (!string.IsNullOrWhiteSpace(satellite))
                    {
                        opts.Add("SATELLITE=" + satellite);
                    }

                    if (dt != DateTime.MinValue && dt != DateTime.MaxValue)
                        opts.Add("DATETIME=" + dt.ToString("yyyy/MM/dd HH:mm"));
                    opts.Add("ORBITDIRECTION=" + (asc ? "ASC" : "DESC"));
                }

                if (progressCallback != null)
                    progressCallback(6, "生成输出文件");
                var dstDs = DatasetFactory.CreateRasterDataset(outfilename, dstSize.Width, dstSize.Height, dstBandCount,
                    DataType.GDT_UInt16, GenericFilename.GetDriverName(prjSettings.OutFormat), null);
                dstDs.SetProjection(dstSpatialRef.ExportToWkt());

                dstDs.SetGeoTransform(new double[]
                {
                    dstEnvelope.MinX, Convert.ToDouble(prjSettings.OutResolutionX.ToString("f6")), 0,
                    dstEnvelope.MaxY, 0, -Convert.ToDouble(prjSettings.OutResolutionY.ToString("f6"))
                });
                WarpDataset prdWriter = new WarpDataset(dstDs, outfilename);
                {
                    float outResolutionX = prjSettings.OutResolutionX;
                    float outResolutionY = prjSettings.OutResolutionY;
                    Size outSize = dstEnvelope.GetSize(outResolutionX, outResolutionY);
                    int blockXNum = 0;
                    int blockYNum = 0;
                    int blockWidth = 0;
                    int blockHeight = 0;
                    GetBlockNumber(outSize, out blockXNum, out blockYNum, out blockWidth, out blockHeight);
                    Size dstBlockSize = new Size(blockWidth, blockHeight);
                    UInt16[] dstRowBlockLUT = new UInt16[blockWidth * blockHeight];
                    UInt16[] dstColBlockLUT = new UInt16[blockWidth * blockHeight];
                    int blockCount = blockYNum * blockXNum;
                    progress = 0;
                    progressCount = blockCount * (dstBandCount + (_angleBands == null ? 0 : _angleBands.Length));
                    percent = 0;
                    for (int blockYIndex = 0; blockYIndex < blockYNum; blockYIndex++)
                    {
                        for (int blockXIndex = 0; blockXIndex < blockXNum; blockXIndex++)
                        {
                            PrjEnvelope blockEnvelope = null;
                            Block orbitBlock = null; //经纬度数据集，计算轨道数据范围偏移
                            double[] blockOrbitXs = null;
                            double[] blockOrbitYs = null;
                            if (blockCount == 1) //没分块的情况
                            {
                                orbitBlock = _orbitBlock;
                                if (_orbitBlock.Width == srcJdSize.Width && _orbitBlock.Height == srcJdSize.Height)
                                {
                                    blockOrbitXs = _xs;
                                    blockOrbitYs = _ys;
                                }
                                else //源
                                {
                                    GetBlockDatas(_xs, _ys, srcJdSize.Width, srcJdSize.Height, orbitBlock.xBegin,
                                        orbitBlock.yBegin, orbitBlock.Width, orbitBlock.Height, out blockOrbitXs,
                                        out blockOrbitYs);
                                }

                                blockEnvelope = dstEnvelope;
                            }
                            else
                            {
                                //当前块的四角范围
                                double blockMinX = dstEnvelope.MinX + blockWidth * outResolutionX * blockXIndex;
                                double blockMaxX = blockMinX + blockWidth * outResolutionX;
                                double blockMaxY = dstEnvelope.MaxY - blockHeight * outResolutionY * blockYIndex;
                                double blockMinY = blockMaxY - blockHeight * outResolutionY;
                                blockEnvelope = new PrjEnvelope(blockMinX, blockMaxX, blockMinY, blockMaxY,
                                    dstSpatialRef);
                                //根据当前输出块，反推出对应的源数据块起始行列
                                GetEnvelope(_xs, _ys, srcJdSize.Width, srcJdSize.Height, blockEnvelope, out orbitBlock);
                                if (orbitBlock.Width <= 0 || orbitBlock.Height <= 0) //当前分块不在图像内部
                                {
                                    progress += dstBandCount;
                                    continue;
                                }

                                GetBlockDatas(_xs, _ys, srcJdSize.Width, srcJdSize.Height, orbitBlock.xBegin,
                                    orbitBlock.yBegin, orbitBlock.Width, orbitBlock.Height, out blockOrbitXs,
                                    out blockOrbitYs);
                            }

                            float[] solarZenithData = null;
                            if (prjSettings.IsRadiation && prjSettings.IsSolarZenith)
                            {
                                if (File.Exists(_szDataFilename))
                                    ReadBandData(out solarZenithData, _solarZenithCacheRaster, 0, orbitBlock.xBegin,
                                        orbitBlock.yBegin, orbitBlock.Width, orbitBlock.Height);
                                TryReadZenithData(orbitBlock.xBegin, orbitBlock.yBegin, orbitBlock.Width,
                                    orbitBlock.Height, srcSize);
                            }

                            Size orbitBlockSize = new Size(orbitBlock.Width, orbitBlock.Height);
                            _rasterProjector.ComputeIndexMapTable(blockOrbitXs, blockOrbitYs, orbitBlockSize,
                                dstBlockSize, blockEnvelope, _maxPrjEnvelope, out dstRowBlockLUT, out dstColBlockLUT,
                                null);
                            //执行投影
                            UInt16[] srcBandData = new UInt16[orbitBlock.Width * orbitBlock.Height];
                            UInt16[] dstBandData = new UInt16[blockWidth * blockHeight];
                            for (int i = 0; i < dstBandCount; i++) //读取原始通道值，投影到目标区域
                            {
                                if (progressCallback != null)
                                {
                                    progress++;
                                    percent = progress * 100 / progressCount;
                                    progressCallback(percent, string.Format("投影完成{0}%", percent));
                                }

                                int bandNo = outBandNos[i];
                                ReadBandData(srcBandData, srcRaster, bandNo - 1, orbitBlock.xBegin, orbitBlock.yBegin,
                                    orbitBlock.Width, orbitBlock.Height);
                                if (prjSettings.IsRadiation)
                                {
                                    DoRadiation(srcBandData, orbitBlock.xBegin, orbitBlock.yBegin, orbitBlock.Size,
                                        bandNo, prjSettings.IsRadiation, prjSettings.IsSolarZenith, solarZenithData);
                                }

                                _rasterProjector.Project<UInt16>(srcBandData, orbitBlock.Size, dstRowBlockLUT,
                                    dstColBlockLUT, dstBlockSize, dstBandData, 0, null);
                                Band band = prdWriter.GetRasterBand(i);
                                {
                                    int blockOffsetY = blockYIndex * dstBlockSize.Height;
                                    int blockOffsetX = blockXIndex * dstBlockSize.Width;
                                    band.WriteRaster(blockOffsetX, blockOffsetY, blockWidth, blockHeight, dstBandData,
                                        blockWidth, blockHeight, 0, 0);
                                }
                            }

                            ReleaseZenithData();
                            srcBandData = null;
                            dstBandData = null;
                            blockOrbitXs = null;
                            blockOrbitYs = null;
                            Size srcBufferSize = new Size(orbitBlock.Width, orbitBlock.Height);
                            ProjectAngle(dstBlockSize, srcBufferSize, blockWidth, blockHeight, blockYIndex, blockXIndex,
                                orbitBlock, dstRowBlockLUT, dstColBlockLUT, progressCallback);
                        }
                    }

                    dstRowBlockLUT = null;
                    dstColBlockLUT = null;
                }
                prdWriter.Dispose();
            }
        }

        protected short[] _sensorZenithData = null;

        protected void TryReadZenithData(int xOffset, int yOffset, int blockWidth, int blockHeight, Size srcSize)
        {
            //ToDo 亮温临边变暗订正,读取卫星天顶角数据。
            if (_isSensorZenith && _vrtDataset != null)
            {
                var sensorBand = _vrtDataset.GetRasterBand(4);
                double[] interpol = null;
                L1BInterpol.InterpolBand(out interpol, sensorBand, srcSize);
                var memDs = DatasetFactory.CreateRasterDataset("", srcSize.Width, srcSize.Height, 1,
                    DataType.GDT_Float32, "MEM", null);
                for (int i = 0; i < interpol.Length; i++)
                {
                    interpol[i] = interpol[i] * 100;
                }

                memDs.GetRasterBand(0).WriteRaster(0, 0, srcSize.Width, srcSize.Height, interpol, srcSize.Width,
                    srcSize.Height, 0, 0);
                _sensorZenithData = ReadBandData(memDs.GetRasterBand(0), xOffset, yOffset, blockWidth, blockHeight);
                (memDs as IDisposable).Dispose();
            }
        }

        protected override void ReleaseZenithData()
        {
            _sensorZenithData = null;
        }

        private void GetBlockNumber(Size size, out int blockXNum, out int blockYNum, out int blockWidth,
            out int blockHeight)
        {
            int w = size.Width;
            int h = size.Height;
            blockXNum = 1;
            blockYNum = 1;
            blockWidth = w;
            blockHeight = h;
            int MaxX = 7000;
            int MaxY = 2000;
            ulong mem = MemoryHelper.GetAvalidPhyMemory(); //系统剩余内存
            long workingSet64 = MemoryHelper.WorkingSet64(); //为该进程已分配内存
            if (mem < 200 * 1024.0f * 1024)
                throw new Exception("当前系统资源不足以完成该操作，请释放部分资源后再试。");
            double usemem = mem; //;

            MaxY = (int) (usemem / 100 / w);
            MaxY = MaxY < 2000 ? 2000 : MaxY;
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
        }

        private void ReadBandData(UInt16[] bandData, AbstractWarpDataset srcRaster, int bandNumber, int xOffset,
            int yOffset, int blockWidth, int blockHeight)
        {
            Band latBand = srcRaster.GetRasterBand(bandNumber);
            {
                latBand.ReadRaster(xOffset, yOffset, blockWidth, blockHeight, bandData, blockWidth, blockHeight, 0, 0);
            }
        }

        private void ReadBandData(UInt16[] bandData, Band latBand, int xOffset, int yOffset, int blockWidth,
            int blockHeight)
        {
            try
            {
                latBand.ReadRaster(xOffset, yOffset, blockWidth, blockHeight, bandData, blockWidth, blockHeight, 0, 0);
            }
            finally
            {
            }
        }

        /// <summary> 
        /// 辐射值计算
        /// </summary>
        private void DoRadiation(ushort[] srcBandData, int xoffset, int yoffset, Size srcSize, int bandNumber,
            bool isRadiation, bool isSolarZenith, float[] solarZenithData)
        {
            if (!isRadiation)
                return;
            switch (bandNumber)
            {
                case 1:
                case 2: //1,2
                    RefSBRadiation(srcBandData, yoffset, srcSize, bandNumber - 1, isSolarZenith, solarZenithData);
                    break;
                case 3: //第三个通道必须判断是3A还是3B
                    if (_isDay) //3A 按可见光
                        RefSBRadiation(srcBandData, yoffset, srcSize, bandNumber - 1, isSolarZenith, solarZenithData);
                    else //3B 近红外
                        EmissiveRadiance3B(srcBandData, yoffset, srcSize, bandNumber - 3);
                    break;
                case 4:
                case 5:
                    EmissiveRadiance(srcBandData, yoffset, srcSize, bandNumber - 3);
                    break;
                default:
                    break;
            }
        }

        private void EmissiveRadiance3B(ushort[] srcBandData, int yoffset, Size srcSize, int coefIndex)
        {
            float A = _emmisive_BT_Coefficients[0];
            float B = _emmisive_BT_Coefficients[1];
            float v = _emmisive_BT_Coefficients[2];
            double v3 = v * v * v;
            int height = srcSize.Height;
            int width = srcSize.Width;
            int beginY = 0;
            int endY = height;
            Parallel.For(beginY, endY, (rowIndex) =>
            {
                double scale2 = _emissive_Radiance_Coeff[rowIndex + yoffset, coefIndex * 3 + 1];
                double offset = _emissive_Radiance_Coeff[rowIndex + yoffset, coefIndex * 3 + 2];
                double radiation;
                int index;
                for (int j = 0; j < width; j++)
                {
                    index = rowIndex * width + j;
                    radiation = (scale2 * srcBandData[index] + offset);
                    srcBandData[index] = (UInt16) (10 * (C2 * v / Math.Log(1 + C1 * v3 / radiation) - A) / B);
                }
            });
        }

        private void EmissiveRadiance(ushort[] srcBandData, int yoffset, Size srcSize, int coefIndex)
        {
            float A = _emmisive_BT_Coefficients[coefIndex * 3];
            float B = _emmisive_BT_Coefficients[coefIndex * 3 + 1];
            float v = _emmisive_BT_Coefficients[coefIndex * 3 + 2];
            double v3 = v * v * v;
            int height = srcSize.Height;
            int width = srcSize.Width;
            int beginY = 0;
            int endY = height;
            Parallel.For(beginY, endY, (rowIndex) =>
            {
                double scale = _emissive_Radiance_Coeff[rowIndex + yoffset, coefIndex * 3];
                double scale2 = _emissive_Radiance_Coeff[rowIndex + yoffset, coefIndex * 3 + 1];
                double offset = _emissive_Radiance_Coeff[rowIndex + yoffset, coefIndex * 3 + 2];
                double radiation;
                int index;

                double temperatureBB;
                double sensorZenith;
                double deltaT;
                for (int j = 0; j < width; j++)
                {
                    index = rowIndex * width + j;
                    radiation = (scale * srcBandData[index] * srcBandData[index] + scale2 * srcBandData[index] +
                                 offset);
                    temperatureBB = (C2 * v / Math.Log(1 + C1 * v3 / radiation) - A) / B;
                    //"临边变暗订正"。
                    if (_isSensorZenith && _sensorZenithData != null)
                    {
                        sensorZenith = _sensorZenithData[index] * 0.01d;
                        deltaT = temperatureBB + (Math.Pow(Math.E, 0.00012 * sensorZenith * sensorZenith) - 1) *
                                 (0.1072 * temperatureBB - 26.81);
                        srcBandData[index] = (UInt16) (deltaT * 10);
                    }
                    else
                        srcBandData[index] = (UInt16) (temperatureBB * 10);
                }
            });
        }

        private void RefSBRadiation(ushort[] srcBandData, int yoffset, Size srcSize, int bandIndex, bool isSolarZenith,
            float[] solarZenithData)
        {
            int width = srcSize.Width;
            int height = srcSize.Height;
            if (isSolarZenith)
            {
                int beginY = 0;
                int endY = height;
                Parallel.For(beginY, endY, (rowIndex) => //Noaa每行一套参数
                {
                    double scale = _refSB_Coeff[rowIndex + yoffset, bandIndex * 5];
                    double offSet = _refSB_Coeff[rowIndex + yoffset, bandIndex * 5 + 1];
                    double scale2 = _refSB_Coeff[rowIndex + yoffset, bandIndex * 5 + 2];
                    double offSet2 = _refSB_Coeff[rowIndex + yoffset, bandIndex * 5 + 3];
                    double inflection = _refSB_Coeff[rowIndex + yoffset, bandIndex * 5 + 4];
                    double radiation = 0;
                    int index = 0;
                    for (int j = 0; j < width; j++)
                    {
                        index = rowIndex * width + j;
                        if (0 < srcBandData[index] && srcBandData[index] < inflection) //用两套斜率和截距
                            radiation = scale * srcBandData[index] + offSet;
                        else if (srcBandData[index] >= inflection)
                            radiation = scale2 * srcBandData[index] + offSet2;
                        //if (solarZenithData[index] > 0 && solarZenithData[index] < 18000) //这里的solarZenithData已经不是真实的高度角，而是经过计算的了
                        {
                            srcBandData[index] = (UInt16) (radiation * solarZenithData[index]);
                            if (srcBandData[index] > 65000) //理论上讲反射率应当是0-100
                            {
                                srcBandData[index] = 0;
                            }
                        }
                    }
                });
            }
            else
            {
                int beginY = 0;
                int endY = height;
                Parallel.For(beginY, endY, (rowIndex) =>
                {
                    double scale = _refSB_Coeff[rowIndex + yoffset, bandIndex * 5];
                    double offSet = _refSB_Coeff[rowIndex + yoffset, bandIndex * 5 + 1];
                    double scale2 = _refSB_Coeff[rowIndex + yoffset, bandIndex * 5 + 2];
                    double offSet2 = _refSB_Coeff[rowIndex + yoffset, bandIndex * 5 + 3];
                    double inflection = _refSB_Coeff[rowIndex + yoffset, bandIndex * 5 + 4];
                    double radiation = 0;
                    int index = 0;
                    for (int j = 0; j < width; j++)
                    {
                        index = rowIndex * width + j;
                        if (0 < srcBandData[index] && srcBandData[index] < inflection) //用两套斜率和截距
                            radiation = scale * srcBandData[index] + offSet;
                        else if (srcBandData[index] >= inflection)
                            radiation = scale2 * srcBandData[index] + offSet2;
                        srcBandData[index] = (UInt16) (10 * radiation);
                    }
                });
            }
        }

        /// <summary>
        /// 读取通道值
        /// </summary>
        private void ReadBandData(UInt16[] bandData, WarpDataset srcRaster, int bandNumber, Size srcSize)
        {
            Band latBand = srcRaster.GetRasterBand(bandNumber);
            latBand.ReadRaster(0, 0, srcSize.Width, srcSize.Height, bandData, srcSize.Width, srcSize.Height, 0, 0);
        }

        /// <summary> 
        /// 准备定位信息,计算投影后的值，并计算范围
        /// </summary>
        private void ReadyLocations(AbstractWarpDataset srcRaster, SpatialReference dstSpatialRef, Size srcSize,
            out double[] xs, out double[] ys, out PrjEnvelope maxPrjEnvelope, Action<int, string> progressCallback)
        {
            if (progressCallback != null)
                progressCallback(1, "读取并插值经度数据集");
            Size locationSize;
            ReadLocations(srcRaster, out xs, out ys, out locationSize);
            TryResetLonlatForLeftRightInvalid(xs, ys, locationSize);
            if (xs == null || xs == null)
                throw new Exception("读取经纬度数据失败");
            if (progressCallback != null)
                progressCallback(3, "预处理经纬度数据集");
            _rasterProjector.ComputeDstEnvelope(_srcSpatialRef, xs, ys, srcSize, dstSpatialRef, out maxPrjEnvelope,
                progressCallback);
        }

        //读取定位信息(经纬度数据集)
        protected override void ReadLocations(AbstractWarpDataset srcRaster, out double[] longitudes,
            out double[] latitudes, out Size locationSize)
        {
            if (srcRaster.Width != 2048)
                throw new ArgumentException("NOAA AVHHR数据的宽度应该为2048");
            locationSize = new Size(srcRaster.Width, srcRaster.Height);
            longitudes = null;
            latitudes = null;

            var xBand = _vrtDataset.GetRasterBand(0);
            var yBand = _vrtDataset.GetRasterBand(1);
            L1BInterpol.InterpolBand(out longitudes, xBand, locationSize);
            L1BInterpol.InterpolBand(out latitudes, yBand, locationSize);
        }

        private double[] ReadBandData(WarpDataset raster, int bandNo)
        {
            Band band = raster.GetRasterBand(bandNo);
            {
                double[] data = new double[band.GetXSize() * band.GetYSize()];
                band.ReadRaster(0, 0, band.GetXSize(), band.GetYSize(), data, band.GetXSize(), band.GetYSize(), 0, 0);
                return data;
            }
        }


        private Double[] ReadBandByName(AbstractWarpDataset srcRaster, string bandName)
        {
            Band[] bands = srcRaster.GetBands(bandName);
            if (bands == null || bands.Length == 0 || bands[0] == null)
                throw new Exception("读取波段" + bandName + "失败:无法获取该通道信息");
            try
            {
                Band band = bands[0];
                {
                    double[] data = new double[band.XSize * band.YSize];
                    band.ReadRaster(0, 0, band.XSize, band.YSize, data, band.XSize, band.YSize, 0, 0);
                    return data;
                }
                (band as IDisposable).Dispose();
            }
            catch (Exception ex)
            {
                throw new Exception("读取波段" + bandName + "失败:" + ex.Message, ex);
            }
        }


        #region 定标参数读取

        //准备[辐射定标]参数
        private void ReadyRadiationArgs(AbstractWarpDataset srcRaster)
        {
            string fileName = srcRaster.fileName;
            L1BDataProvider dp = new L1BDataProvider(fileName);
            try
            {
                if (dp == null)
                    throw new Exception("读取亮温计算参数失败,可能为非NOAA 1BD格式轨道数据");
                D1BDHeader header = dp.Header;
                ushort sateIdentify = (header == null || header.CommonInfoFor1BD == null
                    ? (ushort) 0
                    : header.CommonInfoFor1BD.SatelliteIdentify);
                CreateEvABV(sateIdentify);
                _isDay = false;
                double[,] c = null;
                double[,] b = null;
                double[,] operCoef = null;
                dp.ReadVisiCoefficient(ref operCoef, ref b, ref c);
                bool emety = true;
                for (int i = 0; i < operCoef.GetLength(0); i++)
                {
                    for (int j = 0; j < operCoef.GetLength(1); j++)
                    {
                        if (emety)
                            if (operCoef[i, j] != 0)
                                emety = false;
                    }
                }

                _refSB_Coeff = emety ? c : operCoef;
                double[,] evCoefOper = null;
                dp.ReadIRCoefficient(ref evCoefOper, ref b);
                _emissive_Radiance_Coeff = evCoefOper;
            }
            catch (Exception ex)
            {
                throw new Exception("读取亮温计算参数失败", ex);
            }
        }

        /*
NOAA15
        A[0] = 1.621256f; A[1] = 0.337810f; A[2] = 0.304558f;
        B[0] = 0.998015f; B[1] = 0.998719f; B[2] = 0.999024f;
        v[0] = 2695.9743f; v[1] = 925.4075f; v[2] = 839.8979f;
NOAA16
        A[0] = 1.592459f; A[1] = 0.332380f; A[2] = 0.674623f;
        B[0] = 0.998147f; B[1] = 0.998522f; B[2] = 0.998363f;
        v[0] = 2700.1148f; v[1] = 917.2289f; v[2] = 838.1255f;
NOAA17
        A[0] = 1.702380f; A[1] = 0.271683f; A[2] = 0.309180f;
        B[0] = 0.997378f; B[1] = 0.998794f; B[2] = 0.999012f;
        v[0] = 2669.3554f; v[1] = 926.2947f; v[2] = 839.8246f;
NOAA18
        A[0] = 1.698704f; A[1] = 0.436645f; A[2] = 0.253179f;
        B[0] = 0.996960f; B[1] = 0.998607f; B[2] = 0.999057f;
        v[0] = 2659.7952f; v[1] = 928.1460f; v[2] = 833.2532f;
NOAA19
        A[0] = 1.698704f; A[1] = 0.436645f; A[2] = 0.253179f;
        B[0] = 0.996960f; B[1] = 0.998607f; B[2] = 0.999057f;
        v[0] = 2659.7952f; v[1] = 928.1460f; v[2] = 833.2532f;
        */
        private void CreateEvABV(ushort sateIdentify)
        {
            switch (sateIdentify)
            {
                case 4: //"NOAA15"
                    _emmisive_BT_Coefficients = new float[]
                    {
                        1.621256f, 0.998015f, 2695.9743f, //通道3B:A B V
                        0.337810f, 0.998719f, 925.4075f, //通道4 :A B V
                        0.304558f, 0.999024f, 839.8979f //通道5 :A B V
                    };
                    break;
                case 3: //"NOAA16",识别码待确认
                    _emmisive_BT_Coefficients = new float[]
                    {
                        1.592459f, 0.998147f, 2700.1148f,
                        0.332380f, 0.998522f, 917.2289f,
                        0.674623f, 0.998363f, 838.1255f
                    };
                    break;
                case 11: //"NOAA17"
                    _emmisive_BT_Coefficients = new float[]
                    {
                        1.702380f, 0.997378f, 2669.3554f,
                        0.271683f, 0.998794f, 926.2947f,
                        0.309180f, 0.999012f, 839.8246f
                    };
                    break;
                case 13: //"NOAA18"
                case 14: //"NOAA19"
                default:
                    _emmisive_BT_Coefficients = new float[]
                    {
                        1.698704f, 0.996960f, 2659.7952f,
                        0.436645f, 0.998607f, 928.1460f,
                        0.253179f, 0.999057f, 833.2532f
                    };
                    break;
            }
        }

        private void ReadySolarZenithArgsToFile(AbstractWarpDataset srcRaster)
        {
            if (srcRaster == null)
                throw new ArgumentNullException("srcRaster", "获取太阳天顶角数据失败");
            try
            {
                Size srcSize = new System.Drawing.Size(srcRaster.Width, srcRaster.Height);
                Band szBand = _vrtDataset.GetRasterBand(3);
                double[] readSolarZenithData = null;
                L1BInterpol.InterpolBand(out readSolarZenithData, szBand, srcSize);

                int length = srcRaster.Width * srcRaster.Height;
                float[] saveSolarZenithData = new float[length];
                Parallel.For(0, length, index =>
                {
                    if (readSolarZenithData[index] > 0 && readSolarZenithData[index] < 180)
                        saveSolarZenithData[index] =
                            (float) (10.0f / Math.Cos(readSolarZenithData[index] * 100 * DEG_TO_RAD_P100));
                    else
                        saveSolarZenithData[index] = 0;
                });
                WriteData(saveSolarZenithData, _szDataFilename, srcSize.Width, srcSize.Height);
                saveSolarZenithData = null;
                readSolarZenithData = null;
            }
            catch (Exception ex)
            {
                throw new Exception("获取太阳天顶角数据失败", ex.InnerException);
            }
        }

        private void WriteData(float[] data, string fileName, int width, int height)
        {
            string[] options = new string[]
            {
                "INTERLEAVE=BSQ",
                "VERSION=LDF",
                "WITHHDR=TRUE",
            };
            var outDs = DatasetFactory.CreateRasterDataset(fileName, width, height, 1, DataType.GDT_Float32,
                GenericFilename.GetDriverName("ldf"), null);
            WarpDataset cacheWriter = new WarpDataset(outDs, fileName);
            {
                Band band = cacheWriter.GetRasterBand(0);
                {
                    band.WriteRaster(0, 0, width, height, data, width, height, 0, 0);
                }
            }
            outDs.FlushCache();
            _solarZenithCacheRaster = cacheWriter;
        }

        #endregion

        public override AbstractWarpDataset Project(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings,
            AbstractWarpDataset dstRaster, int beginBandIndex, Action<int, string> progressCallback)
        {
            throw new NotImplementedException();
        }

        public override FilePrjSettings CreateDefaultPrjSettings()
        {
            return new FY3_VIRR_PrjSettings();
        }

        private void TryCreateDefaultArgs(AbstractWarpDataset srcRaster, NOAA_PrjSettings prjSettings,
            ref SpatialReference dstSpatialRef)
        {
            if (dstSpatialRef == null)
                dstSpatialRef = _srcSpatialRef;
            if (string.IsNullOrWhiteSpace(prjSettings.OutFormat))
                prjSettings.OutFormat = "LDF";
            if (prjSettings.OutResolutionX == 0 || prjSettings.OutResolutionY == 0)
            {
                if (dstSpatialRef.IsProjected() == 1)
                {
                    prjSettings.OutResolutionX = 1000F;
                    prjSettings.OutResolutionY = 1000F;
                }
                else
                {
                    prjSettings.OutResolutionX = 0.01F;
                    prjSettings.OutResolutionY = 0.01F;
                }
            }

            if (prjSettings.OutBandNos == null || prjSettings.OutBandNos.Length == 0)
            {
                prjSettings.OutBandNos = new int[] {1, 2, 3, 4, 5};
            }
        }

        public override void ComputeDstEnvelope(AbstractWarpDataset srcRaster, SpatialReference dstSpatialRef,
            out PrjEnvelope maxPrjEnvelope, Action<int, string> progressCallback)
        {
            if (srcRaster != null)
            {
                Size srcSize = new Size(srcRaster.Width, srcRaster.Height);
                double[] xs, ys;
                ReadyLocations(srcRaster, dstSpatialRef, srcSize, out xs, out ys, out maxPrjEnvelope, progressCallback);
            }
            else
            {
                maxPrjEnvelope = PrjEnvelope.Empty;
            }
        }

        protected override void DoRadiation(AbstractWarpDataset srcImgRaster, int i, ushort[] srcBandData,
            float[] solarZenithData, Size srcBlockImgSize, Size angleSize)
        {
            throw new NotImplementedException();
        }

        public override bool HasVaildEnvelope(AbstractWarpDataset geoRaster, PrjEnvelope validEnv,
            SpatialReference dstSpatialRef)
        {
            string vrtPath = GetCacheFilename(geoRaster.fileName, "ext.vrt");
            if (!File.Exists(vrtPath))
                File.WriteAllText(vrtPath,
                    vrtContent.Replace("{FILEPATH}", geoRaster.fileName)
                        .Replace("{HEIGHT}", geoRaster.Height.ToString()));
            if (_vrtDataset == null)
                _vrtDataset = Gdal.Open(vrtPath, Access.GA_ReadOnly);
            Band xband = _vrtDataset.GetRasterBand(1);
            Band yband = _vrtDataset.GetRasterBand(2);
            int width = xband.XSize;
            int height = xband.YSize;

            double[] xs = new double[width * height];
            double[] ys = new double[width * height];
            xband.ReadRaster(0, 0, width, height, xs, width, height, 0, 0);
            yband.ReadRaster(0, 0, width, height, ys, width, height, 0, 0);
            RasterProjector rasterProjector = new RasterProjector();
            return rasterProjector.HasVaildEnvelope(xs, ys, validEnv, null, dstSpatialRef);
        }
    }

    #region L1BInterpol

    /// <summary>
    /// L1BInterpol数据插值  51*3622插值成2048*3622
    /// </summary>
    public class L1BInterpol
    {
        static int MIDDLE_INTERP_ORDER = 4;
        static int END_INTERP_ORDER = 5; /* Ensure this is an odd number, 5 is suitable.*/

        /* Convert number of known point to its index in full array */
        /*IDX(N) ((N)*knownStep+knownFirst)*/
        public static void InterpolBand(out double[] bufs, Band srcBand, Size srcSize)
        {
            double[] tBufs = new double[srcSize.Width * srcSize.Height];
            int height = srcBand.YSize;
            int width = srcBand.XSize;
            double[] buf = new double[width * height];
            //double[] outBuf = new double[srcSize.Width];
            srcBand.ReadRaster(0, 0, width, height, buf,
                width, height, 0, 0);
            Parallel.ForEach(Partitioner.Create(0, height), range =>
            {
                double[] outBuf = new double[srcSize.Width];
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        outBuf[24 + j * 40] = buf[i * width + j];
                    }

                    L1BInterpol.Interpol(outBuf, width, 24, 40, srcSize.Width);
                    Array.Copy(outBuf, 0, tBufs, i * srcSize.Width, srcSize.Width);
                }
            });
            bufs = tBufs;

            //for (int i = 0; i < srcSize.Height; i++)
            //{
            //    for (int j = 0; j < width; j++)
            //    {
            //        outBuf[24 + j * 40] = buf[i * width + j];
            //    }
            //    L1BInterpol.Interpol(outBuf, width, 24, 40, srcSize.Width);
            //    Array.Copy(outBuf, 0, bufs, i * srcSize.Width, srcSize.Width);
            //}
        }

        public static void Interpol(double[] vals,
            int numKnown, /* Number of known points (typically 51) */
            int knownFirst, /* Index in full array of first known point (24) */
            int knownStep, /* Interval to next and subsequent known points (40) */
            int numPoints /* Number of points in whole array (2048) */)
        {
            int i, j;
            double[] x = new double[END_INTERP_ORDER];
            double[] y = new double[END_INTERP_ORDER];

            /* First extrapolate first 24 points */
            for (i = 0; i < END_INTERP_ORDER; i++)
            {
                x[i] = i * knownStep + knownFirst;
                y[i] = vals[i * knownStep + knownFirst];
            }

            for (i = 0; i < knownFirst; i++)
            {
                vals[i] = LagrangeInterpol(x, y, i, END_INTERP_ORDER);
            }

            /* Next extrapolate last 23 points */
            for (i = 0; i < END_INTERP_ORDER; i++)
            {
                x[i] = (numKnown - END_INTERP_ORDER + i) * knownStep + knownFirst;
                y[i] = vals[(numKnown - END_INTERP_ORDER + i) * knownStep + knownFirst];
            }

            for (i = (numKnown - 1) * knownStep + knownFirst; i < numPoints; i++)
            {
                vals[i] = LagrangeInterpol(x, y, i, END_INTERP_ORDER);
            }

            /* Interpolate all intermediate points using two before and two after */
            for (i = knownFirst; i < (numKnown - 1) * knownStep + knownFirst; i++)
            {
                double[] x2 = new double[MIDDLE_INTERP_ORDER];
                double[] y2 = new double[MIDDLE_INTERP_ORDER];
                int startpt;

                /* Find a suitable set of two known points before and two after */
                startpt = (i / knownStep) - MIDDLE_INTERP_ORDER / 2;
                if (startpt < 0)
                    startpt = 0;
                if (startpt + MIDDLE_INTERP_ORDER - 1 >= numKnown)
                    startpt = numKnown - MIDDLE_INTERP_ORDER;
                for (j = 0; j < MIDDLE_INTERP_ORDER; j++)
                {
                    x2[j] = (startpt + j) * knownStep + knownFirst;
                    y2[j] = vals[(startpt + j) * knownStep + knownFirst];
                }

                vals[i] = LagrangeInterpol(x2, y2, i, MIDDLE_INTERP_ORDER);
            }
        }

        /// <summary>
        /// Perform a Lagrangian interpolation through the given x,y coordinates
        /// and return the interpolated y value for the given x value.
        /// The array size and thus the polynomial order is defined by numpt.
        /// Input: x[] and y[] are of size numpt,
        /// x0 is the x value for which we calculate the corresponding y
        /// Returns: y value calculated for given x0.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="x0"></param>
        /// <param name="numpt"></param>
        /// <returns></returns>
        static double LagrangeInterpol(double[] x, double[] y, double x0, int numpt)
        {
            int i, j;
            double L;
            double y0 = 0;

            for (i = 0; i < numpt; i++)
            {
                L = 1.0;
                for (j = 0; j < numpt; j++)
                {
                    if (i == j)
                        continue;
                    L = L * (x0 - x[j]) / (x[i] - x[j]);
                }

                y0 = y0 + L * y[i];
            }

            return y0;
        }
    }

    #endregion
}