using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIE.Meteo.RasterProject;
using PIE.Meteo.HsdReader;
using System.Runtime.InteropServices;
using PIE.Meteo.Core;
using PIE.Geometry;
using PIE.DataSource;
using System.Drawing;
using PIE.Meteo.Common;

namespace PIE.Meteo.FileProject
{
    public class H08_FileProjector : BaseBlockProject
    {
        private H08_PrjSetting _setting;
        private List<float[]> _calChannel = null;

        public override void Project(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings, ISpatialReference dstSpatialRef, Action<int, string> progressCallback)
        {


            _setting = prjSettings as H08_PrjSetting;
            _process = _setting.hsdProcess;
            srcRaster = _setting.hsdProcess.GetDataset(_setting.OutBandNos.First());
            MyLog.Log.Print.Info("打开第一通道数据");
            progressCallback?.Invoke(0, "读取位置参数");
            InitLocationArgs(srcRaster);
            CreateDstRaster(srcRaster, prjSettings, dstSpatialRef, progressCallback);
            WriteMetaData(srcRaster, _prdWriter, prjSettings);
            ReadySession(srcRaster, prjSettings, dstSpatialRef, progressCallback);
            MyLog.Log.Print.Info("读取定标系数");
            progressCallback?.Invoke(0, "读取定标系数");
            _calChannel = ReadCalArray(srcRaster, _setting.OutBandNos);
            MyLog.Log.Print.Info("开始投影");
            progressCallback?.Invoke(0, "开始投影");
            base.Project(srcRaster, prjSettings, dstSpatialRef, progressCallback);
            srcRaster.Dispose();
        }

        private List<float[]> ReadCalArray(AbstractWarpDataset srcRaster, int[] outBandNos)
        {
            List<float[]> result = null;
            HSDProcess process = _setting.hsdProcess;
            if (process != null)
            {
                result = new List<float[]>();
                foreach (var item in outBandNos)
                {
                    var calDs = process.GetCalDataset(item);
                    float[] buf = new float[calDs.Width * calDs.Height];
                    calDs.GetRasterBand(0).Read(0, 0, calDs.Width, calDs.Height, buf, calDs.Width, calDs.Height, PixelDataType.Float32);
                    calDs.Dispose();
                    for (int i = 0; i < buf.Length; i++)
                    {
                        if (buf[i] < 0)
                            buf[i] = 0;
                        if (float.IsNaN(buf[i]))
                            buf[i] = 100;

                    }
                    result.Add(buf);
                }
            }
            return result;
        }

        protected override void DoRadiation(AbstractWarpDataset srcImgRaster, int i, ushort[] srcBandData, float[] solarZenithData, Size srcBlockImgSize, Size angleSize)
        {
            if (_setting.IsRadiation)
            {
                float[] calChannel = _calChannel[i]; //辐亮度（反射率）值查找表
                int lengthCal = calChannel.Length;//查找表长度
                int lengthBandData = srcBandData.Length;//目标文件长度

                UInt16[] lut = new UInt16[lengthCal];
                for (int numOrd = 0; numOrd < lengthCal; numOrd++)
                {
                    //用2来区别是反射率的查找表还是亮温的查找表(用1无法准确过滤反射率)
                    if (calChannel[numOrd] <= 2)
                        lut[numOrd] = Convert.ToUInt16(calChannel[numOrd] * 1000);
                    else
                        lut[numOrd] = Convert.ToUInt16(calChannel[numOrd] * 10);
                }

                Parallel.For(0, lengthBandData, d =>
                {
                    if (srcBandData[d] < lengthCal)
                    {
                        srcBandData[d] = lut[srcBandData[d]];
                    }
                    else
                    {
                        srcBandData[d] = 65535;
                    }
                });
            }
        }

        #region ComputeDstEnvelope
        private void InitLocationArgs(AbstractWarpDataset srcRaster)
        {
            _srcSpatialRef = srcRaster.SpatialRef;
            _srcGeoTrans = (srcRaster.ds as IRasterDataset).GetGeoTransform();
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
        #endregion

        public override bool HasVaildEnvelope(AbstractWarpDataset geoRaster, PrjEnvelope validEnv, ISpatialReference dstSpatialRef)
        {
            //ToDo:加入范围判断
            return true;
        }
    }
}
