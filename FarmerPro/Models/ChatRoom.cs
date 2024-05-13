using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmerPro.Models
{
    [Table("ChatRoom")]
    public class ChatRoom
    {
        [Key]
        [Display(Name = "編號")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "聊天室擁有者ID")]
        public int UserIdOwner { get; set; }

        [Display(Name = "聊天室對談者ID")]
        public int UserIdTalker { get; set; }

        [JsonIgnore]
        [ForeignKey("UserIdTalker")]
        [Display(Name = "使用者表單Talker")]
        public virtual User UserTalker { get; set; }

        [Display(Name = "建立時間")]
        public DateTime CreatTime { get; set; } = DateTime.Now;

        [Display(Name = "訊息")]
        public virtual ICollection<Record> Record { get; set; }
    }
}