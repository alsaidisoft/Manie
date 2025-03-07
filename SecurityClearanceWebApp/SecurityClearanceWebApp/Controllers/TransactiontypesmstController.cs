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
using Newtonsoft.Json;
using portal.Controllers;
using SecurityClearanceWebApp.Models;
using SecurityClearanceWebApp.Util;

namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    //this identities controller to add, edit, view, delete identity 
	public class TransactiontypesmstController : Controller
	{
        //get db connection
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        //identify toats variable 
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify generalFunctions class
        private GeneralFunctions general = new GeneralFunctions();

        private string title = "";


        public TransactiontypesmstController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Transactiontypesmst";
            //set fontawsome icon for the controller
            ViewBag.controllerIconClass = "fa fa-file-invoice-dollar";
            //set title of controller 
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("Transaction" + "_" + "en");
            }
            else
            {
                title = Resources.Settings.ResourceManager.GetString("Transaction" + "_" + "ar");

            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;

        }


		// this is main view to add, edit,delete andd retrive identities 
		public ActionResult Index()
		{
            //check view permession
            if (ViewBag.DEVELOPER != true)
            {
               
                    general.NotFound();

                
            }

            //get stations 
            if (Language.GetCurrentLang() == "en")
            {
                ViewBag.FORCE_ID = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true), "FORCE_ID", "FORCE_NAME_E");

            }
            else
            {
                ViewBag.FORCE_ID = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true), "FORCE_ID", "FORCE_NAME_A");


            }
            //get all identities 
            return View(db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true).OrderByDescending(a=>a.TRANSACTION_TYPE_CODE).ToList());
		}
       //save and edit new identities 
        public JsonResult SaveDataInDatabase(RA42_TRANSACTION_TYPE_MST model)
        {
            
            var result = false;
            try
            {
                //check if model IDENTITY_CODE > 0 that means this record need update 
                if (model.TRANSACTION_TYPE_CODE > 0)
                {
                    //check if user has update permession
                    if (ViewBag.DEVELOPER != true)
                    {
                        
                            AddToast(new Toast("",
                           GetResourcesValue("Dont_have_permissions_to_up"),
                           "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        
                        
                    }
                    //check if record in RA42_IDENTITY_MST table 
                    RA42_TRANSACTION_TYPE_MST rA42_TRANSACTION_TYPE_MST = db.RA42_TRANSACTION_TYPE_MST.SingleOrDefault(x => x.DLT_STS != true && x.TRANSACTION_TYPE_CODE == model.TRANSACTION_TYPE_CODE);
                    rA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A = model.TRANSACTION_NAME_A;
                    rA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_E = model.TRANSACTION_NAME_E;
                    rA42_TRANSACTION_TYPE_MST.FORCE_ID = model.FORCE_ID;
                    rA42_TRANSACTION_TYPE_MST.AMOUNT = model.AMOUNT;
                    rA42_TRANSACTION_TYPE_MST.REMARKS = model.REMARKS;
                    rA42_TRANSACTION_TYPE_MST.UPD_BY = currentUser;
                    rA42_TRANSACTION_TYPE_MST.UPD_DT = DateTime.Now;
                    db.SaveChanges();
                    result = true;
                    AddToast(new Toast("",
                   GetResourcesValue("success_update_message"),
                   "green"));
                }
                else
                {

                    //add new identity 
                    RA42_TRANSACTION_TYPE_MST rA42_TRANSACTION_TYPE_MST = new RA42_TRANSACTION_TYPE_MST();
                    rA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A = model.TRANSACTION_NAME_A;
                    rA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_E = model.TRANSACTION_NAME_E;
                    rA42_TRANSACTION_TYPE_MST.FORCE_ID = model.FORCE_ID;
                    rA42_TRANSACTION_TYPE_MST.AMOUNT = model.AMOUNT;
                    rA42_TRANSACTION_TYPE_MST.REMARKS = model.REMARKS;
                    rA42_TRANSACTION_TYPE_MST.UPD_BY = currentUser;
                    rA42_TRANSACTION_TYPE_MST.UPD_DT = DateTime.Now;
                    rA42_TRANSACTION_TYPE_MST.CRD_BY = currentUser;
                    rA42_TRANSACTION_TYPE_MST.CRD_DT = DateTime.Now;
                    rA42_TRANSACTION_TYPE_MST.DLT_STS = false;
                    db.RA42_TRANSACTION_TYPE_MST.Add(rA42_TRANSACTION_TYPE_MST);
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
        //get identity details as json result 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var value = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.TRANSACTION_TYPE_CODE == Id).FirstOrDefault();
            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(value, JsonRequestBehavior.AllowGet);
        }

      //delete any record by json result 
        public JsonResult DltRecordById(int Id)
        {
            //check deleting permession
            if (ViewBag.DEVELOPER != true)
            {
               
                    AddToast(new Toast("",
                   GetResourcesValue("Dont_have_permissions_to_dlt"),
                   "red"));
                    return Json(false, JsonRequestBehavior.AllowGet);
                
                
            }
            bool result = false;
            RA42_TRANSACTION_TYPE_MST rA42_TRANSACTION_TYPE_MST = db.RA42_TRANSACTION_TYPE_MST.Where(x=> x.TRANSACTION_TYPE_CODE == Id).FirstOrDefault();
            if (rA42_TRANSACTION_TYPE_MST != null)
            {
                rA42_TRANSACTION_TYPE_MST.UPD_BY = currentUser;
                rA42_TRANSACTION_TYPE_MST.UPD_DT = DateTime.Now;
                //hide rcord by set DLT_STS to true 
                rA42_TRANSACTION_TYPE_MST.DLT_STS = true;
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
