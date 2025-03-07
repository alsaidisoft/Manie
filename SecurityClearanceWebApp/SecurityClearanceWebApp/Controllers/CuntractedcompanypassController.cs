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

//this controller for controlling cuntracted companies permits 
namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    public class CuntractedcompanypassController : Controller
    {
        //create db connection 
        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class
        private GeneralFunctions general = new GeneralFunctions();
        //identify some variables 
        private int STATION_CODE = 0;
        private int WORKFLOWID = 0;
        private int RESPO_CODE = 0;
        private int FORCE_ID = 0;
        //access type number in RA42_ACCESS_TYPE_MST 
        private int ACCESS_TYPE_CODE = 6;
        //company type code is 1 which is for cuntracting companies
        private int COMPANY_TYPE_CODE = 1;

        //title of controller
        private string title = Resources.Passes.ResourceManager.GetString("ContractedCompanyPass" + "_" + "ar");


        //constructor
        public CuntractedcompanypassController()
        {
            ViewBag.Managepasses = "Managepasses";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Cuntractedcompanypass";
            if (Language.GetCurrentLang() == "en") {
                title = Resources.Passes.ResourceManager.GetString("ContractedCompanyPass" + "_" + "en");
            }
            //icon of controller
            ViewBag.controllerIconClass = "fa fa-archway";
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;

            //get WORKFLOWID of current user 
            var v = Task.Run(async ()=> await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefaultAsync()).Result;
            if (v != null)
            {
                //check if user not deletd or disactivated
                if (v.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && v.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false)
                {
                    //get id of responsible 
                    ViewBag.RESPO_ID = v.WORKFLOW_RESPO_CODE;
                    //get WORKFLOW id of responsible 
                    ViewBag.RESPO_STATE = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID;
                    RESPO_CODE = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE;
                    WORKFLOWID = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value;
                    STATION_CODE = v.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE.Value;
                    FORCE_ID = v.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.FORCE_ID.Value;
                    ViewBag.STATION_CODE_CHECK = v.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE.Value;
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

                //FORCE_ID = 1;
            }

            
        }

        //comments view 
        public ActionResult Comments(int? id)
        {
            ViewBag.activetab = "Comments";

            if (id == null)
            {
                return NotFound();
            }
           

            RA42_CONTRACTING_COMPANIES_PASS_DTL rA42_COMPANY_PASS_DTL = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Find(id);
            if (rA42_COMPANY_PASS_DTL == null)
            {
                return NotFound();
            }
            // if current user not have authority to open tis permit view notfound page 
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
            

            //get all comments of this permit
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            return View(rA42_COMPANY_PASS_DTL);
        }

        //post new comment for this permit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Comments(RA42_CONTRACTING_COMPANIES_PASS_DTL rA42_COMPANY_PASS_DTL, string COMMENT)
        {
            ViewBag.activetab = "Comments";
            var general_data = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.CONTRACT_CODE == rA42_COMPANY_PASS_DTL.CONTRACT_CODE).FirstOrDefault();





            //add comments
            if (COMMENT.Length > 0)
            {
                RA42_COMMENTS_MST rA42_COMMENT = new RA42_COMMENTS_MST();
                rA42_COMMENT.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_COMMENT.PASS_ROW_CODE = rA42_COMPANY_PASS_DTL.CONTRACT_CODE;
                rA42_COMMENT.CRD_BY = currentUser;
                rA42_COMMENT.CRD_DT = DateTime.Now;
                rA42_COMMENT.COMMENT = COMMENT;
                db.RA42_COMMENTS_MST.Add(rA42_COMMENT);
                db.SaveChanges();
                AddToast(new Toast("",
                  GetResourcesValue("add_comment_success"),
                  "green"));

            }
            //get all commnts of this permit 
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_COMPANY_PASS_DTL.CONTRACT_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            return View(rA42_COMPANY_PASS_DTL);


        }

        // view links on different tabs
        public ActionResult Index()
        {
            
            return View();
        }
        //all companies pemits
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

         
            var empList = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.WORKFLOW_RESPO_CODE != null).Select(a => new
            {
                CONTRACT_CODE = a.CONTRACT_CODE,
                CIVIL_NUMBER = (a.ID_CARD_NUMBER != null ? a.ID_CARD_NUMBER : " "),
                PERSONAL_IMAGE = a.PERSONAL_IMAGE,
                COMPANY_A = (a.COMPANY_CODE != null ? a.RA42_COMPANY_MST.COMPANY_NAME : " "),
                COMPANY_E = (a.COMPANY_CODE != null ? a.RA42_COMPANY_MST.COMPANY_NAME_E : " "),
                NAME_A = (a.NAME_A != null ? a.NAME_A : " "),
                NAME_E = (a.NAME_E != null ? a.NAME_E : " "),
                GSM = (a.GSM != null ? a.GSM : " "),
                PURPOSE_OF_PASS = (a.PURPOSE_OF_PASS != null ? a.PURPOSE_OF_PASS : " "),
                PROFESSION_A = (a.PROFESSION_A != null ? a.PROFESSION_A : " "),
                PROFESSION_E = (a.PROFESSION_E != null ? a.PROFESSION_E : " "),
                STATION_CODE = a.STATION_CODE.Value,
                STATION_A = a.RA42_STATIONS_MST.STATION_NAME_A,
                STATION_E = a.RA42_STATIONS_MST.STATION_NAME_E,
                RESPONSEPLE_NAME = a.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                RESPONSEPLE_NAME_E = a.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E,
                STEP_NAME = a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME,
                STEP_NAME_E = a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E,
                STATUS = a.STATUS,
                DLT_STS = a.DLT_STS,
                ISPRINTED = a.ISPRINTED,
                REJECTED = a.REJECTED,
                DATE_FROM = a.DATE_FROM,
                DATE_TO = a.DATE_TO,
                COMMENTS = a.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == a.CONTRACT_CODE).Count()


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
                Where(x => x.CIVIL_NUMBER.Contains(searchValue) || x.NAME_A.Contains(searchValue) || x.NAME_E.Contains(searchValue) || x.STEP_NAME.Contains(searchValue) 
                || x.PROFESSION_A.Contains(searchValue) || x.PROFESSION_E.Contains(searchValue) || x.PURPOSE_OF_PASS.Contains(searchValue) 
                || x.COMPANY_A.Contains(searchValue) /*|| x.COMPANY_E.Contains(searchValue)*/ || x.GSM.Contains(searchValue) || x.STATION_A == searchValue).ToList();
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
        //get all permits for administrator 
        public ActionResult Allpasses()
        {

            ViewBag.activetab = "Allpasses";
            return View();
        }

        //get all permits for autho person (المنسق الأمني : ركن 1 هندسة)
        public async Task<ActionResult> Authopasses()
        {
            ViewBag.activetab = "Authopasses";
            var rA42_COMPANY_PASS_DTL = await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE).OrderByDescending(a => a.CONTRACT_CODE).ToListAsync();
            return View(rA42_COMPANY_PASS_DTL);
        }
        //get all new permits for permit cell and security wing leader
        public async Task<ActionResult> Newpasses()
        {
            ViewBag.activetab = "Newpasses";
            var rA42_COMPANY_PASS_DTL = await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID  && a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATUS !=true).OrderByDescending(a => a.CONTRACT_CODE).ToListAsync();
            return View(rA42_COMPANY_PASS_DTL.ToList());
        }

        public async Task<ActionResult> ToPrint()
        {
            ViewBag.activetab = "ToPrint";
            var rA42_COMPANY_PASS_DTL = await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID && a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATUS == true && a.ISPRINTED != true).OrderByDescending(a => a.CONTRACT_CODE).ToListAsync();
            return View(rA42_COMPANY_PASS_DTL);
        }
        //get all permits for permits cell 
        public async Task<ActionResult> Printed()
        {
            ViewBag.activetab = "Printed";
            var rA42_COMPANY_PASS_DTL = await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID && a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE  && a.ISPRINTED == true).OrderByDescending(a => a.CONTRACT_CODE).ToListAsync();
            return View(rA42_COMPANY_PASS_DTL);
        }
      
        //this method now not used beacuase cuntracting permits not have zones
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

        // get permit details


        //print card 
        public ActionResult Card(int? id)
        {
            ViewBag.activetab = "card";

            if (id == null)
            {
                return NotFound();
            }
            //check if permit stored in the table RA42_CONTRACTING_COMPANIES_PASS_DTL
            RA42_CONTRACTING_COMPANIES_PASS_DTL rA42_CONTRACTING_COMPANIES_PASS_DTL = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Find(id);
            if (rA42_CONTRACTING_COMPANIES_PASS_DTL == null)
            {
                return NotFound();
            }

           
                //check if card view  opened by uthorized person in permits cell 
                    if (ViewBag.RESPO_STATE != rA42_CONTRACTING_COMPANIES_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                    {
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    return NotFound();
                        }
                    }
                
            
            // get documents of this permit 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_CONTRACTING_COMPANIES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToList();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_CONTRACTING_COMPANIES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToList();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }

            return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
        }
        [HttpPost]
        public ActionResult Card(string CheckPrinted, int TRANSACTION_TYPE_CODE, string TRANSACTION_REMARKS, HttpPostedFileBase RECEIPT, RA42_CONTRACTING_COMPANIES_PASS_DTL _CONTRACTING_COMPANIES_PASS_DTL)
        {
            ViewBag.activetab = "card";

         
            //check if permit stored in the table RA42_CONTRACTING_COMPANIES_PASS_DTL
            RA42_CONTRACTING_COMPANIES_PASS_DTL rA42_CONTRACTING_COMPANIES_PASS_DTL = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Find(_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE);
            if (rA42_CONTRACTING_COMPANIES_PASS_DTL == null)
            {
                return NotFound();
            }


            //check if card view  opened by uthorized person in permits cell 
            if (ViewBag.RESPO_STATE != rA42_CONTRACTING_COMPANIES_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
            {
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    return NotFound();
                }
            }


            // get documents of this permit 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_CONTRACTING_COMPANIES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToList();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_CONTRACTING_COMPANIES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToList();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }

            RA42_TRANSACTION_DTL rA42_TRANSACTION_DTL = new RA42_TRANSACTION_DTL();
            rA42_TRANSACTION_DTL.ACCESS_ROW_CODE = _CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE;
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
                            return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
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
            rA42_CONTRACTING_COMPANIES_PASS_DTL.UPD_BY = currentUser;
            rA42_CONTRACTING_COMPANIES_PASS_DTL.UPD_DT = DateTime.Now;
            rA42_CONTRACTING_COMPANIES_PASS_DTL.ISDELIVERED = false;
            db.SaveChanges();
            TempData["Success"] = "تم تحديث المعاملة بنجاح";
            if (CheckPrinted.Equals("Printed"))
            {
                var deletePrinted = db.RA42_PRINT_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.PASS_ROW_CODE ==
                rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE).ToList();
                if (deletePrinted.Count > 0)
                {
                    foreach (var item in deletePrinted)
                    {
                        item.DLT_STS = true;
                        db.SaveChanges();
                    }
                }
            }

            return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
        }

        // create new permit by (مقدم الطلب)
        public ActionResult Create()
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

                FORCE_ID = check_unit.FORCE_ID.Value;


            }

            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();


            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //get type of permit in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true), "ZONE_CODE", "ZONE_NAME_E");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME_E");
                //get decuments type for this permit in english 
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
                //get cuntracted companies  for this kind of permit 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE), "COMPANY_CODE", "COMPANY_NAME_E");
                //get responsible who will recive this permit 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                //get gender list in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //check if WORKFLOW_RESPO.Count == 0 show error message 
                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    //return RedirectToAction("Create");

                }
            }
            else {
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true), "ZONE_CODE", "ZONE_NAME");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME");
                //get documents types in arabic 
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
                //get cuntracted companies in arabic 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE), "COMPANY_CODE", "COMPANY_NAME");
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //check if WORKFLOW_RESPO.Count == 0 show error message 

                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    //return RedirectToAction("Create");

                }
            }

            return View();
        }

        // POST: Companypass/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(RA42_CONTRACTING_COMPANIES_PASS_DTL rA42_CONTRACTING_COMPANIES_PASS_DTL,
             int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE
            )
        {
            ViewBag.activetab = "Create";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            //int unit = 0;

            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station name 
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

                FORCE_ID = check_unit.FORCE_ID.Value;


            }

            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();

            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E",rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE);
                //get permits types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E",rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE);
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true), "ZONE_CODE", "ZONE_NAME_E");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME_E");
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
                //get cuntracting companies in english 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE), "COMPANY_CODE", "COMPANY_NAME_E",rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE);
                //get responsible person (ركن 1 هندسة )  in english 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME",rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E",rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID);
                //check if WORKFLOW_RESPO.Count == 0 show error message 
                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);

                }
            }
            else
            {
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A",rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE);
                //get permites types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE",rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE);
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true), "ZONE_CODE", "ZONE_NAME");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME");
                //get documents types for this permit in arabic 
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
                //get cuntracting companies in arabic 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE), "COMPANY_CODE", "COMPANY_NAME",rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE);
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME",rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A",rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID);
                //check if WORKFLOW_RESPO.Count == 0 show error message 
                if (WORKFLOW_RESPO.Count == 0)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);

                }

            }

                
            if (ModelState.IsValid)
            {

                        rA42_CONTRACTING_COMPANIES_PASS_DTL.STATION_CODE = STATION_CODE;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.SERVICE_NUMBER =rA42_CONTRACTING_COMPANIES_PASS_DTL.SERVICE_NUMBER;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.RESPONSIBLE = rA42_CONTRACTING_COMPANIES_PASS_DTL.RESPONSIBLE;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_TYPE_CODE =COMPANY_TYPE_CODE;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE =rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.REJECTED = false;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.CARD_FOR_CODE = 2;
                        //create barcode for  every permit 
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID =rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_CARD_NUMBER;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.ID_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.ID_CARD_NUMBER;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.CARD_EXPIRED_DATE = rA42_CONTRACTING_COMPANIES_PASS_DTL.CARD_EXPIRED_DATE;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_A;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_E;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_A;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_E;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_PLACE = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_PLACE;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.GSM = rA42_CONTRACTING_COMPANIES_PASS_DTL.GSM;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.ADDRESS = rA42_CONTRACTING_COMPANIES_PASS_DTL.ADDRESS;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_FROM = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_FROM;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_TO = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_TO;
                        //save personal image for every person 
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


                            //check image file type 
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
                                    return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
                                }

                                rA42_CONTRACTING_COMPANIES_PASS_DTL.PERSONAL_IMAGE = fileName;



                            }
                            else
                            {
                                //show error message if file extention not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
                            }
                        }
                    }



                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                        }
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.CRD_BY = currentUser;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.CRD_DT = DateTime.Now;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.UPD_BY = currentUser;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.UPD_DT = DateTime.Now;
                        db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Add(rA42_CONTRACTING_COMPANIES_PASS_DTL);
                        db.SaveChanges();


                //insert documents 
                if (files != null)
                {

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


                                //check file type 
                                if (general.CheckFileType(file.FileName))
                                {

                                    fileName = "FileNO" + c + "_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);

                                    //insert new document 
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE,
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
                                    //delete documents if somthing wrong happens
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
                                }
                            }

                            else
                            {


                            }
                        }
                    }
                    //catch any error 
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }



                AddToast(new Toast("",
                   GetResourcesValue("success_create_message"),
                   "green"));
                return RedirectToAction("Index");
            }


            AddToast(new Toast("",
                GetResourcesValue("error_create_message"),
                "red"));
            return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
        }


        // this type of create for pemits cells and responsible users who have some authority of the system such as (المنسق الأمني - ضابط الأمن)
        public ActionResult Supercreate()
        {
            ViewBag.activetab = "Supercreate";
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
                //get documents for this type of permits in english 
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
                //get cuntracting companies in english 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE), "COMPANY_CODE", "COMPANY_NAME_E");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");

               
            }
            else
            {
               
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permit types in arabic
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                //get documents types for this permits in arabic 
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
                //get cuntracted companies in arabic 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE), "COMPANY_CODE", "COMPANY_NAME");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                
            }



            return View();
        }

        
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        //posted strings and integers are for other models 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Supercreate(RA42_CONTRACTING_COMPANIES_PASS_DTL rA42_CONTRACTING_COMPANIES_PASS_DTL,
             int[] FILE_TYPES,string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
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

                FORCE_ID = check_unit.FORCE_ID.Value;

            }

            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();

            ViewBag.activetab = "Supercreate";
            if (Language.GetCurrentLang() == "en")
            {
               
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E",rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE);
                //get permit types in englsih 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E",rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE);
                //get documents types for this permit in english 
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
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE), "COMPANY_CODE", "COMPANY_NAME_E",rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE);
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E",rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID);

                
            }
            else
            {
               
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A",rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE);
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE",rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE);
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
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE), "COMPANY_CODE", "COMPANY_NAME",rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE);
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A",rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID);
               

            }

            User user = null;
            Task<User> callTask = Task.Run(
                () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                );
            callTask.Wait();
            user = callTask.Result;
           

            
            if (ModelState.IsValid)
            {

               
                rA42_CONTRACTING_COMPANIES_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.SERVICE_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.SERVICE_NUMBER;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_TYPE_CODE = COMPANY_TYPE_CODE;
            

                if (WORKFLOWID == 2 || WORKFLOWID <= 1)
                {

                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 7 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 2001 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);

                    }
                    else
                    {
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.REJECTED = false;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.STATUS = false;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.ISOPENED = false;
                }


                if (WORKFLOWID == 7)
                {

                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);

                    }
                    else
                    {
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_CONTRACTING_COMPANIES_PASS_DTL.APPROVAL_SN = user.EMP_SERVICE_NO.ToUpper();
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.REJECTED = false;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.STATUS = false;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.ISOPENED = true;
                }

                if (WORKFLOWID == 3)
                {
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);

                    }
                    else
                    {
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_CONTRACTING_COMPANIES_PASS_DTL.PERMIT_SN = user.EMP_SERVICE_NO.ToUpper();
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;

                    rA42_CONTRACTING_COMPANIES_PASS_DTL.REJECTED = false;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.STATUS = false;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.ISOPENED = true;
                }
                if(WORKFLOWID == 4)
                {
                    
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);

                    }
                    else
                    {
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.AUTHO_SN = user.EMP_SERVICE_NO.ToUpper();
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;

                    rA42_CONTRACTING_COMPANIES_PASS_DTL.REJECTED = false;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.STATUS = true;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.ISOPENED = true;
                }
                rA42_CONTRACTING_COMPANIES_PASS_DTL.CRD_BY = currentUser;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.UPD_BY = currentUser;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
              
                
               
                      
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.CARD_FOR_CODE = 2;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID = rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE =rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_CARD_NUMBER;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.ID_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.ID_CARD_NUMBER;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.CARD_EXPIRED_DATE = rA42_CONTRACTING_COMPANIES_PASS_DTL.CARD_EXPIRED_DATE;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_A;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_E;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_A;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_E;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_PLACE = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_PLACE;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.BARCODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.BARCODE;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.GSM = rA42_CONTRACTING_COMPANIES_PASS_DTL.GSM;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.ADDRESS = rA42_CONTRACTING_COMPANIES_PASS_DTL.ADDRESS;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_FROM = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_FROM;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_TO = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_TO;
                        //add personal image for every permit  
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


                            //check files extention
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
                                    return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
                                }

                                rA42_CONTRACTING_COMPANIES_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if image extention not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
                            }
                        }
                    }



                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                        }
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.CRD_BY = currentUser;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.CRD_DT = DateTime.Now;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.UPD_BY = currentUser;
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.UPD_DT = DateTime.Now;
                        db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Add(rA42_CONTRACTING_COMPANIES_PASS_DTL);
                        db.SaveChanges();


                //upload new documents - check if files not null
                if (files != null)
                {

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

                                    fileName = "FileNO" + c + "_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);
                                    //uplaod new documents 
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE,
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
                                    //delete uploaded documents if there is somthing wrong 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
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
                return RedirectToAction("Index");
            }


            AddToast(new Toast("",
                GetResourcesValue("error_create_message"),
                "red"));
            return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
        }
        // details of specific employee permit 
        public ActionResult Details(int? id)
        {
            ViewBag.activetab = "Details";

            if (id == null)
            {
                return NotFound();
            }
            //check if permit id in the table  
            RA42_CONTRACTING_COMPANIES_PASS_DTL rA42_CONTRACTING_COMPANIES_PASS_DTL = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Find(id);
            if (rA42_CONTRACTING_COMPANIES_PASS_DTL == null)
            {
                return NotFound();
            }

            
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();




            return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
        }
      
      
        // GET: Companypass/Edit/5
        public ActionResult Edit(int? id)
        {
            ViewBag.activetab = "edit";

            if (id == null)
            {
                return NotFound();
            }
            RA42_CONTRACTING_COMPANIES_PASS_DTL rA42_CONTRACTING_COMPANIES_PASS_DTL = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Find(id);
            if (rA42_CONTRACTING_COMPANIES_PASS_DTL == null)
            {
                return NotFound();
            }

            //check who authorized to open and edit this permit 
            //for every kind of users there are specific rules 
            if (ViewBag.RESPO_STATE <=1)
            {
                if (rA42_CONTRACTING_COMPANIES_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_CONTRACTING_COMPANIES_PASS_DTL.RESPONSIBLE !=currentUser)
                {
                    if (rA42_CONTRACTING_COMPANIES_PASS_DTL.ISOPENED != true)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {

                            return NotFound();

                        }
                    }
                }

                if (rA42_CONTRACTING_COMPANIES_PASS_DTL.ISOPENED == true)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        if (rA42_CONTRACTING_COMPANIES_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && rA42_CONTRACTING_COMPANIES_PASS_DTL.REJECTED == true)
                        {

                        }
                        else
                        {
                            if (rA42_CONTRACTING_COMPANIES_PASS_DTL.ISOPENED == true)
                            {
                                return NotFound();
                            }
                        }

                    }
                }
            }
            else
            {
                if (rA42_CONTRACTING_COMPANIES_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_CONTRACTING_COMPANIES_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != rA42_CONTRACTING_COMPANIES_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
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
                //get cuntracted companies in english 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E",rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE);
                //get permit types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E",rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE);
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_CONTRACTING_COMPANIES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get uploaded files 
                ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
               
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E",rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID);

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE);
                }

            }
            else{
                //get cuntracted companies in arabic 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A",rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE);
                // get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE",rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE);
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_CONTRACTING_COMPANIES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get uploaded files 
                ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
               
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A",rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID);

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }


            //get files types for this request to check the unuploaded documents
            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_CONTRACTING_COMPANIES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                                  select new
                                  {
                                      FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                      FILE_TYPE = c.FILE_TYPE,
                                      FILE_TYPE_E = c.FILE_TYPE_E,

                                  }).Count();

            ViewBag.PASS_FILES_2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                    join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                    join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_CONTRACTING_COMPANIES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            //get comments for this request 
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            //get status of the request 
            ViewBag.STATUS = rA42_CONTRACTING_COMPANIES_PASS_DTL.STATUS;
            
            return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
        }

        // POST: Companypass/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(RA42_CONTRACTING_COMPANIES_PASS_DTL rA42_CONTRACTING_COMPANIES_PASS_DTL, string COMMENT,
            FormCollection form,
              int[] FILE_TYPES,string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE
           )
        {
            ViewBag.activetab = "edit";
            var general_data = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.CONTRACT_CODE == rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE).FirstOrDefault();

            //get files types for this request to check the unuploaded documents
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
            //get comments for this request
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            //get status of this request
            ViewBag.STATUS = rA42_CONTRACTING_COMPANIES_PASS_DTL.STATUS;

            if (Language.GetCurrentLang() == "en")
            {
                //get cuntracted companies in english 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E",rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE);
                //get permit types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E",rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE);
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == general_data.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get uploaded files 
                ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
               
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E",rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID);

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE);
                }

            }
            else
            {
                //get cuntracted companies in arabic 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A",rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE);
                // get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE",rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE);
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == general_data.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get uploaded files 
                ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
             
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A",rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID);

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
           

            //editing start from here
            if (ModelState.IsValid)
            {

              
                //upload new documents - check if files not null
                if (files != null)
                {
                    
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

                                    fileName = "FileNO" + c + "_" + ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);
                                    //uplaod new documents 
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE,
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
                                    //delete uploaded documents if there is somthing wrong 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return RedirectToAction("Edit", new { id = rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE });
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
                //add new comments
                if (COMMENT.Length > 0)
                {
                    RA42_COMMENTS_MST rA42_COMMENT = new RA42_COMMENTS_MST();
                    rA42_COMMENT.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                    rA42_COMMENT.PASS_ROW_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE;
                    rA42_COMMENT.CRD_BY = currentUser;
                    rA42_COMMENT.CRD_DT = DateTime.Now;
                    rA42_COMMENT.COMMENT = COMMENT;
                    db.RA42_COMMENTS_MST.Add(rA42_COMMENT);


                }
           
                   
                        //upload personal image for this permit 
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


                                    //check image extention 
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
                                            return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
                                        }

                                        rA42_CONTRACTING_COMPANIES_PASS_DTL.PERSONAL_IMAGE = fileName;


                                    }
                                    else
                                    {
                                        //show error if there is somthing wrong 
                                        AddToast(new Toast("",
                                        GetResourcesValue("error_update_message"),
                                        "red"));
                                        TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                        return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
                                    }
                                }
                            }

                            catch (Exception ex)
                            {
                                ex.GetBaseException();
                            }
                        }






                if (rA42_CONTRACTING_COMPANIES_PASS_DTL.PERSONAL_IMAGE != null)
                {
                    general_data.PERSONAL_IMAGE = rA42_CONTRACTING_COMPANIES_PASS_DTL.PERSONAL_IMAGE;
                }
                else
                {
                    general_data.PERSONAL_IMAGE = general_data.PERSONAL_IMAGE;
                }



                //aditing request and proccess every thing by authorized users to finish every permit 
                var x = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS !=true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE !=false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                var currentUserInfo = (new UserInfo()).getUserInfo();

                //this section is for developer
                if (form["approvebtn"] != null && ViewBag.DEVELOPER == true)
                {

                    general_data.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                    general_data.IDENTITY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE;
                    general_data.GENDER_ID = rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID;
                    general_data.PASS_TYPE_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE;
                    general_data.WORK_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_CARD_NUMBER;
                    general_data.ID_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.ID_CARD_NUMBER;
                    general_data.CARD_EXPIRED_DATE = rA42_CONTRACTING_COMPANIES_PASS_DTL.CARD_EXPIRED_DATE;
                    general_data.NAME_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_E;
                    general_data.PROFESSION_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_E;
                    general_data.WORK_PLACE = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_PLACE;
                    general_data.GSM =rA42_CONTRACTING_COMPANIES_PASS_DTL.GSM;
                    general_data.ADDRESS = rA42_CONTRACTING_COMPANIES_PASS_DTL.ADDRESS;
                    general_data.DATE_FROM = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_TO;
                    general_data.WORKFLOW_RESPO_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE;
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.COMPANY_TYPE_CODE = general_data.COMPANY_TYPE_CODE;
                    general_data.COMPANY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE;
                    general_data.SERVICE_NUMBER = general_data.SERVICE_NUMBER;
                    general_data.RESPONSIBLE = rA42_CONTRACTING_COMPANIES_PASS_DTL.RESPONSIBLE;
                    general_data.PURPOSE_OF_PASS = rA42_CONTRACTING_COMPANIES_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_CONTRACTING_COMPANIES_PASS_DTL.REMARKS;
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
                    general_data.ISPRINTED = false;
                    general_data.ISOPENED = true;
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

                    //this section is for applicant 
                    if (form["approvebtn"] != null && WORKFLOWID <= 2)
                    {
                        general_data.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        general_data.IDENTITY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE;
                        general_data.GENDER_ID = rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID;
                        general_data.PASS_TYPE_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE;
                        general_data.WORK_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_CARD_NUMBER;
                        general_data.ID_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.ID_CARD_NUMBER;
                        general_data.CARD_EXPIRED_DATE = rA42_CONTRACTING_COMPANIES_PASS_DTL.CARD_EXPIRED_DATE;
                        general_data.NAME_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_A;
                        general_data.NAME_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_E;
                        general_data.PROFESSION_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_A;
                        general_data.PROFESSION_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_E;
                        general_data.WORK_PLACE = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_PLACE;
                        general_data.GSM = rA42_CONTRACTING_COMPANIES_PASS_DTL.GSM;
                        general_data.ADDRESS = rA42_CONTRACTING_COMPANIES_PASS_DTL.ADDRESS;
                        general_data.DATE_FROM = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_FROM;
                        general_data.DATE_TO = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_TO;
                        general_data.WORKFLOW_RESPO_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE;
                        general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;
                        general_data.COMPANY_TYPE_CODE = general_data.COMPANY_TYPE_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.COMPANY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE;
                        general_data.SERVICE_NUMBER = general_data.SERVICE_NUMBER;
                        general_data.RESPONSIBLE = rA42_CONTRACTING_COMPANIES_PASS_DTL.RESPONSIBLE;
                        general_data.PURPOSE_OF_PASS = rA42_CONTRACTING_COMPANIES_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_CONTRACTING_COMPANIES_PASS_DTL.REMARKS;
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
                        general_data.ISPRINTED = false;
                        general_data.STATUS = general_data.STATUS;
                        db.Entry(general_data).State = EntityState.Modified;
                        db.SaveChanges();
                        AddToast(new Toast("",
                        GetResourcesValue("success_update_message"),
                        "green"));
                        return RedirectToAction("Mypasses");
                    }

                    //this section is for enginner section (ركن 1 هندسة)
                    if (form["approvebtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 7)
                    {


                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE });

                        }
                        general_data.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        general_data.IDENTITY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE;
                        general_data.GENDER_ID = rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID;
                        general_data.PASS_TYPE_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE;
                        general_data.WORK_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_CARD_NUMBER;
                        general_data.ID_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.ID_CARD_NUMBER;
                        general_data.CARD_EXPIRED_DATE = rA42_CONTRACTING_COMPANIES_PASS_DTL.CARD_EXPIRED_DATE;
                        general_data.NAME_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_A;
                        general_data.NAME_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_E;
                        general_data.PROFESSION_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_A;
                        general_data.PROFESSION_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_E;
                        general_data.WORK_PLACE = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_PLACE;
                        general_data.GSM = rA42_CONTRACTING_COMPANIES_PASS_DTL.GSM;
                        general_data.ADDRESS = rA42_CONTRACTING_COMPANIES_PASS_DTL.ADDRESS;
                        general_data.DATE_FROM = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_FROM;
                        general_data.DATE_TO = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_TO;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.COMPANY_TYPE_CODE = general_data.COMPANY_TYPE_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.COMPANY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE;
                        general_data.SERVICE_NUMBER = general_data.SERVICE_NUMBER;
                        general_data.RESPONSIBLE = rA42_CONTRACTING_COMPANIES_PASS_DTL.RESPONSIBLE;
                        general_data.PURPOSE_OF_PASS = rA42_CONTRACTING_COMPANIES_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_CONTRACTING_COMPANIES_PASS_DTL.REMARKS;
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
                        general_data.ISPRINTED = false;
                        general_data.STATUS = general_data.STATUS;
                        db.Entry(general_data).State = EntityState.Modified;
                        db.SaveChanges();
                        AddToast(new Toast("",
                        GetResourcesValue("success_update_message"),
                        "green"));
                        return RedirectToAction("Authopasses");
                    }
                    //this section is for permit cell in hq 

                    if (form["approvebtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3)
                    {


                        if (general_data.STATUS == true)
                        {
                            general_data.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;
                        }
                        else
                        {
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Edit", new { id = rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE });

                            }
                            general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;

                        }
                        general_data.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        general_data.IDENTITY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE;
                        general_data.GENDER_ID = rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID;
                        general_data.PASS_TYPE_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE;
                        general_data.WORK_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_CARD_NUMBER;
                        general_data.ID_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.ID_CARD_NUMBER;
                        general_data.CARD_EXPIRED_DATE = rA42_CONTRACTING_COMPANIES_PASS_DTL.CARD_EXPIRED_DATE;
                        general_data.NAME_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_A;
                        general_data.NAME_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_E;
                        general_data.PROFESSION_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_A;
                        general_data.PROFESSION_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_E;
                        general_data.WORK_PLACE = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_PLACE;
                        general_data.GSM = rA42_CONTRACTING_COMPANIES_PASS_DTL.GSM;
                        general_data.ADDRESS = rA42_CONTRACTING_COMPANIES_PASS_DTL.ADDRESS;
                        general_data.DATE_FROM = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_FROM;
                        general_data.DATE_TO = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_TO;
                        general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.COMPANY_TYPE_CODE = general_data.COMPANY_TYPE_CODE;
                        general_data.COMPANY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE;
                        general_data.SERVICE_NUMBER = general_data.SERVICE_NUMBER;
                        general_data.RESPONSIBLE = rA42_CONTRACTING_COMPANIES_PASS_DTL.RESPONSIBLE;
                        general_data.PURPOSE_OF_PASS = rA42_CONTRACTING_COMPANIES_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_CONTRACTING_COMPANIES_PASS_DTL.REMARKS;
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
                    //this section is for security officer
                    if (form["approvebtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4)
                    {




                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE });

                        }
                        general_data.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        general_data.IDENTITY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE;
                        general_data.GENDER_ID = rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID;
                        general_data.PASS_TYPE_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE;
                        general_data.WORK_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_CARD_NUMBER;
                        general_data.ID_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.ID_CARD_NUMBER;
                        general_data.CARD_EXPIRED_DATE = rA42_CONTRACTING_COMPANIES_PASS_DTL.CARD_EXPIRED_DATE;
                        general_data.NAME_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_A;
                        general_data.NAME_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_E;
                        general_data.PROFESSION_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_A;
                        general_data.PROFESSION_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_E;
                        general_data.WORK_PLACE = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_PLACE;
                        general_data.GSM = rA42_CONTRACTING_COMPANIES_PASS_DTL.GSM;
                        general_data.ADDRESS = rA42_CONTRACTING_COMPANIES_PASS_DTL.ADDRESS;
                        general_data.DATE_FROM = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_FROM;
                        general_data.DATE_TO = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_TO;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.COMPANY_TYPE_CODE = general_data.COMPANY_TYPE_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.COMPANY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE;
                        general_data.SERVICE_NUMBER = general_data.SERVICE_NUMBER;
                        general_data.RESPONSIBLE = rA42_CONTRACTING_COMPANIES_PASS_DTL.RESPONSIBLE;
                        general_data.PURPOSE_OF_PASS = rA42_CONTRACTING_COMPANIES_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_CONTRACTING_COMPANIES_PASS_DTL.REMARKS;
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
                        if (rA42_CONTRACTING_COMPANIES_PASS_DTL.BARCODE != null)
                        {
                            general_data.BARCODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.BARCODE;
                        }
                        else
                        {
                            general_data.BARCODE = general_data.BARCODE;
                        }
                        general_data.REJECTED = false;
                        general_data.STATUS = true;
                        general_data.ISPRINTED = false;
                        general_data.ISOPENED = true;
                        db.Entry(general_data).State = EntityState.Modified;
                        db.SaveChanges();
                        AddToast(new Toast("",
                        GetResourcesValue("success_update_message"),
                        "green"));
                        return RedirectToAction("Newpasses");
                    }

                    

                    if (form["rejectbtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 7)
                    {


                        //var v = db.RA42_WORKFLOW_RESPONSIBLE_MST.Where(a => a.SERVICE_NUMBER == currentUser).FirstOrDefault();
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE });

                        }
                        general_data.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        general_data.IDENTITY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE;
                        general_data.GENDER_ID = rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID;
                        general_data.PASS_TYPE_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE;
                        general_data.WORK_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_CARD_NUMBER;
                        general_data.ID_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.ID_CARD_NUMBER;
                        general_data.CARD_EXPIRED_DATE = rA42_CONTRACTING_COMPANIES_PASS_DTL.CARD_EXPIRED_DATE;
                        general_data.NAME_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_A;
                        general_data.NAME_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_E;
                        general_data.PROFESSION_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_A;
                        general_data.PROFESSION_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_E;
                        general_data.WORK_PLACE = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_PLACE;
                        general_data.GSM = rA42_CONTRACTING_COMPANIES_PASS_DTL.GSM;
                        general_data.ADDRESS = rA42_CONTRACTING_COMPANIES_PASS_DTL.ADDRESS;
                        general_data.DATE_FROM = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_FROM;
                        general_data.DATE_TO = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_TO;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.COMPANY_TYPE_CODE = general_data.COMPANY_TYPE_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.COMPANY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE;
                        general_data.SERVICE_NUMBER = general_data.SERVICE_NUMBER;
                        general_data.RESPONSIBLE = rA42_CONTRACTING_COMPANIES_PASS_DTL.RESPONSIBLE;
                        general_data.PURPOSE_OF_PASS = rA42_CONTRACTING_COMPANIES_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_CONTRACTING_COMPANIES_PASS_DTL.REMARKS;
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

                    if (form["rejectbtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3)
                    {


                        //var v = db.RA42_WORKFLOW_RESPONSIBLE_MST.Where(a => a.SERVICE_NUMBER == general_data.APPROVAL_SN).FirstOrDefault();
                        //var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == general_data.APPROVAL_SN).FirstOrDefault();
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == general_data.APPROVAL_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE });

                        }
                        general_data.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        general_data.IDENTITY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE;
                        general_data.GENDER_ID = rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID;
                        general_data.PASS_TYPE_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE;
                        general_data.WORK_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_CARD_NUMBER;
                        general_data.ID_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.ID_CARD_NUMBER;
                        general_data.CARD_EXPIRED_DATE = rA42_CONTRACTING_COMPANIES_PASS_DTL.CARD_EXPIRED_DATE;
                        general_data.NAME_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_A;
                        general_data.NAME_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_E;
                        general_data.PROFESSION_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_A;
                        general_data.PROFESSION_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_E;
                        general_data.WORK_PLACE = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_PLACE;
                        general_data.GSM = rA42_CONTRACTING_COMPANIES_PASS_DTL.GSM;
                        general_data.ADDRESS = rA42_CONTRACTING_COMPANIES_PASS_DTL.ADDRESS;
                        general_data.DATE_FROM = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_FROM;
                        general_data.DATE_TO = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_TO;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.COMPANY_TYPE_CODE = general_data.COMPANY_TYPE_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.COMPANY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE;
                        general_data.SERVICE_NUMBER = general_data.SERVICE_NUMBER;
                        general_data.RESPONSIBLE = rA42_CONTRACTING_COMPANIES_PASS_DTL.RESPONSIBLE;
                        general_data.PURPOSE_OF_PASS = rA42_CONTRACTING_COMPANIES_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_CONTRACTING_COMPANIES_PASS_DTL.REMARKS;
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

                    if (form["rejectbtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4)
                    {


                       
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == general_data.PERMIT_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE });

                        }
                        general_data.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        general_data.IDENTITY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE;
                        general_data.GENDER_ID = rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID;
                        general_data.PASS_TYPE_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE;
                        general_data.WORK_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_CARD_NUMBER;
                        general_data.ID_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.ID_CARD_NUMBER;
                        general_data.CARD_EXPIRED_DATE = rA42_CONTRACTING_COMPANIES_PASS_DTL.CARD_EXPIRED_DATE;
                        general_data.NAME_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_A;
                        general_data.NAME_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_E;
                        general_data.PROFESSION_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_A;
                        general_data.PROFESSION_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_E;
                        general_data.WORK_PLACE = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_PLACE;
                        general_data.GSM = rA42_CONTRACTING_COMPANIES_PASS_DTL.GSM;
                        general_data.ADDRESS = rA42_CONTRACTING_COMPANIES_PASS_DTL.ADDRESS;
                        general_data.DATE_FROM = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_FROM;
                        general_data.DATE_TO = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_TO;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.COMPANY_TYPE_CODE = general_data.COMPANY_TYPE_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.COMPANY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE;
                        general_data.SERVICE_NUMBER = general_data.SERVICE_NUMBER;
                        general_data.RESPONSIBLE = rA42_CONTRACTING_COMPANIES_PASS_DTL.RESPONSIBLE;
                        general_data.PURPOSE_OF_PASS = rA42_CONTRACTING_COMPANIES_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_CONTRACTING_COMPANIES_PASS_DTL.REMARKS;
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
            return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
        }


        // GET: Companypass/Renew/5
        public ActionResult Renew(int? id)
        {
            ViewBag.activetab = "renew";

            if (id == null)
            {
                return NotFound();
            }
            RA42_CONTRACTING_COMPANIES_PASS_DTL rA42_CONTRACTING_COMPANIES_PASS_DTL = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Find(id);
            if (rA42_CONTRACTING_COMPANIES_PASS_DTL == null)
            {
                return NotFound();
            }

            //check who authorized to open and edit this permit 
            //for every kind of users there are specific rules 
            if (rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_TO != null)
            {
                string date = rA42_CONTRACTING_COMPANIES_PASS_DTL.CheckDate(rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_TO.Value);
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
                //get cuntracted companies in english 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE);
                //get permit types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE);
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_CONTRACTING_COMPANIES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
               
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID);

                
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE);
                

            }
            else
            {
                //get cuntracted companies in arabic 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE);
                // get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE);
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_CONTRACTING_COMPANIES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
               
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID);

                
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE);
                
            }

            //get uploaded files 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get files types for this request to check the unuploaded documents
            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_CONTRACTING_COMPANIES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                                  select new
                                  {
                                      FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                      FILE_TYPE = c.FILE_TYPE,
                                      FILE_TYPE_E = c.FILE_TYPE_E,

                                  }).Count();

            ViewBag.PASS_FILES_2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                    join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                    join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_CONTRACTING_COMPANIES_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();

            rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_FROM = null;
            rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_TO = null;

            return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
        }
        // POST: Companypass/Renew/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Renew(RA42_CONTRACTING_COMPANIES_PASS_DTL rA42_CONTRACTING_COMPANIES_PASS_DTL,
           int CONTRACT_ID,int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "renew";
            var general_data = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.CONTRACT_CODE == CONTRACT_ID).FirstOrDefault();

            //get files types for this request to check the unuploaded documents
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
            //get uploaded files 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == CONTRACT_ID && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            

            if (Language.GetCurrentLang() == "en")
            {
                //get cuntracted companies in english 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE);
                //get permit types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE);
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == general_data.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID);

                
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE);
                

            }
            else
            {
                //get cuntracted companies in arabic 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_CODE);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE);
                // get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE);
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == general_data.RA42_STATIONS_MST.FORCE_ID && d.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
               
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID);

                
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE);
                
            }


            //editing start from here
            if (ModelState.IsValid)
            {


                try { 
               


                //upload personal image for this permit 
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


                            //check image extention 
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
                                    return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
                                }

                                rA42_CONTRACTING_COMPANIES_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if there is somthing wrong 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_CONTRACTING_COMPANIES_PASS_DTL);
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }






                if (rA42_CONTRACTING_COMPANIES_PASS_DTL.PERSONAL_IMAGE != null)
                {
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.PERSONAL_IMAGE = rA42_CONTRACTING_COMPANIES_PASS_DTL.PERSONAL_IMAGE;
                }
                else
                {
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.PERSONAL_IMAGE = general_data.PERSONAL_IMAGE;
                }





                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;

                rA42_CONTRACTING_COMPANIES_PASS_DTL.STATION_CODE = general_data.STATION_CODE;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.SERVICE_NUMBER = currentUser;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.COMPANY_TYPE_CODE = COMPANY_TYPE_CODE;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;

                if (WORKFLOWID <= 1)
                {

                   
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.REJECTED = false;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.STATUS = false;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.ISOPENED = false;
                }


                if (WORKFLOWID == 7)
                {

                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(general_data);

                    }
                    else
                    {
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_CONTRACTING_COMPANIES_PASS_DTL.APPROVAL_SN = user.EMP_SERVICE_NO.ToUpper();
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.REJECTED = false;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.STATUS = false;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.ISOPENED = true;
                }

                if (WORKFLOWID == 3)
                {
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(general_data);

                    }
                    else
                    {
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_CONTRACTING_COMPANIES_PASS_DTL.PERMIT_SN = user.EMP_SERVICE_NO.ToUpper();
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;

                    rA42_CONTRACTING_COMPANIES_PASS_DTL.REJECTED = false;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.STATUS = false;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.ISOPENED = true;
                }
                if (WORKFLOWID == 4)
                {

                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(general_data);

                    }
                    else
                    {
                        rA42_CONTRACTING_COMPANIES_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.AUTHO_SN = user.EMP_SERVICE_NO.ToUpper();
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;

                    rA42_CONTRACTING_COMPANIES_PASS_DTL.REJECTED = false;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.STATUS = true;
                    rA42_CONTRACTING_COMPANIES_PASS_DTL.ISOPENED = true;
                }
                rA42_CONTRACTING_COMPANIES_PASS_DTL.CRD_BY = currentUser;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.UPD_BY = currentUser;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.UPD_DT = DateTime.Now;




                rA42_CONTRACTING_COMPANIES_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.IDENTITY_CODE;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID = rA42_CONTRACTING_COMPANIES_PASS_DTL.GENDER_ID;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.PASS_TYPE_CODE;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_CARD_NUMBER;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.ID_CARD_NUMBER = rA42_CONTRACTING_COMPANIES_PASS_DTL.ID_CARD_NUMBER;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.CARD_EXPIRED_DATE = rA42_CONTRACTING_COMPANIES_PASS_DTL.CARD_EXPIRED_DATE;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_A;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.NAME_E;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_A = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_A;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_E = rA42_CONTRACTING_COMPANIES_PASS_DTL.PROFESSION_E;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_PLACE = rA42_CONTRACTING_COMPANIES_PASS_DTL.WORK_PLACE;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.BARCODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.BARCODE;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.GSM = rA42_CONTRACTING_COMPANIES_PASS_DTL.GSM;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.ADDRESS = rA42_CONTRACTING_COMPANIES_PASS_DTL.ADDRESS;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_FROM = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_FROM;
                rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_TO = rA42_CONTRACTING_COMPANIES_PASS_DTL.DATE_TO;
                db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Add(rA42_CONTRACTING_COMPANIES_PASS_DTL);
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
                                        ACCESS_ROW_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE,
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
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return RedirectToAction("Renew", new { id = CONTRACT_ID });
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
                var selected_files = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == CONTRACT_ID && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
                foreach (var file in selected_files)
                {
                    //add new file
                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                    {
                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                        ACCESS_ROW_CODE = rA42_CONTRACTING_COMPANIES_PASS_DTL.CONTRACT_CODE,
                        FILE_TYPE = file.FILE_TYPE,
                        FILE_TYPE_TEXT = file.FILE_TYPE_TEXT,
                        FILE_NAME = file.FILE_NAME,
                        CRD_BY = currentUser,
                        CRD_DT = DateTime.Now


                    };
                    db.RA42_FILES_MST.Add(fILES_MST);
                    db.SaveChanges();

                }

                   

                    }
                    catch (DbEntityValidationException cc)
                    {
                        foreach (var validationerrors in cc.EntityValidationErrors)
                        {
                            foreach (var validationerror in validationerrors.ValidationErrors)
                            {
                                TempData["Erorr"] = validationerror.PropertyName + " + " + validationerror.ErrorMessage;
                                return RedirectToAction("Renew", new { id = CONTRACT_ID });

                            }
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
            TempData["Erorr"] = "Somthing wrong happen,حدث خطأ ما";
            AddToast(new Toast("",
                GetResourcesValue("error_update_message"),
                "red"));
            return View(general_data);
        }

        // delete request 
        public ActionResult Delete(int? id)
        {
            ViewBag.activetab = "delete";

            if (id == null)
            {
                return NotFound();
            }

            //check if request id already in the RA42_COMPANY_PASS_DTL table 
            RA42_CONTRACTING_COMPANIES_PASS_DTL rA42_COMPANY_PASS_DTL = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Find(id);
            if (rA42_COMPANY_PASS_DTL == null)
            {
                return NotFound();
            }

            if (ViewBag.RESPO_STATE <=1 )
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

            
            //get documents of the request
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get employees registred in this request 
            return View(rA42_COMPANY_PASS_DTL);
        }

        // confirm deleting request
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var general_data = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.CONTRACT_CODE == id).FirstOrDefault();

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

        //delete specific documet
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



      
     

        //delete personal image for specifc employee
        [HttpPost]
        public JsonResult DeleteImage(int id)
        {
            bool result = false;
            var general_data = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.CONTRACT_CODE == id).FirstOrDefault();

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
        [HttpPost]
        public JsonResult PrintById(int id, string type)
        {

            bool result = false;
            var general_data = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.CONTRACT_CODE == id).FirstOrDefault();

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

        //show notfound page if someone try to break any page 
        public ActionResult NotFound()
        {
            return RedirectToAction("NotFound", "Home");
        }

    }
}
