using Jose;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Http.Controllers;
using System.Web.Http;
using System.Web.Http.Filters;

namespace FarmerPro.Securities
{
        /// <summary>
        /// JwtAuthFilter 繼承 ActionFilterAttribute 可生成 [JwtAuthFilter] 使用
        /// </summary>
        public class JwtAuthFilter : ActionFilterAttribute
        {
            private static readonly string secretKey = WebConfigurationManager.AppSettings["TokenKey"];

            /// <summary>
            /// 過濾有用標籤 [JwtAuthFilter] 請求的 API 的 JwtToken 狀態及內容
            /// </summary>
            /// <param name="actionContext"></param>
            public override void OnActionExecuting(HttpActionContext actionContext)
            {
                // 取出請求內容並排除不需要驗證的 API
                var request = actionContext.Request;

                // 有取到 JwtToken 後，判斷授權格式不存在且不正確時
                if (request.Headers.Authorization == null || request.Headers.Authorization.Scheme != "Bearer")
                {
                    string messageJson = JsonConvert.SerializeObject(new { statusCode = 409,status="error", message = "驗證失敗，請重新登入" }); // JwtToken 遺失，需導引重新登入
                    var errorMessage = new HttpResponseMessage()
                    {
                        ReasonPhrase = "JwtToken Lost",
                        Content = new StringContent(messageJson,
                                    Encoding.UTF8,
                                    "application/json")
                    };
                    throw new HttpResponseException(errorMessage); // Debug 模式會停在此行，點繼續執行即可
                }
                else
                {
                    try
                    {
                        // 有 JwtToken 且授權格式正確時執行，用 try 包住，因為如果有篡改可能解密失敗
                        // 解密後會回傳 Json 格式的物件 (即加密前的資料)
                        var jwtObject = GetToken(request.Headers.Authorization.Parameter);

                        // 檢查有效期限是否過期，如 JwtToken 過期，需導引重新登入
                        if (IsTokenExpired(jwtObject["Exp"].ToString()))
                        {
                            string messageJson = JsonConvert.SerializeObject(new { statusCode = 409, status = "error", message = "驗證失敗，請重新登入" }); // JwtToken 過期，需導引重新登入
                            var errorMessage = new HttpResponseMessage()
                            {
                                ReasonPhrase = "JwtToken Expired",
                                Content = new StringContent(messageJson,
                                    Encoding.UTF8,
                                    "application/json")
                            };
                            throw new HttpResponseException(errorMessage); // Debug 模式會停在此行，點繼續執行即可
                        }
                    }
                    catch (Exception)
                    {
                        // 解密失敗
                        string messageJson = JsonConvert.SerializeObject(new { statusCode = 409, status = "error", message = "驗證失敗，請重新登入" }); // JwtToken 不符，需導引重新登入
                        var errorMessage = new HttpResponseMessage()
                        {
                            ReasonPhrase = "JwtToken NotMatch",
                            Content = new StringContent(messageJson,
                                    Encoding.UTF8,
                                    "application/json")
                        };
                        throw new HttpResponseException(errorMessage); // Debug 模式會停在此行，點繼續執行即可
                    }
                }

                base.OnActionExecuting(actionContext);
            }

            /// <summary>
            /// 將 Token 解密取得夾帶的資料
            /// </summary>
            /// <param name="token"></param>
            /// <returns></returns>
            public static Dictionary<string, object> GetToken(string token)
            {
                return JWT.Decode<Dictionary<string, object>>(token, Encoding.UTF8.GetBytes(secretKey), JwsAlgorithm.HS512);
            }

            /// <summary>
            /// 驗證 token 時效
            /// </summary>
            /// <param name="dateTime"></param>
            /// <returns></returns>
            public bool IsTokenExpired(string dateTime)
            {
                return Convert.ToDateTime(dateTime) < DateTime.Now;
            }
        }



   
}