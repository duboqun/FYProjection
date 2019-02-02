using System.Xml;

namespace PIE.Meteo.Model
{
    public class BandRenderInfo
    {
        public string BandID { get; set; }
        public string ColorClass { get; set; }
        public string ColorTable { get; set; }
        public double RenderMin { get; set; } = 0;
        public double RenderMax { get; set; } = 0;
        
        public BandRenderInfo(XmlElement xe)
        {
            double result;
            if (xe.Attributes["bandID"] != null)
            {
                BandID = xe.GetAttribute("bandID").ToString();
            }
            if (xe.Attributes["colorClass"] != null)
            {
                ColorClass = xe.GetAttribute("colorClass").ToString();
            }

            if (xe.Attributes["colorTable"] != null)
            {
                ColorTable = xe.GetAttribute("colorTable").ToString();

            }
            if (xe.Attributes["rendermin"] != null)
            {
                RenderMin = double.TryParse(xe.GetAttribute("rendermin").ToString(), out result) ? result : 0;
            }

            if (xe.Attributes["rendermax"] != null)
            {
                RenderMax = double.TryParse(xe.GetAttribute("rendermax").ToString(), out result) ? result : 0;

            }
        }
    }
}
