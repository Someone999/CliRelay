using System.Collections.Concurrent;
using System.Diagnostics;

namespace CliRelay;

public static class Global
{
    public static ConcurrentBag<Process> ActiveProcesses { get; } = new ConcurrentBag<Process>();
}