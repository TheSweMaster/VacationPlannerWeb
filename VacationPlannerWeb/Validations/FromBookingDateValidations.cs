using VacationPlannerWeb.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace VacationPlannerWeb.Validations
{
    public class FromBookingDateValidations : ValidationAttribute
    {
        private readonly int pastMonths;
        private readonly int futureMonths;

        public FromBookingDateValidations(int pastBoookingLimitMonths, int furuteBookingLimitMonths)
        {
            pastMonths = pastBoookingLimitMonths;
            futureMonths = furuteBookingLimitMonths;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            VacationBooking vac = (VacationBooking)validationContext.ObjectInstance;

            if (vac.FromDate < DateTime.Today.AddMonths(- pastMonths) || vac.FromDate > DateTime.Today.AddMonths(futureMonths))
            {
                return new ValidationResult($"You can only book vacation {pastMonths} months in the past and {futureMonths} months in the future.");
            }
            if (vac.FromDate > vac.ToDate)
            {
                return new ValidationResult($"Your FromDate must be smaller than your ToDate.");
            }

            return ValidationResult.Success;
        }
    }
}
