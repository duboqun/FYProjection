using PIE.DataSource;
using PIE.Geometry;
using PIE.Meteo.Common;
using PIE.Meteo.Core;
using PIE.Meteo.RasterProject;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PIE.Meteo.FileProject
{
    public abstract class GeosProject : FileProjector
    {
        private IRasterBand[] _rasterBands = null;
        protected IProjectionTransform _projectionTransform;
        protected ISpatialReference _srcSpatialRef { get; set; }//转换的目标投影
        protected double[] _srcGeoTrans { get; set; }//转换的仿射变换参数

        protected static UInt16 NODATA_VALUE = 65535;

        protected AbstractWarpDataset _prdWriter { get; set; }
        public GeosProject()
        {
            _name = "GeosProject";
            _fullname = "静止卫星投影";
            _rasterProjector = new RasterProjector();
        }


        public override AbstractWarpDataset Project(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings, AbstractWarpDataset dstRaster, int beginBandIndex, Action<int, string> progressCallback)
        {

            _dstSpatialRef = dstRaster.SpatialRef;
            _projectionTransform = ProjectionTransformFactory.GetProjectionTransform(_srcSpatialRef, _dstSpatialRef);
            ProjectRaster(srcRaster, prjSettings, dstRaster, progressCallback, NODATA_VALUE, beginBandIndex, 1, 1);
            GC.Collect();
            return dstRaster;
        }
        // 1
        public override void Project(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings, ISpatialReference dstSpatialRef, Action<int, string> progressCallback)
        {
            _dstSpatialRef = dstSpatialRef;
            _projectionTransform = ProjectionTransformFactory.GetProjectionTransform(_srcSpatialRef, dstSpatialRef);
            ProjectRaster(srcRaster, prjSettings, _prdWriter, progressCallback, NODATA_VALUE, 0, 1, 1);
            GC.Collect();
        }

        // 2
        private void ProjectRaster(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings, AbstractWarpDataset prdWriter, Action<int, string> progressCallback, UInt16 fillValue, int beginBandIndex, double dataWeight, float zoom)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();

            //输出数据类型
            float outResolutionX = prjSettings.OutResolutionX;
            float outResolutionY = prjSettings.OutResolutionY;
            Size outSize = prjSettings.OutSize;
            int outWidth = outSize.Width;
            int outHeight = outSize.Height;
            int outBandCount = prjSettings.OutBandNos.Length;
            if (progressCallback != null)
                progressCallback(_readyProgress++, "投影准备");
            int srcHeight = srcRaster.Height;
            int srcWidth = srcRaster.Width;
            PrjPoint outLeftTopPoint = prjSettings.OutEnvelope.LeftTop;
            //源数据的分辨率
            float srcResolutionX = Convert.ToSingle(_srcGeoTrans[1]);
            float srcResolutionY = Math.Abs(Convert.ToSingle(_srcGeoTrans[5]));
            //输入数据的大小
            Size srcSize = new Size() { Width = srcWidth, Height = srcHeight };
            MyLog.Log.Print.Info($"投影输入图像尺寸：{srcSize}");
            MyLog.Log.Print.Info($"投影输出图像尺寸：{outSize}");
            //输出数据的左上角地理坐标
            double outLtPointX = outLeftTopPoint.X;
            double outLtPointY = outLeftTopPoint.Y;
            //输入数据源的坐标范围
            PrjEnvelope srcEnvelope = new PrjEnvelope(_srcGeoTrans[0], _srcGeoTrans[0] + srcWidth * _srcGeoTrans[1], _srcGeoTrans[3] + srcHeight * _srcGeoTrans[5], _srcGeoTrans[3], _srcSpatialRef);
            ulong mem = MemoryHelper.GetAvalidPhyMemory();
            MyLog.Log.Print.Info($"当前剩余物理内存：{mem / 1024 / 1024 / 1024}GB");
            mem = mem > 10.0 * 1024 * 1024 * 1024 ? Convert.ToUInt64(10.0 * 1024 * 1024 * 1024) : mem;
            ulong maxLimit = mem / (6 * 8);
            int rowStep = (int)(maxLimit / (UInt32)outWidth);
            if (rowStep == 0)
                rowStep = 1;
            if (rowStep > outHeight)
                rowStep = outHeight;
            int stepCount = (int)(Math.Ceiling((double)outHeight / rowStep));
            int percent = 0;
            int progress = 0;
            int progressCount = outBandCount * stepCount + stepCount * 2;
            MyLog.Log.Print.Info($"当前投影处理每步骤处理：{rowStep}行");
            long stepGeolocationTime = 0, stepReadDataTime = 0, stepRadiationTime = 0, stepProjectRasterTime = 0, stepWriteDataTime = 0;
            for (int oRow = 0; oRow < outHeight; oRow += rowStep)
            {
                MyLog.Log.Print.Info($"当前投影处理开始行号：{oRow}行");
                if (progressCallback != null)
                {
                    percent++;
                    progress = percent * 100 / progressCount;
                    progressCallback(progress, string.Format("投影完成{0}%", progress));
                }
                if (oRow + rowStep > outHeight)
                    rowStep = outHeight - oRow;

                //经纬度数据
                double[] xs, ys;
                UInt16[] rows, cols;
                try
                {
                    xs = new double[outWidth * rowStep];
                    ys = new double[outWidth * rowStep];
                    rows = new UInt16[outWidth * rowStep];
                    cols = new UInt16[outWidth * rowStep];
                }
                catch (Exception ex)
                {
                    MyLog.Log.Print.Error("GeosProject.ProjectRaster()", ex);
                    throw;
                }
                Size srcStepSize = new Size();
                Block oBlock, tBlock;
                timer.Restart();
                #region 初始化查找表
                {
                    double oY = oRow * outResolutionY;
                    Parallel.For(0, rowStep, j =>
                    {
                        double x;
                        double y;
                        int index;
                        y = outLtPointY - j * outResolutionY - oY;
                        for (int i = 0; i < outWidth; i++)
                        {
                            x = outLtPointX + i * outResolutionX;
                            index = i + j * outWidth;
                            xs[index] = x;
                            ys[index] = y;
                        }
                    });
                    GeosInverCorrection(_dstSpatialRef, xs, ys);
                    _projectionTransform.InverTransform(xs, ys);
                    PrjEnvelope tEnvelope = PrjEnvelope.GetEnvelope(xs, ys, null);
                    tEnvelope.Extend(srcResolutionX, srcResolutionY * 4);
                    tEnvelope = PrjEnvelope.Intersect(tEnvelope, srcEnvelope);
                    if (tEnvelope == null || tEnvelope.IsEmpty)
                        continue;
                    Size tSize = tEnvelope.GetSize(srcResolutionX, srcResolutionY);
                    PrjBlockHelper.ComputeBlock(srcEnvelope, srcSize, tEnvelope, tSize, out oBlock, out tBlock);
                    int srcStepWidth = oBlock.xEnd - oBlock.xBegin;
                    int srcStepHeight = oBlock.yEnd - oBlock.yBegin;
                    srcStepSize = new Size(srcStepWidth, srcStepHeight);

                    double srcStepLeftTopX = tEnvelope.MinX;
                    double srcStepLeftTopY = tEnvelope.MaxY;
                    double srcStepRightBottomX = tEnvelope.MaxX;
                    double srcStepRightBottomY = tEnvelope.MinY;

                    if (progressCallback != null)
                    {
                        percent++;
                        progress = percent * 100 / progressCount;
                        progressCallback(progress, string.Format("投影完成{0}%", progress));
                    }
                    Parallel.For(0, rowStep, j =>
                    {
                        double x;
                        double y;
                        int index;
                        for (int i = 0; i < outWidth; i++)
                        {
                            index = i + j * outWidth;
                            x = xs[index];
                            y = ys[index];
                            if (x >= srcStepLeftTopX && x <= srcStepRightBottomX && y <= srcStepLeftTopY && y >= srcStepRightBottomY)
                            {
                                cols[index] = (UInt16)((x - srcStepLeftTopX) / srcResolutionX + 0.5);
                                rows[index] = (UInt16)((srcStepLeftTopY - y) / srcResolutionY + 0.5);
                            }
                        }
                    });
                    xs = null;
                    ys = null;
                }
                #endregion
                stepGeolocationTime += timer.ElapsedMilliseconds;

                //输入波段数据缓冲区
                UInt16[] srcBandData = new UInt16[srcStepSize.Width * srcStepSize.Height];
                //输出波段数据缓冲区
                UInt16[] dstBandData = new UInt16[outWidth * rowStep];
                //逐波段写入数据
                for (int b = beginBandIndex; b < outBandCount + beginBandIndex; b++)
                {
                    if (progressCallback != null)
                    {
                        percent++;
                        progress = percent * 100 / progressCount;
                        progressCallback(progress, string.Format("投影完成{0}%", progress));
                    }
                    var bandNum = b - beginBandIndex;
                    timer.Restart();
                    ReadBandData<UInt16>(srcBandData, bandNum, oBlock.xBegin, oBlock.yBegin, srcStepSize.Width, srcStepSize.Height, PixelDataType.UInt16);
                    stepReadDataTime += timer.ElapsedMilliseconds;

                    Size srcBlockSize = new Size(srcStepSize.Width, srcStepSize.Height);
                    //进行辐射定标
                    timer.Restart();
                    DoRadiation(srcRaster, bandNum, srcBandData, null, srcBlockSize, srcBlockSize);
                    stepRadiationTime += timer.ElapsedMilliseconds;

                    //用于测试全图输出结果，用于查看插值的效果：
#if DebugEaseGrid
                    GCHandle hTe = GCHandle.Alloc(srcBandData, GCHandleType.Pinned);
                    try
                    {Random random = new Random(12);
                        IntPtr bufferPtr = hTe.AddrOfPinnedObject();
                        Int16[] tmp = new Int16[srcBandData.Length];
                        for (int i = 0; i < srcBandData.Length; i++)
                        {
                            tmp[i] = (Int16)random.Next(200, 255);
                        }
                        Marshal.Copy(tmp, 0, bufferPtr, tmp.Length);
                    }
                    finally
                    {
                        hTe.Free();
                    }
#endif
                    //Size outStepSize = new Size(outWidth, rowStep);
                    timer.Restart();
                    _rasterProjector.Project<UInt16>(srcBandData, srcStepSize, rows, cols, new Size(outWidth, rowStep), dstBandData, fillValue, 0, null);
                    stepProjectRasterTime += timer.ElapsedMilliseconds;

                    timer.Restart();
                    if (dataWeight == 1)
                        WriteDataToLDF<UInt16>(prdWriter, PixelDataType.UInt16, outWidth, rowStep, oRow, dstBandData, b);
                    else
                        WriteDataToLDF<UInt16>(prdWriter, PixelDataType.UInt16, outWidth, rowStep, oRow, dstBandData, b, dataWeight, zoom);
                    stepWriteDataTime += timer.ElapsedMilliseconds;

                }
                rows = null;
                rows = null;
                srcBandData = null;
                dstBandData = null;
            }
            MyLog.Log.Print.Info($"坐标投影转换：{stepGeolocationTime}ms");
            MyLog.Log.Print.Info($"读取波段数据：{stepReadDataTime}ms");
            MyLog.Log.Print.Info($"定标波段数据：{stepRadiationTime}ms");
            MyLog.Log.Print.Info($"投影波段数据：{stepProjectRasterTime}ms");
            MyLog.Log.Print.Info($"写入波段数据：{stepWriteDataTime}ms");
        }

        /// <summary>
        /// 参数检验
        /// </summary>
        protected void ArgsCheck(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings, ISpatialReference dstSpatialRef)
        {
            if (srcRaster == null)
                throw new ArgumentNullException("srcRaster");
            if (prjSettings == null)
                throw new ArgumentNullException("prjSettings");
            if (dstSpatialRef == null)
                throw new ArgumentNullException("dstSpatialRef");
            //if (srcRaster.SpatialRef == null)
            //    throw new ArgumentNullException("srcRaster.SpatialRef");
            if (string.IsNullOrWhiteSpace(prjSettings.OutFormat))
            {
                if (srcRaster.DriverName == "MEM")
                    prjSettings.OutFormat = "MEM";
                else
                    prjSettings.OutFormat = "ENVI";
            }
            if (prjSettings.OutResolutionX == 0f || prjSettings.OutResolutionY == 0f)
            {
                bool isProjectRef = srcRaster.SpatialRef is PIE.Geometry.IProjectedCoordinateSystem;
                if ((dstSpatialRef.Type == SpatialReferenceType.GeographicCS && !isProjectRef) ||
                    (dstSpatialRef.Type == SpatialReferenceType.ProjectedCS && isProjectRef))
                {
                    prjSettings.OutResolutionX = srcRaster.ResolutionX;
                    prjSettings.OutResolutionY = srcRaster.ResolutionY;
                }
                else if (dstSpatialRef.Type == SpatialReferenceType.ProjectedCS && !isProjectRef)
                {
                    prjSettings.OutResolutionX = srcRaster.ResolutionX * 100000F;
                    prjSettings.OutResolutionY = srcRaster.ResolutionY * 100000F;
                }
                else if (dstSpatialRef.Type == SpatialReferenceType.GeographicCS && isProjectRef)
                {
                    prjSettings.OutResolutionX = srcRaster.ResolutionX / 100000F;
                    prjSettings.OutResolutionY = srcRaster.ResolutionY / 100000F;
                }
            }
            if (prjSettings.OutBandNos == null || prjSettings.OutBandNos.Length == 0)
            {
                List<int> bandNoList = new List<int>();
                for (int i = 1; i <= srcRaster.BandCount; i++)
                {
                    bandNoList.Add(i);
                }
                prjSettings.OutBandNos = bandNoList.ToArray();
            }
        }

        protected void ReadySession(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings, ISpatialReference dstSpatialRef, Action<int, string> progressCallback)
        {
            if (_curSession == null || _curSession != srcRaster || _isBeginSession)
            {
                Size srcSize = new Size(srcRaster.Width, srcRaster.Height);
                if (progressCallback != null)
                    progressCallback(_readyProgress++, "投影准备");
                _rasterDataBands = TryCreateRasterDataBands(srcRaster, prjSettings, progressCallback);
                _isBeginSession = false;
            }
        }

        public override void EndSession()
        {
            if (_prdWriter != null)
            {
                _prdWriter.Dispose();
                _prdWriter = null;
            }
        }

        protected IRasterBand[] TryCreateRasterDataBands(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings, Action<int, string> progressCallback)
        {
            PrjBand[] bands = PrjBandTable.GetPrjBands(srcRaster);
            List<IRasterBand> rasterBands = new List<IRasterBand>();
            for (int i = 0; i < prjSettings.OutBandNos.Length; i++)
            {
                if (progressCallback != null)
                    progressCallback(_readyProgress++, "投影准备");
                int bandNo = prjSettings.OutBandNos[i];
                PrjBand b = bands[bandNo - 1];
                IRasterBand band = srcRaster.GetBands(b.DataSetName)[0];
                rasterBands.Add(band);
            }
            return rasterBands.ToArray();
        }

        #region NotImplemented
        public override bool IsSupport(string fileName)
        {
            throw new NotImplementedException();
        }


        public override FilePrjSettings CreateDefaultPrjSettings()
        {
            throw new NotImplementedException();
        }


        protected override void ReadLocations(AbstractWarpDataset srcRaster, out double[] xs, out double[] ys, out Size locationSize)
        {
            throw new NotImplementedException();
        }

        protected override void DoRadiation(AbstractWarpDataset srcImgRaster, int i, ushort[] srcBandData, float[] solarZenithData, Size srcBlockImgSize, Size angleSize)
        {
            throw new NotImplementedException();
        }

        private void ProjectToLDF(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings, AbstractWarpDataset prdWriter, ISpatialReference dstSpatialRef, Action<int, string> progressCallback, double weight, float zoom)
        {


        }

        private void ProjectRaster(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings, Action<int, string> progressCallback, AbstractWarpDataset prdWriter, UInt16 fillValue)
        {
            ProjectRaster(srcRaster, prjSettings, prdWriter, progressCallback, fillValue, 0, 1, 1);
        }

        #endregion

        #region 文件读写
        private void WriteDataToLDF<T>(AbstractWarpDataset prdWriter, PixelDataType dataType, int outWidth, int rowStep, int oRow, T[] dstBandData, int b)
        {
            IRasterBand band = null;
            try
            {
                band = prdWriter.GetRasterBand(b);
                GCHandle h = GCHandle.Alloc(dstBandData, GCHandleType.Pinned);
                try
                {
                    IntPtr bufferPtr = h.AddrOfPinnedObject();
                    bool writeState = band.Write(0, oRow, outWidth, rowStep, bufferPtr, outWidth, rowStep, dataType);
                    if (!writeState)
                    {
                        System.Diagnostics.Debug.WriteLine("文件写入失败");
                        MyLog.Log.Print.Error("文件写入失败:" + prdWriter.fileName);
                    }
                }
                finally
                {
                    h.Free();
                }
            }
            finally
            {
                //这里不能释放，由于大部分band是记录在RasterDataProvider中的数组中的，如果释放后，下次取就会出错
                //if (band!=null&&band is IGDALRasterBand)
                //{
                //    band.Dispose();
                //}
            }
        }

        private void WriteDataToLDF<T>(AbstractWarpDataset prdWriter, PixelDataType dataType, int outWidth, int rowStep, int oRow, T[] dstBandData, int b, double dataweight, float zoom)
        {
            IRasterBand band = null;

            for (int i = 0; i < dstBandData.Length; i++)
                dstBandData[i] = (T)Convert.ChangeType(Convert.ToDouble(dstBandData[i]) * dataweight * zoom, typeof(T));
            try
            {
                band = prdWriter.GetRasterBand(b + 1);
                GCHandle h = GCHandle.Alloc(dstBandData, GCHandleType.Pinned);
                try
                {
                    IntPtr bufferPtr = h.AddrOfPinnedObject();
                    band.Write(0, oRow, outWidth, rowStep, bufferPtr, outWidth, rowStep, dataType);
                }
                finally
                {
                    h.Free();
                }
            }
            finally
            {
                //这里不能释放，由于大部分band是记录在RasterDataProvider中的数组中的，如果释放后，下次取就会出错
                //if (band!=null&&band is IGDALRasterBand)
                //{
                //    band.Dispose();
                //}
            }
        }

        private void ReadBandData<T>(T[] bandData, int bandIndex, int xOffset, int yOffset, int blockWidth, int blockHeight, PixelDataType dataType)
        {
            IRasterBand latBand = _rasterDataBands[bandIndex];//
            GCHandle h = h = GCHandle.Alloc(bandData, GCHandleType.Pinned);
            try
            {
                IntPtr bufferPtr = h.AddrOfPinnedObject();
                bool readState = latBand.Read(xOffset, yOffset, blockWidth, blockHeight, bufferPtr, blockWidth, blockHeight, dataType);
                if (!readState)
                {
                    System.Diagnostics.Debug.WriteLine("文件读取失败");
                    MyLog.Log.Print.Error("文件读取失败:GeosProject _rasterDataBands");
                }
            }
            finally
            {
                h.Free();
            }
        }
        #endregion

        #region 创建输出文件
        /// <summary>
        /// 创建输出文件
        /// </summary>
        protected void CreateDstRaster(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings, ISpatialReference dstSpatialRef, Action<int, string> progressCallback)
        {
            ArgsCheck(srcRaster, prjSettings, dstSpatialRef);

            PrjEnvelope dstEnvelope = null;
            ComputeDstEnvelope(srcRaster, dstSpatialRef, out dstEnvelope, progressCallback);
            prjSettings.OutEnvelope = dstEnvelope;
            string outFilename = prjSettings.OutPathAndFileName;
            CheckAndCreateDir(Path.GetDirectoryName(outFilename));
            _prdWriter = CreateOutFile(srcRaster, prjSettings, dstSpatialRef, srcRaster.DataType);
        }

        protected AbstractWarpDataset CreateOutFile(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings, ISpatialReference dstSpatial, PixelDataType dataType)
        {
            float resolutionX = prjSettings.OutResolutionX;
            float resolutionY = prjSettings.OutResolutionY;

            Size outSize = prjSettings.OutEnvelope.GetSize(resolutionX, resolutionY);
            int bandCount = prjSettings.OutBandNos.Length;
            string filename = prjSettings.OutPathAndFileName;

            CheckAndCreateDir(Path.GetDirectoryName(filename));

            PIE.Geometry.Envelope env = new Geometry.Envelope()
            {
                XMax = prjSettings.OutEnvelope.MaxX,
                XMin = prjSettings.OutEnvelope.MinX,
                YMax = prjSettings.OutEnvelope.MaxY,
                YMin = prjSettings.OutEnvelope.MinY,
            };
            string[] _options = new string[] { "header_offset=128" };
            double[] geoTrans = new double[] { env.XMin, Convert.ToDouble(resolutionX.ToString("f6")), 0, env.YMax, 0, -Convert.ToDouble(resolutionY.ToString("f6")) };
            var ds = DatasetFactory.CreateRasterDataset(filename, outSize.Width, outSize.Height, bandCount, PixelDataType.UInt16, "ENVI", null);
            ds.SetGeoTransform(geoTrans);
            if (ds == null)
                throw new ArgumentException("请检查输出文件路径");
            ds.SpatialReference = dstSpatial;
            Enumerable.Range(0, bandCount).ToList().ForEach(t => ds.GetRasterBand(t).SetNoDataValue(NODATA_VALUE));
            return new WarpDataset(ds);
        }
        #endregion

        #region 设置输出文件无效值
        protected void SetPrdwritterNodata(AbstractWarpDataset dstRaster, double nodatavalue)
        {
            Enumerable.Range(0, dstRaster.BandCount).ToList().ForEach(t => dstRaster.GetRasterBand(t).SetNoDataValue(nodatavalue));
        }
        #endregion

        #region 坐标360°修正
        /// <summary>
        /// 经度跨越180度，进行修正
        /// </summary>
        protected void GeosCorrection(ISpatialReference dstSpatialRef, double[] xs, double[] ys)
        {
            //都是WGS84
            if (dstSpatialRef.IsSame(SpatialReferenceFactory.CreateSpatialReference(4326)))
            {
                for (int i = 0; i < xs.Length; i++)
                {
                    if (xs[i] < -90)
                    {
                        xs[i] = xs[i] + 360;
                    }
                }
            }
        }

        /// <summary>
        /// 经度跨越180度，进行修正
        /// </summary>
        protected void GeosInverCorrection(ISpatialReference dstSpatialRef, double[] xs, double[] ys)
        {
            //都是WGS84
            if (dstSpatialRef.IsSame(SpatialReferenceFactory.CreateSpatialReference(4326)))
            {
                for (int i = 0; i < xs.Length; i++)
                {
                    if (xs[i] > 180)
                    {
                        xs[i] = xs[i] - 360;
                    }
                }
            }
        }
        #endregion
    }
}
