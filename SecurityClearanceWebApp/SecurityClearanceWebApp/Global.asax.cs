
using System.Web.Http;
using System.Web.Mvc;

using System.Web.Routing;

namespace SecurityClearanceWebApp
{
    public class MvcApplication : System.Web.HttpApplication
    {

        protected void Application_Start()
        {

            //to get online users
            //Application["OnlineUsers"] = 0;
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            //BundleConfig.RegisterBundles(BundleTable.Bundles);
            
            HttpConfiguration config = GlobalConfiguration.Configuration;
            config.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling =
                Newtonsoft.Json.ReferenceLoopHandling.Ignore;

        }


        //get online visitors 
        //protected void Session_Start(object sender, EventArgs e)
        //{
        //    Application.Lock();
        //    Application["OnlineUsers"] = (int)Application["OnlineUsers"] + 1;
        //    Application.UnLock();
        //}
        ////change total visitors when one visitor leav
        //protected void Session_End(object sender, EventArgs e)
        //{
        //    Application.Lock();
        //    Application["OnlineUsers"] = (int)Application["OnlineUsers"] - 1;
        //    Application.UnLock();


        //}

        //protected void Application_Error(object Sender, EventArgs args)
        //{
        //    Exception exception = Server.GetLastError();
        //    Server.ClearError();
        //    Response.Redirect("~/ar/Errors/Index");
        //}

    }
}
