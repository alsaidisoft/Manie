using APP.Filters;
using SecurityClearanceWebApp.Filters;
using System.Web;
using System.Web.Mvc;

namespace SecurityClearanceWebApp
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new LogWriterFilter());
            filters.Add(new PermissionsFilter());
            filters.Add(new UserInfoFilter());
            filters.Add(new PassesFilter());
            filters.Add(new PermitsFilter());
        }
    }
}
