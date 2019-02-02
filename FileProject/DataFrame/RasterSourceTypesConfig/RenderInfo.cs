using System;
using System.Collections.Generic;
using System.Xml;

namespace PIE.Meteo.Model
{
    public class RenderInfo
    {
        public string Defaultband { get; set; }
        public bool IfCalibration { get; set; }
        public bool IfVector { get; set; }
        public List<BandRenderInfo> BandRenderCol { get; private set; } = null;

        public RenderInfo(XmlElement xe)
        {
            try
            {
                if (xe.Attributes["defaultband"] != null)
                {
                    Defaultband = xe.GetAttribute("defaultband").ToString();
                }
                if (xe.Attributes["ifCalibration"] != null)
                {
                    IfCalibration = Convert.ToBoolean(xe.GetAttribute("ifCalibration").ToString());
                }
                if (xe.Attributes["ifVector"] != null)
                {
                    IfVector = Convert.ToBoolean(xe.GetAttribute("ifVector").ToString());
                }

                XmlNodeList xnl = xe.SelectNodes("BandRender");
                BandRenderCol = new List<BandRenderInfo>();
                foreach (var xnode in xnl)
                {
                    BandRenderInfo BandRenderInfo = new BandRenderInfo(xnode as XmlElement);
                    BandRenderCol.Add(BandRenderInfo);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
