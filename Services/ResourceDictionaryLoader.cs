using System.IO;
using System.Windows;

namespace Myria.Wpf.Services
{
    public static class ResourceDictionaryLoader
    {
        public static ResourceDictionary Load(Uri source, string description)
        {
            try
            {
                return new ResourceDictionary { Source = source };
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(
                    $"Failed to load WPF resource dictionary '{description}' from '{FormatSource(source)}'.",
                    ex);
            }
        }

        private static string FormatSource(Uri source)
        {
            if (!source.IsAbsoluteUri)
                return source.OriginalString;

            return source.IsFile
                ? source.LocalPath
                : source.AbsoluteUri;
        }
    }
}
