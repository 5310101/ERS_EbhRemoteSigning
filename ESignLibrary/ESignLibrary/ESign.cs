// Decompiled with JetBrains decompiler
// Type: ESignLibrary.ESign
// Assembly: ESignLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FC312E54-D344-41AE-A232-7B8768ED42AA
// Assembly location: D:\1. Project\EBH\EBH\Bin\ESignLibrary.dll

using ESignLibrary.Properties;
using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using iTextSharp.text.pdf.security;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.X509;
using SignLib2003;
using SignLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Packaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using XmlSign;

namespace ESignLibrary
{
    public class ESign
    {
        public static int openXmlSignIndex = 1;
        public static string fileLog = Application.StartupPath + "\\ESignErrorLog.txt";
        public static string PathOfFont = "";
        public static bool KyGiaLap = false;
        private static readonly string RT_OfficeDocument = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
        private static readonly string OfficeObjectID = "idOfficeObject";
        private static readonly string SignatureID = "idPackageSignature";
        private static readonly string ManifestHashAlgorithm = "http://www.w3.org/2000/09/xmldsig#sha1";
        private static System.Drawing.Rectangle rectParent = new System.Drawing.Rectangle();
        public static string Tittle = "Danh sách chứng thư số";
        public static DateTime currentDateTime;
        public static int count;
        public static IntPtr parentHandler;
        private static Timer timerCounter;
        private static Timer timer;
        private static CmsSigner signer;

        [DllImport("kernel32.dll", EntryPoint = "GetSystemTime", SetLastError = true)]
        public static extern void Win32GetSystemTime(ref ESign.SystemTime sysTime);

