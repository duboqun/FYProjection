using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PIE.Meteo.FileProject
{
    public class FY3_MERSI_PrjSettings:FilePrjSettings
    {
        //第二个要投影的文件，如果为空，则代表只有一个文件。
        protected AbstractWarpDataset _secondaryOrbitRaster = null;      //250米分辨率的数据。
        protected bool _isRad = false;              //执行辐射定标
        protected bool _isRadRef = true;         //执行辐射定标并计算反射率辐亮度
        protected bool _isSolarZenith = true;       //执行太阳天顶角订正
        protected bool _isOutMapTable = false;      //输出原始行列号
        protected bool _isSensorZenith = false;       //临边变暗订正
        private AbstractWarpDataset _geoFile = null;
       
        public FY3_MERSI_PrjSettings()
            : base()
        {
        }

        /// <summary>
        /// 当投影MERSI250米轨道数据时，需设置本文件为对应的1KM轨道数据，用于获取太阳天顶角数据。
        /// 如果设置为空，将不能执行高度角订正
        /// </summary>
        public AbstractWarpDataset SecondaryOrbitRaster
        {
            get { return _secondaryOrbitRaster; }
            set { _secondaryOrbitRaster = value; }
        }

        public bool IsRadRef
        {
            get { return _isRadRef; }
            set { _isRadRef = value; }
        }
        public bool IsRad
        {
            get { return _isRad; }
            set { _isRad = value; }
        }

        public bool IsSolarZenith
        {
            get { return _isSolarZenith; }
            set { _isSolarZenith = value; }
        }

        public bool IsOutMapTable
        {
            get { return _isOutMapTable; }
            set { _isOutMapTable = value; }
        }

        public bool IsSensorZenith
        {
            get { return _isSensorZenith; }
            set { _isSensorZenith = value; }
        }

        /// <summary>
        /// 执行地形校正
        /// </summary>
        public bool IsTerrainCorrection { get; set; } = false;
        /// <summary>
        /// 2013年9月24日添加属性
        /// 定位文件,目前仅对FY3C有效
        /// </summary>
        public AbstractWarpDataset GeoFile
        {
            get { return _geoFile; }
            set { _geoFile = value; }
        }

        public AbstractWarpDataset AngleFile { get; set; }
    }
}
