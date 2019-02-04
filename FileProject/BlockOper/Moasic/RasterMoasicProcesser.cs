using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using OSGeo.GDAL;
using OSGeo.OGR;
using VBand = PIE.Meteo.FileProject.BlockOper.VirtualRasterBand;

namespace PIE.Meteo.FileProject.BlockOper
{
    public class RasterMoasicProcesser : IRasterMoasic
    {
        public AbstractWarpDataset Moasic(AbstractWarpDataset[] srcRasters, string driver, string outPath,
            bool isProcessInvalid, string[] invalidValues, Action<int, string> progressCallback,
            params object[] options)
        {
            //创建目标文件,dstEnvelope = MaxEnvelope(srcRasters)
            string driverName = driver.ToUpper();
            AbstractWarpDataset dstRaster = CreatDstDataProvider(srcRasters, driverName, outPath);
            Moasic(srcRasters, dstRaster, isProcessInvalid, invalidValues, progressCallback, options);
            return dstRaster;
        }

        public AbstractWarpDataset Moasic<T>(AbstractWarpDataset[] srcRasters, string driver, string outDir,
            bool isProcessInvalid, string[] invalidValues, string processMethod, Action<int, string> progressCallback,
            Func<T, T, T> extensionAction, params object[] options)
        {
            //创建目标文件,dstEnvelope = MaxEnvelope(srcRasters)
            string driverName = driver.ToUpper();
            AbstractWarpDataset dstRaster = CreatDstDataProvider(srcRasters, driverName, outDir);
            Moasic<T>(srcRasters, dstRaster, outDir, isProcessInvalid, invalidValues, processMethod, progressCallback,
                extensionAction, options);
            return dstRaster;
        }

        public AbstractWarpDataset Moasic<T>(AbstractWarpDataset[] srcRasters, string driver, string outDir,
            Envelope dstCrd, float resolutionX, float resolutionY, bool isProcessInvalid, string[] invalidValues,
            string processMethod, Action<int, string> progressCallback, Func<T, T, T> extensionAction,
            params object[] options)
        {
            //创建目标文件
            string driverName = driver.ToUpper();
            AbstractWarpDataset dstRaster =
                CreatDstDataProvider(srcRasters, driverName, outDir, dstCrd, resolutionX, resolutionY);
            Moasic<T>(srcRasters, dstRaster, outDir, isProcessInvalid, invalidValues, processMethod, progressCallback,
                extensionAction, options);
            return dstRaster;
        }

        public AbstractWarpDataset Moasic(AbstractWarpDataset[] srcRasters, int[] bandNos, string driver, string outDir,
            Envelope dstCrd, float resolutionX, float resolutionY, bool isProcessInvalid, string[] invalidValues,
            Action<int, string> progressCallback, params object[] options)
        {
            //创建目标文件
            string driverName = driver.ToUpper();
            AbstractWarpDataset dstRaster = CreatDstDataProvider(srcRasters, bandNos, driverName, outDir, dstCrd,
                resolutionX, resolutionY);
            Moasic(srcRasters, dstRaster, bandNos, isProcessInvalid, invalidValues, progressCallback, options);
            return dstRaster;
        }

        private AbstractWarpDataset CreatDstDataProvider(AbstractWarpDataset[] srcRasters, string driverName,
            string outFileName, Envelope dstCrd, float resolutionX, float resolutionY)
        {
            //目标文件名
            string dstFileName = null;
            if (!string.IsNullOrEmpty(outFileName))
            {
                string outDir = Path.GetDirectoryName(outFileName);
                string outf = Path.GetPathRoot(outFileName);
                string name = Path.GetFileName(outFileName);
                if (outDir != null && Directory.Exists(outDir))
                {
                    dstFileName = outFileName;
                }
                else if (outDir != null)
                {
                    Directory.CreateDirectory(outDir);
                    dstFileName = outFileName;
                }
                else if (outf != null && string.IsNullOrEmpty(name))
                {
                    dstFileName += Path.Combine(outf,
                        Path.GetFileNameWithoutExtension(srcRasters[0].fileName) + "_MOASIC" + ".ldf");
                }
            }
            else
                dstFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "Temp",
                    Path.GetFileNameWithoutExtension(srcRasters[0].fileName) +
                    "_" + "MOASIC" + ".ldf");

            int bandCount = srcRasters[0].BandCount;
            var dataType = srcRasters[0].DataType;
            //计算行列数
            int xSize = 0;
            int ySize = 0;
            xSize = (int) Math.Round((dstCrd.GetWidth() / resolutionX), 0);
            ySize = (int) Math.Round((dstCrd.GetHeight() / resolutionY), 0);
            string[] optionString = new string[]
            {
                "INTERLEAVE=BSQ",
                "VERSION=LDF",
                "WITHHDR=TRUE",
                "SPATIALREF=" + (srcRasters[0].SpatialRef != null
                    ? srcRasters[0].SpatialRef.ExportToProj4()
                    : string.Empty),
            };
            string[] _options = new string[] {"header_offset=128"};
            var ds = DatasetFactory.CreateRasterDataset(dstFileName, xSize, ySize, bandCount, dataType, driverName,
                null);
            if (ds == null)
                throw new ArgumentException("请检查输出文件路径");
            return new WarpDataset(ds,dstFileName);
        }

        private AbstractWarpDataset CreatDstDataProvider(AbstractWarpDataset[] srcRasters, int[] bandNos,
            string driverName, string outFileName, Envelope dstCrd, float resolutionX, float resolutionY)
        {
            //目标文件名
            string dstFileName = null;
            if (!string.IsNullOrEmpty(outFileName))
            {
                string outDir = Path.GetDirectoryName(outFileName);
                string outf = Path.GetPathRoot(outFileName);
                string name = Path.GetFileName(outFileName);
                if (outDir != null && Directory.Exists(outDir))
                {
                    dstFileName = outFileName;
                }
                else if (outDir != null)
                {
                    Directory.CreateDirectory(outDir);
                    dstFileName = outFileName;
                }
                else if (outf != null && string.IsNullOrEmpty(name))
                {
                    dstFileName += Path.Combine(outf,
                        Path.GetFileNameWithoutExtension(srcRasters[0].fileName) + "_MOASIC" + ".ldf");
                }
            }
            else
                dstFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "Temp",
                    Path.GetFileNameWithoutExtension(srcRasters[0].fileName) +
                    "_" + "MOASIC" + ".ldf");

            int bandCount = bandNos.Length;
            var dataType = srcRasters[0].DataType;
            //计算行列数
            int xSize = 0;
            int ySize = 0;
            if (dstCrd == null || dstCrd.GetHeight() == 0 || dstCrd.GetWidth() == 0)
            {
                if (srcRasters.Count() == 0 || srcRasters == null)
                    return null;
                if (srcRasters[0].GetEnvelope() == null)
                    return null;
                else
                {
                    dstCrd = srcRasters[0].GetEnvelope();
                    int count = srcRasters.Count();
                    for (int i = 1; i < count; i++)
                    {
                        if (srcRasters[i].GetEnvelope() == null)
                            return null;
                        dstCrd = dstCrd.Union((srcRasters[i].GetEnvelope()));
                    }
                }
            }

