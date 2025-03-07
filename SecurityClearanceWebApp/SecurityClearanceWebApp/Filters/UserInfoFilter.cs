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
using portal.Controllers;
using SecurityClearanceWebApp.Models;

namespace APP.Filters
{

    public class UserInfoFilter : ActionFilterAttribute
    {
       

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
            db.Database.CommandTimeout = 300;
            string currentUser = (new UserInfo()).getSNO();
            //get user information from RAFO API
            Dictionary<string, string> userInfo = new UserInfo().getUserInfo();
            filterContext.Controller.ViewBag.userInfo = userInfo;
            //filterContext.Controller.ViewBag.state = (userInfo["state"] == "success") ? true : false;
            var lang = filterContext.RouteData.Values["language"] as String;
            if (string.IsNullOrEmpty(lang))
            {
                lang = "ar";
            }

            if (lang != "ar" && lang != "en")
            {
                lang = "ar";
            }
            filterContext.Controller.ViewBag.lang = lang; // ar - arabic, en = english
            filterContext.Controller.ViewBag.UserStatus = true;
            

            var visit = Task.Run(async()=> await db.RA42_VISITOR_MST.Where(a => a.SERVICE_NUMBER == currentUser && a.VISIT_DATE == DateTime.Today).FirstOrDefaultAsync()).Result;

            if (visit == null)
            {
                RA42_VISITOR_MST rA42_VISITOR = new RA42_VISITOR_MST();
                rA42_VISITOR.SERVICE_NUMBER = currentUser;
                rA42_VISITOR.VISIT_DATE = DateTime.Today;
                db.RA42_VISITOR_MST.Add(rA42_VISITOR);
                db.SaveChanges();
            }


        }
 
    }
}