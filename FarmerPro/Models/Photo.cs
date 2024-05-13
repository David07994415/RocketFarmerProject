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

        [JsonIgnore]
        [ForeignKey("AlbumId")]
        [Display(Name = "產品表單")]
        public virtual Album Album { get; set; }

        [Display(Name = "建立時間")]
        public DateTime CreatTime { get; set; } = DateTime.Now;
    }
}