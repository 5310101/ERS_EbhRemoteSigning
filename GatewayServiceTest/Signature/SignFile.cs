using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace GatewayServiceTest.Signature
{
    /// <summary>
    /// Create signature on popular document format
    /// </summary>
    public class SignFile
    {
        // Logger for this class
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(SignFile));

        // Sample pdf input/output file path
        private const string _pdfInput = @"F:/WORK/2018/07-2018/input.pdf";
        private const string _pdfSignedPath = @"F:/WORK/2018/07-2018/signed.pdf";

        // Sample office input/output file path
        private const string _officeInput = @"F:/WORK/2018/07-2018/input.docx";
        private const string _officeSignedPath = @"F:/WORK/2018/07-2018/signed.docx";

        // Sample xml input/output file path
        private const string _xmlInput = @"D:\OFFICE\Documents\2019\2019-07\input.xml";
        private const string _xmlSignedPath = @"D:\OFFICE\Documents\2019\2019-07\signed.xml";

        public static void SignXml()
        {
            try
            {
                // Create a new CspParameters object to specify
                // a key container.
                CspParameters cspParams = new CspParameters();
                cspParams.KeyContainerName = "XML_DSIG_RSA_KEY";

                X509Certificate2 cert = new X509Certificate2(@"D:\OFFICE\Documents\2019\2019-07\CThDT_2019_1.pfx", "1", X509KeyStorageFlags.Exportable);
                RSACryptoServiceProvider rsaKey = (RSACryptoServiceProvider)cert.PrivateKey;

                // Create a new XML document.
                XmlDocument xmlDoc = new XmlDocument();

                // Load an XML file into the XmlDocument object.
                xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(_xmlInput);

                X509Certificate c = new X509Certificate(cert.GetRawCertData());
                // Sign the XML document. 
                SignXml(xmlDoc, rsaKey, c);

                Console.WriteLine("XML file signed.");

                // Save the document.
                xmlDoc.Save(_xmlSignedPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static void SignXml(XmlDocument xmlDoc, RSA rsaKey, X509Certificate cert)
        {
            try
            {
                // Check arguments.
                if (xmlDoc == null)
                    throw new ArgumentException(nameof(xmlDoc));
                if (rsaKey == null)
                    throw new ArgumentException(nameof(rsaKey));

                // Create a SignedXml object.
                SignedXmlCustom signedXml = new SignedXmlCustom(xmlDoc);
                // Add the key to the SignedXml document.
                signedXml.SigningKey = rsaKey;
                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause(new KeyInfoX509Data(cert));
                signedXml.KeyInfo = keyInfo;

                // Create a reference to be signed.
                ReferenceCustom reference = new ReferenceCustom();
                reference.Uri = "#data";

                // Add an enveloped transformation to the reference.
                XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
                reference.AddTransform(env);

                // Add the reference to the SignedXml object.
                signedXml.AddReference(reference);

                // Compute the signature.
                signedXml.ComputeSignature();
                var check = signedXml.CheckSignature();
                // Get the XML representation of the signature and save
                // it to an XmlElement object.
                XmlElement xmlDigitalSignature = signedXml.GetXml();

                // Append the element to the XML document.
                xmlDoc.DocumentElement.AppendChild(xmlDoc.ImportNode(xmlDigitalSignature, true));
            }
            catch(Exception ex)
            {

            }
        }

        /// <summary>
        /// Create pdf signature
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="certId">Certificate ID using to create signature</param>
        /// <param name="access_token">access_token to authorize request</param>
        public static void SignPdf(string certId, string access_token)
        {
            // Get hash value from unsigned data ---------------------------------------------------
            byte[] unsignData = null;
            try
            {
                unsignData = File.ReadAllBytes(_pdfInput);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return;
            }

            // Sign data using service api
            var fileExtension = "pdf";
            var fileContentType = "application/pdf";
            var signedBase64 = _sign(Convert.ToBase64String(unsignData), fileExtension, fileContentType, certId, access_token);
            if (string.IsNullOrEmpty(signedBase64))
            {
                _log.Error("Sign error");
                return;
            }

            File.WriteAllBytes(_pdfSignedPath, Convert.FromBase64String(signedBase64));
            _log.Info("Sign pdf successful. Signed file stored at: " + _pdfSignedPath);
        }

        /// <summary>
        /// Create xml signature
        /// </summary>
        /// <param name="certId">Certificate ID using to create signature</param>
        /// <param name="access_token">access_token to authorize request</param>
        public static void SignXml(string certId, string access_token)
        {
            // Get hash value from unsigned data ---------------------------------------------------
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
            _log.Info($"File size={unsignData.Length}");

            // Sign data using service api
            var fileExtension = "xml";
            var fileContentType = "text/xml";
            var signedBase64 = _sign(Convert.ToBase64String(unsignData), fileExtension, fileContentType, certId, access_token);
            if (string.IsNullOrEmpty(signedBase64))
            {
                _log.Error("Sign error");
                return;
            }

            //File.WriteAllBytes(_xmlSignedPath, Convert.FromBase64String(signedBase64));
        }

        /// <summary>
        /// Create office signature. Office xml document format support only (docx, pptx, xlsx,...)
        /// </summary>
        /// <param name="certId">Certificate ID using to create signature</param>
        /// <param name="access_token">access_token to authorize request</param>
        public static void SignOffice(string certId, string access_token)
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
            _log.Info($"File size={unsignData.Length}");

            // Sign data using service api
            var fileExtension = "docx";
            var fileContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            var signedBase64 = _sign(Convert.ToBase64String(unsignData), fileExtension, fileContentType, certId, access_token);
            if (string.IsNullOrEmpty(signedBase64))
            {
                _log.Error("Sign error");
                return;
            }

            //File.WriteAllBytes(_officeSignedPath, Convert.FromBase64String(signedBase64));
        }

        /// <summary>
        /// Handle sign request to gateway api
        /// </summary>
        /// <param name="_unsignBase64">Data to sign in base64 format</param>
        /// <param name="fileExtension">File extension</param>
        /// <param name="contentType">Document content type</param>
        /// <param name="certId">ID certificate using to create signature</param>
        /// <param name="access_token">access_token to authorize request on gateway api</param>
        /// <returns></returns>
        private static string _sign(string _unsignBase64, string fileExtension, string contentType, 
            string certId, string access_token)
        {
            var response = CoreServiceClient.Query(new RequestMessage
            {
                RequestID = Guid.NewGuid().ToString(),
                ServiceID = "SignServer",
                FunctionName = "Sign",
                Parameter = new SignParameter
                {
                    CertID = certId,
                    Type = fileExtension,
                    ContentType = contentType,
                    DataBase64 = _unsignBase64
                }
            }, access_token);
            if (response != null)
            {
                var str = JsonConvert.SerializeObject(response.Content);
                SignResponse acc = JsonConvert.DeserializeObject<SignResponse>(str);
                if (acc != null)
                {
                    return acc.SignedData;
                }
            }

            return null;
        }

    }
}
