using CodeMechanic.Types;

public class NpmPackageSniffer
{
    private string find_outdated_npm_packages_cmd = " npm outdated --depth=0";

    public async Task<string> FindOutDatedNPMPackages(string rootfolder = "")
    {
        if (rootfolder.IsEmpty()) throw new ArgumentNullException(nameof(rootfolder));
        if (!rootfolder.IsDirectory()) throw new ArgumentException($"{nameof(rootfolder)} not a valid directory!");

        string result = await find_outdated_npm_packages_cmd.Bash();
        Console.WriteLine(result);
        return result;
    }
    
}