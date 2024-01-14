using ContainerResizer.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ContainerResizer.Components
{
    public class SliderComponent : MonoBehaviour
    {
        public GameObject storageUnit;
        public GameObject sliderContainer;
        public GameObject sliderLabel;
        public GameObject sliderValue;
        public TMP_Text sliderLabelText;
        public TMP_Text sliderValueText;
        public Slider storageSlider;

        private TextMeshProUGUI _sortText;
        private int _machineIndex;
        private bool _initalOpen = true;
        private InventoryNavigator _inventoryNavigator;
        
        //Component Methods
        private void Awake()
        {
            storageUnit = transform.Find("Top Level Container/Container/Storage Unit")?.gameObject;
            var sortText = transform.Find("Top Level Container/Container/Storage Unit/Storage Sort Button/Sort Text");
            if (sortText != null)
            {
                _sortText = sortText.GetComponent<TextMeshProUGUI>();
            }

            if (storageUnit != null)
            {
                sliderContainer = Instantiate(ContainerResizer.Assets.LabeledSlider, storageUnit.transform);
                var slider = sliderContainer.GetComponent<RectTransform>();
                slider.anchoredPosition = new Vector2(300, 50);
                sliderLabel = sliderContainer.transform.Find("TMP Label").gameObject;
                sliderLabelText = sliderLabel.GetComponent<TextMeshProUGUI>();
                sliderValue = sliderContainer.transform.Find("TMP ValueLabel").gameObject;
                sliderValueText = sliderValue.GetComponent<TextMeshProUGUI>();
                storageSlider = sliderContainer.transform.Find("Slider").GetComponent<Slider>();

                sliderLabelText.text = "Adjust Storage Size:";
                sliderLabelText.font = _sortText.font;
                sliderLabelText.fontSize = 17;
                sliderLabelText.fontSizeMax = 18;
                sliderLabelText.fontSizeMin = 3;
                sliderLabelText.enableAutoSizing = true;
                
                sliderValueText.font = _sortText.font;
                sliderValueText.fontSize = 20;
                sliderValueText.fontSizeMax = 30;
                sliderValueText.fontSizeMin = 3;
                sliderValueText.enableAutoSizing = true;
                sliderValueText.text = $"{storageSlider.value}";

                storageSlider.onValueChanged.AddListener(UpdateValue); 
            }
        }

        private void UpdateValue(float value)
        {
            var newSlotSize = (int)value;
            
            if (_machineIndex < 0)
                return;
            
            sliderValueText.text = $"{newSlotSize}";

            if (_initalOpen)
            {
                _initalOpen = false;
                return;
            }
            
            if (!ContainerManager.TryResizeChest(_machineIndex, newSlotSize, out var minimumSlots))
                return;
            
            storageSlider.minValue = minimumSlots == 0 ? 1 : minimumSlots;
            _inventoryNavigator.Refresh(true);
        }

        public void OpenChest(MachineInstanceRef<ChestInstance> machineRef, InventoryNavigator invNavigator)
        {
            _inventoryNavigator = invNavigator;
            _machineIndex = machineRef.index;

            var manager = MachineManager.instance.GetMachineList<ChestInstance, ChestDefinition>(MachineTypeEnum.Chest);
            var chest = manager.GetIndex(_machineIndex);

            if (!ContainerManager.TryGetContainer(machineRef._instanceId, out var record))
            {
                ContainerManager.AddContainer(machineRef.instanceId, machineRef.index, chest, out record);
            }

            var minimumSlots = chest.commonInfo.inventories[0].numSlots - chest.commonInfo.inventories[0].GetNumberOfEmptySlots();

            storageSlider.maxValue = record.GetMaxSlots();
            storageSlider.value = chest.commonInfo.inventories[0].numSlots;
            storageSlider.minValue = minimumSlots == 0 ? 1 : minimumSlots;
            
            UpdateValue(chest.commonInfo.inventories[0].numSlots);
        }

        public void CloseChest()
        {
            _machineIndex = -1;
            _initalOpen = true;
            storageSlider.value = -1;
        }
    }
}