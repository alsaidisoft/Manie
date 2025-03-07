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
using SecurityClearanceWebApp.Util;
using System.Linq.Dynamic;

namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    //this is visitor permit controller, simpele one without any complicated views
	public class TraineepassController : Controller
	{
        //get db connection
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get military number of current user
        private string currentUser = (new UserInfo()).getSNO();
       //identify GeneralFunctions class to use some of its functions
        private GeneralFunctions general = new GeneralFunctions();
        //set title of the controller from the resources
        string title = Resources.Passes.ResourceManager.GetString("Trainees_pass" + "_" + "ar");

        //identify some important variables to use them
        private int STATION_CODE = 0;
        private int WORKFLOWID = 0;
        private int RESPO_CODE = 0;
        private int ACCESS_TYPE_CODE = 8;
        private int FORCE_ID = 0;
        public TraineepassController() {
            ViewBag.Managepasses = "Managepasses";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Traineepass";
            if(Language.GetCurrentLang() == "en")
            {
                title = Resources.Passes.ResourceManager.GetString("Trainees_pass" + "_" + "en");
            }
            //set icon of controller from fontawsome library
			ViewBag.controllerIconClass = "fa fa-user-cog";
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;
            //check if the current user has authority in this kind of permit 
            var v = Task.Run(async () => await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefaultAsync()).Result;
            if (v != null)
            {
                if (v.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && v.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false)
                {
                    //get id of the user
                    ViewBag.RESPO_ID = v.WORKFLOW_RESPO_CODE;
                    //get workflow id type 
                    ViewBag.RESPO_STATE = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID;
                    RESPO_CODE = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE;
                    WORKFLOWID = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value;
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
        //get subzones of main zones as json result 
        public JsonResult GetSubZones(int zone)
        {
            db.Configuration.ProxyCreationEnabled = false;
            if (Language.GetCurrentLang() == "en")
            {
                var sub_zones = db.RA42_ZONE_SUB_AREA_MST.Where(a => a.ZONE_CODE == zone && a.DLT_STS != true).Select(a => new { ZONE_SUB_AREA_CODE = a.ZONE_SUB_AREA_CODE, SUB_ZONE_NAME = a.SUB_ZONE_NAME_E }).ToList();

                //Add JsonRequest behavior to allow retrieving states over http get
                return Json(sub_zones, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var sub_zones = db.RA42_ZONE_SUB_AREA_MST.Where(a => a.ZONE_CODE == zone && a.DLT_STS != true).Select(a => new { ZONE_SUB_AREA_CODE = a.ZONE_SUB_AREA_CODE, SUB_ZONE_NAME = a.SUB_ZONE_NAME }).ToList();

                //Add JsonRequest behavior to allow retrieving states over http get
                return Json(sub_zones, JsonRequestBehavior.AllowGet);
            }
        }

        //get comments of the request as seperated view  
        public ActionResult Comments(int? id)
        {
            ViewBag.activetab = "Comments";

            if (id == null)
            {
                return NotFound();
            }
            RA42_TRAINEES_PASS_DTL rA42_TRAINEES_PASS_DTL = db.RA42_TRAINEES_PASS_DTL.Find(id);
            if (rA42_TRAINEES_PASS_DTL == null)
            {
                return NotFound();
            }

            //check if current user has authority to open comments view for this permit 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_TRAINEES_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_TRAINEES_PASS_DTL.RESPONSIBLE != currentUser)
                {
                    if (rA42_TRAINEES_PASS_DTL.ISOPENED != true)
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
                if (rA42_TRAINEES_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_TRAINEES_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != rA42_TRAINEES_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            return NotFound();
                        }
                    }
                }
            }
            //get comments as viewbag 
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            return View(rA42_TRAINEES_PASS_DTL);
        }

        // POST new comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Comments(RA42_TRAINEES_PASS_DTL rA42_TRAINEES_PASS_DTL, string COMMENT)
        {
            ViewBag.activetab = "Comments";
            var general_data = db.RA42_TRAINEES_PASS_DTL.Where(a => a.TRAINEE_PASS_CODE == rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE).FirstOrDefault();

            
            //add comment
            if (COMMENT.Length > 0)
            {
                RA42_COMMENTS_MST rA42_COMMENT = new RA42_COMMENTS_MST();
                rA42_COMMENT.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_COMMENT.PASS_ROW_CODE = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE;
                rA42_COMMENT.CRD_BY = currentUser;
                rA42_COMMENT.CRD_DT = DateTime.Now;
                rA42_COMMENT.COMMENT = COMMENT;
                db.RA42_COMMENTS_MST.Add(rA42_COMMENT);
                db.SaveChanges();
                AddToast(new Toast("",
                  GetResourcesValue("add_comment_success"),
                  "green"));

            }
            //get comments as viewbage
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            return View(rA42_TRAINEES_PASS_DTL);


        }

        // this view to show links 
        public ActionResult Index()
		{
            
            return View();
		}
        // this view to choose which creation type you want to create permit request, this view is for authorized user in the system such as permits cell and applicant 
        public ActionResult Choosecreatetype()
        {
            ViewBag.activetab = "Privatepass";
            return View();
        }
        //this view for administrator or developer to get all visitor permits
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

            var empList = db.RA42_TRAINEES_PASS_DTL.Where(a => a.WORKFLOW_RESPO_CODE != null).Select(a => new
            {
                TRAINEE_PASS_CODE = a.TRAINEE_PASS_CODE,
                SERVICE_NUMBER = (a.SERVICE_NUMBER != null ? a.SERVICE_NUMBER : " "),
                CIVIL_NUMBER = (a.ID_CARD_NUMBER != null ? a.ID_CARD_NUMBER : " "),
                PERSONAL_IMAGE = a.PERSONAL_IMAGE,
                NAME_A = (a.TRAINEE_NAME != null ? a.TRAINEE_NAME : " "),
                MAJOR = (a.MAJOR != null ? a.MAJOR : " "),
                PHONE_NUMBER = (a.PHONE_NUMBER != null ? a.PHONE_NUMBER : " "),
                GSM = (a.GSM != null ? a.GSM : " "),
                PURPOSE_OF_PASS = (a.PURPOSE_OF_PASS != null ? a.PURPOSE_OF_PASS : " "),
                STATION_CODE = a.STATION_CODE,
                FORCE_ID = a.RA42_STATIONS_MST.FORCE_ID.Value,
                STATION_A = a.RA42_STATIONS_MST.STATION_NAME_A,
                STATION_E = a.RA42_STATIONS_MST.STATION_NAME_E,
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
                COMMENTS = a.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == a.TRAINEE_PASS_CODE).Count()


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
                    //empList = empList.
                    //Where(x => x.SERVICE_NUMBER == searchValue || x.NAME_A == searchValue || x.PHONE_NUMBER_NUMBER == searchValue || x.GSM == searchValue || x.PLATE_NUMBER == searchValue || x.STATION_CODE == STATION_CODE).ToList<RA42_VECHILE_PASS_DTL>();
                    empList = empList.
                Where(x => x.SERVICE_NUMBER.Contains(searchValue) || x.CIVIL_NUMBER.Contains(searchValue) || x.STEP_NAME.Contains(searchValue) || x.NAME_A.Contains(searchValue) || x.PHONE_NUMBER.Contains(searchValue) || x.GSM.Contains(searchValue) || x.PURPOSE_OF_PASS.Contains(searchValue) || x.STATION_A == searchValue).ToList();
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
        //printed permits
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
            var empList = db.RA42_TRAINEES_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID && a.STATION_CODE == STATION_CODE 
            && a.DLT_STS != true 
            && a.ISPRINTED == true && a.WORKFLOW_RESPO_CODE !=null).Select(a => new
            {
                TRAINEE_PASS_CODE = a.TRAINEE_PASS_CODE,
                SERVICE_NUMBER = (a.SERVICE_NUMBER != null ? a.SERVICE_NUMBER : " "),
                CIVIL_NUMBER = (a.ID_CARD_NUMBER != null ? a.ID_CARD_NUMBER : " "),
                PERSONAL_IMAGE = a.PERSONAL_IMAGE,
                NAME_A = (a.TRAINEE_NAME != null ? a.TRAINEE_NAME : " "),
                MAJOR = (a.MAJOR != null ? a.MAJOR : " "),
                PHONE_NUMBER = (a.PHONE_NUMBER != null ? a.PHONE_NUMBER : " "),
                GSM = (a.GSM != null ? a.GSM : " "),
                PURPOSE_OF_PASS = (a.PURPOSE_OF_PASS != null ? a.PURPOSE_OF_PASS : " "),
                STATION_CODE = a.STATION_CODE,
                STATION_A = a.RA42_STATIONS_MST.STATION_NAME_A,
                STATION_E = a.RA42_STATIONS_MST.STATION_NAME_E,
                RESPONSEPLE_NAME = a.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                RESPONSEPLE_NAME_E = a.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E,
                STEP_NAME = a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME,
                STEP_NAME_E = a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E,
                STATUS = a.STATUS,
                RETURNED = a.RETURNED,
                ACCESS_TYPE_CODE = a.ACCESS_TYPE_CODE.Value,
                DLT_STS = a.DLT_STS,
                REJECTED = a.REJECTED,
                ISPRINTED = a.ISPRINTED,
                DATE_FROM = a.DATE_FROM,
                DATE_TO = a.DATE_TO,
                COMMENTS = a.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == a.TRAINEE_PASS_CODE).Count()


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
                Where(x => x.SERVICE_NUMBER.Contains(searchValue) || x.CIVIL_NUMBER.Contains(searchValue) || x.NAME_A.Contains(searchValue) || x.PHONE_NUMBER.Contains(searchValue) || x.GSM.Contains(searchValue) || x.PURPOSE_OF_PASS.Contains(searchValue) || x.STATION_A == searchValue).ToList();
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
            
            return View();
        }
        //this view is for autho person (المنسق الأمني)
        public ActionResult Authopasses()
        {
            ViewBag.activetab = "Authopasses";
            var rA42_TRAINEES_PASS_DTLs = db.RA42_TRAINEES_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.DLT_STS != true).OrderByDescending(a => a.TRAINEE_PASS_CODE);
            return View(rA42_TRAINEES_PASS_DTLs.ToList());
        }
        //this view is for permits cell and security officer to proccess the requests
        public async Task<ActionResult> Newpasses()
        {
            ViewBag.activetab = "Newpasses";
            var rA42_TRAINEES_PASS_DTL = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID && a.STATION_CODE == STATION_CODE && a.DLT_STS != true  && a.STATUS !=true).OrderByDescending(a => a.TRAINEE_PASS_CODE).ToListAsync();
            return View(rA42_TRAINEES_PASS_DTL);
        }

        public async Task<ActionResult> ToPrint()
        {
            ViewBag.activetab = "ToPrint";
            var rA42_TRAINEES_PASS_DTL = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID && a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ISPRINTED != true && a.STATUS == true).OrderByDescending(a => a.TRAINEE_PASS_CODE).ToListAsync();
            return View(rA42_TRAINEES_PASS_DTL);
        }
        //this view to show all printed permits of any station and this is for permits cell
        public ActionResult Printed()
        {
            ViewBag.activetab = "Printed";
            return View();
        }

        // details of the permit
        public ActionResult Details(int? id)
		{
			if (id == null)
			{
                return NotFound();
			}
            RA42_TRAINEES_PASS_DTL rA42_TRAINEES_PASS_DTL = db.RA42_TRAINEES_PASS_DTL.Find(id);
            if (rA42_TRAINEES_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if current user has authority to open comments view for this permit 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_TRAINEES_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_TRAINEES_PASS_DTL.RESPONSIBLE != currentUser)
                {
                    if (rA42_TRAINEES_PASS_DTL.ISOPENED != true)
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
                if (rA42_TRAINEES_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_TRAINEES_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != rA42_TRAINEES_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            return NotFound();
                        }
                    }
                }
            }
            //get selected zones and gates and documents of the request
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
           
            return View(rA42_TRAINEES_PASS_DTL);
		}

        // this is card view to print permit directly or save it 
        public ActionResult Card(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            RA42_TRAINEES_PASS_DTL rA42_TRAINEES_PASS_DTL = db.RA42_TRAINEES_PASS_DTL.Find(id);
            if (rA42_TRAINEES_PASS_DTL == null)
            {
                return NotFound();
            }

            //check if current user has authority to open comments view for this permit 
            if (ViewBag.RESPO_STATE != rA42_TRAINEES_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
            {
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    return NotFound();
                }
            }
            //get selected files and zones or gates
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_TRAINEES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToList();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_TRAINEES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToList();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }
            return View(rA42_TRAINEES_PASS_DTL);
        }
        [HttpPost]
        public ActionResult Card(string CheckPrinted, int TRANSACTION_TYPE_CODE, string TRANSACTION_REMARKS, HttpPostedFileBase RECEIPT, RA42_TRAINEES_PASS_DTL _TRAINEES_PASS_DTL)
        {
            
            RA42_TRAINEES_PASS_DTL rA42_TRAINEES_PASS_DTL = db.RA42_TRAINEES_PASS_DTL.Find(_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE);
            if (rA42_TRAINEES_PASS_DTL == null)
            {
                return NotFound();
            }

            //check if current user has authority to open comments view for this permit 
            if (ViewBag.RESPO_STATE != rA42_TRAINEES_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
            {
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    return NotFound();
                }
            }
            //get selected files and zones or gates
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == _TRAINEES_PASS_DTL.TRAINEE_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == _TRAINEES_PASS_DTL.TRAINEE_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_TRAINEES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToList();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_TRAINEES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToList();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }

            RA42_TRANSACTION_DTL rA42_TRANSACTION_DTL = new RA42_TRANSACTION_DTL();
            rA42_TRANSACTION_DTL.ACCESS_ROW_CODE = _TRAINEES_PASS_DTL.TRAINEE_PASS_CODE;
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
                            return View(rA42_TRAINEES_PASS_DTL);
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
            rA42_TRAINEES_PASS_DTL.UPD_BY = currentUser;
            rA42_TRAINEES_PASS_DTL.UPD_DT = DateTime.Now;
            rA42_TRAINEES_PASS_DTL.ISDELIVERED = false;
            db.SaveChanges();
            TempData["Success"] = "تم تحديث المعاملة بنجاح";
            if (CheckPrinted.Equals("Printed"))
            {
                var deletePrinted = db.RA42_PRINT_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.PASS_ROW_CODE ==
                rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE).ToList();
                if (deletePrinted.Count > 0)
                {
                    foreach (var item in deletePrinted)
                    {
                        item.DLT_STS = true;
                        db.SaveChanges();
                    }
                }
            }

            return View(rA42_TRAINEES_PASS_DTL);
        }
        // this is main view to create visitor permit, no other view to create permit 
       
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
                    FORCE_ID = check_unit.FORCE_ID.Value;


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
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 3 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get reponsible person to process this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error if there is no one for applicant only 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View();
                    }

                }
            }
            else
            {
                //get identities in arabic
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 3 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get reponsible person to process this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error if there is no one for applicant only 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View();
                    }

                }
            }
            return View();
		}

		// POST new data
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Create(RA42_TRAINEES_PASS_DTL rA42_TRAINEES_PASS_DTL, int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES,string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
		{
            ViewBag.activetab = "Create";
            ViewBag.Service_No = currentUser;

            var url = Url.RequestContext.RouteData.Values["id"];
            //check if session not null 
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
                    FORCE_ID = check_unit.FORCE_ID.Value;


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
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_TRAINEES_PASS_DTL.IDENTITY_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_TRAINEES_PASS_DTL.GENDER_ID);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 3 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get reponsible person to process this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_TRAINEES_PASS_DTL);
                    }

                }
            }
            else
            {
                //get identities in arabic
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_TRAINEES_PASS_DTL.IDENTITY_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_TRAINEES_PASS_DTL.GENDER_ID);
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 3 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get reponsible person to process this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE);
                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_TRAINEES_PASS_DTL);
                    }

                }

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


                            //check extention of current image 
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
                                    return View(rA42_TRAINEES_PASS_DTL);
                                }

                                rA42_TRAINEES_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_TRAINEES_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                //get current user military info from api
                User permit = null;
                Task<User> callTask2 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask2.Wait();
                permit = callTask2.Result;

                //this section is for applicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {

                    rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_TRAINEES_PASS_DTL.REJECTED = false;
                    rA42_TRAINEES_PASS_DTL.STATUS = false;
                    rA42_TRAINEES_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect the request to permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).FirstOrDefault();
                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_TRAINEES_PASS_DTL);

                    }
                    rA42_TRAINEES_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_TRAINEES_PASS_DTL.APPROVAL_RANK = permit.NAME_RANK_A;
                    rA42_TRAINEES_PASS_DTL.APPROVAL_NAME = permit.NAME_EMP_A;
                    rA42_TRAINEES_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    rA42_TRAINEES_PASS_DTL.REJECTED = false;
                    rA42_TRAINEES_PASS_DTL.STATUS = false;
                    rA42_TRAINEES_PASS_DTL.ISOPENED = true;
                }
               
                //permits cell section
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //they should redirect the permit to the securiy officer to approve the permit 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_TRAINEES_PASS_DTL);

                    }
                    else
                    {
                        rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_TRAINEES_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_TRAINEES_PASS_DTL.PERMIT_RANK = permit.NAME_RANK_A;
                    rA42_TRAINEES_PASS_DTL.PERMIT_NAME = permit.NAME_EMP_A;
                    rA42_TRAINEES_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                    rA42_TRAINEES_PASS_DTL.REJECTED = false;
                    rA42_TRAINEES_PASS_DTL.STATUS = false;
                    rA42_TRAINEES_PASS_DTL.ISOPENED = true;
                }
                //this is security officer section
                if(WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                   
                    //security officer should redirect the complete request to permits cell for printing 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_TRAINEES_PASS_DTL);

                        }
                        else
                        {
                            rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }

                        rA42_TRAINEES_PASS_DTL.AUTHO_SN = currentUser;
                        rA42_TRAINEES_PASS_DTL.AUTHO_RANK = permit.NAME_RANK_A;
                        rA42_TRAINEES_PASS_DTL.AUTHO_NAME = permit.NAME_EMP_A;
                        rA42_TRAINEES_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                        rA42_TRAINEES_PASS_DTL.REJECTED = false;
                        rA42_TRAINEES_PASS_DTL.STATUS = true;
                        rA42_TRAINEES_PASS_DTL.ISOPENED = true;
                        rA42_TRAINEES_PASS_DTL.BARCODE = rA42_TRAINEES_PASS_DTL.BARCODE;


                }
                rA42_TRAINEES_PASS_DTL.SERVICE_NUMBER = currentUser;
                rA42_TRAINEES_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_TRAINEES_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_TRAINEES_PASS_DTL.CARD_FOR_CODE = 3;
                rA42_TRAINEES_PASS_DTL.CRD_BY = currentUser;
                rA42_TRAINEES_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_TRAINEES_PASS_DTL.UPD_BY = currentUser;
                rA42_TRAINEES_PASS_DTL.UPD_DT = DateTime.Now;
                db.RA42_TRAINEES_PASS_DTL.Add(rA42_TRAINEES_PASS_DTL);
				db.SaveChanges();
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE;
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
                        //set foreach loop to upload multiple files 
                        int c = 0;
                        foreach (HttpPostedFileBase file in files)
                        {
                            // Verify that the user selected a file
                            if (file != null && file.ContentLength > 0)
                            {
                                // extract only the filename with extention
                                string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                                string extension = Path.GetExtension(file.FileName);


                                //check extention of the permit 
                                if (general.CheckFileType(file.FileName))
                                {
                                    

                                    fileName = "FileNO" + c + "_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);

                                    
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE,
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
                                    //if there is somthing wrong with one file, it will delete all uploaded documents this is security procedurs 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_TRAINEES_PASS_DTL);
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
			return View(rA42_TRAINEES_PASS_DTL);
		}

        public ActionResult ProveIdentityForWorker()
        {
            ViewBag.activetab = "ProveIdentityForWorker";
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
                    FORCE_ID = check_unit.FORCE_ID.Value;


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
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 18 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get reponsible person to process this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error if there is no one for applicant only 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View();
                    }

                }
            }
            else
            {
                //get identities in arabic
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 18 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get reponsible person to process this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error if there is no one for applicant only 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View();
                    }

                }
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProveIdentityForWorker(RA42_TRAINEES_PASS_DTL rA42_TRAINEES_PASS_DTL, int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "ProveIdentityForWorker";
            ViewBag.Service_No = currentUser;

            var url = Url.RequestContext.RouteData.Values["id"];
            //check if session not null 
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
                    FORCE_ID = check_unit.FORCE_ID.Value;


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
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_TRAINEES_PASS_DTL.IDENTITY_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_TRAINEES_PASS_DTL.GENDER_ID);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 18 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get reponsible person to process this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_TRAINEES_PASS_DTL);
                    }

                }
            }
            else
            {
                //get identities in arabic
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_TRAINEES_PASS_DTL.IDENTITY_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_TRAINEES_PASS_DTL.GENDER_ID);
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 18 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get reponsible person to process this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE);
                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_TRAINEES_PASS_DTL);
                    }

                }

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


                            //check extention of current image 
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
                                    return View(rA42_TRAINEES_PASS_DTL);
                                }

                                rA42_TRAINEES_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_TRAINEES_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                //get current user military info from api
                User permit = null;
                Task<User> callTask2 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask2.Wait();
                permit = callTask2.Result;

                //this section is for applicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {

                    rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_TRAINEES_PASS_DTL.REJECTED = false;
                    rA42_TRAINEES_PASS_DTL.STATUS = false;
                    rA42_TRAINEES_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect the request to permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).FirstOrDefault();
                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_TRAINEES_PASS_DTL);

                    }
                    rA42_TRAINEES_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_TRAINEES_PASS_DTL.APPROVAL_RANK = permit.NAME_RANK_A;
                    rA42_TRAINEES_PASS_DTL.APPROVAL_NAME = permit.NAME_EMP_A;
                    rA42_TRAINEES_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    rA42_TRAINEES_PASS_DTL.REJECTED = false;
                    rA42_TRAINEES_PASS_DTL.STATUS = false;
                    rA42_TRAINEES_PASS_DTL.ISOPENED = true;
                }

                //permits cell section
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //they should redirect the permit to the securiy officer to approve the permit 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_TRAINEES_PASS_DTL);

                    }
                    else
                    {
                        rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_TRAINEES_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_TRAINEES_PASS_DTL.PERMIT_RANK = permit.NAME_RANK_A;
                    rA42_TRAINEES_PASS_DTL.PERMIT_NAME = permit.NAME_EMP_A;
                    rA42_TRAINEES_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                    rA42_TRAINEES_PASS_DTL.REJECTED = false;
                    rA42_TRAINEES_PASS_DTL.STATUS = false;
                    rA42_TRAINEES_PASS_DTL.ISOPENED = true;
                }
                //this is security officer section
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {

                    //security officer should redirect the complete request to permits cell for printing 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_TRAINEES_PASS_DTL);

                    }
                    else
                    {
                        rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_TRAINEES_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_TRAINEES_PASS_DTL.AUTHO_RANK = permit.NAME_RANK_A;
                    rA42_TRAINEES_PASS_DTL.AUTHO_NAME = permit.NAME_EMP_A;
                    rA42_TRAINEES_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_TRAINEES_PASS_DTL.REJECTED = false;
                    rA42_TRAINEES_PASS_DTL.STATUS = true;
                    rA42_TRAINEES_PASS_DTL.ISOPENED = true;
                    rA42_TRAINEES_PASS_DTL.BARCODE = rA42_TRAINEES_PASS_DTL.BARCODE;


                }
                rA42_TRAINEES_PASS_DTL.SERVICE_NUMBER = currentUser;
                rA42_TRAINEES_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_TRAINEES_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_TRAINEES_PASS_DTL.CARD_FOR_CODE = 10;
                rA42_TRAINEES_PASS_DTL.CRD_BY = currentUser;
                rA42_TRAINEES_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_TRAINEES_PASS_DTL.UPD_BY = currentUser;
                rA42_TRAINEES_PASS_DTL.UPD_DT = DateTime.Now;
                db.RA42_TRAINEES_PASS_DTL.Add(rA42_TRAINEES_PASS_DTL);
                db.SaveChanges();
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE;
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
                        //set foreach loop to upload multiple files 
                        int c = 0;
                        foreach (HttpPostedFileBase file in files)
                        {
                            // Verify that the user selected a file
                            if (file != null && file.ContentLength > 0)
                            {
                                // extract only the filename with extention
                                string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                                string extension = Path.GetExtension(file.FileName);


                                //check extention of the permit 
                                if (general.CheckFileType(file.FileName))
                                {


                                    fileName = "FileNO" + c + "_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);


                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE,
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
                                    //if there is somthing wrong with one file, it will delete all uploaded documents this is security procedurs 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_TRAINEES_PASS_DTL);
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
            return View(rA42_TRAINEES_PASS_DTL);
        }

        public ActionResult ProveIdentityForBusAdmin()
        {
            ViewBag.activetab = "ProveIdentityForBusAdmin";
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
                    FORCE_ID = check_unit.FORCE_ID.Value;


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
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 20 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get reponsible person to process this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error if there is no one for applicant only 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View();
                    }

                }
            }
            else
            {
                //get identities in arabic
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 20 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get reponsible person to process this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error if there is no one for applicant only 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View();
                    }

                }
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProveIdentityForBusAdmin(RA42_TRAINEES_PASS_DTL rA42_TRAINEES_PASS_DTL, int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "ProveIdentityForBusAdmin";
            ViewBag.Service_No = currentUser;

            var url = Url.RequestContext.RouteData.Values["id"];
            //check if session not null 
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
                    FORCE_ID = check_unit.FORCE_ID.Value;


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
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_TRAINEES_PASS_DTL.IDENTITY_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_TRAINEES_PASS_DTL.GENDER_ID);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 20 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get reponsible person to process this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_TRAINEES_PASS_DTL);
                    }

                }
            }
            else
            {
                //get identities in arabic
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_TRAINEES_PASS_DTL.IDENTITY_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_TRAINEES_PASS_DTL.GENDER_ID);
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 20 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get reponsible person to process this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE);
                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_TRAINEES_PASS_DTL);
                    }

                }

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


                            //check extention of current image 
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
                                    return View(rA42_TRAINEES_PASS_DTL);
                                }

                                rA42_TRAINEES_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_TRAINEES_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                //get current user military info from api
                User permit = null;
                Task<User> callTask2 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask2.Wait();
                permit = callTask2.Result;

                //this section is for applicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {

                    rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_TRAINEES_PASS_DTL.REJECTED = false;
                    rA42_TRAINEES_PASS_DTL.STATUS = false;
                    rA42_TRAINEES_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect the request to permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).FirstOrDefault();
                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_TRAINEES_PASS_DTL);

                    }
                    rA42_TRAINEES_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_TRAINEES_PASS_DTL.APPROVAL_RANK = permit.NAME_RANK_A;
                    rA42_TRAINEES_PASS_DTL.APPROVAL_NAME = permit.NAME_EMP_A;
                    rA42_TRAINEES_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    rA42_TRAINEES_PASS_DTL.REJECTED = false;
                    rA42_TRAINEES_PASS_DTL.STATUS = false;
                    rA42_TRAINEES_PASS_DTL.ISOPENED = true;
                }

                //permits cell section
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //they should redirect the permit to the securiy officer to approve the permit 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_TRAINEES_PASS_DTL);

                    }
                    else
                    {
                        rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_TRAINEES_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_TRAINEES_PASS_DTL.PERMIT_RANK = permit.NAME_RANK_A;
                    rA42_TRAINEES_PASS_DTL.PERMIT_NAME = permit.NAME_EMP_A;
                    rA42_TRAINEES_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                    rA42_TRAINEES_PASS_DTL.REJECTED = false;
                    rA42_TRAINEES_PASS_DTL.STATUS = false;
                    rA42_TRAINEES_PASS_DTL.ISOPENED = true;
                }
                //this is security officer section
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {

                    //security officer should redirect the complete request to permits cell for printing 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_TRAINEES_PASS_DTL);

                    }
                    else
                    {
                        rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_TRAINEES_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_TRAINEES_PASS_DTL.AUTHO_RANK = permit.NAME_RANK_A;
                    rA42_TRAINEES_PASS_DTL.AUTHO_NAME = permit.NAME_EMP_A;
                    rA42_TRAINEES_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_TRAINEES_PASS_DTL.REJECTED = false;
                    rA42_TRAINEES_PASS_DTL.STATUS = true;
                    rA42_TRAINEES_PASS_DTL.ISOPENED = true;
                    rA42_TRAINEES_PASS_DTL.BARCODE = rA42_TRAINEES_PASS_DTL.BARCODE;


                }
                rA42_TRAINEES_PASS_DTL.SERVICE_NUMBER = currentUser;
                rA42_TRAINEES_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_TRAINEES_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_TRAINEES_PASS_DTL.CARD_FOR_CODE = 10;
                rA42_TRAINEES_PASS_DTL.CRD_BY = currentUser;
                rA42_TRAINEES_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_TRAINEES_PASS_DTL.UPD_BY = currentUser;
                rA42_TRAINEES_PASS_DTL.UPD_DT = DateTime.Now;
                db.RA42_TRAINEES_PASS_DTL.Add(rA42_TRAINEES_PASS_DTL);
                db.SaveChanges();
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE;
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
                        //set foreach loop to upload multiple files 
                        int c = 0;
                        foreach (HttpPostedFileBase file in files)
                        {
                            // Verify that the user selected a file
                            if (file != null && file.ContentLength > 0)
                            {
                                // extract only the filename with extention
                                string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                                string extension = Path.GetExtension(file.FileName);


                                //check extention of the permit 
                                if (general.CheckFileType(file.FileName))
                                {


                                    fileName = "FileNO" + c + "_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);


                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE,
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
                                    //if there is somthing wrong with one file, it will delete all uploaded documents this is security procedurs 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_TRAINEES_PASS_DTL);
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
            return View(rA42_TRAINEES_PASS_DTL);
        }

        //this view to proccess any request 
        public ActionResult Edit(int? id)
		{
			if (id == null)
			{
                return NotFound();
			}
			RA42_TRAINEES_PASS_DTL rA42_TRAINEES_PASS_DTL = db.RA42_TRAINEES_PASS_DTL.Find(id);
			if (rA42_TRAINEES_PASS_DTL == null)
			{
                return NotFound();
			}
            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_TRAINEES_PASS_DTL.IDENTITY_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_TRAINEES_PASS_DTL.GENDER_ID);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_TRAINEES_PASS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_TRAINEES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_TRAINEES_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_TRAINEES_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if the current user is equal 1 or less than 1 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    }


                }

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_TRAINEES_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
            else
            {
                //get identities in arabic
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_TRAINEES_PASS_DTL.IDENTITY_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_TRAINEES_PASS_DTL.GENDER_ID);
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_TRAINEES_PASS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_TRAINEES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_TRAINEES_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person in arabic for this kind of permit 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_TRAINEES_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //if current user is 1 or ess than one show error message 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                    }

                }

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_TRAINEES_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
            //get documents types for this kind of permit to check missing documents 
            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_TRAINEES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_TRAINEES_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_TRAINEES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_TRAINEES_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            //get selected zones and gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get selected documents
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get comments of the request
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            //get personal image
            ViewBag.PERSONAL_IMAGE = rA42_TRAINEES_PASS_DTL.PERSONAL_IMAGE;
            //get status of the permit 
            ViewBag.STATUS = rA42_TRAINEES_PASS_DTL.STATUS;
            return View(rA42_TRAINEES_PASS_DTL);
		}

		// POST new edited data
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(RA42_TRAINEES_PASS_DTL rA42_TRAINEES_PASS_DTL, FormCollection form, string COMMENT, int[] ZONE, int[] SUB_ZONE, 
            int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[]
            files, HttpPostedFileBase PERSONAL_IMAGE)
		{
            //check if record id is in the db table 
            var v = db.RA42_TRAINEES_PASS_DTL.Where(a => a.TRAINEE_PASS_CODE == rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE).FirstOrDefault();

            var currentUserInfo = (new UserInfo()).getUserInfo();

            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == v.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == v.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == v.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == v.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            //get selected files and zones and gates
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get comments 
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == v.TRAINEE_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            //get personal image 
            ViewBag.PERSONAL_IMAGE = v.PERSONAL_IMAGE;
            //get status of permit 
            ViewBag.STATUS = v.STATUS;
            //get documents of this kind of permit to compare between uploaded documenst and missing documenst 
            ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_TRAINEES_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_TRAINEES_PASS_DTL.IDENTITY_CODE);
                //get zoones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == v.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && z.CARD_FOR_CODE == v.CARD_FOR_CODE && d.FORCE_ID == v.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

                //get autho person for this kind of permit in english 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == v.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if current user is less than or equal 1
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_TRAINEES_PASS_DTL);
                    }

                }

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == v.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
            else
            {
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_TRAINEES_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_TRAINEES_PASS_DTL.IDENTITY_CODE);
                //get zoones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == v.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && z.CARD_FOR_CODE == v.CARD_FOR_CODE && d.FORCE_ID == v.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

                //get autho person for this kind of permit in arabic 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == v.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error if the current user is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_TRAINEES_PASS_DTL);
                    }

                }

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == v.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
            if (ModelState.IsValid)
			{
                //check personal image 
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


                            //get file extention of image 
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
                                    return View(rA42_TRAINEES_PASS_DTL);
                                }

                                rA42_TRAINEES_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //if format not supported show error
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_TRAINEES_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                //add new selected zones and gates
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE;
                        rA42_ZONE_MASTER_MST.ZONE_CODE = ZONE[i];
                        rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                        rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                        db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                        db.SaveChanges();
                        //continue;
                    }

                }
                //add new selected documents
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

                                    fileName = "FileNO" + c + "_2_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);
                                    //add new file 
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE,
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
                                    //delete all uploaded files if there is any problem with one file, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return RedirectToAction("Edit", new { id = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE });
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
                    rA42_COMMENT.PASS_ROW_CODE = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE;
                    rA42_COMMENT.CRD_BY = currentUser;
                    rA42_COMMENT.CRD_DT = DateTime.Now;
                    rA42_COMMENT.COMMENT = COMMENT;
                    db.RA42_COMMENTS_MST.Add(rA42_COMMENT);


                }

                if(rA42_TRAINEES_PASS_DTL.PERSONAL_IMAGE != null)
                {
                    v.PERSONAL_IMAGE = rA42_TRAINEES_PASS_DTL.PERSONAL_IMAGE;
                }
                else
                {
                    v.PERSONAL_IMAGE = v.PERSONAL_IMAGE;
                }


                //this section is for developer
                if (form["approvebtn"] != null && ViewBag.DEVELOPER == true)
                {
                    v.WORKFLOW_RESPO_CODE = rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE;

                    v.SERVICE_NUMBER = v.SERVICE_NUMBER;
                    v.RESPONSIBLE = rA42_TRAINEES_PASS_DTL.RESPONSIBLE;
                    v.TRAINEE_NAME = rA42_TRAINEES_PASS_DTL.TRAINEE_NAME;
                    v.MAJOR = rA42_TRAINEES_PASS_DTL.MAJOR;
                    v.PHONE_NUMBER = rA42_TRAINEES_PASS_DTL.PHONE_NUMBER;
                    v.GSM = rA42_TRAINEES_PASS_DTL.GSM;
                    v.ID_CARD_NUMBER = rA42_TRAINEES_PASS_DTL.ID_CARD_NUMBER;
                    v.PURPOSE_OF_PASS = rA42_TRAINEES_PASS_DTL.PURPOSE_OF_PASS;
                    v.REMARKS = rA42_TRAINEES_PASS_DTL.REMARKS;
                    v.DATE_FROM = rA42_TRAINEES_PASS_DTL.DATE_FROM;
                    v.DATE_TO = rA42_TRAINEES_PASS_DTL.DATE_TO;
                    v.IDENTITY_CODE = rA42_TRAINEES_PASS_DTL.IDENTITY_CODE;
                    v.GENDER_ID = rA42_TRAINEES_PASS_DTL.GENDER_ID;
                    v.ACCESS_TYPE_CODE = v.ACCESS_TYPE_CODE;
                    v.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    v.STATION_CODE = v.STATION_CODE;
                    v.CARD_FOR_CODE = v.CARD_FOR_CODE;
                    v.APPROVAL_SN = v.APPROVAL_SN;
                    v.APPROVAL_NAME = v.APPROVAL_NAME;
                    v.APPROVAL_RANK = v.APPROVAL_RANK;
                    v.APPROVAL_APPROVISION_DATE = v.APPROVAL_APPROVISION_DATE;
                    v.AUTHO_SN = v.AUTHO_SN;
                    v.AUTHO_NAME = v.AUTHO_NAME;
                    v.AUTHO_RANK = v.AUTHO_RANK;
                    v.AUTHO_APPROVISION_DATE = v.AUTHO_APPROVISION_DATE;
                    v.PERMIT_SN = v.PERMIT_SN;
                    v.PERMIT_NAME = v.PERMIT_NAME;
                    v.PERMIT_RANK = v.PERMIT_RANK;
                    v.PERMIT_APPROVISION_DATE = v.PERMIT_APPROVISION_DATE;
                    v.BARCODE = v.BARCODE;
                    v.CRD_BY = v.CRD_BY;
                    v.CRD_DT = v.CRD_DT;
                    v.UPD_BY = currentUser;
                    v.UPD_DT = DateTime.Now;
                    v.REJECTED = false;
                    v.STATUS = v.STATUS;
                    v.ISOPENED = true;
                    v.ISPRINTED = false;
                    db.Entry(v).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Allpasses");
                }

                //this section is for applicant 
                if (form["approvebtn"] != null && (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11)))
                {
                    v.SERVICE_NUMBER = v.SERVICE_NUMBER;
                    v.RESPONSIBLE = rA42_TRAINEES_PASS_DTL.RESPONSIBLE;
                    v.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    v.TRAINEE_NAME = rA42_TRAINEES_PASS_DTL.TRAINEE_NAME;
                    v.MAJOR = rA42_TRAINEES_PASS_DTL.MAJOR;
                    v.PHONE_NUMBER = rA42_TRAINEES_PASS_DTL.PHONE_NUMBER;
                    v.GSM = rA42_TRAINEES_PASS_DTL.GSM;
                    v.ID_CARD_NUMBER = rA42_TRAINEES_PASS_DTL.ID_CARD_NUMBER;
                    v.PURPOSE_OF_PASS = rA42_TRAINEES_PASS_DTL.PURPOSE_OF_PASS;
                    v.REMARKS = rA42_TRAINEES_PASS_DTL.REMARKS;
                    v.DATE_FROM = rA42_TRAINEES_PASS_DTL.DATE_FROM;
                    v.DATE_TO = rA42_TRAINEES_PASS_DTL.DATE_TO;
                    v.IDENTITY_CODE = rA42_TRAINEES_PASS_DTL.IDENTITY_CODE;
                    v.GENDER_ID = rA42_TRAINEES_PASS_DTL.GENDER_ID;
                    v.ACCESS_TYPE_CODE = v.ACCESS_TYPE_CODE;
                    v.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    v.STATION_CODE = v.STATION_CODE;
                    v.CARD_FOR_CODE = v.CARD_FOR_CODE;
                    v.BARCODE = v.BARCODE;
                    v.CRD_BY = v.CRD_BY;
                    v.CRD_DT = v.CRD_DT;
                    v.UPD_BY = currentUser;
                    v.UPD_DT = DateTime.Now;
                    v.REJECTED = false;
                    v.STATUS = v.STATUS;
                    v.ISOPENED = false;
                    v.ISPRINTED = v.ISPRINTED;
                    db.Entry(v).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Index", "MyPasses");
                }
                //this section is for autho person 
                if (form["approvebtn"] != null && WORKFLOWID == 2)
                {
                    //he should redirect the request to permits cell
                    var v2 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    if (v2 == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return RedirectToAction("Edit", new { id = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE });

                    }
                    v.SERVICE_NUMBER = v.SERVICE_NUMBER;
                    v.RESPONSIBLE = rA42_TRAINEES_PASS_DTL.RESPONSIBLE;
                    v.WORKFLOW_RESPO_CODE = v2.WORKFLOW_RESPO_CODE;
                    v.TRAINEE_NAME = rA42_TRAINEES_PASS_DTL.TRAINEE_NAME;
                    v.MAJOR = rA42_TRAINEES_PASS_DTL.MAJOR;
                    v.PHONE_NUMBER = rA42_TRAINEES_PASS_DTL.PHONE_NUMBER;
                    v.GSM = rA42_TRAINEES_PASS_DTL.GSM;
                    v.ID_CARD_NUMBER = rA42_TRAINEES_PASS_DTL.ID_CARD_NUMBER;
                    v.PURPOSE_OF_PASS = rA42_TRAINEES_PASS_DTL.PURPOSE_OF_PASS;
                    v.REMARKS = rA42_TRAINEES_PASS_DTL.REMARKS;
                    v.DATE_FROM = rA42_TRAINEES_PASS_DTL.DATE_FROM;
                    v.DATE_TO = rA42_TRAINEES_PASS_DTL.DATE_TO;
                    v.IDENTITY_CODE = rA42_TRAINEES_PASS_DTL.IDENTITY_CODE;
                    v.GENDER_ID = rA42_TRAINEES_PASS_DTL.GENDER_ID;
                    v.ACCESS_TYPE_CODE = v.ACCESS_TYPE_CODE;
                    v.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    v.STATION_CODE = v.STATION_CODE;
                    v.CARD_FOR_CODE = v.CARD_FOR_CODE;
                    v.APPROVAL_SN = currentUserInfo["user_sno"];
                    v.APPROVAL_NAME = currentUserInfo["user_name_ar"];
                    v.APPROVAL_RANK = currentUserInfo["user_rank_ar"];
                    v.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    v.AUTHO_SN = v.AUTHO_SN;
                    v.AUTHO_NAME = v.AUTHO_NAME;
                    v.AUTHO_RANK = v.AUTHO_RANK;
                    v.AUTHO_APPROVISION_DATE = v.AUTHO_APPROVISION_DATE;
                    v.PERMIT_SN = v.PERMIT_SN;
                    v.PERMIT_NAME = v.PERMIT_NAME;
                    v.PERMIT_RANK = v.PERMIT_RANK;
                    v.PERMIT_APPROVISION_DATE = v.PERMIT_APPROVISION_DATE;
                    v.BARCODE = v.BARCODE;
                    v.CRD_BY = v.CRD_BY;
                    v.CRD_DT = v.CRD_DT;
                    v.UPD_BY = currentUser;
                    v.UPD_DT = DateTime.Now;
                    v.REJECTED = false;
                    v.STATUS = v.STATUS;
                    v.ISOPENED = true;
                    v.ISPRINTED = v.ISPRINTED;
                    db.Entry(v).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Authopasses");
                }


                //this section is for permits cell 
                if (form["approvebtn"] != null && WORKFLOWID == 3)
                {
                    //if request status true, that means this request is complete and no need to redirect it to security officer
                    if (v.STATUS == true)
                    {
                        v.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    }
                    else
                    {
                        var v2 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                        if (v2 == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE });

                        }
                        v.WORKFLOW_RESPO_CODE = v2.WORKFLOW_RESPO_CODE;

                    }
                    v.SERVICE_NUMBER = v.SERVICE_NUMBER;
                    v.RESPONSIBLE = rA42_TRAINEES_PASS_DTL.RESPONSIBLE;
                    v.TRAINEE_NAME = rA42_TRAINEES_PASS_DTL.TRAINEE_NAME;
                    v.MAJOR = rA42_TRAINEES_PASS_DTL.MAJOR;
                    v.PHONE_NUMBER = rA42_TRAINEES_PASS_DTL.PHONE_NUMBER;
                    v.GSM = rA42_TRAINEES_PASS_DTL.GSM;
                    v.ID_CARD_NUMBER = rA42_TRAINEES_PASS_DTL.ID_CARD_NUMBER;
                    v.PURPOSE_OF_PASS = rA42_TRAINEES_PASS_DTL.PURPOSE_OF_PASS;
                    v.REMARKS = rA42_TRAINEES_PASS_DTL.REMARKS;
                    v.DATE_FROM = rA42_TRAINEES_PASS_DTL.DATE_FROM;
                    v.DATE_TO = rA42_TRAINEES_PASS_DTL.DATE_TO;
                    v.IDENTITY_CODE = rA42_TRAINEES_PASS_DTL.IDENTITY_CODE;
                    v.GENDER_ID = rA42_TRAINEES_PASS_DTL.GENDER_ID;
                    v.ACCESS_TYPE_CODE = v.ACCESS_TYPE_CODE;
                    v.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    v.STATION_CODE = v.STATION_CODE;
                    v.CARD_FOR_CODE = v.CARD_FOR_CODE;
                    v.APPROVAL_SN = v.APPROVAL_SN;
                    v.APPROVAL_NAME = v.APPROVAL_NAME;
                    v.APPROVAL_RANK = v.APPROVAL_RANK;
                    v.APPROVAL_APPROVISION_DATE = v.APPROVAL_APPROVISION_DATE;
                    v.AUTHO_SN = v.AUTHO_SN;
                    v.AUTHO_NAME = v.AUTHO_NAME;
                    v.AUTHO_RANK = v.AUTHO_RANK;
                    v.AUTHO_APPROVISION_DATE = v.AUTHO_APPROVISION_DATE;
                    v.PERMIT_SN = currentUserInfo["user_sno"];
                    v.PERMIT_NAME = currentUserInfo["user_name_ar"];
                    v.PERMIT_RANK = currentUserInfo["user_rank_ar"];
                    v.PERMIT_APPROVISION_DATE = DateTime.Now;
                    v.BARCODE = v.BARCODE;
                    v.CRD_BY = v.CRD_BY;
                    v.CRD_DT = v.CRD_DT;
                    v.UPD_BY = currentUser;
                    v.UPD_DT = DateTime.Now;
                    v.REJECTED = false;
                    v.STATUS = v.STATUS;
                    v.ISOPENED = true;
                    v.ISPRINTED = false;
                    db.Entry(v).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Newpasses");
                }

               //this is security officr section 
                if (form["approvebtn"] != null && WORKFLOWID == 4)
                {
                    //he should redirect the permit after complete to permits cell 
                    var v2 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    if (v2 == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return RedirectToAction("Edit", new { id = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE });

                    }
                    v.SERVICE_NUMBER = v.SERVICE_NUMBER;
                    v.RESPONSIBLE = rA42_TRAINEES_PASS_DTL.RESPONSIBLE;
                    v.WORKFLOW_RESPO_CODE = v2.WORKFLOW_RESPO_CODE;
                    v.TRAINEE_NAME = rA42_TRAINEES_PASS_DTL.TRAINEE_NAME;
                    v.MAJOR = rA42_TRAINEES_PASS_DTL.MAJOR;
                    v.PHONE_NUMBER = rA42_TRAINEES_PASS_DTL.PHONE_NUMBER;
                    v.GSM = rA42_TRAINEES_PASS_DTL.GSM;
                    v.ID_CARD_NUMBER = rA42_TRAINEES_PASS_DTL.ID_CARD_NUMBER;
                    v.PURPOSE_OF_PASS = rA42_TRAINEES_PASS_DTL.PURPOSE_OF_PASS;
                    v.REMARKS = rA42_TRAINEES_PASS_DTL.REMARKS;
                    v.DATE_FROM = rA42_TRAINEES_PASS_DTL.DATE_FROM;
                    v.DATE_TO = rA42_TRAINEES_PASS_DTL.DATE_TO;
                    v.IDENTITY_CODE = rA42_TRAINEES_PASS_DTL.IDENTITY_CODE;
                    v.GENDER_ID = rA42_TRAINEES_PASS_DTL.GENDER_ID;
                    v.ACCESS_TYPE_CODE = v.ACCESS_TYPE_CODE;
                    v.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    v.STATION_CODE = v.STATION_CODE;
                    v.CARD_FOR_CODE = v.CARD_FOR_CODE;
                    v.APPROVAL_SN = v.APPROVAL_SN;
                    v.APPROVAL_NAME = v.APPROVAL_NAME;
                    v.APPROVAL_RANK = v.APPROVAL_RANK;
                    v.APPROVAL_APPROVISION_DATE = v.APPROVAL_APPROVISION_DATE;
                    v.PERMIT_SN = v.PERMIT_SN;
                    v.PERMIT_NAME = v.PERMIT_NAME;
                    v.PERMIT_RANK = v.PERMIT_RANK;
                    v.PERMIT_APPROVISION_DATE = v.PERMIT_APPROVISION_DATE;
                    v.AUTHO_SN = currentUserInfo["user_sno"];
                    v.AUTHO_NAME = currentUserInfo["user_name_ar"];
                    v.AUTHO_RANK = currentUserInfo["user_rank_ar"];
                    v.AUTHO_APPROVISION_DATE = DateTime.Now;
                    v.BARCODE = rA42_TRAINEES_PASS_DTL.BARCODE;
                    v.CRD_BY = v.CRD_BY;
                    v.CRD_DT = v.CRD_DT;
                    v.UPD_BY = currentUser;
                    v.UPD_DT = DateTime.Now;
                    v.REJECTED = false;
                    v.ISOPENED = v.ISOPENED;
                    v.ISPRINTED = v.ISPRINTED;
                    v.STATUS = true;
                    db.Entry(v).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Newpasses");
                }
                //this section for autho person to reject any recivied  request 
                if (form["rejectbtn"] != null && WORKFLOWID == 2)
                {
                    var v2 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    if (v2 == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return RedirectToAction("Edit", new { id = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE });

                    }

                    v.SERVICE_NUMBER = v.SERVICE_NUMBER;
                    v.RESPONSIBLE = rA42_TRAINEES_PASS_DTL.RESPONSIBLE;
                    v.WORKFLOW_RESPO_CODE = v2.WORKFLOW_RESPO_CODE;
                    v.TRAINEE_NAME = rA42_TRAINEES_PASS_DTL.TRAINEE_NAME;
                    v.MAJOR = rA42_TRAINEES_PASS_DTL.MAJOR;
                    v.PHONE_NUMBER = rA42_TRAINEES_PASS_DTL.PHONE_NUMBER;
                    v.GSM = rA42_TRAINEES_PASS_DTL.GSM;
                    v.ID_CARD_NUMBER = rA42_TRAINEES_PASS_DTL.ID_CARD_NUMBER;
                    v.PURPOSE_OF_PASS = rA42_TRAINEES_PASS_DTL.PURPOSE_OF_PASS;
                    v.REMARKS = rA42_TRAINEES_PASS_DTL.REMARKS;
                    v.DATE_FROM = rA42_TRAINEES_PASS_DTL.DATE_FROM;
                    v.DATE_TO = rA42_TRAINEES_PASS_DTL.DATE_TO;
                    v.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    v.IDENTITY_CODE = rA42_TRAINEES_PASS_DTL.IDENTITY_CODE;
                    v.GENDER_ID = rA42_TRAINEES_PASS_DTL.GENDER_ID;
                    v.ACCESS_TYPE_CODE = v.ACCESS_TYPE_CODE;
                    v.STATION_CODE = v.STATION_CODE;
                    v.CARD_FOR_CODE = v.CARD_FOR_CODE;
                    v.BARCODE = v.BARCODE;
                    v.CRD_BY = v.CRD_BY;
                    v.CRD_DT = v.CRD_DT;
                    v.UPD_BY = currentUser;
                    v.UPD_DT = DateTime.Now;
                    v.REJECTED = true;
                    v.ISOPENED = false;
                    v.STATUS = v.STATUS;
                    db.Entry(v).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Authopasses");
                }
                //this section is for permits cell to reject the reuqest and return it to autho person 
                if (form["rejectbtn"] != null && WORKFLOWID == 3)
                {
                    var v2 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == v.APPROVAL_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    if (v2 == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return RedirectToAction("Edit", new { id = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE });

                    }

                    v.SERVICE_NUMBER = v.SERVICE_NUMBER;
                    v.RESPONSIBLE = rA42_TRAINEES_PASS_DTL.RESPONSIBLE;
                    v.WORKFLOW_RESPO_CODE = v2.WORKFLOW_RESPO_CODE;
                    v.TRAINEE_NAME = rA42_TRAINEES_PASS_DTL.TRAINEE_NAME;
                    v.MAJOR = rA42_TRAINEES_PASS_DTL.MAJOR;
                    v.PHONE_NUMBER = rA42_TRAINEES_PASS_DTL.PHONE_NUMBER;
                    v.GSM = rA42_TRAINEES_PASS_DTL.GSM;
                    v.ID_CARD_NUMBER = rA42_TRAINEES_PASS_DTL.ID_CARD_NUMBER;
                    v.PURPOSE_OF_PASS = rA42_TRAINEES_PASS_DTL.PURPOSE_OF_PASS;
                    v.REMARKS = rA42_TRAINEES_PASS_DTL.REMARKS;
                    v.DATE_FROM = rA42_TRAINEES_PASS_DTL.DATE_FROM;
                    v.DATE_TO = rA42_TRAINEES_PASS_DTL.DATE_TO;
                    v.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    v.IDENTITY_CODE = rA42_TRAINEES_PASS_DTL.IDENTITY_CODE;
                    v.GENDER_ID = rA42_TRAINEES_PASS_DTL.GENDER_ID;
                    v.ACCESS_TYPE_CODE = v.ACCESS_TYPE_CODE;
                    v.STATION_CODE = v.STATION_CODE;
                    v.CARD_FOR_CODE = v.CARD_FOR_CODE;
                    v.BARCODE = v.BARCODE;
                    v.CRD_BY = v.CRD_BY;
                    v.CRD_DT = v.CRD_DT;
                    v.UPD_BY = currentUser;
                    v.UPD_DT = DateTime.Now;
                    v.REJECTED = true;
                    v.ISOPENED = v.ISOPENED;
                    v.STATUS = v.STATUS;
                    db.Entry(v).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Newpasses");
                }
                //this section is for security officer to reject any request and retun it to permits cell
                if (form["rejectbtn"] != null && WORKFLOWID == 4)
                {
                    var v2 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == v.AUTHO_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    if (v2 == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return RedirectToAction("Edit", new { id = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE });

                    }

                    v.SERVICE_NUMBER = v.SERVICE_NUMBER;
                    v.RESPONSIBLE = rA42_TRAINEES_PASS_DTL.RESPONSIBLE;
                    v.WORKFLOW_RESPO_CODE = v2.WORKFLOW_RESPO_CODE;
                    v.TRAINEE_NAME = rA42_TRAINEES_PASS_DTL.TRAINEE_NAME;
                    v.MAJOR = rA42_TRAINEES_PASS_DTL.MAJOR;
                    v.PHONE_NUMBER = rA42_TRAINEES_PASS_DTL.PHONE_NUMBER;
                    v.GSM = rA42_TRAINEES_PASS_DTL.GSM;
                    v.ID_CARD_NUMBER = rA42_TRAINEES_PASS_DTL.ID_CARD_NUMBER;
                    v.PURPOSE_OF_PASS = rA42_TRAINEES_PASS_DTL.PURPOSE_OF_PASS;
                    v.REMARKS = rA42_TRAINEES_PASS_DTL.REMARKS;
                    v.DATE_FROM = rA42_TRAINEES_PASS_DTL.DATE_FROM;
                    v.DATE_TO = rA42_TRAINEES_PASS_DTL.DATE_TO;
                    v.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    v.IDENTITY_CODE = rA42_TRAINEES_PASS_DTL.IDENTITY_CODE;
                    v.GENDER_ID = rA42_TRAINEES_PASS_DTL.GENDER_ID;
                    v.ACCESS_TYPE_CODE = v.ACCESS_TYPE_CODE;
                    v.STATION_CODE = v.STATION_CODE;
                    v.CARD_FOR_CODE = v.CARD_FOR_CODE;
                    v.BARCODE = v.BARCODE;
                    v.CRD_BY = v.CRD_BY;
                    v.CRD_DT = v.CRD_DT;
                    v.UPD_BY = currentUser;
                    v.UPD_DT = DateTime.Now;
                    v.REJECTED = true;
                    v.ISOPENED = v.ISOPENED;
                    v.STATUS = v.STATUS;
                    db.Entry(v).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Newpasses");
                }
            }
            AddToast(new Toast("",
                GetResourcesValue("error_update_message"),
                "red"));
			return View(rA42_TRAINEES_PASS_DTL);
		}

        //renew permit
        public ActionResult Renew(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            RA42_TRAINEES_PASS_DTL rA42_TRAINEES_PASS_DTL = db.RA42_TRAINEES_PASS_DTL.Find(id);
            if (rA42_TRAINEES_PASS_DTL == null)
            {
                return NotFound();
            }
            if (rA42_TRAINEES_PASS_DTL.DATE_TO != null)
            {

                string date = rA42_TRAINEES_PASS_DTL.CheckDate(rA42_TRAINEES_PASS_DTL.DATE_TO.Value);
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
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_TRAINEES_PASS_DTL.IDENTITY_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_TRAINEES_PASS_DTL.GENDER_ID);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_TRAINEES_PASS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_TRAINEES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_TRAINEES_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_TRAINEES_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if the current user is equal 1 or less than 1 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    }


                }

               
            }
            else
            {
                //get identities in arabic
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_TRAINEES_PASS_DTL.IDENTITY_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_TRAINEES_PASS_DTL.GENDER_ID);
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_TRAINEES_PASS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_TRAINEES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_TRAINEES_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person in arabic for this kind of permit 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_TRAINEES_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //if current user is 1 or ess than one show error message 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                    }

                }

               
            }
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (rA42_TRAINEES_PASS_DTL.STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get documents types for this kind of permit to check missing documents 
            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_TRAINEES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_TRAINEES_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_TRAINEES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_TRAINEES_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            //get selected zones and gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get selected documents
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get personal image
            ViewBag.PERSONAL_IMAGE = rA42_TRAINEES_PASS_DTL.PERSONAL_IMAGE;
            rA42_TRAINEES_PASS_DTL.DATE_FROM = null;
            rA42_TRAINEES_PASS_DTL.DATE_TO = null;
            return View(rA42_TRAINEES_PASS_DTL);
        }


        // POST new edited data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Renew(RA42_TRAINEES_PASS_DTL rA42_TRAINEES_PASS_DTL,  int[] ZONE, int[] SUB_ZONE,
            int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[]
            files, HttpPostedFileBase PERSONAL_IMAGE,int TRAINEE_ID)
        {
            //check if record id is in the db table 
            var v = db.RA42_TRAINEES_PASS_DTL.Where(a => a.TRAINEE_PASS_CODE == TRAINEE_ID).FirstOrDefault();
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (v.STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            var currentUserInfo = (new UserInfo()).getUserInfo();

            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == v.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == v.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == v.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == v.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            //get selected files and zones and gates
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == TRAINEE_ID && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == TRAINEE_ID && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
           
            //get personal image 
            ViewBag.PERSONAL_IMAGE = v.PERSONAL_IMAGE;
          
            //get documents of this kind of permit to compare between uploaded documenst and missing documenst 
            ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_TRAINEES_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_TRAINEES_PASS_DTL.IDENTITY_CODE);
                //get zoones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == v.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && z.CARD_FOR_CODE == v.CARD_FOR_CODE && d.FORCE_ID == v.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

                //get autho person for this kind of permit in english 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == v.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if current user is less than or equal 1
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(v);
                    }

                }

                
            }
            else
            {
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_TRAINEES_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_TRAINEES_PASS_DTL.IDENTITY_CODE);
                //get zoones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == v.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && z.CARD_FOR_CODE == v.CARD_FOR_CODE && d.FORCE_ID == v.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

                //get autho person for this kind of permit in arabic 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == v.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error if the current user is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(v);
                    }

                }

               
            }
            if (ModelState.IsValid)
            {
                //check personal image 
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


                            //get file extention of image 
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
                                    return View(v);
                                }

                                rA42_TRAINEES_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //if format not supported show error
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(v);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
               

                if (rA42_TRAINEES_PASS_DTL.PERSONAL_IMAGE != null)
                {
                    rA42_TRAINEES_PASS_DTL.PERSONAL_IMAGE = rA42_TRAINEES_PASS_DTL.PERSONAL_IMAGE;
                }
                else
                {
                    rA42_TRAINEES_PASS_DTL.PERSONAL_IMAGE = v.PERSONAL_IMAGE;
                }


                //get current user military info from api
                User permit = null;
                Task<User> callTask2 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask2.Wait();
                permit = callTask2.Result;

                //this section is for applicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                {

                    rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_TRAINEES_PASS_DTL.REJECTED = false;
                    rA42_TRAINEES_PASS_DTL.STATUS = false;
                    rA42_TRAINEES_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person 
                if (WORKFLOWID == 2)
                {
                    //he should redirect the request to permits cell 
                    var w = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).FirstOrDefault();
                    if (w == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(v);

                    }
                    rA42_TRAINEES_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_TRAINEES_PASS_DTL.APPROVAL_RANK = permit.NAME_RANK_A;
                    rA42_TRAINEES_PASS_DTL.APPROVAL_NAME = permit.NAME_EMP_A;
                    rA42_TRAINEES_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE = w.WORKFLOW_RESPO_CODE;
                    rA42_TRAINEES_PASS_DTL.REJECTED = false;
                    rA42_TRAINEES_PASS_DTL.STATUS = false;
                    rA42_TRAINEES_PASS_DTL.ISOPENED = true;
                }

                //permits cell section
                if (WORKFLOWID == 3)
                {
                    //they should redirect the permit to the securiy officer to approve the permit 
                    var w = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (w == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(v);

                    }
                    else
                    {
                        rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE = w.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_TRAINEES_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_TRAINEES_PASS_DTL.PERMIT_RANK = permit.NAME_RANK_A;
                    rA42_TRAINEES_PASS_DTL.PERMIT_NAME = permit.NAME_EMP_A;
                    rA42_TRAINEES_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                    rA42_TRAINEES_PASS_DTL.REJECTED = false;
                    rA42_TRAINEES_PASS_DTL.STATUS = false;
                    rA42_TRAINEES_PASS_DTL.ISOPENED = true;
                }
                //this is security officer section
                if (WORKFLOWID == 4)
                {

                    //security officer should redirect the complete request to permits cell for printing 
                    var w = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (w == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(v);

                    }
                    else
                    {
                        rA42_TRAINEES_PASS_DTL.WORKFLOW_RESPO_CODE = w.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_TRAINEES_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_TRAINEES_PASS_DTL.AUTHO_RANK = permit.NAME_RANK_A;
                    rA42_TRAINEES_PASS_DTL.AUTHO_NAME = permit.NAME_EMP_A;
                    rA42_TRAINEES_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_TRAINEES_PASS_DTL.REJECTED = false;
                    rA42_TRAINEES_PASS_DTL.STATUS = true;
                    rA42_TRAINEES_PASS_DTL.ISOPENED = true;
                    rA42_TRAINEES_PASS_DTL.BARCODE = rA42_TRAINEES_PASS_DTL.BARCODE;


                }
                rA42_TRAINEES_PASS_DTL.SERVICE_NUMBER = currentUser;
                rA42_TRAINEES_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_TRAINEES_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_TRAINEES_PASS_DTL.CARD_FOR_CODE = v.CARD_FOR_CODE;
                rA42_TRAINEES_PASS_DTL.CRD_BY = currentUser;
                rA42_TRAINEES_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_TRAINEES_PASS_DTL.UPD_BY = currentUser;
                rA42_TRAINEES_PASS_DTL.UPD_DT = DateTime.Now;
                db.RA42_TRAINEES_PASS_DTL.Add(rA42_TRAINEES_PASS_DTL);
                db.SaveChanges();
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE;
                        rA42_ZONE_MASTER_MST.ZONE_CODE = ZONE[i];
                        rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                        rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                        db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                        db.SaveChanges();
                        //continue;
                    }

                }
                //add previues zones to new zone
                var zones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == TRAINEE_ID && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
                foreach (var zone in zones)
                {
                    rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE;
                    rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                    rA42_ZONE_MASTER_MST.ZONE_CODE = zone.ZONE_CODE.Value;
                    rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                    rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                    rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = zone.ZONE_SUB_CODE;
                    db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                    db.SaveChanges();
                }

                //add selected documents
                if (files != null)
                {

                    try
                    {
                        //set foreach loop to upload multiple files 
                        int c = 0;
                        foreach (HttpPostedFileBase file in files)
                        {
                            // Verify that the user selected a file
                            if (file != null && file.ContentLength > 0)
                            {
                                // extract only the filename with extention
                                string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                                string extension = Path.GetExtension(file.FileName);


                                //check extention of the permit 
                                if (general.CheckFileType(file.FileName))
                                {


                                    fileName = "FileNO" + c + "_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);


                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE,
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
                                    //if there is somthing wrong with one file, it will delete all uploaded documents this is security procedurs 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                    return View(v);
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
                var selected_files = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == TRAINEE_ID && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
                foreach (var file in selected_files)
                {
                    //add new file
                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                    {
                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                        ACCESS_ROW_CODE = rA42_TRAINEES_PASS_DTL.TRAINEE_PASS_CODE,
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
            AddToast(new Toast("",
                GetResourcesValue("error_update_message"),
                "red"));
            return View(v);
        }
        // delet view to delete any request 
        public ActionResult Delete(int? id)
		{
			if (id == null)
			{
                return NotFound();
			}
			RA42_TRAINEES_PASS_DTL rA42_TRAINEES_PASS_DTL = db.RA42_TRAINEES_PASS_DTL.Find(id);
			if (rA42_TRAINEES_PASS_DTL == null)
			{
                return NotFound();
            }

            //check if current user has authority to open comments view for this permit 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_TRAINEES_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_TRAINEES_PASS_DTL.RESPONSIBLE != currentUser)
                {
                    if (rA42_TRAINEES_PASS_DTL.ISOPENED != true)
                    {
                        if (rA42_TRAINEES_PASS_DTL.STATUS == true)
                        {
                            if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                            {

                                return NotFound();
                            }
                        }
                    }
                }
            }
            else
            {
                if (rA42_TRAINEES_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_TRAINEES_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    //if (ViewBag.RESPO_STATE != rA42_TRAINEES_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                    //{
                    //    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    //    {
                    //        return NotFound();
                    //    }
                    //}
                }
            }
            //get selected zones and gates and documents of the request
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();

            return View(rA42_TRAINEES_PASS_DTL);
		}

		// confirm deleteing 
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public ActionResult DeleteConfirmed(int id)
		{
           

            var general_data = db.RA42_TRAINEES_PASS_DTL.Where(a => a.TRAINEE_PASS_CODE == id).FirstOrDefault();

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
        //this function to delete any uploaded documents of any request 
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
        //this function to delete personal image of any request 
        [HttpPost]
        public JsonResult DeleteImage(int id)
        {
            bool result = false;
            var general_data = db.RA42_TRAINEES_PASS_DTL.Where(a => a.TRAINEE_PASS_CODE == id).FirstOrDefault();

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
        //this function is to delete any gate or zone of any request 
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

        //this is print function to print card or temprory permit or save them 
        [HttpPost]
        public JsonResult PrintById(int id, string type)
        {

            bool result = false;
            var general_data = db.RA42_TRAINEES_PASS_DTL.Where(a => a.TRAINEE_PASS_CODE == id).FirstOrDefault();

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
