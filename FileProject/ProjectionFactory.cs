using PIE.Meteo.Model;
using PIE.Meteo.RasterProject;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;

namespace PIE.Meteo.FileProject
{
    public class ProjectionFactory
    {
        private FileChecker _fileChecker = null;
        private GenericFilename _genericFilename;

        public ProjectionFactory()
        {
            _fileChecker = new FileChecker();
            _genericFilename = new GenericFilename();
        }

        public string[] Project(string file, PrjOutArg prjOutArg, Action<int, string> progress, out string messageBox)
        {
            AbstractWarpDataset srcRaster = null;
            try
            {
                srcRaster = WarpDataset.Open(file);
                if (srcRaster == null)
                {
                    //RasterDatasetInfo rInfo = mRasterSourceManager.GetInstance().GetRasterDatasetInfo(file);
                    //if (rInfo != null && rInfo.SatelliteID == "AHI8-OBI")
                    //{
                    //    return PrjH08(file, prjOutArg, progress, out messageBox);
                    //}
                    //else
                    //{
                    //    throw new Exception("读取待投影数据失败" + file);
                    //}
                }
                return Project(srcRaster, prjOutArg, progress, out messageBox);
            }
            finally
            {
                if (srcRaster != null)
                    srcRaster.Dispose();
            }
        }

        public string[] Project(AbstractWarpDataset srcRaster, PrjOutArg prjOutArg, Action<int, string> progress,
            out string messageBox)
        {
            messageBox = null;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            string fileType = _fileChecker.GetFileType(srcRaster);
            if (string.IsNullOrWhiteSpace(fileType))
                throw new Exception("暂未支持该类数据的投影");
            if (prjOutArg == null)
                throw new Exception("投影参数为空");
            if (progress != null)
                progress(1, "启动投影");
            string[] outFiles = null;
            StringBuilder errorMessage = null;
            if (prjOutArg.Envelopes == null || prjOutArg.Envelopes.Length == 0)
                prjOutArg.Envelopes = new PrjEnvelopeItem[] { new PrjEnvelopeItem("WHOLE", null) };
            if (string.IsNullOrWhiteSpace(prjOutArg.OutDirOrFile))
                prjOutArg.OutDirOrFile = Path.GetDirectoryName(srcRaster.fileName);
            switch (fileType)
            {
                case "H8":
                    //outFiles = PrjH08(srcRaster, prjOutArg, progress, out messageBox);
                    break;
                case "VIRR_L1":
                    outFiles = PrjVIRR(srcRaster, prjOutArg, progress, out errorMessage);
                    break;

                case "FY3C_VIRR_L1":
                    outFiles = PrjFY3C_VIRR(srcRaster, prjOutArg, progress, out errorMessage);
                    break;

                case "MERSI_1KM_L1":
                    outFiles = PrjMERSI_1KM_L1(srcRaster, prjOutArg, progress, out errorMessage);
                    break;

                case "MERSI_QKM_L1":
                case "MERSI_250M_L1":
                    outFiles = PrjMERSI_QKM_L1(srcRaster, prjOutArg, progress, out errorMessage);
                    break;

                case "FY3C_MERSI_1KM_L1":
                    outFiles = PrjFY3C_MERSI_1KM_L1(srcRaster, prjOutArg, progress, out errorMessage);
                    break;

                case "FY3C_MERSI_QKM_L1":
                    outFiles = PrjFY3C_MERSI_QKM_L1(srcRaster, prjOutArg, progress, out errorMessage);
                    break;

                case "FY3D_MERSI_1KM_L1":
                    outFiles = PrjFY3D_MERSI_1KM_L1(srcRaster, prjOutArg, progress, out errorMessage);
                    break;

                case "FY3D_MERSI_QKM_L1":
                    outFiles = PrjFY3D_MERSI_QKM_L1(srcRaster, prjOutArg, progress, out errorMessage);
                    break;

                case "MODIS_1KM_L1":
                    outFiles = PrjMODIS_1KM_L1(srcRaster, prjOutArg, progress, out errorMessage);
                    break;

                case "MODIS_HKM_L1":
                    outFiles = PrjMODIS_HKM_L1(srcRaster, prjOutArg, progress, out errorMessage);
                    break;

                case "MODIS_QKM_L1":
                    outFiles = PrjMODIS_QKM_L1(srcRaster, prjOutArg, progress, out errorMessage);
                    break;

                case "NOAA_1BD":
                    outFiles = PrjNOAA_1BD_L1(srcRaster, prjOutArg, progress, out errorMessage);
                    break;

                case "FY1X_1A5":
                    outFiles = PrjFY1X_1A5_L1(srcRaster, prjOutArg, progress, out errorMessage);
                    break;
                //case "FY2NOM":
                //    outFiles = PrjFY2NOM_L1(srcRaster, prjOutArg, progress, out errorMessage);
                //    break;
                case "FY4A_AGRI_0500":
                case "FY4A_AGRI_1000":
                case "FY4A_AGRI_2000":
                    outFiles = PrjFY4A_L1(srcRaster, prjOutArg, progress, out errorMessage);
                    break;

                case "FY4A_AGRI_4000":
                    //outFiles = PrjFY4A_L1(srcRaster, prjOutArg, progress, out errorMessage);
                    outFiles = PrjFY4A_Mix(srcRaster, prjOutArg, progress, out errorMessage);
                    break;

                case "FY2NOM":
                    outFiles = PrjFY2X_NOM(srcRaster, prjOutArg, progress, out errorMessage);
                    break;

                case "PROJECTED":
                    outFiles = PrjProjected(srcRaster, prjOutArg, progress, out errorMessage);
                    break;

                default:
                    break;
            }
            stopwatch.Stop();
            Debug.WriteLine("投影耗时" + stopwatch.ElapsedMilliseconds.ToString() + "ms");
            if (errorMessage != null)
                messageBox = errorMessage.ToString();
            return outFiles;
        }

        

        #region FY3_VIRR

        // FY3A_VIRR_06_GLL_L1_20120909_Day3_1000M.LDF
        private string[] PrjVIRR(AbstractWarpDataset fileName, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            SpatialReference dstSpatialRef = prjOutArg.ProjectionRef;
            PrjEnvelopeItem[] prjEnvelopes = prjOutArg.Envelopes;
            AbstractWarpDataset srcRaster = fileName;
            IFileProjector projector = null;
            int[] kmBands;
            FileFinder.GetVIRRBandmapTable(prjOutArg.SelectedBands, out kmBands);
            errorMessage = new StringBuilder();
            try
            {
                List<string> outFiles = new List<string>();
                projector = FileProjector.GetFileProjectByName("FY3_VIRR");
                projector.BeginSession(srcRaster);
                for (int i = 0; i < prjEnvelopes.Length; i++)
                {
                    string outFileName = null;
                    try
                    {
                        if (prjOutArg.ResolutionX == 0)
                        {
                            if (dstSpatialRef == null || dstSpatialRef.IsGeographic()==1)
                            {
                                prjOutArg.ResolutionX = 0.01f;
                                prjOutArg.ResolutionY = 0.01f;
                            }
                            else
                            {
                                prjOutArg.ResolutionX = 1000f;
                                prjOutArg.ResolutionY = 1000f;
                            }
                        }
                        if (IsDir(prjOutArg.OutDirOrFile))
                            outFileName = GetOutPutFile(prjOutArg.OutDirOrFile, srcRaster.fileName, dstSpatialRef, prjEnvelopes[i].Name, srcRaster.DataIdentify, prjOutArg.ResolutionX, ".ldf");
                        else
                            outFileName = prjOutArg.OutDirOrFile;

                        FY3_VIRR_PrjSettings prjSetting = new FY3_VIRR_PrjSettings();
                        prjSetting.OutPathAndFileName = outFileName;
                        prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                        prjSetting.OutBandNos = kmBands;
                        prjSetting.OutResolutionX = prjOutArg.ResolutionX;
                        prjSetting.OutResolutionY = prjOutArg.ResolutionY;
                        if (prjOutArg.Args != null)
                        {
                            if (prjOutArg.Args.Contains("NotRadiation"))
                                prjSetting.IsRadRef = false;
                            if (prjOutArg.Args.Contains("Radiation"))
                            {
                                prjSetting.IsRadRef = false;
                                prjSetting.IsRad = true;
                            }
                            if (prjOutArg.Args.Contains("NotSolarZenith"))
                                prjSetting.IsSolarZenith = false;
                            if (prjOutArg.Args.Contains("IsSensorZenith"))
                                prjSetting.IsSensorZenith = true;
                            if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                prjSetting.IsClearPrjCache = true;
                            prjSetting.ExtArgs = prjOutArg.Args;
                        }
                        projector.Project(srcRaster, prjSetting, dstSpatialRef, progress);
                        outFiles.Add(prjSetting.OutPathAndFileName);
                    }
                    catch (Exception ex)
                    {
                        TryDeleteErrorFile(outFileName);
                        outFiles.Add(null);
                        errorMessage.AppendLine(ex.Message);
                        Console.WriteLine(ex.Message);
                    }
                }
                return outFiles.ToArray();
            }
            finally
            {
                if (projector != null)
                    projector.EndSession();
            }
        }

        #endregion FY3_VIRR

        #region FY3C_VIRR

        private string[] PrjFY3C_VIRR(AbstractWarpDataset srcRaster, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            errorMessage = new StringBuilder();
            AbstractWarpDataset geoRaster = FileFinder.TryFindGeoFileFromFY3C_VIRR(srcRaster);
            SpatialReference dstSpatialRef = prjOutArg.ProjectionRef;
            PrjEnvelopeItem[] prjEnvelopes = prjOutArg.Envelopes;
            IFileProjector projector = null;
            int[] kmBands;
            FileFinder.GetVIRRBandmapTable(prjOutArg.SelectedBands, out kmBands);
            try
            {
                List<string> outFiles = new List<string>();
                projector = FileProjector.GetFileProjectByName("FY3C_VIRR");
                projector.BeginSession(srcRaster);
                for (int i = 0; i < prjEnvelopes.Length; i++)
                {
                    string outFileName = null;
                    try
                    {
                        if (prjOutArg.ResolutionX == 0)
                        {
                            if (dstSpatialRef == null || dstSpatialRef.IsGeographic()==1)
                            {
                                prjOutArg.ResolutionX = 0.01f;
                                prjOutArg.ResolutionY = 0.01f;
                            }
                            else
                            {
                                prjOutArg.ResolutionX = 1000f;
                                prjOutArg.ResolutionY = 1000f;
                            }
                        }
                        if (IsDir(prjOutArg.OutDirOrFile))
                            outFileName = GetOutPutFile(prjOutArg.OutDirOrFile, srcRaster.fileName, dstSpatialRef, prjEnvelopes[i].Name, srcRaster.DataIdentify, prjOutArg.ResolutionX, ".ldf");
                        else
                            outFileName = prjOutArg.OutDirOrFile;

                        FY3_VIRR_PrjSettings prjSetting = new FY3_VIRR_PrjSettings();
                        prjSetting.GeoFile = geoRaster;
                        prjSetting.OutPathAndFileName = outFileName;
                        prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                        prjSetting.OutBandNos = kmBands;
                        prjSetting.OutResolutionX = prjOutArg.ResolutionX;
                        prjSetting.OutResolutionY = prjOutArg.ResolutionY;
                        if (prjOutArg.Args != null)
                        {
                            if (prjOutArg.Args.Contains("NotRadiation"))
                                prjSetting.IsRadRef = false;
                            if (prjOutArg.Args.Contains("Radiation"))
                            {
                                prjSetting.IsRadRef = false;
                                prjSetting.IsRad = true;
                            }
                            if (prjOutArg.Args.Contains("NotSolarZenith"))
                                prjSetting.IsSolarZenith = false;
                            if (prjOutArg.Args.Contains("IsSensorZenith"))
                                prjSetting.IsSensorZenith = true;
                            if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                prjSetting.IsClearPrjCache = true;
                            prjSetting.ExtArgs = prjOutArg.Args;
                        }
                        projector.Project(srcRaster, prjSetting, dstSpatialRef, progress);
                        outFiles.Add(prjSetting.OutPathAndFileName);
                    }
                    catch (Exception ex)
                    {
                        TryDeleteErrorFile(outFileName);
                        outFiles.Add(null);
                        errorMessage.AppendLine(ex.Message);
                        Console.WriteLine(ex.Message);
                    }
                }
                return outFiles.ToArray();
            }
            finally
            {
                if (geoRaster != null)
                    geoRaster.Dispose();
                if (projector != null)
                    projector.EndSession();
            }
        }

        #endregion FY3C_VIRR

        #region FY3C_MERSI

        private string[] PrjFY3C_MERSI_1KM_L1(AbstractWarpDataset fileName, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            AbstractWarpDataset kmRaster = fileName;
            AbstractWarpDataset qkmRaster = FileFinder.TryFindMERSI_QKM_L1FromKM(fileName);
            AbstractWarpDataset geoRaster = FileFinder.TryFindGeoFileFromFY3C_MERSI(fileName);
            AbstractWarpDataset qkmGeoRaster = FileFinder.TryFindQkmGeoFileFromFY3C_MERSI(fileName);//qkmGeo中仅存储了地理坐标信息。
            if (geoRaster == null && qkmGeoRaster == null)
            {
                throw new Exception("无法找到角度数据(如经纬度等)文件[._GEO1K_...HDF]或[._GEOQK_...HDF]");
            }
            return PrjFY3C_MERSI(qkmRaster, kmRaster, qkmGeoRaster, geoRaster, prjOutArg, progress, out errorMessage);
        }

        private string[] PrjFY3C_MERSI_QKM_L1(AbstractWarpDataset fileName, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            AbstractWarpDataset qkmRaster = fileName;
            AbstractWarpDataset kmRaster = FileFinder.TryFindFY3C_MERSI_1KM_L1FromQKM(fileName);
            AbstractWarpDataset kmGeoRaster = FileFinder.TryFindkmGeoFileFromFY3C_MERSI(fileName);
            AbstractWarpDataset qkmGeoRaster = FileFinder.TryFindQkmGeoFileFromFY3C_MERSI(fileName);
            if (kmGeoRaster == null && qkmGeoRaster == null)
            {
                throw new Exception("无法找到角度数据(如经纬度等)文件[._GEO1K_...HDF]或[._GEOQK_...HDF]");
            }
            return PrjFY3C_MERSI(qkmRaster, kmRaster, qkmGeoRaster, kmGeoRaster, prjOutArg, progress, out errorMessage);
        }

