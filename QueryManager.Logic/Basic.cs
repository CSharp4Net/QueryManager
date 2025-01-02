using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using MyException = SimpleFramework.ExceptionHandler;

namespace QueryManager.Logic
{
    public static class Basic
    {
        public static event EventHandler AddonClosed;

        internal static SAPbouiCOM.Application Application { get; private set; }

        internal static SAPbobsCOM.Company Company { get; private set; }

        internal static string AppDataFolder { get { return SimpleFramework.Basic.AppDataFolder; } }

        internal static string BackUpFolder { get { return Path.Combine(AppDataFolder, "Backup"); } }

        public static bool Connect(string[] commandLineArgs)
        {
            SAPbouiCOM.SboGuiApi api = null;

            try
            {
                api = new SAPbouiCOM.SboGuiApi();

                string connectionString = null;

                if (commandLineArgs.Length > 1)
                {
                    connectionString = commandLineArgs[1];
                }
                else
                    connectionString = "0030002C0030002C00530041005000420044005F00440061007400650076002C0050004C006F006D0056004900490056";

                SimpleFramework.Logger.LogInfo("Verbinde zu SAP B1 ...");

                api.Connect(connectionString);

                Application = api.GetApplication(-1);

                if (Application != null)
                {
                    SimpleFramework.Logger.LogInfo("SAP B1 - UI Anwendung: " + Application.Desktop.Title);

                    Company = (Application.Company.GetDICompany() as SAPbobsCOM.Company);

                    SimpleFramework.Logger.LogInfo("SAP B1 - DI Version: " + Company.Version);

                    SimpleFramework.ExceptionHandler.ExceptionThrowned += ExceptionHandler_ExceptionThrowned;

                    Forms.System.Form957.Connect();

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, SimpleFramework.Basic.AddonName + " - Verbindung zu SAP B1 konnte nicht hergestellt werden!");
                return false;
            }
        }

        public static void Run()
        { 
            // Erzeuge Menü in SAP B1
            Menu.Create();

            // Registriere Events
            Application.MenuEvent += Menu.Application_MenuEvent;

            // Erzeuge Backup-Verzeichnis
            if (!Directory.Exists(BackUpFolder))
                Directory.CreateDirectory(BackUpFolder);

            // Bereingie Arbeitsspeicher
            ClearMemory(Process.GetCurrentProcess());
        }

        public static void Stop()
        {
            Application.MenuEvent -= Menu.Application_MenuEvent;

            Menu.Remove();
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int SetProcessWorkingSetSize(System.IntPtr hProcess, int dwMinimumWorkingSetSize, int dwMaximumWorkingSetSize);
        public static void ClearMemory(System.Diagnostics.Process process)
        {
            SimpleFramework.Logger.LogInfo("Bereinige Arbeitsspeicher ...");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            if ((Environment.OSVersion.Platform == PlatformID.Win32NT))
            {
                SetProcessWorkingSetSize(process.Handle, -1, -1);
            }
        }
        
        static void ExceptionHandler_ExceptionThrowned(System.Reflection.MethodBase method, Exception ex)
        {
            try
            {
                Application.StatusBar.SetText(ex.Message, SAPbouiCOM.BoMessageTime.bmt_Medium, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
            }
            catch (Exception newEx)
            {
                MyException.HandleInternal(MethodBase.GetCurrentMethod(), newEx, true);
            }
        }

        internal static void CallAddonClosed()
        {
            if (AddonClosed != null)
                AddonClosed(null, new EventArgs());
        }
    }
}