using APP.Util;
using SecurityClearanceWebApp.Util;
using System;
using System.Collections.Generic;


namespace SecurityClearanceWebApp.Models
{
    public partial class GroupPermitsForCompanies
    {

        public Nullable<int> ACCESS_TYPE_CODE { get; set; }
        public Nullable<int> CARD_FOR_CODE { get; set; }
        public Nullable<int> STATION_CODE { get; set; }
        public Nullable<int> COMPANY_CODE { get; set; }
        public Nullable<int> WORKFLOW_RESPO_CODE { get; set; }
        public string PURPOSE_OF_PASS { get; set; }
        public string REMARKS { get; set; }
        public string BARCODE { get; set; }
        public string UNIT_A { get; set; }
        public string RESPONSIBLE { get; set; }
        public List<RA42_PERMITS_DTL> permits { get; set; } = new List<RA42_PERMITS_DTL> ();


    }
}