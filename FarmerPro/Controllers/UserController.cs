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
using NSwag.Annotations;

namespace FarmerPro.Controllers
{
    [OpenApiTag("Login", Description = "一般登入註冊、無密碼、PassKey")]
    public class UserController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();
        private IFido2 _fido2;

        public UserController()
        {
            Fido2Configuration config = new Fido2Configuration();
            config.ServerDomain = "sun-live.vercel.app";
            config.ServerName = "FarmerProject";
            config.Origins = new HashSet<string>(new[] { @"https://sun-live.vercel.app" });
            config.TimestampDriftTolerance = int.Parse("300000");
            _fido2 = new Fido2(config);
        }

        #region FCR-01 註冊
        /// <summary>
        /// FCR-1 註冊
        /// </summary>
        /// <param name="input">提供註冊所需的 JSON 物件</param>
        /// <returns>返回註冊狀態</returns>
        [HttpPost]
        [Route("api/register")]
        public IHttpActionResult register([FromBody] Register input)
        {
            try
            {
                if (!ModelState.IsValid)
                {
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
                        string password = input.password;
                        var salt = CreateSalt();
                        string saltStr = Convert.ToBase64String(salt);
                        var hash = HashPassword(password, salt);
                        string hashPassword = Convert.ToBase64String(hash);

                        var newaccount = new User
                        {
                            Account = input.account,
                            Password = hashPassword,
                            Category = input.category,
                            Salt = saltStr,
                        };
                        db.Users.Add(newaccount);
                        db.SaveChanges();

                        var result = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "註冊成功",
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
        #endregion FCR-01 註冊

        #region FCS-01 登入
        /// <summary>
        /// FCS-01 登入
        /// </summary>
        /// <param name="input">提供登入所需的 JSON 物件</param>
        /// <returns>返回登入狀態</returns>
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
                        JwtAuthUtil jwtAuthUtil = new JwtAuthUtil();
                        string jwtToken = jwtAuthUtil.GenerateToken(IsUser.Id, (int)IsUser.Category);
                        var result = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "登入成功",
                            token = jwtToken,
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
        #endregion FCS-01 登入

        #region FCS-06 登出
        /// <summary>
        /// FCS-06 登出
        /// </summary>
        /// <param></param>
        /// <returns>返回登出狀態</returns>
        [HttpPost]
        [Route("api/logout")]
        [JwtAuthFilter]
        public IHttpActionResult Logout()
        {
            try
            {
                JwtAuthUtil jwtAuthUtil = new JwtAuthUtil();
                string revokedToken = jwtAuthUtil.RevokeToken();
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
                var result = new
                {
                    statusCode = 500,
                    status = "error",
                    message = "其他錯誤",
                };
                return Content(HttpStatusCode.OK, result);
            }
        }
        #endregion FCS-06 登出

        #region FCS-07 取得google帳號授權網址(登入)
        /// <summary>
        /// FCS-07 取得google帳號授權網址(登入)
        /// </summary>
        /// <param></param>
        /// <returns>返回取得授權網址</returns>
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
                string redirecturi = @"https://sun-live.vercel.app/auth/verify";  // Google Console APP 所對應的前端網址
                var authorizationUrl = flow.CreateAuthorizationCodeRequest(redirecturi);   // 創建 AuthorizationCodeRequestUrl
                authorizationUrl.Scope = @"https://www.googleapis.com/auth/userinfo.profile https://www.googleapis.com/auth/userinfo.email";
                Uri authUrl = authorizationUrl.Build();   // 建立授權 URL
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

        #endregion FCS-07 取得google帳號授權網址(登入)

        #region FCS-08 驗證Oauth2並回傳登入結果
        /// <summary>
        /// FCS-08 驗證Oauth2並回傳登入結果
        /// </summary>
        /// <param name="inputs">提供登入所需的 code</param>
        /// <returns>返回jwtToken及登入結果</returns>
        [HttpPost]
        [Route("api/login/authcode")]
        public async Task<IHttpActionResult> LoginCodeTurntoToken(LoginToken inputs)
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

                string redirecturi = @"https://sun-live.vercel.app/auth/verify";
                string decodedCode = HttpUtility.UrlDecode(inputs.code);
                var tokenResponse = await flow.ExchangeCodeForTokenAsync("user", decodedCode, redirecturi, CancellationToken.None);
                var credential = new UserCredential(flow, "user", tokenResponse);

                GoogleOauth GoObj = new GoogleOauth();
                List<string> UserInfo = GoObj.RetrieveUserInfor(credential);

                if (UserInfo.Count >= 2) // 若Google有回傳資料
                {
                    string UserName = UserInfo[0];
                    string UserAccount = UserInfo[1];
                    User IsUser = GoObj.CheckUser(UserAccount);
                    if (IsUser != null)   // 資料庫已經有此帳號資料
                    {
                        JwtAuthUtil jwtAuthUtil = new JwtAuthUtil();
                        string jwtToken = jwtAuthUtil.GenerateToken(IsUser.Id, (int)IsUser.Category);
                        var ResultLogin = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "第三方登入成功",
                            token = jwtToken,
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
                    else  // 資料庫沒有帳號資料，需要新增
                    {
                        Guid GuidPW = Guid.NewGuid();
                        string password = GuidPW.ToString();   // 創立亂數密碼
                        var salt = CreateSalt();
                        string saltStr = Convert.ToBase64String(salt); // 將 byte 改回字串存回資料表
                        var hash = HashPassword(password, salt);
                        string hashPassword = Convert.ToBase64String(hash);

                        var newaccount = new User
                        {
                            Account = UserAccount,
                            Password = hashPassword,
                            Category = UserCategory.一般會員,
                            NickName = UserName,
                            Salt = saltStr,
                        };
                        db.Users.Add(newaccount);
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
                                message = "註冊與第三方登入成功",
                                token = jwtToken,
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

        #endregion FCS-08 驗證Oauth2並回傳登入結果

        #region FCS-09 回傳使用者證明(Attestation)
        /// <summary>
        /// FCS-09 回傳使用者證明(Attestation)
        /// </summary>
        /// <param name="inputs">提供登入所需的 JSON 物件</param>
        /// <returns>返回 optionsJson</returns>
        [HttpPost]
        [Route("api/login/attestation")]
        public async Task<IHttpActionResult> AuthnAttestation(AuthnUser inputs)
        {
            try
            {
                var username = inputs.inputName;
                var displayName = username;

                string UserAccount = inputs.inputName;
                bool IsRegister = inputs.isRegister;
                if (IsRegister == true)  // 註冊類型
                {
                    // 先確認使用者帳號是否已經在資料庫中
                    var IsUser = db.Users.Where(x => x.Account == UserAccount)?.FirstOrDefault();
                    if (IsUser != null)  // 使用者已經在帳戶中
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
                            Category = UserCategory.一般會員, // 以一般會員為註冊對象
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
                else // 非註冊類型
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
                    //options.RpId = @"sun-live.vercel.app";

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
            }
            catch (Exception e)
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
        #endregion FCS-09 回傳使用者證明(Attestation)

        #region FCS-10 驗證使用者註冊身分(Attestation)
        /// <summary>
        /// FCS-10 驗證使用者註冊身分(Attestation)
        /// </summary>
        /// <param name="inputs">提供第三方註冊所需的 JSON 物件</param>
        /// <returns>返回註冊狀態</returns>
        [HttpPost]
        [Route("api/login/attestation/result")] 
        public async Task<IHttpActionResult> AuthnAttestationResult(AuthnAttestationResultInput inputs)
        {
            try
            {
                // 1. get the options we sent the client
                AuthenticatorAttestationRawResponse aaar = inputs.aarr;
                CredentialCreateOptions options = inputs.ccp;

                // 2. Create callback so that lib can verify credential id is unique to this user
                IsCredentialIdUniqueToUserAsyncDelegate callback = async (args, cancellationToken) =>
                {
                    //簡化設定
                    //var users = db.Users.Where(x => x.Account == args.User.Name)?.FirstOrDefault();
                    //if (users != null)
                    //    return false;
                    return true;
                };

                // 3. Verify and make the credentials
                var success = await _fido2.MakeNewCredentialAsync(
                    aaar,
                    options,
                    callback);

                // 4. Store the credentials in db
                Fido2User inputUserInfor = options.User;
                byte[] userIDbytes = inputUserInfor.Id;
                int userId = BitConverter.ToInt32(userIDbytes, 0); // 此為外鍵欄位
                string PK = success.Result.PublicKey.ToString();
                string CID = success.Result.AttestationClientDataJson.ToString();

                var InsertSC = new Credential   
                {
                    UserId = userId,
                    CredentialId = success.Result.Id, 
                    PublicKey = success.Result.PublicKey
                };
                db.Credential.Add(InsertSC);
                db.SaveChanges();

                var result = new
                {
                    statusCode = 200,
                    status = "success",
                    message = "註冊成功，請重新登入",
                };
                return Content(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.OK, ex.Message + ex.StackTrace);
            }
        }
        #endregion FCS-10 驗證使用者註冊身分(Attestation)

        #region FCS-11 驗證使用者登入身分(Assertion)
        /// <summary>
        /// FCS-11 驗證使用者登入身分(Assertion)
        /// </summary>
        /// <param name="inputs">提供第三方登入驗證所需的 JSON 物件</param>
        /// <returns>返回jwtToken及登入結果</returns>
        [HttpPost]
        [Route("api/login/assertion/result")]
        public async Task<IHttpActionResult> AuthnAssertionResult(AuthnAssertionResultInput inputs)
        {
            try
            {
                // 1. get the options we sent the client
                var options = inputs.ao;
                var AssertionResult = inputs.aarr;

                // 2. Get registered credential from database
                var CredentialData = db.Credential.Where(x => x.CredentialId == AssertionResult.Id)?.FirstOrDefault();
                if (CredentialData == null)
                {
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "不正確的 Credential Id",
                    };
                    return Content(HttpStatusCode.OK, result);
                }

                // 3. Get credential counter from database
                //var storedCounter = creds.SignatureCounter;
                uint storedCounter = 0;

                // 4. Create callback to check if userhandle owns the credentialId
                IsUserHandleOwnerOfCredentialIdAsync callback = async (args, cancellationToken) =>
                {
                    //簡化設定
                    //var storedCreds = this._demoStorage.GetCredentialsByUserHandle(args.UserHandle);
                    //return storedCreds.Any(c => c.DescriptorId.SequenceEqual(args.CredentialId));
                    return true;
                };

                // 5. Make the assertion
                var res = await _fido2.MakeAssertionAsync(
                    AssertionResult,
                    options,
                    CredentialData.PublicKey,
                    new List<byte[]>(),
                    storedCounter,
                    callback);

                if (res.Status == "ok")
                {
                    var IsUser = db.Users.Where(x => x.Id == CredentialData.UserId).FirstOrDefault();
                    JwtAuthUtil jwtAuthUtil = new JwtAuthUtil();
                    string jwtToken = jwtAuthUtil.GenerateToken(IsUser.Id, (int)IsUser.Category);

                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "登入成功",
                        token = jwtToken,
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
                else
                {
                    var result = new
                    {
                        statusCode = 402,
                        status = "error",
                        message = "驗證失敗",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.OK, ex.Message + ex.StackTrace);
            }
        }
        #endregion FCS-11 驗證使用者登入身分(Assertion)


        //產生 Salt 功能
        private byte[] CreateSalt()
        {
            var buffer = new byte[16];
            var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(buffer);
            return buffer;
        }

        // Hash 處理加鹽的密碼功能，使用 Argon2 演算法執行密碼雜湊
        private byte[] HashPassword(string password, byte[] salt)
        {
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));
            argon2.Salt = salt;
            argon2.DegreeOfParallelism = 8; // 4 核心就設成 8
            argon2.Iterations = 2; // 迭代運算次數，更高的迭代次數可以提高安全性
            argon2.MemorySize = 1024; // 1 GB，定義演算法要使用的記憶體大小（以位元組為單位）

            //Argon2 演算法產生 16 位元組雜湊並將其作為位元組數組傳回
            return argon2.GetBytes(16);
        }

        public class login
        {
            [Required]
            [RegularExpression(@"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
            public string account { get; set; }

            [Required]
            [MinLength(6)]
            public string password { get; set; }
        }

        /// <summary>
        /// 登入者 Token
        /// </summary>
        public class LoginToken
        {
            public string code { get; set; }
        }

        /// <summary>
        /// PassKey 登入使用者資料
        /// </summary>
        public class AuthnUser
        {
            [Required]
            public string inputName { get; set; }

            public bool isRegister { get; set; }
        }

        /// <summary>
        /// PassKey 使用者註冊資料
        /// </summary>
        public class AuthnAttestationResultInput
        {
            public AuthenticatorAttestationRawResponse aarr { get; set; }
            public CredentialCreateOptions ccp { get; set; }
        }

        /// <summary>
        /// PassKey 使用者登入資料
        /// </summary>
        public class AuthnAssertionResultInput
        {
            public AuthenticatorAssertionRawResponse aarr { get; set; }
            public AssertionOptions ao { get; set; }
        }
    }
}