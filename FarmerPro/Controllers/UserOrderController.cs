using FarmerPro.Models;
using FarmerPro.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace FarmerPro.Controllers
{
    public class UserOrderController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        #region BGO-1 取得消費者自有訂單清單

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
                    var orders = db.Orders.Where(o => o.UserId == CustomerId && o.IsPay == true).ToList();
                    var orderlist = new List<object>();

                    if (!orders.Any())
                    {
                        //result訊息
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

                        foreach (var order in orders)
                        {
                            var farmerNickName = order.OrderDetail.Select(od => od.Spec.Product.Users.NickName).FirstOrDefault();

                            orderlist.Add(new
                            {
                                orderId = order.Id,
                                farmerNickName,
                                orderSum = order.OrderSum,
                                creatTime = order.CreatTime,
                                shipment = order.Shipment
                            }
                                );
                        }

                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "取得成功",
                        data = orderlist
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

        #endregion BGO-1 取得消費者自有訂單清單
    }
}