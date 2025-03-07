using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SecurityClearanceWebApp.Util
{
       
        public class UserSearchResult
    {
        public int WORKFLOW_RESPO_CODE { get; set; }
        public Nullable<int> WORKFLOWID { get; set; }
        public string SERVICE_NUMBER { get; set; }
        public string RANK { get; set; }
        public string RANK_E { get; set; }
        public string RESPONSEPLE_NAME { get; set; }
        public string RESPONSEPLE_NAME_E { get; set; }
        public string FORCE_NAME { get; set; }
        public string FORCE_NAME_E { get; set; }
        public string CAMP_NAME { get; set; }
        public string CAMP_NAME_E { get; set; }
        public Nullable<int> FORCE_TYPE { get; set; }
        public Nullable<int> UNIT_CODE { get; set; }
        public Nullable<int> STATION_CODE { get; set; }
        public string UNIT_NAME { get; set; }
        public string UNIT_NAME_E { get; set; }
        public Nullable<bool> ACTIVE { get; set; }
        public string SIGNATURE { get; set; }
        public Nullable<bool> AD { get; set; }
        public Nullable<bool> DL { get; set; }
        public Nullable<bool> UP { get; set; }
        public Nullable<bool> VW { get; set; }
        public Nullable<bool> RP { get; set; }
        public Nullable<bool> SETTINGS { get; set; }
        public Nullable<bool> DEVELOPER { get; set; }
        public Nullable<bool> ADMIN { get; set; }
        public Nullable<bool> DLT_STS { get; set; }
        public string CRD_BY { get; set; }
        public Nullable<System.DateTime> CRD_DT { get; set; }
        public string UPD_BY { get; set; }
        public Nullable<System.DateTime> UPD_DT { get; set; }
    }
}