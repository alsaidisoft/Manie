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

    //this filter is to check if current user has permits 
    
    public class PermitsFilter : ActionFilterAttribute
    {

 
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
            db.Database.CommandTimeout = 300;
            string currentUser = (new UserInfo()).getSNO();

            filterContext.Controller.ViewBag.AUTHO_ACCESS = false;
            filterContext.Controller.ViewBag.SECURITY_ACCESS = false;
            filterContext.Controller.ViewBag.FAMILY_ACCESS = false;
            filterContext.Controller.ViewBag.VECHILE_ACCESS = false;
            filterContext.Controller.ViewBag.TEMP_COMPANY_ACCESS = false;
            filterContext.Controller.ViewBag.CONTRACT_COMPANY_ACCESS = false;
            filterContext.Controller.ViewBag.EVENT_EXERCICE_ACCESS = false;
            filterContext.Controller.ViewBag.VISITOR_ACCESS = false;
            filterContext.Controller.ViewBag.TRAINEE_ACCESS = false;
            filterContext.Controller.ViewBag.AIR_ACCESS = false;
            filterContext.Controller.ViewBag.autho_permit = "";
            filterContext.Controller.ViewBag.autho_icon = "";
            filterContext.Controller.ViewBag.private_permit = "";
            filterContext.Controller.ViewBag.private_icon = "";
            filterContext.Controller.ViewBag.employee_others_permit = "";
            filterContext.Controller.ViewBag.employee_other_icon = "";
            filterContext.Controller.ViewBag.family_permit = "";
            filterContext.Controller.ViewBag.family_icon = "";
            filterContext.Controller.ViewBag.companies_permit = "";
            filterContext.Controller.ViewBag.companies_icon = "";
            filterContext.Controller.ViewBag.contracted_permit = "";
            filterContext.Controller.ViewBag.contracted_icon = "";
            filterContext.Controller.ViewBag.events_permit = "";
            filterContext.Controller.ViewBag.events_icon = "";
            filterContext.Controller.ViewBag.trainee_permit = "";
            filterContext.Controller.ViewBag.trainee_icon = "";
            filterContext.Controller.ViewBag.visitor_permit = "";
            filterContext.Controller.ViewBag.visitor_icon = "";
            filterContext.Controller.ViewBag.air_crew_permit = "";
            filterContext.Controller.ViewBag.air_crew_icon = "";

            filterContext.Controller.ViewBag.autho_remarks = "";
            filterContext.Controller.ViewBag.private_remarks = "";
            filterContext.Controller.ViewBag.employee_other_remarks = "";
            filterContext.Controller.ViewBag.family_remarks = "";
            filterContext.Controller.ViewBag.companies_remarks = "";
            filterContext.Controller.ViewBag.contracted_remarks = "";
            filterContext.Controller.ViewBag.events_remarks = "";
            filterContext.Controller.ViewBag.visitor_remarks = "";
            filterContext.Controller.ViewBag.trainee_remarks = "";
            filterContext.Controller.ViewBag.air_crew_remarks = "";
           

            var passes = Task.Run(async () => await db.RA42_ACCESS_SELECT_MST.Where(a =>
               a.RA42_WORKFLOW_RESPONSIBLE_MST.SERVICE_NUMBER == currentUser
               && a.DLT_STS != true && a.RA42_WORKFLOW_RESPONSIBLE_MST.DLT_STS != true
               && a.RA42_WORKFLOW_RESPONSIBLE_MST.ACTIVE != false).ToListAsync()).Result;

            if (passes != null)
            {
                foreach (var access in passes)
                {
                    if (access.ACCESS_TYPE_CODE == 1)
                    {
                        filterContext.Controller.ViewBag.AUTHO_ACCESS = true;
                        filterContext.Controller.ViewBag.autho_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE;
                        filterContext.Controller.ViewBag.autho_icon = access.RA42_ACCESS_TYPE_MST.ICON;
                        filterContext.Controller.ViewBag.autho_remarks = access.RA42_ACCESS_TYPE_MST.REMARKS;

                        if (Language.GetCurrentLang() == "en")
                        {
                            filterContext.Controller.ViewBag.autho_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE_E;
                            filterContext.Controller.ViewBag.autho_remarks = "";
                        }

                    }
                    if (access.ACCESS_TYPE_CODE == 2)
                    {
                        filterContext.Controller.ViewBag.SECURITY_ACCESS = true;
                        filterContext.Controller.ViewBag.private_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE;
                        filterContext.Controller.ViewBag.private_icon = access.RA42_ACCESS_TYPE_MST.ICON;
                        filterContext.Controller.ViewBag.private_remarks = access.RA42_ACCESS_TYPE_MST.REMARKS;

                        if (Language.GetCurrentLang() == "en")
                        {
                            filterContext.Controller.ViewBag.private_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE_E;
                            filterContext.Controller.ViewBag.private_remarks = "";
                        }
                    }
                    if (access.ACCESS_TYPE_CODE == 3)
                    {
                        filterContext.Controller.ViewBag.VECHILE_ACCESS = true;
                        filterContext.Controller.ViewBag.employee_others_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE;
                        filterContext.Controller.ViewBag.employee_other_icon = access.RA42_ACCESS_TYPE_MST.ICON;
                        filterContext.Controller.ViewBag.employee_other_remarks = access.RA42_ACCESS_TYPE_MST.REMARKS;

                        if (Language.GetCurrentLang() == "en")
                        {
                            filterContext.Controller.ViewBag.employee_others_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE_E;
                            filterContext.Controller.ViewBag.employee_other_remarks = "";
                        }
                    }
                    if (access.ACCESS_TYPE_CODE == 4)
                    {
                        filterContext.Controller.ViewBag.FAMILY_ACCESS = true;
                        filterContext.Controller.ViewBag.family_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE;
                        filterContext.Controller.ViewBag.family_icon = access.RA42_ACCESS_TYPE_MST.ICON;
                        filterContext.Controller.ViewBag.family_remarks = access.RA42_ACCESS_TYPE_MST.REMARKS;

                        if (Language.GetCurrentLang() == "en")
                        {
                            filterContext.Controller.ViewBag.family_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE_E;
                            filterContext.Controller.ViewBag.family_remarks = "";
                        }
                    }
                    if (access.ACCESS_TYPE_CODE == 5)
                    {
                        filterContext.Controller.ViewBag.TEMP_COMPANY_ACCESS = true;
                        filterContext.Controller.ViewBag.companies_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE;
                        filterContext.Controller.ViewBag.companies_icon = access.RA42_ACCESS_TYPE_MST.ICON;
                        filterContext.Controller.ViewBag.companies_remarks = access.RA42_ACCESS_TYPE_MST.REMARKS;

                        if (Language.GetCurrentLang() == "en")
                        {
                            filterContext.Controller.ViewBag.companies_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE_E;
                            filterContext.Controller.ViewBag.companies_remarks = "";
                        }
                    }
                    if (access.ACCESS_TYPE_CODE == 6)
                    {
                        filterContext.Controller.ViewBag.CONTRACT_COMPANY_ACCESS = true;
                        filterContext.Controller.ViewBag.contracted_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE;
                        filterContext.Controller.ViewBag.contracted_icon = access.RA42_ACCESS_TYPE_MST.ICON;
                        filterContext.Controller.ViewBag.contracted_remarks = access.RA42_ACCESS_TYPE_MST.REMARKS;

                        if (Language.GetCurrentLang() == "en")
                        {
                            filterContext.Controller.ViewBag.contracted_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE_E;
                            filterContext.Controller.ViewBag.contracted_remarks = "";
                        }
                    }
                    if (access.ACCESS_TYPE_CODE == 7)
                    {
                        filterContext.Controller.ViewBag.EVENT_EXERCICE_ACCESS = true;
                        filterContext.Controller.ViewBag.events_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE;
                        filterContext.Controller.ViewBag.events_icon = access.RA42_ACCESS_TYPE_MST.ICON;
                        filterContext.Controller.ViewBag.events_remarks = access.RA42_ACCESS_TYPE_MST.REMARKS;

                        if (Language.GetCurrentLang() == "en")
                        {
                            filterContext.Controller.ViewBag.events_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE_E;
                            filterContext.Controller.ViewBag.events_remarks = "";
                        }
                    }
                    if (access.ACCESS_TYPE_CODE == 8)
                    {
                        filterContext.Controller.ViewBag.TRAINEE_ACCESS = true;
                        filterContext.Controller.ViewBag.trainee_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE;
                        filterContext.Controller.ViewBag.trainee_icon = access.RA42_ACCESS_TYPE_MST.ICON;
                        filterContext.Controller.ViewBag.trainee_remarks = access.RA42_ACCESS_TYPE_MST.REMARKS;

                        if (Language.GetCurrentLang() == "en")
                        {
                            filterContext.Controller.ViewBag.trainee_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE_E;
                            filterContext.Controller.ViewBag.trainee_remarks = "";
                        }
                    }
                    if (access.ACCESS_TYPE_CODE == 9)
                    {
                        filterContext.Controller.ViewBag.VISITOR_ACCESS = true;
                        filterContext.Controller.ViewBag.visitor_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE;
                        filterContext.Controller.ViewBag.visitor_icon = access.RA42_ACCESS_TYPE_MST.ICON;
                        filterContext.Controller.ViewBag.visitor_remarks = access.RA42_ACCESS_TYPE_MST.REMARKS;

                        if (Language.GetCurrentLang() == "en")
                        {
                            filterContext.Controller.ViewBag.visitor_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE_E;
                            filterContext.Controller.ViewBag.visitor_remarks = "";
                        }
                    }

                    if (access.ACCESS_TYPE_CODE == 10)
                    {
                        filterContext.Controller.ViewBag.AIR_ACCESS = true;
                        filterContext.Controller.ViewBag.air_crew_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE;
                        filterContext.Controller.ViewBag.air_crew_icon = access.RA42_ACCESS_TYPE_MST.ICON;
                        filterContext.Controller.ViewBag.air_crew_remarks = access.RA42_ACCESS_TYPE_MST.REMARKS;

                        if (Language.GetCurrentLang() == "en")
                        {
                            filterContext.Controller.ViewBag.air_crew_permit = access.RA42_ACCESS_TYPE_MST.ACCESS_TYPE_E;
                            filterContext.Controller.ViewBag.air_crew_remarks = "";

                        }
                    }
                }
            }

        }

    }

 }



    
