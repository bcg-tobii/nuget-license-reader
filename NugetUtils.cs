using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using NuGet.Configuration;
using nuget_license_reader;

namespace NugetUtility
{
    public class NugetUtils
    {

        private const string _nugetUrl = "https://api.nuget.org/v3-flatcontainer/";
        static string _nugetLocalPath = SettingsUtility.GetGlobalPackagesFolder(Settings.LoadDefaultSettings(null));
        public static async Task<bool> WriteNugetLicensesToDisk(string csProjFile, string rootPath)
        {
            if(File.Exists(csProjFile))
            {
                var projectRef = GetProjectReferences(csProjFile);
                await GetNugetInformationAsync(projectRef, rootPath);
                return true;
            }

            return false;
        }

        private static IEnumerable<string> GetProjectReferences(string projectPath)
        {
            IEnumerable<string> references = new List<string>();
            XDocument projDefinition = XDocument.Load(projectPath);
            try
            {
                references = projDefinition
                             .Element("Project")
                             .Elements("ItemGroup")
                             .Elements("PackageReference")
                             .Select(refElem => (refElem.Attribute("Include") == null ? "" : refElem.Attribute("Include").Value) + "," +
                                                (refElem.Attribute("Version") == null ? "" : refElem.Attribute("Version").Value));
            }
            catch (System.Exception ex)
            {
                throw ex;
            }

            return references;
        }


        public static async Task<bool> GetNugetInformationAsync(IEnumerable<string> references, string rootPath)
        {
            Dictionary<string, Package> licenses = new Dictionary<string, Package>();
            foreach (var reference in references)
            {
                string referenceName = reference.Split(',')[0];
                string versionNumber = reference.Split(',')[1];
                using (var httpClient = new HttpClient {Timeout = TimeSpan.FromSeconds(10)})
                {
                    string requestUrl = _nugetUrl + referenceName + "/" + versionNumber + "/" + referenceName + ".nuspec";
                    //Console.WriteLine(requestUrl);
                    try
                    {
                        HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                        HttpResponseMessage response = null;
                        response = await httpClient.SendAsync(req);
                        string responseText = await response.Content.ReadAsStringAsync();
                        XmlSerializer serializer = new XmlSerializer(typeof(Package));
                        using (TextReader writer = new StringReader(responseText))
                        {
                            try
                            {
                                Console.WriteLine($"Trying to fetch a license for {referenceName}");

                                Package result = (Package) serializer.Deserialize(new NamespaceIgnorantXmlTextReader(writer));
                                if(licenses.ContainsKey(reference))
                                {
                                    continue;
                                }
                                licenses.Add(reference, result);
                                string dir = $"{rootPath}{Path.DirectorySeparatorChar}licenses{Path.DirectorySeparatorChar}{result.Metadata.Id}{Path.DirectorySeparatorChar}";
                                if (Directory.Exists(rootPath))
                                {
                                    Directory.CreateDirectory(dir);
                                }
                                if (result.Metadata.License != null && !String.IsNullOrEmpty(result.Metadata.License.Type) && result.Metadata.License.Type == "file")
                                {
                                    string licensePath = $"{_nugetLocalPath}{Path.DirectorySeparatorChar}"+
                                    $"{result.Metadata.Id.ToLower()}{Path.DirectorySeparatorChar}"+
                                    $"{result.Metadata.Version.ToLower()}{Path.DirectorySeparatorChar}"+
                                    $"{result.Metadata.License.Text}";
                                    File.Copy(licensePath,($"{dir}{Path.DirectorySeparatorChar}license.txt"),true);
                                }
                                else if(!String.IsNullOrEmpty(result.Metadata.LicenseUrl) && result.Metadata.LicenseUrl != @"https://aka.ms/deprecateLicenseUrl")
                                {
                                    req = new HttpRequestMessage(HttpMethod.Get, result.Metadata.LicenseUrl);
                                    response = await httpClient.SendAsync(req);
                                    responseText = await response.Content.ReadAsStringAsync();
                                    File.WriteAllText($"{dir}{Path.DirectorySeparatorChar}license.txt",responseText);
                                }
                                else if(!String.IsNullOrEmpty(result.Metadata.ProjectUrl))
                                {
                                    string projPath = result.Metadata.ProjectUrl;
                                    string rawPath = projPath.Replace(@"https://github.com/",@"https://raw.githubusercontent.com/");
                                    string licPath = $"{rawPath}/master/LICENSE";
                                    req = new HttpRequestMessage(HttpMethod.Get, licPath);
                                    response = await httpClient.SendAsync(req);
                                    responseText = await response.Content.ReadAsStringAsync();
                                    File.WriteAllText($"{dir}{Path.DirectorySeparatorChar}license.txt", responseText);
                                }
                                else
                                {
                                    Console.WriteLine($"failed getting a license for {result.Metadata.Id}");
                                    File.WriteAllText($"{dir}{Path.DirectorySeparatorChar}unknownlicense.txt", "Unknown");
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                throw;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            return true;
        }

    }
}