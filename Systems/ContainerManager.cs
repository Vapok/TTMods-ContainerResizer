using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ContainerResizer.Network;
using ContainerResizer.Objects;
using UnityEngine;

namespace ContainerResizer.Systems;

public static class ContainerManager
{
    private static readonly Dictionary<uint, ContainerRecord> _containerRegistry = new();
    private static string _saveFolder = Path.Combine(Application.persistentDataPath,nameof(ContainerResizer));

    public static bool TryGetContainer(uint instanceId, out ContainerRecord record)
    {
        record = null;
        if (!_containerRegistry.ContainsKey(instanceId))
            return false;

        record = _containerRegistry[instanceId];
        return true;
    }

    public static bool AddContainer(uint instanceId, int index, ChestInstance chest, out ContainerRecord record)
    {
        record = null;

        if (_containerRegistry.ContainsKey(instanceId))
        {
            record = _containerRegistry[instanceId];
            return false;
        }
        
        record = new ContainerRecord(instanceId, index, chest);
        _containerRegistry.Add(instanceId,record);
        return true;
    }

    public static bool RemoveContainer(uint instanceId)
    {
        if (_containerRegistry.ContainsKey(instanceId))
            _containerRegistry.Remove(instanceId);

        return !_containerRegistry.ContainsKey(instanceId);
    }

    public static bool IsInContainerRegistry(uint instanceId)
    {
        return _containerRegistry.ContainsKey(instanceId);
    }

    public static int RegistryCount()
    {
        return _containerRegistry.Count;
    }

    public static bool TryResizeChest(uint instanceId, int newSlotSize, out int newMinimumSlots)
    {
        var result = false;
        newMinimumSlots = 0;
        
        if (TryGetContainer(instanceId, out var record))
             result = TryResizeChest(record.IndexId, newSlotSize, out var newminimumSlots);
        return result;
    }

    public static bool TryResizeChest(int machineIndex, int newSlotSize, out int newMinimumSlots)
    {
        var manager = MachineManager.instance.GetMachineList<ChestInstance, ChestDefinition>(MachineTypeEnum.Chest);
        var chest = manager.GetIndex(machineIndex);

        var result = TryResizeChest(chest, newSlotSize, out newMinimumSlots);
        
        return result;
    }
    public static bool TryResizeChest(ChestInstance chest, int newSlotSize, out int newMinimumSlots)
    {
        newMinimumSlots = 0;
        if (chest.commonInfo.instanceId == 0)
            return false;
        
        if (!TryGetContainer(chest.commonInfo.instanceId, out var record))
        {
            AddContainer(chest.commonInfo.instanceId, chest.commonInfo.index, chest, out record);
        }
        record.SetSlotCount(newSlotSize);
        
        if (chest.commonInfo.inventories[0].numSlots == newSlotSize)
            return false;
            
        chest.commonInfo.inventories[0].numSlots = newSlotSize;
        Array.Resize(ref chest.commonInfo.inventories[0].myStacks, chest.commonInfo.inventories[0].numSlots);
        Array.Resize(ref chest.commonInfo.inventories[0].customSlotOverrides, chest.commonInfo.inventories[0].numSlots);
        chest.commonInfo.inventories[0].SortAndConsolidate();
            
        newMinimumSlots = chest.commonInfo.inventories[0].numSlots - chest.commonInfo.inventories[0].GetNumberOfEmptySlots();
        
        if (ContainerNetwork.IsServer)
            ContainerNetwork.Instance.UpdateContainerFromServer(record.InstanceId,record.SlotCount);
        
        return true;
    }

    public static List<ContainerRecord> ExportRegistry()
    {
        var exportList = new List<ContainerRecord>();

        foreach (var record in _containerRegistry)
        {
            exportList.Add(record.Value);
        }

        return exportList;
    }
    
    public static void ImportRegistry(List<ContainerRecord> importedRecords)
    {
        _containerRegistry.Clear();
        foreach (var record in importedRecords)
        {
            _containerRegistry.Add(record.InstanceId, record);
        }
    }
    
    public static void ImportRegistry(List<string> importedRecords)
    {
        _containerRegistry.Clear();
        foreach (var recordString in importedRecords)
        {
            var record = new ContainerRecord(recordString);
            _containerRegistry.Add(record.InstanceId, record);
        }
    }
    
    public static void SaveRegistry(string worldName)
    {
        if (_containerRegistry == null)
            return;
            
        var fileName = Path.Combine(_saveFolder, $"{worldName}.bin");
        var bf = new BinaryFormatter();
        var fs = File.Create(fileName);
            
        ContainerResizer.Log.LogDebug($"Saving Container Registry to {worldName}.bin");
            
        bf.Serialize(fs,_containerRegistry);
        fs.Close();
    }

    public static void LoadRegistry(string worldName)
    {
        var fileName = Path.Combine(_saveFolder, $"{worldName}.bin");
            
        if (!File.Exists(fileName))
            return;
            
        ContainerResizer.Log.LogDebug($"Loading Container Registry from {worldName}.bin");
        try
        {
            var bf = new BinaryFormatter();
            var fs = File.Open(fileName,FileMode.Open);
            var tempCounts = bf.Deserialize(fs) as Dictionary<uint, ContainerRecord>;
            fs.Close();

            if (tempCounts == null)
                return;
        
            foreach (var keyVal in tempCounts)
            {
                _containerRegistry[keyVal.Key] = keyVal.Value;
            }

        }
        catch (Exception e)
        {
            ContainerResizer.Log.LogError($"Unable to Load Container Registry: {e.Message}");
        }
    }

}