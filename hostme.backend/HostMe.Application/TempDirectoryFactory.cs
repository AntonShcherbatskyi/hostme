using HostMe.Domain.Services;

namespace HostMe.Application;

public sealed class TempDirectoryScope : ITempDirectory
{
    public string Path { get; }

    public TempDirectoryScope()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hostme_" + Guid.NewGuid());
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);
    }
}

public sealed class TempDirectoryFactory : ITempDirectoryFactory
{
    public ITempDirectory Create() => new TempDirectoryScope();
}
