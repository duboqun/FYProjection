using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace PIE.Meteo.FileProject
{
    public class Block : ICloneable
    {
        public int xBegin;
        public int xEnd;
        public int yBegin;
        public int yEnd;

        public Block()
        { }

        public Block(int _xBegin, int _xEnd, int _yBegin, int _yEnd)
        {
            this.xBegin = _xBegin;
            this.xEnd = _xEnd;
            this.yBegin = _yBegin;
            this.yEnd = _yEnd;
        }

        public int Width
        {
            get { return xEnd - xBegin + 1; }
        }

        public int Height
        {
            get { return yEnd - yBegin + 1; }
        }

        public Size Size
        {
            get { return new Size(Width, Height); }
        }

        public object Clone()
        {
            return new Block(xBegin, xEnd, yBegin, yEnd);
        }

        internal Block Zoom(int xZoom, int yZoom)
        {
            return new Block(xBegin * xZoom, xEnd * xZoom, yBegin * yZoom, yEnd * yZoom);
        }

        public static Block Empty
        {
            get
            {
                return new Block(0, -1, 0, -1);
            }
        }
    }
}
