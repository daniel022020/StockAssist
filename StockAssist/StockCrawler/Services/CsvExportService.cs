using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StockAssist.Log;

namespace StockAssist.StockCrawler.Services
{
    public class CsvExportService : ICsvExportService
    {
        // 多國語言
        public LanguageService LangService => LanguageService.Instance;

        // 解析並整理 JSON 資料
        public string CleanJsonStringFormat(string jsonString, out string errorString)
        {
            LogService.Logger.Info("Clean jsonString format");

            errorString = string.Empty;

            try
            {
                // 解析原始 JSON
                JObject originalObj = JObject.Parse(jsonString);
                JObject filteredObj = new JObject();
                List<string> removedTickers = new List<string>();

                // 遍歷每一個 Ticker
                foreach (var property in originalObj)
                {
                    string tickerName = property.Key;
                    JToken tokenValue = property.Value;

                    // 如果是 Error Ticker 直接剔除
                    if (tokenValue is JObject && tokenValue["error"] != null)
                    {
                        removedTickers.Add($"{tickerName}=>({LangService.GetLanguageString("TickerError")})\n");
                        continue;
                    }

                    // 如果是正常的資料陣列，開始檢查
                    if (tokenValue is JArray dataArray)
                    {
                        // 用來儲存正確的資料
                        JArray cleanDataArray = new JArray();

                        // 遍歷這隻股票的每一天資料
                        foreach (JToken dayToken in dataArray)
                        {
                            if (dayToken is JObject dayObj)
                            {
                                bool hasInvalidField = false;

                                // 檢查所有必要欄位
                                string[] checkFields = new string[] { "Date", "Open", "High", "Low", "Close", "Volume" };
                                foreach (string field in checkFields)
                                {
                                    JToken fieldValue = dayObj[field];

                                    // 判斷是否為 null、JValue.Null、空字串、或是 "Nan"
                                    if (fieldValue == null ||
                                        fieldValue.Type == JTokenType.Null ||
                                        string.IsNullOrEmpty(fieldValue.ToString()))
                                    {
                                        hasInvalidField = true;
                                        break;
                                    }

                                    // 檢查有沒有包含 "Nan" 字串 (忽略大小寫)
                                    string valStr = fieldValue.ToString().Trim();
                                    if (valStr.Equals("NaN", StringComparison.OrdinalIgnoreCase) ||
                                        valStr.Equals("Nan", StringComparison.OrdinalIgnoreCase))
                                    {
                                        hasInvalidField = true;
                                        break;
                                    }
                                }

                                // 如果這天資料完全正常，才加入
                                if (!hasInvalidField)
                                {
                                    cleanDataArray.Add(dayObj);
                                }
                            }
                        }

                        // 檢查筆數是否不滿兩筆
                        if (cleanDataArray.Count < 2)
                        {
                            removedTickers.Add($"{tickerName}=>({LangService.GetLanguageString("AtLeastTwoWorkingDays")})\n");
                        }
                        else
                        {
                            // 滿足 2 筆以上，即可畫圖
                            filteredObj.Add(tickerName, cleanDataArray);
                        }
                    }
                }

                // 紀錄哪些 Ticker 被剃除
                if (removedTickers.Count > 0)
                {
                    string removedLog = string.Join("", removedTickers);
                    errorString = removedLog;
                    LogService.Logger.Warn($"Remove ticker: {removedLog}");
                }

                // 寫回 jsonString
                jsonString = filteredObj.ToString(Formatting.None);
            }
            catch (Exception ex)
            {
                LogService.Logger.Fatal(ex, "[Exception] Clean jsonString format fail");
                throw; // 向上拋出
            }

            return jsonString;
        }

        // 解析歷史 JSON 資料，並將每檔股票存成 CSV 檔案
        public void ExportCsv(string jsonString, string folderPath, string dateFormat)
        {
            LogService.Logger.Info("Start export csv in background");

            if (string.IsNullOrWhiteSpace(jsonString))
                return;

            try
            {
                LogService.Logger.Info($"Csv folder:{folderPath}");

                var allData = JsonConvert.DeserializeObject<Dictionary<string, List<StockDetail>>>(jsonString);

                if (allData == null) return;

                foreach (var stock in allData)
                {
                    StringBuilder csv = new StringBuilder("Date,Open,High,Low,Close,Volume\n");

                    stock.Value.ForEach(d =>
                    {
                        if (DateTime.TryParse(d.Date, out DateTime parsedDate))
                        {
                            string formattedDate = parsedDate.ToString(dateFormat);
                            csv.AppendLine($"{formattedDate},{d.Open},{d.High},{d.Low},{d.Close},{d.Volume}");
                        }
                        else
                        {
                            csv.AppendLine($"{d.Date},{d.Open},{d.High},{d.Low},{d.Close},{d.Volume}");
                        }
                    });

                    LogService.Logger.Info($"Write csv for:{stock.Key}");
                    File.WriteAllText(Path.Combine(folderPath, $"{stock.Key.Replace(":", "_")}.csv"), csv.ToString(), Encoding.UTF8);
                }

                LogService.Logger.Info($"Write all csv success");
            }
            catch (Exception ex)
            {
                LogService.Logger.Fatal(ex, "[Exception] Export csv fail");
                throw; // 向上拋出
            }
        }
    }
}