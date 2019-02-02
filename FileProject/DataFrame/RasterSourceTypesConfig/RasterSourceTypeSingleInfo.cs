using System;
using System.Xml;

namespace PIE.Meteo.Model
{
    public class RasterSourceTypeSingleInfo
    {
        public string ID
        { get; set; }
        public string describe
        { get; set; }
        public string datasetsID
        { get; set; }
        public string bandID
        { get; set; }
        //[DefaultValue("true")]
        public bool displayDataset
        { get; set; }
        public string defaultDisplayDataset
        { get; set; }
        public KeyNamesInfo KeyNamesCol { get; set; }
        public FileInfoInfo FileInfoCol { get; private set; }
        public CoordinateInfo CoordinateCol { get; private set; }
        public DateTimeInfo DateTimeCol { get; private set; }
        public RenderInfo RenderCol { get; private set; }
        public CalibrationInfo CalibrationCol { get; private set; }
        //public static List<KeyNamesInfo> KeyNamesCol { get; private set; }
        //public static List<FileInfoInfo> FileInfoCol { get; private set; }
        //public static List<CoordinateInfo> CoordinateCol { get; private set; }
        //public static List<DateTimeInfo> DateTimeCol { get; private set; }
        //public static List<RenderInfo> RenderCol { get; private set; }
        //public static List<CalibrationInfo> CalibrationCol { get; private set; }

        public RasterSourceTypeSingleInfo(XmlElement xe)
        {
            try
            {

                //RasterSourceTypeSingleInfo RasterSourceTypeInfo = new RasterSourceTypeSingleInfo();
                if (xe.Attributes["ID"] != null)
                {
                    ID = xe.GetAttribute("ID").ToString();
                }
                if (xe.Attributes["describe"] != null)
                {
                    describe = xe.GetAttribute("describe").ToString();
                }
                if (xe.Attributes["datasetsID"] != null)
                {
                    datasetsID = xe.GetAttribute("datasetsID").ToString();
                }
                if (xe.Attributes["bandID"] != null)
                {
                    bandID = xe.GetAttribute("bandID").ToString();
                }
                if (xe.Attributes["displayDataset"] != null)
                {
                    displayDataset = Convert.ToBoolean(xe.GetAttribute("displayDataset").ToString());
                }
                if (xe.Attributes["defaultDisplayDataset"] != null)
                {
                    defaultDisplayDataset = xe.GetAttribute("defaultDisplayDataset").ToString();
                }

                XmlNodeList xnl = xe.ChildNodes;
                foreach (XmlNode xn in xnl)
                {
                    switch (xn.Name)
                    {
                        case "KeyNames":
                            XmlNode xnKeyNames = xe.SelectSingleNode("KeyNames");
                            KeyNamesCol = new KeyNamesInfo(xnKeyNames as XmlElement);
                            //XmlNodeList xnl1 = xe.SelectNodes("KeyNames");
                            //List<KeyNamesInfo> keyNames = new List<KeyNamesInfo>();
                            //foreach (var xnode in xnl1)
                            //{
                            //    //sens.Add(FileInfo.GetSensorIDInfo(xnode as XmlElement));
                            //    KeyNamesCol = new KeyNamesInfo(xnode as XmlElement);
                            //    //keyNames.Add(KeyNamesInfo.GetKeyNamesInfo(xnode as XmlElement));
                            //}
                            //KeyNamesCol = keyNames;
                            break;
                        case "FileInfo":
                            XmlNode xnFileInfo = xe.SelectSingleNode("FileInfo");
                            FileInfoCol = new FileInfoInfo(xnFileInfo as XmlElement);
                            //XmlNodeList xnl2 = xe.SelectNodes("FileInfo");
                            //List<FileInfoInfo> fileInfos = new List<FileInfoInfo>();
                            //foreach (var xnode in xnl2)
                            //{
                            //    //sens.Add(FileInfo.GetSensorIDInfo(xnode as XmlElement));
                            //    fileInfos.Add(FileInfoInfo.GetFileInfoInfo(xnode as XmlElement));
                            //}
                            //FileInfoCol = fileInfos;
                            break;
                        case "Coordinate":
                            XmlNode xnCoordinate = xe.SelectSingleNode("Coordinate");
                            CoordinateCol = new CoordinateInfo(xnCoordinate as XmlElement);
                            //XmlNodeList xnl3 = xe.SelectNodes("Coordinate");
                            //List<CoordinateInfo> coordinates = new List<CoordinateInfo>();
                            //foreach (var xnode in xnl3)
                            //{
                            //    //sens.Add(FileInfo.GetSensorIDInfo(xnode as XmlElement));
                            //    coordinates.Add(CoordinateInfo.GetCoordinateInfo(xnode as XmlElement));
                            //}
                            //CoordinateCol = coordinates;
                            break;
                        case "DateTime":
                            XmlNode xnDateTime = xe.SelectSingleNode("DateTime");
                            DateTimeCol = new DateTimeInfo(xnDateTime as XmlElement);
                            //XmlNodeList xnl4 = xe.SelectNodes("DateTime");
                            //List<DateTimeInfo> dateTimes = new List<DateTimeInfo>();
                            //foreach (var xnode in xnl4)
                            //{
                            //    //sens.Add(FileInfo.GetSensorIDInfo(xnode as XmlElement));
                            //    dateTimes.Add(DateTimeInfo.GetDateTimeInfo(xnode as XmlElement));
                            //}
                            //DateTimeCol = dateTimes;
                            break;
                        case "Render":
                            XmlNode xnRender = xe.SelectSingleNode("Render");
                            RenderCol = new RenderInfo(xnRender as XmlElement);
                            //XmlNodeList xnl5 = xe.SelectNodes("Render");
                            //List<RenderInfo> renders = new List<RenderInfo>();
                            //foreach (var xnode in xnl5)
                            //{
                            //    //sens.Add(FileInfo.GetSensorIDInfo(xnode as XmlElement));
                            //    renders.Add(RenderInfo.GetRenderInfo(xnode as XmlElement));
                            //}
                            //RenderCol = renders;
                            break;
                        case "Calibration":
                            XmlNode xnCalibration = xe.SelectSingleNode("Calibration");
                            CalibrationCol = new CalibrationInfo(xnCalibration as XmlElement);
                            //XmlNodeList xnl6 = xe.SelectNodes("Calibration");
                            //List<CalibrationInfo> calibrations = new List<CalibrationInfo>();
                            //foreach (var xnode in xnl6)
                            //{
                            //    //sens.Add(FileInfo.GetSensorIDInfo(xnode as XmlElement));
                            //    calibrations.Add(CalibrationInfo.GetCalibrationInfo(xnode as XmlElement));
                            //}
                            //CalibrationCol = calibrations;
                            break;
                        default:
                            break;
                    }
                }

                //XmlNode xn=xe.SelectSingleNode("KeyNames")


                //return RasterSourceTypeInfo;
            }
            catch (Exception ex)
            { throw ex; }
        }
        //public static RasterSourceTypeSingleInfo GetRasterSourceTypeSingleInfo(XmlElement xe)
        //{
        //try
        //{

