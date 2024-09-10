// Decompiled with JetBrains decompiler
// Type: PageEventHelper
// Assembly: ESignLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FC312E54-D344-41AE-A232-7B8768ED42AA
// Assembly location: D:\1. Project\EBH\EBH\Bin\ESignLibrary.dll

using iTextSharp.text;
using iTextSharp.text.pdf;

public class PageEventHelper : PdfPageEventHelper
{
  private PdfContentByte cb;
  private PdfTemplate template;

  public override void OnOpenDocument(PdfWriter writer, Document document)
  {
    try
    {
      this.cb = writer.DirectContent;
      this.template = this.cb.CreateTemplate(50f, 50f);
    }
    catch
    {
    }
  }

  public override void OnEndPage(PdfWriter writer, Document document)
  {
    try
    {
      base.OnEndPage(writer, document);
      string text = "Trang " + writer.PageNumber.ToString();
      Rectangle pageSize = document.PageSize;
      this.cb.SetRGBColorFill(100, 100, 100);
      this.cb.BeginText();
      BaseFont font1 = BaseFont.CreateFont("Times-Roman", "Cp1252", false);
      iTextSharp.text.Font font2 = new iTextSharp.text.Font(font1, 8f, 2, BaseColor.RED);
      this.cb.SetFontAndSize(font1, 8f);
      this.cb.SetTextMatrix((float) (((double) document.LeftMargin + (double) pageSize.GetRight(document.RightMargin)) / 2.0 - 20.0), pageSize.GetBottom(document.BottomMargin) - 10f);
      this.cb.ShowText(text);
      this.cb.EndText();
    }
    catch
    {
    }
  }

  public override void OnCloseDocument(PdfWriter writer, Document document)
  {
    try
    {
      base.OnCloseDocument(writer, document);
    }
    catch
    {
    }
  }
}
