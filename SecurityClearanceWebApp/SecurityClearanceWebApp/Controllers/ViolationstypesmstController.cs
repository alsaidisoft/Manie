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
	public class ViolationstypesmstController : Controller
	{
        //get db connection 
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class
        private GeneralFunctions general = new GeneralFunctions();
        //set title for the controller from resources 
        private string title = Resources.Settings.ResourceManager.GetString("Violations_type" + "_" + "ar");
        public ViolationstypesmstController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Violationstypesmst";
            //set icon from fontawsome for the controller 
			ViewBag.controllerIconClass = "fa fa-exclamation-triangle";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("Violations_type" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;

        }


		// this is the main view to add, edit, delete and retrive data 
		public ActionResult Index()
		{
            //check if the current user has view permession 
            if (ViewBag.DEVELOPER != true)
            {
                general.NotFound();

            }

            //get all unhideing RA42_VIOLATIONS_MST
            var rA42_VIOLATIONS_MST = db.RA42_VIOLATIONS_MST;
			return View(rA42_VIOLATIONS_MST.Where(a=>a.DLT_STS != true).OrderByDescending(a=>a.VIOLATION_CODE).ToList());
		}
        //add and edit new data
        public JsonResult SaveDataInDatabase(RA42_VIOLATIONS_MST model)
        {

            var result = false;
            try
            {
                //if the VECHILE_COLOR_CODE greater than 0 that means this record needs update 
                if (model.VIOLATION_CODE > 0)
                {
                   
                        //check if the current user is admin or developer 
                        if (ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_up"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }
                    
                    //check if the record is in the table 
                    RA42_VIOLATIONS_MST rA42_VIOLATIONS_MST = db.RA42_VIOLATIONS_MST.SingleOrDefault(x => x.DLT_STS != true && x.VIOLATION_CODE == model.VIOLATION_CODE);
                    rA42_VIOLATIONS_MST.VIOLATION_TYPE_A = model.VIOLATION_TYPE_A;
                    rA42_VIOLATIONS_MST.VIOLATION_TYPE_E = model.VIOLATION_TYPE_E;
                    rA42_VIOLATIONS_MST.REMARKS = model.REMARKS;
                    rA42_VIOLATIONS_MST.UPD_BY = currentUser;
                    rA42_VIOLATIONS_MST.UPD_DT = DateTime.Now;
                    rA42_VIOLATIONS_MST.DLT_STS = false;
                    db.SaveChanges();
                    result = true;
                    AddToast(new Toast("",
                   GetResourcesValue("success_update_message"),
                   "green"));
                }
                else
                {
                   
                        //check if the current user is admin or developer 
                        if (ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_add"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }
                        
                    
                    //add new record 
                    RA42_VIOLATIONS_MST rA42_VIOLATIONS_MST = new RA42_VIOLATIONS_MST();
                    rA42_VIOLATIONS_MST.VIOLATION_TYPE_A = model.VIOLATION_TYPE_A;
                    rA42_VIOLATIONS_MST.VIOLATION_TYPE_E = model.VIOLATION_TYPE_E;
                    rA42_VIOLATIONS_MST.REMARKS = model.REMARKS;
                    rA42_VIOLATIONS_MST.UPD_BY = currentUser;
                    rA42_VIOLATIONS_MST.UPD_DT = DateTime.Now;
                    rA42_VIOLATIONS_MST.CRD_BY = currentUser;
                    rA42_VIOLATIONS_MST.CRD_DT = DateTime.Now;
                    rA42_VIOLATIONS_MST.DLT_STS = false;
                    db.RA42_VIOLATIONS_MST.Add(rA42_VIOLATIONS_MST);
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
            var value = db.RA42_VIOLATIONS_MST.Where(a => a.VIOLATION_CODE == Id).FirstOrDefault();
            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        //this function to hide any record 
        public JsonResult DltRecordById(int Id)
        {
            
                //check if the current user is admin or developer 
                if (ViewBag.DEVELOPER != true)
                {
                    AddToast(new Toast("",
                  GetResourcesValue("Dont_have_permissions_to_dlt"),
                  "red"));
                    return Json(false, JsonRequestBehavior.AllowGet);
                }
                
            
            bool result = false;

            RA42_VIOLATIONS_MST rA42_VIOLATIONS_MST = db.RA42_VIOLATIONS_MST.Where(x => x.VIOLATION_CODE == Id).FirstOrDefault();
            if (rA42_VIOLATIONS_MST != null)
            {
                rA42_VIOLATIONS_MST.UPD_BY = currentUser;
                rA42_VIOLATIONS_MST.UPD_DT = DateTime.Now;
                //hide record by setting DLT_STS = true 
                rA42_VIOLATIONS_MST.DLT_STS = true;
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
