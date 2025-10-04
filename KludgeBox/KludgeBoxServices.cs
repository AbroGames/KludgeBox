using KludgeBox.Core;
using KludgeBox.Core.Random;
using KludgeBox.DI;

namespace KludgeBox;

internal static class KludgeBoxServices
{
    public static DependencyInjector Di = new DependencyInjector();
    public static RandomService Rand = new RandomService();
    
    public static class Global
    {
        public static DependencyInjector Di => KludgeBoxServices.Di;
    }
}