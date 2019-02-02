using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIE.Meteo.FileProject
{
    public class H08_PrjSetting: FilePrjSettings
    {
        public H08_PrjSetting()
            : base()
        {

        }

        public HSDProcess hsdProcess { get; set; } = null;
        public bool IsRadiation { get; set; } = true;
        public string InfileName { get; set; }
    }
}
