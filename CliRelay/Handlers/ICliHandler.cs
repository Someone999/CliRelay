using CliRelay.Configs;

namespace CliRelay.Handlers;

public interface ICliHandler
{
    void Handle(CliHandlerContext context);
}