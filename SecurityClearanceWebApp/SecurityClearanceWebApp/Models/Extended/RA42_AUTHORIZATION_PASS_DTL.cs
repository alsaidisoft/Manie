using APP.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SecurityClearanceWebApp.Models
{
    [MetadataType(typeof(RA42_AUTHORIZATION_PASS_DTLMetadata))]
    public partial class RA42_AUTHORIZATION_PASS_DTL
    {
        RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();

        public static implicit operator RA42_AUTHORIZATION_PASS_DTL(HttpPostedFileBase v)
        {
            throw new NotImplementedException();
        }

        public int countCommennts(int vechile_pass_code)
        {

            var v = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == vechile_pass_code && a.ACCESS_TYPE_CODE == 1).Count();
            return v;
        }

      

        public string CheckDate(DateTime date)
        {
            string check = "";
            if (date < DateTime.Today)
            {
                check = "منتهي";
            }
            else
            {
                check = "مستمر";
            }


            return check;
        }
    }
    public class RA42_AUTHORIZATION_PASS_DTLMetadata
    {

        public int AUTHORAIZATION_CODE { get; set; }
        public int STATION_CODE { get; set; }
        public string SERVICE_NUMBER { get; set; }
        public string RANK_A { get; set; }
        public string RANK_E { get; set; }
        public string NAME_A { get; set; }
        public string NAME_E { get; set; }
        public string PERSONAL_IMAGE { get; set; }
        public string PROFESSION_A { get; set; }
        public string PROFESSION_E { get; set; }
        public string PHONE_NUMBER { get; set; }
        public string GSM { get; set; }
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
        public Nullable<System.DateTime> DATE_FROM { get; set; }
        public Nullable<System.DateTime> DATE_TO { get; set; }
        public string CRD_BY { get; set; }
        public Nullable<System.DateTime> CRD_DT { get; set; }
        public string UPD_BY { get; set; }
        public Nullable<System.DateTime> UPD_DT { get; set; }
        public Nullable<int> WORKFLOW_RESPO_CODE { get; set; }
        public Nullable<bool> REJECTED { get; set; }
        public Nullable<bool> STATUS { get; set; }
        public Nullable<bool> DLT_STS { get; set; }
    }
}