﻿using PIE.Meteo.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.OGR;
using VBand = PIE.Meteo.FileProject.BlockOper.VirtualRasterBand;


namespace PIE.Meteo.FileProject.BlockOper
{
    public class RasterClipProcesser : IRasterClip
    {

        public VBand[] Clip(VBand srcBand, BlockDef[] blockDefs, int samplePercent, string driver, Action<int, string> progressCallback, params object[] options)
        {
            if (blockDefs.Length == 0 || blockDefs == null || srcBand == null)
                return null;
            VBand[] targetBands = new VBand[blockDefs.Length];
            int blockNums = 0;
            foreach (BlockDef it in blockDefs)
            {
                int tBeginRow = -1, tEndRow = -1, tBeginCol = -1, tEndCol = -1;
                int oBeginRow = -1, oEndRow = -1, oBeginCol = -1, oEndCol = -1;
                Envelope oEnvelope = srcBand.dataset.GetExtent();
                Envelope tEnvelope = it.ToEnvelope();
                Size oSize = new Size(srcBand.band.XSize, srcBand.band.YSize);
                double[] geoTrans = srcBand.dataset.GetGeoTransform();
                Size tSize = ClipCutHelper.GetTargetSize(it, geoTrans[1], -geoTrans[5]);
                bool isInternal = new RasterMoasicClipHelper().ComputeBeginEndRowCol(oEnvelope, oSize, tEnvelope, tSize, ref oBeginRow, ref oBeginCol, ref oEndRow, ref oEndCol, ref tBeginRow, ref tBeginCol, ref tEndRow, ref tEndCol);
                //targetBands[blockNums] = ;
                blockNums++;
            }
            return targetBands;
        }

