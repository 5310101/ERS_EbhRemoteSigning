using EBH_RemoteSigning_Service_ERS.clsUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EBH_RemoteSigning_Service_ERS
{
    public class Utilities
    {
       public static PathProvider globalPath = new PathProvider();  
       public static Logger logger = new Logger();
       public static GlobalVar glbVar = new GlobalVar();  
       public DbService dbService = new DbService();
    }
}