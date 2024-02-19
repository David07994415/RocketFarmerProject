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
using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace FarmerPro.Controllers
{
    public class LoginForgetController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        #region FCS-4 忘記密碼(通知後端)

        [HttpPost]
        [Route("api/login/forget")]
        public IHttpActionResult ForgetPasswordSendMail([FromBody] ForgetPasswordAccount input)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var result = new
                    {
                        statusCode = 400,
                        status = "error",
                        message = "帳號格式不正確，或是不存在此帳號，請重新輸入",
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
                        string link = @"https://sun-live.vercel.app/auth/changepassword?" + $"guid={passwordlessGUID}&account={IsUser.Account}";
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

        #endregion FCS-4 忘記密碼(通知後端)

        #region FCS-5 重設密碼

        [HttpPost]
        [Route("api/login/forget/reset")]
        public IHttpActionResult ForgetPasswordReset([FromBody] ForgetPasswordReset input)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    //result訊息
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "密碼格式不正確，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    var user = db.Users.Where(u => u.Account == input.account && u.EmailGUID.ToString() == input.guid)?.FirstOrDefault();
                    if (user == null)
                    {
                        var resultA = new
                        {
                            statusCode = 400,
                            status = "error",
                            message = "帳號格式不正確，請重新輸入",
                        };
                        return Content(HttpStatusCode.OK, resultA);
                    }

                    //Hash 加鹽加密
                    string password = input.password;
                    var salt = CreateSalt();
                    string saltStr = Convert.ToBase64String(salt); //將 byte 改回字串存回資料表
                    var hash = HashPassword(password, salt);
                    string hashPassword = Convert.ToBase64String(hash);

                    user.Password = hashPassword;
                    user.Salt = saltStr;
                    db.SaveChanges();

                    //result訊息
                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "重設密碼成功",
                    };
                    return Content(HttpStatusCode.OK, result);
                    //return Ok(result);
                }
            }
            catch
            {
                //result訊息
                var result = new
                {
                    statusCode = 500,
                    status = "error",
                    message = "其他錯誤",
                };
                return Content(HttpStatusCode.OK, result);
            }
        }

        // Argon2 加密
        //產生 Salt 功能
        //使用加密安全隨機數產生器 ( ) 產生隨機鹽
        private byte[] CreateSalt()
        {
            //建立一個大小為 16 ( ) 的位元組陣列buffer儲存產生的 salt
            var buffer = new byte[16];
            var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(buffer);
            return buffer;
        }

        // Hash 處理加鹽的密碼功能
        //將使用者的密碼和產生的鹽作為輸入，使用 Argon2 演算法執行密碼雜湊
        private byte[] HashPassword(string password, byte[] salt)
        {
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));

            //底下這些數字會影響運算時間，而且驗證時要用一樣的值
            //設定之前生成的鹽
            argon2.Salt = salt;
            argon2.DegreeOfParallelism = 8; // 4 核心就設成 8
            argon2.Iterations = 2; // 迭代運算次數，更高的迭代次數可以提高安全性
            argon2.MemorySize = 1024; // 1 GB，定義演算法要使用的記憶體大小（以位元組為單位）

            //Argon2 演算法產生 16 位元組雜湊並將其作為位元組數組傳回
            return argon2.GetBytes(16);
        }

        #endregion FCS-5 重設密碼

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
            message.Subject = "搶鮮購電商忘記密碼重設信";
            //建立 html 郵件格式
            BodyBuilder bodyBuilder = new BodyBuilder();
            string domainpath = WebConfigurationManager.AppSettings["Serverurl"].ToString() + "/upload/SunLogo.png";
            bodyBuilder.HtmlBody =
                "<h1>搶鮮購電商忘記密碼重設信</h1>" +
                $"<div><img src='{domainpath}' width='500px' ></div>" +
                $"<h3>{name}，您好：</h3>" +
                $"<h3>請點選下方連結進行忘記密碼重設：</h3>" +
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

    public class ForgetPasswordAccount
    {
        [Required]
        [Display(Name = "忘記密碼使用者帳號")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
        public string account { get; set; }
    }

    public class ForgetPasswordReset
    {
        [Required]
        [Display(Name = "忘記密碼使用者帳號")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
        public string account { get; set; }

        [Required]
        [Display(Name = "驗證之GUID")]
        public string guid { get; set; }

        //[Display(Name = "無密碼驗證之時間項目")]
        //public int time { get; set; }

        [Required]
        [MinLength(6)]
        public string password { get; set; }
    }
}