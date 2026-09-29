# Replication System — Design and Implementation Plan

## 1. Goal

A delta replication mechanism for plain C# game objects.

- **Server side.** For an object and its *baseline* (the last state that was sent), produce a `byte[]` that contains only the members marked with `[Replicated]` that changed since that baseline.
- **Client side.** Apply that `byte[]` to an **existing** object by **assigning members**. The object is never recreated.

Must support:

- values: primitives, strings, enums, `Nullable<T>`, common Godot structs;
- nested objects;
- lists and dictionaries, including lists and dictionaries of objects that have `[Replicated]` members of their own (element members are tracked individually);
- quantization and float comparison tolerance;
- members that are replicated only on full sync or when explicitly marked dirty ("manual" members);
- sending a full snapshot to a newly connected client at **any moment of the frame**.

Out of scope, handled later by other code:

- the service that walks game objects at the end of a frame;
- spawn and despawn of root objects;
- networking and transport;
- references between network entities. Only NetId values are used for those, so a NetId is just an `int`/`long` member for this module.

## 2. Key design decisions

| Decision | Rationale |
|---|---|
| Change detection by comparing live values with a **shadow copy** (the baseline). The comparison is against the last *sent* value, not the previous frame. | No setter hooks or `INotifyPropertyChanged` are needed, and any field or property works. Slow drift below a threshold accumulates and is eventually sent. There is no dependency on CommunityToolkit or MessagePack. |
| **One baseline per object.** One delta per frame is shared by all clients. | Simple, and memory is O(objects). The transport must be **reliable + ordered** (for example ENet reliable channel or reliable RPC). |
| Collections are synchronized by **state**, not by an operation journal. A list is internally a dictionary `slotId → item` plus the order of slots. | Applying a delta is idempotent. This is what makes mid-frame snapshots correct, and there is no single-consumer journal. |
| **No separate "full" mode on the wire.** "Full" means a delta where everything is marked changed: an all-ones member mask, and for collections `reset` plus all entries. | There is one reader code path. The overhead is N mask bits. |
| Nested objects are always written as "presence + type + member mask". If the reference changed, the whole subtree is written (`forceAll`). The receiver reuses its existing instance when the runtime type matches. | Members are assigned and objects are not recreated. The protocol needs no extra "full" flag. |
| **Snapshots are written from the baseline (shadow), not from the live object.** | A snapshot taken mid-frame from live state desyncs the client. Example: a field was 5 in the last delta, becomes 7 mid-frame (the snapshot sends 7), and returns to 5 before the end of the frame. The delta then contains nothing for that field, so the new client stays at 7 forever. A snapshot of the baseline plus any later delta is always consistent. |
| Custom **bit-packing** (`BitWriter`/`BitReader`), no external serializer. | 1-bit booleans and masks, and quantized values use exactly the bits they need. KludgeBox gets no new dependencies. |
| Member access through **reflection + `Expression.Compile`** into typed `Func<TOwner,TValue>` / `Action<TOwner,TValue>` delegates. The type model is built once per type and cached. | No boxing in the hot path. This matches the existing style of `FieldAccessor`. The existing `FieldAccessor`/`PropertyAccessor`/`MembersScanner` are **not** reused, for three reasons. They are `object`-typed, so value types would be boxed every frame. `MembersScanner` eagerly compiles accessors for all members and skips get-only properties. DI depends on their current behavior. |
| The `ITypeIdMapping` interface is used for polymorphic type ids, and `TypesMappingService` implements it. | This is a reuse of existing code. The interface is needed because `TypesMappingService.AddTypes` logs through the Godot sink, which crashes unit tests that run without the engine. |

## 3. Module layout

The new code lives in `KludgeBox/Replication/`. Root namespace `KludgeBox.Replication`; sub-folders use sub-namespaces.

```
KludgeBox/Core/ITypeIdMapping.cs                    (new; TypesMappingService implements it)
KludgeBox/Reflection/Access/TypedAccessors.cs       (new, reusable)
KludgeBox/Replication/
  ReplicatedAttribute.cs      [Replicated(Manual = false, Tolerance = 0)]
  QuantizeAttribute.cs        [Quantize(precision)] | [Quantize(min, max, precision = 1)]
  ManualReplication.cs        static MarkDirty(object owner, string memberName)
  Replicator.cs               public facade
  ReplicationBaseline.cs      shadow of one root object (opaque for users)
  ReplicationLimits.cs
  ReplicationException.cs     ReplicationException, ReplicationFormatException
  ReplicatedList.cs           public collection types live in the root namespace
  ReplicatedDictionary.cs
  Bits/        BitWriter.cs, BitReader.cs                         (namespace KludgeBox.Replication.Bits)
  Codecs/      IReplicationCodec.cs, ReplicationCodecs.cs, PrimitiveCodecs.cs, StringCodec.cs,
               EnumCodec.cs, NullableCodec.cs, GodotCodecs.cs, ComponentAdapter.cs,
               Quantizers.cs, QuantizedCodec.cs, ToleranceCodec.cs, CodecOptions.cs
  Nodes/       ReplicationNode.cs (+ Shadow), ValueNode.cs, ObjectNode.cs, ListNode.cs, DictionaryNode.cs
  Model/       ReplicationContext.cs, ReplicationTypeModel.cs (+ ObjectState), MemberReplicator.cs,
               ReplicationModelBuilder.cs, ValueOptions.cs, SchemaHash.cs
KludgeUnitTests/              new xUnit project, no Godot runtime
```

Code style follows the existing code (see `Persistence/Exposables`):

- Allman braces, `_camelCase` private fields, `ImplicitUsings` enabled, nullable reference types disabled, `net10.0`;
- XML docs in Russian, like the rest of the library.

## 4. Public API

