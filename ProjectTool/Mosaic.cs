using PIE.Meteo.FileProject.BlockOper;
using PIE.Meteo.RasterProject;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using PIE.Meteo.FileProject;

namespace PIE.Meteo.ProjectTool
{
    public class Mosaic
    {
        private InputArg inArg;
        private AbstractWarpDataset fileRaster = null;
        private Action<int, string> action;

        public Mosaic(InputArg inArg, AbstractWarpDataset infileRaster, Action<int, string> action)
        {
            this.inArg = inArg;
            this.fileRaster = infileRaster;
            this.action = action;
        }

        internal AbstractWarpDataset MosaicToFile(string mosaicFilename)
        {
            AbstractWarpDataset mosaicFileRaster = null;
            try
            {
                if (File.Exists(mosaicFilename))
                {
                    var rDs = Gdal.Open(mosaicFilename, Access.GA_Update);
                    mosaicFileRaster = new WarpDataset(rDs,mosaicFilename);
                    if (mosaicFileRaster.BandCount != fileRaster.BandCount)
                    {
                        mosaicFileRaster.Dispose();
                        mosaicFileRaster = CreateMosaicFile(mosaicFilename, inArg);
                    }
                }
                else
                {
                    mosaicFileRaster = CreateMosaicFile(mosaicFilename, inArg);
                }
                if (mosaicFileRaster == null)
                    return null;
                RasterMoasicProcesser mo = new RasterMoasicProcesser();
                //mo.Moasic(new IRasterDataProvider[] { fileRaster }, mosaicFileRaster, true, new string[] { "0" }, action);
                mo.MoasicSimple(fileRaster, mosaicFileRaster, true, new string[] { "0" }, action);
                return mosaicFileRaster;
            }
            finally
            {
            }
        }

        private AbstractWarpDataset CreateMosaicFile(string mosaicFilename, InputArg inArg)
        {
            SpatialReference spatialRef = fileRaster.SpatialRef;
            PrjEnvelope env = inArg.MosaicInputArg.Envelope.PrjEnvelope;
            //string bandNames = BandNameString(fileRaster as ILdfDataProvider);
            Size outSize = env.GetSize(fileRaster.ResolutionX, fileRaster.ResolutionY);
            string[] options = new string[]{
                            "INTERLEAVE=BSQ",
                            "VERSION=LDF",
                            "WITHHDR=TRUE",
                            "SPATIALREF=" + spatialRef.ExportToProj4(),
                            "MAPINFO={" + 1 + "," + 1 + "}:{" + env.MinX + "," + env.MaxY + "}:{" + fileRaster.ResolutionX + "," + fileRaster.ResolutionY + "}",
                            //"BANDNAMES="+ bandNames
                        };
            return CreateOutFile(mosaicFilename, fileRaster.BandCount, outSize, DataType.GDT_UInt16, env,spatialRef, options);
        }

        //private string BandNameString(ILdfDataProvider fileRaster)
        //{
        //    if (fileRaster == null)
        //        return null;
        //    string[] bandNames = (fileRaster as ILdfDataProvider).Hdr.BandNames;
        //    if (bandNames == null || bandNames.Length == 0)
        //        return null;
        //    string bandNameString = "";
        //    foreach (string b in bandNames)
        //    {
        //        bandNameString = bandNameString + b + ",";
        //    }
        //    return bandNameString.TrimEnd(',');
        //}

        internal AbstractWarpDataset CreateOutFile(string outfilename, int dstBandCount, Size outSize, DataType dataType,PrjEnvelope prjEnv,SpatialReference spaRef, string[] options)
        {
            string dir = Path.GetDirectoryName(outfilename);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            Envelope env = new Envelope { MaxX = prjEnv.MaxX, MinX = prjEnv.MinX, MaxY = prjEnv.MaxY, MinY = prjEnv.MinY };
            var rDs =DatasetFactory.CreateRasterDataset(outfilename, outSize.Width, outSize.Height, dstBandCount, dataType,"GTiff", null);
            //env
            double[] geoTrans = Enumerable.Repeat(0.0, 6).ToArray();
            geoTrans[0] = prjEnv.MinX;
            geoTrans[3] = prjEnv.MaxY;
            geoTrans[1] = prjEnv.Width / outSize.Width;
            geoTrans[5] = -prjEnv.Height / outSize.Height;
            rDs.SetProjection(spaRef.ExportToWkt());
            return new WarpDataset(rDs,outfilename);
        }
    }
}
