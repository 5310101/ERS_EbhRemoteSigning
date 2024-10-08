using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using VnptHashSignatures.Common;
using VnptHashSignatures.Interface;
using ERS_Domain.clsUtilities;
using iTextSharp.text;

namespace ERS_Domain.CustomSigner.CustomSignerOffice
{
    public class OfficeCustomHashSigner : BaseHashSigner, IHashSigner
    {
        private const string RtOfficeDocument = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

        private const string OfficeObjectId = "idOfficeObject";

        private const string SignatureId = "idPackageSignature";

        private const string ManifestHashAlgorithm = "http://www.w3.org/2000/09/xmldsig#sha1";

        private const string DigestMethod_SHA256 = "http://www.w3.org/2001/04/xmlenc#sha256";

        private MemoryStream _stream = new MemoryStream();

        private PackageDigitalSignatureManager _packageDigitalSignatureManager;
        public PackageDigitalSignatureManager PackageDigitalSignatureManager
        {
            get
            {
                if (_packageDigitalSignatureManager != null)
                {
                    return _packageDigitalSignatureManager;
                }
                return null;
            }
        }

        private Package _package;

        private string _tempFile;

        private Org.BouncyCastle.X509.X509Certificate _signer;

        private string _signatureId;

        public MessageDigestAlgorithm DigestAlgrothim { get; set; } = MessageDigestAlgorithm.SHA1;


        public OfficeCustomHashSigner()
        {
        }

        public void SetSignatureId(string value)
        {
            _signatureId = value;
        }

        public void SetSignerCertificate(string certBase64)
        {
            //IL_0048: Unknown result type (might be due to invalid IL or missing references)
            //IL_004e: Expected O, but got Unknown
            if (string.IsNullOrEmpty(certBase64))
            {
                return;
            }

            if (certBase64.StartsWith("-----BEGIN CERTIFICATE-----"))
            {
                certBase64 = certBase64.Replace("-----BEGIN CERTIFICATE-----", "").Replace("-----END CERTIFICATE-----", "");
            }

            _signerCert = certBase64;
            try
            {
                X509CertificateParser val = new X509CertificateParser();
                _signer = val.ReadCertificate(Convert.FromBase64String(certBase64));
            }
            catch (Exception)
            {
            }
        }

        public void SetUnsignData(string base64Data)
        {
            _unsignData = Convert.FromBase64String(base64Data);
        }

        public string SignBase64(string signedHashBase64)
        {
            byte[] array = Sign(signedHashBase64);
            if (array != null)
            {
                return Convert.ToBase64String(array);
            }

            Console.WriteLine("Error when package signed data");
            return null;
        }

        public OfficeCustomHashSigner(byte[] unsignData, string certBase64)
            : base(unsignData, certBase64)
        {
        }

        private void Init()
        {
            _tempFile = Path.GetTempPath() + "\\" + Guid.NewGuid().ToString() + ".tmp";
            //luu du lieu chua ky vao tempfile
            File.WriteAllBytes(_tempFile, _unsignData);
            //tao package , package co chua chu ky so duoi dang 1 phan tu xml va lien ket voi cac phan cua tai lieu (relationships)
            _package = Package.Open(_tempFile);
            X509Certificate2 certificate = new X509Certificate2(Convert.FromBase64String(_signerCert));
            //list chua cac uri quan he
            List<Uri> list = new List<Uri>();
            //list xac dinh cac tieu chi lua chon trong moi quan he
            List<PackageRelationshipSelector> list2 = new List<PackageRelationshipSelector>();
            //duyet qua cac lien ket cua file office tim cac quan he
            //liên kết tài liệu chính của Office(document.xml trong Word, workbook.xml trong Excel)
            foreach (PackageRelationship item in _package.GetRelationshipsByType("http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"))
            {
                //duyet qua tung lien ket trong tai lieu (package) va trong (part)
                AddSignableItems(item, list, list2);
            }

            if (DigestAlgrothim == MessageDigestAlgorithm.SHA256)
            {
                //tao lop quan ly tat ca cac chu ky so trong goi
                _packageDigitalSignatureManager = new PackageDigitalSignatureManager(_package)
                {
                    CertificateOption = CertificateEmbeddingOption.InSignaturePart,
                    HashAlgorithm = "http://www.w3.org/2001/04/xmlenc#sha256"
                };
            }
            else
            {
                _packageDigitalSignatureManager = new PackageDigitalSignatureManager(_package)
                {
                    CertificateOption = CertificateEmbeddingOption.InSignaturePart
                };
            }

            DataObjectCustom dataObjectCustom = CreateOfficeObject();
            ReferenceCustom referenceCustom = new ReferenceCustom("#idOfficeObject");
            _secondHash = _packageDigitalSignatureManager.HashDataFile(list, certificate, list2, "idPackageSignature", new DataObjectCustom[1] { dataObjectCustom }, new ReferenceCustom[1] { referenceCustom }, (int)DigestAlgrothim);
        }

