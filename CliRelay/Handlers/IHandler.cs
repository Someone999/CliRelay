using CliRelay.Configs;

namespace CliRelay.Handlers;

public interface IHandler
{
    void Handle(string command, RuntimeConfig config);
}