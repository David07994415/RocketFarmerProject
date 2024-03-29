﻿using FarmerPro.Models;
using FarmerPro.Models.ViewModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;

namespace FarmerPro.Controllers
{
    public class ProductController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        /// <summary>
        /// 取得所有商品
        /// </summary>

        #region FGP-1 取得所有產品

        [HttpGet]
        //自定義路由
        [Route("api/product/all")]
        public IHttpActionResult productall()
        {
            //try
            //{
            //取得Product、Spec、Album、Photo的聯合資料
            var productInfo = from p in db.Products
                              join s in db.Specs on p.Id equals s.ProductId
                              from album in db.Albums.Where(a => p.Id == a.ProductId).DefaultIfEmpty()
                              let photo = db.Photos.FirstOrDefault(ph => album != null && album.Id == ph.AlbumId)
                              where p.ProductState && !s.Size // 確認p.ProductState = true && s.Size = false
                              orderby p.CreatTime descending
                              select new
                              {
                                  productId = p.Id,
                                  productSpecId = s.Id,
                                  productTitle = p.ProductTitle,
                                  smallOriginalPrice = s.Price,
                                  smallPromotionPrice = s.PromotePrice,
                                  productImg = new
                                  {
                                      src = photo != null ? photo.URL : null,
                                      alt = p.ProductTitle
                                  }
                              };

            if (!productInfo.Any())
            {
                //result訊息
                var result = new
                {
                    statusCode = 400,
                    status = "error",
                    message = "取得失敗",
                };
                return Content(HttpStatusCode.OK, result);
            }
            else
            {
                // result 訊息
                var result = new
                {
                    statusCode = 200,
                    status = "success",
                    message = "取得成功",
                    data = productInfo.ToList()
                };
                return Content(HttpStatusCode.OK, result);
            }
            //}
            //catch
            //{
            //    //result訊息
            //    var result = new
            //    {
            //        statusCode = 500,
            //        status = "error",
            //        message = "其他錯誤",
            //    };
            //    return Content(HttpStatusCode.OK, result);
            //}
        }

        #endregion FGP-1 取得所有產品

        #region FGP-2 取得live產品、熱銷產品、特價促銷產品、水果產品、蔬菜產品

