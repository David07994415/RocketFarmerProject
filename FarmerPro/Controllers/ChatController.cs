using FarmerPro.Models;
using FarmerPro.Securities;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;
using MimeKit;
using NSwag.Annotations;
using Org.BouncyCastle.Tls;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace FarmerPro.Controllers
{
    [OpenApiTag("Chat", Description = "聊天室")]
    public class ChatController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        //已經測試性修正，需要把ID改回來

        #region FCW-01 創造並加入特定聊天室(取得聊天訊息)

        /// <summary>
        /// FCW-01 創造並加入特定聊天室(取得聊天訊息)
        /// </summary>
        /// <param name="input">訊息接收者Id回傳資料</param>
        /// <returns>返回聊天室的 JSON 物件</returns>
        [HttpPost]
        //[Route("api/chats/joinroom/{senderId}/{receiverId}")] //測試用途
        [Route("api/chats/joinroom")] //正式用途
        [JwtAuthFilter]
        public IHttpActionResult chatroomjoin(userChatroomcheck input) //(userChatroomcheck input) //正式用途
        {                                                          //測試用途(int senderId, int receiverId)
            //以下三行為正式用途
            var jwtObject = JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter);
            int senderId = (int)jwtObject["Id"];
            int receiverId = input.receiverId;

            var sendercategorycheck = db.Users.Where(x => x.Id == senderId)?.FirstOrDefault();
            bool senderIsUser = false;
            if (sendercategorycheck != null)
            {
                if (sendercategorycheck.Category == UserCategory.一般會員) { senderIsUser = true; }
            }
            if (senderIsUser == true) //使用者為一般會員=>senderId，小農=>receiverId
            {
                var HaveRoom = db.ChatRooms.
                Where(room => room.UserIdOwner == senderId && room.UserIdTalker == receiverId)?.FirstOrDefault();
                int roomId;
                if (HaveRoom == null)
                {
                    var newRoomSet = new ChatRoom
                    {
                        UserIdOwner = senderId,
                        UserIdTalker = receiverId,
                    };
                    // 將 newaccount 加入 User 集合
                    db.ChatRooms.Add(newRoomSet);
                    // 執行資料庫儲存變更操作
                    db.SaveChanges();
                    roomId = newRoomSet.Id;
                }
                else
                {
                    roomId = HaveRoom.Id;
                }
                var messageReturn = db.Records.Where(x => x.ChatRoomId == roomId).AsEnumerable();

                var result = new
                {
                    statusCode = 200,
                    status = "success",
                    message = "房間建立成功",
                    chatroomId = roomId,
                    currentUserId = senderId,
                    farmName = db.Users.Where(x => x.Id == receiverId)?.FirstOrDefault()?.NickName == null ?
                                    db.Users.Where(x => x.Id == receiverId)?.FirstOrDefault()?.Account.Substring(0, 2)
                                 : db.Users.Where(x => x.Id == receiverId)?.FirstOrDefault()?.NickName,
                    farmPhoto = db.Users.Where(x => x.Id == receiverId)?.FirstOrDefault()?.Photo == null ? null
                                 : db.Users.Where(x => x.Id == receiverId)?.FirstOrDefault()?.Photo,
                    chatcontent = messageReturn?.Select(x => new
                    {
                        senderId = x.UserIdSender,
                        senderNickName = db.Users.Where(y => y.Id == x.UserIdSender).FirstOrDefault()?.NickName == null ? "匿名"
                                                        : db.Users.Where(y => y.Id == x.UserIdSender).FirstOrDefault()?.NickName,
                        senderPhoto = db.Users.Where(y => y.Id == x.UserIdSender).FirstOrDefault()?.Photo == null ? null
                                                        : db.Users.Where(y => y.Id == x.UserIdSender).FirstOrDefault()?.Photo,
                        message = x.Message,

                        sendDate = x.CreatTime.Date.ToString("MM/dd"),
                        sendTime = x.CreatTime.ToString("HH:mm"),
                        //isRead=x.IsRead,
                    }).ToList()
                };

                //將小農傳送給使用者的訊息變成已讀狀態
                var ChageMessageRead = db.Records.Where(x => x.ChatRoomId == roomId && x.UserIdSender == receiverId).AsEnumerable();
                foreach (var item in ChageMessageRead)
                {
                    item.IsRead = true;
                }
                db.SaveChanges();

                return Content(HttpStatusCode.OK, result);
            }
            else //使用者為小農  senderId=>小農，receiverId=>一般使用者
            {
                var HaveRoom = db.ChatRooms.
               Where(room => room.UserIdOwner == receiverId && room.UserIdTalker == senderId)?.FirstOrDefault();
                int roomId;
                if (HaveRoom == null)
                {
                    var newRoomSet = new ChatRoom
                    {
                        UserIdOwner = receiverId,
                        UserIdTalker = senderId,
                    };
                    // 將 newaccount 加入 User 集合
                    db.ChatRooms.Add(newRoomSet);
                    // 執行資料庫儲存變更操作
                    db.SaveChanges();
                    roomId = newRoomSet.Id;
                }
                else
                {
                    roomId = HaveRoom.Id;
                }
                var messageReturn = db.Records.Where(x => x.ChatRoomId == roomId).AsEnumerable();

                var result = new
                {
                    statusCode = 200,
                    status = "success",
                    message = "房間建立成功",
                    chatroomId = roomId,
                    currentUserId = senderId,
                    userName = db.Users.Where(x => x.Id == receiverId)?.FirstOrDefault()?.NickName == null ?
                                    db.Users.Where(x => x.Id == receiverId)?.FirstOrDefault()?.Account.Substring(0, 2)
                                 : db.Users.Where(x => x.Id == receiverId)?.FirstOrDefault()?.NickName,
                    userPhoto = db.Users.Where(x => x.Id == receiverId)?.FirstOrDefault()?.Photo == null ? null
                                 : db.Users.Where(x => x.Id == receiverId)?.FirstOrDefault()?.Photo,
                    chatcontent = messageReturn?.Select(x => new
                    {
                        senderId = x.UserIdSender,
                        senderNickName = db.Users.Where(y => y.Id == x.UserIdSender).FirstOrDefault()?.NickName == null ? "匿名"
                                                        : db.Users.Where(y => y.Id == x.UserIdSender).FirstOrDefault()?.NickName,
                        senderPhoto = db.Users.Where(y => y.Id == x.UserIdSender).FirstOrDefault()?.Photo == null ? null
                                                        : db.Users.Where(y => y.Id == x.UserIdSender).FirstOrDefault()?.Photo,
                        message = x.Message,
                        sendDate = x.CreatTime.Date.ToString("MM/dd"),
                        sendTime = x.CreatTime.ToString("HH:mm"),
                        //readStatus=x.IsRead,
                    }).ToList()
                };
                //將使用者傳送給小農的訊息變成已讀狀態
                var ChageMessageRead = db.Records.Where(x => x.ChatRoomId == roomId && x.UserIdSender == receiverId).AsEnumerable();
                foreach (var item in ChageMessageRead)
                {
                    item.IsRead = true;
                }
                db.SaveChanges();

                return Content(HttpStatusCode.OK, result);
            }
        }

        #endregion FCW-01 創造並加入特定聊天室(取得聊天訊息)

        #region FCW-02 傳送並存取聊天資訊

        /// <summary>
        /// FCW-02 傳送並存取聊天資訊
        /// </summary>
        /// <param name="input">聊天室Id、訊息發送者Id、訊息內容回傳資料</param>
        /// <returns>返回聊天室的 JSON 物件</returns>
        [HttpPost]
        [Route("api/chats/roommessages")]
        public async Task<IHttpActionResult> chatsendmessage(ChatSendMessageCheck input)
        {
            //var jwtObject = JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter);
            //int senderId = (int)jwtObject["Id"];
            string categlory = "";
            if (db.Users.Where(x => x.Id == input.userIdSender).FirstOrDefault().Category == UserCategory.小農) { categlory = "小農"; }
            else { categlory = "會員"; };

            var CheckChatRoom = db.ChatRooms.Where(room => room.Id == input.chatroomId)?.FirstOrDefault();
            if (CheckChatRoom == null)
            {
                var result = new
                {
                    statusCode = 400,
                    status = "error",
                    message = "聊天室不存在"
                };
                return Content(HttpStatusCode.OK, result);
            }
            else
            {
                var newRoomSet = new Record
                {
                    UserIdSender = input.userIdSender,
                    Message = input.message,
                    ChatRoomId = input.chatroomId,
                    IsRead = false
                };
                // 將 newaccount 加入 User 集合
                db.Records.Add(newRoomSet);
                // 執行資料庫儲存變更操作
                db.SaveChanges();

                ////進行WS的message傳送
                var hubsocket = GlobalHost.ConnectionManager.GetHubContext<chathub>();
                //await hubsocket.Clients.All.receiveMessage(hubsocket);
                if (categlory == "小農")
                {
                    int id = db.ChatRooms.Where(x => x.Id == input.chatroomId).FirstOrDefault().UserIdOwner;
                    string connectionId = "";
                    if (GlobalVariable._userList.ContainsKey(id.ToString()))
                    {
                        connectionId = GlobalVariable._userList[id.ToString()];
                        //hubsocket.Clients.Client(connectionId).SendAsync("notifyMessage", "您有未讀訊息");
                        await hubsocket.Clients.Client(connectionId).notifyMessage("您有未讀訊息");
                        //await hubsocket.Clients.Client(connectionId).notifyMessage($"userId為:{GlobalVariable._userList[id.ToString()]}，socketId為:{connectionId}");
                    }
                }
                else //使用者是一般用戶
                {
                    int id = db.ChatRooms.Where(x => x.Id == input.chatroomId).FirstOrDefault().UserIdTalker;
                    string connectionId = "";
                    if (GlobalVariable._userList.ContainsKey(id.ToString()))
                    {
                        connectionId = GlobalVariable._userList[id.ToString()];
                        //hubsocket.Clients.Client(connectionId).SendAsync("notifyMessage", "您有未讀訊息");
                        await hubsocket.Clients.Client(connectionId).notifyMessage("您有未讀訊息");
                        // await hubsocket.Clients.Client(connectionId).notifyMessage($"userId為:{GlobalVariable._userList[id.ToString()]}，socketId為:{connectionId}");
                    }
                };

                var messageReturn = db.Records.Where(x => x.ChatRoomId == input.chatroomId).AsEnumerable();

                var result = new
                {
                    statusCode = 200,
                    status = "success",
                    message = "訊息傳送成功",
                    chatcontent = messageReturn?.Select(x => new
                    {
                        senderId = x.UserIdSender,
                        senderNickName = db.Users.Where(y => y.Id == x.UserIdSender).FirstOrDefault()?.NickName == null ? "匿名"
                                                         : db.Users.Where(y => y.Id == x.UserIdSender).FirstOrDefault()?.NickName,
                        senderPhoto = db.Users.Where(y => y.Id == x.UserIdSender).FirstOrDefault()?.Photo == null ? null
                                                        : db.Users.Where(y => y.Id == x.UserIdSender).FirstOrDefault()?.Photo,
                        message = x.Message,
                        sendDate = x.CreatTime.Date.ToString("MM/dd"),
                        sendTime = x.CreatTime.ToString("HH:mm"),
                        isRead = x.IsRead,
                    }).ToList()
                };
                return Content(HttpStatusCode.OK, result);
            }
        }

        #endregion FCW-02 傳送並存取聊天資訊

        #region FCW-03 取得聊天室清單(要回傳小農ID給前端)

        /// <summary>
        /// FCW-03 取得聊天室清單(要回傳小農ID給前端)
        /// </summary>
        /// <param></param>
        /// <returns>返回聊天室的 JSON 物件</returns>
        [HttpGet]
        [Route("api/chats/roomlist")]
        [JwtAuthFilter]
        public IHttpActionResult getchatlist()
        {
            var jwtObject = JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter);
            int userId = (int)jwtObject["Id"];
            var usercheck = db.Users.Where(x => x.Id == userId)?.FirstOrDefault();
            if (usercheck == null)
            {
                var result = new
                {
                    statusCode = 401,
                    status = "error",
                    message = "使用者不存在",
                };
                return Content(HttpStatusCode.OK, result);
            }
            else
            {
                if (usercheck.Category == UserCategory.小農) //使用者為小農
                {
                    var checklist = db.ChatRooms.Where(x => x.UserIdTalker == userId)?.AsEnumerable();
                    if (checklist != null)
                    {
                        var result = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "取得成功(用戶list)",
                            chatList = checklist.Select(chatroom => new
                            {
                                chatroomId = chatroom.Id,
                                userId = db.Users.Where(y => y.Id == chatroom.UserIdOwner)?.FirstOrDefault()?.Id,
                                userNickName = db.Users.Where(y => y.Id == chatroom.UserIdOwner)?.FirstOrDefault()?.NickName == null ?
                                        db.Users.Where(y => y.Id == chatroom.UserIdOwner)?.FirstOrDefault()?.Account?.Substring(0, 2) :
                                        db.Users.Where(y => y.Id == chatroom.UserIdOwner)?.FirstOrDefault()?.NickName,
                                userPhoto = db.Users.Where(y => y.Id == chatroom.UserIdOwner)?.FirstOrDefault()?.Photo == null ?
                                        null :
                                        db.Users.Where(y => y.Id == chatroom.UserIdOwner)?.FirstOrDefault()?.Photo,
                                lastMessageDate = db.Records.Where(y => y.ChatRoomId == chatroom.Id)?.AsEnumerable().LastOrDefault()?.CreatTime.ToString("yyyy/MM/dd"),
                                isRead = db.Records.Where(y => y.ChatRoomId == chatroom.Id && y.UserIdSender != userId && y.IsRead == false)?.FirstOrDefault() == null ? true : false,
                            }).ToList(),
                        };
                        return Content(HttpStatusCode.OK, result);
                    }
                    else
                    {
                        var result = new
                        {
                            statusCode = 402,
                            status = "error",
                            message = "聊天室不存在",
                        };
                        return Content(HttpStatusCode.OK, result);
                    }
                }
                else //使用者為客戶
                {
                    var checklist = db.ChatRooms.Where(x => x.UserIdOwner == userId)?.AsEnumerable();
                    if (checklist != null)
                    {
                        var result = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "取得成功(小農list)",
                            chatList = checklist.Select(chatroom => new
                            {
                                chatroomId = chatroom.Id,
                                farmerId = db.Users.Where(y => y.Id == chatroom.UserIdTalker)?.FirstOrDefault()?.Id,
                                famrerNickName = db.Users.Where(y => y.Id == chatroom.UserIdTalker)?.FirstOrDefault()?.NickName == null ?
                                        db.Users.Where(y => y.Id == chatroom.UserIdTalker)?.FirstOrDefault()?.Account?.Substring(0, 2) :
                                        db.Users.Where(y => y.Id == chatroom.UserIdTalker)?.FirstOrDefault()?.NickName,
                                famrerPhoto = db.Users.Where(y => y.Id == chatroom.UserIdTalker)?.FirstOrDefault()?.Photo == null ?
                                        null :
                                        db.Users.Where(y => y.Id == chatroom.UserIdTalker)?.FirstOrDefault()?.Photo,
                                lastMessageDate = db.Records.Where(y => y.ChatRoomId == chatroom.Id)?.AsEnumerable().LastOrDefault()?.CreatTime.ToString("yyyy/MM/dd"),
                                isRead = db.Records.Where(y => y.ChatRoomId == chatroom.Id && y.UserIdSender != userId && y.IsRead == false)?.FirstOrDefault() == null ? true : false
                            }).ToList(),
                        };
                        return Content(HttpStatusCode.OK, result);
                    }
                    else
                    {
                        var result = new
                        {
                            statusCode = 402,
                            status = "error",
                            message = "聊天室不存在",
                        };
                        return Content(HttpStatusCode.OK, result);
                    }
                }
            }
        }

        #endregion FCW-03 取得聊天室清單(要回傳小農ID給前端)

        //#region FCW-04 傳送單筆直播聊天資訊

        //[HttpPost]
        //[Route("api/chats/livemessages")]
        //[JwtAuthFilter]
        //public IHttpActionResult livechatsendmessage(LiveMessagecheck input)
        //{
        //    var jwtObject = JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter);
        //    int senderId = (int)jwtObject["Id"];
        //    if (!ModelState.IsValid)
        //    {
        //        var result = new
        //        {
        //            statusCode = 401,
        //            status = "error",
        //            message = "欄位格式不正確，請重新輸入",
        //        };
        //        return Content(HttpStatusCode.OK, result);
        //    }
        //    else
        //    {
        //        var userinfor = db.Users.Where(x => x.Id == senderId)?.FirstOrDefault();
        //        if (userinfor == null)
        //        {
        //            var result = new
        //            {
        //                statusCode = 402,
        //                status = "error",
        //                message = "使用者不存在",
        //            };
        //            return Content(HttpStatusCode.OK, result);
        //        }
        //        else
        //        {
        //            var result = new
        //            {
        //                senderName = userinfor.NickName == null ? userinfor.Account.Substring(0, 2) : userinfor.NickName,
        //                senderPhoto = userinfor?.Photo,
        //                message = input.message
        //            };
        //             return Content(HttpStatusCode.OK, result);
        //        }
        //    }
        //}
        //#endregion

        #region FCW-05 取得使用者聊天室個人資訊

        /// <summary>
        /// FCW-05 取得使用者聊天室個人資訊
        /// </summary>
        /// <param></param>
        /// <returns>返回使用者個人資訊 JSON 物件</returns>
        [HttpGet]
        [Route("api/chats/live/check")]
        [JwtAuthFilter]
        public IHttpActionResult checkuser()
        {
            var jwtObject = JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter);
            int senderId = (int)jwtObject["Id"];

            var userinfor = db.Users.Where(x => x.Id == senderId)?.FirstOrDefault();
            if (userinfor == null)
            {
                var result = new
                {
                    statusCode = 401,
                    status = "error",
                    message = "使用者不存在",
                };
                return Content(HttpStatusCode.OK, result);
            }
            else
            {
                var result = new
                {
                    statusCode = 200,
                    status = "success",
                    message = "取得成功",
                    data = new
                    {
                        senderId = userinfor.Id,
                        senderName = userinfor.NickName == null ? userinfor.Account.Substring(0, 2) : userinfor.NickName,
                        senderPhoto = userinfor?.Photo,
                    }
                };
                return Content(HttpStatusCode.OK, result);
            }
        }

        #endregion FCW-05 取得使用者聊天室個人資訊

        #region FCW-07 取得單一使用者未讀訊息通知

        /// <summary>
        /// FCW-05 取得使用者聊天室個人資訊
        /// </summary>
        /// <param></param>
        /// <returns>返回未讀通知</returns>
        [HttpGet]
        [Route("api/chats/roomlist/notify")]
        [JwtAuthFilter]
        public IHttpActionResult getchatlistnotify()
        {
            var jwtObject = JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter);
            int userId = (int)jwtObject["Id"];
            var usercheck = db.Users.Where(x => x.Id == userId)?.FirstOrDefault();
            if (usercheck == null)
            {
                var result = new
                {
                    statusCode = 401,
                    status = "error",
                    message = "使用者不存在",
                };
                return Content(HttpStatusCode.OK, result);
            }
            else
            {
                if (usercheck.Category == UserCategory.小農) //使用者為小農
                {
                    var checklist = db.ChatRooms.Where(x => x.UserIdTalker == userId)?.ToList();
                    if (checklist != null)
                    {
                        bool HaveUnReadMessage = false;
                        foreach (var chats in checklist)
                        {
                            if (chats.Record != null)
                            {
                                foreach (var talks in chats.Record)
                                    if (talks.UserIdSender != userId && talks.IsRead == false)
                                    {
                                        HaveUnReadMessage = true;
                                        break;
                                    }
                            }
                            if (HaveUnReadMessage == true) { break; }
                        }
                        if (HaveUnReadMessage == false)  //該小農沒有未讀訊息
                        {
                            var result = new
                            {
                                statusCode = 200,
                                status = "success",
                                message = "取得成功",
                                haveUnreadMessage = false,
                            };
                            return Content(HttpStatusCode.OK, result);
                        }
                        else     //該小農具有未讀訊息
                        {
                            var result = new
                            {
                                statusCode = 200,
                                status = "success",
                                message = "取得成功",
                                haveUnreadMessage = true,
                            };
                            return Content(HttpStatusCode.OK, result);
                        }
                    }
                    else
                    {
                        var result = new
                        {
                            statusCode = 402,
                            status = "error",
                            message = "聊天室不存在",
                        };
                        return Content(HttpStatusCode.OK, result);
                    }
                }
                else //使用者為客戶
                {
                    var checklist = db.ChatRooms.Where(x => x.UserIdOwner == userId)?.AsEnumerable();
                    if (checklist != null)
                    {
                        bool HaveUnReadMessage = false;
                        foreach (var chats in checklist)
                        {
                            if (chats.Record != null)
                            {
                                foreach (var talks in chats.Record)
                                    if (talks.UserIdSender != userId && talks.IsRead == false)
                                    {
                                        HaveUnReadMessage = true;
                                        break;
                                    }
                            }
                            if (HaveUnReadMessage == true) { break; }
                        }
                        if (HaveUnReadMessage == false)  //該使用者沒有未讀訊息
                        {
                            var result = new
                            {
                                statusCode = 200,
                                status = "success",
                                message = "取得成功",
                                haveUnreadMessage = false,
                            };
                            return Content(HttpStatusCode.OK, result);
                        }
                        else     //該使用者具有未讀訊息
                        {
                            var result = new
                            {
                                statusCode = 200,
                                status = "success",
                                message = "取得成功",
                                haveUnreadMessage = true,
                            };
                            return Content(HttpStatusCode.OK, result);
                        }
                    }
                    else
                    {
                        var result = new
                        {
                            statusCode = 402,
                            status = "error",
                            message = "聊天室不存在",
                        };
                        return Content(HttpStatusCode.OK, result);
                    }
                }
            }
        }

        #endregion FCW-07 取得單一使用者未讀訊息通知
    }

    public class userChatroomcheck
    {
        [Display(Name = "使用者Id=>接收訊息者")]
        public int receiverId { get; set; }
    }

    public class ChatSendMessageCheck
    {
        [Display(Name = "chatroom")]
        public int chatroomId { get; set; }

        [Display(Name = "使用者Id=>發訊者")]
        public int userIdSender { get; set; }

        [Required]
        [MaxLength(500)]
        public string message { get; set; }
    }

    public class LiveMessagecheck
    {
        [Display(Name = "訊息者")]
        public string message { get; set; }
    }
}