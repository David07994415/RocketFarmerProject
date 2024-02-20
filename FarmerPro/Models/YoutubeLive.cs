using Google.Apis.Auth.OAuth2;
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
                //new[] { YouTubeService.Scope.Youtube },
                "user",
                System.Threading.CancellationToken.None
                ).Result;

            // 創建 YouTubeService
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "YourApplicationName_TT"
            });

            return youtubeService;
        }

        public string CreateYouTubeBroadcast(string title,string des,DateTime start,DateTime end)
        {
            try
            {

                string broadcastTitle = title;
                string broadcastDescription = des;

               
                //使用OAuth2
                YouTubeService youtubeService = GetYouTubeService();


                // 建立廣播物件
                LiveBroadcast broadcast = new LiveBroadcast();
                broadcast.Snippet = new LiveBroadcastSnippet();
                broadcast.Snippet.Title = broadcastTitle;
                broadcast.Snippet.Description = broadcastDescription;
                broadcast.Status = new LiveBroadcastStatus();
                broadcast.Status.PrivacyStatus = "unlisted";

                // 設定廣播時間
                DateTime startTime = start; 
                string startTimeString = startTime.ToString("o"); // 将 startTime 转换为 ISO 8601 格式的字符串
                DateTimeOffset startTimeOffset = DateTimeOffset.ParseExact(startTimeString, "o", CultureInfo.InvariantCulture);
                broadcast.Snippet.ScheduledStartTimeDateTimeOffset = startTimeOffset;

                DateTime endTime = end;
                string endTimeString = endTime.ToString("o"); // 将 startTime 转换为 ISO 8601 格式的字符串
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
                // 異常處理
                 return ex.Message+ex.StackTrace;
            }
        }

        public string UpdateYouTubeBroadcast(string title, string des, DateTime start, DateTime end,string broadcastIdd)
        {
            try
            {
                string broadcastTitle = title;
                string broadcastDescription = des;

                //使用OAuth2
                YouTubeService youtubeService = GetYouTubeService();



                // 建立廣播物件
                LiveBroadcast broadcast = new LiveBroadcast();
                broadcast.Id = broadcastIdd;
                broadcast.Snippet = new LiveBroadcastSnippet();
                broadcast.Snippet.Title = broadcastTitle;
                broadcast.Snippet.Description = broadcastDescription;
                broadcast.Status = new LiveBroadcastStatus();
                broadcast.Status.PrivacyStatus = "unlisted";

                // 設定廣播時間
                DateTime startTime = start;
                string startTimeString = startTime.ToString("o"); // 将 startTime 转换为 ISO 8601 格式的字符串
                DateTimeOffset startTimeOffset = DateTimeOffset.ParseExact(startTimeString, "o", CultureInfo.InvariantCulture);
                broadcast.Snippet.ScheduledStartTimeDateTimeOffset = startTimeOffset;

                DateTime endTime = end;
                string endTimeString = endTime.ToString("o"); // 将 startTime 转换为 ISO 8601 格式的字符串
                DateTimeOffset endTimeOffset = DateTimeOffset.ParseExact(endTimeString, "o", CultureInfo.InvariantCulture);
                broadcast.Snippet.ScheduledEndTimeDateTimeOffset = endTimeOffset;



                // 執行更新操作
                LiveBroadcast updatedBroadcast = youtubeService.LiveBroadcasts.Update(broadcast, "snippet,status").Execute();
                // 獲得直播ID
                string broadcastId = updatedBroadcast.Id;
                if (broadcastId != null)
                {
                    return broadcastId;
                }
                else { return "error"; }

            }
            catch (Exception ex)
            {
                // 異常處理
                return "error";
            }
        }









    }
}