using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace testSigning_Winform.Model
{
    public enum FileType
    {
        PDF,
        XML,
        OFFICE
    }

    public class RectanglePosition
    {
        public float rx;
        public float ry;    
        public float lx;
        public float ly;

        public RectanglePosition(float rx, float ry, float lx, float ly)
        {
            this.rx = rx;
            this.ry = ry;
            this.lx = lx;
            this.ly = ly;
        }
    }

    public class SignFileInfo
    {
        public string  FileName { get; set; }
        public FileType type { get; set; }
        public byte[] Data { get; set; }
        //optional property only pdf
        public int PageSign { get; set; } = 0;
        public RectanglePosition SignPosition { get; set; } = null;
    }
}