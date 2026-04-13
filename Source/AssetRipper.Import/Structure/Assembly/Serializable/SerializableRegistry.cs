using AssetRipper.Assets;
using AssetRipper.Assets.Cloning;
using AssetRipper.Assets.IO.Writing;
using AssetRipper.Assets.Metadata;
using AssetRipper.Assets.Traversal;
using AssetRipper.Import.Logging;
using AssetRipper.Import.Structure.Assembly.Managers;
using AssetRipper.IO.Endian;
using AssetRipper.IO.Files.SerializedFiles;
using AssetRipper.SerializationLogic;
using AssetRipper.SourceGenerated.Classes.ClassID_114;

namespace AssetRipper.Import.Structure.Assembly.Serializable;

public sealed class SerializableRegistry : UnityAssetBase, IDeepCloneable
{
	private UnityVersion Version { get; set; }
	public override int SerializedVersion => Type.Version;
	public override bool FlowMappedInYaml => Type.FlowMappedInYaml;
	public SerializableValue ManagedVersionField => Fields[0];

	internal SerializableRegistry(int depth)
	{
		Depth = depth;
		Type = SerializableRegistryType.Shared;
		Fields = new SerializableValue[Type.Fields.Count];
		NestedTypes = [];
	}

	public void Read(ref EndianSpanReader reader, UnityVersion version, TransferInstructionFlags flags, IAssemblyManager assemblyManager, bool InsideManagedRegistry = false)
	{
		if (InsideManagedRegistry)
		{
			Fields = [];
			NestedTypes = [];
			return;
		}
		
		Fields = new SerializableValue[Type.Fields.Count];
		Version = version;
		Fields[0].Read(ref reader, version, flags, assemblyManager, Depth, Type.Fields[0]);

		if (ManagedVersionField.AsInt64 == 1)
		{
			int idx = 0;
			SerializableStructure arrayEntry, typeObj;
			SerializableStructure? valueObj;
			SerializableType.Field? nestedType;
			List<SerializableStructure> refIds = [];
			List<SerializableType.Field?> nestedTypes = [];
			while (ReadReferencedObject(ref reader, version, flags, assemblyManager, Depth + 1, idx, out typeObj, out nestedType, out valueObj))
			{

				arrayEntry = new(SerializableRefObjType.Shared, 0);
				arrayEntry.Fields[0].AsInt64 = idx++;
				arrayEntry.Fields[1].AsAsset = typeObj;
				arrayEntry.Fields[2].AsAsset = valueObj;
				refIds.Add(arrayEntry);
				nestedTypes.Add(nestedType);
			}

			arrayEntry = new(SerializableRefObjType.Shared, 0);
			arrayEntry.Fields[0].AsInt64 = idx++;
			arrayEntry.Fields[1].AsAsset = typeObj;
			arrayEntry.Fields[2].AsAsset = null!;
			refIds.Add(arrayEntry);
			nestedTypes.Add(nestedType);

			Fields[1].AsAssetArray = refIds.ToArray();
			NestedTypes = nestedTypes.ToArray();
		}
		else if (ManagedVersionField.AsInt64 == 2)
		{
			int length = reader.ReadInt32();

			SerializableStructure arrayEntry;
			List<SerializableStructure> refIds = new(length);
			List<SerializableType.Field?> nestedTypes = new(length);
			for (int idx = 0; idx < length; idx++)
			{
				arrayEntry = new(SerializableRefObjType.Shared, 0);
				arrayEntry.Fields[0].Read(ref reader, version, flags, assemblyManager, Depth + 1, arrayEntry.Type.Fields[0]);

				bool result = ReadReferencedObject(ref reader, version, flags, assemblyManager, Depth + 1, arrayEntry.Fields[0].AsInt64, out SerializableStructure typeObj, out SerializableType.Field? nestedType, out SerializableStructure? valueObj);

				arrayEntry.Fields[1].AsAsset = typeObj;
				arrayEntry.Fields[2].AsAsset = valueObj!;
				refIds.Add(arrayEntry);
				nestedTypes.Add(nestedType);
			}

			Fields[1].AsAssetArray = refIds.ToArray();
			NestedTypes = nestedTypes.ToArray();
		}
		else
		{
			throw new NotSupportedException($"Unsupported ManageReferences version {ManagedVersionField.AsInt64}");
		}

		// Returns true on successful object read.
		static bool ReadReferencedObject(ref EndianSpanReader reader, UnityVersion version, TransferInstructionFlags flags, IAssemblyManager assemblyManager, int depth, Int64 refId, out SerializableStructure typeObj, out SerializableType.Field? nestedType, [NotNullWhen(true)] out SerializableStructure? valueObj)
		{
			
			typeObj = new(SerializableManagedTyType.Shared, depth);
			typeObj.Read(ref reader, version, flags, assemblyManager, false);

			string Class = typeObj.Fields[0].AsString, Ns = typeObj.Fields[1].AsString, Asm = typeObj.Fields[2].AsString;
			if ((Class == "Terminus" && Ns == "UnityEngine.DMAT" && Asm == "FAKE_ASM") || refId == -1 || refId == -2)
			{
				valueObj = null;
				nestedType = null;
				return false;
			}

			ScriptIdentifier scriptID = assemblyManager.GetScriptID(Asm, Ns, Class);
			SerializableType? resolvedType;

			if (!assemblyManager.IsSet)
			{
				throw new NotSupportedException("AssemblyManager was not set.");
			}
			else if (!assemblyManager.IsValid(scriptID))
			{
				throw new NotSupportedException($"Referenced assembly {Asm} {Ns} {Class} could not be found.");
			}
			else if (!assemblyManager.TryGetSerializableType(scriptID, version, out resolvedType, out string? failureReason))
			{
				throw new NotSupportedException($"Type of referenced assembly {Asm} {Ns} {Class} could not be resolved: {failureReason}");
			}
			
			nestedType = new(resolvedType, 0, "data", false);
			valueObj = resolvedType.CreateSerializableStructure();
			valueObj.Read(ref reader, version, flags, assemblyManager, true);
			return true;
		}
	}

