using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PIE.Meteo.FileProject.BlockOper
{
    public static class RasterMoasicChecker
    {
        public static bool CheckDataType(AbstractWarpDataset[] srcRasters)
        {
            var type = srcRasters[0].DataType;
            for (int i = 1; i < srcRasters.Count(); i++)
            {
                if (type != srcRasters[i].DataType)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool CheckBandCount(AbstractWarpDataset[] srcRasters)
        {
            int count = srcRasters[0].BandCount;
            for (int i = 1; i < srcRasters.Count(); i++)
            {
                if (count != srcRasters[i].BandCount)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool CheckSpatialRef(AbstractWarpDataset[] srcRasters)
        {
            for (int i = 1; i < srcRasters.Count(); i++)
            {
                if (srcRasters[i].SpatialRef.IsSame(srcRasters[0].SpatialRef)!=1)
                {
                    return false;
                }
            }
            return true;
        }

    }
}
