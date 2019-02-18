using PIE.Meteo.FileProject;
using PIE.Meteo.Model;
using PIE.Meteo.RasterProject;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using OSGeo.OGR;

///投影：LBT、MCT、OTG、AEA、PSG、NOM、NUL
namespace PIE.Meteo.ProjectTool
{
    public class Execute
    {
        private int _prjPngSize = 512;

        public Execute()
        {
            string size = "768"; //ConfigurationManager.AppSettings["ProjectionThumbnailSize"];
            if (!string.IsNullOrWhiteSpace(size))
                int.TryParse(size, out _prjPngSize);
        }

        public void Do(InputArg inArg)
        {
            CheckAtg(inArg);
            string projectionIdentify = inArg.ProjectionIdentify;
            OutputArg outArg = new OutputArg();
            try
            {
                using (AbstractWarpDataset inputRaster = WarpDataset.Open(inArg.InputFilename))
                {
                    RasterDatasetInfo dsInfo =
                        mRasterSourceManager.GetInstance().GetRasterDatasetInfo(inArg.InputFilename);
                    DateTime? dateTime = mRasterSourceManager.GetInstance().GetImageTime(inArg.InputFilename);
                    DataIdentify dataIdentify = new DataIdentify();
                    outArg.OrbitFilename = Path.GetFileName(inArg.InputFilename);
                    outArg.Satellite = dsInfo.SatelliteID;
                    outArg.Sensor = dsInfo.SensorID;
                    outArg.Level = "L1";
                    outArg.ProjectionIdentify = projectionIdentify;
                    outArg.ObservationDate = dateTime.HasValue ? dateTime.Value.ToString("yyyyMMdd") : "";
                    outArg.ObservationTime = dateTime.HasValue ? dateTime.Value.ToString("HHmm") : "";
                    outArg.Station = ParseStation(Path.GetFileName(inArg.InputFilename));
                    outArg.DayOrNight = DayOrNight(inputRaster);
                    if (dateTime.HasValue)
                        outArg.OrbitIdentify = CalcOrbitIdentify(dateTime.Value, inArg.PervObservationDate,
                            inArg.PervObservationTime, inArg.OrbitIdentify);
                    outArg.Length = new FileInfo(inArg.InputFilename).Length;
                    string validEnvelopeMsg = "";

                    #region 日夜检查

                    if (!string.IsNullOrWhiteSpace(inArg.DayNight))
                    {
                        if (inArg.DayNight != "daynight" && outArg.DayOrNight == "X")
                        {
                            outArg.LogLevel = "info";
                            outArg.LogInfo = "未设定处理白天和晚上数据，白天晚上标记未知：X";
                        }
                        else if (inArg.DayNight == "day" && outArg.DayOrNight != "D")
                        {
                            outArg.LogLevel = "info";
                            outArg.LogInfo = "设定为只处理白天数据，当前数据标记为晚上";
                        }
                        else if (inArg.DayNight == "night" && outArg.DayOrNight != "N")
                        {
                            outArg.LogLevel = "info";
                            outArg.LogInfo = "设定为只处理晚上数据，当前数据标记为白天";
                        }
                        else if (inArg.DayNight == "notnight" && outArg.DayOrNight == "N")
                        {
                            outArg.LogLevel = "info";
                            outArg.LogInfo = "设定为不处理晚上数据，当前数据标记为晚上";
                        }
                        else if (inArg.DayNight == "notday" && outArg.DayOrNight == "D")
                        {
                            outArg.LogLevel = "info";
                            outArg.LogInfo = "设定为不处理白天数据，当前数据标记为白天";
                        }
                    }

                    #endregion 日夜检查

                    if (inArg.ValidEnvelopes == null || inArg.ValidEnvelopes.Length == 0)
                    {
                        outArg.LogLevel = "error";
                        outArg.LogInfo = "参数错误：未正确设置ValidEnvelopes";
                    }
                    else if (!ValidEnvelope(inputRaster, inArg.ValidEnvelopes, out validEnvelopeMsg))
                    {
                        outArg.LogLevel = "info";
                        outArg.LogInfo = validEnvelopeMsg;
                    }
                    else
                    {
                        PrjOutArg prjArg;
                        if (inArg.Envelopes == null || inArg.Envelopes.Length == 0)
                            prjArg = new PrjOutArg(projectionIdentify, null, inArg.ResolutionX, inArg.ResolutionY,
                                inArg.OutputDir);
                        else
                            prjArg = new PrjOutArg(projectionIdentify, inArg.Envelopes, inArg.ResolutionX,
                                inArg.ResolutionY, inArg.OutputDir);
                        //prjArg.Args = new string[] { "SolarZenith"};
                        if (inArg.Bands != null && inArg.Bands.Length != 0)
                        {
                            prjArg.SelectedBands = inArg.Bands;
                            Console.WriteLine("SelectedBands:" + string.Join(",", prjArg.SelectedBands));
                        }

                        //扩展参数
                        List<string> extArgs = new List<string>();
                        extArgs.Add("IsClearPrjCache");
                        if (inArg.ExtArgs != null)
                            extArgs.AddRange(inArg.ExtArgs);
                        prjArg.Args = extArgs.ToArray();
                        ProjectionFactory prjFactory = new ProjectionFactory();
                        string retMessage = "";
                        string[] files = prjFactory.Project(inputRaster, prjArg, new Action<int, string>(OnProgress),
                            out retMessage);
                        prjFactory = null;
                        //投影结束，执行拼接，如果有拼接节点
                        List<OutFileArg> fileArgs = new List<OutFileArg>();
                        for (int i = 0; i < files.Length; i++)
                        {
                            string file = files[i];
                            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                                continue;
                            OutFileArg fileArg = new OutFileArg();
                            Envelope env = null;
                            float resolutionX;
                            float resolutionY;
                            string overViewFilename = "";
                            using (AbstractWarpDataset outfileRaster = WarpDataset.Open(file))
                            {
                                Console.WriteLine("生成缩略图开始");
                                overViewFilename = OverViewHelper.OverView(outfileRaster, _prjPngSize);
                                Console.WriteLine("生成缩略图结束");
                                env = outfileRaster.GetEnvelope();
                                resolutionX = outfileRaster.ResolutionX;
                                resolutionY = outfileRaster.ResolutionY;
                                var dt = dateTime.HasValue ? dateTime.Value : DateTime.Now;
                                TryMosaicFile(inArg, outfileRaster, dsInfo, dt, outArg.DayOrNight);
                            }

                            fileArg.OutputFilename = Path.GetFileName(file);
                            fileArg.Thumbnail =
                                (string.IsNullOrWhiteSpace(overViewFilename) && File.Exists(overViewFilename)
                                    ? ""
                                    : Path.GetFileName(overViewFilename));
                            string solarZenithFile = Path.Combine(Path.GetDirectoryName(file),
                                Path.GetFileNameWithoutExtension(file) + ".SolarZenith.ldf");
                            string solarZenithHdrFile = Path.Combine(Path.GetDirectoryName(file),
                                Path.GetFileNameWithoutExtension(file) + ".SolarZenith.hdr");
                            fileArg.ExtendFiles = Path.ChangeExtension(Path.GetFileName(file), "hdr") +
                                                  (string.IsNullOrWhiteSpace(solarZenithFile) &&
                                                   File.Exists(solarZenithFile)
                                                      ? ""
                                                      : "," + Path.GetFileName(solarZenithFile)) +
                                                  (string.IsNullOrWhiteSpace(solarZenithHdrFile) &&
                                                   File.Exists(solarZenithHdrFile)
                                                      ? ""
                                                      : "," + Path.GetFileName(solarZenithHdrFile));
                            fileArg.Envelope = new PrjEnvelopeItem("GBAL",
                                env == null
                                    ? null
                                    : new RasterProject.PrjEnvelope(env.MinX, env.MaxX, env.MinY, env.MaxY,
                                        SpatialReferenceFactory.CreateSpatialReference(4326)));
                            fileArg.ResolutionX = resolutionX.ToString();
                            fileArg.ResolutionY = resolutionY.ToString();
                            fileArg.Length = new FileInfo(file).Length;
                            fileArgs.Add(fileArg);
                            if (inArg.IsOnlySaveMosaicFile)
                                TryDeleteFile(file);
                        }

                        outArg.OutputFiles = fileArgs.ToArray();
                        outArg.LogLevel = "info";
                        if (string.IsNullOrWhiteSpace(retMessage))
                            outArg.LogInfo = "投影成功";
                        else
                            outArg.LogInfo = retMessage;
                        if (string.IsNullOrWhiteSpace(validEnvelopeMsg))
                            outArg.LogInfo = outArg.LogInfo + validEnvelopeMsg;
                    }
                }
            }
            catch (Exception ex)
            {
                outArg.LogLevel = "error";
                outArg.LogInfo = ex.Message + ex.StackTrace;
                Console.WriteLine("PIE.Meteo.ProjectTool.Execute()", ex);
            }
            finally
            {
                //输出参数文件重新命名
                string inputFileName = Path.GetFileName(inArg.InputFilename);

                System.Text.RegularExpressions.Regex rex = new System.Text.RegularExpressions.Regex(@"_\d{4}M");
                if (rex.IsMatch(inputFileName))
                {
                    string oldResStr = rex.Match(inputFileName).Groups[0].Value;
                    if (inArg.ProjectionIdentify == "GLL")
                        inputFileName = inputFileName.Replace(oldResStr,
                            $"_{PrjFileName.GLLResolutionIdentify(inArg.ResolutionX)}");
                    else
                        inputFileName = inputFileName.Replace(oldResStr,
                            $"_{PrjFileName.ResolutionIdentify(inArg.ResolutionX)}");
                }

                string outXmlFilename = Path.Combine(inArg.OutputDir, inputFileName + ".xml");
                OutputArg.WriteXml(outArg, outXmlFilename);
            }
        }

