using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MyException = SimpleFramework.ExceptionHandler;

namespace QueryManager.Logic.Forms.System
{
    /// <summary>
    /// SAP-Form: Abfrage speichern
    /// </summary>
    internal static class Form957
    {
        const string FormType = "957";

        internal static void Connect()
        {
            Basic.Application.ItemEvent += Application_ItemEvent;
        }

        internal static void Disconnect()
        {
            Basic.Application.ItemEvent -= Application_ItemEvent;
        }

        internal static SAPbouiCOM.EditText TextBox(SAPbouiCOM.Form form, string uid)
        {
            return form.Items.Item(uid).Specific as SAPbouiCOM.EditText;
        }

        internal static void SetQueryProperties(SAPbouiCOM.Form form, string category, string queryName)
        {
            TextBox(form, "8").Value = queryName;
            TextBox(form, "20").Value = category;
        }

        internal static void Application_ItemEvent(string FormUID, ref SAPbouiCOM.ItemEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;

            try
            {
                if (pVal.FormTypeEx.Equals(FormType))
                {
                    switch (pVal.EventType)
                    { 
                        case SAPbouiCOM.BoEventTypes.et_FORM_LOAD:

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