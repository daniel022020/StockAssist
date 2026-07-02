using System.Threading.Tasks;

namespace StockAssist.StockCrawler.Services
{
    public interface ICsvExportService
    {
        string CleanJsonStringFormat(string jsonString, out string errorString);
        void ExportCsv(string jsonString, string folderPath, string dateFormat);
    }
}