        [HttpGet]
        //自定義路由
        [Route("api/product")]
        public IHttpActionResult productindex(int topsalesqty = 6, int promoteqty = 4, int fruitqty = 3, int vegatqty = 3)
        {
            try
            {
                var topSaleProduct = (from p in db.Products
                                      join s in db.Specs on p.Id equals s.ProductId
                                      from album in db.Albums.Where(a => p.Id == a.ProductId).DefaultIfEmpty()
                                      let photo = db.Photos.FirstOrDefault(ph => album != null && album.Id == ph.AlbumId)
                                      where p.ProductState && !s.Size // 確認p.ProductState = true && s.Size = false
                                      orderby s.Sales descending, p.CreatTime descending
                                      select new
                                      {
                                          productId = p.Id,
                                          productSpecId = s.Id,
                                          productTitle = p.ProductTitle,
                                          description = p.Description,
                                          smallOriginalPrice = s.Price,
                                          smallPromotionPrice = s.PromotePrice,
                                          productImg = new
                                          {
                                              src = photo != null ? photo.URL : null,
                                              alt = p.ProductTitle
                                          },
                                      }).Take(topsalesqty);

                var promotionProduct = from p in db.Products
                                       join user in db.Users on p.UserId equals user.Id
                                       join s in db.Specs on p.Id equals s.ProductId
                                       from album in db.Albums.Where(a => p.Id == a.ProductId).DefaultIfEmpty()
                                       let photo = db.Photos.FirstOrDefault(ph => album != null && album.Id == ph.AlbumId)
                                       where p.ProductState && !s.Size
                                       orderby p.CreatTime descending
                                       select new
                                       {
                                           productId = p.Id,
                                           productSpecId = s.Id,
                                           productTitle = p.ProductTitle,
                                           description = p.Description,
                                           farmerName = user.NickName,
                                           origin = p.Origin.ToString(),
                                           smallOriginalPrice = s.Price,
                                           smallPromotionPrice = s.PromotePrice,
                                           productImg = new
                                           {
                                               src = photo != null ? photo.URL : null,
                                               alt = p.ProductTitle
                                           },
                                           farmerImg = new
                                           {
                                               src = user.Photo != null ? user.Photo : null,
                                               alt = user.NickName
                                           }
                                       };

                // 轉列表
                var promotionProducts = promotionProduct.ToList();

                // 隨機
                var randomPromotionProducts = promotionProducts.OrderBy(x => Guid.NewGuid()).Take(promoteqty).ToList();

                var fruitProduct = from p in db.Products
                                   join s in db.Specs on p.Id equals s.ProductId
                                   from album in db.Albums.Where(a => p.Id == a.ProductId).DefaultIfEmpty()
                                   let photo = db.Photos.FirstOrDefault(ph => album != null && album.Id == ph.AlbumId)
                                   where ((int)p.Category) == 1 && p.ProductState && !s.Size // 確認p.ProductState = true && s.Size = false
                                   orderby p.CreatTime descending
                                   select new
                                   {
                                       productId = p.Id,
                                       productSpecId = s.Id,
                                       productTitle = p.ProductTitle,
                                       description = p.Description,
                                       smallOriginalPrice = s.Price,
                                       smallPromotionPrice = s.PromotePrice,
                                       productImg = new
                                       {
                                           src = photo != null ? photo.URL : null,
                                           alt = p.ProductTitle
                                       }
                                   };

                var fruitProducts = fruitProduct.ToList();
                var randomFruitProducts = fruitProducts.OrderBy(x => Guid.NewGuid()).Take(fruitqty).ToList();

                var vegetableProduct = from p in db.Products
                                       join s in db.Specs on p.Id equals s.ProductId
                                       from album in db.Albums.Where(a => p.Id == a.ProductId).DefaultIfEmpty()
                                       let photo = db.Photos.FirstOrDefault(ph => album != null && album.Id == ph.AlbumId)
                                       where ((int)p.Category) == 0 && p.ProductState && !s.Size // 確認p.ProductState = true && s.Size = false
                                       orderby p.CreatTime descending
                                       select new
                                       {
                                           productId = p.Id,
                                           productSpecId = s.Id,
                                           productTitle = p.ProductTitle,
                                           description = p.Description,
                                           smallOriginalPrice = s.Price,
                                           smallPromotionPrice = s.PromotePrice,
                                           productImg = new
                                           {
                                               src = photo != null ? photo.URL : null,
                                               alt = p.ProductTitle
                                           }
                                       };

                var vegetableProducts = vegetableProduct.ToList();
                var randomVegetableProducts = vegetableProducts.OrderBy(x => Guid.NewGuid()).Take(vegatqty).ToList();

                if (!promotionProduct.Any())
                {
                    //result訊息
                    var result = new
                    {
                        statusCode = 400,
                        status = "error",
                        message = "取得失敗",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    // result 訊息
                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "取得成功",
                        data = new
                        {
                            topSaleProduct = topSaleProduct.ToList(),
                            promotionProduct = randomPromotionProducts,
                            fruitProduct = randomFruitProducts,
                            vegetableProduct = randomVegetableProducts
                        }
                    };
                    return Content(HttpStatusCode.OK, result);
                }
            }
            catch
            {
                //result訊息
                var result = new
                {
                    statusCode = 500,
                    status = "error",
                    message = "其他錯誤",
                };
                return Content(HttpStatusCode.OK, result);
            }
        }

        #endregion FGP-2 取得live產品、熱銷產品、特價促銷產品、水果產品、蔬菜產品

        #region FGP-3 取得特定商品細節資訊(包含小農介紹、小農產品推薦4筆)

