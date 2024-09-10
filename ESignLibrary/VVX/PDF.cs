// Decompiled with JetBrains decompiler
// Type: VVX.PDF
// Assembly: ESignLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FC312E54-D344-41AE-A232-7B8768ED42AA
// Assembly location: D:\1. Project\EBH\EBH\Bin\ESignLibrary.dll

using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Diagnostics;
using System.IO;

namespace VVX
{
  internal class PDF
  {
    private string msFilePDF = "";
    private string msMsg = "";
    private bool mbRet = false;
    private string msEOL = Environment.NewLine;
    private float mfWidthScaleFactor = 1f;
    private bool mbApplyAlternatingColors = false;
    private string msDocTitle = "";
    private string msDocAuthor = "";
    private string msDocSubject = "";
    private string msDocKeywords = "";
    private bool mbView2PageLayout = false;
    private bool mbViewToolbar = true;
    private bool mbViewMenubar = true;
    private bool mbViewWindowUI = true;
    private bool mbViewResizeToFit = true;
    private bool mbViewCenterOnScreen = true;
    private PDF.PaperSize menShowPaperSize = PDF.PaperSize.LetterUS;
    private bool mbShowTitle = true;
    private bool mbShowPageNumber = true;
    private bool mbShowWatermark = true;
    private string msShowWatermarkText = "WATERMARK";
    private string msShowWatermarkFile = "watermark.png";
    private bool mbShowLandscape = true;
    private PDF.TypeFace menBodyTypeFace = PDF.TypeFace.Arial;
    private float mfBodyTypeSize = 10f;
    private bool mbBodyTypeStyleBold = false;
    private bool mbBodyTypeStyleItalics = false;
    private PDF.TypeFace menHeaderTypeFace = PDF.TypeFace.Arial;
    private float mfHeaderTypeSize = 10f;
    private bool mbHeaderTypeStyleBold = true;
    private bool mbHeaderTypeStyleItalics = false;
    private bool mbEncryptionNeeded = false;
    private string msEncryptionPasswordOfCreator = "";
    private string msEncryptionPasswordOfReader = "";
    private bool mbEncryptionStrong = false;
    private bool mbAllowPrinting = true;
    private bool mbAllowModifyContents = true;
    private bool mbAllowCopy = true;
    private bool mbAllowModifyAnnotations = true;
    private bool mbAllowFillIn = true;
    private bool mbAllowScreenReaders = true;
    private bool mbAllowAssembly = true;
    private bool mbAllowDegradedPrinting = true;

    public PDF.PaperSize ShowPaperSize
    {
      get
      {
        return this.menShowPaperSize;
      }
      set
      {
        this.menShowPaperSize = value;
      }
    }

    public bool View2PageLayout
    {
      get
      {
        return this.mbView2PageLayout;
      }
      set
      {
        this.mbView2PageLayout = value;
      }
    }

    public bool ViewToolbar
    {
      get
      {
        return this.mbViewToolbar;
      }
      set
      {
        this.mbViewToolbar = value;
      }
    }

    public bool ViewMenubar
    {
      get
      {
        return this.mbViewMenubar;
      }
      set
      {
        this.mbViewMenubar = value;
      }
    }

    public bool ViewWindowUI
    {
      get
      {
        return this.mbViewWindowUI;
      }
      set
      {
        this.mbViewWindowUI = value;
      }
    }

    public bool ViewResizeToFit
    {
      get
      {
        return this.mbViewResizeToFit;
      }
      set
      {
        this.mbViewResizeToFit = value;
      }
    }

    public bool ViewCenterOnScreen
    {
      get
      {
        return this.mbViewCenterOnScreen;
      }
      set
      {
        this.mbViewCenterOnScreen = value;
      }
    }

    public bool ShowTitle
    {
      get
      {
        return this.mbShowTitle;
      }
      set
      {
        this.mbShowTitle = value;
      }
    }

    public bool ShowPageNumber
    {
      get
      {
        return this.mbShowPageNumber;
      }
      set
      {
        this.mbShowPageNumber = value;
      }
    }

    public bool ShowWatermark
    {
      get
      {
        return this.mbShowWatermark;
      }
      set
      {
        this.mbShowWatermark = value;
      }
    }

    public string ShowWatermarkText
    {
      get
      {
        return this.msShowWatermarkText;
      }
      set
      {
        this.msShowWatermarkText = value;
      }
    }

    public string ShowWatermarkFile
    {
      get
      {
        return this.msShowWatermarkFile;
      }
      set
      {
        this.msShowWatermarkFile = value;
      }
    }

    public bool ShowLandscape
    {
      get
      {
        return this.mbShowLandscape;
      }
      set
      {
        this.mbShowLandscape = value;
      }
    }

    public bool ApplyAlternatingColors
    {
      get
      {
        return this.mbApplyAlternatingColors;
      }
      set
      {
        this.mbApplyAlternatingColors = value;
      }
    }

