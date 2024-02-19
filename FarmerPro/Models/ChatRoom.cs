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
        //[JsonIgnore]//不會產生無限迴圈
        //[ForeignKey("UserIdOwner")]
        //[Display(Name = "使用者表單Owner")]
        //public virtual User UserOwner { get; set; }//virtual=虛擬資料，會跟資料庫的對應資料相對應


        [Display(Name = "聊天室對談者ID")]
        public int UserIdTalker { get; set; }
        [JsonIgnore]//不會產生無限迴圈
        [ForeignKey("UserIdTalker")]
        [Display(Name = "使用者表單Talker")]
        public virtual User UserTalker { get; set; }//virtual=虛擬資料，會跟資料庫的對應資料相對應


        [Display(Name = "建立時間")]
        public DateTime CreatTime { get; set; } = DateTime.Now;

        [Display(Name = "訊息")]
        public virtual ICollection<Record> Record { get; set; }
    }
}