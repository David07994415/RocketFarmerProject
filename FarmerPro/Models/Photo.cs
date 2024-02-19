using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmerPro.Models
{
    [Table("Photo")]
    public class Photo
    {
        [Key]
        [Display(Name = "編號")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Display(Name = "相片路徑")]
        public string URL { get; set; }


        [Display(Name = "相簿Id")]
        public int AlbumId { get; set; }
        [JsonIgnore]//不會產生無限迴圈
        [ForeignKey("AlbumId")]
        [Display(Name = "產品表單")]
        public virtual Album Album { get; set; }//virtual=虛擬資料，會跟資料庫的對應資料相對應

        [Display(Name = "建立時間")]
        public DateTime CreatTime { get; set; } = DateTime.Now;
    }
}