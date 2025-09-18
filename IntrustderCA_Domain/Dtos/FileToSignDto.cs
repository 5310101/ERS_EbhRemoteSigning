using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.util;

namespace IntrustCA_Domain.Dtos
{
    public class FileToSignDto<TProperties> where TProperties : FileProperties
    {
        public string file_id { get; set; }
        public string file_name { get; set; }
        public string content_file { get; set; }
        public string extension { get; set; }
        public TProperties properties { get; set; }

    }

    public abstract class FileProperties { }
    public class PdfProperties : FileProperties
    {
        public string pageNo { get; set; }
        public string coorDinate { get; set; }
        public string signTime { get; set; }
        public string positionIdentifier { get; set; }
        public string rectangleOffset { get; set; }
        public string rectangleSize { get; set; }
        //base64 data
        public string backgroundImage { get; set; }
        public bool showSignerInfo { get; set; } = true;
        public bool showDatetime { get; set; } = true;
        public bool showSignIcon { get; set; } = true;
        public bool showReason { get; set; }
    }

    public class XmlProperties : FileProperties
    {
        public string option_xml_form { get; set; }
        public string date_sign { get; set; }
        public string tag_signature { get; set; }
        public string tag_id { get; set; }
    }
}
