namespace CliRelay.Processes;

public class ProcessRedirection(StreamWriter? inputStream, StreamReader? outputStream, StreamReader? errorStream)
{
    public StreamWriter? InputStream { get; init; } = inputStream;
    public StreamReader? OutputStream { get; init; } = outputStream;
    public StreamReader? ErrorStream { get; init; } = errorStream;
}