    public string DocTitle
    {
      get
      {
        return this.msDocTitle;
      }
      set
      {
        this.msDocTitle = value;
      }
    }

    public string DocAuthor
    {
      get
      {
        return this.msDocAuthor;
      }
      set
      {
        this.msDocAuthor = value;
      }
    }

    public string DocSubject
    {
      get
      {
        return this.msDocSubject;
      }
      set
      {
        this.msDocSubject = value;
      }
    }

    public string DocKeywords
    {
      get
      {
        return this.msDocKeywords;
      }
      set
      {
        this.msDocKeywords = value;
      }
    }

    public float WidthScaleFactor
    {
      get
      {
        return this.mfWidthScaleFactor;
      }
      set
      {
        this.mfWidthScaleFactor = value;
      }
    }

    public string Message
    {
      get
      {
        return this.msMsg;
      }
      set
      {
        this.msMsg = value;
      }
    }

    public bool Success
    {
      get
      {
        return this.mbRet;
      }
      set
      {
        this.mbRet = value;
      }
    }

    public string Filename
    {
      get
      {
        return this.msFilePDF;
      }
      set
      {
        this.msFilePDF = value;
      }
    }

    public PDF.TypeFace FontBodyTypeFace
    {
      get
      {
        return this.menBodyTypeFace;
      }
      set
      {
        this.menBodyTypeFace = value;
      }
    }

    public float FontBodyTypeSize
    {
      get
      {
        return this.mfBodyTypeSize;
      }
      set
      {
        this.mfBodyTypeSize = value;
      }
    }

    public bool FontBodyTypeStyleBold
    {
      get
      {
        return this.mbBodyTypeStyleBold;
      }
      set
      {
        this.mbBodyTypeStyleBold = value;
      }
    }

    public bool FontBodyTypeStyleItalics
    {
      get
      {
        return this.mbBodyTypeStyleItalics;
      }
      set
      {
        this.mbBodyTypeStyleItalics = value;
      }
    }

    internal PDF.TypeFace FontHeaderTypeFace
    {
      get
      {
        return this.menHeaderTypeFace;
      }
      set
      {
        this.menHeaderTypeFace = value;
      }
    }

    public float FontHeaderTypeSize
    {
      get
      {
        return this.mfHeaderTypeSize;
      }
      set
      {
        this.mfHeaderTypeSize = value;
      }
    }

    public bool FontHeaderTypeStyleBold
    {
      get
      {
        return this.mbHeaderTypeStyleBold;
      }
      set
      {
        this.mbHeaderTypeStyleBold = value;
      }
    }

    public bool FontHeaderTypeStyleItalics
    {
      get
      {
        return this.mbHeaderTypeStyleItalics;
      }
      set
      {
        this.mbHeaderTypeStyleItalics = value;
      }
    }

    public bool EncryptionNeeded
    {
      get
      {
        return this.mbEncryptionNeeded;
      }
      set
      {
        this.mbEncryptionNeeded = value;
      }
    }

    public string EncryptionPasswordOfCreator
    {
      get
      {
        return this.msEncryptionPasswordOfCreator;
      }
      set
      {
        this.msEncryptionPasswordOfCreator = value;
      }
    }

    public string EncryptionPasswordOfReader
    {
      get
      {
        return this.msEncryptionPasswordOfReader;
      }
      set
      {
        this.msEncryptionPasswordOfReader = value;
      }
    }

    public bool EncryptionStrong
    {
      get
      {
        return this.mbEncryptionStrong;
      }
      set
      {
        this.mbEncryptionStrong = value;
      }
    }

    public bool AllowPrinting
    {
      get
      {
        return this.mbAllowPrinting;
      }
      set
      {
        this.mbAllowPrinting = value;
      }
    }

    public bool AllowModifyContents
    {
      get
      {
        return this.mbAllowModifyContents;
      }
      set
      {
        this.mbAllowModifyContents = value;
      }
    }

    public bool AllowCopy
    {
      get
      {
        return this.mbAllowCopy;
      }
      set
      {
        this.mbAllowCopy = value;
      }
    }

    public bool AllowModifyAnnotations
    {
      get
      {
        return this.mbAllowModifyAnnotations;
      }
      set
      {
        this.mbAllowModifyAnnotations = value;
      }
    }

    public bool AllowFillIn
    {
      get
      {
        return this.mbAllowFillIn;
      }
      set
      {
        this.mbAllowFillIn = value;
      }
    }

    public bool AllowScreenReaders
    {
      get
      {
        return this.mbAllowScreenReaders;
      }
      set
      {
        this.mbAllowScreenReaders = value;
      }
    }

    public bool AllowAssembly
    {
      get
      {
        return this.mbAllowAssembly;
      }
      set
      {
        this.mbAllowAssembly = value;
      }
    }

