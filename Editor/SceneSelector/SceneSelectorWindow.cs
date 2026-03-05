using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
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
        private DropdownField _fixedScenesDropdown;
        private DropdownField _preselectionDropdown;
        private readonly List<Button> _buttons = new();

        private List<string> _scenesListNames;
        private List<ScenesList> _scenesListAssets;

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

            RefreshScenesListChoices();

            // Row: Toggle (Use Fixed) + DropdownField (Fixed Scenes)
            var fixedRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var useFixedToggle = new Toggle { value = _useFixedScenes, tooltip = "Use Fixed Scenes" };
            useFixedToggle.style.marginRight = 4;

            int fixedInitialIdx = _fixedScenes != null ? _scenesListAssets.IndexOf(_fixedScenes) : 0;
            if (fixedInitialIdx < 0) fixedInitialIdx = 0;
            _fixedScenesDropdown = new DropdownField("Fixed Scenes", _scenesListNames, fixedInitialIdx);
            _fixedScenesDropdown.style.flexGrow = 1;
            _fixedScenesDropdown.SetEnabled(_useFixedScenes);
            _fixedScenesDropdown.RegisterValueChangedCallback(evt =>
            {
                int idx = _scenesListNames.IndexOf(evt.newValue);
                _fixedScenes = idx > 0 ? _scenesListAssets[idx] : null;
                _useFixedScenes = idx > 0;
                useFixedToggle.value = _useFixedScenes;
                Populate();
            });

            useFixedToggle.RegisterValueChangedCallback(evt =>
            {
                _useFixedScenes = evt.newValue;
                if (!_useFixedScenes)
                {
                    _fixedScenesDropdown.index = 0;
                    _fixedScenes = null;
                }
                _fixedScenesDropdown.SetEnabled(_useFixedScenes);
                Populate();
            });

            fixedRow.Add(useFixedToggle);
            fixedRow.Add(_fixedScenesDropdown);
            rootVisualElement.Add(fixedRow);

            // Row: Toggle (Use Options) + DropdownField (Scene Options)
            var optionsRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var usePreselectionToggle = new Toggle { value = _usePreselection, tooltip = "Use Scene Options" };
            usePreselectionToggle.style.marginRight = 4;

            int preselInitialIdx = _preselection != null ? _scenesListAssets.IndexOf(_preselection) : 0;
            if (preselInitialIdx < 0) preselInitialIdx = 0;
            _preselectionDropdown = new DropdownField("Scene Options", _scenesListNames, preselInitialIdx);
            _preselectionDropdown.style.flexGrow = 1;
            _preselectionDropdown.SetEnabled(_usePreselection);
            _preselectionDropdown.RegisterValueChangedCallback(evt =>
            {
                int idx = _scenesListNames.IndexOf(evt.newValue);
                _preselection = idx > 0 ? _scenesListAssets[idx] : null;
                _usePreselection = idx > 0;
                usePreselectionToggle.value = _usePreselection;
                Populate();
            });

            usePreselectionToggle.RegisterValueChangedCallback(evt =>
            {
                _usePreselection = evt.newValue;
                if (!_usePreselection)
                {
                    _preselectionDropdown.index = 0;
                    _preselection = null;
                }
                _preselectionDropdown.SetEnabled(_usePreselection);
                Populate();
            });

            optionsRow.Add(usePreselectionToggle);
            optionsRow.Add(_preselectionDropdown);
            rootVisualElement.Add(optionsRow);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            _container = new VisualElement();
            scroll.Add(_container);
            rootVisualElement.Add(scroll);

            Populate();
        }

        void RefreshScenesListChoices()
        {
            _scenesListNames = new List<string> { "(All Scenes)" };
            _scenesListAssets = new List<ScenesList> { null };

            foreach (var guid in AssetDatabase.FindAssets("t:ScenesList"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScenesList>(path);
                if (asset != null)
                {
                    _scenesListNames.Add(asset.name);
                    _scenesListAssets.Add(asset);
                }
            }
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