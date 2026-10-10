using CliRelay.Attributes;

namespace CliRelay.Handlers;

[CliHandler("unknown")]
public class UnknownItemHandler : ICliHandler
{
    public void Handle(CliHandleContext context)
    {
    }

    public IEnumerable<DryRunEntry> GetDryRunEntries(CliHandleContext context)
    {
        yield return new DryRunEntry("Unknown", "None", "null");
    }
}