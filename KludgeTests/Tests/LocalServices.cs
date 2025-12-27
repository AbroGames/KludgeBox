using KludgeBox.DI;
using KludgeBox.Reflection.Access;

namespace KludgeTests.Tests;

public static class LocalServices
{
    public static readonly DependencyInjector Di = new DependencyInjector();
    public static readonly MembersScanner Scanner = new MembersScanner();
}