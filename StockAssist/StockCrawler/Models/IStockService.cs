using System.Threading.Tasks;

namespace StockAssist.StockCrawler.Models
{
    public interface IStockService
    {
        void StartStockCrawler();
        void StopStockCrawler();
        Task<string> GetStockDataAsync(string url);
    }
}