//using Microsoft.AspNetCore.Mvc.Filters;
using portal.Controllers;
using SecurityClearanceWebApp.Models;
using SecurityClearanceWebApp.Util;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace SecurityClearanceWebApp.Services
{
   
    public class PermitsCountService
    {

        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        public PermitsCountService() { }
        string currentUser = (new UserInfo()).getSNO();

        public async Task<int> CountMyPermits()
        {


            int total = 0;

            try
            {


                //var access = await db.RA42_ACCESS_TYPE_MST.ToListAsync();


                var autho = await db.RA42_AUTHORIZATION_PASS_DTL.Where(a => a.SERVICE_NUMBER == currentUser && a.DLT_STS != true && a.ISPRINTED != true && a.STATUS != true && a.WORKFLOW_RESPO_CODE != null).CountAsync();
                if (autho > 0)
                    total = total + autho;

                var secu = await db.RA42_SECURITY_PASS_DTL.Where(a => a.SERVICE_NUMBER == currentUser && a.ISPRINTED != true && a.DLT_STS != true && a.STATUS != true && a.WORKFLOW_RESPO_CODE != null).CountAsync();
                if (secu > 0)
                    total = total + secu;


                var vech = await db.RA42_VECHILE_PASS_DTL.Where(a => a.SERVICE_NUMBER == currentUser && a.ISPRINTED != true && a.DLT_STS != true && a.STATUS != true && a.WORKFLOW_RESPO_CODE != null).CountAsync();
                if (vech > 0)
                    total = total + vech;

                var fami = await db.RA42_FAMILY_PASS_DTL.Where(a => a.SERVICE_NUMBER == currentUser && a.ISPRINTED != true && a.DLT_STS != true && a.STATUS != true && a.WORKFLOW_RESPO_CODE != null).CountAsync();
                if (fami > 0)
                    total = total + fami;

               

                var air = await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.SERVICE_NUMBER == currentUser && a.ISPRINTED != true && a.DLT_STS != true && a.STATUS != true && a.WORKFLOW_RESPO_CODE != null).CountAsync();
                if (air > 0)
                    total = total + air;

                var my_permits = await db.RA42_PERMITS_DTL.Where(a => a.SERVICE_NUMBER == currentUser && a.ISPRINTED != true && a.DLT_STS != true && a.STATUS != true && a.WORKFLOW_RESPO_CODE != null).CountAsync();
                if (my_permits > 0)
                    total = total + my_permits;





            }
            catch
            {
                //ignore
                
            }

           
           

            return total;
        }

        public async Task<int> CountOtherPermits()
        {

            int othertotal = 0;

            try
            {

                var secu1 = await db.RA42_SECURITY_PASS_DTL.Where(a => a.RESPONSIBLE == currentUser && a.SERVICE_NUMBER != currentUser && a.DLT_STS != true && a.ISPRINTED != true && a.STATUS != true && a.WORKFLOW_RESPO_CODE != null).CountAsync();
                if (secu1 > 0)
                    othertotal = othertotal + secu1;

                var vech1 = await db.RA42_VECHILE_PASS_DTL.Where(a => a.RESPONSIBLE == currentUser && a.SERVICE_NUMBER != currentUser && a.DLT_STS != true && a.ISPRINTED != true && a.STATUS != true && a.WORKFLOW_RESPO_CODE != null).CountAsync();

                if (vech1 > 0)
                    othertotal = othertotal + vech1;

                var fami1 = await db.RA42_FAMILY_PASS_DTL.Where(a => a.RESPONSIBLE == currentUser && a.SERVICE_NUMBER != currentUser && a.DLT_STS != true && a.ISPRINTED != true && a.STATUS != true && a.WORKFLOW_RESPO_CODE != null).CountAsync();

                if (fami1 > 0)
                    othertotal = othertotal + fami1;

                var comp11 = await db.RA42_TEMPRORY_COMPANY_PASS_DTL.Where(a => a.RA42_COMPANY_PASS_DTL.SERVICE_NUMBER == currentUser && a.RA42_COMPANY_PASS_DTL.ISPRINTED != true && a.RA42_COMPANY_PASS_DTL.DLT_STS != true && a.RA42_COMPANY_PASS_DTL.STATUS != true && a.DLT_STS != true && a.RA42_COMPANY_PASS_DTL.WORKFLOW_RESPO_CODE != null).CountAsync();

                if (comp11 > 0)
                    othertotal = othertotal + comp11;

                var cont1 = await db.RA42_CONTRACTING_COMPANIES_PASS_DTL.Where(a => a.SERVICE_NUMBER == currentUser && a.ISPRINTED != true && a.DLT_STS != true && a.STATUS != true && a.WORKFLOW_RESPO_CODE != null).CountAsync();

                if (cont1 > 0)
                    othertotal = othertotal + cont1;

                var cards_for = new List<string> {
                        "11","13","14","16"};
                var visi1 = await db.RA42_VISITOR_PASS_DTL.Where(a => a.SERVICE_NUMBER == currentUser && a.ISPRINTED != true && a.DLT_STS != true && a.STATUS != true && !cards_for.Contains(a.CARD_FOR_CODE.ToString()) && a.WORKFLOW_RESPO_CODE != null).CountAsync();

                if (visi1 > 0)
                    othertotal = othertotal + visi1;

                var train1 = await db.RA42_TRAINEES_PASS_DTL.Where(a => a.SERVICE_NUMBER == currentUser && a.ISPRINTED != true && a.DLT_STS != true && a.STATUS != true && a.WORKFLOW_RESPO_CODE != null).CountAsync();

                if (train1 > 0)
                    othertotal = othertotal + train1;

                var air1 = await db.RA42_AIR_CREW_PASS_DTL.Where(a => a.RESPONSIBLE == currentUser && a.SERVICE_NUMBER != currentUser && a.DLT_STS != true && a.ISPRINTED != true && a.STATUS != true && a.WORKFLOW_RESPO_CODE != null).CountAsync();

                if (air1 > 0)
                    othertotal = othertotal + air1;
            
                var other_permits = await db.RA42_PERMITS_DTL.Where(a => a.RESPONSIBLE == currentUser && a.SERVICE_NUMBER != currentUser && a.DLT_STS != true && a.ISPRINTED != true && a.STATUS != true && !cards_for.Contains(a.CARD_FOR_CODE.ToString()) && a.WORKFLOW_RESPO_CODE != null).CountAsync();
                if (other_permits > 0)
                    othertotal = othertotal + other_permits;

            }
            catch
            {
                //ignore

            }




            return othertotal;
        }

        public async Task<int> CountMyPermitsTotal()
        {
            int my = await CountMyPermits();
            int other = await CountOtherPermits();
            int total = my + other;
            return total;
        }
    }
}