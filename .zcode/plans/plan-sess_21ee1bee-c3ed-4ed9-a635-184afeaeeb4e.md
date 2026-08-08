# Рефакторинг KludgeBox: вынос Godot-derived типов в отдельную сборку

## Контекст и цель
Проблема: Godot-редактор загружает типы, наследуемые от `Godot.*`, и не может их выгружать/перезагружать → кастомные типы «залипают» в редакторе, ошибки при запуске. Workaround — вынести все Godot-derived типы в отдельную сборку (`KludgeBox.Sources`), которую редактор может перезагружать независимо от ядра.

## Разведка (выполнено)
**Переезжают (9 Godot-derived типов):** `AbstractStorage`, `CheckedAbstractStorage`, `AbstractMultiplayerSpawner`, `Background`, `Camera`, `NodeContainer`, `ProcessDeadChecker`, `ProcessShutdowner`, `AttributeMultiplayerSynchronizer` — все в `KludgeBox/Godot/Nodes/`.

**Ключевые зависимости, определяющие план:**
1. **`KludgeBoxServices` — `internal`** (даёт `Di`, `Rand`, `MembersScanner`). Переезжающие узлы его дёргают → нужен `InternalsVisibleTo`.
2. **Единственное ребро ядро→Sources — MpSync-связка:** `MpSyncInjectionRequest`/`Scanner` (в ядре, `DI/Requests/MpSyncInjection/`) создают `new AttributeMultiplayerSynchronizer(...)` и читают `SyncAttribute`. Решение пользователя: **перенести всю связку в Sources**, разорвав цикл.
3. **Shift-классы** (`Punch`/`Shake`/`ManualShake`/`IShiftProvider`) — не наследуются от Godot, но cohesive с `Camera`. Решение пользователя: **перевезти с Camera**.
4. **Глобальные `using static`** (`Vec2()`, `IsValid()`, `Di`, extension-методы) — надо реплицировать в Sources.

---

## Шаг 1. Создать проект `KludgeBox-Sources`
- Папка `KludgeBox-Sources/` рядом с `KludgeBox/`.
- **`KludgeBox-Sources/KludgeBox-Sources.csproj`:**
  - SDK `Godot.NET.Sdk/4.6.1`, `TargetFramework=net10.0`, `ImplicitUsings=enable`.
  - **`RootNamespace=KludgeBox`** и **`AssemblyName=KludgeBox.Sources`** — критично: неймспейсы всех типов остаются `KludgeBox.*` (как было), ничего не ломается.
  - Продублировать схему версионирования из `KludgeBox.csproj` (`GeneralVersion=3.3.3` + `VersionSuffix`).
  - `<ProjectReference Include="..\KludgeBox\KludgeBox.csproj" />`.
- Добавить проект в `KludgeBox.slnx`.

## Шаг 2. Настроить видимость и глобальные using-и
- В `KludgeBox/KludgeBox.csproj` добавить:
  ```xml
  <ItemGroup><InternalsVisibleTo Include="KludgeBox.Sources" /></ItemGroup>
  ```
  Это даёт Sources доступ к `internal KludgeBoxServices` (`Di`/`Rand`/`MembersScanner`).
- Создать **`KludgeBox-Sources/KludgeBoxGlobalUsings.cs`** — копия текущих `global using static` (extension-классы ядра + `KludgeBoxServices.Global`). Extension-классы остаются в ядре, Sources их импортирует через ProjectReference.

## Шаг 3. Перенести файлы в Sources
Перенести физически (с сохранением относительных путей под папкой, чтобы неймспейсы совпали):

**Godot-derived узлы:**
- `KludgeBox/Godot/Nodes/AbstractStorage.cs`
- `KludgeBox/Godot/Nodes/CheckedAbstractStorage.cs`
- `KludgeBox/Godot/Nodes/AbstractMultiplayerSpawner.cs`
- `KludgeBox/Godot/Nodes/Background.cs`
- `KludgeBox/Godot/Nodes/NodeContainer.cs`
- `KludgeBox/Godot/Nodes/Camera/Camera.cs` (+ `Camera.cs.uid`)
- `KludgeBox/Godot/Nodes/Process/ProcessDeadChecker.cs`
- `KludgeBox/Godot/Nodes/Process/ProcessShutdowner.cs`
- `KludgeBox/Godot/Nodes/MpSync/AttributeMultiplayerSynchronizer.cs`

**MpSync-связка (решение пользователя — целиком в Sources):**
- `KludgeBox/Godot/Nodes/MpSync/SyncAttribute.cs`
- `KludgeBox/DI/Requests/MpSyncInjection/MpSyncInjectionRequest.cs`
- `KludgeBox/DI/Requests/MpSyncInjection/MpSyncInjectionRequestScanner.cs`

