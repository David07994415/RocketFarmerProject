using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace FarmerPro.Models
{
    [Table("Credential")]
    public class Credential
    {
        [Key]
        [Display(Name = "編號")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "CredentialId")]
        [Required]
        public byte[] CredentialId { get; set; }

        [Display(Name = "公鑰")]
        [Required]
        public byte[] PublicKey { get; set; }

        [Display(Name = "建立時間")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        [Display(Name = "User Id")]
        public int UserId { get; set; }

        [JsonIgnore]
        [ForeignKey("UserId")]
        [Display(Name = "User 表單")]
        public virtual User User { get; set; }
    }
}