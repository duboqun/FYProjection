using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PIE.Meteo.RasterProject
{
    public interface IProjectionTransform:IDisposable
    {
        void Transform(double[] xs,double[] ys);
        void InverTransform(double[] xs,double[] ys);
    }
}
