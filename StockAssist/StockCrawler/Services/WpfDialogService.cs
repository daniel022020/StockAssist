using System.Windows.Forms;

namespace StockAssist.StockCrawler.Services
{
    // WPF 環境下的具體實作
    public class WpfDialogService : IDialogService
    {
        public void ShowError(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void ShowWarning(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public string SelectFolder()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = true;
                dialog.FileName = "Select Folder";
                dialog.Filter = "Select Folder |*.this_is_a_folder_marker";
                dialog.ValidateNames = false;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return System.IO.Path.GetDirectoryName(dialog.FileName);
                }
            }
            return null;
        }
    }
}