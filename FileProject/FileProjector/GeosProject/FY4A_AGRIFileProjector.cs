using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIE.Meteo.RasterProject;
using PIE.Meteo.Model;
using System.IO;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using HDF.PInvoke;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace PIE.Meteo.FileProject
{
    public class FY4A_AGRIFileProjector : GeosProject
    {
        private FY4A_AGRIPrjSetting _setting;

        private List<ushort[]> _calChannel = new List<ushort[]>();

        /// <summary>
        /// 中心经度
        /// </summary>
        public string NOMCenterLon = string.Empty;

        /// <summary>
        /// 卫星高度
        /// </summary>
        public string NOMSatHeight = string.Empty;


        public override void Project(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings,
            SpatialReference dstSpatialRef, Action<int, string> progressCallback)
        {
            _setting = prjSettings as FY4A_AGRIPrjSetting;
            progressCallback?.Invoke(0, "读取位置参数");
            InitLocationArgs(srcRaster);
            CreateDstRaster(srcRaster, prjSettings, dstSpatialRef, progressCallback);
            WriteMetaData(srcRaster, _prdWriter, prjSettings);
            ReadySession(srcRaster, prjSettings, dstSpatialRef, progressCallback);
            var calChannel = ReadDataSetToSingle(srcRaster, _setting.OutBandNos);
            for (int i = 0; i < calChannel.Count; i++)
            {
                ushort[] buffer = new ushort[calChannel[i].Length];
                for (int j = 0; j < calChannel[i].Length; j++)
                {
                    if (calChannel[i][j] > 2)
                    {
                        buffer[j] = Convert.ToUInt16(calChannel[i][j] * 10);
                    }
                    else
                    {
                        buffer[j] = Convert.ToUInt16(calChannel[i][j] * 1000);
                    }
                }

                _calChannel.Add(buffer);
            }

            base.Project(srcRaster, prjSettings, dstSpatialRef, progressCallback);
        }


        public override AbstractWarpDataset Project(AbstractWarpDataset srcRaster, FilePrjSettings prjSettings,
            AbstractWarpDataset dstRaster, int beginBandIndex, Action<int, string> progressCallback)
        {
            try
            {
                _setting = prjSettings as FY4A_AGRIPrjSetting;
                progressCallback?.Invoke(0, "读取位置参数");
                InitLocationArgs(srcRaster);
                var calChannel = ReadDataSetToSingle(srcRaster, _setting.OutBandNos);
                for (int i = 0; i < calChannel.Count; i++)
                {
                    ushort[] buffer = new ushort[calChannel[i].Length];
                    for (int j = 0; j < calChannel[i].Length; j++)
                    {
                        if (calChannel[i][j] > 2)
                        {
                            buffer[j] = Convert.ToUInt16(calChannel[i][j] * 10);
                        }
                        else
                        {
                            buffer[j] = Convert.ToUInt16(calChannel[i][j] * 1000);
                        }
                    }

                    _calChannel.Add(buffer);
                }

                ReadySession(srcRaster, prjSettings, dstRaster.SpatialRef, progressCallback);
                SetPrdwritterNodata(dstRaster, NODATA_VALUE);
                return base.Project(srcRaster, prjSettings, dstRaster, beginBandIndex, progressCallback);
            }
            finally
            {
                EndSession();
            }
        }


        protected override void DoRadiation(AbstractWarpDataset srcImgRaster, int i, ushort[] srcBandData,
            float[] solarZenithData, Size srcBlockImgSize, Size angleSize)
        {
            if (_setting.IsRadiation)
            {
                UInt16[] lut = _calChannel[i]; //辐亮度（反射率）值查找表
                int lengthCal = lut.Length; //查找表长度
                int lengthBandData = srcBandData.Length; //目标文件长度


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

        /// <summary>
        /// 设置_rasterProjector与_srcGeoTrans
        /// </summary>
        /// <param name="srcRaster"></param>
        private void InitLocationArgs(AbstractWarpDataset srcRaster)
        {
            RasterDatasetInfo info = mRasterSourceManager.GetInstance().GetRasterDatasetInfo(srcRaster.fileName);
            string resolution = info.BandCol.FirstOrDefault().resolution;
            double nReslution = 0;
            if (!double.TryParse(resolution, out nReslution))
            {
                throw new Exception("获取FY4A影像分辨率失败");
            }

            int beginLineNum = 0;
            var attrsDic = srcRaster.GetAttributes();
            string beginlineNumStr = string.Empty;
            if (attrsDic.ContainsKey("Begin Line Number"))
                beginlineNumStr = attrsDic["Begin Line Number"];
            if (string.IsNullOrEmpty(beginlineNumStr))
            {
                if (attrsDic.ContainsKey("geospatial lat lon extent begin line number"))
                    beginlineNumStr = attrsDic["geospatial lat lon extent begin line number"];
                if (string.IsNullOrEmpty(beginlineNumStr)) beginLineNum = 183;
            }

            if (!string.IsNullOrEmpty(beginlineNumStr))
            {
                StringBuilder sb = new StringBuilder();
                foreach (char c in beginlineNumStr)
                {
                    if ((c >= '0' && c <= '9') || c == ' ' || c == '-') sb.Append(c);
                }

                bool result = int.TryParse(sb.ToString(), out beginLineNum);
            }

            double[] geoTransform = new double[6];
            geoTransform[0] = -5496000;
            geoTransform[1] = nReslution;
            geoTransform[2] = 0;
            geoTransform[3] = 5496000 - beginLineNum * nReslution;
            geoTransform[4] = 0;
            geoTransform[5] = -nReslution;

            //"+proj=geos +h=35785863 +a=6378137.0 +b=6356752.3 +lon_0=104.7 +no_defs"
            string proj =
                "+proj=geos +no_defs +a=6378137.0 +b=6356752.3"; // +h=35785863 +a=6378137.0 +b=6356752.3 +lon_0={0} ";
            if (string.IsNullOrEmpty(NOMCenterLon))
            {
                if (attrsDic.ContainsKey("NOMCenterLon"))
                    proj += string.Format(" +lon_0={0}", attrsDic["NOMCenterLon"]);
                else
                    proj += string.Format(" +lon_0={0}", attrsDic["104.7"]);
            }
            else
            {
                proj += string.Format(" +lon_0={0}", NOMCenterLon);
            }

            if (string.IsNullOrEmpty(NOMSatHeight))
            {
                if (attrsDic.ContainsKey("NOMSatHeight"))
                    proj += string.Format(" +h={0}", attrsDic["NOMSatHeight"]);
                else
                    proj += string.Format(" +h={0}", "35785863");
            }
            else
            {
                proj += string.Format(" +h={0}", NOMSatHeight);
            }

            _srcSpatialRef = new SpatialReference("");
            _srcSpatialRef.ImportFromProj4(proj);
            _srcGeoTrans = geoTransform;
        }

        public override void ComputeDstEnvelope(AbstractWarpDataset srcRaster, SpatialReference dstSpatialRef,
            out PrjEnvelope maxPrjEnvelope, Action<int, string> progressCallback)
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
            int index = 0; //非真实的索引号，采样后的
            for (int rowInx = 0; rowInx <= (srcHeight - hSample); rowInx += hSample)
            {
                for (int colInx = 0; colInx <= (srcWidth - wSample); colInx += wSample)
                {
                    xs[index] = srcLeftTopX + colInx * srcResolutionX;
                    ys[index] = srcLeftTopY - rowInx * srcResolutionY;
                    index++;
                }
            }

            if (dstSpatialRef.IsSame(SpatialReferenceFactory.CreateSpatialReference(4326)) == 1)
            {
                projTrans.Transform(xs, ys);
                GeosCorrection(dstSpatialRef, xs, ys);
                maxPrjEnvelope = PrjEnvelope.GetEnvelope(xs, ys, null);
            }
            else
            {
                _rasterProjector.ComputeDstEnvelope(_srcSpatialRef, xs, ys, srcSize, dstSpatialRef, out maxPrjEnvelope,
                    null);
            }

            if (_setting != null && _setting.OutEnvelope != null)
            {
                //求交
                maxPrjEnvelope.Intersect(_setting.OutEnvelope);
            }
        }

        private List<float[]> ReadDataSetToSingle(AbstractWarpDataset srcbandpro, int[] bands)
        {
            List<float[]> datas = new List<float[]>();
            var prjBands = PrjBandTable.GetPrjBands(srcbandpro);
            long h5FileId = H5F.open(srcbandpro.fileName, H5F.ACC_RDONLY);

            foreach (int index in bands)
            {
                //Single[] data = new Single[srcSize.Width * srcSize.Height];
                var bandIndex = prjBands[index - 1].DataSetIndex;
                string dsName = "CALChannel" + bandIndex.ToString("00");


                long datasetId = H5D.open(h5FileId, dsName);
                if (datasetId <= 0)
                    throw new ArgumentNullException(string.Format("FY4辐射定标，未找到名称为{0}的数据.",
                        "CALChannel" + index.ToString("00")));
                long typeId = H5D.get_type(datasetId);
                long spaceId = H5D.get_space(datasetId);
                if (H5T.get_class(typeId) == H5T.class_t.FLOAT)
                {
                    int rank = H5S.get_simple_extent_ndims(spaceId);
                    ulong[] dims = new ulong[rank];
                    ulong[] maxDims = new ulong[rank];
                    H5S.get_simple_extent_dims(spaceId, dims, maxDims);

                    float[] buffer = new float[dims[0]];
                    GCHandle hnd = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    H5D.read(datasetId, typeId, H5S.ALL, H5S.ALL, H5P.DEFAULT, hnd.AddrOfPinnedObject());
                    hnd.Free();
                    if (buffer.Any(t => t > Math.Pow(10, 10) || t < -Math.Pow(10, 10)))
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            var t = BitConverter.GetBytes(buffer[i]);
                            Array.Reverse(t);
                            buffer[i] = BitConverter.ToSingle(t, 0);
                        }
                    }

                    datas.Add(buffer);
                }

                if (spaceId != 0)
                    H5S.close(spaceId);
                if (typeId != 0)
                    H5T.close(typeId);
                if (datasetId != 0)
                    H5D.close(datasetId);
            }

            if (h5FileId != 0)
                H5F.close(h5FileId);

            return datas;
        }


        public override bool HasVaildEnvelope(AbstractWarpDataset geoRaster, PrjEnvelope validEnv,
            SpatialReference dstSpatialRef)
        {
            //ToDo:加入范围判断
            return true;
        }
    }
}