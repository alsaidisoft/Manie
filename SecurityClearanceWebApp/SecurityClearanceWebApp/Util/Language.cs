using System.Web;
using Microsoft.Ajax.Utilities;
using Resources;

namespace APP.Util
{
    public class Language
    {
        public static string GetUrl(string url)
        {
            var currentLang = GetCurrentLang();
            var toLang = "ar";
            toLang = (currentLang == "ar") ? "en" : "ar";
            string newUrl = "";
            if (url.ToLower().Contains("/" + currentLang))
            {
                newUrl = url.Replace("/" + currentLang, "/" + toLang);
            }
            else
            {
                if (url.EndsWith("/"))
                {
                    newUrl = url + toLang;
                }
                else
                {
                    newUrl = url + "/" + toLang;
                }
            }
            return newUrl;
        }

        public static string GetCurrentLang()
        {

            var lang = HttpContext.Current.Request.RequestContext.RouteData.Values["language"] as string;
            if (lang.IsNullOrWhiteSpace())
            {
                lang = "ar";
            }

            if (lang != "ar" && lang != "en")
            {
                lang = "ar";
            }

            return lang;
        }

        public static string GetResourceValue(string resource, string key)
        {
            switch (resource)
            {
                case "Common":
                    {
                        return Common.ResourceManager.GetString(key + "_" + GetCurrentLang());
                    }
                //case "AppValues":
                //{
                //    return AppValues.ResourceManager.GetString(key + "_" + getCurrentLang());
                //}
                default:
                    {
                        return "resource file not found";
                    }

            }

        }
    }
}