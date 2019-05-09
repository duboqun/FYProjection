using HDF.PInvoke;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using H5AttributeId = System.Int32;
using H5DataTypeId = System.Int32;
using H5DataSpaceId = System.Int32;
using H5DataSetId = System.Int32;
using H5GroupId = System.Int32;
using H5ID = System.Int32;
using System.IO;
using System.Runtime.CompilerServices;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using PIE.Meteo.Model;


namespace PIE.Meteo.FileProject
{
    /// <summary>
    /// 包装数据集
    /// </summary>
    public class WarpDataset : AbstractWarpDataset
    {
        public WarpDataset(Dataset ds, string filePath)
        {
            fileName = filePath;
            if (ds != null)
            {
                Init(ds);
            }
        }

        public static WarpDataset Open(string filePath)
        {
            var ds = Gdal.OpenShared(filePath, Access.GA_ReadOnly);
            if (ds == null)
                return new WarpDataset(null, string.Empty) {fileName = filePath};
            else
                return new WarpDataset(ds, filePath);
        }
        
        Dictionary<string,Band[]> cacheBands = new Dictionary<string, Band[]>();
        public override Band[] GetBands(string v)
        {
            if (cacheBands.ContainsKey(v))
                return cacheBands[v];
            
            Band[] bands = null;
            if (!isMultiDs)
                throw new Exception("非HDF数据集，不可使用GetBands(string)");
            int index = SearchSubDatasetIndex(v);
            if (index != -1)
            {
                string subDsPath = ds.GetSubDatasets().Values.ToList()[index];
                var subRasterDs = Gdal.Open(subDsPath, Access.GA_ReadOnly);
                bands = new Band[subRasterDs.RasterCount];
                if (subRasterDs != null)
                {
                    for (int i = 0; i < subRasterDs.RasterCount; i++)
                    {
                        bands[i] = subRasterDs.GetRasterBand(i + 1);
                    }
                }
            }
            else
            {
                string subDs = $"HDF5:\"{fileName}\"://{v}";
                Dataset subRasterDs = null;
                try
                {
                    subRasterDs = Gdal.Open(subDs, Access.GA_ReadOnly);
                }
                catch (Exception e)
                {
                    subRasterDs = null;
                }
                
                if (subRasterDs != null)
                {
                    bands = new Band[subRasterDs.RasterCount];
                    for (int i = 0; i < subRasterDs.RasterCount; i++)
                    {
                        bands[i] = subRasterDs.GetRasterBand(i + 1);
                    }
                }
            }
            cacheBands.Add(v,bands);
            return bands;
        }

        public override AbstractWarpDataset GetDataset(string name)
        {
            WarpDataset rds = null;
            if (isMultiDs)
            {
                var subDsDic = ds.GetSubDatasets();

                if (!subDsDic.ContainsKey(name))
                    throw new Exception(string.Format("不存在名称为{0}的数据集", name));

                rds = WarpDataset.Open(subDsDic[name]);
            }

            return rds;
        }

        public override Band GetRasterBand(int bandNo)
        {
            Band band = null;
            if (isMultiDs)
            {
                var subDsPathList = ds.GetSubDatasets().Values.ToList();
                if (subDsPathList.Count >= bandNo)
                {
                    var subDs = Open(subDsPathList[bandNo - 1]);
                    band = subDs.GetRasterBand(0);
                }
            }
            else
            {
                band = ds.GetRasterBand(bandNo + 1);
            }

            return band;
        }
    }

    
    public abstract class AbstractWarpDataset : IWarpDataset
    {
        public Dataset ds;

        private Dictionary<string, string> _fileAttrs = new Dictionary<string, string>();

        private Dictionary<string, Dictionary<string, string>> _datasetAttrCache =
            new Dictionary<string, Dictionary<string, string>>();

        #region 接口中的属性

        public OSGeo.GDAL.DataType DataType { get; set; }

        public string fileName { get; set; } = string.Empty;

        public DataIdentify DataIdentify { get; set; }

        public SpatialReference SpatialRef { get; set; }

        public int Width { get; set; } = 0;

        public int Height { get; set; } = 0;

        public float ResolutionX { get; set; }

        public float ResolutionY { get; set; }

        public string DriverName { get; set; }

        public int BandCount { get; set; } = -1;

        public bool isMultiDs { get; set; } = false;

