using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using CliRelay.Attributes;
using CliRelay.Configs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Serilog;
using Serilog.Core;

namespace CliRelay.Handlers.CSharp;

[CliHandler("csharpScriptFile")]
public class CSharpScriptFileHandler : ICliHandler
{
    class ResolvedType
    {
        public MethodInfo? RunMethod;
        public MethodInfo? DryRunMethod;
    }

    public void Handle(CliHandleContext context)
    {
        var type = GetBestMatchType(context);
        if (type == null)
        {
            Log.Logger.Error(
                $"[{nameof(CSharpScriptFileHandler)}] No types marked CliScriptAttribute or no types has Run method");
            return;
        }

        var runMethod = type.RunMethod;
        runMethod?.Invoke(null, [context]);
    }

    public IEnumerable<DryRunEntry> GetDryRunEntries(CliHandleContext context)
    {
        var type = GetBestMatchType(context);
        if (type == null)
        {
            Log.Logger.Error(
                $"[{nameof(CSharpScriptFileHandler)}] No types marked CliScriptAttribute or no types have Run method");
            yield break;
        }

        var dryRunMethod = type.DryRunMethod;
        var list = dryRunMethod?.Invoke(null, [context]) as IEnumerable<DryRunEntry>;
        if (list == null)
        {
            yield return new DryRunEntry(nameof(CSharpScriptFileHandler), "Script", "<Not specified>");
            yield break;
        }

        foreach (var entry in list)
        {
            yield return entry;
        }
    }

    private ResolvedType? GetBestMatchType(CliHandleContext context)
    {
        var asm = BuildAssembly(context);
        if (asm is null)
        {
            return null;
        }

        var matchedTypes = ScanMatchedTypes(asm);
        if (matchedTypes.Count == 0)
        {
            return null;
        }
        
        return matchedTypes.Count > 1
            ? throw new AmbiguousMatchException("More than one types marked as CliScriptAttribute")
            : matchedTypes[0];
    }
    private static ReadOnlyCollection<ResolvedType> ScanMatchedTypes(Assembly assembly)
    {
        List<ResolvedType> resolvedTypes = new List<ResolvedType>();
        var types = assembly.GetTypes();
        foreach (var type in types)
        {
            if (!TryResolveType(type, out var result))
            {
                continue;
            }

            resolvedTypes.Add(result);
        }

        return resolvedTypes.AsReadOnly();

        bool TryResolveType(Type t, [NotNullWhen(true)] out ResolvedType? result)
        {
            if (t.GetCustomAttribute<CliScriptTypeAttribute>() is null)
            {
                result = null;
                return false;
            }

            var runMethod = t.GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
            if (runMethod is null)
            {
                result = null;
                return false;
            }

            var runMethodParams = runMethod.GetParameters();
            if (runMethodParams.Length != 1 || runMethodParams[0].ParameterType != typeof(CliHandleContext))
            {
                result = null;
                return false;
            }

            var dryRunMethod = t.GetMethod("DryRun", BindingFlags.Public | BindingFlags.Static);
            if (dryRunMethod is null)
            {
                result = new ResolvedType
                {
                    DryRunMethod = null,
                    RunMethod = runMethod,
                };

                return true;
            }

            var dryRunMethodParams = dryRunMethod.GetParameters();
            if (dryRunMethodParams.Length != 1 || dryRunMethodParams[0].ParameterType != typeof(CliHandleContext))
            {
                result = null;
                return false;
            }

            result = new ResolvedType
            {
                DryRunMethod = dryRunMethod,
                RunMethod = runMethod,
            };

            return true;
        }
    }

    private static Assembly? BuildAssembly(CliHandleContext context)
    {
        var data = context.Data;
        if (data is null)
        {
            throw new InvalidDataException($"Handler {nameof(CSharpScriptFileHandler)} received a null payload)");
        }

        var files = data["files"]?.AsArray();
        if (files is null)
        {
            throw new InvalidDataException(
                $"data.files must be specified for handler {nameof(CSharpScriptFileHandler)}.");
        }

        List<SyntaxTree> syntaxTrees = new List<SyntaxTree>(files.Count);
        foreach (var file in files)
        {
            if (file is not JsonValue val || !val.TryGetValue(out string? fileName))
            {
                continue;
            }

            var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(fileName));
            syntaxTrees.Add(syntaxTree);
        }

        var referenceArr = data["references"]?.AsArray();
        if (referenceArr is null)
        {
            return CompileScript(syntaxTrees, []);
        }

        HashSet<MetadataReference> references = new HashSet<MetadataReference>(referenceArr.Count);
        foreach (var reference in referenceArr)
        {
            if (reference is not JsonValue val || !val.TryGetValue(out string? referenceName))
            {
                continue;
            }

            if (!File.Exists(referenceName))
            {
                throw new FileNotFoundException("Reference file not found", referenceName);
            }

            references.Add(MetadataReference.CreateFromFile(referenceName));
        }

        return CompileScript(syntaxTrees, references);
    }
    
    private static Assembly? CompileScript(List<SyntaxTree> syntaxTrees, HashSet<MetadataReference> references)
    {
        if (syntaxTrees.Count == 0)
        {
            return null;
        }

        var asmName = Guid.NewGuid().ToString();
        AddSdkDlls(references);

        references.Add(MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location));
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var compilation = CSharpCompilation.Create(asmName, syntaxTrees, references, options);
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Compilation failed: {result.Diagnostics}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        return assembly;

        void AddSdkDlls(HashSet<MetadataReference> refs)
        {
            refs.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            string runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
            string[] coreAssemblies =
            [
                "System.Runtime.dll",
                "System.Console.dll",
                "System.Collections.dll",
                "System.Linq.dll",
                "System.Linq.Expressions.dll",
                "System.ObjectModel.dll",
                "System.Text.RegularExpressions.dll",
                "System.Threading.Tasks.dll",
                "Microsoft.CSharp.dll" 
            ];

            foreach (var sdkAsmPath in coreAssemblies)
            {
                string fullPath = Path.Combine(runtimeDirectory, sdkAsmPath);
                if (File.Exists(fullPath))
                {
                    refs.Add(MetadataReference.CreateFromFile(fullPath));
                }
            }
          
        }
    }
}