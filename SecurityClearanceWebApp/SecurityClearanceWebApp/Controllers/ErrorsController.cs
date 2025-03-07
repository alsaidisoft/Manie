using System.Web.Mvc;

namespace SecurityClearanceWebApp.Controllers
{
	public class ErrorsController : Controller
	{
		
		public ErrorsController() {
			// don't edit ViewBag.controllerName 
			 ViewBag.controllerName = "Errors";
			 ViewBag.controllerIconClass = "fa fa-home";
			ViewBag.controllerNamePlural = "Errors";
			ViewBag.controllerNameSingular = "Errors";
		}


        public ActionResult Index()
        {
           
            return View();
        }

        public ActionResult JundError()
        {

            return View();
        }
    }
}