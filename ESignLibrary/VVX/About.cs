// Decompiled with JetBrains decompiler
// Type: VVX.About
// Assembly: ESignLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FC312E54-D344-41AE-A232-7B8768ED42AA
// Assembly location: D:\1. Project\EBH\EBH\Bin\ESignLibrary.dll

using System;
using System.Diagnostics;
using System.Reflection;

namespace VVX
{
  internal class About
  {
    private const string MODULE_NAME = "VVX.About";
    private const string LAST_MODIFIED = "2007.03.14";

    public static string VersionGet()
    {
      return "Last Modified 2007.03.14";
    }

    public static void Version()
    {
      MsgBox.Info(About.VersionGet(), "VVX.About");
    }

    public static void Show()
    {
      string newLine = Environment.NewLine;
      Assembly executingAssembly = Assembly.GetExecutingAssembly();
      FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(executingAssembly.Location);
      string str = string.Format("{0}\n\nVersion {1}:{2}", (object) versionInfo.Comments, (object) versionInfo.FileMajorPart, (object) versionInfo.FileMinorPart);
      if (versionInfo.FileBuildPart != 0 || versionInfo.FilePrivatePart != 0)
        str += string.Format(":{0}:{1}", (object) versionInfo.FileBuildPart, (object) versionInfo.FilePrivatePart);
      string name = executingAssembly.GetName().Name;
      string legalCopyright = versionInfo.LegalCopyright;
      string sMsg = str + newLine + legalCopyright;
      MsgBox.Info(name, sMsg);
    }
  }
}
