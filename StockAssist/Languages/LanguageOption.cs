using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockAssist.Languages
{
    public class LanguageOption
    {
        // UI上的多國語言項目
        [JsonProperty("DisplayName")]
        public string DisplayName { get; set; }

        // 對應檔案名稱的代碼，ex:zh-TW
        [JsonProperty("LanguageCode")]
        public string LanguageCode { get; set; }
    }
}
