using KludgeBox.DI.Requests;
using KludgeBox.DI.Requests.MpSyncInjection;

namespace KludgeBox.DI;

/// <summary>
/// Точка подключения опциональной MpSync-инъекции (раньше входила в
/// <see cref="RequestsScanner.CreateDefault"/> ядра). Вынесена сюда вместе с
/// Godot-derived типом <c>AttributeMultiplayerSynchronizer</c>.
/// </summary>
public static class MpSyncInjectionExtensions
{
    /// <summary>
    /// Регистрирует <see cref="MpSyncInjectionRequestScanner"/> в сканере запросов,
    /// включая обработку атрибута <c>[Sync]</c> через <c>AttributeMultiplayerSynchronizer</c>.
    /// </summary>
    public static RequestsScanner EnableMpSyncInjection(this RequestsScanner scanner)
    {
        scanner.RegisterRequestScanner(new MpSyncInjectionRequestScanner());
        return scanner;
    }
}
