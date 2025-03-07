using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APP.Util
{
    public class User
    {
        public string EMP_SERVICE_NO { get; set; }
        public int? UNIT_CODE { get; set; }
        public string NAME_RANK_A { get; set; }
        public string NIC_NO { get; set; }
        public string EMP_GSM_NO { get; set; }
        public string NAME_EMP_A { get; set; }
        public string NAME_POST_A { get; set; }
        public string NAME_TRADE_A { get; set; }
        public string NAME_UNIT_A { get; set; }
        public string NAME_FORCE_A { get; set; }
        public string RANK_CODE { get; set; }
        public DateTime? LAST_PROMOTION_DT { get; set; }
        public string NAME_RANK_E { get; set; }
        public string NAME_EMP_E { get; set; }
        public string NAME_TRADE_E { get; set; }
        public string NAME_POST_E { get; set; }
        public string NAME_FORCE_E { get; set; }
        public string NAME_UNIT_E { get; set; }
        public int? POSTING_STN { get; set; }
        //public int? EMP_FORCE { get; set; }
    }
}