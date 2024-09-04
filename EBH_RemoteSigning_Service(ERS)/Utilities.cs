using EBH_RemoteSigning_Service_ERS_.clsUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EBH_RemoteSigning_Service_ERS_
{
    public static class Utilities
    {
       public static PathProvider globalPath = new PathProvider();  
       public static Logger logger = new Logger();
       public static GlobalVar glbVar = new GlobalVar();  
       public static DbService dbService = new DbService();
    }
}