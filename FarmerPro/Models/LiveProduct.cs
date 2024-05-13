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

        [Display(Name = "直播設定")]
        public int LiveSettingId { get; set; }

        [JsonIgnore]
        [ForeignKey("LiveSettingId")]
        public virtual LiveSetting LiveSetting { get; set; }

        [Display(Name = "規格")]
        public int SpecId { get; set; }

        [JsonIgnore]
        [ForeignKey("SpecId")]
        public virtual Spec Spec { get; set; }
    }
}