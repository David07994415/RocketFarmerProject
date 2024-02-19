using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmerPro.Models
{
    [Table("Order")]
    public class Order
    {
        [Key]
        [Display(Name = "編號")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Display(Name = "收貨人")]
        [MaxLength(100)]
        public string Receiver { get; set; }

        [Required]
        [Display(Name = "電話")]
        [MaxLength(100)]
        public string Phone { get; set; }

        [Required]
        [Display(Name = "城市")]
        public City City { get; set; }

        [Required]
        [Display(Name = "區域")]
        public string District { get; set; }

        [Display(Name = "郵遞區號")]
        public int ZipCode { get; set; }

        [Required]
        [Display(Name = "地址")]
        [MaxLength(300)]
        public string Address { get; set; }

        [Display(Name = "運費")]
        public double DeliveryFee { get; set; }

        [Display(Name = "總價")]
        public double OrderSum { get; set; }

        [Display(Name = "出貨狀態")]
        public bool Shipment { get; set; } = false;

        [Display(Name = "訂單編號")]
        public Guid Guid { get; set; }

        [Display(Name = "藍新繳費時間")]
        public DateTime? PaymentTime { get; set; }

        [Display(Name = "藍新繳費狀態")]
        public bool? IsPay { get; set; }

        [Display(Name = "藍新欄位1")]
        [MaxLength(100)]
        public string MerchantID { get; set; }

        [Display(Name = "藍新欄位2")]
        public string TradeInfo { get; set; }

        [Display(Name = "藍新欄位3")]
        public string TradeSha { get; set; }

        [MaxLength(20)]
        [Display(Name = "藍新欄位4")]
        public string Version { get; set; }

        [Display(Name = "建立時間")]
        public DateTime CreatTime { get; set; } = DateTime.Now;



        [Display(Name = "使用者Id")]  // 先不要設定為外鍵
        public int UserId { get; set; }
        //[JsonIgnore]//不會產生無限迴圈
        //[ForeignKey("UserId")]
        //[Display(Name = "使用者表單")]
        //public virtual User User { get; set; }//virtual=虛擬資料，會跟資料庫的對應資料相對應


        [Display(Name = "訂單明細")]
        public virtual ICollection<OrderDetail> OrderDetail { get; set; }

    }
}