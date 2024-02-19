using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace FarmerPro.Models
{
    [Table("LiveProduct")]
    public class LiveProduct
    {
        [Key]
        [Display(Name = "編號")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "留言板置頂")]
        public bool IsTop { get; set; } = false;

        [Display(Name = "建立時間")]
        public DateTime CreatTime { get; set; } = DateTime.Now;


        [Display(Name = "直播設定")]//其他表-主鍵
        public int LiveSettingId { get; set; }
        [JsonIgnore]//不會產生無限迴圈
        [ForeignKey("LiveSettingId")]//本表外鍵名
        public virtual LiveSetting LiveSetting { get; set; }

        [Display(Name = "規格")]//其他表-主鍵
        public int SpecId { get; set; }
        [JsonIgnore]//不會產生無限迴圈
        [ForeignKey("SpecId")]//本表外鍵名
        public virtual Spec Spec { get; set; }

    }
}