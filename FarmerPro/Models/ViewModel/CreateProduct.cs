using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmerPro.Models.ViewModel
{
    public class CreateProduct
    {
        [Required]
        [MaxLength(100)]
        [Display(Name = "農產品名稱")]
        public string productTitle { get; set; }

        [Required]
        [Display(Name = "產品分類")]
        public ProductCategory category { set; get; }

        [Required]
        [Display(Name = "季節")]
        public ProductPeriod period { set; get; }

        [Required]
        [Display(Name = "產地")]
        public ProductOrigin origin { set; get; }

        [Required]
        [Display(Name = "保存方式")]
        public ProductStorage storage { set; get; }

        [MaxLength(500)]
        [Display(Name = "農產品簡述")]
        public string description { get; set; }

        [Display(Name = "農產品介紹")]
        public string introduction { get; set; }

        [Display(Name = "上架狀態")]
        public bool productState { get; set; }

        [Display(Name = "上架時間")]
        public DateTime updateStateTime { get; set; }


        [Display(Name = "大產品原價")]
        public int largeOriginalPrice { get; set; }

        [Display(Name = "大產品庫存量")]
        public int largeStock { get; set; }

        [Display(Name = "大產品促銷價")]
        public int? largePromotionPrice { get; set; }

        [Display(Name = "大產品規格重量")]
        public double largeWeight { get; set; }


        [Display(Name = "小產品原價")]
        public int smallOriginalPrice { get; set; }

        [Display(Name = "小產品庫存量")]
        public int smallStock { get; set; }

        [Display(Name = "小產品促銷價")]
        public int? smallPromotionPrice { get; set; }

        [Display(Name = "小產品規格重量")]
        public double smallWeight { get; set; }
    }
}