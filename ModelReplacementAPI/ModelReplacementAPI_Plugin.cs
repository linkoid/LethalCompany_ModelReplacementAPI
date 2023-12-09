﻿using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
//using System.Numerics;
//using static System.Net.Mime.MediaTypeNames;
//using System.Numerics;

namespace ModelReplacement
{



    [BepInPlugin("meow.ModelReplacementAPI", "ModelReplacementAPI", "1.2.4")]
    [BepInDependency("me.swipez.melonloader.morecompany", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("verity.3rdperson", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("LCThirdPerson", BepInDependency.DependencyFlags.SoftDependency)]
    public class ModelReplacementAPI : BaseUnityPlugin
    {

        private void Awake()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource("ModelReplacementAPI");
            // Plugin startup logic
            bool flag = ModelReplacementAPI.Instance == null;
            if (flag)
            {
                ModelReplacementAPI.Instance = this;
            }

            moreCompanyPresent = Chainloader.PluginInfos.ContainsKey("me.swipez.melonloader.morecompany");
            thirdPersonPresent = Chainloader.PluginInfos.ContainsKey("verity.3rdperson");
            LCthirdPersonPresent = Chainloader.PluginInfos.ContainsKey("LCThirdPerson");


            Harmony harmony = new Harmony("meow.ModelReplacementAPI");
            harmony.PatchAll();
            Logger.LogInfo($"Plugin {"meow.ModelReplacementAPI"} is loaded!");
        }
        //soft dependencies
        public static bool moreCompanyPresent;
        public static bool thirdPersonPresent;
        public static bool LCthirdPersonPresent;


        public static ModelReplacementAPI Instance;
        public new ManualLogSource Logger;
        private static Dictionary<string, Type> RegisteredModelReplacements = new Dictionary<string, Type>();

        /// <summary>
        /// Registers a specified body replacement class to a specified suit name. All players wearing a suit with the specified name will have their model replaced. 
        /// </summary>
        /// <param name="suitNameToReplace"></param>
        /// <param name="type"></param>
        public static void RegisterSuitModelReplacement(string suitNameToReplace, Type type)
        {
            suitNameToReplace = suitNameToReplace.ToLower().Replace(" ", "");
            if (!(type.IsSubclassOf(typeof(BodyReplacement))))
            {
                Instance.Logger.LogError($"Cannot register body replacement type {type.Name}, must inherit from BodyReplacementBase");
                return;
            }
            if (RegisteredModelReplacements.ContainsKey(suitNameToReplace))
            {
                Instance.Logger.LogError($"Cannot register body replacement type {type.Name}, suit name to replace {suitNameToReplace} is already registered.");
                return;
            }

            Instance.Logger.LogInfo($"Registering body replacement type {type.Name} to suit name {suitNameToReplace}.");
            
            RegisteredModelReplacements.Add(suitNameToReplace, type);
        }

        /// <summary>
        /// Sets a body replacement for the specified player, removes existing body replacement if it is a different type than the specified
        /// </summary>
        /// <param name="player"></param>
        /// <param name="type">typeof body replacement class. Must inherit from BodyReplacementBase</param>
        public static void SetPlayerModelReplacement(PlayerControllerB player, Type type)
        {
            if (!(type.IsSubclassOf(typeof(BodyReplacement))))
            {
                Instance.Logger.LogError($"Cannot set body replacement of type {type.Name}, must inherit from BodyReplacementBase");
                return;
            }
            var a = player.thisPlayerBody.gameObject.GetComponent<BodyReplacement>();
            if (a != null)
            {
                if (a.GetType() == type)
                {
                    return; //No need to add a body replacement, there is already a body replacement of this type
                }
                if (a)
                {
                    Destroy(a); //Destroy the existing body replacement
                }
            }

            player.thisPlayerBody.gameObject.AddComponent(type);
        }
        /// <summary>
        /// Removes any existing body replacement 
        /// </summary>
        /// <param name="player"></param>
        public static void RemovePlayerModelReplacement(PlayerControllerB player)
        {
            var a = player.thisPlayerBody.gameObject.GetComponent<BodyReplacement>();
            if (a)
            {
                Destroy(a);
            }
        }


        [HarmonyPatch(typeof(GrabbableObject))]
        public class LocateHeldObjectsOnModelReplacementPatch
        {

            [HarmonyPatch("LateUpdate")]
            [HarmonyPostfix]
            public static void LateUpdatePatch(ref GrabbableObject __instance)
            {
                if (__instance.parentObject == null) { return; }
                if (__instance.playerHeldBy == null) { return; }
                var a = __instance.playerHeldBy.gameObject.GetComponent<BodyReplacement>();
                if (a == null) { return; }
                if (a.RenderBodyReplacement())
                {
                    if(a.renderLocalDebug && !a.renderModel) { return; }

                    Transform parentObject = a.Map.ItemHolder;
                    Vector3 positionOffset = a.Map.ItemHolderPositionOffset;

                    __instance.transform.rotation = parentObject.rotation;
                    __instance.transform.Rotate(__instance.itemProperties.rotationOffset);
                    __instance.transform.position = parentObject.position;
                    Vector3 vector = __instance.itemProperties.positionOffset + positionOffset;
                    vector = parentObject.rotation * vector;
                    __instance.transform.position += vector;


                }

            }
        }



        [HarmonyPatch(typeof(StartOfRound))]
        public class RepairBrokenBodyReplacementsPatch
        {

            [HarmonyPatch("ReviveDeadPlayers")]
            [HarmonyPostfix]
            public static void ReviveDeadPlayersPatch(ref StartOfRound __instance)
            {

                foreach (var item in __instance.allPlayerScripts)
                {
                    if (!item.isPlayerDead) { continue; } //player isn't dead
                    if (item.gameObject.GetComponent<BodyReplacement>() == null) { continue; } //player doesn't have a body replacement

                    Console.WriteLine($"Reinstantiating model replacement for {item.playerUsername} ");
                    Type BodyReplacementType = item.gameObject.GetComponent<BodyReplacement>().GetType();
                    Destroy(item.gameObject.GetComponent<BodyReplacement>());
                    item.gameObject.AddComponent(BodyReplacementType);
                }
            }
        }



        [HarmonyPatch(typeof(PlayerControllerB))]
        public class SetRegisteredBodyReplacements
        {

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            public static void UpdatePatch(ref PlayerControllerB __instance)
            {
                try
                {
                    //return;
                    if (__instance.playerSteamId == 0) { return; }
                    int suitID = __instance.currentSuitID;
                    string suitName = StartOfRound.Instance.unlockablesList.unlockables[suitID].unlockableName;
                    suitName = suitName.ToLower().Replace(" ", "");
                    if (RegisteredModelReplacements.ContainsKey(suitName))
                    {
                        Type type = RegisteredModelReplacements[suitName];
                        SetPlayerModelReplacement(__instance, type);
                    }
                    else
                    {
                        RemovePlayerModelReplacement(__instance);
                    }
                }
                catch (Exception e) { }


            }
        }




    }
}