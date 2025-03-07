using APP.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace SecurityClearanceWebApp.Models
{
    [MetadataType(typeof(RA42_CARD_FOR_MSTMetadata))]
    public partial class RA42_CARD_FOR_MST
    {
        RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();

        public static implicit operator RA42_CARD_FOR_MST(HttpPostedFileBase v)
        {
            throw new NotImplementedException();
        }

        public int[] FORCE_CODE { get; set; }

    }
    public class RA42_CARD_FOR_MSTMetadata
    {

    }
}