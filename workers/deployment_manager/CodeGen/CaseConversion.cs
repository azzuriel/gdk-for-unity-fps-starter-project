using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeGen
{
    internal static class CaseConversion
    {
        public static string CapitalizeFirstLetter(string text)
        {
            return char.ToUpperInvariant(text[0]) + text.Substring(1, text.Length - 1);
        }

        public static string ToPascalCase(IEnumerable<string> parts)
        {
            var result = string.Empty;
            foreach (var s in parts)
            {
                result = result + CapitalizeFirstLetter(s);
            }

            return result;
        }

        public static string SnakeCaseToPascalCase(string text)
        {
            return ToPascalCase(text.Split(new[] {"_"}, StringSplitOptions.RemoveEmptyEntries));
        }

        public static string SnakeCaseToCamelCase(string text)
        {
            var parts = text.Split(new[] {"_"}, StringSplitOptions.RemoveEmptyEntries);
            return parts[0] + ToPascalCase(parts.Skip(1));
        }

        public static string CapitalizeNamespace(string text)
        {
            var strings = text.Split(new[] {"."}, StringSplitOptions.RemoveEmptyEntries);
            return SnakeCaseToCamelCase
                (string.Join('.', strings
                .Select(CapitalizeFirstLetter)));
        }

        public static string GetNamespaceFromTypeName(string text)
        {
            var strings = text.Split(new[] {"."}, StringSplitOptions.RemoveEmptyEntries);
            return SnakeCaseToCamelCase(
                string.Join('.', strings
                .Take(strings.Length - 1)
                .Select(CapitalizeFirstLetter)));
        }
    }
}
