using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace ESignLibrary
{
    /// <summary>
    /// SignatureDescription impl for http://www.w3.org/2001/04/xmldsig-more#rsa-sha256
    /// </summary>
    public class RSAPKCS1SHA256SignatureDescription : SignatureDescription
    {
        /// <summary>
        /// Registers the http://www.w3.org/2001/04/xmldsig-more#rsa-sha256 algorithm
        /// with the .NET CrytoConfig registry. This needs to be called once per
        /// appdomain before attempting to validate SHA256 signatures.
        /// </summary>
        public static void Register()
        {
            CryptoConfig.AddAlgorithm(
                typeof(RSAPKCS1SHA256SignatureDescription),
                "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
        }

        /// <summary>
        /// .NET calls this parameterless ctor
        /// </summary>
        public RSAPKCS1SHA256SignatureDescription()
        {
            base.KeyAlgorithm = typeof(RSACryptoServiceProvider).FullName;
            base.DigestAlgorithm = typeof(SHA256Managed).FullName;
            base.FormatterAlgorithm = typeof(RSAPKCS1SignatureFormatter).FullName;
            base.DeformatterAlgorithm = typeof(RSAPKCS1SignatureDeformatter).FullName;
            //KeyAlgorithm = "System.Security.Cryptography.RSACryptoServiceProvider";
            //DigestAlgorithm = "System.Security.Cryptography.SHA256Managed";
            //FormatterAlgorithm = "System.Security.Cryptography.RSAPKCS1SignatureFormatter";
            //DeformatterAlgorithm = "System.Security.Cryptography.RSAPKCS1SignatureDeformatter";
        }

        public override AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
        {
            var asymmetricSignatureDeformatter =
                (AsymmetricSignatureDeformatter)CryptoConfig.CreateFromName(DeformatterAlgorithm);
            asymmetricSignatureDeformatter.SetKey(key);
            asymmetricSignatureDeformatter.SetHashAlgorithm("SHA256");
            return asymmetricSignatureDeformatter;
        }

        public override AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key)
        {
            var asymmetricSignatureFormatter =
                (AsymmetricSignatureFormatter)CryptoConfig.CreateFromName(FormatterAlgorithm);
            asymmetricSignatureFormatter.SetKey(key);
            asymmetricSignatureFormatter.SetHashAlgorithm("SHA256");
            return asymmetricSignatureFormatter;
        }
    }
}
