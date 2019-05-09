using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using PIE.Meteo.FileProject;

namespace PIE.Meteo.Model
{
    public class mRasterSourceManager
    {
        private string[] REGIONREGEXSTR = new string[]
        {
            @"_(?<region>洞庭湖流域)",
            @"_(?<region>鄱阳湖流域)",
            @"_(?<region>0S\d{2}|S\d{2})_",
            @"_(?<region>0F\S{2}|F\S{2})_",
            @"_(?<region>EA)_",
            @"_(?<region>EB)_",
            @"\S*_(?<region>\S*[\u4E00-\u9FA5]\d*)_", //匹配中文
            @"_(?<region>DXX\d*)_"
        };

        private string[] TIMEZONES = new string[] {@"_TZ\(UTC(?<TimeSpan>\S*)\)(?<TimeZone>\S*)\.\S+"};
        private string workpath = "";
        public List<RasterDatasetInfo> RasterDatasetInfoCol { get; set; }
        public RasterSourceTypesInfo RasterSourceTypesInfo { get; set; }

        public Dictionary<string, string> IdentifysDic { get; set; }

        //private const int Time = 8; 
        private const int Time = 0; //用户要求保持世界时


        private static mRasterSourceManager s_Instance = null;

        public static mRasterSourceManager GetInstance()
        {
            if (s_Instance == null)
            {
                s_Instance = new mRasterSourceManager();
            }

            return s_Instance;
        }

        private mRasterSourceManager()
        {
            Console.WriteLine($"读取卫星传感器配置文件，目录：{Directory.GetCurrentDirectory()}");
            LoadConfigInfo(Directory.GetCurrentDirectory());
        }

