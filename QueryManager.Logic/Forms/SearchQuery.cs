using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using MyException = SimpleFramework.ExceptionHandler;

namespace QueryManager.Logic.Forms
{
    internal class SearchQuery
    {
        internal SearchQuery()
        {
            Loaded = false;
        }

        internal const string FormType = "QM_SearchQuery";

        internal const string GridResults = "GdResults";

        internal const string ComboBoxCategory = "CbCategory";

        internal const string TextBoxQueryName = "TbQueryNam";
        internal const string TextBoxQueryContent = "TbQueryCon";

        internal const string ButtonSearch = "BtnSearch";
        internal const string ButtonReset = "BtnReset";

        internal const string ButtonOpenInMSSQL = "BtnOpenSQL";
        internal const string ButtonOpenInSAPB1 = "BtnOpenSAP";

        internal string XMLSource = Properties.Resources.XML_SearchQuery;

        internal bool queryOpendInMSSQL = false;

        internal bool Loaded { get; private set; }

        internal SAPbouiCOM.Form Form { get; private set; }

        internal SAPbouiCOM.Grid Grid(string uid)
        {
            return Form.Items.Item(uid).Specific as SAPbouiCOM.Grid;
        }

        internal SAPbouiCOM.Button Button(string uid)
        {
            return Form.Items.Item(uid).Specific as SAPbouiCOM.Button;
        }

        internal bool QueryOpendInMSSQL
        {
            get
            {
                return queryOpendInMSSQL;
            }
            set
            {
                queryOpendInMSSQL = value;

                if (value)
                {
                    Button(ButtonOpenInMSSQL).Caption = "SQL-Datei Inhalt laden";
                }
                else
                {
                    Button(ButtonOpenInMSSQL).Caption = "Abfrage in MS SQL öffnen";
                }
            }
        }

        internal string QueryAsSqlFilePath { get; set; }

        internal void Load()
        {
            try
            {
                SAPbouiCOM.FormCreationParams fcp = Basic.Application.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_FormCreationParams) as SAPbouiCOM.FormCreationParams;

                fcp.FormType = FormType;
                fcp.UniqueID = Guid.NewGuid().ToString().Replace("-", string.Empty);
                fcp.XmlData = XMLSource;

                Form = Basic.Application.Forms.AddEx(fcp);

                FillComboBoxCategories();

                IntializeGrid();

                Basic.Application.ItemEvent += Application_ItemEvent;
            }
            catch (Exception ex)
            {
                MyException.HandleInternal(MethodBase.GetCurrentMethod(), ex);
            }
            finally
            {
                Loaded = true;
            }
        }

        void Unload()
        {
            Loaded = false;
            Basic.Application.ItemEvent += Application_ItemEvent;
        }

        void FillComboBoxCategories()
        {
            SAPbouiCOM.ComboBox cb = Form.Items.Item(ComboBoxCategory).Specific as SAPbouiCOM.ComboBox;
            SAPbobsCOM.Recordset rs = Basic.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset) as SAPbobsCOM.Recordset;

            rs.DoQuery("SELECT CategoryId, CatName FROM OQCN WHERE CategoryId NOT IN (-2) AND PermMask NOT LIKE 'N%' ORDER BY CatName");

            cb.ValidValues.Add("-", "Alle Kategorien");

            for (int i = 0; i < rs.RecordCount; i++)
            {
                cb.ValidValues.Add(rs.Fields.Item("CategoryId").Value.ToString(), rs.Fields.Item("CatName").Value.ToString());

                rs.MoveNext();
            }