        /// <summary>
        /// 计算轨道圈号逻辑
        /// </summary>
        /// <param name="curDateTime"></param>
        /// <param name="pervDate"></param>
        /// <param name="pervTime"></param>
        /// <param name="perOrbitIdentify"></param>
        /// <returns></returns>
        private string CalcOrbitIdentify(DateTime curDateTime, string pervDate, string pervTime,
            string perOrbitIdentify)
        {
            if (string.IsNullOrWhiteSpace(pervDate) || string.IsNullOrWhiteSpace(pervTime))
                return curDateTime.ToString("HHmm");
            if (pervDate != curDateTime.ToString("yyyyMMdd")) //日期不相同,现在为第一轨
                return curDateTime.ToString("HHmm");
            DateTime perDateTime;
            if (!DateTime.TryParseExact(pervDate + pervTime, "yyyyMMddHHmm", null,
                System.Globalization.DateTimeStyles.None, out perDateTime))
                return curDateTime.ToString("HHmm");
            if (Math.Abs((perDateTime - curDateTime).TotalMinutes) >= 20)
            {
                //int perOrbitIdentifyNum;
                //if (!int.TryParse(perOrbitIdentify, out perOrbitIdentifyNum))
                //    return curDateTime.ToString("HHmm");
                return curDateTime.ToString("HHmm");
            }
            else
            {
                return perOrbitIdentify;
            }
        }

