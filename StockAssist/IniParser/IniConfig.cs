using System;

namespace StockAssist.IniParser
{
    // INI 類別
    public class IniConfig
    {
        public StockCrawlerSettingSection StockCrawlerSettings { get; set; } = new StockCrawlerSettingSection();
        public SystemSettingsSection SystemSettings { get; set; } = new SystemSettingsSection();
    }

    // INI 子項目
    public class StockCrawlerSettingSection
    {
        public string tickerList { get; set; } = string.Empty;  // 預設空值
        public bool isAllHistory { get; set; } = false;  // 預設false
        public string startDate { get; set; } = string.Empty;  // 預設空值
        public string endDate { get; set; } = string.Empty;  // 預設空值
        public bool isOutputCsv { get; set; } = false;  // 預設false
        public string csvPath { get; set; } = string.Empty;  // 預設空值
        public string dateFormatStr { get; set; } = "yyyyMMdd";  // 預設 yyyyMMdd
    }
    public class SystemSettingsSection
    {
        public string languageSetting { get; set; } = "zh-TW";  // 預設語系
    }
}