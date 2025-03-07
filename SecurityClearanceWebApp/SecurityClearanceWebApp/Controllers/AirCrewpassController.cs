using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.Globalization;
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
using System.Linq.Dynamic;

using SecurityClearanceWebApp.Util;

namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]

    //this is scurity ppermit master, this is responsible for private permit for employees companies worker etc...
    public class AirCrewpassController : Controller
    {
        //get db connection
        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class to use some of its functions 
        private GeneralFunctions general = new GeneralFunctions();
        //set title for the controller from resources
        string title = Resources.Passes.ResourceManager.GetString("AirCrew" + "_" + "ar");
        //identify these variables to use it in whole controller
        private int STATION_CODE = 0;
        private int WORKFLOWID = 0;
        private int RESPO_CODE = 0;
        private int FORCE_ID = 0;
        private int ACCESS_TYPE_CODE = 10;
        public AirCrewpassController()
        {
            ViewBag.Managepasses = "Managepasses";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "AirCrewpass";
            //set icon of the controller from fontawsome library 
            ViewBag.controllerIconClass = "fa fa-plane";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Passes.ResourceManager.GetString("AirCrew" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;
            //check if current user has authority in the securitypass permit to manage the requests
            var v = Task.Run(async () => await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefaultAsync()).Result;
            if (v != null)
            {
                if (v.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && v.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false)
                {
                    //get his id 
                    ViewBag.RESPO_ID = v.WORKFLOW_RESPO_CODE;
                    //get workflow type of the current user 
                    ViewBag.RESPO_STATE = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID;
                    RESPO_CODE = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE;
                    WORKFLOWID = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value;
                    //get the unit-code of the current user 
                    STATION_CODE = v.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE.Value;
                    FORCE_ID = v.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.FORCE_ID.Value;
                }
                else
                {
                    ViewBag.RESPO_STATE = 0;
                }

            }
            else
            {
                ViewBag.RESPO_STATE = 0;
                FORCE_ID = 1;
            }
        }


        // this view is for show links 
        public ActionResult Index()
        {
           
            return View();
        }

        //comments view to retrive and add new comments 
        public ActionResult Comments(int? id)
        {
            ViewBag.activetab = "Comments";

            if (id == null)
            {
                return NotFound();
            }
            //check if record in the table rA42_AIR_CREW_PASS_DTL to get its commenst 
            RA42_AIR_CREW_PASS_DTL rA42_AIR_CREW_PASS_DTL = db.RA42_AIR_CREW_PASS_DTL.Find(id);
            if (rA42_AIR_CREW_PASS_DTL == null)
            {
                 return NotFound();
            }
            //check authority to view cooments 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_AIR_CREW_PASS_DTL.RESPONSIBLE !=currentUser)
                {
                    if (rA42_AIR_CREW_PASS_DTL.ISOPENED != true)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER !=true)
                        {

                               return NotFound();
                        }
                    }
                }
            }
            else
            {
                if (rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_AIR_CREW_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != rA42_AIR_CREW_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            return NotFound();
                        }
                    }
                }
            }
            //get comments of the request 
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            return View(rA42_AIR_CREW_PASS_DTL);
        }

        // POST: new commnt

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Comments(RA42_AIR_CREW_PASS_DTL rA42_AIR_CREW_PASS_DTL, string COMMENT)
        {
            ViewBag.activetab = "Comments";
            var general_data = db.RA42_AIR_CREW_PASS_DTL.Where(a => a.AIR_CREW_PASS_CODE == rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE).FirstOrDefault();
            
            //add comments
            if (COMMENT.Length > 0)
            {
                RA42_COMMENTS_MST rA42_COMMENT = new RA42_COMMENTS_MST();
                rA42_COMMENT.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_COMMENT.PASS_ROW_CODE = rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE;
                rA42_COMMENT.CRD_BY = currentUser;
                rA42_COMMENT.CRD_DT = DateTime.Now;
                rA42_COMMENT.COMMENT = COMMENT;
                db.RA42_COMMENTS_MST.Add(rA42_COMMENT);
                db.SaveChanges();
                AddToast(new Toast("",
                  GetResourcesValue("add_comment_success"),
                  "green"));

            }
            //get comments 
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            return View(rA42_AIR_CREW_PASS_DTL);


        }

        // this view to choose which creation type you want to create permit request, this view is for authorized user in the system such as permits cell and applicant 
        public ActionResult Choosecreatetype()
        {
            ViewBag.activetab = "Privatepass";
            return View();
        }

        //this is for browse all security permits in all stations
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
            var empList = db.RA42_AIR_CREW_PASS_DTL.Where(a => a.WORKFLOW_RESPO_CODE != null).Select(a => new
            {
                AIR_CREW_PASS_CODE = a.AIR_CREW_PASS_CODE,
                SERVICE_NUMBER = (a.SERVICE_NUMBER != null ? a.SERVICE_NUMBER : " "),
                CIVIL_NUMBER = (a.CIVIL_NUMBER != null ? a.CIVIL_NUMBER : " "),
                PERSONAL_IMAGE = a.PERSONAL_IMAGE,
                RANK_A = (a.RANK_A != null ? a.RANK_A : " "),
                RANK_E = (a.RANK_E != null ? a.RANK_E : " "),
                NAME_A = (a.NAME_A != null ? a.NAME_A : " "),
                NAME_E = (a.NAME_E != null ? a.NAME_E : " "),
                PHONE_NUMBER = (a.PHONE_NUMBER != null ? a.PHONE_NUMBER : " "),
                GSM = (a.GSM != null ? a.GSM : " "),
                PURPOSE_OF_PASS = (a.PURPOSE_OF_PASS != null ? a.PURPOSE_OF_PASS : " "),
                PROFESSION_A = (a.PROFESSION_A != null ? a.PROFESSION_A : " "),
                PROFESSION_E = (a.PROFESSION_E != null ? a.PROFESSION_E : " "),
                STATION_CODE = a.STATION_CODE,
                STATION_A = a.RA42_STATIONS_MST.STATION_NAME_A,
                STATION_E = a.RA42_STATIONS_MST.STATION_NAME_E,
                BOOLD_TYPE = a.RA42_BLOOD_TYPE_MST.BLOOD_TYPE,
                RESPONSEPLE_NAME = a.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                RESPONSEPLE_NAME_E = a.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E,
                STEP_NAME = a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME,
                STEP_NAME_E = a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E,
                STATUS = a.STATUS,
                FORCE_ID = a.RA42_STATIONS_MST.FORCE_ID.Value,
                DLT_STS = a.DLT_STS,
                REJECTED = a.REJECTED,
                ISPRINTED = a.ISPRINTED,
                DATE_FROM = a.DATE_FROM,
                DATE_TO = a.DATE_TO,
                COMMENTS = a.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == a.AIR_CREW_PASS_CODE).Count()


            }
             ).ToList();
            if (ViewBag.ADMIN == true)
            {
                //get force id
                int force = Convert.ToInt32(ViewBag.FORCE_TYPE_CODE);
                empList = empList.Where(a => a.FORCE_ID == force).ToList();
            }
            int totalrows = empList.Count;
            if (!string.IsNullOrEmpty(searchValue))//filter
            {
               
                if (searchValue.Contains("منتهي"))
                {
                    empList = empList.Where(z => z.DATE_TO < DateTime.Today).ToList();
                }
                else
                {
                     empList = empList.
                Where(x => x.SERVICE_NUMBER.Contains(searchValue) || x.CIVIL_NUMBER.Contains(searchValue) || 
                x.STEP_NAME.Contains(searchValue) || x.NAME_A.Contains(searchValue) || x.NAME_E.Contains(searchValue) 
                || x.RANK_A.Contains(searchValue) || x.RANK_E.Contains(searchValue) || x.PROFESSION_A.Contains(searchValue) 
                || x.PROFESSION_E.Contains(searchValue) || x.PURPOSE_OF_PASS.Contains(searchValue) 
                || x.PHONE_NUMBER.Contains(searchValue) || x.GSM.Contains(searchValue) || x.STATION_A == searchValue).ToList();
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
        //this view is for permits cell and administrator and developer to show all printed permits for specicfic station

       

        [HttpPost]
        public JsonResult GetListPrinted()
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
            var empList = db.RA42_AIR_CREW_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID 
            && a.DLT_STS != true 
            && a.ISPRINTED == true && a.WORKFLOW_RESPO_CODE !=null).Select(a => new
            {
                AIR_CREW_PASS_CODE = a.AIR_CREW_PASS_CODE,
                SERVICE_NUMBER = (a.SERVICE_NUMBER != null ? a.SERVICE_NUMBER : " "),
                CIVIL_NUMBER = (a.CIVIL_NUMBER != null ? a.CIVIL_NUMBER : " "),
                PERSONAL_IMAGE = a.PERSONAL_IMAGE,
                RANK_A = (a.RANK_A != null ? a.RANK_A : " "),
                RANK_E = (a.RANK_E != null ? a.RANK_E : " "),
                NAME_A = (a.NAME_A != null ? a.NAME_A : " "),
                NAME_E = (a.NAME_E != null ? a.NAME_E : " "),
                PHONE_NUMBER = (a.PHONE_NUMBER != null ? a.PHONE_NUMBER : " "),
                GSM = (a.GSM != null ? a.GSM : " "),
                PURPOSE_OF_PASS = (a.PURPOSE_OF_PASS != null ? a.PURPOSE_OF_PASS : " "),
                PROFESSION_A = (a.PROFESSION_A != null ? a.PROFESSION_A : " "),
                PROFESSION_E = (a.PROFESSION_E != null ? a.PROFESSION_E : " "),
                STATION_CODE = a.STATION_CODE,
                STATION_A = a.RA42_STATIONS_MST.STATION_NAME_A,
                STATION_E = a.RA42_STATIONS_MST.STATION_NAME_E,
                BOOLD_TYPE = a.RA42_BLOOD_TYPE_MST.BLOOD_TYPE,
                RESPONSEPLE_NAME = a.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                RESPONSEPLE_NAME_E = a.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E,
                STEP_NAME = a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME,
                STEP_NAME_E = a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E,
                STATUS = a.STATUS,
                DLT_STS = a.DLT_STS,
                REJECTED = a.REJECTED,
                ISPRINTED = a.ISPRINTED,
                DATE_FROM = a.DATE_FROM,
                DATE_TO = a.DATE_TO,
                COMMENTS = a.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == a.AIR_CREW_PASS_CODE).Count()


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
                     empList = empList.
                Where(x => x.SERVICE_NUMBER.Contains(searchValue) || x.CIVIL_NUMBER.Contains(searchValue) 
                || x.NAME_A.Contains(searchValue)  || x.RANK_A.Contains(searchValue) 
                || x.PROFESSION_A.Contains(searchValue) 
                || x.PHONE_NUMBER.Contains(searchValue) || x.GSM.Contains(searchValue)
                || x.STATION_A == searchValue).ToList();
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
        //this view is for administrator and developer to show all permits for all stations
        public ActionResult Allpasses()
        {

            ViewBag.activetab = "Allpasses";
            return View();
        }
        //this view is for autho person, (المنسق الأمني)
        public ActionResult Authopasses()
        {
            ViewBag.activetab = "Authopasses";
            var rA42_AIR_CREW_PASS_DTL = db.RA42_AIR_CREW_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser 
            && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true 
            && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false).OrderByDescending(a => a.AIR_CREW_PASS_CODE);
            return View(rA42_AIR_CREW_PASS_DTL.ToList());
        }
        //this view is for permits cell and security officer 
        public async Task<ActionResult> Newpasses()
        {
            ViewBag.activetab = "Newpasses";
            var rA42_AIR_CREW_PASS_DTL = await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID 
             && a.DLT_STS != true  && a.STATUS !=true).OrderByDescending(a => a.AIR_CREW_PASS_CODE).ToListAsync();
            return View(rA42_AIR_CREW_PASS_DTL);
        }
        public async Task<ActionResult> ToPrint()
        {
            ViewBag.activetab = "ToPrint";
            var rA42_AIR_CREW_PASS_DTL = await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID
             && a.DLT_STS != true && a.ISPRINTED != true && a.STATUS == true).OrderByDescending(a => a.AIR_CREW_PASS_CODE).ToListAsync();
            return View(rA42_AIR_CREW_PASS_DTL);
        }
        //this view is for permits cell to view printed pirmits only
        public ActionResult Printed()
        {
            ViewBag.activetab = "Printed";
            return View();
        }
      
        // details of the permit view 
        public ActionResult Details(int? id)
        {
            ViewBag.activetab = "details";

            if (id == null)
            {
               return NotFound();
            }
            //check if the permit record in the table 
            RA42_AIR_CREW_PASS_DTL rA42_AIR_CREW_PASS_DTL = db.RA42_AIR_CREW_PASS_DTL.Find(id);
            if (rA42_AIR_CREW_PASS_DTL == null)
            {
                  return NotFound();
            }

            //check authority to open view details 
            if (ViewBag.RESPO_STATE == 0)
            {
                if (rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_AIR_CREW_PASS_DTL.RESPONSIBLE != currentUser)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }
          
            //get documents of the permit
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
           
            return View(rA42_AIR_CREW_PASS_DTL);
        }
     
        //this view to print card, temprory form and save card also
        [HttpGet]
        public ActionResult Card(int? id)
        {
            ViewBag.activetab = "card";

            if (id == null)
            {
                return NotFound();
            }
            //check if the record in the table 
            RA42_AIR_CREW_PASS_DTL rA42_AIR_CREW_PASS_DTL = db.RA42_AIR_CREW_PASS_DTL.Find(id);
            if (rA42_AIR_CREW_PASS_DTL == null)
            {
                 return NotFound();
            }

                    //check if the current user has authority to open the permit 
                    if (ViewBag.RESPO_STATE != rA42_AIR_CREW_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            return NotFound();
                        }
                    }
                
            

          

            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_AIR_CREW_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToList();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_AIR_CREW_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToList();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }
            return View(rA42_AIR_CREW_PASS_DTL);
        }


        //this view to print card, temprory form and save card also
        [HttpPost]
        public ActionResult Card(string CheckPrinted, int TRANSACTION_TYPE_CODE, string TRANSACTION_REMARKS, 
            HttpPostedFileBase RECEIPT, RA42_AIR_CREW_PASS_DTL rA42_AIR_CREW_PASS_DTL)
        {
            ViewBag.activetab = "card";


            //check if the record in the table 
            RA42_AIR_CREW_PASS_DTL _A42_AIR_CREW_PASS_DTL = db.RA42_AIR_CREW_PASS_DTL.Find(rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE);
            if (_A42_AIR_CREW_PASS_DTL == null)
            {
                return NotFound();
            }

            //check if the current user has authority to open the permit 
            if (ViewBag.RESPO_STATE != _A42_AIR_CREW_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
            {
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    return NotFound();
                }
            }





            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == _A42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == _A42_AIR_CREW_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToList();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == _A42_AIR_CREW_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToList();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }

            RA42_TRANSACTION_DTL rA42_TRANSACTION_DTL = new RA42_TRANSACTION_DTL();
            rA42_TRANSACTION_DTL.ACCESS_ROW_CODE = _A42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE;
            rA42_TRANSACTION_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
            rA42_TRANSACTION_DTL.TRANSACTION_TYPE_CODE = TRANSACTION_TYPE_CODE;
            rA42_TRANSACTION_DTL.REMARKS = TRANSACTION_REMARKS;
            rA42_TRANSACTION_DTL.CRD_BY = currentUser;
            rA42_TRANSACTION_DTL.CRD_DT = DateTime.Today;
            rA42_TRANSACTION_DTL.DLT_STS = false;
            //upload receipt
            if (RECEIPT != null)
            {
                try
                {


                    // Verify that the user selected a file
                    if (RECEIPT != null && RECEIPT.ContentLength > 0)
                    {
                        // extract only the filename with extention
                        string fileName = Path.GetFileNameWithoutExtension(RECEIPT.FileName);
                        string extension = Path.GetExtension(RECEIPT.FileName);


                        //check image file extention
                        if (general.CheckFileType(RECEIPT.FileName))
                        {

                            fileName = "Receipt_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                            // store the file inside ~/Files/Profiles/ folder
                            string path = Path.Combine(Server.MapPath("~/Files/Receipt/"), fileName);
                            RECEIPT.SaveAs(path);
                            rA42_TRANSACTION_DTL.RECEIPT = fileName;


                        }
                        else
                        {
                            //throw error if image file extention not supported 
                         
                            TempData["Erorr"] = "صيغة الملف غير مدعومة - File formate not supported";
                            return View(_A42_AIR_CREW_PASS_DTL);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.GetBaseException();
                }
            }

            db.RA42_TRANSACTION_DTL.Add(rA42_TRANSACTION_DTL);
            db.SaveChanges();
            rA42_AIR_CREW_PASS_DTL.UPD_BY = currentUser;
            rA42_AIR_CREW_PASS_DTL.UPD_DT = DateTime.Now;
            rA42_AIR_CREW_PASS_DTL.ISDELIVERED = false;
            db.SaveChanges();
            TempData["Success"] = "تم تحديث المعاملة بنجاح";
            if (CheckPrinted.Equals("Printed"))
            {
                var deletePrinted = db.RA42_PRINT_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.PASS_ROW_CODE ==
                _A42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE).ToList();
                if (deletePrinted.Count > 0)
                {
                    foreach (var item in deletePrinted)
                    {
                        item.DLT_STS = true;
                        db.SaveChanges();
                    }
                }
            }
            return View(_A42_AIR_CREW_PASS_DTL);
        }
        //get subzones of the main zones as json result 
     
        // this view is for searching about employee in the MIMS api 
        public ActionResult Search()
        {
          
            ViewBag.activetab = "Search";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            //int unit = 0;
            //check if the session not null 
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //this is to detrmine is this what to show (الركن المختص - قائد الجناح أو السرب)
                ViewBag.Get_Station_Code = id.ToString();
                var station = int.Parse(id.ToString());
                STATION_CODE = station;
                //get station name 
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


                }
                     }
            else
            {
               //if session is null, get current user unit-code from the system. 
                    var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();

                    if (Language.GetCurrentLang() == "en")
                    {
                        ViewBag.STATION_NAME = "in " + check_unit.STATION_NAME_E;
                    }
                    else
                    {
                        ViewBag.STATION_NAME = "في " + check_unit.STATION_NAME_A;

                    }


                //this is to detrmine is this what to show (الركن المختص - قائد الجناح أو السرب)
                ViewBag.Get_Station_Code = STATION_CODE.ToString();
               

            }
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();

            if (Language.GetCurrentLang() == "en")
            {
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //get permits types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                //get autho person (المنسق الأمني) who is responsible about this kind of permit 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                  //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //if the current user workflow id type is applicant or normal user show error 
                    if (ViewBag.RESPO_STATE <= 1)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                    }
                }
            }
            else
            {
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get idnetities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                //get autho person who set as responsible to manage these type of permit (المنسق الأمني) in arabic 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                 //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //if the current user workflow id type is applicant or normal user show error 
                    if (ViewBag.RESPO_STATE <= 1)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        
                    }
                }
            }

            //get bloods
            ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE");

            return View();
        }

        //post new data from the search view 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Search(RA42_AIR_CREW_PASS_DTL rA42_AIR_CREW_PASS_DTL,
            int[] FILE_TYPES,string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "Search";
            ViewBag.Service_No = currentUser;

            var url = Url.RequestContext.RouteData.Values["id"];
            //int unit = 0;

            //check if the session not null 
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //this is to detrmine is this what to show (الركن المختص - قائد الجناح أو السرب)
                ViewBag.Get_Station_Code = id.ToString();
                var station = int.Parse(id.ToString());
                STATION_CODE = station;
                //get station name 
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


                }
                  }
            else
            {
                //if session is null, get current user unit-code from the system. 
                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();

                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.STATION_NAME = "in " + check_unit.STATION_NAME_E;
                }
                else
                {
                    ViewBag.STATION_NAME = "في " + check_unit.STATION_NAME_A;

                }


                //this is to detrmine is this what to show (الركن المختص - قائد الجناح أو السرب)
                ViewBag.Get_Station_Code = STATION_CODE.ToString();
            
            }
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();
            //get bloods
            ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE",rA42_AIR_CREW_PASS_DTL.BLOOD_CODE);

            if (Language.GetCurrentLang() == "en")
            {
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_AIR_CREW_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE);
                //get permits types in englsih (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE);
                //get autho person who set as responsible to manage these type of permit (المنسق الأمني) in english  
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE);
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if the current user workflow id is 1 or less than 1 
                    if (ViewBag.RESPO_STATE <= 1)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AIR_CREW_PASS_DTL);
                    }
                }
            }
            else
            {
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_AIR_CREW_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic (ثابت - مؤقت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE);
                //get autho person who set as responsible to manage these type of permit (المنسق الأمني) in arabic 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE);
                 //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if the current user workflow id is 1 or less than 1 
                    if (ViewBag.RESPO_STATE <= 1)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AIR_CREW_PASS_DTL);
                    }
                }
            }

            if (ModelState.IsValid)
            {
                //check if user upload new image 
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


                            //check image extention 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {

                                fileName = "Profile_" + ACCESS_TYPE_CODE + DateTime.Now.ToString("yymmssfff") + extension;
                              
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);
                                if (check != true)
                                {
                                    AddToast(new Toast("",
                                   GetResourcesValue("error_update_message"),
                                   "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_AIR_CREW_PASS_DTL);
                                }
                                rA42_AIR_CREW_PASS_DTL.PERSONAL_IMAGE = fileName;



                            }
                            else
                            {
                                //show error message if the file format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_AIR_CREW_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }

                //get serached user military information from the api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER.ToUpper())
                    );
                callTask.Wait();
                user = callTask.Result;
                rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER = rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER.ToUpper();
                rA42_AIR_CREW_PASS_DTL.RESPONSIBLE = currentUser;
                rA42_AIR_CREW_PASS_DTL.RANK_A = user.NAME_RANK_A;
                rA42_AIR_CREW_PASS_DTL.RANK_E = user.NAME_RANK_E;
                rA42_AIR_CREW_PASS_DTL.NAME_A = user.NAME_EMP_A;
                rA42_AIR_CREW_PASS_DTL.NAME_E = user.NAME_EMP_E;
                rA42_AIR_CREW_PASS_DTL.UNIT_A = user.NAME_UNIT_A;
                rA42_AIR_CREW_PASS_DTL.UNIT_E = user.NAME_UNIT_E;
                rA42_AIR_CREW_PASS_DTL.PROFESSION_A = user.NAME_TRADE_A;
                rA42_AIR_CREW_PASS_DTL.PROFESSION_E = user.NAME_TRADE_E;
                //get the military info of the current user from the api 
                User permit = null;
                Task<User> callTask2 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask2.Wait();
                permit = callTask2.Result;
                //this section is for applicant 
                if (WORKFLOWID <= 1 || ViewBag.NOT_RELATED_STATION == true)
                {
                    //the request will redirected to the autho person as normal request 
                    rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_AIR_CREW_PASS_DTL.REJECTED = false;
                    rA42_AIR_CREW_PASS_DTL.STATUS = false;
                    rA42_AIR_CREW_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person (المنسق الأمني) 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //the autho person should redirect the request to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 10 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AIR_CREW_PASS_DTL);

                    }
                    else
                    {
                        rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_AIR_CREW_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_AIR_CREW_PASS_DTL.APPROVAL_RANK = permit.NAME_RANK_A;
                    rA42_AIR_CREW_PASS_DTL.APPROVAL_NAME = permit.NAME_EMP_A;
                    rA42_AIR_CREW_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_AIR_CREW_PASS_DTL.REJECTED = false;
                    rA42_AIR_CREW_PASS_DTL.STATUS = false;
                    rA42_AIR_CREW_PASS_DTL.ISOPENED = true;
                }
               

                //this is ops offecier & permit cell
                if ((WORKFLOWID == 10 || WORKFLOWID == 3) && ViewBag.NOT_RELATED_STATION != true)
                {
                    //after the security oofcier create this permit and completet every thing, the permit should be redirected to the permit cell to print it 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AIR_CREW_PASS_DTL);

                    }
                    else
                    {
                        rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_AIR_CREW_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_AIR_CREW_PASS_DTL.AUTHO_RANK = permit.NAME_RANK_A;
                    rA42_AIR_CREW_PASS_DTL.AUTHO_NAME = permit.NAME_EMP_A;
                    rA42_AIR_CREW_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_AIR_CREW_PASS_DTL.REJECTED = false;
                    rA42_AIR_CREW_PASS_DTL.STATUS = true;
                    rA42_AIR_CREW_PASS_DTL.ISOPENED = true;
                }
                rA42_AIR_CREW_PASS_DTL.CARD_FOR_CODE = 1;
                rA42_AIR_CREW_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_AIR_CREW_PASS_DTL.CRD_BY = currentUser;
                rA42_AIR_CREW_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_AIR_CREW_PASS_DTL.UPD_BY = currentUser;
                rA42_AIR_CREW_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_AIR_CREW_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_AIR_CREW_PASS_DTL.BARCODE = rA42_AIR_CREW_PASS_DTL.BARCODE;
                db.RA42_AIR_CREW_PASS_DTL.Add(rA42_AIR_CREW_PASS_DTL);
                db.SaveChanges();

               
                //upload documents 
                if (files != null)
                {
                    
                    try
                    {
                        //set files in foreach loop to the deal with multiple documents
                        int c = 0;
                        foreach (HttpPostedFileBase file in files)
                        {
                            // Verify that the user selected a file
                            if (file != null && file.ContentLength > 0)
                            {
                                // extract only the filename with extention
                                string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                                string extension = Path.GetExtension(file.FileName);


                                //check document extention
                                if (general.CheckFileType(file.FileName))
                                {

                                    fileName = "FileNO" + c + "_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);
                                    //add new document
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE,
                                        FILE_TYPE = FILE_TYPES[c],
                                        FILE_TYPE_TEXT = FILE_TYPES_TEXT[c],
                                        FILE_NAME = fileName,
                                        CRD_BY = currentUser,
                                        CRD_DT = DateTime.Now


                                    };
                                    db.RA42_FILES_MST.Add(fILES_MST);
                                    db.SaveChanges();
                                    c++;
                                }
                                else
                                {
                                    //delete whole document if there is any error in on document, this is security proceduers
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_AIR_CREW_PASS_DTL);
                                }
                            }

                            else
                            {
                                

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
               
                AddToast(new Toast("",
                   GetResourcesValue("success_create_message"),
                   "green"));
                if (ViewBag.RESPO_STATE <=1)
                {
                    return RedirectToAction("Index", "MyPasses");

                }
                else
                {
                    return RedirectToAction("Index");
                }
            }


            AddToast(new Toast("",
                GetResourcesValue("error_create_message"),
                "red"));
            return View(rA42_AIR_CREW_PASS_DTL);
        }

        // this view to create permit in normal way for normal users 
        public ActionResult Create()
        {
            ViewBag.activetab = "Create";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            
            //check if the session not null 
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //this is to detrmine is this what to show (الركن المختص - قائد الجناح أو السرب)
                ViewBag.Get_Station_Code = id.ToString();
                var station = int.Parse(id.ToString());
                STATION_CODE = station;
                //get station name 
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


                }
                 }
            else
            {
                //if session is null, get current user unit-code from the system. 
                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();

                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.STATION_NAME = "in " + check_unit.STATION_NAME_E;
                }
                else
                {
                    ViewBag.STATION_NAME = "في " + check_unit.STATION_NAME_A;

                }


                //this is to detrmine is this what to show (الركن المختص - قائد الجناح أو السرب)
                ViewBag.Get_Station_Code = STATION_CODE.ToString();
             
            }
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();

            if (Language.GetCurrentLang() == "en")
            {
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //get permits types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                //get autho person (المنسق الأمني) who is responsible about this kind of permit 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE ).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                    
                }
            }
            else
            {
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get idnetities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                //get autho person who set as responsible to manage these type of permit (المنسق الأمني) in arabic 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                 //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        
                    
                }
            }
            //get bloods
            ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE");

            return View();
        }

        // POST: Aircrew/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(RA42_AIR_CREW_PASS_DTL rA42_AIR_CREW_PASS_DTL,
             int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {

            ViewBag.activetab = "Create";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            //check if the session not null 
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //this is to detrmine is this what to show (الركن المختص - قائد الجناح أو السرب)
                ViewBag.Get_Station_Code = id.ToString();
                var station = int.Parse(id.ToString());
                STATION_CODE = station;
                //get station name 
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


                }
                  }
            else
            {
                //if session is null, get current user unit-code from the system. 
                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();

                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.STATION_NAME = "in " + check_unit.STATION_NAME_E;
                }
                else
                {
                    ViewBag.STATION_NAME = "في " + check_unit.STATION_NAME_A;

                }


                //this is to detrmine is this what to show (الركن المختص - قائد الجناح أو السرب)
                ViewBag.Get_Station_Code = STATION_CODE.ToString();
           
            }
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();
            //get bloods types
            ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE", rA42_AIR_CREW_PASS_DTL.BLOOD_CODE);

            if (Language.GetCurrentLang() == "en")
            {
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_AIR_CREW_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE);
                //get permits types in englsih (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE);
                //get autho person who set as responsible to manage these type of permit (المنسق الأمني) in english  
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE);
                 //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");


                if (WORKFLOW_RESPO.Count == 0)
                {
                   
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AIR_CREW_PASS_DTL);
                    
                }
            }
            else
            {
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_AIR_CREW_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic (ثابت - مؤقت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE);
                //get autho person who set as responsible to manage these type of permit (المنسق الأمني) in arabic 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE);
                  //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

                if (WORKFLOW_RESPO.Count == 0)
                {
                   
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AIR_CREW_PASS_DTL);
                    
                }
            }


            if (rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE == null)
            {
                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                return View(rA42_AIR_CREW_PASS_DTL);

            }
            if (ModelState.IsValid)
            {
                //check if user upload image 
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


                            //check extention of image 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {

                                fileName = "Profile_" + ACCESS_TYPE_CODE + DateTime.Now.ToString("yymmssfff") + extension;

                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);
                                if (check != true)
                                {
                                    AddToast(new Toast("",
                                   GetResourcesValue("error_update_message"),
                                   "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_AIR_CREW_PASS_DTL);
                                }
                                rA42_AIR_CREW_PASS_DTL.PERSONAL_IMAGE = fileName;

                            }
                            else
                            {
                                //if image format not supported show error message 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_AIR_CREW_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                //get current user military info from api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;
                rA42_AIR_CREW_PASS_DTL.CARD_FOR_CODE = 1;
                rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER = rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER;
                rA42_AIR_CREW_PASS_DTL.RANK_A = user.NAME_RANK_A;
                rA42_AIR_CREW_PASS_DTL.RANK_E = user.NAME_RANK_E;
                rA42_AIR_CREW_PASS_DTL.NAME_A = user.NAME_EMP_A;
                rA42_AIR_CREW_PASS_DTL.NAME_E = user.NAME_EMP_E;
                rA42_AIR_CREW_PASS_DTL.UNIT_A = user.NAME_UNIT_A;
                rA42_AIR_CREW_PASS_DTL.UNIT_E = user.NAME_UNIT_E;
                rA42_AIR_CREW_PASS_DTL.PROFESSION_A = user.NAME_TRADE_A;
                rA42_AIR_CREW_PASS_DTL.PROFESSION_E = user.NAME_TRADE_E;
                rA42_AIR_CREW_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_AIR_CREW_PASS_DTL.CRD_BY = currentUser;
                rA42_AIR_CREW_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_AIR_CREW_PASS_DTL.UPD_BY = currentUser;
                rA42_AIR_CREW_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_AIR_CREW_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                //get the military info of the current user from the api 
                User permit = null;
                Task<User> callTask2 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask2.Wait();
                permit = callTask2.Result;
                //this section is for applicant 
                if (WORKFLOWID <= 1 || ViewBag.NOT_RELATED_STATION == true)
                {
                    //the request will redirected to the autho person as normal request 
                    rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_AIR_CREW_PASS_DTL.REJECTED = false;
                    rA42_AIR_CREW_PASS_DTL.STATUS = false;
                    rA42_AIR_CREW_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person (المنسق الأمني) 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //the autho person should redirect the request to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 10 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AIR_CREW_PASS_DTL);

                    }
                    else
                    {
                        rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_AIR_CREW_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_AIR_CREW_PASS_DTL.APPROVAL_RANK = permit.NAME_RANK_A;
                    rA42_AIR_CREW_PASS_DTL.APPROVAL_NAME = permit.NAME_EMP_A;
                    rA42_AIR_CREW_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_AIR_CREW_PASS_DTL.REJECTED = false;
                    rA42_AIR_CREW_PASS_DTL.STATUS = false;
                    rA42_AIR_CREW_PASS_DTL.ISOPENED = true;
                }
               

                //this is security offecier
                if ((WORKFLOWID == 10 || WORKFLOWID == 3) && ViewBag.NOT_RELATED_STATION != true)
                {
                    //after the security oofcier create this permit and completet every thing, the permit should be redirected to the permit cell to print it 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AIR_CREW_PASS_DTL);

                    }
                    else
                    {
                        rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_AIR_CREW_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_AIR_CREW_PASS_DTL.AUTHO_RANK = permit.NAME_RANK_A;
                    rA42_AIR_CREW_PASS_DTL.AUTHO_NAME = permit.NAME_EMP_A;
                    rA42_AIR_CREW_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_AIR_CREW_PASS_DTL.REJECTED = false;
                    rA42_AIR_CREW_PASS_DTL.STATUS = true;
                    rA42_AIR_CREW_PASS_DTL.ISOPENED = true;
                }
                rA42_AIR_CREW_PASS_DTL.BARCODE = rA42_AIR_CREW_PASS_DTL.BARCODE;

                db.RA42_AIR_CREW_PASS_DTL.Add(rA42_AIR_CREW_PASS_DTL);
                db.SaveChanges();

                
                //add documents 
                if (files != null)
                {
                   
                    try
                    {
                        //create foreach loop to deal with multiple files 
                        int c = 0;
                        foreach (HttpPostedFileBase file in files)
                        {
                            // Verify that the user selected a file
                            if (file != null && file.ContentLength > 0)
                            {
                                // extract only the filename with extention
                                string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                                string extension = Path.GetExtension(file.FileName);


                                //check file extention 
                                if (general.CheckFileType(file.FileName))
                                {

                                    fileName = "FileNO" + c + "_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);
                                    //add file to db 
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE,
                                        FILE_TYPE = FILE_TYPES[c],
                                        FILE_TYPE_TEXT = FILE_TYPES_TEXT[c],
                                        FILE_NAME = fileName,
                                        CRD_BY = currentUser,
                                        CRD_DT = DateTime.Now


                                    };
                                    db.RA42_FILES_MST.Add(fILES_MST);
                                    db.SaveChanges();
                                    c++;
                                }
                                else
                                {
                                    //delete whole uploaded files if any file is currupte, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_AIR_CREW_PASS_DTL);
                                }
                            }

                            else
                            {
                               

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
             
                AddToast(new Toast("",
                   GetResourcesValue("success_create_message"),
                   "green"));
                return RedirectToAction("Index", "Mypasses");
            }

            AddToast(new Toast("",
                GetResourcesValue("error_create_message"),
                "red"));
            return View(rA42_AIR_CREW_PASS_DTL);
        }

       

        public ActionResult Otherpermit()
        {

            ViewBag.activetab = "Otherpermit";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            //int unit = 0;
            //check if the session not null 
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //this is to detrmine is this what to show (الركن المختص - قائد الجناح أو السرب)
                ViewBag.Get_Station_Code = id.ToString();
                var station = int.Parse(id.ToString());
                STATION_CODE = station;
                //get station name 
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


                }
                    }
            else
            {
                //if session is null, get current user unit-code from the system. 
                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();

                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.STATION_NAME = "in " + check_unit.STATION_NAME_E;
                }
                else
                {
                    ViewBag.STATION_NAME = "في " + check_unit.STATION_NAME_A;

                }


                //this is to detrmine is this what to show (الركن المختص - قائد الجناح أو السرب)
                ViewBag.Get_Station_Code = STATION_CODE.ToString();
         
            }
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();



            if (Language.GetCurrentLang() == "en")
            {
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //get permits types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                //get autho person (المنسق الأمني) who is responsible about this kind of permit 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //if the current user workflow id type is applicant or normal user show error 
                    if (ViewBag.RESPO_STATE <= 1)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                    }
                }
            }
            else
            {
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get idnetities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                //get autho person who set as responsible to manage these type of permit (المنسق الأمني) in arabic 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                 //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //if the current user workflow id type is applicant or normal user show error 
                    if (ViewBag.RESPO_STATE <= 1)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                    }
                }
            }
            //get bloods
            ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE");

            return View();
        }

        // POST data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Otherpermit(RA42_AIR_CREW_PASS_DTL rA42_AIR_CREW_PASS_DTL,
             int[] FILE_TYPES,string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "Otherpermit";
            var url = Url.RequestContext.RouteData.Values["id"];
            //int unit = 0;
            //check if the session not null 
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //this is to detrmine is this what to show (الركن المختص - قائد الجناح أو السرب)
                ViewBag.Get_Station_Code = id.ToString();
                var station = int.Parse(id.ToString());
                STATION_CODE = station;
                //get station name 
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


                }
                 }
            else
            {
                //if session is null, get current user unit-code from the system. 
                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();

                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.STATION_NAME = "in " + check_unit.STATION_NAME_E;
                }
                else
                {
                    ViewBag.STATION_NAME = "في " + check_unit.STATION_NAME_A;

                }


                //this is to detrmine is this what to show (الركن المختص - قائد الجناح أو السرب)
                ViewBag.Get_Station_Code = STATION_CODE.ToString();
            
            }
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();
            //get bloods
            ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE",rA42_AIR_CREW_PASS_DTL.BLOOD_CODE);

            if (Language.GetCurrentLang() == "en")
            {
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_AIR_CREW_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE);
                //get permits types in englsih (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE);
                //get autho person who set as responsible to manage these type of permit (المنسق الأمني) in english  
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE);
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");


                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if the current user workflow id is 1 or less than 1 
                    if (ViewBag.RESPO_STATE <= 1)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AIR_CREW_PASS_DTL);
                    }
                }
            }
            else
            {
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_AIR_CREW_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic (ثابت - مؤقت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE);
                //get autho person who set as responsible to manage these type of permit (المنسق الأمني) in arabic 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE);
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if the current user workflow id is 1 or less than 1 
                    if (ViewBag.RESPO_STATE <= 1)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AIR_CREW_PASS_DTL);
                    }
                }
            }

            if (ModelState.IsValid)
            {


                //check if user upload image file 
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


                            //check file extention 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {

                                fileName = "Profile_" + ACCESS_TYPE_CODE + DateTime.Now.ToString("yymmssfff") + extension;

                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);
                                if (check != true)
                                {
                                    AddToast(new Toast("",
                                   GetResourcesValue("error_update_message"),
                                   "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_AIR_CREW_PASS_DTL);
                                }
                                rA42_AIR_CREW_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //if image format not supported show error message 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_AIR_CREW_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER = rA42_AIR_CREW_PASS_DTL.RESPONSIBLE;
                rA42_AIR_CREW_PASS_DTL.RESPONSIBLE = currentUser;
                rA42_AIR_CREW_PASS_DTL.CARD_FOR_CODE = 1;
                rA42_AIR_CREW_PASS_DTL.RANK_A = rA42_AIR_CREW_PASS_DTL.RANK_A;
                rA42_AIR_CREW_PASS_DTL.RANK_E = rA42_AIR_CREW_PASS_DTL.RANK_E;
                rA42_AIR_CREW_PASS_DTL.NAME_A = rA42_AIR_CREW_PASS_DTL.NAME_A;
                rA42_AIR_CREW_PASS_DTL.NAME_E = rA42_AIR_CREW_PASS_DTL.NAME_E;
                rA42_AIR_CREW_PASS_DTL.UNIT_A = rA42_AIR_CREW_PASS_DTL.UNIT_A;
                rA42_AIR_CREW_PASS_DTL.UNIT_E = rA42_AIR_CREW_PASS_DTL.UNIT_E;
                rA42_AIR_CREW_PASS_DTL.PROFESSION_A = rA42_AIR_CREW_PASS_DTL.PROFESSION_A;
                rA42_AIR_CREW_PASS_DTL.PROFESSION_E = rA42_AIR_CREW_PASS_DTL.PROFESSION_E;
                //get current user info from api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;
                //this section is for applicant 
                if (WORKFLOWID <= 1 || ViewBag.NOT_RELATED_STATION == true)
                {

                    rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_AIR_CREW_PASS_DTL.REJECTED = false;
                    rA42_AIR_CREW_PASS_DTL.STATUS = false;
                    rA42_AIR_CREW_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person (المنسق الأمني)
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect the request to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 10 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AIR_CREW_PASS_DTL);

                    }
                    else
                    {
                        rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_AIR_CREW_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_AIR_CREW_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                    rA42_AIR_CREW_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                    rA42_AIR_CREW_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_AIR_CREW_PASS_DTL.REJECTED = false;
                    rA42_AIR_CREW_PASS_DTL.STATUS = false;
                    rA42_AIR_CREW_PASS_DTL.ISOPENED = true;
                }
                
                //this section is for ops officer & permit cell
                if ((WORKFLOWID == 3 || WORKFLOWID == 10)&& ViewBag.NOT_RELATED_STATION != true)
                {
                    //after security ooficer complete every thing, he should redirect the pernit to the permits cell to print it 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AIR_CREW_PASS_DTL);

                    }
                    else
                    {
                        rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_AIR_CREW_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_AIR_CREW_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_AIR_CREW_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    rA42_AIR_CREW_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_AIR_CREW_PASS_DTL.REJECTED = false;
                    rA42_AIR_CREW_PASS_DTL.STATUS = true;
                    rA42_AIR_CREW_PASS_DTL.ISOPENED = true;
                }
                rA42_AIR_CREW_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_AIR_CREW_PASS_DTL.CRD_BY = currentUser;
                rA42_AIR_CREW_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_AIR_CREW_PASS_DTL.UPD_BY = currentUser;
                rA42_AIR_CREW_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_AIR_CREW_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_AIR_CREW_PASS_DTL.BARCODE = rA42_AIR_CREW_PASS_DTL.BARCODE;
                db.RA42_AIR_CREW_PASS_DTL.Add(rA42_AIR_CREW_PASS_DTL);
                db.SaveChanges();

               
                //add selected documents 
                if (files != null)
                {

                    try
                    {
                        //set foreach loop to deal with multiple files 
                        int c = 0;
                        foreach (HttpPostedFileBase file in files)
                        {
                            // Verify that the user selected a file
                            if (file != null && file.ContentLength > 0)
                            {
                                // extract only the filename with extention
                                string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                                string extension = Path.GetExtension(file.FileName);


                                //get file extention 
                                if (general.CheckFileType(file.FileName))
                                {

                                    fileName = "FileNO" + c + "_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);
                                    //add new file to db table 
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE,
                                        FILE_TYPE = FILE_TYPES[c],
                                        FILE_TYPE_TEXT = FILE_TYPES_TEXT[c],
                                        FILE_NAME = fileName,
                                        CRD_BY = currentUser,
                                        CRD_DT = DateTime.Now


                                    };
                                    db.RA42_FILES_MST.Add(fILES_MST);
                                    db.SaveChanges();
                                    c++;
                                }
                                else
                                {
                                    //if any file corrupted, delete all uploaded file to db, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_AIR_CREW_PASS_DTL);
                                }
                            }

                            else
                            {


                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }

                AddToast(new Toast("",
                   GetResourcesValue("success_create_message"),
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
                GetResourcesValue("error_create_message"),
                "red"));
            return View(rA42_AIR_CREW_PASS_DTL);
        }


       //Edit/5
        public ActionResult Edit(int? id)
        {
            ViewBag.activetab = "edit";
            
            if (id == null)
            {
                return NotFound();
            }
            //check if the permit id is in the table 
            RA42_AIR_CREW_PASS_DTL rA42_AIR_CREW_PASS_DTL = db.RA42_AIR_CREW_PASS_DTL.Find(id);
            if (rA42_AIR_CREW_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if current user is authorized 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_AIR_CREW_PASS_DTL.RESPONSIBLE != currentUser)
                {
                    if (rA42_AIR_CREW_PASS_DTL.ISOPENED != true)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER !=true)
                        {

                            return NotFound();

                        }
                    }
                }

                if (rA42_AIR_CREW_PASS_DTL.ISOPENED == true)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        if (rA42_AIR_CREW_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && rA42_AIR_CREW_PASS_DTL.REJECTED == true)
                        {

                        }
                        else {
                            if (rA42_AIR_CREW_PASS_DTL.ISOPENED == true)
                            {
                                return NotFound();
                            }
                        }

                    }
                }
            }
            else
            {
                if (rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_AIR_CREW_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != rA42_AIR_CREW_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
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
                 //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_AIR_CREW_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE);
                //get permit types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE);
                //get documents types in english for this kind of permit 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
               //get autho person for this kind of permit in english 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if the current user is equal 1 or less than 1 
                    if (ViewBag.RESPO_STATE <= 1)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    }
                    

                }

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
            else
            {
               //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_AIR_CREW_PASS_DTL.GENDER_ID);
                //get idnetities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE);
                //get permit types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE);
                //get autho person in arabic for this kind of permit 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //if current user is 1 or ess than one show error message 
                    if (ViewBag.RESPO_STATE <= 1)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                       
                    }

                }

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                 }

            //get bloods
            ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE",rA42_AIR_CREW_PASS_DTL.BLOOD_CODE);
             //get selected files of the perimt 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get documents of this kind of permit to compare between uploaded documenst and missing documenst 
            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_AIR_CREW_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                                  select new
                                  {
                                      FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                      FILE_TYPE = c.FILE_TYPE,
                                      FILE_TYPE_E = c.FILE_TYPE_E,

                                  }).Count();

            ViewBag.PASS_FILES_2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                    join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                    join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_AIR_CREW_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            //get commenst of this permit 
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            //get uploaded image 
            ViewBag.PERSONAL_IMAGE = rA42_AIR_CREW_PASS_DTL.PERSONAL_IMAGE;
            //get status of this permit 
            ViewBag.STATUS = rA42_AIR_CREW_PASS_DTL.STATUS;
            return View(rA42_AIR_CREW_PASS_DTL);
        }

        // POST: Securitypass/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(RA42_AIR_CREW_PASS_DTL rA42_AIR_CREW_PASS_DTL, string COMMENT,
            FormCollection form,
             int[] FILE_TYPES,string[] FILE_TYPES_TEXT, HttpPostedFileBase[]
            files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "edit";
            var general_data = db.RA42_AIR_CREW_PASS_DTL.Where(a => a.AIR_CREW_PASS_CODE == rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE).FirstOrDefault();
           //get selected files of the perimt 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get commenst 
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            //get personal image 
            ViewBag.PERSONAL_IMAGE = general_data.PERSONAL_IMAGE;
            //get status of permit 
            ViewBag.STATUS = general_data.STATUS;
            //get documents of this kind of permit to compare between uploaded documenst and missing documenst 
            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == general_data.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                                  select new
                                  {
                                      FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                      FILE_TYPE = c.FILE_TYPE,
                                      FILE_TYPE_E = c.FILE_TYPE_E,

                                  }).Count();

            ViewBag.PASS_FILES_2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                    join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                    join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == general_data.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            //get bloods
            ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE", rA42_AIR_CREW_PASS_DTL.BLOOD_CODE);
            if (Language.GetCurrentLang() == "en")
            {
                //companies
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_AIR_CREW_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE);
                //get permits types in englsih 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE);
               //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                  //get autho person for this kind of permit in english 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if current user is less than or equal 1
                    if (ViewBag.RESPO_STATE <= 1)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AIR_CREW_PASS_DTL);
                    }

                }

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
            else
            {
                 //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_AIR_CREW_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE);
                 //get documents types in arabic for this kind of permit 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error if the current user is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_AIR_CREW_PASS_DTL);
                    }

                }

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
          

            //editing start from here
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


                            //check file extention
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {

                                fileName = "Profile_" + ACCESS_TYPE_CODE + DateTime.Now.ToString("yymmssfff") + extension;

                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);
                                if (check != true)
                                {
                                    AddToast(new Toast("",
                                   GetResourcesValue("error_update_message"),
                                   "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_AIR_CREW_PASS_DTL);
                                }
                                rA42_AIR_CREW_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error message if image format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_AIR_CREW_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
               
                //add documents 
                if (files != null)
                {
                   
                    try
                    {
                        //create foreach loop to deal for multiple files 
                        int c = 0;
                        foreach (HttpPostedFileBase file in files)
                        {
                            // Verify that the user selected a file
                            if (file != null && file.ContentLength > 0)
                            {
                                // extract only the filename with extention
                                string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                                string extension = Path.GetExtension(file.FileName);



                                if (general.CheckFileType(file.FileName))
                                {

                                    fileName = "FileNO" + c + "_2_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);

                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE,
                                        FILE_TYPE = FILE_TYPES[c],
                                        FILE_TYPE_TEXT = FILE_TYPES_TEXT[c],
                                        FILE_NAME = fileName,
                                        CRD_BY = currentUser,
                                        CRD_DT = DateTime.Now


                                    };
                                    db.RA42_FILES_MST.Add(fILES_MST);
                                    db.SaveChanges();
                                    c++;
                                }
                                else
                                {
                                    //delete all uploaded files if one file currupted, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return RedirectToAction("Edit", new { id = rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE });
                                }
                            }

                            else
                            {
                               

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                //add comments
                if (COMMENT.Length > 0)
                {
                    RA42_COMMENTS_MST rA42_COMMENT = new RA42_COMMENTS_MST();
                    rA42_COMMENT.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                    rA42_COMMENT.PASS_ROW_CODE = rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE;
                    rA42_COMMENT.CRD_BY = currentUser;
                    rA42_COMMENT.CRD_DT = DateTime.Now;
                    rA42_COMMENT.COMMENT = COMMENT;
                    db.RA42_COMMENTS_MST.Add(rA42_COMMENT);


                }


                //get image name iand inserted to colomun in db table 
                if (rA42_AIR_CREW_PASS_DTL.PERSONAL_IMAGE != null)
                {
                    general_data.PERSONAL_IMAGE = rA42_AIR_CREW_PASS_DTL.PERSONAL_IMAGE;
                }
                else
                {
                    general_data.PERSONAL_IMAGE = general_data.PERSONAL_IMAGE;

                }
                var x = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                var currentUserInfo = (new UserInfo()).getUserInfo();

                //this section is for developer
                if (form["approvebtn"] != null && ViewBag.DEVELOPER == true)
                {
                    general_data.WORKFLOW_RESPO_CODE = rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE;
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.SERVICE_NUMBER = rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER;
                    general_data.CIVIL_NUMBER = rA42_AIR_CREW_PASS_DTL.CIVIL_NUMBER;
                    general_data.RANK_A = rA42_AIR_CREW_PASS_DTL.RANK_A;
                    general_data.RANK_E = rA42_AIR_CREW_PASS_DTL.RANK_E;
                    general_data.NAME_A = rA42_AIR_CREW_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_AIR_CREW_PASS_DTL.NAME_E;
                    general_data.PHONE_NUMBER = rA42_AIR_CREW_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_AIR_CREW_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE;
                    general_data.GENDER_ID = rA42_AIR_CREW_PASS_DTL.GENDER_ID;
                    general_data.UNIT_A = rA42_AIR_CREW_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_AIR_CREW_PASS_DTL.UNIT_E;
                    general_data.PROFESSION_A = rA42_AIR_CREW_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_AIR_CREW_PASS_DTL.PROFESSION_E;
                    general_data.PASS_TYPE_CODE = rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE;
                    general_data.BLOOD_CODE = rA42_AIR_CREW_PASS_DTL.BLOOD_CODE;
                    general_data.JOINING_DATE = rA42_AIR_CREW_PASS_DTL.JOINING_DATE;
                    general_data.DATE_FROM = rA42_AIR_CREW_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_AIR_CREW_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_AIR_CREW_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_AIR_CREW_PASS_DTL.REMARKS;
                    general_data.APPROVAL_SN = general_data.APPROVAL_SN;
                    general_data.APPROVAL_NAME = general_data.APPROVAL_NAME;
                    general_data.APPROVAL_RANK = general_data.APPROVAL_RANK;
                    general_data.APPROVAL_APPROVISION_DATE = general_data.APPROVAL_APPROVISION_DATE;
                    general_data.AUTHO_SN = general_data.AUTHO_SN;
                    general_data.AUTHO_NAME = general_data.AUTHO_NAME;
                    general_data.AUTHO_RANK = general_data.AUTHO_RANK;
                    general_data.AUTHO_APPROVISION_DATE = general_data.AUTHO_APPROVISION_DATE;
                    general_data.PERMIT_SN = general_data.PERMIT_SN;
                    general_data.PERMIT_NAME = general_data.PERMIT_NAME;
                    general_data.PERMIT_RANK = general_data.PERMIT_RANK;
                    general_data.PERMIT_APPROVISION_DATE = general_data.PERMIT_APPROVISION_DATE;
                    general_data.BARCODE = general_data.BARCODE;
                    general_data.REJECTED = false;
                    general_data.STATUS = general_data.STATUS;
                    general_data.ISOPENED = true;
                    general_data.ISPRINTED = false;
                    db.Entry(general_data).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Allpasses");
                }

                //this section is for applicant 
                if (form["approvebtn"] != null && WORKFLOWID <= 1)
                {


                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    if (rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE == null)
                    {
                        general_data.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;
                    }
                    else
                    {
                        general_data.WORKFLOW_RESPO_CODE = rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE;
                    }
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.SERVICE_NUMBER = rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER;
                    general_data.CIVIL_NUMBER = rA42_AIR_CREW_PASS_DTL.CIVIL_NUMBER;
                    general_data.RANK_A = rA42_AIR_CREW_PASS_DTL.RANK_A;
                    general_data.RANK_E = rA42_AIR_CREW_PASS_DTL.RANK_E;
                    general_data.NAME_A = rA42_AIR_CREW_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_AIR_CREW_PASS_DTL.NAME_E;
                    general_data.JOINING_DATE = rA42_AIR_CREW_PASS_DTL.JOINING_DATE;
                    general_data.PHONE_NUMBER = rA42_AIR_CREW_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_AIR_CREW_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE;
                    general_data.BLOOD_CODE = rA42_AIR_CREW_PASS_DTL.BLOOD_CODE;
                    general_data.GENDER_ID = rA42_AIR_CREW_PASS_DTL.GENDER_ID;
                    general_data.UNIT_A = rA42_AIR_CREW_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_AIR_CREW_PASS_DTL.UNIT_E;
                    general_data.PROFESSION_A = rA42_AIR_CREW_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_AIR_CREW_PASS_DTL.PROFESSION_E;
                    general_data.PASS_TYPE_CODE = rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_AIR_CREW_PASS_DTL.DATE_FROM;
                    general_data.PURPOSE_OF_PASS = rA42_AIR_CREW_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.DATE_TO = rA42_AIR_CREW_PASS_DTL.DATE_TO;
                    general_data.REMARKS = rA42_AIR_CREW_PASS_DTL.REMARKS;
                    general_data.APPROVAL_SN = general_data.APPROVAL_SN;
                    general_data.APPROVAL_RANK = general_data.APPROVAL_RANK;
                    general_data.APPROVAL_NAME = general_data.APPROVAL_NAME;
                    general_data.APPROVAL_APPROVISION_DATE = general_data.APPROVAL_APPROVISION_DATE;
                    general_data.AUTHO_SN = general_data.AUTHO_SN;
                    general_data.AUTHO_NAME = general_data.AUTHO_NAME;
                    general_data.AUTHO_RANK = general_data.AUTHO_RANK;
                    general_data.AUTHO_APPROVISION_DATE = general_data.AUTHO_APPROVISION_DATE;
                    general_data.PERMIT_SN = general_data.PERMIT_SN;
                    general_data.PERMIT_NAME = general_data.PERMIT_NAME;
                    general_data.PERMIT_RANK = general_data.PERMIT_RANK;
                    general_data.PERMIT_APPROVISION_DATE = general_data.PERMIT_APPROVISION_DATE;
                    general_data.BARCODE = general_data.BARCODE;
                    general_data.REJECTED = false;
                    general_data.ISOPENED = false;
                    general_data.ISPRINTED = general_data.ISPRINTED;
                    general_data.STATUS = general_data.STATUS;
                    db.Entry(general_data).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Index", "MyPasses");
                }
                //this section is for autho person 
                if (form["approvebtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2)
                {


                    //select permits cell authorized person to redirect for him the request
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 10 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return RedirectToAction("Edit", new { id = rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE });

                    }
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.SERVICE_NUMBER = rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER;
                    general_data.CIVIL_NUMBER = rA42_AIR_CREW_PASS_DTL.CIVIL_NUMBER;
                    general_data.RANK_A = rA42_AIR_CREW_PASS_DTL.RANK_A;
                    general_data.RANK_E = rA42_AIR_CREW_PASS_DTL.RANK_E;
                    general_data.NAME_A = rA42_AIR_CREW_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_AIR_CREW_PASS_DTL.NAME_E;
                    general_data.JOINING_DATE = rA42_AIR_CREW_PASS_DTL.JOINING_DATE;
                    general_data.PHONE_NUMBER = rA42_AIR_CREW_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_AIR_CREW_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE;
                    general_data.BLOOD_CODE = rA42_AIR_CREW_PASS_DTL.BLOOD_CODE;
                    general_data.GENDER_ID = rA42_AIR_CREW_PASS_DTL.GENDER_ID;
                    general_data.UNIT_A = rA42_AIR_CREW_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_AIR_CREW_PASS_DTL.UNIT_E;
                    general_data.PROFESSION_A = rA42_AIR_CREW_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_AIR_CREW_PASS_DTL.PROFESSION_E;
                    general_data.PASS_TYPE_CODE = rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_AIR_CREW_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_AIR_CREW_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_AIR_CREW_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_AIR_CREW_PASS_DTL.REMARKS;
                    general_data.APPROVAL_SN = currentUserInfo["user_sno"];
                    general_data.APPROVAL_NAME = currentUserInfo["user_name_ar"];
                    general_data.APPROVAL_RANK = currentUserInfo["user_rank_ar"];
                    general_data.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    general_data.AUTHO_SN = general_data.AUTHO_SN;
                    general_data.AUTHO_NAME = general_data.AUTHO_NAME;
                    general_data.AUTHO_RANK = general_data.AUTHO_RANK;
                    general_data.AUTHO_APPROVISION_DATE = general_data.AUTHO_APPROVISION_DATE;
                    general_data.PERMIT_SN = general_data.PERMIT_SN;
                    general_data.PERMIT_NAME = general_data.PERMIT_NAME;
                    general_data.PERMIT_RANK = general_data.PERMIT_RANK;
                    general_data.PERMIT_APPROVISION_DATE = general_data.PERMIT_APPROVISION_DATE;
                    general_data.BARCODE = general_data.BARCODE;
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
               
                //this section is for security ooficer 
                if (form["approvebtn"] != null && (x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 || x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 10))
                {




                    //redirect the permit to the permits cell to print the permit 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return RedirectToAction("Edit", new { id = rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE });

                    }
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.SERVICE_NUMBER = rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER;
                    general_data.CIVIL_NUMBER = rA42_AIR_CREW_PASS_DTL.CIVIL_NUMBER;
                    general_data.RANK_A = rA42_AIR_CREW_PASS_DTL.RANK_A;
                    general_data.RANK_E = rA42_AIR_CREW_PASS_DTL.RANK_E;
                    general_data.NAME_A = rA42_AIR_CREW_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_AIR_CREW_PASS_DTL.NAME_E;
                    general_data.JOINING_DATE = rA42_AIR_CREW_PASS_DTL.JOINING_DATE;
                    general_data.PHONE_NUMBER = rA42_AIR_CREW_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_AIR_CREW_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE;
                    general_data.BLOOD_CODE = rA42_AIR_CREW_PASS_DTL.BLOOD_CODE;
                    general_data.GENDER_ID = rA42_AIR_CREW_PASS_DTL.GENDER_ID;
                    general_data.UNIT_A = rA42_AIR_CREW_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_AIR_CREW_PASS_DTL.UNIT_E;
                    general_data.PROFESSION_A = rA42_AIR_CREW_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_AIR_CREW_PASS_DTL.PROFESSION_E;
                    general_data.PASS_TYPE_CODE = rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_AIR_CREW_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_AIR_CREW_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_AIR_CREW_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_AIR_CREW_PASS_DTL.REMARKS;
                    general_data.APPROVAL_SN = general_data.APPROVAL_SN;
                    general_data.APPROVAL_NAME = general_data.APPROVAL_NAME;
                    general_data.APPROVAL_RANK = general_data.APPROVAL_RANK;
                    general_data.APPROVAL_APPROVISION_DATE = general_data.APPROVAL_APPROVISION_DATE;
                    general_data.AUTHO_SN = currentUserInfo["user_sno"];
                    general_data.AUTHO_NAME = currentUserInfo["user_name_ar"];
                    general_data.AUTHO_RANK = currentUserInfo["user_rank_ar"];
                    general_data.AUTHO_APPROVISION_DATE = DateTime.Now;
                    general_data.PERMIT_SN = general_data.PERMIT_SN;
                    general_data.PERMIT_NAME = general_data.PERMIT_NAME;
                    general_data.PERMIT_RANK = general_data.PERMIT_RANK;
                    general_data.PERMIT_APPROVISION_DATE = general_data.PERMIT_APPROVISION_DATE;
                    general_data.BARCODE = rA42_AIR_CREW_PASS_DTL.BARCODE;
                    general_data.REJECTED = false;
                    general_data.STATUS = true;
                    general_data.ISOPENED = general_data.ISOPENED;
                    general_data.ISPRINTED = false;
                    db.Entry(general_data).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Newpasses");
                }
                //this section for autho person to reject the permit 
                if (form["rejectbtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2)
                {


                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return RedirectToAction("Edit", new { id = rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE });

                    }
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.SERVICE_NUMBER = rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER;
                    general_data.CIVIL_NUMBER = rA42_AIR_CREW_PASS_DTL.CIVIL_NUMBER;
                    general_data.RANK_A = rA42_AIR_CREW_PASS_DTL.RANK_A;
                    general_data.RANK_E = rA42_AIR_CREW_PASS_DTL.RANK_E;
                    general_data.NAME_A = rA42_AIR_CREW_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_AIR_CREW_PASS_DTL.NAME_E;
                    general_data.JOINING_DATE = rA42_AIR_CREW_PASS_DTL.JOINING_DATE;
                    general_data.PHONE_NUMBER = rA42_AIR_CREW_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_AIR_CREW_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE;
                    general_data.BLOOD_CODE = rA42_AIR_CREW_PASS_DTL.BLOOD_CODE;
                    general_data.GENDER_ID = rA42_AIR_CREW_PASS_DTL.GENDER_ID;
                    general_data.UNIT_A = rA42_AIR_CREW_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_AIR_CREW_PASS_DTL.UNIT_E;
                    general_data.PROFESSION_A = rA42_AIR_CREW_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_AIR_CREW_PASS_DTL.PROFESSION_E;
                    general_data.PASS_TYPE_CODE = rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_AIR_CREW_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_AIR_CREW_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_AIR_CREW_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_AIR_CREW_PASS_DTL.REMARKS;
                    general_data.APPROVAL_SN = general_data.APPROVAL_SN;
                    general_data.APPROVAL_NAME = general_data.APPROVAL_NAME;
                    general_data.APPROVAL_RANK = general_data.APPROVAL_RANK;
                    general_data.APPROVAL_APPROVISION_DATE = general_data.APPROVAL_APPROVISION_DATE;
                    general_data.AUTHO_SN = general_data.AUTHO_SN;
                    general_data.AUTHO_NAME = general_data.AUTHO_NAME;
                    general_data.AUTHO_RANK = general_data.AUTHO_RANK;
                    general_data.AUTHO_APPROVISION_DATE = general_data.AUTHO_APPROVISION_DATE;
                    general_data.PERMIT_SN = general_data.PERMIT_SN;
                    general_data.PERMIT_NAME = general_data.PERMIT_NAME;
                    general_data.PERMIT_RANK = general_data.PERMIT_RANK;
                    general_data.PERMIT_APPROVISION_DATE = general_data.PERMIT_APPROVISION_DATE;
                    general_data.BARCODE = general_data.BARCODE;
                    general_data.REJECTED = true;
                    general_data.ISOPENED = false;
                    general_data.STATUS = general_data.STATUS;
                    db.Entry(general_data).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Authopasses");
                }
                //this section is for permit cell to reject the permit and return it to autho person 
                if (form["rejectbtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 10)
                {


                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == general_data.APPROVAL_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return RedirectToAction("Edit", new { id = rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE });

                    }
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.SERVICE_NUMBER = rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER;
                    general_data.CIVIL_NUMBER = rA42_AIR_CREW_PASS_DTL.CIVIL_NUMBER;
                    general_data.RANK_A = rA42_AIR_CREW_PASS_DTL.RANK_A;
                    general_data.RANK_E = rA42_AIR_CREW_PASS_DTL.RANK_E;
                    general_data.NAME_A = rA42_AIR_CREW_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_AIR_CREW_PASS_DTL.NAME_E;
                    general_data.JOINING_DATE = rA42_AIR_CREW_PASS_DTL.JOINING_DATE;
                    general_data.PHONE_NUMBER = rA42_AIR_CREW_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_AIR_CREW_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE;
                    general_data.BLOOD_CODE = rA42_AIR_CREW_PASS_DTL.BLOOD_CODE;
                    general_data.GENDER_ID = rA42_AIR_CREW_PASS_DTL.GENDER_ID;
                    general_data.UNIT_A = rA42_AIR_CREW_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_AIR_CREW_PASS_DTL.UNIT_E;
                    general_data.PROFESSION_A = rA42_AIR_CREW_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_AIR_CREW_PASS_DTL.PROFESSION_E;
                    general_data.PASS_TYPE_CODE = rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_AIR_CREW_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_AIR_CREW_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_AIR_CREW_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_AIR_CREW_PASS_DTL.REMARKS;
                    general_data.APPROVAL_SN = general_data.APPROVAL_SN;
                    general_data.APPROVAL_NAME = general_data.APPROVAL_NAME;
                    general_data.APPROVAL_RANK = general_data.APPROVAL_RANK;
                    general_data.APPROVAL_APPROVISION_DATE = general_data.APPROVAL_APPROVISION_DATE;
                    general_data.AUTHO_SN = general_data.AUTHO_SN;
                    general_data.AUTHO_NAME = general_data.AUTHO_NAME;
                    general_data.AUTHO_RANK = general_data.AUTHO_RANK;
                    general_data.AUTHO_APPROVISION_DATE = general_data.AUTHO_APPROVISION_DATE;
                    general_data.PERMIT_SN = general_data.PERMIT_SN;
                    general_data.PERMIT_NAME = general_data.PERMIT_NAME;
                    general_data.PERMIT_RANK = general_data.PERMIT_RANK;
                    general_data.PERMIT_APPROVISION_DATE = general_data.PERMIT_APPROVISION_DATE;
                    general_data.BARCODE = general_data.BARCODE;
                    general_data.REJECTED = true;
                    general_data.STATUS = general_data.STATUS;
                    db.Entry(general_data).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Newpasses");
                }
               


            }
            TempData["Erorr"] = "Somthing wrong happen,حدث خطأ ما";
            AddToast(new Toast("",
                GetResourcesValue("error_update_message"),
                "red"));
            return View(rA42_AIR_CREW_PASS_DTL);
        }

        // GET: Securitypass/Renew
        public ActionResult Renew(int? id)
        {
            ViewBag.activetab = "renew";

            if (id == null)
            {
                return NotFound();
            }
            //check if the permit id is in the table 
            RA42_AIR_CREW_PASS_DTL rA42_AIR_CREW_PASS_DTL = db.RA42_AIR_CREW_PASS_DTL.Find(id);
            if (rA42_AIR_CREW_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if current user is authorized 
            if (rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_AIR_CREW_PASS_DTL.RESPONSIBLE == currentUser)
            {
                if (rA42_AIR_CREW_PASS_DTL.DATE_TO != null)
                {

                    string date = rA42_AIR_CREW_PASS_DTL.CheckDate(rA42_AIR_CREW_PASS_DTL.DATE_TO.Value);
                    if (date == "منتهي" || date == "Expired")
                    {

                    }
                    else
                    {
                        if (ViewBag.DEVELOPER != true)
                        {
                            return NotFound();
                        }
                    }
                }
            }
            else
            {
                if (ViewBag.RESPO_STATE != rA42_AIR_CREW_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                {
                    if (rA42_AIR_CREW_PASS_DTL.DATE_TO != null)
                    {

                        string date = rA42_AIR_CREW_PASS_DTL.CheckDate(rA42_AIR_CREW_PASS_DTL.DATE_TO.Value);
                        if (date == "منتهي" || date == "Expired")
                        {

                        }
                        else
                        {
                            if (ViewBag.DEVELOPER != true)
                            {
                                return NotFound();
                            }
                        }
                    }


                }
            }

            if (Language.GetCurrentLang() == "en")
            {
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_AIR_CREW_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE);
                //get permit types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE);
                //get documents types in english for this kind of permit 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                 //get autho person for this kind of permit in english 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if the current user is equal 1 or less than 1 
                    if (ViewBag.RESPO_STATE <= 1)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    }


                }

               
            }
            else
            {
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_AIR_CREW_PASS_DTL.GENDER_ID);
                //get idnetities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE);
                //get permit types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE);
                 //get autho person in arabic for this kind of permit 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //if current user is 1 or ess than one show error message 
                    if (ViewBag.RESPO_STATE <= 1)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                    }

                }

               
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
             }

            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (rA42_AIR_CREW_PASS_DTL.STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get documents of this kind of permit to compare between uploaded documenst and missing documenst 
            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_AIR_CREW_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                                  select new
                                  {
                                      FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                      FILE_TYPE = c.FILE_TYPE,
                                      FILE_TYPE_E = c.FILE_TYPE_E,

                                  }).Count();

            ViewBag.PASS_FILES_2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                    join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                    join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_AIR_CREW_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            //get uploaded image 
            ViewBag.PERSONAL_IMAGE = rA42_AIR_CREW_PASS_DTL.PERSONAL_IMAGE;
           //get selected files of the perimt 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get bloods
            ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE", rA42_AIR_CREW_PASS_DTL.BLOOD_CODE);

            rA42_AIR_CREW_PASS_DTL.DATE_FROM = null;
            rA42_AIR_CREW_PASS_DTL.DATE_TO = null;
            return View(rA42_AIR_CREW_PASS_DTL);
        }

        // POST: Securitypass/renew
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Renew(RA42_AIR_CREW_PASS_DTL rA42_AIR_CREW_PASS_DTL,
             int[] FILE_TYPES,int AIR_CREW_CODE, string[] FILE_TYPES_TEXT, HttpPostedFileBase[]
            files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "renew";
            var general_data = db.RA42_AIR_CREW_PASS_DTL.Where(a => a.AIR_CREW_PASS_CODE == AIR_CREW_CODE).FirstOrDefault();
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (general_data.STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get personal image 
            ViewBag.PERSONAL_IMAGE = general_data.PERSONAL_IMAGE;
             //get selected documents for this permit 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == AIR_CREW_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get bloods
            ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE", rA42_AIR_CREW_PASS_DTL.BLOOD_CODE);
            //get documents of this kind of permit to compare between uploaded documenst and missing documenst 
            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == general_data.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                                  select new
                                  {
                                      FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                      FILE_TYPE = c.FILE_TYPE,
                                      FILE_TYPE_E = c.FILE_TYPE_E,

                                  }).Count();

            ViewBag.PASS_FILES_2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                    join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                    join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == general_data.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_AIR_CREW_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE);
                //get permits types in englsih 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE);
               //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
               //get autho person for this kind of permit in english 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE ).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if current user is less than or equal 1
                    if (ViewBag.RESPO_STATE <= 1)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(general_data);
                    }

                }

               
            }
            else
            {
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_AIR_CREW_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_AIR_CREW_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_AIR_CREW_PASS_DTL.PASS_TYPE_CODE);
                //get documents types in arabic for this kind of permit 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error if the current user is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(general_data);
                    }

                }

               
            }


            //editing start from here
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


                            //check file extention
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {

                                fileName = "Profile_" + ACCESS_TYPE_CODE + DateTime.Now.ToString("yymmssfff") + extension;

                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);
                                if (check != true)
                                {
                                    AddToast(new Toast("",
                                   GetResourcesValue("error_update_message"),
                                   "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(general_data);
                                }
                                rA42_AIR_CREW_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error message if image format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(general_data);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
               
                
               


                //get image name iand inserted to colomun in db table 
                if (rA42_AIR_CREW_PASS_DTL.PERSONAL_IMAGE != null)
                {
                    rA42_AIR_CREW_PASS_DTL.PERSONAL_IMAGE = rA42_AIR_CREW_PASS_DTL.PERSONAL_IMAGE;
                }
                else
                {
                    rA42_AIR_CREW_PASS_DTL.PERSONAL_IMAGE = general_data.PERSONAL_IMAGE;

                }
                rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER = rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER;
                rA42_AIR_CREW_PASS_DTL.RESPONSIBLE = currentUser;
                rA42_AIR_CREW_PASS_DTL.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                rA42_AIR_CREW_PASS_DTL.RANK_A = rA42_AIR_CREW_PASS_DTL.RANK_A;
                rA42_AIR_CREW_PASS_DTL.RANK_E = rA42_AIR_CREW_PASS_DTL.RANK_E;
                rA42_AIR_CREW_PASS_DTL.NAME_A = rA42_AIR_CREW_PASS_DTL.NAME_A;
                rA42_AIR_CREW_PASS_DTL.NAME_E = rA42_AIR_CREW_PASS_DTL.NAME_E;
                rA42_AIR_CREW_PASS_DTL.UNIT_A = rA42_AIR_CREW_PASS_DTL.UNIT_A;
                rA42_AIR_CREW_PASS_DTL.UNIT_E = rA42_AIR_CREW_PASS_DTL.UNIT_E;
                rA42_AIR_CREW_PASS_DTL.PROFESSION_A = rA42_AIR_CREW_PASS_DTL.PROFESSION_A;
                rA42_AIR_CREW_PASS_DTL.PROFESSION_E = rA42_AIR_CREW_PASS_DTL.PROFESSION_E;
                rA42_AIR_CREW_PASS_DTL.JOINING_DATE = rA42_AIR_CREW_PASS_DTL.JOINING_DATE;

                //get current user military info from api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;
                //this section is for applicant 
                if (WORKFLOWID <= 1 || ViewBag.NOT_RELATED_STATION == true)
                {

                    rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_AIR_CREW_PASS_DTL.REJECTED = false;
                    rA42_AIR_CREW_PASS_DTL.STATUS = false;
                    rA42_AIR_CREW_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person (المنسق الأمني)
                if (WORKFLOWID == 2)
                {
                    //the autho person should redirect the permit to the permits cell after createing it 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 10 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(general_data);

                    }
                    else
                    {
                        rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_AIR_CREW_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_AIR_CREW_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                    rA42_AIR_CREW_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                    rA42_AIR_CREW_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_AIR_CREW_PASS_DTL.REJECTED = false;
                    rA42_AIR_CREW_PASS_DTL.STATUS = false;
                    rA42_AIR_CREW_PASS_DTL.ISOPENED = true;
                }
               
                //this section is for security officer 
                if ((WORKFLOWID == 3 || WORKFLOWID == 10))
                {
                    //after security officer complete every thing the permit will redirected to permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(general_data);

                    }
                    else
                    {
                        rA42_AIR_CREW_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }


                    rA42_AIR_CREW_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_AIR_CREW_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_AIR_CREW_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    rA42_AIR_CREW_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_AIR_CREW_PASS_DTL.REJECTED = false;
                    rA42_AIR_CREW_PASS_DTL.STATUS = true;
                    rA42_AIR_CREW_PASS_DTL.ISOPENED = true;
                    rA42_AIR_CREW_PASS_DTL.BARCODE = rA42_AIR_CREW_PASS_DTL.BARCODE;
                }
                rA42_AIR_CREW_PASS_DTL.STATION_CODE = general_data.STATION_CODE;
                rA42_AIR_CREW_PASS_DTL.CRD_BY = currentUser;
                rA42_AIR_CREW_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_AIR_CREW_PASS_DTL.UPD_BY = currentUser;
                rA42_AIR_CREW_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_AIR_CREW_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                
                db.RA42_AIR_CREW_PASS_DTL.Add(rA42_AIR_CREW_PASS_DTL);
                db.SaveChanges();

                
                //add selected documents
                if (files != null)
                {

                    try
                    {
                        //create foreach loop to upload multiple files
                        int c = 0;
                        foreach (HttpPostedFileBase file in files)
                        {
                            // Verify that the user selected a file
                            if (file != null && file.ContentLength > 0)
                            {
                                // extract only the filename with extention
                                string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                                string extension = Path.GetExtension(file.FileName);


                                //check file extention
                                if (general.CheckFileType(file.FileName))
                                {

                                    fileName = "FileNO" + c + "_3_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);
                                    //add new file
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE,
                                        FILE_TYPE = FILE_TYPES[c],
                                        FILE_TYPE_TEXT = FILE_TYPES_TEXT[c],
                                        FILE_NAME = fileName,
                                        CRD_BY = currentUser,
                                        CRD_DT = DateTime.Now


                                    };
                                    db.RA42_FILES_MST.Add(fILES_MST);
                                    db.SaveChanges();
                                    c++;
                                }
                                else
                                {
                                    //delete all uploaded documents of this request if there is somthing wrong with one file 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return RedirectToAction("Renew", new { id = AIR_CREW_CODE });
                                }
                            }

                            else
                            {


                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                //add previues files to the permit
                var selected_files = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == AIR_CREW_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
                foreach (var file in selected_files)
                {
                    //add new file
                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                    {
                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                        ACCESS_ROW_CODE = rA42_AIR_CREW_PASS_DTL.AIR_CREW_PASS_CODE,
                        FILE_TYPE = file.FILE_TYPE,
                        FILE_TYPE_TEXT = file.FILE_TYPE_TEXT,
                        FILE_NAME = file.FILE_NAME,
                        CRD_BY = currentUser,
                        CRD_DT = DateTime.Now


                    };
                    db.RA42_FILES_MST.Add(fILES_MST);
                    db.SaveChanges();
                }
                AddToast(new Toast("",
                 GetResourcesValue("success_create_message"),
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
            TempData["Erorr"] = "Somthing wrong happen,حدث خطأ ما";
            AddToast(new Toast("",
                GetResourcesValue("error_update_message"),
                "red"));
            return View(general_data);
        }

        // this is delete view to delete permit 
        public ActionResult Delete(int? id)
        {
            ViewBag.activetab = "delete";

            if (id == null)
            {
                 return NotFound();
            }
            //check if security permit id is in the table 
            RA42_AIR_CREW_PASS_DTL rA42_AIR_CREW_PASS_DTL = db.RA42_AIR_CREW_PASS_DTL.Find(id);
            if (rA42_AIR_CREW_PASS_DTL == null)
            {
                                return NotFound();
            }

            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_AIR_CREW_PASS_DTL.RESPONSIBLE != currentUser)
                {
                    if (rA42_AIR_CREW_PASS_DTL.STATUS == true)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {

                            return NotFound();
                        }
                    }
                }

                if (rA42_AIR_CREW_PASS_DTL.ISOPENED == true)
                {
                    if (rA42_AIR_CREW_PASS_DTL.STATUS == true)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {

                            return NotFound();
                        }
                    }
                }
            }
            else
            {
                if (rA42_AIR_CREW_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_AIR_CREW_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    //if (ViewBag.RESPO_STATE != rA42_AIR_CREW_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                    //{
                    //    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    //    {
                    //        return NotFound();
                    //    }
                    //}
                }
            }

           
            //get selected documents and files 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
           
            return View(rA42_AIR_CREW_PASS_DTL);
        }

        // confirm deleting view 
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var general_data = db.RA42_AIR_CREW_PASS_DTL.Where(a => a.AIR_CREW_PASS_CODE == id).FirstOrDefault();

            if (general_data !=null)
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

        //this finction to delete any document of any permit 
        [HttpPost]
        public JsonResult DeleteFile(int id)
        {
            bool result = false;
            RA42_FILES_MST rA42_FILES = db.RA42_FILES_MST.Find(id);
            if (rA42_FILES != null)
            {
                string filpath = "~/Documents/" + rA42_FILES.FILE_NAME;
                general.RemoveFileFromServer(filpath);
                db.RA42_FILES_MST.Remove(rA42_FILES);
                db.SaveChanges();
                AddToast(new Toast("",
                GetResourcesValue("success_delete_message"),
               "green"));
                result = true;
            }

            return Json(result, JsonRequestBehavior.AllowGet);


        }
        //this function is for deleteing PERSONAL_IMAGE of any permit 
        [HttpPost]
        public JsonResult DeleteImage(int id)
        {
            bool result = false;
            var general_data = db.RA42_AIR_CREW_PASS_DTL.Where(a => a.AIR_CREW_PASS_CODE == id).FirstOrDefault();

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
        //this function is for deleting zone and gate of any permit 
        [HttpPost]
        public JsonResult DeleteZone(int id)
        {
            bool result = false;
            RA42_ZONE_MASTER_MST rzONE_MASTER_MST = db.RA42_ZONE_MASTER_MST.Find(id);
            if (rzONE_MASTER_MST != null)
            {
                db.RA42_ZONE_MASTER_MST.Remove(rzONE_MASTER_MST);
                db.SaveChanges();
                AddToast(new Toast("",
                GetResourcesValue("success_delete_message"),
               "green"));
                result = true;
            }

            return Json(result, JsonRequestBehavior.AllowGet);


        }
        //this function is to print card of any permit, this is also used when save card as pdf file, save file as temprory permit 
        [HttpPost]
        public JsonResult PrintById(int id, string type)
        {

            bool result = false;
            var general_data = db.RA42_AIR_CREW_PASS_DTL.Where(a => a.AIR_CREW_PASS_CODE == id).FirstOrDefault();

            if (general_data != null)
            {


                general_data.UPD_BY = currentUser;
                general_data.UPD_DT = DateTime.Now;
                general_data.ISPRINTED = true;
                db.Entry(general_data).State = EntityState.Modified;
                db.SaveChanges();

                var checkifPrint = db.RA42_PRINT_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.PASS_ROW_CODE == id && a.DLT_STS !=true).ToList();
                if(checkifPrint.Count > 0)
                {
                      foreach(var item in checkifPrint)
                    {
                        item.DLT_STS = true;
                        item.UPD_BY = currentUser;
                        item.UPD_DT = DateTime.Now;
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