        private string CalcOrbitIdentify(InputArg inArg)
        {
            throw new NotImplementedException();
        }

        private void CheckAtg(InputArg inArg)
        {
            if (string.IsNullOrWhiteSpace(inArg.InputFilename))
                throw new Exception("参数InputFilename为空值");
            if (!File.Exists(inArg.InputFilename))
                throw new Exception("参数InputFilename提供的文件不存在或者不可访问[" + inArg.InputFilename + "]");
        }

        private void TryDeleteFile(string file)
        {
            try
            {
                File.Delete(file);
                File.Delete(Path.ChangeExtension(file, "hdr"));
            }
            catch
            {
            }
        }

        private void TryMosaicFile(InputArg inArg, AbstractWarpDataset fileRaster, RasterDatasetInfo dataIdentify,
            DateTime dateTime, string dayOrNight)
        {
            if (inArg.MosaicInputArg == null || string.IsNullOrWhiteSpace(inArg.MosaicInputArg.OutputDir) ||
                inArg.MosaicInputArg.Envelope == null)
                return;
            //if (!Day.Contains(dayOrNight))
            //{
            //    Console.WriteLine("非白天数据，不执行拼接");
            //    return;
            //}
            MosaicInputArg mosaicInputArg = inArg.MosaicInputArg;
            string projectionIdentify = inArg.ProjectionIdentify;
            string station = ParseStation(inArg.InputFilename);
            MosaicOutputArg outArg = new MosaicOutputArg();
            outArg.Satellite = dataIdentify.SatelliteID;
            outArg.Sensor = dataIdentify.SensorID;
            outArg.Level = "L1";
            outArg.ProjectionIdentify = projectionIdentify;
            outArg.ObservationDate = dateTime.ToString("yyyyMMdd");
            outArg.Station = station;
            outArg.DayOrNight = dayOrNight;
            AbstractWarpDataset mosaicFileRaster = null;
            try
            {
                string mosaicFilename = CreateMosaicFilename(inArg, dataIdentify, dateTime, projectionIdentify,
                    fileRaster.ResolutionX, station, dayOrNight);
                mosaicFilename = Path.Combine(mosaicInputArg.OutputDir, mosaicFilename);
                Mosaic mosaic = new Mosaic(inArg, fileRaster, new Action<int, string>(OnProgress));
                mosaicFileRaster = mosaic.MosaicToFile(mosaicFilename);
                OutFileArg fileArg = new OutFileArg();
                if (mosaicFileRaster != null)
                {
                    OnProgress(0, "生成缩略图");
                    string overViewFilename = OverViewHelper.OverView(mosaicFileRaster, 1024);
                    OnProgress(100, "完成缩略图");
                    fileArg.Envelope = mosaicInputArg.Envelope;
                    fileArg.ResolutionX = mosaicFileRaster.ResolutionX.ToString();
                    fileArg.ResolutionY = mosaicFileRaster.ResolutionY.ToString();
                    fileArg.OutputFilename = Path.GetFileName(mosaicFileRaster.fileName);
                    fileArg.Thumbnail = (string.IsNullOrWhiteSpace(overViewFilename)
                        ? ""
                        : Path.GetFileName(overViewFilename));
                    fileArg.ExtendFiles = Path.ChangeExtension(Path.GetFileName(mosaicFileRaster.fileName), "hdr");
                    fileArg.Length = new FileInfo(mosaicFileRaster.fileName).Length;
                }

                outArg.OutputFiles = new OutFileArg[] {fileArg};
                outArg.LogLevel = "info";
                outArg.LogInfo = "拼接完成";
            }
            catch (Exception ex)
            {
                outArg.LogLevel = "error";
                outArg.LogInfo = ex.Message;
            }
            finally
            {
                if (mosaicFileRaster != null)
                    mosaicFileRaster.Dispose();
                string outXmlFilename = Path.Combine(inArg.MosaicInputArg.OutputDir,
                    Path.GetFileName(inArg.InputFilename) + ".xml");
                MosaicOutputArg.WriteXml(outArg, outXmlFilename);
            }
        }

