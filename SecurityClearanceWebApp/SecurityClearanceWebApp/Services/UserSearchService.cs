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
    public enum Usersearch
    {
        ServiceNumber,
        
      
    }
    public class UserSearchService
    {

        private RA42_SECURITY_CLEARANCEEntities db = new RA42_SECURITY_CLEARANCEEntities();
        public UserSearchService() { }

        public async Task<List<UserSearchResult>> Search(string serviceNumber)
        {
            var resulat = new List<UserSearchResult>();

            try
            {

                var userRespo = await db.RA42_WORKFLOW_RESPONSIBLE_MST.Where(a => a.SERVICE_NUMBER == serviceNumber && a.ACTIVE != false && a.DLT_STS != true).ToListAsync();
                var USER_RESPO = userRespo.Select(r => new UserSearchResult
                {
                    WORKFLOW_RESPO_CODE = r.WORKFLOW_RESPO_CODE,
                    STATION_CODE = r.STATION_CODE,
                    RESPONSEPLE_NAME = r.RESPONSEPLE_NAME,
                    RESPONSEPLE_NAME_E = r.RESPONSEPLE_NAME_E,
                    RANK = r.RANK,
                    RANK_E = r.RANK_E,
                    WORKFLOWID = r.WORKFLOWID,
                    AD = r.AD,
                    UP = r.UP,
                    DL = r.DL,
                    VW = r.VW,
                    RP = r.RP,
                    DEVELOPER = r.DEVELOPER,
                    ADMIN = r.ADMIN,
                    SETTINGS = r.SETTINGS,
                    UNIT_NAME = r.UNIT_NAME,
                    UNIT_NAME_E = r.UNIT_NAME_E,
                    CAMP_NAME = r.RA42_STATIONS_MST.STATION_NAME_A,
                    CAMP_NAME_E = r.RA42_STATIONS_MST.STATION_NAME_E,
                    FORCE_NAME = r.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_NAME_A,
                    FORCE_NAME_E = r.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_NAME_E,
                    FORCE_TYPE = r.RA42_STATIONS_MST.RA42_FORCES_MST.FORCE_ID,


                }).ToList();
                resulat.AddRange(USER_RESPO);

            }
            catch
            {
                //ignore
                
            }

           
           

            return resulat;
        }
    }
}