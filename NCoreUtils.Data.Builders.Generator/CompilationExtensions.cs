using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NCoreUtils.Data;

internal static class CompilationExtensions
{
    public static bool HasLanguageVersionAtLeastEqualTo(this Compilation compilation, LanguageVersion languageVersion, out LanguageVersion currentVersion)
    {
        if (compilation is CSharpCompilation csharpCompilation)
        {
            currentVersion = csharpCompilation.LanguageVersion;
            return currentVersion >= languageVersion;
        }
        currentVersion = LanguageVersion.Default;
        return false;
    }
}