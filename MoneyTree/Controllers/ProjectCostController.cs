﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MoneyTree.Data;
using MoneyTree.Models;
using MoneyTree.Models.ViewModels;

namespace MoneyTree.Controllers {

    public class ProjectCostController : Controller {

        private readonly ApplicationDbContext _context;

        public ProjectCostController(ApplicationDbContext context) {

            _context = context;
        }

        // GET: ProjectCosts/Create
        public IActionResult Create(int id) {

            CostPerUnitController.MaintainCostPerUnitRecords(_context);

            DateTime Today = DateTime.UtcNow;

            ProjectCostCreateViewModel model = new ProjectCostCreateViewModel {

                ProjectId = id,
                Costs = new List<ProjectCost>(),
                CostItems = _context.CostItem.ToList()
            };

            ProjectCost Cost = new ProjectCost {
                ProjectId = id,
                DateUsed = Today
            };

            model.Costs.Add(Cost);

            return View(model);
        }

        // POST: ProjectCosts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectCostCreateViewModel projectCosts) {

            List<ProjectCost> ProjectCostsInContext = await _context.ProjectCost.Where(pc => pc.ProjectId == projectCosts.ProjectId).ToListAsync();
            List<ProjectCost> CostsEntered = (projectCosts.Costs?.Count > 0) ? projectCosts.Costs :  projectCosts.RejectedEntries;
            List<ProjectCost> RejectecdEntries = new List<ProjectCost>();
            List<ProjectCost> UpdatedRecords = new List<ProjectCost>();

            DateTime CheckDate = DateTime.UtcNow.AddDays(-365);

            foreach (var cost in CostsEntered.ToList()) {

                ProjectCost ExistingCost = ProjectCostsInContext.FirstOrDefault(pc => pc.ProjectId == cost.ProjectId && pc.CostItemId == cost.CostItemId && pc.DateUsed == cost.DateUsed);

                if (cost.DateUsed <= CheckDate || cost.ProjectId == 0 || cost.CostItemId == 0) {

                    CostsEntered.Remove(cost);
                    RejectecdEntries.Add(cost);
                }

                if (ExistingCost != null) {

                    ExistingCost.Quantity += cost.Quantity;
                    CostsEntered.Remove(cost);
                    UpdatedRecords.Add(ExistingCost);

                    _context.Update(ExistingCost);
                    await _context.SaveChangesAsync();
                }
            }

            foreach (var projectCost in CostsEntered) {

                CostPerUnit CuurentCostPerUnit = _context.CostPerUnit.Where(cpu => cpu.CostItemId == projectCost.CostItemId).FirstOrDefault(cpu => cpu.EndDate == null);

                if (projectCost.DateUsed < CuurentCostPerUnit.StartDate) {

                    CostPerUnit CostPerUnitCorrectDate = _context.CostPerUnit.Where(cpu => cpu.CostItemId == projectCost.CostItemId).FirstOrDefault(cpu => projectCost.DateUsed <= cpu.EndDate && projectCost.DateUsed >= cpu.StartDate);

                    projectCost.CostPerUnitId = CostPerUnitCorrectDate.Id;

                } else {

                    projectCost.CostPerUnitId = CuurentCostPerUnit.Id;
                }

                _context.Add(projectCost);
                await _context.SaveChangesAsync();
            }

            if(RejectecdEntries.Count > 0 || UpdatedRecords.Count > 0) {

                if (RejectecdEntries.Count > 0) {

                    foreach (var item in RejectecdEntries) {

                        item.CostItem = _context.CostItem.FirstOrDefault(ci => ci.Id == item.CostItemId);
                        item.CostPerUnit = _context.CostPerUnit.FirstOrDefault(cpu => cpu.Id == item.CostPerUnitId);
                    }
                }

                if (UpdatedRecords.Count > 0) {

                    foreach (var item in UpdatedRecords) { 

                        item.CostItem = _context.CostItem.FirstOrDefault(ci => ci.Id == item.CostItemId);
                        item.CostPerUnit = _context.CostPerUnit.FirstOrDefault(cpu => cpu.Id == item.CostPerUnitId);
                    }
                }

                ProjectCostCreateViewModel viewModel = new ProjectCostCreateViewModel {

                    ProjectId = projectCosts.ProjectId,
                    CostItems = _context.CostItem.ToList(),
                    RejectedEntries = RejectecdEntries,
                    UpdatedRecords = UpdatedRecords
                };

                return View("CreateFinish", viewModel);

            } else {

                return RedirectToAction("Details", "Project", new { id = projectCosts.ProjectId });
            }
        }