        public List<string> DatasetNames { get; set; } = new List<string>();

        public IHdfOperator hdfOperator = null;
        #endregion


        protected void Init(Dataset ds)
        {
            this.ds = ds;
            var subDsDic = ds.GetSubDatasets();
            if (Path.GetExtension(fileName).ToLower()!=".l1b"&& subDsDic.Count > 0)
            {
                isMultiDs = true;
                //初始化IHdfOperator
                {
                    int _h5FileId = H5F.open(fileName, H5F.ACC_RDONLY);

                    if (_h5FileId >= 0)
                    {
                        hdfOperator = new Hdf5Operator(fileName);
                        //HDF5

                        H5F.close(_h5FileId);
                    }
                    else if (HDF4Helper.IsHdf4(fileName))
                    {
                        //HDF4
                        hdfOperator = new Hdf4Operator(fileName);

                    }
                }
                _fileAttrs = hdfOperator?.GetAttributes();

                GetAllSubDatasetFullpaths(ds);

                TryGetSizeOfMultiDs();
            }
            else if (ds.RasterCount>0)
            {
                double[] geoTrans = new double[6];
                ds.GetGeoTransform(geoTrans);

                Width = ds.RasterXSize;
                Height = ds.RasterYSize;
                ResolutionX = Convert.ToSingle(geoTrans[1]);
                ResolutionY = Math.Abs(Convert.ToSingle(geoTrans[5]));
                BandCount = ds.RasterCount;
                DataType = ds.GetRasterBand(1).DataType;
                DriverName = ds.GetDriver().ShortName;
                string wkt = ds.GetProjection();
                if (!string.IsNullOrEmpty(wkt))
                    SpatialRef = new SpatialReference(wkt);
            }
        }

        protected void TryGetSizeOfMultiDs()
        {
            var subDsDic = ds.GetSubDatasets();
            var subDsPathList = ds.GetSubDatasets().Values.ToList();
            RasterSourceTypeSingleInfo info = mRasterSourceManager.GetInstance().GetInputfileRasterSourceInfo(fileName);
            if (subDsDic.Count > 0)
            {
                if (info != null)
                {
                    string name = info.defaultDisplayDataset;
                    string key = subDsDic.Keys.FirstOrDefault(t => t.Contains(name));
                    if (!string.IsNullOrEmpty(key))
                    {
                        var subDs = Gdal.OpenShared(subDsDic[key], Access.GA_ReadOnly);
                        Width = subDs.RasterXSize;
                        Height = subDs.RasterYSize;
                        subDs.Dispose();
                    }

                    return;
                }
                else
                {
                    for (int i = 0; i < subDsDic.Count - 1; i++)
                    {
                        using (var curDs = Gdal.Open(subDsPathList[i], Access.GA_ReadOnly))
                        using (var nextDs = Gdal.Open(subDsPathList[i + 1], Access.GA_ReadOnly))
                        {
                            if (curDs != null && nextDs != null)
                            {
                                if (curDs.RasterXSize != nextDs.RasterXSize ||
                                    (curDs.RasterYSize != nextDs.RasterYSize))
                                {
                                    Width = Height = 0;
                                    return;
                                }
                            }
                        }
                    }

                    using (var rDs = Gdal.Open(subDsPathList[0], Access.GA_ReadOnly))
                    {
                        Width = rDs.RasterXSize;
                        Height = rDs.RasterYSize;
                        return;
                    }
                }
            }
        }

        public Envelope GetEnvelope()
        {
            Envelope env = new Envelope();
            double[] geoTrans = new double[6];
            ds.GetGeoTransform(geoTrans);
            env.MinX = geoTrans[0];
            env.MaxY = geoTrans[3];
            env.MaxX = geoTrans[1] * ds.RasterXSize + geoTrans[0];
            env.MinY = geoTrans[5] * ds.RasterYSize + geoTrans[3];
            return env;
        }

        public void Dispose()
        {
            if (ds is IDisposable)
            {
                (ds as IDisposable)?.Dispose();
                ds = null;
            }
            if (hdfOperator != null)
            {
                hdfOperator.Dispose();
            }
        }

        public abstract Band GetRasterBand(int bandNo);

        public abstract Band[] GetBands(string v);

