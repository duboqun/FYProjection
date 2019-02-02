using System;
using System.Xml;

namespace PIE.Meteo.Model
{
    public class RasterSourceTypeGroupInfo
    {
        public string ID
        { get; set; }
        public string describe
        { get; set; }
        public string datasetsID
        { get; set; }
        public string Groupids
        { get; set; }
        //public KeyNamesInfo KeyNamesCol { get; set; }
        //public FileInfoInfo FileInfoCol { get; private set; }
        //public CoordinateInfo CoordinateCol { get; private set; }
        //public DateTimeInfo DateTimeCol { get; private set; }
        //public RenderInfo RenderCol { get; private set; }
        //public CalibrationInfo CalibrationCol { get; private set; }     
        public RasterSourceTypeGroupInfo(XmlElement xe)
        {
            try
            {
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
                if (xe.Attributes["groupids"] != null)
                {
                    Groupids = xe.GetAttribute("groupids").ToString();
                }
            }
            catch (Exception ex)
            { throw ex; }
        }
        public RasterSourceTypeGroupInfo(XmlElement xe, string groupids)
        {
            try
            {
                ID = xe.GetAttribute("ID").ToString();
                describe = xe.GetAttribute("describe").ToString();
                datasetsID = xe.GetAttribute("datasetsID").ToString();
                Groupids = groupids.ToString();

                //XmlNodeList xnl = xe.ChildNodes;
                //foreach (XmlNode xn in xnl)
                //{
                //    switch (xn.Name)
                //    {
                //        case "KeyNames":
                //            XmlNode xnKeyNames = xe.SelectSingleNode("KeyNames");
                //            KeyNamesCol = new KeyNamesInfo(xnKeyNames as XmlElement);

                //            break;
                //        case "FileInfo":
                //            XmlNode xnFileInfo = xe.SelectSingleNode("FileInfo");
                //            FileInfoCol = new FileInfoInfo(xnFileInfo as XmlElement);

                //            break;
                //        case "Coordinate":
                //            XmlNode xnCoordinate = xe.SelectSingleNode("Coordinate");
                //            CoordinateCol = new CoordinateInfo(xnCoordinate as XmlElement);

                //            break;
                //        case "DateTime":
                //            XmlNode xnDateTime = xe.SelectSingleNode("DateTime");
                //            DateTimeCol = new DateTimeInfo(xnDateTime as XmlElement);

                //            break;
                //        case "Render":
                //            XmlNode xnRender = xe.SelectSingleNode("Render");
                //            RenderCol = new RenderInfo(xnRender as XmlElement);

                //            break;
                //        case "Calibration":
                //            XmlNode xnCalibration = xe.SelectSingleNode("Calibration");
                //            CalibrationCol = new CalibrationInfo(xnCalibration as XmlElement);

                //            break;
                //        default:
                //            break;
                //    }
                //}

                //XmlNode xn=xe.SelectSingleNode("KeyNames")
                //return RasterSourceTypeGroupInfo;
            }
            catch (Exception ex)
            { throw ex; }
        }
    }
}
