using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace architecturizr.Utils;

internal static class KeyNotFoundExceptionExtensions
{
    public static string GetKeyValue(this KeyNotFoundException ex)
    {
        var match = Regex.Match(ex.Message, @"'(.*?)'");
        if (match.Success)
            return match.Groups[1].Value;

        throw ex;
    }
}