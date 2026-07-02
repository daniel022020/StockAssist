using System;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Targets;
using StockAssist.Log;

namespace StockAssist.Log
{
    public class LogService
    {
        public static Logger Logger;

        // 多執行緒，lock 使用
        private static readonly object thislock = new object();

        // 將唯一實例設為 private static
        private static LogService instance;

        // 設為 private，外界不能 new
        private LogService()
        {
        }

        // 外界只能使用靜態方法取得實例
        public static LogService GetInstance()
        {
            // 先判斷目前有沒有實例，沒有的話才開始 lock，
            // 此次的判斷，是避免在有實例的情況，也執行 lock ，影響效能
            if (null == instance)
            {
                // 避免多執行緒可能會產生兩個以上的實例，所以 lock
                lock (thislock)
                {
                    // lock 後，再判斷一次目前有無實例
                    // 此次的判斷，是避免多執行緒，同時通過前一次的 null == instance 判斷
                    if (null == instance)
                    {
                        instance = new LogService();
                    }
                }
            }
            return instance;
        }

        // 初始化 NLog 設定
        public void CreateLogConfig()
        {
            try
            {
                var config = new LoggingConfiguration();

                var fileTarget = new FileTarget
                {
                    FileName = "${basedir}/Logs/${shortdate}.log",

                    Layout = "${date:format=MM/dd HH\\:mm\\:ss.fff} " +                                     // 時間
                             "[${pad:padding=2:inner=${threadid}}]  " +                                    // 執行緒 ID
                             "${pad:padding=-85:inner=${message}} " +                                       // 訊息本文
                             "[${pad:padding=5:inner=${level:uppercase=true}}]" +                           // Log等級
                             "[${callsite:cleanNames=true:className=false:methodName=true} ${logger}]",     // [方法名 類別名]

                    Encoding = Encoding.UTF8,
                    ArchiveSuffixFormat = ".{#}",
                    ArchiveAboveSize = 10000000,    // Log 檔案最大 10MB
                    MaxArchiveFiles = 10            // 最多 10 個 Log 檔案
                };

                config.AddRule(LogLevel.Trace, LogLevel.Fatal, fileTarget);

                NLog.LogManager.Configuration = config;
                Logger = NLog.LogManager.GetCurrentClassLogger();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[Fatal] Create NLog config fail: {ex.Message}");
            }
        }
    }
}