using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OSGeo.OSR;

namespace PIE.Meteo.RasterProject
{
    public static class ProjectionTransformFactory
    {
        public static IProjectionTransform GetDefault()
        {
            return new PrjTranSimpleEquidistantCyclindrical();
        }

        public static IProjectionTransform GetProjectionTransform(SpatialReference srcSpatialRef, SpatialReference dstSpatialRef)
        {
            
            if (dstSpatialRef == null && srcSpatialRef.IsGeographic()==1)//GLL  smart中代码
                return new PrjTranSimpleEquidistantCyclindrical();
            return new ProjectionTransform(srcSpatialRef, dstSpatialRef);
        }
    }
}