	public void Write(AssetWriter writer)
	{
		if (Fields.Length == 0)
		{
			return;
		}
		
		ManagedVersionField.Write(writer, Type.Fields[0]);

		if (ManagedVersionField.AsInt64 == 1)
		{
			int idx = 0;
			foreach (SerializableStructure arrayEntry in Fields[1].AsAssetArray.Cast<SerializableStructure>())
			{
				arrayEntry.Fields[1].Write(writer, arrayEntry.Type.Fields[1]);

				if (NestedTypes[idx++] is SerializableType.Field nestedType)
				{
					arrayEntry.Fields[2].Write(writer, nestedType);
				}
			}
		}
		else if (ManagedVersionField.AsInt64 == 2)
		{
			int idx = 0;
			foreach (SerializableStructure arrayEntry in Fields[1].AsAssetArray.Cast<SerializableStructure>())
			{
				arrayEntry.Fields[0].Write(writer, arrayEntry.Type.Fields[0]);
				arrayEntry.Fields[1].Write(writer, arrayEntry.Type.Fields[1]);

				if (NestedTypes[idx++] is SerializableType.Field nestedType)
				{
					arrayEntry.Fields[2].Write(writer, nestedType);
				}
			}
		}
		else
		{
			throw new NotSupportedException($"Unsupported ManageReferences version {ManagedVersionField.AsInt64}");
		}
	}
	public override void WriteEditor(AssetWriter writer) => Write(writer);
	public override void WriteRelease(AssetWriter writer) => Write(writer);

