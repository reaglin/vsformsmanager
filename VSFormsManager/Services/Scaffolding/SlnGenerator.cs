namespace VSFormsManager.Services.Scaffolding
{
    /// <summary>
    /// Generates the content of a Visual Studio <c>.sln</c> solution file
    /// for a single-project solution.
    ///
    /// Uses the Visual Studio 2022 format header (version 17) which is backward
    /// compatible with VS 2019. Two GUIDs are generated fresh each call:
    /// one for the project and one for the solution configuration.
    ///
    /// The C# project type GUID <c>{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</c>
    /// is a fixed well-known value recognised by Visual Studio.
    /// </summary>
    public static class SlnGenerator
    {
        // Well-known Visual Studio C# project type GUID
        private const string CSharpProjectTypeGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";

        /// <summary>
        /// Produces the complete content of a <c>.sln</c> file for a solution
        /// named <paramref name="solutionName"/> containing a single project
        /// whose <c>.csproj</c> is located at
        /// <c>{solutionName}\{solutionName}.csproj</c> relative to the
        /// solution file itself.
        /// </summary>
        public static string Generate(string solutionName)
        {
            var projectGuid  = Guid.NewGuid().ToString("B").ToUpper();
            var solutionGuid = Guid.NewGuid().ToString("B").ToUpper();

            // Relative path from .sln to .csproj (they live in sub-folder of same name)
            var projectRelativePath =
                $"{solutionName}\\{solutionName}.csproj";

            return
                $"""

                Microsoft Visual Studio Solution File, Format Version 12.00
                # Visual Studio Version 17
                VisualStudioVersion = 17.0.31903.59
                MinimumVisualStudioVersion = 10.0.40219.1
                Project("{CSharpProjectTypeGuid}") = "{solutionName}", "{projectRelativePath}", "{projectGuid}"
                EndProject
                Global
                	GlobalSection(SolutionConfigurationPlatforms) = preSolution
                		Debug|Any CPU = Debug|Any CPU
                		Release|Any CPU = Release|Any CPU
                	EndGlobalSection
                	GlobalSection(ProjectConfigurationPlatforms) = postSolution
                		{projectGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                		{projectGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU
                		{projectGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU
                		{projectGuid}.Release|Any CPU.Build.0 = Release|Any CPU
                	EndGlobalSection
                	GlobalSection(SolutionProperties) = preSolution
                		HideSolutionNode = FALSE
                	EndGlobalSection
                	GlobalSection(ExtensibilityGlobals) = preSolution
                		SolutionGuid = {solutionGuid}
                	EndGlobalSection
                EndGlobal
                """;
        }
    }
}