        private string[] PrjFY3C_MERSI(AbstractWarpDataset qkmRaster, AbstractWarpDataset kmRaster, AbstractWarpDataset qkmGeoRaster, AbstractWarpDataset kmGeoRaster, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            SpatialReference dstSpatialRef = prjOutArg.ProjectionRef;
            PrjEnvelopeItem[] prjEnvelopes = prjOutArg.Envelopes;
            float outResolutionX = prjOutArg.ResolutionX;
            float outResolutionY = prjOutArg.ResolutionY;
            SetDefaultResolutionForMersi(qkmRaster, dstSpatialRef, ref outResolutionX, ref outResolutionY);
            prjOutArg.ResolutionX = outResolutionX;
            prjOutArg.ResolutionY = outResolutionY;
            string outDir = prjOutArg.OutDirOrFile;

            float baseResolutionK = 0.01f;
            if (dstSpatialRef == null || dstSpatialRef.IsGeographic()==1)
            {
                baseResolutionK = 0.01f;
            }
            else
            {
                baseResolutionK = 1000f;
            }
            if (baseResolutionK / outResolutionX < 1.5 && baseResolutionK / outResolutionY < 1.5)
            {
                if (qkmRaster != null && kmRaster != null)
                {
                    qkmRaster.Dispose();
                    qkmRaster = null;
                }
            }

            errorMessage = new StringBuilder();

            int[] qkmBands = null;
            int[] kmBands = null;
            FileFinder.GetBandmapTableMERSI(qkmRaster, kmRaster, prjOutArg.SelectedBands, out qkmBands, out kmBands);
            int bandCount = (qkmBands == null ? 0 : qkmBands.Length) + (kmBands == null ? 0 : kmBands.Length);
            if (bandCount == 0)
            {
                errorMessage.Append("没有获取要投影的通道");
                return null;
            }
            string bandNames = "BANDNAMES="
                + (qkmBands == null || qkmBands.Length == 0 ? "" : BandNameString(qkmBands) + ",")
                + BandNameString(kmBands);
            IFileProjector projtor = FileProjector.GetFileProjectByName("FY3C_MERSI");
            try
            {
                List<string> outFiles = new List<string>();
                AbstractWarpDataset outRaster = null;
                for (int i = 0; i < prjEnvelopes.Length; i++)
                {
                    PrjEnvelope envelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                    if (envelope == null)
                    {
                        if (kmGeoRaster != null)
                            projtor.ComputeDstEnvelope(kmGeoRaster, dstSpatialRef, out envelope, null);
                        else
                            projtor.ComputeDstEnvelope(qkmGeoRaster, dstSpatialRef, out envelope, null);
                        if (envelope == null)
                        {
                            errorMessage.Append("未能读取出文件的经纬度范围：" + kmRaster.fileName);
                            continue;
                        }
                    }
                    string outFileName = null;
                    try
                    {
                        string orbitFileName = null;
                        DataIdentify dataIdentify = null;
                        if (qkmRaster != null)
                        {
                            orbitFileName = qkmRaster.fileName;
                            dataIdentify = qkmRaster.DataIdentify;
                        }
                        else
                        {
                            orbitFileName = kmRaster.fileName;
                            dataIdentify = kmRaster.DataIdentify;
                        }
                        if (IsDir(prjOutArg.OutDirOrFile))
                            outFileName = GetOutPutFile(prjOutArg.OutDirOrFile, orbitFileName, dstSpatialRef, prjEnvelopes[i].Name, dataIdentify, outResolutionX, ".tif");
                        else
                            outFileName = prjOutArg.OutDirOrFile;
                        outRaster = CreateRaster(outFileName, envelope, outResolutionX, outResolutionY, bandCount, dstSpatialRef, bandNames);
                        outFiles.Add(outFileName);
                    }
                    catch (Exception ex)
                    {
                        TryDeleteErrorFile(outFileName);
                        errorMessage.Append(ex.Message);
                        outFiles.Add(null);
                    }
                    finally
                    {
                        if (outRaster != null)
                        {
                            outRaster.Dispose();
                            outRaster = null;
                        }
                    }
                }
                bool hasAngle = false;
                int perBandCount = 0;
                if (qkmBands != null && qkmBands.Length != 0)
                {
                    perBandCount = qkmBands.Length;
                    projtor.BeginSession(qkmRaster);
                    for (int i = 0; i < prjEnvelopes.Length; i++)
                    {
                        if (outFiles[i] == null)
                            continue;
                        try
                        {
                            outRaster = OpenUpdate(outFiles[i]);
                            FY3_MERSI_PrjSettings prjSetting = new FY3_MERSI_PrjSettings();
                            prjSetting.SecondaryOrbitRaster = kmRaster;
                            prjSetting.OutResolutionX = outResolutionX;
                            prjSetting.OutResolutionY = outResolutionY;
                            prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                            prjSetting.OutBandNos = qkmBands;
                            prjSetting.OutPathAndFileName = outFiles[i];
                            prjSetting.GeoFile = qkmGeoRaster != null ? qkmGeoRaster : kmGeoRaster;
                            prjSetting.AngleFile = kmGeoRaster;//
                            if (prjOutArg.Args != null)
                            {
                                if (prjOutArg.Args.Contains("NotRadiation"))
                                    prjSetting.IsRadRef = false;
                                if (prjOutArg.Args.Contains("Radiation"))
                                {
                                    prjSetting.IsRadRef = false;
                                    prjSetting.IsRad = true;
                                }
                                if (prjOutArg.Args.Contains("NotSolarZenith"))
                                    prjSetting.IsSolarZenith = false;
                                if (prjOutArg.Args.Contains("IsSensorZenith"))
                                    prjSetting.IsSensorZenith = true;
                                if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                    prjSetting.IsClearPrjCache = true;
                            }
                            if (!hasAngle)//只输出一次角度数据
                            {
                                prjSetting.ExtArgs = prjOutArg.Args;
                                hasAngle = true;
                            }
                            projtor.Project(qkmRaster, prjSetting, outRaster, 0, progress);
                        }
                        catch (Exception ex)
                        {
                            errorMessage.AppendLine(ex.Message);
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            if (outRaster != null)
                            {
                                outRaster.Dispose();
                                outRaster = null;
                            }
                        }
                    }
                    projtor.EndSession();
                }
                if (kmBands != null && kmBands.Length != 0)
                {
                    projtor.BeginSession(qkmRaster);
                    for (int i = 0; i < prjEnvelopes.Length; i++)
                    {
                        if (outFiles[i] == null)
                            continue;
                        try
                        {
                            outRaster = OpenUpdate(outFiles[i]);
                            FY3_MERSI_PrjSettings prjSetting = new FY3_MERSI_PrjSettings();
                            prjSetting.OutPathAndFileName = outFiles[i];
                            prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                            prjSetting.OutResolutionX = outResolutionX;
                            prjSetting.OutResolutionY = outResolutionY;
                            prjSetting.OutBandNos = kmBands;
                            prjSetting.GeoFile = kmGeoRaster != null ? kmGeoRaster : qkmGeoRaster;
                            prjSetting.AngleFile = kmGeoRaster;//
                            if (prjOutArg.Args != null)
                            {
                                if (prjOutArg.Args.Contains("NotRadiation"))
                                    prjSetting.IsRadRef = false;
                                if (prjOutArg.Args.Contains("Radiation"))
                                {
                                    prjSetting.IsRadRef = false;
                                    prjSetting.IsRad = true;
                                }
                                if (prjOutArg.Args.Contains("NotSolarZenith"))
                                    prjSetting.IsSolarZenith = false;
                                if (prjOutArg.Args.Contains("IsSensorZenith"))
                                    prjSetting.IsSensorZenith = true;
                                if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                    prjSetting.IsClearPrjCache = true;
                                prjSetting.ExtArgs = prjOutArg.Args;
                            }
                            if (!hasAngle)
                            {
                                prjSetting.ExtArgs = prjOutArg.Args;
                                hasAngle = true;
                            }
                            projtor.Project(kmRaster, prjSetting, outRaster, perBandCount, progress);
                        }
                        catch (Exception ex)
                        {
                            errorMessage.AppendLine(ex.Message);
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            if (outRaster != null)
                            {
                                outRaster.Dispose();
                                outRaster = null;
                            }
                        }
                    }
                }
                return outFiles.ToArray();
            }
            finally
            {
                if (projtor != null)
                    projtor.EndSession();
                if (kmRaster != null)
                    kmRaster.Dispose();
            }
        }

        #endregion FY3C_MERSI

        #region FY3D_MERSI

        private string[] PrjFY3D_MERSI_1KM_L1(AbstractWarpDataset srcRaster, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            AbstractWarpDataset kmRaster = null;
            AbstractWarpDataset qkmRaster = null;
            AbstractWarpDataset geoRaster = null;
            AbstractWarpDataset qkmGeoRaster = null;
            try
            {
                kmRaster = srcRaster;
                qkmRaster = FileFinder.TryFindMERSI_QKM_L1FromKM(srcRaster);
                geoRaster = FileFinder.TryFindGeoFileFromFY3C_MERSI(srcRaster);
                qkmGeoRaster = FileFinder.TryFindQkmGeoFileFromFY3C_MERSI(srcRaster);//qkmGeo中仅存储了地理坐标信息。
                if (geoRaster == null && qkmGeoRaster == null)
                {
                    throw new Exception("无法找到角度数据(如经纬度等)文件[._GEO1K_...HDF]或[._GEOQK_...HDF]");
                }
                return PrjFY3D_MERSI_1KM(qkmRaster, kmRaster, qkmGeoRaster, geoRaster, prjOutArg, progress, out errorMessage);
            }
            finally
            {
                if (kmRaster != null)
                    kmRaster.Dispose();
                if (qkmRaster != null)
                    qkmRaster.Dispose();
                if (geoRaster != null)
                    geoRaster.Dispose();
                if (qkmGeoRaster != null)
                    qkmGeoRaster.Dispose();
            }
        }

        private string[] PrjFY3D_MERSI_QKM_L1(AbstractWarpDataset srcRaster, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            AbstractWarpDataset qkmRaster = srcRaster;
            AbstractWarpDataset kmRaster = FileFinder.TryFindFY3C_MERSI_1KM_L1FromQKM(srcRaster);
            AbstractWarpDataset kmGeoRaster = FileFinder.TryFindkmGeoFileFromFY3C_MERSI(srcRaster);
            AbstractWarpDataset qkmGeoRaster = FileFinder.TryFindQkmGeoFileFromFY3C_MERSI(srcRaster);
            if (kmGeoRaster == null && qkmGeoRaster == null)
            {
                throw new Exception("无法找到角度数据(如经纬度等)文件[._GEO1K_...HDF]或[._GEOQK_...HDF]");
            }
            return PrjFY3D_MERSI_QKM(qkmRaster, kmRaster, qkmGeoRaster, kmGeoRaster, prjOutArg, progress, out errorMessage);
        }

        private string[] PrjFY3D_MERSI_QKM(AbstractWarpDataset qkmRaster, AbstractWarpDataset kmRaster, AbstractWarpDataset qkmGeoRaster, AbstractWarpDataset kmGeoRaster, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            SpatialReference dstSpatialRef = prjOutArg.ProjectionRef;
            PrjEnvelopeItem[] prjEnvelopes = prjOutArg.Envelopes;
            float outResolutionX = prjOutArg.ResolutionX;
            float outResolutionY = prjOutArg.ResolutionY;
            SetDefaultResolutionForMersi(qkmRaster, dstSpatialRef, ref outResolutionX, ref outResolutionY);
            prjOutArg.ResolutionX = outResolutionX;
            prjOutArg.ResolutionY = outResolutionY;
            string outDir = prjOutArg.OutDirOrFile;

            errorMessage = new StringBuilder();

            int[] kmBands = null, qkmBands = null;
            FileFinder.GetBandmapTableMERSI2(qkmRaster, kmRaster, prjOutArg.SelectedBands, out qkmBands, out kmBands);
            int bandCount = (qkmBands == null ? 0 : qkmBands.Length);
            if (bandCount == 0)
            {
                errorMessage.Append("没有获取要投影的通道");
                return null;
            }
            string bandNames = "BANDNAMES="
                + (qkmBands == null || qkmBands.Length == 0 ? "" : BandNameString(qkmBands) + ",");

            IFileProjector projtor = FileProjector.GetFileProjectByName("FY3D_MERSI");
            try
            {
                List<string> outFiles = new List<string>();
                for (int i = 0; i < prjEnvelopes.Length; i++)
                {
                    string outFileName = null;
                    try
                    {
                        string orbitFileName = null;
                        if (qkmRaster != null)
                            orbitFileName = qkmRaster.fileName;
                        else
                            orbitFileName = kmRaster.fileName;
                        if (IsDir(prjOutArg.OutDirOrFile))
                            outFileName = GetOutPutFile(prjOutArg.OutDirOrFile, orbitFileName, dstSpatialRef, prjEnvelopes[i].Name, null, outResolutionX, ".tif");
                        else
                            outFileName = prjOutArg.OutDirOrFile;
                        outFiles.Add(outFileName);
                    }
                    catch (Exception ex)
                    {
                        TryDeleteErrorFile(outFileName);
                        errorMessage.Append(ex.Message);
                        outFiles.Add(null);
                    }
                }
                bool hasAngle = false;
                int perBandCount = 0;
                if (qkmBands != null && qkmBands.Length != 0)
                {
                    perBandCount = qkmBands.Length;
                    projtor.BeginSession(qkmRaster);
                    for (int i = 0; i < prjEnvelopes.Length; i++)
                    {
                        AbstractWarpDataset outRaster = null;
                        if (outFiles[i] == null)
                            continue;
                        try
                        {
                            //outRaster = OpenUpdate(outFiles[i]);
                            FY3_MERSI_PrjSettings prjSetting = new FY3_MERSI_PrjSettings();
                            prjSetting.SecondaryOrbitRaster = kmRaster;
                            prjSetting.OutResolutionX = outResolutionX;
                            prjSetting.OutResolutionY = outResolutionY;
                            prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                            prjSetting.OutBandNos = qkmBands;
                            prjSetting.OutPathAndFileName = outFiles[i];
                            prjSetting.GeoFile = qkmGeoRaster != null ? qkmGeoRaster : kmGeoRaster;
                            prjSetting.AngleFile = kmGeoRaster;//
                            if (prjOutArg.Args != null)
                            {
                                if (prjOutArg.Args.Contains("NotRadiation"))
                                    prjSetting.IsRadRef = false;
                                if (prjOutArg.Args.Contains("Radiation"))
                                {
                                    prjSetting.IsRadRef = false;
                                    prjSetting.IsRad = true;
                                }
                                if (prjOutArg.Args.Contains("NotSolarZenith"))
                                    prjSetting.IsSolarZenith = false;
                                if (prjOutArg.Args.Contains("IsSensorZenith"))
                                    prjSetting.IsSensorZenith = true;
                                if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                    prjSetting.IsClearPrjCache = true;
                            }
                            if (!hasAngle)//只输出一次角度数据
                            {
                                prjSetting.ExtArgs = prjOutArg.Args;
                                hasAngle = true;
                            }
                            projtor.Project(qkmRaster, prjSetting, dstSpatialRef, progress);
                            //projtor.Project(qkmRaster, prjSetting, outRaster, 0, progress);
                        }
                        catch (Exception ex)
                        {
                            errorMessage.AppendLine(ex.Message);
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            if (outRaster != null)
                            {
                                outRaster.Dispose();
                                outRaster = null;
                            }
                        }
                    }
                    projtor.EndSession();
                }
                return outFiles.ToArray();
            }
            finally
            {
                if (projtor != null)
                    projtor.EndSession();
                if (kmRaster != null)
                    kmRaster.Dispose();
                if (qkmRaster != null)
                    qkmRaster.Dispose();
            }
        }

