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


//this controller is responsible about controlling access types and this section is developer responsiblities
//the forth method CRDU by json
namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    public class AccesstypemstController : Controller
    {
        //get database connection
        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get service number via current user
        private string currentUser = (new UserInfo()).getSNO();
        private string title = Resources.Settings.ResourceManager.GetString("ACCESS_TYPE" + "_" + "ar");
        private GeneralFunctions general = new GeneralFunctions();

        public AccesstypemstController()
        {
            ViewBag.Settings = "Settings";
            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Accesstypemst";
            if(Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("ACCESS_TYPE" + "_" + "en");
            }
            //icon of the controller, whill showen in view pages
            ViewBag.controllerIconClass = "fa fa-universal-access";
            //title of controller
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;
            
        }

        
        // View all access 
        public ActionResult Index()
        {
            if (ViewBag.DEVELOPER != true)
            {
                general.NotFound();

            }


            return View(db.RA42_ACCESS_TYPE_MST.OrderByDescending(a=>a.ACCESS_TYPE_CODE).ToList());
        }

        //this method is for save new record or updat exciting record
        public JsonResult SaveDataInDatabase(RA42_ACCESS_TYPE_MST model)
        {

            var result = false;
            try
            {
                // if model primay key is more than 0 that means this record is already in the database and we will use update method 
                if (model.ACCESS_TYPE_CODE > 0)
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
                    //update record if evry thing is good
                    RA42_ACCESS_TYPE_MST rA42_ACCESS_TYPE_MST = db.RA42_ACCESS_TYPE_MST.SingleOrDefault(x => x.DLT_STS != true && x.ACCESS_TYPE_CODE == model.ACCESS_TYPE_CODE);
                    rA42_ACCESS_TYPE_MST.ACCESS_TYPE = model.ACCESS_TYPE;
                    rA42_ACCESS_TYPE_MST.ACCESS_TYPE_E = model.ACCESS_TYPE_E;
                    rA42_ACCESS_TYPE_MST.REMARKS = model.REMARKS;
                    rA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE = model.ACCESS_SECRET_CODE;
                    rA42_ACCESS_TYPE_MST.ICON = model.ICON;
                    rA42_ACCESS_TYPE_MST.UPD_BY = currentUser;
                    rA42_ACCESS_TYPE_MST.UPD_DT = DateTime.Now;
                    rA42_ACCESS_TYPE_MST.DLT_STS = false;
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
                    // insert new access type 
                    RA42_ACCESS_TYPE_MST rA42_ACCESS_TYPE_MST = new RA42_ACCESS_TYPE_MST();
                    rA42_ACCESS_TYPE_MST.ACCESS_TYPE = model.ACCESS_TYPE;
                    rA42_ACCESS_TYPE_MST.ACCESS_TYPE_E = model.ACCESS_TYPE_E;
                    rA42_ACCESS_TYPE_MST.REMARKS = model.REMARKS;
                    rA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE = model.ACCESS_SECRET_CODE;
                    rA42_ACCESS_TYPE_MST.ICON = model.ICON;
                    rA42_ACCESS_TYPE_MST.UPD_BY = currentUser;
                    rA42_ACCESS_TYPE_MST.UPD_DT = DateTime.Now;
                    rA42_ACCESS_TYPE_MST.CRD_BY = currentUser;
                    rA42_ACCESS_TYPE_MST.CRD_DT = DateTime.Now;
                    rA42_ACCESS_TYPE_MST.DLT_STS = false;
                    db.RA42_ACCESS_TYPE_MST.Add(rA42_ACCESS_TYPE_MST);
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

        // get data by json, this data will showen in edit, delete, details
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var value = db.RA42_ACCESS_TYPE_MST.Where(a => a.ACCESS_TYPE_CODE == Id).FirstOrDefault();
            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(value, JsonRequestBehavior.AllowGet);
        }
        // this method for deleting record or hiding record
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
            RA42_ACCESS_TYPE_MST rA42_ACCESS_TYPE_MST = db.RA42_ACCESS_TYPE_MST.Where(x => x.ACCESS_TYPE_CODE == Id).FirstOrDefault();
            if (rA42_ACCESS_TYPE_MST != null)
            {
                rA42_ACCESS_TYPE_MST.UPD_BY = currentUser;
                rA42_ACCESS_TYPE_MST.UPD_DT = DateTime.Now;
                //set DLT_STS true to hide record
                rA42_ACCESS_TYPE_MST.DLT_STS = true;
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
