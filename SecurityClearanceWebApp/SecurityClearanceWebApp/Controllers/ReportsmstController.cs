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

namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    //this controller for developer only, this is for adding new reports for the authorized users in deffirent stations 
	public class ReportsmstController : Controller
	{
        //get db connection 
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunction class
        private GeneralFunctions general = new GeneralFunctions();
        //set title for the controller from resources
        private string title = Resources.Settings.ResourceManager.GetString("Reports" + "_" + "ar");
        public ReportsmstController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Reportsmst";
            //set fontawsome icon for the controller 
			ViewBag.controllerIconClass = "fa fa-file-excel";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("Reports" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;



        }


		// get all reports, this is main view to add delete, edit and retrive reports 
		public ActionResult Index()
		{
            if (ViewBag.DEVELOPER != true)
            {
                general.NotFound();

            }





            //get stations 
            if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.STATIONS_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a=>a.DLT_STS !=true), "STATION_CODE", "STATION_NAME_E");

                }
                else
                {
                    ViewBag.STATIONS_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true), "STATION_CODE", "STATION_NAME_A");

                }
                ///list unhiding reports 
                return View(db.RA42_REPORTS_MST.Where(a => a.DLT_STS != true).OrderByDescending(a => a.REPORT_CODE).ToList());
          
            

		}
        //save and edit new data
        public JsonResult SaveDataInDatabase(RA42_REPORTS_MST model)
        {

            var result = false;
            try
            {
                //if REPORT_CODE is > 0 that means this record needs update 
                if (model.REPORT_CODE > 0)
                {
                    //check update permession for the current user 
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
                    //check if the record in the table then update data
                    RA42_REPORTS_MST rA42_REPORTS_MST = db.RA42_REPORTS_MST.SingleOrDefault(x => x.DLT_STS != true && x.REPORT_CODE == model.REPORT_CODE);
                    rA42_REPORTS_MST.STATION_ID = model.STATION_ID;
                    rA42_REPORTS_MST.REPORT_NAME = model.REPORT_NAME;
                    rA42_REPORTS_MST.REPORT_NAME_E = model.REPORT_NAME_E;
                    rA42_REPORTS_MST.REPORT_URL = model.REPORT_URL;
                    rA42_REPORTS_MST.UPD_BY = currentUser;
                    rA42_REPORTS_MST.UPD_DT = DateTime.Now;
                    rA42_REPORTS_MST.DLT_STS = false;
                    db.SaveChanges();
                    result = true;
                    AddToast(new Toast("",
                   GetResourcesValue("success_update_message"),
                   "green"));
                }
                else
                {
                    //check add permession for the current user 
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
                    //add new report
                    RA42_REPORTS_MST rA42_REPORTS_MST = new RA42_REPORTS_MST();
                    rA42_REPORTS_MST.STATION_ID = model.STATION_ID;
                    rA42_REPORTS_MST.REPORT_NAME = model.REPORT_NAME;
                    rA42_REPORTS_MST.REPORT_NAME_E = model.REPORT_NAME_E;
                    rA42_REPORTS_MST.REPORT_URL = model.REPORT_URL;
                    rA42_REPORTS_MST.UPD_BY = currentUser;
                    rA42_REPORTS_MST.UPD_DT = DateTime.Now;
                    rA42_REPORTS_MST.CRD_BY = currentUser;
                    rA42_REPORTS_MST.CRD_DT = DateTime.Now;
                    rA42_REPORTS_MST.DLT_STS = false;
                    db.RA42_REPORTS_MST.Add(rA42_REPORTS_MST);
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
        //get specific record data as json 
        [HttpGet]
        public JsonResult GetObjectById(int Id)
        {
            var value = (from a in db.RA42_REPORTS_MST
                         join b in db.RA42_STATIONS_MST on a.STATION_ID equals b.STATION_CODE
                         where a.REPORT_CODE == Id
                         select new
                         {

                             a.REPORT_CODE,
                             a.REPORT_NAME,
                             a.REPORT_NAME_E,
                             a.REPORT_URL,
                             a.STATION_ID,
                             b.STATION_NAME_A,
                             b.STATION_NAME_E,
                             a.CRD_BY,
                             a.CRD_DT,
                             a.UPD_BY,
                             a.UPD_DT


                         }).FirstOrDefault();

            //return new JsonResult() { Data = value, JsonRequestBehavior = JsonRequestBehavior.AllowGet, MaxJsonLength = Int32.MaxValue };
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
            //check if record in the table then hide record
            RA42_REPORTS_MST rA42_REPORTS_MST = db.RA42_REPORTS_MST.Where(x => x.REPORT_CODE == Id).FirstOrDefault();
            if (rA42_REPORTS_MST != null)
            {
                rA42_REPORTS_MST.UPD_BY = currentUser;
                rA42_REPORTS_MST.UPD_DT = DateTime.Now;
                //hide record by set DLT_STS to true 
                rA42_REPORTS_MST.DLT_STS = true;
                db.SaveChanges();
                result = true;
                AddToast(new Toast("",
                   GetResourcesValue("success_delete_message"),
                   "green"));
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }



        public ActionResult ReportsPage()
        {
            ViewBag.controllerName = "";
            string t = Resources.Common.ResourceManager.GetString("reports" + "_" + "ar");
            if (Language.GetCurrentLang() == "en")
            {
                t = Resources.Common.ResourceManager.GetString("reports" + "_" + "en");
            }
            ViewBag.Settings = "";
            ViewBag.controllerNamePlural = t;
            ViewBag.controllerNameSingular = t;
            ViewBag.ReportPage = "Reports";

            if (ViewBag.RP != true)
            {
                if (ViewBag.ADMIN != true)
                {
                    if (ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }
           
            if (ViewBag.ADMIN == true || ViewBag.DEVELOPER == true)
            {
                int force_id = ViewBag.FORCE_TYPE_CODE;
                var reports = db.RA42_REPORTS_MST.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.FORCE_ID == force_id).OrderBy(a => a.REPORT_CODE).ToList();
                return View(reports);
            }
            else
            {

                int station_code = ViewBag.STATION_CODE_TYPE;
                var reports = db.RA42_REPORTS_MST.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.STATION_CODE == station_code).OrderBy(a => a.REPORT_CODE).ToList();
                return View(reports);

            }
           
        }
        public ActionResult BillPaper()
        {
            ViewBag.Settings = "";
            ViewBag.ReportPage = "Reports";
            return View();
        }
        public ActionResult IdentityByCompanies()
        {
            ViewBag.Settings = "";
            ViewBag.ReportPage = "Reports";
            ViewBag.controllerName = "";
            ViewBag.controllerNamePlural = "تقرير الجنسيات";
            ViewBag.controllerNameSingular = "تقرير الجنسيات";
            ViewBag.Date = DateTime.Today;
            ViewBag.Result = null;

            return View();
        }

        [HttpPost]
        public ActionResult IdentityByCompanies(DateTime? From, DateTime? To)
        {
            if (ViewBag.RP == true || ViewBag.DEVELOPER == true || ViewBag.Admin == true)
            {
                ViewBag.Settings = "";
                ViewBag.ReportPage = "Reports";
                ViewBag.controllerName = "";
                ViewBag.controllerNamePlural = "تقرير الجنسيات";
                ViewBag.controllerNameSingular = "تقرير الجنسيات";
                ViewBag.Date = "من: " + From.Value.ToShortDateString() + " إلى: " + To.Value.ToShortDateString();
                //get current user STATION_CODE from viewbag.STATION_CODE_TYPE which is identified in PermessionFilter class in Filters folder
                int station = Convert.ToInt32(ViewBag.STATION_CODE_TYPE);
                //int station = 20;

                var car = (from d in db.RA42_VECHILE_PASS_DTL
                           where d.DLT_STS != true && d.STATION_CODE == station && d.ISPRINTED == true && d.CARD_FOR_CODE == 2 && (d.CRD_DT >= From && d.CRD_DT <= To)
                           select new IdentityResult
                           {
                               Type = "1",
                               Company = d.RA42_COMPANY_MST.COMPANY_NAME,
                               Identity = d.RA42_IDENTITY_MST.IDENTITY_TYPE_A,


                           }).OrderBy(v => v.Company).ToList();

                var temp = (from d in db.RA42_TEMPRORY_COMPANY_PASS_DTL
                            where d.DLT_STS != true && d.RA42_COMPANY_PASS_DTL.STATION_CODE == station && d.ISPRINTED == true && (d.CRD_DT >= From && d.CRD_DT <= To)
                            select new IdentityResult
                            {
                                Type = "2",
                                Company = d.RA42_COMPANY_PASS_DTL.RA42_COMPANY_MST.COMPANY_NAME,
                                Identity = d.RA42_IDENTITY_MST.IDENTITY_TYPE_A

                            }).OrderBy(v => v.Company).ToList();

                var vistiorByCar = (from d in db.RA42_VISITOR_PASS_DTL
                                    where d.DLT_STS != true && d.VECHILE_CODE != null && d.STATION_CODE == station && d.ISPRINTED == true && d.CARD_FOR_CODE == 12 && (d.CRD_DT >= From && d.CRD_DT <= To)
                                    select new IdentityResult
                                    {
                                        Type = "3",
                                        Company = d.RA42_COMPANY_MST.COMPANY_NAME,
                                        Identity = d.RA42_IDENTITY_MST.IDENTITY_TYPE_A,


                                    }).OrderBy(v => v.Company).ToList();
                var vistiorNotByCar = (from d in db.RA42_VISITOR_PASS_DTL
                                       where d.DLT_STS != true && d.VECHILE_CODE == null && d.STATION_CODE == station && d.ISPRINTED == true && d.CARD_FOR_CODE == 12 && (d.CRD_DT >= From && d.CRD_DT <= To)
                                       select new IdentityResult
                                       {
                                           Type = "4",
                                           Company = d.RA42_COMPANY_MST.COMPANY_NAME,
                                           Identity = d.RA42_IDENTITY_MST.IDENTITY_TYPE_A,


                                       }).OrderBy(v => v.Company).ToList();

                var combination = new[] { car, temp, vistiorByCar, vistiorNotByCar }.SelectMany(x => x);
                //ViewBag.CarResult = Enumerable.Concat(car, secu, temp).ToList();
                ViewBag.Result = combination.ToList();
                return View();
            }
            else
            {
                return RedirectToAction("NotFound", "Home");
            }
        }
        public async Task<ActionResult> RnoPaymentReport()
        {
            if (ViewBag.RP == true || ViewBag.DEVELOPER == true || ViewBag.Admin == true)
            {
                ViewBag.Settings = "";
                ViewBag.ReportPage = "Reports";
                ViewBag.controllerName = "";
                ViewBag.controllerNamePlural = Resources.Passes.ResourceManager.GetString("payment_report" + "_" + ViewBag.lang);
                ViewBag.controllerNameSingular = Resources.Passes.ResourceManager.GetString("payment_report" + "_" + ViewBag.lang);
                ViewBag.Date = DateTime.Today.Date.ToShortDateString();
                //get current user STATION_CODE from viewbag.STATION_CODE_TYPE which is identified in PermessionFilter class in Filters folder
                int station = Convert.ToInt32(ViewBag.STATION_CODE_TYPE);
                //int station = 20;
                DateTime today = DateTime.Today;
                var car = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_VECHILE_PASS_DTL on a.ACCESS_ROW_CODE equals d.VECHILE_PASS_CODE
                           where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                           select new PaymentResult
                           {
                               Id = d.VECHILE_PASS_CODE,
                               Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                               CreatedDate = a.CRD_DT.Value,

                           }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var secu = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_SECURITY_PASS_DTL on a.ACCESS_ROW_CODE equals d.SECURITY_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                            select new PaymentResult
                            {
                                Id = d.SECURITY_CODE,
                                Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                CreatedDate = a.CRD_DT.Value,


                            }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var fami = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_FAMILY_PASS_DTL on a.ACCESS_ROW_CODE equals d.FAMILY_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                            select new PaymentResult

                            {
                                Id = d.FAMILY_CODE,
                                Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                CreatedDate = a.CRD_DT.Value,

                            }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var temp = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_TEMPRORY_COMPANY_PASS_DTL on a.ACCESS_ROW_CODE equals d.TEMPRORY_COMPANY_PASS_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.RA42_COMPANY_PASS_DTL.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                            select new PaymentResult
                            {
                                Id = d.TEMPRORY_COMPANY_PASS_CODE,
                                Sympol = d.ID_CARD_NUMBER,
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - بدون مركبة",
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                CreatedDate = a.CRD_DT.Value,

                            }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var visit = (from a in db.RA42_TRANSACTION_DTL
                             join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                             join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                             join d in db.RA42_VISITOR_PASS_DTL on a.ACCESS_ROW_CODE equals d.VISITOR_PASS_CODE
                             where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                             select new PaymentResult
                             {
                                 Id = d.VISITOR_PASS_CODE,
                                 Sympol = d.ID_CARD_NUMBER,
                                 Name = d.VISITOR_NAME,
                                 TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                 AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - بدون مركبة",
                                 IssueDate = d.DATE_FROM.Value,
                                 ExpiredDate = d.DATE_TO.Value,
                                 Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                 CreatedDate = a.CRD_DT.Value

                             }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var trainee = (from a in db.RA42_TRANSACTION_DTL
                               join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                               join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                               join d in db.RA42_TRAINEES_PASS_DTL on a.ACCESS_ROW_CODE equals d.TRAINEE_PASS_CODE
                               where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                               select new PaymentResult
                               {
                                   Id = d.TRAINEE_PASS_CODE,
                                   Sympol = d.ID_CARD_NUMBER,
                                   Name = d.TRAINEE_NAME,
                                   TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                   AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                   IssueDate = d.DATE_FROM.Value,
                                   ExpiredDate = d.DATE_TO.Value,
                                   Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                   CreatedDate = a.CRD_DT.Value,

                               }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var eve = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_PERMITS_DTL on a.ACCESS_ROW_CODE equals d.PERMIT_CODE
                           where a.DLT_STS != true && a.ACCESS_TYPE_CODE == 7 && d.DLT_STS != true && d.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && a.CRD_DT == DateTime.Today
                           select new PaymentResult
                           {
                               Id = d.PERMIT_CODE,
                               Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                               CreatedDate = a.CRD_DT.Value,

                           }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var contra = (from a in db.RA42_TRANSACTION_DTL
                              join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                              join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                              join d in db.RA42_CONTRACTING_COMPANIES_PASS_DTL on a.ACCESS_ROW_CODE equals d.CONTRACT_CODE
                              where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && a.CRD_DT == DateTime.Today
                              select new PaymentResult
                              {
                                  Id = d.CONTRACT_CODE,
                                  Sympol = d.ID_CARD_NUMBER,
                                  Name = d.NAME_A,
                                  TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                  AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE,
                                  IssueDate = d.DATE_FROM.Value,
                                  ExpiredDate = d.DATE_TO.Value,
                                  Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                  CreatedDate = a.CRD_DT.Value,

                              }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var air = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_AIR_CREW_PASS_DTL on a.ACCESS_ROW_CODE equals d.AIR_CREW_PASS_CODE
                           where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && a.CRD_DT == DateTime.Today
                           select new PaymentResult
                           {
                               Id = d.AIR_CREW_PASS_CODE,
                               Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE,
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                               CreatedDate = a.CRD_DT.Value,

                           }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var combination = new[] { car, secu, fami, temp, visit, trainee, eve, contra, air }.SelectMany(x => x);
                //ViewBag.CarResult = Enumerable.Concat(car, secu, temp).ToList();
                ViewBag.Result = combination.ToList();
                return View();
            }
            else
            {
                return RedirectToAction("NotFound", "Home");
            }
        }
        [HttpPost]
        public ActionResult RnoPaymentReport(DateTime? From, DateTime? To)
        {
            if (ViewBag.RP == true || ViewBag.DEVELOPER == true || ViewBag.Admin == true)
            {
                ViewBag.Settings = "";
                ViewBag.ReportPage = "Reports";
                ViewBag.controllerName = "";
                ViewBag.controllerNamePlural = Resources.Passes.ResourceManager.GetString("payment_report" + "_" + ViewBag.lang);
                ViewBag.controllerNameSingular = Resources.Passes.ResourceManager.GetString("payment_report" + "_" + ViewBag.lang);
                ViewBag.Date = "من: " + From.Value.ToShortDateString() + " إلى: " + To.Value.ToShortDateString();
                //get current user STATION_CODE from viewbag.STATION_CODE_TYPE which is identified in PermessionFilter class in Filters folder
                int station = Convert.ToInt32(ViewBag.STATION_CODE_TYPE);
                //int station = 20;

                var car = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_VECHILE_PASS_DTL on a.ACCESS_ROW_CODE equals d.VECHILE_PASS_CODE
                           where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                           select new PaymentResult
                           {
                               Id = d.VECHILE_PASS_CODE,
                               Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                           }).OrderBy(a => a.Id).ToList();
                var secu = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_SECURITY_PASS_DTL on a.ACCESS_ROW_CODE equals d.SECURITY_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                            select new PaymentResult
                            {
                                Id = d.SECURITY_CODE,
                                Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                            }).OrderBy(a => a.Id).ToList();
                var fami = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_FAMILY_PASS_DTL on a.ACCESS_ROW_CODE equals d.FAMILY_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                            select new PaymentResult
                            {
                                Id = d.FAMILY_CODE,
                                Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                            }).OrderBy(a => a.Id).ToList();
                var temp = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_TEMPRORY_COMPANY_PASS_DTL on a.ACCESS_ROW_CODE equals d.TEMPRORY_COMPANY_PASS_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.RA42_COMPANY_PASS_DTL.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                            select new PaymentResult
                            {
                                Id = d.TEMPRORY_COMPANY_PASS_CODE,
                                Sympol = d.ID_CARD_NUMBER,
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - بدون مركبة",
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                            }).OrderBy(a => a.Id).ToList();
                var visit = (from a in db.RA42_TRANSACTION_DTL
                             join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                             join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                             join d in db.RA42_VISITOR_PASS_DTL on a.ACCESS_ROW_CODE equals d.VISITOR_PASS_CODE
                             where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                             select new PaymentResult
                             {
                                 Id = d.VISITOR_PASS_CODE,
                                 Sympol = d.ID_CARD_NUMBER,
                                 Name = d.VISITOR_NAME,
                                 TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                 AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - بدون مركبة",
                                 IssueDate = d.DATE_FROM.Value,
                                 ExpiredDate = d.DATE_TO.Value,
                                 Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                             }).OrderBy(a => a.Id).ToList();
                var trainee = (from a in db.RA42_TRANSACTION_DTL
                               join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                               join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                               join d in db.RA42_TRAINEES_PASS_DTL on a.ACCESS_ROW_CODE equals d.TRAINEE_PASS_CODE
                               where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                               select new PaymentResult
                               {
                                   Id = d.TRAINEE_PASS_CODE,
                                   Sympol = d.ID_CARD_NUMBER,
                                   Name = d.TRAINEE_NAME,
                                   TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                   AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                   IssueDate = d.DATE_FROM.Value,
                                   ExpiredDate = d.DATE_TO.Value,
                                   Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                               }).OrderBy(a => a.Id).ToList();
                var eve = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_PERMITS_DTL on a.ACCESS_ROW_CODE equals d.PERMIT_CODE
                           where a.DLT_STS != true && a.ACCESS_TYPE_CODE ==7 && d.DLT_STS != true && d.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                           select new PaymentResult
                           {
                               Id = d.PERMIT_CODE,
                               Sympol = (d.CIVIL_NUMBER!= null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                           }).OrderBy(a => a.Id).ToList();
                var contra = (from a in db.RA42_TRANSACTION_DTL
                              join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                              join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                              join d in db.RA42_CONTRACTING_COMPANIES_PASS_DTL on a.ACCESS_ROW_CODE equals d.CONTRACT_CODE
                              where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                              select new PaymentResult
                              {
                                  Id = d.CONTRACT_CODE,
                                  Sympol = d.ID_CARD_NUMBER,
                                  Name = d.NAME_A,
                                  TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                  AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE,
                                  IssueDate = d.DATE_FROM.Value,
                                  ExpiredDate = d.DATE_TO.Value,
                                  Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                              }).OrderBy(a => a.Id).ToList();
                var air = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_AIR_CREW_PASS_DTL on a.ACCESS_ROW_CODE equals d.AIR_CREW_PASS_CODE
                           where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISPRINTED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                           select new PaymentResult
                           {
                               Id = d.AIR_CREW_PASS_CODE,
                               Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE,
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                           }).OrderBy(a => a.Id).ToList();
                var combination = new[] { car, secu, fami, temp, visit, trainee, eve, contra, air }.SelectMany(x => x);
                //ViewBag.CarResult = Enumerable.Concat(car, secu, temp).ToList();
                ViewBag.Result = combination.ToList();
                return View();
            }
            else
            {
                return RedirectToAction("NotFound", "Home");
            }
        }
        public async Task<ActionResult> RaoPaymentReport()
        {
            if (ViewBag.RP == true || ViewBag.DEVELOPER == true || ViewBag.Admin == true)
            {
                ViewBag.Settings = "";
                ViewBag.ReportPage = "Reports";
                ViewBag.controllerName = "";
                ViewBag.controllerNamePlural = Resources.Passes.ResourceManager.GetString("payment_report" + "_" + ViewBag.lang);
                ViewBag.controllerNameSingular = Resources.Passes.ResourceManager.GetString("payment_report" + "_" + ViewBag.lang);
                ViewBag.Date = DateTime.Today.Date.ToShortDateString();
                //get current user STATION_CODE from viewbag.STATION_CODE_TYPE which is identified in PermessionFilter class in Filters folder
                int station = Convert.ToInt32(ViewBag.STATION_CODE_TYPE);
                //int station = 20;
                DateTime today = DateTime.Today;
                var car = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_VECHILE_PASS_DTL on a.ACCESS_ROW_CODE equals d.VECHILE_PASS_CODE
                           where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                           select new PaymentResult
                           {
                               Id = d.VECHILE_PASS_CODE,
                               Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                               CreatedDate = a.CRD_DT.Value,

                           }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var secu = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_SECURITY_PASS_DTL on a.ACCESS_ROW_CODE equals d.SECURITY_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                            select new PaymentResult
                            {
                                Id = d.SECURITY_CODE,
                                Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                CreatedDate = a.CRD_DT.Value,


                            }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var fami = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_FAMILY_PASS_DTL on a.ACCESS_ROW_CODE equals d.FAMILY_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                            select new PaymentResult

                            {
                                Id = d.FAMILY_CODE,
                                Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                CreatedDate = a.CRD_DT.Value,

                            }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var temp = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_TEMPRORY_COMPANY_PASS_DTL on a.ACCESS_ROW_CODE equals d.TEMPRORY_COMPANY_PASS_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.RA42_COMPANY_PASS_DTL.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                            select new PaymentResult
                            {
                                Id = d.TEMPRORY_COMPANY_PASS_CODE,
                                Sympol = d.ID_CARD_NUMBER,
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - بدون مركبة",
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                CreatedDate = a.CRD_DT.Value,

                            }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var visit = (from a in db.RA42_TRANSACTION_DTL
                             join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                             join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                             join d in db.RA42_VISITOR_PASS_DTL on a.ACCESS_ROW_CODE equals d.VISITOR_PASS_CODE
                             where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                             select new PaymentResult
                             {
                                 Id = d.VISITOR_PASS_CODE,
                                 Sympol = d.ID_CARD_NUMBER,
                                 Name = d.VISITOR_NAME,
                                 TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                 AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - بدون مركبة",
                                 IssueDate = d.DATE_FROM.Value,
                                 ExpiredDate = d.DATE_TO.Value,
                                 Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                 CreatedDate = a.CRD_DT.Value

                             }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var trainee = (from a in db.RA42_TRANSACTION_DTL
                               join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                               join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                               join d in db.RA42_TRAINEES_PASS_DTL on a.ACCESS_ROW_CODE equals d.TRAINEE_PASS_CODE
                               where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                               select new PaymentResult
                               {
                                   Id = d.TRAINEE_PASS_CODE,
                                   Sympol = d.ID_CARD_NUMBER,
                                   Name = d.TRAINEE_NAME,
                                   TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                   AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                   IssueDate = d.DATE_FROM.Value,
                                   ExpiredDate = d.DATE_TO.Value,
                                   Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                   CreatedDate = a.CRD_DT.Value,

                               }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var eve = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_PERMITS_DTL on a.ACCESS_ROW_CODE equals d.PERMIT_CODE
                           where a.DLT_STS != true && a.ACCESS_TYPE_CODE ==7 && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && a.CRD_DT == DateTime.Today
                           select new PaymentResult
                           {
                               Id = d.PERMIT_CODE,
                               Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                               CreatedDate = a.CRD_DT.Value,

                           }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var contra = (from a in db.RA42_TRANSACTION_DTL
                              join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                              join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                              join d in db.RA42_CONTRACTING_COMPANIES_PASS_DTL on a.ACCESS_ROW_CODE equals d.CONTRACT_CODE
                              where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && a.CRD_DT == DateTime.Today
                              select new PaymentResult
                              {
                                  Id = d.CONTRACT_CODE,
                                  Sympol = d.ID_CARD_NUMBER,
                                  Name = d.NAME_A,
                                  TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                  AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE,
                                  IssueDate = d.DATE_FROM.Value,
                                  ExpiredDate = d.DATE_TO.Value,
                                  Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                  CreatedDate = a.CRD_DT.Value,

                              }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var air = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_AIR_CREW_PASS_DTL on a.ACCESS_ROW_CODE equals d.AIR_CREW_PASS_CODE
                           where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && a.CRD_DT == DateTime.Today
                           select new PaymentResult
                           {
                               Id = d.AIR_CREW_PASS_CODE,
                               Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE,
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                               CreatedDate = a.CRD_DT.Value,

                           }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var combination = new[] { car, secu, fami, temp, visit, trainee, eve, contra, air }.SelectMany(x => x);
                //ViewBag.CarResult = Enumerable.Concat(car, secu, temp).ToList();
                ViewBag.Result = combination.ToList();
                return View();
            }
            else
            {
                return RedirectToAction("NotFound", "Home");
            }
        }
        [HttpPost]
        public ActionResult RaoPaymentReport(DateTime? From, DateTime? To)
        {
            if (ViewBag.RP == true || ViewBag.DEVELOPER == true || ViewBag.Admin == true)
            {
                ViewBag.Settings = "";
                ViewBag.ReportPage = "Reports";
                ViewBag.controllerName = "";
                ViewBag.controllerNamePlural = Resources.Passes.ResourceManager.GetString("payment_report" + "_" + ViewBag.lang);
                ViewBag.controllerNameSingular = Resources.Passes.ResourceManager.GetString("payment_report" + "_" + ViewBag.lang);
                ViewBag.Date = "من: " + From.Value.ToShortDateString() + " إلى: " + To.Value.ToShortDateString();
                //get current user STATION_CODE from viewbag.STATION_CODE_TYPE which is identified in PermessionFilter class in Filters folder
                int station = Convert.ToInt32(ViewBag.STATION_CODE_TYPE);
                //int station = 20;

                var car = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_VECHILE_PASS_DTL on a.ACCESS_ROW_CODE equals d.VECHILE_PASS_CODE
                           where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                           select new PaymentResult
                           {
                               Id = d.VECHILE_PASS_CODE,
                               Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                           }).OrderBy(a => a.Id).ToList();
                var secu = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_SECURITY_PASS_DTL on a.ACCESS_ROW_CODE equals d.SECURITY_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                            select new PaymentResult
                            {
                                Id = d.SECURITY_CODE,
                                Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                            }).OrderBy(a => a.Id).ToList();
                var fami = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_FAMILY_PASS_DTL on a.ACCESS_ROW_CODE equals d.FAMILY_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                            select new PaymentResult
                            {
                                Id = d.FAMILY_CODE,
                                Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                            }).OrderBy(a => a.Id).ToList();
                var temp = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_TEMPRORY_COMPANY_PASS_DTL on a.ACCESS_ROW_CODE equals d.TEMPRORY_COMPANY_PASS_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.RA42_COMPANY_PASS_DTL.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                            select new PaymentResult
                            {
                                Id = d.TEMPRORY_COMPANY_PASS_CODE,
                                Sympol = d.ID_CARD_NUMBER,
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - بدون مركبة",
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                            }).OrderBy(a => a.Id).ToList();
                var visit = (from a in db.RA42_TRANSACTION_DTL
                             join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                             join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                             join d in db.RA42_VISITOR_PASS_DTL on a.ACCESS_ROW_CODE equals d.VISITOR_PASS_CODE
                             where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                             select new PaymentResult
                             {
                                 Id = d.VISITOR_PASS_CODE,
                                 Sympol = d.ID_CARD_NUMBER,
                                 Name = d.VISITOR_NAME,
                                 TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                 AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - بدون مركبة",
                                 IssueDate = d.DATE_FROM.Value,
                                 ExpiredDate = d.DATE_TO.Value,
                                 Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                             }).OrderBy(a => a.Id).ToList();
                var trainee = (from a in db.RA42_TRANSACTION_DTL
                               join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                               join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                               join d in db.RA42_TRAINEES_PASS_DTL on a.ACCESS_ROW_CODE equals d.TRAINEE_PASS_CODE
                               where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                               select new PaymentResult
                               {
                                   Id = d.TRAINEE_PASS_CODE,
                                   Sympol = d.ID_CARD_NUMBER,
                                   Name = d.TRAINEE_NAME,
                                   TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                   AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                   IssueDate = d.DATE_FROM.Value,
                                   ExpiredDate = d.DATE_TO.Value,
                                   Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                               }).OrderBy(a => a.Id).ToList();
                var eve = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_PERMITS_DTL on a.ACCESS_ROW_CODE equals d.PERMIT_CODE
                           where a.DLT_STS != true && a.ACCESS_TYPE_CODE == 7 && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                           select new PaymentResult
                           {
                               Id = d.PERMIT_CODE,
                               Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                           }).OrderBy(a => a.Id).ToList();
                var contra = (from a in db.RA42_TRANSACTION_DTL
                              join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                              join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                              join d in db.RA42_CONTRACTING_COMPANIES_PASS_DTL on a.ACCESS_ROW_CODE equals d.CONTRACT_CODE
                              where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                              select new PaymentResult
                              {
                                  Id = d.CONTRACT_CODE,
                                  Sympol = d.ID_CARD_NUMBER,
                                  Name = d.NAME_A,
                                  TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                  AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE,
                                  IssueDate = d.DATE_FROM.Value,
                                  ExpiredDate = d.DATE_TO.Value,
                                  Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                              }).OrderBy(a => a.Id).ToList();
                var air = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_AIR_CREW_PASS_DTL on a.ACCESS_ROW_CODE equals d.AIR_CREW_PASS_CODE
                           where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                           select new PaymentResult
                           {
                               Id = d.AIR_CREW_PASS_CODE,
                               Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE,
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                           }).OrderBy(a => a.Id).ToList();
                var combination = new[] { car, secu, fami, temp, visit, trainee, eve, contra, air }.SelectMany(x => x);
                //ViewBag.CarResult = Enumerable.Concat(car, secu, temp).ToList();
                ViewBag.Result = combination.ToList();
                return View();
            }
            else
            {
                return RedirectToAction("NotFound", "Home");
            }
        }
        public async Task<ActionResult> RaoPaymentReportNoBill()
        {
            if (ViewBag.RP == true || ViewBag.DEVELOPER == true || ViewBag.Admin == true)
            {
                ViewBag.Settings = "";
                ViewBag.ReportPage = "Reports";
                ViewBag.controllerName = "";
                ViewBag.controllerNamePlural = Resources.Passes.ResourceManager.GetString("payment_report" + "_" + ViewBag.lang);
                ViewBag.controllerNameSingular = Resources.Passes.ResourceManager.GetString("payment_report" + "_" + ViewBag.lang);
                ViewBag.Date = DateTime.Today.Date.ToShortDateString();
                //get current user STATION_CODE from viewbag.STATION_CODE_TYPE which is identified in PermessionFilter class in Filters folder
                int station = Convert.ToInt32(ViewBag.STATION_CODE_TYPE);
                //int station = 20;
                DateTime today = DateTime.Today;
                var car = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_VECHILE_PASS_DTL on a.ACCESS_ROW_CODE equals d.VECHILE_PASS_CODE
                           where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                           select new PaymentResult
                           {
                               Id = d.VECHILE_PASS_CODE,
                               Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                               Unit = d.UNIT_A,
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                               CreatedDate = a.CRD_DT.Value,

                           }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var secu = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_SECURITY_PASS_DTL on a.ACCESS_ROW_CODE equals d.SECURITY_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                            select new PaymentResult
                            {
                                Id = d.SECURITY_CODE,
                                Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                Unit = d.UNIT_A,
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                CreatedDate = a.CRD_DT.Value,


                            }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var fami = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_FAMILY_PASS_DTL on a.ACCESS_ROW_CODE equals d.FAMILY_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                            select new PaymentResult

                            {
                                Id = d.FAMILY_CODE,
                                Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                Unit = d.UNIT_A,
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                CreatedDate = a.CRD_DT.Value,

                            }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var temp = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_TEMPRORY_COMPANY_PASS_DTL on a.ACCESS_ROW_CODE equals d.TEMPRORY_COMPANY_PASS_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.RA42_COMPANY_PASS_DTL.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                            select new PaymentResult
                            {
                                Id = d.TEMPRORY_COMPANY_PASS_CODE,
                                Sympol = d.ID_CARD_NUMBER,
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - بدون مركبة",
                                Unit = d.RA42_COMPANY_PASS_DTL.RA42_COMPANY_MST.COMPANY_NAME,
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                CreatedDate = a.CRD_DT.Value,

                            }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var visit = (from a in db.RA42_TRANSACTION_DTL
                             join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                             join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                             join d in db.RA42_VISITOR_PASS_DTL on a.ACCESS_ROW_CODE equals d.VISITOR_PASS_CODE
                             where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                             select new PaymentResult
                             {
                                 Id = d.VISITOR_PASS_CODE,
                                 Sympol = d.ID_CARD_NUMBER,
                                 Name = d.VISITOR_NAME,
                                 TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                 AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - بدون مركبة",
                                 Unit = d.RESPONSIBLE,
                                 IssueDate = d.DATE_FROM.Value,
                                 ExpiredDate = d.DATE_TO.Value,
                                 Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                 CreatedDate = a.CRD_DT.Value

                             }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var trainee = (from a in db.RA42_TRANSACTION_DTL
                               join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                               join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                               join d in db.RA42_TRAINEES_PASS_DTL on a.ACCESS_ROW_CODE equals d.TRAINEE_PASS_CODE
                               where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0
                               select new PaymentResult
                               {
                                   Id = d.TRAINEE_PASS_CODE,
                                   Sympol = d.ID_CARD_NUMBER,
                                   Name = d.TRAINEE_NAME,
                                   TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                   AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                   Unit = d.RESPONSIBLE,
                                   IssueDate = d.DATE_FROM.Value,
                                   ExpiredDate = d.DATE_TO.Value,
                                   Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                   CreatedDate = a.CRD_DT.Value,

                               }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var eve = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_PERMITS_DTL on a.ACCESS_ROW_CODE equals d.PERMIT_CODE
                           where a.DLT_STS != true && a.ACCESS_TYPE_CODE == 7 && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && a.CRD_DT == DateTime.Today
                           select new PaymentResult
                           {
                               Id = d.PERMIT_CODE,
                               Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                               Unit = "",
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                               CreatedDate = a.CRD_DT.Value,

                           }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var contra = (from a in db.RA42_TRANSACTION_DTL
                              join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                              join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                              join d in db.RA42_CONTRACTING_COMPANIES_PASS_DTL on a.ACCESS_ROW_CODE equals d.CONTRACT_CODE
                              where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && a.CRD_DT == DateTime.Today
                              select new PaymentResult
                              {
                                  Id = d.CONTRACT_CODE,
                                  Sympol = d.ID_CARD_NUMBER,
                                  Name = d.NAME_A,
                                  TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                  AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE,
                                  Unit = "",
                                  IssueDate = d.DATE_FROM.Value,
                                  ExpiredDate = d.DATE_TO.Value,
                                  Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                                  CreatedDate = a.CRD_DT.Value,

                              }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var air = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_AIR_CREW_PASS_DTL on a.ACCESS_ROW_CODE equals d.AIR_CREW_PASS_CODE
                           where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && a.CRD_DT == DateTime.Today
                           select new PaymentResult
                           {
                               Id = d.AIR_CREW_PASS_CODE,
                               Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE,
                               Unit = d.UNIT_A,
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,
                               CreatedDate = a.CRD_DT.Value,

                           }).Where(a => a.CreatedDate == today).OrderBy(a => a.Id).ToList();
                var combination = new[] { car, secu, fami, temp, visit, trainee, eve, contra, air }.SelectMany(x => x);
                //ViewBag.CarResult = Enumerable.Concat(car, secu, temp).ToList();
                ViewBag.Result = combination.ToList();
                return View();
            }
            else
            {
                return RedirectToAction("NotFound", "Home");
            }
        }
        [HttpPost]
        public ActionResult RaoPaymentReportNoBill(DateTime? From, DateTime? To)
        {
            if (ViewBag.RP == true || ViewBag.DEVELOPER == true || ViewBag.Admin == true)
            {
                ViewBag.Settings = "";
                ViewBag.ReportPage = "Reports";
                ViewBag.controllerName = "";
                ViewBag.controllerNamePlural = Resources.Passes.ResourceManager.GetString("payment_report" + "_" + ViewBag.lang);
                ViewBag.controllerNameSingular = Resources.Passes.ResourceManager.GetString("payment_report" + "_" + ViewBag.lang);
                ViewBag.Date = "من: " + From.Value.ToShortDateString() + " إلى: " + To.Value.ToShortDateString();
                //get current user STATION_CODE from viewbag.STATION_CODE_TYPE which is identified in PermessionFilter class in Filters folder
                int station = Convert.ToInt32(ViewBag.STATION_CODE_TYPE);
                //int station = 20;

                var car = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_VECHILE_PASS_DTL on a.ACCESS_ROW_CODE equals d.VECHILE_PASS_CODE
                           where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                           select new PaymentResult
                           {
                               Id = d.VECHILE_PASS_CODE,
                               Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                               Unit = d.UNIT_A,
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                           }).OrderBy(a => a.Id).ToList();
                var secu = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_SECURITY_PASS_DTL on a.ACCESS_ROW_CODE equals d.SECURITY_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                            select new PaymentResult
                            {
                                Id = d.SECURITY_CODE,
                                Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                Unit = d.UNIT_A,
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                            }).OrderBy(a => a.Id).ToList();
                var fami = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_FAMILY_PASS_DTL on a.ACCESS_ROW_CODE equals d.FAMILY_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                            select new PaymentResult
                            {
                                Id = d.FAMILY_CODE,
                                Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                Unit = d.UNIT_A,
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                            }).OrderBy(a => a.Id).ToList();
                var temp = (from a in db.RA42_TRANSACTION_DTL
                            join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                            join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                            join d in db.RA42_TEMPRORY_COMPANY_PASS_DTL on a.ACCESS_ROW_CODE equals d.TEMPRORY_COMPANY_PASS_CODE
                            where a.DLT_STS != true && d.DLT_STS != true && d.RA42_COMPANY_PASS_DTL.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                            select new PaymentResult
                            {
                                Id = d.TEMPRORY_COMPANY_PASS_CODE,
                                Sympol = d.ID_CARD_NUMBER,
                                Name = d.NAME_A,
                                TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - بدون مركبة",
                                Unit = d.RA42_COMPANY_PASS_DTL.RA42_COMPANY_MST.COMPANY_NAME,
                                IssueDate = d.DATE_FROM.Value,
                                ExpiredDate = d.DATE_TO.Value,
                                Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                            }).OrderBy(a => a.Id).ToList();
                var visit = (from a in db.RA42_TRANSACTION_DTL
                             join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                             join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                             join d in db.RA42_VISITOR_PASS_DTL on a.ACCESS_ROW_CODE equals d.VISITOR_PASS_CODE
                             where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                             select new PaymentResult
                             {
                                 Id = d.VISITOR_PASS_CODE,
                                 Sympol = d.ID_CARD_NUMBER,
                                 Name = d.VISITOR_NAME,
                                 TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                 AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - بدون مركبة",
                                 Unit = d.RESPONSIBLE,
                                 IssueDate = d.DATE_FROM.Value,
                                 ExpiredDate = d.DATE_TO.Value,
                                 Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                             }).OrderBy(a => a.Id).ToList();
                var trainee = (from a in db.RA42_TRANSACTION_DTL
                               join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                               join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                               join d in db.RA42_TRAINEES_PASS_DTL on a.ACCESS_ROW_CODE equals d.TRAINEE_PASS_CODE
                               where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                               select new PaymentResult
                               {
                                   Id = d.TRAINEE_PASS_CODE,
                                   Sympol = d.ID_CARD_NUMBER,
                                   Name = d.TRAINEE_NAME,
                                   TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                   AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                   Unit = d.RESPONSIBLE,
                                   IssueDate = d.DATE_FROM.Value,
                                   ExpiredDate = d.DATE_TO.Value,
                                   Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                               }).OrderBy(a => a.Id).ToList();
                var eve = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_PERMITS_DTL on a.ACCESS_ROW_CODE equals d.PERMIT_CODE
                           where a.DLT_STS != true && a.ACCESS_TYPE_CODE == 7 && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                           select new PaymentResult
                           {
                               Id = d.PERMIT_CODE,
                               Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                               Unit = "",
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                           }).OrderBy(a => a.Id).ToList();
                var contra = (from a in db.RA42_TRANSACTION_DTL
                              join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                              join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                              join d in db.RA42_CONTRACTING_COMPANIES_PASS_DTL on a.ACCESS_ROW_CODE equals d.CONTRACT_CODE
                              where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                              select new PaymentResult
                              {
                                  Id = d.CONTRACT_CODE,
                                  Sympol = d.ID_CARD_NUMBER,
                                  Name = d.NAME_A,
                                  TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                                  AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE,
                                  Unit = "",
                                  IssueDate = d.DATE_FROM.Value,
                                  ExpiredDate = d.DATE_TO.Value,
                                  Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                              }).OrderBy(a => a.Id).ToList();
                var air = (from a in db.RA42_TRANSACTION_DTL
                           join b in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals b.ACCESS_TYPE_CODE
                           join c in db.RA42_TRANSACTION_TYPE_MST on a.TRANSACTION_TYPE_CODE equals c.TRANSACTION_TYPE_CODE
                           join d in db.RA42_AIR_CREW_PASS_DTL on a.ACCESS_ROW_CODE equals d.AIR_CREW_PASS_CODE
                           where a.DLT_STS != true && d.DLT_STS != true && d.STATION_CODE == station && d.ISDELIVERED == true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT > 0 && (a.CRD_DT >= From && a.CRD_DT <= To)
                           select new PaymentResult
                           {
                               Id = d.AIR_CREW_PASS_CODE,
                               Sympol = (d.CIVIL_NUMBER != null ? d.CIVIL_NUMBER : d.SERVICE_NUMBER),
                               Name = d.NAME_A,
                               TransactionType = a.RA42_TRANSACTION_TYPE_MST.TRANSACTION_NAME_A,
                               AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE,
                               Unit = d.UNIT_A,
                               IssueDate = d.DATE_FROM.Value,
                               ExpiredDate = d.DATE_TO.Value,
                               Amount = a.RA42_TRANSACTION_TYPE_MST.AMOUNT.Value,

                           }).OrderBy(a => a.Id).ToList();
                var combination = new[] { car, secu, fami, temp, visit, trainee, eve, contra, air }.SelectMany(x => x);
                //ViewBag.CarResult = Enumerable.Concat(car, secu, temp).ToList();
                ViewBag.Result = combination.ToList();
                return View();
            }
            else
            {
                return RedirectToAction("NotFound", "Home");
            }
        }

        public async Task<ActionResult> RnoVisitorReport()
        {
            if (ViewBag.RP == true || ViewBag.DEVELOPER == true || ViewBag.Admin == true)
            {
                ViewBag.Settings = "";
                ViewBag.ReportPage = "Reports";
                ViewBag.controllerName = "";
                ViewBag.controllerNamePlural = Resources.Passes.ResourceManager.GetString("visitor_temp" + "_" + ViewBag.lang);
                ViewBag.controllerNameSingular = Resources.Passes.ResourceManager.GetString("visitor_temp" + "_" + ViewBag.lang);
                ViewBag.Date = DateTime.Today.Date.ToShortDateString();
                //get current user STATION_CODE from viewbag.STATION_CODE_TYPE which is identified in PermessionFilter class in Filters folder
                int station = Convert.ToInt32(ViewBag.STATION_CODE_TYPE);
                //int station = 20;
                DateTime today = DateTime.Today;

                var visit = (from d in db.RA42_VISITOR_PASS_DTL
                             where d.DLT_STS != true && d.STATION_CODE == station && d.STATUS == true && d.CRD_DT == today
                             select new PaymentResult
                             {
                                 Id = d.VISITOR_PASS_CODE,
                                 Sympol = d.ID_CARD_NUMBER,
                                 Name = d.VISITOR_NAME,
                                 TransactionType = "",
                                 AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                 IssueDate = d.DATE_FROM.Value,
                                 ExpiredDate = d.DATE_TO.Value,
                                 Amount = 1,

                             }).OrderBy(a => a.Id).ToList();

                var combination = new[] { visit }.SelectMany(x => x);
                //ViewBag.CarResult = Enumerable.Concat(car, secu, temp).ToList();
                ViewBag.Result = combination.ToList();
                return View();
            }
            else
            {
                return RedirectToAction("NotFound", "Home");
            }
        }
        [HttpPost]
        public ActionResult RnoVisitorReport(DateTime? From, DateTime? To)
        {
            if (ViewBag.RP == true || ViewBag.DEVELOPER == true || ViewBag.Admin == true)
            {
                ViewBag.Settings = "";
                ViewBag.ReportPage = "Reports";
                ViewBag.controllerName = "";
                ViewBag.controllerNamePlural = Resources.Passes.ResourceManager.GetString("visitor_temp" + "_" + ViewBag.lang);
                ViewBag.controllerNameSingular = Resources.Passes.ResourceManager.GetString("visitor_temp" + "_" + ViewBag.lang);
                ViewBag.Date = "من: " + From.Value.ToShortDateString() + " إلى: " + To.Value.ToShortDateString();
                //get current user STATION_CODE from viewbag.STATION_CODE_TYPE which is identified in PermessionFilter class in Filters folder
                int station = Convert.ToInt32(ViewBag.STATION_CODE_TYPE);
                //int station = 20;

                var visit = (
                             from d in db.RA42_VISITOR_PASS_DTL
                             where d.DLT_STS != true && d.STATION_CODE == station && d.STATUS == true && (d.CRD_DT >= From && d.CRD_DT <= To)
                             select new PaymentResult
                             {
                                 Id = d.VISITOR_PASS_CODE,
                                 Sympol = d.ID_CARD_NUMBER,
                                 Name = d.VISITOR_NAME,
                                 TransactionType = "",
                                 AccessType = d.RA42_ACCESS_TYPE_MST.ACCESS_TYPE + " - " + d.RA42_CARD_FOR_MST.CARD_FOR_A,
                                 IssueDate = d.DATE_FROM.Value,
                                 ExpiredDate = d.DATE_TO.Value,
                                 Amount = 1,

                             }).OrderBy(a => a.Id).ToList();

                var combination = new[] { visit }.SelectMany(x => x);
                //ViewBag.CarResult = Enumerable.Concat(car, secu, temp).ToList();
                ViewBag.Result = combination.ToList();
                return View();
            }
            else
            {
                return RedirectToAction("NotFound", "Home");
            }
        }


        //this is notfound page
        public ActionResult NotFound()
        {
            return View();
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
