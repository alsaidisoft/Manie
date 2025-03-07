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
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;
using APP.Filters;
using APP.Util;
using Newtonsoft.Json;
using portal.Controllers;
using SecurityClearanceWebApp.Models;
using SecurityClearanceWebApp.Util;
//master permessions controller for all parts of the system 
namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
	public class WorkflowresponsiblemstController : Controller
	{
        //get db connection
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user from UserInfo() class
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class
        private GeneralFunctions general = new GeneralFunctions();

        //set title of the controller
        private string title = Resources.Settings.ResourceManager.GetString("WORKFLOW_TYPE_Permissions" + "_" + "ar");
        public WorkflowresponsiblemstController() {
            //this controller related to settings tab
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName
            ViewBag.controllerName = "Workflowresponsiblemst";
            //set icon of controller from fontawsom
			ViewBag.controllerIconClass = "fa fa-expand-arrows-alt";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("WORKFLOW_TYPE_Permissions" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;


        }


        // index will have every things related to View, create, delete and update methods of any user
        //every thing will include in index page 
        //we will retrive datat and send data by json arrays
        public ActionResult Index()
		{
            //check view permession
            if (ViewBag.VW != true)
            {
                //if the user not have any of them check administrator permession
                if (ViewBag.ADMIN != true || ViewBag.DEVELOPER != true)
                {
                    //if no permessions show notfound page 
                    general.NotFound();
                }

            }
            //get current user STATION_CODE from viewbag.STATION_CODE_TYPE which is identified in PermessionFilter class in Filters folder
            int station = Convert.ToInt32(ViewBag.STATION_CODE_TYPE);
            //get force id
            int force = Convert.ToInt32(ViewBag.FORCE_TYPE_CODE);
            //ignore these access if you are not developer
            var access_not_included = new List<string> {
            "1","6","8","7","10","11"};
            if (force == 1)
            {
                if (ViewBag.ADMIN == true || ViewBag.DEVELOPER == true)
                {
                    access_not_included = new List<string> { };
                }
            }
            if(force == 2)
            {
                access_not_included = new List<string> {
            "1","6","10","11"};
            }
            if (force == 3)
            {
                access_not_included = new List<string> {
            "1","6","10","11"};
            }
            if (force == 4 || force == 5)
            {
                access_not_included = new List<string> {
            "1","6","10","11"};
            }
            var workflow_not_included = new List<string> {
             "5","6","7","10","11"
            };
            if(force == 3)
            {
                workflow_not_included = new List<string>
                {
                     "5","6","7","10"
                };
            }
            //sections
            ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == station), "SECTION_NAME", "SECTION_NAME");
            //check language 
            if (Language.GetCurrentLang() == "en")
            {
               //get not deleted access in english as selectlist to show them in select list options
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => !access_not_included.Contains(a.ACCESS_SECRET_CODE.ToString())), "ACCESS_TYPE_CODE", "ACCESS_TYPE_E");
                //get not deleted workflows in english as selectlist to show them in select list options
                ViewBag.WORKFLOWID = new SelectList(db.RA42_WORKFLOW_MST.Where(a => !workflow_not_included.Contains(a.WORKFLOW_SECRET_CODE.ToString())), "WORKFLOWID", "STEP_NAME_E");
                //get unitcode of current user in english as selectlist to show them in select list options
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a=>a.STATION_CODE == station && a.DLT_STS !=true), "STATION_CODE", "STATION_NAME_E");
               
                if (ViewBag.ADMIN == true)
                {

                    //get  access in english as selectlist to show them in select list options
                    ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => !access_not_included.Contains(a.ACCESS_SECRET_CODE.ToString())), "ACCESS_TYPE_CODE", "ACCESS_TYPE_E");
                    //get  workflows in english as selectlist to show them in select list options
                    ViewBag.WORKFLOWID = new SelectList(db.RA42_WORKFLOW_MST.Where(a => !workflow_not_included.Contains(a.WORKFLOW_SECRET_CODE.ToString())), "WORKFLOWID", "STEP_NAME_E");
                    //get stations in english as selectlist to show them in select list options
                    ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == force), "STATION_CODE", "STATION_NAME_E");
                }

                //if the current user have one of theses permession he can show all access and all workflows and all stations to give any body from different stations any permession
                if (ViewBag.DEVELOPER == true)
                {
                    //get  access in english as selectlist to show them in select list options
                    ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST, "ACCESS_TYPE_CODE", "ACCESS_TYPE_E");
                    //get  workflows in english as selectlist to show them in select list options
                    ViewBag.WORKFLOWID = new SelectList(db.RA42_WORKFLOW_MST, "WORKFLOWID", "STEP_NAME_E");
                    //get stations in english as selectlist to show them in select list options
                    ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a=>a.DLT_STS !=true), "STATION_CODE", "STATION_NAME_E");

                }

               

            }
            else
            {
                //get not deleted access in arabic as selectlist to show them in select list options
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => !access_not_included.Contains(a.ACCESS_SECRET_CODE.ToString())), "ACCESS_TYPE_CODE", "ACCESS_TYPE");
                //get not deleted workflows in arabic as selectlist to show them in select list options
                ViewBag.WORKFLOWID = new SelectList(db.RA42_WORKFLOW_MST.Where(a => !workflow_not_included.Contains(a.WORKFLOW_SECRET_CODE.ToString())), "WORKFLOWID", "STEP_NAME");
                //get stationcode of current user in arabic as selectlist to show them in select list options
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == station), "STATION_CODE", "STATION_NAME_A");
                //if the current user have one of theses permession he can show all access and all workflows and all stations to give any body from different stations any permession
                if (ViewBag.ADMIN == true)
                {

                    //get  access in english as selectlist to show them in select list options
                    ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => !access_not_included.Contains(a.ACCESS_SECRET_CODE.ToString())), "ACCESS_TYPE_CODE", "ACCESS_TYPE");
                    //get  workflows in english as selectlist to show them in select list options
                    ViewBag.WORKFLOWID = new SelectList(db.RA42_WORKFLOW_MST.Where(a => !workflow_not_included.Contains(a.WORKFLOW_SECRET_CODE.ToString())), "WORKFLOWID", "STEP_NAME");
                    //get stations in english as selectlist to show them in select list options
                    ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == force), "STATION_CODE", "STATION_NAME_A");

                }

                if (ViewBag.DEVELOPER == true)
                {
                    ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST, "ACCESS_TYPE_CODE", "ACCESS_TYPE");
                    ViewBag.WORKFLOWID = new SelectList(db.RA42_WORKFLOW_MST, "WORKFLOWID", "STEP_NAME");
                    ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a=>a.DLT_STS !=true), "STATION_CODE", "STATION_NAME_A");
                }

               
            }
            //show all users who have permessions if the current user has one of theses two permessions
            if (ViewBag.ADMIN == true || ViewBag.DEVELOPER == true)
            {
                if(ViewBag.DEVELOPER == true)
                {
                    var rA42_WORKFLOW_RESPONSIBLE_MST1 = db.RA42_WORKFLOW_RESPONSIBLE_MST.Include(r => r.RA42_ACCESS_SELECT_MST).Include(r => r.RA42_WORKFLOW_MST);
                    return View(rA42_WORKFLOW_RESPONSIBLE_MST1.Where(a => a.DLT_STS != true).ToList());

                }

                var rA42_WORKFLOW_RESPONSIBLE_MST = db.RA42_WORKFLOW_RESPONSIBLE_MST.Include(r => r.RA42_ACCESS_SELECT_MST).Include(r => r.RA42_WORKFLOW_MST);
                return View(rA42_WORKFLOW_RESPONSIBLE_MST.Where(a => a.DLT_STS != true && a.DEVELOPER !=true && a.RA42_STATIONS_MST.FORCE_ID == force).ToList());
            }
            else
            {
              //show cuurnts users who have permessions for the current user unit_code
                int unit = Convert.ToInt32(ViewBag.STATION_CODE_TYPE);
                
                var rA42_WORKFLOW_RESPONSIBLE_MST = db.RA42_WORKFLOW_RESPONSIBLE_MST.Include(r => r.RA42_ACCESS_SELECT_MST).Include(r => r.RA42_WORKFLOW_MST);
                return View(rA42_WORKFLOW_RESPONSIBLE_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == unit && a.DEVELOPER != true && a.ADMIN !=true).OrderByDescending(a=>a.WORKFLOW_RESPO_CODE).ToList());
            }
            
		}

        //save data / edit date by this json function
        //get data via serilaizing data by javascript and post it as RA42_WORKFLOW_RESPONSIBLE_MST model 
        public JsonResult SaveDataInDatabase(RA42_WORKFLOW_RESPONSIBLE_MST model)
        {

           // first check if user service_number in API (MIMS/HRMS)
            User user = null;
            Task<User> callTask = Task.Run(
                () => (new UserInfo()).getUserInfoFromAPI(model.SERVICE_NUMBER.ToUpper())
                );
            callTask.Wait();
            user = callTask.Result;
            //if user is in API, continue 
            if (user != null)
            {
               //identify result 
                var result = false;
                try
                {
                    //if model WORKFLOW_RESPO_CODE more than 0 that means this user alreaday in database and needs update 
                    if (model.WORKFLOW_RESPO_CODE > 0)
                    {
                        //check if current user has update permession
                        if (ViewBag.UP != true)
                        {
                            // if not check administrator permession
                            if (ViewBag.ADMIN != true || ViewBag.DEVELOPER != true)
                            {
                                //show error message 
                                AddToast(new Toast("",
                              GetResourcesValue("Dont_have_permissions_to_up"),
                              "red"));
                                //return false result as json to show user the error 
                                return Json(false, JsonRequestBehavior.AllowGet);
                            }
                        }


                            //check if SERVICE_NUMBER in the RA42_WORKFLOW_RESPONSIBLE_MST table 
                            RA42_WORKFLOW_RESPONSIBLE_MST rA42_WORKFLOW_RESPONSIBLE_MST = db.RA42_WORKFLOW_RESPONSIBLE_MST.SingleOrDefault(x => x.DLT_STS != true && x.WORKFLOW_RESPO_CODE == model.WORKFLOW_RESPO_CODE);
                            rA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER = model.SERVICE_NUMBER;
                            rA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME = model.RESPONSEPLE_NAME;
                            rA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E = user.NAME_EMP_E;
                            //rA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID = model.WORKFLOWID;
                            rA42_WORKFLOW_RESPONSIBLE_MST.RANK = user.NAME_RANK_A;
                            rA42_WORKFLOW_RESPONSIBLE_MST.RANK_E = user.NAME_RANK_E;
                            rA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE = model.STATION_CODE;
                            rA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME = model.UNIT_NAME;
                            rA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E = user.NAME_UNIT_E;
                            rA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE = model.ACTIVE;

                        //if current user has one of theses permession, he can change these permession SETTINGS, RP, ADMIN , DEVELOPER 
                        if (ViewBag.ADMIN == true || ViewBag.DEVELOPER == true)
                            {

                                rA42_WORKFLOW_RESPONSIBLE_MST.RP = model.RP;
                                rA42_WORKFLOW_RESPONSIBLE_MST.SETTINGS = model.SETTINGS;
                                //developer can give other user this permession, nobody alse can give this permession accept user
                                if (ViewBag.DEVELOPER == true)
                                {
                                    rA42_WORKFLOW_RESPONSIBLE_MST.DEVELOPER = model.DEVELOPER;
                                }
                                else
                                {
                                    rA42_WORKFLOW_RESPONSIBLE_MST.DEVELOPER = rA42_WORKFLOW_RESPONSIBLE_MST.DEVELOPER;
                                }
                                rA42_WORKFLOW_RESPONSIBLE_MST.ADMIN = model.ADMIN;
                                rA42_WORKFLOW_RESPONSIBLE_MST.AD = true;
                                rA42_WORKFLOW_RESPONSIBLE_MST.UP = true;
                                rA42_WORKFLOW_RESPONSIBLE_MST.VW = true;
                                rA42_WORKFLOW_RESPONSIBLE_MST.DL = true;
                            }
                            else

                            {
                                //if current user has STN permession he can only give rp and settings permession
                                if (ViewBag.RP == true)
                                {
                                    rA42_WORKFLOW_RESPONSIBLE_MST.RP = model.RP;
                                }
                                else
                                {
                                    rA42_WORKFLOW_RESPONSIBLE_MST.RP = rA42_WORKFLOW_RESPONSIBLE_MST.RP;

                                }
                                rA42_WORKFLOW_RESPONSIBLE_MST.SETTINGS = model.SETTINGS;
                                rA42_WORKFLOW_RESPONSIBLE_MST.DEVELOPER = rA42_WORKFLOW_RESPONSIBLE_MST.DEVELOPER;
                                rA42_WORKFLOW_RESPONSIBLE_MST.ADMIN = rA42_WORKFLOW_RESPONSIBLE_MST.ADMIN;
                                if (model.SETTINGS == true)
                                {
                                    rA42_WORKFLOW_RESPONSIBLE_MST.AD = true;
                                    rA42_WORKFLOW_RESPONSIBLE_MST.UP = true;
                                    rA42_WORKFLOW_RESPONSIBLE_MST.VW = true;
                                    rA42_WORKFLOW_RESPONSIBLE_MST.DL = true;
                                }

                            }

                            
                           

                       
                            //first delete record 
                            rA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID = rA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID;
                            rA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS = true;
                            rA42_WORKFLOW_RESPONSIBLE_MST.UPD_BY = currentUser;
                            rA42_WORKFLOW_RESPONSIBLE_MST.UPD_DT = DateTime.Now;
                            db.SaveChanges();
                            //then add new record with same data but different id
                            if (!string.IsNullOrWhiteSpace(model.WORKFLOWID.ToString()))
                            {
                                rA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID = model.WORKFLOWID;

                            }
                            else
                            {
                                rA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID = rA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID;

                            }
                            rA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS = false;
                            rA42_WORKFLOW_RESPONSIBLE_MST.CRD_BY = currentUser;
                            rA42_WORKFLOW_RESPONSIBLE_MST.CRD_DT = DateTime.Now;
                            db.RA42_WORKFLOW_RESPONSIBLE_MST.Add(rA42_WORKFLOW_RESPONSIBLE_MST);
                            db.SaveChanges();
                       

                        //now select access in RA42_ACCESS_SELECT_MST table 
                        //first of all get ACCESS_TYPE_CODE[] as a list 
                        List<int> list = model.ACCESS_TYPE_CODE.ToList();
                        //if ACCESS_TYPE_CODE[] not null delete last access registerd in the table RA42_ACCESS_SELECT_MST for this user 
                        if (list != null)
                        {
                            var delete = db.RA42_ACCESS_SELECT_MST.Where(a => a.WORKFLOW_RESPO_CODE == rA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE).ToList();

                            if (delete != null)
                            {
                                foreach (var item in delete)
                                {
                                    item.UPD_BY = currentUser;
                                    item.UPD_DT = DateTime.Now;
                                    item.DLT_STS = true;
                                    db.SaveChanges();
                                    //db.RA42_ACCESS_SELECT_MST.Remove(item);


                                }
                            }
                            //now save every access in the table RA42_ACCESS_SELECT_MST for this user one by one 
                            RA42_ACCESS_SELECT_MST rA42_ACCESS_SELECT_MST = new RA42_ACCESS_SELECT_MST();
                            for (int i = 0; i < list.Count; i++)
                            {
                                rA42_ACCESS_SELECT_MST.ACCESS_TYPE_CODE = list[i];
                                rA42_ACCESS_SELECT_MST.WORKFLOW_RESPO_CODE = rA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE;
                                rA42_ACCESS_SELECT_MST.CRD_BY = currentUser;
                                rA42_ACCESS_SELECT_MST.CRD_DT = DateTime.Now;
                                rA42_ACCESS_SELECT_MST.UPD_BY = currentUser;
                                rA42_ACCESS_SELECT_MST.UPD_DT = DateTime.Now;
                                db.RA42_ACCESS_SELECT_MST.Add(rA42_ACCESS_SELECT_MST);
                                db.SaveChanges();
                            }
                        }
                        ///return true as successfull message then reload view by js 
                        result = true;

                        AddToast(new Toast("",
                       GetResourcesValue("success_update_message"),
                       "green"));
                    }
                    else
                    {
                        //check add permession for current user who use the system 
                        if (ViewBag.AD != true)
                        {
                            //if he dont have add permession,  check administrator and developer permessions 
                            if (ViewBag.ADMIN != true || ViewBag.DEVELOPER != true)
                            {
                                //show error message if no permessions
                                AddToast(new Toast("",
                               GetResourcesValue("Dont_have_permissions_to_add"),
                               "red"));
                                return Json(false, JsonRequestBehavior.AllowGet);
                            }
                           
                        }
                        ///check if service number of the user already in the db 
                        RA42_WORKFLOW_RESPONSIBLE_MST rA42_WORKFLOW_RESPONSIBLE_MST1 = db.RA42_WORKFLOW_RESPONSIBLE_MST.SingleOrDefault(x => x.DLT_STS != true && x.SERVICE_NUMBER == model.SERVICE_NUMBER);

                        List<int> list = model.ACCESS_TYPE_CODE.ToList();
                        //if the service number found the means needs updating 
                        if (rA42_WORKFLOW_RESPONSIBLE_MST1 != null)
                        {
                            rA42_WORKFLOW_RESPONSIBLE_MST1.SERVICE_NUMBER = model.SERVICE_NUMBER;
                            rA42_WORKFLOW_RESPONSIBLE_MST1.RESPONSEPLE_NAME = user.NAME_EMP_A;
                            rA42_WORKFLOW_RESPONSIBLE_MST1.RESPONSEPLE_NAME_E = user.NAME_EMP_E;
                            rA42_WORKFLOW_RESPONSIBLE_MST1.RANK = user.NAME_RANK_A;
                            rA42_WORKFLOW_RESPONSIBLE_MST1.RANK_E = user.NAME_RANK_E;
                            rA42_WORKFLOW_RESPONSIBLE_MST1.STATION_CODE = model.STATION_CODE;
                            if (!string.IsNullOrWhiteSpace(model.UNIT_NAME))
                            {
                                rA42_WORKFLOW_RESPONSIBLE_MST1.UNIT_NAME = model.UNIT_NAME;

                            }
                            else
                            {
                                rA42_WORKFLOW_RESPONSIBLE_MST1.UNIT_NAME = user.NAME_UNIT_A;
                            }
                            rA42_WORKFLOW_RESPONSIBLE_MST1.UNIT_NAME_E = user.NAME_UNIT_E;
                            //rA42_WORKFLOW_RESPONSIBLE_MST1.WORKFLOWID = model.WORKFLOWID;
                            //if current user has one of theses permession, he can change  SETTINGS, RP, ADMIN , DEVELOPER 

                            if (ViewBag.ADMIN == true || ViewBag.DEVELOPER == true)
                            {
                                rA42_WORKFLOW_RESPONSIBLE_MST1.RP = model.RP;
                                rA42_WORKFLOW_RESPONSIBLE_MST1.SETTINGS = model.SETTINGS;
                                if (ViewBag.DEVELOPER == true)
                                {
                                    rA42_WORKFLOW_RESPONSIBLE_MST1.DEVELOPER = model.DEVELOPER;
                                }
                                else
                                {
                                    rA42_WORKFLOW_RESPONSIBLE_MST1.DEVELOPER = rA42_WORKFLOW_RESPONSIBLE_MST1.DEVELOPER;
                                }
                                rA42_WORKFLOW_RESPONSIBLE_MST1.ADMIN = model.ADMIN;
                                rA42_WORKFLOW_RESPONSIBLE_MST1.AD = true;
                                rA42_WORKFLOW_RESPONSIBLE_MST1.UP = true;
                                rA42_WORKFLOW_RESPONSIBLE_MST1.VW = true;
                                rA42_WORKFLOW_RESPONSIBLE_MST1.DL = true;
                                
                            }
                            else
                            {
                                //if current user has STN permession he can only give rp and settings permession
                                if (ViewBag.RP == true)
                                {
                                    rA42_WORKFLOW_RESPONSIBLE_MST1.RP = model.RP;
                                }
                                else
                                {
                                    rA42_WORKFLOW_RESPONSIBLE_MST1.RP = rA42_WORKFLOW_RESPONSIBLE_MST1.RP;

                                }
                                rA42_WORKFLOW_RESPONSIBLE_MST1.SETTINGS = model.SETTINGS;
                                rA42_WORKFLOW_RESPONSIBLE_MST1.DEVELOPER = rA42_WORKFLOW_RESPONSIBLE_MST1.DEVELOPER;
                                rA42_WORKFLOW_RESPONSIBLE_MST1.ADMIN = rA42_WORKFLOW_RESPONSIBLE_MST1.ADMIN;

                                if (model.SETTINGS == true)
                                {
                                    rA42_WORKFLOW_RESPONSIBLE_MST1.AD = true;
                                    rA42_WORKFLOW_RESPONSIBLE_MST1.UP = true;
                                    rA42_WORKFLOW_RESPONSIBLE_MST1.VW = true;
                                    rA42_WORKFLOW_RESPONSIBLE_MST1.DL = true;
                                }
                            }
                      
                           
                                //first delete record 
                                rA42_WORKFLOW_RESPONSIBLE_MST1.WORKFLOWID = rA42_WORKFLOW_RESPONSIBLE_MST1.WORKFLOWID;
                                rA42_WORKFLOW_RESPONSIBLE_MST1.DLT_STS = true;
                                rA42_WORKFLOW_RESPONSIBLE_MST1.UPD_BY = currentUser;
                                rA42_WORKFLOW_RESPONSIBLE_MST1.UPD_DT = DateTime.Now;
                                db.SaveChanges();

                                if (model.SETTINGS == true || model.ADMIN == true || model.DEVELOPER == true)
                                {
                                    rA42_WORKFLOW_RESPONSIBLE_MST1.AD = true;
                                    rA42_WORKFLOW_RESPONSIBLE_MST1.UP = true;
                                    rA42_WORKFLOW_RESPONSIBLE_MST1.VW = true;
                                    rA42_WORKFLOW_RESPONSIBLE_MST1.DL = true;
                                }
                                //then add new record with same data but different id
                                if (!string.IsNullOrWhiteSpace(model.WORKFLOWID.ToString()))
                                {
                                    rA42_WORKFLOW_RESPONSIBLE_MST1.WORKFLOWID = model.WORKFLOWID;

                                }
                                else
                                {
                                    rA42_WORKFLOW_RESPONSIBLE_MST1.WORKFLOWID = rA42_WORKFLOW_RESPONSIBLE_MST1.WORKFLOWID;

                                }
                                rA42_WORKFLOW_RESPONSIBLE_MST1.ACTIVE = model.ACTIVE;
                                rA42_WORKFLOW_RESPONSIBLE_MST1.UPD_BY = currentUser;
                                rA42_WORKFLOW_RESPONSIBLE_MST1.UPD_DT = DateTime.Now;
                                rA42_WORKFLOW_RESPONSIBLE_MST1.DLT_STS = false;
                                //rA42_WORKFLOW_RESPONSIBLE_MST1.CRD_BY = currentUser;
                                //rA42_WORKFLOW_RESPONSIBLE_MST1.CRD_DT = DateTime.Now;
                                db.RA42_WORKFLOW_RESPONSIBLE_MST.Add(rA42_WORKFLOW_RESPONSIBLE_MST1);
                                db.SaveChanges();
                            
                            //first of all get ACCESS_TYPE_CODE[] as a list 
                            if (list != null)
                            {
                                //if ACCESS_TYPE_CODE[] not null delete last access registerd in the table RA42_ACCESS_SELECT_MST for this user 

                                var delete1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.WORKFLOW_RESPO_CODE == rA42_WORKFLOW_RESPONSIBLE_MST1.WORKFLOW_RESPO_CODE).ToList();

                                if (delete1 != null)
                                {
                                    foreach (var item in delete1)
                                    {
                                        item.UPD_BY = currentUser;
                                        item.UPD_DT = DateTime.Now;
                                        item.DLT_STS = true;
                                        db.SaveChanges();
                                        //db.RA42_ACCESS_SELECT_MST.Remove(item);


                                    }
                                }
                                //save current access stored in ACCESS_TYPE_CODE[] array in RA42_ACCESS_SELECT_MST table 
                                RA42_ACCESS_SELECT_MST rA42_ACCESS_SELECT_MST = new RA42_ACCESS_SELECT_MST();
                                for (int i = 0; i < list.Count; i++)
                                {
                                    rA42_ACCESS_SELECT_MST.ACCESS_TYPE_CODE = list[i];
                                    rA42_ACCESS_SELECT_MST.WORKFLOW_RESPO_CODE = rA42_WORKFLOW_RESPONSIBLE_MST1.WORKFLOW_RESPO_CODE;
                                    rA42_ACCESS_SELECT_MST.CRD_BY = currentUser;
                                    rA42_ACCESS_SELECT_MST.CRD_DT = DateTime.Now;
                                    rA42_ACCESS_SELECT_MST.UPD_BY = currentUser;
                                    rA42_ACCESS_SELECT_MST.UPD_DT = DateTime.Now;
                                    db.RA42_ACCESS_SELECT_MST.Add(rA42_ACCESS_SELECT_MST);
                                    db.SaveChanges();
                                   
                            }
                                result = true;
                                AddToast(new Toast("",
                                GetResourcesValue("success_update_message"),
                                "green"));
                            }
                        }
                        else
                        {
                            //if user service number not found in the table RA42_WORKFLOW_RESPONSIBLE_MST, that means this is new user and should adding him as new user
                            RA42_WORKFLOW_RESPONSIBLE_MST rA42_WORKFLOW_RESPONSIBLE_MST = new RA42_WORKFLOW_RESPONSIBLE_MST();
                            rA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER = model.SERVICE_NUMBER;
                            rA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME = user.NAME_EMP_A;
                            rA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E = user.NAME_EMP_E;
                            rA42_WORKFLOW_RESPONSIBLE_MST.RANK = user.NAME_RANK_A;
                            rA42_WORKFLOW_RESPONSIBLE_MST.RANK_E = user.NAME_RANK_E;
                            rA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE = model.STATION_CODE;
                            if (!string.IsNullOrWhiteSpace(model.UNIT_NAME))
                            {
                                rA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME = model.UNIT_NAME;

                            }
                            else
                            {
                                rA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME = user.NAME_UNIT_A;
                            }
                            rA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E = user.NAME_UNIT_E;
                            rA42_WORKFLOW_RESPONSIBLE_MST.RP = model.RP;
                            rA42_WORKFLOW_RESPONSIBLE_MST.SETTINGS = model.SETTINGS;
                            rA42_WORKFLOW_RESPONSIBLE_MST.DEVELOPER = model.DEVELOPER;
                            rA42_WORKFLOW_RESPONSIBLE_MST.ADMIN = model.ADMIN;

                            if (model.SETTINGS == true || model.ADMIN == true || model.DEVELOPER == true)
                            {
                                rA42_WORKFLOW_RESPONSIBLE_MST.AD = true;
                                rA42_WORKFLOW_RESPONSIBLE_MST.UP = true;
                                rA42_WORKFLOW_RESPONSIBLE_MST.VW = true;
                                rA42_WORKFLOW_RESPONSIBLE_MST.DL = true;
                            }
                            rA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE = model.ACTIVE;
                            rA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID = model.WORKFLOWID;
                            rA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS = false;
                            rA42_WORKFLOW_RESPONSIBLE_MST.CRD_BY = currentUser;
                            rA42_WORKFLOW_RESPONSIBLE_MST.CRD_DT = DateTime.Now;
                            rA42_WORKFLOW_RESPONSIBLE_MST.UPD_BY = currentUser;
                            rA42_WORKFLOW_RESPONSIBLE_MST.UPD_DT = DateTime.Now;
                            db.RA42_WORKFLOW_RESPONSIBLE_MST.Add(rA42_WORKFLOW_RESPONSIBLE_MST);
                            //after adding new user save data in the RA42_WORKFLOW_RESPONSIBLE_MST
                            db.SaveChanges();
                            //first of all get ACCESS_TYPE_CODE[] as a list 
                            if (list != null)
                            {
                                //remove or delete last access stored in the database for this user
                                var delete2 = db.RA42_ACCESS_SELECT_MST.Where(a => a.WORKFLOW_RESPO_CODE == rA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE).ToList();

                                if (delete2 != null)
                                {
                                    foreach (var item in delete2)
                                    {
                                        item.UPD_BY = currentUser;
                                        item.UPD_DT = DateTime.Now;
                                        item.DLT_STS = true;
                                        db.SaveChanges();
                                        //db.RA42_ACCESS_SELECT_MST.Remove(item);


                                    }
                                }
                                //save new access in the RA42_ACCESS_SELECT_MST table  
                                RA42_ACCESS_SELECT_MST rA42_ACCESS_SELECT_MST = new RA42_ACCESS_SELECT_MST();
                                for (int i = 0; i < list.Count; i++)
                                {
                                    rA42_ACCESS_SELECT_MST.ACCESS_TYPE_CODE = list[i];
                                    rA42_ACCESS_SELECT_MST.WORKFLOW_RESPO_CODE = rA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE;
                                    rA42_ACCESS_SELECT_MST.CRD_BY = currentUser;
                                    rA42_ACCESS_SELECT_MST.CRD_DT = DateTime.Now;
                                    rA42_ACCESS_SELECT_MST.UPD_BY = currentUser;
                                    rA42_ACCESS_SELECT_MST.UPD_DT = DateTime.Now;
                                    db.RA42_ACCESS_SELECT_MST.Add(rA42_ACCESS_SELECT_MST);
                                    db.SaveChanges();
                                }
                            }
                            //show successfull creation message as a toast
                            AddToast(new Toast("",
                            GetResourcesValue("success_create_message"),
                            "green"));


                            //return true as json result 
                            result = true;
                        }
                    }
                }
                //catch any exception
                catch (Exception ex)
                {
                    AddToast(new Toast("",
                    GetResourcesValue("error_create_message"),
                    "red"));
                    throw ex;
                }

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                //show error message  if there is somthing wrong
                AddToast(new Toast("",
                    GetResourcesValue("error_create_message"),
                    "red"));
                return Json(false, JsonRequestBehavior.AllowGet);

            }
        }
        //get selected access for edit mode 
        [HttpGet]
        public JsonResult GetSelectdAccess(int Id)
        {
            //we need below line for configur proxy, this action is important when we want to return json results somtimes
            db.Configuration.ProxyCreationEnabled = false;
            //var access_not_included = new List<string> {
            //"1","6","10","11"
            //};
            if (Language.GetCurrentLang() == "en")
            {
                //return access in english 
                var v = (from a in db.RA42_ACCESS_SELECT_MST

                         join b in db.RA42_WORKFLOW_RESPONSIBLE_MST on a.WORKFLOW_RESPO_CODE equals b.WORKFLOW_RESPO_CODE
                         where a.WORKFLOW_RESPO_CODE == Id
                         join c in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals c.ACCESS_TYPE_CODE
                         where a.DLT_STS != true 
                         select new
                         {
                             a.ACCESS_TYPE_CODE,
                             ACCESS_TYPE = c.ACCESS_TYPE_E
                         }).ToList();
                return Json(v, JsonRequestBehavior.AllowGet);
            }
            else
            {
                //return access in arabic 
                var v = (from a in db.RA42_ACCESS_SELECT_MST

                         join b in db.RA42_WORKFLOW_RESPONSIBLE_MST on a.WORKFLOW_RESPO_CODE equals b.WORKFLOW_RESPO_CODE
                         where a.WORKFLOW_RESPO_CODE == Id
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
        //get specific data of specific user 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            //we need below line for configur proxy, this action is important when we want to return json results somtimes
            db.Configuration.ProxyCreationEnabled = false;

            ViewBag.ACCESS_TYPE_CODE2 = new SelectList(db.RA42_ACCESS_SELECT_MST.Where(a => a.DLT_STS != true && a.WORKFLOW_RESPO_CODE ==Id), "ACCESS_TYPE_CODE", "RA42_ACCESS_TYPE_MST.ACCESS_TYPE");
            
            var value = db.RA42_WORKFLOW_RESPONSIBLE_MST.Where(a => a.WORKFLOW_RESPO_CODE == Id).FirstOrDefault();
            var v = (from a in db.RA42_WORKFLOW_RESPONSIBLE_MST
                     join b in db.RA42_WORKFLOW_MST on a.WORKFLOWID equals b.WORKFLOWID
                     where a.WORKFLOW_RESPO_CODE == Id
                     select new
                     {
                         a.RESPONSEPLE_NAME,
                         a.RESPONSEPLE_NAME_E,
                         a.RANK,
                         a.RANK_E,
                         a.SERVICE_NUMBER,
                         a.WORKFLOW_RESPO_CODE,
                         a.STATION_CODE,
                         a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_NAME_A,
                         a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_NAME_E,
                         a.UNIT_NAME,
                         a.UNIT_NAME_E,
                         a.AD,
                         a.UP,
                         a.VW,
                         a.DL,
                         a.RP,
                         a.SETTINGS,
                         a.DEVELOPER,
                         a.ADMIN,
                         a.ACTIVE,
                         a.CRD_BY,
                         a.CRD_DT,
                         a.UPD_BY,
                         a.UPD_DT,
                         a.WORKFLOWID,
                         b.STEP_NAME,
                         b.STEP_NAME_E
                     }).FirstOrDefault();
            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(v, JsonRequestBehavior.AllowGet);
        }

       //delete any user and this action is for administrator and developer user
        public JsonResult DltRecordById(int Id)
        {
            //check deleting permession for currnt user who use the system 
            if (ViewBag.DLT != true)
            {
                //check administrator permession and developer permession
                if (ViewBag.ADMIN != true || ViewBag.DEVELOPER != true)
                {
                    //return error messsage if no permession has been found 
                    AddToast(new Toast("",
                  GetResourcesValue("Dont_have_permissions_to_dlt"),
                  "red"));
                    return Json(false, JsonRequestBehavior.AllowGet);
                }
                
            }
            bool result = false;
            //hide user by set DLT_STS true 
            RA42_WORKFLOW_RESPONSIBLE_MST rA42_WORKFLOW_RESPONSIBLE_MST = db.RA42_WORKFLOW_RESPONSIBLE_MST.Where(x => x.WORKFLOW_RESPO_CODE == Id).FirstOrDefault();
            if (rA42_WORKFLOW_RESPONSIBLE_MST != null)
            {
                rA42_WORKFLOW_RESPONSIBLE_MST.UPD_BY = currentUser;
                rA42_WORKFLOW_RESPONSIBLE_MST.UPD_DT = DateTime.Now;
                rA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS = true;
                db.SaveChanges();
                result = true;
                AddToast(new Toast("",
                   GetResourcesValue("success_delete_message"),
                   "green"));
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        

        // this view for any user who has any previllage in the system to see his post data and current position and other permession
        public ActionResult UserInfo(int? id)
        {
            
            if (id == null)
            {
                general.NotFound();
            }
            
            RA42_WORKFLOW_RESPONSIBLE_MST rA42_WORKFLOW_RESPONSIBLE_MST = db.RA42_WORKFLOW_RESPONSIBLE_MST.Where(a => a.SERVICE_NUMBER == currentUser && a.WORKFLOW_RESPO_CODE == id && a.ACTIVE !=false).FirstOrDefault();
           
            if (rA42_WORKFLOW_RESPONSIBLE_MST == null)
            {
               general.NotFound();
            }
       
            var station = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == rA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE).FirstOrDefault();
            if (Language.GetCurrentLang() == "en")
            {
                ViewBag.STATION_NAME = station.STATION_NAME_E;

            }
            else
            {
                ViewBag.STATION_NAME = station.STATION_NAME_A;

            }
            return View(rA42_WORKFLOW_RESPONSIBLE_MST);
        }

        //this action not used now, we use this action to upload signature for any user who have some permession of the system, but now we disactive this one 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserInfo(string ImageVal, RA42_WORKFLOW_RESPONSIBLE_MST rA42_WORKFLOW_RESPONSIBLE_MST)
        {
           
            var v = db.RA42_WORKFLOW_RESPONSIBLE_MST.Where(a => a.WORKFLOW_RESPO_CODE == rA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE).FirstOrDefault();
            
            string fileName = currentUser + "_signature_" + ".png";
            string path = Path.Combine(Server.MapPath("~/Files/Signatures/"), fileName);
            
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    byte[] data = Convert.FromBase64String(ImageVal);
                    bw.Write(data);
                    bw.Close();

                }


                fs.Close();
            }
            if (v != null)
            {
                v.STATION_CODE = v.STATION_CODE;
                v.UNIT_NAME = v.UNIT_NAME;
                v.SERVICE_NUMBER = v.SERVICE_NUMBER;
                v.RESPONSEPLE_NAME = v.RESPONSEPLE_NAME;
                v.WORKFLOWID = v.WORKFLOWID;
                v.ACTIVE = v.ACTIVE;
                v.CRD_BY = v.CRD_BY;
                v.CRD_DT = v.CRD_DT;
                v.SIGNATURE = fileName;
                v.UPD_BY = currentUser;
                v.UPD_DT = DateTime.Now;
                db.Entry(v).State = EntityState.Modified;
                db.SaveChanges();
                AddToast(new Toast("",
                GetResourcesValue("success_update_message"),
                "green"));
                return RedirectToAction("UserInfo", new { id = rA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE });
            }
           
            var station = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == rA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE).FirstOrDefault();
            if (Language.GetCurrentLang() == "en")
            {
                ViewBag.STATION_NAME = station.STATION_NAME_E;

            }
            else
            {
                ViewBag.STATION_NAME = station.STATION_NAME_A;

            }
            return View(rA42_WORKFLOW_RESPONSIBLE_MST);
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
