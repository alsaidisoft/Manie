using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using APP.Filters;
using APP.Util;
using Microsoft.Ajax.Utilities;
using portal.Controllers;
using SecurityClearanceWebApp.Models;
using SecurityClearanceWebApp.Services;
using SecurityClearanceWebApp.Util;

namespace APP.Filters
{

    //this filter is to check if current user is in the RA42_WORKFLOW_RESPONSIBLE_MST table and has permessions
    
    public class PermissionsFilter : ActionFilterAttribute
    {

 
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
            db.Database.CommandTimeout = 300;
            string currentUser = (new UserInfo()).getSNO();
            //var v =  db.RA42_WORKFLOW_RESPONSIBLE_MST.Where(a => a.SERVICE_NUMBER == currentUser && a.ACTIVE != false && a.DLT_STS != true).FirstOrDefault();
            var v = new List<UserSearchResult>();
            var searchServices = new UserSearchService();
            var task = Task.Run(async() => await searchServices.Search(currentUser.ToUpper()));
            v = task.Result;
            int workflow = 0;
            int STATION_CODE = 0;
            int FORCE_ID = 0;
            var cards = new List<string> {};
            filterContext.Controller.ViewBag.state = false;
            filterContext.Controller.ViewBag.COUNTAUTHO = 0;
            filterContext.Controller.ViewBag.COUNTAUTHOTOPRINT = 0;
            filterContext.Controller.ViewBag.COUNTSECUR = 0;
            filterContext.Controller.ViewBag.COUNTSECURTOPRINT = 0;
            filterContext.Controller.ViewBag.COUNTVECHIL = 0;
            filterContext.Controller.ViewBag.COUNTVECHILTOPRINT = 0;
            filterContext.Controller.ViewBag.COUNTFAMILY = 0;
            filterContext.Controller.ViewBag.COUNTFAMILYTOPRINT = 0;
            filterContext.Controller.ViewBag.COUNTCONTRACT = 0;
            filterContext.Controller.ViewBag.COUNTCONTRACTTOPRINT = 0;
            filterContext.Controller.ViewBag.COUNTTEMPRORY = 0;
            filterContext.Controller.ViewBag.COUNTTEMPRORYTOPRINT = 0;
            filterContext.Controller.ViewBag.COUNTEVENTEXERCISE = 0;
            filterContext.Controller.ViewBag.COUNTEVENTEXERCISETOPRINT = 0;
            filterContext.Controller.ViewBag.COUNTVISITOR = 0;
            filterContext.Controller.ViewBag.COUNTVISITORTOPRINT = 0;
            filterContext.Controller.ViewBag.COUNTVISITOR_RNO = 0;
            filterContext.Controller.ViewBag.COUNTTRAINEE = 0;
            filterContext.Controller.ViewBag.COUNTTRAINEETOPRINT = 0;
            filterContext.Controller.ViewBag.COUNTAIR = 0;
            filterContext.Controller.ViewBag.COUNTAIRTOPRINT = 0;
            filterContext.Controller.ViewBag.workflowIDType = 0;
           
