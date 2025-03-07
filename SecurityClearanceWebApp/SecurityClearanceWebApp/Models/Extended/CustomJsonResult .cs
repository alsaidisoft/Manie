using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SecurityClearanceWebApp.Models
{
    public class CustomJsonResult
    {
        public int ACCESS_TYPE_CODE { get; set; }
        public int VECHILE_VIOLATION_CODE { get; set; }
        public int ACCESS_ROW_CODE { get; set; }
        public string ACCESS_TYPE { get; set; }
        public string VIOLATION_DESC { get; set; }
        public string VIOLATION_TYPE { get; set; }
        public string VIOLATION_PRICE { get; set; }
        public string VIOLATION_BY { get; set; }
        public string CONTROLLER { get; set; }
        public Nullable<bool> PREVENT { get; set; }
        public Nullable<System.DateTime> VIOLATION_DATE { get; set; }
      
    }
}