        public string GetSecondHashAsBase64()
        {
            Init();
            if (_secondHash != null)
            {
                return Convert.ToBase64String(_secondHash);
            }

            return null;
        }

        public byte[] GetSecondHashBytes()
        {
            Init();
            return _secondHash;
        }

        public byte[] Sign(string signedHashBase64)
        {
            try
            {
                byte[] sig = Convert.FromBase64String(signedHashBase64);
                PackageDigitalSignature packageDigitalSignature = _packageDigitalSignatureManager.SignFile(sig);
                _package.Close();
                _package = null;
                _packageDigitalSignatureManager = null;
                byte[] result = BaseHashSigner.FileToByteArray(_tempFile);
                File.Delete(_tempFile);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public byte[] SignWithPSM(PackageDigitalSignatureManager psm , string signedHashBase64)
        {
            try
            {
                byte[] sig = Convert.FromBase64String(signedHashBase64);
                PackageDigitalSignature packageDigitalSignature = psm.SignFile(sig);
                _package.Close();
                _package = null;
                _packageDigitalSignatureManager = null;
                byte[] result = BaseHashSigner.FileToByteArray(_tempFile);
                File.Delete(_tempFile);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }


        public byte[] Sign(SignerProfile profile, string signedHashBase64)
        {
            byte[] array = Convert.FromBase64String(signedHashBase64);
            MemoryStream memoryStream = new MemoryStream();
            try
            {
                memoryStream.Write(profile.TempData, 0, profile.TempData.Length);
                Package package = Package.Open(memoryStream, FileMode.Open, FileAccess.ReadWrite);
                PackageDigitalSignatureManager packageDigitalSignatureManager = ((!(profile.HashAlgorithm == "2.16.840.1.101.3.4.2.1")) ? new PackageDigitalSignatureManager(package)
                {
                    CertificateOption = CertificateEmbeddingOption.InSignaturePart
                } : new PackageDigitalSignatureManager(package)
                {
                    CertificateOption = CertificateEmbeddingOption.InSignaturePart,
                    HashAlgorithm = "http://www.w3.org/2001/04/xmlenc#sha256"
                });

                string fieldName = profile.Fieldnames.First();
                //PackageDigitalSignature packageDigitalSignature = packageDigitalSignatureManager.Signatures.Where((PackageDigitalSignature s) => s.Signature?.Id == MethodLibrary.SafeString(fieldName)).First();
                PackageDigitalSignature packageDigitalSignature = packageDigitalSignatureManager.Signatures[0];
                //PackageDigitalSignature packageDigitalSignature = packageDigitalSignatureManager.Signatures.Where((PackageDigitalSignature s) => s.SignaturePart.Uri.ToString().Contains(fieldName)).First();
                var uri = packageDigitalSignature.SignaturePart.Uri;
                var signaturePart = package.GetPart(uri);
                using (Stream stream = signaturePart.GetStream(FileMode.Create, FileAccess.Write))
                {
                    stream.Write(array, 0, array.Length);
                }

                package.Flush();
                package.Close();
              
                return memoryStream.ToArray();

            }
            catch (Exception ex)
            {
                Utilities.logger.ErrorLog(ex, "Sign");
                return null;
            }
            finally
            {
                memoryStream.Close();
            }
        }

        public bool CheckHashSignature(string signedHashBase64)
        {
            try
            {
                byte[] rgbSignature = Convert.FromBase64String(signedHashBase64);
                X509Certificate2 x509Certificate = new X509Certificate2(Convert.FromBase64String(_signerCert));
                RSACryptoServiceProvider rSACryptoServiceProvider = (RSACryptoServiceProvider)x509Certificate.PublicKey.Key;
                if (DigestAlgrothim == MessageDigestAlgorithm.SHA1)
                {
                    return rSACryptoServiceProvider.VerifyHash(_secondHash, CryptoConfig.MapNameToOID("SHA1"), rgbSignature);
                }

                if (DigestAlgrothim == MessageDigestAlgorithm.SHA256)
                {
                    return rSACryptoServiceProvider.VerifyHash(_secondHash, CryptoConfig.MapNameToOID("SHA256"), rgbSignature);
                }

                return rSACryptoServiceProvider.VerifyHash(_secondHash, CryptoConfig.MapNameToOID(HASH_ALGORITHM), rgbSignature);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void AddSignableItems(PackageRelationship packageRelationship, List<Uri> lstUris, List<PackageRelationshipSelector> lstPackageRelationshipSelectors)
        {
            PackageRelationshipSelector item = new PackageRelationshipSelector(packageRelationship.SourceUri, PackageRelationshipSelectorType.Id, packageRelationship.Id);
            lstPackageRelationshipSelectors.Add(item);
            if (packageRelationship.TargetMode != 0)
            {
                return;
            }

            PackagePart part = packageRelationship.Package.GetPart(PackUriHelper.ResolvePartUri(packageRelationship.SourceUri, packageRelationship.TargetUri));
            if (lstUris.Contains(part.Uri))
            {
                return;
            }

            lstUris.Add(part.Uri);
            foreach (PackageRelationship relationship in part.GetRelationships())
            {
                AddSignableItems(relationship, lstUris, lstPackageRelationshipSelectors);
            }
        }

        private DataObjectCustom CreateOfficeObject()
        {
            XmlDocument xmlDocument = new XmlDocument();
            XmlDocument xmlDocument2 = new XmlDocument();
            string xml = "<SignatureProperties xmlns=\"http://www.w3.org/2000/09/xmldsig#\">\r\n<SignatureProperty Id=\"idOfficeV1Details\" Target=\"idPackageSignature\">\r\n<SignatureInfoV1 xmlns=\"http://schemas.microsoft.com/office/2006/digsig\">\r\n<ManifestHashAlgorithm>\r\nhttp://www.w3.org/2000/09/xmldsig#sha1\r\n</ManifestHashAlgorithm>\r\n</SignatureInfoV1>\r\n</SignatureProperty>\r\n</SignatureProperties>";
            if (DigestAlgrothim == MessageDigestAlgorithm.SHA256)
            {
                xml = "<SignatureProperties xmlns=\"http://www.w3.org/2000/09/xmldsig#\">\r\n<SignatureProperty Id=\"idOfficeV1Details\" Target=\"idPackageSignature\">\r\n<SignatureInfoV1 xmlns=\"http://schemas.microsoft.com/office/2006/digsig\">\r\n<ManifestHashAlgorithm>\r\nhttp://www.w3.org/2001/04/xmlenc#sha256\r\n</ManifestHashAlgorithm>\r\n</SignatureInfoV1>\r\n</SignatureProperty>\r\n</SignatureProperties>";
            }

            xmlDocument2.LoadXml(xml);
            if (DigestAlgrothim == MessageDigestAlgorithm.SHA256)
            {
                xmlDocument.LoadXml(string.Format(xmlDocument2.OuterXml, "idPackageSignature", "http://www.w3.org/2001/04/xmlenc#sha256"));
            }
            else
            {
                xmlDocument.LoadXml(string.Format(xmlDocument2.OuterXml, "idPackageSignature", "http://www.w3.org/2000/09/xmldsig#sha1"));
            }

            DataObjectCustom dataObjectCustom = new DataObjectCustom();
            dataObjectCustom.LoadXml(xmlDocument.DocumentElement);
            dataObjectCustom.Id = "idOfficeObject";
            return dataObjectCustom;
        }

        public void SetHashAlgorithm(MessageDigestAlgorithm alg)
        {
            DigestAlgrothim = alg;
        }

        public bool SetSignerCertchain(string pkcs7Base64)
        {
            return false;
        }

        public string GetSignerSubjectDN()
        {
            try
            {
                return ((object)_signer.SubjectDN).ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public SignerProfile GetSignerProfile()
        {
            try
            {
                Init();
            }
            catch (Exception)
            {
                throw;
            }

            //List<byte[]> certs = new List<byte[]>
            //{
            //    Convert.FromBase64String(_signerCert)
            //};
            //thay doi 1 so thu de restore signer
            X509Certificate2 certificate = new X509Certificate2(Convert.FromBase64String(_signerCert));
            
            return new SignerProfile
            {
                TempData = GetTempData(),
                SecondHashBytes = _secondHash,
                HashAlgorithm = CryptoConfig.MapNameToOID(DigestAlgrothim.ToString()),
                DocType = "OFFICE",
                Fieldnames = new List<string> { certificate.SerialNumber }
            };
        }

        private byte[] GetTempData()
        {
            try
            {
                PackageDigitalSignature tempData = _packageDigitalSignatureManager.GetTempData();
                _package.Close();
                _package = null;
                _packageDigitalSignatureManager = null;
                byte[] result = BaseHashSigner.FileToByteArray(_tempFile);
                File.Delete(_tempFile);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool CheckHashSignature(SignerProfile profile, string signedHashBase64)
        {
            return true;
        }
    }
}
