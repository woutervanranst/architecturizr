using System;
namespace architecturizr.InputParsers
{
    internal interface IINputParser<T>
    {
        T Parse(FileInfo f);
    }
}

