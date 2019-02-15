using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using OSGeo.GDAL;
using PIE.Meteo.Model;

namespace PIE.Meteo.FileProject
{
    public class OverViewHelper
    {
        /* public static string OverView(AbstractWarpDataset outfileRaster, int _prjPngSize)
         {
             //int height = _prjPngSize;
             int maxWidth = _prjPngSize;
             int matchHeight = Convert.ToInt32(outfileRaster.Height * 1.0* _prjPngSize / outfileRaster.Width );
             //int width = Convert.ToInt32(outfileRaster.Width * 1.0 / outfileRaster.Height * _prjPngSize);
             var rLayer = LayerFactory.CreateDefaultRasterLayer(outfileRaster.ds as IRasterDataset);
             ILayer layer = rLayer as ILayer;
             mRasterRenderTool.Instance.RenderSingleRasterSource(layer, outfileRaster.fileName);
             
             IDisplayTransformation disTrans = new DisplayTransformation();
             disTrans.Bounds = layer.Extent;
             disTrans.DeviceFrame = new System.Drawing.RectangleF(0, 0, maxWidth, matchHeight);
             disTrans.VisibleBounds = layer.Extent;
             disTrans.SpatialReference = outfileRaster.SpatialRef;
 
             Bitmap bit = new Bitmap(maxWidth, matchHeight);
             Graphics graphic = Graphics.FromImage(bit);
             layer.Draw(graphic, disTrans, SystemUI.LayerDrawPhaseType.DPGeography, null);
             //bit.MakeTransparent(Color.Black);
             string outOverviewFile = Path.Combine(Path.GetDirectoryName(outfileRaster.fileName), Path.GetFileNameWithoutExtension(outfileRaster.fileName) + ".OverView.png");
             bit.Save(outOverviewFile);
             //根据输入影像 生成指定尺寸的png
             return outOverviewFile;
         }*/

        public static string OverView(AbstractWarpDataset outfileRaster, int _prjPngSize)
        {
            int maxWidth = _prjPngSize;
            int matchHeight = Convert.ToInt32(outfileRaster.Height * 1.0 * _prjPngSize / outfileRaster.Width);
            string outOverviewFile = Path.Combine(Path.GetDirectoryName(outfileRaster.fileName),
                Path.GetFileNameWithoutExtension(outfileRaster.fileName) + ".overview.png");

            RasterSourceTypeSingleInfo single =
                mRasterSourceManager.GetInstance().GetInputfileRasterSourceInfo(outfileRaster.fileName);

            int[] bandNos = new int[] {3, 2, 1};
            if (single != null)
            {
                var bands = single.RenderCol.Defaultband.Split(',');
                if (bands.Length == 1)
                {
                    bandNos[0] = Convert.ToInt32(bands[0]);
                    bandNos[1] = Convert.ToInt32(bands[0]);
                    bandNos[2] = Convert.ToInt32(bands[0]);
                }
                else if (bands.Length == 3)
                {
                    bandNos[0] = Convert.ToInt32(bands[0]);
                    bandNos[1] = Convert.ToInt32(bands[1]);
                    bandNos[2] = Convert.ToInt32(bands[2]);
                }
            }

            Console.WriteLine("Bands :{0} {1} {2}", bandNos[0], bandNos[1], bandNos[2]);
            var outDs = Gdal.GetDriverByName("MEM").Create("", maxWidth, matchHeight, 4, DataType.GDT_Byte, null);

            UInt16[][] buffer = new UInt16[4][];
            for (int i = 0; i < 4; i++)
            {
                buffer[i] = new UInt16[maxWidth * matchHeight];
            }


            outfileRaster.GetRasterBand(bandNos[0] - 1)
                .ReadRaster(0, 0, outfileRaster.Width, outfileRaster.Height, buffer[0], maxWidth, matchHeight, 0, 0);
            outfileRaster.GetRasterBand(bandNos[1] - 1)
                .ReadRaster(0, 0, outfileRaster.Width, outfileRaster.Height, buffer[1], maxWidth, matchHeight, 0, 0);
            outfileRaster.GetRasterBand(bandNos[2] - 1)
                .ReadRaster(0, 0, outfileRaster.Width, outfileRaster.Height, buffer[2], maxWidth, matchHeight, 0, 0);

            for (int i = 0; i < maxWidth * matchHeight; i++)
            {
                buffer[0][i] = (UInt16) (buffer[0][i] / (UInt16) 4);
                buffer[1][i] = (UInt16) (buffer[1][i] / (UInt16) 4);
                buffer[2][i] = (UInt16) (buffer[2][i] / (UInt16) 4);
                var t = buffer[0][i] + buffer[1][i] + buffer[2][i];
                if (t <= 0 || t >= 255 * 3)
                {
                    buffer[3][i] = 0;
                }
                else
                {
                    buffer[3][i] = 255;
                }
            }

            outDs.GetRasterBand(1).WriteRaster(0, 0, maxWidth, matchHeight, buffer[0], maxWidth, matchHeight, 0, 0);
            outDs.GetRasterBand(2).WriteRaster(0, 0, maxWidth, matchHeight, buffer[1], maxWidth, matchHeight, 0, 0);
            outDs.GetRasterBand(3).WriteRaster(0, 0, maxWidth, matchHeight, buffer[2], maxWidth, matchHeight, 0, 0);
            outDs.GetRasterBand(4).WriteRaster(0, 0, maxWidth, matchHeight, buffer[3], maxWidth, matchHeight, 0, 0);


            Dataset pngDs = Gdal.GetDriverByName("PNG").CreateCopy(outOverviewFile, outDs, 0, null, null, null);
            pngDs.Dispose();

            outDs.Dispose();
            return outOverviewFile;
        }
    }
}