using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PIE.Meteo.FileProject
{
    public class FileFinder
    {
        #region Modis

        public static AbstractWarpDataset TryFind03FileFromModisImgFile(AbstractWarpDataset fileRaster)
        {
            string dir = Path.GetDirectoryName(fileRaster.fileName);
            string fileName = Path.GetFileName(fileRaster.fileName);
            string retFile;
            if (fileName.Contains("MOD021KM"))
                retFile = fileName.Replace("MOD021KM", "MOD03");
            else if (fileName.Contains("MOD02HKM"))
                retFile = fileName.Replace("MOD02HKM", "MOD03");
            else if (fileName.Contains("MOD02QKM"))
                retFile = fileName.Replace("MOD02QKM", "MOD03");
            else if (fileName.Contains("MYD021KM"))
                retFile = fileName.Replace("MYD021KM", "MYD03");
            else if (fileName.Contains("MYD02HKM"))
                retFile = fileName.Replace("MYD02HKM", "MYD03");
            else if (fileName.Contains("MYD02QKM"))
                retFile = fileName.Replace("MYD02QKM", "MYD03");
            else
                throw new Exception("无法找到角度数据(如经纬度等)文件[.MOD/MYD03.hdf]");
            retFile = Path.Combine(dir, retFile);
            if (retFile == fileName || !File.Exists(retFile))
                throw new Exception("无法找到角度数据(如经纬度等)文件[.MOD/MYD03.hdf]");
            try
            {
                return Open(retFile);
            }
            catch (Exception ex)
            {
                throw new Exception("读取经纬度文件失败，无法使用角度数据(如经纬度等)文件[.MOD/MYD03.hdf]" + ex.Message, ex);
            }
        }

        public static AbstractWarpDataset TryFindMODIS_1KM_L1From03(AbstractWarpDataset fileRaster)
        {
            string dir = Path.GetDirectoryName(fileRaster.fileName);
            string fileName = Path.GetFileName(fileRaster.fileName);
            string retFile = null;
            retFile = fileName.Replace("MOD03", "MOD021KM").Replace("MYD03", "MYD021KM");
            retFile = Path.Combine(dir, retFile);
            if (retFile == fileName || !File.Exists(retFile))
                return null;
            return Open(retFile);
        }

        public static AbstractWarpDataset TryFindMODIS_HKM_L1From03(AbstractWarpDataset fileRaster)
        {
            string dir = Path.GetDirectoryName(fileRaster.fileName);
            string fileName = Path.GetFileName(fileRaster.fileName);
            string retFile = null;
            retFile = fileName.Replace("MOD03", "MOD02HKM").Replace("MYD03", "MYD02HKM");
            retFile = Path.Combine(dir, retFile);
            if (retFile == fileName || !File.Exists(retFile))
                return null;
            return Open(retFile);
        }

        public static AbstractWarpDataset TryFindMODIS_QKM_L1From03(AbstractWarpDataset fileRaster)
        {
            string dir = Path.GetDirectoryName(fileRaster.fileName);
            string fileName = Path.GetFileName(fileRaster.fileName);
            string retFile = null;
            retFile = fileName.Replace("MOD03", "MOD02QKM").Replace("MYD03", "MYD02QKM");
            retFile = Path.Combine(dir, retFile);
            if (retFile == fileName || !File.Exists(retFile))
                return null;
            return Open(retFile);
        }

        #endregion

        public static AbstractWarpDataset Open(string filename)
        {
            var ds = WarpDataset.Open(filename);
            if (ds != null && ds.ds == null)
                ds = null;
            return ds;
        }

        #region FY

        /// <summary>
        /// 20130418添加了对以下类型文件名的支持。(尚未完成)
        /// Z_SATE_C_BAWX_20130321034403_P_FY3B_MERSI_GBAL_L1_20110220_0510_0250M_MS.HDF
        /// Z_SATE_C_BAWX_20130321034729_P_FY3B_MERSI_GBAL_L1_20110220_0510_1000M_MS.HDF
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static AbstractWarpDataset TryFindMERSI_1KM_L1FromQKM(AbstractWarpDataset fileRaster)
        {
            try
            {
                string dir = Path.GetDirectoryName(fileRaster.fileName);
                string fileName = Path.GetFileName(fileRaster.fileName);
                string retFile = fileName.Replace("_0250M_", "_1000M_");
                string resultretFile = Path.Combine(dir, retFile);
                if (retFile == fileName || !File.Exists(resultretFile))
                {
                    //这里再进一步扫描目录下的文件，用正则匹配，如果能找到，同样适用。
                    if (File.Exists(resultretFile.Replace("250M", "1000M")))
                    {
                        return Open(resultretFile.Replace("250M", "1000M"));
                    }
                    else
                        throw new Exception("无法找到对应1KM文件(获取太阳天顶角等角度数据使用)");
                }
                else
                    return Open(resultretFile);
            }
            catch (Exception ex)
            {
                throw new Exception("无法找到对应1KM文件(获取太阳天顶角等角度数据使用)", ex);
            }
        }

        public static AbstractWarpDataset TryFindMERSI_QKM_L1FromKM(AbstractWarpDataset fileRaster)
        {
            try
            {
                string dir = Path.GetDirectoryName(fileRaster.fileName);
                string fileName = Path.GetFileName(fileRaster.fileName);
                string retFile = fileName.Replace("_1000M", "_0250M");
                if (retFile == fileName) //文件名中不包含_1000M_
                    return null;
                string resultretFile = Path.Combine(dir, retFile);
                if (File.Exists(resultretFile))
                    return Open(resultretFile);
                //增加文件夹匹配原则搜索文件
                else if (File.Exists(Path.Combine(dir.Replace("1000M", "250M"), retFile)))
                {
                    return Open(Path.Combine(dir.Replace("1000M", "250M"), retFile));
                }

                //if (kmFileName.Contains("Z_SATE_C_BAWX_"))
                //{ 
                //}
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static AbstractWarpDataset TryFindGeoFileFromFY3C_VIRR(AbstractWarpDataset fileRaster)
        {
            string dir = Path.GetDirectoryName(fileRaster.fileName);
            string fileName = Path.GetFileName(fileRaster.fileName);
            string retFile;
            if (fileName.Contains("_1000M_"))
                retFile = fileName.Replace("_1000M_", "_GEOXX_");
            else if(fileName.Contains("L1B"))
            retFile = fileName.Replace("L1B", "GEOXX");
            else
                throw new Exception("无法找到角度数据(如经纬度等)文件[._GEOXX_...HDF]");
            retFile = Path.Combine(dir.Replace("1000M", "GEO"), retFile);
            if (retFile == fileName || !File.Exists(retFile))
                throw new Exception("无法找到角度数据(如经纬度等)文件[._GEOXX_...HDF]");
            try
            {
                return Open(retFile);
            }
            catch (Exception ex)
            {
                throw new Exception("获取经纬度文件失败" + ex.Message, ex);
            }
        }

        public static AbstractWarpDataset TryFindGeoFileFromFY3C_MERSI(AbstractWarpDataset fileRaster)
        {
            try
            {
                AbstractWarpDataset kmGeo = TryFindkmGeoFileFromFY3C_MERSI(fileRaster);
                if (kmGeo == null)
                {
                    kmGeo = TryFindQkmGeoFileFromFY3C_MERSI(fileRaster);
                }

                if (kmGeo == null)
                    throw new Exception("无法找到角度数据(如经纬度等)文件[._GEO1K_...HDF]或[._GEOQK_...HDF]");
                return kmGeo;
            }
            catch (Exception ex)
            {
                throw new Exception("打开经纬度文件失败" + ex.Message, ex);
            }
        }

        public static AbstractWarpDataset TryFindkmGeoFileFromFY3C_MERSI(AbstractWarpDataset fileRaster)
        {
            string fileName = fileRaster.fileName;
            string dir = Path.GetDirectoryName(fileName);
            fileName = Path.GetFileName(fileName);
            string retFile = fileName.Replace("_1000M_", "_GEO1K_").Replace("_0250M_", "_GEO1K_");
            ;
            string fileFullPath = Path.Combine(dir, retFile);
            if (retFile == fileName || !File.Exists(fileFullPath))
            {
                //如果文件夹内部不存在再匹配上面两层文件夹是否包含该文件
                fileFullPath = Path.Combine(dir.Replace("1000M", "GEO1K").Replace("250M", "GEOQK"), retFile);
                if (!File.Exists(fileFullPath))
                {
                    return null;
                }
            }

            try
            {
                return Open(fileFullPath);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public static AbstractWarpDataset TryFindQkmGeoFileFromFY3C_MERSI(AbstractWarpDataset fileRaster)
        {
            string fileName = fileRaster.fileName;
            string dir = Path.GetDirectoryName(fileName);
            fileName = Path.GetFileName(fileName);
            string retFile = fileName.Replace("_1000M_", "_GEOQK_").Replace("_0250M_", "_GEOQK_");
            string fileFullPath = Path.Combine(dir, retFile);
            if (retFile == fileName || !File.Exists(fileFullPath))
            {
                //如果文件夹内部不存在再匹配上面两层文件夹是否包含该文件
                fileFullPath = Path.Combine(dir.Replace("1000M", "GEOQK").Replace("250M", "GEOQK"), retFile);
                if (!File.Exists(fileFullPath))
                {
                    return null;
                }
            }

            try
            {
                return Open(fileFullPath);
            }
            catch
            {
                return null;
            }
        }

        public static AbstractWarpDataset TryFindFY3C_MERSI_1KM_L1FromQKM(AbstractWarpDataset fileRaster)
        {
            try
            {
                string dir = Path.GetDirectoryName(fileRaster.fileName);
                string fileName = Path.GetFileName(fileRaster.fileName);
                string retFile = fileName.Replace("_GEOQK_", "_1000M_");
                string fullpath = Path.Combine(dir, retFile);
                if (retFile == fileName)
                    return null;
                if (retFile == fileName || !File.Exists(fullpath))
                {
                    //如果文件夹内部不存在再匹配上面两层文件夹是否包含该文件
                    retFile = Path.Combine(dir.Replace("GEOQK", "1000M"), retFile);
                    if (!File.Exists(retFile))
                    {
                        return null;
                    }
                }

                return Open(retFile);
            }
            catch
            {
                return null;
            }
        }

        internal static AbstractWarpDataset TryFindFY4A_HKM_FromFKM(AbstractWarpDataset srcRaster)
        {
            AbstractWarpDataset result = null;
            string dir = Path.GetDirectoryName(srcRaster.fileName);
            string fileName = Path.GetFileName(srcRaster.fileName);
            string newFileName = fileName.Replace("_4000M_", "_0500M_");
            string newPath = Path.Combine(dir, newFileName);
            if (File.Exists(newPath))
            {
                result = WarpDataset.Open(newPath);
            }

            return result;
        }

        internal static AbstractWarpDataset TryFindFY4A_KM_FromFKM(AbstractWarpDataset srcRaster)
        {
            AbstractWarpDataset result = null;
            string dir = Path.GetDirectoryName(srcRaster.fileName);
            string fileName = Path.GetFileName(srcRaster.fileName);
            string newFileName = fileName.Replace("_4000M_", "_1000M_");
            string newPath = Path.Combine(dir, newFileName);
            if (File.Exists(newPath))
            {
                result = WarpDataset.Open(newPath);
            }

            return result;
        }

        internal static AbstractWarpDataset TryFindFY4A_DKM_FromFKM(AbstractWarpDataset srcRaster)
        {
            AbstractWarpDataset result = null;
            string dir = Path.GetDirectoryName(srcRaster.fileName);
            string fileName = Path.GetFileName(srcRaster.fileName);
            string newFileName = fileName.Replace("_4000M_", "_2000M_");
            string newPath = Path.Combine(dir, newFileName);
            if (File.Exists(newPath))
            {
                result = WarpDataset.Open(newPath);
            }

            return result;
        }

        #endregion

        #region BandmapTable

        public static void GetModisBandmapTable(AbstractWarpDataset kmRaster, AbstractWarpDataset hkmRaster,
            AbstractWarpDataset qkmRaster, int[] bandNumbers,
            out int[] qkmBandNumberMaps, out int[] hkmBandNumberMaps, out int[] kmBandNumberMaps)
        {
            qkmBandNumberMaps = null;
            hkmBandNumberMaps = null;
            kmBandNumberMaps = null;
            if (qkmRaster == null && kmRaster == null && hkmRaster == null)
                return;
            int qkmBandLength = PrjBand.MODIS_250_Orbit.Length;
            int hkmBandLength = PrjBand.MODIS_500_Orbit.Length;
            int kmBandLength = PrjBand.MODIS_1000_Orbit.Length;
            if (bandNumbers == null || bandNumbers.Length == 0)
            {
                bandNumbers = new int[kmBandLength];
                for (int i = 0; i < bandNumbers.Length; i++)
                {
                    bandNumbers[i] = i + 1;
                }
            }

            List<int> qkm = new List<int>();
            List<int> hkm = new List<int>();
            List<int> km = new List<int>();
            for (int i = 0; i < bandNumbers.Length; i++)
            {
                if (qkmRaster != null)
                {
                    if (bandNumbers[i] <= qkmBandLength)
                    {
                        qkm.Add(bandNumbers[i]); //当前通道号为bandIndexs[i]，目标的为i
                        continue;
                    }
                }

                if (hkmRaster != null)
                {
                    if (bandNumbers[i] <= hkmBandLength)
                    {
                        hkm.Add(bandNumbers[i]); //当前通道号为bandIndexs[i]，目标的为i
                        continue;
                    }
                }

                if (kmRaster != null)
                    if (bandNumbers[i] <= kmBandLength)
                        km.Add(bandNumbers[i]);
            }

            qkmBandNumberMaps = qkm.Count == 0 ? null : qkm.ToArray();
            hkmBandNumberMaps = hkm.Count == 0 ? null : hkm.ToArray();
            kmBandNumberMaps = km.Count == 0 ? null : km.ToArray();
        }

        internal static void GetBandmapTableMERSI(AbstractWarpDataset qkmRaster, AbstractWarpDataset kmRaster,
            int[] bandNumbers,
            out int[] qkmBandNoMaps, out int[] kmBandNoMaps)
        {
            qkmBandNoMaps = null;
            kmBandNoMaps = null;
            if (qkmRaster == null && kmRaster == null)
                return;
            int kmBandLength = PrjBand.MERSI_1000_Orbit.Length;
            int qkmBandLength = PrjBand.MERSI_0250_Orbit.Length;
            if (bandNumbers == null || bandNumbers.Length == 0)
            {
                bandNumbers = new int[kmBandLength];
                for (int i = 0; i < bandNumbers.Length; i++)
                {
                    bandNumbers[i] = i + 1;
                }
            }

            List<int> qkm = new List<int>();
            List<int> km = new List<int>();
            for (int i = 0; i < bandNumbers.Length; i++)
            {
                if (qkmRaster != null)
                {
                    if (bandNumbers[i] <= qkmBandLength)
                    {
                        qkm.Add(bandNumbers[i]); //当前通道号为bandIndexs[i]，目标的为i
                        continue;
                    }
                }

                if (kmRaster != null)
                    if (bandNumbers[i] <= kmBandLength)
                        km.Add(bandNumbers[i]);
            }

            qkmBandNoMaps = qkm.Count == 0 ? null : qkm.ToArray();
            kmBandNoMaps = km.Count == 0 ? null : km.ToArray();
        }

        public static void GetVIRRBandmapTable(int[] bandNumbers, out int[] kmBandNoMaps)
        {
            kmBandNoMaps = null;
            int kmBandLength = PrjBand.VIRR_1000_Orbit.Length;
            if (bandNumbers == null || bandNumbers.Length == 0)
            {
                bandNumbers = new int[kmBandLength];
                for (int i = 0; i < bandNumbers.Length; i++)
                {
                    bandNumbers[i] = i + 1;
                }
            }

            List<int> km = new List<int>();
            for (int i = 0; i < bandNumbers.Length; i++)
            {
                if (bandNumbers[i] <= kmBandLength)
                    km.Add(bandNumbers[i]);
            }

            kmBandNoMaps = km.Count == 0 ? null : km.ToArray();
        }

        internal static void GetBandmapTable(AbstractWarpDataset srcRaster, int[] bandIndexs, out int[] kmBandNoMaps)
        {
            kmBandNoMaps = null;
            PrjBand[] srcBands = PrjBandTable.GetPrjBands(srcRaster);
            int kmBandLength = srcBands.Length;
            if (bandIndexs == null || bandIndexs.Length == 0)
            {
                bandIndexs = new int[kmBandLength];
                for (int i = 0; i < bandIndexs.Length; i++)
                {
                    bandIndexs[i] = i + 1;
                }
            }

            List<int> km = new List<int>();
            for (int i = 0; i < bandIndexs.Length; i++)
            {
                if (bandIndexs[i] <= kmBandLength)
                    km.Add(bandIndexs[i]);
            }

            kmBandNoMaps = km.Count == 0 ? null : km.ToArray();
        }

        public static void GetNoaaBandmapTable(int[] bandIndexs, out int[] kmBandNoMaps)
        {
            kmBandNoMaps = null;
            int kmBandLength = PrjBand.AVHRR_1000_Orbit.Length;
            if (bandIndexs == null || bandIndexs.Length == 0)
            {
                bandIndexs = new int[kmBandLength];
                for (int i = 0; i < bandIndexs.Length; i++)
                {
                    bandIndexs[i] = i + 1;
                }
            }

            List<int> km = new List<int>();
            for (int i = 0; i < bandIndexs.Length; i++)
            {
                if (bandIndexs[i] <= kmBandLength)
                    km.Add(bandIndexs[i]);
            }

            kmBandNoMaps = km.Count == 0 ? null : km.ToArray();
        }

        public static void GetVISSRBandmapTable(int[] bandIndexs, out int[] kmBandNoMaps)
        {
            kmBandNoMaps = null;
            int kmBandLength = PrjBand.VISSR_5000_Orbit.Length;
            if (bandIndexs == null || bandIndexs.Length == 0)
            {
                bandIndexs = new int[kmBandLength];
                for (int i = 0; i < bandIndexs.Length; i++)
                {
                    bandIndexs[i] = i + 1;
                }
            }

            List<int> km = new List<int>();
            for (int i = 0; i < bandIndexs.Length; i++)
            {
                if (bandIndexs[i] <= kmBandLength)
                    km.Add(bandIndexs[i]);
            }

            kmBandNoMaps = km.Count == 0 ? null : km.ToArray();
        }

        internal static void GetBandmapTableMERSI2(AbstractWarpDataset qkmRaster, AbstractWarpDataset kmRaster,
            int[] bandNumbers,
            out int[] qkmBandNoMaps, out int[] kmBandNoMaps)
        {
            qkmBandNoMaps = null;
            kmBandNoMaps = null;
            if (qkmRaster == null && kmRaster == null)
                return;
            int kmBandLength = PrjBand.MERSI2_1000_Orbit.Length;
            int qkmBandLength = PrjBand.MERSI2_0250_Orbit.Length;
            if (bandNumbers == null || bandNumbers.Length == 0)
            {
                bandNumbers = new int[kmBandLength];
                for (int i = 0; i < bandNumbers.Length; i++)
                {
                    bandNumbers[i] = i + 1;
                }
            }

            List<int> qkm = new List<int>();
            List<int> km = new List<int>();
            for (int i = 0; i < bandNumbers.Length; i++)
            {
                if (qkmRaster != null)
                {
                    if (bandNumbers[i] <= qkmBandLength)
                    {
                        qkm.Add(bandNumbers[i]); //当前通道号为bandIndexs[i]，目标的为i
                        continue;
                    }
                }

                if (kmRaster != null)
                    if (bandNumbers[i] <= kmBandLength)
                        km.Add(bandNumbers[i]);
            }

            qkmBandNoMaps = qkm.Count == 0 ? null : qkm.ToArray();
            kmBandNoMaps = km.Count == 0 ? null : km.ToArray();
        }

        internal static void GetFY4ABandmapTable(AbstractWarpDataset hkmRaster, AbstractWarpDataset kmRaster,
            AbstractWarpDataset dkmRaster, AbstractWarpDataset fkmRaster, ref int[] bandNumbers, out int[] hkmBands,
            out int[] kmBands, out int[] dkmBands, out int[] fkmBands)
        {
            hkmBands = null;
            kmBands = null;
            dkmBands = null;
            fkmBands = null;
            List<AbstractWarpDataset> srcRasterList = new List<AbstractWarpDataset>
                {hkmRaster, kmRaster, dkmRaster, fkmRaster};
            List<PrjBand[]> bandDefList = new List<PrjBand[]>
            {
                BandDefCollection.FY4_0500_OrbitDefCollecges(), BandDefCollection.FY4_1000_OrbitDefCollecges(),
                BandDefCollection.FY4_2000_OrbitDefCollecges(), BandDefCollection.FY4_4000_OrbitDefCollecges()
            };
            if (srcRasterList.All(t => t == null)) return;
            int index = srcRasterList.FindLastIndex(t => t != null);
            PrjBand[] basePrjBand = bandDefList[index];

            if (bandNumbers == null || bandNumbers.Length == 0)
            {
                bandNumbers = Enumerable.Range(1, basePrjBand.Length).ToArray();
            }

            List<int> hkm = new List<int>();
            List<int> km = new List<int>();
            List<int> dkm = new List<int>();
            List<int> fkm = new List<int>();
            for (int i = 0; i < bandNumbers.Length; i++)
            {
                if (kmRaster != null)
                {
                    if (bandNumbers[i] <= bandDefList[1].Length)
                    {
                        km.Add(bandNumbers[i]); //当前通道号为bandIndexs[i]，目标的为i
                        continue;
                    }
                }

                if (dkmRaster != null)
                {
                    if (bandNumbers[i] <= bandDefList[2].Length)
                    {
                        dkm.Add(bandNumbers[i]); //当前通道号为bandIndexs[i]，目标的为i
                        continue;
                    }
                }

                if (fkmRaster != null)
                    if (bandNumbers[i] <= bandDefList[3].Length)
                        fkm.Add(bandNumbers[i]);
            }

            int hkmIndex = bandNumbers.ToList().FindIndex(t => t == 2);
            if (hkmRaster != null)
            {
                hkm.Add(hkmIndex);
            }

            hkmBands = hkm.Count == 0 ? null : hkm.ToArray();
            kmBands = km.Count == 0 ? null : km.ToArray();
            dkmBands = dkm.Count == 0 ? null : dkm.ToArray();
            fkmBands = fkm.Count == 0 ? null : fkm.ToArray();
        }

        #endregion

        #region HSD

        public static List<string> TryFindHSDGroupt(AbstractWarpDataset fileRaster)
        {
            Console.WriteLine
                ($"开始搜索与文件：{Environment.NewLine}          {fileRaster.fileName}{Environment.NewLine}相同时次的HSD数据");
            FileInfo h8FileInfo = new FileInfo(fileRaster.fileName);
            string[] part = Path.GetFileNameWithoutExtension(fileRaster.fileName).Split('_');
            DateTime time = DateTime.ParseExact(part[2] + part[3], "yyyyMMddHHmm",
                System.Globalization.CultureInfo.CurrentCulture);
            var files = h8FileInfo.Directory.GetFiles("*.bz2");

            List<string> groupBz2Files = new List<string>();
            foreach (var bzFile in files)
            {
                string[] parts = Path.GetFileNameWithoutExtension(bzFile.FullName).Split('_');
                DateTime curTime = DateTime.ParseExact(parts[2] + parts[3], "yyyyMMddHHmm",
                    System.Globalization.CultureInfo.CurrentCulture);
                if (curTime == time)
                    groupBz2Files.Add(bzFile.FullName);
            }

            Console.WriteLine
                ($"搜索到匹配时次数据{groupBz2Files.Count}个");
            return groupBz2Files;
        }

        #endregion
        
                #region NPP
        //匹配路径
        static string[] regStrArray = new string[]{
            "gdnbo" , "gimgo", "gitco", "gmodo", "gmtco",
            "icdbg","ivcdb","ivobc","svdnb",
            "svi\\d{2}","svm\\d{2}"
        };
        static string[] regStrArray2 = new string[]{
            "GDNBO" , "GIMGO", "GITCO", "GMODO", "GMTCO",
            "ICDBG","IVCDB","IVOBC","SVDNB",
            "SVI\\d{2}","SVM\\d{2}"
        };
        private static string TryFindNppFile(string filePath,string type)
        {
            string result = string.Empty;
            string searchDir = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileName(filePath);
            foreach (var item in regStrArray)
            {
                if (Regex.IsMatch(fileName, item))
                {
                    var temp = Path.Combine(searchDir, Regex.Replace(fileName, item, type));
                    if (File.Exists(temp))
                        result = temp;
                    else
                        throw new ArgumentException($"NPP数据的{type}类型数据无法找到");
                    break;
                }

            }
            if(string.IsNullOrEmpty(result))
            {
                foreach (var item in regStrArray2)
                {
                    if (Regex.IsMatch(fileName, item))
                    {
                        var temp = Path.Combine(searchDir, Regex.Replace(fileName, item, type.ToUpper()));
                        if (File.Exists(temp))
                            result = temp;
                        else
                            throw new ArgumentException($"NPP数据的{type}类型数据无法找到");
                        break;
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// 获取NppM波段的Geo文件
        /// </summary>
        /// <param name="nppDataset"></param>
        /// <returns></returns>
        public static string TryFindNppMbandGeoFile(AbstractWarpDataset nppDataset)
        {
            return TryFindNppFile(nppDataset.fileName, "gmodo");
        }
        /// <summary>
        /// 获取Npp数据I波段Geo文件
        /// </summary>
        /// <param name="nppDataset"></param>
        /// <returns></returns>
        public static string TryFindNppIbandGeoFile(AbstractWarpDataset nppDataset)
        {
            return TryFindNppFile(nppDataset.fileName, "gimgo");
        }
        /// <summary>
        /// 获取Npp数据I波段文件路径
        /// </summary>
        /// <param name="nppDataset"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        public static string TryFindNppIbandFile(AbstractWarpDataset nppDataset, int num)
        {
            return TryFindNppFile(nppDataset.fileName, $"svi{num:00}");
        }
        /// <summary>
        /// 获取Npp数据M波段文件路径
        /// </summary>
        /// <param name="nppDataset"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        public static string TryFindNppMbandFile(AbstractWarpDataset nppDataset, int num)
        {
            return TryFindNppFile(nppDataset.fileName, $"svm{num:00}");
        }
        #endregion
    }
}