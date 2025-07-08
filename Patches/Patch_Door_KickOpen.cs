using EFT;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace tarkin.doordash.Patches
{
    internal class Patch_Door_KickOpen : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Door), nameof(Door.KickOpen), [typeof(Vector3), typeof(bool)]);
        }

        [PatchPrefix]
        private static bool PatchPrefix(Door __instance, Vector3 yourPosition)
        {
            if (Plugin.Enabled.Value && Random.value < Plugin.DislodgeChance.Value)
            {
                __instance.gameObject.AddComponent<PhysicalDoor>();
                return false;
            }

            return true;
        }
    }
}
