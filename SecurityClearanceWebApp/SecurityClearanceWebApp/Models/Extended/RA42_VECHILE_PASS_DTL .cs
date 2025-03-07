using APP.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace SecurityClearanceWebApp.Models
{
    [MetadataType(typeof(RA42_VECHILE_PASS_DTLMetadata))]
    public partial class RA42_VECHILE_PASS_DTL
    {
        RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();

        private int ACCESS_TYPE = 3;

        public static implicit operator RA42_VECHILE_PASS_DTL(HttpPostedFileBase v)
        {
            throw new NotImplementedException();
        }

        public int countCommennts(int vechile_pass_code)
        {
           
            var v = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == vechile_pass_code && a.ACCESS_TYPE_CODE == ACCESS_TYPE).Count();
            return v;
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
            if(date < DateTime.Today)
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

      


        

        public List<RA42_ZONE_MASTER_MST> Zones(int? pass_code)
        {
            List<RA42_ZONE_MASTER_MST> zone = new List<RA42_ZONE_MASTER_MST>();
            var v = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_TYPE_CODE == ACCESS_TYPE && a.ACCESS_ROW_CODE == pass_code && a.DLT_STS != true).ToList();
            if (v != null)
            {
                zone = v;
            }

            return zone;
        }

       

    }
    public class RA42_VECHILE_PASS_DTLMetadata
    {


        public int VECHILE_PASS_CODE { get; set; }
        public string SERVICE_NUMBER { get; set; }
        public string CIVIL_NUMBER { get; set; }
        public string RANK_A { get; set; }
        public string RANK_E { get; set; }
        public string NAME_A { get; set; }
        public string NAME_E { get; set; }
        public string PERSONAL_IMAGE { get; set; }
        public string PHONE_NUMBER { get; set; }
        [Required(ErrorMessage = "required - رقم الهاتف النقال مطلوب")]
        public string GSM { get; set; }
        [Required(ErrorMessage = "required - الجنسية مطلوبة")]
        public Nullable<int> IDENTITY_CODE { get; set; }
        [Required(ErrorMessage = "required - الجنس مطلوب")]
        public Nullable<int> GENDER_ID { get; set; }
        public Nullable<int> STATION_CODE { get; set; }
        public string UNIT_A { get; set; }
        public string UNIT_E { get; set; }
        public string PROFESSION_A { get; set; }
        public string PROFESSION_E { get; set; }
        [Required(ErrorMessage = "required - نوع السيارة مطلوب")]
        public Nullable<int> VECHILE_CODE { get; set; }
        [Required(ErrorMessage = "required - اسم السيارة مطلوب")]
        public Nullable<int> VECHILE_NAME_CODE { get; set; }
        [Required(ErrorMessage = "required - نوع اللوحة مطلوب")]
        public Nullable<int> PLATE_CODE { get; set; }
        [Required(ErrorMessage = "required - رقم اللوحة مطلوب")]
        public Nullable<int> PLATE_NUMBER { get; set; }
        [Required(ErrorMessage = "required - رمز اللوحة مطلوب")]
        public Nullable<int> PLATE_CHAR_CODE { get; set; }
        [Required(ErrorMessage = "required - لون السيارة مطلوب")]
        public Nullable<int> VECHILE_COLOR_CODE { get; set; }
        [Required(ErrorMessage = "required - نوع التصريح مطلوب")]
        public Nullable<int> PASS_TYPE_CODE { get; set; }
        public Nullable<int> ACCESS_TYPE_CODE { get; set; }
        public Nullable<System.DateTime> DATE_FROM { get; set; }
        public Nullable<System.DateTime> DATE_TO { get; set; }
        public string PURPOSE_OF_PASS { get; set; }
        public string REMARKS { get; set; }
        //[Required(ErrorMessage = "المفوض الأمني مطلوب")]
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
        public Nullable<bool> STATUS { get; set; }
        public Nullable<bool> DLT_STS { get; set; }
        public string CRD_BY { get; set; }
        public Nullable<System.DateTime> CRD_DT { get; set; }
        public string UPD_BY { get; set; }
        public Nullable<System.DateTime> UPD_DT { get; set; }


    }
}