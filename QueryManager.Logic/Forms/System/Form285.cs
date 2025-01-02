using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MyException = SimpleFramework.ExceptionHandler;

namespace QueryManager.Logic.Forms.System
{
    /// <summary>
    /// SAP-Form: Abfrage Ausführen (dynamischer FormType)
    /// </summary>
    internal class Form285
    {
        internal string FormType { get; private set; }

        internal string FormUID { get; private set; }

        internal SAPbouiCOM.Form Form { get; private set; }

        internal void Init(string formType, string formUID, SAPbouiCOM.Form form)
        {
            FormType = formType;
            FormUID = formUID;
            Form = form;

            Basic.Application.ItemEvent += Application_ItemEvent;
        }

        internal void Disconnect()
        {
            Basic.Application.ItemEvent -= Application_ItemEvent;

            FormType = null;
            FormUID = null;
            Form = null;
        }
        
        internal string Category { get; set; }
        internal string QueryName { get; set; }

        internal void Application_ItemEvent(string FormUID, ref SAPbouiCOM.ItemEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;

            try
            {
                if (pVal.FormTypeEx.Equals(FormType) && pVal.FormUID.Equals(FormUID))
                {
                    if (pVal.BeforeAction)
                    {
                        switch (pVal.EventType)
                        { 
                            case SAPbouiCOM.BoEventTypes.et_FORM_CLOSE:
                                Disconnect();
                                break;
                        }
                    }
                    else
                        switch (pVal.EventType)
                        {
                            case SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED:
                                switch (pVal.ItemUID)
                                {
                                    case "5": // Button [Speichern]
                                        Form957.SetQueryProperties(Basic.Application.Forms.ActiveForm, Category, QueryName);
                                        break;
                                }
                                break;
                        }
                }
            }
            catch (Exception ex)
            {
                MyException.HandlePublic(MethodBase.GetCurrentMethod(), ex);
            }
        }
    }
}
