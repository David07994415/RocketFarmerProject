﻿using FarmerPro.Models;
using FarmerPro.Models.ViewModel;
using FarmerPro.Securities;
using Microsoft.Ajax.Utilities;
using NSwag.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace FarmerPro.Controllers
{
    [OpenApiTag("Cart", Description = "購物車")]
    public class CartController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        #region FGC-02 取得購物車清單

        /// <summary>
        /// FGC-02 取得購物車清單
        /// </summary>
        /// <param></param>
        /// <returns>返回購物車清單的 JSON 物件</returns>
        [HttpGet]
        [Route("api/cart")]
        [JwtAuthFilter]
        public IHttpActionResult GetCartItem()
        {
            try
            {
                int CustomerId = Convert.ToInt16(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);

                var Getcart = db.Carts.Where(c => c.UserId == CustomerId && c.IsPay == false);

                if (Getcart == null)
                {
                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "購物車已清空",
                        data = new object[] { }
                    };
                    return Content(HttpStatusCode.OK, result);
                }

                var cartIdInfo = Getcart.FirstOrDefault().Id;
                var cartItemProductIds = Getcart.SelectMany(c => c.CartItem).Select(ci => ci.Spec.ProductId);

                var detailProduct = from p in db.Products
                                    join user in db.Users on p.UserId equals user.Id
                                    join s in db.Specs on p.Id equals s.ProductId
                                    where cartItemProductIds.Contains(p.Id)
                                    let largeSpec = db.Specs.FirstOrDefault(s => s.ProductId == p.Id && s.Size) //大= F
                                    let smallSpec = db.Specs.FirstOrDefault(s => s.ProductId == p.Id && !s.Size)  //小 = T
                                    select new
                                    {
                                        productId = p.Id,
                                        largeproductSpecId = largeSpec.Id,
                                        largeOriginalPrice = largeSpec != null ? (int?)largeSpec.Price : null,
                                        largePromotionPrice = largeSpec != null ? (int?)largeSpec.PromotePrice : null,
                                        largeLivePrice = largeSpec != null ? (int?)largeSpec.LivePrice : null,
                                        largeWeight = largeSpec != null ? (int?)largeSpec.Weight : null,
                                        smallproductSpecId = smallSpec.Id,
                                        smallOriginalPrice = smallSpec != null ? (int?)smallSpec.Price : null,
                                        smallPromotionPrice = smallSpec != null ? (int?)smallSpec.PromotePrice : null,
                                        smallLivePrice = largeSpec != null ? (int?)smallSpec.LivePrice : null,
                                        smallWeight = smallSpec != null ? (int?)smallSpec.Weight : null,
                                    };

                var cartInfo = db.Carts.Where(c => c.UserId == CustomerId && c.IsPay == false).Include(c => c.CartItem)
                                .Select(cart => new
                                {
                                    totalOriginalPrice = cart.CartItem.Sum(c => c.Spec.Price * c.Qty),
                                    totalPromotionPrice = cart.CartItem.Sum(c => c.IsLivePrice == true ? c.Spec.LivePrice * c.Qty : c.Spec.PromotePrice * c.Qty),
                                }).Select(d => new
                                {
                                    d.totalOriginalPrice,
                                    d.totalPromotionPrice,
                                    totalDiscount = d.totalOriginalPrice - d.totalPromotionPrice,
                                });

                var cartItemInfo = db.CartItems.Where(c => c.CartId == cartIdInfo).GroupBy(gruop => gruop.SpecId)
                                        .Select(cartItemGruop => new
                                        {
                                            productId = cartItemGruop.FirstOrDefault().Spec.ProductId,
                                            productTitle = cartItemGruop.FirstOrDefault().Spec.Product.ProductTitle, // Spec--Product--Title
                                            productSpecId = cartItemGruop.Key,
                                            productSpecSize = cartItemGruop.FirstOrDefault().Spec.Size,
                                            productSpecWeight = (int)cartItemGruop.FirstOrDefault().Spec.Weight,
                                            cartItemOriginalPrice = cartItemGruop.FirstOrDefault().Spec.Price,
                                            cartItemPromotionPrice = cartItemGruop.FirstOrDefault().Spec.PromotePrice,
                                            cartItemLivePrice = cartItemGruop.FirstOrDefault().IsLivePrice == true ? cartItemGruop.FirstOrDefault().Spec.LivePrice : (decimal?)null,
                                            cartItemQty = cartItemGruop.Sum(item => item.Qty),
                                            subtotal = cartItemGruop.Sum(item => item.IsLivePrice == true ? item.Qty * item.Spec.LivePrice : item.Qty * item.Spec.PromotePrice),
                                            productImg = new
                                            {
                                                src = db.Albums.Where(a => a.ProductId == cartItemGruop.FirstOrDefault().Spec.ProductId).FirstOrDefault().Photo.FirstOrDefault().URL ?? null,
                                                alt = cartItemGruop.FirstOrDefault().Spec.Product.ProductTitle,
                                            },
                                        }).ToList();

                var cartItemLength = cartItemInfo.Select(item => item.productSpecId).Distinct().Count();

                var cartItemProductInfo = (from cartItem in cartItemInfo
                                           join detail in detailProduct on cartItem.productId equals detail.productId into details
                                           from d in details.DefaultIfEmpty()
                                           select new
                                           {
                                               productId = cartItem.productId,
                                               productTitle = cartItem.productTitle,
                                               productSpecId = cartItem.productSpecId,
                                               productSpecSize = cartItem.productSpecSize,
                                               productSpecWeight = cartItem.productSpecWeight,
                                               cartItemOriginalPrice = cartItem.cartItemOriginalPrice,
                                               cartItemPromotionPrice = cartItem.cartItemPromotionPrice,
                                               cartItemLivePrice = cartItem.cartItemLivePrice,
                                               cartItemQty = cartItem.cartItemQty,
                                               subtotal = cartItem.subtotal,
                                               productImg = cartItem.productImg,

                                               largeProductSpecId = d?.largeproductSpecId,
                                               largeOriginalPrice = d?.largeOriginalPrice,
                                               largePromotionPrice = d?.largePromotionPrice,
                                               largeLivePrice = d?.largeLivePrice,
                                               largeWeight = d?.largeWeight,
                                               smallProductSpecId = d?.smallproductSpecId,
                                               smallOriginalPrice = d?.smallOriginalPrice,
                                               smallPromotionPrice = d?.smallPromotionPrice,
                                               smallLivePrice = d?.smallLivePrice,
                                               smallWeight = d?.smallWeight,
                                           })
                            .GroupBy(x => x.productSpecId)
                            .Select(g => g.FirstOrDefault())
                            .ToList();

                if (!cartItemInfo.Any())
                {
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
                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "取得成功",
                        data = new
                        {
                            cartId = cartIdInfo,
                            cartItemLength,
                            cartInfo,
                            cartItemProductInfo
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

        #endregion FGC-02 取得購物車清單

        #region FGC-01 加入購物車(要補上購物車數量欄位)

        /// <summary>
        /// FGC-01 加入購物車
        /// </summary>
        /// <remarks>要補上購物車數量欄位</remarks>
        /// <param name="input">提供購物車清單的 JSON 物件</param>
        /// <returns>返回購物車清單的 JSON 物件</returns>
        [HttpPost]
        [Route("api/cart")]
        [JwtAuthFilter]
        public IHttpActionResult AddCartItem([FromBody] GetCartItemClass input)
        {
            try
            {
                int CustomerId = Convert.ToInt16(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);
                var cart = db.Carts.FirstOrDefault(c => c.UserId == CustomerId && c.IsPay == false); //如果沒有就創建

                if (cart == null)
                {
                    cart = new Cart { UserId = CustomerId, IsPay = false };
                    db.Carts.Add(cart);
                    db.SaveChanges();
                }

                var CartId = cart.Id;
                var SpecId = input.productSpecId; //沒有此商品，請重新輸入
                var Qty = input.cartItemQty;  //數量不可為0

                var SpecInfo = db.Specs.FirstOrDefault(s => s.Id == SpecId);

                if (SpecInfo == null)
                {
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "沒有商品SpecId，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else if (Qty <= 0)
                {
                    var result = new
                    {
                        statusCode = 402,
                        status = "error",
                        message = "數量不可為0，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                { 
                    bool Islive = input.liveId == null ? false : true; //判斷是否有直播
 
                    var productPrice = Islive ? SpecInfo.LivePrice : SpecInfo.PromotePrice; //判斷目前商品價格

                    var addcartitem = new CartItem
                    {
                        CartId = cart.Id,
                        SpecId = input.productSpecId,
                        Qty = input.cartItemQty,
                        SubTotal = (int)productPrice * input.cartItemQty,
                        IsLivePrice = Islive
                    };
                    db.CartItems.Add(addcartitem);
                    db.SaveChanges();

                    var cartIdInfo = db.Carts.Where(c => c.UserId == CustomerId && c.IsPay == false).FirstOrDefault().Id;

                    var cartItemInfo = db.CartItems.Where(c => c.CartId == cartIdInfo).GroupBy(group => group.SpecId)
                        .Select(cartItemGroup => new
                        {
                            productId = cartItemGroup.FirstOrDefault().Spec.ProductId,
                            productTitle = cartItemGroup.FirstOrDefault().Spec.Product.ProductTitle,
                            productSpecId = cartItemGroup.Key,
                            productSpecSize = cartItemGroup.FirstOrDefault().Spec.Size,
                            productSpecWeight = (int)cartItemGroup.FirstOrDefault().Spec.Weight,
                            cartItemOriginalPrice = cartItemGroup.FirstOrDefault().Spec.Price,
                            cartItemPromotionPrice = cartItemGroup.FirstOrDefault().Spec.PromotePrice,
                            cartItemLivePrice = cartItemGroup.FirstOrDefault().IsLivePrice == true ? cartItemGroup.FirstOrDefault().Spec.LivePrice : (decimal?)null,
                            cartItemQty = cartItemGroup.Sum(item => item.Qty),
                            subtotal = cartItemGroup.Sum(item => item.IsLivePrice == true ? item.Qty * item.Spec.LivePrice : item.Qty * item.Spec.PromotePrice),
                            productImg = new
                            {
                                src = db.Albums.Where(a => a.ProductId == cartItemGroup.FirstOrDefault().Spec.ProductId).FirstOrDefault().Photo.FirstOrDefault().URL ?? null,
                                alt = cartItemGroup.FirstOrDefault().Spec.Product.ProductTitle,
                            },
                        }).ToList();

                    var distinctProducts = cartItemInfo.GroupBy(item => item.productId)
                        .Select(group => new
                        {
                            productId = group.Key,
                            productTitle = group.First().productTitle,
                            productSpecId = group.First().productSpecId,
                            productSpecSize = group.First().productSpecSize,
                            productSpecWeight = group.First().productSpecWeight,
                            cartItemOriginalPrice = group.First().cartItemOriginalPrice,
                            cartItemPromotionPrice = group.First().cartItemPromotionPrice,
                            cartItemLivePrice = group.First().cartItemLivePrice,
                            cartItemQty = group.Sum(item => item.cartItemQty),
                            subtotal = group.Sum(item => item.subtotal),
                            productImg = group.First().productImg
                        }).ToList();

                    cartItemInfo.Clear();
                    cartItemInfo.AddRange(distinctProducts);

                    var cartItemLength = db.CartItems.Where(c => c.CartId == cartIdInfo).Select(c => c.SpecId).Distinct().Count();

                    if (!cartItemInfo.Any())
                    {
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
                        var result = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "加入成功",
                            data = new
                            {
                                cartId = cartIdInfo,
                                cartItemLength,
                                cartItemInfo
                            }
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

        #endregion FGC-01 加入購物車(要補上購物車數量欄位)

        #region FGC-03 修改商品數量

        /// <summary>
        /// FGC-03 修改商品數量
        /// </summary>
        /// <param name="input">提供購物車清單的 JSON 物件</param>
        /// <returns>返回購物車清單的 JSON 物件</returns>
        [HttpPut]
        [Route("api/cart")]
        [JwtAuthFilter]
        public IHttpActionResult UpdateCartItem([FromBody] GetCartItemClass input)
        {
            try
            {
                int CustomerId = Convert.ToInt16(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);

                var cart = db.Carts.FirstOrDefault(c => c.UserId == CustomerId && c.IsPay == false); //如果沒有就創建
                if (cart == null)
                {
                    var result = new
                    {
                        statusCode = 403,
                        status = "error",
                        message = "使用者未創建購物車，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }

                var CartId = cart.Id;
                var SpecId = input.productSpecId; //沒有此商品，請重新輸入
                var Qty = input.cartItemQty;  //數量不可為0

                var SpecInfo = db.Specs.FirstOrDefault(s => s.Id == SpecId);

                var cartItemSpecId = db.CartItems.FirstOrDefault(ci => ci.CartId == CartId && ci.SpecId == SpecId);
                if (cartItemSpecId == null)
                {
                    var result = new
                    {
                        statusCode = 404,
                        status = "error",
                        message = "此商品SpecId不存在於購物車中，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }

                if (SpecInfo == null)
                {
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "沒有商品SpecId，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else if (Qty <= 0)
                {
                    var result = new
                    {
                        statusCode = 402,
                        status = "error",
                        message = "數量不可為0，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    var updatecartitem = new CartItem //修改商品
                    {
                        CartId = cart.Id,
                        SpecId = input.productSpecId,
                        Qty = input.cartItemQty,
                    };

                    var cartItemUpdate = db.CartItems.FirstOrDefault(ci => ci.CartId == CartId && ci.SpecId == SpecId);
                    cartItemUpdate.Qty = updatecartitem.Qty;
                    cartItemUpdate.SpecId = updatecartitem.SpecId;
                    db.SaveChanges();

                    var cartItemProductIds = db.Carts.Where(c => c.UserId == CustomerId).SelectMany(c => c.CartItem).Select(ci => ci.Spec.ProductId);

                    var detailProduct = from p in db.Products
                                        join user in db.Users on p.UserId equals user.Id
                                        join s in db.Specs on p.Id equals s.ProductId
                                        where cartItemProductIds.Contains(p.Id)
                                        let largeSpec = db.Specs.FirstOrDefault(s => s.ProductId == p.Id && s.Size) //大= F
                                        let smallSpec = db.Specs.FirstOrDefault(s => s.ProductId == p.Id && !s.Size)  //小 = T
                                        select new
                                        {
                                            productId = p.Id,
                                            largeproductSpecId = largeSpec.Id,
                                            largeOriginalPrice = largeSpec != null ? (int?)largeSpec.Price : null,
                                            largePromotionPrice = largeSpec != null ? (int?)largeSpec.PromotePrice : null,
                                            largeLivePrice = largeSpec != null ? (int?)largeSpec.LivePrice : null,
                                            largeWeight = largeSpec != null ? (int?)largeSpec.Weight : null,
                                            smallproductSpecId = smallSpec.Id,
                                            smallOriginalPrice = smallSpec != null ? (int?)smallSpec.Price : null,
                                            smallPromotionPrice = smallSpec != null ? (int?)smallSpec.PromotePrice : null,
                                            smallLivePrice = largeSpec != null ? (int?)smallSpec.LivePrice : null,
                                            smallWeight = smallSpec != null ? (int?)smallSpec.Weight : null,
                                        };

                    var cartInfo = db.Carts.Where(c => c.UserId == CustomerId)
                                  .Select(updatecart => new
                                  {
                                      totalOriginalPrice = updatecart.CartItem.Sum(c => c.Spec.Price * c.Qty),
                                      totalPromotionPrice = updatecart.CartItem.Sum(c => c.IsLivePrice == true ? c.Spec.LivePrice * c.Qty : c.Spec.PromotePrice * c.Qty),
                                  }).Select(d => new
                                  {
                                      d.totalOriginalPrice,
                                      d.totalPromotionPrice,
                                      totalDiscount = d.totalOriginalPrice - d.totalPromotionPrice,
                                  });

                    var cartItemInfo = db.CartItems.Where(c => c.Cart.UserId == CustomerId).GroupBy(gruop => gruop.SpecId)
                                        .Select(cartItemGruop => new
                                        {
                                            productId = cartItemGruop.FirstOrDefault().Spec.ProductId,
                                            productTitle = cartItemGruop.FirstOrDefault().Spec.Product.ProductTitle, // Spec--Product--Title
                                            productSpecId = cartItemGruop.Key,
                                            productSpecSize = cartItemGruop.FirstOrDefault().Spec.Size,
                                            productSpecWeight = (int)cartItemGruop.FirstOrDefault().Spec.Weight,
                                            cartItemOriginalPrice = cartItemGruop.FirstOrDefault().Spec.Price,
                                            cartItemPromotionPrice = cartItemGruop.FirstOrDefault().Spec.PromotePrice,
                                            cartItemLivePrice = cartItemGruop.FirstOrDefault().IsLivePrice == true ? cartItemGruop.FirstOrDefault().Spec.LivePrice : (decimal?)null,
                                            cartItemQty = cartItemGruop.Sum(item => item.Qty),
                                            subtotal = cartItemGruop.Sum(item => item.IsLivePrice == true ? item.Qty * item.Spec.LivePrice : item.Qty * item.Spec.PromotePrice),
                                            productImg = new
                                            {
                                                src = db.Albums.Where(a => a.ProductId == cartItemGruop.FirstOrDefault().Spec.ProductId).FirstOrDefault().Photo.FirstOrDefault().URL ?? null,
                                                alt = cartItemGruop.FirstOrDefault().Spec.Product.ProductTitle,
                                            },
                                        }).ToList();

                    var cartItemProductInfo = (from cartItem in cartItemInfo
                                               join detail in detailProduct on cartItem.productId equals detail.productId into details
                                               from d in details.DefaultIfEmpty()
                                               select new
                                               {
                                                   productId = cartItem.productId,
                                                   productSpecId = cartItem.productSpecId,
                                                   productSpecSize = cartItem.productSpecSize,
                                                   productSpecWeight = cartItem.productSpecWeight,
                                                   cartItemOriginalPrice = cartItem.cartItemOriginalPrice,
                                                   cartItemPromotionPrice = cartItem.cartItemPromotionPrice,
                                                   cartItemLivePrice = cartItem.cartItemLivePrice,
                                                   cartItemQty = cartItem.cartItemQty,
                                                   subtotal = cartItem.subtotal,

                                                   largeProductSpecId = d?.largeproductSpecId,
                                                   largeOriginalPrice = d?.largeOriginalPrice,
                                                   largePromotionPrice = d?.largePromotionPrice,
                                                   largeLivePrice = d?.largeLivePrice,
                                                   largeWeight = d?.largeWeight,
                                                   smallProductSpecId = d?.smallproductSpecId,
                                                   smallOriginalPrice = d?.smallOriginalPrice,
                                                   smallPromotionPrice = d?.smallPromotionPrice,
                                                   smallLivePrice = d?.smallLivePrice,
                                                   smallWeight = d?.smallWeight,
                                               })
                            .GroupBy(x => x.productSpecId)
                            .Select(g => g.FirstOrDefault())
                            .ToList();

                    var cartItemLength = cartItemInfo.Select(item => item.productSpecId).Distinct().Count();

                    if (!cartItemInfo.Any())
                    {
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
                        var result = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "更新成功",
                            data = new
                            {
                                cartItemLength,
                                cartInfo,
                                cartItemProductInfo
                            }
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

        #endregion FGC-03 修改商品數量

        #region FGC-04 修改商品規格

        /// <summary>
        /// FGC-04 修改商品規格
        /// </summary>
        /// <param name="input">提供購物車清單的 JSON 物件</param>
        /// <returns>返回購物車清單的 JSON 物件</returns>
        [HttpPut]
        [Route("api/cart/specId")]
        [JwtAuthFilter]
        public IHttpActionResult UpdateCartItemspecId([FromBody] GetCartItemClass input)
        {
            try
            {
                int CustomerId = Convert.ToInt16(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);

                var cart = db.Carts.FirstOrDefault(c => c.UserId == CustomerId && c.IsPay == false); //如果沒有就創建
                if (cart == null)
                {
                    var result = new
                    {
                        statusCode = 403,
                        status = "error",
                        message = "使用者未創建購物車，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }

                var CartId = cart.Id;
                var SpecId = input.productSpecId; //沒有此商品，請重新輸入

                var SpecInfo = db.Specs.FirstOrDefault(s => s.Id == SpecId);

                var cartItemUpdate = db.CartItems.Where(ci => ci.CartId == CartId && ci.Spec.ProductId == input.productId).ToList();
                if (cartItemUpdate == null)
                {
                    var result = new
                    {
                        statusCode = 404,
                        status = "error",
                        message = "此商品Id不存在於購物車中，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }

                if (SpecInfo == null)
                {
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "沒有商品SpecId，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    foreach (var item in cartItemUpdate) //修改商品
                    {
                        item.SpecId = SpecId;
                    }
                    db.SaveChanges();

                    var cartItemProductIds = db.Carts.Where(c => c.UserId == CustomerId).SelectMany(c => c.CartItem).Select(ci => ci.Spec.ProductId);

                    var detailProduct = from p in db.Products
                                        join user in db.Users on p.UserId equals user.Id
                                        join s in db.Specs on p.Id equals s.ProductId
                                        where cartItemProductIds.Contains(p.Id)
                                        let largeSpec = db.Specs.FirstOrDefault(s => s.ProductId == p.Id && s.Size) //大= F
                                        let smallSpec = db.Specs.FirstOrDefault(s => s.ProductId == p.Id && !s.Size)  //小 = T
                                        select new
                                        {
                                            productId = p.Id,
                                            largeproductSpecId = largeSpec.Id,
                                            largeOriginalPrice = largeSpec != null ? (int?)largeSpec.Price : null,
                                            largePromotionPrice = largeSpec != null ? (int?)largeSpec.PromotePrice : null,
                                            largeLivePrice = largeSpec != null ? (int?)largeSpec.LivePrice : null,
                                            largeWeight = largeSpec != null ? (int?)largeSpec.Weight : null,
                                            smallproductSpecId = smallSpec.Id,
                                            smallOriginalPrice = smallSpec != null ? (int?)smallSpec.Price : null,
                                            smallPromotionPrice = smallSpec != null ? (int?)smallSpec.PromotePrice : null,
                                            smallLivePrice = largeSpec != null ? (int?)smallSpec.LivePrice : null,
                                            smallWeight = smallSpec != null ? (int?)smallSpec.Weight : null,
                                        };

                    var cartInfo = db.Carts.Where(c => c.UserId == CustomerId)
                                  .Select(updatecart => new
                                  {
                                      totalOriginalPrice = updatecart.CartItem.Sum(c => c.Spec.Price * c.Qty),
                                      totalPromotionPrice = updatecart.CartItem.Sum(c => c.IsLivePrice == true ? c.Spec.LivePrice * c.Qty : c.Spec.PromotePrice * c.Qty),
                                  }).Select(d => new
                                  {
                                      d.totalOriginalPrice,
                                      d.totalPromotionPrice,
                                      totalDiscount = d.totalOriginalPrice - d.totalPromotionPrice,
                                  });

                    var cartItemInfo = db.CartItems.Where(c => c.Cart.UserId == CustomerId).GroupBy(gruop => gruop.SpecId)
                                        .Select(cartItemGruop => new
                                        {
                                            productId = cartItemGruop.FirstOrDefault().Spec.ProductId,
                                            productTitle = cartItemGruop.FirstOrDefault().Spec.Product.ProductTitle, // Spec--Product--Title
                                            productSpecId = cartItemGruop.Key,
                                            productSpecSize = cartItemGruop.FirstOrDefault().Spec.Size,
                                            productSpecWeight = (int)cartItemGruop.FirstOrDefault().Spec.Weight,
                                            cartItemOriginalPrice = cartItemGruop.FirstOrDefault().Spec.Price,
                                            cartItemPromotionPrice = cartItemGruop.FirstOrDefault().Spec.PromotePrice,
                                            cartItemLivePrice = cartItemGruop.FirstOrDefault().IsLivePrice == true ? cartItemGruop.FirstOrDefault().Spec.LivePrice : (decimal?)null,
                                            cartItemQty = cartItemGruop.Sum(item => item.Qty),
                                            subtotal = cartItemGruop.Sum(item => item.IsLivePrice == true ? item.Qty * item.Spec.LivePrice : item.Qty * item.Spec.PromotePrice),
                                            productImg = new
                                            {
                                                src = db.Albums.Where(a => a.ProductId == cartItemGruop.FirstOrDefault().Spec.ProductId).FirstOrDefault().Photo.FirstOrDefault().URL ?? null,
                                                alt = cartItemGruop.FirstOrDefault().Spec.Product.ProductTitle,
                                            },
                                        }).ToList();

                    var cartItemProductInfo = (from cartItem in cartItemInfo
                                               join detail in detailProduct on cartItem.productId equals detail.productId into details
                                               from d in details.DefaultIfEmpty()
                                               select new
                                               {
                                                   productId = cartItem.productId,
                                                   productSpecId = cartItem.productSpecId,
                                                   productSpecSize = cartItem.productSpecSize,
                                                   productSpecWeight = cartItem.productSpecWeight,
                                                   cartItemOriginalPrice = cartItem.cartItemOriginalPrice,
                                                   cartItemPromotionPrice = cartItem.cartItemPromotionPrice,
                                                   cartItemLivePrice = cartItem.cartItemLivePrice,
                                                   cartItemQty = cartItem.cartItemQty,
                                                   subtotal = cartItem.subtotal,

                                                   largeProductSpecId = d?.largeproductSpecId,
                                                   largeOriginalPrice = d?.largeOriginalPrice,
                                                   largePromotionPrice = d?.largePromotionPrice,
                                                   largeLivePrice = d?.largeLivePrice,
                                                   largeWeight = d?.largeWeight,
                                                   smallProductSpecId = d?.smallproductSpecId,
                                                   smallOriginalPrice = d?.smallOriginalPrice,
                                                   smallPromotionPrice = d?.smallPromotionPrice,
                                                   smallLivePrice = d?.smallLivePrice,
                                                   smallWeight = d?.smallWeight,
                                               })
                            .GroupBy(x => x.productSpecId)
                            .Select(g => g.FirstOrDefault())
                            .ToList();

                    var cartItemLength = cartItemInfo.Select(item => item.productSpecId).Distinct().Count();

                    if (!cartItemInfo.Any())
                    {
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
                        var result = new
                        {
                            statusCode = 200,
                            status = "success",
                            message = "更新成功",
                            data = new
                            {
                                cartItemLength,
                                cartInfo,
                                cartItemProductInfo
                            }
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

        #endregion FGC-04 修改商品規格

        #region FGC-05 刪除特定商品

        /// <summary>
        /// FGC-05 刪除特定商品
        /// </summary>
        /// <param name="input">提供購物車清單的 JSON 物件</param>
        /// <returns>返回購物車清單的 JSON 物件</returns>
        [HttpDelete]
        [Route("api/cart")]
        [JwtAuthFilter]
        public IHttpActionResult DeleteCartItem([FromBody] GetCartItemClass input)
        {
            try
            {
                int CustomerId = Convert.ToInt16(JwtAuthFilter.GetToken(Request.Headers.Authorization.Parameter)["Id"]);

                var Getcart = db.Carts.FirstOrDefault(c => c.UserId == CustomerId && c.IsPay == false);//如果沒有就創建

                if (Getcart == null)
                {
                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "購物車已清空",
                        data = new object[] { }
                    };
                    return Content(HttpStatusCode.OK, result);
                }

                var cartIdInfo = Getcart.Id;

                var SpecId = input.productSpecId; //沒有此商品，請重新輸入

                var SpecInfo = db.Specs.FirstOrDefault(s => s.Id == SpecId);

                if (SpecInfo == null)
                {
                    //result訊息
                    var result = new
                    {
                        statusCode = 402,
                        status = "error",
                        message = "沒有商品SpecId，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }

                var cartItems = db.CartItems.Where(ci => ci.SpecId == SpecId && ci.CartId == cartIdInfo).ToList();
                if (!cartItems.Any())
                {
                    var result = new
                    {
                        statusCode = 404,
                        status = "error",
                        message = "購物車不含此商品SpecId，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }

                db.CartItems.RemoveRange(cartItems); //刪除商品
                db.SaveChanges();

                if (db.CartItems.Any(ci => ci.CartId == cartIdInfo))
                {
                    var cartInfo = db.Carts.Where(c => c.UserId == CustomerId && c.IsPay == false).Include(c => c.CartItem)
                                .Select(cart => new
                                {
                                    totalOriginalPrice = cart.CartItem.Sum(c => c.Spec.Price * c.Qty),
                                    totalPromotionPrice = cart.CartItem.Sum(c => c.IsLivePrice == true ? c.Spec.LivePrice * c.Qty : c.Spec.PromotePrice * c.Qty),
                                }).Select(d => new
                                {
                                    d.totalOriginalPrice,
                                    d.totalPromotionPrice,
                                    totalDiscount = d.totalOriginalPrice - d.totalPromotionPrice,
                                });

                    var cartItemInfo = db.CartItems.Where(c => c.CartId == cartIdInfo).GroupBy(gruop => gruop.SpecId)
                                            .Select(cartItemGruop => new
                                            {
                                                productId = cartItemGruop.FirstOrDefault().Spec.ProductId,
                                                productTitle = cartItemGruop.FirstOrDefault().Spec.Product.ProductTitle, // Spec--Product--Title
                                                productSpecId = cartItemGruop.Key,
                                                productSpecSize = cartItemGruop.FirstOrDefault().Spec.Size,
                                                productSpecWeight = (int)cartItemGruop.FirstOrDefault().Spec.Weight,
                                                cartItemOriginalPrice = cartItemGruop.FirstOrDefault().Spec.Price,
                                                cartItemPromotionPrice = cartItemGruop.FirstOrDefault().Spec.PromotePrice,
                                                cartItemLivePrice = cartItemGruop.FirstOrDefault().IsLivePrice == true ? cartItemGruop.FirstOrDefault().Spec.LivePrice : (decimal?)null,
                                                cartItemQty = cartItemGruop.Sum(item => item.Qty),
                                                subtotal = cartItemGruop.Sum(item => item.IsLivePrice == true ? item.Qty * item.Spec.LivePrice : item.Qty * item.Spec.PromotePrice),
                                                productImg = new
                                                {
                                                    src = db.Albums.Where(a => a.ProductId == cartItemGruop.FirstOrDefault().Spec.ProductId).FirstOrDefault().Photo.FirstOrDefault().URL ?? null,
                                                    alt = cartItemGruop.FirstOrDefault().Spec.Product.ProductTitle,
                                                },
                                            }).ToList();

                    var cartItemLength = cartItemInfo.Select(item => item.productSpecId).Distinct().Count();

                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "刪除成功",
                        data = new
                        {
                            cartItemLength,
                            cartInfo,
                            cartItemInfo
                        }
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    var emptyCartResult = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "購物車已清空",
                        data = new object[] { }
                    };
                    return Content(HttpStatusCode.OK, emptyCartResult);
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

        #endregion FGC-05 刪除特定商品

        /// <summary>
        /// 取得購物車產品資訊
        /// </summary>
        public class GetCartItemClass
        {
            [Display(Name = "產品編號")]
            public int productId { get; set; }

            [Display(Name = "規格spec編號")]
            public int productSpecId { get; set; }

            [Display(Name = "數量")]
            public int cartItemQty { get; set; }

            public int? liveId { get; set; }
        }
    }
}