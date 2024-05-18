// See https://aka.ms/new-console-template for more information

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using CodeMechanic.Advanced.Regex;
using CodeMechanic.Diagnostics;
using CodeMechanic.FileSystem;
using CodeMechanic.Shargs;
using Newtonsoft.Json;

Console.WriteLine("Hello, Woof!");

var cmds = new ArgumentsCollection(args);

(var _, var project_directories) = cmds.Matching("-p", "--projects");
project_directories.Dump("project directories");
(var _, var root_dir) = cmds.Matching("-r", "--root-dir");


string root = (root_dir.SingleOrDefault() ?? string.Empty).GoUp();
root.Dump("root");

bool root_exists = Path.Exists(root);
Console.WriteLine($"root exists? {root_exists}");


new ProjectFinder().DiscoverProjects(root);

public class ProjectFinder
{
    private readonly Dictionary<string, string> packages_I_wanna_touch = new();
    private readonly string[] folders_I_wanna_ignore;

    public ProjectFinder()
    {
        packages_I_wanna_touch = new Dictionary<string, string>()
        {
            ["code-mechanic"] = "CodeMechanic.Diagnostics"
        };
        folders_I_wanna_ignore = new string[]
        {
            "wwwroot", "node_modules", "obj/", "bin/", ".idea", ".git"
        };
    }

    public async Task<bool> DiscoverProjects(string root)
    {
        var dirs_only = packages_I_wanna_touch.Select(x => x.Key);
        var all_dirs_pattern = "(" + string.Join("|", dirs_only) + ")";
        var all_exclusions_pattern = $@"\/?\b(?!{string.Join("|", folders_I_wanna_ignore)})\b";
        string full_pattern = all_exclusions_pattern + all_dirs_pattern;
        // Console.WriteLine("fullpattern:>>" + full_pattern);
        
        // sample: https://regex101.com/r/kdW20j/1
        var all_dirs_regex = new Regex(full_pattern,
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // return true;
        ConcurrentDictionary<string, List<Grepper.GrepResult>> project_dirs =
            new ConcurrentDictionary<string, List<Grepper.GrepResult>>();

        var iterator = root
            .AsDirectory()
            .DiscoverDirectories(
                all_dirs_regex
                , debug: true);

        await foreach (var dir in iterator)
        {
            var files_found = new Grepper()
                {
                    RootPath = dir,
                    FileSearchMask = "*.csproj",
                    FileSearchLinePattern = @"\<PackageReference"
                }
                .GetMatchingFiles()
                .DistinctBy(gr => gr.FilePath)
                .ToList();

            Console.WriteLine($"{files_found.Count} total .csproj files found");

            // files_found.Select(gr => gr.FileName).Dump("projects");

            if (files_found.Count > 0)
                project_dirs.TryAdd(dir, files_found);
        }

        var results = project_dirs.ToDictionary();
        Console.WriteLine("total dirs red:>>" + results.Count);
        // string json = JsonConvert.SerializeObject(results);
        // File.WriteAllText("results.json", json);

        // group the results by filepath and filename to prevent dups and make the updates easier.

        var project_info = results
            .SelectMany(kvp => kvp.Value
                .Select(grp =>
                {
                    return new ProjectInfo
                    {
                        directory_path = kvp.Key,
                        grep_result = grp,
                        version_info = grp.Line
                            .Extract<VersionInfo>(
                                // https://regex101.com/r/yufkKD/1
                                @"Include=""(?<package_name>[\w\d.]+?)""\s*Version=""(?<version_number>(\d+\.\d+\.\d+|\w+))""")
                            .SingleOrDefault() ?? new VersionInfo()
                    };
                }));

        var grouped_projects = project_info
                .OrderBy(pi => pi.grep_result.FileName)
                .ThenByDescending(pi => pi.grep_result.LineNumber)
                .ThenBy(pi => pi.directory_path)
                .GroupBy(pi => pi.grep_result.FileName)
            ;

        string grouped_projects_json = JsonConvert.SerializeObject(grouped_projects);
        File.WriteAllText("grouped.json", grouped_projects_json);

        return true;
    }
}

public record ProjectInfo
{
    public string[] raw_using_statements { get; set; } = Array.Empty<string>();
    public string directory_path { get; set; }
    public Grepper.GrepResult grep_result { get; set; } = new();
    public VersionInfo version_info { get; set; } = new();
}

public record VersionInfo
{
    public string package_name { get; set; } = string.Empty;
    public string version_number { get; set; } = string.Empty;
}

public record UsingStatement
{
    public string imported_package { get; set; } = string.Empty;
}

public record NamespaceStatement
{
    public string detected_namespace { get; set; } = string.Empty;
}