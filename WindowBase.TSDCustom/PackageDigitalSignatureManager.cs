using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Mime;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Security.Permissions;
using System.Windows;
using System.Xml;
using MS.Internal;
using MS.Internal.IO.Packaging;

namespace System.IO.Packaging
{
    public sealed class PackageDigitalSignatureManager
    {
        private delegate bool RelationshipOperation(PackageRelationship r, object context);

        private class StringMatchPredicate
        {
            private string _id;

            public StringMatchPredicate(string id)
            {
                _id = id;
            }

            public bool Match(string id)
            {
                return string.CompareOrdinal(_id, id) == 0;
            }
        }

        private CertificateEmbeddingOption _certificateEmbeddingOption;

        private Package _container;

        private static readonly string _defaultHashAlgorithm = "http://www.w3.org/2000/09/xmldsig#sha1";

        private static Uri _defaultOriginPartName = PackUriHelper.CreatePartUri(new Uri("/package/services/digital-signature/origin.psdsor", UriKind.Relative));

        private static readonly string _defaultSignaturePartNameExtension = ".psdsxs";

        private static readonly string _defaultSignaturePartNamePrefix = "/package/services/digital-signature/xml-signature/";

        private static readonly string _guidStorageFormatString = "N";

        private string _hashAlgorithmString = _defaultHashAlgorithm;

        private PackagePart _originPart;

        private static readonly ContentType _originPartContentType = new ContentType("application/vnd.openxmlformats-package.digital-signature-origin");

        private bool _originPartExists;

        private Uri _originPartName = _defaultOriginPartName;

        private static readonly string _originRelationshipType = "http://schemas.openxmlformats.org/package/2006/relationships/digital-signature/origin";

        private bool _originSearchConducted;

        private static readonly string _originToSignatureRelationshipType = "http://schemas.openxmlformats.org/package/2006/relationships/digital-signature/signature";

        private IntPtr _parentWindow;

        private ReadOnlyCollection<PackageDigitalSignature> _signatureList;

        private List<PackageDigitalSignature> _signatures;

        private string _signatureTimeFormat = XmlSignatureProperties.DefaultDateTimeFormat;

        private Dictionary<string, string> _transformDictionary;

        private XmlDigitalSignatureProcessor processor = null;

        public CertificateEmbeddingOption CertificateOption
        {
            get
            {
                return _certificateEmbeddingOption;
            }
            set
            {
                if (value < CertificateEmbeddingOption.InCertificatePart || value > CertificateEmbeddingOption.NotEmbedded)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _certificateEmbeddingOption = value;
            }
        }

        public static string DefaultHashAlgorithm => _defaultHashAlgorithm;

        public string HashAlgorithm
        {
            get
            {
                return _hashAlgorithmString;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (value == string.Empty)
                {
                    throw new ArgumentException(SR.Get("UnsupportedHashAlgorithm"), "value");
                }

                _hashAlgorithmString = value;
            }
        }

        public bool IsSigned
        {
            get
            {
                EnsureSignatures();
                return _signatures.Count > 0;
            }
        }

        private PackagePart OriginPart
        {
            get
            {
                if (_originPart == null && !OriginPartExists())
                {
                    _originPart = _container.CreatePart(_originPartName, _originPartContentType.ToString());
                    _container.CreateRelationship(_originPartName, TargetMode.Internal, _originRelationshipType);
                }

                return _originPart;
            }
        }

        internal Package Package => _container;

        public IntPtr ParentWindow
        {
            get
            {
                return _parentWindow;
            }
            set
            {
                _parentWindow = value;
            }
        }

        private bool ReadOnly => _container.FileOpenAccess == FileAccess.Read;

        public Uri SignatureOrigin
        {
            get
            {
                OriginPartExists();
                return _originPartName;
            }
        }

        public static string SignatureOriginRelationshipType => _originRelationshipType;

        public ReadOnlyCollection<PackageDigitalSignature> Signatures
        {
            get
            {
                EnsureSignatures();
                if (_signatureList == null)
                {
                    _signatureList = new ReadOnlyCollection<PackageDigitalSignature>(_signatures);
                }

                return _signatureList;
            }
        }

        public string TimeFormat
        {
            get
            {
                return _signatureTimeFormat;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (!XmlSignatureProperties.LegalFormat(value))
                {
                    throw new FormatException(SR.Get("BadSignatureTimeFormatString"));
                }

                _signatureTimeFormat = value;
            }
        }

