using FarmerPro.Models;
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
                string root = HttpContext.Current.Server.MapPath($"~/upload/user/thumbnail/{CustomerId}");
                if (!Directory.Exists(root))
                {
                    Directory.CreateDirectory(root);
                }

                try
                {
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
                    foreach (var content in provider.Contents)
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
                    foreach (var content in provider.Contents)
                    {
                        string fileNameData = content.Headers.ContentDisposition.FileName.Trim('\"');
                        string fileType = fileNameData.Remove(0, fileNameData.LastIndexOf('.'));

                        string fileName = CustomerId.ToString() + DateTime.Now.ToString("yyyyMMddHHmmssfff") + fileType;

                        var fileBytes = await content.ReadAsByteArrayAsync();
                        var outputPath = Path.Combine(root, fileName);
                        using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                        {
                            await output.WriteAsync(fileBytes, 0, fileBytes.Length);
                        }

                        using (var image = Image.Load<Rgba32>(outputPath))
                        {
                            long maxFileSizeInBytes = 2 * 1024 * 1024;

                            using (var memoryStream = new MemoryStream())
                            {
                                image.Save(memoryStream, new JpegEncoder());
                                long currentFileSize = memoryStream.Length;

                                if (currentFileSize > maxFileSizeInBytes)
                                {
                                    int MaxWidth = 800;   // 先設定800px
                                    int MaxHeight = 600;  // 先設定600px

                                    image.Mutate(x => x.Resize(new ResizeOptions // 裁切圖片
                                    {
                                        Size = new Size(MaxWidth, MaxHeight),
                                        Mode = ResizeMode.Max
                                    }));
                                }
                                else { }
                            }
                            image.Save(outputPath);
                        }

                        imglList.Add(fileName);
                        string url = WebConfigurationManager.AppSettings["Serverurl"].ToString() + $"/upload/user/thumbnail/{CustomerId}/" + fileName;
                        imglList.Add(url);
                    }

                    userExist.Photo = imglList[1];

                    db.SaveChanges();

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