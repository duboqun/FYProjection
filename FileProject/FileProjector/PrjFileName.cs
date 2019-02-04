﻿using PIE.Meteo.RasterProject;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.OSR;

namespace PIE.Meteo.FileProject
{
    public class PrjFileName
    {
        public static string GetPrjFileName(string outDir, string filename, SpatialReference projRef, string blockname)
        {
            string outFileNmae = "";
            string orbitFilename = Path.GetFileName(filename);
            string prjIdentify;
            prjIdentify = GetPrjShortName(projRef);
            string otname = new GenericFilename().PrjBlockFilename(orbitFilename, prjIdentify, blockname, ".tif");
            outFileNmae = Path.Combine(outDir, otname);
            return CreateOnlyFilename(outFileNmae);
        }

        private static string GetPrjShortName(SpatialReference projRef)
        {
            string prjIdentify;

            if (projRef == null || string.IsNullOrWhiteSpace(projRef.__str__()))
                prjIdentify = "GLL";
            else if (projRef.IsGeographic() == 1)
                prjIdentify = GenericFilename.GetProjectionIdentify(projRef.GetAttrValue("GEOGCS",0));
            else
                prjIdentify = GenericFilename.GetProjectionIdentify(projRef.GetAttrValue("PROJCS",0));
            return prjIdentify;
        }

        /// <summary>
        /// 投影分幅文件命名
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="satellite"></param>
        /// <param name="sensor"></param>
        /// <param name="datetime"></param>
        /// <param name="spatialRef"></param>
        /// <param name="blockName"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public static string GetL1PrjFilenameWithOutDir(string filename, string satellite, string sensor,
            DateTime datetime, SpatialReference spatialRef, string blockName, float resolution)
        {
            string prjIdentify = GetPrjShortName(spatialRef);
            return GetL1PrjFilenameWithOutDir(filename, satellite, sensor, datetime, prjIdentify, blockName,
                resolution);
        }

        public static string GetL1PrjFilenameWithOutDir(string orbitFilename, string satellite, string sensor,
            DateTime orbitTime, string prjIdentify, string blockName, float resolution)
        {
            if (string.IsNullOrWhiteSpace(satellite) || string.IsNullOrWhiteSpace(sensor))
            {
                string filename = new GenericFilename().PrjBlockFilename(orbitFilename, prjIdentify, blockName, ".ldf");
                return Path.GetFileName(filename);
            }

            return string.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}_{7}.ldf",
                satellite,
                sensor,
                blockName,
                prjIdentify,
                "L1",
                orbitTime.ToString("yyyyMMdd"),
                orbitTime.ToString("HHmm"),
                prjIdentify == "GLL" ? GLLResolutionIdentify(resolution) : ResolutionIdentify(resolution)
            );
        }

        public static string ResolutionIdentify(float resolution, SpatialReference projRef)
        {
            if (projRef == null || projRef.IsGeographic()==1)
                return GLLResolutionIdentify(resolution);
            else
                return ResolutionIdentify(resolution);
        }

        public static string ResolutionIdentify(float resolution)
        {
            if (resolution == 1000f)
                return "1000M";
            else if (resolution == 250f)
                return "0250M";
            else if (resolution == 500f)
                return "0500M";
            else if (resolution == 5000f)
                return "5000M";
            else if (resolution == 4000f)
                return "4000M";
            else if (resolution == 2000f)
                return "2000M";
            else if (resolution == 10000f)
                return "010KM";
            else if (resolution == 25000f)
                return "025KM";
            else if (resolution == 100000f)
                return "100KM";
            else
                return "00000";
        }

        public static string GLLResolutionIdentify(float resolution)
        {
            if (resolution == 0.01f)
                return "1000M";
            else if (resolution == 0.0025f)
                return "0250M";
            else if (resolution == 0.005f)
                return "0500M";
            else if (resolution == 0.05f)
                return "5000M";
            else if (resolution == 0.04f)
                return "4000M";
            else if (resolution == 0.02f)
                return "2000M";
            else if (resolution == 0.1f)
                return "010KM";
            else if (resolution == 0.25f)
                return "025KM";
            else if (resolution == 1.0f)
                return "100KM";
            else
                return "00000";
        }

        /// <summary>
        /// 生成非重复的文件名：如果已经存在了，自动在其后添加(1)或(2)等。
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private static string CreateOnlyFilename(string filename)
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

        public static bool IsDir(string path)
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
    }
}