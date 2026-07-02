using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using StockAssist.Languages;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using StockAssist.IniParser;
using StockAssist.Log;
using System.Linq;
using StockAssist.Setting.ViewModels;

public class LanguageService : ObservableObject
{
    public static LanguageService Instance { get; } = new LanguageService();

    // 當前載入語系
    private string _currentLanguageCode;
    public string CurrentLanguageCode
    {
        get => _currentLanguageCode;
        private set => SetProperty(ref _currentLanguageCode, value);
    }

    // 當前語言包
    private Dictionary<string, string> _resources;
    public Dictionary<string, string> Resources
    {
        get => _resources;
        private set => SetProperty(ref _resources, value);
    }

    // 初始化時根據預設載入
    private LanguageService()
    {
        string initialLanguage = IniFile.Current.SystemSettings.languageSetting;

        // 載入INI儲存的語系指定檔
        LoadLanguage(initialLanguage);
    }

    // 預設語言包
    private static Dictionary<string, string> GetDefaultResourcesDictionary()
    {
        return new Dictionary<string, string>
        {
            { "StockCrawler", "Stock Crawler" },
            { "Settings", "Settings" },
            { "StockCrawlerSystem", "📊 Stock Crawler System" },
            { "TickerSymbol", "Ticker Symbol" },
            { "Start", "Start" },
            { "Processing", " Processing..." },
            { "AllHistory", "All History" },
            { "From:", "From:" },
            { "To:", "To:" },
            { "SelectDate", "Select Date" },
            { "OutputCsv", "Output Csv" },
            { "DateFormat", "Date Format " },
            { "List", "List" },
            { "SystemSetting", "🛠 System Settings" },
            { "LanguageSelect", "Language" },
            { "Placeholder", "Please select..." },
            { "BackendServiceError", "Backend service error" },
            { "StopBackendServiceError", "Stop backend service error" },
            { "PleaseAddTickerSymbol", "Please add ticker symbol!" },
            { "AddTickerError", "Add Ticker Error" },
            { "StartCrawlDataError", "Start crawl data error" },
            { "SelectFolderError", "Select folder error" },
            { "OutputCsvError", "Output csv error" },
            { "OutputCsvPathFormatError", "Output csv path error! (empty or not exists)" },
            { "InitialCrawlSettingsError", "Initial crawl settings error" },
            { "TickerError", "Ticker error!" },
            { "AtLeastTwoWorkingDays", "At least two working days!" },
            { "Hint", "(Please Input ticker symbol (ex:2330.TW) and add to list)" }
        };
    }

    // 找不到檔案、或解析失敗時載入系統預設
    private void SetDefaultFallbackResources()
    {
        Resources = GetDefaultResourcesDictionary();
        CurrentLanguageCode = "en-US";
    }

    // 載入語言包
    public void LoadLanguage(string languageCode)
    {
        // 切換語系與當前載入一樣，就跳過
        if (CurrentLanguageCode == languageCode && Resources != null)
            return;

        // 多國語言檔案路徑
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages", "Resource", $"{languageCode}.json");

        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (result != null && result.Count > 0)
                {
                    Resources = result;
                    CurrentLanguageCode = languageCode; // 成功載入並更新語系

                    // 儲存LanguageSetting的INI
                    IniFile.Current.SystemSettings.languageSetting = languageCode;
                    return;
                }
            }
            catch (Exception ex)
            {
                LogService.Logger.Fatal(ex, "[Exception] Load language fail");
            }
        }
        else
        {
            // 找不到檔案則讀取預設
            SetDefaultFallbackResources();
        }
    }

    // UI初始化語言選項使用
    public List<LanguageOption> LoadLanguageOptions()
    {
        // 多國語言語系目錄儲存檔
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages", "List", "languages.json");

        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                var result = JsonConvert.DeserializeObject<List<LanguageOption>>(json);

                if (result != null && result.Count > 0)
                {
                    var validOptions = result
                        .Where(x => !string.IsNullOrWhiteSpace(x.DisplayName) &&
                                    !string.IsNullOrWhiteSpace(x.LanguageCode))
                        .ToList();

                    if (validOptions.Count > 0)
                    {
                        return validOptions;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Logger.Fatal(ex, "[Exception] Load language options fail");
            }
        }

        // 找不到檔案則讀取預設
        return new List<LanguageOption>
        {
            new LanguageOption { DisplayName = "English", LanguageCode = "en-US" }
        };
    }

    // 外部取得多國語言文字的方法
    public string GetLanguageString(string key)
    {
        // 先從當前載入的語系尋找
        if (Resources != null && Resources.TryGetValue(key, out string value))
        {
            return value;
        }

        // 若找不到，載入預設值
        var fallbackResources = GetDefaultResourcesDictionary();
        if (fallbackResources != null && fallbackResources.TryGetValue(key, out string fallbackValue))
        {
            return fallbackValue;
        }

        // 都找不到則直接回傳
        return key;
    }
}