using System;

namespace IntrustCA_Domain
{
    /// <summary>
    /// Phien ky het han va ko gia han duoc (refresh token het han...), can tao phien moi
    /// </summary>
    public class SessionExpiredException : Exception
    {
        public SessionExpiredException(string message, Exception innerException = null) : base(message, innerException) { }
    }
}
