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
using System.Security.Policy;

namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    //this controller is responsible for any family permit 
	public class FamilypassController : Controller
	{
        //get db connection
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        //initialaiz toast 
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number
        private string currentUser = (new UserInfo()).getSNO();
        //initilaize generalfunction class to use some functions
        private GeneralFunctions general = new GeneralFunctions();
        private int STATION_CODE = 0;
        private int WORKFLOWID = 0;
        private int RESPO_CODE = 0;
        private int FORCE_ID = 0;
        //this permit has number 4 as ACCESS_TYPE_CODE in RA42_ACCESS_TYPE_MST table 
        private int ACCESS_TYPE_CODE = 4;
        //get string title from resources files of this permit 
        private string title = Resources.Passes.ResourceManager.GetString("Family_pass" + "_" + "ar");

        public FamilypassController() {
            ViewBag.Managepasses = "Managepasses";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Familypass";
            //set icon for this permit 
			 ViewBag.controllerIconClass = "fa fa-users";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Passes.ResourceManager.GetString("Family_pass" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;
            //check if current user has authority of this kind of permit and get his WORKFLOW type 
            var v = Task.Run(async () => await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefaultAsync()).Result;
            if (v != null)
            {
                if (v.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && v.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false)
                {
                    //get id of yser
                    ViewBag.RESPO_ID = v.WORKFLOW_RESPO_CODE;
                    //get WORKFLOW id of this user
                    ViewBag.RESPO_STATE = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID;
                    RESPO_CODE = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE;
                    WORKFLOWID = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value;
                    FORCE_ID = v.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.FORCE_ID.Value;
                    //define his station_code 
                    STATION_CODE = v.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE.Value;
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


        //this view is to show some tabs and links 
        public ActionResult Index()
        {
          
            return View();
        }
        //comments view to let user add comments 
        public ActionResult Comments(int? id)
        {
            ViewBag.activetab = "Comments";

            if (id == null)
            {
                return NotFound();
            }
            RA42_FAMILY_PASS_DTL rA42_FAMILY_PASS_DTL = db.RA42_FAMILY_PASS_DTL.Find(id);
            if (rA42_FAMILY_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if current user has authority to open comments view for this permit 
            if (ViewBag.RESPO_STATE <=1 )
            {
                if (rA42_FAMILY_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_FAMILY_PASS_DTL.RESPONSIBLE !=currentUser)
                {
                    if (rA42_FAMILY_PASS_DTL.ISOPENED != true)
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
                if (rA42_FAMILY_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_FAMILY_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != rA42_FAMILY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
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
            return View(rA42_FAMILY_PASS_DTL);
        }

       //post new comments 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Comments(RA42_FAMILY_PASS_DTL rA42_FAMILY_PASS_DTL, string COMMENT)
        {
            ViewBag.activetab = "Comments";
            var general_data = db.RA42_FAMILY_PASS_DTL.Where(a => a.FAMILY_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE).FirstOrDefault();
            
            //add comments
            if (COMMENT.Length > 0)
            {
                RA42_COMMENTS_MST rA42_COMMENT = new RA42_COMMENTS_MST();
                rA42_COMMENT.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_COMMENT.PASS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE;
                rA42_COMMENT.CRD_BY = currentUser;
                rA42_COMMENT.CRD_DT = DateTime.Now;
                rA42_COMMENT.COMMENT = COMMENT;
                db.RA42_COMMENTS_MST.Add(rA42_COMMENT);
                db.SaveChanges();
                AddToast(new Toast("",
                  GetResourcesValue("add_comment_success"),
                  "green"));

            }
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            return View(rA42_FAMILY_PASS_DTL);


        }
        //get subzones of any zone as jsonarray
        //this function working probably, but subzones not activated until now 
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

        // this view to show links of creation type of permits like (searcg, supercreate)
        public ActionResult Choosecreatetype()
        {
            ViewBag.activetab = "Privatepass";
            return View();
        }
       

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


            var empList = db.RA42_FAMILY_PASS_DTL.Where(a => a.WORKFLOW_RESPO_CODE != null).Select(a => new
            {
                FAMILY_CODE = a.FAMILY_CODE,
                SERVICE_NUMBER = (a.SERVICE_NUMBER != null ? a.SERVICE_NUMBER : " "),
                CIVIL_NUMBER = (a.CIVIL_NUMBER != null ? a.CIVIL_NUMBER : " "),
                PERSONAL_IMAGE = a.PERSONAL_IMAGE,
                RANK_A = (a.HOST_RANK_A != null ? a.HOST_RANK_A : " "),
                RANK_E = (a.HOST_RANK_E != null ? a.HOST_RANK_E : " "),
                H_NAME_A = (a.HOST_NAME_A != null ? a.HOST_NAME_A : " "),
                H_NAME_E = (a.HOST_NAME_E != null ? a.HOST_NAME_E : " "),
                NAME_A = (a.NAME_A != null ? a.NAME_A : " "),
                NAME_E = (a.NAME_E != null ? a.NAME_E : " "),
                PHONE_NUMBER = (a.PHONE_NUMBER != null ? a.PHONE_NUMBER : " "),
                GSM = (a.GSM != null ? a.GSM : " "),
                PURPOSE_OF_PASS = (a.PURPOSE_OF_PASS != null ? a.PURPOSE_OF_PASS : " "),
                PROFESSION_A = (a.PROFESSION_A != null ? a.PROFESSION_A : " "),
                PROFESSION_E = (a.PROFESSION_E != null ? a.PROFESSION_E : " "),
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
                COMMENTS = a.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == a.FAMILY_CODE).Count()


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
                Where(x => x.SERVICE_NUMBER.Contains(searchValue) || x.CIVIL_NUMBER.Contains(searchValue) 
                || x.NAME_A.Contains(searchValue) || x.NAME_E.Contains(searchValue) || x.H_NAME_A.Contains(searchValue) 
                || x.H_NAME_E.Contains(searchValue) || x.RANK_A.Contains(searchValue) || x.RANK_E.Contains(searchValue) 
                || x.PROFESSION_A.Contains(searchValue) || x.PROFESSION_E.Contains(searchValue) 
                || x.PHONE_NUMBER.Contains(searchValue) || x.GSM.Contains(searchValue) 
                || x.PURPOSE_OF_PASS.Contains(searchValue) || x.STATION_A == searchValue).ToList();
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

            var empList = db.RA42_FAMILY_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID && a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ISPRINTED == true && a.WORKFLOW_RESPO_CODE !=null).Select(a => new
            {
                FAMILY_CODE = a.FAMILY_CODE,
                SERVICE_NUMBER = (a.SERVICE_NUMBER != null ? a.SERVICE_NUMBER : " "),
                CIVIL_NUMBER = (a.CIVIL_NUMBER != null ? a.CIVIL_NUMBER : " "),
                PERSONAL_IMAGE = a.PERSONAL_IMAGE,
                RANK_A = (a.HOST_RANK_A != null ? a.HOST_RANK_A : " "),
                RANK_E = (a.HOST_RANK_E != null ? a.HOST_RANK_E : " "),
                H_NAME_A = (a.HOST_NAME_A != null ? a.HOST_NAME_A : " "),
                H_NAME_E = (a.HOST_NAME_E != null ? a.HOST_NAME_E : " "),
                NAME_A = (a.NAME_A != null ? a.NAME_A : " "),
                NAME_E = (a.NAME_E != null ? a.NAME_E : " "),
                PHONE_NUMBER = (a.PHONE_NUMBER != null ? a.PHONE_NUMBER : " "),
                GSM = (a.GSM != null ? a.GSM : " "),
                PURPOSE_OF_PASS = (a.PURPOSE_OF_PASS != null ? a.PURPOSE_OF_PASS : " "),
                PROFESSION_A = (a.PROFESSION_A != null ? a.PROFESSION_A : " "),
                PROFESSION_E = (a.PROFESSION_E != null ? a.PROFESSION_E : " "),
                STATION_CODE = a.STATION_CODE,
                PASS_TYPE = a.RA42_PASS_TYPE_MST.PASS_TYPE,
                STATION_A = a.RA42_STATIONS_MST.STATION_NAME_A,
                STATION_E = a.RA42_STATIONS_MST.STATION_NAME_E,
                RESPONSEPLE_NAME = a.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                RESPONSEPLE_NAME_E = a.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E,
                STEP_NAME = a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME,
                STEP_NAME_E = a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E,
                STATUS = a.STATUS,
                RETURNED = a.RETURNED,
                ACCESS_TYPE_CODE = a.ACCESS_TYPE_CODE.Value,
                REJECTED = a.REJECTED,
                DLT_STS = a.DLT_STS,
                ISPRINTED = a.ISPRINTED,
                DATE_FROM = a.DATE_FROM,
                DATE_TO = a.DATE_TO,
                COMMENTS = a.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == a.FAMILY_CODE).Count()


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
                Where(x => x.SERVICE_NUMBER.Contains(searchValue) || x.CIVIL_NUMBER.Contains(searchValue) || x.NAME_A.Contains(searchValue) 
                | x.GSM.Contains(searchValue) 
                ).ToList();
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
        //this view for admin and developer 
        public ActionResult Allpasses()
        {

            ViewBag.activetab = "Allpasses";
            return View();
        }
        //this view for autho person (المنسق الأمني)
        public ActionResult Authopasses()
        {
            ViewBag.activetab = "Authopasses";
            var rA42_FAMILY_PASS_DTL = db.RA42_FAMILY_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.DLT_STS != true).OrderByDescending(a => a.FAMILY_CODE);
            return View(rA42_FAMILY_PASS_DTL.ToList());
        }
        //this view for permits cell and security officer
        public async Task<ActionResult> Newpasses()
        {
            ViewBag.activetab = "Newpasses";
            var rA42_FAMILY_PASS_DTL = await db.RA42_FAMILY_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID && a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.STATUS !=true).OrderByDescending(a => a.FAMILY_CODE).ToListAsync();
            return View(rA42_FAMILY_PASS_DTL);
        }
        public async Task<ActionResult> ToPrint()
        {
            ViewBag.activetab = "ToPrint";
            var rA42_FAMILY_PASS_DTL = await db.RA42_FAMILY_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID && a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.STATUS == true && a.ISPRINTED !=true).OrderByDescending(a => a.FAMILY_CODE).ToListAsync();
            return View(rA42_FAMILY_PASS_DTL);
        }
        //this view is for permits cell to follow printed permits 
        public ActionResult Printed()
        {
            ViewBag.activetab = "Printed";
            return View();
        }
      
        //this function to get relatives as json array
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;

            if (Language.GetCurrentLang() == "en") {
                var value = (from a in db.RA42_MEMBERS_DTL
                             join b in db.RA42_RELATIVE_TYPE_MST on a.RELATIVE_TYPE_CODE equals b.RELATIVE_TYPE_CODE
                             join c in db.RA42_IDENTITY_MST on a.IDENTITY_CODE equals c.IDENTITY_CODE
                             where a.MEMBER_CODE == Id
                             select new
                             {

                                 a.FULL_NAME,
                                 a.CIVIL_NUMBER,
                                 a.PASSPORT_NUMBER,
                                 a.PHONE_NUMBER,
                                 GENDER = a.RA42_GENDERS_MST.GENDER_E,
                                 RELATIVE_TYPE = b.RELATIVE_TYPE_E,
                                 IDENTITY = c.IDENTITY_TYPE_E,
                                 REMARKS = a.REMARKS



                             }).FirstOrDefault();
                //Add JsonRequest behavior to allow retrieving states over http get
                return Json(value, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var value = (from a in db.RA42_MEMBERS_DTL
                             join b in db.RA42_RELATIVE_TYPE_MST on a.RELATIVE_TYPE_CODE equals b.RELATIVE_TYPE_CODE
                             join c in db.RA42_IDENTITY_MST on a.IDENTITY_CODE equals c.IDENTITY_CODE
                             where a.MEMBER_CODE == Id
                             select new
                             {

                                 a.FULL_NAME,
                                 a.CIVIL_NUMBER,
                                 a.PASSPORT_NUMBER,
                                 a.PHONE_NUMBER,
                                 GENDER = a.RA42_GENDERS_MST.GENDER_A,
                                 RELATIVE_TYPE = b.RELATIVE_TYPE,
                                 IDENTITY = c.IDENTITY_TYPE_A,
                                 REMARKS = a.REMARKS



                             }).FirstOrDefault();
                //Add JsonRequest behavior to allow retrieving states over http get
                return Json(value, JsonRequestBehavior.AllowGet);
            }
        }

       
        //this is card view to print card and temprory permit 
        [HttpGet]
        public ActionResult Card(int? id)
        {
            ViewBag.activetab = "card";

            if (id == null)
            {
                return NotFound();
            }
            RA42_FAMILY_PASS_DTL rA42_FAMILY_PASS_DTL = db.RA42_FAMILY_PASS_DTL.Find(id);
            if (rA42_FAMILY_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if current user has authority tomopen this page 
            if (ViewBag.RESPO_STATE != rA42_FAMILY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
            {
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    return NotFound();
                }
            }
            //get zones of this permit 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get relatives of this permit 
            ViewBag.GetRelativs = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.ACCESS_ROW_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE && a.DLT_STS != true).ToList();
            //get documents of this permit 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_FAMILY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToList();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_FAMILY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToList();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }


            return View(rA42_FAMILY_PASS_DTL);
        }

        [HttpPost]
        public ActionResult Card(string CheckPrinted, int TRANSACTION_TYPE_CODE, string TRANSACTION_REMARKS, HttpPostedFileBase RECEIPT, RA42_FAMILY_PASS_DTL _FAMILY_PASS_DTL)
        {
            ViewBag.activetab = "card";

            
            RA42_FAMILY_PASS_DTL rA42_FAMILY_PASS_DTL = db.RA42_FAMILY_PASS_DTL.Find(_FAMILY_PASS_DTL.FAMILY_CODE);
            if (rA42_FAMILY_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if current user has authority tomopen this page 
            if (ViewBag.RESPO_STATE != rA42_FAMILY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
            {
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    return NotFound();
                }
            }
            //get zones of this permit 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == _FAMILY_PASS_DTL.FAMILY_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get relatives of this permit 
            ViewBag.GetRelativs = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.ACCESS_ROW_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE && a.DLT_STS != true).ToList();
            //get documents of this permit 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == _FAMILY_PASS_DTL.FAMILY_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_FAMILY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToList();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_FAMILY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToList();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }

            RA42_TRANSACTION_DTL rA42_TRANSACTION_DTL = new RA42_TRANSACTION_DTL();
            rA42_TRANSACTION_DTL.ACCESS_ROW_CODE = _FAMILY_PASS_DTL.FAMILY_CODE;
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
                            return View(rA42_FAMILY_PASS_DTL);
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
            rA42_FAMILY_PASS_DTL.UPD_BY = currentUser;
            rA42_FAMILY_PASS_DTL.UPD_DT = DateTime.Now;
            rA42_FAMILY_PASS_DTL.ISDELIVERED = false;
            db.SaveChanges();
            TempData["Success"] = "تم تحديث المعاملة بنجاح";
            if (CheckPrinted.Equals("Printed"))
            {
                var deletePrinted = db.RA42_PRINT_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.PASS_ROW_CODE ==
                _FAMILY_PASS_DTL.FAMILY_CODE).ToList();
                if (deletePrinted.Count > 0)
                {
                    foreach (var item in deletePrinted)
                    {
                        item.DLT_STS = true;
                        db.SaveChanges();
                    }
                }
            }

            return View(rA42_FAMILY_PASS_DTL);
        }
        // this is details view 
        public ActionResult Details(int? id)
		{
            ViewBag.activetab = "details";

            if (id == null)
            {
                return NotFound();
            }
            //check if id in the RA42_FAMILY_PASS_DTL table 
            RA42_FAMILY_PASS_DTL rA42_FAMILY_PASS_DTL = db.RA42_FAMILY_PASS_DTL.Find(id);
            if (rA42_FAMILY_PASS_DTL == null)
            {
                return NotFound();
            }
            //check authority to open this kind of permit 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_FAMILY_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_FAMILY_PASS_DTL.RESPONSIBLE !=currentUser)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }
           
            //get zones of permit 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get relatives of the permit 
            ViewBag.GetRelativs = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.ACCESS_ROW_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE && a.DLT_STS != true).ToList();
            //get documents of the permit 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                    
            return View(rA42_FAMILY_PASS_DTL);
		}

		// this is create view to let any one create family permit for his family 
		public ActionResult Create()
		{
            ViewBag.activetab = "Create";
            ViewBag.Service_No = currentUser;

            var url = Url.RequestContext.RouteData.Values["id"];
            //int unit = 0;
            //get station name 
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                var station = int.Parse(id.ToString());
                STATION_CODE = station;
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
                //get unit code to use it to detrmine the autho persone who will recive the permit(الركن المختص، قائد الجناح أو السرب)
                //هذا الخيار يحدد العبارة التي ستظهر للمسخدم (الركن المختص أم قائد الجناح او السرب)
                ViewBag.Get_Station_Code = id.ToString();
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

                    ViewBag.HQ_UNIT = STATION_CODE.ToString();
                
                //get unit code to use it to detrmine the autho persone who will recive the permit(الركن المختص، قائد الجناح أو السرب)
                //هذا الخيار يحدد العبارة التي ستظهر للمسخدم (الركن المختص أم قائد الجناح او السرب)
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
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
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
                //get genders in english
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get relatives types in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E");

                //get gates only in english 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME_E");
                    //get autho person workflow id for this kind of permit in english for this station
                    var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (STATION_CODE != 26)
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
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                //get permits types codes in arabic 
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get zones and gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 3 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE+ " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get relatives in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE");


                //get gates only in arabic 
               // ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME");
                    //get autho person workflow id for this kind of permit in arabic for this station
                    var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (STATION_CODE != 26)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View();

                    }
                }


               
            }

            return View();
		}

		//post data
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Create(RA42_FAMILY_PASS_DTL rA42_FAMILY_PASS_DTL,int[] RELATIVE_TYPES,HttpPostedFileBase[] RELATIVE_IMAGE, int[] IDENTITY_TYPES,int[] GENDER_TYPES,string[] FULL_NAME,string[] CIVIL_NUM, string[] PASSPORT_NUMBER, string[] PHONE_NUMBER_M, string[] REMARKS_LIST,
            int[] FILE_TYPES,string[] FILE_TYPES_TEXT, int[] ZONE, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
		{
            ViewBag.activetab = "Create";
            ViewBag.Service_No = currentUser;
            //get STATION_CODE form session 
            var url = Url.RequestContext.RouteData.Values["id"];
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == id).FirstOrDefault();
                if (check_unit != null)
                {
                    //get unit-code from id
                    var station = int.Parse(id.ToString());
                    STATION_CODE = station;
                    ViewBag.HQ_UNIT = id;
                    //get security caveates
                    ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();
                    //get station name 
                    if (Language.GetCurrentLang() == "en")
                    {
                        ViewBag.STATION_NAME = "in " + check_unit.STATION_NAME_E;
                    }
                    else
                    {
                        ViewBag.STATION_NAME = "في " + check_unit.STATION_NAME_A;

                    }

                    ViewBag.Get_Station_Code = id.ToString();
                    FORCE_ID = check_unit.FORCE_ID.Value;

                }
            }
            else
            {
              
                    //get security caveates 
                    ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();
                    var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();
                    //get station name 
                    if (Language.GetCurrentLang() == "en")
                    {
                        ViewBag.STATION_NAME = "in " + check_unit.STATION_NAME_E;
                    }
                    else
                    {
                        ViewBag.STATION_NAME = "في " + check_unit.STATION_NAME_A;

                    }
                

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
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E",rA42_FAMILY_PASS_DTL.IDENTITY_CODE);
                //get permit types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E",rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                //get permits types codes in arabic 
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
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
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E",rA42_FAMILY_PASS_DTL.GENDER_ID);
                //get relatives in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E",rA42_FAMILY_PASS_DTL.RELATIVE_TYPE_CODE);
                //get gates only in english 
               // ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME_E");
                //get workflow id for autho person (المنسق الأمني) in english for this kind of permit 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME",rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE);
                 if (WORKFLOW_RESPO.Count == 0)
                 {

                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (STATION_CODE != 26)
                        {
                            //show error message if ther is no autho person 
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_FAMILY_PASS_DTL);
                        }
                    }
                    

                 }
                
            }
            else
            {
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_FAMILY_PASS_DTL.IDENTITY_CODE);
                //get permit types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE",rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                //get permits types codes in arabic 
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
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
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A",rA42_FAMILY_PASS_DTL.GENDER_ID);
                //get relatives in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE",rA42_FAMILY_PASS_DTL.RELATIVE_TYPE_CODE);
                //get gates only in arabic 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME");
                //get autho person in arabic 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME",rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE);

                    if (WORKFLOW_RESPO.Count == 0)
                    {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (STATION_CODE != 26)
                        {
                            //show error message if ther is no autho person 
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_FAMILY_PASS_DTL);
                        }
                    }

                }


                
            }


            //if no autho person return erro not set responding person 
            if (rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE == null)
            {
                if (ViewBag.RESPO_STATE <= 1)
                {
                    if (STATION_CODE != 26)
                    {
                        //show error message if ther is no autho person 
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_FAMILY_PASS_DTL);
                    }
                }

            }
           

            if (ModelState.IsValid)
			{
                //check personal image if the user upload it  
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


                            //check image file extention
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {

                                fileName = "Profile_4_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);
                                if (check != true)
                                {
                                    AddToast(new Toast("",
                                   GetResourcesValue("error_update_message"),
                                   "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_FAMILY_PASS_DTL);
                                }
                                rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //throw error if image file extention not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_FAMILY_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                //get current user details from api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;
                //set current user details as host person 
                rA42_FAMILY_PASS_DTL.SERVICE_NUMBER = rA42_FAMILY_PASS_DTL.SERVICE_NUMBER.ToUpper();
                rA42_FAMILY_PASS_DTL.HOST_RANK_A = user.NAME_RANK_A;
                rA42_FAMILY_PASS_DTL.HOST_RANK_E = user.NAME_RANK_E;
                rA42_FAMILY_PASS_DTL.HOST_NAME_A = user.NAME_EMP_A;
                rA42_FAMILY_PASS_DTL.HOST_NAME_E = user.NAME_EMP_E;
                rA42_FAMILY_PASS_DTL.PROFESSION_A = user.NAME_TRADE_A;
                rA42_FAMILY_PASS_DTL.PROFESSION_E = user.NAME_TRADE_E;
                if (!string.IsNullOrEmpty(rA42_FAMILY_PASS_DTL.UNIT_A))
                {
                    rA42_FAMILY_PASS_DTL.UNIT_A = rA42_FAMILY_PASS_DTL.UNIT_A;

                }
                else
                {
                    rA42_FAMILY_PASS_DTL.UNIT_A = user.NAME_UNIT_A;
                }
                rA42_FAMILY_PASS_DTL.UNIT_E = user.NAME_UNIT_E;
                rA42_FAMILY_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_FAMILY_PASS_DTL.CARD_FOR_CODE = 1;
                //insert rest of data 
                rA42_FAMILY_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_FAMILY_PASS_DTL.CRD_BY = currentUser;
                rA42_FAMILY_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_FAMILY_PASS_DTL.UPD_BY = currentUser;
                rA42_FAMILY_PASS_DTL.UPD_DT = DateTime.Now;
                //get current user details from api 
                User permit = null;
                Task<User> callTask2 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask2.Wait();
                permit = callTask2.Result;
                //this section for applicant, workflow id <= 1
                if ((WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11)) || ViewBag.NOT_RELATED_STATION == true)
                {
                    if (STATION_CODE == 26)
                    {
                        //he should redirect this request to the permits cell 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_FAMILY_PASS_DTL);

                        }
                        else
                        {
                            rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }

                        rA42_FAMILY_PASS_DTL.APPROVAL_SN = currentUser;
                        rA42_FAMILY_PASS_DTL.APPROVAL_RANK = permit.NAME_RANK_A;
                        rA42_FAMILY_PASS_DTL.APPROVAL_NAME = permit.NAME_EMP_A;
                        rA42_FAMILY_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;

                        rA42_FAMILY_PASS_DTL.REJECTED = false;
                        rA42_FAMILY_PASS_DTL.STATUS = false;
                        rA42_FAMILY_PASS_DTL.ISOPENED = true;
                    }
                    else
                    {
                        rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE;
                        rA42_FAMILY_PASS_DTL.REJECTED = false;
                        rA42_FAMILY_PASS_DTL.STATUS = false;
                        rA42_FAMILY_PASS_DTL.ISOPENED = false;
                    }
                }
                //this section is for autho person (المنسق الأمني) his workflow id is 2
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect this request to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_FAMILY_PASS_DTL);

                    }
                    else
                    {
                        rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_FAMILY_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_FAMILY_PASS_DTL.APPROVAL_RANK = permit.NAME_RANK_A;
                    rA42_FAMILY_PASS_DTL.APPROVAL_NAME = permit.NAME_EMP_A;
                    rA42_FAMILY_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;

                    rA42_FAMILY_PASS_DTL.REJECTED = false;
                    rA42_FAMILY_PASS_DTL.STATUS = false;
                    rA42_FAMILY_PASS_DTL.ISOPENED = true;
                }
                //this section is for permits cell,
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    if (STATION_CODE == 26)
                    {
                        //after the security officer create permit, the request will be completed and should be redirected to the permits cell
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_FAMILY_PASS_DTL);

                        }
                        else
                        {
                            rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }

                        rA42_FAMILY_PASS_DTL.PERMIT_SN = currentUser;
                        rA42_FAMILY_PASS_DTL.PERMIT_RANK = permit.NAME_RANK_A;
                        rA42_FAMILY_PASS_DTL.PERMIT_NAME = permit.NAME_EMP_A;
                        rA42_FAMILY_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                        rA42_FAMILY_PASS_DTL.REJECTED = false;
                        rA42_FAMILY_PASS_DTL.STATUS = true;
                        rA42_FAMILY_PASS_DTL.ISOPENED = true;
                        string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                        rA42_FAMILY_PASS_DTL.BARCODE = barcode;
                    }
                    else
                    {
                        //he should redirect the request to the security officer
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_FAMILY_PASS_DTL);

                        }
                        else
                        {
                            rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }

                        rA42_FAMILY_PASS_DTL.PERMIT_SN = currentUser;
                        rA42_FAMILY_PASS_DTL.PERMIT_RANK = permit.NAME_RANK_A;
                        rA42_FAMILY_PASS_DTL.PERMIT_NAME = permit.NAME_EMP_A;
                        rA42_FAMILY_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;

                        rA42_FAMILY_PASS_DTL.REJECTED = false;
                        rA42_FAMILY_PASS_DTL.STATUS = false;
                        rA42_FAMILY_PASS_DTL.ISOPENED = true;
                    }
                }
                //this section is for security officer
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //after the security officer create permit, the request will be completed and should be redirected to the permits cell
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_FAMILY_PASS_DTL);

                    }
                    else
                    {
                        rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_FAMILY_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_FAMILY_PASS_DTL.AUTHO_RANK = permit.NAME_RANK_A;
                    rA42_FAMILY_PASS_DTL.AUTHO_NAME = permit.NAME_EMP_A;
                    rA42_FAMILY_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_FAMILY_PASS_DTL.REJECTED = false;
                    rA42_FAMILY_PASS_DTL.STATUS = true;
                    rA42_FAMILY_PASS_DTL.ISOPENED = true;
                    rA42_FAMILY_PASS_DTL.BARCODE = rA42_FAMILY_PASS_DTL.BARCODE;
                }
                db.RA42_FAMILY_PASS_DTL.Add(rA42_FAMILY_PASS_DTL);
				db.SaveChanges();
                //add relatives 
                RA42_MEMBERS_DTL rA42_MEMBERS_DTL = new RA42_MEMBERS_DTL();
                if (IDENTITY_TYPES != null && !IDENTITY_TYPES.Contains(0))
                {
                    try
                    {
                        for (int i = 0; i < RELATIVE_TYPES.Length; i++)
                        {

                            //create barcode for every relative
                            string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                            rA42_MEMBERS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                            rA42_MEMBERS_DTL.ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE;
                            rA42_MEMBERS_DTL.CIVIL_NUMBER = CIVIL_NUM[i];
                            rA42_MEMBERS_DTL.FULL_NAME = FULL_NAME[i];
                            rA42_MEMBERS_DTL.PHONE_NUMBER = PHONE_NUMBER_M[i];
                            rA42_MEMBERS_DTL.PASSPORT_NUMBER = PASSPORT_NUMBER[i];
                            rA42_MEMBERS_DTL.RELATIVE_TYPE_CODE = RELATIVE_TYPES[i];
                            rA42_MEMBERS_DTL.IDENTITY_CODE = IDENTITY_TYPES[i];
                            rA42_MEMBERS_DTL.GENDER_ID = GENDER_TYPES[i];
                            rA42_MEMBERS_DTL.REMARKS = REMARKS_LIST[i];

                            try
                            {

                                // Verify that the user selected a file
                                if (RELATIVE_IMAGE[i].ContentLength > 0)
                                {
                                    // extract only the filename with extention
                                    string fileName = Path.GetFileNameWithoutExtension(RELATIVE_IMAGE[i].FileName);
                                    string extension = Path.GetExtension(RELATIVE_IMAGE[i].FileName);


                                    //check the extention of the image file 
                                    if (general.CheckPersonalImage(RELATIVE_IMAGE[i].FileName))
                                    {

                                        fileName = "Relative_Profile_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                                        // store the file inside ~/Files/Profiles/ folder
                                        bool check = general.ResizeImage(RELATIVE_IMAGE[i], fileName);

                                        if (check != true)
                                        {
                                            AddToast(new Toast("",
                                           GetResourcesValue("error_update_message"),
                                           "red"));
                                            TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                            return View(rA42_FAMILY_PASS_DTL);
                                        }

                                        rA42_MEMBERS_DTL.PERSONAL_IMAGE = fileName;


                                    }
                                    else
                                    {
                                        //if format not supported, show error message 
                                        AddToast(new Toast("",
                                        GetResourcesValue("error_update_message"),
                                        "red"));
                                        TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                        return View(rA42_FAMILY_PASS_DTL);
                                    }
                                }
                            }

                            catch (Exception ex)
                            {
                                ex.GetBaseException();
                            }

                            rA42_MEMBERS_DTL.CRD_BY = currentUser;
                            rA42_MEMBERS_DTL.CRD_DT = DateTime.Now;
                            rA42_MEMBERS_DTL.UPD_BY = currentUser;
                            rA42_MEMBERS_DTL.UPD_DT = DateTime.Now;
                            db.RA42_MEMBERS_DTL.Add(rA42_MEMBERS_DTL);
                            db.SaveChanges();
                            //continue;
                        }



                    }
                    catch (IndexOutOfRangeException ex)
                    {
                        TempData["Erorr"] = ex.GetBaseException().Message + RELATIVE_TYPES.Length + " - " + IDENTITY_TYPES.Length + " - " +
                            GENDER_TYPES.Length + " - " + FULL_NAME.Length + " - " + REMARKS_LIST.Length + " - " + CIVIL_NUM.Length;
                        return View(rA42_FAMILY_PASS_DTL);
                    }
                }
                //add gates to the RA42_ZONE_MASTER_MST table  
                RA42_ZONE_MASTER_MST rA42_ZONE_MASTER_MST = new RA42_ZONE_MASTER_MST();
                if (ZONE != null && !ZONE.Contains(0))
                {
                    for (int i = 0; i < ZONE.Length; i++)
                    {

                       
                        rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE;
                        rA42_ZONE_MASTER_MST.ZONE_CODE = ZONE[i];
                        rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = 0;
                        rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                        rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                        db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                        db.SaveChanges();
                        //continue;
                    }

                }
                //upload documents if not null 
                if (files != null)
                {
                   //create foreach loop to deal with multiple files
                    try
                    {
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

                                    fileName = "FileNO" + c + "_4_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);
                                    //add new file 
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE,
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
                                    //as security, delete whole files uploaded if there is any corrupted file
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_FAMILY_PASS_DTL);
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
               
                
                    return RedirectToAction("Index", "MyPasses");

                
            }


            AddToast(new Toast("",
                GetResourcesValue("error_create_message"),
                "red"));
			return View(rA42_FAMILY_PASS_DTL);
		}

        // this view is for authorized person who have authority to add permits for others such as permits cell, security officer and request introducer
        public ActionResult Supercreate()
        {
            ViewBag.activetab = "Supercreate";
            ViewBag.Service_No = currentUser;
            //get unit-code from session 
            var url = Url.RequestContext.RouteData.Values["id"];
            //int unit = 0;

            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get unit code to use it to detrmine the autho persone who will recive the permit(الركن المختص، قائد الجناح أو السرب)
                //هذا الخيار يحدد العبارة التي ستظهر للمسخدم (الركن المختص أم قائد الجناح او السرب)
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
               
                    var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();

                    if (Language.GetCurrentLang() == "en")
                    {
                        ViewBag.STATION_NAME = "in " + check_unit.STATION_NAME_E;
                    }
                    else
                    {
                        ViewBag.STATION_NAME = "في " + check_unit.STATION_NAME_A;

                    }


                
                //get unit code to use it to detrmine the autho persone who will recive the permit(الركن المختص، قائد الجناح أو السرب)
                //هذا الخيار يحدد العبارة التي ستظهر للمسخدم (الركن المختص أم قائد الجناح او السرب)
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
                //get permit types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
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
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get relatives types in english
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E");
                //get gates types in english 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE=="G"), "ZONE_CODE", "ZONE_NAME_E");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME_E");
                //get autho person workflow id for this kind of permit in english for this station
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (STATION_CODE != 26)
                    {
                        if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        }
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
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
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
                //get genders in arabic
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get relatives types in arabic
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE");
                //get gates types in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME");
                ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME");
                //get autho person workflow id for this kind of permit in arabic for this station
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (STATION_CODE != 26)
                    {
                        if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        }
                    }
                }


            }
            return View();
        }

       //post data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Supercreate(RA42_FAMILY_PASS_DTL rA42_FAMILY_PASS_DTL, int[] RELATIVE_TYPES, HttpPostedFileBase[] RELATIVE_IMAGE, int[] IDENTITY_TYPES, int[] GENDER_TYPES, string[] FULL_NAME, string[] CIVIL_NUM, string[] PASSPORT_NUMBER, string[] PHONE_NUMBER_M
            , string[] REMARKS_LIST, int[] FILE_TYPES,string[] FILE_TYPES_TEXT, int[] ZONE, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "Supercreate";
            ViewBag.Service_No = currentUser;
            //get unit-code form session
            var url = Url.RequestContext.RouteData.Values["id"];
            //if session not null continue
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get unit code to use it to detrmine the autho persone who will recive the permit(الركن المختص، قائد الجناح أو السرب)
                //هذا الخيار يحدد العبارة التي ستظهر للمسخدم (الركن المختص أم قائد الجناح او السرب)
                ViewBag.Get_Station_Code = id.ToString();
                var station = int.Parse(id.ToString());
                STATION_CODE = station;
                //get station naem, this option will shoen as title 
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
               
                    var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();

                    if (Language.GetCurrentLang() == "en")
                    {
                        ViewBag.STATION_NAME = "in " + check_unit.STATION_NAME_E;
                    }
                    else
                    {
                        ViewBag.STATION_NAME = "في " + check_unit.STATION_NAME_A;

                    }


                //get unit code to use it to detrmine the autho persone who will recive the permit(الركن المختص، قائد الجناح أو السرب)
                //هذا الخيار يحدد العبارة التي ستظهر للمسخدم (الركن المختص أم قائد الجناح او السرب)
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
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E",rA42_FAMILY_PASS_DTL.IDENTITY_CODE);
                //get permits type
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E",rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
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
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E",rA42_FAMILY_PASS_DTL.GENDER_ID);
                //get relatives types in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_FAMILY_PASS_DTL.RELATIVE_TYPE_CODE);
                //get gates types in english 
               // ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME_E");
               // ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME_E");
                //get autho person workflow id for this kind of permit in english for this station
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (STATION_CODE != 26)
                    {
                        if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                        {
                            //show error message if ther is no autho person 
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_FAMILY_PASS_DTL);
                        }
                    }

                }



            }
            else
            {
               //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A",rA42_FAMILY_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE",rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get zones and gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE ==3 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A",rA42_FAMILY_PASS_DTL.GENDER_ID);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_FAMILY_PASS_DTL.RELATIVE_TYPE_CODE);
                //get gates in arabic 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME");
                //get autho person workflow id for this kind of permit in arabic for this station
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (STATION_CODE != 26)
                    {
                        if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                        {
                            //show error for user if there is no autho person setted
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_FAMILY_PASS_DTL);
                        }
                    }
                }



            }

            //get host details from api
            User user = null;
            Task<User> callTask = Task.Run(
                () => (new UserInfo()).getUserInfoFromAPI(rA42_FAMILY_PASS_DTL.SERVICE_NUMBER.ToUpper())
                );
            callTask.Wait();
            user = callTask.Result;



            if (user != null && ModelState.IsValid)
            {
                //check if user upload personal image 
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


                            //get image extention
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {

                                fileName = "Profile_4_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);
                                if (check != true)
                                {
                                    AddToast(new Toast("",
                                   GetResourcesValue("error_update_message"),
                                   "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_FAMILY_PASS_DTL);
                                }
                                rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE = fileName;



                            }
                            else
                            { 
                                //show error message if image not match supported extention
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_FAMILY_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                //get host details from api
                rA42_FAMILY_PASS_DTL.SERVICE_NUMBER = rA42_FAMILY_PASS_DTL.SERVICE_NUMBER.ToUpper();
                rA42_FAMILY_PASS_DTL.RESPONSIBLE = currentUser;
                rA42_FAMILY_PASS_DTL.HOST_RANK_A = user.NAME_RANK_A;
                rA42_FAMILY_PASS_DTL.HOST_RANK_E = user.NAME_RANK_E;
                rA42_FAMILY_PASS_DTL.HOST_NAME_A = user.NAME_EMP_A;
                rA42_FAMILY_PASS_DTL.HOST_NAME_E = user.NAME_EMP_E;
                rA42_FAMILY_PASS_DTL.PROFESSION_A = user.NAME_TRADE_A;
                rA42_FAMILY_PASS_DTL.PROFESSION_E = user.NAME_TRADE_E;
                if (!string.IsNullOrEmpty(rA42_FAMILY_PASS_DTL.UNIT_A))
                {
                    rA42_FAMILY_PASS_DTL.UNIT_A = rA42_FAMILY_PASS_DTL.UNIT_A;

                }
                else
                {
                    rA42_FAMILY_PASS_DTL.UNIT_A = user.NAME_UNIT_A;
                }
                rA42_FAMILY_PASS_DTL.UNIT_E = user.NAME_UNIT_E;
                rA42_FAMILY_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_FAMILY_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_FAMILY_PASS_DTL.CARD_FOR_CODE = 1;
                //get current user details from api 
                User permit = null;
                Task<User> callTask2 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask2.Wait();
                permit = callTask2.Result;
                //this section for applicant, workflow id <= 1
                if ((WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11)) || ViewBag.NOT_RELATED_STATION == true)
                {
                    if (STATION_CODE == 26)
                    {
                        //he should redirect this request to the permits cell 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_FAMILY_PASS_DTL);

                        }
                        else
                        {
                            rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }

                        rA42_FAMILY_PASS_DTL.APPROVAL_SN = currentUser;
                        rA42_FAMILY_PASS_DTL.APPROVAL_RANK = permit.NAME_RANK_A;
                        rA42_FAMILY_PASS_DTL.APPROVAL_NAME = permit.NAME_EMP_A;
                        rA42_FAMILY_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;

                        rA42_FAMILY_PASS_DTL.REJECTED = false;
                        rA42_FAMILY_PASS_DTL.STATUS = false;
                        rA42_FAMILY_PASS_DTL.ISOPENED = true;
                    }
                    else
                    {

                        rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE;
                        rA42_FAMILY_PASS_DTL.REJECTED = false;
                        rA42_FAMILY_PASS_DTL.STATUS = false;
                        rA42_FAMILY_PASS_DTL.ISOPENED = false;
                    }
                }
                //this section is for autho person (المنسق الأمني) his workflow id is 2
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect this request to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_FAMILY_PASS_DTL);

                    }
                    else
                    {
                        rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_FAMILY_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_FAMILY_PASS_DTL.APPROVAL_RANK = permit.NAME_RANK_A;
                    rA42_FAMILY_PASS_DTL.APPROVAL_NAME = permit.NAME_EMP_A;
                    rA42_FAMILY_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;

                    rA42_FAMILY_PASS_DTL.REJECTED = false;
                    rA42_FAMILY_PASS_DTL.STATUS = false;
                    rA42_FAMILY_PASS_DTL.ISOPENED = true;
                }
                //this section is for permits cell,
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    if (STATION_CODE == 26)
                    {
                        //after the security officer create permit, the request will be completed and should be redirected to the permits cell
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_FAMILY_PASS_DTL);

                        }
                        else
                        {
                            rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }

                        rA42_FAMILY_PASS_DTL.PERMIT_SN = currentUser;
                        rA42_FAMILY_PASS_DTL.PERMIT_RANK = permit.NAME_RANK_A;
                        rA42_FAMILY_PASS_DTL.PERMIT_NAME = permit.NAME_EMP_A;
                        rA42_FAMILY_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;

                        rA42_FAMILY_PASS_DTL.REJECTED = false;
                        rA42_FAMILY_PASS_DTL.STATUS = true;
                        rA42_FAMILY_PASS_DTL.ISOPENED = true;
                        string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                        rA42_FAMILY_PASS_DTL.BARCODE = barcode;
                    }
                    else
                    {
                        //he should redirect the request to the security officer
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_FAMILY_PASS_DTL);

                        }
                        else
                        {
                            rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }

                        rA42_FAMILY_PASS_DTL.PERMIT_SN = currentUser;
                        rA42_FAMILY_PASS_DTL.PERMIT_RANK = permit.NAME_RANK_A;
                        rA42_FAMILY_PASS_DTL.PERMIT_NAME = permit.NAME_EMP_A;
                        rA42_FAMILY_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;

                        rA42_FAMILY_PASS_DTL.REJECTED = false;
                        rA42_FAMILY_PASS_DTL.STATUS = false;
                        rA42_FAMILY_PASS_DTL.ISOPENED = true;
                    }
                }
                //this section is for security officer
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //after the security officer create permit, the request will be completed and should be redirected to the permits cell
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_FAMILY_PASS_DTL);

                    }
                    else
                    {
                        rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_FAMILY_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_FAMILY_PASS_DTL.AUTHO_RANK = permit.NAME_RANK_A;
                    rA42_FAMILY_PASS_DTL.AUTHO_NAME = permit.NAME_EMP_A;
                    rA42_FAMILY_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_FAMILY_PASS_DTL.REJECTED = false;
                    rA42_FAMILY_PASS_DTL.STATUS = true;
                    rA42_FAMILY_PASS_DTL.ISOPENED = true;
                    rA42_FAMILY_PASS_DTL.BARCODE = rA42_FAMILY_PASS_DTL.BARCODE;
                }
                rA42_FAMILY_PASS_DTL.CRD_BY = currentUser;
                rA42_FAMILY_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_FAMILY_PASS_DTL.UPD_BY = currentUser;
                rA42_FAMILY_PASS_DTL.UPD_DT = DateTime.Now;
                db.RA42_FAMILY_PASS_DTL.Add(rA42_FAMILY_PASS_DTL);
                db.SaveChanges();
                //add relatives 
                RA42_MEMBERS_DTL rA42_MEMBERS_DTL = new RA42_MEMBERS_DTL();
                if (IDENTITY_TYPES != null && !IDENTITY_TYPES.Contains(0))
                {
                    try
                    {
                        for (int i = 0; i < RELATIVE_TYPES.Length; i++)
                        {

                            //create barcode for every relative
                            string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                            rA42_MEMBERS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                            rA42_MEMBERS_DTL.ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE;
                            rA42_MEMBERS_DTL.CIVIL_NUMBER = CIVIL_NUM[i];
                            rA42_MEMBERS_DTL.FULL_NAME = FULL_NAME[i];
                            rA42_MEMBERS_DTL.PHONE_NUMBER = PHONE_NUMBER_M[i];
                            rA42_MEMBERS_DTL.PASSPORT_NUMBER = PASSPORT_NUMBER[i];
                            rA42_MEMBERS_DTL.RELATIVE_TYPE_CODE = RELATIVE_TYPES[i];
                            rA42_MEMBERS_DTL.IDENTITY_CODE = IDENTITY_TYPES[i];
                            rA42_MEMBERS_DTL.GENDER_ID = GENDER_TYPES[i];
                            rA42_MEMBERS_DTL.REMARKS = REMARKS_LIST[i];

                            try
                            {

                                // Verify that the user selected a file
                                if (RELATIVE_IMAGE[i].ContentLength > 0)
                                {
                                    // extract only the filename with extention
                                    string fileName = Path.GetFileNameWithoutExtension(RELATIVE_IMAGE[i].FileName);
                                    string extension = Path.GetExtension(RELATIVE_IMAGE[i].FileName);


                                    //check the extention of the image file 
                                    if (general.CheckPersonalImage(RELATIVE_IMAGE[i].FileName))
                                    {

                                        fileName = "Relative_Profile_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                                        // store the file inside ~/Files/Profiles/ folder
                                        bool check = general.ResizeImage(RELATIVE_IMAGE[i], fileName);

                                        if (check != true)
                                        {
                                            AddToast(new Toast("",
                                           GetResourcesValue("error_update_message"),
                                           "red"));
                                            TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                            return View(rA42_FAMILY_PASS_DTL);
                                        }

                                        rA42_MEMBERS_DTL.PERSONAL_IMAGE = fileName;


                                    }
                                    else
                                    {
                                        //if format not supported, show error message 
                                        AddToast(new Toast("",
                                        GetResourcesValue("error_update_message"),
                                        "red"));
                                        TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                        return View(rA42_FAMILY_PASS_DTL);
                                    }
                                }
                            }

                            catch (Exception ex)
                            {
                                ex.GetBaseException();
                            }

                            rA42_MEMBERS_DTL.CRD_BY = currentUser;
                            rA42_MEMBERS_DTL.CRD_DT = DateTime.Now;
                            rA42_MEMBERS_DTL.UPD_BY = currentUser;
                            rA42_MEMBERS_DTL.UPD_DT = DateTime.Now;
                            db.RA42_MEMBERS_DTL.Add(rA42_MEMBERS_DTL);
                            db.SaveChanges();
                            //continue;
                        }



                    }
                    catch (IndexOutOfRangeException ex)
                    {
                        TempData["Erorr"] = ex.GetBaseException().Message + RELATIVE_TYPES.Length + " - " + IDENTITY_TYPES.Length + " - " +
                            GENDER_TYPES.Length + " - " + FULL_NAME.Length + " - " + REMARKS_LIST.Length + " - " + CIVIL_NUM.Length;
                        return View(rA42_FAMILY_PASS_DTL);
                    }
                }
                //add gates 
                RA42_ZONE_MASTER_MST rA42_ZONE_MASTER_MST = new RA42_ZONE_MASTER_MST();
                if (ZONE != null && !ZONE.Contains(0))
                {
                    for (int i = 0; i < ZONE.Length; i++)
                    {


                        rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE;
                        rA42_ZONE_MASTER_MST.ZONE_CODE = ZONE[i];
                        rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = 0;
                        rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                        rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                        db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                        db.SaveChanges();
                        //continue;
                    }

                }
                //add documents
                if (files != null)
                {
                    //the files should be setting inide foreach loop to deal with multiple documents
                    try
                    {
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

                                    fileName = "FileNO" + c + "_4_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);
                                    //add new document
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE,
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
                                    //delete whole documents if there is one file corrupted 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_FAMILY_PASS_DTL);
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
            return View(rA42_FAMILY_PASS_DTL);
        }


        // this permit for people who are from otside rafo and thier orgnizations/companies/goverment responsiple about them 
        public ActionResult Otherpermit()
        {
            ViewBag.activetab = "Otherpermit";
            ViewBag.Service_No = currentUser;
            //get unit-code from session 
            var url = Url.RequestContext.RouteData.Values["id"];
            //int unit = 0;

            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get unit code to use it to detrmine the autho persone who will recive the permit(الركن المختص، قائد الجناح أو السرب)
                //هذا الخيار يحدد العبارة التي ستظهر للمسخدم (الركن المختص أم قائد الجناح او السرب)
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

                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();

                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.STATION_NAME = "in " + check_unit.STATION_NAME_E;
                }
                else
                {
                    ViewBag.STATION_NAME = "في " + check_unit.STATION_NAME_A;

                }



                //get unit code to use it to detrmine the autho persone who will recive the permit(الركن المختص، قائد الجناح أو السرب)
                //هذا الخيار يحدد العبارة التي ستظهر للمسخدم (الركن المختص أم قائد الجناح او السرب)
                ViewBag.Get_Station_Code = STATION_CODE.ToString();
                FORCE_ID = check_unit.FORCE_ID.Value;

            }

            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();

            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //get permit types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
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
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get relatives types in english
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E");
                //get gates types in english 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME_E");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME_E");
                //get autho person workflow id for this kind of permit in english for this station
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
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
                //get identities in arabic  
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types in arabic
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                //get zones and gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
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
                //get genders in arabic
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get relatives types in arabic
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE");
                //get gates types in arabic 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME");
                //get autho person workflow id for this kind of permit in arabic for this station
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
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
        public ActionResult Otherpermit(RA42_FAMILY_PASS_DTL rA42_FAMILY_PASS_DTL, int[] RELATIVE_TYPES, HttpPostedFileBase[] RELATIVE_IMAGE, int[] IDENTITY_TYPES, int[] GENDER_TYPES, string[] FULL_NAME, string[] CIVIL_NUM, string[] PASSPORT_NUMBER, string[] PHONE_NUMBER_M, string[] REMARKS_LIST,
            int[] FILE_TYPES,string[] FILE_TYPES_TEXT, int[] ZONE, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {

            
            ViewBag.activetab = "Otherpermit";
            ViewBag.Service_No = currentUser;
            //get unit-code form session
            var url = Url.RequestContext.RouteData.Values["id"];

            //if session not null continue
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get unit code to use it to detrmine the autho persone who will recive the permit(الركن المختص، قائد الجناح أو السرب)
                //هذا الخيار يحدد العبارة التي ستظهر للمسخدم (الركن المختص أم قائد الجناح او السرب)
                ViewBag.Get_Station_Code = id.ToString();
                var station = int.Parse(id.ToString());
                STATION_CODE = station;
                //get station naem, this option will shoen as title 
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

                var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();

                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.STATION_NAME = "in " + check_unit.STATION_NAME_E;
                }
                else
                {
                    ViewBag.STATION_NAME = "في " + check_unit.STATION_NAME_A;

                }


                //get unit code to use it to detrmine the autho persone who will recive the permit(الركن المختص، قائد الجناح أو السرب)
                //هذا الخيار يحدد العبارة التي ستظهر للمسخدم (الركن المختص أم قائد الجناح او السرب)
                ViewBag.Get_Station_Code = STATION_CODE.ToString();
                FORCE_ID = check_unit.FORCE_ID.Value;

            }

            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();

            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_FAMILY_PASS_DTL.IDENTITY_CODE);
                //get permits type
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE ==3 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_FAMILY_PASS_DTL.GENDER_ID);
                //get relatives types in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_FAMILY_PASS_DTL.RELATIVE_TYPE_CODE);
                //get gates types in english 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME_E");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME_E");
                //get autho person workflow id for this kind of permit in english for this station
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        //show error message if ther is no autho person 
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_FAMILY_PASS_DTL);
                    }

                }



            }
            else
            {
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_FAMILY_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                //get zones and gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
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
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_FAMILY_PASS_DTL.GENDER_ID);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_FAMILY_PASS_DTL.RELATIVE_TYPE_CODE);
                //get gates in arabic 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME");
                //get autho person workflow id for this kind of permit in arabic for this station
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {

                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        //show error for user if there is no autho person setted
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_FAMILY_PASS_DTL);
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


                            //check image extention 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {

                                fileName = "Profile_4_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);
                                if (check != true)
                                {
                                    AddToast(new Toast("",
                                   GetResourcesValue("error_update_message"),
                                   "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_FAMILY_PASS_DTL);
                                }
                                rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE = fileName;



                            }
                            else
                            {
                                //show error if image format not supported
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_FAMILY_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                rA42_FAMILY_PASS_DTL.SERVICE_NUMBER = rA42_FAMILY_PASS_DTL.RESPONSIBLE;
                rA42_FAMILY_PASS_DTL.RESPONSIBLE = currentUser;
                rA42_FAMILY_PASS_DTL.HOST_RANK_A = rA42_FAMILY_PASS_DTL.HOST_RANK_A;
                rA42_FAMILY_PASS_DTL.HOST_RANK_E = rA42_FAMILY_PASS_DTL.HOST_RANK_E;
                rA42_FAMILY_PASS_DTL.HOST_NAME_A = rA42_FAMILY_PASS_DTL.HOST_NAME_A;
                rA42_FAMILY_PASS_DTL.HOST_NAME_E = rA42_FAMILY_PASS_DTL.HOST_NAME_E;
                rA42_FAMILY_PASS_DTL.PROFESSION_A = rA42_FAMILY_PASS_DTL.PROFESSION_A;
                rA42_FAMILY_PASS_DTL.PROFESSION_E = rA42_FAMILY_PASS_DTL.PROFESSION_E;
                rA42_FAMILY_PASS_DTL.UNIT_A = rA42_FAMILY_PASS_DTL.UNIT_A;
                rA42_FAMILY_PASS_DTL.UNIT_E = rA42_FAMILY_PASS_DTL.UNIT_E;
                rA42_FAMILY_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_FAMILY_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_FAMILY_PASS_DTL.BARCODE = rA42_FAMILY_PASS_DTL.BARCODE;
                rA42_FAMILY_PASS_DTL.CARD_FOR_CODE = 3;
                //get current user details from api
                User permit = null;
                Task<User> callTask2 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask2.Wait();
                permit = callTask2.Result;
                //this section is for applicant (مقدم الطلب)
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                {
                    rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE;

                    rA42_FAMILY_PASS_DTL.REJECTED = false;
                    rA42_FAMILY_PASS_DTL.STATUS = false;
                    rA42_FAMILY_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person (المنسق الأمني)
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect this request to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_FAMILY_PASS_DTL);

                    }
                    else
                    {
                        rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_FAMILY_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_FAMILY_PASS_DTL.APPROVAL_RANK = permit.NAME_RANK_A;
                    rA42_FAMILY_PASS_DTL.APPROVAL_NAME = permit.NAME_EMP_A;
                    rA42_FAMILY_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;

                    rA42_FAMILY_PASS_DTL.REJECTED = false;
                    rA42_FAMILY_PASS_DTL.STATUS = false;
                    rA42_FAMILY_PASS_DTL.ISOPENED = true;
                }
                //this section is for permits cell 
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //they should redirect request to the security officer 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_FAMILY_PASS_DTL);

                    }
                    else
                    {
                        rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                   
                    rA42_FAMILY_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_FAMILY_PASS_DTL.PERMIT_RANK = permit.NAME_RANK_A;
                    rA42_FAMILY_PASS_DTL.PERMIT_NAME = permit.NAME_EMP_A;
                    rA42_FAMILY_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                    rA42_FAMILY_PASS_DTL.REJECTED = false;
                    rA42_FAMILY_PASS_DTL.STATUS = false;
                    rA42_FAMILY_PASS_DTL.ISOPENED = true;
                }
                //this section is for security offcier
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //after he create this request and complete every thing, the request will redirected to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_FAMILY_PASS_DTL);

                    }
                    else
                    {
                        rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_FAMILY_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_FAMILY_PASS_DTL.AUTHO_RANK = permit.NAME_RANK_A;
                    rA42_FAMILY_PASS_DTL.AUTHO_NAME = permit.NAME_EMP_A;
                    rA42_FAMILY_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;

                    rA42_FAMILY_PASS_DTL.REJECTED = false;
                    rA42_FAMILY_PASS_DTL.STATUS = true;
                    rA42_FAMILY_PASS_DTL.ISOPENED = true;
                }
                rA42_FAMILY_PASS_DTL.CRD_BY = currentUser;
                rA42_FAMILY_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_FAMILY_PASS_DTL.UPD_BY = currentUser;
                rA42_FAMILY_PASS_DTL.UPD_DT = DateTime.Now;
                db.RA42_FAMILY_PASS_DTL.Add(rA42_FAMILY_PASS_DTL);
                db.SaveChanges();
               
                //add gates 
                RA42_ZONE_MASTER_MST rA42_ZONE_MASTER_MST = new RA42_ZONE_MASTER_MST();
                if (ZONE != null && !ZONE.Contains(0))
                {
                    for (int i = 0; i < ZONE.Length; i++)
                    {


                        rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE;
                        rA42_ZONE_MASTER_MST.ZONE_CODE = ZONE[i];
                        rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = 0;
                        rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                        rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                        db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                        db.SaveChanges();
                        //continue;
                    }

                }
                //add documents
                if (files != null)
                {
                   //set documents inside foreach loop to deal with multiple uploaded files 
                    try
                    {
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

                                    fileName = "FileNO" + c + "_4_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);
                                    //add new document
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE,
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
                                    //delet all documents if there is somthing wrong with one file 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_FAMILY_PASS_DTL);
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

                //add relatives 
                RA42_MEMBERS_DTL rA42_MEMBERS_DTL = new RA42_MEMBERS_DTL();
                if (IDENTITY_TYPES != null && !IDENTITY_TYPES.Contains(0))
                {
                    try
                    {
                        for (int i = 0; i < RELATIVE_TYPES.Length; i++)
                        {

                            //create barcode for every relative
                            string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                            rA42_MEMBERS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                            rA42_MEMBERS_DTL.ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE;
                            rA42_MEMBERS_DTL.CIVIL_NUMBER = CIVIL_NUM[i];
                            rA42_MEMBERS_DTL.FULL_NAME = FULL_NAME[i];
                            rA42_MEMBERS_DTL.PHONE_NUMBER = PHONE_NUMBER_M[i];
                            rA42_MEMBERS_DTL.PASSPORT_NUMBER = PASSPORT_NUMBER[i];
                            rA42_MEMBERS_DTL.RELATIVE_TYPE_CODE = RELATIVE_TYPES[i];
                            rA42_MEMBERS_DTL.IDENTITY_CODE = IDENTITY_TYPES[i];
                            rA42_MEMBERS_DTL.GENDER_ID = GENDER_TYPES[i];
                            rA42_MEMBERS_DTL.REMARKS = REMARKS_LIST[i];

                            try
                            {

                                // Verify that the user selected a file
                                if (RELATIVE_IMAGE[i].ContentLength > 0)
                                {
                                    // extract only the filename with extention
                                    string fileName = Path.GetFileNameWithoutExtension(RELATIVE_IMAGE[i].FileName);
                                    string extension = Path.GetExtension(RELATIVE_IMAGE[i].FileName);


                                    //check the extention of the image file 
                                    if (general.CheckPersonalImage(RELATIVE_IMAGE[i].FileName))
                                    {

                                        fileName = "Relative_Profile_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                                        // store the file inside ~/Files/Profiles/ folder
                                        bool check = general.ResizeImage(RELATIVE_IMAGE[i], fileName);

                                        if (check != true)
                                        {
                                            AddToast(new Toast("",
                                           GetResourcesValue("error_update_message"),
                                           "red"));
                                            TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                            return View(rA42_FAMILY_PASS_DTL);
                                        }

                                        rA42_MEMBERS_DTL.PERSONAL_IMAGE = fileName;


                                    }
                                    else
                                    {
                                        //if format not supported, show error message 
                                        AddToast(new Toast("",
                                        GetResourcesValue("error_update_message"),
                                        "red"));
                                        TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                        return View(rA42_FAMILY_PASS_DTL);
                                    }
                                }
                            }

                            catch (Exception ex)
                            {
                                ex.GetBaseException();
                            }

                            rA42_MEMBERS_DTL.CRD_BY = currentUser;
                            rA42_MEMBERS_DTL.CRD_DT = DateTime.Now;
                            rA42_MEMBERS_DTL.UPD_BY = currentUser;
                            rA42_MEMBERS_DTL.UPD_DT = DateTime.Now;
                            db.RA42_MEMBERS_DTL.Add(rA42_MEMBERS_DTL);
                            db.SaveChanges();
                            //continue;
                        }



                    }
                    catch (IndexOutOfRangeException ex)
                    {
                        TempData["Erorr"] = ex.GetBaseException().Message + RELATIVE_TYPES.Length + " - " + IDENTITY_TYPES.Length + " - " +
                            GENDER_TYPES.Length + " - " + FULL_NAME.Length + " - " + REMARKS_LIST.Length + " - " + CIVIL_NUM.Length;
                        return View(rA42_FAMILY_PASS_DTL);
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
            return View(rA42_FAMILY_PASS_DTL);
        }

        //edit relative
        public ActionResult EditRelative(int? id)
        {
            ViewBag.activetab = "EditRelative";

            if (id == null)
            {
                return NotFound();
            }
            //check if permit in the table 
            RA42_MEMBERS_DTL rA42_MEMBERS_DTL = db.RA42_MEMBERS_DTL.Find(id);
            if (rA42_MEMBERS_DTL == null)
            {
                return NotFound();
            }



            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_MEMBERS_DTL.IDENTITY_CODE);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
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
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_MEMBERS_DTL.GENDER_ID);
                //get relatives types in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_MEMBERS_DTL.RELATIVE_TYPE_CODE);
                //get gates types in english 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME_E");
                



            }
            else
            {
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_MEMBERS_DTL.IDENTITY_CODE);
                //get zones and gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
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
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_MEMBERS_DTL.GENDER_ID);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_MEMBERS_DTL.RELATIVE_TYPE_CODE);
                //get gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME");
               


            }

            //get selected zones and gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_MEMBERS_DTL.ACCESS_ROW_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get selected files 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_MEMBERS_DTL.ACCESS_ROW_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get documnts types for this kind of permit to check missing files with this request 
            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true ).ToList();
            var f = db.RA42_FAMILY_PASS_DTL.Where(a => a.FAMILY_CODE == rA42_MEMBERS_DTL.ACCESS_ROW_CODE).FirstOrDefault();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == f.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == f.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == f.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == f.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();


            return View(rA42_MEMBERS_DTL);
        }

        //post new employee data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditRelative(RA42_MEMBERS_DTL rA42_MEMBERS_DTL, HttpPostedFileBase PERSONAL_IMAGE, int[] ZONE, int[] SUB_ZONE,
            int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files)
        {
            ViewBag.activetab = "EditRelative";
            var v = db.RA42_MEMBERS_DTL.Where(a => a.MEMBER_CODE == rA42_MEMBERS_DTL.MEMBER_CODE).FirstOrDefault();
            var f = db.RA42_FAMILY_PASS_DTL.Where(a => a.FAMILY_CODE == v.ACCESS_ROW_CODE).FirstOrDefault();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == f.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == f.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == f.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == f.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            //get selected zones and gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == v.ACCESS_ROW_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get selected files 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == v.ACCESS_ROW_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();

            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_MEMBERS_DTL.IDENTITY_CODE);
                //get relatives types in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_MEMBERS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_MEMBERS_DTL.GENDER_ID);
                //get zones and gates in english
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == f.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == f.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == f.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_MEMBERS_DTL.IDENTITY_CODE);
                //get relatives types in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_MEMBERS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_MEMBERS_DTL.GENDER_ID);
                //get zones and gates in ar
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == f.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == f.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == f.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                    v.ACCESS_ROW_CODE = f.FAMILY_CODE;
                    v.RELATIVE_TYPE_CODE = rA42_MEMBERS_DTL.RELATIVE_TYPE_CODE;
                    v.CIVIL_NUMBER = rA42_MEMBERS_DTL.CIVIL_NUMBER;
                    v.PASSPORT_NUMBER = rA42_MEMBERS_DTL.PASSPORT_NUMBER;
                    v.PHONE_NUMBER = rA42_MEMBERS_DTL.PHONE_NUMBER;
                    v.FULL_NAME = rA42_MEMBERS_DTL.FULL_NAME;
                    v.REMARKS = rA42_MEMBERS_DTL.REMARKS;
                    v.IDENTITY_CODE = rA42_MEMBERS_DTL.IDENTITY_CODE;
                    v.GENDER_ID = rA42_MEMBERS_DTL.GENDER_ID;
                    
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



                                    fileName = "Relative_Profile_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                                    // store the file inside ~/Files/Profiles/ folder
                                    bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                    if (check != true)
                                    {
                                        AddToast(new Toast("",
                                       GetResourcesValue("error_update_message"),
                                       "red"));
                                        TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                        return View(rA42_MEMBERS_DTL);
                                    }

                                    v.PERSONAL_IMAGE = fileName;


                                }
                                else
                                {
                                    AddToast(new Toast("",
                                    GetResourcesValue("error_update_message"),
                                    "red"));
                                    TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                    return View(rA42_MEMBERS_DTL);
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
                    db.Entry(v).State = EntityState.Modified;
                    db.SaveChanges();
                    var cr = db.RA42_FAMILY_PASS_DTL.Where(a => a.FAMILY_CODE == v.ACCESS_ROW_CODE).FirstOrDefault();
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
                            rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = v.ACCESS_ROW_CODE;
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
                                            ACCESS_ROW_CODE = v.ACCESS_ROW_CODE,
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
                                        var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == v.ACCESS_ROW_CODE).ToList();
                                        foreach (var del in delete)
                                        {
                                            string filpath = "~/Documents/" + del.FILE_NAME;
                                            general.RemoveFileFromServer(filpath);
                                            db.RA42_FILES_MST.Remove(del);
                                            db.SaveChanges();
                                        }
                                        TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                        return View(rA42_MEMBERS_DTL);
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
                    return RedirectToAction("EditRelative", new { id = rA42_MEMBERS_DTL.MEMBER_CODE });
                }
            }

            return View(rA42_MEMBERS_DTL);
        }


        // this section to proccess any family member permit by authorized users inside the sytem such as permits cell, security officer and autho person and applicant himself 
        public ActionResult PrintMember(int? id)
        {
            ViewBag.activetab = "PrintMember";
            ViewBag.MEMBER_CODE = id;
            if (id == null)
            {
                return NotFound();
            }
            var checkMember = db.RA42_MEMBERS_DTL.Where(a => a.MEMBER_CODE == id).FirstOrDefault();
            if(checkMember == null)
            {
                return NotFound();
            }
            RA42_FAMILY_PASS_DTL rA42_FAMILY_PASS_DTL = db.RA42_FAMILY_PASS_DTL.Find(checkMember.ACCESS_ROW_CODE);
            if (rA42_FAMILY_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if user has permission to edit this permit 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_FAMILY_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_FAMILY_PASS_DTL.RESPONSIBLE != currentUser)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
                if (rA42_FAMILY_PASS_DTL.ISOPENED == true)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        if (rA42_FAMILY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && rA42_FAMILY_PASS_DTL.REJECTED == true)
                        {

                        }
                        else
                        {
                            if (rA42_FAMILY_PASS_DTL.ISOPENED == true)
                            {
                                return NotFound();
                            }

                        }

                    }
                }
            }
            else
            {
                if (rA42_FAMILY_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_FAMILY_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != rA42_FAMILY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
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
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", checkMember.IDENTITY_CODE);
                //get permits types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                if(rA42_FAMILY_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE ==1), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);

                }
                //get zones and gates in english
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_FAMILY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_FAMILY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", checkMember.GENDER_ID);
                //get relatives types in english
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", checkMember.RELATIVE_TYPE_CODE);
                //get gates in english 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME_E");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME_E");
               

            }
            else
            {
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", checkMember.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                if (rA42_FAMILY_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);

                }
                //get zones and gates in ar
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_FAMILY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_FAMILY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", checkMember.GENDER_ID);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", checkMember.RELATIVE_TYPE_CODE);
                //get gates in arabic 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME");
               // ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME");
               


            }
            //get documents types for this kind of permit, we need this to compare it with missing files 
            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_FAMILY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_FAMILY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_FAMILY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_FAMILY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            //get selected gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get documents
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
           
            //get personal image 
            ViewBag.PERSONAL_IMAGE = checkMember.PERSONAL_IMAGE;
            //get status of the permit 
            ViewBag.STATUS = rA42_FAMILY_PASS_DTL.STATUS;

            rA42_FAMILY_PASS_DTL.RELATIVE_TYPE_CODE = checkMember.RELATIVE_TYPE_CODE.Value;
            rA42_FAMILY_PASS_DTL.CIVIL_NUMBER = checkMember.CIVIL_NUMBER;
            rA42_FAMILY_PASS_DTL.NAME_A = checkMember.FULL_NAME;
            rA42_FAMILY_PASS_DTL.PHONE_NUMBER = checkMember.PHONE_NUMBER;
            rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE = checkMember.PERSONAL_IMAGE;
            rA42_FAMILY_PASS_DTL.IDENTITY_CODE = checkMember.IDENTITY_CODE.Value;
            rA42_FAMILY_PASS_DTL.GENDER_ID = checkMember.GENDER_ID.Value;
            rA42_FAMILY_PASS_DTL.REMARKS = checkMember.REMARKS;
            return View(rA42_FAMILY_PASS_DTL);
        }

        //post new data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PrintMember(RA42_FAMILY_PASS_DTL rA42_FAMILY_PASS_DTL, string COMMENT,
            int[] FILE_TYPES, string[] FILE_TYPES_TEXT, int[] ZONE, int FAMILY_ID,int MEMBER_CODE, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            var general_data = db.RA42_FAMILY_PASS_DTL.Where(a => a.FAMILY_CODE == FAMILY_ID).FirstOrDefault();
            var relative_data = db.RA42_MEMBERS_DTL.Where(a => a.MEMBER_CODE == MEMBER_CODE).FirstOrDefault();
            if(relative_data == null || general_data == null)
            {
                return NotFound();
            }
            ViewBag.activetab = "PrintMember";

            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (general_data.STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }

            //get documents types for this kind of permit, we need this to compare it with missing files 
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
            //get selected gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == FAMILY_ID && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get documents
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == FAMILY_ID && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get personal image 
            ViewBag.PERSONAL_IMAGE = relative_data.PERSONAL_IMAGE;
            //get status of the permit 


            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_FAMILY_PASS_DTL.IDENTITY_CODE);
                //get permits types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                if (rA42_FAMILY_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
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
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_FAMILY_PASS_DTL.GENDER_ID);
                //get relatives types in english
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_FAMILY_PASS_DTL.RELATIVE_TYPE_CODE);
                //get gates in english 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME_E");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME_E");
               



            }
            else
            {
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_FAMILY_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                if (rA42_FAMILY_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
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
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_FAMILY_PASS_DTL.GENDER_ID);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_FAMILY_PASS_DTL.RELATIVE_TYPE_CODE);
                //get gates in arabic 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME");
                




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

                                fileName = "Profile_4_" + DateTime.Now.ToString("yymmssfff") + extension;
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
                                rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE = fileName;



                            }
                            else
                            {
                                //show error message if image extention not supported 
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






                //add new personal image in case user upload new one successfully
                if (rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE != null)
                {
                    rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE = rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE;
                }
                else
                {
                    rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE = relative_data.PERSONAL_IMAGE;

                }
                //get host details from api
                rA42_FAMILY_PASS_DTL.SERVICE_NUMBER = rA42_FAMILY_PASS_DTL.SERVICE_NUMBER.ToUpper();
                rA42_FAMILY_PASS_DTL.RESPONSIBLE = rA42_FAMILY_PASS_DTL.RESPONSIBLE;
                rA42_FAMILY_PASS_DTL.HOST_RANK_A = rA42_FAMILY_PASS_DTL.HOST_RANK_A;
                rA42_FAMILY_PASS_DTL.HOST_RANK_E = rA42_FAMILY_PASS_DTL.HOST_RANK_E;
                rA42_FAMILY_PASS_DTL.HOST_NAME_A = rA42_FAMILY_PASS_DTL.HOST_NAME_A;
                rA42_FAMILY_PASS_DTL.HOST_NAME_E = rA42_FAMILY_PASS_DTL.HOST_NAME_E;
                rA42_FAMILY_PASS_DTL.PROFESSION_A = rA42_FAMILY_PASS_DTL.PROFESSION_A;
                rA42_FAMILY_PASS_DTL.PROFESSION_E = rA42_FAMILY_PASS_DTL.PROFESSION_E;
                rA42_FAMILY_PASS_DTL.NAME_A = rA42_FAMILY_PASS_DTL.NAME_A;
                rA42_FAMILY_PASS_DTL.NAME_E = rA42_FAMILY_PASS_DTL.NAME_E;
                rA42_FAMILY_PASS_DTL.UNIT_A = rA42_FAMILY_PASS_DTL.UNIT_A;
                rA42_FAMILY_PASS_DTL.UNIT_E = rA42_FAMILY_PASS_DTL.UNIT_E;
                rA42_FAMILY_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_FAMILY_PASS_DTL.STATION_CODE = general_data.STATION_CODE;
                rA42_FAMILY_PASS_DTL.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                rA42_FAMILY_PASS_DTL.GENDER_ID = rA42_FAMILY_PASS_DTL.GENDER_ID;
                rA42_FAMILY_PASS_DTL.IDENTITY_CODE = rA42_FAMILY_PASS_DTL.IDENTITY_CODE;
                rA42_FAMILY_PASS_DTL.CIVIL_NUMBER = rA42_FAMILY_PASS_DTL.CIVIL_NUMBER;
                //get current user details from api 
                User permit = null;
                Task<User> callTask2 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask2.Wait();
                permit = callTask2.Result;
                //this section for applicant, workflow id <= 1
               
               

                rA42_FAMILY_PASS_DTL.APPROVAL_SN = general_data.APPROVAL_SN;
                rA42_FAMILY_PASS_DTL.APPROVAL_RANK = general_data.APPROVAL_RANK;
                rA42_FAMILY_PASS_DTL.APPROVAL_NAME = general_data.APPROVAL_NAME;
                rA42_FAMILY_PASS_DTL.APPROVAL_APPROVISION_DATE = general_data.APPROVAL_APPROVISION_DATE;
                rA42_FAMILY_PASS_DTL.PERMIT_SN = currentUser;
                rA42_FAMILY_PASS_DTL.PERMIT_RANK = permit.NAME_RANK_A;
                rA42_FAMILY_PASS_DTL.PERMIT_NAME = permit.NAME_EMP_A;
                rA42_FAMILY_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE;
                rA42_FAMILY_PASS_DTL.AUTHO_SN = general_data.AUTHO_SN;
                rA42_FAMILY_PASS_DTL.AUTHO_RANK = general_data.AUTHO_RANK;
                rA42_FAMILY_PASS_DTL.AUTHO_NAME = general_data.AUTHO_NAME; 
                rA42_FAMILY_PASS_DTL.AUTHO_APPROVISION_DATE = general_data.AUTHO_APPROVISION_DATE;
                rA42_FAMILY_PASS_DTL.REJECTED = false;
                rA42_FAMILY_PASS_DTL.STATUS = true;
                rA42_FAMILY_PASS_DTL.ISOPENED = true;
                rA42_FAMILY_PASS_DTL.ISPRINTED = false;
                rA42_FAMILY_PASS_DTL.BARCODE = rA42_FAMILY_PASS_DTL.BARCODE;
                rA42_FAMILY_PASS_DTL.CRD_BY = currentUser;
                rA42_FAMILY_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_FAMILY_PASS_DTL.UPD_BY = currentUser;
                rA42_FAMILY_PASS_DTL.UPD_DT = DateTime.Now;
                db.RA42_FAMILY_PASS_DTL.Add(rA42_FAMILY_PASS_DTL);
                db.SaveChanges();


                //add comments
                if (COMMENT.Length > 0)
                {
                    RA42_COMMENTS_MST rA42_COMMENT = new RA42_COMMENTS_MST();
                    rA42_COMMENT.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                    rA42_COMMENT.PASS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE;
                    rA42_COMMENT.CRD_BY = currentUser;
                    rA42_COMMENT.CRD_DT = DateTime.Now;
                    rA42_COMMENT.COMMENT = COMMENT;
                    db.RA42_COMMENTS_MST.Add(rA42_COMMENT);


                }
                //add selected zones and gates
                RA42_ZONE_MASTER_MST rA42_ZONE_MASTER_MST = new RA42_ZONE_MASTER_MST();
                if (ZONE != null && !ZONE.Contains(0))
                {
                    for (int i = 0; i < ZONE.Length; i++)
                    {


                        rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE;
                        rA42_ZONE_MASTER_MST.ZONE_CODE = ZONE[i];
                        rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = 0;
                        rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                        rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                        db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                        db.SaveChanges();
                        //continue;
                    }

                }
                var zones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == FAMILY_ID && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
                //add previues zones to new zone
                foreach (var zone in zones)
                {
                    rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE;
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
                                        ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE,
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
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return RedirectToAction("Renew", new { id = FAMILY_ID });
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
                var selected_files = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == FAMILY_ID && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
                foreach (var file in selected_files)
                {
                    //add new file
                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                    {
                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                        ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE,
                        FILE_TYPE = file.FILE_TYPE,
                        FILE_TYPE_TEXT = file.FILE_TYPE_TEXT,
                        FILE_NAME = file.FILE_NAME,
                        CRD_BY = currentUser,
                        CRD_DT = DateTime.Now


                    };
                    db.RA42_FILES_MST.Add(fILES_MST);
                    db.SaveChanges();
                }
                try
                {
                    
                    relative_data.ISPRINTED = true;
                    db.SaveChanges();
                }
                catch(Exception xcv)
                {
                    TempData["Erorr"] = xcv.GetBaseException();
                    return View(rA42_FAMILY_PASS_DTL);
                }

                TempData["status"] = "تم حفظ البيانات بنجاح!";


                return RedirectToAction("Card", new { id = rA42_FAMILY_PASS_DTL.FAMILY_CODE });







            }
            TempData["Erorr"] = "Somthing wrong happen,حدث خطأ ما";
            AddToast(new Toast("",
                GetResourcesValue("error_update_message"),
                "red"));
            return View(general_data);
        }

        // this section to proccess any family permit by authorized users inside the sytem such as permits cell, security officer and autho person and applicant himself 
        public ActionResult Edit(int? id)
		{
            ViewBag.activetab = "edit";

            if (id == null)
            {
                return NotFound();
            }
            RA42_FAMILY_PASS_DTL rA42_FAMILY_PASS_DTL = db.RA42_FAMILY_PASS_DTL.Find(id);
            if (rA42_FAMILY_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if user has permission to edit this permit 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_FAMILY_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_FAMILY_PASS_DTL.RESPONSIBLE != currentUser)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
                if (rA42_FAMILY_PASS_DTL.ISOPENED == true)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        if (rA42_FAMILY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && rA42_FAMILY_PASS_DTL.REJECTED == true)
                        {

                        }
                        else
                        {
                            if (rA42_FAMILY_PASS_DTL.ISOPENED == true)
                            {
                                return NotFound();
                            }
                            
                        }

                    }
                }
            }
            else
            {
                if (rA42_FAMILY_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_FAMILY_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != rA42_FAMILY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
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
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_FAMILY_PASS_DTL.IDENTITY_CODE);
                //get permits types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                if (rA42_FAMILY_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get zones and gates in en 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in en 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_FAMILY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_FAMILY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_FAMILY_PASS_DTL.GENDER_ID);
                //get relatives types in english
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_FAMILY_PASS_DTL.RELATIVE_TYPE_CODE);
                //get gates in english 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME_E");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME_E");
                //get autho person for this kind of permits in english
                //this option is for applicant, if he want to change autho person (المنسق الأمني) before he proccess the request 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (rA42_FAMILY_PASS_DTL.STATION_CODE != 26)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            //return View();
                        }
                    }

                }

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE);
                }



            }
            else
            {
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_FAMILY_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                if (rA42_FAMILY_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get zones and gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_FAMILY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_FAMILY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_FAMILY_PASS_DTL.GENDER_ID);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_FAMILY_PASS_DTL.RELATIVE_TYPE_CODE);
                //get gates in arabic 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME");
                //get autho person for this kind of permits in arabic
                //this option is for applicant, if he want to change autho person (المنسق الأمني) before he proccess the request
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (rA42_FAMILY_PASS_DTL.STATION_CODE != 26)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        }
                        //return View();
                    }
                }
                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE);
                }


            }
            //get documents types for this kind of permit, we need this to compare it with missing files 
            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_FAMILY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_FAMILY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_FAMILY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_FAMILY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            //get selected gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get relatives 
            ViewBag.GetRelativs = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.ACCESS_ROW_CODE == id && a.DLT_STS != true).ToList();
            //get documents
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get comments
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            //get personal image 
            ViewBag.PERSONAL_IMAGE = rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE;
            //get status of the permit 
            ViewBag.STATUS = rA42_FAMILY_PASS_DTL.STATUS;
           
            return View(rA42_FAMILY_PASS_DTL);
        }

		//post new data
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(RA42_FAMILY_PASS_DTL rA42_FAMILY_PASS_DTL, FormCollection form, string COMMENT, HttpPostedFileBase[] RELATIVE_IMAGE, int[] RELATIVE_TYPES, int[] IDENTITY_TYPES, int[] GENDER_TYPES, string[] FULL_NAME, string[] PASSPORT_NUMBER, string[] PHONE_NUMBER_M, string[] CIVIL_NUM, string[] REMARKS_LIST,
            int[] FILE_TYPES,string[] FILE_TYPES_TEXT, int[] ZONE, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
		{
            var general_data = db.RA42_FAMILY_PASS_DTL.Where(a => a.FAMILY_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE).FirstOrDefault();
            ViewBag.activetab = "edit";
            User user = null;
            Task<User> callTask = Task.Run(
                () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                );
            callTask.Wait();
            user = callTask.Result;

            //get documents types for this kind of permit, we need this to compare it with missing files 
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
            //get selected gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get relatives 
            ViewBag.GetRelativs = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_ROW_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
            //get documents
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get comments
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            //get personal image 
            ViewBag.PERSONAL_IMAGE = rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE;
            //get status of the permit 
            ViewBag.STATUS = rA42_FAMILY_PASS_DTL.STATUS;

            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_FAMILY_PASS_DTL.IDENTITY_CODE);
                //get permits types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                if (general_data.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);

                }
                //get zones and gates in english
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
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
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_FAMILY_PASS_DTL.GENDER_ID);
                //get relatives types in english
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_FAMILY_PASS_DTL.RELATIVE_TYPE_CODE);
                //get gates in english 
                // ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME_E");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME_E");
                //get autho person for this kind of permits in english
                //this option is for applicant, if he want to change autho person (المنسق الأمني) before he proccess the request 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (general_data.STATION_CODE != 26)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_FAMILY_PASS_DTL);
                        }
                    }
                }

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE);
                }

            }
            else
            {
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_FAMILY_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                if (general_data.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);

                }
                //get zones and gates in ar
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
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
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_FAMILY_PASS_DTL.GENDER_ID);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_FAMILY_PASS_DTL.RELATIVE_TYPE_CODE);
                //get gates in arabic 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME");
                //get autho person for this kind of permits in arabic
                //this option is for applicant, if he want to change autho person (المنسق الأمني) before he proccess the request
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (general_data.STATION_CODE != 26)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_FAMILY_PASS_DTL);
                        }
                    }

                }

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE);
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

                                fileName = "Profile_4_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);
                                if (check != true)
                                {
                                    AddToast(new Toast("",
                                   GetResourcesValue("error_update_message"),
                                   "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_FAMILY_PASS_DTL);
                                }
                                rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE = fileName;



                            }
                            else
                            {
                                //show error message if image extention not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_FAMILY_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
               

                //add relatives 
                RA42_MEMBERS_DTL rA42_MEMBERS_DTL = new RA42_MEMBERS_DTL();
                if (IDENTITY_TYPES != null && !IDENTITY_TYPES.Contains(0))
                {
                    try
                    {
                        for (int i = 0; i < RELATIVE_TYPES.Length; i++)
                        {

                            //create barcode for every relative
                            string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                            rA42_MEMBERS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                            rA42_MEMBERS_DTL.ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE;
                            rA42_MEMBERS_DTL.CIVIL_NUMBER = CIVIL_NUM[i];
                            rA42_MEMBERS_DTL.FULL_NAME = FULL_NAME[i];
                            rA42_MEMBERS_DTL.PHONE_NUMBER = PHONE_NUMBER_M[i];
                            rA42_MEMBERS_DTL.PASSPORT_NUMBER = PASSPORT_NUMBER[i];
                            rA42_MEMBERS_DTL.RELATIVE_TYPE_CODE = RELATIVE_TYPES[i];
                            rA42_MEMBERS_DTL.IDENTITY_CODE = IDENTITY_TYPES[i];
                            rA42_MEMBERS_DTL.GENDER_ID = GENDER_TYPES[i];
                            rA42_MEMBERS_DTL.REMARKS = REMARKS_LIST[i];

                            try
                            {

                                // Verify that the user selected a file
                                if (RELATIVE_IMAGE[i].ContentLength > 0)
                                {
                                    // extract only the filename with extention
                                    string fileName = Path.GetFileNameWithoutExtension(RELATIVE_IMAGE[i].FileName);
                                    string extension = Path.GetExtension(RELATIVE_IMAGE[i].FileName);


                                    //check the extention of the image file 
                                    if (general.CheckPersonalImage(RELATIVE_IMAGE[i].FileName))
                                    {

                                        fileName = "Relative_Profile_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                                        // store the file inside ~/Files/Profiles/ folder
                                        bool check = general.ResizeImage(RELATIVE_IMAGE[i], fileName);

                                        if (check != true)
                                        {
                                            AddToast(new Toast("",
                                           GetResourcesValue("error_update_message"),
                                           "red"));
                                            TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                            return View(rA42_FAMILY_PASS_DTL);
                                        }

                                        rA42_MEMBERS_DTL.PERSONAL_IMAGE = fileName;


                                    }
                                    else
                                    {
                                        //if format not supported, show error message 
                                        AddToast(new Toast("",
                                        GetResourcesValue("error_update_message"),
                                        "red"));
                                        TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                        return View(rA42_FAMILY_PASS_DTL);
                                    }
                                }
                            }

                            catch (Exception ex)
                            {
                                ex.GetBaseException();
                            }

                            rA42_MEMBERS_DTL.CRD_BY = currentUser;
                            rA42_MEMBERS_DTL.CRD_DT = DateTime.Now;
                            rA42_MEMBERS_DTL.UPD_BY = currentUser;
                            rA42_MEMBERS_DTL.UPD_DT = DateTime.Now;
                            db.RA42_MEMBERS_DTL.Add(rA42_MEMBERS_DTL);
                            db.SaveChanges();
                            //continue;
                        }



                    }
                    catch (IndexOutOfRangeException ex)
                    {
                        TempData["Erorr"] = ex.GetBaseException().Message + RELATIVE_TYPES.Length + " - " + IDENTITY_TYPES.Length + " - " +
                            GENDER_TYPES.Length + " - " + FULL_NAME.Length + " - " + REMARKS_LIST.Length + " - " + CIVIL_NUM.Length;
                        return View(rA42_FAMILY_PASS_DTL);
                    }
                }
                //add new gates 
                RA42_ZONE_MASTER_MST rA42_ZONE_MASTER_MST = new RA42_ZONE_MASTER_MST();
                if (ZONE != null && !ZONE.Contains(0))
                {
                    for (int i = 0; i < ZONE.Length; i++)
                    {


                        rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE;
                        rA42_ZONE_MASTER_MST.ZONE_CODE = ZONE[i];
                        rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = 0;
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
                    //set files in foreach loop to deal with multiple uploaded files
                    try
                    {
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

                                    fileName = "FileNO" + c + "_4_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);
                                    //add new document
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE,
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
                                    //delete whole documents if ther is somthing wrong with one document
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return RedirectToAction("Edit", new { id = rA42_FAMILY_PASS_DTL.FAMILY_CODE });
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
                    rA42_COMMENT.PASS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE;
                    rA42_COMMENT.CRD_BY = currentUser;
                    rA42_COMMENT.CRD_DT = DateTime.Now;
                    rA42_COMMENT.COMMENT = COMMENT;
                    db.RA42_COMMENTS_MST.Add(rA42_COMMENT);


                }

           
                //add new personal image in case user upload new one successfully
                if (rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE != null)
                {
                    general_data.PERSONAL_IMAGE = rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE;
                }
                else
                {
                    general_data.PERSONAL_IMAGE = general_data.PERSONAL_IMAGE;

                }
                var x = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE !=false && a.DLT_STS !=true && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                var currentUserInfo = (new UserInfo()).getUserInfo();

               

                //this section is for applicant
                //he can edit his request before any one process it
                //he can edit his request when rejected by autho person 
                if (form["approvebtn"] != null && (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11)))
                {


                    
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    if (rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE == null)
                    {
                        general_data.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;
                    }
                    else
                    {
                        general_data.WORKFLOW_RESPO_CODE = rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE;
                    }
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.BUILDING_NUMBER = rA42_FAMILY_PASS_DTL.BUILDING_NUMBER;
                    general_data.SERVICE_NUMBER = rA42_FAMILY_PASS_DTL.SERVICE_NUMBER;
                    general_data.CIVIL_NUMBER = rA42_FAMILY_PASS_DTL.CIVIL_NUMBER;
                    general_data.HOST_RANK_A = rA42_FAMILY_PASS_DTL.HOST_RANK_A;
                    general_data.HOST_RANK_E = rA42_FAMILY_PASS_DTL.HOST_RANK_E;
                    general_data.HOST_NAME_A = rA42_FAMILY_PASS_DTL.HOST_NAME_A;
                    general_data.HOST_NAME_E = rA42_FAMILY_PASS_DTL.HOST_NAME_E;
                    general_data.PROFESSION_A = rA42_FAMILY_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_FAMILY_PASS_DTL.PROFESSION_E;
                    general_data.NAME_A = rA42_FAMILY_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_FAMILY_PASS_DTL.NAME_E;
                    general_data.UNIT_A = rA42_FAMILY_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_FAMILY_PASS_DTL.UNIT_E;
                    general_data.PHONE_NUMBER = rA42_FAMILY_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_FAMILY_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_FAMILY_PASS_DTL.IDENTITY_CODE;
                    general_data.GENDER_ID = rA42_FAMILY_PASS_DTL.GENDER_ID;
                    general_data.PASS_TYPE_CODE = rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_FAMILY_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_FAMILY_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_FAMILY_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_FAMILY_PASS_DTL.REMARKS;
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

                //this section is for security coordinator
                if (form["approvebtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2)
                {

                    //he should redirect th request to permit cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return RedirectToAction("Edit",new { id=rA42_FAMILY_PASS_DTL.FAMILY_CODE});

                    }
                    general_data.ISOPENED = true;
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.SERVICE_NUMBER = rA42_FAMILY_PASS_DTL.SERVICE_NUMBER;
                    general_data.BUILDING_NUMBER = rA42_FAMILY_PASS_DTL.BUILDING_NUMBER;
                    general_data.CIVIL_NUMBER = rA42_FAMILY_PASS_DTL.CIVIL_NUMBER;
                    general_data.HOST_RANK_A = rA42_FAMILY_PASS_DTL.HOST_RANK_A;
                    general_data.HOST_RANK_E = rA42_FAMILY_PASS_DTL.HOST_RANK_E;
                    general_data.HOST_NAME_A = rA42_FAMILY_PASS_DTL.HOST_NAME_A;
                    general_data.HOST_NAME_E = rA42_FAMILY_PASS_DTL.HOST_NAME_E;
                    general_data.PROFESSION_A = rA42_FAMILY_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_FAMILY_PASS_DTL.PROFESSION_E;
                    general_data.NAME_A = rA42_FAMILY_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_FAMILY_PASS_DTL.NAME_E;
                    general_data.UNIT_A = rA42_FAMILY_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_FAMILY_PASS_DTL.UNIT_E;
                    general_data.PHONE_NUMBER = rA42_FAMILY_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_FAMILY_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_FAMILY_PASS_DTL.IDENTITY_CODE;
                    general_data.GENDER_ID = rA42_FAMILY_PASS_DTL.GENDER_ID;
                    general_data.PASS_TYPE_CODE = rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_FAMILY_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_FAMILY_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_FAMILY_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_FAMILY_PASS_DTL.REMARKS;
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
                    general_data.STATUS = general_data.STATUS;
                    general_data.ISPRINTED = general_data.ISPRINTED;
                    general_data.ISOPENED = true;
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
                    if (general_data.STATION_CODE == 26 && (general_data.CARD_FOR_CODE == 1))
                    {
                        //if request reach this stage, then the security ooficer will complete the requet and redirect it to the permit cell
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_FAMILY_PASS_DTL.FAMILY_CODE });

                        }
                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.SERVICE_NUMBER = rA42_FAMILY_PASS_DTL.SERVICE_NUMBER;
                        general_data.CIVIL_NUMBER = rA42_FAMILY_PASS_DTL.CIVIL_NUMBER;
                        general_data.BUILDING_NUMBER = rA42_FAMILY_PASS_DTL.BUILDING_NUMBER;
                        general_data.HOST_RANK_A = rA42_FAMILY_PASS_DTL.HOST_RANK_A;
                        general_data.HOST_RANK_E = rA42_FAMILY_PASS_DTL.HOST_RANK_E;
                        general_data.HOST_NAME_A = rA42_FAMILY_PASS_DTL.HOST_NAME_A;
                        general_data.HOST_NAME_E = rA42_FAMILY_PASS_DTL.HOST_NAME_E;
                        general_data.PROFESSION_A = rA42_FAMILY_PASS_DTL.PROFESSION_A;
                        general_data.PROFESSION_E = rA42_FAMILY_PASS_DTL.PROFESSION_E;
                        general_data.NAME_A = rA42_FAMILY_PASS_DTL.NAME_A;
                        general_data.NAME_E = rA42_FAMILY_PASS_DTL.NAME_E;
                        general_data.UNIT_A = rA42_FAMILY_PASS_DTL.UNIT_A;
                        general_data.UNIT_E = rA42_FAMILY_PASS_DTL.UNIT_E;
                        general_data.PHONE_NUMBER = rA42_FAMILY_PASS_DTL.PHONE_NUMBER;
                        general_data.GSM = rA42_FAMILY_PASS_DTL.GSM;
                        general_data.IDENTITY_CODE = rA42_FAMILY_PASS_DTL.IDENTITY_CODE;
                        general_data.GENDER_ID = rA42_FAMILY_PASS_DTL.GENDER_ID;
                        general_data.PASS_TYPE_CODE = rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE;
                        general_data.DATE_FROM = rA42_FAMILY_PASS_DTL.DATE_FROM;
                        general_data.DATE_TO = rA42_FAMILY_PASS_DTL.DATE_TO;
                        general_data.PURPOSE_OF_PASS = rA42_FAMILY_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_FAMILY_PASS_DTL.REMARKS;
                        general_data.APPROVAL_SN = general_data.APPROVAL_SN;
                        general_data.APPROVAL_NAME = general_data.APPROVAL_NAME;
                        general_data.APPROVAL_RANK = general_data.APPROVAL_RANK;
                        general_data.APPROVAL_APPROVISION_DATE = general_data.APPROVAL_APPROVISION_DATE;
                        general_data.PERMIT_SN = currentUserInfo["user_sno"];
                        general_data.PERMIT_NAME = currentUserInfo["user_name_ar"];
                        general_data.PERMIT_RANK = currentUserInfo["user_rank_ar"];
                        general_data.PERMIT_APPROVISION_DATE = DateTime.Now;
                        general_data.AUTHO_SN = general_data.AUTHO_SN;
                        general_data.AUTHO_NAME = general_data.AUTHO_NAME;
                        general_data.AUTHO_RANK = general_data.AUTHO_RANK;
                        general_data.AUTHO_APPROVISION_DATE = general_data.AUTHO_APPROVISION_DATE;
                        string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                        general_data.BARCODE = barcode;
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
                    else
                    {
                        //if status of permit is true, that means the request is completed and no need to redirect it to the security officer
                        if (general_data.STATUS == true)
                        {
                            general_data.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;
                        }
                        else
                        {
                            //if request not completed, the request should be redirected to the security officer
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Edit", new { id = rA42_FAMILY_PASS_DTL.FAMILY_CODE });

                            }
                            general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;

                        }

                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.SERVICE_NUMBER = rA42_FAMILY_PASS_DTL.SERVICE_NUMBER;
                        general_data.BUILDING_NUMBER = rA42_FAMILY_PASS_DTL.BUILDING_NUMBER;
                        general_data.CIVIL_NUMBER = rA42_FAMILY_PASS_DTL.CIVIL_NUMBER;
                        general_data.HOST_RANK_A = rA42_FAMILY_PASS_DTL.HOST_RANK_A;
                        general_data.HOST_RANK_E = rA42_FAMILY_PASS_DTL.HOST_RANK_E;
                        general_data.HOST_NAME_A = rA42_FAMILY_PASS_DTL.HOST_NAME_A;
                        general_data.HOST_NAME_E = rA42_FAMILY_PASS_DTL.HOST_NAME_E;
                        general_data.PROFESSION_A = rA42_FAMILY_PASS_DTL.PROFESSION_A;
                        general_data.PROFESSION_E = rA42_FAMILY_PASS_DTL.PROFESSION_E;
                        general_data.NAME_A = rA42_FAMILY_PASS_DTL.NAME_A;
                        general_data.NAME_E = rA42_FAMILY_PASS_DTL.NAME_E;
                        general_data.UNIT_A = rA42_FAMILY_PASS_DTL.UNIT_A;
                        general_data.UNIT_E = rA42_FAMILY_PASS_DTL.UNIT_E;
                        general_data.PHONE_NUMBER = rA42_FAMILY_PASS_DTL.PHONE_NUMBER;
                        general_data.GSM = rA42_FAMILY_PASS_DTL.GSM;
                        general_data.IDENTITY_CODE = rA42_FAMILY_PASS_DTL.IDENTITY_CODE;
                        general_data.GENDER_ID = rA42_FAMILY_PASS_DTL.GENDER_ID;
                        general_data.PASS_TYPE_CODE = rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE;
                        general_data.DATE_FROM = rA42_FAMILY_PASS_DTL.DATE_FROM;
                        general_data.DATE_TO = rA42_FAMILY_PASS_DTL.DATE_TO;
                        general_data.PURPOSE_OF_PASS = rA42_FAMILY_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_FAMILY_PASS_DTL.REMARKS;
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
                        general_data.ISPRINTED = false;
                        general_data.ISOPENED = true;

                        general_data.STATUS = general_data.STATUS;
                        db.Entry(general_data).State = EntityState.Modified;
                        db.SaveChanges();
                        AddToast(new Toast("",
                        GetResourcesValue("success_update_message"),
                        "green"));
                        return RedirectToAction("Newpasses");
                    }
                }
                //this section is for security ooficer
                if (form["approvebtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4)
                {




                   //if request reach this stage, then the security ooficer will complete the requet and redirect it to the permit cell
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return RedirectToAction("Edit", new { id = rA42_FAMILY_PASS_DTL.FAMILY_CODE });

                    }
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.SERVICE_NUMBER = rA42_FAMILY_PASS_DTL.SERVICE_NUMBER;
                    general_data.CIVIL_NUMBER = rA42_FAMILY_PASS_DTL.CIVIL_NUMBER;
                    general_data.BUILDING_NUMBER = rA42_FAMILY_PASS_DTL.BUILDING_NUMBER;
                    general_data.HOST_RANK_A = rA42_FAMILY_PASS_DTL.HOST_RANK_A;
                    general_data.HOST_RANK_E = rA42_FAMILY_PASS_DTL.HOST_RANK_E;
                    general_data.HOST_NAME_A = rA42_FAMILY_PASS_DTL.HOST_NAME_A;
                    general_data.HOST_NAME_E = rA42_FAMILY_PASS_DTL.HOST_NAME_E;
                    general_data.PROFESSION_A = rA42_FAMILY_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_FAMILY_PASS_DTL.PROFESSION_E;
                    general_data.NAME_A = rA42_FAMILY_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_FAMILY_PASS_DTL.NAME_E;
                    general_data.UNIT_A = rA42_FAMILY_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_FAMILY_PASS_DTL.UNIT_E;
                    general_data.PHONE_NUMBER = rA42_FAMILY_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_FAMILY_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_FAMILY_PASS_DTL.IDENTITY_CODE;
                    general_data.GENDER_ID = rA42_FAMILY_PASS_DTL.GENDER_ID;
                    general_data.PASS_TYPE_CODE = rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_FAMILY_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_FAMILY_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_FAMILY_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_FAMILY_PASS_DTL.REMARKS;
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
                    general_data.BARCODE = rA42_FAMILY_PASS_DTL.BARCODE;
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

                //all other code is when authorized user reject the request
                if (form["rejectbtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2)
                {


                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    //if (v == null)
                    //{
                    //    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    //    return RedirectToAction("Edit", new { id = rA42_FAMILY_PASS_DTL.FAMILY_CODE });

                    //}
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    if (v != null)
                    {
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    }
                    else
                    {
                        general_data.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;
                    }
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.SERVICE_NUMBER = rA42_FAMILY_PASS_DTL.SERVICE_NUMBER;
                    general_data.CIVIL_NUMBER = rA42_FAMILY_PASS_DTL.CIVIL_NUMBER;
                    general_data.HOST_RANK_A = rA42_FAMILY_PASS_DTL.HOST_RANK_A;
                    general_data.HOST_RANK_E = rA42_FAMILY_PASS_DTL.HOST_RANK_E;
                    general_data.HOST_NAME_A = rA42_FAMILY_PASS_DTL.HOST_NAME_A;
                    general_data.HOST_NAME_E = rA42_FAMILY_PASS_DTL.HOST_NAME_E;
                    general_data.PROFESSION_A = rA42_FAMILY_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_FAMILY_PASS_DTL.PROFESSION_E;
                    general_data.NAME_A = rA42_FAMILY_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_FAMILY_PASS_DTL.NAME_E;
                    general_data.UNIT_A = rA42_FAMILY_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_FAMILY_PASS_DTL.UNIT_E;
                    general_data.BUILDING_NUMBER = rA42_FAMILY_PASS_DTL.BUILDING_NUMBER;
                    general_data.PHONE_NUMBER = rA42_FAMILY_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_FAMILY_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_FAMILY_PASS_DTL.IDENTITY_CODE;
                    general_data.GENDER_ID = rA42_FAMILY_PASS_DTL.GENDER_ID;
                    general_data.PASS_TYPE_CODE = rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_FAMILY_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_FAMILY_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_FAMILY_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_FAMILY_PASS_DTL.REMARKS;
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
                    general_data.REJECTED = true;
                    general_data.STATUS = false;
                    general_data.ISOPENED = false;

                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Authopasses");
                }

                if (form["rejectbtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3)
                {


                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == general_data.APPROVAL_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    //if (v == null)
                    //{
                    //    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    //    return RedirectToAction("Edit", new { id = rA42_FAMILY_PASS_DTL.FAMILY_CODE });

                    //}
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    if (v != null)
                    {
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    }
                    else
                    {
                        general_data.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;
                    }
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.SERVICE_NUMBER = rA42_FAMILY_PASS_DTL.SERVICE_NUMBER;
                    general_data.CIVIL_NUMBER = rA42_FAMILY_PASS_DTL.CIVIL_NUMBER;
                    general_data.HOST_RANK_A = rA42_FAMILY_PASS_DTL.HOST_RANK_A;
                    general_data.HOST_RANK_E = rA42_FAMILY_PASS_DTL.HOST_RANK_E;
                    general_data.HOST_NAME_A = rA42_FAMILY_PASS_DTL.HOST_NAME_A;
                    general_data.HOST_NAME_E = rA42_FAMILY_PASS_DTL.HOST_NAME_E;
                    general_data.PROFESSION_A = rA42_FAMILY_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_FAMILY_PASS_DTL.PROFESSION_E;
                    general_data.NAME_A = rA42_FAMILY_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_FAMILY_PASS_DTL.NAME_E;
                    general_data.UNIT_A = rA42_FAMILY_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_FAMILY_PASS_DTL.UNIT_E;
                    general_data.BUILDING_NUMBER = rA42_FAMILY_PASS_DTL.BUILDING_NUMBER;
                    general_data.PHONE_NUMBER = rA42_FAMILY_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_FAMILY_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_FAMILY_PASS_DTL.IDENTITY_CODE;
                    general_data.GENDER_ID = rA42_FAMILY_PASS_DTL.GENDER_ID;
                    general_data.PASS_TYPE_CODE = rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_FAMILY_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_FAMILY_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_FAMILY_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_FAMILY_PASS_DTL.REMARKS;
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
                    general_data.REJECTED = true;
                    general_data.STATUS = false;
                    db.Entry(general_data).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Newpasses");
                }

                if (form["rejectbtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4)
                {


                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == general_data.PERMIT_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    //if (v == null)
                    //{
                    //    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    //    return RedirectToAction("Edit", new { id = rA42_FAMILY_PASS_DTL.FAMILY_CODE });

                    //}
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    if (v != null)
                    {
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    }
                    else
                    {
                        general_data.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;
                    }
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.SERVICE_NUMBER = rA42_FAMILY_PASS_DTL.SERVICE_NUMBER;
                    general_data.CIVIL_NUMBER = rA42_FAMILY_PASS_DTL.CIVIL_NUMBER;
                    general_data.HOST_RANK_A = rA42_FAMILY_PASS_DTL.HOST_RANK_A;
                    general_data.HOST_RANK_E = rA42_FAMILY_PASS_DTL.HOST_RANK_E;
                    general_data.HOST_NAME_A = rA42_FAMILY_PASS_DTL.HOST_NAME_A;
                    general_data.HOST_NAME_E = rA42_FAMILY_PASS_DTL.HOST_NAME_E;
                    general_data.PROFESSION_A = rA42_FAMILY_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_FAMILY_PASS_DTL.PROFESSION_E;
                    general_data.NAME_A = rA42_FAMILY_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_FAMILY_PASS_DTL.NAME_E;
                    general_data.UNIT_A = rA42_FAMILY_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_FAMILY_PASS_DTL.UNIT_E;
                    general_data.BUILDING_NUMBER = rA42_FAMILY_PASS_DTL.BUILDING_NUMBER;
                    //general_data.GATE_NUMBER = rA42_FAMILY_PASS_DTL.GATE_NUMBER;
                    general_data.PHONE_NUMBER = rA42_FAMILY_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_FAMILY_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_FAMILY_PASS_DTL.IDENTITY_CODE;
                    general_data.GENDER_ID = rA42_FAMILY_PASS_DTL.GENDER_ID;
                    general_data.PASS_TYPE_CODE = rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_FAMILY_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_FAMILY_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_FAMILY_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_FAMILY_PASS_DTL.REMARKS;
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
                    general_data.REJECTED = true;
                    general_data.STATUS = false;
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
            return View(rA42_FAMILY_PASS_DTL);
		}

        //renew permit view
        public ActionResult Renew(int? id)
        {
            ViewBag.activetab = "renew";

            if (id == null)
            {
                return NotFound();
            }
            RA42_FAMILY_PASS_DTL rA42_FAMILY_PASS_DTL = db.RA42_FAMILY_PASS_DTL.Find(id);
            if (rA42_FAMILY_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if user has permission to edit this permit 
            if (rA42_FAMILY_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_FAMILY_PASS_DTL.RESPONSIBLE == currentUser)
            {
                if (rA42_FAMILY_PASS_DTL.DATE_TO != null)
                {

                    string date = rA42_FAMILY_PASS_DTL.CheckDate(rA42_FAMILY_PASS_DTL.DATE_TO.Value);
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
                if (ViewBag.RESPO_STATE != rA42_FAMILY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                {
                    if (rA42_FAMILY_PASS_DTL.DATE_TO != null)
                    {

                        string date = rA42_FAMILY_PASS_DTL.CheckDate(rA42_FAMILY_PASS_DTL.DATE_TO.Value);
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
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_FAMILY_PASS_DTL.IDENTITY_CODE);
                //get permits types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                if (rA42_FAMILY_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get zones and gates in en 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in en 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_FAMILY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_FAMILY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_FAMILY_PASS_DTL.GENDER_ID);
                //get relatives types in english
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_FAMILY_PASS_DTL.RELATIVE_TYPE_CODE);
                //get gates in english 
               // ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME_E");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME_E");
                //get autho person for this kind of permits in english
                //this option is for applicant, if he want to change autho person (المنسق الأمني) before he proccess the request 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (rA42_FAMILY_PASS_DTL.STATION_CODE != 26)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            //return View();
                        }
                    }

                }

               



            }
            else
            {
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_FAMILY_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                if (rA42_FAMILY_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get zones and gates in ar 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_FAMILY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_FAMILY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_FAMILY_PASS_DTL.GENDER_ID);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_FAMILY_PASS_DTL.RELATIVE_TYPE_CODE);
                //get gates in arabic 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME");
                //get autho person for this kind of permits in arabic
                //this option is for applicant, if he want to change autho person (المنسق الأمني) before he proccess the request
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_FAMILY_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (rA42_FAMILY_PASS_DTL.STATION_CODE != 26)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            //return View();
                        }
                    }
                }
               


            }
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (rA42_FAMILY_PASS_DTL.STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get documents types for this kind of permit, we need this to compare it with missing files 
            // ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_FAMILY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_FAMILY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_FAMILY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_FAMILY_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            //get selected gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get relatives 
            ViewBag.GetRelativs = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.ACCESS_ROW_CODE == id && a.DLT_STS != true).ToList();
            //get documents
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get personal image 
            ViewBag.PERSONAL_IMAGE = rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE;

            rA42_FAMILY_PASS_DTL.DATE_FROM = null;
            rA42_FAMILY_PASS_DTL.DATE_TO = null;

            return View(rA42_FAMILY_PASS_DTL);
        }

        //post new data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Renew(RA42_FAMILY_PASS_DTL rA42_FAMILY_PASS_DTL, int[] RELATIVE_TYPES, HttpPostedFileBase[] RELATIVE_IMAGE, int[] IDENTITY_TYPES, int[] GENDER_TYPES, 
            string[] FULL_NAME, string[] CIVIL_NUM, string[] PASSPORT_NUMBER, string[] PHONE_NUMBER_M, string[] REMARKS_LIST,
            int[] FILE_TYPES, string[] FILE_TYPES_TEXT, int[] ZONE, int FAMILY_ID, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            var general_data = db.RA42_FAMILY_PASS_DTL.Where(a => a.FAMILY_CODE == FAMILY_ID).FirstOrDefault();
            ViewBag.activetab = "renew";

            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (general_data.STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }

            //get documents types for this kind of permit, we need this to compare it with missing files 
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
            //get selected gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == FAMILY_ID && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get relatives 
            ViewBag.GetRelativs = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.ACCESS_ROW_CODE == FAMILY_ID && a.DLT_STS != true).ToList();
            //get documents
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == FAMILY_ID && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get comments
           
            //get personal image 
            ViewBag.PERSONAL_IMAGE = rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE;
            //get status of the permit 
           

            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_FAMILY_PASS_DTL.IDENTITY_CODE);
                //get permits types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                if (general_data.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get zones and gates in english
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
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
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_FAMILY_PASS_DTL.GENDER_ID);
                //get relatives types in english
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_FAMILY_PASS_DTL.RELATIVE_TYPE_CODE);
                //get gates in english 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME_E");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME_E");
                //get autho person for this kind of permits in english
                //this option is for applicant, if he want to change autho person (المنسق الأمني) before he proccess the request 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (general_data.STATION_CODE != 26)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(general_data);
                        }
                    }
                }

               

            }
            else
            {
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_FAMILY_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_FAMILY_PASS_DTL.PASS_TYPE_CODE);
                if (general_data.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get zones and gates in ar
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
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
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_FAMILY_PASS_DTL.GENDER_ID);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_FAMILY_PASS_DTL.RELATIVE_TYPE_CODE);
                //get gates in arabic 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME");
                //get autho person for this kind of permits in arabic
                //this option is for applicant, if he want to change autho person (المنسق الأمني) before he proccess the request
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (general_data.STATION_CODE != 26)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(general_data);
                        }
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

                                fileName = "Profile_4_" + DateTime.Now.ToString("yymmssfff") + extension;
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
                                rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE = fileName;



                            }
                            else
                            {
                                //show error message if image extention not supported 
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

               



               
                    //add new personal image in case user upload new one successfully
                    if (rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE != null)
                    {
                        rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE = rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE;
                    }
                    else
                    {
                        rA42_FAMILY_PASS_DTL.PERSONAL_IMAGE = general_data.PERSONAL_IMAGE;

                    }
                    //get host details from api
                    rA42_FAMILY_PASS_DTL.SERVICE_NUMBER = rA42_FAMILY_PASS_DTL.SERVICE_NUMBER.ToUpper();
                    rA42_FAMILY_PASS_DTL.RESPONSIBLE = currentUser;
                    rA42_FAMILY_PASS_DTL.HOST_RANK_A = rA42_FAMILY_PASS_DTL.HOST_RANK_A;
                    rA42_FAMILY_PASS_DTL.HOST_RANK_E = rA42_FAMILY_PASS_DTL.HOST_RANK_E;
                    rA42_FAMILY_PASS_DTL.HOST_NAME_A = rA42_FAMILY_PASS_DTL.HOST_NAME_A;
                    rA42_FAMILY_PASS_DTL.HOST_NAME_E = rA42_FAMILY_PASS_DTL.HOST_NAME_E;
                    rA42_FAMILY_PASS_DTL.PROFESSION_A = rA42_FAMILY_PASS_DTL.PROFESSION_A;
                    rA42_FAMILY_PASS_DTL.PROFESSION_E = rA42_FAMILY_PASS_DTL.PROFESSION_E;
                    rA42_FAMILY_PASS_DTL.NAME_A = rA42_FAMILY_PASS_DTL.NAME_A;
                    rA42_FAMILY_PASS_DTL.NAME_E = rA42_FAMILY_PASS_DTL.NAME_E;
                    rA42_FAMILY_PASS_DTL.UNIT_A = rA42_FAMILY_PASS_DTL.UNIT_A;
                    rA42_FAMILY_PASS_DTL.UNIT_E = rA42_FAMILY_PASS_DTL.UNIT_E;
                    rA42_FAMILY_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                    rA42_FAMILY_PASS_DTL.STATION_CODE = general_data.STATION_CODE;
                    rA42_FAMILY_PASS_DTL.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    //get current user details from api 
                    User permit = null;
                    Task<User> callTask2 = Task.Run(
                        () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                        );
                    callTask2.Wait();
                    permit = callTask2.Result;
                    //this section for applicant, workflow id <= 1
                    if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                    {

                    if (STATION_CODE == 26)
                    {
                        //he should redirect this request to the permits cell 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_FAMILY_PASS_DTL);

                        }
                        else
                        {
                            rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }

                        rA42_FAMILY_PASS_DTL.APPROVAL_SN = currentUser;
                        rA42_FAMILY_PASS_DTL.APPROVAL_RANK = permit.NAME_RANK_A;
                        rA42_FAMILY_PASS_DTL.APPROVAL_NAME = permit.NAME_EMP_A;
                        rA42_FAMILY_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;

                        rA42_FAMILY_PASS_DTL.REJECTED = false;
                        rA42_FAMILY_PASS_DTL.STATUS = false;
                        rA42_FAMILY_PASS_DTL.ISOPENED = true;
                    }
                    else
                    {

                        rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE;
                        rA42_FAMILY_PASS_DTL.REJECTED = false;
                        rA42_FAMILY_PASS_DTL.STATUS = false;
                        rA42_FAMILY_PASS_DTL.ISOPENED = false;
                    }
                }
                    //this section is for autho person (المنسق الأمني) his workflow id is 2
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
                            rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }

                        rA42_FAMILY_PASS_DTL.APPROVAL_SN = currentUser;
                        rA42_FAMILY_PASS_DTL.APPROVAL_RANK = permit.NAME_RANK_A;
                        rA42_FAMILY_PASS_DTL.APPROVAL_NAME = permit.NAME_EMP_A;
                        rA42_FAMILY_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;

                        rA42_FAMILY_PASS_DTL.REJECTED = false;
                        rA42_FAMILY_PASS_DTL.STATUS = false;
                        rA42_FAMILY_PASS_DTL.ISOPENED = true;
                    }
                    //this section is for permits cell,
                    if (WORKFLOWID == 3)
                    {
                    if (general_data.STATION_CODE == 26 && general_data.CARD_FOR_CODE == 1)
                    {
                        //after the security officer create permit, the request will be completed and should be redirected to the permits cell
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(general_data);

                        }
                        else
                        {
                            rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }

                        rA42_FAMILY_PASS_DTL.PERMIT_SN = currentUser;
                        rA42_FAMILY_PASS_DTL.PERMIT_RANK = permit.NAME_RANK_A;
                        rA42_FAMILY_PASS_DTL.PERMIT_NAME = permit.NAME_EMP_A;
                        rA42_FAMILY_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                        rA42_FAMILY_PASS_DTL.REJECTED = false;
                        rA42_FAMILY_PASS_DTL.STATUS = true;
                        rA42_FAMILY_PASS_DTL.ISOPENED = true;
                        string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                        rA42_FAMILY_PASS_DTL.BARCODE = barcode;
                    }
                    else
                    {
                        //he should redirect the request to the security officer
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(general_data);

                        }
                        else
                        {
                            rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }

                        rA42_FAMILY_PASS_DTL.PERMIT_SN = currentUser;
                        rA42_FAMILY_PASS_DTL.PERMIT_RANK = permit.NAME_RANK_A;
                        rA42_FAMILY_PASS_DTL.PERMIT_NAME = permit.NAME_EMP_A;
                        rA42_FAMILY_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;

                        rA42_FAMILY_PASS_DTL.REJECTED = false;
                        rA42_FAMILY_PASS_DTL.STATUS = false;
                        rA42_FAMILY_PASS_DTL.ISOPENED = true;
                    }
                    }
                    //this section is for security officer
                    if (WORKFLOWID == 4)
                    {
                        //after the security officer create permit, the request will be completed and should be redirected to the permits cell
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(general_data);

                        }
                        else
                        {
                            rA42_FAMILY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }

                        rA42_FAMILY_PASS_DTL.AUTHO_SN = currentUser;
                        rA42_FAMILY_PASS_DTL.AUTHO_RANK = permit.NAME_RANK_A;
                        rA42_FAMILY_PASS_DTL.AUTHO_NAME = permit.NAME_EMP_A;
                        rA42_FAMILY_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                        rA42_FAMILY_PASS_DTL.REJECTED = false;
                        rA42_FAMILY_PASS_DTL.STATUS = true;
                        rA42_FAMILY_PASS_DTL.ISOPENED = true;
                        rA42_FAMILY_PASS_DTL.BARCODE = rA42_FAMILY_PASS_DTL.BARCODE;
                    }
                    rA42_FAMILY_PASS_DTL.CRD_BY = currentUser;
                    rA42_FAMILY_PASS_DTL.CRD_DT = DateTime.Now;
                    rA42_FAMILY_PASS_DTL.UPD_BY = currentUser;
                    rA42_FAMILY_PASS_DTL.UPD_DT = DateTime.Now;
                    db.RA42_FAMILY_PASS_DTL.Add(rA42_FAMILY_PASS_DTL);
                    db.SaveChanges();

                //add relatives 
                RA42_MEMBERS_DTL rA42_MEMBERS_DTL = new RA42_MEMBERS_DTL();
                if (IDENTITY_TYPES != null && !IDENTITY_TYPES.Contains(0))
                {
                    try
                    {
                        for (int i = 0; i < RELATIVE_TYPES.Length; i++)
                        {

                            //create barcode for every relative
                            string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                            rA42_MEMBERS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                            rA42_MEMBERS_DTL.ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE;
                            rA42_MEMBERS_DTL.CIVIL_NUMBER = CIVIL_NUM[i];
                            rA42_MEMBERS_DTL.FULL_NAME = FULL_NAME[i];
                            rA42_MEMBERS_DTL.PHONE_NUMBER = PHONE_NUMBER_M[i];
                            rA42_MEMBERS_DTL.PASSPORT_NUMBER = PASSPORT_NUMBER[i];
                            rA42_MEMBERS_DTL.RELATIVE_TYPE_CODE = RELATIVE_TYPES[i];
                            rA42_MEMBERS_DTL.IDENTITY_CODE = IDENTITY_TYPES[i];
                            rA42_MEMBERS_DTL.GENDER_ID = GENDER_TYPES[i];
                            rA42_MEMBERS_DTL.REMARKS = REMARKS_LIST[i];

                            try
                            {

                                // Verify that the user selected a file
                                if (RELATIVE_IMAGE[i].ContentLength > 0)
                                {
                                    // extract only the filename with extention
                                    string fileName = Path.GetFileNameWithoutExtension(RELATIVE_IMAGE[i].FileName);
                                    string extension = Path.GetExtension(RELATIVE_IMAGE[i].FileName);


                                    //check the extention of the image file 
                                    if (general.CheckPersonalImage(RELATIVE_IMAGE[i].FileName))
                                    {

                                        fileName = "Relative_Profile_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                                        // store the file inside ~/Files/Profiles/ folder
                                        bool check = general.ResizeImage(RELATIVE_IMAGE[i], fileName);

                                        if (check != true)
                                        {
                                            AddToast(new Toast("",
                                           GetResourcesValue("error_update_message"),
                                           "red"));
                                            TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                            return View(rA42_FAMILY_PASS_DTL);
                                        }

                                        rA42_MEMBERS_DTL.PERSONAL_IMAGE = fileName;


                                    }
                                    else
                                    {
                                        //if format not supported, show error message 
                                        AddToast(new Toast("",
                                        GetResourcesValue("error_update_message"),
                                        "red"));
                                        TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                        return View(rA42_FAMILY_PASS_DTL);
                                    }
                                }
                            }

                            catch (Exception ex)
                            {
                                ex.GetBaseException();
                            }

                            rA42_MEMBERS_DTL.CRD_BY = currentUser;
                            rA42_MEMBERS_DTL.CRD_DT = DateTime.Now;
                            rA42_MEMBERS_DTL.UPD_BY = currentUser;
                            rA42_MEMBERS_DTL.UPD_DT = DateTime.Now;
                            db.RA42_MEMBERS_DTL.Add(rA42_MEMBERS_DTL);
                            db.SaveChanges();
                            //continue;
                        }



                    }
                    catch (IndexOutOfRangeException ex)
                    {
                        TempData["Erorr"] = ex.GetBaseException().Message + RELATIVE_TYPES.Length + " - " + IDENTITY_TYPES.Length + " - " +
                            GENDER_TYPES.Length + " - " + FULL_NAME.Length + " - " + REMARKS_LIST.Length + " - " + CIVIL_NUM.Length;
                        return View(rA42_FAMILY_PASS_DTL);
                    }
                }

                var relatevies = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_ROW_CODE == FAMILY_ID && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
                //add previues relatives to new relatives
                foreach (var relative in relatevies)
                {
                    rA42_MEMBERS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                    rA42_MEMBERS_DTL.ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE;
                    rA42_MEMBERS_DTL.CIVIL_NUMBER = relative.CIVIL_NUMBER;
                    rA42_MEMBERS_DTL.PASSPORT_NUMBER = relative.PASSPORT_NUMBER;
                    rA42_MEMBERS_DTL.PHONE_NUMBER = relative.PHONE_NUMBER;
                    rA42_MEMBERS_DTL.FULL_NAME = relative.FULL_NAME;
                    rA42_MEMBERS_DTL.RELATIVE_TYPE_CODE = relative.RELATIVE_TYPE_CODE;
                    rA42_MEMBERS_DTL.IDENTITY_CODE = relative.IDENTITY_CODE;
                    rA42_MEMBERS_DTL.GENDER_ID = relative.GENDER_ID;
                    rA42_MEMBERS_DTL.REMARKS = relative.REMARKS;
                    rA42_MEMBERS_DTL.CRD_BY = currentUser;
                    rA42_MEMBERS_DTL.CRD_DT = DateTime.Now;
                    db.RA42_MEMBERS_DTL.Add(rA42_MEMBERS_DTL);
                    db.SaveChanges();
                }

                //add selected zones and gates
                RA42_ZONE_MASTER_MST rA42_ZONE_MASTER_MST = new RA42_ZONE_MASTER_MST();
                if (ZONE != null && !ZONE.Contains(0))
                {
                    for (int i = 0; i < ZONE.Length; i++)
                    {


                        rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE;
                        rA42_ZONE_MASTER_MST.ZONE_CODE = ZONE[i];
                        rA42_ZONE_MASTER_MST.ZONE_SUB_CODE = 0;
                        rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                        rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                        db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                        db.SaveChanges();
                        //continue;
                    }

                }
                var zones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == FAMILY_ID && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
                //add previues zones to new zone
                foreach (var zone in zones)
                {
                    rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE;
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
                                        ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE,
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
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return RedirectToAction("Renew", new { id = FAMILY_ID });
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
                var selected_files = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == FAMILY_ID && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
                foreach (var file in selected_files)
                {
                    //add new file
                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                    {
                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                        ACCESS_ROW_CODE = rA42_FAMILY_PASS_DTL.FAMILY_CODE,
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

        //this is delete view 
        public ActionResult Delete(int? id)
		{
            ViewBag.activetab = "delete";

            if (id == null)
            {
                return NotFound();
            }
            RA42_FAMILY_PASS_DTL rA42_FAMILY_PASS_DTL = db.RA42_FAMILY_PASS_DTL.Find(id);
            if (rA42_FAMILY_PASS_DTL == null)
            {
                
                    return NotFound();
                
            }

            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_FAMILY_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_FAMILY_PASS_DTL.RESPONSIBLE != currentUser)
                {
                    if (rA42_FAMILY_PASS_DTL.STATUS == true)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {

                            return NotFound();
                        }
                    }
                }
                if (rA42_FAMILY_PASS_DTL.ISOPENED == true)
                {
                    if (rA42_FAMILY_PASS_DTL.STATUS == true)
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
                if (rA42_FAMILY_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_FAMILY_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    //if (ViewBag.RESPO_STATE != rA42_FAMILY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                    //{
                    //    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    //    {
                    //        return NotFound();
                    //    }
                    //}
                }
            }
            //get gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get relatives
            ViewBag.GetRelativs = db.RA42_RELATIVE_MST.Where(a => a.FAMILY_CODE == rA42_FAMILY_PASS_DTL.FAMILY_CODE && a.DLT_STS != true).ToList();
            //get documents
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
         

            return View(rA42_FAMILY_PASS_DTL);
		}

		// confirm delete
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public ActionResult DeleteConfirmed(int id)
		{
            var general_data = db.RA42_FAMILY_PASS_DTL.Where(a => a.FAMILY_CODE == id).FirstOrDefault();

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



       
        //delete any document uploaded to any request
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
        //delete any relatives
        [HttpPost]
        public JsonResult DeleteRelative(int id)
        {
            bool result = false;
            RA42_MEMBERS_DTL rA42_RELATIVE_MST = db.RA42_MEMBERS_DTL.Find(id);
            if (rA42_RELATIVE_MST != null)
            {
                db.RA42_MEMBERS_DTL.Remove(rA42_RELATIVE_MST);
                db.SaveChanges();
                AddToast(new Toast("",
                GetResourcesValue("success_delete_message"),
               "green"));
                result = true;
            }

            return Json(result, JsonRequestBehavior.AllowGet);


        }
        //delete personal image 
        [HttpPost]
        public JsonResult DeleteImage(int id)
        {
            bool result = false;
            var general_data = db.RA42_FAMILY_PASS_DTL.Where(a => a.FAMILY_CODE == id).FirstOrDefault();

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

        //delete personal image 
        [HttpPost]
        public JsonResult DeleteRelativeImage(int id)
        {
            bool result = false;
            var general_data = db.RA42_MEMBERS_DTL.Where(a => a.MEMBER_CODE == id).FirstOrDefault();

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
        //delete any gate
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
        //print id card 
        [HttpPost]
        public JsonResult PrintById(int id, string type)
        {

            bool result = false;
            var general_data = db.RA42_FAMILY_PASS_DTL.Where(a => a.FAMILY_CODE == id).FirstOrDefault();

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
        public ActionResult NotFound()
        {
            return RedirectToAction("NotFound", "Home");
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
