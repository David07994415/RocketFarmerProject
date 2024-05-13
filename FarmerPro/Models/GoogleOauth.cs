using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.YouTube.v3;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using Google.Apis.PeopleService.v1;
using Google.Apis.PeopleService.v1.Data;
using Microsoft.Ajax.Utilities;
using static Google.Apis.PeopleService.v1.PeopleResource;

namespace FarmerPro.Models
{
    public class GoogleOauth
    {
        private FarmerProDB db = new FarmerProDB();

        public List<string> RetrieveUserInfor(UserCredential credential)
        {
            try
            {
                // 創建 PeopleService
                var PeopleService = new PeopleServiceService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "user"
                });

                GetRequest peopleRequest = PeopleService.People.Get("people/me");
                peopleRequest.PersonFields = "names,emailAddresses";
                Person me = peopleRequest.Execute();

                List<string> UserInforList = new List<string>();
                if (me != null)
                {
                    if (me.Names[0] != null)
                    {
                        UserInforList.Add(me.Names[0].DisplayName);
                    }
                    if (me.EmailAddresses[0] != null)
                    {
                        UserInforList.Add(me.EmailAddresses[0].Value);
                    }
                }
                return UserInforList;
            }
            catch (Exception ex)
            {
                List<string> catcherror = new List<string>
                {
                    ex.Message+"_"+ex.StackTrace
                };
                return catcherror;
            }
        }

        public User CheckUser(string account)
        {
            var IsRegister = db.Users.Where(x => x.Account == account)?.FirstOrDefault();
            return IsRegister;
        }

    }
}