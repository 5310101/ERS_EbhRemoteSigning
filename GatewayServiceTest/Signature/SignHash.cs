using Newtonsoft.Json;
using System;
using System.IO;
using VnptHashSignatures.Interface;
using VnptHashSignatures.Pdf;
using VnptHashSignatures.Xml;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Collections.Generic;

namespace GatewayServiceTest.Signature
{
    /// <summary>
    /// Calculate hash value on client, 
    /// send hash value to create signature, 
    /// finaly package signed file on client
    /// </summary>
    public class SignHash
    {
        // Logger for this class
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(SignHash));

        // Sample pdf input file
        private const string _pdfInput = @"C:\Users\Hung Vu\Desktop\test.pdf";
        private const string _pdfInput1 = @"F:/WORK/2018/07-2018/input1.pdf";
        private const string _pdfSignedPath = @"C:\Users\Hung Vu\Desktop\test_signed.pdf";
        private const string _textFilter = "Dịch vụ VNPT-Kyso";
        private const int _width = 220;
        private const int _height = 80;
        private const int _marginTop = 15;

        // Sample office input file
        private const string _officeInput = @"F:/WORK/2018/07-2018/input.docx";
        private const string _officeSignedPath = @"F:/WORK/2019/05-2019/signed.docx";

        // Sample xml input file
        private const string _xmlInput = @"D:\OFFICE\Documents\2019\2019-07\input.xml";
        private const string _xmlSignedPath = @"D:\OFFICE\Documents\2019\2019-07\signed.xml";



        public class LocationTextExtractionStrategyWithPosition : LocationTextExtractionStrategy
        {
            private readonly List<TextChunk> locationalResult = new List<TextChunk>();

            private readonly ITextChunkLocationStrategy tclStrat;

            public LocationTextExtractionStrategyWithPosition() : this(new TextChunkLocationStrategyDefaultImp())
            {
            }

            /**
             * Creates a new text extraction renderer, with a custom strategy for
             * creating new TextChunkLocation objects based on the input of the
             * TextRenderInfo.
             * @param strat the custom strategy
             */
            public LocationTextExtractionStrategyWithPosition(ITextChunkLocationStrategy strat)
            {
                tclStrat = strat;
            }


            private bool StartsWithSpace(string str)
            {
                if (str.Length == 0) return false;
                return str[0] == ' ';
            }


            private bool EndsWithSpace(string str)
            {
                if (str.Length == 0) return false;
                return str[str.Length - 1] == ' ';
            }

            /**
             * Filters the provided list with the provided filter
             * @param textChunks a list of all TextChunks that this strategy found during processing
             * @param filter the filter to apply.  If null, filtering will be skipped.
             * @return the filtered list
             * @since 5.3.3
             */

            private List<TextChunk> filterTextChunks(List<TextChunk> textChunks, ITextChunkFilter filter)
            {
                if (filter == null)
                {
                    return textChunks;
                }

                var filtered = new List<TextChunk>();

                foreach (var textChunk in textChunks)
                {
                    if (filter.Accept(textChunk))
                    {
                        filtered.Add(textChunk);
                    }
                }

                return filtered;
            }

            public override void RenderText(TextRenderInfo renderInfo)
            {
                LineSegment segment = renderInfo.GetBaseline();
                if (renderInfo.GetRise() != 0)
                { // remove the rise from the baseline - we do this because the text from a super/subscript render operations should probably be considered as part of the baseline of the text the super/sub is relative to 
                    Matrix riseOffsetTransform = new Matrix(0, -renderInfo.GetRise());
                    segment = segment.TransformBy(riseOffsetTransform);
                }
                TextChunk tc = new TextChunk(renderInfo.GetText(), tclStrat.CreateLocation(renderInfo, segment));
                locationalResult.Add(tc);
            }


