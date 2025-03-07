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
    //this controller to active disactive any user by service number from ussing this app 
	public class GeneralusersmstController : Controller
	{
        //get db connection
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class 
        private GeneralFunctions general = new GeneralFunctions();
        private string title = Resources.Settings.ResourceManager.GetString("Block_active_users" + "_" + "ar");

        public GeneralusersmstController() {
            ViewBag.Settings = "Settings";
            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Generalusersmst";
            //set fontawsome icon for this controller 
			ViewBag.controllerIconClass = "fa fa-user-slash";
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;


        }




		// this is main view to retrive, add,edit,delete data 
		public ActionResult Index()
		{
            
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    general.NotFound();
                }
            
            

           
                return View(db.RA42_GENERAL_USERS_MST.ToList());
            
           
        }
        //save and edit new data 
        public JsonResult SaveDataInDatabase(RA42_GENERAL_USERS_MST model)
        {

            var result = false;
            try
            {
                //if model GENERAL_USER_CODE > 0 that means this record need update 
                if (model.GENERAL_USER_CODE > 0)
                {
                    //check update permession
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
                    //check if record in RA42_GENERAL_USERS_MST table 
                    var v = db.RA42_GENERAL_USERS_MST.Where(a => a.SERVICE_NUMBER == model.SERVICE_NUMBER.ToUpper()).FirstOrDefault();
                        if (v != null)
                        {

                            v.SERVICE_NUMBER = v.SERVICE_NUMBER.ToUpper();
                            v.NAME_A = v.NAME_A;
                            v.UNIT_A = v.UNIT_A;
                            v.UNIT_CODE = v.UNIT_CODE;
                            v.ACTIVE = model.ACTIVE;
                            v.UPD_BY = currentUser;
                            v.UPD_DT = DateTime.Now;
                            db.Entry(v).State = EntityState.Modified;
                            db.SaveChanges();
                            result = true;
                            AddToast(new Toast("",
                            GetResourcesValue("success_create_message"),
                            "green"));
                        }
                    else
                    {
                        AddToast(new Toast("",
                        GetResourcesValue("error_create_message"),
                        "red"));
                        return Json(false, JsonRequestBehavior.AllowGet);

                    }
                }
                else
                {
                    //check add permession 
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
                    if (model.SERVICE_NUMBER != null)
                    {
                        User user = null;
                        Task<User> callTask = Task.Run(
                            () => (new UserInfo()).getUserInfoFromAPI(model.SERVICE_NUMBER.ToUpper())
                            );
                        callTask.Wait();
                        user = callTask.Result;
                       
                        if (user != null && ModelState.IsValid)
                        {
                            //check if this user already in RA42_GENERAL_USERS_MST by service number 
                            //if is in RA42_GENERAL_USERS_MST table, so this record need update 
                            var v = db.RA42_GENERAL_USERS_MST.Where(a => a.SERVICE_NUMBER == model.SERVICE_NUMBER.ToUpper()).FirstOrDefault();
                            if (ViewBag.ADMIN == true || ViewBag.DEVELOPER == true)
                            {
                                if (v != null)
                                {

                                    v.SERVICE_NUMBER = model.SERVICE_NUMBER.ToUpper();
                                    v.NAME_A = user.NAME_EMP_A;
                                    v.UNIT_A = user.NAME_UNIT_A;
                                    v.UNIT_CODE = v.UNIT_CODE;
                                    v.ACTIVE = model.ACTIVE;
                                    v.UPD_BY = currentUser;
                                    v.UPD_DT = DateTime.Now;
                                    db.Entry(v).State = EntityState.Modified;
                                    db.SaveChanges();
                                    result = true;
                                    AddToast(new Toast("",
                                    GetResourcesValue("success_create_message"),
                                    "green"));
                                }
                                else
                                {
                                    //add new record if no one has this service number in RA42_GENERAL_USERS_MST table 
                                    RA42_GENERAL_USERS_MST rA42_GENERAL_USERS_MST1 = new RA42_GENERAL_USERS_MST();
                                    rA42_GENERAL_USERS_MST1.SERVICE_NUMBER = model.SERVICE_NUMBER.ToUpper();
                                    rA42_GENERAL_USERS_MST1.NAME_A = user.NAME_EMP_A;
                                    rA42_GENERAL_USERS_MST1.UNIT_A = user.NAME_UNIT_A;
                                    rA42_GENERAL_USERS_MST1.UNIT_CODE = 2001;
                                    rA42_GENERAL_USERS_MST1.ACTIVE = model.ACTIVE;
                                    rA42_GENERAL_USERS_MST1.CRD_BY = currentUser;
                                    rA42_GENERAL_USERS_MST1.CRD_DT = DateTime.Now;
                                    rA42_GENERAL_USERS_MST1.UPD_BY = currentUser;
                                    rA42_GENERAL_USERS_MST1.UPD_DT = DateTime.Now;
                                    db.RA42_GENERAL_USERS_MST.Add(rA42_GENERAL_USERS_MST1);
                                    db.SaveChanges();
                                    result = true;
                                    AddToast(new Toast("",
                                    GetResourcesValue("success_create_message"),
                                    "green"));
                                }
                            }
                            
                        }
                    }
                    else
                    {
                        AddToast(new Toast("",
                                   GetResourcesValue("Not_Entered"),
                                   "red"));
                        return Json(false, JsonRequestBehavior.AllowGet);
                    }
                   
                    
                }
            }
            catch (Exception ex)
            {
                AddToast(new Toast("",
                GetResourcesValue("error_create_message"),
                "red"));
                //throw ex;
                return Json(false, JsonRequestBehavior.AllowGet);
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }
        //get record details as json result 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var value = db.RA42_GENERAL_USERS_MST.Where(a => a.GENERAL_USER_CODE == Id).FirstOrDefault();
            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(value, JsonRequestBehavior.AllowGet);
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
