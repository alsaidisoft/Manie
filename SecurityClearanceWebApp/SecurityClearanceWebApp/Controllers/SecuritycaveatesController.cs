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
    //this controller is for security caveats,add,edit,delete and rerive caveates 
	public class SecuritycaveatesController : Controller
	{
        //get db connection 
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GEneralFunction class
        private GeneralFunctions general = new GeneralFunctions();
        //set title for the controller from the resources 
        private string title = Resources.Passes.ResourceManager.GetString("Security_caves" + "_" + "ar");
        public SecuritycaveatesController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Securitycaveates";
            //set fontawsome icon for the controller 
			ViewBag.controllerIconClass = "fa fa-exclamation";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Passes.ResourceManager.GetString("Security_caves" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;



        }


		// this is main view to add,edit,delete and retrive caveates 
		public ActionResult Index()
		{
            //check if current user has view permession 
           
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    general.NotFound();

                }
            

           
            if(ViewBag.DEVELOPER == true)
            {
                //get force
                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.FORCE_ID = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true), "FORCE_ID", "FORCE_NAME_E");
                
                }
                else
                {
                    ViewBag.FORCE_ID = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true), "FORCE_ID", "FORCE_NAME_A");
                 

                }
                //list all unhiding security caveates 
                return View(db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).ToList());

            }
            else
            {
                //get force id
                int force = Convert.ToInt32(ViewBag.FORCE_TYPE_CODE);
                //get force
                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.FORCE_ID = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == force), "FORCE_ID", "FORCE_NAME_E");

                }
                else
                {
                    ViewBag.FORCE_ID = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == force), "FORCE_ID", "FORCE_NAME_A");


                }
                //list all unhiding security caveates 
                return View(db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == force).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).ToList());

            }




        }

        //add or edit new data
        public JsonResult SaveDataInDatabase(RA42_SECURITY_CAVEATES_DTL model, HttpPostedFileBase FILE_IMAGE)
        {

            var result = false;
            try
            {

                //check if user upload new file image 
                if (FILE_IMAGE != null)
                {
                    try
                    {


                        // Verify that the user selected a file
                        if (FILE_IMAGE != null && FILE_IMAGE.ContentLength > 0)
                        {
                            // extract only the filename with extention
                            string fileName = Path.GetFileNameWithoutExtension(FILE_IMAGE.FileName);
                            string extension = Path.GetExtension(FILE_IMAGE.FileName);


                            //check extention of the image 
                            if (general.CheckPersonalImage(FILE_IMAGE.FileName))
                            {

                                fileName = "Security_caveates" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                string path = Path.Combine(Server.MapPath("~/Files/Others/"), fileName);
                                model.FILE_IMAGE = fileName;
                                FILE_IMAGE.SaveAs(path);


                            }
                            else
                            {
                                //if the extention not supported show error message 
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

                //if the SECURITY_CAVEATES_CODE is > 0 that means this record needs update 
                if (model.SECURITY_CAVEATES_CODE > 0)
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
                    //check if record in the database
                    RA42_SECURITY_CAVEATES_DTL rA42_SECURITY_CAVEATES_DTL = db.RA42_SECURITY_CAVEATES_DTL.SingleOrDefault(x => x.DLT_STS != true && x.SECURITY_CAVEATES_CODE == model.SECURITY_CAVEATES_CODE);
                    if (model.FILE_IMAGE != null)
                    {
                        //if file image is equal undefined that means user not upload any image, so set it as its
                        if (model.FILE_IMAGE == "undefined")
                        {
                            rA42_SECURITY_CAVEATES_DTL.FILE_IMAGE = rA42_SECURITY_CAVEATES_DTL.FILE_IMAGE;
                        }
                        else
                        {
                            rA42_SECURITY_CAVEATES_DTL.FILE_IMAGE = model.FILE_IMAGE;
                        }
                    }
                    rA42_SECURITY_CAVEATES_DTL.SECURITY_CAVEATES_AR = model.SECURITY_CAVEATES_AR;
                    rA42_SECURITY_CAVEATES_DTL.SECURITY_CAVEATES_EN = model.SECURITY_CAVEATES_EN;
                    rA42_SECURITY_CAVEATES_DTL.FORCE_ID = model.FORCE_ID;
                    rA42_SECURITY_CAVEATES_DTL.UPD_BY = currentUser;
                    rA42_SECURITY_CAVEATES_DTL.UPD_DT = DateTime.Now;
                    rA42_SECURITY_CAVEATES_DTL.DLT_STS = false;
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
                    RA42_SECURITY_CAVEATES_DTL rA42_SECURITY_CAVEATES_DTL = new RA42_SECURITY_CAVEATES_DTL();
                    rA42_SECURITY_CAVEATES_DTL.SECURITY_CAVEATES_AR = model.SECURITY_CAVEATES_AR;
                    rA42_SECURITY_CAVEATES_DTL.SECURITY_CAVEATES_EN = model.SECURITY_CAVEATES_EN;
                    rA42_SECURITY_CAVEATES_DTL.FORCE_ID = model.FORCE_ID;

                    if (model.FILE_IMAGE != null)
                    {
                        //check if user upload new image
                        //if FILE_IMAGE = undefined that means user not upload any image 
                        //so set it as its
                        if (model.FILE_IMAGE == "undefined")
                        {
                            rA42_SECURITY_CAVEATES_DTL.FILE_IMAGE = rA42_SECURITY_CAVEATES_DTL.FILE_IMAGE;
                        }
                        else
                        {
                            rA42_SECURITY_CAVEATES_DTL.FILE_IMAGE = model.FILE_IMAGE;
                        }
                    }
                    rA42_SECURITY_CAVEATES_DTL.UPD_BY = currentUser;
                    rA42_SECURITY_CAVEATES_DTL.UPD_DT = DateTime.Now;
                    rA42_SECURITY_CAVEATES_DTL.CRD_BY = currentUser;
                    rA42_SECURITY_CAVEATES_DTL.CRD_DT = DateTime.Now;
                    rA42_SECURITY_CAVEATES_DTL.DLT_STS = false;
                    db.RA42_SECURITY_CAVEATES_DTL.Add(rA42_SECURITY_CAVEATES_DTL);
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
        //retrive data as json for specific record 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            var value = (from a in db.RA42_SECURITY_CAVEATES_DTL
                         where a.SECURITY_CAVEATES_CODE == Id
                         select new
                         {

                             a.SECURITY_CAVEATES_CODE,
                             a.SECURITY_CAVEATES_AR,
                             a.SECURITY_CAVEATES_EN,
                             a.RA42_FORCES_MST.FORCE_NAME_A,
                             a.RA42_FORCES_MST.FORCE_NAME_E,
                             a.FILE_IMAGE,
                             a.FORCE_ID,
                             a.CRD_BY,
                             a.CRD_DT,
                             a.UPD_BY,
                             a.UPD_DT


                         }).FirstOrDefault();

            //return new JsonResult() { Data = value, JsonRequestBehavior = JsonRequestBehavior.AllowGet, MaxJsonLength = Int32.MaxValue };
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        //this method to delete or hide specific record 
        public JsonResult DltRecordById(int Id)
        {
            //check deleting permession for the current user 
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
            //check if record is in the table then hide recor d
            RA42_SECURITY_CAVEATES_DTL rA42_SECURITY_CAVEATES_DTL = db.RA42_SECURITY_CAVEATES_DTL.Where(x => x.SECURITY_CAVEATES_CODE == Id).FirstOrDefault();
            if (rA42_SECURITY_CAVEATES_DTL != null)
            {
                rA42_SECURITY_CAVEATES_DTL.UPD_BY = currentUser;
                rA42_SECURITY_CAVEATES_DTL.UPD_DT = DateTime.Now;
                //hide record by set DLT_STS to true 
                rA42_SECURITY_CAVEATES_DTL.DLT_STS = true;
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