        public Dictionary<string, string> TransformMapping => _transformDictionary;

        public event InvalidSignatureEventHandler InvalidSignatureEvent;

        public PackageDigitalSignatureManager(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            _parentWindow = IntPtr.Zero;
            _container = package;
            _transformDictionary = new Dictionary<string, string>(4);
            _transformDictionary[PackagingUtilities.RelationshipPartContentType.ToString()] = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
            _transformDictionary[XmlDigitalSignatureProcessor.ContentType.ToString()] = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
        }

        private int CertificatePartReferenceCount(Uri certificatePartUri)
        {
            int num = 0;
            for (int i = 0; i < _signatures.Count; i++)
            {
                if (_signatures[i].GetCertificatePart() != null && PackUriHelper.ComparePartUri(certificatePartUri, _signatures[i].GetCertificatePart().Uri) == 0)
                {
                    num++;
                }
            }

            return num;
        }

        public PackageDigitalSignature Countersign()
        {
            if (!IsSigned)
            {
                throw new InvalidOperationException(SR.Get("NoCounterSignUnsignedContainer"));
            }

            X509Certificate x509Certificate = PromptForSigningCertificate(ParentWindow);
            if (x509Certificate == null)
            {
                return null;
            }

            return Countersign(x509Certificate);
        }

        public PackageDigitalSignature Countersign(X509Certificate certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException("certificate");
            }

            if (!IsSigned)
            {
                throw new InvalidOperationException(SR.Get("NoCounterSignUnsignedContainer"));
            }

            List<Uri> list = new List<Uri>(_signatures.Count);
            for (int i = 0; i < _signatures.Count; i++)
            {
                list.Add(_signatures[i].SignaturePart.Uri);
            }

            return Sign(list, certificate);
        }