            cb.Select("-", SAPbouiCOM.BoSearchKey.psk_ByValue);
        }

        void IntializeGrid()
        {
            SAPbouiCOM.Grid grid = Form.Items.Item(GridResults).Specific as SAPbouiCOM.Grid;

            grid.DataTable = Form.DataSources.DataTables.Add(GridResults);
        }

        bool WriteQueryContentToSQLFile(out string filePath)
        {
            filePath = null;

            SAPbouiCOM.Grid grid = Grid(GridResults);

            if (grid.Rows.SelectedRows.Count > 0)
            {
                int selectedIndex = grid.Rows.SelectedRows.Item(0, SAPbouiCOM.BoOrderType.ot_RowOrder);

                string category = grid.DataTable.GetValue("Kategorie", grid.GetDataTableRowIndex(selectedIndex)).ToString();
                string queryName = grid.DataTable.GetValue("Abfrage", grid.GetDataTableRowIndex(selectedIndex)).ToString();
                string queryContent = grid.DataTable.GetValue("Abfrageinhalt", grid.GetDataTableRowIndex(selectedIndex)).ToString();

                // MTA @ 2013-11-12 : Entferne Sonderzeichen aus Namen, welche nicht für Dateipfade verwendet werden können.
                category = category.Replace("/", string.Empty).Replace("\\", string.Empty).Replace(":", string.Empty).Replace("*", string.Empty).Replace("?", string.Empty).Replace("\"", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty).Replace("|", string.Empty);
                queryName = queryName.Replace("/", string.Empty).Replace("\\", string.Empty).Replace(":", string.Empty).Replace("*", string.Empty).Replace("?", string.Empty).Replace("\"", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty).Replace("|", string.Empty);

                filePath = Path.Combine(Basic.AppDataFolder, "QM_" + category + "_" + queryName + ".sql");

                bool write = WriteContentToFile(filePath, queryContent);

                if (write)
                {
                    // Öffne geschriebene Datei mit Standardprogramm
                    Process.Start(filePath);
                }

                return write;
            }
            else
            {
                Basic.Application.StatusBar.SetText("Bitte markieren Sie erst eine Abfrage.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                return false;
            }
        }

        bool WriteContentToFile(string filePath, string content)
        {
            using (StreamWriter sw = new StreamWriter(filePath, false))
            {
                sw.Write(content);
                sw.Close();
            }

            return true;
        }

        bool ReadQueryContentFromSQLFile(string filePath, int queryID, int categoryID)
        {
            string fileContent = null;

            if (File.Exists(filePath))
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    fileContent = sr.ReadToEnd();
                    sr.Close();
                }

                SAPbobsCOM.UserQueries uq = Basic.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oUserQueries) as SAPbobsCOM.UserQueries;

                try
                {
                    if (uq.GetByKey(queryID, categoryID))
                    {
                        // Überschreibe SQL-Query der Abfrage durch Inhalt von Datei
                        uq.Query = fileContent;
                        int result = uq.Update();

                        if (result == 0)
                            return true;
                        else
                        {
                            Basic.Application.StatusBar.SetText("Fehler beim Aktualisieren der gespeicherten Abfrage: " + Basic.Company.GetLastErrorDescription(), SAPbouiCOM.BoMessageTime.bmt_Medium, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                            return false;
                        }
                    }
                    else
                    {
                        Basic.Application.StatusBar.SetText("Die gespeicherten Abfrage konnte nicht gefunden werden.", SAPbouiCOM.BoMessageTime.bmt_Medium, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                        return false;
                    }
                }
                finally
                {
                    if (uq != null)
                    {
                        Marshal.ReleaseComObject(uq);
                        uq = null;
                    }
                }
            }
            else
            {
                Basic.Application.StatusBar.SetText("Die SQL-Datei wurde nicht gefunden, Pfad: " + filePath, SAPbouiCOM.BoMessageTime.bmt_Medium, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                return false;
            }
        }

        void Application_ItemEvent(string FormUID, ref SAPbouiCOM.ItemEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;

            try
            {
                if (Loaded && FormUID == Form.UniqueID)
                {
                    if (pVal.BeforeAction)
                    {
                        switch (pVal.EventType)
                        {
                            case SAPbouiCOM.BoEventTypes.et_FORM_CLOSE:
                                Unload();
                                break;
                        }
                    }
                    else
                    {
                        switch (pVal.EventType)
                        {
                            case SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED:
                                switch (pVal.ItemUID)
                                {
                                    case ButtonSearch:
                                        ButtonSearch_ItemPressed();
                                        break;
                                    case ButtonReset:
                                        ButtonReset_ItemPressed();
                                        break;

                                    case ButtonOpenInMSSQL:
                                        ButtonOpenInMSSQL_ItemPressed();
                                        break;
                                    case ButtonOpenInSAPB1:
                                        ButtonOpenInSAPB1_ItemPressed();
                                        break;

                                    case GridResults:
                                        GridResults_ItemPressed(pVal.Row);
                                        break;
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MyException.HandlePublic(MethodBase.GetCurrentMethod(), ex);
            }
        }

        void ButtonSearch_ItemPressed()
        {
            try
            {
                Form.Freeze(true);

                StringBuilder sqlQuery = new StringBuilder();

                string category = Form.DataSources.UserDataSources.Item(ComboBoxCategory).Value.Trim();
                string queryName = Form.DataSources.UserDataSources.Item(TextBoxQueryName).Value.Trim();
                string queryContent = Form.DataSources.UserDataSources.Item(TextBoxQueryContent).Value.Trim();

                sqlQuery.AppendLine("SELECT");
                sqlQuery.AppendLine("T0.IntrnalKey AS AbfrageID, T1.CatName AS Kategorie, T1.CategoryID AS KategorieID, T0.QName AS Abfrage, T0.QString AS Abfrageinhalt");
                sqlQuery.AppendLine("FROM OUQR T0");
                sqlQuery.AppendLine("INNER JOIN OQCN T1 ON T1.CategoryId = T0.QCategory AND T1.CategoryId NOT IN (-2) AND T1.PermMask NOT LIKE 'N%'");

                // Platzhalter mit Escape-Sequenz versehen
                category = category.Replace("%", "~%").Replace("_", "~_").Replace("[", "[~").Replace("]", "~]");
                queryName = queryName.Replace("%", "~%").Replace("_", "~_").Replace("[", "[~").Replace("]", "~]");
                queryContent = queryContent.Replace("%", "~%").Replace("_", "~_").Replace("[", "[~").Replace("]", "~]");

                bool conditionAdded = false;

                if (!category.Equals("-"))
                {
                    if (!conditionAdded)
                        sqlQuery.AppendLine("WHERE");
                    else
                        sqlQuery.AppendLine("AND");

                    sqlQuery.AppendLine(string.Format("T0.QCategory = '{0}'", category));

                    conditionAdded = true;
                }

                if (!string.IsNullOrEmpty(queryName))
                {
                    if (!conditionAdded)
                        sqlQuery.AppendLine("WHERE");
                    else
                        sqlQuery.AppendLine("AND");

                    sqlQuery.AppendLine(string.Format("T0.QName LIKE N'%{0}%'", queryName));

                    conditionAdded = true;
                }

                if (!string.IsNullOrEmpty(queryContent))
                {
                    if (!conditionAdded)
                        sqlQuery.AppendLine("WHERE");
                    else
                        sqlQuery.AppendLine("AND");

                    sqlQuery.AppendLine(string.Format("T0.QString LIKE N'%{0}%'", queryContent));

                    conditionAdded = true;
                }

                Form.DataSources.DataTables.Item(GridResults).ExecuteQuery(sqlQuery.ToString());

                SAPbouiCOM.Grid grid = Form.Items.Item(GridResults).Specific as SAPbouiCOM.Grid;

                grid.Columns.Item("KategorieID").Visible = false;
                grid.Columns.Item("AbfrageID").Visible = false;

                for (int i = 0; i < grid.Columns.Count; i++)
                    grid.Columns.Item(i).Editable = false;
            }
            finally
            {
                Form.Freeze(false);
            }
        }

        void ButtonReset_ItemPressed()
        {
            Form.DataSources.UserDataSources.Item(ComboBoxCategory).Value = "-";
            string queryName = Form.DataSources.UserDataSources.Item(TextBoxQueryName).Value = string.Empty;
            string queryContent = Form.DataSources.UserDataSources.Item(TextBoxQueryContent).Value = string.Empty;
        }

        void ButtonOpenInMSSQL_ItemPressed()
        {
            if (QueryOpendInMSSQL)
            {
                if (Basic.Application.MessageBox("Möchten Sie den Inhalt der gespeicherten Abfrage wirklich überschreiben lassen?", 1, "Ja", "Nein") == 1)
                {
                    SAPbouiCOM.Grid grid = Grid(GridResults);

                    int selectedIndex = grid.Rows.SelectedRows.Item(0, SAPbouiCOM.BoOrderType.ot_RowOrder);

                    int categoryID = int.Parse(grid.DataTable.GetValue("KategorieID", grid.GetDataTableRowIndex(selectedIndex)).ToString());
                    int queryID = int.Parse(grid.DataTable.GetValue("AbfrageID", grid.GetDataTableRowIndex(selectedIndex)).ToString());

                    string category = grid.DataTable.GetValue("Kategorie", grid.GetDataTableRowIndex(selectedIndex)).ToString();
                    string queryName = grid.DataTable.GetValue("Abfrage", grid.GetDataTableRowIndex(selectedIndex)).ToString();
                    string query = grid.DataTable.GetValue("Abfrageinhalt", grid.GetDataTableRowIndex(selectedIndex)).ToString();

                    // MTA @ 2013-11-12 : Entferne Sonderzeichen aus Namen, welche nicht für Dateipfade verwendet werden können.
                    category = category.Replace("/", string.Empty).Replace("\\", string.Empty).Replace(":", string.Empty).Replace("*", string.Empty).Replace("?", string.Empty).Replace("\"", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty).Replace("|", string.Empty);
                    queryName = category.Replace("/", string.Empty).Replace("\\", string.Empty).Replace(":", string.Empty).Replace("*", string.Empty).Replace("?", string.Empty).Replace("\"", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty).Replace("|", string.Empty);

                    // Speichere SQL-Query der Abfrage sicherheitshalber weg
                    string backUpFilePath = Path.Combine(Basic.BackUpFolder, DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_QM_" + category + "_" + queryName + ".sql");
                    WriteContentToFile(backUpFilePath, query);

                    if (ReadQueryContentFromSQLFile(QueryAsSqlFilePath, queryID, categoryID))
                    {
                        QueryAsSqlFilePath = null;

                        QueryOpendInMSSQL = false;

                        Basic.Application.StatusBar.SetText("Die gespeicherte Abfrage wurde überschrieben.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Success);
                    }
                }
            }
            else
            {
                string filePath = null;

                if (WriteQueryContentToSQLFile(out filePath))
                {
                    QueryAsSqlFilePath = filePath;

                    QueryOpendInMSSQL = true;
                }
            }
        }

        void ButtonOpenInSAPB1_ItemPressed()
        {
            SAPbouiCOM.Grid grid = Grid(GridResults);

            if (grid.Rows.SelectedRows.Count > 0)
            {
                int selectedIndex = grid.Rows.SelectedRows.Item(0, SAPbouiCOM.BoOrderType.ot_RowOrder);

                string category = grid.DataTable.GetValue("Kategorie", grid.GetDataTableRowIndex(selectedIndex)).ToString();
                string queryName = grid.DataTable.GetValue("Abfrage", grid.GetDataTableRowIndex(selectedIndex)).ToString();

                string xmlContent = Basic.Application.Menus.Item("43573").SubMenus.GetAsXML();

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlContent);

                // MTA @ 2013-11-12 : Der QueryName wird als Menüeintrag auf maximal 50 Zeichen gekürzt.
                // Daraus ergibt sich eine ungenaue Angabe, sollten mehr als eine Query mit den selben 50 Zeichen beginnen.
                // Abbruch wenn mehr als eine gefunden wurden und entsprechende Meldung!
                string categoryInXml = category;
                string queryNameInXml = queryName;
                if (categoryInXml.Length > 50) categoryInXml = category.Substring(0, 50);
                if (queryNameInXml.Length > 50) queryNameInXml = queryName.Substring(0, 50);
                
                string xPath = string.Format("/Application/Menus/action/Menu[@UniqueID='53248']/Menus/action/Menu[@String='{0}']/Menus/action/Menu[@String='{1}']", categoryInXml, queryNameInXml);

                XmlNodeList xmlNodes = xmlDoc.SelectNodes(xPath);

                if (xmlNodes.Count > 0)
                {
                    // Wurde nur ein Menüeintrag gefunden?
                    if (xmlNodes.Count.Equals(1))
                    {
                        XmlAttribute uniqueID = xmlNodes[0].Attributes["UniqueID"];

                        Basic.Application.ActivateMenuItem(uniqueID.InnerXml);

                        SAPbouiCOM.Form form = Basic.Application.Forms.ActiveForm;

                        System.Form285 queryForm = new System.Form285();
                        queryForm.Init(form.TypeEx, form.UniqueID, form);
                        queryForm.Category = category;
                        queryForm.QueryName = queryName;
                    }
                    else
                        Basic.Application.StatusBar.SetText(string.Format("Es wurden mehr als eine Abfrage in der Kategorie gefunden, deren Namen mit '{0}' beginnen. Das Öffnen der Abfrage ist daher nicht möglich.", queryNameInXml));
                }
                else
                    Basic.Application.StatusBar.SetText("Die ausgewählte Abfrage wurde noch nicht in SAP B1 hinterlegt, bitte melden Sie sich neu in SAP B1 an.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
            }
            else
                Basic.Application.StatusBar.SetText("Bitte markieren Sie erst eine Abfrage.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
        }

        void GridResults_ItemPressed(int rowIndex)
        {
            if (rowIndex > -1)
            {
                Grid(GridResults).Rows.SelectedRows.Clear();
                Grid(GridResults).Rows.SelectedRows.Add(rowIndex);

                QueryOpendInMSSQL = false;
            }
        }
    }
}