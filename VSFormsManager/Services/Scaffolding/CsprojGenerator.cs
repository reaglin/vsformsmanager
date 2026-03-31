using VSFormsManager.Models;

namespace VSFormsManager.Services.Scaffolding
{
    /// <summary>
    /// Generates the content of a new SDK-style <c>.csproj</c> file based on
    /// the properties captured in <see cref="ProjectScaffoldConfig"/>.
    ///
    /// Mirrors the source project's TargetFramework, OutputType, UseWindowsForms,
    /// Nullable, and ImplicitUsings settings and includes only the
    /// <see cref="PackageReferenceEntry"/> items the user left checked.
    /// </summary>
    public static class CsprojGenerator
    {
        /// <summary>
        /// Produces the complete content of the new <c>.csproj</c> file.
        /// </summary>
        public static string Generate(
            ProjectScaffoldConfig     config,
            CsprojReader.CsprojInfo   sourceInfo)
        {
            var selectedPackages = config.PackageReferences
                .Where(p => p.IsIncluded)
                .ToList();

            var sb = new System.Text.StringBuilder();

            sb.AppendLine("""<Project Sdk="Microsoft.NET.Sdk">""");
            sb.AppendLine();
            sb.AppendLine("  <PropertyGroup>");
            sb.AppendLine($"    <OutputType>{config.OutputType}</OutputType>");
            sb.AppendLine($"    <TargetFramework>{config.TargetFramework}</TargetFramework>");
            sb.AppendLine($"    <RootNamespace>{config.RootNamespace}</RootNamespace>");
            sb.AppendLine($"    <AssemblyName>{config.SolutionName}</AssemblyName>");

            if (sourceInfo.Nullable)
                sb.AppendLine("    <Nullable>enable</Nullable>");

            if (sourceInfo.ImplicitUsings)
                sb.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");

            if (config.UseWindowsForms)
                sb.AppendLine("    <UseWindowsForms>true</UseWindowsForms>");

            sb.AppendLine("  </PropertyGroup>");

            if (selectedPackages.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("  <ItemGroup>");
                foreach (var pkg in selectedPackages)
                {
                    if (!string.IsNullOrWhiteSpace(pkg.Version))
                        sb.AppendLine(
                            $"""    <PackageReference Include="{pkg.PackageId}" Version="{pkg.Version}" />""");
                    else
                        sb.AppendLine(
                            $"""    <PackageReference Include="{pkg.PackageId}" />""");
                }
                sb.AppendLine("  </ItemGroup>");
            }

            sb.AppendLine();
            sb.AppendLine("</Project>");

            return sb.ToString();
        }
    }
}
