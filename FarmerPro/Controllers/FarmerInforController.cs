﻿using FarmerPro.Models;
using FarmerPro.Securities;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web;
using System.Web.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using NSwag.Annotations;

namespace FarmerPro.Controllers
{
    [OpenApiTag("UserInfo", Description = "會員資料")]
    public class FarmerInforController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        #region BFI-01 上傳小農頭貼圖片(單張，及時渲染，有PUT功能)
        /// <summary>
        /// BFI-01 上傳小農頭貼圖片(單張，及時渲染，有PUT功能)
        /// </summary>
        /// <param></param>
        /// <returns>返回小農頭貼</returns>
        [HttpPost]
        [Route("api/farmer/pic")]
        [JwtAuthFilter]
        public async Task<IHttpActionResult> UploadfarmerInforphoto()
        {
            var jwtObject = JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter);
            int FarmerId = (int)jwtObject["Id"];
            var userExist = db.Users.Where(x => x.Id == FarmerId)?.FirstOrDefault();

            // 檢查請求是否包含 multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                var resultfileType = new
                {
                    statusCode = 408,
                    status = "error",
                    message = "上傳格式有誤",
                };
                return Content(HttpStatusCode.OK, resultfileType);
            }
            if (userExist == null)
            {
                var resultfileType = new
                {
                    statusCode = 400,
                    status = "error",
                    message = "上傳失敗，無此LiveId",
                };
                return Content(HttpStatusCode.OK, resultfileType);
            }
            else
            {
                // 檢查資料夾是否存在，若無則建立
                string root = HttpContext.Current.Server.MapPath($"~/upload/farmer/thumbnail/{FarmerId}");
                if (!Directory.Exists(root))
                {
                    Directory.CreateDirectory(root);
                }

                try
                {
                    // 讀取 MIME 資料
                    var provider = new MultipartMemoryStreamProvider();
                    await Request.Content.ReadAsMultipartAsync(provider);
                    if (provider.Contents.Count > 1)
                    {
                        var resultfileType = new
                        {
                            statusCode = 401,
                            status = "error",
                            message = "上傳失敗，請確認上傳圖片數量",
                        };
                        return Content(HttpStatusCode.OK, resultfileType);
                    }
                    foreach (var content in provider.Contents) //檢查附檔名類型
                    {
                        string fileNameData = content.Headers.ContentDisposition.FileName.Trim('\"');
                        string fileType = fileNameData.Remove(0, fileNameData.LastIndexOf('.')).ToLower();
                        if (fileType != ".jpg" && fileType != ".jpeg" && fileType != ".png")
                        {
                            var resultfileType = new
                            {
                                statusCode = 402,
                                status = "error",
                                message = "上傳失敗，請確認圖檔格式",
                            };
                            return Content(HttpStatusCode.OK, resultfileType);
                        }
                    }

                    List<string> imglList = new List<string>();
                    //foreach provider.Contents 中的每個 content，處理多個圖片檔案
                    foreach (var content in provider.Contents)
                    {
                        // 取得檔案副檔名
                        string fileNameData = content.Headers.ContentDisposition.FileName.Trim('\"');
                        string fileType = fileNameData.Remove(0, fileNameData.LastIndexOf('.'));
                        string fileName = FarmerId.ToString() + DateTime.Now.ToString("yyyyMMddHHmmssfff") + fileType;

                        // 儲存圖片
                        var fileBytes = await content.ReadAsByteArrayAsync();
                        var outputPath = Path.Combine(root, fileName);
                        using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                        {
                            await output.WriteAsync(fileBytes, 0, fileBytes.Length);
                        }

                        // 載入原始圖片，調整圖片大小
                        using (var image = Image.Load<Rgba32>(outputPath))
                        {
                            // 設定最大檔案大小 (2MB)
                            long maxFileSizeInBytes = 2 * 1024 * 1024;

                            // 計算目前圖片的檔案大小
                            using (var memoryStream = new MemoryStream())
                            {
                                image.Save(memoryStream, new JpegEncoder());
                                long currentFileSize = memoryStream.Length;

                                // 檢查檔案大小是否超過限制
                                if (currentFileSize > maxFileSizeInBytes)
                                {
                                    int MaxWidth = 800;   // 設定800px
                                    int MaxHeight = 600;  // 設定600px

                                    // 裁切圖片
                                    image.Mutate(x => x.Resize(new ResizeOptions
                                    {
                                        Size = new Size(MaxWidth, MaxHeight),
                                        Mode = ResizeMode.Max
                                    }));
                                }
                                else { }
                            }
                            image.Save(outputPath);
                        }
                        //加入至List
                        imglList.Add(fileName);
                        string url = WebConfigurationManager.AppSettings["Serverurl"].ToString() + $"/upload/farmer/thumbnail/{FarmerId}/" + fileName;
                        imglList.Add(url);
                    }
                    userExist.Photo = imglList[1];
                    db.SaveChanges();

                    //撈取使用者相片資料庫
                    var checkUserInfor = db.Users.Where(x => x.Id == FarmerId)?.FirstOrDefault();
                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "上傳成功",
                        data = new
                        {
                            src = checkUserInfor.Photo,
                            alt = checkUserInfor.Photo.Substring(checkUserInfor.Photo.LastIndexOf('/') + 1),
                        },
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                catch
                {
                    var resultfileType = new
                    {
                        statusCode = 408,
                        status = "error",
                        message = "上傳格式有誤",
                    };
                    return Content(HttpStatusCode.OK, resultfileType);
                }
            }
        }
        #endregion BFI-01 上傳小農頭貼圖片(單張，及時渲染，有PUT功能)

        #region BFI-02 修改小農個人資訊頁
        /// <summary>
        /// BFI-02 修改小農個人資訊頁
        /// </summary>
        /// <param name="input">提供小農個人資訊的 JSON 物件</param>
        /// <returns>返回小農個人資訊的 JSON 物件</returns>
        [HttpPost]
        [Route("api/farmer/info")]
        [JwtAuthFilter]
        public IHttpActionResult UpdatefarmerInfor(CheckFarmerInfor input)
        {
            if (!ModelState.IsValid)
            {
                var result = new
                {
                    statusCode = 401,
                    status = "error",
                    message = "欄位格式不正確，請重新輸入",
                };
                return Content(HttpStatusCode.OK, result);
            }
            else
            {
                try
                {
                    int farmerId = Convert.ToInt16(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);
                    var getUserInfor = db.Users.Where(x => x.Id == farmerId)?.FirstOrDefault();
                    if (getUserInfor == null)  // 若沒有此小農用戶
                    {
                        var result = new
                        {
                            statusCode = 402,
                            status = "error",
                            message = "沒有此小農用戶"
                        };
                        return Content(HttpStatusCode.OK, result);
                    }
                    else  // 若已經有建立小農用戶
                    {
                        getUserInfor.NickName = input.nickName;
                        getUserInfor.Phone = input.phone;
                        getUserInfor.Vision = input.vision;
                        getUserInfor.Description = input.description;
                        db.SaveChanges();

                        var getUpdateUserInfor = db.Users.Where(x => x.Id == farmerId)?.FirstOrDefault();
                        var result = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "上傳成功",
                            data = new
                            {
                                nickName = getUpdateUserInfor?.NickName,
                                account = getUpdateUserInfor?.Account,
                                phone = getUpdateUserInfor?.Phone,
                                photo = getUpdateUserInfor?.Photo,
                                vision = getUpdateUserInfor?.Vision,
                                description = getUpdateUserInfor?.Description,
                            },
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
        }
        #endregion BFI-02 修改小農個人資訊頁

        #region BFI-03 取得小農個人資訊頁
        /// <summary>
        /// BFI-03 取得小農個人資訊頁
        /// </summary>
        /// <param></param>
        /// <returns>返回小農個人資訊的 JSON 物件</returns>
        [HttpGet]
        [Route("api/farmer/info")]
        [JwtAuthFilter]
        public IHttpActionResult GetfarmerInfor()
        {
            try
            {
                int farmerId = Convert.ToInt16(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);
                var getUserInfor = db.Users.Where(x => x.Id == farmerId)?.FirstOrDefault();
                if (getUserInfor == null) // 若沒有此小農用戶
                {
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "沒有此小農用戶"
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else  // 若已經有建立小農用戶
                {
                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "取得成功",
                        data = new
                        {
                            nickName = getUserInfor?.NickName,
                            account = getUserInfor?.Account,
                            phone = getUserInfor?.Phone,
                            photo = getUserInfor?.Photo,
                            vision = getUserInfor?.Vision,
                            description = getUserInfor?.Description,
                        },
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
        #endregion BFI-03 取得小農個人資訊頁
    }

    /// <summary>
    /// 小農會員資訊
    /// </summary>
    public class CheckFarmerInfor
    {
        [Required]
        [Display(Name = "暱稱")]
        public string nickName { get; set; }

        [Required]
        [Display(Name = "電話")]
        [RegularExpression(@"^\d{10}$")]
        public string phone { get; set; }

        [Required]
        [Display(Name = "小農願景")]
        public string vision { get; set; }

        [Required]
        [Display(Name = "小農介紹")]
        public string description { get; set; }
    }
}