using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace architecturizr.Utils;

internal static class ExceptionExtensions
{
    public static string GetFullMessage(this Exception ex)
    {
        if (ex is AggregateException)
            return ex.ToString();

        // https://stackoverflow.com/a/35084416/1582323
        return ex.InnerException == null
            ? ex.Message
            : ex.Message + " --> " + ex.InnerException.GetFullMessage();
    }
}