using FarmerPro.Models;
using FarmerPro.Securities;
using MailKit.Search;
using Microsoft.AspNet.SignalR;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Owin;
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
                    var orderInfo = db.OrderFarmer.AsEnumerable().Where(of => of.UserId == farmerId).Select(of => new
                    {
                        orderFarmerId = of.Id,
                        orderId = of.Order.Id,
                        userId = of.UserId,
                        createTime = of.Order.CreatTime.ToString("yyyy.MM.dd"),
                        ispay = of.Order.IsPay,
                        shipmentFarmer = of.ShipmentFarmer
                    })
                    .ToList();

                    //var orderbyfarmerInfo = db.OrderFarmer.Where(of => of.UserId == 2).SelectMany(of => of.Order.OrderDetail)
                    //                        .Select(od => new
                    //                        {
                    //                            orderId = od.OrderId,
                    //                            farmerUserId = od.Spec.Product.UserId,
                    //                            specId = od.SpecId,
                    //                            subtotal = od.SubTotal
                    //                        })
                    //                        .Where(sel => sel.farmerUserId == 2)  // 篩選出farmerId商品
                    //                        .ToList();

                    var orderbyfarmerInfo = db.OrderFarmer.Where(of => of.UserId == farmerId).SelectMany(of => of.Order.OrderDetail).Where(od => od.Spec.Product.UserId == farmerId)
                                            .GroupBy(od => od.OrderId)  // 按 orderId 進行分組
                                            .Select(group => new
                                            {
                                                orderId = group.Key,
                                                subtotal = group.Sum(item => item.SubTotal),
                                            }).ToList();

                    var orderbyfarmerall = (from info in orderInfo
                                            join spec in orderbyfarmerInfo on info.orderId equals spec.orderId into orders
                                            from os in orders.DefaultIfEmpty()
                                            select new
                                            {
                                                orderFarmerId = info.orderFarmerId,
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
        public IHttpActionResult PatchFarmerOrder(int orderFarmerId)
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
                    var order = db.Orders.FirstOrDefault(o => o.OrderDetail.Any(od => od.Spec.Product.UserId == farmerId) && o.IsPay == true);//要支付後才能改

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
                        //查詢orderbyfarmer
                        var orderbyfarmer = db.OrderFarmer.FirstOrDefault(o => o.Id == orderFarmerId);
                        if (orderbyfarmer != null)
                        {
                            orderbyfarmer.ShipmentFarmer = !orderbyfarmer.ShipmentFarmer;
                            db.SaveChanges();
                        }
                        //要同時判斷orderId底下的ShipmentFarmer都按下出貨，order的shipment才能顯示出貨
                        //在order.orderfarmer.ShipmentFarmer做判斷，只要有其中一個是false就不會將order.Shipment==true
                        var allShipment = db.OrderFarmer.Where(o => o.OrderId == orderbyfarmer.OrderId).All(of => of.ShipmentFarmer);
                        if (allShipment)
                        {
                            var orderShipment = db.Orders.FirstOrDefault(o => o.Id == orderbyfarmer.OrderId);
                            if (orderShipment != null)
                            {
                                orderShipment.Shipment = true;
                                db.SaveChanges();
                            }

                            //進行WS的message傳送
                            var hubsocket = GlobalHost.ConnectionManager.GetHubContext<chathub>();
                            int id = db.Orders.Where(x => x.Id == orderbyfarmer.OrderId).FirstOrDefault().UserId;
                            string connectionId = "";
                            if (GlobalVariable._userList.ContainsKey(id.ToString()))
                            {
                                connectionId = GlobalVariable._userList[id.ToString()];
                                hubsocket.Clients.Client(connectionId).notifyShipment("您的商品已出貨");
                            }
                        }

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