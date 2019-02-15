using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OSGeo.OSR;

namespace PIE.Meteo.RasterProject
{
    /// <summary>
    /// This class is a thicker interface to the PROJ.4 library.  It exposes a small 
    /// set of methods, but generally these are all that is needed.
    /// </summary>
    public class Proj4Projection : IDisposable
    {
        public const double RAD_TO_DEG = 57.29577951308232;
        public const double DEG_TO_RAD = .0174532925199432958;
        
        private string proj4Str = string.Empty;
        SpatialReference srs = null;

        /// <summary>
        /// Cache of the definition string returned by pj_get_def
        /// </summary>
        internal string out_def = null;

        //internal CoordinateDomain _coordinateDomain = null;

        /// <summary>
        /// The default constructor
        /// </summary>
        public Proj4Projection()
        {
        }

        /// <summary>
        /// Constructor with a definition
        /// </summary>
        /// <param name="paramaters">string defining the coordinate system</param>
        public Proj4Projection(string definition)
            : base()
        {
            this.Initialize(definition);
        }

        private void Initialize(string definition)
        {
            proj4Str = definition;
            srs = new SpatialReference("");
            srs.ImportFromProj4(proj4Str);

            this.out_def = null;
        }

        public static int GetErrNo()
        {
            int errno = 0;
            return errno;
        }

        public static string GetErrorMessage(int errno)
        {
            if (errno == 0) return null;
            return String.Empty;
        }

        private void CheckInitialized()
        {
            Proj4Projection.CheckInitialized(this);
        }

        private static void CheckInitialized(Proj4Projection p)
        {
            if (p.srs == null)
            {
                throw new ApplicationException("Projection not initialized");
            }
        }
        // PROPERTIES

        public string Definition
        {
            set { this.Initialize(value); }
            get
            {
                this.CheckInitialized();
                if (this.out_def == null)
                {
                    if (srs.IsGeographic() == 1)
                        out_def = srs.GetAttrValue("GEOGCS", 0);
                    else
                        out_def = srs.GetAttrValue("PROJCS", 0);
                }

                return this.out_def;
            }
        }

        public bool IsLatLong
        {
            get
            {
                this.CheckInitialized();
                return srs.IsGeographic() == 1;
            }
        }

        public override string ToString()
        {
            return this.Definition;
        }

        public void Transform(Proj4Projection dst, double[] x, double[] y)
        {
            this.Transform(dst, x, y, null);
        }

        public void Transform(Proj4Projection dst, double[] x, double[] y, double[] z)
        {
            Proj4Projection.Transform(this, dst, x, y, z);
        }

        public static void Transform(Proj4Projection src, Proj4Projection dst,
            double[] x, double[] y)
        {
            {
                Proj4Projection.Transform(src, dst, x, y, null);
            }
        }


        private static string GetProjParam(string projStr, string key, string defaultValue = "")
        {
            string[] parts = projStr.Split(' ');
            foreach (var item in parts)
            {
                if (item.Contains(key))
                {
                    return item.Replace(key + "=", "");
                }
            }

            return defaultValue;
        }

        private static double SPI = 3.14159265359;
        private static double TWOPI = 6.2831853071795864769;
        private static double ONEPI = 3.14159265358979323846;

        private static object lockObj = new object();

        public static void Transform(Proj4Projection src, Proj4Projection dst,
            double[] x, double[] y, double[] z)
        {
            lock (lockObj)
            {
                //Proj4Projection.CheckInitialized(src);
                //Proj4Projection.CheckInitialized(dst);
                if (x == null)
                {
                    throw new ArgumentException("Argument is required", "x");
                }

                if (y == null)
                {
                    throw new ArgumentException("Argument is required", "y");
                }

                if (x.Length != y.Length || (z != null && z.Length != x.Length))
                {
                    throw new ArgumentException("Coordinate arrays must have the same length");
                }

                CoordinateTransformation trans = new CoordinateTransformation(src.srs, dst.srs);

                trans.TransformPoints(x.Length, x, y, z);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (srs != null)
                srs.Dispose();
        }

        #endregion
    }
}