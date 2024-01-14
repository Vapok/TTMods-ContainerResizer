using ContainerResizer.Network;
using HarmonyLib;
using JetBrains.Annotations;
using Mirror;
using VLB;

namespace ContainerResizer.Patches
{
    public static class NetworkIdentityPatches
    {
        [HarmonyPatch(typeof(NetworkIdentity), "Awake")]
        public static class NetworkIdentityAwakeChanges
        {
            [UsedImplicitly]
            public static void Prefix(NetworkIdentity __instance)
            {
                __instance.GetOrAddComponent<ContainerNetwork>();
            }
        }
    }
}