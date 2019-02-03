using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VBand = PIE.Meteo.FileProject.BlockOper.VirtualRasterBand;

namespace PIE.Meteo.FileProject.BlockOper
{
    public interface IRasterClip
    {
        /*samplePercent=[1~100]*/
        AbstractWarpDataset[] Clip(AbstractWarpDataset srcRaster, BlockDef[] blockDefs, int samplePercent, string driver, string outdir, Action<int, string> progressCallback, params object[] options);
        VBand[] Clip(VBand srcBand, BlockDef[] blockDefs, int samplePercent, string driver, Action<int, string> progressCallback, params object[] options);
    }
}