```csharp
public sealed class Replicator
{
    public Replicator(ITypeIdMapping typeIds = null, ReplicationCodecs codecs = null,
                      ReplicationLimits limits = null, Serilog.ILogger logger = null); // logger defaults to LogFactory
    public ReplicationCodecs Codecs { get; }

    public ReplicationBaseline CreateBaseline(object target);                    // the first delta writes everything
    public bool TryWriteDelta(ReplicationBaseline baseline, out byte[] data);    // false = nothing changed
    public bool TryWriteDelta(ReplicationBaseline baseline, BitWriter writer);   // for batching many objects into one packet
    public bool TryWriteSnapshot(ReplicationBaseline baseline, out byte[] data); // from the baseline, valid at any moment
    public bool TryWriteSnapshot(ReplicationBaseline baseline, BitWriter writer);// false = baseline never written yet
    public void Apply(object target, ReadOnlySpan<byte> data);                   // rejects trailing data (>= 8 bits left)
    public void Apply(object target, ref BitReader reader);                      // reads exactly one object
    public ulong GetSchemaHash(Type type);                                       // compare on handshake
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ReplicatedAttribute : Attribute
{
    public bool Manual { get; set; }      // not auto-compared; sent on full sync or after ManualReplication.MarkDirty
    public double Tolerance { get; set; } // 0 = exact comparison
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class QuantizeAttribute : Attribute
{
    public QuantizeAttribute(double precision);                         // unbounded, zigzag varint
    public QuantizeAttribute(double min, double max, double precision = 1); // bounded, fixed bits
}

public static class ManualReplication
{
    public static void MarkDirty(object owner, string memberName);
}
```

Contracts, to be documented in XML docs and in the README:

- After `TryWriteDelta` returns `true`, the data **must** reach all clients, because the baseline has already been updated.
- If `TryWriteDelta` throws, the writer is rewound and the baseline is reset to "not initialized". The next delta then writes everything, which recovers the clients.
- Replicated collections on the receiving side must not be mutated locally, because slot ids belong to the server.
- Codecs registered after a type model was built do not affect that model.

## 5. Nodes and shadows (core abstraction)

Every replicated value is handled by a **node**. Members, collection items and dictionary values all compose from the same nodes, which is why lists of objects, dictionaries of objects and lists of lists need no special code.

```csharp
internal abstract class Shadow { }

internal abstract class ReplicationNode
{
    public abstract Type ValueType { get; }
    public abstract bool RequiresSetter { get; }  // true for value nodes (the member must be assignable)
    public abstract bool HasInnerState { get; }   // true for object/collection nodes: content may change while the reference stays the same
    public abstract Shadow CreateShadow();
    public abstract void AppendSchema(StringBuilder sb, HashSet<Type> visited);
}

internal abstract class ReplicationNode<T> : ReplicationNode
{
    // Compare current with the shadow, write the payload, update the shadow.
    // Returns false and leaves the writer untouched (rewinds it) when nothing changed and forceAll == false.
    public abstract bool WriteDelta(T current, Shadow shadow, BitWriter writer, bool forceAll);
    // Snapshot path: write the shadow's content as a "forceAll" payload. Never reads live state
    // (except manual members, see §9).
    public abstract void WriteShadow(Shadow shadow, BitWriter writer);
    // Receiver path. For object/collection nodes it returns `existing` when it can be reused.
    public abstract T Read(T existing, ref BitReader reader);
}
```

Nodes receive a shared `ReplicationContext` (limits, `ITypeIdMapping`, model builder, logger, depth counter) through their constructor.

- `ValueNode<T>`: wraps an `IReplicationCodec<T>`. Its shadow is `{ T LastSent; bool HasValue; }`.
- `ObjectNode<T> where T : class`: presence, type info and the body from `ReplicationTypeModel`. Its shadow is `{ object Ref; bool Written; ObjectState State; }`.
- `ListNode<TItem>`: for `ReplicatedList<TItem>`, with items handled by a child node. Its shadow is:
  ```
  { object Ref; bool Written; int Version; List<Shadow> SlotShadows /* index = slot, null = absent */; List<int> Order; }
  ```
- `DictionaryNode<TKey,TValue>`: for `ReplicatedDictionary<TKey,TValue>`, with keys through a codec and values through a child node. Its shadow is `{ object Ref; bool Written; int Version; Dictionary<TKey, Shadow> Entries; }`.
- `MemberReplicator<TOwner,TValue>`: getter, optional setter, node, name and manual flag. On read it calls the setter only if the node returned a different reference (reference types) or always (value types). If a setter is needed but missing, it throws `ReplicationException`. This is why a readonly field holding a `ReplicatedList` is fine.

`ReplicationTypeModel` holds the ordered members of one runtime type. `ObjectState` holds `Shadow[] Members` and `int[] ManualVersions`, the latter being null if the type has no manual members.

## 6. Wire format (bit-level, LSB-first)

**Primitives**

- `WriteBits(value, n)` writes the low `n` bits of `value`, least significant bit first, into bytes LSB-first.
- `varuint` is a sequence of 8-bit groups: 7 data bits (low part first) plus a continuation bit (bit 7). At most 10 groups.
- `varint` is `varuint` of the zigzag-encoded value.
- `string` is `varuint(byteCount + 1)` (0 means null) followed by UTF-8 bytes of 8 bits each. The codec has a maximum byte count (default 64 KiB).

**Object body**

1. A member mask of `N` bits, where `N` is the number of members in the model, in model order.
2. The payloads of members whose bit is set, in model order.

The writer reserves the mask bits, lets every member try to write, rewinds the members that wrote nothing, and patches the mask bits afterwards. This is a single pass.

**Root payload**

The root payload is just the object body of the root type: no header, no presence bit, no type info. A snapshot is the same body with all mask bits set.

**Object node payload**

1. `[1 bit present]`.
2. If present and the declared type is not sealed: `[1 bit isDeclaredType]`, followed by `varuint typeId` from `ITypeIdMapping` when that bit is 0. If polymorphism is needed and there is no mapping, throw `ReplicationException`.
3. The object body of the runtime type.

**Collection payload** (list and dictionary)

```
[1 bit present]
[1 bit reset]
if !reset:  removals  ([1][key])* [0]
upserts:    ([1][key][item payload])* [0]
list only:  [1 bit hasOrder] (varuint count, count × varuint slot)
```

