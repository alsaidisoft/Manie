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
using portal.Controllers;
using SecurityClearanceWebApp.Models;
using SecurityClearanceWebApp.Util;

namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    //this controller to detrmine documents types for any permit type 
    //this controller is for developer only
    public class FiletypemstController : Controller
    {
        //get db connection
        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class 
        private GeneralFunctions general = new GeneralFunctions();
        private string title = Resources.Settings.ResourceManager.GetString("file_type" + "_" + "ar");

        public FiletypemstController()
        {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Filetypemst";
            //set fontawsome icon for this controller views 
            ViewBag.controllerIconClass = "fa fa-file-pdf";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("file_type" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;
            
        }


        //index view will has all operations, add - edit - delete - retrive
        public ActionResult Index()
        {
            if (ViewBag.DEVELOPER != true)
            {
                general.NotFound();

            }

            if (Language.GetCurrentLang() == "en")
            {
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a=>a.DLT_STS !=true), "ACCESS_TYPE_CODE", "ACCESS_TYPE_E");
                ViewBag.FORCE_ID = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true), "FORCE_ID", "FORCE_NAME_E");
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.DLT_STS != true), "CARD_FOR_CODE", "CARD_FOR_E");

            }
            else
            {
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true), "ACCESS_TYPE_CODE", "ACCESS_TYPE");
                ViewBag.FORCE_ID = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true), "FORCE_ID", "FORCE_NAME_A");
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.DLT_STS != true), "CARD_FOR_CODE", "CARD_FOR_A");

            }
            ViewBag.FILE_NOTE_CODE = new SelectList(db.RA42_FILE_NOTE_TYPE_MST.Where(a => a.DLT_STS != true), "FILE_NOTE_CODE", "FILE_NOTE_TYPE");

            return View(db.RA42_FILE_TYPE_MST.Where(a => a.DLT_STS != true).Include(a=>a.RA42_DOCUMENTS_ACCESS_MST).OrderByDescending(a=>a.FILE_TYPE_CODE).ToList());
        }
        //add or edit data
        public JsonResult SaveDataInDatabase(RA42_FILE_TYPE_MST model)
        {

            var result = false;
            try
            {
                //if model FILE_TYPE_CODE > 0 that means this record need update 
                if (model.FILE_TYPE_CODE > 0)
                {
                    //check update permession
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
                    //check if record is in RA42_FILE_TYPE_MST table 
                    RA42_FILE_TYPE_MST rA42_FILE_TYPE_MST = db.RA42_FILE_TYPE_MST.SingleOrDefault(x => x.DLT_STS != true && x.FILE_TYPE_CODE == model.FILE_TYPE_CODE);
                    //edit file types details
                    rA42_FILE_TYPE_MST.FILE_NOTE_CODE = model.FILE_NOTE_CODE;
                    rA42_FILE_TYPE_MST.FILE_TYPE = model.FILE_TYPE;
                    rA42_FILE_TYPE_MST.FILE_TYPE_E = model.FILE_TYPE_E;
                    rA42_FILE_TYPE_MST.REMARKS = model.REMARKS;
                    rA42_FILE_TYPE_MST.UPD_BY = currentUser;
                    rA42_FILE_TYPE_MST.UPD_DT = DateTime.Now;
                    rA42_FILE_TYPE_MST.DLT_STS = false;
                    db.SaveChanges();
                    //add access to the document
                    List<int> list = model.ACCESS_TYPE_CODE.ToList();
                    List<int> list1 = model.FORCE_ID.ToList();
                    List<int> list2 = model.CARD_FOR_CODE.ToList();
                    if (list != null)
                    {
                        //delete previues access related to this record 
                        var delete = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.FILE_TYPE_CODE == model.FILE_TYPE_CODE).ToList();

                        if (delete != null)
                        {
                            foreach (var item in delete)
                            {
                                item.ACCESS_TYPE_CODE = item.ACCESS_TYPE_CODE;
                                item.FILE_TYPE_CODE = item.FILE_TYPE_CODE;
                                item.CRD_BY = item.CRD_BY;
                                item.CRD_DT = item.CRD_DT;
                                item.UPD_BY = currentUser;
                                item.UPD_DT = DateTime.Now;
                                item.DLT_STS = true;
                                db.Entry(item).State = EntityState.Modified;

                            }
                        }
                        //now add new access 
                        RA42_DOCUMENTS_ACCESS_MST rA42_DOCUMENTS_ACCESS_MST = new RA42_DOCUMENTS_ACCESS_MST();
                        for (int i = 0; i < list.Count; i++)
                        {
                            rA42_DOCUMENTS_ACCESS_MST.ACCESS_TYPE_CODE = list[i];
                            //rA42_DOCUMENTS_ACCESS_MST.FORCE_ID = list1[i];
                            rA42_DOCUMENTS_ACCESS_MST.FILE_TYPE_CODE = model.FILE_TYPE_CODE;
                            rA42_DOCUMENTS_ACCESS_MST.CRD_BY = currentUser;
                            rA42_DOCUMENTS_ACCESS_MST.CRD_DT = DateTime.Now;
                            rA42_DOCUMENTS_ACCESS_MST.UPD_BY = currentUser;
                            rA42_DOCUMENTS_ACCESS_MST.UPD_DT = DateTime.Now;
                            db.RA42_DOCUMENTS_ACCESS_MST.Add(rA42_DOCUMENTS_ACCESS_MST);
                            db.SaveChanges();

                        }
                    }

                    if (list1 != null)
                    {
                        //delete previues force related to this record 
                        var delete = db.RA42_FILE_FORCES_MST.Where(a => a.FILE_TYPE_CODE == model.FILE_TYPE_CODE).ToList();

                        if (delete != null)
                        {
                            foreach (var item in delete)
                            {
                                item.FORCE_ID = item.FORCE_ID;
                                item.FILE_TYPE_CODE = item.FILE_TYPE_CODE;
                                item.CRD_BY = item.CRD_BY;
                                item.CRD_DT = item.CRD_DT;
                                item.UPD_BY = currentUser;
                                item.UPD_DT = DateTime.Now;
                                item.DLT_STS = true;
                                db.Entry(item).State = EntityState.Modified;

                            }
                        }
                        //now add new forces 
                        RA42_FILE_FORCES_MST rA42_FILE_FORCES_MST = new RA42_FILE_FORCES_MST();
                        for (int i = 0; i < list1.Count; i++)
                        {
                            rA42_FILE_FORCES_MST.FORCE_ID = list1[i];
                            rA42_FILE_FORCES_MST.FILE_TYPE_CODE = model.FILE_TYPE_CODE;
                            rA42_FILE_FORCES_MST.CRD_BY = currentUser;
                            rA42_FILE_FORCES_MST.CRD_DT = DateTime.Now;
                            rA42_FILE_FORCES_MST.UPD_BY = currentUser;
                            rA42_FILE_FORCES_MST.UPD_DT = DateTime.Now;
                            db.RA42_FILE_FORCES_MST.Add(rA42_FILE_FORCES_MST);
                            db.SaveChanges();
                        }
                    }

                    if (list2 != null)
                    {
                        //delete previues card_for related to this record 
                        var delete = db.RA42_FILE_CARD_MST.Where(a => a.FILE_TYPE_CODE == model.FILE_TYPE_CODE).ToList();

                        if (delete != null)
                        {
                            foreach (var item in delete)
                            {
                                item.CARD_FOR_CODE = item.CARD_FOR_CODE;
                                item.FILE_TYPE_CODE = item.FILE_TYPE_CODE;
                                item.CRD_BY = item.CRD_BY;
                                item.CRD_DT = item.CRD_DT;
                                item.UPD_BY = currentUser;
                                item.UPD_DT = DateTime.Now;
                                item.DLT_STS = true;
                                db.Entry(item).State = EntityState.Modified;

                            }
                        }
                        //now add new card_for 
                        RA42_FILE_CARD_MST rA42_FILE_CARD_MST = new RA42_FILE_CARD_MST();
                        for (int i = 0; i < list2.Count; i++)
                        {
                            rA42_FILE_CARD_MST.CARD_FOR_CODE = list2[i];
                            rA42_FILE_CARD_MST.FILE_TYPE_CODE = model.FILE_TYPE_CODE;
                            rA42_FILE_CARD_MST.CRD_BY = currentUser;
                            rA42_FILE_CARD_MST.CRD_DT = DateTime.Now;
                            rA42_FILE_CARD_MST.UPD_BY = currentUser;
                            rA42_FILE_CARD_MST.UPD_DT = DateTime.Now;
                            db.RA42_FILE_CARD_MST.Add(rA42_FILE_CARD_MST);
                            db.SaveChanges();
                        }
                    }
                    result = true;
                    AddToast(new Toast("",
                   GetResourcesValue("success_update_message"),
                   "green"));
                }
                else
                {
                    //check if user has add permession
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
                    //now add new document type details
                    RA42_FILE_TYPE_MST rA42_FILE_TYPE_MST = new RA42_FILE_TYPE_MST();
                    rA42_FILE_TYPE_MST.FILE_NOTE_CODE = model.FILE_NOTE_CODE;
                    rA42_FILE_TYPE_MST.FILE_TYPE = model.FILE_TYPE;
                    rA42_FILE_TYPE_MST.FILE_TYPE_E = model.FILE_TYPE_E;
                    rA42_FILE_TYPE_MST.REMARKS = model.REMARKS;
                    rA42_FILE_TYPE_MST.UPD_BY = currentUser;
                    rA42_FILE_TYPE_MST.UPD_DT = DateTime.Now;
                    rA42_FILE_TYPE_MST.CRD_BY = currentUser;
                    rA42_FILE_TYPE_MST.CRD_DT = DateTime.Now;
                    rA42_FILE_TYPE_MST.DLT_STS = false;
                    db.RA42_FILE_TYPE_MST.Add(rA42_FILE_TYPE_MST);
                    db.SaveChanges();

                    List<int> list = model.ACCESS_TYPE_CODE.ToList();
                    List<int> list1 = model.FORCE_ID.ToList();
                    List<int> list2 = model.CARD_FOR_CODE.ToList();
                    //this section not important but i but it in case edit section not completed 
                    if (list != null)
                    {
                        var delete = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.FILE_TYPE_CODE == rA42_FILE_TYPE_MST.FILE_TYPE_CODE).ToList();

                        if (delete != null)
                        {
                            foreach (var item in delete)
                            {
                                item.ACCESS_TYPE_CODE = item.ACCESS_TYPE_CODE;
                                item.FILE_TYPE_CODE = item.FILE_TYPE_CODE;
                                item.CRD_BY = item.CRD_BY;
                                item.CRD_DT = item.CRD_DT;
                                item.UPD_BY = currentUser;
                                item.UPD_DT = DateTime.Now;
                                item.DLT_STS = true;
                                db.Entry(item).State = EntityState.Modified;

                            }
                        }
                        //this section to attach new access to the document type 
                        RA42_DOCUMENTS_ACCESS_MST rA42_DOCUMENTS_ACCESS_MST = new RA42_DOCUMENTS_ACCESS_MST();
                        for (int i = 0; i < list.Count; i++)
                        {
                            rA42_DOCUMENTS_ACCESS_MST.ACCESS_TYPE_CODE = list[i];
                            // rA42_DOCUMENTS_ACCESS_MST.FORCE_ID = list1[i];
                            rA42_DOCUMENTS_ACCESS_MST.FILE_TYPE_CODE = rA42_FILE_TYPE_MST.FILE_TYPE_CODE;
                            rA42_DOCUMENTS_ACCESS_MST.CRD_BY = currentUser;
                            rA42_DOCUMENTS_ACCESS_MST.CRD_DT = DateTime.Now;
                            rA42_DOCUMENTS_ACCESS_MST.UPD_BY = currentUser;
                            rA42_DOCUMENTS_ACCESS_MST.UPD_DT = DateTime.Now;
                            db.RA42_DOCUMENTS_ACCESS_MST.Add(rA42_DOCUMENTS_ACCESS_MST);
                            db.SaveChanges();
                        }
                    }

                    if (list1 != null)
                    {
                        var delete = db.RA42_FILE_FORCES_MST.Where(a => a.FILE_TYPE_CODE == rA42_FILE_TYPE_MST.FILE_TYPE_CODE).ToList();

                        if (delete != null)
                        {
                            foreach (var item in delete)
                            {
                                item.FORCE_ID = item.FORCE_ID;
                                item.FILE_TYPE_CODE = item.FILE_TYPE_CODE;
                                item.CRD_BY = item.CRD_BY;
                                item.CRD_DT = item.CRD_DT;
                                item.UPD_BY = currentUser;
                                item.UPD_DT = DateTime.Now;
                                item.DLT_STS = true;
                                db.Entry(item).State = EntityState.Modified;

                            }
                        }
                        //this section to insert new force 
                        RA42_FILE_FORCES_MST rA42_FILE_FORCES_MST = new RA42_FILE_FORCES_MST();
                        for (int i = 0; i < list1.Count; i++)
                        {
                            rA42_FILE_FORCES_MST.FORCE_ID = list1[i];
                            rA42_FILE_FORCES_MST.FILE_TYPE_CODE = rA42_FILE_TYPE_MST.FILE_TYPE_CODE;
                            rA42_FILE_FORCES_MST.CRD_BY = currentUser;
                            rA42_FILE_FORCES_MST.CRD_DT = DateTime.Now;
                            rA42_FILE_FORCES_MST.UPD_BY = currentUser;
                            rA42_FILE_FORCES_MST.UPD_DT = DateTime.Now;
                            db.RA42_FILE_FORCES_MST.Add(rA42_FILE_FORCES_MST);
                            db.SaveChanges();
                        }
                    }
                    if (list2 != null)
                    {
                        ////delete previues card_for related to this record 
                        var delete = db.RA42_FILE_CARD_MST.Where(a => a.FILE_TYPE_CODE == model.FILE_TYPE_CODE).ToList();

                        if (delete != null)
                        {
                            foreach (var item in delete)
                            {
                                item.CARD_FOR_CODE = item.CARD_FOR_CODE;
                                item.FILE_TYPE_CODE = item.FILE_TYPE_CODE;
                                item.CRD_BY = item.CRD_BY;
                                item.CRD_DT = item.CRD_DT;
                                item.UPD_BY = currentUser;
                                item.UPD_DT = DateTime.Now;
                                item.DLT_STS = true;
                                db.Entry(item).State = EntityState.Modified;

                            }
                        }
                        //now add new card_for 
                        RA42_FILE_CARD_MST rA42_FILE_CARD_MST = new RA42_FILE_CARD_MST();
                        for (int i = 0; i < list2.Count; i++)
                        {
                            rA42_FILE_CARD_MST.CARD_FOR_CODE = list2[i];
                            rA42_FILE_CARD_MST.FILE_TYPE_CODE = rA42_FILE_TYPE_MST.FILE_TYPE_CODE;
                            rA42_FILE_CARD_MST.CRD_BY = currentUser;
                            rA42_FILE_CARD_MST.CRD_DT = DateTime.Now;
                            rA42_FILE_CARD_MST.UPD_BY = currentUser;
                            rA42_FILE_CARD_MST.UPD_DT = DateTime.Now;
                            db.RA42_FILE_CARD_MST.Add(rA42_FILE_CARD_MST);
                            db.SaveChanges();
                        }
                    }
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
                throw ex.GetBaseException();
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }
        //get document type details as json result 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var value = db.RA42_FILE_TYPE_MST.Where(a => a.FILE_TYPE_CODE == Id).FirstOrDefault();
            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(value, JsonRequestBehavior.AllowGet);
        }

       //delete any record 
        public JsonResult DltRecordById(int Id)
        {
            //check if current user has authority to delete any record 
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
            RA42_FILE_TYPE_MST rA42_FILE_TYPE_MST = db.RA42_FILE_TYPE_MST.Where(x => x.FILE_TYPE_CODE == Id).FirstOrDefault();
            if (rA42_FILE_TYPE_MST != null)
            {
                rA42_FILE_TYPE_MST.UPD_BY = currentUser;
                rA42_FILE_TYPE_MST.UPD_DT = DateTime.Now;
                //we don't delete record actully we hide record by set DLT_STS to true
                rA42_FILE_TYPE_MST.DLT_STS = true;
                db.SaveChanges();
                result = true;
                AddToast(new Toast("",
                   GetResourcesValue("success_delete_message"),
                   "green"));
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }
        //get access types 
        [HttpGet]
        public JsonResult GetSelectdAccess(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            if (Language.GetCurrentLang() == "en")
            {
                var v = (from a in db.RA42_DOCUMENTS_ACCESS_MST

                         join b in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals b.FILE_TYPE_CODE
                         where a.FILE_TYPE_CODE == Id
                         join c in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals c.ACCESS_TYPE_CODE
                         where a.DLT_STS != true
                         select new
                         {
                             a.ACCESS_TYPE_CODE,
                             ACCESS_TYPE = c.ACCESS_TYPE_E,
                             
                         }).ToList();
                return Json(v, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var v = (from a in db.RA42_DOCUMENTS_ACCESS_MST

                         join b in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals b.FILE_TYPE_CODE
                         where a.FILE_TYPE_CODE == Id
                         join c in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals c.ACCESS_TYPE_CODE
                         where a.DLT_STS != true
                         select new
                         {
                             a.ACCESS_TYPE_CODE,
                             c.ACCESS_TYPE
                         }).ToList();
                return Json(v, JsonRequestBehavior.AllowGet);
            }
        
            

        }

        //get access types 
        [HttpGet]
        public JsonResult GetSelectdForces(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            if (Language.GetCurrentLang() == "en")
            {
                var v = (from a in db.RA42_FILE_FORCES_MST

                         join b in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals b.FILE_TYPE_CODE
                         where a.FILE_TYPE_CODE == Id
                         join c in db.RA42_FORCES_MST on a.FORCE_ID equals c.FORCE_ID
                         where a.DLT_STS != true
                         select new
                         {
                             a.FORCE_ID,
                             FORCE_NAME = c.FORCE_NAME_E,

                         }).ToList();
                return Json(v, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var v = (from a in db.RA42_FILE_FORCES_MST

                         join b in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals b.FILE_TYPE_CODE
                         where a.FILE_TYPE_CODE == Id
                         join c in db.RA42_FORCES_MST on a.FORCE_ID equals c.FORCE_ID
                         where a.DLT_STS != true
                         select new
                         {
                             a.FORCE_ID,
                             FORCE_NAME = c.FORCE_NAME_A,

                         }).ToList();
                return Json(v, JsonRequestBehavior.AllowGet);
            }



        }

        //get access types 
        [HttpGet]
        public JsonResult GetSelectdCardsFor(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            if (Language.GetCurrentLang() == "en")
            {
                var v = (from a in db.RA42_FILE_CARD_MST

                         join b in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals b.FILE_TYPE_CODE
                         where a.FILE_TYPE_CODE == Id
                         join c in db.RA42_CARD_FOR_MST on a.CARD_FOR_CODE equals c.CARD_FOR_CODE
                         where a.DLT_STS != true
                         select new
                         {
                             a.CARD_FOR_CODE,
                             CARD_FOR = c.CARD_FOR_E,

                         }).ToList();
                return Json(v, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var v = (from a in db.RA42_FILE_CARD_MST

                         join b in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals b.FILE_TYPE_CODE
                         where a.FILE_TYPE_CODE == Id
                         join c in db.RA42_CARD_FOR_MST on a.CARD_FOR_CODE equals c.CARD_FOR_CODE
                         where a.DLT_STS != true
                         select new
                         {
                             a.CARD_FOR_CODE,
                             CARD_FOR = c.CARD_FOR_A,

                         }).ToList();
                return Json(v, JsonRequestBehavior.AllowGet);
            }



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
