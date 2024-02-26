using FarmerPro.Models;
using FarmerPro.Securities;
using MailKit.Search;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Razor.Parser.SyntaxTree;

namespace FarmerPro.Controllers
{
    public class OrderTestController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        #region BFO-4 取得小農各自訂單清單

        [HttpGet]
        [Route("api/farmer/orderlistNew")]
        [JwtAuthFilter]
        public IHttpActionResult GetUserOrderTest()
        {
            try
            {
                int farmerId = Convert.ToInt16(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);

                var user = db.Users.FirstOrDefault(u => u.Id == farmerId && (int)u.Category == 1);
                if (user == null)
                {
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "沒有此小農，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    //order內specId的spec->productId->userId == OrderFarmer的userId == farmerId
                    // 小農個人訂單+出貨狀態
                    var orderbyfarmerInfo = db.OrderFarmer.AsEnumerable().Where(of => of.UserId == farmerId).Select(of => new
                    {
                        orderId = of.Order.Id,
                        userId = of.UserId,
                        createTime = of.Order.CreatTime.ToString("yyyy.MM.dd"),
                        ispay = of.Order.IsPay,
                        shipmentFarmer = of.ShipmentFarmer
                    })
                    .ToList();

                    //var orderbyfarmerspec = db.OrderFarmer.Where(of => of.UserId == 2).SelectMany(of => of.Order.OrderDetail)
                    //                        .Select(od => new
                    //                        {
                    //                            orderId = od.OrderId,
                    //                            farmerUserId = od.Spec.Product.UserId,
                    //                            specId = od.SpecId,
                    //                            subtotal = od.SubTotal
                    //                        })
                    //                        .Where(sel => sel.farmerUserId == 2)  // 篩選出farmerId商品
                    //                        .ToList();

                    var orderbyfarmerspec = db.OrderFarmer.Where(of => of.UserId == farmerId).SelectMany(of => of.Order.OrderDetail).Where(od => od.Spec.Product.UserId == farmerId)
                                            .GroupBy(od => od.OrderId)  // 按 orderId 進行分組
                                            .Select(group => new
                                            {
                                                orderId = group.Key,
                                                subtotal = group.Sum(item => item.SubTotal),
                                            }).ToList();

                    var orderbyfarmerall = (from info in orderbyfarmerInfo
                                            join spec in orderbyfarmerspec on info.orderId equals spec.orderId into orders
                                            from os in orders.DefaultIfEmpty()
                                            select new
                                            {
                                                orderId = info.orderId,
                                                userId = info.userId,
                                                orderSum = (int)os.subtotal,
                                                createTime = info.createTime,
                                                ispay = info.ispay,
                                                shipmentFarmer = info.shipmentFarmer,
                                            }).ToList();

                    if (!orderbyfarmerall.Any())
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
                    {
                        //var userNickName = (from order in db.Orders
                        //                    join u in db.Users on order.UserId equals user.Id
                        //                    select user.NickName).FirstOrDefault();
                        var result = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "取得成功",
                            data = orderbyfarmerall
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

        //userId==2 的商品，prom的價格

        #endregion BFO-4 取得小農各自訂單清單

        #region BFO-5 修改小農單一訂單狀態

        [HttpPatch]
        [Route("api/farmer/order/{orderId}/toggleNew")]
        [JwtAuthFilter]
        public IHttpActionResult PatchFarmerOrder(int orderId)
        {
            try
            {
                int farmerId = Convert.ToInt16(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);

                var user = db.Users.FirstOrDefault(u => u.Id == farmerId && (int)u.Category == 1);
                if (user == null)
                {
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "沒有此小農，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    var order = db.Orders.FirstOrDefault(o => o.Id == orderId && o.OrderDetail.Any(od => od.Spec.Product.UserId == farmerId) && o.IsPay == true);//要支付後才能改

                    if (order == null)
                    {
                        //result訊息
                        var getOrder = new
                        {
                            statusCode = 402,
                            status = "error",
                            message = "找不到該訂單資料，請確認是否付款",
                        };
                        return Content(HttpStatusCode.OK, getOrder);
                    }
                    else
                    {
                        order.Shipment = !order.Shipment;
                        db.SaveChanges();

                        var result = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "訂單狀態更新成功",
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

        #endregion BFO-5 修改小農單一訂單狀態
    }
}