using System.Xml;

namespace PIE.Meteo.Model
{
    public class BandCalibrationInfo
    {
        public string BandID { get; set; }
        public string CalibrationTable { get; set; }

        public BandCalibrationInfo(XmlElement xe)
        {
            if (xe.Attributes["bandID"] != null)
            {
                BandID = xe.GetAttribute("bandID").ToString();
            }
            if (xe.Attributes["calibrationTable"] != null)
            {
                CalibrationTable = xe.GetAttribute("calibrationTable").ToString();
            }
        }
    }
}