        //    RasterSourceTypeSingleInfo RasterSourceTypeInfo = new RasterSourceTypeSingleInfo();
        //    RasterSourceTypeInfo.ID = xe.GetAttribute("ID").ToString();
        //    RasterSourceTypeInfo.describe = xe.GetAttribute("describe").ToString();
        //    RasterSourceTypeInfo.datasetsID = xe.GetAttribute("datasetsID").ToString();

        //    XmlNodeList xnl = xe.ChildNodes;
        //    foreach (XmlNode xn in xnl)
        //    {
        //        switch(xn.Name)
        //        {
        //            case "KeyNames":
        //                XmlNodeList xnl1 = xe.SelectNodes("KeyNames");
        //                //List<KeyNamesInfo> keyNames = new List<KeyNamesInfo>();
        //                foreach (var xnode in xnl1)
        //                {
        //                    //sens.Add(FileInfo.GetSensorIDInfo(xnode as XmlElement));
        //                    KeyNamesCol = new KeyNamesInfo(xnode as XmlElement);
        //                    //keyNames.Add(KeyNamesInfo.GetKeyNamesInfo(xnode as XmlElement));
        //                }
        //                //KeyNamesCol = keyNames;
        //                break;
        //            case "FileInfo":
        //                XmlNodeList xnl2 = xe.SelectNodes("FileInfo");
        //                List<FileInfoInfo> fileInfos = new List<FileInfoInfo>();
        //                foreach (var xnode in xnl2)
        //                {
        //                    //sens.Add(FileInfo.GetSensorIDInfo(xnode as XmlElement));
        //                    fileInfos.Add(FileInfoInfo.GetFileInfoInfo(xnode as XmlElement));
        //                }
        //                FileInfoCol = fileInfos;
        //                break;
        //            case "Coordinate":
        //                XmlNodeList xnl3 = xe.SelectNodes("Coordinate");
        //                List<CoordinateInfo> coordinates = new List<CoordinateInfo>();
        //                foreach (var xnode in xnl3)
        //                {
        //                    //sens.Add(FileInfo.GetSensorIDInfo(xnode as XmlElement));
        //                    coordinates.Add(CoordinateInfo.GetCoordinateInfo(xnode as XmlElement));
        //                }
        //                CoordinateCol = coordinates;
        //                break;
        //            case "DateTime":
        //                XmlNodeList xnl4 = xe.SelectNodes("DateTime");
        //                List<DateTimeInfo> dateTimes = new List<DateTimeInfo>();
        //                foreach (var xnode in xnl4)
        //                {
        //                    //sens.Add(FileInfo.GetSensorIDInfo(xnode as XmlElement));
        //                    dateTimes.Add(DateTimeInfo.GetDateTimeInfo(xnode as XmlElement));
        //                }
        //                DateTimeCol = dateTimes;
        //                break;
        //            case "Render":
        //                XmlNodeList xnl5 = xe.SelectNodes("Render");
        //                List<RenderInfo> renders = new List<RenderInfo>();
        //                foreach (var xnode in xnl5)
        //                {
        //                    //sens.Add(FileInfo.GetSensorIDInfo(xnode as XmlElement));
        //                    renders.Add(RenderInfo.GetRenderInfo(xnode as XmlElement));
        //                }
        //                RenderCol = renders;
        //                break;
        //            case "Calibration":
        //                 XmlNodeList xnl6 = xe.SelectNodes("Calibration");
        //                List<CalibrationInfo> calibrations = new List<CalibrationInfo>();
        //                foreach (var xnode in xnl6)
        //                {
        //                    //sens.Add(FileInfo.GetSensorIDInfo(xnode as XmlElement));
        //                    calibrations.Add(CalibrationInfo.GetCalibrationInfo(xnode as XmlElement));
        //                }
        //                CalibrationCol = calibrations;
        //                break;
        //            default:
        //                break;
        //        }
        //    }

        //    //XmlNode xn=xe.SelectSingleNode("KeyNames")
        //    return RasterSourceTypeInfo;
        //}
        //catch(Exception ex)
        //{ throw ex; }
        // }
    }
}
