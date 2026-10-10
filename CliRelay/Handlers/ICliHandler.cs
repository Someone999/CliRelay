using CliRelay.Configs;

namespace CliRelay.Handlers;

public record DryRunEntry(string HandlerName, string Verb, string Content, string? Note = null);
public interface ICliHandler
{
    void Handle(CliHandleContext context);
    IEnumerable<DryRunEntry> GetDryRunEntries(CliHandleContext context);
}