// /Services/PasswordRuleService.cs
using System.Globalization;
using AppraisalPortal.Models;

namespace AppraisalPortal
{
    /// Password = FirstInitialCapital + ddMM + yyyy from DOB; fallback DOJ; else 01-01-1990
    public class PasswordRuleService
    {
        public string Generate(Employee e) => Generate(e.Name, e.EmpCode, e.DateOfBirth, e.DateOfJoining);

        public string Generate(string? name, string? empCode, DateTime? dob, DateTime? doj)
        {
            char first = !string.IsNullOrWhiteSpace(name) ? char.ToUpperInvariant(name.Trim()[0])
                      : !string.IsNullOrWhiteSpace(empCode) ? char.ToUpperInvariant(empCode.Trim()[0])
                      : 'A';

            var basis = dob ?? doj ?? new DateTime(1990, 1, 1);
            return $"{first}{basis:dd}{basis:MM}{basis:yyyy}";
        }
    }
}
