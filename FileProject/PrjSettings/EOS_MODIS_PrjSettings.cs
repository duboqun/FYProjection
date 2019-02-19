using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PIE.Meteo.FileProject
{
    public class EOS_MODIS_PrjSettings : FilePrjSettings
    {
        private AbstractWarpDataset[] _secondaryOrbitRaster = null;
        private AbstractWarpDataset _locationFile = null;      //定位文件,03文件
        protected bool _isRad = false;              //执行辐射定标
        private bool _isRadRef = true;         ///执行辐射定标并计算反射率辐亮度
        private bool _isSolarZenith = true;       //执行太阳天顶角订正
        private bool _isOutMapTable = false;      //输出原始行列号
        public bool IsSensorZenith = false;

        public EOS_MODIS_PrjSettings()
            : base()
        {
        }

        public AbstractWarpDataset[] SecondaryOrbitRaster
        {
            get { return _secondaryOrbitRaster; }
            set { _secondaryOrbitRaster = value; }
        }

        /// <summary>
        /// 经纬度坐标文件
        /// ***.MOD03.hdf文件
        /// </summary>
        public AbstractWarpDataset LocationFile
        {
            get { return _locationFile; }
            set { _locationFile = value; }
        }

        public bool IsRad
        {
            get { return _isRad; }
            set { _isRad = value; }
        }

        /// <summary>
        /// <value>true</value>:执行亮温计算
        /// <value>false</value>:不执行亮温计算
        /// </summary>
        public bool IsRadRef
        {
            get { return _isRadRef; }
            set { _isRadRef = value; }
        }

        /// <summary>
        /// 是否对反射通道执行太阳高度角订正
        /// </summary>
        public bool IsSolarZenith
        {
            get { return _isSolarZenith; }
            set { _isSolarZenith = value; }
        }

        /// <summary>
        /// 是否输出原始行列号（暂未支持）
        /// </summary>
        public bool IsOutMapTable
        {
            get { return _isOutMapTable; }
            set { _isOutMapTable = value; }
        }
    }
}
