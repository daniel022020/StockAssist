using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using StockAssist.Languages;

namespace StockAssist.Setting.ViewModels
{
    public class SettingViewModel : ObservableObject
    {
        // 多國語言
        public LanguageService LangService => LanguageService.Instance;

        // UI 下拉選單清單
        public ObservableCollection<LanguageOption> Languages { get; }

        private string _copyRights;

        // 版權、版號
        public string CopyRights
        {
            get => _copyRights;
            set => SetProperty(ref _copyRights, value);
        }

        public SettingViewModel()
        {
            // 初始化下拉選單清單
            Languages = new ObservableCollection<LanguageOption>(LangService.LoadLanguageOptions());

            // 尋找清單中 CultureCode 符合當前 LanguageService 正在使用的語系
            var currentActiveLang = Languages.FirstOrDefault(x => x.LanguageCode == LangService.CurrentLanguageCode);

            if (currentActiveLang != null)
            {
                _selectedLanguage = currentActiveLang;
            }
            else if (Languages.Count > 0)
            {
                // 如果在清單中找不到當前語系，才選第一個
                SelectedLanguage = Languages[0];
            }

            InitCopyRightsText();
        }

        private LanguageOption _selectedLanguage;
        public LanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                // 只有當使用者在 UI 切換語系時，才會進來這裡讀取新檔案
                if (SetProperty(ref _selectedLanguage, value) && value != null)
                {
                    LangService.LoadLanguage(value.LanguageCode);
                }
            }
        }

        private void InitCopyRightsText()
        {
            try
            {
                // 取得組件資訊
                Assembly assembly = Assembly.GetExecutingAssembly();

                // 版號
                string version = assembly.GetName().Version?.ToString() ?? "2.6.0.6";

                // 取得 Copyright
                var copyrightAttr = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
                string copyright = copyrightAttr?.Copyright ?? "© 2026 Daniel Yang. All rights reserved.";

                // 取得開發者名稱
                var companyAttr = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
                string developer = companyAttr?.Company ?? "Daniel Yang";

                string contact = "daniel022020@gmail.com";

                // 組合字串
                CopyRights = $"Version {version}\n" +
                             $"Developer : {developer}\n" +
                             $"Contact : {contact}\n" +
                             $"{copyright}";
            }
            catch (Exception)
            {
                // 預設值
                CopyRights = "Version 2.6.0.6\nDeveloper : Daniel Yang\nContact : daniel022020@gmail.com\n© 2026 Daniel Yang. All rights reserved.";
            }
        }
    }
}