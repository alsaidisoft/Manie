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
    
    public partial class RA42_SECURITY_PASS_DTL
    {
        public int SECURITY_CODE { get; set; }
        public Nullable<int> CARD_FOR_CODE { get; set; }
        public Nullable<int> STATION_CODE { get; set; }
        public string SERVICE_NUMBER { get; set; }
        public string RESPONSIBLE { get; set; }
        public string CIVIL_NUMBER { get; set; }
        public string RANK_A { get; set; }
        public string RANK_E { get; set; }
        public string NAME_A { get; set; }
        public string NAME_E { get; set; }
        public string PERSONAL_IMAGE { get; set; }
        public string PHONE_NUMBER { get; set; }
        public string GSM { get; set; }
        public Nullable<int> IDENTITY_CODE { get; set; }
        public Nullable<int> GENDER_ID { get; set; }
        public Nullable<int> UNIT_CODE { get; set; }
        public string UNIT_A { get; set; }
        public string UNIT_E { get; set; }
        public string PROFESSION_A { get; set; }
        public string PROFESSION_E { get; set; }
        public Nullable<int> PASS_TYPE_CODE { get; set; }
        public Nullable<int> COMPANY_CODE { get; set; }
        public Nullable<int> ACCESS_TYPE_CODE { get; set; }
        public Nullable<System.DateTime> DATE_FROM { get; set; }
        public Nullable<System.DateTime> DATE_TO { get; set; }
        public string PURPOSE_OF_PASS { get; set; }
        public string REMARKS { get; set; }
        public Nullable<int> WORKFLOW_RESPO_CODE { get; set; }
        public string BARCODE { get; set; }
        public string APPROVAL_SN { get; set; }
        public string APPROVAL_RANK { get; set; }
        public string APPROVAL_NAME { get; set; }
        public Nullable<System.DateTime> APPROVAL_APPROVISION_DATE { get; set; }
        public string AUTHO_SN { get; set; }
        public string AUTHO_RANK { get; set; }
        public string AUTHO_NAME { get; set; }
        public Nullable<System.DateTime> AUTHO_APPROVISION_DATE { get; set; }
        public string PERMIT_SN { get; set; }
        public string PERMIT_RANK { get; set; }
        public string PERMIT_NAME { get; set; }
        public Nullable<System.DateTime> PERMIT_APPROVISION_DATE { get; set; }
        public Nullable<bool> REJECTED { get; set; }
        public Nullable<bool> ISOPENED { get; set; }
        public Nullable<bool> ISPRINTED { get; set; }
        public Nullable<bool> STATUS { get; set; }
        public Nullable<bool> ISDELIVERED { get; set; }
        public Nullable<bool> RETURNED { get; set; }
        public Nullable<bool> DLT_STS { get; set; }
        public string CRD_BY { get; set; }
        public Nullable<System.DateTime> CRD_DT { get; set; }
        public string UPD_BY { get; set; }
        public Nullable<System.DateTime> UPD_DT { get; set; }
    
        public virtual RA42_ACCESS_TYPE_MST RA42_ACCESS_TYPE_MST { get; set; }
        public virtual RA42_CARD_FOR_MST RA42_CARD_FOR_MST { get; set; }
        public virtual RA42_COMPANY_MST RA42_COMPANY_MST { get; set; }
        public virtual RA42_GENDERS_MST RA42_GENDERS_MST { get; set; }
        public virtual RA42_IDENTITY_MST RA42_IDENTITY_MST { get; set; }
        public virtual RA42_PASS_TYPE_MST RA42_PASS_TYPE_MST { get; set; }
        public virtual RA42_STATIONS_MST RA42_STATIONS_MST { get; set; }
        public virtual RA42_WORKFLOW_RESPONSIBLE_MST RA42_WORKFLOW_RESPONSIBLE_MST { get; set; }
    }
}
