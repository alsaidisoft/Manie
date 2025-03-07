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
	public class CardDesignController : Controller
	{
        //get db connection 
		private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        //identify GeneralFunction class
        private GeneralFunctions general = new GeneralFunctions();
        //set title for the controller from resources
        private string title = Resources.Settings.ResourceManager.GetString("card_design" + "_" + "ar");
        public CardDesignController() {
            ViewBag.Settings = "Settings";

            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "CardDesign";
            //set fontawsome icon for the controller 
			ViewBag.controllerIconClass = "fa fa-credit-card";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Settings.ResourceManager.GetString("card_design" + "_" + "en");
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
            

           

          
                //get force, access, card for 
                if (Language.GetCurrentLang() == "en")
                {
                ViewBag.FORCE_ID = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true), "FORCE_ID", "FORCE_NAME_E");
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST, "ACCESS_TYPE_CODE", "ACCESS_TYPE_E");
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST, "CARD_FOR_CODE", "CARD_FOR_E");

            }
            else
                {
                ViewBag.FORCE_ID = new SelectList(db.RA42_FORCES_MST.Where(a => a.DLT_STS != true), "FORCE_ID", "FORCE_NAME_A");
                ViewBag.ACCESS_TYPE_CODE = new SelectList(db.RA42_ACCESS_TYPE_MST, "ACCESS_TYPE_CODE", "ACCESS_TYPE");
                ViewBag.CARD_FOR_CODE = new SelectList(db.RA42_CARD_FOR_MST, "CARD_FOR_CODE", "CARD_FOR_A");


            }
            ///list unhiding reports 
            return View(db.RA42_CARD_DESIGN_DTL.Where(a => a.DLT_STS != true).OrderByDescending(a => a.CARD_CODE).ToList());
          
            

		}
        //save and edit new data
        public JsonResult SaveDataInDatabase(RA42_CARD_DESIGN_DTL model)
        {

            var result = false;
            try
            {
                //if REPORT_CODE is > 0 that means this record needs update 
                if (model.CARD_CODE > 0)
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
                    RA42_CARD_DESIGN_DTL rA42_CARD_DESIGN_DTL = db.RA42_CARD_DESIGN_DTL.SingleOrDefault(x => x.DLT_STS != true && x.CARD_CODE == model.CARD_CODE);
                    rA42_CARD_DESIGN_DTL.FORCE_ID = model.FORCE_ID;
                    rA42_CARD_DESIGN_DTL.ACCESS_TYPE_CODE = model.ACCESS_TYPE_CODE;
                    rA42_CARD_DESIGN_DTL.CARD_FOR_CODE = model.CARD_FOR_CODE;
                    rA42_CARD_DESIGN_DTL.BASE64_ENCODE_FRONT = model.BASE64_ENCODE_FRONT;
                    rA42_CARD_DESIGN_DTL.BASE64_ENCODE_BACK = model.BASE64_ENCODE_BACK;
                    rA42_CARD_DESIGN_DTL.BASE64_ENCODE_TEMP = model.BASE64_ENCODE_TEMP;
                    rA42_CARD_DESIGN_DTL.REMARKS = model.REMARKS;
                    rA42_CARD_DESIGN_DTL.UPD_BY = currentUser;
                    rA42_CARD_DESIGN_DTL.UPD_DT = DateTime.Now;
                    rA42_CARD_DESIGN_DTL.DLT_STS = false;
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
                    //add new design
                    RA42_CARD_DESIGN_DTL rA42_CARD_DESIGN_DTL = new RA42_CARD_DESIGN_DTL();
                    rA42_CARD_DESIGN_DTL.FORCE_ID = model.FORCE_ID;
                    rA42_CARD_DESIGN_DTL.ACCESS_TYPE_CODE = model.ACCESS_TYPE_CODE;
                    rA42_CARD_DESIGN_DTL.CARD_FOR_CODE = model.CARD_FOR_CODE;
                    rA42_CARD_DESIGN_DTL.BASE64_ENCODE_FRONT = model.BASE64_ENCODE_FRONT;
                    rA42_CARD_DESIGN_DTL.BASE64_ENCODE_BACK = model.BASE64_ENCODE_BACK;
                    rA42_CARD_DESIGN_DTL.BASE64_ENCODE_TEMP = model.BASE64_ENCODE_TEMP;
                    rA42_CARD_DESIGN_DTL.REMARKS = model.REMARKS;
                    rA42_CARD_DESIGN_DTL.UPD_BY = currentUser;
                    rA42_CARD_DESIGN_DTL.UPD_DT = DateTime.Now;
                    rA42_CARD_DESIGN_DTL.CRD_BY = currentUser;
                    rA42_CARD_DESIGN_DTL.CRD_DT = DateTime.Now;
                    rA42_CARD_DESIGN_DTL.DLT_STS = false;
                    db.RA42_CARD_DESIGN_DTL.Add(rA42_CARD_DESIGN_DTL);
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
            var value = (from a in db.RA42_CARD_DESIGN_DTL
                         join b in db.RA42_CARD_FOR_MST on a.CARD_FOR_CODE equals b.CARD_FOR_CODE
                         join c in db.RA42_ACCESS_TYPE_MST on a.ACCESS_TYPE_CODE equals c.ACCESS_TYPE_CODE
                         join d in db.RA42_FORCES_MST on a.FORCE_ID equals d.FORCE_ID
                         where a.CARD_CODE == Id
                         select new
                         {

                             a.CARD_CODE,
                             c.ACCESS_TYPE_CODE,
                             c.ACCESS_TYPE,
                             c.ACCESS_TYPE_E,
                             b.CARD_FOR_CODE,
                             b.CARD_FOR_A,
                             b.CARD_FOR_E,
                             a.BASE64_ENCODE_FRONT,
                             a.BASE64_ENCODE_BACK,
                             a.BASE64_ENCODE_TEMP,
                             a.REMARKS,
                             d.FORCE_ID,
                             d.FORCE_NAME_A,
                             d.FORCE_NAME_E,
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
            RA42_CARD_DESIGN_DTL rA42_CARD_DESIGN_DTL = db.RA42_CARD_DESIGN_DTL.Where(x => x.CARD_CODE == Id).FirstOrDefault();
            if (rA42_CARD_DESIGN_DTL != null)
            {
                rA42_CARD_DESIGN_DTL.UPD_BY = currentUser;
                rA42_CARD_DESIGN_DTL.UPD_DT = DateTime.Now;
                //hide record by set DLT_STS to true 
                rA42_CARD_DESIGN_DTL.DLT_STS = true;
                db.SaveChanges();
                result = true;
                AddToast(new Toast("",
                   GetResourcesValue("success_delete_message"),
                   "green"));
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
        
    }
}
