//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SecurityClearanceWebApp.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class RA42_ANNOUNCEMENT_MST
    {
        public int ANNOUNCE_CODE { get; set; }
        public string ANNOUNCEMENT_A { get; set; }
        public string ANNOUNCEMENT_E { get; set; }
        public string URL { get; set; }
        public System.DateTime START_DATE { get; set; }
        public System.DateTime END_DATE { get; set; }
        public Nullable<bool> DLT_STS { get; set; }
        public string CRD_BY { get; set; }
        public Nullable<System.DateTime> CRD_DT { get; set; }
        public string UPD_BY { get; set; }
        public Nullable<System.DateTime> UPD_DT { get; set; }
    }
}
