// Decompiled with JetBrains decompiler
// Type: VVX.File
// Assembly: ESignLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FC312E54-D344-41AE-A232-7B8768ED42AA
// Assembly location: D:\1. Project\EBH\EBH\Bin\ESignLibrary.dll

using System;
using System.IO;

namespace VVX
{
  internal class File
  {
    public static bool Exists(string filename)
    {
      bool flag = false;
      try
      {
        flag = new FileInfo(filename).Exists;
      }
      catch (Exception ex)
      {
        File.FileResult fileResult = new File.FileResult(true, "") { Success = false, Msg = ex.ToString() };
      }
      return flag;
    }

    public static bool IsReadOnly(string filename)
    {
      bool flag = false;
      try
      {
        FileInfo fileInfo = new FileInfo(filename);
        if (fileInfo.Exists)
          flag = fileInfo.IsReadOnly;
      }
      catch (Exception ex)
      {
        File.FileResult fileResult = new File.FileResult(true, "") { Success = false, Msg = ex.ToString() };
      }
      return flag;
    }

    public static bool Delete(string filename)
    {
      File.FileResult frRet = new File.FileResult(true, "");
      return File.Delete(filename, ref frRet);
    }

    public static bool Delete(string filename, ref File.FileResult frRet)
    {
      frRet = new File.FileResult(true, "");
      try
      {
        FileInfo fileInfo = new FileInfo(filename);
        if (fileInfo.Exists)
          fileInfo.Delete();
      }
      catch (Exception ex)
      {
        frRet.Success = false;
        frRet.Msg = ex.ToString();
      }
      return frRet.Success;
    }

    public static bool Encrypt(string filename)
    {
      File.FileResult frRet = new File.FileResult(true, "");
      return File.Encrypt(filename, ref frRet);
    }

    public static bool Encrypt(string filename, ref File.FileResult frRet)
    {
      frRet = new File.FileResult(true, "");
      try
      {
        FileInfo fileInfo = new FileInfo(filename);
        if (fileInfo.Exists)
          fileInfo.Encrypt();
      }
      catch (Exception ex)
      {
        frRet.Success = false;
        frRet.Msg = ex.ToString();
      }
      return frRet.Success;
    }

    public static bool Decrypt(string filename)
    {
      File.FileResult frRet = new File.FileResult(true, "");
      return File.Decrypt(filename, ref frRet);
    }

    public static bool Decrypt(string filename, ref File.FileResult frRet)
    {
      frRet = new File.FileResult(true, "");
      try
      {
        FileInfo fileInfo = new FileInfo(filename);
        if (fileInfo.Exists)
          fileInfo.Decrypt();
      }
      catch (Exception ex)
      {
        frRet.Success = false;
        frRet.Msg = ex.ToString();
      }
      return frRet.Success;
    }

    public static string DoExtractFilename(string sPathAndName)
    {
      try
      {
        return new FileInfo(sPathAndName).Name;
      }
      catch (Exception ex)
      {
        File.FileResult fileResult = new File.FileResult(true, "") { Success = false, Msg = ex.ToString() };
      }
      return sPathAndName;
    }

    public struct FileResult
    {
      public bool Success;
      public string Msg;

      public FileResult(bool bDefault, string sMsg)
      {
        this.Success = bDefault;
        this.Msg = "";
      }
    }
  }
}
