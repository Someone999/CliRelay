namespace CliRelay.Configs.Resolvers;

public interface IConfigItemLoader<in TSource, out TTarget>
{
    public TTarget? LoadConfig(TSource content, ProgramCommandArguments arguments);
}