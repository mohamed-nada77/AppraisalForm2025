using Microsoft.EntityFrameworkCore;

namespace AppraisalPortal
{
    public class ScoringService
    {
        private readonly ApplicationDbContext _db;
        public ScoringService(ApplicationDbContext db) { _db = db; }

        /// <summary>
        /// New model: KPI percent = average of 0–100 scores.
        /// Soft percent = average of 1–10 scores, scaled to 0–100.
        /// Final = KPI%*0.70 + Soft%*0.30.
        /// Fallback: legacy Responses weighting if no new rows exist.
        /// </summary>
        public async Task ComputeAsync(int formId)
        {
            var form = await _db.Forms
                .Include(f => f.Responses).ThenInclude(r => r.Question)
                .FirstAsync(f => f.Id == formId);

            var kpiScores = await _db.KPIItems
                .Where(k => k.FormId == formId)
                .Select(k => k.Score)
                .ToListAsync();

            var softScores = await _db.SoftSkillRatings
                .Where(s => s.FormId == formId)
                .Select(s => s.Score)
                .ToListAsync();

            if (kpiScores.Count > 0 || softScores.Count > 0)
            {
                // KPI % = average of 0–100 (as decimal)
                decimal kpiAvg = kpiScores.Count > 0
                    ? kpiScores.Select(s => (decimal)Math.Clamp(s, 0, 100)).Average()
                    : 0m;
                int kpiPercent = (int)Math.Round(kpiAvg, MidpointRounding.AwayFromZero);

                // Soft % = average(1–10) scaled to 0–100 (as decimal)
                decimal softAvg10 = softScores.Count > 0
                    ? softScores.Select(s => (decimal)Math.Clamp(s, 1, 10)).Average()
                    : 0m;
                int softPercent = (int)Math.Round((softAvg10 / 10m) * 100m, MidpointRounding.AwayFromZero);

                // Weights (keep decimals)
                decimal kpiWeighted = Math.Round(kpiPercent * 0.70m, 2);
                decimal softWeighted = Math.Round(softPercent * 0.30m, 2);
                decimal final = Math.Round(kpiWeighted + softWeighted, 2);


                form.ManagerScore = final;  // manager composite
                form.FinalScore = final;  // keep identical unless HR/CEO adjust later
                await _db.SaveChangesAsync();
                return;
            }

            // Fallback legacy model (Responses)
            decimal totalW = form.Responses.Sum(r => r.Question.Weight);
            if (totalW == 0) totalW = 1;

            decimal emp = form.Responses
                .Where(r => r.SelfRating.HasValue)
                .Sum(r => r.SelfRating!.Value * r.Question.Weight) / totalW;

            decimal mgr = form.Responses
                .Where(r => r.ManagerRating.HasValue)
                .Sum(r => r.ManagerRating!.Value * r.Question.Weight) / totalW;

            form.EmployeeScore = Math.Round(emp, 2);
            form.ManagerScore = Math.Round(mgr, 2);
            form.FinalScore = Math.Round((emp + mgr) / 2m, 2);

            await _db.SaveChangesAsync();
        }
    }
}
