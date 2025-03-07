using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using APP.Filters;
using APP.Util;
using portal.Controllers;
using SecurityClearanceWebApp.Models;
using SecurityClearanceWebApp.Util;
using WebGrease.Css.Ast;


namespace APP.Controllers
{
    // to get user info, check the Filters/UserInfoFilter.cs
    //[UserInfoFilter]
    //this is home controller 
    public class HomeController : Controller
    {
        //get db connection
        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class
        private GeneralFunctions general = new GeneralFunctions();
        //set title for controller from resources 
        private string title = Resources.Common.ResourceManager.GetString("home" + "_" + "ar");

        public HomeController()
        {
            //don't edit any of theses codes 
            if (Language.GetCurrentLang() == "en") {
                title = Resources.Common.ResourceManager.GetString("home" + "_" + "en");
            }
            ViewBag.controllerName = "Home";
            //set fontawsome icon for the controller
            ViewBag.controllerIconClass = "fa fa-home";
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;

        }

        public async Task<ActionResult> Index(int? FORCE)
        {

            try
            {
                //get current user details by service number from api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;
                if (Language.GetCurrentLang() == "en")
                {
                    if (user.POSTING_STN != null)
                    {
                        ViewBag.Username = user.NAME_RANK_E + "/" + user.NAME_EMP_E;
                        //ViewBag.Station = user.NAME_UNIT_E;
                        //ViewBag.Position = user.NAME_POST_E;

                    }
                    else
                    {
                        ViewBag.Username = user.NAME_RANK_E + "/" + user.NAME_EMP_E;
                        //ViewBag.Station = user.NAME_UNIT_E;
                        //ViewBag.Position = user.NAME_TRADE_E;
                    }
                }
                else
                {
                    if (user.POSTING_STN != null)
                    {
                        // ViewBag.UNIT = user.POSTING_STN.Value;
                        ViewBag.Username = user.NAME_RANK_A + "/" + user.NAME_EMP_A;
                        //ViewBag.Station = user.NAME_UNIT_A;
                        //ViewBag.Position = user.NAME_POST_A;

                    }
                    else
                    {
                        ViewBag.Username = user.NAME_RANK_A + "/" + user.NAME_EMP_A;
                        //ViewBag.Station = user.NAME_UNIT_A;
                        //ViewBag.Position = user.NAME_TRADE_A;
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Username = currentUser;
                //TempData["Erorr"] = "Can't retrive data";
            }

            //get image slider phone numbers
            ViewBag.SLIDER = Task.Run(async () => await db.RA42_SLIDER_DTL.Where(a => a.DLT_STS != true).ToListAsync()).Result;
            //get stations and forces for menue
            if (FORCE != null)
            {
                ViewBag.STATIONS_LIST = Task.Run(async () => await db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE).ToListAsync()).Result;

            }
            else
            {
                ViewBag.STATIONS_LIST = Task.Run(async () => await db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true).ToListAsync()).Result;
            }
            if (Language.GetCurrentLang() == "en")
            {
                ViewBag.FORCES_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true).OrderBy(a => a.FORCE_CODE), "FORCE_ID", "FORCE_NAME_E");
            }
            else
            {
                ViewBag.FORCES_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true).OrderBy(a => a.FORCE_CODE), "FORCE_ID", "FORCE_NAME_A");

            }
          
            return View();
        }

        //this view to show usermanual videos 
        public ActionResult UserManual()
        {
            ViewBag.UserManual = "UserManual";
            ViewBag.controllerName = "";


            return View();
        }
        //this view to show pronter settings videos, this view for permits cell 
        public ActionResult PrinterSettings()
        {
            ViewBag.Printer_Settings = "Printer_Settings";
            ViewBag.controllerName = "";


            return View();
        }

        //this is home controller 
        public ActionResult Home()
        {
            
            return View();
        }
        //this controller will showen when the system under maintenance 
        public ActionResult Postpond()
        {

            return View();
        }
        //this is notfound page
        public ActionResult NotFound()
        {
            return View();
        }

        //this is NotSupported browse page
        public ActionResult NotSupported()
        {

            return View();
        }

    }
}