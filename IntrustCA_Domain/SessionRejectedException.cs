using System;

namespace IntrustCA_Domain
{
    /// <summary>
    /// Tao phien ky that bai do user (tu choi, ko xac nhan kip) hoac Intrust tu choi, ko nen retry vi se hoi lai user
    /// </summary>
    public class SessionRejectedException : Exception
    {
        public SessionRejectedException(string message, Exception innerException = null) : base(message, innerException) { }
    }
}
