using CliRelay.Attributes;

namespace CliRelay.Handlers;

[CliHandler("unknown")]
public class UnknownItemHandler : ICliHandler
{
    public void Handle(CliHandlerContext context)
    {
    }
}