        private string[] PrjFY3D_MERSI_1KM(AbstractWarpDataset qkmRaster, AbstractWarpDataset kmRaster, AbstractWarpDataset qkmGeoRaster, AbstractWarpDataset kmGeoRaster, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            SpatialReference dstSpatialRef = prjOutArg.ProjectionRef;
            PrjEnvelopeItem[] prjEnvelopes = prjOutArg.Envelopes;
            float outResolutionX = prjOutArg.ResolutionX;
            float outResolutionY = prjOutArg.ResolutionY;
            SetDefaultResolutionForMersi(qkmRaster, dstSpatialRef, ref outResolutionX, ref outResolutionY);
            prjOutArg.ResolutionX = outResolutionX;
            prjOutArg.ResolutionY = outResolutionY;
            string outDir = prjOutArg.OutDirOrFile;

            float baseResolutionK = 0.01f;
            if (dstSpatialRef == null || dstSpatialRef.IsGeographic()==1)
            {
                baseResolutionK = 0.01f;
            }
            else
            {
                baseResolutionK = 1000f;
            }
            if (baseResolutionK / outResolutionX < 1.5 && baseResolutionK / outResolutionY < 1.5)
            {
                if (qkmRaster != null && kmRaster != null)
                {
                    qkmRaster.Dispose();
                    qkmRaster = null;
                }
            }

            errorMessage = new StringBuilder();

            int[] qkmBands = null, kmBands = null;
            FileFinder.GetBandmapTableMERSI2(qkmRaster, kmRaster, prjOutArg.SelectedBands, out qkmBands, out kmBands);
            int bandCount = (qkmBands == null ? 0 : qkmBands.Length) + (kmBands == null ? 0 : kmBands.Length);
            if (bandCount == 0)
            {
                errorMessage.Append("没有获取要投影的通道");
                return null;
            }
            string bandNames = "BANDNAMES="
                + (qkmBands == null || qkmBands.Length == 0 ? "" : BandNameString(qkmBands) + ",")
                + BandNameString(kmBands);
            FY3D_MERSIFileProjector projtor = FileProjector.GetFileProjectByName("FY3D_MERSI") as FY3D_MERSIFileProjector;
            try
            {
                List<string> outFiles = new List<string>();
                AbstractWarpDataset outRaster = null;

                #region 创建输出文件

                for (int i = 0; i < prjEnvelopes.Length; i++)
                {
                    PrjEnvelope envelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                    if (envelope == null)
                    {
                        if (kmGeoRaster != null)
                            projtor.ComputeDstEnvelope(kmGeoRaster, dstSpatialRef, out envelope, null);
                        else
                            projtor.ComputeDstEnvelope(qkmGeoRaster, dstSpatialRef, out envelope, null);
                        if (envelope == null)
                        {
                            errorMessage.Append("未能读取出文件的经纬度范围：" + kmRaster.fileName);
                            continue;
                        }
                    }
                    string outFileName = null;
                    try
                    {
                        string orbitFileName = null;
                        DataIdentify dataIdentify = null;
                        if (qkmRaster != null)
                        {
                            orbitFileName = qkmRaster.fileName;
                            dataIdentify = qkmRaster.DataIdentify;
                        }
                        else
                        {
                            orbitFileName = kmRaster.fileName;
                            dataIdentify = kmRaster.DataIdentify;
                        }
                        if (IsDir(prjOutArg.OutDirOrFile))
                            outFileName = GetOutPutFile(prjOutArg.OutDirOrFile, orbitFileName, dstSpatialRef, prjEnvelopes[i].Name, dataIdentify, outResolutionX, ".ldf");
                        else
                            outFileName = prjOutArg.OutDirOrFile;
                        outRaster = CreateRaster(outFileName, envelope, outResolutionX, outResolutionY, bandCount, dstSpatialRef, bandNames);
                        //int[] bands = new int[] { 1, 2, 3, 4, 24, 25, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23 };
                        
                        FilePrjSettings setting = new FilePrjSettings { OutBandNos = prjOutArg.SelectedBands?? Enumerable.Range(1, 25).ToArray()};
                        FileProjector.WriteMetaData(kmRaster, outRaster, setting);
                        outFiles.Add(outFileName);
                    }
                    catch (Exception ex)
                    {
                        TryDeleteErrorFile(outFileName);
                        errorMessage.Append(ex.Message);
                        outFiles.Add(null);
                    }
                    finally
                    {
                        progress?.Invoke(0, "创建输出文件...");
                        if (outRaster != null)
                        {
                            outRaster.Dispose();
                            outRaster = null;
                        }
                    }
                }

                #endregion 创建输出文件

                bool hasAngle = false;
                int perBandCount = 0;

                #region 设置qkm波段setting

                if (qkmBands != null && qkmBands.Length != 0)
                {
                    perBandCount = qkmBands.Length;
                    projtor.BeginSession(qkmRaster);
                    for (int i = 0; i < prjEnvelopes.Length; i++)
                    {
                        if (outFiles[i] == null)
                            continue;
                        try
                        {
                            outRaster = OpenUpdate(outFiles[i]);
                            FY3_MERSI_PrjSettings prjSetting = new FY3_MERSI_PrjSettings();
                            prjSetting.SecondaryOrbitRaster = kmRaster;
                            prjSetting.OutResolutionX = outResolutionX;
                            prjSetting.OutResolutionY = outResolutionY;
                            prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                            prjSetting.OutBandNos = qkmBands;
                            prjSetting.OutPathAndFileName = outFiles[i];
                            prjSetting.GeoFile = qkmGeoRaster != null ? qkmGeoRaster : kmGeoRaster;
                            prjSetting.AngleFile = kmGeoRaster;//
                            if (prjOutArg.Args != null)
                            {
                                if (prjOutArg.Args.Contains("NotRadiation"))
                                    prjSetting.IsRadRef = false;
                                if (prjOutArg.Args.Contains("Radiation"))
                                {
                                    prjSetting.IsRadRef = false;
                                    prjSetting.IsRad = true;
                                }
                                if (prjOutArg.Args.Contains("NotSolarZenith"))
                                    prjSetting.IsSolarZenith = false;
                                if (prjOutArg.Args.Contains("IsSensorZenith"))
                                    prjSetting.IsSensorZenith = true;
                                if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                    prjSetting.IsClearPrjCache = true;
                            }
                            if (!hasAngle)//只输出一次角度数据
                            {
                                prjSetting.ExtArgs = prjOutArg.Args;
                                hasAngle = true;
                            }
                            projtor.Project(qkmRaster, prjSetting, outRaster, 0, progress);
                        }
                        catch (Exception ex)
                        {
                            errorMessage.AppendLine(ex.Message);
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            if (outRaster != null)
                            {
                                outRaster.Dispose();
                                outRaster = null;
                            }
                        }
                    }
                    projtor.EndSession();
                }

                #endregion 设置qkm波段setting

                #region 设置km波段setting

                if (kmBands != null && kmBands.Length != 0)
                {
                    projtor.BeginSession(qkmRaster);
                    for (int i = 0; i < prjEnvelopes.Length; i++)
                    {
                        if (outFiles[i] == null)
                            continue;
                        try
                        {
                            outRaster = OpenUpdate(outFiles[i]);
                            FY3_MERSI_PrjSettings prjSetting = new FY3_MERSI_PrjSettings();
                            prjSetting.OutPathAndFileName = outFiles[i];
                            prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                            prjSetting.OutResolutionX = outResolutionX;
                            prjSetting.OutResolutionY = outResolutionY;
                            prjSetting.OutBandNos = kmBands;
                            prjSetting.GeoFile = kmGeoRaster != null ? kmGeoRaster : qkmGeoRaster;
                            prjSetting.AngleFile = kmGeoRaster;//
                            if (prjOutArg.Args != null)
                            {
                                if (prjOutArg.Args.Contains("NotRadiation"))
                                    prjSetting.IsRadRef = false;
                                if (prjOutArg.Args.Contains("Radiation"))
                                {
                                    prjSetting.IsRadRef = false;
                                    prjSetting.IsRad = true;
                                }
                                if (prjOutArg.Args.Contains("NotSolarZenith"))
                                    prjSetting.IsSolarZenith = false;
                                if (prjOutArg.Args.Contains("IsSensorZenith"))
                                    prjSetting.IsSensorZenith = true;
                                if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                    prjSetting.IsClearPrjCache = true;
                                if (prjOutArg.Args.Contains("TerrainCorrection"))
                                    prjSetting.IsTerrainCorrection = true;
                                prjSetting.ExtArgs = prjOutArg.Args;
                            }
                            if (!hasAngle)
                            {
                                prjSetting.ExtArgs = prjOutArg.Args;
                                hasAngle = true;
                            }
                            projtor.Project(kmRaster, prjSetting, outRaster, perBandCount, progress);
                        }
                        catch (Exception ex)
                        {
                            errorMessage.AppendLine(ex.Message);
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            if (outRaster != null)
                            {
                                outRaster.Dispose();
                                outRaster = null;
                            }
                        }
                    }
                }

                #endregion 设置km波段setting

                return outFiles.ToArray();
            }
            finally
            {
                if (projtor != null)
                    projtor.EndSession();
                if (kmRaster != null)
                    kmRaster.Dispose();
                if (qkmRaster != null)
                    qkmRaster.Dispose();
                if (prjOutArg.Args != null && prjOutArg.Args.Contains("IsClearPrjCache"))
                    projtor.TryDeleteCurCatch();
            }
        }

        #endregion FY3D_MERSI

        #region FY4A

        private string[] PrjFY4A_L1(AbstractWarpDataset srcRaster, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            //500 1000 2000 4000 融合处理
            SpatialReference dstSpatialRef = prjOutArg.ProjectionRef;
            PrjEnvelopeItem[] prjEnvelopes = prjOutArg.Envelopes;
            FY4A_AGRIFileProjector projector = null;
            int[] kmBands;
            FileFinder.GetBandmapTable(srcRaster, prjOutArg.SelectedBands, out kmBands);
            errorMessage = new StringBuilder();
            try
            {
                List<string> outFiles = new List<string>();
                projector = FileProjector.GetFileProjectByName("FY4A") as FY4A_AGRIFileProjector;
                projector.NOMCenterLon = GetExtArgValue(prjOutArg, "NOMCenterLon=");
                projector.NOMSatHeight = GetExtArgValue(prjOutArg, "NOMSatHeight=");
                projector.BeginSession(srcRaster);
                for (int i = 0; i < prjEnvelopes.Length; i++)
                {
                    string outFileName = null;
                    if (IsDir(prjOutArg.OutDirOrFile))
                        outFileName = GetOutPutFile(prjOutArg.OutDirOrFile, srcRaster.fileName, dstSpatialRef, prjEnvelopes[i].Name, srcRaster.DataIdentify, prjOutArg.ResolutionX, ".ldf");
                    else
                        outFileName = prjOutArg.OutDirOrFile;

                    FY4A_AGRIPrjSetting prjSetting = new FY4A_AGRIPrjSetting();
                    prjSetting.OutPathAndFileName = outFileName;
                    try
                    {
                        prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                        prjSetting.OutBandNos = kmBands;
                        prjSetting.OutResolutionX = prjOutArg.ResolutionX;
                        prjSetting.OutResolutionY = prjOutArg.ResolutionY;
                        if (prjOutArg.Args != null)
                        {
                            if (prjOutArg.Args.Contains("NotRadiation"))
                                prjSetting.IsRadiation = false;
                            if (prjOutArg.Args.Contains("NotSolarZenith"))
                                prjSetting.IsSolarZenith = false;
                            if (prjOutArg.Args.Contains("IsSensorZenith"))
                                prjSetting.IsSensorZenith = true;
                            if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                prjSetting.IsClearPrjCache = true;
                            prjSetting.ExtArgs = prjOutArg.Args;
                        }
                        PrjEnvelope envelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                        projector.Project(srcRaster, prjSetting, dstSpatialRef, progress);
                        outFiles.Add(prjSetting.OutPathAndFileName);
                    }
                    catch (Exception ex)
                    {
                        outFiles.Add(null);
                        errorMessage.AppendLine(ex.Message);
                        TryDeleteErrorFile(outFileName);
                        Console.WriteLine(ex.Message);
                    }
                }
                return outFiles.ToArray();
            }
            finally
            {
                if (projector != null)
                    projector.EndSession();
            }
        }

        private string GetExtArgValue(PrjOutArg arg, string argName)
        {
            string value = string.Empty;
            if (arg.Args != null && arg.Args.Any(t => t.ToString().Contains(argName)))
            {
                string config = arg.Args.First(t => t.ToString().Contains(argName)).ToString();
                value = config.Replace(argName, "");
            }
            return value;
        }

        private string[] PrjFY4A_Mix(AbstractWarpDataset srcRaster, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            AbstractWarpDataset fkmRaster = srcRaster;
            AbstractWarpDataset hkmRaster = null;
            AbstractWarpDataset kmRaster = null;
            AbstractWarpDataset dkmRaster = null;
            string arg500 = GetExtArgValue(prjOutArg, "FY4_0500M=");
            if (string.IsNullOrEmpty(arg500))
                hkmRaster = FileFinder.TryFindFY4A_HKM_FromFKM(srcRaster);
            else
                hkmRaster = WarpDataset.Open(arg500);

            string arg1000 = GetExtArgValue(prjOutArg, "FY4_1000M=");
            if (string.IsNullOrEmpty(arg1000))
                kmRaster = FileFinder.TryFindFY4A_KM_FromFKM(srcRaster);
            else
                kmRaster = WarpDataset.Open(arg1000);

            string arg2000 = GetExtArgValue(prjOutArg, "FY4_2000M=");
            if (string.IsNullOrEmpty(arg2000))
                dkmRaster = FileFinder.TryFindFY4A_DKM_FromFKM(srcRaster);
            else
                dkmRaster = WarpDataset.Open(arg2000);

            //未找到置空
            if (hkmRaster != null && hkmRaster.ds == null)
                hkmRaster = null;
            if (kmRaster != null && kmRaster.ds == null)
                kmRaster = null;
            if (dkmRaster != null && dkmRaster.ds == null)
                dkmRaster = null;

            return PrjFY4A(prjOutArg, out errorMessage, hkmRaster, kmRaster, dkmRaster, fkmRaster, progress);
        }

