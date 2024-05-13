using FarmerPro.Models;
using FarmerPro.Models.ViewModel;
using NSwag.Annotations;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Security.Policy;
using System.Web.Http;

namespace FarmerPro.Controllers
{
    [OpenApiTag("Live", Description = "直播內容及設定")]
    public class LiveController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        #region FCL-01 取得目前直播內容(包含取得近期直播內容)
        /// <summary>
        /// FCL-01 取得目前直播內容(包含取得近期直播內容)
        /// </summary>
        /// <param></param>
        /// <returns>返回目前直播內容</returns>
        [HttpGet]
        [Route("api/live/")]
        public IHttpActionResult RenderLiveSession()
        {
            try
            {
                var CurrentLiveEvent = db.LiveSettings.AsEnumerable()
                    .Where(x => x.Id == 47)  // Fix to render the data
                                             //.Where(x => x.LiveDate.Date == DateTime.Now.Date && (x.StartTime.Hours <= DateTime.Now.Hour && x.StartTime.Minutes <= DateTime.Now.Minute &&  x.EndTime.Hours > DateTime.Now.Hour && x.EndTime.Minutes <= DateTime.Now.Minute))
                    .Select(liveSetting => new
                    {
                        LiveSetting = liveSetting,
                        LiveAlbum = db.LiveAlbum
                                           .Where(album => album.LiveId == liveSetting.Id)
                                           .OrderByDescending(album => album.CreateTime)
                                           .FirstOrDefault()
                    })
                    .FirstOrDefault();

                var UpcomingLiveEvent = db.LiveSettings.AsEnumerable()
                    .Where(x => x.LiveDate.Date >= new DateTime(2024, 03, 09))  // Fix to render the data
                                                                                //.Where(x => (x.LiveDate.Date == DateTime.Now.Date && x.StartTime.Hours > DateTime.Now.Hour) || x.LiveDate.Date > DateTime.Now.Date)
                    .OrderBy(x => x.LiveDate)
                    .Select(liveSetting => new
                    {
                        LiveSetting = liveSetting,
                        LiveAlbum = db.LiveAlbum
                        .Where(album => album.LiveId == liveSetting.Id)
                        .OrderByDescending(album => album.CreateTime)
                        .FirstOrDefault()
                    })
                    .Take(6);

                if (CurrentLiveEvent == null)  // 若現正沒有直播場次
                {
                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "沒有直播",
                        data = new
                        {
                            upcomingLive = UpcomingLiveEvent?.Select(eventItem => new
                            {
                                liveId = eventItem.LiveSetting.Id,
                                liveProductId = eventItem.LiveSetting.LiveProduct.FirstOrDefault()?.Spec.Product.Id,
                                liveProductName = eventItem.LiveSetting.LiveProduct.FirstOrDefault()?.Spec.Product.ProductTitle,
                                livePrice = eventItem.LiveSetting.LiveProduct.FirstOrDefault()?.Spec.LivePrice,
                                liveFarmer = eventItem.LiveSetting.LiveProduct.FirstOrDefault()?.Spec.Product.Users.NickName,
                                liveFarmerPic = eventItem.LiveSetting.LiveProduct.FirstOrDefault()?.Spec.Product.Users.Photo,
                                livePic = eventItem.LiveAlbum?.Photo,
                                liveTime = eventItem.LiveSetting.LiveDate.Date.ToString("yyyy.MM.dd") + " " + SwitchDayofWeek(eventItem.LiveSetting.LiveDate.DayOfWeek) + " " + eventItem.LiveSetting.StartTime.ToString().Substring(0, 5),
                            }).ToList()
                        }
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else  // 若現正有直播
                {
                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "有直播資料",
                        data = new
                        {
                            liveId = CurrentLiveEvent.LiveSetting.Id,
                            liveFarmerId = db.Users.Where(x => x.Id == CurrentLiveEvent.LiveSetting.UserId)?.FirstOrDefault()?.Id,
                            liveProductId = CurrentLiveEvent.LiveSetting.LiveProduct.FirstOrDefault()?.Spec.Product.Id,
                            liveProductName = CurrentLiveEvent.LiveSetting.LiveProduct.FirstOrDefault()?.Spec.Product.ProductTitle,
                            livePrice = CurrentLiveEvent.LiveSetting.LiveProduct.FirstOrDefault()?.Spec.LivePrice,
                            description = CurrentLiveEvent.LiveSetting.LiveProduct.FirstOrDefault()?.Spec.Product.Description,
                            upcomingLive = UpcomingLiveEvent?.Select(eventItem => new
                            {
                                liveId = eventItem.LiveSetting.Id,
                                liveProductId = eventItem.LiveSetting.LiveProduct.FirstOrDefault()?.Spec.Product.Id,
                                liveProductName = eventItem.LiveSetting.LiveProduct.FirstOrDefault()?.Spec.Product.ProductTitle,
                                livePrice = eventItem.LiveSetting.LiveProduct.FirstOrDefault()?.Spec.LivePrice,
                                liveFarmer = eventItem.LiveSetting.LiveProduct.FirstOrDefault()?.Spec.Product.Users.NickName,
                                liveFarmerPic = eventItem.LiveSetting.LiveProduct.FirstOrDefault()?.Spec.Product.Users.Photo,
                                livePic = eventItem.LiveAlbum?.Photo,
                                liveTime = eventItem.LiveSetting.LiveDate.Date.ToString("yyyy.MM.dd") + " " + SwitchDayofWeek(eventItem.LiveSetting.LiveDate.DayOfWeek) + " " + eventItem.LiveSetting.StartTime.ToString().Substring(0, 5),
                            }).ToList()
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

        #endregion FCL-01 取得目前直播內容(包含取得近期直播內容)

        #region FCL-02 取得特定直播場次內容(包含庫存資料)
        /// <summary>
        /// FCL-02 取得特定直播場次內容(包含庫存資料)
        /// </summary>
        /// <param name="liveId">提供直播Id</param>
        /// <returns>返回特定直播的 JSON 物件</returns>
        [HttpGet]
        [Route("api/live/{liveId}")]
        public IHttpActionResult RenderLiveEvent(int liveId)
        {
            try
            {
                var LiveEvent = db.LiveSettings.Where(x => x.Id == liveId)?.AsEnumerable().FirstOrDefault();
                if (LiveEvent == null)
                {
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "沒有此直播Id",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    if (LiveEvent.Id == 47) // Fix for rendering data
                    //if (LiveEvent.LiveDate.Date == DateTime.Now.Date && (LiveEvent.StartTime.Hours <= DateTime.Now.Hour && LiveEvent.StartTime.Minutes <= DateTime.Now.Minute && LiveEvent.EndTime.Hours > DateTime.Now.Hour && LiveEvent.EndTime.Minutes <= DateTime.Now.Minute))
                    {
                        var topproduct = db.LiveProducts.Where(x => x.LiveSettingId == liveId && x.IsTop == true)?.FirstOrDefault();
                        int topproductspecId = 0;
                        if (topproduct != null) { topproductspecId = topproduct.Spec.ProductId; };
                        var result = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "取得成功",
                            data = new
                            {
                                liveId = LiveEvent.Id,
                                yturl = LiveEvent.ShareURL,
                                liveName = LiveEvent.LiveName,
                                liveFarmerId = db.Users.Where(x => x.Id == LiveEvent.UserId)?.FirstOrDefault()?.Id,
                                liveFarmer = db.Users.Where(x => x.Id == LiveEvent.UserId)?.FirstOrDefault()?.NickName == null ?
                                                       db.Users.Where(x => x.Id == LiveEvent.UserId)?.FirstOrDefault()?.Account.ToString().Substring(0, 2) + "小農"
                                                       : db.Users.Where(x => x.Id == LiveEvent.UserId)?.FirstOrDefault()?.NickName,
                                liveFarmerPic = db.Users.Where(x => x.Id == LiveEvent.UserId)?.FirstOrDefault()?.Photo,
                                liveDate = LiveEvent.LiveDate.Date.ToString("yyyy.MM.dd") + " " + SwitchDayofWeek(LiveEvent.LiveDate.DayOfWeek) + " " + LiveEvent.StartTime.ToString().Substring(0, 5),
                                endTime = LiveEvent.EndTime.ToString().Substring(0, 5),
                                liveDescription = db.Users.Where(x => x.Id == LiveEvent.UserId)?.FirstOrDefault()?.Description,
                                topProductId = LiveEvent.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Spec.ProductId,
                                topProductName = db.Products.Where(x => x.Id == topproductspecId)?.FirstOrDefault()?.ProductTitle,
                                topSpecId = LiveEvent.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.SpecId,
                                topProductStock = LiveEvent.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Spec.Stock,
                                topProductLivePrice = LiveEvent.LiveProduct.Where(x => x.IsTop == true)?.FirstOrDefault()?.Spec.LivePrice,
                                topProductPhoto = db.Albums.Where(x => x.ProductId == topproductspecId)?.FirstOrDefault()?.Photo?.FirstOrDefault()?.URL,
                                liveProductList = LiveEvent.LiveProduct.Select(y => new
                                {
                                    productId = y.Spec?.ProductId,
                                    productName = db.Products.Where(k => k.Id == y.Spec.ProductId)?.FirstOrDefault().ProductTitle,
                                    specId = y.SpecId,
                                    productStock = y.Spec?.Stock,
                                    productLivePrice = y.Spec?.LivePrice,
                                    productOriginPrice = y.Spec?.Price,
                                    productPhoto = db.Albums.Where(z => z.ProductId == y.Spec.ProductId)?.FirstOrDefault()?.Photo?.FirstOrDefault()?.URL,
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
                            message = "此直播Id目前尚未到達直播時間",
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
        #endregion FCL-02 取得特定直播場次內容(包含庫存資料)

        public string SwitchDayofWeek(DayOfWeek input)
        {
            string chineseDayOfWeek = "";
            switch (input)
            {
                case DayOfWeek.Sunday:
                    chineseDayOfWeek = "(日)";
                    break;

                case DayOfWeek.Monday:
                    chineseDayOfWeek = "(一)";
                    break;

                case DayOfWeek.Tuesday:
                    chineseDayOfWeek = "(二)";
                    break;

                case DayOfWeek.Wednesday:
                    chineseDayOfWeek = "(三)";
                    break;

                case DayOfWeek.Thursday:
                    chineseDayOfWeek = "(四)";
                    break;

                case DayOfWeek.Friday:
                    chineseDayOfWeek = "(五)";
                    break;

                case DayOfWeek.Saturday:
                    chineseDayOfWeek = "(六)";
                    break;
            }
            return chineseDayOfWeek.ToString();
        }
    }
}