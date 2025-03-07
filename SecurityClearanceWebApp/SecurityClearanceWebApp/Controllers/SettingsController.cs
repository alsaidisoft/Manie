using System.Collections.Generic;
using System.Web.Mvc;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using APP.Filters;
using APP.Util;
using portal.Controllers;
using SecurityClearanceWebApp.Models;
using SecurityClearanceWebApp.Util;

namespace APP.Controllers
{
    // to get user info, check the Filters/UserInfoFilter.cs
    [UserInfoFilter]

    //this controller is the show main page of settings with current user info and permessions
    public class SettingsController : Controller
    {
        //get db connection 
        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunction class
        private GeneralFunctions general = new GeneralFunctions();
        //get title of the controller from the resources 
        private string title = Resources.Settings.ResourceManager.GetString("Settings" + "_" + "ar");

        public SettingsController()
        {
            //dont edit this section
            ViewBag.Settings = "Settings";
            ViewBag.controllerName = "Settings";
            //set fontawsome icon of the controller
            ViewBag.controllerIconClass = "fa fa-cogs";
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;


        }

        //this is the main view 
        public ActionResult Index()
        {
            //if the current user not has any permession of these, show notfound page 
            if (ViewBag.ADMIN == true || ViewBag.STN == true || ViewBag.DEVELOPER == true)
            {
                if (ViewBag.DEVELOPER != true)
                {
                    if (ViewBag.ADMIN != true)
                    {
                        
                            if (ViewBag.STN != true)
                            {
                                general.NotFound();
                            }
                        
                    }
                }
                if (ViewBag.ADMIN != true){
                    if (ViewBag.DEVELOPER != true)
                    {
                       
                            if (ViewBag.STN != true)
                            {
                                general.NotFound();
                            }
                        
                    }
                }

                if (ViewBag.STN != true)
                {
                    if (ViewBag.DEVELOPER != true)
                    {

                        if (ViewBag.ADMIN != true)
                        {
                            general.NotFound();
                        }

                    }
                    if (ViewBag.ADMIN != true)
                    {

                        if (ViewBag.DEVELOPER != true)
                        {
                            general.NotFound();
                        }

                    }
                }


            }
            else
            {
                general.NotFound();
            }
            return View();
        }

        
    }
}