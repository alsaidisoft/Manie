using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using APP.Filters;
using APP.Util;
using portal.Controllers;
using Newtonsoft.Json;
using SecurityClearanceWebApp.Models;
using System.IO;
using SecurityClearanceWebApp.Util;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq.Dynamic;
using System.Security.Cryptography;

namespace SecurityClearanceWebApp.Controllers
{


    [UserInfoFilter]
    //this is the most important permit, vehicle permit, read the comments very clear 
    public class VechilepassController : Controller
    {
        //get db connection
        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user serive number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunctions class to use some functions 
        private GeneralFunctions general = new GeneralFunctions();
        //identify theses important variable to use them 
       // private int STATION_CODE = 0;
        private int WORKFLOWID = 0;
        private int RESPO_CODE = 0;
        private int ACCESS_TYPE_CODE = 3;
        private int COMPANY_TYPE_CODE = 2;
        private int STATION_CODE = 0;
        private int FORCE_ID = 0;
        //set title of the whole controller 
        string title = Resources.Passes.ResourceManager.GetString("access_type_vechile" + "_" + "ar");
        public VechilepassController()
        {
            ViewBag.Managepasses = "Managepasses";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Vechilepass";
            //set icon from fontawsome library for the controller 
            ViewBag.controllerIconClass = "fa fa-car";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Passes.ResourceManager.GetString("access_type_vechile" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;
            //check if the current user has authirty in this type of permit 
            var v = Task.Run(async ()=> await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefaultAsync()).Result;
            if (v != null)
            {
                if (v.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && v.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false)
                {
                    //get id of the user
                    ViewBag.RESPO_ID = v.WORKFLOW_RESPO_CODE;
                    //check workflow id type of the user 
                    ViewBag.RESPO_STATE = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID;
                    RESPO_CODE = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE;
                    WORKFLOWID = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value;
                    FORCE_ID = v.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.FORCE_ID.Value;
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


        // this view to show links tab 
        public ActionResult Index()
        {
           
            return View();
        }

        //delete violation
        public ActionResult DeleteViolationAction(int? id)
        {
            ViewBag.ViolationPage = "Violations";
            if (id == null)
            {
                return NotFound();
            }
            //check if the record id of the permit is in the table 
            RA42_VECHILE_VIOLATION_DTL rA42_VECHILE_VIOLATION_DTL = db.RA42_VECHILE_VIOLATION_DTL.Find(id);
            if (rA42_VECHILE_VIOLATION_DTL == null)
            {
                return NotFound();
            }
            //check if the current user has authority to see the violations
            if (ViewBag.RESPO_STATE == 3 || ViewBag.RESPO_STATE == 9 || ViewBag.ADMIN == true || ViewBag.DEVELOPER == true)
            {
             
            }
            else
            {
                return NotFound();
            }

            
            return View(rA42_VECHILE_VIOLATION_DTL);
        }
        // confirm deleting
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteViolationAction(int id)
        {
            var general_data = db.RA42_VECHILE_VIOLATION_DTL.Where(a => a.VECHILE_VIOLATION_CODE == id).FirstOrDefault();

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

                return RedirectToAction("Violations", "MyPasses");


            }
            AddToast(new Toast("",
               GetResourcesValue("Dont_have_permissions_to_dlt"),
               "red"));
            return View(general_data);
        }

        //this is the view of violations, normal users can browse it to see their violations and permits cell can add new violation
        public ActionResult Violations(int? id)
        {
            ViewBag.activetab = "Violations";
            if (id == null)
            {
                return NotFound();
            }
            //check if the record id of the permit is in the table 
            RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL = db.RA42_VECHILE_PASS_DTL.Find(id);
            if (rA42_VECHILE_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if the current user has authority to see the violations
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_VECHILE_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_VECHILE_PASS_DTL.RESPONSIBLE != currentUser)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }

            //get violatopn as viewBag
            var vIOLATIONS = db.RA42_VECHILE_VIOLATION_DTL.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.VIOLATIONS = vIOLATIONS;
            //get vilations type
            if (Language.GetCurrentLang() == "en")
            {
                ViewBag.VIOLATION_CODE = new SelectList(db.RA42_VIOLATIONS_MST.Where(a => a.DLT_STS != true), "VIOLATION_CODE", "VIOLATION_TYPE_E");
            }
            else
            {
                ViewBag.VIOLATION_CODE = new SelectList(db.RA42_VIOLATIONS_MST.Where(a => a.DLT_STS != true), "VIOLATION_CODE", "VIOLATION_TYPE_A");

            }
            return View(rA42_VECHILE_PASS_DTL);
        }
        // POST new violation 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Violations(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL, string VIOLATION_DESC, string VIOLATION_BY,string is_prevent, string VIOLATION_DATE,float VIOLATION_PRICE,int VIOLATION_CODE)
        {
            ViewBag.activetab = "Violations";
            var general_data = db.RA42_VECHILE_PASS_DTL.Where(a => a.VECHILE_PASS_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE).FirstOrDefault();
            //get vilations type
            if (Language.GetCurrentLang() == "en")
            {
                ViewBag.VIOLATION_CODE = new SelectList(db.RA42_VIOLATIONS_MST.Where(a => a.DLT_STS != true), "VIOLATION_CODE", "VIOLATION_TYPE_E");
            }
            else
            {
                ViewBag.VIOLATION_CODE = new SelectList(db.RA42_VIOLATIONS_MST.Where(a => a.DLT_STS != true), "VIOLATION_CODE", "VIOLATION_TYPE_A");

            }



            //add violation by check if the VIOLATION_DESC not null
            if (VIOLATION_DESC != null)
            {
                if (VIOLATION_DESC.Length > 0)
                {

                    RA42_VECHILE_VIOLATION_DTL vECHILE_VIOLATION_DTL = new RA42_VECHILE_VIOLATION_DTL();
                    vECHILE_VIOLATION_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                    vECHILE_VIOLATION_DTL.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
                    vECHILE_VIOLATION_DTL.CRD_BY = currentUser;
                    vECHILE_VIOLATION_DTL.CRD_DT = DateTime.Now;
                    vECHILE_VIOLATION_DTL.VIOLATION_DESC = VIOLATION_DESC;
                    vECHILE_VIOLATION_DTL.VIOLATION_PRICE = VIOLATION_PRICE;
                    vECHILE_VIOLATION_DTL.VIOLATION_CODE = VIOLATION_CODE;
                    vECHILE_VIOLATION_DTL.VIOLATION_BY = VIOLATION_BY;
                    if (is_prevent.Equals("yes"))
                    {
                        vECHILE_VIOLATION_DTL.PREVENT = true;
                    }
                    else
                    {
                        vECHILE_VIOLATION_DTL.PREVENT = false;
                    }
                    if (VIOLATION_DATE.Length > 0)
                    {
                        DateTime vdate = Convert.ToDateTime(VIOLATION_DATE);
                        vECHILE_VIOLATION_DTL.VIOLATION_DATE = vdate;
                    }
                    else
                    {
                        vECHILE_VIOLATION_DTL.VIOLATION_DATE = DateTime.Now;
                    }
                    db.RA42_VECHILE_VIOLATION_DTL.Add(vECHILE_VIOLATION_DTL);
                    db.SaveChanges();
                    AddToast(new Toast("",
                      GetResourcesValue("adde_violation_success"),
                      "green"));
                }

            }
            //get violation as viewbag 
            var vIOLATIONS = db.RA42_VECHILE_VIOLATION_DTL.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.VIOLATIONS = vIOLATIONS;
            return View(rA42_VECHILE_PASS_DTL);


        }
        //this is comments view, to browse comments and new comments 
        public ActionResult Comments(int? id)
        {
            ViewBag.activetab = "Comments";

            if (id == null)
            {
                return NotFound();
            }
            //check if the record is in the db table 
            RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL = db.RA42_VECHILE_PASS_DTL.Find(id);
            if (rA42_VECHILE_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if the current user has authority to view this comments 

            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_VECHILE_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_VECHILE_PASS_DTL.RESPONSIBLE != currentUser)
                {
                    if (rA42_VECHILE_PASS_DTL.ISOPENED != true)
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
                if (rA42_VECHILE_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_VECHILE_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != rA42_VECHILE_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {
                            return NotFound();
                        }
                    }
                }
            }
            //get comments of the current permit 
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            return View(rA42_VECHILE_PASS_DTL);
        }

        // POST new comment 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Comments(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL, string COMMENT)
        {
            ViewBag.activetab = "Comments";
            var general_data = db.RA42_VECHILE_PASS_DTL.Where(a => a.VECHILE_PASS_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE).FirstOrDefault();





            //add comments
            if (COMMENT.Length > 0)
            {
                RA42_COMMENTS_MST rA42_COMMENT = new RA42_COMMENTS_MST();
                rA42_COMMENT.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_COMMENT.PASS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
                rA42_COMMENT.CRD_BY = currentUser;
                rA42_COMMENT.CRD_DT = DateTime.Now;
                rA42_COMMENT.COMMENT = COMMENT;
                db.RA42_COMMENTS_MST.Add(rA42_COMMENT);
                db.SaveChanges();
                AddToast(new Toast("",
                  GetResourcesValue("add_comment_success"),
                  "green"));

            }
            //get comments of the permit
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            return View(rA42_VECHILE_PASS_DTL);


        }

        // this view to choose how to create permit
        //we have different types of creation
        //create view is for normal user when he wants to get permit for him self
        //Familyvechilepass view to create permit for user family (his wife - his sester etc...)
        //supercreate view to create permit for some one alse permit (this is can be used also to create permit for family, becuase you need to set responsible from MOD)
        //Companypermit view to create permit for someon work on companies or other organizations
        //serach view is to create permit for someone in the MOD, by get his info from api 
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

            //List<RA42_VECHILE_PASS_DTL> empList = new List<RA42_VECHILE_PASS_DTL>();

            // empList = db.RA42_VECHILE_PASS_DTL.OrderByDescending(a => a.VECHILE_PASS_CODE).ToList<RA42_VECHILE_PASS_DTL>();
            var empList = db.RA42_VECHILE_PASS_DTL.Where(a=>a.WORKFLOW_RESPO_CODE !=null).Select(a => new
            {
                VECHILE_PASS_CODE = a.VECHILE_PASS_CODE,
                SERVICE_NUMBER = (a.SERVICE_NUMBER != null ? a.SERVICE_NUMBER : " "),
                CIVIL_NUMBER = (a.CIVIL_NUMBER != null ? a.CIVIL_NUMBER : " "),
                PERSONAL_IMAGE = a.PERSONAL_IMAGE,
                RANK_A = (a.RANK_A != null ? a.RANK_A : " "),
                RANK_E = (a.RANK_E != null ? a.RANK_E : " "),
                NAME_A = (a.NAME_A != null ? a.NAME_A : " "),
                NAME_E = (a.NAME_E != null ? a.NAME_E : " "),
                PHONE_NUMBER = (a.PHONE_NUMBER != null ? a.PHONE_NUMBER : " "),
                GSM = (a.GSM != null ? a.GSM : " "),
                PLATE_NUMBER = (a.PLATE_NUMBER != null ? a.PLATE_NUMBER : " "),
                PURPOSE_OF_PASS = (a.PURPOSE_OF_PASS != null ? a.PURPOSE_OF_PASS : " "),
                PROFESSION_A = (a.PROFESSION_A != null ? a.PROFESSION_A : " "),
                PROFESSION_E = (a.PROFESSION_E != null ? a.PROFESSION_E : " "),
                FORCE_ID = a.RA42_STATIONS_MST.FORCE_ID.Value,
                STATION_CODE = a.STATION_CODE,
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
                COMMENTS = a.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == a.VECHILE_PASS_CODE && a.DLT_STS != true).Count(),
                VIOLATIONS = a.RA42_ACCESS_TYPE_MST.RA42_VECHILE_VIOLATION_DTL.Where(x => x.DLT_STS != true && x.ACCESS_ROW_CODE == a.VECHILE_PASS_CODE).Count()


            }
             ).ToList();
            if(ViewBag.ADMIN == true)
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
                    try
                    {

                        empList = empList.
                 Where(x => x.SERVICE_NUMBER.Contains(searchValue) || x.CIVIL_NUMBER.Contains(searchValue) || x.NAME_A.Contains(searchValue)
                 || x.NAME_E.Contains(searchValue) || x.RANK_A.Contains(searchValue) || x.RANK_E.Contains(searchValue)
                 || x.PROFESSION_A.Contains(searchValue) || x.PURPOSE_OF_PASS.Contains(searchValue)
                 //|| x.STEP_NAME.Contains(searchValue)
                 || x.PROFESSION_E.Contains(searchValue) || x.PHONE_NUMBER.Contains(searchValue) || x.GSM.Contains(searchValue)
                 || x.PLATE_NUMBER.Contains(searchValue) || x.STATION_A == searchValue
                 ).ToList();
                    }catch(Exception ex)
                    {
                        ex.GetBaseException();
                    }
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
            var empList = db.RA42_VECHILE_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID && a.STATION_CODE == STATION_CODE 
            && a.DLT_STS != true 
            && a.ISPRINTED == true && a.WORKFLOW_RESPO_CODE !=null).Select(a => new
            {
                VECHILE_PASS_CODE = a.VECHILE_PASS_CODE,
                CARD_FOR = a.RA42_ACCESS_TYPE_MST.ACCESS_TYPE +" - "+ a.RA42_CARD_FOR_MST.CARD_FOR_A,
                SERVICE_NUMBER = (a.SERVICE_NUMBER!=null?a.SERVICE_NUMBER:" "),
                CIVIL_NUMBER = (a.CIVIL_NUMBER != null ? a.CIVIL_NUMBER : " "),
                PERSONAL_IMAGE = a.PERSONAL_IMAGE,
                RANK_A = (a.RANK_A != null ? a.RANK_A : " "),
                RANK_E = (a.RANK_E != null ? a.RANK_E : " "),
                NAME_A = (a.NAME_A != null ? a.NAME_A : " "),
                NAME_E = (a.NAME_E != null ? a.NAME_E : " "),
                PHONE_NUMBER = (a.PHONE_NUMBER != null ? a.PHONE_NUMBER : " "),
                GSM = (a.GSM != null ? a.GSM : " "),
                PLATE_NUMBER = (a.PLATE_NUMBER != null ? a.PLATE_NUMBER : " "),
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
                COMPANY = a.RA42_COMPANY_MST.COMPANY_NAME,
                STATUS = a.STATUS,
                RETURNED = a.RETURNED,
                ACCESS_TYPE_CODE = a.ACCESS_TYPE_CODE.Value,
                VECHIL_DETAILS = "<p>بيانات المركبة </p>"+"<div>اسم المركبة: "+a.RA42_VECHILE_CATIGORY_MST.VECHILE_CAT +" - "+a.RA42_VECHILE_NAME_MST.VECHILE_NAME
                +"</div> <div> اللون: "+a.RA42_VECHILE_COLOR_MST.COLOR +"</div> <div> الرقم: "+a.RA42_PLATE_CHAR_MST.PLATE_CHAR +" - "+a.PLATE_NUMBER+"</div>",
                DLT_STS = a.DLT_STS,
                REJECTED = a.REJECTED,
                ISPRINTED = a.ISPRINTED,
                DATE_FROM = a.DATE_FROM,
                DATE_TO = a.DATE_TO,
                COMMENTS = a.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == a.VECHILE_PASS_CODE && a.DLT_STS !=true).Count(),
                VIOLATIONS = a.RA42_ACCESS_TYPE_MST.RA42_VECHILE_VIOLATION_DTL.Where(x => x.DLT_STS != true && x.ACCESS_ROW_CODE == a.VECHILE_PASS_CODE).Count()

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
                Where(x => x.SERVICE_NUMBER.Contains(searchValue)  || x.CIVIL_NUMBER.Contains(searchValue) || x.NAME_A.Contains(searchValue) 
                || x.GSM.Contains(searchValue) || x.PLATE_NUMBER.Contains(searchValue) 
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

       
        //this view is for administrator and developer to see all permits for all station 
        public ActionResult Allpasses()
        {
            ViewBag.activetab = "Allpasses";
            
            return View();
            
        }
        //this view is for autho person (المنسق الأمني)
        public ActionResult Authopasses()
        {
            ViewBag.activetab = "Authopasses";
            var rA42_VECHILE_PASS_DTL = db.RA42_VECHILE_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.DLT_STS != true).OrderByDescending(a => a.VECHILE_PASS_CODE);
            return View(rA42_VECHILE_PASS_DTL.ToList());
        }
        //this view is for permits cell and security officer
        public async Task<ActionResult> Newpasses()
        {
            ViewBag.activetab = "Newpasses";
            var rA42_VECHILE_PASS_DTL = await db.RA42_VECHILE_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID && a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ISPRINTED !=true && a.STATUS !=true).OrderByDescending(a => a.VECHILE_PASS_CODE).ToListAsync();
            return View(rA42_VECHILE_PASS_DTL);
        }

