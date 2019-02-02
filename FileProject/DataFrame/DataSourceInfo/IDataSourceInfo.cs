using System;

namespace PIE.Meteo.Model
{

    public interface IDataSourceInfo
    {      
        /// <summary>
        /// 数据时间
        /// </summary>
        DateTime? ImageTime { get; set; }
        /// <summary>
        /// 获取影像数据时间
        /// </summary>
        /// <param name="filePath">数据路径</param>
        /// <returns></returns>
        DateTime? GetImageTime(string filePath);      
    }
}
