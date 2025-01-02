using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MyException = SimpleFramework.ExceptionHandler;

namespace QueryManager.Logic
{
    internal static class Menu
    {
        internal static bool Create()
        {
            SAPbouiCOM.Form formMenu = null;
            try
            {
                formMenu = Basic.Application.Forms.GetForm("169", 1);

                formMenu.Freeze(true);

                SAPbouiCOM.MenuItem menuModules = Basic.Application.Menus.Item("43520");

                SAPbouiCOM.MenuItem mainFolder = null;

                if (menuModules.SubMenus.Exists("B1AdminTools"))
                    mainFolder = menuModules.SubMenus.Item("B1AdminTools");
                else
                    mainFolder = menuModules.SubMenus.Add("B1AdminTools", "B1AdminTools", SAPbouiCOM.BoMenuType.mt_POPUP, 99);

                if (mainFolder.SubMenus.Exists("QM_MenuFolder"))
                    mainFolder.SubMenus.RemoveEx("QM_MenuFolder");

                SAPbouiCOM.MenuItem addOnFolder = mainFolder.SubMenus.Add("QM_MenuFolder", "QueryManager", SAPbouiCOM.BoMenuType.mt_POPUP, 99);

                SAPbouiCOM.MenuItem menuItem = null;

                //menuItem = menuFolder.SubMenus.Add("QM_4865", "Abfragemanager ...", SAPbouiCOM.BoMenuType.mt_STRING, 99);
                //menuItem = menuFolder.SubMenus.Add("QM_4102", "Abfragegenerator", SAPbouiCOM.BoMenuType.mt_STRING, 99);
                //menuItem = menuFolder.SubMenus.Add("QM_4103", "Abfrageassistent", SAPbouiCOM.BoMenuType.mt_STRING, 99);

                menuItem = addOnFolder.SubMenus.Add("QM_SearchQuery", "Abfrage suchen", SAPbouiCOM.BoMenuType.mt_STRING, 99);
                menuItem = addOnFolder.SubMenus.Add("QM_About", "Über ...", SAPbouiCOM.BoMenuType.mt_STRING, 99);

                menuItem = addOnFolder.SubMenus.Add("QM_Exit", "Add-on beenden", SAPbouiCOM.BoMenuType.mt_STRING, 99); 
                
                //menuItem = addOnFolder.SubMenus.Add("QM_Test", "0123456789012345678901234567890123456789_0123456789", SAPbouiCOM.BoMenuType.mt_STRING, 99);

                formMenu.Freeze(false);

                // Erzeuge Dummy Eintrag, da Form.Freeze() das Laden des Menü verhindert und es so neu geladen wird
                addOnFolder.SubMenus.Add("QM_Dummy", "", SAPbouiCOM.BoMenuType.mt_STRING, 99);
                // entferne DummyEintrag wieder
                addOnFolder.SubMenus.RemoveEx("QM_Dummy");

                return true;
            }
            catch (Exception ex)
            {
                MyException.HandleInternal(MethodBase.GetCurrentMethod(), ex);
                return false;
            }
            finally
            {
                formMenu.Freeze(false); 
            }
        }

        internal static bool Remove()
        {
            try
            {
                SAPbouiCOM.MenuItem menuModules = Basic.Application.Menus.Item("43520");

                if (menuModules.SubMenus.Exists("QM_MenuFolder"))
                    menuModules.SubMenus.RemoveEx("QM_MenuFolder");

                return true;
            }
            catch (Exception ex)
            {
                MyException.HandleInternal(MethodBase.GetCurrentMethod(), ex);
                return false;
            }
        }

        internal static void Application_MenuEvent(ref SAPbouiCOM.MenuEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;

            try
            {
                if (pVal.BeforeAction)
                {
                    if (pVal.MenuUID.StartsWith("QM_"))
                    {
                        string menuID = pVal.MenuUID.Substring("QM_".Length, pVal.MenuUID.Length - "QM_".Length);
                        switch (menuID)
                        {
                            case "4865":
                            case "4103":
                            case "4102":
                                // Systemformulare öffnen
                                Basic.Application.ActivateMenuItem(menuID);
                                break;


                            case "SearchQuery":
                                Forms.SearchQuery formQuery = new Forms.SearchQuery();
                                formQuery.Load();
                                break;
                            case "About":
                                Forms.About formAbout = new Forms.About();
                                formAbout.Load();
                                break;


                            case "Exit":
                                Basic.CallAddonClosed();
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MyException.HandleInternal(MethodBase.GetCurrentMethod(), ex);
            }
        }
    }
}