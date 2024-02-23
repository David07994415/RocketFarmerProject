using FarmerPro.Models;
using FarmerPro.Models.ViewModel;
using FarmerPro.Securities;
using MimeKit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection.Emit;
using System.Security.Principal;
using System.Web.Configuration;
using System.Web.Http;

namespace FarmerPro.Controllers
{
    public class OrderController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();


        #region FCO-1 新增訂單(未付款)(包含藍新資料)
        [HttpPost]
        [Route("api/order/")]
        [JwtAuthFilter]
        public IHttpActionResult CreateNewOrder([FromBody] CreateNewOrder input) 
        {
            try
            {
                //取得使用者ID
                int CustomerId = Convert.ToInt16(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);
                if (!ModelState.IsValid)
                {
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "欄位格式不正確，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    var newOrder = new Order
                    {
                        Receiver = input.receiver,
                        Phone = input.phone,           //這個model要改名稱=>Done
                        City = input.city,
                        District = input.district,          //信任前端資料，未來可以調整
                        ZipCode = input.zipCode,    //信任前端資料，未來可以調整
                        Address = input.address,
                        DeliveryFee = 100,                    //前端是否要傳?
                        OrderSum = input.orderSum,   //這邊為包含運費的總價
                        Shipment = false,
                        Guid = Guid.NewGuid(),
                        UserId = CustomerId,
                    };

                    db.Orders.Add(newOrder);
                    db.SaveChanges();
                    int OrderID = newOrder.Id;

                    var newOrderDetail = input.cartList.Select(x => new OrderDetail
                    {
                        Qty = x.qty,                                        
                        SpecId = x.specId,
                        SubTotal = x.subTotal,
                        OrderId = OrderID,
                    }).ToList();

                    db.OrderDetails.AddRange(newOrderDetail);
                    db.SaveChanges();

                    //寫入OrderFarmer資料庫
                    var uniqueProductIds = input.cartList.Select(item => item.productId).Distinct().ToList();
                    List<int> uniqueFarmer = new List<int>();
                    foreach (var product in uniqueProductIds) 
                    {
                        int farmerid = db.Products.Where(x => x.Id == product).FirstOrDefault().UserId;
                        if (uniqueFarmer.Contains(farmerid) == false) { uniqueFarmer.Add(farmerid); }
                    }
                    foreach (var of in uniqueFarmer) 
                    {
                        var orderFarmer = new OrderFarmer
                        {
                            UserId = of,
                            OrderId = OrderID,
                        };
                        db.OrderFarmer.Add(orderFarmer);
                        db.SaveChanges();
                    }


                    foreach (var cartItem in input.cartList)
                    {
                        var specToUpdate = db.Specs.Where(x => x.Id == cartItem.specId)?.FirstOrDefault();

                        if (specToUpdate != null)
                        {
                            specToUpdate.Stock -= cartItem.qty;   //庫存減少
                            specToUpdate.Sales += cartItem.qty;   //銷貨增加
                            db.SaveChanges();
                        }
                    }


                    //以上為訂單存入資料庫
                    //以下為藍新資料建立

                    // 整理金流串接資料
                    // 加密用金鑰
                    string hashKey = WebConfigurationManager.AppSettings["BlueKey"];
                    string hashIV = WebConfigurationManager.AppSettings["BlueIV"];

                    // 金流接收必填資料
                    string merchantID = "MS151432737";
                    string tradeInfo = "";
                    string tradeSha = "";
                    string version = "2.0"; // 參考文件串接程式版本  //不確定，先設定2.0

                    // tradeInfo 內容，導回的網址都需為 https 
                    string respondType = "JSON"; // 回傳格式
                    string timeStamp = ((int)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds).ToString();
                    string merchantOrderNo = timeStamp +"_"+input.cartId+ "_" + OrderID.ToString();//"訂單ID"; // 底線後方為訂單ID，解密比對用，不可重覆(規則參考文件) =>後端儲存cartId
                    string amt = input.orderSum.ToString();//"訂單金額";
                    string itemDesc = input.cartId.ToString(); //"商品資訊"
                    string tradeLimit = "600"; // 交易限制秒數
                    string notifyURL = @"http://" + Request.RequestUri.Host + "/api/order/payment"; //變成HTTP //"NotifyURL"; // NotifyURL 填後端接收藍新付款結果的 API 位置，如 : /api/users/getpaymentdata
                    string returnURL = @"https://sun-live.vercel.app/api/payment/return";//"付款完成導回頁面網址" + "/" + "訂單ID";  // 前端可用 Status: SUCCESS 來判斷付款成功，網址夾帶可拿來取得活動內容
                    string email = @"14rocketback@gmail.com";//"消費者信箱"; // 通知付款完成用
                    string loginType = "0"; // 0不須登入藍新金流會員

                    // 將 model 轉換為List<KeyValuePair<string, string>>
                    List<KeyValuePair<string, string>> tradeData = new List<KeyValuePair<string, string>>() {
                            new KeyValuePair<string, string>("MerchantID", merchantID),
                            new KeyValuePair<string, string>("RespondType", respondType),
                            new KeyValuePair<string, string>("TimeStamp", timeStamp),
                            new KeyValuePair<string, string>("Version", version),
                            new KeyValuePair<string, string>("MerchantOrderNo", merchantOrderNo),
                            new KeyValuePair<string, string>("Amt", amt),
                            new KeyValuePair<string, string>("ItemDesc", itemDesc),
                            new KeyValuePair<string, string>("TradeLimit", tradeLimit),
                            new KeyValuePair<string, string>("NotifyURL", notifyURL),
                            new KeyValuePair<string, string>("ReturnURL", returnURL),
                            new KeyValuePair<string, string>("Email", email),
                            new KeyValuePair<string, string>("LoginType", loginType)
                      };

                    // 將 List<KeyValuePair<string, string>> 轉換為 key1=Value1&key2=Value2&key3=Value3...
                    var tradeQueryPara = string.Join("&", tradeData.Select(x => $"{x.Key}={x.Value}"));
                    // AES 加密
                    tradeInfo = CryptoUtil.EncryptAESHex(tradeQueryPara, hashKey, hashIV);
                    // SHA256 加密
                    tradeSha = CryptoUtil.EncryptSHA256($"HashKey={hashKey}&{tradeInfo}&HashIV={hashIV}");

                    //將藍新資料送入資料庫

                    var updateBlueNewData=db.Orders.Where(x=>x.Id== OrderID)?.FirstOrDefault();
                    if (updateBlueNewData != null) 
                    {
                        updateBlueNewData.MerchantID=merchantID;
                        updateBlueNewData.TradeInfo=tradeInfo;
                        updateBlueNewData.TradeSha=tradeSha;
                        updateBlueNewData.Version=version;
                        db.SaveChanges();
                    }


                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "新增成功",
                        paymentData = new
                        {
                            MerchantID = merchantID,
                            TradeInfo = tradeInfo,
                            TradeSha = tradeSha,
                            Version = version
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
        #endregion


        #region FCO-2 藍新回送付款狀態(金流已付款)
        [HttpPost]
        [Route("api/order/payment")]
        // JwtAuthFilter 不用驗證
        public IHttpActionResult BlueReturnOrderState([FromBody] NewebPayReturn data)
        {
            try
            {
                // 付款失敗跳離執行
                //var response = Request.CreateResponse(HttpStatusCode.OK);
                if (!data.Status.Equals("SUCCESS"))
                {
                    var result2 = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "付款失敗",
                    };
                    return Content(HttpStatusCode.OK, result2);
                }
                else
                {
                    // 加密用金鑰
                    string hashKey = WebConfigurationManager.AppSettings["BlueKey"];
                    string hashIV = WebConfigurationManager.AppSettings["BlueIV"];
                    // AES 解密
                    string decryptTradeInfo = CryptoUtil.DecryptAESHex(data.TradeInfo, hashKey, hashIV);
                    PaymentResult result = JsonConvert.DeserializeObject<PaymentResult>(decryptTradeInfo);
                    // 取出交易記錄資料庫的訂單ID
                    string[] orderNo = result.Result.MerchantOrderNo.Split('_');
                    int logId = Convert.ToInt32(orderNo[2]); //取得訂單Id
                    int cartId= Convert.ToInt32(orderNo[1]); //取得購物車Id

                    var changePayState = db.Orders.Where(x => x.Id == logId)?.FirstOrDefault();
                    if (changePayState != null) 
                    {
                        changePayState.PaymentTime = DateTime.Now; 
                        changePayState.IsPay = true;
                        db.SaveChanges();
                    }
                    //藍新交易紀錄目前沒有存在資料庫內，後續可以考慮新增

                    //修改購物車變成IsPay，以利於清空
                    var changeCartPayState = db.Carts.Where(x => x.Id == cartId && x.IsPay==false)?.FirstOrDefault();
                    if (changeCartPayState != null)
                    {
                        changeCartPayState.IsPay = true;
                        db.SaveChanges();
                    }

                    var result2 = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "訂單狀態修改成功",
                    };
                    return Content(HttpStatusCode.OK, result2);
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
        #endregion
    }
}