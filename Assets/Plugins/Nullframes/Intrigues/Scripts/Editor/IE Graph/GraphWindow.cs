using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Nullframes.Intrigues.EDITOR;
using Nullframes.Intrigues.EDITOR.Utils;
using Nullframes.Intrigues.Graph.Nodes;
using Nullframes.Intrigues.Utils;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph
{
    public class GraphWindow : EditorWindow
    {
        private static IEGraphView ieGraphView;
        
        private Button miniMapBtn;
        private Button debugModeBtn;
        private Button nexusBtn;
        private Button ruleBtn;
        private Button familyBtn;
        private Button clanBtn;
        private Button policyBtn;
        private Button characterBtn;
        private Button variableBtn;
        private Button cultureBtn;
        private Button debugBtn;
        private Button stopBtn;
        private static Label currentDatabaseLabel;
        private static Label selectionLabel;

        public static IEDatabase CurrentDatabase;

        private static IERoutine routine;

        public GenericNodeType CurrentPage;

        private VisualElement childRoot;
        private VisualElement creationPanel;
        private VisualElement clanPanel;
        private VisualElement familyPanel;

        private VisualElement isPlayingPanel;

        private VisualElement actorField;
        private VisualElement storyField;
        private VisualElement clanField;
        private VisualElement familyField;
        private VisualElement ruleField;

        private DropdownField actorList;
        private DropdownField storyList;
        private DropdownField clanList;
        private DropdownField familyList;
        private DropdownField ruleList;

        private bool refreshRate;

        public Dictionary<string, string> characters;
        public Dictionary<string, string> stories;
        public Dictionary<string, string> clans;
        public Dictionary<string, string> families;
        public Dictionary<string, string> rules;

        public static GraphWindow instance;

        public string storyKey = string.Empty;
        public string clanKey = string.Empty;
        public string familyKey = string.Empty;
        public string ruleKey = string.Empty;
        public string actorKey = string.Empty;

        private bool creationMenuOpened;

        public bool DebugMode = false;

        private string searchStr;
        private Vector2 lastScrollRect;
        private Vector2 lastClanScrollRect;
        private Vector2 lastFamilyScrollRect;

        private enum KeyType
        {
            None,
            Left,
            Right,
            Up,
            Down
        }

        private KeyType currentKey;

        private void DisableButtons()
        {
            nexusBtn.EnableInClassList("ide-toolbar__button__selected", false);
            ruleBtn.EnableInClassList("ide-toolbar__button__selected", false);
            familyBtn.EnableInClassList("ide-toolbar__button__selected", false);
            clanBtn.EnableInClassList("ide-toolbar__button__selected", false);
            policyBtn.EnableInClassList("ide-toolbar__button__selected", false);
            characterBtn.EnableInClassList("ide-toolbar__button__selected", false);
            variableBtn.EnableInClassList("ide-toolbar__button__selected", false);
            cultureBtn.EnableInClassList("ide-toolbar__button__selected", false);
            GraphSaveUtility.LoadCurrent(ieGraphView);
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (routine != null) return false;
            if (Selection.activeObject is not IEDatabase obj) return false;
            var path = UnityEditor.AssetDatabase.GetAssetPath(obj);
            var ideDB = UnityEditor.AssetDatabase.LoadAssetAtPath<IEDatabase>(path);
            CurrentDatabase = ideDB;
            GraphSaveUtility.processCount = 0;
            PlayerPrefs.SetString("IDE_db_Path", path);
            PlayerPrefs.Save();
            if (ieGraphView == null)
            {
                Open();
                GetWindow<GraphWindow>().ToggleView();
            }
            else
            {
                GraphSaveUtility.LoadCurrent(ieGraphView);
                GetWindow<GraphWindow>().ToggleView();
            }
            
            currentDatabaseLabel.text = $"Current Database: {Selection.activeObject.name}";

            return true;
        }

        public static void Open(IEDatabase database)
        {
            CurrentDatabase = database;
            GraphSaveUtility.processCount = 0;
            PlayerPrefs.SetString("IDE_db_Path", UnityEditor.AssetDatabase.GetAssetPath(database));
            PlayerPrefs.Save();
            if (ieGraphView == null)
            {
                Open();
            }
            else
            {
                GraphSaveUtility.LoadCurrent(ieGraphView);
                currentDatabaseLabel.text = $"Current Database: {database.name}";
                Open();
                GetWindow<GraphWindow>().ToggleView();
            }
        }

        private static void Open()
        {
            var wnd = GetWindow<GraphWindow>();
            wnd.titleContent = new GUIContent("Intrigues | Graph Editor");
        }

        private void OnEnable()
        {
            instance = this;
            childRoot = new VisualElement()
            {
                pickingMode = PickingMode.Ignore
            };
            childRoot.AddStyleSheets("Nullframes/GraphView_DownTop.uss");
            childRoot.AddClasses("root");

            Setup();
            AddToolbar();

            if (PlayerPrefs.HasKey("IDE_CurrentStory")) storyKey = PlayerPrefs.GetString("IDE_CurrentStory");

            if (PlayerPrefs.HasKey("IDE_CurrentClan")) clanKey = PlayerPrefs.GetString("IDE_CurrentClan");

            if (PlayerPrefs.HasKey("IDE_CurrentFamily")) familyKey = PlayerPrefs.GetString("IDE_CurrentFamily");

            if (PlayerPrefs.HasKey("IDE_CurrentRule"))
                ruleKey = PlayerPrefs.GetString("IDE_CurrentRule");

            if (PlayerPrefs.HasKey("IDE_ActorID"))
                actorKey = PlayerPrefs.GetString("IDE_ActorID");

            rootVisualElement.AddStyleSheets("Nullframes/IEVariables.uss");

            var pageNumber = PlayerPrefs.GetInt("IDE_PAGE");
            CurrentPage = (GenericNodeType)pageNumber;

            if (PlayerPrefs.HasKey("IDE_db_Path"))
            {
                var path = PlayerPrefs.GetString("IDE_db_Path");
                CurrentDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<IEDatabase>(path);

                if (CurrentDatabase != null)
                {
                    GraphSaveUtility.LoadCurrent(ieGraphView);
                    currentDatabaseLabel.text = $"Current Database: {Path.GetFileNameWithoutExtension(path)}";

                    EditorRoutine.StartRoutine(0.1f, GoView);
                }
            }

            if (PlayerPrefs.HasKey("IDE_Map_Open_State"))
            {
                var isOpen = bool.Parse(PlayerPrefs.GetString("IDE_Map_Open_State"));
                if (isOpen)
                {
                    miniMapBtn.ToggleInClassList("ide-toolbar__button__selected");
                    ieGraphView.ToggleMiniMap();
                }
            }
            
            if (PlayerPrefs.HasKey("IDE_DebugMode"))
            {
                DebugMode = bool.Parse(PlayerPrefs.GetString("IDE_DebugMode"));
                if (DebugMode)
                {
                    debugModeBtn.ToggleInClassList("ide-toolbar__button__selected");
                }
            }

            if (!PlayerPrefs.HasKey("IDE_PAGE"))
            {
                PlayerPrefs.SetInt("IDE_PAGE", 0);
                PlayerPrefs.Save();
            }

            AddActorMenu();
            AddStoryMenu();
            AddClanMenu();
            AddFamilyMenu();
            AddRuleMenu();
            IsPlaying();

            if (CurrentPage == GenericNodeType.Scheme)
            {
                nexusBtn.EnableInClassList("ide-toolbar__button__selected", true);
                ToggleView();
            }

            if (CurrentPage == GenericNodeType.Rule)
            {
                ruleBtn.EnableInClassList("ide-toolbar__button__selected", true);
                ToggleView();
            }

            if (CurrentPage == GenericNodeType.Family)
            {
                familyBtn.EnableInClassList("ide-toolbar__button__selected", true);
                ToggleView();
            }

            if (CurrentPage == GenericNodeType.Clan)
            {
                clanBtn.EnableInClassList("ide-toolbar__button__selected", true);
                ToggleView();
            }            
            
            if (CurrentPage == GenericNodeType.Policy)
            {
                policyBtn.EnableInClassList("ide-toolbar__button__selected", true);
                ToggleView();
            }

            if (CurrentPage == GenericNodeType.Actor)
            {
                characterBtn.EnableInClassList("ide-toolbar__button__selected", true);
                ToggleView();
            }

            if (CurrentPage == GenericNodeType.Variable)
            {
                variableBtn.EnableInClassList("ide-toolbar__button__selected", true);
                ToggleView();
            }

            if (CurrentPage == GenericNodeType.Culture)
            {
                cultureBtn.EnableInClassList("ide-toolbar__button__selected", true);
                ToggleView();
            }

            UnityEditor.Undo.undoRedoPerformed += Undo;
            rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
            {
                currentKey = evt.keyCode switch
                {
                    KeyCode.RightArrow => KeyType.Right,
                    KeyCode.LeftArrow => KeyType.Left,
                    KeyCode.UpArrow => KeyType.Up,
                    KeyCode.DownArrow => KeyType.Down,
                    _ => currentKey
                };
            });

            rootVisualElement.RegisterCallback<KeyUpEvent>(_ => { currentKey = KeyType.None; });

            EditorApplication.update += Update;
        }

        private void Update()
        {
            if (currentKey == KeyType.Right)
            {
                var currentPos = ieGraphView.contentViewContainer.transform.position;
                ieGraphView.contentViewContainer.transform.position = new Vector3(
                    Mathf.MoveTowards(currentPos.x, currentPos.x - 0.5f, Time.deltaTime), currentPos.y,
                    currentPos.z);
            }

            if (currentKey == KeyType.Left)
            {
                var currentPos = ieGraphView.contentViewContainer.transform.position;
                ieGraphView.contentViewContainer.transform.position = new Vector3(
                    Mathf.MoveTowards(currentPos.x, currentPos.x + 0.5f, Time.deltaTime), currentPos.y,
                    currentPos.z);
            }

            if (currentKey == KeyType.Up)
            {
                var currentPos = ieGraphView.contentViewContainer.transform.position;
                ieGraphView.contentViewContainer.transform.position = new Vector3(
                    currentPos.x, Mathf.MoveTowards(currentPos.y, currentPos.y + 0.5f, Time.deltaTime),
                    currentPos.z);
            }

            if (currentKey == KeyType.Down)
            {
                var currentPos = ieGraphView.contentViewContainer.transform.position;
                ieGraphView.contentViewContainer.transform.position = new Vector3(
                    currentPos.x, Mathf.MoveTowards(currentPos.y, currentPos.y - 0.5f, Time.deltaTime),
                    currentPos.z);
            }
        }

        private void OnDisable()
        {
            UnityEditor.Undo.undoRedoPerformed -= Undo;
            EditorApplication.update -= Update;
            ieGraphView = null;
        }

        private IERoutine undoRoutine;
        private int lastCultureFilterIndex;
        private int lastStateFilterIndex;
        private int lastAgeFilterIndex;

        private void Undo()
        {
            if (focusedWindow != this || refreshRate) return;
            GraphSaveUtility.LoadCurrent(ieGraphView);
            GetWindow<GraphWindow>().ToggleView(false);
            EditorRoutine.StartRoutine(0.05f, () => { refreshRate = false; });
            refreshRate = true;
        }

        private void OnGUI()
        {
            if (Application.isPlaying || CurrentDatabase == null)
                isPlayingPanel.Show();
            else
                isPlayingPanel.Hide();

            if (ieGraphView != null && ieGraphView.selection.Count(s => s is Node) > 0)
                selectionLabel.text = $"Selected Items: {ieGraphView.selection.Count(s => s is Node)} | ";
            else
                selectionLabel.text = string.Empty;
        }

        private void IsPlaying()
        {
            isPlayingPanel = new VisualElement()
            {
                style =
                {
                    flexGrow = 1,
                    display = new StyleEnum<DisplayStyle>(DisplayStyle.None),
                    fontSize = 28,
                    unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter),
                    position = new StyleEnum<Position>(Position.Absolute),
                    height = new StyleLength(Length.Percent(100)),
                    width = new StyleLength(Length.Percent(100)),
                    backgroundColor = new StyleColor(new Color(24f / 255, 24f / 255, 24f / 255, 0.7f))
                }
            };

            var str = CurrentDatabase == null ? "There is no selected database." : "Not allows in Play Mode..";
            var playingLabel = IEGraphUtility.CreateLabel(str);

            playingLabel.style.marginTop = 25f;

            isPlayingPanel.Add(playingLabel);

            rootVisualElement.Add(isPlayingPanel);
        }

        private void AddToolbar()
        {
            var toolbar = new Toolbar();
            currentDatabaseLabel = IEGraphUtility.CreateLabel(null);
            currentDatabaseLabel.style.alignSelf = new StyleEnum<Align>(Align.Center);
            currentDatabaseLabel.style.color = NullUtils.HTMLColor("#6C6C6C");
            currentDatabaseLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);

            selectionLabel = IEGraphUtility.CreateLabel(null);
            selectionLabel.style.alignSelf = new StyleEnum<Align>(Align.Center);
            selectionLabel.style.color = NullUtils.HTMLColor("#6C6C6C");
            selectionLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);

            var clearBtn = IEGraphUtility.CreateButton("Clear", () =>
            {
                if(!EditorUtility.DisplayDialog("Are you sure?", $"Are you sure you want to reset the database named {CurrentDatabase.name}?", "Yes", "No")) return;
                ieGraphView.ClearGraph();
                UnityEditor.Undo.RecordObject(CurrentDatabase, "IEGraph");
                CurrentDatabase.Reset();
                actorList.choices = new List<string> { "Actor: NULL" };
                storyList.choices = new List<string>() { "Intrigue: NULL" };
                ruleList.choices = new List<string>() { "Rule: NULL" };
                clanList.choices = new List<string>() { "Clan: NULL" };
                familyList.choices = new List<string>() { "Family: NULL" };
                actorList.index = 0;
                storyList.index = 0;
            });

            miniMapBtn = IEGraphUtility.CreateButton("Minimap", () =>
            {
                ieGraphView.ToggleMiniMap();
                miniMapBtn.ToggleInClassList("ide-toolbar__button__selected");
            });            
            
            debugModeBtn = IEGraphUtility.CreateButton("Debug Mode", () =>
            {
                DebugMode = !DebugMode;
                PlayerPrefs.SetString("IDE_DebugMode", DebugMode.ToString());
                debugModeBtn.ToggleInClassList("ide-toolbar__button__selected");
            });

            nexusBtn = IEGraphUtility.CreateButton("Schemes", () =>
            {
                CurrentPage = GenericNodeType.Scheme;
                DisableButtons();
                nexusBtn.EnableInClassList("ide-toolbar__button__selected", true);

                ToggleView();
            });

            ruleBtn = IEGraphUtility.CreateButton("Rules", () =>
            {
                CurrentPage = GenericNodeType.Rule;
                DisableButtons();
                ruleBtn.EnableInClassList("ide-toolbar__button__selected", true);

                ToggleView();
            });

            familyBtn = IEGraphUtility.CreateButton("Family Trees", () =>
            {
                CurrentPage = GenericNodeType.Family;
                DisableButtons();
                familyBtn.EnableInClassList("ide-toolbar__button__selected", true);

                ToggleView();
            });

            clanBtn = IEGraphUtility.CreateButton("Clans", () =>
            {
                CurrentPage = GenericNodeType.Clan;
                DisableButtons();
                clanBtn.EnableInClassList("ide-toolbar__button__selected", true);

                ToggleView();
            });            
            
            policyBtn = IEGraphUtility.CreateButton("Policies", () =>
            {
                CurrentPage = GenericNodeType.Policy;
                DisableButtons();
                policyBtn.EnableInClassList("ide-toolbar__button__selected", true);

                ToggleView();
            });

            characterBtn = IEGraphUtility.CreateButton("Actors", () =>
            {
                CurrentPage = GenericNodeType.Actor;
                DisableButtons();
                characterBtn.EnableInClassList("ide-toolbar__button__selected", true);

                ToggleView();
            });

            variableBtn = IEGraphUtility.CreateButton("Variables", () =>
            {
                CurrentPage = GenericNodeType.Variable;
                DisableButtons();
                variableBtn.EnableInClassList("ide-toolbar__button__selected", true);

                ToggleView();
            });

            cultureBtn = IEGraphUtility.CreateButton("Cultures", () =>
            {
                CurrentPage = GenericNodeType.Culture;
                DisableButtons();
                cultureBtn.EnableInClassList("ide-toolbar__button__selected", true);

                ToggleView();
            });

            var refreshBtn = IEGraphUtility.CreateButton("Reload", () =>
            {
                if (CurrentDatabase != null && !refreshRate)
                {
                    GraphSaveUtility.LoadCurrent(ieGraphView);
                    ToggleView(false);
                    EditorRoutine.StartRoutine(0.5f, () => { refreshRate = false; });
                    refreshRate = true;
                }
            });

            var logo = new VisualElement
            {
                style =
                {
                    width = 19,
                    height = 18,
                    alignSelf = new StyleEnum<Align>(Align.Center)
                }
            };

            var line = new Label("|")
            {
                style =
                {
                    alignSelf = new StyleEnum<Align>(Align.Center),
                    unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold),
                    marginLeft = 6,
                    marginRight = 2,
                    color = NullUtils.HTMLColor("#6C6C6C")
                }
            };

            var texture = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/miniLogo.png");
            logo.style.backgroundImage = new StyleBackground(texture);

            toolbar.Add(logo);
            toolbar.Add(line);
            toolbar.Add(selectionLabel);
            toolbar.Add(currentDatabaseLabel);
            toolbar.Add(nexusBtn);
            toolbar.Add(ruleBtn);
            toolbar.Add(familyBtn);
            toolbar.Add(clanBtn);
            toolbar.Add(policyBtn);
            toolbar.Add(characterBtn);
            toolbar.Add(cultureBtn);
            // toolbar.Add(themeBtn);
            toolbar.Add(variableBtn);
            toolbar.Add(miniMapBtn);
            // toolbar.Add(debugModeBtn);
            toolbar.Add(refreshBtn);
            toolbar.Add(clearBtn);

            toolbar.AddStyleSheets("Nullframes/IEToolbarStyles.uss");

            rootVisualElement.Add(toolbar);
        }

        public void GotoActor(string actorid)
        {
            SetActorKey(actorid);

            if (CurrentPage != GenericNodeType.Actor)
            {
                CurrentPage = GenericNodeType.Actor;
                DisableButtons();
                characterBtn.EnableInClassList("ide-toolbar__button__selected", true);
                ToggleView();
            }

            GraphSaveUtility.LoadCurrent(ieGraphView);

            var element = ieGraphView.graphElements.FirstOrDefault(e =>
                e is ActorNode actorNode && actorNode.ID == actorid);
            
            if (element != null)
            {
                ieGraphView.ClearSelection();
                ieGraphView.AddToSelection(element);
                element.Focus();
            }
        }

        public void GotoFamilyMember(string familyid, string actorid)
        {
            SetFamilyKey(familyid);

            if (CurrentPage != GenericNodeType.Family)
            {
                CurrentPage = GenericNodeType.Family;
                DisableButtons();
                familyBtn.EnableInClassList("ide-toolbar__button__selected", true);
                ToggleView(false);
            }

            GraphSaveUtility.LoadCurrent(ieGraphView);

            var element = ieGraphView.graphElements.FirstOrDefault(e =>
                e is FamilyMemberNode actorNode && actorNode.ActorID == actorid);
            
            if (element != null)
            {
                ieGraphView.ClearSelection();
                ieGraphView.AddToSelection(element);
                element.Focus();

                EditorRoutine.StartRoutine(0.03f, () => ieGraphView.Fit(e => e == element));
            }
        }

        public void GotoClanMember(string clanid, string actorid)
        {
            SetClanKey(clanid);

            if (CurrentPage != GenericNodeType.Clan)
            {
                CurrentPage = GenericNodeType.Clan;
                DisableButtons();
                clanBtn.EnableInClassList("ide-toolbar__button__selected", true);
                ToggleView(false);
            }

            GraphSaveUtility.LoadCurrent(ieGraphView);

            var element = ieGraphView.graphElements.FirstOrDefault(e =>
                e is ClanMemberNode actorNode && actorNode.ActorID == actorid);
            
            if (element != null)
            {
                ieGraphView.ClearSelection();
                ieGraphView.AddToSelection(element);
                element.Focus();

                EditorRoutine.StartRoutine(0.03f, () => ieGraphView.Fit(e => e == element));
            }
        }

        private void LoadActorRightMenu()
        {
            if (creationPanel != null)
            {
                if(childRoot.Contains(creationPanel))
                    childRoot.Remove(creationPanel);
            }

            creationPanel = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            creationPanel.AddStyleSheets("Nullframes/GraphView_TopDown.uss");
            creationPanel.AddClasses("root");

            var arrowIcon = new VisualElement
            {
                style =
                {
                    width = 128,
                    height = 128
                },
                userData = false
            };

            arrowIcon.AddClasses("arrow");

            var panel = new ScrollView
            {
                mode = ScrollViewMode.Vertical,
            };
            panel.AddClasses("panel");

            var leftArrow = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/leftArrow.png");
            arrowIcon.style.backgroundImage = new StyleBackground(leftArrow);

            void Toggle()
            {
                if (creationMenuOpened)
                {
                    panel.Show();
                    arrowIcon.style.rotate = new StyleRotate(new Rotate(180));
                }
                else
                {
                    panel.Hide();
                    arrowIcon.style.rotate = new StyleRotate(new Rotate(0));
                }
            }

            arrowIcon.RegisterCallback<MouseDownEvent>(_ =>
            {
                creationMenuOpened = !creationMenuOpened;

                Toggle();
            });

            var actorMenu = IEGraphUtility.CreateLabel("Actor Menu");
            actorMenu.AddClasses("title");

            var searchBox =
                IEGraphUtility.CreateTextField(string.IsNullOrEmpty(searchStr) ? "Search by name.." : searchStr);
            searchBox.AddClasses("searchBox");

            var characterList = new ScrollView
            {
                mode = ScrollViewMode.Vertical,
                scrollOffset = lastScrollRect
            };
            characterList.AddClasses("characterList");

            characterList.verticalScroller.RegisterCallback<ChangeEvent<float>>(_ =>
            {
                lastScrollRect = characterList.scrollOffset;
            });
            
            var scrollContainer = characterList.Q<VisualElement>("unity-content-container");
            scrollContainer.AddClasses("scrollContainer");

            #region FILTER

            var filterBox = new VisualElement()
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                }
            };
            
            var cultureFilter = IEGraphUtility.CreateDropdown(null);

            cultureFilter.choices = new List<string>() { "Culture Filter", "None" };
            cultureFilter.choices.AddRange(CurrentDatabase.culturalProfiles.Select(c => c.CultureName));
            cultureFilter.index = lastCultureFilterIndex;

            var stateFilter = IEGraphUtility.CreateDropdown(null);

            stateFilter.choices = new List<string>() { "State Filter", "Active", "Passive(Dead)" };
            stateFilter.index = lastStateFilterIndex;

            var ageFilter = IEGraphUtility.CreateDropdown(null);

            ageFilter.choices = new List<string>() { "Age Filter", "Descending", "Ascending" };
            ageFilter.index = lastAgeFilterIndex;

            filterBox.Add(cultureFilter);
            filterBox.Add(stateFilter);
            filterBox.Add(ageFilter);

            void Filter()
            {
                var list = CurrentDatabase.nodeDataList.OfType<ActorData>();

                if (cultureFilter.index != 0)
                {
                    if (cultureFilter.index == 1)
                    {
                        var cultureIds = CurrentDatabase.culturalProfiles.Select(c => c.ID);
                        list = list.Where(a => !cultureIds.Contains(a.CultureID));
                    }
                    else
                    {
                        var cultureIds = CurrentDatabase.culturalProfiles.FindAll(c =>
                                c.CultureName.Contains(cultureFilter.value, StringComparison.OrdinalIgnoreCase))
                            .Select(c => c.ID);

                        list = list.Where(a => cultureIds.Contains(a.CultureID));
                    }
                }

                //Search by Name
                if (!string.IsNullOrEmpty(searchStr))
                {
                    list = list.Where(n =>
                        n.ActorName.Contains(searchStr, StringComparison.OrdinalIgnoreCase));
                }

                if (stateFilter.index != 0)
                {
                    list = list.Where(a => a.State == (Actor.IState)stateFilter.index - 1);
                }

                if (ageFilter.index != 0)
                {
                    list = ageFilter.index == 1 ? list.OrderByDescending(a => a.Age) : list.OrderBy(a => a.Age);
                }

                //Player is First
                list = list.OrderByDescending(a => a.IsPlayer);
                LoadCharacters(list);
            }

            cultureFilter.RegisterValueChangedCallback(_ =>
            {
                lastCultureFilterIndex = cultureFilter.index;
                Filter();
            });
            stateFilter.RegisterValueChangedCallback(_ =>
            {
                lastStateFilterIndex = stateFilter.index;
                Filter();
            });
            ageFilter.RegisterValueChangedCallback(_ =>
            {
                lastAgeFilterIndex = ageFilter.index;
                Filter();
            });

            #endregion

            CurrentDatabase.ageClassificationTable ??= new List<AgeSuffix>();
            var suffixes = CurrentDatabase.ageClassificationTable;

            var btns = new VisualElement()
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignSelf = new StyleEnum<Align>(Align.Center)
                }
            };

            var createGameObjects = IEGraphUtility.CreateButton("Import Actors to Scene", () =>
            {
                var actors = FindObjectsOfType<Actor>().Select(a => a.ID).ToList();
                var actorIds = new List<ActorData>();
                foreach (var character in characterList.Children())
                {
                    var data = (ActorData)character.userData;
                    if (actors.Contains(data.ID)) continue;
                    actorIds.Add(data);
                }

                if (actorIds.Count < 1) return;
                if (EditorUtility.DisplayDialog("Actor GameObjects",
                        $"{actorIds.Count} actor objects named '{SceneManager.GetActiveScene().name}' will be added to the scene.",
                        "Yes", "Cancel"))
                {
                    foreach (var data in actorIds.OrderByDescending(a => a.IsPlayer).ThenBy(a => a.CultureID))
                    {
                        var culture = CurrentDatabase.culturalProfiles.Find(c => c.ID == data.CultureID);
                        var cultureName = culture == null ? "None" : culture.CultureName;
                        var goName = data.IsPlayer ? $"{data.ActorName}(Player)" : $"{data.ActorName}({cultureName})";
                        var gObject = new GameObject(goName);
                        InitialActor initialActor = gObject.AddComponent<InitialActor>();
                        initialActor.SerializedProperty(o =>
                        {
                            var id = o.FindProperty("id");
                            var isPlayer = o.FindProperty("isPlayer");
                            id.stringValue = data.ID;
                            isPlayer.boolValue = data.IsPlayer;
                        });
                    }
                }
            });

            createGameObjects.AddClasses("createBtn");

            var clearActors = IEGraphUtility.CreateButton("Clear Actors", () =>
            {
                if (EditorUtility.DisplayDialog("Clear",
                        $"Are you sure you want to delete all {CurrentDatabase.actorRegistry.Count} actors, {CurrentDatabase.nodeDataList.Count(n => n is ClanMemberData)} clan members, and {CurrentDatabase.nodeDataList.Count(n => n is FamilyMemberData)} family members? This action can be undone.",
                        "Yes", "Cancel"))
                {
                    UnityEditor.Undo.RecordObject(CurrentDatabase, "Clear Actor Operation");
                    CurrentDatabase.nodeDataList.RemoveAll(n => n is ActorData or FamilyMemberData or ClanMemberData);
                    CurrentDatabase.actorRegistry.Clear();
                    EditorUtility.SetDirty(CurrentDatabase);

                    GraphSaveUtility.LoadCurrent(ieGraphView);
                    ToggleView(false);
                }
            });
            clearActors.AddClasses("clearBtn");

            btns.Add(createGameObjects);
            btns.Add(clearActors);

            Filter();

            if (CurrentDatabase.actorRegistry.Count < 1)
            {
                actorMenu.Hide();
                searchBox.Hide();
                characterList.Hide();
                btns.Hide();
            }

            void LoadCharacters(IEnumerable list)
            {
                characterList.Clear();

                EditorRoutine.StartRoutine(() => characterList.childCount == 0, () =>
                {
                    foreach (var data in list)
                    {
                        var actorData = (ActorData)data;

                        var character = new VisualElement()
                        {
                            style =
                            {
                                backgroundImage = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/none.png"),
                            }
                        };
                        character.AddClasses("character");

                        character.AddManipulator(new ContextualMenuManipulator(evt =>
                        {
                            evt.menu.AppendAction("Focus GameObject", _ =>
                            {
                                var actorObj = FindObjectsOfType<Actor>().FirstOrDefault(a => a.ID == actorData.ID);
                                Selection.activeObject = actorObj;
                                EditorGUIUtility.PingObject(actorObj);
                            });
                            
                            evt.menu.AppendAction("Go to Family", _ =>
                            {
                                var groupId = CurrentDatabase.nodeDataList.FirstOrDefault(n => n is FamilyMemberData fData && !string.IsNullOrEmpty(n.GroupId) && fData.ActorID == actorData.ID);
                                if (groupId != null)
                                {
                                    GotoFamilyMember(groupId.GroupId, actorData.ID);
                                }
                            });
                            
                            evt.menu.AppendAction("Go to Clan", _ =>
                            {
                                var groupId = CurrentDatabase.nodeDataList.FirstOrDefault(n => n is ClanMemberData fData && !string.IsNullOrEmpty(n.GroupId) && fData.ActorID == actorData.ID);
                                if (groupId != null)
                                {
                                    GotoClanMember(groupId.GroupId, actorData.ID);
                                }
                            });
                        }));

                        character.tooltip +=
                            $"Name: {actorData.ActorName}\nAge: {actorData.Age}\nGender: {actorData.Gender}";
                        var culture = CurrentDatabase.culturalProfiles.Find(c => c.ID == actorData.CultureID);
                        character.tooltip += $"\nCulture: {culture?.CultureName}";

                        if (actorData.IsPlayer)
                        {
                            character.SetBorderColor(NullUtils.HTMLColor("#FF9E00"));
                        }

                        character.RegisterCallback<MouseDownEvent>(evt =>
                        {
                            if (evt.button != 0) return;
                            SetActorKey(actorData.ID);
                            GraphSaveUtility.LoadCurrent(ieGraphView);
                            GoView();

                            var element = ieGraphView.graphElements.FirstOrDefault(e =>
                                e is ActorNode actorNode && actorNode.ID == actorData.ID);
                            if (element != null)
                            {
                                ieGraphView.ClearSelection();
                                ieGraphView.AddToSelection(element);
                                element.Focus();
                            }
                        });

                        if (actorData.Portrait != null)
                        {
                            character.style.backgroundImage = new StyleBackground(actorData.Portrait);
                        }

                        character.userData = actorData;

                        characterList.Add(character);
                    }
                });
            }

            var actorCreation = IEGraphUtility.CreateLabel("Actor Creation<size=14>(Drag Area)</size>");
            actorCreation.style.marginBottom = 10;
            actorCreation.AddClasses("title");

            var dropArea = new VisualElement()
            {
                style =
                {
                    marginBottom = 10,
                    minWidth = 48,
                    minHeight = 48,
                    alignSelf = new StyleEnum<Align>(Align.Center),
                    unityBackgroundImageTintColor = new Color(255f, 255f, 255f, 0.5f)
                }
            };

            _ = new DragAndDropManipulator(dropArea, typeof(Texture2D), objects =>
            {
                if (objects.Length > 0)
                {
                    UnityEditor.Undo.RecordObject(CurrentDatabase, "GenerateActor");
                }

                for (int i = 0; i < objects.Length; i++)
                {
                    var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(objects[i]);

                    if (CurrentDatabase.actorRegistry.Exists(a => a.Portrait == sprite))
                    {
                        if (EditorUtility.DisplayDialog("Same Portrait",
                                "There is another actor with the same portrait. Do you want to proceed?", "Yes",
                                "Cancel"))
                        {
                            Add();
                        }

                        continue;
                    }

                    Add();

                    void Add()
                    {
                        var pattern = suffixes.Aggregate(@"-m\b|-f\b|-dead\b|-all\b",
                            (current, suffix) =>
                                current + @$"|-{suffix.Suffix.ToLower(CultureInfo.InvariantCulture)}\b");

                        pattern = CurrentDatabase.culturalProfiles.Aggregate(pattern,
                            (current, culture) =>
                                current + @$"|-{culture.CultureName.ToLower(CultureInfo.InvariantCulture)}\b");

                        var matches = Regex.Matches(sprite.name, pattern);

                        object culture = null;
                        object suffix = null;
                        object gender = null;
                        object isDead = null;
                        object isAll = null;

                        foreach (Match match in matches)
                        {
                            if (gender is null)
                            {
                                if (match.Value == "-f")
                                {
                                    gender = Actor.IGender.Female;
                                }

                                if (match.Value == "-m")
                                {
                                    gender = Actor.IGender.Male;
                                }
                            }

                            if (isDead is null)
                            {
                                if (match.Value == "-dead")
                                {
                                    isDead = true;
                                }
                            }

                            if (suffix is null)
                            {
                                var index = suffixes.FindIndex(s => $"-{s.Suffix}" == match.Value);
                                if (index != -1)
                                {
                                    suffix = suffixes[index];
                                }
                            }

                            if (isAll is null)
                            {
                                if (match.Value == "-all")
                                {
                                    isAll = true;
                                }
                            }

                            if (culture is null && isAll == null)
                            {
                                var index = CurrentDatabase.culturalProfiles.FindIndex(s =>
                                    $"-{s.CultureName}".ToLower(CultureInfo.InvariantCulture) ==
                                    match.Value.ToLower(CultureInfo.InvariantCulture));
                                if (index != -1)
                                {
                                    culture = CurrentDatabase.culturalProfiles[index];
                                }
                            }
                        }

                        ActorData actorData = null;

                        if (isAll != null)
                        {
                            foreach (var cl in CurrentDatabase.culturalProfiles)
                            {
                                var _gender = gender is null ? Actor.IGender.Male : (Actor.IGender)gender;
                                var _state = isDead is null ? Actor.IState.Active : Actor.IState.Passive;
                                var _name = cl.GenerateName(_gender);
                                var _age = suffix is null
                                    ? 0
                                    : UnityEngine.Random.Range(((AgeSuffix)suffix).MinValue,
                                        ((AgeSuffix)suffix).MaxValue);
                                var _cultureId = cl.ID;

                                actorData = new ActorData(NullUtils.GenerateID(), Vector2.one, _name, _age, _gender,
                                    _state,
                                    _cultureId, sprite, false);

                                CurrentDatabase.nodeDataList.Add(actorData);

                                GraphSaveUtility.SaveActorItem(actorData);
                            }
                        }
                        else
                        {
                            var _gender = gender is null ? Actor.IGender.Male : (Actor.IGender)gender;
                            var _state = isDead is null ? Actor.IState.Active : Actor.IState.Passive;
                            var _name = culture is null ? "Actor" : ((Culture)culture).GenerateName(_gender);
                            var _age = suffix is null
                                ? 0
                                : UnityEngine.Random.Range(((AgeSuffix)suffix).MinValue, ((AgeSuffix)suffix).MaxValue);
                            var _cultureId = culture is null ? string.Empty : ((Culture)culture).ID;

                            actorData = new ActorData(NullUtils.GenerateID(), Vector2.one, _name, _age, _gender, _state,
                                _cultureId, sprite, false);

                            CurrentDatabase.nodeDataList.Add(actorData);

                            GraphSaveUtility.SaveActorItem(actorData);
                        }


                        if (i == objects.Length - 1 && actorData != null)
                        {
                            EditorUtility.SetDirty(CurrentDatabase);

                            SetActorKey(actorData.ID);
                            GraphSaveUtility.LoadCurrent(ieGraphView);

                            LoadActorRightMenu();

                            ieGraphView.ClearSelection();
                            ieGraphView.AddToSelection(ieGraphView.graphElements.First());

                            GoView();
                        }
                    }
                }
            });

            var texture = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/action_choice.png");
            dropArea.style.backgroundImage = new StyleBackground(texture);

            var defaultSuffix =
                IEGraphUtility.CreateLabel(
                    "-m (Male)\n-f (Female)\n-dead (State)\n-{culturename} (Sets Culture)\n-all (Adds all cultures)");
            defaultSuffix.AddClasses("defaultSuffix");

            var suffixBox = new ScrollView()
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column),
                    flexGrow = 1,
                },
                mode = ScrollViewMode.Vertical
            };

            foreach (var suffix in suffixes)
            {
                CreateSuffix(suffix.Suffix, suffix.MinValue, suffix.MaxValue);
            }

            var addSuffix = IEGraphUtility.CreateButton("Add Suffix", () => { CreateSuffix(null, 25, 45); });

            addSuffix.style.alignSelf = new StyleEnum<Align>(Align.FlexEnd);

            void CreateSuffix(string suffx, int minValue, int maxValue)
            {
                var suffixField = new VisualElement()
                {
                    style =
                    {
                        marginTop = 10,
                        flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    }
                };

                var suffixLabel = IEGraphUtility.CreateLabel("Suffix: ");
                suffixLabel.AddClasses("suffixLabel");

                var suffix = IEGraphUtility.CreateTextField(suffx);
                suffix.AddClasses("suffix");

                int index = suffixes.FindIndex(s => s.Suffix == suffx);
                if (index != -1)
                {
                    suffix.userData = suffixes[index];
                }

                var ageRange = new MinMaxSlider(minValue, maxValue, 0, STATIC.MAX_AGE);
                ageRange.AddClasses("ageRange");

                var minmax = IEGraphUtility.CreateLabel(string.Empty);
                minmax.AddClasses("rangeLabel");

                minmax.text =
                    Mathf.RoundToInt(ageRange.minValue).ToString(CultureInfo.InvariantCulture) + " / " +
                    Mathf.RoundToInt(ageRange.maxValue).ToString(CultureInfo.InvariantCulture);

                ageRange.RegisterValueChangedCallback(_ =>
                {
                    minmax.text =
                        Mathf.RoundToInt(ageRange.minValue).ToString(CultureInfo.InvariantCulture) + " / " +
                        Mathf.RoundToInt(ageRange.maxValue).ToString(CultureInfo.InvariantCulture);

                    if (suffix.userData is AgeSuffix currentSuffix)
                    {
                        currentSuffix.MinValue = Mathf.RoundToInt(ageRange.minValue);
                        currentSuffix.MaxValue = Mathf.RoundToInt(ageRange.maxValue);

                        EditorUtility.SetDirty(CurrentDatabase);
                    }
                });

                suffix.RegisterCallback<FocusOutEvent>(_ =>
                {
                    if (suffix.value == "age" || suffix.value == "dead" || suffix.value == "m" ||
                        suffix.value == "f" || suffix.value == "all" || CurrentDatabase.culturalProfiles.Exists(c =>
                            c.CultureName.ToLower(CultureInfo.InvariantCulture) ==
                            suffix.value.ToLower(CultureInfo.InvariantCulture)))
                    {
                        suffix.value = string.Empty;
                    }

                    if (string.IsNullOrEmpty(suffix.value))
                    {
                        suffixBox.Remove(suffixField);
                        if (suffix.userData != null)
                        {
                            suffixes.Remove((AgeSuffix)suffix.userData);
                        }

                        return;
                    }

                    if (suffix.userData == null)
                    {
                        var nSuffix = new AgeSuffix(suffix.value, Mathf.RoundToInt(ageRange.minValue),
                            Mathf.RoundToInt(ageRange.maxValue));
                        suffixes.Add(nSuffix);
                        suffix.userData = nSuffix;
                        EditorUtility.SetDirty(CurrentDatabase);
                    }
                    else
                    {
                        var lSuffix = (AgeSuffix)suffix.userData;
                        if (lSuffix.Suffix == suffix.value) return;
                        lSuffix.Suffix = suffix.value;
                        EditorUtility.SetDirty(CurrentDatabase);
                    }
                });

                suffix.RegisterCallback<ChangeEvent<string>>(evt =>
                {
                    suffix.value = suffix.value.RemoveWhitespaces().RemoveSpecialCharacters();

                    if (suffixes.Count(s => s != suffix.userData && s.Suffix == evt.newValue) > 0)
                    {
                        suffix.value = evt.previousValue;
                    }
                });

                suffixField.Add(suffixLabel);
                suffixField.Add(suffix);
                suffixField.Add(ageRange);
                suffixField.Add(minmax);

                suffixBox.Add(suffixField);
            }

            #region SEARCH

            searchBox.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                searchStr = searchBox.value;
                Filter();
            });

            searchBox.RegisterCallback<FocusInEvent>(_ =>
            {
                if (string.IsNullOrEmpty(searchStr))
                {
                    searchBox.SetValueWithoutNotify(string.Empty);
                }
            });

            searchBox.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (string.IsNullOrEmpty(searchStr))
                {
                    searchBox.SetValueWithoutNotify("Search by name..");
                }
            });

            #endregion

            Toggle();

            panel.Add(actorMenu);
            panel.Add(searchBox);
            panel.Add(filterBox);
            panel.Add(characterList);
            panel.Add(btns);
            panel.Add(actorCreation);
            panel.Add(dropArea);
            panel.Add(defaultSuffix);
            panel.Add(addSuffix);
            panel.Add(suffixBox);

            creationPanel.Add(arrowIcon);
            creationPanel.Add(panel);

            childRoot.Add(creationPanel);
            
            lastScrollRect = characterList.scrollOffset;
        }

        public void LoadClanRightMenu()
        {
            if (clanPanel != null)
            {
                childRoot.Remove(clanPanel);
            }

            clanPanel = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            clanPanel.AddStyleSheets("Nullframes/GraphView_TopDown.uss");
            clanPanel.AddClasses("root");

            var arrowIcon = new VisualElement
            {
                style =
                {
                    width = 128,
                    height = 128
                },
                userData = false
            };

            arrowIcon.AddClasses("arrow");

            var panel = new ScrollView
            {
                mode = ScrollViewMode.Vertical,
                scrollOffset = lastClanScrollRect
            };
            panel.AddClasses("panel");
            
            panel.verticalScroller.RegisterCallback<ChangeEvent<float>>(_ =>
            {
                lastClanScrollRect = panel.scrollOffset;
            });
            
            var leftArrow = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/leftArrow.png");
            arrowIcon.style.backgroundImage = new StyleBackground(leftArrow);

            void Toggle()
            {
                if (creationMenuOpened)
                {
                    panel.Show();
                    arrowIcon.style.rotate = new StyleRotate(new Rotate(180));
                }
                else
                {
                    panel.Hide();
                    arrowIcon.style.rotate = new StyleRotate(new Rotate(0));
                }
            }

            arrowIcon.RegisterCallback<MouseDownEvent>(_ =>
            {
                creationMenuOpened = !creationMenuOpened;

                Toggle();
            });

            var actorMenu = IEGraphUtility.CreateLabel("Actor Menu");
            actorMenu.AddClasses("title");

            var searchBox =
                IEGraphUtility.CreateTextField(string.IsNullOrEmpty(searchStr) ? "Search by name.." : searchStr);
            searchBox.AddClasses("searchBox");

            var characterList = new ScrollView
            {
                mode = ScrollViewMode.Vertical,
            };
            characterList.AddClasses("characterListEx");

            var scrollContainer = characterList.Q<VisualElement>("unity-content-container");
            scrollContainer.AddClasses("scrollContainer");

            #region FILTER

            var filterBox = new VisualElement()
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                }
            };

            var cultureFilter = IEGraphUtility.CreateDropdown(null);

            cultureFilter.choices = new List<string>() { "Culture Filter", "None" };
            cultureFilter.choices.AddRange(CurrentDatabase.culturalProfiles.Select(c => c.CultureName));
            cultureFilter.index = lastCultureFilterIndex;

            var stateFilter = IEGraphUtility.CreateDropdown(null);

            stateFilter.choices = new List<string>() { "State Filter", "Active", "Passive(Dead)" };
            stateFilter.index = lastStateFilterIndex;

            var ageFilter = IEGraphUtility.CreateDropdown(null);

            ageFilter.choices = new List<string>() { "Age Filter", "Descending", "Ascending" };
            ageFilter.index = lastAgeFilterIndex;

            filterBox.Add(cultureFilter);
            filterBox.Add(stateFilter);
            filterBox.Add(ageFilter);

            void Filter()
            {
                var list = CurrentDatabase.nodeDataList.OfType<ActorData>();

                if (cultureFilter.index != 0)
                {
                    if (cultureFilter.index == 1)
                    {
                        var cultureIds = CurrentDatabase.culturalProfiles.Select(c => c.ID);
                        list = list.Where(a => !cultureIds.Contains(a.CultureID));
                    }
                    else
                    {
                        var cultureIds = CurrentDatabase.culturalProfiles.FindAll(c =>
                                c.CultureName.Contains(cultureFilter.value, StringComparison.OrdinalIgnoreCase))
                            .Select(c => c.ID);

                        list = list.Where(a => cultureIds.Contains(a.CultureID));
                    }
                }

                //Search by Name
                if (!string.IsNullOrEmpty(searchStr))
                {
                    list = list.Where(n =>
                        n.ActorName.Contains(searchStr, StringComparison.OrdinalIgnoreCase));
                }

                if (stateFilter.index != 0)
                {
                    list = list.Where(a => a.State == (Actor.IState)stateFilter.index - 1);
                }

                if (ageFilter.index != 0)
                {
                    list = ageFilter.index == 1 ? list.OrderByDescending(a => a.Age) : list.OrderBy(a => a.Age);
                }

                //Player is First
                list = list.OrderByDescending(a => a.IsPlayer);
                LoadCharacters(list);
            }

            cultureFilter.RegisterValueChangedCallback(_ =>
            {
                lastCultureFilterIndex = cultureFilter.index;
                Filter();
            });
            stateFilter.RegisterValueChangedCallback(_ =>
            {
                lastStateFilterIndex = stateFilter.index;
                Filter();
            });
            ageFilter.RegisterValueChangedCallback(_ =>
            {
                lastAgeFilterIndex = ageFilter.index;
                Filter();
            });

            #endregion

            Filter();

            if (characterList.childCount == 0)
            {
                characterList.Hide();
            }

            void LoadCharacters(IEnumerable list)
            {
                characterList.Clear();

                foreach (var data in list)
                {
                    var actorData = (ActorData)data;

                    var character = new VisualElement()
                    {
                        style =
                        {
                            backgroundImage = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/none.png"),
                        }
                    };
                    character.AddClasses("character");

                    character.tooltip +=
                        $"Name: {actorData.ActorName}\nAge: {actorData.Age}\nGender: {actorData.Gender}";
                    var culture = CurrentDatabase.culturalProfiles.Find(c => c.ID == actorData.CultureID);
                    character.tooltip += $"\nCulture: {culture?.CultureName}";

                    if (actorData.IsPlayer)
                    {
                        character.SetBorderColor(NullUtils.HTMLColor("#FF9E00"));
                    }

                    var memberNode = CurrentDatabase.nodeDataList.OfType<ClanMemberData>()
                        .FirstOrDefault(c => c.ActorID == actorData.ID);
                    if (memberNode != null)
                    {
                        if (!actorData.IsPlayer) character.SetBorderColor(NullUtils.HTMLColor("#A44343"));
                        character.userData = true;
                    }

                    character.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        if (evt.button != 0) return;

                        IGroup group = (IGroup)ieGraphView.graphElements.FirstOrDefault(g => g is ClanGroup);

                        if (character.userData != null)
                        {
                            if (memberNode == null) return;

                            if (group?.ID != memberNode.GroupId || group == null)
                            {
                                SetClanKey(memberNode.GroupId);
                                GraphSaveUtility.LoadCurrent(ieGraphView);

                                var getNode = ieGraphView.graphElements.OfType<ClanMemberNode>()
                                    .FirstOrDefault(c => c.ID == memberNode.ID);

                                if (getNode != null)
                                {
                                    ieGraphView.ClearSelection();
                                    ieGraphView.AddToSelection(getNode);
                                    getNode.Focus();
                                    EditorRoutine.StartRoutine(0.03f,
                                        () => ieGraphView.Fit(element => element == getNode));
                                }
                            }
                            else
                            {
                                var getNode = ieGraphView.graphElements.OfType<ClanMemberNode>()
                                    .FirstOrDefault(c => c.ID == memberNode.ID);

                                if (getNode != null)
                                {
                                    ieGraphView.ClearSelection();
                                    ieGraphView.AddToSelection(getNode);
                                    ieGraphView.Fit(element => element == getNode);
                                    getNode.Focus();
                                }
                            }

                            return;
                        }

                        if (group != null)
                        {
                            var node = ieGraphView.CreateNode<ClanMemberNode>(
                                ieGraphView.selection.LastOrDefault() is not INode lastSelection
                                    ? group.containedElements.LastOrDefault() is not INode lastElement
                                        ? group.GetPosition().position
                                        : new Vector2(lastElement.GetPosition().position.x + lastElement.layout.width,
                                            lastElement.GetPosition().y)
                                    : new Vector2(lastSelection.GetPosition().position.x + lastSelection.layout.width,
                                        lastSelection.GetPosition().y), false);
                            node.ActorID = actorData.ID;

                            node.Group = group;
                            group.AddElement(node);
                            ieGraphView.AddElement(node);

                            node.Draw();
                            node.OnCreated();

                            GraphSaveUtility.SaveCurrent();

                            character.SetBorderColor(NullUtils.HTMLColor("#A44343"));
                            character.userData = true;

                            ieGraphView.ClearSelection();
                            ieGraphView.AddToSelection(node);
                            node.Focus();

                            EditorRoutine.StartRoutine(0.01f, () => { ieGraphView.Fit(element => element == node); });

                            memberNode = CurrentDatabase.nodeDataList.OfType<ClanMemberData>()
                                .FirstOrDefault(c => c.ActorID == actorData.ID);
                        }
                    });

                    if (actorData.Portrait != null)
                    {
                        character.style.backgroundImage = new StyleBackground(actorData.Portrait);
                    }

                    characterList.Add(character);
                }
            }

            searchBox.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                searchStr = searchBox.value;
                Filter();
            });

            searchBox.RegisterCallback<FocusInEvent>(_ =>
            {
                if (string.IsNullOrEmpty(searchStr))
                {
                    searchBox.SetValueWithoutNotify(string.Empty);
                }
            });

            searchBox.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (string.IsNullOrEmpty(searchStr))
                {
                    searchBox.SetValueWithoutNotify("Search by name..");
                }
            });

            Toggle();

            panel.Add(actorMenu);
            panel.Add(searchBox);
            panel.Add(filterBox);
            panel.Add(characterList);

            clanPanel.Add(arrowIcon);
            clanPanel.Add(panel);

            childRoot.Add(clanPanel);
            
            lastClanScrollRect = panel.scrollOffset;
        }

        public void LoadFamilyRightMenu()
        {
            if (familyPanel != null)
            {
                childRoot.Remove(familyPanel);
            }

            familyPanel = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            familyPanel.AddStyleSheets("Nullframes/GraphView_TopDown.uss");
            familyPanel.AddClasses("root");

            var arrowIcon = new VisualElement
            {
                style =
                {
                    width = 128,
                    height = 128
                },
                userData = false
            };

            arrowIcon.AddClasses("arrow");

            var panel = new ScrollView
            {
                mode = ScrollViewMode.Vertical,
                scrollOffset = lastFamilyScrollRect
            };
            panel.AddClasses("panel");
            
            panel.verticalScroller.RegisterCallback<ChangeEvent<float>>(_ =>
            {
                lastFamilyScrollRect = panel.scrollOffset;
            });
            
            var leftArrow = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/leftArrow.png");
            arrowIcon.style.backgroundImage = new StyleBackground(leftArrow);

            void Toggle()
            {
                if (creationMenuOpened)
                {
                    panel.Show();
                    arrowIcon.style.rotate = new StyleRotate(new Rotate(180));
                }
                else
                {
                    panel.Hide();
                    arrowIcon.style.rotate = new StyleRotate(new Rotate(0));
                }
            }

            arrowIcon.RegisterCallback<MouseDownEvent>(_ =>
            {
                creationMenuOpened = !creationMenuOpened;

                Toggle();
            });

            var actorMenu = IEGraphUtility.CreateLabel("Actor Menu");
            actorMenu.AddClasses("title");

            var searchBox =
                IEGraphUtility.CreateTextField(string.IsNullOrEmpty(searchStr) ? "Search by name.." : searchStr);
            searchBox.AddClasses("searchBox");

            var characterList = new ScrollView
            {
                mode = ScrollViewMode.Vertical,
            };
            characterList.AddClasses("characterListEx");

            var scrollContainer = characterList.Q<VisualElement>("unity-content-container");
            scrollContainer.AddClasses("scrollContainer");

            #region FILTER

            var filterBox = new VisualElement()
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                }
            };

            var cultureFilter = IEGraphUtility.CreateDropdown(null);

            cultureFilter.choices = new List<string>() { "Culture Filter", "None" };
            cultureFilter.choices.AddRange(CurrentDatabase.culturalProfiles.Select(c => c.CultureName));
            cultureFilter.index = lastCultureFilterIndex;

            var stateFilter = IEGraphUtility.CreateDropdown(null);

            stateFilter.choices = new List<string>() { "State Filter", "Active", "Passive(Dead)" };
            stateFilter.index = lastStateFilterIndex;

            var ageFilter = IEGraphUtility.CreateDropdown(null);

            ageFilter.choices = new List<string>() { "Age Filter", "Descending", "Ascending" };
            ageFilter.index = lastAgeFilterIndex;

            filterBox.Add(cultureFilter);
            filterBox.Add(stateFilter);
            filterBox.Add(ageFilter);

            void Filter()
            {
                var list = CurrentDatabase.nodeDataList.OfType<ActorData>();

                if (cultureFilter.index != 0)
                {
                    if (cultureFilter.index == 1)
                    {
                        var cultureIds = CurrentDatabase.culturalProfiles.Select(c => c.ID);
                        list = list.Where(a => !cultureIds.Contains(a.CultureID));
                    }
                    else
                    {
                        var cultureIds = CurrentDatabase.culturalProfiles.FindAll(c =>
                                c.CultureName.Contains(cultureFilter.value, StringComparison.OrdinalIgnoreCase))
                            .Select(c => c.ID);

                        list = list.Where(a => cultureIds.Contains(a.CultureID));
                    }
                }

                //Search by Name
                if (!string.IsNullOrEmpty(searchStr))
                {
                    list = list.Where(n =>
                        n.ActorName.Contains(searchStr, StringComparison.OrdinalIgnoreCase));
                }

                if (stateFilter.index != 0)
                {
                    list = list.Where(a => a.State == (Actor.IState)stateFilter.index - 1);
                }

                if (ageFilter.index != 0)
                {
                    list = ageFilter.index == 1 ? list.OrderByDescending(a => a.Age) : list.OrderBy(a => a.Age);
                }

                //Player is First
                list = list.OrderByDescending(a => a.IsPlayer);
                LoadCharacters(list);
            }

            cultureFilter.RegisterValueChangedCallback(_ =>
            {
                lastCultureFilterIndex = cultureFilter.index;
                Filter();
            });
            stateFilter.RegisterValueChangedCallback(_ =>
            {
                lastStateFilterIndex = stateFilter.index;
                Filter();
            });
            ageFilter.RegisterValueChangedCallback(_ =>
            {
                lastAgeFilterIndex = ageFilter.index;
                Filter();
            });

            #endregion

            Filter();

            if (characterList.childCount == 0)
            {
                characterList.Hide();
            }

            void LoadCharacters(IEnumerable list)
            {
                characterList.Clear();

                foreach (var data in list)
                {
                    var actorData = (ActorData)data;

                    var character = new VisualElement()
                    {
                        style =
                        {
                            backgroundImage = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/none.png"),
                        }
                    };
                    character.AddClasses("character");

                    character.tooltip +=
                        $"Name: {actorData.ActorName}\nAge: {actorData.Age}\nGender: {actorData.Gender}";
                    var culture = CurrentDatabase.culturalProfiles.Find(c => c.ID == actorData.CultureID);
                    character.tooltip += $"\nCulture: {culture?.CultureName}";

                    if (actorData.IsPlayer)
                    {
                        character.SetBorderColor(NullUtils.HTMLColor("#FF9E00"));
                    }

                    var memberNode = CurrentDatabase.nodeDataList.OfType<FamilyMemberData>()
                        .FirstOrDefault(c => c.ActorID == actorData.ID);
                    if (memberNode != null)
                    {
                        if (!actorData.IsPlayer) character.SetBorderColor(NullUtils.HTMLColor("#A44343"));
                        character.userData = true;
                    }

                    character.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        if (evt.button != 0) return;

                        IGroup group = (IGroup)ieGraphView.graphElements.FirstOrDefault(g => g is FamilyGroup);

                        if (character.userData != null)
                        {
                            if (memberNode == null) return;

                            if (group?.ID != memberNode.GroupId || group == null)
                            {
                                SetFamilyKey(memberNode.GroupId);
                                GraphSaveUtility.LoadCurrent(ieGraphView);

                                var getNode = ieGraphView.graphElements.OfType<FamilyMemberNode>()
                                    .FirstOrDefault(c => c.ID == memberNode.ID);

                                if (getNode != null)
                                {
                                    ieGraphView.ClearSelection();
                                    ieGraphView.AddToSelection(getNode);
                                    EditorRoutine.StartRoutine(0.03f,
                                        () => ieGraphView.Fit(element => element == getNode));
                                    getNode.Focus();
                                }
                            }
                            else
                            {
                                var getNode = ieGraphView.graphElements.OfType<FamilyMemberNode>()
                                    .FirstOrDefault(c => c.ID == memberNode.ID);

                                if (getNode != null)
                                {
                                    ieGraphView.ClearSelection();
                                    ieGraphView.AddToSelection(getNode);
                                    ieGraphView.Fit(element => element == getNode);
                                    getNode.Focus();
                                }
                            }

                            return;
                        }

                        if (group != null)
                        {
                            var node = ieGraphView.CreateNode<FamilyMemberNode>(
                                ieGraphView.selection.LastOrDefault() is not INode lastSelection
                                    ? group.containedElements.LastOrDefault() is not INode lastElement
                                        ? group.GetPosition().position
                                        : new Vector2(lastElement.GetPosition().position.x + lastElement.layout.width,
                                            lastElement.GetPosition().y)
                                    : new Vector2(lastSelection.GetPosition().position.x + lastSelection.layout.width,
                                        lastSelection.GetPosition().y), false);
                            node.ActorID = actorData.ID;

                            node.Group = group;
                            group.AddElement(node);
                            ieGraphView.AddElement(node);

                            node.Draw();
                            node.OnCreated();

                            GraphSaveUtility.SaveCurrent();

                            character.SetBorderColor(NullUtils.HTMLColor("#A44343"));
                            character.userData = true;

                            memberNode = CurrentDatabase.nodeDataList.OfType<FamilyMemberData>()
                                .FirstOrDefault(c => c.ActorID == actorData.ID);

                            ieGraphView.ClearSelection();
                            ieGraphView.AddToSelection(node);
                            node.Focus();

                            EditorRoutine.StartRoutine(0.01f, () => { ieGraphView.Fit(element => element == node); });
                        }
                    });

                    if (actorData.Portrait != null)
                    {
                        character.style.backgroundImage = new StyleBackground(actorData.Portrait);
                    }

                    characterList.Add(character);
                }
            }

            searchBox.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                searchStr = searchBox.value;
                Filter();
            });

            searchBox.RegisterCallback<FocusInEvent>(_ =>
            {
                if (string.IsNullOrEmpty(searchStr))
                {
                    searchBox.SetValueWithoutNotify(string.Empty);
                }
            });

            searchBox.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (string.IsNullOrEmpty(searchStr))
                {
                    searchBox.SetValueWithoutNotify("Search by name..");
                }
            });

            Toggle();

            panel.Add(actorMenu);
            panel.Add(searchBox);
            panel.Add(filterBox);
            panel.Add(characterList);

            familyPanel.Add(arrowIcon);
            familyPanel.Add(panel);

            childRoot.Add(familyPanel);
            
            lastFamilyScrollRect = panel.scrollOffset;
        }

        #region STORY

        private void AddStoryMenu()
        {
            storyField = new VisualElement()
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                },
                pickingMode = PickingMode.Ignore
            };
            storyField.AddClasses("field");

            storyList = new DropdownField();
            var dropdownChild = storyList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#ABABAB"));

            storyList.RegisterCallback<ChangeEvent<string>>(SelectStory);
            
            debugBtn = new Button()
            {
                text = "Debug"
            };

            stopBtn = new Button()
            {
                text = "Stop"
            };

            storyField.Add(storyList);
            // storyField.Add(debugBtn);
            // storyField.Add(stopBtn);
            childRoot.Add(storyField);
            rootVisualElement.Add(childRoot);
        }

        private void SelectStory(ChangeEvent<string> storyListEvt)
        {
            if (storyList.index <= 0)
            {
                storyKey = string.Empty;
                PlayerPrefs.SetString("IDE_CurrentStory", storyKey);
                PlayerPrefs.Save();

                GraphSaveUtility.LoadCurrent(ieGraphView);

                ToggleView();
                return;
            }

            var id = stories.Keys.ElementAt(storyList.index - 1);
            storyKey = id;
            PlayerPrefs.SetString("IDE_CurrentStory", storyKey);
            PlayerPrefs.Save();

            GraphSaveUtility.LoadCurrent(ieGraphView);

            ToggleView();
        }

        public void SetStoryKey(string key)
        {
            storyKey = key;
            PlayerPrefs.SetString("IDE_CurrentStory", storyKey);
            PlayerPrefs.Save();

            LoadStories();
        }

        public void SetLastStoryKey()
        {
            var lastIntrigue = CurrentDatabase.groupDataList.LastOrDefault(g => g is SchemeGroupData);
            storyKey = lastIntrigue?.ID;
            PlayerPrefs.SetString("IDE_CurrentStory", storyKey);
            PlayerPrefs.Save();

            LoadStories();
        }

        private void LoadStories()
        {
            if (CurrentDatabase == null) return;
            stories = new Dictionary<string, string>(CurrentDatabase.groupDataList.OfType<SchemeGroupData>()
                .OrderBy(n => n.Title).ToDictionary(story => story.ID, _actor => _actor.Title));

            storyList.choices = new List<string>(stories.Values);
            storyList.choices.Insert(0, "Intrigue: NULL");
            storyList.SetValueWithoutNotify(!string.IsNullOrEmpty(storyKey) && stories.ContainsKey(storyKey) ? stories[storyKey] : storyList.choices[0]);
        }

        #endregion

        #region CLAN

        private void AddClanMenu()
        {
            clanField = new VisualElement()
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                },
                pickingMode = PickingMode.Ignore
            };
            clanField.AddClasses("field");

            clanList = new DropdownField();
            var dropdownChild = clanList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#ABABAB"));

            clanList.RegisterCallback<ChangeEvent<string>>(SelectClan);

            clanField.Add(clanList);
            childRoot.Add(clanField);
            rootVisualElement.Add(childRoot);
        }

        private void SelectClan(ChangeEvent<string> storyListEvt)
        {
            if (clanList.index <= 0)
            {
                clanKey = string.Empty;
                PlayerPrefs.SetString("IDE_CurrentClan", clanKey);
                PlayerPrefs.Save();

                GraphSaveUtility.LoadCurrent(ieGraphView);

                ToggleView();
                return;
            }

            var id = clans.Keys.ElementAt(clanList.index - 1);
            clanKey = id;
            PlayerPrefs.SetString("IDE_CurrentClan", clanKey);
            PlayerPrefs.Save();

            GraphSaveUtility.LoadCurrent(ieGraphView);

            ToggleView();
        }

        public void SetClanKey(string key)
        {
            clanKey = key;
            PlayerPrefs.SetString("IDE_CurrentClan", clanKey);
            PlayerPrefs.Save();

            LoadClans();
        }

        public void SetLastClanKey()
        {
            clanKey = clans.Keys.LastOrDefault() ?? string.Empty;
            PlayerPrefs.SetString("IDE_CurrentClan", clanKey);
            PlayerPrefs.Save();

            LoadClans();
        }

        private void LoadClans()
        {
            if (CurrentDatabase == null) return;
            clans = new Dictionary<string, string>(CurrentDatabase.groupDataList.OfType<ClanGroupData>().OrderBy(n => n.Title)
                .ToDictionary(story => story.ID, _actor => _actor.Title));

            clanList.choices = new List<string>(clans.Values);
            clanList.choices.Insert(0, "Clan: NULL");
            clanList.SetValueWithoutNotify(clans.ContainsKey(clanKey) ? clans[clanKey] : clanList.choices[0]);
        }

        #endregion

        #region FAMILY

        private void AddFamilyMenu()
        {
            familyField = new VisualElement()
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                },
                pickingMode = PickingMode.Ignore
            };
            familyField.AddClasses("field");

            familyList = new DropdownField();
            var dropdownChild = familyList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#ABABAB"));

            familyList.RegisterCallback<ChangeEvent<string>>(SelectFamily);

            familyField.Add(familyList);
            childRoot.Add(familyField);
            rootVisualElement.Add(childRoot);
        }

        private void SelectFamily(ChangeEvent<string> storyListEvt)
        {
            if (familyList.index <= 0)
            {
                familyKey = string.Empty;
                PlayerPrefs.SetString("IDE_CurrentFamily", familyKey);
                PlayerPrefs.Save();

                GraphSaveUtility.LoadCurrent(ieGraphView);

                ToggleView();
                return;
            }

            var id = families.Keys.ElementAt(familyList.index - 1);
            familyKey = id;
            PlayerPrefs.SetString("IDE_CurrentFamily", familyKey);
            PlayerPrefs.Save();

            GraphSaveUtility.LoadCurrent(ieGraphView);

            ToggleView();
        }

        public void SetFamilyKey(string key)
        {
            familyKey = key;
            PlayerPrefs.SetString("IDE_CurrentFamily", familyKey);
            PlayerPrefs.Save();

            LoadFamilies();
        }

        public void SetLastFamilyKey()
        {
            familyKey = families.Keys.LastOrDefault() ?? string.Empty;
            PlayerPrefs.SetString("IDE_CurrentFamily", familyKey);
            PlayerPrefs.Save();

            LoadFamilies();
        }

        private void LoadFamilies()
        {
            if (CurrentDatabase == null) return;
            families = new Dictionary<string, string>(CurrentDatabase.groupDataList.OfType<FamilyGroupData>()
                .OrderBy(n => n.Title).ToDictionary(story => story.ID, _actor => _actor.Title));

            familyList.choices = new List<string>(families.Values);
            familyList.choices.Insert(0, "Family: NULL");
            familyList.SetValueWithoutNotify(families.ContainsKey(familyKey)
                ? families[familyKey]
                : familyList.choices[0]);
        }

        #endregion

        #region RULE

        private void AddRuleMenu()
        {
            ruleField = new VisualElement()
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                },
                pickingMode = PickingMode.Ignore
            };
            ruleField.AddClasses("field");

            ruleList = new DropdownField();
            var dropdownChild = ruleList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#ABABAB"));

            ruleList.RegisterCallback<ChangeEvent<string>>(SelectRule);

            ruleField.Add(ruleList);
            childRoot.Add(ruleField);
            rootVisualElement.Add(childRoot);
        }

        private void SelectRule(ChangeEvent<string> storyListEvt)
        {
            if (ruleList.index <= 0)
            {
                ruleKey = string.Empty;
                PlayerPrefs.SetString("IDE_CurrentRule", ruleKey);
                PlayerPrefs.Save();

                GraphSaveUtility.LoadCurrent(ieGraphView);

                ToggleView();
                return;
            }

            var id = rules.Keys.ElementAt(ruleList.index - 1);
            ruleKey = id;
            PlayerPrefs.SetString("IDE_CurrentRule", ruleKey);
            PlayerPrefs.Save();

            GraphSaveUtility.LoadCurrent(ieGraphView);

            ToggleView();
        }

        public void SetRuleKey(string key)
        {
            ruleKey = key;
            PlayerPrefs.SetString("IDE_CurrentRule", ruleKey);
            PlayerPrefs.Save();

            LoadRules();
        }

        public void SetLastRuleKey()
        {
            var lastRule = CurrentDatabase.groupDataList.LastOrDefault(g => g is RuleGroupData);
            ruleKey = lastRule?.ID;
            PlayerPrefs.SetString("IDE_CurrentRule", ruleKey);
            PlayerPrefs.Save();

            LoadRules();
        }

        public void SetLastActorKey()
        {
            var lastActor = CurrentDatabase.actorRegistry.LastOrDefault();
            actorKey = lastActor?.ID;
            PlayerPrefs.SetString("IDE_ActorID", actorKey);
            PlayerPrefs.Save();

            LoadActorList();
        }

        private void LoadRules()
        {
            if (CurrentDatabase == null) return;
            rules = new Dictionary<string, string>(CurrentDatabase.groupDataList.OfType<RuleGroupData>()
                .OrderBy(n => n.Title).ToDictionary(story => story.ID, _actor => _actor.Title));

            ruleList.choices = new List<string>(rules.Values);
            ruleList.choices.Insert(0, "Rule: NULL");
            ruleList.SetValueWithoutNotify(!string.IsNullOrEmpty(ruleKey) && rules.ContainsKey(ruleKey)
                ? rules[ruleKey]
                : ruleList.choices[0]);
        }

        #endregion

        private void AddActorMenu()
        {
            actorField = new VisualElement()
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                },
                pickingMode = PickingMode.Ignore
            };
            actorField.AddClasses("field");

            actorList = new DropdownField();
            var dropdownChild = actorList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#ABABAB"));

            actorList.RegisterCallback<ChangeEvent<string>>(SelectActor);

            actorField.Add(actorList);
            childRoot.Add(actorField);
            rootVisualElement.Add(childRoot);
        }

        private void LoadActorList()
        {
            if (CurrentDatabase == null) return;
            var i = 0;
            characters = new Dictionary<string, string>(CurrentDatabase.actorRegistry.OrderBy(n => n.Name)
                .ToDictionary(_actor => _actor.ID, _actor => $"[{i++}]: {_actor.Name}({_actor.Age})"));

            actorList.choices = new List<string>(characters.Values);
            actorList.choices.Insert(0, "Actor: NULL");
            var index = characters.Keys.ToList().IndexOf(actorKey);
            actorList.SetValueWithoutNotify(index != -1 ? actorList.choices[index + 1] : actorList.choices[0]);

            LoadActorRightMenu();
        }

        private void SelectActor(ChangeEvent<string> actorListEvt)
        {
            var isActorPage = CurrentPage == GenericNodeType.Actor;
            if (actorList.index <= 0)
            {
                if (!isActorPage) return;
                actorKey = string.Empty;
                PlayerPrefs.SetString("IDE_ActorID", actorKey);
                PlayerPrefs.Save();

                GraphSaveUtility.LoadCurrent(ieGraphView);
                ToggleView();
                return;
            }

            var id = characters.Keys.ElementAt(actorList.index - 1);
            var element = CurrentDatabase.nodeDataList.FirstOrDefault(n => n.ID == id);
            if (element == null) return;

            if (isActorPage)
            {
                actorKey = element.ID;
                PlayerPrefs.SetString("IDE_ActorID", actorKey);
                PlayerPrefs.Save();

                GraphSaveUtility.LoadCurrent(ieGraphView);

                ToggleView();
            }

            var node = ieGraphView.graphElements.OfType<INode>().FirstOrDefault(n => n.ID == element.ID);

            ieGraphView.Fit();

            ieGraphView.ClearSelection();
            ieGraphView.AddToSelection(node);
        }

        public void SetActorKey(string key)
        {
            actorKey = key;
            PlayerPrefs.SetString("IDE_ActorID", actorKey);
            PlayerPrefs.Save();

            LoadActorList();
        }

        private void LoadFamilyMembers()
        {
            if (CurrentDatabase == null) return;
            var i = 0;
            characters = new Dictionary<string, string>(ieGraphView.graphElements.OfType<FamilyMemberNode>()
                .Where(n => n.actor != null).OrderBy(n => n.actor.Name).ToDictionary(_actor => _actor.ID,
                    _actor => $"[{i++}]: {_actor.actor.Name}({_actor.actor.Age})"));

            actorList.choices = new List<string>(characters.Values);
            actorList.choices.Insert(0, "Actor: NULL");
            actorList.index = 0;

            LoadFamilyRightMenu();
        }

        private void LoadClanMembers()
        {
            if (CurrentDatabase == null) return;
            var i = 0;
            characters = new Dictionary<string, string>(ieGraphView.graphElements.OfType<ClanMemberNode>()
                .Where(n => n.actor != null).OrderBy(n => n.actor.Name).ToDictionary(_actor => _actor.ID,
                    _actor => $"[{i++}]: {_actor.actor.Name}({_actor.actor.Age})"));

            actorList.choices = new List<string>(characters.Values);
            actorList.choices.Insert(0, "Actor: NULL");
            actorList.index = 0;

            LoadClanRightMenu();
        }

        private void ToggleView(bool focus = true)
        {
            ieGraphView.ClearSelection();
            actorField.Hide();
            storyField.Hide();
            clanField.Hide();
            familyField.Hide();
            ruleField.Hide();
            creationPanel?.Hide();
            clanPanel?.Hide();
            familyPanel?.Hide();
            switch (CurrentPage)
            {
                case GenericNodeType.Scheme:
                    if (focus) GoView();

                    storyField.Show();
                    LoadStories();

                    ieGraphView.AddManipulators(GenericNodeType.Scheme);
                    ieGraphView.AddSearchWindow();

                    PlayerPrefs.SetInt("IDE_PAGE", 0);
                    PlayerPrefs.Save();
                    break;

                case GenericNodeType.Rule:
                    if (focus) GoView();

                    ruleField.Show();
                    LoadRules();

                    ieGraphView.AddManipulators(GenericNodeType.Rule);
                    ieGraphView.AddSearchWindow();

                    PlayerPrefs.SetInt("IDE_PAGE", 1);
                    PlayerPrefs.Save();
                    break;

                case GenericNodeType.Family:
                    if (focus) GoView();

                    familyField.Show();
                    LoadFamilies();
                    LoadFamilyMembers();
                    familyPanel.Show();

                    ieGraphView.AddManipulators(GenericNodeType.Family);
                    ieGraphView.AddSearchWindow();

                    PlayerPrefs.SetInt("IDE_PAGE", 2);
                    PlayerPrefs.Save();
                    break;

                case GenericNodeType.Clan:
                    if (focus) GoView();

                    clanField.Show();
                    LoadClans();
                    LoadClanMembers();
                    clanPanel.Show();

                    ieGraphView.AddManipulators(GenericNodeType.Clan);
                    ieGraphView.AddSearchWindow();

                    PlayerPrefs.SetInt("IDE_PAGE", 3);
                    PlayerPrefs.Save();
                    break;
                
                case GenericNodeType.Policy:
                    if (focus) GoView();

                    ieGraphView.AddManipulators(GenericNodeType.Policy);
                    ieGraphView.RemoveSearchWindow();

                    PlayerPrefs.SetInt("IDE_PAGE", 4);
                    PlayerPrefs.Save();
                    break;

                case GenericNodeType.Actor:
                    if (focus) GoView();

                    LoadActorList();
                    creationPanel.Show();

                    ieGraphView.AddManipulators(GenericNodeType.Actor);
                    ieGraphView.RemoveSearchWindow();

                    PlayerPrefs.SetInt("IDE_PAGE", 5);
                    PlayerPrefs.Save();
                    break;

                case GenericNodeType.Culture:
                    if (focus) GoView();

                    ieGraphView.AddManipulators(GenericNodeType.Culture);
                    ieGraphView.RemoveSearchWindow();

                    PlayerPrefs.SetInt("IDE_PAGE", 6);
                    PlayerPrefs.Save();
                    break;

                case GenericNodeType.Variable:
                    if (focus) GoView();

                    ieGraphView.AddManipulators(GenericNodeType.Variable);
                    ieGraphView.RemoveSearchWindow();

                    PlayerPrefs.SetInt("IDE_PAGE", 7);
                    PlayerPrefs.Save();
                    break;
            }
        }

        public void GoView()
        {
            EditorRoutine.StartRoutine((0.03f), () => { ieGraphView?.Fit(); });
        }

        #region Element Styles

        private void Setup()
        {
            ieGraphView = new IEGraphView(this);
            ieGraphView.StretchToParentSize();
            rootVisualElement.Add(ieGraphView);
        }

        #endregion
    }
}