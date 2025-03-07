using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using APP.Util;
using SecurityClearanceWebApp.Models;
using portal.Controllers;
using SecurityClearanceWebApp.Services;
using System.Linq.Dynamic;
using SecurityClearanceWebApp.Util;
using PagedList;
using System.Web.WebPages;
using System.IO;
using System.Data.Entity.Validation;
using System.Web.UI.WebControls;


namespace SecurityClearanceWebApp.Controllers
{
    //[UserInfoFilter]
	public class PermitsdtlController : Controller
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
        private int ACCESS_TYPE_CODE = 0;
        private int STATION_CODE = 0;
        private string FORCE_CODE = "";
       // List<string> permitsTypes = new List<string>();

        //set title of the whole controller 
        string title = Resources.Common.ResourceManager.GetString("permits" + "_" + "ar");
        public PermitsdtlController() {
            ViewBag.Managepasses = "Managepasses";
            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Permitsdtl";
            //set icon from fontawsome library for the controller 
            ViewBag.controllerIconClass = "fa fa-sheild";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Passes.ResourceManager.GetString("access_type_vechile" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;
            //check if the current user has authirty in this type of permit 
            var v = Task.Run(async () => await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefaultAsync()).Result;
            if (v != null)
            {
                if (v.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && v.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false)
                {
                    //get id of the user
                    ViewBag.RESPO_ID = v.WORKFLOW_RESPO_CODE;
                    //check workflow id type of the user 
                    ViewBag.RESPO_STATE = v.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE;
                    RESPO_CODE = v.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE;
                    WORKFLOWID = v.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE.Value;
                    FORCE_CODE = v.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE.ToString();
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
        // GET: RA42_VECHILE_PASS_DTL
        public ActionResult Index()
		{
			return View();
		}

        [HttpPost]
        public JsonResult GetList(int id,string tab)
        {

            db.Configuration.ProxyCreationEnabled = false;

            //Server Side Parameter
            int start = Convert.ToInt32(Request["start"]);
            int length = Convert.ToInt32(Request["length"]);
            string searchValue = Request["search[value]"];
            string sortColumnName = Request["columns[" + Request["order[0][column]"] + "][name]"];
            string sortDirection = Request["order[0][dir]"];
            int access = id;
            List<ClearanceSearchResult> result = new List<ClearanceSearchResult>();

            DateTime threeYearsAgo = DateTime.Now.AddYears(-3);
            DateTime overThreeMonth = DateTime.Now.AddMonths(-3);

            try
            {
                var new_permits = db.RA42_PERMITS_DTL.AsNoTracking().Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == access
                && a.WORKFLOW_RESPO_CODE != null &&  a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo
                ).Select(r => new ClearanceSearchResult
                {
                    Id = r.PERMIT_CODE,
                    ControllerName = "Permitsdtl",
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
                    ServiceNumber = r.SERVICE_NUMBER,
                    Responsipole = (r.COMPANY_PASS_CODE != null ? r.RA42_COMPANY_PASS_DTL.RESPONSIBLE : "-"),
                    PesronalImage = r.PERSONAL_IMAGE,
                    PurposeOfPass = r.PURPOSE_OF_PASS,
                    Name = r.NAME_A,
                    Rank = r.RANK_A,
                    HosRank = r.HOST_RANK_A,
                    HostName = r.HOST_NAME_A,
                    Phone = r.PHONE_NUMBER,
                    Gsm = r.GSM,
                    PermitType = r.RA42_CARD_FOR_MST.CARD_FOR_A,
                    CivilNumber = r.CIVIL_NUMBER,
                    IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                    ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                    CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                    UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                    StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                    Status = r.STATUS,
                    Delivered = r.ISDELIVERED,
                    CardFor = r.CARD_FOR_CODE.Value,
                    RegistrationSerial = r.REGISTRATION_SERIAL,
                    Company = (r.COMPANY_CODE != null ? r.RA42_COMPANY_MST.COMPANY_NAME : " "),
                    EventExercise = (r.EVENT_EXERCISE_CODE != null ? r.RA42_EVENT_EXERCISE_MST.EVENT_EXERCISE_NAME : " "),
                    Printed = r.ISPRINTED,
                    Opened = r.ISOPENED,
                    Rejected = r.REJECTED,
                    Returned = r.RETURNED,
                    PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                    Deleted = r.DLT_STS,
                    Station = r.STATION_CODE.Value,
                    Unit = (r.UNIT_A != null ? r.UNIT_A : "-"),
                    Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                    ResponsipoleServiceNumber = r.RESPONSIBLE,
                    CarColor = (r.VECHILE_COLOR_CODE != null ? r.RA42_VECHILE_COLOR_MST.COLOR : "-"),
                    PlateNumber = (r.PLATE_NUMBER != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR + "-" + r.PLATE_NUMBER : "-"),
                    PlateCode = (r.PLATE_CHAR_CODE != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR : "-"),
                    CarType = (r.VECHILE_NAME_CODE != null ? r.RA42_VECHILE_NAME_MST.VECHILE_NAME : "-"),
                    CarName = (r.VECHILE_CODE != null ? r.RA42_VECHILE_CATIGORY_MST.VECHILE_CAT : "-"),
                    WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                    Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                    WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                    CompanyPermitId = (r.COMPANY_PASS_CODE != null ? r.COMPANY_PASS_CODE.Value : 0),
                    Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Count(x => x.PASS_ROW_CODE == r.PERMIT_CODE && x.DLT_STS != true && x.CRD_DT > new DateTime(2025, 2, 23))

                }).ToList();


                if (access == 3)
                {

                    var car = db.RA42_VECHILE_PASS_DTL.AsNoTracking()
                    .Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == access 
                    && a.WORKFLOW_RESPO_CODE != null && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo
                    ).Select(r => new ClearanceSearchResult
                    {
                        Id = r.VECHILE_PASS_CODE,
                        ControllerName = "Vechilepass",
                        AccessNumber = r.ACCESS_TYPE_CODE.Value,
                        ServiceNumber = r.SERVICE_NUMBER,
                        PesronalImage = r.PERSONAL_IMAGE,
                        PurposeOfPass = r.PURPOSE_OF_PASS,
                        Name = r.NAME_A,
                        Rank = r.RANK_A,
                        HosRank = "",
                        HostName = "",
                        Phone = r.PHONE_NUMBER,
                        Gsm = r.GSM,
                        PermitType = "تصريح مركبة - " + r.RA42_CARD_FOR_MST.CARD_FOR_A,
                        CivilNumber = r.CIVIL_NUMBER,
                        IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                        ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                        CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                        UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                        StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                        Unit = (r.UNIT_A != null ? r.UNIT_A : "-"),
                        Status = r.STATUS,
                        Delivered = r.ISDELIVERED,
                        CardFor = r.CARD_FOR_CODE.Value,
                        Company = (r.COMPANY_CODE != null ? r.RA42_COMPANY_MST.COMPANY_NAME : " "),
                        EventExercise = "",
                        Printed = r.ISPRINTED,
                        Opened = r.ISOPENED,
                        Rejected = r.REJECTED,
                        Returned = r.RETURNED,
                        PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                        Deleted = r.DLT_STS,
                        Station = r.STATION_CODE.Value,
                        Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                        ResponsipoleServiceNumber = r.RESPONSIBLE,
                        CarColor = (r.VECHILE_COLOR_CODE != null ? r.RA42_VECHILE_COLOR_MST.COLOR : "-"),
                        PlateNumber = (r.PLATE_NUMBER != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR + "-" + r.PLATE_NUMBER : "-"),
                        PlateCode = (r.PLATE_CHAR_CODE != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR : "-"),
                        CarType = (r.VECHILE_NAME_CODE != null ? r.RA42_VECHILE_NAME_MST.VECHILE_NAME : "-"),
                        CarName = (r.VECHILE_CODE != null ? r.RA42_VECHILE_CATIGORY_MST.VECHILE_CAT : "-"),
                        WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                        Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                        WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                        CompanyPermitId = 0,
                        Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Count(x => x.PASS_ROW_CODE == r.VECHILE_PASS_CODE && x.DLT_STS != true && x.CRD_DT < new DateTime(2025, 2, 23))


                    }).ToList();
                    result = new_permits.Concat(car).ToList();

                }
                if (access == 2)
                {

                    var secu = db.RA42_SECURITY_PASS_DTL.AsNoTracking().Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == access
                     && a.WORKFLOW_RESPO_CODE != null && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo
                      ).Select(r => new ClearanceSearchResult
                      {
                          Id = r.SECURITY_CODE,
                          ControllerName = "Securitypass",
                          AccessNumber = r.ACCESS_TYPE_CODE.Value,
                          ServiceNumber = r.SERVICE_NUMBER,
                          PesronalImage = r.PERSONAL_IMAGE,
                          PurposeOfPass = r.PURPOSE_OF_PASS,
                          Name = r.NAME_A,
                          Rank = r.RANK_A,
                          HosRank = "",
                          HostName = "",
                          Phone = r.PHONE_NUMBER,
                          Gsm = r.GSM,
                          PermitType = "تصريح شخصي بدون مركبة - " + r.RA42_CARD_FOR_MST.CARD_FOR_A,
                          CivilNumber = r.CIVIL_NUMBER,
                          IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                          ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                          CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                          UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                          StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                          Unit = (r.UNIT_A != null ? r.UNIT_A : "-"),
                          Status = r.STATUS,
                          Delivered = r.ISDELIVERED,
                          CardFor = r.CARD_FOR_CODE.Value,
                          Company = (r.COMPANY_CODE != null ? r.RA42_COMPANY_MST.COMPANY_NAME : " "),
                          EventExercise = "",
                          Printed = r.ISPRINTED,
                          Opened = r.ISOPENED,
                          Rejected = r.REJECTED,
                          Returned = r.RETURNED,
                          PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                          Deleted = r.DLT_STS,
                          Station = r.STATION_CODE.Value,
                          Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                          ResponsipoleServiceNumber = r.RESPONSIBLE,
                          CarColor = "-",
                          PlateNumber = "-",
                          PlateCode = "-",
                          CarType = "-",
                          CarName = "-",
                          WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                          Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                          WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                          CompanyPermitId = 0,
                          Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Count(x => x.PASS_ROW_CODE == r.SECURITY_CODE && x.DLT_STS != true && x.CRD_DT < new DateTime(2025, 2, 23))


                      }).ToList();
                    result = new_permits.Concat(secu).ToList();

                }
                if (access == 4)
                {
                    var family = db.RA42_FAMILY_PASS_DTL.AsNoTracking().Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == access
                        && a.WORKFLOW_RESPO_CODE != null && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo
                         ).Select(r => new ClearanceSearchResult
                         {
                             Id = r.FAMILY_CODE,
                             ControllerName = "Familypass",
                             AccessNumber = r.ACCESS_TYPE_CODE.Value,
                             ServiceNumber = r.SERVICE_NUMBER,
                             PesronalImage = r.PERSONAL_IMAGE,
                             PurposeOfPass = r.PURPOSE_OF_PASS,
                             Name = r.NAME_A,
                             Rank = "",
                             HosRank = r.HOST_NAME_A,
                             HostName = r.HOST_RANK_A,
                             Phone = r.PHONE_NUMBER,
                             Gsm = r.GSM,
                             PermitType = "تصريح عائلة بدون مركبة",
                             CivilNumber = r.CIVIL_NUMBER,
                             IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                             ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                             CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                             UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                             StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                             Unit = (r.UNIT_A != null ? r.UNIT_A : "-"),
                             Status = r.STATUS,
                             Delivered = r.ISDELIVERED,
                             CardFor = r.CARD_FOR_CODE.Value,
                             Company = "",
                             EventExercise = "",
                             Printed = r.ISPRINTED,
                             Opened = r.ISOPENED,
                             Rejected = r.REJECTED,
                             Returned = r.RETURNED,
                             PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                             Deleted = r.DLT_STS,
                             Station = r.STATION_CODE.Value,
                             Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                             ResponsipoleServiceNumber = r.RESPONSIBLE,
                             CarColor = "-",
                             PlateNumber = "-",
                             PlateCode = "-",
                             CarType = "-",
                             CarName = "-",
                             WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                             Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                             WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                             CompanyPermitId = 0,
                             Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Count(x => x.PASS_ROW_CODE == r.FAMILY_CODE && x.DLT_STS != true && x.CRD_DT < new DateTime(2025, 2, 23))


                         }).ToList();
                    result = new_permits.Concat(family).ToList();

                }
                if (access == 1)
                {
                    var autho = db.RA42_AUTHORIZATION_PASS_DTL.AsNoTracking().Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == access
                     && a.WORKFLOW_RESPO_CODE != null && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo
                    ).Select(r => new ClearanceSearchResult
                    {
                        Id = r.AUTHORAIZATION_CODE,
                        AccessNumber = r.ACCESS_TYPE_CODE.Value,
                        CardFor = r.CARD_FOR_CODE.Value,
                        ControllerName = "Authoraizationpass",
                        ServiceNumber = r.SERVICE_NUMBER,
                        PesronalImage = r.PERSONAL_IMAGE,
                        PurposeOfPass = r.PURPOSE_OF_PASS,
                        Name = r.NAME_A,
                        Phone = r.PHONE_NUMBER,
                        Gsm = r.GSM,
                        PermitType = "التفويض الأمني",
                        PassType = "",
                        IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                        ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                        CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                        UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                        CivilNumber = r.CIVIL_NUMBER,
                        Printed = r.ISPRINTED,
                        Delivered = r.ISDELIVERED,
                        Opened = r.ISOPENED,
                        Rejected = r.REJECTED,
                        Returned = false,
                        Deleted = r.DLT_STS,
                        CarColor = "-",
                        PlateNumber = "-",
                        PlateCode = "-",
                        CarType = "-",
                        CarName = "-",
                        Unit = (r.UNIT_A != null ? r.UNIT_A : "-"),
                        StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                        Station = r.STATION_CODE.Value,
                        Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                        Status = r.STATUS,
                        Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                        WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                        CompanyPermitId = 0,
                        Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Count(x => x.PASS_ROW_CODE == r.AUTHORAIZATION_CODE && x.DLT_STS != true && x.CRD_DT < new DateTime(2025, 2, 23))




                    }).ToList();
                    result = new_permits.Concat(autho).ToList();
                }
                if (access == 9)
                {
                    var visitors = db.RA42_VISITOR_PASS_DTL.AsNoTracking().Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == access
                     && a.WORKFLOW_RESPO_CODE != null && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo
                    ).Select(r => new ClearanceSearchResult
                    {
                        Id = r.VISITOR_PASS_CODE,
                        ControllerName = "Visitorpass",
                        AccessNumber = r.ACCESS_TYPE_CODE.Value,
                        ServiceNumber = r.SERVICE_NUMBER,
                        PesronalImage = r.PERSONAL_IMAGE,
                        PurposeOfPass = r.PURPOSE_OF_PASS,
                        Name = r.VISITOR_NAME,
                        Rank = "",
                        HosRank = "",
                        HostName = "",
                        Phone = r.PHONE_NUMBER,
                        Gsm = r.GSM,
                        PermitType = "تصريح الزوار - " + r.RA42_CARD_FOR_MST.CARD_FOR_A,
                        CivilNumber = r.ID_CARD_NUMBER,
                        IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                        ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                        CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                        UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                        StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                        Unit = "-",
                        Status = r.STATUS,
                        Delivered = r.ISDELIVERED,
                        CardFor = r.CARD_FOR_CODE.Value,
                        Company = (r.COMPANY_CODE != null ? r.RA42_COMPANY_MST.COMPANY_NAME : " "),
                        EventExercise = "",
                        Printed = r.ISPRINTED,
                        Opened = r.ISOPENED,
                        Rejected = r.REJECTED,
                        Returned = r.RETURNED,
                        PassType = "مؤقت",
                        Deleted = r.DLT_STS,
                        Station = r.STATION_CODE.Value,
                        Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                        ResponsipoleServiceNumber = r.RESPONSIBLE,
                        CarColor = (r.VECHILE_COLOR_CODE != null ? r.RA42_VECHILE_COLOR_MST.COLOR : "-"),
                        PlateNumber = (r.PLATE_NUMBER != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR + "-" + r.PLATE_NUMBER : "-"),
                        PlateCode = (r.PLATE_CHAR_CODE != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR : "-"),
                        CarType = (r.VECHILE_NAME_CODE != null ? r.RA42_VECHILE_NAME_MST.VECHILE_NAME : "-"),
                        CarName = (r.VECHILE_CODE != null ? r.RA42_VECHILE_CATIGORY_MST.VECHILE_CAT : "-"),
                        WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                        Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                        WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                        CompanyPermitId = 0,
                        Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Count(x => x.PASS_ROW_CODE == r.VISITOR_PASS_CODE && x.DLT_STS != true && x.CRD_DT < new DateTime(2025, 2, 23))


                    }).ToList();
                    result = new_permits.Concat(visitors).ToList();
                }
                if (access == 8)
                {
                    var trainees = db.RA42_TRAINEES_PASS_DTL.AsNoTracking().Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == access
                     && a.WORKFLOW_RESPO_CODE != null && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo
                    ).Select(r => new ClearanceSearchResult
                    {
                        Id = r.TRAINEE_PASS_CODE,
                        ControllerName = "Traineepass",
                        AccessNumber = r.ACCESS_TYPE_CODE.Value,
                        ServiceNumber = r.SERVICE_NUMBER,
                        PesronalImage = r.PERSONAL_IMAGE,
                        PurposeOfPass = r.PURPOSE_OF_PASS,
                        Name = r.TRAINEE_NAME,
                        Rank = "",
                        HosRank = "",
                        HostName = "",
                        Phone = r.PHONE_NUMBER,
                        Gsm = r.GSM,
                        PermitType = "تصريح المتدربين وإثبات الهوية - " + r.RA42_CARD_FOR_MST.CARD_FOR_A,
                        CivilNumber = r.ID_CARD_NUMBER,
                        IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                        ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                        CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                        UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                        StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                        Unit = "-",
                        Status = r.STATUS,
                        Delivered = r.ISDELIVERED,
                        CardFor = r.CARD_FOR_CODE.Value,
                        Company = "",
                        EventExercise = "",
                        Printed = r.ISPRINTED,
                        Opened = r.ISOPENED,
                        Rejected = r.REJECTED,
                        Returned = r.RETURNED,
                        PassType = "مؤقت",
                        Deleted = r.DLT_STS,
                        Station = r.STATION_CODE.Value,
                        Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                        ResponsipoleServiceNumber = r.RESPONSIBLE,
                        CarColor = "-",
                        PlateNumber = "-",
                        PlateCode = "-",
                        CarType = "-",
                        CarName = "-",
                        WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                        Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                        WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                        CompanyPermitId = 0,
                        Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Count(x => x.PASS_ROW_CODE == r.TRAINEE_PASS_CODE && x.DLT_STS != true && x.CRD_DT < new DateTime(2025, 2, 23))



                    }).ToList();
                    result = new_permits.Concat(trainees).ToList();
                }
                if (access == 5)
                {
                    var companies_temp = db.RA42_TEMPRORY_COMPANY_PASS_DTL.AsNoTracking().Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == access
                    && a.RA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE != null && a.RA42_COMPANY_PASS_DTL.DLT_STS != true 
                    && a.RA42_COMPANY_PASS_DTL.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo
                    ).Select(r => new ClearanceSearchResult
                    {
                        Id = r.TEMPRORY_COMPANY_PASS_CODE,
                        AccessNumber = r.ACCESS_TYPE_CODE.Value,
                        CardFor = r.RA42_COMPANY_PASS_DTL.CARD_FOR_CODE.Value,
                        ControllerName = "Temprorycompanypass",
                        ServiceNumber = r.RA42_COMPANY_PASS_DTL.SERVICE_NUMBER,
                        PesronalImage = r.PERSONAL_IMAGE,
                        PurposeOfPass = r.RA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS,
                        Name = r.NAME_A,
                        Phone = "-",
                        Gsm = r.GSM,
                        PermitType = "تصريح الشركات" + " - بدون مركبة",
                        Company = r.RA42_COMPANY_PASS_DTL.RA42_COMPANY_MST.COMPANY_NAME,
                        PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                        CivilNumber = r.ID_CARD_NUMBER,
                        Printed = r.ISPRINTED,
                        Delivered = r.ISDELIVERED,
                        Unit = r.RA42_COMPANY_PASS_DTL.RESPONSIBLE,
                        Rejected = r.RA42_COMPANY_PASS_DTL.REJECTED,
                        Returned = r.RETURNED,
                        Opened = r.RA42_COMPANY_PASS_DTL.ISOPENED,
                        Deleted = r.RA42_PASS_TYPE_MST.DLT_STS,
                        IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                        ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                        CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                        UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                        CarColor = "-",
                        PlateNumber = "-",
                        PlateCode = "-",
                        CarType = "-",
                        CarName = "-",
                        StationName = r.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.STATION_NAME_A,
                        Station = r.RA42_COMPANY_PASS_DTL.STATION_CODE.Value,
                        Force = r.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID.Value,
                        Status = r.RA42_COMPANY_PASS_DTL.STATUS,
                        ResponsipoleServiceNumber = r.RA42_COMPANY_PASS_DTL.SERVICE_NUMBER,
                        Workflow = r.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                        WorkflowId = r.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                        CompanyPermitId = r.COMPANY_PASS_CODE.Value,
                        Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Count(x => x.PASS_ROW_CODE == r.COMPANY_PASS_CODE && x.DLT_STS != true && x.CRD_DT < new DateTime(2025, 2, 23))



                    }).ToList();
                    result = new_permits.Concat(companies_temp).ToList();

                }
                if (access == 6)
                {
                    var contract = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.AsNoTracking().Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == access
                     && a.WORKFLOW_RESPO_CODE != null && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo
                    ).Select(r => new ClearanceSearchResult
                    {
                        Id = r.CONTRACT_CODE,
                        AccessNumber = r.ACCESS_TYPE_CODE.Value,
                        CardFor = r.CARD_FOR_CODE.Value,
                        ControllerName = "Cuntractedcompanypass",
                        ServiceNumber = "-",
                        PesronalImage = r.PERSONAL_IMAGE,
                        Name = r.NAME_A,
                        Phone = "-",
                        Gsm = r.GSM,
                        PermitType = "تصريح الشركات المتعاقدة",
                        PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                        CivilNumber = r.ID_CARD_NUMBER,
                        Printed = r.ISPRINTED,
                        Rejected = r.REJECTED,
                        Opened = r.ISOPENED,
                        Deleted = r.DLT_STS,
                        Delivered = r.ISDELIVERED,
                        Returned = r.RETURNED,
                        Company = r.RA42_COMPANY_MST.COMPANY_NAME,
                        Unit = "-",
                        CarColor = "-",
                        PlateNumber = "-",
                        PlateCode = "-",
                        CarType = "-",
                        CarName = "-",
                        IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                        ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                        CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                        UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                        StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                        Station = r.STATION_CODE.Value,
                        Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                        Status = r.STATUS,
                        ResponsipoleServiceNumber = r.SERVICE_NUMBER,
                        Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                        WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                        CompanyPermitId = 0,
                        Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Count(x => x.PASS_ROW_CODE == r.CONTRACT_CODE && x.DLT_STS != true && x.CRD_DT < new DateTime(2025, 2, 23))



                    }).ToList();
                    result = new_permits.Concat(contract).ToList();

                }
                if (access == 10)
                {
                    var air = db.RA42_AIR_CREW_PASS_DTL.AsNoTracking().Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == access
                   && a.WORKFLOW_RESPO_CODE != null && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo
                    ).Select(r => new ClearanceSearchResult
                    {
                        Id = r.AIR_CREW_PASS_CODE,
                        ControllerName = "AirCrewpass",
                        AccessNumber = r.ACCESS_TYPE_CODE.Value,
                        ServiceNumber = r.SERVICE_NUMBER,
                        PesronalImage = r.PERSONAL_IMAGE,
                        PurposeOfPass = r.PURPOSE_OF_PASS,
                        Name = r.NAME_A,
                        Rank = r.RANK_A,
                        HosRank = "",
                        HostName = "",
                        Phone = r.PHONE_NUMBER,
                        Gsm = r.GSM,
                        PermitType = "تصريح أطقم الطيران",
                        CivilNumber = r.CIVIL_NUMBER,
                        IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                        ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                        CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                        UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                        StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                        Status = r.STATUS,
                        Delivered = r.ISDELIVERED,
                        Returned = r.RETURNED,
                        CardFor = r.CARD_FOR_CODE.Value,
                        Unit = (r.UNIT_A != null ? r.UNIT_A : "-"),
                        Company = "",
                        EventExercise = "",
                        Printed = r.ISPRINTED,
                        Opened = r.ISOPENED,
                        Rejected = r.REJECTED,
                        PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                        Deleted = r.DLT_STS,
                        Station = r.STATION_CODE.Value,
                        Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                        ResponsipoleServiceNumber = r.RESPONSIBLE,
                        CarColor = "-",
                        PlateNumber = "-",
                        PlateCode = "-",
                        CarType = "-",
                        CarName = "-",
                        WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                        Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                        WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                        CompanyPermitId = 0,
                        Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Count(x => x.PASS_ROW_CODE == r.AIR_CREW_PASS_CODE && x.DLT_STS != true && x.CRD_DT < new DateTime(2025, 2, 23))




                    }).ToList();
                    result = new_permits.Concat(air).ToList();
                }
                if (access == 7)
                {
                    result = new_permits.ToList();
                }

            }
            catch
            {
            }



           

            switch (tab)
            {

                case "NewPermits":
                    ViewBag.activetab = "NewPermits";
                    ViewBag.IconTab = "fa fa-download";                    
                    result =  result.Where(r => r.WorkflowId == WORKFLOWID && r.Printed != true && r.Status != true)
                               .OrderByDescending(a => a.Id).ToList();
                    break;
                case "ToPrint":
                    ViewBag.activetab = "ToPrint";
                    ViewBag.IconTab = "fa fa-upload";
                    if (ViewBag.FORCE_TYPE_CODE == 3)
                    {
                        result = result.Where(r =>   r.WorkflowId == WORKFLOWID && r.Printed != true && r.Status == true && r.UpdatedDate >= overThreeMonth)
                              .OrderByDescending(a => a.Id).ToList();
                    }
                    else
                    {
                        result = result.Where(r =>   r.WorkflowId == WORKFLOWID && r.Printed != true && r.Status == true)
                                   .OrderByDescending(a => a.Id).ToList();
                    }
                    break;
                case "printed_permits":
                    ViewBag.activetab = "printed_permits";
                    ViewBag.IconTab = "fa fa-print";
                    result = result.Where(r => r.WorkflowId == WORKFLOWID && r.Printed == true && r.Status == true)
                               .OrderByDescending(a => a.Id).ToList();
                    break;
                case "AuthoPermits":
                    ViewBag.activetab = "AuthoPermits";
                    ViewBag.IconTab = "fa fa-download";
                    result = result.Where(r => r.WorkflowServiceNumber == currentUser).ToList();

                    break;
                case "AllPermits":
                    ViewBag.activetab = "AllPermits";
                    ViewBag.IconTab = "fa fa-globe";
                    if (ViewBag.ADMIN == true)
                    {
                        result = result.ToList();

                    }
                    if (ViewBag.DEVELOPER == true)
                    {
                        result = result.ToList();
                    }
                    break;
            }


            var permits = result.ToList();

            //custom filtering
            if (!string.IsNullOrEmpty(Request["columns[0][search][value]"]))
                permits = permits.Where(x => x.Unit != null && x.Unit.ToLower().Contains(Request["columns[0][search][value]"].ToLower())).ToList();
            if (!string.IsNullOrEmpty(Request["columns[1][search][value]"]))
                permits = permits.Where(x => x.ServiceNumber != null && x.ServiceNumber.ToLower() == Request["columns[1][search][value]"].ToLower()).ToList();
            if (!string.IsNullOrEmpty(Request["columns[2][search][value]"]))
                permits = permits.Where(x => x.CivilNumber != null && x.CivilNumber == Request["columns[2][search][value]"].ToLower()).ToList();
            if (!string.IsNullOrEmpty(Request["columns[3][search][value]"]))
                permits = permits.Where(x => x.PlateNumber != null && x.PlateNumber == Request["columns[3][search][value]"].ToLower()).ToList();
            if (!string.IsNullOrEmpty(Request["columns[4][search][value]"]))
                permits = permits.Where(x => x.Gsm == Request["columns[4][search][value]"].ToLower() || x.Phone == Request["columns[4][search][value]"].ToLower()).ToList();
            if (!string.IsNullOrEmpty(Request["columns[5][search][value]"]))
                permits = permits.Where(x => x.Name !=null && x.Name.Contains(Request["columns[5][search][value]"])).ToList();
            if (!string.IsNullOrEmpty(Request["columns[6][search][value]"]))
                permits = permits.Where(x => x.Company != null && x.Company.Contains(Request["columns[6][search][value]"])).ToList();
            if (!string.IsNullOrEmpty(Request["columns[7][search][value]"]))
                permits = permits.Where(x => x.StationName != null && x.StationName.Contains(Request["columns[7][search][value]"])).ToList();

            int totalrows = permits.Count;

            int totalrowsafterfiltering = permits.Count;
            //sorting
            permits = permits.OrderBy(sortColumnName + " " + sortDirection).ToList();
            //paging
            permits = permits.Skip(start).Take(length).ToList();


            return Json(new { data = permits, draw = Request["draw"], recordsTotal = totalrows, recordsFiltered = totalrowsafterfiltering }, JsonRequestBehavior.AllowGet);

        }
        public ActionResult BrowsePermits(
            string type, string tab, string mergedSearch,
            int? page)
        {
            ViewBag.PermitMainType = type;
            int access = Convert.ToInt32(type);
            List<ClearanceSearchResult> result = new List<ClearanceSearchResult>();
            string searchType = "";
            string string_to_search = "";
            if (string.IsNullOrWhiteSpace(mergedSearch))
            {
                searchType = mergedSearch;
            }
            else
            {
                // Split the input string at the underscore
                string[] parts = mergedSearch.Split('_');
                // Return the first part

                if (!parts[1].IsEmpty())
                {
                    string_to_search = parts[1];
                    searchType = parts[0];
                }

            }
            ViewBag.activetab = "";
            ViewBag.IconTab = "";
            ViewBag.CurrentFilter = mergedSearch;
            ViewBag.SearchValue = string_to_search;
            ViewBag.SearchType = searchType;

            int pageSize = 10; // Number of items to display per page
            int pageNumber = page ?? 1; // Default to the first page if no page number is specified
            try
            {
                var new_permits = db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == access &&
                 a.WORKFLOW_RESPO_CODE != null
                ).Select(r => new ClearanceSearchResult
                {
                    Id = r.PERMIT_CODE,
                    ControllerName = "Permitsdtl",
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
                    ServiceNumber = r.SERVICE_NUMBER,
                    Responsipole = (r.COMPANY_PASS_CODE != null ? r.RA42_COMPANY_PASS_DTL.RESPONSIBLE : "-"),
                    PesronalImage = r.PERSONAL_IMAGE,
                    PurposeOfPass = r.PURPOSE_OF_PASS,
                    Name = r.NAME_A,
                    Rank = r.RANK_A,
                    HosRank = r.HOST_RANK_A,
                    HostName = r.HOST_NAME_A,
                    Phone = r.PHONE_NUMBER,
                    Gsm = r.GSM,
                    PermitType = r.RA42_CARD_FOR_MST.CARD_FOR_A,
                    CivilNumber = r.CIVIL_NUMBER,
                    IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                    ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                    CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                    StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                    Status = r.STATUS,
                    Delivered = r.ISDELIVERED,
                    CardFor = r.CARD_FOR_CODE.Value,
                    RegistrationSerial = r.REGISTRATION_SERIAL,
                    Company = (r.COMPANY_CODE != null ? r.RA42_COMPANY_MST.COMPANY_NAME : " "),
                    EventExercise = (r.EVENT_EXERCISE_CODE != null ? r.RA42_EVENT_EXERCISE_MST.EVENT_EXERCISE_NAME : " "),
                    Printed = r.ISPRINTED,
                    Opened = r.ISOPENED,
                    Rejected = r.REJECTED,
                    Returned = r.RETURNED,
                    PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                    Deleted = r.DLT_STS,
                    Station = r.STATION_CODE.Value,
                    Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                    ResponsipoleServiceNumber = r.RESPONSIBLE,
                    CarColor = (r.VECHILE_COLOR_CODE != null ? r.RA42_VECHILE_COLOR_MST.COLOR : "-"),
                    PlateNumber = (r.PLATE_NUMBER != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR + "-" + r.PLATE_NUMBER : "-"),
                    PlateCode = (r.PLATE_CHAR_CODE != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR : "-"),
                    CarType = (r.VECHILE_NAME_CODE != null ? r.RA42_VECHILE_NAME_MST.VECHILE_NAME : "-"),
                    CarName = (r.VECHILE_CODE != null ? r.RA42_VECHILE_CATIGORY_MST.VECHILE_CAT : "-"),
                    WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                    Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                    WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                    Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == r.PERMIT_CODE && r.DLT_STS != true).Count(),
                    Violations = r.RA42_ACCESS_TYPE_MST.RA42_VECHILE_VIOLATION_DTL.Where(x => x.DLT_STS != true && x.ACCESS_ROW_CODE == r.PERMIT_CODE).Count(),
                    CompanyPermitId = (r.COMPANY_PASS_CODE != null ? r.COMPANY_PASS_CODE.Value : 0),

                }).AsQueryable();
                result.AddRange(new_permits);
            }
            catch
            {

            }
            try
            {
                if (access == 3)
                {
                    var car = db.RA42_VECHILE_PASS_DTL
                    .Where(a => a.DLT_STS != true && a.ACCESS_TYPE_CODE == access /*&& permitsTypes.Contains(a.RA42_CARD_FOR_MST.CARD_SECRET_CODE)*/
                    && a.WORKFLOW_RESPO_CODE != null).Select(r => new ClearanceSearchResult
                    {
                        Id = r.VECHILE_PASS_CODE,
                        ControllerName = "Vechilepass",
                        AccessNumber = r.ACCESS_TYPE_CODE.Value,
                        ServiceNumber = r.SERVICE_NUMBER,
                        PesronalImage = r.PERSONAL_IMAGE,
                        PurposeOfPass = r.PURPOSE_OF_PASS,
                        Name = r.NAME_A,
                        Rank = r.RANK_A,
                        HosRank = "",
                        HostName = "",
                        Phone = r.PHONE_NUMBER,
                        Gsm = r.GSM,
                        PermitType = "تصريح مركبة - " + r.RA42_CARD_FOR_MST.CARD_FOR_A,
                        CivilNumber = r.CIVIL_NUMBER,
                        IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                        ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                        CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                        StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                        Status = r.STATUS,
                        Delivered = r.ISDELIVERED,
                        CardFor = r.CARD_FOR_CODE.Value,
                        Company = (r.COMPANY_CODE != null ? r.RA42_COMPANY_MST.COMPANY_NAME : " "),
                        EventExercise = "",
                        Printed = r.ISPRINTED,
                        Opened = r.ISOPENED,
                        Rejected = r.REJECTED,
                        Returned = r.RETURNED,
                        PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                        Deleted = r.DLT_STS,
                        Station = r.STATION_CODE.Value,
                        Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                        ResponsipoleServiceNumber = r.RESPONSIBLE,
                        CarColor = (r.VECHILE_COLOR_CODE != null ? r.RA42_VECHILE_COLOR_MST.COLOR : "-"),
                        PlateNumber = (r.PLATE_NUMBER != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR + "-" + r.PLATE_NUMBER : "-"),
                        PlateCode = (r.PLATE_CHAR_CODE != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR : "-"),
                        CarType = (r.VECHILE_NAME_CODE != null ? r.RA42_VECHILE_NAME_MST.VECHILE_NAME : "-"),
                        CarName = (r.VECHILE_CODE != null ? r.RA42_VECHILE_CATIGORY_MST.VECHILE_CAT : "-"),
                        WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                        Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                        WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                        Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == r.VECHILE_PASS_CODE && r.DLT_STS != true).Count(),
                        Violations = r.RA42_ACCESS_TYPE_MST.RA42_VECHILE_VIOLATION_DTL.Where(x => x.DLT_STS != true && x.ACCESS_ROW_CODE == r.VECHILE_PASS_CODE).Count(),
                        CompanyPermitId = 0,


                    }).AsQueryable();

                    result.AddRange(car);
                }

            }
            catch
            {

            }
            try
            {
                if (access == 2)
                {
                    var secu = db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && a.ACCESS_TYPE_CODE == access
                    /*&& permitsTypes.Contains(a.RA42_CARD_FOR_MST.CARD_SECRET_CODE)*/ && a.WORKFLOW_RESPO_CODE != null
                      ).Select(r => new ClearanceSearchResult
                      {
                          Id = r.SECURITY_CODE,
                          ControllerName = "Securitypass",
                          AccessNumber = r.ACCESS_TYPE_CODE.Value,
                          ServiceNumber = r.SERVICE_NUMBER,
                          PesronalImage = r.PERSONAL_IMAGE,
                          PurposeOfPass = r.PURPOSE_OF_PASS,
                          Name = r.NAME_A,
                          Rank = r.RANK_A,
                          HosRank = "",
                          HostName = "",
                          Phone = r.PHONE_NUMBER,
                          Gsm = r.GSM,
                          PermitType = "تصريح شخصي بدون مركبة - " + r.RA42_CARD_FOR_MST.CARD_FOR_A,
                          CivilNumber = r.CIVIL_NUMBER,
                          IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                          ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                          CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                          StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                          Status = r.STATUS,
                          Delivered = r.ISDELIVERED,
                          CardFor = r.CARD_FOR_CODE.Value,
                          Company = (r.COMPANY_CODE != null ? r.RA42_COMPANY_MST.COMPANY_NAME : " "),
                          EventExercise = "",
                          Printed = r.ISPRINTED,
                          Opened = r.ISOPENED,
                          Rejected = r.REJECTED,
                          Returned = r.RETURNED,
                          PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                          Deleted = r.DLT_STS,
                          Station = r.STATION_CODE.Value,
                          Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                          ResponsipoleServiceNumber = r.RESPONSIBLE,
                          CarColor = "-",
                          PlateNumber = "-",
                          PlateCode = "-",
                          CarType = "-",
                          CarName = "-",
                          WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                          Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                          WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                          Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == r.SECURITY_CODE && r.DLT_STS != true).Count(),
                          Violations = 0,
                          CompanyPermitId = 0,

                      }).AsQueryable();

                    result.AddRange(secu);
                }
            }
            catch
            {

            }
            try
            {
                if (access == 4)
                {
                    var family = db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && a.ACCESS_TYPE_CODE == access
                    /*&& permitsTypes.Contains(a.RA42_CARD_FOR_MST.CARD_SECRET_CODE)*/ && a.WORKFLOW_RESPO_CODE != null
                      ).Select(r => new ClearanceSearchResult
                      {
                          Id = r.FAMILY_CODE,
                          ControllerName = "Familypass",
                          AccessNumber = r.ACCESS_TYPE_CODE.Value,
                          ServiceNumber = r.SERVICE_NUMBER,
                          PesronalImage = r.PERSONAL_IMAGE,
                          PurposeOfPass = r.PURPOSE_OF_PASS,
                          Name = r.NAME_A,
                          Rank = "",
                          HosRank = r.HOST_NAME_A,
                          HostName = r.HOST_RANK_A,
                          Phone = r.PHONE_NUMBER,
                          Gsm = r.GSM,
                          PermitType = "تصريح عائلة بدون مركبة",
                          CivilNumber = r.CIVIL_NUMBER,
                          IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                          ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                          CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                          StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                          Status = r.STATUS,
                          Delivered = r.ISDELIVERED,
                          CardFor = r.CARD_FOR_CODE.Value,
                          Company = "",
                          EventExercise = "",
                          Printed = r.ISPRINTED,
                          Opened = r.ISOPENED,
                          Rejected = r.REJECTED,
                          Returned = r.RETURNED,
                          PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                          Deleted = r.DLT_STS,
                          Station = r.STATION_CODE.Value,
                          Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                          ResponsipoleServiceNumber = r.RESPONSIBLE,
                          CarColor = "-",
                          PlateNumber = "-",
                          PlateCode = "-",
                          CarType = "-",
                          CarName = "-",
                          WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                          Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                          WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                          Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == r.FAMILY_CODE && r.DLT_STS != true).Count(),
                          Violations = 0,
                          CompanyPermitId = 0,

                      }).AsQueryable();
                    result.AddRange(family);
                }
            }
            catch
            {

            }
            try
            {
                if (access == 1)
                {
                    var autho = db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.ACCESS_TYPE_CODE == access
                    /*&& permitsTypes.Contains(a.RA42_CARD_FOR_MST.CARD_SECRET_CODE)*/ && a.WORKFLOW_RESPO_CODE != null
                    ).Select(r => new ClearanceSearchResult
                    {
                        Id = r.AUTHORAIZATION_CODE,
                        AccessNumber = r.ACCESS_TYPE_CODE.Value,
                        CardFor = r.CARD_FOR_CODE.Value,
                        ControllerName = "Authoraizationpass",
                        ServiceNumber = r.SERVICE_NUMBER,
                        PesronalImage = r.PERSONAL_IMAGE,
                        PurposeOfPass = r.PURPOSE_OF_PASS,
                        Name = r.NAME_A,
                        Phone = r.PHONE_NUMBER,
                        Gsm = r.GSM,
                        PermitType = "التفويض الأمني",
                        PassType = "",
                        CivilNumber = r.CIVIL_NUMBER,
                        Printed = r.ISPRINTED,
                        Delivered = r.ISDELIVERED,
                        Opened = r.ISOPENED,
                        Rejected = r.REJECTED,
                        Returned = false,
                        Deleted = r.DLT_STS,
                        CarColor = "-",
                        PlateNumber = "-",
                        PlateCode = "-",
                        CarType = "-",
                        CarName = "-",
                        IssueingDate = r.DATE_FROM.ToString(),
                        ExpiredDate = r.DATE_TO.Value,
                        CreatedDate = r.CRD_DT.Value,
                        StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                        Station = r.STATION_CODE.Value,
                        Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                        Status = r.STATUS,
                        Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                        WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                        Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.DLT_STS != true && x.PASS_ROW_CODE == r.AUTHORAIZATION_CODE).Count(),
                        CompanyPermitId = 0,
                        Violations = 0

                    }).AsQueryable();
                    result.AddRange(autho);
                }
            }
            catch
            {

            }
            try
            {
                if (access == 9)
                {
                    var visitors = db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.ACCESS_TYPE_CODE == access
                    /*&& permitsTypes.Contains(a.RA42_CARD_FOR_MST.CARD_SECRET_CODE)*/ && a.WORKFLOW_RESPO_CODE != null
                    ).Select(r => new ClearanceSearchResult
                    {
                        Id = r.VISITOR_PASS_CODE,
                        ControllerName = "Visitorpass",
                        AccessNumber = r.ACCESS_TYPE_CODE.Value,
                        ServiceNumber = r.SERVICE_NUMBER,
                        PesronalImage = r.PERSONAL_IMAGE,
                        PurposeOfPass = r.PURPOSE_OF_PASS,
                        Name = r.VISITOR_NAME,
                        Rank = "",
                        HosRank = "",
                        HostName = "",
                        Phone = r.PHONE_NUMBER,
                        Gsm = r.GSM,
                        PermitType = "تصريح الزوار - " + r.RA42_CARD_FOR_MST.CARD_FOR_A,
                        CivilNumber = r.ID_CARD_NUMBER,
                        IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                        ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                        CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                        StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                        Status = r.STATUS,
                        Delivered = r.ISDELIVERED,
                        CardFor = r.CARD_FOR_CODE.Value,
                        Company = (r.COMPANY_CODE != null ? r.RA42_COMPANY_MST.COMPANY_NAME : " "),
                        EventExercise = "",
                        Printed = r.ISPRINTED,
                        Opened = r.ISOPENED,
                        Rejected = r.REJECTED,
                        Returned = r.RETURNED,
                        PassType = "مؤقت",
                        Deleted = r.DLT_STS,
                        Station = r.STATION_CODE.Value,
                        Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                        ResponsipoleServiceNumber = r.RESPONSIBLE,
                        CarColor = (r.VECHILE_COLOR_CODE != null ? r.RA42_VECHILE_COLOR_MST.COLOR : "-"),
                        PlateNumber = (r.PLATE_NUMBER != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR + "-" + r.PLATE_NUMBER : "-"),
                        PlateCode = (r.PLATE_CHAR_CODE != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR : "-"),
                        CarType = (r.VECHILE_NAME_CODE != null ? r.RA42_VECHILE_NAME_MST.VECHILE_NAME : "-"),
                        CarName = (r.VECHILE_CODE != null ? r.RA42_VECHILE_CATIGORY_MST.VECHILE_CAT : "-"),
                        WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                        Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                        WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                        Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == r.VISITOR_PASS_CODE && r.DLT_STS != true).Count(),
                        Violations = r.RA42_ACCESS_TYPE_MST.RA42_VECHILE_VIOLATION_DTL.Where(x => x.DLT_STS != true && x.ACCESS_ROW_CODE == r.VISITOR_PASS_CODE).Count(),
                        CompanyPermitId = 0,


                    }).AsQueryable();
                    result.AddRange(visitors);
                }
            }
            catch
            {

            }
            try
            {
                if (access == 8)
                {
                    var trainees = db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.ACCESS_TYPE_CODE == access
                    /*&& permitsTypes.Contains(a.RA42_CARD_FOR_MST.CARD_SECRET_CODE)*/ && a.WORKFLOW_RESPO_CODE != null
                    ).Select(r => new ClearanceSearchResult
                    {
                        Id = r.TRAINEE_PASS_CODE,
                        ControllerName = "Traineepass",
                        AccessNumber = r.ACCESS_TYPE_CODE.Value,
                        ServiceNumber = r.SERVICE_NUMBER,
                        PesronalImage = r.PERSONAL_IMAGE,
                        PurposeOfPass = r.PURPOSE_OF_PASS,
                        Name = r.TRAINEE_NAME,
                        Rank = "",
                        HosRank = "",
                        HostName = "",
                        Phone = r.PHONE_NUMBER,
                        Gsm = r.GSM,
                        PermitType = "تصريح المتدربين وإثبات الهوية - " + r.RA42_CARD_FOR_MST.CARD_FOR_A,
                        CivilNumber = r.ID_CARD_NUMBER,
                        IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                        ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                        CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                        StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                        Status = r.STATUS,
                        Delivered = r.ISDELIVERED,
                        CardFor = r.CARD_FOR_CODE.Value,
                        Company = "",
                        EventExercise = "",
                        Printed = r.ISPRINTED,
                        Opened = r.ISOPENED,
                        Rejected = r.REJECTED,
                        Returned = r.RETURNED,
                        PassType = "مؤقت",
                        Deleted = r.DLT_STS,
                        Station = r.STATION_CODE.Value,
                        Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                        ResponsipoleServiceNumber = r.RESPONSIBLE,
                        CarColor = "-",
                        PlateNumber = "-",
                        PlateCode = "-",
                        CarType = "-",
                        CarName = "-",
                        WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                        Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                        WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                        Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == r.TRAINEE_PASS_CODE && r.DLT_STS != true).Count(),
                        Violations = r.RA42_ACCESS_TYPE_MST.RA42_VECHILE_VIOLATION_DTL.Where(x => x.DLT_STS != true && x.ACCESS_ROW_CODE == r.TRAINEE_PASS_CODE).Count(),
                        CompanyPermitId = 0,



                    }).AsQueryable();
                    result.AddRange(trainees);
                }
            }
            catch
            {

            }
            try
            {
                if (access == 5)
                {
                    var companies_temp = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.ACCESS_TYPE_CODE == access
                    //&& permitsTypes.Contains(a.RA42_COMPANY_PASS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE)
                    && a.RA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE != null && a.RA42_COMPANY_PASS_DTL.DLT_STS != true
                    ).Select(r => new ClearanceSearchResult
                    {
                        Id = r.TEMPRORY_COMPANY_PASS_CODE,
                        AccessNumber = r.ACCESS_TYPE_CODE.Value,
                        CardFor = r.RA42_COMPANY_PASS_DTL.CARD_FOR_CODE.Value,
                        ControllerName = "Temprorycompanypass",
                        ServiceNumber = r.RA42_COMPANY_PASS_DTL.SERVICE_NUMBER,
                        PesronalImage = r.PERSONAL_IMAGE,
                        PurposeOfPass = r.RA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS,
                        Name = r.NAME_A,
                        Phone = "-",
                        Gsm = r.GSM,
                        PermitType = "تصريح الشركات" + " - بدون مركبة",
                        Company = r.RA42_COMPANY_PASS_DTL.RA42_COMPANY_MST.COMPANY_NAME,
                        PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                        CivilNumber = r.ID_CARD_NUMBER,
                        Printed = r.ISPRINTED,
                        Delivered = r.ISDELIVERED,
                        Rejected = r.RA42_COMPANY_PASS_DTL.REJECTED,
                        Returned = r.RETURNED,
                        Opened = r.RA42_COMPANY_PASS_DTL.ISOPENED,
                        Deleted = r.RA42_PASS_TYPE_MST.DLT_STS,
                        IssueingDate = r.DATE_FROM.ToString(),
                        ExpiredDate = r.DATE_TO.Value,
                        CreatedDate = r.CRD_DT.Value,
                        CarColor = "-",
                        PlateNumber = "-",
                        PlateCode = "-",
                        CarType = "-",
                        CarName = "-",
                        StationName = r.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.STATION_NAME_A,
                        Station = r.RA42_COMPANY_PASS_DTL.STATION_CODE.Value,
                        Force = r.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID.Value,
                        Status = r.RA42_COMPANY_PASS_DTL.STATUS,
                        ResponsipoleServiceNumber = r.RA42_COMPANY_PASS_DTL.SERVICE_NUMBER,
                        Workflow = r.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                        WorkflowId = r.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                        Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.DLT_STS != true && x.PASS_ROW_CODE == r.COMPANY_PASS_CODE).Count(),
                        CompanyPermitId = r.COMPANY_PASS_CODE.Value,
                        Violations = 0



                    }).AsQueryable();
                    result.AddRange(companies_temp);
                }

            }
            catch
            {

            }
            try
            {
                if (access == 6)
                {
                    var contract = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && a.ACCESS_TYPE_CODE == access
                    /*&& permitsTypes.Contains(a.RA42_CARD_FOR_MST.CARD_SECRET_CODE)*/ && a.WORKFLOW_RESPO_CODE != null
                    ).Select(r => new ClearanceSearchResult
                    {
                        Id = r.CONTRACT_CODE,
                        AccessNumber = r.ACCESS_TYPE_CODE.Value,
                        CardFor = r.CARD_FOR_CODE.Value,
                        ControllerName = "Cuntractedcompanypass",
                        ServiceNumber = "-",
                        PesronalImage = r.PERSONAL_IMAGE,
                        Name = r.NAME_A,
                        Phone = "-",
                        Gsm = r.GSM,
                        PermitType = "تصريح الشركات المتعاقدة",
                        PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                        CivilNumber = r.ID_CARD_NUMBER,
                        Printed = r.ISPRINTED,
                        Rejected = r.REJECTED,
                        Opened = r.ISOPENED,
                        Deleted = r.DLT_STS,
                        Delivered = r.ISDELIVERED,
                        Returned = r.RETURNED,
                        CarColor = "-",
                        PlateNumber = "-",
                        PlateCode = "-",
                        CarType = "-",
                        CarName = "-",
                        IssueingDate = r.DATE_FROM.ToString(),
                        ExpiredDate = r.DATE_TO.Value,
                        CreatedDate = r.CRD_DT.Value,
                        StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                        Station = r.STATION_CODE.Value,
                        Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                        Status = r.STATUS,
                        ResponsipoleServiceNumber = r.SERVICE_NUMBER,
                        Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                        WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                        Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.DLT_STS != true && x.PASS_ROW_CODE == r.CONTRACT_CODE).Count(),
                        CompanyPermitId = 0,
                        Violations = 0



                    }).AsQueryable();
                    result.AddRange(contract);
                }


            }
            catch
            {

            }
            try
            {
                if (access == 10)
                {
                    var air = db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true && a.ACCESS_TYPE_CODE == access
                    /*&& permitsTypes.Contains(a.RA42_CARD_FOR_MST.CARD_SECRET_CODE)*/ && a.WORKFLOW_RESPO_CODE != null
                    ).Select(r => new ClearanceSearchResult
                    {
                        Id = r.AIR_CREW_PASS_CODE,
                        ControllerName = "AirCrewpass",
                        AccessNumber = r.ACCESS_TYPE_CODE.Value,
                        ServiceNumber = r.SERVICE_NUMBER,
                        PesronalImage = r.PERSONAL_IMAGE,
                        PurposeOfPass = r.PURPOSE_OF_PASS,
                        Name = r.NAME_A,
                        Rank = r.RANK_A,
                        HosRank = "",
                        HostName = "",
                        Phone = r.PHONE_NUMBER,
                        Gsm = r.GSM,
                        PermitType = "تصريح أطقم الطيران",
                        CivilNumber = r.CIVIL_NUMBER,
                        IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                        ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                        CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                        StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                        Status = r.STATUS,
                        Delivered = r.ISDELIVERED,
                        Returned = r.RETURNED,
                        CardFor = r.CARD_FOR_CODE.Value,
                        Company = "",
                        EventExercise = "",
                        Printed = r.ISPRINTED,
                        Opened = r.ISOPENED,
                        Rejected = r.REJECTED,
                        PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                        Deleted = r.DLT_STS,
                        Station = r.STATION_CODE.Value,
                        Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                        ResponsipoleServiceNumber = r.RESPONSIBLE,
                        CarColor = "-",
                        PlateNumber = "-",
                        PlateCode = "-",
                        CarType = "-",
                        CarName = "-",
                        WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                        Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                        WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                        Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == r.AIR_CREW_PASS_CODE && r.DLT_STS != true).Count(),
                        Violations = 0,
                        CompanyPermitId = 0,




                    }).AsQueryable();
                    result.AddRange(air);
                }

            }
            catch
            {

            }

            try
            {
                switch (searchType)
                {
                    case "ServiceNumber":
                        result = result.Where(v => v.ServiceNumber == string_to_search).ToList();
                        break;
                    case "CivilNumber":
                        result = result.Where(v => v.CivilNumber == string_to_search).ToList();
                        break;
                    case "Name":
                        result = result.Where(v => v.Name == string_to_search).ToList();
                        break;
                    case "PlateNumber":
                        result = result.Where(v => v.PlateNumber == string_to_search).ToList();
                        break;
                    case "kafeel":
                        result = result.Where(v => v.ResponsipoleServiceNumber == string_to_search).ToList();
                        break;
                    case "Phone":
                        result = result.Where(v => v.Gsm == string_to_search || v.Phone == string_to_search).ToList();
                        break;
                    case "Company":
                        result = result.Where(v => v.Company.Contains(string_to_search)).ToList();
                        break;
                    case "Station":
                        result = result.Where(v => v.StationName.Contains(string_to_search)).ToList();
                        break;
                }

            }
            catch
            {

            }
            DateTime threeYearsAgo = DateTime.Now.AddYears(-3);
            switch (tab)
            {
                //case "NewPermits":
                //    ViewBag.activetab = "NewPermits";
                //    ViewBag.IconTab = "fa fa-download";
                //    result = result.Where(r => r.Station == STATION_CODE && r.WorkflowId == WORKFLOWID && r.Printed != true && r.Status != true)
                //               .OrderByDescending(a => a.Id).ToList();
                //    break;
                //case "ToPrint":
                //    ViewBag.activetab = "ToPrint";
                //    ViewBag.IconTab = "fa fa-upload";
                //    result = result.Where(r => r.Station == STATION_CODE && r.WorkflowId == WORKFLOWID && r.Printed != true && r.Status == true)
                //               .OrderByDescending(a => a.Id).ToList();
                //    break;
                //case "printed_permits":
                //    ViewBag.activetab = "printed_permits";
                //    ViewBag.IconTab = "fa fa-print";
                //    result = result.Where(r => r.Station == STATION_CODE && r.WorkflowId == WORKFLOWID && r.Printed == true && r.Status == true)
                //               .OrderByDescending(a => a.Id).ToList();
                //    break;
                //case "AuthoPermits":
                //    ViewBag.activetab = "AuthoPermits";
                //    ViewBag.IconTab = "fa fa-download";
                //    result = result.Where(v => v.WorkflowServiceNumber == currentUser).ToList();

                   // break;

                case "AllPermits":
                    ViewBag.activetab = "AllPermits";
                    ViewBag.IconTab = "fa fa-globe";
                    if (ViewBag.ADMIN == true)
                    {
                        result = result.Where(a => a.Force == ViewBag.FORCE_TYPE_CODE && a.CreatedDate > threeYearsAgo).ToList();

                    }
                    if (ViewBag.DEVELOPER == true)
                    {
                        result = result.ToList();
                    }
                    break;
            }

            //result = result.Where(v => v.AccessNumber == access).ToList();


            List<ClearanceSearchResult> itemList = result.ToList();
            ViewBag.TotalRecords = itemList.Count();
            var pagedList = itemList.OrderByDescending(v => v.CreatedDate).ToPagedList(pageNumber, pageSize);


            if (ViewBag.FORCE_TYPE_CODE == 2)
            {
                pagedList = itemList.OrderBy(v => v.CreatedDate).ToPagedList(pageNumber, pageSize);
            }

            //if (Request.IsAjaxRequest())
            //{
            //    return PartialView("_BrowsePartial", pagedList);
            //}

            return View(pagedList);
        }
        public ActionResult Browse(
            string type, string tab, string permits)
        {
            
            int access = Convert.ToInt32(type);
           
           
            ViewBag.activetab = "";
            ViewBag.IconTab = "";

           
           
            switch (tab)
            {
                case "NewPermits":
                    ViewBag.activetab = "NewPermits";
                    ViewBag.IconTab = "fa fa-download";
                  
                    break;
                case "ToPrint":
                    ViewBag.activetab = "ToPrint";
                    ViewBag.IconTab = "fa fa-upload";
                 
                    break;
                case "printed_permits":
                    ViewBag.activetab = "printed_permits";
                    ViewBag.IconTab = "fa fa-print";
                  
                    break;
                case "AuthoPermits":
                    ViewBag.activetab = "AuthoPermits";
                    ViewBag.IconTab = "fa fa-download";
                   

                    break;
                case "AllPermits":
                    ViewBag.activetab = "AllPermits";
                    ViewBag.IconTab = "fa fa-globe";
                   
                    break;
            }

           

           
            return View();
        }


        public ActionResult CompaniesBrowse(
           string type, string tab, string mergedSearch,
           int? page)
        {
            ViewBag.PermitMainType = type;
            int access = Convert.ToInt32(type);
            List<ClearanceSearchResult> result = new List<ClearanceSearchResult>();
            string searchType = "";
            string string_to_search = "";
            if (string.IsNullOrWhiteSpace(mergedSearch))
            {
                searchType = mergedSearch;
            }
            else
            {
                // Split the input string at the underscore
                string[] parts = mergedSearch.Split('_');
                // Return the first part

                if (!parts[1].IsEmpty())
                {
                    string_to_search = parts[1];
                    searchType = parts[0];
                }

            }

            ViewBag.activetab = "";
            ViewBag.IconTab = "";
            ViewBag.CurrentFilter = mergedSearch;
            ViewBag.SearchValue = string_to_search;
            ViewBag.SearchType = searchType;

            int pageSize = 10; // Number of items to display per page
            int pageNumber = page ?? 1; // Default to the first page if no page number is specified
            try
            {
                var new_permits = db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == access &&
                a.WORKFLOW_RESPO_CODE != null && a.RA42_COMPANY_PASS_DTL.DLT_STS != true
                ).Select(r => new ClearanceSearchResult
                {
                    Id = r.PERMIT_CODE,
                    ControllerName = "Permitsdtl",
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
                    ServiceNumber = r.SERVICE_NUMBER,
                    PesronalImage = r.PERSONAL_IMAGE,
                    PurposeOfPass = r.PURPOSE_OF_PASS,
                    MainPurposeOfPass = (r.COMPANY_PASS_CODE != null ? r.RA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS : "-"),
                    Responsipole = (r.COMPANY_PASS_CODE != null ? r.RA42_COMPANY_PASS_DTL.RESPONSIBLE : "-"),
                    Name = r.NAME_A,
                    Rank = r.RANK_A,
                    HosRank = r.HOST_RANK_A,
                    HostName = r.HOST_NAME_A,
                    Phone = r.PHONE_NUMBER,
                    Gsm = r.GSM,
                    PermitType = r.RA42_CARD_FOR_MST.CARD_FOR_A,
                    CivilNumber = r.CIVIL_NUMBER,
                    IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                    ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                    CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                    UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                    StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                    Status = r.STATUS,
                    Delivered = r.ISDELIVERED,
                    CardFor = r.CARD_FOR_CODE.Value,
                    RegistrationSerial = r.REGISTRATION_SERIAL,
                    Company = (r.COMPANY_CODE != null ? r.RA42_COMPANY_MST.COMPANY_NAME : " "),
                    EventExercise = (r.EVENT_EXERCISE_CODE != null ? r.RA42_EVENT_EXERCISE_MST.EVENT_EXERCISE_NAME : " "),
                    Printed = r.ISPRINTED,
                    Opened = r.ISOPENED,
                    Rejected = r.REJECTED,
                    Returned = r.RETURNED,
                    MainPrinted = (r.COMPANY_PASS_CODE != null ? r.RA42_COMPANY_PASS_DTL.ISPRINTED : null),
                    MainOpened = (r.COMPANY_PASS_CODE != null ? r.RA42_COMPANY_PASS_DTL.ISOPENED : null),
                    MainRejected = (r.COMPANY_PASS_CODE != null ? r.RA42_COMPANY_PASS_DTL.REJECTED : null),
                    MainStatus = (r.COMPANY_PASS_CODE != null ? r.RA42_COMPANY_PASS_DTL.STATUS : null),
                    PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                    Deleted = r.DLT_STS,
                    Station = r.STATION_CODE.Value,
                    Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                    ResponsipoleServiceNumber = r.RESPONSIBLE,
                    CarColor = (r.VECHILE_COLOR_CODE != null ? r.RA42_VECHILE_COLOR_MST.COLOR : "-"),
                    PlateNumber = (r.PLATE_NUMBER != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR + "-" + r.PLATE_NUMBER : "-"),
                    PlateCode = (r.PLATE_CHAR_CODE != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR : "-"),
                    CarType = (r.VECHILE_NAME_CODE != null ? r.RA42_VECHILE_NAME_MST.VECHILE_NAME : "-"),
                    CarName = (r.VECHILE_CODE != null ? r.RA42_VECHILE_CATIGORY_MST.VECHILE_CAT : "-"),
                    WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                    Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                    WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                    Violations = 0,
                    CompanyPermitId = (r.COMPANY_PASS_CODE != null ? r.COMPANY_PASS_CODE.Value : 0),
                    Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == r.COMPANY_PASS_CODE && x.DLT_STS != true).Count(),

                }).AsQueryable();
                result.AddRange(new_permits);
            }
            catch
            {

            }

            try
            {
                var companies_temp = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true
                && a.RA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE != null && a.RA42_COMPANY_PASS_DTL.DLT_STS != true

                ).Select(r => new ClearanceSearchResult
                {
                    Id = r.TEMPRORY_COMPANY_PASS_CODE,
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
                    CardFor = r.RA42_COMPANY_PASS_DTL.CARD_FOR_CODE.Value,
                    ControllerName = "Temprorycompanypass",
                    ServiceNumber = r.RA42_COMPANY_PASS_DTL.SERVICE_NUMBER,
                    PesronalImage = r.PERSONAL_IMAGE,
                    PurposeOfPass = r.RA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS,
                    Responsipole = (r.COMPANY_PASS_CODE != null ? r.RA42_COMPANY_PASS_DTL.RESPONSIBLE : "-"),
                    MainPurposeOfPass = (r.COMPANY_PASS_CODE != null ? r.RA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS : "-"),
                    Name = r.NAME_A,
                    Gsm = r.GSM,
                    PermitType = "تصريح الشركات" + " - بدون مركبة",
                    Company = r.RA42_COMPANY_PASS_DTL.RA42_COMPANY_MST.COMPANY_NAME,
                    PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                    CivilNumber = r.ID_CARD_NUMBER,
                    Printed = r.ISPRINTED,
                    Delivered = r.ISDELIVERED,
                    Rejected = r.RA42_COMPANY_PASS_DTL.REJECTED,
                    Returned = r.RETURNED,
                    Opened = r.RA42_COMPANY_PASS_DTL.ISOPENED,
                    Deleted = r.RA42_PASS_TYPE_MST.DLT_STS,
                    IssueingDate = r.DATE_FROM.ToString(),
                    ExpiredDate = r.DATE_TO.Value,
                    CreatedDate = r.CRD_DT.Value,
                    UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                    CarColor = "-",
                    PlateNumber = "-",
                    PlateCode = "-",
                    CarType = "-",
                    CarName = "-",
                    MainPrinted = r.RA42_COMPANY_PASS_DTL.ISPRINTED,
                    MainOpened = r.RA42_COMPANY_PASS_DTL.ISOPENED,
                    MainRejected = r.RA42_COMPANY_PASS_DTL.REJECTED,
                    MainStatus = r.RA42_COMPANY_PASS_DTL.STATUS,
                    StationName = r.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.STATION_NAME_A,
                    Station = r.RA42_COMPANY_PASS_DTL.STATION_CODE.Value,
                    Force = r.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID.Value,
                    Status = r.RA42_COMPANY_PASS_DTL.STATUS,
                    ResponsipoleServiceNumber = r.RA42_COMPANY_PASS_DTL.SERVICE_NUMBER,
                    Workflow = r.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                    WorkflowId = r.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                    Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.DLT_STS != true && x.PASS_ROW_CODE == r.COMPANY_PASS_CODE).Count(),
                    CompanyPermitId = r.COMPANY_PASS_CODE.Value,
                    Violations = 0



                }).AsQueryable();
                result.AddRange(companies_temp);

            }
            catch
            {

            }


            try
            {
                switch (searchType)
                {
                    case "ServiceNumber":
                        result = result.Where(v => v.ServiceNumber == string_to_search).ToList();
                        break;
                    case "CivilNumber":
                        result = result.Where(v => v.CivilNumber == string_to_search).ToList();
                        break;
                    case "Name":
                        result = result.Where(v => v.Name !=null && v.Name.Contains(string_to_search)).ToList();
                        break;
                    case "PlateNumber":
                        result = result.Where(v => v.PlateNumber == string_to_search).ToList();
                        break;
                    case "kafeel":
                        result = result.Where(v => v.ResponsipoleServiceNumber == string_to_search).ToList();
                        break;
                    case "Phone":
                        result = result.Where(v => v.Gsm == string_to_search || v.Phone == string_to_search).ToList();
                        break;
                    case "Company":
                        result = result.Where(v => v.Company !=null &&  v.Company.Contains(string_to_search)).ToList();
                        break;
                    case "Station":
                        result = result.Where(v => v.StationName != null && v.StationName.Contains(string_to_search)).ToList();
                        break;
                }

            }
            catch
            {

            }

            DateTime threeYearsAgo = DateTime.Now.AddYears(-3);
            DateTime overThreeMonth = DateTime.Now.AddMonths(-3);

            switch (tab)
            {
                case "NewPermits":
                    ViewBag.activetab = "NewPermits";
                    ViewBag.IconTab = "fa fa-download";
                    result = result.Where(r => r.Station == STATION_CODE && r.WorkflowId == WORKFLOWID && r.Printed != true && r.Status != true && r.CreatedDate >= threeYearsAgo)
                               .OrderByDescending(a => a.CompanyPermitId).ToList();
                    break;
                case "ToPrint":
                    ViewBag.activetab = "ToPrint";
                    ViewBag.IconTab = "fa fa-upload";
                    if (ViewBag.FORCE_TYPE_CODE == 3)
                    {
                        result = result.Where(r => r.Station == STATION_CODE && r.WorkflowId == WORKFLOWID && r.Printed != true && r.Status == true && r.UpdatedDate >= overThreeMonth)
                               .OrderByDescending(a => a.CompanyPermitId).ToList();
                    }
                    else
                    {
                        result = result.Where(r => r.Station == STATION_CODE && r.WorkflowId == WORKFLOWID && r.Printed != true && r.Status == true && r.CreatedDate >= threeYearsAgo)
                              .OrderByDescending(a => a.CompanyPermitId).ToList();
                    }
                    break;
                case "printed_permits":
                    ViewBag.activetab = "printed_permits";
                    ViewBag.IconTab = "fa fa-print";
                    result = result.Where(r => r.Station == STATION_CODE && r.WorkflowId == WORKFLOWID && r.Printed == true && r.Status == true && r.CreatedDate >= threeYearsAgo)
                               .OrderByDescending(a => a.CompanyPermitId).ToList();
                    break;
                case "AuthoPermits":
                    ViewBag.activetab = "AuthoPermits";
                    ViewBag.IconTab = "fa fa-download";
                    result = result.Where(r => r.WorkflowServiceNumber == currentUser && r.CreatedDate >= threeYearsAgo).OrderByDescending(a => a.CompanyPermitId).ToList();

                    break;
                case "AllPermits":
                    ViewBag.activetab = "AllPermits";
                    ViewBag.IconTab = "fa fa-globe";
                    if (ViewBag.ADMIN == true)
                    {
                        result = result.Where(r => r.Station == STATION_CODE && r.CreatedDate >= threeYearsAgo).OrderByDescending(a => a.CompanyPermitId).ToList();

                    }
                    if (ViewBag.DEVELOPER == true)
                    {
                        result = result.OrderByDescending(a => a.CompanyPermitId).ToList();
                    }
                    break;
            }

            // Group results by CompanyPermitId
            var groupedResult = result.GroupBy(r => new { r.CompanyPermitId, r.Company, r.ServiceNumber,
                r.MainOpened,r.MainPrinted,r.MainRejected,r.MainStatus,r.MainPurposeOfPass,r.ControllerName
            ,r.Comments,r.Responsipole})
                                      .Select(g => new GroupedClearanceSearchResult
                                      {
                                          CompanyPermitId = g.Key.CompanyPermitId,
                                          Company = g.Key.Company,
                                          ServiceNumber = g.Key.ServiceNumber,
                                          MainOpened = g.Key.MainOpened,
                                          MainPrinted = g.Key.MainPrinted,
                                          MainRejected = g.Key.MainRejected,
                                          MainStatus = g.Key.MainStatus,
                                          MainPurposeOfPass = g.Key.MainPurposeOfPass,
                                          ControllerName = g.Key.ControllerName,
                                          Comments = g.Key.Comments,
                                          Responsipole = g.Key.Responsipole,
                                          Items = g.ToList()
                                      })
                                      .ToList();

            ViewBag.TotalRecords = groupedResult.Count();
            var pagedList = new PagedList<GroupedClearanceSearchResult>(groupedResult, pageNumber, pageSize);

          

            //if (ViewBag.FORCE_TYPE_CODE == 2)
            //{
            //    pagedList = new PagedList<GroupedClearanceSearchResult>(groupedResult.OrderBy(g => g.Items.First().CreatedDate).ToList(), pageNumber, pageSize);
            //}


            return View(pagedList);
        }

        [HttpGet]
        public ActionResult Comments(int? id,string tab,string access)
        {
            ViewBag.activetab = tab;

            if (id == null)
            {
                return NotFound();
            }
            
            //check if the record is in the db table 
            RA42_PERMITS_DTL ra42_PERMITS_DTL = db.RA42_PERMITS_DTL.Find(id);
            if (ra42_PERMITS_DTL == null)
            {
                return NotFound();
            }
            List<string> RnoVisitors = new List<string> { "11", "12", "13", "14", "16" };

            if (RnoVisitors.Contains(ra42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
            {
                ViewBag.Managepasses = "";
                ViewBag.RnoTempPermits = "RnoTempPermits";
            }
            //check if the current user has authority to view this comments 

            if (ViewBag.RESPO_STATE <= 1)
            {
                if (ra42_PERMITS_DTL.SERVICE_NUMBER != currentUser && ra42_PERMITS_DTL.RESPONSIBLE != currentUser)
                {
                    if (ra42_PERMITS_DTL.ISOPENED != true)
                    {
                       

                            return NotFound();
                        
                    }
                }
            }
            else
            {
                if (ra42_PERMITS_DTL.SERVICE_NUMBER == currentUser || ra42_PERMITS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != ra42_PERMITS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                    {
                       
                            return NotFound();
                        
                    }
                }
            }

            int permit_code = ra42_PERMITS_DTL.PERMIT_CODE;
            if(ra42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5)
            {
                permit_code = ra42_PERMITS_DTL.COMPANY_PASS_CODE.Value;
            }
          
                var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == permit_code && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ra42_PERMITS_DTL.ACCESS_TYPE_CODE && a.CRD_DT > new DateTime(2024, 7, 1)).ToList();
                //get comments of the current permit 
                ViewBag.COMMENTS = cOMMENTS;
            
           
            return View(ra42_PERMITS_DTL);
        }

        // POST new comment 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Comments(RA42_PERMITS_DTL ra42_PERMITS_DTL, string COMMENT,string tab)
        {
            ViewBag.activetab = tab;
            var rA42_PERMITS_DTL = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == ra42_PERMITS_DTL.PERMIT_CODE).FirstOrDefault();
            int permit_code = ra42_PERMITS_DTL.PERMIT_CODE;
            if (rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5)
            {
                permit_code = rA42_PERMITS_DTL.COMPANY_PASS_CODE.Value;
            }

            List<string> RnoVisitors = new List<string> { "11", "12", "13", "14", "16" };

            if (RnoVisitors.Contains(rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
            {
                ViewBag.Managepasses = "";
                ViewBag.RnoTempPermits = "RnoTempPermits";
            }

            //add comments
            if (COMMENT.Length > 0)
            {
                RA42_COMMENTS_MST rA42_COMMENT = new RA42_COMMENTS_MST();
                rA42_COMMENT.ACCESS_TYPE_CODE = rA42_PERMITS_DTL.ACCESS_TYPE_CODE;
                rA42_COMMENT.PASS_ROW_CODE = permit_code;
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
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == permit_code && a.DLT_STS != true && a.ACCESS_TYPE_CODE == rA42_PERMITS_DTL.ACCESS_TYPE_CODE && a.CRD_DT > new DateTime(2024, 7, 1)).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            return View(rA42_PERMITS_DTL);


        }

        // details view of any company permit 
        [HttpGet]
        public ActionResult CompanyPermitDetails(int? id, string tab)
        {
            ViewBag.activetab = tab;

            if (id == null)
            {
                return NotFound();
            }
            //check if the record id is in the db table 
            RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL = db.RA42_COMPANY_PASS_DTL.Find(id);
            if (rA42_COMPANY_PASS_DTL == null)
            {
                return NotFound();
            }
           
            //check if the current user has authority to view details 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_COMPANY_PASS_DTL.SERVICE_NUMBER != currentUser)
                {

                    return NotFound();

                }
            }

            //get employees of this permit 
            ViewBag.GetEmployees = db.RA42_PERMITS_DTL.Where(a => a.COMPANY_PASS_CODE == id && a.DLT_STS != true).ToList();
            foreach(var item in ViewBag.GetEmployees)
            {
                ViewBag.Permit_code_emp = item.PERMIT_CODE;
            }
            //get selected zones and gates
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE ).ToList();
            //get selected documenst 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CONTROLLER_NAME.Equals("Companies")).ToList();
            //get comments of the request
            var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).ToList();
            ViewBag.COMMENTS = cOMMENTS;
            
            return View(rA42_COMPANY_PASS_DTL);
        }

        // details view of any permit 
        [HttpGet]
        public ActionResult Details(int? id,string tab)
        {
            ViewBag.activetab = tab;

            if (id == null)
            {
                return NotFound();
            }
            //check if the record id is in the db table 
            RA42_PERMITS_DTL rA42_PERMITS_DTL = db.RA42_PERMITS_DTL.Find(id);
            if (rA42_PERMITS_DTL == null)
            {
                return NotFound();
            }
            var RnoVisitors = new List<string>() { "11", "12", "13", "14", "16" };
            if (RnoVisitors.Contains(rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
            {
                ViewBag.Managepasses = "";
                ViewBag.RnoTempPermits = "RnoTempPermits";
            }
            //check if the current user has authority to view details 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_PERMITS_DTL.SERVICE_NUMBER != currentUser && rA42_PERMITS_DTL.RESPONSIBLE != currentUser)
                {
                   
                        return NotFound();
                    
                }
            }

            //get relatives of this permit 
            ViewBag.GetRelativs =  db.RA42_MEMBERS_DTL.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.CRD_DT > new DateTime(2024, 8, 1)).ToList();
            //get selected zones and gates
            if (rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE != 5)
            {
                ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToList();
            }
            else
            {
                if (rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "25")
                {
                    ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToList();

                }
                else
                {
                    ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToList();
                }
            }
            //get selected documenst 
            ViewBag.GetFiles =  db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1) && !a.CONTROLLER_NAME.Equals("Companies")).ToList();
            //get comments of the request
            if (rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE != 5)
            {
                var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToList();
                ViewBag.COMMENTS = cOMMENTS;
            }
            else
            {
                var cOMMENTS = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_PERMITS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).ToList();
                ViewBag.COMMENTS = cOMMENTS;
            }
            return View(rA42_PERMITS_DTL);
        }

        //function to gets all car types for specific catigory (أنواع السيارات صالون - دفع رباعي - شاحنة - دراجة) as json result 
        [HttpGet]
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
        [HttpGet]
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
        //for normal permits
        [HttpGet]
        public async Task<ActionResult> PrintAllCards(string selectedIds)
        {
            ViewBag.activetab = "ToPrint";
            if (string.IsNullOrEmpty(selectedIds))
            {
                return NotFound();
            }
            ViewBag.SelectedIds = selectedIds;
            // Split the comma-separated string into a list of IDs
            List<int> ids = selectedIds.Split(',').Select(int.Parse).ToList();
            List<RA42_PERMITS_DTL> employees = new List<RA42_PERMITS_DTL>();
            List<RA42_ZONE_MASTER_MST> zones = new List<RA42_ZONE_MASTER_MST>();
            RA42_PERMITS_DTL rA42_PERMITS_DTL = new RA42_PERMITS_DTL();
            // Process the selected IDs
            foreach (var id in ids)
            {
                //get the permit 
                var emp = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == id).FirstOrDefault();
                // Add to the list if not null
                if (emp != null)
                {
                    employees.Add(emp);
                    var zone =  db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == emp.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 10, 1)).ToList();
                    if (zone.Count() > 0)
                    {
                        foreach (var z in zone)
                        {
                            zones.Add(z);
                        }
                    }
                    rA42_PERMITS_DTL = emp;
                }

            }

            //check if the current user has authority to view this permit 
            if (ViewBag.RESPO_STATE != rA42_PERMITS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
            {
                if (ViewBag.ADMIN != true)
                {
                    if (ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }
            ViewBag.checkViolations = false;
            RA42_PERMITS_DTL employee = new RA42_PERMITS_DTL();
            
            foreach (var emp in employees)
            {
                var violation = (from a in db.RA42_VECHILE_VIOLATION_DTL
                                           join b in db.RA42_PERMITS_DTL on a.ACCESS_ROW_CODE equals b.PERMIT_CODE
                                           where a.DLT_STS != true && (b.PLATE_NUMBER == emp.PLATE_NUMBER && b.PLATE_CHAR_CODE == emp.PLATE_CHAR_CODE) || (b.SERVICE_NUMBER == emp.SERVICE_NUMBER.ToUpper() || b.CIVIL_NUMBER == emp.CIVIL_NUMBER) && a.PREVENT == true && b.STATION_CODE == emp.STATION_CODE
                                           select a).Any();
                if(violation == true)
                {
                    ViewBag.checkViolations = true;
                    employee = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == emp.PERMIT_CODE).FirstOrDefault();
                } 
            }
            if(employee != null) {
                ViewBag.EmployeeViolation = employee;

            }
            else
            {
                ViewBag.EmployeeViolation = null;
            }
            ViewBag.GetZones = zones.ToList();
            ViewBag.GetEmployees = employees.ToList();

            if (Language.GetCurrentLang() == "en")
            {
                var types = await db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_PERMITS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToListAsync();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = await db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_PERMITS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToListAsync();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }

            return View(rA42_PERMITS_DTL);
        }

        [HttpPost]
        public async Task<ActionResult> PrintAllCards(string CheckPrinted, int TRANSACTION_TYPE_CODE, string TRANSACTION_REMARKS,
           HttpPostedFileBase RECEIPT, string selectedIds, string tab)
        {
            ViewBag.activetab = tab;

            ViewBag.SelectedIds = selectedIds;
            // Split the comma-separated string into a list of IDs
            List<int> ids = selectedIds.Split(',').Select(int.Parse).ToList();
            List<RA42_PERMITS_DTL> employees = new List<RA42_PERMITS_DTL>();
            List<RA42_ZONE_MASTER_MST> zones = new List<RA42_ZONE_MASTER_MST>();
            RA42_PERMITS_DTL rA42_PERMITS_DTL = new RA42_PERMITS_DTL();
            // Process the selected IDs
            foreach (var id in ids)
            {
                //get the permit 
                var emp = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == id).FirstOrDefault();
                // Add to the list if not null
                if (emp != null)
                {
                    employees.Add(emp);
                    var zone = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == emp.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 10, 1)).ToList();
                    if (zone.Count() > 0)
                    {
                        foreach (var z in zone)
                        {
                            zones.Add(z);
                        }
                    }
                    rA42_PERMITS_DTL = emp;
                }

            }

            //check if the current user has authority to view this permit 
            if (ViewBag.RESPO_STATE != rA42_PERMITS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
            {
                if (ViewBag.ADMIN != true)
                {
                    if (ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }
            ViewBag.checkViolations = false;
            RA42_PERMITS_DTL employee = new RA42_PERMITS_DTL();

            foreach (var emp in employees)
            {
                var violation = (from a in db.RA42_VECHILE_VIOLATION_DTL
                                 join b in db.RA42_PERMITS_DTL on a.ACCESS_ROW_CODE equals b.PERMIT_CODE
                                 where a.DLT_STS != true && ((b.PLATE_NUMBER !=null && b.PLATE_NUMBER == emp.PLATE_NUMBER) && (b.PLATE_CHAR_CODE !=null && b.PLATE_CHAR_CODE == emp.PLATE_CHAR_CODE)) || ((b.SERVICE_NUMBER !=null && b.SERVICE_NUMBER == emp.SERVICE_NUMBER.ToUpper()) || (b.CIVIL_NUMBER != null && b.CIVIL_NUMBER == emp.CIVIL_NUMBER)) && a.PREVENT == true && b.STATION_CODE == emp.STATION_CODE
                                 select a).Any();
                if (violation == true)
                {
                    ViewBag.checkViolations = true;
                    employee = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == emp.PERMIT_CODE).FirstOrDefault();
                }
            }
            if (employee != null)
            {
                ViewBag.EmployeeViolation = employee;

            }
            else
            {
                ViewBag.EmployeeViolation = null;
            }
            ViewBag.GetZones = zones.ToList();
            ViewBag.GetEmployees = employees.ToList();

            if (Language.GetCurrentLang() == "en")
            {
                var types = await db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_PERMITS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToListAsync();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = await db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_PERMITS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToListAsync();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }

            
            

            foreach (var trans in employees)
            {
                RA42_TRANSACTION_DTL rA42_TRANSACTION_DTL = new RA42_TRANSACTION_DTL();
                rA42_TRANSACTION_DTL.ACCESS_ROW_CODE = trans.PERMIT_CODE;
                rA42_TRANSACTION_DTL.ACCESS_TYPE_CODE = trans.ACCESS_TYPE_CODE;
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

                                fileName = "Receipt_" + trans.ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                string path = Path.Combine(Server.MapPath("~/Files/Receipt/"), fileName);
                                RECEIPT.SaveAs(path);
                                rA42_TRANSACTION_DTL.RECEIPT = fileName;


                            }
                            else
                            {
                                //throw error if image file extention not supported 

                                TempData["Erorr"] = "صيغة الملف غير مدعومة - File formate not supported";
                                return View(rA42_PERMITS_DTL);
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

                if (CheckPrinted.Equals("Printed"))
                {
                    var deletePrinted = await db.RA42_PRINT_MST.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == trans.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.PASS_ROW_CODE ==
                    trans.PERMIT_CODE).ToListAsync();
                    if (deletePrinted.Count > 0)
                    {
                        foreach (var item in deletePrinted)
                        {
                            item.DLT_STS = true;
                            db.SaveChanges();
                        }
                    }
                }
                trans.UPD_BY = currentUser;
                trans.UPD_DT = DateTime.Now;
                trans.ISPRINTED = true;
                db.SaveChanges();
            }

            TempData["Success"] = "تم تحديث المعاملة بنجاح";

            return View(rA42_PERMITS_DTL);
        }


        //to print companies employees
        [HttpGet]
        public async Task<ActionResult> PrintAll(int? id, string tab)
        {
            ViewBag.activetab = tab;

            if (id == null)
            {
                return NotFound();
            }
            //check if the record is in the db table 
            RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL = db.RA42_COMPANY_PASS_DTL.Find(id);
            if (rA42_COMPANY_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if the current user has authority to view this permit 
            if (ViewBag.RESPO_STATE != rA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
            {
                if (ViewBag.ADMIN != true)
                {
                    if (ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }

            //get relatives of the permit 
            ViewBag.GetEmployees = await db.RA42_PERMITS_DTL.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.COMPANY_PASS_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true).ToListAsync();
            //get zones and gates selected for the current permit 
            ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 10, 1)).ToListAsync();
             //get documents selected for the current permit 
            ViewBag.GetFiles = await db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CONTROLLER_NAME.Equals("Companies")).ToListAsync();
           var emp = db.RA42_PERMITS_DTL.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.COMPANY_PASS_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true).FirstOrDefault();
            if (emp != null)
            {
                ViewBag.Permit_code_emp = emp.PERMIT_CODE;
            }

            if (Language.GetCurrentLang() == "en")
            {
                var types = await db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToListAsync();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = await db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToListAsync();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }
            return View(rA42_COMPANY_PASS_DTL);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PrintAll(string CheckPrinted, int TRANSACTION_TYPE_CODE, string TRANSACTION_REMARKS,
            HttpPostedFileBase RECEIPT, RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL, int? id, string tab)
        {
            ViewBag.activetab = tab;


            //check if the record is in the db table 
            RA42_COMPANY_PASS_DTL _rA42_COMPANY_PASS_DTL = db.RA42_COMPANY_PASS_DTL.Find(id);
            if (_rA42_COMPANY_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if the current user has authority to view this permit 
            if (ViewBag.RESPO_STATE != _rA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
            {
                if (ViewBag.ADMIN != true)
                {
                    if (ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }

            //get relatives of the permit 
            ViewBag.GetEmployees = await db.RA42_PERMITS_DTL.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == _rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.COMPANY_PASS_CODE == _rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true).ToListAsync();
            //get zones and gates selected for the current permit 
            ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == _rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == _rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 10, 1)).ToListAsync();
            //get documents selected for the current permit 
            ViewBag.GetFiles = await db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == _rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == _rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CONTROLLER_NAME.Equals("Companies")).ToListAsync();
            var emp = db.RA42_PERMITS_DTL.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == _rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.COMPANY_PASS_CODE == _rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true).FirstOrDefault();
            if (emp != null)
            {
                ViewBag.Permit_code_emp = emp.PERMIT_CODE;
            }

            if (Language.GetCurrentLang() == "en")
            {
                var types = await db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == _rA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToListAsync();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types = await db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == _rA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToListAsync();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }
            foreach (var trans in ViewBag.GetEmployees)
            {
                RA42_TRANSACTION_DTL rA42_TRANSACTION_DTL = new RA42_TRANSACTION_DTL();
                rA42_TRANSACTION_DTL.ACCESS_ROW_CODE = trans.PERMIT_CODE;
                rA42_TRANSACTION_DTL.ACCESS_TYPE_CODE = _rA42_COMPANY_PASS_DTL.ACCESS_TYPE_CODE;
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

                                fileName = "Receipt_" + _rA42_COMPANY_PASS_DTL.ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Profiles/ folder
                                string path = Path.Combine(Server.MapPath("~/Files/Receipt/"), fileName);
                                RECEIPT.SaveAs(path);
                                rA42_TRANSACTION_DTL.RECEIPT = fileName;


                            }
                            else
                            {
                                //throw error if image file extention not supported 

                                TempData["Erorr"] = "صيغة الملف غير مدعومة - File formate not supported";
                                return View(_rA42_COMPANY_PASS_DTL);
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

                if (CheckPrinted.Equals("Printed"))
                {
                    var deletePrinted = await db.RA42_PRINT_MST.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 
                    _rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.PASS_ROW_CODE == rA42_TRANSACTION_DTL.ACCESS_ROW_CODE).ToListAsync();
                    if (deletePrinted.Count > 0)
                    {
                        foreach (var item in deletePrinted)
                        {
                            item.DLT_STS = true;
                            db.SaveChanges();
                        }
                    }
                }
                trans.UPD_BY = currentUser;
                trans.UPD_DT = DateTime.Now;
                trans.ISPRINTED = true;
                db.SaveChanges();
            }

            _rA42_COMPANY_PASS_DTL.UPD_BY = currentUser;
            _rA42_COMPANY_PASS_DTL.UPD_DT = DateTime.Now;
            _rA42_COMPANY_PASS_DTL.ISPRINTED = true;
            db.SaveChanges();
            TempData["Success"] = "تم تحديث المعاملة بنجاح";
            
            return RedirectToAction("PrintAll", new { id = id, tab = tab });
        }

        //this is card view to print card or temprory form or save them 
        [HttpGet]
        public ActionResult Card(int? id,string tab)
        {
            ViewBag.activetab = tab;

            if (id == null)
            {
                return NotFound();
            }
            //check if the record is in the db table 
            RA42_PERMITS_DTL rA42_PERMITS_DTL = db.RA42_PERMITS_DTL.Find(id);
            if (rA42_PERMITS_DTL == null)
            {
                return NotFound();
            }
            //check if the current user has authority to view this permit 
            if (ViewBag.RESPO_STATE != rA42_PERMITS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
            {
                if (ViewBag.ADMIN != true)
                {
                    if (ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }

            //get relatives of the permit 
            ViewBag.GetRelativs =  db.RA42_MEMBERS_DTL.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.CRD_DT > new DateTime(2024,10, 10)).ToList();
            //get zones and gates selected for the current permit 
            if (rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE != 5)
            {
                ViewBag.GetZones =  db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 10, 1)).ToList();
            }
            else
            {
                if (rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "25")
                {
                    ViewBag.GetZones =  db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 08, 1)).ToList();

                }
                else
                {
                    ViewBag.GetZones =  db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 10, 1)).ToList();
                }
            }            //get documents selected for the current permit 
            ViewBag.GetFiles =  db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 10, 1)).ToList();

            ViewBag.checkViolations = (from a in db.RA42_VECHILE_VIOLATION_DTL
                                   join b in db.RA42_PERMITS_DTL on a.ACCESS_ROW_CODE equals b.PERMIT_CODE
                                   where a.DLT_STS != true && ((b.PLATE_NUMBER !=null && b.PLATE_NUMBER == rA42_PERMITS_DTL.PLATE_NUMBER) && (b.PLATE_CHAR_CODE !=null && b.PLATE_CHAR_CODE == rA42_PERMITS_DTL.PLATE_CHAR_CODE)) || ((b.SERVICE_NUMBER != null && b.SERVICE_NUMBER == rA42_PERMITS_DTL.SERVICE_NUMBER.ToUpper()) 
                                   || (b.CIVIL_NUMBER !=null && b.CIVIL_NUMBER == rA42_PERMITS_DTL.CIVIL_NUMBER)) && a.PREVENT == true && b.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE
                                       select  a).Any();
            if (Language.GetCurrentLang() == "en")
            {
                var types =  db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_PERMITS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToList();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types =  db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == rA42_PERMITS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToList();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }
            return View(rA42_PERMITS_DTL);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Card(string CheckPrinted, int TRANSACTION_TYPE_CODE, string TRANSACTION_REMARKS,
            HttpPostedFileBase RECEIPT, RA42_PERMITS_DTL rA42_PERMITS_DTL, int?id, string tab)
        {
            ViewBag.activetab = tab;


            //check if the record is in the db table 
            RA42_PERMITS_DTL _rA42_PERMITS_DTL = db.RA42_PERMITS_DTL.Find(rA42_PERMITS_DTL.PERMIT_CODE);
            if (_rA42_PERMITS_DTL == null)
            {
                return NotFound();
            }
            //check if the current user has authority to view this permit 
            if (ViewBag.RESPO_STATE != _rA42_PERMITS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
            {
                if (ViewBag.ADMIN != true)
                {
                    if (ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }

            //get relatives of the permit 
            ViewBag.GetRelativs =  db.RA42_MEMBERS_DTL.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == _rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.ACCESS_ROW_CODE == _rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.CRD_DT > new DateTime(2024, 10, 10)).ToList();
            //get zones and gates selected for the current permit 
            if (_rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE != 5)
            {
                ViewBag.GetZones =  db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == _rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == _rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 07, 1)).ToList();
            }
            else
            {
                if (_rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "25")
                {
                    ViewBag.GetZones =  db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == _rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == _rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 08, 1)).ToList();

                }
                else
                {
                    ViewBag.GetZones =  db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == _rA42_PERMITS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == _rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 10, 1)).ToList();
                }
            }            //get documents selected for the current permit 
            ViewBag.GetFiles =  db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == _rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == _rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 10, 1) && !a.CONTROLLER_NAME.Equals("Companies")).ToList();

            ViewBag.checkViolations = (from a in db.RA42_VECHILE_VIOLATION_DTL
                                       join b in db.RA42_PERMITS_DTL on a.ACCESS_ROW_CODE equals b.PERMIT_CODE
                                       where a.DLT_STS != true && ((b.PLATE_NUMBER != null && b.PLATE_NUMBER == rA42_PERMITS_DTL.PLATE_NUMBER) && (b.PLATE_CHAR_CODE != null && b.PLATE_CHAR_CODE == rA42_PERMITS_DTL.PLATE_CHAR_CODE)) || ((b.SERVICE_NUMBER != null && b.SERVICE_NUMBER == rA42_PERMITS_DTL.SERVICE_NUMBER.ToUpper()) || (b.CIVIL_NUMBER != null && b.CIVIL_NUMBER == rA42_PERMITS_DTL.CIVIL_NUMBER))
                                       && a.PREVENT == true && b.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE
                                       select a).Any();
            if (Language.GetCurrentLang() == "en")
            {
                var types =  db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == _rA42_PERMITS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_E + " - Amount: " + s.AMOUNT + " R.O" }).ToList();
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");

            }
            else
            {
                var types =  db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == _rA42_PERMITS_DTL.RA42_STATIONS_MST.FORCE_ID).Select(s => new { TRANSACTION_TYPE_CODE = s.TRANSACTION_TYPE_CODE, TRANSACTION_NAME = s.TRANSACTION_NAME_A + " - السعر: " + s.AMOUNT + " ريال" }).ToList();

                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(types, "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME");
            }
            RA42_TRANSACTION_DTL rA42_TRANSACTION_DTL = new RA42_TRANSACTION_DTL();
            rA42_TRANSACTION_DTL.ACCESS_ROW_CODE = _rA42_PERMITS_DTL.PERMIT_CODE;
            rA42_TRANSACTION_DTL.ACCESS_TYPE_CODE = _rA42_PERMITS_DTL.ACCESS_TYPE_CODE;
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

                            fileName = "Receipt_" + _rA42_PERMITS_DTL.ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                            // store the file inside ~/Files/Profiles/ folder
                            string path = Path.Combine(Server.MapPath("~/Files/Receipt/"), fileName);
                            RECEIPT.SaveAs(path);
                            rA42_TRANSACTION_DTL.RECEIPT = fileName;


                        }
                        else
                        {
                            //throw error if image file extention not supported 

                            TempData["Erorr"] = "صيغة الملف غير مدعومة - File formate not supported";
                            return View(_rA42_PERMITS_DTL);
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

            _rA42_PERMITS_DTL.UPD_BY = currentUser;
            _rA42_PERMITS_DTL.UPD_DT = DateTime.Now;
            _rA42_PERMITS_DTL.ISDELIVERED = false;
            db.SaveChanges();
            TempData["Success"] = "تم تحديث المعاملة بنجاح";
            if (CheckPrinted.Equals("Printed"))
            {
                var deletePrinted =  db.RA42_PRINT_MST.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == _rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.PASS_ROW_CODE ==
                _rA42_PERMITS_DTL.PERMIT_CODE).ToList();
                if (deletePrinted.Count > 0)
                {
                    foreach (var item in deletePrinted)
                    {
                        item.DLT_STS = true;
                        db.SaveChanges();
                    }
                }
            }
            return RedirectToAction("Card", new {id=id,tab=tab});
        }
        [HttpGet]
        public async Task<ActionResult> CardTemp(int? id,string tab)
        {
            ViewBag.Managepasses = "";
            ViewBag.RnoTempPermits = "RnoTempPermits";
            ViewBag.activetab = tab;

            if (id == null)
            {
                return NotFound();
            }
            RA42_PERMITS_DTL rA42_PERMITS_DTL = db.RA42_PERMITS_DTL.Find(id);
            if (rA42_PERMITS_DTL == null)
            {
                return NotFound();
            }

            //check if current user has authority to open comments view for this permit 
            if (ViewBag.RESPO_STATE != rA42_PERMITS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
            {
                if (ViewBag.ADMIN != true)
                {
                    if (ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }
            //get relatives of this permit 
            ViewBag.GetRelativs = await db.RA42_MEMBERS_DTL.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.CRD_DT > new DateTime(2024, 07, 1)).ToListAsync();
            //get selected files and zones or gates
            ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 07, 1)).ToListAsync();
            ViewBag.GetFiles = await db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 07, 1) && !a.CONTROLLER_NAME.Equals("Companies")).ToListAsync();
            ViewBag.checkViolations = (from a in db.RA42_VECHILE_VIOLATION_DTL
                                       join b in db.RA42_PERMITS_DTL on a.ACCESS_ROW_CODE equals b.PERMIT_CODE
                                       where a.DLT_STS != true && ((b.PLATE_NUMBER != null && b.PLATE_NUMBER == rA42_PERMITS_DTL.PLATE_NUMBER) && (b.PLATE_CHAR_CODE != null && b.PLATE_CHAR_CODE == rA42_PERMITS_DTL.PLATE_CHAR_CODE)) || ((b.SERVICE_NUMBER != null && b.SERVICE_NUMBER == rA42_PERMITS_DTL.SERVICE_NUMBER.ToUpper()) || (b.CIVIL_NUMBER != null && b.CIVIL_NUMBER == rA42_PERMITS_DTL.CIVIL_NUMBER))
                                       && a.PREVENT == true && b.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE
                                       select a).Any();
            

            return View(rA42_PERMITS_DTL);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CardTemp(RA42_PERMITS_DTL _rA42_PERMITS_DTL, DateTime? ENTER_TIME, DateTime? EXIT_TIME, int? enter_id, DateTime?[] E, DateTime?[] X, int?[] T)
        {
            ViewBag.Managepasses = "";
            ViewBag.RnoTempPermits = "RnoTempPermits";
            RA42_PERMITS_DTL rA42_PERMITS_DTL = db.RA42_PERMITS_DTL.Find(_rA42_PERMITS_DTL.PERMIT_CODE);
            if (rA42_PERMITS_DTL == null)
            {
                return NotFound();
            }

            if (ENTER_TIME.HasValue)
            {
                RA42_ENTER_EXIT_REGISTERATION_DTL rA42_ENTER_EXIT_REGISTERATION_DTL = new RA42_ENTER_EXIT_REGISTERATION_DTL();
                rA42_ENTER_EXIT_REGISTERATION_DTL.ENTER_TIME = ENTER_TIME.Value;
                rA42_ENTER_EXIT_REGISTERATION_DTL.ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE;
                rA42_ENTER_EXIT_REGISTERATION_DTL.ACCESS_TYPE_CODE = rA42_PERMITS_DTL.ACCESS_TYPE_CODE;
                rA42_ENTER_EXIT_REGISTERATION_DTL.CRD_BY = currentUser;
                rA42_ENTER_EXIT_REGISTERATION_DTL.CRD_DT = DateTime.Now;
                db.RA42_ENTER_EXIT_REGISTERATION_DTL.Add(rA42_ENTER_EXIT_REGISTERATION_DTL);
                db.SaveChanges();
                TempData["Success"] = "تمت إضافة الوقت بنجاح";



            }

            if (EXIT_TIME.HasValue)
            {
                var check = db.RA42_ENTER_EXIT_REGISTERATION_DTL.Where(a => a.ENTER_EXIT_TIME_CODE == enter_id).FirstOrDefault();
                if (check != null)
                {
                    check.EXIT_TIME = EXIT_TIME.Value;
                    check.UPD_BY = currentUser;
                    check.UPD_DT = DateTime.Now;
                    db.SaveChanges();
                    TempData["Success"] = "تمت إضافة الوقت بنجاح";

                }
            }

            if (X != null)
            {
                for (int i = 0; i < X.Length; i++)
                {
                    int id_t = T[i].Value;
                    var e = db.RA42_ENTER_EXIT_REGISTERATION_DTL.Where(a => a.ENTER_EXIT_TIME_CODE == id_t && a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE).FirstOrDefault();
                    if (e != null)
                    {
                        e.UPD_BY = currentUser;
                        e.UPD_DT = DateTime.Now;
                        e.EXIT_TIME = X[i];
                        db.SaveChanges();
                    }
                }
            }
            //check if current user has authority to open comments view for this permit 
            if (ViewBag.RESPO_STATE != rA42_PERMITS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
            {
                if (ViewBag.ADMIN != true)
                {
                    if (ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }
            //get relatives of this permit 
            ViewBag.GetRelativs = await db.RA42_MEMBERS_DTL.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.CRD_DT > new DateTime(2024, 07, 1)).ToListAsync();
            //get selected files and zones or gates
            ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 07, 1)).ToListAsync();
            ViewBag.GetFiles = await db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 07, 1) && !a.CONTROLLER_NAME.Equals("Companies")).ToListAsync();
            ViewBag.checkViolations = (from a in db.RA42_VECHILE_VIOLATION_DTL
                                       join b in db.RA42_PERMITS_DTL on a.ACCESS_ROW_CODE equals b.PERMIT_CODE
                                       where a.DLT_STS != true && ((b.PLATE_NUMBER != null && b.PLATE_NUMBER == rA42_PERMITS_DTL.PLATE_NUMBER) && (b.PLATE_CHAR_CODE != null && b.PLATE_CHAR_CODE == rA42_PERMITS_DTL.PLATE_CHAR_CODE)) || ((b.SERVICE_NUMBER != null && b.SERVICE_NUMBER == rA42_PERMITS_DTL.SERVICE_NUMBER.ToUpper()) || (b.CIVIL_NUMBER != null && b.CIVIL_NUMBER == rA42_PERMITS_DTL.CIVIL_NUMBER))
                                       && a.PREVENT == true && b.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE
                                       select a).Any();
           

           

            rA42_PERMITS_DTL.ISPRINTED = true;
            rA42_PERMITS_DTL.ISDELIVERED = true;
            rA42_PERMITS_DTL.UPD_BY = currentUser;
            rA42_PERMITS_DTL.UPD_DT = DateTime.Now;
            rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
            rA42_PERMITS_DTL.PERMIT_SN = currentUser;
            rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
            rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
            db.SaveChanges();
            

            return RedirectToAction("CardTemp", new { id = rA42_PERMITS_DTL.PERMIT_CODE });
        }

        //delete violation
        [HttpGet]
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
        [HttpGet]
        public async Task<ActionResult> Violations(int? id)
        {
            ViewBag.activetab = "Violations";
            if (id == null)
            {
                return NotFound();
            }
            //check if the record id of the permit is in the table 
            RA42_PERMITS_DTL rA42_PERMITS_DTL = db.RA42_PERMITS_DTL.Find(id);
            if (rA42_PERMITS_DTL == null)
            {
                return NotFound();
            }
            //check if the current user has authority to see the violations
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_PERMITS_DTL.SERVICE_NUMBER != currentUser && rA42_PERMITS_DTL.RESPONSIBLE != currentUser)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
            }

            //get violatopn as viewBag
            var vIOLATIONS = await db.RA42_VECHILE_VIOLATION_DTL.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 10, 1)).ToListAsync();
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
            return View(rA42_PERMITS_DTL);
        }
        // POST new violation 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Violations(RA42_PERMITS_DTL rA42_PERMITS_DTL, string VIOLATION_DESC, string VIOLATION_BY, string is_prevent, string VIOLATION_DATE, float VIOLATION_PRICE, int VIOLATION_CODE)
        {
            ViewBag.activetab = "Violations";
            var general_data = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == rA42_PERMITS_DTL.PERMIT_CODE).FirstOrDefault();
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
                    vECHILE_VIOLATION_DTL.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
                    vECHILE_VIOLATION_DTL.ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE;
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
            var vIOLATIONS = db.RA42_VECHILE_VIOLATION_DTL.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == general_data.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 10, 1)).ToList();
            ViewBag.VIOLATIONS = vIOLATIONS;
            return View(general_data);


        }
        [HttpGet]
        public async Task<ActionResult> Archive(int? page, string mergedSearch)
        {
            ViewBag.Managepasses = "";
            ViewBag.activetab = "Archive";
            ViewBag.RnoTempPermits = "RnoTempPermits";

            string searchType = "";
            string string_to_search = "";
            if (string.IsNullOrWhiteSpace(mergedSearch))
            {
                searchType = mergedSearch;
            }
            else
            {
                // Split the input string at the underscore
                string[] parts = mergedSearch.Split('_');
                // Return the first part

                if (!parts[1].IsEmpty())
                {
                    string_to_search = parts[1];
                    searchType = parts[0];
                }

            }
            ViewBag.CurrentFilter = mergedSearch;
            ViewBag.SearchValue = string_to_search;
            ViewBag.SearchType = searchType;
            List<ClearanceSearchResult> result = new List<ClearanceSearchResult>();
            int pageSize = 10; // Number of items to display per page
            int pageNumber = page ?? 1; // Default to the first page if no page number is specified
            var RnoVisitors = new List<string>() { "11", "12", "13", "14", "16" };
            try
            {
                var new_permits = db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.ACCESS_TYPE_CODE == 9 &&
                RnoVisitors.Contains(a.RA42_CARD_FOR_MST.CARD_SECRET_CODE) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID 
                && a.STATION_CODE == STATION_CODE && a.DATE_TO < DateTime.Today
                ).Select(r => new ClearanceSearchResult
                {
                    Id = r.PERMIT_CODE,
                    ControllerName = "Permitsdtl",
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
                    ServiceNumber = r.SERVICE_NUMBER,
                    Responsipole = r.UNIT_A,
                    PesronalImage = r.PERSONAL_IMAGE,
                    PurposeOfPass = r.PURPOSE_OF_PASS,
                    Name = r.NAME_A,
                    Rank = r.RANK_A,
                    HosRank = r.HOST_RANK_A,
                    HostName = r.HOST_NAME_A,
                    Phone = r.PHONE_NUMBER,
                    Gsm = r.GSM,
                    PermitType = r.RA42_CARD_FOR_MST.CARD_FOR_A,
                    CivilNumber = r.CIVIL_NUMBER,
                    IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                    ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                    CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                    StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                    Status = r.STATUS,
                    Delivered = r.ISDELIVERED,
                    CardFor = r.CARD_FOR_CODE.Value,
                    RegistrationSerial = r.REGISTRATION_SERIAL,
                    Company = (r.COMPANY_CODE != null ? r.RA42_COMPANY_MST.COMPANY_NAME : " "),
                    EventExercise = (r.EVENT_EXERCISE_CODE != null ? r.RA42_EVENT_EXERCISE_MST.EVENT_EXERCISE_NAME : " "),
                    Printed = r.ISPRINTED,
                    Opened = r.ISOPENED,
                    Rejected = r.REJECTED,
                    Returned = r.RETURNED,
                    PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                    Deleted = r.DLT_STS,
                    Station = r.STATION_CODE.Value,
                    Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                    ResponsipoleServiceNumber = r.RESPONSIBLE,
                    CarColor = (r.VECHILE_COLOR_CODE != null ? r.RA42_VECHILE_COLOR_MST.COLOR : "-"),
                    PlateNumber = (r.PLATE_NUMBER != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR + "-" + r.PLATE_NUMBER : "-"),
                    PlateCode = (r.PLATE_CHAR_CODE != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR : "-"),
                    CarType = (r.VECHILE_NAME_CODE != null ? r.RA42_VECHILE_NAME_MST.VECHILE_NAME : "-"),
                    CarName = (r.VECHILE_CODE != null ? r.RA42_VECHILE_CATIGORY_MST.VECHILE_CAT : "-"),
                    WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                    Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                    WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                    Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == r.PERMIT_CODE && r.DLT_STS != true).Count(),
                    Violations = r.RA42_ACCESS_TYPE_MST.RA42_VECHILE_VIOLATION_DTL.Where(x => x.DLT_STS != true && x.ACCESS_ROW_CODE == r.PERMIT_CODE).Count(),
                    CompanyPermitId = (r.COMPANY_PASS_CODE != null ? r.COMPANY_PASS_CODE.Value : 0),

                }).AsQueryable();
                result.AddRange(new_permits);
            }
            catch
            {

            }

            try
            {
                var visitors = db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true &&
                RnoVisitors.Contains(a.RA42_CARD_FOR_MST.CARD_SECRET_CODE) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == WORKFLOWID
                && a.STATION_CODE == STATION_CODE && a.DATE_TO < DateTime.Today
                ).Select(r => new ClearanceSearchResult
                {
                    Id = r.VISITOR_PASS_CODE,
                    ControllerName = "Visitorpass",
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
                    ServiceNumber = r.SERVICE_NUMBER,
                    PesronalImage = r.PERSONAL_IMAGE,
                    PurposeOfPass = r.PURPOSE_OF_PASS,
                    Name = r.VISITOR_NAME,
                    Rank = "",
                    HosRank = "",
                    HostName = "",
                    Phone = r.PHONE_NUMBER,
                    Gsm = r.GSM,
                    PermitType = r.RA42_CARD_FOR_MST.CARD_FOR_A,
                    CivilNumber = r.ID_CARD_NUMBER,
                    IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                    ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                    CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                    StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                    Status = r.STATUS,
                    Delivered = r.ISDELIVERED,
                    CardFor = r.CARD_FOR_CODE.Value,
                    Company = (r.COMPANY_CODE != null ? r.RA42_COMPANY_MST.COMPANY_NAME : " "),
                    EventExercise = "",
                    Printed = r.ISPRINTED,
                    Opened = r.ISOPENED,
                    Rejected = r.REJECTED,
                    Returned = r.RETURNED,
                    PassType = "مؤقت",
                    Deleted = r.DLT_STS,
                    Station = r.STATION_CODE.Value,
                    Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                    ResponsipoleServiceNumber = r.RESPONSIBLE,
                    CarColor = (r.VECHILE_COLOR_CODE != null ? r.RA42_VECHILE_COLOR_MST.COLOR : "-"),
                    PlateNumber = (r.PLATE_NUMBER != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR + "-" + r.PLATE_NUMBER : "-"),
                    PlateCode = (r.PLATE_CHAR_CODE != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR : "-"),
                    CarType = (r.VECHILE_NAME_CODE != null ? r.RA42_VECHILE_NAME_MST.VECHILE_NAME : "-"),
                    CarName = (r.VECHILE_CODE != null ? r.RA42_VECHILE_CATIGORY_MST.VECHILE_CAT : "-"),
                    WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                    Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                    WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                    Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == r.VISITOR_PASS_CODE && r.DLT_STS != true).Count(),
                    Violations = r.RA42_ACCESS_TYPE_MST.RA42_VECHILE_VIOLATION_DTL.Where(x => x.DLT_STS != true && x.ACCESS_ROW_CODE == r.VISITOR_PASS_CODE).Count(),
                    CompanyPermitId = 0,


                }).AsQueryable();
                result.AddRange(visitors);
            }
            catch
            {
            }

            try
            {
                switch (searchType)
                {
                  
                    case "CivilNumber":
                        result = result.Where(v => v.CivilNumber == string_to_search).ToList();
                        break;
                    case "Name":
                        result = result.Where(v => v.Name.Contains(string_to_search)).ToList();
                        break;
                    case "PlateNumber":
                        result = result.Where(v => v.PlateNumber == string_to_search).ToList();
                        break;
                    case "kafeel":
                        result = result.Where(v => v.ResponsipoleServiceNumber == string_to_search || v.Responsipole == string_to_search).ToList();
                        break;
                    case "Phone":
                        result = result.Where(v => v.Gsm == string_to_search || v.Phone == string_to_search).ToList();
                        break;
                    case "Company":
                        result = result.Where(v => v.Company == string_to_search).ToList();
                        break;
                   
                }

            }
            catch
            {

            }

            List<ClearanceSearchResult> itemList = result.ToList();
            ViewBag.TotalRecords = itemList.Count();
            var pagedList = itemList.OrderByDescending(v => v.CreatedDate).ToPagedList(pageNumber, pageSize);
            return View(pagedList);
        }
        [HttpGet]
        //this action for tem permits of rno
        public async Task<ActionResult> RnoTempPermitsManage(int?page, string permit)
        {
            ViewBag.Managepasses = "";
            ViewBag.RnoTempPermits = "RnoTempPermits";
            ViewBag.controllerName = "";
            string ttt = Resources.Passes.ResourceManager.GetString("manage_temp_permits" + "_" + "ar");
            if (Language.GetCurrentLang() == "en")
            {
                ttt = Resources.Passes.ResourceManager.GetString("manage_temp_permits" + "_" + "en");
            }
            ViewBag.controllerIconClass = "fa fa-user-clock";
            ViewBag.controllerNamePlural = ttt;
            ViewBag.controllerNameSingular = ttt;
            int station = Convert.ToInt32(ViewBag.STATION_CODE_TYPE);
            int workflow = Convert.ToInt32(ViewBag.workflowIDType);
            var total1 = await db.RA42_VISITOR_PASS_DTL.Where(a => a.STATION_CODE == station && a.DLT_STS != true && a.DATE_TO >= DateTime.Today && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.REJECTED != true).ToListAsync();
            var total2 = await db.RA42_PERMITS_DTL.Where(a => a.STATION_CODE == station && a.DLT_STS != true && a.DATE_TO >= DateTime.Today && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.REJECTED != true).ToListAsync();
            ViewBag.companies_temp = total1.Where(a => a.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "12").Count() + total2.Where(a => a.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "12").Count();
            ViewBag.families_temp = total1.Where(a => a.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "13").Count() + total2.Where(a => a.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "13").Count();
            ViewBag.medical_temp = total1.Where(a => a.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "11").Count() + total2.Where(a => a.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "11").Count();
            ViewBag.visitors_temp = total1.Where(a => a.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "14").Count() + total2.Where(a => a.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "14").Count();
            ViewBag.visitors_mod_temp = total1.Where(a => a.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "16").Count() + total2.Where(a => a.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "16").Count();
            List<ClearanceSearchResult> result = new List<ClearanceSearchResult>();
            int pageSize = 10; // Number of items to display per page
            int pageNumber = page ?? 1; // Default to the first page if no page number is specified
            
            try
            {
                var new_permits = total2.Where(a => a.DLT_STS != true && a.ACCESS_TYPE_CODE == 9 &&
                a.RA42_CARD_FOR_MST.CARD_SECRET_CODE == permit
                ).Select(r => new ClearanceSearchResult
                {
                    Id = r.PERMIT_CODE,
                    ControllerName = "Permitsdtl",
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
                    ServiceNumber = r.SERVICE_NUMBER,
                    Responsipole = r.UNIT_A,
                    PesronalImage = r.PERSONAL_IMAGE,
                    PurposeOfPass = r.PURPOSE_OF_PASS,
                    Name = r.NAME_A,
                    Rank = r.RANK_A,
                    HosRank = r.HOST_RANK_A,
                    HostName = r.HOST_NAME_A,
                    Phone = r.PHONE_NUMBER,
                    Gsm = r.GSM,
                    PermitType = r.RA42_CARD_FOR_MST.CARD_FOR_A,
                    CivilNumber = r.CIVIL_NUMBER,
                    IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.Value.ToShortDateString() : " "),
                    ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                    CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                    StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                    Status = r.STATUS,
                    Delivered = r.ISDELIVERED,
                    CardFor = r.CARD_FOR_CODE.Value,
                    RegistrationSerial = r.REGISTRATION_SERIAL,
                    Company = (r.COMPANY_CODE != null ? r.RA42_COMPANY_MST.COMPANY_NAME : " "),
                    EventExercise = (r.EVENT_EXERCISE_CODE != null ? r.RA42_EVENT_EXERCISE_MST.EVENT_EXERCISE_NAME : " "),
                    Printed = r.ISPRINTED,
                    Opened = r.ISOPENED,
                    Rejected = r.REJECTED,
                    Returned = r.RETURNED,
                    PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                    Deleted = r.DLT_STS,
                    NumOfHosted = r.NUMBER_OF_HOSTED.Value,
                    VisitorEmployer = r.VISITOR_EMPLOYER,
                    Relative = (r.RELATIVE_TYPE_CODE != null ? r.RA42_RELATIVE_TYPE_MST.RELATIVE_TYPE : " "),
                    SnGardian = r.SN_FOR_THE_GUARDIAN,
                    HomeNumber = r.BUILDING_NUMBER,
                    Station = r.STATION_CODE.Value,
                    Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                    ResponsipoleServiceNumber = r.RESPONSIBLE,
                    CarColor = (r.VECHILE_COLOR_CODE != null ? r.RA42_VECHILE_COLOR_MST.COLOR : "-"),
                    PlateNumber = (r.PLATE_NUMBER != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR + "-" + r.PLATE_NUMBER : "-"),
                    PlateCode = (r.PLATE_CHAR_CODE != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR : "-"),
                    CarType = (r.VECHILE_NAME_CODE != null ? r.RA42_VECHILE_NAME_MST.VECHILE_NAME : "-"),
                    CarName = (r.VECHILE_CODE != null ? r.RA42_VECHILE_CATIGORY_MST.VECHILE_CAT : "-"),
                    WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                    Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                    WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                    Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == r.PERMIT_CODE && r.DLT_STS != true && x.CRD_DT > new DateTime(2024, 8, 1)).Count(),
                    Violations = r.RA42_ACCESS_TYPE_MST.RA42_VECHILE_VIOLATION_DTL.Where(x => x.DLT_STS != true && x.ACCESS_ROW_CODE == r.PERMIT_CODE && x.CRD_DT > new DateTime(2024, 8, 1)).Count(),
                    CompanyPermitId = (r.COMPANY_PASS_CODE != null ? r.COMPANY_PASS_CODE.Value : 0),

                }).AsQueryable();
                result.AddRange(new_permits);
            }
            catch
            {

            }

            try
            {
                var visitors = total1.Where(a => a.DLT_STS != true 
                && a.RA42_CARD_FOR_MST.CARD_SECRET_CODE == permit
                ).Select(r => new ClearanceSearchResult
                {
                    Id = r.VISITOR_PASS_CODE,
                    ControllerName = "Visitorpass",
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
                    ServiceNumber = r.SERVICE_NUMBER,
                    PesronalImage = r.PERSONAL_IMAGE,
                    PurposeOfPass = r.PURPOSE_OF_PASS,
                    Name = r.VISITOR_NAME,
                    Rank = "",
                    HosRank = "",
                    HostName = "",
                    Phone = r.PHONE_NUMBER,
                    Gsm = r.GSM,
                    PermitType =  r.RA42_CARD_FOR_MST.CARD_FOR_A,
                    CivilNumber = r.ID_CARD_NUMBER,
                    IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.Value.ToShortDateString() : " "),
                    ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                    CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                    StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                    Status = r.STATUS,
                    Delivered = r.ISDELIVERED,
                    CardFor = r.CARD_FOR_CODE.Value,
                    Company = (r.COMPANY_CODE != null ? r.RA42_COMPANY_MST.COMPANY_NAME : " "),
                    EventExercise = "",
                    Printed = r.ISPRINTED,
                    NumOfHosted = r.NUMBER_OF_HOSTED.Value,
                    VisitorEmployer = r.VISITOR_EMPLOYER,
                    Relative = (r.RELATIVE_TYPE_CODE != null ? r.RA42_RELATIVE_TYPE_MST.RELATIVE_TYPE : " "),
                    HomeNumber = r.HOME_NUMBER,
                    SnGardian = r.SN_FOR_THE_GUARDIAN,
                    Opened = r.ISOPENED,
                    Rejected = r.REJECTED,
                    Returned = r.RETURNED,
                    PassType = "مؤقت",
                    Deleted = r.DLT_STS,
                    Station = r.STATION_CODE.Value,
                    Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                    ResponsipoleServiceNumber = r.RESPONSIBLE,
                    CarColor = (r.VECHILE_COLOR_CODE != null ? r.RA42_VECHILE_COLOR_MST.COLOR : "-"),
                    PlateNumber = (r.PLATE_NUMBER != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR + "-" + r.PLATE_NUMBER : "-"),
                    PlateCode = (r.PLATE_CHAR_CODE != null ? r.RA42_PLATE_CHAR_MST.PLATE_CHAR : "-"),
                    CarType = (r.VECHILE_NAME_CODE != null ? r.RA42_VECHILE_NAME_MST.VECHILE_NAME : "-"),
                    CarName = (r.VECHILE_CODE != null ? r.RA42_VECHILE_CATIGORY_MST.VECHILE_CAT : "-"),
                    WorkflowServiceNumber = r.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER,
                    Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                    WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                    Comments = r.RA42_ACCESS_TYPE_MST.RA42_COMMENTS_MST.Where(x => x.PASS_ROW_CODE == r.VISITOR_PASS_CODE && r.DLT_STS != true && x.CRD_DT < new DateTime(2024, 8, 1)).Count(),
                    Violations = r.RA42_ACCESS_TYPE_MST.RA42_VECHILE_VIOLATION_DTL.Where(x => x.DLT_STS != true && x.ACCESS_ROW_CODE == r.VISITOR_PASS_CODE).Count(),
                    CompanyPermitId = 0,


                }).AsQueryable();
                result.AddRange(visitors);
            }
            catch
            {

            }

            List<ClearanceSearchResult> itemList = result.OrderByDescending(v => v.CreatedDate).ToList();
            return View(itemList);
        }
        [HttpGet]
        public async Task<ActionResult> _search_by_car(string plate_number, string char_num)
        {

            ViewBag.activetab = "_search_by_car";
            ViewBag.Search = null;
            //get plate char types in english 
            ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR");

            var result = new List<ClearanceSearchResult>();

            if (!string.IsNullOrWhiteSpace(plate_number))
            {
                int c_num = Convert.ToInt32(char_num.ToString());
                var searchServices = new ClearanceSearchByCarNumber();

                result = await searchServices.Search(plate_number, c_num, ClearanceSearchtypeByCar.plate);

            }

            return View(result);

        }
        //Create company type of permit
        public async Task<ActionResult> MultiCreate(int? type, string permit, int? station)
        {
            if (type == null || permit == null || station == null)
            {
                return NotFound();
            }
            ViewBag.AddPermit = "AddPermit";
            ViewBag.Managepasses = "";
            STATION_CODE = station.Value;
            ACCESS_TYPE_CODE = type.Value;
            //get station name 
            var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == station).FirstOrDefault();
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
                ViewBag.HQ_UNIT = station;
                FORCE_CODE = check_unit.RA42_FORCES_MST.FORCE_CODE.ToString();
                ViewBag.Selected_Station = STATION_CODE;
                ViewBag.Selected_Force = FORCE_CODE;

            }

            var checkSubPermit = db.RA42_CARD_FOR_MST.Where(a => a.CARD_SECRET_CODE == permit).FirstOrDefault();
            if (checkSubPermit != null)
            {

                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.SUB_PERMIT = checkSubPermit.CARD_FOR_E;
                }
                else
                {
                    ViewBag.SUB_PERMIT = checkSubPermit.CARD_FOR_A;

                }
                ViewBag.MAIN_PERMIT_ICON = checkSubPermit.RA42_ACCESS_TYPE_MST.ICON;
                ViewBag.WITH_CAR = checkSubPermit.WITH_CAR;

            }

            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();
            if (Language.GetCurrentLang() == "en")
            {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == FORCE_CODE), "FORCE_ID", "FORCE_NAME_E");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == type), "ACCESS_TYPE_CODE", "ACCESS_TYPE_E");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.CARD_SECRET_CODE == permit), "CARD_FOR_CODE", "CARD_FOR_E");
                //company in english
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E");
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "STATION_CODE", "STATION_NAME_E");
                //sections
                ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "SECTION_NAME", "SECTION_NAME");
               
                //get zones and gates in english 
                switch (permit)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == permit && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE,
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get documents types for this kind of permit in arabic
                var doc1 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                            join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                            join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                            join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                            where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "24" && d.DLT_STS != true && z.DLT_STS != true
                            select new
                            {
                                FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                            }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_SUB_TYPES_WITHCAR = new SelectList(doc1, "FILE_TYPE_CODE", "FILE_TYPE");
                //get documents types for this kind of permit in arabic
                var doc2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                            join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                            join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                            join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                            where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "29" && d.DLT_STS != true && z.DLT_STS != true
                            select new
                            {
                                FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                            }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_SUB_TYPES_WITHOUT_CAR = new SelectList(doc2, "FILE_TYPE_CODE", "FILE_TYPE");
                ////get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
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
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == FORCE_CODE), "FORCE_ID", "FORCE_NAME_A");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == type), "ACCESS_TYPE_CODE", "ACCESS_TYPE");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.CARD_SECRET_CODE == permit), "CARD_FOR_CODE", "CARD_FOR_A");
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "STATION_CODE", "STATION_NAME_A");
                //company in english
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME");
                //sections
                ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "SECTION_NAME", "SECTION_NAME");
                
                //get zones and gates in arabic 
                switch (permit)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }
                //get documents types for this kind of permit in arabic
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == permit && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get documents types for this kind of permit in arabic
                var doc1 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                            join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                            join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                            join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                            where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "24" && d.DLT_STS != true && z.DLT_STS != true
                            select new
                            {
                                FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                            }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_SUB_TYPES_WITHCAR = new SelectList(doc1, "FILE_TYPE_CODE", "FILE_TYPE");
                //get documents types for this kind of permit in arabic
                var doc2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                            join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                            join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                            join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                            where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "29" && d.DLT_STS != true && z.DLT_STS != true
                            select new
                            {
                                FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                            }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_SUB_TYPES_WITHOUT_CAR = new SelectList(doc2, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
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

            User user = null;
            Task<User> callTask = Task.Run(
                () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                );
            callTask.Wait();
            user = callTask.Result;
            ViewBag.JundStop = "";
            if (user == null)
            {
                ViewBag.JundStop = Resources.Common.ResourceManager.GetString("jund_error" + "_" + ViewBag.lang);
            }

            var model = new GroupPermitsForCompanies();
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> MultiCreate(GroupPermitsForCompanies groupPermitsForCompanies, int[] ZONE, int[] SUB_ZONE,
            int[] FILE_TYPES, string[] FILE_TYPES_TEXT, int[] FILE_INDEX, int[] FILE_SUB_TYPES, string[] FILE_SUB_TYPES_TEXT, string[] is_with_car,
             HttpPostedFileBase[] files, IEnumerable<HttpPostedFileBase>[] subfiles, HttpPostedFileBase[] PERSONAL_IMAGE, int[] IDENTITY_CODE, int[] GENDER_ID, int[] PASS_TYPE_CODE,
             string[] CIVIL_NUMBER, string[] NAME_A, string[] NAME_E, string[] PROFESSION_A, string[] PROFESSION_E,
             int[] VECHILE_CODE, int[] VECHILE_NAME_CODE, int[] PLATE_CODE, string[] PLATE_NUMBER, int[] PLATE_CHAR_CODE, 
             int[] VECHILE_COLOR_CODE, DateTime[] DATE_FROM, DateTime[] DATE_TO, string[] GSM, string[] WORK_PLACE, 
             string[] ADDRESS, DateTime[] CARD_EXPIRED_DATE, string[] REMARKS_SUB, int[] INDEX_NUM,
            int? type, string permit, int? station)
        {
            if (type == null || permit == null || station == null)
            {
                return NotFound();
            }
            ViewBag.AddPermit = "AddPermit";
            ViewBag.Managepasses = "";
            STATION_CODE = station.Value;
            ACCESS_TYPE_CODE = type.Value;
            //get station name 
            var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == station).FirstOrDefault();
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
                ViewBag.HQ_UNIT = station;
                FORCE_CODE = check_unit.RA42_FORCES_MST.FORCE_CODE.ToString();
                ViewBag.Selected_Station = STATION_CODE;
                ViewBag.Selected_Force = FORCE_CODE;

            }

            var checkSubPermit = db.RA42_CARD_FOR_MST.Where(a => a.CARD_SECRET_CODE == permit).FirstOrDefault();
            if (checkSubPermit != null)
            {

                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.SUB_PERMIT = checkSubPermit.CARD_FOR_E;
                }
                else
                {
                    ViewBag.SUB_PERMIT = checkSubPermit.CARD_FOR_A;

                }
                ViewBag.MAIN_PERMIT_ICON = checkSubPermit.RA42_ACCESS_TYPE_MST.ICON;
                ViewBag.WITH_CAR = checkSubPermit.WITH_CAR;

            }

            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();
            if (Language.GetCurrentLang() == "en")
            {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == FORCE_CODE), "FORCE_ID", "FORCE_NAME_E");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == type), "ACCESS_TYPE_CODE", "ACCESS_TYPE_E");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.CARD_SECRET_CODE == permit), "CARD_FOR_CODE", "CARD_FOR_E");
                //company in english
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", groupPermitsForCompanies.COMPANY_CODE);
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "STATION_CODE", "STATION_NAME_E", groupPermitsForCompanies.STATION_CODE);
                //sections
                ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "SECTION_NAME", "SECTION_NAME", groupPermitsForCompanies.UNIT_A);

                //get zones and gates in english 
                switch (permit)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == permit && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE,
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get documents types for this kind of permit in arabic
                var doc1 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                            join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                            join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                            join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                            where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "24" && d.DLT_STS != true && z.DLT_STS != true
                            select new
                            {
                                FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                            }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_SUB_TYPES_WITHCAR = new SelectList(doc1, "FILE_TYPE_CODE", "FILE_TYPE");
                //get documents types for this kind of permit in arabic
                var doc2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                            join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                            join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                            join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                            where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "29" && d.DLT_STS != true && z.DLT_STS != true
                            select new
                            {
                                FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                            }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_SUB_TYPES_WITHOUT_CAR = new SelectList(doc2, "FILE_TYPE_CODE", "FILE_TYPE");
                ////get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", groupPermitsForCompanies.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {

                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return RedirectToAction("MultiCreate", new { type = type, station = station, permit = permit });

                    }
                }
            }
            else
            {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == FORCE_CODE), "FORCE_ID", "FORCE_NAME_A");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == type), "ACCESS_TYPE_CODE", "ACCESS_TYPE");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.CARD_SECRET_CODE == permit), "CARD_FOR_CODE", "CARD_FOR_A");
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "STATION_CODE", "STATION_NAME_A");
                //company in english
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", groupPermitsForCompanies.COMPANY_CODE);
                //sections
                ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "SECTION_NAME", "SECTION_NAME", groupPermitsForCompanies.UNIT_A);

                //get zones and gates in arabic 
                switch (permit)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }
                //get documents types for this kind of permit in arabic
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == permit && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get documents types for this kind of permit in arabic
                var doc1 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                            join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                            join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                            join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                            where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "24" && d.DLT_STS != true && z.DLT_STS != true
                            select new
                            {
                                FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                            }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_SUB_TYPES_WITHCAR = new SelectList(doc1, "FILE_TYPE_CODE", "FILE_TYPE");
                //get documents types for this kind of permit in arabic
                var doc2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                            join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                            join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                            join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                            where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "29" && d.DLT_STS != true && z.DLT_STS != true
                            select new
                            {
                                FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                            }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_SUB_TYPES_WITHOUT_CAR = new SelectList(doc2, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", groupPermitsForCompanies.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {

                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return RedirectToAction("MultiCreate", new { type = type, station = station, permit = permit });

                    }
                }
            }
           
            User user = null;
            Task<User> callTask = Task.Run(
                () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                );
            callTask.Wait();
            user = callTask.Result;
            ViewBag.JundStop = "";
            if (user == null)
            {
                ViewBag.JundStop = @Resources.Common.ResourceManager.GetString("jund_error" + "_" + ViewBag.lang);
            }


            if (ModelState.IsValid) 
            {
                try
                {
                    RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL = new RA42_COMPANY_PASS_DTL();

                    rA42_COMPANY_PASS_DTL.SERVICE_NUMBER = groupPermitsForCompanies.RESPONSIBLE;
                    rA42_COMPANY_PASS_DTL.STATION_CODE = groupPermitsForCompanies.STATION_CODE;
                    rA42_COMPANY_PASS_DTL.ACCESS_TYPE_CODE = groupPermitsForCompanies.ACCESS_TYPE_CODE;
                    rA42_COMPANY_PASS_DTL.CARD_FOR_CODE = groupPermitsForCompanies.CARD_FOR_CODE;
                    rA42_COMPANY_PASS_DTL.RESPONSIBLE = groupPermitsForCompanies.UNIT_A;
                    rA42_COMPANY_PASS_DTL.COMPANY_TYPE_CODE = 2;
                    rA42_COMPANY_PASS_DTL.COMPANY_CODE = groupPermitsForCompanies.COMPANY_CODE;

                if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                {

                    rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = groupPermitsForCompanies.WORKFLOW_RESPO_CODE;
                    rA42_COMPANY_PASS_DTL.REJECTED = false;
                    rA42_COMPANY_PASS_DTL.STATUS = false;
                    rA42_COMPANY_PASS_DTL.ISOPENED = false;
                }
                //this section is for autho person 
                if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //he should redirect this request to the permits cell 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("MultiCreate", new { type = type, station = station, permit = permit });

                        }
                        else
                    {
                        rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        rA42_COMPANY_PASS_DTL.APPROVAL_SN = currentUser;
                        rA42_COMPANY_PASS_DTL.APPROVAL_RANK = ViewBag.RNK;
                        rA42_COMPANY_PASS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                        rA42_COMPANY_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                        rA42_COMPANY_PASS_DTL.REJECTED = false;
                        rA42_COMPANY_PASS_DTL.STATUS = false;
                        rA42_COMPANY_PASS_DTL.ISOPENED = false;
                    }

                   
                }
                //this section is for permits cell
                if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //permits cell should redirect the request for the security officer 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("MultiCreate", new { type = type, station = station, permit = permit });

                        }
                        else
                    {
                        rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        rA42_COMPANY_PASS_DTL.PERMIT_SN = currentUser;
                        rA42_COMPANY_PASS_DTL.PERMIT_RANK = ViewBag.RNK;
                        rA42_COMPANY_PASS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                        rA42_COMPANY_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                        rA42_COMPANY_PASS_DTL.REJECTED = false;
                        rA42_COMPANY_PASS_DTL.STATUS = false;
                        rA42_COMPANY_PASS_DTL.ISOPENED = true;
                    }

                   
                }
                //this section is for security officer 
                if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                {
                    //afetr he create and complete the request, the request will redirected for the permit cell for printing 
                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                    if (v == null)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("MultiCreate", new { type = type, station = station, permit = permit });

                        }
                        else
                    {
                        rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                        rA42_COMPANY_PASS_DTL.AUTHO_SN = currentUser;
                        rA42_COMPANY_PASS_DTL.AUTHO_RANK = ViewBag.RNK;
                        rA42_COMPANY_PASS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                        rA42_COMPANY_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                        rA42_COMPANY_PASS_DTL.REJECTED = false;
                        rA42_COMPANY_PASS_DTL.STATUS = true;
                        rA42_COMPANY_PASS_DTL.ISOPENED = true;
                    }

                    
                }
                rA42_COMPANY_PASS_DTL.REMARKS = groupPermitsForCompanies.REMARKS;
                rA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS = groupPermitsForCompanies.PURPOSE_OF_PASS;
                rA42_COMPANY_PASS_DTL.CRD_BY = currentUser;
                rA42_COMPANY_PASS_DTL.CRD_DT = DateTime.Now;
                rA42_COMPANY_PASS_DTL.UPD_BY = currentUser;
                rA42_COMPANY_PASS_DTL.UPD_DT = DateTime.Now;
                rA42_COMPANY_PASS_DTL.BARCODE = groupPermitsForCompanies.BARCODE;
                db.RA42_COMPANY_PASS_DTL.Add(rA42_COMPANY_PASS_DTL);
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
                            rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = groupPermitsForCompanies.ACCESS_TYPE_CODE;
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

                                        fileName = "FileNO" + c + "_" + groupPermitsForCompanies.ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                        // store the file inside ~/App_Data/uploads folder
                                        string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                        file.SaveAs(path);
                                        //add file name to db table 
                                        RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                        {
                                            ACCESS_TYPE_CODE = groupPermitsForCompanies.ACCESS_TYPE_CODE,
                                            ACCESS_ROW_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE,
                                            FILE_TYPE = FILE_TYPES[c],
                                            FILE_TYPE_TEXT = FILE_TYPES_TEXT[c],
                                            CONTROLLER_NAME = "Companies",
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
                                        var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE && a.ACCESS_TYPE_CODE == groupPermitsForCompanies.ACCESS_TYPE_CODE && a.CONTROLLER_NAME.Equals("Companies")).ToList();
                                        foreach (var del in delete)
                                        {
                                            string filpath = "~/Documents/" + del.FILE_NAME;
                                            general.RemoveFileFromServer(filpath);
                                            db.RA42_FILES_MST.Remove(del);
                                            db.SaveChanges();
                                        }
                                        TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                        return RedirectToAction("MultiCreate", new { type = type, station = station, permit = permit });
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
                    //add workers
                    if (IDENTITY_CODE != null)
                    {
                        RA42_PERMITS_DTL rA42_TEMPRORY_COMPANY_PASS_DTL = new RA42_PERMITS_DTL();

                        for (int j = 0; j < IDENTITY_CODE.Length; j++)
                        {
                            //create barcode for every worker
                            string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                            rA42_TEMPRORY_COMPANY_PASS_DTL.COMPANY_PASS_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.ACCESS_TYPE_CODE = groupPermitsForCompanies.ACCESS_TYPE_CODE;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.STATION_CODE = groupPermitsForCompanies.STATION_CODE;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.COMPANY_CODE = groupPermitsForCompanies.COMPANY_CODE;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.RESPONSIBLE = groupPermitsForCompanies.RESPONSIBLE;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.UNIT_A = groupPermitsForCompanies.UNIT_A;
                            if (is_with_car[j].Equals("yes"))
                            {
                                rA42_TEMPRORY_COMPANY_PASS_DTL.VECHILE_CODE = VECHILE_CODE[j];
                                rA42_TEMPRORY_COMPANY_PASS_DTL.VECHILE_NAME_CODE = VECHILE_NAME_CODE[j];
                                rA42_TEMPRORY_COMPANY_PASS_DTL.VECHILE_COLOR_CODE = VECHILE_COLOR_CODE[j];
                                rA42_TEMPRORY_COMPANY_PASS_DTL.PLATE_CHAR_CODE = PLATE_CHAR_CODE[j];
                                rA42_TEMPRORY_COMPANY_PASS_DTL.PLATE_CODE = PLATE_CODE[j];
                                rA42_TEMPRORY_COMPANY_PASS_DTL.PLATE_NUMBER = PLATE_NUMBER[j];
                                rA42_TEMPRORY_COMPANY_PASS_DTL.CARD_FOR_CODE = 24;
                            }
                            else
                            {
                                rA42_TEMPRORY_COMPANY_PASS_DTL.VECHILE_CODE = null;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.VECHILE_NAME_CODE = null;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.VECHILE_COLOR_CODE = null;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.PLATE_CHAR_CODE = null;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.PLATE_CODE = null;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.PLATE_NUMBER = null;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.CARD_FOR_CODE = 29;

                            }
                            rA42_TEMPRORY_COMPANY_PASS_DTL.IDENTITY_CODE = IDENTITY_CODE[j];
                            rA42_TEMPRORY_COMPANY_PASS_DTL.GENDER_ID = GENDER_ID[j];
                            rA42_TEMPRORY_COMPANY_PASS_DTL.PASS_TYPE_CODE = PASS_TYPE_CODE[j];
                            rA42_TEMPRORY_COMPANY_PASS_DTL.CIVIL_NUMBER = CIVIL_NUMBER[j];
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
                            rA42_TEMPRORY_COMPANY_PASS_DTL.REMARKS = REMARKS_SUB[j];
                            if (PERSONAL_IMAGE[j] != null)
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
                                                return RedirectToAction("MultiCreate", new { type = type, station = station, permit = permit });
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
                                            return RedirectToAction("MultiCreate", new { type = type, station = station, permit = permit });
                                        }
                                    }
                                }

                                catch (Exception ex)
                                {
                                    ex.GetBaseException();
                                }
                            }
                            rA42_TEMPRORY_COMPANY_PASS_DTL.PURPOSE_OF_PASS = groupPermitsForCompanies.PURPOSE_OF_PASS;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.APPROVAL_SN = rA42_COMPANY_PASS_DTL.APPROVAL_SN;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.APPROVAL_RANK = rA42_COMPANY_PASS_DTL.APPROVAL_RANK;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.APPROVAL_NAME = rA42_COMPANY_PASS_DTL.APPROVAL_NAME;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.APPROVAL_APPROVISION_DATE = rA42_COMPANY_PASS_DTL.APPROVAL_APPROVISION_DATE;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.PERMIT_SN = rA42_COMPANY_PASS_DTL.PERMIT_SN;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.PERMIT_RANK = rA42_COMPANY_PASS_DTL.PERMIT_RANK;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.PERMIT_NAME = rA42_COMPANY_PASS_DTL.PERMIT_NAME;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.PERMIT_APPROVISION_DATE = rA42_COMPANY_PASS_DTL.PERMIT_APPROVISION_DATE;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.AUTHO_SN = rA42_COMPANY_PASS_DTL.AUTHO_SN;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.AUTHO_RANK = rA42_COMPANY_PASS_DTL.AUTHO_RANK;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.AUTHO_NAME = rA42_COMPANY_PASS_DTL.AUTHO_NAME;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.AUTHO_APPROVISION_DATE = rA42_COMPANY_PASS_DTL.AUTHO_APPROVISION_DATE;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.REJECTED = rA42_COMPANY_PASS_DTL.REJECTED;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.STATUS = rA42_COMPANY_PASS_DTL.STATUS;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.ISOPENED = rA42_COMPANY_PASS_DTL.ISOPENED;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.CRD_BY = currentUser;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.CRD_DT = DateTime.Now;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.UPD_BY = currentUser;
                            rA42_TEMPRORY_COMPANY_PASS_DTL.UPD_DT = DateTime.Now;
                            db.RA42_PERMITS_DTL.Add(rA42_TEMPRORY_COMPANY_PASS_DTL);
                            db.SaveChanges();

                            //add selected documents
                            if (subfiles[j] != null)
                            {

                                try
                                {
                                    //create foreach loop to upload multiple files 
                                    int c = 0;
                                    foreach (HttpPostedFileBase file in subfiles[j])
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

                                                    fileName = "FileNO" + c + "_" + groupPermitsForCompanies.ACCESS_TYPE_CODE + "_" + rA42_TEMPRORY_COMPANY_PASS_DTL.CARD_FOR_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                                    // store the file inside ~/App_Data/uploads folder
                                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                                    file.SaveAs(path);
                                                    //add file name to db table 

                                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                                    {
                                                        ACCESS_TYPE_CODE = groupPermitsForCompanies.ACCESS_TYPE_CODE,
                                                        ACCESS_ROW_CODE = rA42_TEMPRORY_COMPANY_PASS_DTL.PERMIT_CODE,
                                                        FILE_TYPE = FILE_SUB_TYPES[c],
                                                        FILE_TYPE_TEXT = FILE_SUB_TYPES_TEXT[c],
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
                                                var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.PERMIT_CODE && a.ACCESS_TYPE_CODE == groupPermitsForCompanies.ACCESS_TYPE_CODE).ToList();
                                                foreach (var del in delete)
                                                {
                                                    string filpath = "~/Documents/" + del.FILE_NAME;
                                                    general.RemoveFileFromServer(filpath);
                                                    db.RA42_FILES_MST.Remove(del);
                                                    db.SaveChanges();
                                                }
                                                TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                                return RedirectToAction("MultiCreate", new { type = type,station=station,permit=permit });

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
                        }

                    }
                    AddToast(new Toast("",
                       GetResourcesValue("success_create_message"),
                       "green"));
                   
                    return RedirectToAction("Index", "Mypasses");

                }
                catch (DbEntityValidationException cc)
                {
                    foreach (var validationerrors in cc.EntityValidationErrors)
                    {
                        foreach (var validationerror in validationerrors.ValidationErrors)
                        {
                            TempData["Erorr"] = validationerror.PropertyName + " + " + validationerror.ErrorMessage;
                            return View(groupPermitsForCompanies);
                        }
                    }

                }


            }
            
            return View(groupPermitsForCompanies);
        }

        //Create any type of permit
        public async Task<ActionResult> Create(int? type, string permit, int? station)
		{
            if(type == null || permit == null || station == null)
            {
                return NotFound();
            }
            ViewBag.AddPermit = "AddPermit";
            ViewBag.Managepasses = "";
            STATION_CODE = station.Value;
            ACCESS_TYPE_CODE = type.Value;
            var RnoVisitors = new List<string>() { "11", "12", "13", "14", "16" };
            if (RnoVisitors.Contains(permit))
            {
                switch (permit)
                {
                    case "11":
                        ViewBag.activetab = "MedicalCenterTemporary";
                        break;
                    case "12":
                        ViewBag.activetab = "CompaniesTemporary";
                        break;
                    case "13":
                        ViewBag.activetab = "FamilyTemporary";
                        break;
                    case "14":
                        ViewBag.activetab = "VisitorTemporary";
                        break;
                    case "16":
                        ViewBag.activetab = "VisitorFromRno";
                        break;

                }
            }
            //get station name 
            var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == station).FirstOrDefault();
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
                ViewBag.HQ_UNIT = station;
                FORCE_CODE = check_unit.RA42_FORCES_MST.FORCE_CODE.ToString();
                ViewBag.Selected_Station = STATION_CODE;
                ViewBag.Selected_Force = FORCE_CODE;

            }
            
            var checkSubPermit =  db.RA42_CARD_FOR_MST.Where(a=>a.CARD_SECRET_CODE == permit && a.DLT_STS !=true).FirstOrDefault();
            if(checkSubPermit != null)
            {

                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.SUB_PERMIT = checkSubPermit.CARD_FOR_E;
                }
                else
                {
                    ViewBag.SUB_PERMIT = checkSubPermit.CARD_FOR_A;

                }
                ViewBag.MAIN_PERMIT_ICON = checkSubPermit.RA42_ACCESS_TYPE_MST.ICON;
                ViewBag.WITH_CAR = checkSubPermit.WITH_CAR;

            }

            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }

            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();
            //same workflow for rno
            var same_workflow = new List<string>() { "8", "2", "5", "7", "9" };
            //employees and families
            var employees_and_families = new List<string>() { "21", "23", "27", "35", "15" };
            if (Language.GetCurrentLang() == "en")
            {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == FORCE_CODE), "FORCE_ID", "FORCE_NAME_E");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == type), "ACCESS_TYPE_CODE", "ACCESS_TYPE_E");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.DLT_STS != true && a.CARD_SECRET_CODE == permit), "CARD_FOR_CODE", "CARD_FOR_E");
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "STATION_CODE", "STATION_NAME_E");
                //sections
                ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "SECTION_NAME", "SECTION_NAME");
                //get bloods
                ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE");
                //get events and excrises in english
                ViewBag.EVENT_EXERCISE_CODE = new SelectList(db.RA42_EVENT_EXERCISE_MST.Where(a => a.DLT_STS != true && a.ACTIVE != false && a.DATE_TO <= DateTime.Today), "EVENT_EXERCISE_CODE", "EVENT_EXERCISE_NAME_E");
                //company name 
                if (type == 6)
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 1), "COMPANY_CODE", "COMPANY_NAME_E");

                }
                else
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E");
                }
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E");
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                if (FORCE_CODE == "3")
                {
                    //this option for RANO, all temprory permits in the visitor permits
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                else
                {
                    //gt permits types in english (مؤقت - ثابت)
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E");
                }

                if (RnoVisitors.Contains(permit))
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 2), "PASS_TYPE_CODE", "PASS_TYPE_E");

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
                switch (permit)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == permit && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var unique_permits = new List<string> { "1", "6", "10" };
                if(unique_permits.Contains(type.ToString()))
                {
                    if (type == 10)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
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
                    if(type == 1)
                    {
                        // here to detrmine who is autho resonsible to proccess to permit
                        int workflow_id = 5;
                        if (ViewBag.RESPO_STATE == 6)
                        {
                            workflow_id = 6;
                        }
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E +"-"+s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                        //if WORKFLOW_RESPO.Count == 0 that means no body setting in this position and the system should return error message no body in this posision

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            //return View();

                        }
                    }

                    if(type == 6)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E +" - "+s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                        //check if WORKFLOW_RESPO.Count == 0 show error message 
                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            //return RedirectToAction("Create");

                        }
                    }
                }
                if (!unique_permits.Contains(type.ToString()))
                {
                    var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                    if (WORKFLOW_RESPO.Count == 0)
                    {
                        //show error message if there is no autho person, when current user workflow id is one or less than one 
                        if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                        {
                            
                                if (FORCE_CODE != "3" || !employees_and_families.Contains(permit))
                                {
                                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                                }
                                 
                            
                        }
                    }
                }
            }
            else
            {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == FORCE_CODE), "FORCE_ID", "FORCE_NAME_A");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == type), "ACCESS_TYPE_CODE", "ACCESS_TYPE");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.DLT_STS != true && a.CARD_SECRET_CODE == permit), "CARD_FOR_CODE", "CARD_FOR_A");
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "STATION_CODE", "STATION_NAME_A");
                //sections
                ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "SECTION_NAME", "SECTION_NAME");
                //get bloods
                ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE");
                //get events and excrises in english
                ViewBag.EVENT_EXERCISE_CODE = new SelectList(db.RA42_EVENT_EXERCISE_MST.Where(a => a.DLT_STS != true && a.ACTIVE != false && a.DATE_TO <= DateTime.Today), "EVENT_EXERCISE_CODE", "EVENT_EXERCISE_NAME");
                //company name 
                if (type == 6)
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 1), "COMPANY_CODE", "COMPANY_NAME");

                }
                else
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME");
                }
                //get relatives typs in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE");
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types codes in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (FORCE_CODE == "3")
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                if (RnoVisitors.Contains(permit))
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 2), "PASS_TYPE_CODE", "PASS_TYPE");

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
                switch (permit)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }
                //get documents types for this kind of permit in arabic
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == permit && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني)
                var unique_permits = new List<string> { "1", "6", "10" };
                if (unique_permits.Contains(type.ToString()))
                {
                    if (type == 10)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
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
                    if (type == 1)
                    {
                        // here to detrmine who is autho resonsible to proccess to permit
                        int workflow_id = 5;
                        if (ViewBag.RESPO_STATE == 6)
                        {
                            workflow_id = 6;
                        }
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                        //if WORKFLOW_RESPO.Count == 0 that means no body setting in this position and the system should return error message no body in this posision

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            //return View();

                        }
                    }

                    if (type == 6)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");
                        //check if WORKFLOW_RESPO.Count == 0 show error message 
                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            //return RedirectToAction("Create");

                        }
                    }
                }
                if (!unique_permits.Contains(type.ToString()))
                {
                    var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME");

                    if (WORKFLOW_RESPO.Count == 0)
                    {
                        //show error message if there is no autho person, when current user workflow id is one or less than one 
                        if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                        {
                           
                                if (FORCE_CODE != "3" || !employees_and_families.Contains(permit))
                                {
                                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                                }
                            
                        }
                    }
                }
            }

            User user = null;
            Task<User> callTask = Task.Run(
                () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                );
            callTask.Wait();
            user = callTask.Result;
            ViewBag.JundStop = "";
            if (user == null)
            {
               ViewBag.JundStop = @Resources.Common.ResourceManager.GetString("jund_error" + "_" + ViewBag.lang);
            }
            RA42_PERMITS_DTL rA42_PERMITS_DTL = new RA42_PERMITS_DTL();
            TempData["PermitModel"] = rA42_PERMITS_DTL;

            return View();
		}

		// POST: RA42_PERMITS_DTL/Create
		// To protect from overposting attacks, enable the specific properties you want to bind to, for 
		// more details see https://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Create(RA42_PERMITS_DTL rA42_PERMITS_DTL, int? type, string permit, int? station,
            int[] RELATIVE_TYPES, HttpPostedFileBase[] RELATIVE_IMAGE, int[] IDENTITY_TYPES, int[] GENDER_TYPES, 
            string[] FULL_NAME, string[] CIVIL_NUM, string[] PASSPORT_NUMBER, string[] PHONE_NUMBER_M
            , string[] REMARKS_LIST, int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES, string is_with_car,
            string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
		{
            if (type == null || permit == null || station == null)
            {
                return NotFound();
            }
            TempData["PermitModel"] = rA42_PERMITS_DTL;
            ViewBag.AddPermit = "AddPermit";
            ViewBag.Managepasses = "";
            STATION_CODE = station.Value;
            ACCESS_TYPE_CODE = type.Value;
            var RnoVisitors = new List<string>() { "11", "12", "13", "14", "16" };
            if (RnoVisitors.Contains(permit))
            {
                switch (permit)
                {
                    case "11":
                        ViewBag.activetab = "MedicalCenterTemporary";
                        break;
                    case "12":
                        ViewBag.activetab = "CompaniesTemporary";
                        break;
                    case "13":
                        ViewBag.activetab = "FamilyTemporary";
                        break;
                    case "14":
                        ViewBag.activetab = "VisitorTemporary";
                        break;
                    case "16":
                        ViewBag.activetab = "VisitorFromRno";
                        break;

                }
            }
            //get station name 
            var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == station).FirstOrDefault();
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
                ViewBag.HQ_UNIT = station;
                FORCE_CODE = check_unit.RA42_FORCES_MST.FORCE_CODE.ToString();
                ViewBag.Selected_Station = STATION_CODE;
                ViewBag.Selected_Force = FORCE_CODE;

            }

            var checkSubPermit = await db.RA42_CARD_FOR_MST.Where(a => a.CARD_SECRET_CODE == permit && a.DLT_STS != true).FirstOrDefaultAsync();
            if (checkSubPermit != null)
            {

                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.SUB_PERMIT = checkSubPermit.CARD_FOR_E;
                }
                else
                {
                    ViewBag.SUB_PERMIT = checkSubPermit.CARD_FOR_A;

                }
                ViewBag.MAIN_PERMIT_ICON = checkSubPermit.RA42_ACCESS_TYPE_MST.ICON;
                ViewBag.WITH_CAR = checkSubPermit.WITH_CAR;

            }

            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //get security caveates 
            ViewBag.CAVEATES = db.RA42_SECURITY_CAVEATES_DTL.Where(a => a.DLT_STS != true && a.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE).OrderByDescending(a => a.SECURITY_CAVEATES_CODE).FirstOrDefault();
            //same workflow for rno
            var same_workflow = new List<string>() { "8", "2", "5", "7", "9" };
            var employees_and_families = new List<string>() { "21", "23", "27", "35", "15" };

            if (Language.GetCurrentLang() == "en")
            {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == FORCE_CODE), "FORCE_ID", "FORCE_NAME_E");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == type), "ACCESS_TYPE_CODE", "ACCESS_TYPE_E");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.DLT_STS != true && a.CARD_SECRET_CODE == permit), "CARD_FOR_CODE", "CARD_FOR_E");
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "STATION_CODE", "STATION_NAME_E");
                //sections
                ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "SECTION_NAME", "SECTION_NAME");
                //get bloods
                ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE",rA42_PERMITS_DTL.BLOOD_CODE);
                //get events and excrises in english
                ViewBag.EVENT_EXERCISE_CODE = new SelectList(db.RA42_EVENT_EXERCISE_MST.Where(a => a.DLT_STS != true && a.ACTIVE != false && a.DATE_TO <= DateTime.Today), "EVENT_EXERCISE_CODE", "EVENT_EXERCISE_NAME_E",rA42_PERMITS_DTL.EVENT_EXERCISE_CODE);
                //company name 
                if (type == 6)
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 1), "COMPANY_CODE", "COMPANY_NAME_E",rA42_PERMITS_DTL.COMPANY_CODE);

                }
                else
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E",rA42_PERMITS_DTL.COMPANY_CODE);
                }
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E",rA42_PERMITS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E",rA42_PERMITS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E",rA42_PERMITS_DTL.IDENTITY_CODE);
                if (FORCE_CODE == "3")
                {
                    //this option for RANO, all temprory permits in the visitor permits
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                else
                {
                    //gt permits types in english (مؤقت - ثابت)
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E",rA42_PERMITS_DTL.PASS_TYPE_CODE);
                }
                if (RnoVisitors.Contains(permit))
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 2), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E",rA42_PERMITS_DTL.PLATE_CODE);
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E",rA42_PERMITS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E",rA42_PERMITS_DTL.VECHILE_CODE);
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E",rA42_PERMITS_DTL.VECHILE_COLOR_CODE);
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E",rA42_PERMITS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english 
                switch (permit)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE  && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == permit && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var unique_permits = new List<string> { "1", "6", "10" };
                if (unique_permits.Contains(type.ToString()))
                {
                    if (type == 10)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            //show error message if there is no autho person, when current user workflow id is one or less than one 
                            if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });
                            }
                        }
                    }
                    if (type == 1)
                    {
                        // here to detrmine who is autho resonsible to proccess to permit
                        int workflow_id = 5;
                        if (ViewBag.RESPO_STATE == 6)
                        {
                            workflow_id = 6;
                        }
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //if WORKFLOW_RESPO.Count == 0 that means no body setting in this position and the system should return error message no body in this posision

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                        }
                    }

                    if (type == 6)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //check if WORKFLOW_RESPO.Count == 0 show error message 
                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                        }
                    }
                }
                if (!unique_permits.Contains(type.ToString()))
                {
                    var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                    if (WORKFLOW_RESPO.Count == 0)
                    {
                        //show error message if there is no autho person, when current user workflow id is one or less than one 
                        if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                        {

                            if (FORCE_CODE != "3" || !employees_and_families.Contains(permit))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                            }
                        }
                    }
                }
            }
            else
            {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == FORCE_CODE), "FORCE_ID", "FORCE_NAME_A");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == type), "ACCESS_TYPE_CODE", "ACCESS_TYPE");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.DLT_STS != true && a.CARD_SECRET_CODE == permit), "CARD_FOR_CODE", "CARD_FOR_A");
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "STATION_CODE", "STATION_NAME_A");
                //sections
                ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "SECTION_NAME", "SECTION_NAME");
                //get bloods
                ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE",rA42_PERMITS_DTL.BLOOD_CODE);
                //get events and excrises in english
                ViewBag.EVENT_EXERCISE_CODE = new SelectList(db.RA42_EVENT_EXERCISE_MST.Where(a => a.DLT_STS != true && a.ACTIVE != false && a.DATE_TO <= DateTime.Today), "EVENT_EXERCISE_CODE", "EVENT_EXERCISE_NAME",rA42_PERMITS_DTL.EVENT_EXERCISE_CODE);
                //company name 
                if (type == 6)
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 1), "COMPANY_CODE", "COMPANY_NAME",rA42_PERMITS_DTL.COMPANY_CODE);

                }
                else
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME",rA42_PERMITS_DTL.COMPANY_CODE);
                } 
                //get relatives typs in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE",rA42_PERMITS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A",rA42_PERMITS_DTL.GENDER_ID);
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A",rA42_PERMITS_DTL.IDENTITY_CODE);
                //get permits types codes in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE",rA42_PERMITS_DTL.PASS_TYPE_CODE);
                if (FORCE_CODE == "3")
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                if (RnoVisitors.Contains(permit))
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 2), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE",rA42_PERMITS_DTL.PLATE_CODE);
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR",rA42_PERMITS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT",rA42_PERMITS_DTL.VECHILE_CODE);
                //get colrs in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR",rA42_PERMITS_DTL.VECHILE_COLOR_CODE);
                //get vechiles types in arabic (صالون - دفع رباعي)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME",rA42_PERMITS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic 
                switch (permit)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }
                //get documents types for this kind of permit in arabic
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == permit && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var unique_permits = new List<string> { "1", "6", "10" };
                if (unique_permits.Contains(type.ToString()))
                {
                    if (type == 10)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            //show error message if there is no autho person, when current user workflow id is one or less than one 
                            if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });
                            }
                        }
                    }
                    if (type == 1)
                    {
                        // here to detrmine who is autho resonsible to proccess to permit
                        int workflow_id = 5;
                        if (ViewBag.RESPO_STATE == 6)
                        {
                            workflow_id = 6;
                        }
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE).
                            Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME",rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //if WORKFLOW_RESPO.Count == 0 that means no body setting in this position and the system should return error message no body in this posision

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                        }
                    }

                    if (type == 6)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE,
                            RESPONSEPLE_NAME= s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME
                        }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME",rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //check if WORKFLOW_RESPO.Count == 0 show error message 
                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                        }
                    }
                }
                if (!unique_permits.Contains(type.ToString()))
                {
                    var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                    if (WORKFLOW_RESPO.Count == 0)
                    {
                        //show error message if there is no autho person, when current user workflow id is one or less than one 
                        if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                        {
                            if (FORCE_CODE != "3" || !employees_and_families.Contains(permit))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                            }
                        }
                    }
                }
            }

            User user = null;
            Task<User> callTask = Task.Run(
                () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                );
            callTask.Wait();
            user = callTask.Result;
            ViewBag.JundStop = "";
            if (user == null)
            {
                ViewBag.JundStop = @Resources.Common.ResourceManager.GetString("jund_error" + "_" + ViewBag.lang);
            }
            else
            {
                switch (permit)
                {
                    case "7":
                    case "8":
                    case "9":
                    case "15":
                    case "16":
                    case "21":
                    case "23":
                    case "27":
                    case "30":
                    case "35":
                    case "36":
                    case "37":
                    case "40":
                    case "41":
                    case "42":
                    case "43":
                    case "44":
                        if (string.IsNullOrWhiteSpace(rA42_PERMITS_DTL.NAME_A))
                        {
                            rA42_PERMITS_DTL.NAME_A = user.NAME_EMP_A;
                            rA42_PERMITS_DTL.RANK_A = user.NAME_RANK_A;
                            rA42_PERMITS_DTL.NAME_E = user.NAME_EMP_E;
                            rA42_PERMITS_DTL.RANK_E = user.NAME_RANK_E;
                            rA42_PERMITS_DTL.PROFESSION_A = user.NAME_TRADE_A;
                            rA42_PERMITS_DTL.PROFESSION_E = user.NAME_TRADE_E; 
                        }

                        if (string.IsNullOrWhiteSpace(rA42_PERMITS_DTL.UNIT_A))
                        {
                            rA42_PERMITS_DTL.UNIT_A = user.NAME_UNIT_A;
                        }
                        rA42_PERMITS_DTL.UNIT_E = user.NAME_UNIT_E;
                        if (permit == "30" || permit == "23")
                        {
                            rA42_PERMITS_DTL.HOST_NAME_A = user.NAME_EMP_A;
                            rA42_PERMITS_DTL.HOST_RANK_A = user.NAME_RANK_A;
                            rA42_PERMITS_DTL.HOST_NAME_E = user.NAME_EMP_E;
                            rA42_PERMITS_DTL.HOST_RANK_E = user.NAME_RANK_E;
                            rA42_PERMITS_DTL.HOST_PROFESSION_A = user.NAME_TRADE_A;
                            rA42_PERMITS_DTL.HOST_PROFESSION_E = user.NAME_TRADE_E;
                        }
                        break;

                }
            }
            var no_more_than_2 = new List<string>() { "1","44","43","42","21", "37", "36", "35", "7", "8" };
            if (no_more_than_2.Contains(permit))
            {
                
                    //check if employee has more than 2 permits still valid
                    var num_permits = await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.DATE_TO > DateTime.Today && a.STATION_CODE == STATION_CODE &&
                    a.SERVICE_NUMBER == rA42_PERMITS_DTL.SERVICE_NUMBER.ToUpper() && (a.ISPRINTED == true || a.STATUS != true || a.REJECTED == true) && a.RETURNED != true && 
                    no_more_than_2.Contains(a.RA42_CARD_FOR_MST.CARD_SECRET_CODE)).ToListAsync();
                    if (ViewBag.RESPO_STATE < 3)
                    {
                        if (num_permits.Count >= 2)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("more_than_two_permits" + "_" + ViewBag.lang);
                        return RedirectToAction("Create", new { type = type, station = station, permit = permit });
                    }
                }

                //check if employee has more than 2 permits still valid
                var num_permits2 = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.DATE_TO > DateTime.Today && a.STATION_CODE == STATION_CODE &&
                a.SERVICE_NUMBER == rA42_PERMITS_DTL.SERVICE_NUMBER.ToUpper() && (a.ISPRINTED == true || a.STATUS != true || a.REJECTED == true) && a.RETURNED != true 
                && no_more_than_2.Contains(a.RA42_CARD_FOR_MST.CARD_SECRET_CODE)).ToListAsync();
                if (ViewBag.RESPO_STATE < 3)
                {
                    if (num_permits.Count >= 2)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("more_than_two_permits" + "_" + ViewBag.lang);
                        return RedirectToAction("Create", new { type = type, station = station, permit = permit });
                    }
                }

            }
            if (ModelState.IsValid)
			{
                if (permit == "25")
                {
                    try
                    {
                        RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL = new RA42_COMPANY_PASS_DTL();

                        rA42_COMPANY_PASS_DTL.SERVICE_NUMBER = rA42_PERMITS_DTL.RESPONSIBLE;
                        rA42_COMPANY_PASS_DTL.STATION_CODE = rA42_PERMITS_DTL.STATION_CODE;
                        rA42_COMPANY_PASS_DTL.ACCESS_TYPE_CODE = rA42_PERMITS_DTL.ACCESS_TYPE_CODE;
                        rA42_COMPANY_PASS_DTL.CARD_FOR_CODE = rA42_PERMITS_DTL.CARD_FOR_CODE;
                        rA42_COMPANY_PASS_DTL.RESPONSIBLE = rA42_PERMITS_DTL.UNIT_A;
                        rA42_COMPANY_PASS_DTL.COMPANY_TYPE_CODE = 2;
                        rA42_COMPANY_PASS_DTL.COMPANY_CODE = rA42_PERMITS_DTL.COMPANY_CODE;

                        if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                        {

                            rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE;
                            rA42_COMPANY_PASS_DTL.REJECTED = false;
                            rA42_COMPANY_PASS_DTL.STATUS = false;
                            rA42_COMPANY_PASS_DTL.ISOPENED = false;
                        }
                        //this section is for autho person 
                        if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                        {
                            //he should redirect this request to the permits cell 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_COMPANY_PASS_DTL.APPROVAL_SN = currentUser;
                                rA42_COMPANY_PASS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                rA42_COMPANY_PASS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                rA42_COMPANY_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                rA42_COMPANY_PASS_DTL.REJECTED = false;
                                rA42_COMPANY_PASS_DTL.STATUS = false;
                                rA42_COMPANY_PASS_DTL.ISOPENED = false;
                            }


                        }
                        //this section is for permits cell
                        if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                        {
                            //permits cell should redirect the request for the security officer 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_COMPANY_PASS_DTL.PERMIT_SN = currentUser;
                                rA42_COMPANY_PASS_DTL.PERMIT_RANK = ViewBag.RNK;
                                rA42_COMPANY_PASS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                rA42_COMPANY_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                rA42_COMPANY_PASS_DTL.REJECTED = false;
                                rA42_COMPANY_PASS_DTL.STATUS = false;
                                rA42_COMPANY_PASS_DTL.ISOPENED = true;
                            }


                        }
                        //this section is for security officer 
                        if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                        {
                            //afetr he create and complete the request, the request will redirected for the permit cell for printing 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_COMPANY_PASS_DTL.AUTHO_SN = currentUser;
                                rA42_COMPANY_PASS_DTL.AUTHO_RANK = ViewBag.RNK;
                                rA42_COMPANY_PASS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                rA42_COMPANY_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                rA42_COMPANY_PASS_DTL.REJECTED = false;
                                rA42_COMPANY_PASS_DTL.STATUS = true;
                                rA42_COMPANY_PASS_DTL.ISOPENED = true;
                            }


                        }
                        rA42_COMPANY_PASS_DTL.REMARKS = rA42_PERMITS_DTL.REMARKS;
                        rA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS = rA42_PERMITS_DTL.PURPOSE_OF_PASS;
                        rA42_COMPANY_PASS_DTL.CRD_BY = currentUser;
                        rA42_COMPANY_PASS_DTL.CRD_DT = DateTime.Now;
                        rA42_COMPANY_PASS_DTL.UPD_BY = currentUser;
                        rA42_COMPANY_PASS_DTL.UPD_DT = DateTime.Now;
                        rA42_COMPANY_PASS_DTL.BARCODE = rA42_PERMITS_DTL.BARCODE;
                        db.RA42_COMPANY_PASS_DTL.Add(rA42_COMPANY_PASS_DTL);
                        db.SaveChanges();
                        rA42_PERMITS_DTL.COMPANY_PASS_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE;
                    }
                    catch (Exception ex)
                    {
                        TempData["Erorr"] = ex.GetBaseException();
                        return View(rA42_PERMITS_DTL);
                    }

                   
                }
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



                                fileName = "Profile_" + rA42_PERMITS_DTL.ACCESS_TYPE_CODE+"_"+permit+ "_" + DateTime.Now.ToString("yymmssfff") + extension;

                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                                   GetResourcesValue("error_update_message"),
                                   "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_PERMITS_DTL);
                                }

                                rA42_PERMITS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_PERMITS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }

                if (is_with_car.Equals("no"))
                {
                    rA42_PERMITS_DTL.VECHILE_CODE = null;
                    rA42_PERMITS_DTL.VECHILE_NAME_CODE = null;
                    rA42_PERMITS_DTL.VECHILE_COLOR_CODE = null;
                    rA42_PERMITS_DTL.PLATE_CHAR_CODE = null;
                    rA42_PERMITS_DTL.PLATE_CODE = null;
                    rA42_PERMITS_DTL.PLATE_NUMBER = null;
                    
                }
                else
                {
                    if (!rA42_PERMITS_DTL.VECHILE_CODE.HasValue || !rA42_PERMITS_DTL.VECHILE_COLOR_CODE.HasValue 
                        || !rA42_PERMITS_DTL.VECHILE_NAME_CODE.HasValue || !rA42_PERMITS_DTL.PLATE_CHAR_CODE.HasValue)
                    {
                        TempData["Erorr"] = "بيانات المركبة ناقصة، يرجى وضع بيانات صحيحة - Car details missing";
                        return RedirectToAction("Create", new { type = type, station = station, permit = permit });
                    }
                }
                if ((FORCE_CODE == "2" || FORCE_CODE == "10") && employees_and_families.Contains(permit))
                {
                    rA42_PERMITS_DTL.DATE_TO = rA42_PERMITS_DTL.DATE_TO.Value.AddDays(30);
                }
                var not_same_workflow = new List<string>() { "1", "6", "10" };

                
                if (!not_same_workflow.Contains(type.ToString()) && !RnoVisitors.Contains(permit))
                {
                    
                    //this section is for applicant 
                    if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                    {

                        if (FORCE_CODE == "3" && employees_and_families.Contains(permit.ToString()))
                        {
                            //autho person should redirect the permit to the permits cell 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE
                            && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == rA42_PERMITS_DTL.ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                                rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }
                           
                        }
                        else
                        {
                            rA42_PERMITS_DTL.REJECTED = false;
                            rA42_PERMITS_DTL.STATUS = false;
                            rA42_PERMITS_DTL.ISOPENED = false;
                        }
                    }
                    //this section is for autho person 
                    if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                    {
                        //autho person should redirect the permit to the permits cell 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE
                        && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == rA42_PERMITS_DTL.ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                        }
                        else
                        {
                            rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                            rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                            rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                            rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                            rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                            rA42_PERMITS_DTL.REJECTED = false;
                            rA42_PERMITS_DTL.STATUS = false;
                            rA42_PERMITS_DTL.ISOPENED = true;
                        }
                       
                    }
                    //this section is for permits cell 
                    if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                    {
                        if (FORCE_CODE == "3" && WORKFLOWID == 3)
                        {
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE
                            && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == rA42_PERMITS_DTL.ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = true;
                                rA42_PERMITS_DTL.ISOPENED = true;
                                string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                                rA42_PERMITS_DTL.BARCODE = barcode;
                            }

                           


                        }
                        else
                        {
                            //permits cell should redirect the permit for the security officer as final step 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE
                            && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == rA42_PERMITS_DTL.ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }
                            
                        }
                    }
                    if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                    {
                        //security officer should redirect the permit to the permits cel for printing
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE
                        && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == rA42_PERMITS_DTL.ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                        }
                        else
                        {
                            rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                            rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                            rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                            rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                            rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                            rA42_PERMITS_DTL.REJECTED = false;
                            rA42_PERMITS_DTL.STATUS = true;
                            rA42_PERMITS_DTL.ISOPENED = true;
                        }

                      

                    }

                    if (permit == "15")
                    {
                        //autho person should redirect the permit to the permits cell 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE
                        && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == rA42_PERMITS_DTL.ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                        }
                        else
                        {
                            rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                            rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                            rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                            rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                            rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;

                            rA42_PERMITS_DTL.DATE_FROM = DateTime.Today;
                            rA42_PERMITS_DTL.DATE_TO = DateTime.Today.AddDays(300);
                            rA42_PERMITS_DTL.REJECTED = false;
                            rA42_PERMITS_DTL.STATUS = true;
                            rA42_PERMITS_DTL.ISOPENED = true;
                            rA42_PERMITS_DTL.ISPRINTED = true;
                            rA42_PERMITS_DTL.ISDELIVERED = true;
                            string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                            rA42_PERMITS_DTL.BARCODE = barcode;
                        }

                    }
                }
                if (not_same_workflow.Contains(type.ToString()))
                {
                    //for autho permit
                    if (type == 1)
                    {
                        if (WORKFLOWID == 2 || WORKFLOWID <= 1)
                        {


                            rA42_PERMITS_DTL.REJECTED = false;
                            rA42_PERMITS_DTL.STATUS = false;
                            rA42_PERMITS_DTL.ISOPENED = false;


                        }
                        if (WORKFLOWID == 5)
                        {

                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 6 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                                rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }


                        }

                        if (WORKFLOWID == 3)
                        {
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 5 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE
                            == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }


                        }
                        if (WORKFLOWID == 4)
                        {
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 5 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE
                            == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }


                        }
                        if (WORKFLOWID == 6)
                        {

                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                                rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = true;
                                rA42_PERMITS_DTL.ISOPENED = true;
                                string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                                rA42_PERMITS_DTL.BARCODE = barcode;
                            }

                        }
                    }
                    //for contracted companies
                    if (type == 6)
                    {
                        if (WORKFLOWID == 2 || WORKFLOWID <= 1)
                        {

                           
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = false;
                            
                           
                        }
                        if (WORKFLOWID == 7)
                        {

                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                                rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }

                           
                        }

                        if (WORKFLOWID == 3)
                        {
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE 
                            == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }

                            
                        }
                        if (WORKFLOWID == 4)
                        {

                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                                rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = true;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }
                            
                        }
                    }
                    //for aircrew
                    if(type == 10)
                    {
                        //this section is for applicant 
                        if (WORKFLOWID <= 1 || ViewBag.NOT_RELATED_STATION == true)
                        {
                            //the request will redirected to the autho person as normal request 
                            rA42_PERMITS_DTL.REJECTED = false;
                            rA42_PERMITS_DTL.STATUS = false;
                            rA42_PERMITS_DTL.ISOPENED = false;
                        }
                        //this section is for autho person (المنسق الأمني) 
                        if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                        {
                            //the autho person should redirect the request to the permits cell 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 10 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                                rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }
                            
                        }


                        //this is security offecier
                        if ((WORKFLOWID == 10 || WORKFLOWID == 3) && ViewBag.NOT_RELATED_STATION != true)
                        {
                            //after the security oofcier create this permit and completet every thing, the permit should be redirected to the permit cell to print it 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                if (WORKFLOWID != 3)
                                {
                                    rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                    rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                                    rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                                    rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                    rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                    rA42_PERMITS_DTL.REJECTED = false;
                                    rA42_PERMITS_DTL.STATUS = true;
                                    rA42_PERMITS_DTL.ISOPENED = true;
                                    string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                                    rA42_PERMITS_DTL.BARCODE = barcode;
                                }
                            }
                           
                            if(WORKFLOWID == 3)
                            {
                                rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                               
                            }
                        }
                    }

                }
                if (RnoVisitors.Contains(permit))
                {
                    rA42_PERMITS_DTL.PASS_TYPE_CODE = 2;
                    if (permit != "12")
                    {
                        //this section is for autho person 
                        if ((WORKFLOWID == 2 || WORKFLOWID == 3 || ViewBag.RESPO_STATE <= 1))
                        {

                            //he should redirect the request to permits cell 
                            var v = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 11 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 
                            STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).FirstOrDefaultAsync();
                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                                rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }
                           



                        }
                        //this is security officer section
                        if ((WORKFLOWID == 4 || WORKFLOWID == 11) && ViewBag.NOT_RELATED_STATION != true)
                        {

                            //security officer should redirect the complete request to permits cell for printing 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 11 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                                rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = true;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }

                           

                        }

                        if (permit == "16")
                        {
                            rA42_PERMITS_DTL.DATE_FROM = DateTime.Today;
                            rA42_PERMITS_DTL.DATE_TO = DateTime.Today;

                        }
                        
                    }
                    else
                    {
                        
                        if (WORKFLOWID == 11)
                        {

                            rA42_PERMITS_DTL.REJECTED = false;
                            rA42_PERMITS_DTL.STATUS = false;
                            rA42_PERMITS_DTL.ISOPENED = false;


                        }
                        //this section is for autho person 
                        if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                        {
                            //he should redirect the request to permits cell 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 11 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).FirstOrDefault();
                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                            rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                            rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                            rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                            rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                            rA42_PERMITS_DTL.REJECTED = false;
                            rA42_PERMITS_DTL.STATUS = true;
                            rA42_PERMITS_DTL.ISOPENED = true;
                            string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                            rA42_PERMITS_DTL.BARCODE = barcode;
                        }

                        //permits cell section
                        if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                        {
                            //they should redirect the permit to the securiy officer to approve the permit 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 11 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = true;
                                rA42_PERMITS_DTL.ISOPENED = true;
                                string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                                rA42_PERMITS_DTL.BARCODE = barcode;
                            }

                           
                        }
                        //this is security officer section
                        if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                        {

                            //security officer should redirect the complete request to permits cell for printing 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 11 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Create", new { type = type, station = station, permit = permit });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                                rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = true;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }

                           

                        }
                    }

                }
                rA42_PERMITS_DTL.CRD_BY = currentUser;
                rA42_PERMITS_DTL.CRD_DT = DateTime.Now;
                rA42_PERMITS_DTL.UPD_BY = currentUser;
                rA42_PERMITS_DTL.UPD_DT = DateTime.Now;
                if (rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE != null && rA42_PERMITS_DTL.CARD_FOR_CODE != null && rA42_PERMITS_DTL.ACCESS_TYPE_CODE != null)
                {
                    db.RA42_PERMITS_DTL.Add(rA42_PERMITS_DTL);
                    await db.SaveChangesAsync();
                }
                else
                {
                    AddToast(new Toast("",
                   GetResourcesValue("error_create_message"),
                   "red"));
                    
                    TempData["Erorr"] = Resources.Common.ResourceManager.GetString("error_create_message" + "_" + ViewBag.lang); ;
                    return RedirectToAction("Create", new { type = type, station = station, permit = permit });
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
                        rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = rA42_PERMITS_DTL.ACCESS_TYPE_CODE;
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE;
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


                                    fileName = "FileNO" + c + "_" + rA42_PERMITS_DTL.ACCESS_TYPE_CODE+"_"+permit + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);


                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = rA42_PERMITS_DTL.ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE,
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
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.ACCESS_TYPE_CODE == rA42_PERMITS_DTL.ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                    return RedirectToAction("Create", new { type = type, station = station, permit = permit });
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
                            rA42_MEMBERS_DTL.ACCESS_TYPE_CODE = rA42_PERMITS_DTL.ACCESS_TYPE_CODE;
                            rA42_MEMBERS_DTL.ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE;
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

                                        fileName = "Relative_Profile_" + rA42_PERMITS_DTL.ACCESS_TYPE_CODE+"_"+permit + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                                        // store the file inside ~/Files/Profiles/ folder
                                        bool check = general.ResizeImage(RELATIVE_IMAGE[i], fileName);

                                        if (check != true)
                                        {
                                            AddToast(new Toast("",
                                           GetResourcesValue("error_update_message"),
                                           "red"));
                                            TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                            return RedirectToAction("Create", new { type = type, station = station, permit = permit });
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
                                        return RedirectToAction("Create", new { type = type, station = station, permit = permit });
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
                        return RedirectToAction("Create", new { type = type, station = station, permit = permit });
                    }
                }
                AddToast(new Toast("",
                    GetResourcesValue("success_create_message"),
                    "green"));
                if (WORKFLOWID == 11 && RnoVisitors.Contains(permit) && permit !="12")
                {
                    return RedirectToAction("RnoTempPermitsManage", "Permitsdtl");

                }
                return RedirectToAction("Index","Mypasses");
			}


            return RedirectToAction("Create", new { type = type, station = station, permit = permit });
        }

        //edit companies permits
        [HttpGet]
        public async Task<ActionResult> CompanyPermitEdit(int? id, string tab)
        {

            ViewBag.activetab = tab;
            if (id == null)
            {
                return NotFound();
            }
            RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL = await db.RA42_COMPANY_PASS_DTL.FindAsync(id);
            if (rA42_COMPANY_PASS_DTL == null)
            {
                return NotFound();
            }
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != rA42_COMPANY_PASS_DTL.STATION_CODE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //check authority to proccess this request 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_COMPANY_PASS_DTL.SERVICE_NUMBER != currentUser)
                {

                    return NotFound();

                }
                if (rA42_COMPANY_PASS_DTL.ISOPENED == true)
                {

                    if (rA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && rA42_COMPANY_PASS_DTL.REJECTED == true)
                    {

                    }
                    else
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
                    if (ViewBag.RESPO_STATE != rA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE)
                    {

                        return NotFound();

                    }
                }
            }
            if (Language.GetCurrentLang() == "en")
                {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == FORCE_CODE), "FORCE_ID", "FORCE_NAME_E");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE), "ACCESS_TYPE_CODE", "ACCESS_TYPE_E");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.CARD_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE), "CARD_FOR_CODE", "CARD_FOR_E");
                //company in english
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == rA42_COMPANY_PASS_DTL.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_COMPANY_PASS_DTL.COMPANY_CODE);
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == rA42_COMPANY_PASS_DTL.STATION_CODE), "STATION_CODE", "STATION_NAME_E");
                //sections
                ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == rA42_COMPANY_PASS_DTL.STATION_CODE), "SECTION_NAME", "SECTION_NAME");

                //get zones and gates in english 
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");

                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE,
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get documents types for this kind of permit in arabic
                var doc1 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                            join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                            join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                            join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                            where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "24" && d.DLT_STS != true && z.DLT_STS != true
                            select new
                            {
                                FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                            }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_SUB_TYPES_WITHCAR = new SelectList(doc1, "FILE_TYPE_CODE", "FILE_TYPE");
                //get documents types for this kind of permit in arabic
                var doc2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                            join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                            join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                            join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                            where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "29" && d.DLT_STS != true && z.DLT_STS != true
                            select new
                            {
                                FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                            }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_SUB_TYPES_WITHOUT_CAR = new SelectList(doc2, "FILE_TYPE_CODE", "FILE_TYPE");
                ////get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
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
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == FORCE_CODE), "FORCE_ID", "FORCE_NAME_A");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE), "ACCESS_TYPE_CODE", "ACCESS_TYPE");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.CARD_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE), "CARD_FOR_CODE", "CARD_FOR_A");
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == rA42_COMPANY_PASS_DTL.STATION_CODE), "STATION_CODE", "STATION_NAME_A");
                //company in english
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == rA42_COMPANY_PASS_DTL.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME",rA42_COMPANY_PASS_DTL.COMPANY_CODE);
                //sections
                ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == rA42_COMPANY_PASS_DTL.STATION_CODE), "SECTION_NAME", "SECTION_NAME");
                //zons in arabic
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                      
                //get documents types for this kind of permit in arabic
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get documents types for this kind of permit in arabic
                var doc1 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                            join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                            join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                            join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                            where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "24" && d.DLT_STS != true && z.DLT_STS != true
                            select new
                            {
                                FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                            }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_SUB_TYPES_WITHCAR = new SelectList(doc1, "FILE_TYPE_CODE", "FILE_TYPE");
                //get documents types for this kind of permit in arabic
                var doc2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                            join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                            join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                            join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                            where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "29" && d.DLT_STS != true && z.DLT_STS != true
                            select new
                            {
                                FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                            }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_SUB_TYPES_WITHOUT_CAR = new SelectList(doc2, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
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

            User user = null;
            Task<User> callTask = Task.Run(
                () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                );
            callTask.Wait();
            user = callTask.Result;
            ViewBag.JundStop = "";
            if (user == null)
            {
                ViewBag.JundStop = Resources.Common.ResourceManager.GetString("jund_error" + "_" + ViewBag.lang);
            }
            //get employees of this permit 
            ViewBag.GetEmployees = await db.RA42_PERMITS_DTL.Where(a => a.COMPANY_PASS_CODE == id && a.DLT_STS != true).ToListAsync();
            bool check_general_permit = false;
            int gid = 0;
            foreach (var item in ViewBag.GetEmployees)
            {
                ViewBag.Permit_code_emp = item.PERMIT_CODE;
                if(item.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "25")
                {
                    check_general_permit = true;
                }
            }
            ViewBag.Is_General = check_general_permit;
            //get selected zones and gates
            ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).ToListAsync();
            //get selected documenst 
            ViewBag.GetFiles = await db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CONTROLLER_NAME.Equals("Companies")).ToListAsync();
            //get comments of the request
            var cOMMENTS = await db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).ToListAsync();
            ViewBag.COMMENTS = cOMMENTS;
            //get documenst for this kind of permit to check missing documenst and make compare
            // ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();

            ViewBag.PassIcon = rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ICON;
            ViewBag.PassAccessName = rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_TYPE;
            ViewBag.CompanyPassCode = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE;
            ViewBag.CRDBY = rA42_COMPANY_PASS_DTL.CRD_BY;
            ViewBag.CREJECTED = rA42_COMPANY_PASS_DTL.REJECTED;
            ViewBag.CSTATUS = rA42_COMPANY_PASS_DTL.STATUS;
            ViewBag.CISOPENED = rA42_COMPANY_PASS_DTL.ISOPENED;
            var model = new GroupPermitsForCompanies();
            model.ACCESS_TYPE_CODE = rA42_COMPANY_PASS_DTL.ACCESS_TYPE_CODE;
            //model.RESPONSIBLE = rA42_COMPANY_PASS_DTL.RESPONSIBLE;
            model.REMARKS = rA42_COMPANY_PASS_DTL.REMARKS;
            model.PURPOSE_OF_PASS = rA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS;
            model.CARD_FOR_CODE = rA42_COMPANY_PASS_DTL.CARD_FOR_CODE;
            model.STATION_CODE = rA42_COMPANY_PASS_DTL.STATION_CODE;
            model.WORKFLOW_RESPO_CODE = rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE;
            model.BARCODE = rA42_COMPANY_PASS_DTL.BARCODE;
            model.COMPANY_CODE = rA42_COMPANY_PASS_DTL.COMPANY_CODE;
            model.UNIT_A = rA42_COMPANY_PASS_DTL.RESPONSIBLE;
            
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CompanyPermitEdit(GroupPermitsForCompanies groupPermitsForCompanies, int[] ZONE, int[] SUB_ZONE,
         int[] FILE_TYPES, string[] FILE_TYPES_TEXT, int[] FILE_INDEX, int[] FILE_SUB_TYPES, string[] FILE_SUB_TYPES_TEXT, string[] is_with_car,
          HttpPostedFileBase[] files, IEnumerable<HttpPostedFileBase>[] subfiles, HttpPostedFileBase[] PERSONAL_IMAGE, int[] IDENTITY_CODE, int[] GENDER_ID, int[] PASS_TYPE_CODE,
          string[] CIVIL_NUMBER, string[] NAME_A, string[] NAME_E, string[] PROFESSION_A, string[] PROFESSION_E,
          int[] VECHILE_CODE, int[] VECHILE_NAME_CODE, int[] PLATE_CODE, string[] PLATE_NUMBER, int[] PLATE_CHAR_CODE,
          int[] VECHILE_COLOR_CODE, DateTime[] DATE_FROM, DateTime[] DATE_TO, string[] GSM, string[] WORK_PLACE,
          string[] ADDRESS, DateTime[] CARD_EXPIRED_DATE, string[] REMARKS_SUB, int[] INDEX_NUM,
          FormCollection form, string COMMENT, string tab, int COMPANY_PASS_CODE)
        {
           
            ViewBag.activetab = tab;
            ViewBag.Managepasses = "";
            STATION_CODE = groupPermitsForCompanies.STATION_CODE.Value;
            ACCESS_TYPE_CODE = groupPermitsForCompanies.ACCESS_TYPE_CODE.Value;

            var CompanyPermitDtl = db.RA42_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == COMPANY_PASS_CODE).FirstOrDefault();
            //get station name 
            var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();
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
                ViewBag.HQ_UNIT = STATION_CODE;
                FORCE_CODE = check_unit.RA42_FORCES_MST.FORCE_CODE.ToString();
                ViewBag.Selected_Station = STATION_CODE;
                ViewBag.Selected_Force = FORCE_CODE;

            }


            //get employees of this permit 
            ViewBag.GetEmployees = await db.RA42_PERMITS_DTL.Where(a => a.COMPANY_PASS_CODE == COMPANY_PASS_CODE && a.DLT_STS != true).ToListAsync();
            bool check_general_permit = false;
            int gid = 0;
            foreach (var item in ViewBag.GetEmployees)
            {
                ViewBag.Permit_code_emp = item.PERMIT_CODE;
                if (item.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "25")
                {
                    check_general_permit = true;
                }
            }
            ViewBag.Is_General = check_general_permit;
            //get selected zones and gates
            ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).ToListAsync();
            //get selected documenst 
            ViewBag.GetFiles = await db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CONTROLLER_NAME.Equals("Companies")).ToListAsync();
            //get comments of the request
            var cOMMENTS = await db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).ToListAsync();
            ViewBag.COMMENTS = cOMMENTS;
            //get documenst for this kind of permit to check missing documenst and make compare
            // ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == CompanyPermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == CompanyPermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();

            ViewBag.PassIcon = CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ICON;
            ViewBag.PassAccessName = CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_TYPE;
            ViewBag.CompanyPassCode = CompanyPermitDtl.COMPANY_PASS_CODE;
            ViewBag.CRDBY = CompanyPermitDtl.CRD_BY;
            ViewBag.CREJECTED = CompanyPermitDtl.REJECTED;
            ViewBag.CSTATUS = CompanyPermitDtl.STATUS;
            ViewBag.CISOPENED = CompanyPermitDtl.ISOPENED;

            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            if (Language.GetCurrentLang() == "en")
            {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == FORCE_CODE), "FORCE_ID", "FORCE_NAME_E");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE), "ACCESS_TYPE_CODE", "ACCESS_TYPE_E");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.CARD_SECRET_CODE == CompanyPermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE), "CARD_FOR_CODE", "CARD_FOR_E");
                //company in english
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", groupPermitsForCompanies.COMPANY_CODE);
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "STATION_CODE", "STATION_NAME_E", groupPermitsForCompanies.STATION_CODE);
                //sections
                ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "SECTION_NAME", "SECTION_NAME", groupPermitsForCompanies.UNIT_A);

                //get zones and gates in english 
                switch (CompanyPermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == CompanyPermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE,
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get documents types for this kind of permit in arabic
                var doc1 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                            join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                            join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                            join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                            where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "24" && d.DLT_STS != true && z.DLT_STS != true
                            select new
                            {
                                FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                            }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_SUB_TYPES_WITHCAR = new SelectList(doc1, "FILE_TYPE_CODE", "FILE_TYPE");
                //get documents types for this kind of permit in arabic
                var doc2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                            join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                            join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                            join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                            where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "29" && d.DLT_STS != true && z.DLT_STS != true
                            select new
                            {
                                FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                            }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_SUB_TYPES_WITHOUT_CAR = new SelectList(doc2, "FILE_TYPE_CODE", "FILE_TYPE");
                ////get autho person for this kind of permit in english (المنسق الأمني)
                var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true 
                && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", groupPermitsForCompanies.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {

                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return RedirectToAction("CompanyPermitEdit", new { id = CompanyPermitDtl.COMPANY_PASS_CODE, tab =tab });

                    }
                }
            }
            else
            {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == FORCE_CODE), "FORCE_ID", "FORCE_NAME_A");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE), "ACCESS_TYPE_CODE", "ACCESS_TYPE");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.CARD_SECRET_CODE == CompanyPermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE), "CARD_FOR_CODE", "CARD_FOR_A");
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "STATION_CODE", "STATION_NAME_A");
                //company in english
                ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", groupPermitsForCompanies.COMPANY_CODE);
                //sections
                ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "SECTION_NAME", "SECTION_NAME", groupPermitsForCompanies.UNIT_A);

                //get zones and gates in arabic 
                switch (CompanyPermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }
                //get documents types for this kind of permit in arabic
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == CompanyPermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get documents types for this kind of permit in arabic
                var doc1 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                            join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                            join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                            join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                            where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "24" && d.DLT_STS != true && z.DLT_STS != true
                            select new
                            {
                                FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                            }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_SUB_TYPES_WITHCAR = new SelectList(doc1, "FILE_TYPE_CODE", "FILE_TYPE");
                //get documents types for this kind of permit in arabic
                var doc2 = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                            join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                            join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                            join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                            where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "29" && d.DLT_STS != true && z.DLT_STS != true
                            select new
                            {
                                FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                                FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                            }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_SUB_TYPES_WITHOUT_CAR = new SelectList(doc2, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE
                && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", groupPermitsForCompanies.WORKFLOW_RESPO_CODE);

                if (WORKFLOW_RESPO.Count == 0)
                {
                    //show error message if there is no autho person, when current user workflow id is one or less than one 
                    if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                    {

                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                        return RedirectToAction("CompanyPermitEdit", new { id = CompanyPermitDtl.COMPANY_PASS_CODE, tab = tab});

                    }
                }
            }

            User user = null;
            Task<User> callTask = Task.Run(
                () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                );
            callTask.Wait();
            user = callTask.Result;
            ViewBag.JundStop = "";
            if (user == null)
            {
                ViewBag.JundStop = @Resources.Common.ResourceManager.GetString("jund_error" + "_" + ViewBag.lang);
            }


            if (ModelState.IsValid)
            {
                try
                {


                    CompanyPermitDtl.SERVICE_NUMBER = CompanyPermitDtl.SERVICE_NUMBER;
                    CompanyPermitDtl.STATION_CODE = CompanyPermitDtl.STATION_CODE;
                    CompanyPermitDtl.ACCESS_TYPE_CODE = CompanyPermitDtl.ACCESS_TYPE_CODE;
                    CompanyPermitDtl.CARD_FOR_CODE = CompanyPermitDtl.CARD_FOR_CODE;
                    //CompanyPermitDtl.RESPONSIBLE = groupPermitsForCompanies.UNIT_A;
                    CompanyPermitDtl.COMPANY_TYPE_CODE = 2;
                    CompanyPermitDtl.COMPANY_CODE = groupPermitsForCompanies.COMPANY_CODE;
                    CompanyPermitDtl.PURPOSE_OF_PASS = groupPermitsForCompanies.PURPOSE_OF_PASS;
                    CompanyPermitDtl.REMARKS = groupPermitsForCompanies.REMARKS;
                    CompanyPermitDtl.CRD_BY = CompanyPermitDtl.CRD_BY;
                    CompanyPermitDtl.CRD_DT = CompanyPermitDtl.CRD_DT;
                    CompanyPermitDtl.UPD_BY = currentUser;
                    CompanyPermitDtl.UPD_DT = DateTime.Now;
                    CompanyPermitDtl.BARCODE = groupPermitsForCompanies.BARCODE;
                    CompanyPermitDtl.COMPANY_PASS_CODE = CompanyPermitDtl.COMPANY_PASS_CODE;

                    if (form["approvebtn"] != null)
                    {

                        if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                        {

                            CompanyPermitDtl.WORKFLOW_RESPO_CODE = groupPermitsForCompanies.WORKFLOW_RESPO_CODE;
                            CompanyPermitDtl.REJECTED = false;
                            CompanyPermitDtl.STATUS = false;
                            CompanyPermitDtl.ISOPENED = false;
                        }
                        //this section is for autho person 
                        if (WORKFLOWID == 2)
                        {
                            //he should redirect this request to the permits cell 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("CompanyPermitEdit", new { id = CompanyPermitDtl.COMPANY_PASS_CODE, tab = tab });

                            }
                            else
                            {
                                groupPermitsForCompanies.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                CompanyPermitDtl.APPROVAL_SN = currentUser;
                                CompanyPermitDtl.APPROVAL_RANK = ViewBag.RNK;
                                CompanyPermitDtl.APPROVAL_NAME = ViewBag.FULL_NAME;
                                CompanyPermitDtl.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                CompanyPermitDtl.REJECTED = false;
                                CompanyPermitDtl.STATUS = false;
                                CompanyPermitDtl.ISOPENED = false;
                            }


                        }
                        //this section is for permits cell
                        if (WORKFLOWID == 3)
                        {
                            if (CompanyPermitDtl.STATUS == true)
                            {
                            }
                            else
                            {
                                //permits cell should redirect the request for the security officer 
                                var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                if (v == null)
                                {
                                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                    return RedirectToAction("CompanyPermitEdit", new { id = CompanyPermitDtl.COMPANY_PASS_CODE, tab = tab });

                                }
                                else
                                {
                                    groupPermitsForCompanies.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                    CompanyPermitDtl.PERMIT_SN = currentUser;
                                    CompanyPermitDtl.PERMIT_RANK = ViewBag.RNK;
                                    CompanyPermitDtl.PERMIT_NAME = ViewBag.FULL_NAME;
                                    CompanyPermitDtl.PERMIT_APPROVISION_DATE = DateTime.Now;
                                    CompanyPermitDtl.REJECTED = false;
                                    CompanyPermitDtl.STATUS = false;
                                    CompanyPermitDtl.ISOPENED = true;
                                }
                            }


                        }
                        //this section is for security officer 
                        if (WORKFLOWID == 4)
                        {
                            //afetr he create and complete the request, the request will redirected for the permit cell for printing 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("CompanyPermitEdit", new { id = CompanyPermitDtl.COMPANY_PASS_CODE, tab = tab });

                            }
                            else
                            {
                                groupPermitsForCompanies.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                CompanyPermitDtl.AUTHO_SN = currentUser;
                                CompanyPermitDtl.AUTHO_RANK = ViewBag.RNK;
                                CompanyPermitDtl.AUTHO_NAME = ViewBag.FULL_NAME;
                                CompanyPermitDtl.AUTHO_APPROVISION_DATE = DateTime.Now;
                                CompanyPermitDtl.REJECTED = false;
                                CompanyPermitDtl.STATUS = true;
                                CompanyPermitDtl.ISOPENED = true;
                            }


                        }
                    }
                    else
                    {
                        if (form["rejectbtn"] != null)
                        {
                            if (WORKFLOWID == 2)
                            {
                                CompanyPermitDtl.REJECTED = true;
                                CompanyPermitDtl.ISOPENED = false;
                            }
                            if (WORKFLOWID == 3)
                            {
                                var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == CompanyPermitDtl.APPROVAL_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                                if (v != null)
                                {
                                    CompanyPermitDtl.REJECTED = true;
                                    groupPermitsForCompanies.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                                }
                                else
                                {
                                    CompanyPermitDtl.REJECTED = true;
                                    CompanyPermitDtl.ISOPENED = false;
                                }
                            }

                            if (WORKFLOWID == 4)
                            {
                                var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == CompanyPermitDtl.PERMIT_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                                if (v != null)
                                {
                                    CompanyPermitDtl.REJECTED = true;
                                    groupPermitsForCompanies.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                                }
                                else
                                {
                                    CompanyPermitDtl.REJECTED = true;
                                }

                                groupPermitsForCompanies.BARCODE = null;
                            }
                        }
                    }
                    if (groupPermitsForCompanies.WORKFLOW_RESPO_CODE != null && CompanyPermitDtl.CARD_FOR_CODE != null && CompanyPermitDtl.ACCESS_TYPE_CODE != null)
                    {
                        try
                        {
                            db.Entry(CompanyPermitDtl).CurrentValues.SetValues(groupPermitsForCompanies);
                            //db.Entry(rA42_PERMITS_DTL).State = EntityState.Modified;
                            await db.SaveChangesAsync();

                            var PermitsDtl = db.RA42_PERMITS_DTL.Where(a => a.COMPANY_PASS_CODE == CompanyPermitDtl.COMPANY_PASS_CODE && a.DLT_STS != true).ToList();

                            foreach(var item in PermitsDtl)
                            {
                                item.STATUS = CompanyPermitDtl.STATUS;
                                item.REJECTED = CompanyPermitDtl.REJECTED;
                                item.APPROVAL_SN = CompanyPermitDtl.APPROVAL_SN;
                                item.APPROVAL_RANK = CompanyPermitDtl.APPROVAL_RANK;
                                item.APPROVAL_NAME = CompanyPermitDtl.APPROVAL_NAME;
                                item.APPROVAL_APPROVISION_DATE = CompanyPermitDtl.APPROVAL_APPROVISION_DATE;
                                item.PERMIT_SN = CompanyPermitDtl.PERMIT_SN;
                                item.PERMIT_RANK = CompanyPermitDtl.PERMIT_RANK;
                                item.PERMIT_NAME = CompanyPermitDtl.PERMIT_NAME;
                                item.PERMIT_APPROVISION_DATE = CompanyPermitDtl.PERMIT_APPROVISION_DATE;
                                item.AUTHO_SN = CompanyPermitDtl.AUTHO_SN;
                                item.AUTHO_RANK = CompanyPermitDtl.AUTHO_RANK;
                                item.AUTHO_NAME = CompanyPermitDtl.AUTHO_NAME;
                                item.AUTHO_APPROVISION_DATE = CompanyPermitDtl.AUTHO_APPROVISION_DATE;
                                item.WORKFLOW_RESPO_CODE = CompanyPermitDtl.WORKFLOW_RESPO_CODE;
                                item.ISOPENED = CompanyPermitDtl.ISOPENED;
                                item.CRD_BY = CompanyPermitDtl.CRD_BY;
                                item.CRD_DT =CompanyPermitDtl.CRD_DT;
                                item.UPD_BY = currentUser;
                                item.UPD_DT = DateTime.Now;

                                db.Entry(item).State = EntityState.Modified;
                                db.SaveChanges();
                            }
                        }
                        catch (Exception ex)
                        {
                            TempData["Erorr"] = ex.GetBaseException().Message;
                            return RedirectToAction("CompanyPermitEdit", new { id = CompanyPermitDtl.COMPANY_PASS_CODE, tab = tab });

                        }
                    }
                    else
                    {
                        AddToast(new Toast("",
                        GetResourcesValue("error_update_message"),
                        "red"));
                        TempData["Erorr"] = Resources.Common.ResourceManager.GetString("error_create_message" + "_" + ViewBag.lang); ;
                        return RedirectToAction("CompanyPermitEdit", new { id = CompanyPermitDtl.COMPANY_PASS_CODE, tab = tab });
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
                                rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = groupPermitsForCompanies.ACCESS_TYPE_CODE;
                                rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = CompanyPermitDtl.COMPANY_PASS_CODE;
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

                                            fileName = "FileNO" + c + "_" + groupPermitsForCompanies.ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                            // store the file inside ~/App_Data/uploads folder
                                            string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                            file.SaveAs(path);
                                            //add file name to db table 
                                            RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                            {
                                                ACCESS_TYPE_CODE = groupPermitsForCompanies.ACCESS_TYPE_CODE,
                                                ACCESS_ROW_CODE = CompanyPermitDtl.COMPANY_PASS_CODE,
                                                FILE_TYPE = FILE_TYPES[c],
                                                FILE_TYPE_TEXT = FILE_TYPES_TEXT[c],
                                                CONTROLLER_NAME = "Companies",
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
                                            var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == CompanyPermitDtl.COMPANY_PASS_CODE && a.ACCESS_TYPE_CODE == groupPermitsForCompanies.ACCESS_TYPE_CODE && a.CONTROLLER_NAME.Equals("Companies")).ToList();
                                            foreach (var del in delete)
                                            {
                                                string filpath = "~/Documents/" + del.FILE_NAME;
                                                general.RemoveFileFromServer(filpath);
                                                db.RA42_FILES_MST.Remove(del);
                                                db.SaveChanges();
                                            }
                                            TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                            return RedirectToAction("CompanyPermitEdit", new { id = CompanyPermitDtl.COMPANY_PASS_CODE, tab = tab });
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
                        //add workers
                        if (IDENTITY_CODE != null)
                        {
                            RA42_PERMITS_DTL rA42_TEMPRORY_COMPANY_PASS_DTL = new RA42_PERMITS_DTL();

                            for (int j = 0; j < IDENTITY_CODE.Length; j++)
                            {
                                //create barcode for every worker
                                string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                                rA42_TEMPRORY_COMPANY_PASS_DTL.COMPANY_PASS_CODE = CompanyPermitDtl.COMPANY_PASS_CODE;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.ACCESS_TYPE_CODE = groupPermitsForCompanies.ACCESS_TYPE_CODE;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.STATION_CODE = groupPermitsForCompanies.STATION_CODE;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.COMPANY_CODE = groupPermitsForCompanies.COMPANY_CODE;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.RESPONSIBLE = CompanyPermitDtl.SERVICE_NUMBER;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.UNIT_A = groupPermitsForCompanies.UNIT_A;
                                if (is_with_car[j].Equals("yes"))
                                {
                                    rA42_TEMPRORY_COMPANY_PASS_DTL.VECHILE_CODE = VECHILE_CODE[j];
                                    rA42_TEMPRORY_COMPANY_PASS_DTL.VECHILE_NAME_CODE = VECHILE_NAME_CODE[j];
                                    rA42_TEMPRORY_COMPANY_PASS_DTL.VECHILE_COLOR_CODE = VECHILE_COLOR_CODE[j];
                                    rA42_TEMPRORY_COMPANY_PASS_DTL.PLATE_CHAR_CODE = PLATE_CHAR_CODE[j];
                                    rA42_TEMPRORY_COMPANY_PASS_DTL.PLATE_CODE = PLATE_CODE[j];
                                    rA42_TEMPRORY_COMPANY_PASS_DTL.PLATE_NUMBER = PLATE_NUMBER[j];
                                    rA42_TEMPRORY_COMPANY_PASS_DTL.CARD_FOR_CODE = 24;
                                }
                                else
                                {
                                    rA42_TEMPRORY_COMPANY_PASS_DTL.VECHILE_CODE = null;
                                    rA42_TEMPRORY_COMPANY_PASS_DTL.VECHILE_NAME_CODE = null;
                                    rA42_TEMPRORY_COMPANY_PASS_DTL.VECHILE_COLOR_CODE = null;
                                    rA42_TEMPRORY_COMPANY_PASS_DTL.PLATE_CHAR_CODE = null;
                                    rA42_TEMPRORY_COMPANY_PASS_DTL.PLATE_CODE = null;
                                    rA42_TEMPRORY_COMPANY_PASS_DTL.PLATE_NUMBER = null;
                                    rA42_TEMPRORY_COMPANY_PASS_DTL.CARD_FOR_CODE = 29;

                                }
                                rA42_TEMPRORY_COMPANY_PASS_DTL.IDENTITY_CODE = IDENTITY_CODE[j];
                                rA42_TEMPRORY_COMPANY_PASS_DTL.GENDER_ID = GENDER_ID[j];
                                rA42_TEMPRORY_COMPANY_PASS_DTL.PASS_TYPE_CODE = PASS_TYPE_CODE[j];
                                rA42_TEMPRORY_COMPANY_PASS_DTL.CIVIL_NUMBER = CIVIL_NUMBER[j];
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
                                rA42_TEMPRORY_COMPANY_PASS_DTL.REMARKS = REMARKS_SUB[j];
                                if (PERSONAL_IMAGE[j] != null)
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
                                                    return RedirectToAction("CompanyPermitEdit", new { id = CompanyPermitDtl.COMPANY_PASS_CODE, tab = tab });
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
                                                return RedirectToAction("CompanyPermitEdit", new { id = CompanyPermitDtl.COMPANY_PASS_CODE, tab = tab });
                                            }
                                        }
                                    }

                                    catch (Exception ex)
                                    {
                                        ex.GetBaseException();
                                    }
                                }
                                rA42_TEMPRORY_COMPANY_PASS_DTL.PURPOSE_OF_PASS = groupPermitsForCompanies.PURPOSE_OF_PASS;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.APPROVAL_SN = CompanyPermitDtl.APPROVAL_SN;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.APPROVAL_RANK = CompanyPermitDtl.APPROVAL_RANK;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.APPROVAL_NAME = CompanyPermitDtl.APPROVAL_NAME;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.APPROVAL_APPROVISION_DATE = CompanyPermitDtl.APPROVAL_APPROVISION_DATE;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.PERMIT_SN = CompanyPermitDtl.PERMIT_SN;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.PERMIT_RANK = CompanyPermitDtl.PERMIT_RANK;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.PERMIT_NAME = CompanyPermitDtl.PERMIT_NAME;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.PERMIT_APPROVISION_DATE = CompanyPermitDtl.PERMIT_APPROVISION_DATE;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.AUTHO_SN = CompanyPermitDtl.AUTHO_SN;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.AUTHO_RANK = CompanyPermitDtl.AUTHO_RANK;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.AUTHO_NAME = CompanyPermitDtl.AUTHO_NAME;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.AUTHO_APPROVISION_DATE = CompanyPermitDtl.AUTHO_APPROVISION_DATE;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = CompanyPermitDtl.WORKFLOW_RESPO_CODE;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.REJECTED = CompanyPermitDtl.REJECTED;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.STATUS = CompanyPermitDtl.STATUS;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.ISOPENED = CompanyPermitDtl.ISOPENED;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.CRD_BY = currentUser;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.CRD_DT = DateTime.Now;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.UPD_BY = currentUser;
                                rA42_TEMPRORY_COMPANY_PASS_DTL.UPD_DT = DateTime.Now;
                                db.RA42_PERMITS_DTL.Add(rA42_TEMPRORY_COMPANY_PASS_DTL);
                                db.SaveChanges();

                                //add selected documents
                                if (subfiles[j] != null)
                                {

                                    try
                                    {
                                        //create foreach loop to upload multiple files 
                                        int c = 0;
                                        foreach (HttpPostedFileBase file in subfiles[j])
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

                                                    fileName = "FileNO" + c + "_" + groupPermitsForCompanies.ACCESS_TYPE_CODE + "_" + rA42_TEMPRORY_COMPANY_PASS_DTL.CARD_FOR_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                                    // store the file inside ~/App_Data/uploads folder
                                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                                    file.SaveAs(path);
                                                    //add file name to db table 

                                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                                    {
                                                        ACCESS_TYPE_CODE = groupPermitsForCompanies.ACCESS_TYPE_CODE,
                                                        ACCESS_ROW_CODE = rA42_TEMPRORY_COMPANY_PASS_DTL.PERMIT_CODE,
                                                        FILE_TYPE = FILE_SUB_TYPES[c],
                                                        FILE_TYPE_TEXT = FILE_SUB_TYPES_TEXT[c],
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
                                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_TEMPRORY_COMPANY_PASS_DTL.PERMIT_CODE && a.ACCESS_TYPE_CODE == groupPermitsForCompanies.ACCESS_TYPE_CODE).ToList();
                                                    foreach (var del in delete)
                                                    {
                                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                                        general.RemoveFileFromServer(filpath);
                                                        db.RA42_FILES_MST.Remove(del);
                                                        db.SaveChanges();
                                                    }
                                                    TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                                    return RedirectToAction("CompanyPermitEdit", new { id = CompanyPermitDtl.COMPANY_PASS_CODE, tab = tab });

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
                            }

                        }
                    if (COMMENT.Length > 0)
                    {
                        RA42_COMMENTS_MST rA42_COMMENT = new RA42_COMMENTS_MST();
                        rA42_COMMENT.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                        rA42_COMMENT.PASS_ROW_CODE = CompanyPermitDtl.COMPANY_PASS_CODE;
                        rA42_COMMENT.CRD_BY = currentUser;
                        rA42_COMMENT.CRD_DT = DateTime.Now;
                        rA42_COMMENT.COMMENT = COMMENT;
                        db.RA42_COMMENTS_MST.Add(rA42_COMMENT);
                        db.SaveChanges();

                    }
                    AddToast(new Toast("",
                           GetResourcesValue("success_update_message"),
                           "green"));

                    if (WORKFLOWID == 11)
                    {
                        return RedirectToAction("RnoTempPermitsManage", "Permitsdtl");

                    }

                    if (ViewBag.RESPO_STATE <= 1)
                    {

                        return RedirectToAction("Index", "MyPasses");
                    }
                    else
                    {
                        return RedirectToAction("Index", new { type = CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE });
                    }
                
                }
                catch (DbEntityValidationException cc)
                {
                    foreach (var validationerrors in cc.EntityValidationErrors)
                    {
                        foreach (var validationerror in validationerrors.ValidationErrors)
                        {
                            TempData["Erorr"] = validationerror.PropertyName + " + " + validationerror.ErrorMessage;
                            return View(groupPermitsForCompanies);
                        }
                    }

                }


            }

            return View(groupPermitsForCompanies);
        }

        // GET: RA42_PERMITS_DTL/Edit/5
        [HttpGet]
        public async Task<ActionResult> Edit(int? id,string tab)
		{
            ViewBag.activetab = tab;

            if (id == null)
			{
                return NotFound();
			}
			RA42_PERMITS_DTL rA42_PERMITS_DTL = await db.RA42_PERMITS_DTL.FindAsync(id);
			if (rA42_PERMITS_DTL == null)
			{
				return NotFound();
			}
            FORCE_CODE = rA42_PERMITS_DTL.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE.ToString();
            var same_workflow = new List<string>() { "8", "2", "5", "7", "9" };
            var RnoVisitors = new List<string>() { "11", "12", "13", "14", "16" };
            if (RnoVisitors.Contains(rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
            {
                ViewBag.Managepasses = "";
                ViewBag.RnoTempPermits = "RnoTempPermits";
            }

            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != rA42_PERMITS_DTL.STATION_CODE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //check authority to proccess this request 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_PERMITS_DTL.SERVICE_NUMBER != currentUser && rA42_PERMITS_DTL.RESPONSIBLE != currentUser)
                {
                   
                        return NotFound();
                    
                }
                if (rA42_PERMITS_DTL.ISOPENED == true)
                {
                    
                    if (rA42_PERMITS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && rA42_PERMITS_DTL.REJECTED == true)
                    {

                    }
                    else
                    {
                           
                           return NotFound();
                            
                    }

                    
                }
            }
            else
            {
                if (rA42_PERMITS_DTL.SERVICE_NUMBER == currentUser || rA42_PERMITS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != rA42_PERMITS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE)
                    {
                        
                            return NotFound();
                        
                    }
                }
            }

            var employees_and_families = new List<string>() { "21", "23", "27", "35", "15" };


            if (Language.GetCurrentLang() == "en")
            {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == rA42_PERMITS_DTL.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE), "FORCE_ID", "FORCE_NAME_E");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE), "ACCESS_TYPE_CODE", "ACCESS_TYPE_E");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.DLT_STS != true && a.CARD_SECRET_CODE == rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE), "CARD_FOR_CODE", "CARD_FOR_E");
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE), "STATION_CODE", "STATION_NAME_E");
                //get bloods
                ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE", rA42_PERMITS_DTL.BLOOD_CODE);
                //get events and excrises in english
                ViewBag.EVENT_EXERCISE_CODE = new SelectList(db.RA42_EVENT_EXERCISE_MST.Where(a => a.DLT_STS != true && a.ACTIVE != false && a.DATE_TO <= DateTime.Today), "EVENT_EXERCISE_CODE", "EVENT_EXERCISE_NAME_E", rA42_PERMITS_DTL.EVENT_EXERCISE_CODE);

                //get company name
                if (rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6)
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 1), "COMPANY_CODE", "COMPANY_NAME_E",rA42_PERMITS_DTL.COMPANY_CODE);

                }
                else
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E",rA42_PERMITS_DTL.COMPANY_CODE);
                }                
                //get realtives types in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_PERMITS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_PERMITS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_PERMITS_DTL.IDENTITY_CODE);
                //get permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_PERMITS_DTL.PASS_TYPE_CODE);
                if (rA42_PERMITS_DTL.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3")
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                if (RnoVisitors.Contains(rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 2), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plates types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_PERMITS_DTL.PLATE_CODE);
                //get plates chars in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_PERMITS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in english 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_PERMITS_DTL.VECHILE_CODE);
                //get colors of vechiles in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_PERMITS_DTL.VECHILE_COLOR_CODE);
                //get vechiles types in english (صالون - دفع رباعي ....)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_PERMITS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english
                switch (rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }                       //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE ==  FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get responsible person (المنسق الأمني)-- this option will be for applicant to change autho person if he wants
                //get autho person for this kind of permit in english (المنسق الأمني)
                var unique_permits = new List<string> { "1", "6", "10" };
                if (unique_permits.Contains(rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.ToString()))
                {
                    if (rA42_PERMITS_DTL.ACCESS_TYPE_CODE == 10)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            //show error message if there is no autho person, when current user workflow id is one or less than one 
                            if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });
                            }
                        }
                    }
                    if (rA42_PERMITS_DTL.ACCESS_TYPE_CODE == 1)
                    {
                        // here to detrmine who is autho resonsible to proccess to permit
                        int workflow_id = 5;
                        if (ViewBag.RESPO_STATE == 6)
                        {
                            workflow_id = 6;
                        }
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //if WORKFLOW_RESPO.Count == 0 that means no body setting in this position and the system should return error message no body in this posision

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                        }
                    }

                    if (rA42_PERMITS_DTL.ACCESS_TYPE_CODE == 6)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //check if WORKFLOW_RESPO.Count == 0 show error message 
                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                        }
                    }
                }
                if (!unique_permits.Contains(rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.ToString()))
                {
                    var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                    if (WORKFLOW_RESPO.Count == 0)
                    {
                        //show error message if there is no autho person, when current user workflow id is one or less than one 
                        if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                        {
                           
                            if (FORCE_CODE != "3" || !employees_and_families.Contains(rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });
                            }
                        }
                    }
                }
            }
            else
            {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == rA42_PERMITS_DTL.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE), "FORCE_ID", "FORCE_NAME_A");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE), "ACCESS_TYPE_CODE", "ACCESS_TYPE");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.DLT_STS != true && a.CARD_SECRET_CODE == rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE), "CARD_FOR_CODE", "CARD_FOR_A");
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE), "STATION_CODE", "STATION_NAME_A");
                //get bloods
                ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE", rA42_PERMITS_DTL.BLOOD_CODE);
                //get events and excrises in english
                ViewBag.EVENT_EXERCISE_CODE = new SelectList(db.RA42_EVENT_EXERCISE_MST.Where(a => a.DLT_STS != true && a.ACTIVE != false && a.DATE_TO <= DateTime.Today), "EVENT_EXERCISE_CODE", "EVENT_EXERCISE_NAME", rA42_PERMITS_DTL.EVENT_EXERCISE_CODE);
                //get company name
                if (rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6)
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 1), "COMPANY_CODE", "COMPANY_NAME", rA42_PERMITS_DTL.COMPANY_CODE);

                }
                else
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_PERMITS_DTL.COMPANY_CODE);
                }                  
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_PERMITS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_PERMITS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_PERMITS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_PERMITS_DTL.PASS_TYPE_CODE);
                if (rA42_PERMITS_DTL.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3")
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                if (RnoVisitors.Contains(rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 2), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_PERMITS_DTL.PLATE_CODE);
                //get plates chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_PERMITS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_PERMITS_DTL.VECHILE_CODE);
                //get vechiles colors in arabic
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_PERMITS_DTL.VECHILE_COLOR_CODE);
                //get vechiles types (صالون - دفع رباعي ....) in arabic
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_PERMITS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic
                switch (rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                var unique_permits = new List<string> { "1", "6", "10" };
                if (unique_permits.Contains(rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.ToString()))
                {
                    if (rA42_PERMITS_DTL.ACCESS_TYPE_CODE == 10)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            //show error message if there is no autho person, when current user workflow id is one or less than one 
                            if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });
                            }
                        }
                    }
                    if (rA42_PERMITS_DTL.ACCESS_TYPE_CODE == 1)
                    {
                        // here to detrmine who is autho resonsible to proccess to permit
                        int workflow_id = 5;
                        if (ViewBag.RESPO_STATE == 6)
                        {
                            workflow_id = 6;
                        }
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //if WORKFLOW_RESPO.Count == 0 that means no body setting in this position and the system should return error message no body in this posision

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                        }
                    }

                    if (rA42_PERMITS_DTL.ACCESS_TYPE_CODE == 6)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //check if WORKFLOW_RESPO.Count == 0 show error message 
                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                        }
                    }
                }
                if (!unique_permits.Contains(rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.ToString()))
                {
                    var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                    if (WORKFLOW_RESPO.Count == 0)
                    {
                        //show error message if there is no autho person, when current user workflow id is one or less than one 
                        if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                        {
                            if (FORCE_CODE != "3" || !employees_and_families.Contains(rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                            }
                        }
                    }
                }
            }
            //get relatives of this permit 
            ViewBag.GetRelativs = await db.RA42_MEMBERS_DTL.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.CRD_DT > new DateTime(2024, 7, 1)).ToListAsync();
            //get selected zones and gates
            if (rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE != 5)
            {
                ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();
            }
            else
            {
                if (rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "25")
                {
                    ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();

                }
                else
                {
                    ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();
                }
            }            //get selected documenst 
            ViewBag.GetFiles = await db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 7, 1) && !a.CONTROLLER_NAME.Equals("Companies")).ToListAsync();
            //get comments of the request
            //var cOMMENTS = await db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 7, 1)).ToListAsync();
            //ViewBag.COMMENTS = cOMMENTS;

            if (rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE != 5 || rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "25")
            {
                var cOMMENTS = await db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();
                ViewBag.COMMENTS = cOMMENTS;
            }
            else
            {
                var cOMMENTS = await db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_PERMITS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).ToListAsync();
                ViewBag.COMMENTS = cOMMENTS;
            }
            //get documenst for this kind of permit to check missing documenst and make compare
            // ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true 
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();

           
            //get personal image 
            ViewBag.PERSONAL_IMAGE = rA42_PERMITS_DTL.PERSONAL_IMAGE;
            //get status of the request
            ViewBag.STATUS = rA42_PERMITS_DTL.STATUS;
            ViewBag.ISPRINTED = rA42_PERMITS_DTL.ISPRINTED;

            return View(rA42_PERMITS_DTL);
		}

		// POST: RA42_PERMITS_DTL/Edit/5
		// To protect from overposting attacks, enable the specific properties you want to bind to, for 
		// more details see https://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Edit(RA42_PERMITS_DTL rA42_PERMITS_DTL,
            int[] RELATIVE_TYPES, HttpPostedFileBase[] RELATIVE_IMAGE, int[] IDENTITY_TYPES, int[] GENDER_TYPES,
            string[] FULL_NAME, string[] CIVIL_NUM, string[] PASSPORT_NUMBER, string[] PHONE_NUMBER_M
            , string[] REMARKS_LIST, int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES, string is_with_car,
            string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE, FormCollection form, string COMMENT,string tab)
        {
            ViewBag.activetab = tab;

            var PermitDtl = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == rA42_PERMITS_DTL.PERMIT_CODE).FirstOrDefault();

            if(PermitDtl == null)
            {
                return NotFound();
            }
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != rA42_PERMITS_DTL.STATION_CODE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            FORCE_CODE = PermitDtl.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE.ToString();
            STATION_CODE = PermitDtl.STATION_CODE.Value;
            ACCESS_TYPE_CODE = PermitDtl.ACCESS_TYPE_CODE.Value;
            int type = PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.Value;
            string permit = PermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE.ToString();
            var checkSubPermit =  db.RA42_CARD_FOR_MST.Where(a => a.CARD_SECRET_CODE == permit && a.DLT_STS != true).FirstOrDefault();
            if (checkSubPermit != null)
            {

                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.SUB_PERMIT = checkSubPermit.CARD_FOR_E;
                }
                else
                {
                    ViewBag.SUB_PERMIT = checkSubPermit.CARD_FOR_A;

                }
                ViewBag.MAIN_PERMIT_ICON = checkSubPermit.RA42_ACCESS_TYPE_MST.ICON;
                ViewBag.WITH_CAR = checkSubPermit.WITH_CAR;

            }

            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            var same_workflow = new List<string>() { "8", "2", "5", "7", "9" };
            var RnoVisitors = new List<string>() { "11", "12", "13", "14", "16" };
            var not_same_workflow = new List<string>() { "1", "6", "10" };
            var employees_and_families = new List<string>() { "21", "23", "27", "35","15" };

            if (RnoVisitors.Contains(PermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
            {
                ViewBag.Managepasses = "";
                ViewBag.RnoTempPermits = "RnoTempPermits";
            }
            if (Language.GetCurrentLang() == "en")
            {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == FORCE_CODE), "FORCE_ID", "FORCE_NAME_E");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE), "ACCESS_TYPE_CODE", "ACCESS_TYPE_E");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.DLT_STS != true && a.CARD_SECRET_CODE == PermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE), "CARD_FOR_CODE", "CARD_FOR_E");
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == PermitDtl.STATION_CODE), "STATION_CODE", "STATION_NAME_E");
                //get bloods
                ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE", PermitDtl.BLOOD_CODE);
                //get events and excrises in english
                ViewBag.EVENT_EXERCISE_CODE = new SelectList(db.RA42_EVENT_EXERCISE_MST.Where(a => a.DLT_STS != true && a.ACTIVE != false && a.DATE_TO <= DateTime.Today), "EVENT_EXERCISE_CODE", "EVENT_EXERCISE_NAME_E", PermitDtl.EVENT_EXERCISE_CODE);

                //get company name
                if (PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6)
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 1), "COMPANY_CODE", "COMPANY_NAME_E", PermitDtl.COMPANY_CODE);

                }
                else
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == PermitDtl.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", PermitDtl.COMPANY_CODE);
                }
                //get realtives types in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", PermitDtl.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", PermitDtl.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", PermitDtl.IDENTITY_CODE);
                //get permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", PermitDtl.PASS_TYPE_CODE);
                if (FORCE_CODE == "3")
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                if (RnoVisitors.Contains(PermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 2), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plates types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", PermitDtl.PLATE_CODE);
                //get plates chars in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", PermitDtl.PLATE_CHAR_CODE);
                //get vechile catigories in english 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", PermitDtl.VECHILE_CODE);
                //get colors of vechiles in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", PermitDtl.VECHILE_COLOR_CODE);
                //get vechiles types in english (صالون - دفع رباعي ....)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", PermitDtl.VECHILE_NAME_CODE);
                //get zones and gates in english
                switch (PermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == PermitDtl.STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == PermitDtl.STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == PermitDtl.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == PermitDtl.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == PermitDtl.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == PermitDtl.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == PermitDtl.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }                       //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == permit && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get responsible person (المنسق الأمني)-- this option will be for applicant to change autho person if he wants
                //get autho person for this kind of permit in english (المنسق الأمني)
                var unique_permits = new List<string> { "1", "6", "10" };
                if (unique_permits.Contains(PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.ToString()))
                {
                    if (PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10)
                    {
                        var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            //show error message if there is no autho person, when current user workflow id is one or less than one 
                            if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });
                            }
                        }
                    }
                    if (PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 1)
                    {
                        // here to detrmine who is autho resonsible to proccess to permit
                        int workflow_id = 5;
                        if (ViewBag.RESPO_STATE == 6)
                        {
                            workflow_id = 6;
                        }
                        var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //if WORKFLOW_RESPO.Count == 0 that means no body setting in this position and the system should return error message no body in this posision

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                        }
                    }

                    if (PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6)
                    {
                        var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //check if WORKFLOW_RESPO.Count == 0 show error message 
                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                        }
                    }
                }
                if (!unique_permits.Contains(PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.ToString()))
                {
                    var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == PermitDtl.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                    if (WORKFLOW_RESPO.Count == 0)
                    {
                        //show error message if there is no autho person, when current user workflow id is one or less than one 
                        if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                        {
                            if (FORCE_CODE != "3" || !employees_and_families.Contains(permit))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                            }
                        }
                    }
                }
            }
            else
            {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == FORCE_CODE), "FORCE_ID", "FORCE_NAME_A");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE), "ACCESS_TYPE_CODE", "ACCESS_TYPE");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.DLT_STS != true && a.CARD_SECRET_CODE == PermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE), "CARD_FOR_CODE", "CARD_FOR_A");
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == PermitDtl.STATION_CODE), "STATION_CODE", "STATION_NAME_A");
                //get bloods
                ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE", PermitDtl.BLOOD_CODE);
                //get events and excrises in english
                ViewBag.EVENT_EXERCISE_CODE = new SelectList(db.RA42_EVENT_EXERCISE_MST.Where(a => a.DLT_STS != true && a.ACTIVE != false && a.DATE_TO <= DateTime.Today), "EVENT_EXERCISE_CODE", "EVENT_EXERCISE_NAME", PermitDtl.EVENT_EXERCISE_CODE);
                //get company name
                if (PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6)
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 1), "COMPANY_CODE", "COMPANY_NAME", PermitDtl.COMPANY_CODE);

                }
                else
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == PermitDtl.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", PermitDtl.COMPANY_CODE);
                } 
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", PermitDtl.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", PermitDtl.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", PermitDtl.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", PermitDtl.PASS_TYPE_CODE);
                if (FORCE_CODE == "3")
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                if (RnoVisitors.Contains(PermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 2), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", PermitDtl.PLATE_CODE);
                //get plates chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", PermitDtl.PLATE_CHAR_CODE);
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", PermitDtl.VECHILE_CODE);
                //get vechiles colors in arabic
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", PermitDtl.VECHILE_COLOR_CODE);
                //get vechiles types (صالون - دفع رباعي ....) in arabic
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", PermitDtl.VECHILE_NAME_CODE);
                //get zones and gates in arabic
                switch (PermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == PermitDtl.STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == PermitDtl.STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == PermitDtl.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == PermitDtl.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == PermitDtl.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == PermitDtl.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == PermitDtl.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == PermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                var unique_permits = new List<string> { "1", "6", "10" };
                if (unique_permits.Contains(PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.ToString()))
                {
                    if (PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10)
                    {
                        var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            //show error message if there is no autho person, when current user workflow id is one or less than one 
                            if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });
                            }
                        }
                    }
                    if (PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 1)
                    {
                        // here to detrmine who is autho resonsible to proccess to permit
                        int workflow_id = 5;
                        if (ViewBag.RESPO_STATE == 6)
                        {
                            workflow_id = 6;
                        }
                        var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //if WORKFLOW_RESPO.Count == 0 that means no body setting in this position and the system should return error message no body in this posision

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                        }
                    }

                    if (PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6)
                    {
                        var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //check if WORKFLOW_RESPO.Count == 0 show error message 
                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                        }
                    }
                }
                if (!unique_permits.Contains(PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.ToString()))
                {
                    var WORKFLOW_RESPO = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == PermitDtl.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToList();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                    if (WORKFLOW_RESPO.Count == 0)
                    {
                        //show error message if there is no autho person, when current user workflow id is one or less than one 
                        if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                        {
                            if (FORCE_CODE != "3" || !employees_and_families.Contains(permit))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                            }
                        }
                    }
                }
            }
            //get relatives of this permit 
            ViewBag.GetRelativs = await db.RA42_MEMBERS_DTL.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.CRD_DT > new DateTime(2024, 7, 1)).ToListAsync();
            //get selected zones and gates
            if (PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE != 5)
            {
                ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();
            }
            else
            {
                if (PermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "25")
                {
                    ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();

                }
                else
                {
                    ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == PermitDtl.COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();
                }
            }            
            //get selected documenst 
            ViewBag.GetFiles = await db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 7, 1) && !a.CONTROLLER_NAME.Equals("Companies")).ToListAsync();
            //get comments of the request
            //var cOMMENTS = await db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 7, 1)).ToListAsync();
            //ViewBag.COMMENTS = cOMMENTS;

            if (PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE != 5 || PermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "25")
            {
                var cOMMENTS = await db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();
                ViewBag.COMMENTS = cOMMENTS;
            }
            else
            {
                var cOMMENTS = await db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == PermitDtl.COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).ToListAsync();
                ViewBag.COMMENTS = cOMMENTS;
            }
            //get documenst for this kind of permit to check missing documenst and make compare
            // ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == PermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == PermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();

            
                ViewBag.PERSONAL_IMAGE = PermitDtl.PERSONAL_IMAGE;
                //get status of the request
                ViewBag.STATUS = PermitDtl.STATUS;
                ViewBag.ISPRINTED = PermitDtl.ISPRINTED;
           
               
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



                                        fileName = "Profile_" + rA42_PERMITS_DTL.ACCESS_TYPE_CODE + "_" + PermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                                        // store the file inside ~/Files/Profiles/ folder
                                        bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                        if (check != true)
                                        {
                                            AddToast(new Toast("",
                                           GetResourcesValue("error_update_message"),
                                           "red"));
                                            TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                            return View(rA42_PERMITS_DTL);
                                        }

                                        rA42_PERMITS_DTL.PERSONAL_IMAGE = fileName;


                                    }
                                    else
                                    {
                                        //show error if format not supported 
                                        AddToast(new Toast("",
                                        GetResourcesValue("error_update_message"),
                                        "red"));
                                        TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                    return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });
                                }
                            }
                            }
                            catch (Exception ex)
                            {
                                ex.GetBaseException();
                            }
                        }
                    if (rA42_PERMITS_DTL.PERSONAL_IMAGE != null)
                    {
                        rA42_PERMITS_DTL.PERSONAL_IMAGE = rA42_PERMITS_DTL.PERSONAL_IMAGE;
                    }
                    else
                    {
                        rA42_PERMITS_DTL.PERSONAL_IMAGE = PermitDtl.PERSONAL_IMAGE;

                    }
                    if (is_with_car.Equals("no"))
                    {
                        rA42_PERMITS_DTL.VECHILE_CODE = null;
                        rA42_PERMITS_DTL.VECHILE_NAME_CODE = null;
                        rA42_PERMITS_DTL.VECHILE_COLOR_CODE = null;
                        rA42_PERMITS_DTL.PLATE_CHAR_CODE = null;
                        rA42_PERMITS_DTL.PLATE_CODE = null;
                        rA42_PERMITS_DTL.PLATE_NUMBER = null;

                    }
                    else
                    {
                        if (!rA42_PERMITS_DTL.VECHILE_CODE.HasValue || !rA42_PERMITS_DTL.VECHILE_COLOR_CODE.HasValue
                            || !rA42_PERMITS_DTL.VECHILE_NAME_CODE.HasValue || !rA42_PERMITS_DTL.PLATE_CHAR_CODE.HasValue)
                        {
                            TempData["Erorr"] = "بيانات المركبة ناقصة، يرجى وضع بيانات صحيحة - Car details missing";
                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });
                        }
                    }
                if (type == 5)
                {
                    //just update the request as it is
                    rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = PermitDtl.WORKFLOW_RESPO_CODE;
                }
                else
                {
                    if (!not_same_workflow.Contains(PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.ToString()) && !RnoVisitors.Contains(PermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE.ToString()))
                    {
                        if (form["rejectbtn"] != null)
                        {
                            if (WORKFLOWID == 2)
                            {
                                rA42_PERMITS_DTL.REJECTED = true;
                                rA42_PERMITS_DTL.ISOPENED = false;
                            }
                            if (WORKFLOWID == 3)
                            {
                                var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == PermitDtl.APPROVAL_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                                if (v != null)
                                {
                                    rA42_PERMITS_DTL.REJECTED = true;
                                    rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                                }
                                else
                                {
                                    rA42_PERMITS_DTL.REJECTED = true;
                                    rA42_PERMITS_DTL.ISOPENED = false;
                                }
                            }

                            if (WORKFLOWID == 4)
                            {
                                var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == PermitDtl.PERMIT_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                                if (v != null)
                                {
                                    rA42_PERMITS_DTL.REJECTED = true;
                                    rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                                }
                                else
                                {
                                    rA42_PERMITS_DTL.REJECTED = true;
                                }
                            }
                        }
                        else
                        {
                            //this section is for applicant 
                            if (form["approvebtn"] != null && WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                            {

                                if (FORCE_CODE == "3" && employees_and_families.Contains(permit))
                                {
                                    //autho person should redirect the permit to the permits cell 
                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE
                                    && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                    if (v == null)
                                    {
                                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                        return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                        rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                                        rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                        rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                        rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                        rA42_PERMITS_DTL.REJECTED = false;
                                        rA42_PERMITS_DTL.STATUS = false;
                                        rA42_PERMITS_DTL.ISOPENED = true;
                                    }

                                }
                                else
                                {
                                    rA42_PERMITS_DTL.REJECTED = false;
                                    rA42_PERMITS_DTL.STATUS = false;
                                    rA42_PERMITS_DTL.ISOPENED = false;
                                }
                            }

                            if (form["approvebtn"] != null && ViewBag.NOT_RELATED_STATION != true && WORKFLOWID == 2)
                            {
                                //autho person should redirect the permit to the permits cell 
                                var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE
                                && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                if (v == null)
                                {
                                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                    return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                }
                                else
                                {
                                    rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                    rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                                    rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                    rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                    rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                    rA42_PERMITS_DTL.REJECTED = false;
                                    rA42_PERMITS_DTL.STATUS = false;
                                    rA42_PERMITS_DTL.ISOPENED = true;
                                }
                            }

                            //this section is for permits cell 
                            if (form["approvebtn"] != null && WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                            {
                                if (PermitDtl.STATUS == true)
                                {
                                    rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = PermitDtl.WORKFLOW_RESPO_CODE;
                                }
                                else
                                {
                                    if (FORCE_CODE == "3" && WORKFLOWID == 3)
                                    {

                                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE
                                            && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                        if (v == null)
                                        {
                                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                        }
                                        else
                                        {
                                            rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                            rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                            rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                            rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                            rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                            rA42_PERMITS_DTL.REJECTED = false;
                                            rA42_PERMITS_DTL.STATUS = true;
                                            rA42_PERMITS_DTL.ISOPENED = true;
                                            string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                                            rA42_PERMITS_DTL.BARCODE = barcode;
                                        }




                                    }
                                    else
                                    {
                                        //permits cell should redirect the permit for the security officer as final step 
                                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE
                                        && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                        if (v == null)
                                        {
                                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                        }
                                        else
                                        {
                                            rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                            rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                            rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                            rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                            rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                            rA42_PERMITS_DTL.REJECTED = false;
                                            rA42_PERMITS_DTL.STATUS = false;
                                            rA42_PERMITS_DTL.ISOPENED = true;
                                        }

                                    }
                                }
                            }
                            if (form["approvebtn"] != null && WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                            {
                                //security officer should redirect the permit to the permits cel for printing
                                var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE
                                && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                if (v == null)
                                {
                                    TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                    return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                }
                                else
                                {
                                    rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                    rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                                    rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                    rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                                    rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                    rA42_PERMITS_DTL.REJECTED = false;
                                    rA42_PERMITS_DTL.STATUS = true;
                                    rA42_PERMITS_DTL.ISOPENED = true;
                                }



                            }
                        }
                    }
                    if (not_same_workflow.Contains(PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.ToString()))
                    {
                        //for autho permit
                        if (type == 1)
                        {
                            if (form["rejectbtn"] != null)
                            {
                                if (WORKFLOWID == 2 || WORKFLOWID == 1 || WORKFLOWID == 3)
                                {
                                    rA42_PERMITS_DTL.REJECTED = true;
                                    rA42_PERMITS_DTL.ISOPENED = false;
                                }
                                if (WORKFLOWID == 5 || WORKFLOWID == 4)
                                {
                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == PermitDtl.PERMIT_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true 
                                    && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                                    if (v != null)
                                    {
                                        rA42_PERMITS_DTL.REJECTED = true;
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.REJECTED = true;
                                        rA42_PERMITS_DTL.ISOPENED = false;
                                    }
                                }

                                if (WORKFLOWID == 6)
                                {
                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == PermitDtl.APPROVAL_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                                    if (v != null)
                                    {
                                        rA42_PERMITS_DTL.REJECTED = true;
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.REJECTED = true;
                                    }
                                }
                            }
                            else
                            {
                                if (form["approvebtn"] != null && (WORKFLOWID == 2 || WORKFLOWID <= 1))
                                {


                                    rA42_PERMITS_DTL.REJECTED = false;
                                    rA42_PERMITS_DTL.STATUS = false;
                                    rA42_PERMITS_DTL.ISOPENED = false;


                                }
                                if (form["approvebtn"] != null && WORKFLOWID == 5)
                                {

                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 6 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true 
                                    && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                    if (v == null)
                                    {
                                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                        return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                        rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                                        rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                        rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                        rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                        rA42_PERMITS_DTL.REJECTED = false;
                                        rA42_PERMITS_DTL.STATUS = false;
                                        rA42_PERMITS_DTL.ISOPENED = true;
                                    }


                                }

                                if (form["approvebtn"] != null && WORKFLOWID == 3)
                                {
                                    if (PermitDtl.STATUS == true)
                                    {
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = PermitDtl.WORKFLOW_RESPO_CODE;
                                    }
                                    else
                                    {
                                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 5 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true
                                        && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                        if (v == null)
                                        {
                                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                        }
                                        else
                                        {
                                            rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                            rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                            rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                            rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                            rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                            rA42_PERMITS_DTL.REJECTED = false;
                                            rA42_PERMITS_DTL.STATUS = false;
                                            rA42_PERMITS_DTL.ISOPENED = true;
                                        }
                                    }

                                }
                                if (form["approvebtn"] != null && WORKFLOWID == 4)
                                {
                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 5 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true
                                   && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                    if (v == null)
                                    {
                                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                        return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                        rA42_PERMITS_DTL.REJECTED = false;
                                        rA42_PERMITS_DTL.STATUS = false;
                                        rA42_PERMITS_DTL.ISOPENED = true;
                                    }


                                }
                                if (form["approvebtn"] != null && WORKFLOWID == 6)
                                {

                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true
                                    && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                    if (v == null)
                                    {
                                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                        return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                        rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                                        rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                                        rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                        rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                        rA42_PERMITS_DTL.REJECTED = false;
                                        rA42_PERMITS_DTL.STATUS = true;
                                        rA42_PERMITS_DTL.ISOPENED = true;
                                        string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                                        rA42_PERMITS_DTL.BARCODE = barcode;
                                    }
                                }
                            }
                        }
                        //for contracted companies
                        if (type == 6)
                        {

                            if (form["rejectbtn"] != null)
                            {
                                if (WORKFLOWID == 2 || WORKFLOWID == 7)
                                {
                                    rA42_PERMITS_DTL.REJECTED = true;
                                    rA42_PERMITS_DTL.ISOPENED = false;
                                }
                                if (WORKFLOWID == 3)
                                {
                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == PermitDtl.APPROVAL_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true 
                                    && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                                    if (v != null)
                                    {
                                        rA42_PERMITS_DTL.REJECTED = true;
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.REJECTED = true;
                                        rA42_PERMITS_DTL.ISOPENED = false;
                                    }
                                }

                                if (WORKFLOWID == 4)
                                {
                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == PermitDtl.PERMIT_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true 
                                    && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                                    if (v != null)
                                    {
                                        rA42_PERMITS_DTL.REJECTED = true;
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.REJECTED = true;
                                    }
                                }
                            }
                            else
                            {
                                if (form["approvebtn"] != null && (WORKFLOWID == 2 || WORKFLOWID <= 1))
                                {


                                    rA42_PERMITS_DTL.REJECTED = false;
                                    rA42_PERMITS_DTL.STATUS = false;
                                    rA42_PERMITS_DTL.ISOPENED = false;


                                }
                                if (form["approvebtn"] != null && WORKFLOWID == 7)
                                {

                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true
                                    && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                    if (v == null)
                                    {
                                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                        return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                        rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                                        rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                        rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                        rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                        rA42_PERMITS_DTL.REJECTED = false;
                                        rA42_PERMITS_DTL.STATUS = false;
                                        rA42_PERMITS_DTL.ISOPENED = true;
                                    }


                                }

                                if (form["approvebtn"] != null && WORKFLOWID == 3)
                                {
                                    if (PermitDtl.STATUS == true)
                                    {
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = PermitDtl.WORKFLOW_RESPO_CODE;
                                    }
                                    else
                                    {
                                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true
                                        && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                        if (v == null)
                                        {
                                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                        }
                                        else
                                        {
                                            rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                            rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                            rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                            rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                            rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                            rA42_PERMITS_DTL.REJECTED = false;
                                            rA42_PERMITS_DTL.STATUS = false;
                                            rA42_PERMITS_DTL.ISOPENED = true;
                                        }


                                    }
                                }
                                if (form["approvebtn"] != null && WORKFLOWID == 4)
                                {

                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true
                                    && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                    if (v == null)
                                    {
                                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                        return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                        rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                                        rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                                        rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                        rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                        rA42_PERMITS_DTL.REJECTED = false;
                                        rA42_PERMITS_DTL.STATUS = true;
                                        rA42_PERMITS_DTL.ISOPENED = true;
                                    }

                                }
                            }
                        }
                        //for aircrew
                        if (type == 10)
                        {
                            if (form["rejectbtn"] != null)
                            {
                                if (WORKFLOWID == 2)
                                {
                                    rA42_PERMITS_DTL.REJECTED = true;
                                    rA42_PERMITS_DTL.ISOPENED = false;
                                }
                                if (WORKFLOWID == 3)
                                {
                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == PermitDtl.APPROVAL_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true 
                                    && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                                    if (v != null)
                                    {
                                        rA42_PERMITS_DTL.REJECTED = true;
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.REJECTED = true;
                                        rA42_PERMITS_DTL.ISOPENED = false;
                                    }
                                }

                                if (WORKFLOWID == 10)
                                {
                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == PermitDtl.PERMIT_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true 
                                    && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                                    if (v != null)
                                    {
                                        rA42_PERMITS_DTL.REJECTED = true;
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.REJECTED = true;
                                    }
                                }
                            }
                            else
                            {
                                //this section is for applicant 
                                if (form["approvebtn"] != null && WORKFLOWID <= 1 || ViewBag.NOT_RELATED_STATION == true)
                                {
                                    //the request will redirected to the autho person as normal request 
                                    rA42_PERMITS_DTL.REJECTED = false;
                                    rA42_PERMITS_DTL.STATUS = false;
                                    rA42_PERMITS_DTL.ISOPENED = false;
                                }
                                //this section is for autho person (المنسق الأمني) 
                                if (form["approvebtn"] != null && WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                                {
                                    //the autho person should redirect the request to the permits cell 
                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 10 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true 
                                    && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                    if (v == null)
                                    {
                                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                        return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                        rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                                        rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                        rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                        rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                        rA42_PERMITS_DTL.REJECTED = false;
                                        rA42_PERMITS_DTL.STATUS = false;
                                        rA42_PERMITS_DTL.ISOPENED = true;
                                    }

                                }


                                //this is security offecier
                                if (form["approvebtn"] != null && (WORKFLOWID == 10 || WORKFLOWID == 3) && ViewBag.NOT_RELATED_STATION != true)
                                {
                                    if (PermitDtl.STATUS == true)
                                    {
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = PermitDtl.WORKFLOW_RESPO_CODE;
                                    }
                                    else
                                    {
                                        //after the security oofcier create this permit and completet every thing, the permit should be redirected to the permit cell to print it 
                                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true 
                                        && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                        if (v == null)
                                        {
                                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                        }
                                        else
                                        {
                                            if (WORKFLOWID != 3)
                                            {
                                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                                rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                                                rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                                                rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                                rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                                rA42_PERMITS_DTL.REJECTED = false;
                                                rA42_PERMITS_DTL.STATUS = true;
                                                rA42_PERMITS_DTL.ISOPENED = true;
                                            }
                                        }

                                        if (WORKFLOWID == 3)
                                        {
                                            rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                            rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                            rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                            rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                            string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                                            rA42_PERMITS_DTL.BARCODE = barcode;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (RnoVisitors.Contains(permit))
                    {
                        rA42_PERMITS_DTL.PASS_TYPE_CODE = 2;
                        if (permit != "12")
                        {
                            if (form["rejectbtn"] != null)
                            {
                                if (WORKFLOWID == 2)
                                {
                                    rA42_PERMITS_DTL.REJECTED = true;
                                    rA42_PERMITS_DTL.ISOPENED = false;
                                }
                                if (WORKFLOWID == 11 || WORKFLOWID == 4)
                                {
                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == PermitDtl.APPROVAL_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true 
                                    && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                                    if (v != null)
                                    {
                                        rA42_PERMITS_DTL.REJECTED = true;
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.REJECTED = true;
                                        rA42_PERMITS_DTL.ISOPENED = false;
                                    }
                                }

                               

                            }
                            else
                            {
                                //this section is for autho person 
                                if (form["approvebtn"] != null && (WORKFLOWID == 2 || WORKFLOWID == 3 || ViewBag.RESPO_STATE <= 1))
                                {

                                    //he should redirect the request to permits cell 
                                    var v = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 11 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE ==
                                    STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).FirstOrDefaultAsync();
                                    if (v == null)
                                    {
                                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                        return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                        rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                                        rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                        rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                        rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                        rA42_PERMITS_DTL.REJECTED = false;
                                        rA42_PERMITS_DTL.STATUS = false;
                                        rA42_PERMITS_DTL.ISOPENED = true;
                                    }




                                }
                                //this is security officer section
                                if (form["approvebtn"] != null && (WORKFLOWID == 4 || WORKFLOWID == 11) && ViewBag.NOT_RELATED_STATION != true)
                                {

                                    //security officer should redirect the complete request to permits cell for printing 
                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 11 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true
                                    && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                    if (v == null)
                                    {
                                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                        return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                        rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                                        rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                                        rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                        rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                        rA42_PERMITS_DTL.REJECTED = false;
                                        rA42_PERMITS_DTL.STATUS = true;
                                        rA42_PERMITS_DTL.ISOPENED = true;
                                    }



                                }


                            }
                        }
                        else
                        {

                            if (form["rejectbtn"] != null)
                            {
                                if (WORKFLOWID == 2)
                                {
                                    rA42_PERMITS_DTL.REJECTED = true;
                                    rA42_PERMITS_DTL.ISOPENED = false;
                                }
                                if (WORKFLOWID == 11 || WORKFLOWID == 4)
                                {
                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == PermitDtl.APPROVAL_SN && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true
                                   && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();
                                    if (v != null)
                                    {
                                        rA42_PERMITS_DTL.REJECTED = true;
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.REJECTED = true;
                                        rA42_PERMITS_DTL.ISOPENED = false;
                                    }
                                }


                            }
                            else
                            {
                                if (form["approvebtn"] != null && WORKFLOWID == 11)
                                {

                                    rA42_PERMITS_DTL.REJECTED = false;
                                    rA42_PERMITS_DTL.STATUS = false;
                                    rA42_PERMITS_DTL.ISOPENED = false;


                                }
                                //this section is for autho person 
                                if (form["approvebtn"] != null && WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                                {
                                    //he should redirect the request to permits cell 
                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 11 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true
                                    && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).FirstOrDefault();
                                    if (v == null)
                                    {
                                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                        return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                    }
                                    rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                                    rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                    rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                    rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                    rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                                    rA42_PERMITS_DTL.REJECTED = false;
                                    rA42_PERMITS_DTL.STATUS = true;
                                    rA42_PERMITS_DTL.ISOPENED = true;
                                    string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                                    rA42_PERMITS_DTL.BARCODE = barcode;
                                }

                                //permits cell section
                                if (form["approvebtn"] != null && (WORKFLOWID == 3 || WORKFLOWID == 11) && ViewBag.NOT_RELATED_STATION != true)
                                {
                                    if (PermitDtl.STATUS == true)
                                    {
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = PermitDtl.WORKFLOW_RESPO_CODE;
                                    }
                                    else
                                    {
                                        //they should redirect the permit to the securiy officer to approve the permit 
                                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 11 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true 
                                        && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                        if (v == null)
                                        {
                                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                        }
                                        else
                                        {
                                            rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                            rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                            rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                            rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                            rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                            rA42_PERMITS_DTL.REJECTED = false;
                                            rA42_PERMITS_DTL.STATUS = true;
                                            rA42_PERMITS_DTL.ISOPENED = true;
                                            string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                                            rA42_PERMITS_DTL.BARCODE = barcode;
                                        }
                                    }

                                }
                                //this is security officer section
                                if (form["approvebtn"] != null && WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                                {

                                    //security officer should redirect the complete request to permits cell for printing 
                                    var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 11 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true 
                                    && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                                    if (v == null)
                                    {
                                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                        return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                                    }
                                    else
                                    {
                                        rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                        rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                                        rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                                        rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                        rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                        rA42_PERMITS_DTL.REJECTED = false;
                                        rA42_PERMITS_DTL.STATUS = true;
                                        rA42_PERMITS_DTL.ISOPENED = true;
                                    }


                                }
                            }
                        }

                    }
                }
                    rA42_PERMITS_DTL.UPD_BY = currentUser;
                    rA42_PERMITS_DTL.UPD_DT = DateTime.Now;
                    if (rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE != null && rA42_PERMITS_DTL.CARD_FOR_CODE != null && rA42_PERMITS_DTL.ACCESS_TYPE_CODE != null)
                    {
                    try
                    {
                        db.Entry(PermitDtl).CurrentValues.SetValues(rA42_PERMITS_DTL);
                        //db.Entry(rA42_PERMITS_DTL).State = EntityState.Modified;
                        await db.SaveChangesAsync();
                    }catch(Exception ex)
                    {
                        TempData["Erorr"] = ex.GetBaseException().Message;
                        return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                    }
                    }
                    else
                    {
                    AddToast(new Toast("",
                    GetResourcesValue("error_update_message"),
                    "red"));
                    TempData["Erorr"] = Resources.Common.ResourceManager.GetString("error_create_message" + "_" + ViewBag.lang); ;
                        return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });
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
                            rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = rA42_PERMITS_DTL.ACCESS_TYPE_CODE;
                            if(PermitDtl.COMPANY_PASS_CODE != null)
                            {
                            rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = PermitDtl.COMPANY_PASS_CODE;

                            }
                            else
                            {
                            rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE;

                            }
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


                                        fileName = "FileNO" + c + "_" + rA42_PERMITS_DTL.ACCESS_TYPE_CODE + "_" + permit + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                        // store the file inside ~/App_Data/uploads folder
                                        string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                        file.SaveAs(path);


                                        RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                        {
                                            ACCESS_TYPE_CODE = rA42_PERMITS_DTL.ACCESS_TYPE_CODE,
                                            ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE,
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
                                        var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.ACCESS_TYPE_CODE == rA42_PERMITS_DTL.ACCESS_TYPE_CODE).ToList();
                                        foreach (var del in delete)
                                        {
                                            string filpath = "~/Documents/" + del.FILE_NAME;
                                            general.RemoveFileFromServer(filpath);
                                            db.RA42_FILES_MST.Remove(del);
                                            db.SaveChanges();
                                        }
                                        TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                        return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });
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
                                rA42_MEMBERS_DTL.ACCESS_TYPE_CODE = rA42_PERMITS_DTL.ACCESS_TYPE_CODE;
                                rA42_MEMBERS_DTL.ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE;
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

                                            fileName = "Relative_Profile_" + rA42_PERMITS_DTL.ACCESS_TYPE_CODE + "_" + permit + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                                            // store the file inside ~/Files/Profiles/ folder
                                            bool check = general.ResizeImage(RELATIVE_IMAGE[i], fileName);

                                            if (check != true)
                                            {
                                                AddToast(new Toast("",
                                               GetResourcesValue("error_update_message"),
                                               "red"));
                                                TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                                return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });
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
                                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });
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
                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });
                        }
                    }
                    if (COMMENT.Length > 0)
                    {
                        RA42_COMMENTS_MST rA42_COMMENT = new RA42_COMMENTS_MST();
                        rA42_COMMENT.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                    if (PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5)
                    {
                        rA42_COMMENT.PASS_ROW_CODE = PermitDtl.COMPANY_PASS_CODE;

                    }
                    else
                    {
                        rA42_COMMENT.PASS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE;
                    }
                        rA42_COMMENT.CRD_BY = currentUser;
                        rA42_COMMENT.CRD_DT = DateTime.Now;
                        rA42_COMMENT.COMMENT = COMMENT;
                        db.RA42_COMMENTS_MST.Add(rA42_COMMENT);
                        db.SaveChanges();

                    }
                   
                    AddToast(new Toast("",
                            GetResourcesValue("success_update_message"),
                            "green"));
                    if (WORKFLOWID == 11 && RnoVisitors.Contains(permit) && permit != "12")
                    {
                        return RedirectToAction("RnoTempPermitsManage", "Permitsdtl");

                    }

                    if (ViewBag.RESPO_STATE <= 1)
                    {

                        return RedirectToAction("Index", "MyPasses");
                    }
                    else
                    {
                     return RedirectToAction("Index", new {type=PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE});
                    }
			        }

			       AddToast(new Toast("",
                GetResourcesValue("error_update_message"),
                "red"));
			return View(rA42_PERMITS_DTL);
		}


        //renew RA42_PERMITS_DTL action
        [HttpGet]
        public async Task<ActionResult> Renew(int? id, string tab)
        {
            ViewBag.activetab = tab;

            if (id == null)
            {
                return NotFound();
            }
            //check if request in the db table 
            RA42_PERMITS_DTL rA42_PERMITS_DTL = db.RA42_PERMITS_DTL.Find(id);
            if (rA42_PERMITS_DTL == null)
            {
                return NotFound();
            }
            //check authority to proccess this request 

            if (rA42_PERMITS_DTL.SERVICE_NUMBER == currentUser || rA42_PERMITS_DTL.RESPONSIBLE == currentUser)
            {
                if (rA42_PERMITS_DTL.DATE_TO != null)
                {

                    if (DateTime.Now > rA42_PERMITS_DTL.DATE_TO.Value || IsExpiringWithinOneMonth(rA42_PERMITS_DTL.DATE_TO.Value))
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
                if (ViewBag.RESPO_STATE != rA42_PERMITS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
                {
                    if (rA42_PERMITS_DTL.DATE_TO != null)
                    {

                        if (DateTime.Now > rA42_PERMITS_DTL.DATE_TO.Value || IsExpiringWithinOneMonth(rA42_PERMITS_DTL.DATE_TO.Value))
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

            FORCE_CODE = rA42_PERMITS_DTL.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE.ToString();
            var same_workflow = new List<string>() { "8", "2", "5", "7", "9" };
            var RnoVisitors = new List<string>() { "11", "12", "13", "14", "16" };
            var employees_and_families = new List<string>() { "21", "23", "27", "35", "15" };

            if (RnoVisitors.Contains(rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
            {
                ViewBag.Managepasses = "";
                ViewBag.RnoTempPermits = "RnoTempPermits";
            }

            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != rA42_PERMITS_DTL.STATION_CODE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }


            if (Language.GetCurrentLang() == "en")
            {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == rA42_PERMITS_DTL.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE), "FORCE_ID", "FORCE_NAME_E");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE), "ACCESS_TYPE_CODE", "ACCESS_TYPE_E");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.DLT_STS != true && a.CARD_SECRET_CODE == rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE), "CARD_FOR_CODE", "CARD_FOR_E");
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE), "STATION_CODE", "STATION_NAME_E");
                //get bloods
                ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE", rA42_PERMITS_DTL.BLOOD_CODE);
                //get events and excrises in english
                ViewBag.EVENT_EXERCISE_CODE = new SelectList(db.RA42_EVENT_EXERCISE_MST.Where(a => a.DLT_STS != true && a.ACTIVE != false && a.DATE_TO <= DateTime.Today), "EVENT_EXERCISE_CODE", "EVENT_EXERCISE_NAME_E", rA42_PERMITS_DTL.EVENT_EXERCISE_CODE);

                //get company name
                if (rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6)
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 1), "COMPANY_CODE", "COMPANY_NAME_E", rA42_PERMITS_DTL.COMPANY_CODE);

                }
                else
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_PERMITS_DTL.COMPANY_CODE);
                }
                //get realtives types in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_PERMITS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_PERMITS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_PERMITS_DTL.IDENTITY_CODE);
                //get permits types in english (مؤقت - ثابت)
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_PERMITS_DTL.PASS_TYPE_CODE);
                if (rA42_PERMITS_DTL.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3")
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                if (RnoVisitors.Contains(rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 2), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plates types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_PERMITS_DTL.PLATE_CODE);
                //get plates chars in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_PERMITS_DTL.PLATE_CHAR_CODE);
                //get vechile catigories in english 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_PERMITS_DTL.VECHILE_CODE);
                //get colors of vechiles in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_PERMITS_DTL.VECHILE_COLOR_CODE);
                //get vechiles types in english (صالون - دفع رباعي ....)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_PERMITS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english
                switch (rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }                       //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get responsible person (المنسق الأمني)-- this option will be for applicant to change autho person if he wants
                //get autho person for this kind of permit in english (المنسق الأمني)
                var unique_permits = new List<string> { "1", "6", "10" };
                if (unique_permits.Contains(rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.ToString()))
                {
                    if (rA42_PERMITS_DTL.ACCESS_TYPE_CODE == 10)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            //show error message if there is no autho person, when current user workflow id is one or less than one 
                            if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });
                            }
                        }
                    }
                    if (rA42_PERMITS_DTL.ACCESS_TYPE_CODE == 1)
                    {
                        // here to detrmine who is autho resonsible to proccess to permit
                        int workflow_id = 5;
                        if (ViewBag.RESPO_STATE == 6)
                        {
                            workflow_id = 6;
                        }
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //if WORKFLOW_RESPO.Count == 0 that means no body setting in this position and the system should return error message no body in this posision

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                        }
                    }

                    if (rA42_PERMITS_DTL.ACCESS_TYPE_CODE == 6)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //check if WORKFLOW_RESPO.Count == 0 show error message 
                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                        }
                    }
                }
                if (!unique_permits.Contains(rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.ToString()))
                {
                    var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                    if (WORKFLOW_RESPO.Count == 0)
                    {
                        //show error message if there is no autho person, when current user workflow id is one or less than one 
                        if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                        {

                            if (FORCE_CODE != "3" || !employees_and_families.Contains(rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });
                            }
                        }
                    }
                }
            }
            else
            {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == rA42_PERMITS_DTL.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE), "FORCE_ID", "FORCE_NAME_A");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE), "ACCESS_TYPE_CODE", "ACCESS_TYPE");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.DLT_STS != true && a.CARD_SECRET_CODE == rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE), "CARD_FOR_CODE", "CARD_FOR_A");
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE), "STATION_CODE", "STATION_NAME_A");
                //get bloods
                ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE", rA42_PERMITS_DTL.BLOOD_CODE);
                //get events and excrises in english
                ViewBag.EVENT_EXERCISE_CODE = new SelectList(db.RA42_EVENT_EXERCISE_MST.Where(a => a.DLT_STS != true && a.ACTIVE != false && a.DATE_TO <= DateTime.Today), "EVENT_EXERCISE_CODE", "EVENT_EXERCISE_NAME", rA42_PERMITS_DTL.EVENT_EXERCISE_CODE);
                //get company name
                if (rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6)
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 1), "COMPANY_CODE", "COMPANY_NAME", rA42_PERMITS_DTL.COMPANY_CODE);

                }
                else
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_PERMITS_DTL.COMPANY_CODE);
                }
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_PERMITS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_PERMITS_DTL.GENDER_ID);
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_PERMITS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_PERMITS_DTL.PASS_TYPE_CODE);
                if (rA42_PERMITS_DTL.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3")
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                if (RnoVisitors.Contains(rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 2), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_PERMITS_DTL.PLATE_CODE);
                //get plates chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_PERMITS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_PERMITS_DTL.VECHILE_CODE);
                //get vechiles colors in arabic
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_PERMITS_DTL.VECHILE_COLOR_CODE);
                //get vechiles types (صالون - دفع رباعي ....) in arabic
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_PERMITS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic
                switch (rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }                //get documents types for this kind of permit in arabic 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                var unique_permits = new List<string> { "1", "6", "10" };
                if (unique_permits.Contains(rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.ToString()))
                {
                    if (rA42_PERMITS_DTL.ACCESS_TYPE_CODE == 10)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            //show error message if there is no autho person, when current user workflow id is one or less than one 
                            if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });
                            }
                        }
                    }
                    if (rA42_PERMITS_DTL.ACCESS_TYPE_CODE == 1)
                    {
                        // here to detrmine who is autho resonsible to proccess to permit
                        int workflow_id = 5;
                        if (ViewBag.RESPO_STATE == 6)
                        {
                            workflow_id = 6;
                        }
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //if WORKFLOW_RESPO.Count == 0 that means no body setting in this position and the system should return error message no body in this posision

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                        }
                    }

                    if (rA42_PERMITS_DTL.ACCESS_TYPE_CODE == 6)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //check if WORKFLOW_RESPO.Count == 0 show error message 
                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Edit", new { id = rA42_PERMITS_DTL.PERMIT_CODE });

                        }
                    }
                }
                if (!unique_permits.Contains(rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.ToString()))
                {
                    var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                    if (WORKFLOW_RESPO.Count == 0)
                    {
                        //show error message if there is no autho person, when current user workflow id is one or less than one 
                        if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                        {
                            if (FORCE_CODE != "3" || !employees_and_families.Contains(rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                            }
                        }
                    }
                }
            }
            //get relatives of this permit 
            ViewBag.GetRelativs = await db.RA42_MEMBERS_DTL.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();
            //get selected zones and gates
            if (rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE != 5)
            {
                ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();
            }
            else
            {
                if (rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "25")
                {
                    ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();

                }
                else
                {
                    ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();
                }
            }            //get selected documenst 
            ViewBag.GetFiles = await db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1) && !a.CONTROLLER_NAME.Equals("Companies")).ToListAsync();
          
            //get documenst for this kind of permit to check missing documenst and make compare
            // ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).ToList();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();


            //get personal image 
            ViewBag.PERSONAL_IMAGE = rA42_PERMITS_DTL.PERSONAL_IMAGE;
            //get status of the request
            ViewBag.STATUS = rA42_PERMITS_DTL.STATUS;
            ViewBag.ISPRINTED = rA42_PERMITS_DTL.ISPRINTED;
            rA42_PERMITS_DTL.DATE_FROM = null;
            rA42_PERMITS_DTL.DATE_TO = null;
            rA42_PERMITS_DTL.CARD_EXPIRED_DATE = null;
            rA42_PERMITS_DTL.BARCODE = null;

            var no_more_than_2 = new List<string>() { "1", "44", "43", "42", "21", "37", "36", "35", "7", "8" };
            if (no_more_than_2.Contains(rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
            {

                
                //check if employee has more than 2 permits still valid
                var num_permits = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.DATE_TO.Value > DateTime.Now && a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE &&
                a.SERVICE_NUMBER == rA42_PERMITS_DTL.SERVICE_NUMBER.ToUpper() && (a.ISPRINTED == true || a.STATUS != true || a.STATUS == true || a.REJECTED == true) && a.RETURNED != true
                && no_more_than_2.Contains(a.RA42_CARD_FOR_MST.CARD_SECRET_CODE)).ToListAsync();
                if (ViewBag.RESPO_STATE < 3)
                {
                    if (num_permits.Count >= 2)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("more_than_two_permits" + "_" + ViewBag.lang);
                        ViewBag.TotalAllowRenewPermits = num_permits.Count;
                    }
                }

            }

            return View(rA42_PERMITS_DTL);


        }

        // POST RA42_PERMITS_DTL data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Renew(RA42_PERMITS_DTL rA42_PERMITS_DTL,
            int[] RELATIVE_TYPES, HttpPostedFileBase[] RELATIVE_IMAGE, int[] IDENTITY_TYPES, int[] GENDER_TYPES,
            string[] FULL_NAME, string[] CIVIL_NUM, string[] PASSPORT_NUMBER, string[] PHONE_NUMBER_M
            , string[] REMARKS_LIST, int[] ZONE, int[] SUB_ZONE, int[] FILE_TYPES, string is_with_car,
            string[] FILE_TYPES_TEXT, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE, string COMMENT, string tab, int PERMIT_ID)
        {
            //check if the request id is in the db table 
            var PermitsDtl = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == PERMIT_ID).FirstOrDefault();
            ViewBag.activetab = tab;
            //get selected zones and gates
            ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == PERMIT_ID && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitsDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();
            //get selected documents
            ViewBag.GetFiles = await db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == PERMIT_ID && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitsDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && a.CRD_DT > new DateTime(2024, 8, 1) && !a.CONTROLLER_NAME.Equals("Companies")).ToListAsync();
            //get documents types for this kind of permit to check missing files 
            ViewBag.PASS_FILES = await db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitsDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true ).ToListAsync();
            //get relatives of this permit 
            ViewBag.GetRelativs = await db.RA42_MEMBERS_DTL.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitsDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.ACCESS_ROW_CODE == PermitsDtl.PERMIT_CODE && a.DLT_STS != true && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();
            //get personal image 
            ViewBag.PERSONAL_IMAGE = PermitsDtl.PERSONAL_IMAGE;
            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (PermitsDtl.STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }

            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitsDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == PermitsDtl.RA42_STATIONS_MST.FORCE_ID && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == PermitsDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitsDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == PermitsDtl.RA42_STATIONS_MST.FORCE_ID && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == PermitsDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();

         
            STATION_CODE = PermitsDtl.STATION_CODE.Value;
            ACCESS_TYPE_CODE = PermitsDtl.ACCESS_TYPE_CODE.Value;
            var permit = PermitsDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE;
            var type = PermitsDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE;
            var RnoVisitors = new List<string>() { "11", "12", "13", "14", "16" };
            if (RnoVisitors.Contains(permit))
            {
                switch (permit)
                {
                    case "11":
                        ViewBag.activetab = "MedicalCenterTemporary";
                        break;
                    case "12":
                        ViewBag.activetab = "CompaniesTemporary";
                        break;
                    case "13":
                        ViewBag.activetab = "FamilyTemporary";
                        break;
                    case "14":
                        ViewBag.activetab = "VisitorTemporary";
                        break;
                    case "16":
                        ViewBag.activetab = "VisitorFromRno";
                        break;

                }
            }
            //get station name 
            var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == STATION_CODE).FirstOrDefault();
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
                ViewBag.HQ_UNIT = STATION_CODE;
                FORCE_CODE = check_unit.RA42_FORCES_MST.FORCE_CODE.ToString();
                ViewBag.Selected_Station = STATION_CODE;
                ViewBag.Selected_Force = FORCE_CODE;

            }

            var checkSubPermit = await db.RA42_CARD_FOR_MST.Where(a => a.CARD_SECRET_CODE == permit && a.DLT_STS != true).FirstOrDefaultAsync();
            if (checkSubPermit != null)
            {

                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.SUB_PERMIT = checkSubPermit.CARD_FOR_E;
                }
                else
                {
                    ViewBag.SUB_PERMIT = checkSubPermit.CARD_FOR_A;

                }
                ViewBag.MAIN_PERMIT_ICON = checkSubPermit.RA42_ACCESS_TYPE_MST.ICON;
                ViewBag.WITH_CAR = checkSubPermit.WITH_CAR;

            }

            //check if the user want to request permit in his station
            ViewBag.NOT_RELATED_STATION = false;
            if (STATION_CODE != ViewBag.STATION_CODE_TYPE)
            {
                ViewBag.NOT_RELATED_STATION = true;
            }
            //same workflow for rno
            var same_workflow = new List<string>() { "8", "2", "5", "7", "9" };
            var employees_and_families = new List<string>() { "21", "23", "27", "35", "15" };

            if (Language.GetCurrentLang() == "en")
            {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == FORCE_CODE), "FORCE_ID", "FORCE_NAME_E");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == type), "ACCESS_TYPE_CODE", "ACCESS_TYPE_E");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.DLT_STS != true && a.CARD_SECRET_CODE == permit), "CARD_FOR_CODE", "CARD_FOR_E");
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "STATION_CODE", "STATION_NAME_E");
                //sections
                ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "SECTION_NAME", "SECTION_NAME");
                //get bloods
                ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE", rA42_PERMITS_DTL.BLOOD_CODE);
                //get events and excrises in english
                ViewBag.EVENT_EXERCISE_CODE = new SelectList(db.RA42_EVENT_EXERCISE_MST.Where(a => a.DLT_STS != true && a.ACTIVE != false && a.DATE_TO <= DateTime.Today), "EVENT_EXERCISE_CODE", "EVENT_EXERCISE_NAME_E", rA42_PERMITS_DTL.EVENT_EXERCISE_CODE);
                //company name 
                if (type == 6)
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 1), "COMPANY_CODE", "COMPANY_NAME_E", rA42_PERMITS_DTL.COMPANY_CODE);

                }
                else
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME_E", rA42_PERMITS_DTL.COMPANY_CODE);
                }
                //get relatives typs in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_PERMITS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_PERMITS_DTL.GENDER_ID);
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_PERMITS_DTL.IDENTITY_CODE);
                if (FORCE_CODE == "3")
                {
                    //this option for RANO, all temprory permits in the visitor permits
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                else
                {
                    //gt permits types in english (مؤقت - ثابت)
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_PERMITS_DTL.PASS_TYPE_CODE);
                }
                if (RnoVisitors.Contains(permit))
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 2), "PASS_TYPE_CODE", "PASS_TYPE_E");

                }
                //get plate types in english 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE_E", rA42_PERMITS_DTL.PLATE_CODE);
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR_E", rA42_PERMITS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in english
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT_E", rA42_PERMITS_DTL.VECHILE_CODE);
                //get color types in english 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR_E", rA42_PERMITS_DTL.VECHILE_COLOR_CODE);
                //get vechiles name (صالون -دفع رباعي ....) in english 
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME_E", rA42_PERMITS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in english 
                switch (permit)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == permit && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE_E + " - " + c.FILE_NOTE_CODE
                           }).ToList();
                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true && a.RA42_FILE_TYPE_MST.FORCE_ID.Equals(FORCE_ID)).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE_E + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in english (المنسق الأمني)
                var unique_permits = new List<string> { "1", "6", "10" };
                if (unique_permits.Contains(type.ToString()))
                {
                    if (type == 10)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            //show error message if there is no autho person, when current user workflow id is one or less than one 
                            if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });
                            }
                        }
                    }
                    if (type == 1)
                    {
                        // here to detrmine who is autho resonsible to proccess to permit
                        int workflow_id = 5;
                        if (ViewBag.RESPO_STATE == 6)
                        {
                            workflow_id = 6;
                        }
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //if WORKFLOW_RESPO.Count == 0 that means no body setting in this position and the system should return error message no body in this posision

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                        }
                    }

                    if (type == 6)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //check if WORKFLOW_RESPO.Count == 0 show error message 
                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                        }
                    }
                }
                if (!unique_permits.Contains(type.ToString()))
                {
                    var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME_E + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME_E }).ToListAsync();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                    if (WORKFLOW_RESPO.Count == 0)
                    {
                        //show error message if there is no autho person, when current user workflow id is one or less than one 
                        if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                        {

                            if (FORCE_CODE != "3" || !employees_and_families.Contains(permit))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                            }
                        }
                    }
                }
            }
            else
            {
                //get force
                ViewBag.FORCE_CODE = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true && a.FORCE_CODE == FORCE_CODE), "FORCE_ID", "FORCE_NAME_A");
                //get main permit
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST.Where(a => a.DLT_STS != true && a.ACCESS_SECRET_CODE == type), "ACCESS_TYPE_CODE", "ACCESS_TYPE");
                //get sub permit
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST.Where(a => a.DLT_STS != true && a.CARD_SECRET_CODE == permit), "CARD_FOR_CODE", "CARD_FOR_A");
                //get station 
                ViewBag.STATION_CODE = new SelectList(db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "STATION_CODE", "STATION_NAME_A");
                //sections
                ViewBag.SECTION_NAME = new SelectList(db.RA42_SECTIONS_MST.Where(a => a.DLT_STS != true && a.STATION_CODE == STATION_CODE), "SECTION_NAME", "SECTION_NAME");
                //get bloods
                ViewBag.BLOOD_CODE = new SelectList(db.RA42_BLOOD_TYPE_MST.Where(a => a.DLT_STS != true), "BLOOD_CODE", "BLOOD_TYPE", rA42_PERMITS_DTL.BLOOD_CODE);
                //get events and excrises in english
                ViewBag.EVENT_EXERCISE_CODE = new SelectList(db.RA42_EVENT_EXERCISE_MST.Where(a => a.DLT_STS != true && a.ACTIVE != false && a.DATE_TO <= DateTime.Today), "EVENT_EXERCISE_CODE", "EVENT_EXERCISE_NAME", rA42_PERMITS_DTL.EVENT_EXERCISE_CODE);
                //company name 
                if (type == 6)
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 1), "COMPANY_CODE", "COMPANY_NAME", rA42_PERMITS_DTL.COMPANY_CODE);

                }
                else
                {
                    ViewBag.COMPANY_CODE = new SelectList(db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true && a.COMPANY_TYPE_CODE == 2 && a.STATION_CODE == STATION_CODE), "COMPANY_CODE", "COMPANY_NAME", rA42_PERMITS_DTL.COMPANY_CODE);
                }
                //get relatives typs in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_PERMITS_DTL.RELATIVE_TYPE_CODE);
                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_PERMITS_DTL.GENDER_ID);
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_PERMITS_DTL.IDENTITY_CODE);
                //get permits types codes in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_PERMITS_DTL.PASS_TYPE_CODE);
                if (FORCE_CODE == "3")
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                if (RnoVisitors.Contains(permit))
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 2), "PASS_TYPE_CODE", "PASS_TYPE");

                }
                //get plates types in arabic 
                ViewBag.PLATE_CODE = new SelectList(db.RA42_PLATE_TYPE_MST.Where(a => a.DLT_STS != true), "PLATE_CODE", "PLATE_TYPE", rA42_PERMITS_DTL.PLATE_CODE);
                //get plate chars in arabic 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR", rA42_PERMITS_DTL.PLATE_CHAR_CODE);
                //get vechiles catigories in arabic 
                ViewBag.VECHILE_CODE = new SelectList(db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true), "VECHILE_CODE", "VECHILE_CAT", rA42_PERMITS_DTL.VECHILE_CODE);
                //get colrs in arabic 
                ViewBag.VECHILE_COLOR_CODE = new SelectList(db.RA42_VECHILE_COLOR_MST.Where(a => a.DLT_STS != true), "VECHILE_COLOR_CODE", "COLOR", rA42_PERMITS_DTL.VECHILE_COLOR_CODE);
                //get vechiles types in arabic (صالون - دفع رباعي)
                ViewBag.VECHILE_NAME_CODE = new SelectList(db.RA42_VECHILE_NAME_MST.Where(a => a.DLT_STS != true), "VECHILE_NAME_CODE", "VECHILE_NAME", rA42_PERMITS_DTL.VECHILE_NAME_CODE);
                //get zones and gates in arabic 
                switch (permit)
                {
                    case "7":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS == true && a.ZONE_CODE == 129).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "8":
                        var zones = new List<string> {
                        "222","1111","111"};
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && zones.Contains(a.ZONE_NUMBER.ToString())).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "11":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "13":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "14":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    case "16":
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true && a.ZONE_CODE != 128).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                    default:
                        ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                        break;
                }
                //get documents types for this kind of permit in arabic
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.DLT_STS != true && c.DLT_STS != true && d.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE && z.RA42_CARD_FOR_MST.CARD_SECRET_CODE == permit && d.DLT_STS != true && z.DLT_STS != true
                           select new
                           {
                               FILE_TYPE_CODE = c.FILE_TYPE_CODE,
                               FILE_TYPE = c.FILE_TYPE + " - " + c.FILE_NOTE_CODE
                           }).ToList();

                //var documents = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true).Select(s => new { FILE_TYPE_CODE = s.RA42_FILE_TYPE_MST.FILE_TYPE_CODE, FILE_TYPE = s.RA42_FILE_TYPE_MST.FILE_TYPE + " - " + s.RA42_FILE_TYPE_MST.FILE_NOTE_CODE }).ToList();
                ViewBag.FILE_TYPES = new SelectList(doc, "FILE_TYPE_CODE", "FILE_TYPE");
                //get autho person for this kind of permit in arabic (المنسق الأمني) 
                var unique_permits = new List<string> { "1", "6", "10" };
                if (unique_permits.Contains(type.ToString()))
                {
                    if (type == 10)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            //show error message if there is no autho person, when current user workflow id is one or less than one 
                            if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });
                            }
                        }
                    }
                    if (type == 1)
                    {
                        // here to detrmine who is autho resonsible to proccess to permit
                        int workflow_id = 5;
                        if (ViewBag.RESPO_STATE == 6)
                        {
                            workflow_id = 6;
                        }
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == workflow_id && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == FORCE_CODE).
                            Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //if WORKFLOW_RESPO.Count == 0 that means no body setting in this position and the system should return error message no body in this posision

                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                        }
                    }

                    if (type == 6)
                    {
                        var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 7 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type).Select(s => new {
                            WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE,
                            RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME
                        }).ToListAsync();
                        ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);
                        //check if WORKFLOW_RESPO.Count == 0 show error message 
                        if (WORKFLOW_RESPO.Count == 0)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                        }
                    }
                }
                if (!unique_permits.Contains(type.ToString()))
                {
                    var WORKFLOW_RESPO = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 2 && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE).Select(s => new { WORKFLOW_RESPO_CODE = s.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOW_RESPO_CODE, RESPONSEPLE_NAME = s.RA42_WORKFLOW_RESPONSIBLE_MST.RANK + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME + " - " + s.RA42_WORKFLOW_RESPONSIBLE_MST.UNIT_NAME }).ToListAsync();
                    ViewBag.WORKFLOW_RESPO_CODE = new SelectList(WORKFLOW_RESPO, "WORKFLOW_RESPO_CODE", "RESPONSEPLE_NAME", rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE);

                    if (WORKFLOW_RESPO.Count == 0)
                    {
                        //show error message if there is no autho person, when current user workflow id is one or less than one 
                        if (ViewBag.RESPO_STATE <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11))
                        {
                            if (FORCE_CODE != "3" || !employees_and_families.Contains(permit))
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);

                            }
                        }
                    }
                }
            }

            User user = null;
            Task<User> callTask = Task.Run(
                () => (new UserInfo()).getUserInfoFromAPI(currentUser)
                );
            callTask.Wait();
            user = callTask.Result;
            ViewBag.JundStop = "";
            if (user == null)
            {
                ViewBag.JundStop = @Resources.Common.ResourceManager.GetString("jund_error" + "_" + ViewBag.lang);
            }
            else
            {
                switch (permit)
                {
                    case "7":
                    case "8":
                    case "9":
                    case "15":
                    case "16":
                    case "21":
                    case "23":
                    case "27":
                    case "30":
                    case "35":
                    case "36":
                    case "37":
                    case "40":
                    case "41":
                    case "42":
                    case "43":
                    case "44":
                        if (string.IsNullOrWhiteSpace(rA42_PERMITS_DTL.NAME_A))
                        {
                            rA42_PERMITS_DTL.NAME_A = user.NAME_EMP_A;
                            rA42_PERMITS_DTL.RANK_A = user.NAME_RANK_A;
                            rA42_PERMITS_DTL.NAME_E = user.NAME_EMP_E;
                            rA42_PERMITS_DTL.RANK_E = user.NAME_RANK_E;
                            rA42_PERMITS_DTL.PROFESSION_A = user.NAME_TRADE_A;
                            rA42_PERMITS_DTL.PROFESSION_E = user.NAME_TRADE_E;
                        }

                        if (string.IsNullOrWhiteSpace(rA42_PERMITS_DTL.UNIT_A))
                        {
                            rA42_PERMITS_DTL.UNIT_A = user.NAME_UNIT_A;
                        }
                        rA42_PERMITS_DTL.UNIT_E = user.NAME_UNIT_E;
                        if (permit == "30" || permit == "23")
                        {
                            rA42_PERMITS_DTL.HOST_NAME_A = user.NAME_EMP_A;
                            rA42_PERMITS_DTL.HOST_RANK_A = user.NAME_RANK_A;
                            rA42_PERMITS_DTL.HOST_NAME_E = user.NAME_EMP_E;
                            rA42_PERMITS_DTL.HOST_RANK_E = user.NAME_RANK_E;
                            rA42_PERMITS_DTL.HOST_PROFESSION_A = user.NAME_TRADE_A;
                            rA42_PERMITS_DTL.HOST_PROFESSION_E = user.NAME_TRADE_E;
                        }
                        break;

                }
            }
            var no_more_than_2 = new List<string>() { "1", "44", "43", "42", "21", "37", "36", "35", "7", "8" };
            if (no_more_than_2.Contains(permit))
            {

                //check if employee has more than 2 permits still valid
                var num_permits = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.DATE_TO.Value > DateTime.Now && a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE &&
                a.SERVICE_NUMBER == rA42_PERMITS_DTL.SERVICE_NUMBER.ToUpper() && (a.ISPRINTED == true || a.STATUS != true || a.REJECTED == true) && a.RETURNED != true
                && no_more_than_2.Contains(a.RA42_CARD_FOR_MST.CARD_SECRET_CODE)).ToListAsync();
                if (ViewBag.RESPO_STATE < 3)
                {
                    if (num_permits.Count >= 2)
                    {
                        TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("more_than_two_permits" + "_" + ViewBag.lang);
                        ViewBag.TotalAllowRenewPermits = num_permits.Count;
                    }
                }


            }
            if (ModelState.IsValid)
            {
                try
                {
                if (permit == "25" || type == 5)
                {
                    try
                    {
                        RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL = new RA42_COMPANY_PASS_DTL();

                        rA42_COMPANY_PASS_DTL.SERVICE_NUMBER = rA42_PERMITS_DTL.RESPONSIBLE;
                        rA42_COMPANY_PASS_DTL.STATION_CODE = rA42_PERMITS_DTL.STATION_CODE;
                        rA42_COMPANY_PASS_DTL.ACCESS_TYPE_CODE = rA42_PERMITS_DTL.ACCESS_TYPE_CODE;
                        rA42_COMPANY_PASS_DTL.CARD_FOR_CODE = rA42_PERMITS_DTL.CARD_FOR_CODE;
                        rA42_COMPANY_PASS_DTL.RESPONSIBLE = rA42_PERMITS_DTL.UNIT_A;
                        rA42_COMPANY_PASS_DTL.COMPANY_TYPE_CODE = 2;
                        rA42_COMPANY_PASS_DTL.COMPANY_CODE = rA42_PERMITS_DTL.COMPANY_CODE;

                        if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                        {

                            rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE;
                            rA42_COMPANY_PASS_DTL.REJECTED = false;
                            rA42_COMPANY_PASS_DTL.STATUS = false;
                            rA42_COMPANY_PASS_DTL.ISOPENED = false;
                        }
                        //this section is for autho person 
                        if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                        {
                            //he should redirect this request to the permits cell 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_COMPANY_PASS_DTL.APPROVAL_SN = currentUser;
                                rA42_COMPANY_PASS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                rA42_COMPANY_PASS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                rA42_COMPANY_PASS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                rA42_COMPANY_PASS_DTL.REJECTED = false;
                                rA42_COMPANY_PASS_DTL.STATUS = false;
                                rA42_COMPANY_PASS_DTL.ISOPENED = false;
                            }


                        }
                        //this section is for permits cell
                        if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                        {
                            //permits cell should redirect the request for the security officer 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_COMPANY_PASS_DTL.PERMIT_SN = currentUser;
                                rA42_COMPANY_PASS_DTL.PERMIT_RANK = ViewBag.RNK;
                                rA42_COMPANY_PASS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                rA42_COMPANY_PASS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                rA42_COMPANY_PASS_DTL.REJECTED = false;
                                rA42_COMPANY_PASS_DTL.STATUS = false;
                                rA42_COMPANY_PASS_DTL.ISOPENED = true;
                            }


                        }
                        //this section is for security officer 
                        if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                        {
                            //afetr he create and complete the request, the request will redirected for the permit cell for printing 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == type && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_COMPANY_PASS_DTL.AUTHO_SN = currentUser;
                                rA42_COMPANY_PASS_DTL.AUTHO_RANK = ViewBag.RNK;
                                rA42_COMPANY_PASS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                rA42_COMPANY_PASS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                rA42_COMPANY_PASS_DTL.REJECTED = false;
                                rA42_COMPANY_PASS_DTL.STATUS = true;
                                rA42_COMPANY_PASS_DTL.ISOPENED = true;
                            }


                        }
                        rA42_COMPANY_PASS_DTL.REMARKS = rA42_PERMITS_DTL.REMARKS;
                        rA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS = rA42_PERMITS_DTL.PURPOSE_OF_PASS;
                        rA42_COMPANY_PASS_DTL.CRD_BY = currentUser;
                        rA42_COMPANY_PASS_DTL.CRD_DT = DateTime.Now;
                        rA42_COMPANY_PASS_DTL.UPD_BY = currentUser;
                        rA42_COMPANY_PASS_DTL.UPD_DT = DateTime.Now;
                        rA42_COMPANY_PASS_DTL.BARCODE = rA42_PERMITS_DTL.BARCODE;
                        db.RA42_COMPANY_PASS_DTL.Add(rA42_COMPANY_PASS_DTL);
                        db.SaveChanges();
                        rA42_PERMITS_DTL.COMPANY_PASS_CODE = rA42_COMPANY_PASS_DTL.COMPANY_PASS_CODE;
                    }
                    catch (Exception ex)
                    {
                        TempData["Erorr"] = ex.GetBaseException();
                        return View(rA42_PERMITS_DTL);
                    }


                }
                    string barcode5 = Guid.NewGuid().ToString().Substring(0, 5);
                    rA42_PERMITS_DTL.BARCODE = barcode5;

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



                                fileName = "Profile_" + rA42_PERMITS_DTL.ACCESS_TYPE_CODE + "_" + permit + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                                // store the file inside ~/Files/Profiles/ folder
                                bool check = general.ResizeImage(PERSONAL_IMAGE, fileName);

                                if (check != true)
                                {
                                    AddToast(new Toast("",
                                   GetResourcesValue("error_update_message"),
                                   "red"));
                                    TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                    return View(rA42_PERMITS_DTL);
                                }

                                rA42_PERMITS_DTL.PERSONAL_IMAGE = fileName;


                            }
                            else
                            {
                                //show error if format not supported 
                                AddToast(new Toast("",
                                GetResourcesValue("error_update_message"),
                                "red"));
                                TempData["Erorr"] = "Personal image should be in image format - يجب أن تكون الصورة الشخصية ملف (JPG,GIF)";
                                return View(rA42_PERMITS_DTL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }

                if (rA42_PERMITS_DTL.PERSONAL_IMAGE != null)
                {
                    rA42_PERMITS_DTL.PERSONAL_IMAGE = rA42_PERMITS_DTL.PERSONAL_IMAGE;
                }
                else
                {
                    rA42_PERMITS_DTL.PERSONAL_IMAGE = PermitsDtl.PERSONAL_IMAGE;

                }
                if (is_with_car.Equals("no"))
                {
                    rA42_PERMITS_DTL.VECHILE_CODE = null;
                    rA42_PERMITS_DTL.VECHILE_NAME_CODE = null;
                    rA42_PERMITS_DTL.VECHILE_COLOR_CODE = null;
                    rA42_PERMITS_DTL.PLATE_CHAR_CODE = null;
                    rA42_PERMITS_DTL.PLATE_CODE = null;
                    rA42_PERMITS_DTL.PLATE_NUMBER = null;

                }
                else
                {
                    if (!rA42_PERMITS_DTL.VECHILE_CODE.HasValue || !rA42_PERMITS_DTL.VECHILE_COLOR_CODE.HasValue
                        || !rA42_PERMITS_DTL.VECHILE_NAME_CODE.HasValue || !rA42_PERMITS_DTL.PLATE_CHAR_CODE.HasValue)
                    {
                        TempData["Erorr"] = "بيانات المركبة ناقصة، يرجى وضع بيانات صحيحة - Car details missing";
                        return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });
                    }
                }
                if ((FORCE_CODE == "2" || FORCE_CODE == "10") && employees_and_families.Contains(permit))
                {
                    rA42_PERMITS_DTL.DATE_TO = rA42_PERMITS_DTL.DATE_TO.Value.AddDays(30);
                }
                var not_same_workflow = new List<string>() { "1", "6", "10" };


                if (!not_same_workflow.Contains(type.ToString()) && !RnoVisitors.Contains(permit))
                {

                    //this section is for applicant 
                    if (WORKFLOWID <= 1 || (WORKFLOWID >= 5 && WORKFLOWID <= 11) || ViewBag.NOT_RELATED_STATION == true)
                    {

                        if (FORCE_CODE == "3" && employees_and_families.Contains(permit.ToString()))
                        {
                            //autho person should redirect the permit to the permits cell 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE
                            && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == rA42_PERMITS_DTL.ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                                rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }

                        }
                        else
                        {
                            rA42_PERMITS_DTL.REJECTED = false;
                            rA42_PERMITS_DTL.STATUS = false;
                            rA42_PERMITS_DTL.ISOPENED = false;
                        }
                    }
                    //this section is for autho person 
                    if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                    {
                        //autho person should redirect the permit to the permits cell 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE
                        && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == rA42_PERMITS_DTL.ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                        }
                        else
                        {
                            rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                            rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                            rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                            rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                            rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                            rA42_PERMITS_DTL.REJECTED = false;
                            rA42_PERMITS_DTL.STATUS = false;
                            rA42_PERMITS_DTL.ISOPENED = true;
                        }

                    }
                    //this section is for permits cell 
                    if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                    {
                        if (FORCE_CODE == "3" && WORKFLOWID == 3 && employees_and_families.Contains(permit.ToString()))
                        {
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE
                            && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == rA42_PERMITS_DTL.ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = true;
                                rA42_PERMITS_DTL.ISOPENED = true;
                                string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                                rA42_PERMITS_DTL.BARCODE = barcode;
                            }




                        }
                        else
                        {
                            //permits cell should redirect the permit for the security officer as final step 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE
                            && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == rA42_PERMITS_DTL.ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                                
                            }

                        }
                    }
                    if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                    {
                        //security officer should redirect the permit to the permits cel for printing
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE
                        && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == rA42_PERMITS_DTL.ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                        }
                        else
                        {
                            rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                            rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                            rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                            rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                            rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                            rA42_PERMITS_DTL.REJECTED = false;
                            rA42_PERMITS_DTL.STATUS = true;
                            rA42_PERMITS_DTL.ISOPENED = true;
                        }



                    }

                    if (permit == "15")
                    {
                        //autho person should redirect the permit to the permits cell 
                        var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE
                        && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == rA42_PERMITS_DTL.ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                        if (v == null)
                        {
                            TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                            return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                        }
                        else
                        {
                            rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                            rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                            rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                            rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                            rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;

                            rA42_PERMITS_DTL.DATE_FROM = DateTime.Today;
                            rA42_PERMITS_DTL.DATE_TO = DateTime.Today.AddDays(300);
                            rA42_PERMITS_DTL.REJECTED = false;
                            rA42_PERMITS_DTL.STATUS = true;
                            rA42_PERMITS_DTL.ISOPENED = true;
                            rA42_PERMITS_DTL.ISPRINTED = true;
                            rA42_PERMITS_DTL.ISDELIVERED = true;
                            string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                            rA42_PERMITS_DTL.BARCODE = barcode;
                        }

                    }
                }
                if (not_same_workflow.Contains(type.ToString()))
                {
                    //for autho permit
                    if (type == 1)
                    {
                        if (WORKFLOWID == 2 || WORKFLOWID <= 1)
                        {


                            rA42_PERMITS_DTL.REJECTED = false;
                            rA42_PERMITS_DTL.STATUS = false;
                            rA42_PERMITS_DTL.ISOPENED = false;


                        }
                        if (WORKFLOWID == 5)
                        {

                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 6 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                                rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }


                        }

                        if (WORKFLOWID == 3)
                        {
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 5 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE
                            == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }


                        }
                        if (WORKFLOWID == 4)
                        {
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 5 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE
                            == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }


                        }
                        if (WORKFLOWID == 6)
                        {

                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                                rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = true;
                                rA42_PERMITS_DTL.ISOPENED = true;
                                string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                                rA42_PERMITS_DTL.BARCODE = barcode;
                            }

                        }
                    }
                    //for contracted companies
                    if (type == 6)
                    {
                        if (WORKFLOWID == 2 || WORKFLOWID <= 1)
                        {


                            rA42_PERMITS_DTL.REJECTED = false;
                            rA42_PERMITS_DTL.STATUS = false;
                            rA42_PERMITS_DTL.ISOPENED = false;


                        }
                        if (WORKFLOWID == 7)
                        {

                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                                rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }


                        }

                        if (WORKFLOWID == 3)
                        {
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 4 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE
                            == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }


                        }
                        if (WORKFLOWID == 4)
                        {

                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                                rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = true;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }

                        }
                    }
                    //for aircrew
                    if (type == 10)
                    {
                        //this section is for applicant 
                        if (WORKFLOWID <= 1 || ViewBag.NOT_RELATED_STATION == true)
                        {
                            //the request will redirected to the autho person as normal request 
                            rA42_PERMITS_DTL.REJECTED = false;
                            rA42_PERMITS_DTL.STATUS = false;
                            rA42_PERMITS_DTL.ISOPENED = false;
                        }
                        //this section is for autho person (المنسق الأمني) 
                        if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                        {
                            //the autho person should redirect the request to the permits cell 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 10 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                                rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }

                        }


                        //this is security offecier
                        if ((WORKFLOWID == 10 || WORKFLOWID == 3) && ViewBag.NOT_RELATED_STATION != true)
                        {
                            //after the security oofcier create this permit and completet every thing, the permit should be redirected to the permit cell to print it 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 3 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                if (WORKFLOWID != 3)
                                {
                                    rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                    rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                                    rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                                    rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                    rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                    rA42_PERMITS_DTL.REJECTED = false;
                                    rA42_PERMITS_DTL.STATUS = true;
                                    rA42_PERMITS_DTL.ISOPENED = true;
                                    string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                                    rA42_PERMITS_DTL.BARCODE = barcode;
                                }
                            }

                            if (WORKFLOWID == 3)
                            {
                                rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;

                            }
                        }
                    }

                }
                if (RnoVisitors.Contains(permit))
                {
                    rA42_PERMITS_DTL.PASS_TYPE_CODE = 2;
                    if (permit != "12")
                    {
                        //this section is for autho person 
                        if ((WORKFLOWID == 2 || WORKFLOWID == 3 || ViewBag.RESPO_STATE <= 1))
                        {

                            //he should redirect the request to permits cell 
                            var v = await db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 11 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE ==
                            STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).FirstOrDefaultAsync();
                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                                rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = false;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }




                        }
                        //this is security officer section
                        if ((WORKFLOWID == 4 || WORKFLOWID == 11) && ViewBag.NOT_RELATED_STATION != true)
                        {

                            //security officer should redirect the complete request to permits cell for printing 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 11 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                                rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = true;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }



                        }

                        if (permit == "16")
                        {
                            rA42_PERMITS_DTL.DATE_FROM = DateTime.Today;
                            rA42_PERMITS_DTL.DATE_TO = DateTime.Today;

                        }

                    }
                    else
                    {

                        if (WORKFLOWID == 11)
                        {

                            rA42_PERMITS_DTL.REJECTED = false;
                            rA42_PERMITS_DTL.STATUS = false;
                            rA42_PERMITS_DTL.ISOPENED = false;


                        }
                        //this section is for autho person 
                        if (WORKFLOWID == 2 && ViewBag.NOT_RELATED_STATION != true)
                        {
                            //he should redirect the request to permits cell 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 11 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).FirstOrDefault();
                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            rA42_PERMITS_DTL.APPROVAL_SN = currentUser;
                            rA42_PERMITS_DTL.APPROVAL_RANK = ViewBag.RNK;
                            rA42_PERMITS_DTL.APPROVAL_NAME = ViewBag.FULL_NAME;
                            rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = DateTime.Now;
                            rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE;
                            rA42_PERMITS_DTL.REJECTED = false;
                            rA42_PERMITS_DTL.STATUS = true;
                            rA42_PERMITS_DTL.ISOPENED = true;
                            string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                            rA42_PERMITS_DTL.BARCODE = barcode;
                        }

                        //permits cell section
                        if (WORKFLOWID == 3 && ViewBag.NOT_RELATED_STATION != true)
                        {
                            //they should redirect the permit to the securiy officer to approve the permit 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 11 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                                rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = true;
                                rA42_PERMITS_DTL.ISOPENED = true;
                                string barcode = Guid.NewGuid().ToString().Substring(0, 5);
                                rA42_PERMITS_DTL.BARCODE = barcode;
                            }


                        }
                        //this is security officer section
                        if (WORKFLOWID == 4 && ViewBag.NOT_RELATED_STATION != true)
                        {

                            //security officer should redirect the complete request to permits cell for printing 
                            var v = db.RA42_ACCESS_SELECT_MST.Where(a => a.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.WORKFLOW_SECRET_CODE == 11 && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true).FirstOrDefault();

                            if (v == null)
                            {
                                TempData["Erorr"] = Resources.Passes.ResourceManager.GetString("Not_set_respond" + "_" + ViewBag.lang);
                                return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });

                            }
                            else
                            {
                                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = v.WORKFLOW_RESPO_CODE.Value;
                                rA42_PERMITS_DTL.AUTHO_SN = currentUser;
                                rA42_PERMITS_DTL.AUTHO_RANK = ViewBag.RNK;
                                rA42_PERMITS_DTL.AUTHO_NAME = ViewBag.FULL_NAME;
                                rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = DateTime.Now;
                                rA42_PERMITS_DTL.REJECTED = false;
                                rA42_PERMITS_DTL.STATUS = true;
                                rA42_PERMITS_DTL.ISOPENED = true;
                            }



                        }
                    }

                }
                rA42_PERMITS_DTL.CRD_BY = currentUser;
                rA42_PERMITS_DTL.CRD_DT = DateTime.Now;
                rA42_PERMITS_DTL.UPD_BY = currentUser;
                rA42_PERMITS_DTL.UPD_DT = DateTime.Now;
               
                db.RA42_PERMITS_DTL.Add(rA42_PERMITS_DTL);
                await db.SaveChangesAsync();
                    
               
               
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
                        rA42_ZONE_MASTER_MST.ACCESS_TYPE_CODE = rA42_PERMITS_DTL.ACCESS_TYPE_CODE;
                        if(permit !="25" && type == 5)
                        {
                            rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_PERMITS_DTL.COMPANY_PASS_CODE;
                        }
                        else
                        {
                            rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE;

                        }
                        rA42_ZONE_MASTER_MST.ZONE_CODE = ZONE[i];
                        rA42_ZONE_MASTER_MST.CRD_BY = currentUser;
                        rA42_ZONE_MASTER_MST.CRD_DT = DateTime.Now;
                        db.RA42_ZONE_MASTER_MST.Add(rA42_ZONE_MASTER_MST);
                        db.SaveChanges();
                        //continue;
                    }

                }

                var zones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == PERMIT_ID && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitsDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && a.CRD_DT > new DateTime(2024, 8, 1)).ToList();
                //add previues zones to new zone
                foreach (var zone in zones)
                {
                    if (permit != "25" && type == 5)
                    {
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_PERMITS_DTL.COMPANY_PASS_CODE;
                    }
                    else
                    {
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE;

                    }
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


                                    fileName = "FileNO" + c + "_" + rA42_PERMITS_DTL.ACCESS_TYPE_CODE + "_" + permit + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);


                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = rA42_PERMITS_DTL.ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE,
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
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.ACCESS_TYPE_CODE == rA42_PERMITS_DTL.ACCESS_TYPE_CODE).ToList();
                                    foreach (var del in delete)
                                    {
                                        string filpath = "~/Documents/" + del.FILE_NAME;
                                        general.RemoveFileFromServer(filpath);
                                        db.RA42_FILES_MST.Remove(del);
                                        db.SaveChanges();
                                    }
                                    TempData["Erorr"] = "Not sopported files format - صيغة الملف غير مدعومة";
                                    return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });
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
                var selected_files = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == PERMIT_ID && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitsDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && a.CRD_DT > new DateTime(2024, 8, 1)).ToList();
                foreach (var file in selected_files)
                {
                    //add new file
                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                    {
                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                        ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE,
                        FILE_TYPE = file.FILE_TYPE,
                        FILE_TYPE_TEXT = file.FILE_TYPE_TEXT,
                        FILE_NAME = file.FILE_NAME,
                        CRD_BY = currentUser,
                        CRD_DT = DateTime.Now


                    };
                    db.RA42_FILES_MST.Add(fILES_MST);
                    db.SaveChanges();
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
                            rA42_MEMBERS_DTL.ACCESS_TYPE_CODE = rA42_PERMITS_DTL.ACCESS_TYPE_CODE;
                            rA42_MEMBERS_DTL.ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE;
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

                                        fileName = "Relative_Profile_" + rA42_PERMITS_DTL.ACCESS_TYPE_CODE + "_" + permit + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                                        // store the file inside ~/Files/Profiles/ folder
                                        bool check = general.ResizeImage(RELATIVE_IMAGE[i], fileName);

                                        if (check != true)
                                        {
                                            AddToast(new Toast("",
                                           GetResourcesValue("error_update_message"),
                                           "red"));
                                            TempData["Erorr"] = "حدث خطأ ماء أثناء رفع الصورة الشخصية - There is somthing wrong while uploading personal image";
                                            return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });
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
                                        return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });
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
                        return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });
                    }
                }

                var relatevies = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_ROW_CODE == PERMIT_ID && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == PermitsDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.DLT_STS != true && a.CRD_DT > new DateTime(2024, 8, 1)).ToList();
                //add previues relatives to new relatives
                foreach (var relative in relatevies)
                {
                    rA42_MEMBERS_DTL.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                    rA42_MEMBERS_DTL.ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE;
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
                }
                catch (DbEntityValidationException cc)
                {
                    foreach (var validationerrors in cc.EntityValidationErrors)
                    {
                        foreach (var validationerror in validationerrors.ValidationErrors)
                        {
                            TempData["Erorr"] = validationerror.PropertyName + " + " + validationerror.ErrorMessage;
                            return RedirectToAction("Renew", new { id = PermitsDtl.PERMIT_CODE, tab = tab });
                        }
                    }

                }
                AddToast(new Toast("",
                    GetResourcesValue("success_renew_message"),
                    "green"));
                if (WORKFLOWID == 11 && RnoVisitors.Contains(permit) && permit != "12")
                {
                    return RedirectToAction("RnoTempPermitsManage", "Permitsdtl");

                }
                return RedirectToAction("Index", "Mypasses");
            }




          

            TempData["Erorr"] = "Somthing wrong happen,حدث خطأ ما";
            AddToast(new Toast("",
                GetResourcesValue("error_update_message"),
                "red"));
            return View(rA42_PERMITS_DTL);
        }




        // this section to proccess any family member permit by authorized users inside the sytem such as permits cell, security officer and autho person and applicant himself 
        public ActionResult PrintMember(int? id)
        {
            ViewBag.activetab = "ToPrint";
            ViewBag.MEMBER_CODE = id;
            if (id == null)
            {
                return NotFound();
            }
            var checkMember = db.RA42_MEMBERS_DTL.Where(a => a.MEMBER_CODE == id).FirstOrDefault();
            if (checkMember == null)
            {
                return NotFound();
            }
            RA42_PERMITS_DTL rA42_PERMITS_DTL = db.RA42_PERMITS_DTL.Find(checkMember.ACCESS_ROW_CODE);
            if (rA42_PERMITS_DTL == null)
            {
                return NotFound();
            }
            //check if user has permission to edit this permit 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_PERMITS_DTL.SERVICE_NUMBER != currentUser && rA42_PERMITS_DTL.RESPONSIBLE != currentUser)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        return NotFound();
                    }
                }
                if (rA42_PERMITS_DTL.ISOPENED == true)
                {
                    if (ViewBag.ADMIN != true && ViewBag.DEVELOPER != true)
                    {
                        if (rA42_PERMITS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == 2 && rA42_PERMITS_DTL.REJECTED == true)
                        {

                        }
                        else
                        {
                            if (rA42_PERMITS_DTL.ISOPENED == true)
                            {
                                return NotFound();
                            }

                        }

                    }
                }
            }
            else
            {
                if (rA42_PERMITS_DTL.SERVICE_NUMBER == currentUser || rA42_PERMITS_DTL.RESPONSIBLE == currentUser)
                {

                }
                else
                {
                    if (ViewBag.RESPO_STATE != rA42_PERMITS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID)
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
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_PERMITS_DTL.PASS_TYPE_CODE);
                if (rA42_PERMITS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_PERMITS_DTL.PASS_TYPE_CODE);

                }
                //get zones and gates in english
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_PERMITS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_PERMITS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_PERMITS_DTL.PASS_TYPE_CODE);
                if (rA42_PERMITS_DTL.STATION_CODE == 26)
                {
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE", rA42_PERMITS_DTL.PASS_TYPE_CODE);

                }
                //get zones and gates in ar
                ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == rA42_PERMITS_DTL.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_PERMITS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_PERMITS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                  where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_PERMITS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_PERMITS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == rA42_PERMITS_DTL.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == rA42_PERMITS_DTL.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            //get selected gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();
            //get documents
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE).ToList();

            //get personal image 
            ViewBag.PERSONAL_IMAGE = checkMember.PERSONAL_IMAGE;
            //get status of the permit 
            ViewBag.STATUS = rA42_PERMITS_DTL.STATUS;

            rA42_PERMITS_DTL.RELATIVE_TYPE_CODE = checkMember.RELATIVE_TYPE_CODE.Value;
            rA42_PERMITS_DTL.CIVIL_NUMBER = checkMember.CIVIL_NUMBER;
            rA42_PERMITS_DTL.NAME_A = checkMember.FULL_NAME;
            rA42_PERMITS_DTL.PHONE_NUMBER = checkMember.PHONE_NUMBER;
            rA42_PERMITS_DTL.PERSONAL_IMAGE = checkMember.PERSONAL_IMAGE;
            rA42_PERMITS_DTL.IDENTITY_CODE = checkMember.IDENTITY_CODE.Value;
            rA42_PERMITS_DTL.GENDER_ID = checkMember.GENDER_ID.Value;
            rA42_PERMITS_DTL.REMARKS = checkMember.REMARKS;
            return View(rA42_PERMITS_DTL);
        }

        //post new data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PrintMember(RA42_PERMITS_DTL rA42_PERMITS_DTL, string COMMENT,
            int[] FILE_TYPES, string[] FILE_TYPES_TEXT, int[] ZONE, int FAMILY_ID, int MEMBER_CODE, HttpPostedFileBase[] files, HttpPostedFileBase PERSONAL_IMAGE)
        {
            var general_data = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == FAMILY_ID).FirstOrDefault();
            var relative_data = db.RA42_MEMBERS_DTL.Where(a => a.MEMBER_CODE == MEMBER_CODE).FirstOrDefault();
            if (relative_data == null || general_data == null)
            {
                return NotFound();
            }
            ViewBag.activetab = "ToPrint";

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
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_PERMITS_DTL.IDENTITY_CODE);
                //get permits types in english 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE_E", rA42_PERMITS_DTL.PASS_TYPE_CODE);
                if (rA42_PERMITS_DTL.STATION_CODE == 26)
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
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_PERMITS_DTL.GENDER_ID);
                //get relatives types in english
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_PERMITS_DTL.RELATIVE_TYPE_CODE);
                //get gates in english 
                //ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G"), "ZONE_CODE", "ZONE_NAME_E");
                //ViewBag.SUB_ZONE_AREA_CODE = new SelectList(db.RA42_ZONE_SUB_AREA_MST.Where(a => a.RA42_ZONE_AREA_MST.STATION_CODE == general_data.STATION_CODE && a.DLT_STS != true), "ZONE_SUB_AREA_CODE", "SUB_ZONE_NAME_E");




            }
            else
            {
                //get identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A", rA42_PERMITS_DTL.IDENTITY_CODE);
                //get permits types in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE", rA42_PERMITS_DTL.PASS_TYPE_CODE);
                if (rA42_PERMITS_DTL.STATION_CODE == 26)
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
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A", rA42_PERMITS_DTL.GENDER_ID);
                //get relatives types in arabic 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE", rA42_PERMITS_DTL.RELATIVE_TYPE_CODE);
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
                                rA42_PERMITS_DTL.PERSONAL_IMAGE = fileName;



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
                if (rA42_PERMITS_DTL.PERSONAL_IMAGE != null)
                {
                    rA42_PERMITS_DTL.PERSONAL_IMAGE = rA42_PERMITS_DTL.PERSONAL_IMAGE;
                }
                else
                {
                    rA42_PERMITS_DTL.PERSONAL_IMAGE = relative_data.PERSONAL_IMAGE;

                }
                //get host details from api
                //rA42_FAMILY_PASS_DTL.SERVICE_NUMBER = rA42_FAMILY_PASS_DTL.SERVICE_NUMBER.ToUpper();
                rA42_PERMITS_DTL.RESPONSIBLE = rA42_PERMITS_DTL.RESPONSIBLE;
                rA42_PERMITS_DTL.HOST_RANK_A = rA42_PERMITS_DTL.HOST_RANK_A;
                rA42_PERMITS_DTL.HOST_RANK_E = rA42_PERMITS_DTL.HOST_RANK_E;
                rA42_PERMITS_DTL.HOST_NAME_A = rA42_PERMITS_DTL.HOST_NAME_A;
                rA42_PERMITS_DTL.HOST_NAME_E = rA42_PERMITS_DTL.HOST_NAME_E;
                rA42_PERMITS_DTL.PROFESSION_A = rA42_PERMITS_DTL.PROFESSION_A;
                rA42_PERMITS_DTL.PROFESSION_E = rA42_PERMITS_DTL.PROFESSION_E;
                rA42_PERMITS_DTL.NAME_A = rA42_PERMITS_DTL.NAME_A;
                rA42_PERMITS_DTL.NAME_E = rA42_PERMITS_DTL.NAME_E;
                rA42_PERMITS_DTL.UNIT_A = rA42_PERMITS_DTL.UNIT_A;
                rA42_PERMITS_DTL.UNIT_E = rA42_PERMITS_DTL.UNIT_E;
                rA42_PERMITS_DTL.ACCESS_TYPE_CODE = rA42_PERMITS_DTL.ACCESS_TYPE_CODE;
                rA42_PERMITS_DTL.STATION_CODE = general_data.STATION_CODE;
                rA42_PERMITS_DTL.CARD_FOR_CODE = general_data.CARD_FOR_CODE;
                rA42_PERMITS_DTL.GENDER_ID = rA42_PERMITS_DTL.GENDER_ID;
                rA42_PERMITS_DTL.IDENTITY_CODE = rA42_PERMITS_DTL.IDENTITY_CODE;
                rA42_PERMITS_DTL.CIVIL_NUMBER = rA42_PERMITS_DTL.CIVIL_NUMBER;
              
                rA42_PERMITS_DTL.APPROVAL_SN = general_data.APPROVAL_SN;
                rA42_PERMITS_DTL.APPROVAL_RANK = general_data.APPROVAL_RANK;
                rA42_PERMITS_DTL.APPROVAL_NAME = general_data.APPROVAL_NAME;
                rA42_PERMITS_DTL.APPROVAL_APPROVISION_DATE = general_data.APPROVAL_APPROVISION_DATE;
                
                rA42_PERMITS_DTL.PERMIT_SN = currentUser;
                rA42_PERMITS_DTL.PERMIT_RANK = ViewBag.RNK;
                rA42_PERMITS_DTL.PERMIT_NAME = ViewBag.FULL_NAME;
                rA42_PERMITS_DTL.PERMIT_APPROVISION_DATE = DateTime.Now;
                rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE = rA42_PERMITS_DTL.WORKFLOW_RESPO_CODE;
                rA42_PERMITS_DTL.AUTHO_SN = general_data.AUTHO_SN;
                rA42_PERMITS_DTL.AUTHO_RANK = general_data.AUTHO_RANK;
                rA42_PERMITS_DTL.AUTHO_NAME = general_data.AUTHO_NAME;
                rA42_PERMITS_DTL.AUTHO_APPROVISION_DATE = general_data.AUTHO_APPROVISION_DATE;
                rA42_PERMITS_DTL.REJECTED = false;
                rA42_PERMITS_DTL.STATUS = true;
                rA42_PERMITS_DTL.ISOPENED = true;
                rA42_PERMITS_DTL.ISPRINTED = false;
                rA42_PERMITS_DTL.BARCODE = rA42_PERMITS_DTL.BARCODE;
                rA42_PERMITS_DTL.CRD_BY = currentUser;
                rA42_PERMITS_DTL.CRD_DT = DateTime.Now;
                rA42_PERMITS_DTL.UPD_BY = currentUser;
                rA42_PERMITS_DTL.UPD_DT = DateTime.Now;
                db.RA42_PERMITS_DTL.Add(rA42_PERMITS_DTL);
                db.SaveChanges();


                //add comments
                if (COMMENT.Length > 0)
                {
                    RA42_COMMENTS_MST rA42_COMMENT = new RA42_COMMENTS_MST();
                    rA42_COMMENT.ACCESS_TYPE_CODE = ACCESS_TYPE_CODE;
                    rA42_COMMENT.PASS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE;
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
                        rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE;
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
                    rA42_ZONE_MASTER_MST.ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE;
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

                                    fileName = "FileNO" + c + "_4_" + DateTime.Now.ToString("yymmssfff") + extension;
                                    // store the file inside ~/App_Data/uploads folder
                                    string path = Path.Combine(Server.MapPath("~/Files/Documents/"), fileName);
                                    file.SaveAs(path);
                                    //add new file
                                    RA42_FILES_MST fILES_MST = new RA42_FILES_MST()
                                    {
                                        ACCESS_TYPE_CODE = ACCESS_TYPE_CODE,
                                        ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE,
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
                                    var delete = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE).ToList();
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
                        ACCESS_ROW_CODE = rA42_PERMITS_DTL.PERMIT_CODE,
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
                catch (Exception xcv)
                {
                    TempData["Erorr"] = xcv.GetBaseException();
                    return View(rA42_PERMITS_DTL);
                }

                TempData["status"] = "تم حفظ البيانات بنجاح!";


                return RedirectToAction("Card", new { id = rA42_PERMITS_DTL.PERMIT_CODE });







            }
            TempData["Erorr"] = "Somthing wrong happen,حدث خطأ ما";
            AddToast(new Toast("",
                GetResourcesValue("error_update_message"),
                "red"));
            return View(general_data);
        }


        // GET: RA42_PERMITS_DTL/Delete/5
        [HttpGet]
        public async Task<ActionResult> Delete(int? id,string tab)
		{
            ViewBag.activetab = tab;

            if (id == null)
            {
                return NotFound();
            }
            //check if the record id is in the db table 
            RA42_PERMITS_DTL rA42_PERMITS_DTL = db.RA42_PERMITS_DTL.Find(id);
            if (rA42_PERMITS_DTL == null)
            {
                return NotFound();
            }
            var RnoVisitors = new List<string>() { "11", "12", "13", "14", "16" };
            if (RnoVisitors.Contains(rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE))
            {
                ViewBag.Managepasses = "";
                ViewBag.RnoTempPermits = "RnoTempPermits";
            }
            //check if the current user has authority to view details 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_PERMITS_DTL.SERVICE_NUMBER != currentUser && rA42_PERMITS_DTL.RESPONSIBLE != currentUser)
                {

                    return NotFound();

                }
            }

            //get relatives of this permit 
            ViewBag.GetRelativs = await db.RA42_MEMBERS_DTL.Where(a => a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.PERMIT_CODE && a.DLT_STS != true && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();
            //get selected zones and gates
            if (rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE != 5)
            {
                ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();
            }
            else
            {
                if (rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "25")
                {
                    ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();

                }
                else
                {
                    ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_PERMITS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();
                }
            }
            //get selected documenst 
            ViewBag.GetFiles = await db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1) && !a.CONTROLLER_NAME.Equals("Companies")).ToListAsync();
            //get comments of the request
            if (rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE != 5 || rA42_PERMITS_DTL.RA42_CARD_FOR_MST.CARD_SECRET_CODE == "25")
            {
                var cOMMENTS = await db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CRD_DT > new DateTime(2024, 8, 1)).ToListAsync();
                ViewBag.COMMENTS = cOMMENTS;
            }
            else
            {
                var cOMMENTS = await db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == rA42_PERMITS_DTL.COMPANY_PASS_CODE && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_PERMITS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).ToListAsync();
                ViewBag.COMMENTS = cOMMENTS;
            }
            return View(rA42_PERMITS_DTL);
        }

        // POST: RA42_PERMITS_DTL/Delete/5
        [HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public ActionResult DeleteConfirmed(int id)
		{
            var PermitDtl = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == id).FirstOrDefault();

            if (PermitDtl != null)
            {


                PermitDtl.UPD_BY = currentUser;
                PermitDtl.UPD_DT = DateTime.Now;
                PermitDtl.DLT_STS = true;
                db.Entry(PermitDtl).State = EntityState.Modified;
                db.SaveChanges();
                AddToast(new Toast("",
                GetResourcesValue("success_delete_message"),
                "green"));
                var RnoVisitors = new List<string>() { "11", "12", "13", "14", "16" };

                if (WORKFLOWID == 11 && RnoVisitors.Contains(PermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE) && PermitDtl.RA42_CARD_FOR_MST.CARD_SECRET_CODE != "12")
                {
                    return RedirectToAction("RnoTempPermitsManage", "Permitsdtl");

                }

                if (ViewBag.RESPO_STATE <= 1)
                {

                    return RedirectToAction("Index", "MyPasses", new {tab="employee"});
                }
                else
                {
                    return RedirectToAction("Index", new { type = PermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE });
                }

            }
            AddToast(new Toast("",
               GetResourcesValue("Dont_have_permissions_to_dlt"),
               "red"));
            if (ViewBag.RESPO_STATE <= 1)
            {
                return RedirectToAction("Index", "MyPasses", new { tab = "employee" });

            }
            else
            {
                return RedirectToAction("Index", "Mypasses", new {tab="employee"});
            }
        }

        // details view of any company permit 
        [HttpGet]
        public async Task<ActionResult> CompanyPermitDelete(int? id, string tab)
        {
            ViewBag.activetab = tab;

            if (id == null)
            {
                return NotFound();
            }
            //check if the record id is in the db table 
            RA42_COMPANY_PASS_DTL rA42_COMPANY_PASS_DTL = db.RA42_COMPANY_PASS_DTL.Find(id);
            if (rA42_COMPANY_PASS_DTL == null)
            {
                return NotFound();
            }

            //check if the current user has authority to view details 
            if (ViewBag.RESPO_STATE <= 1)
            {
                if (rA42_COMPANY_PASS_DTL.SERVICE_NUMBER != currentUser)
                {

                    return NotFound();

                }
            }

            //get employees of this permit 
            ViewBag.GetEmployees = await db.RA42_PERMITS_DTL.Where(a => a.COMPANY_PASS_CODE == id && a.DLT_STS != true).ToListAsync();
            foreach (var item in ViewBag.GetEmployees)
            {
                ViewBag.Permit_code_emp = item.PERMIT_CODE;
            }
            //get selected zones and gates
            ViewBag.GetZones = await db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).ToListAsync();
            //get selected documenst 
            ViewBag.GetFiles = await db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE && a.CONTROLLER_NAME.Equals("Companies")).ToListAsync();
            //get comments of the request
            var cOMMENTS = await db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == id && a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == rA42_COMPANY_PASS_DTL.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE).ToListAsync();
            ViewBag.COMMENTS = cOMMENTS;

            return View(rA42_COMPANY_PASS_DTL);
        }

        // POST: RA42_PERMITS_DTL/Delete/5
        [HttpPost, ActionName("CompanyPermitDelete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteComapnyEmployeesPermitConfirmed(int id)
        {
            var CompanyPermitDtl = db.RA42_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == id).FirstOrDefault();

            if (CompanyPermitDtl != null)
            {


                CompanyPermitDtl.UPD_BY = currentUser;
                CompanyPermitDtl.UPD_DT = DateTime.Now;
                CompanyPermitDtl.DLT_STS = true;
                db.Entry(CompanyPermitDtl).State = EntityState.Modified;
                db.SaveChanges();
                AddToast(new Toast("",
                GetResourcesValue("success_delete_message"),
                "green"));
              

                if (ViewBag.RESPO_STATE <= 1)
                {

                    return RedirectToAction("Index", "MyPasses", new { tab = "others" });
                }
                else
                {
                    return RedirectToAction("Index", new { type = CompanyPermitDtl.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE });
                }

            }
            AddToast(new Toast("",
               GetResourcesValue("Dont_have_permissions_to_dlt"),
               "red"));
            if (ViewBag.RESPO_STATE <= 1)
            {
                return RedirectToAction("Index", "MyPasses", new { tab = "others" });

            }
            else
            {
                return RedirectToAction("Index", "Mypasses", new { tab = "others" });
            }
        }
        [HttpGet]
        public ActionResult GetPassForm(int index,int station,string permit)
        {
            STATION_CODE = station;

            //get station name 
            var check_unit = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == station).FirstOrDefault();
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
                ViewBag.HQ_UNIT = station;
                FORCE_CODE = check_unit.RA42_FORCES_MST.FORCE_CODE.ToString();
                ViewBag.Selected_Station = STATION_CODE;
                ViewBag.Selected_Force = FORCE_CODE;

            }

            if (Language.GetCurrentLang() == "en")
            {

                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E");
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E");
                if (FORCE_CODE == "3")
                {
                    //this option for RANO, all temprory permits in the visitor permits
                    ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true && a.PASS_TYPE_CODE == 1), "PASS_TYPE_CODE", "PASS_TYPE_E");

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
               
            }
            else
            {

                //get genders in arabic 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_A");
                //get all identities in arabic 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_A");
                //get permits types codes in arabic 
                ViewBag.PASS_TYPE_CODE = new SelectList(db.RA42_PASS_TYPE_MST.Where(a => a.DLT_STS != true), "PASS_TYPE_CODE", "PASS_TYPE");
                if (FORCE_CODE == "3")
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
                
            }
            var model = new RA42_PERMITS_DTL();
            index = index + 1;
            ViewData["index"] = index;
            return PartialView("_CompaniesCreatePermit", model);
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
            
            var vv = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == rA42_MEMBERS_DTL.ACCESS_ROW_CODE && a.CRD_DT > new DateTime(2024, 7, 1)).FirstOrDefault();
            ViewData["permit"] = vv.RA42_CARD_FOR_MST.CARD_SECRET_CODE.ToString();

            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_MEMBERS_DTL.IDENTITY_CODE);
                if(vv.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4)
                {
                    //get gates only in en
                    ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == vv.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");

                }
                else
                {   //get zones and gates in en
                    ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == vv.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");

                }
                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == vv.ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == vv.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == vv.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == vv.ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == vv.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == vv.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                if (vv.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4)
                {
                    //get gates only in arabic 
                    ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == vv.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                }
                else
                {
                    //get zones and gates in arabic 
                    ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == vv.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");

                }


            }

            //get selected zones and gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == rA42_MEMBERS_DTL.ACCESS_ROW_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == vv.ACCESS_TYPE_CODE && a.CRD_DT > new DateTime(2024, 7, 1)).ToList();
            //get selected files 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == rA42_MEMBERS_DTL.ACCESS_ROW_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == vv.ACCESS_TYPE_CODE && a.CRD_DT > new DateTime(2024, 7, 1)).ToList();
            //get documnts types for this kind of permit to check missing files with this request 
            //ViewBag.PASS_FILES = db.RA42_DOCUMENTS_ACCESS_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE_CODE && a.DLT_STS != true && a.RA42_FILE_TYPE_MST.DLT_STS != true ).ToList();
            var f = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == rA42_MEMBERS_DTL.ACCESS_ROW_CODE).FirstOrDefault();
            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == f.ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == f.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == f.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.ACCESS_TYPE_CODE == f.ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == f.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == f.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
            var f = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == v.ACCESS_ROW_CODE).FirstOrDefault();
            ViewData["permit"] = f.RA42_CARD_FOR_MST.CARD_SECRET_CODE.ToString();

            ViewBag.PASS_FILES = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                                  join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                                  join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                                  join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                                  where a.ACCESS_TYPE_CODE == f.ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == f.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == f.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                                    where a.ACCESS_TYPE_CODE == f.ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == f.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == f.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
                                    select new
                                    {
                                        a.FILE_TYPE_CODE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE,
                                        a.RA42_FILE_TYPE_MST.FILE_TYPE_E,

                                    }).ToList();
            //get selected zones and gates 
            ViewBag.GetZones = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_ROW_CODE == v.ACCESS_ROW_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == f.ACCESS_TYPE_CODE && a.CRD_DT > new DateTime(2024, 7, 1)).ToList();
            //get selected files 
            ViewBag.GetFiles = db.RA42_FILES_MST.Where(a => a.ACCESS_ROW_CODE == v.ACCESS_ROW_CODE && a.DLT_STS != true && a.ACCESS_TYPE_CODE == f.ACCESS_TYPE_CODE && a.CRD_DT > new DateTime(2024, 7, 1)).ToList();

            if (Language.GetCurrentLang() == "en")
            {
                //get identities in english 
                ViewBag.IDENTITY_CODE = new SelectList(db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true), "IDENTITY_CODE", "IDENTITY_TYPE_E", rA42_MEMBERS_DTL.IDENTITY_CODE);
                //get relatives types in english 
                ViewBag.RELATIVE_TYPE_CODE = new SelectList(db.RA42_RELATIVE_TYPE_MST.Where(a => a.DLT_STS != true), "RELATIVE_TYPE_CODE", "RELATIVE_TYPE_E", rA42_MEMBERS_DTL.RELATIVE_TYPE_CODE);
                //get genders in english 
                ViewBag.GENDER_ID = new SelectList(db.RA42_GENDERS_MST.Where(a => a.DLT_STS != true), "GENDER_ID", "GENDER_E", rA42_MEMBERS_DTL.GENDER_ID);
                //get zones and gates in english
                if (f.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4)
                {
                    //get gates only in en
                    ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == f.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");

                }
                else
                {   //get zones and gates in en
                    ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == f.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME_E + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");

                }                //get documents types for this kind of permit in english 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == f.ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == f.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == f.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                if (f.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4)
                {
                    //get gates only in arabic 
                    ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == f.STATION_CODE && a.DLT_STS != true && a.ZONE_TYPE == "G").Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");
                }
                else
                {
                    //get zones and gates in arabic 
                    ViewBag.ZONE_CODE = new SelectList(db.RA42_ZONE_AREA_MST.Where(a => a.STATION_CODE == f.STATION_CODE && a.DLT_STS != true).Select(s => new { ZONE_CODE = s.ZONE_CODE, ZONE_NAME = s.ZONE_NAME + (s.REMARKS != null ? " ( " + s.REMARKS + " )" : "") }), "ZONE_CODE", "ZONE_NAME");

                }
                //get documents types for this kind of permit in ar 
                var doc = (from a in db.RA42_DOCUMENTS_ACCESS_MST
                           join c in db.RA42_FILE_TYPE_MST on a.FILE_TYPE_CODE equals c.FILE_TYPE_CODE
                           join d in db.RA42_FILE_FORCES_MST on c.FILE_TYPE_CODE equals d.FILE_TYPE_CODE
                           join z in db.RA42_FILE_CARD_MST on c.FILE_TYPE_CODE equals z.FILE_TYPE_CODE
                           where a.ACCESS_TYPE_CODE == f.ACCESS_TYPE_CODE && a.DLT_STS != true && c.DLT_STS != true && d.FORCE_ID == f.RA42_STATIONS_MST.FORCE_ID && z.CARD_FOR_CODE == f.CARD_FOR_CODE && d.DLT_STS != true && z.DLT_STS != true
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
                    v.ACCESS_ROW_CODE = f.PERMIT_CODE;
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



                                    fileName = "Relative_Profile_" + f.ACCESS_TYPE_CODE + "_" + DateTime.Now.ToString("yymmssfff") + extension;

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
                    var cr = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == v.ACCESS_ROW_CODE).FirstOrDefault();
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
        public JsonResult PrintById(int id, string type)
        {

            bool result = false;
            var general_data = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == id).FirstOrDefault();

            if (general_data != null)
            {


                general_data.UPD_BY = currentUser;
                general_data.UPD_DT = DateTime.Now;
                general_data.ISPRINTED = true;
                db.Entry(general_data).State = EntityState.Modified;
                db.SaveChanges();
                var checkifPrint = db.RA42_PRINT_MST.Where(a => a.ACCESS_TYPE_CODE == general_data.ACCESS_TYPE_CODE && a.PASS_ROW_CODE == id && a.DLT_STS != true).ToList();
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
                rA42_PRINT.ACCESS_TYPE_CODE = general_data.ACCESS_TYPE_CODE;
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

            var company = db.RA42_PERMITS_DTL.Where(a => a.COMPANY_PASS_CODE == general_data.COMPANY_PASS_CODE && a.DLT_STS != true).ToList();
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
                var comp_pass = db.RA42_COMPANY_PASS_DTL.Where(a => a.COMPANY_PASS_CODE == general_data.COMPANY_PASS_CODE).FirstOrDefault();
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
        public ActionResult NotFound()
        {
            return RedirectToAction("NotFound", "Home");
        }

        public bool IsExpiringWithinOneMonth(DateTime expiryDate)
        {
            DateTime currentDate = DateTime.Now;
            DateTime oneMonthBeforeExpiry = expiryDate.AddMonths(-1);

            // Check if the current date is within the last month before the expiry date
            return currentDate >= oneMonthBeforeExpiry && currentDate <= expiryDate;
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
