using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EBH_UpdateManual_Service.Model
{
    public class ZipFileModel
    {
        public string FileName { get; set; }      
        public byte[] FileData {  get; set; }   
    }
}