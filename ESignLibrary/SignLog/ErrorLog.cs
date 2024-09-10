// Decompiled with JetBrains decompiler
// Type: SignLog.ErrorLog
// Assembly: ESignLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FC312E54-D344-41AE-A232-7B8768ED42AA
// Assembly location: D:\1. Project\EBH\EBH\Bin\ESignLibrary.dll

using System;
using System.Diagnostics;
using System.IO;

namespace SignLog
{
  public static class ErrorLog
  {
    public static void WriteToErrorLog(string filePath, string msg, string stkTrace, string title)
    {
      FileInfo fileInfo1 = (FileInfo) null;
      FileStream fileStream1 = (FileStream) null;
      FileStream fileStream2 = (FileStream) null;
      try
      {
        FileInfo fileInfo2 = new FileInfo(filePath);
        if (fileInfo2.Exists && fileInfo2.Length > 10000000L)
          fileInfo2.Delete();
        FileStream fileStream3 = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        new StreamWriter((Stream) fileStream3).Close();
        fileStream3.Close();
        FileStream fileStream4 = new FileStream(filePath, FileMode.Append, FileAccess.Write);
        StreamWriter streamWriter = new StreamWriter((Stream) fileStream4);
        streamWriter.Write("Title: " + title + Environment.NewLine);
        streamWriter.Write("Message: " + msg + Environment.NewLine);
        streamWriter.Write("StackTrace: " + stkTrace + Environment.NewLine);
        streamWriter.Write("Date/Time: " + DateTime.Now.ToString() + Environment.NewLine);
        streamWriter.Write("================================================" + Environment.NewLine);
        streamWriter.Close();
        fileStream4.Close();
      }
      catch (Exception ex)
      {
      }
      finally
      {
        fileStream2 = (FileStream) null;
        fileInfo1 = (FileInfo) null;
        fileStream1 = (FileStream) null;
      }
    }

    public static void WriteMessageLog(string msg, string FileName)
    {
      FileInfo fileInfo1 = (FileInfo) null;
      FileStream fileStream1 = (FileStream) null;
      FileStream fileStream2 = (FileStream) null;
      try
      {
        FileInfo fileInfo2 = new FileInfo(FileName);
        if (fileInfo2.Exists)
          fileInfo2.Delete();
        fileStream1 = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        new StreamWriter((Stream) fileStream1).Close();
        fileStream1.Close();
        fileStream2 = new FileStream(FileName, FileMode.Append, FileAccess.Write);
        StreamWriter streamWriter = new StreamWriter((Stream) fileStream2);
        streamWriter.Write(msg);
        streamWriter.Close();
        fileStream2.Close();
      }
      catch (Exception ex)
      {
      }
      finally
      {
        if (fileStream2 != null)
          fileStream2.Dispose();
        fileInfo1 = (FileInfo) null;
        if (fileStream1 != null)
          fileStream1.Dispose();
      }
    }

    public static bool WriteToEventLog(string entry, string appName, EventLogEntryType eventType, string logName)
    {
      EventLog eventLog = new EventLog();
      try
      {
        if (!EventLog.SourceExists(appName))
          EventLog.CreateEventSource(appName, logName);
        eventLog.Source = appName;
        eventLog.WriteEntry(entry, eventType);
        return true;
      }
      catch (Exception ex)
      {
        return false;
      }
      finally
      {
        eventLog.Dispose();
      }
    }

    public static void LogInfo(string sourceName, string sMessage)
    {
      try
      {
        if (!EventLog.SourceExists(sourceName))
          EventLog.CreateEventSource(sourceName, "Application");
        EventLog.WriteEntry(sourceName, sMessage, EventLogEntryType.Information);
      }
      catch (Exception ex)
      {
      }
    }

    public static void LogEvent(string sourceName, string sMessage)
    {
      try
      {
        if (!EventLog.SourceExists(sourceName))
          EventLog.CreateEventSource(sourceName, "Application");
        EventLog.WriteEntry(sourceName, sMessage, EventLogEntryType.Error);
      }
      catch (Exception ex)
      {
      }
    }
  }
}
