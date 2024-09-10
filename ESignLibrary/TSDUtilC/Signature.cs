// Decompiled with JetBrains decompiler
// Type: TSDUtilC.Signature
// Assembly: ESignLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FC312E54-D344-41AE-A232-7B8768ED42AA
// Assembly location: D:\1. Project\EBH\EBH\Bin\ESignLibrary.dll

using System;

namespace TSDUtilC
{
  public struct Signature
  {
    public string subjectAll { get; set; }

    public string issuerAll { get; set; }

    public string SignSubject { get; set; }

    public bool SignWholeDocument { get; set; }

    public TSDUtilC.Certificate[] Certificate { get; set; }

    public bool Verified { get; set; }

    public DateTime SignDate { get; set; }
  }
}
