using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using PIE.Geometry;
using PIE.Meteo.Core;

namespace PIE.Meteo.FileProject.BlockOper
{
    public enum MosaicType
    {
        Mosaic,
        NoMosaic,
        MosaicByDay
    }
    public class RasterMoasicClipHelper
    {
        private static double _errorValue = Math.Pow(5, -1);

        public bool ComputeBeginEndRowCol(AbstractWarpDataset args, AbstractWarpDataset dstArgs, Size targetSize, ref int oBeginRow, ref int oBeginCol, ref int oEndRow, ref int oEndCol, ref int tBeginRow, ref int tBeginCol, ref int tEndRow, ref int tEndCol)
        {
            IEnvelope targetEnvelope = dstArgs.GetEnvelope();
            IEnvelope innerEnvelope = ((targetEnvelope as Geometry.Geometry).Intersection(args.GetEnvelope() as IGeometry) as IEnvelope);
            if (innerEnvelope == null)
                return  false;
            double tResolutionX = dstArgs.ResolutionX;
            double tResolutionY = dstArgs.ResolutionY;
            double oResolutionX = args.ResolutionX;
            double oResolutionY = args.ResolutionY;
            IEnvelope oEnvelope = args.GetEnvelope();
            //
            if (oEnvelope.XMin >= targetEnvelope.XMin)//左边界在目标图像内部
            {
                oBeginCol = 0;
                tBeginCol = (int)((oEnvelope.XMin - targetEnvelope.XMin) / tResolutionX + _errorValue);
            }
            else//左边界在目标图像外部
            {
                oBeginCol = (int)((targetEnvelope.XMin - oEnvelope.XMin) / oResolutionX + _errorValue);
                tBeginCol = 0;
            }
            if (oEnvelope.XMax >= targetEnvelope.XMax)//右边界在目标图像外部
            {
                oEndCol = (int)((targetEnvelope.XMax - oEnvelope.XMin) / oResolutionX + _errorValue);
                tEndCol = targetSize.Width;
            }
            else//右边界在目标图像内部
            {
                oEndCol = (int)((args.GetEnvelope().XMax - args.GetEnvelope().XMin) / oResolutionX + _errorValue);
                tEndCol = (int)((oEnvelope.XMax - targetEnvelope.XMin) / tResolutionX + _errorValue);
            }
            if (oEnvelope.YMax <= targetEnvelope.YMax)//上边界在目标图像内部
            {
                oBeginRow = 0;
                tBeginRow = (int)((targetEnvelope.YMax - oEnvelope.YMax) / tResolutionY + _errorValue);
            }
            else//上边界在目标边界外部
            {
                oBeginRow = (int)((oEnvelope.YMax - targetEnvelope.YMax) / oResolutionY + _errorValue);
                tBeginRow = 0;
            }
            if (oEnvelope.YMin <= targetEnvelope.YMin)//下边界在目标图像外部
            {
                oEndRow = (int)((oEnvelope.YMax - targetEnvelope.YMin) / oResolutionY + _errorValue);
                tEndRow = targetSize.Height;
            }
            else//下边界在目标图像内部
            {
                oEndRow = args.Height;
                tEndRow = (int)((targetEnvelope.YMax - oEnvelope.YMin) / tResolutionY + _errorValue);
            }
            ////以下添加对偏移计算的纠正，取最小行列数。
            //int oWidth = oEndCol - oBeginCol;
            //int oHeight = oEndRow - oBeginRow;
            //int tWidth = tEndCol - tBeginCol;
            //int tHeight = tEndRow - tBeginRow;
            //oWidth = tWidth = Math.Min(oWidth, tWidth);
            //oHeight = tHeight = Math.Min(oHeight, tHeight);
            //oEndRow = oBeginRow + oHeight;
            //oEndCol = oBeginCol + oWidth;
            //tEndRow = tBeginRow + tHeight;
            //tEndCol = tBeginCol + tWidth;
            return true;
        }

