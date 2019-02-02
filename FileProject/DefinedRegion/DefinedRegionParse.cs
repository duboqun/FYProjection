using PIE.Geometry;
using PIE.Meteo.Core;
using PIE.Meteo.RasterProject;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PIE.Meteo.FileProject
{
    public class DefinedRegionParse
    {
        static string configFilePath = Path.Combine(mService.WorkDir.FullName, "appconfig/DefinedRegion.xml");
        private Dictionary<string, PrjEnvelopeItem[]> _definedRegion;
        private BlockDefined _blockDefined = new BlockDefined();
        private ISpatialReference wgs84;
        public DefinedRegionParse()
        {
            LoadConfig();
            wgs84 = SpatialReferenceFactory.CreateSpatialReference(4326);
        }

        public BlockDefined BlockDefined
        {
            get { return _blockDefined; }
            set { _blockDefined = value; }
        }

        /// <summary>
        /// 过时的,请使用BlockDefined属性替换
        /// </summary>
        public Dictionary<string, PrjEnvelopeItem[]> DefinedRegion
        {
            get { return _definedRegion; }
        }

        /// <summary>
        /// 过时的,请使用BlockDefined属性替换
        /// </summary>
        public PrjEnvelopeItem[] GetEnvelopeItems(string blockIdentify)
        {
            if (_definedRegion == null || !_definedRegion.ContainsKey(blockIdentify))
                return null;
            return _definedRegion[blockIdentify];
        }

        private void LoadConfig()
        {
            
            if (!File.Exists(configFilePath))
            {
                Console.WriteLine("提供的预定义范围配置文件不存在" + configFilePath);
                return;
            }
            XElement xml = XElement.Load(configFilePath);
            if (xml == null)
            {
                Console.WriteLine("提供的预定义范围配置文件解析为空xml" + configFilePath);
                return;
            }
            _definedRegion = new Dictionary<string, PrjEnvelopeItem[]>();
            List<BlockItemGroup> groups = new List<BlockItemGroup>();
            IEnumerable<XElement> els = xml.Elements("EnvelopeGroup");
            XAttribute attr = null;
            foreach (XElement item in els)
            {
                string name = item.Attribute("name").Value;
                string desc = (attr = item.Attribute("description")) == null ? null : attr.Value;
                string identify = (attr = item.Attribute("identify")) == null ? null : attr.Value;
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                PrjEnvelopeItem[] prjs = ParseEnvelopes(item);
                _definedRegion.Add(name, prjs);
                groups.Add(new BlockItemGroup(name, desc, identify, prjs));
            }
            _blockDefined.AddRange(groups.ToArray());
        }

        private PrjEnvelopeItem[] ParseEnvelopes(XElement xml)
        {
            List<PrjEnvelopeItem> items = new List<PrjEnvelopeItem>();
            IEnumerable<XElement> els = xml.Elements("EnvelopeItem");
            XAttribute attr = null;
            foreach (XElement item in els)
            {
                string name = item.Attribute("name").Value;
                string identify = (attr = item.Attribute("identify")) == null ? null : attr.Value;
                PrjEnvelope env = ParseEnvelope(item);
                if (env != null)
                {
                    PrjEnvelopeItem envItem = new PrjEnvelopeItem(name, env, identify);
                    items.Add(envItem);
                }
            }
            return items.ToArray();
        }

        private PrjEnvelope ParseEnvelope(XElement item)
        {
            //<EnvelopeItem name="海河流域" minLongitude="113.94" minLatitude="36.94" maxLongitude="119.06" maxLatitude="42.06" />
            string minLongitude = item.Attribute("minLongitude").Value;
            string minLatitude = item.Attribute("minLatitude").Value;
            string maxLongitude = item.Attribute("maxLongitude").Value;
            string maxLatitude = item.Attribute("maxLatitude").Value;
            double minLongitudei, minLatitudei, maxLongitudei, maxLatitudei;
            if (double.TryParse(minLongitude, out minLongitudei) && double.TryParse(minLatitude, out minLatitudei)
            && double.TryParse(maxLongitude, out maxLongitudei) && double.TryParse(maxLatitude, out maxLatitudei))
            {
                PrjEnvelope env = new PrjEnvelope(minLongitudei, maxLongitudei, minLatitudei, maxLatitudei, wgs84);
                return env;
            }
            else
                return null;
        }

        public void ReLoad()
        {
            LoadConfig();
        }

        public void SaveTo()
        {
            Save(_blockDefined);
        }

        public static void Save(BlockDefined blockDefined)
        {
            var xml = new XElement("Root",
                        from item in blockDefined.BlockItemGroups
                        select new XElement("EnvelopeGroup",
                            new XAttribute("name", item.Name),
                            new XAttribute("description", item.Description == null ? "" : item.Description),
                            new XAttribute("identify", item.Identify == null ? "" : item.Identify),
                            from block in item.BlockItems
                            select new XElement("EnvelopeItem",
                                new XAttribute("name", block.Name),
                                new XAttribute("identify", block.Identify == null ? "" : block.Identify),
                                new XAttribute("minLongitude", block.PrjEnvelope.MinX),
                                new XAttribute("minLatitude", block.PrjEnvelope.MinY),
                                new XAttribute("maxLongitude", block.PrjEnvelope.MaxX),
                                new XAttribute("maxLatitude", block.PrjEnvelope.MaxY))));

            if (!Directory.Exists(Path.GetDirectoryName(configFilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(configFilePath));
            xml.Save(configFilePath);
        }
    }
}
