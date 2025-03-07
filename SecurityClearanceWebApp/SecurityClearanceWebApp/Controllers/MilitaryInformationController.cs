using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using APP.Util;
using IdentityModel.Client;
using Newtonsoft.Json;
using portal.Controllers;
using SecurityClearanceWebApp.Models;

//this controller developed by Ghaith
namespace SecurityClearanceWebApp.Controllers
{
    public class GetInfoReceiveData
    {
        public string sNo { get; set; }
    }
    
    //this controller to get user military information 
    public class MilitaryInformationController : Controller
    {
        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        private string Api = "app/integration/person-by-military-service-id_v3";
       

        public string getApi() {

            var url = db.RA42_API.Where(a => a.IsActive == true).FirstOrDefault();
              if(url != null)
              {
              Api = url.FullStringUrl;
              }
            return Api; 
        
        }

      

        private Dictionary<string, string> JundKeys = new Dictionary<string, string>()
        {
            
            { "AuthServer", "https://jundauth"},
            {"Server", "https://jundbe/api" },
            {"ClientId", "RAFO_Apps"},
            {"ClientSecret", "RAFO@123"},
            {"Scope", "Jund"},
            {"MilitaryPersonInfo", "app/integration/person-by-military-service-id" },
            {"MilitaryPersonPhoto", "app/integration/person-photo-by-person-id"}
        };
        private HttpClient JundClient { get; set; }


        //get user info by service number 
        [HttpPost]
        public async Task<ActionResult> GetInfo(string sNo)
        {

            var jund = await GetMilitaryPersonInfo(sNo);
            if (jund != null)
            {
                var user = MapJundUser(jund);
                return Json(new { status = "1", info = user }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { status = "0" }, JsonRequestBehavior.AllowGet);
            }
            //return Json(values, JsonRequestBehavior.AllowGet);
            //UserInfo userInfo = new UserInfo();
            //var user = await userInfo.getUserInfoFromAPI(sNo);

            //if (user != null)
            //{
            //    return Json(new { status = "1", info = user }, JsonRequestBehavior.AllowGet);
            //}
            //else
            //{
            //    return Json(new { status = "0" }, JsonRequestBehavior.AllowGet);
            //}
        }

        public User MapJundUser(JundUserDto jund)
        {
            return new User()
            {
                EMP_SERVICE_NO = jund.serviceNumber,
                UNIT_CODE = int.Parse(jund.hrmsMainUnitCode),
                NAME_RANK_A = jund.rankArabic,
                NIC_NO = jund.civilNumber,
                EMP_GSM_NO = jund.mobile,
                NAME_EMP_A = jund.empNameAr,
                NAME_POST_A = jund.mainUnitAr /*+ " - "+jund.positionAr*/,
                NAME_TRADE_A = jund.positionAr,
                NAME_UNIT_A = jund.currentUnitAr,
                NAME_FORCE_A = jund.serviceBranchNameAr,
                RANK_CODE = jund.rankEnlgish,
                NAME_RANK_E = jund.rankEnlgish,
                NAME_EMP_E = jund.empNameEn,
                NAME_TRADE_E = jund.positionEn,
                NAME_POST_E = jund.mainUnitEn /*+ " - " + jund.positionEn*/,
                NAME_FORCE_E = jund.serviceBranchNameEn,
                NAME_UNIT_E =  jund.currentUnitEn,
                POSTING_STN = int.Parse(jund.hrmsMainUnitCode),
            };
        }

        //get specific data by service number 
        public string GetNamesByServiceNumber(string srv)
        {
            try
            {
                JundUserDto user = null;
                Task<JundUserDto> callTask = Task.Run(
                    () => GetMilitaryPersonInfo(srv)
                    );
                callTask.Wait();
                user = callTask.Result;
                if (user == null)
                {
                    return "";
                }

                return (Language.GetCurrentLang() == "en")
                    ? user.rankEnlgish + "-" + user.empNameEn
                    : user.rankArabic + "-" + user.empNameAr;
            }
            catch (Exception)
            {
                //ignore
            }
            return "";
        }
        //get station name by service number 
        public string GetStationNamesByServiceNumber(string srv)
        {
            try
            {

                JundUserDto user = null;
                Task<JundUserDto> callTask = Task.Run(
                    () => GetMilitaryPersonInfo(srv)
                    );
                callTask.Wait();
                user = callTask.Result;
                if(user == null) {
                    return "";
                }

                return (Language.GetCurrentLang() == "en")
                    ? user.mainUnitEn
                    : user.mainUnitAr;
            }
            catch (Exception)
            {
                //ignore
            }
            return "";
        }
        //get posted position by service number 
        public string GetPostNameByServiceNumber(string srv)
        {
            try
            {

                JundUserDto user = null;
                Task<JundUserDto> callTask = Task.Run(
                    () => GetMilitaryPersonInfo(srv)
                    );
                callTask.Wait();
                user = callTask.Result;
                if (user == null)
                {
                    return "";
                }

                return (Language.GetCurrentLang() == "en")
                    ? user.positionEn
                    : user.positionAr;
            }
            catch (Exception)
            {
                //ignore
            }
            return "";
        }

        public async Task<bool> InitConnection()
        {
            JundClient = new HttpClient();
            var descover = await JundClient.GetDiscoveryDocumentAsync(JundKeys["AuthServer"]);
            if (descover.IsError)
            {
                return false;
            }

            var tokenResponse = await JundClient.RequestClientCredentialsTokenAsync(
                new ClientCredentialsTokenRequest
                {
                    Address = descover.TokenEndpoint,
                    ClientId = JundKeys["ClientId"],
                    ClientSecret = JundKeys["ClientSecret"],
                    Scope = JundKeys["Scope"]
                });

            if (tokenResponse.IsError)
            {
                return false;
            }

            JundClient.SetBearerToken(tokenResponse.AccessToken);


            return true;
        }

        public async Task<List<JundUserDto>> GetMilitaryPeopleInfo(List<string> serviceNumbers)
        {
            var list = new List<JundUserDto>();
            try
            {
                if (!await this.InitConnection())
                {
                    return null;
                }

                foreach (var serviceNumber in serviceNumbers.Where(r => !string.IsNullOrWhiteSpace(r)).ToList())
                {
                    var getInfo = await GetJundInfo(serviceNumber);
                    if (getInfo != null)
                    {
                        list.Add(getInfo);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            return list;
        }

        public async Task<JundUserDto> GetMilitaryPersonInfo(string serviceNumber)
        {
            var response = new JundUserDto();
            try
            {
                if (!await this.InitConnection())
                {
                    return null;
                }

                return await GetJundInfo(serviceNumber);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
            return null;
        }

        private async Task<JundUserDto> GetJundInfo(string serviceNumber)
        {
            try
            {
                
                var url = $"{JundKeys["Server"]}/{getApi()}/{serviceNumber}";
                var response = await JundClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var user = JsonConvert.DeserializeObject<JundUserDto>(content);

                //var photoUrl = $"{JundKeys["Server"]}/{JundKeys["MilitaryPersonPhoto"]}/{user.personId}";
                //var photoResponse = await JundClient.GetAsync(photoUrl);
                //var photo = await photoResponse.Content.ReadAsStringAsync();

                return user;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            return null;
        }
    }
}