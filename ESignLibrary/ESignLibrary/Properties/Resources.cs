// Decompiled with JetBrains decompiler
// Type: ESignLibrary.Properties.Resources
// Assembly: ESignLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FC312E54-D344-41AE-A232-7B8768ED42AA
// Assembly location: D:\1. Project\EBH\EBH\Bin\ESignLibrary.dll

using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace ESignLibrary.Properties
{
  [DebuggerNonUserCode]
  [CompilerGenerated]
  [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "2.0.0.0")]
  internal class Resources
  {
    private static ResourceManager resourceMan;
    private static CultureInfo resourceCulture;

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static ResourceManager ResourceManager
    {
      get
      {
        if (object.ReferenceEquals((object) ESignLibrary.Properties.Resources.resourceMan, (object) null))
          ESignLibrary.Properties.Resources.resourceMan = new ResourceManager("ESignLibrary.Properties.Resources", typeof (ESignLibrary.Properties.Resources).Assembly);
        return ESignLibrary.Properties.Resources.resourceMan;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static CultureInfo Culture
    {
      get
      {
        return ESignLibrary.Properties.Resources.resourceCulture;
      }
      set
      {
        ESignLibrary.Properties.Resources.resourceCulture = value;
      }
    }

    internal static string OfficeObject
    {
      get
      {
        return ESignLibrary.Properties.Resources.ResourceManager.GetString("OfficeObject", ESignLibrary.Properties.Resources.resourceCulture);
      }
    }

    internal Resources()
    {
    }
  }
}