        private string[] PrjFY4A(PrjOutArg prjOutArg, out StringBuilder errorMessage, AbstractWarpDataset hkmRaster, AbstractWarpDataset kmRaster, AbstractWarpDataset dkmRaster, AbstractWarpDataset fkmRaster, Action<int, string> progress)
        {
            errorMessage = new StringBuilder();
            //默认分辨率，默认参数
            SpatialReference dstSpatialRef = prjOutArg.ProjectionRef;
            float outResolutionX = prjOutArg.ResolutionX;
            float outResolutionY = prjOutArg.ResolutionY;
            SetDefaultResolutionForFY4A(hkmRaster, kmRaster, dkmRaster, fkmRaster, dstSpatialRef, ref outResolutionX, ref outResolutionY);
            prjOutArg.ResolutionX = outResolutionX;
            prjOutArg.ResolutionY = outResolutionY;
            FilterRasterForFY4A(ref hkmRaster, ref kmRaster, ref dkmRaster, ref fkmRaster, dstSpatialRef, outResolutionX, outResolutionY);
            int[] hkmBands, kmBands, dkmBands, fkmBands;
            FileFinder.GetFY4ABandmapTable(hkmRaster, kmRaster, dkmRaster, fkmRaster, ref prjOutArg.SelectedBands, out hkmBands, out kmBands, out dkmBands, out fkmBands);
            int bandCount = 0;
            new List<int[]> { kmBands, dkmBands, fkmBands }.ForEach(t => { if (t == null) bandCount += 0; else bandCount += t.Length; });

            if (bandCount == 0)
            {
                errorMessage.Append("没有获取要投影的通道");
                return null;
            }
            string bandNames = string.Empty;
            //string bandNames = "BANDNAMES="
            //    + (qkmBands == null || qkmBands.Length == 0 ? "" : BandNameString(qkmBands) + ",")
            //    + (hkmBands == null || hkmBands.Length == 0 ? "" : BandNameString(hkmBands) + ",")
            //    + BandNameString(kmBands);
            FY4A_AGRIFileProjector projtor = FileProjector.GetFileProjectByName("FY4A") as FY4A_AGRIFileProjector;
            projtor.NOMCenterLon = GetExtArgValue(prjOutArg, "NOMCenterLon=");
            projtor.NOMSatHeight = GetExtArgValue(prjOutArg, "NOMSatHeight=");
            PrjEnvelopeItem[] prjEnvelopes = prjOutArg.Envelopes;
            try
            {
                List<AbstractWarpDataset> srcRasterList = new List<AbstractWarpDataset> { hkmRaster, kmRaster, dkmRaster, fkmRaster };
                AbstractWarpDataset baseRaster = srcRasterList.Last(t => t != null);
                if (baseRaster == null)
                    throw new ArgumentException("正在进行FY4融合投影，输入源数据均为NULL");
                List<string> outFiles = new List<string>();
                AbstractWarpDataset outRaster = null;
                for (int i = 0; i < prjEnvelopes.Length; i++)
                {
                    PrjEnvelope envelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                    if (envelope == null)
                    {
                        projtor.ComputeDstEnvelope(baseRaster, dstSpatialRef, out envelope, null);
                        if (envelope == null)
                        {
                            errorMessage.Append("未能读取出文件的经纬度范围：" + baseRaster.fileName);
                            continue;
                        }
                        prjEnvelopes[i].PrjEnvelope = envelope;
                    }
                    string outFileName = null;
                    try
                    {
                        if (IsDir(prjOutArg.OutDirOrFile))
                            outFileName = GetOutPutFile(prjOutArg.OutDirOrFile, baseRaster.fileName, dstSpatialRef, prjEnvelopes[i].Name, baseRaster.DataIdentify, prjOutArg.ResolutionX, ".ldf");
                        else
                            outFileName = prjOutArg.OutDirOrFile;
                        outRaster = CreateRaster(outFileName, envelope, outResolutionX, outResolutionY, bandCount, dstSpatialRef, bandNames);
                        FilePrjSettings setting = new FilePrjSettings { OutBandNos = prjOutArg.SelectedBands };
                        FileProjector.WriteMetaData(fkmRaster, outRaster, setting);
                        outFiles.Add(outFileName);
                    }
                    catch (Exception ex)
                    {
                        TryDeleteErrorFile(outFileName);
                        errorMessage.Append(ex.Message);
                        outFiles.Add(null);
                    }
                    finally
                    {
                        progress?.Invoke(0, "创建输出文件...");
                        if (outRaster != null)
                        {
                            outRaster.Dispose();
                            outRaster = null;
                        }
                    }
                }
                bool hasAngle = false;
                int perBandBegin = 0;
                int curBandBegin = 0;

                if (kmBands != null && kmBands.Length != 0)
                {
                    curBandBegin = 0;
                    perBandBegin += kmBands.Length;
                    projtor.BeginSession(kmRaster);
                    for (int i = 0; i < prjEnvelopes.Length; i++)
                    {
                        if (outFiles[i] == null)
                            continue;
                        try
                        {
                            outRaster = OpenUpdate(outFiles[i]);

                            FY4A_AGRIPrjSetting prjSetting = new FY4A_AGRIPrjSetting();
                            prjSetting.OutPathAndFileName = outFiles[i];
                            prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                            prjSetting.OutBandNos = kmBands;
                            prjSetting.OutResolutionX = prjOutArg.ResolutionX;
                            prjSetting.OutResolutionY = prjOutArg.ResolutionY;
                            if (prjOutArg.Args != null)
                            {
                                if (prjOutArg.Args.Contains("NotRadiation"))
                                    prjSetting.IsRadiation = false;
                                if (prjOutArg.Args.Contains("NotSolarZenith"))
                                    prjSetting.IsSolarZenith = false;
                                if (prjOutArg.Args.Contains("IsSensorZenith"))
                                    prjSetting.IsSensorZenith = true;
                                if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                    prjSetting.IsClearPrjCache = true;
                            }
                            if (!hasAngle)
                            {
                                prjSetting.ExtArgs = prjOutArg.Args;
                                hasAngle = true;
                            }
                            projtor.Project(kmRaster, prjSetting, outRaster, curBandBegin, progress);
                        }
                        catch (Exception ex)
                        {
                            errorMessage.AppendLine(ex.Message);
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            if (outRaster != null)
                            {
                                outRaster.Dispose();
                                outRaster = null;
                            }
                        }
                    }
                    projtor.EndSession();
                }
                if (dkmBands != null && dkmBands.Length != 0)
                {
                    curBandBegin = perBandBegin;
                    perBandBegin += dkmBands.Length;
                    projtor.BeginSession(dkmRaster);
                    for (int i = 0; i < prjEnvelopes.Length; i++)
                    {
                        if (outFiles[i] == null)
                            continue;
                        try
                        {
                            outRaster = OpenUpdate(outFiles[i]);
                            FY4A_AGRIPrjSetting prjSetting = new FY4A_AGRIPrjSetting();
                            prjSetting.OutPathAndFileName = outFiles[i];
                            prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                            prjSetting.OutBandNos = dkmBands;
                            prjSetting.OutResolutionX = prjOutArg.ResolutionX;
                            prjSetting.OutResolutionY = prjOutArg.ResolutionY;
                            if (prjOutArg.Args != null)
                            {
                                if (prjOutArg.Args.Contains("NotRadiation"))
                                    prjSetting.IsRadiation = false;
                                if (prjOutArg.Args.Contains("NotSolarZenith"))
                                    prjSetting.IsSolarZenith = false;
                                if (prjOutArg.Args.Contains("IsSensorZenith"))
                                    prjSetting.IsSensorZenith = true;
                                if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                    prjSetting.IsClearPrjCache = true;
                            }
                            if (!hasAngle)
                            {
                                prjSetting.ExtArgs = prjOutArg.Args;
                                hasAngle = true;
                            }
                            projtor.Project(dkmRaster, prjSetting, outRaster, curBandBegin, progress);
                        }
                        catch (Exception ex)
                        {
                            errorMessage.AppendLine(ex.Message);
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            if (outRaster != null)
                            {
                                outRaster.Dispose();
                                outRaster = null;
                            }
                        }
                    }
                    projtor.EndSession();
                }
                if (fkmBands != null && fkmBands.Length != 0)
                {
                    curBandBegin = perBandBegin;
                    perBandBegin += fkmBands.Length;
                    projtor.BeginSession(fkmRaster);
                    for (int i = 0; i < prjEnvelopes.Length; i++)
                    {
                        if (outFiles[i] == null)
                            continue;
                        try
                        {
                            outRaster = OpenUpdate(outFiles[i]);
                            FY4A_AGRIPrjSetting prjSetting = new FY4A_AGRIPrjSetting();
                            prjSetting.OutPathAndFileName = outFiles[i];
                            prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                            prjSetting.OutBandNos = fkmBands;
                            prjSetting.OutResolutionX = prjOutArg.ResolutionX;
                            prjSetting.OutResolutionY = prjOutArg.ResolutionY;
                            if (prjOutArg.Args != null)
                            {
                                if (prjOutArg.Args.Contains("NotRadiation"))
                                    prjSetting.IsRadiation = false;
                                if (prjOutArg.Args.Contains("NotSolarZenith"))
                                    prjSetting.IsSolarZenith = false;
                                if (prjOutArg.Args.Contains("IsSensorZenith"))
                                    prjSetting.IsSensorZenith = true;
                                if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                    prjSetting.IsClearPrjCache = true;
                            }
                            if (!hasAngle)
                            {
                                prjSetting.ExtArgs = prjOutArg.Args;
                                hasAngle = true;
                            }
                            projtor.Project(fkmRaster, prjSetting, outRaster, curBandBegin, progress);
                        }
                        catch (Exception ex)
                        {
                            errorMessage.AppendLine(ex.Message);
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            if (outRaster != null)
                            {
                                outRaster.Dispose();
                                outRaster = null;
                            }
                        }
                    }
                    projtor.EndSession();
                }

                if (hkmBands != null && hkmBands.Length != 0)
                {
                    curBandBegin = 0;
                    projtor.BeginSession(hkmRaster);
                    for (int i = 0; i < prjEnvelopes.Length; i++)
                    {
                        if (outFiles[i] == null)
                            continue;
                        try
                        {
                            outRaster = OpenUpdate(outFiles[i]);

                            FY4A_AGRIPrjSetting prjSetting = new FY4A_AGRIPrjSetting();
                            prjSetting.OutPathAndFileName = outFiles[i];
                            prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                            prjSetting.OutBandNos = new int[] { 1 };
                            prjSetting.OutResolutionX = prjOutArg.ResolutionX;
                            prjSetting.OutResolutionY = prjOutArg.ResolutionY;

                            if (prjOutArg.Args != null)
                            {
                                if (prjOutArg.Args.Contains("NotRadiation"))
                                    prjSetting.IsRadiation = false;
                                if (prjOutArg.Args.Contains("NotSolarZenith"))
                                    prjSetting.IsSolarZenith = false;
                                if (prjOutArg.Args.Contains("IsSensorZenith"))
                                    prjSetting.IsSensorZenith = true;
                                if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                    prjSetting.IsClearPrjCache = true;
                            }
                            if (!hasAngle)
                            {
                                prjSetting.ExtArgs = prjOutArg.Args;
                                hasAngle = true;
                            }
                            projtor.Project(hkmRaster, prjSetting, outRaster, hkmBands[0], progress);
                        }
                        catch (Exception ex)
                        {
                            errorMessage.AppendLine(ex.Message);
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            if (outRaster != null)
                            {
                                outRaster.Dispose();
                                outRaster = null;
                            }
                        }
                    }
                    projtor.EndSession();
                }
                return outFiles.ToArray();
            }
            finally
            {
                if (projtor != null)
                    projtor.EndSession();
                if (kmRaster != null)
                    kmRaster.Dispose();
                if (hkmRaster != null)
                    hkmRaster.Dispose();
                if (dkmRaster != null)
                    dkmRaster.Dispose();
                if (fkmRaster != null)
                    fkmRaster.Dispose();
            }
        }

        /// <summary>
        /// 释放用不到的数据集
        /// </summary>
        private void FilterRasterForFY4A(ref AbstractWarpDataset hkmRaster, ref AbstractWarpDataset kmRaster, ref AbstractWarpDataset dkmRaster, ref AbstractWarpDataset fkmRaster, SpatialReference dstSpatialRef, float outResolutionX, float outResolutionY)
        {
            float baseResolution = 0;

            AbstractWarpDataset[] dsList = new AbstractWarpDataset[] { hkmRaster, kmRaster, dkmRaster, fkmRaster };
            List<Tuple<float, float>> resList = new List<Tuple<float, float>>
            {
                Tuple.Create(0.005F,500F),Tuple.Create(0.01F,1000F),
                Tuple.Create(0.02F,2000F),Tuple.Create(0.04F,4000F),
            };
            for (int i = 0; i < 3; i++)
            {
                if (dsList[i] != null)
                {
                    if (dstSpatialRef.IsGeographic()==1)
                        baseResolution = resList[i].Item1;
                    else
                        baseResolution = resList[i].Item2;
                    if (outResolutionX > baseResolution)
                    {
                        dsList[i].Dispose();
                        dsList[i] = null;
                    }
                }
            }
            hkmRaster = dsList[0]; kmRaster = dsList[1]; dkmRaster = dsList[2]; fkmRaster = dsList[3];
        }

        private void SetDefaultResolutionForFY4A(AbstractWarpDataset hkmRaster, AbstractWarpDataset kmRaster, AbstractWarpDataset dkmRaster, AbstractWarpDataset fkmRaster, SpatialReference dstSpatialRef, ref float outResolutionX, ref float outResolutionY)
        {
            List<Tuple<AbstractWarpDataset, float, float>> resDic = new List<Tuple<AbstractWarpDataset, float, float>>
            {
                Tuple.Create(hkmRaster,0.005F,500F),
                Tuple.Create(kmRaster,0.01F,1000F),
                Tuple.Create(dkmRaster,0.02F,2000F),
                Tuple.Create(fkmRaster,0.04F,4000F),
            };
            if (outResolutionX == 0 || outResolutionY == 0)
            {
                foreach (var item in resDic)
                {
                    if (item.Item1 != null)
                    {
                        if (dstSpatialRef.IsGeographic()==1)
                        {
                            outResolutionX = item.Item2;
                            outResolutionY = item.Item2;
                        }
                        else
                        {
                            outResolutionX = item.Item3;//投影坐标系
                            outResolutionY = item.Item3;
                        }
                    }
                }
            }
        }

        #endregion FY4A

        #region FY2X

        private string[] PrjFY2X_NOM(AbstractWarpDataset srcRaster, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            SpatialReference dstSpatialRef = prjOutArg.ProjectionRef;
            PrjEnvelopeItem[] prjEnvelopes = prjOutArg.Envelopes;
            IFileProjector projector = null;
            int[] kmBands;
            FileFinder.GetBandmapTable(srcRaster, prjOutArg.SelectedBands, out kmBands);
            errorMessage = new StringBuilder();
            try
            {
                List<string> outFiles = new List<string>();
                projector = FileProjector.GetFileProjectByName("FY2");
                projector.BeginSession(srcRaster);
                for (int i = 0; i < prjEnvelopes.Length; i++)
                {
                    string outFileName = null;
                    if (IsDir(prjOutArg.OutDirOrFile))
                        outFileName = GetOutPutFile(prjOutArg.OutDirOrFile, srcRaster.fileName, dstSpatialRef, prjEnvelopes[i].Name, srcRaster.DataIdentify, prjOutArg.ResolutionX, ".ldf");
                    else
                        outFileName = prjOutArg.OutDirOrFile;

                    Fy2_NOM_PrjSettings prjSetting = new Fy2_NOM_PrjSettings();
                    prjSetting.OutPathAndFileName = outFileName;
                    try
                    {
                        prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                        prjSetting.OutBandNos = kmBands;
                        prjSetting.OutResolutionX = prjOutArg.ResolutionX;
                        prjSetting.OutResolutionY = prjOutArg.ResolutionY;
                        if (prjOutArg.Args != null)
                        {
                            if (prjOutArg.Args.Contains("NotRadiation"))
                                prjSetting.IsRadiation = false;
                            if (prjOutArg.Args.Contains("NotSolarZenith"))
                                prjSetting.IsSolarZenith = false;
                            if (prjOutArg.Args.Contains("IsSensorZenith"))
                                prjSetting.IsSensorZenith = true;
                            if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                prjSetting.IsClearPrjCache = true;
                            prjSetting.ExtArgs = prjOutArg.Args;
                        }
                        PrjEnvelope envelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                        //projector.ComputeDstEnvelope(srcRaster, dstSpatialRef, out envelope, progress);
                        projector.Project(srcRaster, prjSetting, dstSpatialRef, progress);
                        outFiles.Add(prjSetting.OutPathAndFileName);
                    }
                    catch (Exception ex)
                    {
                        outFiles.Add(null);
                        errorMessage.AppendLine(ex.Message);
                        TryDeleteErrorFile(outFileName);
                        Console.WriteLine(ex.Message);
                    }
                }
                return outFiles.ToArray();
            }
            finally
            {
                if (projector != null)
                    projector.EndSession();
            }
        }