        private void LoadConfigInfo(string workPath)
        {
            try
            {
                workpath = workPath;
                string rasterSourceTypesConfigPath =
                    Path.Combine(workPath, "Config", "RasterSourceTypesConfig.xml");
                if (File.Exists(rasterSourceTypesConfigPath))
                {
                    XmlDocument xmlDoc = mXMLHelper.GetXmlDocument(rasterSourceTypesConfigPath);
                    XmlNode xn = xmlDoc.SelectSingleNode("RasterSourceTypes");
                    RasterSourceTypesInfo = new RasterSourceTypesInfo(xn as XmlElement);
                }

                string rasterDatasetsConfigPath = Path.Combine(workPath, "Config", "RasterDatasetsConfig.xml");
                if (File.Exists(rasterDatasetsConfigPath))
                {
                    XmlDocument xmlDoc = mXMLHelper.GetXmlDocument(rasterDatasetsConfigPath);
                    XmlNode xn = xmlDoc.SelectSingleNode("RasterDatasets");
                    XmlNodeList xnl = xn.ChildNodes;
                    RasterDatasetInfoCol = new List<RasterDatasetInfo>();
                    foreach (XmlNode xnode in xnl)
                    {
                        XmlElement xe = (XmlElement) xnode;
                        RasterDatasetInfo rasterDatasetInfo = new RasterDatasetInfo(xe);
                        RasterDatasetInfoCol.Add(rasterDatasetInfo);
                    }
                }

                string identifyListsConfigPath = Path.Combine(workPath, "Config", "IdentifyListsConfig.xml");
                if (File.Exists(identifyListsConfigPath))
                {
                    XmlDocument xmlDoc = mXMLHelper.GetXmlDocument(identifyListsConfigPath);
                    XmlNode xn = xmlDoc.SelectSingleNode("Config");
                    XmlNodeList xnl = xn.ChildNodes;
                    IdentifysDic = new Dictionary<string, string>();
                    foreach (XmlNode xnode in xnl)
                    {
                        XmlElement xe = (XmlElement) xnode;
                        IdentifysDic.Add(xe.Name.ToString(), xe.GetAttribute("identify").ToString());
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// 根据文件名提取文件节点信息
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <returns></returns>
        public IdentifyDataCls GetIdentifyCls(string fileName)
        {
            try
            {
                string filePath = Path.GetFileName(fileName);

                IdentifyDataCls identifyDataCls = new IdentifyDataCls();
                if (IdentifysDic == null || IdentifysDic.Count < 1)
                {
                    return null;
                }

                identifyDataCls.Suffix = Path.GetExtension(filePath);
                identifyDataCls.ImageTime = GetImageTime(filePath);
                if (identifyDataCls.ImageTime == null)
                {
                    identifyDataCls.ImageTime = GetOrbitDateTime(Path.GetFileName(fileName));
                }
                //RasterDatasetInfo info = GetRasterDatasetInfo(filePath);
                //if (info != null && info.BandCol != null && info.BandCol.Count > 0)
                //    identifyDataCls.Resolution = info.BandCol[0].resolution;
                //else
                //    identifyDataCls.Resolution = "";

                List<string> lists = Path.GetFileNameWithoutExtension(filePath).ToUpper().Split('_').ToList();
                List<string> newlists = new List<string>();

                foreach (var item in lists)
                {
                    if (item.Contains("--"))
                        newlists.Add(item.Replace("--", ""));
                    else if (item.Contains("-"))
                        newlists.Add(item.Replace("-", ""));
                    else
                        newlists.Add(item);
                }

                foreach (var item in IdentifysDic)
                {
                    var value = item.Value.ToUpper().Split(',').ToList();
                    switch (item.Key)
                    {
                        case "Satellites":
                            identifyDataCls.Satellite = GetEqualValue(newlists, value);
                            break;
                        case "Sensors":
                            identifyDataCls.Sensor = GetEqualValue(newlists, value);
                            break;
                        case "Regions":
                            identifyDataCls.Region = GetRegion(filePath, REGIONREGEXSTR);
                            break;
                        case "Resolutions":
                            identifyDataCls.Resolution = GetEqualValue(newlists, value);
                            break;
                        case "Products":
                            identifyDataCls.Product = GetEqualValue(newlists, value);
                            break;
                        case "SubProducts":
                            identifyDataCls.SubProduct = GetEqualValue(newlists, value);
                            break;
                        case "Levels":
                            identifyDataCls.Level = GetEqualValue(newlists, value);
                            break;
                        default:
                            break;
                    }
                }

                if (string.IsNullOrEmpty(identifyDataCls.Satellite) || string.IsNullOrEmpty(identifyDataCls.Sensor))
                {
                    if (filePath.Contains("AQUA"))
                    {
                        identifyDataCls.Satellite = "EOSA";
                        identifyDataCls.Sensor = "MODIS";
                    }
                    else if (filePath.Contains("TERRA"))
                    {
                        identifyDataCls.Satellite = "EOST";
                        identifyDataCls.Sensor = "MODIS";
                    }
                }

                if (string.IsNullOrEmpty(identifyDataCls.Resolution))
                {
                    identifyDataCls.Resolution = GetResolution(identifyDataCls.Satellite + identifyDataCls.Sensor);
                }

                return identifyDataCls;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetIdentifyCls({fileName}) ex:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取卫星时间
        /// </summary>
        /// <param name="fname"></param>
        /// <returns></returns>
        private DateTime? GetOrbitDateTime(string fname)
        {
            //日期
            string exp = @"_(?<year>\d{4})_(?<month>\d{2})_(?<day>\d{2})_(?<hour>\d{2})_(?<monutes>\d{2})";
            Match m = Regex.Match(fname, exp);
            if (m.Success)
            {
                try
                {
                    return DateTime.Parse(
                        m.Groups["year"].Value + "-" +
                        m.Groups["month"].Value + "-" +
                        m.Groups["day"].Value + " " +
                        m.Groups["hour"].Value + ":" +
                        m.Groups["monutes"].Value);
                }
                catch
                {
                }
            }

            return null;
        }

        /// <summary>
        /// 获取GF卫星分辨率
        /// </summary>
        /// <param name="satsen">卫星+载荷</param>
        /// <returns></returns>
        private string GetResolution(string satsen)
        {
            FileInfo ResolutionDir = new FileInfo(Path.Combine(workpath, "SystemData\\GetResolution.xml"));
            XmlDocument doc = mXMLHelper.GetXmlDocument(ResolutionDir.FullName);
            XmlNodeList xnl = doc.SelectSingleNode("Config").SelectNodes("Satellite");
            if (xnl == null)
            {
                return null;
            }

            foreach (XmlNode xn in xnl)
            {
                XmlElement xe = xn as XmlElement;
                if (xe.GetAttribute("Identity").ToUpper().Contains(satsen.ToUpper()))
                    return xe.GetAttribute("Resolution");
            }

            return null;
        }

        /// <summary>
        /// 获取区域信息
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="value">区域信息正则表达式</param>
        /// <returns></returns>
        private string GetRegion(string fileName, string[] values)
        {
            //string[] values = value.Split(',');
            string region = "";
            if (values.Length > 0)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    Match m = Regex.Match(Path.GetFileName(fileName), values[i]);
                    if (m.Success)
                    {
                        return m.Groups["region"].Value.Replace("\\", "");
                    }
                }
            }

            return region;
        }

        /// <summary>
        /// 获取 A∩B 集合A与集合B的交集
        /// </summary>
        /// <param name="listFileName">集合</param>
        /// <param name="listToCompare">被交的集合</param>
        /// <returns></returns>
        private string GetEqualValue(List<string> listFileName, List<string> listToCompare)
        {
            IEnumerable<string> en = listFileName.Intersect(listToCompare); // A∩B 集合A与集合B的交集
            if (en != null && en.Count() > 0)
            {
                return en.First();
            }

            return null;
        }

        /// <summary>
        /// 通过输入文件获取其相应卫星
        /// </summary>
        /// <param name="inputfilePath">输入文件名</param>
        /// <returns></returns>
        public RasterDatasetInfo GetRasterDatasetInfo(string inputfilePath)
        {
            try
            {
                string upperFileName = Path.GetFileNameWithoutExtension(inputfilePath).ToUpper();
                RasterSourceTypeSingleInfo single = GetInputfileRasterSourceInfo(inputfilePath);
                if (single != null)
                {
                    var dataSet = from item in RasterDatasetInfoCol
                        where item.ID.ToUpper().Equals(single.datasetsID.ToUpper())
                        select item;
                    if (dataSet != null && dataSet.FirstOrDefault() != null)
                    {
                        return dataSet.FirstOrDefault();
                    }
                }
                else
                {
                    foreach (var rasterDataset in RasterDatasetInfoCol)
                    {
                        string[] singleKeys = rasterDataset.ID.ToUpper().Split('-');
                        int count = 0;
                        if (singleKeys != null && singleKeys.Length > 0)
                        {
                            for (int i = 0; i < singleKeys.Length; i++)
                            {
                                if (upperFileName.Contains(singleKeys[i]))
                                {
                                    count++;
                                }
                            }
                        }

                        if (count == singleKeys.Length)
                        {
                            return rasterDataset;
                        }
                    }
                }

                Console.WriteLine(
                    string.Format("Info :GetRasterDatasetInfo()\r\n   InputfilePath:{0}", inputfilePath));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("RasterSourceManager.GetRasterDatasetInfo()", inputfilePath), ex);
            }

            return null;
        }

        /// <summary>
        /// 通过输入文件获取其相应卫星 数据源
        /// </summary>
        /// <param name="inputfilePath">输入文件全路径或文件名</param>
        /// <returns></returns>
        public RasterSourceTypeSingleInfo GetInputfileRasterSourceInfo(string inputfilePath)
        {
            try
            {
                string suffix = Path.GetExtension(inputfilePath).Replace(".", "").ToUpper();
                foreach (var single in RasterSourceTypesInfo.RasterSourceTypeSingleCol)
                {
                    string[] keys = single.KeyNamesCol.Key.Split(',');
                    int count = 0;
                    //判断文件名是否包含所有主键以及后缀名是否匹配
                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (inputfilePath.ToUpper().Contains(keys[i].ToUpper()))
                        {
                            count++;
                        }
                    }

                    if ((count == keys.Length) && single.KeyNamesCol.Suffix.ToUpper().Contains(suffix.ToUpper()))
                    {
                        return single;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("RasterSourceManager.GetInputfileRasterSourceInfo()", inputfilePath), ex);
            }

            return null;
        }

        /// <summary>
        /// 根据波段类型获取波段号（如果不存在则返回-1）
        /// </summary>
        /// <param name="info"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public int TryGetBandNum(RasterDatasetInfo info, string name)
        {
            int num = -1;
            for (int i = 0; i < info.BandCol.Count; i++)
            {
                var bandInfo = info.BandCol[i];

                if (!string.IsNullOrEmpty(bandInfo.bandName))
                {
                    if (name == bandInfo.bandName)
                    {
                        num = Convert.ToInt32(bandInfo.id);
                        return num;
                    }
                }
            }

            return num;
        }


        /// <summary>
        /// 根据波段中心波长获取波段号（如果不存在则返回-1）
        /// </summary>
        /// <param name="info"></param>
        /// <param name="waveLength"></param>
        /// <returns></returns>
        public int TryGetBandNum(RasterDatasetInfo info, double waveLength)
        {
            int num = -1;
            if (info != null)
            {
                Dictionary<int, double> bandInfoDic = new Dictionary<int, double>();
                for (int i = 0; i < info.BandCol.Count; i++)
                {
                    var bandInfo = info.BandCol[i];
                    if (!string.IsNullOrEmpty(bandInfo.middleWaveLength))
                    {
                        bandInfoDic.Add(Convert.ToInt32(bandInfo.id),
                            Math.Abs(Convert.ToDouble(bandInfo.middleWaveLength) - waveLength));
                    }
                }

                if (bandInfoDic.Count > 1)
                {
                    num = bandInfoDic.OrderBy(t => t.Value).First().Key;
                }
            }

            return num;
        }

        /// <summary>
        /// 获取输入影像时间字符串
        /// </summary>
        /// <param name="inputFilePath">输入影像文件路径</param>
        /// <returns></returns>
        [Obsolete]
        public string GetImageDateName(string inputFilePath)
        {
            try
            {
                string path = inputFilePath.ToString();
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName.Contains("GF"))
                {
                    Match match = Regex.Match(fileName, @"_(\d{8})_");
                    if (match.Groups.Count > 1)
                    {
                        return match.Groups[1].Value;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                else if (fileName.ToUpper().Contains("FY2E") || fileName.ToUpper().Contains("FY2F") ||
                         fileName.ToUpper().Contains("FY3A") || fileName.ToUpper().Contains("FY3B") ||
                         fileName.ToUpper().Contains("FY3C"))
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
                        return string.Empty;
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

                    return date + time;
                }
                else if (fileName.ToUpper().Contains("FY4A"))
                {
                    Match match = Regex.Match(fileName, @"_(\d{14})_");
                    if (match.Groups.Count > 1)
                    {
                        return match.Groups[1].Value;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                else if (fileName.ToUpper().Contains("HJ"))
                {
                    Match match = Regex.Match(fileName, @"-(\d{8})-");
                    if (match.Groups.Count > 1)
                    {
                        return match.Groups[1].Value;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    string.Format("RasterSourceManager.GetImageDateName({0})", Path.GetFileName(inputFilePath)), ex);
            }

            return string.Empty;
        }

        /// <summary>
        /// 输入路径，返回图像时间信息（世界时）
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <returns></returns>
        public DateTime? GetImageTime(string inputFilePath)
        {
            try
            {
                DateTime? imageTime = null;
                IDataSourceInfo dataSourceInfo = null;
                string path = inputFilePath.ToString();
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName.Contains("GF"))
                {
                    dataSourceInfo = new GFDataSourceInfo();
                }
                else if (fileName.ToUpper().Contains("FY2E") || fileName.ToUpper().Contains("FY2F") ||
                         fileName.ToUpper().Contains("FY3A") || fileName.ToUpper().Contains("FY3B") ||
                         fileName.ToUpper().Contains("FY3C") || fileName.ToUpper().Contains("FY3D") ||
                         fileName.ToUpper().Contains("HS_H08"))
                {
                    dataSourceInfo = new FY23DataSourceInfo();
                }
                else if (fileName.ToUpper().Contains("FY4A"))
                {
                    dataSourceInfo = new FY4ADataSourceInfo();
                }
                else if (fileName.ToUpper().Contains("HJ"))
                {
                    dataSourceInfo = new HJDataSourceInfo();
                }
                else if (fileName.ToUpper().Contains("MOD"))
                {
                    dataSourceInfo = new ModisDataSourceInfo();
                }
                else if(fileName.ToUpper().Contains("NPP"))
                {
                    dataSourceInfo = new NppDataSourceInfo();
                }
                else if(fileName.ToUpper().Contains("AVHRR"))
                {
                    dataSourceInfo = new NoaaDataSourceInfo();
                }
                else
                {
                    return null;
                }

                imageTime = dataSourceInfo?.GetImageTime(inputFilePath);
                if (imageTime != null)
                {
                    DateTime dt = (DateTime) imageTime;
                    imageTime = dt.AddHours(Time);
                }

                return imageTime;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("RasterSourceManager.GetImageInfo({0})", Path.GetFileName(inputFilePath)),
                    ex);
            }

            return null;
        }

        /// <summary>
        /// 获取产品数据时间
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public DateTime? GetProductImageTime(string fileName)
        {
            DateTime? dateTime = null;
            try
            {
                string file = Path.GetFileNameWithoutExtension(fileName);
                List<string> list = file.Split('_').ToList();
                if (list.Count <= 1) return null;
                string time = list[list.Count - 1].PadRight(14, '0');
                dateTime = DateTime.ParseExact(time, "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, ex);
                return null;
            }

            return dateTime;
        }

        /// <summary>
        /// 获取配置文件中可识别文件的后缀名
        /// </summary>
        /// <returns></returns>
        public string GetFilterStr()
        {
            string filter = "Raster Files | ";
            foreach (var item in this.RasterSourceTypesInfo?.RasterSourceTypeSingleCol)
            {
                string str = item.KeyNamesCol.Suffix;
                string[] strArr = str.Split(',');

                for (int i = 0; i < strArr.Length; i++)
                {
                    if (strArr[i].ToUpper().Contains("XML"))
                        continue;
                    if (!filter.ToUpper().Contains(strArr[i].ToString().ToUpper()))
                    {
                        filter += "*." + strArr[i].ToString() + ";";
                    }
                }
            }

            filter = filter + "*.tif;*.img" +
                     "|Shape Files|*.shp;*.000|环境星数据|*.xml|气象卫星自动化产品|*.awx;*.hdf;*.nc|所有格式|*.*";
            return filter;
        }
    }
}