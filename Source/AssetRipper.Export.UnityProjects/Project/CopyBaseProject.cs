using AssetRipper.Export.Configuration;
using AssetRipper.Export.UnityProjects.Configuration;
using AssetRipper.Import.Logging;
using AssetRipper.Processing;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AssetRipper.Export.UnityProjects.Project
{
	public sealed class CopyBaseProject : IPostExporter
	{
		private static string RIFT_PROJECT_PATH = "Resources/BaseRiftProject.zip";

		public void DoPostExport(GameData gameData, FullConfiguration settings, FileSystem fileSystem)
		{
			List<string> scriptsToKeep = new List<string>()
			{
				"Assembly-CSharp",
				"Assembly-CSharp-firstpass",
				"ALINE",
				"andywiecko.BurstTriangulator",
				"AstarPathfindingProject",
				"BakeryRuntimeAssembly",
				"Clipper2Lib",
				"Coffee.SoftMaskForUGUI",
				"Coffee.SoftMaskForUGUI.R",
				"DarkMachineUI",
				"DOTween",
				"Drawing",
				"Facepunch.Steamworks.Win64",
				"Febucci.Attributes.Runtime",
				"Febucci.TextAnimator.Runtime",
				"Febucci.TextAnimator.TMP.Runtime",
				"PackageTools",
				"Pathfinding.Ionic.Zip.Reduced",
				"PrefabBaker",
				"SeppePeelman.EditorTools.SurfaceAlignTool",
				"Sirenix.OdinInspector.Attributes",
				"Sirenix.Serialization.Config",
				"Sirenix.Serialization",
				"Sirenix.Utilities",
				"WFowler1.BspImporter.Runtime",
				"XNode",
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
	}
}
