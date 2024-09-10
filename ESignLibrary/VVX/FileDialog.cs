// Decompiled with JetBrains decompiler
// Type: VVX.FileDialog
// Assembly: ESignLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FC312E54-D344-41AE-A232-7B8768ED42AA
// Assembly location: D:\1. Project\EBH\EBH\Bin\ESignLibrary.dll

using System.Windows.Forms;

namespace VVX
{
  public static class FileDialog
  {
    public static string GetFilters(FileDialog.FileType enFileType)
    {
      string str = "";
      switch (enFileType)
      {
        case FileDialog.FileType.Image:
          str = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG";
          break;
        case FileDialog.FileType.Text:
          str = "Text Files(*.TXT;*.CSV)|*.TXT;*.CSV";
          break;
        case FileDialog.FileType.XML:
          str = "XML Files(*.XML)|*.XML";
          break;
      }
      if (str.Length > 0)
        str += "|";
      return str + "All files (*.*)|*.*";
    }

    public static string GetFilenameToOpen(FileDialog.FileType enFileType)
    {
      return FileDialog.GetFilenameToOpen(enFileType, false, "");
    }

    public static string GetFilenameToOpen(FileDialog.FileType enFileType, string sFolder)
    {
      return FileDialog.GetFilenameToOpen(enFileType, false, sFolder);
    }

    public static string GetFilenameToOpen(FileDialog.FileType enFileType, bool bRestoreDirectory, string sFolder)
    {
      OpenFileDialog openFileDialog = new OpenFileDialog();
      openFileDialog.Filter = FileDialog.GetFilters(enFileType);
      openFileDialog.RestoreDirectory = bRestoreDirectory;
      if (sFolder.Length > 0)
        openFileDialog.InitialDirectory = sFolder;
      if (openFileDialog.ShowDialog() == DialogResult.OK)
        return openFileDialog.FileName;
      return "";
    }

    public static string GetFilenameToSave(FileDialog.FileType enFileType, string sFile)
    {
      SaveFileDialog saveFileDialog = new SaveFileDialog();
      saveFileDialog.Filter = FileDialog.GetFilters(enFileType);
      saveFileDialog.FileName = sFile;
      if (saveFileDialog.ShowDialog() == DialogResult.OK)
        return saveFileDialog.FileName;
      return "";
    }

    public enum FileType
    {
      All,
      Image,
      Text,
      XML,
    }
  }
}
