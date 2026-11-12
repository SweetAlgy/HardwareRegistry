using System.ComponentModel.DataAnnotations;

namespace HardwareRegistry.Application.UnitTests.TestUtilities
{
    /// <summary>
    /// Runs the same data annotations check that model binding runs for API requests.
    /// </summary>
    internal static class ModelValidator
    {
        public static IReadOnlyList<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
            return results;
        }
    }
}
