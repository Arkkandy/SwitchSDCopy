using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwitchSDCopy
{
    public class SettingCache
    {
        private SettingCache() {
        }

        public static OperationData ReadUserSettings() {
            OperationData data = new OperationData();

            // Read file
            try
            {
                foreach (var line in System.IO.File.ReadLines(@"UserSettings.dat"))
                {
                    if (line.Count() == 0) continue;
                    if (line[0] == '#') continue;

                    if (line.StartsWith("source=")) {
                        data.source = line.Remove(0, 7);
                    }
                    else if (line.StartsWith("target="))
                    {
                        data.target = line.Remove(0, 7);
                    }
                    else if (line.StartsWith("date="))
                    {
                        data.maintainDateFolders = (line.Remove(0, 5) == "Y");
                    }
                    else if (line.StartsWith("dateformat="))
                    {
                        int value = 0;
                        int.TryParse(line.Remove(0, 11), out value);
                        data.dateMode = (DateMode)value;
                        
                    }
                    else if (line.StartsWith("shotclip="))
                    {
                        data.separateShotsAndClips = (line.Remove(0, 9) == "Y");
                    }
                    else if (line.StartsWith("screenshots="))
                    {
                        data.shotFolderName = line.Remove(0, 12);
                    }
                    else if (line.StartsWith("clips="))
                    {
                        data.clipFolderName = line.Remove(0, 6);
                    }
                    else if (line.StartsWith("extra="))
                    {
                        data.includeExtra = (line.Remove(0, 6) == "Y");
                    }
                    else if (line.StartsWith("separateextra="))
                    {
                        data.separateExtra = (line.Remove(0, 14) == "Y");
                    }
                    else if (line.StartsWith("game="))
                    {
                        data.separateByGame = (line.Remove(0, 5) == "Y");
                    }
                    // Game entry
                    else if (line.StartsWith("createalbum="))
                    {
                        data.createAlbumFolder = (line.Remove(0, 12) == "Y");
                    }
                    else if (line.StartsWith("album="))
                    {
                        data.albumName = line.Remove(0, 6);
                    }
                }
                data.specificGameCode = false;
                data.gameCodeMatch = "00000000000000000000000000000000";
            }
            catch (Exception)
            {
                data.source = "E:\\Nintendo\\Album";
                data.target = "C:\\";

                data.createAlbumFolder = false;
                data.albumName = "Album";

                data.maintainDateFolders = true;
                data.dateMode = DateMode.KeepYMD;

                data.separateShotsAndClips = false;
                data.clipFolderName = "Clips";
                data.shotFolderName = "Screenshots";

                data.includeExtra = false;
                data.separateExtra = false;

                data.separateByGame = false;
                data.specificGameCode = false;
                data.gameCodeMatch = "00000000000000000000000000000000";
            }


            return data;
        }

        public static void SaveUserSettings(OperationData data) {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"UserSettings.dat", false)) {
                file.WriteLine("source=" + data.source);
                file.WriteLine("target=" + data.target);
                file.WriteLine("date=" + (data.maintainDateFolders ? "Y" : "N"));
                file.WriteLine("dateformat=" + (int)data.dateMode);
                file.WriteLine("shotclip=" + (data.separateShotsAndClips ? "Y" : "N"));
                file.WriteLine("screenshots=" + data.shotFolderName);
                file.WriteLine("clips=" + data.clipFolderName);
                file.WriteLine("extra=" + (data.includeExtra ? "Y" : "N"));
                file.WriteLine("separateextra=" + (data.separateExtra ? "Y" : "N"));
                file.WriteLine("game=" + (data.separateByGame ? "Y" : "N"));
                //file.WriteLine("gameentry=" + (data.specificGameCode);
                file.WriteLine("createalbum=" + (data.createAlbumFolder ? "Y" : "N" ));
                file.WriteLine("album=" + data.albumName);
            }
        }
    }
}
