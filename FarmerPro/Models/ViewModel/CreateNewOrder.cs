using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Data.Entity.Core.Metadata.Edm;
using System.Web.UI.WebControls;
using FarmerPro.Models;

namespace FarmerPro.Models.ViewModel
{
    public class CreateNewOrder
    {

        [Required]
        [Display(Name = "收貨人")]
        [MaxLength(100)]
        public string receiver { get; set; }

        [Required]
        [Display(Name = "電話")]
        [RegularExpression(@"^\d{10}$")]
        public string phone { get; set; }

        [Required]
        [Display(Name = "城市")]
        public City city { get; set; }

        [Required]
        [Display(Name = "區域")]
        public string district { get; set; }

        [Display(Name = "郵遞區號")]
        public int zipCode { get; set; }

        [Display(Name = "訂單總價")]
        public double orderSum { get; set; }

        [Display(Name = "訂單Id")]
        public int cartId { get; set; }

        [Required]
        [Display(Name = "地址")]
        [MaxLength(300)]
        public string address { get; set; }

        [Display(Name = "購物車清單")]
        public List<OrderItem> cartList { get; set; }
    }

    public class OrderItem
    {
        [Display(Name = "產品ID")]
        public int productId { get; set; }


        [Display(Name = "數量")]
        public int qty { get; set; }

        [Display(Name = "規格編號")]      //要傳規格編號比較好找資料
        public int specId { get; set; }

        [Display(Name = "小計")]
        public double subTotal { get; set; }
    }



}