namespace CliRelay.Attributes;

public class RouteAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}