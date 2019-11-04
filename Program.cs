using NugetUtility;

namespace nuget_license_reader
{
    class Program
    {
        static void Main(string[] args)
        {
            
            var a1 = NugetUtils.WriteNugetLicensesToDisk(@"/home/tobii.intra/bcg/GIT/flora-tools/FloraVisualizer/FloraVisualizer.csproj",
            @"/home/tobii.intra/bcg/GIT/flora-tools/FloraVisualizer/");
            a1.Wait();

        }
    }
}
