using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using APP.Util;
using SecurityClearanceWebApp.Controllers;
using SecurityClearanceWebApp.Models;

namespace portal.Controllers
{
    public class UserInfo
    {
        RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        public string getSNO()
        {
        
            string serviceNumber = "D1-7932";

            //if (System.Web.HttpContext.Current.User.Identity.IsAuthenticated)

            //{
            //    serviceNumber = System.Web.HttpContext.Current.User.Identity.Name.Substring(4);
            //}
            return serviceNumber.ToUpper();
            
        }

        public string FULL_NAME(string service_number)
        {
            string full_name = "";
            var checkName = Task.Run(async () => await db.RA42_WORKFLOW_RESPONSIBLE_MST.Where(a => a.SERVICE_NUMBER == service_number.ToUpper() && a.DLT_STS != true).FirstOrDefaultAsync()).Result;
            if (checkName != null) { 
                if (Language.GetCurrentLang() == "en")
                {
                    full_name = checkName.RANK_E + "/" + checkName.RESPONSEPLE_NAME_E;
                }
                else
                {
                    full_name = checkName.RANK + "/" + checkName.RESPONSEPLE_NAME;
                }
            }
            else
            {
                var user_ = new Dictionary<string, string>();

                try
                {
                    if (!string.IsNullOrEmpty(service_number))
                    {
                        User user = null;
                        Task<User> callTask = Task.Run(
                            () => getUserInfoFromAPI(service_number)
                            );
                        callTask.Wait();
                        user = callTask.Result;
                        if (Language.GetCurrentLang() == "en")
                        {
                            full_name = user.NAME_RANK_E + "/" + user.NAME_EMP_E;
                        }
                        else
                        {
                            full_name = user.NAME_RANK_A + "/" + user.NAME_EMP_A;
                        }
                    }
                }
                catch (Exception)
                {
                    user_.Add("state", "error");
                }
            }
                return full_name;
            
        }

        public string WorkFlowType(string sn)
        {

            var v = Task.Run(async ()=> await db.RA42_WORKFLOW_RESPONSIBLE_MST.Where(a => a.SERVICE_NUMBER == sn && a.DLT_STS !=true).FirstOrDefaultAsync()).Result;
            string workflow;
            if (v != null)
            {
                if (Language.GetCurrentLang() == "en")
                {
                    workflow = v.RA42_WORKFLOW_MST.STEP_NAME_E;
                }
                else {
                    workflow = v.RA42_WORKFLOW_MST.STEP_NAME;

                }
            }
            else
            {

                if (Language.GetCurrentLang() == "en")
                {
                    workflow = "Request owner";

                }
                else
                {
                    workflow = "مقدم الطلب";


                }




            }
            
            return workflow;
        }

        public string getSignatureUrl(string sn)
        {

            RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
            var v = db.RA42_WORKFLOW_RESPONSIBLE_MST.Where(a => a.SERVICE_NUMBER == sn).FirstOrDefault();
            string signature;
            if (v != null)
            {
                signature = v.SIGNATURE;
            }
            else
            {
                signature = null;
            }

            return signature;
        }

        public Dictionary<string, string> getUserInfoFromDB()
        {
            var user_ = new Dictionary<string, string>();
            var user_service_number = getSNO();
            user_.Add("user_sno", user_service_number);

            try
            {

                User user = null;
                Task<User> callTask = Task.Run(
                    () => getUserInfoFromAPI(user_service_number)
                    );
                callTask.Wait();
                user = callTask.Result;


                user_.Add("user_name_ar", user.NAME_EMP_A);
                user_.Add("user_name_en", user.NAME_EMP_E);
                user_.Add("user_rank_ar", user.NAME_RANK_A);
                user_.Add("user_rank_en", user.NAME_RANK_E);
                user_.Add("user_post_ar", user.NAME_POST_A);
                user_.Add("user_post_en", user.NAME_POST_E);
                user_.Add("user_unit_code", user.UNIT_CODE.ToString());
                user_.Add("user_unit_name_en", user.NAME_UNIT_E);
                user_.Add("user_unit_name_ar", user.NAME_UNIT_A);
                user_.Add("user_force_name_ar", user.NAME_FORCE_A);
                user_.Add("user_force_name_en", user.NAME_FORCE_E);
                user_.Add("user_force_trade_ar", user.NAME_TRADE_A);
                user_.Add("user_force_trade_en", user.NAME_TRADE_E);
                user_.Add("user_phone", user.EMP_GSM_NO);
                user_.Add("user_posting_station_code", user.POSTING_STN.ToString());
                user_.Add("state", "success");
            }
            catch (Exception)
            {
                user_.Add("state", "error");
            }

            return user_;

        }

        internal User getUserInfoFromAPI(object p)
        {
            throw new NotImplementedException();
        }

        //new api to get specific HRMS/MIMS user from api 
        public async Task<User> getUserInfoFromAPI(string userSno)
        {
            var mi = new MilitaryInformationController();
            var jund = await mi.GetMilitaryPersonInfo(userSno);
            if (jund != null)
            {
                return mi.MapJundUser(jund);

            }

            return null;

            //    HttpClient client = new HttpClient();
            //var Token = "YC9m8ojhW9kK5zEMXUWLb7yDKMuFGeQNLNq6c38tPtPQvM9yfvsZbdK4DmwapgjTcQrdixgDhCGhkTVFff2Ls2ebEJmrgnrg3eCSCfqyTxcqaGCu8BeYwSaenUshXAavvtQWC9dEA9GwdV9XB4WAUNhfnqeFEJFtRGJUCkdQ2oZ4ZccCpAdz2jkCVHkEfWwY5iNiLrdizvgZBr3X4RWjwpyzfHQCLsUQVtDPmxcPADavZuVH9UbLcYvdDGadCEz8";
            //var Code = "105733";
            //var values = new Dictionary<string, string>
            //{
            //    {"Code", Code},
            //    {"Token", Token},
            //    {"Key", userSno},
            //    {"SearchedBy", ""},
            //};

            //var content = new FormUrlEncodedContent(values);

            //var response = await client.PostAsync("http://mamrafowebgov01/API/hr/search", content);


            //var responseString = await response.Content.ReadAsAsync<User>();
            //return responseString;
        }


        public Dictionary<string, string> getUserInfo()
        {
            var userInfo = new Dictionary<string, string>();
            if (System.Web.HttpContext.Current.Session["user_info"] != null)
            {
                //user info is exists in session
                userInfo = (Dictionary<string, string>)System.Web.HttpContext.Current.Session["user_info"];
            }
            else
            {
                //user info isn't exists in session
                userInfo = this.getUserInfoFromDB();
                System.Web.HttpContext.Current.Session["user_info"] = userInfo;
            }

            return userInfo;
        }

    }
}