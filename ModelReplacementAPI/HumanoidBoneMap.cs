using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ModelReplacement
{
	[JsonObject]
	public class HumanoidBoneMap : IBoneMap
	{
		[JsonProperty] private List<List<string>> boneMap = new List<List<string>>();
		[JsonProperty] private List<float> positionOffset = new List<float>();
		[JsonProperty] private List<float> itemHolderPositionOffset = new List<float>();
		[JsonProperty] private string itemHolderBone = "";
		[JsonProperty] private string rootBone = "";


		public IReadOnlyCollection<MappedBone> MappedBones => mappedBones;
		[JsonIgnore] private List<MappedBone> mappedBones = new List<MappedBone>();
		[JsonIgnore] public Vector3 PositionOffset { get; private set; } = Vector3.zero;
		[JsonIgnore] public Vector3 ItemHolderPositionOffset { get; private set; } = Vector3.zero;
		[JsonIgnore] public Transform ItemHolder { get; private set; } = null;
		[JsonIgnore] public Transform RootBone { get; private set; } = null;
		[JsonIgnore] protected IReadOnlyCollection<Transform> PlayerBoneList { get; private set; } = null;
		[JsonIgnore] public IReadOnlyCollection<Transform> ModelBones { get; private set; } = null;

		[JsonIgnore] private BodyReplacement replacementBase;



		public static BoneMap DeserializeFromJson(string jsonStr)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject<BoneMap>(jsonStr);
		}

		public string SerializeToJsonString()
		{
			positionOffset = new List<float> { PositionOffset.x, PositionOffset.y, PositionOffset.z };
			itemHolderPositionOffset = new List<float> { ItemHolderPositionOffset.x, ItemHolderPositionOffset.y, ItemHolderPositionOffset.z };

			boneMap.Clear();
			foreach (var item in MappedBones)
			{
				List<string> listStr = new List<string>();
				listStr.Add(item.playerBoneString);
				listStr.Add(item.modelBoneString);
				if (item.rotationOffset != Quaternion.identity)
				{
					listStr.Add(item.rotationOffset.x.ToString());
					listStr.Add(item.rotationOffset.y.ToString());
					listStr.Add(item.rotationOffset.z.ToString());
					listStr.Add(item.rotationOffset.w.ToString());
					if (item.additionalVars.Count > 0)
					{
						foreach (var item1 in item.additionalVars)
						{
							listStr.Add(item1);
						}
					}
				}

				boneMap.Add(listStr);
			}


			return Newtonsoft.Json.JsonConvert.SerializeObject(this);
		}

		public void SetBodyReplacement(BodyReplacement body)
		{
			replacementBase = body;
		}

		public void MapBones(Transform[] playerBones, Transform[] modelBones)
		{
			PlayerBoneList = playerBones;
			ModelBones = modelBones;

			//Set ragdoll bone
			if (!boneMap.Where(x => x[0] == "PlayerRagdoll(Clone)").Any())
			{
				if (boneMap.Where(x => x[0] == "spine").Any())
				{
					List<string> ragdollSpineBonevars = new List<string>();
					List<string> spineVars = boneMap.Where(x => x[0] == "spine").First();
					for (int i = 0; i < spineVars.Count(); i++)
					{
						if (i == 0)
						{
							ragdollSpineBonevars.Add("PlayerRagdoll(Clone)");
						}
						else
						{
							ragdollSpineBonevars.Add(spineVars[i]);
						}
					}


					boneMap.Add(ragdollSpineBonevars);
				}

			}

			mappedBones.Clear();
			ItemHolder = null;
			if (positionOffset.Count == 3)
			{
				PositionOffset = new Vector3(positionOffset[0], positionOffset[1], positionOffset[2]);
			}
			if (itemHolderPositionOffset.Count == 3)
			{
				ItemHolderPositionOffset = new Vector3(itemHolderPositionOffset[0], itemHolderPositionOffset[1], itemHolderPositionOffset[2]);
			}

			foreach (var vars in boneMap)
			{
				string playerBone = vars[0];
				string modelBone = vars[1];

				if (modelBone == "") { continue; }
				if (!modelBones.Any(x => x.name == modelBone))
				{
					ModelReplacementAPI.Instance.Logger.LogWarning($"No bone in model with name ({modelBone})");
					continue;
				}
				if (!playerBones.Any(x => x.name == playerBone))
				{
					ModelReplacementAPI.Instance.Logger.LogWarning($"No bone in player with name ({playerBone})");
					continue;
				}

				Transform playerTransform = playerBones.Where(x => x.name == playerBone).First();
				Transform modelTransform = modelBones.Where(x => x.name == modelBone).First();

				mappedBones.Add(new MappedBone(vars, playerTransform, modelTransform));

			}

			ItemHolder = modelBones.Where(x => x.name == itemHolderBone).First();
			RootBone = modelBones.Where(x => x.name == rootBone).First();
		}

		public void UpdateModelbones()
		{
			mappedBones.ForEach(x =>
			{
				bool destroyMappedBone = x.Update();

			});

			List<MappedBone> destroyBones = new List<MappedBone>();
			mappedBones.ForEach(x =>
			{
				bool destroyMappedBone = x.Update();
				if (destroyMappedBone) { destroyBones.Add(x); }
			});
			destroyBones.ForEach(x => { mappedBones.Remove(x); });
		}

		bool IBoneMap.CompletelyDestroyed
		{
			get
			{
				if ((mappedBones.Count() == 0) || RootBone == null)
				{
					Console.WriteLine("bone Map destroyed");
					return true;
				}
				return false;
			}
		}

		public Transform GetMappedTransform(string playerTransformName)
		{
			var a = MappedBones.Where(x => x.playerBoneString == playerTransformName);

			if (a.Any())
			{
				return a.First().modelTransform;
			}
			ModelReplacementAPI.Instance.Logger.LogWarning($"No mapped bone with player bone name {playerTransformName}");
			return null;
		}

		public MappedBone GetMappedBoneWithPlayerName(string playerTransformName)
		{
			var a = MappedBones.Where(x => x.playerBoneString == playerTransformName);

			if (a.Any())
			{
				return a.First();
			}
			ModelReplacementAPI.Instance.Logger.LogWarning($"No mapped bone with player bone name {playerTransformName}");
			return null;
		}

		public void UpdateMappedBone(string playerBoneString, string modelBoneString, Quaternion rotationOffset)
		{
			var oldMapped = MappedBones.Where(x => x.playerBoneString == playerBoneString);
			if (oldMapped.Any())
			{
				var a = GetMappedBoneWithPlayerName(playerBoneString);
				a.modelBoneString = modelBoneString;
				a.rotationOffset = rotationOffset;

				var b = ModelBones.Where(x => x.name == modelBoneString);
				if (b.Any()) { a.modelTransform = b.First(); }

			}
			else
			{
				var a = PlayerBoneList.Where(x => x.name == playerBoneString);
				var b = ModelBones.Where(x => x.name == modelBoneString);

				Transform plTransform = null;
				Transform mdTransform = null;

				if (a.Any()) { plTransform = a.First(); }
				if (b.Any()) { mdTransform = b.First(); }

				MappedBone newMB = new MappedBone(playerBoneString, modelBoneString, rotationOffset, plTransform, mdTransform);
				mappedBones.Add(newMB);

			}

		}

		public void UpdateRootBoneAndOffset(Transform newRootBone, Vector3 offset)
		{
			if (newRootBone != RootBone)
			{
				RootBone.parent = null;

				RootBone = newRootBone;
				rootBone = newRootBone.name;
			}

			PositionOffset = offset;
			replacementBase.ReparentModel();

		}

		public void UpdateItemHolderBoneAndOffset(Transform newRootBone, Vector3 offset)
		{
			ItemHolder = newRootBone;
			itemHolderBone = newRootBone.name;
			ItemHolderPositionOffset = offset;
			replacementBase.flagReparentObject = true;
		}
	}
}