        public AbstractWarpDataset[] Clip(AbstractWarpDataset srcRaster, BlockDef[] blockDefs, int samplePercent, string driver, string outdir, Action<int, string> progressCallback, params object[] options)
        {
            AbstractWarpDataset[] tProviders = new AbstractWarpDataset[blockDefs.Length];
            if (progressCallback != null)
                progressCallback(0, "开始数据分幅");
            for (int blockNums = 0; blockNums < blockDefs.Length; blockNums++)
            {
                if (progressCallback != null)
                    progressCallback((int)((blockNums + 1.0) / blockDefs.Length * 100), string.Format("正在分幅第{0}/{1}个文件...", (blockNums + 1).ToString(), blockDefs.Length));
                //位置映射参数
                int tBeginRow = -1, tEndRow = -1, tBeginCol = -1, tEndCol = -1;
                int oBeginRow = -1, oEndRow = -1, oBeginCol = -1, oEndCol = -1;
                Envelope oEnvelope = srcRaster.GetEnvelope();
                Envelope tEnvelope = blockDefs[blockNums].ToEnvelope();
                Size oSize = new Size(srcRaster.Width, srcRaster.Height);
                Size tSize = ClipCutHelper.GetTargetSize(blockDefs[blockNums], srcRaster.ResolutionX, srcRaster.ResolutionY);
                bool isInternal = new RasterMoasicClipHelper().ComputeBeginEndRowCol(oEnvelope, oSize, tEnvelope, tSize, ref oBeginRow, ref oBeginCol, ref oEndRow, ref oEndCol,
                                                                               ref tBeginRow, ref tBeginCol, ref tEndRow, ref tEndCol);
                string blockFilename = ClipCutHelper.GetBlockFilename(blockDefs[blockNums], srcRaster.fileName, outdir, driver);
                int oWidth = 0;
                int oHeight = 0;
                float tResolutionX;
                float tResolutionY;
                if (samplePercent > 0 && samplePercent < 100)
                {
                    oHeight = (int)(tSize.Width * samplePercent * 1f / 100 + 0.5);
                    oWidth = (int)(tSize.Width * samplePercent * 1f / 100 + 0.5);
                    tResolutionX = srcRaster.ResolutionX * samplePercent * 1f / 100;
                    tResolutionY = srcRaster.ResolutionY * samplePercent * 1f / 100;
                }
                else
                {
                    oHeight = tSize.Height;
                    oWidth = tSize.Width;
                    tResolutionX = srcRaster.ResolutionX;
                    tResolutionY = srcRaster.ResolutionY;
                }
                string[] optionString = new string[]{
                "INTERLEAVE=BSQ",
                "VERSION=LDF",
                "WITHHDR=TRUE",
                "SPATIALREF=" + srcRaster.SpatialRef.ExportToProj4(),
                "MAPINFO={" + 1 + "," + 1 + "}:{" + tEnvelope.XMin + "," + tEnvelope.YMax + "}:{" + tResolutionX + "," + tResolutionY + "}"
                };
                string[] _options = new string[] { "header_offset=128" };

                double[] geoTrans = new double[] { tEnvelope.XMin, Convert.ToDouble(tResolutionX.ToString("f6")), 0, tEnvelope.YMax, 0, -Convert.ToDouble(tResolutionY.ToString("f6")) };
                var rDs = DatasetFactory.CreateRasterDataset(blockFilename, oWidth, oHeight, srcRaster.BandCount, srcRaster.DataType, "ENVI", null);
                rDs.SetGeoTransform(geoTrans);

                rDs.SpatialReference = srcRaster.SpatialRef;
                tProviders[blockNums] = new WarpDataset(rDs);
                int rowStep = ClipCutHelper.ComputeRowStep(srcRaster, oBeginRow, oEndRow);
                for (int oRow = oBeginRow; oRow < oEndRow; oRow += rowStep)
                {
                    if (oRow + rowStep > oEndRow)
                        rowStep = oEndRow - oRow;
                    for (int bandIndex = 1; bandIndex <= srcRaster.BandCount; bandIndex++)
                    {
                        int sample = (oEndCol - oBeginCol);
                        int typeSize = ClipCutHelper.GetSize(srcRaster.DataType);
                        int bufferSize = sample * rowStep * typeSize;
                        byte[] databuffer = new byte[bufferSize];
                        unsafe
                        {
                            fixed (byte* ptr = databuffer)
                            {
                                IntPtr buffer = new IntPtr(ptr);
                                srcRaster.GetRasterBand(bandIndex).Read(oBeginCol, oRow, sample, rowStep, buffer, sample, rowStep, srcRaster.DataType);

                                if (samplePercent > 0 && samplePercent < 100)
                                {
                                    tProviders[blockNums].GetRasterBand(bandIndex).Write((int)(tBeginCol * samplePercent * 1f / 100 + 0.5), (int)((tBeginRow + (oRow - oBeginRow)) * samplePercent * 1f / 100 + 0.5), (int)(sample * samplePercent * 1f / 100 + 0.5), (int)(rowStep * samplePercent * 1f / 100 + 0.5), buffer, (int)(sample * samplePercent * 1f / 100 + 0.5), (int)(rowStep * samplePercent * 1f / 100 + 0.5), srcRaster.DataType);
                                }
                                else
                                {
                                    tProviders[blockNums].GetRasterBand(bandIndex).Write(tBeginCol, tBeginRow + (oRow - oBeginRow), sample, rowStep, buffer, sample, rowStep, srcRaster.DataType);
                                }
                            }
                        }
                    }
                }

            }
            return tProviders;
        }

