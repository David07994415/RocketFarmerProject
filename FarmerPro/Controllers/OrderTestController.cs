using FarmerPro.Models;
using FarmerPro.Securities;
using MailKit.Search;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace FarmerPro.Controllers
{
    public class OrderTestController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        #region BFO-4 取得小農各自訂單清單

        [HttpGet]
        [Route("api/farmer/orderlistTest")]
        public IHttpActionResult GetUserOrderTest()
        {
            //int farmerId = Convert.ToInt16(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);

            //var user = db.Users.FirstOrDefault();

            //小農個人訂單
            var orderbyfarmer = db.OrderFarmer.Where(of => of.UserId == 2).Select(of => of.Order).ToList();

            //order內specId的spec->productId->userId == OrderFarmer的userId == farmerId
            var orderbyfarmerInfo = orderbyfarmer.Select(o => new
            {
                orderId = o.Id,
                userId = o.UserId,
                createTime = o.CreatTime,
                shipment = o.Shipment
            }).ToList();

            var orderbyfarmerspec = db.OrderFarmer.Where(of => of.UserId == 2).SelectMany(of => of.Order.OrderDetail)
                                    .Select(od => new
                                    {
                                        farmerUserId = od.Spec.Product.UserId,
                                        specId = od.SpecId,
                                        subtotal = od.SubTotal
                                    })
                                    .Where(sel => sel.farmerUserId == 2)  // 篩選出farmerId商品
                                    .ToList();

            var result = new
            {
                statusCode = 200,
                status = "success",
                message = "取得成功",
                data = new
                {
                    orderbyfarmerInfo,
                    orderbyfarmerspec
                }
            };
            return Content(HttpStatusCode.OK, result);
        }

        //userId==2 的商品，prom的價格

        #endregion BFO-4 取得小農各自訂單清單
    }
}