        #endregion FY2X

        #region FY3_MERSI

        private string[] PrjMERSI_QKM_L1(AbstractWarpDataset fileName, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            AbstractWarpDataset qkmRaster = fileName;
            AbstractWarpDataset kmRaster = FileFinder.TryFindMERSI_1KM_L1FromQKM(qkmRaster);
            return PrjMersi(qkmRaster, kmRaster, prjOutArg, progress, out errorMessage);
        }

        private string[] PrjMERSI_1KM_L1(AbstractWarpDataset fileName, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            AbstractWarpDataset qkmRaster = FileFinder.TryFindMERSI_QKM_L1FromKM(fileName);
            AbstractWarpDataset kmRaster = fileName;
            return PrjMersi(qkmRaster, kmRaster, prjOutArg, progress, out errorMessage);
        }

        private string[] PrjMersi(AbstractWarpDataset qkmRaster, AbstractWarpDataset kmRaster, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            SpatialReference dstSpatialRef = prjOutArg.ProjectionRef;
            PrjEnvelopeItem[] prjEnvelopes = prjOutArg.Envelopes;
            float outResolutionX = prjOutArg.ResolutionX;
            float outResolutionY = prjOutArg.ResolutionY;
            SetDefaultResolutionForMersi(qkmRaster, dstSpatialRef, ref outResolutionX, ref outResolutionY);
            prjOutArg.ResolutionX = outResolutionX;
            prjOutArg.ResolutionY = outResolutionY;
            string outDir = prjOutArg.OutDirOrFile;

            float baseResolutionK = 0.01f;
            if (dstSpatialRef == null || dstSpatialRef.IsGeographic()==1)
            {
                baseResolutionK = 0.01f;
            }
            else
            {
                baseResolutionK = 1000f;
            }
            if (baseResolutionK / outResolutionX < 1.5 && baseResolutionK / outResolutionY < 1.5)
            {
                if (qkmRaster != null && kmRaster != null)
                {
                    qkmRaster.Dispose();
                    qkmRaster = null;
                }
            }

            errorMessage = new StringBuilder();

            int[] qkmBands = null;
            int[] kmBands = null;
            FileFinder.GetBandmapTableMERSI(qkmRaster, kmRaster, prjOutArg.SelectedBands, out qkmBands, out kmBands);
            int bandCount = (qkmBands == null ? 0 : qkmBands.Length) + (kmBands == null ? 0 : kmBands.Length);
            if (bandCount == 0)
            {
                errorMessage.Append("没有获取要投影的通道");
                return null;
            }
            string bandNames = "BANDNAMES="
                + (qkmBands == null || qkmBands.Length == 0 ? "" : BandNameString(qkmBands) + ",")
                + BandNameString(kmBands);
            IFileProjector projtor = FileProjector.GetFileProjectByName("FY3_MERSI");
            try
            {
                List<string> outFiles = new List<string>();
                AbstractWarpDataset outRaster = null;
                for (int i = 0; i < prjEnvelopes.Length; i++)
                {
                    PrjEnvelope envelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                    if (envelope == null)
                    {
                        projtor.ComputeDstEnvelope(kmRaster, dstSpatialRef, out envelope, null);
                        if (envelope == null)
                        {
                            errorMessage.Append("未能读取出文件的经纬度范围：" + kmRaster.fileName);
                            continue;
                        }
                    }
                    string outFileName = null;
                    try
                    {
                        string orbitFileName = qkmRaster != null ? qkmRaster.fileName : (kmRaster != null ? kmRaster.fileName : null);
                        if (IsDir(prjOutArg.OutDirOrFile))
                            outFileName = GetOutPutFile(prjOutArg.OutDirOrFile, orbitFileName, dstSpatialRef, prjEnvelopes[i].Name, kmRaster.DataIdentify, outResolutionX, ".ldf");
                        else
                            outFileName = prjOutArg.OutDirOrFile;
                        outRaster = CreateRaster(outFileName, envelope, outResolutionX, outResolutionY, bandCount, dstSpatialRef, bandNames);
                        FilePrjSettings setting = new FilePrjSettings { OutBandNos = prjOutArg.SelectedBands };
                        FileProjector.WriteMetaData(kmRaster, outRaster, setting);
                        outFiles.Add(outFileName);
                    }
                    catch (Exception ex)
                    {
                        TryDeleteErrorFile(outFileName);
                        errorMessage.Append(ex.Message);
                        outFiles.Add(null);
                    }
                    finally
                    {
                        progress?.Invoke(0, "创建输出文件...");
                        if (outRaster != null)
                        {
                            outRaster.Dispose();
                            outRaster = null;
                        }
                    }
                }
                bool hasAngle = false;
                int perBandCount = 0;
                List<string> errorFiles = new List<string>();
                if (qkmBands != null && qkmBands.Length != 0)
                {
                    perBandCount = qkmBands.Length;
                    projtor.BeginSession(qkmRaster);
                    for (int i = 0; i < prjEnvelopes.Length; i++)
                    {
                        if (outFiles[i] == null)
                            continue;
                        try
                        {
                            outRaster = OpenUpdate(outFiles[i]);
                            FY3_MERSI_PrjSettings prjSetting = new FY3_MERSI_PrjSettings();
                            prjSetting.SecondaryOrbitRaster = kmRaster;
                            prjSetting.OutResolutionX = outResolutionX;
                            prjSetting.OutResolutionY = outResolutionY;
                            prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                            prjSetting.OutBandNos = qkmBands;
                            prjSetting.OutPathAndFileName = outFiles[i];
                            if (prjOutArg.Args != null)
                            {
                                if (prjOutArg.Args.Contains("NotRadiation"))
                                    prjSetting.IsRadRef = false;
                                if (prjOutArg.Args.Contains("Radiation"))
                                {
                                    prjSetting.IsRadRef = false;
                                    prjSetting.IsRad = true;
                                }
                                if (prjOutArg.Args.Contains("NotSolarZenith"))
                                    prjSetting.IsSolarZenith = false;
                                if (prjOutArg.Args.Contains("IsSensorZenith"))
                                    prjSetting.IsSensorZenith = true;
                                if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                    prjSetting.IsClearPrjCache = true;
                            }
                            if (!hasAngle)
                            {
                                prjSetting.ExtArgs = prjOutArg.Args;
                                hasAngle = true;
                            }
                            projtor.Project(qkmRaster, prjSetting, outRaster, 0, progress);
                        }
                        catch (Exception ex)
                        {
                            errorMessage.AppendLine(ex.Message);
                            Console.WriteLine(ex.Message);
                            errorFiles.Add(outFiles[i]);
                            outFiles[i] = null;
                        }
                        finally
                        {
                            if (outRaster != null)
                            {
                                outRaster.Dispose();
                                outRaster = null;
                            }
                        }
                    }
                    projtor.EndSession();
                }

                if (kmBands != null && kmBands.Length != 0)
                {
                    projtor.BeginSession(qkmRaster);
                    for (int i = 0; i < prjEnvelopes.Length; i++)
                    {
                        if (outFiles[i] == null)
                            continue;
                        try
                        {
                            outRaster = OpenUpdate(outFiles[i]);
                            FY3_MERSI_PrjSettings prjSetting = new FY3_MERSI_PrjSettings();
                            prjSetting.OutPathAndFileName = outFiles[i];
                            prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                            prjSetting.OutResolutionX = outResolutionX;
                            prjSetting.OutResolutionY = outResolutionY;
                            prjSetting.OutBandNos = kmBands;
                            if (prjOutArg.Args != null)
                            {
                                if (prjOutArg.Args.Contains("NotRadiation"))
                                    prjSetting.IsRadRef = false;
                                if (prjOutArg.Args.Contains("Radiation"))
                                {
                                    prjSetting.IsRadRef = false;
                                    prjSetting.IsRad = true;
                                }
                                if (prjOutArg.Args.Contains("NotSolarZenith"))
                                    prjSetting.IsSolarZenith = false;
                                if (prjOutArg.Args.Contains("IsSensorZenith"))
                                    prjSetting.IsSensorZenith = true;
                                if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                    prjSetting.IsClearPrjCache = true;
                                prjSetting.ExtArgs = prjOutArg.Args;
                            }
                            if (!hasAngle)
                            {
                                prjSetting.ExtArgs = prjOutArg.Args;
                                hasAngle = true;
                            }
                            projtor.Project(kmRaster, prjSetting, outRaster, perBandCount, progress);
                        }
                        catch (Exception ex)
                        {
                            errorMessage.AppendLine(ex.Message);
                            Console.WriteLine(ex.Message);
                            errorFiles.Add(outFiles[i]);
                            outFiles[i] = null;
                        }
                        finally
                        {
                            if (outRaster != null)
                            {
                                outRaster.Dispose();
                                outRaster = null;
                            }
                        }
                    }
                }
                foreach (string errorfile in errorFiles)
                {
                    TryDeleteErrorFile(errorfile);
                }
                return outFiles.ToArray();
            }
            finally
            {
                if (projtor != null)
                    projtor.EndSession();
                if (kmRaster != null)
                    kmRaster.Dispose();
            }
        }

        #endregion FY3_MERSI

        #region PROJECTED

        private string[] PrjProjected(AbstractWarpDataset srcRaster, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            IFileProjector projTor = null;
            errorMessage = new StringBuilder();
            try
            {
                FileFinder.TryFindMODIS_HKM_L1From03(srcRaster);

                List<string> outFiles = new List<string>();
                projTor = FileProjector.GetFileProjectByName("ProjectedTransform");
                projTor.BeginSession(srcRaster);
                PrjEnvelopeItem[] prjEnvelopes = prjOutArg.Envelopes;
                var srsSpatialRef = srcRaster.SpatialRef;
                if (prjOutArg.ResolutionX == 0)
                {
                    if (IsGeoSpatial(prjOutArg.ProjectionRef))
                    {
                        if (IsGeoSpatial(srsSpatialRef))
                        {
                            prjOutArg.ResolutionX = srcRaster.ResolutionX;
                            prjOutArg.ResolutionY = srcRaster.ResolutionY;
                        }
                        else
                        {
                            prjOutArg.ResolutionX = srcRaster.ResolutionX / 100000f;
                            prjOutArg.ResolutionY = srcRaster.ResolutionY / 100000f;
                        }
                    }
                    else
                    {
                        if (IsGeoSpatial(srsSpatialRef))
                        {
                            prjOutArg.ResolutionX = srcRaster.ResolutionX * 100000f;
                            prjOutArg.ResolutionY = srcRaster.ResolutionY * 100000f;
                        }
                        else
                        {
                            prjOutArg.ResolutionX = srcRaster.ResolutionX;
                            prjOutArg.ResolutionY = srcRaster.ResolutionY;
                        }
                    }
                }
                for (int i = 0; i < prjEnvelopes.Length; i++)
                {
                    string outFileName = null;
                    try
                    {
                        if (IsDir(prjOutArg.OutDirOrFile))
                            if (srcRaster.DriverName == "MEM")
                                outFileName = GetOutPutFile(prjOutArg.OutDirOrFile, srcRaster.fileName, prjOutArg.ProjectionRef, prjEnvelopes[i].Name, srcRaster.DataIdentify, prjOutArg.ResolutionX, ".dat");
                            else
                                outFileName = GetOutPutFile(prjOutArg.OutDirOrFile, srcRaster.fileName, prjOutArg.ProjectionRef, prjEnvelopes[i].Name, srcRaster.DataIdentify, prjOutArg.ResolutionX, ".ldf");
                        else
                            outFileName = prjOutArg.OutDirOrFile;

                        FilePrjSettings prjSetting = new FilePrjSettings();
                        prjSetting.OutPathAndFileName = outFileName;
                        prjSetting.OutResolutionX = prjOutArg.ResolutionX;
                        prjSetting.OutResolutionY = prjOutArg.ResolutionY;
                        prjSetting.OutEnvelope = prjEnvelopes[i].PrjEnvelope;
                        SpatialReference dstSpatialRef = prjOutArg.ProjectionRef;

                        projTor.Project(srcRaster, prjSetting, dstSpatialRef, progress);
                        outFiles.Add(prjSetting.OutPathAndFileName);
                    }
                    catch (Exception ex)
                    {
                        TryDeleteErrorFile(outFileName);
                        errorMessage.AppendLine(ex.Message);
                        Console.WriteLine(ex.Message);
                    }
                }
                return outFiles.ToArray();
            }
            finally
            {
                if (projTor != null)
                    projTor.EndSession();
            }
        }

        #endregion PROJECTED

        private static void SetDefaultResolutionForMersi(AbstractWarpDataset qkmRaster, SpatialReference dstSpatialRef, ref float outResolutionX, ref float outResolutionY)
        {
            if (outResolutionX == 0 || outResolutionY == 0)
            {
                if (qkmRaster != null)
                {
                    if (dstSpatialRef.IsGeographic()==1)
                    {
                        outResolutionX = 0.0025F;
                        outResolutionY = 0.0025F;
                    }
                    else
                    {
                        outResolutionX = 250F;//投影坐标系
                        outResolutionY = 250F;
                    }
                }
                else
                {
                    if (dstSpatialRef.IsGeographic()==1)
                    {
                        outResolutionX = 0.01f;
                        outResolutionY = 0.01f;
                    }
                    else
                    {
                        outResolutionX = 1000;//投影坐标系
                        outResolutionY = 1000;
                    }
                }
            }
        }

        private AbstractWarpDataset CreateRaster(string outfilename, PrjEnvelope envelope, float outResolutionX, float outResolutionY, int bandCount, SpatialReference spatialRef, string bandNames)
        {
            Size outSize = envelope.GetSize(outResolutionX, outResolutionY);
            string[] options = new string[]{
                "INTERLEAVE=BSQ",
                "VERSION=LDF",
                "WITHHDR=TRUE",
                "SPATIALREF=" + spatialRef.ExportToProj4(),
                "MAPINFO={" + 1 + "," + 1 + "}:{" + envelope.MinX + "," + envelope.MaxY + "}:{" + outResolutionX + "," + outResolutionY + "}"
                ,bandNames
            };
            PrjEnvelope env = new PrjEnvelope()
            {
                Srs = spatialRef,
                MaxX = envelope.MaxX,
                MinX = envelope.MinX,
                MaxY = envelope.MaxY,
                MinY = envelope.MinY
            };

            return CreateOutFile(outfilename, bandCount, outSize, env, outResolutionX, outResolutionY);
        }

