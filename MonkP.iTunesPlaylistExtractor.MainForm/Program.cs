using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MonkP.iTunesPlaylistExtractor.MainForm
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(LogHelper.LogLevel.Fatal, "Unhandled exception, application exiting.", ex);
                throw;
            }
            finally
            {
                // 正常退出前确保队列中的日志全部写完
                LogHelper.Shutdown();
            }
        }
    }
}
