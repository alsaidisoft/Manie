using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
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

//this controller for controlling announcments

namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    public class AnnouncementmstController : Controller
    {
        //get db connection
        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //get GeneralFunctions class object
        private GeneralFunctions general = new GeneralFunctions();
        //set title for the controller from the resources
        private string title = Resources.Settings.ResourceManager.GetString("Announce" + "_" + "ar");

        public AnnouncementmstController()
        {
            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Announcementmst";
            ViewBag.Settings = "Settings";
            //icon of controller, this icon will showen in view pages
            ViewBag.controllerIconClass = "fa fa-bullhorn";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("Announce" + "_" + "en");
            }
            //title of controller
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;


        }


        // view all announcments
        public ActionResult Index()
        {
            //check view permession
            if (ViewBag.VW != true)
            {
                //check administrator or developer permessions
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    general.NotFound();

                }
            }
           
            //view all announcement which are not deleted order by DESC
            return View(db.RA42_ANNOUNCEMENT_MST.Where(a => a.DLT_STS != true).OrderByDescending(a => a.ANNOUNCE_CODE).ToList());
        }
        //this method is for adding or updating record and rerive json result
        public JsonResult SaveDataInDatabase(RA42_ANNOUNCEMENT_MST model)
        {

            var result = false;
            try
            {
                // this is for updating new record if model primary key is grater than 0 
                if (model.ANNOUNCE_CODE > 0)
                {
                    //check update permession
                    if (ViewBag.UP != true)
                    {
                        //check administrator or developer permessions
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_up"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }

                    }
                    //check id if is in table 
                    RA42_ANNOUNCEMENT_MST rA42_ANNOUNCEMENT_MST = db.RA42_ANNOUNCEMENT_MST.SingleOrDefault(x => x.DLT_STS != true && x.ANNOUNCE_CODE == model.ANNOUNCE_CODE);
                    rA42_ANNOUNCEMENT_MST.ANNOUNCEMENT_A = model.ANNOUNCEMENT_A;
                    rA42_ANNOUNCEMENT_MST.ANNOUNCEMENT_E = model.ANNOUNCEMENT_E;
                    rA42_ANNOUNCEMENT_MST.URL = model.URL;
                    rA42_ANNOUNCEMENT_MST.START_DATE = model.START_DATE;
                    rA42_ANNOUNCEMENT_MST.END_DATE = model.END_DATE;
                    rA42_ANNOUNCEMENT_MST.UPD_BY = currentUser;
                    rA42_ANNOUNCEMENT_MST.UPD_DT = DateTime.Now;
                    rA42_ANNOUNCEMENT_MST.DLT_STS = false;
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
                        //check administrator or developer permessions
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_add"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }

                    }
                    //this is for inserting new record if model primary key is = 0
                    RA42_ANNOUNCEMENT_MST rA42_ANNOUNCEMENT_MST = new RA42_ANNOUNCEMENT_MST();
                    rA42_ANNOUNCEMENT_MST.ANNOUNCEMENT_A = model.ANNOUNCEMENT_A;
                    rA42_ANNOUNCEMENT_MST.ANNOUNCEMENT_E = model.ANNOUNCEMENT_E;
                    rA42_ANNOUNCEMENT_MST.URL = model.URL;
                    rA42_ANNOUNCEMENT_MST.START_DATE = model.START_DATE;
                    rA42_ANNOUNCEMENT_MST.END_DATE = model.END_DATE;
                    rA42_ANNOUNCEMENT_MST.UPD_BY = currentUser;
                    rA42_ANNOUNCEMENT_MST.UPD_DT = DateTime.Now;
                    rA42_ANNOUNCEMENT_MST.CRD_BY = currentUser;
                    rA42_ANNOUNCEMENT_MST.CRD_DT = DateTime.Now;
                    rA42_ANNOUNCEMENT_MST.DLT_STS = false;
                    db.RA42_ANNOUNCEMENT_MST.Add(rA42_ANNOUNCEMENT_MST);
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

        //get data by json array, this data will showen in edit, delete and details views
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var value = db.RA42_ANNOUNCEMENT_MST.Where(a => a.ANNOUNCE_CODE == Id).FirstOrDefault();
            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        //deleting or hiding record 
        public JsonResult DltRecordById(int Id)
        {
            //check deleting permession
            if (ViewBag.DLT != true)
            {
                //check administrator or developer permessions
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    AddToast(new Toast("",
                  GetResourcesValue("Dont_have_permissions_to_dlt"),
                  "red"));
                    return Json(false, JsonRequestBehavior.AllowGet);
                }

            }
            bool result = false;
            RA42_ANNOUNCEMENT_MST rA42_ANNOUNCEMENT_MST = db.RA42_ANNOUNCEMENT_MST.Where(x => x.ANNOUNCE_CODE == Id).FirstOrDefault();
            if (rA42_ANNOUNCEMENT_MST != null)
            {
                rA42_ANNOUNCEMENT_MST.UPD_BY = currentUser;
                rA42_ANNOUNCEMENT_MST.UPD_DT = DateTime.Now;
                //hide record by setting DLT_STS = true
                rA42_ANNOUNCEMENT_MST.DLT_STS = true;
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