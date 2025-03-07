using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Http;
using APP.Util;

namespace SecurityClearanceWebApp.Models
{
    [MetadataType(typeof(RA42_CONTRACTING_COMPANIES_PASS_DTLMetadata))]
    public partial class RA42_CONTRACTING_COMPANIES_PASS_DTL
    {
        RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();

        public static implicit operator RA42_CONTRACTING_COMPANIES_PASS_DTL(HttpPostedFileBase v)
        {
            throw new NotImplementedException();
        }

        public int countCommennts(int company_pass_code)
        {
           
            var v = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == company_pass_code && a.ACCESS_TYPE_CODE == 6).Count();
            return v;
        }

        



      
        public string CheckDate(DateTime date)
        {
            string check = "";
            if (date < DateTime.Today)
            {
                if (Language.GetCurrentLang() == "en")
                {
                    check = "Expired";

                }
                else
                {
                    check = "منتهي";
                }
            }
            else
            {
                if (Language.GetCurrentLang() == "en")
                {
                    check = "Continued";

                }
                else
                {
                    check = "مستمر";
                }
            }
            return check;
        }

      

       

    }
    public class RA42_CONTRACTING_COMPANIES_PASS_DTLMetadata
    {

        public int CONTRACT_CODE { get; set; }
        public Nullable<int> STATION_CODE { get; set; }
        public string SERVICE_NUMBER { get; set; }
        public string RESPONSIBLE { get; set; }
        public Nullable<int> COMPANY_TYPE_CODE { get; set; }
        [Required(ErrorMessage = "اسم الشركة مطلوب ")]
        public Nullable<int> COMPANY_CODE { get; set; }
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
        public string BARCODE { get; set; }
        public Nullable<int> ACCESS_TYPE_CODE { get; set; }
        public string PURPOSE_OF_PASS { get; set; }
        public string REMARKS { get; set; }
        public Nullable<int> WORKFLOW_RESPO_CODE { get; set; }
        public Nullable<bool> REJECTED { get; set; }
        public Nullable<bool> STATUS { get; set; }
        public Nullable<bool> DLT_STS { get; set; }
        public string CRD_BY { get; set; }
        public Nullable<System.DateTime> CRD_DT { get; set; }
        public string UPD_BY { get; set; }
        public Nullable<System.DateTime> UPD_DT { get; set; }



    }
}