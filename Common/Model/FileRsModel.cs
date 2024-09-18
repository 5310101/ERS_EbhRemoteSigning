using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EBH_UpdateManual_Service.Model
{
    public class FileRsModel
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string DirectoryName {  get; set; }  
        public string RelativePath { get; set; }
        public DateTime FileDate { get; set; }
        public Boolean isUpdate { get; set; } = false;   
    }
}