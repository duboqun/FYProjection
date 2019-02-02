using System;
using System.Xml;

namespace PIE.Meteo.Model
{
    public class BandInfo
    {
        public string id
        { get; set; }
        public string fileDataset
        { get; set; }
        public string bandName
        { get; set; }
        public string index
        { get; set; }
        public string invalidValue
        { get; set; }
        public string resolution
        { get; set; }
        public string maxWaveLength
        { get; set; }
        public string minWaveLength
        { get; set; }
        public string middleWaveLength
        { get; set; }

        public BandInfo(XmlElement xe)
        {
            try
            {
                id = xe.GetAttribute("id").ToString();
                if (xe.Attributes["fileDataset"] != null)
                {
                    fileDataset = xe.GetAttribute("fileDataset").ToString();
                }
                bandName = xe.GetAttribute("bandName").ToString();
                index = xe.GetAttribute("index").ToString();
                invalidValue = xe.GetAttribute("invalidValue").ToString();
                resolution = xe.GetAttribute("resolution").ToString();
                maxWaveLength = xe.GetAttribute("maxWaveLength").ToString();
                minWaveLength = xe.GetAttribute("minWaveLength").ToString();
                middleWaveLength = xe.GetAttribute("middleWaveLength").ToString();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
