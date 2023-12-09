using System.Collections.Generic;
using UnityEngine;

namespace ModelReplacement
{
	public interface IBoneMap
	{
		IReadOnlyCollection<MappedBone> MappedBones { get; }
		IReadOnlyCollection<Transform> ModelBones { get; }
		bool CompletelyDestroyed { get; }
		Vector3 PositionOffset { get; }
		Transform RootBone { get; }
		Transform ItemHolder { get; }
		Vector3 ItemHolderPositionOffset { get; }
		



		MappedBone GetMappedBoneWithPlayerName(string playerTransformName);
		Transform GetMappedTransform(string playerTransformName);
		void MapBones(Transform[] playerBones, Transform[] modelBones);
		void SetBodyReplacement(BodyReplacement body);
		void UpdateItemHolderBoneAndOffset(Transform newRootBone, Vector3 offset);
		void UpdateMappedBone(string playerBoneString, string modelBoneString, Quaternion rotationOffset);
		void UpdateModelbones();
		void UpdateRootBoneAndOffset(Transform newRootBone, Vector3 offset);
	}

}