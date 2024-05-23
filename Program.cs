// See https://aka.ms/new-console-template for more information

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using CodeMechanic.Advanced.Regex;
using CodeMechanic.Diagnostics;
using CodeMechanic.FileSystem;
using CodeMechanic.Shargs;
using CodeMechanic.Types;
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

var all_projects = new ProjectSniffer().DiscoverProjects(root);

public class ProjectSniffer
{
    private readonly Dictionary<string, string> packages_I_wanna_touch = new();
    private readonly string[] folders_I_wanna_ignore;

    public ProjectSniffer()
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

    public async Task<List<ProjectInfo>> DiscoverProjects(string root)
    {
        Console.WriteLine($"{nameof(folders_I_wanna_ignore)} {folders_I_wanna_ignore.Length}");

        var dirs_only = packages_I_wanna_touch.Select(x => x.Key);
        dirs_only.Dump(nameof(dirs_only));
        var all_dirs_pattern = "(" + string.Join("|", dirs_only) + ")";
        // var all_exclusions_pattern = $@"\/?\b(?!{string.Join("|", folders_I_wanna_ignore)})\b";
        string full_pattern = all_dirs_pattern;
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
            var collisions = folders_I_wanna_ignore.Where(x => x.Contains(dir)).ToList();
            if (collisions.Count > 0)
            {
                collisions.Dump("COLLISIONS");
                Console.WriteLine("skipping blacklisted directory '" + dir + "'");
                continue;
            }

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
                                VersionRegex.Version.CompiledRegex)
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

        string versioning_pattern = @"(?<major_release>\d+)\.(?<minor_release>\d{1,3})\.(?<patch>\d{1,3})";
        var versioning_regex = new Regex(versioning_pattern,
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        var updated_version_info = project_info
            .GroupBy(project => project.version_info.package_name)
            .SelectMany(group =>
            {
                // find the latest installed version.
                var all_versions_for_package = group.Select(pi => pi.version_info).ToArray();

                string max_version = "blah";
                string min_version = "who cares";


                // string max_version = group.Max(v => v.version_info.version_number);
                // string min_version = group.Min(v => v.version_info.version_number);


                // update the version to the latest in the group.
                foreach (var package in group)
                {
                    package.version_info.lastest_version_number = max_version;
                    package.version_info.oldest_version_number = min_version;
                }

                return group;
            });

        var upgrade_plan = updated_version_info.DistinctBy(x => x.version_info.package_name);
        string upgrade_plan_json = JsonConvert.SerializeObject(upgrade_plan);

        File.WriteAllText("upgrade_plan.json", upgrade_plan_json);

        var sorted_projects = grouped_projects.Flatten().ToList();

        return sorted_projects;
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
    public string lastest_version_number { get; set; } = string.Empty;
    public string oldest_version_number { get; set; } = string.Empty;

    public int major_release { get; set; } = 1;
    public int minor_release { get; set; } = 0;
    public int patch { get; set; } = 0;
}


public class VersionRegex : Enumeration
{
    public Regex CompiledRegex { get; }

    // https://regex101.com/r/JS9kCx/1
    public static VersionRegex Version { get; set; } = new VersionRegex(1, nameof(Version),
        @"Include=""(?<package_name>[\w\d.]+?)""\s*Version=""(?<version_number>(\d+\.\d+\.\d+|\w+))""");

    public VersionRegex(int id, string name, string pattern) : base(id, name)
    {
        CompiledRegex = new Regex(pattern, RegexOptions.Compiled);
    }
}

public record UsingStatement
{
    public string imported_package { get; set; } = string.Empty;
}

public record NamespaceStatement
{
    public string detected_namespace { get; set; } = string.Empty;
}