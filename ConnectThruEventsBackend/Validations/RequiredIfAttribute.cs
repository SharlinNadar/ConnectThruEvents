using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ConnectThruEventsBackend.Validations
{
    public class RequiredIfAttribute : ValidationAttribute
    {
        private readonly string _conditionProperty;
        private readonly object _expectedValue;

        public RequiredIfAttribute(string conditionProperty, object expectedValue)
        {
            _conditionProperty = conditionProperty;
            _expectedValue = expectedValue;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var conditionProperty = validationContext.ObjectType.GetProperty(_conditionProperty);
            if (conditionProperty == null)
                return new ValidationResult($"Unknown property: {_conditionProperty}");

            var conditionValue = conditionProperty.GetValue(validationContext.ObjectInstance);

            if (conditionValue != null && conditionValue.Equals(_expectedValue) && value == null)
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}
