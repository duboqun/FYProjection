﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PIE.Meteo.FileProject
{
    public class BandDefCollection
    {
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
                default:
                    return null;
            }
            return null;
        }

        internal static PrjBand[] AVHRR_1000_OrbitDefCollecges()
        {
            return new PrjBand[]{
                new PrjBand("AVHRR", 0.01f, "1", 0, "通道", "0.58-0.68 band_1", "band"),
                new PrjBand("AVHRR", 0.01f, "2", 1, "通道", "0.70-1.10 band_2", "band"),
                new PrjBand("AVHRR", 0.01f, "3", 2, "通道", "3.55-3.95 band_3", "band"),
                new PrjBand("AVHRR", 0.01f, "4", 3, "通道", "10.3-11.3 band_4", "band"),
                new PrjBand("AVHRR", 0.01f, "5", 4, "通道", "11.5-12.5 band_5", "band")
            };
        }

        internal static PrjBand[] MERSI_0250_OrbitDefCollecges()
        {
            return new PrjBand[]{
            new PrjBand("MERSI", 0.0025f, "1", 0, "通道", "0.470 band_1", "EV_250_RefSB_b1"),
            new PrjBand("MERSI", 0.0025f, "2", 0, "通道", "0.550 band_2", "EV_250_RefSB_b2"),
            new PrjBand("MERSI", 0.0025f, "3", 0, "通道", "0.650 band_3", "EV_250_RefSB_b3"),
            new PrjBand("MERSI", 0.0025f, "4", 0, "通道", "0.865 band_4", "EV_250_RefSB_b4"),
            new PrjBand("MERSI", 0.0025f, "5", 0, "通道", "11.25 band_5", "EV_250_Emissive")
            };
        }

        internal static PrjBand[] MERSI2_0250_OrbitDefCollecges()
        {
            return new PrjBand[]{
            new PrjBand("MERSI", 0.0025f, "1", 0, "通道", "可见光 0250M 0.47", "EV_250_RefSB_b1"),
            new PrjBand("MERSI", 0.0025f, "2", 0, "通道", "可见光 0250M 0.55", "EV_250_RefSB_b2"),
            new PrjBand("MERSI", 0.0025f, "3", 0, "通道", "可见光 0250M 0.65", "EV_250_RefSB_b3"),
            new PrjBand("MERSI", 0.0025f, "4", 0, "通道", "可见光 0250M 0.865", "EV_250_RefSB_b4"),
            new PrjBand("MERSI", 0.0025f, "5", 0, "通道", "远红外 0250M 10.8", "EV_250_Emissive_b24"),
            new PrjBand("MERSI", 0.0025f, "6", 0, "通道", "远红外 0250M 12.0", "EV_250_Emissive_b25")
            };
        }

        internal static PrjBand[] MERSI_1000_OrbitDefCollecges()
        {
            return new PrjBand[]{
            new PrjBand("MERSI", 0.01f, "1", 0, "通道", "0.470 band_1", "EV_250_Aggr.1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "2", 1, "通道", "0.550 band_2", "EV_250_Aggr.1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "3", 2, "通道", "0.650 band_3", "EV_250_Aggr.1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "4", 3, "通道", "0.865 band_4", "EV_250_Aggr.1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "5", 0, "通道", "11.25 band_5", "EV_250_Aggr.1KM_Emissive"),
            new PrjBand("MERSI", 0.01f, "6", 0, "通道", "1.640 band_6", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "7", 1, "通道", "2.130 band_7", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "8", 2, "通道", "0.412 band_8", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "9", 3, "通道", "0.443 band_9", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "10", 4, "通道", "0.490 band_10", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "11", 5, "通道", "0.520 band_11", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "12", 6, "通道", "0.565 band_12", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "13", 7, "通道", "0.650 band_13", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "14", 8, "通道", "0.685 band_14", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "15", 9, "通道", "0.765 band_15", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "16", 10, "通道", "0.865 band_16", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "17", 11, "通道", "0.905 band_17", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "18", 12, "通道", "0.940 band_18", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "19", 13, "通道", "0.980 band_19", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "20", 14, "通道", "1.030 band_20", "EV_1KM_RefSB")
            };
        }


        internal static PrjBand[] MERSI2_1000_OrbitDefCollecges()
        {
            return new PrjBand[]{
            new PrjBand("MERSI", 0.01f, "1", 0, "通道", "可见光 0250M 0.47μm", "EV_250_Aggr.1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "2", 1, "通道", "可见光 0250M 0.55μm", "EV_250_Aggr.1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "3", 2, "通道", "可见光 0250M 0.65μm", "EV_250_Aggr.1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "4", 3, "通道", "可见光 0250M 0.865μm", "EV_250_Aggr.1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "24", 0, "通道", "远红外 0250M 10.8μm", "EV_250_Aggr.1KM_Emissive"),
            new PrjBand("MERSI", 0.01f, "25", 1, "通道", "远红外 0250M 12.0μm", "EV_250_Aggr.1KM_Emissive"),
            new PrjBand("MERSI", 0.01f, "5", 0, "通道", "近红外 1000M 1.38μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "6", 1, "通道", "近红外 1000M 1.64μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "7", 2, "通道", "近红外 1000M 2.13μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "8", 3, "通道", "可见光 1000M 0.412μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "9", 4, "通道", "可见光 1000M 0.443μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "10", 5, "通道", "可见光 1000M 0.49μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "11", 6, "通道", "可见光 1000M 0.555μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "12", 7, "通道", "可见光 1000M 0.67μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "13", 8, "通道", "可见光 1000M 0.709μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "14", 9, "通道", "可见光 1000M 0.746μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "15", 10, "通道", "可见光 1000M 0.865μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "16", 11, "通道", "可见光 1000M 0.905μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "17", 12, "通道", "可见光 1000M 0.936μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "18", 13, "通道", "可见光 1000M 0.940μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "19", 14, "通道", "近红外 1000M 1.030μm", "EV_1KM_RefSB"),
            new PrjBand("MERSI", 0.01f, "20", 0, "通道", "中红外 1000M 3.80μm", "EV_1KM_Emissive"),
            new PrjBand("MERSI", 0.01f, "21", 1, "通道", "中红外 1000M 4.05μm", "EV_1KM_Emissive"),
            new PrjBand("MERSI", 0.01f, "22", 2, "通道", "中红外 1000M 7.20μm", "EV_1KM_Emissive"),
            new PrjBand("MERSI", 0.01f, "23", 3, "通道", "远红外 1000M 8.55μm", "EV_1KM_Emissive")

            };
        }


        internal static PrjBand[] MODIS_1000_OrbitDefCollecges()
        {
            return new PrjBand[]{
            new PrjBand("MODIS", 0.01f, "1", 0, "通道", "620-670 云/陆地边界", "EV_250_Aggr1km_RefSB"),
            new PrjBand("MODIS", 0.01f, "2", 1, "通道", "841-876 云/陆地边界", "EV_250_Aggr1km_RefSB"),
            new PrjBand("MODIS", 0.01f, "3", 0, "通道", "459-479 云/陆地特性", "EV_500_Aggr1km_RefSB"),
            new PrjBand("MODIS", 0.01f, "4", 1, "通道", "545-565 云/陆地特性", "EV_500_Aggr1km_RefSB"),
            new PrjBand("MODIS", 0.01f, "5", 2, "通道", "1230-1250 云/陆地特性", "EV_500_Aggr1km_RefSB"),
            new PrjBand("MODIS", 0.01f, "6", 3, "通道", "1628-1652 云/陆地特性", "EV_500_Aggr1km_RefSB"),
            new PrjBand("MODIS", 0.01f, "7", 4, "通道", "2105-2155 云/陆地特性", "EV_500_Aggr1km_RefSB"),
            new PrjBand("MODIS", 0.01f, "8", 0, "通道", "405-420 海洋水色/生化", "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "9", 1, "通道", "438-448 海洋水色/生化", "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "10", 2, "通道", "483-493 海洋水色/生化", "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "11", 3, "通道", "526-536 海洋水色/生化", "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "12", 4, "通道", "546-556 海洋水色/生化", "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "13lo", 5, "通道", "662-672 海洋水色/生化", "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "13hi", 6, "通道", "662-672 海洋水色/生化", "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "14lo", 7, "通道", "673-753 海洋水色/生化", "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "14hi", 8, "通道", "673-753 海洋水色/生化", "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "15", 9, "通道", "743-753 海洋水色/生化", "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "16", 10, "通道", "862-877 海洋水色/生化", "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "17", 11, "通道", "890-920 大气水汽", "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "18", 12, "通道", "931-941 大气水汽", "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "19", 13, "通道", "915-965 大气水汽", "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "20", 0, "通道", "3.66-3.84 地表/云顶温度", "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "21", 1, "通道", "3.93-3.99 地表/云顶温度", "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "22", 2, "通道", "3.93-3.99 地表/云顶温度", "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "23", 3, "通道", "4.02-4.08 地表/云顶温度", "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "24", 4, "通道", "4.43-4.49 大气温度", "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "25", 5, "通道", "4.48-4.55 大气温度", "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "26", 14, "通道", "1.36-1.39 卷云", "EV_1KM_RefSB"),
            new PrjBand("MODIS", 0.01f, "27", 6, "通道", "6.53-6.89 水汽", "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "28", 7, "通道", "7.17-7.47 水汽", "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "29", 8, "通道", "8.40-8.70 水汽", "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "30", 9, "通道", "9.58-9.88 臭氧", "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "31", 10, "通道", "10.78-11.28 地表/云顶温度", "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "32", 11, "通道", "11.77-12.27 地表/云顶温度", "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "33", 12, "通道", "13.18-13.48 云顶高度", "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "34", 13, "通道", "13.48-13.78 云顶高度", "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "35", 14, "通道", "13.78-14.08 云顶高度", "EV_1KM_Emissive"),
            new PrjBand("MODIS", 0.01f, "36", 15, "通道", "14.08-14.38 云顶高度", "EV_1KM_Emissive")
            };
        }

        internal static PrjBand[] MODIS_250_OrbitDefCollecges()
        {
            return new PrjBand[]{
            new PrjBand("MODIS", 0.0025f, "1", 0, "通道", "620-670 云/陆地边界", "EV_250_RefSB"),
            new PrjBand("MODIS", 0.0025f, "2", 1, "通道", "841-876 云/陆地边界", "EV_250_RefSB"),
            };
        }

        internal static PrjBand[] MODIS_500_OrbitDefCollecges()
        {
            return new PrjBand[]{
            new PrjBand("MODIS", 0.005f, "1", 0, "通道", "620-670 云/陆地边界", "EV_250_Aggr500_RefSB"),
            new PrjBand("MODIS", 0.005f, "2", 1, "通道", "841-876 云/陆地边界", "EV_250_Aggr500_RefSB"),
            new PrjBand("MODIS", 0.005f, "3", 0, "通道", "459-479 云/陆地特性", "EV_500_RefSB"),
            new PrjBand("MODIS", 0.005f, "4", 1, "通道", "545-565 云/陆地特性", "EV_500_RefSB"),
            new PrjBand("MODIS", 0.005f, "5", 2, "通道", "1230-1250 云/陆地特性", "EV_500_RefSB"),
            new PrjBand("MODIS", 0.005f, "6", 3, "通道", "1628-1652 云/陆地特性", "EV_500_RefSB"),
            new PrjBand("MODIS", 0.005f, "7", 4, "通道", "2105-2155 云/陆地特性", "EV_500_RefSB")
            };
        }

        internal static PrjBand[] VIRR_1000_OrbitDefCollecges()
        {
            return new PrjBand[]{
            new PrjBand("VIRR", 0.01f, "1", 0, "通道", "0.58-0.68 band_1", "EV_RefSB"),
            new PrjBand("VIRR", 0.01f, "2", 1, "通道", "0.84-0.89 band_2", "EV_RefSB"),
            new PrjBand("VIRR", 0.01f, "3", 0, "通道", "3.55-3.93 band_3", "EV_Emissive"),
            new PrjBand("VIRR", 0.01f, "4", 1, "通道", "10.3-11.3 band_4", "EV_Emissive"),
            new PrjBand("VIRR", 0.01f, "5", 2, "通道", "11.5-12.5 band_5", "EV_Emissive"),
            new PrjBand("VIRR", 0.01f, "6", 2, "通道", "1.55-1.64 band_6", "EV_RefSB"),
            new PrjBand("VIRR", 0.01f, "7", 3, "通道", "0.43-0.48 band_7", "EV_RefSB"),
            new PrjBand("VIRR", 0.01f, "8", 4, "通道", "0.48-0.53 band_8", "EV_RefSB"),
            new PrjBand("VIRR", 0.01f, "9", 5, "通道", "0.53-0.58 band_9", "EV_RefSB"),
            new PrjBand("VIRR", 0.01f, "10", 6, "通道", "1.325-1.395 band_10", "EV_RefSB")
            };
        }

        internal static PrjBand[] FY1D_4000_OrbitDefCollecges()
        {
            return new PrjBand[]{
            new PrjBand("VISSR", 0.01f, "1", 0, "通道", "0.58-0.68 band_1", ""),
            new PrjBand("VISSR", 0.01f, "2", 1, "通道", "0.84-0.89 band_2", ""),
            new PrjBand("VISSR", 0.01f, "3", 2, "通道", "3.55-3.95 band_3", ""),
            new PrjBand("VISSR", 0.01f, "4", 3, "通道", "0.3-11.3 band_4", "")
            //,new PrjBand("VISSR", 0.01f, "5", 4, "通道", "11.5-12.5 band_5", ""),
            //new PrjBand("VISSR", 0.01f, "6", 5, "通道", "1.58-1.64 band_6", ""),
            //new PrjBand("VISSR", 0.01f, "7", 6, "通道", "0.43-0.48 band_7", ""),
            //new PrjBand("VISSR", 0.01f, "8", 7, "通道", "0.48-0.53 band_8", ""),
            //new PrjBand("VISSR", 0.01f, "9", 8, "通道", "0.53-0.58 band_9", ""),
            //new PrjBand("VISSR", 0.01f, "10", 9, "通道", "0.9-0.985 band_10", "")
            };
        }

        internal static PrjBand[] FY1D_1000_OrbitDefCollecges()
        {
            return new PrjBand[]{
            new PrjBand("VISSR", 0.01f, "1", 0, "通道", "0.58-0.68 band_1", ""),
            new PrjBand("VISSR", 0.01f, "2", 1, "通道", "0.84-0.89 band_2", ""),
            new PrjBand("VISSR", 0.01f, "3", 2, "通道", "3.55-3.95 band_3", ""),
            new PrjBand("VISSR", 0.01f, "4", 3, "通道", "0.3-11.3 band_4", "")
            ,new PrjBand("VISSR", 0.01f, "5", 4, "通道", "11.5-12.5 band_5", ""),
            new PrjBand("VISSR", 0.01f, "6", 5, "通道", "1.58-1.64 band_6", ""),
            new PrjBand("VISSR", 0.01f, "7", 6, "通道", "0.43-0.48 band_7", ""),
            new PrjBand("VISSR", 0.01f, "8", 7, "通道", "0.48-0.53 band_8", ""),
            new PrjBand("VISSR", 0.01f, "9", 8, "通道", "0.53-0.58 band_9", ""),
            new PrjBand("VISSR", 0.01f, "10", 9, "通道", "0.9-0.985 band_10", "")
            };
        }

        internal static PrjBand[] FY2_5000_OrbitDefCollecges()
        {
            return new PrjBand[]{
            new PrjBand("VIRR", 5000f, "1", 0, "通道", "NOMChannelIR1 band_1", "NOMChannelIR1"),
            new PrjBand("VIRR", 5000f, "2", 0, "通道", "NOMChannelIR2 band_2", "NOMChannelIR2"),
            new PrjBand("VIRR", 5000f, "3", 0, "通道", "NOMChannelIR3 band_3", "NOMChannelIR3"),
            new PrjBand("VIRR", 5000f, "4", 0, "通道", "NOMChannelIR4 band_4", "NOMChannelIR4"),
            new PrjBand("VIRR", 5000f, "5", 0, "通道", "NOMChannelVIS band_5", "NOMChannelVIS"),
            //new PrjBand("VIRR", 5000f, "6", 0, "NOMCloudClassification", "NOMCloudClassification", "NOMCloudClassification")
            };
        }
        //ToDo:修改内部属性

        internal static PrjBand[] FY4_4000_OrbitDefCollecges()
        {
            return new PrjBand[]{
            new PrjBand("AGRI", 2000f, "1", 1, "通道", "0.47um channel", "NOMChannel01"),
            new PrjBand("AGRI", 2000f, "2", 2, "通道", "0.65um channel", "NOMChannel02"),
            new PrjBand("AGRI", 2000f, "3", 3, "通道", "0.83um channel", "NOMChannel03"),
            new PrjBand("AGRI", 2000f, "4", 4, "通道", "1.37um channel", "NOMChannel04"),
            new PrjBand("AGRI", 2000f, "5", 5, "通道", "1.61um channel", "NOMChannel05"),
            new PrjBand("AGRI", 2000f, "6", 6, "通道", "2.22um channel", "NOMChannel06"),
            new PrjBand("AGRI", 2000f, "7", 7, "通道", "3.72um channel", "NOMChannel07"),
            new PrjBand("AGRI", 2000f, "8", 8, "通道", "3.72um channel", "NOMChannel08"),
            new PrjBand("AGRI", 2000f, "9", 9, "通道", "6.25um channel", "NOMChannel09"),
            new PrjBand("AGRI", 2000f, "10", 10, "通道", "7.10um channel", "NOMChannel10"),
            new PrjBand("AGRI", 2000f, "11", 11, "通道", "8.50um channel", "NOMChannel11"),
            new PrjBand("AGRI", 2000f, "12", 12, "通道", "10.8um channel", "NOMChannel12"),
            new PrjBand("AGRI", 2000f, "13", 13, "通道", "12.0um channel", "NOMChannel13"),
            new PrjBand("AGRI", 2000f, "14", 14, "通道", "13.5um channel", "NOMChannel14"),
            };
        }

        internal static PrjBand[] FY4_2000_OrbitDefCollecges()
        {
            return new PrjBand[]{
            new PrjBand("AGRI", 2000f, "1", 1, "通道", "0.47um channel", "NOMChannel01"),
            new PrjBand("AGRI", 2000f, "2", 2, "通道", "0.65um channel", "NOMChannel02"),
            new PrjBand("AGRI", 2000f, "3", 3, "通道", "0.83um channel", "NOMChannel03"),
            new PrjBand("AGRI", 2000f, "4", 4, "通道", "1.37um channel", "NOMChannel04"),
            new PrjBand("AGRI", 2000f, "5", 5, "通道", "1.61um channel", "NOMChannel05"),
            new PrjBand("AGRI", 2000f, "6", 6, "通道", "2.22um channel", "NOMChannel06"),
            new PrjBand("AGRI", 2000f, "7", 7, "通道", "3.72um channel", "NOMChannel07"),
            };
        }

        internal static PrjBand[] FY4_1000_OrbitDefCollecges()
        {
            return new PrjBand[]{
            new PrjBand("AGRI", 1000f, "1", 1, "通道", "0.47um channel", "NOMChannel01"),
            new PrjBand("AGRI", 1000f, "2", 2, "通道", "0.65um channel", "NOMChannel02"),
            new PrjBand("AGRI", 1000f, "3", 3, "通道", "0.83um channel", "NOMChannel03"),

            };
        }

        internal static PrjBand[] FY4_0500_OrbitDefCollecges()
        {
            return new PrjBand[]{
            new PrjBand("AGRI", 500f, "1", 2, "通道", "0.65um channel", "NOMChannel02"),
            };
        }

        internal static PrjBand[] H08_OrbitDefCollecges()
        {
            return new PrjBand[]{
            new PrjBand("H08", 1000f, "1", 1, "通道", "0.47um channel", "NOMChannel01"),
            new PrjBand("H08", 1000f, "2", 2, "通道", "0.51um channel", "NOMChannel02"),
            new PrjBand("H08", 500f, "3", 3, "通道", "0.64um channel", "NOMChannel03"),
            new PrjBand("H08", 1000f, "4", 4, "通道", "0.86um channel", "NOMChannel04"),
            new PrjBand("H08", 2000f, "5", 5, "通道", "1.6um channel", "NOMChannel05"),
            new PrjBand("H08", 2000f, "6", 6, "通道", "2.3um channel", "NOMChannel06"),
            new PrjBand("H08", 2000f, "7", 7, "通道", "3.9um channel", "NOMChannel07"),
            new PrjBand("H08", 2000f, "8", 8, "通道", "6.2um channel", "NOMChannel08"),
            new PrjBand("H08", 2000f, "9", 9, "通道", "6.9um channel", "NOMChannel09"),
            new PrjBand("H08", 2000f, "10", 10, "通道", "7.3um channel", "NOMChannel10"),
            new PrjBand("H08", 2000f, "11", 11, "通道", "8.6um channel", "NOMChannel11"),
            new PrjBand("H08", 2000f, "12", 12, "通道", "9.6um channel", "NOMChannel12"),
            new PrjBand("H08", 2000f, "13", 13, "通道", "10.4um channel", "NOMChannel13"),
            new PrjBand("H08", 2000f, "14", 14, "通道", "11.2um channel", "NOMChannel14"),
            new PrjBand("H08", 2000f, "15", 15, "通道", "12.4um channel", "NOMChannel15"),
            new PrjBand("H08", 2000f, "16", 16, "通道", "13.3um channel", "NOMChannel16"),
            };
        }
    }
}
