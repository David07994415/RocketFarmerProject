using FarmerPro.Models;
using FarmerPro.Models.ViewModel;
using FarmerPro.Securities;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Http.Results;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Data.Entity.Validation;
using System.Web.Hosting;
using System.Security.Cryptography;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Globalization;
using Google.Apis.Util.Store;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;
using System.Threading;
using Google.Apis.Auth.OAuth2.Responses;
using NSwag.Annotations;

namespace FarmerPro.Controllers
{
    [OpenApiTag("Live", Description = "直播內容及設定")]
    public class LiveSettingController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        #region BFL-01 新增後台直播資訊(新增產品直播價)
        /// <summary>
        /// BFL-01 新增後台直播資訊(新增產品直播價)
        /// </summary>
        /// <param name="input">提供直播資訊的 JSON 物件</param>
        /// <returns>返回直播資訊的 JSON 物件</returns>
        [HttpPost]
        [Route("api/livesetting")]
        [JwtAuthFilter]
        public IHttpActionResult CreateNewLiveSetting([FromBody] CreateNewLiveSetting input)
        {
            var jwtObject = JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter);
            int FarmerId = (int)jwtObject["Id"];

            try
            {
                if (!ModelState.IsValid)
                {
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "欄位輸入格式不正確，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }

                YoutubeLive addboardcast = new YoutubeLive();
                UserCredential credentialoutput = addboardcast.CreateToken(input.accessToken);
                DateTime ytstarttime = new DateTime(input.liveDate.Date.Year, input.liveDate.Date.Month, input.liveDate.Date.Day, input.startTime.Hours, input.startTime.Minutes, input.startTime.Seconds);
                DateTime ytendtime = new DateTime(input.liveDate.Date.Year, input.liveDate.Date.Month, input.liveDate.Date.Day, input.endTime.Hours, input.endTime.Minutes, input.endTime.Seconds);
                string resultid = addboardcast.CreateYouTubeBroadcast(credentialoutput, input.liveName, input.liveName, ytstarttime, ytendtime);
                if (resultid != "error" && resultid.Length == 11)
                {
                    string youtubeliveurl = @"https://youtube.com/live/" + resultid;
                    var NewLiveSetting = new LiveSetting
                    {
                        LiveName = input.liveName,
                        LiveDate = input.liveDate.Date,
                        StartTime = input.startTime,
                        EndTime = input.endTime,
                        YTURL = youtubeliveurl,
                        ShareURL = youtubeliveurl.Substring(youtubeliveurl.LastIndexOf('/') + 1),
                        UserId = FarmerId,
                    };

                    db.LiveSettings.Add(NewLiveSetting);
                    db.SaveChanges();
                    int LiveSettingId = NewLiveSetting.Id;

                    var LiveProduct = input.liveproduct.Select(LP => new LiveProduct
                    {
                        IsTop = false,
                        LiveSettingId = LiveSettingId,
                        SpecId = db.Products.Where(x => x.Id == LP.productId).AsEnumerable().FirstOrDefault().Spec.Where(x => x.Size == Convert.ToBoolean(LP.productSize)).FirstOrDefault().Id,
                    }).ToList();

                    db.LiveProducts.AddRange(LiveProduct);
                    db.SaveChanges();

                    var LiveProductList = db.LiveProducts.Where(x => x.LiveSettingId == LiveSettingId)?.FirstOrDefault();
                    if (LiveProductList != null)
                    {
                        LiveProductList.IsTop = true;
                        db.SaveChanges();
                    }

                    foreach (var LP in input.liveproduct)
                    {
                        var specitem = db.Products.Where(x => x.Id == LP.productId).AsEnumerable().FirstOrDefault().Spec.Where(x => x.Size == Convert.ToBoolean(LP.productSize)).FirstOrDefault();
                        if (specitem != null)
                        {
                            specitem.LivePrice = LP.liveprice;
                        }
                    }
                    db.SaveChanges();

                    var searchLiveSetting = db.LiveSettings.Where(x => x.Id == LiveSettingId)?
                    .Select(liveSetting => new
                    {
                        LiveSetting = liveSetting,
                        LiveAlbum = db.LiveAlbum
                            .Where(album => album.LiveId == liveSetting.Id)
                            .OrderByDescending(album => album.CreateTime)
                            .FirstOrDefault()
                    })
                    .FirstOrDefault();

                    var resultReturn = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "新增成功",
                        data = new
                        {
                            liveId = searchLiveSetting.LiveSetting.Id,
                            liveName = searchLiveSetting.LiveSetting.LiveName,
                            liveDate = searchLiveSetting.LiveSetting.LiveDate.Date.ToString("yyyy/MM/dd"),
                            startTime = searchLiveSetting.LiveSetting.StartTime.ToString().Substring(0, 5),
                            endTime = searchLiveSetting.LiveSetting.EndTime.ToString().Substring(0, 5),
                            yturl = searchLiveSetting.LiveSetting.YTURL,
                            topProductId = searchLiveSetting.LiveSetting.LiveProduct.Where(x => x.IsTop == true).FirstOrDefault().Spec.Product.Id,
                            topProductSize = searchLiveSetting.LiveSetting.LiveProduct.Where(x => x.IsTop == true).FirstOrDefault().Spec.Size,
                            liveproduct = searchLiveSetting.LiveSetting.LiveProduct.Select(x => new
                            {
                                productId = db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().ProductId,
                                productName = db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().Product.ProductTitle,
                                productSize = db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().Size,
                                liveprice = db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().LivePrice,
                                liveproductId = x.Id,
                            }).ToList()
                        }
                    };
                    return Content(HttpStatusCode.OK, resultReturn);
                }
                else
                {
                    var result = new
                    {
                        statusCode = 402,
                        status = "error",
                        message = "加入youtube失敗，請確認youtube帳號已開啟Live功能",
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
        #endregion BFL-01 新增後台直播資訊(新增產品直播價)

        #region BFL-02 修改後台直播資訊(修改產品直播價)
        /// <summary>
        /// BFL-02 修改後台直播資訊(修改產品直播價)
        /// </summary>
        /// <param name="liveId">提供直播Id</param>
        /// <param name="input">提供直播資訊的 JSON 物件</param>
        /// <returns>返回直播資訊的 JSON 物件</returns>
        [HttpPut]
        [Route("api/livesetting/{liveId}")]
        [JwtAuthFilter]
        public IHttpActionResult ReviseLiveSetting([FromUri] int liveId, [FromBody] CreateNewLiveSetting input)
        {
            var jwtObject = JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter);
            int FarmerId = (int)jwtObject["Id"];

            try
            {
                if (!ModelState.IsValid)
                {
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "欄位輸入格式不正確，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }

                var searchLiveSetting = db.LiveSettings.Where(x => x.UserId == FarmerId && x.Id == liveId)?.FirstOrDefault();
                searchLiveSetting.LiveName = input.liveName;
                searchLiveSetting.LiveDate = input.liveDate;
                searchLiveSetting.StartTime = input.startTime;
                searchLiveSetting.EndTime = input.endTime;
                searchLiveSetting.YTURL = input.yturl;
                searchLiveSetting.ShareURL = input.yturl.Substring(input.yturl.LastIndexOf('/') + 1);

                db.SaveChanges();
                int LiveSettingId = searchLiveSetting.Id;

                for (int i = 0; i < searchLiveSetting.LiveProduct.Count; i++)
                {
                    // 改變liveproduct資料表中的產品SPEC
                    searchLiveSetting.LiveProduct.ToList()[i].SpecId =
                    db.Specs.AsEnumerable().Where(x => x.ProductId == input.liveproduct[i].productId && x.Size == Convert.ToBoolean(input.liveproduct[i].productSize)).FirstOrDefault().Id;
                    // 改變spec資料表中的產品價格
                    var ReviseSpecLivePrice = db.Products.AsEnumerable().Where(x => x.Id == input.liveproduct[i].productId).FirstOrDefault().Spec.Where(x => x.Size == Convert.ToBoolean(input.liveproduct[i].productSize)).FirstOrDefault();
                    if (ReviseSpecLivePrice != null)
                    {
                        ReviseSpecLivePrice.LivePrice = input.liveproduct[i].liveprice;
                    }
                }
                db.SaveChanges();

                // 如果新增了多筆資料，要透過input再新增進去
                if (searchLiveSetting.LiveProduct.Count < input.liveproduct.Count)
                {
                    for (int j = searchLiveSetting.LiveProduct.Count; j < input.liveproduct.Count; j++)
                    {
                        var LiveProduct = new LiveProduct
                        {
                            IsTop = false,
                            LiveSettingId = LiveSettingId,
                            SpecId = db.Products.AsEnumerable().Where(x => x.Id == input.liveproduct[j].productId).AsEnumerable().FirstOrDefault().Spec.Where(x => x.Size == Convert.ToBoolean(input.liveproduct[j].productSize)).FirstOrDefault().Id,
                        };
                        db.LiveProducts.Add(LiveProduct);
                        db.SaveChanges();
                    }
                }
                else if (searchLiveSetting.LiveProduct.Count > input.liveproduct.Count)
                {
                    var productsToDelete = searchLiveSetting.LiveProduct.Skip(input.liveproduct.Count); // 跳過前面的資料，取得最後兩筆資料
                    foreach (var product in productsToDelete)
                    {
                        db.LiveProducts.Remove(product);
                    }
                    db.SaveChanges();
                }

                var LiveProductListIsTop = db.LiveProducts.Where(x => x.LiveSettingId == LiveSettingId && x.IsTop == true)?.FirstOrDefault();
                if (LiveProductListIsTop == null)  //先判斷是否有置頂資料，如果有就保留設定，如果沒有就進行第一筆的預設
                {
                    // 前端不需要在此頁面加入置頂，因此後端選取第一個產品，做為置頂產品
                    var LiveProductList = db.LiveProducts.Where(x => x.LiveSettingId == LiveSettingId)?.FirstOrDefault();
                    if (LiveProductList != null)
                    {
                        LiveProductList.IsTop = true;
                        db.SaveChanges();
                    }
                }

                var GetUpdateLiveSetting = db.LiveSettings.AsEnumerable()
                    .Where(x => x.UserId == FarmerId && x.Id == liveId)?
                    .Select(liveSetting => new
                    {
                        LiveSetting = liveSetting,
                        LiveAlbum = db.LiveAlbum
                            .Where(album => album.LiveId == liveSetting.Id)
                            .OrderByDescending(album => album.CreateTime)
                            .FirstOrDefault()
                    })
                    .FirstOrDefault();
                var resultReviser = new
                {
                    statusCode = 200,
                    status = "success",
                    message = "修改成功",
                    data = new
                    {
                        liveId = GetUpdateLiveSetting.LiveSetting.Id,
                        liveName = GetUpdateLiveSetting.LiveSetting.LiveName,
                        liveDate = GetUpdateLiveSetting.LiveSetting.LiveDate.Date.ToString("yyyy/MM/dd"),
                        startTime = GetUpdateLiveSetting.LiveSetting.StartTime.ToString().Substring(0, 5),
                        endTime = GetUpdateLiveSetting.LiveSetting.EndTime.ToString().Substring(0, 5),
                        yturl = GetUpdateLiveSetting.LiveSetting.LivePic,
                        topProductId = GetUpdateLiveSetting.LiveSetting.LiveProduct.Where(x => x.IsTop == true).FirstOrDefault().Spec.Product.Id,
                        topProductSize = GetUpdateLiveSetting.LiveSetting.LiveProduct.Where(x => x.IsTop == true).FirstOrDefault().Spec.Size,
                        liveproudct = GetUpdateLiveSetting.LiveSetting.LiveProduct.Select(x => new
                        {
                            productId = db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().ProductId,
                            productName = db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().Product.ProductTitle,
                            productSize = db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().Size,
                            liveprice = db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().LivePrice,
                            liveproductId = x.Id,
                        }).ToList()
                    }
                };
                return Content(HttpStatusCode.OK, resultReviser);
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

        #endregion BFL-02 修改後台直播資訊(修改產品直播價)

        #region BFL-03 取得後台直播資訊(包含產品直播價)
        /// <summary>
        /// BFL-03 取得後台直播資訊(包含產品直播價)
        /// </summary>
        /// <param name="liveId">提供直播Id</param>
        /// <returns>返回直播資訊的 JSON 物件</returns>
        [HttpGet]
        [Route("api/livesetting/{liveId}")]
        [JwtAuthFilter]
        public IHttpActionResult RenderLiveSettingInfor(int liveId)
        {
            var jwtObject = JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter);
            int FarmerId = (int)jwtObject["Id"];

            try
            {
                var getUserLiveSetting = db.LiveSettings.AsEnumerable().Where(x => x.UserId == FarmerId && x.Id == liveId)?
                     .Select(liveSetting => new
                     {
                         LiveSetting = liveSetting,
                         LiveAlbum = db.LiveAlbum
                        .Where(album => album.LiveId == liveSetting.Id)
                        .OrderByDescending(album => album.CreateTime)
                        .FirstOrDefault()
                     })
                    .FirstOrDefault();

                var resultReturn = new
                {
                    statusCode = 200,
                    status = "success",
                    message = "取得成功",
                    data = new
                    {
                        liveId = getUserLiveSetting.LiveSetting.Id,
                        liveName = getUserLiveSetting.LiveSetting.LiveName,
                        liveDate = getUserLiveSetting.LiveSetting.LiveDate.ToString("yyyy/MM/dd"),
                        startTime = getUserLiveSetting.LiveSetting.StartTime.ToString().Substring(0, 5),
                        endTime = getUserLiveSetting.LiveSetting.EndTime.ToString().Substring(0, 5),
                        livepic = getUserLiveSetting.LiveAlbum?.Photo,
                        yturl = getUserLiveSetting.LiveSetting.YTURL,
                        topProductId = getUserLiveSetting.LiveSetting.LiveProduct.Where(x => x.IsTop == true).FirstOrDefault().Spec.Product.Id,
                        topProductSize = getUserLiveSetting.LiveSetting.LiveProduct.Where(x => x.IsTop == true).FirstOrDefault().Spec.Size,
                        liveproudct = getUserLiveSetting.LiveSetting.LiveProduct?.Select(x => new
                        {
                            productId = db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().ProductId,
                            productName = db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().Product.ProductTitle,
                            productSize = db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().Size,
                            liveprice = db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().LivePrice,
                            liveproductId = x.Id,
                        }).ToList()
                    }
                };
                return Content(HttpStatusCode.OK, resultReturn);
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

        #endregion BFL-03 取得後台直播資訊(包含產品直播價)

        #region BFL-05 上傳回拋直播單一圖片(單張，延遲渲染，有PUT功能)
        /// <summary>
        /// BFL-05 上傳回拋直播單一圖片(單張，延遲渲染，有PUT功能)
        /// </summary>
        /// <param name="liveId">提供直播Id</param>
        /// <returns>返回直播單一圖片</returns>
        [HttpPost]
        [Route("api/livesetting/pic/{liveId}")]
        [JwtAuthFilter]
        public async Task<IHttpActionResult> UploadSingleLiveEnevtPhoto(int liveId)
        {
            var jwtObject = JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter);
            int FarmerId = (int)jwtObject["Id"];
            var userExist = db.Users.Any(u => u.Id == FarmerId);

            // 檢查請求是否包含 multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var checkliveevent = db.LiveSettings.Where(x => x.Id == liveId)?.FirstOrDefault();
            if (checkliveevent == null)
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
                string root = HttpContext.Current.Server.MapPath($"~/upload/livesetting/{liveId}");
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
                    foreach (var content in provider.Contents) // 檢查附檔名類型
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
                        // 定義檔案名稱
                        string fileName = FarmerId.ToString() + liveId.ToString() + DateTime.Now.ToString("yyyyMMddHHmmssfff") + fileType;
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
                                    // 設定裁切尺寸
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
                            // 儲存後的圖片
                            image.Save(outputPath);
                        }
                        //加入至List
                        imglList.Add(fileName);
                        string url = WebConfigurationManager.AppSettings["Serverurl"].ToString() + $"/upload/livesetting/{liveId}/" + fileName;
                        imglList.Add(url);
                    }
                    var checklivecover = db.LiveAlbum.Where(x => x.UserId == FarmerId && x.LiveId == liveId)?.FirstOrDefault();
                    if (checklivecover != null)
                    {
                        checklivecover.Photo = imglList[1];
                        db.SaveChanges();
                    }
                    else
                    {
                        var newCover = new LiveAlbum
                        {
                            UserId = FarmerId,
                            LiveId = liveId,
                            Photo = imglList[1],
                        };
                        db.LiveAlbum.Add(newCover);
                        db.SaveChanges();
                    }
                    //撈取相片資料庫
                    var checkliveeventupdate = db.LiveSettings.Where(x => x.Id == liveId)?.FirstOrDefault();
                    var checklivecoverupdate = db.LiveAlbum.Where(x => x.UserId == FarmerId && x.LiveId == liveId)?.FirstOrDefault();

                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "上傳成功",
                        data = new
                        {
                            photoId = checklivecoverupdate.Id,
                            src = checklivecoverupdate.Photo,
                            alt = checklivecoverupdate.Photo.Substring(checklivecoverupdate.Photo.LastIndexOf('/') + 1),
                        },
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                catch
                {
                    var result = new
                    {
                        statusCode = 200,
                        status = "error",
                        message = "其他錯誤訊息"
                    };
                    return Content(HttpStatusCode.OK, result);
                }
            }
        }
        #endregion BFL-05 上傳回拋直播單一圖片(單張，延遲渲染，有PUT功能)

        #region BFL-06 取得小農自有直播清單
        /// <summary>
        /// BFL-06 取得小農自有直播清單
        /// </summary>
        /// <param></param>
        /// <returns>返回直播清單</returns>
        [HttpGet]
        [Route("api/farmer/livelist")]
        [JwtAuthFilter]
        public IHttpActionResult Getfarmerproductlist()
        {
            try
            {
                int farmerId = Convert.ToInt16(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);
                var searchlivelist = db.LiveSettings
                    .Where(x => x.UserId == farmerId)?
                    .OrderByDescending(y => y.LiveDate).AsEnumerable();

                if (searchlivelist == null)  // 沒有建立直播
                {
                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "沒有結果",
                        data = new object[] { }
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else   // 已經有建立直播
                {
                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "取得成功",
                        data = searchlivelist?.Select(live => new
                        {
                            liveId = live.Id,
                            liveName = live.LiveName,
                            liveDate = live.LiveDate.ToString("yyyy/MM/dd"),
                            startTime = live.StartTime.ToString().Substring(0, 5),
                            yturl = live.YTURL,
                            shareurl = live.ShareURL,
                            topLiveProductId = live.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Id,
                            topProductName = live.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Id == null ? null :
                                            live.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Spec.Size == true ?
                                            live.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Spec.Product.ProductTitle + "(大)" :
                                            live.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Spec.Product.ProductTitle + "(小)",
                            liveProudct = live.LiveProduct?.Select(x => new
                            {
                                liveProductId = x.Id,
                                liveProductName = db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().Size == true ?
                                                         db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().Product.ProductTitle + "(大)" :
                                                         db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().Product.ProductTitle + "(小)",
                                liveProductPhoto = db.Albums.Where(y => y.ProductId == x.Spec.Product.Id)?.FirstOrDefault()?.Photo?.FirstOrDefault()?.URL,
                                liveProductPrice = db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().LivePrice,
                                liveProductStock = db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().Stock,
                            }).ToList()
                        })
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
        #endregion BFL-06 取得小農自有直播清單

        #region BFL-07 修改直播置頂商品
        /// <summary>
        /// BFL-07 修改直播置頂商品
        /// </summary>
        /// <param name="liveId">提供直播Id</param>
        /// <param name="liveProductId">提供直播ProductId</param>
        /// <returns>返回直播置頂商品的資訊</returns>
        [HttpPut]
        [Route("api/farmer/livelist/{liveId}/{liveProductId}")]
        [JwtAuthFilter]
        public IHttpActionResult ReviseLiveProduct([FromUri] int liveId, [FromUri] int liveProductId)
        {
            try
            {
                int farmerId = Convert.ToInt16(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);

                // 取消目前的置頂商品
                var currentliveproduct = db.LiveProducts.Where(x => x.LiveSettingId == liveId && x.IsTop == true)?.FirstOrDefault();
                if (currentliveproduct != null)
                {
                    currentliveproduct.IsTop = false;
                    db.SaveChanges();
                }
                // 重設定新的置頂商品
                var newliveproduct = db.LiveProducts.Where(x => x.LiveSettingId == liveId && x.Id == liveProductId)?.FirstOrDefault();
                if (newliveproduct == null)
                {
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "更新失敗，沒有此live產品Id",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    newliveproduct.IsTop = true;
                    db.SaveChanges();

                    var livedate = db.LiveSettings.Where(x => x.Id == liveId)?.FirstOrDefault();
                    if (livedate != null)
                    {
                        var result = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "更新成功",
                            data = new
                            {
                                liveId = livedate.Id,
                                liveName = livedate.LiveName,
                                liveDate = livedate.LiveDate.ToString("yyyy/MM/dd"),
                                startTime = livedate.StartTime.ToString().Substring(0, 5),
                                yturl = livedate.YTURL,
                                shareurl = livedate.ShareURL,
                                topLiveProductId = livedate.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Id,
                                topProductName = livedate.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Id == null ? null :
                                                livedate.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Spec.Size == true ?
                                                livedate.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Spec.Product.ProductTitle + "(大)" :
                                                livedate.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Spec.Product.ProductTitle + "(小)",
                                liveProudct = livedate.LiveProduct?.Select(x => new
                                {
                                    liveProductId = x.Id,
                                    liveProductName = x.Spec.Size == true ?
                                                x.Spec.Product.ProductTitle + "(大)" :
                                                x.Spec.Product.ProductTitle + "(小)",
                                }).ToList()
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
                            message = "更新失敗，沒有此liveId",
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
        #endregion BFL-07 修改直播置頂商品

        #region BFL-12 取得google帳號授權網址
        /// <summary>
        /// BFL-12 取得google帳號授權網址
        /// </summary>
        /// <param></param>
        /// <returns>返回 google 帳號授權網址</returns>
        [HttpGet]
        [Route("api/livesetting/google")]
        [JwtAuthFilter]
        public IHttpActionResult getgoogleoauth2link()
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
                string redirecturi = @"https://sun-live.vercel.app/dashboard/live/verify";
                var authorizationUrl = flow.CreateAuthorizationCodeRequest(redirecturi);
                authorizationUrl.Scope = @"https://www.googleapis.com/auth/youtube";
                Uri authUrl = authorizationUrl.Build();

                if (authUrl != null)
                {
                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "取得成功",
                        url = authUrl.ToString(),
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
        #endregion BFL-12 取得google帳號授權網址

        #region BFL-13 驗證Oauth2並回傳Token
        /// <summary>
        /// BFL-13 驗證Oauth2並回傳Token
        /// </summary>
        /// <param name="inputs">提供登入所需的 code</param>
        /// <returns>返回 AccessToken</returns>
        [HttpPost]
        [Route("api/livesetting/authcode")]
        public async Task<IHttpActionResult> codeturntotoken(OauthTwo inputs)
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

                string redirecturi = @"https://sun-live.vercel.app/dashboard/live/verify";
                string decodedCode = HttpUtility.UrlDecode(inputs.code);
                var tokenResponse = await flow.ExchangeCodeForTokenAsync("user", decodedCode, redirecturi, CancellationToken.None);

                if (tokenResponse != null)
                {
                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "取得成功",
                        token = tokenResponse.AccessToken.ToString(),
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
        #endregion BFL-13 驗證Oauth2並回傳Token

        public class OauthTwo
        {
            public string code { get; set; }
        }
    }