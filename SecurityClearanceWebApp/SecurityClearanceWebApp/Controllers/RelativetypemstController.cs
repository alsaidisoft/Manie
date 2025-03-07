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
    //this is relatives types master controller, here we can add, edit, delete and retrive relatives data such as (أخ، أخت، أب أم، زوج، زوجة)
	public class RelativetypemstController : Controller
	{
        //get db connection 
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class to use some functions 
        private GeneralFunctions general = new GeneralFunctions();
        //set title from resources for the controller 
        private string title = Resources.Settings.ResourceManager.GetString("RELATIVE_TYPE" + "_" + "ar");
        public RelativetypemstController() {
            ViewBag.Settings = "Settings";
            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Relativetypemst";
            //set fontawsome icon for the controller 
			 ViewBag.controllerIconClass = "fa fa-user-friends";
            if(Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("RELATIVE_TYPE" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;
        }


        // get all relatives, this is main view to add,edit,delete and view relatives 
        public ActionResult Index()
        {
            //check view permession for the current user 
             if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    general.NotFound();
                }

            
          //list unhiding relatives 
            return View(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true).OrderByDescending(a => a.RELATIVE_TYPE_CODE).ToList());
        }
        //add or edit new data
        public JsonResult SaveDataInDatabase(RA42_RELATIVE_TYPE_MST model)
        {

            var result = false;
            try
            {
                //if RELATIVE_TYPE_CODE > 0 that means this record needs update 
                if (model.RELATIVE_TYPE_CODE > 0)
                {
                    //check if current user has update permession 
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
                    //check if record is in the table then update data
                    RA42_RELATIVE_TYPE_MST rA42_RELATIVE_TYPE_MST = db.RA42_RELATIVE_TYPE_MST.SingleOrDefault(x => x.DLT_STS != true && x.RELATIVE_TYPE_CODE == model.RELATIVE_TYPE_CODE);
                    rA42_RELATIVE_TYPE_MST.RELATIVE_TYPE = model.RELATIVE_TYPE;
                    rA42_RELATIVE_TYPE_MST.RELATIVE_TYPE_E = model.RELATIVE_TYPE_E;
                    rA42_RELATIVE_TYPE_MST.REMARKS = model.REMARKS;
                    rA42_RELATIVE_TYPE_MST.UPD_BY = currentUser;
                    rA42_RELATIVE_TYPE_MST.UPD_DT = DateTime.Now;
                    rA42_RELATIVE_TYPE_MST.DLT_STS = false;
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
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_add"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }
                        
                    }
                    //add new record 
                    RA42_RELATIVE_TYPE_MST rA42_RELATIVE_TYPE_MST = new RA42_RELATIVE_TYPE_MST();
                    rA42_RELATIVE_TYPE_MST.RELATIVE_TYPE = model.RELATIVE_TYPE;
                    rA42_RELATIVE_TYPE_MST.RELATIVE_TYPE_E = model.RELATIVE_TYPE_E;
                    rA42_RELATIVE_TYPE_MST.REMARKS = model.REMARKS;
                    rA42_RELATIVE_TYPE_MST.UPD_BY = currentUser;
                    rA42_RELATIVE_TYPE_MST.UPD_DT = DateTime.Now;
                    rA42_RELATIVE_TYPE_MST.CRD_BY = currentUser;
                    rA42_RELATIVE_TYPE_MST.CRD_DT = DateTime.Now;
                    rA42_RELATIVE_TYPE_MST.DLT_STS = false;
                    db.RA42_RELATIVE_TYPE_MST.Add(rA42_RELATIVE_TYPE_MST);
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
        //get specific record data as json result, we will use the method as details view and delete view also
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var value = db.RA42_RELATIVE_TYPE_MST.Where(a => a.RELATIVE_TYPE_CODE == Id).FirstOrDefault();
            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        //delete or hiding in other word method 
        public JsonResult DltRecordById(int Id)
        {
            //check if current user has deleting permession 
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
            //check if record in the table then hide record 
            RA42_RELATIVE_TYPE_MST rA42_RELATIVE_TYPE_MST = db.RA42_RELATIVE_TYPE_MST.Where(x => x.RELATIVE_TYPE_CODE == Id).FirstOrDefault();
            if (rA42_RELATIVE_TYPE_MST != null)
            {
                rA42_RELATIVE_TYPE_MST.UPD_BY = currentUser;
                rA42_RELATIVE_TYPE_MST.UPD_DT = DateTime.Now;
                //hide record by set DLT_STS to true 
                rA42_RELATIVE_TYPE_MST.DLT_STS = true;
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
