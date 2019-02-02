using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;

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
//            var rLayer = LayerFactory.CreateDefaultRasterLayer(outfileRaster.ds as IRasterDataset);
//            ILayer layer = rLayer as ILayer;
//            mRasterRenderTool.Instance.RenderSingleRasterSource(layer, outfileRaster.fileName);
//
//            //IRasterDisplayProps renderProps = rLayer.Render as IRasterDisplayProps;
//            //renderProps.NoDataColor = Color.Black;
//            //rLayer.Render = renderProps as IRasterRender;
//
//            if(!rLayer.Dataset.GetRasterBand(0).IsExsitNoDataValue())
//            {
//                IRasterFilterProps filterPro = rLayer.Render as IRasterFilterProps;
//                PIE.Carto.ITransparentAfterFilter Afterfilter = new TransparentAfterFilter();
//                Afterfilter.FilterValue = Color.Black;
//                filterPro.AddAfterRasterFilter(Afterfilter as IAfterRasterFilter);
//            }
//
//            Map tempMap = new Map("");
//            tempMap.AddLayer(layer);
//            PIE.Carto.IActiveView activeView = tempMap;
//            activeView.Extent = layer.Extent;
//            Display.IDisplayTransformation dT = activeView.DisplayTransformation;
//            dT.DeviceFrame = new RectangleF(0, 0, maxWidth, matchHeight);
//            //dT.DeviceFrame.Width = maxWidth;
//            //dT.DeviceFrame.Height = matchHeight;
//            PIE.Carto.ExportPNG export = new Carto.ExportPNG();
//            export.Width = maxWidth;
//            export.Height = matchHeight;
//            string outOverviewFile = Path.Combine(Path.GetDirectoryName(outfileRaster.fileName), Path.GetFileNameWithoutExtension(outfileRaster.fileName) + ".overview.png");
//            export.ExportFileName = outOverviewFile;
//            export.StartExporting();
//            activeView.Output(export as PIE.Carto.IExport, 96, dT.DeviceFrame, layer.Extent, null);
//            export.FinishExporting();
//            return outOverviewFile;
            return string.Empty;
        }
    }
}
