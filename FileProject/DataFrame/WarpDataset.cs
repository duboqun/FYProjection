using HDF.PInvoke;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using H5AttributeId = System.Int64;
using H5DataTypeId = System.Int64;
using H5DataSpaceId = System.Int64;
using H5DataSetId = System.Int64;
using H5GroupId = System.Int64;
using System.IO;
using System.Runtime.CompilerServices;
using  OSGeo.GDAL;
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
        public WarpDataset(Dataset ds,string filePath)
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
                return new WarpDataset(null,string.Empty) { fileName = filePath };
            else
                return new WarpDataset(ds,filePath);
        }

        public override Band[] GetBands(string v)
        {
            Band[] bands = null;
            if (!isMultiDs)
                throw new Exception("非HDF数据集，不可使用GetBands(string)");
            int index = SearchSubDatasetIndex(v);
            if (index != -1)
            {
                string subDsPath = ds.GetSubDatasets().Values.ToList()[index];
                var subRasterDs = Gdal.OpenShared(subDsPath, Access.GA_ReadOnly);
                bands = new Band[subRasterDs.RasterCount];
                for (int i = 0; i < subRasterDs.RasterCount; i++)
                {
                    bands[i] = subRasterDs.GetRasterBand(i+1);
                }
            }
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
                var subDsPathList =ds.GetSubDatasets().Values.ToList();
                if (subDsPathList.Count >= bandNo)
                {
                    var subDs = Open(subDsPathList[bandNo-1]);
                    band = subDs.GetRasterBand(0);
                }
            }
            else
            {
                    band = ds.GetRasterBand(bandNo+1);
            }
            return band;
        }
    }

    /// <summary>
    /// 包装数据集
    /// </summary>
    public abstract class AbstractWarpDataset : IWarpDataset
    {
        public Dataset ds;

        private Dictionary<string, string> _fileAttrs = new Dictionary<string, string>();

        private Dictionary<string, Dictionary<string, string>> _datasetAttrCache = new Dictionary<string, Dictionary<string, string>>();

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
        #endregion


        protected void Init(Dataset ds)
        {
            this.ds = ds;
            var subDsDic = ds.GetSubDatasets();
            if (subDsDic.Count>0)
            {
                isMultiDs = true;

                Int64 _h5FileId = H5F.open(fileName, H5F.ACC_RDONLY);

                if (_h5FileId >= 0)
                {
                    GetAllFileAttributes(_h5FileId);
                    H5F.close(_h5FileId);
                }

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
                if(!string.IsNullOrEmpty(wkt))
                    SpatialRef = new SpatialReference(wkt);
            }
        }

        protected void TryGetSizeOfMultiDs()
        {
            var subDsDic = ds.GetSubDatasets();
            var subDsPathList = ds.GetSubDatasets().Values.ToList();
            RasterSourceTypeSingleInfo info = mRasterSourceManager.GetInstance().GetInputfileRasterSourceInfo(fileName);
            if (subDsDic.Count>0)
            {
                if (info != null)
                {
                    string name = info.defaultDisplayDataset;
                    var subDs = Gdal.OpenShared(subDsDic[name], Access.GA_ReadOnly);
                    Width = subDs.RasterXSize;
                    Height = subDs.RasterYSize;
                    subDs.Dispose();
                    return;
                }
                else
                {
                    for (int i = 0; i < subDsDic.Count-1; i++)
                    {
                        using (var curDs = Gdal.Open(subDsPathList[i], Access.GA_ReadOnly))
                        using (var nextDs = Gdal.Open(subDsPathList[i+1], Access.GA_ReadOnly))
                        {
                            if (curDs!=null&&nextDs!=null)
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
            env.MaxX = geoTrans[1] * ds.RasterXSize+geoTrans[0];
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
        }

        public abstract Band GetRasterBand(int bandNo);

        public abstract Band[] GetBands(string v);

        //public WarpDataset GetDataset(string name)
        //{

        //}

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
            if (_datasetAttrCache.ContainsKey(originalDatasetName))
                return _datasetAttrCache[originalDatasetName];

            long h5FileId = 0;
            H5DataSetId datasetId = 0;
            H5GroupId groupId = 0;
            H5DataTypeId typeId = 0;
            H5DataSpaceId spaceId = 0;
            try
            {
                h5FileId = H5F.open(fileName, H5F.ACC_RDONLY);
                if (h5FileId == 0)
                    return null;
                string datasetName = GetDatasetFullNames(originalDatasetName, h5FileId);
                if (string.IsNullOrEmpty(datasetName))
                    return null;
                int groupIndex = datasetName.LastIndexOf('/');
                if (groupIndex == -1)
                    datasetId = H5D.open(h5FileId, datasetName);
                else
                {
                    string groupName = datasetName.Substring(0, groupIndex + 1);
                    string dsName = datasetName.Substring(groupIndex + 1);
                    groupId = H5G.open(h5FileId, groupName);
                    datasetId = H5D.open(groupId, dsName);
                }
                if (datasetId == 0)
                    return null;
                Dictionary<string, string> attValues = new Dictionary<string, string>();

                typeId = H5D.get_type(datasetId);
                H5T.class_t type = H5T.get_class(typeId);
                IntPtr tSize = H5T.get_size(typeId);

                spaceId = H5D.get_space(datasetId);

                int length = H5S.get_simple_extent_ndims(spaceId);
                ulong[] dims = new ulong[length];
                H5S.get_simple_extent_dims(spaceId, dims, null);
                ulong storageSize = H5D.get_storage_size(datasetId);

                attValues.Add("DataSetName", datasetName);
                attValues.Add("DataType", type.ToString());
                attValues.Add("DataTypeSize", tSize.ToString() + "Byte");
                attValues.Add("Dims", String.Join("*", dims));
                attValues.Add("StorageSize", storageSize.ToString() + "Byte");


                //所有Attributes的键
                ArrayList arrayList = new ArrayList();
                GCHandle handle = GCHandle.Alloc(arrayList);
                ulong n = 0;
                // the callback is defined in H5ATest.cs
                H5A.operator_t cb = (Int64 location_id, IntPtr attr_name, ref H5A.info_t ainfo, IntPtr op_data) =>
                {
                    GCHandle hnd = (GCHandle)op_data;
                    ArrayList al = (hnd.Target as ArrayList);
                    int len = 0;
                    while (Marshal.ReadByte(attr_name, len) != 0) { ++len; }
                    byte[] buf = new byte[len];
                    Marshal.Copy(attr_name, buf, 0, len);
                    al.Add(Encoding.UTF8.GetString(buf));
                    return 0;
                };
                H5A.iterate(datasetId, H5.index_t.NAME, H5.iter_order_t.NATIVE, ref n, cb, (IntPtr)handle);
                handle.Free();

                foreach (string attName in arrayList)
                {
                    attValues.Add(attName, ReadAttributeValue(datasetId, attName));
                }
                _datasetAttrCache.Add(originalDatasetName, attValues);
                return attValues;
            }
            finally
            {
                if (spaceId != 0)
                    H5S.close(spaceId);
                if (typeId != 0)
                    H5T.close(typeId);
                if (datasetId != 0)
                    H5D.close(datasetId);
                if (groupId != 0)
                    H5G.close(groupId);
                if (h5FileId != 0)
                    H5F.close(h5FileId);
            }
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
        protected void GetAllFileAttributes(long _h5FileId)
        {
            //所有Attributes的键
            ArrayList arrayList = new ArrayList();
            GCHandle handle = GCHandle.Alloc(arrayList);
            ulong n = 0;

            // the callback is defined in H5ATest.cs
            H5A.operator_t cb = (Int64 location_id, IntPtr attr_name, ref H5A.info_t ainfo, IntPtr op_data) =>
            {
                GCHandle hnd = (GCHandle)op_data;
                ArrayList al = (hnd.Target as ArrayList);
                int len = 0;
                while (Marshal.ReadByte(attr_name, len) != 0) { ++len; }
                byte[] buf = new byte[len];
                Marshal.Copy(attr_name, buf, 0, len);
                al.Add(Encoding.UTF8.GetString(buf));
                return 0;
            };

            H5A.iterate(_h5FileId, H5.index_t.NAME, H5.iter_order_t.NATIVE, ref n, cb, (IntPtr)handle);
            handle.Free();

            foreach (string attributeName in arrayList)
            {
                _fileAttrs.Add(attributeName, ReadAttributeValue(_h5FileId, attributeName));
            }
        }

        protected string ReadAttributeValue(long _h5FileId, string attributeName)
        {
            object v = GetAttributeValue(_h5FileId, attributeName);
            return TryArrayToString(v);
        }
        protected string TryArrayToString(object v)
        {
            if (v == null)
                return string.Empty;
            double[] attrArray;
            if (v is double[])
            {
                attrArray = v as double[];
                return doubleArrayJoin(attrArray);
            }
            else if (v is float[])
            {
                attrArray = (v as float[]).Select(t => Convert.ToDouble(t)).ToArray();
                return doubleArrayJoin(attrArray);
            }
            else if (v is byte[])
            {
                attrArray = (v as byte[]).Select(t => Convert.ToDouble(t)).ToArray();
                return doubleArrayJoin(attrArray);
            }
            else if (v is UInt16[])
            {
                attrArray = (v as UInt16[]).Select(t => Convert.ToDouble(t)).ToArray();
                return doubleArrayJoin(attrArray);
            }
            else if (v is Int16[])
            {
                attrArray = (v as Int16[]).Select(t => Convert.ToDouble(t)).ToArray();
                return doubleArrayJoin(attrArray);
            }
            else if (v is Int32[])
            {
                attrArray = (v as Int32[]).Select(t => Convert.ToDouble(t)).ToArray();
                return doubleArrayJoin(attrArray);
            }
            else if (v is UInt32[])
            {
                attrArray = (v as UInt32[]).Select(t => Convert.ToDouble(t)).ToArray();
                return doubleArrayJoin(attrArray);
            }
            else if (v is Int64[])
            {
                attrArray = (v as Int64[]).Select(t => Convert.ToDouble(t)).ToArray();
                return doubleArrayJoin(attrArray);
            }
            else if (v is UInt64[])
            {
                attrArray = (v as UInt64[]).Select(t => Convert.ToDouble(t)).ToArray();
                return doubleArrayJoin(attrArray);
            }


            return v.ToString().Replace('\0', ' ').TrimEnd();
        }

        private string doubleArrayJoin(double[] array)
        {
            var strs = array.Select(t => t.ToString("G"));
            return string.Join(",", strs);
        }
        protected object GetAttributeValue(long _h5FileId, string attributeName)
        {
            H5AttributeId attId = H5A.open(_h5FileId, attributeName);
            if (attId == 0)
                return null;
            H5DataTypeId typeId = 0;
            H5DataTypeId dtId = 0;
            H5A.info_t attInfo = new H5A.info_t();
            H5DataSpaceId spaceId = 0;
            H5DataTypeId oldTypeId = 0;
            object retObject = null;
            try
            {
                typeId = H5A.get_type(attId);
                H5A.get_info(attId, ref attInfo);
                dtId = H5A.get_type(attId);
                spaceId = H5A.get_space(attId);
                IntPtr dataSize = H5T.get_size(dtId);
                //
                oldTypeId = typeId;
                typeId = H5T.get_native_type(typeId, H5T.direction_t.DEFAULT);
                H5T.class_t typeClass = H5T.get_class(typeId);
                int ndims = H5S.get_simple_extent_ndims(spaceId);
                ulong[] dims = new ulong[ndims];
                H5S.get_simple_extent_dims(spaceId, dims, null);
                ulong dimSize = 1;
                if (dims.Length == 0)
                {
                    dimSize = 1;
                }
                else
                {
                    foreach (ulong dim in dims)
                    {
                        dimSize *= dim;
                    }
                }
                switch (typeClass)
                {
                    case H5T.class_t.NO_CLASS:
                        break;
                    case H5T.class_t.INTEGER:
                        // H5T.Sign.TWOS_COMPLEMENT;
                        H5T.sign_t sign = H5T.get_sign(oldTypeId);
                        switch (dataSize.ToInt32())
                        {
                            case 1:
                                retObject = ReadArray<byte>(dimSize, attId, typeId);
                                break;
                            case 2:
                                switch (sign)
                                {
                                    case H5T.sign_t.SGN_2:
                                        retObject = ReadArray<Int16>(dimSize, attId, typeId);
                                        break;
                                    case H5T.sign_t.NONE:
                                        retObject = ReadArray<UInt16>(dimSize, attId, typeId);
                                        break;
                                }
                                break;
                            case 4:
                                switch (sign)
                                {
                                    case H5T.sign_t.SGN_2:
                                        retObject = ReadArray<Int32>(dimSize, attId, typeId);
                                        break;
                                    case H5T.sign_t.NONE:
                                        retObject = ReadArray<UInt32>(dimSize, attId, typeId);
                                        break;
                                }
                                break;
                            case 8:
                                switch (sign)
                                {
                                    case H5T.sign_t.SGN_2:
                                        retObject = ReadArray<Int64>(dimSize, attId, typeId);
                                        break;
                                    case H5T.sign_t.NONE:
                                        retObject = ReadArray<UInt64>(dimSize, attId, typeId);
                                        break;
                                }
                                break;
                        }
                        break;
                    case H5T.class_t.FLOAT:
                        switch (dataSize.ToInt32())
                        {
                            case 4:
                                retObject = ReadArray<float>(dimSize, attId, typeId);
                                break;
                            case 8:
                                retObject = ReadArray<double>(dimSize, attId, typeId);
                                break;
                        }
                        break;
                    case H5T.class_t.STRING:
                        ulong size = attInfo.data_size;
                        byte[] chars = ReadArray<byte>(size, attId, typeId);
                        retObject = Encoding.ASCII.GetString(chars);
                        break;
                    default:
                        break;
                }
                return retObject;
            }
            finally
            {
                if (spaceId != 0)
                    H5S.close(spaceId);
                if (attId != 0)
                    H5A.close(attId);
                if (oldTypeId != 0)
                    H5T.close(oldTypeId);
                if (typeId != 0)
                    H5T.close(typeId);
                if (dtId != 0)
                    H5T.close(dtId);
            }
        }

        protected T[] ReadArray<T>(ulong size, long attId, long typeId)
        {
            T[] v = new T[size];
            if (size == 0)
                return v;
            GCHandle hnd = GCHandle.Alloc(v, GCHandleType.Pinned);
            H5A.read(attId, typeId, hnd.AddrOfPinnedObject());
            return v;
        }


        protected string GetDatasetFullNames(string datasetName, long fileId)
        {
            string shortDatasetName = GetDatasetShortName(datasetName);
            return FindDatasetFullNames(shortDatasetName, fileId);
        }

        protected string FindDatasetFullNames(string shortDatasetName, long fileId)
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
            if (subDsDic.Count>0)
            {
                long h5FileId = H5F.open(fileName, H5F.ACC_RDONLY);
                long datasetId = H5D.open(h5FileId, datasetName);
                long typeId = H5D.get_type(datasetId);
                long spaceId = H5D.get_space(datasetId);
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
                            
                            long subTypeId = H5T.get_member_type(typeId, i);
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

        private string ReadBuffer(byte[] buffer, int offset, H5T.class_t typeClass, long typeId)
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
        OSGeo.GDAL.DataType DataType { get; set; }

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
