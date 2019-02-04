using System;
using System.IO;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace ProjectTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Gdal.AllRegister();
            
            Console.WriteLine("Hello World!");
            
//            FileProject.Class1.Test();
           Dataset rDs = Gdal.OpenShared("/media/duboqun/DU/FY4A-_AGRI--_N_DISK_1047E_L1-_FDI-_MULT_NOM_20181116090000_20181116091459_4000M_V0001.HDF", Access.GA_ReadOnly);
           foreach (var VARIABLE in rDs.GetMetadata("SUBDATASETS"))
           {
               Console.WriteLine(VARIABLE);
           }

           Console.WriteLine("-------");
           foreach (var VARIABLE in rDs.GetSubDatasets())
           {
               Console.WriteLine($"k:{VARIABLE.Key} v:{VARIABLE.Value}");
           }
           
           SpatialReference srs = new SpatialReference("");
           srs.ImportFromEPSG(4326);
        }
    }
}