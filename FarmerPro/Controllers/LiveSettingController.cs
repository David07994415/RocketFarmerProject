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

namespace FarmerPro.Controllers
{
    public class LiveSettingController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        #region BFL-1 新增後台直播資訊(新增產品直播價)
        [HttpPost]
        [Route("api/livesetting")]
        [JwtAuthFilter]
        public  IHttpActionResult CreateNewLiveSetting([FromBody] CreateNewLiveSetting input)
        {
            // 解密後會回傳 Json 格式的物件 (即加密前的資料)
            var jwtObject = JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter);
            int FarmerId = (int)jwtObject["Id"];

            //try
            //{
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

            //bool HasPhotoFile = false;
            //List<string> imglList = new List<string>();
            //// 檢查請求是否包含 multipart/form-data.
            //if (Request.Content.IsMimeMultipartContent())
            //{
            //    HasPhotoFile= true;
            //    string root = HttpContext.Current.Server.MapPath($"~/upload/livepic/{FarmerId}");
            //    if (!Directory.Exists(root))
            //    {
            //        Directory.CreateDirectory(root);
            //    }
            //    // 讀取 MIME 資料
            //    var provider = new MultipartMemoryStreamProvider();
            //    await Request.Content.ReadAsMultipartAsync(provider);
            //    //遍歷 provider.Contents 中的每個 content，處理多個圖片檔案
            //    foreach (var content in provider.Contents)
            //    {

            //        // 取得檔案副檔名
            //        string fileNameData = content.Headers.ContentDisposition.FileName.Trim('\"');
            //        string fileType = fileNameData.Remove(0, fileNameData.LastIndexOf('.')); // .jpg

            //        // 定義檔案名稱
            //        string fileName = FarmerId.ToString()  + DateTime.Now.ToString("yyyyMMddHHmmssfff") + fileType;

            //        // 儲存圖片
            //        var fileBytes = await content.ReadAsByteArrayAsync();
            //        var outputPath = Path.Combine(root, fileName);
            //        using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            //        {
            //            await output.WriteAsync(fileBytes, 0, fileBytes.Length);
            //        }

            //        //// 載入原始圖片，直接存入伺服器(未裁切)
            //        //using (var image = Image.Load<Rgba32>(outputPath))
            //        //{
            //        //    // 儲存裁切後的圖片
            //        //    image.Save(outputPath);
            //        //}

            //        // 載入原始圖片，調整圖片大小
            //        using (var image = Image.Load<Rgba32>(outputPath))
            //        {

            //            // 設定最大檔案大小 (2MB)
            //            long maxFileSizeInBytes = 2 * 1024 * 1024;

            //            // 計算目前圖片的檔案大小
            //            using (var memoryStream = new MemoryStream())
            //            {
            //                image.Save(memoryStream, new JpegEncoder());
            //                long currentFileSize = memoryStream.Length;

            //                // 檢查檔案大小是否超過限制
            //                if (currentFileSize > maxFileSizeInBytes)
            //                {

            //                    // 如果超過，可能需要進一步調整，或者進行其他處理
            //                    // 這裡僅僅是一個簡單的示例，實際應用可能需要更複雜的處理邏輯
            //                    //// 設定裁切尺寸
            //                    int MaxWidth = 800;   // 先設定800px
            //                    int MaxHeight = 600;  // 先設定600px

            //                    // 裁切圖片
            //                    image.Mutate(x => x.Resize(new ResizeOptions
            //                    {
            //                        Size = new Size(MaxWidth, MaxHeight),
            //                        Mode = ResizeMode.Max
            //                    }));

            //                }
            //                else { }
            //            }
            //            // 儲存後的圖片
            //            image.Save(outputPath);
            //        }

