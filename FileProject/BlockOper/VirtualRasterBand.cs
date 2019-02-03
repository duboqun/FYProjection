using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;

namespace PIE.Meteo.FileProject.BlockOper
{
    public class VirtualRasterBand
    {
        public VirtualRasterBand(Dataset dataset, int bandNum)
        {
            this.dataset = dataset;
            band = dataset.GetRasterBand(bandNum);
            _width = dataset.RasterXSize;
            _height = dataset.RasterYSize;
            geoTrans = dataset.GetGeoTransform();
            extent = dataset.GetExtent();
        }

        public Dataset dataset;
        public Band band;
        private int _width;
        private int _height;
        public double[] geoTrans;
        public Envelope extent;

        private double ltC = 0, ltR = 0, rdC = 0, rdR = 0;
        private int startX = 0, startY = 0, wndWidth = 0, wndHeight = 0;

        public bool Read<T>(Envelope env, int width, int height, T[] buffer)
        {
            WorldToPixel(env.MinX, env.MaxY, ref ltC, ref ltR);
            WorldToPixel(env.MaxX, env.MinY, ref rdC, ref rdR);
            ltC = Math.Round(ltC);
            ltR = Math.Round(ltR);
            rdC = Math.Round(rdC);
            rdR = Math.Round(rdR);
            startX = (int) ltC < 0 ? 0 : (int) ltC;
            startY = (int) ltR < 0 ? 0 : (int) ltR;
            wndWidth = (int) (rdC - ltC) + 1;
            wndWidth = wndWidth + startX > _width ? _width - startX : wndWidth;
            wndHeight = (int) (rdR - ltR) + 1;
            wndHeight = wndHeight + startY > _height ? _height - startY : wndHeight;
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            CPLErr err = CPLErr.CE_None;

            try
            {
                CPLErr retval = band.ReadRaster(startX, startY, wndWidth, wndHeight, handle.AddrOfPinnedObject(), width,
                    height, DataTypeHelper.Type2PixelDataType(typeof(T)), 0, 0);
            }
            finally
            {
                handle.Free();
            }

            return err != CPLErr.CE_Failure;
        }

        public bool Write<T>(Envelope env, int width, int height, T[] buffer)
        {
            WorldToPixel(env.MinX, env.MaxY, ref ltC, ref ltR);
            WorldToPixel(env.MaxX, env.MinY, ref rdC, ref rdR);
            ltC = Math.Round(ltC);
            ltR = Math.Round(ltR);
            rdC = Math.Round(rdC);
            rdR = Math.Round(rdR);
            startX = (int) ltC < 0 ? 0 : (int) ltC;
            startY = (int) ltR < 0 ? 0 : (int) ltR;
            wndWidth = (int) (rdC - ltC) + 1;
            wndWidth = wndWidth + startX > _width ? _width - startX : wndWidth;
            wndHeight = (int) (rdR - ltR) + 1;
            wndHeight = wndHeight + startY > _height ? _height - startY : wndHeight;
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            CPLErr err = CPLErr.CE_None;

            try
            {
                CPLErr retval = band.WriteRaster(startX, startY, wndWidth, wndHeight, handle.AddrOfPinnedObject(),
                    width,
                    height, DataTypeHelper.Type2PixelDataType(typeof(T)), 0, 0);
            }
            finally
            {
                handle.Free();
            }

            return err != CPLErr.CE_Failure;
        }

        public void PixelToWorld(int col, int row, ref double x, ref double y)
        {
            x = geoTrans[0] + col * geoTrans[1];
            y = geoTrans[3] + row * geoTrans[5];
        }

        public void WorldToPixel(double x, double y, ref double col, ref double row)
        {
            col = (x - geoTrans[0]) / geoTrans[1];
            row = (y - geoTrans[3]) / geoTrans[5];
        }
    }

    internal class DataTypeHelper
    {
        public static Type Enum2Type(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.GDT_Unknown:
                    return null;
                case DataType.GDT_Byte:
                    return typeof(byte);
                case DataType.GDT_UInt16:
                    return typeof(ushort);
                case DataType.GDT_Int16:
                    return typeof(short);
                case DataType.GDT_UInt32:
                    return typeof(UInt32);
                case DataType.GDT_Int32:
                    return typeof(Int32);
                case DataType.GDT_Float32:
                    return typeof(float);
                case DataType.GDT_Float64:
                    return typeof(double);
                default:
                    return null;
            }
        }

        public static string Enum2DataTypeString(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.GDT_Unknown:
                    return string.Empty;
                case DataType.GDT_Byte:
                    return "byte";
                case DataType.GDT_UInt16:
                    return "ushort";
                case DataType.GDT_Int16:
                    return "short";
                case DataType.GDT_UInt32:
                    return "uint";
                case DataType.GDT_Int32:
                    return "int";
                case DataType.GDT_Float32:
                    return "float";
                case DataType.GDT_Float64:
                    return "double";
                default:
                    return string.Empty;
            }
        }

        public static DataType Type2PixelDataType(Type type)
        {
            if (type == typeof(Byte))
            {
                return DataType.GDT_Byte;
            }
            else if (type == typeof(UInt16))
            {
                return DataType.GDT_UInt16;
            }
            else if (type == typeof(Int16))
            {
                return DataType.GDT_Int16;
            }
            else if (type == typeof(Int32))
            {
                return DataType.GDT_Int32;
            }
            else if (type == typeof(UInt32))
            {
                return DataType.GDT_UInt32;
            }
            else if (type == typeof(Single))
            {
                return DataType.GDT_Float32;
            }
            else if (type == typeof(Double))
            {
                return DataType.GDT_Float64;
            }
            else
            {
                return DataType.GDT_Unknown;
            }
        }

        internal static int SizeOf(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.GDT_Byte:
                    return 1;
                case DataType.GDT_UInt16:
                    return 2;
                case DataType.GDT_Int16:
                    return 2;
                case DataType.GDT_UInt32:
                    return 4;
                case DataType.GDT_Int32:
                    return 4;
                case DataType.GDT_Float32:
                    return 4;
                case DataType.GDT_Float64:
                    return 8;
                default:
                    return 4;
            }
        }
    }
}