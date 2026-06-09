using EngravingStation.Core.Services;

namespace EngravingStation.Infrastructure;

public sealed class LocalFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
}
