using System.Collections.Concurrent;
using System.Collections.Immutable;
using CliRelay.Collections;
using CliRelay.Processes;
using HsManCommonLibrary.ValueHolders;

namespace CliRelay;

public static class Global
{
    public static IValueHolder<ProcessWrapper> ActiveProcess { get; internal set; } = new ValueHolder<ProcessWrapper>();
    public static ConcurrentList<ProcessWrapper> HistoryProcesses { get; } = new ConcurrentList<ProcessWrapper>();
}