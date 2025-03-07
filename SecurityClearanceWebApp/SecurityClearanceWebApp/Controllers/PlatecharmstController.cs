using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using APP.Filters;
using APP.Util;
using portal.Controllers;
using SecurityClearanceWebApp.Models;
using SecurityClearanceWebApp.Util;

namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    //this is plate chars controller, this controller responsible for plate chars of cars
	public class PlatecharmstController : Controller
	{
        //get db conection 
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class
        private GeneralFunctions general = new GeneralFunctions();
        //set controller title from resources
        private string title = Resources.Settings.ResourceManager.GetString("plate_char_a" + "_" + "ar");
        public PlatecharmstController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Platecharmst";
            if(Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("plate_char_a" + "_" + "en");
            }
            //set fontawsome icon of controller
			ViewBag.controllerIconClass = "fa fa-th";
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;

        }


		// GET: platecharmst
		public ActionResult Index()
		{
            //check current user view permession
           
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER !=true)
                {
                    general.NotFound();
                }
               
            
          //list all platchars
            return View(db.RA42_PLATE_CHAR_MST.Where(a=>a.DLT_STS !=true).OrderByDescending(a=>a.PLATE_CHAR_CODE).ToList());
		}

        //save or edit new data
        public JsonResult SaveDataInDatabase(RA42_PLATE_CHAR_MST model)
        {

            var result = false;
            try
            {
                //if PLAT_CHAR_CODE > 0 that means this record need update 
                if (model.PLATE_CHAR_CODE > 0)
                {
                    //check update permession for the current user 
                    if (ViewBag.UP != true)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_up"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }
                    }
                    //check if record in th table then update 
                    RA42_PLATE_CHAR_MST rA42_PLATE_CHAR_MST = db.RA42_PLATE_CHAR_MST.SingleOrDefault(x => x.DLT_STS != true && x.PLATE_CHAR_CODE == model.PLATE_CHAR_CODE);
                    rA42_PLATE_CHAR_MST.PLATE_CHAR = model.PLATE_CHAR;
                    rA42_PLATE_CHAR_MST.PLATE_CHAR_E = model.PLATE_CHAR_E;
                    rA42_PLATE_CHAR_MST.UPD_BY = currentUser;
                    rA42_PLATE_CHAR_MST.UPD_DT = DateTime.Now;
                    rA42_PLATE_CHAR_MST.DLT_STS = false;
                    db.SaveChanges();
                    result = true;
                    AddToast(new Toast("",
                   GetResourcesValue("success_update_message"),
                   "green"));
                }
                else
                {
                    //check add permession for the current user 
                    if (ViewBag.AD != true)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_add"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }
                        
                    }
                    //add new plate char
                    RA42_PLATE_CHAR_MST rA42_PLATE_CHAR_MST = new RA42_PLATE_CHAR_MST();
                    rA42_PLATE_CHAR_MST.PLATE_CHAR = model.PLATE_CHAR;
                    rA42_PLATE_CHAR_MST.PLATE_CHAR_E = model.PLATE_CHAR_E;
                    rA42_PLATE_CHAR_MST.UPD_BY = currentUser;
                    rA42_PLATE_CHAR_MST.UPD_DT = DateTime.Now;
                    rA42_PLATE_CHAR_MST.CRD_BY = currentUser;
                    rA42_PLATE_CHAR_MST.CRD_DT = DateTime.Now;
                    rA42_PLATE_CHAR_MST.DLT_STS = false;
                    db.RA42_PLATE_CHAR_MST.Add(rA42_PLATE_CHAR_MST);
                    db.SaveChanges();
                    result = true;
                    AddToast(new Toast("",
                    GetResourcesValue("success_create_message"),
                    "green"));
                }
            }
            catch (Exception ex)
            {
                AddToast(new Toast("",
                GetResourcesValue("error_create_message"),
                "red"));
                throw ex;
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }
        //retrive data for specifc record as json result 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var value = db.RA42_PLATE_CHAR_MST.Where(a => a.PLATE_CHAR_CODE == Id).FirstOrDefault();
            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        //delete or hide specific record 
        public JsonResult DltRecordById(int Id)
        {
            //check deleting permession for current user 
            if (ViewBag.DLT != true)
            {
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    AddToast(new Toast("",
                  GetResourcesValue("Dont_have_permissions_to_dlt"),
                  "red"));
                    return Json(false, JsonRequestBehavior.AllowGet);
                }
                
            }
            bool result = false;
            //check if record in the table 
            RA42_PLATE_CHAR_MST rA42_PLATE_CHAR_MST = db.RA42_PLATE_CHAR_MST.Where(x => x.PLATE_CHAR_CODE == Id).FirstOrDefault();
            if (rA42_PLATE_CHAR_MST != null)
            {
                rA42_PLATE_CHAR_MST.UPD_BY = currentUser;
                rA42_PLATE_CHAR_MST.UPD_DT = DateTime.Now;
                //hide record by set DLT_STS to true 
                rA42_PLATE_CHAR_MST.DLT_STS = true;
                db.SaveChanges();
                result = true;
                AddToast(new Toast("",
                   GetResourcesValue("success_delete_message"),
                   "green"));
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				db.Dispose();
			}
			base.Dispose(disposing);
		}

        public string GetResourcesValue(string key)
        {
            return Resources.Common
                .ResourceManager
                .GetString(key + "_" + ViewBag.lang);
        }

        public void AddToast(Toast toast)
        {
            toasts.Add(toast);
            TempData["toasts"] = toasts;
        }

        
    }
}
