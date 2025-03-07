using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SecurityClearanceWebApp.Util
{
        //public class ClearanceSearchRequest
        //{
        //    public string Query { get; set; }
        //    public string Token { get; set; }
        //}
        public class IdentityResult
    {
        public string Company { get; set; }
        public string Type { get; set; }
        public string Identity { get; set; }
        public string Total { get; set; } = "";
       


    }
}