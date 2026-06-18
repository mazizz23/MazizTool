using System;
using System.Threading;
using System.Windows.Forms;
using MazizTool.Controls;

namespace MazizTool
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) =>
                    MessageBox.Show("ThreadException:\n\n" + e.Exception.Message + "\n\n" + e.Exception.StackTrace,
                        "MazizTool Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    var ex = e.ExceptionObject as Exception;
                    MessageBox.Show("Unhandled:\n\n" + (ex?.Message ?? "?") + "\n\n" + (ex?.StackTrace ?? ""),
                        "MazizTool Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };

                using (var splash = new SplashForm())
                {
                    splash.Show();
                    splash.Update();
                    Thread.Sleep(2200);
                }

                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal:\n\n" + ex.Message + "\n\n" + ex.StackTrace,
                    "MazizTool Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
