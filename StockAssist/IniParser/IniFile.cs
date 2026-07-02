using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using StockAssist.Log;

namespace StockAssist.IniParser
{
    public static class IniFile
    {
        private static readonly string IniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

        // 儲存目前記憶體中的設定值，外部可讀取
        public static IniConfig Current { get; private set; }

        static IniFile()
        {
            // 初始時自動讀取
            Current = Load();
        }

        // 讀取 INI
        public static IniConfig Load()
        {
            // 若檔案不存在，則回傳預設值
            if (!File.Exists(IniPath))
            {
                Current = new IniConfig();
                return Current;
            }

            try
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddIniFile("config.ini", optional: true, reloadOnChange: false)
                    .Build();

                var config = new IniConfig();
                configuration.Bind(config);

                // 同步更新記憶體中的全域變數
                Current = config;

                return config;
            }
            catch (Exception ex)
            {
                LogService.Logger.Fatal(ex, "[Exception] Load ini file fail");

                Current = new IniConfig();
                return Current;
            }
        }

        // 儲存 INI
        public static void Save(IniConfig config)
        {
            if (config == null) 
                return;

            try
            {
                using (var writer = new StreamWriter(IniPath, false, Encoding.UTF8))
                {
                    // StockCrawler
                    writer.WriteLine($"[{nameof(IniConfig.StockCrawlerSettings)}]");
                    writer.WriteLine($"{nameof(StockCrawlerSettingSection.tickerList)}={config.StockCrawlerSettings.tickerList}");
                    writer.WriteLine($"{nameof(StockCrawlerSettingSection.isAllHistory)}={config.StockCrawlerSettings.isAllHistory}");
                    writer.WriteLine($"{nameof(StockCrawlerSettingSection.startDate)}={config.StockCrawlerSettings.startDate}");
                    writer.WriteLine($"{nameof(StockCrawlerSettingSection.endDate)}={config.StockCrawlerSettings.endDate}");
                    writer.WriteLine($"{nameof(StockCrawlerSettingSection.isOutputCsv)}={config.StockCrawlerSettings.isOutputCsv}");
                    writer.WriteLine($"{nameof(StockCrawlerSettingSection.csvPath)}={config.StockCrawlerSettings.csvPath}");
                    writer.WriteLine($"{nameof(StockCrawlerSettingSection.dateFormatStr)}={config.StockCrawlerSettings.dateFormatStr}");
                    writer.WriteLine();

                    // SystemSettings
                    writer.WriteLine($"[{nameof(IniConfig.SystemSettings)}]");
                    writer.WriteLine($"{nameof(SystemSettingsSection.languageSetting)}={config.SystemSettings.languageSetting}");
                }

                // 儲存成功後，將記憶體中的設定同步更新
                Current = config;
            }
            catch (Exception ex)
            {
                LogService.Logger.Fatal(ex, "[Exception] Save ini file fail");
            }
        }
    }
}