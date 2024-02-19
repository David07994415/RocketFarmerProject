using FarmerPro.Models;
using FarmerPro.Models.ViewModel;
using FarmerPro.Securities;
using Konscious.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http;
using System.Web.Http.Results;

namespace FarmerPro.Controllers
{
    public class UserController : ApiController
    {
        //第一步: 取得資料來源
        private FarmerProDB db = new FarmerProDB();

        //第二步: 使用的CRUD方法+簡易判斷的方法
        //建立 POST: api/User

        #region FCR-1 註冊
        [HttpPost]
        //自定義路由
        [Route("api/register")]
        //使用 IHttpActionResult 作為返回 HTTP 回應類型
        public IHttpActionResult register([FromBody] Register input)
        {
            //try
            //{
                if (!ModelState.IsValid)
                {
                    //result訊息
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "帳號密碼格式不正確，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    string accountCheck = input.account;
                    var Isregister = db.Users.Where(ac => ac.Account == accountCheck)?.FirstOrDefault();
                    if (Isregister != null)
                    {
                        //result訊息
                        var result = new
                        {
                            statusCode = 402,
                            status = "error",
                            message = "帳號已存在，請重新輸入",
                        };
                        return Content(HttpStatusCode.OK, result);
                    }
                    else
                    {
                        //Hash 加鹽加密
                        string password = input.password;
                        var salt = CreateSalt();
                        string saltStr = Convert.ToBase64String(salt); //將 byte 改回字串存回資料表
                        var hash = HashPassword(password, salt);
                        string hashPassword = Convert.ToBase64String(hash);

                        //資料表User 賦予 到 newaccount   
                        var newaccount = new User
                        {
                            //將input 輸入的 Account(model資料表欄位) 賦予 到 Account         
                            Account = input.account,
                            Password = hashPassword,
                            Category = input.category,
                            Salt = saltStr,
                        };

                        // 將 newaccount 加入 User 集合
                        db.Users.Add(newaccount);
                        // 執行資料庫儲存變更操作
                        db.SaveChanges();

                        //result訊息
                        var result = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "註冊成功",
                        };
                        return Content(HttpStatusCode.OK, result);
                        //return Ok(result);
                    }

                }

            //}
            //catch
            //{
            //    //result訊息
            //    var result = new
            //    {
            //        statusCode = 500,
            //        status = "error",
            //        message = "其他錯誤",
            //    };
            //    return Content(HttpStatusCode.OK, result);
            //}
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
        #endregion

        #region FCR-1 登入
        //第二支 POST: api/User
        [HttpPost]
        [Route("api/login/general")]
        public IHttpActionResult logincheck([FromBody] login input)
        {
            if (!ModelState.IsValid)
            {
                var result = new
                {
                    statusCode = 401,
                    status = "error",
                    message = "登入失敗，帳號密碼格式不正確",
                };
                return Content(HttpStatusCode.OK, result);
            }
            else
            {
                var IsUser = db.Users.Where(x => x.Account == input.account).FirstOrDefault();
                if (IsUser == null)
                {
                    //result訊息
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "登入失敗，帳號密碼格式不正確",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    string salt = IsUser.Salt;
                    string pw = input.password;

                    byte[] saltbyte = Convert.FromBase64String(salt);
                    byte[] Hash = HashPassword(pw, saltbyte);
                    string Hashstring = Convert.ToBase64String(Hash);
                    if (Hashstring != IsUser.Password)
                    {
                        var result = new
                        {
                            statusCode = 402,
                            status = "error",
                            message = "登入失敗，您的帳號或密碼不正確"
                        };
                        return Content(HttpStatusCode.OK, result);

                    }
                    else
                    {
                        // GenerateToken() 生成新 JwtToken 用法
                        JwtAuthUtil jwtAuthUtil = new JwtAuthUtil();
                        string jwtToken = jwtAuthUtil.GenerateToken(IsUser.Id, (int)IsUser.Category);
                       
                        var result = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "登入成功", // token失效時間:一天
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
        #endregion

        #region FCS-6 登出
        [HttpPost]
        [Route("api/logout")]
        [JwtAuthFilter]
        //使用 IHttpActionResult 作為返回 HTTP 回應類型
        public IHttpActionResult Logout()
        {
            try
            {
                JwtAuthUtil jwtAuthUtil = new JwtAuthUtil();
                string revokedToken = jwtAuthUtil.RevokeToken(); // ""

                //result訊息
                var result = new
                {
                    statusCode = 200,
                    status = "success",
                    message = "登出成功",
                };
                return Content(HttpStatusCode.OK, result);
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
        //注意，記得新增一個 account JSON class -> 資料表欄位
        public class login
        {
            [Required]
            [RegularExpression(@"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
            public string account { get; set; }

            [Required]
            [MinLength(6)]
            public string password { get; set; }
        }
        #endregion

    }
}
