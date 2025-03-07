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
    public enum ClearanceSearchtypeByBarcode
    {
        Barcode,

        /* CarSearch*/ //=> ا-5214
    }
    public class ClearanceSearchServicesByBarcode
    {

        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        public ClearanceSearchServicesByBarcode() { }

        public async Task<List<ClearanceSearchResult>> Search(string key, ClearanceSearchtypeByBarcode searchtype)
        {
            var resulat = new List<ClearanceSearchResult>();


            try
            {
                var new_permits = db.RA42_PERMITS_DTL.Where(x => x.BARCODE == key && (x.DLT_STS != true)
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

                var access_VECHILES = await db.RA42_VECHILE_PASS_DTL.Where(x => x.BARCODE == key && (x.DLT_STS != true)).ToListAsync();
                var VECHILES = access_VECHILES.Select(r => new ClearanceSearchResult
                {
                    Id = r.VECHILE_PASS_CODE,
                    ControllerName = "Vechilepass",
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
                    ServiceNumber = r.SERVICE_NUMBER,
                    PesronalImage = r.PERSONAL_IMAGE,
                    PurposeOfPass = r.PURPOSE_OF_PASS,
                    Name = r.NAME_A,
                    Phone = r.PHONE_NUMBER,
                    Gsm = r.GSM,
                    PermitType = "تصريح المركبات",
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


                }).OrderByDescending(r => r.Id).ToList();
                resulat.AddRange(VECHILES);

            }
            catch
            {
                //ignore

            }
            try
            {
                var access_SECURITY = await db.RA42_SECURITY_PASS_DTL.Where(x => x.BARCODE == key && (x.DLT_STS != true)).ToListAsync();

                var SECURITY = access_SECURITY.Select(r => new ClearanceSearchResult
                {
                    Id = r.SECURITY_CODE,
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
                    ControllerName = "Securitypass",
                    ServiceNumber = r.SERVICE_NUMBER,
                    PesronalImage = r.PERSONAL_IMAGE,
                    PurposeOfPass = r.PURPOSE_OF_PASS,
                    Name = r.NAME_A,
                    Phone = r.PHONE_NUMBER,
                    Gsm = r.GSM,
                    PermitType = "التصريح الشخصي",
                    PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                    CivilNumber = r.CIVIL_NUMBER,
                    Printed = r.ISPRINTED,
                    Opened = r.ISOPENED,
                    IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                    ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                    CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                    UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                    StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                    Status = r.STATUS,
                    Delivered = r.ISDELIVERED,
                    Station = r.STATION_CODE.Value,
                    Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                    Rejected = r.REJECTED,
                    Deleted = r.DLT_STS,
                    ResponsipoleServiceNumber = r.RESPONSIBLE,
                    Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                    WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                    Comments = r.countCommennts(r.SECURITY_CODE),
                    CompanyPermitId = 0,
                    Violations = 0



                }).OrderByDescending(r => r.Id).ToList();
                resulat.AddRange(SECURITY);

            }
            catch
            {

            }
            try
            {
                var access_FAMILY = await db.RA42_FAMILY_PASS_DTL.Where(x => x.BARCODE == key && (x.DLT_STS != true)).ToListAsync();

                var FAMILY = access_FAMILY.Select(r => new ClearanceSearchResult
                {
                    Id = r.FAMILY_CODE,
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
                    ControllerName = "Familypass",
                    ServiceNumber = r.SERVICE_NUMBER,
                    PesronalImage = r.PERSONAL_IMAGE,
                    PurposeOfPass = r.PURPOSE_OF_PASS,
                    Name = r.NAME_A,
                    Phone = r.PHONE_NUMBER,
                    Gsm = r.GSM,
                    PermitType = "تصريح العوائل",
                    PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                    CivilNumber = r.CIVIL_NUMBER,
                    Printed = r.ISPRINTED,
                    Delivered = r.ISDELIVERED,
                    Station = r.STATION_CODE.Value,
                    Opened = r.ISOPENED,
                    Deleted = r.DLT_STS,
                    IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                    ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                    CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                    UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                    StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                    Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                    Status = r.STATUS,
                    Rejected = r.REJECTED,
                    ResponsipoleServiceNumber = r.RESPONSIBLE,
                    Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                    WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                    Comments = r.countCommennts(r.FAMILY_CODE),
                    CompanyPermitId = 0,
                    Violations = 0




                }).OrderByDescending(r => r.Id).ToList();
                resulat.AddRange(FAMILY);

            }
            catch
            {

            }
            try
            {
                var access_AUTHORAIZATION = await db.RA42_AUTHORIZATION_PASS_DTL.Where(x => x.BARCODE == key && (x.DLT_STS != true)).ToListAsync();

                var AUTHORAIZATION = access_AUTHORAIZATION.Select(r => new ClearanceSearchResult
                {
                    Id = r.AUTHORAIZATION_CODE,
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
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
                    Deleted = r.DLT_STS,
                    IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                    ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                    CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                    UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                    StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                    Station = r.STATION_CODE.Value,
                    Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                    Status = r.STATUS,
                    Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                    WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                    Comments = r.countCommennts(r.AUTHORAIZATION_CODE),
                    CompanyPermitId = 0,
                    Violations = 0




                }).OrderByDescending(r => r.Id).ToList();
                resulat.AddRange(AUTHORAIZATION);
            }
            catch
            {

            }
            try
            {
                var access_VISITORS = await db.RA42_VISITOR_PASS_DTL.Where(x => x.BARCODE == key && (x.DLT_STS != true)).ToListAsync();

                var VISITORS = access_VISITORS.Select(r => new ClearanceSearchResult
                {
                    Id = r.VISITOR_PASS_CODE,
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
                    ControllerName = "Visitorpass",
                    ServiceNumber = "-",
                    PesronalImage = r.PERSONAL_IMAGE,
                    PurposeOfPass = r.PURPOSE_OF_PASS,
                    Name = r.VISITOR_NAME,
                    Phone = "-",
                    Gsm = r.GSM,
                    PermitType = "تصريح الزوار",
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
                    Violations = 0



                }).OrderByDescending(r => r.Id).ToList();
                resulat.AddRange(VISITORS);
            }
            catch
            {

            }
            try
            {
                var access_TRAINEES = await db.RA42_TRAINEES_PASS_DTL.Where(x => x.BARCODE == key && (x.DLT_STS != true)).ToListAsync();

                var TRAINEES = access_TRAINEES.Select(r => new ClearanceSearchResult
                {
                    Id = r.TRAINEE_PASS_CODE,
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
                    ControllerName = "Traineepass",
                    ServiceNumber = "-",
                    PesronalImage = r.PERSONAL_IMAGE,
                    PurposeOfPass = r.PURPOSE_OF_PASS,
                    Name = r.TRAINEE_NAME,
                    Phone = r.PHONE_NUMBER,
                    Gsm = r.GSM,
                    PermitType = "تصريح المتدربين",
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
                    Comments = r.countCommennts(r.TRAINEE_PASS_CODE),
                    CompanyPermitId = 0,
                    Violations = 0



                }).OrderByDescending(r => r.Id).ToList();
                resulat.AddRange(TRAINEES);
            }
            catch
            {

            }
            try
            {
                var access_TEMP = await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(x => (x.BARCODE == key || x.RA42_COMPANY_PASS_DTL.BARCODE == key) && (x.DLT_STS != true)).ToListAsync();

                var TEMP = access_TEMP.Select(r => new ClearanceSearchResult
                {
                    Id = r.TEMPRORY_COMPANY_PASS_CODE,
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
                    ControllerName = "Temprorycompanypass",
                    ServiceNumber = "-",
                    PesronalImage = r.PERSONAL_IMAGE,
                    PurposeOfPass = r.RA42_COMPANY_PASS_DTL.PURPOSE_OF_PASS,
                    Name = r.NAME_A,
                    Phone = "-",
                    Gsm = r.GSM,
                    PermitType = "تصريح الشركات",
                    PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                    CivilNumber = r.ID_CARD_NUMBER,
                    Printed = r.ISPRINTED,
                    Delivered = r.ISDELIVERED,
                    Rejected = r.RA42_COMPANY_PASS_DTL.REJECTED,
                    Opened = r.RA42_COMPANY_PASS_DTL.ISOPENED,
                    Deleted = r.RA42_PASS_TYPE_MST.DLT_STS,
                    IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                    ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                    CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                    UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                    StationName = r.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.STATION_NAME_A,
                    Station = r.RA42_COMPANY_PASS_DTL.STATION_CODE.Value,
                    Force = r.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.FORCE_ID.Value,
                    Status = r.RA42_COMPANY_PASS_DTL.STATUS,
                    ResponsipoleServiceNumber = r.RA42_COMPANY_PASS_DTL.SERVICE_NUMBER,
                    Workflow = r.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                    WorkflowId = r.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                    Comments = r.RA42_COMPANY_PASS_DTL.countCommennts(r.COMPANY_PASS_CODE.Value),
                    CompanyPermitId = r.COMPANY_PASS_CODE.Value,
                    Violations = 0



                }).OrderByDescending(r => r.Id).ToList();
                resulat.AddRange(TEMP);

            }
            catch
            {

            }
            try
            {
                var access_CONTRACT = await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(x => x.BARCODE == key && (x.DLT_STS != true)).ToListAsync();

                var CONTRACT = access_CONTRACT.Select(r => new ClearanceSearchResult
                {
                    Id = r.CONTRACT_CODE,
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
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
                    Comments = r.countCommennts(r.CONTRACT_CODE),
                    CompanyPermitId = 0,
                    Violations = 0



                }).OrderByDescending(r => r.Id).ToList();
                resulat.AddRange(CONTRACT);


            }
            catch
            {

            }
            try
            {
                var access_Air = await db.RA42_AIR_CREW_PASS_DTL.Where(x => x.BARCODE == key && (x.DLT_STS != true)).ToListAsync();

                var AIR = access_Air.Select(r => new ClearanceSearchResult
                {
                    Id = r.AIR_CREW_PASS_CODE,
                    AccessNumber = r.ACCESS_TYPE_CODE.Value,
                    ControllerName = "AirCrewpass",
                    ServiceNumber = r.SERVICE_NUMBER,
                    PesronalImage = r.PERSONAL_IMAGE,
                    PurposeOfPass = r.PURPOSE_OF_PASS,
                    Name = r.NAME_A,
                    Phone = r.PHONE_NUMBER,
                    Gsm = r.GSM,
                    PermitType = "تصريح أطقم الطيران",
                    PassType = r.RA42_PASS_TYPE_MST.PASS_TYPE,
                    CivilNumber = r.CIVIL_NUMBER,
                    Printed = r.ISPRINTED,
                    Opened = r.ISOPENED,
                    Delivered = r.ISDELIVERED,
                    IssueingDate = (r.DATE_FROM != null ? r.DATE_FROM.ToString() : " "),
                    ExpiredDate = (r.DATE_TO != null ? r.DATE_TO.Value : DateTime.Today),
                    CreatedDate = (r.CRD_DT != null ? r.CRD_DT.Value : DateTime.Today),
                    UpdatedDate = (r.UPD_DT != null ? r.UPD_DT.Value : DateTime.Today),
                    StationName = r.RA42_STATIONS_MST.STATION_NAME_A,
                    Status = r.STATUS,
                    Station = r.STATION_CODE.Value,
                    Force = r.RA42_STATIONS_MST.FORCE_ID.Value,
                    Rejected = r.REJECTED,
                    Deleted = r.DLT_STS,
                    ResponsipoleServiceNumber = r.RESPONSIBLE,
                    Workflow = r.RA42_WORKFLOW_RESPONSIBLE_MST.RA42_WORKFLOW_MST.STEP_NAME + " - " + r.RA42_WORKFLOW_RESPONSIBLE_MST.RESPONSEPLE_NAME,
                    WorkflowId = r.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID.Value,
                    Comments = r.countCommennts(r.AIR_CREW_PASS_CODE),
                    CompanyPermitId = 0,
                    Violations = 0



                }).OrderByDescending(r => r.Id).ToList();
                resulat.AddRange(AIR);

            }
            catch
            {

            }
            if (resulat.Any())
            {

                var user = new UserInfo();
                var currentUser = user.getSNO();
                switch (searchtype)
                {
                    case ClearanceSearchtypeByBarcode.Barcode:
                        {

                            var v = db.RA42_WORKFLOW_RESPONSIBLE_MST.Where(a => a.SERVICE_NUMBER == currentUser &&
                            a.ACTIVE != false && a.DLT_STS != true).FirstOrDefault();
                            if (v != null)
                            {
                                if (v.DEVELOPER == true || v.ADMIN == true)
                                {
                                    if (v.ADMIN == true && v.DEVELOPER != true)
                                    {
                                        resulat = resulat.Where(r => r.Deleted != true).ToList();

                                    }
                                    else
                                    {
                                        resulat = resulat.ToList();
                                    }

                                }
                                else
                                {

                                    resulat = resulat.Where(r => r.Station == v.STATION_CODE.Value).ToList();
                                }
                            }
                            break;
                        }
                }
            }

            return resulat;
        }
    }
}