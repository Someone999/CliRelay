namespace CliRelay.Configs.Resolvers;

public interface IRuntimeConfigGenerator<in T>
{
    RuntimeConfig? Generate(T cfg, ProgramCommandArguments arguments);
}