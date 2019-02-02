using PIE.Meteo.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PIE.Meteo.FileProject
{
    public class HDF4FilePrjSettings : FilePrjSettings
    {
         protected AbstractWarpDataset _secondaryOrbitRaster = null;      //250米分辨率的数据。

         public HDF4FilePrjSettings()
            : base()
        {
        }

        /// <summary>
        /// 经纬度数据集所在Raster
        /// </summary>
        public AbstractWarpDataset LocationFile
        {
            get { return _secondaryOrbitRaster; }
            set { _secondaryOrbitRaster = value; }
        }
    }
}
