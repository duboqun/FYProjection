using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using OSGeo.OSR;
    
namespace PIE.Meteo.RasterProject
{
    public interface IRasterProjector
    {
        void Transform(SpatialReference srcSpatialRef, double[] srcXs, double[] srcYs, SpatialReference dstSpatialRef);

        void ComputeDstEnvelope(SpatialReference srcSpatialRef, double[] srcXs, double[] srcYs, Size srcSize,
                                SpatialReference dstSpatialRef,
                                out PrjEnvelope dstEnvelope, Action<int, string> progressCallback);

        void ComputeDstEnvelope(SpatialReference srcSpatialRef, double[] srcXs, double[] srcYs, Size srcSize,
                                SpatialReference dstSpatialRef, PrjEnvelope maskEnvelope,
                                out PrjEnvelope dstEnvelope, Action<int, string> progressCallback);

        bool HasVaildEnvelope(double[] xs, double[] ys, PrjEnvelope validEnv, SpatialReference oSpatialRef, SpatialReference tSpatialRef);

        bool VaildEnvelope(double[] xs, double[] ys, PrjEnvelope validEnv, SpatialReference oSpatialRef, SpatialReference tSpatialRef,
            out double vaildRate, out PrjEnvelope env);

        //void ComputeIndexMapTable(SpatialReference srcSpatialRef, double[] srcXs, double[] srcYs, Size srcSize,
        //                          SpatialReference dstSpatialRef, Size dstSize, PrjEnvelope dstEnvelope,
        //                          out UInt16[] rowMapTable, out UInt16[] colMapTable, Action<int, string> progressCallback);

        void ComputeIndexMapTable(double[] srcToDstSpatialXs, double[] srcToDstSpatialYs, Size srcSize,
                                  Size dstSize, PrjEnvelope dstEnvelope, PrjEnvelope maxDstEnvelope,
                                  out UInt16[] rowMapTable, out UInt16[] colMapTable, Action<int, string> progressCallback);

        void ComputeIndexMapTable(double[] srcToDstSpatialXs, double[] srcToDstSpatialYs, Size srcSize, Size srcDataSize,
                                    Size dstSize, PrjEnvelope dstEnvelope,
                                    out UInt16[] rowMapTable, out UInt16[] colMapTable, Action<int, string> progressCallback);
        void ComputeIndexMapTable(double[] srcToDstSpatialXs, double[] srcToDstSpatialYs, Size srcSize, Size srcDataSize,
                                    Size dstSize, PrjEnvelope dstEnvelope,
                                    out UInt16[] rowMapTable, out UInt16[] colMapTable, Action<int, string> progressCallback, int scanLineWidth);

        void Project<T>(T[] srcRaster, Size srcSize, UInt16[] rowMapTable, UInt16[] colMapTable, Size mapSize, T[] dstRaster, T destFillValue, Action<int, string> progressCallback);
    }
}