        // GET: ProjectCosts/Edit/5
        public async Task<IActionResult> Edit(int? id) {

            if (id == null) {

                return NotFound();
            }

            var projectCost = await _context.ProjectCost.FindAsync(id);

            List<CostPerUnit> CostPerUnitList = await _context.CostPerUnit.Where(cpu => cpu.CostItemId == projectCost.CostItemId).OrderByDescending(cpu => cpu.StartDate).ToListAsync();

            List<SelectListItem> CPUSelectList = new List<SelectListItem>();

            foreach (var item in CostPerUnitList) {
                
                string StartDateString = item.StartDate.ToShortDateString();
                string EndDateString = item.EndDate?.ToShortDateString() ?? "----";

                CPUSelectList.Add(new SelectListItem {
                    Value = item.Id.ToString(),
                    Text = $"Cost: ${item.Cost} - Date Range: {StartDateString} - {EndDateString}"
                });
            }

            ViewData["CostItemId"] = new SelectList(_context.CostItem, "Id", "ItemName", projectCost.CostItemId);
            ViewData["ProjectId"] = new SelectList(_context.Project, "Id", "ProjectName", projectCost.ProjectId);
            ViewData["CostPerUnitId"] = CPUSelectList;
            return View(projectCost);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, 
            [Bind("Id,CostItemId,ProjectId,CostPerUnitId,DateUsed,Quantity")] ProjectCost projectCost) {

            if (id != projectCost.Id) {

                return NotFound();
            }

            if (ModelState.IsValid) {

                ProjectCost ExistingCost = _context.ProjectCost.FirstOrDefault(pc => pc.ProjectId == projectCost.ProjectId && pc.CostItemId == projectCost.CostItemId && pc.DateUsed == projectCost.DateUsed);

                if (ExistingCost != null && ExistingCost.Id != projectCost.Id) {

                    ExistingCost.Quantity += projectCost.Quantity;
                    _context.Update(ExistingCost);
                    _context.Remove(projectCost);
                    await _context.SaveChangesAsync();
                } else {

                    ExistingCost.ProjectId = projectCost.ProjectId;
                    ExistingCost.CostItemId = projectCost.CostItemId;
                    ExistingCost.CostPerUnitId = projectCost.CostPerUnitId;
                    ExistingCost.DateUsed = projectCost.DateUsed;
                    ExistingCost.Quantity = projectCost.Quantity;
                    _context.Update(ExistingCost);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("Details", "Project", new { id = projectCost.ProjectId });
            }

            List<CostPerUnit> CostPerUnitList = await _context.CostPerUnit.Where(cpu => cpu.CostItemId == projectCost.CostItemId).OrderByDescending(cpu => cpu.StartDate).ToListAsync();

            List<SelectListItem> CPUSelectList = new List<SelectListItem>();

            foreach (var item in CostPerUnitList) {
                
                string StartDateString = item.StartDate.ToShortDateString();
                string EndDateString = item.EndDate?.ToShortDateString() ?? "----";

                CPUSelectList.Add(new SelectListItem {
                    Value = item.Id.ToString(),
                    Text = $"Cost: ${item.Cost} - Date Range: {StartDateString} - {EndDateString}"
                });
            }

            ViewData["CostItemId"] = new SelectList(_context.CostItem, "Id", "ItemName", projectCost.CostItemId);
            ViewData["ProjectId"] = new SelectList(_context.Project, "Id", "ProjectName", projectCost.ProjectId);
            ViewData["CostPerUnitId"] = CPUSelectList;
            return View(projectCost);
        }

        // GET: ProjectCosts/Delete/5
        public async Task<IActionResult> Delete(int? id) {

            if (id == null) {

                return NotFound();
            }

            var projectCost = await _context.ProjectCost
                .Include(p => p.CostItem)
                .Include(p => p.Project)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (projectCost == null) {

                return NotFound();
            }

            return View(projectCost);
        }

        // POST: ProjectCosts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id) {

            var projectCost = await _context.ProjectCost.FindAsync(id);
            _context.ProjectCost.Remove(projectCost);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Project", new { id = projectCost.ProjectId });
        }
    }
}
