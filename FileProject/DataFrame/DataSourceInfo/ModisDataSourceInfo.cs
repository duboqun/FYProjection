using System;
using System.IO;
using System.Text.RegularExpressions;

namespace PIE.Meteo.Model
{
    internal class ModisDataSourceInfo : IDataSourceInfo
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
            if (ImageTime == null)
            {
                ImageTime = GetImageTimeFromFileName2(fileName);
            }
            return ImageTime;
        }

        private DateTime? GetImageTimeFromFileName2(string fileName)
        {
            DateTime? result = null;
            string regTxt = @"_(\d{4})_(\d{2})_(\d{2})_(\d{2})_(\d{2})";
            if (Regex.IsMatch(fileName, regTxt))
            {
                Match mch = Regex.Match(fileName, regTxt);
                string timeStr = mch.Groups[1].Value + mch.Groups[2].Value + mch.Groups[3].Value + mch.Groups[4].Value + mch.Groups[5].Value;
                result = DateTime.ParseExact(timeStr, "yyyyMMddHHmm", System.Globalization.CultureInfo.CurrentCulture);
            }
            return result;
        }

        public DateTime? GetImageTimeFromFileName(string fileName)
        {
            string date = "";
            string time = "";

            Match match = Regex.Match(fileName, @"_(\d{8})_");

            if (match.Groups.Count > 1)
            {
                date = match.Groups[1].Value;
            }
            else
            {
                return null;
            }
            Match match1 = Regex.Match(fileName, @"_(\d{4})_");
            string[] strArr = fileName.Split('_');
            foreach (var item in strArr)
            {
                System.Text.RegularExpressions.Regex rex = new System.Text.RegularExpressions.Regex(@"^\d+$");
                if (rex.IsMatch(item) && item.Length == 4)
                {
                    time = item.ToString();
                    break;
                }
            }
            return DateTime.ParseExact(date + time, "yyyyMMddHHmm", System.Globalization.CultureInfo.CurrentCulture);
        }
    }
}
