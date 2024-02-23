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
            //Clients.All.hello();
            return "Hello, This message from BACKEND Server!";
        }
        private readonly HttpClient _httpClient;
        //private readonly IConnectionManager _connectionManager;

       
        public static Dictionary<string, int> _groupUserCounts = new Dictionary<string, int>();
        

        public chathub()
        {
            _httpClient = new HttpClient();   //要調整成domain
            _httpClient.BaseAddress = new Uri(WebConfigurationManager.AppSettings["Serverurl"].ToString());
            //_connectionManager = GlobalHost.ConnectionManager;
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
                // 將回應資料傳送給前端
                //return JsonConvert.DeserializeObject<string>(responseBody); //將字串解析成JSON =>會出錯
                //return responseBody;

                // 將回應資料作為 JSON 物件格式返回給前端
                var data = JsonConvert.DeserializeObject(responseBody); //將字串解析成JSON物件
                return data;

            }
            else
            {
                // 讀取失敗的回應資料
                string responseBody = await response.Content.ReadAsStringAsync();
                // 將回應資料傳送給前端
                //return JsonConvert.DeserializeObject<string>(responseBody);
                //return responseBody;

                // 將回應資料作為 JSON 物件格式返回給前端
                var data = JsonConvert.DeserializeObject(responseBody); //將字串解析成JSON物件
                return data;

            }
        }



        public async Task<string> JoinChatRoom(int chatroomId)
        {

            // 將連接客戶加入指定的聊天室
            await Groups.Add(Context.ConnectionId, chatroomId.ToString());
            //var connection = await Clients.Group(chatroomId.ToString());
            //var connectionIds = new List<string>(connection);
            //int count = connectionIds.Count();
            //await Clients.Group(chatroomId.ToString()).receivePeople($"backend message，people in room are:{count}");
            return $"Return by backend：client join for chatroom-{chatroomId}";

        }

        public async Task<string> SendMessageToRoom(int chatroomId, int userIdSender, string message)
        {
            try
            { 
                var groupClient = await Clients.Group(chatroomId.ToString()).receiveMessage(await SendMessageToApi(chatroomId, userIdSender, message));
                //string returnbody=await SendMessageToApi(chatroomId, userIdSender, message); //先關閉
                if (groupClient == null)
                {
                    return "Successfully send message to 1-1 room";
                    //return returnbody;
                    //return $"Return by backend：client join for chatroom-{chatroomId}，sending" + message;
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

        // 加入Live聊天室
        public async Task<string> JoinLiveRoom(string liveroomstring)
        {
            // 將連接客戶加入指定的聊天室
            await  Groups.Add(Context.ConnectionId, liveroomstring);
            bool GroupinDictory = false; //計算人數
            if (_groupUserCounts.ContainsKey(liveroomstring)) 
            { GroupinDictory = true; }
            if(GroupinDictory==false)
            {
                _groupUserCounts.Add(liveroomstring, 0);
            }
            _groupUserCounts[liveroomstring] += 1;
            int peoplecount = _groupUserCounts[liveroomstring];

            //var connections = _connectionManager.GetHubContext<chathub>().Clients.Group(liveroomstring);
            //int count = 0;

            //foreach (var connection in connections)
            //{
            //    count++;
            //}

            //var connection = await Clients.Group(liveroomstring.ToString());
            //var connectionIds = new List<string>();
            //foreach (var connId in connection)
            //{
            //    connectionIds.Add(connId.ToString());
            //}
            //int count = connectionIds.Count();
            
           
            await Clients.Group(liveroomstring.ToString()).receivePeople($"{peoplecount}");
            return $"Return by backend：client join for chatroom-{liveroomstring}";
        }

        public async Task<string> LeftLiveRoom(string liveroomstring)
        {
            // 將連接客戶加入指定的聊天室
            await Groups.Remove(Context.ConnectionId, liveroomstring);
            _groupUserCounts[liveroomstring] -= 1; //計算人數
            int peoplecount = _groupUserCounts[liveroomstring];
            //var connections = _connectionManager.GetHubContext<chathub>().Clients.Group(liveroomstring);
            //int count = 0;

            //foreach (var connection in connections)
            //{
            //    count++;
            //}

            //var connection = await Clients.Group(liveroomstring.ToString());
            //var connectionIds = new List<string>();
            //foreach (var connId in connection)
            //{
            //    connectionIds.Add(connId.ToString());
            //}
            //int count = connectionIds.Count();

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


                var groupClient = await Clients.Group(liveroomstring).receiveMessage(postData);
                //string returnbody=await SendMessageToApi(chatroomId, userIdSender, message); //先關閉
                if (groupClient == null)
                {
                    return "Successfully send message to liveroom";
                    //return returnbody;
                    //return $"Return by backend：client join for chatroom-{chatroomId}，sending" + message;
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
        //public async Task<object> SendMessageToLivApi(int userId ,string message)
        //{
        //    var postData = new
        //    {
        //        message
        //    };

        //    // 將資料序列化為 JSON 字串
        //    var jsonContent = JsonConvert.SerializeObject(postData);

        //    // 發送 POST 請求到 Web API
        //    var response = await _httpClient.PostAsync("api/chats/roommessages", new StringContent(jsonContent, Encoding.UTF8, "application/json"));

        //    // 檢查是否成功
        //    if (response.IsSuccessStatusCode)
        //    {
        //        // 讀取成功的回應資料
        //        string responseBody = await response.Content.ReadAsStringAsync();
        //        // 將回應資料傳送給前端
        //        //return JsonConvert.DeserializeObject<string>(responseBody); //將字串解析成JSON =>會出錯
        //        //return responseBody;

        //        // 將回應資料作為 JSON 物件格式返回給前端
        //        var data = JsonConvert.DeserializeObject(responseBody); //將字串解析成JSON物件
        //        return data;

        //    }
        //    else
        //    {
        //        // 讀取失敗的回應資料
        //        string responseBody = await response.Content.ReadAsStringAsync();
        //        // 將回應資料傳送給前端
        //        //return JsonConvert.DeserializeObject<string>(responseBody);
        //        //return responseBody;

        //        // 將回應資料作為 JSON 物件格式返回給前端
        //        var data = JsonConvert.DeserializeObject(responseBody); //將字串解析成JSON物件
        //        return data;

        //    }
        //}
        public void AddintoSocket(int userId)
        {
            string connectionId = Context.ConnectionId;
            bool hasUser = false;
            if (GlobalVariable._userList.ContainsKey(userId.ToString())) //如果有使用者ID，把connectionId更新
            { hasUser = true; GlobalVariable._userList[userId.ToString()] = connectionId; };
            if (hasUser == false) { GlobalVariable._userList.Add(userId.ToString(), connectionId); } //如果沒有使用者ID，把connectionId加入
        }

        //新增
        //var hub = GlobalHost.ConnectionManager.GetHubContext<chathub>();





    }



}