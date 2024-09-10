// Decompiled with JetBrains decompiler
// Type: VVX.MyTableEvent
// Assembly: ESignLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FC312E54-D344-41AE-A232-7B8768ED42AA
// Assembly location: D:\1. Project\EBH\EBH\Bin\ESignLibrary.dll

using iTextSharp.text.pdf;
using System;

namespace VVX
{
  internal class MyTableEvent : IPdfPTableEvent
  {
    public void TableLayout(PdfPTable table, float[][] width, float[] heights, int headerRows, int rowStart, PdfContentByte[] canvases)
    {
      float[] numArray1 = width[0];
      int length1 = numArray1.Length;
      int length2 = heights.Length;
      int index1 = 0;
      int index2 = 0;
      int index3 = length1 - 1;
      int index4 = length2 - 1;
      PdfContentByte canvase1 = canvases[3];
      canvase1.SaveState();
      canvase1.SetLineWidth(2f);
      canvase1.SetRGBColorStroke((int) byte.MaxValue, 0, 0);
      float x1 = numArray1[index1];
      float height1 = heights[index4];
      float w1 = numArray1[index3] - numArray1[index1];
      float h1 = heights[index2] - heights[index4];
      canvase1.Rectangle(x1, height1, w1, h1);
      canvase1.Stroke();
      if (headerRows > 0)
      {
        float height2 = heights[index2];
        for (int index5 = index2; index5 < headerRows; ++index5)
          height2 += heights[index5];
        canvase1.SetRGBColorStroke(0, 0, (int) byte.MaxValue);
        float x2 = numArray1[index1];
        float height3 = heights[headerRows];
        float w2 = numArray1[index3] - numArray1[index1];
        float h2 = heights[index2] - heights[headerRows];
        canvase1.Rectangle(x2, height3, w2, h2);
        canvase1.Stroke();
        canvase1.SetRGBColorStrokeF(0.5f, 0.5f, 0.5f);
        canvase1.MoveTo(x2, height3);
        canvase1.LineTo(x2 + w2, height3 + h2);
        canvase1.Stroke();
      }
      canvase1.RestoreState();
      PdfContentByte canvase2 = canvases[0];
      canvase2.SaveState();
      canvase2.SetLineWidth(0.5f);
      Random random = new Random();
      for (int index5 = index2; index5 < index4; ++index5)
      {
        float[] numArray2 = width[index5];
        float red = 0.8f;
        float green = 0.8f;
        float blue = 0.8f;
        for (int index6 = index1; index6 < index3; ++index6)
        {
          string url = "http://www.geocities.com/itextpdf";
          if (index5 == index2 && index6 == index1)
            canvase2.SetAction(new PdfAction(url), numArray2[index6], heights[index5 + 1], numArray2[index6 + 1], heights[index5]);
          canvase2.SetRGBColorStrokeF(red, green, blue);
          canvase2.MoveTo(numArray2[index6], heights[index5]);
          canvase2.LineTo(numArray2[index6 + 1], heights[index5]);
          canvase2.Stroke();
          canvase2.SetRGBColorStrokeF(red, green, blue);
          canvase2.MoveTo(numArray2[index6], heights[index5]);
          canvase2.LineTo(numArray2[index6], heights[index5 + 1]);
          canvase2.Stroke();
        }
      }
      canvase2.RestoreState();
    }
  }
}
