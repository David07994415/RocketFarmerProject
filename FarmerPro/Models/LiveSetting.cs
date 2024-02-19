using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace FarmerPro.Models
{
    [Table("LiveSetting")]
    public class LiveSetting
    {
        [Key]
        [Display(Name = "編號")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        [Display(Name = "直播名稱")]
        public string LiveName { get; set; }

        [Display(Name = "直播日期")]
        public DateTime LiveDate { get; set; }

        [Display(Name = "開始時間")]
        public TimeSpan StartTime { get; set; }

        [Display(Name = "結束時間")]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "直播圖片")]
        public string LivePic { get; set; }

        [Required]
        [Display(Name = "YT直播網址")]
        public string YTURL { get; set; }

        [Required]
        [Display(Name = "分享直播網址")]
        public string ShareURL { get; set; }

        [Display(Name = "建立時間")]
        public DateTime CreatTime { get; set; } = DateTime.Now;


        [Display(Name = "使用者Id")]  // 先不要設定為外鍵
        public int UserId { get; set; }
        //[JsonIgnore]//不會產生無限迴圈
        //[ForeignKey("UserId")]//本表外鍵名
        //public virtual User Users { get; set; }


        [Display(Name = "直播產品")]//其他表-外鍵
        public virtual ICollection<LiveProduct> LiveProduct { get; set; }
    }
}