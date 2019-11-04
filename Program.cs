using NugetUtility;

namespace nuget_license_reader
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                System.Console.WriteLine("Usage: <pathTo .csproj> <output path>");
                return;
            }
            string csProjFile = args[0];
            string outputDir = args[1];
            var a1 = NugetUtils.WriteNugetLicensesToDisk(csProjFile, outputDir);
            a1.Wait();
        }
    }
}
