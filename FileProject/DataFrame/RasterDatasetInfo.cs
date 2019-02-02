using System;
using System.Collections.Generic;
using System.Xml;

namespace PIE.Meteo.Model
{
    public class RasterDatasetInfo
    {

        public string ID
        { get; set; }
        public string SatelliteID
        { get; set; }
        public string SensorID
        { get; set; }
        public string SatelliteDescribe
        { get; set; }
        public string SensorDescribe
        { get; set; }
        public List<BandInfo> BandCol
        { get; set; }

        public RasterDatasetInfo(XmlElement xe)
        {
            try
            {
                ID = xe.GetAttribute("ID").ToString();
                SatelliteID = xe.GetAttribute("SatelliteID").ToString();
                SensorID = xe.GetAttribute("SensorID").ToString();
                SatelliteDescribe = xe.GetAttribute("Satellitedescribe").ToString();
                SensorDescribe = xe.GetAttribute("Sensordescribe").ToString();

                XmlNodeList xnl = xe.SelectNodes("Band");
                BandCol = new List<BandInfo>();
                foreach (var xnode in xnl)
                {
                    BandInfo BandInfo = new BandInfo(xnode as XmlElement);
                    BandCol.Add(BandInfo);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