        //源为待裁切的文件，目标为裁切输出的小文件
        public bool ComputeBeginEndRowCol(IEnvelope oEnvelope, Size oSize, IEnvelope tEnvelope, Size tSize, ref int oBeginRow, ref int oBeginCol, ref int oEndRow, ref int oEndCol, ref int tBeginRow, ref int tBeginCol, ref int tEndRow, ref int tEndCol)
        {
            if (!IsInteractived(oEnvelope, tEnvelope))
                return false;
            double resolutionX = oEnvelope.GetWidth() / oSize.Width;
            double resolutionY = oEnvelope.GetHeight() / oSize.Height;

            if (oEnvelope.XMin >= tEnvelope.XMin)//左边界在目标图像内部
            {
                oBeginCol = 0;
                tBeginCol = GetInteger((oEnvelope.XMin - tEnvelope.XMin) / resolutionX);
            }
            else//左边界在目标图像外部
            {
                oBeginCol = GetInteger((tEnvelope.XMin - oEnvelope.XMin) / resolutionX);
                tBeginCol = 0;
            }
            if (oEnvelope.XMax >= tEnvelope.XMax)//右边界在目标图像外部
            {
                oEndCol = GetInteger((tEnvelope.XMax - oEnvelope.XMin) / resolutionX);
                tEndCol = tSize.Width;
            }
            else//右边界在目标图像内部
            {
                oEndCol = GetInteger((oEnvelope.XMax - oEnvelope.XMin) / resolutionX);
                tEndCol = GetInteger((oEnvelope.XMax - tEnvelope.XMin) / resolutionX);
            }
            if (oEnvelope.YMax <= tEnvelope.YMax)//上边界在目标图像内部
            {
                oBeginRow = 0;
                tBeginRow = GetInteger((tEnvelope.YMax - oEnvelope.YMax) / resolutionY);
            }
            else//上边界在目标边界外部
            {
                oBeginRow = GetInteger((oEnvelope.YMax - tEnvelope.YMax) / resolutionY);
                tBeginRow = 0;
            }
            if (oEnvelope.YMin <= tEnvelope.YMin)//下边界在目标图像外部
            {
                oEndRow = GetInteger((oEnvelope.YMax - tEnvelope.YMin) / resolutionY);
                tEndRow = tSize.Height;
            }
            else//下边界在目标图像内部
            {
                oEndRow = oSize.Height;
                tEndRow = GetInteger((tEnvelope.YMax - oEnvelope.YMin) / resolutionY);
            }
            int oWidth = oEndCol - oBeginCol;
            int oHeight = oEndRow - oBeginRow;
            int tWidth = tEndCol - tBeginCol;
            int tHeight = tEndRow - tBeginRow;
            oWidth = tWidth = Math.Min(oWidth, tWidth);
            oHeight = tHeight = Math.Min(oHeight, tHeight);
            oEndRow = oBeginRow + oHeight;
            oEndCol = oBeginCol + oWidth;
            tEndRow = tBeginRow + tHeight;
            tEndCol = tBeginCol + tWidth;
            return true;
        }

        protected int GetInteger(double fWidth)
        {
            //int v = (int)Math.Round(fWidth);
            //if (fWidth - v > 0.9000001)
            //    v++;
            //return v;
            return (int)(fWidth);
        }

        private static bool IsInteractived(IEnvelope envelopeA, IEnvelope envelopeB)
        {
            RectangleF a = new RectangleF((float)envelopeA.XMin, (float)envelopeA.YMin, (float)envelopeA.GetWidth(), (float)envelopeA.GetHeight());
            RectangleF b = new RectangleF((float)envelopeB.XMin, (float)envelopeB.YMin, (float)envelopeB.GetWidth(), (float)envelopeB.GetHeight());
            return a.IntersectsWith(b);
        }
        
