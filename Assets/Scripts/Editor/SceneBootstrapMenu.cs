using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiniMart.Editor
{
    public static class SceneBootstrapMenu
    {
        [MenuItem("MiniMart/Open Game Scene")]
        public static void OpenGameScene()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Game.unity");
            if (sceneAsset != null)
            {
                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(sceneAsset));
                Debug.Log("Opened MiniMart Game scene.");
            }
            else
            {
                Debug.LogError("Could not find Assets/Game.unity");
            }
        }
    }
}
