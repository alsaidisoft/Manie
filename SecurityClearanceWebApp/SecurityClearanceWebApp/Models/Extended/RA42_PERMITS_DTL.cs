using APP.Util;
using portal.Controllers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace SecurityClearanceWebApp.Models
{
    [MetadataType(typeof(RA42_PERMITS_DTLMetadata))]
    public partial class RA42_PERMITS_DTL
    {
        RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();


        public static implicit operator RA42_PERMITS_DTL(HttpPostedFileBase v)
        {
            throw new NotImplementedException();
        }

        public  int countCommennt(int permits_code, int access_type_code)
        {
            //var v = await db.RA42_COMMENTS_MST.Where(a => a.ACCESS_TYPE_CODE == access_type_code && a.PASS_ROW_CODE == permits_code  && a.DLT_STS !=true && a.COMMENT_CODE >= 36117).CountAsync();
            var v = db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == permits_code && a.DLT_STS != true && a.ACCESS_TYPE_CODE == access_type_code && a.CRD_DT > new DateTime(2024, 7, 1)).Count();

            return v;
        }


        public int AllowedMemebrsTotal(int id)
        {
            int total = 1;
            DateTime specificDate = new DateTime(2024, 7, 1);

            var v = db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_TYPE_CODE == 9 && a.ACCESS_ROW_CODE == id && a.DLT_STS != true).ToList();

            if (v.Count > 0)
            {
                total = total + v.Count;
            }

            return total;
        }


        public string getFullName(string service_number)
        {
            UserInfo user = new UserInfo();
        return user.FULL_NAME(service_number); 
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



        public List<RA42_ZONE_MASTER_MST> Zones(int? pass_code,int access_type_code)
        {
            List<RA42_ZONE_MASTER_MST> zone = new List<RA42_ZONE_MASTER_MST>();
            var v = db.RA42_ZONE_MASTER_MST.Where(a => a.ACCESS_TYPE_CODE == access_type_code && a.ACCESS_ROW_CODE == pass_code && a.DLT_STS != true).ToList();
            if (v != null)
            {
                zone = v;
            }

            return zone;
        }

       

    }
    public class RA42_PERMITS_DTLMetadata
    {


        public int PERMIT_CODE { get; set; }
        public Nullable<int> ACCESS_TYPE_CODE { get; set; }
        public Nullable<int> CARD_FOR_CODE { get; set; }
        public Nullable<int> STATION_CODE { get; set; }
        public Nullable<int> BLOOD_CODE { get; set; }
        public Nullable<int> IDENTITY_CODE { get; set; }
        public Nullable<int> GENDER_ID { get; set; }
        public Nullable<int> RELATIVE_TYPE_CODE { get; set; }
        public Nullable<int> VECHILE_CODE { get; set; }
        public Nullable<int> VECHILE_NAME_CODE { get; set; }
        public Nullable<int> PLATE_CHAR_CODE { get; set; }
        public Nullable<int> VECHILE_COLOR_CODE { get; set; }
        public Nullable<int> PASS_TYPE_CODE { get; set; }
        public Nullable<int> COMPANY_CODE { get; set; }
        public Nullable<int> EVENT_EXERCISE_CODE { get; set; }
        public Nullable<int> WORKFLOW_RESPO_CODE { get; set; }
        public string SERVICE_NUMBER { get; set; }
        public string RESPONSIBLE { get; set; }
        public string CIVIL_NUMBER { get; set; }
        public string PASSPORT_NUMBER { get; set; }
        public string RANK_A { get; set; }
        public string RANK_E { get; set; }
        public string NAME_A { get; set; }
        public string NAME_E { get; set; }
        public string HOST_RANK_A { get; set; }
        public string HOST_RANK_E { get; set; }
        public string HOST_NAME_A { get; set; }
        public string HOST_NAME_E { get; set; }
        public string PERSONAL_IMAGE { get; set; }
        public string PHONE_NUMBER { get; set; }
        public string GSM { get; set; }
        public Nullable<int> NUMBER_OF_HOSTED { get; set; }
        public string SN_FOR_THE_GUARDIAN { get; set; }
        public string VISITOR_EMPLOYER { get; set; }
        public Nullable<System.DateTime> CARD_EXPIRED_DATE { get; set; }
        public Nullable<System.DateTime> JOINING_DATE { get; set; }
        public string UNIT_A { get; set; }
        public string UNIT_E { get; set; }
        public string PROFESSION_A { get; set; }
        public string PROFESSION_E { get; set; }
        public string BUILDING_NUMBER { get; set; }
        public Nullable<int> PLATE_CODE { get; set; }
        public string PLATE_NUMBER { get; set; }
        public string WORK_PLACE { get; set; }
        public string ADDRESS { get; set; }
        public Nullable<System.DateTime> DATE_FROM { get; set; }
        public Nullable<System.DateTime> DATE_TO { get; set; }
        public string PURPOSE_OF_PASS { get; set; }
        public string REMARKS { get; set; }
        public string REGISTRATION_SERIAL { get; set; }
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
        public Nullable<int> COMPANY_PASS_CODE { get; set; }
        public string HOST_PROFESSION_A { get; set; }
        public string HOST_PROFESSION_E { get; set; }


    }
}