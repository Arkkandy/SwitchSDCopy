using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.ComponentModel;

namespace SwitchSDCopy
{

    public enum DateMode {
        KeepYMD,
        KeepYM,
        KeepY
    }
    public struct OperationData {
        public string source;
        public string target;

        public bool maintainDateFolders;
        public DateMode dateMode;

        public bool separateShotsAndClips;
        public string shotFolderName;
        public string clipFolderName;

        public bool createAlbumFolder;
        public string albumName;

        public bool includeExtra;
        public bool separateExtra;

        public bool separateByGame;

        public bool specificGameCode;
        public string gameCodeMatch;
    }


    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Initialize data: Game Codes and User Data
            GameCodes.LoadGameCodes();
            OperationData userData = SettingCache.ReadUserSettings();
            ApplySettingsFromUserData(userData);

            //GameCodes.Codes.Select<()
            comboGameCode.Items.Add("All");

            foreach (var code in GameCodes.Codes) {
                comboGameCode.Items.Add(code.Value + " (" + code.Key + ")");
            }
            comboGameCode.SelectedIndex = 0;

            //AppUtil.ShowMessageWarning("Loaded " + GameCodes.Codes.Count() + " game codes.", "Game codes successfully read");
        }

        private void ApplySettingsFromUserData(OperationData userData) {
            sourceValue.Text = userData.source;
            targetValue.Text = userData.target;


            checkFolder.IsChecked = userData.maintainDateFolders;
            if (userData.dateMode == DateMode.KeepY) {
                folderPerDay.IsChecked = true;
            }
            else if (userData.dateMode == DateMode.KeepYM)
            {
                folderPerMonth.IsChecked = true;
            }
            else if (userData.dateMode == DateMode.KeepY)
            {
                folderPerYear.IsChecked = true;
            }

            separateShotsClips.IsChecked = userData.separateShotsAndClips;
            screenshotName.Text = userData.shotFolderName;
            clipName.Text = userData.clipFolderName;

            checkCreateAlbum.IsChecked = userData.createAlbumFolder;
            albumName.Text = userData.albumName;

            extraCheck.IsChecked = userData.includeExtra;
            separateExtraCheck.IsChecked = userData.separateExtra;

            checkByGame.IsChecked = userData.separateByGame;
            //comboGame -> automatically set elsewhere
        }

        private void SourceLookup_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dial = new FolderBrowserDialog();
            dial.Description = "Select Album folder:";
            if (Directory.Exists(sourceValue.Text))
            {
                dial.RootFolder = Environment.SpecialFolder.Desktop;
                dial.SelectedPath = sourceValue.Text;
            }
            else
            {
                dial.RootFolder = Environment.SpecialFolder.MyComputer;
            }

            DialogResult dres = dial.ShowDialog();
            if (dres == System.Windows.Forms.DialogResult.OK)
            {
                sourceValue.Text = dial.SelectedPath;
            }
        }

        private void TargetLookup_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dial = new FolderBrowserDialog();
            dial.Description = "Select destination folder:";
            if (Directory.Exists(targetValue.Text))
            {
                dial.RootFolder = Environment.SpecialFolder.Desktop;
                dial.SelectedPath = targetValue.Text;
            }
            else
            {
                dial.RootFolder = Environment.SpecialFolder.MyComputer;
            }

            DialogResult dres = dial.ShowDialog();
            if (dres == System.Windows.Forms.DialogResult.OK)
            {
                targetValue.Text = dial.SelectedPath;
            }
        }

        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            // Prepare data to be sent
            OperationData data = new OperationData();
            data.createAlbumFolder = checkCreateAlbum.IsChecked ?? false;
            data.albumName = albumName.Text;

            data.source = sourceValue.Text;
            data.target = targetValue.Text;

            data.maintainDateFolders = checkFolder.IsChecked ?? false;
            if (data.maintainDateFolders) {
                if (folderPerDay.IsChecked ?? false) {
                    data.dateMode = DateMode.KeepYMD;
                }
                else if (folderPerMonth.IsChecked ?? false) {
                    data.dateMode = DateMode.KeepYM;
                }
                else //if (folderPerYear.IsChecked ?? false)
                {
                    data.dateMode = DateMode.KeepY;
                }
            }

            data.separateShotsAndClips = separateShotsClips.IsChecked ?? false;
            data.clipFolderName = clipName.Text;
            data.shotFolderName = screenshotName.Text;

            data.includeExtra = extraCheck.IsChecked ?? false;
            data.separateExtra = separateExtraCheck.IsChecked ?? false;

            data.separateByGame = checkByGame.IsChecked ?? false;
            data.specificGameCode = comboGameCode.SelectedIndex != 0 && comboGameCode.SelectedIndex != -1;
            string contentString = comboGameCode.SelectedItem as string;
            data.gameCodeMatch = data.specificGameCode ? contentString.Substring(contentString.Count() - 33, 32) : "";

            // Error for source = target
            if (data.source == data.target) {
                AppUtil.ShowMessageWarning("Source directory and target directory are the same!", "SwitchSDCopy");
                return;
            }

            // Check if given source path exist
            if (!Directory.Exists(data.source))
            {
                AppUtil.ShowMessageWarning("Source directory was not found.", "SwitchSDCopy");
                return;
            }

            // Attempt to access source folder
            try
            {
                Directory.EnumerateDirectories(data.source);
            }
            catch (Exception ex)
            {
                AppUtil.ShowMessageWarning("Cannot access source directory: " + ex.Message, "SwitchSDCopy");
                return;
            }

            // Check if given target path exists - Attempt to create if it doesn't
            if (!Directory.Exists(data.target))
            {
                var result = AppUtil.PromptUserYesNo("SwitchSDCopy - Create new folder?", "Target path not found. Would you like to create it?");
                if (result == System.Windows.Forms.DialogResult.No)
                {
                    return;
                }
                else
                {
                    try
                    {
                        Directory.CreateDirectory(data.target);
                    }
                    catch (Exception ex)
                    {
                        AppUtil.ShowMessageWarning(ex.Message, "SwitchSDCopy - Unable to create target directory");
                        return;
                    }
                }
            }

            // Attempt to access target folder
            try
            {
                Directory.EnumerateDirectories(data.target);
            }
            catch (Exception ex)
            {
                AppUtil.ShowMessageWarning("Cannot access target directory: " + ex.Message, "SwitchSDCopy");
                return;
            }

            // If control comes to this point, then it should be safe to proceed with the copy operation
            SettingCache.SaveUserSettings(data);

            // Open progress bar window
            var newWindow = new ProgressPopup();
            newWindow.Loaded += (_, args) => {
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, workerArgs) => newWindow.StartOperation(data);

                worker.RunWorkerAsync();
            };

            //newWindow.StartOperation(data);
            newWindow.ShowDialog();
        }

        private void CheckFolder_Checked(object sender, RoutedEventArgs e)
        {
            folderPerDay.IsEnabled = true;
            folderPerMonth.IsEnabled = true;
            folderPerYear.IsEnabled = true;
        }
        private void CheckFolder_Unchecked(object sender, RoutedEventArgs e)
        {
            folderPerDay.IsEnabled = false;
            folderPerMonth.IsEnabled = false;
            folderPerYear.IsEnabled = false;
        }

        private void SeparateShotsClips_Checked(object sender, RoutedEventArgs e)
        {
            screenshotName.IsEnabled = true;
            clipName.IsEnabled = true;
        }

        private void SeparateShotsClips_Unchecked(object sender, RoutedEventArgs e)
        {
            screenshotName.IsEnabled = false;
            clipName.IsEnabled = false;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ExtraCheck_Checked(object sender, RoutedEventArgs e)
        {
            separateExtraCheck.IsEnabled = true;
        }
        private void ExtraCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            separateExtraCheck.IsEnabled = false;
        }

        private void checkCreateAlbum_Checked(object sender, RoutedEventArgs e)
        {
            albumName.IsEnabled = true;
        }

        private void checkCreateAlbum_Unchecked(object sender, RoutedEventArgs e)
        {
            albumName.IsEnabled = false;
        }

        private void GameCheck_Checked(object sender, RoutedEventArgs e)
        {
            comboGameCode.IsEnabled = true;
        }
        private void GameCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            comboGameCode.IsEnabled = false;
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            // Prepare data to be sent
            OperationData data = new OperationData();
            data.createAlbumFolder = checkCreateAlbum.IsChecked ?? false;
            data.albumName = albumName.Text;

            data.source = sourceValue.Text;
            data.target = targetValue.Text;

            data.maintainDateFolders = checkFolder.IsChecked ?? false;
            if (data.maintainDateFolders)
            {
                if (folderPerDay.IsChecked ?? false)
                {
                    data.dateMode = DateMode.KeepYMD;
                }
                else if (folderPerMonth.IsChecked ?? false)
                {
                    data.dateMode = DateMode.KeepYM;
                }
                else //if (folderPerYear.IsChecked ?? false)
                {
                    data.dateMode = DateMode.KeepY;
                }
            }

            data.separateShotsAndClips = separateShotsClips.IsChecked ?? false;
            data.clipFolderName = clipName.Text;
            data.shotFolderName = screenshotName.Text;

            data.includeExtra = extraCheck.IsChecked ?? false;
            data.separateExtra = separateExtraCheck.IsChecked ?? false;

            data.separateByGame = checkByGame.IsChecked ?? false;
            data.specificGameCode = comboGameCode.SelectedIndex != 0 && comboGameCode.SelectedIndex != -1;
            string contentString = comboGameCode.SelectedItem as string;
            data.gameCodeMatch = data.specificGameCode ? contentString.Substring(contentString.Count() - 33, 32) : "";

            // Check if given source path exist
            if (!Directory.Exists(data.source))
            {
                AppUtil.ShowMessageWarning("Source directory was not found.", "SwitchSDCopy");
                return;
            }

            // Attempt to access source folder
            try
            {
                Directory.EnumerateDirectories(data.source);
            }
            catch (Exception ex)
            {
                AppUtil.ShowMessageWarning("Cannot access source directory: " + ex.Message, "SwitchSDCopy");
                return;
            }

            // Open progress bar window
            var newWindow = new ProgressPopup();
            newWindow.Loaded += (_, args) => {
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, workerArgs) => newWindow.StartScan(data);

                worker.RunWorkerAsync();
            };

            //newWindow.StartOperation(data);
            newWindow.ShowDialog();
        }
    }
}