            //        //加入至List
            //        //imglList.Add(fileName);
            //        string url = WebConfigurationManager.AppSettings["Serverurl"].ToString() + $"/upload/livepic/{FarmerId}" + fileName;
            //        imglList.Add(url);
            //    }
            //}
            DateTime ytstarttime = new DateTime(input.liveDate.Date.Year, input.liveDate.Date.Month, input.liveDate.Date.Day, input.startTime.Hours, input.startTime.Minutes, input.startTime.Seconds);
            DateTime ytendtime = new DateTime(input.liveDate.Date.Year, input.liveDate.Date.Month, input.liveDate.Date.Day, input.endTime.Hours, input.endTime.Minutes, input.endTime.Seconds);

            YoutubeLive addboardcast=new YoutubeLive();
            string resultid = addboardcast.CreateYouTubeBroadcast(input.liveName, input.liveName,  ytstarttime,  ytendtime);
            if (resultid != "error") 
            {
                string youtubeliveurl = @"https://youtube.com/live/" + resultid;
                var NewLiveSetting = new LiveSetting
                {
                    LiveName = input.liveName,
                    LiveDate = input.liveDate.Date,
                    StartTime = input.startTime,
                    EndTime = input.endTime,
                    YTURL = youtubeliveurl,
                    //LivePic= HasPhotoFile? imglList[0]:null,
                    ShareURL = youtubeliveurl.Substring(youtubeliveurl.LastIndexOf('/') + 1), //這邊可能要改
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

                //這邊要加上設定置頂specid。因為前端不需要在此頁面加入置頂，所以先隱藏，換成下面那一段
                //int topspecId = db.Products.Where(x => x.Id == input.topProductId).FirstOrDefault().Spec.AsEnumerable().Where(y => y.Size == Convert.ToBoolean(input.topProductSize)).FirstOrDefault().Id;
                //var TopProductSetting = db.LiveProducts.Where(x => x.LiveSettingId == LiveSettingId && x.SpecId == topspecId)?.FirstOrDefault();
                //if (TopProductSetting != null) 
                //{
                //    TopProductSetting.IsTop = true;
                //    db.SaveChanges();
                //}
                //前端不需要在此頁面加入置頂，因此後端選取第一個產品，做為置頂產品
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
                .Select(liveSetting => new {
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
                        //livePic= searchLiveSetting.LiveAlbum?.Photo,
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
                    message = "加入yotube失敗",
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

        #region BFL-2 修改後台直播資訊(修改產品直播價)
        [HttpPut]
        [Route("api/livesetting/{liveId}")]
        [JwtAuthFilter]
        public IHttpActionResult ReviseLiveSetting([FromUri]int liveId, [FromBody] CreateNewLiveSetting input)
        {
            // 解密後會回傳 Json 格式的物件 (即加密前的資料)
            var jwtObject = JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter);
            int FarmerId = (int)jwtObject["Id"];

            //try
            //{
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


            var searchLiveSetting = db.LiveSettings.Where(x => x.UserId == FarmerId && x.Id== liveId)?.FirstOrDefault();
            DateTime ytstarttime = new DateTime(input.liveDate.Date.Year, input.liveDate.Date.Month, input.liveDate.Date.Day, input.startTime.Hours, input.startTime.Minutes, input.startTime.Seconds);
            DateTime ytendtime = new DateTime(input.liveDate.Date.Year, input.liveDate.Date.Month, input.liveDate.Date.Day, input.endTime.Hours, input.endTime.Minutes, input.endTime.Seconds);

            YoutubeLive updateboardcast = new YoutubeLive();
            string resultid = updateboardcast.UpdateYouTubeBroadcast(input.liveName, input.liveName, ytstarttime, ytendtime, searchLiveSetting.ShareURL);
            if (resultid != "error")
            {
                string youtubeliveurl = @"https://youtube.com/live/" + resultid;
                searchLiveSetting.LiveName = input.liveName;
                searchLiveSetting.LiveDate = input.liveDate;
                searchLiveSetting.StartTime = input.startTime;
                searchLiveSetting.EndTime = input.endTime;
                searchLiveSetting.YTURL = youtubeliveurl;
                searchLiveSetting.ShareURL = youtubeliveurl.Substring(youtubeliveurl.LastIndexOf('/') + 1); //這邊可能要改
                //圖片部分透過另外一支API處理
                db.SaveChanges();
                int LiveSettingId = searchLiveSetting.Id;

                for (int i = 0; i < searchLiveSetting.LiveProduct.Count; i++)
                {
                    //改變liveproduct資料表中的產品SPEC
                    searchLiveSetting.LiveProduct.ToList()[i].SpecId =
                        db.Specs.AsEnumerable().Where(x => x.ProductId == input.liveproduct[i].productId && x.Size == Convert.ToBoolean(input.liveproduct[i].productSize)).FirstOrDefault().Id;
                    //改變spec資料表中的產品價格
                    var ReviseSpecLivePrice = db.Products.AsEnumerable().Where(x => x.Id == input.liveproduct[i].productId).FirstOrDefault().Spec.Where(x => x.Size == Convert.ToBoolean(input.liveproduct[i].productSize)).FirstOrDefault();
                    if (ReviseSpecLivePrice != null)
                    {
                        ReviseSpecLivePrice.LivePrice = input.liveproduct[i].liveprice;
                    }
                }
                db.SaveChanges();

                //如果新增了多筆資料，要透過put再新增進去
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

                ////這裡要先清除已經設定置頂的specid。因為前端不需要在此頁面加入置頂，所以先隱藏，換成下面那一段
                //var CurrentTopProductSetting = db.LiveProducts.Where(x => x.LiveSettingId == LiveSettingId && x.IsTop == true)?.FirstOrDefault();
                //if (CurrentTopProductSetting != null)
                //{
                //    CurrentTopProductSetting.IsTop = false;
                //    db.SaveChanges();
                //}
                ////這邊要加上設定置頂specid。因為前端不需要在此頁面加入置頂，所以先隱藏，換成下面那一段
                //int topspecId = db.Products.Where(x => x.Id == input.topProductId).FirstOrDefault().Spec.AsEnumerable().Where(y => y.Size == Convert.ToBoolean(input.topProductSize)).FirstOrDefault().Id;
                //var TopProductSetting = db.LiveProducts.Where(x => x.LiveSettingId == LiveSettingId && x.SpecId == topspecId)?.FirstOrDefault();
                //if (TopProductSetting != null)
                //{
                //    TopProductSetting.IsTop = true;
                //    db.SaveChanges();
                //}

                //這裡要先清除已經設定置頂的specid。
                var LiveProductListIsTop = db.LiveProducts.Where(x => x.LiveSettingId == LiveSettingId && x.IsTop == true)?.FirstOrDefault();
                if (LiveProductListIsTop == null)  //這裡要先判斷是否有置頂資料，如果有就保留設定，如果沒有就進行第一筆的預設
                {
                    //前端不需要在此頁面加入置頂，因此後端選取第一個產品，做為置頂產品
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
                        //livePic = GetUpdateLiveSetting.LiveAlbum?.Photo,   //要補上livePic
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
            else 
            {
                var result = new
                {
                    statusCode = 402,
                    status = "error",
                    message = "更新yotube失敗",
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

        #region BFL-3 取得後台直播資訊(包含產品直播價)
        [HttpGet]
        [Route("api/livesetting/{liveId}")]
        [JwtAuthFilter]
        public IHttpActionResult RenderLiveSettingInfor(int liveId)
        {
            // 解密後會回傳 Json 格式的物件 (即加密前的資料)
            var jwtObject = JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter);
            int FarmerId = (int)jwtObject["Id"];

            try
            {
                var getUserLiveSetting = db.LiveSettings.AsEnumerable().Where(x => x.UserId == FarmerId && x.Id== liveId)?
                     .Select(liveSetting => new {
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
                        startTime = getUserLiveSetting.LiveSetting.StartTime.ToString().Substring(0,5),
                        endTime = getUserLiveSetting.LiveSetting.EndTime.ToString().Substring(0, 5),
                        livepic= getUserLiveSetting.LiveAlbum?.Photo,
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
        #endregion

        #region BFL-05 上傳回拋直播單一圖片(單張，延遲渲染，有PUT功能)
        [HttpPost]
        [Route("api/livesetting/pic/{liveId}")]
        [JwtAuthFilter]
        public async Task<IHttpActionResult> UploadSingleLiveEnevtPhoto(int liveId)
        {
            // 解密後會回傳 Json 格式的物件 (即加密前的資料)
            var jwtObject = JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter);
            int FarmerId = (int)jwtObject["Id"];

            var userExist = db.Users.Any(u => u.Id == FarmerId);

            //if (userExist)//使用者存在
            //{
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
                            string fileName = FarmerId.ToString() + liveId.ToString() + DateTime.Now.ToString("yyyyMMddHHmmssfff") + fileType;

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
                            string url = WebConfigurationManager.AppSettings["Serverurl"].ToString() + $"/upload/livesetting/{liveId}/" + fileName;
                            imglList.Add(url);
                        }
                        var checklivecover = db.LiveAlbum.Where(x => x.UserId == FarmerId && x.LiveId == liveId)?.FirstOrDefault();
                        if (checklivecover != null)
                        {
                            //可以補上清空資料夾的特定圖片資料
                            checklivecover.Photo = imglList[1];
                            //可以補上照片名稱的資料庫欄位
                            db.SaveChanges();
                        }
                        else
                        {
                            var newCover = new LiveAlbum
                            {
                                UserId = FarmerId,
                                LiveId = liveId,
                                Photo = imglList[1],
                                //可以補上照片名稱的資料庫欄位
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
            //}
        }
        #endregion

        #region BFL-06 取得小農自有直播清單
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
                    .OrderBy(y => y.LiveDate).AsEnumerable();


                if (searchlivelist == null)
                {
                    // 沒有建立直播
                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "沒有結果",
                        data = new object[] { }
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    // 已經有建立直播
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
                            yturl = live.YTURL,             //和shareurl 二擇一使用
                            shareurl = live.ShareURL,  //和youtubeurl  二擇一使用
                            topLiveProductId = live.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Id,
                            topProductName = live.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Id == null ? null :
                                            live.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Spec.Size==true?
                                            live.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Spec.Product.ProductTitle + "(大)" :
                                            live.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Spec.Product.ProductTitle + "(小)",
                            liveProudct = live.LiveProduct?.Select(x => new
                            {
                                liveProductId = x.Id,
                                liveProductName = db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().Size == true ?
                                                         db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().Product.ProductTitle + "(大)" :
                                                         db.Specs.Where(y => y.Id == x.SpecId)?.FirstOrDefault().Product.ProductTitle + "(小)",
                            }).ToList()
                        })
                    };
                    return Content(HttpStatusCode.OK, result);
                }
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
        #endregion
        //WHERE條件要加上IsDelete==false
        //於此頁面調整置頂產品，需要額外一隻API

        #region BFL-07 修改直播置頂商品
        [HttpPut]
        [Route("api/farmer/livelist/{liveId}/{liveProductId}")]
        [JwtAuthFilter]
        public IHttpActionResult ReviseLiveProduct([FromUri]int liveId, [FromUri] int liveProductId)
        {
            //try
            //{
                int farmerId = Convert.ToInt16(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);

                //先取消目前的置頂商品，
                var currentliveproduct = db.LiveProducts.Where(x => x.LiveSettingId == liveId && x.IsTop == true)?.FirstOrDefault();
                if (currentliveproduct != null) 
                {
                    currentliveproduct.IsTop = false;
                    db.SaveChanges();
                }

                //先設定新的置頂商品，
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
                                yturl = livedate.YTURL,             //和shareurl 二擇一使用
                                shareurl = livedate.ShareURL,  //和youtubeurl  二擇一使用
                                topLiveProductId = livedate.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Id,
                                topProductName = livedate.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Id==null?null:
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
        #endregion
        //WHERE條件要加上IsDelete==false


        //需要再補上一隻刪除特定直播的api、需要再補上一隻刪除特定直播圖片的api


    }
}