        [DllImport("kernel32.dll", EntryPoint = "SetSystemTime", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Win32SetSystemTime([In] ref ESign.SystemTime sysTime);

        [DllImport("user32.dll")]
        public static extern int FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern int GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(int hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out System.Drawing.Rectangle lpRect);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int cx, int cy, bool repaint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hwnd);

        private static void SetPositionSelectCertificateWindow()
        {
            try
            {
                StringBuilder text = new StringBuilder(1024);
                int foregroundWindow = ESign.GetForegroundWindow();
                IntPtr windowEx = ESign.FindWindowEx(ESign.parentHandler, IntPtr.Zero, (string)null, "");
                if (ESign.GetWindowText(foregroundWindow, text, 1024) <= 0 || windowEx.ToInt32() <= 0)
                    return;
                Console.WriteLine(text.ToString());
                System.Drawing.Rectangle lpRect = new System.Drawing.Rectangle();
                ESign.GetWindowRect(new IntPtr(foregroundWindow), out lpRect);
                int num1 = lpRect.Width / 2;
                int num2 = lpRect.Height / 2;
                IntPtr hWnd = new IntPtr(foregroundWindow);
                IntPtr hWndInsertAfter = new IntPtr(-1);
                Point location = ESign.rectParent.Location;
                int X = location.X + ESign.rectParent.Width / 2 - num1;
                location = ESign.rectParent.Location;
                int Y = location.Y + ESign.rectParent.Height / 2 - num2;
                int width = lpRect.Width;
                int height = lpRect.Height;
                int num3 = 65;
                ESign.SetWindowPos(hWnd, hWndInsertAfter, X, Y, width, height, (uint)num3);
                ESign.timer.Enabled = false;
            }
            catch
            {
                ESign.timer.Enabled = false;
            }
            finally
            {
            }
        }

        private static void timer_Tick(object sender, EventArgs e)
        {
            ESign.SetPositionSelectCertificateWindow();
        }

        public static X509Certificate2 GetCertificate(string fileName, string pass)
        {
            return new X509Certificate2(fileName, pass, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }

        public static X509Certificate2 GetCertificate(IntPtr parentFormHandler)
        {
            ESign.parentHandler = parentFormHandler;
            return ESign.GetCertificate(parentFormHandler, "Danh sách chứng thư số" + (ESign.KyGiaLap ? " - Chế độ khai thử" : ""), "Hãy chọn một chữ ký số" + (ESign.KyGiaLap ? " (chữ ký trong chế độ khai thử không có giá trị pháp lý):" : ":"));
        }

        public static X509Certificate2 GetCertificate(IntPtr parentFormHandler, string tittle, string message)
        {
            ESign.parentHandler = parentFormHandler;
            ESign.Tittle = tittle;
            int num = 0;
            StringBuilder stringBuilder = new StringBuilder(1024);
            num = ESign.GetForegroundWindow();
            stringBuilder.ToString();
            ESign.rectParent = new System.Drawing.Rectangle();
            ESign.GetWindowRect(parentFormHandler, out ESign.rectParent);
            ESign.timer = new Timer();
            ESign.timer = new Timer();
            ESign.timer.Interval = 1;
            ESign.timer.Tick += new EventHandler(ESign.timer_Tick);
            ESign.timer.Start();
            X509Store x509Store1 = new X509Store(StoreName.TrustedPeople, StoreLocation.CurrentUser);
            x509Store1.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certificates = x509Store1.Certificates;
            X509Store x509Store2 = new X509Store(StoreName.AddressBook, StoreLocation.CurrentUser);
            x509Store2.Open(OpenFlags.ReadOnly);
            certificates.AddRange(x509Store2.Certificates);
            X509Store x509Store3 = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            x509Store3.Open(OpenFlags.ReadOnly);
            certificates.AddRange(x509Store3.Certificates);
            X509Certificate2 x509Certificate2 = (X509Certificate2)null;
            X509Certificate2Collection certificate2Collection = X509Certificate2UI.SelectFromCollection(certificates, tittle, message, X509SelectionFlag.SingleSelection, parentFormHandler);
            if (certificate2Collection.Count > 0)
            {
                X509Certificate2Enumerator enumerator = certificate2Collection.GetEnumerator();
                enumerator.MoveNext();
                x509Certificate2 = enumerator.Current;
            }
            x509Store2.Close();
            x509Store3.Close();
            return x509Certificate2;
        }

        public static int NumberOfWindowCertificate()
        {
            X509Store x509Store1 = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            x509Store1.Open(OpenFlags.ReadOnly);
            int count = x509Store1.Certificates.Count;
            x509Store1.Close();
            X509Store x509Store2 = new X509Store(StoreName.AddressBook, StoreLocation.CurrentUser);
            x509Store2.Open(OpenFlags.ReadOnly);
            int num1 = count + x509Store2.Certificates.Count;
            x509Store2.Close();
            X509Store x509Store3 = new X509Store(StoreName.TrustedPeople, StoreLocation.CurrentUser);
            x509Store3.Open(OpenFlags.ReadOnly);
            int num2 = num1 + x509Store3.Certificates.Count;
            x509Store3.Close();
            return num2;
        }

        public static X509Certificate2 FindCertificate(StoreLocation location, StoreName name, X509FindType findType, string findValue)
        {
            X509Store x509Store = new X509Store(name, location);
            try
            {
                x509Store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certificate2Collection = x509Store.Certificates.Find(findType, (object)findValue, false);
                if (certificate2Collection.Count > 0)
                    return certificate2Collection[0];
                return (X509Certificate2)null;
            }
            finally
            {
                x509Store.Close();
            }
        }
        public static bool ETAXSign(X509Certificate2 card, string InputFile, string OutputFile)
        {
            PdfSignatureAppearance signatureAppearance1 = (PdfSignatureAppearance)null;
            Dictionary<PdfName, int> exclusionSizes = new Dictionary<PdfName, int>();
            PdfStamper pdfStamper = (PdfStamper)null;
            FileStream fileStream1 = new FileStream(InputFile, FileMode.Open, FileAccess.Read);
            FileStream fileStream2 = (FileStream)null;
            try
            {
                card = ESign.FindCertificate(StoreLocation.CurrentUser, StoreName.My, X509FindType.FindBySerialNumber, card.SerialNumber);
                if (card == null)
                {
                    ErrorLog.WriteToErrorLog(ESign.fileLog, "Không tìm thấy CKS", "", "");
                    return false;
                }
                Org.BouncyCastle.X509.X509Certificate[] certChain = new Org.BouncyCastle.X509.X509Certificate[1] { new X509CertificateParser().ReadCertificate(card.RawData) };
                byte[] numArray1 = new byte[fileStream1.Length];
                fileStream1.Read(numArray1, 0, (int)fileStream1.Length);
                fileStream1.Close();
                fileStream2 = new FileStream("temzxc.pdf", FileMode.Create);
                PdfReader reader = new PdfReader(numArray1);
                PdfSignatureAppearance signatureAppearance2 = PdfStamper.CreateSignature(reader, (Stream)fileStream2, char.MinValue, (string)null, true).SignatureAppearance;
                signatureAppearance2.SignDate = ESign.currentDateTime;
                string name = ESign.PathOfFont;
                if (name.Length == 0)
                    name = Application.StartupPath + "\\times.ttf";
                iTextSharp.text.Font font = new iTextSharp.text.Font(BaseFont.CreateFont(name, "Identity-H", false));
                font.SetColor((int)byte.MaxValue, 0, 0);
                signatureAppearance2.Layer2Font = font;
                signatureAppearance2.Layer2Text = !ESign.KyGiaLap ? "Ký bởi: " + PdfPKCS7.GetSubjectFields(certChain[0]).GetField("CN") + "\nKý ngày: " + string.Format("{0:d/M/yyyy HH:mm:ss}", (object)ESign.currentDateTime) : "Ký khai thử: " + PdfPKCS7.GetSubjectFields(certChain[0]).GetField("CN") + "\nKý ngày: " + string.Format("{0:d/M/yyyy HH:mm:ss}", (object)ESign.currentDateTime);
                signatureAppearance2.SetVisibleSignature(new iTextSharp.text.Rectangle(0.0f, 20f, 400f, 0.0f), reader.NumberOfPages, (string)null);
                signatureAppearance2.SetCrypto((ICipherParameters)null, certChain, (object[])null, PdfSignatureAppearance.WINCER_SIGNED);
                PdfSignature pdfSignature1 = new PdfSignature(PdfName.ADOBE_PPKMS, PdfName.ADBE_PKCS7_SHA1);
                pdfSignature1.Date = new PdfDate(signatureAppearance2.SignDate);
                pdfSignature1.Name = "";
                if (signatureAppearance2.Reason != null)
                    pdfSignature1.Reason = signatureAppearance2.Reason;
                if (signatureAppearance2.Location != null)
                    pdfSignature1.Location = signatureAppearance2.Location;
                signatureAppearance2.CryptoDictionary = (PdfDictionary)pdfSignature1;
                PdfSignature pdfSignature2 = (PdfSignature)null;
                int length = 4000;
                exclusionSizes[PdfName.CONTENTS] = length * 2 + 2;
                signatureAppearance2.PreClose(exclusionSizes);
                HashAlgorithm hashAlgorithm = (HashAlgorithm)new SHA1CryptoServiceProvider();
                Stream rangeStream = signatureAppearance2.GetRangeStream();
                byte[] numArray2 = new byte[8192];
                int inputCount;
                while ((inputCount = rangeStream.Read(numArray2, 0, 8192)) > 0)
                    hashAlgorithm.TransformBlock(numArray2, 0, inputCount, numArray2, 0);
                rangeStream.Close();
                rangeStream.Dispose();
                hashAlgorithm.TransformFinalBlock(numArray2, 0, 0);
                byte[] numArray3 = ESign.SignPass(hashAlgorithm.Hash, card, false);
                if (numArray3 != null)
                {
                    byte[] bytes = new byte[length];
                    PdfDictionary update = new PdfDictionary();
                    Array.Copy((Array)numArray3, 0, (Array)bytes, 0, numArray3.Length);
                    update.Put(PdfName.CONTENTS, (PdfObject)new PdfString(bytes).SetHexWriting(true));
                    signatureAppearance2.Close(update);
                    File.Copy("temzxc.pdf", OutputFile, true);
                    return true;
                }
                reader.Close();
                pdfStamper.Close();
                pdfStamper.Dispose();
                signatureAppearance1 = (PdfSignatureAppearance)null;
                pdfSignature2.Clear();
                return false;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Input: " + InputFile + "| Output: " + OutputFile);
                ex.ToString();
                signatureAppearance1 = (PdfSignatureAppearance)null;
                return false;
            }
            finally
            {
                fileStream1.Close();
                fileStream1.Dispose();
                if (fileStream2 != null)
                {
                    fileStream2.Close();
                    fileStream2.Dispose();
                }
                if (File.Exists("temzxc.pdf"))
                {
                    try
                    {
                        File.Delete("temzxc.pdf");
                    }
                    catch
                    {
                    }
                }
            }
        }
        public static bool ETAXSign(X509Certificate2 card, string InputFile, string OutputFile, int ViTriCKS = 2, int PageSign = 1)
        {
            PdfSignatureAppearance signatureAppearance1 = (PdfSignatureAppearance)null;
            Dictionary<PdfName, int> exclusionSizes = new Dictionary<PdfName, int>();
            PdfStamper pdfStamper = (PdfStamper)null;
            FileStream fileStream1 = new FileStream(InputFile, FileMode.Open, FileAccess.Read);
            FileStream fileStream2 = (FileStream)null;
            try
            {
                card = ESign.FindCertificate(StoreLocation.CurrentUser, StoreName.My, X509FindType.FindBySerialNumber, card.SerialNumber);
                if (card == null)
                {
                    ErrorLog.WriteToErrorLog(ESign.fileLog, "Không tìm thấy CKS", "", "");
                    return false;
                }
                Org.BouncyCastle.X509.X509Certificate[] certChain = new Org.BouncyCastle.X509.X509Certificate[1] { new X509CertificateParser().ReadCertificate(card.RawData) };
                byte[] numArray1 = new byte[fileStream1.Length];
                fileStream1.Read(numArray1, 0, (int)fileStream1.Length);
                fileStream1.Close();
                fileStream2 = new FileStream("temzxc.pdf", FileMode.Create);
                PdfReader reader = new PdfReader(numArray1);


                PdfSignatureAppearance signatureAppearance2 = PdfStamper.CreateSignature(reader, (Stream)fileStream2, char.MinValue, (string)null, true).SignatureAppearance;
                signatureAppearance2.SignDate = ESign.currentDateTime;
                string name = ESign.PathOfFont;
                if (name.Length == 0)
                    name = Application.StartupPath + "\\times.ttf";
                iTextSharp.text.Font font = new iTextSharp.text.Font(BaseFont.CreateFont(name, "Identity-H", false));
                font.Size = 12f;

                font.SetColor((int)byte.MaxValue, 0, 0);
                signatureAppearance2.Layer2Font = font;
                signatureAppearance2.Layer2Text = !ESign.KyGiaLap ? "Ký bởi: " + PdfPKCS7.GetSubjectFields(certChain[0]).GetField("CN") + "\nKý ngày: " + string.Format("{0:d/M/yyyy HH:mm:ss}", (object)ESign.currentDateTime) : "Ký khai thử: " + PdfPKCS7.GetSubjectFields(certChain[0]).GetField("CN") + "\nKý ngày: " + string.Format("{0:d/M/yyyy HH:mm:ss}", (object)ESign.currentDateTime);
                int PageIndex = 1;
                switch (PageSign)
                {
                    case -1:
                        PageIndex = -1;
                        break;
                    case 0:
                        PageIndex = 1;
                        break;
                    case 1:
                        PageIndex = reader.NumberOfPages;
                        break;
                    default:
                        break;
                }
                float pWidth = reader.GetPageSize(PageIndex).Width;
                float pHeight = reader.GetPageSize(PageIndex).Height;//reader.NumberOfPages
                if (pHeight > pWidth && reader.GetPageRotation(PageIndex) > 0)
                {
                    pWidth = reader.GetPageSize(PageIndex).Height;
                    pHeight = reader.GetPageSize(PageIndex).Width;
                }
                //signatureAppearance2.SignatureGraphic. =  0;
                //signatureAppearance2.SetVisibleSignature(new iTextSharp.text.Rectangle(0f, pHeight, 100f, pHeight - 20), reader.NumberOfPages, (string)null);
                //ViTriCKS = 3;
                switch (ViTriCKS)
                {
                    case 0:
                        signatureAppearance2.SetVisibleSignature(new iTextSharp.text.Rectangle(0f, pHeight, 200f, pHeight - 100f), PageIndex, (string)null);
                        break;
                    case 1:
                        signatureAppearance2.SetVisibleSignature(new iTextSharp.text.Rectangle(pWidth, pHeight, pWidth - 200f, pHeight - 100f), PageIndex, (string)null);
                        break;
                    case 2:
                        signatureAppearance2.SetVisibleSignature(new iTextSharp.text.Rectangle(0.0f, 100f, 200f, 0.0f), PageIndex, (string)null);
                        break;
                    case 3:
                        signatureAppearance2.SetVisibleSignature(new iTextSharp.text.Rectangle(pWidth, 100f, pWidth - 200f, 0.0f), PageIndex, (string)null);
                        break;
                    default:
                        signatureAppearance2.SetVisibleSignature(new iTextSharp.text.Rectangle(0.0f, 100f, 200f, 0.0f), PageIndex, (string)null);
                        break;
                }
                

                signatureAppearance2.SetCrypto((ICipherParameters)null, certChain, (object[])null, PdfSignatureAppearance.WINCER_SIGNED);
                PdfSignature pdfSignature1 = new PdfSignature(PdfName.ADOBE_PPKMS, PdfName.ADBE_PKCS7_SHA1);
                pdfSignature1.Date = new PdfDate(signatureAppearance2.SignDate);
                pdfSignature1.Name = "";
                if (signatureAppearance2.Reason != null)
                    pdfSignature1.Reason = signatureAppearance2.Reason;
                if (signatureAppearance2.Location != null)
                    pdfSignature1.Location = signatureAppearance2.Location;
                signatureAppearance2.CryptoDictionary = (PdfDictionary)pdfSignature1;
                PdfSignature pdfSignature2 = (PdfSignature)null;
                int length = 4000;
                exclusionSizes[PdfName.CONTENTS] = length * 2 + 2;
                signatureAppearance2.PreClose(exclusionSizes);
                HashAlgorithm hashAlgorithm = (HashAlgorithm)new SHA1CryptoServiceProvider();
                Stream rangeStream = signatureAppearance2.GetRangeStream();
                byte[] numArray2 = new byte[8192];
                int inputCount;
                while ((inputCount = rangeStream.Read(numArray2, 0, 8192)) > 0)
                    hashAlgorithm.TransformBlock(numArray2, 0, inputCount, numArray2, 0);
                rangeStream.Close();
                rangeStream.Dispose();
                hashAlgorithm.TransformFinalBlock(numArray2, 0, 0);
                byte[] numArray3 = ESign.SignPass(hashAlgorithm.Hash, card, false);
                if (numArray3 != null)
                {
                    byte[] bytes = new byte[length];
                    PdfDictionary update = new PdfDictionary();
                    Array.Copy((Array)numArray3, 0, (Array)bytes, 0, numArray3.Length);
                    update.Put(PdfName.CONTENTS, (PdfObject)new PdfString(bytes).SetHexWriting(true));
                    signatureAppearance2.Close(update);
                    File.Copy("temzxc.pdf", OutputFile, true);
                    return true;
                }
                reader.Close();
                pdfStamper.Close();
                pdfStamper.Dispose();
                signatureAppearance1 = (PdfSignatureAppearance)null;
                pdfSignature2.Clear();
                return false;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Input: " + InputFile + "| Output: " + OutputFile);
                ex.ToString();
                signatureAppearance1 = (PdfSignatureAppearance)null;
                return false;
            }
            finally
            {
                fileStream1.Close();
                fileStream1.Dispose();
                if (fileStream2 != null)
                {
                    fileStream2.Close();
                    fileStream2.Dispose();
                }
                if (File.Exists("temzxc.pdf"))
                {
                    try
                    {
                        File.Delete("temzxc.pdf");
                    }
                    catch
                    {
                    }
                }
            }
            //return false;
        }

        public struct RectPosition
        {
            public RectPosition(float lx, float ly, float rx, float ry)
            {
                this.lx = lx;
                this.ly = ly;
                this.rx = rx;
                this.ry = ry;
            }

            public float lx { get; set; }
            public float ly { get; set; }
            public float rx { get; set; }
            public float ry { get; set; }

        }


        public static bool SignAllPages(X509Certificate2 card, string InputFile, string OutputFile, int ViTriCKS = 2)
        {
            try
            {
                bool check = true;
                PdfReader reader = new PdfReader(InputFile);
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    check = SignEachPages(card, InputFile, OutputFile, i, ViTriCKS);
                    if (!check)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Input: " + InputFile + "| Output: " + OutputFile);
                ex.ToString();
                return false;
            }
        }

        public static bool SignEachPages(X509Certificate2 card, string InputFile, string OutputFile, int Page, int ViTriCKS = 2)
        {
            RectPosition rectangle;
            Dictionary<PdfName, int> exclusionSizes = new Dictionary<PdfName, int>();
            FileStream fileStream1 = new FileStream(InputFile, FileMode.Open, FileAccess.Read);
            try
            {
                Org.BouncyCastle.X509.X509Certificate[] certChain = new Org.BouncyCastle.X509.X509Certificate[1] { new X509CertificateParser().ReadCertificate(card.RawData) };
                byte[] numArray1 = new byte[fileStream1.Length];
                fileStream1.Read(numArray1, 0, (int)fileStream1.Length);
                fileStream1.Close();
                using (MemoryStream stream = new MemoryStream(numArray1))
                {
                    PdfReader reader = new PdfReader(stream);
                    float pWidth = reader.GetPageSize(Page).Width;
                    float pHeight = reader.GetPageSize(Page).Height;//reader.NumberOfPages
                    if (pHeight > pWidth && reader.GetPageRotation(Page) > 0)
                    {
                        pWidth = reader.GetPageSize(Page).Height;
                        pHeight = reader.GetPageSize(Page).Width;
                    }
                    switch (ViTriCKS)
                    {
                        case 0:
                            rectangle = new RectPosition(0f, pHeight, 200f, pHeight - 100f);
                            break;
                        case 1:
                            rectangle = new RectPosition(pWidth, pHeight, pWidth - 200f, pHeight - 100f);
                            break;
                        case 2:
                            rectangle = new RectPosition(0.0f, 100f, 200f, 0.0f);
                            break;
                        case 3:
                            rectangle = new RectPosition(pWidth, 100f, pWidth - 200f, 0.0f);
                            break;
                        default:
                            rectangle = new RectPosition(0.0f, 100f, 200f, 0.0f);
                            break;
                    }
                    using (MemoryStream outPutStream = new MemoryStream())
                    {
                        PdfStamper pdfStamper = PdfStamper.CreateSignature(reader, (Stream)outPutStream, char.MinValue, (string)null, true);
                        PdfSignatureAppearance signatureAppearance2 = pdfStamper.SignatureAppearance;
                        signatureAppearance2.SignDate = ESign.currentDateTime;
                        string name = ESign.PathOfFont;
                        if (name.Length == 0)
                            name = Application.StartupPath + "\\times.ttf";
                        iTextSharp.text.Font font = new iTextSharp.text.Font(BaseFont.CreateFont(name, "Identity-H", false));
                        font.Size = 12f;
                        font.SetColor((int)byte.MaxValue, 0, 0);
                        signatureAppearance2.Layer2Font = font;
                        signatureAppearance2.Layer2Text = !ESign.KyGiaLap ? "Ký bởi: " + PdfPKCS7.GetSubjectFields(certChain[0]).GetField("CN") + "\nKý ngày: " + string.Format("{0:d/M/yyyy HH:mm:ss}", (object)ESign.currentDateTime) : "Ký khai thử: " + PdfPKCS7.GetSubjectFields(certChain[0]).GetField("CN") + "\nKý ngày: " + string.Format("{0:d/M/yyyy HH:mm:ss}", (object)ESign.currentDateTime);
                        signatureAppearance2.SetVisibleSignature(new iTextSharp.text.Rectangle(rectangle.lx, rectangle.ly, rectangle.rx, rectangle.ry), Page, "signature" + Page);

                        signatureAppearance2.SetCrypto((ICipherParameters)null, certChain, (object[])null, PdfSignatureAppearance.WINCER_SIGNED);
                        PdfSignature pdfSignature1 = new PdfSignature(PdfName.ADOBE_PPKMS, PdfName.ADBE_PKCS7_SHA1);
                        pdfSignature1.Date = new PdfDate(signatureAppearance2.SignDate);
                        pdfSignature1.Name = "";
                        if (signatureAppearance2.Reason != null)
                            pdfSignature1.Reason = signatureAppearance2.Reason;
                        if (signatureAppearance2.Location != null)
                            pdfSignature1.Location = signatureAppearance2.Location;
                        signatureAppearance2.CryptoDictionary = (PdfDictionary)pdfSignature1;
                        PdfSignature pdfSignature2 = (PdfSignature)null;

                        byte[] numArray2 = new byte[8192];
                        int inputCount;
                        int length = 4000;
                        exclusionSizes[PdfName.CONTENTS] = length * 2 + 2;
                        signatureAppearance2.PreClose(exclusionSizes);
                        HashAlgorithm hashAlgorithm = (HashAlgorithm)new SHA1CryptoServiceProvider();
                        Stream rangeStream = signatureAppearance2.GetRangeStream();
                        while ((inputCount = rangeStream.Read(numArray2, 0, 8192)) > 0)
                            hashAlgorithm.TransformBlock(numArray2, 0, inputCount, numArray2, 0);
                        rangeStream.Close();
                        rangeStream.Dispose();
                        hashAlgorithm.TransformFinalBlock(numArray2, 0, 0);
                        byte[] numArray3 = ESign.SignPass(hashAlgorithm.Hash, card, false);
                        if (numArray3 != null)
                        {
                            byte[] bytes = new byte[length];
                            PdfDictionary update = new PdfDictionary();
                            Array.Copy((Array)numArray3, 0, (Array)bytes, 0, numArray3.Length);
                            update.Put(PdfName.CONTENTS, (PdfObject)new PdfString(bytes).SetHexWriting(true));
                            signatureAppearance2.Close(update);
                        }
                        else
                        {
                            reader.Close();
                            pdfStamper.Close();
                            pdfStamper.Dispose();
                            pdfSignature2.Clear();
                            return false;
                        }
                        pdfStamper.Close();
                        File.WriteAllBytes(OutputFile, outPutStream.ToArray());
                    }
                    reader.Close();
                    return true;
                }

            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Input: " + InputFile + "| Output: " + OutputFile);
                return false;
            }
        }

        public static bool ETAXSign(X509Certificate2 card, byte[] InputFile, ref byte[] Output)
        {
            if (card == null)
                return false;
            PdfSignatureAppearance signatureAppearance1 = (PdfSignatureAppearance)null;
            Dictionary<PdfName, int> exclusionSizes = new Dictionary<PdfName, int>();
            PdfStamper pdfStamper = (PdfStamper)null;
            try
            {
                card = ESign.FindCertificate(StoreLocation.CurrentUser, StoreName.My, X509FindType.FindBySerialNumber, card.SerialNumber);
                if (card == null)
                {
                    ErrorLog.WriteToErrorLog(ESign.fileLog, "Không tìm thấy CKS", "", "");
                    return false;
                }
                string str = ESign.PathOfFont;
                if (str.Length == 0)
                    str = Application.StartupPath + "\\times.ttf";
                ErrorLog.WriteToErrorLog(ESign.fileLog, str, "", "");
                string path = new FileInfo(str).DirectoryName + "\\temp.pdf";
                Org.BouncyCastle.X509.X509Certificate[] certChain = new Org.BouncyCastle.X509.X509Certificate[1] { new X509CertificateParser().ReadCertificate(card.RawData) };
                PdfSignatureAppearance signatureAppearance2 = PdfStamper.CreateSignature(new PdfReader(InputFile), (Stream)new FileStream(path, FileMode.Create), char.MinValue, (string)null, true).SignatureAppearance;
                signatureAppearance2.SignDate = ESign.currentDateTime;
                iTextSharp.text.Font font = new iTextSharp.text.Font(BaseFont.CreateFont(str, "Identity-H", false));
                font.SetColor((int)byte.MaxValue, 0, 0);
                signatureAppearance2.Layer2Font = font;
                signatureAppearance2.Layer2Text = "Ký bởi: " + PdfPKCS7.GetSubjectFields(certChain[0]).GetField("CN") + "\nKý ngày: " + string.Format("{0:d/M/yyyy HH:mm:ss}", (object)ESign.currentDateTime);
                signatureAppearance2.SetVisibleSignature(new iTextSharp.text.Rectangle(400f, 20f, 600f, 0.0f), 1, (string)null);
                signatureAppearance2.SetCrypto((ICipherParameters)null, certChain, (object[])null, PdfSignatureAppearance.WINCER_SIGNED);
                PdfSignature pdfSignature = new PdfSignature(PdfName.ADOBE_PPKMS, PdfName.ADBE_PKCS7_SHA1);
                pdfSignature.Date = new PdfDate(signatureAppearance2.SignDate);
                pdfSignature.Name = "";
                if (signatureAppearance2.Reason != null)
                    pdfSignature.Reason = signatureAppearance2.Reason;
                if (signatureAppearance2.Location != null)
                    pdfSignature.Location = signatureAppearance2.Location;
                signatureAppearance2.CryptoDictionary = (PdfDictionary)pdfSignature;
                int length = 4000;
                exclusionSizes[PdfName.CONTENTS] = length * 2 + 2;
                signatureAppearance2.PreClose(exclusionSizes);
                HashAlgorithm hashAlgorithm = (HashAlgorithm)new SHA1CryptoServiceProvider();
                Stream rangeStream = signatureAppearance2.GetRangeStream();
                byte[] numArray1 = new byte[8192];
                int inputCount;
                while ((inputCount = rangeStream.Read(numArray1, 0, 8192)) > 0)
                    hashAlgorithm.TransformBlock(numArray1, 0, inputCount, numArray1, 0);
                rangeStream.Close();
                hashAlgorithm.TransformFinalBlock(numArray1, 0, 0);
                byte[] numArray2 = ESign.SignPass(hashAlgorithm.Hash, card, false);
                if (numArray2 != null)
                {
                    byte[] bytes = new byte[length];
                    PdfDictionary update = new PdfDictionary();
                    Array.Copy((Array)numArray2, 0, (Array)bytes, 0, numArray2.Length);
                    update.Put(PdfName.CONTENTS, (PdfObject)new PdfString(bytes).SetHexWriting(true));
                    signatureAppearance2.Close(update);
                    FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read);
                    byte[] buffer = new byte[fileStream.Length];
                    fileStream.Read(buffer, 0, (int)fileStream.Length);
                    fileStream.Close();
                    Output = buffer;
                    return true;
                }
                pdfStamper = (PdfStamper)null;
                signatureAppearance1 = (PdfSignatureAppearance)null;
                ErrorLog.WriteToErrorLog(ESign.fileLog, "Error", "", "");
                return false;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi ");
                ex.ToString();
                pdfStamper = (PdfStamper)null;
                signatureAppearance1 = (PdfSignatureAppearance)null;
                return false;
            }
        }

        private static iTextSharp.text.Rectangle FindLocationText(PdfReader reader, string findtext)
        {
            try
            {
                var strategy = new MyStrategy();
                var ex = PdfTextExtractor.GetTextFromPage(reader, reader.NumberOfPages, strategy);
                if (strategy.myPoints != null && strategy.myPoints.Count > 0)
                {
                    var find = strategy.myPoints.FindLast(x => x.text == findtext);
                    if (find != null) return find.rect;
                }
                return new iTextSharp.text.Rectangle(0.0f, 20f, 400f, 0.0f);
            }
            catch
            {
                return new iTextSharp.text.Rectangle(0.0f, 20f, 400f, 0.0f);
            }
        }

        public static bool ETAXSign_KyHopDongBenABenB(X509Certificate2 card, string InputFile, string OutputFile, string findtext = "")
        {
            PdfSignatureAppearance signatureAppearance1 = (PdfSignatureAppearance)null;
            Dictionary<PdfName, int> exclusionSizes = new Dictionary<PdfName, int>();
            PdfStamper pdfStamper = (PdfStamper)null;
            FileStream fileStream1 = new FileStream(InputFile, FileMode.Open, FileAccess.Read);
            FileStream fileStream2 = (FileStream)null;
            try
            {
                card = ESign.FindCertificate(StoreLocation.CurrentUser, StoreName.My, X509FindType.FindBySerialNumber, card.SerialNumber);
                if (card == null)
                {
                    ErrorLog.WriteToErrorLog(ESign.fileLog, "Không tìm thấy CKS", "", "");
                    return false;
                }
                Org.BouncyCastle.X509.X509Certificate[] certChain = new Org.BouncyCastle.X509.X509Certificate[1] { new X509CertificateParser().ReadCertificate(card.RawData) };
                byte[] numArray1 = new byte[fileStream1.Length];
                fileStream1.Read(numArray1, 0, (int)fileStream1.Length);
                fileStream1.Close();
                fileStream2 = new FileStream("temzxc.pdf", FileMode.Create);
                PdfReader reader = new PdfReader(numArray1);
                PdfSignatureAppearance signatureAppearance2 = PdfStamper.CreateSignature(reader, (Stream)fileStream2, char.MinValue, (string)null, true).SignatureAppearance;
                signatureAppearance2.SignDate = ESign.currentDateTime;
                string name = ESign.PathOfFont;
                if (name.Length == 0)
                    name = Application.StartupPath + "\\times.ttf";
                iTextSharp.text.Font font = new iTextSharp.text.Font(BaseFont.CreateFont(name, "Identity-H", false));
                font.SetColor((int)byte.MaxValue, 0, 0);

                //get position text
                iTextSharp.text.Rectangle rect = new iTextSharp.text.Rectangle(0.0f, 20f, 400f, 0.0f);
                if (!string.IsNullOrEmpty(findtext))
                {
                    rect = FindLocationText(reader, findtext);
                }
                //end

                signatureAppearance2.Layer2Font = font;
                signatureAppearance2.Layer2Text = !ESign.KyGiaLap ? "Ký bởi: " + PdfPKCS7.GetSubjectFields(certChain[0]).GetField("CN") + "\nKý ngày: " + string.Format("{0:d/M/yyyy HH:mm:ss}", (object)ESign.currentDateTime) : "Ký khai thử: " + PdfPKCS7.GetSubjectFields(certChain[0]).GetField("CN") + "\nKý ngày: " + string.Format("{0:d/M/yyyy HH:mm:ss}", (object)ESign.currentDateTime);

                //signatureAppearance2.SetVisibleSignature(new iTextSharp.text.Rectangle(0.0f, 20f, 400f, 0.0f), 1, (string)null);
                if (rect.Left > 0)
                {
                    signatureAppearance2.SetVisibleSignature(new iTextSharp.text.Rectangle(rect.Left, rect.Bottom - 70, rect.Right, rect.Top - 30), reader.NumberOfPages, (string)null);
                }
                else
                {
                    signatureAppearance2.SetVisibleSignature(new iTextSharp.text.Rectangle(0.0f, 20f, 400f, 0.0f), 1, (string)null);
                }

                signatureAppearance2.SetCrypto((ICipherParameters)null, certChain, (object[])null, PdfSignatureAppearance.WINCER_SIGNED);
                PdfSignature pdfSignature1 = new PdfSignature(PdfName.ADOBE_PPKMS, PdfName.ADBE_PKCS7_SHA1);
                pdfSignature1.Date = new PdfDate(signatureAppearance2.SignDate);
                pdfSignature1.Name = "";
                if (signatureAppearance2.Reason != null)
                    pdfSignature1.Reason = signatureAppearance2.Reason;
                if (signatureAppearance2.Location != null)
                    pdfSignature1.Location = signatureAppearance2.Location;
                signatureAppearance2.CryptoDictionary = (PdfDictionary)pdfSignature1;
                PdfSignature pdfSignature2 = (PdfSignature)null;
                int length = 4000;
                exclusionSizes[PdfName.CONTENTS] = length * 2 + 2;
                signatureAppearance2.PreClose(exclusionSizes);
                HashAlgorithm hashAlgorithm = (HashAlgorithm)new SHA1CryptoServiceProvider();
                Stream rangeStream = signatureAppearance2.GetRangeStream();
                byte[] numArray2 = new byte[8192];
                int inputCount;
                while ((inputCount = rangeStream.Read(numArray2, 0, 8192)) > 0)
                    hashAlgorithm.TransformBlock(numArray2, 0, inputCount, numArray2, 0);
                rangeStream.Close();
                rangeStream.Dispose();
                hashAlgorithm.TransformFinalBlock(numArray2, 0, 0);
                byte[] numArray3 = ESign.SignPass(hashAlgorithm.Hash, card, false);
                if (numArray3 != null)
                {
                    byte[] bytes = new byte[length];
                    PdfDictionary update = new PdfDictionary();
                    Array.Copy((Array)numArray3, 0, (Array)bytes, 0, numArray3.Length);
                    update.Put(PdfName.CONTENTS, (PdfObject)new PdfString(bytes).SetHexWriting(true));
                    signatureAppearance2.Close(update);
                    File.Copy("temzxc.pdf", OutputFile, true);
                    return true;
                }
                reader.Close();
                pdfStamper.Close();
                pdfStamper.Dispose();
                signatureAppearance1 = (PdfSignatureAppearance)null;
                pdfSignature2.Clear();
                return false;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Input: " + InputFile + "| Output: " + OutputFile);
                ex.ToString();
                signatureAppearance1 = (PdfSignatureAppearance)null;
                return false;
            }
            finally
            {
                fileStream1.Close();
                fileStream1.Dispose();
                if (fileStream2 != null)
                {
                    fileStream2.Close();
                    fileStream2.Dispose();
                }
                if (File.Exists("temzxc.pdf"))
                {
                    try
                    {
                        File.Delete("temzxc.pdf");
                    }
                    catch
                    {
                    }
                }
            }
        }

        public static byte[] SignPass(byte[] msg, X509Certificate2 X509Cert2, bool detached)
        {
            SignedCms signedCms1 = new SignedCms(new ContentInfo(msg), detached);
            ContentInfo contentInfo;
            SignedCms signedCms2;
            try
            {
                bool flag = true;
                ESign.signer = new CmsSigner(X509Cert2);
                ESign.signer.IncludeOption = X509IncludeOption.EndCertOnly;
                flag = true;
                signedCms1.ComputeSignature(ESign.signer, false);
                return signedCms1.Encode();
            }
            catch (ArgumentNullException ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi ");
                return (byte[])null;
            }
            catch (CryptographicException ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi ");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi ");
                return (byte[])null;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi ");
                contentInfo = (ContentInfo)null;
                signedCms2 = (SignedCms)null;
                return (byte[])null;
            }
            finally
            {
                contentInfo = (ContentInfo)null;
                signedCms2 = (SignedCms)null;
            }
        }

