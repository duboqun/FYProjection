using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PIE.Meteo.Model;

namespace PIE.Meteo.FileProject
{
    public interface IFileChecker
    {
        string GetFileType(string file);
        string GetFileType(AbstractWarpDataset file);
    }

    public abstract class FileCheckerBase : IFileChecker
    {
        public string GetFileType(string file)
        {
            WarpDataset rad = GetSrcRaster(file);
            try
            {
                return GetFileType(rad);
            }
            finally
            {
                if (rad != null)
                    rad.Dispose();
            }
        }

        public abstract string GetFileType(AbstractWarpDataset file);

        private WarpDataset GetSrcRaster(string filename)
        {
            return WarpDataset.Open(filename);
        }
    }

    public class FileChecker : FileCheckerBase
    {
        public FileChecker()
        { }
        
        public override string GetFileType(AbstractWarpDataset file)
        {
            try
            {
                if (file == null)
                    throw new Exception("文件为空");
                RasterDatasetInfo info = mRasterSourceManager.GetInstance().GetRasterDatasetInfo(file.fileName);

                if (file.isMultiDs)
                {
                    if (info.SensorID == "VIRR")       //dataIdentify.Satellite == "FY3B"||"FY3A"
                    {
                        if (info.SatelliteID == "FY3C")
                            return "FY3C_VIRR_L1";
                        else
                            return "VIRR_L1";
                    }
                    else if (info.SensorID == "MERSI")
                    {
                        if (info.SatelliteID == "FY3C")
                        {
                            if (file.fileName.Contains("1000M"))
                                return "FY3C_MERSI_1KM_L1";
                            else if (file.fileName.Contains("0250M"))
                                return "FY3C_MERSI_QKM_L1";
                        }
                        else if (info.SatelliteID == "FY3D")
                        {
                            if (file.fileName.Contains("1000M"))
                                return "FY3D_MERSI_1KM_L1";
                            else if (file.fileName.Contains("0250M"))
                                return "FY3D_MERSI_QKM_L1";
                        }
                        else
                        {
                            if (file.fileName.Contains("1000M"))
                                return "MERSI_1KM_L1";
                            else if (file.fileName.Contains("0250M"))
                                return "MERSI_QKM_L1";
                        }
                    }
                    else if (modisSensor.Contains(info.SensorID) || modisSatellite.Contains(info.SatelliteID)) //info.SatelliteID == "EOST"||"EOST"//TERRA
                    {
                        if (file.fileName.Contains("MOD021KM") || file.fileName.Contains("MYD021KM"))//MOD/MYD021KM)、MOD/MYD03GEO、
                            return "MODIS_1KM_L1";
                        else if (file.fileName.Contains("MOD02HKM") || file.fileName.Contains("MYD02HKM"))
                            return "MODIS_HKM_L1";
                        else if (file.fileName.Contains("MOD02QKM") || file.fileName.Contains("MYD02QKM"))
                            return "MODIS_QKM_L1";
                    }
                    else if ((info.SatelliteID == "FY1D" || info.SatelliteID == "FY1C") && info.SensorID == "AVHRR")
                        return "FY1X_1A5";
                    else if (info.SensorID == "AVHRR") //info.SatelliteID == "NOAA-18" && 
                        return "NOAA_1BD";
                    else if (info.SatelliteID == "NOAA" && info.SensorID == "AVHRR")
                        return "NOAA_1A5_L1";
                    else if (info.SensorID == "VISSR" || fy2Satellite.Contains(info.SatelliteID))
                        return "FY2NOM";
                    else if (info.SensorID == "AGRI" && info.SatelliteID == "FY4A")
                    {
                        switch (info.BandCol.FirstOrDefault().resolution)
                        {
                            case "500":
                                return "FY4A_AGRI_0500";
                            case "1000":
                                return "FY4A_AGRI_1000";
                            case "2000":
                                return "FY4A_AGRI_2000";
                            case "4000":
                                return "FY4A_AGRI_4000";
                        }
                    }
                    else if (info.SensorID == "VIIRS" && info.SatelliteID == "NPP")
                        return "NPP";

                }
                else if (info.SatelliteID == "H8")
                {
                    return "H8";
                }
                else if (info.SatelliteID == "NOAA")
                {
                    return "NOAA";
                }
                else if (file.SpatialRef != null)
                    return "PROJECTED";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FileChecker.GetFileType() {file.fileName}");
                throw new Exception($"未获得是轨道数据或者是已投影数据标识，无法确定正确的数据来源. {Path.GetFileName(file.fileName)}", ex.InnerException);
            }

            Console.WriteLine($"FileChecker.GetFileType() {file.fileName}");
            throw new Exception($"未获得是轨道数据或者是已投影数据标识，无法确定正确的数据来源. {Path.GetFileName(file.fileName)}");
        }

        private static string[] modisSensor = new string[] { "MODIS", "MOD" };
        private static string[] modisSatellite = new string[] { "TERRA", "AQUA", "EOST", "EOSA" };
        private static string[] fy2Satellite = new string[] { "FY2G", "FY2H", "FY2E", "FY2F" };
    }
}
