// See https://aka.ms/new-console-template for more information

using CodeMechanic.Diagnostics;
using CodeMechanic.FileSystem;
using CodeMechanic.Shargs;

Console.WriteLine("Hello, Woof!");


var cmds = new ArgumentsCollection(args);

(var _, var project_directories) = cmds.Matching("-p", "--projects");
project_directories.Dump("project directories");
(var _, var root_dir) = cmds.Matching("-r", "--root-dir");

string root = root_dir.SingleOrDefault() ?? string.Empty;
root.Dump("root");

bool root_exists = Path.Exists(root);
Console.WriteLine($"root exists? {root_exists}");

var files_found = new Grepper()
    {
        RootPath = root,
        FileSearchMask = "*.csproj",
        FileSearchLinePattern = @"\<PackageReference"
    }
    .GetMatchingFiles()
    .DistinctBy(gr => gr.FilePath)
    .ToList();

Console.WriteLine($"{files_found.Count} total .csproj files found");

files_found.Select(gr => gr.FileName).Dump("projects");

// TODO: find all out of date packages...