using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.ComponentModel;

namespace SwitchSDCopy
{
    /// <summary>
    /// Lógica interna para ProgressPopup.xaml
    /// </summary>
    public partial class ProgressPopup : Window
    {
        private object CopyMutex = new object();
        private object LogMutex = new object();

        private bool InProgress = false;

        public ProgressPopup()
        {
            InitializeComponent();
        }


        public void StartOperation(OperationData opdata)
        {
            CopyAlbumRegex(opdata);

            //CopyAlbum(opdata);
        }

        public void StartScan(OperationData opdata)
        {
            ScanCodes(opdata);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            errorLogText.ScrollToEnd();   
        }

        private void CopyAlbum(OperationData opdata) {
            InProgress = true;

            string sourcePath = opdata.source;
            string targetPath = opdata.target;
            bool separateClips = opdata.separateShotsAndClips;
            bool includeExtraFolder = opdata.includeExtra;
            bool separateExtraFolder = opdata.separateExtra;
            string screenshotFolderName = opdata.shotFolderName;
            string clipFolderName = opdata.clipFolderName;
            int folderStructureType = (!opdata.maintainDateFolders ? 0 : (opdata.dateMode == DateMode.KeepY ? 1 : opdata.dateMode == DateMode.KeepYM ? 2 : 3));
            string albumName = opdata.albumName;

            // INVARIANT: Assume source and target have been verified.

            // Count files
            Dispatcher.Invoke(() =>
            {
                LogMessage("Analyzing directories...");
            });
            int numFiles = CountRelevantFiles(sourcePath,includeExtraFolder);
            if (numFiles == 0)
            {
                Dispatcher.Invoke(() =>
                {
                    FailOperation("No files to copy");
                });
                return;
            }
            Dispatcher.Invoke(() =>
            {
                this.Title = this.Title + " -> copying " + numFiles + " files";
                //LogMessage("Copying " + numFiles + " files.");
                ResetProgressBar(numFiles);
            });

            string albumPath = targetPath + "\\" + albumName;
            // Retrieve directories from album path
            IEnumerable<string> albumDirs = null;
            try
            {
                albumDirs = Directory.EnumerateDirectories(sourcePath);
            }
            catch (Exception ex)
            {
                //LogMessage("Unable to access source path: " + ex.Message);
                Dispatcher.Invoke(() =>
                {
                    FailOperation("Unable to access source path: " + ex.Message);
                });
                return;
            }

            // Create directory in advance
            string clipPath = albumPath + "\\" + clipFolderName;
            string shotPath = albumPath + "\\" + screenshotFolderName;

            string clipExtraPath = albumPath + "\\Extra\\" + clipFolderName;
            string shotExtraPath = albumPath + "\\Extra\\" + screenshotFolderName;
            try
            {
                Directory.CreateDirectory(albumPath);
                /*if (separateClips)
                {
                    Directory.CreateDirectory(shotPath);
                    Directory.CreateDirectory(clipPath);
                }*/
            }
            catch (Exception ex)
            {
                //LogMessage("Unable to create album folder: " + ex.Message);
                Dispatcher.Invoke(() =>
                {
                    FailOperation("Unable to create album folder: " + ex.Message);
                });
                return;
            }

            // Find directories that match years [Optional: Include 'Extra' folder)
            IEnumerable<string> yearDirs;
            /*Scope to isolate n*/
            {
                uint n = 0;
                yearDirs = albumDirs.Where(a => a.StartsWith(sourcePath) && uint.TryParse(a.Substring(Math.Max(a.Length - 4, 0), 4), out n));
            }

            // Include extra folder if requested
            string extraSourcePath = sourcePath + "\\" + "Extra";
            if (includeExtraFolder)
            {
                // If extra folder does not exist, proceed as normal
                if (Directory.Exists(extraSourcePath))
                {
                    IEnumerable<string> extraCodeDirs = null;

                    // Retrieve directories from extra path
                    try
                    {
                        extraCodeDirs = Directory.EnumerateDirectories(extraSourcePath);
                    }
                    catch (Exception ex)
                    {
                        //AppUtil.ShowMessageWarning(ex.Message, "SwitchSDCopy - Unable to access 'Extra' folder");
                        Dispatcher.Invoke(() =>
                        {
                            FailOperation("Unable to access 'Extra' folder: " + ex.Message);
                        });
                        return;
                    }

                    IEnumerable<string> extraYearDirs = new List<string>();
                    try
                    {
                        foreach ( var edir in extraCodeDirs )
                        {
                            extraYearDirs = extraYearDirs.Concat(Directory.EnumerateDirectories(edir));
                        }
                    }
                    catch (Exception ex) {
                        Dispatcher.Invoke(() =>
                        {
                            FailOperation("Unable to access 'Extra' sub-folders: " + ex.Message);
                        });

                        return;
                    }

                    uint n = 0;
                    yearDirs = yearDirs.Concat(extraYearDirs.Where(a => a.StartsWith(extraSourcePath) && uint.TryParse(a.Substring(Math.Max(a.Length - 4, 0), 4), out n)));
                }
            }


            // Start looping folders / copying files
            // Start stopwatch for progress bar
            Stopwatch stopwatch = Stopwatch.StartNew();
            int numCopied = 0;
            if (yearDirs.Count() > 0)
            {
                // Loop year directories
                foreach (var year in yearDirs)
                {
                    string yearNum = year.Substring(year.Length - 4, 4);
                    bool isExtraYear = year.Substring(sourcePath.Length).StartsWith("\\Extra");

                    IEnumerable<string> monthDirs = null;

                    try
                    {
                        monthDirs = Directory.EnumerateDirectories(year);

                    }
                    /*catch (DirectoryNotFoundException dnfe)
                    {
                        //LogMessage("Source directory does not exist: " + dnfe.Message);
                        //FailOperation("Source directory does not exist: " + dnfe.Message);
                        continue;
                    }*/
                    catch (Exception excp)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LogMessage(excp.Message);
                        });
                        //LogMessage("Error occurred while searching directories: " + excp.Message);
                        //FailOperation("Error occurred while searching directories: " + excp.Message);
                        continue;
                    }