        public async Task<ActionResult> ToPrint()
        {
            ViewBag.activetab = "ToPrint";
            var rA42_VECHILE_PASS_DTL = await db.RA42_VECHILE_PASS_DTL.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID && a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ISPRINTED != true && a.STATUS == true).OrderByDescending(a => a.VECHILE_PASS_CODE).ToListAsync();
            return View(rA42_VECHILE_PASS_DTL);
        }

        //this view is for permits cell to show all printed permits 
        public ActionResult Printed()
        {
            ViewBag.activetab = "Printed";
            return View();
        }
      
        //function to gets all car types for specific catigory (أنواع السيارات صالون - دفع رباعي - شاحنة - دراجة) as json result 
        public JsonResult GetCars(int catigory)
        {
            db.Configuration.ProxyCreationEnabled = false;
            if (Language.GetCurrentLang() == "en")
            {
                var cars = db.RA42_VECHILE_NAME_MST.Where(a => a.VECHILE_CODE == catigory && a.DLT_STS != true).Select(a => new { VECHILE_NAME_CODE = a.VECHILE_NAME_CODE, VECHILE_NAME = a.VECHILE_NAME_E }).ToList();
                //Add JsonRequest behavior to allow retrieving states over http get
                return Json(cars, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var cars = db.RA42_VECHILE_NAME_MST.Where(a => a.VECHILE_CODE == catigory && a.DLT_STS != true).Select(a => new { VECHILE_NAME_CODE = a.VECHILE_NAME_CODE, VECHILE_NAME = a.VECHILE_NAME }).ToList();
                //Add JsonRequest behavior to allow retrieving states over http get
                return Json(cars, JsonRequestBehavior.AllowGet);
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
        // details view of any vechile permit 
        public ActionResult Details(int? id)
        {
            ViewBag.activetab = "details";

            if (id == null)
            {
                return NotFound();
            }
            //check if the record id is in the db table 
            RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL = db.RA42_VECHILE_PASS_DTL.Find(id);
            if (rA42_VECHILE_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if the current user has authority to view details 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_VECHILE_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_VECHILE_PASS_DTL.RESPONSIBLE != currentUser)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER !=true)
                    {
                        return NotFound();
                    }
                }
            }

            //get relatives of the permit 
            ViewBag.GetRelativs = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.DLT_STS != true).ToList();
            //get zones and gates selected for the current permit 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get documents selected for the current permit 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
          
            return View(rA42_VECHILE_PASS_DTL);
        }

        //this is card view to print card or temprory form or save them 
        [HttpGet]
        public ActionResult Card(int? id)
        {
            ViewBag.activetab = "card";

            if (id == null)
            {
                return NotFound();
            }
            //check if the record is in the db table 
            RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL = db.RA42_VECHILE_PASS_DTL.Find(id);
            if (rA42_VECHILE_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if the current user has authority to view this permit 
            if (ViewBag.RESPO_STATE != rA42_VECHILE_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
            {
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    return NotFound();
                }
            }
            
            //get relatives of the permit 
            ViewBag.GetRelativs = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.DLT_STS != true).ToList();
            //get zones and gates selected for the current permit 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get documents selected for the current permit 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();

            if (Language.GetCurrentLang() == "en")
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_VECHILE_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToList();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_VECHILE_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToList();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }
            return View(rA42_VECHILE_PASS_DTL);
        }

        [HttpPost]
        public ActionResult Card(string CheckPrinted, int TRANSACTION_TYPE_CODE, string TRANSACTION_REMARKS, 
            HttpPostedFileBase RECEIPT, RA42_VECHILE_PASS_DTL _VECHILE_PASS_DTL)
        {
            ViewBag.activetab = "card";

          
            //check if the record is in the db table 
            RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL = db.RA42_VECHILE_PASS_DTL.Find(_VECHILE_PASS_DTL.VECHILE_PASS_CODE);
            if (rA42_VECHILE_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if the current user has authority to view this permit 
            if (ViewBag.RESPO_STATE != rA42_VECHILE_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
            {
                if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                {
                    return NotFound();
                }
            }
           
            //get relatives of the permit 
            ViewBag.GetRelativs = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.ACCESS_ROW_CODE == _VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.DLT_STS != true).ToList();
            //get zones and gates selected for the current permit 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == _VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get documents selected for the current permit 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == _VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();

            if (Language.GetCurrentLang() == "en")
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_VECHILE_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToList();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_VECHILE_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToList();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }
            RA42_TRANSACTION_DTL rA42_TRANSACTION_DTL = new RA42_TRANSACTION_DTL();
            rA42_TRANSACTION_DTL.ACCESS_ROW_CODE = _VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                            return View(rA42_VECHILE_PASS_DTL);
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

            //var permit = db.RA42_VECHILE_PASS_DTL.Where(a => a.VECHILE_PASS_CODE == _VECHILE_PASS_DTL.VECHILE_PASS_CODE).FirstOrDefault();
            rA42_VECHILE_PASS_DTL.UPD_BY = currentUser;
            rA42_VECHILE_PASS_DTL.UPD_DT = DateTime.Now;
            rA42_VECHILE_PASS_DTL.ISDELIVERED = false;
            db.SaveChanges();
            TempData["Success"] = "تم تحديث المعاملة بنجاح";
            if (CheckPrinted.Equals("Printed"))
            {
                var deletePrinted = db.RA42_PRINT_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.PASS_ROW_CODE ==
                rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE).ToList();
                if (deletePrinted.Count > 0)
                {
                    foreach (var item in deletePrinted)
                    {
                        item.DLT_STS = true;
                        db.SaveChanges();
                    }
                }
            }
            return View(rA42_VECHILE_PASS_DTL);
        }


        // this is serach view to create permit for somone in the MOD
        //we get information of employee via service number via api. 
        public ActionResult ForOfficerDriver()
        {


            ViewBag.activetab = "ForOfficerDriver";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];

            //int unit = 0;
            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E");
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E");
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E");
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E");
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E");
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 9 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }
                }
            }
            else
            {
                //get relatives typs in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types codes in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE");
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR");
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT");
                //get colrs in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR");
                //get vechiles types in arabic (صالون - دفع رباعي)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME");
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 9 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }
                }
            }
            return View();
        }

        // post new data 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForOfficerDriver(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files,
            HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "ForOfficerDriver";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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

                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 9 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                // var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
                    }
                }
            }
            else
            {
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get vechile colors in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechile types (صالون - دفع رباعي ...) in arabic 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 9 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
                    }
                }
            }

            //check if employee has more than 2 permits still valid
            //var num_permits = db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.DATE_TO > rA42_VECHILE_PASS_DTL.DATE_TO &&
            //a.SERVICE_NUMBER == rA42_VECHILE_PASS_DTL.SERVICE_NUMBER.ToUpper() && a.ISPRINTED == true).ToList();
            //if (ViewBag.RESPO_CODE < 3)
            //{
            //    if (num_permits.Count >= 2)
            //    {
            //        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("more_than_two_permits" + "_" + ViewBag.lang);
            //        return View(rA42_VECHILE_PASS_DTL);
            //    }
            //}
            //get user military information from api by service number 
            User user = null;
            Task<User> callTask = Task.Run(
                () => (new UserInfo()).getUserInfoFromAPI(rA42_VECHILE_PASS_DTL.SERVICE_NUMBER.ToUpper())
                );
            callTask.Wait();
            user = callTask.Result;

            if (user != null && ModelState.IsValid)
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


                            //check extention of the image 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {

                                fileName = "Profile_3_" + DateTime.Now.ToString("yymmssfff") + extension;

                                // resize image
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_VECHILE_PASS_DTL);
                                }
                                rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = fileName;

                            }
                            else
                            {
                                //show error if the format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_VECHILE_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                //add information 
                rA42_VECHILE_PASS_DTL.CARD_FOR_CODE = 9;
                rA42_VECHILE_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_VECHILE_PASS_DTL.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.SERVICE_NUMBER;
                rA42_VECHILE_PASS_DTL.RESPONSIBLE = currentUser;
                rA42_VECHILE_PASS_DTL.RANK_A = user.NAME_RANK_A;
                rA42_VECHILE_PASS_DTL.RANK_E = user.NAME_RANK_E;
                rA42_VECHILE_PASS_DTL.NAME_A = user.NAME_EMP_A;
                rA42_VECHILE_PASS_DTL.NAME_E = user.NAME_EMP_E;
                if (!string.IsNullOrEmpty(rA42_VECHILE_PASS_DTL.UNIT_A))
                {
                    rA42_VECHILE_PASS_DTL.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;

                }
                else
                {
                    rA42_VECHILE_PASS_DTL.UNIT_A = user.NAME_UNIT_A;
                }
                rA42_VECHILE_PASS_DTL.UNIT_E = user.NAME_UNIT_E;
                rA42_VECHILE_PASS_DTL.PROFESSION_A = user.NAME_TRADE_A;
                rA42_VECHILE_PASS_DTL.PROFESSION_E = user.NAME_TRADE_E;
                rA42_VECHILE_PASS_DTL.CRD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.UPD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_VECHILE_PASS_DTL.BARCODE = rA42_VECHILE_PASS_DTL.BARCODE;

                //get current user info from api 
                User sec = null;
                Task<User> callTask3 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask3.Wait();
                sec = callTask3.Result;

                //this section is for applicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {
                    rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //autho person should redirect the permit to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.APPROVAL_NAME = sec.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_RANK = sec.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for permits cell 
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //permits cell should redirect the permit for the security officer as final step 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.PERMIT_NAME = sec.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_RANK = sec.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;

                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //security officer should redirect the permit to the permits cel for printing
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_VECHILE_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.AUTHO_NAME = sec.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_RANK = sec.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;

                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = true;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //add permit details
                db.RA42_VECHILE_PASS_DTL.Add(rA42_VECHILE_PASS_DTL);
                db.SaveChanges();

                //add zones and gates 
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                        //set documenst in for loop to upload multiple files 
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
                                        ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE,
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
                                    //delete whole documenst if there is somthing wrong with one file, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = " Not supported files - صيغة الملف غير مدعومة";
                                    return View(rA42_VECHILE_PASS_DTL);
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
                               GetResourcesValue("error_update_message"),
                               "red"));
            return View(rA42_VECHILE_PASS_DTL);

        }

        // this is serach view to create permit for somone in the MOD
        //we get information of employee via service number via api. 
        public ActionResult ForSpecialArea()
        {


            ViewBag.activetab = "ForSpecialArea";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];

            //int unit = 0;
            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                 //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E");
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E");
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E");
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E");
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E");
                var zones = new List<string> {
                        "222","1111","111"};
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 8 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            //return RedirectToAction("Create");
                        
                    }
                }
            }
            else
            {
                 //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE");
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR");
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT");
                //get colrs in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR");
                //get vechiles types in arabic (صالون - دفع رباعي)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME");
                var zones = new List<string> {
                        "222","1111","111"};
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE  && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 8 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            //return RedirectToAction("Create");
                        
                    }
                }
            }
            return View();
        }

        // post new data 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForSpecialArea(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files,
            HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "ForSpecialArea";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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

                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                var zones = new List<string> {
                        "222","1111","111"};
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 8 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                // var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                       
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);
                        
                    }
                }
            }
            else
            {
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get vechile colors in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechile types (صالون - دفع رباعي ...) in arabic 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                var zones = new List<string> {
                        "222","1111","111"};
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 8 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                      
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);
                        
                    }
                }
            }

            //check if employee has more than 2 permits still valid
            //var num_permits = db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.DATE_TO > rA42_VECHILE_PASS_DTL.DATE_TO &&
            //a.SERVICE_NUMBER == rA42_VECHILE_PASS_DTL.SERVICE_NUMBER.ToUpper() && a.ISPRINTED == true).ToList();
            //if (ViewBag.RESPO_CODE < 3)
            //{
            //    if (num_permits.Count >= 2)
            //    {
            //        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("more_than_two_permits" + "_" + ViewBag.lang);
            //        return View(rA42_VECHILE_PASS_DTL);
            //    }
            //}
            //get user military information from api by service number 
            User user = null;
            Task<User> callTask = Task.Run(
                () => (new UserInfo()).getUserInfoFromAPI(rA42_VECHILE_PASS_DTL.SERVICE_NUMBER.ToUpper())
                );
            callTask.Wait();
            user = callTask.Result;

            if (user != null && ModelState.IsValid)
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


                            //check extention of the image 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {

                                fileName = "Profile_3_" + DateTime.Now.ToString("yymmssfff") + extension;

                                // resize image
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_VECHILE_PASS_DTL);
                                }
                                rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = fileName;

                            }
                            else
                            {
                                //show error if the format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_VECHILE_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                //add information 
                rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE = 1;
                rA42_VECHILE_PASS_DTL.CARD_FOR_CODE = 8;
                rA42_VECHILE_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_VECHILE_PASS_DTL.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.SERVICE_NUMBER;
                rA42_VECHILE_PASS_DTL.RESPONSIBLE = currentUser;
                rA42_VECHILE_PASS_DTL.RANK_A = user.NAME_RANK_A;
                rA42_VECHILE_PASS_DTL.RANK_E = user.NAME_RANK_E;
                rA42_VECHILE_PASS_DTL.NAME_A = user.NAME_EMP_A;
                rA42_VECHILE_PASS_DTL.NAME_E = user.NAME_EMP_E;
                if (!string.IsNullOrEmpty(rA42_VECHILE_PASS_DTL.UNIT_A))
                {
                    rA42_VECHILE_PASS_DTL.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;

                }
                else
                {
                    rA42_VECHILE_PASS_DTL.UNIT_A = user.NAME_UNIT_A;
                }
                rA42_VECHILE_PASS_DTL.UNIT_E = user.NAME_UNIT_E;
                rA42_VECHILE_PASS_DTL.PROFESSION_A = user.NAME_TRADE_A;
                rA42_VECHILE_PASS_DTL.PROFESSION_E = user.NAME_TRADE_E;
                rA42_VECHILE_PASS_DTL.CRD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.UPD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_VECHILE_PASS_DTL.BARCODE = rA42_VECHILE_PASS_DTL.BARCODE;

                //get current user info from api 
                User sec = null;
                Task<User> callTask3 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask3.Wait();
                sec = callTask3.Result;

                //this section is for applicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {
                    rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = false;
                    
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //autho person should redirect the permit to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.APPROVAL_NAME = sec.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_RANK = sec.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for permits cell 
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //permits cell should redirect the permit for the security officer as final step 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.PERMIT_NAME = sec.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_RANK = sec.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;

                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //security officer should redirect the permit to the permits cel for printing
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_VECHILE_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.AUTHO_NAME = sec.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_RANK = sec.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;

                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = true;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //add permit details
                db.RA42_VECHILE_PASS_DTL.Add(rA42_VECHILE_PASS_DTL);
                db.SaveChanges();

                //add zones and gates 
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                        //set documenst in for loop to upload multiple files 
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
                                        ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE,
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
                                    //delete whole documenst if there is somthing wrong with one file, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = " Not supported files - صيغة الملف غير مدعومة";
                                    return View(rA42_VECHILE_PASS_DTL);
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
                               GetResourcesValue("error_update_message"),
                               "red"));
            return View(rA42_VECHILE_PASS_DTL);

        }


        // this is serach view to create permit for somone in the MOD
        //we get information of employee via service number via api. 
        public ActionResult Search()
        {


            ViewBag.activetab = "Search";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];

            //int unit = 0;
            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
                ViewBag.Get_Station_Code = STATION_CODE.ToString();
                FORCE_ID = check_unit.FORCE_ID.Value;


            }
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE) {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.FORCE_ID == FORCE_ID).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();
            //sections
            ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "SECTION_NAME", "SECTION_NAME");

            if (Language.GetCurrentLang() == "en")
            {
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE ==1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                else
                {
                    //gt permits types in english (مؤقت - ثابت)
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E");
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E");
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E");
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E");
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E");
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 1 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (STATION_CODE != 26)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            //return RedirectToAction("Create");
                        }
                    }
                }
            }
            else
            {
                //get relatives typs in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types codes in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE");
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR");
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true ), "VECHILE_CODE", "VECHILE_CAT");
                //get colrs in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR");
                //get vechiles types in arabic (صالون - دفع رباعي)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME");
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 1 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME}).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (STATION_CODE != 26)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            //return RedirectToAction("Create");
                        }
                    }
                }
            }
            return View();
        }

        // post new data 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Search(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES,string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, 
            HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "Search";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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

                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E",rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E",rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E",rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E",rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E",rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E",rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E",rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E",rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E",rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 1 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                // var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME",rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (STATION_CODE != 26)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);
                        }
                    }
                }
            }
            else
            {
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get vechile colors in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechile types (صالون - دفع رباعي ...) in arabic 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 1 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (STATION_CODE != 26)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);
                        }
                    }
                }
            }

            //check if employee has more than 2 permits still valid
            var num_permits = db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.DATE_TO > DateTime.Today && a.STATION_CODE == STATION_CODE && 
            a.SERVICE_NUMBER == rA42_VECHILE_PASS_DTL.SERVICE_NUMBER.ToUpper() && (a.ISPRINTED == true || a.STATUS !=true || a.REJECTED == true) && a.RETURNED != true && a.CARD_FOR_CODE == 1).ToList();
            if (ViewBag.RESPO_STATE < 3)
            {
                if (num_permits.Count >= 2)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("more_than_two_permits" + "_" + ViewBag.lang);
                    return View(rA42_VECHILE_PASS_DTL);
                }
            }
            //get user military information from api by service number 
            User user = null;
            Task<User> callTask = Task.Run(
                () => (new UserInfo()).getUserInfoFromAPI(rA42_VECHILE_PASS_DTL.SERVICE_NUMBER.ToUpper())
                );
            callTask.Wait();
            user = callTask.Result;

            if (user != null && ModelState.IsValid)
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


                            //check extention of the image 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {

                                fileName = "Profile_3_" + DateTime.Now.ToString("yymmssfff") + extension;
                               
                                // resize image
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_VECHILE_PASS_DTL);
                                }
                                rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = fileName;

                            }
                            else
                            {
                                //show error if the format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_VECHILE_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                //add information 
                rA42_VECHILE_PASS_DTL.CARD_FOR_CODE = 1;
                rA42_VECHILE_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_VECHILE_PASS_DTL.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.SERVICE_NUMBER;
                rA42_VECHILE_PASS_DTL.RESPONSIBLE = currentUser;
                rA42_VECHILE_PASS_DTL.RANK_A = user.NAME_RANK_A;
                rA42_VECHILE_PASS_DTL.RANK_E = user.NAME_RANK_E;
                rA42_VECHILE_PASS_DTL.NAME_A = user.NAME_EMP_A;
                rA42_VECHILE_PASS_DTL.NAME_E = user.NAME_EMP_E;
                if (!string.IsNullOrEmpty(rA42_VECHILE_PASS_DTL.UNIT_A))
                {
                    rA42_VECHILE_PASS_DTL.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;

                }
                else
                {
                    rA42_VECHILE_PASS_DTL.UNIT_A = user.NAME_UNIT_A;
                }
                rA42_VECHILE_PASS_DTL.UNIT_E = user.NAME_UNIT_E;
                rA42_VECHILE_PASS_DTL.PROFESSION_A = user.NAME_TRADE_A;
                rA42_VECHILE_PASS_DTL.PROFESSION_E = user.NAME_TRADE_E;
                rA42_VECHILE_PASS_DTL.CRD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.UPD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                if(FORCE_ID == 2)
                {
                    rA42_VECHILE_PASS_DTL.DATE_TO = rA42_VECHILE_PASS_DTL.DATE_TO.Value.AddDays(30);
                }
               
                //get current user info from api 
                User sec = null;
                Task<User> callTask3 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask3.Wait();
                sec = callTask3.Result;
              
                //this section is for applicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {
                   
                    if(STATION_CODE == 26)
                    {
                        //autho person should redirect the permit to the permits cell 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);

                        }
                        else
                        {
                            rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }
                        rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                        rA42_VECHILE_PASS_DTL.APPROVAL_NAME = sec.NAME_EMP_A;
                        rA42_VECHILE_PASS_DTL.APPROVAL_RANK = sec.NAME_RANK_A;
                        rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                        rA42_VECHILE_PASS_DTL.REJECTED = false;
                        rA42_VECHILE_PASS_DTL.STATUS = false;
                        rA42_VECHILE_PASS_DTL.ISOPENED = true;
                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                        rA42_VECHILE_PASS_DTL.REJECTED = false;
                        rA42_VECHILE_PASS_DTL.STATUS = false;
                        rA42_VECHILE_PASS_DTL.ISOPENED = false;
                    }
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //autho person should redirect the permit to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.APPROVAL_NAME = sec.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_RANK = sec.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for permits cell 
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    if (STATION_CODE == 26 && WORKFLOWID == 3)
                    {
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);

                        }
                        else
                        {
                            rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }

                        rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                        rA42_VECHILE_PASS_DTL.PERMIT_NAME = sec.NAME_EMP_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_RANK = sec.NAME_RANK_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;

                        rA42_VECHILE_PASS_DTL.REJECTED = false;
                        rA42_VECHILE_PASS_DTL.STATUS = true;
                        rA42_VECHILE_PASS_DTL.ISOPENED = true;
                        string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                        rA42_VECHILE_PASS_DTL.BARCODE = barcode;


                    }
                    else
                    {
                        //permits cell should redirect the permit for the security officer as final step 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);

                        }
                        else
                        {
                            rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }
                        rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                        rA42_VECHILE_PASS_DTL.PERMIT_NAME = sec.NAME_EMP_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_RANK = sec.NAME_RANK_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                        rA42_VECHILE_PASS_DTL.REJECTED = false;

                        rA42_VECHILE_PASS_DTL.STATUS = false;
                        rA42_VECHILE_PASS_DTL.ISOPENED = true;
                    }
                }
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //security officer should redirect the permit to the permits cel for printing
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                
                    rA42_VECHILE_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.AUTHO_NAME = sec.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_RANK = sec.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;

                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = true;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                    rA42_VECHILE_PASS_DTL.BARCODE = rA42_VECHILE_PASS_DTL.BARCODE;

                }
                //add permit details
                db.RA42_VECHILE_PASS_DTL.Add(rA42_VECHILE_PASS_DTL);
                db.SaveChanges();

                //add zones and gates 
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                        //set documenst in for loop to upload multiple files 
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
                                        ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE,
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
                                    //delete whole documenst if there is somthing wrong with one file, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = " Not supported files - صيغة الملف غير مدعومة";
                                    return View(rA42_VECHILE_PASS_DTL);
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
                               GetResourcesValue("error_update_message"),
                               "red"));
            return View(rA42_VECHILE_PASS_DTL);

        }

        // this view to create vechile permit for family, this should user it self create it for his family
        public ActionResult Familyvechilepass()
        {
            ViewBag.activetab = "Familypass";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E");
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E");
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E");
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E");
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E");
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 6 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (STATION_CODE != 26)
                {
                    if (WORKFLOW_RESPO.Count == 0)
                    {

                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");

                    }
                }
            }
            else
            {
                //get relatives typs in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types codes in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE");
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR");
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT");
                //get colrs in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR");
                //get vechiles types in arabic (صالون - دفع رباعي)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME");
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true), "ZONE_CODE", "ZONE_NAME");
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 6 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                if (STATION_CODE != 26)
                {
                    if (WORKFLOW_RESPO.Count == 0)
                    {

                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");

                    }
                }
            }



            return View();
        }
        // POST data 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Familyvechilepass(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL, int[] RELATIVE_TYPES, HttpPostedFileBase[] RELATIVE_IMAGE, int[] IDENTITY_TYPES, int[] GENDER_TYPES, string[] FULL_NAME, string[] CIVIL_NUM, string[] PASSPORT_NUMBER, string[] PHONE_NUMBER_M
            , string[] REMARKS_LIST,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "Familypass";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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

                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 6 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (STATION_CODE != 26)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
                    }

                }
            }
            else
            {
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get vechile colors in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechile types (صالون - دفع رباعي ...) in arabic 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 6 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (STATION_CODE != 26)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
                    }
                }
            }

            if (ModelState.IsValid)
            {
                if (rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE == null)
                {
                    if (STATION_CODE != 26)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
                    }

                }
                ViewBag.Service_No = currentUser;

                //check if current user upload personal image 
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


                                fileName = "Profile_3_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_VECHILE_PASS_DTL);
                                }
                                rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if extention not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_VECHILE_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                rA42_VECHILE_PASS_DTL.CARD_FOR_CODE = 6;
                rA42_VECHILE_PASS_DTL.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.SERVICE_NUMBER;
                rA42_VECHILE_PASS_DTL.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                rA42_VECHILE_PASS_DTL.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                rA42_VECHILE_PASS_DTL.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                rA42_VECHILE_PASS_DTL.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                rA42_VECHILE_PASS_DTL.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                rA42_VECHILE_PASS_DTL.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                rA42_VECHILE_PASS_DTL.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                rA42_VECHILE_PASS_DTL.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                //rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                rA42_VECHILE_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_VECHILE_PASS_DTL.CRD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.UPD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                if (FORCE_ID == 2)
                {
                    rA42_VECHILE_PASS_DTL.DATE_TO = rA42_VECHILE_PASS_DTL.DATE_TO.Value.AddDays(30);
                }
                //get current user info from api 
                User sec = null;
                Task<User> callTask3 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask3.Wait();
                sec = callTask3.Result;

                //this section is for applicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {
                    if (STATION_CODE == 26)
                    {
                        //autho person should redirect the permit to the permits cell 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);

                        }
                        else
                        {
                            rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }
                        rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                        rA42_VECHILE_PASS_DTL.APPROVAL_NAME = sec.NAME_EMP_A;
                        rA42_VECHILE_PASS_DTL.APPROVAL_RANK = sec.NAME_RANK_A;
                        rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                        rA42_VECHILE_PASS_DTL.REJECTED = false;
                        rA42_VECHILE_PASS_DTL.STATUS = false;
                        rA42_VECHILE_PASS_DTL.ISOPENED = true;
                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                        rA42_VECHILE_PASS_DTL.REJECTED = false;
                        rA42_VECHILE_PASS_DTL.STATUS = false;
                        rA42_VECHILE_PASS_DTL.ISOPENED = false;
                    }
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //autho person should redirect the permit to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.APPROVAL_NAME = sec.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_RANK = sec.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for permits cell 
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    if (STATION_CODE == 26) {

                        //security officer should redirect the permit to the permits cel for printing
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);

                        }
                        else
                        {
                            rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }

                        rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                        rA42_VECHILE_PASS_DTL.PERMIT_NAME = sec.NAME_EMP_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_RANK = sec.NAME_RANK_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                        rA42_VECHILE_PASS_DTL.REJECTED = false;
                        rA42_VECHILE_PASS_DTL.STATUS = true;
                        rA42_VECHILE_PASS_DTL.ISOPENED = true;
                        string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                        rA42_VECHILE_PASS_DTL.BARCODE = barcode;



                    }
                    else
                    {
                        //permits cell should redirect the permit for the security officer as final step 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);

                        }
                        else
                        {
                            rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }
                        rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                        rA42_VECHILE_PASS_DTL.PERMIT_NAME = sec.NAME_EMP_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_RANK = sec.NAME_RANK_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                        rA42_VECHILE_PASS_DTL.REJECTED = false;

                        rA42_VECHILE_PASS_DTL.STATUS = false;
                        rA42_VECHILE_PASS_DTL.ISOPENED = true;
                    }
                }
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //security officer should redirect the permit to the permits cel for printing
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_VECHILE_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.AUTHO_NAME = sec.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_RANK = sec.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.BARCODE = rA42_VECHILE_PASS_DTL.BARCODE;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = true;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                db.RA42_VECHILE_PASS_DTL.Add(rA42_VECHILE_PASS_DTL);
                db.SaveChanges();

                //add selected gates and zones 
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
                        rA42_ZONE_MASTER_MST.ZONE_CODE = ZONE[i];
                        rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                        rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                        db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                        db.SaveChanges();
                        //continue;
                    }

                }
                //add selectd documents
                if (files != null)
                {
                    
                    try
                    {

                        //create for loop to upload multiple documents 
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
                                        ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE,
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
                                    //delete all uploaded documents if there is any problem with one file, this is security procedures
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported format - صيغة الملف غير مدعومة";
                                    return View(rA42_VECHILE_PASS_DTL);
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
                            rA42_MEMBERS_DTL.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                                            return View(rA42_VECHILE_PASS_DTL);
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
                                        return View(rA42_VECHILE_PASS_DTL);
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
                        return View(rA42_VECHILE_PASS_DTL);
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
            return View(rA42_VECHILE_PASS_DTL);
        }


        // this view is for applicant to create car permit for some one family, beacuase we need here military information for responsible person 
        public ActionResult Supercreate()
        {
            ViewBag.activetab = "Supercreate";
            ViewBag.Service_No = currentUser;


            var url = Url.RequestContext.RouteData.Values["id"];

            //int unit = 0;
            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E");
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E");
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E");
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E");
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E");
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 6 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (STATION_CODE != 26)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            //return RedirectToAction("Create");
                        }
                    }
                }
            }
            else
            {
                //get relatives typs in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types codes in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE");
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR");
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT");
                //get colrs in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR");
                //get vechiles types in arabic (صالون - دفع رباعي)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME");
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 6 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (STATION_CODE != 26)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            //return RedirectToAction("Create");
                        }
                    }
                }
            }



            return View();
        }
        // POST data 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Supercreate(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL, int[] RELATIVE_TYPES, HttpPostedFileBase[] RELATIVE_IMAGE, int[] IDENTITY_TYPES, int[] GENDER_TYPES, string[] FULL_NAME, string[] CIVIL_NUM, string[] PASSPORT_NUMBER, string[] PHONE_NUMBER_M
            , string[] REMARKS_LIST,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "Supercreate";
            ViewBag.Service_No = currentUser;

            var url = Url.RequestContext.RouteData.Values["id"];

            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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

                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 6 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (STATION_CODE != 26)
                        {

                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);
                        }
                    }
                }
            }
            else
            {
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get vechile colors in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechile types (صالون - دفع رباعي ...) in arabic 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 6 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (STATION_CODE != 26)
                        {

                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);
                        }
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


                            //check extention of image 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {


                                fileName = "Profile_3_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_VECHILE_PASS_DTL);
                                }
                                rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_VECHILE_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                rA42_VECHILE_PASS_DTL.CARD_FOR_CODE = 6;
                rA42_VECHILE_PASS_DTL.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.SERVICE_NUMBER.ToUpper();
                rA42_VECHILE_PASS_DTL.RESPONSIBLE = currentUser;
                rA42_VECHILE_PASS_DTL.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                rA42_VECHILE_PASS_DTL.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                rA42_VECHILE_PASS_DTL.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                rA42_VECHILE_PASS_DTL.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                rA42_VECHILE_PASS_DTL.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                rA42_VECHILE_PASS_DTL.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                rA42_VECHILE_PASS_DTL.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                rA42_VECHILE_PASS_DTL.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                rA42_VECHILE_PASS_DTL.STATION_CODE = STATION_CODE;
                if (FORCE_ID == 2)
                {
                    rA42_VECHILE_PASS_DTL.DATE_TO = rA42_VECHILE_PASS_DTL.DATE_TO.Value.AddDays(30);
                }
                //get current user military information from api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;

                //this section is for applicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {

                    if (STATION_CODE == 26)
                    {
                        //autho person should redirect the permit to the permits cell 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);

                        }
                        else
                        {
                            rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }
                        rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                        rA42_VECHILE_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                        rA42_VECHILE_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                        rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                        rA42_VECHILE_PASS_DTL.REJECTED = false;
                        rA42_VECHILE_PASS_DTL.STATUS = false;
                        rA42_VECHILE_PASS_DTL.ISOPENED = true;
                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                        rA42_VECHILE_PASS_DTL.REJECTED = false;
                        rA42_VECHILE_PASS_DTL.STATUS = false;
                        rA42_VECHILE_PASS_DTL.ISOPENED = false;
                    }
                }

                //this section is for autho pperson (المنسق الأمني)
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect the permit for permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for permits cell 
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    if (STATION_CODE == 26)
                    {
                        //security officer should redirect the permit to the permits cell for print the permit 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);

                        }
                        else
                        {
                            rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }
                        rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                        rA42_VECHILE_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                        rA42_VECHILE_PASS_DTL.REJECTED = false;
                        rA42_VECHILE_PASS_DTL.STATUS = true;
                        rA42_VECHILE_PASS_DTL.ISOPENED = true;
                        string barcode = Guid.NewGuid().ToString().Substring(0, 5);

                        rA42_VECHILE_PASS_DTL.BARCODE = barcode;

                    }
                    else
                    {
                        //permits cell should redirect the request to the security officer 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);

                        }
                        else
                        {
                            rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }
                        rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                        rA42_VECHILE_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;


                        rA42_VECHILE_PASS_DTL.REJECTED = false;
                        rA42_VECHILE_PASS_DTL.STATUS = false;
                        rA42_VECHILE_PASS_DTL.ISOPENED = true;
                    }
                }
                //this section is for security officer
                if(WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //security officer should redirect the permit to the permits cell for print the permit 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;

                  
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = true;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                    rA42_VECHILE_PASS_DTL.BARCODE = rA42_VECHILE_PASS_DTL.BARCODE;

                }

                rA42_VECHILE_PASS_DTL.CRD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.UPD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
             
                db.RA42_VECHILE_PASS_DTL.Add(rA42_VECHILE_PASS_DTL);
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
                        rA42_ZONE_MASTER_MST.ZONE_CODE = ZONE[i];
                        rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                        rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                        db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                        db.SaveChanges();
                        //continue;
                    }

                }
                //add selected documents and files 
                if (files != null)
                {
                   
                    try
                    {
                        //show 
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
                                        ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE,
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
                                    //delete all uploaded documents for this request if there is somthing wrong with one file, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_VECHILE_PASS_DTL);
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
                //}
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
                            rA42_MEMBERS_DTL.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                                            return View(rA42_VECHILE_PASS_DTL);
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
                                        return View(rA42_VECHILE_PASS_DTL);
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
                        return View(rA42_VECHILE_PASS_DTL);
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
            return View(rA42_VECHILE_PASS_DTL);
        }


        //permit for others outside of MOD
        public ActionResult Otherpermit()
        {

            ViewBag.activetab = "Otherpermit";
            //ViewBag.activetab = "Search";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];

            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E");
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E");
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E");
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E");
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E");
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E");
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
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }
                }
            }
            else
            {
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME");
                //get relatives typs in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types codes in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE");
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR");
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT");
                //get colrs in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR");
                //get vechiles types in arabic (صالون - دفع رباعي)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME");
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
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }
                }
            }



            return View();
        }
        // POST data 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Otherpermit(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES,string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "Otherpermit";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];


            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
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
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
                    }
                }
            }
            else
            {
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get vechile colors in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechile types (صالون - دفع رباعي ...) in arabic 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
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
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
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


                            //check image format 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {


                                fileName = "Profile_3_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_VECHILE_PASS_DTL);
                                }
                                rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_VECHILE_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                rA42_VECHILE_PASS_DTL.CARD_FOR_CODE = 3;
                rA42_VECHILE_PASS_DTL.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.RESPONSIBLE;
                rA42_VECHILE_PASS_DTL.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                rA42_VECHILE_PASS_DTL.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                rA42_VECHILE_PASS_DTL.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                rA42_VECHILE_PASS_DTL.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                rA42_VECHILE_PASS_DTL.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                rA42_VECHILE_PASS_DTL.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                rA42_VECHILE_PASS_DTL.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                rA42_VECHILE_PASS_DTL.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                rA42_VECHILE_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_VECHILE_PASS_DTL.RESPONSIBLE = currentUser;

                //get user military info from api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;

                //this section is for apllicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {

                    rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect th permit to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for permits cell 
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //permits cell should redirect the permit for the security officer 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;


                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for security officer 
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect the permit for permits cell for printing 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                   
                    rA42_VECHILE_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = true;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }

                rA42_VECHILE_PASS_DTL.CRD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.UPD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_VECHILE_PASS_DTL.BARCODE = rA42_VECHILE_PASS_DTL.BARCODE;

                db.RA42_VECHILE_PASS_DTL.Add(rA42_VECHILE_PASS_DTL);
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                                        ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE,
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
                                    //delete all uploaded files if there is problem with one file, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_VECHILE_PASS_DTL);
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
            return View(rA42_VECHILE_PASS_DTL);
        }


        // this view is for adding permit for someone work in companies or other orgnizations
        public ActionResult Companypermit()
        {
            
            ViewBag.activetab = "Companypermit";
            //ViewBag.activetab = "Search";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            
            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E");
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E");
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E");
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E");
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E");
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E");
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
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
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }
                }
            }
            else
            {
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME");
                //get relatives typs in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types codes in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE");
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR");
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT");
                //get colrs in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR");
                //get vechiles types in arabic (صالون - دفع رباعي)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME");
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
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
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }
                }
            }



            return View();
        }
        // POST data 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Companypermit(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL, int[] RELATIVE_TYPES, HttpPostedFileBase[] RELATIVE_IMAGE, int[] IDENTITY_TYPES, int[] GENDER_TYPES, string[] FULL_NAME, string[] CIVIL_NUM, string[] PASSPORT_NUMBER, string[] PHONE_NUMBER_M
            , string[] REMARKS_LIST,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES,string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "Companypermit";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            
           
            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E",rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
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
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
                    }
                }
            }
            else
            {
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get vechile colors in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechile types (صالون - دفع رباعي ...) in arabic 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
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
                // var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
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


                            //check image format 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {


                                fileName = "Profile_3_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_VECHILE_PASS_DTL);
                                }
                                rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_VECHILE_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                rA42_VECHILE_PASS_DTL.CARD_FOR_CODE = 2;
                rA42_VECHILE_PASS_DTL.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.RESPONSIBLE;
                rA42_VECHILE_PASS_DTL.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                rA42_VECHILE_PASS_DTL.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                rA42_VECHILE_PASS_DTL.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                rA42_VECHILE_PASS_DTL.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                rA42_VECHILE_PASS_DTL.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                rA42_VECHILE_PASS_DTL.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                rA42_VECHILE_PASS_DTL.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                rA42_VECHILE_PASS_DTL.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                rA42_VECHILE_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_VECHILE_PASS_DTL.RESPONSIBLE = currentUser;
               
                //get user military info from api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;

                //this section is for apllicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {

                    rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect th permit to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for permits cell 
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //permits cell should redirect the permit for the security officer 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                   

                    rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;


                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for security officer 
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect the permit for permits cell for printing 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = true;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
            
                rA42_VECHILE_PASS_DTL.CRD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.UPD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_VECHILE_PASS_DTL.BARCODE = rA42_VECHILE_PASS_DTL.BARCODE;

                db.RA42_VECHILE_PASS_DTL.Add(rA42_VECHILE_PASS_DTL);
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                                        ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE,
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
                                    //delete all uploaded files if there is problem with one file, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_VECHILE_PASS_DTL);
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
                            rA42_MEMBERS_DTL.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                                            return View(rA42_VECHILE_PASS_DTL);
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
                                        return View(rA42_VECHILE_PASS_DTL);
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
                        return View(rA42_VECHILE_PASS_DTL);
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
            return View(rA42_VECHILE_PASS_DTL);
        }


        // this view is for adding permit for someone work in companies or other orgnizations
        public ActionResult Companypermitgeneraluse()
        {

            ViewBag.activetab = "Companypermitgeneraluse";
            //ViewBag.activetab = "Search";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];

            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E");
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E");
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E");
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E");
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E");
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E");
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 4 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }
                }
            }
            else
            {
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME");
                //get relatives typs in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types codes in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE");
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR");
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT");
                //get colrs in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR");
                //get vechiles types in arabic (صالون - دفع رباعي)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME");
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 4 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }
                }
            }



            return View();
        }
        // POST data 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Companypermitgeneraluse(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES,string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "Companypermitgeneraluse";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];


            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 4 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
                    }
                }
            }
            else
            {
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get vechile colors in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechile types (صالون - دفع رباعي ...) in arabic 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 4 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
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


                            //check image format 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {


                                fileName = "Profile_3_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_VECHILE_PASS_DTL);
                                }
                                rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_VECHILE_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                rA42_VECHILE_PASS_DTL.CARD_FOR_CODE = 4;
                rA42_VECHILE_PASS_DTL.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.RESPONSIBLE;
                rA42_VECHILE_PASS_DTL.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                rA42_VECHILE_PASS_DTL.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                rA42_VECHILE_PASS_DTL.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                rA42_VECHILE_PASS_DTL.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                rA42_VECHILE_PASS_DTL.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                rA42_VECHILE_PASS_DTL.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                rA42_VECHILE_PASS_DTL.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                rA42_VECHILE_PASS_DTL.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                rA42_VECHILE_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_VECHILE_PASS_DTL.RESPONSIBLE = currentUser;

                //get user military info from api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;

                //this section is for apllicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {

                    rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect th permit to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for permits cell 
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //permits cell should redirect the permit for the security officer 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;


                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for security officer 
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect the permit for permits cell for printing 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    
                    rA42_VECHILE_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = true;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }

                rA42_VECHILE_PASS_DTL.CRD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.UPD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_VECHILE_PASS_DTL.BARCODE = rA42_VECHILE_PASS_DTL.BARCODE;

                db.RA42_VECHILE_PASS_DTL.Add(rA42_VECHILE_PASS_DTL);
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                                        ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE,
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
                                    //delete all uploaded files if there is problem with one file, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_VECHILE_PASS_DTL);
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
            return View(rA42_VECHILE_PASS_DTL);
        }
        // this view is create permit for every employee in MOD to create permit with normal workflow way
        public ActionResult Create()
        {
            ViewBag.activetab = "Create";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E");
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E");
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E");
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E");
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E");
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 1 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (FORCE_ID != 3)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }

                }
            }
            else
            {
                //get relatives typs in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types codes in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE");
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR");
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT");
                //get colrs in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR");
                //get vechiles types in arabic (صالون - دفع رباعي)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME");
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE ==1 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                // var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (FORCE_ID != 3)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }
                }
            }


            return View();
        }

        // POST: RA42_VECHILE_PASS_DTL/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES,string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "Create";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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

                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 1 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (FORCE_ID != 3)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
                    }
                }
            }
            else
            {
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get vechile colors in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechile types (صالون - دفع رباعي ...) in arabic 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 1 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (FORCE_ID != 3)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
                    }
                }
            }

            //check if employee has more than 2 permits still valid
            var num_permits = db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.DATE_TO > DateTime.Today && a.STATION_CODE == STATION_CODE &&
            a.SERVICE_NUMBER == rA42_VECHILE_PASS_DTL.SERVICE_NUMBER.ToUpper() && (a.ISPRINTED == true || a.STATUS != true || a.REJECTED == true) && a.RETURNED != true && a.CARD_FOR_CODE == 1).ToList();
            if (ViewBag.RESPO_STATE < 3)
            {
                if (num_permits.Count >= 2)
                {
                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("more_than_two_permits" + "_" + ViewBag.lang);
                    return View(rA42_VECHILE_PASS_DTL);
                }
            }

            if (ModelState.IsValid)
            {


                //check if user upload PERSONAL_IMAGE 
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


                                fileName = "Profile_3_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_VECHILE_PASS_DTL);
                                }
                                rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if formate not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_VECHILE_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                //get current user military information from api 
                User user1 = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user1 = callTask.Result;
                rA42_VECHILE_PASS_DTL.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.SERVICE_NUMBER;
                rA42_VECHILE_PASS_DTL.CARD_FOR_CODE = 1;
                rA42_VECHILE_PASS_DTL.RANK_A = user1.NAME_RANK_A;
                rA42_VECHILE_PASS_DTL.RANK_E = user1.NAME_RANK_E;
                rA42_VECHILE_PASS_DTL.NAME_A = user1.NAME_EMP_A;
                rA42_VECHILE_PASS_DTL.NAME_E = user1.NAME_EMP_E;
                if (!string.IsNullOrEmpty(rA42_VECHILE_PASS_DTL.UNIT_A))
                {
                    rA42_VECHILE_PASS_DTL.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;

                }
                else
                {
                    rA42_VECHILE_PASS_DTL.UNIT_A = user1.NAME_UNIT_A;
                }
                rA42_VECHILE_PASS_DTL.UNIT_E = user1.NAME_UNIT_E;
                rA42_VECHILE_PASS_DTL.PROFESSION_A = user1.NAME_TRADE_A;
                rA42_VECHILE_PASS_DTL.PROFESSION_E = user1.NAME_TRADE_E;
                
                rA42_VECHILE_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_VECHILE_PASS_DTL.CRD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.UPD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_VECHILE_PASS_DTL.REJECTED = false;
                if (FORCE_ID == 2)
                {
                    rA42_VECHILE_PASS_DTL.DATE_TO = rA42_VECHILE_PASS_DTL.DATE_TO.Value.AddDays(30);
                }
                //get user military info from api 
                User user = null;
                Task<User> callTask1 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask1.Wait();
                user = callTask1.Result;

                //this section is for apllicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {



                    if (STATION_CODE == 26)
                    {
                        //he should redirect th permit to the permits cell 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);

                        }
                        else
                        {
                            rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }
                        rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                        rA42_VECHILE_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                        rA42_VECHILE_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                        rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                        rA42_VECHILE_PASS_DTL.REJECTED = false;
                        rA42_VECHILE_PASS_DTL.STATUS = false;
                        rA42_VECHILE_PASS_DTL.ISOPENED = true;
                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                        rA42_VECHILE_PASS_DTL.REJECTED = false;
                        rA42_VECHILE_PASS_DTL.STATUS = false;
                        rA42_VECHILE_PASS_DTL.ISOPENED = false;
                    }
                    
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect th permit to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for permits cell 
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    if (STATION_CODE == 26)
                    {
                        //he should redirect the permit for permits cell for printing 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);

                        }
                        else
                        {
                            rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }

                        rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                        rA42_VECHILE_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                        rA42_VECHILE_PASS_DTL.REJECTED = false;
                        rA42_VECHILE_PASS_DTL.STATUS = true;
                        rA42_VECHILE_PASS_DTL.ISOPENED = true;
                        string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                        rA42_VECHILE_PASS_DTL.BARCODE = barcode;
                    }
                    else
                    {
                        //permits cell should redirect the permit for the security officer 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);

                        }
                        else
                        {
                            rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }

                        rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                        rA42_VECHILE_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;


                        rA42_VECHILE_PASS_DTL.REJECTED = false;
                        rA42_VECHILE_PASS_DTL.STATUS = false;
                        rA42_VECHILE_PASS_DTL.ISOPENED = true;
                    }
                }
                //this section is for security officer 
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect the permit for permits cell for printing 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_VECHILE_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = true;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                    rA42_VECHILE_PASS_DTL.BARCODE = rA42_VECHILE_PASS_DTL.BARCODE;

                }
                db.RA42_VECHILE_PASS_DTL.Add(rA42_VECHILE_PASS_DTL);
                db.SaveChanges();

                //add selected zones and gates from api 
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                                        ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE,
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
                                    //delete all uploaded files if there is somthing wrong with one file, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_VECHILE_PASS_DTL);
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
                return RedirectToAction("Index","Mypasses");
            }



            AddToast(new Toast("",
                GetResourcesValue("error_create_message"),
                "red"));
            return View(rA42_VECHILE_PASS_DTL);
        }

        // this view to proccess any request 
        public ActionResult Edit(int? id)
        {
            ViewBag.activetab = "edit";

            if (id == null)
            {
                return NotFound();
            }
            //check if request in the db table 
            RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL = db.RA42_VECHILE_PASS_DTL.Find(id);
            if (rA42_VECHILE_PASS_DTL == null)
            {
                return NotFound();
            }
            //check authority to proccess this request 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_VECHILE_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_VECHILE_PASS_DTL.RESPONSIBLE != currentUser)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER !=true)
                    {
                        return NotFound();
                    }
                }
                if (rA42_VECHILE_PASS_DTL.ISOPENED == true)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        if (rA42_VECHILE_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && rA42_VECHILE_PASS_DTL.REJECTED == true)
                        {

                        }
                        else
                        {
                            if (rA42_VECHILE_PASS_DTL.ISOPENED == true)
                            {
                                return NotFound();
                            }
                        }

                    }
                }
            }
            else
            {
                if (rA42_VECHILE_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_VECHILE_PASS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != rA42_VECHILE_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
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
                //get violation type
                ViewBag.VIOLATION_CODE = new SelectList(db.RA42_VIOLATIONS_MST.Where(a => a.DLT_STS != true), "VIOLATION_CODE", "VIOLATION_TYPE_E");
                //get company name
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == rA42_VECHILE_PASS_DTL.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get realtives types in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (rA42_VECHILE_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plates types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plates chars in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E",rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in english 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
               //get colors of vechiles in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles types in english (صالون - دفع رباعي ....)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
               //get zones and gates in english
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_VECHILE_PASS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_VECHILE_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_VECHILE_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                 //get responsible person (المنسق الأمني)-- this option will be for applicant to change autho person if he wants
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_VECHILE_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME",rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //throw error if there is no responsible. 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (rA42_VECHILE_PASS_DTL.STATION_CODE == 26)
                        {
                            var cards = new List<string> {
                                                            "1","6"};
                            if (!cards.Contains(rA42_VECHILE_PASS_DTL.CARD_FOR_CODE.ToString()))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                            }
                        }
                        else
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        }

                    }

                }

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_VECHILE_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
            else
            {
                //get violation type
                ViewBag.VIOLATION_CODE = new SelectList(db.RA42_VIOLATIONS_MST.Where(a => a.DLT_STS != true), "VIOLATION_CODE", "VIOLATION_TYPE_A");
                //get company name
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == rA42_VECHILE_PASS_DTL.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (rA42_VECHILE_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plates chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR",rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in arabic 
               
               ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
               
                //get vechiles colors in arabic
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles types (صالون - دفع رباعي ....) in arabic
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_VECHILE_PASS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_VECHILE_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_VECHILE_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get responsible person (المنسق الأمني)-- this option will be for applicant to change autho person if he wants in arabic
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_VECHILE_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {                  
                    //throw error if there is no responsible. 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (rA42_VECHILE_PASS_DTL.STATION_CODE == 26)
                        {
                            var cards = new List<string> {
                                                            "1","6"};
                            if (!cards.Contains(rA42_VECHILE_PASS_DTL.CARD_FOR_CODE.ToString()))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                            }
                        }
                        else
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        }
                    }

                }

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a =>  a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_VECHILE_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME+ " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
            //get relatives of this permit 
            ViewBag.GetRelativs = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.DLT_STS != true).ToList();
            //get selected zones and gates
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get selected documenst 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get documenst for this kind of permit to check missing documenst and make compare
            // ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_VECHILE_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_VECHILE_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_VECHILE_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_VECHILE_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();

            //get relatives 
            ViewBag.GetRelativs = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.ACCESS_ROW_CODE == id && a.DLT_STS != true).ToList();
            //get comments of the request
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            //get violations of the permit 
            var vIOLATIONS = db.RA42_VECHILE_VIOLATION_DTL.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.VIOLATIONS = vIOLATIONS;
            //get personal image 
            ViewBag.PERSONAL_IMAGE = rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE;
            //get status of the request
            ViewBag.STATUS = rA42_VECHILE_PASS_DTL.STATUS;
            ViewBag.ISPRINTED = rA42_VECHILE_PASS_DTL.ISPRINTED;

            return View(rA42_VECHILE_PASS_DTL);


        }

        //POST data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL, string COMMENT, int[] RELATIVE_TYPES, HttpPostedFileBase[] RELATIVE_IMAGE, int[] IDENTITY_TYPES, int[] GENDER_TYPES, string[] FULL_NAME, string[] CIVIL_NUM, string[] PASSPORT_NUMBER, string[] PHONE_NUMBER_M
            , string[] REMARKS_LIST,
             FormCollection form,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES,string[] FILE_TYPES_TEXT, HttpPostedFileBase[] 
            files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            //check if the request id is in the db table 
            var general_data = db.RA42_VECHILE_PASS_DTL.Where(a => a.VECHILE_PASS_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE).FirstOrDefault();
            ViewBag.activetab = "edit";
            //get selected zones and gates
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
            //get selected documents
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
            //get relatives of this permit 
            ViewBag.GetRelativs = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.ACCESS_ROW_CODE == general_data.VECHILE_PASS_CODE && a.DLT_STS != true).ToList();
            //get documents types for this kind of permit to check missing files 
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
            //get comments of the request
            var cOMMENTS_MSTs = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
            ViewBag.COMMENTS = cOMMENTS_MSTs;
            //get vilations of the permit
            var vIOLATIONS = db.RA42_VECHILE_VIOLATION_DTL.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
            ViewBag.VIOLATIONS = vIOLATIONS;
            //get personal image 
            ViewBag.PERSONAL_IMAGE = general_data.PERSONAL_IMAGE;
            //get status of the request
            ViewBag.STATUS = general_data.STATUS;
            ViewBag.ISPRINTED = general_data.ISPRINTED;
            if (Language.GetCurrentLang() == "en")
            {
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == general_data.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get realtives types in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (general_data.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plates types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plates chars in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in english 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
               //get colors of vechiles in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles types in english (صالون - دفع رباعي ....)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
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
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get responsible person (المنسق الأمني)-- this option will be for applicant to change autho person if he wants in english
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (general_data.STATION_CODE == 26)
                        {
                            var cards = new List<string> {
                                                            "1","6"};
                            if (!cards.Contains(general_data.CARD_FOR_CODE.ToString()))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return View(rA42_VECHILE_PASS_DTL);
                            }
                        }
                        else
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);
                        }
                        
                    }

                }

                if(ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " +s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME_E + " - "+ s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
            else
            {
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == general_data.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (general_data.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plates chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get vechiles colors in arabic
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles types (صالون - دفع رباعي ....) in arabic
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
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
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get responsible person (المنسق الأمني)-- this option will be for applicant to change autho person if he wants in arabic
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (general_data.STATION_CODE == 26)
                        {
                            var cards = new List<string> {
                                                            "1","6"};
                            if (!cards.Contains(general_data.CARD_FOR_CODE.ToString()))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return View(rA42_VECHILE_PASS_DTL);
                            }
                        }
                        else
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(rA42_VECHILE_PASS_DTL);
                        }
                    }

                }

                if (ViewBag.DEVELOPER == true)
                {
                    //get all responsible
                    var WORKFLOW_RESPO_1 = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID > 1 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME+" - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO_1, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);
                }
            }
          

            //editing start from here
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

                                fileName = "Profile_3_" + DateTime.Now.ToString("yymmssfff")+extension;
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE,fileName);

                                if(check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_VECHILE_PASS_DTL);
                                }
                                rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = fileName;
                                


                            }
                            else
                            {
                                //show error if image format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_VECHILE_PASS_DTL);
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
                            rA42_MEMBERS_DTL.ACCESS_ROW_CODE = general_data.VECHILE_PASS_CODE;
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
                                            return View(rA42_VECHILE_PASS_DTL);
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
                                        return View(rA42_VECHILE_PASS_DTL);
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
                        return View(rA42_VECHILE_PASS_DTL);
                    }
                }
                //add zones and files before editing any thing 
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                                        ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE,
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
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return RedirectToAction("Edit", new { id = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE });
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
                    rA42_COMMENT.PASS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
                    rA42_COMMENT.CRD_BY = currentUser;
                    rA42_COMMENT.CRD_DT = DateTime.Now;
                    rA42_COMMENT.COMMENT = COMMENT;
                    db.RA42_COMMENTS_MST.Add(rA42_COMMENT);


                }
               

                if (rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE != null)
                {
                    general_data.PERSONAL_IMAGE = rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE;
                }
                else
                {
                    general_data.PERSONAL_IMAGE = general_data.PERSONAL_IMAGE;

                }
                var x = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                var currentUserInfo = (new UserInfo()).getUserInfo();

                //this section is for permits cell
                if (form["approvebtn"] != null && ViewBag.DEVELOPER == true)
                {

                    general_data.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                    general_data.COMPANY_CODE = rA42_VECHILE_PASS_DTL.COMPANY_CODE;
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.SERVICE_NUMBER;
                    general_data.RESPONSIBLE = general_data.RESPONSIBLE;
                    general_data.CIVIL_NUMBER = rA42_VECHILE_PASS_DTL.CIVIL_NUMBER;
                    general_data.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                    general_data.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                    general_data.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                    general_data.PHONE_NUMBER = rA42_VECHILE_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_VECHILE_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_VECHILE_PASS_DTL.IDENTITY_CODE;
                    general_data.GENDER_ID = rA42_VECHILE_PASS_DTL.GENDER_ID;
                    if (rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE != null)
                    {
                        general_data.RELATIVE_TYPE_CODE = rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE;
                        general_data.BUILDING_NUMBER = rA42_VECHILE_PASS_DTL.BUILDING_NUMBER;
                    }
                    general_data.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                    general_data.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                    general_data.VECHILE_CODE = rA42_VECHILE_PASS_DTL.VECHILE_CODE;
                    general_data.VECHILE_NAME_CODE = rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE;
                    general_data.VECHILE_COLOR_CODE = rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE;
                    general_data.PLATE_CODE = rA42_VECHILE_PASS_DTL.PLATE_CODE;
                    general_data.PLATE_NUMBER = rA42_VECHILE_PASS_DTL.PLATE_NUMBER;
                    general_data.PLATE_CHAR_CODE = rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE;
                    general_data.PASS_TYPE_CODE = rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_VECHILE_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_VECHILE_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_VECHILE_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_VECHILE_PASS_DTL.REMARKS;
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
                    general_data.STATUS = general_data.STATUS;
                    general_data.ISOPENED = general_data.ISOPENED;
                    general_data.ISPRINTED = false;
                    db.Entry(general_data).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Newpasses");
                }
                //this section is for applicant
                if (form["approvebtn"] != null && (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11)))
                {


                    general_data.COMPANY_CODE = rA42_VECHILE_PASS_DTL.COMPANY_CODE;
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    if(rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE == null)
                    {
                        general_data.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;
                    }
                    else
                    {
                        general_data.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                    }
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.RESPONSIBLE = general_data.RESPONSIBLE;
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.SERVICE_NUMBER;
                    general_data.CIVIL_NUMBER = rA42_VECHILE_PASS_DTL.CIVIL_NUMBER;
                    general_data.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                    general_data.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                    general_data.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                    general_data.PHONE_NUMBER = rA42_VECHILE_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_VECHILE_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_VECHILE_PASS_DTL.IDENTITY_CODE;
                    general_data.GENDER_ID = rA42_VECHILE_PASS_DTL.GENDER_ID;
                    if (rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE != null)
                    {
                        general_data.RELATIVE_TYPE_CODE = rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE;
                        general_data.BUILDING_NUMBER = rA42_VECHILE_PASS_DTL.BUILDING_NUMBER;
                    }

                    general_data.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                    general_data.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                    general_data.VECHILE_CODE = rA42_VECHILE_PASS_DTL.VECHILE_CODE;
                    general_data.VECHILE_NAME_CODE = rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE;
                    general_data.VECHILE_COLOR_CODE = rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE;
                    general_data.PLATE_CODE = rA42_VECHILE_PASS_DTL.PLATE_CODE;
                    general_data.PLATE_NUMBER = rA42_VECHILE_PASS_DTL.PLATE_NUMBER;
                    general_data.PLATE_CHAR_CODE = rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE;
                    general_data.PASS_TYPE_CODE = rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_VECHILE_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_VECHILE_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_VECHILE_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_VECHILE_PASS_DTL.REMARKS;
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
                    general_data.STATUS = general_data.STATUS;
                    db.Entry(general_data).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Index","Mypasses");
                }
                //this section is for autho person (المنسق الأمني)
                if (form["approvebtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2)
                {

                    //he should redirect the request to the permits cell to proccess the request
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return RedirectToAction("Edit",new { id=rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE});

                    }
                    general_data.COMPANY_CODE = rA42_VECHILE_PASS_DTL.COMPANY_CODE;
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.SERVICE_NUMBER;
                    general_data.RESPONSIBLE = general_data.RESPONSIBLE;
                    general_data.CIVIL_NUMBER = rA42_VECHILE_PASS_DTL.CIVIL_NUMBER;
                    general_data.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                    general_data.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                    general_data.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                    general_data.PHONE_NUMBER = rA42_VECHILE_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_VECHILE_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_VECHILE_PASS_DTL.IDENTITY_CODE;
                    general_data.GENDER_ID = rA42_VECHILE_PASS_DTL.GENDER_ID;
                    if (rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE != null)
                    {
                        general_data.RELATIVE_TYPE_CODE = rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE;
                        general_data.BUILDING_NUMBER = rA42_VECHILE_PASS_DTL.BUILDING_NUMBER;
                    }
                    general_data.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                    general_data.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                    general_data.VECHILE_CODE = rA42_VECHILE_PASS_DTL.VECHILE_CODE;
                    general_data.VECHILE_NAME_CODE = rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE;
                    general_data.VECHILE_COLOR_CODE = rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE;
                    general_data.PLATE_CODE = rA42_VECHILE_PASS_DTL.PLATE_CODE;
                    general_data.PLATE_NUMBER = rA42_VECHILE_PASS_DTL.PLATE_NUMBER;
                    general_data.PLATE_CHAR_CODE = rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE;
                    general_data.PASS_TYPE_CODE = rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_VECHILE_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_VECHILE_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_VECHILE_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_VECHILE_PASS_DTL.REMARKS;
                    general_data.APPROVAL_SN = currentUser;
                    general_data.APPROVAL_NAME = v.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME;
                    general_data.APPROVAL_RANK = v.RA42_WORKFLOW_RESPONSIBLE_MST.RANK;
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
                    general_data.STATUS = general_data.STATUS;
                    general_data.ISPRINTED = general_data.ISPRINTED;
                    db.Entry(general_data).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Authopasses");
                }

                //this section is for permits cell
                if (form["approvebtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3)
                {
                    if (general_data.STATION_CODE == 26 && (general_data.CARD_FOR_CODE == 1 || general_data.CARD_FOR_CODE == 6))
                    {
                        //security officer should redirect the permit to the permits cell for printing the permit after he complete and approve the request
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE });

                        }
                        general_data.COMPANY_CODE = rA42_VECHILE_PASS_DTL.COMPANY_CODE;
                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.RESPONSIBLE = general_data.RESPONSIBLE;
                        general_data.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.SERVICE_NUMBER;
                        general_data.CIVIL_NUMBER = rA42_VECHILE_PASS_DTL.CIVIL_NUMBER;
                        general_data.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                        general_data.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                        general_data.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                        general_data.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                        general_data.PHONE_NUMBER = rA42_VECHILE_PASS_DTL.PHONE_NUMBER;
                        general_data.GSM = rA42_VECHILE_PASS_DTL.GSM;
                        general_data.IDENTITY_CODE = rA42_VECHILE_PASS_DTL.IDENTITY_CODE;
                        general_data.GENDER_ID = rA42_VECHILE_PASS_DTL.GENDER_ID;
                        if (rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE != null)
                        {
                            general_data.RELATIVE_TYPE_CODE = rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE;
                            general_data.BUILDING_NUMBER = rA42_VECHILE_PASS_DTL.BUILDING_NUMBER;
                        }
                        general_data.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                        general_data.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                        general_data.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                        general_data.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                        general_data.VECHILE_CODE = rA42_VECHILE_PASS_DTL.VECHILE_CODE;
                        general_data.VECHILE_NAME_CODE = rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE;
                        general_data.VECHILE_COLOR_CODE = rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE;
                        general_data.PLATE_CODE = rA42_VECHILE_PASS_DTL.PLATE_CODE;
                        general_data.PLATE_NUMBER = rA42_VECHILE_PASS_DTL.PLATE_NUMBER;
                        general_data.PLATE_CHAR_CODE = rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE;
                        general_data.PASS_TYPE_CODE = rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE;
                        general_data.DATE_FROM = rA42_VECHILE_PASS_DTL.DATE_FROM;
                        general_data.DATE_TO = rA42_VECHILE_PASS_DTL.DATE_TO;
                        general_data.PURPOSE_OF_PASS = rA42_VECHILE_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_VECHILE_PASS_DTL.REMARKS;
                        general_data.APPROVAL_SN = general_data.APPROVAL_SN;
                        general_data.APPROVAL_NAME = general_data.APPROVAL_NAME;
                        general_data.APPROVAL_RANK = general_data.APPROVAL_RANK;
                        general_data.APPROVAL_APPROVISION_DATE = general_data.APPROVAL_APPROVISION_DATE;
                        general_data.PERMIT_SN = currentUser;
                        general_data.PERMIT_NAME = v.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME; 
                        general_data.PERMIT_RANK = v.RA42_WORKFLOW_RESPONSIBLE_MST.RANK; 
                        general_data.PERMIT_APPROVISION_DATE = DateTime.Now;
                        general_data.AUTHO_SN = general_data.AUTHO_SN;
                        general_data.AUTHO_NAME = general_data.AUTHO_NAME;
                        general_data.AUTHO_RANK = general_data.AUTHO_RANK;
                        general_data.AUTHO_APPROVISION_DATE = general_data.AUTHO_APPROVISION_DATE;
                        string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                        general_data.BARCODE = barcode;
                        general_data.REJECTED = false;
                        general_data.STATUS = true;
                        general_data.ISOPENED = general_data.ISOPENED;
                        general_data.ISPRINTED = general_data.ISPRINTED;
                        db.Entry(general_data).State = EntityState.Modified;
                        db.SaveChanges();
                        AddToast(new Toast("",
                        GetResourcesValue("success_update_message"),
                        "green"));
                        return RedirectToAction("Newpasses");
                    }
                    else
                    {
                        string name = "";
                        string rank = "";
                        //check if the status of the request is true, that means no need to redirect the request to the security officer
                        if (general_data.STATUS == true)
                        {
                            general_data.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;
                        }
                        else
                        {
                            //if the status is not true, the permits cell should redirect the reqquest to security officer to complete the request
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Edit", new { id = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE });

                            }
                            general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                            name = v.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME;
                            rank = v.RA42_WORKFLOW_RESPONSIBLE_MST.RANK;

                        }
                        general_data.COMPANY_CODE = rA42_VECHILE_PASS_DTL.COMPANY_CODE;
                        general_data.CRD_BY = general_data.CRD_BY;
                        general_data.CRD_DT = general_data.CRD_DT;
                        general_data.UPD_BY = currentUser;
                        general_data.UPD_DT = DateTime.Now;
                        general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                        general_data.STATION_CODE = general_data.STATION_CODE;
                        general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                        general_data.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.SERVICE_NUMBER;
                        general_data.RESPONSIBLE = general_data.RESPONSIBLE;
                        general_data.CIVIL_NUMBER = rA42_VECHILE_PASS_DTL.CIVIL_NUMBER;
                        general_data.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                        general_data.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                        general_data.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                        general_data.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                        general_data.PHONE_NUMBER = rA42_VECHILE_PASS_DTL.PHONE_NUMBER;
                        general_data.GSM = rA42_VECHILE_PASS_DTL.GSM;
                        general_data.IDENTITY_CODE = rA42_VECHILE_PASS_DTL.IDENTITY_CODE;
                        general_data.GENDER_ID = rA42_VECHILE_PASS_DTL.GENDER_ID;
                        if (rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE != null)
                        {
                            general_data.RELATIVE_TYPE_CODE = rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE;
                            general_data.BUILDING_NUMBER = rA42_VECHILE_PASS_DTL.BUILDING_NUMBER;
                        }
                        general_data.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                        general_data.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                        general_data.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                        general_data.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                        general_data.VECHILE_CODE = rA42_VECHILE_PASS_DTL.VECHILE_CODE;
                        general_data.VECHILE_NAME_CODE = rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE;
                        general_data.VECHILE_COLOR_CODE = rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE;
                        general_data.PLATE_CODE = rA42_VECHILE_PASS_DTL.PLATE_CODE;
                        general_data.PLATE_NUMBER = rA42_VECHILE_PASS_DTL.PLATE_NUMBER;
                        general_data.PLATE_CHAR_CODE = rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE;
                        general_data.PASS_TYPE_CODE = rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE;
                        general_data.DATE_FROM = rA42_VECHILE_PASS_DTL.DATE_FROM;
                        general_data.DATE_TO = rA42_VECHILE_PASS_DTL.DATE_TO;
                        general_data.PURPOSE_OF_PASS = rA42_VECHILE_PASS_DTL.PURPOSE_OF_PASS;
                        general_data.REMARKS = rA42_VECHILE_PASS_DTL.REMARKS;
                        general_data.APPROVAL_SN = general_data.APPROVAL_SN;
                        general_data.APPROVAL_NAME = general_data.APPROVAL_NAME;
                        general_data.APPROVAL_RANK = general_data.APPROVAL_RANK;
                        general_data.APPROVAL_APPROVISION_DATE = general_data.APPROVAL_APPROVISION_DATE;
                        general_data.AUTHO_SN = general_data.AUTHO_SN;
                        general_data.AUTHO_NAME = general_data.AUTHO_NAME;
                        general_data.AUTHO_RANK = general_data.AUTHO_RANK;
                        general_data.AUTHO_APPROVISION_DATE = general_data.AUTHO_APPROVISION_DATE;
                        general_data.PERMIT_SN = currentUser;
                        general_data.PERMIT_NAME = name;
                        general_data.PERMIT_RANK = rank;
                        general_data.PERMIT_APPROVISION_DATE = DateTime.Now;
                        general_data.BARCODE = general_data.BARCODE;
                        general_data.REJECTED = false;
                        general_data.STATUS = general_data.STATUS;
                        general_data.ISOPENED = general_data.ISOPENED;
                        general_data.ISPRINTED = false;
                        db.Entry(general_data).State = EntityState.Modified;
                        db.SaveChanges();
                        AddToast(new Toast("",
                        GetResourcesValue("success_update_message"),
                        "green"));
                        return RedirectToAction("Newpasses");
                    }
                }
                //this section is for security officer
                if (form["approvebtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4)
                {

                   //security officer should redirect the permit to the permits cell for printing the permit after he complete and approve the request
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS !=true).FirstOrDefault();
                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return RedirectToAction("Edit", new { id = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE });

                    }
                    general_data.COMPANY_CODE = rA42_VECHILE_PASS_DTL.COMPANY_CODE;
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.RESPONSIBLE = general_data.RESPONSIBLE;
                    general_data.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.SERVICE_NUMBER;
                    general_data.CIVIL_NUMBER = rA42_VECHILE_PASS_DTL.CIVIL_NUMBER;
                    general_data.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                    general_data.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                    general_data.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                    general_data.PHONE_NUMBER = rA42_VECHILE_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_VECHILE_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_VECHILE_PASS_DTL.IDENTITY_CODE;
                    general_data.GENDER_ID = rA42_VECHILE_PASS_DTL.GENDER_ID;
                    if (rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE != null)
                    {
                        general_data.RELATIVE_TYPE_CODE = rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE;
                        general_data.BUILDING_NUMBER = rA42_VECHILE_PASS_DTL.BUILDING_NUMBER;
                    }
                    general_data.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                    general_data.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                    general_data.VECHILE_CODE = rA42_VECHILE_PASS_DTL.VECHILE_CODE;
                    general_data.VECHILE_NAME_CODE = rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE;
                    general_data.VECHILE_COLOR_CODE = rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE;
                    general_data.PLATE_CODE = rA42_VECHILE_PASS_DTL.PLATE_CODE;
                    general_data.PLATE_NUMBER = rA42_VECHILE_PASS_DTL.PLATE_NUMBER;
                    general_data.PLATE_CHAR_CODE = rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE;
                    general_data.PASS_TYPE_CODE = rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_VECHILE_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_VECHILE_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_VECHILE_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_VECHILE_PASS_DTL.REMARKS;
                    general_data.APPROVAL_SN = general_data.APPROVAL_SN;
                    general_data.APPROVAL_NAME = general_data.APPROVAL_NAME;
                    general_data.APPROVAL_RANK = general_data.APPROVAL_RANK;
                    general_data.APPROVAL_APPROVISION_DATE = general_data.APPROVAL_APPROVISION_DATE;
                    general_data.AUTHO_SN = currentUser;
                    general_data.AUTHO_NAME = v.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME; 
                    general_data.AUTHO_RANK = v.RA42_WORKFLOW_RESPONSIBLE_MST.RANK; 
                    general_data.AUTHO_APPROVISION_DATE = DateTime.Now;
                    general_data.PERMIT_SN = general_data.PERMIT_SN;
                    general_data.PERMIT_NAME = general_data.PERMIT_NAME;
                    general_data.PERMIT_RANK = general_data.PERMIT_RANK;
                    general_data.PERMIT_APPROVISION_DATE = general_data.PERMIT_APPROVISION_DATE;
                    general_data.BARCODE = rA42_VECHILE_PASS_DTL.BARCODE;
                    general_data.REJECTED = false;
                    general_data.STATUS = true;
                    general_data.ISOPENED = general_data.ISOPENED;
                    general_data.ISPRINTED = general_data.ISPRINTED;
                    db.Entry(general_data).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Newpasses");
                }
                //this section is for autho person to reject any request
                if (form["rejectbtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2)
                {


                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    //if (v == null)
                    //{
                    //    if (general_data.STATION_CODE != 26)
                    //    {
                    //        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    //        return RedirectToAction("Edit", new { id = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE });
                    //    }
                    //}
                    general_data.COMPANY_CODE = rA42_VECHILE_PASS_DTL.COMPANY_CODE;
                    general_data.RESPONSIBLE = general_data.RESPONSIBLE;
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    if (v == null)
                    {
                        general_data.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;

                    }
                    else
                    {
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    }
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.SERVICE_NUMBER;
                    general_data.RESPONSIBLE = general_data.RESPONSIBLE;
                    general_data.CIVIL_NUMBER = rA42_VECHILE_PASS_DTL.CIVIL_NUMBER;
                    general_data.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                    general_data.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                    general_data.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                    general_data.PHONE_NUMBER = rA42_VECHILE_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_VECHILE_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_VECHILE_PASS_DTL.IDENTITY_CODE;
                    general_data.GENDER_ID = rA42_VECHILE_PASS_DTL.GENDER_ID;
                    if (rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE != null)
                    {
                        general_data.RELATIVE_TYPE_CODE = rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE;
                        general_data.BUILDING_NUMBER = rA42_VECHILE_PASS_DTL.BUILDING_NUMBER;
                    }
                    general_data.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                    general_data.RESPONSIBLE = general_data.RESPONSIBLE;
                    general_data.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                    general_data.VECHILE_CODE = rA42_VECHILE_PASS_DTL.VECHILE_CODE;
                    general_data.VECHILE_NAME_CODE = rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE;
                    general_data.VECHILE_COLOR_CODE = rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE;
                    general_data.PLATE_CODE = rA42_VECHILE_PASS_DTL.PLATE_CODE;
                    general_data.PLATE_NUMBER = rA42_VECHILE_PASS_DTL.PLATE_NUMBER;
                    general_data.PLATE_CHAR_CODE = rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE;
                    general_data.PASS_TYPE_CODE = rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_VECHILE_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_VECHILE_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_VECHILE_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_VECHILE_PASS_DTL.REMARKS;
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
                    general_data.ISPRINTED = general_data.ISPRINTED;
                    db.Entry(general_data).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Authopasses");
                }
                //this section is for permits cell to reject any permit and redirect it to the autho person
                if (form["rejectbtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3)
                {

                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == general_data.APPROVAL_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    //if (v == null)
                    //{
                    //    if (general_data.STATION_CODE != 26)
                    //    {
                    //        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    //        return RedirectToAction("Edit", new { id = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE });
                    //    }
                       

                    //}
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.COMPANY_CODE = rA42_VECHILE_PASS_DTL.COMPANY_CODE;
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    if (v == null)
                    {
                        general_data.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;

                    }
                    else
                    {
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    }
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.SERVICE_NUMBER;
                    general_data.RESPONSIBLE = general_data.RESPONSIBLE;
                    general_data.CIVIL_NUMBER = rA42_VECHILE_PASS_DTL.CIVIL_NUMBER;
                    general_data.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                    general_data.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                    general_data.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                    general_data.PHONE_NUMBER = rA42_VECHILE_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_VECHILE_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_VECHILE_PASS_DTL.IDENTITY_CODE;
                    general_data.GENDER_ID = rA42_VECHILE_PASS_DTL.GENDER_ID;
                    if (rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE != null)
                    {
                        general_data.RELATIVE_TYPE_CODE = rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE;
                        general_data.BUILDING_NUMBER = rA42_VECHILE_PASS_DTL.BUILDING_NUMBER;
                    }
                    general_data.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                    general_data.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                    general_data.VECHILE_CODE = rA42_VECHILE_PASS_DTL.VECHILE_CODE;
                    general_data.VECHILE_NAME_CODE = rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE;
                    general_data.VECHILE_COLOR_CODE = rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE;
                    general_data.PLATE_CODE = rA42_VECHILE_PASS_DTL.PLATE_CODE;
                    general_data.PLATE_NUMBER = rA42_VECHILE_PASS_DTL.PLATE_NUMBER;
                    general_data.PLATE_CHAR_CODE = rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE;
                    general_data.PASS_TYPE_CODE = rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_VECHILE_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_VECHILE_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_VECHILE_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_VECHILE_PASS_DTL.REMARKS;
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
                    general_data.ISOPENED = general_data.ISOPENED;
                    general_data.ISPRINTED = general_data.ISPRINTED;
                    db.Entry(general_data).State = EntityState.Modified;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("success_update_message"),
                    "green"));
                    return RedirectToAction("Newpasses");
                }
                //this section is for security officer to reject any request and redirect it to permits cell
                if (form["rejectbtn"] != null && x.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4)
                {


                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == general_data.PERMIT_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                    //if (v == null)
                    //{
                    //    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                    //    return RedirectToAction("Edit", new { id = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE });

                    //}
                    general_data.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                    general_data.COMPANY_CODE = rA42_VECHILE_PASS_DTL.COMPANY_CODE;
                    general_data.CRD_BY = general_data.CRD_BY;
                    general_data.CRD_DT = general_data.CRD_DT;
                    general_data.UPD_BY = currentUser;
                    general_data.UPD_DT = DateTime.Now;
                    if (v == null)
                    {
                        general_data.WORKFLOW_RESPO_CODE = general_data.WORKFLOW_RESPO_CODE;

                    }
                    else
                    {
                        general_data.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                    }
                    general_data.STATION_CODE = general_data.STATION_CODE;
                    general_data.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    general_data.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.SERVICE_NUMBER;
                    general_data.RESPONSIBLE = general_data.RESPONSIBLE;
                    general_data.CIVIL_NUMBER = rA42_VECHILE_PASS_DTL.CIVIL_NUMBER;
                    general_data.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                    general_data.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                    general_data.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                    general_data.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                    general_data.PHONE_NUMBER = rA42_VECHILE_PASS_DTL.PHONE_NUMBER;
                    general_data.GSM = rA42_VECHILE_PASS_DTL.GSM;
                    general_data.IDENTITY_CODE = rA42_VECHILE_PASS_DTL.IDENTITY_CODE;
                    general_data.GENDER_ID = rA42_VECHILE_PASS_DTL.GENDER_ID;
                    if (rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE != null)
                    {
                        general_data.RELATIVE_TYPE_CODE = rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE;
                        general_data.BUILDING_NUMBER = rA42_VECHILE_PASS_DTL.BUILDING_NUMBER;
                    }
                    general_data.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                    general_data.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                    general_data.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                    general_data.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                    general_data.VECHILE_CODE = rA42_VECHILE_PASS_DTL.VECHILE_CODE;
                    general_data.VECHILE_NAME_CODE = rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE;
                    general_data.VECHILE_COLOR_CODE = rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE;
                    general_data.PLATE_CODE = rA42_VECHILE_PASS_DTL.PLATE_CODE;
                    general_data.PLATE_NUMBER = rA42_VECHILE_PASS_DTL.PLATE_NUMBER;
                    general_data.PLATE_CHAR_CODE = rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE;
                    general_data.PASS_TYPE_CODE = rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE;
                    general_data.DATE_FROM = rA42_VECHILE_PASS_DTL.DATE_FROM;
                    general_data.DATE_TO = rA42_VECHILE_PASS_DTL.DATE_TO;
                    general_data.PURPOSE_OF_PASS = rA42_VECHILE_PASS_DTL.PURPOSE_OF_PASS;
                    general_data.REMARKS = rA42_VECHILE_PASS_DTL.REMARKS;
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
                    general_data.ISOPENED = general_data.ISOPENED;
                    general_data.ISPRINTED = general_data.ISPRINTED;
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
            return View(rA42_VECHILE_PASS_DTL);
        }


        //renew permit action
        public ActionResult Renew(int? id)
        {
            ViewBag.activetab = "Renew";

            if (id == null)
            {
                return NotFound();
            }
            //check if request in the db table 
            RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL = db.RA42_VECHILE_PASS_DTL.Find(id);
            if (rA42_VECHILE_PASS_DTL == null)
            {
                return NotFound();
            }
            //check authority to proccess this request 

            if (rA42_VECHILE_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_VECHILE_PASS_DTL.RESPONSIBLE == currentUser)
            {
                if (rA42_VECHILE_PASS_DTL.DATE_TO != null)
                {

                    string date = rA42_VECHILE_PASS_DTL.CheckDate(rA42_VECHILE_PASS_DTL.DATE_TO.Value);
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
                if (ViewBag.RESPO_STATE != rA42_VECHILE_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                {
                    if (rA42_VECHILE_PASS_DTL.DATE_TO != null)
                    {

                        string date = rA42_VECHILE_PASS_DTL.CheckDate(rA42_VECHILE_PASS_DTL.DATE_TO.Value);
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
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == rA42_VECHILE_PASS_DTL.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get realtives types in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (rA42_VECHILE_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plates types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plates chars in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in english 
               
               ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
              //get colors of vechiles in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles types in english (صالون - دفع رباعي ....)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_VECHILE_PASS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_VECHILE_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_VECHILE_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get responsible person (المنسق الأمني)-- this option will be for applicant to change autho person if he wants
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_VECHILE_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //throw error if there is no responsible. 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (rA42_VECHILE_PASS_DTL.STATION_CODE == 26)
                        {
                            var cards = new List<string> {
                                                            "1","6"};
                            if (!cards.Contains(rA42_VECHILE_PASS_DTL.CARD_FOR_CODE.ToString()))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            }
                        }
                        else
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        }

                    }

                }

                
            }
            else
            {
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == rA42_VECHILE_PASS_DTL.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (rA42_VECHILE_PASS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plates chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in arabic 
               
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);

                //get vechiles colors in arabic
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles types (صالون - دفع رباعي ....) in arabic
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_VECHILE_PASS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_VECHILE_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_VECHILE_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get responsible person (المنسق الأمني)-- this option will be for applicant to change autho person if he wants in arabic
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_VECHILE_PASS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //throw error if there is no responsible. 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (rA42_VECHILE_PASS_DTL.STATION_CODE == 26)
                        {
                            var cards = new List<string> {
                                                            "1","6"};
                            if (!cards.Contains(rA42_VECHILE_PASS_DTL.CARD_FOR_CODE.ToString()))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            }
                        }
                        else
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        }
                    }

                }

                
            }

            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (rA42_VECHILE_PASS_DTL.STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get relatives of this permit 
            ViewBag.GetRelativs = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.DLT_STS != true).ToList();
            //get selected zones and gates
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get selected documenst 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get documenst for this kind of permit to check missing documenst and make compare
            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_VECHILE_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_VECHILE_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_VECHILE_PASS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_VECHILE_PASS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            //get violations of the permit 
            var vIOLATIONS = db.RA42_VECHILE_VIOLATION_DTL.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.VIOLATIONS = vIOLATIONS;
            //get personal image 
            ViewBag.PERSONAL_IMAGE = rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE;
            rA42_VECHILE_PASS_DTL.DATE_FROM = null;
            rA42_VECHILE_PASS_DTL.DATE_TO = null;
            return View(rA42_VECHILE_PASS_DTL);


        }

        // POST data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Renew(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL, int[] RELATIVE_TYPES, HttpPostedFileBase[] RELATIVE_IMAGE, int[] IDENTITY_TYPES, int[] GENDER_TYPES, string[] FULL_NAME, string[] CIVIL_NUM, string[] PASSPORT_NUMBER, string[] PHONE_NUMBER_M
            , string[] REMARKS_LIST,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[]
            files, HttpPostedFileBase PERSONAL_IMAGE,int VECHILE_ID)
        {
            //check if the request id is in the db table 
            var general_data = db.RA42_VECHILE_PASS_DTL.Where(a => a.VECHILE_PASS_CODE == VECHILE_ID).FirstOrDefault();
            ViewBag.activetab = "Renew";
            //get selected zones and gates
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == VECHILE_ID && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
            //get selected documents
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == VECHILE_ID && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
            //get documents types for this kind of permit to check missing files 
            ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE  && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            //get relatives of this permit 
            ViewBag.GetRelativs = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.ACCESS_ROW_CODE == general_data.VECHILE_PASS_CODE && a.DLT_STS != true).ToList();
            //get personal image 
            ViewBag.PERSONAL_IMAGE = general_data.PERSONAL_IMAGE;
            //get violations of the permit 
            var vIOLATIONS = db.RA42_VECHILE_VIOLATION_DTL.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            ViewBag.VIOLATIONS = vIOLATIONS;
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (rA42_VECHILE_PASS_DTL.STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }

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
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == general_data.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get realtives types in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (general_data.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plates types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plates chars in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in english 
               ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get colors of vechiles in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles types in english (صالون - دفع رباعي ....)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
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
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
               //get responsible person (المنسق الأمني)-- this option will be for applicant to change autho person if he wants in english
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (general_data.STATION_CODE == 26)
                        {
                            var cards = new List<string> {
                                                            "1","6"};
                            if (!cards.Contains(general_data.CARD_FOR_CODE.ToString()))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return View(general_data);
                            }
                        }
                        else
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(general_data);
                        }
                    }

                }

              
            }
            else
            {
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == general_data.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (general_data.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plates chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in arabic 
                 ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get vechiles colors in arabic
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles types (صالون - دفع رباعي ....) in arabic
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
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
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get responsible person (المنسق الأمني)-- this option will be for applicant to change autho person if he wants in arabic
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == general_data.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        if (general_data.STATION_CODE == 26)
                        {
                            var cards = new List<string> {
                                                            "1","6"};
                            if (!cards.Contains(general_data.CARD_FOR_CODE.ToString()))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return View(general_data);
                            }
                        }
                        else
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(general_data);
                        }
                       
                    }

                }

              
            }


            //editing start from here
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

                                fileName = "Profile_3_" + DateTime.Now.ToString("yymmssfff") + extension;
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
                                rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = fileName;



                            }
                            else
                            {
                                //show error if image format not supported 
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
                
                
                

                if (rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE != null)
                {
                    rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE;
                }
                else
                {
                    rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = general_data.PERSONAL_IMAGE;

                }
               

                rA42_VECHILE_PASS_DTL.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                rA42_VECHILE_PASS_DTL.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.SERVICE_NUMBER;
                rA42_VECHILE_PASS_DTL.RESPONSIBLE = currentUser;
                rA42_VECHILE_PASS_DTL.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                rA42_VECHILE_PASS_DTL.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                rA42_VECHILE_PASS_DTL.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                rA42_VECHILE_PASS_DTL.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                rA42_VECHILE_PASS_DTL.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                rA42_VECHILE_PASS_DTL.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                rA42_VECHILE_PASS_DTL.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                rA42_VECHILE_PASS_DTL.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                rA42_VECHILE_PASS_DTL.STATION_CODE = general_data.STATION_CODE;

                //get current user military information from api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;

                //this section is for applicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                {
                    if (STATION_CODE == 26)
                    {
                        var cards = new List<string> {
                                                            "1","6"};
                        if (cards.Contains(general_data.CARD_FOR_CODE.ToString()))
                        {
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return View(general_data);

                            }
                            else
                            {
                                rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                            }
                            rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                            rA42_VECHILE_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                            rA42_VECHILE_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                            rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                            rA42_VECHILE_PASS_DTL.REJECTED = false;
                            rA42_VECHILE_PASS_DTL.STATUS = false;
                            rA42_VECHILE_PASS_DTL.ISOPENED = true;
                        }
                        else
                        {
                            rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                            rA42_VECHILE_PASS_DTL.REJECTED = false;
                            rA42_VECHILE_PASS_DTL.STATUS = false;
                            rA42_VECHILE_PASS_DTL.ISOPENED = false;
                        }
                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                        rA42_VECHILE_PASS_DTL.REJECTED = false;
                        rA42_VECHILE_PASS_DTL.STATUS = false;
                        rA42_VECHILE_PASS_DTL.ISOPENED = false;
                    }
                }

                //this section is for autho pperson (المنسق الأمني)
                if (WORKFLOWID == 2)
                {
                    //he should redirect the permit for permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(general_data);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for permits cell 
                if (WORKFLOWID == 3)
                {
                    if (general_data.STATION_CODE == 26 && (general_data.CARD_FOR_CODE == 1 || general_data.CARD_FOR_CODE == 6))
                    {
                        //security officer should redirect the permit to the permits cell for print the permit 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(general_data);

                        }
                        else
                        {
                            rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }
                        rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                        rA42_VECHILE_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;


                        rA42_VECHILE_PASS_DTL.REJECTED = false;
                        rA42_VECHILE_PASS_DTL.STATUS = true;
                        rA42_VECHILE_PASS_DTL.ISOPENED = true;
                        string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                        rA42_VECHILE_PASS_DTL.BARCODE = barcode;
                    }
                    else
                    {
                        //permits cell should redirect the request to the security officer 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return View(general_data);

                        }
                        else
                        {
                            rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        }
                        rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                        rA42_VECHILE_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                        rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;


                        rA42_VECHILE_PASS_DTL.REJECTED = false;
                        rA42_VECHILE_PASS_DTL.STATUS = false;
                        rA42_VECHILE_PASS_DTL.ISOPENED = true;
                    }
                }
                //this section is for security officer
                if (WORKFLOWID == 4)
                {
                    //security officer should redirect the permit to the permits cell for print the permit 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(general_data);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;


                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = true;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                    rA42_VECHILE_PASS_DTL.BARCODE = rA42_VECHILE_PASS_DTL.BARCODE;

                }

                rA42_VECHILE_PASS_DTL.CRD_BY = currentUser;
                    rA42_VECHILE_PASS_DTL.CRD_DT = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.UPD_BY = currentUser;
                    rA42_VECHILE_PASS_DTL.UPD_DT = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                if (FORCE_ID == 2 && (rA42_VECHILE_PASS_DTL.CARD_FOR_CODE == 1 || rA42_VECHILE_PASS_DTL.CARD_FOR_CODE == 6))
                {
                    rA42_VECHILE_PASS_DTL.DATE_TO = rA42_VECHILE_PASS_DTL.DATE_TO.Value.AddDays(30);
                }
                db.RA42_VECHILE_PASS_DTL.Add(rA42_VECHILE_PASS_DTL);
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
                            rA42_MEMBERS_DTL.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                                            return View(rA42_VECHILE_PASS_DTL);
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
                                        return View(rA42_VECHILE_PASS_DTL);
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
                        return View(rA42_VECHILE_PASS_DTL);
                    }
                }

                var relatevies = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_ROW_CODE == VECHILE_ID && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
                //add previues relatives to new relatives
                foreach (var relative in relatevies)
                {
                    rA42_MEMBERS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                    rA42_MEMBERS_DTL.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                //add zones and files before editing any thing 
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
                        rA42_ZONE_MASTER_MST.ZONE_CODE = ZONE[i];
                        rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                        rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                        db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                        db.SaveChanges();
                        //continue;
                    }
                    


                }
                var zones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == VECHILE_ID && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
                //add previues zones to new zone
                foreach (var zone in zones)
                {
                    rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                                        ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE,
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
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return RedirectToAction("Renew", new { id = VECHILE_ID });
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
                var selected_files = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == VECHILE_ID && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true).ToList();
                foreach(var file in selected_files)
                {
                    //add new file
                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                    {
                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                        ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE,
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


        // delete the permit or request
        public ActionResult Delete(int? id)
        {
          
                ViewBag.activetab = "delete";

                if (id == null)
                {
                return NotFound();
                }
                //check if the id of permit is in the db table 
                RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL = db.RA42_VECHILE_PASS_DTL.Find(id);
                if (rA42_VECHILE_PASS_DTL == null)
                {
                

                    return NotFound();


                }
            
                //check if current user has authority to delete the permit or request
                if (ViewBag.RESPO_STATE <= 1)
                {
                if (rA42_VECHILE_PASS_DTL.SERVICE_NUMBER != currentUser && rA42_VECHILE_PASS_DTL.RESPONSIBLE != currentUser)
                {
                    if (rA42_VECHILE_PASS_DTL.STATUS == true)
                    {
                        if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        {

                            return NotFound();
                        }
                    }
                }
                if (rA42_VECHILE_PASS_DTL.ISOPENED == true)
                {
                    if (rA42_VECHILE_PASS_DTL.STATUS == true)
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
                    if (rA42_VECHILE_PASS_DTL.SERVICE_NUMBER == currentUser || rA42_VECHILE_PASS_DTL.RESPONSIBLE == currentUser)
                    {

                    }
                    else
                    {
                        //if (ViewBag.RESPO_STATE != rA42_VECHILE_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                        //{
                        //if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                        //{
                        //    return NotFound();
                        //    }
                        //}
                    }
                }

             //get selected gates and zones
             ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get selected documents
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
               

                return View(rA42_VECHILE_PASS_DTL);
            
            
            
        }

        // confirm deleting
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var general_data = db.RA42_VECHILE_PASS_DTL.Where(a => a.VECHILE_PASS_CODE == id).FirstOrDefault();

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

        //this function is for deleteing any uploaded documents for any request
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
        //this function is to delete any personal image of any request
        [HttpPost]
        public JsonResult DeleteImage(int id)
        {
            bool result = false;
            var general_data = db.RA42_VECHILE_PASS_DTL.Where(a => a.VECHILE_PASS_CODE == id).FirstOrDefault();

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
        //this function to delete any violation of any permit 
        [HttpPost]
        public JsonResult DeleteViolation(int id)
        {
            bool result = false;
            RA42_VECHILE_VIOLATION_DTL rA42_VECHILE_VIOLATION_DTL = db.RA42_VECHILE_VIOLATION_DTL.Find(id);
            if (rA42_VECHILE_VIOLATION_DTL != null)
            {
                rA42_VECHILE_VIOLATION_DTL.DLT_STS = true;
                rA42_VECHILE_VIOLATION_DTL.UPD_BY = currentUser;
                rA42_VECHILE_VIOLATION_DTL.UPD_DT = DateTime.Now;
                db.SaveChanges();
                AddToast(new Toast("",
                GetResourcesValue("success_delete_message"),
               "green"));
                result = true;
            }

            return Json(result, JsonRequestBehavior.AllowGet);


        }
        //this function is to print card, or temprory permit or save both of them
        [HttpPost]
        public JsonResult PrintById(int id,string type)
        {

            bool result = false;
            var general_data = db.RA42_VECHILE_PASS_DTL.Where(a => a.VECHILE_PASS_CODE == id).FirstOrDefault();

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



        //permit for OtherpermitForFamily outside of MOD
        public ActionResult OtherpermitForFamily()
        {

            ViewBag.activetab = "OtherpermitForFamily";
            //ViewBag.activetab = "Search";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];

            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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


            if (Language.GetCurrentLang() == "en")
            {
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E");
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E");
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E");
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E");
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E");
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E");
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 6 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }
                }
            }
            else
            {
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME");
                //get relatives typs in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types codes in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE");
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR");
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT");
                //get colrs in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR");
                //get vechiles types in arabic (صالون - دفع رباعي)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME");
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 6 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }
                }
            }



            return View();
        }
        // POST data 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OtherpermitForFamily(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL, int[] RELATIVE_TYPES, HttpPostedFileBase[] RELATIVE_IMAGE, int[] IDENTITY_TYPES, int[] GENDER_TYPES, string[] FULL_NAME, string[] CIVIL_NUM, string[] PASSPORT_NUMBER, string[] PHONE_NUMBER_M
            , string[] REMARKS_LIST,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "OtherpermitForFamily";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];


            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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


            if (Language.GetCurrentLang() == "en")
            {
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 6 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
                    }
                }
            }
            else
            {
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get vechile colors in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechile types (صالون - دفع رباعي ...) in arabic 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 6 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
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


                            //check image format 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {


                                fileName = "Profile_3_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_VECHILE_PASS_DTL);
                                }
                                rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_VECHILE_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                rA42_VECHILE_PASS_DTL.CARD_FOR_CODE = 6;
                rA42_VECHILE_PASS_DTL.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.RESPONSIBLE;
                rA42_VECHILE_PASS_DTL.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                rA42_VECHILE_PASS_DTL.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                rA42_VECHILE_PASS_DTL.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                rA42_VECHILE_PASS_DTL.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                rA42_VECHILE_PASS_DTL.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                rA42_VECHILE_PASS_DTL.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                rA42_VECHILE_PASS_DTL.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                rA42_VECHILE_PASS_DTL.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                rA42_VECHILE_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_VECHILE_PASS_DTL.RESPONSIBLE = currentUser;

                //get user military info from api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;

                //this section is for apllicant 
                if (WORKFLOWID <= 1 || ViewBag.NOT_RELATED_STATION == true)
                {

                    rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect th permit to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for permits cell 
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //permits cell should redirect the permit for the security officer 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;


                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for security officer 
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect the permit for permits cell for printing 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_VECHILE_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = true;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }

                rA42_VECHILE_PASS_DTL.CRD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.UPD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_VECHILE_PASS_DTL.BARCODE = rA42_VECHILE_PASS_DTL.BARCODE;

                db.RA42_VECHILE_PASS_DTL.Add(rA42_VECHILE_PASS_DTL);
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                                        ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE,
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
                                    //delete all uploaded files if there is problem with one file, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_VECHILE_PASS_DTL);
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
                            rA42_MEMBERS_DTL.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                                            return View(rA42_VECHILE_PASS_DTL);
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
                                        return View(rA42_VECHILE_PASS_DTL);
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
                        return View(rA42_VECHILE_PASS_DTL);
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
            return View(rA42_VECHILE_PASS_DTL);
        }



        //permit for others outside of MOD
        public ActionResult ProveIdentity()
        {

            ViewBag.activetab = "ProveIdentity";
            //ViewBag.activetab = "Search";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];

            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E");
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E");
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E");
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E");
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E");
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E");
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 10 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }
                }
            }
            else
            {
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME");
                //get relatives typs in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types codes in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE");
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR");
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT");
                //get colrs in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR");
                //get vechiles types in arabic (صالون - دفع رباعي)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME");
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 10 && d.DLT_STS != true && z.DLT_STS !=true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }
                }
            }



            return View();
        }
        // POST data 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProveIdentity(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "ProveIdentity";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];


            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 10 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
                    }
                }
            }
            else
            {
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get vechile colors in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechile types (صالون - دفع رباعي ...) in arabic 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 10 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
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


                            //check image format 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {


                                fileName = "Profile_3_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_VECHILE_PASS_DTL);
                                }
                                rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_VECHILE_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                rA42_VECHILE_PASS_DTL.CARD_FOR_CODE = 10;
                rA42_VECHILE_PASS_DTL.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.RESPONSIBLE;
                rA42_VECHILE_PASS_DTL.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                rA42_VECHILE_PASS_DTL.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                rA42_VECHILE_PASS_DTL.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                rA42_VECHILE_PASS_DTL.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                rA42_VECHILE_PASS_DTL.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                rA42_VECHILE_PASS_DTL.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                rA42_VECHILE_PASS_DTL.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                rA42_VECHILE_PASS_DTL.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                rA42_VECHILE_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_VECHILE_PASS_DTL.RESPONSIBLE = currentUser;

                //get user military info from api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;

                //this section is for apllicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {

                    rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect th permit to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for permits cell 
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //permits cell should redirect the permit for the security officer 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;


                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for security officer 
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect the permit for permits cell for printing 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_VECHILE_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = true;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }

                rA42_VECHILE_PASS_DTL.CRD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.UPD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_VECHILE_PASS_DTL.BARCODE = rA42_VECHILE_PASS_DTL.BARCODE;

                db.RA42_VECHILE_PASS_DTL.Add(rA42_VECHILE_PASS_DTL);
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                                        ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE,
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
                                    //delete all uploaded files if there is problem with one file, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_VECHILE_PASS_DTL);
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
            return View(rA42_VECHILE_PASS_DTL);
        }


        //permit for others outside of MOD
        public ActionResult ProveIdentityBusdriver()
        {

            ViewBag.activetab = "ProveIdentityBusdriver";
            //ViewBag.activetab = "Search";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];

            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E");
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E");
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E");
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E");
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E");
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E");
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 19 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }
                }
            }
            else
            {
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME");
                //get relatives typs in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types codes in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE");
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR");
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT");
                //get colrs in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR");
                //get vechiles types in arabic (صالون - دفع رباعي)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME");
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 19 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }
                }
            }



            return View();
        }
        // POST data 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProveIdentityBusdriver(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "ProveIdentityBusdriver";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];


            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 19 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
                    }
                }
            }
            else
            {
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get vechile colors in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechile types (صالون - دفع رباعي ...) in arabic 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 19 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
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


                            //check image format 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {


                                fileName = "Profile_3_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_VECHILE_PASS_DTL);
                                }
                                rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_VECHILE_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                rA42_VECHILE_PASS_DTL.CARD_FOR_CODE = 10;
                rA42_VECHILE_PASS_DTL.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.RESPONSIBLE;
                rA42_VECHILE_PASS_DTL.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                rA42_VECHILE_PASS_DTL.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                rA42_VECHILE_PASS_DTL.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                rA42_VECHILE_PASS_DTL.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                rA42_VECHILE_PASS_DTL.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                rA42_VECHILE_PASS_DTL.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                rA42_VECHILE_PASS_DTL.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                rA42_VECHILE_PASS_DTL.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                rA42_VECHILE_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_VECHILE_PASS_DTL.RESPONSIBLE = currentUser;

                //get user military info from api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;

                //this section is for apllicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {

                    rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect th permit to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for permits cell 
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //permits cell should redirect the permit for the security officer 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;


                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for security officer 
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect the permit for permits cell for printing 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_VECHILE_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = true;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }

                rA42_VECHILE_PASS_DTL.CRD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.UPD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_VECHILE_PASS_DTL.BARCODE = rA42_VECHILE_PASS_DTL.BARCODE;

                db.RA42_VECHILE_PASS_DTL.Add(rA42_VECHILE_PASS_DTL);
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                                        ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE,
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
                                    //delete all uploaded files if there is problem with one file, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_VECHILE_PASS_DTL);
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
            return View(rA42_VECHILE_PASS_DTL);
        }

        public ActionResult ForRnoOfficersOnly()
        {
            ViewBag.activetab = "ForRnoOfficersOnly";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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


            if (Language.GetCurrentLang() == "en")
            {
               
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E");
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E");
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E");
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E");
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 15 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");


            }
            else
            {
               
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE");
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR");
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT");
                //get colrs in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR");
                //get vechiles types in arabic (صالون - دفع رباعي)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 15 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

            }


            return View();
        }

        // POST: RA42_VECHILE_PASS_DTL/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForRnoOfficersOnly(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL, int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "ForRnoOfficersOnly";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];
            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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

            if (Language.GetCurrentLang() == "en")
            {

                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_VECHILE_PASS_DTL.GENDER_ID);
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 15 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

            }
            else
            {
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get vechile colors in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechile types (صالون - دفع رباعي ...) in arabic 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 15 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");

            }



            if (ModelState.IsValid)
            {


                //check if user upload PERSONAL_IMAGE 
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


                                fileName = "Profile_3_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_VECHILE_PASS_DTL);
                                }
                                rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if formate not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_VECHILE_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                //get current user military information from api 
                User user1 = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user1 = callTask.Result;
                rA42_VECHILE_PASS_DTL.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.SERVICE_NUMBER;
                rA42_VECHILE_PASS_DTL.CARD_FOR_CODE = 15;
                rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE = 1;
                rA42_VECHILE_PASS_DTL.RANK_A = user1.NAME_RANK_A;
                rA42_VECHILE_PASS_DTL.RANK_E = user1.NAME_RANK_E;
                rA42_VECHILE_PASS_DTL.NAME_A = user1.NAME_EMP_A;
                rA42_VECHILE_PASS_DTL.NAME_E = user1.NAME_EMP_E;
                rA42_VECHILE_PASS_DTL.UNIT_A = user1.NAME_UNIT_A;
                rA42_VECHILE_PASS_DTL.UNIT_E = user1.NAME_UNIT_E;
                rA42_VECHILE_PASS_DTL.PROFESSION_A = user1.NAME_TRADE_A;
                rA42_VECHILE_PASS_DTL.PROFESSION_E = user1.NAME_TRADE_E;
                rA42_VECHILE_PASS_DTL.DATE_FROM = DateTime.Today;
                rA42_VECHILE_PASS_DTL.DATE_TO = DateTime.Today.AddDays(300);
                rA42_VECHILE_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_VECHILE_PASS_DTL.CRD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.UPD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_VECHILE_PASS_DTL.REJECTED = false;

                //get user military info from api 
                User user = null;
                Task<User> callTask1 = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask1.Wait();
                user = callTask1.Result;

               
                
               
                    //he should redirect the permit for permits cell for printing 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_VECHILE_PASS_DTL.AUTHO_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = true;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                rA42_VECHILE_PASS_DTL.ISPRINTED = true;
                rA42_VECHILE_PASS_DTL.ISDELIVERED = true;
                rA42_VECHILE_PASS_DTL.BARCODE = rA42_VECHILE_PASS_DTL.BARCODE;
                db.RA42_VECHILE_PASS_DTL.Add(rA42_VECHILE_PASS_DTL);
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
                                        ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE,
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
                                    //delete all uploaded files if there is somthing wrong with one file, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_VECHILE_PASS_DTL);
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
            return View(rA42_VECHILE_PASS_DTL);
        }

        //for trainees
        //permit for others outside of MOD
        public ActionResult Trainee()
        {

            ViewBag.activetab = "Trainee";
            //ViewBag.activetab = "Search";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];

            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E");
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E");
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E");
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E");
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E");
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E");
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 17 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }
                }
            }
            else
            {
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME");
                //get relatives typs in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types codes in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE");
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR");
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT");
                //get colrs in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR");
                //get vechiles types in arabic (صالون - دفع رباعي)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME");
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 17 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        //return RedirectToAction("Create");
                    }
                }
            }



            return View();
        }
        // POST data 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Trainee(RA42_VECHILE_PASS_DTL rA42_VECHILE_PASS_DTL,
            int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES, string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            ViewBag.activetab = "Trainee";
            ViewBag.Service_No = currentUser;
            var url = Url.RequestContext.RouteData.Values["id"];


            //check if session not null
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //get station code to detrmine how to show autho person (الركن المختص - قائد الجناح أو السرب)
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
                //company name 
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //gt permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 17 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
                    }
                }
            }
            else
            {
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == COMPANY_TYPE_CODE && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_VECHILE_PASS_DTL.COMPANY_CODE);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_VECHILE_PASS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_VECHILE_PASS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_VECHILE_PASS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_VECHILE_PASS_DTL.PASS_TYPE_CODE);
                if (STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_VECHILE_PASS_DTL.PLATE_CODE);
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_VECHILE_PASS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_VECHILE_PASS_DTL.VECHILE_CODE);
                //get vechile colors in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_VECHILE_PASS_DTL.VECHILE_COLOR_CODE);
                //get vechile types (صالون - دفع رباعي ...) in arabic 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_VECHILE_PASS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == FORCE_ID && z.CARD_FOR_CODE == 17 && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني)
                var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);
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


                            //check image format 
                            if (general.CheckPersonalImage(PERSONAL_IMAGE.FileName))
                            {


                                fileName = "Profile_3_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                               GetResourcesValue("error_update_message"),
                               "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_VECHILE_PASS_DTL);
                                }
                                rA42_VECHILE_PASS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_VECHILE_PASS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }
                rA42_VECHILE_PASS_DTL.CARD_FOR_CODE = 17;
                rA42_VECHILE_PASS_DTL.SERVICE_NUMBER = rA42_VECHILE_PASS_DTL.RESPONSIBLE;
                rA42_VECHILE_PASS_DTL.RANK_A = rA42_VECHILE_PASS_DTL.RANK_A;
                rA42_VECHILE_PASS_DTL.RANK_E = rA42_VECHILE_PASS_DTL.RANK_E;
                rA42_VECHILE_PASS_DTL.NAME_A = rA42_VECHILE_PASS_DTL.NAME_A;
                rA42_VECHILE_PASS_DTL.NAME_E = rA42_VECHILE_PASS_DTL.NAME_E;
                rA42_VECHILE_PASS_DTL.UNIT_A = rA42_VECHILE_PASS_DTL.UNIT_A;
                rA42_VECHILE_PASS_DTL.UNIT_E = rA42_VECHILE_PASS_DTL.UNIT_E;
                rA42_VECHILE_PASS_DTL.PROFESSION_A = rA42_VECHILE_PASS_DTL.PROFESSION_A;
                rA42_VECHILE_PASS_DTL.PROFESSION_E = rA42_VECHILE_PASS_DTL.PROFESSION_E;
                rA42_VECHILE_PASS_DTL.STATION_CODE = STATION_CODE;
                rA42_VECHILE_PASS_DTL.RESPONSIBLE = currentUser;

                //get user military info from api 
                User user = null;
                Task<User> callTask = Task.Run(
                    () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                    );
                callTask.Wait();
                user = callTask.Result;

                //this section is for apllicant 
                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {

                    rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect th permit to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }
                    rA42_VECHILE_PASS_DTL.APPROVAL_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.APPROVAL_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for permits cell 
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //permits cell should redirect the permit for the security officer 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_VECHILE_PASS_DTL.PERMIT_SN = currentUser;
                    rA42_VECHILE_PASS_DTL.PERMIT_NAME = user.NAME_EMP_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;


                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = false;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }
                //this section is for security officer 
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect the permit for permits cell for printing 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return View(rA42_VECHILE_PASS_DTL);

                    }
                    else
                    {
                        rA42_VECHILE_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                    }

                    rA42_VECHILE_PASS_DTL.AUTHO_SN = currentUser;
                    //rA42_VECHILE_PASS_DTL.AUTHO_NAME = user.NAME_EMP_A;
                    //rA42_VECHILE_PASS_DTL.AUTHO_RANK = user.NAME_RANK_A;
                    rA42_VECHILE_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                    rA42_VECHILE_PASS_DTL.REJECTED = false;
                    rA42_VECHILE_PASS_DTL.STATUS = true;
                    rA42_VECHILE_PASS_DTL.ISOPENED = true;
                }

                rA42_VECHILE_PASS_DTL.CRD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.UPD_BY = currentUser;
                rA42_VECHILE_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_VECHILE_PASS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                rA42_VECHILE_PASS_DTL.BARCODE = rA42_VECHILE_PASS_DTL.BARCODE;

                db.RA42_VECHILE_PASS_DTL.Add(rA42_VECHILE_PASS_DTL);
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE;
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
                                        ACCESS_ROW_CODE = rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE,
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
                                    //delete all uploaded files if there is problem with one file, this is security procedures 
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_VECHILE_PASS_DTL.VECHILE_PASS_CODE && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not supported files format - صيغة الملف غير مدعومة";
                                    return View(rA42_VECHILE_PASS_DTL);
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
            return View(rA42_VECHILE_PASS_DTL);
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

            var vv = db.RA42_VECHILE_PASS_DTL.Where(a => a.VECHILE_PASS_CODE == rA42_MEMBERS_DTL.ACCESS_ROW_CODE).FirstOrDefault();

            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_MEMBERS_DTL.IDENTITY_CODE);
                //get zones and gates in en
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == vv.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == vv.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == vv.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == vv.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
            var f = db.RA42_VECHILE_PASS_DTL.Where(a => a.VECHILE_PASS_CODE == rA42_MEMBERS_DTL.ACCESS_ROW_CODE).FirstOrDefault();
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
            var f = db.RA42_VECHILE_PASS_DTL.Where(a => a.VECHILE_PASS_CODE == v.ACCESS_ROW_CODE).FirstOrDefault();
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
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == f.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
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
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == f.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
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
                    v.ACCESS_ROW_CODE = f.VECHILE_PASS_CODE;
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


        //this function to get relatives as json array
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            db.Configuration.ProxyCreationEnabled = false;

            if (Language.GetCurrentLang() == "en")
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


        [HttpPost]
        public JsonResult DeleteRelative(int id)
        {
            bool result = false;
            RA42_MEMBERS_DTL rA42_RELATIVE_MST = db.RA42_MEMBERS_DTL.Find(id);
            if (rA42_RELATIVE_MST != null)
            {
                rA42_RELATIVE_MST.DLT_STS = true;
                rA42_RELATIVE_MST.UPD_BY = currentUser;
                rA42_RELATIVE_MST.UPD_DT = DateTime.Now;
                db.SaveChanges();
                AddToast(new Toast("",
                GetResourcesValue("success_delete_message"),
               "green"));
                result = true;
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            AddToast(new Toast("",
               GetResourcesValue("error_update_message"),
              "red"));
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
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            AddToast(new Toast("",
             GetResourcesValue("error_update_message"),
            "red"));

            return Json(result, JsonRequestBehavior.AllowGet);


        }
        //notfound page 
        public ActionResult NotFound()
        {
            return RedirectToAction("NotFound", "Home");
        }


        



    }
}
