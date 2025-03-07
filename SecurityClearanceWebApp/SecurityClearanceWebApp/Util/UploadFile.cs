using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
namespace SecurityClearanceWebApp.Util
{
    public class UploadFile
    {
        public static void UploadList(IEnumerable<HttpPostedFileBase> files,
            List<string> filesTitles, string uploadPath)
        {

            var i = 0;
            try
            {
                if (files.Any())
                {
                    foreach (var file in files)
                    {
                        UploadFile.Upload(file, uploadPath);
                        i++;
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        public static string Upload(HttpPostedFileBase file, string uploadPath)
        {
            UploadFile.CheckFolderIsExists("UploadFiles/" + uploadPath + "/");

            string fileName = null;
            string extension = Path.GetExtension(file.FileName);
            if (CheckFileType(extension))
            {
                fileName = DateTime.Now.ToString("yymmssfff") + extension;
                // store the file inside ~/App_Data/uploads folder
                string path = Path.Combine(HttpContext.Current.Server.MapPath("~/UploadFiles/" + uploadPath + "/"), fileName);
                file.SaveAs(path);
            }
            return fileName;
        }

        public static bool CheckFolderIsExists(string FolderName)
        {
            var directory = new DirectoryInfo(HttpContext.Current.Server.MapPath("~/" + FolderName));
            if (!directory.Exists)
            {
                directory.Create();
                return true;
            }
            else
            {
                return true;
            }
        }

        public static bool CheckFileType(string extension)
        {
            List<string> typesList = new List<string>()
            {
                "pdf",
                "rtf",
                "doc",
                "docx",
                "txt",
                "png",
                "jpeg",
                "jpg",
                "gif",
                "psd",
                "raw"
            };

            extension = extension.Replace(".", "");
            foreach (var type in typesList)
            {
                if (extension.ToLower() == type.ToLower())
                {
                    return true;
                }
            }

            return false;
        }

    }
}