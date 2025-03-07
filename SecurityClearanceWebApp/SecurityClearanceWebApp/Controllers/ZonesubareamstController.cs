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

namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    //this is sub zomes controller to attach sub zones for main zones, this is working probaply now, but they dont activate sub zones yet, today is (10-11-2020)
    
	public class ZonesubareamstController : Controller
	{
        //get db connection 
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify generalfunctions class to use some of its functions 
        private GeneralFunctions general = new GeneralFunctions();
        private string title = Resources.Settings.ResourceManager.GetString("Subzone" + "_" + "ar");
        public ZonesubareamstController() {
            ViewBag.Settings = "Settings";
            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Zonesubareamst";
            //set icon of controller 
			ViewBag.controllerIconClass = "fa fa-clone";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("Subzone" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;

        }


		// this is main view to add, edit, delete and retrive sub zones
		public ActionResult Index()
		{
            //check if current user has view permession
            if (ViewBag.VW != true)
            {
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    general.NotFound();
                }

            }
            if (ViewBag.ADMIN == true || ViewBag.DEVELOPER == true)
            {
                //get all zones and stations for admin and developer
                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.ZONES = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.DLT_STS != true), "ZONE_CODE", "ZONE_NAME_E");
                    ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST, "STATION_CODE", "STATION_NAME_E");
                }
                else
                {
                    ViewBag.ZONES = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.DLT_STS != true), "ZONE_CODE", "ZONE_NAME");
                    ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST, "STATION_CODE", "STATION_NAME_A");
                }
                

            }
            else
            {
                //show specific station zones and station
                int unit = Convert.ToInt32(ViewBag.UNIT_TYPE_CODE);

                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.ZONES = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.DLT_STS != true), "ZONE_CODE", "ZONE_NAME_E");
                    ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST.Where(a => a.UNIT_CODE == unit.ToString()), "STATION_CODE", "STATION_NAME_E");
                }
                else
                {
                    ViewBag.ZONES = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.DLT_STS != true), "ZONE_CODE", "ZONE_NAME");
                    ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST.Where(a => a.UNIT_CODE == unit.ToString()), "STATION_CODE", "STATION_NAME_A");
                }
            }
           
            var rA42_ZONE_SUB_AREA_MST = db.RA42_ZONE_SUB_AREA_MST.Include(r => r.RA42_ZONE_AREA_MST);
            if (ViewBag.ADMIN == true || ViewBag.DEVELOPER == true)
            {
                return View(rA42_ZONE_SUB_AREA_MST.Where(a => a.DLT_STS != true).OrderByDescending(a => a.ZONE_SUB_AREA_CODE).ToList());

            }
            else
            {
                //show specific station zones and station
                int unit = Convert.ToInt32(ViewBag.UNIT_TYPE_CODE);
                return View(rA42_ZONE_SUB_AREA_MST.Where(a =>a.RA42_ZONE_AREA_MST.UNIT_CODE == unit && a.DLT_STS != true).OrderByDescending(a => a.ZONE_SUB_AREA_CODE).ToList());
            }
            
		}
        //get main zones 
        public JsonResult GetZones(int station)
        {
            db.Configuration.ProxyCreationEnabled = false;
            if (Language.GetCurrentLang() == "en"){
                var zones = db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == station && a.DLT_STS != true).Select(a => new { ZONE_CODE = a.ZONE_CODE, ZONE_NAME = a.ZONE_NAME_E }).ToList();

                //Add JsonRequest behavior to allow retrieving states over http get
                return Json(zones, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var zones = db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == station && a.DLT_STS != true).Select(a => new { ZONE_CODE = a.ZONE_CODE, ZONE_NAME = a.ZONE_NAME }).ToList();

                //Add JsonRequest behavior to allow retrieving states over http get
                return Json(zones, JsonRequestBehavior.AllowGet);
            }
            
        }

        //save or edit new sub zones 
        public JsonResult SaveDataInDatabase(RA42_ZONE_SUB_AREA_MST model)
        {

            var result = false;
            try
            {
                //if greater than zero that means this record needs update 
                if (model.ZONE_SUB_AREA_CODE > 0)
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
                    //check if record is in the table then update data 
                    RA42_ZONE_SUB_AREA_MST rA42_ZONE_SUB_AREA_MST = db.RA42_ZONE_SUB_AREA_MST.SingleOrDefault(x => x.DLT_STS != true && x.ZONE_SUB_AREA_CODE == model.ZONE_SUB_AREA_CODE);

                    rA42_ZONE_SUB_AREA_MST.ZONE_CODE = model.ZONE_CODE;
                    rA42_ZONE_SUB_AREA_MST.SUB_ZONE_NAME = model.SUB_ZONE_NAME;
                    rA42_ZONE_SUB_AREA_MST.SUB_ZONE_NAME_E = model.SUB_ZONE_NAME_E;
                    rA42_ZONE_SUB_AREA_MST.ZONE_NUMBER = model.ZONE_NUMBER;
                    rA42_ZONE_SUB_AREA_MST.REMARKS = model.REMARKS;
                    rA42_ZONE_SUB_AREA_MST.UPD_BY = currentUser;
                    rA42_ZONE_SUB_AREA_MST.UPD_DT = DateTime.Now;
                    rA42_ZONE_SUB_AREA_MST.DLT_STS = false;
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
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_add"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }
                        
                    }
                    //dont add subzones if below condition is achivied 
                    RA42_ZONE_SUB_AREA_MST rA42_ZONE_SUB_AREA_MST = new RA42_ZONE_SUB_AREA_MST();
                    var v = db.RA42_ZONE_SUB_AREA_MST.Where(a => a.ZONE_NUMBER == model.ZONE_NUMBER && a.ZONE_CODE == model.ZONE_CODE && a.DLT_STS != true).FirstOrDefault();
                    if (v == null)
                    {
                        //add new record if condition not achived 
                        rA42_ZONE_SUB_AREA_MST.ZONE_CODE = model.ZONE_CODE;
                        rA42_ZONE_SUB_AREA_MST.SUB_ZONE_NAME = model.SUB_ZONE_NAME;
                        rA42_ZONE_SUB_AREA_MST.SUB_ZONE_NAME_E = model.SUB_ZONE_NAME_E;
                        rA42_ZONE_SUB_AREA_MST.ZONE_NUMBER = model.ZONE_NUMBER;
                        rA42_ZONE_SUB_AREA_MST.REMARKS = model.REMARKS;
                        rA42_ZONE_SUB_AREA_MST.CRD_BY = currentUser;
                        rA42_ZONE_SUB_AREA_MST.CRD_DT = DateTime.Now;
                        rA42_ZONE_SUB_AREA_MST.UPD_BY = currentUser;
                        rA42_ZONE_SUB_AREA_MST.UPD_DT = DateTime.Now;
                        rA42_ZONE_SUB_AREA_MST.DLT_STS = false;
                        db.RA42_ZONE_SUB_AREA_MST.Add(rA42_ZONE_SUB_AREA_MST);
                        db.SaveChanges();
                        result = true;
                        AddToast(new Toast("",
                        GetResourcesValue("success_create_message"),
                        "green"));
                    }

                    else
                    {
                        AddToast(new Toast("",
                          GetResourcesValue("same_area_number"),
                          "red"));
                        return Json(false, JsonRequestBehavior.AllowGet);
                    }
                }
            }

            catch (Exception ex)
            {
                AddToast(new Toast("",
                GetResourcesValue("error_create_message"),
                "red"));
                throw ex.GetBaseException();
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        //get specific subzone data
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;

            var value = (from a in db.RA42_ZONE_SUB_AREA_MST
                         join b in db.RA42_ZONE_AREA_MST on a.ZONE_CODE equals b.ZONE_CODE
                         where a.ZONE_SUB_AREA_CODE == Id
                         select new
                         {
                             a.ZONE_SUB_AREA_CODE,
                             a.SUB_ZONE_NAME,
                             a.SUB_ZONE_NAME_E,
                             a.ZONE_NUMBER,
                             a.REMARKS,
                             b.ZONE_CODE,
                             b.ZONE_NAME,
                             b.ZONE_NAME_E,
                             b.STATION_CODE,
                             b.RA42_STATIONS_MST.STATION_NAME_A,
                             b.RA42_STATIONS_MST.STATION_NAME_E,
                             a.CRD_BY,
                             a.CRD_DT,
                             a.UPD_BY,
                             a.UPD_DT


                         }).FirstOrDefault();

            //return new JsonResult() { Data = value, JsonRequestBehavior = JsonRequestBehavior.AllowGet, MaxJsonLength = Int32.MaxValue };
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        //hide subzone 
        public JsonResult DltRecordById(int Id)
        {
            //check if current user has deleting permession 
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
            RA42_ZONE_SUB_AREA_MST rA42_ZONE_SUB_AREA_MST = db.RA42_ZONE_SUB_AREA_MST.Where(x => x.ZONE_SUB_AREA_CODE == Id).FirstOrDefault();
            if (rA42_ZONE_SUB_AREA_MST != null)
            {
                rA42_ZONE_SUB_AREA_MST.UPD_BY = currentUser;
                rA42_ZONE_SUB_AREA_MST.UPD_DT = DateTime.Now;
                //hide record by set DLT_STS to true 
                rA42_ZONE_SUB_AREA_MST.DLT_STS = true;
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
