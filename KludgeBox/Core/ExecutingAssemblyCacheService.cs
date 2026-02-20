using System.Reflection;

namespace KludgeBox.Core;

public class ExecutingAssemblyCacheService
{
    public IReadOnlyList<Type> Types => _executingAssemblyTypes.AsReadOnly();
    private List<Type> _executingAssemblyTypes;

    public void Init(Assembly executingAssembly)
    {
        _executingAssemblyTypes = executingAssembly.GetTypes().ToList();
    }
}