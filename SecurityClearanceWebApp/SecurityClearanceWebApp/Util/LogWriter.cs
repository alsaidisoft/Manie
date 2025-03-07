using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Reflection;

namespace SecurityClearanceWebApp.Util
{
    public class LogWriter
    {

        public static void Writer(string audit)
        {
            LogWrite(audit);
        }
        private static void LogWrite(string logMessage)
        {
            string pathToLog = "/Logs/" + DateTime.Now.ToString("yyyy/MM");
            UploadFile.CheckFolderIsExists(pathToLog);
            string logFile = "~" + pathToLog + "/" + DateTime.Now.ToString("dd") + ".txt";
            string path = HttpContext.Current.Server.MapPath(logFile);
            try
            {
                using (StreamWriter w = File.AppendText(path))
                {
                    Log(logMessage, w);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static void Log(string logMessage, TextWriter txtWriter)
        {
            try
            {
                txtWriter.WriteLine("{0}", logMessage);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}