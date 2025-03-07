using APP.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace SecurityClearanceWebApp.Models
{
    [MetadataType(typeof(RA42_WORKFLOW_RESPONSIBLE_MSTMetadata))]
    public partial class RA42_WORKFLOW_RESPONSIBLE_MST
    {
        RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();

        public static implicit operator RA42_WORKFLOW_RESPONSIBLE_MST(HttpPostedFileBase v)
        {
            throw new NotImplementedException();
        }

        public int[] ACCESS_TYPE_CODE { get; set; }
        //public List<RA42_ACCESS_SELECT_MST> rA42_ACCESS_SELECT_MSTs { get; set; }

      



    }
    public class RA42_WORKFLOW_RESPONSIBLE_MSTMetadata
    {

        


    }
}