        [HttpGet]
        //自定義路由
        [Route("api/product/{productId}")]
        public IHttpActionResult productdetail(int productId)
        {
            try
            {
                var detailProduct = from p in db.Products
                                    join user in db.Users on p.UserId equals user.Id
                                    join s in db.Specs on p.Id equals s.ProductId
                                    from album in db.Albums.Where(a => p.Id == a.ProductId).DefaultIfEmpty()
                                    where p.ProductState && p.Id == productId
                                    orderby p.CreatTime descending
                                    let largeSpec = db.Specs.FirstOrDefault(s => s.ProductId == p.Id && s.Size) //大= F
                                    let smallSpec = db.Specs.FirstOrDefault(s => s.ProductId == p.Id && !s.Size)  //小 = T

                                    select new
                                    {
                                        productId = p.Id,
                                        productTitle = p.ProductTitle,
                                        category = p.Category.ToString(),
                                        period = p.Period.ToString(),
                                        origin = p.Origin.ToString(),
                                        storage = p.Storage.ToString(),
                                        productDescription = p.Description,
                                        introduction = p.Introduction,
                                        largeproductSpecId = largeSpec.Id,   //這邊要提供大的SPECID
                                        largeOriginalPrice = largeSpec != null ? (int?)largeSpec.Price : null,
                                        largePromotionPrice = largeSpec != null ? (int?)largeSpec.PromotePrice : null,
                                        largeWeight = largeSpec != null ? (int?)largeSpec.Weight : null,
                                        largeStock = largeSpec != null ? (int?)largeSpec.Stock : null,
                                        smallproductSpecId = smallSpec.Id,  //這邊要提供小的SPECID
                                        smallOriginalPrice = smallSpec != null ? (int?)smallSpec.Price : null,
                                        smallPromotionPrice = smallSpec != null ? (int?)smallSpec.PromotePrice : null,
                                        smallWeight = smallSpec != null ? (int?)smallSpec.Weight : null,
                                        smallStock = smallSpec != null ? (int?)smallSpec.Stock : null,
                                        productImages = db.Photos.Where(ph => album != null && album.Id == ph.AlbumId).Select(ph => new
                                        {
                                            src = ph.URL,
                                            alt = p.ProductTitle
                                        }).ToList(),
                                        farmerId = user.Id,
                                        farmerName = user.NickName,
                                        farmerVision = user.Vision,
                                        farmerDescription = user.Description,
                                        farmerImg = new
                                        {
                                            src = user.Photo != null ? user.Photo : null,
                                            alt = user.NickName
                                        }
                                    };

                //取得productId的UserId
                var productUserId = db.Products
                                .Where(p => p.Id == productId && p.ProductState)
                                .Select(p => new { p.Id, p.UserId })
                                .FirstOrDefault();

                //取得Product、Spec、Album、Photo的聯合資料
                var productInfoByUser = from p in db.Products
                                        join s in db.Specs on p.Id equals s.ProductId
                                        from album in db.Albums.Where(a => p.Id == a.ProductId).DefaultIfEmpty()
                                        let photo = db.Photos.FirstOrDefault(ph => album != null && album.Id == ph.AlbumId)
                                        where p.UserId == productUserId.UserId
                                              && p.Id != productId
                                              && p.ProductState && !s.Size // 確認p.ProductState = true && s.Size = false
                                        orderby p.CreatTime descending
                                        select new
                                        {
                                            productId = p.Id,
                                            productSpecId = s.Id,
                                            productTitle = p.ProductTitle,
                                            smallOriginalPrice = s.Price,
                                            smallPromotionPrice = s.PromotePrice,
                                            productImg = new
                                            {
                                                src = photo != null ? photo.URL : null,
                                                alt = p.ProductTitle
                                            }
                                        };

                if (!detailProduct.Any())
                {
                    //result訊息
                    var result = new
                    {
                        statusCode = 401,
                        status = "error",
                        message = "沒有此商品Id，請重新輸入",
                    };
                    return Content(HttpStatusCode.OK, result);
                }
                else
                {
                    // result 訊息
                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "取得成功",
                        data = new
                        {
                            detailProduct = detailProduct.FirstOrDefault(),
                            productInfoByUser = productInfoByUser.ToList(),
                        }
                    };
                    return Content(HttpStatusCode.OK, result);
                }
            }
            catch
            {
                //result訊息
                var result = new
                {
                    statusCode = 500,
                    status = "error",
                    message = "其他錯誤",
                };
                return Content(HttpStatusCode.OK, result);
            }
        }

        #endregion FGP-3 取得特定商品細節資訊(包含小農介紹、小農產品推薦4筆)

        #region FCI-1 搜尋特定產品(input)

        [HttpPost]
        //自定義路由
        [Route("api/product/search")]
        public IHttpActionResult productsearch([FromBody] SerchProduct input)
        {
            try
            {
                string searchCheck = input.serchQuery;

                var searchProduct = from p in db.Products
                                    join s in db.Specs on p.Id equals s.ProductId
                                    from album in db.Albums.Where(a => p.Id == a.ProductId).DefaultIfEmpty()
                                    let photo = db.Photos.FirstOrDefault(ph => album != null && album.Id == ph.AlbumId)
                                    where p.ProductState && !s.Size // 確認p.ProductState = true && s.Size = false
                                       && p.ProductTitle.Contains(searchCheck)
                                    orderby p.CreatTime descending
                                    select new
                                    {
                                        productId = p.Id,
                                        productSpecId = s.Id,
                                        productTitle = p.ProductTitle,
                                        description = p.Description,
                                        smallOriginalPrice = s.Price,
                                        smallPromotionPrice = s.PromotePrice,
                                        productImg = new
                                        {
                                            src = photo != null ? photo.URL : null,
                                            alt = p.ProductTitle
                                        },
                                    };

                if (!searchProduct.Any())
                {
                    //result訊息
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
                    // result 訊息
                    var result = new
                    {
                        statusCode = 200,
                        status = "success",
                        message = "取得成功",
                        data = searchProduct.ToList(),
                    };
                    return Content(HttpStatusCode.OK, result);
                }
            }
            catch
            {
                //result訊息
                var result = new
                {
                    statusCode = 500,
                    status = "error",
                    message = "其他錯誤",
                };
                return Content(HttpStatusCode.OK, result);
            }
        }

        #endregion FCI-1 搜尋特定產品(input)
    }

    public class SerchProduct
    {
        [Display(Name = "搜尋")]
        public string serchQuery { get; set; }
    }
}