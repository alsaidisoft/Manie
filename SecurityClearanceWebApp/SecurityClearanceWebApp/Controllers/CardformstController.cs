using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Antlr.Runtime;
using APP.Filters;
using APP.Util;
using Newtonsoft.Json;
using portal.Controllers;
using SecurityClearanceWebApp.Models;
using SecurityClearanceWebApp.Util;
using Swashbuckle.Swagger;

namespace SecurityClearanceWebApp.Controllers
{
  
    [UserInfoFilter]
    //this controller to detrmine documents types for any permit type 
    //this controller is for developer only
    public class CardformstController : Controller
    {
        //get db connection
        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class 
        private GeneralFunctions general = new GeneralFunctions();
        private string title = Resources.Settings.ResourceManager.GetString("card_for" + "_" + "ar");

        public CardformstController()
        {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Cardformst";
            //set fontawsome icon for this controller views 
            ViewBag.controllerIconClass = "fa fa-id-card";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("card_for" + "_" + "en");
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
            if (Language.GetCurrentLang() == "en")
            {
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST, "ACCESS_TYPE_CODE", "ACCESS_TYPE_E");
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true), "FORCE_CODE", "FORCE_NAME_E");
            }
            else
            {
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST, "ACCESS_TYPE_CODE", "ACCESS_TYPE");
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true), "FORCE_CODE", "FORCE_NAME_A");
            }

            return View(db.RA42_CARD_FOR_MST.Where(a => a.DLT_STS != true).OrderByDescending(a=>a.CARD_FOR_CODE).ToList());
        }
        //add or edit data
        public JsonResult SaveDataInDatabase(RA42_CARD_FOR_MST model)
        {

            var result = false;
            
            try
            {
                //if model FILE_TYPE_CODE > 0 that means this record need update 
                if (model.CARD_FOR_CODE > 0)
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

                    //check if record is in RA42_CARD_FOR_MST table 
                    RA42_CARD_FOR_MST rA42_CARD_FOR_MST = db.RA42_CARD_FOR_MST.SingleOrDefault(x => x.DLT_STS != true && x.CARD_FOR_CODE == model.CARD_FOR_CODE);
                    //edit RA42_CARD_FOR_MST details
                    rA42_CARD_FOR_MST.CARD_FOR_A = model.CARD_FOR_A;
                    rA42_CARD_FOR_MST.CARD_FOR_E = model.CARD_FOR_E;
                    rA42_CARD_FOR_MST.CARD_SECRET_CODE = model.CARD_SECRET_CODE;
                    rA42_CARD_FOR_MST.ACCESS_TYPE_CODE = model.ACCESS_TYPE_CODE;
                    rA42_CARD_FOR_MST.FORCES_IDS = model.FORCES_IDS;
                    rA42_CARD_FOR_MST.WITH_CAR = model.WITH_CAR;
                    rA42_CARD_FOR_MST.REMARKS = model.REMARKS;
                    rA42_CARD_FOR_MST.UPD_BY = currentUser;
                    rA42_CARD_FOR_MST.UPD_DT = DateTime.Now;
                    rA42_CARD_FOR_MST.DLT_STS = false;
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

                    List<int> list = model.FORCE_CODE.ToList();
                    var forcesJson = JsonConvert.SerializeObject(list);
                    //now add RA42_CARD_FOR_MST type details
                    RA42_CARD_FOR_MST rA42_CARD_FOR_MST = new RA42_CARD_FOR_MST();
                    rA42_CARD_FOR_MST.CARD_FOR_A = model.CARD_FOR_A;
                    rA42_CARD_FOR_MST.CARD_FOR_E = model.CARD_FOR_E;
                    rA42_CARD_FOR_MST.CARD_SECRET_CODE = model.CARD_SECRET_CODE;
                    rA42_CARD_FOR_MST.ACCESS_TYPE_CODE = model.ACCESS_TYPE_CODE;
                    rA42_CARD_FOR_MST.FORCES_IDS = forcesJson;
                    rA42_CARD_FOR_MST.WITH_CAR = model.WITH_CAR;
                    rA42_CARD_FOR_MST.REMARKS = model.REMARKS;
                    rA42_CARD_FOR_MST.UPD_BY = currentUser;
                    rA42_CARD_FOR_MST.UPD_DT = DateTime.Now;
                    rA42_CARD_FOR_MST.CRD_BY = currentUser;
                    rA42_CARD_FOR_MST.CRD_DT = DateTime.Now;
                    rA42_CARD_FOR_MST.DLT_STS = false;
                    db.RA42_CARD_FOR_MST.Add(rA42_CARD_FOR_MST);
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

       
        //get forces
        
        //get RA42_CARD_FOR_MST type details as json result 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var value = (from a in db.RA42_CARD_FOR_MST
                         where a.CARD_FOR_CODE == Id
                         select new
                         {

                             a.CARD_FOR_CODE,
                             a.CARD_FOR_A,
                             a.CARD_FOR_E,
                             a.ACCESS_TYPE_CODE,
                             a.RA42_ACCESS_TYPE_MST.ACCESS_TYPE,
                             a.RA42_ACCESS_TYPE_MST.ACCESS_TYPE_E,
                             a.FORCES_IDS,
                             a.CARD_SECRET_CODE,
                             a.REMARKS,
                             a.WITH_CAR,
                             a.CRD_BY,
                             a.CRD_DT,
                             a.UPD_BY,
                             a.UPD_DT


                         }).FirstOrDefault();


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
            RA42_CARD_FOR_MST rA42_CARD_FOR_MST = db.RA42_CARD_FOR_MST.Where(x => x.CARD_FOR_CODE == Id).FirstOrDefault();
            if (rA42_CARD_FOR_MST != null)
            {
                rA42_CARD_FOR_MST.UPD_BY = currentUser;
                rA42_CARD_FOR_MST.UPD_DT = DateTime.Now;
                //we don't delete record actully we hide record by set DLT_STS to true
                rA42_CARD_FOR_MST.DLT_STS = true;
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