        /// <summary>
        /// 支持不同分辨率的拼接
        /// </summary>
        /// <param name="args"></param>
        /// <param name="dstArgs"></param>
        /// <param name="tSize"></param>
        /// <param name="oBeginRow"></param>
        /// <param name="oBeginCol"></param>
        /// <param name="oIntersectSize"></param>
        /// <param name="tBeginRow"></param>
        /// <param name="tBeginCol"></param>
        /// <param name="tIntersectSize"></param>
        /// <returns></returns>
        public static bool ComputeBeginEndRowCol(AbstractWarpDataset args, AbstractWarpDataset dstArgs,
            ref int oBeginRow, ref int oBeginCol, ref Size oIntersectSize, 
            ref int tBeginRow, ref int tBeginCol, ref Size tIntersectSize)
        {
            IEnvelope oEnvelope = args.GetEnvelope();
            IEnvelope tEnvelope = dstArgs.GetEnvelope();
            IEnvelope inEnv = ((oEnvelope as Geometry.Geometry).Intersection(tEnvelope as IGeometry) as IEnvelope);
            if (inEnv == null)
                return false;
            if (!IsInteractived(oEnvelope, tEnvelope))
                return false;
            float oResolutionX = args.ResolutionX;
            float oResolutionY = args.ResolutionY;
            float tResolutionX = dstArgs.ResolutionX;
            float tResolutionY = dstArgs.ResolutionY;
            oIntersectSize = CoordEnvelopeToSize(inEnv, oResolutionX, oResolutionY);
            tIntersectSize = CoordEnvelopeToSize(inEnv, tResolutionX, tResolutionY);
            //
            if (oEnvelope.XMin >= tEnvelope.XMin)//左边界在目标图像内部
            {
                oBeginCol = 0;
                tBeginCol = (int)((oEnvelope.XMin - tEnvelope.XMin) / tResolutionX + _errorValue);
            }
            else//左边界在目标图像外部
            {
                oBeginCol = (int)((tEnvelope.XMin - oEnvelope.XMin) / oResolutionX + _errorValue);
                tBeginCol = 0;
            }
            if (oEnvelope.YMax <= tEnvelope.YMax)//上边界在目标图像内部
            {
                oBeginRow = 0;
                tBeginRow = (int)((tEnvelope.YMax - oEnvelope.YMax) / tResolutionY + _errorValue);
            }
            else//上边界在目标边界外部
            {
                oBeginRow = (int)((oEnvelope.YMax - tEnvelope.YMax) / oResolutionY + _errorValue);
                tBeginRow = 0;
            }
            Size oSize = new Size(args.Width, args.Height);
            Size tSize = new Size(dstArgs.Width, dstArgs.Height);
            //以下添加对偏移计算的纠正，取最小行列数
            if (oIntersectSize.Width + oBeginCol > oSize.Width)
                oIntersectSize.Width = oSize.Width - oBeginCol;
            if (oIntersectSize.Height + oBeginRow > oSize.Height)
                oIntersectSize.Height = oSize.Height - oBeginRow;
            if (tIntersectSize.Width + tBeginCol > tSize.Width)
                tIntersectSize.Width = tSize.Width - tBeginCol;
            if (tIntersectSize.Height + tBeginRow > tSize.Height)
                tIntersectSize.Height = tSize.Height - tBeginRow;
            return true;
        }

        private static Size CoordEnvelopeToSize(IEnvelope envelope, float resolutonX, float resolutonY)
        {
            int w = (int)((envelope.XMax - envelope.XMin) / resolutonX + _errorValue);
            int h = (int)((envelope.YMax - envelope.YMin) / resolutonY + _errorValue);
            return w <= 0 || h <= 0 ? Size.Empty : new Size(w, h);
        }

        public static int ComputeRowStep(AbstractWarpDataset srcRaster, int maxRow)
        {
            ulong mSize = MemoryHelper.GetAvalidPhyMemory() / 3;
            ulong maxLimit = mSize;
            int row = (int)(maxLimit / (ulong)srcRaster.Height);
            if (row == 0)
                row = 1;
            if (row > srcRaster.Height)
                row = srcRaster.Height;
            if (row > maxRow)
                row = maxRow;
            return row;
        }
    }
}
