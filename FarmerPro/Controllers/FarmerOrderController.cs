﻿using FarmerPro.Models;
using FarmerPro.Securities;
using MailKit.Search;
using Microsoft.AspNet.SignalR;
using NSwag.Annotations;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace FarmerPro.Controllers
{
    [OpenApiTag("Order", Description = "訂單及金流")]
    public class FarmerOrderController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        #region BFO-01 取得小農訂單清單
        /// <summary>
        /// BFO-01 取得小農訂單清單
        /// </summary>
        /// <param></param>
        /// <returns>返回小農訂單清單的 JSON 物件</returns>
        [HttpGet]
        [Route("api/farmer/orderlist")]
        [JwtAuthFilter]
        public IHttpActionResult GetUserOrder()
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
                    var orderInfo = db.Orders.AsEnumerable()
                                .Where(o => o.OrderDetail.Any(od => od.Spec.Product.UserId == farmerId))
                                .OrderByDescending(o => o.CreatTime)
                                .Select(o => new
                                {
                                    orderId = o.Id,
                                    userNickName = db.Users.Where(u => u.Id == o.UserId).Select(u => u.NickName).FirstOrDefault(),
                                    orderSum = (int)o.OrderSum,
                                    creatTime = o.CreatTime.ToString("yyyy.MM.dd"),
                                    ispay = o.IsPay,
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
        #endregion BFO-01 取得小農訂單清單

        #region BFO-02 修改小農單一訂單狀態
        /// <summary>
        /// BFO-02 修改小農單一訂單狀態
        /// </summary>
        /// <param name="orderId">提供訂單Id</param>
        /// <returns>返回小農單一訂單狀態</returns>
        [HttpPatch]
        [Route("api/farmer/order/{orderId}/toggle")]
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

                        var hubsocket = GlobalHost.ConnectionManager.GetHubContext<chathub>();
                        int id = db.Orders.Where(x => x.Id == orderId).FirstOrDefault().UserId;
                        string connectionId = "";
                        if (GlobalVariable._userList.ContainsKey(id.ToString()))
                        {
                            connectionId = GlobalVariable._userList[id.ToString()];
                            hubsocket.Clients.Client(connectionId).notifyShipment("您的商品已出貨");
                        }

                        var result = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = $"訂單狀態更新成功",
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
        #endregion BFO-02 修改小農單一訂單狀態

        #region BFO-03 搜尋特定訂單(小農自有，搜尋清單內含商品)
        /// <summary>
        /// BFO-03 搜尋特定訂單(小農自有，搜尋清單內含商品)
        /// </summary>
        /// <param name="input">提供商品名稱</param>
        /// <returns>返回小農特定訂單的 JSON 物件</returns>
        [HttpPost]
        [Route("api/farmer/order/search")]
        [JwtAuthFilter]
        public IHttpActionResult SearchFarmerOrder([FromBody] SerchFarmerOrder input)
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
                    var orders = db.Orders.Where(o => o.OrderDetail.Any(od => od.Spec.Product.UserId == farmerId)).ToList();

                    if (!orders.Any())
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
                        string searchCheck = input.serchQuery;

                        var searchFarmerOrder = from o in db.Orders
                                                join od in db.OrderDetails on o.Id equals od.OrderId
                                                join p in db.Products on od.Spec.ProductId equals p.Id
                                                where p.ProductTitle.Contains(searchCheck) && p.UserId == farmerId
                                                select new
                                                {
                                                    orderId = o.Id,
                                                    userNickName = db.Users.Where(u => u.Id == o.UserId).Select(u => u.NickName).FirstOrDefault(),
                                                    orderSum = (int)o.OrderSum,
                                                    creatTime = o.CreatTime,
                                                    ispay = o.IsPay,
                                                    shipment = o.Shipment
                                                };

                        if (!searchFarmerOrder.Any())
                        {
                            var searchresult = new
                            {
                                statusCode = 200,
                                status = "success",
                                message = "沒有訂單資料",
                                data = new object[] { }
                            };
                            return Content(HttpStatusCode.OK, searchresult);
                        }
                        else
                        {
                            var searchResult = new
                            {
                                statusCode = 200,
                                status = "success",
                                message = "取得成功",
                                data = searchFarmerOrder.ToList()
                            };
                            return Content(HttpStatusCode.OK, searchResult);
                        }
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
        #endregion BFO-03 搜尋特定訂單(小農自有，搜尋清單內含商品)

        /// <summary>
        /// 搜尋商品名
        /// </summary>
        public class SerchFarmerOrder
        {
            [Display(Name = "搜尋")]
            public string serchQuery { get; set; }
        }
    }
}