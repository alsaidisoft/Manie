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
        public class PaymentResult
        {
        public int Id { get; set; }
        public string Sympol { get; set; }
        public string Name { get; set; }
        public string AccessType { get; set; } = "";
        public string Unit { get; set; } = "";
        public string TransactionType { get; set; } = "";
        public DateTime IssueDate { get; set; }
        public DateTime ExpiredDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public double Amount { get; set; }


    }
}