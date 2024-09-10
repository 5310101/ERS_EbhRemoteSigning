// Decompiled with JetBrains decompiler
// Type: TSDUtilC.Certificate
// Assembly: ESignLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FC312E54-D344-41AE-A232-7B8768ED42AA
// Assembly location: D:\1. Project\EBH\EBH\Bin\ESignLibrary.dll

using System;
using System.Collections;

namespace TSDUtilC
{
  public struct Certificate
  {
    public ArrayList IsSuer { get; set; }

    public string DN_Name { get; set; }

    public string DN_MST { get; set; }

    public DateTime NotAfter { get; set; }

    public DateTime NotBefore { get; set; }
  }
}
