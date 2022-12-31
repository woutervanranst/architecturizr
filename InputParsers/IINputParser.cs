using System;
namespace architecturizr.InputParsers
{
    internal interface IINputParser<T>
    {
        IEnumerable<T> Parse(FileInfo f);
    }
}

