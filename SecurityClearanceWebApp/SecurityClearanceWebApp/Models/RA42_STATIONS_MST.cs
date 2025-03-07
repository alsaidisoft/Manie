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
    
    public partial class RA42_STATIONS_MST
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public RA42_STATIONS_MST()
        {
            this.RA42_AIR_CREW_PASS_DTL = new HashSet<RA42_AIR_CREW_PASS_DTL>();
            this.RA42_AUTHORAIZATION_SIGNAURE_MST = new HashSet<RA42_AUTHORAIZATION_SIGNAURE_MST>();
            this.RA42_AUTHORIZATION_PASS_DTL = new HashSet<RA42_AUTHORIZATION_PASS_DTL>();
            this.RA42_COMPANY_MST = new HashSet<RA42_COMPANY_MST>();
            this.RA42_COMPANY_PASS_DTL = new HashSet<RA42_COMPANY_PASS_DTL>();
            this.RA42_CONTRACTING_COMPANIES_PASS_DTL = new HashSet<RA42_CONTRACTING_COMPANIES_PASS_DTL>();
            this.RA42_FAMILY_PASS_DTL = new HashSet<RA42_FAMILY_PASS_DTL>();
            this.RA42_PERMITS_DTL = new HashSet<RA42_PERMITS_DTL>();
            this.RA42_REPORTS_MST = new HashSet<RA42_REPORTS_MST>();
            this.RA42_SECTIONS_MST = new HashSet<RA42_SECTIONS_MST>();
            this.RA42_SECURITY_PASS_DTL = new HashSet<RA42_SECURITY_PASS_DTL>();
            this.RA42_TRAINEES_PASS_DTL = new HashSet<RA42_TRAINEES_PASS_DTL>();
            this.RA42_VECHILE_PASS_DTL = new HashSet<RA42_VECHILE_PASS_DTL>();
            this.RA42_VISITOR_PASS_DTL = new HashSet<RA42_VISITOR_PASS_DTL>();
            this.RA42_WORKFLOW_RESPONSIBLE_MST = new HashSet<RA42_WORKFLOW_RESPONSIBLE_MST>();
            this.RA42_ZONE_AREA_MST = new HashSet<RA42_ZONE_AREA_MST>();
        }
    
        public int STATION_CODE { get; set; }
        public string STATION_NAME_A { get; set; }
        public string STATION_NAME_E { get; set; }
        public string UNIT_CODE { get; set; }
        public Nullable<int> FORCE_ID { get; set; }
        public string PHONE_NUMBER { get; set; }
        public string REMARKS { get; set; }
        public string CRD_BY { get; set; }
        public Nullable<System.DateTime> CRD_DT { get; set; }
        public string UPD_BY { get; set; }
        public Nullable<System.DateTime> UPD_DT { get; set; }
        public Nullable<bool> DLT_STS { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_AIR_CREW_PASS_DTL> RA42_AIR_CREW_PASS_DTL { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_AUTHORAIZATION_SIGNAURE_MST> RA42_AUTHORAIZATION_SIGNAURE_MST { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_AUTHORIZATION_PASS_DTL> RA42_AUTHORIZATION_PASS_DTL { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_COMPANY_MST> RA42_COMPANY_MST { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_COMPANY_PASS_DTL> RA42_COMPANY_PASS_DTL { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_CONTRACTING_COMPANIES_PASS_DTL> RA42_CONTRACTING_COMPANIES_PASS_DTL { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_FAMILY_PASS_DTL> RA42_FAMILY_PASS_DTL { get; set; }
        public virtual RA42_FORCES_MST RA42_FORCES_MST { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_PERMITS_DTL> RA42_PERMITS_DTL { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_REPORTS_MST> RA42_REPORTS_MST { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_SECTIONS_MST> RA42_SECTIONS_MST { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_SECURITY_PASS_DTL> RA42_SECURITY_PASS_DTL { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_TRAINEES_PASS_DTL> RA42_TRAINEES_PASS_DTL { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_VECHILE_PASS_DTL> RA42_VECHILE_PASS_DTL { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_VISITOR_PASS_DTL> RA42_VISITOR_PASS_DTL { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_WORKFLOW_RESPONSIBLE_MST> RA42_WORKFLOW_RESPONSIBLE_MST { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RA42_ZONE_AREA_MST> RA42_ZONE_AREA_MST { get; set; }
    }
}
