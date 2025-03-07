
using System.Threading.Tasks;
using System.Web.Mvc;
using SecurityClearanceWebApp.Services;

namespace APP.Filters
{
    //this is notifications filter
    //for example, when the security officer who has workflow id type 4, he will get notification when he recive new requests in different types of permits  
    public class PassesFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {

            var countService = new PermitsCountService();
            filterContext.Controller.ViewBag.MyPermits = Task.Run(async () => await countService.CountMyPermits()).Result;
            filterContext.Controller.ViewBag.OtherTotal = Task.Run(async () => await countService.CountOtherPermits()).Result;
            filterContext.Controller.ViewBag.MyPermitsTotal = Task.Run(async () => await countService.CountMyPermitsTotal()).Result;
        }

        

    }



    }
