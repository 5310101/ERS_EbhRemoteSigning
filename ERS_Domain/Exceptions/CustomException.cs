using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using VnptHashSignatures.Common;

namespace ERS_Domain.Exceptions
{
    public class DatabaseInteractException : Exception
    {
        public string[] listIdError;

        public DatabaseInteractException()
        {
        }

        public DatabaseInteractException(string message) : base(message)
        {
        }

        public DatabaseInteractException(string message, Exception innerException) : base(message, innerException)
        {
        }
        public DatabaseInteractException(string message, string[] listIdError) : base(message)
        {
            this.listIdError = listIdError;
        }

    }

    public class CreateSignerException : Exception
    {
        public CreateSignerException()
        {
        }

        public CreateSignerException(string message) : base(message)
        {
        }

        public CreateSignerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
