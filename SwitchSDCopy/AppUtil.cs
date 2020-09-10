using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SwitchSDCopy
{
    public static class AppUtil
    {
        public static void ShowMessageWarning(string message, string description)
        {
            MessageBoxButtons buttons = MessageBoxButtons.OK;

            System.Windows.Forms.MessageBox.Show(message, description, buttons);
        }
        public static DialogResult PromptUserYesNo(string title, string message)
        {
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            return System.Windows.Forms.MessageBox.Show(message, title, buttons);
        }

        public static string GetDatePath(int folderType, string year, string month, string day)
        {
            switch (folderType)
            {
                case 1: return "\\" + year;
                case 2: return "\\" + year + "\\" + month;
                case 3: return "\\" + year + "\\" + month + "\\" + day;
            }
            return "";
        }

        public static bool CreateDir(string path)
        {
            try
            {

                Directory.CreateDirectory(path);
            }
            catch (Exception)
            {
                //ShowMessageWarning(ex.Message, "SwitchSDCopy - Failed to create directory");
                return false;
            }
            return true;
        }
    }
}
