// Decompiled with JetBrains decompiler
// Type: VVX.XmlStore
// Assembly: ESignLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FC312E54-D344-41AE-A232-7B8768ED42AA
// Assembly location: D:\1. Project\EBH\EBH\Bin\ESignLibrary.dll

using System;
using System.Diagnostics;
using System.Xml;

namespace VVX
{
  internal class XmlStore
  {
    private XmlDocument mxDoc = (XmlDocument) null;
    private XmlNode mxnodeSchema = (XmlNode) null;
    private XmlNodeList mxnodelistData = (XmlNodeList) null;
    private string msFile = "";
    private bool mbIsReadOnly = false;
    private string msDataNodeName = "";
    private XmlStore.Field[] mFields = (XmlStore.Field[]) null;
    private string msIdColumnName = "ID";
    private bool mbIdColumnAdded = false;
    private int mnIdColumn = -1;
    private bool mbThrowExceptions = false;

    public bool IsReadOnly
    {
      get
      {
        return this.mbIsReadOnly;
      }
      set
      {
        this.mbIsReadOnly = value;
      }
    }

    public int ColumnUID
    {
      get
      {
        return this.mnIdColumn;
      }
      set
      {
        this.mnIdColumn = value;
      }
    }

    public string NameOfFieldWithUniqueID
    {
      get
      {
        return this.msIdColumnName;
      }
      set
      {
        this.msIdColumnName = value;
      }
    }

    public XmlStore.Field[] Fields
    {
      get
      {
        return this.mFields;
      }
      set
      {
        this.mFields = value;
      }
    }

    public string File
    {
      get
      {
        return this.msFile;
      }
      set
      {
        this.msFile = value;
      }
    }

    public string RecordNodeName
    {
      get
      {
        return this.msDataNodeName;
      }
      set
      {
        this.msDataNodeName = value;
      }
    }

    public bool ThrowExceptions
    {
      get
      {
        return this.mbThrowExceptions;
      }
      set
      {
        this.mbThrowExceptions = value;
      }
    }

    private XmlStore()
    {
    }

    public XmlStore(string sFile)
    {
      this.msFile = sFile;
      this.mbIsReadOnly = VVX.File.IsReadOnly(sFile);
    }

    private bool DoAdjustForUniqueID()
    {
      this.mnIdColumn = -1;
      bool flag = false;
      this.mbIdColumnAdded = false;
      for (int index = 0; index < this.mFields.Length; index = index + 1 + 1)
      {
        if (!flag)
        {
          if (this.mFields[index].name.ToUpper() == this.NameOfFieldWithUniqueID.ToUpper())
          {
            flag = true;
            this.NameOfFieldWithUniqueID = this.mFields[index].name;
          }
        }
        else
          this.mnIdColumn = index;
      }
      if (!flag)
      {
        this.DoFieldAdd(new XmlStore.Field(this.NameOfFieldWithUniqueID, this.NameOfFieldWithUniqueID, XmlStore.DataType.String));
        this.mbIdColumnAdded = true;
        this.mnIdColumn = this.mFields.Length - 1;
      }
      return this.mbIdColumnAdded;
    }

    public int DoLoadSchema(XmlDocument xDoc)
    {
      int num = 0;
      this.mnIdColumn = -1;
      this.mbIdColumnAdded = false;
      try
      {
        if (xDoc != null)
        {
          this.mxnodeSchema = xDoc.SelectSingleNode("//schema");
          if (this.mxnodeSchema != null || num > 0)
          {
            this.msDataNodeName = ((XmlElement) this.mxnodeSchema).GetAttribute("datanodename");
            XmlNodeList xmlNodeList = this.mxDoc.SelectNodes("//schema/field");
            if (xmlNodeList != null)
            {
              this.mFields = new XmlStore.Field[xmlNodeList.Count];
              int index = 0;
              foreach (XmlElement xmlElement in xmlNodeList)
              {
                this.mFields[index].name = xmlElement.GetAttribute("name");
                this.mFields[index].title = xmlElement.GetAttribute("title");
                this.mFields[index].type = XmlStore.DataType.String;
                this.mFields[index].width = xmlElement.GetAttribute("width");
                ++index;
              }
              this.DoAdjustForUniqueID();
              num = this.mFields.Length;
            }
          }
          else
          {
            XmlNodeList childNodes = this.mxDoc.DocumentElement.ChildNodes;
            if (childNodes != null && childNodes.Count > 0)
            {
              XmlElement xmlElement = (XmlElement) childNodes[0];
              this.msDataNodeName = xmlElement.Name;
              int count = xmlElement.Attributes.Count;
              this.mFields = new XmlStore.Field[count];
              for (int index = 0; index < count; ++index)
              {
                this.mFields[index].name = xmlElement.Attributes[index].Name;
                this.mFields[index].title = this.mFields[index].name;
                this.mFields[index].type = XmlStore.DataType.String;
              }
              this.DoAdjustForUniqueID();
              num = this.mFields.Length;
            }
          }
        }
      }
      catch (Exception ex)
      {
        string message = ex.ToString();
        Debug.WriteLine(message);
        if (this.mbThrowExceptions)
          throw new AccessViolationException(message);
      }
      return num;
    }

