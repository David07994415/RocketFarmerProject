using FarmerPro.Models;
using FarmerPro.Securities;
using NSwag.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace FarmerPro.Controllers
{
    [OpenApiTag("Order", Description = "訂單及金流")]
    public class UserOrderController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        #region BGO-01 取得消費者自有訂單清單

        /// <summary>
        /// BGO-01 取得消費者自有訂單清單
        /// </summary>
        /// <param></param>
        /// <returns>返回消費者訂單清單的 JSON 物件</returns>
        [HttpGet]
        [Route("api/user/orderlist")]
        [JwtAuthFilter]
        public IHttpActionResult GetUserOrder()
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
                    var orderInfo = db.Orders.AsEnumerable()
                                .Where(o => o.UserId == CustomerId && o.IsPay == true)
                                .OrderByDescending(o => o.CreatTime)
                                .Select(o => new
                                {
                                    orderId = o.Id,
                                    farmerNickName = o.OrderDetail.Select(od => od.Spec.Product.Users.NickName).FirstOrDefault(),
                                    orderSum = (int)o.OrderSum,
                                    creatTime = o.CreatTime.ToString("yyyy.MM.dd"),
                                    shipment = o.Shipment
                                }).ToList();

                    if (!orderInfo.Any())
                    {
                        var getOrder = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "沒有訂單資料",
                            data = new object[] { }
                        };
                        return Content(HttpStatusCode.OK, getOrder);
                    }
                    else
                    {
                        var result = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "取得成功",
                            data = orderInfo.ToList()
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

        #endregion BGO-01 取得消費者自有訂單清單
    }
}