        public static bool SignOfficeDocument(string path, X509Certificate2 certificate)
        {
            ESign.count = 0;
            ESign.timerCounter = new Timer();
            ESign.timerCounter.Interval = 1;
            ESign.timerCounter.Tick += new EventHandler(ESign.timerCounter_Tick);
            ESign.timerCounter.Enabled = true;
            DateTime universalTime1 = ESign.currentDateTime.ToUniversalTime();
            ESign.SystemTime sysTime = new ESign.SystemTime();
            sysTime.Year = (ushort)universalTime1.Year;
            sysTime.Month = (ushort)universalTime1.Month;
            sysTime.DayOfWeek = (ushort)universalTime1.DayOfWeek;
            sysTime.Day = (ushort)universalTime1.Day;
            sysTime.Hour = (ushort)universalTime1.Hour;
            sysTime.Minute = (ushort)universalTime1.Minute;
            sysTime.Second = (ushort)universalTime1.Second;
            sysTime.Millisecond = (ushort)universalTime1.Millisecond;
            DateTime universalTime2 = DateTime.Now.ToUniversalTime();
            try
            {
                ESign.Win32SetSystemTime(ref sysTime);
                ESign.timerCounter.Start();
                SignDocument.InitLicenseKey();
                return new SignDocument(certificate).Sign(path, true);
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi ");
                return false;
            }
            finally
            {
                ESign.timerCounter.Enabled = false;
                universalTime2.AddMilliseconds((double)ESign.count);
                sysTime.Year = (ushort)universalTime2.Year;
                sysTime.Month = (ushort)universalTime2.Month;
                sysTime.DayOfWeek = (ushort)universalTime2.DayOfWeek;
                sysTime.Day = (ushort)universalTime2.Day;
                sysTime.Hour = (ushort)universalTime2.Hour;
                sysTime.Minute = (ushort)universalTime2.Minute;
                sysTime.Second = (ushort)universalTime2.Second;
                sysTime.Millisecond = (ushort)ESign.currentDateTime.Millisecond;
                ESign.Win32SetSystemTime(ref sysTime);
            }
        }

