using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PIE.Meteo.FileProject
{
    public class BandDefCollection
    {
        private static string FileProject_BandDefCollection_Channel = "";
        private static string FileProject_BandDefCollection_VisibleLight = "";
        private static string FileProject_BandDefCollection_FarIR = "";
        private static string FileProject_BandDefCollection_Cloudland_boundary="";
        private static string FileProject_BandDefCollection_Cloudlandcharacteristics="";
        private static string FileProejct_BandDefCollection_Oceanwatercolorbiochemistry="";
        private static string FileProject_BandDefCollection_watervapour="";
        private static string FileProject_BandDefCollection_SurfaceCloudTopTemperature="";
        private static string FileProject_BandDefCollection_CloudTopHeight="";
        private static string FileProject_BandDefCollection_CirrusCloud="";
        private static string FileProject_BandDefCollection_watervap="";
        private static string FileProject_BandDefCollection_Ozone="";
        private static string FileProject_BandDefCollection_NearIR="";
        private static string FileProject_BandDefCollection_MidIR="";
        public static PrjBand[] GetOrbitBandDefCollection(string satellite, string sensorTypes, float resolution)
        {
            switch (sensorTypes)
            {
                case "VIRR":
                    if (satellite == "FY1D")
                    {
                        if ((resolution - 0.01f) >= 0.0005)
                        {
                            break;
                        }
                        return FY1D_1000_OrbitDefCollecges();
                    }
                    else
                    {
                        if ((resolution - 0.01f) >= 0.0005)
                        {
                            break;
                        }
                        return VIRR_1000_OrbitDefCollecges();
                    }
                case "MERSI":
                    if ((resolution - 0.005f) >= 0.0005)
                    {
                        if ((resolution - 0.01f) >= 5E-05)
                        {
                            break;
                        }
                        return MERSI_1000_OrbitDefCollecges();
                    }
                    return MERSI_0250_OrbitDefCollecges();
                case "AVHRR":
                    if (satellite == "FY1D")
                    {
                        return FY1D_4000_OrbitDefCollecges();
                    }
                    else
                    {
                        if ((resolution - 0.01f) >= 5E-05)
                        {
                            break;
                        }
                        return AVHRR_1000_OrbitDefCollecges();
                    }
                    break;
                case "MODIS":
                    if ((resolution - 0.0025f) >= 5E-05)
                    {
                        if ((resolution - 0.005f) < 0.0005)
                        {
                            return MODIS_500_OrbitDefCollecges();
                        }
                        if ((resolution - 0.01f) < 5E-05)
                        {
                            return MODIS_1000_OrbitDefCollecges();
                        }
                        break;
                    }
                    return MODIS_250_OrbitDefCollecges();
                case "VISSR":
                    return FY2_5000_OrbitDefCollecges();
                case "AGRI":
                    if (resolution == 500f)
                        return FY4_0500_OrbitDefCollecges();
                    else if (resolution == 1000f)
                        return FY4_1000_OrbitDefCollecges();
                    else if (resolution == 2000f)
                        return FY4_2000_OrbitDefCollecges();
                    else if (resolution == 4000f)
                        return FY4_4000_OrbitDefCollecges();
                    break;
                case "VIIRS"://NPP VIIRS
                    return NPP_OrbitDefCollecges();
                default:
                    return null;
            }
            return null;
        }

        internal static PrjBand[] AVHRR_1000_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            return new PrjBand[]{
                new PrjBand("AVHRR", 0.01f, "1", 0, channel, "0.58-0.68 band_1", "band"),
                new PrjBand("AVHRR", 0.01f, "2", 1, channel, "0.70-1.10 band_2", "band"),
                new PrjBand("AVHRR", 0.01f, "3", 2, channel, "3.55-3.95 band_3", "band"),
                new PrjBand("AVHRR", 0.01f, "4", 3, channel, "10.3-11.3 band_4", "band"),
                new PrjBand("AVHRR", 0.01f, "5", 4, channel, "11.5-12.5 band_5", "band")
            };
        }

        internal static PrjBand[] MERSI_0250_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            return new PrjBand[]{
            new PrjBand("MERSI", 0.0025f, "1", 0, channel, "0.470 band_1", "EV_250_RefSB_b1"),
            new PrjBand("MERSI", 0.0025f, "2", 0, channel, "0.550 band_2", "EV_250_RefSB_b2"),
            new PrjBand("MERSI", 0.0025f, "3", 0, channel, "0.650 band_3", "EV_250_RefSB_b3"),
            new PrjBand("MERSI", 0.0025f, "4", 0, channel, "0.865 band_4", "EV_250_RefSB_b4"),
            new PrjBand("MERSI", 0.0025f, "5", 0, channel, "11.25 band_5", "EV_250_Emissive")
            };
        }

        internal static PrjBand[] MERSI2_0250_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            string visiblelight = FileProject_BandDefCollection_VisibleLight;
            string farIR = FileProject_BandDefCollection_FarIR;
            return new PrjBand[]{
            new PrjBand("MERSI", 0.0025f, "1", 0, channel, visiblelight+" 0250M 0.47", "EV_250_RefSB_b1"),
            new PrjBand("MERSI", 0.0025f, "2", 0, channel, visiblelight+" 0250M 0.55", "EV_250_RefSB_b2"),
            new PrjBand("MERSI", 0.0025f, "3", 0, channel, visiblelight+" 0250M 0.65", "EV_250_RefSB_b3"),
            new PrjBand("MERSI", 0.0025f, "4", 0, channel, visiblelight+" 0250M 0.865", "EV_250_RefSB_b4"),
            new PrjBand("MERSI", 0.0025f, "5", 0, channel, farIR+" 0250M 10.8", "EV_250_Emissive_b24"),
            new PrjBand("MERSI", 0.0025f, "6", 0, channel, farIR+" 0250M 12.0", "EV_250_Emissive_b25")
            };
        }

        internal static PrjBand[] MERSI_1000_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            return new PrjBand[]{
            new PrjBand("MERSI", 0.01f, "1", 0, channel, "0.470 band_1", "EV_250_Aggr.1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "2", 1, channel, "0.550 band_2", "EV_250_Aggr.1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "3", 2, channel, "0.650 band_3", "EV_250_Aggr.1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "4", 3, channel, "0.865 band_4", "EV_250_Aggr.1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "5", 0, channel, "11.25 band_5", "EV_250_Aggr.1KM_Emissive"),
            new PrjBand("MERSI", 0.01f, "6", 0, channel, "1.640 band_6", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "7", 1, channel, "2.130 band_7", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "8", 2, channel, "0.412 band_8", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "9", 3, channel, "0.443 band_9", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "10", 4, channel, "0.490 band_10", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "11", 5, channel, "0.520 band_11", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "12", 6, channel, "0.565 band_12", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "13", 7, channel, "0.650 band_13", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "14", 8, channel, "0.685 band_14", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "15", 9, channel, "0.765 band_15", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "16", 10, channel, "0.865 band_16", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "17", 11, channel, "0.905 band_17", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "18", 12, channel, "0.940 band_18", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "19", 13, channel, "0.980 band_19", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "20", 14, channel, "1.030 band_20", "EV_1KM_RefSB")
            };
        }


        internal static PrjBand[] MERSI2_1000_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            string visiblelight = FileProject_BandDefCollection_VisibleLight;
            string farIR = FileProject_BandDefCollection_FarIR;
            string nearIR = FileProject_BandDefCollection_NearIR;
            string midIR = FileProject_BandDefCollection_MidIR;
            return new PrjBand[]{
            new PrjBand("MERSI", 0.01f, "1", 0, channel, visiblelight+" 0250M 0.47μm", "EV_250_Aggr.1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "2", 1, channel, visiblelight+" 0250M 0.55μm", "EV_250_Aggr.1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "3", 2, channel, visiblelight+" 0250M 0.65μm", "EV_250_Aggr.1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "4", 3, channel, visiblelight+" 0250M 0.865μm", "EV_250_Aggr.1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "24", 0, channel, farIR+" 0250M 10.8μm", "EV_250_Aggr.1KM_Emissive"),
            new PrjBand("MERSI", 0.01f, "25", 1, channel, farIR+" 0250M 12.0μm", "EV_250_Aggr.1KM_Emissive"),
            new PrjBand("MERSI", 0.01f, "5", 0, channel, nearIR+" 1000M 1.38μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "6", 1, channel, nearIR+" 1000M 1.64μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "7", 2, channel, nearIR+" 1000M 2.13μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "8", 3, channel, visiblelight+" 1000M 0.412μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "9", 4, channel, visiblelight+" 1000M 0.443μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "10", 5, channel, visiblelight+" 1000M 0.49μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "11", 6, channel, visiblelight+" 1000M 0.555μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "12", 7, channel, visiblelight+" 1000M 0.67μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "13", 8, channel, visiblelight+" 1000M 0.709μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "14", 9, channel, visiblelight+" 1000M 0.746μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "15", 10, channel, visiblelight+" 1000M 0.865μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "16", 11, channel, visiblelight+" 1000M 0.905μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "17", 12, channel, visiblelight+" 1000M 0.936μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "18", 13, channel, visiblelight+" 1000M 0.940μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "19", 14, channel, nearIR+" 1000M 1.030μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "20", 0, channel, midIR+" 1000M 3.80μm", "EV_1KM_Emissive"),
            new PrjBand("MERSI", 0.01f, "21", 1, channel, midIR+" 1000M 4.05μm", "EV_1KM_Emissive"),
            new PrjBand("MERSI", 0.01f, "22", 2, channel, midIR+" 1000M 7.20μm", "EV_1KM_Emissive"),
            new PrjBand("MERSI", 0.01f, "23", 3, channel, farIR+" 1000M 8.55μm", "EV_1KM_Emissive")

            };
        }


        internal static PrjBand[] MODIS_1000_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            string boundary = FileProject_BandDefCollection_Cloudland_boundary;
            string characteristics = FileProject_BandDefCollection_Cloudlandcharacteristics;
            string sea = FileProejct_BandDefCollection_Oceanwatercolorbiochemistry;
            string water = FileProject_BandDefCollection_watervapour;
            string surface = FileProject_BandDefCollection_SurfaceCloudTopTemperature;
            string cloudTop = FileProject_BandDefCollection_CloudTopHeight;
            string CirrusCloud = FileProject_BandDefCollection_CirrusCloud;
            string wat = FileProject_BandDefCollection_watervap;
            string ozo = FileProject_BandDefCollection_Ozone;
            return new PrjBand[]{
            new PrjBand("MODIS", 0.01f, "1", 0, channel, "620 - 670 "+boundary, "EV_250_Aggr1km_RefSB"),
            new PrjBand("MODIS", 0.01f, "2", 1, channel, "841 - 876 "+boundary, "EV_250_Aggr1km_RefSB"),
            new PrjBand("MODIS", 0.01f, "3", 0, channel, "459 - 479 "+characteristics, "EV_500_Aggr1km_RefSB"),
            new PrjBand("MODIS", 0.01f, "4", 1, channel, "545 - 565 "+characteristics, "EV_500_Aggr1km_RefSB"),
            new PrjBand("MODIS", 0.01f, "5", 2, channel, "1230-1250 "+characteristics, "EV_500_Aggr1km_RefSB"),
            new PrjBand("MODIS", 0.01f, "6", 3, channel, "1628-1652 "+characteristics, "EV_500_Aggr1km_RefSB"),
            new PrjBand("MODIS", 0.01f, "7", 4, channel, "2105-2155 "+characteristics, "EV_500_Aggr1km_RefSB"),
            new PrjBand("MODIS", 0.01f, "8", 0, channel, "405 - 420 "+sea, "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "9", 1, channel, "438 - 448 "+sea, "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "10", 2, channel, "483 - 493 "+sea, "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "11", 3, channel, "526 - 536 "+sea, "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "12", 4, channel, "546 - 556 "+sea, "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "13l", 5, channel, "662 - 672 "+sea, "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "13h", 6, channel, "662 - 672 "+sea, "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "14l", 7, channel, "673 - 753 "+sea, "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "14h", 8, channel, "673 - 753 "+sea, "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "15", 9, channel, "743 - 753 "+sea, "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "16", 10, channel, "862 - 877 "+sea, "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "17", 11, channel, "890 - 920 "+water, "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "18", 12, channel, "931 - 941 "+water, "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "19", 13, channel, "915 - 965 "+water, "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "20", 0, channel, "3.66-3.84 "+surface, "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "21", 1, channel, "3.93-3.99 "+surface, "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "22", 2, channel, "3.93-3.99 "+surface, "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "23", 3, channel, "4.02-4.08 "+surface, "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "24", 4, channel, "4.43-4.49 "+water, "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "25", 5, channel, "4.48-4.55 "+water, "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "26", 14, channel, "1.36-1.39 "+CirrusCloud, "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "27", 6, channel, "6.53-6.89 "+wat, "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "28", 7, channel, "7.17-7.47 "+wat, "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "29", 8, channel, "8.40-8.70 "+wat, "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "30", 9, channel, "9.58-9.88 "+ozo, "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "31", 10, channel, "10.78-11.28 "+surface, "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "32", 11, channel, "11.77-12.27 "+surface, "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "33", 12, channel, "13.18-13.48 "+cloudTop, "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "34", 13, channel, "13.48-13.78 "+cloudTop, "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "35", 14, channel, "13.78-14.08 "+cloudTop, "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "36", 15, channel, "14.08-14.38 "+cloudTop, "EV_1KM_Emissive")
            };
        }

        internal static PrjBand[] MODIS_250_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            string boundary = FileProject_BandDefCollection_Cloudland_boundary;
            return new PrjBand[]{
            new PrjBand("MODIS", 0.0025f, "1", 0, channel, "620-670 "+boundary, "EV_250_RefSB"),
            new PrjBand("MODIS", 0.0025f, "2", 1, channel, "841-876 "+boundary, "EV_250_RefSB"),
            };
        }

        internal static PrjBand[] MODIS_500_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            string boundary = FileProject_BandDefCollection_Cloudland_boundary;
            string characteristics = FileProject_BandDefCollection_Cloudlandcharacteristics;
            return new PrjBand[]{
            new PrjBand("MODIS", 0.005f, "1", 0, channel, "620-670 "+boundary, "EV_250_Aggr500_RefSB"),
            new PrjBand("MODIS", 0.005f, "2", 1, channel, "841-876 "+boundary, "EV_250_Aggr500_RefSB"),
            new PrjBand("MODIS", 0.005f, "3", 0, channel, "459-479 "+characteristics, "EV_500_RefSB"),
            new PrjBand("MODIS", 0.005f, "4", 1, channel, "545-565 "+characteristics, "EV_500_RefSB"),
            new PrjBand("MODIS", 0.005f, "5", 2, channel, "1230-1250 "+characteristics, "EV_500_RefSB"),
            new PrjBand("MODIS", 0.005f, "6", 3, channel, "1628-1652 "+characteristics, "EV_500_RefSB"),
            new PrjBand("MODIS", 0.005f, "7", 4, channel, "2105-2155 "+characteristics, "EV_500_RefSB")
            };
        }

        internal static PrjBand[] VIRR_1000_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            return new PrjBand[]{
            new PrjBand("VIRR", 0.01f, "1", 0, channel, "0.58-0.68 band_1", "EV_RefSB"),
            new PrjBand("VIRR", 0.01f, "2", 1, channel, "0.84-0.89 band_2", "EV_RefSB"),
            new PrjBand("VIRR", 0.01f, "3", 0, channel, "3.55-3.93 band_3", "EV_Emissive"),
            new PrjBand("VIRR", 0.01f, "4", 1, channel, "10.3-11.3 band_4", "EV_Emissive"),
            new PrjBand("VIRR", 0.01f, "5", 2, channel, "11.5-12.5 band_5", "EV_Emissive"),
            new PrjBand("VIRR", 0.01f, "6", 2, channel, "1.55-1.64 band_6", "EV_RefSB"),
            new PrjBand("VIRR", 0.01f, "7", 3, channel, "0.43-0.48 band_7", "EV_RefSB"),
            new PrjBand("VIRR", 0.01f, "8", 4, channel, "0.48-0.53 band_8", "EV_RefSB"),
            new PrjBand("VIRR", 0.01f, "9", 5, channel, "0.53-0.58 band_9", "EV_RefSB"),
            new PrjBand("VIRR", 0.01f, "10", 6, channel, "1.325-1.395 band_10", "EV_RefSB")
            };
        }

        internal static PrjBand[] FY1D_4000_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            return new PrjBand[]{
            new PrjBand("VISSR", 0.01f, "1", 0, channel, "0.58-0.68 band_1", ""),
            new PrjBand("VISSR", 0.01f, "2", 1, channel, "0.84-0.89 band_2", ""),
            new PrjBand("VISSR", 0.01f, "3", 2, channel, "3.55-3.95 band_3", ""),
            new PrjBand("VISSR", 0.01f, "4", 3, channel, "0.3-11.3 band_4", "")
            //,new PrjBand("VISSR", 0.01f, "5", 4, channel, "11.5-12.5 band_5", ""),
            //new PrjBand("VISSR", 0.01f, "6", 5, channel, "1.58-1.64 band_6", ""),
            //new PrjBand("VISSR", 0.01f, "7", 6, channel, "0.43-0.48 band_7", ""),
            //new PrjBand("VISSR", 0.01f, "8", 7, channel, "0.48-0.53 band_8", ""),
            //new PrjBand("VISSR", 0.01f, "9", 8, channel, "0.53-0.58 band_9", ""),
            //new PrjBand("VISSR", 0.01f, "10", 9, channel, "0.9-0.985 band_10", "")
            };
        }

        internal static PrjBand[] FY1D_1000_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            return new PrjBand[]{
            new PrjBand("VISSR", 0.01f, "1", 0, channel, "0.58-0.68 band_1", ""),
            new PrjBand("VISSR", 0.01f, "2", 1, channel, "0.84-0.89 band_2", ""),
            new PrjBand("VISSR", 0.01f, "3", 2, channel, "3.55-3.95 band_3", ""),
            new PrjBand("VISSR", 0.01f, "4", 3, channel, "0.3-11.3 band_4", "")
            ,new PrjBand("VISSR", 0.01f, "5", 4, channel, "11.5-12.5 band_5", ""),
            new PrjBand("VISSR", 0.01f, "6", 5, channel, "1.58-1.64 band_6", ""),
            new PrjBand("VISSR", 0.01f, "7", 6, channel, "0.43-0.48 band_7", ""),
            new PrjBand("VISSR", 0.01f, "8", 7, channel, "0.48-0.53 band_8", ""),
            new PrjBand("VISSR", 0.01f, "9", 8, channel, "0.53-0.58 band_9", ""),
            new PrjBand("VISSR", 0.01f, "10", 9, channel, "0.9-0.985 band_10", "")
            };
        }

        internal static PrjBand[] FY2_5000_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            return new PrjBand[]{
            new PrjBand("VIRR", 5000f, "1", 0, channel, "NOMChannelIR1 band_1", "NOMChannelIR1"),
            new PrjBand("VIRR", 5000f, "2", 0, channel, "NOMChannelIR2 band_2", "NOMChannelIR2"),
            new PrjBand("VIRR", 5000f, "3", 0, channel, "NOMChannelIR3 band_3", "NOMChannelIR3"),
            new PrjBand("VIRR", 5000f, "4", 0, channel, "NOMChannelIR4 band_4", "NOMChannelIR4"),
            new PrjBand("VIRR", 5000f, "5", 0, channel, "NOMChannelVIS band_5", "NOMChannelVIS"),
            //new PrjBand("VIRR", 5000f, "6", 0, "NOMCloudClassification", "NOMCloudClassification", "NOMCloudClassification")
            };
        }
        //ToDo:修改内部属性

        internal static PrjBand[] FY4_4000_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            return new PrjBand[]{
            new PrjBand("AGRI", 2000f, "1", 1, channel, "0.47um channel", "NOMChannel01"),
            new PrjBand("AGRI", 2000f, "2", 2, channel, "0.65um channel", "NOMChannel02"),
            new PrjBand("AGRI", 2000f, "3", 3, channel, "0.83um channel", "NOMChannel03"),
            new PrjBand("AGRI", 2000f, "4", 4, channel, "1.37um channel", "NOMChannel04"),
            new PrjBand("AGRI", 2000f, "5", 5, channel, "1.61um channel", "NOMChannel05"),
            new PrjBand("AGRI", 2000f, "6", 6, channel, "2.22um channel", "NOMChannel06"),
            new PrjBand("AGRI", 2000f, "7", 7, channel, "3.72um channel", "NOMChannel07"),
            new PrjBand("AGRI", 2000f, "8", 8, channel, "3.72um channel", "NOMChannel08"),
            new PrjBand("AGRI", 2000f, "9", 9, channel, "6.25um channel", "NOMChannel09"),
            new PrjBand("AGRI", 2000f, "10", 10, channel, "7.10um channel", "NOMChannel10"),
            new PrjBand("AGRI", 2000f, "11", 11, channel, "8.50um channel", "NOMChannel11"),
            new PrjBand("AGRI", 2000f, "12", 12, channel, "10.8um channel", "NOMChannel12"),
            new PrjBand("AGRI", 2000f, "13", 13, channel, "12.0um channel", "NOMChannel13"),
            new PrjBand("AGRI", 2000f, "14", 14, channel, "13.5um channel", "NOMChannel14"),
            };
        }

        internal static PrjBand[] FY4_2000_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            return new PrjBand[]{
            new PrjBand("AGRI", 2000f, "1", 1, channel, "0.47um channel", "NOMChannel01"),
            new PrjBand("AGRI", 2000f, "2", 2, channel, "0.65um channel", "NOMChannel02"),
            new PrjBand("AGRI", 2000f, "3", 3, channel, "0.83um channel", "NOMChannel03"),
            new PrjBand("AGRI", 2000f, "4", 4, channel, "1.37um channel", "NOMChannel04"),
            new PrjBand("AGRI", 2000f, "5", 5, channel, "1.61um channel", "NOMChannel05"),
            new PrjBand("AGRI", 2000f, "6", 6, channel, "2.22um channel", "NOMChannel06"),
            new PrjBand("AGRI", 2000f, "7", 7, channel, "3.72um channel", "NOMChannel07"),
            };
        }

        internal static PrjBand[] FY4_1000_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            return new PrjBand[]{
            new PrjBand("AGRI", 1000f, "1", 1, channel, "0.47um channel", "NOMChannel01"),
            new PrjBand("AGRI", 1000f, "2", 2, channel, "0.65um channel", "NOMChannel02"),
            new PrjBand("AGRI", 1000f, "3", 3, channel, "0.83um channel", "NOMChannel03"),

            };
        }

        internal static PrjBand[] FY4_0500_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            return new PrjBand[]{
            new PrjBand("AGRI", 500f, "1", 2, channel, "0.65um channel", "NOMChannel02"),
            };
        }

        internal static PrjBand[] H08_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            return new PrjBand[]{
            new PrjBand("H08", 1000f, "1", 1, channel, "0.47um channel", "NOMChannel01"),
            new PrjBand("H08", 1000f, "2", 2, channel, "0.51um channel", "NOMChannel02"),
            new PrjBand("H08", 500f, "3", 3, channel, "0.64um channel", "NOMChannel03"),
            new PrjBand("H08", 1000f, "4", 4, channel, "0.86um channel", "NOMChannel04"),
            new PrjBand("H08", 2000f, "5", 5, channel, "1.6um channel", "NOMChannel05"),
            new PrjBand("H08", 2000f, "6", 6, channel, "2.3um channel", "NOMChannel06"),
            new PrjBand("H08", 2000f, "7", 7, channel, "3.9um channel", "NOMChannel07"),
            new PrjBand("H08", 2000f, "8", 8, channel, "6.2um channel", "NOMChannel08"),
            new PrjBand("H08", 2000f, "9", 9, channel, "6.9um channel", "NOMChannel09"),
            new PrjBand("H08", 2000f, "10", 10, channel, "7.3um channel", "NOMChannel10"),
            new PrjBand("H08", 2000f, "11", 11, channel, "8.6um channel", "NOMChannel11"),
            new PrjBand("H08", 2000f, "12", 12, channel, "9.6um channel", "NOMChannel12"),
            new PrjBand("H08", 2000f, "13", 13, channel, "10.4um channel", "NOMChannel13"),
            new PrjBand("H08", 2000f, "14", 14, channel, "11.2um channel", "NOMChannel14"),
            new PrjBand("H08", 2000f, "15", 15, channel, "12.4um channel", "NOMChannel15"),
            new PrjBand("H08", 2000f, "16", 16, channel, "13.3um channel", "NOMChannel16"),
            };
        }

        internal static PrjBand[] NPP_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            return new PrjBand[]{
                new PrjBand("NPP", 375f, "1", 1, channel, "375m 0.640um", "Imagery,vegetation"),
                new PrjBand("NPP", 375f, "2", 2, channel, "375m 0.865um", "Vegetation"),
                new PrjBand("NPP", 375f, "3", 3, channel, "375m 1.61um", "Binary snow map"),
                new PrjBand("NPP", 375f, "4", 4, channel, "375m 3.74um", "Imagery,clouds"),
                new PrjBand("NPP", 375f, "5", 5, channel, "375m 11.45um", "Cloud imagery"),

                new PrjBand("NPP", 750f, "6", 1, channel, "750m 0.412um", "Ocean color,aerosols"),
                new PrjBand("NPP", 750f, "7", 2, channel, "750m 0.445um", "Ocean color,aerosols"),
                new PrjBand("NPP", 750f, "8", 3, channel, "750m 0.488um", "Ocean color,aerosols"),
                new PrjBand("NPP", 750f, "9", 4, channel, "750m 0.555um", "Ocean color,aerosols"),
                new PrjBand("NPP", 750f, "10", 5, channel, "750m 0.672um", "Ocean color,aerosols"),
                new PrjBand("NPP", 750f, "11", 6, channel, "750m 0.746um", "Atmospheric correction"),
                new PrjBand("NPP", 750f, "12", 7, channel, "750m 0.865um", "Ocean color,aerosols"),
                new PrjBand("NPP", 750f, "13", 8, channel, "750m 1.24um", "Cloud particle size"),
                new PrjBand("NPP", 750f, "14", 9, channel, "750m 1.38um", "Cirrus cloud cover"),
                new PrjBand("NPP", 750f, "15", 10, channel, "750m 1.61um", "Snow fraction"),
                new PrjBand("NPP", 750f, "16", 11, channel, "750m 2.25um", "Clouds"),
                new PrjBand("NPP", 750f, "17", 12, channel, "750m 3.70um", "SST"),
                new PrjBand("NPP", 750f, "18", 13, channel, "750m 4.05um", "SST,fires"),
                new PrjBand("NPP", 750f, "19", 14, channel, "750m 8.55um", "Cloud-top properties"),
                new PrjBand("NPP", 750f, "20", 15, channel, "750m 10.76um", "SST"),
                new PrjBand("NPP", 750f, "21", 16, channel, "750m 12.01um", "SST"),
            };
        }
        internal static PrjBand[] NOAA_AVHRR_OrbitDefCollecges()
        {
            string channel = FileProject_BandDefCollection_Channel;
            return new PrjBand[]{
                new PrjBand("AVHRR", 1000f, "1", 0, channel, "1KM 0.58-0.68", "可见光"),
                new PrjBand("AVHRR", 1000f, "2", 1, channel, "1KM 0.73-1.1", "近红外"),
                new PrjBand("AVHRR", 1000f, "3", 2, channel, "1KM 3.55-3.93", "中红外"),
                new PrjBand("AVHRR", 1000f, "4", 3, channel, "1KM 10.3-11.3", "热红外"),
                new PrjBand("AVHRR", 1000f, "5", 4, channel, "1KM 11.5-12.5", "热红外"),
            };
        }
    }
}
