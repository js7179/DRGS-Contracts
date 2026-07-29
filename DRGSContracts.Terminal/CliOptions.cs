using System.CommandLine;

namespace DRGSContracts.Terminal;

/// <summary>
/// Helper class that returns the possible <see cref="System.CommandLine.Option" /> that should
/// be attached to a <see cref="System.CommandLine.RootCommand" />
/// </summary>
internal static class CliOptions
{
    /// <summary>
    /// Returns an immutable list of command-line arguments for the program in the form of
    /// a <see cref="System.CommandLine.Option" /> that should be attached to
    /// <see cref="System.CommandLine.RootCommand" />. For internal use only.
    /// </summary>
    /// <returns>Immutable list of <see cref="System.CommandLine.Option" /> to be attached</returns>
    internal static IReadOnlyList<Option> GetProgramOptions()
    {
        return
        [
            new Option<int>("--gpu-index", "-g")
            {
                Description = "Which GPU that is available should be used for display capturing",
                DefaultValueFactory = _ => 0
            },
            new Option<int>("--display-index", "-d")
            {
                Description = "What display to capture under the specified GPU",
                DefaultValueFactory = _ => 0
            },
            new Option<DirectoryInfo>("--output-folder", "-o")
            {
                Description = "Where the screengrabs for VC/LOs go for further processing",
                DefaultValueFactory = _ => new DirectoryInfo(Directory.GetCurrentDirectory()),
            }
        ];
    }
}