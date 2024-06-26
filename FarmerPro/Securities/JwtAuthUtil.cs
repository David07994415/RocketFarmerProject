﻿using Jose;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Configuration;
using FarmerPro.Models;

namespace FarmerPro.Securities
{
    /// <summary>
    /// JwtToken 生成功能
    /// </summary>
    public class JwtAuthUtil
    {
        /// <summary>
        /// 生成 JwtToken
        /// </summary>
        /// <param name="id">會員id</param>
        /// <returns>JwtToken</returns>
        public string GenerateToken(int id, int userCategory)
        {
            string secretKey = WebConfigurationManager.AppSettings["TokenKey"]; // 從 appSettings 取出
            var payload = new Dictionary<string, object>
            {
                { "Id", id},
                { "UserCategory", userCategory },
                { "Iat", DateTime.Now },
                { "Exp", DateTime.Now.AddMinutes(1440).ToString() } // JwtToken 時效設定 1440 分 (一天)
            };
            var token = JWT.Encode(payload, Encoding.UTF8.GetBytes(secretKey), JwsAlgorithm.HS512);
            return token;
        }

        /// <summary>
        /// 生成只刷新效期的 JwtToken
        /// </summary>
        /// <returns>JwtToken</returns>
        public string ExpRefreshToken(Dictionary<string, object> tokenData)
        {
            string secretKey = WebConfigurationManager.AppSettings["TokenKey"];
            // payload 從原本 token 傳遞的資料沿用，並刷新效期
            var payload = new Dictionary<string, object>
            {
                { "Id", (int)tokenData["Id"] },
                { "Account", tokenData["Account"].ToString() },
                { "NickName", tokenData["NickName"].ToString() },
                { "Image", tokenData["Image"].ToString() },
                { "Exp", DateTime.Now.AddMinutes(30).ToString() } // JwtToken 時效刷新設定 30 分
            };
            //產生刷新時效的 JwtToken
            var token = JWT.Encode(payload, Encoding.UTF8.GetBytes(secretKey), JwsAlgorithm.HS512);
            return token;
        }

        /// <summary>
        /// 生成無效 JwtToken
        /// </summary>
        /// <returns>JwtToken</returns>
        public string RevokeToken()
        {
            string secretKey = "RevokeToken"; // 故意用不同的 key 生成
            var payload = new Dictionary<string, object>
            {
                { "Id", 0 },
                { "Account", "None" },
                { "NickName", "None" },
                { "Image", "None" },
                { "Exp", DateTime.Now.AddDays(-15).ToString() } // 使 JwtToken 過期 失效
            };
            // 產生失效的 JwtToken
            var token = JWT.Encode(payload, Encoding.UTF8.GetBytes(secretKey), JwsAlgorithm.HS512);
            return "";
        }
    }
}