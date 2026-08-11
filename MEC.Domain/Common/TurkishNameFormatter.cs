using System.Globalization;
using System.Text;

namespace MEC.Domain.Common
{
    public static class TurkishNameFormatter
    {
        private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

        public static string Format(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalizedValue = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            var lowerCaseValue = normalizedValue.ToLower(TurkishCulture);
            var result = new StringBuilder(lowerCaseValue.Length);
            var capitalizeNextLetter = true;

            foreach (var character in lowerCaseValue)
            {
                if (capitalizeNextLetter && char.IsLetter(character))
                {
                    result.Append(char.ToUpper(character, TurkishCulture));
                    capitalizeNextLetter = false;
                    continue;
                }

                result.Append(character);
                capitalizeNextLetter = character is ' ' or '-' or '\'' or '’';
            }

            return result.ToString();
        }
    }
}
