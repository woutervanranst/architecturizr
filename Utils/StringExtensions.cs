using Newtonsoft.Json.Linq;
using System;
using System.Text.RegularExpressions;

namespace architecturizr.Utils;

public static class StringExtensions
{
    // https://gist.github.com/wsloth/5e9f0e83bdd0c3c9341da7d83ffb8dbb

    public static string ToKebabCase(this string value)
    {
        // Replace all non-alphanumeric characters with a dash
        value = Regex.Replace(value, @"[^0-9a-zA-Z]", "-");

        // Replace all subsequent dashes with a single dash
        value = Regex.Replace(value, @"[-]{2,}", "-");

        // Remove any trailing dashes
        value = Regex.Replace(value, @"-+$", string.Empty);

        // Remove any dashes in position zero
        if (value.StartsWith("-")) value = value.Substring(1);

        // Lowercase and return
        return value.ToLower();
    }

    public static string Value(this string? s)
    {
        ArgumentNullException.ThrowIfNull(s, nameof(s));
        
        return s;
    }
}