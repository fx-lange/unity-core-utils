using System.Collections.Generic;
using System.Linq;
using CoreFx;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace CoreUtilsFx.Editor.SceneSelector
{
    public class SceneSelectorWindow : EditorWindow
    {
        private enum ActiveSceneChoice
        {
            OpenedScene,
            FirstFixedScene
        }

        [SerializeField] private string _keyword = "";
        [SerializeField] private ScenesList _fixedScenes;
        [SerializeField] private ScenesList _preselection;
        [SerializeField] private bool _useFixedScenes = true;
        [SerializeField] private bool _usePreselection = true;
        [SerializeField] private ActiveSceneChoice _activeSceneChoice = ActiveSceneChoice.OpenedScene;

        private TextField _input;
        private VisualElement _container;
        private DropdownField _fixedScenesDropdown;
        private DropdownField _preselectionDropdown;
        private EnumField _activeSceneField;

        private List<string> _scenesListNames;
        private List<ScenesList> _scenesListAssets;

        [MenuItem("Tools/Scenes/Scene Window")]
        private static void OpenWindow()
        {
            GetWindow<SceneSelectorWindow>(title: "Scene Selector");
        }

        private void CreateGUI()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            RefreshScenesListChoices();

            // ── Settings section ──────────────────────────────────────────
            var settingsSection = SettingsSection();
            rootVisualElement.Add(settingsSection);

            // ── Filter ────────────────────────────────────────────────────
            rootVisualElement.Add(MakeDivider());

            _input = new TextField
            {
                label = "Filter", value = _keyword,
                style =
                {
                    marginLeft = 4,
                    marginRight = 4,
                    marginTop = 2
                }
            };
            _input.RegisterValueChangedCallback(UpdateKeyword);
            rootVisualElement.Add(_input);

            // ── Scene list ────────────────────────────────────────────────
            rootVisualElement.Add(MakeDivider());

            var scroll = new ScrollView(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1
                }
            };
            _container = new VisualElement();
            scroll.Add(_container);
            rootVisualElement.Add(scroll);

            Populate();
        }

        private VisualElement SettingsSection()
        {
            var settingsSection = new VisualElement
            {
                style =
                {
                    backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.12f)),
                    paddingTop = 4,
                    paddingBottom = 4,
                    paddingLeft = 4,
                    paddingRight = 4
                }
            };

            // Row: Toggle + Dropdown + Ping (Fixed Scenes)
            var useFixedToggle = new Toggle
            {
                value = _useFixedScenes, tooltip = "Use Fixed Scenes",
                style =
                {
                    marginRight = 4
                }
            };

            var fixedInitialIdx = _fixedScenes != null ? _scenesListAssets.IndexOf(_fixedScenes) : 0;
            if (fixedInitialIdx < 0) fixedInitialIdx = 0;
            _fixedScenesDropdown = new DropdownField("Fixed Scenes", _scenesListNames, fixedInitialIdx)
            {
                style =
                {
                    flexGrow = 1
                }
            };
            _fixedScenesDropdown.SetEnabled(_useFixedScenes);

            var fixedPingBtn = MakePingButton(() => _fixedScenes);
            fixedPingBtn.SetEnabled(_fixedScenes != null);

            _fixedScenesDropdown.RegisterValueChangedCallback(evt =>
            {
                var idx = _scenesListNames.IndexOf(evt.newValue);
                _fixedScenes = idx > 0 ? _scenesListAssets[idx] : null;
                fixedPingBtn.SetEnabled(_fixedScenes != null);
                _activeSceneField.SetEnabled(_useFixedScenes && _fixedScenes != null);
                Populate();
            });

            useFixedToggle.RegisterValueChangedCallback(evt =>
            {
                _useFixedScenes = evt.newValue;
                _fixedScenesDropdown.SetEnabled(_useFixedScenes);
                _activeSceneField.SetEnabled(_useFixedScenes && _fixedScenes != null);
                Populate();
            });

            var fixedRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            fixedRow.Add(useFixedToggle);
            fixedRow.Add(_fixedScenesDropdown);
            fixedRow.Add(fixedPingBtn);
            RegisterDropTarget(fixedRow, asset =>
            {
                var idx = _scenesListAssets.IndexOf(asset);
                if (idx < 0) return;
                _fixedScenesDropdown.index = idx;
                _fixedScenes = asset;
                fixedPingBtn.SetEnabled(true);
                _activeSceneField.SetEnabled(_useFixedScenes && _fixedScenes != null);
                Populate();
            });
            settingsSection.Add(fixedRow);

            // Row: Toggle + Dropdown + Ping (Scene Options)
            var usePreselectionToggle = new Toggle
            {
                value = _usePreselection, tooltip = "Use Scene Options",
                style =
                {
                    marginRight = 4
                }
            };

            var preselInitialIdx = _preselection != null ? _scenesListAssets.IndexOf(_preselection) : 0;
            if (preselInitialIdx < 0) preselInitialIdx = 0;
            _preselectionDropdown = new DropdownField("Scene Options", _scenesListNames, preselInitialIdx)
            {
                style =
                {
                    flexGrow = 1
                }
            };
            _preselectionDropdown.SetEnabled(_usePreselection);

            var preselPingBtn = MakePingButton(() => _preselection);
            preselPingBtn.SetEnabled(_preselection != null);

            _preselectionDropdown.RegisterValueChangedCallback(evt =>
            {
                var idx = _scenesListNames.IndexOf(evt.newValue);
                _preselection = idx > 0 ? _scenesListAssets[idx] : null;
                preselPingBtn.SetEnabled(_preselection != null);
                Populate();
            });

            usePreselectionToggle.RegisterValueChangedCallback(evt =>
            {
                _usePreselection = evt.newValue;
                _preselectionDropdown.SetEnabled(_usePreselection);
                Populate();
            });

            var optionsRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            optionsRow.Add(usePreselectionToggle);
            optionsRow.Add(_preselectionDropdown);
            optionsRow.Add(preselPingBtn);
            RegisterDropTarget(optionsRow, asset =>
            {
                var idx = _scenesListAssets.IndexOf(asset);
                if (idx >= 0)
                {
                    _preselectionDropdown.index = idx;
                    _preselection = asset;
                    preselPingBtn.SetEnabled(true);
                    Populate();
                }
            });
            settingsSection.Add(optionsRow);

            // Row: EnumField (Active Scene)
            var activeSceneRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            _activeSceneField = new EnumField("Active Scene", _activeSceneChoice)
            {
                style =
                {
                    flexGrow = 1
                }
            };
            _activeSceneField.SetEnabled(_useFixedScenes && _fixedScenes != null);
            _activeSceneField.RegisterValueChangedCallback(evt =>
            {
                _activeSceneChoice = (ActiveSceneChoice)evt.newValue;
            });
            activeSceneRow.Add(_activeSceneField);
            settingsSection.Add(activeSceneRow);
            return settingsSection;
        }

        // Small "locate in project" button next to a dropdown
        private static Button MakePingButton(System.Func<ScenesList> getAsset)
        {
            var btn = new Button(() =>
            {
                var asset = getAsset();
                if (asset == null) return;
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            })
            {
                text = "⊙",
                tooltip = "Select in Project",
                style =
                {
                    width = 22,
                    paddingLeft = 0,
                    paddingRight = 0,
                    marginLeft = 2
                }
            };
            return btn;
        }

        // Register a ScenesList drag-and-drop target on the given element
        private static void RegisterDropTarget(VisualElement target, System.Action<ScenesList> onDrop)
        {
            var normalBg = new StyleColor(Color.clear);
            var highlightBg = new StyleColor(new Color(0.25f, 0.55f, 1f, 0.25f));

            target.RegisterCallback<DragEnterEvent>(_ =>
            {
                if (DragAndDrop.objectReferences.OfType<ScenesList>().Any())
                    target.style.backgroundColor = highlightBg;
            });
            target.RegisterCallback<DragLeaveEvent>(_ => { target.style.backgroundColor = normalBg; });
            target.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (DragAndDrop.objectReferences.OfType<ScenesList>().Any())
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    evt.StopPropagation();
                }
            });
            target.RegisterCallback<DragPerformEvent>(evt =>
            {
                var asset = DragAndDrop.objectReferences.OfType<ScenesList>().FirstOrDefault();
                if (asset == null) return;
                DragAndDrop.AcceptDrag();
                target.style.backgroundColor = normalBg;
                onDrop(asset);
                evt.StopPropagation();
            });
            target.RegisterCallback<DragExitedEvent>(_ => { target.style.backgroundColor = normalBg; });
        }

        private static VisualElement MakeDivider()
        {
            var line = new VisualElement
            {
                style =
                {
                    height = 1,
                    backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.35f)),
                    marginTop = 2,
                    marginBottom = 2
                }
            };
            return line;
        }

        private void RefreshScenesListChoices()
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

        private void UpdateKeyword(ChangeEvent<string> evt)
        {
            _keyword = evt.newValue;
            Populate();
        }

        private void Populate()
        {
            _container.Clear();

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

            if (string.IsNullOrEmpty(_keyword))
            {
                foreach (var sceneAsset in scenes)
                    _container.Add(CreateSceneButton(sceneAsset, null));
            }
            else
            {
                var scored = scenes
                    .Select(s => (scene: s, result: FuzzyMatch(s.name, _keyword)))
                    .Where(x => x.result.matches)
                    .OrderByDescending(x => x.result.score)
                    .ToList();

                foreach (var (scene, result) in scored)
                    _container.Add(CreateSceneButton(scene, result.matchedIndices));
            }
        }

        private static (bool matches, int score, int[] matchedIndices) FuzzyMatch(string target, string query)
        {
            if (string.IsNullOrEmpty(query))
                return (true, 0, System.Array.Empty<int>());

            var targetLower = target.ToLower();
            var queryLower = query.ToLower();

            int score = 0, queryIdx = 0, lastMatchIdx = -1, consecutive = 0;
            var matchedIndices = new int[queryLower.Length];

            for (var i = 0; i < targetLower.Length && queryIdx < queryLower.Length; i++)
            {
                if (targetLower[i] != queryLower[queryIdx])
                {
                    consecutive = 0;
                    continue;
                }

                matchedIndices[queryIdx++] = i;

                if (lastMatchIdx == i - 1) score += ++consecutive * 3;
                else consecutive = 0;

                var isWordBoundary = i == 0 || target[i - 1] == '_' || target[i - 1] == ' '
                                     || (char.IsUpper(target[i]) && char.IsLower(target[i - 1]));
                if (isWordBoundary) score += 5;

                score += 1;
                lastMatchIdx = i;
            }

            var matched = queryIdx == queryLower.Length;
            return (matched, score, matched ? matchedIndices : null);
        }

        private static bool HasUnsavedScenes()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    return true;
            return false;
        }

        private static bool ConfirmDiscardOrSave()
        {
            if (!HasUnsavedScenes()) return true;

            var choice = EditorUtility.DisplayDialogComplex(
                "Unsaved Scenes",
                "You have unsaved changes in the current scene(s). What would you like to do?",
                "Save and Open", // 0
                "Cancel", // 1
                "Discard and Open" // 2
            );
            switch (choice)
            {
                case 0: return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                case 2: return true;
                default: return false;
            }
        }

        private VisualElement CreateSceneButton(SceneAsset sceneAsset, int[] highlightIndices)
        {
            var scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            var buttonGroup = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, marginLeft = 3 }
            };

            var label = BuildLabel(sceneAsset.name, highlightIndices);
            label.style.width = 150;
            buttonGroup.Add(label);

            var openButton = new Button(() =>
            {
                if (!ConfirmDiscardOrSave()) return;

                if (_useFixedScenes && _fixedScenes != null)
                {
                    var first = true;
                    foreach (var fixedScene in _fixedScenes.Scenes)
                    {
                        var path = AssetDatabase.GetAssetPath(fixedScene);
                        EditorSceneManager.OpenScene(path, first ? OpenSceneMode.Single : OpenSceneMode.Additive);
                        first = false;
                    }

                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

                    switch (_activeSceneChoice)
                    {
                        case ActiveSceneChoice.FirstFixedScene:
                            SceneManager.SetActiveScene(SceneManager.GetSceneByPath(
                                AssetDatabase.GetAssetPath(_fixedScenes.Scenes[0])));
                            break;
                        default:
                            SceneManager.SetActiveScene(SceneManager.GetSceneByPath(scenePath));
                            break;
                    }
                }
                else
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }
            }) { text = "Open" };

            buttonGroup.Add(openButton);

            var openAddButton = new Button(() => { EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); })
                { text = "Open Additive" };

            buttonGroup.Add(openAddButton);
            return buttonGroup;
        }

        private static VisualElement BuildLabel(string name, int[] highlightIndices)
        {
            if (highlightIndices == null || highlightIndices.Length == 0)
                return new Label(name);

            var container = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var highlightSet = new HashSet<int>(highlightIndices);
            var i = 0;
            while (i < name.Length)
            {
                var highlighted = highlightSet.Contains(i);
                var start = i;
                while (i < name.Length && highlightSet.Contains(i) == highlighted) i++;

                var lbl = new Label(name.Substring(start, i - start))
                {
                    style =
                    {
                        paddingLeft = 0,
                        paddingRight = 0
                    }
                };
                if (highlighted)
                {
                    lbl.style.color = new StyleColor(new Color(0.4f, 0.8f, 1f));
                    lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                }

                container.Add(lbl);
            }

            return container;
        }

        private void OnDisable()
        {
            _input?.UnregisterValueChangedCallback(UpdateKeyword);
        }

        [InitializeOnLoadMethod]
        private static void RegisterCallbacks()
        {
            EditorApplication.playModeStateChanged += ReturnToPreviousScene;
        }

        private static void ReturnToPreviousScene(PlayModeStateChange change)
        {
            if (!HasOpenInstances<SceneSelectorWindow>()) return;

            if (change == PlayModeStateChange.EnteredPlayMode)
                GetWindow<SceneSelectorWindow>().SetActive(false);
            else if (change == PlayModeStateChange.EnteredEditMode)
                GetWindow<SceneSelectorWindow>().SetActive(true);
            // ExitingEditMode and ExitingPlayMode are intentionally ignored
        }

        private void SetActive(bool active)
        {
            rootVisualElement.SetEnabled(active);
        }
    }
}