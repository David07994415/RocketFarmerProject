using FarmerPro.Models;
using FarmerPro.Models.ViewModel;
using Newtonsoft.Json;
using NSwag.Annotations;
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
    [OpenApiTag("Product", Description = "商品")]
    public class ProductController : ApiController
    {
        private FarmerProDB db = new FarmerProDB();

        #region FGP-01 取得所有商品

        /// <summary>
        /// FGP-01 取得所有商品
        /// </summary>
        /// <param></param>
        /// <returns>返回所有商品的 JSON 物件</returns>
        [HttpGet]
        [Route("api/product/all")]
        public IHttpActionResult productall()
        {
            try
            {
                var productInfo = from p in db.Products
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
                var result = new
                {
                    statusCode = 200,
                    status = "success",
                    message = "取得成功",
                    data = productInfo.ToList()
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

        #endregion FGP-01 取得所有商品

        #region FGP-02 取得live商品、熱銷商品、特價促銷商品、水果商品、蔬菜商品

        /// <summary>
        /// FGP-02 取得live商品、熱銷商品、特價促銷商品、水果商品、蔬菜商品
        /// </summary>
        /// <param></param>
        /// <returns>返回特定筆數商品的 JSON 物件</returns>
        [HttpGet]
        [Route("api/product")]
        public IHttpActionResult productindex(int topsalesqty = 6, int promoteqty = 4, int fruitqty = 3, int vegatqty = 3)
        {
            try
            {
                var topSaleProduct = (from p in db.Products
                                    join s in db.Specs on p.Id equals s.ProductId
                                    from album in db.Albums.Where(a => p.Id == a.ProductId).DefaultIfEmpty()
                                    let photo = db.Photos.FirstOrDefault(ph => album != null && album.Id == ph.AlbumId)
                                    where p.ProductState && !s.Size
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

                var promotionProducts = promotionProduct.ToList();

                var randomPromotionProducts = promotionProducts.OrderBy(x => Guid.NewGuid()).Take(promoteqty).ToList();

                var fruitProduct = from p in db.Products
                                join s in db.Specs on p.Id equals s.ProductId
                                from album in db.Albums.Where(a => p.Id == a.ProductId).DefaultIfEmpty()
                                let photo = db.Photos.FirstOrDefault(ph => album != null && album.Id == ph.AlbumId)
                                where ((int)p.Category) == 1 && p.ProductState && !s.Size
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
                                    where ((int)p.Category) == 0 && p.ProductState && !s.Size
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
                var result = new
                {
                    statusCode = 500,
                    status = "error",
                    message = "其他錯誤",
                };
                return Content(HttpStatusCode.OK, result);
            }
        }

        #endregion FGP-02 取得live商品、熱銷商品、特價促銷商品、水果商品、蔬菜商品

        #region FGP-03 取得特定商品細節資訊(包含小農介紹、小農商品推薦4筆)

        /// <summary>
        /// FGP-03 取得特定商品細節資訊(包含小農介紹、小農商品推薦4筆)
        /// </summary>
        /// <param name="productId">提供商品Id</param>
        /// <returns>返回特定商品的 JSON 物件</returns>
        [HttpGet]
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
                                        largeproductSpecId = largeSpec.Id,
                                        largeOriginalPrice = largeSpec != null ? (int?)largeSpec.Price : null,
                                        largePromotionPrice = largeSpec != null ? (int?)largeSpec.PromotePrice : null,
                                        largeWeight = largeSpec != null ? (int?)largeSpec.Weight : null,
                                        largeStock = largeSpec != null ? (int?)largeSpec.Stock : null,
                                        smallproductSpecId = smallSpec.Id,
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

                var productUserId = db.Products
                                .Where(p => p.Id == productId && p.ProductState)
                                .Select(p => new { p.Id, p.UserId })
                                .FirstOrDefault();

                var productInfoByUser = from p in db.Products
                                        join s in db.Specs on p.Id equals s.ProductId
                                        from album in db.Albums.Where(a => p.Id == a.ProductId).DefaultIfEmpty()
                                        let photo = db.Photos.FirstOrDefault(ph => album != null && album.Id == ph.AlbumId)
                                        where p.UserId == productUserId.UserId
                                              && p.Id != productId
                                              && p.ProductState && !s.Size
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
                var result = new
                {
                    statusCode = 500,
                    status = "error",
                    message = "其他錯誤",
                };
                return Content(HttpStatusCode.OK, result);
            }
        }

        #endregion FGP-03 取得特定商品細節資訊(包含小農介紹、小農商品推薦4筆)

        #region FCI-01 搜尋特定商品(input)

        /// <summary>
        /// FCI-01 搜尋特定商品(input)
        /// </summary>
        /// <param name="input">提供商品名稱</param>
        /// <returns>返回搜尋商品的 JSON 物件</returns>
        [HttpPost]
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
                                    where p.ProductState && !s.Size
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
                        data = searchProduct.ToList(),
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

        #endregion FCI-01 搜尋特定商品(input)
    }

    /// <summary>
    /// 搜尋商品名
    /// </summary>
    public class SerchProduct
    {
        [Display(Name = "搜尋")]
        public string serchQuery { get; set; }
    }
}