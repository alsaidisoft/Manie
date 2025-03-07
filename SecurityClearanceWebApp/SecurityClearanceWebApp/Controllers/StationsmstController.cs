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

namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    //this controller for developer only, this is for adding new reports for the authorized users in deffirent stations 
	public class StationsmstController : Controller
	{
        //get db connection 
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunction class
        private GeneralFunctions general = new GeneralFunctions();
        //set title for the controller from resources
        private string title = Resources.Settings.ResourceManager.GetString("stations_camps" + "_" + "ar");
        public StationsmstController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Stationsmst";
            //set fontawsome icon for the controller 
			ViewBag.controllerIconClass = "fa fa-university";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("stations_camps" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;



        }


		// get all reports, this is main view to add delete, edit and retrive reports 
		public ActionResult Index()
		{
            //check view permession for current user 
            if (ViewBag.DEVELOPER != true)
            {
                general.NotFound();

            }





            //get stations 
            if (Language.GetCurrentLang() == "en")
                {
                ViewBag.FORCE_ID = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true), "FORCE_ID", "FORCE_NAME_E");
               
            }
            else
                {
                ViewBag.FORCE_ID = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true), "FORCE_ID", "FORCE_NAME_A");
              

            }
            ///list unhiding reports 
            return View(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true).OrderByDescending(a => a.STATION_CODE).ToList());
          
            

		}
        //save and edit new data
        public JsonResult SaveDataInDatabase(RA42_STATIONS_MST model)
        {

            var result = false;
            try
            {
                //if station_code that means this record needs update 
                if (model.STATION_CODE > 0)
                {
                    //check update permession for the current user 
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
                    //check if the record in the table then update data
                    RA42_STATIONS_MST rA42_STATIONS_MST = db.RA42_STATIONS_MST.SingleOrDefault(x => x.DLT_STS != true && x.STATION_CODE == model.STATION_CODE);
                    rA42_STATIONS_MST.FORCE_ID = model.FORCE_ID;
                    rA42_STATIONS_MST.STATION_NAME_A = model.STATION_NAME_A;
                    rA42_STATIONS_MST.STATION_NAME_E = model.STATION_NAME_E;
                    rA42_STATIONS_MST.UNIT_CODE = model.UNIT_CODE;
                    rA42_STATIONS_MST.PHONE_NUMBER = model.PHONE_NUMBER;
                    rA42_STATIONS_MST.REMARKS = model.REMARKS;
                    rA42_STATIONS_MST.UPD_BY = currentUser;
                    rA42_STATIONS_MST.UPD_DT = DateTime.Now;
                    rA42_STATIONS_MST.DLT_STS = false;
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
                        if (ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_add"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }
                        
                    }
                    //add new station
                    RA42_STATIONS_MST rA42_STATIONS_MST = new RA42_STATIONS_MST();
                    rA42_STATIONS_MST.FORCE_ID = model.FORCE_ID;
                    rA42_STATIONS_MST.STATION_NAME_A = model.STATION_NAME_A;
                    rA42_STATIONS_MST.STATION_NAME_E = model.STATION_NAME_E;
                    rA42_STATIONS_MST.UNIT_CODE = model.UNIT_CODE;
                    rA42_STATIONS_MST.PHONE_NUMBER = model.PHONE_NUMBER;
                    rA42_STATIONS_MST.REMARKS = model.REMARKS;
                    rA42_STATIONS_MST.UPD_BY = currentUser;
                    rA42_STATIONS_MST.UPD_DT = DateTime.Now;
                    rA42_STATIONS_MST.CRD_BY = currentUser;
                    rA42_STATIONS_MST.CRD_DT = DateTime.Now;
                    rA42_STATIONS_MST.DLT_STS = false;
                    db.RA42_STATIONS_MST.Add(rA42_STATIONS_MST);
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
        //get specific record data as json 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            var value = (from a in db.RA42_STATIONS_MST
                         join b in db.RA42_FORCES_MST on a.FORCE_ID equals b.FORCE_ID
                         where a.STATION_CODE == Id
                         select new
                         {

                             a.STATION_CODE,
                             a.STATION_NAME_A,
                             a.STATION_NAME_E,
                             a.UNIT_CODE,
                             a.PHONE_NUMBER,
                             a.REMARKS,
                             b.FORCE_ID,
                             b.FORCE_NAME_A,
                             b.FORCE_NAME_E,
                             a.CRD_BY,
                             a.CRD_DT,
                             a.UPD_BY,
                             a.UPD_DT


                         }).FirstOrDefault();

            //return new JsonResult() { Data = value, JsonRequestBehavior = JsonRequestBehavior.AllowGet, MaxJsonLength = Int32.MaxValue };
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        //delete or hide record 
        public JsonResult DltRecordById(int Id)
        {
            //check if current user has deleting permession 
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
            //check if record in the table then hide record
            RA42_STATIONS_MST rA42_STATIONS_MST = db.RA42_STATIONS_MST.Where(x => x.STATION_CODE == Id).FirstOrDefault();
            if (rA42_STATIONS_MST != null)
            {
                rA42_STATIONS_MST.UPD_BY = currentUser;
                rA42_STATIONS_MST.UPD_DT = DateTime.Now;
                //hide record by set DLT_STS to true 
                rA42_STATIONS_MST.DLT_STS = true;
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
