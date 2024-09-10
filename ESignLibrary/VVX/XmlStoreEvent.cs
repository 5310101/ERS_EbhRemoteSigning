// Decompiled with JetBrains decompiler
// Type: VVX.XmlStoreEvent
// Assembly: ESignLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FC312E54-D344-41AE-A232-7B8768ED42AA
// Assembly location: D:\1. Project\EBH\EBH\Bin\ESignLibrary.dll

using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.IO;

namespace VVX
{
  internal class XmlStoreEvent : IPdfPTableEvent
  {
    private string msMsg = "";
    private string msEOL = Environment.NewLine;
    private string msPageTitle = "";
    private string msPageTitleFormat = "{0}";
    private int mnPagesTotal = 0;
    private int mnPageNumber = 0;
    private int mnPageSection = 0;
    private string msPageNumFormat = "";
    private string msWatermarkText = "";
    private string msWatermarkFile = "";
    private float mPageW;
    private float mPageH;
    private BaseFont mBaseFont;

    public int PageNumberStartingValue
    {
      get
      {
        return this.mnPageNumber;
      }
      set
      {
        this.mnPageNumber = value;
      }
    }

    public int PageSectionStartingValue
    {
      get
      {
        return this.mnPageSection;
      }
      set
      {
        this.mnPageSection = value;
      }
    }

    public string PageNumberFormat
    {
      get
      {
        return this.msPageNumFormat;
      }
      set
      {
        this.msPageNumFormat = value;
      }
    }

    public int PagesTotal
    {
      get
      {
        return this.mnPagesTotal;
      }
      set
      {
        this.mnPagesTotal = value;
      }
    }

    public string PageTitle
    {
      get
      {
        return this.msPageTitle;
      }
      set
      {
        this.msPageTitle = value;
      }
    }

    public string PageTitleFormat
    {
      get
      {
        return this.msPageTitleFormat;
      }
      set
      {
        this.msPageTitleFormat = value;
      }
    }

    public string WatermarkText
    {
      get
      {
        return this.msWatermarkText;
      }
      set
      {
        this.msWatermarkText = value;
      }
    }

