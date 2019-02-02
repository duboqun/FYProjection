using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using PIE.Meteo.FileProject;

namespace PIE.Meteo.Model
{
    internal class GFDataSourceInfo : IDataSourceInfo
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
            try
            {
                FilePath = filePath;
                string folderPath = Path.GetDirectoryName(filePath);
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string headerPath = Path.Combine(folderPath, fileName + ".xml");
                if (!File.Exists(headerPath))
                {
                    ImageTime = GetImageTimeFromFileName(fileName);
                }
                else
                {
                    ImageTime = GetImageTimeFromHeader(headerPath);
                }
                return ImageTime;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public DateTime? GetImageTimeFromHeader(string filePath)
        {
            XmlDocument xmlDoc = mXMLHelper.GetXmlDocument(filePath);
            XmlNode xn = xmlDoc.SelectSingleNode("ProductMetaData").SelectSingleNode("StartTime");
            string dateTime = xn.InnerText;
            DateTime dt;
            if (DateTime.TryParse(dateTime, out dt))
            {
                return dt;
            }
            else
                return null;
            // return DateTime.ParseExact(dateTime, "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);
        }
        public DateTime? GetImageTimeFromFileName(string fileName)
        {
            Match match = Regex.Match(fileName, @"_(\d{8})_");
            if (match.Groups.Count > 1)
            {
                return DateTime.ParseExact(match.Groups[1].Value, "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
            }
            else
            {
                return null;
            }
        }
    }
}