        private PrjEnvelope CoordToEnvelope(Envelope coordEnvelope)
        {
            return new PrjEnvelope(coordEnvelope.MinX, coordEnvelope.MaxX, coordEnvelope.MinY, coordEnvelope.MaxY,
                SpatialReferenceFactory.CreateSpatialReference(4326));
        }

        /// <summary>
        /// FY3A_MERSI_GBAL_L1_20120808_0000_1000M_MS.HDF->
        /// FY3A_MERSI_GBAL_GLL_L1_20120808_0000_1000M_MS_D.ldf
        /// </summary>
        /// <param name="inArg"></param>
        /// <param name="dataIdentify"></param>
        /// <returns></returns>
        private string CreateMosaicFilename(InputArg inArg, RasterDatasetInfo dataIdentify, DateTime dateTime,
            string projectionIdentify, float resolution, string station, string dayOrNight)
        {
            PrjEnvelopeItem item = inArg.MosaicInputArg.Envelope;

            return string.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}_{7}_{8}_{9}.ldf",
                dataIdentify.SatelliteID,
                dataIdentify.SensorID,
                item.Name,
                projectionIdentify,
                "L1",
                dateTime.ToString("yyyyMMdd"),
                string.IsNullOrWhiteSpace(inArg.OrbitIdentify) ? "0000" : inArg.OrbitIdentify,
                projectionIdentify == "GLL"
                    ? PrjFileName.GLLResolutionIdentify(resolution)
                    : PrjFileName.ResolutionIdentify(resolution),
                station,
                dayOrNight
            );
        }

        private string[] Days = new string[] {"D", "Day"};
        private string[] Nights = new string[] {"N", "Night"};
        private string[] Ms = new string[] {"M", "Both"};

        /// <summary>
        /// 会出现三个值D、N、M
        /// </summary>
        /// <param name="fileRaster"></param>
        /// <returns></returns>
        private string DayOrNight(AbstractWarpDataset fileRaster)
        {
            try
            {
                string v = "";
                Dictionary<string, string> filaAttrs = fileRaster.GetAttributes();
                if (filaAttrs.ContainsKey("Day Or Night Flag")) //VIRR:D MERSI:Day
                    v = filaAttrs["Day Or Night Flag"];
                else if (filaAttrs.ContainsKey("DAYNIGHTFLAG")) //MODIS:Day
                    v = filaAttrs["DAYNIGHTFLAG"];
                else
                    v = "";
                if (v == "")
                    return "X";
                if (Days.Contains(v))
                    return "D";
                if (Nights.Contains(v))
                    return "N";
                if (Ms.Contains(v))
                    return "M";
                return "X";
            }
            catch
            {
                return "X";
            }
        }

        private bool ValidEnvelope(AbstractWarpDataset inputRaster, PrjEnvelopeItem[] validEnvelopes, out string msg)
        {
            bool hasValid = false;
            StringBuilder str = new StringBuilder();
            foreach (PrjEnvelopeItem validEnvelope in validEnvelopes)
            {
                if (!ProjectionFactory.HasInvildEnvelope(inputRaster, validEnvelope.PrjEnvelope))
                    str.AppendLine("数据不在范围内：" + validEnvelope.Name + validEnvelope.PrjEnvelope.ToString());
                else
                    hasValid = true; //只要这块数据在一个有效区域内，就返回true，执行整块数据投影。
            }

            msg = str.ToString();
            return hasValid;
        }

        private string ParseStation(string filename)
        {
            string[] stations = new string[] {"BJ", "GZ", "XJ", "XZ", "JM", "KS", "SW", "MS"};
            foreach (string station in stations)
            {
                if (filename.Contains(station))
                    return station;
            }

            return "XX";
        }

        public void OnProgress(int progress, string text)
        {
            Console.WriteLine(progress + "," + text);
        }
    }
}