using System;
using System.IO;
using System.Text.RegularExpressions;

namespace PIE.Meteo.Model
{
    internal class FY23DataSourceInfo : IDataSourceInfo
    {
        public string FilePath
        {
            get;

            set;
        }

        public DateTime? ImageTime
        {
            get;
            set;
        }

        public DateTime? GetImageTime(string filePath)
        {
            FilePath = filePath;
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            ImageTime = GetImageTimeFromFileName(fileName);
            return ImageTime;
        }

        public DateTime? GetImageTimeFromFileName(string fileName)
        {
            //获取日期与时间函数、我们在程序中认为日期为8位数字，出现在时间之前。
            //在获取日期之后对文件名进行裁剪操作。
            string date = "";
            string time = "0000";

            Match match = Regex.Match(fileName, @"_(\d{8})_");
            int? dataStrIndex = null;
            if (match.Groups.Count > 1)
            {
                dataStrIndex = match.Index;
                date = match.Groups[1].Value;
            }
            else
            {
                return null;
            }
            Match match1 = Regex.Match(fileName, @"_(\d{4})_");
            if (match1.Groups.Count > 1)
            {
                if (dataStrIndex != null && dataStrIndex < match1.Index)
                    time = match1.Groups[1].Value;
            }
            //string[] strArr = fileName.Split('_');
            //foreach (var item in strArr)
            //{
            //    System.Text.RegularExpressions.Regex rex = new System.Text.RegularExpressions.Regex(@"^\d+$");
            //    if (rex.IsMatch(item) && item.Length == 4)
            //    {

            //        time = item.ToString();
            //        break;
            //    }
            //}
            return DateTime.ParseExact(date + time, "yyyyMMddHHmm", System.Globalization.CultureInfo.CurrentCulture);
        }
    }
}
