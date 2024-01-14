using System.IO;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace ContainerResizer
{
    public static class Utils
    {
        public static AssetBundle LoadAssetBundle(string filename, bool loadFromPath = false)
        {
            // Optionally load asset bundle from path, if it exists
            if (loadFromPath)
            {
                var assetBundlePath = GetAssetPath(filename);
                if (!string.IsNullOrEmpty(assetBundlePath))
                {
                    return AssetBundle.LoadFromFile(assetBundlePath);
                }
            }

            var assembly = Assembly.GetCallingAssembly();
            
            var assetBundle = AssetBundle.LoadFromStream(assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Assets.{filename}"));

            return assetBundle;
        }
        
        public static string GetAssetPath(string assetName)
        {
            var assetFileName = Path.Combine(Paths.PluginPath, "ContainerResizer", assetName);
            if (!File.Exists(assetFileName))
            {
                var assembly = typeof(ContainerResizer).Assembly;
                assetFileName = Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, assetName);
                if (!File.Exists(assetFileName))
                {
                    ContainerResizer.Log.LogError($"Could not find asset ({assetName})");
                    return null;
                }
            }

            return assetFileName;
        }
        public static Sprite LoadNewSprite(string filePath, float pixelsPerUnit = 100.0f, SpriteMeshType spriteType = SpriteMeshType.Tight)
        {
            // Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference
            Texture2D spriteTexture = LoadTexture(filePath);
            Sprite newSprite = Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0, 0), pixelsPerUnit, 0, spriteType);
            return newSprite;
        }
        
        public static Texture2D LoadTexture(string filePath)
        {
            // Load a PNG or JPG file from disk to a Texture2D
            // Returns null if load fails
            Texture2D tex2D;
            byte[] fileData;
            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                tex2D = new Texture2D(2, 2);           // Create new "empty" texture
                if (tex2D.LoadImage(fileData))           // Load the imagedata into the texture (size is set automatically)
                    return tex2D;                 // If data = readable -> return texture
            }
            return null;                     // Return null if load failed
        }
    }
}