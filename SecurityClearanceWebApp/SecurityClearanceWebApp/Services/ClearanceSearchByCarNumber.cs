using portal.Controllers;
using SecurityClearanceWebApp.Models;
using SecurityClearanceWebApp.Util;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace SecurityClearanceWebApp.Services
{
    public enum ClearanceSearchtypeByCar
    {
        plate
       /* CarSearch*/ //=> ا-5214
    }
    public class ClearanceSearchByCarNumber
    {

        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        public ClearanceSearchByCarNumber() { }

        public async Task<List<ClearanceSearchResult>> Search(string plate_number,int char_num, ClearanceSearchtypeByCar searchtype)
        {
            var resulat = new List<ClearanceSearchResult>();


            try
            {
                var new_permits = db.RA42_PERMITS_DTL.Where(x => (x.PLATE_NUMBER == plate_number 
                && x.PLATE_CHAR_CODE == char_num) && (x.DLT_STS != true
                && x.WORKFLOW_RESPO_CODE != null)
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
                resulat.AddRange(new_permits);
            }
            catch
            {

            }
            try
            {

                var access_VECHILES = await db.RA42_VECHILE_PASS_DTL.Where(x => (x.PLATE_NUMBER == plate_number && x.PLATE_CHAR_CODE == char_num) && (x.DLT_STS != true 
                && x.WORKFLOW_RESPO_CODE !=null)).ToListAsync();
                var VECHILES = access_VECHILES.Select(r => new ClearanceSearchResult
                {
                    Id = r.VECHILE_PASS_CODE,
                    ControllerName = "Vechilepass",
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
                    CardFor = r.CARD_FOR_CODE.Value,
                    ServiceNumber = r.SERVICE_NUMBER,
                    PesronalImage = r.PERSONAL_IMAGE,
                    PurposeOfPass = r.PURPOSE_OF_PASS,
                    Name = r.NAME_A,
                    Phone = r.PHONE_NUMBER,
                    Gsm = r.GSM,
                    PermitType = "تصريح المركبات" +" - "+ r.RA42_CARD_FOR_MST.CARD_FOR_A,
                    CivilNumber = r.CIVIL_NUMBER,
                    CarName = r.RA42_VECHILE_CATIGORY_MST.VECHILE_CAT,
                    IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                    ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                    CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                    UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                    StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                    Status = r.STATUS,
                    Delivered = r.ISDELIVERED,
                    Printed = r.ISPRINTED,
                    Opened = r.ISOPENED,
                    Rejected = r.REJECTED,
                    PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                    Deleted = r.DLT_STS,
                    Station = r.STATION_CODE.Value,
                    Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                    ResponsipoleServiceNumber = r.RESPONSIBLE,
                    CarColor = r.RA42_VECHILE_COLOR_MST.COLOR,
                    PlateNumber = r.PLATE_NUMBER,
                    PlateCode = r.RA42_PLATE_CHAR_MST.PLATE_CHAR,
                    CarType = r.RA42_VECHILE_NAME_MST.VECHILE_NAME,
                    Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                    WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                    Comments = r.countCommennts(r.VECHILE_PASS_CODE),
                    CompanyPermitId = 0,
                    Violations = r.RA42_ACCESS_TYPE_MST.RA42_VECHILE_VIOLATION_DTL.Where(x => x.DLT_STS != true && x.ACCESS_ROW_CODE == r.VECHILE_PASS_CODE).Count()

                }).OrderByDescending(r=>r.Id).ToList();
                resulat.AddRange(VECHILES);

            }
            catch
            {
                //ignore
                
            }
            try
            {
                var access_VISITORS = await db.RA42_VISITOR_PASS_DTL.Where(x => (x.PLATE_NUMBER == plate_number && x.PLATE_CHAR_CODE == char_num) && (x.DLT_STS != true && x.WORKFLOW_RESPO_CODE != null)).ToListAsync();

                var VISITORS = access_VISITORS.Select(r => new ClearanceSearchResult
                {
                    Id = r.VISITOR_PASS_CODE,
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
                    CardFor = r.CARD_FOR_CODE.Value,
                    ControllerName = "Visitorpass",
                    ServiceNumber = "-",
                    PesronalImage = r.PERSONAL_IMAGE,
                    PurposeOfPass = r.PURPOSE_OF_PASS,
                    Name = r.VISITOR_NAME,
                    Phone = "-",
                    Gsm = r.GSM,
                    PermitType = "تصريح الزوار" + " - " + r.RA42_CARD_FOR_MST.CARD_FOR_A,
                    PassType = "مؤقت",
                    CivilNumber = r.ID_CARD_NUMBER,
                    Opened = r.ISOPENED,
                    IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                    ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                    CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                    UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                    StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                    Station = r.STATION_CODE.Value,
                    Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                    Status = r.STATUS,
                    Delivered = r.ISDELIVERED,
                    Printed = r.ISPRINTED,
                    Rejected = r.REJECTED,
                    Deleted = r.DLT_STS,
                    ResponsipoleServiceNumber = r.SERVICE_NUMBER,
                    Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                    WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                    Comments = r.countCommennts(r.VISITOR_PASS_CODE),
                    CompanyPermitId = 0,
                    Violations = r.RA42_ACCESS_TYPE_MST.RA42_VECHILE_VIOLATION_DTL.Where(x => x.DLT_STS != true && x.ACCESS_ROW_CODE == r.VISITOR_PASS_CODE).Count()



                }).OrderByDescending(r => r.Id).ToList();
                resulat.AddRange(VISITORS);
            }
            catch
            {

            }

            if (resulat.Any())
            {
                DateTime threeYearsAgo = DateTime.Now.AddYears(-3);

                resulat = resulat.Where(r => r.CreatedDate >= threeYearsAgo).ToList();
                var user = new UserInfo();
                var currentUser = user.getSNO();
                switch (searchtype)
                {
                 
                    
                    case ClearanceSearchtypeByCar.plate:
                        {

                            var v = db.RA42_WORKFLOW_RESPONSIBLE_MST.Where(a => a.SERVICE_NUMBER == currentUser && a.ACTIVE != false && a.DLT_STS != true).FirstOrDefault();
                           

                            resulat = resulat.Where(r => r.Station == v.STATION_CODE.Value && r.Deleted != true && r.Printed == true).ToList();
                                
                            
                            break;
                        }
                   
                }
            }

            return resulat;
        }
    }
}