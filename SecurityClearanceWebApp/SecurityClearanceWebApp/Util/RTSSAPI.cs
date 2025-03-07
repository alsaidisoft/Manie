using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace APP.Util
{

    public class RTSSAPIAuthentication
    {
        public string Code { get; set; }
        public string Token { get; set; }
    }

    public class RTSSAPIUsernameHash
    {
        public RTSSAPIAuthentication Auth { get; set; }
        public string Username { get; set; }
    }

    public class RTSSAPINotification
    {
        public int Id { get; set; }
        public List<string> ServiceNumberList { get; set; } = null;
        public string TargetAppCode { get; set; }
        public string TargetPlatform { get; set; } = "W"; //W => web, D => desktop
        public string Message { get; set; }
        public string TextDirection { get; set; } = "left";
        public string AlertType { get; set; } = "dark";
        public int IsForAll { get; set; } = 0;
        public string Link { get; set; } = null;
        public string CreatedAt { get; set; } = null;
    }

    public class RTSSAPIPushNotification
    {
        public RTSSAPIAuthentication Auth { get; set; }
        public RTSSAPINotification Notification { get; set; }
    }

    public class RTSSAPI
    {


        public readonly RTSSAPIAuthentication auth = new RTSSAPIAuthentication
        {
            //to get hash and code the application should be added to
            //RTSS application by admin, then he give you code and token for
            //yor application
            Code = "112233",
            Token = "avVAgT9KEwJZCBm8NmLghuimT9Dg7FNKYWSVhHvSwuPLkoLTgvBMvJNnFD6dd9JNVZbWysB6P2YCYepc6P4XikiMACSQrekjTkkhZyxUXWDGkzpcvdnpGbnxYjhmgrqtJqZsfGSvmCoDENRWn7CmquTJEFceE3goU3bpWNNiatQqqtFqkn4f3HKZNe8KGKRE7qo6mnjVvrtqgdTUKkEKjpHwwiesAi88Jjx8rwHofRAiPH8wJiZyMYUc32QNhV6P"
        };

        private readonly string ApiURL = "http://mamrafowebgov01/RTSS/api/";

        public async Task<HttpResponseMessage> SendData(object values, string section)
        {
            try
            {
                var client = new HttpClient();
                var content = JsonConvert.SerializeObject(values);
                var stringContent = new StringContent(content, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(this.ApiURL + section, stringContent);
                return response;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<string> GetClientHashUserName(string username)
        {
            try
            {
                var values = new RTSSAPIUsernameHash
                {
                    Auth = auth,
                    Username = username
                };

                Task<HttpResponseMessage> callTask = Task.Run(
                    () => this.SendData(values, "access/hash")
                );
                callTask.Wait();
                var response = callTask.Result;
                var responseString = await response.Content.ReadAsAsync<string>();
                return responseString;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<string> SendNotification(RTSSAPINotification notification)
        {
            try
            {

                var values = new RTSSAPIPushNotification()
                {
                    Auth = auth,
                    Notification = notification
                };

                Task<HttpResponseMessage> callTask = Task.Run(
                    () => this.SendData(values, "notification/push")
                );
                callTask.Wait();
                var response = callTask.Result;
                var responseString = await response.Content.ReadAsAsync<string>();
                return responseString;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}