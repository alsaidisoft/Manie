using SecurityClearanceWebApp.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.SqlServer.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace SecurityClearanceWebApp.Util
{
        
        public class ClearanceSearchResult
        {
        public int Id { get; set; }
        public int CompanyPermitId { get; set; }
        public int AccessNumber { get; set; }
        public int CardFor { get; set; }
        public string ControllerName { get; set; } = "";
        public string ActionName { get; set; } = "";
        public string ServiceNumber { get; set; }
        public string CivilNumber { get; set; }
        public string Name { get; set; }
        public string Rank { get; set; }
        public string Unit { get; set; } = "";
        public string PesronalImage { get; set; }
        public string Phone { get; set; }
        public string Gsm { get; set; }
        public string PermitType { get; set; }
        public string RegistrationSerial { get; set; }
        public string PassType { get; set; }
        public string PurposeOfPass { get; set; }
        public string MainPurposeOfPass { get; set; }
        public string Workflow { get; set; }
        public string Company { get; set; } = null;
        public string SnGardian { get; set; } = null;
        public string EventExercise { get; set; } = null;
        public string Relative { get; set; } = null;
        public string WorkflowServiceNumber { get; set; }
        public int WorkflowId { get; set; }
        public string CarName { get; set; }
        public string IssueingDate { get; set; }
        public DateTime ExpiredDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public int Station { get; set; }
        public int Comments { get; set; }
        public int Violations { get; set; }
        public int NumOfHosted { get; set; }
        public string HomeNumber { get; set; }
        public string VisitorEmployer { get; set; }
        public string StationName { get; set; }
        public int Force { get; set; }
        public string PlateNumber { get; set; }
        public string PlateCode { get; set; }
        public string CarColor { get; set; }
        public string CarType { get; set; }
        public string ResponsipoleServiceNumber { get; set; } = null;
        public string Responsipole { get; set; } = null;

        public string HostName { get; set; } = null;
        public string HosRank { get; set; } = null;

        public bool? Rejected { get; set; }
        public bool? Opened { get; set; }
        public bool? Printed { get; set; }
        public bool? Status { get; set; }

        public bool? MainRejected { get; set; }
        public bool? MainOpened { get; set; }
        public bool? MainPrinted { get; set; }
        public bool? MainStatus { get; set; }
        public bool? Deleted { get; set; }
        public bool? Delivered { get; set; }
        public bool? Returned { get; set; }
        public string StatusTitle
        {
            get
            {
                if (Rejected == true)
                {
                    return "مرفوض";
                    
                }
                else if (Status == true)
                {
                    return "مؤكد";
                }
                return "قيد المتابعة";
            }
        }
        public string PersonalInfo
        {
            get
            {
                string info = "<div dir='rtl' class='text-right'>";
                if (!string.IsNullOrWhiteSpace(CivilNumber))
                {
                    info += $" <h6>الرقم المدني: {CivilNumber}</h6>";
                }
                else if (!string.IsNullOrWhiteSpace(ServiceNumber))
                {
                    info += $" <h6>الرقم العسكري: {ServiceNumber}</h6>";
                }
                if (!string.IsNullOrWhiteSpace(Name))
                {
                    info += $"<h6>الاسم: {Name}</h6>";
                }
                if (PermitType.Contains("تصريح المركبات")) {
                    info += $"<h6>رقم السيارة: {PlateNumber}</h6>";
                    info += $"<h6>لون السيارة: {CarColor}</h6>";
                    info += $"<h6>إسم السيارة: {CarName}</h6>";
                    info += $"<h6>نوع السيارة: {CarType }</h6>";
                }
                return info += "</div>";
            }
        }

        public bool IsExpired => (DateTime.Today > ExpiredDate);

        public int countCommennts(int permits_code, int access_type_code)
        {
            RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
            DateTime specificDate = new DateTime(2024, 7, 1);
            var v = Task.Run(async () => await db.RA42_COMMENTS_MST.Where(a => a.PASS_ROW_CODE == permits_code && a.ACCESS_TYPE_CODE == access_type_code && a.DLT_STS !=true && a.CRD_DT > new DateTime(2024, 8, 1)).CountAsync()).Result;
            return v;
        }


        public int AllowedMemebrsTotal(int id)
        {
            RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
            int total = 1;
            DateTime specificDate = new DateTime(2024, 7, 1);

            var v = Task.Run(async () => await db.RA42_MEMBERS_DTL.Where(a => a.ACCESS_TYPE_CODE == 9 && a.ACCESS_ROW_CODE == id && a.DLT_STS != true).CountAsync()).Result;
            
            total = total + v;
            

            return total;
        }

        public bool IsExpiringWithinOneMonth(DateTime expiryDate)
        {
            DateTime currentDate = DateTime.Now;
            DateTime oneMonthBeforeExpiry = expiryDate.AddMonths(-1);

            // Check if the current date is within the last month before the expiry date
            return currentDate >= oneMonthBeforeExpiry && currentDate <= expiryDate;
        }

    }
}