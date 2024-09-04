using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace EBH_RemoteSigning_Service_ERS_.clsUtilities
{
    public class GlobalVar
    {
		private string  _secretKey;
		public string  SecretKey
		{
			get 
			{
				if (string.IsNullOrEmpty(_secretKey))
				{
					_secretKey = ConfigurationManager.AppSettings["SECRETKEY"].ToString();
				}
				return _secretKey; 
			}
		}

	}
}