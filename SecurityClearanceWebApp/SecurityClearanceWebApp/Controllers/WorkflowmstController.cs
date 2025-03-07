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
    //this is workflow types controller, this is for developer only
	public class WorkflowmstController : Controller
	{
        //get db connection
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify generalfucntions class to use some of its functions 
        private GeneralFunctions general = new GeneralFunctions();
        //set title of controller from resources 
        private string title = Resources.Settings.ResourceManager.GetString("WORKFLOW_TYPE" + "_" + "ar");
        public WorkflowmstController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Workflowmst";
            //set icon of controller from fontawsome library
			ViewBag.controllerIconClass = "fa fa-exchange-alt";
            if(Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("WORKFLOW_TYPE" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;


        }


        // this is main view to add, edit, delete and retrive data
        public ActionResult Index()
		{
                //check if current user has view permession
           
                if (ViewBag.DEVELOPER !=true)
                {
                    general.NotFound();
                }

            
            //show all workflow types 
            return View(db.RA42_WORKFLOW_MST.ToList());
		}

        //save or edit data
        public JsonResult SaveDataInDatabase(RA42_WORKFLOW_MST model)
        {

            var result = false;
            try
            {
                //if workflow id is greater than 0 this means this record needs update
                if (model.WORKFLOWID > 0)
                {
                    //check if current user has update permession 
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
                    //check if record is in the db table then update data
                    RA42_WORKFLOW_MST rA42_WORKFLOW_MST = db.RA42_WORKFLOW_MST.SingleOrDefault(x => x.DLT_STS != true && x.WORKFLOWID == model.WORKFLOWID);
                    rA42_WORKFLOW_MST.STEP_NAME = model.STEP_NAME;
                    rA42_WORKFLOW_MST.STEP_NAME_E = model.STEP_NAME_E;
                    rA42_WORKFLOW_MST.UPD_BY = currentUser;
                    rA42_WORKFLOW_MST.UPD_DT = DateTime.Now;
                    rA42_WORKFLOW_MST.DLT_STS = false;
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
                        if (ViewBag.DEVELOPER != true)
                        {
                            AddToast(new Toast("",
                          GetResourcesValue("Dont_have_permissions_to_add"),
                          "red"));
                            return Json(false, JsonRequestBehavior.AllowGet);
                        }
                        
                    }
                    //add new record 
                    RA42_WORKFLOW_MST rA42_WORKFLOW_MST = new RA42_WORKFLOW_MST();
                    rA42_WORKFLOW_MST.STEP_NAME = model.STEP_NAME;
                    rA42_WORKFLOW_MST.STEP_NAME_E = model.STEP_NAME_E;
                    rA42_WORKFLOW_MST.UPD_BY = currentUser;
                    rA42_WORKFLOW_MST.UPD_DT = DateTime.Now;
                    rA42_WORKFLOW_MST.CRD_BY = currentUser;
                    rA42_WORKFLOW_MST.CRD_DT = DateTime.Now;
                    rA42_WORKFLOW_MST.DLT_STS = false;
                    db.RA42_WORKFLOW_MST.Add(rA42_WORKFLOW_MST);
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
        //get specific data record 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var value = db.RA42_WORKFLOW_MST.Where(a => a.WORKFLOWID == Id).FirstOrDefault();
            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        //delete or hide record 
        public JsonResult DltRecordById(int Id)
        {
            //check if current user has deleting permession 
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
            RA42_WORKFLOW_MST rA42_WORKFLOW_MST = db.RA42_WORKFLOW_MST.Where(x => x.WORKFLOWID == Id).FirstOrDefault();
            if (rA42_WORKFLOW_MST != null)
            {
                rA42_WORKFLOW_MST.UPD_BY = currentUser;
                rA42_WORKFLOW_MST.UPD_DT = DateTime.Now;
                //hide record by set DLT_STS to true 
                rA42_WORKFLOW_MST.DLT_STS = true;
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
