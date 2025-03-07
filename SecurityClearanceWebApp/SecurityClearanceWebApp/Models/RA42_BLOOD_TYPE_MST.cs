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
    
    public partial class RA42_BLOOD_TYPE_MST
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public RA42_BLOOD_TYPE_MST()
        {
            this.RA42_AIR_CREW_PASS_DTL = new HashSet<RA42_AIR_CREW_PASS_DTL>();
            this.RA42_PERMITS_DTL = new HashSet<RA42_PERMITS_DTL>();
        }
    
        public int BLOOD_CODE { get; set; }
        public string BLOOD_TYPE { get; set; }
        public string REMARKS { get; set; }
        public Nullable<bool> DLT_STS { get; set; }
        public string CRD_BY { get; set; }
        public Nullable<System.DateTime> CRD_DT { get; set; }
        public string UPD_BY { get; set; }
        public Nullable<System.DateTime> UPD_DT { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_AIR_CREW_PASS_DTL> RA42_AIR_CREW_PASS_DTL { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_PERMITS_DTL> RA42_PERMITS_DTL { get; set; }
    }
}
