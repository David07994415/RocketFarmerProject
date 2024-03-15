using FarmerPro.Models;
using FarmerPro.Models.ViewModel;
using FarmerPro.Securities;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2;
using Konscious.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Http.Results;
using System.Threading.Tasks;
using System.Threading;
using System.Web;
using System.Security.Principal;
using Fido2NetLib;
using static Fido2NetLib.Fido2;
using Microsoft.Extensions.DependencyInjection;
using Fido2NetLib.Objects;
using System.Security;
using Newtonsoft.Json.Linq;
using System.Security.Claims;
using System.Web.Security;
using Google.Apis.PeopleService.v1.Data;
using Newtonsoft.Json;

namespace FarmerPro.Controllers
{
    public class UserController : ApiController
    {
        //第一步: 取得資料來源
        private FarmerProDB db = new FarmerProDB();

        private IFido2 _fido2;
        public UserController()
        {
            Fido2Configuration config = new Fido2Configuration();
            config.ServerDomain = "localhost";//System.Configuration.ConfigurationManager.AppSettings["serverDomain"];
            config.ServerName = "FIDO2 Test";
            config.Origins = new HashSet<string>(new[] { @"http://localhost:44364" }); //這邊要改
            config.TimestampDriftTolerance = int.Parse("300000");
            // < add key = "serverDomain" value = "localhost" />
            //< add key = "origins" value = "http://localhost:52363" />
            //< add key = "timestampDriftTolerance" value = "300000" />

            _fido2 = new Fido2(config);
            //_fido2 = WebApiApplication.ServiceLocator.GetService<IFido2>();
        }


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