    public string WatermarkFile
    {
      get
      {
        return this.msWatermarkFile;
      }
      set
      {
        this.msWatermarkFile = value;
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

    public XmlStoreEvent()
    {
    }

    public XmlStoreEvent(int nStartPageNum)
    {
      this.mnPageNumber = nStartPageNum;
    }

    public void TableLayout(PdfPTable table, float[][] width, float[] heights, int headerRows, int rowStart, PdfContentByte[] canvases)
    {
      float[] colWidths = width[0];
      int length1 = colWidths.Length;
      int length2 = heights.Length;
      int num1 = 0;
      int num2 = 0;
      int num3 = length1 - 1;
      int num4 = length2 - 1;
      PdfContentByte canvase1 = canvases[1];
      this.mPageW = canvase1.PdfDocument.Right + canvase1.PdfDocument.RightMargin;
      this.mPageH = canvase1.PdfDocument.Top + canvase1.PdfDocument.TopMargin;
      ++this.mnPageNumber;
      this.mBaseFont = BaseFont.CreateFont("Helvetica", "Cp1252", false);
      PdfContentByte canvase2 = canvases[3];
      canvase2.SaveState();
      this.DoDrawPageTitle(canvase2);
      this.DoDrawPageNumber(canvase2);
      canvase2.RestoreState();
      PdfContentByte canvase3 = canvases[1];
      canvase3.SaveState();
      this.DoDrawHeaderBackground(canvase3, colWidths, heights, headerRows, rowStart);
      this.DoDrawWatermarkText(canvase3);
      this.DoDrawWatermarkImage(canvase3);
      canvase3.RestoreState();
      PdfContentByte canvase4 = canvases[0];
      canvase4.SaveState();
      canvase4.SetLineWidth(0.5f);
      Random random = new Random();
      for (int index1 = num2; index1 < num4; ++index1)
      {
        float[] numArray = width[index1];
        float red = 0.8f;
        float green = 0.8f;
        float blue = 0.8f;
        for (int index2 = num1; index2 < num3; ++index2)
        {
          string url = "http://www.geocities.com/itextpdf";
          if (index1 == num2 && index2 == num1)
            canvase4.SetAction(new PdfAction(url), numArray[index2], heights[index1 + 1], numArray[index2 + 1], heights[index1]);
          canvase4.SetRGBColorStrokeF(red, green, blue);
          canvase4.MoveTo(numArray[index2], heights[index1]);
          canvase4.LineTo(numArray[index2 + 1], heights[index1]);
          canvase4.Stroke();
          canvase4.SetRGBColorStrokeF(red, green, blue);
          canvase4.MoveTo(numArray[index2], heights[index1]);
          canvase4.LineTo(numArray[index2], heights[index1 + 1]);
          canvase4.Stroke();
        }
      }
      canvase4.RestoreState();
    }

    private bool DoDrawPageTitle(PdfContentByte canvas)
    {
      bool flag = false;
      if (this.msPageTitle.Length > 0)
      {
        try
        {
          canvas.BeginText();
          string text = string.Format(this.msPageTitleFormat, (object) this.msPageTitle);
          float size = 10f;
          canvas.SetFontAndSize(this.mBaseFont, size);
          float x = this.mPageW / 2f;
          float y = this.mPageH - (canvas.PdfDocument.TopMargin - 8f);
          canvas.ShowTextAligned(1, text, x, y, 0.0f);
          canvas.EndText();
        }
        catch (DocumentException ex)
        {
          XmlStoreEvent xmlStoreEvent = this;
          string str = xmlStoreEvent.Message + ex.Message + this.msEOL;
          xmlStoreEvent.Message = str;
        }
      }
      return flag;
    }

    private bool DoDrawPageNumber(PdfContentByte canvas)
    {
      bool flag = false;
      try
      {
        if (this.msPageNumFormat.Length > 0)
        {
          canvas.BeginText();
          string text = this.msPageNumFormat.IndexOf("{1}") <= 0 ? string.Format(this.msPageNumFormat, (object) this.mnPageNumber) : string.Format(this.msPageNumFormat, (object) this.mnPageNumber, (object) this.mnPageSection);
          if (this.mnPagesTotal != 0)
            text = text + " of " + this.mnPagesTotal.ToString();
          canvas.SetFontAndSize(this.mBaseFont, 8f);
          float rotation = 0.0f;
          float x = this.mPageW / 2f;
          float y = 20f;
          canvas.ShowTextAligned(1, text, x, y, rotation);
          canvas.EndText();
        }
      }
      catch (DocumentException ex)
      {
        XmlStoreEvent xmlStoreEvent = this;
        string str = xmlStoreEvent.Message + ex.Message + this.msEOL;
        xmlStoreEvent.Message = str;
      }
      return flag;
    }

    private bool DoDrawWatermarkText(PdfContentByte canvas)
    {
      bool flag = false;
      try
      {
        canvas.BeginText();
        canvas.SetFontAndSize(this.mBaseFont, 72f);
        float rotation = 45f;
        float x = this.mPageW / 2f;
        float y = this.mPageH / 2f;
        canvas.ShowTextAligned(1, this.msWatermarkText, x, y, rotation);
        canvas.EndText();
      }
      catch (DocumentException ex)
      {
        XmlStoreEvent xmlStoreEvent = this;
        string str = xmlStoreEvent.Message + ex.Message + this.msEOL;
        xmlStoreEvent.Message = str;
      }
      return flag;
    }

    private void DoDrawLine(PdfContentByte canvas, float x1, float y1, float x2, float y2)
    {
      this.DoDrawLine(canvas, x1, y1, x2, y2, 0.5f, 0.5f, 0.5f);
    }

    private void DoDrawLine(PdfContentByte canvas, float x1, float y1, float x2, float y2, float fR, float fG, float fB)
    {
      canvas.SetRGBColorStrokeF(fR, fG, fB);
      canvas.MoveTo(x1, y1);
      canvas.LineTo(x2, y2);
      canvas.Stroke();
    }

    private void DoDrawHeaderBackground(PdfContentByte canvas, float[] colWidths, float[] heights, int headerRows, int rowStart)
    {
      int length1 = colWidths.Length;
      int length2 = heights.Length;
      int index1 = 0;
      int index2 = 0;
      int index3 = length1 - 1;
      int num = length2 - 1;
      float height1 = heights[index2];
      for (int index4 = index2; index4 < headerRows; ++index4)
        height1 += heights[index4];
      float colWidth = colWidths[index1];
      float height2 = heights[headerRows];
      float w = colWidths[index3] - colWidths[index1];
      float h = heights[index2] - heights[headerRows];
      canvas.Rectangle(colWidth, height2, w, h);
      canvas.SetRGBColorFillF(0.8f, 0.8f, 0.8f);
      canvas.FillStroke();
    }

    private bool DoDrawWatermarkImage(PdfContentByte canvas)
    {
      bool flag = false;
      try
      {
        if (this.msWatermarkFile.Length > 0)
        {
          if (!File.Exists(this.msWatermarkFile))
            this.msWatermarkFile = FileDialog.GetFilenameToOpen(FileDialog.FileType.Image);
          if (File.Exists(this.msWatermarkFile))
          {
            Image instance = Image.GetInstance(this.msWatermarkFile);
            if (instance != null)
            {
              float absoluteX = (float) (((double) this.mPageW - (double) instance.Width) / 2.0);
              float absoluteY = (float) (((double) this.mPageH - (double) instance.Height) / 2.0);
              instance.SetAbsolutePosition(absoluteX, absoluteY);
              instance.RotationDegrees = 0.0f;
              canvas.PdfDocument.Add((IElement) instance);
            }
          }
          flag = true;
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
      return flag;
    }
  }
}