        public static bool RemoveSignOfOfficeDocument(string path)
        {
            try
            {
                using (Package package = Package.Open(path))
                {
                    new PackageDigitalSignatureManager(package)
                    {
                        CertificateOption = CertificateEmbeddingOption.InSignaturePart
                    }.RemoveAllSignatures();
                    package.Flush();
                }
                return true;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "RemoveSignOfOfficeDocument ");
                return false;
            }
            finally
            {
            }
        }

        public static bool SignXmlDocumentDKTvan(string path, X509Certificate2 certificate, string sendType)
        {
            try
            {
                XmlDocument document = new XmlDocument();
                document.Load(path);
                string xmlSigned = "";
                if (clsXmlSignature.Sign_EnvelopedDKTVAN(document, ref xmlSigned, (RSA)certificate.PrivateKey, certificate).Length != 0)
                    return false;
                document.InnerXml = xmlSigned;
                document.Save(path);
                return true;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi ky file XML");
                return false;
            }
        }

        public static bool SignXmlDocument(string path, X509Certificate2 certificate)
        {
            try
            {
                XmlDocument document = new XmlDocument();
                document.Load(path);
                string xmlSigned = "";
                if (clsXmlSignature.Sign_Enveloped(document, ref xmlSigned, (RSA)certificate.PrivateKey, certificate).Length != 0)
                    return false;
                document.InnerXml = xmlSigned;
                document.Save(path);
                return true;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi ky file XML");
                return false;
            }
        }

