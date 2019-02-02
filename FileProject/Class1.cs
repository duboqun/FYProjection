using System;
using OSGeo.GDAL;
using PIE.Meteo.RasterProject;

namespace FileProject
{
    public class Class1
    {
        public static void Test()
        {
            Gdal.AllRegister();
            Dataset rDs = Gdal.Open("/media/duboqun/DU/FY3D_MERSI_0FEH_GLL_L1_20190123_0500_1000M.overview.png", Access.GA_ReadOnly);
            Console.WriteLine(rDs);
            string proj4Str = SpatialReferenceFactory.CreateSpatialReference(4326).ExportToProj4();
            Proj4Projection prj = new Proj4Projection(proj4Str);
        }
    }
}
