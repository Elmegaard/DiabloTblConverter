// See https://aka.ms/new-console-template for more information

using Newtonsoft.Json;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;

if (Environment.GetCommandLineArgs().Length != 4)
{
    throw new Exception("Did not find exactly 3 Arguments: <output type: [tbl|json]> <path/to/input.tbl> <path/to/output.json>");
}

string type = Environment.GetCommandLineArgs()[1];
string from = Environment.GetCommandLineArgs()[2];
string to = Environment.GetCommandLineArgs()[3];

if (type != "tbl" && type != "json")
{
    throw new Exception("First argument has to be tbl or json to refer to the output type");
}

if (!File.Exists(from))
{
    throw new Exception("Second argument has to be a valid path");
}

if (type == "json")
{
    if (!from.ToLower().EndsWith(".tbl"))
    {
        throw new Exception("Second argument has to be a .tbl file");
    }

    if (!to.ToLower().EndsWith(".json"))
    {
        throw new Exception("Third argument has to be a .json file");
    }
}
else if (type == "tbl")
{
    if (!from.ToLower().EndsWith(".json"))
    {
        throw new Exception("Second argument has to be a .json file");
    }

    if (!to.ToLower().EndsWith(".tbl"))
    {
        throw new Exception("Third argument has to be a .tbl file");
    }
}


if (File.Exists(to))
{
    File.Delete(to);
}

if (type == "json")
{
    var table = TableProcessor.ReadTablesFile(from);
    var json = JsonConvert.SerializeObject(table);
    File.WriteAllText(to, json);
}
else if (type == "tbl")
{
    var json = File.ReadAllText(from);
    TableProcessor.WriteTablesFile(to, json);
}
