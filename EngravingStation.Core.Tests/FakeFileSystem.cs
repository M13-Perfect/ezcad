using EngravingStation.Core.Services;

namespace EngravingStation.Core.Tests;

internal sealed class FakeFileSystem : IFileSystem
{
    private readonly HashSet<string> _files;

    public FakeFileSystem(IEnumerable<string> files)
    {
        _files = files.Select(Path.GetFullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public bool FileExists(string path) => _files.Contains(Path.GetFullPath(path));
}
