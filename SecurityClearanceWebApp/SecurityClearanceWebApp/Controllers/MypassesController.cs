using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Results;
using System.Web.Mvc;
using APP.Filters;
using APP.Util;
using System.Data.Entity;
using Microsoft.Ajax.Utilities;
using System.Linq.Dynamic;
using portal.Controllers;
using SecurityClearanceWebApp.Models;
using SecurityClearanceWebApp.Services;
using SecurityClearanceWebApp.Util;


namespace SecurityClearanceWebApp.Controllers
{
    [UserInfoFilter]
    //this mypasses controller for many things, this is for noormal user and authorized user
    //for example, normal user can check his owen permits
    //authorized user can mange recived request of permits
    //also, there are differnt view here such as dashboard, search, select permit type to create  
    public class MypassesController : Controller
    {
        //get db connection 
        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private IList<Toast> toasts = new List<Toast>();
        //get current user service number 
        private string currentUser = (new UserInfo()).getSNO();
        private GeneralFunctions general = new GeneralFunctions();
        //set controller title 
        private string title = Resources.Passes.ResourceManager.GetString("mypasses1" + "_" + "ar");

        public MypassesController()
        {
            // don't edit ViewBag.controllerName 
            ViewBag.controllerName = "Mypasses";
            ViewBag.AddPermit = "";
            //set fontawsome icon for the controller 
            ViewBag.controllerIconClass = "fa fa-universal-access";
            if (Language.GetCurrentLang() == "en")
            {
                title = Resources.Passes.ResourceManager.GetString("mypasses1" + "_" + "en");
            }
            ViewBag.controllerNamePlural = title;
            ViewBag.controllerNameSingular = title;

        }
       

