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
    public class UserInfoController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        #region BGI-01 上傳會員頭貼圖片(單張，及時渲染，有PUT功能)

        /// <summary>
        /// BGI-01 上傳會員頭貼圖片(單張，及時渲染，有PUT功能)
        /// </summary>
        /// <param></param>
        /// <returns>返回一般會員頭貼</returns>
        [HttpPost]
        [Route("api/user/pic")]
        [JwtAuthFilter]
        public async Task<IHttpActionResult> UploadUserInforphoto()
        {
            int CustomerId = Convert.ToInt16(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);

            var userExist = db.Users.Where(x => x.Id == CustomerId)?.FirstOrDefault();

            // 檢查請求是否包含 multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }
            if (userExist == null)
            {
                var resultfileType = new
                {
                    statusCode = 400,
                    status = "error",
                    message = "上傳失敗，無此一般會員Id",
                };
                return Content(HttpStatusCode.OK, resultfileType);
            }
            else
            {
                // 檢查資料夾是否存在，若無則建立
                string root = HttpContext.Current.Server.MapPath($"~/upload/user/thumbnail/{CustomerId}");
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
                        string fileType = fileNameData.Remove(0, fileNameData.LastIndexOf('.')).ToLower(); // .jpg
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
                    //遍歷 provider.Contents 中的每個 content，處理多個圖片檔案
                    foreach (var content in provider.Contents)
                    {
                        // 取得檔案副檔名
                        string fileNameData = content.Headers.ContentDisposition.FileName.Trim('\"');
                        string fileType = fileNameData.Remove(0, fileNameData.LastIndexOf('.')); // .jpg

                        // 定義檔案名稱
                        string fileName = CustomerId.ToString() + DateTime.Now.ToString("yyyyMMddHHmmssfff") + fileType;

                        // 儲存圖片
                        var fileBytes = await content.ReadAsByteArrayAsync();
                        var outputPath = Path.Combine(root, fileName);
                        using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                        {
                            await output.WriteAsync(fileBytes, 0, fileBytes.Length);
                        }

                        //// 載入原始圖片，直接存入伺服器(未裁切)
                        //using (var image = Image.Load<Rgba32>(outputPath))
                        //{
                        //    // 儲存裁切後的圖片
                        //    image.Save(outputPath);
                        //}

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
                                    // 如果超過，可能需要進一步調整，或者進行其他處理
                                    // 這裡僅僅是一個簡單的示例，實際應用可能需要更複雜的處理邏輯
                                    //// 設定裁切尺寸
                                    int MaxWidth = 800;   // 先設定800px
                                    int MaxHeight = 600;  // 先設定600px

                                    // 裁切圖片
                                    image.Mutate(x => x.Resize(new ResizeOptions
                                    {
                                        Size = new Size(MaxWidth, MaxHeight),
                                        Mode = ResizeMode.Max
                                    }));
                                }
                                else { }
                            }
                            // 儲存後的圖片
                            image.Save(outputPath);
                        }
                        //加入至List
                        imglList.Add(fileName);
                        string url = WebConfigurationManager.AppSettings["Serverurl"].ToString() + $"/upload/user/thumbnail/{CustomerId}/" + fileName;
                        imglList.Add(url);
                    }

                    //可以補上清空資料夾的特定圖片資料
                    userExist.Photo = imglList[1];
                    //可以補上照片名稱的資料庫欄位
                    db.SaveChanges();

                    //撈取使用者相片資料庫
                    var checkUserInfor = db.Users.Where(x => x.Id == CustomerId)?.FirstOrDefault();

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
                catch (DbEntityValidationException ex)
                {
                    // Handle entity validation errors
                    var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.ErrorMessage);

                    var fullErrorMessage = string.Join("; ", errorMessages);
                    var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                    return BadRequest(exceptionMessage);
                }
            }
        }

        #endregion BGI-01 上傳會員頭貼圖片(單張，及時渲染，有PUT功能)

        #region BGI-02 修改會員個人資訊頁

        /// <summary>
        /// BGI-02 修改會員個人資訊頁
        /// </summary>
        /// <param name="input">提供一般會員個人資訊的 JSON 物件</param>
        /// <returns>返回一般會員個人資訊的 JSON 物件</returns>
        [HttpPost]
        [Route("api/user/info")]
        [JwtAuthFilter]
        public IHttpActionResult UpdateUserInfo([FromBody] UserInfoClass input)
        {
            try
            {
                int CustomerId = Convert.ToInt32(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);

                var user = db.Users.FirstOrDefault(u => u.Id == CustomerId && u.Category == 0);
                if (user == null)
                {
                    var result = new
                    {
                        statusCode = 400,
                        status = "error",
                        message = "沒有此一般會員，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else if (!ModelState.IsValid)
                {
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "欄位輸入格式不正確，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    user.NickName = input.nickName ?? user.NickName;
                    user.Phone = input.phone ?? user.Phone;
                    user.Sex = input.sex ?? user.Sex;
                    user.Birthday = input.birthday;

                    db.SaveChanges();

                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "修改成功",
                        data = new
                        {
                            nickName = user?.NickName,
                            account = user.Account,
                            phone = user?.Phone,
                            photo = user?.Photo,
                            sex = user?.Sex,
                            birthday = user.Birthday?.ToString("yyyy/MM/dd")
                        }
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

        #endregion BGI-02 修改會員個人資訊頁

        #region BGI-03 取得會員個人資訊頁

        /// <summary>
        /// BGI-03 取得會員個人資訊頁
        /// </summary>
        /// <param></param>
        /// <returns>返回一般會員個人資訊的 JSON 物件</returns>
        [HttpGet]
        [Route("api/user/info")]
        [JwtAuthFilter]
        public IHttpActionResult GetUserInfo()
        {
            try
            {
                int CustomerId = Convert.ToInt32(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);

                var user = db.Users.FirstOrDefault(u => u.Id == CustomerId && u.Category == 0);
                if (user == null)
                {
                    var result = new
                    {
                        statusCode = 400,
                        status = "error",
                        message = "沒有此一般會員，請重新輸入",
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
                            nickName = user?.NickName,
                            account = user.Account,
                            phone = user?.Phone,
                            photo = user?.Photo,
                            sex = user?.Sex,
                            birthday = user?.Birthday
                        }
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

        #endregion BGI-03 取得會員個人資訊頁

        /// <summary>
        /// 一般會員資訊
        /// </summary>
        public class UserInfoClass
        {
            [MaxLength(500)]
            [Display(Name = "暱稱")]
            public string nickName { get; set; }

            [Display(Name = "照片")]
            public string photo { get; set; }

            [Display(Name = "生日")]
            [DataType(DataType.Date)]
            public DateTime birthday { get; set; }

            [Display(Name = "性別")]
            public bool? sex { get; set; }

            [MaxLength(100)]
            [Display(Name = "電話")]
            public string phone { get; set; }
        }
    }
}