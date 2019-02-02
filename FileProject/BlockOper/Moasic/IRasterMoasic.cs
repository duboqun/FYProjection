using PIE.DataSource;
using PIE.Meteo.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VBand = PIE.Meteo.FileProject.BlockOper.VirtualRasterBand;

namespace PIE.Meteo.FileProject.BlockOper
{
    public interface IRasterMoasic
    {
        /*DataType==,BandCount==,SpatialRef==,dstEnvelope = MaxEnvelope(srcRasters)*/
        AbstractWarpDataset Moasic(AbstractWarpDataset[] srcRasters, string driver, string outDir, bool isProcessInvalid, string[] invalidValues, Action<int, string> progressCallback, params object[] options);
        /*DataType==,BandCount==,SpatialRef==*/
        void Moasic(AbstractWarpDataset[] srcRasters, AbstractWarpDataset dstRaster,bool isProcessInvalid, string[] invalidValues,Action<int, string> progressCallback, params object[] options);
        /*DataType==,SpatialRef==*/
        void Moasic(VBand[] srcBands, VBand dstBand, Action<int, string> progressCallback, params object[] options);       
    }
}
