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
    //this controller responsible for permits types, (مؤقت، ثابت)
	public class PasstypemstController : Controller
	{
        //get db connection 
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class 
        private GeneralFunctions general = new GeneralFunctions();
        //get title from resources
        private string title = Resources.Settings.ResourceManager.GetString("pass_type_a" + "_" + "ar");
        public PasstypemstController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Passtypemst";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("pass_type_a" + "_" + "en");
            }
            //set fontawsome icon for the controller 
			ViewBag.controllerIconClass = "fa fa-id-badge";
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;



        }


		// this is main view for add, retrive , edit and delete peass type 
		public ActionResult Index()
		{
            
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    general.NotFound();

                }
            
         //get all passes types 
            return View(db.RA42_PASS_TYPE_MST.Where(a=>a.DLT_STS !=true).OrderByDescending(a=>a.PASS_TYPE_CODE).ToList());
		}

        //save 7 edit new data
        public JsonResult SaveDataInDatabase(RA42_PASS_TYPE_MST model)
        {

            var result = false;
            try
            {
                //check if PASS_TYPE_CODE > 0, that means this record needs update 
                if (model.PASS_TYPE_CODE > 0)
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
                    //check if id is in the tabl, then update record
                    RA42_PASS_TYPE_MST rA42_PASS_TYPE_MST = db.RA42_PASS_TYPE_MST.SingleOrDefault(x => x.DLT_STS != true && x.PASS_TYPE_CODE == model.PASS_TYPE_CODE);
                    rA42_PASS_TYPE_MST.PASS_TYPE = model.PASS_TYPE;
                    rA42_PASS_TYPE_MST.PASS_TYPE_E = model.PASS_TYPE_E;
                    rA42_PASS_TYPE_MST.REMARKS = model.REMARKS;
                    rA42_PASS_TYPE_MST.UPD_BY = currentUser;
                    rA42_PASS_TYPE_MST.UPD_DT = DateTime.Now;
                    rA42_PASS_TYPE_MST.DLT_STS = false;
                    db.SaveChanges();
                    result = true;
                    AddToast(new Toast("",
                   GetResourcesValue("success_update_message"),
                   "green"));
                }
                else
                {
                    //check if user has add permession
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
                    RA42_PASS_TYPE_MST rA42_PASS_TYPE_MST = new RA42_PASS_TYPE_MST();
                    rA42_PASS_TYPE_MST.PASS_TYPE = model.PASS_TYPE;
                    rA42_PASS_TYPE_MST.PASS_TYPE_E = model.PASS_TYPE_E;
                    rA42_PASS_TYPE_MST.REMARKS = model.REMARKS;
                    rA42_PASS_TYPE_MST.UPD_BY = currentUser;
                    rA42_PASS_TYPE_MST.UPD_DT = DateTime.Now;
                    rA42_PASS_TYPE_MST.CRD_BY = currentUser;
                    rA42_PASS_TYPE_MST.CRD_DT = DateTime.Now;
                    rA42_PASS_TYPE_MST.DLT_STS = false;
                    db.RA42_PASS_TYPE_MST.Add(rA42_PASS_TYPE_MST);
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

        //get pass type details as json result 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var value = db.RA42_PASS_TYPE_MST.Where(a => a.PASS_TYPE_CODE == Id).FirstOrDefault();
            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        //delete record
        public JsonResult DltRecordById(int Id)
        {
            //check if current user has dleting permession
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
            
            RA42_PASS_TYPE_MST rA42_PASS_TYPE_MST = db.RA42_PASS_TYPE_MST.Where(x => x.PASS_TYPE_CODE == Id).FirstOrDefault();
            if (rA42_PASS_TYPE_MST != null)
            {
                rA42_PASS_TYPE_MST.UPD_BY = currentUser;
                rA42_PASS_TYPE_MST.UPD_DT = DateTime.Now;
                //hide record by set DLT_STS = true 
                rA42_PASS_TYPE_MST.DLT_STS = true;
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
