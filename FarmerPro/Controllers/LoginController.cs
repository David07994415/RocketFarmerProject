using FarmerPro.Models;
using FarmerPro.Securities;
using MimeKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MailKit.Net.Smtp;
using System.Web.Configuration;
using System.Web.Http.Results;
using NSwag.Annotations;

namespace FarmerPro.Controllers
{
    [OpenApiTag("Login", Description = "無密碼登入及忘記密碼")]
    public class LoginController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        #region FCS-02 無密碼登入寄送驗證信

        /// <summary>
        /// FCS-02 無密碼登入寄送驗證信
        /// </summary>
        /// <param name="input">提供無密碼登入之帳號 (Email)</param>
        /// <returns>返回信件傳送狀態</returns>
        [HttpPost]
        [Route("api/login/passwordless/")]
        public IHttpActionResult PasswordlessSendMail([FromBody] PasswordlessAccount input)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "帳號格式不正確",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    var IsUser = db.Users.Where(x => x.Account == input.account)?.FirstOrDefault();
                    if (IsUser == null)
                    {
                        var result = new
                        {
                            statusCode = 401,
                            status = "error",
                            message = "帳號格式不正確",
                        };
                        return Content(HttpStatusCode.OK, result);
                    }
                    else
                    {
                        Guid passwordlessGUID = Guid.NewGuid();
                        IsUser.EmailGUID = passwordlessGUID;
                        db.SaveChanges();    //產生guid並更新資料庫

                        int timedifference = (int)(DateTime.Now - new DateTime(1970, 01, 01)).TotalSeconds;
                        string link = @"https://sun-live.vercel.app/auth/login" + $"?guid={passwordlessGUID}&account={IsUser.Account}&time={timedifference}";
                        sendGmail(IsUser.Account, IsUser.NickName == null ? IsUser.Account : IsUser.NickName, link);  //寄送認證信件

                        var result = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "郵件已寄送成功",
                        };
                        return Content(HttpStatusCode.OK, result);
                    }
                }
            }
            catch
            {
                var result = new
                {
                    statusCode = 500,
                    status = "error",
                    message = "其他錯誤",
                };
                return Content(HttpStatusCode.OK, result);
            }
        }

        #endregion FCS-02 無密碼登入寄送驗證信

        #region FCS-03 無密碼登入驗證使用者

        /// <summary>
        /// FCS-03 無密碼登入驗證使用者
        /// </summary>
        /// <param name="input">提供信件網址內的 JSON 物件</param>
        /// <returns>返回驗證狀態及使用者個人資訊的 JSON 物件</returns>
        [HttpPost]
        [Route("api/login/passwordless/checkout")]
        public IHttpActionResult PasswordlessVerifyAccount([FromBody] PasswordlessVerify input)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "輸入格式不正確，請再次確認",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    var IsUser = db.Users.Where(x => x.Account == input.account && x.EmailGUID.ToString() == input.guid)?.FirstOrDefault();
                    if (IsUser == null)
                    {
                        var result = new
                        {
                            statusCode = 401,
                            status = "error",
                            message = "輸入格式不正確，請再次確認",
                        };
                        return Content(HttpStatusCode.OK, result);
                    }
                    else
                    {
                        DateTime check = new DateTime(1970, 01, 01).AddSeconds(Convert.ToInt64(input.time));
                        int timecheck = (int)(DateTime.Now - check).TotalSeconds;
                        if (timecheck > 300) //如果大於300秒鐘
                        {
                            var result = new
                            {
                                statusCode = 402,
                                status = "error",
                                message = "驗證超時，請重新進行驗證步驟",
                            };
                            return Content(HttpStatusCode.OK, result);
                        }
                        else
                        {
                            JwtAuthUtil jwtAuthUtil = new JwtAuthUtil();
                            string jwtToken = jwtAuthUtil.GenerateToken(IsUser.Id, (int)IsUser.Category); //生成新的TOKEN

                            var result = new
                            {
                                statusCode = 200,
                                status = "success",
                                message = "驗證成功", // token失效時間:一天
                                token = jwtToken,  // 登入成功時，回傳登入成功順便夾帶 JwtToken
                                data = new
                                {
                                    id = IsUser.Id,
                                    nickName = IsUser.NickName,
                                    account = IsUser.Account,
                                    photo = IsUser.Photo,
                                    category = IsUser.Category,
                                    birthday = IsUser.Birthday,
                                    phone = IsUser.Phone,
                                    sex = IsUser.Sex,
                                    vision = IsUser.Vision,
                                    description = IsUser.Description,
                                }
                            };
                            return Content(HttpStatusCode.OK, result);
                        }
                    }
                }
            }
            catch
            {
                var result = new
                {
                    statusCode = 500,
                    status = "error",
                    message = "其他錯誤",
                };
                return Content(HttpStatusCode.OK, result);
            }
        }

        #endregion FCS-03 無密碼登入驗證使用者

        //信件寄送方法
        public static void sendGmail(string account, string name, string link)
        {
            //宣告使用 MimeMessage
            var message = new MimeMessage();
            //設定發信地址 ("發信人", "發信 email")
            message.From.Add(new MailboxAddress("搶鮮購電商信箱", "14rocketback@gmail.com"));
            //設定收信地址 ("收信人", "收信 email")
            message.To.Add(new MailboxAddress(name, account));
            //寄件副本email
            message.Cc.Add(new MailboxAddress("搶鮮購電商信箱", "14rocketback@gmail.com"));
            //設定優先權
            //message.Priority = MessagePriority.Normal;
            //信件標題
            message.Subject = "搶鮮購電商登入驗證信";
            //建立 html 郵件格式
            BodyBuilder bodyBuilder = new BodyBuilder();
            string domainpath = WebConfigurationManager.AppSettings["Serverurl"].ToString() + "/upload/SunLogo.png";
            bodyBuilder.HtmlBody =
                "<h1>搶鮮購電商登入驗證信</h1>" +
                $"<div><img src='{domainpath}' width='500px' ></div>" +
                $"<h3>{name}，您好：</h3>" +
                $"<h3>請點選下方連結進行無密碼驗證登入：</h3>" +
                $"<h3 style='width: 500px;'>{link}</h3>" +
                $"<h3>請留意，上方連結僅於五分鐘內點選有效</h3>";
            //$"<p>{Comments.Text.Trim()}</p>";
            //設定郵件內容
            message.Body = bodyBuilder.ToMessageBody(); //轉成郵件內容格式

            using (var client = new SmtpClient())
            {
                //有開防毒時需設定 false 關閉檢查
                client.CheckCertificateRevocation = false;
                //設定連線 gmail ("smtp Server", Port, SSL加密)
                client.Connect("smtp.gmail.com", 587, false); // localhost 測試使用加密需先關閉

                // Note: only needed if the SMTP server requires authentication
                client.Authenticate("14rocketback@gmail.com", WebConfigurationManager.AppSettings["MailSend"]);
                //發信
                client.Send(message);
                //結束連線
                client.Disconnect(true);
            }
        }
    }

    /// <summary>
    /// 無密碼登入使用者帳號
    /// </summary>
    public class PasswordlessAccount
    {
        [Required]
        [Display(Name = "無密碼登入之使用者帳號")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
        public string account { get; set; }
    }

    /// <summary>
    /// 無密碼登入驗證項
    /// </summary>
    public class PasswordlessVerify
    {
        [Required]
        [Display(Name = "無密碼驗證之使用者帳號")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
        public string account { get; set; }

        [Required]
        [Display(Name = "無密碼驗證之GUID")]
        public string guid { get; set; }

        [Display(Name = "無密碼驗證之時間項目")]
        public int time { get; set; }
    }
}