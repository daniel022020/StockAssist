using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockAssist.StockCrawler.ViewModels;
using StockAssist.Setting.ViewModels;
using StockAssist.Log;
using StockAssist.StockCrawler.Models;
using StockAssist.IniParser;
using StockAssist.StockCrawler.Services;

namespace StockAssist.MainWindow.ViewModels
{
    public class MainViewModel : ObservableRecipient
    {
        private readonly IStockService _stockService;
        private readonly IDataService _dataService;
        private readonly IDialogService _dialogService;

        // StockCrawler ViewModel
        public StockCrawlerViewModel StockCrawlerVM { get; }

        // Setting ViewModel
        public SettingViewModel SettingVM { get; }

        public LanguageService LangService => LanguageService.Instance;
        public RelayCommand ToggleMenuCommand { get; }
        public RelayCommand<string> NavigateCommand { get; }

        // 視窗關閉的 Command
        public RelayCommand WindowClosingCommand { get; }

        public MainViewModel()
        {
            _stockService = new StockService();
            _dataService = new DataService();
            _dialogService = new DialogService();

            // 頁面初始化
            StockCrawlerVM = new StockCrawlerViewModel(_stockService, _dataService, _dialogService);
            SettingVM = new SettingViewModel();

            // 預設 0  (StockCrawler)
            _selectedViewIndex = 0;

            ToggleMenuCommand = new RelayCommand(() => IsMenuOpen = !IsMenuOpen);
            NavigateCommand = new RelayCommand<string>(Navigate);

            // 綁定關閉邏輯
            WindowClosingCommand = new RelayCommand(OnWindowClosing);

            LogService.Logger.Info("Initial StockAssist...");
        }

        private void OnWindowClosing()
        {
            IniFile.Save(IniFile.Current);
            LogService.Logger.Info("Close Window ...");
            StockCrawlerVM?.OnViewClosing();
        }

        private bool _isMenuOpen = false;
        public bool IsMenuOpen
        {
            get => _isMenuOpen;
            set => SetProperty(ref _isMenuOpen, value);
        }

        // UI畫面上顯示頁面
        private int _selectedViewIndex;
        public int SelectedViewIndex
        {
            get => _selectedViewIndex;
            set => SetProperty(ref _selectedViewIndex, value);
        }

        // 頁面切換
        private void Navigate(string target)
        {
            if (target == "StockCrawler")
                SelectedViewIndex = 0;
            else if (target == "Setting")
                SelectedViewIndex = 1;
        }
    }
}