- The key is a `varuint` slot id for lists and the key codec for dictionaries.
- The item payload is the child node's payload. For an existing object item that means only its changed members; for a new item it means everything (`forceAll`).
- Entries use continuation bits rather than a count prefix. The writer does not know in advance whether an item delta will be empty, so it rewinds empty entries.

**Receiver semantics**

- Object present = 0: the result is null.
- Object present = 1: resolve the runtime type. Reuse `existing` if `existing.GetType() == type`, otherwise create a new instance through the parameterless constructor (non-public allowed, reuse `ExposableReflection.GetInstanceOfType`). Then read the body. A type id that is unknown or not assignable to the declared type is a `ReplicationFormatException`.
- Collection, no reset:
  1. Apply removals. A missing key is a no-op, which is required for idempotence after a snapshot.
  2. Apply upserts. A new list slot is appended to the end of the order, in upsert order.
  3. If `hasOrder`, the slot sequence must be a permutation of the current slots, otherwise it is a format error.
- Collection, `reset`:
  1. Keep the collection instance.
  2. Upserted keys reuse the existing item under the same key.
  3. Keys that were not upserted are removed.
  4. For lists, the new order is the upsert sequence.

**Sender semantics**

- **Value node.** Write if `forceAll`, or if the shadow has no value, or if `codec.IsChanged(lastSent, current)`. Then store `current` as `LastSent`.
- **Object node.**
  - `null` both now and in the shadow: nothing is written unless `forceAll`.
  - Same reference: write a member delta. If it is empty and `!forceAll`, rewind and return false.
  - New reference: create a new `ObjectState` and write with `forceAll`.
- **List node.**
  - `reset` = `forceAll`, or the shadow was never written, or the reference changed.
  - `structural` = `reset`, or `list.Version` differs from the shadow's version.
  - If `!structural` and the item node has no inner state, return false (O(1) fast path).
  - **Order check.** Do it before modifying the shadow. `expected` = the shadow order filtered by slots still in use, followed by slots absent from the shadow in current order. Set `hasOrder` if `current.Order != expected`. In practice `Add`/`RemoveAt`/indexer set send no order, while `Insert`/`Sort` send the full order.
  - Removals are shadow slots that are no longer used.
  - Upserts iterate the current order. New slots get a fresh item shadow and `forceAll`. Existing slots get an item delta, but value items only when `structural`.
  - Finally copy the current order and version into the shadow.
- **Dictionary node.** The same as the list node, without order.

## 7. Collections

**`ReplicatedList<T> : IList<T>, IReadOnlyList<T>`**

Storage:

- `T[] _items` indexed by slot, plus `bool[] _used`;
- `List<int> _order`;
- a `Stack<int>` of free slots. Slots are reused, so ids stay small in varuint.
- `int _version`, incremented on every mutation: add, insert, remove, indexer set, clear, sort.

Public API: indexer, `Count`, `Add`, `AddRange`, `Insert`, `RemoveAt`, `Remove`, `Clear`, `IndexOf`, `Contains`, `CopyTo`, `Sort(Comparison<T>)`, `Sort(IComparer<T> = null)`, and a struct enumerator with version checking.

Internal API for the node:

- `Version`, `Order`, `IsSlotUsed(slot)`, `GetSlot(slot)`, `TryGetSlot(slot, out item)`;
- receiver side: `SetSlot(slot, item)` (append to the order if new), `MarkSlotRemoved(slot)` + `CompactOrder()`, `SetOrder(...)`, `BeginReset()` / `EndReset()`.

Slot allocation lazily skips slots that are in use, because the receiver may assign arbitrary slot ids. A generation counter is **not** needed on the wire: an upsert overwrites the slot's state, and the server detects a reused slot either by reference (object items) or simply sends the new value (value items).

**`ReplicatedDictionary<TKey,TValue> : IDictionary<TKey,TValue>, IReadOnlyDictionary<TKey,TValue>`**

A wrapper over `Dictionary<TKey,TValue>` plus `int _version`, which is incremented on every mutation. It has an internal accessor to the inner dictionary for the node. Keys must be codec types; object keys are rejected by the model builder.

Plain `List<T>`, `T[]`, `Dictionary<,>` and `HashSet<T>` members are rejected with a hint to use the replicated collections.

## 8. Quantization and tolerance

- `[Quantize(min, max, precision)]`
  - Steps: `steps = round((max - min) / precision)`. Bits: `bitLength(steps)`, which may be 0.
  - Writes are fixed-width.
  - A value that is out of range or NaN is clamped (NaN becomes `min`), and a **warning** is logged **once per member** to avoid per-frame spam.
  - On read, a step greater than `steps` is a format error.
  - The model builder rejects `precision <= 0`, `max <= min`, and `steps > 2^62`.
- `[Quantize(precision)]` (unbounded)
  - Writes `round(v / precision)` as a zigzag `varint`, so the size depends on magnitude.
  - `|scaled| > 2^62` or NaN is clamped and warned about once.
- Change detection for quantized members compares **quantized steps**. A change below one quantum is not sent, while slow drift is still sent once it crosses a step boundary.
- `[Replicated(Tolerance = x)]`: a value counts as changed if `max |Δcomponent| > x` relative to the last sent value.
  - If a delta is NaN, fall back to exact comparison, where NaN equals NaN and NaN differs from a number.
  - Tolerance can be combined with `Quantize`: both conditions must hold.
  - Without `Quantize` or `Tolerance`, comparison is exact, using `float.Equals` semantics so that NaN equals NaN and `-0` equals `0`.
