using System;
using ExcelToEnumerable;

namespace architecturizr;

/// <summary>
/// Read the Excel Source file
/// https://stackoverflow.com/a/15793495
/// </summary>
public class SourceFile
{

    public SourceFile(string fileName)
    {
        var exceptionList = new List<Exception>();

        // Parse General Tab
        var generalRows = fileName.ExcelToEnumerable<General>(
            x => x
                .UsingSheet("General")
                .UsingHeaderNames(false) //map using column numbers, not names
                .OutputExceptionsTo(exceptionList)
            );

        if (exceptionList.Any())
            throw new Exception();

        Title = generalRows.Single(r => r.Key == "Title").Value;


        // Parse Nodes Tab
        var nodes = fileName.ExcelToEnumerable<Node>(
            x => x
                .UsingSheet("Nodes")
                .OutputExceptionsTo(exceptionList)
                .UsingHeaderNames(false) //map using column numbers, not names
                .StartingFromRow(3) //data as of row 3
            );
    }



    

    public string Title { get; init; }

    private class General
    {
        public string Key { get; init; }
        public string Value { get; init; }
    }

    private class Node
    {
        public string Person { get; init; }

        public string SoftwareSystem_Key { get; init; }
        public string SoftwareSystem_Description { get; init; }

        public string Container_Key { get; init; }
        public string Container_Description { get; init; }
        public string Component { get; init; }
        public string Tier { get; init; }
        public string Technology { get; init; }
        public string Owner { get; init; }
        public string Deprecated { get; init; }
    }


}



