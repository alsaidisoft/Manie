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
    
    public partial class RA42_TEMPRORY_COMPANY_PASS_DTL
    {
        public int TEMPRORY_COMPANY_PASS_CODE { get; set; }
        public Nullable<int> ACCESS_TYPE_CODE { get; set; }
        public Nullable<int> COMPANY_PASS_CODE { get; set; }
        public string WORK_CARD_NUMBER { get; set; }
        public Nullable<System.DateTime> CARD_EXPIRED_DATE { get; set; }
        public string ID_CARD_NUMBER { get; set; }
        public string NAME_A { get; set; }
        public string NAME_E { get; set; }
        public string PROFESSION_A { get; set; }
        public string PROFESSION_E { get; set; }
        public string WORK_PLACE { get; set; }
        public string PERSONAL_IMAGE { get; set; }
        public Nullable<int> GENDER_ID { get; set; }
        public Nullable<int> IDENTITY_CODE { get; set; }
        public string ADDRESS { get; set; }
        public string GSM { get; set; }
        public Nullable<int> PASS_TYPE_CODE { get; set; }
        public string BARCODE { get; set; }
        public Nullable<System.DateTime> DATE_FROM { get; set; }
        public Nullable<System.DateTime> DATE_TO { get; set; }
        public Nullable<bool> ISPRINTED { get; set; }
        public Nullable<bool> ISDELIVERED { get; set; }
        public Nullable<bool> RETURNED { get; set; }
        public Nullable<bool> DLT_STS { get; set; }
        public string CRD_BY { get; set; }
        public Nullable<System.DateTime> CRD_DT { get; set; }
        public string UPD_BY { get; set; }
        public Nullable<System.DateTime> UPD_DT { get; set; }
    
        public virtual RA42_ACCESS_TYPE_MST RA42_ACCESS_TYPE_MST { get; set; }
        public virtual RA42_COMPANY_PASS_DTL RA42_COMPANY_PASS_DTL { get; set; }
        public virtual RA42_GENDERS_MST RA42_GENDERS_MST { get; set; }
        public virtual RA42_IDENTITY_MST RA42_IDENTITY_MST { get; set; }
        public virtual RA42_PASS_TYPE_MST RA42_PASS_TYPE_MST { get; set; }
    }
}
