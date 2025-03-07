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
using Newtonsoft.Json;
using portal.Controllers;
using SecurityClearanceWebApp.Models;
using SecurityClearanceWebApp.Util;

namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    //this identities controller to add, edit, view, delete identity 
	public class IdentitymstController : Controller
	{
        //get db connection
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        //identify toats variable 
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify generalFunctions class
        private GeneralFunctions general = new GeneralFunctions();

        private string title = "";


        public IdentitymstController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Identitymst";
            //set fontawsome icon for the controller
            ViewBag.controllerIconClass = "fa fa-fingerprint";
            //set title of controller 
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("identity_type_a" + "_" + "en");
            }
            else
            {
                title = Resources.Settings.ResourceManager.GetString("identity_type_a" + "_" + "ar");

            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;

        }


		// this is main view to add, edit,delete andd retrive identities 
		public ActionResult Index()
		{
            //check view permession
           
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    general.NotFound();

                }
            
            //get all identities 
            return View(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true).OrderByDescending(a=>a.IDENTITY_CODE).ToList());
		}
       //save and edit new identities 
        public JsonResult SaveDataInDatabase(RA42_IDENTITY_MST model)
        {
            
            var result = false;
            try
            {
                //check if model IDENTITY_CODE > 0 that means this record need update 
                if (model.IDENTITY_CODE > 0)
                {
                    //check if user has update permession
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
                    //check if record in RA42_IDENTITY_MST table 
                    RA42_IDENTITY_MST rA42_IDENTITY = db.RA42_IDENTITY_MST.SingleOrDefault(x => x.DLT_STS != true && x.IDENTITY_CODE == model.IDENTITY_CODE);
                    rA42_IDENTITY.IDENTITY_TYPE_A = model.IDENTITY_TYPE_A;
                    rA42_IDENTITY.IDENTITY_TYPE_E = model.IDENTITY_TYPE_E;
                    rA42_IDENTITY.REMARKS = model.REMARKS;
                    rA42_IDENTITY.UPD_BY = currentUser;
                    rA42_IDENTITY.UPD_DT = DateTime.Now;
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
                    //add new identity 
                    RA42_IDENTITY_MST rA42_IDENTITY = new RA42_IDENTITY_MST();
                    rA42_IDENTITY.IDENTITY_TYPE_A = model.IDENTITY_TYPE_A;
                    rA42_IDENTITY.IDENTITY_TYPE_E = model.IDENTITY_TYPE_E;
                    rA42_IDENTITY.REMARKS = model.REMARKS;
                    rA42_IDENTITY.CRD_BY = currentUser;
                    rA42_IDENTITY.CRD_DT = DateTime.Now;
                    rA42_IDENTITY.UPD_BY = currentUser;
                    rA42_IDENTITY.UPD_DT = DateTime.Now;
                    rA42_IDENTITY.DLT_STS = false;
                    db.RA42_IDENTITY_MST.Add(rA42_IDENTITY);
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
        //get identity details as json result 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var value = db.RA42_IDENTITY_MST.Where(a => a.IDENTITY_CODE == Id).FirstOrDefault();
            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(value, JsonRequestBehavior.AllowGet);
        }

      //delete any record by json result 
        public JsonResult DltRecordById(int Id)
        {
            //check deleting permession
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
            RA42_IDENTITY_MST rA42_IDENTITY_MST = db.RA42_IDENTITY_MST.Where(x=> x.IDENTITY_CODE == Id).FirstOrDefault();
            if (rA42_IDENTITY_MST != null)
            {
                rA42_IDENTITY_MST.UPD_BY = currentUser;
                rA42_IDENTITY_MST.UPD_DT = DateTime.Now;
                //hide rcord by set DLT_STS to true 
                rA42_IDENTITY_MST.DLT_STS = true;
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
