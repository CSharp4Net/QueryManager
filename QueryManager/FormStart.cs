using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using MyException = SimpleFramework.ExceptionHandler;

namespace QueryManager
{
    public partial class FormStart : Form
    {
        public FormStart()
        {
            InitializeComponent();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            try
            {
                SimpleFramework.Basic.Initialize(Application.ProductName, "QueryManager");

                if (QueryManager.Logic.Basic.Connect(Environment.GetCommandLineArgs()))
                {
                    QueryManager.Logic.Basic.Run();
                    QueryManager.Logic.Basic.AddonClosed += Basic_AddonClosed;
                }
                else
                    Application.Exit();
            }
            catch (Exception ex)
            {
                MyException.HandleInternal(MethodBase.GetCurrentMethod(), ex);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            try
            {
                // do something
            }
            catch (Exception ex)
            {
                MyException.HandleInternal(MethodBase.GetCurrentMethod(), ex);
            }
        }

        private void ItemExit_Click(object sender, EventArgs e)
        {
            try
            {
                QueryManager.Logic.Basic.Stop();

                Application.Exit();
            }
            catch (Exception ex)
            {
                MyException.HandleInternal(MethodBase.GetCurrentMethod(), ex);
            }
        }

        private void ItemAbout_Click(object sender, EventArgs e)
        {
            try
            {
                // do something
            }
            catch (Exception ex)
            {
                MyException.HandleInternal(MethodBase.GetCurrentMethod(), ex);
            }
        }

        private void Basic_AddonClosed(object sender, EventArgs e)
        {
            try
            {
                QueryManager.Logic.Basic.Stop();

                Application.Exit();
            }
            catch (Exception ex)
            {
                MyException.HandleInternal(MethodBase.GetCurrentMethod(), ex);
            }
        }

        private void ItemAppData_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", SimpleFramework.Basic.AppDataFolder);
            }
            catch (Exception ex)
            {
                MyException.HandleInternal(MethodBase.GetCurrentMethod(), ex);
            }
        }
    }
}