using OSGeo.OSR;

namespace PIE.Meteo.RasterProject
{
    public class SpatialReferenceFactory
    {
        public static SpatialReference CreateSpatialReference(int p0)
        {
            SpatialReference srs = new SpatialReference("");
             srs.ImportFromEPSG(p0);
             return srs;
        }
    }
}