            if (v.Count > 0)
            {


                filterContext.Controller.ViewBag.state = true;
                filterContext.Controller.ViewBag.FULL_NAME = (Language.GetCurrentLang()=="en"?v.FirstOrDefault().RESPONSEPLE_NAME_E: v.FirstOrDefault().RESPONSEPLE_NAME);
                filterContext.Controller.ViewBag.RNK = (Language.GetCurrentLang() == "en" ? v.FirstOrDefault().RANK_E : v.FirstOrDefault().RANK);
                filterContext.Controller.ViewBag.AD = v.FirstOrDefault().AD;
                filterContext.Controller.ViewBag.DLT = v.FirstOrDefault().DL;
                filterContext.Controller.ViewBag.VW = v.FirstOrDefault().VW;
                filterContext.Controller.ViewBag.UP = v.FirstOrDefault().UP;
                filterContext.Controller.ViewBag.RP = v.FirstOrDefault().RP;
                filterContext.Controller.ViewBag.STN = v.FirstOrDefault().SETTINGS;
                filterContext.Controller.ViewBag.DEVELOPER = v.FirstOrDefault().DEVELOPER;
                filterContext.Controller.ViewBag.ADMIN = v.FirstOrDefault().ADMIN;
                filterContext.Controller.ViewBag.STATION_CODE_TYPE = v.FirstOrDefault().STATION_CODE.Value;
                filterContext.Controller.ViewBag.CAMP_NAME = (Language.GetCurrentLang() == "en" ? v.FirstOrDefault().CAMP_NAME_E : v.FirstOrDefault().CAMP_NAME);
                filterContext.Controller.ViewBag.FORCE_TYPE_CODE = v.FirstOrDefault().FORCE_TYPE;
                filterContext.Controller.ViewBag.FORCE_NAME = (Language.GetCurrentLang() == "en" ? v.FirstOrDefault().FORCE_NAME_E : v.FirstOrDefault().FORCE_NAME);
                filterContext.Controller.ViewBag.USER_UNIT = (Language.GetCurrentLang() == "en" ? v.FirstOrDefault().UNIT_NAME_E : v.FirstOrDefault().UNIT_NAME);
                filterContext.Controller.ViewBag.RESPO_ID = v.FirstOrDefault().WORKFLOW_RESPO_CODE;
                filterContext.Controller.ViewBag.WORKFLOW_ID_F = v.FirstOrDefault().WORKFLOWID;

                STATION_CODE = v.FirstOrDefault().STATION_CODE.Value;
                workflow = v.FirstOrDefault().WORKFLOWID.Value;
                FORCE_ID = v.FirstOrDefault().FORCE_TYPE.Value;
                filterContext.Controller.ViewBag.workflowIDType = v.FirstOrDefault().WORKFLOWID.Value;
                DateTime threeYearsAgo = DateTime.Now.AddYears(-3);
                DateTime overThreeMonth = DateTime.Now.AddMonths(-3);
                if (workflow == 2)
                {
                filterContext.Controller.ViewBag.COUNTAUTHO = Task.Run(async () => await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.CRD_DT >=threeYearsAgo).CountAsync()).Result;
                filterContext.Controller.ViewBag.COUNTSECUR = Task.Run(async () => await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                filterContext.Controller.ViewBag.COUNTVECHIL = Task.Run(async () => await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                filterContext.Controller.ViewBag.COUNTFAMILY = Task.Run(async () => await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                filterContext.Controller.ViewBag.COUNTCONTRACT = Task.Run(async () => await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.COMPANY_TYPE_CODE == 1 && a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                filterContext.Controller.ViewBag.COUNTTEMPRORY = Task.Run(async () => await db.RA42_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_TEMPRORY_COMPANY_PASS_DTL.Any(t => t.DLT_STS != true) && a.RA42_TEMPRORY_COMPANY_PASS_DTL.Count() > 0 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                filterContext.Controller.ViewBag.COUNTVISITOR = Task.Run(async () => await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                filterContext.Controller.ViewBag.COUNTTRAINEE = Task.Run(async () => await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                filterContext.Controller.ViewBag.COUNTAIR = Task.Run(async () => await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;

                //adding new notification from the new main permits table
             
                filterContext.Controller.ViewBag.COUNTAUTHO += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE ==1 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
            
                filterContext.Controller.ViewBag.COUNTSECUR += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.STATION_CODE == STATION_CODE && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
              
                filterContext.Controller.ViewBag.COUNTVECHIL += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
             
                filterContext.Controller.ViewBag.COUNTFAMILY += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                
                filterContext.Controller.ViewBag.COUNTCONTRACT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                
                filterContext.Controller.ViewBag.COUNTTEMPRORY += Task.Run(async () => await db.RA42_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5 && a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_PERMITS_DTL.Any(t => t.DLT_STS != true) && a.RA42_PERMITS_DTL.Count() > 0 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
               
                filterContext.Controller.ViewBag.COUNTEVENTEXERCISE += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
              
                filterContext.Controller.ViewBag.COUNTVISITOR += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
              
                filterContext.Controller.ViewBag.COUNTTRAINEE += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
             
                filterContext.Controller.ViewBag.COUNTAIR += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;

                }
                if (workflow == 3)
                    {
                    if (FORCE_ID != 3)
                    {


                        filterContext.Controller.ViewBag.COUNTAUTHO = Task.Run(async () => await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTAUTHOTOPRINT = Task.Run(async () => await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTSECUR = Task.Run(async () => await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTSECURTOPRINT = Task.Run(async () => await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTVECHIL = Task.Run(async () => await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTVECHILTOPRINT = Task.Run(async () => await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTFAMILY = Task.Run(async () => await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTFAMILYTOPRINT = Task.Run(async () => await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTCONTRACT = Task.Run(async () => await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTCONTRACTTOPRINT = Task.Run(async () => await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTTEMPRORY = Task.Run(async () => await db.RA42_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_TEMPRORY_COMPANY_PASS_DTL.Any(t => t.DLT_STS != true) && a.RA42_TEMPRORY_COMPANY_PASS_DTL.Count() > 0 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTTEMPRORYTOPRINT = Task.Run(async () => await db.RA42_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_TEMPRORY_COMPANY_PASS_DTL.Any(t => t.DLT_STS != true) && a.RA42_TEMPRORY_COMPANY_PASS_DTL.Count() > 0 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTVISITOR = Task.Run(async () => await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTVISITORTOPRINT = Task.Run(async () => await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTTRAINEE = Task.Run(async () => await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTTRAINEETOPRINT = Task.Run(async () => await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTAIR = Task.Run(async () => await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTAIRTOPRINT = Task.Run(async () => await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;



                        filterContext.Controller.ViewBag.COUNTAUTHO += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 1 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTAUTHOTOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 1 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTSECUR += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTSECURTOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTVECHIL += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTVECHILTOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTFAMILY += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTFAMILYTOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTCONTRACT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTCONTRACTTOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTTEMPRORY += Task.Run(async () => await db.RA42_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5 && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_PERMITS_DTL.Any(t => t.DLT_STS != true && a.CRD_DT >= threeYearsAgo) && a.RA42_PERMITS_DTL.Count() > 0).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTTEMPRORYTOPRINT += Task.Run(async () => await db.RA42_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5 && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_PERMITS_DTL.Any(t => t.DLT_STS != true) && a.RA42_PERMITS_DTL.Count() > 0 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTEVENTEXERCISE += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTEVENTEXERCISETOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTVISITOR += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTVISITORTOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTTRAINEE += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTTRAINEETOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTAIR += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTAIRTOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                    }
                    else
                    {
                        filterContext.Controller.ViewBag.COUNTAUTHO = Task.Run(async () => await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTAUTHOTOPRINT = Task.Run(async () => await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.UPD_DT >= overThreeMonth).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTSECUR = Task.Run(async () => await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTSECURTOPRINT = Task.Run(async () => await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.UPD_DT >= overThreeMonth).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTVECHIL = Task.Run(async () => await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTVECHILTOPRINT = Task.Run(async () => await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.UPD_DT >= overThreeMonth).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTFAMILY = Task.Run(async () => await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTFAMILYTOPRINT = Task.Run(async () => await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.UPD_DT >= overThreeMonth).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTCONTRACT = Task.Run(async () => await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTCONTRACTTOPRINT = Task.Run(async () => await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.UPD_DT >= overThreeMonth).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTTEMPRORY = Task.Run(async () => await db.RA42_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_TEMPRORY_COMPANY_PASS_DTL.Any(t => t.DLT_STS != true) && a.RA42_TEMPRORY_COMPANY_PASS_DTL.Count() > 0 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTTEMPRORYTOPRINT = Task.Run(async () => await db.RA42_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_TEMPRORY_COMPANY_PASS_DTL.Any(t => t.DLT_STS != true) && a.RA42_TEMPRORY_COMPANY_PASS_DTL.Count() > 0 && a.UPD_DT >= overThreeMonth).CountAsync()).Result;
                        //filterContext.Controller.ViewBag.COUNTTEMPRORYTOPRINT = Task.Run(async () => await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_COMPANY_PASS_DTL.DLT_STS != true && (a.RA42_COMPANY_PASS_DTL.STATUS == true && a.ISPRINTED != true)  && a.RA42_COMPANY_PASS_DTL.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_COMPANY_PASS_DTL.STATION_CODE == STATION_CODE ).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTVISITOR = Task.Run(async () => await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTVISITORTOPRINT = Task.Run(async () => await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.UPD_DT >= overThreeMonth).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTTRAINEE = Task.Run(async () => await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTTRAINEETOPRINT = Task.Run(async () => await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.UPD_DT >= overThreeMonth).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTAIR = Task.Run(async () => await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTAIRTOPRINT = Task.Run(async () => await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.UPD_DT >= overThreeMonth).CountAsync()).Result;



                        filterContext.Controller.ViewBag.COUNTAUTHO += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 1 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTAUTHOTOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 1 && a.UPD_DT >= overThreeMonth).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTSECUR += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTSECURTOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2 && a.UPD_DT >= overThreeMonth).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTVECHIL += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTVECHILTOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3 && a.UPD_DT >= overThreeMonth).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTFAMILY += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTFAMILYTOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4 && a.UPD_DT >= overThreeMonth).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTCONTRACT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTCONTRACTTOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6 && a.UPD_DT >= overThreeMonth).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTTEMPRORY += Task.Run(async () => await db.RA42_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5 && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_PERMITS_DTL.Any(t => t.DLT_STS != true && a.CRD_DT >= threeYearsAgo) && a.RA42_PERMITS_DTL.Count() > 0).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTTEMPRORYTOPRINT += Task.Run(async () => await db.RA42_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5 && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_PERMITS_DTL.Any(t => t.DLT_STS != true) && a.RA42_PERMITS_DTL.Count() > 0 && a.UPD_DT >= overThreeMonth).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTEVENTEXERCISE += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTEVENTEXERCISETOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7 && a.UPD_DT >= overThreeMonth).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTVISITOR += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTVISITORTOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9 && a.UPD_DT >= overThreeMonth).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTTRAINEE += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTTRAINEETOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8 && a.UPD_DT >= overThreeMonth).CountAsync()).Result;

                        filterContext.Controller.ViewBag.COUNTAIR += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                        filterContext.Controller.ViewBag.COUNTAIRTOPRINT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && (a.STATUS == true && a.ISPRINTED != true) && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10 && a.UPD_DT >= overThreeMonth).CountAsync()).Result;

                    }
                }
                if (workflow == 4)
                    {
                    filterContext.Controller.ViewBag.COUNTAUTHO = Task.Run(async () => await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow  && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                    filterContext.Controller.ViewBag.COUNTSECUR = Task.Run(async () => await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                    filterContext.Controller.ViewBag.COUNTVECHIL = Task.Run(async () => await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                    filterContext.Controller.ViewBag.COUNTFAMILY = Task.Run(async () => await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                    filterContext.Controller.ViewBag.COUNTCONTRACT = Task.Run(async () => await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.COMPANY_TYPE_CODE == 1 && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                    filterContext.Controller.ViewBag.COUNTTEMPRORY = Task.Run(async () => await db.RA42_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_TEMPRORY_COMPANY_PASS_DTL.Any(t => t.DLT_STS != true) && a.RA42_TEMPRORY_COMPANY_PASS_DTL.Count() > 0 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                    filterContext.Controller.ViewBag.COUNTVISITOR = Task.Run(async () => await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                    filterContext.Controller.ViewBag.COUNTTRAINEE = Task.Run(async () => await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                    //adding new notification from the new main permits table
                
                    filterContext.Controller.ViewBag.COUNTAUTHO += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 1 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;

                    filterContext.Controller.ViewBag.COUNTSECUR += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                 
                    filterContext.Controller.ViewBag.COUNTVECHIL += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                
                    filterContext.Controller.ViewBag.COUNTFAMILY += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                 
                    filterContext.Controller.ViewBag.COUNTCONTRACT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_WORKFLOW_RESPONSIBLE_MST.STATION_CODE == 22 && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                 
                    filterContext.Controller.ViewBag.COUNTTEMPRORY += Task.Run(async () => await db.RA42_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5 && a.RA42_PERMITS_DTL.Any(t => t.DLT_STS != true) && a.RA42_PERMITS_DTL.Count() > 0 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                  
                    filterContext.Controller.ViewBag.COUNTEVENTEXERCISE += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                
                    filterContext.Controller.ViewBag.COUNTVISITOR += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
              
                    filterContext.Controller.ViewBag.COUNTTRAINEE += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.STATION_CODE == STATION_CODE && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                    

                }
                if (workflow == 5)
                {
                    filterContext.Controller.ViewBag.COUNTAUTHO = Task.Run(async () => await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                  
                    filterContext.Controller.ViewBag.COUNTAUTHO += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 1 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;

                }
                if (workflow == 6)
                {
                    filterContext.Controller.ViewBag.COUNTAUTHO = Task.Run(async () => await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                 
                    filterContext.Controller.ViewBag.COUNTAUTHO += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 1 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;

                }
                if (workflow == 7)
                {
                    filterContext.Controller.ViewBag.COUNTCONTRACT = Task.Run(async () => await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.COMPANY_TYPE_CODE == 1 && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
              
                    filterContext.Controller.ViewBag.COUNTCONTRACT += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;

                }
                if (workflow == 10)
                {
                    filterContext.Controller.ViewBag.COUNTAIR = Task.Run(async () => await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
               
                    filterContext.Controller.ViewBag.COUNTAIR += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10 && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;

                }
                if (workflow == 11)
                {
                    cards.Clear();
                    cards.AddRange(new List<string> { "3", "28" });
                    filterContext.Controller.ViewBag.COUNTVISITOR_RNO = Task.Run(async () => await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow
                    && a.STATION_CODE == STATION_CODE && a.DATE_TO >= DateTime.Today && !cards.Contains(a.CARD_FOR_CODE.ToString()) && a.REJECTED != true
                    && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;
                    cards.Clear();
                    cards.AddRange(new List<string> { "3", "28","33", "34", "39" });
                    filterContext.Controller.ViewBag.COUNTVISITOR_RNO += Task.Run(async () => await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.WORKFLOWID == workflow
                    && a.STATION_CODE == STATION_CODE && a.DATE_TO >= DateTime.Today && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9 && !cards.Contains(a.CARD_FOR_CODE.ToString()) && a.REJECTED != true
                    && a.CRD_DT >= threeYearsAgo).CountAsync()).Result;

                }
                

            }
            else
            {
                filterContext.Controller.ViewBag.FULL_NAME = null;
                filterContext.Controller.ViewBag.RNK = null;
                filterContext.Controller.ViewBag.AD = false;
                filterContext.Controller.ViewBag.DLT = false;
                filterContext.Controller.ViewBag.VW = false;
                filterContext.Controller.ViewBag.UP = false;
                filterContext.Controller.ViewBag.RP = false;
                filterContext.Controller.ViewBag.STN = false;
                filterContext.Controller.ViewBag.DEVELOPER = false;
                filterContext.Controller.ViewBag.ADMIN = false;
                filterContext.Controller.ViewBag.STATION_CODE_TYPE = null;
                filterContext.Controller.ViewBag.CAMP_NAME = null;
                filterContext.Controller.ViewBag.RESPO_ID = null;
                filterContext.Controller.ViewBag.FORCE_TYPE_CODE = null;
                filterContext.Controller.ViewBag.WORKFLOW_ID_F = null;

                // filterContext.Controller.ViewBag.UNITCODE = null;


            }


        }

    }

 }



    