        public bool TryGetBandNameFromBandNo(int bandNo, out int bandName)
        {
            bandName = bandNo;
            return false;
        }
        public bool TryGetBandNoFromBandName(int bandName, out int bandNo)
        {
            bandNo = bandName;
            return false;
        }
        public bool TryGetBandNameFromBandNos(int[] basebands, out int[] bandNames)
        {
            bandNames = basebands;
            return false;
        }
        public bool TryGetBandNoFromBandNames(int[] basebands, out int[] bandNos)
        {
            bandNos = basebands;
            return false;
        }

        public bool IsBandNameRaster()
        {
            if (isMultiDs)
                return true;
            else
                return false;
        }

        public Dictionary<string, string> GetAttributes()
        {
            return _fileAttrs;
        }

        public Dictionary<string, string> GetDatasetAttributes(string originalDatasetName)
        {
            return hdfOperator?.GetDatasetAttributes(originalDatasetName);
        }


        protected int SearchSubDatasetIndex(string name)
        {
            int index = -1;
            if (isMultiDs)
            {
                var subDsDic = ds.GetSubDatasets();
                int count = subDsDic.Count;
                for (int i = 0; i < count; i++)
                {
                    if (subDsDic.Keys.ToList()[i].Contains(name))
                    {
                        index = i;
                        break;
                    }
                }
            }

            return index;
        }


        #region Attributes












        protected string GetDatasetFullNames(string datasetName, int fileId)
        {
            string shortDatasetName = GetDatasetShortName(datasetName);
            return FindDatasetFullNames(shortDatasetName, fileId);
        }

        protected string FindDatasetFullNames(string shortDatasetName, int fileId)
        {
            for (int i = 0; i < DatasetNames.Count; i++)
            {
                string shortGdalDatasetName = GetDatasetShortName(DatasetNames[i]);
                if (shortGdalDatasetName == shortDatasetName)
                    return DatasetNames[i];
            }
            return null;
        }

        protected string GetDatasetShortName(string datasetName)
        {
            string shortDatasetName = null;
            int groupIndex = datasetName.LastIndexOf("/");
            if (groupIndex == -1)
                shortDatasetName = datasetName;
            else
                shortDatasetName = datasetName.Substring(groupIndex + 1);
            return shortDatasetName;
        }

            protected void GetAllSubDatasetFullpaths(Dataset multiDs)
            {
                var sss = multiDs.GetSubDatasets();
                DatasetNames = sss.Select(t => t.Key).ToList();
            }

        public abstract AbstractWarpDataset GetDataset(string name);
        #endregion

        public Dictionary<string, string> TryReadDataTable(string datasetName)
        {
            Dictionary<string, string> result = null;
            var subDsDic = ds.GetSubDatasets();
            if (subDsDic.Count > 0)
            {
                H5ID h5FileId = H5F.open(fileName, H5F.ACC_RDONLY);
                H5ID datasetId = H5D.open(h5FileId, datasetName);
                H5ID typeId = H5D.get_type(datasetId);
                H5ID spaceId = H5D.get_space(datasetId);
                if (H5T.get_class(typeId) == H5T.class_t.COMPOUND)
                {
                    int numCount = H5T.get_nmembers(typeId);
                    var size = H5T.get_size(typeId);
                    byte[] buffer = new byte[size.ToInt32()];
                    GCHandle hnd = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    int ndims = H5S.get_simple_extent_ndims(spaceId);
                    if (ndims == 1)
                    {
                        result = new Dictionary<string, string>();
                        H5D.read(datasetId, typeId, H5S.ALL, H5S.ALL, H5P.DEFAULT, hnd.AddrOfPinnedObject());

                        for (uint i = 0; i < numCount; i++)
                        {
                            string name = Marshal.PtrToStringAnsi(H5T.get_member_name(typeId, i));
                            int offset = H5T.get_member_offset(typeId, i).ToInt32();

                            H5ID subTypeId = H5T.get_member_type(typeId, i);
                            H5T.class_t typeClass = H5T.get_member_class(typeId, i);
                            string value = ReadBuffer(buffer, offset, typeClass, subTypeId);
                            result.Add(name, value);
                            H5T.close(subTypeId);
                        }
                    }

                    hnd.Free();
                }

                if (spaceId != 0)
                    H5S.close(spaceId);
                if (typeId != 0)
                    H5T.close(typeId);
                if (datasetId != 0)
                    H5D.close(datasetId);
                if (h5FileId != 0)
                    H5F.close(h5FileId);
            }

            return result;
        }

