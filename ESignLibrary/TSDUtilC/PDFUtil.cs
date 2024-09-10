// Decompiled with JetBrains decompiler
// Type: TSDUtilC.PDFUtil
// Assembly: ESignLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FC312E54-D344-41AE-A232-7B8768ED42AA
// Assembly location: D:\1. Project\EBH\EBH\Bin\ESignLibrary.dll

using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using Org.BouncyCastle.X509;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace TSDUtilC
{
  public class PDFUtil
  {
    public Hashtable pdfGetListField(string sFileName)
    {
      Hashtable hashtable = new Hashtable();
      PdfReader pdfReader = new PdfReader(sFileName);
      foreach (KeyValuePair<string, AcroFields.Item> field in (IEnumerable<KeyValuePair<string, AcroFields.Item>>) pdfReader.AcroFields.Fields)
        hashtable.Add((object) field.Key.ToString(), (object) pdfReader.AcroFields.GetField(field.Key.ToString()));
      pdfReader.Close();
      return hashtable;
    }

    public string pdfGetFieldValue(string sFileName, string sFieldName)
    {
      Hashtable listField = this.pdfGetListField(sFileName);
      string str = "";
      if (listField.ContainsKey((object) sFieldName))
        str = (string) listField[(object) sFieldName];
      return str;
    }

    public void pdfAddFieldWithValue(PdfWriter writer, string sFieldName, string sFieldValue, bool isHide)
    {
      TextField textField = new TextField(writer, new Rectangle(40f, 500f, 360f, 530f), sFieldName);
      textField.Text = sFieldValue;
      textField.FontSize = 12f;
      if (isHide)
        textField.Visibility = 1;
      writer.AddAnnotation((PdfAnnotation) textField.GetTextField());
    }

    public void pdfAddListField(string sFileName, string sListFieldName, string sListFieldValue, char sCharacter)
    {
      PdfWriter instance = PdfWriter.GetInstance(new Document(), (Stream) new FileStream(sFileName, FileMode.Create));
      string[] strArray1 = sListFieldName.Split(sCharacter);
      string[] strArray2 = sListFieldValue.Split(sCharacter);
      for (int index = 0; index < strArray2.Length; ++index)
        this.pdfAddFieldWithValue(instance, strArray1[index], strArray2[index], true);
    }

    public static Hashtable pdfGetListField(byte[] pdfContent)
    {
      Hashtable hashtable = new Hashtable();
      PdfReader pdfReader = new PdfReader(pdfContent);
      foreach (KeyValuePair<string, AcroFields.Item> field in (IEnumerable<KeyValuePair<string, AcroFields.Item>>) pdfReader.AcroFields.Fields)
        hashtable.Add((object) field.Key.ToString().ToUpper(), (object) pdfReader.AcroFields.GetField(field.Key.ToString()));
      pdfReader.Close();
      return hashtable;
    }

    public static string pdfGetFieldValue(byte[] pdfContent, string sFieldName)
    {
      Hashtable listField = PDFUtil.pdfGetListField(pdfContent);
      if (listField.ContainsKey((object) sFieldName.ToUpper()))
        return (string) listField[(object) sFieldName.ToUpper()];
      return (string) null;
    }

    public static bool VerifySignedPdf(string filePath)
    {
      Signature[] signaturesFromPdf = PDFUtil.GetSignaturesFromPdf(filePath);
      bool flag = signaturesFromPdf.Length > 0;
      for (int index = 0; index < signaturesFromPdf.Length; ++index)
        flag &= signaturesFromPdf[index].Verified;
      return flag;
    }

    public static bool VerifySignedPdf(byte[] pdfIn)
    {
      Signature[] signaturesFromPdf = PDFUtil.GetSignaturesFromPdf(pdfIn);
      bool flag = true;
      for (int index = 0; index < signaturesFromPdf.Length; ++index)
        flag &= signaturesFromPdf[index].Verified;
      return flag;
    }

    public static Signature[] GetSignaturesFromPdf(string filePath)
    {
      if (!File.Exists(filePath))
        return new Signature[0];
      return PDFUtil.GetSignaturesFromPdf((object) filePath);
    }

    public static Signature[] GetSignaturesFromPdf(byte[] pdfIn)
    {
      return PDFUtil.GetSignaturesFromPdf((object) pdfIn);
    }

    public static Signature[] GetSignaturesFromPdf(object pdf)
    {
      PdfReader pdfReader = (PdfReader) null;
      Signature[] signatureArray1 = new Signature[0];
      try
      {
        if (pdf.GetType().ToString().Equals("System.String"))
          pdfReader = new PdfReader(pdf.ToString());
        else if (pdf.GetType().ToString().Equals("System.Byte[]"))
          pdfReader = new PdfReader(pdf as byte[]);
        AcroFields acroFields = pdfReader.AcroFields;
        List<string> signatureNames = acroFields.GetSignatureNames();
        Signature[] signatureArray2 = new Signature[signatureNames.Count];
        for (int index1 = 0; index1 < signatureNames.Count; ++index1)
        {
          string str1 = signatureNames[index1];
          Console.Out.WriteLine("Signature name: " + str1);
          Console.Out.WriteLine("Signature covers whole document: " + (object) acroFields.SignatureCoversWholeDocument(str1));
          Console.Out.WriteLine("Document revision: " + (object) acroFields.GetRevision(str1) + " of " + (object) acroFields.TotalRevisions);
          PdfPKCS7 pdfPkcS7 = acroFields.VerifySignature(str1);
          DateTime signDate = pdfPkcS7.SignDate;
          Console.Out.WriteLine("Subject: " + PdfPKCS7.GetSubjectFields(pdfPkcS7.SigningCertificate).GetField("CN"));
          Console.Out.WriteLine("Document modified: " + (object) !pdfPkcS7.Verify());
          X509Certificate[] certificates = pdfPkcS7.Certificates;
          Certificate[] certificateArray = new Certificate[certificates.Length];
          string str2 = "";
          string str3 = "";
          for (int index2 = 0; index2 < certificates.Length; ++index2)
          {
            certificateArray[index2] = new Certificate();
            certificateArray[index2].IsSuer = certificates[index2].IssuerDN.GetValues();
            for (int index3 = 0; index3 < certificateArray[index2].IsSuer.Count; ++index3)
              str2 = str2 + " " + certificateArray[index2].IsSuer[index3].ToString();
            ArrayList values = certificates[index2].SubjectDN.GetValues();
            for (int index3 = 0; index3 < certificateArray[index2].IsSuer.Count; ++index3)
              str3 = str3 + " " + values[index3].ToString();
            if (values.Count > 4)
              certificateArray[index2].DN_MST = values[4].ToString().Replace("MST:", "");
            if (values.Count > 3)
              certificateArray[index2].DN_Name = values[3].ToString();
            certificateArray[index2].NotAfter = certificates[index2].NotAfter;
            certificateArray[index2].NotBefore = certificates[index2].NotBefore;
          }
          signatureArray2[index1] = new Signature();
          signatureArray2[index1].SignSubject = PdfPKCS7.GetSubjectFields(pdfPkcS7.SigningCertificate).GetField("CN");
          signatureArray2[index1].SignWholeDocument = acroFields.SignatureCoversWholeDocument(str1);
          signatureArray2[index1].Certificate = certificateArray;
          signatureArray2[index1].Verified = pdfPkcS7.Verify();
          signatureArray2[index1].SignDate = pdfPkcS7.SignDate;
          signatureArray2[index1].issuerAll = str2;
          signatureArray2[index1].subjectAll = str3;
        }
        pdfReader.Close();
        return signatureArray2;
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
        return new Signature[0];
      }
      finally
      {
        if (pdfReader != null)
          pdfReader.Close();
      }
    }

    public static bool IsSignedPdf(object pdf)
    {
      PdfReader pdfReader = (PdfReader) null;
      try
      {
        if (pdf.GetType().ToString().Equals("System.String"))
          pdfReader = new PdfReader(pdf.ToString());
        else if (pdf.GetType().ToString().Equals("System.Byte[]"))
          pdfReader = new PdfReader(pdf as byte[]);
        return pdfReader.AcroFields.GetSignatureNames().Count > 0;
      }
      catch
      {
        return false;
      }
      finally
      {
        if (pdfReader != null)
          pdfReader.Close();
      }
    }

    public static string GetLastSignSerialFromPdf(object pdf)
    {
      PdfReader pdfReader = (PdfReader) null;
      Signature[] signatureArray = new Signature[0];
      try
      {
        if (pdf.GetType().ToString().Equals("System.String"))
          pdfReader = new PdfReader(pdf.ToString());
        else if (pdf.GetType().ToString().Equals("System.Byte[]"))
          pdfReader = new PdfReader(pdf as byte[]);
        AcroFields acroFields = pdfReader.AcroFields;
        List<string> signatureNames = acroFields.GetSignatureNames();
        if (signatureNames.Count == 0)
          return "";
        string name = signatureNames[signatureNames.Count - 1];
        PdfPKCS7 pdfPkcS7 = acroFields.VerifySignature(name);
        if (!pdfPkcS7.Verify())
          return "";
        return pdfPkcS7.SigningCertificate.SerialNumber.ToString();
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
        signatureArray = new Signature[0];
        return "";
      }
      finally
      {
        if (pdfReader != null)
          pdfReader.Close();
      }
    }

    public static bool RemoveAllSignaturesFromPdf(ref object pdf)
    {
      PdfReader reader = (PdfReader) null;
      Signature[] signatureArray = new Signature[0];
      try
      {
        if (pdf.GetType().ToString().Equals("System.String"))
          reader = new PdfReader(pdf.ToString());
        else if (pdf.GetType().ToString().Equals("System.Byte[]"))
          reader = new PdfReader(pdf as byte[]);
        List<string> signatureNames = reader.AcroFields.GetSignatureNames();
        for (int index = 0; index < signatureNames.Count; ++index)
        {
          string name = signatureNames[index];
          reader.AcroFields.RemoveField(name);
          reader.AcroForm.Remove(PdfName.SIGFLAGS);
        }
        using (MemoryStream memoryStream = new MemoryStream())
        {
          new PdfStamper(reader, (Stream) memoryStream).Close();
          if (pdf.GetType().ToString().Equals("System.String"))
            File.WriteAllBytes(pdf.ToString(), memoryStream.ToArray());
          else if (pdf.GetType().ToString().Equals("System.Byte[]"))
            pdf = (object) memoryStream.ToArray();
        }
        reader.Close();
        return true;
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
        return false;
      }
      finally
      {
        if (reader != null)
          reader.Close();
      }
    }

    public static void AddNewPageWithText(string fontPath, string destinationFile, string sourceFiles, string text)
    {
      try
      {
        PdfReader reader = new PdfReader(sourceFiles);
        int numberOfPages = reader.NumberOfPages;
        Document document = new Document(reader.GetPageSizeWithRotation(1));
        PdfWriter instance = PdfWriter.GetInstance(document, (Stream) new FileStream(destinationFile, FileMode.Create));
        PageEventHelper pageEventHelper = new PageEventHelper();
        instance.PageEvent = (IPdfPageEvent) pageEventHelper;
        document.Open();
        PdfContentByte directContent = instance.DirectContent;
        int num = 0;
        while (num < numberOfPages)
        {
          ++num;
          document.SetPageSize(reader.GetPageSizeWithRotation(num));
          document.NewPage();
          PdfImportedPage importedPage = instance.GetImportedPage(reader, num);
          int pageRotation = reader.GetPageRotation(num);
          if (pageRotation == 90 | pageRotation == 270)
            directContent.AddTemplate((PdfTemplate) importedPage, 0.0f, -1f, 1f, 0.0f, 0.0f, reader.GetPageSizeWithRotation(num).Height);
          else
            directContent.AddTemplate((PdfTemplate) importedPage, 1f, 0.0f, 0.0f, 1f, 0.0f, 0.0f);
        }
        iTextSharp.text.Font font = new iTextSharp.text.Font(BaseFont.CreateFont(fontPath, "Identity-H", false), 15f, 1, BaseColor.BLACK);
        Paragraph paragraph = new Paragraph(0.0f, new Chunk(text + "\n", font));
        document.NewPage();
        document.SetPageSize(PageSize.A4);
        document.Add((IElement) paragraph);
        document.Add((IElement) new Chunk());
        document.Close();
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex.Message);
        Console.Error.WriteLine(ex.StackTrace);
      }
    }

    public static void AddTextToPage0(string fontPath, string destinationFile, string sourceFiles, string text)
    {
      try
      {
        Document document = new Document(new PdfReader(sourceFiles).GetPageSizeWithRotation(1));
        PdfWriter.GetInstance(document, (Stream) new FileStream(destinationFile, FileMode.Create));
        document.Open();
        iTextSharp.text.Font font = new iTextSharp.text.Font(BaseFont.CreateFont(fontPath, "Identity-H", false), 15f, 1, BaseColor.BLACK);
        Paragraph paragraph = new Paragraph(0.0f, new Chunk(text + "\n", font));
        document.NewPage();
        document.SetPageSize(PageSize.A4);
        document.Add((IElement) paragraph);
        document.Add((IElement) new Chunk());
        document.Close();
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex.Message);
        Console.Error.WriteLine(ex.StackTrace);
      }
    }

    public static bool AddImageMauHD(string fontPath, string fileName, int pageNumber, string pathIMG, string tenHD)
    {
      PdfReader reader = new PdfReader(fileName);
      try
      {
        string str1 = Application.StartupPath + "\\temp_create.pdf";
        string str2 = Application.StartupPath + "\\temp_create1.pdf";
        PdfStamper pdfStamper = new PdfStamper(reader, (Stream) new FileStream(str1, FileMode.Create));
        if (reader.NumberOfPages < pageNumber)
        {
          PDFUtil.AddNewPageWithText(fontPath, str2, fileName, tenHD);
          reader = new PdfReader(str2);
          pdfStamper.Close();
          pdfStamper = new PdfStamper(reader, (Stream) new FileStream(str1, FileMode.Create));
        }
        PdfContentByte overContent = pdfStamper.GetOverContent(pageNumber);
        float width = reader.GetPageSizeWithRotation(pageNumber).Width;
        float height = reader.GetPageSizeWithRotation(pageNumber).Height;
        float num1 = width - 100f;
        float num2 = height - 100f;
        Image instance = Image.GetInstance(pathIMG);
        float scaledWidth = instance.ScaledWidth;
        float scaledHeight = instance.ScaledHeight;
        float percent1 = 100f;
        float percent2 = 100f;
        if ((double) scaledWidth >= (double) num1)
          percent1 = 100f * num1 / scaledWidth;
        if ((double) scaledHeight >= (double) num2)
          percent2 = 100f * num2 / scaledHeight;
        if ((double) percent2 < (double) percent1)
          instance.ScalePercent(percent2);
        else
          instance.ScalePercent(percent1);
        if (tenHD.Length > 80)
          instance.SetAbsolutePosition(overContent.PdfDocument.PageSize.Left + 50f, height - 90f - instance.ScaledHeight);
        else
          instance.SetAbsolutePosition(overContent.PdfDocument.PageSize.Left + 50f, height - 60f - instance.ScaledHeight);
        overContent.AddImage(instance);
        overContent.ClosePath();
        pdfStamper.Close();
        reader.Close();
        File.Copy(str1, fileName, true);
        return true;
      }
      catch
      {
        return false;
      }
      finally
      {
      }
    }

    public static byte[] HtmToPdf(string htmlDisplayText)
    {
      Document document = new Document();
      MemoryStream memoryStream = new MemoryStream();
      PdfWriter.GetInstance(document, (Stream) memoryStream);
      StringReader stringReader = new StringReader(htmlDisplayText);
      HTMLWorker htmlWorker = new HTMLWorker((IDocListener) document);
      document.Open();
      htmlWorker.Parse((TextReader) stringReader);
      document.Close();
      byte[] buffer = memoryStream.GetBuffer();
      memoryStream.Close();
      return buffer;
    }
  }
}