        #region FCS-7 取得google帳號授權網址(登入)
        [HttpGet]
        [Route("api/login/google")]
        public IHttpActionResult GetGooleOauth2Link()
        {
            try
            {
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
                string redirecturi = @"https://sun-live.vercel.app/auth/verify";  //這邊前端要改，後端console要加入
                //string redirecturi = @"http://localhost:3000/auth/verify";  //這邊前端要改，後端console要加入
                // 創建 AuthorizationCodeRequestUrl
                var authorizationUrl = flow.CreateAuthorizationCodeRequest(redirecturi);

                // 設置額外的參數，如範例中的 scope
                authorizationUrl.Scope = @"https://www.googleapis.com/auth/userinfo.profile https://www.googleapis.com/auth/userinfo.email";
                //https://www.googleapis.com/auth/userinfo.email%20
                // 建立授權 URL
                Uri authUrl = authorizationUrl.Build();
                string authUrlSpace = authUrl.ToString().Replace(" ", "%20");

                if (authUrl != null)
                {
                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "取得成功",
                        url = authUrlSpace,
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "取得失敗",
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
        #endregion

        #region FCS-8  驗證Oauth2並回傳登入結果
        [HttpPost]
        [Route("api/login/authcode")]
        public async Task<IHttpActionResult> LoginCodeTurntoToken(LoginToken inputs)
        {
            //try
            //{
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


            string redirecturi = @"https://sun-live.vercel.app/auth/verify"; //這邊前端要改，後端console要加入
            //string redirecturi = @"http://localhost:3000/auth/verify";  //這邊前端要改，後端console要加入
            string decodedCode = HttpUtility.UrlDecode(inputs.code);
            var tokenResponse = await flow.ExchangeCodeForTokenAsync("user", decodedCode, redirecturi, CancellationToken.None);

            var credential = new UserCredential(flow, "user", tokenResponse);

            GoogleOauth GoObj = new GoogleOauth();
            List<string> UserInfo = GoObj.RetrieveUserInfor(credential);

            if (UserInfo.Count >= 2) //Google有回傳資料
            {
                string UserName = UserInfo[0];
                string UserAccount = UserInfo[1];
                User IsUser = GoObj.CheckUser(UserAccount);
                if (IsUser != null)   //資料庫已經有此帳號資料
                {
                    JwtAuthUtil jwtAuthUtil = new JwtAuthUtil();
                    string jwtToken = jwtAuthUtil.GenerateToken(IsUser.Id, (int)IsUser.Category);

                    var ResultLogin = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "第三方登入成功", // token失效時間:一天
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
                    return Content(HttpStatusCode.OK, ResultLogin);
                }
                else  //資料庫沒有帳號資料，要先新增
                {

                    //Hash 加鹽加密
                    Guid GuidPW = Guid.NewGuid();
                    string password = GuidPW.ToString();   // 亂數密碼
                    var salt = CreateSalt();
                    string saltStr = Convert.ToBase64String(salt); //將 byte 改回字串存回資料表
                    var hash = HashPassword(password, salt);
                    string hashPassword = Convert.ToBase64String(hash);

                    //資料表User 賦予 到 newaccount   
                    var newaccount = new User
                    {
                        //將input 輸入的 Account(model資料表欄位) 賦予 到 Account         
                        Account = UserAccount,
                        Password = hashPassword,
                        Category = UserCategory.一般會員,
                        NickName = UserName,
                        Salt = saltStr,
                    };

                    // 將 newaccount 加入 User 集合
                    db.Users.Add(newaccount);
                    // 執行資料庫儲存變更操作
                    db.SaveChanges();

                    var IsRegister = db.Users.Where(x => x.Account == UserAccount)?.ToList().FirstOrDefault();
                    if (IsRegister != null)
                    {
                        JwtAuthUtil jwtAuthUtilnew = new JwtAuthUtil();
                        string jwtToken = jwtAuthUtilnew.GenerateToken(IsRegister.Id, (int)IsRegister.Category);
                        var ResultLogin = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "註冊與第三方登入成功", // token失效時間:一天
                            token = jwtToken,  // 登入成功時，回傳登入成功順便夾帶 JwtToken
                            data = new
                            {
                                id = IsRegister.Id,
                                nickName = IsRegister.NickName,
                                account = IsRegister.Account,
                                photo = IsRegister.Photo,
                                category = IsRegister.Category,
                                birthday = IsRegister.Birthday,
                                phone = IsRegister.Phone,
                                sex = IsRegister.Sex,
                                vision = IsRegister.Vision,
                                description = IsRegister.Description,
                            }
                        };
                        return Content(HttpStatusCode.OK, ResultLogin);
                    }
                    else
                    {
                        var result = new
                        {
                            statusCode = 401,
                            status = "error",
                            message = "登入失敗",
                        };
                        return Content(HttpStatusCode.OK, result);
                    }
                }
            }
            else
            {
                var result = new
                {
                    statusCode = 401,
                    status = "error",
                    message = "登入失敗",
                };
                return Content(HttpStatusCode.OK, result);
            }
            //}
            //catch
            //{
            //    var result = new
            //    {
            //        statusCode = 500,
            //        status = "error",
            //        message = "其他錯誤",
            //    };
            //    return Content(HttpStatusCode.OK, result);
            //}
        }
        #endregion

        #region FCS-9  回傳使用者證明(Attestation)
        //[Authorize]
        [HttpPost]
        [Route("api/login/attestation")]
        public async Task<IHttpActionResult> AuthnAttestation(AuthnUser inputs)
        {

            //try
            //{

            //var username = this.User.Identity.Name;  // USE   [Authorize]
            //var displayName = username;     // USE   [Authorize]

            var username = inputs.inputName;
            var displayName = username;

            string UserAccount = inputs.inputName;
            bool IsRegister = inputs.isRegister;
            if (IsRegister == true)  // 註冊類型
            {
                //先確認使用者帳號是否已經在資料庫中
                var IsUser = db.Users.Where(x => x.Account == UserAccount)?.FirstOrDefault();
                if (IsUser != null)  //使用者已經在帳戶中
                {
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "註冊失敗，帳號已存在",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else  // 使用者帳號沒有存在，進行FIDO註冊
                {

                    //Hash 加鹽加密
                    Guid GuidPW = Guid.NewGuid();
                    string password = GuidPW.ToString();   // 亂數密碼
                    var salt = CreateSalt();
                    string saltStr = Convert.ToBase64String(salt); //將 byte 改回字串存回資料表
                    var hash = HashPassword(password, salt);
                    string hashPassword = Convert.ToBase64String(hash);

                    var InsertUser = new User
                    {
                        Account = UserAccount,
                        Password = hashPassword,
                        NickName = username,
                        Category = UserCategory.一般會員, //先固定
                        Salt = saltStr
                    };
                    db.Users.Add(InsertUser);
                    db.SaveChanges();
                    int userId = InsertUser.Id;
                    byte[] userIdBytes = BitConverter.GetBytes(userId);

                    // 2. Get user existing keys by username
                    //var existingKeys = this.db.GetCredentialsByUser(user).Select(c => new PublicKeyCredentialDescriptor(c.DescriptorId)).ToList();
                    var existingKeys = new List<PublicKeyCredentialDescriptor>();


                    // 3. Create options
                    var authenticatorSelection = new AuthenticatorSelection
                    {
                        ResidentKey = ResidentKeyRequirement.Required,
                        UserVerification = UserVerificationRequirement.Preferred,
                        //AuthenticatorAttachment = AuthenticatorAttachment.CrossPlatform
                    };
                    var exts = new AuthenticationExtensionsClientInputs() { };

                    var options =
                        _fido2.RequestNewCredential(
                            new Fido2User()
                            {
                                DisplayName = displayName,
                                Id = userIdBytes,
                                Name = username
                            },
                            existingKeys,
                            authenticatorSelection,
                            AttestationConveyancePreference.None,
                            exts);
                    var optionsJson = options;

                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "設定成功，請返回option物件給予使用者",
                        option = options
                    };
                    return Content(HttpStatusCode.OK, result);
                }
            }
            else // 非註冊類型 if (IsRegister == false) 
            {

                // 1. Create options
                var authenticatorSelection = new AuthenticatorSelection
                {
                    ResidentKey = ResidentKeyRequirement.Required,
                    UserVerification = UserVerificationRequirement.Required,
                    //AuthenticatorAttachment   = AuthenticatorAttachment.CrossPlatform
                };

                var exts = new AuthenticationExtensionsClientInputs() { };

                var options =
                    _fido2.GetAssertionOptions(
                        new List<PublicKeyCredentialDescriptor>() { },
                        UserVerificationRequirement.Required,
                        exts);

                // 2. Temporarily store options, session/in-memory cache/redis/db
                //HttpContext.Current.Session.Add("fido2.assertionOptions", options.ToJson());

                var result = new
                {
                    statusCode = 200,
                    status = "success",
                    message = "設定成功，請返回option物件給予使用者",
                    option = options
                };
                return Content(HttpStatusCode.OK, result);
            }

            // }
            //catch (Exception e)
            //{
            //    var result = new
            //    {
            //        statusCode = 500,
            //        status = "error",
            //        message = "其他錯誤",
            //    };
            //    return Content(HttpStatusCode.OK, result);         
            ////    return this.Ok(new CredentialCreateOptions { Status = "error", ErrorMessage = e.Message });
            //}

        }
        #endregion


        #region FCS-10  驗證使用者身分(Attestation)  
        [HttpPost]
        [Route("api/login/attestation/result")]     //AuthnAttestationResultInput
        public async Task<IHttpActionResult> AuthnAttestationResult(AuthnAttestationResultInput inputs)
        {
            try 
            {
                // 1. get the options we sent the client
                AuthenticatorAttestationRawResponse aaar = inputs.aarr;
                CredentialCreateOptions options = inputs.ccp;
                //var parsedResponse = AuthenticatorAttestationResponse.Parse(aaar);
                //var jsonOptions = inputs.ccp as string;
                //var options = CredentialCreateOptions.FromJson(jsonOptions);

                //// 2. Create callback so that lib can verify credential id is unique to this user
                IsCredentialIdUniqueToUserAsyncDelegate callback = async (args, cancellationToken) =>
            {
                //先省略
                //var users = db.Users.Where(x => x.Account == args.User.Name)?.FirstOrDefault();
                //if (users != null)
                //    return false;

                //return true;
                return false;
            };

                //// 2. Verify and make the credentials
                var success = await _fido2.MakeNewCredentialAsync(
                    aaar,
                    options,
                    callback);

                //// 3. Store the credentials in db
                //Fido2User inputUserInfor = options.User;
                //byte[] userIDbytes = inputUserInfor.Id;
                //int userId = BitConverter.ToInt32(userIDbytes, 0); //外鍵
                //string PK= success.Result.PublicKey.ToString();
                //string CID = success.Result.AttestationClientDataJson.ToString();


                //var InsertSC = new Credential    //這邊要新增一張表
                //{
                //    UserId = userId,
                //    CredentialId = success.Result.Id.ToString(),  //success.Result.CredentialId,
                //     PublicKey = success.Result.PublicKey.ToString()//success.Result.PublicKey,
                //    //Descriptor = new PublicKeyCredentialDescriptor( success.Result.CredentialId ),
                //    //DescriptorId = success.Result.Id,
                //    //RegDate = DateTime.Now
                //    /*
                //    Id = success.Result.Id,
                //    Descriptor = new PublicKeyCredentialDescriptor( success.Result.Id ),
                //    PublicKey = success.Result.PublicKey,
                //    UserHandle = success.Result.User.Id,
                //    SignCount = success.Result.Counter,
                //    CredType = success.Result.CredType,
                //    RegDate = DateTime.Now,
                //    AaGuid = success.Result.AaGuid,
                //    Transports = success.Result.Transports,
                //    BE = success.Result.BE,
                //    BS = success.Result.BS,
                //    AttestationObject = success.Result.AttestationObject,
                //    AttestationClientDataJSON = success.Result.AttestationClientDataJSON,
                //    DevicePublicKeys = new List<byte[]>() { success.Result.DevicePublicKey }
                //    */
                //};
                //db.Credential.Add(InsertSC);
                //db.SaveChanges();

                // 4. return "ok" to the client
                var result = new
            {
                statusCode = 200,
                status = "success",
                message = "註冊成功，請重新登入",
            };
            return Content(HttpStatusCode.OK, success.ToString());
            }
            catch (Exception ex) 
            {
                return Content(HttpStatusCode.OK, ex.Message + ex.StackTrace);
            }

        }
        #endregion


        #region FCS-11  驗證使用者身分(Assertion)   
        //[HttpPost]
        //[Route("api/login/assertion/result")]
        //public async Task<IHttpActionResult> AuthnAssertionResult(AuthnAssertionResultInput inputs)
        //{

        //    // 1. get the options we sent the client
        //    var options = inputs.ao;

        //    // 2. Get registered credential from database
        //    var creds = this._demoStorage.GetCredentialById(inputs.aarr.Id) ?? throw new Exception("Unknown credentials");

        //    // 3. Get credential counter from database
        //    var storedCounter = creds.SignatureCounter;

        //    // 4. Create callback to check if userhandle owns the credentialId
        //    IsUserHandleOwnerOfCredentialIdAsync callback = async (args, cancellationToken) =>
        //    {
        //        var storedCreds = this._demoStorage.GetCredentialsByUserHandle(args.UserHandle);
        //        return storedCreds.Any(c => c.DescriptorId.SequenceEqual(args.CredentialId));
        //    };

        //    // 5. Make the assertion
        //    var res = await _fido2.MakeAssertionAsync(
        //        clientResponse,
        //        options,
        //        creds.PublicKey,
        //        new List<byte[]>(),
        //        storedCounter,
        //        callback);

        //    if (res.Status == "ok")
        //    {
        //        var users = this._demoStorage.GetUsersByCredentialId(res.CredentialId);

        //        if (users.Count() > 0)
        //        {
        //            var username = users.First().Name;

        //            // create identity
        //            var identity = new ClaimsIdentity(
        //            new[]
        //            {
        //                    new Claim( ClaimTypes.NameIdentifier, Guid.NewGuid().ToString() ),
        //                    new Claim( ClaimTypes.Name, username )
        //            }, "custom");
        //            ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        //            FormsAuthentication.SetAuthCookie(username, false);
        //        }
        //        else
        //        {
        //            throw new Exception("no user");
        //        }
        //    }
        //    else
        //    {
        //        throw new Exception("validation failed");
        //    }

        //    // 6. Store the updated counter
        //    this._demoStorage.UpdateCounter(res.CredentialId, res.SignCount);

        //    // 7. return OK to client
        //    return this.Ok(res);

        //}
        #endregion

        #region FCS-12  Credential Test()   
        [HttpGet]
        [Route("api/login/test/test")]
        public IHttpActionResult TestWebAuthn() 
        {
            var showAllCredential = db.Credential.Where(x => x.UserId == 1)?.FirstOrDefault();
            if (showAllCredential != null) 
            {
                var result = new
                {
                    CredentialId = showAllCredential.Id,
                    CredentialColumn = showAllCredential.CredentialId,
                    PublicColumn = showAllCredential.PublicKey,
                    MainKeyUserName = showAllCredential.User.Account
                };
                return Content(HttpStatusCode.OK, result);
            }
            else
            {
                var result = new
                {
                    state = "null status",
                };
                return Content(HttpStatusCode.OK, result);
            }
        }
        #endregion




        public class LoginToken
        {
            public string code { get; set; }
        }

        public class AuthnUser
        {
            [Required]
            public string inputName { get; set; }
            public bool isRegister { get; set; }  // true or false
        }

        public class AuthnAttestationResultInput
        {
            public AuthenticatorAttestationRawResponse aarr { get; set; }
            public CredentialCreateOptions ccp { get; set; }
        }


        public class Otherclass
        {
            public string ccp2 { get; set; }
            public string ccp3 { get; set; }
        }

        //public AuthenticatorAttestationRawResponse aarr { get; set; }

        public class AuthnAssertionResultInput
        {
            public AuthenticatorAssertionRawResponse aarr { get; set; }
            public AssertionOptions ao { get; set; }
        }


        public class Fake
        {
            public PublicKeyCredentialRpEntity rp { get; set; }
            public Fido2User user { get; set; }
            [JsonConverter(typeof(Base64UrlConverter))]
            public byte[] challenge { get; set; }
            public List<PubKeyCredParam> pubKeyCredParams { get; set; }
            public long timeout { get; set; }
            public AttestationConveyancePreference attestation { get; set; }
            public AuthenticatorSelection authenticatorSelection { get; set; }
            public List<PublicKeyCredentialDescriptor> excludeCredentials { get; set; }
            public AuthenticationExtensionsClientInputs extensions { get; set; }
        }

    }
}






