// See https://aka.ms/new-console-template for more information

using CodeMechanic.Diagnostics;

Console.WriteLine("Hello, Woof!");

var package_json_sniffer = new NpmPackageSniffer(args);

var output = await package_json_sniffer.FindOutDatedNPMPackages(true);
await package_json_sniffer.UpdateNpmPackages(true);
var updated_file_success = await package_json_sniffer.ApplyUpdates(true);


#region fixme

bool test_csproject_sniffer = false;

if (test_csproject_sniffer)
{
// Todo: make this a sample, for now.  then upgrade to actually call myget for updates...
    var csproj_sniffer = new ProjectSniffer(args);
    var all_projects = await csproj_sniffer.DiscoverProjects();
    Console.WriteLine("Total projects to upgrade :>> " +
                      all_projects.DistinctBy(proj => proj.grep_result.FileName).Count());

    var stats = await csproj_sniffer.UpdateAllProjects(all_projects);
    stats.Dump(nameof(stats));
}

#endregion fixme