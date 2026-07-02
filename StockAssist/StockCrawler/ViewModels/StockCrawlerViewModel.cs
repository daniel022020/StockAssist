using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using StockAssist.IniParser;
using StockAssist.Languages;
using StockAssist.Log;
using StockAssist.StockCrawler.Models;
using StockAssist.StockCrawler.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace StockAssist.StockCrawler.ViewModels
{
    // 股票爬蟲功能的 ViewModel
    public class StockCrawlerViewModel : ObservableObject
    {
        // 多國語言
        public LanguageService LangService => LanguageService.Instance;

        private readonly IStockService _stockService;         // 爬蟲服務
        private readonly ICsvExportService _csvExportService; // 資料與 CSV 服務
        private readonly IDialogService _dialogService;       // 視窗對話服務
        private int _maxListCount = 10;

        // 用於確認WedView以及exe串接均已就緒時才啟用載入按鈕
        private bool _isWebViewReady = false;
        private bool _isStockCrawlerReady = false;

        // UI 橋接委派：通知 View 執行前端 JavaScript 繪圖
        public Func<string, Task> RequestExecuteJs { get; set; }

        // UI 橋接委派：通知 View 的 WebView 導向 HTML 路徑
        public Action RequestNavigate { get; set; }
        public RelayCommand AddTickerCommand { get; }
        public RelayCommand<string> RemoveTickerCommand { get; }
        public AsyncRelayCommand StartCommand { get; }
        public RelayCommand SelectCsvFollderActionCommand { get; }

        // 初始化
        public StockCrawlerViewModel(
            IStockService stockService,
            ICsvExportService csvExportService,
            IDialogService dialogService)
        {
            _stockService = stockService;
            _csvExportService = csvExportService;
            _dialogService = dialogService;

            InitialCrawlSettings();

            AddTickerCommand = new RelayCommand(AddTicker);
            RemoveTickerCommand = new RelayCommand<string>(RemoveTicker);
            StartCommand = new AsyncRelayCommand(StartCrawlDataAsync);
            SelectCsvFollderActionCommand = new RelayCommand(SelectCsvFollderAction);
        }

        private ObservableCollection<string> _tickerList = new ObservableCollection<string>();

        // 股票代碼清單
        public ObservableCollection<string> TickerList
        {
            get => _tickerList;
            set => SetProperty(ref _tickerList, value);
        }

        private string _txtTicker;

        // UI輸入框中的股票代碼
        public string TxtTicker
        {
            get => _txtTicker;
            set => SetProperty(ref _txtTicker, value);
        }

        private bool _isAllHistory = false;

        // 是否爬取所有歷史資料
        public bool IsAllHistory
        {
            get => _isAllHistory;
            set
            {
                if (SetProperty(ref _isAllHistory, value))
                {
                    OnPropertyChanged(nameof(DatePanelVisibility));

                    // 儲存Csv Path的 INI
                    IniFile.Current.StockCrawlerSettings.isAllHistory = _isAllHistory;
                }
            }
        }

        // 時間區間設定（選取所有歷史則隱藏）
        public Visibility DatePanelVisibility => IsAllHistory ? Visibility.Collapsed : Visibility.Visible;

        private bool _isOutputCsv = false;

        // 是否輸出CSV檔
        public bool IsOutputCsv
        {
            get => _isOutputCsv;
            set
            {
                if (SetProperty(ref _isOutputCsv, value))
                {
                    OnPropertyChanged(nameof(CsvPathVisibility));

                    // 儲存 isOutputCsv 的 INI
                    IniFile.Current.StockCrawlerSettings.isOutputCsv = _isOutputCsv;
                }
            }
        }

        // CSV路徑設定（不勾選CSV則隱藏）
        public Visibility CsvPathVisibility => IsOutputCsv ? Visibility.Visible : Visibility.Collapsed;

        private DateTime _startDate;

        // 爬取起始日期
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    // 儲存 startDate 的 INI
                    IniFile.Current.StockCrawlerSettings.startDate = _startDate.ToString("yyyy-MM-dd");
                }
            }
        }

        private DateTime _endDate;

        // 爬取結束日期
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    // 儲存 endDate 的 INI
                    IniFile.Current.StockCrawlerSettings.endDate = _endDate.ToString("yyyy-MM-dd");
                }
            }
        }

        private bool _isEnableAddTicker = true;

        // 增添股票號按鈕是否可用（當清單額滿時為 false）
        public bool IsEnableAddTicker
        {
            get => _isEnableAddTicker;
            set => SetProperty(ref _isEnableAddTicker, value);
        }

        private bool _isEnableStart = false;

        // 載入按鈕是否可用（當WebView與後端exe皆 Ready 時為 true）
        public bool IsEnableStart
        {
            get => _isEnableStart;
            set => SetProperty(ref _isEnableStart, value);
        }

        // Csv檔案路徑
        private string _csvFolderPath;
        public string CsvFolderPath
        {
            get => _csvFolderPath;
            set => SetProperty(ref _csvFolderPath, value);
        }

        // 提示文字 (添加代號)
        private string _hint_AddTicker;
        public string Hint_AddTicker
        {
            get => _hint_AddTicker;
            set => SetProperty(ref _hint_AddTicker, value);
        }

        private bool _isProcessing = false;

        // 是否正在載入資料
        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (SetProperty(ref _isProcessing, value))
                {
                    OnPropertyChanged(nameof(StartButtonVisibility));
                    OnPropertyChanged(nameof(ProcessingVisibility));
                }
            }
        }

        // 下拉選單的日期格式清單
        public ObservableCollection<string> DateFormats { get; set; }

        // 目前選中的日期格式
        private string _selectedDateFormat;
        public string SelectedDateFormat
        {
            get => _selectedDateFormat;
            set
            {
                if (_selectedDateFormat != value)
                {
                    _selectedDateFormat = value;
                    OnPropertyChanged();

                    IniFile.Current.StockCrawlerSettings.dateFormatStr = _selectedDateFormat;
                }
            }
        }

        private string _listCount;

        // 清單數量 (UI顯示)
        public string ListCount
        {
            get => _listCount;
            set => SetProperty(ref _listCount, value);
        }

        // 控制 開始處理 按鈕的可見度（載入中就隱藏）
        public Visibility StartButtonVisibility => IsProcessing ? Visibility.Collapsed : Visibility.Visible;

        // 控制 處理中 控制項的可見度（載入中才顯示）
        public Visibility ProcessingVisibility => IsProcessing ? Visibility.Visible : Visibility.Collapsed;

        // 初始化設定
        private void InitialCrawlSettings()
        {
            LogService.Logger.Info("Initial crawl settings");

            try
            {
                var settings = IniFile.Current.StockCrawlerSettings;

                // 初始化 Ticker 清單
                if (!string.IsNullOrWhiteSpace(settings.tickerList))
                {
                    TickerList = JsonConvert.DeserializeObject<ObservableCollection<string>>(settings.tickerList);

                    // 如果清單已滿，就禁止加入
                    if (TickerList.Count >= _maxListCount)
                        IsEnableAddTicker = false;
                }
                else
                {
                    TickerList = new ObservableCollection<string>();
                }

                // 更新清單數量顯示
                ListCount = $"({TickerList.Count}/{_maxListCount})";

                IsAllHistory = settings.isAllHistory;
                IsOutputCsv = settings.isOutputCsv;
                CsvFolderPath = settings.csvPath;


                // 解析起始日期 (StartDate)
                if (DateTime.TryParse(settings.startDate, out var parsedStart))
                {
                    StartDate = parsedStart;
                }
                else
                {
                    StartDate = DateTime.Now.AddYears(-1); // 預設一年前
                }


                // 解析結束日期 (EndDate)
                if (DateTime.TryParse(settings.endDate, out var parsedEnd))
                {
                    EndDate = parsedEnd;
                }
                else
                {
                    EndDate = DateTime.Now; // 預設今天
                }


                // 初始化日期格式清單
                DateFormats = new ObservableCollection<string>
                                {
                                    "yyyyMMdd",
                                    "yyyy/MM/dd",
                                };


                // 檢查並設定日期格式
                if (DateFormats.Contains(settings.dateFormatStr))
                {
                    SelectedDateFormat = settings.dateFormatStr;
                }
                else
                {
                    SelectedDateFormat = DateFormats[0]; // 預設使用第一個 ("yyyyMMdd")
                }

                LogService.Logger.Info("Initial crawl settings success");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(LangService.GetLanguageString("InitialCrawlSettingsError") + $":{ex.Message}", "Error");
                LogService.Logger.Fatal(ex, "[Exception] Initial crawl settings fail");
            }
        }

        // 畫面載入完成時觸發，於背景執行緒啟動 Python 爬蟲後端服務
        public async Task OnViewLoadedAsync()
        {
            if (_isStockCrawlerReady) return;

            await Task.Run(() =>
            {
                try
                {
                    LogService.Logger.Info("Initial backend service");

                    _stockService.StartStockCrawler();

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        UpdateSystemReadyStatus(webViewReady: false, stockCrawlerReady: true);
                    });

                    LogService.Logger.Info("Backend service is ready!");
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        _dialogService.ShowError(LangService.GetLanguageString("BackendServiceError") + $":{ex.Message}", "Error");
                    });
                    LogService.Logger.Fatal(ex, "[Exception] Backend service fail, Mesage");
                }
            });
        }

        // 畫面關閉時觸發，用以中斷並關閉爬蟲服務 (StockCrawler.exe)
        public void OnViewClosing()
        {
            try
            {
                LogService.Logger.Info("Close backend service");

                _stockService.StopStockCrawler();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(LangService.GetLanguageString("StopBackendServiceError") + $":{ex.Message}", "Error");
                LogService.Logger.Fatal(ex, "[Exception] Close backend service fail");
            }
        }

        // 將目前UI輸入框的股票代碼加入至清單
        private void AddTicker()
        {
            if (!IsEnableAddTicker)
                return;

            try
            {
                string ticker = TxtTicker?.Trim().ToUpper();

                if (!string.IsNullOrEmpty(ticker))
                {
                    if (!TickerList.Contains(ticker))
                    {
                        TickerList.Add(ticker);
                        ListCount = $"({TickerList.Count}/{_maxListCount})";
                        LogService.Logger.Info($"Add ticker:{ticker} to list");

                        string tickerString = JsonConvert.SerializeObject(TickerList);

                        // 儲存AddTicker的INI
                        IniFile.Current.StockCrawlerSettings.tickerList = tickerString;
                    }

                    TxtTicker = string.Empty;

                    // 清單限制10筆
                    if (TickerList.Count >= _maxListCount)
                    {
                        LogService.Logger.Info($"Ticker up to max count{_maxListCount}");
                        IsEnableAddTicker = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(LangService.GetLanguageString("AddTickerError") + $":{ex.Message}", "Error");
                LogService.Logger.Fatal(ex, "[Exception] Add ticker fail");
            }
        }

        // 從清單中移除特定的股票代碼
        private void RemoveTicker(string ticker)
        {
            TickerList.Remove(ticker);
            ListCount = $"({TickerList.Count}/{_maxListCount})";
            LogService.Logger.Info($"Remove ticker:{ticker} from list");

            string tickerString = JsonConvert.SerializeObject(TickerList);

            // 儲存AddTicker的INI
            IniFile.Current.StockCrawlerSettings.tickerList = tickerString;

            if (TickerList.Count < _maxListCount)
                IsEnableAddTicker = true;
        }

        // 非同步從本地後端 API 獲取股票 JSON 資料，並要求 View 呼叫 JS 刷新前端圖表
        private async Task StartCrawlDataAsync()
        {
            LogService.Logger.Info("Start crawl data");

            if (!CheckStatusBeforeStart())
                return;

            IsEnableStart = false;
            IsProcessing = true;

            // 記錄目前步奏，回報錯誤使用
            string csvErrorMessage = string.Empty;

            try
            {
                string combined = string.Join(",", _tickerList);
                string url = $"http://localhost:5000/api/stock?tickers={Uri.EscapeDataString(combined)}";

                if (IsAllHistory)
                {
                    url += "&period=max";
                }
                else
                {
                    // 將 EndDate 自動加 1 天，因為Yifinance會把EndDay從凌晨算
                    DateTime adjustedEndDate = EndDate.AddDays(1);
                    url += $"&start={StartDate:yyyy-MM-dd}&end={adjustedEndDate:yyyy-MM-dd}";
                }

                LogService.Logger.Info("Get data from StockCrawler.exe");

                // API 網路請求
                string rawJsonString = await _stockService.GetStockDataAsync(url);

                if (string.IsNullOrWhiteSpace(rawJsonString))
                {
                    LogService.Logger.Error("jsonString is null or empty!");
                    IsProcessing = false;
                    IsEnableStart = true;
                    return;
                }

                // JSON 格式清洗
                string jsonString = string.Empty;
                string errorString = string.Empty;

                await Task.Run(() =>
                {
                    jsonString = _csvExportService.CleanJsonStringFormat(rawJsonString, out errorString);
                });

                LogService.Logger.Info("Update webview");

                // 執行前端網頁 JS
                if (RequestExecuteJs != null)
                    await RequestExecuteJs.Invoke(jsonString);

                // CSV 匯出
                if (IsOutputCsv)
                {
                    // 複製設定值，避免跨執行緒衝突
                    string currentFormat = SelectedDateFormat;
                    string currentPath = CsvFolderPath;

                    try
                    {
                        await Task.Run(() => { _csvExportService.ExportCsv(jsonString, currentPath, currentFormat); });
                    }
                    catch (Exception csvEx)
                    {
                        LogService.Logger.Fatal(csvEx, "[Exception] Output csv fail");
                        csvErrorMessage = LangService.GetLanguageString("OutputCsvError") + $":{csvEx.Message}";
                    }
                }

                // 切換狀態
                IsProcessing = false;
                IsEnableStart = true;

                if (!string.IsNullOrWhiteSpace(errorString))
                    _dialogService.ShowWarning(errorString, "Warn");
                if (!string.IsNullOrWhiteSpace(csvErrorMessage)) 
                    _dialogService.ShowWarning(csvErrorMessage, "CSV Export Warning");
            }
            catch (Exception ex)
            {
                IsProcessing = false;
                IsEnableStart = true;

                _dialogService.ShowError(LangService.GetLanguageString("StartCrawlDataError") + $":{ex.Message}", "Error");
                LogService.Logger.Fatal(ex, "[Exception] Start crawl data fail");
            }
        }

        private bool CheckStatusBeforeStart()
        {
            bool result = true;
            string erroMsg = string.Empty;

            // 股票代號清單為0
            if (TickerList != null && TickerList.Count == 0)
            {
                erroMsg += $"{LangService.GetLanguageString("PleaseAddTickerSymbol")}\n";
            }

            // 輸出 Csv 開啟
            if (IsOutputCsv)
            {
                // 空值 或 不存在
                if (string.IsNullOrWhiteSpace(CsvFolderPath) || !Directory.Exists(CsvFolderPath))
                {
                    erroMsg += $"{LangService.GetLanguageString("OutputCsvPathFormatError")}";
                }
            }

            if (!string.IsNullOrWhiteSpace(erroMsg))
            {
                _dialogService.ShowWarning(erroMsg, "Warning");
                result = false;
            }

            return result;
        }

        private void SelectCsvFollderAction()
        {
            LogService.Logger.Info("Start select csv follder");

            string selectedPath = _dialogService.SelectFolder();

            if (!string.IsNullOrEmpty(selectedPath))
            {
                try
                {
                    CsvFolderPath = selectedPath;
                    LogService.Logger.Info($"Follder Path:{CsvFolderPath}");

                    // 儲存Csv Path的 INI
                    IniFile.Current.StockCrawlerSettings.csvPath = CsvFolderPath;
                }
                catch (Exception ex)
                {
                    _dialogService.ShowError(LangService.GetLanguageString("SelectFolderError") + $":{ex.Message}", "Error");
                    LogService.Logger.Fatal(ex, "[Exception] Select folder fail");
                }
            }
            else
            {
                _dialogService.ShowError(LangService.GetLanguageString("SelectFolderError"));
                LogService.Logger.Fatal("selectedPath is null or empty!");
            }
        }

        // 更新系統準備就緒狀態旗擺，秀出開始按鈕
        public void UpdateSystemReadyStatus(bool webViewReady, bool stockCrawlerReady)
        {
            if (webViewReady) _isWebViewReady = true;
            if (stockCrawlerReady) _isStockCrawlerReady = true;

            if (_isWebViewReady && _isStockCrawlerReady)
            {
                IsEnableStart = true;
            }
        }
    }
}