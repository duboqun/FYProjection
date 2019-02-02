using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace PIE.Meteo.FileProject
{
    public static class mXMLHelper
    {
        /// <summary>
        /// 文本化XML反序列化
        /// </summary>
        /// <param name="str">字符串序列</param>
        public static T FromXml<T>(string str)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (XmlReader reader = new XmlTextReader(new StringReader(str)))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static XmlDocument GetXmlDocument(string path)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;
            XmlReader reader = XmlReader.Create(path, settings);
            xmlDoc.Load(reader);
            reader.Close();
            return xmlDoc;
        }

        /// <summary>
        /// 文本化XML序列化
        /// </summary>
        /// <param name="item">对象</param>
        public static string ToXml<T>(T item)
        {
            XmlSerializer serializer = new XmlSerializer(item.GetType());
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;

            using (XmlWriter writer = XmlWriter.Create(sb, xmlWriterSettings))
            {
                serializer.Serialize(writer, item);
                return sb.ToString();
            }
        }

        /// <summary>
        /// xml文件反序列化
        /// </summary>
        /// <typeparam name="T">反序列化对象类型</typeparam>
        /// <param name="path">xml文件路径</param>
        /// <returns></returns>
        public static T FromXmlFile<T>(string path)
        {
            if (!File.Exists(path))
                return default(T);
            var xmlStr = File.ReadAllText(path);
            return mXMLHelper.FromXml<T>(xmlStr);
        }

        public static void ToXmlFile<T>(T item, string path)
        {

            XmlSerializer serializer = new XmlSerializer(item.GetType());
            FileStream stream = null;
            stream = File.Open(path, FileMode.Create);
            if (stream == null) return;
            TextWriter txtWriter = new StreamWriter(stream);
            var setting = new XmlWriterSettings();
            setting.Indent = true;//允许缩进
            using (XmlWriter writer = XmlWriter.Create(txtWriter, setting))
            {
                serializer.Serialize(writer, item);
            }
            txtWriter.Flush();
            txtWriter.Dispose();
        }
    }
}
