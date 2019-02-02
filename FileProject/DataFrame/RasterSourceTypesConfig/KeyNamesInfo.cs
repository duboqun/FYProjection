using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace PIE.Meteo.Model
{
    public class KeyNamesInfo
    {
        public string SourceID { get; set; }
        public string SourceKey { get; set; }
        public string Key { get; set; }
        public string Suffix { get; set; }
        public string OutPutKey { get; set; }
        public KeyNamesInfo(XmlElement xe)
        {
            if (xe.Attributes["sourceID"] != null)
            {
                SourceID = xe.GetAttribute("sourceID").ToString();
            }
            if (xe.Attributes["key"] != null && xe.Attributes["sourceID"] == null)
            {
                Key = xe.GetAttribute("key").ToString();
            }
            if (xe.Attributes["outPutKey"] != null)
            {
                OutPutKey = xe.GetAttribute("outPutKey").ToString();
            }
            //将产品类型（投影等）datasourcetype中的sourceid转换成对应卫星数据的key
            if (xe.Attributes["key"] != null && xe.Attributes["sourceID"] != null)
            {
                //if (xe.GetAttribute("key") == "") return;
                XmlNode xn = xe.ParentNode.ParentNode;
                XmlNodeList xnl = xe.ParentNode.ParentNode.SelectNodes("RasterSourceType");
                foreach (var xnode in xnl)
                {
                    XmlElement xe1 = xnode as XmlElement;
                    if (xe1.Attributes["ID"] != null && SourceID == xe1.GetAttribute("ID"))
                    {
                        XmlNode xnKeyNames = xe1.SelectSingleNode("KeyNames");
                        XmlElement xeKeyNames = xnKeyNames as XmlElement;
                        if (xeKeyNames != null && xeKeyNames.Attributes["key"] != null)
                        {
                            SourceKey = xeKeyNames.GetAttribute("key").ToString();
                            break;
                        }
                    }
                }
                if (SourceKey != "" && xe.GetAttribute("key") == "")
                {
                    Key = SourceKey;
                }
                else if (SourceKey != "" && xe.GetAttribute("key") != "")
                {
                    Key = SourceKey + "," + xe.GetAttribute("key");
                }
            }
            if (xe.Attributes["key"] == null && xe.Attributes["sourceID"] != null)
            {
                XmlNode xn = xe.ParentNode.ParentNode;
                XmlNodeList xnl = xe.ParentNode.ParentNode.SelectNodes("RasterSourceType");
                foreach (var xnode in xnl)
                {
                    XmlElement xe1 = xnode as XmlElement;
                    if (xe1.Attributes["ID"] != null && SourceID == xe1.GetAttribute("ID"))
                    {
                        XmlNode xnKeyNames = xe1.SelectSingleNode("KeyNames");
                        XmlElement xeKeyNames = xnKeyNames as XmlElement;
                        if (xeKeyNames != null && xeKeyNames.Attributes["key"] != null)
                        {
                            SourceKey = xeKeyNames.GetAttribute("key").ToString();
                            break;
                        }
                    }
                }

                if (SourceKey != null && OutPutKey != null && OutPutKey != "")
                {
                    string result = "";
                    List<string> sourceKeyArr = SourceKey.Split(',').ToList();
                    string[] outPutKeyArr = OutPutKey.Split(',');
                    foreach (var op in outPutKeyArr)
                    {
                        string str1 = op.Substring(0, op.IndexOf('('));
                        string str2 = op.Substring(op.IndexOf('(') + 1, op.IndexOf(')') - op.IndexOf('(') - 1);
                        if (str2.Contains("+"))
                        {
                            //int i = Convert.ToInt32(str2.Replace("+", ""));
                            sourceKeyArr.Add(str1);
                            continue;

                        }
                        if (str2.Contains("-"))
                        {
                            int i = Convert.ToInt32(str2.Replace("-", ""));
                            string delStr = sourceKeyArr[i];
                            if (sourceKeyArr.Contains(delStr))
                            {

                                sourceKeyArr.RemoveAt(i);
                            }

                            continue;
                        }
                        if (!str2.Contains("+") && !str2.Contains("-") && (Convert.ToInt32(str2) < 100) && Convert.ToInt32(str2) < sourceKeyArr.Count)
                        {
                            int i = Convert.ToInt32(str2);
                            if (i < sourceKeyArr.Count - 1 && i > 0)
                            {

                                sourceKeyArr[i] = str1;
                            }

                            continue;
                        }
                        if (Convert.ToInt32(str2) == 100)
                        {
                            sourceKeyArr.Add(str1);
                            continue;
                        }

                    }
                    for (int i = 0; i < sourceKeyArr.Count - 1; i++)
                    {
                        result += sourceKeyArr[i] + ",";
                    }
                    result = result + sourceKeyArr[sourceKeyArr.Count - 1];
                    Key = result;
                }

            }
            if (SourceKey != null && (OutPutKey == null || OutPutKey == "") && xe.Attributes["key"] == null)
            {
                Key = SourceKey;
            }
            if (xe.Attributes["suffix"] != null)
            {
                Suffix = xe.GetAttribute("suffix").ToString();
            }
        }

        //public static KeyNamesInfo GetKeyNamesInfo(XmlElement xe)
        //{
        //    try
        //    {
        //        KeyNamesInfo keyName = new KeyNamesInfo();
        //        if(xe.Attributes["sourceID"]!=null)
        //        {
        //            keyName.SourceID = xe.GetAttribute("sourceID").ToString();
        //        }
        //        keyName.Key = xe.GetAttribute("key").ToString();
        //        keyName.Suffix = xe.GetAttribute("suffix").ToString();
        //        return keyName;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}
    }
}
