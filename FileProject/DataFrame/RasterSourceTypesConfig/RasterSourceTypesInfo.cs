using System.Collections.Generic;
using System.Xml;

namespace PIE.Meteo.Model
{
    public class RasterSourceTypesInfo
    {
        public string Describe
        { get; set; }
        public List<RasterSourceTypeSingleInfo> RasterSourceTypeSingleCol
        { get; set; }
        public List<RasterSourceTypeGroupInfo> RasterSourceTypeGroupCol
        { get; set; }
        public RasterSourceTypesInfo(XmlElement xe)
        {
            Describe = xe.GetAttribute("describe").ToString();
            XmlNodeList xnl = xe.SelectNodes("RasterSourceType");
            List<RasterSourceTypeSingleInfo> RasterSourceTypeSingles = new List<RasterSourceTypeSingleInfo>();
            List<RasterSourceTypeGroupInfo> RasterSourceTypeGroups = new List<RasterSourceTypeGroupInfo>();
            foreach (var xnode in xnl)
            {
                XmlElement xmlElement = xnode as XmlElement;
                if (xmlElement.Attributes["datasetsID"] != null)
                {
                    RasterSourceTypeSingleInfo RasterSourceTypeSingleInfo = new RasterSourceTypeSingleInfo(xmlElement);
                    RasterSourceTypeSingles.Add(RasterSourceTypeSingleInfo);
                }
                else if (xmlElement.Attributes["groupids"] != null)
                {
                    RasterSourceTypeGroupInfo RasterSourceTypeGroupInfo = new RasterSourceTypeGroupInfo(xmlElement);
                    RasterSourceTypeGroups.Add(RasterSourceTypeGroupInfo);
                    //string groupids = xmlElement.GetAttribute("groupids").ToString();
                    //string[] groupArr = groupids.Split(',');
                    //if (groupArr.Length > 0)
                    //{
                    //    string firstStr = groupArr[0];
                    //    XmlNode xnParent = xmlElement.ParentNode;
                    //    XmlNodeList xnChilds = xnParent.ChildNodes;
                    //    foreach (var item in xnChilds)
                    //    {
                    //        XmlElement xe1 = item as XmlElement;
                    //        //for (int i = 0; i < groupArr.Length; i++)
                    //        //{
                    //        //    if (xe1.GetAttribute("ID").ToString().Contains(groupArr[i]))
                    //        //    {
                    //        //        //获取group组合中第一个数据集类型的配置信息
                    //        //        RasterSourceTypeGroupInfo RasterSourceTypeGroupInfo = new RasterSourceTypeGroupInfo(xe1, groupids);
                    //        //        RasterSourceTypeGroups.Add(RasterSourceTypeGroupInfo);
                    //        //        break;
                    //        //    }
                    //        //}
                    //        if (xe1.GetAttribute("ID").ToString().Contains(firstStr))
                    //        {
                    //            //获取group组合中第一个数据集类型的配置信息
                    //            RasterSourceTypeGroupInfo RasterSourceTypeGroupInfo = new RasterSourceTypeGroupInfo(xe1, groupids);
                    //            RasterSourceTypeGroups.Add(RasterSourceTypeGroupInfo);
                    //            break;
                    //        }
                    //    }
                    //}
                }
            }
            RasterSourceTypeSingleCol = RasterSourceTypeSingles;
            RasterSourceTypeGroupCol = RasterSourceTypeGroups;
        }
    }
}
