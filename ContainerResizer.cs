using System.IO;
using BepInEx;
using BepInEx.Logging;
using ContainerResizer.Assets;
using HarmonyLib;
using UnityEngine;

namespace ContainerResizer
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class ContainerResizer : BaseUnityPlugin
    {
        private const string MyGUID = "com.vapok.ContainerResizer";
        private const string PluginName = "ContainerResizer";
        private const string VersionString = "1.0.1";

        public static ContainerResizer Instance => _instance;
        public static ManualLogSource Log = new ManualLogSource(PluginName);
        public static readonly TTModAssets Assets = new TTModAssets();

        private static ContainerResizer _instance;
        private static Harmony _harmony = new Harmony(MyGUID);
        private string _saveFolder = Path.Combine(Application.persistentDataPath, "ContainerResizer");

        public MachineInstanceList<ChestInstance, ChestDefinition> ChestManager = null; 

        private void Awake()
        {
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            _instance = this;
            Logger.LogInfo($"{PluginName} [{VersionString}] is loading...");
            Log = Logger;

            Directory.CreateDirectory(_saveFolder);
            
            LoadAssets();

            _harmony.PatchAll();
        }
        
        private void LoadAssets()
        {
            var assetBundle = Utils.LoadAssetBundle("vapokttmods");
            Assets.LabeledSlider = assetBundle.LoadAsset<GameObject>("LabeledSliderWithValue");
        }
    }
}