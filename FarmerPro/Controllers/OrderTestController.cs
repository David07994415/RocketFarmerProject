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
    public class OrderTestController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        #region BFO-1 取得小農訂單清單

        [HttpGet]
        [Route("api/farmer/orderlistTest")]
        public IHttpActionResult GetUserOrderTest()
        {
            //int farmerId = Convert.ToInt16(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);

            //var user = db.Users.FirstOrDefault();
            var test = db.OrderFarmer.Where(x => x.Id == 1).FirstOrDefault().Order;

            //userId==2 的商品，prom的價格
            var product = db.OrderFarmer.Where(o => o.UserId == 2).FirstOrDefault().Order.OrderDetail.Select(p => new
            {
                SpecId = p.SpecId
            }
            );
            var result = new
            {
                statusCode = 401,
                status = "error",
                message = "沒有此小農，請重新輸入",
                data = product
            };
            return Content(HttpStatusCode.OK, result);
        }

        #endregion BFO-1 取得小農訂單清單
    }
}