        //this view to view permits that current user own them 
        public async Task<ActionResult> Index(string tab)
        {
            ViewBag.SERVICE_NUMBER = currentUser;

            if (string.IsNullOrWhiteSpace(tab))
            {
                tab = "employee";
            }
            //var result = new List<ClearanceSearchResult>();

            if (!string.IsNullOrWhiteSpace(currentUser))
            {
                var searchServices = new ClearanceSearchServices();
                if (tab.Equals("employee"))
                {
                    ViewBag.ByServiceNumber = await searchServices.Search(currentUser.ToUpper(), ClearanceSearchtype.ServiceNumber);
                }
                else
                {
                    ViewBag.ByResponsible = await searchServices.Search(currentUser.ToUpper(), ClearanceSearchtype.Responsipole);
                }
            }

            //ViewBag.FORCES_LIST = await db.RA42_FORCES_MST.Where(a => a.DLT_STS != true).OrderBy(a => a.FORCE_CODE).ToListAsync();


            return View();
        }
        [HttpGet]
        //get transaction by id
        public JsonResult GetObjectById(int Id)
        {
            var value = (from a in db.RA42_TRANSACTION_DTL
                         where a.TRANSACTION_CODE == Id
                         select new
                         {

                             a.TRANSACTION_CODE,
                             a.REMARKS,
                             a.TRANSACTION_TYPE_CODE,
                             a.RECEIPT,
                             a.CRD_BY,
                             a.CRD_DT,
                             a.UPD_BY,
                             a.UPD_DT


                         }).FirstOrDefault();

            return Json(value, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult Transactions(string id)
        {
            ViewBag.controllerName = "";
            ViewBag.controllerNamePlural = Resources.Common.ResourceManager.GetString("prevuies_transactions" + "_" + ViewBag.lang);
            ViewBag.controllerNameSingular = Resources.Common.ResourceManager.GetString("prevuies_transactions" + "_" + ViewBag.lang);

            string accessNum = id.Split('-').Last();
            string permitNum = id.Split('-').First();

            int access = Convert.ToInt32(accessNum);
            int permit = Convert.ToInt32(permitNum);
            //get force id
            int force = Convert.ToInt32(ViewBag.FORCE_TYPE_CODE);
            var v = db.RA42_TRANSACTION_DTL.Where(a=>a.ACCESS_TYPE_CODE == access && a.ACCESS_ROW_CODE == permit && a.DLT_STS !=true ).ToList();
            if (Language.GetCurrentLang() == "en")
            {
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == force), "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME_E");

            }
            else
            {
                ViewBag.TRANSACTION_TYPE_CODE = new SelectList(db.RA42_TRANSACTION_TYPE_MST.Where(a => a.DLT_STS != true && a.FORCE_ID == force), "TRANSACTION_TYPE_CODE", "TRANSACTION_NAME_A");

            }

            return View(v);
        }
        [HttpPost]
        //to update transaction
        public JsonResult SaveDataInDatabase(RA42_TRANSACTION_DTL model, HttpPostedFileBase RECEIPT)
        {

            var result = false;
            try
            {

                //check if user upload new file image 
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


                            //check extention of the image 
                            if (general.CheckFileType(RECEIPT.FileName))
                            {

                                fileName = "Receipt_" + "345" + "_" + DateTime.Now.ToString("yymmssfff") + extension;
                                // store the file inside ~/Files/Receipt/ folder
                                string path = Path.Combine(Server.MapPath("~/Files/Receipt/"), fileName);
                                model.RECEIPT = fileName;
                                RECEIPT.SaveAs(path);


                            }
                            else
                            {
                                //if the extention not supported show error message 
                                AddToast(new Toast("",
                                GetResourcesValue("error"),
                                "red"));
                                TempData["Erorr"] = " image should be in image format - يجب أن تكون الصورة  ملف (JPG,GIF)";
                                return Json(false, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.GetBaseException();
                    }
                }

                //if the SECURITY_CAVEATES_CODE is > 0 that means this record needs update 
                if (model.TRANSACTION_CODE > 0)
                {
                   
                    //check if record in the database
                    RA42_TRANSACTION_DTL rA42_TRANSACTION = db.RA42_TRANSACTION_DTL.Where(x => x.TRANSACTION_CODE == model.TRANSACTION_CODE).FirstOrDefault();
                    if (model.RECEIPT != null)
                    {
                        //if file image is equal undefined that means user not upload any image, so set it as its
                        if (model.RECEIPT == "undefined")
                        {
                            rA42_TRANSACTION.RECEIPT = rA42_TRANSACTION.RECEIPT;
                        }
                        else
                        {
                            rA42_TRANSACTION.RECEIPT = model.RECEIPT;
                        }
                    }
                    rA42_TRANSACTION.REMARKS = model.REMARKS;
                    rA42_TRANSACTION.TRANSACTION_TYPE_CODE = model.TRANSACTION_TYPE_CODE;
                    rA42_TRANSACTION.UPD_BY = currentUser;
                    rA42_TRANSACTION.UPD_DT = DateTime.Now;
                    rA42_TRANSACTION.DLT_STS = false;
                    db.SaveChanges();
                    result = true;
                    AddToast(new Toast("",
                   GetResourcesValue("updated_successfully"),
                   "green"));
                    return Json(result, JsonRequestBehavior.AllowGet);

                }

            }
            catch (Exception ex)
            {
                AddToast(new Toast("",
                GetResourcesValue("error"),
                "red"));
                throw ex;
            }
            AddToast(new Toast("",
                GetResourcesValue("error"),
                "red"));
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult DeleteTransactin(int id)
        {
            var result = false;
            ViewBag.controllerName = "";
            ViewBag.controllerNamePlural = Resources.Common.ResourceManager.GetString("prevuies_transactions" + "_" + ViewBag.lang);
            ViewBag.controllerNameSingular = Resources.Common.ResourceManager.GetString("prevuies_transactions" + "_" + ViewBag.lang);

            var v = db.RA42_TRANSACTION_DTL.Where(a => a.TRANSACTION_CODE == id).FirstOrDefault();
            if (v != null)
            {
                v.DLT_STS = true;
                v.UPD_BY = currentUser;
                v.UPD_DT = DateTime.Now;
                db.SaveChanges();

                var p = db.RA42_PRINT_MST.Where(a => a.ACCESS_TYPE_CODE == v.ACCESS_TYPE_CODE && a.PASS_ROW_CODE == v.ACCESS_ROW_CODE && a.DLT_STS != true).ToList();
                if (p.Count > 0)
                {
                    foreach(var item in p)
                    {
                        item.UPD_BY = currentUser;
                        item.UPD_DT = DateTime.Now;
                        item.DLT_STS = true;
                        db.SaveChanges();
                    }
                }
                AddToast(new Toast("",
                    GetResourcesValue("deleted_done"),
                   "green"));
                result = true;
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            AddToast(new Toast("",
                    GetResourcesValue("error"),
                   "red"));
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> DeliveredPermits(string search)
        {
            if (ViewBag.workflowIDType == 3 || ViewBag.DEVELOPER == true)
            {
                ViewBag.controllerName = "";
                ViewBag.controllerNamePlural = Resources.Passes.ResourceManager.GetString("delivered_permits" + "_" + ViewBag.lang);
                ViewBag.controllerNameSingular = Resources.Passes.ResourceManager.GetString("delivered_permits" + "_" + ViewBag.lang);
                ViewBag.SearchPage = "SearchDelivered";
                ViewBag.Search = null;

                var result = new List<ClearanceSearchResult>();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchServices = new ClearanceSearchServicesByBarcode();

                    result = await searchServices.Search(search.ToUpper(), ClearanceSearchtypeByBarcode.Barcode);
                    if (search != null)
                    {
                        //add key word to the RA42_SEARCH_MST table as history 
                        RA42_SEARCH_MST sEARCH_MST = new RA42_SEARCH_MST();
                        sEARCH_MST.SEARCH_TEXT = search;
                        sEARCH_MST.SEARCH_BY = currentUser;
                        sEARCH_MST.SEARCH_DATE = DateTime.Now;
                        db.RA42_SEARCH_MST.Add(sEARCH_MST);
                        db.SaveChanges();

                    }
                }

                return View(result);
            }
            else
            {
                return RedirectToAction("NotFound", "Home");
            }
        }

        public async Task<ActionResult> NewSearch(string search)
        {
            if (ViewBag.workflowIDType == 11 || ViewBag.workflowIDType == 2 || ViewBag.workflowIDType == 3 || ViewBag.workflowIDType == 4 || ViewBag.workflowIDType == 8 || ViewBag.ADMIN == true || ViewBag.DEVELOPER == true)
            {
                ViewBag.controllerName = "";
                ViewBag.controllerNamePlural = Resources.Common.ResourceManager.GetString("Search" + "_" + ViewBag.lang);
                ViewBag.controllerNameSingular = Resources.Common.ResourceManager.GetString("Search" + "_" + ViewBag.lang);
                ViewBag.SearchPage = "Search";
                ViewBag.Search = null;

                var result = new List<ClearanceSearchResult>();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchServices = new ClearanceSearchServices();

                    var searchResults = await searchServices.Search(search.ToUpper(), ClearanceSearchtype.SearchBox);
                    result = searchResults.Take(10).ToList();
                    if (search != null)
                    {
                        //add key word to the RA42_SEARCH_MST table as history 
                        RA42_SEARCH_MST sEARCH_MST = new RA42_SEARCH_MST();
                        sEARCH_MST.SEARCH_TEXT = search;
                        sEARCH_MST.SEARCH_BY = currentUser;
                        sEARCH_MST.SEARCH_DATE = DateTime.Now;
                        db.RA42_SEARCH_MST.Add(sEARCH_MST);
                        db.SaveChanges();

                    }
                }

                return View(result);
            }
            else
            {
                return RedirectToAction("NotFound", "Home");
            }
        }

        [HttpGet]
        public async Task<JsonResult> SearchResults(string search)
        {
            var result = new List<ClearanceSearchResult>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchServices = new ClearanceSearchServices();

                result = await searchServices.Search(search.ToUpper(), ClearanceSearchtype.SearchBox);
                //result = searchResults.Take(10).ToList();
            }

            // Return lightweight JSON data
            return Json(result.Select(r => new
            {
                r.PermitType, // Suggestion value
                r.StationName,
                r.PersonalInfo,
                r.PesronalImage,
                r.PurposeOfPass,
                r.Delivered,
                r.ExpiredDate,
                r.Status,
                r.Id,
                r.IssueingDate,
                r.Name,
                r.Returned,
                r.Printed

                
            }), JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> Violations()
        {
            if (ViewBag.workflowIDType == 9 || ViewBag.workflowIDType == 3 || ViewBag.DEVELOPER == true || ViewBag.ADMIN == true)
            {
                ViewBag.controllerName = "";
                ViewBag.controllerNamePlural = Resources.Passes.ResourceManager.GetString("Violations" + "_" + ViewBag.lang);
                ViewBag.controllerNameSingular = Resources.Passes.ResourceManager.GetString("Violations" + "_" + ViewBag.lang);
                ViewBag.ViolationPage = "Violations";
                //get all unhideing colors
                var rA42_VECHILE_VIOLATION_MST = db.RA42_VECHILE_VIOLATION_DTL;

                //get current user STATION_CODE from viewbag.STATION_CODE_TYPE which is identified in PermessionFilter class in Filters folder
                int station = Convert.ToInt32(ViewBag.STATION_CODE_TYPE);
                //get force id
                int force = Convert.ToInt32(ViewBag.FORCE_TYPE_CODE);

                var v1 = (from a in db.RA42_VECHILE_VIOLATION_DTL
                         join b in db.RA42_VECHILE_PASS_DTL on a.ACCESS_ROW_CODE equals b.VECHILE_PASS_CODE
                         //join c in db.RA42_VISITOR_PASS_DTL on a.ACCESS_ROW_CODE equals c.VISITOR_PASS_CODE
                         where a.DLT_STS != true && b.STATION_CODE == station && a.ACCESS_TYPE_CODE == 3 //&& c.STATION_CODE == station
                         select new CustomJsonResult
                         {
                             ACCESS_TYPE_CODE = a.ACCESS_TYPE_CODE.Value,
                             ACCESS_TYPE = a.RA42_ACCESS_TYPE_MST.ACCESS_TYPE,
                             VECHILE_VIOLATION_CODE = a.VECHILE_VIOLATION_CODE,
                             VIOLATION_TYPE = a.RA42_VIOLATIONS_MST.VIOLATION_TYPE_A,
                             VIOLATION_BY = a.VIOLATION_BY,
                             VIOLATION_PRICE = a.VIOLATION_PRICE.ToString(),
                             VIOLATION_DATE = a.VIOLATION_DATE.Value,
                             VIOLATION_DESC = a.VIOLATION_DESC,
                             PREVENT = a.PREVENT.Value,
                             ACCESS_ROW_CODE = a.ACCESS_ROW_CODE.Value,
                             CONTROLLER = "Vechilepass"
                         }).OrderByDescending(a => a.VECHILE_VIOLATION_CODE).ToList();

                var v2 = (from a in db.RA42_VECHILE_VIOLATION_DTL
                         //join b in db.RA42_VECHILE_PASS_DTL on a.ACCESS_ROW_CODE equals b.VECHILE_PASS_CODE
                         join c in db.RA42_VISITOR_PASS_DTL on a.ACCESS_ROW_CODE equals c.VISITOR_PASS_CODE
                         where a.DLT_STS != true && c.STATION_CODE == station && a.ACCESS_TYPE_CODE == 9 //&& c.STATION_CODE == station
                          select new CustomJsonResult
                         {
                             ACCESS_TYPE_CODE = a.ACCESS_TYPE_CODE.Value,
                             ACCESS_TYPE = a.RA42_ACCESS_TYPE_MST.ACCESS_TYPE,
                             VECHILE_VIOLATION_CODE = a.VECHILE_VIOLATION_CODE,
                             VIOLATION_TYPE = a.RA42_VIOLATIONS_MST.VIOLATION_TYPE_A,
                             VIOLATION_BY = a.VIOLATION_BY,
                             VIOLATION_PRICE = a.VIOLATION_PRICE.ToString(),
                             VIOLATION_DATE = a.VIOLATION_DATE.Value,
                             VIOLATION_DESC = a.VIOLATION_DESC,
                             PREVENT = a.PREVENT.Value,
                             ACCESS_ROW_CODE = a.ACCESS_ROW_CODE.Value,
                              CONTROLLER = "Visitorpass"

                          }).OrderByDescending(a => a.VECHILE_VIOLATION_CODE).ToList();
                var v3 = (from a in db.RA42_VECHILE_VIOLATION_DTL
                              //join b in db.RA42_VECHILE_PASS_DTL on a.ACCESS_ROW_CODE equals b.VECHILE_PASS_CODE
                          join c in db.RA42_PERMITS_DTL on a.ACCESS_ROW_CODE equals c.PERMIT_CODE
                          where a.DLT_STS != true && c.STATION_CODE == station && a.CRD_DT > new DateTime(2024, 10, 1) //&& c.STATION_CODE == station
                          select new CustomJsonResult
                          {
                              ACCESS_TYPE_CODE = a.ACCESS_TYPE_CODE.Value,
                              ACCESS_TYPE = a.RA42_ACCESS_TYPE_MST.ACCESS_TYPE,
                              VECHILE_VIOLATION_CODE = a.VECHILE_VIOLATION_CODE,
                              VIOLATION_TYPE = a.RA42_VIOLATIONS_MST.VIOLATION_TYPE_A,
                              VIOLATION_BY = a.VIOLATION_BY,
                              VIOLATION_PRICE = a.VIOLATION_PRICE.ToString(),
                              VIOLATION_DATE = a.VIOLATION_DATE.Value,
                              VIOLATION_DESC = a.VIOLATION_DESC,
                              PREVENT = a.PREVENT.Value,
                              ACCESS_ROW_CODE = a.ACCESS_ROW_CODE.Value,
                              CONTROLLER = "Permitsdtl"
                          }).OrderByDescending(a => a.VECHILE_VIOLATION_CODE).ToList();
                var combination = new[] { v1, v2,v3 }.SelectMany(x => x);
                return View(combination);

            }
            else
            {
                return RedirectToAction("NotFound", "Home");
            }
          
        }

        public async Task<ActionResult> SearchViolations(string plate_number, string char_num, string civil_number)
        {
            if (ViewBag.workflowIDType == 3 || ViewBag.workflowIDType == 9 || ViewBag.DEVELOPER == true || ViewBag.ADMIN == true)
            {
                ViewBag.controllerName = "";
                ViewBag.controllerNamePlural = Resources.Passes.ResourceManager.GetString("Violations" + "_" + ViewBag.lang);
                ViewBag.controllerNameSingular = Resources.Passes.ResourceManager.GetString("Violations" + "_" + ViewBag.lang);
                ViewBag.ViolationPage = "Violations";
                ViewBag.Search = null;
                //get plate char types in english 
                ViewBag.PLATE_CHAR_CODE = new SelectList(db.RA42_PLATE_CHAR_MST.Where(a => a.DLT_STS != true), "PLATE_CHAR_CODE", "PLATE_CHAR");

                var result = new List<ClearanceSearchResult>();

                if (!string.IsNullOrWhiteSpace(plate_number) && !string.IsNullOrWhiteSpace(char_num))
                {
                    int c_num = Convert.ToInt32(char_num.ToString());
                    var searchCarServices = new ClearanceSearchByCarNumber();

                    result = await searchCarServices.Search(plate_number,c_num, ClearanceSearchtypeByCar.plate);
                    
                }

                if (!string.IsNullOrWhiteSpace(civil_number))
                {
                    var searchServices = new ClearanceSearchServices();

                    result = await searchServices.Search(civil_number, ClearanceSearchtype.searchViolations);
                }

                return View(result);
            }
            else
            {
                return RedirectToAction("NotFound", "Home");
            }
        }

       
        //this view for authorized users who will manage different types of permits 
        public async Task<ActionResult> Managepasses()
        {
            if((ViewBag.WORKFLOW_ID_F > 1 && ViewBag.WORKFLOW_ID_F < 8) || ViewBag.ADMIN == true || ViewBag.DEVELOPER == true || ViewBag.WORKFLOW_ID_F == 10)
            {
                ViewBag.Service_No = currentUser;

                ViewBag.controllerNamePlural = Resources.Passes.ResourceManager.GetString("Manage_passes" + "_" + ViewBag.lang);
                ViewBag.controllerNameSingular = Resources.Passes.ResourceManager.GetString("Manage_passes" + "_" + ViewBag.lang);

                ViewBag.controllerName = "";
                ViewBag.Managepasses = "Managepasses";

                return View();
            }
            else
            {
                return RedirectToAction("NotFound", "Home");

            }
        }
        [HttpGet]
        public ActionResult OnlineUsers()
        {
            ViewBag.controllerName = "";
            ViewBag.Dashboard = "Dashboard";
            var url = Url.RequestContext.RouteData.Values["id"].ToString();
            var id = int.Parse(url.ToString());
            var total = db.RA42_VISITOR_MST.OrderByDescending(a=>a.VISITOR_CODE).Take(id).ToList();
            //this code from global class 
            return View(total);
        }
        //this is dashboard view for administrator, developers and authorized users such as manager of security guard & defense directorate 
        //[HttpGet]


        [HttpGet]
        public async Task<ActionResult> Dashboard(int? id)
        {
            if (ViewBag.DEVELOPER == true || ViewBag.ADMIN == true)
            {
                
                if (ViewBag.DEVELOPER == true)
                {
                    ViewBag.STATIONS =  db.RA42_STATIONS_MST.Where(a => a.DLT_STS != true).ToList();
                
                }
                //get specific station for normal user 
                if (ViewBag.ADMIN == true)
                {
                    int unit = Convert.ToInt32(ViewBag.FORCE_TYPE_CODE);
                    ViewBag.STATIONS = db.RA42_STATIONS_MST.Where(a => a.FORCE_ID == unit && a.DLT_STS != true);
                   
                }

                //var url = Url.RequestContext.RouteData.Values["id"].ToString();
                //var id = int.Parse(url.ToString());
                var station = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == id).FirstOrDefault();
                ViewBag.STATION_CAMP_NAME = station.STATION_NAME_A;
                if (Language.GetCurrentLang() == "en")
                {
                    ViewBag.STATION_CAMP_NAME = station.STATION_NAME_E;

                }


                ViewBag.Service_No = currentUser;

                ViewBag.controllerNamePlural = Resources.Passes.ResourceManager.GetString("dashboard" + "_" + ViewBag.lang);
                ViewBag.controllerNameSingular = Resources.Passes.ResourceManager.GetString("dashboard" + "_" + ViewBag.lang);

                ViewBag.controllerName = "";
                ViewBag.Dashboard = "Dashboard";

                //online visitors
                //this code from global class 
                //ViewBag.VISITORS_ONLINE = HttpContext.Application["OnlineUsers"].ToString();
                //total size of files in directory
                //DirectoryInfo info_d = new DirectoryInfo(@"//mamrafohisnapp01/c$/inetpub/wwwroot/Hisn/Files/Documents");
                //DirectoryInfo info_p = new DirectoryInfo(@"//mamrafohisnapp01/c$/inetpub/wwwroot/Hisn/Files/Profiles");
                //long totalSize_d = info_d.EnumerateFiles().Sum(file => file.Length);
                //long totalSize_p = info_p.EnumerateFiles().Sum(file => file.Length);

                //ViewBag.TotalFileLength_d =  SizeSuffix(totalSize_d);
                //ViewBag.TotalFileLength_p =  SizeSuffix(totalSize_p);

                ViewBag.TotalFileLength_d = 0;
                ViewBag.TotalFileLength_p = 0;
                //احصاءيات عامة
                //total comments
                ViewBag.COMMENTS_COUNT = await db.RA42_COMMENTS_MST.Where(a => a.DLT_STS != true).CountAsync();
                //total authorized users in the system 
                ViewBag.WORKRESP_COUNT = await db.RA42_WORKFLOW_RESPONSIBLE_MST.Where(a => a.DLT_STS != true).CountAsync();
                //total zones and gates 
                ViewBag.ZONES_COUNT = await db.RA42_ZONE_AREA_MST.Where(a => a.DLT_STS != true).CountAsync();
                //total companies types 
                ViewBag.COMPANY_COUNT = await db.RA42_COMPANY_MST.Where(a => a.DLT_STS != true).CountAsync();
                //total car catigories
                ViewBag.CAR_CAT_COUNT = await db.RA42_VECHILE_CATIGORY_MST.Where(a => a.DLT_STS != true).CountAsync();
                //total identities 
                ViewBag.IDENTITY_COUNT = await db.RA42_IDENTITY_MST.Where(a => a.DLT_STS != true).CountAsync();
                //total events and exersises 
                ViewBag.EVEX_COUNT = await db.RA42_EVENT_EXERCISE_MST.CountAsync();
                //get total documents 
                ViewBag.FILES_COUNT = await db.RA42_FILES_MST.Where(a => a.DLT_STS != true).CountAsync();
                //get total reports 
                ViewBag.REPORTS_COUNT = await db.RA42_REPORTS_MST.Where(a => a.DLT_STS != true).CountAsync();
                //get total search 
                ViewBag.SEARCH_COUNT = await db.RA42_SEARCH_MST.CountAsync();
                //total print
                //ViewBag.PRINT_COUNT = db.RA42_PRINT_MST.Where(a=>a.DLT_STS !=true).Count();
                //get total visitors 
                ViewBag.VISITOR_COUNT = await db.RA42_VISITOR_MST.CountAsync();
                //get total violation 
                ViewBag.VIOLATION_COUNT = await db.RA42_VECHILE_VIOLATION_DTL.Where(a => a.DLT_STS != true).CountAsync();
                //get total announncments
                ViewBag.ANNOUNCMENT_COUNT = await db.RA42_ANNOUNCEMENT_MST.Where(a => a.DLT_STS != true).CountAsync();

                //احصائيات بأعداد التصاريح في جميع قواعد سلاح الجو
                ViewBag.AUTHO_COUNT_P_IN_RAFO = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 1).CountAsync();
                ViewBag.SEC_COUNT_P_IN_RAFO = await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2").CountAsync()
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2).CountAsync();
                ViewBag.VEC_COUNT_P_IN_RAFO = await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2").CountAsync()
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3).CountAsync();
                ViewBag.FAM_COUNT_P_IN_RAFO = await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2").CountAsync()
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4).CountAsync();
                ViewBag.TCOM_COUNT_P_IN_RAFO = await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2").CountAsync()
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5).CountAsync();
                ViewBag.CCOM_COUNT_P_IN_RAFO = await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2").CountAsync()
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6).CountAsync();
                ViewBag.EXEV_COUNT_P_IN_RAFO = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7).CountAsync();
                ViewBag.VISI_COUNT_P_IN_RAFO = await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2").CountAsync()
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9).CountAsync();
                ViewBag.TRAIN_COUNT_P_IN_RAFO = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2").CountAsync()
                    + await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8).CountAsync();
                ViewBag.AIR_COUNT_P_IN_RAFO = await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2").CountAsync()
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "2" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10).CountAsync();

                ViewBag.TOTAL_PERMITS_IN_RAFO = ViewBag.AUTHO_COUNT_P_IN_RAFO + ViewBag.SEC_COUNT_P_IN_RAFO + ViewBag.VEC_COUNT_P_IN_RAFO
                    + ViewBag.FAM_COUNT_P_IN_RAFO + ViewBag.TCOM_COUNT_P_IN_RAFO +
                    ViewBag.CCOM_COUNT_P_IN_RAFO + ViewBag.EXEV_COUNT_P_IN_RAFO + ViewBag.VISI_COUNT_P_IN_RAFO + ViewBag.TRAIN_COUNT_P_IN_RAFO + ViewBag.AIR_COUNT_P_IN_RAFO;

                //احصائيات بأعداد التصاريح في جميع معسكرات الجيش
                ViewBag.AUTHO_COUNT_P_IN_RAO = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "1").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "1" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 1).CountAsync();
                ViewBag.SEC_COUNT_P_IN_RAO = await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "1").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "1" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2).CountAsync();
                ViewBag.VEC_COUNT_P_IN_RAO = await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "1").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "1" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3).CountAsync();
                ViewBag.FAM_COUNT_P_IN_RAO = await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "1").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "1" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4).CountAsync();
                ViewBag.TCOM_COUNT_P_IN_RAO = await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "1").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "1" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5).CountAsync();
                ViewBag.EXEV_COUNT_P_IN_RAO = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "1" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7).CountAsync();
                ViewBag.VISI_COUNT_P_IN_RAO = await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "1").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "1" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE ==9).CountAsync();
                ViewBag.TRAIN_COUNT_P_IN_RAO = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "1").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "1" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE ==8).CountAsync();

                ViewBag.TOTAL_PERMITS_IN_RAO = ViewBag.AUTHO_COUNT_P_IN_RAO + ViewBag.SEC_COUNT_P_IN_RAO + ViewBag.VEC_COUNT_P_IN_RAO
                    + ViewBag.FAM_COUNT_P_IN_RAO + ViewBag.TCOM_COUNT_P_IN_RAO +
                     ViewBag.EXEV_COUNT_P_IN_RAO + ViewBag.VISI_COUNT_P_IN_RAO + ViewBag.TRAIN_COUNT_P_IN_RAO;

                //احصائيات بأعداد التصاريح في جميع قواعد البحرية 
                ViewBag.AUTHO_COUNT_P_IN_RANO = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE==1).CountAsync();
                ViewBag.SEC_COUNT_P_IN_RANO = await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE==2).CountAsync();
                ViewBag.VEC_COUNT_P_IN_RANO = await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3).CountAsync();
                ViewBag.FAM_COUNT_P_IN_RANO = await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE==4).CountAsync() ;
                ViewBag.TCOM_COUNT_P_IN_RANO = await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE ==5).CountAsync();
                ViewBag.EXEV_COUNT_P_IN_RANO = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7).CountAsync();
                ViewBag.VISI_COUNT_P_IN_RANO = await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE ==9).CountAsync();
                ViewBag.TRAIN_COUNT_P_IN_RANO = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "3" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8).CountAsync();

                ViewBag.TOTAL_PERMITS_IN_RANO = ViewBag.AUTHO_COUNT_P_IN_RANO + ViewBag.SEC_COUNT_P_IN_RANO + ViewBag.VEC_COUNT_P_IN_RANO
                    + ViewBag.FAM_COUNT_P_IN_RANO + ViewBag.TCOM_COUNT_P_IN_RANO +
                     ViewBag.EXEV_COUNT_P_IN_RANO + ViewBag.VISI_COUNT_P_IN_RANO + ViewBag.TRAIN_COUNT_P_IN_RANO;

                //احصائيات بأعداد التصاريح في جميع معسكرات الخدمات الهندسية 
                ViewBag.AUTHO_COUNT_P_IN_MODES = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "4").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "4" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE ==1).CountAsync();
                ViewBag.SEC_COUNT_P_IN_MODES = await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "4").CountAsync()
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "4" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE==2).CountAsync();
                ViewBag.VEC_COUNT_P_IN_MODES = await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "4").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "4" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE==3).CountAsync();
                ViewBag.FAM_COUNT_P_IN_MODES = await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "4").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "4" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE==4).CountAsync();
                ViewBag.TCOM_COUNT_P_IN_MODES = await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "4").CountAsync()
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "4" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE ==5).CountAsync();
                ViewBag.EXEV_COUNT_P_IN_MODES = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "4" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7).CountAsync();
                ViewBag.VISI_COUNT_P_IN_MODES = await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "4").CountAsync() 
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "4" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9).CountAsync();
                ViewBag.TRAIN_COUNT_P_IN_MODES = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "4").CountAsync()
                    + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "4" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE ==8).CountAsync();

                ViewBag.TOTAL_PERMITS_IN_MODES = ViewBag.AUTHO_COUNT_P_IN_MODES + ViewBag.SEC_COUNT_P_IN_MODES + ViewBag.VEC_COUNT_P_IN_MODES
                    + ViewBag.FAM_COUNT_P_IN_MODES + ViewBag.TCOM_COUNT_P_IN_MODES +
                    ViewBag.EXEV_COUNT_P_IN_MODES + ViewBag.VISI_COUNT_P_IN_MODES + ViewBag.TRAIN_COUNT_P_IN_MODES;

                //احصائيات بأعداد التصاريح في الكلية العسكرية التقنية 
                ViewBag.AUTHO_COUNT_P_IN_MTC = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "5").CountAsync()+ await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "5" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE==1).CountAsync();
                ViewBag.SEC_COUNT_P_IN_MTC = await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "5").CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "5" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE==2).CountAsync();
                ViewBag.VEC_COUNT_P_IN_MTC = await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "5").CountAsync()+ await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "5" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3).CountAsync(); 
                ViewBag.FAM_COUNT_P_IN_MTC = await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "5").CountAsync()+ await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "5" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4).CountAsync(); 
                ViewBag.TCOM_COUNT_P_IN_MTC = await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_COMPANY_PASS_DTL.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "5").CountAsync()+ await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "5" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5).CountAsync(); 
                ViewBag.EXEV_COUNT_P_IN_MTC = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "5" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7).CountAsync();
                ViewBag.VISI_COUNT_P_IN_MTC = await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "5").CountAsync()+ await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "5" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9).CountAsync(); 
                ViewBag.TRAIN_COUNT_P_IN_MTC = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "5").CountAsync()+ await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_CODE == "5" && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8).CountAsync(); 

                ViewBag.TOTAL_PERMITS_IN_MTC = ViewBag.AUTHO_COUNT_P_IN_MTC + ViewBag.SEC_COUNT_P_IN_MTC + ViewBag.VEC_COUNT_P_IN_MTC
                    + ViewBag.FAM_COUNT_P_IN_MTC + ViewBag.TCOM_COUNT_P_IN_MTC +
                    ViewBag.EXEV_COUNT_P_IN_MTC + ViewBag.VISI_COUNT_P_IN_MTC + ViewBag.TRAIN_COUNT_P_IN_MTC;


                //احصائيات بأعداد التصاريح في جميع الأسلحة
                ViewBag.AUTHO_COUNT_P = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true).CountAsync()+ await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 1).CountAsync();
                ViewBag.SEC_COUNT_P = await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2).CountAsync();
                ViewBag.VEC_COUNT_P = await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true).CountAsync()+ await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3).CountAsync();
                ViewBag.FAM_COUNT_P = await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4).CountAsync();
                ViewBag.TCOM_COUNT_P = await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5).CountAsync();
                ViewBag.CCOM_COUNT_P = await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6).CountAsync();
                ViewBag.EXEV_COUNT_P = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.ACCESS_TYPE_CODE == 7).CountAsync();
                ViewBag.VISI_COUNT_P = await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9).CountAsync();
                ViewBag.TRAIN_COUNT_P = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8).CountAsync();
                ViewBag.AIR_COUNT_P = await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10).CountAsync();

                ViewBag.TOTAL_PERMITS = ViewBag.AUTHO_COUNT_P + ViewBag.SEC_COUNT_P + ViewBag.VEC_COUNT_P + ViewBag.FAM_COUNT_P + ViewBag.TCOM_COUNT_P +
                    ViewBag.CCOM_COUNT_P + ViewBag.EXEV_COUNT_P + ViewBag.VISI_COUNT_P + ViewBag.TRAIN_COUNT_P + ViewBag.AIR_COUNT_P;

                //احصائيات بأعداد التصاريح قيد المتابعة
                ViewBag.AUTHO_PROG_P = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 1).CountAsync();
                ViewBag.SEC_PROG_P = await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2).CountAsync(); 
                ViewBag.VEC_PROG_P = await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3).CountAsync(); 
                ViewBag.FAM_PROG_P = await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4).CountAsync(); 
                ViewBag.TCOM_PROG_P = await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_COMPANY_PASS_DTL.STATUS != true && a.RA42_COMPANY_PASS_DTL.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5).CountAsync(); 
                ViewBag.CCOM_PROG_P = await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6).CountAsync(); 
                ViewBag.EXEV_PROG_P = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7).CountAsync();
                ViewBag.VISI_PROG_P = await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9).CountAsync();
                ViewBag.TRAIN_PROG_P = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8).CountAsync();
                ViewBag.AIR_PROG_P = await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10).CountAsync();

                ViewBag.PROGRESS_TOTAL = ViewBag.AUTHO_PROG_P + ViewBag.SEC_PROG_P + ViewBag.VEC_PROG_P + ViewBag.FAM_PROG_P + ViewBag.TCOM_PROG_P
                   + ViewBag.CCOM_PROG_P + ViewBag.EXEV_PROG_P + ViewBag.VISI_PROG_P + ViewBag.TRAIN_PROG_P + ViewBag.AIR_PROG_P;
                //احصائيات بأعداد التصاريح المرفوضة
                ViewBag.AUTHO_PROG_R = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true).CountAsync()+ await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE==1).CountAsync();
                ViewBag.SEC_PROG_R = await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2).CountAsync();
                ViewBag.VEC_PROG_R = await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3).CountAsync();
                ViewBag.FAM_PROG_R = await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4).CountAsync();
                ViewBag.TCOM_PROG_R = await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_COMPANY_PASS_DTL.REJECTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5).CountAsync();
                ViewBag.CCOM_PROG_R = await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6).CountAsync();
                ViewBag.EXEV_PROG_R = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7).CountAsync();
                ViewBag.VISI_PROG_R = await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9).CountAsync();
                ViewBag.TRAIN_PROG_R = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8).CountAsync();
                ViewBag.AIR_PROG_R = await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10).CountAsync();

                ViewBag.REJECTED_TOTAL = ViewBag.AUTHO_PROG_R + ViewBag.SEC_PROG_R + ViewBag.VEC_PROG_R + ViewBag.FAM_PROG_R + ViewBag.TCOM_PROG_R
                    + ViewBag.CCOM_PROG_R + ViewBag.EXEV_PROG_R + ViewBag.VISI_PROG_R + ViewBag.TRAIN_PROG_R + ViewBag.AIR_PROG_R;

                //احصائيات بأعداد التصاريح المكتملة
                ViewBag.AUTHO_PROG_C = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true).CountAsync()+ await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE==1).CountAsync();
                ViewBag.SEC_PROG_C = await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2).CountAsync();
                ViewBag.VEC_PROG_C = await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3).CountAsync();
                ViewBag.FAM_PROG_C = await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4).CountAsync();
                ViewBag.TCOM_PROG_C = await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_COMPANY_PASS_DTL.STATUS == true && a.ISPRINTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5).CountAsync();
                ViewBag.CCOM_PROG_C = await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6).CountAsync();
                ViewBag.EXEV_PROG_C = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7).CountAsync();
                ViewBag.VISI_PROG_C = await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9).CountAsync();
                ViewBag.TRAIN_PROG_C = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8).CountAsync();
                ViewBag.AIR_PROG_C = await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10).CountAsync();


                ViewBag.COMPLETE_TOTAL = ViewBag.AUTHO_PROG_C + ViewBag.SEC_PROG_C + ViewBag.VEC_PROG_C + ViewBag.FAM_PROG_C + ViewBag.TCOM_PROG_C
                    + ViewBag.CCOM_PROG_C + ViewBag.EXEV_PROG_C + ViewBag.VISI_PROG_C + ViewBag.TRAIN_PROG_C + ViewBag.AIR_PROG_C;
                //احصائيات بأعداد التصاريح المطبوعة 
                ViewBag.AUTHO_PROG_PR = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE==1).CountAsync();
                ViewBag.SEC_PROG_PR = await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2).CountAsync(); 
                ViewBag.VEC_PROG_PR = await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true).CountAsync()+ await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3).CountAsync();
                ViewBag.FAM_PROG_PR = await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true).CountAsync()+ await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4).CountAsync();
                ViewBag.TCOM_PROG_PR = await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_COMPANY_PASS_DTL.STATUS == true && a.ISPRINTED == true).CountAsync()+ await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5).CountAsync();
                ViewBag.CCOM_PROG_PR = await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6).CountAsync();
                ViewBag.EXEV_PROG_PR = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7).CountAsync();
                ViewBag.VISI_PROG_PR = await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9).CountAsync();
                ViewBag.TRAIN_PROG_PR = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8).CountAsync();
                ViewBag.AIR_PROG_PR = await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10).CountAsync();

                ViewBag.PRINTED_TOTAL = ViewBag.AUTHO_PROG_PR + ViewBag.SEC_PROG_PR + ViewBag.VEC_PROG_PR + ViewBag.FAM_PROG_PR + ViewBag.TCOM_PROG_PR
                   + ViewBag.CCOM_PROG_PR + ViewBag.EXEV_PROG_PR + ViewBag.VISI_PROG_PR + ViewBag.TRAIN_PROG_PR + ViewBag.AIR_PROG_PR;

                // حسب قاعدة معينة احصائيات القواعد
                ViewBag.AUTHO_COUNT_STN = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE==1).CountAsync(); 
                ViewBag.SEC_COUNT_STN = await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2).CountAsync();
                ViewBag.VEC_COUNT_STN = await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3).CountAsync();
                ViewBag.FAM_COUNT_STN = await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4).CountAsync();
                ViewBag.TCOM_COUNT_STN = await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_COMPANY_PASS_DTL.STATION_CODE == id).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5).CountAsync();
                ViewBag.CCOM_COUNT_STN = await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6).CountAsync();
                ViewBag.EXEV_COUNT_STN = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7).CountAsync();
                ViewBag.VISI_COUNT_STN = await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9).CountAsync();
                ViewBag.TRAIN_COUNT_STN = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8).CountAsync();
                ViewBag.AIR_COUNT_STN = await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10).CountAsync();



                //حسب قاعدة معينة in progress
                ViewBag.AUTHO_COUNT_STATIONS_P = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE==1).CountAsync();
                ViewBag.SEC_COUNT_STATIONS_P = await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2).CountAsync();
                ViewBag.VEC_COUNT_STATIONS_P = await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3).CountAsync();
                ViewBag.FAM_COUNT_STATIONS_P = await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4).CountAsync();
                ViewBag.TCOM_COUNT_STATIONS_P = await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_COMPANY_PASS_DTL.STATION_CODE == id && a.RA42_COMPANY_PASS_DTL.STATUS != true && a.RA42_COMPANY_PASS_DTL.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5).CountAsync();
                ViewBag.CCOM_COUNT_STATIONS_P = await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6).CountAsync();
                ViewBag.EXEV_COUNT_STATIONS_P = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7).CountAsync();
                ViewBag.VISI_COUNT_STATIONS_P = await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9).CountAsync();
                ViewBag.TRAIN_COUNT_STATIONS_P = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8).CountAsync();
                ViewBag.AIR_COUNT_STATIONS_P = await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS != true && a.REJECTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10).CountAsync();

                ViewBag.TOTAL_PERMITS_STATIONS_P = ViewBag.AUTHO_COUNT_STATIONS_P + ViewBag.SEC_COUNT_STATIONS_P + ViewBag.VEC_COUNT_STATIONS_P + ViewBag.FAM_COUNT_STATIONS_P + ViewBag.TCOM_COUNT_STATIONS_P +
                    ViewBag.CCOM_COUNT_STATIONS_P + ViewBag.EXEV_COUNT_STATIONS_P + ViewBag.VISI_COUNT_STATIONS_P + ViewBag.TRAIN_COUNT_STATIONS_P + ViewBag.AIR_COUNT_STATIONS_P;

                //حسب قاعدة معينة  rejected
                ViewBag.AUTHO_COUNT_STATIONS_R = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true).CountAsync()+ await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE==1).CountAsync();
                ViewBag.SEC_COUNT_STATIONS_R = await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2).CountAsync();
                ViewBag.VEC_COUNT_STATIONS_R = await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3).CountAsync();
                ViewBag.FAM_COUNT_STATIONS_R = await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4).CountAsync();
                ViewBag.TCOM_COUNT_STATIONS_R = await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_COMPANY_PASS_DTL.STATION_CODE == id && a.RA42_COMPANY_PASS_DTL.REJECTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5).CountAsync();
                ViewBag.CCOM_COUNT_STATIONS_R = await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6).CountAsync();
                ViewBag.EXEV_COUNT_STATIONS_R = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7).CountAsync();
                ViewBag.VISI_COUNT_STATIONS_R = await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9).CountAsync();
                ViewBag.TRAIN_COUNT_STATIONS_R = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8).CountAsync();
                ViewBag.AIR_COUNT_STATIONS_R = await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.REJECTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10).CountAsync();

                ViewBag.TOTAL_PERMITS_STATIONS_R = ViewBag.AUTHO_COUNT_STATIONS_R + ViewBag.SEC_COUNT_STATIONS_R + ViewBag.VEC_COUNT_STATIONS_R + ViewBag.FAM_COUNT_STATIONS_R + ViewBag.TCOM_COUNT_STATIONS_R +
                    ViewBag.CCOM_COUNT_STATIONS_R + ViewBag.EXEV_COUNT_STATIONS_R + ViewBag.VISI_COUNT_STATIONS_R + ViewBag.TRAIN_COUNT_STATIONS_R + ViewBag.AIR_COUNT_STATIONS_R;

                //حسب قاعدة معينة  completed
                ViewBag.AUTHO_COUNT_STATIONS_C = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true).CountAsync()+ await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE==1).CountAsync();
                ViewBag.SEC_COUNT_STATIONS_C = await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2).CountAsync();
                ViewBag.VEC_COUNT_STATIONS_C = await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3).CountAsync();
                ViewBag.FAM_COUNT_STATIONS_C = await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4).CountAsync();
                ViewBag.TCOM_COUNT_STATIONS_C = await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_COMPANY_PASS_DTL.STATION_CODE == id && a.RA42_COMPANY_PASS_DTL.STATUS == true && a.RA42_COMPANY_PASS_DTL.ISPRINTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5).CountAsync();
                ViewBag.CCOM_COUNT_STATIONS_C = await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6).CountAsync();
                ViewBag.EXEV_COUNT_STATIONS_C = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 7).CountAsync();
                ViewBag.VISI_COUNT_STATIONS_C = await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9).CountAsync();
                ViewBag.TRAIN_COUNT_STATIONS_C = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8).CountAsync();
                ViewBag.AIR_COUNT_STATIONS_C = await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED != true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10).CountAsync();

                ViewBag.TOTAL_PERMITS_STATIONS_C = ViewBag.AUTHO_COUNT_STATIONS_C + ViewBag.SEC_COUNT_STATIONS_C + ViewBag.VEC_COUNT_STATIONS_C + ViewBag.FAM_COUNT_STATIONS_C + ViewBag.TCOM_COUNT_STATIONS_C +
                    ViewBag.CCOM_COUNT_STATIONS_C + ViewBag.EXEV_COUNT_STATIONS_C + ViewBag.VISI_COUNT_STATIONS_C + ViewBag.TRAIN_COUNT_STATIONS_C + ViewBag.AIR_COUNT_STATIONS_C;

                //حسب قاعدة معينة  printed
                ViewBag.AUTHO_COUNT_STATIONS_PR = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true).CountAsync()+ await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE==1).CountAsync();
                ViewBag.SEC_COUNT_STATIONS_PR = await db.RA42_SECURITY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 2).CountAsync();
                ViewBag.VEC_COUNT_STATIONS_PR = await db.RA42_VECHILE_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 3).CountAsync();
                ViewBag.FAM_COUNT_STATIONS_PR = await db.RA42_FAMILY_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 4).CountAsync();
                ViewBag.TCOM_COUNT_STATIONS_PR = await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.DLT_STS != true && a.RA42_COMPANY_PASS_DTL.STATION_CODE == id && a.RA42_COMPANY_PASS_DTL.STATUS == true && a.RA42_COMPANY_PASS_DTL.ISPRINTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 5).CountAsync();
                ViewBag.CCOM_COUNT_STATIONS_PR = await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 6).CountAsync();
                ViewBag.EXEV_COUNT_STATIONS_PR = await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true && a.ACCESS_TYPE_CODE == 7).CountAsync();
                ViewBag.VISI_COUNT_STATIONS_PR = await db.RA42_VISITOR_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 9).CountAsync();
                ViewBag.TRAIN_COUNT_STATIONS_PR = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 8).CountAsync();
                ViewBag.AIR_COUNT_STATIONS_PR = await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true).CountAsync() + await db.RA42_PERMITS_DTL.Where(a => a.DLT_STS != true && a.STATION_CODE == id && a.STATUS == true && a.ISPRINTED == true && a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE == 10).CountAsync();

                ViewBag.TOTAL_PERMITS_STATIONS_PR = ViewBag.AUTHO_COUNT_STATIONS_PR + ViewBag.SEC_COUNT_STATIONS_PR + ViewBag.VEC_COUNT_STATIONS_PR + ViewBag.FAM_COUNT_STATIONS_PR + ViewBag.TCOM_COUNT_STATIONS_PR +
                    ViewBag.CCOM_COUNT_STATIONS_PR + ViewBag.EXEV_COUNT_STATIONS_PR + ViewBag.VISI_COUNT_STATIONS_PR + ViewBag.TRAIN_COUNT_STATIONS_PR + ViewBag.AIR_COUNT_STATIONS_PR;

                return View();
            }
            else
            {
                return RedirectToAction("NotFound", "Home");

            }
        }

        //view of choose force
        public ActionResult Forces()
        {
            ViewBag.AddPermit = "AddPermit";
            var v = db.RA42_FORCES_MST.Where(a => a.DLT_STS !=true).OrderBy(a => a.FORCE_CODE).ToList();

            return View(v);
        }
        //this is station view to let user select station that he wants to apply permit in 
        public ActionResult Stations(int id)
        {
            ViewBag.AddPermit = "AddPermit";
            ViewBag.Selected_Force = id;
            var v = db.RA42_STATIONS_MST.Where(a=>a.FORCE_ID == id).OrderByDescending(a => a.STATION_CODE).ToList();
            
            return View(v);
        }
        public ActionResult Permits()
        {
            ViewBag.AddPermit = "AddPermit";
            var url = Url.RequestContext.RouteData.Values["id"].ToString();
            if (url != null)
            {
                var id = int.Parse(url.ToString());
                var check = db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == id).FirstOrDefault();
                if (check != null)
                {

                    if (Language.GetCurrentLang() == "en")
                    {
                        ViewBag.Station = check.STATION_NAME_E;
                    }
                    else
                    {
                        ViewBag.Station = check.STATION_NAME_A;
                    }
                    Session["STATION_CODE"] = url;
                    ViewBag.Selected_Station = url;
                    ViewBag.Selected_Force = check.FORCE_ID;

                }
                else
                {
                    Session["STATION_CODE"] = null;
                    ViewBag.Selected_Station = null;
                    ViewBag.Selected_Force = null;


                }

            }
            var w = db.RA42_WORKFLOW_RESPONSIBLE_MST.Where(a => a.SERVICE_NUMBER == currentUser && a.ACTIVE != false && a.DLT_STS != true).FirstOrDefault();
            if (w != null)
            {
                var access = db.RA42_ACCESS_SELECT_MST.Where(a => a.WORKFLOW_RESPO_CODE == w.WORKFLOW_RESPO_CODE && a.DLT_STS != true).ToList();
                //get force id
                int force = Convert.ToInt32(ViewBag.FORCE_TYPE_CODE);
                var access_not_included = new List<string> {
                "1","6","10","11"};
                if (force == 1)
                {
                    access_not_included = new List<string> { };

                }
                if (force == 2)
                {
                    access_not_included = new List<string> {
                    "6","10","11"};

                    access = access.Where(a => !access_not_included.Contains(a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.ToString())).ToList();
                }

                if (force == 3)
                {
                    access_not_included = new List<string> {
                    "1","6","10","11"};

                    access = access.Where(a => !access_not_included.Contains(a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.ToString())).ToList();
                }

                if (force == 4)
                {
                    access_not_included = new List<string> {
                    "1","6","10","11"};

                    access = access.Where(a => !access_not_included.Contains(a.RA42_ACCESS_TYPE_MST.ACCESS_SECRET_CODE.ToString())).ToList();
                }



                return View(access.ToList());
            }
            else
            {
                var access = db.RA42_ACCESS_SELECT_MST.Where(a => a.WORKFLOW_RESPO_CODE == null).ToList();
                return View(access);
            }


        }

        //select view to select permit type 
        public async Task<ActionResult> Select()
        {
            ViewBag.AddPermit = "AddPermit";
            var url = Url.RequestContext.RouteData.Values["id"].ToString();
            var id = int.Parse(url.ToString());
            var force = "";
            if (Session["STATION_CODE"] != null)
            {
                string unit = Session["STATION_CODE"].ToString();
                var station_code = int.Parse(unit.ToString());
                var check = await db.RA42_STATIONS_MST.Where(a => a.STATION_CODE == station_code).FirstOrDefaultAsync();
                if (check != null)
                {
                    if (Language.GetCurrentLang() == "en")
                    {
                        ViewBag.Station = check.STATION_NAME_E;
                    }
                    else
                    {
                        ViewBag.Station = check.STATION_NAME_A;
                    }
                    Session["STATION_CODE"] = unit;
                    ViewBag.Selected_Force = check.FORCE_ID;
                    ViewBag.Selected_Station = unit;
                    force = check.RA42_FORCES_MST.FORCE_CODE;
                }
                else
                {
                    Session["STATION_CODE"] = null;
                    ViewBag.Selected_Force = null;
                    ViewBag.Selected_Station = null;
                }

            }
            var checkUser = await db.RA42_WORKFLOW_RESPONSIBLE_MST.Where(a => a.SERVICE_NUMBER == currentUser && a.ACTIVE != false && a.DLT_STS != true).FirstOrDefaultAsync();

            if (checkUser == null)
            {
                var permitsIncluded = new List<string> {
                "8","10","11","12","13","14","15","16","21","23","30","37"};
                var permits = await db.RA42_CARD_FOR_MST.Where(a => a.ACCESS_TYPE_CODE == id && a.DLT_STS != true && a.FORCES_IDS.Contains(force) && permitsIncluded.Contains(a.CARD_SECRET_CODE)).ToListAsync();

                return View(permits);
            }
            else
            {
                var permits = await db.RA42_CARD_FOR_MST.Where(a => a.ACCESS_TYPE_CODE == id && a.DLT_STS != true && a.FORCES_IDS.Contains(force)).ToListAsync();

                return View(permits);
            }

           
        }

        //update delivery
        [HttpPost]
        public JsonResult Delivery(int id,int access)
        {
            bool result = false;
            switch (access)
            {
                case 1:
                    var auth = db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.AUTHORAIZATION_CODE == id).FirstOrDefault();
                    auth.UPD_BY = currentUser;
                    auth.UPD_DT = DateTime.Now;
                    auth.ISDELIVERED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("delivery_status"),
                   "green"));
                    result = true;
                    break;
                case 2:
                    var sec = db.RA42_SECURITY_PASS_DTL.Where(a => a.SECURITY_CODE == id).FirstOrDefault();
                    sec.UPD_BY = currentUser;
                    sec.UPD_DT = DateTime.Now;
                    sec.ISDELIVERED = true;
                    db.SaveChanges();

                    AddToast(new Toast("",
                    GetResourcesValue("delivery_status"),
                   "green"));
                    result = true;
                    break;
                case 3:
                    var vec = db.RA42_VECHILE_PASS_DTL.Where(a => a.VECHILE_PASS_CODE == id).FirstOrDefault();
                    vec.UPD_BY = currentUser;
                    vec.UPD_DT = DateTime.Now;
                    vec.ISDELIVERED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("delivery_status"),
                   "green"));
                    result = true;
                    break;

                case 4:
                    var fam = db.RA42_FAMILY_PASS_DTL.Where(a => a.FAMILY_CODE == id).FirstOrDefault();
                    fam.UPD_BY = currentUser;
                    fam.UPD_DT = DateTime.Now;
                    fam.ISDELIVERED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("delivery_status"),
                   "green"));
                    result = true;
                    break;
                case 5:
                    var com = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.TEMPRORY_COMPANY_PASS_CODE == id).FirstOrDefault();
                    com.UPD_BY = currentUser;
                    com.UPD_DT = DateTime.Now;
                    com.ISDELIVERED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("delivery_status"),
                   "green"));
                    result = true;
                    break;
                case 6:
                    var con = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.CONTRACT_CODE == id).FirstOrDefault();
                    con.UPD_BY = currentUser;
                    con.UPD_DT = DateTime.Now;
                    con.ISDELIVERED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("delivery_status"),
                   "green"));
                    result = true;
                    break;
                case 7:
                    var eve = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == id && a.ACCESS_TYPE_CODE == 7).FirstOrDefault();
                    eve.UPD_BY = currentUser;
                    eve.UPD_DT = DateTime.Now;
                    eve.ISDELIVERED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("delivery_status"),
                   "green"));
                    result = true;
                    break;
                case 8:
                    var tra = db.RA42_TRAINEES_PASS_DTL.Where(a => a.TRAINEE_PASS_CODE == id).FirstOrDefault();
                    tra.UPD_BY = currentUser;
                    tra.UPD_DT = DateTime.Now;
                    tra.ISDELIVERED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("delivery_status"),
                   "green"));
                    result = true;
                    break;
                case 9:
                    var vis = db.RA42_VISITOR_PASS_DTL.Where(a => a.VISITOR_PASS_CODE == id).FirstOrDefault();
                    vis.UPD_BY = currentUser;
                    vis.UPD_DT = DateTime.Now;
                    vis.ISDELIVERED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("delivery_status"),
                   "green"));
                    result = true;
                    break;
                case 10:
                    var air = db.RA42_AIR_CREW_PASS_DTL.Where(a => a.AIR_CREW_PASS_CODE == id).FirstOrDefault();
                    air.UPD_BY = currentUser;
                    air.UPD_DT = DateTime.Now;
                    air.ISDELIVERED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("delivery_status"),
                   "green"));
                    result = true;
                    break;

                case 1000:
                    var permit = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == id).FirstOrDefault();
                    permit.UPD_BY = currentUser;
                    permit.UPD_DT = DateTime.Now;
                    permit.ISDELIVERED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("delivery_status"),
                   "green"));
                    result = true;
                    break;
                default:
                    AddToast(new Toast("",
                     GetResourcesValue("error"),
                    "red"));
                    result = true;
                    break;
            }

            var transactions = db.RA42_TRANSACTION_DTL.Where(a => a.ACCESS_TYPE_CODE == access && a.ACCESS_ROW_CODE == id && a.DLT_STS !=true && a.RA42_TRANSACTION_TYPE_MST.AMOUNT !=0).OrderByDescending(a=>a.TRANSACTION_CODE).ToList();
            int b = 1;
            foreach (var item in transactions)
            {
               
                    if (b == 1)
                    {

                        item.CRD_DT = DateTime.Today;
                        item.UPD_BY = currentUser;
                        item.UPD_DT = DateTime.Now;
                        db.SaveChanges();
                        b = b + 1;
                    }
                
                
                
            }


            return Json(result, JsonRequestBehavior.AllowGet);


        }

        [HttpPost]
        public JsonResult Returned(int id, int access)
        {
            bool result = false;
            switch (access)
            {
               

                case 2:
                    var sec = db.RA42_SECURITY_PASS_DTL.Where(a => a.SECURITY_CODE == id).FirstOrDefault();
                    sec.UPD_BY = currentUser;
                    sec.UPD_DT = DateTime.Now;
                    sec.RETURNED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("returned_status"),
                   "green"));
                    result = true;
                    break;
                case 3:
                    var vec = db.RA42_VECHILE_PASS_DTL.Where(a => a.VECHILE_PASS_CODE == id).FirstOrDefault();
                    vec.UPD_BY = currentUser;
                    vec.UPD_DT = DateTime.Now;
                    vec.RETURNED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("returned_status"),
                   "green"));
                    result = true;
                    break;

                case 4:
                    var fam = db.RA42_FAMILY_PASS_DTL.Where(a => a.FAMILY_CODE == id).FirstOrDefault();
                    fam.UPD_BY = currentUser;
                    fam.UPD_DT = DateTime.Now;
                    fam.RETURNED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("returned_status"),
                   "green"));
                    result = true;
                    break;
                case 5:
                    var com = db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.TEMPRORY_COMPANY_PASS_CODE == id).FirstOrDefault();
                    com.UPD_BY = currentUser;
                    com.UPD_DT = DateTime.Now;
                    com.RETURNED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("returned_status"),
                   "green"));
                    result = true;
                    break;
                case 6:
                    var con = db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.CONTRACT_CODE == id).FirstOrDefault();
                    con.UPD_BY = currentUser;
                    con.UPD_DT = DateTime.Now;
                    con.RETURNED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("returned_status"),
                   "green"));
                    result = true;
                    break;
                case 7:
                    var eve = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == id && a.ACCESS_TYPE_CODE == 7).FirstOrDefault();
                    eve.UPD_BY = currentUser;
                    eve.UPD_DT = DateTime.Now;
                    eve.RETURNED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("returned_status"),
                   "green"));
                    result = true;
                    break;
                case 8:
                    var tra = db.RA42_TRAINEES_PASS_DTL.Where(a => a.TRAINEE_PASS_CODE == id).FirstOrDefault();
                    tra.UPD_BY = currentUser;
                    tra.UPD_DT = DateTime.Now;
                    tra.RETURNED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("returned_status"),
                   "green"));
                    result = true;
                    break;
                case 9:
                    var vis = db.RA42_VISITOR_PASS_DTL.Where(a => a.VISITOR_PASS_CODE == id).FirstOrDefault();
                    vis.UPD_BY = currentUser;
                    vis.UPD_DT = DateTime.Now;
                    vis.RETURNED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("returned_status"),
                   "green"));
                    result = true;
                    break;
                case 10:
                    var air = db.RA42_AIR_CREW_PASS_DTL.Where(a => a.AIR_CREW_PASS_CODE == id).FirstOrDefault();
                    air.UPD_BY = currentUser;
                    air.UPD_DT = DateTime.Now;
                    air.RETURNED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("returned_status"),
                   "green"));
                    result = true;
                    break;
                case 1000:
                    var permits = db.RA42_PERMITS_DTL.Where(a => a.PERMIT_CODE == id).FirstOrDefault();
                    permits.UPD_BY = currentUser;
                    permits.UPD_DT = DateTime.Now;
                    permits.RETURNED = true;
                    db.SaveChanges();
                    AddToast(new Toast("",
                    GetResourcesValue("returned_status"),
                   "green"));
                    result = true;
                    break;
                default:
                    AddToast(new Toast("",
                     GetResourcesValue("error"),
                    "red"));
                    result = true;
                    break;
            }


            return Json(result, JsonRequestBehavior.AllowGet);


        }

        [HttpPost]
        public JsonResult NoPrevent(int id)
        {
            var result = false;
            ViewBag.controllerName = "";
            ViewBag.controllerNamePlural = Resources.Passes.ResourceManager.GetString("Violations" + "_" + ViewBag.lang);
            ViewBag.controllerNameSingular = Resources.Passes.ResourceManager.GetString("Violations" + "_" + ViewBag.lang);
            ViewBag.ViolationPage = "Violations";

            var v = db.RA42_VECHILE_VIOLATION_DTL.Where(a => a.VECHILE_VIOLATION_CODE == id).FirstOrDefault();
            if (v != null)
            {
                v.PREVENT = false;
                v.UPD_BY = currentUser;
                v.UPD_DT = DateTime.Now;
                db.SaveChanges();
                AddToast(new Toast("",
                    GetResourcesValue("updated_successfully"),
                   "green"));
                result = true;
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            AddToast(new Toast("",
                    GetResourcesValue("error"),
                   "red"));
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
            return Resources.Passes
                .ResourceManager
                .GetString(key + "_" + ViewBag.lang);
        }

        public void AddToast(Toast toast)
        {
            toasts.Add(toast);
            TempData["toasts"] = toasts;
        }

        static readonly string[] SizeSuffixes =
                  { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }

            int i = 0;
            decimal dValue = (decimal)value;
            while (Math.Round(dValue, decimalPlaces) >= 1000)
            {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[i]);
        }

    }
}
