using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using MyException = SimpleFramework.ExceptionHandler;

namespace QueryManager.Logic.Forms
{
    internal class About
    {
        internal const string FormType = "QM_About";

        internal const string ButtonOK = "BtnSubmit";

        internal const string LabelVersion = "LbVersion";
        internal const string LabelAuthors = "LbAuthors";
        internal const string LabelLicense = "LbLicense";
        internal const string LabelCopyRight = "LbCopyRigh";

        internal string XMLSource = Properties.Resources.XML_About;

        internal bool Loaded { get; private set; }

        internal SAPbouiCOM.Form Form { get; private set; }

        internal SAPbouiCOM.StaticText Label(string uid)
        {
            return Form.Items.Item(uid).Specific as SAPbouiCOM.StaticText;
        }

        internal void Load()
        {
            try
            {
                SAPbouiCOM.FormCreationParams fcp = Basic.Application.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_FormCreationParams) as SAPbouiCOM.FormCreationParams;

                fcp.FormType = FormType;
                fcp.UniqueID = Guid.NewGuid().ToString().Replace("-", string.Empty);
                fcp.XmlData = XMLSource;

                Form = Basic.Application.Forms.AddEx(fcp);

                Label(LabelVersion).Caption = Application.ProductVersion;
                Label(LabelAuthors).Caption = "NzzNzz Solutions";
                Label(LabelLicense).Caption = "Free";
                Label(LabelCopyRight).Caption = "Copyright © NzzNzz Solutions 2013";

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
                                    case ButtonOK:
                                        Form.Close();
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
    }
}