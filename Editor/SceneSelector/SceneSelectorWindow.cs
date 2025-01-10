using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using CoreFx;

//TODO ask for save
//TODO case insensitive filter
//TODO filter via path?
//TODO active scene control (first fixed or last)
// -> Main Scene, Fixed Scenes, "Levels"

namespace SceneSelector
{
    public class SceneSelectorWindow : EditorWindow
    {
        [SerializeField] private string keyword = "";
        [SerializeField] private ScenesList _fixedScenes;
        [SerializeField] private ScenesList _preselection;

        private TextField _input;
        private VisualElement _container;
        private ObjectField _objectField;
        private readonly List<Button> _buttons = new();

        [MenuItem("Tools/Scenes/Scene Window")]
        static void OpenWindow()
        {
            GetWindow<SceneSelectorWindow>(title: "Scene Selector");
        }
        
        void CreateGUI()
        {
            _container = new VisualElement();
            rootVisualElement.Add(_container);
            
            _input = new TextField
            {
                label = "Filter",
                value = keyword
            };
            rootVisualElement.Add(_input);
            
            _input.RegisterValueChangedCallback(UpdateKeyword);
            _input.RegisterCallback<FocusOutEvent>(CallPopulate);

            _objectField = new ObjectField
            {
                label = "Fixed Scenes",
                objectType = typeof(ScenesList),
                value = _fixedScenes
            };
            _objectField.RegisterValueChangedCallback(SceneChangeCallback);
            var preSelectObjField = new ObjectField()
            {
                label = "Scene Options",
                objectType = typeof(ScenesList),
                value = _preselection
            };
            preSelectObjField.RegisterValueChangedCallback(evt =>
            {
                _preselection = evt.newValue as ScenesList;
                Populate();
            });
            
            rootVisualElement.Add(_objectField);
            rootVisualElement.Add(preSelectObjField);
            
            Populate();
        }

        private void SceneChangeCallback(ChangeEvent<Object> evt)
        {
            _fixedScenes = evt.newValue as ScenesList;;

            // foreach (var button in _buttons)
            // {
            //     button.SetEnabled(_fixedScenes != null);
            // }
        }

        void UpdateKeyword(ChangeEvent<string> evt)
        {
            keyword = evt.newValue;
        }

        void CallPopulate(FocusOutEvent evt)
        {
            Populate();
        }
        
        void Populate()
        {
            _container.Clear();
            _buttons.Clear();
         
            List<SceneAsset> scenes;
            if (_preselection != null)
            {
                scenes = _preselection.Scenes;
            }
            else
            {
                var sceneGuids = AssetDatabase.FindAssets("t:Scene");
                scenes = sceneGuids.Select(guid =>
                {
                    var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                    return AssetDatabase.LoadAssetAtPath(scenePath, typeof(SceneAsset)) as SceneAsset;
                }).ToList();
            }
            
            foreach (var sceneAsset in scenes)
            {
                if (!sceneAsset.name.ToLower().Contains(keyword.ToLower()))
                {
                    continue;
                }
                var visualElement = CreateSceneButton(sceneAsset);
                _container.Add(visualElement);
            }
        }

        private void OnDisable()
        {
            _input?.UnregisterValueChangedCallback(UpdateKeyword);
            _input?.UnregisterCallback<FocusOutEvent>(CallPopulate);
            _objectField?.UnregisterValueChangedCallback(SceneChangeCallback);
        }

        VisualElement CreateSceneButton(SceneAsset sceneAsset)
        {
           string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
           var buttonGroup = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginLeft = 3
                }
            };

            var label = new Label($"{sceneAsset.name}");
            label.style.width = 150;
            buttonGroup.Add(label);

            var openButton = new Button(() =>
            {
                bool first = true;
                if (_fixedScenes != null)
                {
                    foreach (var fixedScene in _fixedScenes.Scenes)
                    {
                        var path =AssetDatabase.GetAssetPath(fixedScene);
                        if (first)
                        {
                            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                            first = false;
                            continue;
                        }
                        
                        EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    }
                }
                
                var scene = EditorSceneManager.OpenScene(scenePath, first ? OpenSceneMode.Single : OpenSceneMode.Additive);
                // SceneManager.SetActiveScene(scene);
            })
            {
                text = "Open"
            };
            
            // openButton.SetEnabled(_fixedScenes != null);
            
            _buttons.Add(openButton);
            buttonGroup.Add(openButton);

            var openAddButton = new Button(() =>
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                // EditorApplication.EnterPlaymode();
            })
            {
                text = "Open Additive"
            };
            
            buttonGroup.Add(openAddButton);
            _buttons.Add(openAddButton);

            return buttonGroup;
        }
        
        [InitializeOnLoadMethod]
        static void RegisterCallbacks()
        {
            EditorApplication.playModeStateChanged += ReturnToPreviousScene;
        }

        static void ReturnToPreviousScene(PlayModeStateChange change)
        {
            if (!HasOpenInstances<SceneSelectorWindow>())
            {
                return;
            }
            
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                GetWindow<SceneSelectorWindow>().SetActive(true);
                // EditorSceneManager.OpenScene(SceneSelectorSettings.instance.PreviousScenePath, OpenSceneMode.Single);
            }
            else
            {
                GetWindow<SceneSelectorWindow>().SetActive(false);
            }
        }

        private void SetActive(bool active)
        {
            rootVisualElement.SetEnabled(active);
        }
    }
}