using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
    //this controller is for car catigory (وكالات السيارات)
	public class VechilecatigorymstController : Controller
	{
        //get db connection
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify generalfunction class to use some ready functions 
        private GeneralFunctions general = new GeneralFunctions();
        //st title for the controller from the resources 
        private string title = Resources.Settings.ResourceManager.GetString("vehicle_cat_a" + "_" + "ar");
        public VechilecatigorymstController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Vechilecatigorymst";
            //set fontawsome icon for this controller
			ViewBag.controllerIconClass = "fa fa-car";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("vehicle_cat_a" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;

        }


        // this is the main view to add, edit, delete and retrive car catigories 
        public ActionResult Index()
        {
            
                //check if the current user is developer 
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    general.NotFound();
                }

             
            
             

                return View(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true).OrderByDescending(a => a.VECHILE_CODE).ToList());
            
          
		}

        //add and edit new data
        public JsonResult SaveDataInDatabase(RA42_VECHILE_CATIGORY_MST model)
        {

            var result = false;
            try
            {
                //if the VECHIL_CODE is > 0 that means this record needs update
                if (model.VECHILE_CODE > 0)
                {
                    //check update permession for the current user 
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
                    //check if record is in the table, then update the record 
                    RA42_VECHILE_CATIGORY_MST rA42_VECHILE_CATIGORY_MST = db.RA42_VECHILE_CATIGORY_MST.SingleOrDefault(x => x.DLT_STS != true && x.VECHILE_CODE == model.VECHILE_CODE);
                    rA42_VECHILE_CATIGORY_MST.VECHILE_CAT = model.VECHILE_CAT;
                    rA42_VECHILE_CATIGORY_MST.VECHILE_CAT_E = model.VECHILE_CAT_E;
                    //rA42_VECHILE_CATIGORY_MST.FORCE_ID = model.FORCE_ID;
                    rA42_VECHILE_CATIGORY_MST.REMARKS = model.REMARKS;
                    rA42_VECHILE_CATIGORY_MST.UPD_BY = currentUser;
                    rA42_VECHILE_CATIGORY_MST.UPD_DT = DateTime.Now;
                    rA42_VECHILE_CATIGORY_MST.DLT_STS = false;
                    db.SaveChanges();
                    result = true;
                    AddToast(new Toast("",
                   GetResourcesValue("success_update_message"),
                   "green"));
                }
                else
                {
                    //check if the current user has add permession 
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
                    RA42_VECHILE_CATIGORY_MST rA42_VECHILE_CATIGORY_MST = new RA42_VECHILE_CATIGORY_MST();
                    rA42_VECHILE_CATIGORY_MST.VECHILE_CAT = model.VECHILE_CAT;
                    rA42_VECHILE_CATIGORY_MST.VECHILE_CAT_E = model.VECHILE_CAT_E;
                   // rA42_VECHILE_CATIGORY_MST.FORCE_ID = model.FORCE_ID;
                    rA42_VECHILE_CATIGORY_MST.REMARKS = model.REMARKS;
                    rA42_VECHILE_CATIGORY_MST.UPD_BY = currentUser;
                    rA42_VECHILE_CATIGORY_MST.UPD_DT = DateTime.Now;
                    rA42_VECHILE_CATIGORY_MST.CRD_BY = currentUser;
                    rA42_VECHILE_CATIGORY_MST.CRD_DT = DateTime.Now;
                    rA42_VECHILE_CATIGORY_MST.DLT_STS = false;
                    db.RA42_VECHILE_CATIGORY_MST.Add(rA42_VECHILE_CATIGORY_MST);
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
        //get specific record details as json result 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var value = db.RA42_VECHILE_CATIGORY_MST.Where(a => a.VECHILE_CODE == Id).FirstOrDefault();
            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        //delete specific record 
        public JsonResult DltRecordById(int Id)
        {
            //check if the current user has deleting permession 
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
            RA42_VECHILE_CATIGORY_MST rA42_VECHILE_CATIGORY_MST = db.RA42_VECHILE_CATIGORY_MST.Where(x => x.VECHILE_CODE == Id).FirstOrDefault();
            if (rA42_VECHILE_CATIGORY_MST != null)
            {
                rA42_VECHILE_CATIGORY_MST.UPD_BY = currentUser;
                rA42_VECHILE_CATIGORY_MST.UPD_DT = DateTime.Now;
                //we dont delete record, we haide record by set DLT_STS to true 
                rA42_VECHILE_CATIGORY_MST.DLT_STS = true;
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
