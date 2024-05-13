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
using NSwag.Annotations;

namespace FarmerPro.Controllers
{
    [OpenApiTag("Login", Description = "無密碼登入及忘記密碼")]
    public class LoginForgetController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        #region FCS-04 忘記密碼(通知後端)

        /// <summary>
        /// FCS-04 忘記密碼(通知後端)
        /// </summary>
        /// <param name="input">提供忘記密碼者之帳號 (Email)</param>
        /// <returns>返回信件傳送狀態</returns>
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
                        db.SaveChanges();

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

        #endregion FCS-04 忘記密碼(通知後端)

        #region FCS-05 重設密碼

        /// <summary>
        /// FCS-05 重設密碼
        /// </summary>
        /// <param name="input">提供忘記密碼的 JSON 物件</param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/login/forget/reset")]
        public IHttpActionResult ForgetPasswordReset([FromBody] ForgetPasswordReset input)
        {
            try
            {
                if (!ModelState.IsValid)
                {
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

                    string password = input.password; //Hash 加鹽加密
                    var salt = CreateSalt();
                    string saltStr = Convert.ToBase64String(salt); //將 byte 改回字串存回資料表
                    var hash = HashPassword(password, salt);
                    string hashPassword = Convert.ToBase64String(hash);

                    user.Password = hashPassword;
                    user.Salt = saltStr;
                    db.SaveChanges();

                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "重設密碼成功",
                    };
                    return Content(HttpStatusCode.OK, result);
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

        /// <summary>
        /// Argon2 加密，產生 Salt 功能
        /// </summary>
        private byte[] CreateSalt()
        {
            var buffer = new byte[16];
            var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(buffer);
            return buffer;
        }

        /// <summary>
        /// Hash 處理加鹽的密碼
        /// </summary>
        private byte[] HashPassword(string password, byte[] salt)
        {
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));

            argon2.Salt = salt;
            argon2.DegreeOfParallelism = 8; 
            argon2.Iterations = 2; 
            argon2.MemorySize = 1024; 

            return argon2.GetBytes(16);
        }

        #endregion FCS-05 重設密碼

        /// <summary>
        /// 信件寄送方法
        /// </summary>
        public static void sendGmail(string account, string name, string link)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("搶鮮購電商信箱", "14rocketback@gmail.com"));
            message.To.Add(new MailboxAddress(name, account));
            message.Cc.Add(new MailboxAddress("搶鮮購電商信箱", "14rocketback@gmail.com"));
            message.Subject = "搶鮮購電商忘記密碼重設信";
            BodyBuilder bodyBuilder = new BodyBuilder();
            string domainpath = WebConfigurationManager.AppSettings["Serverurl"].ToString() + "/upload/SunLogo.png";
            bodyBuilder.HtmlBody =
                "<h1>搶鮮購電商忘記密碼重設信</h1>" +
                $"<div><img src='{domainpath}' width='500px' ></div>" +
                $"<h3>{name}，您好：</h3>" +
                $"<h3>請點選下方連結進行忘記密碼重設：</h3>" +
                $"<h3 style='width: 500px;'>{link}</h3>" +
                $"<h3>請留意，上方連結僅於五分鐘內點選有效</h3>";

            message.Body = bodyBuilder.ToMessageBody(); 

            using (var client = new SmtpClient())
            {
                client.CheckCertificateRevocation = false;
                client.Connect("smtp.gmail.com", 587, false);
                client.Authenticate("14rocketback@gmail.com", WebConfigurationManager.AppSettings["MailSend"]);
                client.Send(message);
                client.Disconnect(true);
            }
        }
    }

    /// <summary>
    /// 忘記密碼使用者帳號
    /// </summary>
    public class ForgetPasswordAccount
    {
        [Required]
        [Display(Name = "忘記密碼使用者帳號")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
        public string account { get; set; }
    }

    /// <summary>
    /// 忘記密碼請求參數物件
    /// </summary>
    public class ForgetPasswordReset
    {
        [Required]
        [Display(Name = "忘記密碼使用者帳號")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
        public string account { get; set; }

        [Required]
        [Display(Name = "驗證之GUID")]
        public string guid { get; set; }

        [Required]
        [Display(Name = "密碼")]
        [MinLength(6)]
        public string password { get; set; }
    }
}