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
    
    public partial class RA42_AUTHORAIZATION_SIGNAURE_MST
    {
        public int AUTHO_SIGNATURE_CODE { get; set; }
        public Nullable<int> STATION_ID { get; set; }
        public string SIGNATURE { get; set; }
        public Nullable<bool> DLT_STS { get; set; }
        public string CRD_BY { get; set; }
        public Nullable<System.DateTime> CRD_DT { get; set; }
        public string UPD_BY { get; set; }
        public Nullable<System.DateTime> UPD_DT { get; set; }
    
        public virtual RA42_STATIONS_MST RA42_STATIONS_MST { get; set; }
    }
}
