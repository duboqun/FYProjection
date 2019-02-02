using System.Xml;

namespace PIE.Meteo.Model
{
    public class FileInfoInfo
    {
        public string MetadataReader { get; set; }

        public FileInfoInfo(XmlElement xe)
        {
            if (xe.Attributes["MetadataReader"] != null)
            {
                MetadataReader = xe.GetAttribute("MetadataReader").ToString();
            }
        }
        //public static FileInfoInfo GetFileInfoInfo(XmlElement xe)
        //{
        //    try
        //    {
        //        FileInfoInfo fileInfo = new FileInfoInfo();
        //        fileInfo.MetadataReader = xe.GetAttribute("MetadataReader").ToString();

        //        return fileInfo;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}
    }
}
