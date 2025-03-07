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
    //this controller is special for Security ground & defense directorate manager 
    //developer and admin can controll this controller also
	public class ManagersignaturemstController : Controller
	{
        //get db connection 
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        private GeneralFunctions general = new GeneralFunctions();
        //set controller title 
        private string title = Resources.Settings.ResourceManager.GetString("Managersign" + "_" + "ar");

        public ManagersignaturemstController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Managersignaturemst";
            //set fontawsome icon for this controller 
			 ViewBag.controllerIconClass = "fa fa-signature";
            if(Language.GetCurrentLang() == "en"){
                title = Resources.Settings.ResourceManager.GetString("Managersign" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;

            
        }


        //this is main view to add, edit,retrive,delete manager signature 
        public ActionResult Index()
        {
            //check view permession for current user 
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    //if no authority, check if user is directorate manager by workflow id type 
                    if (ViewBag.workflowIDType != 6)
                    {
                        general.NotFound();
                    }
                }
            
            ///get user details from api 
            MilitaryInformationController militaryInfo = new MilitaryInformationController();
            ViewBag.NamesBySrvNo = new Func<string, string>(militaryInfo.GetNamesByServiceNumber);


            //get signatures 
            return View(db.RA42_MANAGER_SIGNATURE_MST.Where(a => a.DLT_STS !=true).OrderByDescending(a => a.MANAGER_SIGN_CODE).ToList());

            
            
        }
        //save and edit date 
        public JsonResult SaveDataInDatabase(RA42_MANAGER_SIGNATURE_MST model, HttpPostedFileBase SIGNATURE_IMAGE)
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

                                fileName = "manager_signature" + DateTime.Now.ToString("yymmssfff") + extension;

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
                    //convert canvas drowing to image 
                        string fileName2 = "manager_signature" + DateTime.Now.ToString("yymmssfff") + ".png";
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
                        TempData["Pending"] = "قمت بالتوقيع بالنجاح";

                    }
                }
                    catch (Exception exc)
                    {
                    TempData["Pending"] = "قمت برفع ملف توقيع بنجاح";

                }
                //if mode MANAGER_SIG_CODE > 0 that means this record need update 
                if (model.MANAGER_SIGN_CODE > 0)
                {
                    //check if current user has update permession 
                    if (ViewBag.UP != true)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            //if no authority, check current user if he is manager of directorate by check workflow id type 
                            if (ViewBag.workflowIDType != 6)
                            {
                                AddToast(new Toast("",
                              GetResourcesValue("Dont_have_permissions_to_up"),
                              "red"));
                                return Json(false, JsonRequestBehavior.AllowGet);
                            }
                        }

                    }
                    //check if record is in the db 
                    RA42_MANAGER_SIGNATURE_MST rA42_AUTHORAIZATION_SIGNAURE_MST = db.RA42_MANAGER_SIGNATURE_MST.SingleOrDefault(x => x.MANAGER_SIGN_CODE == model.MANAGER_SIGN_CODE);
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
                    //check if current user has add permession 

                    if (ViewBag.AD != true)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            //if no authority, check current user if he is manager of directorate by check workflow id type 

                            if (ViewBag.workflowIDType != 6)
                            {
                                AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_add"),
                          "red"));
                                return Json(false, JsonRequestBehavior.AllowGet);
                            }
                           
                        }

                    }
                    //add new signature 
                    RA42_MANAGER_SIGNATURE_MST rA42_AUTHORAIZATION_SIGNAURE_MST = new RA42_MANAGER_SIGNATURE_MST();
                    rA42_AUTHORAIZATION_SIGNAURE_MST.SIGNATURE = model.SIGNATURE;
                    rA42_AUTHORAIZATION_SIGNAURE_MST.UPD_BY = currentUser;
                    rA42_AUTHORAIZATION_SIGNAURE_MST.UPD_DT = DateTime.Now;
                    rA42_AUTHORAIZATION_SIGNAURE_MST.CRD_BY = currentUser;
                    rA42_AUTHORAIZATION_SIGNAURE_MST.CRD_DT = DateTime.Now;
                    rA42_AUTHORAIZATION_SIGNAURE_MST.DLT_STS = false;
                    db.RA42_MANAGER_SIGNATURE_MST.Add(rA42_AUTHORAIZATION_SIGNAURE_MST);
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
        //get record data as json result 
        public JsonResult GetObjectById(int Id)
        {
            //db.Configuration.ProxyCreationEnabled = false;

            var value = (from a in db.RA42_MANAGER_SIGNATURE_MST
                         where a.MANAGER_SIGN_CODE == Id
                         select new
                         {

                             a.MANAGER_SIGN_CODE,
                             a.SIGNATURE,
                             a.CRD_BY,
                             a.CRD_DT,
                             a.UPD_BY,
                             a.UPD_DT


                         }).FirstOrDefault();

            //return new JsonResult() { Data = value, JsonRequestBehavior = JsonRequestBehavior.AllowGet, MaxJsonLength = Int32.MaxValue };
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        //delete signature
        public JsonResult DltRecordById(int Id)
        {
            //check if current user has deleting permession 
            if (ViewBag.DLT != true)
            {
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    //if no authority, check current user if he is manager of directorate by check workflow id type 
                    if (ViewBag.workflowIDType != 6)
                    {
                        AddToast(new Toast("",
                  GetResourcesValue("Dont_have_permissions_to_dlt"),
                  "red"));
                        return Json(false, JsonRequestBehavior.AllowGet);
                    }
                }

            }
            bool result = false;
            RA42_MANAGER_SIGNATURE_MST rA42_AUTHORAIZATION_SIGNAURE_MST = db.RA42_MANAGER_SIGNATURE_MST.Where(x => x.MANAGER_SIGN_CODE == Id).FirstOrDefault();
            if (rA42_AUTHORAIZATION_SIGNAURE_MST != null)
            {
                rA42_AUTHORAIZATION_SIGNAURE_MST.UPD_BY = currentUser;
                rA42_AUTHORAIZATION_SIGNAURE_MST.UPD_DT = DateTime.Now;
                //hide record by set DLT_STS to true 
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