using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace FarmerPro.Models.ViewModel
{
    public class CreateNewLiveSetting
    {
        [Display(Name = "access token")]
        public string accessToken { get; set; }

        [Display(Name = "IssueAt")]
        public string issue { get; set; }

        [Display(Name = "IssueAt UTC")]
        public string issueUTC { get; set; }

        [Display(Name = "直播Id")]
        public int liveId { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "直播名稱")]
        public string liveName { get; set; }

        [Display(Name = "直播日期")]
        [DataType(DataType.Date)]
        public DateTime liveDate { get; set; }

        [Display(Name = "開始時間")]
        [DataType(DataType.Time)]
        public TimeSpan startTime { get; set; }

        [Display(Name = "結束時間")]
        [DataType(DataType.Time)]
        public TimeSpan endTime { get; set; }

        [Display(Name = "直播圖片")]
        public string livePic { get; set; }    

        [Required]
        [Display(Name = "YT直播網址")]  
        public string yturl { get; set; }

        [Display(Name = "直播產品")]
        public List<CreateNewLiveProudct> liveproduct { get; set; }
    }

    public class CreateNewLiveProudct
    {
        [Display(Name = "產品Id")]
        public int productId { get; set; }

        [Display(Name = "產品尺寸")]
        public bool productSize { get; set; }

        [Display(Name = "直播價格")]
        public int liveprice { get; set; }
    }
}