        private string ReadBuffer(byte[] buffer, int offset, H5T.class_t typeClass, int typeId)
        {
            string result = string.Empty;
            IntPtr dataSize = H5T.get_size(typeId);
            H5T.order_t order = H5T.get_order(typeId);
            byte[] temp = new byte[dataSize.ToInt32()];

            Array.Copy(buffer, offset, temp, 0, dataSize.ToInt32());
            if (order == H5T.order_t.BE && typeClass != H5T.class_t.STRING)
            {
                Array.Reverse(temp);
            }
            switch (typeClass)
            {
                case H5T.class_t.NO_CLASS:
                    break;
                case H5T.class_t.INTEGER:
                    // H5T.Sign.TWOS_COMPLEMENT;
                    H5T.sign_t sign = H5T.get_sign(typeId);

                    switch (dataSize.ToInt32())
                    {
                        case 1:
                            result = ((double)BitConverter.ToChar(temp, 0)).ToString("G");
                            break;
                        case 2:
                            switch (sign)
                            {
                                case H5T.sign_t.SGN_2:
                                    result = ((double)BitConverter.ToInt16(temp, 0)).ToString("G");
                                    break;
                                case H5T.sign_t.NONE:
                                    result = ((double)BitConverter.ToUInt16(temp, 0)).ToString("G");
                                    break;
                            }
                            break;
                        case 4:
                            switch (sign)
                            {
                                case H5T.sign_t.SGN_2:
                                    result = ((double)BitConverter.ToInt32(temp, 0)).ToString("G");
                                    break;
                                case H5T.sign_t.NONE:
                                    result = ((double)BitConverter.ToUInt32(temp, 0)).ToString("G");
                                    break;
                            }
                            break;
                        case 8:
                            switch (sign)
                            {
                                case H5T.sign_t.SGN_2:
                                    result = ((double)BitConverter.ToInt64(temp, 0)).ToString("G");
                                    break;
                                case H5T.sign_t.NONE:
                                    result = ((double)BitConverter.ToUInt64(temp, 0)).ToString("G");
                                    break;
                            }
                            break;
                    }
                    break;
                case H5T.class_t.FLOAT:
                    switch (dataSize.ToInt32())
                    {
                        case 4:
                            {
                                result = BitConverter.ToSingle(temp, 0).ToString("G");
                                break;
                            }
                        case 8:
                            {
                                result = BitConverter.ToDouble(temp, 0).ToString("G");
                                break;
                            }
                    }
                    break;
                case H5T.class_t.STRING:
                    {
                        GCHandle handler = GCHandle.Alloc(temp, GCHandleType.Pinned);
                        var str = Marshal.PtrToStringAnsi(handler.AddrOfPinnedObject());
                        handler.Free();
                        result = str;
                        break;
                    }
                default:
                    break;
            }
            return result;
        }

        public int[] GetDefaultBands()
        {
            throw new NotImplementedException();
        }
    }

    public interface IWarpDataset : IDisposable
    {

        DataType DataType { get; set; }

        string fileName { get; set; }

        /// <summary>
        /// 卫星传感器标识
        /// </summary>
        [Obsolete("不要使用这个属性")]
        DataIdentify DataIdentify { get; }

        SpatialReference SpatialRef { get; set; }
        int Width { get; set; }
        int Height { get; set; }
        float ResolutionX { get; set; }
        float ResolutionY { get; set; }
        string DriverName { get; set; }
        int BandCount { get; set; }

        bool isMultiDs { get; set; }
        List<string> DatasetNames { get; set; }

        Envelope GetEnvelope();

        Band GetRasterBand(int bandNo);
        Band[] GetBands(string v);
        AbstractWarpDataset GetDataset(string name);
        bool TryGetBandNameFromBandNo(int bandNo, out int bandName);
        bool TryGetBandNoFromBandName(int bandName, out int bandNo);
        bool TryGetBandNameFromBandNos(int[] basebands, out int[] bandNames);
        bool TryGetBandNoFromBandNames(int[] basebands, out int[] bandNos);
        bool IsBandNameRaster();
        Dictionary<string, string> GetAttributes();
        Dictionary<string, string> GetDatasetAttributes(string originalDatasetName);

        Dictionary<string, string> TryReadDataTable(string datasetName);

    }
}