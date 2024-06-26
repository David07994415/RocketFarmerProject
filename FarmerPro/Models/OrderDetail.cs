﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmerPro.Models
{
    [Table("OrderDetail")]
    public class OrderDetail
    {
        [Key]
        [Display(Name = "編號")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "數量")]
        public int Qty { get; set; }

        [Display(Name = "小計")]
        public double SubTotal { get; set; }

        [Display(Name = "建立時間")]
        public DateTime CreatTime { get; set; } = DateTime.Now;

        [Display(Name = "SpecId")]
        public int SpecId { get; set; }

        [JsonIgnore]
        [ForeignKey("SpecId")]
        [Display(Name = "Spec表單")]
        public virtual Spec Spec { get; set; }

        [Display(Name = "OrderId")]
        public int OrderId { get; set; }

        [JsonIgnore]
        [ForeignKey("OrderId")]
        [Display(Name = "訂單表單")]
        public virtual Order Order { get; set; }
    }
}