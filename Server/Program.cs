using System;
using System.Threading;
using System.Threading.Tasks;
using IceFireMan.Shared;

namespace IceFireMan.Server
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "森林冰火人 - 服务器";

            PrintBanner();

            int port = GameConfig.DefaultPort;
            
            // 检查命令行参数
            if (args.Length > 0 && int.TryParse(args[0], out int customPort))
            {
                port = customPort;
            }

            var server = new GameServer(port);
            var cts = new CancellationTokenSource();

            // 处理Ctrl+C
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\n正在关闭服务器...");
                cts.Cancel();
            };

            try
            {
                await server.StartAsync(cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"服务器错误: {ex.Message}");
            }

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        static void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════╗
║                                                           ║
║      ❄️  森林冰火人网络版 - 游戏服务器  🔥               ║
║                                                           ║
║      Ice and Fire Man Network Game - Server               ║
║                                                           ║
╚═══════════════════════════════════════════════════════════╝
");
            Console.ResetColor();

            Console.WriteLine("服务器命令：");
            Console.WriteLine("  Ctrl+C - 关闭服务器");
            Console.WriteLine();
        }
    }
}

