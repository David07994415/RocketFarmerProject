using FarmerPro.Models;
using Jose;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.UI.WebControls;

namespace FarmerPro
{
    public class chathub : Hub
    {
        public string Hello()
        {
            return "Hello, This message from BACKEND Server!";
        }
        private readonly HttpClient _httpClient;

        public static Dictionary<string, int> _groupUserCounts = new Dictionary<string, int>();
        
        public chathub()
        {
            _httpClient = new HttpClient();   
            _httpClient.BaseAddress = new Uri(WebConfigurationManager.AppSettings["Serverurl"].ToString());
        }

        public async Task<object> SendMessageToApi(int chatroomId, int userIdSender, string message)
        {
            var postData = new
            {
                chatroomId,
                userIdSender,
                message,
            };
            // 將資料序列化為 JSON 字串
            var jsonContent = JsonConvert.SerializeObject(postData);
            // 發送 POST 請求到 Web API
            var response = await _httpClient.PostAsync("api/chats/roommessages", new StringContent(jsonContent, Encoding.UTF8, "application/json"));
            // 檢查是否成功
            if (response.IsSuccessStatusCode)
            {
                // 讀取成功的回應資料
                string responseBody = await response.Content.ReadAsStringAsync();
                // 將回應資料作為 JSON 物件格式返回給前端
                var data = JsonConvert.DeserializeObject(responseBody); //將字串解析成JSON物件
                return data;
            }
            else
            {
                // 讀取失敗的回應資料
                string responseBody = await response.Content.ReadAsStringAsync();
                // 將回應資料作為 JSON 物件格式返回給前端
                var data = JsonConvert.DeserializeObject(responseBody); //將字串解析成JSON物件
                return data;
            }
        }

        public async Task<string> JoinChatRoom(int chatroomId)
        {
            // 將連接客戶加入指定的聊天室
            await Groups.Add(Context.ConnectionId, chatroomId.ToString());
            return $"Return by backend：client join for chatroom-{chatroomId}";
        }

        public async Task<string> SendMessageToRoom(int chatroomId, int userIdSender, string message)
        {
            try
            { 
                var groupClient = await Clients.Group(chatroomId.ToString()).receiveMessage(await SendMessageToApi(chatroomId, userIdSender, message));
                if (groupClient == null)
                {
                    return "Successfully send message to 1-1 room";
                }
                else
                {
                    return "Successfully set up client for group:";
                }
            }
            catch (Exception ex)
            {
                return "Error sending message to group" + ex.Message;
            }
        }

        public async Task<string> JoinLiveRoom(string liveroomstring)
        {
            // 將連接客戶加入指定的聊天室
            await  Groups.Add(Context.ConnectionId, liveroomstring);
            bool GroupinDictory = false; // For 計算人數用途
            if (_groupUserCounts.ContainsKey(liveroomstring)) 
            { 
                GroupinDictory = true; 
            }
            if(GroupinDictory==false)
            {
                _groupUserCounts.Add(liveroomstring, 0);
            }
            _groupUserCounts[liveroomstring] += 1;
            int peoplecount = _groupUserCounts[liveroomstring];
            await Clients.Group(liveroomstring.ToString()).receivePeople($"{peoplecount}");
            return $"Return by backend：client join for chatroom-{liveroomstring}";
        }

        public async Task<string> LeftLiveRoom(string liveroomstring)
        {
            // 將連接客戶加入指定的聊天室
            await Groups.Remove(Context.ConnectionId, liveroomstring);
            _groupUserCounts[liveroomstring] -= 1; //計算人數
            int peoplecount = _groupUserCounts[liveroomstring];
            await Clients.Group(liveroomstring.ToString()).receivePeople($"{peoplecount}");
            return $"Return by backend：client join for chatroom-{liveroomstring}";
        }

        public async Task<string> SendMessageToLiveRoom(string liveroomstring, int userIdSender, string nickName, string photo, string message)
        {
            try
            {
                var postData = new
                {
                    userIdSender,
                    nickName,
                    photo,
                    message
                };
                var groupClient = await Clients.Group(liveroomstring).receiveLiveMessage(postData);
                if (groupClient == null)
                {
                    return "Successfully send message to liveroom";
                }
                else
                {
                    return "Successfully set up client for group:";
                }
            }
            catch (Exception ex)
            {
                return "Error sending message to group" + ex.Message;
            }
        }

        public void AddintoSocket(int userId)
        {
            try
            {
                string connectionId = Context.ConnectionId;
                bool hasUser = false;
                if (GlobalVariable._userList.ContainsKey(userId.ToString())) //如果有使用者ID，把connectionId更新
                { 
                    hasUser = true; 
                    GlobalVariable._userList[userId.ToString()] = connectionId; 
                };
                if (hasUser == false)  //如果沒有使用者ID，把connectionId加入
                { 
                    GlobalVariable._userList.Add(userId.ToString(), connectionId); 
                }
            }
            catch(Exception ex)
            {
                Clients.All.notifyMessage(ex.Message);
            }
        }
    }
}