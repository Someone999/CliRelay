namespace CliRelay.Attributes;

public class CliFunctionAttribute(string name) : Attribute
{
    public string Name => name;
}