        public static bool SignXmlDocument(byte[] input, X509Certificate2 certificate, ref byte[] output)
        {
            try
            {
                string filename = Application.StartupPath + "\\temp.xml";
                string str = Application.StartupPath + "\\tempOut.xml";
                File.WriteAllBytes(Application.StartupPath + "\\temp.xml", input);
                XmlDocument document = new XmlDocument();
                document.Load(filename);
                string xmlSigned = "";
                string stkTrace = clsXmlSignature.Sign_Enveloped(document, ref xmlSigned, (RSA)certificate.PrivateKey, certificate);
                if (stkTrace.Length == 0)
                {
                    document.InnerXml = xmlSigned;
                    document.Save(str);
                    output = File.ReadAllBytes(str);
                    return true;
                }
                ErrorLog.WriteToErrorLog(ESign.fileLog, "", stkTrace, "Loi ky file XML");
                return false;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi ky file XML");
                return false;
            }
        }

        public static bool SignXmlDocument_BH(string path, X509Certificate2 certificate, bool isSHA256 = false)
        {
            return ESign.SignXmlDocument_BH(path, certificate, "CKYDTU_DVI", "BaoHiemDienTu/CKyDTu", isSHA256);
        }

        public static bool SignXmlDocument_BH(string path, X509Certificate2 certificate, string nodeKy, string nodeStart, bool isSHA256 = false)
        {
            try
            {
                string sigId = "sigid";
                string sigIdProperty = "proid";
                XmlDocument document = new XmlDocument();
                document.Load(path);
                string xmlSigned = "";

                if (isSHA256)
                {
                    CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA256SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
                }

                if (clsXmlSignature.Sign_Enveloped_BH(document, ref xmlSigned, certificate, nodeKy, sigId, sigIdProperty, nodeStart, isSHA256).Length != 0)
                    return false;
                document.InnerXml = xmlSigned;
                document.Save(path);
                return true;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + path);
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi ky file XML");
                return false;
            }
        }

        public static bool SignXmlDocument_BH(byte[] input, X509Certificate2 certificate, ref byte[] output, bool isSHA256 = false)
        {
            try
            {
                string nodeKy = "CKYDTU_DVI";
                string sigId = "sigid";
                string sigIdProperty = "proid";
                string nodeStart = "BaoHiemDienTu/CKyDTu";
                string filename = Application.StartupPath + "\\temp.xml";
                string str = Application.StartupPath + "\\tempOut.xml";
                File.WriteAllBytes(Application.StartupPath + "\\temp.xml", input);
                XmlDocument document = new XmlDocument();
                document.Load(filename);
                string xmlSigned = "";

                if (isSHA256)
                {
                    CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA256SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
                }
                string stkTrace = clsXmlSignature.Sign_Enveloped_BH(document, ref xmlSigned, certificate, nodeKy, sigId, sigIdProperty, nodeStart, isSHA256);
                if (stkTrace.Length == 0)
                {
                    document.InnerXml = xmlSigned;
                    document.Save(str);
                    output = File.ReadAllBytes(str);
                    return true;
                }
                ErrorLog.WriteToErrorLog(ESign.fileLog, "", stkTrace, "Loi ky file XML");
                return false;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi ky file XML");
                return false;
            }
        }

        public static bool SignXmlDocument_BH_HN(string path, X509Certificate2 certificate, bool isSHA256 = false)
        {
            return ESign.SignXmlDocument_BH_HN(path, certificate, "CKYDTU_DVI", "Envelope/Content", isSHA256);
        }

        public static bool SignXmlDocument_BH_HN(string path, X509Certificate2 certificate, string nodeKy, string nodeStart, bool isSHA256 = false)
        {
            try
            {
                string sigId = "sigid";
                string sigIdProperty = "sigid";
                XmlDocument document = new XmlDocument();
                document.Load(path);
                string xmlSigned = "";
                if (isSHA256)
                {
                    CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA256SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
                }
                if (clsXmlSignature.Sign_Enveloped_BH_HN(document, ref xmlSigned, certificate, nodeKy, sigId, sigIdProperty, nodeStart, isSHA256).Length != 0)
                    return false;
                document.InnerXml = xmlSigned;
                document.Save(path);
                return true;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi ky file XML");
                return false;
            }
        }

        public static bool SignXmlDocument_BH_HN(byte[] input, X509Certificate2 certificate, ref byte[] output, bool isSHA256 = false)
        {
            try
            {
                string nodeKy = "CKyDTu";
                string sigId = "sigid";
                string sigIdProperty = "sigid";
                string nodeStart = "Envelope";
                string filename = Application.StartupPath + "\\temp.xml";
                string str = Application.StartupPath + "\\tempOut.xml";
                File.WriteAllBytes(Application.StartupPath + "\\temp.xml", input);
                XmlDocument document = new XmlDocument();
                document.Load(filename);
                string xmlSigned = "";
                if (isSHA256)
                {
                    CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA256SignatureDescription), "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
                }
                string stkTrace = clsXmlSignature.Sign_Enveloped_BH_HN(document, ref xmlSigned, certificate, nodeKy, sigId, sigIdProperty, nodeStart, isSHA256);
                if (stkTrace.Length == 0)
                {
                    document.InnerXml = xmlSigned;
                    document.Save(str);
                    output = File.ReadAllBytes(str);
                    return true;
                }
                ErrorLog.WriteToErrorLog(ESign.fileLog, "", stkTrace, "Loi ky file XML");
                return false;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi ky file XML");
                return false;
            }
        }

        //public static bool SignXmlDocument_NTDT(byte[] input, X509Certificate2 certificate, ref byte[] output, string signPath, string xPath, bool isNNT)
        //{
        //  try
        //  {
        //    string filename = Application.StartupPath + "\\temp.xml";
        //    string path = Application.StartupPath + "\\tempOut.xml";
        //    File.WriteAllBytes(Application.StartupPath + "\\temp.xml", input);
        //    XmlDocument document = new XmlDocument();
        //    document.PreserveWhitespace = true;
        //    document.Load(filename);
        //    string XmlSigFileName = "";
        //    string stkTrace = !isNNT ? clsXmlSignature.Sign_Enveloped_NTDT(document, ref XmlSigFileName, (RSA) certificate.PrivateKey, certificate, signPath, xPath) : clsXmlSignature.Sign_Enveloped_NTDT_NNT(document, ref XmlSigFileName, (RSA) certificate.PrivateKey, certificate, signPath, xPath);
        //    if (stkTrace.Length == 0)
        //    {
        //      File.WriteAllText(path, XmlSigFileName, Encoding.UTF8);
        //      output = File.ReadAllBytes(path);
        //      return true;
        //    }
        //    ErrorLog.WriteToErrorLog(ESign.fileLog, "", stkTrace, "Loi ky file XML NTDT");
        //    return false;
        //  }
        //  catch (Exception ex)
        //  {
        //    ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi ky file XML");
        //    return false;
        //  }
        //}

        private static void timerCounter_Tick(object sender, EventArgs e)
        {
            ++ESign.count;
        }

        public static bool SignOfficeDocument(byte[] InputFile, X509Certificate2 certificate, ref byte[] Output)
        {
            try
            {
                File.WriteAllBytes(Application.StartupPath + "\\temp.xlsx", InputFile);
                ESign.SignOfficeDocument(Application.StartupPath + "\\temp.xlsx", certificate);
                Output = File.ReadAllBytes(Application.StartupPath + "\\temp.xlsx");
                return true;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi ");
                return false;
            }
        }

        public static bool isSignedExcel(string filePath)
        {
            try
            {
                using (Package package = Package.Open(filePath))
                {
                    PackageDigitalSignatureManager signatureManager = new PackageDigitalSignatureManager(package);
                    if (signatureManager.Signatures.Count > 0 && DateTime.Now > DateTime.Parse(signatureManager.Signatures[0].Signer.GetExpirationDateString()))
                        return false;
                    return signatureManager.Signatures.Count >= 2;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static string isSignedExcel1(string filePath, string mst)
        {
            try
            {
                using (Package package = Package.Open(filePath))
                {
                    PackageDigitalSignatureManager signatureManager = new PackageDigitalSignatureManager(package);
                    if (signatureManager.Signatures.Count > 0 && DateTime.Now > DateTime.Parse(signatureManager.Signatures[0].Signer.GetExpirationDateString()))
                        return "Chữ ký hết hạn sử dụng";
                    if (signatureManager.Signatures.Count < 2)
                        return "Không đủ 2 chữ ký số";
                    if (!signatureManager.Signatures[0].Signer.Subject.Replace("-", "").Contains(mst.Replace("-", "")))
                        return "Chữ ký số không phải của doanh nghiệp " + mst;
                    return "";
                }
            }
            catch (Exception ex)
            {
                return "Lỗi xác thực chữ ký số";
            }
        }

        public static DateTime GetDateSignedExcel(string filePath)
        {
            try
            {
                using (Package package = Package.Open(filePath))
                {
                    PackageDigitalSignatureManager signatureManager = new PackageDigitalSignatureManager(package);
                    if (signatureManager.Signatures.Count > 0)
                        return signatureManager.Signatures[signatureManager.Signatures.Count - 1].SigningTime;
                    return DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                return DateTime.Now;
            }
        }

        public static string GetSerialFromSignedExcel(string filePath)
        {
            try
            {
                using (Package package = Package.Open(filePath))
                {
                    PackageDigitalSignatureManager signatureManager = new PackageDigitalSignatureManager(package);
                    if (signatureManager.Signatures.Count > 0)
                        return signatureManager.Signatures[signatureManager.Signatures.Count - 1].Signer.GetSerialNumberString();
                    return "";
                }
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public static System.Security.Cryptography.X509Certificates.X509Certificate GetCerFromSignedExcel(string filePath)
        {
            try
            {
                using (Package package = Package.Open(filePath))
                {
                    PackageDigitalSignatureManager signatureManager = new PackageDigitalSignatureManager(package);
                    if (signatureManager.Signatures.Count > 0)
                        return signatureManager.Signatures[signatureManager.Signatures.Count - 1].Signer;
                    return (System.Security.Cryptography.X509Certificates.X509Certificate)null;
                }
            }
            catch (Exception ex)
            {
                return (System.Security.Cryptography.X509Certificates.X509Certificate)null;
            }
        }

        public static string GetLastSignSerialFromPdf(object pdf)
        {
            PdfReader pdfReader = (PdfReader)null;
            System.Security.Cryptography.Xml.Signature[] signatureArray = new System.Security.Cryptography.Xml.Signature[0];
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
                signatureArray = new System.Security.Cryptography.Xml.Signature[0];
                return "";
            }
            finally
            {
                if (pdfReader != null)
                    pdfReader.Close();
            }
        }

        public static string GetLastCertFromPdf(object pdf)
        {
            PdfReader pdfReader = (PdfReader)null;
            System.Security.Cryptography.Xml.Signature[] signatureArray = new System.Security.Cryptography.Xml.Signature[0];
            try
            {
                if (pdf.GetType().ToString().Equals("System.String"))
                    pdfReader = new PdfReader(pdf.ToString());
                else if (pdf.GetType().ToString().Equals("System.Byte[]"))
                    pdfReader = new PdfReader(pdf as byte[]);
                AcroFields acroFields = pdfReader.AcroFields;
                List<string> signatureNames = acroFields.GetSignatureNames();
                ErrorLog.WriteToErrorLog(ESign.fileLog, signatureNames.Count.ToString(), "So cks", "Loi xac thuc ");
                if (signatureNames.Count < 2)
                    return "File phải có đủ 2 chữ ký số";
                string name = signatureNames[signatureNames.Count - 2];
                PdfPKCS7 pdfPkcS7 = acroFields.VerifySignature(name);
                if (!pdfPkcS7.Verify())
                    return "Không xác thực được chữ ký";
                Org.BouncyCastle.X509.X509Certificate[] certificates = pdfPkcS7.Certificates;
                for (int index = 0; index < certificates.Length; ++index)
                {
                    if (certificates[index].SubjectDN.GetValues().Count <= 4)
                        ;
                    if (certificates[index].NotAfter < DateTime.Now | certificates[index].NotBefore > DateTime.Now)
                        return "Chữ ký số hết hạn sử dụng";
                }
                pdfPkcS7.SigningCertificate.SubjectDN.GetValues();
                return "";
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi xac thuc ");
                signatureArray = new System.Security.Cryptography.Xml.Signature[0];
                return "Lỗi xác thực";
            }
            finally
            {
                if (pdfReader != null)
                    pdfReader.Close();
            }
        }

        public static string GetLastCertFromPdf(object pdf, string mst)
        {
            PdfReader pdfReader = (PdfReader)null;
            System.Security.Cryptography.Xml.Signature[] signatureArray = new System.Security.Cryptography.Xml.Signature[0];
            try
            {
                if (pdf.GetType().ToString().Equals("System.String"))
                    pdfReader = new PdfReader(pdf.ToString());
                else if (pdf.GetType().ToString().Equals("System.Byte[]"))
                    pdfReader = new PdfReader(pdf as byte[]);
                AcroFields acroFields = pdfReader.AcroFields;
                List<string> signatureNames = acroFields.GetSignatureNames();
                ErrorLog.WriteToErrorLog(ESign.fileLog, signatureNames.Count.ToString(), "So cks", "Loi xac thuc ");
                if (signatureNames.Count < 2)
                    return "File phải có đủ 2 chữ ký số";
                string name = signatureNames[signatureNames.Count - 2];
                PdfPKCS7 pdfPkcS7 = acroFields.VerifySignature(name);
                if (!pdfPkcS7.Verify())
                    return "Không xác thực được chữ ký";
                Org.BouncyCastle.X509.X509Certificate[] certificates = pdfPkcS7.Certificates;
                for (int index1 = 0; index1 < certificates.Length; ++index1)
                {
                    ArrayList values = certificates[index1].SubjectDN.GetValues();
                    if (index1 == 0 && values.Count > 4)
                    {
                        int index2 = 0;
                        while (index2 < values.Count && !values[index2].ToString().Replace("-", "").Contains(mst.Replace("-", "")))
                            ++index2;
                        if (index2 == values.Count)
                            return "Chữ ký số không phải của doanh nghiệp " + mst;
                    }
                    if (certificates[index1].NotAfter < DateTime.Now | certificates[index1].NotBefore > DateTime.Now)
                        return "Chữ ký số hết hạn sử dụng";
                }
                pdfPkcS7.SigningCertificate.SubjectDN.GetValues();
                return "";
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi xac thuc ");
                signatureArray = new System.Security.Cryptography.Xml.Signature[0];
                return "Lỗi xác thực";
            }
            finally
            {
                if (pdfReader != null)
                    pdfReader.Close();
            }
        }

        public static bool isVerifySignedExcel(string filePath)
        {
            try
            {
                using (Package package = Package.Open(filePath))
                {
                    PackageDigitalSignatureManager signatureManager = new PackageDigitalSignatureManager(package);
                    if (signatureManager.IsSigned)
                        return signatureManager.Signatures[0].Verify() == VerifyResult.Success;
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static string GetMSTFromSignature(string filePath)
        {
            try
            {
                using (Package package = Package.Open(filePath))
                {
                    PackageDigitalSignatureManager signatureManager = new PackageDigitalSignatureManager(package);
                    if (!signatureManager.IsSigned)
                        return "";
                    string subject = signatureManager.Signatures[0].Signer.Subject;
                    int startIndex = subject.IndexOf("MST");
                    return subject.Substring(startIndex, subject.IndexOf(",", startIndex) - startIndex).Replace("MST:", "");
                }
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        private static void SignPackage(Package package, X509Certificate2 certificate)
        {
            List<Uri> partsToSign = new List<Uri>();
            List<PackageRelationshipSelector> relationshipsToSign = new List<PackageRelationshipSelector>();
            List<Uri> uriList = new List<Uri>();
            string signPartGUID = "";
            PackagePart partSign = (PackagePart)null;
            foreach (PackageRelationship relationship in package.GetRelationshipsByType(ESign.RT_OfficeDocument))
                ESign.AddSignableItems(relationship, partsToSign, relationshipsToSign, ref partSign);
            string name = certificate.SubjectName.Name;
            if (partSign != null)
            {
                signPartGUID = ESign.GetSignID(partSign, ESign.openXmlSignIndex);
                ESign.SetSuggestedSigneForSignPart(partSign, ESign.getSignName(name), ESign.openXmlSignIndex);
            }
            PackageDigitalSignatureManager signatureManager = new PackageDigitalSignatureManager(package);
            signatureManager.CertificateOption = CertificateEmbeddingOption.InSignaturePart;
            signatureManager.RemoveAllSignatures();
            package.Flush();
            string signatureId = ESign.SignatureID;
            string manifestHashAlgorithm = ESign.ManifestHashAlgorithm;
            string subject = "Ký ngày " + DateTime.Now.ToString("dd/MM/yyyy");
            System.Security.Cryptography.Xml.DataObject officeObject = ESign.CreateOfficeObject(signatureId, manifestHashAlgorithm, subject, signPartGUID);
            Reference reference = new Reference("#" + ESign.OfficeObjectID);
            signatureManager.Sign((IEnumerable<Uri>)partsToSign, (System.Security.Cryptography.X509Certificates.X509Certificate)certificate, (IEnumerable<PackageRelationshipSelector>)relationshipsToSign, signatureId, (IEnumerable<System.Security.Cryptography.Xml.DataObject>)new System.Security.Cryptography.Xml.DataObject[1]
      {
        officeObject
      }, (IEnumerable<Reference>)new Reference[1]
      {
        reference
      });
        }

        private static string getSignName(string name)
        {
            try
            {
                int startIndex = name.IndexOf("CN=");
                if (startIndex < 0)
                    return "";
                if (name.IndexOf(",", startIndex) > 0)
                    return name.Substring(startIndex + 3, name.IndexOf(",", startIndex) - startIndex - 3);
                return name.Substring(startIndex + 3);
            }
            catch
            {
                return "";
            }
        }

        private static void AddSignableItems(PackageRelationship relationship, List<Uri> partsToSign, List<PackageRelationshipSelector> relationshipsToSign, ref PackagePart partSign)
        {
            PackageRelationshipSelector relationshipSelector = new PackageRelationshipSelector(relationship.SourceUri, PackageRelationshipSelectorType.Id, relationship.Id);
            relationshipsToSign.Add(relationshipSelector);
            if (relationship.TargetMode != TargetMode.Internal)
                return;
            PackagePart part = relationship.Package.GetPart(PackUriHelper.ResolvePartUri(relationship.SourceUri, relationship.TargetUri));
            if (part.Uri.Equals((object)"/xl/drawings/vmlDrawing1.vml"))
                partSign = part;
            if (!partsToSign.Contains(part.Uri))
            {
                partsToSign.Add(part.Uri);
                foreach (PackageRelationship relationship1 in part.GetRelationships())
                    ESign.AddSignableItems(relationship1, partsToSign, relationshipsToSign, ref partSign);
            }
        }

        private static string GetSignID(PackagePart part, int signIndex)
        {
            try
            {
                string msg = "";
                if (part.Uri.Equals((object)"/xl/drawings/vmlDrawing1.vml"))
                {
                    Stream stream = part.GetStream();
                    byte[] numArray = new byte[(int)stream.Length];
                    stream.Read(numArray, 0, (int)stream.Length);
                    string xml = Encoding.UTF8.GetString(numArray);
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(xml);
                    XmlNode firstChild = xmlDocument.FirstChild;
                    int num = 0;
                    foreach (XmlNode childNode in firstChild.ChildNodes)
                    {
                        if (childNode.Name.Equals("v:shape"))
                            ++num;
                        if (num == signIndex)
                        {
                            IEnumerator enumerator = childNode.ChildNodes.GetEnumerator();
                            try
                            {
                                while (enumerator.MoveNext())
                                {
                                    XmlNode current = (XmlNode)enumerator.Current;
                                    if (current.Name.Equals("o:signatureline"))
                                    {
                                        msg = current.Attributes.GetNamedItem("id").InnerText;
                                        ErrorLog.WriteToErrorLog(ESign.fileLog, msg, "", "ID:");
                                        break;
                                    }
                                }
                                break;
                            }
                            finally
                            {
                                IDisposable disposable = enumerator as IDisposable;
                                if (disposable != null)
                                    disposable.Dispose();
                            }
                        }
                    }
                    return msg;
                }
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi GetSignID:");
            }
            return "";
        }

        private static void SetSuggestedSigneForSignPart(PackagePart part, string value, int signIndex)
        {
            if (signIndex >= 2)
                return;
            try
            {
                if (part.Uri.Equals((object)"/xl/drawings/vmlDrawing1.vml"))
                {
                    Stream stream = part.GetStream();
                    byte[] numArray = new byte[(int)stream.Length];
                    stream.Read(numArray, 0, (int)stream.Length);
                    string xml = Encoding.UTF8.GetString(numArray);
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(xml);
                    XmlNode firstChild = xmlDocument.FirstChild;
                    int num1 = 0;
                    foreach (XmlNode childNode in firstChild.ChildNodes)
                    {
                        if (childNode.Name.Equals("v:shape"))
                            ++num1;
                        if (num1 == signIndex)
                        {
                            IEnumerator enumerator = childNode.ChildNodes.GetEnumerator();
                            try
                            {
                                while (enumerator.MoveNext())
                                {
                                    XmlNode current = (XmlNode)enumerator.Current;
                                    if (current.Name.Equals("o:signatureline"))
                                    {
                                        current.Attributes.GetNamedItem("o:suggestedsigner").InnerText = value;
                                        break;
                                    }
                                }
                                break;
                            }
                            finally
                            {
                                IDisposable disposable = enumerator as IDisposable;
                                if (disposable != null)
                                    disposable.Dispose();
                            }
                        }
                    }
                    string innerXml = xmlDocument.InnerXml;
                    int startIndex = innerXml.IndexOf("o:suggestedsigner");
                    int num2 = innerXml.IndexOf("\"", startIndex);
                    int num3 = innerXml.IndexOf("\"", num2 + 1);
                    string oldValue = innerXml.Substring(num2 + 1, num3 - num2 - 1);
                    byte[] bytes = Encoding.UTF8.GetBytes(innerXml.Replace(oldValue, value).Replace("z-index:" + ESign.openXmlSignIndex.ToString().Trim() + ";visibility:hidden", "z-index:" + ESign.openXmlSignIndex.ToString().Trim()));
                    stream.Position = 0L;
                    stream.SetLength((long)bytes.Length);
                    stream.Write(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                ErrorLog.WriteToErrorLog(ESign.fileLog, ex.Message, ex.StackTrace, "Loi SetSuggestedSigneForSignPart:");
            }
        }

        private static System.Security.Cryptography.Xml.DataObject CreateOfficeObject(string signatureID, string manifestHashAlgorithm, string subject, string signPartGUID)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(string.Format(Resources.OfficeObject, (object)signatureID, (object)manifestHashAlgorithm, (object)signPartGUID, (object)subject));
            System.Security.Cryptography.Xml.DataObject dataObject = new System.Security.Cryptography.Xml.DataObject();
            dataObject.LoadXml(xmlDocument.DocumentElement);
            dataObject.Id = ESign.OfficeObjectID;
            return dataObject;
        }

        public static byte[] CreateTVAN_TO_DN_PDF(TVAN_To_DN_Pdf pdfStruct, string pfxPath, string pass)
        {
            byte[] toDangKy = ESign.CreateToDangKy(pdfStruct);
            byte[] Output = new byte[0];
            ESign.ETAXSign(ESign.GetCertificate(pfxPath, pass), toDangKy, ref Output);
            return Output;
        }

        public static byte[] CreateTVAN_TO_DN_PDF(Thue_To_DN_Pdf pdfStruct, string pfxPath, string pass, bool isSign)
        {
            byte[] toDangKy = ESign.CreateToDangKy(pdfStruct);
            if (!isSign)
                return toDangKy;
            byte[] Output = new byte[0];
            ESign.ETAXSign(ESign.GetCertificate(pfxPath, pass), toDangKy, ref Output);
            return Output;
        }

        public static byte[] CreateTVAN_TO_DN_PDF(Thue_To_DN_Pdf pdfStruct, string pfxPath, string pass, bool isSign, ref string body)
        {
            byte[] toDangKy = ESign.CreateToDangKy(pdfStruct, ref body);
            if (!isSign)
                return toDangKy;
            byte[] Output = new byte[0];
            ESign.ETAXSign(ESign.GetCertificate(pfxPath, pass), toDangKy, ref Output);
            return Output;
        }

        public static byte[] CreatePhanHoi_TO_DN_PDF(PhanHoi_To_DN_Pdf pdfStruct, string pfxPath, string pass, bool isSign, ref string body)
        {
            byte[] phanHoiNopSaiCqt = ESign.CreateToPhanHoiNopSaiCQT(pdfStruct, ref body);
            if (!isSign)
                return phanHoiNopSaiCqt;
            byte[] Output = new byte[0];
            ESign.ETAXSign(ESign.GetCertificate(pfxPath, pass), phanHoiNopSaiCqt, ref Output);
            return Output;
        }

        private static byte[] CreateToDangKy(TVAN_To_DN_Pdf pdfStruct)
        {
            DateTime now = DateTime.Now;
            string htmlDisplayText = File.ReadAllText(pdfStruct.htmlFile);
            string str1 = "$maGD$|$maloaiGD$|$noidungGD$|$ngayTN$|$fullName$|$tIN$|$Ngay$";
            string str2 = pdfStruct.maGD + "|" + pdfStruct.maLoaiGD + "|" + pdfStruct.noiDungGD + "|" + pdfStruct.ngayTN + "|" + pdfStruct.fullName + "|" + pdfStruct.tIN + "|" + string.Format("Ngày {0} tháng {1} năm {2}", (object)now.Day, (object)now.Month, (object)now.Year);
            string[] strArray1 = str1.Split('|');
            string[] strArray2 = str2.Split('|');
            for (int index = 0; index < strArray1.Length; ++index)
                htmlDisplayText = htmlDisplayText.Replace(strArray1[index], strArray2[index]);
            return ESign.HtmToPdf(htmlDisplayText);
        }

        private static byte[] CreateToPhanHoiNopSaiCQT(PhanHoi_To_DN_Pdf pdfStruct, ref string body)
        {
            DateTime now = DateTime.Now;
            string htmlDisplayText = File.ReadAllText(pdfStruct.htmlFile);
            string str1 = "$soCV$|$diaChi$|$msg$|$msgName$|$loaiTK$|$ngayTN$|$cqtNop$|$cqt$|$diaChiCqt$|$ngayGui$";
            string str2 = pdfStruct.soCV + "|" + pdfStruct.diaChi + "|" + pdfStruct.msg + "|" + pdfStruct.msgName + "|" + pdfStruct.loaiTK + "|" + pdfStruct.ngayTN + "|" + pdfStruct.cqtNop + "|" + pdfStruct.cqt + "|" + pdfStruct.diaChiCqt + "|" + pdfStruct.ngayGui;
            string[] strArray1 = str1.Split('|');
            string[] strArray2 = str2.Split('|');
            for (int index = 0; index < strArray1.Length; ++index)
                htmlDisplayText = htmlDisplayText.Replace(strArray1[index], strArray2[index]);
            if (pdfStruct.cqt != "" & !pdfStruct.cqt.ToUpper().Contains("HỒ CHÍ MINH"))
                htmlDisplayText = htmlDisplayText.Replace("CỤC THUẾ TP.HỒ CHÍ MINH", pdfStruct.cqt.ToUpper()).Replace("TỔNG CỤC THUẾ", "CỤC THUẾ TP.HỒ CHÍ MINH");
            body = htmlDisplayText;
            return ESign.HtmToPdf(htmlDisplayText);
        }

        private static byte[] CreateToDangKy(Thue_To_DN_Pdf pdfStruct)
        {
            DateTime now = DateTime.Now;
            string htmlDisplayText = File.ReadAllText(pdfStruct.htmlFile);
            string str1 = "$maGD$|$maloaiGD$|$noidungGD$|$maketquaGD$|$ketquaGD$|$maloiGD$|$motaloiGD$|$ngayTN$|$fullName$|$tIN$|$Ngay$";
            string str2 = pdfStruct.maGD + "|" + pdfStruct.maLoaiGD + "|" + pdfStruct.noiDungGD + "|" + pdfStruct.maketquaGD + "|" + pdfStruct.ketquaGD + "|" + pdfStruct.maloiGD + "|" + pdfStruct.motaloiGD + "|" + pdfStruct.ngayTN + "|" + pdfStruct.fullName + "|" + pdfStruct.tIN + "|" + string.Format("Ngày {0} tháng {1} năm {2}", (object)now.Day, (object)now.Month, (object)now.Year);
            string[] strArray1 = str1.Split('|');
            string[] strArray2 = str2.Split('|');
            for (int index = 0; index < strArray1.Length; ++index)
                htmlDisplayText = htmlDisplayText.Replace(strArray1[index], strArray2[index]);
            return ESign.HtmToPdf(htmlDisplayText);
        }

        private static byte[] CreateToDangKy(Thue_To_DN_Pdf pdfStruct, ref string body)
        {
            DateTime now = DateTime.Now;
            string htmlDisplayText = File.ReadAllText(pdfStruct.htmlFile);
            string str1 = "$maGD$|$maloaiGD$|$noidungGD$|$maketquaGD$|$ketquaGD$|$maloiGD$|$motaloiGD$|$ngayTN$|$fullName$|$tIN$|$Ngay$";
            string str2 = pdfStruct.maGD + "|" + pdfStruct.maLoaiGD + "|" + pdfStruct.noiDungGD + "|" + pdfStruct.maketquaGD + "|" + pdfStruct.ketquaGD + "|" + pdfStruct.maloiGD + "|" + pdfStruct.motaloiGD + "|" + pdfStruct.ngayTN + "|" + pdfStruct.fullName + "|" + pdfStruct.tIN + "|" + string.Format("Ngày {0} tháng {1} năm {2}", (object)now.Day, (object)now.Month, (object)now.Year);
            string[] strArray1 = str1.Split('|');
            string[] strArray2 = str2.Split('|');
            for (int index = 0; index < strArray1.Length; ++index)
                htmlDisplayText = htmlDisplayText.Replace(strArray1[index], strArray2[index]);
            body = htmlDisplayText;
            return ESign.HtmToPdf(htmlDisplayText);
        }

        private static byte[] HtmToPdf(string htmlDisplayText)
        {
            Document document = new Document();
            MemoryStream memoryStream = new MemoryStream();
            PdfWriter.GetInstance(document, (Stream)memoryStream);
            StringReader stringReader = new StringReader(htmlDisplayText);
            HTMLWorker htmlWorker = new HTMLWorker((IDocListener)document);
            document.Open();
            htmlWorker.Parse((TextReader)stringReader);
            document.Close();
            return memoryStream.GetBuffer();
        }

        public struct SystemTime
        {
            public ushort Year;
            public ushort Month;
            public ushort DayOfWeek;
            public ushort Day;
            public ushort Hour;
            public ushort Minute;
            public ushort Second;
            public ushort Millisecond;
        }

        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        public class RectAndText
        {
            public iTextSharp.text.Rectangle rect { get; set; }
            public string text { get; set; }
            public RectAndText(iTextSharp.text.Rectangle _rect, string _text)
            {
                rect = _rect;
                text = _text;
            }
        }

        public class MyStrategy : LocationTextExtractionStrategy
        {
            public List<RectAndText> myPoints = new List<RectAndText>();
            public override void RenderText(TextRenderInfo renderInfo)
            {
                base.RenderText(renderInfo);

                var bottomLeft = renderInfo.GetDescentLine().GetStartPoint();
                var topright = renderInfo.GetAscentLine().GetEndPoint();

                var rect = new iTextSharp.text.Rectangle(bottomLeft[Vector.I1], bottomLeft[Vector.I2],
                                                topright[Vector.I1], topright[Vector.I2]);
                myPoints.Add(new RectAndText(rect, renderInfo.GetText()));
            }
        }
    }
}
