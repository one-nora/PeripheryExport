namespace AssetRipper.SerializationLogic;

public sealed class SerializableRegistryType : SerializableType
{
    public static SerializableRegistryType Shared { get; } = new(2);

    private SerializableRegistryType(int version) : base(null, PrimitiveType.Complex, "ManagedReferencesRegistry")
    {
        MaxDepth = 0;
        Fields = [
            new(SerializablePrimitiveType.GetOrCreate(PrimitiveType.Int), 0, "version", true),
            new(SerializableRefObjType.Shared, 1, "RefIds", true)
        ];
    }
}

public sealed class SerializableRefObjType : SerializableType
{
    public static SerializableRefObjType Shared { get; } = new();

    private SerializableRefObjType() : base(null, PrimitiveType.Complex, "ReferencedObject")
    {
        MaxDepth = 1;
        Fields = [
            new(SerializablePrimitiveType.GetOrCreate(PrimitiveType.Long), 0, "rid", true),
            new(SerializableManagedTyType.Shared, 0, "type", true),
            new(SerializableRefDataType.Shared, 0, "data", true),
        ];
    }
}

public sealed class SerializableManagedTyType : SerializableType
{
    public static SerializableManagedTyType Shared { get; } = new();

    private SerializableManagedTyType() : base(null, PrimitiveType.Complex, "ReferencedManagedType")
    {
        MaxDepth = 1;
        Fields = [
            new(SerializablePrimitiveType.GetOrCreate(PrimitiveType.String), 0, "class", true),
            new(SerializablePrimitiveType.GetOrCreate(PrimitiveType.String), 0, "ns", true),
            new(SerializablePrimitiveType.GetOrCreate(PrimitiveType.String), 0, "asm", true),
        ];
    }
}

public sealed class SerializableRefDataType : SerializableType
{
    public static SerializableRefDataType Shared { get; } = new();

    private SerializableRefDataType() : base(null, PrimitiveType.Complex, "ReferencedObjectData")
    {
        MaxDepth = 0;
    }
}