using System;

namespace ContainerResizer.Objects;

[Serializable]
public class ContainerRecord
{
    private uint _instanceId;
    private int _indexId;
    private int _slotCount;
    private int _originalSlotCount;
    public bool AdjustedSlotCount => SlotCount != OriginalSlotCount;

    public int OriginalSlotCount
    {
        get => _originalSlotCount;
        set => _originalSlotCount = value;
    }

    public int SlotCount => _slotCount;

    public int IndexId => _indexId;

    public uint InstanceId => _instanceId;

    public ContainerRecord(ChestInstance chest)
    {
        _instanceId = chest.commonInfo.instanceId;
        _indexId = chest.commonInfo.index;
        _slotCount = chest.commonInfo.inventories[0].numSlots;
        OriginalSlotCount = chest.commonInfo.inventories[0].numSlots;
    }

    public ContainerRecord(uint instanceID, int index, ChestInstance chest)
    {
        _instanceId = instanceID;
        _indexId = index;
        _slotCount = chest.commonInfo.inventories[0].numSlots;
        OriginalSlotCount = chest.commonInfo.inventories[0].numSlots;
    }

    public ContainerRecord(uint instanceID, int index, int slotCount, int originalSlotCount)
    {
        _instanceId = instanceID;
        _indexId = index;
        _slotCount = slotCount;
        OriginalSlotCount = originalSlotCount;
    }

    public ContainerRecord(string csvValue)
    {
        var values = csvValue.Split(',');
        _instanceId = uint.Parse(values[0]);
        _indexId = int.Parse(values[1]);
        _slotCount = int.Parse(values[2]);
        OriginalSlotCount = int.Parse(values[3]);
    }

    public void SetSlotCount(int newSlotCount)
    {
        _slotCount = newSlotCount;
    }

    public int GetMaxSlots()
    {
        return OriginalSlotCount;
    }

    public override string ToString()
    {
        return $"{_instanceId},{_indexId},{_slotCount},{_originalSlotCount}";
    }
}