        private AbstractWarpDataset CreateOutFile(string outfilename, int dstBandCount, Size outSize, PrjEnvelope env, float outResolutionX, float outResolutionY)
        {
            string dir = Path.GetDirectoryName(outfilename);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            string[] _options = new string[] { "header_offset=128" };
            double[] geoTrans = new double[] { env.MinX, Convert.ToDouble(outResolutionX.ToString("f6")), 0, env.MaxY, 0, -Convert.ToDouble(outResolutionY.ToString("f6")) };
            var ds = DatasetFactory.CreateRasterDataset(outfilename, outSize.Width, outSize.Height, dstBandCount, DataType.GDT_UInt16, "ENVI", null);
            ds.SetGeoTransform(geoTrans);
            if (ds == null)
                throw new ArgumentException("请检查输出文件路径");
            ds.SetProjection(env.Srs.ExportToWkt());
            //设置无效值
            //Enumerable.Range(0, dstBandCount).ToList().ForEach(t => ds.GetRasterBand(t).SetNoDataValue(0));
            return new WarpDataset(ds,outfilename);
        }

        public static PrjOutArg GetDefaultArg(string fileName)
        {
            AbstractWarpDataset rad = null;
            try
            {
                var ds = Gdal.OpenShared(fileName, Access.GA_ReadOnly);
                rad = new WarpDataset(ds,fileName);
                FileChecker checker = new FileChecker();
                string type = checker.GetFileType(rad);
                switch (type)
                {
                    case "VIRR_L1":
                        return new PrjOutArg("", new PrjEnvelopeItem[] { null }, 0.01f, 0.01f, Path.GetDirectoryName(fileName));

                    case "MERSI_1KM_L1":
                        return new PrjOutArg("", new PrjEnvelopeItem[] { null }, 0.01f, 0.01f, Path.GetDirectoryName(fileName));

                    case "MERSI_QKM_L1":
                    case "MERSI_250M_L1":
                        return new PrjOutArg("", new PrjEnvelopeItem[] { null }, 0.0025f, 0.0025f, Path.GetDirectoryName(fileName));

                    case "MODIS_1KM_L1":
                        return new PrjOutArg("", new PrjEnvelopeItem[] { null }, 0.0025f, 0.0025f, Path.GetDirectoryName(fileName));

                    case "NOAA_1BD":
                    case "FY1X_1A5":
                        return new PrjOutArg("", new PrjEnvelopeItem[] { null }, 0.01f, 0.01f, Path.GetDirectoryName(fileName));

                    case "FY2NOM":
                        return new PrjOutArg("", new PrjEnvelopeItem[] { null }, 0.05f, 0.05f, Path.GetDirectoryName(fileName));

                    default:
                        return null;
                }
            }
            finally
            {
                if (rad != null)
                    rad.Dispose();
            }
        }

        public static AbstractWarpDataset OpenUpdate(string filename)
        {
            var ds = Gdal.OpenShared(filename, Access.GA_Update);
            return new WarpDataset(ds,filename);
        }

        public static AbstractWarpDataset CheckPrjArg(AbstractWarpDataset rasterIn)
        {
            string fileType = new FileChecker().GetFileType(rasterIn);
            switch (fileType)
            {
                case "VIRR_L1":
                case "FY1X_1A5":
                case "MERSI_1KM_L1":
                case "MODIS_1KM_L1":
                case "NOAA_1BD":
                case "PROJECTED":
                case "FY2NOM":
                case "FY4A_AGRI_4000":
                case "FY4A_AGRI_0500":
                case "FY4A_AGRI_1000":
                case "FY4A_AGRI_2000":
                    return rasterIn;

                case "FY3C_VIRR_L1":
                    using (AbstractWarpDataset geo = FileFinder.TryFindGeoFileFromFY3C_VIRR(rasterIn))
                    {
                        return rasterIn;
                    }
                case "FY3C_MERSI_1KM_L1":
                case "FY3C_MERSI_QKM_L1":
                    using (AbstractWarpDataset geo = FileFinder.TryFindGeoFileFromFY3C_MERSI(rasterIn))
                    {
                        return rasterIn;
                    }
                case "FY3D_MERSI_1KM_L1":
                case "FY3D_MERSI_QKM_L1":
                    using (AbstractWarpDataset geo = FileFinder.TryFindGeoFileFromFY3C_MERSI(rasterIn))
                    {
                        return rasterIn;
                    }
                case "MERSI_QKM_L1":
                case "MERSI_250M_L1":
                    return FileFinder.TryFindMERSI_1KM_L1FromQKM(rasterIn);

                case "MODIS_HKM_L1":
                    using (AbstractWarpDataset mod03 = FileFinder.TryFind03FileFromModisImgFile(rasterIn))
                    {
                        return FileFinder.TryFindMODIS_1KM_L1From03(mod03);
                    }
                case "MODIS_QKM_L1":
                    using (AbstractWarpDataset mod03 = FileFinder.TryFind03FileFromModisImgFile(rasterIn))
                    {
                        return FileFinder.TryFindMODIS_1KM_L1From03(mod03);
                    }
                default:
                    return null;
            }
        }

        public static PrjEnvelope GetEnvelope(AbstractWarpDataset srcRaster)
        {
            string fileType = new FileChecker().GetFileType(srcRaster);
            if (string.IsNullOrWhiteSpace(fileType))
                throw new Exception("暂未支持该类数据的投影");
            PrjEnvelope env = null;
            IFileProjector projector;
            switch (fileType)
            {
                case "VIRR_L1":
                    projector = FileProjector.GetFileProjectByName("FY3_VIRR");
                    projector.ComputeDstEnvelope(srcRaster, SpatialReferenceFactory.CreateSpatialReference(4326), out env, null);
                    break;

                case "MERSI_1KM_L1":
                case "MERSI_QKM_L1":
                case "MERSI_250M_L1":
                    projector = FileProjector.GetFileProjectByName("FY3_MERSI");
                    projector.ComputeDstEnvelope(srcRaster, SpatialReferenceFactory.CreateSpatialReference(4326), out env, null);
                    break;

                case "MODIS_1KM_L1":
                    projector = FileProjector.GetFileProjectByName("EOS");
                    using (AbstractWarpDataset locationRaster = FileFinder.TryFind03FileFromModisImgFile(srcRaster))
                    {
                        projector.ComputeDstEnvelope(locationRaster, SpatialReferenceFactory.CreateSpatialReference(4326), out env, null);
                    }
                    break;

                case "NOAA_1BD":
                    projector = FileProjector.GetFileProjectByName("NOAA_1BD");
                    projector.ComputeDstEnvelope(srcRaster, SpatialReferenceFactory.CreateSpatialReference(4326), out env, null);
                    break;

                case "FY2NOM":
                    projector = FileProjector.GetFileProjectByName("FY2NOM");
                    projector.ComputeDstEnvelope(srcRaster, SpatialReferenceFactory.CreateSpatialReference(4326), out env, null);
                    break;

                case "PROJECTED":
                    Envelope coord = srcRaster.GetEnvelope();
                    if (coord != null)
                        env = new PrjEnvelope(coord.MinX, coord.MaxX, coord.MinY, coord.MaxY, srcRaster.SpatialRef);
                    break;
            }
            return env;
        }

        /// <summary>
        /// 数据在指定的经纬度内
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        public static bool HasInvildEnvelope(AbstractWarpDataset raster, PrjEnvelope invalidEnv)
        {
            string fileType = new FileChecker().GetFileType(raster);
            if (string.IsNullOrWhiteSpace(fileType))
                throw new Exception("暂未支持该类数据的投影");
            PrjEnvelope env = null;
            IFileProjector projector;
            bool hasVaild = false;
            switch (fileType)
            {
                case "FY3C_VIRR_L1":
                    projector = FileProjector.GetFileProjectByName("FY3C_VIRR");
                    using (AbstractWarpDataset locationRaster = FileFinder.TryFindGeoFileFromFY3C_VIRR(raster))
                    {
                        hasVaild = projector.HasVaildEnvelope(locationRaster, invalidEnv, SpatialReferenceFactory.CreateSpatialReference(4326));
                    }
                    break;

                case "FY3C_MERSI_1KM_L1":
                case "FY3C_MERSI_QKM_L1":
                    projector = FileProjector.GetFileProjectByName("FY3C_MERSI");
                    using (AbstractWarpDataset locationRaster = FileFinder.TryFindGeoFileFromFY3C_MERSI(raster))
                    {
                        hasVaild = projector.HasVaildEnvelope(locationRaster, invalidEnv, SpatialReferenceFactory.CreateSpatialReference(4326));
                    }
                    break;

                case "FY3D_MERSI_1KM_L1":
                    projector = FileProjector.GetFileProjectByName("FY3D_MERSI");
                    using (AbstractWarpDataset locationRaster = FileFinder.TryFindGeoFileFromFY3C_MERSI(raster))
                    {
                        hasVaild = projector.HasVaildEnvelope(locationRaster, invalidEnv, SpatialReferenceFactory.CreateSpatialReference(4326));
                    }
                    break;

                case "VIRR_L1":
                    projector = FileProjector.GetFileProjectByName("FY3_VIRR");
                    hasVaild = projector.HasVaildEnvelope(raster, invalidEnv, SpatialReferenceFactory.CreateSpatialReference(4326));
                    break;

                case "MERSI_1KM_L1":
                case "MERSI_QKM_L1":
                case "MERSI_250M_L1":
                    projector = FileProjector.GetFileProjectByName("FY3_MERSI");
                    hasVaild = projector.HasVaildEnvelope(raster, invalidEnv, SpatialReferenceFactory.CreateSpatialReference(4326));
                    break;

                case "MODIS_1KM_L1":
                case "MODIS_HKM_L1":
                case "MODIS_QKM_L1":
                    projector = FileProjector.GetFileProjectByName("EOS");
                    using (AbstractWarpDataset locationRaster = FileFinder.TryFind03FileFromModisImgFile(raster))
                    {
                        hasVaild = projector.HasVaildEnvelope(locationRaster, invalidEnv, SpatialReferenceFactory.CreateSpatialReference(4326));
                    }
                    break;

                case "NOAA_1BD":
                    projector = FileProjector.GetFileProjectByName("NOAA_1BD");
                    hasVaild = projector.HasVaildEnvelope(raster, invalidEnv, SpatialReferenceFactory.CreateSpatialReference(4326));
                    break;

                case "FY2NOM":
                    {
                        projector = FileProjector.GetFileProjectByName("FY2NOM");
                        hasVaild = projector.HasVaildEnvelope(raster, invalidEnv, SpatialReferenceFactory.CreateSpatialReference(4326));
                    }
                    break;

                case "PROJECTED":
                    Envelope coord = raster.GetEnvelope();
                    if (coord != null)
                        env = new PrjEnvelope(coord.MinX, coord.MaxX, coord.MinY, coord.MaxY, raster.SpatialRef);
                    hasVaild = env.IntersectsWith(invalidEnv);
                    break;
                case "FY4A_AGRI_0500":
                case "FY4A_AGRI_1000":
                case "FY4A_AGRI_2000":
                case "FY4A_AGRI_4000":
                    {
                        projector = FileProjector.GetFileProjectByName("FY4A");
                        hasVaild = projector.HasVaildEnvelope(raster, invalidEnv, SpatialReferenceFactory.CreateSpatialReference(4326));
                    }
                    break;
                case "H8":
                    hasVaild = true;
                    break;

                default:
                    throw new Exception("尚未支持的数据类型" + fileType);
            }
            return hasVaild;
        }

        /// <summary>
        /// 数据在指定的经纬度内，并且计算出有效率，以及实际输出范围
        /// </summary>
        /// <param name="raster"></param>
        /// <param name="validEnv"></param>
        /// <param name="envSpatialReference"></param>
        /// <param name="validRate"></param>
        /// <param name="outEnv"></param>
        /// <returns></returns>
        public static bool ValidEnvelope(AbstractWarpDataset raster, PrjEnvelope validEnv, SpatialReference envSpatialReference, out double validRate, out PrjEnvelope outEnv)
        {
            string fileType = new FileChecker().GetFileType(raster);
            if (string.IsNullOrWhiteSpace(fileType))
                throw new Exception("暂未支持该类数据的投影");
            IFileProjector projector = null;
            bool hasVaild = false;
            switch (fileType)
            {
                case "FY3C_VIRR_L1":
                    projector = FileProjector.GetFileProjectByName("FY3C_VIRR");
                    using (AbstractWarpDataset locationRaster = FileFinder.TryFindGeoFileFromFY3C_VIRR(raster))
                    {
                        hasVaild = projector.ValidEnvelope(locationRaster, validEnv, envSpatialReference, out validRate, out outEnv);
                    }
                    break;

                case "FY3C_MERSI_1KM_L1":
                case "FY3C_MERSI_QKM_L1":
                    projector = FileProjector.GetFileProjectByName("FY3C_MERSI");
                    using (AbstractWarpDataset locationRaster = FileFinder.TryFindGeoFileFromFY3C_MERSI(raster))
                    {
                        hasVaild = projector.ValidEnvelope(locationRaster, validEnv, envSpatialReference, out validRate, out outEnv);
                    }
                    break;

                case "VIRR_L1":
                    projector = FileProjector.GetFileProjectByName("FY3_VIRR");
                    hasVaild = projector.ValidEnvelope(raster, validEnv, envSpatialReference, out validRate, out outEnv);
                    break;

                case "MERSI_1KM_L1":
                case "MERSI_QKM_L1":
                case "MERSI_250M_L1":
                    projector = FileProjector.GetFileProjectByName("FY3_MERSI");
                    hasVaild = projector.ValidEnvelope(raster, validEnv, envSpatialReference, out validRate, out outEnv);
                    break;

                case "MODIS_1KM_L1":
                    projector = FileProjector.GetFileProjectByName("EOS");
                    using (AbstractWarpDataset locationRaster = FileFinder.TryFind03FileFromModisImgFile(raster))
                    {
                        hasVaild = projector.ValidEnvelope(locationRaster, validEnv, envSpatialReference, out validRate, out outEnv);
                    }
                    break;

                case "NOAA_1BD":
                    projector = FileProjector.GetFileProjectByName("NOAA_1BD");
                    hasVaild = projector.ValidEnvelope(raster, validEnv, envSpatialReference, out validRate, out outEnv);
                    break;

                case "FY2NOM":
                    {
                        projector = FileProjector.GetFileProjectByName("FY2NOM");
                        hasVaild = projector.ValidEnvelope(raster, validEnv, envSpatialReference, out validRate, out outEnv);
                    }
                    break;

                case "FY1X_1A5":
                    projector = FileProjector.GetFileProjectByName("FY1X_1A5");
                    hasVaild = projector.ValidEnvelope(raster, validEnv, envSpatialReference, out validRate, out outEnv);
                    break;

                case "PROJECTED":
                    var coord = raster.GetEnvelope();
                    if (coord != null)
                    {
                        outEnv = new PrjEnvelope(coord.MinX, coord.MaxX, coord.MinY, coord.MaxY, raster.SpatialRef);
                        hasVaild = outEnv.IntersectsRate(validEnv, out validRate);
                    }
                    break;

                default:
                    throw new Exception("尚未支持的数据类型" + fileType);
            }
            validRate = 0;
            outEnv = null;
            return hasVaild;
        }

