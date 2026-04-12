using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace AssetRipper.PackageGuid
{
	public class FileData
	{
		public string FileName { get; set; }
		public string Guid { get; set; }
	}

	public class ShaderData
	{
		public string ShaderName { get; set; }
		public string Guid { get; set; }
	}

	public class ScriptData
	{
		public string Namespace { get; set; }
		public string ClassName { get; set; }
		public string Guid { get; set; }
	}

	public class PackageGuids
	{
		public List<FileData> Files;
		public List<ShaderData> Shaders;
		public List<ScriptData> Scripts;

		public static PackageGuids FromJson(string jsonText)
		{
			PackageGuids data = new PackageGuids();
			data.Files = new List<FileData>();
			data.Shaders = new List<ShaderData>();
			data.Scripts = new List<ScriptData>();

            JObject obj = JObject.Parse(jsonText);
            foreach (var token in obj["Files"].Children())
			{
				FileData fileData = new FileData();
				fileData.FileName = token["FileName"].Value<string>();
				fileData.Guid = token["Guid"].Value<string>();

				data.Files.Add(fileData);
			}
            foreach (var token in obj["Shaders"].Children())
            {
                ShaderData shaderData = new ShaderData();
                shaderData.ShaderName = token["ShaderName"].Value<string>();
                shaderData.Guid = token["Guid"].Value<string>();

                data.Shaders.Add(shaderData);
            }
            foreach (var token in obj["Scripts"].Children())
            {
                ScriptData scriptData = new ScriptData();
                scriptData.Namespace = token["Namespace"].Value<string>();
                scriptData.ClassName = token["ClassName"].Value<string>();
                scriptData.Guid = token["Guid"].Value<string>();

                data.Scripts.Add(scriptData);
            }

            return data;
        }
	}
}
