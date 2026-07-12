using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace MiniMart.Editor
{
    public static class UpdateBuildSettings
    {
        [MenuItem("MiniMart/Add MegaMart to Build Settings")]
        public static void AddMegaMart()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            string scenePath = "Assets/MegaMart.unity";
            
            if (scenes.Any(s => s.path == scenePath))
            {
                Debug.Log("MegaMart is already in Build Settings.");
                return;
            }

            var newScene = new EditorBuildSettingsScene(scenePath, true);
            scenes.Add(newScene);
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Added MegaMart to Build Settings.");
        }
    }
}
