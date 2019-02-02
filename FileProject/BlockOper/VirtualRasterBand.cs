using PIE.DataSource;
using PIE.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIE.Meteo.FileProject.BlockOper
{
    public class VirtualRasterBand
    {
        public VirtualRasterBand(IRasterDataset dataset, int bandNum)
        {
            this.dataset = dataset;
            band = dataset.GetRasterBand(bandNum);
            _width = dataset.GetRasterXSize();
            _height = dataset.GetRasterYSize();
            geoTrans = dataset.GetGeoTransform();
            extent = dataset.GetExtent();
        }

        public IRasterDataset dataset;
        public IRasterBand band;
        private int _width;
        private int _height;
        public double[] geoTrans;
        public IEnvelope extent;

        private double ltC = 0, ltR = 0, rdC = 0, rdR = 0;
        private int startX = 0, startY = 0, wndWidth = 0, wndHeight = 0;

        public bool Read<T>(IEnvelope env, int width, int height, T[] buffer)
        {
            WorldToPixel(env.XMin, env.YMax, ref ltC, ref ltR);
            WorldToPixel(env.XMax, env.YMin, ref rdC, ref rdR);
            ltC = Math.Round(ltC);
            ltR = Math.Round(ltR);
            rdC = Math.Round(rdC);
            rdR = Math.Round(rdR);
            startX = (int)ltC < 0 ? 0 : (int)ltC;
            startY = (int)ltR < 0 ? 0 : (int)ltR;
            wndWidth = (int)(rdC - ltC) + 1;
            wndWidth = wndWidth + startX > _width ? _width - startX : wndWidth;
            wndHeight = (int)(rdR - ltR) + 1;
            wndHeight = wndHeight + startY > _height ? _height - startY : wndHeight;
            return band.Read(startX, startY, wndWidth, wndHeight, buffer, width, height, DataTypeHelper.Type2PixelDataType(typeof(T)));
        }

        public bool Write<T>(IEnvelope env, int width, int height, T[] buffer)
        {
            WorldToPixel(env.XMin, env.YMax, ref ltC, ref ltR);
            WorldToPixel(env.XMax, env.YMin, ref rdC, ref rdR);
            ltC = Math.Round(ltC);
            ltR = Math.Round(ltR);
            rdC = Math.Round(rdC);
            rdR = Math.Round(rdR);
            startX = (int)ltC < 0 ? 0 : (int)ltC;
            startY = (int)ltR < 0 ? 0 : (int)ltR;
            wndWidth = (int)(rdC - ltC) + 1;
            wndWidth = wndWidth + startX > _width ? _width - startX : wndWidth;
            wndHeight = (int)(rdR - ltR) + 1;
            wndHeight = wndHeight + startY > _height ? _height - startY : wndHeight;
            return band.Write(startX, startY, wndWidth, wndHeight, buffer, width, height, DataTypeHelper.Type2PixelDataType(typeof(T)));
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
        public static Type Enum2Type(PixelDataType dataType)
        {
            switch (dataType)
            {
                case PixelDataType.Unknown:
                    return null;
                case PixelDataType.Byte:
                    return typeof(byte);
                case PixelDataType.UInt16:
                    return typeof(ushort);
                case PixelDataType.Int16:
                    return typeof(short);
                case PixelDataType.UInt32:
                    return typeof(UInt32);
                case PixelDataType.Int32:
                    return typeof(Int32);
                case PixelDataType.Float32:
                    return typeof(float);
                case PixelDataType.Float64:
                    return typeof(double);
                default:
                    return null;
            }
        }
        public static string Enum2DataTypeString(PixelDataType dataType)
        {
            switch (dataType)
            {
                case PixelDataType.Unknown:
                    return string.Empty;
                case PixelDataType.Byte:
                    return "byte";
                case PixelDataType.UInt16:
                    return "ushort";
                case PixelDataType.Int16:
                    return "short";
                case PixelDataType.UInt32:
                    return "uint";
                case PixelDataType.Int32:
                    return "int";
                case PixelDataType.Float32:
                    return "float";
                case PixelDataType.Float64:
                    return "double";
                default:
                    return string.Empty;
            }
        }

        public static PixelDataType Type2PixelDataType(Type type)
        {
            if (type == typeof(Byte))
            {
                return PixelDataType.Byte;
            }
            else if (type == typeof(UInt16))
            {
                return PixelDataType.UInt16;
            }
            else if (type == typeof(Int16))
            {
                return PixelDataType.Int16;
            }
            else if (type == typeof(Int32))
            {
                return PixelDataType.Int32;
            }
            else if (type == typeof(UInt32))
            {
                return PixelDataType.UInt32;
            }
            else if (type == typeof(Single))
            {
                return PixelDataType.Float32;
            }
            else if (type == typeof(Double))
            {
                return PixelDataType.Float64;
            }
            else
            {
                return PixelDataType.Unknown;
            }
        }

        internal static int SizeOf(PixelDataType dataType)
        {
            switch (dataType)
            {
                case PixelDataType.Byte:
                    return 1;
                case PixelDataType.UInt16:
                    return 2;
                case PixelDataType.Int16:
                    return 2;
                case PixelDataType.UInt32:
                    return 4;
                case PixelDataType.Int32:
                    return 4;
                case PixelDataType.Float32:
                    return 4;
                case PixelDataType.Float64:
                    return 8;
                default:
                    return 4;
            }
        }
    }
}
