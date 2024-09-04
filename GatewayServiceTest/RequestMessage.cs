using System;
using System.Collections.Generic;
using System.Text;

namespace GatewayServiceTest
{
    /// <summary>
    /// 
    /// </summary>
    public class RequestMessage
    {
        public string RequestID { get; set; }
        public string ServiceID { get; set; }
        public string FunctionName { get; set; }

        // Complex property in request message. See API document to custom object for each request type
        public object Parameter { get; set; }
    }

    public class ReqCredentialSmartCA
    {

    }
    public class ReqCertificateSmartCA
    {
        public string credentialId { get; set; }
        public string certificates { get; set; }
        public bool certInfo { get; set; }
        public bool authInfo { get; set; }
    }

    public class TranInfoSmartCAResp
    {
        public int code { get; set; }
        public string codeDesc { get; set; }
        public string message { get; set; }
        public TranInfoSmartCARespContent content { get; set; }
    }

    public class TranInfoSmartCARespContent
    {
        public string refTranId { get; set; }
        public string tranId { get; set; }
        public string sub { get; set; }
        public string credentialId { get; set; }
        public int tranType { get; set; }
        public string tranTypeDesc { get; set; }
        public int tranStatus { get; set; }
        public string transStatusDesc { get; set; }
        public DateTime? reqTime { get; set; }
        public List<DocumentResp> documents { get; set; }

    }
    public class DocumentResp
    {
        public string name { get; set; }
        public string type { get; set; }
        public string size { get; set; }
        public string data { get; set; }
        public string hash { get; set; }
        public string sig { get; set; }
        public string signature { get; set; }
        public string dataSigned { get; set; }
        public string url { get; set; }

    }

    /// <summary>
    /// Property 'Parameter' in request message
    /// </summary>
    public class Parameter
    {
        public string Email { get; set; }
    }

    // Certificate ----------------------------------------------------
    public class CertParameter : Parameter
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

    public class CertResponse
    {
        public int Count { get; set; }
        public int PageNumber { get; set; }
        public int PageCount { get; set; }
        public List<Certificate> Items { get; set; }
    }

    public class CredentialSmartCAResponse
    {
        public int code { get; set; }
        public string codeDesc { get; set; }
        public string message { get; set; }
        public List<string> content { get; set; }
    }

    public class SignHashSmartCAResponse
    {
        public int code { get; set; }
        public string codeDesc { get; set; }
        public string message { get; set; }
        public ContenSignHash content { get; set; }
    }

    public class ContenSignHash
    {
        public string tranId { get; set; }
    }

    public class CertificateSmartCAResponse
    {
        public CertRes cert { get; set; }
        public keyRes key { get; set; }
        public string authMode { get; set; }
        public string scal { get; set; }
        public string mutisign { get; set; }
        public string status { get; set; }
    }

    public class SignHashSmartCAReq
    {
        public string credentialId { get; set; }
        public string refTranId { get; set; }
        public string notifyUrl { get; set; }
        public string description { get; set; }
        public List<DataSignHash> datas { get; set; }

    }

    public class SignSmartCAReq
    {
        public string credentialId { get; set; }
        public string refTranId { get; set; }
        public string notifyUrl { get; set; }
        public string description { get; set; }
        public List<DataSign> datas { get; set; }
    }
    public class DataSign
    {
        public string name { get; set; }
        public string dataBase64 { get; set; }
    }

    public class DataSignHash
    {
        public string name { get; set; }
        public string hash { get; set; }
    }

    public class CertRes
    {
        public string status { get; set; }
        public string serialNumber { get; set; }
        public string subjectDN { get; set; }
        public string issuerDN { get; set; }
        public List<string> certificates { get; set; }
        public string validFrom { get; set; }
        public string validTo { get; set; }
    }

    public class keyRes
    {
        public string status { get; set; }
        public List<string> alg { get; set; }
        public int len { get; set; }
    }


    public class Certificate
    {
        public string ID { get; set; }
        public string CertBase64 { get; set; }
        // More properties, see json response
    }

    // ---------------------------------------------------------------

    // Signature -----------------------------------------------------
    public class SignParameter
    {
        public string CertID { get; set; }
        public string ServiceGroupID { get; set; }
        public string FileName { get; set; }
        public string Type { get; set; }
        public string ContentType { get; set; }
        public string DataBase64 { get; set; }
    }
    public class SignResponse
    {
        public string TranID { get; set; }
        public int ResponseCode { get; set; }
        public string Message { get; set; }
        public string SignedData { get; set; }
    }
    // ---------------------------------------------------------------

    // Verify response -----------------------------------------------
    public class VerifyResultModel
    {
        public string TranID { get; set; }
        public bool status { get; set; }
        public string message { get; set; }

        public List<SignServerVerifyResultModel> signatures { get; set; }
    }
    public class SignServerVerifyResultModel
    {
        public string signingTime { get; set; }
        public bool signatureStatus { get; set; }
        public string certStatus { get; set; }
        public string certificate { get; set; }
        public int signatureIndex { get; set; }
        public int code { get; set; }
    }
    // ---------------------------------------------------------------
}
