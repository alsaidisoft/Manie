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

//create companies type controller, tgis controller for developer 
namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    public class CompanytypemstController : Controller
    {
        //create db connection
        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user
        private string currentUser = (new UserInfo()).getSNO();
        private GeneralFunctions general = new GeneralFunctions();

        private string title = Resources.Settings.ResourceManager.GetString("company_type_a" + "_" + "ar");
        public CompanytypemstController()
        {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Companytypemst";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("company_type_a" + "_" + "en");
            }
            ViewBag.controllerIconClass = "fa fa-warehouse";
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;



        }


        // show all types 
        public ActionResult Index()
        {
            //check view permession
            if (ViewBag.DEVELOPER != true)
            {
                general.NotFound();

            }


            return View(db.RA42_COMPANY_TYPE_MST.Where(a => a.DLT_STS != true).OrderByDescending(a => a.COMPANY_TYPE_CODE).ToList());
        }

        //save data in db 
        public JsonResult SaveDataInDatabase(RA42_COMPANY_TYPE_MST model)
        {

            var result = false;
            try
            {
                // if model.COMPANY_TYPE_CODE > 0 that means this record need update
                if (model.COMPANY_TYPE_CODE > 0)
                {
                    //check update permession
                    if (ViewBag.UP != true)
                    {
                        //check developer permession
                        if (ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_up"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }

                    }
                    RA42_COMPANY_TYPE_MST rA42_COMPANY_TYPE_MST = db.RA42_COMPANY_TYPE_MST.SingleOrDefault(x => x.DLT_STS != true && x.COMPANY_TYPE_CODE == model.COMPANY_TYPE_CODE);
                    rA42_COMPANY_TYPE_MST.COMPANY_TYPE = model.COMPANY_TYPE;
                    rA42_COMPANY_TYPE_MST.COMPANY_TYPE_E = model.COMPANY_TYPE_E;
                    rA42_COMPANY_TYPE_MST.REMARKS = model.REMARKS;
                    rA42_COMPANY_TYPE_MST.UPD_BY = currentUser;
                    rA42_COMPANY_TYPE_MST.UPD_DT = DateTime.Now;
                    rA42_COMPANY_TYPE_MST.DLT_STS = false;
                    db.SaveChanges();
                    result = true;
                    AddToast(new Toast("",
                   GetResourcesValue("success_update_message"),
                   "green"));
                }
                else
                {
                    //check add permession
                    if (ViewBag.AD != true)
                    {
                        //check developer permession
                        if (ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_add"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }

                    }
                    //add new type
                    RA42_COMPANY_TYPE_MST rA42_COMPANY_TYPE_MST = new RA42_COMPANY_TYPE_MST();
                    rA42_COMPANY_TYPE_MST.COMPANY_TYPE = model.COMPANY_TYPE;
                    rA42_COMPANY_TYPE_MST.COMPANY_TYPE_E = model.COMPANY_TYPE_E;
                    rA42_COMPANY_TYPE_MST.REMARKS = model.REMARKS;
                    rA42_COMPANY_TYPE_MST.UPD_BY = currentUser;
                    rA42_COMPANY_TYPE_MST.UPD_DT = DateTime.Now;
                    rA42_COMPANY_TYPE_MST.CRD_BY = currentUser;
                    rA42_COMPANY_TYPE_MST.CRD_DT = DateTime.Now;
                    rA42_COMPANY_TYPE_MST.DLT_STS = false;
                    db.RA42_COMPANY_TYPE_MST.Add(rA42_COMPANY_TYPE_MST);
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

        //get specific type details
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var value = db.RA42_COMPANY_TYPE_MST.Where(a => a.COMPANY_TYPE_CODE == Id).FirstOrDefault();
            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(value, JsonRequestBehavior.AllowGet);
        }

       //delete record or hide record
        public JsonResult DltRecordById(int Id)
        {
            //check delete permession
            if (ViewBag.DLT != true)
            {
                //check developer permession
                if (ViewBag.DEVELOPER != true)
                {
                    AddToast(new Toast("",
                  GetResourcesValue("Dont_have_permissions_to_dlt"),
                  "red"));
                    return Json(false, JsonRequestBehavior.AllowGet);
                }

            }
            bool result = false;
            RA42_COMPANY_TYPE_MST rA42_COMPANY_TYPE_MST = db.RA42_COMPANY_TYPE_MST.Where(x => x.COMPANY_TYPE_CODE == Id).FirstOrDefault();
            if (rA42_COMPANY_TYPE_MST != null)
            {
                rA42_COMPANY_TYPE_MST.UPD_BY = currentUser;
                rA42_COMPANY_TYPE_MST.UPD_DT = DateTime.Now;
                //hide record by set DLT_STS = true
                rA42_COMPANY_TYPE_MST.DLT_STS = true;
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
