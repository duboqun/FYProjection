using PIE.Meteo.Core;
using PIE.Meteo.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PIE.Meteo.FileProject
{
    public class PrjBandTable
    {
        public static PrjBand[] GetDefaultBandTable(string satelite, string sensor, string resolution)
        {
            if(sensor=="MERSI"&& satelite=="FY3D")
            {
                if(resolution == "0250M")
                {
                    return BandDefCollection.MERSI2_0250_OrbitDefCollecges();
                }
                if(resolution == "1000M")
                {
                    return BandDefCollection.MERSI2_1000_OrbitDefCollecges();
                }
            }
            if (sensor == "VIRR")
            {
                return BandDefCollection.GetOrbitBandDefCollection("FY3A", "VIRR", 0.01f);
            }
            else if (sensor == "MERSI"&&resolution =="0250M")
            {
                return BandDefCollection.GetOrbitBandDefCollection("FY3A", "MERSI", 0.0025f);
            }
            else if (sensor == "MERSI"&&resolution =="1000M")
            {
                return BandDefCollection.GetOrbitBandDefCollection("FY3A", "MERSI", 0.01f);
            }
            else if (satelite == "FY1D" && sensor == "AVHRR")
            {
                return BandDefCollection.GetOrbitBandDefCollection("FY1D", "AVHRR", 0.04f);
            }
            else if (satelite =="NOAA"||sensor == "AVHRR")
            {
                return BandDefCollection.GetOrbitBandDefCollection("NOAA", "AVHRR", 0.01f);
            }
            else if ((satelite == "EOS" || sensor == "MODIS") && resolution == "0250M")
            {
                return BandDefCollection.GetOrbitBandDefCollection("EOS", "MODIS", 0.0025f);
            }
            else if ((satelite == "EOS" || sensor == "MODIS") && resolution == "0500M")
            {
                return BandDefCollection.GetOrbitBandDefCollection("EOS", "MODIS", 0.005f);
            }
            else if (satelite == "EOS" || sensor == "MODIS")
            {
                return BandDefCollection.GetOrbitBandDefCollection("EOS", "MODIS", 0.01f);
            }
            else if (satelite == "FY2" || sensor == "VISSR")
            {
                return BandDefCollection.GetOrbitBandDefCollection("FY2", "VISSR", 0.05f);
            }
            else if(satelite=="FY4A"&&sensor=="AGRI")
            {
                if(resolution=="0500M") return BandDefCollection.GetOrbitBandDefCollection("FY4A", "AGRI", 500f);
                else if(resolution=="1000M") return BandDefCollection.GetOrbitBandDefCollection("FY4A", "AGRI", 1000f);
                else if(resolution=="2000M") return BandDefCollection.GetOrbitBandDefCollection("FY4A", "AGRI", 2000f);
                else if(resolution=="4000M") return BandDefCollection.GetOrbitBandDefCollection("FY4A", "AGRI", 4000f);
            }
            else if(satelite=="FY2G")
            {
                return BandDefCollection.GetOrbitBandDefCollection("FY2G", "VISSR", 5000f);
            }
            return null;
        }

        public static PrjBand[] GetPrjBands(AbstractWarpDataset rasterDataProvider)
        {
            
            PrjBand[] prjBands = null;
            string fileType = new FileChecker().GetFileType(rasterDataProvider);
            switch (fileType)
            {
                case "VIRR_L1":
                    prjBands = PrjBandTable.GetDefaultBandTable("FY3A", "VIRR", "1000M");
                    break;
                case "FY3C_VIRR_L1":
                    prjBands = PrjBandTable.GetDefaultBandTable("FY3C", "VIRR", "1000M");
                    break;
                case "MERSI_1KM_L1":
                case "MERSI_QKM_L1":
                    prjBands = PrjBandTable.GetDefaultBandTable("FY3A", "MERSI", "1000M");
                    break;
                case "FY3C_MERSI_1KM_L1":
                    prjBands = PrjBandTable.GetDefaultBandTable("FY3C", "MERSI", "1000M");
                    break;
                case "FY3C_MERSI_QKM_L1":
                    prjBands = PrjBandTable.GetDefaultBandTable("FY3C", "MERSI", "0250M");
                    break;
                case "FY3D_MERSI_1KM_L1":
                    prjBands = PrjBandTable.GetDefaultBandTable("FY3D", "MERSI", "1000M");
                    break;
                case "FY3D_MERSI_QKM_L1":
                    prjBands = PrjBandTable.GetDefaultBandTable("FY3D", "MERSI", "0250M");
                    break;
                case "MODIS_1KM_L1":
                    prjBands = PrjBandTable.GetDefaultBandTable("EOS", "MODIS", "1000M");
                    break;
                case "NOAA_1BD":
                    prjBands = PrjBandTable.GetDefaultBandTable("NOAA", "AVHRR", "1000M");
                    break;
                case "FY2NOM":
                    prjBands = PrjBandTable.GetDefaultBandTable("FY2", "VISSR", "5000M");
                    break;
                case "FY1X_1A5":
                    prjBands = PrjBandTable.GetDefaultBandTable("FY1D", "AVHRR", "4000M");
                    break;
                case "FY4A_AGRI_0500":
                    prjBands = PrjBandTable.GetDefaultBandTable("FY4A", "AGRI", "0500M");
                    break;
                case "FY4A_AGRI_1000":
                    prjBands = PrjBandTable.GetDefaultBandTable("FY4A", "AGRI", "1000M");
                    break;
                case "FY4A_AGRI_2000":
                    prjBands = PrjBandTable.GetDefaultBandTable("FY4A", "AGRI", "2000M");
                    break;
                case "FY4A_AGRI_4000":
                    prjBands = PrjBandTable.GetDefaultBandTable("FY4A", "AGRI", "4000M");
                    break;
                case "PROJECTED":
                    break;
                default:
                    break;
            }
            return prjBands;
        }
    }
}
