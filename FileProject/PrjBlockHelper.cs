using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PIE.Meteo.RasterProject;
using System.Drawing;

namespace PIE.Meteo.FileProject
{
    public class PrjBlockHelper
    {
        //源为待裁切的文件，目标为裁切输出的小文件
        public static bool ComputeBeginEndRowCol(PrjEnvelope oEnvelope, Size oSize, PrjEnvelope tEnvelope, Size tSize,
            ref int oBeginRow, ref int oBeginCol, ref int oEndRow, ref int oEndCol,
            ref int tBeginRow, ref int tBeginCol, ref int tEndRow, ref int tEndCol)
        {
            if (!oEnvelope.IntersectsWith(tEnvelope))
                return false;
            double resolutionX = oEnvelope.Width / oSize.Width;
            double resolutionY = oEnvelope.Height / oSize.Height;

            if (oEnvelope.MinX >= tEnvelope.MinX)//左边界在目标图像内部
            {
                oBeginCol = 0;
                tBeginCol = (int)((oEnvelope.MinX - tEnvelope.MinX) / resolutionX + 0.5);
            }
            else//左边界在目标图像外部
            {
                oBeginCol = (int)((tEnvelope.MinX - oEnvelope.MinX) / resolutionX + 0.5);
                tBeginCol = 0;
            }
            if (oEnvelope.MaxX >= tEnvelope.MaxX)//右边界在目标图像外部
            {
                oEndCol = (int)((tEnvelope.MaxX - oEnvelope.MinX) / resolutionX + 0.5);
                tEndCol = tSize.Width;
            }
            else//右边界在目标图像内部
            {
                oEndCol = (int)((oEnvelope.MaxX - oEnvelope.MinX) / resolutionX + 0.5);
                tEndCol = (int)((oEnvelope.MaxX - tEnvelope.MinX) / resolutionX + 0.5);
            }
            if (oEnvelope.MaxY <= tEnvelope.MaxY)//上边界在目标图像内部
            {
                oBeginRow = 0;
                tBeginRow = (int)((tEnvelope.MaxY - oEnvelope.MaxY) / resolutionY + 0.5);
            }
            else//上边界在目标边界外部
            {
                oBeginRow = (int)((oEnvelope.MaxY - tEnvelope.MaxY) / resolutionY + 0.5);
                tBeginRow = 0;
            }
            if (oEnvelope.MinY <= tEnvelope.MinY)//下边界在目标图像外部
            {
                oEndRow = (int)((oEnvelope.MaxY - tEnvelope.MinY) / resolutionY + 0.5);
                tEndRow = tSize.Height;
            }
            else//下边界在目标图像内部
            {
                oEndRow = oSize.Height;
                tEndRow = (int)((tEnvelope.MaxY - oEnvelope.MinY) / resolutionY + 0.5);
            }
            return true;
        }

        public static bool ComputeBlock(PrjEnvelope oEnvelope, Size oSize, PrjEnvelope tEnvelope, Size tSize, out Block oBlock, out Block tBlock)
        {
            int tBeginRow = -1, tEndRow = -1, tBeginCol = -1, tEndCol = -1;
            int oBeginRow = -1, oEndRow = -1, oBeginCol = -1, oEndCol = -1;
            bool ok = ComputeBeginEndRowCol(oEnvelope, oSize, tEnvelope, tSize,
                ref oBeginRow, ref oBeginCol, ref oEndRow, ref oEndCol,
                ref tBeginRow, ref tBeginCol, ref tEndRow, ref tEndCol);
            oBlock = new Block { yBegin = oBeginRow, yEnd = oEndRow, xBegin = oBeginCol, xEnd = oEndCol };
            tBlock = new Block { yBegin = tBeginRow, yEnd = tEndRow, xBegin = tBeginCol, xEnd = tEndCol };
            return ok;
        }
    }
}
