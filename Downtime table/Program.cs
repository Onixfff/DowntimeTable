using NLog;
using System;
using System.Windows.Forms;

namespace Downtime_table
{
    internal static class Program
    {
        private static ILogger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new Form1(_logger));
        }
    }
}
