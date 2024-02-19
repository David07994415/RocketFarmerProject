using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmerPro.Models
{
    [Table("Album")]
    public class Album
    {
        [Key]
        [Display(Name = "編號")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "產品Id")]   // 先不要設定為外鍵
        public int ProductId { get; set; }

        [Display(Name = "使用者Id")]   // 先不要設定為外鍵
        public int UserId { get; set; }

        [Display(Name = "建立時間")]
        public DateTime CreatTime { get; set; } = DateTime.Now;

        public virtual ICollection<Photo> Photo { get; set; }
    }
}