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
    //this controller is responsible for plate type, private, commercial or goverment etc...
	public class PlatetypemstController : Controller
	{
        //get db connection
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunction class to use some of its functions
        private GeneralFunctions general = new GeneralFunctions();
        //identify controller title from resources 
        private string title = Resources.Settings.ResourceManager.GetString("plate_type_a" + "_" + "ar");
        public PlatetypemstController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Platetypemst";
            //set fontawsome icon for this controller
			ViewBag.controllerIconClass = "fa fa-ruler-horizontal";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("plate_type_a" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;



        }


		// GET all plate types, this is main view to add, edit, delete and retrive data
		public ActionResult Index()
		{
            //check view permession for the current user 
          
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    general.NotFound();
                }

            
          //list all unhiding plate type 
            return View(db.RA42_PLATE_TYPE_MST.Where(a=>a.DLT_STS !=true).OrderByDescending(a=>a.PLATE_CODE).ToList());
		}
        //save or edit new data
        public JsonResult SaveDataInDatabase(RA42_PLATE_TYPE_MST model)
        {

            var result = false;
            try
            {
                //if PLATE_CODE is > 0 that means this record need update 
                if (model.PLATE_CODE > 0)
                {
                    //check update permession for the current user 
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
                    //check if record in the table then update record 
                    RA42_PLATE_TYPE_MST rA42_PLATE_TYPE_MST = db.RA42_PLATE_TYPE_MST.SingleOrDefault(x => x.DLT_STS != true && x.PLATE_CODE == model.PLATE_CODE);
                    rA42_PLATE_TYPE_MST.PLATE_TYPE = model.PLATE_TYPE;
                    rA42_PLATE_TYPE_MST.PLATE_TYPE_E = model.PLATE_TYPE_E;
                    rA42_PLATE_TYPE_MST.REMARKS = model.REMARKS;
                    rA42_PLATE_TYPE_MST.UPD_BY = currentUser;
                    rA42_PLATE_TYPE_MST.UPD_DT = DateTime.Now;
                    rA42_PLATE_TYPE_MST.DLT_STS = false;
                    db.SaveChanges();
                    result = true;
                    AddToast(new Toast("",
                   GetResourcesValue("success_update_message"),
                   "green"));
                }
                else
                {
                    //check add permession for the current user 
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
                    RA42_PLATE_TYPE_MST rA42_PLATE_TYPE_MST = new RA42_PLATE_TYPE_MST();
                    rA42_PLATE_TYPE_MST.PLATE_TYPE = model.PLATE_TYPE;
                    rA42_PLATE_TYPE_MST.PLATE_TYPE_E = model.PLATE_TYPE_E;
                    rA42_PLATE_TYPE_MST.REMARKS = model.REMARKS;
                    rA42_PLATE_TYPE_MST.UPD_BY = currentUser;
                    rA42_PLATE_TYPE_MST.UPD_DT = DateTime.Now;
                    rA42_PLATE_TYPE_MST.CRD_BY = currentUser;
                    rA42_PLATE_TYPE_MST.CRD_DT = DateTime.Now;
                    rA42_PLATE_TYPE_MST.DLT_STS = false;
                    db.RA42_PLATE_TYPE_MST.Add(rA42_PLATE_TYPE_MST);
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

        //get specific record data as json result 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var value = db.RA42_PLATE_TYPE_MST.Where(a => a.PLATE_CODE == Id).FirstOrDefault();
            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        //delete or hide record 
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
            //check if record in the table then hide record 
            RA42_PLATE_TYPE_MST rA42_PLATE_TYPE_MST = db.RA42_PLATE_TYPE_MST.Where(x => x.PLATE_CODE == Id).FirstOrDefault();
            if (rA42_PLATE_TYPE_MST != null)
            {
                rA42_PLATE_TYPE_MST.UPD_BY = currentUser;
                rA42_PLATE_TYPE_MST.UPD_DT = DateTime.Now;
                //hide record by setting DLT_STS to true 
                rA42_PLATE_TYPE_MST.DLT_STS = true;
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
