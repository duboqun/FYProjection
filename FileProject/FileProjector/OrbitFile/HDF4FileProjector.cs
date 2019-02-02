﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace GeoDo.FileProject
{
    public class HDF4FileProjector : FileProjector
    {
        private HDF4FilePrjSettings _prjSettings = null;
        private WarpDataset _locationRaster = null;
        private ISpatialReference _srcSpatialRef = null;

        public HDF4FileProjector()
            : base()
        {
            _name = "HDF4";
            _fullname = "HDF4轨道文件投影";
            _rasterProjector = new RasterProjector();
            _srcSpatialRef = SpatialReference.GetDefault();
        }

        public override bool IsSupport(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;
            return false;
        }

        public override void ComputeDstEnvelope(RSS.Core.DF.WarpDataset srcRaster, Project.ISpatialReference dstSpatialRef, out RasterProject.PrjEnvelope maxPrjEnvelope, Action<int, string> progressCallback)
        {
            //throw new NotImplementedException();
            if (srcRaster != null)
            {
                Size srcSize = new Size(srcRaster.Width, srcRaster.Height);
                double[] xs, ys;
                ReadyLocations(srcRaster, dstSpatialRef, srcSize, out xs, out ys, out maxPrjEnvelope, progressCallback);
            }
            else
            {
                maxPrjEnvelope = PrjEnvelope.Empty;
            }
        }
        /// <summary> 
        /// 准备定位信息,计算投影后的值，并计算范围
        /// </summary>
        private void ReadyLocations(WarpDataset srcRaster, ISpatialReference dstSpatialRef, Size srcSize,
            out double[] xs, out double[] ys, out PrjEnvelope maxPrjEnvelope, Action<int, string> progressCallback)
        {
            Size locationSize;
            ReadLocations(srcRaster, out xs, out ys, out locationSize);
            TryResetLonlatForLeftRightInvalid(xs, ys, locationSize);
            _rasterProjector.ComputeDstEnvelope(_srcSpatialRef, xs, ys, srcSize, dstSpatialRef, out maxPrjEnvelope, progressCallback);
        }

        public override FilePrjSettings CreateDefaultPrjSettings()
        {
            return new HDF4FilePrjSettings();
        }

        protected override void DoRadiation(RSS.Core.DF.WarpDataset srcImgRaster, int i, ushort[] srcBandData, float[] solarZenithData, System.Drawing.Size srcBlockImgSize, System.Drawing.Size angleSize)
        {
        }

        protected override void ReadLocations(WarpDataset locationRaster, out double[] xs, out double[] ys, out System.Drawing.Size locationSize)
        {
            IBandProvider srcbandpro = locationRaster.BandProvider as IBandProvider;
            {
                IRasterBand[] lonsBands = srcbandpro.GetBands("Longitude");
                using (IRasterBand lonsBand = lonsBands[0])
                {
                    locationSize = new Size(lonsBand.Width, lonsBand.Height);
                    xs = new Double[lonsBand.Width * lonsBand.Height];
                    unsafe
                    {
                        fixed (Double* ptrLong = xs)
                        {
                            IntPtr bufferPtrLong = new IntPtr(ptrLong);
                            lonsBand.Read(0, 0, lonsBand.Width, lonsBand.Height, bufferPtrLong, enumDataType.Double, lonsBand.Width, lonsBand.Height);
                        }
                    }
                }
                IRasterBand[] latBands = srcbandpro.GetBands("Latitude");
                using (IRasterBand latBand = latBands[0])
                {
                    ys = new Double[locationSize.Width * locationSize.Height];
                    unsafe
                    {
                        fixed (Double* ptrLat = ys)
                        {
                            {
                                IntPtr bufferPtrLat = new IntPtr(ptrLat);
                                latBand.Read(0, 0, latBand.Width, latBand.Height, bufferPtrLat, enumDataType.Double, latBand.Width, latBand.Height);
                            }
                        }
                    }
                }
            }
            if (_xzoom != 1d)
            {
                for (int i = 0; i < xs.Length; i++)
                {
                    xs[i] = xs[i] * _xzoom;
                }
            }
            if (_xzoom != 1d)
            {
                for (int i = 0; i < ys.Length; i++)
                {
                    ys[i] = ys[i] * _yzoom;
                }
            }
            if (_prjSettings != null && _prjSettings.ExtArgs != null && _prjSettings.ExtArgs.Contains("360"))
            {
                for (int i = 0; i < xs.Length; i++)
                {
                    if (xs[i] > 180)
                        xs[i] = xs[i] - 360d;
                }
            }
        }

        public override void Project(RSS.Core.DF.WarpDataset srcRaster, FilePrjSettings prjSettings, Project.ISpatialReference dstSpatialRef, Action<int, string> progressCallback)
        {
            try
            {
                ReadyArgs(srcRaster, prjSettings, dstSpatialRef, progressCallback);
                WarpDataset outwriter = null;
                try
                {
                    Size outSize = _prjSettings.OutSize;
                    string[] options = new string[]{
                            "INTERLEAVE=BSQ",
                            "VERSION=LDF",
                            "WITHHDR=TRUE",
                            "SPATIALREF=" + _dstSpatialRef.ToProj4String(),
                            "MAPINFO={" + 1 + "," + 1 + "}:{" + _prjSettings.OutEnvelope.MinX + "," + _prjSettings.OutEnvelope.MaxY + "}:{" + _outResolutionX + "," + _outResolutionY + "}"
                        };
                    outwriter = CreateOutFile(_outfilename, _dstBandCount, outSize, srcRaster.DataType, options);
                    if (!_fillValue.HasValue)
                        ProjectRaster(srcRaster, outwriter, 0, progressCallback);
                    else
                        ProjectRaster(srcRaster, outwriter, 0, _fillValue.Value, progressCallback);
                }
                catch (IOException ex)
                {
                    if (ex.Message == "磁盘空间不足。\r\n" && File.Exists(_outfilename))
                        File.Delete(_outfilename);
                    throw ex;
                }
                finally
                {
                    if (outwriter != null)
                    {
                        outwriter.Dispose();
                        outwriter = null;
                    }
                }
            }
            catch
            {
                EndSession();
                TryDeleteCurCatch();
                throw;
            }
            finally
            {
                if (_curSession == null)
                {
                    EndSession();
                    if (prjSettings.IsClearPrjCache)
                        TryDeleteCurCatch();
                }
            }
        }

        /// <summary>
        /// 投影
        /// </summary>
        /// <param name="srcRaster"></param>
        /// <param name="prjSettings"></param>
        /// <param name="dstRaster"></param>
        /// <param name="beginBandIndex"></param>
        /// <param name="progressCallback"></param>
        /// <returns></returns>
        public override WarpDataset Project(WarpDataset srcRaster, FilePrjSettings prjSettings, RSS.Core.DF.WarpDataset dstRaster, int beginBandIndex, Action<int, string> progressCallback)
        {
            if (dstRaster == null)
                return null;
            try
            {
                _dstSpatialRef = dstRaster.SpatialRef;
                CoordEnvelope coordEnv = dstRaster.CoordEnvelope;
                prjSettings.OutEnvelope = PrjEnvelope.CreateByLeftTop(coordEnv.MinX, coordEnv.MaxY, coordEnv.Width, coordEnv.Height);
                ReadyArgs(srcRaster, prjSettings, dstRaster.SpatialRef, progressCallback);
                string outfilename = _prjSettings.OutPathAndFileName;
                try
                {
                    Size outSize = new Size(dstRaster.Width, dstRaster.Height);
                    string[] options = new string[] { 
                            "INTERLEAVE=BSQ",
                            "VERSION=LDF",
                            "WITHHDR=TRUE",
                            "SPATIALREF=" + _dstSpatialRef.ToProj4String(),
                            "MAPINFO={" + 1 + "," + 1 + "}:{" + _prjSettings.OutEnvelope.MinX + "," + _prjSettings.OutEnvelope.MaxY + "}:{" + dstRaster.ResolutionX + "," + dstRaster.ResolutionY + "}"
                            ,"BANDNAMES="+ BandNameString(_prjSettings.OutBandNos)    
                    };
                    if (!_fillValue.HasValue)
                        ProjectRaster(srcRaster, dstRaster, 0, progressCallback);
                    else
                        ProjectRaster(srcRaster, dstRaster, 0, _fillValue.Value, progressCallback);
                    return dstRaster;
                }
                catch (IOException ex)
                {
                    if (ex.Message == "磁盘空间不足。\r\n" && File.Exists(outfilename))
                        File.Delete(outfilename);
                    throw ex;
                }
                finally
                {
                    if (dstRaster != null)
                    {
                        dstRaster.Dispose();
                        dstRaster = null;
                    }
                }
            }
            catch
            {
                EndSession();
                TryDeleteCurCatch();
                throw;
            }
            finally
            {
                if (_curSession == null)
                {
                    EndSession();
                    if (prjSettings.IsClearPrjCache)
                        TryDeleteCurCatch();
                }
            }
        }

        private void ReadyArgs(WarpDataset srcRaster, FilePrjSettings prjSettings, ISpatialReference dstSpatialRef, Action<int, string> progressCallback)
        {
            float resolutionScale = 1f;
            _readyProgress = 0;
            if (progressCallback != null)
                progressCallback(_readyProgress++, "准备相关参数");
            if (dstSpatialRef == null)
                dstSpatialRef = SpatialReference.GetDefault();
            _dstSpatialRef = dstSpatialRef;
            _prjSettings = ArgsCheck(srcRaster, prjSettings, progressCallback);
            //_fileType = CheckFile(srcRaster);
            _locationRaster = (prjSettings as HDF4FilePrjSettings).LocationFile;
            ReadExtArgs(prjSettings);
            TryCreateDefaultArg(srcRaster, _prjSettings, ref _dstSpatialRef);
            _left = 0;
            _right = 0;
            TrySetLeftRightInvalidPixel(_prjSettings.ExtArgs);
            DoSession(srcRaster, _dstSpatialRef, _prjSettings, progressCallback);
            if (_prjSettings.OutEnvelope == null || _prjSettings.OutEnvelope == PrjEnvelope.Empty)
            {
                _prjSettings.OutEnvelope = _maxPrjEnvelope;
                _orbitBlock = new Block { xOffset = 0, yBegin = 0, xEnd = srcRaster.Width - 1, yEnd = srcRaster.Height - 1 };
            }
            else
            {
                GetEnvelope(_xs, _ys, _srcLocationSize.Width, _srcLocationSize.Height, _prjSettings.OutEnvelope, out _orbitBlock);
                if (_orbitBlock == null || _orbitBlock.Width <= 0 || _orbitBlock.Height <= 0)
                    throw new Exception("数据不在目标区间内");
                float invalidPresent = (_orbitBlock.Width * _orbitBlock.Height * resolutionScale) / (srcRaster.Width * srcRaster.Height);
                if (invalidPresent < 0.0001f)
                    throw new Exception("数据占轨道数据比例太小,有效率" + invalidPresent * 100 + "%");
                if (invalidPresent > 0.60f)
                    _orbitBlock = new Block { xOffset = 0, yBegin = 0, xEnd = srcRaster.Width - 1, yEnd = srcRaster.Height - 1 };
            }
            _dstEnvelope = _prjSettings.OutEnvelope;
            if (!_dstEnvelope.IntersectsWith(_maxPrjEnvelope))
                throw new Exception("数据不在目标区间内");
            _outResolutionX = _prjSettings.OutResolutionX;
            _outResolutionY = _prjSettings.OutResolutionY;
            _outFormat = _prjSettings.OutFormat;
            _outfilename = _prjSettings.OutPathAndFileName;
            _dstProj4 = _dstSpatialRef.ToProj4String();
            //_dstBandCount = _prjBands.Length;
            _dstBandCount = _rasterDataBands.Length;
            _dstSize = _prjSettings.OutSize;
            //_isRadiation = _prjSettings.IsRadiation;
            //_isSolarZenith = _prjSettings.IsSolarZenith;
            //_isSensorZenith = _prjSettings.IsSensorZenith;
        }

        private double _xzoom = 1d;
        private double _yzoom = 1d;
        private double? _fillValue = null;

        /// <summary>
        /// 针对某些经纬度数据集，使用整型数据表达的数据提供支持
        /// 如xzoom=0.0001，xzoom=0.0001
        /// 需要在读取经度后，将其数据*0.0001还原为真实的经纬度值
        /// </summary>
        /// <param name="prjSettings"></param>
        private void ReadExtArgs(FilePrjSettings prjSettings)
        {
            if (prjSettings.ExtArgs != null && prjSettings.ExtArgs.Length != 0)
            {
                foreach (object arg in prjSettings.ExtArgs)
                {
                    if (arg is Dictionary<string, double>)
                    {
                        Dictionary<string, double> exAtg = arg as Dictionary<string, double>;
                        if (exAtg.ContainsKey("xzoom"))
                            _xzoom = exAtg["xzoom"];
                        if (exAtg.ContainsKey("yzoom"))
                            _yzoom = exAtg["yzoom"];
                        if (exAtg.ContainsKey("FillValue"))
                            _fillValue = exAtg["FillValue"];
                    }
                }
            }
        }

        private void TryCreateDefaultArg(WarpDataset srcRaster, HDF4FilePrjSettings prjSettings, ref ISpatialReference dstSpatialRef)
        {
            if (dstSpatialRef == null)
                dstSpatialRef = _srcSpatialRef;
            if (string.IsNullOrWhiteSpace(prjSettings.OutFormat))
                prjSettings.OutFormat = "LDF";
            if (dstSpatialRef.ProjectionCoordSystem == null)
            {
                _srcImgResolution = 0.01f;
            }
            else
            {
                _srcImgResolution = 0.01f;
            }
            if (prjSettings.OutResolutionX == 0 || prjSettings.OutResolutionY == 0)
            {
                if (dstSpatialRef.ProjectionCoordSystem == null)
                {
                    prjSettings.OutResolutionX = 0.01f;
                    prjSettings.OutResolutionY = 0.01f;
                }
                else
                {
                    prjSettings.OutResolutionX = 1000f;
                    prjSettings.OutResolutionY = 1000f;
                }
            }
        }

        private void SetPrjBand(HDF4FilePrjSettings prjSettings, PrjBand[] defaultPrjBands)
        {
        }

        private HDF4FilePrjSettings ArgsCheck(WarpDataset srcRaster, FilePrjSettings prjSettings, Action<int, string> progressCallback)
        {
            if (srcRaster == null)
                throw new ArgumentNullException("srcRaster", "待投影数据为空");
            if (prjSettings == null)
                throw new ArgumentNullException("prjSettings", "投影参数为空");
            HDF4FilePrjSettings _prjSettings = prjSettings as HDF4FilePrjSettings;
            if (_prjSettings.LocationFile == null)
                throw new ArgumentNullException("prjSettings.LocationFile", "L2L3级轨道数据投影未设置经纬度数据集文件");
            return _prjSettings;
        }

        private void DoSession(WarpDataset srcRaster, ISpatialReference dstSpatialRef, HDF4FilePrjSettings prjSettings, Action<int, string> progressCallback)
        {
            if (_curSession == null || _curSession != srcRaster || _isBeginSession)
            {
                WarpDataset locationRester = prjSettings.LocationFile;
                ReadyLocations(locationRester, dstSpatialRef, out _xs, out _ys, out _maxPrjEnvelope, out _srcLocationSize, progressCallback);
                if (progressCallback != null)
                    progressCallback(_readyProgress++, "准备其他参数");
                //if (prjSettings.IsSolarZenith && prjSettings.IsRadiation)
                //{
                //    _szDataFilename = GetSolarZenithCacheFilename(locationRester.fileName);
                //    if (!File.Exists(_szDataFilename))
                //        ReadySolarZenithArgsToFile(locationRester);
                //    else
                //        _solarZenithCacheRaster = GeoDataDriver.Open(_szDataFilename) as WarpDataset;
                //    if (prjSettings.IsSensorZenith)
                //    {
                //        ReadySensorZenith(locationRester);
                //    }
                //}
                _rasterDataBands = TryCreateRasterDataBands(srcRaster, prjSettings, progressCallback);
                _isBeginSession = false;
            }
        }

        private void ReadyLocations(WarpDataset locationRaster, ISpatialReference dstSpatialRef,
            out double[] xs, out double[] ys, out PrjEnvelope maxPrjEnvelope, out Size locationSize, Action<int, string> progressCallback)
        {
            if (progressCallback != null)
                progressCallback(_readyProgress++, "读取经纬度数据集");
            ReadLocations(locationRaster, out xs, out ys, out locationSize);
            TryResetLonlatForLeftRightInvalid(xs, ys, locationSize);
            if (progressCallback != null)
                progressCallback(_readyProgress++, "预处理经纬度数据集");
            PrjEnvelope maskEnvelope = new PrjEnvelope(-180d, 180d, -90d, 90d);
            _rasterProjector.ComputeDstEnvelope(_srcSpatialRef, xs, ys, locationSize, dstSpatialRef, maskEnvelope, out maxPrjEnvelope, progressCallback);
        }

        private IRasterBand[] TryCreateRasterDataBands(WarpDataset srcRaster, HDF4FilePrjSettings prjSettings, Action<int, string> progressCallback)
        {
            IBandProvider srcbandpro = srcRaster.BandProvider as IBandProvider;
            int count = srcRaster.BandCount;
            List<IRasterBand> rasterBands = new List<IRasterBand>();
            for (int i = 0; i < count; i++)
            {
                if (progressCallback != null)
                    progressCallback(_readyProgress++, "准备第" + i + "个输入数据通道");
                IRasterBand band = srcRaster.GetRasterBand(i + 1);
                rasterBands.Add(band);
            }
            return rasterBands.ToArray();
        }
    }
}
