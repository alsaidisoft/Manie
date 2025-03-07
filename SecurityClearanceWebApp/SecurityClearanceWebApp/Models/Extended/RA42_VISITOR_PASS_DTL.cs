using APP.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace SecurityClearanceWebApp.Models
{
    [MetadataType(typeof(RA42_VISITOR_PASS_DTLMetadata))]
    public partial class RA42_VISITOR_PASS_DTL
    {
        RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();

        public static implicit operator RA42_VISITOR_PASS_DTL(HttpPostedFileBase v)
        {
            throw new NotImplementedException();
        }

        public int countCommennts(int visitor_pass_code)
        {
           
            var v = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == visitor_pass_code && a.ACCESS_TYPE_CODE == 9).Count();
            return v;
        }


       public int AllowedMemebrsTotal(int visitor_id)
        {
            int total = 1;
            var v = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_TYPE_CODE == 9 && a.ACCESS_ROW_CODE == visitor_id && a.DLT_STS != true).ToList();

            if(v.Count > 0)
            {
                 total = total + v.Count;
            }

            return total;
        }
        

        

        public string getZoneName(int? id)
        {
            string name;
            if (id != 0)
            {
                var v = db.RA42_ZONE_SUB_AREA_MST.Where(a => a.ZONE_SUB_AREA_CODE == id).FirstOrDefault();
                if (Language.GetCurrentLang() == "en")
                {
                    name = v.SUB_ZONE_NAME_E;

                }
                else
                {
                    name = v.SUB_ZONE_NAME;
                }
            }
            else
            {
                if (Language.GetCurrentLang() == "en")
                {
                    name = "";

                }
                else
                {
                    name = "";
                }
            }

            return name;
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
    public class RA42_VISITOR_PASS_DTLMetadata
    {

        public int VISITOR_PASS_CODE { get; set; }
        public string VISITOR_NAME { get; set; }
        public string ID_CARD_NUMBER { get; set; }
        [Required(ErrorMessage = "الجنسية مطلوبة")]
        public Nullable<int> IDENTITY_CODE { get; set; }
        [Required(ErrorMessage = "الجنس مطلوب")]
        public string GENDER_ID { get; set; }
        public string GSM { get; set; }
        public string PERSONAL_IMAGE { get; set; }
        public string PURPOSE_OF_PASS { get; set; }
        public string REMARKS { get; set; }
        public Nullable<int> CARD_FOR_CODE { get; set; }
        public Nullable<int> STATION_CODE { get; set; }
        public string BARCODE { get; set; }
        public Nullable<System.DateTime> DATE_FROM { get; set; }
        public Nullable<System.DateTime> DATE_TO { get; set; }
        public Nullable<int> WORKFLOW_RESPO_CODE { get; set; }
        public Nullable<int> ACCESS_TYPE_CODE { get; set; }
        public Nullable<bool> STATUS { get; set; }
        public Nullable<bool> DLT_STS { get; set; }
        public string CRD_BY { get; set; }
        public Nullable<System.DateTime> CRD_DT { get; set; }
        public string UPD_BY { get; set; }
        public Nullable<System.DateTime> UPD_DT { get; set; }



    }
}