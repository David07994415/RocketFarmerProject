using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using Newtonsoft.Json;

namespace FarmerPro.Models
{
    [Table("OrderFarmer")]
    public class OrderFarmer
    {
        [Key]
        [Display(Name = "編號")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "使用者Id")]
        public int UserId { get; set; }

        [Display(Name = "建立時間")]
        public DateTime CreatTime { get; set; } = DateTime.Now;

        [Display(Name = "訂單標號外鍵")]
        public int OrderId { get; set; }

        [JsonIgnore]//不會產生無限迴圈
        [ForeignKey("OrderId")]
        [Display(Name = "使用者表單")]
        public virtual Order Order { get; set; }//virtual=虛擬資料，會跟資料庫的對應資料相對應
    }
}