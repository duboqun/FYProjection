﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using OSGeo.OGR;

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
            Envelope targetEnvelope = dstArgs.GetEnvelope();
            Geometry targetGeo = targetEnvelope.GetGeometry();
            Geometry argGeo = args.GetEnvelope().GetGeometry();
            
            Geometry innerEnvelopeGeo = targetGeo.Intersection(argGeo);
            if (innerEnvelopeGeo == null)
                return  false;
            double tResolutionX = dstArgs.ResolutionX;
            double tResolutionY = dstArgs.ResolutionY;
            double oResolutionX = args.ResolutionX;
            double oResolutionY = args.ResolutionY;
            Envelope oEnvelope = args.GetEnvelope();
            //
            if (oEnvelope.MinX >= targetEnvelope.MinX)//左边界在目标图像内部
            {
                oBeginCol = 0;
                tBeginCol = (int)((oEnvelope.MinX - targetEnvelope.MinX) / tResolutionX + _errorValue);
            }
            else//左边界在目标图像外部
            {
                oBeginCol = (int)((targetEnvelope.MinX - oEnvelope.MinX) / oResolutionX + _errorValue);
                tBeginCol = 0;
            }
            if (oEnvelope.MaxX >= targetEnvelope.MaxX)//右边界在目标图像外部
            {
                oEndCol = (int)((targetEnvelope.MaxX - oEnvelope.MinX) / oResolutionX + _errorValue);
                tEndCol = targetSize.Width;
            }
            else//右边界在目标图像内部
            {
                oEndCol = (int)((args.GetEnvelope().MaxX - args.GetEnvelope().MinX) / oResolutionX + _errorValue);
                tEndCol = (int)((oEnvelope.MaxX - targetEnvelope.MinX) / tResolutionX + _errorValue);
            }
            if (oEnvelope.MaxY <= targetEnvelope.MaxY)//上边界在目标图像内部
            {
                oBeginRow = 0;
                tBeginRow = (int)((targetEnvelope.MaxY - oEnvelope.MaxY) / tResolutionY + _errorValue);
            }
            else//上边界在目标边界外部
            {
                oBeginRow = (int)((oEnvelope.MaxY - targetEnvelope.MaxY) / oResolutionY + _errorValue);
                tBeginRow = 0;
            }
            if (oEnvelope.MinY <= targetEnvelope.MinY)//下边界在目标图像外部
            {
                oEndRow = (int)((oEnvelope.MaxY - targetEnvelope.MinY) / oResolutionY + _errorValue);
                tEndRow = targetSize.Height;
            }
            else//下边界在目标图像内部
            {
                oEndRow = args.Height;
                tEndRow = (int)((targetEnvelope.MaxY - oEnvelope.MinY) / tResolutionY + _errorValue);
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
        public bool ComputeBeginEndRowCol(Envelope oEnvelope, Size oSize, Envelope tEnvelope, Size tSize, ref int oBeginRow, ref int oBeginCol, ref int oEndRow, ref int oEndCol, ref int tBeginRow, ref int tBeginCol, ref int tEndRow, ref int tEndCol)
        {
            if (!IsInteractived(oEnvelope, tEnvelope))
                return false;
            double resolutionX = oEnvelope.GetWidth() / oSize.Width;
            double resolutionY = oEnvelope.GetHeight() / oSize.Height;

            if (oEnvelope.MinX >= tEnvelope.MinX)//左边界在目标图像内部
            {
                oBeginCol = 0;
                tBeginCol = GetInteger((oEnvelope.MinX - tEnvelope.MinX) / resolutionX);
            }
            else//左边界在目标图像外部
            {
                oBeginCol = GetInteger((tEnvelope.MinX - oEnvelope.MinX) / resolutionX);
                tBeginCol = 0;
            }
            if (oEnvelope.MaxX >= tEnvelope.MaxX)//右边界在目标图像外部
            {
                oEndCol = GetInteger((tEnvelope.MaxX - oEnvelope.MinX) / resolutionX);
                tEndCol = tSize.Width;
            }
            else//右边界在目标图像内部
            {
                oEndCol = GetInteger((oEnvelope.MaxX - oEnvelope.MinX) / resolutionX);
                tEndCol = GetInteger((oEnvelope.MaxX - tEnvelope.MinX) / resolutionX);
            }
            if (oEnvelope.MaxY <= tEnvelope.MaxY)//上边界在目标图像内部
            {
                oBeginRow = 0;
                tBeginRow = GetInteger((tEnvelope.MaxY - oEnvelope.MaxY) / resolutionY);
            }
            else//上边界在目标边界外部
            {
                oBeginRow = GetInteger((oEnvelope.MaxY - tEnvelope.MaxY) / resolutionY);
                tBeginRow = 0;
            }
            if (oEnvelope.MinY <= tEnvelope.MinY)//下边界在目标图像外部
            {
                oEndRow = GetInteger((oEnvelope.MaxY - tEnvelope.MinY) / resolutionY);
                tEndRow = tSize.Height;
            }
            else//下边界在目标图像内部
            {
                oEndRow = oSize.Height;
                tEndRow = GetInteger((tEnvelope.MaxY - oEnvelope.MinY) / resolutionY);
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

        private static bool IsInteractived(Envelope envelopeA, Envelope envelopeB)
        {
            RectangleF a = new RectangleF((float)envelopeA.MinX, (float)envelopeA.MinY, (float)envelopeA.GetWidth(), (float)envelopeA.GetHeight());
            RectangleF b = new RectangleF((float)envelopeB.MinX, (float)envelopeB.MinY, (float)envelopeB.GetWidth(), (float)envelopeB.GetHeight());
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
            Envelope oEnvelope = args.GetEnvelope();
            Envelope tEnvelope = dstArgs.GetEnvelope();
            Geometry oGeo =  oEnvelope.GetGeometry();
            Geometry tGeo =  tEnvelope.GetGeometry();

            Geometry inEnvGeo = oGeo.Intersection(tGeo);
            if (inEnvGeo == null)
                return false;
            Envelope inEnv = new Envelope();
            inEnvGeo.GetEnvelope(inEnv);
            
            if (!IsInteractived(oEnvelope, tEnvelope))
                return false;
            float oResolutionX = args.ResolutionX;
            float oResolutionY = args.ResolutionY;
            float tResolutionX = dstArgs.ResolutionX;
            float tResolutionY = dstArgs.ResolutionY;
            
            oIntersectSize = CoordEnvelopeToSize(inEnv, oResolutionX, oResolutionY);
            tIntersectSize = CoordEnvelopeToSize(inEnv, tResolutionX, tResolutionY);
            //
            if (oEnvelope.MinX >= tEnvelope.MinX)//左边界在目标图像内部
            {
                oBeginCol = 0;
                tBeginCol = (int)((oEnvelope.MinX - tEnvelope.MinX) / tResolutionX + _errorValue);
            }
            else//左边界在目标图像外部
            {
                oBeginCol = (int)((tEnvelope.MinX - oEnvelope.MinX) / oResolutionX + _errorValue);
                tBeginCol = 0;
            }
            if (oEnvelope.MaxY <= tEnvelope.MaxY)//上边界在目标图像内部
            {
                oBeginRow = 0;
                tBeginRow = (int)((tEnvelope.MaxY - oEnvelope.MaxY) / tResolutionY + _errorValue);
            }
            else//上边界在目标边界外部
            {
                oBeginRow = (int)((oEnvelope.MaxY - tEnvelope.MaxY) / oResolutionY + _errorValue);
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

        private static Size CoordEnvelopeToSize(Envelope envelope, float resolutonX, float resolutonY)
        {
            int w = (int)((envelope.MaxX - envelope.MinX) / resolutonX + _errorValue);
            int h = (int)((envelope.MaxY - envelope.MinY) / resolutonY + _errorValue);
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