                    monthDirs = monthDirs.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase);

                    // Loop month directories
                    foreach (var month in monthDirs)
                    {
                        string monthNum = month.Substring(year.Length - 2, 2);
                        /*if (!monthNum.All(c => char.IsNumber(c))) {
                            monthNum = month;
                        }*/
                        IEnumerable<string> dayDirs = null;

                        // If maintain structure, create year folder

                        try
                        {
                            dayDirs = Directory.EnumerateDirectories(month);

                        }
                        /*catch (DirectoryNotFoundException dnfe)
                        {
                            //ShowMessageWarning(dnfe.Message, "Source directory does not exist.");
                            continue;
                        }*/
                        catch (Exception excp)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                LogMessage(excp.Message);
                            });
                            //ShowMessageWarning("An error has occurred", "An error occurred while looking up the source directory.");
                            continue;
                        }

                        dayDirs = dayDirs.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase);

                        //ShowMessageWarning(String.Join("\n", dayDirs), "Debug string thing");

                        // Loop days
                        foreach (var day in dayDirs)
                        {
                            string dayNum = day.Substring(day.Length - 2, 2);
                            /*if (!monthNum.All(c => char.IsNumber(c))) {
                                monthNum = month;
                            }*/

                            Dispatcher.Invoke(() =>
                            {
                                LogMessage(day);
                            });

                            IEnumerable<string> contentFiles = null;

                            // If maintain structure, create year folder
                            try
                            {
                                contentFiles = Directory.EnumerateFiles(day).Where(x => x.EndsWith(".jpg") || x.EndsWith(".mp4"));

                            }
                            /*catch (DirectoryNotFoundException dnfe)
                            {
                                //ShowMessageWarning(dnfe.Message, "Source directory does not exist.");
                                //FailOperation();
                                continue;
                            }*/
                            catch (Exception excp)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    LogMessage(excp.Message);
                                });
                                //ShowMessageWarning("An error has occurred", "An error occurred while looking up the source directory.");
                                //FailOperation();
                                continue;
                            }

                            contentFiles = contentFiles.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase);

                            if (contentFiles.Count() > 0)
                            {
                                // Create directory beforehand
                                string dateTag = AppUtil.GetDatePath(folderStructureType, yearNum, monthNum, dayNum);

                                if (separateClips)
                                {
                                    string directoryClip = "";// clipPath + dateTag;
                                    string directoryShot = "";//shotPath + dateTag;
                                    if (isExtraYear && includeExtraFolder && separateExtraFolder)
                                    {
                                        directoryClip = clipExtraPath + dateTag;
                                        directoryShot = shotExtraPath + dateTag;
                                    }
                                    else {
                                        directoryClip = clipPath + dateTag;
                                        directoryShot = shotPath + dateTag;
                                    }

                                    if (!Directory.Exists(directoryClip) && contentFiles.Any( x => x.EndsWith(".mp4"))) {
                                        if (!AppUtil.CreateDir(directoryClip))
                                        {
                                            Dispatcher.Invoke(() =>
                                            {
                                                FailOperation("Failed to create directory: " + directoryClip);
                                            });
                                            return;
                                        }
                                    }
                                    if (!Directory.Exists(directoryShot) && contentFiles.Any( x => x.EndsWith(".jpg"))) {
                                        if (!AppUtil.CreateDir(directoryShot))
                                        {
                                            Dispatcher.Invoke(() =>
                                            {
                                                FailOperation("Failed to create directory: " + directoryShot);
                                            });
                                            return;
                                        }
                                    }

                                    foreach (var file in contentFiles)
                                    {
                                        if (!InProgress)
                                        {
                                            return;
                                        }

                                        string fileName = System.IO.Path.GetFileName(file);//.Split('\\').Last();
                                        bool isClip = fileName.EndsWith(".mp4");

                                        string targetFileName = (isClip ? directoryClip : directoryShot) + "\\" + fileName;

                                        try
                                        {
                                            File.Copy(file, targetFileName);
                                            numCopied++;
                                        }
                                        catch (Exception ex)
                                        {
                                            Dispatcher.Invoke(() =>
                                            {
                                                //ShowMessageWarning(ex.Message, "SwitchSDCopy - Unable to copy file");
                                                FailOperation("Unable to copy file - " + ex.Message);
                                            });
                                            return;
                                        }

                                        if (stopwatch.ElapsedMilliseconds >= 1000)
                                        {
                                            Dispatcher.Invoke(() =>
                                            {
                                                UpdateProgress(numCopied);
                                            });
                                            stopwatch.Restart();
                                        }
                                    }
                                }
                                else
                                {
                                    string mainDirectory = "";// albumPath + dateTag;
                                    if (isExtraYear && includeExtraFolder && separateExtraFolder)
                                    {
                                        mainDirectory = albumPath + "\\Extra" + dateTag;
                                    }
                                    else {
                                        mainDirectory = albumPath + dateTag;
                                    }

                                    if (!Directory.Exists(mainDirectory)) {
                                        if (!AppUtil.CreateDir(mainDirectory))
                                        {
                                            Dispatcher.Invoke(() =>
                                            {
                                                FailOperation("Failed to create directory: " + mainDirectory);
                                            });
                                            return;
                                        }
                                    }

                                    foreach (var file in contentFiles)
                                    {
                                        if (!InProgress)
                                        {
                                            return;
                                        }

                                        //string targetFileName = mainDirectory + "\\" + file.Split('\\').Last();
                                        string targetFileName = mainDirectory + "\\" + System.IO.Path.GetFileName(file);

                                        if (!File.Exists(targetFileName))
                                        {
                                            try
                                            {
                                                File.Copy(file, targetFileName);
                                            }
                                            catch (Exception ex)
                                            {
                                                Dispatcher.Invoke(() =>
                                                {
                                                    //ShowMessageWarning(ex.Message, "SwitchSDCopy - Unable to copy file");
                                                    FailOperation("Unable to copy file - " + ex.Message);
                                                });
                                                return;
                                            }
                                        }

                                        numCopied++;

                                        if (stopwatch.ElapsedMilliseconds >= 1000) {
                                            Dispatcher.Invoke(() =>
                                            {
                                                LogMessage(numCopied + " out of " + numFiles);
                                                UpdateProgress(numCopied);
                                            });
                                            stopwatch.Restart();
                                        }
                                    }
                                }
                            }

                            /*if (!InProgress)
                            {
                                //UserInterrupted();
                                return;
                            }*/
                        }
                    }

                }

                Dispatcher.Invoke(() =>
                {
                    CompleteOperation("Copy complete!");
                });
            }
        }


        private void CopyAlbumRegex(OperationData opdata)
        {
            InProgress = true;

            string sourcePath = opdata.source;
            string targetPath = opdata.target;
            bool createAlbumFolder = opdata.createAlbumFolder;
            bool separateClips = opdata.separateShotsAndClips;
            bool includeExtraFolder = opdata.includeExtra;
            bool separateExtraFolder = opdata.separateExtra;
            bool separateByGame = opdata.separateByGame;
            bool filterSpecificGame = opdata.specificGameCode;
            string gameCodeMatch = opdata.gameCodeMatch;
            string screenshotFolderName = opdata.shotFolderName;
            string clipFolderName = opdata.clipFolderName;
            int folderStructureType = (!opdata.maintainDateFolders ? 0 : (opdata.dateMode == DateMode.KeepY ? 1 : opdata.dateMode == DateMode.KeepYM ? 2 : 3));
            string albumName = opdata.albumName;

            string targetAlbumPath = targetPath + (createAlbumFolder ? "\\" + albumName : "");

            // INVARIANT: Assume source and target have been verified.

            // Count files
            Dispatcher.Invoke(() =>
            {
                LogMessage("Analyzing directories...");
            });
            /*int numFiles = CountRelevantFiles(sourcePath, includeExtraFolder);
            if (numFiles == 0)
            {
                Dispatcher.Invoke(() =>
                {
                    FailOperation("No files to copy");
                });
                return;
            }*/

            // Retrieve all files
            IEnumerable<string> allFiles = null;
            try
            {
                allFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                //LogMessage("Unable to access source path: " + ex.Message);
                Dispatcher.Invoke(() =>
                {
                    FailOperation("Unable to access source path: " + ex.Message);
                });
                return;
            }
            allFiles = allFiles.Where(x => x.EndsWith(".jpg") || x.EndsWith(".mp4"));
            // Include extra folder if requested
            string extraSourcePath = sourcePath + "\\" + "Extra";
            if (!includeExtraFolder)
            {
                allFiles = allFiles.Where(x => !System.IO.Path.GetFileNameWithoutExtension(x).EndsWith("X"));
            }
            if (separateByGame && filterSpecificGame) {
                allFiles = allFiles.Where(x => System.IO.Path.GetFileNameWithoutExtension(x).Substring(17, 32) == gameCodeMatch);
            }

            // Sort files to start copying from oldest to newest
            allFiles = allFiles.OrderBy(x => System.IO.Path.GetFileNameWithoutExtension(x), StringComparer.CurrentCultureIgnoreCase);

            int numFiles = allFiles.Count();
            Dispatcher.Invoke(() =>
            {
                this.Title = this.Title + " -> copying " + numFiles + " files";
                LogMessage("Copying " + numFiles + " files.");
                ResetProgressBar(numFiles);
            });

            // Create directory in advance
            try
            {
                Directory.CreateDirectory(targetAlbumPath);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    FailOperation("Unable to create album folder: " + ex.Message);
                });
                return;
            }

            // Start looping folders / copying files
            // Start stopwatch for progress bar
            Stopwatch stopwatch = Stopwatch.StartNew();
            int numCopied = 0;

            // Process files
            foreach (var filePath in allFiles)
            {
                if (!InProgress)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Cancelled();
                    });
                    return;
                }

                // Read file name info
                string fileFullName = System.IO.Path.GetFileName(filePath);//.Split('\\').Last();
                string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                string year = fileName.Substring(0, 4);
                string month = fileName.Substring(4, 2);
                string day = fileName.Substring(6, 2);
                string hour = fileName.Substring(8, 2);
                string minute = fileName.Substring(10, 2);
                string second = fileName.Substring(12, 2);
                string gamecode = fileName.Substring(17, 32);

                string dateTag = AppUtil.GetDatePath(folderStructureType, year, month, day);
                bool isExtraFile = fileName.EndsWith("X");

                string targetDirectory = targetAlbumPath;

                // Separate by game: Highest priority
                if (separateByGame) {
                    string gameTag = "";
                    try
                    {
                        gameTag = "\\" + GameCodes.Codes[gamecode];
                    }
                    catch (KeyNotFoundException) {
                        gameTag = "\\Other";
                    }

                    targetDirectory += gameTag;
                }

                // Separate by Extra
                if (isExtraFile && includeExtraFolder && separateExtraFolder) {
                    targetDirectory += "\\Extra";
                }

                // Check if necessary to separate clips
                if (separateClips)
                {
                    bool isClip = filePath.EndsWith(".mp4");

                    if (isClip)
                    {
                        targetDirectory += "\\" + clipFolderName;
                    }
                    else
                    {
                        targetDirectory += "\\" + screenshotFolderName;
                    }
                }

                // Add dateTag - Alread formatted with \ where necessary
                targetDirectory += dateTag;

                // Create target directory if it does not exist
                if (!Directory.Exists(targetDirectory))
                {
                    if (!AppUtil.CreateDir(targetDirectory))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            FailOperation("Failed to create directory(ies) in: " + targetDirectory);
                        });
                        return;
                    }
                }

                string targetFileName = targetDirectory + "\\" + fileFullName;

                numCopied++;
                if (!File.Exists(targetFileName))
                {
                    try
                    {
                        File.Copy(filePath, targetFileName);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            //ShowMessageWarning(ex.Message, "SwitchSDCopy - Unable to copy file");
                            FailOperation("Unable to copy file - " + ex.Message);
                        });
                        return;
                    }
                }

                // Update progress every second
                if (stopwatch.ElapsedMilliseconds >= 1000)
                {
                    Dispatcher.Invoke(() =>
                    {
                        LogMessage(numCopied + " out of " + numFiles);
                    });
                    Dispatcher.Invoke(() =>
                    {
                        UpdateProgress(numCopied);
                    });
                    stopwatch.Restart();
                }


            }

            Dispatcher.Invoke(() =>
            {
                CompleteOperation("Copy complete!");
            });
        }

        private void ScanCodes(OperationData opdata)
        {
            InProgress = true;

            string sourcePath = opdata.source;
            string targetPath = opdata.target;
            bool createAlbumFolder = opdata.createAlbumFolder;
            bool separateClips = opdata.separateShotsAndClips;
            bool includeExtraFolder = opdata.includeExtra;
            bool separateExtraFolder = opdata.separateExtra;
            bool separateByGame = opdata.separateByGame;
            string screenshotFolderName = opdata.shotFolderName;
            string clipFolderName = opdata.clipFolderName;
            int folderStructureType = (!opdata.maintainDateFolders ? 0 : (opdata.dateMode == DateMode.KeepY ? 1 : opdata.dateMode == DateMode.KeepYM ? 2 : 3));
            string albumName = opdata.albumName;

            string targetAlbumPath = targetPath + (createAlbumFolder ? "\\" + albumName : "");

            // INVARIANT: Assume source and target have been verified.
            IEnumerable<string> allFiles = null;
            try
            {
                allFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                //LogMessage("Unable to access source path: " + ex.Message);
                Dispatcher.Invoke(() =>
                {
                    FailOperation("Unable to access source path: " + ex.Message);
                });
                return;
            }
            allFiles = allFiles.Where(x => x.EndsWith(".jpg") || x.EndsWith(".mp4"));
            // Include extra folder if requested
            /*string extraSourcePath = sourcePath + "\\" + "Extra";
            if (!includeExtraFolder)
            {
                allFiles = allFiles.Where(x => !System.IO.Path.GetFileNameWithoutExtension(x).EndsWith("X"));
            }*/

            // Sort files to start copying from oldest to newest
            allFiles = allFiles.OrderBy(x => System.IO.Path.GetFileNameWithoutExtension(x), StringComparer.CurrentCultureIgnoreCase);

            int numFiles = allFiles.Count();
            Dispatcher.Invoke(() =>
            {
                this.Title = this.Title + " -> Scanning Files";
                ResetProgressBar(numFiles);
            });

            HashSet<string> unknownCodes = new HashSet<string>();

            // Process files
            foreach (var filePath in allFiles)
            {
                if (!InProgress)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Cancelled();
                    });
                    return;
                }

                // Read file name info
                string fileFullName = System.IO.Path.GetFileName(filePath);//.Split('\\').Last();
                string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                string year = fileName.Substring(0, 4);
                string month = fileName.Substring(4, 2);
                string day = fileName.Substring(6, 2);
                string hour = fileName.Substring(8, 2);
                string minute = fileName.Substring(10, 2);
                string second = fileName.Substring(12, 2);
                string gamecode = fileName.Substring(17, 32);

                string dateTag = AppUtil.GetDatePath(folderStructureType, year, month, day);
                bool isExtraFile = fileName.EndsWith("X");


                if (!GameCodes.Codes.ContainsKey(gamecode)) {
                    if (unknownCodes.Add(gamecode))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            //LogMessage("Code: " + gamecode + "  Sample file: " + fileFullName );
                            LogMessage("Code: " + gamecode);
                            LogMessage("Sample file: " + fileFullName );
                        });
                    }
                }
            }

            Dispatcher.Invoke(() =>
            {
                CompleteOperation( unknownCodes.Count > 0 ? "Scan complete!" : "Scan complete - no unknown codes found." );
            });
        }

        // Count number of files that are inside yyyy\mm\dd folders
        // Filter out files from within \Extra folder if 'countExtra' is false
        private int CountRelevantFiles(string directory, bool countExtra) {
            int count = 0;

            var allFiles = Directory.EnumerateFiles(directory,"*",SearchOption.AllDirectories);
            var filterFiles = allFiles.Where((a) => a.EndsWith(".jpg") || a.EndsWith(".mp4"));

            var filterDirs = filterFiles.Select(x => System.IO.Path.GetDirectoryName(x));

            if (!countExtra) {
                filterDirs = filterDirs.Where(a => !a.Substring(directory.Length).StartsWith("\\Extra"));
            }


            Regex filterDirExp = new Regex(@"\d\d\d\d\\\d\d\\\d\d$");
            foreach (var dir in filterDirs) {
                if (filterDirExp.IsMatch(dir)) count++;
            }

            return count;
        }

        private void ResetProgressBar(int maxVal) {
            completionBar.Maximum = maxVal;
            completionBar.Value = 0;
        }

        private void UpdateProgress(int pg) {
            completionBar.Value = pg;
            progressLabel.Content = Math.Floor((float)completionBar.Value / (float)completionBar.Maximum * 100) + "%";
        }

        private void FailOperation(string message) {
            lock (CopyMutex)
            {
                cancelButton.Content = "Close";
                completionBar.Foreground = Brushes.Red;
                LogMessage(message);
                InProgress = false;
            }
        }
        private void CompleteOperation(string message) {
            lock (CopyMutex)
            {
                cancelButton.Content = "OK";
                completionBar.Value = completionBar.Maximum;
                progressLabel.Content = "100%";
                LogMessage(message);
                InProgress = false;
            }
        }

        private void UserInterrupted()
        {
            InProgress = false;
        }

        private void Cancelled() {
            completionBar.Foreground = Brushes.Yellow;
            cancelButton.Content = "Close";
            LogMessage("Cancelled.");
        }

        private void LogMessage(string message) {
            lock (LogMutex)
            {
                errorLogText.Text += message + "\n";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            lock (CopyMutex)
            {
                if (InProgress)
                {
                    UserInterrupted();
                }
                else {
                    Close();
                }
            }
        }

        public void ClosingX(object sender, CancelEventArgs e) {
            InProgress = false;
        }
    }
}
