using ContainerResizer.Systems;
using HarmonyLib;

namespace ContainerResizer.Patches;

public static class MachineDefinitionPatches
{
    [HarmonyPatch(typeof(MachineDefinition<ChestInstance, ChestDefinition>),
        nameof(MachineDefinition<ChestInstance, ChestDefinition>.BuildTypedInstance)
        ,new []{typeof(ChestInstance), typeof(bool), typeof(SaveState.BuildableSaveData)}
        , new [] {ArgumentType.Ref,ArgumentType.Normal,ArgumentType.Ref})]
    public static class MachineDefinitionBuildTypedInstancePatch
    {
        public static void Postfix(MachineDefinition<ChestInstance, ChestDefinition> __instance, ref ChestInstance newInstance)
        {
            var chest = newInstance;
            var gameObject = chest.commonInfo.refGameObj;
            var index = __instance.myMachineList.curCount - 1;
            
            
            if (!ContainerManager.TryGetContainer(chest.commonInfo.instanceId,out var record))
            {
                //Add to Registry
                ContainerManager.AddContainer(chest.commonInfo.instanceId, index, chest, out record);

                record.OriginalSlotCount = __instance.inventorySizes[0].x * __instance.inventorySizes[0].y;
            }

            //Adjust Container to Record Properties
            ContainerManager.TryResizeChest(chest, record.SlotCount, out _);
        }
    }

    [HarmonyPatch(typeof(MachineDefinition<ChestInstance, ChestDefinition>),
        nameof(MachineDefinition<ChestInstance, ChestDefinition>.OnDeconstruct)
        ,new []{typeof(ChestInstance)}
        , new [] {ArgumentType.Ref})]
    public static class MachineDefinitionOnDeconstruct
    {
        public static void Postfix(ref ChestInstance erasedInstance)
        {
            ContainerManager.RemoveContainer(erasedInstance.commonInfo.instanceId);
        }
    }
}