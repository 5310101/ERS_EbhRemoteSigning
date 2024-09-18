using ERS_Domain.clsUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERS_Domain
{
    public class Utilities
    {
       public static PathProvider globalPath = new PathProvider();  
       public static Logger logger = new Logger();
       public static GlobalVar glbVar = new GlobalVar();  
    }
}