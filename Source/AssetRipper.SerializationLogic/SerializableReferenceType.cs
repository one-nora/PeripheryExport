namespace AssetRipper.SerializationLogic;

public sealed class SerializableReferenceType : SerializableType
{
    public static SerializableReferenceType Single { get; } = new("managedReference");
    public static SerializableReferenceType Array { get; } = new("managedRefArrayItem");

    private SerializableReferenceType(string Name) : base(null, PrimitiveType.Complex, Name)
    {
        MaxDepth = 0;
        Fields = [
            new(SerializablePrimitiveType.GetOrCreate(PrimitiveType.Long), 0, "rid", true)
        ];
    }
}