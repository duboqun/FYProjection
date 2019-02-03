using OSGeo.GDAL;

namespace PIE.Meteo.FileProject
{
    public class DatasetFactory
    {
        public static Dataset CreateRasterDataset(string outfilename, int outSizeWidth, int outSizeHeight,
            int dstBandCount, DataType dataType, string driverName, object o)
        {
            var driver = Gdal.GetDriverByName(driverName);
            Dataset outDs = null;
            if (driver != null)
            {
                outDs = driver.Create(outfilename, outSizeWidth, outSizeHeight, dstBandCount, dataType, null);
            }

            return outDs;
        }
    }
}