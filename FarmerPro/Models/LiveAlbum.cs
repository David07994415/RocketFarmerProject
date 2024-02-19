using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmerPro.Models
{
    [Table("LiveAlbum")]
    public class LiveAlbum
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "編號")]
        public int Id { get; set; }

        [Display(Name = "使用者Id")]   // 先不要設定為外鍵
        public int UserId { get; set; }

        [Display(Name = "直播Id")]   // 先不要設定為外鍵
        public int LiveId { get; set; }

        [Required]
        [Display(Name = "照片URL")]
        public string Photo { get; set; }

        [Display(Name = "建立時間")]
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}