    public int DoLoadRecords()
    {
      return this.DoLoadRecords(this.msFile, this.msDataNodeName);
    }

    public int DoLoadRecords(string nameOfDataNode)
    {
      this.msDataNodeName = nameOfDataNode;
      return this.DoLoadRecords(this.msFile, nameOfDataNode);
    }

    public int DoLoadRecords(string nameOfXmlStoreFile, string nameOfDataNode)
    {
      int num = 0;
      this.msFile = nameOfXmlStoreFile;
      this.msDataNodeName = nameOfDataNode;
      try
      {
        this.mxDoc = new XmlDocument();
        this.mxDoc.Load(this.msFile);
        this.DoLoadSchema(this.mxDoc);
        if (this.Fields.Length > 0)
        {
          this.mxnodelistData = this.mxDoc.SelectNodes("//" + this.msDataNodeName);
          num = this.mxnodelistData.Count;
        }
      }
      catch (Exception ex)
      {
        string message = ex.ToString();
        Debug.WriteLine(message);
        if (this.mbThrowExceptions)
          throw new AccessViolationException(message);
      }
      return num;
    }

    private void DoAppendString(ref string[] sArray, string sNew)
    {
      string[] strArray = new string[sArray.Length + 1];
      sArray.CopyTo((Array) strArray, 0);
      strArray[sArray.Length] = sNew;
      sArray = strArray;
    }

    private void DoFieldAdd(XmlStore.Field sNew)
    {
      XmlStore.Field[] fieldArray = new XmlStore.Field[this.mFields.Length + 1];
      this.mFields.CopyTo((Array) fieldArray, 0);
      fieldArray[this.mFields.Length] = sNew;
      this.mFields = fieldArray;
    }

    private void DoFieldAdd(ref XmlStore.Field[] sArray, XmlStore.Field sNew)
    {
      XmlStore.Field[] fieldArray = new XmlStore.Field[sArray.Length + 1];
      sArray.CopyTo((Array) fieldArray, 0);
      fieldArray[sArray.Length] = sNew;
      sArray = fieldArray;
    }

    public long DoGenerateUID(int nRow)
    {
      long binary = DateTime.Now.ToUniversalTime().ToBinary();
      if (this.mbIdColumnAdded)
        binary += (long) nRow;
      return binary;
    }

    public string DoGenerateSID(int nRow)
    {
      return this.DoGenerateUID(nRow).ToString();
    }

    public string[] DoGetRecord(int nRow)
    {
      string[] strArray = (string[]) null;
      try
      {
        XmlElement xmlElement = (XmlElement) this.mxnodelistData[nRow];
        if (xmlElement != null)
        {
          if (xmlElement.GetAttribute(this.NameOfFieldWithUniqueID) == "")
          {
            string sid = this.DoGenerateSID(nRow);
            xmlElement.SetAttribute(this.NameOfFieldWithUniqueID, sid);
          }
          XmlAttributeCollection attributes = xmlElement.Attributes;
          int count = attributes.Count;
          if (strArray == null)
            strArray = new string[count];
          for (int index = 0; index < count; ++index)
            strArray[index] = attributes[index].Value;
        }
      }
      catch (Exception ex)
      {
        string message = ex.ToString();
        Debug.WriteLine(message);
        if (this.mbThrowExceptions)
          throw new AccessViolationException(message);
      }
      return strArray;
    }

    public string[] DoGetRecordOrdered(int nRow)
    {
      string[] strArray = (string[]) null;
      try
      {
        XmlElement xmlElement = (XmlElement) this.mxnodelistData[nRow];
        if (xmlElement != null)
        {
          if (xmlElement.GetAttribute(this.NameOfFieldWithUniqueID) == "")
          {
            string sid = this.DoGenerateSID(nRow);
            xmlElement.SetAttribute(this.NameOfFieldWithUniqueID, sid);
          }
          strArray = new string[this.mFields.Length];
          for (int index = 0; index < this.mFields.Length; ++index)
          {
            string attribute = xmlElement.GetAttribute(this.mFields[index].name);
            strArray[index] = attribute;
          }
        }
      }
      catch (Exception ex)
      {
        string message = ex.ToString();
        Debug.WriteLine(message);
        if (this.mbThrowExceptions)
          throw new AccessViolationException(message);
      }
      return strArray;
    }

