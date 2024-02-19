using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmerPro.Models
{
    [Table("Record")]
    public class Record
    {
        [Key]
        [Display(Name = "編號")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "使用者Id=>發訊者")]
        public int UserIdSender { get; set; }

        [Required]
        [MaxLength(500)]
        public string Message { get; set; }

        [Display(Name = "已讀狀態")]
        public bool IsRead { get; set; } = false;


        [Display(Name = "建立時間")]
        public DateTime CreatTime { get; set; } = DateTime.Now;


        [Display(Name = "1-1聊天室Id")]
        public int ChatRoomId { get; set; }
        [JsonIgnore]//不會產生無限迴圈
        [ForeignKey("ChatRoomId")]
        [Display(Name = "1-1聊天室")]
        public virtual ChatRoom ChatRoom { get; set; }//virtual=虛擬資料，會跟資料庫的對應資料相對應


    }
}