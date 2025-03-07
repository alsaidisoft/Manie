using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
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

namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    //this controller for company permit, normal companies not contracted companies 
	public class TemprorycompanypassController : Controller
	{
        //get db connection 
        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify General Functions class to use some important functions 
        private GeneralFunctions general = new GeneralFunctions();
        private int STATION_CODE = 0;
        private int WORKFLOWID = 0;
        private int RESPO_CODE = 0;
        private int ACCESS_TYPE_CODE = 5;
        private int COMPANY_TYPE_CODE = 2;
        private int FORCE_ID = 0;

        string title = Resources.Passes.ResourceManager.GetString("TemproryCompanyPass" + "_" + "ar");
        public TemprorycompanypassController() {
            ViewBag.Managepasses = "Managepasses";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Temprorycompanypass";
            //set icon of the controller from fontawsome 
			ViewBag.controllerIconClass = "fa fa-hotel";
            if(Language.GetCurrentLang() == "en")
            {
                title = Resources.Passes.ResourceManager.GetString("TemproryCompanyPass" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;

            //check if current user has authority in this type of permit 
            var v = Task.Run(async () => await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefaultAsync()).Result;
            if (v != null)
            {
                if (v.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && v.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false)
                {
                    //get ide of the user 
                    ViewBag.RESPO_ID = v.WORKFLOW_RESPO_CODE;
                    //get workflow id type 
                    ViewBag.RESPO_STATE = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID;
                    RESPO_CODE = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE;
                    WORKFLOWID = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value;
                    //get unit-code of the current user 
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

                //FORCE_ID = 1;
            }
        }

        //this is comments view of specific permit 
        public ActionResult Comments(int? id)
        {
            ViewBag.activetab = "Comments";

            if (id == null)
            {
                return NotFound();
            }

            //check if the permit request is in the table RA42_COMPANY_PASS_DTL
            RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL = db.RA42_COMPANY_PASS_DTL.Find(id);
            if (rA42_COMPANY_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if the current user has authority to open this view 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_COMPANY_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_COMPANY_PASS_DTL.RESPONSIBLE !=currentUser)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }
            else
            {
                if (rA42_COMPANY_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_COMPANY_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != rA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            return NotFound();
                        }
                    }
                }
            }

            //get comments 
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            return View(rA42_COMPANY_PASS_DTL);
        }

        // POST new comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Comments(RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL, string COMMENT)
        {
            ViewBag.activetab = "Comments";
            var general_data = db.RA42_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE).FirstOrDefault();





            //add comments
            if (COMMENT.Length > 0)
            {
                RA42_COMMENTS_MST rA42_COMMENT = new RA42_COMMENTS_MST();
                rA42_COMMENT.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_COMMENT.PASS_ROW_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE;
                rA42_COMMENT.CRD_BY = currentUser;
                rA42_COMMENT.CRD_DT = DateTime.Now;
                rA42_COMMENT.COMMENT = COMMENT;
                db.RA42_COMMENTS_MST.Add(rA42_COMMENT);
                db.SaveChanges();
                AddToast(new Toast("",
                  GetResourcesValue("add_comment_success"),
                  "green"));

            }
            //get all comments of this permit 
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            return View(rA42_COMPANY_PASS_DTL);


        }
        // this view to show links 
        public ActionResult Index()
		{
           
            return View();
		}
        //view for choose permit creation type
        public ActionResult Choosecreatetype()
        {
            ViewBag.activetab = "Privatepass";
            return View();
        }
        //all permits for companies for developer and admin
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


            var empList = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.RA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE != null).Select(a => new
            {
                TEMPRORY_COMPANY_PASS_CODE = a.TEMPRORY_COMPANY_PASS_CODE,
                COMPANY_PASS_CODE = a.COMPANY_PASS_CODE,
                CIVIL_NUMBER = (a.ID_CARD_NUMBER != null ? a.ID_CARD_NUMBER : " "),
                PERSONAL_IMAGE = a.PERSONAL_IMAGE,
                COMPANY_A = (a.RA42_COMPANY_PASS_DTL.COMPANY_CODE != null ? a.RA42_COMPANY_PASS_DTL.RA42_COMPANY_MST.COMPANY_NAME : " "),
                COMPANY_E = (a.RA42_COMPANY_PASS_DTL.COMPANY_CODE != null ? a.RA42_COMPANY_PASS_DTL.RA42_COMPANY_MST.COMPANY_NAME_E : " "),
                NAME_A = (a.NAME_A != null ? a.NAME_A : " "),
                NAME_E = (a.NAME_E != null ? a.NAME_E : " "),
                GSM = (a.GSM != null ? a.GSM : " "),
                PURPOSE_OF_PASS = (a.RA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS != null ? a.RA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS : " "),
                PROFESSION_A = (a.PROFESSION_A != null ? a.PROFESSION_A : " "),
                PROFESSION_E = (a.PROFESSION_E != null ? a.PROFESSION_E : " "),
                STATION_CODE = a.RA42_COMPANY_PASS_DTL.STATION_CODE,
                FORCE_ID = a.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID.Value,
                STATION_A = a.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.STATION_NAME_A,
                STATION_E = a.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.STATION_NAME_E,
                RESPONSEPLE_NAME = a.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                RESPONSEPLE_NAME_E = a.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E,
                STEP_NAME = a.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME,
                STEP_NAME_E = a.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E,
                STATUS = a.RA42_COMPANY_PASS_DTL.STATUS,
                DLT_STS = a.DLT_STS,
                REJECTED = a.RA42_COMPANY_PASS_DTL.REJECTED,
                ISPRINTED = a.ISPRINTED,
                DATE_FROM = a.DATE_FROM,
                DATE_TO = a.DATE_TO,
                COMMENTS = a.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == a.COMPANY_PASS_CODE).Count()


            }
             ).ToList();
            int totalrows = empList.Count;
            if (ViewBag.ADMIN == true)
            {
                //get force id
                int force = Convert.ToInt32(ViewBag.FORCE_TYPE_CODE);
                empList = empList.Where(a => a.FORCE_ID == force).ToList();
            }
            if (!string.IsNullOrEmpty(searchValue))//filter
            {
                
                if (searchValue.Contains("منتهي"))
                {
                    empList = empList.Where(z => z.DATE_TO < DateTime.Today).ToList();
                }
                else
                {
                    empList = empList.
                Where(x => x.CIVIL_NUMBER.Contains(searchValue) || x.NAME_A.Contains(searchValue) || x.NAME_E.Contains(searchValue)
                || x.PROFESSION_A.Contains(searchValue) || x.PROFESSION_E.Contains(searchValue)
                || x.PURPOSE_OF_PASS.Contains(searchValue) || x.COMPANY_A.Contains(searchValue)
                || x.GSM.Contains(searchValue) || x.STEP_NAME.Contains(searchValue) || x.STATION_A == searchValue
                ).ToList();
                }
            }
            int totalrowsafterfiltering = empList.Count;
            //sorting
            empList = empList.OrderBy(sortColumnName + " " + sortDirection).ToList();
            //paging
            empList = empList.Skip(start).Take(length).ToList();


            return Json(new { data = empList, draw = Request["draw"], recordsTotal = totalrows, recordsFiltered = totalrowsafterfiltering }, JsonRequestBehavior.AllowGet);


        }
        //all printed permits of companies
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


            var empList = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID && a.RA42_COMPANY_PASS_DTL.STATION_CODE == STATION_CODE && a.DLT_STS != true  && a.ISPRINTED == true && a.RA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE !=null).Select(a => new
            {
                TEMPRORY_COMPANY_PASS_CODE = a.TEMPRORY_COMPANY_PASS_CODE,
                COMPANY_PASS_CODE = a.COMPANY_PASS_CODE,
                CIVIL_NUMBER = (a.ID_CARD_NUMBER != null ? a.ID_CARD_NUMBER : " "),
                PERSONAL_IMAGE = a.PERSONAL_IMAGE,
                COMPANY_A = (a.RA42_COMPANY_PASS_DTL.COMPANY_CODE != null ? a.RA42_COMPANY_PASS_DTL.RA42_COMPANY_MST.COMPANY_NAME : " "),
                COMPANY_E = (a.RA42_COMPANY_PASS_DTL.COMPANY_CODE != null ? a.RA42_COMPANY_PASS_DTL.RA42_COMPANY_MST.COMPANY_NAME_E : " "),
                NAME_A = (a.NAME_A != null ? a.NAME_A : " "),
                NAME_E = (a.NAME_E != null ? a.NAME_E : " "),
                GSM = (a.GSM != null ? a.GSM : " "),
                PURPOSE_OF_PASS = (a.RA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS != null ? a.RA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS : " "),
                PROFESSION_A = (a.PROFESSION_A != null ? a.PROFESSION_A : " "),
                PROFESSION_E = (a.PROFESSION_E != null ? a.PROFESSION_E : " "),
                PASS_TYPE = a.RA42_PASS_TYPE_MST.PASS_TYPE,
                STATION_CODE = a.RA42_COMPANY_PASS_DTL.STATION_CODE,
                STATION_A = a.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.STATION_NAME_A,
                STATION_E = a.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.STATION_NAME_E,
                RESPONSEPLE_NAME = a.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                RESPONSEPLE_NAME_E = a.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E,
                STEP_NAME = a.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME,
                STEP_NAME_E = a.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E,
                STATUS = a.RA42_COMPANY_PASS_DTL.STATUS,
                RETURNED = a.RETURNED,
                ACCESS_TYPE_CODE = a.ACCESS_TYPE_CODE.Value,
                DLT_STS = a.DLT_STS,
                REJECTED = a.RA42_COMPANY_PASS_DTL.REJECTED,
                ISPRINTED = a.ISPRINTED,
                DATE_FROM = a.DATE_FROM,
                DATE_TO = a.DATE_TO,
                COMMENTS = a.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == a.COMPANY_PASS_CODE).Count()


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
                    empList = empList.
                Where(x => x.CIVIL_NUMBER.Contains(searchValue) || x.NAME_A.Contains(searchValue) || x.NAME_E.Contains(searchValue) 
                || x.PROFESSION_A.Contains(searchValue) || x.PROFESSION_E.Contains(searchValue) || x.COMPANY_A.Contains(searchValue) 
                || x.PURPOSE_OF_PASS.Contains(searchValue) || x.GSM.Contains(searchValue) 
                || x.STATION_A == searchValue).ToList();
                }
            }
            int totalrowsafterfiltering = empList.Count;
            //sorting
            //empList = empList.OrderBy(sortColumnName + " " + sortDirection).ToList<RA42_VECHILE_PASS_DTL>();
            empList = empList.OrderBy(sortColumnName + " " + sortDirection).ToList();
            //paging
            empList = empList.Skip(start).Take(length).ToList();


            return Json(new { data = empList, draw = Request["draw"], recordsTotal = totalrows, recordsFiltered = totalrowsafterfiltering }, JsonRequestBehavior.AllowGet);


        }
        //this view is for administrator and developer 
        public ActionResult Allpasses()
        {

            ViewBag.activetab = "Allpasses";
            return View();
        }
        //this view is for autho person (المنسق الأمني)
        public ActionResult Authopasses()
        {
            ViewBag.activetab = "Authopasses";
            var rA42_COMPANY_PASS_DTL = db.RA42_COMPANY_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE).OrderByDescending(a => a.COMPANY_PASS_CODE);
            return View(rA42_COMPANY_PASS_DTL.ToList());
        }
        //this view is for permits cell and security officer 
        public async Task<ActionResult> Newpasses()
        {
            ViewBag.activetab = "Newpasses";
            var rA42_COMPANY_PASS_DTL = await db.RA42_COMPANY_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID && a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATUS != true).OrderByDescending(a => a.COMPANY_PASS_CODE).ToListAsync();
            return View(rA42_COMPANY_PASS_DTL);
        }
        public async Task<ActionResult> ToPrint()
        {
            ViewBag.activetab = "ToPrint";
            var rA42_COMPANY_PASS_DTL = await db.RA42_COMPANY_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID && a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATUS == true && a.ISPRINTED !=true).OrderByDescending(a => a.COMPANY_PASS_CODE).ToListAsync();
            return View(rA42_COMPANY_PASS_DTL);
        }
        //this view is for permits cell only, show all printed permits for their stations 
        public ActionResult Printed()
        {
            ViewBag.activetab = "Printed";
            return View();
        }
       
        //get subzones of main zones as json result 
        public JsonResult GetSubZones(int zone)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var sub_zones = db.RA42_ZONE_SUB_AREA_MST.Where(a => a.ZONE_CODE == zone && a.DLT_STS != true).Select(a => new { ZONE_SUB_AREA_CODE = a.ZONE_SUB_AREA_CODE, SUB_ZONE_NAME = a.SUB_ZONE_NAME }).ToList();

            //Add JsonRequest behavior to allow retrieving states over http get
            return Json(sub_zones, JsonRequestBehavior.AllowGet);
        }
        // this is the main details view of the main request, will show all the employee registred in this request 
        public ActionResult Details(int? id)
		{
            ViewBag.activetab = "details";

            if (id == null)
            {
                return NotFound();
            }
           

            RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL = db.RA42_COMPANY_PASS_DTL.Find(id);
            if (rA42_COMPANY_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if the current user has authority to open this view 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_COMPANY_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_COMPANY_PASS_DTL.RESPONSIBLE !=currentUser)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }
           
           
            //get selected documents and zones and employess registerd in this request 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.GetEmployees = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == id && a.DLT_STS != true).ToList();
            return View(rA42_COMPANY_PASS_DTL);
		}

        // this is the main print view of the main request
        public ActionResult PrintAll(int? id)
        {
            ViewBag.activetab = "PrintAll";

            if (id == null)
            {
                return NotFound();
            }


            RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL = db.RA42_COMPANY_PASS_DTL.Find(id);
            if (rA42_COMPANY_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if the current user has authority to open this view 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_COMPANY_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_COMPANY_PASS_DTL.RESPONSIBLE != currentUser)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }

            //get selected documents and zones and employess registerd in this request 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.GetEmployees = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == id && a.DLT_STS != true).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToList();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToList();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }
            return View(rA42_COMPANY_PASS_DTL);
        }


        [HttpPost]
        public ActionResult PrintAll(string CheckPrinted, int TRANSACTION_TYPE_CODE, string TRANSACTION_REMARKS, HttpPostedFileBase RECEIPT, RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL)
        {
            ViewBag.activetab = "PrintAll";


            RA42_COMPANY_PASS_DTL v = db.RA42_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE).FirstOrDefault();
            if (v == null)
            {
                return NotFound();
            }

            //get selected documents and zones and employess registerd in this request 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.GetEmployees = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == v.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToList();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == v.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToList();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }
            foreach (var item in v.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true)) { 
            item.UPD_BY = currentUser;
            item.UPD_DT = DateTime.Now;
            item.ISPRINTED = true;
            item.ISDELIVERED = false;
            db.SaveChanges();
                RA42_TRANSACTION_DTL rA42_TRANSACTION_DTL = new RA42_TRANSACTION_DTL();
                rA42_TRANSACTION_DTL.ACCESS_ROW_CODE = item.TEMPRORY_COMPANY_PASS_CODE;
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
                                return View(rA42_COMPANY_PASS_DTL);
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
                TempData["Success"] = "تم تحديث المعاملة بنجاح";
            if (CheckPrinted.Equals("Printed"))
            {
                var deletePrinted = db.RA42_PRINT_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.PASS_ROW_CODE ==
                item.TEMPRORY_COMPANY_PASS_CODE).ToList();
                if (deletePrinted.Count > 0)
                {
                    foreach (var ii in deletePrinted)
                    {
                        ii.DLT_STS = true;
                        db.SaveChanges();
                    }
                }

            }

                RA42_PRINT_MST p = new RA42_PRINT_MST();
                p.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                p.PASS_ROW_CODE = item.TEMPRORY_COMPANY_PASS_CODE;
                p.UPD_BY = currentUser;
                p.UPD_DT = DateTime.Now;
                db.RA42_PRINT_MST.Add(p);
                db.SaveChanges();
            }
            return View(v);
        }

        // this is the main print view of the main request
        public ActionResult Card(int? id)
        {
            ViewBag.activetab = "card";

            if (id == null)
            {
                return NotFound();
            }
            RA42_TEMPRORY_COMPANY_PASS_DTL rA42_TEMPRORY_COMPANY_PASS_DTL = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Find(id);
            if (rA42_TEMPRORY_COMPANY_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if current user has authority to open this view 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RESPONSIBLE !=currentUser)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }
            else
            {
                if (rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            return NotFound();
                        }
                    }
                }
            }
            //get zones and dgates and documents selected for the main request 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToList();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToList();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }
            return View(rA42_TEMPRORY_COMPANY_PASS_DTL);
        }


        [HttpPost]
        public ActionResult Card(string CheckPrinted, int TRANSACTION_TYPE_CODE, string TRANSACTION_REMARKS, HttpPostedFileBase RECEIPT, RA42_TEMPRORY_COMPANY_PASS_DTL _TEMPRORY_COMPANY_PASS_DTL)
        {
            ViewBag.activetab = "card";

            
            RA42_TEMPRORY_COMPANY_PASS_DTL rA42_TEMPRORY_COMPANY_PASS_DTL = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Find(_TEMPRORY_COMPANY_PASS_DTL.TEMPRORY_COMPANY_PASS_CODE);
            if (rA42_TEMPRORY_COMPANY_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if current user has authority to open this view 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RESPONSIBLE != currentUser)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }
            else
            {
                if (rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            return NotFound();
                        }
                    }
                }
            }
            //get zones and dgates and documents selected for the main request 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToList();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToList();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }

            RA42_TRANSACTION_DTL rA42_TRANSACTION_DTL = new RA42_TRANSACTION_DTL();
            rA42_TRANSACTION_DTL.ACCESS_ROW_CODE = _TEMPRORY_COMPANY_PASS_DTL.TEMPRORY_COMPANY_PASS_CODE;
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
                            return View(rA42_TEMPRORY_COMPANY_PASS_DTL);
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
            rA42_TEMPRORY_COMPANY_PASS_DTL.UPD_BY = currentUser;
            rA42_TEMPRORY_COMPANY_PASS_DTL.UPD_DT = DateTime.Now;
            rA42_TEMPRORY_COMPANY_PASS_DTL.ISDELIVERED = false;
            db.SaveChanges();
            TempData["Success"] = "تم تحديث المعاملة بنجاح";
            if (CheckPrinted.Equals("Printed"))
            {
                var deletePrinted = db.RA42_PRINT_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.PASS_ROW_CODE ==
                rA42_TEMPRORY_COMPANY_PASS_DTL.TEMPRORY_COMPANY_PASS_CODE).ToList();
                if (deletePrinted.Count > 0)
                {
                    foreach (var item in deletePrinted)
                    {
                        item.DLT_STS = true;
                        db.SaveChanges();
                    }
                }
              
            }
            return View(rA42_TEMPRORY_COMPANY_PASS_DTL);
        }

        // this view is for applicant to create new company permit for workers 
        public ActionResult Create()
		{
            ViewBag.activetab = "Create";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            //int unit = 0;
            //check if session not null 
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //this below code is to check is the request in HQ or other stations, to view (الركن المختص أو قائد الجناح - السرب)
                ViewBag.Get_Station_Code = id.ToString();
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
                    FORCE_ID = check_unit.FORCE_ID.Value;


                }
                int station = int.Parse(id.ToString());
                STATION_CODE = station;
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


                //this below code is to check is the request in HQ or other stations, to view (الركن المختص أو قائد الجناح - السرب)
                ViewBag.Get_Station_Code = STATION_CODE.ToString();
                FORCE_ID = check_unit.FORCE_ID.Value;

            }
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();
            //sections
            ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "SECTION_NAME", "SECTION_NAME");

            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //get permits types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 2 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get companies list for specific station in english 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E");
                //get autho person who is authorized to process this kind of permit 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                //get genders inenglish 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //show error if ther is no autho person setting for this type of permit 
                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                }
            }
            else
            {
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get zones and gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 2 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get companies list for specific station in arabic 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME");
                //get autho person who is authorized to process this kind of permit in arbic 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //show error if ther is no autho person setting for this type of permit 
                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                }
            }

            return View();
		}

		// POST data of the request 
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Create(RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES,string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE
            , int IDENTITY_CODE,int GENDER, int PASS_TYPE_CODE, string WORK_CARD_NUMBER,string ID_CARD_NUMBER
            ,string NAME_A,string NAME_E, string PROFESSION_A, string PROFESSION_E, string WORK_PLACE, string GSM, string ADDRESS, DateTime CARD_EXPIRED_DATE, DateTime DATE_FROM, DateTime DATE_TO)
		{
            ViewBag.activetab = "Create";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            //int unit = 0;
            //check if session not null 
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //this below code is to check is the request in HQ or other stations, to view (الركن المختص أو قائد الجناح - السرب)
                ViewBag.Get_Station_Code = id.ToString();
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

                    FORCE_ID = check_unit.FORCE_ID.Value;

                }
                int station = int.Parse(id.ToString());
                STATION_CODE = station;
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


                //this below code is to check is the request in HQ or other stations, to view (الركن المختص أو قائد الجناح - السرب)
                ViewBag.Get_Station_Code = STATION_CODE.ToString();
                FORCE_ID = check_unit.FORCE_ID.Value;

            }
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();
            //sections
            ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "SECTION_NAME", "SECTION_NAME");

            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //get permits types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get autho person who is authorized to process this kind of permit in english 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 2 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get companies list for specific station in english 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_COMPANY_PASS_DTL.COMPANY_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
            }
            else
            {
                //get idnetities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types in arabic (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get autho person who is authorized to process this kind of permit in arbic 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE);
                //get zones and gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 2 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get companies list for specific station in arabic 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_COMPANY_PASS_DTL.COMPANY_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");

            }
            
            if (ModelState.IsValid)
            {

                rA42_COMPANY_PASS_DTL.SERVICE_NUMBER = rA42_COMPANY_PASS_DTL.SERVICE_NUMBER;
                rA42_COMPANY_PASS_DTL.RESPONSIBLE = rA42_COMPANY_PASS_DTL.RESPONSIBLE;
                rA42_COMPANY_PASS_DTL.COMPANY_CODE = rA42_COMPANY_PASS_DTL.COMPANY_CODE;
                rA42_COMPANY_PASS_DTL.COMPANY_TYPE_CODE = COMPANY_TYPE_CODE;
                rA42_COMPANY_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_COMPANY_PASS_DTL.CARD_FOR_CODE = 2;
                //rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE;
                //get user military information from api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;
                //this section is for applicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {

                    rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE =rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_COMPANY_PASS_DTL.REJECTED = false;
                    rA42_COMPANY_PASS_DTL.STATUS = false;
                    rA42_COMPANY_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect this request to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_COMPANY_PASS_DTL);

                    }
                    else
                    {
                        rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_COMPANY_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_COMPANY_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                    rA42_COMPANY_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                    rA42_COMPANY_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_COMPANY_PASS_DTL.REJECTED = false;
                    rA42_COMPANY_PASS_DTL.STATUS = false;
                    rA42_COMPANY_PASS_DTL.ISOPENED = false;
                }
                //this section is for permits cell
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //permits cell should redirect the request for the security officer 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_COMPANY_PASS_DTL);

                    }
                    else
                    {
                        rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_COMPANY_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_COMPANY_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                    rA42_COMPANY_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                    rA42_COMPANY_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                    rA42_COMPANY_PASS_DTL.REJECTED = false;
                    rA42_COMPANY_PASS_DTL.STATUS = false;
                    rA42_COMPANY_PASS_DTL.ISOPENED = true;
                }
                //this section is for security officer 
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //afetr he create and complete the request, the request will redirected for the permit cell for printing 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_COMPANY_PASS_DTL);

                    }
                    else
                    {
                        rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_COMPANY_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_COMPANY_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_COMPANY_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    rA42_COMPANY_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;

                    rA42_COMPANY_PASS_DTL.REJECTED = false;
                    rA42_COMPANY_PASS_DTL.STATUS = true;
                    rA42_COMPANY_PASS_DTL.ISOPENED = true;
                }
                rA42_COMPANY_PASS_DTL.CRD_BY = currentUser;
                rA42_COMPANY_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_COMPANY_PASS_DTL.UPD_BY = currentUser;
                rA42_COMPANY_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_COMPANY_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_COMPANY_PASS_DTL.REJECTED = false;
                rA42_COMPANY_PASS_DTL.BARCODE = rA42_COMPANY_PASS_DTL.BARCODE;
                db.RA42_COMPANY_PASS_DTL.Add(rA42_COMPANY_PASS_DTL);
                
                try
                {
                    
                    db.SaveChanges();
                   
                }
                catch (DbEntityValidationException cc)
                {
                    foreach (var validationerrors in cc.EntityValidationErrors)
                    {
                        foreach (var validationerror in validationerrors.ValidationErrors)
                        {
                            TempData["Erorr"] = validationerror.PropertyName + " + " + validationerror.ErrorMessage;
                            return RedirectToAction("Create");
                        }
                    }
                 
                }
                //add selected zones and gates 
                RA42_ZONE_MASTER_MST rA42_ZONE_MASTER_MST = new RA42_ZONE_MASTER_MST();
                if (ZONE != null && !ZONE.Contains(0))
                {
                    for (int i = 0; i < ZONE.Length; i++)
                    {

                        try
                        {
                            if (SUB_ZONE[i] == 0)
                            {
                                //rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = null;
                            }

                            if (SUB_ZONE[i].Equals(null))
                            {
                                //rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = null;
                            }
                            else
                            {
                                rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = SUB_ZONE[i];
                            }


                        }
                        catch (System.IndexOutOfRangeException ex)
                        {
                            //rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = null;
                        }
                        rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE;
                        rA42_ZONE_MASTER_MST.ZONE_CODE = ZONE[i];
                        rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                        rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                        db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                        db.SaveChanges();
                        //continue;
                    }

                }
                //add selected documenst
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

                                    fileName = "FileNO" + c + "_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);
                                    //add file name to the db table 
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE,
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
                                    //delete all uploaded files if there is currupted file, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                    return RedirectToAction("Create");
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
                //deal only with one worker 
                RA42_TEMPRORY_COMPANY_PASS_DTL rA42_TEMPRORY_COMPANY_PASS_DTL = new RA42_TEMPRORY_COMPANY_PASS_DTL();
                string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                rA42_TEMPRORY_COMPANY_PASS_DTL.COMPANY_PASS_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE;
                rA42_TEMPRORY_COMPANY_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_TEMPRORY_COMPANY_PASS_DTL.IDENTITY_CODE = IDENTITY_CODE;
                rA42_TEMPRORY_COMPANY_PASS_DTL.GENDER_ID = GENDER;
                rA42_TEMPRORY_COMPANY_PASS_DTL.PASS_TYPE_CODE = PASS_TYPE_CODE;
                rA42_TEMPRORY_COMPANY_PASS_DTL.WORK_CARD_NUMBER = WORK_CARD_NUMBER;
                rA42_TEMPRORY_COMPANY_PASS_DTL.ID_CARD_NUMBER = ID_CARD_NUMBER;
                rA42_TEMPRORY_COMPANY_PASS_DTL.CARD_EXPIRED_DATE = CARD_EXPIRED_DATE;
                rA42_TEMPRORY_COMPANY_PASS_DTL.NAME_A = NAME_A;
                rA42_TEMPRORY_COMPANY_PASS_DTL.NAME_E = NAME_E;
                rA42_TEMPRORY_COMPANY_PASS_DTL.PROFESSION_A = PROFESSION_A;
                rA42_TEMPRORY_COMPANY_PASS_DTL.PROFESSION_E = PROFESSION_E;
                rA42_TEMPRORY_COMPANY_PASS_DTL.WORK_PLACE = WORK_PLACE;
                rA42_TEMPRORY_COMPANY_PASS_DTL.BARCODE = barcode;
                rA42_TEMPRORY_COMPANY_PASS_DTL.GSM = GSM;
                rA42_TEMPRORY_COMPANY_PASS_DTL.ADDRESS = ADDRESS;
                rA42_TEMPRORY_COMPANY_PASS_DTL.DATE_FROM = DATE_FROM;
                rA42_TEMPRORY_COMPANY_PASS_DTL.DATE_TO = DATE_TO;
                if (PERSONAL_IMAGE != null)
                {
                    try
                    {

                        // Verify that the user selected a file
                        if (PERSONAL_IMAGE.ContentLength > 0)
                        {
                            // extract only the filename with extention
                            string fileName = Path.GetFileNameWithoutExtension(PERSONAL_IMAGE.FileName);
                            string extension = Path.GetExtension(PERSONAL_IMAGE.FileName);


                            //check extention of the image file 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {



                                fileName = "Profile_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                                   GetResourcesValue("error_update_message"),
                                   "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_COMPANY_PASS_DTL);
                                }

                                rA42_TEMPRORY_COMPANY_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error message if user uploaded unsupported format 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_COMPANY_PASS_DTL);
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                rA42_TEMPRORY_COMPANY_PASS_DTL.CRD_BY = currentUser;
                rA42_TEMPRORY_COMPANY_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_TEMPRORY_COMPANY_PASS_DTL.UPD_BY = currentUser;
                rA42_TEMPRORY_COMPANY_PASS_DTL.UPD_DT = DateTime.Now;
                db.RA42_TEMPRORY_COMPANY_PASS_DTL.Add(rA42_TEMPRORY_COMPANY_PASS_DTL);
                db.SaveChanges();
            

        
      



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
            return View(rA42_COMPANY_PASS_DTL);
        }


        // this view is for authorized user of the system such as autho person, applicant, security officer
        public ActionResult Supercreate()
        {
            ViewBag.activetab = "Supercreate";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            //check if session not null 
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //this below code is to check is the request in HQ or other stations, to view (الركن المختص أو قائد الجناح - السرب)
                ViewBag.Get_Station_Code = id.ToString();
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
                    FORCE_ID = check_unit.FORCE_ID.Value;


                }
                int station = int.Parse(id.ToString());
                STATION_CODE = station;
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


                //this below code is to check is the request in HQ or other stations, to view (الركن المختص أو قائد الجناح - السرب)
                ViewBag.Get_Station_Code = STATION_CODE.ToString();
                FORCE_ID = check_unit.FORCE_ID.Value;

            }
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();
            //sections
            ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "SECTION_NAME", "SECTION_NAME");

            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //get permits types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 2 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get companies list for specific station in english 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E");
                //get genders inenglish 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");

                //get autho person (المنسق الأمني) who is responsible about this kind of permit 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //if the current user workflow id type is applicant or normal user show error 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                    }
                }


            }
            else
            {
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get zones and gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 2 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get companies list for specific station in arabic 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get autho person (المنسق الأمني) who is responsible about this kind of permit 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //if the current user workflow id type is applicant or normal user show error 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                    }
                }
            }

            return View();
        }

        // POST new data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Supercreate(RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES,string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase[] PERSONAL_IMAGE
            , int[] IDENTITY_CODE, int[] GENDER, int[] PASS_TYPE_CODE, string[] WORK_CARD_NUMBER, string[] ID_CARD_NUMBER
            , string[] NAME_A, string[] NAME_E, string[] PROFESSION_A, string[] PROFESSION_E, string[] WORK_PLACE, string[] GSM, string[] ADDRESS, DateTime[] CARD_EXPIRED_DATE, DateTime[] DATE_FROM, DateTime[] DATE_TO)
        {
           
            ViewBag.activetab = "Supercreate";
            var url = Url.RequestContext.RouteData.Values["id"];
            //check if session not null 
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //this below code is to check is the request in HQ or other stations, to view (الركن المختص أو قائد الجناح - السرب)
                ViewBag.Get_Station_Code = id.ToString();
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

                    FORCE_ID = check_unit.FORCE_ID.Value;

                }
                int station = int.Parse(id.ToString());
                STATION_CODE = station;
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


                //this below code is to check is the request in HQ or other stations, to view (الركن المختص أو قائد الجناح - السرب)
                ViewBag.Get_Station_Code = STATION_CODE.ToString();
                FORCE_ID = check_unit.FORCE_ID.Value;

            }
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();
            //sections
            ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "SECTION_NAME", "SECTION_NAME");

            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //get permits types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 2 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get companies list for specific station in english 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_COMPANY_PASS_DTL.COMPANY_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
            }
            else
            {
                //get idnetities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get zones and gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 2 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get companies list for specific station in arabic  
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_COMPANY_PASS_DTL.COMPANY_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");

            }


            if (ModelState.IsValid)
            {
                
                rA42_COMPANY_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_COMPANY_PASS_DTL.CARD_FOR_CODE = 2;
                rA42_COMPANY_PASS_DTL.SERVICE_NUMBER = rA42_COMPANY_PASS_DTL.SERVICE_NUMBER;
                rA42_COMPANY_PASS_DTL.RESPONSIBLE = rA42_COMPANY_PASS_DTL.RESPONSIBLE;
                rA42_COMPANY_PASS_DTL.COMPANY_CODE = rA42_COMPANY_PASS_DTL.COMPANY_CODE;
                rA42_COMPANY_PASS_DTL.COMPANY_TYPE_CODE = COMPANY_TYPE_CODE;

                //get user military information from api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;
                
                //this section is for applicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {

                    rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_COMPANY_PASS_DTL.REJECTED = false;
                    rA42_COMPANY_PASS_DTL.STATUS = false;
                    rA42_COMPANY_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect this request to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_COMPANY_PASS_DTL);

                    }
                    else
                    {
                        rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_COMPANY_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_COMPANY_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                    rA42_COMPANY_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                    rA42_COMPANY_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_COMPANY_PASS_DTL.REJECTED = false;
                    rA42_COMPANY_PASS_DTL.STATUS = false;
                    rA42_COMPANY_PASS_DTL.ISOPENED = false;
                }
                //this section is for permits cell
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //permits cell should redirect the request for the security officer 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_COMPANY_PASS_DTL);

                    }
                    else
                    {
                        rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    
                    rA42_COMPANY_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_COMPANY_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                    rA42_COMPANY_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                    rA42_COMPANY_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                    rA42_COMPANY_PASS_DTL.REJECTED = false;
                    rA42_COMPANY_PASS_DTL.STATUS = false;
                    rA42_COMPANY_PASS_DTL.ISOPENED = true;
                }
                //this section is for security officer 
                if(WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //afetr he create and complete the request, the request will redirected for the permit cell for printing 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_COMPANY_PASS_DTL);

                    }
                    else
                    {
                        rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_COMPANY_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_COMPANY_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_COMPANY_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    rA42_COMPANY_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;

                    rA42_COMPANY_PASS_DTL.REJECTED = false;
                    rA42_COMPANY_PASS_DTL.STATUS = true;
                    rA42_COMPANY_PASS_DTL.ISOPENED = true;
                }
                rA42_COMPANY_PASS_DTL.CRD_BY = currentUser;
                rA42_COMPANY_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_COMPANY_PASS_DTL.UPD_BY = currentUser;
                rA42_COMPANY_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_COMPANY_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_COMPANY_PASS_DTL.BARCODE = rA42_COMPANY_PASS_DTL.BARCODE;
                db.RA42_COMPANY_PASS_DTL.Add(rA42_COMPANY_PASS_DTL);

                try
                {

                    db.SaveChanges();

                }
                catch (DbEntityValidationException cc)
                {
                    foreach (var validationerrors in cc.EntityValidationErrors)
                    {
                        foreach (var validationerror in validationerrors.ValidationErrors)
                        {
                            TempData["Erorr"] = validationerror.PropertyName + " + " + validationerror.ErrorMessage;
                            return RedirectToAction("Supercreate");
                           
                        }
                    }
                   
                }
                //add selected zones and gates 
                RA42_ZONE_MASTER_MST rA42_ZONE_MASTER_MST = new RA42_ZONE_MASTER_MST();
                if (ZONE != null && !ZONE.Contains(0))
                {
                    for (int i = 0; i < ZONE.Length; i++)
                    {

                        try
                        {
                            if (SUB_ZONE[i] == 0)
                            {
                                //rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = null;
                            }

                            if (SUB_ZONE[i].Equals(null))
                            {
                                //rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = null;
                            }
                            else
                            {
                                rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = SUB_ZONE[i];
                            }


                        }
                        catch (System.IndexOutOfRangeException ex)
                        {
                            //rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = null;
                        }
                        rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE;
                        rA42_ZONE_MASTER_MST.ZONE_CODE = ZONE[i];
                        rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                        rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                        db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                        db.SaveChanges();
                        //continue;
                    }

                }
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


                                //check the file extention 
                                if (general.CheckFileType(file.FileName))
                                {

                                    fileName = "FileNO" + c + "_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);
                                    //add file name to db table 
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE,
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
                                    //delete all uploaded files for this request if ther is some wrong with one file, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                    return RedirectToAction("Supercreate");
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
                //check if user create one employee or more by checking IDENTITY_CODE length 
                if (IDENTITY_CODE != null)
                {
                    RA42_TEMPRORY_COMPANY_PASS_DTL rA42_TEMPRORY_COMPANY_PASS_DTL = new RA42_TEMPRORY_COMPANY_PASS_DTL();
                    
                    for (int j = 0; j < IDENTITY_CODE.Length; j++)
                    {
                        //create barcode for every employee
                        string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                        rA42_TEMPRORY_COMPANY_PASS_DTL.COMPANY_PASS_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE;
                        rA42_TEMPRORY_COMPANY_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        rA42_TEMPRORY_COMPANY_PASS_DTL.IDENTITY_CODE = IDENTITY_CODE[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.GENDER_ID = GENDER[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.PASS_TYPE_CODE = PASS_TYPE_CODE[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.WORK_CARD_NUMBER = WORK_CARD_NUMBER[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.ID_CARD_NUMBER = ID_CARD_NUMBER[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.CARD_EXPIRED_DATE = CARD_EXPIRED_DATE[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.NAME_A = NAME_A[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.NAME_E = NAME_E[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.PROFESSION_A = PROFESSION_A[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.PROFESSION_E = PROFESSION_E[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.WORK_PLACE = WORK_PLACE[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.BARCODE = barcode;
                        rA42_TEMPRORY_COMPANY_PASS_DTL.GSM = GSM[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.ADDRESS = ADDRESS[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.DATE_FROM = DATE_FROM[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.DATE_TO = DATE_TO[j];
                        if (PERSONAL_IMAGE != null)
                        {
                            try
                            {

                                // Verify that the user selected a file
                                if (PERSONAL_IMAGE[j].ContentLength > 0)
                                {
                                    // extract only the filename with extention
                                    string fileName = Path.GetFileNameWithoutExtension(PERSONAL_IMAGE[j].FileName);
                                    string extension = Path.GetExtension(PERSONAL_IMAGE[j].FileName);


                                    //check the extention of the image file 
                                    if (general.CheckPersonalImage(PERSONAL_IMAGE[j].FileName))
                                    {

                                        fileName = "Profile_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                                        // store the file inside ~/Files/Profiles/ folder
                                        bool check = general.ResizeImage(PERSONAL_IMAGE[j], fileName);

                                        if (check != true)
                                        {
                                            AddToast(new Toast("",
                                           GetResourcesValue("error_update_message"),
                                           "red"));
                                            TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                            return View(rA42_COMPANY_PASS_DTL);
                                        }

                                        rA42_TEMPRORY_COMPANY_PASS_DTL.PERSONAL_IMAGE = fileName;


                                    }
                                    else
                                    {
                                        //if format not supported, show error message 
                                        AddToast(new Toast("",
                                        GetResourcesValue("error_update_message"),
                                        "red"));
                                        TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                        return View(rA42_COMPANY_PASS_DTL);
                                    }
                                }
                            }

                            catch (Exception ex)
                            {
                                ex.GetBaseException();
                            }
                        }
                        rA42_TEMPRORY_COMPANY_PASS_DTL.CRD_BY = currentUser;
                        rA42_TEMPRORY_COMPANY_PASS_DTL.CRD_DT = DateTime.Now;
                        rA42_TEMPRORY_COMPANY_PASS_DTL.UPD_BY = currentUser;
                        rA42_TEMPRORY_COMPANY_PASS_DTL.UPD_DT = DateTime.Now;
                        db.RA42_TEMPRORY_COMPANY_PASS_DTL.Add(rA42_TEMPRORY_COMPANY_PASS_DTL);
                        db.SaveChanges();
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
            return View(rA42_COMPANY_PASS_DTL);
        }
        // get specific employee details 
        public ActionResult DetailsTemproryPass(int? id)
        {
            ViewBag.activetab = "DetailsTemproryPass";

            if (id == null)
            {
                return NotFound();
            }
            RA42_TEMPRORY_COMPANY_PASS_DTL rA42_TEMPRORY_COMPANY_PASS_DTL = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Find(id);
            if (rA42_TEMPRORY_COMPANY_PASS_DTL == null)
            {
                return NotFound();
            }
            
            return View(rA42_TEMPRORY_COMPANY_PASS_DTL);
        }
        // edit specific employee details 
        public ActionResult EditTemproryPass(int? id)
        {
            ViewBag.activetab = "EditTemproryPass";

            if (id == null)
            {
                return NotFound();
            }
            //check if permit in the table 
            RA42_TEMPRORY_COMPANY_PASS_DTL rA42_TEMPRORY_COMPANY_PASS_DTL = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Find(id);
            if (rA42_TEMPRORY_COMPANY_PASS_DTL == null)
            {
                return NotFound();
            }



            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_TEMPRORY_COMPANY_PASS_DTL.IDENTITY_CODE);
                //get permits types in englsih 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_TEMPRORY_COMPANY_PASS_DTL.PASS_TYPE_CODE);
                if (rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_TEMPRORY_COMPANY_PASS_DTL.GENDER_ID);
                //get zones and gates in en 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in en 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

            }
            else
            {
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_TEMPRORY_COMPANY_PASS_DTL.IDENTITY_CODE);
                //get permits type in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_TEMPRORY_COMPANY_PASS_DTL.PASS_TYPE_CODE);
                if (rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get genders in arabic
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_TEMPRORY_COMPANY_PASS_DTL.GENDER_ID);
                //get zones and gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

            }

            //get selected zones and gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get selected files 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get documnts types for this kind of permit to check missing files with this request 
            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true ).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                  select new
                                  {
                                      FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                      FILE_TYPE = c.FILE_TYPE,
                                      FILE_TYPE_E = c.FILE_TYPE_E,

                                  }).Count();

            ViewBag.PASS_FILES_2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                    join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                    join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                    join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();


            return View(rA42_TEMPRORY_COMPANY_PASS_DTL);
        }

      
        //post new employee data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditTemproryPass(RA42_TEMPRORY_COMPANY_PASS_DTL rA42_TEMPRORY_COMPANY_PASS_DTL, HttpPostedFileBase PERSONAL_IMAGE, int[] ZONE, int[] SUB_ZONE,
            int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files)
        {
            ViewBag.activetab = "EditTemproryPass";
            var v = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.TEMPRORY_COMPANY_PASS_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.TEMPRORY_COMPANY_PASS_CODE).FirstOrDefault();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == v.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == v.RA42_COMPANY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                  select new
                                  {
                                      FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                      FILE_TYPE = c.FILE_TYPE,
                                      FILE_TYPE_E = c.FILE_TYPE_E,

                                  }).Count();

            ViewBag.PASS_FILES_2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                    join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                    join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                    join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == v.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == v.RA42_COMPANY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            //get selected zones and gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == v.COMPANY_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get selected files 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == v.COMPANY_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();

            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_TEMPRORY_COMPANY_PASS_DTL.IDENTITY_CODE);
                //get permits types in englsih 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_TEMPRORY_COMPANY_PASS_DTL.PASS_TYPE_CODE);
                if (v.RA42_COMPANY_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_TEMPRORY_COMPANY_PASS_DTL.GENDER_ID);
                //get zones and gates in en 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == v.RA42_COMPANY_PASS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in en 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == v.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == v.RA42_COMPANY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
            }
            else
            {
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_TEMPRORY_COMPANY_PASS_DTL.IDENTITY_CODE);
                //get permits type in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_TEMPRORY_COMPANY_PASS_DTL.PASS_TYPE_CODE);
                if (v.RA42_COMPANY_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get genders in arabic
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_TEMPRORY_COMPANY_PASS_DTL.GENDER_ID);
                //get zones and gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == v.RA42_COMPANY_PASS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == v.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == v.RA42_COMPANY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
            }

            if (ModelState.IsValid)
            {
                if (v != null)
                {
                    v.COMPANY_PASS_CODE = rA42_TEMPRORY_COMPANY_PASS_DTL.COMPANY_PASS_CODE;
                    v.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                    v.ID_CARD_NUMBER = rA42_TEMPRORY_COMPANY_PASS_DTL.ID_CARD_NUMBER;
                    v.WORK_CARD_NUMBER = rA42_TEMPRORY_COMPANY_PASS_DTL.WORK_CARD_NUMBER;
                    v.CARD_EXPIRED_DATE = rA42_TEMPRORY_COMPANY_PASS_DTL.CARD_EXPIRED_DATE;
                    v.NAME_A = rA42_TEMPRORY_COMPANY_PASS_DTL.NAME_A;
                    v.NAME_E = rA42_TEMPRORY_COMPANY_PASS_DTL.NAME_E;
                    v.PROFESSION_A = rA42_TEMPRORY_COMPANY_PASS_DTL.PROFESSION_A;
                    v.PROFESSION_E = rA42_TEMPRORY_COMPANY_PASS_DTL.PROFESSION_E;
                    v.WORK_PLACE = rA42_TEMPRORY_COMPANY_PASS_DTL.WORK_PLACE;
                    v.BARCODE = v.BARCODE;
                    v.IDENTITY_CODE = rA42_TEMPRORY_COMPANY_PASS_DTL.IDENTITY_CODE;
                    v.GENDER_ID = rA42_TEMPRORY_COMPANY_PASS_DTL.GENDER_ID;
                    v.ADDRESS = rA42_TEMPRORY_COMPANY_PASS_DTL.ADDRESS;
                    v.GSM = rA42_TEMPRORY_COMPANY_PASS_DTL.GSM;
                    v.PASS_TYPE_CODE = rA42_TEMPRORY_COMPANY_PASS_DTL.PASS_TYPE_CODE;
                    v.DATE_FROM = rA42_TEMPRORY_COMPANY_PASS_DTL.DATE_FROM;
                    v.DATE_TO = rA42_TEMPRORY_COMPANY_PASS_DTL.DATE_TO;

                    //check if the user upload new image 
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

                                   

                                    fileName = "Profile_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                                    // store the file inside ~/Files/Profiles/ folder
                                    bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                    if (check != true)
                                    {
                                        AddToast(new Toast("",
                                       GetResourcesValue("error_update_message"),
                                       "red"));
                                        TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                        return View(rA42_TEMPRORY_COMPANY_PASS_DTL);
                                    }

                                    v.PERSONAL_IMAGE = fileName;


                                }
                                else
                                {
                                    AddToast(new Toast("",
                                    GetResourcesValue("error_update_message"),
                                    "red"));
                                    TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                    return View(rA42_TEMPRORY_COMPANY_PASS_DTL);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ex.GetBaseException();
                        }
                    }
                    v.CRD_BY = v.CRD_BY;
                    v.CRD_DT = v.CRD_DT;
                    v.UPD_BY = currentUser;
                    v.UPD_DT = DateTime.Now;
                    v.ISPRINTED = false;
                    db.Entry(v).State = EntityState.Modified;
                    db.SaveChanges();
                    var cr = db.RA42_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == v.COMPANY_PASS_CODE).FirstOrDefault();
                    if (cr != null)
                    {
                        cr.ISPRINTED = false;
                        cr.UPD_BY = currentUser;
                        cr.UPD_DT = DateTime.Now;
                        db.SaveChanges();
                    }

                    //add zones and files before editing any thing 
                    RA42_ZONE_MASTER_MST rA42_ZONE_MASTER_MST = new RA42_ZONE_MASTER_MST();
                    if (ZONE != null && !ZONE.Contains(0))
                    {
                        for (int i = 0; i < ZONE.Length; i++)
                        {

                            try
                            {
                                if (SUB_ZONE[i] == 0)
                                {
                                    //rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = null;
                                }

                                if (SUB_ZONE[i].Equals(null))
                                {
                                    //rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = null;
                                }
                                else
                                {
                                    rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = SUB_ZONE[i];
                                }


                            }
                            catch (System.IndexOutOfRangeException ex)
                            {
                                //rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = null;
                            }
                            rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                            rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = v.COMPANY_PASS_CODE;
                            rA42_ZONE_MASTER_MST.ZONE_CODE = ZONE[i];
                            rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                            rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                            db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                            db.SaveChanges();
                            //continue;
                        }

                    }

                    //add new documents 
                    if (files != null)
                    {

                        try
                        {
                            //create foreach loop for uploading multiple files 
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
                                        //add new file to db table 
                                        RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                        {
                                            ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                            ACCESS_ROW_CODE = v.COMPANY_PASS_CODE,
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
                                        //delete all uploaded files if there is somthing wrong with one document, this is security proceduers
                                        var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == v.COMPANY_PASS_CODE).ToList();
                                        foreach (var del in delete)
                                        {
                                            string filpath = "~/Documents/" + del.FILE_NAME;
                                            general.RemoveFileFromServer(filpath);
                                            db.RA42_FILES_MST.Remove(del);
                                            db.SaveChanges();
                                        }
                                        TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                        return View(rA42_TEMPRORY_COMPANY_PASS_DTL);
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
                                GetResourcesValue("success_update_message"),
                                "green"));
                    return RedirectToAction("EditTemproryPass", new { id = rA42_TEMPRORY_COMPANY_PASS_DTL.TEMPRORY_COMPANY_PASS_CODE });
                }
            }

            return View(rA42_TEMPRORY_COMPANY_PASS_DTL);
        }

        //renew permit
        public ActionResult Renew(int? id)
        {
            ViewBag.activetab = "renew";

            if (id == null)
            {
                return NotFound();
            }
            //check if permit in the table 
            RA42_TEMPRORY_COMPANY_PASS_DTL rA42_TEMPRORY_COMPANY_PASS_DTL = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Find(id);
            if (rA42_TEMPRORY_COMPANY_PASS_DTL == null)
            {
                return NotFound();
            }
            if (rA42_TEMPRORY_COMPANY_PASS_DTL.DATE_TO != null)
            {
                string date = rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.CheckDate(rA42_TEMPRORY_COMPANY_PASS_DTL.DATE_TO.Value);
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



            if (Language.GetCurrentLang() == "en")
            {
                //get companies list in english 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.STATION_CODE && a.COMPANY_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.COMPANY_CODE), "COMPANY_CODE", "COMPANY_NAME_E");

                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_TEMPRORY_COMPANY_PASS_DTL.IDENTITY_CODE);
                //get permits types in englsih 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_TEMPRORY_COMPANY_PASS_DTL.PASS_TYPE_CODE);
                if (rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get zones and gates in en 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in en 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get autho person (المنسق الأمني) in case applicant want to change the autho person in english
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                    }

                }

               
            }
            else
            {
                //get companies list in arabic 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.STATION_CODE && a.COMPANY_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.COMPANY_CODE), "COMPANY_CODE", "COMPANY_NAME");
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_TEMPRORY_COMPANY_PASS_DTL.IDENTITY_CODE);
                //get permits type in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_TEMPRORY_COMPANY_PASS_DTL.PASS_TYPE_CODE);
                if (rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get zones and gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get genders in arabic
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get autho person (المنسق الأمني) in case applicant want to change the autho person in arabic 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return View();
                    }
                }
                
            }
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get selected zones and gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get selected files 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get documnts types for this kind of permit to check missing files with this request 
            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                  select new
                                  {
                                      FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                      FILE_TYPE = c.FILE_TYPE,
                                      FILE_TYPE_E = c.FILE_TYPE_E,

                                  }).Count();

            ViewBag.PASS_FILES_2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                    join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                    join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                    join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.RA42_COMPANY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            rA42_TEMPRORY_COMPANY_PASS_DTL.DATE_FROM = null;
            rA42_TEMPRORY_COMPANY_PASS_DTL.DATE_TO = null;

            return View(rA42_TEMPRORY_COMPANY_PASS_DTL);
        }

        // POST new edited data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Renew(RA42_TEMPRORY_COMPANY_PASS_DTL rA42_TEMPRORY_COMPANY_PASS_DTL,
           
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE
            ,int COMPANY_PASS_ID,int TEMPRORY_ID,string REMARKS, string PURPOSE_OF_PASS,int WORKFLOW_RESPO_CODE)
        {
            ViewBag.activetab = "renew";
            var general_data = db.RA42_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == COMPANY_PASS_ID).FirstOrDefault();
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (general_data.STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get selected zones and gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == COMPANY_PASS_ID && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get selected files
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == COMPANY_PASS_ID && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();

            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == general_data.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == general_data.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                  select new
                                  {
                                      FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                      FILE_TYPE = c.FILE_TYPE,
                                      FILE_TYPE_E = c.FILE_TYPE_E,

                                  }).Count();

            ViewBag.PASS_FILES_2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                    join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                    join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                    join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == general_data.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == general_data.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                //get companies list in english 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == general_data.STATION_CODE && a.COMPANY_CODE == general_data.COMPANY_CODE), "COMPANY_CODE", "COMPANY_NAME_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E",rA42_TEMPRORY_COMPANY_PASS_DTL.IDENTITY_CODE);
                //get permits types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E",rA42_TEMPRORY_COMPANY_PASS_DTL.PASS_TYPE_CODE);

                if (general_data.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }//get zones and gates in en 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in en 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == general_data.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == general_data.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                  //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E",rA42_TEMPRORY_COMPANY_PASS_DTL.GENDER_ID);
                //get autho person (المنسق الأمني) in case applicant want to change the autho person in english
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(general_data);
                    }
                }

                
            }
            else
            {
                //get companies list in arabic 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == general_data.STATION_CODE && a.COMPANY_CODE ==general_data.COMPANY_CODE), "COMPANY_CODE", "COMPANY_NAME");
                //get identities in arabic  
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A",rA42_TEMPRORY_COMPANY_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic  
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE",rA42_TEMPRORY_COMPANY_PASS_DTL.PASS_TYPE_CODE);
                if (general_data.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get zones and gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == general_data.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == general_data.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A",rA42_TEMPRORY_COMPANY_PASS_DTL.GENDER_ID);
                //get autho person (المنسق الأمني) in case applicant want to change the autho person in arabic
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(general_data);
                    }

                }

               
            }

            if (ModelState.IsValid)
            {
                RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL = new RA42_COMPANY_PASS_DTL();
                rA42_COMPANY_PASS_DTL.STATION_CODE = general_data.STATION_CODE;
                rA42_COMPANY_PASS_DTL.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                rA42_COMPANY_PASS_DTL.SERVICE_NUMBER = currentUser;
                rA42_COMPANY_PASS_DTL.RESPONSIBLE = general_data.RESPONSIBLE;
                rA42_COMPANY_PASS_DTL.COMPANY_CODE = general_data.COMPANY_CODE;
                rA42_COMPANY_PASS_DTL.COMPANY_TYPE_CODE = COMPANY_TYPE_CODE;
                rA42_COMPANY_PASS_DTL.REMARKS = REMARKS;
                rA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS = PURPOSE_OF_PASS;

                //get user military information from api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;
                //this section is for applicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {

                    rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = WORKFLOW_RESPO_CODE;
                    rA42_COMPANY_PASS_DTL.REJECTED = false;
                    rA42_COMPANY_PASS_DTL.STATUS = false;
                    rA42_COMPANY_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person 
                if (WORKFLOWID == 2)
                {
                    //he should redirect this request to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(general_data);

                    }
                    else
                    {
                        rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_COMPANY_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_COMPANY_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                    rA42_COMPANY_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                    rA42_COMPANY_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_COMPANY_PASS_DTL.REJECTED = false;
                    rA42_COMPANY_PASS_DTL.STATUS = false;
                    rA42_COMPANY_PASS_DTL.ISOPENED = false;
                }
                //this section is for permits cell
                if (WORKFLOWID == 3)
                {
                    //permits cell should redirect the request for the security officer 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(general_data);

                    }
                    else
                    {
                        rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_COMPANY_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_COMPANY_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                    rA42_COMPANY_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                    rA42_COMPANY_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                    rA42_COMPANY_PASS_DTL.REJECTED = false;
                    rA42_COMPANY_PASS_DTL.STATUS = false;
                    rA42_COMPANY_PASS_DTL.ISOPENED = true;
                }
                //this section is for security officer 
                if (WORKFLOWID == 4)
                {
                    //afetr he create and complete the request, the request will redirected for the permit cell for printing 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(general_data);

                    }
                    else
                    {
                        rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_COMPANY_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_COMPANY_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_COMPANY_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    rA42_COMPANY_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;

                    rA42_COMPANY_PASS_DTL.REJECTED = false;
                    rA42_COMPANY_PASS_DTL.STATUS = true;
                    rA42_COMPANY_PASS_DTL.ISOPENED = true;
                }
                rA42_COMPANY_PASS_DTL.CRD_BY = currentUser;
                rA42_COMPANY_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_COMPANY_PASS_DTL.UPD_BY = currentUser;
                rA42_COMPANY_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_COMPANY_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_COMPANY_PASS_DTL.BARCODE = rA42_COMPANY_PASS_DTL.BARCODE;
                db.RA42_COMPANY_PASS_DTL.Add(rA42_COMPANY_PASS_DTL);

                try
                {

                    db.SaveChanges();

                }
                catch (DbEntityValidationException cc)
                {
                    foreach (var validationerrors in cc.EntityValidationErrors)
                    {
                        foreach (var validationerror in validationerrors.ValidationErrors)
                        {
                            TempData["Erorr"] = validationerror.PropertyName + " + " + validationerror.ErrorMessage;
                            return RedirectToAction("Renew", new { id = TEMPRORY_ID });

                        }
                    }

                }

                //add zones and files before editing any thing 
                RA42_ZONE_MASTER_MST rA42_ZONE_MASTER_MST = new RA42_ZONE_MASTER_MST();
                if (ZONE != null && !ZONE.Contains(0))
                {
                    for (int i = 0; i < ZONE.Length; i++)
                    {

                        try
                        {
                            if (SUB_ZONE[i] == 0)
                            {
                                //rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = null;
                            }

                            if (SUB_ZONE[i].Equals(null))
                            {
                                //rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = null;
                            }
                            else
                            {
                                rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = SUB_ZONE[i];
                            }


                        }
                        catch (System.IndexOutOfRangeException ex)
                        {
                            //rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = null;
                        }
                        rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE;
                        rA42_ZONE_MASTER_MST.ZONE_CODE = ZONE[i];
                        rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                        rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                        db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                        db.SaveChanges();
                        //continue;
                    }

                }
                //add previues zones to new zone
                var zones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == COMPANY_PASS_ID && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
                foreach (var zone in zones)
                {
                    rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE;
                    rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                    rA42_ZONE_MASTER_MST.ZONE_CODE = zone.ZONE_CODE.Value;
                    rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                    rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                    rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = zone.ZONE_SUB_CODE;
                    db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                    db.SaveChanges();
                }
                //add new documents 
                if (files != null)
                {

                    try
                    {
                        //create foreach loop for uploading multiple files 
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
                                    //add new file to db table 
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE,
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
                                    //delete all uploaded files if there is somthing wrong with one document, this is security proceduers
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return RedirectToAction("Renew", new { id = TEMPRORY_ID });
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
                var selected_files = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == COMPANY_PASS_ID && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
                foreach (var file in selected_files)
                {
                    //add new file
                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                    {
                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                        ACCESS_ROW_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE,
                        FILE_TYPE = file.FILE_TYPE,
                        FILE_TYPE_TEXT = file.FILE_TYPE_TEXT,
                        FILE_NAME = file.FILE_NAME,
                        CRD_BY = currentUser,
                        CRD_DT = DateTime.Now


                    };
                    db.RA42_FILES_MST.Add(fILES_MST);
                    db.SaveChanges();
                }

                var temp = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.TEMPRORY_COMPANY_PASS_CODE == TEMPRORY_ID).FirstOrDefault();
                if (temp != null)
                {
                    //generate new barcode 
                    string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                    rA42_TEMPRORY_COMPANY_PASS_DTL.COMPANY_PASS_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.ACCESS_TYPE_CODE = temp.ACCESS_TYPE_CODE;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.ID_CARD_NUMBER = rA42_TEMPRORY_COMPANY_PASS_DTL.ID_CARD_NUMBER;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.WORK_CARD_NUMBER = rA42_TEMPRORY_COMPANY_PASS_DTL.WORK_CARD_NUMBER;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.CARD_EXPIRED_DATE = rA42_TEMPRORY_COMPANY_PASS_DTL.CARD_EXPIRED_DATE;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.NAME_A = rA42_TEMPRORY_COMPANY_PASS_DTL.NAME_A;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.NAME_E = rA42_TEMPRORY_COMPANY_PASS_DTL.NAME_E;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.PROFESSION_A = rA42_TEMPRORY_COMPANY_PASS_DTL.PROFESSION_A;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.PROFESSION_E = rA42_TEMPRORY_COMPANY_PASS_DTL.PROFESSION_E;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.WORK_PLACE = rA42_TEMPRORY_COMPANY_PASS_DTL.WORK_PLACE;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.BARCODE = barcode;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.IDENTITY_CODE = rA42_TEMPRORY_COMPANY_PASS_DTL.IDENTITY_CODE;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.GENDER_ID = rA42_TEMPRORY_COMPANY_PASS_DTL.GENDER_ID;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.ADDRESS = rA42_TEMPRORY_COMPANY_PASS_DTL.ADDRESS;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.GSM = rA42_TEMPRORY_COMPANY_PASS_DTL.GSM;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.PASS_TYPE_CODE = rA42_TEMPRORY_COMPANY_PASS_DTL.PASS_TYPE_CODE;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.DATE_FROM = rA42_TEMPRORY_COMPANY_PASS_DTL.DATE_FROM;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.DATE_TO = rA42_TEMPRORY_COMPANY_PASS_DTL.DATE_TO;

                    //check if the user upload new image 
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



                                    fileName = "Profile_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;

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

                                    rA42_TEMPRORY_COMPANY_PASS_DTL.PERSONAL_IMAGE = fileName;


                                }
                                else
                                {
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
                    if (rA42_TEMPRORY_COMPANY_PASS_DTL.PERSONAL_IMAGE != null)
                    {
                        rA42_TEMPRORY_COMPANY_PASS_DTL.PERSONAL_IMAGE = rA42_TEMPRORY_COMPANY_PASS_DTL.PERSONAL_IMAGE;
                    }
                    else
                    {
                        rA42_TEMPRORY_COMPANY_PASS_DTL.PERSONAL_IMAGE = temp.PERSONAL_IMAGE;
                    }
                    rA42_TEMPRORY_COMPANY_PASS_DTL.CRD_BY = currentUser;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.CRD_DT = DateTime.Now;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.UPD_BY = currentUser;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.UPD_DT = DateTime.Now;
                    rA42_TEMPRORY_COMPANY_PASS_DTL.ISPRINTED = false;
                    db.RA42_TEMPRORY_COMPANY_PASS_DTL.Add(rA42_TEMPRORY_COMPANY_PASS_DTL);
                    db.SaveChanges();

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
            }




            TempData["Erorr"] = "Somthing wrong happen,حدث خطأ ما";
            AddToast(new Toast("",
                GetResourcesValue("error_update_message"),
                "red"));
            return View(general_data);
        }

        // edit the main request 
        public ActionResult Edit(int? id)
		{
            ViewBag.activetab = "edit";

            if (id == null)
            {
                return NotFound();
            }
            //check if the request in the table 
            RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL = db.RA42_COMPANY_PASS_DTL.Find(id);
            if (rA42_COMPANY_PASS_DTL == null)
            {
                return NotFound();
            }
            //cheeck if current user has authority to edit 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_COMPANY_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_COMPANY_PASS_DTL.RESPONSIBLE != currentUser)
                {
                    if (rA42_COMPANY_PASS_DTL.ISOPENED != true)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER !=true)
                        {

                            return NotFound();

                        }
                    }
                }

                if (rA42_COMPANY_PASS_DTL.ISOPENED == true)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        if (rA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && rA42_COMPANY_PASS_DTL.REJECTED == true)
                        {

                        }
                        else
                        {
                            if (rA42_COMPANY_PASS_DTL.ISOPENED == true)
                            {
                                return NotFound();
                            }
                        }

                    }
                }
            }
            else
            {
                if (rA42_COMPANY_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_COMPANY_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != rA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
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
                //get companies in english 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == rA42_COMPANY_PASS_DTL.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_COMPANY_PASS_DTL.COMPANY_CODE);
                //get identities in english
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //get permits types in english
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                if (rA42_COMPANY_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get zones and gates in en 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in en 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_COMPANY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                 //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get autho person (المنسق الأمني) in case applicant want to change the autho person in english
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_COMPANY_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE);
                
                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                       
                    }

                }

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_COMPANY_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
            else {
                //get companies list in arabic
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == rA42_COMPANY_PASS_DTL.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_COMPANY_PASS_DTL.COMPANY_CODE);
                //get identities in arabic
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (rA42_COMPANY_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get zones and gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_COMPANY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get genders in arabic
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get autho person (المنسق الأمني) in case applicant want to change the autho person in arabic 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_COMPANY_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return View();
                    }
                }
                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_COMPANY_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
            //get selected zones and gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get selected files 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get employees info 
            ViewBag.GetEmployees = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == id && a.DLT_STS != true).ToList();
            //get documnts types for this kind of permit to check missing files with this request 
            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_COMPANY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                  select new
                                  {
                                      FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                      FILE_TYPE = c.FILE_TYPE,
                                      FILE_TYPE_E = c.FILE_TYPE_E,

                                  }).Count();

            ViewBag.PASS_FILES_2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                    join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                    join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                    join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_COMPANY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            //get comments of the request
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            //get request status 
            ViewBag.STATUS = rA42_COMPANY_PASS_DTL.STATUS;
            return View(rA42_COMPANY_PASS_DTL);
		}

		// POST new edited data
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL, string COMMENT,
            FormCollection form,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES,string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase[] PERSONAL_IMAGE
            , int[] IDENTITY_CODE, int[] GENDER, int[] PASS_TYPE_CODE, string[] WORK_CARD_NUMBER, string[] ID_CARD_NUMBER
            , string[] NAME_A, string[] NAME_E, string[] PROFESSION_A, string[] PROFESSION_E, string[] WORK_PLACE, string[] GSM, string[] ADDRESS, DateTime[] CARD_EXPIRED_DATE, DateTime[] DATE_FROM, DateTime[] DATE_TO)
		{
            ViewBag.activetab = "edit";
            var general_data = db.RA42_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE).FirstOrDefault();

            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == general_data.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == general_data.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                  select new
                                  {
                                      FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                      FILE_TYPE = c.FILE_TYPE,
                                      FILE_TYPE_E = c.FILE_TYPE_E,

                                  }).Count();

            ViewBag.PASS_FILES_2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                    join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                    join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                    join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == general_data.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == general_data.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                //get companies list in english 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == general_data.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_COMPANY_PASS_DTL.COMPANY_CODE);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //get permits types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                if (general_data.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get zones and gates in en 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in en 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == general_data.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == general_data.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get selected zones and gates 
                ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                //get selected files
                ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                //get employees of the request 
                ViewBag.GetEmployees = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS !=true).ToList();
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get autho person (المنسق الأمني) in case applicant want to change the autho person in english
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_COMPANY_PASS_DTL);
                    }
                }

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
            else
            {
                //get companies list in arabic 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == general_data.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_COMPANY_PASS_DTL.COMPANY_CODE);
                //get identities in arabic  
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types in arabic  
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (general_data.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get zones and gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == general_data.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == general_data.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get selected zones and gates 
                ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                //get employees for this request
                ViewBag.GetEmployees = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true).ToList();
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get autho person (المنسق الأمني) in case applicant want to change the autho person in arabic
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_COMPANY_PASS_DTL);
                    }

                }

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }

            //editing start from here
            if (ModelState.IsValid)
            {
                
                //add zones and files before editing any thing 
                RA42_ZONE_MASTER_MST rA42_ZONE_MASTER_MST = new RA42_ZONE_MASTER_MST();
                if (ZONE != null && !ZONE.Contains(0))
                {
                    for (int i = 0; i < ZONE.Length; i++)
                    {

                        try
                        {
                            if (SUB_ZONE[i] == 0)
                            {
                                //rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = null;
                            }

                            if (SUB_ZONE[i].Equals(null))
                            {
                                //rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = null;
                            }
                            else
                            {
                                rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = SUB_ZONE[i];
                            }


                        }
                        catch (System.IndexOutOfRangeException ex)
                        {
                            //rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = null;
                        }
                        rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE;
                        rA42_ZONE_MASTER_MST.ZONE_CODE = ZONE[i];
                        rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                        rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                        db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                        db.SaveChanges();
                        //continue;
                    }

                }
                //add new documents 
                if (files != null)
                {
                   
                    try
                    {
                        //create foreach loop for uploading multiple files 
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
                                    //add new file to db table 
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE,
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
                                    //delete all uploaded files if there is somthing wrong with one document, this is security proceduers
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return RedirectToAction("Edit", new { id = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE });
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
                    rA42_COMMENT.PASS_ROW_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE;
                    rA42_COMMENT.CRD_BY = currentUser;
                    rA42_COMMENT.CRD_DT = DateTime.Now;
                    rA42_COMMENT.COMMENT = COMMENT;
                    db.RA42_COMMENTS_MST.Add(rA42_COMMENT);


                }
                //add new employee, by checking if the IDENTITY_CODE not null, that means there is new employees created
                if (IDENTITY_CODE != null)
                {
                    RA42_TEMPRORY_COMPANY_PASS_DTL rA42_TEMPRORY_COMPANY_PASS_DTL = new RA42_TEMPRORY_COMPANY_PASS_DTL();
                    for (int j = 0; j < IDENTITY_CODE.Length; j++)
                    {
                        //create barcode for every permit 
                        string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                        rA42_TEMPRORY_COMPANY_PASS_DTL.COMPANY_PASS_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE;
                        rA42_TEMPRORY_COMPANY_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        rA42_TEMPRORY_COMPANY_PASS_DTL.IDENTITY_CODE = IDENTITY_CODE[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.GENDER_ID = GENDER[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.PASS_TYPE_CODE = PASS_TYPE_CODE[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.WORK_CARD_NUMBER = WORK_CARD_NUMBER[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.ID_CARD_NUMBER = ID_CARD_NUMBER[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.CARD_EXPIRED_DATE = CARD_EXPIRED_DATE[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.NAME_A = NAME_A[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.NAME_E = NAME_E[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.PROFESSION_A = PROFESSION_A[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.PROFESSION_E = PROFESSION_E[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.WORK_PLACE = WORK_PLACE[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.BARCODE = barcode;
                        rA42_TEMPRORY_COMPANY_PASS_DTL.GSM = GSM[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.ADDRESS = ADDRESS[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.DATE_FROM = DATE_FROM[j];
                        rA42_TEMPRORY_COMPANY_PASS_DTL.DATE_TO = DATE_TO[j];
                        if (PERSONAL_IMAGE != null)
                        {
                            try
                            {
                                
                                // Verify that the user selected a file
                                if (PERSONAL_IMAGE[j].ContentLength > 0)
                                {
                                    // extract only the filename with extention
                                    string fileName = Path.GetFileNameWithoutExtension(PERSONAL_IMAGE[j].FileName);
                                    string extension = Path.GetExtension(PERSONAL_IMAGE[j].FileName);


                                    //check file extention 
                                    if (general.CheckPersonalImage(PERSONAL_IMAGE[j].FileName))
                                    {

                                        fileName = "Profile_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                                        // store the file inside ~/Files/Profiles/ folder
                                        bool check = general.ResizeImage(PERSONAL_IMAGE[j], fileName);

                                        if (check != true)
                                        {
                                            AddToast(new Toast("",
                                           GetResourcesValue("error_update_message"),
                                           "red"));
                                            TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                            return View(rA42_COMPANY_PASS_DTL);
                                        }

                                        rA42_TEMPRORY_COMPANY_PASS_DTL.PERSONAL_IMAGE = fileName;


                                    }
                                    else
                                    {
                                        //show error message if   file extention not supported 
                                        AddToast(new Toast("",
                                        GetResourcesValue("error_update_message"),
                                        "red"));
                                        TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                        return View(rA42_COMPANY_PASS_DTL);
                                    }
                                }
                            }

                            catch (Exception ex)
                            {
                                ex.GetBaseException();
                            }
                        }
                        rA42_TEMPRORY_COMPANY_PASS_DTL.CRD_BY = currentUser;
                        rA42_TEMPRORY_COMPANY_PASS_DTL.CRD_DT = DateTime.Now;
                        rA42_TEMPRORY_COMPANY_PASS_DTL.UPD_BY = currentUser;
                        rA42_TEMPRORY_COMPANY_PASS_DTL.UPD_DT = DateTime.Now;
                        db.RA42_TEMPRORY_COMPANY_PASS_DTL.Add(rA42_TEMPRORY_COMPANY_PASS_DTL);
                        db.SaveChanges();
                    }
                }




                general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                var x = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE !=false && a.DLT_STS !=true && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                var currentUserInfo = (new UserInfo()).getUserInfo();
               
                if (x != null)
                {

                   

                    //this section is for applicant 
                    if (form["approvebtn"] != null && (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11)))
                    {


                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.WORKFLOW_RESPO_CODE == general_data.WORKFLOW_RESPO_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        //he can change the autho person 
                        if (rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE == null)
                        {
                            general_data.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;
                        }
                        else
                        {
                            general_data.WORKFLOW_RESPO_CODE = rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE;
                        }
                        general_data.COMPANY_TYPE_CODE = general_data.COMPANY_TYPE_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.COMPANY_CODE = rA42_COMPANY_PASS_DTL.COMPANY_CODE;
                        general_data.SERVICE_NUMBER = rA42_COMPANY_PASS_DTL.SERVICE_NUMBER;
                        general_data.RESPONSIBLE = rA42_COMPANY_PASS_DTL.RESPONSIBLE;
                        general_data.PURPOSE_OF_PASS = rA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_COMPANY_PASS_DTL.REMARKS;
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
                        return RedirectToAction("Mypasses");
                    }

                    //this section is for autho person 
                    if (form["approvebtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2)
                    {

                        //he should redirect the request to the permits cell 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE });

                        }
                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.COMPANY_TYPE_CODE = general_data.COMPANY_TYPE_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.COMPANY_CODE = rA42_COMPANY_PASS_DTL.COMPANY_CODE;
                        general_data.SERVICE_NUMBER = rA42_COMPANY_PASS_DTL.SERVICE_NUMBER;
                        general_data.RESPONSIBLE = rA42_COMPANY_PASS_DTL.RESPONSIBLE;
                        general_data.PURPOSE_OF_PASS = rA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_COMPANY_PASS_DTL.REMARKS;
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

                    //this section is for permit cell 
                    if (form["approvebtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3)
                    {

                        //if the status is true, that means the whole request is complete and no need to redirect it to the security officer 
                        if (general_data.STATUS == true)
                        {
                            general_data.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;
                        }
                        else
                        {
                            //if the status not true, redirect the request to the security officer 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Edit", new { id = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE });

                            }
                            general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;

                        }
                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.COMPANY_TYPE_CODE = general_data.COMPANY_TYPE_CODE;
                        general_data.COMPANY_CODE = rA42_COMPANY_PASS_DTL.COMPANY_CODE;
                        general_data.SERVICE_NUMBER = rA42_COMPANY_PASS_DTL.SERVICE_NUMBER;
                        general_data.RESPONSIBLE = rA42_COMPANY_PASS_DTL.RESPONSIBLE;
                        general_data.PURPOSE_OF_PASS = rA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_COMPANY_PASS_DTL.REMARKS;
                        general_data.APPROVAL_SN = general_data.APPROVAL_SN;
                        general_data.APPROVAL_NAME = general_data.APPROVAL_NAME;
                        general_data.APPROVAL_RANK = general_data.APPROVAL_RANK;
                        general_data.APPROVAL_APPROVISION_DATE = general_data.APPROVAL_APPROVISION_DATE;
                        general_data.AUTHO_SN = general_data.AUTHO_SN;
                        general_data.AUTHO_NAME = general_data.AUTHO_NAME;
                        general_data.AUTHO_RANK = general_data.AUTHO_RANK;
                        general_data.AUTHO_APPROVISION_DATE = general_data.AUTHO_APPROVISION_DATE;
                        general_data.PERMIT_SN = currentUserInfo["user_sno"];
                        general_data.PERMIT_NAME = currentUserInfo["user_name_ar"];
                        general_data.PERMIT_RANK = currentUserInfo["user_rank_ar"];
                        general_data.PERMIT_APPROVISION_DATE = DateTime.Now;
                        general_data.BARCODE = general_data.BARCODE;
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
                    //this section is for seurity officer 
                    if (form["approvebtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4)
                    {



                        //after complete everthing, security officer should redirect the permit to the permits cell for print
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE });

                        }
                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.COMPANY_TYPE_CODE = general_data.COMPANY_TYPE_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.COMPANY_CODE = rA42_COMPANY_PASS_DTL.COMPANY_CODE;
                        general_data.SERVICE_NUMBER = rA42_COMPANY_PASS_DTL.SERVICE_NUMBER;
                        general_data.RESPONSIBLE = rA42_COMPANY_PASS_DTL.RESPONSIBLE;
                        general_data.PURPOSE_OF_PASS = rA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_COMPANY_PASS_DTL.REMARKS;
                        general_data.APPROVAL_SN = general_data.APPROVAL_SN;
                        general_data.APPROVAL_NAME = general_data.APPROVAL_NAME;
                        general_data.APPROVAL_RANK = general_data.APPROVAL_RANK;
                        general_data.APPROVAL_APPROVISION_DATE = general_data.APPROVAL_APPROVISION_DATE;
                        //general_data.AUTHO_SN = currentUserInfo["user_sno"];
                        //general_data.AUTHO_NAME = currentUserInfo["user_name_ar"];
                        //general_data.AUTHO_RANK = currentUserInfo["user_rank_ar"];
                        general_data.AUTHO_APPROVISION_DATE = DateTime.Now;
                        general_data.PERMIT_SN = general_data.PERMIT_SN;
                        general_data.PERMIT_NAME = general_data.PERMIT_NAME;
                        general_data.PERMIT_RANK = general_data.PERMIT_RANK;
                        general_data.PERMIT_APPROVISION_DATE = general_data.PERMIT_APPROVISION_DATE;
                        if (rA42_COMPANY_PASS_DTL.BARCODE != null)
                        {
                            general_data.BARCODE = rA42_COMPANY_PASS_DTL.BARCODE;
                        }
                        else
                        {
                            general_data.BARCODE = general_data.BARCODE;
                        }
                        general_data.ISOPENED = general_data.ISOPENED;
                        general_data.REJECTED = false;
                        general_data.STATUS = true;
                        general_data.ISPRINTED = general_data.ISPRINTED;

                        db.Entry(general_data).State = EntityState.Modified;
                        db.SaveChanges();
                        AddToast(new Toast("",
                        GetResourcesValue("success_update_message"),
                        "green"));
                        return RedirectToAction("Newpasses");
                    }

                    
                    //this section is for autho person for reject the request 
                    if (form["rejectbtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2)
                    {


                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE });

                        }
                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.COMPANY_TYPE_CODE = general_data.COMPANY_TYPE_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.COMPANY_CODE = rA42_COMPANY_PASS_DTL.COMPANY_CODE;
                        general_data.SERVICE_NUMBER = rA42_COMPANY_PASS_DTL.SERVICE_NUMBER;
                        general_data.RESPONSIBLE = rA42_COMPANY_PASS_DTL.RESPONSIBLE;
                        general_data.PURPOSE_OF_PASS = rA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_COMPANY_PASS_DTL.REMARKS;
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
                    //this section is for permits cell for reject request and return it to the autho person 
                    if (form["rejectbtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3)
                    {


                        //find the autho person from APPROVAL_SN, this service number of the autho person 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == general_data.APPROVAL_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE });

                        }
                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.COMPANY_TYPE_CODE = general_data.COMPANY_TYPE_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.COMPANY_CODE = rA42_COMPANY_PASS_DTL.COMPANY_CODE;
                        general_data.SERVICE_NUMBER = rA42_COMPANY_PASS_DTL.SERVICE_NUMBER;
                        general_data.RESPONSIBLE = rA42_COMPANY_PASS_DTL.RESPONSIBLE;
                        general_data.PURPOSE_OF_PASS = rA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_COMPANY_PASS_DTL.REMARKS;
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
                    //this section is for security officer tomreject the request and return it to the permits cell 
                    if (form["rejectbtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4)
                    {
                        
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == general_data.PERMIT_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE });

                        }
                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.COMPANY_TYPE_CODE = general_data.COMPANY_TYPE_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.COMPANY_CODE = rA42_COMPANY_PASS_DTL.COMPANY_CODE;
                        general_data.SERVICE_NUMBER = rA42_COMPANY_PASS_DTL.SERVICE_NUMBER;
                        general_data.RESPONSIBLE = rA42_COMPANY_PASS_DTL.RESPONSIBLE;
                        general_data.PURPOSE_OF_PASS = rA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_COMPANY_PASS_DTL.REMARKS;
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
                else
                {
                   
                }



            }
            TempData["Erorr"] = "Somthing wrong happen,حدث خطأ ما";
            AddToast(new Toast("",
                GetResourcesValue("error_update_message"),
                "red"));
            return View(rA42_COMPANY_PASS_DTL);
        }

        // this view to delete record 
        public ActionResult Delete(int? id)
		{
            ViewBag.activetab = "delete";

            if (id == null)
            {
                return NotFound();
            }
            //check if the request id is in the table 
            RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL = db.RA42_COMPANY_PASS_DTL.Find(id);
            if (rA42_COMPANY_PASS_DTL == null)
            {
                return NotFound();
            }

            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_COMPANY_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_COMPANY_PASS_DTL.RESPONSIBLE !=currentUser)
                {
                    if (rA42_COMPANY_PASS_DTL.STATUS == true)
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
                if (rA42_COMPANY_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_COMPANY_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    //if (ViewBag.RESPO_STATE != rA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                    //{
                    //    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    //    {
                    //        return NotFound();
                    //    }
                    //}
                }
            }

           
            //get zones and gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get documents 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get employees asigned to the request to get for them permits 
            ViewBag.GetEmployees = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == id && a.DLT_STS != true).ToList();
            return View(rA42_COMPANY_PASS_DTL);
        }

		// confirm deleting the record 
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public ActionResult DeleteConfirmed(int id)
		{
            var general_data = db.RA42_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == id).FirstOrDefault();

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
        //delete any document of the request 
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
        //delete personal image of any employee 
        [HttpPost]
        public JsonResult DeleteImage(int id)
        {
            bool result = false;
            var general_data = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.TEMPRORY_COMPANY_PASS_CODE == id).FirstOrDefault();

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
        //delete zones and gates for any request 
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
        //delete any employee from any request 
        [HttpPost]
        public JsonResult DeleteEmployee(int id)
        {
            bool result = false;
            var general_data = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.TEMPRORY_COMPANY_PASS_CODE == id).FirstOrDefault();

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
                result = true;
            }

            return Json(result, JsonRequestBehavior.AllowGet);


        }
        //this is to confirm printing, and redirect the printed permits to the printed view
        [HttpPost]
        public JsonResult PrintById(int id, string type)
        {
            //here we have to print two sides, first we need to print specifci employee permit 
            //then check if its all employees i tha same request are printed
            //if its yes, set whole request to print 

            bool result = false;
            var general_data = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.TEMPRORY_COMPANY_PASS_CODE == id && a.DLT_STS !=true).FirstOrDefault();

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

            //check if its all employees i tha same request are printed
            //if its yes, set whole request to print 

            var company = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == general_data.COMPANY_PASS_CODE && a.DLT_STS !=true).ToList();
            bool check = true;
            foreach (var item in company)
            {
                if(item.ISPRINTED != true)
                {
                    check = false;
                }
                
            }
            //so if check still true that means all employees are printed, and should whole request set to true in print column
            if (check == true)
            {
                var comp_pass = db.RA42_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == general_data.COMPANY_PASS_CODE && a.DLT_STS !=true).FirstOrDefault();
                if (comp_pass != null)
                {


                    comp_pass.UPD_BY = currentUser;
                    comp_pass.UPD_DT = DateTime.Now;
                    comp_pass.ISPRINTED = true;
                    db.Entry(comp_pass).State = EntityState.Modified;
                    db.SaveChanges();
                    result = true;
                }
            }



            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult PrintAllById(int id, string type)
        {
            //here we have to print two sides, first we need to print specifci employee permit 
            //then check if its all employees i tha same request are printed
            //if its yes, set whole request to print 

            bool result = false;
          

            //check if its all employees i tha same request are printed
            //if its yes, set whole request to print 

            var company = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == id && a.DLT_STS != true).ToList();
            bool check = true;
            foreach (var item in company)
            {
                if (item.ISPRINTED != true)
                {
                    check = false;
                }

            }
            //so if check still true that means all employees are printed, and should whole request set to true in print column
            if (check == true)
            {
                var comp_pass = db.RA42_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == id && a.DLT_STS != true).FirstOrDefault();
                if (comp_pass != null)
                {


                    comp_pass.UPD_BY = currentUser;
                    comp_pass.UPD_DT = DateTime.Now;
                    comp_pass.ISPRINTED = true;
                    db.Entry(comp_pass).State = EntityState.Modified;
                    db.SaveChanges();
                    result = true;
                }
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
        //show notfound page if someone try to break any page 
        public ActionResult NotFound()
        {
            return RedirectToAction("NotFound", "Home");
        }

    }
}
