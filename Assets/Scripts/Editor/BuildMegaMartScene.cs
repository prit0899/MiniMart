using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MiniMart;

namespace MiniMart.Editor
{
    public static class BuildMegaMartScene
    {
        public static void CreateScene()
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("_Bootstrap");
            go.AddComponent<SceneBootstrapper2>();
            
            bool success = EditorSceneManager.SaveScene(newScene, "Assets/MegaMart.unity");
            if (success)
            {
                Debug.Log("Successfully created Assets/MegaMart.unity");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("Failed to save MegaMart scene.");
                EditorApplication.Exit(1);
            }
        }
    }
}
