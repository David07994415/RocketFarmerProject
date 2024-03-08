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

namespace FarmerPro.Models
{
    public class GoogleOauth
    {
        private FarmerProDB db = new FarmerProDB();
        // https://developers.google.com/identity/protocols/oauth2/scopes?hl=zh-tw#people

        public static PeopleServiceService GetPeopleService()
        {

            string[] scopes = { "https://www.googleapis.com/auth/userinfo.email", "https://www.googleapis.com/auth/userinfo.profile" };
            UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = WebConfigurationManager.AppSettings["ytid"].ToString(),
                    ClientSecret = WebConfigurationManager.AppSettings["ytkey"].ToString(),
                },
                scopes,
                "user",
                System.Threading.CancellationToken.None
                ).Result;

            // 創建 PeopleService
            var PeopleServiceObj = new PeopleServiceService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Google People Infor APIs"
            });

            return PeopleServiceObj;
        }

        public List<string> RetrieveUserInfor(UserCredential credential)
        {
            try
            {
                //tokenResponse.Scope= 從這裡開始
                // 創建 YouTubeService
                var PeopleService = new PeopleServiceService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Google People Infor APIs"
                });


                Person me = PeopleService.People.Get("people/me").Execute();

                List<string> UserInforList=new List<string>();
                if (me != null) 
                {
                    UserInforList.Add(me.Names[0].DisplayName);
                    UserInforList.Add(me.EmailAddresses[0].Value);
                }

                return UserInforList;
            }
            catch (Exception ex) 
            {
                List<string> catcherror = new List<string>
                {
                    ex.Message
                };
                return catcherror;
            }
        }

        public UserCredential CreateToken(string tokeninput)
        {
            TokenResponse token = new TokenResponse();
            token.AccessToken = tokeninput;
            token.Scope = @"https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/userinfo.profile";
            token.ExpiresInSeconds = 3584;
            token.Issued = DateTime.Now;
            token.IssuedUtc = DateTime.UtcNow;


            var clientSecrets = new ClientSecrets
            {
                ClientId = WebConfigurationManager.AppSettings["ytid"].ToString(),
                ClientSecret = WebConfigurationManager.AppSettings["ytkey"].ToString()
            };


            // 建立授權資料流
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets
            });

            // 将 TokenResponse 转换为 UserCredential
            var credential = new UserCredential(flow, "user", token);
            return credential;
        }

        public User CheckUser(string account) 
        {
            var IsRegister = db.Users.Where(x=> x.Account == account)?.FirstOrDefault();
            return IsRegister;

        }

        

    }
}