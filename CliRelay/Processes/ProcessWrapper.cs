using System.Diagnostics;

namespace CliRelay.Processes;

public class ProcessWrapper
{
    public ProcessWrapper(Process process)
    {
        OriginalProcess = process;
        StreamReader? error = null, output = null;
        StreamWriter? input = null;
        if (process.StartInfo.RedirectStandardOutput)
        {
            output = process.StandardOutput;
        }
        
        if (process.StartInfo.RedirectStandardError)
        {
            error = process.StandardError;
        }

        if (process.StartInfo.RedirectStandardInput)
        {
            input = process.StandardInput;
        }
        
        Redirection = new ProcessRedirection(input, output, error);
    }
    
    public Process OriginalProcess { get; }
    public ProcessRedirection Redirection { get; }
    public int? ExitCode
    {
        get
        {
            if (!OriginalProcess.HasExited)
            {
                return null;
            }
            
            return OriginalProcess.ExitCode;
        }
    }
    
    
    public int ProcessId => OriginalProcess.Id;
    public TimeSpan Duration => OriginalProcess.ExitTime - OriginalProcess.StartTime;
}