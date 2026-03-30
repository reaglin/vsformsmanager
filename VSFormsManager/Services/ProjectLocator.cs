namespace VSFormsManager.Services
{
    /// <summary>
    /// Locates the Visual Studio project that owns a given source file by walking
    /// up the directory tree until a <c>.csproj</c> file is found.
    ///
    /// The project name is taken from the <c>.csproj</c> filename (without extension),
    /// which matches the convention used by Visual Studio.
    /// </summary>
    public static class ProjectLocator
    {
        /// <summary>
        /// Starting from the directory containing <paramref name="sourceFilePath"/>,
        /// walks up the tree looking for a <c>.csproj</c> file.
        /// </summary>
        /// <returns>
        /// A tuple of (<c>projectName</c>, <c>projectFilePath</c>).
        /// Both are <see cref="string.Empty"/> if no project file is found within
        /// <paramref name="maxDepth"/> levels.
        /// </returns>
        public static (string ProjectName, string ProjectFilePath) FindProject(
            string sourceFilePath,
            int    maxDepth = 8)
        {
            var directory = Path.GetDirectoryName(sourceFilePath);
            if (string.IsNullOrEmpty(directory))
                return (string.Empty, string.Empty);

            var current = new DirectoryInfo(directory);
            int depth   = 0;

            while (current != null && depth < maxDepth)
            {
                var csproj = current.GetFiles("*.csproj").FirstOrDefault();
                if (csproj != null)
                {
                    var projectName = Path.GetFileNameWithoutExtension(csproj.Name);
                    return (projectName, csproj.FullName);
                }

                // Also look for .sln one level above as a stopping heuristic,
                // but do not stop — the .csproj may still be above.
                current = current.Parent;
                depth++;
            }

            return (string.Empty, string.Empty);
        }

        /// <summary>
        /// Returns just the project name for a given source file.
        /// Useful for quick display without needing the full path.
        /// </summary>
        public static string GetProjectName(string sourceFilePath) =>
            FindProject(sourceFilePath).ProjectName;
    }
}
