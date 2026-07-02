namespace StockAssist.StockCrawler.Services
{
    public interface IDialogService
    {
        void ShowError(string message, string title = "Error");
        void ShowWarning(string message, string title = "Warning");
        string SelectFolder();
    }
}