using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Imaging;
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

//this controller for security wing leader
namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
	public class SliderdtlController : Controller
	{
        //get database connection
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        //toast identifier
        private IList<Toast> toasts = new List<Toast>();
        //get current user
        private string currentUser = (new UserInfo()).getSNO();
        //get some function from GeneralFunctions class
        private GeneralFunctions general = new GeneralFunctions();

        private string title = Resources.Settings.ResourceManager.GetString("slidr_images" + "_" + "ar");

        public SliderdtlController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Sliderdtl";

			 ViewBag.controllerIconClass = "fa fa-images";
            if(Language.GetCurrentLang() == "en"){
                title = Resources.Settings.ResourceManager.GetString("slidr_images" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;

          
        }


        // view signatuers
        public ActionResult Index()
        {
            
                //check developer permessions
                if (ViewBag.DEVELOPER != true)
                {
                    general.NotFound();

                }
            
               
                return View(db.RA42_SLIDER_DTL.Where(a=>a.DLT_STS !=true).OrderByDescending(a => a.SLIDER_ID).ToList());
            

            
            
            
            
        }

        public JsonResult SaveDataInDatabase(RA42_SLIDER_DTL model, HttpPostedFileBase SLIDER_IMAGE)
        {
            var result = false;
            try
            {
               
                if (SLIDER_IMAGE != null)
                {
                    try
                    {


                        // Verify that the user selected a file
                        if (SLIDER_IMAGE != null && SLIDER_IMAGE.ContentLength > 0)
                        {
                            // extract only the filename with extention
                            string fileName = Path.GetFileNameWithoutExtension(SLIDER_IMAGE.FileName);
                            string extension = Path.GetExtension(SLIDER_IMAGE.FileName);

                           
                            //check extention of image file 
                            if (general.CheckFileType(SLIDER_IMAGE.FileName))
                            {

                                fileName = "slider_image" + DateTime.Now.ToString("yymmssfff") + extension;

                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeSliderImage(SLIDER_IMAGE, fileName);

                                if (check != true)
                                {
                                   

                                //SIGNATURE_IMAGE.SaveAs(path);

                           
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                    TempData["Erorr"] = " image should be in image format - يجب أن تكون الصورة  ملف (PNG,JPG,GIF)";
                                    return Json(false, JsonRequestBehavior.AllowGet);
                            }
                                model.SLIDER_IMAGE = fileName;
                            }
                            }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }

                


                if (model.SLIDER_ID > 0)
                {
                   
                        //check developer permessions
                        if (ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_up"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }

                    
                    RA42_SLIDER_DTL rA42_SLIDER_DTL = db.RA42_SLIDER_DTL.SingleOrDefault(x => x.SLIDER_ID == model.SLIDER_ID);

                    rA42_SLIDER_DTL.SLIDER_TITLE_A = model.SLIDER_TITLE_A;
                    rA42_SLIDER_DTL.SLIDER_TITLE_E = model.SLIDER_TITLE_E;
                    if (model.SLIDER_IMAGE.Equals("undefined"))
                    {
                        rA42_SLIDER_DTL.SLIDER_IMAGE = rA42_SLIDER_DTL.SLIDER_IMAGE;

                    }
                    else
                    {
                        rA42_SLIDER_DTL.SLIDER_IMAGE = model.SLIDER_IMAGE;
                    }
                    rA42_SLIDER_DTL.DESCRIPTION_A = model.DESCRIPTION_A;
                    rA42_SLIDER_DTL.DESCRIPTION_E = model.DESCRIPTION_E;
                    rA42_SLIDER_DTL.UPD_BY = currentUser;
                    rA42_SLIDER_DTL.UPD_DT = DateTime.Now;
                    rA42_SLIDER_DTL.DLT_STS = false;
                    db.SaveChanges();
                    result = true;
                    AddToast(new Toast("",
                   GetResourcesValue("success_update_message"),
                   "green"));
                }
                else
                {
                   
                        //check developer permessions
                        if ( ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_add"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }

                    if (SLIDER_IMAGE == null || SLIDER_IMAGE.Equals("undefined"))
                    {
                       TempData["Erorr"] = "لم تقم برفع صوره";
                       return Json(false, JsonRequestBehavior.AllowGet);
                    }
                    //add new slider  
                    RA42_SLIDER_DTL rA42_SLIDER_DTL = new RA42_SLIDER_DTL();
                    rA42_SLIDER_DTL.SLIDER_TITLE_A = model.SLIDER_TITLE_A;
                    rA42_SLIDER_DTL.SLIDER_TITLE_E = model.SLIDER_TITLE_E;
                    rA42_SLIDER_DTL.SLIDER_IMAGE = model.SLIDER_IMAGE;
                    rA42_SLIDER_DTL.DESCRIPTION_A = model.DESCRIPTION_A;
                    rA42_SLIDER_DTL.DESCRIPTION_E = model.DESCRIPTION_E;
                    rA42_SLIDER_DTL.UPD_BY = currentUser;
                    rA42_SLIDER_DTL.UPD_DT = DateTime.Now;
                    rA42_SLIDER_DTL.CRD_BY = currentUser;
                    rA42_SLIDER_DTL.CRD_DT = DateTime.Now;
                    rA42_SLIDER_DTL.DLT_STS = false;
                    db.RA42_SLIDER_DTL.Add(rA42_SLIDER_DTL);
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
                TempData["Erorr"] =ex.GetBaseException();
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        //get signature details by json array
        public JsonResult GetObjectById(int Id)
        {
            //db.Configuration.ProxyCreationEnabled = false;

            var value = (from a in db.RA42_SLIDER_DTL
                        
                         where a.SLIDER_ID == Id
                         select new
                         {

                             a.SLIDER_ID,
                             a.SLIDER_IMAGE,
                             a.SLIDER_TITLE_A,
                             a.SLIDER_TITLE_E,
                             a.DESCRIPTION_A,
                             a.DESCRIPTION_E,
                             a.CRD_BY,
                             a.CRD_DT,
                             a.UPD_BY,
                             a.UPD_DT


                         }).FirstOrDefault();

            //return new JsonResult() { Data = value, JsonRequestBehavior = JsonRequestBehavior.AllowGet, MaxJsonLength = Int32.MaxValue };
            return Json(value, JsonRequestBehavior.AllowGet);
        }

      //delete or record 
        public JsonResult DltRecordById(int Id)
        {
         
                //check developer permessions
                if (ViewBag.DEVELOPER != true)
                {
                    AddToast(new Toast("",
                  GetResourcesValue("Dont_have_permissions_to_dlt"),
                  "red"));
                    return Json(false, JsonRequestBehavior.AllowGet);
                }

            
            bool result = false;
            RA42_SLIDER_DTL ra42_SLIDER_DTL = db.RA42_SLIDER_DTL.Where(x => x.SLIDER_ID == Id).FirstOrDefault();
            if (ra42_SLIDER_DTL != null)
            {
                ra42_SLIDER_DTL.UPD_BY = currentUser;
                ra42_SLIDER_DTL.UPD_DT = DateTime.Now;
                ra42_SLIDER_DTL.DLT_STS = true;
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