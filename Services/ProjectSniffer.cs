using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using CodeMechanic.Async;
using CodeMechanic.Diagnostics;
using CodeMechanic.FileSystem;
using CodeMechanic.RegularExpressions;
using CodeMechanic.Types;

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
            "wwwroot", "node_modules", "obj/", "bin/", ".idea", ".git", ".vscode"
        };
    }

    public async Task<List<ProjectInfo>> DiscoverProjects(string root, bool debug = false)
    {
        Console.WriteLine($"{nameof(folders_I_wanna_ignore)} {folders_I_wanna_ignore.Length}");

        var dirs_only = Directory.GetDirectories(root).Select(dir => dir.AsDirectory()).ToList();
        // var ignorelist = HashExtensions.ToHashSet<string>(
        //     folders_I_wanna_ignore.ToList()
        //     , key => key);
        //
        // var ignoredirs = HashExtensions.ToHashSet<DirectoryInfo>(
        //     dirs_only
        //     , info => info.FullName);

        // dirs_only.Dump(nameof(dirs_only));

        // return new List<ProjectInfo>();

        var all_dirs_pattern = "(" + string.Join("|", dirs_only) + ")";
        // var all_exclusions_pattern = $@"\/?\b(?!{string.Join("|", folders_I_wanna_ignore)})\b";
        string full_pattern = all_dirs_pattern;
        // Console.WriteLine("fullpattern:>>" + full_pattern);

        // sample: https://regex101.com/r/kdW20j/1
        var all_dirs_regex = new Regex(full_pattern,
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Console.WriteLine("full directory pattern :>> " + full_pattern);
        ConcurrentDictionary<string, List<Grepper.GrepResult>> project_dirs =
            new ConcurrentDictionary<string, List<Grepper.GrepResult>>();

        var iterator = root
            .AsDirectory()
            .DiscoverDirectories(
                all_dirs_regex
                , debug: false);

        await foreach (var dir in iterator)
        {
            // Console.WriteLine("Dir :>> " + dir);
            var collisions = folders_I_wanna_ignore.Where(x => dir.Contains(x) || dir.Equals(x)).ToList();
            // Console.WriteLine("Collisions :>> " + collisions.Count);
            if (collisions.Count > 0)
            {
                // collisions.Dump("COLLISIONS");
                // Console.WriteLine("skipping blacklisted directory '" + dir + "'");
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

            // Console.WriteLine($"{files_found.Count} total .csproj files found");

            // files_found.Select(gr => gr.FileName).Dump("projects");

            if (files_found.Count > 0)
                project_dirs.TryAdd(dir, files_found);
        }

        var results = project_dirs.ToDictionary();

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
                })).ToArray();

        var grouped_projects = project_info
                .OrderBy(pi => pi.grep_result.FileName)
                .ThenByDescending(pi => pi.grep_result.LineNumber)
                .ThenBy(pi => pi.directory_path)
                .GroupBy(pi => pi.grep_result.FileName)
                .ToArray()
            ;

        // string grouped_projects_json = JsonConvert.SerializeObject(grouped_projects);
        // string grouped_projects_filepath = "grouped_projects.json";
        // if (File.Exists(grouped_projects_filepath))
        //     File.Delete(grouped_projects_filepath); // ensures freshness
        // File.WriteAllText(grouped_projects_filepath, grouped_projects_json);

        var updated_version_info = project_info
            .GroupBy(project => project.version_info.package_name)
            .SelectMany(group =>
            {
                // Console.WriteLine("group name : " + group.Key);

                var all_versions_for_package = group
                    .Select(pi => pi.version_info)
                    .ToArray();

                var all_version_numbers = all_versions_for_package
                        .Select(pkg => pkg.version_number)
                        .Except(new string[] { "Unknown" })
                    ;

                if (all_version_numbers.Count() > 0)
                {
                    Console.WriteLine($"Total versions for package {group.Key} " + all_version_numbers.Count());
                    if (debug) all_version_numbers.Dump("all version numbers");
                    // var min_version =
                    //         all_version_numbers
                    //             .SelectMany(vn => vn.Extract<VersionInfo>(VersionRegex.Version.CompiledRegex))
                    //             .OrderBy(version => version.major_release)
                    //             .ThenBy(version => version.minor_release)
                    //             .ThenBy(version => version.patch)
                    //             .FirstOrDefault()
                    //     ;
                    //
                    // min_version.Dump("min ver");
                    VersionInfo max_version =
                            (all_version_numbers
                                // .SelectMany(vn => vn.Extract<VersionInfo>(VersionRegex.Release.CompiledRegex))
                                .SelectMany(vn => vn.Extract<VersionInfo>(VersionRegex.Release.CompiledRegex))
                                // .Dump("all extracted versions!!!")
                                .OrderByDescending(version => version.major_release)
                                .ThenByDescending(version => version.minor_release)
                                .ThenByDescending(version => version.patch)
                                .FirstOrDefault() ?? new VersionInfo())
                            .With(vi => { vi.package_name = group.Key; })
                        ;

                    // max_version.Dump("Max ver.");
                    //
                    // // update the version to the latest in the group.
                    foreach (var package in group)
                    {
                        // Console.WriteLine("updating package :>> " + package.version_info.package_name);
                        package.version_info.major_release = max_version.major_release;
                        package.version_info.minor_release = max_version.minor_release;
                        package.version_info.patch = max_version.patch;
                    }
                }

                return group;
            }).ToArray();

        var upgrade_plan = updated_version_info
            .DistinctBy(x => x.version_info.package_name).ToArray();

        if (debug) Console.WriteLine("Total upgrades planned :" + upgrade_plan.Length);

        // string upgrade_plan_json = JsonConvert.SerializeObject(upgrade_plan);
        // string upgrade_plan_filepath = "upgrade_plan.json";
        // if (File.Exists(upgrade_plan_filepath))
        //     File.Delete(upgrade_plan_filepath);
        //
        // File.WriteAllText(upgrade_plan_filepath, upgrade_plan_json);
        var sorted_projects = grouped_projects.Flatten().ToList();

        return sorted_projects;
    }

    public async Task<UpgradeStats> UpdateAllProjects(List<ProjectInfo> allProjects)
    {
        var Q = new SerialQueue();

        var tasks = allProjects
            .Take(1)
            .Select(project => Q
                .Enqueue(async () => { await UpdateCsprojFile(project); }));

        await Task.WhenAll(tasks);

        return new UpgradeStats();
    }

    private async Task<string> UpdateCsprojFile(ProjectInfo project)
    {
        string next_version = project.version_info.lastest_version_number;
        string package_name = project.version_info.package_name;
        string csproj_filepath = project.grep_result.FilePath;
        
        string[] lines = File.ReadAllLines(csproj_filepath);
        if (lines.Length == 0) return string.Empty;
        // string package_pattern = @"\<PackageReference";
        var replacement_map = new Dictionary<string, string>()
        {
            [$@"<PackageReference\s*Include=""{package_name}""\s*Version=""([\w\.0-9]+)""\s\/>"]
                = $"<PackageReference Include=\"{package_name}\" Version=\"{next_version}\" />"
        };

        var updated_text = lines.ReplaceAll(replacement_map);
        updated_text.Dump();
        File.WriteAllLines(csproj_filepath, updated_text);
        Console.WriteLine("Updated cproj : " + csproj_filepath);
        return "";
    }
}