        public PackageDigitalSignature Countersign(X509Certificate certificate, IEnumerable<Uri> signatures)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException("certificate");
            }

            if (signatures == null)
            {
                throw new ArgumentNullException("signatures");
            }

            if (!IsSigned)
            {
                throw new InvalidOperationException(SR.Get("NoCounterSignUnsignedContainer"));
            }

            foreach (Uri signature in signatures)
            {
                if (!_container.GetPart(signature).ValidatedContentType.AreTypeAndSubTypeEqual(XmlDigitalSignatureProcessor.ContentType))
                {
                    throw new ArgumentException(SR.Get("CanOnlyCounterSignSignatureParts", signatures));
                }
            }

            return Sign(signatures, certificate);
        }

        private bool DeleteCertificateIfReferenceCountBecomesZeroVisitor(PackageRelationship r, object context)
        {
            if (r.TargetMode != 0)
            {
                throw new FileFormatException(SR.Get("PackageSignatureCorruption"));
            }

            Uri uri = PackUriHelper.ResolvePartUri(r.SourceUri, r.TargetUri);
            if (CertificatePartReferenceCount(uri) == 1)
            {
                _container.DeletePart(uri);
            }

            return true;
        }

        private void DeleteOriginPart()
        {
            try
            {
                SafeVisitRelationships(_container.GetRelationshipsByType(_originRelationshipType), DeleteRelationshipOfTypePackageToOriginVisitor);
                _container.DeletePart(_originPartName);
            }
            finally
            {
                _originPartExists = false;
                _originSearchConducted = true;
                _originPart = null;
            }
        }

        private bool DeleteRelationshipOfTypePackageToOriginVisitor(PackageRelationship r, object context)
        {
            if (r.TargetMode != 0)
            {
                throw new FileFormatException(SR.Get("PackageSignatureCorruption"));
            }

            if (PackUriHelper.ComparePartUri(PackUriHelper.ResolvePartUri(r.SourceUri, r.TargetUri), _originPartName) == 0)
            {
                _container.DeleteRelationship(r.Id);
            }

            return true;
        }

        private bool DeleteRelationshipToSignature(PackageRelationship r, object signatureUri)
        {
            Uri secondPartUri = signatureUri as Uri;
            if (r.TargetMode != 0)
            {
                throw new FileFormatException(SR.Get("PackageSignatureCorruption"));
            }

            if (PackUriHelper.ComparePartUri(PackUriHelper.ResolvePartUri(r.SourceUri, r.TargetUri), secondPartUri) == 0)
            {
                OriginPart.DeleteRelationship(r.Id);
            }

            return true;
        }

        private void EnsureSignatures()
        {
            if (_signatures != null)
            {
                return;
            }

            _signatures = new List<PackageDigitalSignature>();
            if (!OriginPartExists())
            {
                return;
            }

            foreach (PackageRelationship item2 in _originPart.GetRelationshipsByType(_originToSignatureRelationshipType))
            {
                if (item2.TargetMode != 0)
                {
                    throw new FileFormatException(SR.Get("PackageSignatureCorruption"));
                }

                Uri partUri = PackUriHelper.ResolvePartUri(_originPart.Uri, item2.TargetUri);
                if (!_container.PartExists(partUri))
                {
                    throw new FileFormatException(SR.Get("PackageSignatureCorruption"));
                }

                PackagePart part = _container.GetPart(partUri);
                if (part.ValidatedContentType.AreTypeAndSubTypeEqual(XmlDigitalSignatureProcessor.ContentType))
                {
                    PackageDigitalSignature item = new PackageDigitalSignature(this, part);
                    _signatures.Add(item);
                }
            }
        }

        private bool EnumeratorEmptyCheck(IEnumerable enumerable)
        {
            if (enumerable != null)
            {
                if (enumerable is ICollection collection)
                {
                    return collection.Count == 0;
                }

                IEnumerator enumerator = enumerable.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    object current = enumerator.Current;
                    return false;
                }
            }

            return true;
        }

        private Uri GenerateSignaturePartName()
        {
            return PackUriHelper.CreatePartUri(new Uri(_defaultSignaturePartNamePrefix + Guid.NewGuid().ToString(_guidStorageFormatString, null) + _defaultSignaturePartNameExtension, UriKind.Relative));
        }

        public PackageDigitalSignature GetSignature(Uri signatureUri)
        {
            if (signatureUri == null)
            {
                throw new ArgumentNullException("signatureUri");
            }

            int signatureIndex = GetSignatureIndex(signatureUri);
            if (signatureIndex < 0)
            {
                return null;
            }

            return _signatures[signatureIndex];
        }

        private int GetSignatureIndex(Uri uri)
        {
            EnsureSignatures();
            for (int i = 0; i < _signatures.Count; i++)
            {
                if (PackUriHelper.ComparePartUri(uri, _signatures[i].SignaturePart.Uri) == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private void InternalRemoveSignature(Uri signatureUri, int countOfSignaturesRemaining)
        {
            if (countOfSignaturesRemaining == 0)
            {
                DeleteOriginPart();
            }
            else
            {
                SafeVisitRelationships(OriginPart.GetRelationshipsByType(_originToSignatureRelationshipType), DeleteRelationshipToSignature, signatureUri);
            }

            SafeVisitRelationships(_container.GetPart(signatureUri).GetRelationshipsByType(CertificatePart.RelationshipType), DeleteCertificateIfReferenceCountBecomesZeroVisitor);
            _container.DeletePart(signatureUri);
        }

        private bool OriginPartExists()
        {
            if (!_originSearchConducted)
            {
                try
                {
                    foreach (PackageRelationship item in _container.GetRelationshipsByType(_originRelationshipType))
                    {
                        if (item.TargetMode != 0)
                        {
                            throw new FileFormatException(SR.Get("PackageSignatureCorruption"));
                        }

                        Uri uri = PackUriHelper.ResolvePartUri(item.SourceUri, item.TargetUri);
                        if (!_container.PartExists(uri))
                        {
                            throw new FileFormatException(SR.Get("SignatureOriginNotFound"));
                        }

                        PackagePart part = _container.GetPart(uri);
                        if (part.ValidatedContentType.AreTypeAndSubTypeEqual(_originPartContentType))
                        {
                            if (_originPartExists)
                            {
                                throw new FileFormatException(SR.Get("MultipleSignatureOrigins"));
                            }

                            _originPartName = uri;
                            _originPart = part;
                            _originPartExists = true;
                        }
                    }
                }
                finally
                {
                    _originSearchConducted = true;
                }
            }

            return _originPartExists;
        }

        [SecurityTreatAsSafe]
        [SecurityCritical]
        internal static X509Certificate PromptForSigningCertificate(IntPtr hwndParent)
        {
            X509Certificate2 result = null;
            X509Store x509Store = new X509Store(StoreLocation.CurrentUser);
            x509Store.Open(OpenFlags.OpenExistingOnly);
            X509Certificate2Collection x509Certificate2Collection = x509Store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, validOnly: true).Find(X509FindType.FindByKeyUsage, X509KeyUsageFlags.DigitalSignature, validOnly: false);
            for (int num = x509Certificate2Collection.Count - 1; num >= 0; num--)
            {
                if (!x509Certificate2Collection[num].HasPrivateKey)
                {
                    x509Certificate2Collection.RemoveAt(num);
                }
            }

            if (x509Certificate2Collection.Count > 0)
            {
                x509Certificate2Collection = X509Certificate2UI.SelectFromCollection(x509Certificate2Collection, SR.Get("CertSelectionDialogTitle"), SR.Get("CertSelectionDialogMessage"), X509SelectionFlag.SingleSelection, hwndParent);
                if (x509Certificate2Collection.Count > 0)
                {
                    result = x509Certificate2Collection[0];
                }
            }

            return result;
        }

        public void RemoveAllSignatures()
        {
            if (ReadOnly)
            {
                throw new InvalidOperationException(SR.Get("CannotRemoveSignatureFromReadOnlyFile"));
            }

            EnsureSignatures();
            try
            {
                for (int i = 0; i < _signatures.Count; i++)
                {
                    PackagePart signaturePart = _signatures[i].SignaturePart;
                    foreach (PackageRelationship item in signaturePart.GetRelationshipsByType(CertificatePart.RelationshipType))
                    {
                        if (item.TargetMode == TargetMode.Internal)
                        {
                            _container.DeletePart(PackUriHelper.ResolvePartUri(item.SourceUri, item.TargetUri));
                        }
                    }

                    _container.DeletePart(signaturePart.Uri);
                    _signatures[i].Invalidate();
                }

                DeleteOriginPart();
            }
            finally
            {
                _signatures.Clear();
            }
        }

        public void RemoveSignature(Uri signatureUri)
        {
            if (ReadOnly)
            {
                throw new InvalidOperationException(SR.Get("CannotRemoveSignatureFromReadOnlyFile"));
            }

            if (signatureUri == null)
            {
                throw new ArgumentNullException("signatureUri");
            }

            if (!IsSigned)
            {
                return;
            }

            int signatureIndex = GetSignatureIndex(signatureUri);
            if (signatureIndex >= 0)
            {
                try
                {
                    InternalRemoveSignature(signatureUri, _signatures.Count - 1);
                    _signatures[signatureIndex].Invalidate();
                }
                finally
                {
                    _signatures.RemoveAt(signatureIndex);
                }
            }
        }

        private void SafeVisitRelationships(PackageRelationshipCollection relationships, RelationshipOperation visit)
        {
            SafeVisitRelationships(relationships, visit, null);
        }

        private void SafeVisitRelationships(PackageRelationshipCollection relationships, RelationshipOperation visit, object context)
        {
            List<PackageRelationship> list = new List<PackageRelationship>(relationships);
            for (int i = 0; i < list.Count && visit(list[i], context); i++)
            {
            }
        }

        public PackageDigitalSignature Sign(IEnumerable<Uri> parts)
        {
            X509Certificate x509Certificate = PromptForSigningCertificate(ParentWindow);
            if (x509Certificate == null)
            {
                return null;
            }

            return Sign(parts, x509Certificate);
        }

        public PackageDigitalSignature Sign(IEnumerable<Uri> parts, X509Certificate certificate)
        {
            return Sign(parts, certificate, null);
        }

        public PackageDigitalSignature Sign(IEnumerable<Uri> parts, X509Certificate certificate, IEnumerable<PackageRelationshipSelector> relationshipSelectors)
        {
            return Sign(parts, certificate, relationshipSelectors, XTable.Get(XTable.ID.OpcSignatureAttrValue));
        }

        public PackageDigitalSignature Sign(IEnumerable<Uri> parts, X509Certificate certificate, IEnumerable<PackageRelationshipSelector> relationshipSelectors, string signatureId)
        {
            if (parts == null && relationshipSelectors == null)
            {
                throw new ArgumentException(SR.Get("NothingToSign"));
            }

            return Sign(parts, certificate, relationshipSelectors, signatureId, null, null);
        }

        [SecurityCritical]
        public PackageDigitalSignature Sign(IEnumerable<Uri> parts, X509Certificate certificate, IEnumerable<PackageRelationshipSelector> relationshipSelectors, string signatureId, IEnumerable<DataObjectCustom> signatureObjects, IEnumerable<ReferenceCustom> objectReferences)
        {
            if (ReadOnly)
            {
                throw new InvalidOperationException(SR.Get("CannotSignReadOnlyFile"));
            }

            VerifySignArguments(parts, certificate, relationshipSelectors, signatureId, signatureObjects, objectReferences);
            if (signatureId == null || signatureId == string.Empty)
            {
                signatureId = "packageSignature";
            }

            EnsureSignatures();
            Uri uri = GenerateSignaturePartName();
            if (_container.PartExists(uri))
            {
                throw new ArgumentException(SR.Get("DuplicateSignature"));
            }

            OriginPart.CreateRelationship(uri, TargetMode.Internal, _originToSignatureRelationshipType);
            _container.Flush();
            VerifyPartsExist(parts);
            bool embedCertificate = _certificateEmbeddingOption == CertificateEmbeddingOption.InSignaturePart;
            X509Certificate2 x509Certificate = certificate as X509Certificate2;
            if (x509Certificate == null)
            {
                x509Certificate = new X509Certificate2(certificate.Handle);
            }

            PackageDigitalSignature packageDigitalSignature = null;
            PackagePart packagePart = null;
            try
            {
                packagePart = _container.CreatePart(uri, XmlDigitalSignatureProcessor.ContentType.ToString());
                packageDigitalSignature = XmlDigitalSignatureProcessor.Sign(this, packagePart, parts, relationshipSelectors, x509Certificate, signatureId, embedCertificate, signatureObjects, objectReferences);
            }
            catch (InvalidOperationException)
            {
                InternalRemoveSignature(uri, _signatures.Count);
                _container.Flush();
                throw;
            }
            catch (IOException)
            {
                InternalRemoveSignature(uri, _signatures.Count);
                _container.Flush();
                throw;
            }
            catch (CryptographicException)
            {
                InternalRemoveSignature(uri, _signatures.Count);
                _container.Flush();
                throw;
            }

            _signatures.Add(packageDigitalSignature);
            if (_certificateEmbeddingOption == CertificateEmbeddingOption.InCertificatePart)
            {
                Uri uri2 = PackUriHelper.CreatePartUri(new Uri(CertificatePart.PartNamePrefix + x509Certificate.SerialNumber + CertificatePart.PartNameExtension, UriKind.Relative));
                CertificatePart certificatePart = new CertificatePart(_container, uri2);
                certificatePart.SetCertificate(x509Certificate);
                packagePart.CreateRelationship(uri2, TargetMode.Internal, CertificatePart.RelationshipType);
                packageDigitalSignature.SetCertificatePart(certificatePart);
            }

            _container.Flush();
            return packageDigitalSignature;
        }

        public byte[] HashDataFile(IEnumerable<Uri> parts, X509Certificate certificate, IEnumerable<PackageRelationshipSelector> relationshipSelectors, string signatureId, IEnumerable<DataObjectCustom> signatureObjects, IEnumerable<ReferenceCustom> objectReferences, int DigestAlgrothim)
        {
            if (ReadOnly)
            {
                throw new InvalidOperationException(SR.Get("CannotSignReadOnlyFile"));
            }

            VerifySignArguments(parts, certificate, relationshipSelectors, signatureId, signatureObjects, objectReferences);
            if (signatureId == null || signatureId == string.Empty)
            {
                signatureId = "packageSignature";
            }

            EnsureSignatures();
            Uri uri = GenerateSignaturePartName();
            if (_container.PartExists(uri))
            {
                throw new ArgumentException(SR.Get("DuplicateSignature"));
            }

            OriginPart.CreateRelationship(uri, TargetMode.Internal, _originToSignatureRelationshipType);
            _container.Flush();
            VerifyPartsExist(parts);
            bool embedCertificate = _certificateEmbeddingOption == CertificateEmbeddingOption.InSignaturePart;
            X509Certificate2 x509Certificate = certificate as X509Certificate2;
            if (x509Certificate == null)
            {
                x509Certificate = new X509Certificate2(certificate.Handle);
            }

            PackagePart packagePart = null;
            byte[] array = null;
            try
            {
                packagePart = _container.CreatePart(uri, XmlDigitalSignatureProcessor.ContentType.ToString());
                processor = new XmlDigitalSignatureProcessor(this, packagePart);
                processor.SetCertificate(x509Certificate);
                return processor.HashData(parts, relationshipSelectors, signatureId, embedCertificate, signatureObjects, objectReferences, DigestAlgrothim);
            }
            catch (InvalidOperationException)
            {
                InternalRemoveSignature(uri, _signatures.Count);
                _container.Flush();
                throw;
            }
            catch (IOException)
            {
                InternalRemoveSignature(uri, _signatures.Count);
                _container.Flush();
                throw;
            }
            catch (CryptographicException)
            {
                InternalRemoveSignature(uri, _signatures.Count);
                _container.Flush();
                throw;
            }
        }

        public PackageDigitalSignature GetTempData()
        {
            return processor.GetTempData();
        }

        public PackageDigitalSignature SignFile(byte[] sig)
        {
            return processor.PushSignature(sig);
        }

        [SecurityCritical]
        [SecurityPermission(SecurityAction.LinkDemand, Unrestricted = true)]
        public static X509ChainStatusFlags VerifyCertificate(X509Certificate certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException("certificate");
            }

            X509ChainStatusFlags x509ChainStatusFlags = X509ChainStatusFlags.NoError;
            X509Chain x509Chain = new X509Chain();
            if (!x509Chain.Build(new X509Certificate2(certificate.Handle)))
            {
                X509ChainStatus[] chainStatus = x509Chain.ChainStatus;
                for (int i = 0; i < chainStatus.Length; i++)
                {
                    x509ChainStatusFlags |= chainStatus[i].Status;
                }
            }

            return x509ChainStatusFlags;
        }

        private void VerifyPartsExist(IEnumerable<Uri> parts)
        {
            if (parts == null)
            {
                return;
            }

            foreach (Uri part in parts)
            {
                if (!_container.PartExists(part))
                {
                    if (_signatures.Count == 0)
                    {
                        DeleteOriginPart();
                    }

                    throw new ArgumentException(SR.Get("PartToSignMissing"), "parts");
                }
            }
        }

        private void VerifySignArguments(IEnumerable<Uri> parts, X509Certificate certificate, IEnumerable<PackageRelationshipSelector> relationshipSelectors, string signatureId, IEnumerable<DataObjectCustom> signatureObjects, IEnumerable<ReferenceCustom> objectReferences)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException("certificate");
            }

            if (EnumeratorEmptyCheck(parts) && EnumeratorEmptyCheck(relationshipSelectors) && EnumeratorEmptyCheck(signatureObjects) && EnumeratorEmptyCheck(objectReferences))
            {
                throw new ArgumentException(SR.Get("NothingToSign"));
            }

            if (signatureObjects != null)
            {
                List<string> list = new List<string>();
                foreach (DataObjectCustom signatureObject in signatureObjects)
                {
                    if (string.CompareOrdinal(signatureObject.Id, XTable.Get(XTable.ID.OpcAttrValue)) == 0)
                    {
                        throw new ArgumentException(SR.Get("SignaturePackageObjectTagMustBeUnique"), "signatureObjects");
                    }

                    if (list.Exists(new StringMatchPredicate(signatureObject.Id).Match))
                    {
                        throw new ArgumentException(SR.Get("SignatureObjectIdMustBeUnique"), "signatureObjects");
                    }

                    list.Add(signatureObject.Id);
                }
            }

            if (signatureId != null && signatureId != string.Empty)
            {
                try
                {
                    XmlConvert.VerifyNCName(signatureId);
                }
                catch (XmlException innerException)
                {
                    throw new ArgumentException(SR.Get("NotAValidXmlIdString", signatureId), "signatureId", innerException);
                }
            }
        }

        public VerifyResult VerifySignatures(bool exitOnFailure)
        {
            EnsureSignatures();
            if (_signatures.Count == 0)
            {
                return VerifyResult.NotSigned;
            }

            VerifyResult result = VerifyResult.Success;
            for (int i = 0; i < _signatures.Count; i++)
            {
                VerifyResult verifyResult = _signatures[i].Verify();
                if (verifyResult != 0)
                {
                    result = verifyResult;
                    if (this.InvalidSignatureEvent != null)
                    {
                        this.InvalidSignatureEvent(this, new SignatureVerificationEventArgs(_signatures[i], verifyResult));
                    }

                    if (exitOnFailure)
                    {
                        return result;
                    }
                }
            }

            return result;
        }
    }
}

