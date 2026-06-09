using System.Globalization;
using System.Text;

namespace EngravingStation.Core.Services;

public sealed class OrderCodeNormalizer
{
    public string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var compatibility = input.Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(compatibility.Length);
        foreach (var character in compatibility)
        {
            if (!char.IsControl(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Trim().ToUpper(CultureInfo.InvariantCulture);
    }
}
