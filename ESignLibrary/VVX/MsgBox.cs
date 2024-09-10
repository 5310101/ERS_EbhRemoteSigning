// Decompiled with JetBrains decompiler
// Type: VVX.MsgBox
// Assembly: ESignLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FC312E54-D344-41AE-A232-7B8768ED42AA
// Assembly location: D:\1. Project\EBH\EBH\Bin\ESignLibrary.dll

using System.Windows.Forms;

namespace VVX
{
  public static class MsgBox
  {
    public static bool Confirm(string sMsg)
    {
      return MsgBox.Confirm("Confirm", sMsg);
    }

    public static bool Confirm(string sTitle, string sMsg)
    {
      return MessageBox.Show(sMsg, sTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }

    public static void Error(string sMsg)
    {
      MsgBox.Error("Error", sMsg);
    }

    public static void Error(string sTitle, string sMsg)
    {
      int num = (int) MessageBox.Show(sMsg, sTitle, MessageBoxButtons.OK, MessageBoxIcon.Hand);
    }

    public static void Warning(string sMsg)
    {
      MsgBox.Warning("", sMsg);
    }

    public static void Warning(string sCaption, string sMsg)
    {
      if (sCaption.Length == 0)
        sCaption = "Warning";
      int num = (int) MessageBox.Show(sMsg, sCaption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
    }

    public static void Info(string sMsg)
    {
      MsgBox.Info("", sMsg);
    }

    public static void Info(string sCaption, string sMsg)
    {
      if (sCaption.Length == 0)
        sCaption = "Information";
      int num = (int) MessageBox.Show(sMsg, sCaption, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
    }
  }
}
