using Spectre.Console;
using Spectre.Console.Cli;

namespace CleanupBinObj.Commands.Validation;

public class ValidateStringAttribute : ParameterValidationAttribute
{
    private const int MinimumLength = 3;

    public ValidateStringAttribute() : base(errorMessage: string.Empty)
    {
    }

    public override ValidationResult Validate(CommandParameterContext context)
        => context.Value switch
        {
            string { Length: >= MinimumLength } => ValidationResult.Success(),
            string value => ValidationResult.Error(
                $"{context.Parameter.PropertyName} ({value}) needs to be at " +
                $"least {MinimumLength} characters long was {value.Length}."),
            _ => ValidationResult.Error(
                $"Invalid {context.Parameter.PropertyName} ({context.Value ?? "<null>"}) specified.")
        };
}