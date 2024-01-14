using ContainerResizer.Components;
using HarmonyLib;
using VLB;

namespace ContainerResizer.Patches
{
    public static class InventoryNavigatorPatches
    {
        [HarmonyPatch(typeof(InventoryNavigator), nameof(InventoryNavigator.Open))]
        public static class InventoryNavigatorOpen
        {
            public static void Prefix(InventoryNavigator __instance, MachineInstanceRef<ChestInstance> machineRef)
            {
                var inventorySizer = __instance.GetOrAddComponent<SliderComponent>();
                
                inventorySizer.OpenChest(machineRef, __instance);
            }
        }
        
        [HarmonyPatch(typeof(InventoryNavigator), nameof(InventoryNavigator.OnClose))]
        public static class InventoryNavigatorOnClose
        {
            public static void Postfix(InventoryNavigator __instance)
            {
                var inventorySizer = __instance.GetOrAddComponent<SliderComponent>();
                
                inventorySizer.CloseChest();
            }
        }
    }
}