using System.Xml;

namespace PIE.Meteo.Model
{
    public class CoordinateInfo
    {
        public string CoordinateType { get; set; }
        public string Wkt { get; set; }
        public string Corners { get; set; }
        public string LocationClass { get; set; }
        public string TransitClass { get; set; }
        public string TransitBand { get; set; }
        public string BandScale { get; set; }
        public CoordinateInfo(XmlElement xe)
        {
            // CoordinateInfo coordinateInfo = new CoordinateInfo();
            if (xe.Attributes["CoordinateType"] != null)
            {
                CoordinateType = xe.GetAttribute("CoordinateType").ToString();
            }
            if (xe.Attributes["wkt"] != null)
            {
                Wkt = xe.GetAttribute("wkt").ToString();
            }
            if (xe.Attributes["corners"] != null)
            {
                Corners = xe.GetAttribute("corners").ToString();
            }
            if (xe.Attributes["locationClass"] != null)
            {
                LocationClass = xe.GetAttribute("locationClass").ToString();
            }
            if (xe.Attributes["transitClass"] != null)
            {
                TransitClass = xe.GetAttribute("transitClass").ToString();
            }
            if (xe.Attributes["transitBand"] != null)
            {
                TransitBand = xe.GetAttribute("transitBand").ToString();
            }
            if (xe.Attributes["bandScale"] != null)
            {
                BandScale = xe.GetAttribute("bandScale").ToString();
            }
        }
    }
}
