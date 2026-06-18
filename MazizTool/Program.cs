using System;
using System.Windows.Forms;

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
                {
                    MessageBox.Show("ThreadException:\n\n" + e.Exception.Message + "\n\n" + e.Exception.StackTrace,
                        "MazizTool Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    var ex = e.ExceptionObject as Exception;
                    MessageBox.Show("Unhandled:\n\n" + (ex?.Message ?? "?") + "\n\n" + (ex?.StackTrace ?? ""),
                        "MazizTool Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };
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
