using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using APP.Filters;
using APP.Util;
using Newtonsoft.Json;
using portal.Controllers;
using SecurityClearanceWebApp.Models;
using SecurityClearanceWebApp.Util;

namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    //this controller is for vehcil names, but we chang this controller to set vehcil types such as (صالون - دفع رباعي - شاحنة)
    //because there are many many vehcil names and we cant insert all of the one time 
	public class VechilenamemstController : Controller
	{
        //get db connection 
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class to use some of its functions
        private GeneralFunctions general = new GeneralFunctions();
        //set title of the controller from resources 
        private string title = Resources.Settings.ResourceManager.GetString("vehicle_name_a" + "_" + "ar");
        public VechilenamemstController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Vechilenamemst";
            //set icon for the controller from fontawsome library
			ViewBag.controllerIconClass = "fa fa-car-side";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("vehicle_name_a" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;

        }


		// this is main view for add, edit, delete and retrive data
		public ActionResult Index()
		{
            //check view permession for the current user
            
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER !=true)
                {
                    general.NotFound();
                }

            
            if (ViewBag.DEVELOPER == true)
            {


                List<RA42_VECHILE_CATIGORY_MST> catList = db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true).ToList();
                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.VECHILE_CATIGORIES = new SelectList(catList, "VECHILE_CODE", "VECHILE_CAT_E");

                }
                else
                {
                    ViewBag.VECHILE_CATIGORIES = new SelectList(catList, "VECHILE_CODE", "VECHILE_CAT");

                }
                //show all unhidden vechiles types 
                var rA42_VECHILE_NAME_MST = db.RA42_VECHILE_NAME_MST.Include(r => r.RA42_VECHILE_CATIGORY_MST);
                return View(rA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true).OrderByDescending(a => a.VECHILE_NAME_CODE).ToList());

            }
            else
            {
                int force = Convert.ToInt32(ViewBag.FORCE_TYPE_CODE);
                //get vechils catigories 
                List<RA42_VECHILE_CATIGORY_MST> catList = db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true).ToList();
                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.VECHILE_CATIGORIES = new SelectList(catList, "VECHILE_CODE", "VECHILE_CAT_E");

                }
                else
                {
                    ViewBag.VECHILE_CATIGORIES = new SelectList(catList, "VECHILE_CODE", "VECHILE_CAT");

                }
                //show all unhidden vechiles types 
                var rA42_VECHILE_NAME_MST = db.RA42_VECHILE_NAME_MST.Include(r => r.RA42_VECHILE_CATIGORY_MST);
                return View(rA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true).OrderByDescending(a => a.VECHILE_NAME_CODE).ToList());
            }
		}

        //add and edit new data
        public JsonResult SaveDataInDatabase(RA42_VECHILE_NAME_MST model)
        {

            var result = false;
            try
            {
                //if VECHILE_NAME_CODE is > 0 this means this record needs update 
                if (model.VECHILE_NAME_CODE > 0)
                {
                    //check if the current user has update permession 
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
                    //check if the record id is in the db table then update data
                    RA42_VECHILE_NAME_MST rA42_VECHILE_NAME_MST = db.RA42_VECHILE_NAME_MST.SingleOrDefault(x => x.DLT_STS != true && x.VECHILE_NAME_CODE == model.VECHILE_NAME_CODE);
                    rA42_VECHILE_NAME_MST.VECHILE_NAME = model.VECHILE_NAME;
                    rA42_VECHILE_NAME_MST.VECHILE_NAME_E = model.VECHILE_NAME_E;
                    rA42_VECHILE_NAME_MST.VECHILE_CODE = model.VECHILE_CODE;
                    rA42_VECHILE_NAME_MST.REMARKS = model.REMARKS;
                    rA42_VECHILE_NAME_MST.UPD_BY = currentUser;
                    rA42_VECHILE_NAME_MST.UPD_DT = DateTime.Now;
                    rA42_VECHILE_NAME_MST.DLT_STS = false;
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
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_add"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }
                        
                    }
                    //add new vechile type
                    RA42_VECHILE_NAME_MST rA42_VECHILE_NAME_MST = new RA42_VECHILE_NAME_MST();
                    rA42_VECHILE_NAME_MST.VECHILE_NAME = model.VECHILE_NAME;
                    rA42_VECHILE_NAME_MST.VECHILE_NAME_E = model.VECHILE_NAME_E;
                    rA42_VECHILE_NAME_MST.VECHILE_CODE = model.VECHILE_CODE;
                    rA42_VECHILE_NAME_MST.REMARKS = model.REMARKS;
                    rA42_VECHILE_NAME_MST.CRD_BY = currentUser;
                    rA42_VECHILE_NAME_MST.CRD_DT = DateTime.Now;
                    rA42_VECHILE_NAME_MST.UPD_BY = currentUser;
                    rA42_VECHILE_NAME_MST.UPD_DT = DateTime.Now;
                    rA42_VECHILE_NAME_MST.DLT_STS = false;
                    db.RA42_VECHILE_NAME_MST.Add(rA42_VECHILE_NAME_MST);
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
        //get specific record data
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            var value = (from a in db.RA42_VECHILE_NAME_MST
                          join b in db.RA42_VECHILE_CATIGORY_MST on a.VECHILE_CODE equals b.VECHILE_CODE where a.VECHILE_NAME_CODE == Id
                           select new
                          {

                              a.VECHILE_NAME_CODE,
                              a.VECHILE_NAME,
                              a.VECHILE_NAME_E,
                              a.REMARKS,
                              b.VECHILE_CODE,
                              b.VECHILE_CAT,
                              b.VECHILE_CAT_E,
                              a.CRD_BY,
                              a.CRD_DT,
                              a.UPD_BY,
                              a.UPD_DT
                             

                          }).FirstOrDefault();

            //return new JsonResult() { Data = value, JsonRequestBehavior = JsonRequestBehavior.AllowGet, MaxJsonLength = Int32.MaxValue };
            return Json(value, JsonRequestBehavior.AllowGet);

        }

        //hide specifc data record 
        public JsonResult DltRecordById(int Id)
        {
            //check delete permession for the current user 
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
            RA42_VECHILE_NAME_MST rA42_VECHILE_NAME_MST = db.RA42_VECHILE_NAME_MST.Where(x => x.VECHILE_NAME_CODE == Id).FirstOrDefault();
            if (rA42_VECHILE_NAME_MST != null)
            {
                rA42_VECHILE_NAME_MST.UPD_BY = currentUser;
                rA42_VECHILE_NAME_MST.UPD_DT = DateTime.Now;
                //hide record by set DLT_STS to true 
                rA42_VECHILE_NAME_MST.DLT_STS = true;
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
