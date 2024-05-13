using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace FarmerPro.Models
{
    [Table("Product")]
    public class Product
    {
        [Key]
        [Display(Name = "編號")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        [Display(Name = "農產品名稱")]
        public string ProductTitle { get; set; }

        [Required]
        [Display(Name = "產品分類")]
        public ProductCategory Category { set; get; }

        [Required]
        [Display(Name = "季節")]
        public ProductPeriod Period { set; get; }

        [Required]
        [Display(Name = "產地")]
        public ProductOrigin Origin { set; get; }

        [Required]
        [Display(Name = "保存方式")]
        public ProductStorage Storage { set; get; }

        [MaxLength(500)]
        [Display(Name = "農產品簡述")]
        public string Description { get; set; }

        [Display(Name = "農產品介紹")]
        public string Introduction { get; set; }

        [Display(Name = "上架狀態")]
        public bool ProductState { get; set; } = false;

        [Display(Name = "上架時間")]
        public DateTime? UpdateStateTime { get; set; }

        [Display(Name = "建立時間")]
        public DateTime CreatTime { get; set; } = DateTime.Now;

        [Display(Name = "刪除狀態")]
        public bool IsDelete { get; set; } = false;

        [Display(Name = "使用者")]
        public int UserId { get; set; }
        [JsonIgnore]
        [ForeignKey("UserId")]
        public virtual User Users { get; set; }


        [Display(Name = "種類")]
        public virtual ICollection<Spec> Spec { get; set; }

    }
}