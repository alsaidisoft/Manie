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
    //this controller to detrmine documents types for any permit type 
    //this controller is for developer only
    public class ForcesmstController : Controller
    {
        //get db connection
        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class 
        private GeneralFunctions general = new GeneralFunctions();
        private string title = Resources.Settings.ResourceManager.GetString("Forces" + "_" + "ar");

        public ForcesmstController()
        {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Forcesmst";
            //set fontawsome icon for this controller views 
            ViewBag.controllerIconClass = "fa fa-jedi";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("Forces" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;
            
        }


        //index view will has all operations, add - edit - delete - retrive
        public ActionResult Index()
        {
            if (ViewBag.DEVELOPER != true)
            {
                general.NotFound();

            }



            return View(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true).OrderByDescending(a=>a.FORCE_ID).ToList());
        }
        //add or edit data
        public JsonResult SaveDataInDatabase(RA42_FORCES_MST model)
        {

            var result = false;
            try
            {
                //if model FORCE_ID > 0 that means this record need update 
                if (model.FORCE_ID > 0)
                {
                    //check update permession
                    if (ViewBag.UP != true)
                    {
                        if (ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                         GetResourcesValue("Dont_have_permissions_to_up"),
                         "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }
                    }
                    //check if record is in RA42_FORCES_MST table 
                    RA42_FORCES_MST rA42_FORCES_MST = db.RA42_FORCES_MST.SingleOrDefault(x => x.DLT_STS != true && x.FORCE_ID == model.FORCE_ID);
                    //edit force details
                    rA42_FORCES_MST.FORCE_CODE = model.FORCE_CODE;
                    rA42_FORCES_MST.FORCE_NAME_A = model.FORCE_NAME_A;
                    rA42_FORCES_MST.FORCE_NAME_E = model.FORCE_NAME_E;
                    rA42_FORCES_MST.DIRECTORATE_A = model.DIRECTORATE_A;
                    rA42_FORCES_MST.DIRECTORATE_E = model.DIRECTORATE_E;
                    rA42_FORCES_MST.LOGO = model.LOGO;
                    rA42_FORCES_MST.UPD_BY = currentUser;
                    rA42_FORCES_MST.UPD_DT = DateTime.Now;
                    rA42_FORCES_MST.DLT_STS = false;
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
                        if (ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                         GetResourcesValue("Dont_have_permissions_to_add"),
                         "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }
                       
                    }
                    //now add new force type details
                   RA42_FORCES_MST rA42_FORCES_MST = new RA42_FORCES_MST();
                    rA42_FORCES_MST.FORCE_CODE = model.FORCE_CODE;
                    rA42_FORCES_MST.FORCE_NAME_A = model.FORCE_NAME_A;
                    rA42_FORCES_MST.FORCE_NAME_E = model.FORCE_NAME_E;
                    rA42_FORCES_MST.DIRECTORATE_A = model.DIRECTORATE_A;
                    rA42_FORCES_MST.DIRECTORATE_E = model.DIRECTORATE_E;
                    rA42_FORCES_MST.UPD_BY = currentUser;
                    rA42_FORCES_MST.UPD_DT = DateTime.Now;
                    rA42_FORCES_MST.CRD_BY = currentUser;
                    rA42_FORCES_MST.CRD_DT = DateTime.Now;
                    rA42_FORCES_MST.DLT_STS = false;
                    db.RA42_FORCES_MST.Add(rA42_FORCES_MST);
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
        //get document type details as json result 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var value = db.RA42_FORCES_MST.Where(a => a.FORCE_ID == Id).FirstOrDefault();
            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(value, JsonRequestBehavior.AllowGet);
        }

       //delete any record 
        public JsonResult DltRecordById(int Id)
        {
            //check if current user has authority to delete any record 
            if (ViewBag.DLT != true)
            {
                if (ViewBag.DEVELOPER != true)
                {
                    AddToast(new Toast("",
                  GetResourcesValue("Dont_have_permissions_to_dlt"),
                  "red"));
                    return Json(false, JsonRequestBehavior.AllowGet);
                }
                
            }
            bool result = false;
            RA42_FORCES_MST rA42_FORCES_MST = db.RA42_FORCES_MST.Where(x => x.FORCE_ID == Id).FirstOrDefault();
            if (rA42_FORCES_MST != null)
            {
                rA42_FORCES_MST.UPD_BY = currentUser;
                rA42_FORCES_MST.UPD_DT = DateTime.Now;
                //we don't delete record actully we hide record by set DLT_STS to true
                rA42_FORCES_MST.DLT_STS = true;
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