- Supported component types for Quantize and Tolerance:
  - `float`, `double`, `Vector2`, `Vector3`, `Vector4`, `Quaternion`, `Color`;
  - integers `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, which round to the nearest integer and are clamped to the type range.
- Quantize and Tolerance on a collection member apply to its **value items** (dictionary: values, not keys). On an object member they are a model error.
- Integers without `Quantize` are written at full width.

## 9. Manual members

- `[Replicated(Manual = true)]` is never auto-compared.
- `ManualReplication.MarkDirty(owner, nameof(Member))` increments a version in a `ConditionalWeakTable<object, Dictionary<string,int>>`.
- `WriteMembers` writes a manual member only when `forceAll` is set or the owner's version differs from `ObjectState.ManualVersions[i]`. When it does, it writes the member with `forceAll: true` and stores the version.
- Versions are used instead of flags, so nobody "consumes" the mark and the order of calls does not matter.
- The first write and snapshots write manual members from the **live** object, via `node.WriteDelta(live, node.CreateShadow(), writer, forceAll: true)` with a throwaway shadow. A new client gets the current value.
- A manual nested object or collection makes the whole subtree manual.

## 10. Type model

- **Member discovery.** Walk the type chain from the most-base type to the most-derived, stopping before `object`. On each level take `DeclaredOnly | Instance | Public | NonPublic` fields and properties that have `[Replicated]`, sorted by name (ordinal). The order is deterministic; `GetFields` order is not guaranteed by the spec.
- **Skipped or rejected members.**
  - Skip overriding properties (`GetMethod.GetBaseDefinition() != GetMethod`); the base declaration wins.
  - Reject static members, indexers, and properties without a getter.
- **Recursive types.** The model is put into the cache **before** its members are built, so recursive types work. If building fails, it is removed from the cache.
- **Node selection by member type:**

  | Member type | Node |
  |---|---|
  | has a codec (incl. enums and `Nullable<T>` of codec types) | `ValueNode` (requires setter) |
  | `ReplicatedList<>` | `ListNode` |
  | `ReplicatedDictionary<,>` | `DictionaryNode` |
  | `List<>`, arrays, `Dictionary<,>`, `HashSet<>` | error with a hint |
  | other class | `ObjectNode` |
  | struct without a codec | error "register a codec" |

- Every error is a `ReplicationException` that includes `DeclaringType.FullName + "." + member.Name`.
- **Generic construction.** The builder creates generic nodes and members through generic factory methods (`MakeGenericMethod`), so everything inside is strongly typed.
- **Schema hash.** FNV-1a 64 over a canonical description:

  ```
  type full name
    { member name
      node descriptor (value type + quantize/tolerance/manual,
                       object declared type recursively with a visited set,
                       list(item),
                       dict(key, value)) }
  ```

## 11. Environment notes for implementers

- **.NET 10 SDK.** In the cloud sandbox install it with `apt-get install -y dotnet-sdk-10.0`, because `builds.dotnet.microsoft.com` is blocked there. nuget.org is reachable.
- **Build and test commands:** `dotnet build KludgeBox.slnx` and `dotnet test KludgeUnitTests`.
- **Godot runtime.** Unit tests run **without the Godot runtime**, so Godot structs (`Vector2`, …) work, but any `GD.*` call crashes the process. Consequences:
  - Replication code must never call `GD.*`.
  - Unit tests must pass an explicit Serilog logger to `Replicator`, e.g. an in-memory `ILogEventSink` that collects events. `LogFactory` loggers write through the Godot sink.
  - Never construct `TypesMappingService` in unit tests; use a fake `ITypeIdMapping`.
- **`KludgeTests/`** is the old Godot-hosted test runner. Do not add replication tests there.
- **Cross-agent coordination.** `plan.md` is the source of truth. If a step has to change the design, update `plan.md` in the same commit.

## 12. Implementation steps

Workflow for every step:

1. An implementer agent does the step on the branch. The step includes its tests.
2. The agent runs `dotnet build KludgeBox.slnx` and `dotnet test KludgeUnitTests`; everything must be green.
3. The agent commits as `Replication step N: <title>`.
4. A reviewer agent checks the diff against this plan and the review checklist (§13).
5. Fixes are made, and the next step starts.

A step must not implement features of later steps. It may add `internal` hooks that later steps need.

### Step 1 — Unit test project, CI, bit streams

Files:

- `KludgeUnitTests/KludgeUnitTests.csproj`:
  - `Microsoft.NET.Sdk`, `net10.0`, xUnit (`Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`), `IsPackable=false`;
  - a `ProjectReference` to `KludgeBox/KludgeBox.csproj`;
  - added to `KludgeBox.slnx`.
- `.github/workflows/build.yml`: `dotnet-version: 10.0.x`. The current 8.0.x cannot build net10 or `.slnx`.
- `KludgeBox/Replication/ReplicationException.cs` and `ReplicationLimits.cs` (`MaxDepth = 64`, `MaxCollectionCount = 65536`).
- `KludgeBox/Replication/Bits/BitWriter.cs`: a growable `byte[]` and `BitPosition`, with these operations:
  - `WriteBits(ulong, int 0..64)`, `WriteBool`;
  - `WriteVarUInt`, `WriteVarInt`;
  - `WriteSingle`, `WriteDouble` (raw bits), `WriteBytes(ReadOnlySpan<byte>)`;
  - `Rewind(int bitPosition)`, `SetBit(int bitPosition, bool)`, `Reset()`;
  - `ByteLength`, `ToArray()`, `AsSpan()`.

  Writes clear the target bits before setting them, so `Rewind` needs no zeroing. The unused high bits of the last byte are zeroed on `ToArray` and `AsSpan`.
- `KludgeBox/Replication/Bits/BitReader.cs`: a `ref struct` over `ReadOnlySpan<byte>` with the mirror read methods plus `BitPosition` and `RemainingBits`. Any overrun, or a varuint longer than 10 groups, throws `ReplicationFormatException`.

Implementation notes (as built):

- `ReplicationFormatException` derives from `ReplicationException`.
- `ReplicationLimits` also exposes `DefaultMaxDepth` / `DefaultMaxCollectionCount` constants; both limits are settable properties.
- `BitWriter.AsSpan()` returns `ReadOnlySpan<byte>`, valid until the next write. The mirror of `WriteBytes` is `BitReader.ReadBytes(Span<byte> destination)`.
- `BitWriter.MaxVarUIntGroups` (10), `BitWriter.ZigZagEncode` and `BitReader.ZigZagDecode` are public helpers.
- `ReadVarUInt` additionally rejects a 10th group that carries more than one data bit (value does not fit in 64 bits) or has the continuation bit set.
- A failed read (overrun) does not move `BitPosition`. Invalid bit counts (outside 0..64) and invalid `Rewind`/`SetBit` positions are programmer errors and throw `ArgumentOutOfRangeException`.

Tests (`KludgeUnitTests/Replication/BitStreamTests.cs`):

- a smoke test that a KludgeBox type (e.g. `Godot.Vector2` math) loads without the engine;
- round-trips for:
  - bit widths 0..64 at arbitrary offsets;
  - booleans;
  - varuint/varint edge values (0, 127, 128, max, min, negative);
  - float/double including NaN, ±0 and ±∞;
  - byte spans;
- `Rewind` followed by new writes produces the same bytes as if nothing had been written;
- `SetBit` patching;
- zeroed trailing bits;
- overrun throws;
- a malformed varuint throws.

### Step 2 — Typed accessors and type id mapping

Files:

- `KludgeBox/Reflection/Access/TypedAccessors.cs`:
  - `CreateGetter<TOwner,TValue>(MemberInfo)`;
  - `CreateSetter<TOwner,TValue>(MemberInfo)`, which returns `null` for readonly fields, literal fields and properties without a setter;
  - `CanWrite(MemberInfo)`.

  Uses Expression trees, converts the owner to `member.DeclaringType` and the value to or from `TValue` when they differ, and works with non-public members.
- `KludgeBox/Core/ITypeIdMapping.cs` (`int GetId(Type)`, `Type GetType(int)`). `TypesMappingService` implements it; no behavior change.

Implementation notes (as built):

- `TypedAccessors` is a `public static class` in `KludgeBox.Reflection.Access`.
- Static members are supported, and the owner argument is ignored for them. A literal (`const`) field has a getter that returns the constant value.
- Invalid input throws `ArgumentException`, not `ReplicationException`, because the helper is general-purpose; the model builder (step 5) wraps these errors with the member path. `ArgumentException` covers: something other than a field or property, indexers, a getter for a property that has no getter, an owner type unrelated to `DeclaringType`, and a value type that `Expression.Convert` cannot convert. A `null` member throws `ArgumentNullException`.
- `CreateSetter` for an instance member of a **value-type** declaring type throws `ArgumentException`, because the write would change a copy. `CanWrite` is checked first, so a readonly field of a struct still returns `null`.
- `CanWrite` returns `false` for readonly fields, literal fields and properties with no setter (including a non-public one). An init-only setter counts as writable.
- A `PropertyInfo` obtained through a derived type (`ReflectedType != DeclaringType`) is normalized to the declaring type's `PropertyInfo` first, because otherwise private accessors of the base type are invisible. A literal field's value is taken with `FieldInfo.GetValue(null)` so that enum constants keep their enum type.
- Tests are in `KludgeUnitTests/Reflection/TypedAccessorsTests.cs`. `TypesMappingService` is checked only through `typeof` (never constructed, see §11).

Tests: getters and setters for:

- public, private and protected fields and properties;
- base-class members accessed through a derived `TOwner`;
- private setters;
- struct-typed values;
- readonly field → `null` setter;
- get-only property → `null` setter;
- init-only property → a working setter.

### Step 3 — Value codecs

Files: `Codecs/IReplicationCodec.cs`:

```csharp
public interface IReplicationCodec<T>
{
    void Write(BitWriter writer, T value);
    T Read(ref BitReader reader);
    bool IsChanged(T lastSent, T current);   // wire-level change detection
}
```

`ReplicationCodecs` (public registry):

- `Register<T>(IReplicationCodec<T>)`, `TryGet<T>(out IReplicationCodec<T>)`, `CanEncode(Type)`;
- defaults are registered in the constructor;
- `TryGet` lazily builds `EnumCodec<TEnum>` and `NullableCodec<T>` through `MakeGenericType` and caches them.

Codecs:

- Primitives: `bool` (1 bit), `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `char` at full width; `float` and `double` as raw bits with `float.Equals`/`double.Equals` for `IsChanged`.
- `StringCodec`: format as in §6, ordinal comparison, configurable max bytes; exceeding the limit on read throws `ReplicationFormatException`.
- `EnumCodec<TEnum>`:
  - non-`[Flags]` enums whose defined values are all non-negative use `max(1, bitLength(maxDefined))` bits; writing an undefined out-of-range value throws `ReplicationException`;
  - otherwise the full width of the underlying type is used, as raw bits;
  - no boxing (use `Unsafe.As`).
