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
using System.Linq.Dynamic;

// this controller for is responsible all authorization accesss
namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    public class AuthoraizationpassController : Controller
    {
        //database connection
        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        //tost messages identifier
        private IList<Toast> toasts = new List<Toast>();
        //get current user which is service number
        private string currentUser = (new UserInfo()).getSNO();
        // another class for using addditional functions
        private GeneralFunctions general = new GeneralFunctions();

        //by defualt the STATION_CODE & WORKFLOW type & RESPONSIBLE_CODE is 0 
        private int STATION_CODE = 0;
        private int WORKFLOWID = 0;
        private int RESPO_CODE = 0;
        private int FORCE_ID = 0;
        //access type code from Table RA42_ACCESS_TYPE_MST for the kind of permit equal 1
        private int ACCESS_TYPE_CODE = 1;
        //this title will appear always as a title of web page
        private string title = Resources.Passes.ResourceManager.GetString("access_type_autho" + "_" + "ar");

        //constructor
        public AuthoraizationpassController()
        {
            ViewBag.Managepasses = "Managepasses";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Authoraizationpass";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Passes.ResourceManager.GetString("access_type_autho" + "_" + "en");
            }
            //icon from fontawesom will showen in view pages
            ViewBag.controllerIconClass = "fa fa-shield-alt";
            // we will use these viewbags in differnt places in the views
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;

            //here we will check if the user who has some aothorities in RA42_WORKFLOW_RESPONSIBLE_MST in this type of permit and what is the type of WORKFLOW (مقدم طلب، منسق أمني، خلية تصاريح etc...)
            var v = Task.Run(async () => await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefaultAsync()).Result;

            if (v != null)
            {
                //check if responsible not deleted or not active
                if (v.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && v.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false)
                {
                    //get responsible id for view use
                    ViewBag.RESPO_ID = v.WORKFLOW_RESPO_CODE;
                    //get workflow type for view use
                    ViewBag.RESPO_STATE = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID;
                    //get responsible code for controller use
                    RESPO_CODE = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE;
                    //workfloid id for controller use
                    WORKFLOWID = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value;
                    //unitcode of resonsible for controller use
                    STATION_CODE = v.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE.Value;
                    //get unitcode of the user for view use
                    ViewBag.STATION_CODE_CHECK = v.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE.Value;
                    //get force id
                    FORCE_ID = v.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.FORCE_ID.Value;
                }
                else
                {
                    ViewBag.RESPO_STATE = 0;
                    ViewBag.STATION_CODE_CHECK = 0;
                   

                }

            }
            else
            {
                ViewBag.RESPO_STATE = 0;
                ViewBag.STATION_CODE_CHECK = 0;
                

            }


        }



        // this view willl show link tabs
        public ActionResult Index()
        {
           
            return View();
        }

        // this view for controlling comments of this type of permits
        public ActionResult Comments(int? id)
        {
            ViewBag.activetab = "Comments";

            if (id == null)
            {
                return NotFound();
            }
            RA42_AUTHORIZATION_PASS_DTL rA42_AUTHORIZATION_PASS_DTL = db.RA42_AUTHORIZATION_PASS_DTL.Find(id);
            if (rA42_AUTHORIZATION_PASS_DTL == null)
            {
                return NotFound();
            }

            // herre we check if the normal user who dont't have aouthority is owen the current permit by comparing service number
            if (ViewBag.RESPO_STATE == 0)
            {
                if (rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER != currentUser)
                {
                    // admin can view every thing
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {

                        return NotFound();
                        }
                    
                }
            }
            else
            {
               



                if (rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER == currentUser)
                {

                }
                else
                {
                    // here if the user have authority, we check if the permit is in the same stage with the user
                    if (ViewBag.RESPO_STATE != rA42_AUTHORIZATION_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                    {
                        if (ViewBag.ADMIN != true)
                        {
                            return NotFound();
                        }
                    }
                }
            }
            //get all comments for this permit 
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            return View(rA42_AUTHORIZATION_PASS_DTL);
        }

        // POST comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Comments(RA42_AUTHORIZATION_PASS_DTL rA42_AUTHORIZATION_PASS_DTL, string COMMENT)
        {
            ViewBag.activetab = "Comments";
            var general_data = db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.AUTHORAIZATION_CODE == rA42_AUTHORIZATION_PASS_DTL.AUTHORAIZATION_CODE).FirstOrDefault();



            

            //add comments
            if (COMMENT.Length > 0)
            {
                RA42_COMMENTS_MST rA42_COMMENT = new RA42_COMMENTS_MST();
                rA42_COMMENT.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_COMMENT.PASS_ROW_CODE = rA42_AUTHORIZATION_PASS_DTL.AUTHORAIZATION_CODE;
                rA42_COMMENT.CRD_BY = currentUser;
                rA42_COMMENT.CRD_DT = DateTime.Now;
                rA42_COMMENT.COMMENT = COMMENT;
                db.RA42_COMMENTS_MST.Add(rA42_COMMENT);
                db.SaveChanges();
                AddToast(new Toast("",
                  GetResourcesValue("add_comment_success"),
                  "green"));

            }
            //get all comments for this permit 
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_AUTHORIZATION_PASS_DTL.AUTHORAIZATION_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            return View(rA42_AUTHORIZATION_PASS_DTL);


        }

        // this view for choosing how to create permit 
        public ActionResult Choosecreatetype()
        {
            ViewBag.activetab = "Privatepass";
            return View();
        }
       
        // this view for printing card
        [HttpGet]
        public ActionResult Card(int? id)
        {
            ViewBag.activetab = "card";

            if (id == null)
            {
               return NotFound();
            }
            //check id of permit
            RA42_AUTHORIZATION_PASS_DTL rA42_AUTHORIZATION_PASS_DTL = db.RA42_AUTHORIZATION_PASS_DTL.Find(id);
            if (rA42_AUTHORIZATION_PASS_DTL == null)
            {
               return NotFound();
            }

            
           
               
                // here if the user have authority, we check if the permit is in the same stage with the user

                if (ViewBag.RESPO_STATE != rA42_AUTHORIZATION_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                    {
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    return NotFound();
                        }
                    }
                
            
           //get prsonal image 
            ViewBag.PERSONAL_IMAGE = rA42_AUTHORIZATION_PASS_DTL.PERSONAL_IMAGE;
            //get status of the permit (rejected - in progress - completed)
            ViewBag.STATUS = rA42_AUTHORIZATION_PASS_DTL.STATUS;
            return View(rA42_AUTHORIZATION_PASS_DTL);
        }




        // View all permits from every station, this section is for Administrator (مشرف النظام)
        [HttpPost]
        public JsonResult GetList()
        {
            db.Configuration.ProxyCreationEnabled = false;

            //Server Side Parameter
            int start = Convert.ToInt32(Request["start"]);
            int length = Convert.ToInt32(Request["length"]);
            string searchValue = Request["search[value]"];
            string sortColumnName = Request["columns[" + Request["order[0][column]"] + "][name]"];
            string sortDirection = Request["order[0][dir]"];

            //List<RA42_VECHILE_PASS_DTL> empList = new List<RA42_VECHILE_PASS_DTL>();

            // empList = db.RA42_VECHILE_PASS_DTL.OrderByDescending(a => a.VECHILE_PASS_CODE).ToList<RA42_VECHILE_PASS_DTL>();
            var empList = db.RA42_AUTHORIZATION_PASS_DTL.Select(a => new
            {
                AUTHORAIZATION_CODE = a.AUTHORAIZATION_CODE,
                SERVICE_NUMBER = (a.SERVICE_NUMBER != null ? a.SERVICE_NUMBER : " "),
                CIVIL_NUMBER = (a.CIVIL_NUMBER != null ? a.CIVIL_NUMBER : " "),
                PERSONAL_IMAGE = a.PERSONAL_IMAGE,
                RANK_A = (a.RANK_A != null ? a.RANK_A : " "),
                RANK_E = (a.RANK_E != null ? a.RANK_E : " "),
                NAME_A = (a.NAME_A != null ? a.NAME_A : " "),
                NAME_E = (a.NAME_E != null ? a.NAME_A : " "),
                PHONE_NUMBER = (a.PHONE_NUMBER != null ? a.PHONE_NUMBER : " "),
                GSM = (a.GSM != null ? a.GSM : " "),
                PURPOSE_OF_PASS = (a.PURPOSE_OF_PASS != null ? a.PURPOSE_OF_PASS : " "),
                PROFESSION_A = (a.PROFESSION_A != null ? a.PROFESSION_A : " "),
                PROFESSION_E = (a.PROFESSION_E != null ? a.PROFESSION_E : " "),
                STATION_CODE = a.STATION_CODE,
                STATION_A = a.RA42_STATIONS_MST.STATION_NAME_A,
                STATION_E = a.RA42_STATIONS_MST.STATION_NAME_E,
                RESPONSEPLE_NAME = a.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                RESPONSEPLE_NAME_E = a.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E,
                STEP_NAME = a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME,
                STEP_NAME_E = a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E,
                STATUS = a.STATUS,
                REJECTED = a.REJECTED,
                DLT_STS = a.DLT_STS,
                ISPRINTED = a.ISPRINTED,
                DATE_FROM = a.DATE_FROM,
                DATE_TO = a.DATE_TO,
                COMMENTS = a.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == a.AUTHORAIZATION_CODE).Count()


            }
             ).ToList();
            int totalrows = empList.Count;
            if (!string.IsNullOrEmpty(searchValue))//filter
            {
               
                if (searchValue.Contains("منتهي"))
                {
                    empList = empList.Where(z => z.DATE_TO < DateTime.Today).ToList();
                }
                else
                {
                    //empList = empList.
                    //Where(x => x.SERVICE_NUMBER == searchValue || x.NAME_A == searchValue || x.PHONE_NUMBER == searchValue || x.GSM == searchValue || x.PLATE_NUMBER == searchValue || x.STATION_CODE == STATION_CODE).ToList<RA42_VECHILE_PASS_DTL>();
                    empList = empList.
                Where(x => x.SERVICE_NUMBER.Contains(searchValue) || x.CIVIL_NUMBER.Contains(searchValue) 
                || x.NAME_A.Contains(searchValue) || x.NAME_E.Contains(searchValue) || x.RANK_A.Contains(searchValue)
                || x.RANK_E.Contains(searchValue) || x.PROFESSION_A.Contains(searchValue) || x.PROFESSION_E.Contains(searchValue) 
                || x.PHONE_NUMBER.Contains(searchValue) || x.PURPOSE_OF_PASS.Contains(searchValue) || x.GSM.Contains(searchValue) 
                || x.STEP_NAME.Contains(searchValue) || x.STATION_A == searchValue).ToList();
                }
            }
            int totalrowsafterfiltering = empList.Count;
            //sorting
            //empList = empList.OrderBy(sortColumnName + " " + sortDirection).ToList<RA42_VECHILE_PASS_DTL>();
            empList = empList.OrderBy(sortColumnName + " " + sortDirection).ToList();
            //paging
            //empList = empList.Skip(start).Take(length).ToList<RA42_VECHILE_PASS_DTL>();
            empList = empList.Skip(start).Take(length).ToList();


            return Json(new { data = empList, draw = Request["draw"], recordsTotal = totalrows, recordsFiltered = totalrowsafterfiltering }, JsonRequestBehavior.AllowGet);


        }


        public ActionResult Allpasses()
        {

            ViewBag.activetab = "Allpasses";
            var rA42_AUTHORIZATION_PASS_DTL = db.RA42_AUTHORIZATION_PASS_DTL.OrderByDescending(a => a.AUTHORAIZATION_CODE);
            return View(rA42_AUTHORIZATION_PASS_DTL.ToList());
        }

       

        // view permits for autho person (المنسق الأمني)
        public ActionResult Authopasses()
        {
            
            ViewBag.activetab = "Authopasses";
            if(ViewBag.RESPO_STATE == 5 && STATION_CODE == 22)
            {
                var rA42_AUTHORIZATION_PASS_DTL = db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID).OrderByDescending(a => a.AUTHORAIZATION_CODE);
                return View(rA42_AUTHORIZATION_PASS_DTL.ToList());
            }
            else
            {
               return NotFound();
                
            }
          
        }
        // view new permits for permit cells and manager of directorate
        public async Task<ActionResult> Newpasses()
        {
           
            ViewBag.activetab = "Newpasses";
            if(ViewBag.RESPO_STATE == 6 && STATION_CODE == 22)
            {
                var rA42_AUTHORIZATION_PASS_DTL = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID && a.DLT_STS != true).OrderByDescending(a => a.AUTHORAIZATION_CODE).ToListAsync();
                return View(rA42_AUTHORIZATION_PASS_DTL);

            }
            if(ViewBag.RESPO_STATE == 4 && STATION_CODE != 22)
            {
                var rA42_AUTHORIZATION_PASS_DTL = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true ).OrderByDescending(a => a.AUTHORAIZATION_CODE).ToListAsync();
                return View(rA42_AUTHORIZATION_PASS_DTL);
            }

            if (ViewBag.RESPO_STATE == 4)
            {
                var rA42_AUTHORIZATION_PASS_DTL = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true  && a.ISPRINTED != true).OrderByDescending(a => a.AUTHORAIZATION_CODE).ToListAsync();
                return View(rA42_AUTHORIZATION_PASS_DTL);
            }
            if (ViewBag.RESPO_STATE == 3 && STATION_CODE == 22)
            {
                var rA42_AUTHORIZATION_PASS_DTL = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID  && a.DLT_STS != true  && a.STATUS != true).OrderByDescending(a => a.AUTHORAIZATION_CODE).ToListAsync();
                return View(rA42_AUTHORIZATION_PASS_DTL);
            }
           return NotFound();
           
        }

        public async Task<ActionResult> ToPrint()
        {

            ViewBag.activetab = "ToPrint";
            
            if (ViewBag.RESPO_STATE == 3 && STATION_CODE == 22)
            {
                var rA42_AUTHORIZATION_PASS_DTL = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID && a.DLT_STS != true && a.STATUS == true && a.ISPRINTED !=true).OrderByDescending(a => a.AUTHORAIZATION_CODE).ToListAsync();
                return View(rA42_AUTHORIZATION_PASS_DTL);
            }
            return NotFound();

        }
        //view printed permits for permits cell
        public async Task<ActionResult> Printed()
        {

            ViewBag.activetab = "Printed";
           
            if (ViewBag.RESPO_STATE == 3 && STATION_CODE == 22)
            {
                var rA42_AUTHORIZATION_PASS_DTL = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID && a.DLT_STS != true  && a.ISPRINTED == true).OrderByDescending(a => a.AUTHORAIZATION_CODE).ToListAsync();
                return View(rA42_AUTHORIZATION_PASS_DTL);
            }
            return NotFound();

        }
       
        // permit details 
        public ActionResult Details(int? id)
        {

            ViewBag.activetab = "details";

            if (id == null)
            {
               return NotFound();
            }
            RA42_AUTHORIZATION_PASS_DTL rA42_AUTHORIZATION_PASS_DTL = db.RA42_AUTHORIZATION_PASS_DTL.Find(id);
            if (rA42_AUTHORIZATION_PASS_DTL == null)
            {
               return NotFound();
            }

            //check if user is authorized
            if (ViewBag.RESPO_STATE == 0)
            {
                if (rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER != currentUser)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }

            var v = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = v;
            ViewBag.PERSONAL_IMAGE = rA42_AUTHORIZATION_PASS_DTL.PERSONAL_IMAGE;
            ViewBag.STATUS = rA42_AUTHORIZATION_PASS_DTL.STATUS;
            return View(rA42_AUTHORIZATION_PASS_DTL);

        }

        //this view is to create new permit and this for some persons who have aouthority in the system
        public ActionResult Supercreate()
        {
            
            ViewBag.activetab = "Supercreate";
            if (Language.GetCurrentLang() == "en")
            {
                //here to detrmine the station, where the user will create the permit
                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();
                ViewBag.STATION_NAME = " in " + check_unit.STATION_NAME_E;

                // here to detrmine who is autho resonsible to proccess to permit
                int workflow_id = 5;
                if (ViewBag.RESPO_STATE == 6)
                {
                    workflow_id = 6;
                }
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    return View();

                }
            }
            else
            {
                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();
                ViewBag.STATION_NAME = " في " + check_unit.STATION_NAME_A;
                int workflow_id = 5;
                if(ViewBag.RESPO_STATE == 6)
                {
                    workflow_id = 6;
                }
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    return View();

                }
            }
               
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Supercreate(RA42_AUTHORIZATION_PASS_DTL rA42_AUTHORIZATION_PASS_DTL, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "Supercreate";
            if (Language.GetCurrentLang() == "en")
            {          
                //get station name
                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();
                ViewBag.STATION_NAME = " in " + check_unit.STATION_NAME_E;
             
                int workflow_id = 5;
                //if current user workflow id = 6 show  manager responsible
                if (ViewBag.RESPO_STATE == 6)
                {
                    workflow_id = 6;
                }
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE);
                //if WORKFLOW_RESPO.Count == 0 that means no body setting in this position and the system should return error message no body in this posision
                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    return View(rA42_AUTHORIZATION_PASS_DTL);

                }
            }

            else
            {                
                //get station name
                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();
                ViewBag.STATION_NAME = " في " + check_unit.STATION_NAME_A;
                int workflow_id = 5;
                //if current user workflow id = 6 show  manager responsible

                if (ViewBag.RESPO_STATE == 6)
                {
                    workflow_id = 6;
                }
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE);
                //if WORKFLOW_RESPO.Count == 0 that means no body setting in this position and the system should return error message no body in this posision

                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    return View(rA42_AUTHORIZATION_PASS_DTL);

                }
            }
          
            if ( ModelState.IsValid)
            {
                if (PERSONAL_IMAGE != null)
                {
                    try
                    {

                        // uploading personal image
                        // Verify that the user selected a file
                        if (PERSONAL_IMAGE != null && PERSONAL_IMAGE.ContentLength > 0)
                        {
                            // extract only the filename with extention
                            string fileName = Path.GetFileNameWithoutExtension(PERSONAL_IMAGE.FileName);
                            string extension = Path.GetExtension(PERSONAL_IMAGE.FileName);


                            // check the type of image 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {

                                fileName = "Profile_1_" + DateTime.Now.ToString("yymmssfff") + extension;
                               
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_AUTHORIZATION_PASS_DTL);
                                }
                                rA42_AUTHORIZATION_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                // error if the file is not image 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_AUTHORIZATION_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                
                // inserting data in the table RA42_AUTHORIZATION_PASS_DTL
                
                rA42_AUTHORIZATION_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER = rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER.ToUpper();
                rA42_AUTHORIZATION_PASS_DTL.CARD_FOR_CODE = 1;
               
                //to get data from API (HRMS/MIMS)
                User permit = null;
                Task<User> callTask2 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask2.Wait();
                permit = callTask2.Result;

                
                rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE;
                
                rA42_AUTHORIZATION_PASS_DTL.CRD_BY = currentUser;
                rA42_AUTHORIZATION_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_AUTHORIZATION_PASS_DTL.UPD_BY = currentUser;
                rA42_AUTHORIZATION_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_AUTHORIZATION_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_AUTHORIZATION_PASS_DTL.REJECTED = false;
                //check WORKFLOW type to insert data 

                if (ViewBag.RESPO_STATE == 4)
                {
                    rA42_AUTHORIZATION_PASS_DTL.ISOPENED = false;


                }

                if (ViewBag.RESPO_STATE == 3 && ViewBag.UNITCODE == 22)
                {
                    rA42_AUTHORIZATION_PASS_DTL.ISOPENED = true;
                    rA42_AUTHORIZATION_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_AUTHORIZATION_PASS_DTL.PERMIT_RANK = permit.NAME_RANK_A;
                    rA42_AUTHORIZATION_PASS_DTL.PERMIT_NAME = permit.NAME_EMP_A;
                    rA42_AUTHORIZATION_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                    
                }

                //check WORKFLOW type to insert data 

                if (ViewBag.RESPO_STATE == 5  && ViewBag.UNITCODE == 22)
                {
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 6 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AUTHORIZATION_PASS_DTL);

                    }
                    rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    rA42_AUTHORIZATION_PASS_DTL.ISOPENED = true;
                    rA42_AUTHORIZATION_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_AUTHORIZATION_PASS_DTL.APPROVAL_RANK = permit.NAME_RANK_A;
                    rA42_AUTHORIZATION_PASS_DTL.APPROVAL_NAME = permit.NAME_EMP_A;
                    rA42_AUTHORIZATION_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                }
                //check WORKFLOW type to insert data 

                if (ViewBag.RESPO_STATE == 6 && ViewBag.UNITCODE == 22)
                {
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AUTHORIZATION_PASS_DTL);

                    }
                    rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    rA42_AUTHORIZATION_PASS_DTL.BARCODE = rA42_AUTHORIZATION_PASS_DTL.BARCODE;
                    rA42_AUTHORIZATION_PASS_DTL.STATUS = true;
                    rA42_AUTHORIZATION_PASS_DTL.ISOPENED = true;
                    rA42_AUTHORIZATION_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_AUTHORIZATION_PASS_DTL.AUTHO_RANK = permit.NAME_RANK_A;
                    rA42_AUTHORIZATION_PASS_DTL.AUTHO_NAME = permit.NAME_EMP_A;
                    rA42_AUTHORIZATION_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                }
                //saving data
                db.RA42_AUTHORIZATION_PASS_DTL.Add(rA42_AUTHORIZATION_PASS_DTL);
                db.SaveChanges();
                //show succssful message
                AddToast(new Toast("",
                   GetResourcesValue("success_create_message"),
                   "green"));
                return RedirectToAction("Index");
            }

           //show error message
            AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
            return View(rA42_AUTHORIZATION_PASS_DTL);
        }
        // This view is for search for somone inside HRMS/MIMS
        public ActionResult Search()
        {
            
            ViewBag.activetab = "Search";
            if (Language.GetCurrentLang() == "en")
            {
                //here to detrmine the station, where the user will create the permit
                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();
                ViewBag.STATION_NAME = " in " + check_unit.STATION_NAME_E;

                // here to detrmine who is autho resonsible to proccess to permit
                int workflow_id = 5;
                if (ViewBag.RESPO_STATE == 6)
                {
                    workflow_id = 6;
                }
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                //if WORKFLOW_RESPO.Count == 0 that means no body setting in this position and the system should return error message no body in this posision

                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    return View();

                }
            }
            else
            {
                //check station name 
                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();
                ViewBag.STATION_NAME = " في " + check_unit.STATION_NAME_A;
                int workflow_id = 5;
                //check current user workflow id
                if (ViewBag.RESPO_STATE == 6)
                {
                    workflow_id = 6;
                }
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                //if WORKFLOW_RESPO.Count == 0 that means no body setting in this position and the system should return error message no body in this posision

                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    return View();

                }
            }
            return View();
        }

        // GET: RA42_AUTHORIZATION_PASS_DTL/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Search(RA42_AUTHORIZATION_PASS_DTL rA42_AUTHORIZATION_PASS_DTL, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "Search";
            if (Language.GetCurrentLang() == "en")
            {     
                //get unit name      
                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();
                ViewBag.STATION_NAME = " in " + check_unit.STATION_NAME_E;
                int workflow_id = 5;
                //check responsible workflow id 
                if (ViewBag.RESPO_STATE == 6)
                {
                    workflow_id = 6;
                }
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE);
                //if WORKFLOW_RESPO.Count == 0 that means no body setting in this position and the system should return error message no body in this posision

                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    return View(rA42_AUTHORIZATION_PASS_DTL);

                }
            }

            else
            {
                //get station name 
                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();
                ViewBag.STATION_NAME = " في " + check_unit.STATION_NAME_A;
                int workflow_id = 5;
                if (ViewBag.RESPO_STATE == 6)
                {
                    workflow_id = 6;
                }
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE);
                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    return View(rA42_AUTHORIZATION_PASS_DTL);

                }
            }
            //get data from HRMS/MIMS
            User user = null;
            Task<User> callTask = Task.Run(
                () => (new UserInfo()).getUserInfoFromAPI(rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER.ToUpper())
                );
            callTask.Wait();
            user = callTask.Result;
            if (user != null && ModelState.IsValid)
            {
                if (PERSONAL_IMAGE != null)
                {
                    try
                    {


                        // Verify that the user selected a file
                        if (PERSONAL_IMAGE != null && PERSONAL_IMAGE.ContentLength > 0)
                        {
                            // extract only the filename with extention
                            string fileName = Path.GetFileNameWithoutExtension(PERSONAL_IMAGE.FileName);
                            string extension = Path.GetExtension(PERSONAL_IMAGE.FileName);


                            //check file type
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {

                                fileName = "Profile_1_" + DateTime.Now.ToString("yymmssfff") + extension;

                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_AUTHORIZATION_PASS_DTL);
                                }
                                rA42_AUTHORIZATION_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //error if the file not image
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (PNG,JPG,GIF)";
                                return View(rA42_AUTHORIZATION_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }

               
                rA42_AUTHORIZATION_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER = rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER.ToUpper();
                rA42_AUTHORIZATION_PASS_DTL.RANK_A = user.NAME_RANK_A;
                rA42_AUTHORIZATION_PASS_DTL.RANK_E = user.NAME_RANK_E;
                rA42_AUTHORIZATION_PASS_DTL.NAME_A = user.NAME_EMP_A;
                rA42_AUTHORIZATION_PASS_DTL.NAME_E = user.NAME_EMP_E;
                rA42_AUTHORIZATION_PASS_DTL.PROFESSION_A = user.NAME_TRADE_A;
                rA42_AUTHORIZATION_PASS_DTL.PROFESSION_E = user.NAME_TRADE_E;
                rA42_AUTHORIZATION_PASS_DTL.UNIT_A = user.NAME_UNIT_A;
                rA42_AUTHORIZATION_PASS_DTL.UNIT_E = user.NAME_UNIT_E;
                rA42_AUTHORIZATION_PASS_DTL.CARD_FOR_CODE = 1;
                rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE;
                rA42_AUTHORIZATION_PASS_DTL.CRD_BY = currentUser;
                rA42_AUTHORIZATION_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_AUTHORIZATION_PASS_DTL.UPD_BY = currentUser;
                rA42_AUTHORIZATION_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_AUTHORIZATION_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_AUTHORIZATION_PASS_DTL.REJECTED = false;

                // here to check WORKFLOW type of the current user to redirect permit to someone 
                User permit = null;
                Task<User> callTask2 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask2.Wait();
                permit = callTask2.Result;


                if (ViewBag.RESPO_STATE == 4)
                {
                    rA42_AUTHORIZATION_PASS_DTL.ISOPENED = false;
                  

                }

                if (ViewBag.RESPO_STATE == 3 && STATION_CODE == 22)
                {
                    rA42_AUTHORIZATION_PASS_DTL.ISOPENED = true;
                    rA42_AUTHORIZATION_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_AUTHORIZATION_PASS_DTL.PERMIT_RANK = permit.NAME_RANK_A;
                    rA42_AUTHORIZATION_PASS_DTL.PERMIT_NAME = permit.NAME_EMP_A;
                    rA42_AUTHORIZATION_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;

                }


                if (ViewBag.RESPO_STATE == 5 && STATION_CODE == 22)
                {
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 6 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AUTHORIZATION_PASS_DTL);

                    }
                    rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    rA42_AUTHORIZATION_PASS_DTL.ISOPENED = true;
                    rA42_AUTHORIZATION_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_AUTHORIZATION_PASS_DTL.APPROVAL_RANK = permit.NAME_RANK_A;
                    rA42_AUTHORIZATION_PASS_DTL.APPROVAL_NAME = permit.NAME_EMP_A;
                    rA42_AUTHORIZATION_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                }

                if (ViewBag.RESPO_STATE == 6 && STATION_CODE == 22)
                {
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AUTHORIZATION_PASS_DTL);

                    }
                    rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    rA42_AUTHORIZATION_PASS_DTL.BARCODE = rA42_AUTHORIZATION_PASS_DTL.BARCODE;
                    rA42_AUTHORIZATION_PASS_DTL.STATUS = true;
                    rA42_AUTHORIZATION_PASS_DTL.ISOPENED = true;
                    rA42_AUTHORIZATION_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_AUTHORIZATION_PASS_DTL.AUTHO_RANK = permit.NAME_RANK_A;
                    rA42_AUTHORIZATION_PASS_DTL.AUTHO_NAME = permit.NAME_EMP_A;
                    rA42_AUTHORIZATION_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                }
                db.RA42_AUTHORIZATION_PASS_DTL.Add(rA42_AUTHORIZATION_PASS_DTL);
                db.SaveChanges();
                AddToast(new Toast("",
                   GetResourcesValue("success_create_message"),
                   "green"));
                return RedirectToAction("Index");
            }


            AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
            return View(rA42_AUTHORIZATION_PASS_DTL);
        }
        // create new permit/ this section is for responsible who can create permit for himself
        public ActionResult Create()
        {

            var url = Url.RequestContext.RouteData.Values["id"];
            //int unit = 0;

            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //check station 
                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == id).FirstOrDefault();
                if (check_unit != null)
                {

                    if (Language.GetCurrentLang() == "en")
                    {
                        ViewBag.STATION_NAME = "in " + check_unit.STATION_NAME_E;
                    }
                    else
                    {
                        ViewBag.STATION_NAME = "في " + check_unit.STATION_NAME_A;

                    }
                    ViewBag.HQ_UNIT = id;
                    var station = int.Parse(id.ToString());
                    STATION_CODE = station;

                }
                  }
            else
            {


                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();

                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.STATION_NAME = "in " + check_unit.STATION_NAME_E;
                }
                else
                {
                    ViewBag.STATION_NAME = "في " + check_unit.STATION_NAME_A;

                }



            }
            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();

            ViewBag.activetab = "Create";
            ViewBag.Service_No = currentUser;

            if (Language.GetCurrentLang() == "en")
            {
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 5 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    return View();

                }
            }
            else
            {
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 5 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    return View();

                }
            }



            
            return View();
        }

        // POST: RA42_AUTHORIZATION_PASS_DTL/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(RA42_AUTHORIZATION_PASS_DTL rA42_AUTHORIZATION_PASS_DTL, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "Create";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            //int unit = 0;

            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //check station 
                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == id).FirstOrDefault();
                if (check_unit != null)
                {

                    if (Language.GetCurrentLang() == "en")
                    {
                        ViewBag.STATION_NAME = "in " + check_unit.STATION_NAME_E;
                    }
                    else
                    {
                        ViewBag.STATION_NAME = "في " + check_unit.STATION_NAME_A;

                    }
                    ViewBag.HQ_UNIT = id;
                    var station = int.Parse(id.ToString());
                    STATION_CODE = station;

                }
                         }
            else
            {


                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();

                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.STATION_NAME = "in " + check_unit.STATION_NAME_E;
                }
                else
                {
                    ViewBag.STATION_NAME = "في " + check_unit.STATION_NAME_A;

                }



            }

            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();


            if (Language.GetCurrentLang() == "en")
            {
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 5 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE);
                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    

                }
            }
            else
            {
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 5 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME",rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE);
                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                   

                }
            }


            if (rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE == null)
            {
                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                return View(rA42_AUTHORIZATION_PASS_DTL);

            }
            if (ModelState.IsValid)
            {
                if (PERSONAL_IMAGE != null)
                {
                    try
                    {


                        // Verify that the user selected a file
                        if (PERSONAL_IMAGE != null && PERSONAL_IMAGE.ContentLength > 0)
                        {
                            // extract only the filename with extention
                            string fileName = Path.GetFileNameWithoutExtension(PERSONAL_IMAGE.FileName);
                            string extension = Path.GetExtension(PERSONAL_IMAGE.FileName);


                            //check personal image file type 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {

                                fileName = "Profile_1_" + DateTime.Now.ToString("yymmssfff") + extension;

                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_AUTHORIZATION_PASS_DTL);
                                }
                                rA42_AUTHORIZATION_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_AUTHORIZATION_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                var currentUserInfo = (new UserInfo()).getUserInfo();

                rA42_AUTHORIZATION_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_AUTHORIZATION_PASS_DTL.CARD_FOR_CODE = 1;
                rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER = currentUser;
                rA42_AUTHORIZATION_PASS_DTL.RANK_A = currentUserInfo["user_rank_ar"];
                rA42_AUTHORIZATION_PASS_DTL.RANK_E = currentUserInfo["user_rank_en"];
                rA42_AUTHORIZATION_PASS_DTL.NAME_A = currentUserInfo["user_name_ar"];
                rA42_AUTHORIZATION_PASS_DTL.NAME_E = currentUserInfo["user_name_en"];
                rA42_AUTHORIZATION_PASS_DTL.PROFESSION_A = currentUserInfo["user_force_trade_ar"];
                rA42_AUTHORIZATION_PASS_DTL.PROFESSION_E = currentUserInfo["user_force_trade_en"];
                rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE;
                rA42_AUTHORIZATION_PASS_DTL.CRD_BY = currentUser;
                rA42_AUTHORIZATION_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_AUTHORIZATION_PASS_DTL.UPD_BY = currentUser;
                rA42_AUTHORIZATION_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_AUTHORIZATION_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                db.RA42_AUTHORIZATION_PASS_DTL.Add(rA42_AUTHORIZATION_PASS_DTL);
                db.SaveChanges();
                AddToast(new Toast("",
                   GetResourcesValue("success_create_message"),
                   "green"));
                return RedirectToAction("Index");
                }

           
            AddToast(new Toast("",
                GetResourcesValue("error_create_message"),
                "red"));
            return View(rA42_AUTHORIZATION_PASS_DTL);
        }

        // here to proccess the permit 
        public ActionResult Edit(int? id)
        {
           
            ViewBag.activetab = "edit";

            if (id == null)
            {
               return NotFound();
            }
            RA42_AUTHORIZATION_PASS_DTL rA42_AUTHORIZATION_PASS_DTL = db.RA42_AUTHORIZATION_PASS_DTL.Find(id);
            if (rA42_AUTHORIZATION_PASS_DTL == null)
            {
               return NotFound();
            }

            if (ViewBag.RESPO_STATE == 0)
            {
                if (rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER != currentUser)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }
            else
            {
                if (rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != rA42_AUTHORIZATION_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            return NotFound();
                        }
                    }
                }
            }
            if (Language.GetCurrentLang() == "en")
            {
                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
            else
            {
                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
            //get comments
            var v = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id  && a.DLT_STS !=true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = v;
            //get personal image
            ViewBag.PERSONAL_IMAGE = rA42_AUTHORIZATION_PASS_DTL.PERSONAL_IMAGE;
            //get status of the permit, complete / rejected / in progress
            ViewBag.STATUS = rA42_AUTHORIZATION_PASS_DTL.STATUS;
            return View(rA42_AUTHORIZATION_PASS_DTL);
        }

        // POST: RA42_AUTHORIZATION_PASS_DTL/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string COMMENT, FormCollection form, RA42_AUTHORIZATION_PASS_DTL rA42_AUTHORIZATION_PASS_DTL, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "edit";
            //ceck if the permit id in the database
            var general_data = db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.AUTHORAIZATION_CODE == rA42_AUTHORIZATION_PASS_DTL.AUTHORAIZATION_CODE).FirstOrDefault();
            var cOMMENTS_MSTs = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_AUTHORIZATION_PASS_DTL.AUTHORAIZATION_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
            ViewBag.COMMENTS = cOMMENTS_MSTs;
            //get personal image

            ViewBag.PERSONAL_IMAGE = general_data.PERSONAL_IMAGE;
            //get status of the permit, complete / rejected / in progress
            ViewBag.STATUS = general_data.STATUS;

            if (Language.GetCurrentLang() == "en")
            {
                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
            else
            {
                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
            if (ModelState.IsValid)
            {
                //check the workflow id in the database
                var x = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                var currentUserInfo = (new UserInfo()).getUserInfo();
                if (PERSONAL_IMAGE != null)
                {
                    try
                    {


                        // Verify that the user selected a file
                        if (PERSONAL_IMAGE != null && PERSONAL_IMAGE.ContentLength > 0)
                        {
                            // extract only the filename with extention
                            string fileName = Path.GetFileNameWithoutExtension(PERSONAL_IMAGE.FileName);
                            string extension = Path.GetExtension(PERSONAL_IMAGE.FileName);


                            //check image type
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {

                                fileName = "Profile_1_" + DateTime.Now.ToString("yymmssfff") + extension;

                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_AUTHORIZATION_PASS_DTL);
                                }
                                rA42_AUTHORIZATION_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //error if the file not image
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_AUTHORIZATION_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                //adding new comments
                if (COMMENT.Length > 0)
                {
                    RA42_COMMENTS_MST rA42_COMMENT = new RA42_COMMENTS_MST();
                    rA42_COMMENT.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                    rA42_COMMENT.PASS_ROW_CODE = rA42_AUTHORIZATION_PASS_DTL.AUTHORAIZATION_CODE;
                    rA42_COMMENT.CRD_BY = currentUser;
                    rA42_COMMENT.CRD_DT = DateTime.Now;
                    rA42_COMMENT.COMMENT = COMMENT;
                    db.RA42_COMMENTS_MST.Add(rA42_COMMENT);


                }
                if (rA42_AUTHORIZATION_PASS_DTL.PERSONAL_IMAGE != null)
                {
                    general_data.PERSONAL_IMAGE = rA42_AUTHORIZATION_PASS_DTL.PERSONAL_IMAGE;
                }
                else
                {
                    general_data.PERSONAL_IMAGE = general_data.PERSONAL_IMAGE;

                }
                general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                //this section is for developer
                if (form["approvebtn"] != null && ViewBag.DEVELOPER == true)
                {

                    general_data.WORKFLOW_RESPO_CODE = rA42_AUTHORIZATION_PASS_DTL.WORKFLOW_RESPO_CODE;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    general_data.PERMIT_SN = general_data.PERMIT_SN;
                    general_data.PERMIT_NAME = general_data.PERMIT_NAME;
                    general_data.PERMIT_RANK = general_data.PERMIT_RANK;
                    general_data.PERMIT_APPROVISION_DATE = general_data.PERMIT_APPROVISION_DATE;
                    general_data.BARCODE = general_data.BARCODE;
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.SERVICE_NUMBER = rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.RANK_A = rA42_AUTHORIZATION_PASS_DTL.RANK_A;
                    general_data.RANK_E = rA42_AUTHORIZATION_PASS_DTL.RANK_E;
                    general_data.NAME_A = rA42_AUTHORIZATION_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_AUTHORIZATION_PASS_DTL.NAME_E;
                    general_data.PROFESSION_A = rA42_AUTHORIZATION_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_AUTHORIZATION_PASS_DTL.PROFESSION_E;
                    general_data.UNIT_A = rA42_AUTHORIZATION_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_AUTHORIZATION_PASS_DTL.UNIT_E;
                    general_data.PHONE_NUMBER = rA42_AUTHORIZATION_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_AUTHORIZATION_PASS_DTL.GSM;
                    general_data.PURPOSE_OF_PASS = rA42_AUTHORIZATION_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_AUTHORIZATION_PASS_DTL.REMARKS;
                    general_data.DATE_FROM = rA42_AUTHORIZATION_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_AUTHORIZATION_PASS_DTL.DATE_TO;
                    general_data.REJECTED = false;
                    general_data.ISOPENED = true;
                    general_data.ISPRINTED = false;
                    general_data.STATUS = general_data.STATUS;
                    db.Entry(general_data).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Allpasses");
                }


                if (x != null)
                {


                   


                    //workflow id is 3 who is (خلية التصاريح)
                    if (form["approvebtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 &&  x.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22)
                    {

                        if (general_data.STATUS == true)
                        {
                            general_data.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;
                        }
                        else
                        {
                            //redirect message to the Responsible who have workflow id number 5 (ركن 1 سياسة وخطط أمن )
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 5 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Edit", new { id = rA42_AUTHORIZATION_PASS_DTL.AUTHORAIZATION_CODE });

                            }
                            general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;

                        }
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.PERMIT_SN = currentUserInfo["user_sno"];
                        general_data.PERMIT_NAME = currentUserInfo["user_name_ar"];
                        general_data.PERMIT_RANK = currentUserInfo["user_rank_ar"];
                        general_data.PERMIT_APPROVISION_DATE = DateTime.Now;
                        general_data.BARCODE = general_data.BARCODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.SERVICE_NUMBER = rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.RANK_A = rA42_AUTHORIZATION_PASS_DTL.RANK_A;
                        general_data.RANK_E = rA42_AUTHORIZATION_PASS_DTL.RANK_E;
                        general_data.NAME_A = rA42_AUTHORIZATION_PASS_DTL.NAME_A;
                        general_data.NAME_E = rA42_AUTHORIZATION_PASS_DTL.NAME_E;
                        general_data.PROFESSION_A = rA42_AUTHORIZATION_PASS_DTL.PROFESSION_A;
                        general_data.PROFESSION_E = rA42_AUTHORIZATION_PASS_DTL.PROFESSION_E;
                        general_data.UNIT_A = rA42_AUTHORIZATION_PASS_DTL.UNIT_A;
                        general_data.UNIT_E = rA42_AUTHORIZATION_PASS_DTL.UNIT_E;
                        general_data.PHONE_NUMBER = rA42_AUTHORIZATION_PASS_DTL.PHONE_NUMBER;
                        general_data.GSM = rA42_AUTHORIZATION_PASS_DTL.GSM;
                        general_data.PURPOSE_OF_PASS = rA42_AUTHORIZATION_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_AUTHORIZATION_PASS_DTL.REMARKS;
                        general_data.DATE_FROM = rA42_AUTHORIZATION_PASS_DTL.DATE_FROM;
                        general_data.DATE_TO = rA42_AUTHORIZATION_PASS_DTL.DATE_TO;
                        general_data.REJECTED = false;
                        general_data.ISOPENED = true;
                        general_data.ISPRINTED = false;
                        general_data.STATUS = general_data.STATUS;
                        db.Entry(general_data).State = EntityState.Modified;
                        db.SaveChanges();
                        AddToast(new Toast("",
                        GetResourcesValue("success_update_message"),
                        "green"));
                        return RedirectToAction("Newpasses");
                    }


                    //workflow type is 4 who is (ضابط الأمن)

                    if (form["approvebtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4)
                    {

                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 5 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_AUTHORIZATION_PASS_DTL.AUTHORAIZATION_CODE });

                        }
                        general_data.AUTHO_SN = general_data.AUTHO_SN;
                        general_data.AUTHO_NAME = general_data.AUTHO_NAME;
                        general_data.AUTHO_RANK = general_data.AUTHO_RANK;
                        general_data.AUTHO_APPROVISION_DATE = general_data.AUTHO_APPROVISION_DATE;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.BARCODE = general_data.BARCODE;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.SERVICE_NUMBER = rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER;
                        general_data.RANK_A = rA42_AUTHORIZATION_PASS_DTL.RANK_A;
                        general_data.RANK_E = rA42_AUTHORIZATION_PASS_DTL.RANK_E;
                        general_data.NAME_A = rA42_AUTHORIZATION_PASS_DTL.NAME_A;
                        general_data.NAME_E = rA42_AUTHORIZATION_PASS_DTL.NAME_E;
                        general_data.PROFESSION_A = rA42_AUTHORIZATION_PASS_DTL.PROFESSION_A;
                        general_data.PROFESSION_E = rA42_AUTHORIZATION_PASS_DTL.PROFESSION_E;
                        general_data.UNIT_A = rA42_AUTHORIZATION_PASS_DTL.UNIT_A;
                        general_data.UNIT_E = rA42_AUTHORIZATION_PASS_DTL.UNIT_E;
                        general_data.PHONE_NUMBER = rA42_AUTHORIZATION_PASS_DTL.PHONE_NUMBER;
                        general_data.GSM = rA42_AUTHORIZATION_PASS_DTL.GSM;
                        general_data.PURPOSE_OF_PASS = rA42_AUTHORIZATION_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_AUTHORIZATION_PASS_DTL.REMARKS;
                        general_data.DATE_FROM = rA42_AUTHORIZATION_PASS_DTL.DATE_FROM;
                        general_data.DATE_TO = rA42_AUTHORIZATION_PASS_DTL.DATE_TO;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.REJECTED = false;
                        general_data.STATUS = general_data.STATUS;
                        general_data.ISPRINTED = false;
                        general_data.ISOPENED = false;

                        db.Entry(general_data).State = EntityState.Modified;
                        db.SaveChanges();
                        AddToast(new Toast("",
                        GetResourcesValue("success_update_message"),
                        "green"));
                        return RedirectToAction("Newpasses");
                    }
                    // if the user is (ركن 1 سياسة وخطط امن)
                    if (form["approvebtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 5 && x.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22)
                    {

                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 6 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_AUTHORIZATION_PASS_DTL.AUTHORAIZATION_CODE });

                        }
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.APPROVAL_SN = currentUserInfo["user_sno"];
                        general_data.APPROVAL_NAME = currentUserInfo["user_name_ar"];
                        general_data.APPROVAL_RANK = currentUserInfo["user_rank_ar"];
                        general_data.APPROVAL_APPROVISION_DATE = DateTime.Now;
                        general_data.SERVICE_NUMBER = rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER;
                        general_data.RANK_A = rA42_AUTHORIZATION_PASS_DTL.RANK_A;
                        general_data.RANK_E = rA42_AUTHORIZATION_PASS_DTL.RANK_E;
                        general_data.NAME_A = rA42_AUTHORIZATION_PASS_DTL.NAME_A;
                        general_data.NAME_E = rA42_AUTHORIZATION_PASS_DTL.NAME_E;
                        general_data.PROFESSION_A = rA42_AUTHORIZATION_PASS_DTL.PROFESSION_A;
                        general_data.PROFESSION_E = rA42_AUTHORIZATION_PASS_DTL.PROFESSION_E;
                        general_data.UNIT_A = rA42_AUTHORIZATION_PASS_DTL.UNIT_A;
                        general_data.UNIT_E = rA42_AUTHORIZATION_PASS_DTL.UNIT_E;
                        general_data.PHONE_NUMBER = rA42_AUTHORIZATION_PASS_DTL.PHONE_NUMBER;
                        general_data.GSM = rA42_AUTHORIZATION_PASS_DTL.GSM;
                        general_data.PURPOSE_OF_PASS = rA42_AUTHORIZATION_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_AUTHORIZATION_PASS_DTL.REMARKS;
                        general_data.DATE_FROM = rA42_AUTHORIZATION_PASS_DTL.DATE_FROM;
                        general_data.DATE_TO = rA42_AUTHORIZATION_PASS_DTL.DATE_TO;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.REJECTED = false;
                        general_data.ISOPENED = true;
                        general_data.ISPRINTED = general_data.ISPRINTED;
                        general_data.STATUS = general_data.STATUS;
                        db.Entry(general_data).State = EntityState.Modified;
                        db.SaveChanges();
                        AddToast(new Toast("",
                        GetResourcesValue("success_update_message"),
                        "green"));
                        return RedirectToAction("Authopasses");
                    }

                    //if the user is security manager 
                    if (form["approvebtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 6 && x.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22)
                    {

                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_AUTHORIZATION_PASS_DTL.AUTHORAIZATION_CODE });

                        }
                        general_data.AUTHO_SN = currentUserInfo["user_sno"];
                        general_data.AUTHO_NAME = currentUserInfo["user_name_ar"];
                        general_data.AUTHO_RANK = currentUserInfo["user_rank_ar"];
                        general_data.AUTHO_APPROVISION_DATE = DateTime.Now;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.BARCODE = rA42_AUTHORIZATION_PASS_DTL.BARCODE;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.SERVICE_NUMBER = rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER;
                        general_data.RANK_A = rA42_AUTHORIZATION_PASS_DTL.RANK_A;
                        general_data.RANK_E = rA42_AUTHORIZATION_PASS_DTL.RANK_E;
                        general_data.NAME_A = rA42_AUTHORIZATION_PASS_DTL.NAME_A;
                        general_data.NAME_E = rA42_AUTHORIZATION_PASS_DTL.NAME_E;
                        general_data.PROFESSION_A = rA42_AUTHORIZATION_PASS_DTL.PROFESSION_A;
                        general_data.PROFESSION_E = rA42_AUTHORIZATION_PASS_DTL.PROFESSION_E;
                        general_data.UNIT_A = rA42_AUTHORIZATION_PASS_DTL.UNIT_A;
                        general_data.UNIT_E = rA42_AUTHORIZATION_PASS_DTL.UNIT_E;
                        general_data.PHONE_NUMBER = rA42_AUTHORIZATION_PASS_DTL.PHONE_NUMBER;
                        general_data.GSM = rA42_AUTHORIZATION_PASS_DTL.GSM;
                        general_data.PURPOSE_OF_PASS = rA42_AUTHORIZATION_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_AUTHORIZATION_PASS_DTL.REMARKS;
                        general_data.DATE_FROM = rA42_AUTHORIZATION_PASS_DTL.DATE_FROM;
                        general_data.DATE_TO = rA42_AUTHORIZATION_PASS_DTL.DATE_TO;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.REJECTED = false;
                        general_data.STATUS = true;
                        general_data.ISPRINTED = general_data.ISPRINTED;
                        general_data.ISOPENED = general_data.ISOPENED;

                        db.Entry(general_data).State = EntityState.Modified;
                        db.SaveChanges();
                        AddToast(new Toast("",
                        GetResourcesValue("success_update_message"),
                        "green"));
                        return RedirectToAction("Newpasses");
                    }


                 
                    if (form["rejectbtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 5)
                    {

                     
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;
                        general_data.SERVICE_NUMBER = rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER;
                        general_data.RANK_A = rA42_AUTHORIZATION_PASS_DTL.RANK_A;
                        general_data.RANK_E = rA42_AUTHORIZATION_PASS_DTL.RANK_E;
                        general_data.NAME_A = rA42_AUTHORIZATION_PASS_DTL.NAME_A;
                        general_data.NAME_E = rA42_AUTHORIZATION_PASS_DTL.NAME_E;
                        general_data.PROFESSION_A = rA42_AUTHORIZATION_PASS_DTL.PROFESSION_A;
                        general_data.PROFESSION_E = rA42_AUTHORIZATION_PASS_DTL.PROFESSION_E;
                        general_data.UNIT_A = rA42_AUTHORIZATION_PASS_DTL.UNIT_A;
                        general_data.UNIT_E = rA42_AUTHORIZATION_PASS_DTL.UNIT_E;
                        general_data.PHONE_NUMBER = rA42_AUTHORIZATION_PASS_DTL.PHONE_NUMBER;
                        general_data.GSM = rA42_AUTHORIZATION_PASS_DTL.GSM;
                        general_data.PURPOSE_OF_PASS = rA42_AUTHORIZATION_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_AUTHORIZATION_PASS_DTL.REMARKS;
                        general_data.DATE_FROM = rA42_AUTHORIZATION_PASS_DTL.DATE_FROM;
                        general_data.DATE_TO = rA42_AUTHORIZATION_PASS_DTL.DATE_TO;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.REJECTED = true;
                        general_data.ISOPENED = false;

                        db.Entry(general_data).State = EntityState.Modified;
                        db.SaveChanges();
                        AddToast(new Toast("",
                        GetResourcesValue("success_update_message"),
                        "green"));
                        return RedirectToAction("Authopasses");
                    }
                    if (form["rejectbtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 6 )
                    {

                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == general_data.APPROVAL_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_AUTHORIZATION_PASS_DTL.AUTHORAIZATION_CODE });

                        }
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.SERVICE_NUMBER = rA42_AUTHORIZATION_PASS_DTL.SERVICE_NUMBER;
                        general_data.RANK_A = rA42_AUTHORIZATION_PASS_DTL.RANK_A;
                        general_data.RANK_E = rA42_AUTHORIZATION_PASS_DTL.RANK_E;
                        general_data.NAME_A = rA42_AUTHORIZATION_PASS_DTL.NAME_A;
                        general_data.NAME_E = rA42_AUTHORIZATION_PASS_DTL.NAME_E;
                        general_data.PROFESSION_A = rA42_AUTHORIZATION_PASS_DTL.PROFESSION_A;
                        general_data.PROFESSION_E = rA42_AUTHORIZATION_PASS_DTL.PROFESSION_E;
                        general_data.UNIT_A = rA42_AUTHORIZATION_PASS_DTL.UNIT_A;
                        general_data.UNIT_E = rA42_AUTHORIZATION_PASS_DTL.UNIT_E;
                        general_data.PHONE_NUMBER = rA42_AUTHORIZATION_PASS_DTL.PHONE_NUMBER;
                        general_data.GSM = rA42_AUTHORIZATION_PASS_DTL.GSM;
                        general_data.PURPOSE_OF_PASS = rA42_AUTHORIZATION_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_AUTHORIZATION_PASS_DTL.REMARKS;
                        general_data.DATE_FROM = rA42_AUTHORIZATION_PASS_DTL.DATE_FROM;
                        general_data.DATE_TO = rA42_AUTHORIZATION_PASS_DTL.DATE_TO;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.REJECTED = true;
                        db.Entry(general_data).State = EntityState.Modified;
                        db.SaveChanges();
                        AddToast(new Toast("",
                        GetResourcesValue("success_update_message"),
                        "green"));
                        return RedirectToAction("Newpasses");
                    }
                }
                else
                {
                  
                }

            }
            
            //show error message if there is somthing wrong
            AddToast(new Toast("",
                GetResourcesValue("error_update_message"),
                "red"));
            return View(rA42_AUTHORIZATION_PASS_DTL);
        }

        // delete permit by administrator
        public ActionResult Delete(int? id)
        {
           
            
                ViewBag.activetab = "delete";

                if (id == null)
                {
                    
                       return NotFound();
                    
                }
                RA42_AUTHORIZATION_PASS_DTL aUTHORIZATION_PASS_DTL = db.RA42_AUTHORIZATION_PASS_DTL.Find(id);
                if (aUTHORIZATION_PASS_DTL == null)
                {
                   
                       return NotFound();
                    
                }

                if (ViewBag.RESPO_STATE <= 1)
                {
                return NotFound();
                }
                else
                {
                   
                        //if (ViewBag.RESPO_STATE != aUTHORIZATION_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                        //{
                        //if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        //{
                        //    return NotFound();
                        //    }
                        //}
                    
                }

                
             
              
                return View(aUTHORIZATION_PASS_DTL);
            
            
        }

        // confirm deleting 
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var general_data = db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.AUTHORAIZATION_CODE == id).FirstOrDefault();

            if (general_data != null)
            {
                general_data.UPD_BY = currentUser;
                general_data.UPD_DT = DateTime.Now;
                general_data.DLT_STS = true;
                db.Entry(general_data).State = EntityState.Modified;
                db.SaveChanges();
                AddToast(new Toast("",
                GetResourcesValue("success_delete_message"),
                "green"));
                if (ViewBag.RESPO_STATE <= 1)
                {
                    return RedirectToAction("Index", "MyPasses");

                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            AddToast(new Toast("",
                 GetResourcesValue("Dont_have_permissions_to_dlt"),
                 "red"));
            if (ViewBag.RESPO_STATE <= 1)
            {
                return RedirectToAction("Index", "MyPasses");

            }
            else
            {
                return RedirectToAction("Index");
            }

        }

        //delete personal image 
        [HttpPost]
        public JsonResult DeleteImage(int id)
        {
            bool result = false;
            var general_data = db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.AUTHORAIZATION_CODE == id).FirstOrDefault();

            if (general_data != null)
            {


                general_data.UPD_BY = currentUser;
                general_data.UPD_DT = DateTime.Now;
                general_data.PERSONAL_IMAGE = null;
                db.Entry(general_data).State = EntityState.Modified;
                db.SaveChanges();
                string filpath = "~/Profiles/" + general_data.PERSONAL_IMAGE;
                general.RemoveFileFromServer(filpath);
                AddToast(new Toast("",
                GetResourcesValue("success_delete_message"),
               "green"));
                result = true;
            }

            return Json(result, JsonRequestBehavior.AllowGet);


        }

        //print card 
        [HttpPost]
        public JsonResult PrintById(int id, string type)
        {

            bool result = false;
            var general_data = db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.AUTHORAIZATION_CODE == id).FirstOrDefault();

            if (general_data != null)
            {


                general_data.UPD_BY = currentUser;
                general_data.UPD_DT = DateTime.Now;
                general_data.ISPRINTED = true;
                db.Entry(general_data).State = EntityState.Modified;
                db.SaveChanges();
                var checkifPrint = db.RA42_PRINT_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.PASS_ROW_CODE == id && a.DLT_STS != true).ToList();
                if (checkifPrint.Count > 0)
                {
                    foreach (var item in checkifPrint)
                    {
                        item.DLT_STS = true;
                        item.UPD_BY = currentUser;
                        item.UPD_DT = DateTime.Now;
                        db.Entry(item).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
                RA42_PRINT_MST rA42_PRINT = new RA42_PRINT_MST();
                rA42_PRINT.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_PRINT.PASS_ROW_CODE = id;
                rA42_PRINT.PRINT_TYPE = type;
                rA42_PRINT.PRINT_BY = currentUser;
                rA42_PRINT.PRINT_DT = DateTime.Now;
                db.RA42_PRINT_MST.Add(rA42_PRINT);
                db.SaveChanges();

                result = true;
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

        public ActionResult NotFound()
        {
            return RedirectToAction("NotFound", "Home");
        }

    }
}
