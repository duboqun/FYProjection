using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PIE.Meteo.Model
{
    public class DateTimeInfo
    {
        public string DateIndexstart
        { get; set; }
        public string DateIndexend
        { get; set; }
        public string DateIndexformat
        { get; set; }
        public string TimeIndexstart
        { get; set; }
        public string TimeIndexend
        { get; set; }
        public string TimeIndexformat
        { get; set; }
        public string TimeZone
        { get; set; }

        public DateTimeInfo(XmlElement xe)
        {
            if (xe.Attributes["DateIndexstart"] != null)
            {
                DateIndexstart = xe.GetAttribute("DateIndexstart").ToString();
            }
            if (xe.Attributes["DateIndexend"] != null)
            {
                DateIndexend = xe.GetAttribute("DateIndexend").ToString();
            }
            if (xe.Attributes["DateIndexformat"] != null)
            {
                DateIndexformat = xe.GetAttribute("DateIndexformat").ToString();
            }
            if (xe.Attributes["TimeIndexstart"] != null)
            {
                TimeIndexstart = xe.GetAttribute("TimeIndexstart").ToString();
            }
            if (xe.Attributes["TimeIndexend"] != null)
            {
                TimeIndexend = xe.GetAttribute("TimeIndexend").ToString();
            }
            if (xe.Attributes["TimeIndexformat"] != null)
            {
                TimeIndexformat = xe.GetAttribute("TimeIndexformat").ToString();
            }
            if (xe.Attributes["TimeZone"] != null)
            {
                TimeZone = xe.GetAttribute("TimeZone").ToString();
            }
        }
    }
}
