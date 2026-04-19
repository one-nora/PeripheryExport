using AssetRipper.Export.Configuration;
using AssetRipper.Export.UnityProjects.Configuration;
using AssetRipper.Import.Logging;
using AssetRipper.Import.Structure.Assembly.Managers;
using AssetRipper.Processing;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using AsmResolver.DotNet;

namespace AssetRipper.Export.UnityProjects.Project
{
	public sealed class CopyBaseProject : IPostExporter
	{
		private static string RIFT_PROJECT_PATH = "Resources/BaseRiftProject.zip";

		public void DoPostExport(GameData gameData, FullConfiguration settings, FileSystem fileSystem)
		{
			DeleteScripts(settings);
			CopyOverPlugins(gameData, settings, fileSystem);
			
			string baseProjectPath = RIFT_PROJECT_PATH;
			if (!File.Exists(baseProjectPath))
			{
				Logger.Error($"Could not find the required file: {baseProjectPath}");
				throw new IOException($"Could not find the required file: {baseProjectPath}");
			}

			using (ZipArchive zip = new ZipArchive(File.Open(baseProjectPath, FileMode.Open, FileAccess.Read), ZipArchiveMode.Read))
			{
				zip.ExtractToDirectory(settings.ProjectRootPath, true);
			}
			
			ReplaceShaderCopiesWithZipFiles(baseProjectPath, settings, fileSystem);
			CopyModsFolder(gameData, settings, fileSystem);
		}

		private void DeleteScripts(FullConfiguration settings)
		{
			List<string> scriptsToKeep = new List<string>()
			{
				"Assembly-CSharp",
				"Assembly-CSharp-firstpass",
				"BakeryRuntimeAssembly",
				"DarkMachineUI",
				"DOTween",
				"Febucci.Attributes.Runtime",
				"Febucci.TextAnimator.Runtime",
				"Febucci.TextAnimator.TMP.Runtime",
				"PrefabBaker",
			};
			
			string scriptsDirectory = Path.Combine(settings.AssetsPath, "Scripts");
            if (Directory.Exists(scriptsDirectory))
            {
            	foreach (string dir in Directory.GetDirectories(scriptsDirectory))
            	{
            		if (!scriptsToKeep.Contains(Path.GetFileName(dir)))
            			Directory.Delete(dir, true);
            	}
            }
		}

		private void CopyOverPlugins(GameData gameData, FullConfiguration settings, FileSystem fileSystem)
		{
			List<string> pluginsToCopy = new List<string>()
			{
				"ALINE",
				"andywiecko.BurstTriangulator",
				"Clipper2Lib",
				"Coffee.SoftMaskForUGUI",
				"Coffee.SoftMaskForUGUI.R",
				"Drawing",
				"Facepunch.Steamworks.Win64",
				"Pathfinding.Ionic.Zip.Reduced",
				"SeppePeelman.EditorTools.SurfaceAlignTool",
				"Sirenix.OdinInspector.Attributes",
				"Sirenix.Serialization.Config",
				"Sirenix.Serialization",
				"Sirenix.Utilities",
				"WFowler1.BspImporter.Runtime",
				"XNode",
			};
			
			IAssemblyManager assemblyManager = gameData.AssemblyManager;
			AssemblyDefinition[] assemblies = assemblyManager.GetAssemblies().ToArray();
			if (assemblies.Length != 0)
			{
				string pluginsDirectory = fileSystem.Path.Join(settings.AssetsPath, "Plugins");
				if (!Directory.Exists(pluginsDirectory))
					Directory.CreateDirectory(pluginsDirectory);
				
				foreach (AssemblyDefinition assembly in assemblies)
				{
					if (pluginsToCopy.Contains(assembly.Name))
					{
						string filepath = fileSystem.Path.Join(pluginsDirectory, SpecialFileNames.AddAssemblyFileExtension(assembly.Name!));
						assemblyManager.SaveAssembly(assembly, filepath, fileSystem);
					}
				}
			}
		}
		
		private void ReplaceShaderCopiesWithZipFiles(string zipPath, FullConfiguration settings, FileSystem fileSystem)
		{
			// Load all .shader files from Assets/Shader inside the zip
			// Then replace the copies like xshade_0.shader with the original copy while keeping the name correctly
			Dictionary<string, byte[]> zipShaders = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

			using (ZipArchive archive = ZipFile.OpenRead(zipPath))
			{
				foreach (ZipArchiveEntry entry in archive.Entries)
				{
					if (!entry.FullName.StartsWith("Assets/Shader/", StringComparison.OrdinalIgnoreCase))
						continue;

					if (!entry.FullName.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
						continue;

					string fileName = Path.GetFileName(entry.FullName);
					using (Stream s = entry.Open())
					using (MemoryStream ms = new MemoryStream())
					{
						s.CopyTo(ms);
						zipShaders[fileName] = ms.ToArray();
					}
				}
			}

			string shadersDirectory = fileSystem.Path.Join(settings.AssetsPath, "Shader");
			if (!Directory.Exists(shadersDirectory))
				Directory.CreateDirectory(shadersDirectory);
			
			// Read target folder and replace xshader_0.shader with xshader.shader from zip
			foreach (string filePath in Directory.EnumerateFiles(shadersDirectory, "*.shader", SearchOption.TopDirectoryOnly))
			{
				string fileName = Path.GetFileName(filePath);

				string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

				// Find last underscore
				int underscoreIndex = nameWithoutExt.LastIndexOf('_');
				if (underscoreIndex == -1)
					continue;

				// Check if suffix is a number
				string suffix = nameWithoutExt.Substring(underscoreIndex + 1);
				if (!int.TryParse(suffix, out _))
					continue;

				// Base name (before _number)
				string baseName = nameWithoutExt.Substring(0, underscoreIndex);
				string sourceName = baseName + ".shader";

				if (zipShaders.TryGetValue(sourceName, out byte[] data))
				{
					File.WriteAllBytes(filePath, data);
				}
			}
		}

		private void CopyModsFolder(GameData gameData, FullConfiguration settings, FileSystem fileSystem)
		{
			var modsDirectory = fileSystem.Path.Join(gameData.PlatformStructure.GameDataPath, "Mods");
			var destDirectory = fileSystem.Path.Join(settings.AssetsPath, "Mods");

			if (!Directory.Exists(modsDirectory))
				return;

			if (!Directory.Exists(destDirectory))
				Directory.CreateDirectory(destDirectory);
			
			foreach (string filePath in Directory.EnumerateFiles(modsDirectory, "*", SearchOption.AllDirectories))
			{
				string relativePath = Path.GetRelativePath(modsDirectory, filePath);
				string destinationPath = Path.Combine(destDirectory, relativePath);

				string? destinationFolder = Path.GetDirectoryName(destinationPath);
				if (!string.IsNullOrEmpty(destinationFolder))
					Directory.CreateDirectory(destinationFolder);

				File.Copy(filePath, destinationPath, true);	
			}
		}
	}
}
