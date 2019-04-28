using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIE.Meteo.FileProject
{
    public class NPP_PrjSetting : FilePrjSettings
    {
        public bool IsRad { get; internal set; }
        public bool IsRadRef { get; internal set; } = true;
        public bool IsSensorZenith { get; internal set; }
        public bool IsSolarZenith { get; internal set; }
    }
}
