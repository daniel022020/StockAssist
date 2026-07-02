using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using StockAssist.Log;
using StockAssist.StockCrawler.ViewModels;

namespace StockAssist.StockCrawler.Views
{
    public partial class StockCrawler : UserControl
    {
        private StockCrawlerViewModel _viewModel;

        // 用來阻擋 TabControl 切換回來時重複進行初始化
        private bool _isInitialized = false;


        // 初始化
        public StockCrawler()
        {
            InitializeComponent();
        }

        // 用於啟動單次初始化的前端與後端建立流程
        private async void StockCrawler_Loaded(object sender, RoutedEventArgs e)
        {
            // 只要初始化一次
            if (_isInitialized) return;

            if (this.DataContext is StockCrawlerViewModel viewModel)
            {
                _viewModel = viewModel;

                // 依序初始化 WebView2 與通知後端服務啟動
                await SetupWebViewBridgeAsync();
                await _viewModel.OnViewLoadedAsync();

                // 鎖定旗標
                _isInitialized = true;
            }
        }

        // 初始化 WebView2
        private async Task SetupWebViewBridgeAsync()
        {
            try
            {
                LogService.Logger.Info("Initial Webview");

                // 確保 WebView2 底層核心初始化完成
                await StockWebview.EnsureCoreWebView2Async(null);

                // 先解除綁定再重新繫結，防止重複掛載事件
                StockWebview.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
                StockWebview.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

                // 實作 ViewModel 要求執行前端 JavaScript 繪圖的委派邏輯
                _viewModel.RequestExecuteJs = async (json) =>
                {
                    if (StockWebview.CoreWebView2 != null)
                    {
                        await StockWebview.CoreWebView2.ExecuteScriptAsync($"updateCharts({json})");
                    }
                };

                // 實作 ViewModel 要求導航網頁至特定本地圖表 HTML 的委派邏輯
                _viewModel.RequestNavigate = () =>
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StockCrawler", "Draw", "StockPlot.html");
                    if (File.Exists(path) && StockWebview.CoreWebView2 != null)
                    {
                        StockWebview.CoreWebView2.Navigate($"file:///{path}");
                    }
                };

                // 預先觸發一次導航，讓 HTML 開始載入
                _viewModel.RequestNavigate.Invoke();
            }
            catch (Exception ex)
            {
                LogService.Logger.Fatal(ex, "[Exception] Setup webview bridge fail");
            }
        }

        // 當 WebView2 載入完成時觸發
        private void CoreWebView2_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                // 網頁準備完成
                _viewModel.UpdateSystemReadyStatus(webViewReady: true, stockCrawlerReady: false);
                LogService.Logger.Info("Webview is ready!");
            }
            else
            {
                LogService.Logger.Fatal("[Exception]  Core Web View fail");
            }
        }
    }
}