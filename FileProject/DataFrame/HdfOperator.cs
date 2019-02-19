using HDF.PInvoke;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using H5AttributeId = System.Int32;
using H5DataTypeId = System.Int32;
using H5DataSpaceId = System.Int32;
using H5DataSetId = System.Int32;
using H5GroupId = System.Int32;
using H5FileId = System.Int32;

namespace PIE.Meteo.FileProject
{
    #region 读取HDF4
    /// <summary>
    /// 读取HDF4
    /// </summary>
    public static class HDF4Helper
    {
        public static bool IsHdf4(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] buffer = new byte[4];
                try
                {
                    fs.Read(buffer, 0, 4);
                }
                catch (ArgumentOutOfRangeException)
                {
                    return false;
                }
                //HDF4的文件头，共4个字节
                byte[] hdf4 = new byte[4] { 0x0E, 0x03, 0x13, 0x01 };
                for (int i = 0; i < buffer.Length; i++)
                    if (hdf4[i] != buffer[i])
                        return false;
            }
            return true;
        }

        public static bool IsHdf4(byte[] bytes1024)
        {
            if (bytes1024 == null || bytes1024.Length < 4)
                return false;
            //HDF4的文件头，共4个字节
            byte[] hdf4 = new byte[4] { 0x0E, 0x03, 0x13, 0x01 };
            for (int i = 0; i < hdf4.Length; i++)
                if (hdf4[i] != bytes1024[i])
                    return false;
            return true;
        }

        #region DLL Imports
        private const string HDF4DLL = "libmfhdf.so";

        #region file / dataset access

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDcreate(int sd_id, string name, DataTypeDefinitions data_type, int rank, int[] dimsizes);

        [DllImport(HDF4DLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDstart(string filename, AccessCodes access_mode);

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern short SDendaccess(int sds_id);

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern short SDend(int sd_id);

        #endregion

        #region data access

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDselect(int sd_id, int sds_index);

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDreaddata(int sds_id, int[] start, int[] stride, int[] edge, IntPtr buffer);

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDwritedata(int sds_id, int[] start, int[] stride, int[] edge, IntPtr buffer);

        [DllImport(HDF4DLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern short SDgetdatastrs(int sds_id, StringBuilder label, StringBuilder unit, StringBuilder format, int length);

        [DllImport(HDF4DLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern short SDsetdatastrs(int sds_id, string label, string unit, string format, string coordsys);

        #endregion

        #region dim

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDgetdimid(int sds_id, int dim_index);

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern short SDdiminfo(int dim_id, StringBuilder name, out int size, out DataTypeDefinitions datay_type, out int num_attrs);

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern short SDgetdimstrs(int dim_id, StringBuilder label, StringBuilder unit, StringBuilder format, int length);

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern short SDgetdimscale(int dim_id, IntPtr data);

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern short SDsetdimname(int dim_id, string dim_name);

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern short SDsetdimscale(int dim_id, int count, DataTypeDefinitions data_type, IntPtr data);

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern short SDsetdimstrs(int dim_id, string label, string unit, string format);

        #endregion

        #region lookups / checks

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDcheckempty(int sds_id, out bool emptySDS);

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDnametoindex(int sd_id, string sds_name);

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDfileinfo(int sd_id, out int num_datasets, out int num_global_attrs);

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern short SDgetinfo(int sds_id, StringBuilder sds_name, out int rank, [In, Out] int[] dimsizes, out DataTypeDefinitions data_type,
            out int num_attrs);

        [DllImport(HDF4DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SDiscoordvar(int sds_id);

        #endregion

        #region attr

        [DllImport(HDF4DLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDfindattr(int obj_id, string attr_name);

        [DllImport(HDF4DLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern short SDattrinfo(int obj_id, int attr_index, StringBuilder attr_name, out DataTypeDefinitions data_type, out int count);

        /// <summary>
        /// C中定义原参见
        /// http://www.hdfgroup.org/training/HDFtraining/RefManual/RM_Section_II_SD.fm22.html
        /// IntPtr attr_buff也可以定义为void* attr_buff
        /// </summary>
        /// <param name="obj_id"></param>
        /// <param name="attr_index"></param>
        /// <param name="attr_buff">这里也可以</param>
        /// <returns></returns>
        [DllImport(HDF4DLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern short SDreadattr(int obj_id, int attr_index, IntPtr attr_buff);

        [DllImport(HDF4DLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern short SDsetattr(int obj_id, string attr_name, DataTypeDefinitions data_type, int count, StringBuilder values);

        #endregion

        #endregion

        #region Enums / Consts

        public const uint DFNT_NATIVE = 4096;
        public const int MAX_VAR_DIMS = 32;

        [Flags]
        public enum DataTypeDefinitions : uint
        {
            DFNT_CHAR8 = 4,
            DFNT_CHAR = 4,
            DFNT_UCHAR8 = 3,
            DFNT_UCHAR = 3,
            DFNT_INT8 = 20,
            DFNT_UINT8 = 21,
            DFNT_INT16 = 22,
            DFNT_UINT16 = 23,
            DFNT_INT32 = 24,
            DFNT_UINT32 = 25,
            DFNT_FLOAT32 = 5,
            DFNT_FLOAT64 = 6,
            DFNT_NINT8 = DFNT_NATIVE | DFNT_INT8,
            DFNT_NUINT8 = DFNT_NATIVE | DFNT_UINT8,
            DFNT_NINT16 = DFNT_NATIVE | DFNT_INT16,
            DFNT_NUINT16 = DFNT_NATIVE | DFNT_UINT16,
            DFNT_NINT32 = DFNT_NATIVE | DFNT_INT32,
            DFNT_NUINT32 = DFNT_NATIVE | DFNT_UINT32,
            DFNT_NFLOAT32 = DFNT_NATIVE | DFNT_FLOAT32,
            DFNT_NFLOAT64 = DFNT_NATIVE | DFNT_FLOAT64
        }

        public enum AccessCodes : int
        {
            DFACC_READ = 1,
            DFACC_WRITE = 2,
            DFACC_CREATE = 4,
            DFACC_ALL = 7,
            DFACC_RDONLY = 1,
            DFACC_RDWR = 3
        }
        #endregion

        #region Helper Functions

        public static double[] SDgetdimscaledouble(int dim_id, int size)
        {
            double d = 0;
            IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(d) * size);
            SDgetdimscale(dim_id, p);
            double[] dest = new double[size];
            Marshal.Copy(p, dest, 0, size);
            Marshal.FreeHGlobal(p);
            return dest;
        }

        public static void SDsetdimscaledouble(int dim_id, double[] d)
        {
            IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(d[0]) * d.Length);
            Marshal.Copy(d, 0, p, d.Length);
            int res = SDsetdimscale(dim_id, d.Length, DataTypeDefinitions.DFNT_FLOAT64, p);
            Marshal.FreeHGlobal(p);
        }

        public static double SDreaddouble(int sds_id, int[] start, int[] edge)
        {
            double d = 0;
            IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(d));
            SDreaddata(sds_id, start, null, edge, p);
            double[] dest = new double[1];
            Marshal.Copy(p, dest, 0, 1);
            d = dest[0];
            Marshal.FreeHGlobal(p);
            return d;
        }

        public static void SDwritedouble(int sds_id, int[] start, int[] edge, double d)
        {
            IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(d));
            double[] a = new double[1];
            a[0] = d;
            Marshal.Copy(a, 0, p, 1);
            int res = SDwritedata(sds_id, start, null, edge, p);
            Marshal.FreeHGlobal(p);
        }

        #endregion
    }
    #endregion
    public interface IHdfOperator : IDisposable
    {
        string GetAttributeValue(string attributeName);
        string GetAttributeValue(string datasetName, string attributeName);
        string[] GetDatasetNames { get; }
        Dictionary<string, string> GetAttributes();
        Dictionary<string, string> GetDatasetAttributes(string datasetName);
    }
    public class Hdf4Operator : IHdfOperator
    {
        private const int MAX_DIMSIZES = 10;
        private string _fname = null;

        /// <summary>
        /// 所有数据集
        /// </summary>
        protected string[] _datasetNames = null;
        /// <summary>
        /// 文件属性
        /// </summary>
        protected Dictionary<string, string> _fileAttrs = new Dictionary<string, string>();
        private bool _readFileAttrs = false;
        int sd_id = -1;

        public Hdf4Operator(string fname)
        {
            _fname = fname;
            sd_id = HDF4Helper.SDstart(_fname, HDF4Helper.AccessCodes.DFACC_READ);
            _datasetNames = GetALLDataset();
        }

        public string[] GetDatasetNames
        {
            get { return _datasetNames; }
        }

        private string[] GetALLDataset()
        {
            int num_datasets = 0;
            int num_global_attrs = 0;
            HDF4Helper.SDfileinfo(sd_id, out num_datasets, out num_global_attrs);
            string[] dsNames = ReadSDinfo(sd_id, num_datasets);
            return dsNames;
        }

        private static string[] ReadSDinfo(int sd_id, int num_datasets)
        {
            string[] dsNames = new string[num_datasets];
            for (int dsIndex = 0; dsIndex < num_datasets; dsIndex++)
            {
                int sds_id = HDF4Helper.SDselect(sd_id, dsIndex);
                try
                {
                    StringBuilder sds_name = new StringBuilder(256);
                    int rank;
                    int[] dimsizes = new int[256];
                    HDF4Helper.DataTypeDefinitions data_type;
                    int num_attrs;
                    HDF4Helper.SDgetinfo(sds_id, sds_name, out rank, dimsizes, out data_type, out num_attrs);
                    dsNames[dsIndex] = sds_name.ToString();
                }
                finally
                {
                    HDF4Helper.SDend(sds_id);
                }

            }
            return dsNames;
        }

        private string ReadAttribute(int objId, int attIndex)
        {
            HDF4Helper.DataTypeDefinitions dType = HDF4Helper.DataTypeDefinitions.DFNT_CHAR;
            int count = 0;
            StringBuilder attrName = new StringBuilder(256);
            HDF4Helper.SDattrinfo(objId, attIndex, attrName, out dType, out count);
            string attValue = GetAttributevalue(dType, objId, attIndex, count);
            return attValue;
        }

        public string GetAttributeValue(string attributeName)
        {
            GetAttributes();
            if (_fileAttrs == null || _fileAttrs.Count == 0)
                return null;
            else if (_fileAttrs.ContainsKey(attributeName))
                return _fileAttrs[attributeName];
            else
                return null;
        }

        public string GetAttributeValue(string datasetName, string attributeName)
        {

            try
            {
                int dsIndex = HDF4Helper.SDnametoindex(sd_id, datasetName);
                int dsObjId = HDF4Helper.SDselect(sd_id, dsIndex);
                int attIndex = HDF4Helper.SDfindattr(dsObjId, attributeName);
                return ReadAttribute(dsObjId, attIndex);
            }
            finally
            {

            }
        }

        public Dictionary<string, string> GetAttributes()
        {
            if (!_readFileAttrs)
            {
                _readFileAttrs = true;

                try
                {
                    int dsCount = 0;
                    int attCount = 0;
                    HDF4Helper.SDfileinfo(sd_id, out dsCount, out attCount);
                    if (attCount == 0)
                        return null;
                    _fileAttrs = GetAttributes(sd_id, attCount);
                }
                finally
                {

                }
            }
            return _fileAttrs;
        }

        public Dictionary<string, string> GetDatasetAttributes(string datasetName)
        {
            StringBuilder sds_name = new StringBuilder();
            int rank = 0;
            int[] dimsizes = new int[MAX_DIMSIZES];
            HDF4Helper.DataTypeDefinitions datatype;
            int attCount = 0;
            try
            {
                int dsIndex = HDF4Helper.SDnametoindex(sd_id, datasetName);
                System.Diagnostics.Debug.WriteLine($"name:{datasetName} index:{dsIndex}");
                int dsObjId = HDF4Helper.SDselect(sd_id, dsIndex);
                System.Diagnostics.Debug.WriteLine($"dsObjId:{dsObjId}");
                HDF4Helper.SDgetinfo(dsObjId, sds_name, out rank, dimsizes, out datatype, out attCount);
                //int dsCount = 0;
                //HDF4Helper.SDfileinfo(id, out dsCount, out attCount);
                if (attCount == 0)
                    return null;
                return GetAttributes(dsObjId, attCount);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public bool GetDataSizeInfos(string datasetName, out int rank, out int[] dimsizes, out Type dataType, out int dataTypeSize)
        {
            HDF4Helper.DataTypeDefinitions hdf4Type = HDF4Helper.DataTypeDefinitions.DFNT_NUINT16;
            if (!GetDataSizeInfos(datasetName, out rank, out dimsizes, out hdf4Type, out dataType, out dataTypeSize))
                return false;
            return true;
        }

        public bool GetDataSizeInfos(string datasetName, out int rank, out int[] dimsizes, out HDF4Helper.DataTypeDefinitions hdf4Type, out Type dataType, out int dataTypeSize)
        {
            StringBuilder sds_name = new StringBuilder();
            rank = 0;
            dimsizes = new int[MAX_DIMSIZES];
            dataType = typeof(UInt16);
            hdf4Type = HDF4Helper.DataTypeDefinitions.DFNT_NUINT16;
            dataTypeSize = 0;

            int dsIndex = HDF4Helper.SDnametoindex(sd_id, datasetName);
            int dsObjId = HDF4Helper.SDselect(sd_id, dsIndex);
            int attCount = 0;
            HDF4Helper.SDgetinfo(dsObjId, sds_name, out rank, dimsizes, out hdf4Type, out attCount);
            if (rank != 0)
            {
                dataType = GetTypeFromHDF4DataType(hdf4Type, out dataTypeSize);
                return true;
            }

            return false;

        }

        public static Type GetTypeFromHDF4DataType(HDF4Helper.DataTypeDefinitions data_type, out int dataTypeSize)
        {
            dataTypeSize = 0;
            switch (data_type)
            {
                case HDF4Helper.DataTypeDefinitions.DFNT_CHAR:
                case HDF4Helper.DataTypeDefinitions.DFNT_UCHAR:
                    dataTypeSize = Marshal.SizeOf(typeof(byte));
                    return typeof(byte);
                case HDF4Helper.DataTypeDefinitions.DFNT_INT8:
                    dataTypeSize = Marshal.SizeOf(typeof(byte));
                    return typeof(byte);
                case HDF4Helper.DataTypeDefinitions.DFNT_UINT8:
                    dataTypeSize = Marshal.SizeOf(typeof(byte));
                    return typeof(byte);
                case HDF4Helper.DataTypeDefinitions.DFNT_INT16:
                    dataTypeSize = Marshal.SizeOf(typeof(Int16));
                    return typeof(Int16);
                case HDF4Helper.DataTypeDefinitions.DFNT_UINT16:
                    dataTypeSize = Marshal.SizeOf(typeof(UInt16));
                    return typeof(UInt16);
                case HDF4Helper.DataTypeDefinitions.DFNT_INT32:
                    dataTypeSize = Marshal.SizeOf(typeof(int));
                    return typeof(int);
                case HDF4Helper.DataTypeDefinitions.DFNT_UINT32:
                    dataTypeSize = Marshal.SizeOf(typeof(uint));
                    return typeof(uint);
                case HDF4Helper.DataTypeDefinitions.DFNT_FLOAT32:
                    dataTypeSize = Marshal.SizeOf(typeof(float));
                    return typeof(float);
                case HDF4Helper.DataTypeDefinitions.DFNT_FLOAT64:
                    dataTypeSize = Marshal.SizeOf(typeof(double));
                    return typeof(double);
                case HDF4Helper.DataTypeDefinitions.DFNT_NINT8:
                    break;
                case HDF4Helper.DataTypeDefinitions.DFNT_NUINT8:
                    break;
                case HDF4Helper.DataTypeDefinitions.DFNT_NINT16:
                    break;
                case HDF4Helper.DataTypeDefinitions.DFNT_NUINT16:
                    break;
                case HDF4Helper.DataTypeDefinitions.DFNT_NINT32:
                    break;
                case HDF4Helper.DataTypeDefinitions.DFNT_NUINT32:
                    break;
                case HDF4Helper.DataTypeDefinitions.DFNT_NFLOAT32:
                    break;
                case HDF4Helper.DataTypeDefinitions.DFNT_NFLOAT64:
                    break;
                default:
                    break;
            }
            return null;
        }

        private Dictionary<string, string> GetAttributes(int objId, int attCount)
        {
            Dictionary<string, string> atts = new Dictionary<string, string>();
            for (int i = 0; i < attCount; i++)
            {
                string attName, attValue;
                GetAttribute(objId, i, out attName, out attValue);
                if (!string.IsNullOrWhiteSpace(attValue))
                    atts.Add(attName, attValue);
            }
            return atts;
        }

        private void GetAttribute(int objId, int attIndex, out string attName, out string attValue)
        {
            attName = "";
            attValue = "";
            StringBuilder attr_name = new StringBuilder(256);
            HDF4Helper.DataTypeDefinitions data_type;
            int data_count = 0;
            HDF4Helper.SDattrinfo(objId, attIndex, attr_name, out data_type, out data_count);
            attName = attr_name.ToString();
            attValue = GetAttributevalue(data_type, objId, attIndex, data_count);
        }

        private string GetAttributevalue(HDF4Helper.DataTypeDefinitions data_type, int objId, int attIndex, int data_count)
        {
            StringBuilder attr_Data = new StringBuilder(256);
            switch (data_type)
            {
                case HDF4Helper.DataTypeDefinitions.DFNT_CHAR:
                case HDF4Helper.DataTypeDefinitions.DFNT_UCHAR:
                    {
                        IntPtr attr_buff = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(byte)) * data_count);
                        HDF4Helper.SDreadattr(objId, attIndex, attr_buff);
                        byte[] dest = new byte[data_count];
                        Marshal.Copy(attr_buff, dest, 0, data_count);
                        Marshal.FreeHGlobal(attr_buff);
                        foreach (char value in dest)
                        {
                            attr_Data.Append(value);
                        }
                        break;
                    }
                case HDF4Helper.DataTypeDefinitions.DFNT_INT8:
                    {
                        IntPtr attr_buff = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(byte)) * data_count);
                        HDF4Helper.SDreadattr(objId, attIndex, attr_buff);
                        byte[] dest = new byte[data_count];
                        Marshal.Copy(attr_buff, dest, 0, data_count);
                        Marshal.FreeHGlobal(attr_buff);
                        foreach (sbyte value in dest)
                        {
                            attr_Data.Append(value);
                            attr_Data.Append(",");
                        }
                        break;
                    }
                case HDF4Helper.DataTypeDefinitions.DFNT_UINT8:
                    {
                        IntPtr attr_buff = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(byte)) * data_count);
                        HDF4Helper.SDreadattr(objId, attIndex, attr_buff);
                        byte[] dest = new byte[data_count];
                        Marshal.Copy(attr_buff, dest, 0, data_count);
                        Marshal.FreeHGlobal(attr_buff);
                        foreach (byte value in dest)
                        {
                            attr_Data.Append(value);
                            attr_Data.Append(",");
                        }
                        break;
                    }
                case HDF4Helper.DataTypeDefinitions.DFNT_INT16:
                    {
                        IntPtr attr_buff = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(short)) * data_count);
                        HDF4Helper.SDreadattr(objId, attIndex, attr_buff);
                        short[] dest = new short[data_count];
                        Marshal.Copy(attr_buff, dest, 0, data_count);
                        Marshal.FreeHGlobal(attr_buff);
                        foreach (short value in dest)
                        {
                            attr_Data.Append(value);
                            attr_Data.Append(",");
                        }
                    }
                    break;
                case HDF4Helper.DataTypeDefinitions.DFNT_UINT16:
                    {
                        IntPtr attr_buff = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(short)) * data_count);
                        HDF4Helper.SDreadattr(objId, attIndex, attr_buff);
                        short[] dest = new short[data_count];
                        Marshal.Copy(attr_buff, dest, 0, data_count);
                        Marshal.FreeHGlobal(attr_buff);
                        foreach (ushort value in dest)
                        {
                            attr_Data.Append(value);
                            attr_Data.Append(",");
                        }
                    }
                    break;
                case HDF4Helper.DataTypeDefinitions.DFNT_INT32:
                    {
                        IntPtr attr_buff = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)) * data_count);
                        HDF4Helper.SDreadattr(objId, attIndex, attr_buff);
                        int[] dest = new int[data_count];
                        Marshal.Copy(attr_buff, dest, 0, data_count);
                        Marshal.FreeHGlobal(attr_buff);
                        foreach (int value in dest)
                        {
                            attr_Data.Append(value);
                            attr_Data.Append(",");
                        }
                    }
                    break;
                case HDF4Helper.DataTypeDefinitions.DFNT_UINT32:
                    {
                        IntPtr attr_buff = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)) * data_count);
                        HDF4Helper.SDreadattr(objId, attIndex, attr_buff);
                        int[] dest = new int[data_count];
                        Marshal.Copy(attr_buff, dest, 0, data_count);
                        Marshal.FreeHGlobal(attr_buff);
                        foreach (uint value in dest)
                        {
                            attr_Data.Append(value);
                            attr_Data.Append(",");
                        }
                    }
                    break;
                case HDF4Helper.DataTypeDefinitions.DFNT_FLOAT32:
                    {
                        IntPtr attr_buff = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(float)) * data_count);
                        HDF4Helper.SDreadattr(objId, attIndex, attr_buff);
                        float[] dest = new float[data_count];
                        Marshal.Copy(attr_buff, dest, 0, data_count);
                        Marshal.FreeHGlobal(attr_buff);
                        foreach (float value in dest)
                        {
                            attr_Data.Append(value);
                            attr_Data.Append(",");
                        }
                    }
                    break;
                case HDF4Helper.DataTypeDefinitions.DFNT_FLOAT64:
                    {
                        IntPtr attr_buff = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(double)) * data_count);
                        HDF4Helper.SDreadattr(objId, attIndex, attr_buff);
                        double[] dest = new double[data_count];
                        Marshal.Copy(attr_buff, dest, 0, data_count);
                        Marshal.FreeHGlobal(attr_buff);
                        foreach (double value in dest)
                        {
                            attr_Data.Append(value);
                            attr_Data.Append(",");
                        }
                    }
                    break;
                case HDF4Helper.DataTypeDefinitions.DFNT_NINT8:
                case HDF4Helper.DataTypeDefinitions.DFNT_NUINT8:
                case HDF4Helper.DataTypeDefinitions.DFNT_NINT16:
                case HDF4Helper.DataTypeDefinitions.DFNT_NUINT16:
                case HDF4Helper.DataTypeDefinitions.DFNT_NINT32:
                case HDF4Helper.DataTypeDefinitions.DFNT_NUINT32:
                case HDF4Helper.DataTypeDefinitions.DFNT_NFLOAT32:
                case HDF4Helper.DataTypeDefinitions.DFNT_NFLOAT64:
                    break;
                default:
                    break;
            }
            string str = attr_Data.ToString();
            if (str != null)
            {
                if (str.EndsWith(","))
                    str = str.Substring(0, str.Length - 1);
                str = str.Replace('\0', ' ');
            }
            return str;
        }

        public void Dispose()
        {
            if (sd_id > 0)
                HDF4Helper.SDend(sd_id);
        }
    }

    public class Hdf5Operator : IHdfOperator
    {
        protected H5FileId _h5FileId = -1;
        protected List<string> _datasetNames = new List<string>();
        protected Dictionary<string, string> _fileAttrs = new Dictionary<string, string>();
        private bool _readFileAttrs = false;
        private string _fname = null;

        public Hdf5Operator(string filename)
        {
            _fname = filename;
            _h5FileId = H5F.open(filename, H5F.ACC_RDONLY);
            GetAllFileAttributes();
            GetAllDatasetNames();
        }

        public string[] GetDatasetNames
        {
            get { return _datasetNames.Count > 0 ? _datasetNames.ToArray() : null; }
        }

        public Dictionary<string, string> GetAttributes()
        {
            return _fileAttrs;
        }

        public Dictionary<string, string> GetDatasetAttributes(string originalDatasetName)
        {
            H5DataSetId datasetId = 0;
            H5GroupId groupId = 0;
            H5DataTypeId typeId = 0;
            H5DataSpaceId spaceId = 0;
            try
            {
                if (_h5FileId < 0)
                    return null;
                string datasetName = GetDatasetFullNames(originalDatasetName, _h5FileId);
                if (string.IsNullOrEmpty(datasetName))
                    return null;
                int groupIndex = datasetName.LastIndexOf('/');
                if (groupIndex == -1)
                    datasetId = H5D.open(_h5FileId, datasetName);
                else
                {
                    string groupName = datasetName.Substring(0, groupIndex + 1);
                    string dsName = datasetName.Substring(groupIndex + 1);
                    groupId = H5G.open(_h5FileId, groupName);
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
                H5A.operator_t cb = (int location_id, IntPtr attr_name, ref H5A.info_t ainfo, IntPtr op_data) =>
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
            }
        }

        protected string GetDatasetFullNames(string datasetName, int fileId)
        {
            string shortDatasetName = GetDatasetShortName(datasetName);
            return FindDatasetFullNames(shortDatasetName, fileId);
        }

        protected string FindDatasetFullNames(string shortDatasetName, int fileId)
        {
            for (int i = 0; i < _datasetNames.Count; i++)
            {
                string shortGdalDatasetName = GetDatasetShortName(_datasetNames[i]);
                if (shortGdalDatasetName == shortDatasetName)
                    return _datasetNames[i];
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

        private void GetAllDatasetNames()
        {
            GetGroupDatasetNames("/");
        }

        private void GetGroupDatasetNames(string groupName)
        {
            ulong pos = 0;
            List<string> groupNames = new List<string>();
            Int32 groupId = H5G.open(_h5FileId, groupName);
            if (groupId > 0)
            {
                ArrayList al = new ArrayList();
                GCHandle hnd = GCHandle.Alloc(al);
                IntPtr op_data = (IntPtr)hnd;
                H5L.iterate(groupId, H5.index_t.NAME, H5.iter_order_t.NATIVE, ref pos,
                    delegate (int _objectId, IntPtr _namePtr, ref H5L.info_t _info, IntPtr _data)
                    {
                        string objectName = Marshal.PtrToStringAnsi(_namePtr);
                        groupNames.Add(objectName);
                        return 0;
                    }, op_data);
                hnd.Free();
                H5G.close(groupId);

                foreach (var itemName in groupNames)
                {
                    //判断是不是数据集
                    string curPath = string.Empty;
                    if (groupName == "/")
                    {
                        curPath = itemName;
                    }
                    else
                    {
                        curPath = groupName + itemName;
                    }
                    H5O.info_t gInfo = new H5O.info_t();
                    H5O.get_info_by_name(_h5FileId, curPath, ref gInfo);
                    var objId = H5O.open(_h5FileId, curPath);
                    if (objId > 0)
                    {
                        if (gInfo.type == H5O.type_t.DATASET)
                        {
                            _datasetNames.Add(curPath);
                        }
                        if (gInfo.type == H5O.type_t.GROUP)
                        {
                            GetGroupDatasetNames(curPath + "/");
                        }
                        H5O.close(objId);
                    }
                }

            }





            //    H5GroupId h5GroupId = H5G.open(_h5FileId, groupName);
            //try
            //{

            //    long dscount = H5G.getNumObjects(h5GroupId);
            //    for (int i = 0; i < dscount; i++)
            //    {
            //        string objname = H5G.getObjectNameByIndex(h5GroupId, (ulong)i);
            //        ObjectInfo objInfo = H5G.getObjectInfo(h5GroupId, objname, false);
            //        switch (objInfo.objectType)
            //        {
            //            case H5GType.DATASET:
            //                if (objInfo.objectType == H5GType.DATASET)
            //                {
            //                    if (groupName == "/")
            //                        _datasetNames.Add(objname);
            //                    else
            //                        _datasetNames.Add(groupName + objname);
            //                }
            //                break;
            //            case H5GType.GROUP:
            //                if (groupName == "/")
            //                    GetGroupDatasetNames(objname + "/");
            //                else
            //                    GetGroupDatasetNames(groupName + objname + "/");
            //                break;
            //            case H5GType.LINK:
            //                break;
            //            case H5GType.TYPE:
            //                break;
            //            default:
            //                break;
            //        }
            //    }
            //}
            //finally
            //{
            //    if (h5GroupId != null)
            //        H5G.close(h5GroupId);
            //}

        }

        private void GetAllFileAttributes()
        {
            //所有Attributes的键
            ArrayList arrayList = new ArrayList();
            GCHandle handle = GCHandle.Alloc(arrayList);
            ulong n = 0;
            // the callback is defined in H5ATest.cs
            H5A.operator_t cb = (int location_id, IntPtr attr_name, ref H5A.info_t ainfo, IntPtr op_data) =>
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

        public string GetAttributeValue(string attributeName)
        {
            GetAttributes();
            if (_fileAttrs == null || _fileAttrs.Count == 0)
                return null;
            else if (_fileAttrs.ContainsKey(attributeName))
                return _fileAttrs[attributeName];
            else
                return null;
        }

        public string GetAttributeValue(string datasetName, string attributeName)
        {
            if (string.IsNullOrEmpty(datasetName) || !_datasetNames.Contains(datasetName))
                return null;
            H5DataSetId datasetId = H5D.open(_h5FileId, datasetName);
            if (datasetId == null)
                return null;
            try
            {
                return ReadAttributeValue(datasetId, attributeName);
            }
            finally
            {
                H5D.close(datasetId);
            }
        }

        private string ReadAttributeValue(int _h5FileId, string attributeName)
        {
            object v = GetAttributeValue(_h5FileId, attributeName);
            return TryArrayToString(v);
        }


        private string TryArrayToString(object v)
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


        private object GetAttributeValue(int _h5FileId, string attributeName)
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

        protected T[] ReadArray<T>(ulong size, int attId, int typeId)
        {
            T[] v = new T[size];
            if (size == 0)
                return v;
            GCHandle hnd = GCHandle.Alloc(v, GCHandleType.Pinned);
            H5A.read(attId, typeId, hnd.AddrOfPinnedObject());
            return v;
        }

        public void Dispose()
        {
            H5F.close(_h5FileId);
            _h5FileId = -1;
        }
    }
}
