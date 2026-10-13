namespace AssetRipper.SerializationLogic;

public sealed class SerializableRegistryType : SerializableType
{
    public static SerializableRegistryType Shared { get; } = new();

    private SerializableRegistryType() : base(null, PrimitiveType.Complex, "ManagedReferencesRegistry")
    {
        MaxDepth = 0;
        Fields = [
            new(SerializablePrimitiveType.GetOrCreate(PrimitiveType.Int), 0, "version", true)
        ];
    }
}