            xSize = (int) Math.Round((dstCrd.GetWidth() / resolutionX), 0);
            ySize = (int) Math.Round((dstCrd.GetHeight() / resolutionY), 0);
            string[] optionString = new string[]
            {
                "INTERLEAVE=BSQ",
                "VERSION=LDF",
                "WITHHDR=TRUE",
                "SPATIALREF=" + (srcRasters[0].SpatialRef != null
                    ? srcRasters[0].SpatialRef.ExportToProj4()
                    : string.Empty),
            };
            string[] _options = new string[] {"header_offset=128"};
            var ds = DatasetFactory.CreateRasterDataset(dstFileName, xSize, ySize, bandCount, dataType, driverName,
                null);
            if (ds == null)
                throw new ArgumentException("请检查输出文件路径");
            return new WarpDataset(ds,dstFileName);
        }

        public void Moasic(AbstractWarpDataset[] srcRasters, AbstractWarpDataset dstRaster, bool isProcessInvalid,
            string[] invalidValues, Action<int, string> progressCallback, params object[] options)
        {
            if (srcRasters == null || srcRasters.Count() == 0)
                return;
            //检查待拼接文件波段数、数据类型、空间参考是否相等
            if (RasterMoasicChecker.CheckBandCount(srcRasters) == false ||
                RasterMoasicChecker.CheckDataType(srcRasters) == false ||
                RasterMoasicChecker.CheckSpatialRef(srcRasters) == false)
                return;
            var dataType = srcRasters[0].DataType;
            if (progressCallback != null)
                progressCallback(0, "开始镶嵌/拼接");
            Envelope currentEnvelope = srcRasters[0].GetEnvelope();
            for (int num = 0; num < srcRasters.Length; num++)
            {
                float persent = num * 1.0f;
                int oBeginRow = -1, oBeginCol = -1, oEndRow = -1, oEndCol = -1;
                int tBeginRow = -1, tBeginCol = -1, tEndRow = -1, tEndCol = -1;
                new RasterMoasicClipHelper().ComputeBeginEndRowCol(srcRasters[num], dstRaster,
                    new Size(dstRaster.Width, dstRaster.Height), ref oBeginRow, ref oBeginCol, ref oEndRow, ref oEndCol,
                    ref tBeginRow, ref tBeginCol, ref tEndRow, ref tEndCol);
                int rowStep = ClipCutHelper.ComputeRowStep(srcRasters[num], oBeginRow, oEndRow);
                int stepCount = (int) ((oEndRow - oBeginRow) / rowStep + 0.5f) * srcRasters[num].BandCount;
                int curStep = -1;
                for (int oRow = oBeginRow; oRow < oEndRow; oRow += rowStep)
                {
                    if (oRow + rowStep > oEndRow)
                        rowStep = oEndRow - oRow;
                    //计算待拼接文件块的外包矩形(XMin/XMax与原待拼接文件外包矩形一致)
                    Envelope blockEnvelope = GetBlockEnvelope(srcRasters[num], oRow, oRow + rowStep);
                    //与当前外包矩形(已拼接完成部分)是否有交集
                    bool isInternal;
                    //待拼接文件外包矩形与现外包矩形的交集
                    Envelope innerCord = GetInternalEnvelope(currentEnvelope, num, blockEnvelope, out isInternal);

                    #region

                    switch (dataType)
                    {
                        case DataType.GDT_Byte:

                            #region Byte

                        {
                            for (int bandIndex = 0; bandIndex < srcRasters[num].BandCount; bandIndex++)
                            {
                                curStep++;
                                int curp = (int) (((persent + curStep * 1.0f / stepCount) / srcRasters.Length) * 100);
                                if (progressCallback != null)
                                    progressCallback(curp, "拼接/镶嵌完成" + curp + "%");

                                int sample = oEndCol - oBeginCol;
                                long bufferSize = sample * rowStep;
                                byte[] databuffer = new byte[bufferSize];
                                unsafe
                                {
                                    fixed (byte* ptr = databuffer)
                                    {
                                        IntPtr buffer = new IntPtr(ptr);
                                        srcRasters[num].GetRasterBand(bandIndex).ReadRaster(oBeginCol, oRow, sample,
                                            rowStep, buffer, sample, rowStep, srcRasters[num].DataType, 0, 0);
                                        //进行无效值处理
                                        if (isInternal && isProcessInvalid)
                                        {
                                            //求出现有拼接文件在此交集范围内的值
                                            //在外包矩形交集范围内寻找无效值
                                            int innerBeginRow =
                                                (int) ((dstRaster.GetEnvelope().MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int innerEndRow = (int) Math.Round(
                                                (dstRaster.GetEnvelope().MaxY - innerCord.MinY) /
                                                srcRasters[num].ResolutionY, 0);
                                            int innerBeginCol =
                                                (int) ((innerCord.MinX - dstRaster.GetEnvelope().MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int innerEndCol = (int) Math.Round(
                                                (innerCord.MaxX - dstRaster.GetEnvelope().MinX) /
                                                srcRasters[num].ResolutionX, 0);
                                            int xSize = innerEndCol - innerBeginCol;
                                            int ySize = innerEndRow - innerBeginRow;
                                            if (ySize > rowStep)
                                                ySize = rowStep;
                                            //交集部分在待拼接文件的外包矩形中的位置
                                            int srcBeginRow =
                                                (int) ((blockEnvelope.MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int srcBeginCol =
                                                (int) ((innerCord.MinX - blockEnvelope.MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int srcEndRow = srcBeginRow + ySize;
                                            int srcEndCol = srcBeginCol + xSize;
                                            if (srcEndCol > srcRasters[num].Width)
                                            {
                                                xSize = srcRasters[num].Width - srcBeginCol;
                                                srcEndCol = srcRasters[num].Width;
                                            }

                                            if (srcEndRow > srcRasters[num].Height)
                                            {
                                                ySize = srcRasters[num].Height - srcBeginRow;
                                                srcEndRow = srcRasters[num].Height;
                                            }

                                            long currentBufferSize = xSize * ySize;
                                            //保存现有已拼接的交集部分的值
                                            byte[] currentDatabuffer = new byte[currentBufferSize];
                                            fixed (byte* cptr = currentDatabuffer)
                                            {
                                                IntPtr currentBuffer = new IntPtr(cptr);
                                                dstRaster.GetRasterBand(bandIndex).ReadRaster(innerBeginCol,
                                                    innerBeginRow, xSize, ySize, currentBuffer, xSize, ySize,
                                                    srcRasters[num].DataType, 0, 0);
                                            }

                                            int[] rowColRange = new int[]
                                                {srcBeginRow, srcEndRow, srcBeginCol, srcEndCol, sample};
                                            //无效值处理
                                            byte[] invalids = GetByteValues(invalidValues);
                                            double minute = 0;
                                            //上下五分钟相交区域，取平均值
                                            if (dstRaster.DataIdentify != null && srcRasters[num].DataIdentify != null)
                                            {
                                                DateTime dst = dstRaster.DataIdentify.OrbitDateTime;
                                                DateTime src = srcRasters[num].DataIdentify.OrbitDateTime;
                                                minute = Math.Abs((dst - src).TotalMinutes);
                                            }
                                            //if (minute == 5)
                                            //    InvalidValueProcesser<byte>.ProcessInvalidValue(currentDatabuffer, ref databuffer, rowColRange, invalids, true);
                                            //else

                                            try
                                            {
                                                InvalidValueProcesser<byte>.ProcessInvalidValue(currentDatabuffer,
                                                    ref databuffer, rowColRange, invalids);
                                            }
                                            catch (Exception)
                                            {
                                                Console.WriteLine(
                                                    $"ProcessInvalidValue<byte>() srcImage xSize ： {xSize}  ySize：{ySize}");
                                                Console.WriteLine(
                                                    $"ProcessInvalidValue<byte>() dstImage xSize ： {sample}  ySize：{rowStep}");
                                                Console.WriteLine(
                                                    $"ProcessInvalidValue<byte>() rowColRange：{string.Join(",", rowColRange)}");
                                                throw;
                                            }
                                        }

                                        dstRaster.GetRasterBand(bandIndex).WriteRaster(tBeginCol,
                                            tBeginRow + (oRow - oBeginRow), sample, rowStep, buffer, sample, rowStep,
                                            srcRasters[num].DataType, 0, 0);
                                    }
                                }
                            }

                            break;
                        }

                        #endregion

                        case DataType.GDT_UInt16:

                            #region UInt16

                        {
                            for (int bandIndex = 0; bandIndex < srcRasters[num].BandCount; bandIndex++)
                            {
                                curStep++;
                                int curp = (int) (((persent + curStep * 1.0f / stepCount) / srcRasters.Length) * 100);
                                if (progressCallback != null)
                                    progressCallback(curp, "拼接/镶嵌完成" + curp + "%");

                                int sample = oEndCol - oBeginCol;
                                long bufferSize = sample * rowStep;
                                UInt16[] databuffer = new UInt16[bufferSize];
                                unsafe
                                {
                                    fixed (UInt16* ptr = databuffer)
                                    {
                                        IntPtr buffer = new IntPtr(ptr);
                                        srcRasters[num].GetRasterBand(bandIndex).ReadRaster(oBeginCol, oRow, sample,
                                            rowStep, buffer, sample, rowStep, srcRasters[num].DataType, 0, 0);
                                        //进行无效值处理
                                        if (isInternal && isProcessInvalid)
                                        {
                                            //求出现有拼接文件在此交集范围内的值
                                            //在外包矩形交集范围内寻找无效值
                                            int innerBeginRow =
                                                (int) ((dstRaster.GetEnvelope().MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int innerEndRow = (int) Math.Round(
                                                (dstRaster.GetEnvelope().MaxY - innerCord.MinY) /
                                                srcRasters[num].ResolutionY, 0);
                                            int innerBeginCol =
                                                (int) ((innerCord.MinX - dstRaster.GetEnvelope().MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int innerEndCol = (int) Math.Round(
                                                (innerCord.MaxX - dstRaster.GetEnvelope().MinX) /
                                                srcRasters[num].ResolutionX, 0);
                                            int xSize = innerEndCol - innerBeginCol;
                                            int ySize = innerEndRow - innerBeginRow;
                                            if (ySize > rowStep)
                                                ySize = rowStep;
                                            //交集部分在待拼接文件的外包矩形中的位置
                                            int srcBeginRow =
                                                (int) ((blockEnvelope.MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int srcBeginCol =
                                                (int) ((innerCord.MinX - blockEnvelope.MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int srcEndRow = srcBeginRow + ySize;
                                            int srcEndCol = srcBeginCol + xSize;
                                            if (srcEndCol > srcRasters[num].Width)
                                            {
                                                xSize = srcRasters[num].Width - srcBeginCol;
                                                srcEndCol = srcRasters[num].Width;
                                            }

                                            if (srcEndRow > srcRasters[num].Height)
                                            {
                                                ySize = srcRasters[num].Height - srcBeginRow;
                                                srcEndRow = srcRasters[num].Height;
                                            }

                                            long currentBufferSize = xSize * ySize;
                                            //保存现有已拼接的交集部分的值
                                            UInt16[] currentDatabuffer = new UInt16[currentBufferSize];
                                            fixed (UInt16* cptr = currentDatabuffer)
                                            {
                                                IntPtr currentBuffer = new IntPtr(cptr);
                                                dstRaster.GetRasterBand(bandIndex).ReadRaster(innerBeginCol,
                                                    innerBeginRow, xSize, ySize, currentBuffer, xSize, ySize,
                                                    srcRasters[num].DataType, 0, 0);
                                            }

                                            int[] rowColRange = new int[]
                                                {srcBeginRow, srcEndRow, srcBeginCol, srcEndCol, sample};
                                            //无效值处理
                                            UInt16[] invalids = GetUInt16Values(invalidValues);
                                            double minute = 0;
                                            //上下五分钟相交区域，取平均值
                                            if (dstRaster.DataIdentify != null && srcRasters[num].DataIdentify != null)
                                            {
                                                DateTime dst = dstRaster.DataIdentify.OrbitDateTime;
                                                DateTime src = srcRasters[num].DataIdentify.OrbitDateTime;
                                                minute = Math.Abs((dst - src).TotalMinutes);
                                            }

                                            try
                                            {
                                                if (minute == 5)
                                                    InvalidValueProcesser<UInt16>.ProcessInvalidValue(currentDatabuffer,
                                                        ref databuffer, rowColRange, invalids, true);
                                                else
                                                    InvalidValueProcesser<UInt16>.ProcessInvalidValue(currentDatabuffer,
                                                        ref databuffer, rowColRange, invalids);
                                            }
                                            catch (Exception)
                                            {
                                                Console.WriteLine(
                                                    $"ProcessInvalidValue<UInt16>() srcImage xSize ： {xSize}  ySize：{ySize}");
                                                Console.WriteLine(
                                                    $"ProcessInvalidValue<UInt16>() dstImage xSize ： {sample}  ySize：{rowStep}");
                                                Console.WriteLine(
                                                    $"ProcessInvalidValue<UInt16>() rowColRange：{string.Join(",", rowColRange)}");
                                                throw;
                                            }
                                        }

                                        dstRaster.GetRasterBand(bandIndex).WriteRaster(tBeginCol,
                                            tBeginRow + (oRow - oBeginRow), sample, rowStep, buffer, sample, rowStep,
                                            srcRasters[num].DataType, 0, 0);
                                    }
                                }
                            }

                            break;
                        }

                        #endregion

                        case DataType.GDT_Float32:

                            #region Float32

                        {
                            for (int bandIndex = 1; bandIndex <= srcRasters[num].BandCount; bandIndex++)
                            {
                                int sample = oEndCol - oBeginCol;
                                long bufferSize = sample * rowStep;
                                float[] databuffer = new float[bufferSize];
                                unsafe
                                {
                                    fixed (float* ptr = databuffer)
                                    {
                                        IntPtr buffer = new IntPtr(ptr);
                                        srcRasters[num].GetRasterBand(bandIndex).ReadRaster(oBeginCol, oRow, sample,
                                            rowStep, buffer, sample, rowStep, srcRasters[num].DataType, 0, 0);
                                        //进行无效值处理
                                        if (isInternal && isProcessInvalid)
                                        {
                                            //求出现有拼接文件在此交集范围内的值
                                            //在外包矩形交集范围内寻找无效值
                                            int innerBeginRow =
                                                (int) ((dstRaster.GetEnvelope().MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int innerEndRow = (int) Math.Round(
                                                (dstRaster.GetEnvelope().MaxY - innerCord.MinY) /
                                                srcRasters[num].ResolutionY, 0);
                                            int innerBeginCol =
                                                (int) ((innerCord.MinX - dstRaster.GetEnvelope().MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int innerEndCol = (int) Math.Round(
                                                (innerCord.MaxX - dstRaster.GetEnvelope().MinX) /
                                                srcRasters[num].ResolutionX, 0);
                                            int xSize = innerEndCol - innerBeginCol;
                                            int ySize = innerEndRow - innerBeginRow;
                                            if (ySize > rowStep)
                                                ySize = rowStep;
                                            //交集部分在待拼接文件的外包矩形中的位置
                                            int srcBeginRow =
                                                (int) ((blockEnvelope.MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int srcBeginCol =
                                                (int) ((innerCord.MinX - blockEnvelope.MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int srcEndRow = srcBeginRow + ySize;
                                            int srcEndCol = srcBeginCol + xSize;
                                            if (srcEndCol > srcRasters[num].Width)
                                            {
                                                xSize = srcRasters[num].Width - srcBeginCol;
                                                srcEndCol = srcRasters[num].Width;
                                            }

                                            if (srcEndRow > srcRasters[num].Height)
                                            {
                                                ySize = srcRasters[num].Height - srcBeginRow;
                                                srcEndRow = srcRasters[num].Height;
                                            }

                                            long currentBufferSize = xSize * ySize;
                                            //保存现有已拼接的交集部分的值
                                            float[] currentDatabuffer = new float[currentBufferSize];
                                            fixed (float* cptr = currentDatabuffer)
                                            {
                                                IntPtr currentBuffer = new IntPtr(cptr);
                                                dstRaster.GetRasterBand(bandIndex).ReadRaster(innerBeginCol,
                                                    innerBeginRow, xSize, ySize, currentBuffer, xSize, ySize,
                                                    srcRasters[num].DataType, 0, 0);
                                            }

                                            int[] rowColRange = new int[]
                                                {srcBeginRow, srcEndRow, srcBeginCol, srcEndCol, sample};
                                            //无效值处理
                                            float[] invalids = GetFloatValues(invalidValues);
                                            try
                                            {
                                                InvalidValueProcesser<float>.ProcessInvalidValue(currentDatabuffer,
                                                    ref databuffer, rowColRange, invalids);
                                            }
                                            catch (Exception)
                                            {
                                                Console.WriteLine(
                                                    $"ProcessInvalidValue<float>() srcImage xSize ： {xSize}  ySize：{ySize}");
                                                Console.WriteLine(
                                                    $"ProcessInvalidValue<float>() dstImage xSize ： {sample}  ySize：{rowStep}");
                                                Console.WriteLine(
                                                    $"ProcessInvalidValue<float>() rowColRange：{string.Join(",", rowColRange)}");
                                                throw;
                                            }
                                        }

                                        dstRaster.GetRasterBand(bandIndex).WriteRaster(tBeginCol,
                                            tBeginRow + (oRow - oBeginRow), sample, rowStep, buffer, sample, rowStep,
                                            srcRasters[num].DataType, 0, 0);
                                    }
                                }
                            }

                            break;
                        }

                        #endregion

                        case DataType.GDT_Int16:

                            #region Int16

                        {
                            for (int bandIndex = 1; bandIndex <= srcRasters[num].BandCount; bandIndex++)
                            {
                                curStep++;
                                int curp = (int) (((persent + curStep * 1.0f / stepCount) / srcRasters.Length) * 100);
                                if (progressCallback != null)
                                    progressCallback(curp, "拼接/镶嵌完成" + curp + "%");

                                int sample = oEndCol - oBeginCol;
                                long bufferSize = sample * rowStep;
                                Int16[] databuffer = new Int16[bufferSize];
                                unsafe
                                {
                                    fixed (Int16* ptr = databuffer)
                                    {
                                        IntPtr buffer = new IntPtr(ptr);
                                        srcRasters[num].GetRasterBand(bandIndex).ReadRaster(oBeginCol, oRow, sample,
                                            rowStep, buffer, sample, rowStep, srcRasters[num].DataType, 0, 0);
                                        //进行无效值处理
                                        if (isInternal && isProcessInvalid)
                                        {
                                            //求出现有拼接文件在此交集范围内的值
                                            //在外包矩形交集范围内寻找无效值
                                            int innerBeginRow =
                                                (int) ((dstRaster.GetEnvelope().MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int innerEndRow = (int) Math.Round(
                                                (dstRaster.GetEnvelope().MaxY - innerCord.MinY) /
                                                srcRasters[num].ResolutionY, 0);
                                            int innerBeginCol =
                                                (int) ((innerCord.MinX - dstRaster.GetEnvelope().MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int innerEndCol = (int) Math.Round(
                                                (innerCord.MaxX - dstRaster.GetEnvelope().MinX) /
                                                srcRasters[num].ResolutionX, 0);
                                            int xSize = innerEndCol - innerBeginCol;
                                            int ySize = innerEndRow - innerBeginRow;
                                            if (ySize > rowStep)
                                                ySize = rowStep;
                                            //交集部分在待拼接文件的外包矩形中的位置
                                            int srcBeginRow =
                                                (int) ((blockEnvelope.MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int srcBeginCol =
                                                (int) ((innerCord.MinX - blockEnvelope.MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int srcEndRow = srcBeginRow + ySize;
                                            int srcEndCol = srcBeginCol + xSize;
                                            if (srcEndCol > srcRasters[num].Width)
                                            {
                                                xSize = srcRasters[num].Width - srcBeginCol;
                                                srcEndCol = srcRasters[num].Width;
                                            }

                                            if (srcEndRow > srcRasters[num].Height)
                                            {
                                                ySize = srcRasters[num].Height - srcBeginRow;
                                                srcEndRow = srcRasters[num].Height;
                                            }

                                            long currentBufferSize = xSize * ySize;
                                            //保存现有已拼接的交集部分的值
                                            Int16[] currentDatabuffer = new Int16[currentBufferSize];
                                            fixed (Int16* cptr = currentDatabuffer)
                                            {
                                                IntPtr currentBuffer = new IntPtr(cptr);
                                                dstRaster.GetRasterBand(bandIndex).ReadRaster(innerBeginCol,
                                                    innerBeginRow, xSize, ySize, currentBuffer, xSize, ySize,
                                                    srcRasters[num].DataType, 0, 0);
                                            }

                                            int[] rowColRange = new int[]
                                                {srcBeginRow, srcEndRow, srcBeginCol, srcEndCol, sample};
                                            //无效值处理
                                            //无效值处理
                                            Int16[] invalids = GetInt16Values(invalidValues);
                                            try
                                            {
                                                InvalidValueProcesser<Int16>.ProcessInvalidValue(currentDatabuffer,
                                                    ref databuffer, rowColRange, invalids);
                                            }
                                            catch (Exception)
                                            {
                                                Console.WriteLine(
                                                    $"ProcessInvalidValue<Int16>() srcImage xSize ： {xSize}  ySize：{ySize}");
                                                Console.WriteLine(
                                                    $"ProcessInvalidValue<Int16>() dstImage xSize ： {sample}  ySize：{rowStep}");
                                                Console.WriteLine(
                                                    $"ProcessInvalidValue<Int16>() rowColRange：{string.Join(",", rowColRange)}");
                                                throw;
                                            }
                                        }

                                        dstRaster.GetRasterBand(bandIndex).WriteRaster(tBeginCol,
                                            tBeginRow + (oRow - oBeginRow), sample, rowStep, buffer, sample, rowStep,
                                            srcRasters[num].DataType, 0, 0);
                                    }
                                }
                            }

                            break;
                        }

                        #endregion
                    }

                    #endregion
                }

                //记录已拼接文件外包矩形，以用于无效值处理
                currentEnvelope = currentEnvelope.Union(srcRasters[num].GetEnvelope());
            }

            if (progressCallback != null)
                progressCallback(100, "镶嵌/拼接完成");
        }

        public void Moasic(AbstractWarpDataset[] srcRasters, AbstractWarpDataset dstRaster, int[] bandNos,
            bool isProcessInvalid, string[] invalidValues, Action<int, string> progressCallback,
            params object[] options)
        {
            if (srcRasters == null || srcRasters.Count() == 0)
                return;
            //检查待拼接文件波段数、数据类型、空间参考是否相等
            if (RasterMoasicChecker.CheckBandCount(srcRasters) == false ||
                RasterMoasicChecker.CheckDataType(srcRasters) == false ||
                RasterMoasicChecker.CheckSpatialRef(srcRasters) == false)
                return;
            DataType dataType = srcRasters[0].DataType;
            if (progressCallback != null)
                progressCallback(0, "开始镶嵌/拼接");
            Envelope currentEnvelope = dstRaster.GetEnvelope().Intersection(srcRasters[0].GetEnvelope());
            for (int num = 0; num < srcRasters.Length; num++)
            {
                float persent = num * 1.0f;
                int oBeginRow = -1, oBeginCol = -1, oEndRow = -1, oEndCol = -1;
                int tBeginRow = -1, tBeginCol = -1, tEndRow = -1, tEndCol = -1;
                bool isInter = new RasterMoasicClipHelper().ComputeBeginEndRowCol(srcRasters[num], dstRaster,
                    new Size(dstRaster.Width, dstRaster.Height), ref oBeginRow, ref oBeginCol,
                    ref oEndRow, ref oEndCol, ref tBeginRow, ref tBeginCol, ref tEndRow, ref tEndCol);
                if (!isInter)
                {
                    int curp = (int) (((persent + 1.0f) / srcRasters.Length) * 100);
                    if (progressCallback != null)
                        progressCallback(curp, "拼接/镶嵌完成" + curp + "%");
                    continue;
                }

                int rowStep = ClipCutHelper.ComputeRowStep(srcRasters[num], oBeginRow, oEndRow);
                int stepCount = (int) ((oEndRow - oBeginRow) / rowStep + 0.5f) * srcRasters[num].BandCount;
                int curStep = -1;
                for (int oRow = oBeginRow; oRow < oEndRow; oRow += rowStep)
                {
                    if (oRow + rowStep > oEndRow)
                        rowStep = oEndRow - oRow;
                    //计算待拼接文件块的外包矩形(XMin/XMax与原待拼接文件外包矩形一致)
                    Envelope blockEnvelope = GetBlockEnvelope(srcRasters[num], oRow, oRow + rowStep);
                    //与当前外包矩形(已拼接完成部分)是否有交集
                    bool isInternal;
                    //待拼接文件外包矩形与现外包矩形的交集
                    Envelope innerCord = GetInternalEnvelope(currentEnvelope, num, blockEnvelope, out isInternal);
                    switch (dataType)
                    {
                        case DataType.GDT_UInt16:

                            #region

                        {
                            int bandNo = 1;
                            foreach (int bandIndex in bandNos)
                            {
                                curStep++;
                                int curp = (int) (((persent + curStep * 1.0f / stepCount) / srcRasters.Length) * 100);
                                if (progressCallback != null)
                                    progressCallback(curp, "拼接/镶嵌完成" + curp + "%");

                                int sample = oEndCol - oBeginCol;
                                long bufferSize = sample * rowStep;
                                UInt16[] databuffer = new UInt16[bufferSize];
                                unsafe
                                {
                                    fixed (UInt16* ptr = databuffer)
                                    {
                                        IntPtr buffer = new IntPtr(ptr);
                                        srcRasters[num].GetRasterBand(bandIndex).ReadRaster(oBeginCol, oRow, sample,
                                            rowStep, buffer, sample, rowStep, srcRasters[num].DataType, 0, 0);
                                        //进行无效值处理
                                        if (isInternal && isProcessInvalid)
                                        {
                                            //求出现有拼接文件在此交集范围内的值
                                            //在外包矩形交集范围内寻找无效值
                                            int innerBeginRow =
                                                (int) ((dstRaster.GetEnvelope().MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int innerEndRow = (int) Math.Round(
                                                (dstRaster.GetEnvelope().MaxY - innerCord.MinY) /
                                                srcRasters[num].ResolutionY, 0);
                                            int innerBeginCol =
                                                (int) ((innerCord.MinX - dstRaster.GetEnvelope().MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int innerEndCol = (int) Math.Round(
                                                (innerCord.MaxX - dstRaster.GetEnvelope().MinX) /
                                                srcRasters[num].ResolutionX, 0);
                                            int xSize = innerEndCol - innerBeginCol;
                                            int ySize = innerEndRow - innerBeginRow;
                                            if (ySize > rowStep)
                                                ySize = rowStep;
                                            //交集部分在待拼接文件的外包矩形中的位置
                                            int srcBeginRow =
                                                (int) ((blockEnvelope.MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int srcBeginCol =
                                                (int) ((innerCord.MinX - blockEnvelope.MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int srcEndRow = srcBeginRow + ySize;
                                            int srcEndCol = srcBeginCol + xSize;
                                            if (srcEndCol > srcRasters[num].Width)
                                            {
                                                xSize = srcRasters[num].Width - srcBeginCol;
                                                srcEndCol = srcRasters[num].Width;
                                            }

                                            if (srcEndRow > srcRasters[num].Height)
                                            {
                                                ySize = srcRasters[num].Height - srcBeginRow;
                                                srcEndRow = srcRasters[num].Height;
                                            }

                                            long currentBufferSize = xSize * ySize;
                                            //保存现有已拼接的交集部分的值
                                            UInt16[] currentDatabuffer = new UInt16[currentBufferSize];
                                            fixed (UInt16* cptr = currentDatabuffer)
                                            {
                                                IntPtr currentBuffer = new IntPtr(cptr);
                                                dstRaster.GetRasterBand(bandIndex).ReadRaster(innerBeginCol,
                                                    innerBeginRow, xSize, ySize, currentBuffer, xSize, ySize,
                                                    srcRasters[num].DataType, 0, 0);
                                            }

                                            int[] rowColRange = new int[]
                                                {srcBeginRow, srcEndRow, srcBeginCol, srcEndCol, sample};
                                            //无效值处理
                                            UInt16[] invalids = GetUInt16Values(invalidValues);
                                            InvalidValueProcesser<UInt16>.ProcessInvalidValue(currentDatabuffer,
                                                ref databuffer, rowColRange, invalids);
                                        }

                                        dstRaster.GetRasterBand(bandNo++).WriteRaster(tBeginCol,
                                            tBeginRow + (oRow - oBeginRow), sample, rowStep, buffer, sample, rowStep,
                                            srcRasters[num].DataType, 0, 0);
                                    }
                                }
                            }

                            break;
                        }

                        #endregion

                        case DataType.GDT_Float32:

                            #region

                        {
                            int bandNo = 1;
                            foreach (int bandIndex in bandNos)
                            {
                                int sample = oEndCol - oBeginCol;
                                long bufferSize = sample * rowStep;
                                float[] databuffer = new float[bufferSize];
                                unsafe
                                {
                                    fixed (float* ptr = databuffer)
                                    {
                                        IntPtr buffer = new IntPtr(ptr);
                                        srcRasters[num].GetRasterBand(bandIndex).ReadRaster(oBeginCol, oRow, sample,
                                            rowStep, buffer, sample, rowStep, srcRasters[num].DataType, 0, 0);
                                        //进行无效值处理
                                        if (isInternal && isProcessInvalid)
                                        {
                                            //求出现有拼接文件在此交集范围内的值
                                            //在外包矩形交集范围内寻找无效值
                                            int innerBeginRow =
                                                (int) ((dstRaster.GetEnvelope().MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int innerEndRow = (int) Math.Round(
                                                (dstRaster.GetEnvelope().MaxY - innerCord.MinY) /
                                                srcRasters[num].ResolutionY, 0);
                                            int innerBeginCol =
                                                (int) ((innerCord.MinX - dstRaster.GetEnvelope().MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int innerEndCol = (int) Math.Round(
                                                (innerCord.MaxX - dstRaster.GetEnvelope().MinX) /
                                                srcRasters[num].ResolutionX, 0);
                                            int xSize = innerEndCol - innerBeginCol;
                                            int ySize = innerEndRow - innerBeginRow;
                                            if (ySize > rowStep)
                                                ySize = rowStep;
                                            //交集部分在待拼接文件的外包矩形中的位置
                                            int srcBeginRow =
                                                (int) ((blockEnvelope.MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int srcBeginCol =
                                                (int) ((innerCord.MinX - blockEnvelope.MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int srcEndRow = srcBeginRow + ySize;
                                            int srcEndCol = srcBeginCol + xSize;
                                            if (srcEndCol > srcRasters[num].Width)
                                            {
                                                xSize = srcRasters[num].Width - srcBeginCol;
                                                srcEndCol = srcRasters[num].Width;
                                            }

                                            if (srcEndRow > srcRasters[num].Height)
                                            {
                                                ySize = srcRasters[num].Height - srcBeginRow;
                                                srcEndRow = srcRasters[num].Height;
                                            }

                                            long currentBufferSize = xSize * ySize;
                                            //保存现有已拼接的交集部分的值
                                            float[] currentDatabuffer = new float[currentBufferSize];
                                            fixed (float* cptr = currentDatabuffer)
                                            {
                                                IntPtr currentBuffer = new IntPtr(cptr);
                                                dstRaster.GetRasterBand(bandIndex).ReadRaster(innerBeginCol,
                                                    innerBeginRow, xSize, ySize, currentBuffer, xSize, ySize,
                                                    srcRasters[num].DataType, 0, 0);
                                            }

                                            int[] rowColRange = new int[]
                                                {srcBeginRow, srcEndRow, srcBeginCol, srcEndCol, sample};
                                            //无效值处理
                                            float[] invalids = GetFloatValues(invalidValues);
                                            InvalidValueProcesser<float>.ProcessInvalidValue(currentDatabuffer,
                                                ref databuffer, rowColRange, invalids);
                                        }

                                        dstRaster.GetRasterBand(bandNo++).WriteRaster(tBeginCol,
                                            tBeginRow + (oRow - oBeginRow), sample, rowStep, buffer, sample, rowStep,
                                            srcRasters[num].DataType, 0, 0);
                                    }
                                }
                            }

                            break;
                        }

                        #endregion

                        case DataType.GDT_Byte:

                            #region

                        {
                            int bandNo = 1;
                            foreach (int bandIndex in bandNos)
                            {
                                curStep++;
                                int curp = (int) (((persent + curStep * 1.0f / stepCount) / srcRasters.Length) * 100);
                                if (progressCallback != null)
                                    progressCallback(curp, "拼接/镶嵌完成" + curp + "%");

                                int sample = oEndCol - oBeginCol;
                                long bufferSize = sample * rowStep;
                                Byte[] databuffer = new Byte[bufferSize];
                                unsafe
                                {
                                    fixed (Byte* ptr = databuffer)
                                    {
                                        IntPtr buffer = new IntPtr(ptr);
                                        srcRasters[num].GetRasterBand(bandIndex).ReadRaster(oBeginCol, oRow, sample,
                                            rowStep, buffer, sample, rowStep, srcRasters[num].DataType, 0, 0);
                                        //进行无效值处理
                                        if (isInternal && isProcessInvalid)
                                        {
                                            //求出现有拼接文件在此交集范围内的值
                                            //在外包矩形交集范围内寻找无效值
                                            int innerBeginRow =
                                                (int) ((dstRaster.GetEnvelope().MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int innerEndRow = (int) Math.Round(
                                                (dstRaster.GetEnvelope().MaxY - innerCord.MinY) /
                                                srcRasters[num].ResolutionY, 0);
                                            int innerBeginCol =
                                                (int) ((innerCord.MinX - dstRaster.GetEnvelope().MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int innerEndCol = (int) Math.Round(
                                                (innerCord.MaxX - dstRaster.GetEnvelope().MinX) /
                                                srcRasters[num].ResolutionX, 0);
                                            int xSize = innerEndCol - innerBeginCol;
                                            int ySize = innerEndRow - innerBeginRow;
                                            if (ySize > rowStep)
                                                ySize = rowStep;
                                            //交集部分在待拼接文件的外包矩形中的位置
                                            int srcBeginRow =
                                                (int) ((blockEnvelope.MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int srcBeginCol =
                                                (int) ((innerCord.MinX - blockEnvelope.MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int srcEndRow = srcBeginRow + ySize;
                                            int srcEndCol = srcBeginCol + xSize;
                                            if (srcEndCol > srcRasters[num].Width)
                                            {
                                                xSize = srcRasters[num].Width - srcBeginCol;
                                                srcEndCol = srcRasters[num].Width;
                                            }

                                            if (srcEndRow > srcRasters[num].Height)
                                            {
                                                ySize = srcRasters[num].Height - srcBeginRow;
                                                srcEndRow = srcRasters[num].Height;
                                            }

                                            long currentBufferSize = xSize * ySize;
                                            //保存现有已拼接的交集部分的值
                                            Byte[] currentDatabuffer = new Byte[currentBufferSize];
                                            fixed (Byte* cptr = currentDatabuffer)
                                            {
                                                IntPtr currentBuffer = new IntPtr(cptr);
                                                dstRaster.GetRasterBand(bandIndex).ReadRaster(innerBeginCol,
                                                    innerBeginRow, xSize, ySize, currentBuffer, xSize, ySize,
                                                    srcRasters[num].DataType, 0, 0);
                                            }

                                            int[] rowColRange = new int[]
                                                {srcBeginRow, srcEndRow, srcBeginCol, srcEndCol, sample};
                                            //无效值处理
                                            Byte[] invalids = GetByteValues(invalidValues);
                                            InvalidValueProcesser<Byte>.ProcessInvalidValue(currentDatabuffer,
                                                ref databuffer, rowColRange, invalids);
                                        }

                                        dstRaster.GetRasterBand(bandNo++).WriteRaster(tBeginCol,
                                            tBeginRow + (oRow - oBeginRow), sample, rowStep, buffer, sample, rowStep,
                                            srcRasters[num].DataType, 0, 0);
                                    }
                                }
                            }

                            break;
                        }

                        #endregion

                        case DataType.GDT_Int16:

                            #region

                        {
                            int bandNo = 1;
                            foreach (int bandIndex in bandNos)
                            {
                                curStep++;
                                int curp = (int) (((persent + curStep * 1.0f / stepCount) / srcRasters.Length) * 100);
                                if (progressCallback != null)
                                    progressCallback(curp, "拼接/镶嵌完成" + curp + "%");

                                int sample = oEndCol - oBeginCol;
                                long bufferSize = sample * rowStep;
                                Int16[] databuffer = new Int16[bufferSize];
                                unsafe
                                {
                                    fixed (Int16* ptr = databuffer)
                                    {
                                        IntPtr buffer = new IntPtr(ptr);
                                        srcRasters[num].GetRasterBand(bandIndex).ReadRaster(oBeginCol, oRow, sample,
                                            rowStep, buffer, sample, rowStep, srcRasters[num].DataType, 0, 0);
                                        //进行无效值处理
                                        if (isInternal && isProcessInvalid)
                                        {
                                            //求出现有拼接文件在此交集范围内的值
                                            //在外包矩形交集范围内寻找无效值
                                            int innerBeginRow =
                                                (int) ((dstRaster.GetEnvelope().MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int innerEndRow = (int) Math.Round(
                                                (dstRaster.GetEnvelope().MaxY - innerCord.MinY) /
                                                srcRasters[num].ResolutionY, 0);
                                            int innerBeginCol =
                                                (int) ((innerCord.MinX - dstRaster.GetEnvelope().MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int innerEndCol = (int) Math.Round(
                                                (innerCord.MaxX - dstRaster.GetEnvelope().MinX) /
                                                srcRasters[num].ResolutionX, 0);
                                            int xSize = innerEndCol - innerBeginCol;
                                            int ySize = innerEndRow - innerBeginRow;
                                            if (ySize > rowStep)
                                                ySize = rowStep;
                                            //交集部分在待拼接文件的外包矩形中的位置
                                            int srcBeginRow =
                                                (int) ((blockEnvelope.MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int srcBeginCol =
                                                (int) ((innerCord.MinX - blockEnvelope.MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int srcEndRow = srcBeginRow + ySize;
                                            int srcEndCol = srcBeginCol + xSize;
                                            if (srcEndCol > srcRasters[num].Width)
                                            {
                                                xSize = srcRasters[num].Width - srcBeginCol;
                                                srcEndCol = srcRasters[num].Width;
                                            }

                                            if (srcEndRow > srcRasters[num].Height)
                                            {
                                                ySize = srcRasters[num].Height - srcBeginRow;
                                                srcEndRow = srcRasters[num].Height;
                                            }

                                            long currentBufferSize = xSize * ySize;
                                            //保存现有已拼接的交集部分的值
                                            Int16[] currentDatabuffer = new Int16[currentBufferSize];
                                            fixed (Int16* cptr = currentDatabuffer)
                                            {
                                                IntPtr currentBuffer = new IntPtr(cptr);
                                                dstRaster.GetRasterBand(bandIndex).ReadRaster(innerBeginCol,
                                                    innerBeginRow, xSize, ySize, currentBuffer, xSize, ySize,
                                                    srcRasters[num].DataType, 0, 0);
                                            }

                                            int[] rowColRange = new int[]
                                                {srcBeginRow, srcEndRow, srcBeginCol, srcEndCol, sample};
                                            //无效值处理
                                            Int16[] invalids = GetInt16Values(invalidValues);
                                            InvalidValueProcesser<Int16>.ProcessInvalidValue(currentDatabuffer,
                                                ref databuffer, rowColRange, invalids);
                                        }

                                        dstRaster.GetRasterBand(bandNo++).WriteRaster(tBeginCol,
                                            tBeginRow + (oRow - oBeginRow), sample, rowStep, buffer, sample, rowStep,
                                            srcRasters[num].DataType, 0, 0);
                                    }
                                }
                            }

                            break;
                        }

                        #endregion

                        case DataType.GDT_Int32:

                            #region

                        {
                            int bandNo = 1;
                            foreach (int bandIndex in bandNos)
                            {
                                curStep++;
                                int curp = (int) (((persent + curStep * 1.0f / stepCount) / srcRasters.Length) * 100);
                                if (progressCallback != null)
                                    progressCallback(curp, "拼接/镶嵌完成" + curp + "%");

                                int sample = oEndCol - oBeginCol;
                                long bufferSize = sample * rowStep;
                                Int32[] databuffer = new Int32[bufferSize];
                                unsafe
                                {
                                    fixed (Int32* ptr = databuffer)
                                    {
                                        IntPtr buffer = new IntPtr(ptr);
                                        srcRasters[num].GetRasterBand(bandIndex).ReadRaster(oBeginCol, oRow, sample,
                                            rowStep, buffer, sample, rowStep, srcRasters[num].DataType, 0, 0);
                                        //进行无效值处理
                                        if (isInternal && isProcessInvalid)
                                        {
                                            //求出现有拼接文件在此交集范围内的值
                                            //在外包矩形交集范围内寻找无效值
                                            int innerBeginRow =
                                                (int) ((dstRaster.GetEnvelope().MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int innerEndRow = (int) Math.Round(
                                                (dstRaster.GetEnvelope().MaxY - innerCord.MinY) /
                                                srcRasters[num].ResolutionY, 0);
                                            int innerBeginCol =
                                                (int) ((innerCord.MinX - dstRaster.GetEnvelope().MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int innerEndCol = (int) Math.Round(
                                                (innerCord.MaxX - dstRaster.GetEnvelope().MinX) /
                                                srcRasters[num].ResolutionX, 0);
                                            int xSize = innerEndCol - innerBeginCol;
                                            int ySize = innerEndRow - innerBeginRow;
                                            if (ySize > rowStep)
                                                ySize = rowStep;
                                            //交集部分在待拼接文件的外包矩形中的位置
                                            int srcBeginRow =
                                                (int) ((blockEnvelope.MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int srcBeginCol =
                                                (int) ((innerCord.MinX - blockEnvelope.MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int srcEndRow = srcBeginRow + ySize;
                                            int srcEndCol = srcBeginCol + xSize;
                                            if (srcEndCol > srcRasters[num].Width)
                                            {
                                                xSize = srcRasters[num].Width - srcBeginCol;
                                                srcEndCol = srcRasters[num].Width;
                                            }

                                            if (srcEndRow > srcRasters[num].Height)
                                            {
                                                ySize = srcRasters[num].Height - srcBeginRow;
                                                srcEndRow = srcRasters[num].Height;
                                            }

                                            long currentBufferSize = xSize * ySize;
                                            //保存现有已拼接的交集部分的值
                                            Int32[] currentDatabuffer = new Int32[currentBufferSize];
                                            fixed (Int32* cptr = currentDatabuffer)
                                            {
                                                IntPtr currentBuffer = new IntPtr(cptr);
                                                dstRaster.GetRasterBand(bandIndex).ReadRaster(innerBeginCol,
                                                    innerBeginRow, xSize, ySize, currentBuffer, xSize, ySize,
                                                    srcRasters[num].DataType, 0, 0);
                                            }

                                            int[] rowColRange = new int[]
                                                {srcBeginRow, srcEndRow, srcBeginCol, srcEndCol, sample};
                                            //无效值处理
                                            Int32[] invalids = GetInt32Values(invalidValues);
                                            InvalidValueProcesser<Int32>.ProcessInvalidValue(currentDatabuffer,
                                                ref databuffer, rowColRange, invalids);
                                        }

                                        dstRaster.GetRasterBand(bandNo++).WriteRaster(tBeginCol,
                                            tBeginRow + (oRow - oBeginRow), sample, rowStep, buffer, sample, rowStep,
                                            srcRasters[num].DataType, 0, 0);
                                    }
                                }
                            }

                            break;
                        }

                        #endregion
                    }
                }

                //记录已拼接文件外包矩形，以用于无效值处理
                currentEnvelope = currentEnvelope.Union(srcRasters[num].GetEnvelope());
            }

            if (progressCallback != null)
                progressCallback(100, "镶嵌/拼接完成");
        }

        public void Moasic<T>(AbstractWarpDataset[] srcRasters, AbstractWarpDataset dstRaster, string outDir,
            bool isProcessInvalid, string[] invalidValues, string processMethod, Action<int, string> progressCallback,
            params object[] options)
        {
            if (srcRasters == null || srcRasters.Count() == 0)
                return;
            if (RasterMoasicChecker.CheckBandCount(srcRasters) == false ||
                RasterMoasicChecker.CheckDataType(srcRasters) == false ||
                RasterMoasicChecker.CheckSpatialRef(srcRasters) == false)
                return;
            if (progressCallback != null)
                progressCallback(0, "开始镶嵌/拼接");
            DataType dataType = srcRasters[0].DataType;
            Envelope currentEnvelope = srcRasters[0].GetEnvelope();
            for (int num = 0; num < srcRasters.Length; num++)
            {
                float persent = (num + 1.0f) / srcRasters.Length;
                //if (progressCallback != null)
                //    progressCallback((int)((num + 1.0f) / srcRasters.Length * 100), string.Format("正在拼接/镶嵌第{0}/{1}个文件...", (num + 1).ToString(), srcRasters.Length));
                int oBeginRow = -1, oBeginCol = -1, oEndRow = -1, oEndCol = -1;
                int tBeginRow = -1, tBeginCol = -1, tEndRow = -1, tEndCol = -1;
                new RasterMoasicClipHelper().ComputeBeginEndRowCol(srcRasters[num], dstRaster,
                    new Size(dstRaster.Width, dstRaster.Height), ref oBeginRow, ref oBeginCol, ref oEndRow, ref oEndCol,
                    ref tBeginRow, ref tBeginCol, ref tEndRow, ref tEndCol);
                int rowStep = ClipCutHelper.ComputeRowStep(srcRasters[num], oBeginRow, oEndRow);
                for (int oRow = oBeginRow; oRow < oEndRow; oRow += rowStep)
                {
                    if (oRow + rowStep > oEndRow)
                        rowStep = oEndRow - oRow;
                    //计算待拼接文件块的外包矩形(XMin/XMax与原待拼接文件外包矩形一致)                 
                    Envelope blockEnvelope = GetBlockEnvelope(srcRasters[num], oRow, oRow + rowStep);
                    //与当前外包矩形(已拼接完成部分)是否有交集
                    bool isInternal;
                    //待拼接文件外包矩形与现外包矩形的交集
                    Envelope innerCord = GetInternalEnvelope(currentEnvelope, num, blockEnvelope, out isInternal);
                    switch (dataType)
                    {
                        case DataType.GDT_UInt16:

                            #region

                        {
                            for (int bandIndex = 1; bandIndex <= srcRasters[num].BandCount; bandIndex++)
                            {
                                int sample = oEndCol - oBeginCol;
                                int typeSize = ClipCutHelper.GetSize(srcRasters[num].DataType);
                                long bufferSize = sample * rowStep;
                                UInt16[] databuffer = new UInt16[bufferSize];
                                unsafe
                                {
                                    fixed (UInt16* ptr = databuffer)
                                    {
                                        IntPtr buffer = new IntPtr(ptr);
                                        srcRasters[num].GetRasterBand(bandIndex).ReadRaster(oBeginCol, oRow, sample,
                                            rowStep, buffer, sample, rowStep, srcRasters[num].DataType, 0, 0);
                                        //进行无效值处理
                                        if (isInternal && isProcessInvalid)
                                        {
                                            //求出现有拼接文件在此交集范围内的值
                                            //在外包矩形交集范围内寻找无效值
                                            int innerBeginRow =
                                                (int) ((dstRaster.GetEnvelope().MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int innerEndRow = (int) Math.Round(
                                                (dstRaster.GetEnvelope().MaxY - innerCord.MinY) /
                                                srcRasters[num].ResolutionY, 0);
                                            int innerBeginCol =
                                                (int) ((innerCord.MinX - dstRaster.GetEnvelope().MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int innerEndCol = (int) Math.Round(
                                                (innerCord.MaxX - dstRaster.GetEnvelope().MinX) /
                                                srcRasters[num].ResolutionX, 0);
                                            int xSize = innerEndCol - innerBeginCol;
                                            int ySize = innerEndRow - innerBeginRow;
                                            if (ySize > rowStep)
                                                ySize = rowStep;
                                            //交集部分在待拼接文件的外包矩形中的位置
                                            int srcBeginRow =
                                                (int) ((blockEnvelope.MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int srcBeginCol =
                                                (int) ((innerCord.MinX - blockEnvelope.MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int srcEndRow = srcBeginRow + ySize;
                                            int srcEndCol = srcBeginCol + xSize;
                                            if (srcEndCol > srcRasters[num].Width)
                                            {
                                                xSize = srcRasters[num].Width - srcBeginCol;
                                                srcEndCol = srcRasters[num].Width;
                                            }

                                            if (srcEndRow > srcRasters[num].Height)
                                            {
                                                ySize = srcRasters[num].Height - srcBeginRow;
                                                srcEndRow = srcRasters[num].Height;
                                            }

                                            long currentBufferSize = xSize * ySize;
                                            //保存现有已拼接的交集部分的值
                                            UInt16[] currentDatabuffer = new UInt16[currentBufferSize];
                                            fixed (UInt16* cptr = currentDatabuffer)
                                            {
                                                IntPtr currentBuffer = new IntPtr(cptr);
                                                dstRaster.GetRasterBand(bandIndex).ReadRaster(innerBeginCol,
                                                    innerBeginRow, xSize, ySize, currentBuffer, xSize, ySize,
                                                    srcRasters[num].DataType, 0, 0);
                                            }

                                            int[] rowColRange = new int[]
                                                {srcBeginRow, srcEndRow, srcBeginCol, srcEndCol, sample};
                                            //无效值处理
                                            UInt16[] invalids = GetUInt16Values(invalidValues);
                                            InvalidValueProcesser<UInt16>.ProcessInvalidValue(currentDatabuffer,
                                                ref databuffer, rowColRange, invalids, processMethod,
                                                (srcValue, dstValue) =>
                                                {
                                                    return (UInt16) ((dstValue + srcValue) / 2);
                                                });
                                        }

                                        dstRaster.GetRasterBand(bandIndex).WriteRaster(tBeginCol,
                                            tBeginRow + (oRow - oBeginRow), sample, rowStep, buffer, sample, rowStep,
                                            srcRasters[num].DataType, 0, 0);
                                    }
                                }
                            }

                            break;
                        }

                        #endregion

                        case DataType.GDT_Float32:

                            #region

                        {
                            for (int bandIndex = 1; bandIndex <= srcRasters[num].BandCount; bandIndex++)
                            {
                                int sample = oEndCol - oBeginCol;
                                int typeSize = ClipCutHelper.GetSize(srcRasters[num].DataType);
                                long bufferSize = sample * rowStep;
                                float[] databuffer = new float[bufferSize];
                                unsafe
                                {
                                    fixed (float* ptr = databuffer)
                                    {
                                        IntPtr buffer = new IntPtr(ptr);
                                        srcRasters[num].GetRasterBand(bandIndex).ReadRaster(oBeginCol, oRow, sample,
                                            rowStep, buffer, sample, rowStep, srcRasters[num].DataType, 0, 0);
                                        //进行无效值处理
                                        if (isInternal && isProcessInvalid)
                                        {
                                            //求出现有拼接文件在此交集范围内的值
                                            //在外包矩形交集范围内寻找无效值
                                            int innerBeginRow =
                                                (int) ((dstRaster.GetEnvelope().MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int innerEndRow = (int) Math.Round(
                                                (dstRaster.GetEnvelope().MaxY - innerCord.MinY) /
                                                srcRasters[num].ResolutionY, 0);
                                            int innerBeginCol =
                                                (int) ((innerCord.MinX - dstRaster.GetEnvelope().MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int innerEndCol = (int) Math.Round(
                                                (innerCord.MaxX - dstRaster.GetEnvelope().MinX) /
                                                srcRasters[num].ResolutionX, 0);
                                            int xSize = innerEndCol - innerBeginCol;
                                            int ySize = innerEndRow - innerBeginRow;
                                            if (ySize > rowStep)
                                                ySize = rowStep;
                                            //交集部分在待拼接文件的外包矩形中的位置
                                            int srcBeginRow =
                                                (int) ((blockEnvelope.MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int srcBeginCol =
                                                (int) ((innerCord.MinX - blockEnvelope.MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int srcEndRow = srcBeginRow + ySize;
                                            int srcEndCol = srcBeginCol + xSize;
                                            if (srcEndCol > srcRasters[num].Width)
                                            {
                                                xSize = srcRasters[num].Width - srcBeginCol;
                                                srcEndCol = srcRasters[num].Width;
                                            }

                                            if (srcEndRow > srcRasters[num].Height)
                                            {
                                                ySize = srcRasters[num].Height - srcBeginRow;
                                                srcEndRow = srcRasters[num].Height;
                                            }

                                            long currentBufferSize = xSize * ySize;
                                            //保存现有已拼接的交集部分的值
                                            float[] currentDatabuffer = new float[currentBufferSize];
                                            fixed (float* cptr = currentDatabuffer)
                                            {
                                                IntPtr currentBuffer = new IntPtr(cptr);
                                                dstRaster.GetRasterBand(bandIndex).ReadRaster(innerBeginCol,
                                                    innerBeginRow, xSize, ySize, currentBuffer, xSize, ySize,
                                                    srcRasters[num].DataType, 0, 0);
                                            }

                                            int[] rowColRange = new int[]
                                                {srcBeginRow, srcEndRow, srcBeginCol, srcEndCol, sample};
                                            //无效值处理
                                            float[] invalids = GetFloatValues(invalidValues);
                                            InvalidValueProcesser<float>.ProcessInvalidValue(currentDatabuffer,
                                                ref databuffer, rowColRange, invalids, processMethod,
                                                (srcValue, dstValue) =>
                                                {
                                                    return (float) ((dstValue + srcValue) / 2f);
                                                });
                                        }

                                        dstRaster.GetRasterBand(bandIndex).WriteRaster(tBeginCol,
                                            tBeginRow + (oRow - oBeginRow), sample, rowStep, buffer, sample, rowStep,
                                            srcRasters[num].DataType, 0, 0);
                                    }
                                }
                            }

                            break;
                        }

                        #endregion

                        case DataType.GDT_Int16:

                            #region

                        {
                            for (int bandIndex = 1; bandIndex <= srcRasters[num].BandCount; bandIndex++)
                            {
                                int sample = oEndCol - oBeginCol;
                                int typeSize = ClipCutHelper.GetSize(srcRasters[num].DataType);
                                long bufferSize = sample * rowStep;
                                Int16[] databuffer = new Int16[bufferSize];
                                unsafe
                                {
                                    fixed (Int16* ptr = databuffer)
                                    {
                                        IntPtr buffer = new IntPtr(ptr);
                                        srcRasters[num].GetRasterBand(bandIndex).ReadRaster(oBeginCol, oRow, sample,
                                            rowStep, buffer, sample, rowStep, srcRasters[num].DataType, 0, 0);
                                        //进行无效值处理
                                        if (isInternal && isProcessInvalid)
                                        {
                                            //求出现有拼接文件在此交集范围内的值
                                            //在外包矩形交集范围内寻找无效值
                                            int innerBeginRow =
                                                (int) ((dstRaster.GetEnvelope().MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int innerEndRow = (int) Math.Round(
                                                (dstRaster.GetEnvelope().MaxY - innerCord.MinY) /
                                                srcRasters[num].ResolutionY, 0);
                                            int innerBeginCol =
                                                (int) ((innerCord.MinX - dstRaster.GetEnvelope().MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int innerEndCol = (int) Math.Round(
                                                (innerCord.MaxX - dstRaster.GetEnvelope().MinX) /
                                                srcRasters[num].ResolutionX, 0);
                                            int xSize = innerEndCol - innerBeginCol;
                                            int ySize = innerEndRow - innerBeginRow;
                                            if (ySize > rowStep)
                                                ySize = rowStep;
                                            //交集部分在待拼接文件的外包矩形中的位置
                                            int srcBeginRow =
                                                (int) ((blockEnvelope.MaxY - innerCord.MaxY) /
                                                       srcRasters[num].ResolutionY);
                                            int srcBeginCol =
                                                (int) ((innerCord.MinX - blockEnvelope.MinX) /
                                                       srcRasters[num].ResolutionX);
                                            int srcEndRow = srcBeginRow + ySize;
                                            int srcEndCol = srcBeginCol + xSize;
                                            if (srcEndCol > srcRasters[num].Width)
                                            {
                                                xSize = srcRasters[num].Width - srcBeginCol;
                                                srcEndCol = srcRasters[num].Width;
                                            }

                                            if (srcEndRow > srcRasters[num].Height)
                                            {
                                                ySize = srcRasters[num].Height - srcBeginRow;
                                                srcEndRow = srcRasters[num].Height;
                                            }

                                            long currentBufferSize = xSize * ySize;
                                            //保存现有已拼接的交集部分的值
                                            Int16[] currentDatabuffer = new Int16[currentBufferSize];
                                            fixed (Int16* cptr = currentDatabuffer)
                                            {
                                                IntPtr currentBuffer = new IntPtr(cptr);
                                                dstRaster.GetRasterBand(bandIndex).ReadRaster(innerBeginCol,
                                                    innerBeginRow, xSize, ySize, currentBuffer, xSize, ySize,
                                                    srcRasters[num].DataType, 0, 0);
                                            }

                                            int[] rowColRange = new int[]
                                                {srcBeginRow, srcEndRow, srcBeginCol, srcEndCol, sample};
                                            //无效值处理
                                            Int16[] invalids = GetInt16Values(invalidValues);
                                            InvalidValueProcesser<Int16>.ProcessInvalidValue(currentDatabuffer,
                                                ref databuffer, rowColRange, invalids, processMethod,
                                                (srcValue, dstValue) =>
                                                {
                                                    return (Int16) ((dstValue + srcValue) / 2);
                                                });
                                        }

                                        dstRaster.GetRasterBand(bandIndex).WriteRaster(tBeginCol,
                                            tBeginRow + (oRow - oBeginRow), sample, rowStep, buffer, sample, rowStep,
                                            srcRasters[num].DataType, 0, 0);
                                    }
                                }
                            }

                            break;
                        }

                        #endregion
                    }
                }

                //记录已拼接文件外包矩形，以用于无效值处理
                currentEnvelope = currentEnvelope.Union(srcRasters[num].GetEnvelope());
            }

            if (progressCallback != null)
                progressCallback(100, "镶嵌/拼接完成");
        }

        public void Moasic(VBand[] srcBands, VBand dstBand, Action<int, string> progressCallback,
            params object[] options)
        {
            if (srcBands == null || srcBands.Count() == 0)
            {
                return;
            }

            if (dstBand == null)
            {
            }
            else
            {
                progressCallback(0, "开始镶嵌/拼接");
                for (int num = 0; num < srcBands.Length; num++)
                {
                    if (progressCallback != null)
                        progressCallback((num + 1) / srcBands.Length * 100,
                            string.Format("正在镶嵌/拼接第{0}/{1}个文件...", (num + 1).ToString(), srcBands.Length));
                    int oBeginRow = 0, oBeginCol = 0, oEndRow = 0, oEndCol = 0;
                    int tBeginRow = 0, tBeginCol = 0, tEndRow = 0, tEndCol = 0;
                    bool isInteractived = new RasterMoasicClipHelper().ComputeBeginEndRowCol(srcBands[num].extent,
                        new Size(srcBands[num].band.XSize, srcBands[num].band.YSize), dstBand.extent,
                        new Size(dstBand.band.XSize, dstBand.band.YSize), ref oBeginRow, ref oBeginCol, ref oEndRow,
                        ref oEndCol, ref tBeginRow, ref tBeginCol, ref tEndRow, ref tEndCol);
                    if (!isInteractived)
                    {
                        continue;
                    }

                    int oRows = oEndRow - oBeginRow;
                    int oCols = oEndCol - oBeginCol;
                    int rowStep = ComputeRowStep(srcBands[num].band, oBeginRow, oEndRow);
                    for (int oRow = oBeginRow; oRow < oEndRow; oRow += rowStep)
                    {
                        if (oRow + rowStep > oEndRow)
                            rowStep = oEndRow - oRow;
                        long bufferSize = (oEndCol - oBeginCol) * rowStep;
                        byte[] databuffer = new byte[bufferSize];
                        unsafe
                        {
                            fixed (byte* ptr = databuffer)
                            {
                                IntPtr buffer = new IntPtr(ptr);
                                srcBands[num].band.ReadRaster(oBeginCol, oRow, oCols, rowStep, buffer,
                                    (oEndCol - oBeginCol), rowStep, srcBands[num].band.DataType, 0, 0);
                                dstBand.band.WriteRaster(tBeginCol, tBeginRow + (oRow - oBeginRow),
                                    (tEndCol - tBeginCol), rowStep, buffer, (tEndCol - tBeginCol),
                                    (tEndRow - tBeginRow), srcBands[num].band.DataType, 0, 0);
                            }
                        }
                    }
                }
            }
        }

        #region Helper

        /// <summary>
        /// 创建目标文件DataProvider
        /// </summary>
        /// <param name="srcRasters">待拼接文件DataProvider</param>
        /// <param name="driver">驱动名称</param>
        /// <param name="outPathName">指定输出路径</param>
        /// <returns></returns>
        public AbstractWarpDataset CreatDstDataProvider(AbstractWarpDataset[] srcRasters, string driver,
            string outPathName)
        {
            //目标文件名
            string dstFileName = null;
            if (!string.IsNullOrEmpty(outPathName))
            {
                string outDir = Path.GetDirectoryName(outPathName);
                string outf = Path.GetPathRoot(outPathName);
                string name = Path.GetFileName(outPathName);
                if (outDir != null)
                {
                    if (!Directory.Exists(outDir))
                        Directory.CreateDirectory(outDir);
                    dstFileName = outPathName;
                }
                else if (outf != null)
                {
                    if (string.IsNullOrEmpty(name))
                        dstFileName += Path.Combine(outf,
                            Path.GetFileNameWithoutExtension(srcRasters[0].fileName) + "_MOASIC" + ".ldf");
                    dstFileName = outPathName;
                }
            }
            else
                dstFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "Temp",
                    Path.GetFileNameWithoutExtension(srcRasters[0].fileName) +
                    "_" + "MOASIC" + ".ldf");

            int bandCount = srcRasters[0].BandCount;
            DataType dataType = srcRasters[0].DataType;
            //计算目标文件外包矩形
            Envelope dstEnvelope;
            if (srcRasters.Count() == 0 || srcRasters == null)
                return null;
            if (srcRasters[0].GetEnvelope() == null)
                return null;
            else
            {
                dstEnvelope = srcRasters[0].GetEnvelope();
                int count = srcRasters.Count();
                for (int i = 1; i < count; i++)
                {
                    if (srcRasters[i].GetEnvelope() == null)
                        return null;
                    dstEnvelope = dstEnvelope.Union(srcRasters[i].GetEnvelope());
                }
            }

            //计算行列数
            int xSize = 0;
            int ySize = 0;
            float resolutionX = srcRasters[0].ResolutionX;
            float resolutionY = srcRasters[0].ResolutionY;
            xSize = (int) Math.Round((dstEnvelope.GetWidth() / resolutionX), 0);
            ySize = (int) Math.Round((dstEnvelope.GetHeight() / resolutionY), 0);
            string[] optionString = new string[]
            {
                "INTERLEAVE=BSQ",
                "VERSION=LDF",
                "WITHHDR=TRUE",
                "SPATIALREF=" + (srcRasters[0].SpatialRef != null
                    ? srcRasters[0].SpatialRef.ExportToProj4()
                    : string.Empty),
            };
            string[] _options = new string[] {"header_offset=128"};
            double[] geoTrans = new double[]
            {
                dstEnvelope.MinX, Convert.ToDouble(resolutionX.ToString("f6")), 0, dstEnvelope.MaxY, 0,
                -Convert.ToDouble(resolutionY.ToString("f6"))
            };
            var rDs = DatasetFactory.CreateRasterDataset(dstFileName, xSize, ySize, bandCount, dataType, "ENVI", null);
            rDs.SetGeoTransform(geoTrans);
            if (rDs == null)

                throw new ArgumentException("请检查输出文件路径");
            rDs.SetProjection(srcRasters[0].SpatialRef.ExportToWkt());
            return new WarpDataset(rDs, dstFileName);
        }


        private int ComputeRowStep(Band srcBand, int oMaxEndRow, int oMinBeginRow)
        {
            ulong mSize = MemoryHelper.GetAvalidPhyMemory() / 3;
            ulong maxLimit = mSize;
            int row = (int) (maxLimit / (ulong) srcBand.YSize);
            if (row == 0)
                row = 1;
            if (row > srcBand.YSize)
                row = srcBand.YSize;
            if (row > oMaxEndRow - oMinBeginRow)
                row = oMaxEndRow - oMinBeginRow;
            return row;
        }

        private Envelope GetBlockEnvelope(AbstractWarpDataset srcDataProvider, int beginRow, int endRow)
        {
            double _XMin = srcDataProvider.GetEnvelope().MinX;
            double _XMax = srcDataProvider.GetEnvelope().MaxX;
            double _YMax = srcDataProvider.GetEnvelope().MaxY - beginRow * srcDataProvider.ResolutionY;
            double _YMin = srcDataProvider.GetEnvelope().MaxY - endRow * srcDataProvider.ResolutionY;
            Envelope blockEnvelope = new Envelope {MaxX = _XMax, MinX = _XMin, MaxY = _YMax, MinY = _YMin};
            return blockEnvelope;
        }

        private Envelope GetInternalEnvelope(Envelope currentEnvelope, int index, Envelope blockEnvelope,
            out bool isInternal)
        {
            Envelope innerEnvelope = currentEnvelope.Intersection(blockEnvelope);
            if (innerEnvelope == null || innerEnvelope.GetWidth() == 0 || innerEnvelope.GetHeight() == 0)
                isInternal = false;
            else
                isInternal = true;
            return innerEnvelope;
        }

        private double[] GetDoubleValues(string[] valueStrings)
        {
            double[] invalidValues = new double[valueStrings.Length];
            for (int i = 0; i < valueStrings.Length; i++)
            {
                double value;
                Double.TryParse(valueStrings[i], out value);
                invalidValues[i] = value;
            }

            return invalidValues;
        }

        private float[] GetFloatValues(string[] valueStrings)
        {
            float[] invalidValues = new float[valueStrings.Length];
            for (int i = 0; i < valueStrings.Length; i++)
            {
                float value;
                float.TryParse(valueStrings[i], out value);
                invalidValues[i] = value;
            }

            return invalidValues;
        }

        private UInt16[] GetUInt16Values(string[] valueString)
        {
            List<UInt16> invalidValues = new List<ushort>();
            for (int i = 0; i < valueString.Length; i++)
            {
                UInt16 value;
                if (UInt16.TryParse(valueString[i], out value))
                    invalidValues.Add(value);
            }

            return invalidValues.ToArray();
        }

        private Int16[] GetInt16Values(string[] valueString)
        {
            List<Int16> invalidValues = new List<Int16>();
            for (int i = 0; i < valueString.Length; i++)
            {
                Int16 value;
                if (Int16.TryParse(valueString[i], out value))
                    invalidValues.Add(value);
            }

            return invalidValues.ToArray();
        }

        private Byte[] GetByteValues(string[] valueString)
        {
            List<Byte> invalidValues = new List<Byte>();
            for (int i = 0; i < valueString.Length; i++)
            {
                Byte value;
                if (Byte.TryParse(valueString[i], out value))
                    invalidValues.Add(value);
            }

            return invalidValues.ToArray();
        }

        private Int32[] GetInt32Values(string[] valueString)
        {
            List<Int32> invalidValues = new List<Int32>();
            for (int i = 0; i < valueString.Length; i++)
            {
                Int32 value;
                if (Int32.TryParse(valueString[i], out value))
                    invalidValues.Add(value);
            }

            return invalidValues.ToArray();
        }

        #endregion

        /// <summary>
        /// 镶嵌到目标数据上
        /// 镶嵌时候自动查找天顶角信息，取最小值（尚未完成）
        /// </summary>
        /// <param name="srcRasters"></param>
        /// <param name="dstRaster"></param>
        /// <param name="isProcessInvalid"></param>
        /// <param name="invalidValues"></param>
        /// <param name="progressCallback"></param>
        /// <param name="options"></param>
        public void Moasic(AbstractWarpDataset srcRasters, AbstractWarpDataset dstRaster, bool isProcessInvalid,
            string[] invalidValues, Action<int, string> progressCallback, params object[] options)
        {
            if (srcRasters == null)
                return;
            //检查待拼接文件波段数、数据类型、空间参考是否相等
            if (RasterMoasicChecker.CheckBandCount(new AbstractWarpDataset[] {srcRasters, dstRaster}) == false
                || RasterMoasicChecker.CheckDataType(new AbstractWarpDataset[] {srcRasters, dstRaster}) == false
                || RasterMoasicChecker.CheckSpatialRef(new AbstractWarpDataset[] {srcRasters, dstRaster}) == false)
                return;
            DataType dataType = srcRasters.DataType;
            if (progressCallback != null)
                progressCallback(0, "开始镶嵌/拼接");
            //计算相交区域数据
            int oBeginRow = -1, oBeginCol = -1, oEndRow = -1, oEndCol = -1;
            int tBeginRow = -1, tBeginCol = -1, tEndRow = -1, tEndCol = -1;
            bool inEnv = new RasterMoasicClipHelper().ComputeBeginEndRowCol(srcRasters, dstRaster,
                new Size(dstRaster.Width, dstRaster.Height),
                ref oBeginRow, ref oBeginCol, ref oEndRow, ref oEndCol,
                ref tBeginRow, ref tBeginCol, ref tEndRow, ref tEndCol);
            if (!inEnv)
                return;
            int oWidth = oEndCol - oBeginCol;
            int oHeight = oEndRow - oBeginRow;
            int tWidth = tEndCol - tBeginCol;
            int tHeight = tEndRow - tBeginRow;
            if (oWidth == 0 || oHeight == 0 || tWidth == 0 || tHeight == 0)
                return;
            int rowStep = ClipCutHelper.ComputeRowStep(srcRasters, oBeginRow, oEndRow);
            int stepCount = (int) ((oHeight) / rowStep + 0.5f) * srcRasters.BandCount;
            int curStep = -1;
            AbstractWarpDataset oRad = null;
            AbstractWarpDataset tRad = null;
            if (isProcessInvalid && invalidValues != null && invalidValues.Length != 0)
            {
                string oSolarZenithFile = Path.ChangeExtension(srcRasters.fileName, ".SolarZenith.ldf");
                string tSolarZenithFile = Path.ChangeExtension(dstRaster.fileName, ".SolarZenith.ldf");
                if (File.Exists(oSolarZenithFile) && File.Exists(tSolarZenithFile))
                {
                    oRad = WarpDataset.Open(oSolarZenithFile);
                    var rds = Gdal.Open(tSolarZenithFile, Access.GA_Update);
                    tRad = new WarpDataset(rds, tSolarZenithFile);
                }
            }

            if (oRad != null & tRad != null)
            {
                for (int oRow = oBeginRow, tRow = tBeginRow; oRow < oEndRow; oRow += rowStep, tRow += rowStep)
                {
                    if (oRow + rowStep > oEndRow)
                        rowStep = oEndRow - oRow;
                    if (tRow + rowStep > tEndRow)
                        rowStep = tEndRow - tRow;
                    long bufferSize = oWidth * rowStep;
                    short[] oSolarZenith = null;
                    short[] tSolarZenith = null;
                    if (oRad != null && tRad != null)
                    {
                        oSolarZenith = new short[bufferSize];
                        tSolarZenith = new short[bufferSize];
                        unsafe
                        {
                            fixed (short* optr = oSolarZenith)
                            {
                                IntPtr oSzBuffer = new IntPtr(optr);
                                fixed (short* tptr = tSolarZenith)
                                {
                                    IntPtr tSzBuffer = new IntPtr(tptr);
                                    oRad.GetRasterBand(1).ReadRaster(oBeginCol, oRow, oWidth, rowStep, oSzBuffer,
                                        oWidth, rowStep, oRad.DataType, 0, 0);
                                    tRad.GetRasterBand(1).ReadRaster(tBeginCol, tRow, tWidth, tHeight, tSzBuffer,
                                        tWidth, tHeight, tRad.DataType, 0, 0);
                                }

                                switch (dataType)
                                {
                                    case DataType.GDT_UInt16:
                                    {
                                        for (int bandNo = 1; bandNo <= srcRasters.BandCount; bandNo++)
                                        {
                                            curStep++;
                                            int curp = (int) ((curStep * 1.0f / stepCount) * 100);
                                            if (progressCallback != null)
                                                progressCallback(curp, "拼接/镶嵌完成" + curp + "%");
                                            UInt16[] oDbuffer = new UInt16[bufferSize];
                                            unsafe
                                            {
                                                fixed (UInt16* ptr = oDbuffer)
                                                {
                                                    IntPtr oDataBuffer = new IntPtr(ptr);
                                                    srcRasters.GetRasterBand(bandNo).ReadRaster(oBeginCol, oRow, oWidth,
                                                        rowStep, oDataBuffer, oWidth, rowStep, srcRasters.DataType, 0,
                                                        0);
                                                    //进行无效值处理
                                                    if (isProcessInvalid && invalidValues != null &&
                                                        invalidValues.Length != 0)
                                                    {
                                                        long tBufferSize = tWidth * tHeight;
                                                        //保存现有已拼接的交集部分的值
                                                        UInt16[] tDatabuffer = new UInt16[tBufferSize];
                                                        fixed (UInt16* cptr = tDatabuffer)
                                                        {
                                                            IntPtr currentBuffer = new IntPtr(cptr);
                                                            dstRaster.GetRasterBand(bandNo).ReadRaster(tBeginCol, tRow,
                                                                tWidth, tHeight, currentBuffer, tWidth, tHeight,
                                                                srcRasters.DataType, 0, 0);
                                                        }

                                                        UInt16[] invalids = GetUInt16Values(invalidValues);
                                                        int[] rowColRange = new int[]
                                                            {oBeginRow, oEndRow, oBeginCol, oEndCol, oWidth};
                                                        if (oSolarZenith == null || tSolarZenith == null)
                                                            InvalidValueProcesser<UInt16>.ProcessInvalidValue(
                                                                tDatabuffer, ref oDbuffer, rowColRange, invalids);
                                                        else
                                                            InvalidValueProcesser<UInt16>.ProcessInvalidValue(
                                                                tDatabuffer, ref oDbuffer, rowColRange, invalids,
                                                                index =>
                                                                {
                                                                    return tSolarZenith[index] > oSolarZenith[index];
                                                                }, //取高度角小的
                                                                index =>
                                                                {
                                                                    oSolarZenith[index] = tSolarZenith[index];
                                                                });
                                                        tRad.GetRasterBand(1).WriteRaster(tBeginCol, tRow, tWidth,
                                                            rowStep, oSzBuffer, oWidth, rowStep, tRad.DataType, 0, 0);
                                                    }

                                                    dstRaster.GetRasterBand(bandNo).WriteRaster(tBeginCol, tRow, tWidth,
                                                        rowStep, oDataBuffer, oWidth, rowStep, dstRaster.DataType, 0,
                                                        0);
                                                }
                                            }
                                        }

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (progressCallback != null)
                    progressCallback(100, "镶嵌/拼接完成");
            }
            else
            {
                for (int oRow = oBeginRow, tRow = tBeginRow; oRow < oEndRow; oRow += rowStep, tRow += rowStep)
                {
                    if (oRow + rowStep > oEndRow)
                        rowStep = oEndRow - oRow;
                    if (tRow + rowStep > tEndRow)
                        rowStep = tEndRow - tRow;
                    long bufferSize = oWidth * rowStep;
                    switch (dataType)
                    {
                        case DataType.GDT_UInt16:
                        {
                            for (int bandNo = 1; bandNo <= srcRasters.BandCount; bandNo++)
                            {
                                curStep++;
                                int curp = (int) ((curStep * 1.0f / stepCount) * 100);
                                if (progressCallback != null)
                                    progressCallback(curp, "拼接/镶嵌完成" + curp + "%");
                                UInt16[] oDbuffer = new UInt16[bufferSize];
                                unsafe
                                {
                                    fixed (UInt16* ptr = oDbuffer)
                                    {
                                        IntPtr oDataBuffer = new IntPtr(ptr);
                                        srcRasters.GetRasterBand(bandNo).ReadRaster(oBeginCol, oRow, oWidth, rowStep,
                                            oDataBuffer, oWidth, rowStep, srcRasters.DataType, 0, 0);
                                        //进行无效值处理
                                        if (isProcessInvalid && invalidValues != null && invalidValues.Length != 0)
                                        {
                                            long tBufferSize = tWidth * tHeight;
                                            //保存现有已拼接的交集部分的值
                                            UInt16[] tDatabuffer = new UInt16[tBufferSize];
                                            fixed (UInt16* cptr = tDatabuffer)
                                            {
                                                IntPtr currentBuffer = new IntPtr(cptr);
                                                dstRaster.GetRasterBand(bandNo).ReadRaster(tBeginCol, tRow, tWidth,
                                                    tHeight, currentBuffer, tWidth, tHeight, srcRasters.DataType, 0, 0);
                                            }

                                            UInt16[] invalids = GetUInt16Values(invalidValues);
                                            Size imageBufferSize = new Size(oWidth, oHeight);
                                            InvalidValueProcesser<UInt16>.ProcessInvalidValue(tDatabuffer, ref oDbuffer,
                                                imageBufferSize, invalids);
                                        }

                                        dstRaster.GetRasterBand(bandNo).WriteRaster(tBeginCol, tRow, tWidth, rowStep,
                                            oDataBuffer, oWidth, rowStep, dstRaster.DataType, 0, 0);
                                    }
                                }
                            }

                            break;
                        }
                    }
                }

                if (progressCallback != null)
                    progressCallback(100, "镶嵌/拼接完成");
            }
        }

        public void MoasicSimple(AbstractWarpDataset srcRasters, AbstractWarpDataset dstRaster, bool isProcessInvalid,
            string[] invalidValues, Action<int, string> progressCallback, params object[] options)
        {
            if (srcRasters == null)
                return;
            //检查待拼接文件波段数、数据类型、空间参考是否相等
            if (RasterMoasicChecker.CheckBandCount(new AbstractWarpDataset[] {srcRasters, dstRaster}) == false
                || RasterMoasicChecker.CheckDataType(new AbstractWarpDataset[] {srcRasters, dstRaster}) == false
                || RasterMoasicChecker.CheckSpatialRef(new AbstractWarpDataset[] {srcRasters, dstRaster}) == false)
                return;
            DataType dataType = srcRasters.DataType;
            if (progressCallback != null)
                progressCallback(0, "开始镶嵌/拼接");
            //计算相交区域数据
            int oBeginRow = -1, oBeginCol = -1;
            int tBeginRow = -1, tBeginCol = -1;
            Size oSize = Size.Empty;
            Size tSize = Size.Empty;
            bool inEnv = RasterMoasicClipHelper.ComputeBeginEndRowCol(srcRasters, dstRaster,
                ref oBeginRow, ref oBeginCol, ref oSize,
                ref tBeginRow, ref tBeginCol, ref tSize);
            if (!inEnv)
                return;
            int oWidth = oSize.Width;
            int oHeight = oSize.Height;
            int tWidth = tSize.Width;
            int tHeight = tSize.Height;
            if (oWidth == 0 || oHeight == 0 || tWidth == 0 || tHeight == 0)
                return;
            int oEndRow = oBeginRow + oHeight;
            int tEndRow = tBeginRow + tHeight;
            int rowStep = RasterMoasicClipHelper.ComputeRowStep(srcRasters, oHeight);
            int stepCount = (int) ((oHeight) / rowStep + 0.5f) * srcRasters.BandCount;
            int curStep = -1;
            for (int oRow = oBeginRow, tRow = tBeginRow; oRow < oEndRow; oRow += rowStep, tRow += rowStep)
            {
                if (oRow + rowStep > oEndRow)
                    rowStep = oEndRow - oRow;
                if (tRow + rowStep > tEndRow)
                    rowStep = tEndRow - tRow;
                long bufferSize = oWidth * rowStep;
                switch (dataType)
                {
                    case DataType.GDT_UInt16:
                    {
                        for (int bandNo = 1; bandNo <= srcRasters.BandCount; bandNo++)
                        {
                            curStep++;
                            int curp = (int) ((curStep * 1.0f / stepCount) * 100);
                            if (progressCallback != null)
                                progressCallback(curp, "拼接/镶嵌完成" + curp + "%");
                            UInt16[] oDbuffer = new UInt16[bufferSize];
                            unsafe
                            {
                                fixed (UInt16* ptr = oDbuffer)
                                {
                                    IntPtr oDataBuffer = new IntPtr(ptr);
                                    srcRasters.GetRasterBand(bandNo).ReadRaster(oBeginCol, oRow, oWidth, rowStep,
                                        oDataBuffer, oWidth, rowStep, srcRasters.DataType, 0, 0);
                                    //进行无效值处理
                                    if (isProcessInvalid && invalidValues != null && invalidValues.Length != 0)
                                    {
                                        long tBufferSize = tWidth * tHeight;
                                        //保存现有已拼接的交集部分的值
                                        UInt16[] tDatabuffer = new UInt16[tBufferSize];
                                        fixed (UInt16* cptr = tDatabuffer)
                                        {
                                            IntPtr currentBuffer = new IntPtr(cptr);
                                            dstRaster.GetRasterBand(bandNo).ReadRaster(tBeginCol, tRow, tWidth, tHeight,
                                                currentBuffer, tWidth, tHeight, srcRasters.DataType, 0, 0);
                                        }

                                        UInt16[] invalids = GetUInt16Values(invalidValues);
                                        Size imageBufferSize = new Size(oWidth, oHeight);
                                        InvalidValueProcesser<UInt16>.ProcessInvalidValue(tDatabuffer, ref oDbuffer,
                                            imageBufferSize, invalids);
                                    }

                                    dstRaster.GetRasterBand(bandNo).WriteRaster(tBeginCol, tRow, tWidth, rowStep,
                                        oDataBuffer, oWidth, rowStep, dstRaster.DataType, 0, 0);
                                }
                            }
                        }

                        break;
                    }
                    case DataType.GDT_Int16:
                    {
                        for (int bandNo = 1; bandNo <= srcRasters.BandCount; bandNo++)
                        {
                            curStep++;
                            int curp = (int) ((curStep * 1.0f / stepCount) * 100);
                            if (progressCallback != null)
                                progressCallback(curp, "拼接/镶嵌完成" + curp + "%");
                            Int16[] oDbuffer = new Int16[bufferSize];
                            unsafe
                            {
                                fixed (Int16* ptr = oDbuffer)
                                {
                                    IntPtr oDataBuffer = new IntPtr(ptr);
                                    srcRasters.GetRasterBand(bandNo).ReadRaster(oBeginCol, oRow, oWidth, rowStep,
                                        oDataBuffer, oWidth, rowStep, srcRasters.DataType, 0, 0);
                                    //进行无效值处理
                                    if (isProcessInvalid && invalidValues != null && invalidValues.Length != 0)
                                    {
                                        long tBufferSize = tWidth * tHeight;
                                        //保存现有已拼接的交集部分的值
                                        Int16[] tDatabuffer = new Int16[tBufferSize];
                                        fixed (Int16* cptr = tDatabuffer)
                                        {
                                            IntPtr currentBuffer = new IntPtr(cptr);
                                            dstRaster.GetRasterBand(bandNo).ReadRaster(tBeginCol, tRow, tWidth, tHeight,
                                                currentBuffer, tWidth, tHeight, srcRasters.DataType, 0, 0);
                                        }

                                        Int16[] invalids = GetInt16Values(invalidValues);
                                        Size imageBufferSize = new Size(oWidth, oHeight);
                                        InvalidValueProcesser<Int16>.ProcessInvalidValue(tDatabuffer, ref oDbuffer,
                                            imageBufferSize, invalids);
                                    }

                                    dstRaster.GetRasterBand(bandNo).WriteRaster(tBeginCol, tRow, tWidth, rowStep,
                                        oDataBuffer, oWidth, rowStep, dstRaster.DataType, 0, 0);
                                }
                            }
                        }

                        break;
                    }
                }
            }

            if (progressCallback != null)
                progressCallback(100, "镶嵌/拼接完成");
        }
    }
}