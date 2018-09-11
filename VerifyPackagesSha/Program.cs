using NuGet.Protocol;
using NuGet.Common;
using System;
using System.IO;
using System.Text;

namespace VerifyPackagesSha
{
    class Program
    {
        static void Main(string[] args)
        {
            var dotnetFallback = @"C:\Program Files\dotnet\sdk\NuGetFallbackFolder";
            var offlineFolder = @"C:\Program Files (x86)\Microsoft SDKs\NuGetPackages";

            VerifyPackagesIntegrity(offlineFolder, @"c:\temp\offlineFolder.csv");
            VerifyPackagesIntegrity(dotnetFallback, @"c:\temp\fallbackFolder.csv");

            Console.ReadLine();
        }


        private static void VerifyPackagesIntegrity(string folderRoot, string fileName)
        {
            var sb = new StringBuilder();

            if (!Directory.Exists(folderRoot))
            {
                Console.WriteLine($"Could not find folder: {folderRoot}");
                return;
            }

            var packageInfos = LocalFolderUtility.GetPackagesV3(folderRoot, NullLogger.Instance);
            var numIncorrectHash = 0;

            foreach (var packageInfo in packageInfos)
            {
                if (File.Exists(packageInfo.Path))
                {
                    var packageHash = string.Empty;
                    using (var stream = new FileStream(
                       packageInfo.Path,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.Read,
                       bufferSize: 4096,
                       useAsync: true))
                    {
                        var bytes = new CryptoHashProvider("SHA512").CalculateHash(stream);
                        packageHash = Convert.ToBase64String(bytes);
                    }

                    var sha512File = packageInfo.Path + ".sha512";
                    if (File.Exists(sha512File))
                    {
                        var existingHash = File.ReadAllText(sha512File);

                        var flag = string.Equals(existingHash, packageHash);
                        if (!flag)
                        {
                            numIncorrectHash++;
                        }

                        sb.AppendLine(string.Join(',',
                            packageInfo.Identity.Id,
                            packageInfo.Identity.Version.ToNormalizedString(),
                            existingHash,
                            packageHash,
                            flag));
                    }
                }
            }

            Console.WriteLine($"Total number of incorrect hashed packages in {folderRoot}: {numIncorrectHash}");

            File.WriteAllText(fileName, sb.ToString());
        }

    }
}
