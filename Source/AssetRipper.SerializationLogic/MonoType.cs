using System.Diagnostics;

namespace AssetRipper.SerializationLogic;

internal sealed class MonoType : SerializableType
{
	private MonoType(ITypeDefOrRef type) : base(type.Namespace ?? "", PrimitiveType.Complex, type.Name ?? "")
	{
	}

	internal MonoType(ITypeDefOrRef type, IReadOnlyList<Field> fields) : this(type)
	{
		Fields = fields;
	}

	internal void SetDepth()
	{
		Debug.Assert(IsMaxDepthKnown == false, "The depth of this type is already known.");
		int maxDepth = 0;
		foreach (Field field in Fields)
		{
			if (field.Type.IsMaxDepthKnown)
			{
				maxDepth = Math.Max(maxDepth, field.Type.MaxDepth + 1);
			}
			else
			{
				maxDepth = -1;
				break;
			}
		}
		MaxDepth = maxDepth;
	}

	internal void SetHasManagedReference()
	{
		Debug.Assert(HasManagedReference == false, "Managed references have already been searched.");

		if (Fields.Count > 0 && Fields[^1] is { Type.Name: "ManagedReferencesRegistry", Name: "references" })
		{
			HasManagedReference = true;
			return;
		}

		bool hasManagedReference = false;
		foreach (Field field in Fields)
		{
			hasManagedReference |= field.Type.IsManagedReference() || field.Type.HasManagedReference;
		}
		HasManagedReference = hasManagedReference;
	}
}