        /// <summary>
        /// 获取输出文件名
        /// </summary>
        private string GetOutPutFile(string outDir, string filename, SpatialReference projRef, string blockname, DataIdentify identify, float resolution, string extName)
        {
            string outFileNmae = "";
            string filenameOnly = Path.GetFileName(filename);
            string prjIdentify;
            prjIdentify = GetPrjIdentify(projRef);
            RasterDatasetInfo dsInfo = mRasterSourceManager.GetInstance().GetRasterDatasetInfo(filename);
            DateTime? imageTime = mRasterSourceManager.GetInstance().GetImageTime(filename);
            if (dsInfo != null && !string.IsNullOrWhiteSpace(dsInfo.SatelliteID)
                && !string.IsNullOrWhiteSpace(dsInfo.SensorID) && imageTime.HasValue)
            {
                string satellite = dsInfo.SatelliteID;
                string sensor = dsInfo.SensorID;
                DateTime datetime = imageTime.Value;//TimeOfDay == TimeSpan.Zero ? dataIdentify.OrbitDateTime : identify.OrbitDateTime;
                string otname = _genericFilename.GetL1PrjFilename(satellite, sensor, datetime, filenameOnly, prjIdentify, blockname, resolution, extName);
                outFileNmae = Path.Combine(outDir, otname);
            }
            else
            {
                string otname = _genericFilename.PrjBlockFilename(filenameOnly, prjIdentify, blockname, extName);
                outFileNmae = Path.Combine(outDir, otname);
            }
            return CreateOnlyFilename(outFileNmae);
        }

        private static string GetPrjIdentify(SpatialReference projRef)
        {
            string prjIdentify;
            if (projRef == null || string.IsNullOrWhiteSpace(projRef.__str__()))
                prjIdentify = "GLL";
            else if (projRef.IsGeographic()==1)
                prjIdentify = GenericFilename.GetProjectionIdentify(projRef.GetAttrValue("GEOGCS",0));
            else
                prjIdentify = GenericFilename.GetProjectionIdentify(projRef.GetAttrValue("PROJCS",0));
            return prjIdentify;
        }

        /// <summary>
        /// 生成非重复的文件名：如果已经存在了，自动在其后添加(1)或(2)等。
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private string CreateOnlyFilename(string filename)
        {
            if (File.Exists(filename))
            {
                string dir = Path.GetDirectoryName(filename);
                string filenameWithExt = Path.GetFileNameWithoutExtension(filename);
                string fileExt = Path.GetExtension(filename);
                int i = 1;
                string outFileNmae = Path.Combine(dir, filenameWithExt + "(" + i + ")" + fileExt);
                while (File.Exists(outFileNmae))
                    outFileNmae = Path.Combine(dir, filenameWithExt + "(" + i++ + ")" + fileExt);
                return outFileNmae;
            }
            else
                return filename;
        }

        private string GetBlockName()
        {
            return "DXX";
        }

        private void TryDeleteErrorFile(string filename)
        {
            try
            {
                if (File.Exists(filename))
                    File.Delete(filename);
                string hdr = Path.ChangeExtension(filename, ".hdr");
                if (File.Exists(hdr))
                    File.Delete(hdr);
            }
            catch
            {
            }
        }

        private bool IsGeoSpatial(SpatialReference dstSpatialRef)
        {
            return dstSpatialRef == null || dstSpatialRef.IsGeographic()==1;
        }

        private bool IsDir(string path)
        {
            if (Directory.Exists(path))
                return true;
            else if (File.Exists(path))
                return false;
            else if (Path.HasExtension(path))
                return false;
            else
                return true;
        }

        protected string BandNameString(int[] outBandNos)
        {
            if (outBandNos == null || outBandNos.Length == 0)
                return "";
            string bandNames = string.Empty;
            foreach (int bandNo in outBandNos)
                bandNames += ("band " + bandNo + ",");
            if (bandNames.EndsWith(","))
                bandNames = bandNames.Substring(0, bandNames.Length - 1);
            return bandNames;
        }

        #region 放弃支持的老卫星

        private string[] PrjFY2NOM_L1(AbstractWarpDataset srcRaster, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            SpatialReference dstSpatialRef = prjOutArg.ProjectionRef;
            PrjEnvelopeItem[] prjEnvelopes = prjOutArg.Envelopes;
            IFileProjector projector = null;
            int[] kmBands;
            FileFinder.GetVISSRBandmapTable(prjOutArg.SelectedBands, out kmBands);
            errorMessage = new StringBuilder();
            try
            {
                List<string> outFiles = new List<string>();
                projector = FileProjector.GetFileProjectByName("FY2NOM");
                projector.BeginSession(srcRaster);
                for (int i = 0; i < prjEnvelopes.Length; i++)
                {
                    string outFileName = null;
                    if (IsDir(prjOutArg.OutDirOrFile))
                        outFileName = GetOutPutFile(prjOutArg.OutDirOrFile, srcRaster.fileName, dstSpatialRef, prjEnvelopes[i].Name, srcRaster.DataIdentify, prjOutArg.ResolutionX, ".ldf");
                    else
                        outFileName = prjOutArg.OutDirOrFile;

                    Fy2_NOM_PrjSettings prjSetting = new Fy2_NOM_PrjSettings();
                    prjSetting.OutPathAndFileName = outFileName;
                    try
                    {
                        prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                        prjSetting.OutBandNos = kmBands;
                        prjSetting.OutResolutionX = prjOutArg.ResolutionX;
                        prjSetting.OutResolutionY = prjOutArg.ResolutionY;
                        if (prjOutArg.Args != null)
                        {
                            if (prjOutArg.Args.Contains("NotRadiation"))
                                prjSetting.IsRadiation = false;
                            if (prjOutArg.Args.Contains("NotSolarZenith"))
                                prjSetting.IsSolarZenith = false;
                            if (prjOutArg.Args.Contains("IsSensorZenith"))
                                prjSetting.IsSensorZenith = true;
                            if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                prjSetting.IsClearPrjCache = true;
                            prjSetting.ExtArgs = prjOutArg.Args;
                        }
                        projector.Project(srcRaster, prjSetting, dstSpatialRef, progress);
                        outFiles.Add(prjSetting.OutPathAndFileName);
                    }
                    catch (Exception ex)
                    {
                        outFiles.Add(null);
                        errorMessage.AppendLine(ex.Message);
                        TryDeleteErrorFile(outFileName);
                        Console.WriteLine(ex.Message);
                    }
                }
                return outFiles.ToArray();
            }
            finally
            {
                if (projector != null)
                    projector.EndSession();
            }
        }

        private string[] PrjMODIS_HKM_L1(AbstractWarpDataset srcRaster, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            errorMessage = new StringBuilder();
            AbstractWarpDataset mod03File = FileFinder.TryFind03FileFromModisImgFile(srcRaster);
            if (mod03File == null)
                return null;
            AbstractWarpDataset qkmRaster = FileFinder.TryFindMODIS_QKM_L1From03(mod03File);
            AbstractWarpDataset hkmRaster = srcRaster;
            AbstractWarpDataset kmRaster = FileFinder.TryFindMODIS_1KM_L1From03(mod03File);
            return PrjMODIS(prjOutArg, errorMessage, mod03File, ref qkmRaster, ref hkmRaster, kmRaster, progress);
        }

        private string[] PrjMODIS_1KM_L1(AbstractWarpDataset srcRaster, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            errorMessage = new StringBuilder();
            AbstractWarpDataset mod03File = FileFinder.TryFind03FileFromModisImgFile(srcRaster);
            if (mod03File == null)
                return null;
            AbstractWarpDataset qkmRaster = FileFinder.TryFindMODIS_QKM_L1From03(mod03File);
            AbstractWarpDataset hkmRaster = FileFinder.TryFindMODIS_HKM_L1From03(mod03File);
            AbstractWarpDataset kmRaster = srcRaster;
            return PrjMODIS(prjOutArg, errorMessage, mod03File, ref qkmRaster, ref hkmRaster, kmRaster, progress);
        }

        private string[] PrjMODIS_1KM_L1Only(AbstractWarpDataset fileName, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            SpatialReference dstSpatialRef = prjOutArg.ProjectionRef;
            PrjEnvelopeItem[] prjEnvelopes = prjOutArg.Envelopes;
            AbstractWarpDataset srcRaster = fileName;
            AbstractWarpDataset locationRaster = null;
            IFileProjector projTor = null;
            errorMessage = new StringBuilder();
            int[] qkmBands;
            int[] hkmBands;
            int[] kmBands;
            FileFinder.GetModisBandmapTable(fileName, null, null, prjOutArg.SelectedBands, out qkmBands, out hkmBands, out kmBands);
            try
            {
                List<string> outFiles = new List<string>();
                locationRaster = FileFinder.TryFind03FileFromModisImgFile(fileName);
                projTor = FileProjector.GetFileProjectByName("EOS");
                projTor.BeginSession(srcRaster);
                for (int i = 0; i < prjEnvelopes.Length; i++)
                {
                    try
                    {
                        EOS_MODIS_PrjSettings prjSetting = new EOS_MODIS_PrjSettings();
                        prjSetting.LocationFile = locationRaster;
                        //string outfilename = IsDir(prjOutArg.OutDirOrFile) ? GetOutPutFile(prjOutArg.OutDirOrFile, fileName.fileName, dstSpatialRef, prjEnvelopes[i].Name) : prjOutArg.OutDirOrFile;

                        string outFileName = null;
                        if (IsDir(prjOutArg.OutDirOrFile))
                            outFileName = GetOutPutFile(prjOutArg.OutDirOrFile, srcRaster.fileName, dstSpatialRef, prjEnvelopes[i].Name, srcRaster.DataIdentify, prjOutArg.ResolutionX, ".ldf");
                        else
                            outFileName = prjOutArg.OutDirOrFile;

                        prjSetting.OutPathAndFileName = outFileName;
                        prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                        prjSetting.OutBandNos = kmBands;
                        prjSetting.OutResolutionX = prjOutArg.ResolutionX;
                        prjSetting.OutResolutionY = prjOutArg.ResolutionY;
                        projTor.Project(srcRaster, prjSetting, dstSpatialRef, progress);
                        outFiles.Add(prjSetting.OutPathAndFileName);
                    }
                    catch (Exception ex)
                    {
                        outFiles.Add(null);
                        errorMessage.AppendLine(ex.Message);
                        Console.WriteLine(ex.Message);
                    }
                }
                return outFiles.ToArray();
            }
            finally
            {
                if (locationRaster != null)
                    locationRaster.Dispose();
                if (projTor != null)
                    projTor.EndSession();
            }
        }

        private string[] PrjFY1X_1A5_L1(AbstractWarpDataset raster, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            SpatialReference dstSpatialRef = prjOutArg.ProjectionRef;
            PrjEnvelopeItem[] prjEnvelopes = prjOutArg.Envelopes;
            AbstractWarpDataset srcRaster = raster;
            IFileProjector projTor = null;
            int[] kmBands;
            FileFinder.GetNoaaBandmapTable(prjOutArg.SelectedBands, out kmBands);
            errorMessage = new StringBuilder();
            try
            {
                List<string> outFiles = new List<string>();
                projTor = FileProjector.GetFileProjectByName("FY1X_1A5");
                projTor.BeginSession(srcRaster);
                for (int i = 0; i < prjEnvelopes.Length; i++)
                {
                    string outFileName = null;
                    try
                    {
                        if (prjOutArg.ResolutionX == 0)
                        {
                            if (dstSpatialRef == null || dstSpatialRef.IsGeographic()==1)
                            {
                                prjOutArg.ResolutionX = 0.01f;
                                prjOutArg.ResolutionY = 0.01f;
                            }
                            else
                            {
                                prjOutArg.ResolutionX = 1000f;
                                prjOutArg.ResolutionY = 1000f;
                            }
                        }
                        if (IsDir(prjOutArg.OutDirOrFile))
                            outFileName = GetOutPutFile(prjOutArg.OutDirOrFile, srcRaster.fileName, dstSpatialRef, prjEnvelopes[i].Name, srcRaster.DataIdentify, prjOutArg.ResolutionX, ".ldf");
                        else
                            outFileName = prjOutArg.OutDirOrFile;

                        NOAA_PrjSettings prjSetting = new NOAA_PrjSettings();
                        prjSetting.OutPathAndFileName = outFileName;
                        prjSetting.OutEnvelope = prjEnvelopes[i].PrjEnvelope;
                        prjSetting.OutBandNos = kmBands;
                        prjSetting.OutResolutionX = prjOutArg.ResolutionX;
                        prjSetting.OutResolutionY = prjOutArg.ResolutionY;
                        if (prjOutArg.Args != null)
                        {
                            //if (prjOutArg.Args.Contains("NotRadiation"))//NotRadiation
                            prjSetting.IsRadiation = false;
                            //if (prjOutArg.Args.Contains("NotSolarZenith"))
                            prjSetting.IsSolarZenith = false;
                            if (prjOutArg.Args.Contains("IsSensorZenith"))
                                prjSetting.IsSensorZenith = true;
                            if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                prjSetting.IsClearPrjCache = true;
                            prjSetting.ExtArgs = prjOutArg.Args;
                        }
                        projTor.Project(srcRaster, prjSetting, dstSpatialRef, progress);
                        outFiles.Add(prjSetting.OutPathAndFileName);
                    }
                    catch (Exception ex)
                    {
                        TryDeleteErrorFile(outFileName);
                        outFiles.Add(null);
                        errorMessage.AppendLine(ex.Message);
                        Console.WriteLine(ex.Message);
                    }
                }
                return outFiles.ToArray();
            }
            finally
            {
                if (projTor != null)
                    projTor.EndSession();
            }
        }

        private string[] PrjNOAA_1BD_L1(AbstractWarpDataset raster, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            SpatialReference dstSpatialRef = prjOutArg.ProjectionRef;
            PrjEnvelopeItem[] prjEnvelopes = prjOutArg.Envelopes;
            AbstractWarpDataset srcRaster = raster;
            IFileProjector projTor = null;
            int[] kmBands;
            FileFinder.GetNoaaBandmapTable(prjOutArg.SelectedBands, out kmBands);
            errorMessage = new StringBuilder();
            try
            {
                List<string> outFiles = new List<string>();
                projTor = FileProjector.GetFileProjectByName("NOAA_1BD");
                projTor.BeginSession(srcRaster);
                for (int i = 0; i < prjEnvelopes.Length; i++)
                {
                    string outFileName = null;
                    try
                    {
                        if (prjOutArg.ResolutionX == 0)
                        {
                            if (dstSpatialRef == null || dstSpatialRef.IsGeographic()==1)
                            {
                                prjOutArg.ResolutionX = 0.01f;
                                prjOutArg.ResolutionY = 0.01f;
                            }
                            else
                            {
                                prjOutArg.ResolutionX = 1000f;
                                prjOutArg.ResolutionY = 1000f;
                            }
                        }
                        if (IsDir(prjOutArg.OutDirOrFile))
                            outFileName = GetOutPutFile(prjOutArg.OutDirOrFile, srcRaster.fileName, dstSpatialRef, prjEnvelopes[i].Name, srcRaster.DataIdentify, prjOutArg.ResolutionX, ".ldf");
                        else
                            outFileName = prjOutArg.OutDirOrFile;

                        NOAA_PrjSettings prjSetting = new NOAA_PrjSettings();
                        prjSetting.OutPathAndFileName = outFileName;
                        prjSetting.OutEnvelope = prjEnvelopes[i].PrjEnvelope;
                        prjSetting.OutBandNos = kmBands;
                        prjSetting.OutResolutionX = prjOutArg.ResolutionX;
                        prjSetting.OutResolutionY = prjOutArg.ResolutionY;
                        if (prjOutArg.Args != null)
                        {
                            if (prjOutArg.Args.Contains("NotRadiation"))//NotRadiation
                                prjSetting.IsRadiation = false;
                            if (prjOutArg.Args.Contains("NotSolarZenith"))
                                prjSetting.IsSolarZenith = false;
                            if (prjOutArg.Args.Contains("IsSensorZenith"))
                                prjSetting.IsSensorZenith = true;
                            if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                prjSetting.IsClearPrjCache = true;
                            prjSetting.ExtArgs = prjOutArg.Args;
                        }
                        projTor.Project(srcRaster, prjSetting, dstSpatialRef, progress);
                        outFiles.Add(prjSetting.OutPathAndFileName);
                    }
                    catch (Exception ex)
                    {
                        TryDeleteErrorFile(outFileName);
                        outFiles.Add(null);
                        errorMessage.AppendLine(ex.Message);
                        Console.WriteLine(ex.Message);
                    }
                }
                return outFiles.ToArray();
            }
            finally
            {
                if (projTor != null)
                    projTor.EndSession();
            }
        }

