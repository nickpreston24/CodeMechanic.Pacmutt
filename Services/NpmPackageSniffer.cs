using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeMechanic.Diagnostics;
using CodeMechanic.RegularExpressions;
using CodeMechanic.Shargs;
using CodeMechanic.Types;

public class NpmPackageSniffer
{
    private string find_outdated_npm_packages_cmd = "npm outdated --depth=0";
    private string rootfolder = string.Empty;
    private string output = string.Empty;
    private List<NpmUpdate> package_updates = new();

    public NpmPackageSniffer(string[] args)
    {
        args.Dump(nameof(args));
        // var options = new ArgumentsCollection(args);
        // options.Dump(nameof(options));
        // options.Arguments.Dump("arguments");

        // Console.WriteLine(options.Length);
        rootfolder = args.Length == 2
            ? args[1]
            : "/home/nick/Desktop/projects/samples/hot-reloading-docker/nodejs-with-mongodb-api-example";
        Console.WriteLine($"root folder set to '{rootfolder}'");
    }

    public async Task<NpmPackageSniffer> FindOutDatedNPMPackages(
        bool debug = false
    )
    {
        if (rootfolder.IsEmpty()) throw new ArgumentNullException(nameof(rootfolder));
        if (!rootfolder.IsDirectory())
            throw new ArgumentException($"{nameof(rootfolder)} '{rootfolder}' not a valid directory!");

        if (debug) await RunLs();

        // change dir if package.json exists:
        string packagejson_filepath = Path.Combine(rootfolder, "package.json");
        if (File.Exists(packagejson_filepath))
        {
            Console.WriteLine("setting current directory to npm package");
            Directory.SetCurrentDirectory(rootfolder);
            if (debug) Console.WriteLine("cwd: \n" + Directory.GetCurrentDirectory());
        }

        Console.WriteLine("package.json path: \n" + packagejson_filepath);

        output = await find_outdated_npm_packages_cmd.Bash(verbose: debug);

        return this;
    }

    private async Task RunLs()
    {
        // list files
        Console.WriteLine("looking in :> " + rootfolder);
        string ls_result = await $"ls {rootfolder}".Bash();
        Console.WriteLine(ls_result);
    }

    public async Task<NpmPackageSniffer> UpdateNpmPackages( //string output = "", 
        bool debug = true
    )
    {
        var lines = output.Split("\n");
        int total_output_rows = lines.Length - 1;

        if (debug) Console.WriteLine(" rows :>> " + total_output_rows);
        if (debug) Console.WriteLine($" # of lines : {lines.Length}");

        package_updates = lines
            .SelectMany(line => line
                .Extract<NpmUpdate>(NpmRegexPattern.Bump.CompiledRegex))
            .ToList();

        if (debug)
            package_updates.Dump(nameof(package_updates));

        int total_updates = package_updates.Count;

        Console.WriteLine($"total updates : {total_updates}");

        if (debug)
            Console.WriteLine($"{total_updates} of {total_output_rows} npm regex patterns extracted: ");

        return this;
    }

    public async Task<bool> ApplyUpdates(
        // List<NpmUpdate> npmUpdates, string rootfolder,
        bool debug = false
    )
    {
        try
        {
            if (rootfolder.IsEmpty()) throw new ArgumentNullException(nameof(rootfolder));
            if (!rootfolder.IsDirectory()) throw new ArgumentException($"{nameof(rootfolder)} not a valid directory!");

            string packagejson_filepath = Path.Combine(rootfolder, "package.json");
            if (File.Exists(packagejson_filepath))
            {
                if (debug)
                    Console.WriteLine("setting current directory to npm package");

                Directory.SetCurrentDirectory(rootfolder);
                if (debug) Console.WriteLine("cwd: \n" + Directory.GetCurrentDirectory());
            }

            if (debug)
                Console.WriteLine("package.json path: \n" + packagejson_filepath);

            // ("(?<Package>\w+)"\:\s*"(?<Latest>\^?\d+\.\d+\.\d+(-\w+\d*)*)")
            // .ToDictionary(kvp => kvp.Package, kvp => kvp.Latest)
            var replacement_map = package_updates
                    .ToDictionary(
                        kvp =>
                            $"(\"(?<Package>{kvp.Package})\"\\:\\s*\"(?<Latest>\\^?\\d+\\.\\d+\\.\\d+(-\\w+\\d*)*)\")",
                        kvp => $"'{kvp.Package}':'{kvp.Latest}'") // "concurrently": "8.2.2",
                ;

            foreach (var kvp in replacement_map)
            {
                Console.WriteLine(kvp.Key);
            }
            // replacement_map.Dump(nameof(replacement_map));
            // replacement_map = replacement_map.TryAdd("'", "\"");

            string[] package_json_lines = File.ReadAllLines(packagejson_filepath);
            string[] updated_lines = package_json_lines
                .ReplaceAll(replacement_map)
                .Select(line =>
                    {
                        line = line.Replace("'", "\"");
                        return line;
                    }
                )
                .ToArray();

            string filename = Path.GetFileNameWithoutExtension(packagejson_filepath);
            // File.WriteAllLines($"{filename}.sample.json", updated_lines);
            string udpated_fp = $"{filename}.updated.json";
            string old_fp = $"{filename}.old.json";

            File.WriteAllLines(udpated_fp, updated_lines, Encoding.UTF8);
            File.WriteAllLines(old_fp, package_json_lines);

            File.Replace(udpated_fp, packagejson_filepath, old_fp);

            Console.WriteLine("running npm here: " + Directory.GetCurrentDirectory());

            await "npm install ".Bash(verbose: true);

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
}