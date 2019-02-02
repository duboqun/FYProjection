using System;
using System.Collections.Generic;
using System.Xml;

namespace PIE.Meteo.Model
{
    public class CalibrationInfo
    {
        public string CalibrationClass { get; set; }

        public List<BandCalibrationInfo> BandCalibrationCol { get; private set; }
        public CalibrationInfo(XmlElement xe)
        {
            try
            {
                CalibrationClass = xe.GetAttribute("calibrationClass").ToString();

                XmlNodeList xnl = xe.SelectNodes("BandCalibration");
                if (xnl.Count > 0)
                {
                    BandCalibrationCol = new List<BandCalibrationInfo>();
                    foreach (var xnode in xnl)
                    {
                        BandCalibrationInfo BandCalibrationInfo = new BandCalibrationInfo(xnode as XmlElement);
                        BandCalibrationCol.Add(BandCalibrationInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
