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
    //this is zone master controller to add,delete,edit and retrive zones and gates for all stations 
    public class ZoneareamstController : Controller
    {
        //get db connection 
        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class 
        private GeneralFunctions general = new GeneralFunctions();
        //set title for controller from resources 
        private string title = Resources.Settings.ResourceManager.GetString("Zones" + "_" + "ar");
        public ZoneareamstController()
        {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Zoneareamst";
            //set icon of controller from fontawsome library 
            ViewBag.controllerIconClass = "fa fa-braille";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("Zones" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;


        }


        //this is main view to add, edit, delete and retrive zones and gates 
        public ActionResult Index()
        {
           
            
            //get all stations for administrator and developer 
            if (ViewBag.DEVELOPER == true)
            {
                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST.Where(a=>a.DLT_STS !=true), "STATION_CODE", "STATION_NAME_E");

                }
                else
                {
                    ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true), "STATION_CODE", "STATION_NAME_A");

                }
                return View(db.RA42_ZONE_AREA_MST.Where(a => a.DLT_STS != true).OrderByDescending(a => a.ZONE_CODE).ToList());

            }
            //get specific station for normal user 
            else
            {
                
                int unit = Convert.ToInt32(ViewBag.FORCE_TYPE_CODE);


                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST.Where(a => a.FORCE_ID == unit && a.DLT_STS !=true), "STATION_CODE", "STATION_NAME_E");

                }
                else
                {
                    ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST.Where(a => a.FORCE_ID == unit && a.DLT_STS !=true), "STATION_CODE", "STATION_NAME_A");

                }


                if (ViewBag.ADMIN == true)
                {

                    return View(db.RA42_ZONE_AREA_MST.Where(a => a.RA42_STATIONS_MST.FORCE_ID == unit && a.DLT_STS != true).OrderByDescending(a => a.ZONE_CODE).ToList());
                }
                else
                {
                    //get current user STATION_CODE from viewbag.STATION_CODE_TYPE which is identified in PermessionFilter class in Filters folder
                    int station = Convert.ToInt32(ViewBag.STATION_CODE_TYPE);
                    return View(db.RA42_ZONE_AREA_MST.Where(a => a.RA42_STATIONS_MST.STATION_CODE == station && a.DLT_STS != true).OrderByDescending(a => a.ZONE_CODE).ToList());

                }
            }


        }
        //save and edit new gat and zone
        public JsonResult SaveDataInDatabase(RA42_ZONE_AREA_MST model)
        {

            var result = false;
            try
            {
                //check if ZONE_CODE is greater than 0 that means this record needs update 
                if (model.ZONE_CODE > 0)
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
                    //check if record in the db table then update record 
                    RA42_ZONE_AREA_MST rA42_ZONE_AREA_MST = db.RA42_ZONE_AREA_MST.SingleOrDefault(x => x.DLT_STS != true && x.ZONE_CODE == model.ZONE_CODE);
                    //var stn = db.RA42_STATIONS_MST.Where(a => a.UNIT_CODE == model.UNIT_CODE.ToString()).FirstOrDefault();
                    rA42_ZONE_AREA_MST.STATION_CODE = model.STATION_CODE;
                   // rA42_ZONE_AREA_MST.UNIT_CODE = model.UNIT_CODE;
                    rA42_ZONE_AREA_MST.ZONE_NAME = model.ZONE_NAME;
                    rA42_ZONE_AREA_MST.ZONE_NAME_E = model.ZONE_NAME_E;
                    rA42_ZONE_AREA_MST.ZONE_NUMBER = model.ZONE_NUMBER;
                    rA42_ZONE_AREA_MST.ZONE_TYPE = model.ZONE_TYPE;
                    rA42_ZONE_AREA_MST.REMARKS = model.REMARKS;
                    rA42_ZONE_AREA_MST.UPD_BY = currentUser;
                    rA42_ZONE_AREA_MST.UPD_DT = DateTime.Now;
                    rA42_ZONE_AREA_MST.DLT_STS = false;
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
                    //add new record 
                    RA42_ZONE_AREA_MST rA42_ZONE_AREA_MST = new RA42_ZONE_AREA_MST();

                    //var stn = db.RA42_STATIONS_MST.Where(a => a.UNIT_CODE == model.UNIT_CODE.ToString()).FirstOrDefault();
                    //var v = db.RA42_ZONE_AREA_MST.Where(a => a.ZONE_NUMBER == model.ZONE_NUMBER && a.STATION_CODE == model.STATION_CODE && a.ZONE_TYPE == a.ZONE_TYPE && a.DLT_STS != true).ToList();


                    if (model.ZONE_NUMBER <= 0)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("number_is_less_than_zero"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }
                    //if (v == null)
                    //{
                        rA42_ZONE_AREA_MST.STATION_CODE = model.STATION_CODE;
                        //rA42_ZONE_AREA_MST.UNIT_CODE = model.UNIT_CODE;
                        rA42_ZONE_AREA_MST.ZONE_NAME = model.ZONE_NAME;
                        rA42_ZONE_AREA_MST.ZONE_NUMBER = model.ZONE_NUMBER;
                        rA42_ZONE_AREA_MST.ZONE_NAME_E = model.ZONE_NAME_E;
                        rA42_ZONE_AREA_MST.ZONE_TYPE = model.ZONE_TYPE;
                        rA42_ZONE_AREA_MST.REMARKS = model.REMARKS;
                        rA42_ZONE_AREA_MST.CRD_BY = currentUser;
                        rA42_ZONE_AREA_MST.CRD_DT = DateTime.Now;
                        rA42_ZONE_AREA_MST.UPD_BY = currentUser;
                        rA42_ZONE_AREA_MST.UPD_DT = DateTime.Now;
                        rA42_ZONE_AREA_MST.DLT_STS = false;
                        db.RA42_ZONE_AREA_MST.Add(rA42_ZONE_AREA_MST);
                        db.SaveChanges();
                        result = true;
                        AddToast(new Toast("",
                        GetResourcesValue("success_create_message"),
                        "green"));
                    //}
                    //else
                    //{
                    //    AddToast(new Toast("",
                    //     GetResourcesValue("same_area_number"),
                    //     "red"));
                    //    return Json(false, JsonRequestBehavior.AllowGet);
                    //}
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

        //ghet specific record data
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            var value = (from a in db.RA42_ZONE_AREA_MST
                         join b in db.RA42_STATIONS_MST on a.STATION_CODE equals b.STATION_CODE
                         where a.ZONE_CODE == Id
                         select new
                         {

                             a.ZONE_CODE,
                             a.ZONE_NAME,
                             a.ZONE_NAME_E,
                             a.ZONE_NUMBER,
                             a.ZONE_TYPE,
                             a.STATION_CODE,
                             a.REMARKS,
                             b.STATION_NAME_A,
                             b.STATION_NAME_E,
                             //b.STATION_CODE,
                             a.CRD_BY,
                             a.CRD_DT,
                             a.UPD_BY,
                             a.UPD_DT


                         }).FirstOrDefault();

            //return new JsonResult() { Data = value, JsonRequestBehavior = JsonRequestBehavior.AllowGet, MaxJsonLength = Int32.MaxValue };
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        //hide record 
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
            RA42_ZONE_AREA_MST rA42_ZONE_AREA_MST = db.RA42_ZONE_AREA_MST.Where(x => x.ZONE_CODE == Id).FirstOrDefault();
            if (rA42_ZONE_AREA_MST != null)
            {
                rA42_ZONE_AREA_MST.UPD_BY = currentUser;
                rA42_ZONE_AREA_MST.UPD_DT = DateTime.Now;
                //hide record by set DLT_STS to true 
                rA42_ZONE_AREA_MST.DLT_STS = true;
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