- `NullableCodec<T>`: `[1 bit hasValue]` followed by the inner value.
- Godot: `Vector2`, `Vector2I`, `Vector3`, `Vector3I`, `Vector4`, `Vector4I`, `Color`, `Quaternion`, `Rect2`, `Rect2I`, written component-wise; `IsChanged` compares components with `float.Equals` semantics.

Tests:

- a round-trip for every codec;
- `IsChanged` semantics (NaN, -0, equal, different);
- enum bit width and the out-of-range throw;
- the null string;
- the string limit;
- registering a custom codec overrides the default;
- `CanEncode` for enum, `int?` and an unsupported class.

Implementation notes (as built):

- All codecs live in `KludgeBox.Replication.Codecs` and are `public sealed` so that users can compose them. Stateless codecs expose a shared `Instance`; `StringCodec.Default` is the 64 KiB instance registered by default. Primitive codec names follow the CLR names (`Int32Codec`, `SingleCodec`, …).
- `ReplicationCodecs` is thread-safe (a lock; the registry is used at model build time, not per frame). Explicitly registered codecs always win over lazily built ones. `Register<T>` also drops all lazily built codecs, so a cached `NullableCodec<int>` picks up a newly registered `int` codec. `Nullable<T>` is buildable for any `T` that has a codec, including user-registered structs. `CanEncode` returns `false` for open generic, by-ref, pointer and by-ref-like types; `null` throws `ArgumentNullException`.
- `EnumCodec<TEnum>` has the constraint `where TEnum : unmanaged, Enum` and exposes `BitCount` and `IsCompact`. Signedness is checked against the underlying type, so a negative value is detected by its sign bit. In compact mode, **reading** a value above the maximum defined one throws `ReplicationFormatException` (the plan only specified the write side). Undefined values inside the range are allowed in both directions. An enum without defined values uses 1 bit and only accepts 0.
- `StringCodec`: writing a string longer than the limit throws `ReplicationException`. On read, the declared length is checked against the limit and against `RemainingBits` before any buffer is taken, so bogus lengths cannot cause large allocations. Encoding and decoding use a 256-byte `stackalloc` buffer or an `ArrayPool` buffer. Invalid UTF-8 is decoded with replacement characters, not an exception. An empty string is read back as `string.Empty`, which is distinct from `null`.
- `NullableCodec<T>` exposes the wrapped codec as `Inner`. Its `IsChanged` treats null vs. value as changed and otherwise delegates to the inner codec.
- The Godot codecs assume the default single-precision Godot build (`real_t` = `float`). Integer vectors and `Rect2I` compare with `==`; float vectors, `Color`, `Quaternion` and `Rect2` compare component-wise with `float.Equals`.
- `BitReader.ReadBytes` now takes a `scoped Span<byte>`. Without it, a `stackalloc` buffer cannot be passed to a `ref BitReader` parameter. There is no behavior change.
- Tests are in `KludgeUnitTests/Replication/CodecTests.cs`. They include a no-allocation check for enum `Write` and `IsChanged`.

