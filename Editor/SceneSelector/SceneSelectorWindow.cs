using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using CoreFx;

//TODO ask for save
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
        [SerializeField] private bool _useFixedScenes = true;
        [SerializeField] private bool _usePreselection = true;

        private TextField _input;
        private VisualElement _container;
        private ObjectField _objectField;
        private ObjectField _preSelectObjField;
        private readonly List<Button> _buttons = new();

        [MenuItem("Tools/Scenes/Scene Window")]
        static void OpenWindow()
        {
            GetWindow<SceneSelectorWindow>(title: "Scene Selector");
        }

        void CreateGUI()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            _input = new TextField
            {
                label = "Filter",
                value = keyword
            };
            rootVisualElement.Add(_input);
            _input.RegisterValueChangedCallback(UpdateKeyword);

            // Row: Toggle (Use Fixed) + ObjectField (Fixed Scenes)
            var fixedRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var useFixedToggle = new Toggle { value = _useFixedScenes, tooltip = "Use Fixed Scenes" };
            useFixedToggle.style.marginRight = 4;
            useFixedToggle.RegisterValueChangedCallback(evt =>
            {
                _useFixedScenes = evt.newValue;
                _objectField.SetEnabled(_useFixedScenes);
                Populate();
            });
            _objectField = new ObjectField
            {
                label = "Fixed Scenes",
                objectType = typeof(ScenesList),
                value = _fixedScenes
            };
            _objectField.style.flexGrow = 1;
            _objectField.SetEnabled(_useFixedScenes);
            _objectField.RegisterValueChangedCallback(evt => { _fixedScenes = evt.newValue as ScenesList; });
            fixedRow.Add(useFixedToggle);
            fixedRow.Add(_objectField);
            rootVisualElement.Add(fixedRow);

            // Row: Toggle (Use Options) + ObjectField (Scene Options)
            var optionsRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var usePreselectionToggle = new Toggle { value = _usePreselection, tooltip = "Use Scene Options" };
            usePreselectionToggle.style.marginRight = 4;
            usePreselectionToggle.RegisterValueChangedCallback(evt =>
            {
                _usePreselection = evt.newValue;
                _preSelectObjField.SetEnabled(_usePreselection);
                Populate();
            });
            _preSelectObjField = new ObjectField
            {
                label = "Scene Preselection",
                objectType = typeof(ScenesList),
                value = _preselection
            };
            _preSelectObjField.style.flexGrow = 1;
            _preSelectObjField.SetEnabled(_usePreselection);
            _preSelectObjField.RegisterValueChangedCallback(evt =>
            {
                _preselection = evt.newValue as ScenesList;
                Populate();
            });
            optionsRow.Add(usePreselectionToggle);
            optionsRow.Add(_preSelectObjField);
            rootVisualElement.Add(optionsRow);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            _container = new VisualElement();
            scroll.Add(_container);
            rootVisualElement.Add(scroll);

            Populate();
        }

        void UpdateKeyword(ChangeEvent<string> evt)
        {
            keyword = evt.newValue;
            Populate();
        }

        void Populate()
        {
            _container.Clear();
            _buttons.Clear();

            List<SceneAsset> scenes;
            if (_usePreselection && _preselection != null)
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
                }).Where(s => s != null).ToList();
            }

            if (string.IsNullOrEmpty(keyword))
            {
                foreach (var sceneAsset in scenes)
                {
                    _container.Add(CreateSceneButton(sceneAsset, null));
                }
            }
            else
            {
                var scored = scenes
                    .Select(s => (scene: s, result: FuzzyMatch(s.name, keyword)))
                    .Where(x => x.result.matches)
                    .OrderByDescending(x => x.result.score)
                    .ToList();

                foreach (var (scene, result) in scored)
                {
                    _container.Add(CreateSceneButton(scene, result.matchedIndices));
                }
            }
        }

        static (bool matches, int score, int[] matchedIndices) FuzzyMatch(string target, string query)
        {
            if (string.IsNullOrEmpty(query))
                return (true, 0, System.Array.Empty<int>());

            var targetLower = target.ToLower();
            var queryLower = query.ToLower();

            int score = 0;
            int queryIdx = 0;
            int lastMatchIdx = -1;
            int consecutive = 0;
            var matchedIndices = new int[queryLower.Length];

            for (int i = 0; i < targetLower.Length && queryIdx < queryLower.Length; i++)
            {
                if (targetLower[i] != queryLower[queryIdx])
                {
                    consecutive = 0;
                    continue;
                }

                matchedIndices[queryIdx] = i;
                queryIdx++;

                // Consecutive bonus
                if (lastMatchIdx == i - 1)
                {
                    consecutive++;
                    score += consecutive * 3;
                }
                else
                {
                    consecutive = 0;
                }

                // Word boundary bonus
                bool isWordBoundary = i == 0
                                      || target[i - 1] == '_'
                                      || target[i - 1] == ' '
                                      || (char.IsUpper(target[i]) && char.IsLower(target[i - 1]));
                if (isWordBoundary)
                    score += 5;

                score += 1;
                lastMatchIdx = i;
            }

            bool matched = queryIdx == queryLower.Length;
            return (matched, score, matched ? matchedIndices : null);
        }

        VisualElement CreateSceneButton(SceneAsset sceneAsset, int[] highlightIndices)
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

            var label = BuildLabel(sceneAsset.name, highlightIndices);
            label.style.width = 150;
            buttonGroup.Add(label);

            var openButton = new Button(() =>
            {
                if (_useFixedScenes && _fixedScenes != null)
                {
                    bool first = true;
                    foreach (var fixedScene in _fixedScenes.Scenes)
                    {
                        var path = AssetDatabase.GetAssetPath(fixedScene);
                        if (first)
                        {
                            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                            first = false;
                            continue;
                        }

                        EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    }

                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }
                else
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }
            })
            {
                text = "Open"
            };

            _buttons.Add(openButton);
            buttonGroup.Add(openButton);

            var openAddButton = new Button(() => { EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); })
            {
                text = "Open Additive"
            };

            buttonGroup.Add(openAddButton);
            _buttons.Add(openAddButton);

            return buttonGroup;
        }

        static VisualElement BuildLabel(string name, int[] highlightIndices)
        {
            if (highlightIndices == null || highlightIndices.Length == 0)
            {
                var plain = new Label(name);
                return plain;
            }

            var container = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var highlightSet = new HashSet<int>(highlightIndices);

            int i = 0;
            while (i < name.Length)
            {
                if (highlightSet.Contains(i))
                {
                    // Collect consecutive highlighted chars
                    int start = i;
                    while (i < name.Length && highlightSet.Contains(i))
                        i++;
                    var hl = new Label(name.Substring(start, i - start));
                    hl.style.color = new StyleColor(new Color(0.4f, 0.8f, 1f));
                    hl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    hl.style.paddingLeft = 0;
                    hl.style.paddingRight = 0;
                    container.Add(hl);
                }
                else
                {
                    int start = i;
                    while (i < name.Length && !highlightSet.Contains(i))
                        i++;
                    var normal = new Label(name.Substring(start, i - start));
                    normal.style.paddingLeft = 0;
                    normal.style.paddingRight = 0;
                    container.Add(normal);
                }
            }

            return container;
        }

        private void OnDisable()
        {
            _input?.UnregisterValueChangedCallback(UpdateKeyword);
        }

        [InitializeOnLoadMethod]
        static void RegisterCallbacks()
        {
            EditorApplication.playModeStateChanged += ReturnToPreviousScene;
        }

        static void ReturnToPreviousScene(PlayModeStateChange change)
        {
            if (!HasOpenInstances<SceneSelectorWindow>())
                return;

            if (change == PlayModeStateChange.EnteredEditMode)
                GetWindow<SceneSelectorWindow>().SetActive(true);
            else
                GetWindow<SceneSelectorWindow>().SetActive(false);
        }

        private void SetActive(bool active)
        {
            rootVisualElement.SetEnabled(active);
        }
    }
}