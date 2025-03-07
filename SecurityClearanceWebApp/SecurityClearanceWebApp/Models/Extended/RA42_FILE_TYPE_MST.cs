using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace SecurityClearanceWebApp.Models
{
    [MetadataType(typeof(RA42_FILE_TYPE_MSTMetadata))]
    public partial class RA42_FILE_TYPE_MST
    {

        public static implicit operator RA42_FILE_TYPE_MST(HttpPostedFileBase v)
        {
            throw new NotImplementedException();
        }

        public int[] ACCESS_TYPE_CODE { get; set; }
        public int[] FORCE_ID { get; set; }
        public int[] CARD_FOR_CODE { get; set; }




    }
    public class RA42_FILE_TYPE_MSTMetadata
    {



    }
}