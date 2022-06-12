// See https://aka.ms/new-console-template for more information

using Newtonsoft.Json;

if (Environment.GetCommandLineArgs().Length != 3)
{
    throw new Exception("Did not find exactly 2 Arguments: <path/to/input.tbl> <path/to/output.json>");
}

string from = Environment.GetCommandLineArgs()[1];
string to = Environment.GetCommandLineArgs()[2];

if (!from.ToLower().EndsWith(".tbl"))
{
    throw new Exception("First argument has to be a .tbl file");
}

if (!File.Exists(from))
{
    throw new Exception("First argument has to be a valid path");
}

if (!to.ToLower().EndsWith(".json"))
{
    throw new Exception("Second argument has to be a .json file");
}

if (File.Exists(to))
{
    File.Delete(to);
}

var table = TableProcessor.ReadTablesFile(from);
var json = JsonConvert.SerializeObject(table);
File.WriteAllText(to, json);
