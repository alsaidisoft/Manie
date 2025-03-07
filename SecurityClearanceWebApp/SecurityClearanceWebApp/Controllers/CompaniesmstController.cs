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

//this controller responsible companies
namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
	public class CompaniesmstController : Controller
	{
        //get database connection 
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        //tost list identifier
        private IList<Toast> toasts = new List<Toast>();
        //get current user 
        private string currentUser = (new UserInfo()).getSNO();

        //identify GeneralFunctions class
        private GeneralFunctions general = new GeneralFunctions();
        private string title = Resources.Settings.ResourceManager.GetString("Companies" + "_" + "ar");
       
        public CompaniesmstController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Companiesmst";
            // icon of controller
			 ViewBag.controllerIconClass = "fa fa-building";
            if(Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("Companies" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;
           
        }


       //show companies 
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

            //get force id
            int force = Convert.ToInt32(ViewBag.FORCE_TYPE_CODE);

            if (ViewBag.DEVELOPER == true)
            {
                List<RA42_COMPANY_TYPE_MST> campList = db.RA42_COMPANY_TYPE_MST.Where(a => a.DLT_STS != true).ToList();
                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.COMPANY_TYPES = new SelectList(campList, "COMPANY_TYPE_CODE", "COMPANY_TYPE_E");
                    ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST, "STATION_CODE", "STATION_NAME_E");
                }
                else
                {
                    ViewBag.COMPANY_TYPES = new SelectList(campList, "COMPANY_TYPE_CODE", "COMPANY_TYPE");
                    ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST.Where(a=>a.DLT_STS !=true), "STATION_CODE", "STATION_NAME_A");
                }
                return View(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true).Include(a => a.RA42_COMPANY_TYPE_MST).OrderByDescending(a => a.COMPANY_CODE).ToList());

            }

            if (ViewBag.ADMIN == true)
            {
                List<RA42_COMPANY_TYPE_MST> campList = db.RA42_COMPANY_TYPE_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2).ToList();
                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.COMPANY_TYPES = new SelectList(campList, "COMPANY_TYPE_CODE", "COMPANY_TYPE_E");
                    ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST.Where(a=>a.DLT_STS !=true && a.FORCE_ID == force), "STATION_CODE", "STATION_NAME_E");
                }
                else
                {
                    ViewBag.COMPANY_TYPES = new SelectList(campList, "COMPANY_TYPE_CODE", "COMPANY_TYPE");
                    ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == force), "STATION_CODE", "STATION_NAME_A");
                }
                return View(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.FORCE_ID == force && a.COMPANY_TYPE_CODE == 2).Include(a => a.RA42_COMPANY_TYPE_MST).OrderByDescending(a => a.COMPANY_CODE).ToList());

            }

            
            
            int unit = Convert.ToInt32(ViewBag.STATION_CODE_TYPE);
            List<RA42_COMPANY_TYPE_MST> campList1 = db.RA42_COMPANY_TYPE_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                ViewBag.COMPANY_TYPES = new SelectList(campList1, "COMPANY_TYPE_CODE", "COMPANY_TYPE_E");
                ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == unit), "STATION_CODE", "STATION_NAME_E");
            }
            else
            {
                ViewBag.COMPANY_TYPES = new SelectList(campList1, "COMPANY_TYPE_CODE", "COMPANY_TYPE");
                ViewBag.STATIONS = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == unit), "STATION_CODE", "STATION_NAME_A");
            }

            return View(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == unit && a.COMPANY_TYPE_CODE == 2).Include(a => a.RA42_COMPANY_TYPE_MST).OrderByDescending(a => a.COMPANY_CODE).ToList());
            
        }

        //saving data
        public JsonResult SaveDataInDatabase(RA42_COMPANY_MST model, HttpPostedFileBase DOCUMENT)
        {

            var result = false;

            if (DOCUMENT != null)
            {
                try
                {


                    // Verify that the user selected a file
                    if (DOCUMENT != null && DOCUMENT.ContentLength > 0)
                    {
                        // extract only the filename with extention
                        string fileName = Path.GetFileNameWithoutExtension(DOCUMENT.FileName);
                        string extension = Path.GetExtension(DOCUMENT.FileName);


                        //check extention of image file 
                        if (general.CheckFileType(DOCUMENT.FileName))
                        {

                            fileName = "company_document" + DateTime.Now.ToString("yymmssfff") + extension;
                            // store the file inside ~/App_Data/uploads folder
                            string path = Path.Combine(Server.MapPath("~/Files/Others/"), fileName);
                            DOCUMENT.SaveAs(path);
                            model.FILE_NAME = fileName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.GetBaseException();
                }
            }
            try
            {
                // if model.COMPANY_CODE > 0 that means this record is excting in table an need update 
                if (model.COMPANY_CODE > 0)
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
                    
                    RA42_COMPANY_MST rA42_COMPANY_MST = db.RA42_COMPANY_MST.SingleOrDefault(x => x.DLT_STS != true && x.COMPANY_CODE == model.COMPANY_CODE);
                    rA42_COMPANY_MST.COMPANY_NAME = model.COMPANY_NAME;
                    rA42_COMPANY_MST.COMPANY_NAME_E = model.COMPANY_NAME_E;
                    rA42_COMPANY_MST.STATION_CODE = model.STATION_CODE;
                    //administrator user can add any type of company (normal companies or contracting company)
                    rA42_COMPANY_MST.COMPANY_TYPE_CODE = model.COMPANY_TYPE_CODE;
                    rA42_COMPANY_MST.PHONE_NUMBER = model.PHONE_NUMBER;
                    rA42_COMPANY_MST.GSM = model.GSM;
                    rA42_COMPANY_MST.WEBSITE = model.WEBSITE;
                    rA42_COMPANY_MST.REMARKS = model.REMARKS;
                    if (!string.IsNullOrWhiteSpace(model.FILE_NAME))
                    {
                        rA42_COMPANY_MST.FILE_NAME = model.FILE_NAME;
                    }
                    rA42_COMPANY_MST.UPD_BY = currentUser;
                    rA42_COMPANY_MST.UPD_DT = DateTime.Now;
                    rA42_COMPANY_MST.DLT_STS = false;
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
                    RA42_COMPANY_MST rA42_COMPANY_MST = new RA42_COMPANY_MST();
                    rA42_COMPANY_MST.COMPANY_NAME = model.COMPANY_NAME;
                    rA42_COMPANY_MST.COMPANY_NAME_E = model.COMPANY_NAME_E;
                    rA42_COMPANY_MST.STATION_CODE = model.STATION_CODE;
                    //administrator user can add any type of company (normal companies or contracting company)
                    rA42_COMPANY_MST.COMPANY_TYPE_CODE = model.COMPANY_TYPE_CODE;
                    rA42_COMPANY_MST.PHONE_NUMBER = model.PHONE_NUMBER;
                    rA42_COMPANY_MST.GSM = model.GSM;
                    rA42_COMPANY_MST.WEBSITE = model.WEBSITE;
                    rA42_COMPANY_MST.REMARKS = model.REMARKS;
                    rA42_COMPANY_MST.FILE_NAME = model.FILE_NAME;
                    rA42_COMPANY_MST.CRD_BY = currentUser;
                    rA42_COMPANY_MST.CRD_DT = DateTime.Now;
                    rA42_COMPANY_MST.UPD_BY = currentUser;
                    rA42_COMPANY_MST.UPD_DT = DateTime.Now;
                    rA42_COMPANY_MST.DLT_STS = false;
                    db.RA42_COMPANY_MST.Add(rA42_COMPANY_MST);
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

        //get json array data of specific company 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            //in english if its true 
            if (Language.GetCurrentLang() == "en")
            {
                var value = (from a in db.RA42_COMPANY_MST
                             join b in db.RA42_COMPANY_TYPE_MST on a.COMPANY_TYPE_CODE equals b.COMPANY_TYPE_CODE
                             where a.COMPANY_CODE == Id
                             select new
                             {

                                 a.COMPANY_CODE,
                                 a.STATION_CODE,
                                 a.COMPANY_NAME,
                                 a.COMPANY_NAME_E,
                                 a.PHONE_NUMBER,
                                 a.GSM,
                                 a.WEBSITE,
                                 a.REMARKS,
                                 COMPANY_TYPE = b.COMPANY_TYPE_E,
                                 STATION = a.RA42_STATIONS_MST.STATION_NAME_E,
                                 FORCE = a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_NAME_E,
                                 a.COMPANY_TYPE_CODE,
                                 a.FILE_NAME,
                                 a.CRD_BY,
                                 a.CRD_DT,
                                 a.UPD_BY,
                                 a.UPD_DT


                             }).FirstOrDefault();

                
                //Add JsonRequest behavior to allow retrieving states over http get
                return Json(value, JsonRequestBehavior.AllowGet);
            }
            else
            {
                //in arabic if its true 
                var value = (from a in db.RA42_COMPANY_MST
                             join b in db.RA42_COMPANY_TYPE_MST on a.COMPANY_TYPE_CODE equals b.COMPANY_TYPE_CODE
                             where a.COMPANY_CODE == Id
                             select new
                             {

                                 a.COMPANY_CODE,
                                 a.STATION_CODE,
                                 a.COMPANY_NAME,
                                 a.COMPANY_NAME_E,
                                 a.PHONE_NUMBER,
                                 a.GSM,
                                 a.WEBSITE,
                                 a.REMARKS,
                                 b.COMPANY_TYPE,
                                 a.COMPANY_TYPE_CODE,
                                 STATION = a.RA42_STATIONS_MST.STATION_NAME_A,
                                 FORCE = a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_NAME_A,
                                 a.FILE_NAME,
                                 a.CRD_BY,
                                 a.CRD_DT,
                                 a.UPD_BY,
                                 a.UPD_DT


                             }).FirstOrDefault();

              
                //Add JsonRequest behavior to allow retrieving states over http get
                return Json(value, JsonRequestBehavior.AllowGet);
            }
               
        }

        //delete or hide record
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
            RA42_COMPANY_MST rA42_COMPANY_MST = db.RA42_COMPANY_MST.Where(x => x.COMPANY_CODE == Id).FirstOrDefault();
            if (rA42_COMPANY_MST != null)
            {
                rA42_COMPANY_MST.UPD_BY = currentUser;
                rA42_COMPANY_MST.UPD_DT = DateTime.Now;
                //hide record by setting DLT_STS = true
                rA42_COMPANY_MST.DLT_STS = true;
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