### Step 4 — Quantization and tolerance

Files:

- `QuantizeAttribute.cs` (§4).
- `Codecs/ComponentAdapter.cs`: `ComponentAdapter<T>` with `Count`, `IsInteger`, `Get(T, i)`, `Compose(double[])` and `MaxDelta(T, T)` (NaN-propagating), plus a static registry for the types in §8.
- `Codecs/Quantizers.cs`: `BoundedQuantizer` and `UnboundedQuantizer` (§8), with an out-of-range reporter that logs a warning once per member (`Serilog.ILogger` and a member path).
- `Codecs/QuantizedCodec.cs`: component-wise over a quantizer; `IsChanged` compares quantized steps; a reused buffer so reads do not allocate.
- `Codecs/ToleranceCodec.cs`: decorator (§8).
- `Codecs/CodecOptions.cs`: `IReplicationCodec<T> Apply<T>(IReplicationCodec<T> baseCodec, QuantizeAttribute quantize, double tolerance, string memberPath, ILogger logger)`. It validates the parameters and throws `ReplicationException` for unsupported types or invalid parameters.

Tests:

- bounded and unbounded round-trips within precision;
- bit counts;
- clamping plus exactly one warning across repeated out-of-range writes (in-memory sink);
- NaN handling;
- a change below one quantum does not count as changed, while accumulated drift does;
- tolerance semantics and the combination with quantize;
- integer rounding and clamping;
- vector, quaternion and color components;
- invalid attribute parameters throw.

Implementation notes (as built):

- All new types are public and live in `KludgeBox.Replication.Codecs` (except `QuantizeAttribute`, root namespace). `QuantizeAttribute` exposes `Min`, `Max`, `Precision` and `IsBounded` (unbounded: `Min`/`Max` are NaN); it does not validate in its constructor, validation happens in `CodecOptions.Apply` so the error carries the member path.
- **Deviation:** `ComponentAdapter<T>.Compose` takes `ReadOnlySpan<double>` instead of `double[]`, and `QuantizedCodec<T>.Read` collects components in a `stackalloc` buffer instead of a reused field buffer. This is allocation-free and also thread-safe. The static registry is the non-generic `ComponentAdapter` class (`TryGet<T>`, `IsSupported(Type)`), backed by a static generic cache, so lookup costs nothing after the first call. `MaxDelta` is NaN-propagating (a NaN component or `inf - inf` gives NaN).
- Rounding everywhere (steps, quantized values, integer composition) is `MidpointRounding.AwayFromZero`. Integer adapters use generic math (`CreateSaturating`), so composition clamps to the type range and NaN becomes 0. `long` components go through `double`, so values above 2^53 lose precision.
- `Quantizer` is the abstract base (`Quantize(double) → long step`, `Dequantize`, `WriteStep`, `ReadStep`); the once-per-member warning lives in the base (`ReportOutOfRange`, `HasReportedOutOfRange`), one quantizer instance per member, shared by all components. A `null` logger means no logging.
- `BoundedQuantizer`: `steps = round((max - min) / precision)`, `bitLength(steps)` bits. **Clarification:** the effective step size is `(max - min) / steps`, not `precision`, so `min` and `max` are exactly representable and dequantized values never leave `[min, max]` (with `precision` stepping, a rounded-up `steps` would dequantize above `max`). The error stays below one `precision`. `steps == 0` writes 0 bits and always reads `min`. Also rejected: non-finite `min`/`max`/`precision`. Reading a step above `steps` is a `ReplicationFormatException`.
- `UnboundedQuantizer`: NaN becomes step 0, `|step| > 2^62` is clamped to ±2^62; reading a step with magnitude above 2^62 is a `ReplicationFormatException`.
- Out-of-range warnings can also be triggered from `IsChanged` (it quantizes both values); it is still only one warning per member.
- `ToleranceCodec<T>.IsChanged` = `inner.IsChanged && (MaxDelta is NaN || MaxDelta > tolerance)`. With a plain inner codec this gives the exact NaN fallback of §8; with a `QuantizedCodec` inner it gives "both conditions must hold". Tolerance must be finite and `>= 0`.
- `CodecOptions.Apply` returns the base codec unchanged when there is no `Quantize` and tolerance is 0 (no type support needed then). With `Quantize`, the base codec is not used for the wire. `Nullable<T>` of supported types is not supported (not listed in §8).
- Tests are in `KludgeUnitTests/Replication/QuantizationTests.cs`, including a no-allocation check for quantized/tolerance `Write`, `Read` and `IsChanged`.

### Step 5 — Type model, value members, `Replicator` core

Files:

