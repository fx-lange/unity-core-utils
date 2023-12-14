using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoreFx
{
    [CreateAssetMenu(fileName = "SceneList", menuName = "Utils/SceneList", order = 0)]
    public class ScenesList : ScriptableObject
    {
        // [ReadOnly]
        [SerializeField] private List<string> _scenes = new();

        public int Count => _scenes.Count;
        
        public string Get(int idx)
        {
            if (idx < 0 || idx >= _scenes.Count)
            {
                Debug.LogError("Scene not found");
                return "";
            }

            return _scenes[idx];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UpdateScenes();
        }

        public List<UnityEditor.SceneAsset> Scenes => _sceneAssets;

        [SerializeField] private List<UnityEditor.SceneAsset> _sceneAssets = new();
        private void UpdateScenes()
        {
            _scenes = _sceneAssets.Where(asset => asset != null).Select(UnityEditor.AssetDatabase.GetAssetPath).ToList();
        }
#endif
    }
}