	public override void WalkEditor(AssetWalker walker)
	{
		if (Fields.Length == 0)
		{
			if (walker.EnterAsset(this))
			{
				walker.ExitAsset(this);
			}
			return;
		}

		if (walker.EnterAsset(this))
		{
			if (walker.EnterField(this, "version"))
			{
				ManagedVersionField.WalkEditor(walker, Type.Fields[0]);
				walker.ExitField(this, "version");
			}
			
			walker.DivideAsset(this);
			if (walker.EnterField(this, "RefIds"))
			{
				if (ManagedVersionField.AsInt64 == 1)
				{
					if (walker.EnterList(Fields[1].AsAssetArray))
					{
						int idx = 0;
						foreach (SerializableStructure arrayEntry in Fields[1].AsAssetArray.Cast<SerializableStructure>())
						{
							if (idx > 0)
							{
								walker.DivideList(Fields[1].AsAssetArray);
							}

							if (walker.EnterAsset(arrayEntry))
							{
								if (walker.EnterField(arrayEntry.Fields[1].AsAsset, "type"))
								{
									arrayEntry.Fields[1].WalkEditor(walker, arrayEntry.Type.Fields[1]);
									walker.ExitField(arrayEntry.Fields[1].AsAsset, "type");
								}

								if (NestedTypes[idx] is SerializableType.Field nestedType)
								{
									walker.DivideAsset(arrayEntry);
									if (walker.EnterField(arrayEntry.Fields[2].AsAsset, "data"))
									{
										arrayEntry.Fields[2].WalkEditor(walker, nestedType);
										walker.ExitField(arrayEntry.Fields[2].AsAsset, "data");
									}
								}
								walker.ExitAsset(arrayEntry);
							}
							idx++;
						}

						walker.ExitList(Fields[1].AsAssetArray);
					}

				}
				else if (ManagedVersionField.AsInt64 == 2)
				{
					if (walker.EnterList(Fields[1].AsAssetArray))
					{
						int idx = 0;
						foreach (SerializableStructure arrayEntry in Fields[1].AsAssetArray.Cast<SerializableStructure>())
						{
							if (idx > 0)
							{
								walker.DivideList(Fields[1].AsAssetArray);
							}

							if (walker.EnterAsset(arrayEntry))
							{
								if (walker.EnterField(arrayEntry.Fields[0].AsAsset, "rid"))
								{
									arrayEntry.Fields[0].WalkEditor(walker, arrayEntry.Type.Fields[0]);
									walker.ExitField(arrayEntry.Fields[0].AsAsset, "rid");
								}

								walker.DivideAsset(arrayEntry);

								if (walker.EnterField(arrayEntry.Fields[1].AsAsset, "type"))
								{
									arrayEntry.Fields[1].WalkEditor(walker, arrayEntry.Type.Fields[1]);
									walker.ExitField(arrayEntry.Fields[1].AsAsset, "type");
								}

								if (NestedTypes[idx] is SerializableType.Field nestedType)
								{
									walker.DivideAsset(arrayEntry);
									if (walker.EnterField(arrayEntry.Fields[2].AsAsset, "data"))
									{
										arrayEntry.Fields[2].WalkEditor(walker, nestedType);
										walker.ExitField(arrayEntry.Fields[2].AsAsset, "data");
									}
								}
								walker.ExitAsset(arrayEntry);
							}
							idx++;
						}

						walker.ExitList(Fields[1].AsAssetArray);
					}
				}
				else
				{
					throw new NotSupportedException($"Unsupported ManageReferences version {ManagedVersionField.AsInt64}");
				}

				walker.ExitField(this, "RefIds");
			}
			walker.ExitAsset(this);
		}
	}
	//For now, only the editor version is implemented.
	public override void WalkRelease(AssetWalker walker) => WalkEditor(walker);
	public override void WalkStandard(AssetWalker walker) => WalkEditor(walker);

	public override IEnumerable<(string, PPtr)> FetchDependencies()
	{
		if (Fields.Length == 0)
		{
			yield break;
		}
		
		int idx = 0;
		foreach (SerializableStructure arrayEntry in Fields[1].AsAssetArray.Cast<SerializableStructure>())
		{
			if (NestedTypes[idx++] is SerializableType.Field nestedType)
			{
				foreach ((string, PPtr) pair in arrayEntry.Fields[2].FetchDependencies(nestedType))
				{
					yield return pair;
				}
			}
		}
	}

	public override string ToString() => Type.FullName;

	public int Depth { get; }
	public SerializableType Type { get; }
	public SerializableType.Field?[] NestedTypes { get; private set; }
	public SerializableValue[] Fields { get; private set; }

