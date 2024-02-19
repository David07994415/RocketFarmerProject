using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmerPro.Models.ViewModel
{
    public class Register
    {
        [Required]
        public UserCategory category { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
        public string account { get; set; }

        [Required]
        [MinLength(6)]
        public string password { get; set; }
    }
}