    public string DoGetField(int nRow, string nameOfField)
    {
      string str = "";
      try
      {
        XmlElement xmlElement = (XmlElement) this.mxnodelistData[nRow];
        if (xmlElement != null)
          str = xmlElement.GetAttribute(nameOfField);
      }
      catch (Exception ex)
      {
        string message = ex.ToString();
        Debug.WriteLine(message);
        if (this.mbThrowExceptions)
          throw new AccessViolationException(message);
      }
      return str;
    }

    public string[] DoGetRecordNew()
    {
      string[] strArray = (string[]) null;
      try
      {
        if (this.Fields.Length > 0)
        {
          int length = this.Fields.Length;
          strArray = new string[length];
          for (int index = 0; index < length; ++index)
            strArray[index] = index == this.mnIdColumn ? this.DoGenerateSID(0) : "";
        }
      }
      catch (Exception ex)
      {
        string message = ex.ToString();
        Debug.WriteLine(message);
        if (this.mbThrowExceptions)
          throw new AccessViolationException(message);
      }
      return strArray;
    }

    public bool DoSetRecord(int nRow, string[] rowCells)
    {
      bool flag = false;
      try
      {
        XmlElement xmlElement = (XmlElement) null;
        if (this.mnIdColumn >= 0)
        {
          int mnIdColumn = this.mnIdColumn;
          string name = this.Fields[mnIdColumn].name;
          xmlElement = (XmlElement) this.mxDoc.SelectSingleNode("//" + this.msDataNodeName + "[@" + this.msIdColumnName + "='" + rowCells[mnIdColumn] + "']");
        }
        if (xmlElement == null)
        {
          xmlElement = (XmlElement) this.mxDoc.CreateNode("element", this.msDataNodeName, "");
          this.mxDoc.DocumentElement.AppendChild((XmlNode) xmlElement);
        }
        if (xmlElement != null)
        {
          for (int index = 0; index < this.Fields.Length; ++index)
            xmlElement.SetAttribute(this.Fields[index].name, rowCells[index]);
          flag = true;
        }
      }
      catch (Exception ex)
      {
        string message = ex.ToString();
        Debug.WriteLine(message);
        if (this.mbThrowExceptions)
          throw new AccessViolationException(message);
      }
      return flag;
    }

    public int DoSaveRecords(string nameOfXmlStoreFile, bool bRemoveIdColIfAdded)
    {
      int num = 0;
      try
      {
        if (VVX.File.IsReadOnly(nameOfXmlStoreFile))
        {
          MsgBox.Info("Sorry! The XmlStore file is 'ReadOnly'");
          return num;
        }
        if (this.mxDoc != null)
        {
          if (this.mbIdColumnAdded && bRemoveIdColIfAdded)
          {
            string filename = "temp.xml";
            this.mxDoc.Save(filename);
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(filename);
            XmlNodeList xmlNodeList = xmlDocument.SelectNodes("//" + this.RecordNodeName);
            if (xmlNodeList != null && xmlNodeList.Count > 0)
            {
              foreach (XmlElement xmlElement in xmlNodeList)
              {
                xmlElement.RemoveAttributeNode(this.NameOfFieldWithUniqueID, "");
                ++num;
              }
            }
            xmlDocument.Save(nameOfXmlStoreFile);
            VVX.File.Delete(filename);
          }
          else
            this.mxDoc.Save(nameOfXmlStoreFile);
        }
      }
      catch (Exception ex)
      {
        string message = ex.ToString();
        Debug.WriteLine(message);
        if (this.mbThrowExceptions)
          throw new AccessViolationException(message);
      }
      return num;
    }

    public float DoGetColumnWidthsTotal()
    {
      float num = 0.0f;
      for (int col = 0; col < this.Fields.Length; ++col)
        num += this.DoGetColumnWidth(col);
      return num;
    }

    public float DoGetColumnWidth(int col)
    {
      float num = 0.0f;
      try
      {
        if (0 <= col && col < this.Fields.Length && this.Fields[col].width.Length > 0 && this.Fields[col].width != null)
          num = (float) Convert.ToDouble(this.Fields[col].width);
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex.ToString());
        num = 0.0f;
      }
      return num;
    }

    public enum DataType
    {
      String,
    }

    public struct Field
    {
      public string name;
      public string title;
      public XmlStore.DataType type;
      public string width;

      public Field(string sName, string sTitle, XmlStore.DataType enType)
      {
        this.name = sName;
        this.title = sTitle;
        this.type = enType;
        this.width = "";
      }
    }
  }
}
