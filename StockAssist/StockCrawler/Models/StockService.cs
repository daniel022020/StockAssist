using StockAssist.Log;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace StockAssist.StockCrawler.Models
{
    public class StockService : IStockService
    {
        private Process _pythonProcess;

        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            // 改善冷啟動後第一次交互過慢的問題
            UseProxy = false,
            Proxy = null
        });

        public void StartStockCrawler()
        {
            try
            {
                // 【新增：啟動前先檢查並清理可能殘留的舊 exe，避免佔用 Port】
                KillExistingCrawlersByName();

                // Python exe檔案位置
                string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StockCrawler", "Python", "StockCrawler.exe");

                if (File.Exists(exePath))
                {
                    _pythonProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
                else
                {
                    throw new FileNotFoundException($"Can not find: {exePath}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Backend service initial fail!: {ex.Message}", ex);
            }
        }

        // 透過處理程序名稱強制關閉殘留的 StockCrawler 進程
        private void KillExistingCrawlersByName()
        {
            try
            {
                LogService.Logger.Info("Checking for lingering StockCrawler processes before startup...");

                // 取得所有名稱為 "StockCrawler" 的處理程序
                Process[] lingeringProcesses = Process.GetProcessesByName("StockCrawler");

                foreach (var process in lingeringProcesses)
                {
                    try
                    {
                        LogService.Logger.Info($"Found lingering process ID: {process.Id}, attempting to kill...");

                        // 使用 taskkill 強制結束，避免殘留進程
                        using (Process killProcess = Process.Start(new ProcessStartInfo("taskkill", $"/F /T /PID {process.Id}")
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false
                        }))
                        {
                            killProcess?.WaitForExit(3000); // 最多等待 3 秒
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Logger.Warn($"Failed to kill process {process.Id}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Logger.Warn($"Error while cleaning up existing processes: {ex.Message}");
            }
        }

        public void StopStockCrawler()
        {
            try
            {
                if (_pythonProcess != null && !_pythonProcess.HasExited)
                {
                    // 使用 taskkill 強制結束，避免殘留進程
                    using (Process killProcess = Process.Start(new ProcessStartInfo("taskkill", $"/F /T /PID {_pythonProcess.Id}")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }))
                    {
                        killProcess?.WaitForExit(3000); // 最多等待 3 秒
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Logger.Fatal(ex, "[Exception] Stop stock crawler fail");
            }
        }

        public async Task<string> GetStockDataAsync(string url)
        {
            // 非同步併發獲取 API 文本
            return await _httpClient.GetStringAsync(url);
        }
    }
}