using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace BarManegment.Models
{
    public class JobTitle
    {
        public int Id { get; set; }

        [Required, Display(Name = "المسمى الوظيفي")]
        public string Name { get; set; }
    }
}