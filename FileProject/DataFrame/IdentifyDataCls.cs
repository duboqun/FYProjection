using System;

namespace PIE.Meteo.Model
{
    /// <summary>
    /// 数据识别类
    /// </summary>
    public class IdentifyDataCls
    {
        /// <summary>
        /// 卫星
        /// </summary>
        public string Satellite { get; set; }
        /// <summary>
        /// 载荷
        /// </summary>
        public string Sensor { get; set; }
        /// <summary>
        /// 产品
        /// </summary>
        public string Product { get; set; }
        /// <summary>
        /// 子产品
        /// </summary>
        public string SubProduct { get; set; }
        /// <summary>
        /// 区域
        /// </summary>
        public string Region { get; set; }
        /// <summary>
        /// 分辨率
        /// </summary>
        public string Resolution { get; set; }

        /// <summary>
        /// 产品级别
        /// </summary>
        public string Level { get; set; }
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime? ImageTime { get; set; } 
        /// <summary>
        /// 后缀
        /// </summary>
        public string Suffix { get; set; }
    }
}
