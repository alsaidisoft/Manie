using SecurityClearanceWebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SecurityClearanceWebApp.Util
{
       
        public class GroupedClearanceSearchResult
    {
        public int CompanyPermitId { get; set; }
        public string ServiceNumber { get; set; }
        public string Responsipole { get; set; } = null;

        public string Company { get; set; } = null;
        public string MainPurposeOfPass { get; set; }
        public string ControllerName { get; set; } = "";
        public int Comments { get; set; }

        public bool? MainRejected { get; set; }
        public bool? MainOpened { get; set; }
        public bool? MainPrinted { get; set; }
        public bool? MainStatus { get; set; }

        public DateTime CompanyPermitCreatedDate { get; set; }
        public List<ClearanceSearchResult> Items { get; set; }
    }
}