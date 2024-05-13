using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.AspNet.SignalR.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using static Google.Apis.Requests.BatchRequest;

namespace FarmerPro.Models
{
    public class YoutubeLive
    {

        public static YouTubeService GetYouTubeService()
        {
            string[] scopes = { "https://www.googleapis.com/auth/youtube" };
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

            // 創建 YouTubeService 物件
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "YourApplicationName_TT"
            });

            return youtubeService;
        }

        public string CreateYouTubeBroadcast(UserCredential credential, string title, string des, DateTime start, DateTime end)
        {
            try
            {
                // 創建 YouTubeService 物件
                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "YourApplicationName_TT"
                });

                string broadcastTitle = title;
                string broadcastDescription = des;

                // 建立廣播物件
                LiveBroadcast broadcast = new LiveBroadcast();
                broadcast.Snippet = new LiveBroadcastSnippet();
                broadcast.Snippet.Title = broadcastTitle;
                broadcast.Snippet.Description = broadcastDescription;
                broadcast.Status = new LiveBroadcastStatus();
                broadcast.Status.PrivacyStatus = "unlisted";

                // 設定廣播時間
                DateTime startTime = start;
                string startTimeString = startTime.ToString("o");
                DateTimeOffset startTimeOffset = DateTimeOffset.ParseExact(startTimeString, "o", CultureInfo.InvariantCulture);
                broadcast.Snippet.ScheduledStartTimeDateTimeOffset = startTimeOffset;

                DateTime endTime = end;
                string endTimeString = endTime.ToString("o");
                DateTimeOffset endTimeOffset = DateTimeOffset.ParseExact(endTimeString, "o", CultureInfo.InvariantCulture);
                broadcast.Snippet.ScheduledEndTimeDateTimeOffset = endTimeOffset;

                // 創建直播
                LiveBroadcast createdBroadcast = youtubeService.LiveBroadcasts.Insert(broadcast, "snippet,status").Execute();
                // 獲得直播ID
                string broadcastId = createdBroadcast.Id;
                if (broadcastId != null)
                {
                    return broadcastId;
                }
                else { return "error"; }

            }
            catch (Exception ex)
            {
                return ex.Message + ex.StackTrace;
            }
        }

        public UserCredential CreateToken(string tokeninput)
        {
            // 建立 Token
            TokenResponse token = new TokenResponse();
            token.AccessToken = tokeninput;
            token.Scope = @"https://www.googleapis.com/auth/youtube";
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

            // 取得 credential 資料
            var credential = new UserCredential(flow, "user", token);

            return credential;
        }
    }
}