using System;
using System.Windows.Forms;

namespace FireboyAndWatergirl.GameClient
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 启用视觉样式
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // 启用高DPI支持
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            // 运行主窗口
            Application.Run(new MainForm());
        }
    }
}

