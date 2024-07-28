using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Reflection;
using System.Linq;

namespace VirtualDesktopManager.Permissions
{
    /// <summary>
    /// Help with getting data from an embedded resource.
    /// </summary>
    public static class EmbeddedResourceCode
    {
        /// <summary>
        /// Get a stream for an embedded resource.
        /// 
        /// Source:
        /// https://stackoverflow.com/questions/3314140/how-to-read-embedded-resource-text-file
        /// </summary>
        /// <param name="resourceName">The name of the embedded resource. For example: "MyCompany.MyProduct.MyFile.txt" or when using a relative path: "MyFile.txt".</param>
        /// <param name="relativeName">Only check so that the resource's name ends with the correct name. Ignore namespaces.</param>
        /// <param name="assembly">The assembly where the embeded file is located. Defaults to the calling methods assembly.</param>
        /// <returns>A stream to the embedded resource.</returns>
        public static Stream GetStreamForResource(string resourceName, bool relativeName = true, Assembly assembly = null)
        {
            if (assembly == null)
                assembly = Assembly.GetCallingAssembly();

            if (relativeName)
            {
                resourceName = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith(resourceName));
            }

            return assembly.GetManifestResourceStream(resourceName);
        }

        /// <summary>
        /// Get a string from an embedded text file.
        /// 
        /// Source:
        /// https://stackoverflow.com/questions/3314140/how-to-read-embedded-resource-text-file
        /// </summary>
        /// <param name="resourceName">The name of the embedded resource. For example: "MyCompany.MyProduct.MyFile.txt" or when using a relative path: "MyFile.txt".</param>
        /// <param name="relativeName">Only check so that the resource's name ends with the correct name. Ignore namespaces.</param>
        /// <param name="assembly">The assembly where the embeded file is located. Defaults to the calling methods assembly.</param>
        /// <returns>The text in the embedded text file.</returns>
        public static string GetStringFromResource(string resourceName, bool relativeName = true, Assembly assembly = null)
        {
            if (assembly == null)
                assembly = Assembly.GetCallingAssembly();

            using (Stream stream = GetStreamForResource(resourceName, relativeName, assembly))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
