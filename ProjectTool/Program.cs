using PIE.Meteo.FileProject;
using PIE.Meteo.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OSGeo.GDAL;

namespace PIE.Meteo.ProjectTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Gdal.AllRegister();
            //UInt16[] buffer = new ushort[100*100];
            //string hdfPath = @"E:\RSDATA\FY4A_0724\FY4A-_AGRI--_N_DISK_1047E_L1-_FDI-_MULT_NOM_20180724000000_20180724001459_0500M_V0001.HDF";
            //hdfPath = @"R:\FY4A\AGRI\L1\FDI\DISK\2018\20180804\FY4A-_AGRI--_N_DISK_1047E_L1-_FDI-_MULT_NOM_20180804010000_20180804011459_0500M_V0001.HDF";
            //var ds = DatasetFactory.OpenDataset(hdfPath, OpenMode.ReadOnly);
            //IMultiDataset mDs = ds as IMultiDataset;
            //var rds =  mDs.GetDataset("NOMChannel02") as IRasterDataset;
            //Console.WriteLine($"Width: {rds.GetRasterXSize()} Height: {rds.GetRasterYSize()}");
            //var band = rds.GetRasterBand(0);

            //bool ok = band.Read(10000, 10000, 100, 100, buffer, 100, 100, PixelDataType.UInt16);
            //Console.WriteLine($"读取{ok}");

            try
            {
                //ImportBlock s = new ImportBlock();
                //s.GetFeatures();
                //string xmlFile = "投影输入";
                string xmlFile = args[0];
                InputArg arg = InputArg.ParseXml(xmlFile);
                Console.WriteLine(arg.InputFilename);
                new Execute().Do(arg);

                //GenericFilename g = new GenericFilename();
                //string prjFilename = g.GenericPrjFilename("D:\\FY3A_MERSI_GBAL_L1_20120808_0000_1000M_MS.HDF", "GLL");
                //Console.WriteLine(prjFilename);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("ProjectTool.Main()", ex);
            }
        }
        private static object lockobj = new object();
        /// <summary>
        /// 日志保存
        /// </summary>
        /// <param name="sMessageInfo"></param>
        /// <param name="sFileName"></param>
        public static void SaveLog(string sMessageInfo, string sFileName)
        {
            lock (lockobj)
            {
                bool append = true;

                StreamWriter myWriter = null;
                try
                {
                    append = false;
                    myWriter = new StreamWriter(sFileName, append, Encoding.Default);
                    myWriter.Write(sMessageInfo);
                }
                catch
                {
                }
                finally
                {
                    if (myWriter != null)
                    {
                        myWriter.Close();
                    }
                }
            }
        }

        static void OnProgress(int progress, string text)
        {
            Console.WriteLine(progress + "," + text);
        }
    }
}