**Shift-кластер (решение пользователя — с Camera):**
- `KludgeBox/Godot/Nodes/Camera/Shifts/ShiftProvider.cs` (+.uid)
- `KludgeBox/Godot/Nodes/Camera/Shifts/ManualShake.cs` (+.uid)
- `KludgeBox/Godot/Nodes/Camera/Shifts/Punch.cs` (+.uid)
- `KludgeBox/Godot/Nodes/Camera/Shifts/Shake.cs` (+.uid)

→ Целевые пути: те же относительные пути, но под `KludgeBox-Sources/` (например `KludgeBox-Sources/Godot/Nodes/...`). Неймспейсы внутри файлов **не меняются** (`KludgeBox.Godot.Nodes.*`, `KludgeBox.DI.Requests.MpSyncInjection`).

## Шаг 4. Обновить ядро
- **`DI/Requests/RequestsScanner.cs`:** убрать `new MpSyncInjectionRequestScanner()` из `CreateDefault()` и удалить `using KludgeBox.DI.Requests.MpSyncInjection;`. MpSync-инъекция становится **опциональной** (регистрируется хост-проектом).
- Удалить освободившиеся папки/файлы в `KludgeBox/` (см. Шаг 3 — исходники переехали).
- Проверить, что `KludgeBox/Godot/Nodes/` не осталась пустой без нужды (если пуста — удалить папку; `.uid`-файлы тоже переехали).

## Шаг 5. Helper регистрации MpSync в Sources
Добавить в Sources extension-метод для удобного включения MpSync-инъекции хост-проектом:
```csharp
// KludgeBox-Sources/DI/MpSyncInjectionExtensions.cs
namespace KludgeBox.DI;
public static class MpSyncInjectionExtensions
{
    public static RequestsScanner EnableMpSyncInjection(this RequestsScanner scanner)
    {
        scanner.RegisterRequestScanner(new MpSyncInjectionRequestScanner());
        return scanner;
    }
}
```
Пользователь вызывает `di.RequestsScanner.EnableMpSyncInjection()` (или аналогично), если нужна синхронизация.

## Шаг 6. Проверка сборки
- `dotnet restore` + `dotnet build` для `KludgeBox.slnx`.
- Убедиться, что ядро собирается без ссылок на Sources (однонаправленная зависимость Sources→ядро), Sources — с ProjectReference на ядро, `InternalsVisibleTo` работает.

## Шаг 7. Отчёт о затронутых зависимостях (для решения пользователя)
По итогам рефакторинга предоставлю таблицу: «что в ядре зависело от вынесенных типов → как разрешено / что осталось на твоё решение». Ожидаемые пункты:
- MpSyncInjection-код — перевезён целиком (цикл разорван).
- Extension-классы (`CameraExtensions`, `NodeTreeExtensions` и т.д.) — **остаются в ядре**, не ссылаются на вынесенные типы напрямую (`CameraExtensions` использует только Godot `Camera2D`, не наш `Camera`; `NodeTreeExtensions.FindOrAddChild<T>` — дженерик). Не сломаются.
- Сервисы (`NodeTreeService`, `I18NService`, `AutoScalingService`, `ExceptionHandlerService`) — не ссылаются на вынесенные типы. Не сломаются.
- `KludgeTests` — переезжающие узлы в тестах не используются (только `TestNode` + DI-атрибуты из ядра); при необходимости добавлю `ProjectReference` на Sources (по умолчанию добавлю превентивно).
- **Открытый вопрос (не блокирующий):** как упаковывать Sources в NuGet — включить доп. dll в пакет `KludgeBox` или отдельный пакет `KludgeBox.Sources`. Отмечу в отчёте, сделаю проект компилируемым и связанным; упаковку настрою по твоему решению.

## Примечание (не входит в задачу, только доклад)
Обнаружены артефакты: пустой `KludgeBox.SourceGenerators/` (только bin/obj, нет .csproj/исходников), осиротевший `KludgeBox/Persistence/Exposables/Persistence.csproj` (не в solution, конфликтует с основным проектом, GodotSharp 4.5.1 vs 4.6.1). Не трогаю, только укажу.

## Что НЕ делается без отдельного запроса
- Упаковка/NuGet-конфигурация Sources (открытый вопрос).
- Удаление ghost/orphan проектов.
- Изменение `KludgeTests/` (только превентивный ProjectReference при необходимости).
- Directory.Build.props (упомяну как опциональное улучшение).
