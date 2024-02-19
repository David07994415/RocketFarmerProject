using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace FarmerPro.Models
{
    [Table("Spec")]
    public class Spec
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "編號")]
        public int Id { get; set; }

        [Display(Name = "原價")]
        public int Price { get; set; }

        [Display(Name = "庫存量")]
        public int Stock { get; set; }

        [Display(Name = "促銷價")]
        public int? PromotePrice { get; set; }

        [Display(Name = "直播價")]
        public int? LivePrice { get; set; }

        [Display(Name = "規格大小")]
        public bool Size { get; set; }

        [Display(Name = "規格重量")]
        public double Weight { get; set; }

        [Display(Name = "建立時間")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        [Display(Name = "商品")]//鍵結主鍵名
        public int ProductId { get; set; }

        [JsonIgnore]//不會產生無限迴圈
        [ForeignKey("ProductId")]//本表外鍵名
        public virtual Product Product { get; set; }

        [Display(Name = "訂單明細")]
        public virtual ICollection<OrderDetail> OrderDetail { get; set; }

        [Display(Name = "購物車商品列表")]//其他表-外鍵
        public virtual ICollection<CartItem> CartItem { get; set; }

        [Display(Name = "銷量")]
        public int Sales { get; set; } = 0;

        [Display(Name = "是否為直播價")]
        public bool? IsLivePrice { get; set; } = false;

        [Display(Name = "刪除狀態")]
        public bool IsDelete { get; set; } = false;
    }
}