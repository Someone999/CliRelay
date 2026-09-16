namespace CliRelay.Configs.Resolvers.Generators;

public interface IRuntimeConfigGenerator<in T>
{
    RuntimeConfig? Generate(T cfg, ProgramCommandArguments arguments);
}