using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Nullframes.Intrigues.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nullframes.Intrigues.EDITOR.Utils
{
    public static class EditorUtilities
    {
        public static void SaveAsset(params Object[] assets)
        {
            foreach (var asset in assets)
            {
                if (asset == null) return;
                EditorUtility.SetDirty(asset);
            }

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }

        public static void BackupAsset(Object asset)
        {
            if (asset == null)
            {
                NDebug.Log("IDatabase is null.", NLogType.Error);
                return;
            }

            if (!UnityEditor.AssetDatabase.Contains(asset))
            {
                NDebug.Log("IDatabase is not found in the project.", NLogType.Error);
                return;
            }

            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(asset);
            var assetName = Path.GetFileNameWithoutExtension(assetPath);
            var backupPath = Path.GetDirectoryName(assetPath) + "/" + assetName + "_Backup" +
                             Path.GetExtension(assetPath);

            if (File.Exists(backupPath))
                try
                {
                    File.Delete(backupPath);
                }
                catch (Exception e)
                {
                    NDebug.Log("Failed to delete existing backup file: " + e.Message, NLogType.Error);
                    return;
                }

            try
            {
                UnityEditor.AssetDatabase.CopyAsset(assetPath, backupPath);
                NDebug.Log("Intrigues Database backup is successful.");
            }
            catch (Exception e)
            {
                NDebug.Log("Intrigues Database backup failed: " + e.Message, NLogType.Error);
            }
        }

        public static string ToRelativePath(string path)
        {
            if (!Regex.IsMatch(path, Application.dataPath))
                return path;
            path = Regex.Replace(path, Application.dataPath, string.Empty);
            path = path.Insert(0, "Assets");
            return path;
        }
        
        public static IEnumerable<T> FindAssetsByType<T>() where T : Object
        {
            var guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T)}");
            foreach (var t in guids)
            {
                var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(t);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    yield return asset;
                }
            }
        }
    }
}