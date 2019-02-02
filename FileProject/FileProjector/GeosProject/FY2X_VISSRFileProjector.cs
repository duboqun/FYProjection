using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIE.Meteo.RasterProject;
using PIE.DataSource;
using PIE.Meteo.Core;
using PIE.Geometry;

namespace PIE.Meteo.FileProject
{
    public class FY2X_VISSRFileProjector : GeosProject
    {
        Fy2_NOM_PrjSettings _setting = null;
        static readonly string fileInfo = "NomFileInfo";
        List<UInt16[]> _lutList = new List<ushort[]>();
        public override void Project(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings, ISpatialReference dstSpatialRef, Action<int, string> progressCallback)
        {
            try
            {
                _setting = prjSettings as Fy2_NOM_PrjSettings;
                InitLutList(srcRaster);
                InitLocationArgs(srcRaster);
                CreateDstRaster(srcRaster, prjSettings, dstSpatialRef, progressCallback);
                WriteMetaData(srcRaster, _prdWriter, prjSettings);
                ReadySession(srcRaster, prjSettings, dstSpatialRef, progressCallback);
                base.Project(srcRaster, prjSettings, dstSpatialRef, progressCallback);
            }
            finally
            {
                EndSession();
            }

        }

        private void InitLutList(AbstractWarpDataset srcRaster)
        {
            PrjBand[] prjBands = PrjBandTable.GetPrjBands(srcRaster);
            for (int i = 0; i < _setting.OutBandNos.Length; i++)
            {
                int bandNo = _setting.OutBandNos[i];
                PrjBand b = prjBands[bandNo - 1];
                string calName = b.DataSetName.Replace("NOMChannel","CALChannel");
                IRasterBand[] bands = srcRaster.GetBands(calName);
                if (bands == null||bands.Length==0)
                    throw new ArgumentNullException(string.Format("FY2X辐射定标，未找到名称为{0}的数据.", calName));
                IRasterBand band = bands[0];
                float[] buffer = new float[band.GetXSize() * band.GetYSize()];
                band.Read(0, 0, band.GetXSize(), band.GetYSize(), buffer, band.GetXSize(), band.GetYSize(), PixelDataType.Float32);
                UInt16[] ubuffer = new ushort[band.GetXSize() * band.GetYSize()];
                for (int j = 0; j < buffer.Length; j++)
                {
                    if (buffer[j] > 1)
                        ubuffer[j] = (UInt16)(buffer[j]*10 + 0.5);
                    else
                        ubuffer[j] = (UInt16)(buffer[j] * 1000 + 0.5);
                }
                _lutList.Add(ubuffer);
            }
        }

        private void InitLocationArgs(AbstractWarpDataset srcRaster)
        {
            double nReslution = 5000;
            var infoDic = srcRaster.TryReadDataTable(fileInfo);
            string proj = "+proj=geos +no_defs +a=6378137.0 +b=6356752.3 +h=35785863";// +h=35785863 +a=6378137.0 +b=6356752.3 +lon_0={0} ";
            if (infoDic.ContainsKey("NOMCenterLon"))
                proj += string.Format(" +lon_0={0}", infoDic["NOMCenterLon"]);
            else
                proj += string.Format(" +lon_0={0}", infoDic["104.5"]);

            double[] geoTransform = new double[6];
            geoTransform[0] = -5720000;
            geoTransform[1] = nReslution;
            geoTransform[2] = 0;
            geoTransform[3] = 5720000 - 0 * nReslution;
            geoTransform[4] = 0;
            geoTransform[5] = -nReslution;
            _srcSpatialRef = new ProjectedCoordinateSystem();
            _srcSpatialRef.ImportFromProj4(proj);
            _srcGeoTrans = geoTransform;
        }

        public override void ComputeDstEnvelope(AbstractWarpDataset srcRaster, ISpatialReference dstSpatialRef, out PrjEnvelope maxPrjEnvelope, Action<int, string> progressCallback)
        {
            InitLocationArgs(srcRaster);
            var projTrans = ProjectionTransformFactory.GetProjectionTransform(_srcSpatialRef, dstSpatialRef);

            float srcResolutionX = Convert.ToSingle(_srcGeoTrans[1]);
            float srcResolutionY = Math.Abs(Convert.ToSingle(_srcGeoTrans[5]));

            double srcLeftTopX = _srcGeoTrans[0];
            double srcLeftTopY = _srcGeoTrans[3];
            int srcWidth = srcRaster.Width;
            int srcHeight = srcRaster.Height;
            Size srcSize = new Size(srcWidth, srcHeight);

            int wSample = 1;
            int hSample = 1;
            if (srcWidth > 1000)
            {
                wSample = srcWidth / 1000;
            }
            if (srcHeight > 1000)
            {
                hSample = srcHeight / 1000;
            }
            double[] xs = new double[(srcWidth / wSample) * (srcHeight / hSample)];
            double[] ys = new double[(srcWidth / wSample) * (srcHeight / hSample)];
            int index = 0;//非真实的索引号，采样后的
            for (int rowInx = 0; rowInx <= (srcHeight - hSample); rowInx += hSample)
            {
                for (int colInx = 0; colInx <= (srcWidth - wSample); colInx += wSample)
                {
                    xs[index] = srcLeftTopX + colInx * srcResolutionX;
                    ys[index] = srcLeftTopY - rowInx * srcResolutionY;
                    index++;
                }
            }
            if (dstSpatialRef.IsSame(SpatialReferenceFactory.CreateSpatialReference(4326)))
            {
                projTrans.Transform(xs, ys);
                GeosCorrection(dstSpatialRef, xs, ys);
                maxPrjEnvelope = PrjEnvelope.GetEnvelope(xs, ys, null);
            }
            else
            {
                _rasterProjector.ComputeDstEnvelope(_srcSpatialRef, xs, ys, srcSize, dstSpatialRef, out maxPrjEnvelope, null);
            }
            if (_setting != null && _setting.OutEnvelope != null)
            {
                //求交
                maxPrjEnvelope.Intersect(_setting.OutEnvelope);
            }
        }

        protected override void DoRadiation(AbstractWarpDataset srcImgRaster, int i, ushort[] srcBandData, float[] solarZenithData, Size srcBlockImgSize, Size angleSize)
        {
            if (_setting.IsRadiation)
            {
                UInt16[] buffer = _lutList[i];
                int bufferLength = _lutList[i].Length;
                for (int j = 0; j < srcBandData.Length; j++)
                {
                    if (srcBandData[j] > bufferLength-1)
                        srcBandData[j] = 65535;
                    else
                        srcBandData[j] = buffer[srcBandData[j]];
                }
            }
        }
    }
}
