using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ModelReplacement
{
	public class MappedBone
	{
		public string playerBoneString;
		public string modelBoneString;
		public Quaternion rotationOffset = Quaternion.identity;

		public Transform playerTransform;
		public Transform modelTransform;

		public List<string> additionalVars = new List<string>();

		public MappedBone(string playerBoneString, string modelBoneString, Quaternion rotationOffset, Transform playerTransform, Transform modelTransform)
		{
			this.playerBoneString = playerBoneString;
			this.modelBoneString = modelBoneString;
			this.rotationOffset = rotationOffset;
			this.playerTransform = playerTransform;
			this.modelTransform = modelTransform;
		}

		public MappedBone(List<string> vars, Transform player, Transform model)
		{
			playerTransform = player;
			modelTransform = model;

			int varsCount = vars.Count;
			if (varsCount >= 2)
			{
				playerBoneString = vars[0];
				modelBoneString = vars[1];

			}
			if (varsCount >= 6)
			{
				float x, y, z, w;
				try
				{

					x = float.Parse(vars[2]);
					y = float.Parse(vars[3]);
					z = float.Parse(vars[4]);
					w = float.Parse(vars[5]);
					rotationOffset = new Quaternion(x, y, z, w);
					//Console.WriteLine($"Setting quaternion for {modelBoneString} xyzw({x},{y}, {z}, {w})");
				}
				catch (Exception e)
				{
					ModelReplacementAPI.Instance.Logger.LogError($"could not parse rotation offset for player bone {playerBoneString} xyzw({vars[2]},{vars[3]},{vars[4]},{vars[5]})");
				}

			}
			if (varsCount > 6)
			{
				for (int i = 6; i < varsCount; i++)
				{
					additionalVars.Add(vars[i]);
				}
			}



		}

		public bool Update()
		{
			if ((modelTransform == null) || (playerTransform == null))
			{
				ModelReplacementAPI.Instance.Logger.LogError($"Could not Update bone, model or player transform is null. Destroying MappedBone ({modelBoneString})");

				return true;
			}
			try
			{
				modelTransform.rotation = new Quaternion(playerTransform.rotation.x, playerTransform.rotation.y, playerTransform.rotation.z, playerTransform.rotation.w);
				modelTransform.rotation *= rotationOffset;
			}
			catch
			{
				ModelReplacementAPI.Instance.Logger.LogError($"Could not Update bones for {playerTransform.name} to {modelTransform.name} ");
				return true;
			}
			return false;


		}



	}
}
