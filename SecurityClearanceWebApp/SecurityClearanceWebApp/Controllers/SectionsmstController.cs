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
using Microsoft.Ajax.Utilities;
using portal.Controllers;
using SecurityClearanceWebApp.Models;
using SecurityClearanceWebApp.Util;

namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    //this is color controller for the vechiles 
	public class SectionsmstController : Controller
	{
        //get db connection 
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class
        private GeneralFunctions general = new GeneralFunctions();
        //set title for the controller from resources 
        private string title = Resources.Settings.ResourceManager.GetString("sections" + "_" + "ar");
        public SectionsmstController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Sectionsmst";
            //set icon from fontawsome for the controller 
			ViewBag.controllerIconClass = "fa fa-object-group";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("sections" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;

        }


		// this is the main view to add, edit, delete and retrive data 
		public ActionResult Index()
		{
            //get force id
            int force = Convert.ToInt32(ViewBag.FORCE_TYPE_CODE);

            if (ViewBag.DEVELOPER == true)
            {
                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST, "STATION_CODE", "STATION_NAME_E");
                }
                else
                {
                    ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true), "STATION_CODE", "STATION_NAME_A");
                }
                return View(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true).Include(a => a.RA42_STATIONS_MST).OrderByDescending(a => a.SECTION_CODE).ToList());

            }

            if (ViewBag.ADMIN == true)
            {
                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == force), "STATION_CODE", "STATION_NAME_E");
                }
                else
                {
                    ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == force), "STATION_CODE", "STATION_NAME_A");
                }
                return View(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.FORCE_ID == force).Include(a => a.RA42_STATIONS_MST).OrderByDescending(a => a.SECTION_CODE).ToList());

            }



            int unit = Convert.ToInt32(ViewBag.STATION_CODE_TYPE);
            if (Language.GetCurrentLang() == "en")
            {
                ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == unit), "STATION_CODE", "STATION_NAME_E");
            }
            else
            {
                ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == unit), "STATION_CODE", "STATION_NAME_A");
            }

            return View(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == unit).Include(a => a.RA42_STATIONS_MST).OrderByDescending(a => a.SECTION_CODE).ToList());

        }
        //add and edit new data
        public JsonResult SaveDataInDatabase(RA42_SECTIONS_MST model)
        {

            var result = false;
            try
            {
                //if the VECHILE_COLOR_CODE greater than 0 that means this record needs update 
                if (model.SECTION_CODE > 0)
                {
                    //check if current user has update permession 
                    if (ViewBag.UP != true)
                    {
                        //check if the current user is admin or developer 
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_up"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }
                    }
                    //check if the record is in the table 
                    RA42_SECTIONS_MST rA42_SECTIONS_MST = db.RA42_SECTIONS_MST.SingleOrDefault(x => x.DLT_STS != true && x.SECTION_CODE == model.SECTION_CODE);
                    rA42_SECTIONS_MST.SECTION_NAME = model.SECTION_NAME;
                    rA42_SECTIONS_MST.STATION_CODE = model.STATION_CODE;
                    rA42_SECTIONS_MST.UPD_BY = currentUser;
                    rA42_SECTIONS_MST.UPD_DT = DateTime.Now;
                    rA42_SECTIONS_MST.DLT_STS = false;
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
                        //check if the current user is admin or developer 
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_add"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }
                        
                    }
                    //add new record 
                    RA42_SECTIONS_MST rA42_SECTIONS_MST = new RA42_SECTIONS_MST();
                    rA42_SECTIONS_MST.SECTION_NAME = model.SECTION_NAME;
                    rA42_SECTIONS_MST.STATION_CODE = model.STATION_CODE;
                    rA42_SECTIONS_MST.UPD_BY = currentUser;
                    rA42_SECTIONS_MST.UPD_DT = DateTime.Now;
                    rA42_SECTIONS_MST.CRD_BY = currentUser;
                    rA42_SECTIONS_MST.CRD_DT = DateTime.Now;
                    rA42_SECTIONS_MST.DLT_STS = false;
                    db.RA42_SECTIONS_MST.Add(rA42_SECTIONS_MST);
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
        //get specific data for any record as json result 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var value = (from a in db.RA42_SECTIONS_MST
                         where a.SECTION_CODE == Id
                         select new
                         {

                             a.STATION_CODE,
                             a.SECTION_NAME,
                             a.SECTION_CODE,
                             a.RA42_STATIONS_MST.STATION_NAME_A,
                             a.RA42_STATIONS_MST.STATION_NAME_E,
                             a.CRD_BY,
                             a.CRD_DT,
                             a.UPD_BY,
                             a.UPD_DT


                         }).FirstOrDefault();


            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        //this function to hide any record 
        public JsonResult DltRecordById(int Id)
        {
            //check if the current user has delete permession 
            if (ViewBag.DLT != true)
            {
                //check if the current user is admin or developer 
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    AddToast(new Toast("",
                  GetResourcesValue("Dont_have_permissions_to_dlt"),
                  "red"));
                    return Json(false, JsonRequestBehavior.AllowGet);
                }
                
            }
            bool result = false;

            RA42_SECTIONS_MST rA42_SECTIONS_MST = db.RA42_SECTIONS_MST.Where(x => x.SECTION_CODE == Id).FirstOrDefault();
            if (rA42_SECTIONS_MST != null)
            {
                rA42_SECTIONS_MST.UPD_BY = currentUser;
                rA42_SECTIONS_MST.UPD_DT = DateTime.Now;
                //hide record by setting DLT_STS = true 
                rA42_SECTIONS_MST.DLT_STS = true;
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
