﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MoneyTree.Models {
    public class Project {

        [Required]
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Project Name")]
        public string ProjectName { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime StartDate { get; set; }

        [Display(Name = "Completion Date")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime CompletionDate { get; set; }

        [Display(Name = "Amount Charged")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public int AmountCharged { get; set; }

        public int CustomerId { get; set; }

        [Required]
        public int UserId { get; set; }

        public Customer Customer { get; set; }

        public ApplicationUser User { get; set; }
    }
}
