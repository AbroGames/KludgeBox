using KludgeBox.Core.Random;
using KludgeBox.DI;
using KludgeBox.Reflection.Access;

namespace KludgeBox;

internal static class KludgeBoxServices
{
    public static DependencyInjector Di = new DependencyInjector();
    public static RandomService Rand = new RandomService();
    public static MembersScanner MembersScanner => Di.MembersScanner;
    
    public static class Global
    {
        public static DependencyInjector Di => KludgeBoxServices.Di;
    }
}