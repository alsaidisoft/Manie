using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using APP.Filters;
using APP.Util;
using portal.Controllers;
using SecurityClearanceWebApp.Models;
using SecurityClearanceWebApp.Util;

//this controller for controlling any event or excersise 

namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
	public class EventExcersicemstController : Controller
	{
        //get db connection
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get currnt user
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class
        private GeneralFunctions general = new GeneralFunctions();

        //set controller title 
        private string title = Resources.Settings.ResourceManager.GetString("eve_exc" + "_" + "ar");

        //this is constructor 
        public EventExcersicemstController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "EventExcersicemst";
            //this is fontawesom icon of this controller and will be showen in views
			 ViewBag.controllerIconClass = "fa fa-calendar-alt";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("eve_exc" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;
		}


        // get all events and excersises
        public ActionResult Index()
        {
           
                //check administrator or developer permession
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    general.NotFound();

                }
            
            List<SelectListItem> lst = new List<SelectListItem>();
            if (Language.GetCurrentLang() == "en")
            {
                ViewBag.EVENT_EXCERSICE_TYPES = new SelectList(db.RA42_EVENTS_TYPE_MST.Where(a => a.DLT_STS != true), "EVENT_CODE", "EVENT_TYPE_E");
            }
            else
            {
                ViewBag.EVENT_EXCERSICE_TYPES = new SelectList(db.RA42_EVENTS_TYPE_MST.Where(a => a.DLT_STS != true), "EVENT_CODE", "EVENT_TYPE_A");

            }
            //return all events and excersises
            return View(db.RA42_EVENT_EXERCISE_MST.Where(a => a.DLT_STS != true).OrderByDescending(a => a.EVENT_EXERCISE_CODE).ToList());
        }
        
        //save new data in the database 
        [HttpPost]
        public JsonResult SaveDataInDatabase(RA42_EVENT_EXERCISE_MST model, HttpPostedFileBase EVENT_EXERCISE_IMAGE)
        {

            var result = false;
            try
            {
                //upload new image file for this event'
                //uploading now not activated, because we use rafo logo in each excersise and event card
                if (EVENT_EXERCISE_IMAGE != null)
                {
                    try
                    {


                        // Verify that the user selected a file
                        if (EVENT_EXERCISE_IMAGE != null && EVENT_EXERCISE_IMAGE.ContentLength > 0)
                        {
                            // extract only the filename with extention
                            string fileName = Path.GetFileNameWithoutExtension(EVENT_EXERCISE_IMAGE.FileName);
                            string extension = Path.GetExtension(EVENT_EXERCISE_IMAGE.FileName);


                            //check extention of image file 
                            if (general.CheckPersonalImage(EVENT_EXERCISE_IMAGE.FileName))
                            {

                                fileName = "Event_Exersice_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                string path = Path.Combine(Server.MapPath("~/Files/Others/"), fileName);
                                model.EVENT_EXERCISE_IMAGE = fileName;
                                EVENT_EXERCISE_IMAGE.SaveAs(path);


                            }
                            else
                            {
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = " image should be in image format - يجب أن تكون الصورة  ملف (JPG,GIF)";
                                return Json(false, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                //if model.EVENT_EXERCISE_CODE > 0 is greatr than 0 that means this record need to update 
                if (model.EVENT_EXERCISE_CODE > 0)
                {
                    //check update permession 
                    if (ViewBag.UP != true)
                    {
                        //check administrator or developer permession
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_up"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }

                    }
                    RA42_EVENT_EXERCISE_MST rA42_EVENT_EXERSICE_MST = db.RA42_EVENT_EXERCISE_MST.SingleOrDefault(x => x.DLT_STS != true && x.EVENT_EXERCISE_CODE == model.EVENT_EXERCISE_CODE);
                    rA42_EVENT_EXERSICE_MST.EVENT_EXERCISE_NAME = model.EVENT_EXERCISE_NAME;
                    rA42_EVENT_EXERSICE_MST.EVENT_EXERCISE_NAME_E = model.EVENT_EXERCISE_NAME_E;
                    //check if image file not null to upload new image 
                    if (model.EVENT_EXERCISE_IMAGE != null)
                    {
                        if (model.EVENT_EXERCISE_IMAGE == "undefined")
                        {
                            rA42_EVENT_EXERSICE_MST.EVENT_EXERCISE_IMAGE = rA42_EVENT_EXERSICE_MST.EVENT_EXERCISE_IMAGE;
                        }
                        else
                        {
                            rA42_EVENT_EXERSICE_MST.EVENT_EXERCISE_IMAGE = model.EVENT_EXERCISE_IMAGE;
                        }
                    }
                    rA42_EVENT_EXERSICE_MST.EVENT_CODE = model.EVENT_CODE;
                    rA42_EVENT_EXERSICE_MST.DATE_FROM = model.DATE_FROM;
                    rA42_EVENT_EXERSICE_MST.DATE_TO = model.DATE_TO;
                    rA42_EVENT_EXERSICE_MST.REMARKS = model.REMARKS;
                    rA42_EVENT_EXERSICE_MST.ACTIVE = model.ACTIVE;
                    rA42_EVENT_EXERSICE_MST.UPD_BY = currentUser;
                    rA42_EVENT_EXERSICE_MST.UPD_DT = DateTime.Now;
                    rA42_EVENT_EXERSICE_MST.DLT_STS = false;
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
                        //check administrator or developer permession
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            //show error message if no permessions
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_add"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }

                    }
                    //add new event 
                    RA42_EVENT_EXERCISE_MST rA42_EVENT_EXERSICE_MST = new RA42_EVENT_EXERCISE_MST();
                    rA42_EVENT_EXERSICE_MST.EVENT_EXERCISE_NAME = model.EVENT_EXERCISE_NAME;
                    rA42_EVENT_EXERSICE_MST.EVENT_EXERCISE_NAME_E = model.EVENT_EXERCISE_NAME_E;
                    rA42_EVENT_EXERSICE_MST.EVENT_EXERCISE_IMAGE = model.EVENT_EXERCISE_IMAGE;
                    rA42_EVENT_EXERSICE_MST.EVENT_CODE = model.EVENT_CODE;
                    rA42_EVENT_EXERSICE_MST.DATE_FROM = model.DATE_FROM;
                    rA42_EVENT_EXERSICE_MST.DATE_TO = model.DATE_TO;
                    rA42_EVENT_EXERSICE_MST.REMARKS = model.REMARKS;
                    rA42_EVENT_EXERSICE_MST.ACTIVE = model.ACTIVE;
                    rA42_EVENT_EXERSICE_MST.UPD_BY = currentUser;
                    rA42_EVENT_EXERSICE_MST.UPD_DT = DateTime.Now;
                    rA42_EVENT_EXERSICE_MST.CRD_BY = currentUser;
                    rA42_EVENT_EXERSICE_MST.CRD_DT = DateTime.Now;
                    rA42_EVENT_EXERSICE_MST.DLT_STS = false;
                    db.RA42_EVENT_EXERCISE_MST.Add(rA42_EVENT_EXERSICE_MST);
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
        
        //get data for specific event 
        public JsonResult GetObjectById(int Id)
        {
            //this line is important to return json result 
            db.Configuration.ProxyCreationEnabled = false;
            
                var value = (from a in db.RA42_EVENT_EXERCISE_MST
                             join b in db.RA42_EVENTS_TYPE_MST on a.EVENT_CODE equals b.EVENT_CODE
                             where a.EVENT_EXERCISE_CODE == Id
                             select new
                             {

                                 a.EVENT_EXERCISE_CODE,
                                 a.EVENT_CODE,
                                 a.EVENT_EXERCISE_IMAGE,
                                a.EVENT_EXERCISE_NAME,
                                 a.EVENT_EXERCISE_NAME_E,
                                 a.DATE_FROM,
                                 a.DATE_TO,
                                 a.REMARKS,
                                 b.EVENT_TYPE_A,
                                 b.EVENT_TYPE_E,
                                 a.CRD_BY,
                                 a.CRD_DT,
                                 a.UPD_BY,
                                 a.UPD_DT


                             }).FirstOrDefault();

               
                //Add JsonRequest behavior to allow retrieving states over http get
                return Json(value, JsonRequestBehavior.AllowGet);
          
        
            
        }

       //delet or hide specifc record 
        public JsonResult DltRecordById(int Id)
        {
            //cehck delete permession
            if (ViewBag.DLT != true)
            {
                //check administrator or developer permession
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    AddToast(new Toast("",
                  GetResourcesValue("Dont_have_permissions_to_dlt"),
                  "red"));
                    return Json(false, JsonRequestBehavior.AllowGet);
                }

            }
            //set DLT_STS to true to hide this event 
            bool result = false;
            RA42_EVENT_EXERCISE_MST rA42_EVENT_EXERSICE_MST = db.RA42_EVENT_EXERCISE_MST.Where(x => x.EVENT_EXERCISE_CODE == Id).FirstOrDefault();
            if (rA42_EVENT_EXERSICE_MST != null)
            {
                rA42_EVENT_EXERSICE_MST.UPD_BY = currentUser;
                rA42_EVENT_EXERSICE_MST.UPD_DT = DateTime.Now;
                rA42_EVENT_EXERSICE_MST.DLT_STS = true;
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