    public bool AllowDegradedPrinting
    {
      get
      {
        return this.mbAllowDegradedPrinting;
      }
      set
      {
        this.mbAllowDegradedPrinting = value;
      }
    }

    private bool DoInsertImageFile(Document document, string sFilename, bool bInsertMsg)
    {
      bool flag = false;
      try
      {
        if (!File.Exists(sFilename) && MsgBox.Confirm("Unable to find '" + sFilename + "' in the current folder.\n\nWould you like to locate it?"))
          sFilename = FileDialog.GetFilenameToOpen(FileDialog.FileType.Image);
        Image img = (Image) null;
        if (File.Exists(sFilename))
          this.DoGetImageFile(sFilename, out img);
        if (img != null)
        {
          document.Add((IElement) img);
          flag = true;
        }
        else if (bInsertMsg)
          document.Add((IElement) new Paragraph(sFilename + " not found"));
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex.ToString());
      }
      return flag;
    }

    private Image DoGetImageFile(string sFilename)
    {
      bool flag = false;
      Image image = (Image) null;
      try
      {
        if (!File.Exists(sFilename) && MsgBox.Confirm("Unable to find '" + sFilename + "' in the current folder.\n\nWould you like to locate it?"))
          sFilename = FileDialog.GetFilenameToOpen(FileDialog.FileType.Image);
        if (File.Exists(sFilename))
          image = Image.GetInstance(sFilename);
        flag = image != null;
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex.ToString());
      }
      return image;
    }

    private bool DoGetImageFile(string sFilename, out Image img)
    {
      bool flag = false;
      img = (Image) null;
      try
      {
        if (!File.Exists(sFilename) && MsgBox.Confirm("Unable to find '" + sFilename + "' in the current folder.\n\nWould you like to locate it?"))
          sFilename = FileDialog.GetFilenameToOpen(FileDialog.FileType.Image);
        if (File.Exists(sFilename))
          img = Image.GetInstance(sFilename);
        flag = img != null;
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex.ToString());
      }
      return flag;
    }

    private bool DoLocateImageFile(ref string sFilename)
    {
      bool flag = false;
      try
      {
        if (!File.Exists(sFilename))
        {
          if (MsgBox.Confirm("Unable to find '" + sFilename + "' in the current folder.\n\nWould you like to locate it?"))
            sFilename = FileDialog.GetFilenameToOpen(FileDialog.FileType.Image);
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex.ToString());
      }
      return flag = File.Exists(sFilename);
    }

    private bool DoAddWatermark_EXPT(Document document, string sFileWatermark)
    {
      bool flag1 = false;
      try
      {
        if (sFileWatermark.Length > 0)
        {
          if (!File.Exists(sFileWatermark))
            sFileWatermark = FileDialog.GetFilenameToOpen(FileDialog.FileType.Image);
          Image imageFile = this.DoGetImageFile(sFileWatermark);
          bool flag2 = true;
          if (imageFile != null)
          {
            imageFile.SetAbsolutePosition(120f, 300f);
            imageFile.RotationDegrees = 30f;
            document.Add((IElement) imageFile);
          }
          else if (flag2)
            document.Add((IElement) new Paragraph(sFileWatermark + " not found"));
          flag1 = true;
        }
      }
      catch (DocumentException ex)
      {
        this.Message = ex.Message;
      }
      catch (IOException ex)
      {
        this.Message = ex.Message;
      }
      return flag1;
    }

    public bool CreateEmptyPDF(string sFilePDF)
    {
      this.mbRet = false;
      Debug.WriteLine("Creating an empty PDF");
      Document document = new Document(PageSize.LETTER.Rotate());
      try
      {
        PdfWriter.GetInstance(document, (Stream) new FileStream(sFilePDF, FileMode.Create));
        document.Open();
        document.Add((IElement) new Paragraph("Nothing to display"));
        this.mbRet = true;
      }
      catch (DocumentException ex)
      {
        this.Message = ex.Message;
      }
      catch (IOException ex)
      {
        this.Message = ex.Message;
      }
      document.Close();
      if (this.mbRet)
        this.Message = sFilePDF + " has been created";
      return this.mbRet;
    }

    private string DoCvtToFontName(PDF.TypeFace enTypeFace)
    {
      string str;
      switch (enTypeFace)
      {
        case PDF.TypeFace.Times:
          str = "Times-Roman";
          break;
        case PDF.TypeFace.Courier:
          str = "Courier";
          break;
        default:
          str = "Helvetica";
          break;
      }
      return str;
    }

    private int DoCvtToStyle(bool bBold, bool bItalics)
    {
      int num = 0;
      if (bBold)
        num |= 1;
      if (bItalics)
        num |= 2;
      return num;
    }

    private bool DoSetViewerPreferences(PdfWriter writer)
    {
      bool flag = true;
      int num = 0;
      if (this.mbView2PageLayout)
        num |= 4;
      if (this.mbViewCenterOnScreen)
        num |= 65536;
      if (!this.mbViewMenubar)
        num |= 8192;
      if (this.mbViewResizeToFit)
        num |= 32768;
      if (!this.mbViewToolbar)
        num |= 4096;
      if (!this.mbViewWindowUI)
        num |= 16384;
      writer.ViewerPreferences = num;
      return flag;
    }

    private bool DoSetViewerPermissions(PdfWriter writer)
    {
      bool flag = true;
      int permissions = 0;
      if (this.mbAllowPrinting)
        permissions |= 2052;
      else
        this.mbEncryptionNeeded = true;
      if (this.mbAllowModifyContents)
        permissions |= 8;
      else
        this.mbEncryptionNeeded = true;
      if (this.mbAllowCopy)
        permissions |= 16;
      else
        this.mbEncryptionNeeded = true;
      if (this.mbAllowModifyAnnotations)
        permissions |= 32;
      else
        this.mbEncryptionNeeded = true;
      if (this.mbAllowFillIn)
        permissions |= 256;
      else
        this.mbEncryptionNeeded = true;
      if (this.mbAllowScreenReaders)
        permissions |= 512;
      else
        this.mbEncryptionNeeded = true;
      if (this.mbAllowAssembly)
        permissions |= 1024;
      else
        this.mbEncryptionNeeded = true;
      if (this.mbAllowDegradedPrinting)
        permissions |= 4;
      else
        this.mbEncryptionNeeded = true;
      if (this.mbEncryptionNeeded)
        writer.SetEncryption(this.mbEncryptionStrong, this.msEncryptionPasswordOfReader, this.msEncryptionPasswordOfCreator, permissions);
      return flag;
    }

    private bool DoAddMetaData(Document document)
    {
      bool flag = true;
      if (document != null)
      {
        document.AddTitle(this.msDocTitle);
        document.AddSubject(this.msDocSubject);
        document.AddKeywords(this.msDocKeywords);
        document.AddCreator("VVX.PDF");
        document.AddAuthor(this.msDocAuthor);
        document.AddHeader("Expires", "0");
      }
      else
        flag = false;
      return flag;
    }

    public bool DoCreateFromXmlStore(string sFilePDF, string sXmlStoreFileIn)
    {
      this.mbRet = false;
      Debug.WriteLine("CreateFromXmlStore: " + sXmlStoreFileIn);
      Rectangle pageSize = (Rectangle) null;
      if (this.mbShowLandscape)
        pageSize = pageSize.Rotate();
      Document document = new Document(pageSize);
      try
      {
        PdfWriter instance = PdfWriter.GetInstance(document, (Stream) new FileStream(sFilePDF, FileMode.Create));
        this.DoSetViewerPreferences(instance);
        this.DoSetViewerPermissions(instance);
        this.DoAddMetaData(document);
        document.Open();
        this.DoLoadDocument(document, sXmlStoreFileIn);
        this.mbRet = true;
      }
      catch (DocumentException ex)
      {
        this.Message = ex.Message;
      }
      catch (IOException ex)
      {
        this.Message = ex.Message;
      }
      document.Close();
      if (this.mbRet)
        this.Message = sFilePDF + " has been created";
      return this.mbRet;
    }

    private bool DoLoadDocument(Document document, string sXmlStoreFile)
    {
      bool flag1 = false;
      try
      {
        bool flag2 = true;
        int index1 = 0;
        int index2 = 1;
        if (sXmlStoreFile.Length > 0)
        {
          XmlStore xmlStore = new XmlStore(sXmlStoreFile);
          int num1 = xmlStore.DoLoadRecords();
          int length = xmlStore.Fields.Length;
          if (num1 > 0 && length > 0)
          {
            int numColumns = length;
            if (flag2)
              numColumns = length - 1;
            PdfPTable pdfPtable = new PdfPTable(numColumns);
            pdfPtable.DefaultCell.Padding = 3f;
            float[] relativeWidths = new float[numColumns];
            float columnWidthsTotal = xmlStore.DoGetColumnWidthsTotal();
            for (int col = 0; col < numColumns; ++col)
            {
              if ((double) columnWidthsTotal == 0.0)
              {
                relativeWidths[col] = 100f / (float) numColumns;
              }
              else
              {
                float columnWidth = xmlStore.DoGetColumnWidth(col);
                relativeWidths[col] = columnWidth;
              }
            }
            int num2 = (double) this.mfWidthScaleFactor <= 0.0 ? 0 : ((double) columnWidthsTotal != 0.0 ? 1 : 0);
            pdfPtable.WidthPercentage = num2 != 0 ? columnWidthsTotal * this.mfWidthScaleFactor : 100f;
            pdfPtable.SetWidths(relativeWidths);
            iTextSharp.text.Font[] fontArray = new iTextSharp.text.Font[2];
            this.DoCvtToFontName(this.menBodyTypeFace);
            float mfBodyTypeSize = this.mfBodyTypeSize;
            int num3 = 0;
            if (this.mbBodyTypeStyleBold)
              num3 |= 1;
            if (this.mbBodyTypeStyleItalics)
            {
              int num4 = num3 | 2;
            }
            fontArray[0] = FontFactory.GetFont(this.DoCvtToFontName(this.menBodyTypeFace), this.mfBodyTypeSize, this.DoCvtToStyle(this.mbBodyTypeStyleBold, this.mbBodyTypeStyleItalics));
            fontArray[1] = FontFactory.GetFont(this.DoCvtToFontName(this.menHeaderTypeFace), this.mfHeaderTypeSize, this.DoCvtToStyle(this.mbHeaderTypeStyleBold, this.mbHeaderTypeStyleItalics));
            pdfPtable.DefaultCell.BorderWidth = 1f;
            pdfPtable.DefaultCell.HorizontalAlignment = 1;
            for (int index3 = 0; index3 < length; ++index3)
            {
              if (!flag2 || index3 != xmlStore.ColumnUID)
              {
                Phrase phrase = new Phrase(new Chunk(xmlStore.Fields[index3].title, fontArray[index2]));
                pdfPtable.AddCell(phrase);
              }
            }
            pdfPtable.HeaderRows = 1;
            for (int nRow = 0; nRow < num1; ++nRow)
            {
              if (this.mbApplyAlternatingColors && nRow % 2 == 1)
                pdfPtable.DefaultCell.GrayFill = 0.9f;
              string[] record = xmlStore.DoGetRecord(nRow);
              for (int index3 = 0; index3 < length; ++index3)
              {
                if (!flag2 || index3 != xmlStore.ColumnUID)
                {
                  Phrase phrase = new Phrase(new Chunk(record[index3], fontArray[index1]));
                  pdfPtable.AddCell(phrase);
                }
              }
              if (this.mbApplyAlternatingColors && nRow % 2 == 1)
                pdfPtable.DefaultCell.GrayFill = 1f;
            }
            XmlStoreEvent pageEvent = new XmlStoreEvent();
            this.DoConfigPageEventHandler(pageEvent);
            pdfPtable.TableEvent = (IPdfPTableEvent) pageEvent;
            document.Add((IElement) pdfPtable);
            flag1 = true;
          }
        }
      }
      catch (Exception ex)
      {
        this.Message += ex.StackTrace;
        Debug.WriteLine(this.Message);
      }
      if (!flag1)
        document.Add((IElement) new Paragraph("Failed to load data from" + sXmlStoreFile));
      return flag1;
    }

    private void DoConfigPageEventHandler(XmlStoreEvent pageEvent)
    {
      if (this.ShowTitle)
        pageEvent.PageTitle = this.msDocTitle;
      if (this.ShowPageNumber)
      {
        pageEvent.PageNumberFormat = "Page {0}";
        pageEvent.PageNumberStartingValue = 0;
      }
      if (!this.ShowWatermark)
        return;
      if (this.ShowWatermarkText.Length > 0)
        pageEvent.WatermarkText = this.ShowWatermarkText;
      if (this.ShowWatermarkFile.Length > 0)
        pageEvent.WatermarkFile = this.ShowWatermarkFile;
    }

    public bool Chap0518(string sFilePDF)
    {
      this.mbRet = false;
      Debug.WriteLine("Chapter 5 example 18: PdfPTable");
      Document document = new Document(PageSize.LETTER.Rotate(), 10f, 10f, 10f, 10f);
      try
      {
        PdfWriter.GetInstance(document, (Stream) new FileStream(sFilePDF, FileMode.Create));
        document.Open();
        this.LoadDocument(document);
        this.mbRet = true;
      }
      catch (DocumentException ex)
      {
        this.Message = ex.Message;
      }
      catch (IOException ex)
      {
        this.Message = ex.Message;
      }
      document.Close();
      if (this.mbRet)
        this.Message = sFilePDF + " has been created";
      return this.mbRet;
    }

    private void LoadDocument(Document document)
    {
      string[] strArray = new string[12]{ "M0065920", "Số lượng", "Tháng", "PCGOLD", "119000", "Ngày", "2001-08-13", "4350", "6011648299", "Máy chủ", "153", "Năm" };
      int numColumns = 12;
      try
      {
        PdfPTable pdfPtable = new PdfPTable(numColumns);
        iTextSharp.text.Font font = new iTextSharp.text.Font(BaseFont.CreateFont("C:\\WINDOWS.0\\Fonts\\TIMES.TTF", "Identity-H", false), 12f, 0);
        pdfPtable.DefaultCell.Padding = 3f;
        float[] relativeWidths = new float[12]{ 9f, 4f, 8f, 10f, 8f, 11f, 9f, 7f, 9f, 10f, 4f, 10f };
        pdfPtable.SetWidths(relativeWidths);
        pdfPtable.WidthPercentage = 100f;
        pdfPtable.DefaultCell.BorderWidth = 2f;
        pdfPtable.DefaultCell.HorizontalAlignment = 1;
        pdfPtable.DefaultCell.BackgroundColor = new BaseColor(200);
        pdfPtable.AddCell("  #");
        pdfPtable.AddCell("Trans loại");
        pdfPtable.AddCell("Cusip");
        pdfPtable.AddCell("Long Tên");
        pdfPtable.AddCell("Số lượng");
        pdfPtable.AddCell("Fraction Price");
        pdfPtable.AddCell("Settle Date");
        pdfPtable.AddCell("Portfolio");
        pdfPtable.AddCell("ADP Number");
        pdfPtable.AddCell("Account ID");
        pdfPtable.AddCell("Reg Rep ID");
        pdfPtable.AddCell("Amt To Go ");
        pdfPtable.HeaderRows = 1;
        pdfPtable.DefaultCell.BorderWidth = 1f;
        int num = 66;
        for (int index1 = 1; index1 < num; ++index1)
        {
          if (index1 % 2 == 1)
            pdfPtable.DefaultCell.GrayFill = 0.9f;
          for (int index2 = 0; index2 < numColumns; ++index2)
          {
            PdfPCell cell = new PdfPCell(new Phrase(strArray[index2].ToString(), font));
            pdfPtable.AddCell(cell);
          }
          if (index1 % 2 == 1)
            pdfPtable.DefaultCell.GrayFill = 1f;
        }
        document.Add((IElement) pdfPtable);
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex.StackTrace);
      }
    }

    public bool Chap1202(string sFile)
    {
      this.mbRet = false;
      Debug.WriteLine("Chapter 12 example 2: Table events");
      Document document = new Document(PageSize.LETTER, 20f, 20f, 20f, 20f);
      float[] relativeWidths1 = new float[2]{ 40f, 59f };
      try
      {
        PdfWriter.GetInstance(document, (Stream) new FileStream(sFile, FileMode.Create));
        document.Open();
        BaseFont font1 = BaseFont.CreateFont("C:\\WINDOWS\\Fonts\\TIMES.TTF", "Identity-H", false);
        float size = 14f;
        PdfPTable pdfPtable = new PdfPTable(2);
        pdfPtable.SetWidths(relativeWidths1);
        pdfPtable.WidthPercentage = 100f;
        pdfPtable.DefaultCell.Border = 0;
        PdfPTable table1 = new PdfPTable(1);
        PdfPTable table2 = new PdfPTable(1);
        PdfPTable table3 = new PdfPTable(1);
        PdfPTable table4 = new PdfPTable(2);
        PdfPTable table5 = new PdfPTable(6);
        PdfPTable table6 = new PdfPTable(1);
        float[] relativeWidths2 = new float[2]{ 20f, 79f };
        table4.SetWidths(relativeWidths2);
        table4.WidthPercentage = 100f;
        int num1 = 6;
        for (int index = 0; index < num1; ++index)
        {
          switch (index)
          {
            case 0:
              pdfPtable.AddCell(table1);
              break;
            case 1:
              pdfPtable.AddCell(table2);
              break;
            case 2:
              pdfPtable.DefaultCell.Colspan = 2;
              pdfPtable.AddCell(table3);
              break;
            case 3:
              pdfPtable.DefaultCell.Colspan = 2;
              pdfPtable.AddCell(table4);
              break;
            case 4:
              pdfPtable.DefaultCell.Colspan = 2;
              pdfPtable.AddCell(table5);
              break;
            case 5:
              pdfPtable.DefaultCell.Colspan = 2;
              pdfPtable.AddCell(table6);
              break;
          }
        }
        table1.DefaultCell.Border = 0;
        int num2 = 3;
        for (int index = 0; index < num2; ++index)
        {
          if (index != 0)
          {
            if (index == 1)
              table1.AddCell(new Phrase("-----------------", new iTextSharp.text.Font(font1, size)));
            if (index == 2)
              table1.AddCell(new Phrase("Số: 1018105808/2010TB-iHTKK", new iTextSharp.text.Font(font1, size)));
          }
          else
          {
            table1.DefaultCell.HorizontalAlignment = 1;
            table1.AddCell(new Phrase("TỔNG CỤC THUẾ", new iTextSharp.text.Font(font1, size)));
          }
        }
        table2.DefaultCell.Border = 0;
        int num3 = 4;
        BaseFont font2 = BaseFont.CreateFont("C:\\WINDOWS\\Fonts\\TIMESBI.TTF", "Identity-H", false);
        for (int index = 0; index < num3; ++index)
        {
          if (index != 0)
          {
            if (index == 1)
              table2.AddCell(new Phrase("Độc lập - Tự do - Hạnh phúc", new iTextSharp.text.Font(font1, size)));
            if (index == 2)
              table2.AddCell(new Phrase("----------o0o----------", new iTextSharp.text.Font(font1, size)));
            if (index == 3)
              table2.AddCell(new Phrase("Ngày 18 tháng 10 năm 2010", new iTextSharp.text.Font(font2, size)));
          }
          else
          {
            table2.DefaultCell.HorizontalAlignment = 1;
            table2.AddCell(new Phrase("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM", new iTextSharp.text.Font(font1, size)));
          }
        }
        table3.DefaultCell.Border = 0;
        int num4 = 2;
        BaseFont font3 = BaseFont.CreateFont("C:\\WINDOWS\\Fonts\\TIMESBD.TTF", "Identity-H", false);
        for (int index = 0; index < num4; ++index)
        {
          if (index != 0)
          {
            if (index == 1)
              table3.AddCell(new Phrase("V/v: Xác nhận đã nộp hồ sơ khai thuế qua mạng", new iTextSharp.text.Font(font3, size)));
          }
          else
          {
            table3.DefaultCell.HorizontalAlignment = 1;
            table3.AddCell(new Phrase("THÔNG BÁO", new iTextSharp.text.Font(font3, size)));
          }
        }
        table4.DefaultCell.Border = 0;
        int num5 = 6;
        for (int index = 0; index < num5; ++index)
        {
          if (index != 0)
          {
            if (index == 1)
            {
              table4.DefaultCell.HorizontalAlignment = 0;
              table4.AddCell(new Phrase("Công ty TNHH phát triển công nghệ Thái Sơn", new iTextSharp.text.Font(font1, size)));
            }
            if (index == 2)
              table4.AddCell(new Phrase("", new iTextSharp.text.Font(font3, size)));
            if (index == 3)
            {
              table4.DefaultCell.HorizontalAlignment = 0;
              table4.AddCell(new Phrase("Mã số thuế: 0101300842", new iTextSharp.text.Font(font1, size)));
            }
            if (index == 4)
            {
              table4.DefaultCell.Colspan = 2;
              table4.AddCell(new Phrase("", new iTextSharp.text.Font(font3, size)));
            }
            if (index == 5)
            {
              table4.DefaultCell.Colspan = 2;
              table4.AddCell(new Phrase("10 Giờ 58 Phút 8 Giây, Ngày 18/10/2010, Cơ quan Thuế đã nhận được hồ sơ khai thuế của đơn vị, gồm có:", new iTextSharp.text.Font(font1, size)));
            }
          }
          else
          {
            table4.DefaultCell.HorizontalAlignment = 0;
            table4.AddCell(new Phrase("Kính gửi:", new iTextSharp.text.Font(font2, size)));
          }
        }
        BaseFont font4 = BaseFont.CreateFont("C:\\WINDOWS\\Fonts\\TIMESBD.TTF", "Identity-H", false);
        table5.DefaultCell.Padding = 3f;
        float[] relativeWidths3 = new float[6]{ 7f, 30f, 15f, 10f, 9f, 28f };
        table5.SetWidths(relativeWidths3);
        table5.WidthPercentage = 100f;
        table5.DefaultCell.NoWrap = false;
        table5.DefaultCell.BorderWidth = 2f;
        table5.DefaultCell.HorizontalAlignment = 1;
        table5.DefaultCell.GrayFill = 0.9f;
        table5.AddCell(new Phrase("STT", new iTextSharp.text.Font(font4, size)));
        table5.AddCell(new Phrase("Tờ khai/Phụ lục", new iTextSharp.text.Font(font4, size)));
        table5.AddCell(new Phrase("Loại tờ khai", new iTextSharp.text.Font(font4, size)));
        table5.AddCell(new Phrase("Kỳ tính thuế", new iTextSharp.text.Font(font4, size)));
        table5.AddCell(new Phrase("Lần nộp", new iTextSharp.text.Font(font4, size)));
        table5.AddCell(new Phrase("Nơi nộp", new iTextSharp.text.Font(font4, size)));
        table5.HeaderRows = 1;
        string[] strArray = new string[6]{ "1", "Báo cáo tình hình sử dụng hóa đơn", "Chính thức", "09/2010", "1", "Chi cục Thuế Quận Hai Bà Trưng" };
        table5.DefaultCell.BorderWidth = 1f;
        int num6 = 6;
        for (int index = 0; index < num6; ++index)
        {
          table5.DefaultCell.GrayFill = 3.1f;
          table5.DefaultCell.HorizontalAlignment = 1;
          table5.AddCell(new Phrase(strArray[index].ToString(), new iTextSharp.text.Font(font1, size)));
        }
        table6.DefaultCell.Border = 0;
        int num7 = 8;
        for (int index = 0; index < num7; ++index)
        {
          if (index != 0)
          {
            if (index == 1)
            {
              table6.DefaultCell.HorizontalAlignment = 0;
              table6.AddCell(new Phrase(" ", new iTextSharp.text.Font(font1, size)));
            }
            if (index == 2)
              table6.AddCell(new Phrase(" ", new iTextSharp.text.Font(font3, size)));
            if (index == 3)
            {
              table6.DefaultCell.FixedHeight = 20f;
              table6.AddCell(new Phrase(" ", new iTextSharp.text.Font(font1, size)));
            }
            if (index == 4)
              table6.AddCell(new Phrase("Ghi chú: Thông báo này được gửi tự động từ hệ thống nhận tờ khai qua mạng của Cơ quan Thuế.", new iTextSharp.text.Font(font3, size)));
            if (index == 5)
              table6.AddCell(new Phrase("10 Giờ 58 Phút 8 Giây, Ngày 18/10/2010, Cơ quan Thuế đã nhận được hồ sơ khai thuế của đơn vị, gồm có:", new iTextSharp.text.Font(font1, size)));
            if (index == 6)
              table6.AddCell(new Phrase("_____________________________________________________________________________________________________", new iTextSharp.text.Font(font1, size)));
            if (index == 7)
              table6.AddCell(new Phrase("'Nộp hồ sơ khai thuế qua mạng là sự lựa chọn thông minh của bạn!'", new iTextSharp.text.Font(font1, size)));
          }
          else
          {
            table6.DefaultCell.HorizontalAlignment = 0;
            table6.AddCell(new Phrase("Để tra cứu thông tin đã kê khai, xin vui lòng truy cập theo đường dẫn: http://kekhaithue.gdt.gov.vn", new iTextSharp.text.Font(font2, size)));
          }
        }
        document.Add((IElement) pdfPtable);
        this.mbRet = true;
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex.Message);
        Debug.WriteLine(ex.StackTrace);
      }
      document.Close();
      return this.mbRet;
    }

    public bool Chap1011(string sFile)
    {
      this.mbRet = false;
      Console.WriteLine("Chapter 10 example 11: a PdfPTable in a template");
      Document document = new Document(new Rectangle(PageSize.A4), 50f, 50f, 50f, 50f);
      try
      {
        PdfWriter instance1 = PdfWriter.GetInstance(document, (Stream) new FileStream(sFile, FileMode.Create));
        document.Open();
        PdfTemplate template = instance1.DirectContent.CreateTemplate(20f, 20f);
        BaseFont font = BaseFont.CreateFont("Helvetica", "Cp1252", false);
        string text = "Vertical";
        float num = 16f;
        float widthPoint = font.GetWidthPoint(text, num);
        template.BeginText();
        template.SetRGBColorFillF(1f, 1f, 1f);
        template.SetFontAndSize(font, num);
        template.SetTextMatrix(0.0f, 2f);
        template.ShowText(text);
        template.EndText();
        template.Width = widthPoint;
        template.Height = num + 2f;
        Image instance2 = Image.GetInstance(template);
        instance2.Rotation = 90f;
        Chunk chunk = new Chunk(instance2, 0.0f, 0.0f);
        PdfPTable pdfPtable = new PdfPTable(3);
        pdfPtable.WidthPercentage = 100f;
        pdfPtable.DefaultCell.HorizontalAlignment = 1;
        pdfPtable.DefaultCell.VerticalAlignment = 5;
        PdfPCell cell = new PdfPCell(instance2);
        cell.Padding = 4f;
        cell.HorizontalAlignment = 1;
        pdfPtable.AddCell("I see a template on my right");
        pdfPtable.AddCell(cell);
        pdfPtable.AddCell("I see a template on my left");
        pdfPtable.AddCell(cell);
        pdfPtable.AddCell("I see a template everywhere");
        pdfPtable.AddCell(cell);
        pdfPtable.AddCell("I see a template on my right");
        pdfPtable.AddCell(cell);
        pdfPtable.AddCell("I see a template on my left");
        Paragraph paragraph1 = new Paragraph("This is a template ");
        paragraph1.Add((IElement) chunk);
        paragraph1.Add(" just here.");
        paragraph1.Leading = instance2.ScaledHeight * 1.1f;
        document.Add((IElement) paragraph1);
        document.Add((IElement) pdfPtable);
        Paragraph paragraph2 = new Paragraph("More templates ");
        paragraph2.Leading = instance2.ScaledHeight * 1.1f;
        paragraph2.Alignment = 3;
        instance2.ScalePercent(70f);
        for (int index = 0; index < 20; ++index)
          paragraph2.Add((IElement) chunk);
        document.Add((IElement) paragraph2);
        this.mbRet = true;
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex.Message);
        Debug.WriteLine(ex.StackTrace);
      }
      document.Close();
      return this.mbRet;
    }

    public enum PaperSize
    {
      LetterUS,
      LegalUS,
      A4,
      _COUNT,
    }

    public enum TypeFace
    {
      Times,
      Arial,
      Courier,
      _COUNT,
    }

    public enum ViewLayout
    {
      OnePage = 1,
      TwoPage = 4,
    }

    public enum ViewMode
    {
      Default,
    }
  }
}
