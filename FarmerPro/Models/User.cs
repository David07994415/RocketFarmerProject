using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;

namespace FarmerPro.Models
{
    public class User
    {
        [Key]
        [Display(Name = "編號")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Display(Name = "用戶類型")]
        public UserCategory Category { set; get; }

        [Required]
        [MaxLength(500)]
        [Display(Name = "帳號")]
        public string Account { get; set; }

        [Display(Name = "GUID驗證碼")]
        public Guid? EmailGUID { get; set; }

        [Required]
        [MaxLength(500)]
        [Display(Name = "密碼")]
        public string Password { get; set; }

        [Required]
        [MaxLength(500)]
        [Display(Name = "加鹽")]
        public string Salt { get; set; }

        [MaxLength(500)]
        [Display(Name = "金鑰")]
        public string Token { get; set; }

        [MaxLength(500)]
        [Display(Name = "暱稱")]
        public string NickName { get; set; }

        [Display(Name = "照片")]
        public string Photo { get; set; }

        [Display(Name = "生日")]
        public DateTime? Birthday { get; set; }

        [Display(Name = "性別")]
        public bool? Sex { get; set; }

        [MaxLength(100)]
        [Display(Name = "電話")]
        public string Phone { get; set; }

        [Display(Name = "小農理念")]
        public string Vision { get; set; }

        [Display(Name = "自我介紹")]
        public string Description { get; set; }

        [Display(Name = "建立時間")]
        public DateTime CreatTime { get; set; } = DateTime.Now;


        [Display(Name = "商品")]//其他表-外鍵
        public virtual ICollection<Product> Product { get; set; }

        [Display(Name = "1-1聊天室")]//其他表-外鍵
        public virtual ICollection<ChatRoom> ChatRoom { get; set; }
    }
}