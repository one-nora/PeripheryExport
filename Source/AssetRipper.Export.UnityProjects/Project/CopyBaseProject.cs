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
				"DarkMachineUI",
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
					Logger.Info(LogCategory.Export, $"{assembly.Name}");
					if (pluginsToCopy.Contains(assembly.Name))
					{
						string filepath = fileSystem.Path.Join(pluginsDirectory, SpecialFileNames.AddAssemblyFileExtension(assembly.Name!));
						assemblyManager.SaveAssembly(assembly, filepath, fileSystem);
					}
				}
			}
		}
	}
}
