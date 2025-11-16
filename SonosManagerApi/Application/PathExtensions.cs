using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Web;

namespace SonosManagerApi.Application
{
    public static class PathExtensions
    {
        public static string ToHost(this string path)
        {
            if (path.StartsWith("\\\\"))
            {
                return path.Split('\\', StringSplitOptions.RemoveEmptyEntries)[0];
            }

            var pathSegments = path.Split(':', StringSplitOptions.RemoveEmptyEntries);

            var basePath = pathSegments[1];

            basePath = basePath.Split('/', StringSplitOptions.RemoveEmptyEntries)[0];

            return basePath;
        }

        public static string ParentFolder(this string path)
        {
            var pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            var encoded = "unknown";

            if (pathParts.Length > 1)
            {
                encoded = pathParts[pathParts.Length - 2];
            }
            else
            {
                pathParts = path.Split(':', StringSplitOptions.RemoveEmptyEntries);
                encoded = pathParts[0];
            }

            return HttpUtility.UrlDecode(encoded);
        }

        public static bool IsFile (this string path) => path.StartsWith("x-file-cifs://", StringComparison.OrdinalIgnoreCase);

        public static string ToUNCPath(this string path)
        {
            // Ensure the URL starts with the expected prefix
            if (path.IsFile())
            {
                // Remove the x-file-cifs:// prefix
                path = path.Replace("x-file-cifs://", @"\\");

                // Replace forward slashes with backslashes
                path = path.Replace("/", "\\");
            }

            return HttpUtility.UrlDecode(path);
        }

    }
}