	public ref SerializableValue this[string name]
	{
		get
		{
			if (TryGetIndex(name, out int index))
			{
				return ref Fields[index];
			}
			throw new KeyNotFoundException($"Field {name} wasn't found in {Type.Name}");
		}
	}

	public bool ContainsField(string name) => TryGetIndex(name, out _);

	public bool TryGetField(string name, out SerializableValue field)
	{
		if (TryGetIndex(name, out int index))
		{
			field = Fields[index];
			return true;
		}
		field = default;
		return false;
	}

	public SerializableValue? TryGetField(string name)
	{
		if (TryGetIndex(name, out int index))
		{
			return Fields[index];
		}
		return null;
	}

	public bool TryGetIndex(string name, out int index)
	{
		for (int i = 0; i < Fields.Length; i++)
		{
			if (Type.Fields[i].Name == name)
			{
				index = i;
				return true;
			}
		}
		index = -1;
		return false;
	}

	public override void CopyValues(IUnityAssetBase? source, PPtrConverter converter)
	{
		CopyValues((SerializableRegistry?)source, converter);
	}

	public void CopyValues(SerializableRegistry? source, PPtrConverter converter)
	{
		if (source is null)
		{
			Reset();
			return;
		}
		if (source.Depth != Depth)
		{
			throw new ArgumentException($"Depth {source.Depth} doesn't match with {Depth}", nameof(source));
		}
		Version = source.Version;

		if (source.Type == Type)
		{
			if (source.Fields.Length == 0)
			{
				Fields = [];
				return;
			}

			if (Fields.Length == 0)
			{
				Fields = new SerializableValue[Type.Fields.Count];
			}

			Fields[0] = source.Fields[0];

			List<SerializableStructure> refIds = [];
			int idx = 0;
			foreach (SerializableStructure arrayEntry in source.Fields[1].AsAssetArray.Cast<SerializableStructure>())
			{
				SerializableStructure newEntry = new(SerializableRefObjType.Shared, 0);
				newEntry.Fields[0] = arrayEntry.Fields[0];
				newEntry.Fields[1].CopyValues(arrayEntry.Fields[1], 0, arrayEntry.Type.Fields[1], converter);
				if (source.NestedTypes[idx++] is SerializableType.Field nestedType)

				{
					newEntry.Fields[2].AsAsset = nestedType.Type.CreateSerializableStructure();
					newEntry.Fields[2].CopyValues(arrayEntry.Fields[2], 0, nestedType, converter);
				}
				else
				{
					newEntry.Fields[2].AsAsset = null!;
				}

				refIds.Add(newEntry);
			}

			Fields[1].AsAssetArray = refIds.ToArray();
			NestedTypes = (SerializableType.Field?[])source.NestedTypes.Clone();
		}
	}

	public SerializableRegistry DeepClone(PPtrConverter converter)
	{
		SerializableRegistry clone = new(Depth);
		clone.CopyValues(this, converter);
		return clone;
	}

	IUnityAssetBase IDeepCloneable.DeepClone(PPtrConverter converter) => DeepClone(converter);

	public override void Reset()
	{
		Fields = new SerializableValue[Type.Fields.Count];
		NestedTypes = [];
	}

	public void InitializeFields(UnityVersion version)
	{
		Version = version;
		if (Fields.Length > 0)
		{
			Fields[1].Initialize(version, 1, Type.Fields[1]);
		}
	}

	/// <summary>
	/// Unity has a maximum serialization depth to prevent infinite recursion in cyclic references.
	/// In Unity versions prior to 2020.2.0a21, this limit is 7. From 2020.2.0a21 onwards, the limit was increased to 10.
	/// </summary>
	/// <remarks>
	/// <see href="https://forum.unity.com/threads/serialization-depth-limit-and-recursive-serialization.1263599/"/><br/>
	/// <see href="https://forum.unity.com/threads/getting-a-serialization-depth-limit-7-error-for-no-reason.529850/"/><br/>
	/// <see href="https://forum.unity.com/threads/4-5-serialization-depth.248321/"/>
	/// </remarks>
	private static int GetMaxDepthLevel(UnityVersion version) => version.GreaterThanOrEquals(2020, 2, 0, UnityVersionType.Alpha, 21) ? 10 : 7;
}
