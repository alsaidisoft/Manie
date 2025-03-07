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
	public class AuthosignaturemstController : Controller
	{
        //get database connection
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        //toast identifier
        private IList<Toast> toasts = new List<Toast>();
        //get current user
        private string currentUser = (new UserInfo()).getSNO();
        //get some function from GeneralFunctions class
        private GeneralFunctions general = new GeneralFunctions();

        private string title = Resources.Settings.ResourceManager.GetString("autho_signature" + "_" + "ar");

        public AuthosignaturemstController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Authosignaturemst";

			 ViewBag.controllerIconClass = "fa fa-file-signature";
            if(Language.GetCurrentLang() == "en"){
                title = Resources.Settings.ResourceManager.GetString("autho_signature" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;

          
        }


        // view signatuers
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


            //check developer permessions
            if (ViewBag.DEVELOPER == true)
            {
                if (Language.GetCurrentLang() == "en")
                {
                    // if current user is administrator show all station in english
                    ViewBag.STATIONS_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a=>a.DLT_STS !=true), "STATION_CODE", "STATION_NAME_E");

                }
                else
                {
                    // if current user is administrator show all station in arabic
                    ViewBag.STATIONS_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a=>a.DLT_STS !=true), "STATION_CODE", "STATION_NAME_A");

                }
                return View(db.RA42_AUTHORAIZATION_SIGNAURE_MST.Where(a=>a.DLT_STS !=true).OrderByDescending(a => a.AUTHO_SIGNATURE_CODE).ToList());
            }

            if (ViewBag.ADMIN == true)
            {
                //get currnt user unitcode    
                int unit = Convert.ToInt32(ViewBag.FORCE_TYPE_CODE);
                if (Language.GetCurrentLang() == "en")
                {
                    // if current user is administrator show all station in english
                    ViewBag.STATIONS_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a=>a.FORCE_ID == unit && a.DLT_STS !=true), "STATION_CODE", "STATION_NAME_E");

                }
                else
                {
                    // if current user is administrator show all station in arabic
                    ViewBag.STATIONS_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a=>a.FORCE_ID == unit && a.DLT_STS !=true), "STATION_CODE", "STATION_NAME_A");

                }
                return View(db.RA42_AUTHORAIZATION_SIGNAURE_MST.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.FORCE_ID == unit).OrderByDescending(a => a.AUTHO_SIGNATURE_CODE).ToList());
            }
            
                //get currnt user unitcode    
                int station = Convert.ToInt32(ViewBag.STATION_CODE_TYPE);
                
                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.STATIONS_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == station && a.DLT_STS !=true), "STATION_CODE", "STATION_NAME_E");
                    
                }
                else
                {
                    ViewBag.STATIONS_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == station && a.DLT_STS !=true), "STATION_CODE", "STATION_NAME_A");
                   

                }
               
                return View(db.RA42_AUTHORAIZATION_SIGNAURE_MST.Where(a => a.STATION_ID == station && a.DLT_STS !=true).OrderByDescending(a => a.AUTHO_SIGNATURE_CODE).ToList());

            
            
        }

        public JsonResult SaveDataInDatabase(RA42_AUTHORAIZATION_SIGNAURE_MST model, HttpPostedFileBase SIGNATURE_IMAGE)
        {
            var result = false;
            try
            {

                if (SIGNATURE_IMAGE != null)
                {
                    try
                    {


                        // Verify that the user selected a file
                        if (SIGNATURE_IMAGE != null && SIGNATURE_IMAGE.ContentLength > 0)
                        {
                            // extract only the filename with extention
                            string fileName = Path.GetFileNameWithoutExtension(SIGNATURE_IMAGE.FileName);
                            string extension = Path.GetExtension(SIGNATURE_IMAGE.FileName);

                           
                            //check extention of image file 
                            if (general.CheckFileType(SIGNATURE_IMAGE.FileName))
                            {

                                fileName = "authorization_signature" + DateTime.Now.ToString("yymmssfff") + extension;

                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeSignature(SIGNATURE_IMAGE, fileName);

                                if (check != true)
                                {
                                   

                                //SIGNATURE_IMAGE.SaveAs(path);

                           
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                    TempData["Erorr"] = " image should be in image format - يجب أن تكون الصورة  ملف (PNG,JPG,GIF)";
                                    return Json(false, JsonRequestBehavior.AllowGet);
                            }
                                model.SIGNATURE = fileName;
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
                   
                        //upload signature as png image 
                        string fileName2 = "authorization_signature" + DateTime.Now.ToString("yymmssfff") + ".png";
                        //save it in signatures folder
                        string dir = Path.Combine(Server.MapPath("~/Files/Signatures"), fileName2);
                        using (FileStream fs = new FileStream(dir, FileMode.Create))
                        {
                            using (BinaryWriter bw = new BinaryWriter(fs))
                            {
                                byte[] data = Convert.FromBase64String(model.SIGNATURE);
                                bw.Write(data);
                                bw.Close();

                            }


                            fs.Close();
                            model.SIGNATURE = fileName2;
                            TempData["Success"] = "قمت بالتوقيع بالنجاح";

                    }
                }
                
                catch (Exception exc)
                {
                    TempData["Erorr"] = exc.GetBaseException();
                }


                if (model.AUTHO_SIGNATURE_CODE > 0)
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
                    RA42_AUTHORAIZATION_SIGNAURE_MST rA42_AUTHORAIZATION_SIGNAURE_MST = db.RA42_AUTHORAIZATION_SIGNAURE_MST.SingleOrDefault(x => x.AUTHO_SIGNATURE_CODE == model.AUTHO_SIGNATURE_CODE);
                   
                    rA42_AUTHORAIZATION_SIGNAURE_MST.STATION_ID = model.STATION_ID;
                    if (model.SIGNATURE != null)
                    {
                        if (model.SIGNATURE == null) { 
                            rA42_AUTHORAIZATION_SIGNAURE_MST.SIGNATURE = rA42_AUTHORAIZATION_SIGNAURE_MST.SIGNATURE;
                        }
                        else
                        {
                            rA42_AUTHORAIZATION_SIGNAURE_MST.SIGNATURE = model.SIGNATURE;
                        }
                    }

                    rA42_AUTHORAIZATION_SIGNAURE_MST.UPD_BY = currentUser;
                    rA42_AUTHORAIZATION_SIGNAURE_MST.UPD_DT = DateTime.Now;
                    rA42_AUTHORAIZATION_SIGNAURE_MST.DLT_STS = false;
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
                    //add new signature  
                    RA42_AUTHORAIZATION_SIGNAURE_MST rA42_AUTHORAIZATION_SIGNAURE_MST = new RA42_AUTHORAIZATION_SIGNAURE_MST();
                    rA42_AUTHORAIZATION_SIGNAURE_MST.STATION_ID = model.STATION_ID;
                    rA42_AUTHORAIZATION_SIGNAURE_MST.SIGNATURE = model.SIGNATURE;
                    rA42_AUTHORAIZATION_SIGNAURE_MST.UPD_BY = currentUser;
                    rA42_AUTHORAIZATION_SIGNAURE_MST.UPD_DT = DateTime.Now;
                    rA42_AUTHORAIZATION_SIGNAURE_MST.CRD_BY = currentUser;
                    rA42_AUTHORAIZATION_SIGNAURE_MST.CRD_DT = DateTime.Now;
                    rA42_AUTHORAIZATION_SIGNAURE_MST.DLT_STS = false;
                    db.RA42_AUTHORAIZATION_SIGNAURE_MST.Add(rA42_AUTHORAIZATION_SIGNAURE_MST);
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

            var value = (from a in db.RA42_AUTHORAIZATION_SIGNAURE_MST
                         join b in db.RA42_STATIONS_MST on a.STATION_ID equals b.STATION_CODE
                         where a.AUTHO_SIGNATURE_CODE == Id
                         select new
                         {

                             a.AUTHO_SIGNATURE_CODE,
                             a.SIGNATURE,
                             a.STATION_ID,
                             b.STATION_NAME_A,
                             b.STATION_NAME_E,
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
            //check delete permession
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
            RA42_AUTHORAIZATION_SIGNAURE_MST rA42_AUTHORAIZATION_SIGNAURE_MST = db.RA42_AUTHORAIZATION_SIGNAURE_MST.Where(x => x.AUTHO_SIGNATURE_CODE == Id).FirstOrDefault();
            if (rA42_AUTHORAIZATION_SIGNAURE_MST != null)
            {
                rA42_AUTHORAIZATION_SIGNAURE_MST.UPD_BY = currentUser;
                rA42_AUTHORAIZATION_SIGNAURE_MST.UPD_DT = DateTime.Now;
                rA42_AUTHORAIZATION_SIGNAURE_MST.DLT_STS = true;
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