        public AbstractWarpDataset Clip(AbstractWarpDataset srcRaster, BlockDef blockDefs, int samplePercent, string driver, string outdir, Action<int, string> progressCallback, out double validPercent, params object[] options)
        {
            AbstractWarpDataset tProviders = null;
            if (progressCallback != null)
                progressCallback(0, "开始数据分幅");
            //位置映射参数
            int tBeginRow = -1, tEndRow = -1, tBeginCol = -1, tEndCol = -1;
            int oBeginRow = -1, oEndRow = -1, oBeginCol = -1, oEndCol = -1;
            Envelope oEnvelope = srcRaster.GetEnvelope();
            Envelope tEnvelope = blockDefs.ToEnvelope();
            Size oSize = new Size(srcRaster.Width, srcRaster.Height);
            Size tSize = ClipCutHelper.GetTargetSize(blockDefs, srcRaster.ResolutionX, srcRaster.ResolutionY);
            bool isInternal = new RasterMoasicClipHelper().ComputeBeginEndRowCol(oEnvelope, oSize, tEnvelope, tSize, ref oBeginRow, ref oBeginCol, ref oEndRow, ref oEndCol,
                                                                           ref tBeginRow, ref tBeginCol, ref tEndRow, ref tEndCol);
            string blockFilename = ClipCutHelper.GetBlockFilename(blockDefs, srcRaster.fileName, outdir, driver);
            int oWidth = 0;
            int oHeight = 0;
            float tResolutionX;
            float tResolutionY;
            if (samplePercent > 0 && samplePercent < 100)
            {
                oHeight = (int)(tSize.Width * samplePercent * 1f / 100 + 0.5);
                oWidth = (int)(tSize.Width * samplePercent * 1f / 100 + 0.5);
                tResolutionX = srcRaster.ResolutionX * samplePercent * 1f / 100;
                tResolutionY = srcRaster.ResolutionY * samplePercent * 1f / 100;
            }
            else
            {
                oHeight = tSize.Height;
                oWidth = tSize.Width;
                tResolutionX = srcRaster.ResolutionX;
                tResolutionY = srcRaster.ResolutionY;
            }
            string[] optionString = new string[]{
                "INTERLEAVE=BSQ",
                "VERSION=LDF",
                "WITHHDR=TRUE",
                "SPATIALREF=" + srcRaster.SpatialRef.ExportToProj4(),
                "MAPINFO={" + 1 + "," + 1 + "}:{" + tEnvelope.XMin + "," + tEnvelope.YMax + "}:{" + tResolutionX + "," + tResolutionY + "}"
                };
            string[] _options = new string[] { "header_offset=128" };
            double[] geoTrans = new double[] { tEnvelope.XMin, Convert.ToDouble(tResolutionX.ToString("f6")), 0, tEnvelope.YMax, 0, -Convert.ToDouble(tResolutionY.ToString("f6")) };
            var rDs = DatasetFactory.CreateRasterDataset(blockFilename, oWidth, oHeight, srcRaster.BandCount, srcRaster.DataType, "ENVI", null);
            rDs.SetGeoTransform(geoTrans);

            rDs.SpatialReference = srcRaster.SpatialRef;
            tProviders = new WarpDataset(rDs);
            int rowStep = ClipCutHelper.ComputeRowStep(srcRaster, oBeginRow, oEndRow);
            int sample = (oEndCol - oBeginCol);
            int typeSize = ClipCutHelper.GetSize(srcRaster.DataType);

            long allPixelByte = sample * (oEndRow - oBeginRow) * typeSize * srcRaster.BandCount;
            long validPixelByte = 0;
            long validPer = (int)(allPixelByte * 0.1f);
            int stepCount = (int)((oEndRow - oBeginRow) / rowStep + 0.5) * srcRaster.BandCount;
            int step = 0;
            int percent = 0;
            for (int oRow = oBeginRow; oRow < oEndRow; oRow += rowStep)
            {
                if (oRow + rowStep > oEndRow)
                    rowStep = oEndRow - oRow;
                for (int bandIndex = 0; bandIndex < srcRaster.BandCount; bandIndex++)
                {
                    step++;
                    percent = (int)(step * 1.0f / stepCount * 100);
                    if (progressCallback != null)
                        progressCallback(percent, "完成数据分幅" + percent + "%");
                    int bufferSize = sample * rowStep * typeSize;
                    byte[] databuffer = new byte[bufferSize];
                    unsafe
                    {
                        fixed (byte* ptr = databuffer)
                        {
                            IntPtr buffer = new IntPtr(ptr);
                            srcRaster.GetRasterBand(bandIndex).Read(oBeginCol, oRow, sample, rowStep, buffer, sample, rowStep, srcRaster.DataType);
                            if (validPixelByte < validPer)
                            {
                                foreach (byte b in databuffer)
                                {
                                    if (b != 0)
                                        validPixelByte++;
                                }
                            }
                            if (validPixelByte == 0)
                                continue;
                            if (samplePercent > 0 && samplePercent < 100)
                            {
                                tProviders.GetRasterBand(bandIndex).Write((int)(tBeginCol * samplePercent * 1f / 100 + 0.5), (int)((tBeginRow + (oRow - oBeginRow)) * samplePercent * 1f / 100 + 0.5), (int)(sample * samplePercent * 1f / 100 + 0.5), (int)(rowStep * samplePercent * 1f / 100 + 0.5), buffer, (int)(sample * samplePercent * 1f / 100 + 0.5), (int)(rowStep * samplePercent * 1f / 100 + 0.5), srcRaster.DataType);
                            }
                            else
                            {
                                tProviders.GetRasterBand(bandIndex).Write(tBeginCol, tBeginRow + (oRow - oBeginRow), sample, rowStep, buffer, sample, rowStep, srcRaster.DataType);
                            }
                        }
                    }
                }
            }
            validPercent = validPixelByte * 1.0d / allPixelByte;
            if (progressCallback != null)
                progressCallback(100, "完成数据分幅");
            return tProviders;
        }
    }
}
