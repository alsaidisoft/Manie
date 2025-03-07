using System;
using System.Web.Mvc;
using APP.Util;
using Newtonsoft.Json;
using SecurityClearanceWebApp.Util;

namespace SecurityClearanceWebApp.Filters
{
    public class LogWriterFilter : ActionFilterAttribute
    {

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {

                var request = filterContext.HttpContext.Request;

                Audit audit = new Audit
                {
                    UserName = (request.IsAuthenticated)
                        ? filterContext.HttpContext.User.Identity.Name.Substring(4)
                        : "Anonymous",
                    IPAddress = request.ServerVariables["HTTP_X_FORWARDED"] ?? request.UserHostAddress,
                    AreaAccessed = request.RawUrl,
                    TimesAccessed = DateTime.Now,
                };


                LogWriter.Writer(JsonConvert.SerializeObject(audit));
                base.OnActionExecuting(filterContext);
            }
            catch (Exception e)
            {
                // ignored
            }
        }
    }
}