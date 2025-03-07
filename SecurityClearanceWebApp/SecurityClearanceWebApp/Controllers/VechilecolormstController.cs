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
    //this is color controller for the vechiles 
	public class VechilecolormstController : Controller
	{
        //get db connection 
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class
        private GeneralFunctions general = new GeneralFunctions();
        //set title for the controller from resources 
        private string title = Resources.Settings.ResourceManager.GetString("vehicle_color_a" + "_" + "ar");
        public VechilecolormstController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Vechilecolormst";
            //set icon from fontawsome for the controller 
			ViewBag.controllerIconClass = "fa fa-palette";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("vehicle_color_a" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;

        }


		// this is the main view to add, edit, delete and retrive data 
		public ActionResult Index()
		{
            //check if the current user has view permession 
           
                //check if the current user is admin or developer 
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    general.NotFound();
                }

            
            //get all unhideing colors
            var rA42_VECHILE_COLOR_MST = db.RA42_VECHILE_COLOR_MST;
			return View(rA42_VECHILE_COLOR_MST.Where(a=>a.DLT_STS != true).OrderByDescending(a=>a.VECHILE_COLOR_CODE).ToList());
		}
        //add and edit new data
        public JsonResult SaveDataInDatabase(RA42_VECHILE_COLOR_MST model)
        {

            var result = false;
            try
            {
                //if the VECHILE_COLOR_CODE greater than 0 that means this record needs update 
                if (model.VECHILE_COLOR_CODE > 0)
                {
                    //check if current user has update permession 
                    if (ViewBag.UP != true)
                    {
                        //check if the current user is admin or developer 
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_up"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }
                    }
                    //check if the record is in the table 
                    RA42_VECHILE_COLOR_MST rA42_VECHILE_COLOR_MST = db.RA42_VECHILE_COLOR_MST.SingleOrDefault(x => x.DLT_STS != true && x.VECHILE_COLOR_CODE == model.VECHILE_COLOR_CODE);
                    rA42_VECHILE_COLOR_MST.COLOR = model.COLOR;
                    rA42_VECHILE_COLOR_MST.COLOR_E = model.COLOR_E;
                    rA42_VECHILE_COLOR_MST.REMARKS = model.REMARKS;
                    rA42_VECHILE_COLOR_MST.UPD_BY = currentUser;
                    rA42_VECHILE_COLOR_MST.UPD_DT = DateTime.Now;
                    rA42_VECHILE_COLOR_MST.DLT_STS = false;
                    db.SaveChanges();
                    result = true;
                    AddToast(new Toast("",
                   GetResourcesValue("success_update_message"),
                   "green"));
                }
                else
                {
                    //check if current user has add permession 
                    if (ViewBag.AD != true)
                    {
                        //check if the current user is admin or developer 
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_add"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }
                        
                    }
                    //add new record 
                    RA42_VECHILE_COLOR_MST rA42_VECHILE_COLOR_MST = new RA42_VECHILE_COLOR_MST();
                    rA42_VECHILE_COLOR_MST.COLOR = model.COLOR;
                    rA42_VECHILE_COLOR_MST.COLOR_E = model.COLOR_E;
                    rA42_VECHILE_COLOR_MST.REMARKS = model.REMARKS;
                    rA42_VECHILE_COLOR_MST.UPD_BY = currentUser;
                    rA42_VECHILE_COLOR_MST.UPD_DT = DateTime.Now;
                    rA42_VECHILE_COLOR_MST.CRD_BY = currentUser;
                    rA42_VECHILE_COLOR_MST.CRD_DT = DateTime.Now;
                    rA42_VECHILE_COLOR_MST.DLT_STS = false;
                    db.RA42_VECHILE_COLOR_MST.Add(rA42_VECHILE_COLOR_MST);
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
        //get specific data for any record as json result 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var value = db.RA42_VECHILE_COLOR_MST.Where(a => a.VECHILE_COLOR_CODE == Id).FirstOrDefault();
            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        //this function to hide any record 
        public JsonResult DltRecordById(int Id)
        {
            //check if the current user has delete permession 
            if (ViewBag.DLT != true)
            {
                //check if the current user is admin or developer 
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    AddToast(new Toast("",
                  GetResourcesValue("Dont_have_permissions_to_dlt"),
                  "red"));
                    return Json(false, JsonRequestBehavior.AllowGet);
                }
                
            }
            bool result = false;

            RA42_VECHILE_COLOR_MST rA42_VECHILE_COLOR_MST = db.RA42_VECHILE_COLOR_MST.Where(x => x.VECHILE_COLOR_CODE == Id).FirstOrDefault();
            if (rA42_VECHILE_COLOR_MST != null)
            {
                rA42_VECHILE_COLOR_MST.UPD_BY = currentUser;
                rA42_VECHILE_COLOR_MST.UPD_DT = DateTime.Now;
                //hide record by setting DLT_STS = true 
                rA42_VECHILE_COLOR_MST.DLT_STS = true;
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
