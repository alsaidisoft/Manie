using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace APP.Util


{
    public class GeneralFunctions
    {

        public void NotFound()
        {
            HttpContext.Current.Response.Redirect("~/ar/Home/NotFound", false);
        }

        public bool CheckFileType(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            switch (ext.ToLower())
            {
                case ".PDF":
                    return true;
                case ".pdf":
                    return true;
                case ".JPG":
                    return true;
                case ".jpg":
                    return true;
                case ".JPEG":
                    return true;
                case ".jpeg":
                    return true;
                case ".PNG":
                    return true;
                case ".png":
                    return true;
                case ".GIF":
                    return true;
                case ".gif":
                    return true;
                case ".SVG":
                    return true;
                case ".svg":
                    return true;
                case ".DOCX":
                    return true;
                case ".docx":
                    return true;
                case ".PPTX":
                    return true;
                case ".pptx":
                    return true;
                case ".XLSX":
                    return true;
                case ".xlsx":
                    return true;
                default:
                    return false;
            }
        }
        public bool CheckPersonalImage(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            switch (ext.ToLower())
            {

                case ".JPG":
                    return true;
                case ".jpg":
                    return true;
                case ".JPEG":
                    return true;
                case ".jpeg":
                    return true;
                //case ".PNG":
                //    return true;
                //case ".png":
                //    return true;
                case ".GIF":
                    return true;
                case ".gif":
                    return true;
                default:
                    return false;
            }
        }
        public bool RemoveFileFromServer(string path)
        {
            var fullPath = HttpContext.Current.Request.MapPath(path);
            if (!System.IO.File.Exists(fullPath)) return false;

            try //Maybe error could happen like Access denied or Presses Already User used
            {
                System.IO.File.Delete(fullPath);
                return true;
            }
            catch (Exception e)
            {
                //Debug.WriteLine(e.Message);
            }
            return false;
        }

        public bool ResizeImage(HttpPostedFileBase fileToUpload, string myfile)
        {



            bool check = false;

            try
            {
                using (Image image = Image.FromStream(fileToUpload.InputStream, true, false))
                {
                    var path = Path.Combine(System.Web.HttpContext.Current.Server.MapPath("~/Files/Profiles"), myfile);
                    try
                    {
                        //Size can be change according to your requirement 
                        float thumbWidth = 270F;
                        float thumbHeight = 180F;
                        //calculate  image  size
                        if (image.Width > image.Height)
                        {
                            thumbHeight = ((float)image.Height / image.Width) * thumbWidth;
                        }
                        else
                        {
                            thumbWidth = ((float)image.Width / image.Height) * thumbHeight;
                        }

                        int actualthumbWidth = Convert.ToInt32(Math.Floor(thumbWidth));
                        int actualthumbHeight = Convert.ToInt32(Math.Floor(thumbHeight));
                        var thumbnailBitmap = new Bitmap(actualthumbWidth, actualthumbHeight);
                        var thumbnailGraph = Graphics.FromImage(thumbnailBitmap);
                        thumbnailGraph.CompositingQuality = CompositingQuality.HighQuality;
                        thumbnailGraph.SmoothingMode = SmoothingMode.HighQuality;
                        thumbnailGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        var imageRectangle = new Rectangle(0, 0, actualthumbWidth, actualthumbHeight);
                        thumbnailGraph.DrawImage(image, imageRectangle);
                        var ms = new MemoryStream();
                        thumbnailBitmap.Save(path, ImageFormat.Jpeg);
                        ms.Position = 0;
                        GC.Collect();
                        thumbnailGraph.Dispose();
                        thumbnailBitmap.Dispose();
                        image.Dispose();
                        check = true;
                    }
                    catch (Exception ex)
                    {
                        check = false;
                        throw ex;

                    }
                }
            }
            catch (Exception ex)
            {
                check = false;
                throw ex;
            }
            return check;
        }


        public bool ResizeSliderImage(HttpPostedFileBase fileToUpload, string myfile)
        {



            bool check = false;

            try
            {
                using (Image image = Image.FromStream(fileToUpload.InputStream, true, false))
                {
                    var path = Path.Combine(System.Web.HttpContext.Current.Server.MapPath("~/Files/Others"), myfile);
                    try
                    {
                        //Size can be change according to your requirement 
                        float thumbWidth = 1000F;
                        float thumbHeight = 500F;
                        //calculate  image  size
                        if (image.Width > image.Height)
                        {
                            thumbHeight = ((float)image.Height / image.Width) * thumbWidth;
                        }
                        else
                        {
                            thumbWidth = ((float)image.Width / image.Height) * thumbHeight;
                        }

                        int actualthumbWidth = Convert.ToInt32(Math.Floor(thumbWidth));
                        int actualthumbHeight = Convert.ToInt32(Math.Floor(thumbHeight));
                        var thumbnailBitmap = new Bitmap(actualthumbWidth, actualthumbHeight);
                        var thumbnailGraph = Graphics.FromImage(thumbnailBitmap);
                        thumbnailGraph.CompositingQuality = CompositingQuality.HighQuality;
                        thumbnailGraph.SmoothingMode = SmoothingMode.HighQuality;
                        thumbnailGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        var imageRectangle = new Rectangle(0, 0, actualthumbWidth, actualthumbHeight);
                        thumbnailGraph.DrawImage(image, imageRectangle);
                        var ms = new MemoryStream();
                        thumbnailBitmap.Save(path, ImageFormat.Jpeg);
                        ms.Position = 0;
                        GC.Collect();
                        thumbnailGraph.Dispose();
                        thumbnailBitmap.Dispose();
                        image.Dispose();
                        check = true;
                    }
                    catch (Exception ex)
                    {
                        check = false;
                        throw ex;

                    }
                }
            }
            catch (Exception ex)
            {
                check = false;
                throw ex;
            }
            return check;
        }
        public bool ResizeSignature(HttpPostedFileBase fileToUpload, string myfile)
        {



            bool check = false;

            try
            {
                using (Image image1 = Image.FromStream(fileToUpload.InputStream, true, false))
                {
                    Image image = Maketransparent(image1);
                    var path = Path.Combine(System.Web.HttpContext.Current.Server.MapPath("~/Files/Signatures"), myfile);
                    try
                    {
                        //Size can be change according to your requirement 
                        float thumbWidth = 270F;
                        float thumbHeight = 100F;
                        //calculate  image  size
                        if (image.Width > image.Height)
                        {
                            thumbHeight = ((float)image.Height / image.Width) * thumbWidth;
                        }
                        else
                        {
                            thumbWidth = ((float)image.Width / image.Height) * thumbHeight;
                        }

                        int actualthumbWidth = Convert.ToInt32(Math.Floor(thumbWidth));
                        int actualthumbHeight = Convert.ToInt32(Math.Floor(thumbHeight));
                        var thumbnailBitmap = new Bitmap(actualthumbWidth, actualthumbHeight);
                        var thumbnailGraph = Graphics.FromImage(thumbnailBitmap);
                        thumbnailGraph.CompositingQuality = CompositingQuality.HighQuality;
                        thumbnailGraph.SmoothingMode = SmoothingMode.HighQuality;
                        thumbnailGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        var imageRectangle = new Rectangle(0, 0, actualthumbWidth, actualthumbHeight);
                        thumbnailGraph.DrawImage(image, imageRectangle);
                        var ms = new MemoryStream();
                        thumbnailBitmap.Save(path, ImageFormat.Png);
                        ms.Position = 0;
                        GC.Collect();
                        thumbnailGraph.Dispose();
                        thumbnailBitmap.Dispose();
                        image.Dispose();
                        check = true;

                    }
                    catch (Exception ex)
                    {
                        check = false;
                        throw ex;

                    }
                }
            }
            catch (Exception ex)
            {
                check = false;
                throw ex;
            }
            return check;
        }

        public static Bitmap Maketransparent(Image image)
        {
            Bitmap b = new Bitmap(image);
            var replacmentColor = Color.FromArgb(255, 255, 255);
            var tolerance = 10;
            for (int i = b.Size.Width - 1; i >= 0; i--)
            {
                for (int j = b.Size.Height - 1; j >= 0; j--)
                {
                    var col = b.GetPixel(i, j);
                    if (255 - col.R < tolerance && 255 - col.G < tolerance && 255 - col.B < tolerance)
                    {
                        b.SetPixel(i, j, replacmentColor);
                    }
                }
            }
            b.MakeTransparent(replacmentColor);
            return b;
        }



        public bool DownloadImage(string myfile, string fileName)
        {



            bool ischeck = false;


            using (WebClient webClient = new WebClient())
            {
                try
                {
                    webClient.Headers.Add("user-agent", "Mozilla/4.0(compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705)");
                    webClient.UseDefaultCredentials = true;
                    byte[] data = webClient.DownloadData(myfile);


                    using (MemoryStream mem = new MemoryStream(data))
                    {
                        using (var myImage = Image.FromStream(mem))
                        {
                            var path = Path.Combine(System.Web.HttpContext.Current.Server.MapPath("~/Files/Profiles"), fileName);
                            try
                            {
                                //Size can be change according to your requirement
                                float thumbWidth = 270F;
                                float thumbHeight = 180F;
                                //calculate  image  size
                                if (myImage.Width > myImage.Height)
                                {
                                    thumbHeight = ((float)myImage.Height / myImage.Width) * thumbWidth;
                                }
                                else
                                {
                                    thumbWidth = ((float)myImage.Width / myImage.Height) * thumbHeight;
                                }

                                int actualthumbWidth = Convert.ToInt32(Math.Floor(thumbWidth));
                                int actualthumbHeight = Convert.ToInt32(Math.Floor(thumbHeight));
                                var thumbnailBitmap = new Bitmap(actualthumbWidth, actualthumbHeight);
                                var thumbnailGraph = Graphics.FromImage(thumbnailBitmap);
                                thumbnailGraph.CompositingQuality = CompositingQuality.HighQuality;
                                thumbnailGraph.SmoothingMode = SmoothingMode.HighQuality;
                                thumbnailGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                var imageRectangle = new Rectangle(0, 0, actualthumbWidth, actualthumbHeight);
                                thumbnailGraph.DrawImage(myImage, imageRectangle);
                                var ms = new MemoryStream();
                                thumbnailBitmap.Save(path, ImageFormat.Jpeg);
                                ms.Position = 0;
                                GC.Collect();
                                thumbnailGraph.Dispose();
                                thumbnailBitmap.Dispose();
                                myImage.Dispose();
                                ischeck = true;

                                //myfile = "~/Files/Profiles/" + fileName + ".gif";
                            }
                            catch (Exception ex)
                            {
                                ischeck = false;
                                throw ex;
                               

                            }
                        }


                        return ischeck;
                    }

                }
                catch (Exception exc)
                {
                    return ischeck;
                }


            }
        }
           
    }
    
}