        /// <summary>
        /// 规范Noaa轨道文件名
        /// p1bn07a5.n16.12.1bd
        /// 为
        /// NOAA16_AVHRR_L1_20121108_1058.1bd
        /// </summary>
        /// <param name="raster"></param>
        /// <param name="outfilename"></param>
        private void TryUpdateNoaaOrbitFilename(AbstractWarpDataset raster, ref string outfilename)
        {
            if (outfilename.IndexOf("AVHRR") != -1)
                return;
            string dir = Path.GetDirectoryName(outfilename);
            string filename = Path.GetFileNameWithoutExtension(outfilename);
            string fileext = Path.GetExtension(outfilename);
            DataIdentify identify = raster.DataIdentify;
            if (identify == null)
                return;
            filename = identify.Satellite.Replace("-", "") + "_"
                    + raster.DataIdentify.Sensor + "_"
                    + "L1_"
                    + raster.DataIdentify.OrbitDateTime.ToString("yyyyMMdd") + "_"
                    + raster.DataIdentify.OrbitDateTime.ToString("HHmm") + "_"
                    + "1000M"
                    + fileext;
            outfilename = Path.Combine(dir, filename);
        }

        private string[] PrjMODIS_QKM_L1(AbstractWarpDataset srcRaster, PrjOutArg prjOutArg, Action<int, string> progress, out StringBuilder errorMessage)
        {
            errorMessage = new StringBuilder();
            AbstractWarpDataset mod03File = FileFinder.TryFind03FileFromModisImgFile(srcRaster);
            if (mod03File == null)
                return null;
            AbstractWarpDataset qkmRaster = srcRaster;
            AbstractWarpDataset hkmRaster = FileFinder.TryFindMODIS_HKM_L1From03(mod03File);
            AbstractWarpDataset kmRaster = FileFinder.TryFindMODIS_1KM_L1From03(mod03File);
            return PrjMODIS(prjOutArg, errorMessage, mod03File, ref qkmRaster, ref hkmRaster, kmRaster, progress);
        }

        private string[] PrjMODIS(PrjOutArg prjOutArg, StringBuilder errorMessage, AbstractWarpDataset locationRaster,
            ref AbstractWarpDataset qkmRaster, ref AbstractWarpDataset hkmRaster, AbstractWarpDataset kmRaster, Action<int, string> progress)
        {
            //默认分辨率，默认参数
            SpatialReference dstSpatialRef = prjOutArg.ProjectionRef;
            float outResolutionX = prjOutArg.ResolutionX;
            float outResolutionY = prjOutArg.ResolutionY;
            SetDefaultResolutionForModis(qkmRaster, hkmRaster, dstSpatialRef, ref outResolutionX, ref outResolutionY);
            prjOutArg.ResolutionX = outResolutionX;
            prjOutArg.ResolutionY = outResolutionY;
            FilterRasterForModis(ref qkmRaster, ref hkmRaster, kmRaster, dstSpatialRef, outResolutionX, outResolutionY);
            int[] qkmBands;
            int[] hkmBands;
            int[] kmBands;
            FileFinder.GetModisBandmapTable(kmRaster, hkmRaster, qkmRaster, prjOutArg.SelectedBands, out qkmBands, out hkmBands, out kmBands);
            int bandCount = (qkmBands == null ? 0 : qkmBands.Length) + (hkmBands == null ? 0 : hkmBands.Length) + (kmBands == null ? 0 : kmBands.Length);
            if (bandCount == 0)
            {
                errorMessage.Append("没有获取要投影的通道");
                return null;
            }
            string bandNames = "BANDNAMES="
                + (qkmBands == null || qkmBands.Length == 0 ? "" : BandNameString(qkmBands) + ",")
                + (hkmBands == null || hkmBands.Length == 0 ? "" : BandNameString(hkmBands) + ",")
                + BandNameString(kmBands);
            IFileProjector projtor = FileProjector.GetFileProjectByName("EOS");
            PrjEnvelopeItem[] prjEnvelopes = prjOutArg.Envelopes;
            try
            {
                List<string> outFiles = new List<string>();
                AbstractWarpDataset outRaster = null;
                for (int i = 0; i < prjEnvelopes.Length; i++)
                {
                    PrjEnvelope envelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                    if (envelope == null)
                    {
                        projtor.ComputeDstEnvelope(kmRaster, dstSpatialRef, out envelope, null);
                        if (envelope == null)
                        {
                            errorMessage.Append("未能读取出文件的经纬度范围：" + kmRaster.fileName);
                            continue;
                        }
                    }
                    string outFileName = null;
                    try
                    {
                        AbstractWarpDataset rad = qkmRaster != null ? qkmRaster : (kmRaster != null ? kmRaster : null);
                        string orbitFileName = rad == null ? null : rad.fileName;
                        if (IsDir(prjOutArg.OutDirOrFile))
                            outFileName = GetOutPutFile(prjOutArg.OutDirOrFile, rad.fileName, dstSpatialRef, prjEnvelopes[i].Name, rad.DataIdentify, prjOutArg.ResolutionX, ".ldf");
                        else
                            outFileName = prjOutArg.OutDirOrFile;
                        outRaster = CreateRaster(outFileName, envelope, outResolutionX, outResolutionY, bandCount, dstSpatialRef, bandNames);
                        outFiles.Add(outFileName);
                    }
                    catch (Exception ex)
                    {
                        TryDeleteErrorFile(outFileName);
                        errorMessage.Append(ex.Message);
                        outFiles.Add(null);
                    }
                    finally
                    {
                        if (outRaster != null)
                        {
                            outRaster.Dispose();
                            outRaster = null;
                        }
                    }
                }
                bool hasAngle = false;
                int perBandBegin = 0;
                int curBandBegin = 0;
                if (qkmBands != null && qkmBands.Length != 0)
                {
                    curBandBegin = 0;
                    perBandBegin = qkmBands.Length;
                    projtor.BeginSession(qkmRaster);
                    for (int i = 0; i < prjEnvelopes.Length; i++)
                    {
                        if (outFiles[i] == null)
                            continue;
                        try
                        {
                            outRaster = OpenUpdate(outFiles[i]);
                            EOS_MODIS_PrjSettings prjSetting = new EOS_MODIS_PrjSettings();
                            prjSetting.LocationFile = locationRaster;
                            prjSetting.OutResolutionX = outResolutionX;
                            prjSetting.OutResolutionY = outResolutionY;
                            prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                            prjSetting.OutBandNos = qkmBands;
                            prjSetting.OutPathAndFileName = outFiles[i];
                            if (prjOutArg.Args != null)
                            {
                                if (prjOutArg.Args.Contains("NotRadiation"))
                                    prjSetting.IsRadRef = false;
                                if (prjOutArg.Args.Contains("NotSolarZenith"))
                                    prjSetting.IsSolarZenith = false;
                                if (prjOutArg.Args.Contains("IsSensorZenith"))
                                    prjSetting.IsSensorZenith = true;
                                if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                    prjSetting.IsClearPrjCache = true;
                            }
                            if (!hasAngle)
                            {
                                prjSetting.ExtArgs = prjOutArg.Args;
                                hasAngle = true;
                            }
                            projtor.Project(qkmRaster, prjSetting, outRaster, curBandBegin, progress);
                        }
                        catch (Exception ex)
                        {
                            errorMessage.AppendLine(ex.Message);
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            if (outRaster != null)
                            {
                                outRaster.Dispose();
                                outRaster = null;
                            }
                        }
                    }
                    projtor.EndSession();
                }
                if (hkmBands != null && hkmBands.Length != 0)
                {
                    curBandBegin += perBandBegin;
                    perBandBegin += hkmBands.Length;
                    projtor.BeginSession(hkmRaster);
                    for (int i = 0; i < prjEnvelopes.Length; i++)
                    {
                        if (outFiles[i] == null)
                            continue;
                        try
                        {
                            outRaster = OpenUpdate(outFiles[i]);
                            EOS_MODIS_PrjSettings prjSetting = new EOS_MODIS_PrjSettings();
                            prjSetting.LocationFile = locationRaster;
                            prjSetting.OutResolutionX = outResolutionX;
                            prjSetting.OutResolutionY = outResolutionY;
                            prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                            prjSetting.OutBandNos = hkmBands;
                            prjSetting.OutPathAndFileName = outFiles[i];
                            if (prjOutArg.Args != null)
                            {
                                if (prjOutArg.Args.Contains("NotRadiation"))
                                    prjSetting.IsRadRef = false; ;
                                if (prjOutArg.Args.Contains("NotSolarZenith"))
                                    prjSetting.IsSolarZenith = false;
                                if (prjOutArg.Args.Contains("IsSensorZenith"))
                                    prjSetting.IsSensorZenith = true;
                                if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                    prjSetting.IsClearPrjCache = true;
                            }
                            if (!hasAngle)
                            {
                                prjSetting.ExtArgs = prjOutArg.Args;
                                hasAngle = true;
                            }
                            projtor.Project(hkmRaster, prjSetting, outRaster, curBandBegin, progress);
                        }
                        catch (Exception ex)
                        {
                            errorMessage.AppendLine(ex.Message);
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            if (outRaster != null)
                            {
                                outRaster.Dispose();
                                outRaster = null;
                            }
                        }
                    }
                    projtor.EndSession();
                }
                if (kmBands != null && kmBands.Length != 0)
                {
                    projtor.BeginSession(qkmRaster);
                    for (int i = 0; i < prjEnvelopes.Length; i++)
                    {
                        if (outFiles[i] == null)
                            continue;
                        bool isSuccess = false;
                        try
                        {
                            outRaster = OpenUpdate(outFiles[i]);
                            EOS_MODIS_PrjSettings prjSetting = new EOS_MODIS_PrjSettings();
                            prjSetting.LocationFile = locationRaster;
                            prjSetting.OutEnvelope = prjEnvelopes[i] == null ? null : prjEnvelopes[i].PrjEnvelope;
                            prjSetting.OutResolutionX = outResolutionX;
                            prjSetting.OutResolutionY = outResolutionY;
                            prjSetting.OutBandNos = kmBands;
                            prjSetting.OutPathAndFileName = outFiles[i];
                            if (prjOutArg.Args != null)
                            {
                                if (prjOutArg.Args.Contains("NotRadiation"))
                                    prjSetting.IsRadRef = false;
                                if (prjOutArg.Args.Contains("NotSolarZenith"))
                                    prjSetting.IsSolarZenith = false;
                                if (prjOutArg.Args.Contains("IsSensorZenith"))
                                    prjSetting.IsSensorZenith = true;
                                if (prjOutArg.Args.Contains("IsClearPrjCache"))
                                    prjSetting.IsClearPrjCache = true;
                            }
                            if (!hasAngle)
                            {
                                prjSetting.ExtArgs = prjOutArg.Args;
                                hasAngle = true;
                            }
                            projtor.Project(kmRaster, prjSetting, outRaster, perBandBegin, progress);
                            isSuccess = true;
                        }
                        catch (Exception ex)
                        {
                            errorMessage.AppendLine(ex.Message);
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            if (outRaster != null)
                            {
                                outRaster.Dispose();
                                outRaster = null;
                            }
                            if (!isSuccess && File.Exists(outFiles[i]))
                            {
                                File.Delete(outFiles[i]);
                                outFiles[i] = null;
                            }
                        }
                    }
                }
                return outFiles.ToArray();
            }
            finally
            {
                if (projtor != null)
                    projtor.EndSession();
                if (kmRaster != null)
                    kmRaster.Dispose();
            }
        }

        private static void FilterRasterForModis(ref AbstractWarpDataset qkmRaster, ref AbstractWarpDataset hkmRaster, AbstractWarpDataset kmRaster, SpatialReference dstSpatialRef, float outResolutionX, float outResolutionY)
        {
            float baseResolutionK = 0.01f;
            float baseResolutionH = 0.005f;
            if (dstSpatialRef.IsProjected()==1)
            {
                baseResolutionK = 1000f;
                baseResolutionH = 500f;
            }
            if (outResolutionX >= baseResolutionK && outResolutionY >= baseResolutionK)
            {
                if (kmRaster != null)
                {
                    if (qkmRaster != null)
                    {
                        qkmRaster.Dispose();
                        qkmRaster = null;
                    }
                    if (hkmRaster != null)
                    {
                        hkmRaster.Dispose();
                        hkmRaster = null;
                    }
                }
            }
            else if (outResolutionX >= baseResolutionH && outResolutionY >= baseResolutionH)
            {
                if (kmRaster != null || hkmRaster != null)
                {
                    if (qkmRaster != null)
                    {
                        qkmRaster.Dispose();
                        qkmRaster = null;
                    }
                }
            }
        }

        private static void SetDefaultResolutionForModis(AbstractWarpDataset qkmRaster, AbstractWarpDataset hkmRaster, SpatialReference dstSpatialRef, ref float outResolutionX, ref float outResolutionY)
        {
            if (outResolutionX == 0 || outResolutionY == 0)
            {
                if (qkmRaster != null)
                {
                    if (dstSpatialRef == null || dstSpatialRef.IsGeographic()==1)
                    {
                        outResolutionX = 0.0025f;
                        outResolutionY = 0.0025f;
                    }
                    else
                    {
                        outResolutionX = 250f;
                        outResolutionY = 250f;
                    }
                }
                else if (hkmRaster != null)
                {
                    if (dstSpatialRef == null || dstSpatialRef.IsGeographic()==1)
                    {
                        outResolutionX = 0.005f;
                        outResolutionY = 0.005f;
                    }
                    else
                    {
                        outResolutionX = 500f;
                        outResolutionY = 500f;
                    }
                }
                else
                {
                    if (dstSpatialRef == null || dstSpatialRef.IsGeographic()==1)
                    {
                        outResolutionX = 0.01f;
                        outResolutionY = 0.01f;
                    }
                    else
                    {
                        outResolutionX = 1000f;//投影坐标系
                        outResolutionY = 1000f;
                    }
                }
            }
        }

        #endregion 放弃支持的老卫星
    }
}