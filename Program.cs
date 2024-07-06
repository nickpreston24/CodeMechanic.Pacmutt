// See https://aka.ms/new-console-template for more information

using CodeMechanic.Diagnostics;
using CodeMechanic.FileSystem;
using CodeMechanic.Shargs;
using Newtonsoft.Json;

Console.WriteLine("Hello, Woof!");

var cmds = new ArgumentsCollection(args);

// (var _, var project_directories) = cmds.Matching("-p", "--projects");
// project_directories.Dump("project directories"); // Todo: allow dashes in shargs regex!!  Example: `code-mechanic` gets parsed as `code`!!

(var _, var root_dir) = cmds.Matching("-r", "--root-dir");

// string root = (root_dir.SingleOrDefault() ?? string.Empty).GoUp();
string root = root_dir.SingleOrDefault() ?? Directory.GetCurrentDirectory().GoUp();
Console.WriteLine("root :>> " + root);

bool root_exists = Path.Exists(root);
// Console.WriteLine($"root exists? {root_exists}");

var csproj_sniffer = new ProjectSniffer();
var package_json_sniffer = new NpmPackageSniffer();
    
    
var all_projects = await csproj_sniffer.DiscoverProjects(root);
Console.WriteLine("Total projects to upgrade :>> " +
                  all_projects.DistinctBy(proj => proj.grep_result.FileName).Count());

var stats = await csproj_sniffer.UpdateAllProjects(all_projects);
stats.Dump(nameof(stats));


public record UsingStatement
{
    public string imported_package { get; set; } = string.Empty;
}

public record NamespaceStatement
{
    public string detected_namespace { get; set; } = string.Empty;
}