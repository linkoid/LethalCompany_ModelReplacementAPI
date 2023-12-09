using ModelReplacement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ModelReplacement
{
	[Obsolete($"Use {nameof(HumanoidBodyReplacement)} instead", true)]
	public abstract class BodyReplacementBase : BodyReplacement
	{
		public string boneMapJsonStr;
		public string jsonPath;

		/// <summary>
		/// the name of this model replacement's bone mapping .json. Can be anywhere in the bepinex/plugins folder
		/// </summary>
		public abstract string boneMapFileName { get; }

		protected sealed override IBoneMap LoadBoneMap()
		{
			if (string.IsNullOrEmpty(jsonPath))
			{
				//Get all .jsons in plugins and select the matching boneMap.json, deserialize bone map
				string pluginsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				while (true)
				{
					string folder = new DirectoryInfo(pluginsPath).Name;
					if (folder == "plugins") { break; }
					pluginsPath = Path.Combine(pluginsPath, "..");
				}
				string[] allfiles = Directory.GetFiles(pluginsPath, "*.json", SearchOption.AllDirectories);
				jsonPath = allfiles.Where(f => Path.GetFileName(f) == boneMapFileName).First();
				boneMapJsonStr = File.ReadAllText(jsonPath);
			}
			

			return BoneMap.DeserializeFromJson(boneMapJsonStr);
		}
	}
}
