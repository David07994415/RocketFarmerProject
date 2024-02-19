using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace FarmerPro.Models
{
    [Table("CartItem")]
    public class CartItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "編號")]
        public int Id { get; set; }

        [Display(Name = "數量")]
        public int Qty { get; set; }

        [Display(Name = "小計")]
        public double SubTotal { get; set; }

        [Display(Name = "建立時間")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        [Display(Name = "購物車Id")]
        public int CartId { get; set; }

        [JsonIgnore]//不會產生無限迴圈
        [ForeignKey("CartId")]
        public virtual Cart Cart { get; set; }//virtual=虛擬資料，會跟資料庫的對應資料相對應

        [Display(Name = "SpecId")]
        public int SpecId { get; set; }

        [JsonIgnore]//不會產生無限迴圈
        [ForeignKey("SpecId")]
        public virtual Spec Spec { get; set; }//virtual=虛擬資料，會跟資料庫的對應資料相對應

        [Display(Name = "是否為直播價")]
        public bool? IsLivePrice { get; set; } = false;
    }
}