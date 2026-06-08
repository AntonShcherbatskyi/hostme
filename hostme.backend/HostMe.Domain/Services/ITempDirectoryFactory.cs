namespace HostMe.Domain.Services;

public interface ITempDirectory : IDisposable
{
    string Path { get; }
}

public interface ITempDirectoryFactory
{
    ITempDirectory Create();
}
