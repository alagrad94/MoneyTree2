﻿using MoneyTree.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Linq;

namespace MoneyTree.Models
{

    public class CostItem {

        private readonly ApplicationDbContext _context;

        public CostItem() {

        }

        public CostItem(ApplicationDbContext context) {

            _context = context;
        }

        [Required]
        [Key]
        public int Id { get; set; }

        [Display(Name = "Item Name")]
        public string ItemName { get; set; }

        [Required]
        public int UnitOfMeasureId { get; set; }

        [Required]
        public int CostCategoryId { get; set; }

        [Display(Name = "Unit of Measure")]
        public UnitOfMeasure UnitOfMeasure { get; set; }

        [Display(Name = "Cost Category")]
        public CostCategory CostCategory { get; set; }

        public CostPerUnit CurrentCost {
            get {

                return GetCurrentCost();
            }
        }

        private CostPerUnit GetCurrentCost () {

            CostPerUnit CuurentCostPerUnit = _context.CostPerUnit.Where(cpu => cpu.CostItemId == Id)
                                                        .FirstOrDefault(cpu => cpu.EndDate == null);
            return CuurentCostPerUnit;
        }
    }
}
 