            public IList<TextLocation> GetLocations()
            {

                var filteredTextChunks = filterTextChunks(locationalResult, null);
                filteredTextChunks.Sort();

                TextChunk lastChunk = null;

                var textLocations = new List<TextLocation>();

                foreach (var chunk in filteredTextChunks)
                {

                    if (lastChunk == null)
                    {
                        //initial
                        textLocations.Add(new TextLocation
                        {
                            Text = chunk.Text,
                            X = chunk.Location.StartLocation[0],
                            Y = chunk.Location.StartLocation[1]                            
                        });

                    }
                    else
                    {
                        if (chunk.SameLine(lastChunk))
                        {
                            var text = "";
                            // we only insert a blank space if the trailing character of the previous string wasn't a space, and the leading character of the current string isn't a space
                            if (IsChunkAtWordBoundary(chunk, lastChunk) && !StartsWithSpace(chunk.Text) && !EndsWithSpace(lastChunk.Text))
                                text += ' ';

                            text += chunk.Text;

                            textLocations[textLocations.Count - 1].Text += text;

                        }
                        else
                        {

                            textLocations.Add(new TextLocation
                            {
                                Text = chunk.Text,
                                X = chunk.Location.StartLocation[0],
                                Y = chunk.Location.StartLocation[1]
                            });
                        }
                    }
                    lastChunk = chunk;
                }

                //now find the location(s) with the given texts
                return textLocations;

            }
        }

        public class TextLocation
        {
            public float X { get; set; }
            public float Y { get; set; }

            public string Text { get; set; }
        }