- `ReplicatedAttribute.cs` with `Tolerance` only. `Manual` comes in step 9.
- `Nodes/ReplicationNode.cs` (+ `Shadow`) and `Nodes/ValueNode.cs`.
- `Model/ReplicationContext.cs`, `Model/ValueOptions.cs`, `Model/MemberReplicator.cs`, `Model/ReplicationTypeModel.cs` (+ `ObjectState`, `WriteMembers`, `WriteShadowMembers`, `ReadMembers`), `Model/ReplicationModelBuilder.cs`, `Model/SchemaHash.cs`.
- `ReplicationBaseline.cs` and `Replicator.cs`, with the full API from §4, delta, snapshot and apply.

Scope:

- In this step only value members are supported. Class-typed members and collections throw `ReplicationException("not supported yet")`; steps 6–8 replace that.
- `Apply(target, ReadOnlySpan<byte>)` rejects trailing data of 8 bits or more.
- Baseline recovery on an exception while writing (§4).
- `CreateBaseline`, `TryWrite*` and `Apply` validate their arguments. A baseline created by another `Replicator` throws.

Tests:

- The first delta contains everything.
- No change → `false` and the writer is untouched.
- One changed member → a smaller payload, and only that member changes on the client (the client's other members were set to sentinel values beforehand and stay unchanged).
- Coverage of member kinds:
  - private, protected and base-class fields and properties;
  - a property setter is invoked on the client (a counter in the setter);
  - `[field: Replicated]` on an auto-property backing field.
- Model errors: a readonly value member, a static member, `List<int>`, `int[]`, a struct without a codec.
- Tolerance and Quantize on members end to end.
- **Snapshot from the baseline:**
  - a mid-frame snapshot followed by the end-of-frame delta makes the client equal to the server, including the case "a value changed after the last delta and then reverted before the next one";
  - `TryWriteSnapshot` before the first delta returns `false`.
- Schema hash: stable across `Replicator` instances; changes when a member, its type or its quantization changes.
- Recovery after an exception from a custom codec: the next delta is full.
- Trailing data rejected.

Implementation notes (as built):

- **Public API additions:** `ReplicationBaseline.Target` and `ReplicationBaseline.IsWritten` (read-only). Everything in `Nodes/` and `Model/` is `internal`. `Replicator` is not thread-safe; the model builder is (lock).
- `Replicator` copies `ReplicationLimits` in its constructor and validates them (`MaxDepth >= 1`, `MaxCollectionCount >= 1`, otherwise `ArgumentOutOfRangeException`). `logger == null` falls back to `LogFactory.GetForStatic<Replicator>()`. The `out byte[]` overloads use one reused scratch `BitWriter`; only the returned array is allocated.
- Argument validation: `null` arguments throw `ArgumentNullException`; a baseline from another `Replicator` throws `ReplicationException`. The model type must be a concrete class (value types, interfaces, abstract, array, open generic types → `ReplicationException`).
- **Nodes.** `ReplicationNode<T>.ValueType` is sealed to `typeof(T)`. `ValueNode<T>` holds the final codec (`CodecOptions.Apply` is called **once per member**, and that node is used for delta, snapshot and read, so warn-once is per member) and a precomputed schema descriptor. The shadow stores the **raw** value. `WriteShadow` on a never-written value shadow throws `InvalidOperationException` (cannot happen through the public API).
- **`MemberReplicator<TOwner,TValue>`**: `TOwner` = `member.DeclaringType`, `TValue` = exact member type (created via `MakeGenericMethod`, so no conversion or boxing). The owner arrives as `object` and is cast with `Unsafe.As` (the model guarantees the type). Value nodes (`!HasInnerState`) never call the getter on read and always call the setter; inner-state nodes (steps 6–8) read the existing value and call the setter only when the node returned a different reference, throwing `ReplicationException` if there is no setter. `WriteShadow(owner, …)` and `ReplicationTypeModel.WriteShadowMembers(owner, …)` already take the live owner for manual members (step 9). `ObjectState.ManualVersions` exists and is always `null` for now.
- **Exceptions with member path.** `WriteMembers`, `WriteShadowMembers` and `ReadMembers` wrap any exception (except `OutOfMemoryException`) that has no path yet as `"{path}: failed to write/read: {message}"`, keeping the original as `InnerException`. `ReplicationFormatException` stays `ReplicationFormatException`; everything else (including non-replication exceptions from user codecs) becomes `ReplicationException`. The path is stored in `Exception.Data`, so outer levels (nested objects, step 6) do not re-wrap: the message names the innermost member.
- The member mask may be longer than 64 bits (written/read in 64-bit chunks; read into a `stackalloc` buffer for up to 512 members).
- **Model builder.** Member discovery walks base → derived with `DeclaredOnly | Instance | Static | Public | NonPublic` per level (so private base members are found and static members can be rejected), fields and properties together sorted by ordinal name per level. `[Quantize]` without `[Replicated]` is ignored. Overriding properties: if the override has `[Replicated]` and the base declaration has it too, the override is skipped (the base declaration is the member; calls are virtual anyway). **Deviation:** if only the override has `[Replicated]`, this is a `ReplicationException` ("put the attribute on the base declaration") instead of being silently skipped. Getter/setter creation errors from `TypedAccessors` are wrapped into `ReplicationException` with the path. By-ref, pointer and by-ref-like member types are rejected.
- Node routing (`ReplicationModelBuilder.CreateNode(Type, ValueOptions, path)`, reusable for collection items in steps 7–8): codec → `ValueNode`; arrays, `List<>`, `Dictionary<,>`, `HashSet<>` → error with a hint; other structs → "register a codec"; every other class → `"… not supported yet."` (steps 6–8 replace this).
- Recursive-type support: the model is cached before its members are built; if any build fails, **all** models created during the outermost `GetModel` call are removed, so no cached model can reference a half-built one.
- Depth: `ReplicationContext.EnterWrite/EnterRead/Exit/ResetDepth` exist for step 6 (write overflow → `ReplicationException`, read overflow → `ReplicationFormatException`). The root is depth 0 and does not enter; root operations reset the depth after an exception.
- **Schema hash (deviation and additions).** The canonical description is `TypeName{member:descriptor;…}`, where the object type is written as `Type.Name` (without namespace), so the hash describes the wire layout and not the type's location; nested object types already described are written as `ref(Name)`. A value descriptor is `value(<value type full name without assembly info>;codec=<codec type>(params);q=min..max/precision;t=tolerance)`. Codec params: `StringCodec` max byte count, `EnumCodec` bit count and compact flag (so a changed enum width changes the hash), `NullableCodec` inner codec; user codecs contribute their type name. Numbers use `"R"` in the invariant culture. Generic type names are formatted recursively without assembly-qualified arguments, so the hash does not depend on assembly versions.
- `ReplicationCodecs` now creates lazy codecs with `BindingFlags.DoNotWrapExceptions` (review 3, item 4), so a throwing codec constructor surfaces as its own exception, not `TargetInvocationException`.
- Tests are in `KludgeUnitTests/Replication/ReplicatorTests.cs` (53 cases), including no-allocation checks for an unchanged delta and for changed delta + apply of value members.

### Step 6 — Nested objects

Files: `Nodes/ObjectNode.cs`, plus builder routing for class-typed members.

Covers:

- presence, type info and polymorphism through `ITypeIdMapping`;
- instance reuse on the client;
- setter usage rules (§5);
- depth limit on write and read (a cycle produces a clear `ReplicationException` instead of a `StackOverflowException`);
- recursive types, e.g. a tree node that has a child of its own type;
- `WriteShadow` support.

Tests:

- a nested change is applied into the **same** client instance (`Assert.Same`);
- a server-side replacement with a new instance of the same type makes the client reuse its instance and receive all members;
- a replacement with a subtype makes the client create a subtype instance through the fake `ITypeIdMapping`;
- a subtype without a mapping throws `ReplicationException`;
- null ↔ object transitions in both directions;
- a get-only nested member works while the instance is reused, and throws when the client must create an instance;
- a cycle hits the depth limit;
- a recursive type;
- mid-frame snapshot plus delta with nested objects;
- an unknown type id or a non-assignable type id on read throws `ReplicationFormatException`.

### Step 7 — `ReplicatedList<T>`

Files: `ReplicatedList.cs` (§7) and `Nodes/ListNode.cs` (§6). Builder routing, including Quantize/Tolerance propagation to value items.

Tests:

- `ReplicatedList` behaves like `List<T>` for all public operations (compare against a `List<T>` model after random operations);
- value items: add, remove, set, clear and insert replicate correctly;
- Add/RemoveAt/Set send no order, while Insert/Sort do (assert via payload size or a debug hook);
- slot reuse keeps slot ids small;
- no changes → `false` (O(1) fast path);
- object items:
  - an element member change is sent as an element delta and applied into the same client element instance;
  - a new element is sent in full;
  - a removed element is removed;
- a list of lists;
- a readonly `ReplicatedList` field (reused instance, no setter needed);
- replacing the list instance on the server triggers a reset and the client keeps its instance;
- mid-frame snapshot plus delta;
- **a randomized test**: 1000 frames of random mutations on the server list (value items and object items), with the client structurally equal to the server after every frame, including when a snapshot is taken at a random moment for a second client;
- malformed data (a bad order permutation, a duplicate slot in reset, a slot id above the limit) throws `ReplicationFormatException`.

### Step 8 — `ReplicatedDictionary<TKey,TValue>`

Files: `ReplicatedDictionary.cs` (§7) and `Nodes/DictionaryNode.cs` (§6). Builder routing; keys must have a codec.

Tests:

- a behavior parity test against `Dictionary<,>`;
- value values and object values (in-place element deltas);
- removals;
- clear;
- reset on instance replacement;
- a dictionary of lists and a list of dictionaries;
- a mid-frame snapshot;
- a randomized test like in step 7;
- an object key type is rejected by the model builder;
- a null key on read throws `ReplicationFormatException`.

### Step 9 — Manual members

Files: `ManualReplication.cs`, plus `Manual` in `ReplicatedAttribute`, `ObjectState.ManualVersions`, and the manual branches in `WriteMembers` and `WriteShadowMembers` (§9). The schema hash includes the manual flag.

Tests:

- a manual member is sent in the first delta;
- it is not sent when it changes without `MarkDirty`;
- after `MarkDirty` it is sent once and not again until the next `MarkDirty`;
- a snapshot contains the live value of a manual member even when it was not marked dirty;
- manual nested objects and collections send the whole subtree;
- `MarkDirty` on an unknown member name is harmless;
- manual members of objects inside a list.

### Step 10 — Hardening and documentation

- An end-to-end randomized test over a complex model: nested objects with lists of objects that contain dictionaries, quantized values and manual members. It runs many frames with snapshots for late clients at random moments, and every client must equal the server after each frame.
- A fuzz test: random or truncated byte arrays applied to fresh objects. The only allowed exception type is `ReplicationFormatException`, and there must be no hangs or huge allocations (limits respected).
- A performance sanity test (not a benchmark): a delta pass over 1000 unchanged objects allocates nothing (`GC.GetAllocatedBytesForCurrentThread`).
- A "Replication" section in `README.md`: attributes, `Replicator` usage for server and client, snapshot usage, the contracts from §4, and caveats (the reliable-ordered transport requirement, no local mutation of replicated collections on the client, place `[Replicated]` on properties when setter side effects are needed).
- `GeneralVersion` → `4.1.0` in `KludgeBox.csproj`.

## 13. Review checklist (for every step)

- The implementation matches this plan, especially the wire format (§6) and receiver semantics. Deviations are reflected in `plan.md`.
- The hot path has no boxing and no per-frame allocations for unchanged objects: typed delegates, no LINQ in write or read paths.
- No `GD.*` calls. The logger is used only for warnings and is injected.
- Exceptions: `ReplicationException` for configuration errors, with the member path in the message; `ReplicationFormatException` for bad input data.
- Tests cover the step's edge cases, including snapshot and delta interaction where applicable. Tests do not depend on internals unless unavoidable.
- `dotnet build KludgeBox.slnx` has no new warnings, and `dotnet test KludgeUnitTests` is green.
- Style matches the existing code; XML docs are on public types and members.