        /// <summary>
        /// Create pdf external signature
        /// </summary>
        /// <param name="certId">Certificate ID using to create signature</param>
        /// <param name="certBase64">Certificate data in base64 format to calculate second hash value</param>
        /// <param name="access_token">access_token to authorize request</param>
        public static void SignHashPdf(string groupId, string certId, string certBase64, string access_token)
        {
            _log.Info("SignHash PDF: Starting...");

            // 1. Get second hash value from unsigned data ---------------------------------------------------
            byte[] unsignData = null;
            try
            {
                unsignData = File.ReadAllBytes(_pdfInput);
            }
            catch(Exception ex)
            {
                _log.Error(ex);
                return;
            }
            //var tsaUrl = "https://timestamp.geotrust.com/tsa";
            IHashSigner signer = HashSignerFactory.GenerateSigner(unsignData, certBase64, null, HashSignerFactory.PDF);

            #region Optional -----------------------------------
            // Property: Lý do ký số
            ((PdfHashSigner)signer).SetReason("Xác nhận tài liệu");
            // Hình ảnh hiển thị trên chữ ký (mặc định là logo VNPT)
            var imgBytes = File.ReadAllBytes(@"C:\Users\Hung Vu\Desktop\Logo_MISA.jpg");
            var x = Convert.ToBase64String(imgBytes);
            ((PdfHashSigner)signer).SetCustomImage(imgBytes);
            // Signing page (@deprecated)
            //((PdfHashSigner)signer).SetSigningPage(1);
            // Vị trí và kích thước chữ ký (@deprecated)
            //((PdfHashSigner)signer).SetSignaturePosition(20, 20, 220, 50);
            // Kiểu hiển thị chữ ký (OPTIONAL/DEFAULT=TEXT_WITH_BACKGROUND)
            ((PdfHashSigner)signer).SetRenderingMode(PdfHashSigner.RenderMode.TEXT_WITH_BACKGROUND);
            // Nội dung text trên chữ ký (OPTIONAL)
            //((PdfHashSigner)signer).SetLayer2Text("Ký bởi: Subject name\nNgày ký: Datetime.now");
            // Fontsize cho text trên chữ ký (OPTIONAL/DEFAULT = 10)
            ((PdfHashSigner)signer).SetFontSize(10);
            ((PdfHashSigner)signer).SetLayer2Text("yahooooooooooooooooooooooooooo");
            // Màu text trên chữ ký (OPTIONAL/DEFAULT=000000)
            ((PdfHashSigner)signer).SetFontColor("0000ff");
            // Kiểu chữ trên chữ ký
            ((PdfHashSigner)signer).SetFontStyle(PdfHashSigner.FontStyle.Normal);
            // Font chữ trên chữ ký
            ((PdfHashSigner)signer).SetFontName(PdfHashSigner.FontName.Times_New_Roman);

            //Hiển thị chữ ký và vị trí bên dưới dòng _textFilter cách 1 đoạn _marginTop, độ rộng _width, độ cao _height
            //using (var reader = new PdfReader(unsignData))
            //{

            //    var parser = new PdfReaderContentParser(reader);

            //    for (int pageNum = 1; pageNum <= reader.NumberOfPages; ++pageNum)
            //    {
            //        var strategy = parser.ProcessContent(pageNum, new LocationTextExtractionStrategyWithPosition());

            //        var res = strategy.GetLocations();

            //        var post = new TextLocation();

            //        foreach (TextLocation textContent in res)
            //        {
            //            if (textContent.Text.Contains(_textFilter))
            //            {
            //                ((PdfHashSigner)signer).AddSignatureView(new PdfSignatureView
            //                {
            //                    Rectangle = string.Format("{0},{1},{2},{3}", (int)textContent.X, (object)(int)(textContent.Y - _marginTop - _height), (int)(textContent.X + _width), (int)(textContent.Y - _marginTop)),
            //                    Page = pageNum
            //                });
            //            }
            //        }
            //    }

                

            //    reader.Close();
            //    //var searchResult = res.Where(p => p.Text.Contains(searchText)).OrderBy(p => p.Y).Reverse().ToList();
            //}

            // Hiển thị ảnh chữ ký tại nhiều vị trí trên tài liệu
            ((PdfHashSigner)signer).AddSignatureView(new PdfSignatureView
            {
                Rectangle = "349,488,569,554",
                Page = 5
            });
            // Hiển thị ảnh chữ ký tại nhiều vị trí trên tài liệu
            ((PdfHashSigner)signer).AddSignatureView(new PdfSignatureView
            {
                Rectangle = "49,488,269,554",
                Page = 1
            });

            //((PdfHashSigner)signer).AddSignatureComment(new PdfSignatureComment
            //{
            //    Type = (int)PdfSignatureComment.Types.TEXT,
            //    Text = "This is comment",
            //    Page = 1,
            //    Rectangle = "20,20,220,50",
            //});

            // Thêm comment vào dữ liệu
            ((PdfHashSigner)signer).AddSignatureComment(new PdfSignatureComment
            {
                Page = 1,
                Rectangle = "92,609,292,630",
                Text = "yahohohohohohohhodsánlfn",
                FontName = PdfHashSigner.FontName.Times_New_Roman,
                FontSize = 13,
                FontColor = "0000FF",
                FontStyle = 2,
                Type = (int)PdfSignatureComment.Types.TEXT,
            });

            ((PdfHashSigner)signer).AddSignatureComment(new PdfSignatureComment
            {
                Page = 5,
                Rectangle = "92,609,292,630",
                Text = "pag5",
                FontName = PdfHashSigner.FontName.Times_New_Roman,
                FontSize = 13,
                FontColor = "0000FF",
                FontStyle = 2,
                Type = (int)PdfSignatureComment.Types.TEXT,
            });

            // Signature widget border type (OPTIONAL)
            ((PdfHashSigner)signer).SetSignatureBorderType(PdfHashSigner.VisibleSigBorder.DASHED);
            #endregion -----------------------------------------
            var hashValue = signer.GetSecondHashAsBase64();
            // ------------------------------------------------------------------------------------------

            // 2. Sign hashvalue using service api ------------------------------------------------------
            var externalSignature = _signHash(hashValue, "pdf", "application/pdf", groupId, certId, access_token);
            if (string.IsNullOrEmpty(externalSignature))
            {
                _log.Error("Sign error");
                return;
            }

            if (!signer.CheckHashSignature(externalSignature))
            {
                _log.Error("Signature not match");
                return;
            }
            // ------------------------------------------------------------------------------------------

            // 3. Package external signature to signed file
            byte[] signed = signer.Sign(externalSignature);

            File.WriteAllBytes(_pdfSignedPath, signed);
            _log.Info("SignHash PDF: Successfull. signed file at '" + _pdfSignedPath + "'");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="certId"></param>
        /// <param name="certBase64"></param>
        /// <param name="access_token"></param>
        public static void SignHashXml(string groupId, string certId, string certBase64, string access_token)
        {
            // Get hash value from unsigned data ---------------------------------------------------\
            byte[] unsignData = null;
            try
            {
                unsignData = File.ReadAllBytes(_xmlInput);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return;
            }
            IHashSigner signer = HashSignerFactory.GenerateSigner(unsignData, certBase64, HashSignerFactory.XML);
            // Node chứa chữ ký ex: //root/desigs/desig hoặc //namespace:invoice
            ((XmlHashSigner)signer).SetParentNodePath("//inv:invoice");
            // Id thẻ data 
            ((XmlHashSigner)signer).SetSignatureID("#data");
            // Namespace (required trong trường hợp xml sử dụng namespace)
            ((XmlHashSigner)signer).SetNameSpace("inv", "http://laphoadon.gdt.gov.vn/2014/09/invoicexml/v1");
            // Định nghĩa ID thẻ Signature (options)
            ((XmlHashSigner)signer).SetSignatureID("seller");

            string hashValue = null;
            try
            {
                hashValue = signer.GetSecondHashAsBase64();
            }
            catch(Exception ex)
            {
                _log.Error(ex);
                return;
            }

            // Sign hashvalue using service api
            var externalSignature = _signHash(hashValue, "xml", "text/xml", groupId, certId, access_token);
            if (string.IsNullOrEmpty(externalSignature))
            {
                _log.Error("Sign error");
                return;
            }

            if (!signer.CheckHashSignature(externalSignature))
            {
                _log.Error("Signature not match");
                return;
            }
            byte[] signed = signer.Sign(externalSignature);
            File.WriteAllBytes(_xmlSignedPath, signed);
            _log.Info("SignHash XML: Successfull. signed file at '" + _xmlSignedPath + "'");
        }


        /// <summary>
        /// 
        /// </summary>
        public static void SignHashOffice(string groupId, string certId, string certBase64, string access_token)
        {
            // Get hash value from unsigned data ---------------------------------------------------
            byte[] unsignData = null;
            try
            {
                unsignData = File.ReadAllBytes(_officeInput);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return;
            }
            IHashSigner signer = HashSignerFactory.GenerateSigner(unsignData, certBase64, HashSignerFactory.OFFICE);
            var hashValue = signer.GetSecondHashAsBase64();

            // Sign hashvalue using service api
            var externalSignature = _signHash(hashValue, "docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", groupId, certId, access_token);
            if (string.IsNullOrEmpty(externalSignature))
            {
                _log.Error("Sign error");
                return;
            }

            if (!signer.CheckHashSignature(externalSignature))
            {
                _log.Error("Signature not match");
                return;
            }
            byte[] signed = signer.Sign(externalSignature);

            File.WriteAllBytes(_officeSignedPath, signed);
            _log.Info("SignHash Office: Successfull. signed file at '" + _officeSignedPath + "'");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hashValue"></param>
        /// <param name="type"></param>
        /// <param name="contentType"></param>
        /// <param name="certId"></param>
        /// <param name="groupId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static string _signHash(string hashValue, string type, string contentType, string groupId, string certId, string token)
        {
            var response = CoreServiceClient.Query(new RequestMessage
            {
                RequestID = Guid.NewGuid().ToString(),
                ServiceID = "SignServer",
                FunctionName = "SignHash",
                Parameter = new SignParameter
                {
                    ServiceGroupID = groupId,
                    CertID = certId,
                    Type = type,
                    ContentType = contentType,
                    DataBase64 = hashValue
                }
            }, token);
            if(response == null)
            {
                _log.Error("Service not response");
                return null;
            }
            if (response.ResponseCode == 1)
            {
                var str = JsonConvert.SerializeObject(response.Content);
                SignResponse acc = JsonConvert.DeserializeObject<SignResponse>(str);
                if (acc != null)
                {
                    return acc.SignedData;
                }
            }
            else
            {
                _log.Error(response.ResponseContent);
            }

            return null;
        }
    }
}
