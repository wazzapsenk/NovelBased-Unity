using Nullframes.Intrigues.Graph;
using Nullframes.Intrigues.Utils;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.UI {
    public class UIEditor : EditorWindow {
        private int currentPage;
        private List<VisualElement> pages;

        private enum TAB {
            Main,
            Dialogue
        }

        private TAB tabGroup;

        private Dictionary<VisualElement, List<VisualElement>> categoryElements;

        [MenuItem("Tools/Nullframes/Intrigues/Setup UI", false, 1)]
        public static void ShowExample() {
            UIEditor wnd = GetWindow<UIEditor>();
            wnd.titleContent = new GUIContent("Setup UI");

            wnd.maxSize = new Vector2(600f, 800f);
            wnd.minSize = new Vector2(600f, 800f);
        }

        public void CreateGUI() {
            VisualElement root = rootVisualElement;

            // Import UXML
            var visualTree = (VisualTreeAsset)EditorGUIUtility.Load("Nullframes/UIEditor.uxml");
            visualTree.CloneTree(root);

            // Import USS
            var styleSheet = (StyleSheet)EditorGUIUtility.Load(EditorGUIUtility.isProSkin
                ? "Nullframes/UIEditor_Dark.uss"
                : "Nullframes/UIEditor_Light.uss");
            root.styleSheets.Add(styleSheet);

            currentPage = 0;
            categoryElements = new Dictionary<VisualElement, List<VisualElement>>();

            var mainTab = root.Q<VisualElement>("mainTab");
            var dialogueTab = root.Q<VisualElement>("dialogueTab");

            mainTab.Show();
            dialogueTab.Hide();

            var dialogueCanvas = root.Q<ObjectField>("dialogueCanvas");
            dialogueCanvas.objectType = typeof(Canvas);

            var clearDialogueButton = root.Q<Button>("clearDialogue");
            var loadDialogueButton = root.Q<Button>("loadDialogue");

            var setupDialogueButton = root.Q<Button>("setupDialogueBtn");

            var dialoguePanel = root.Q<ObjectField>("dialoguePanel");
            dialoguePanel.objectType = typeof(RectTransform);

            var dialogueTitle = root.Q<ObjectField>("dialogueTitle");
            dialogueTitle.objectType = typeof(TextMeshProUGUI);

            var dialogueContent = root.Q<ObjectField>("dialogueContent");
            dialogueContent.objectType = typeof(TextMeshProUGUI);            
            
            var theme = root.Q<ObjectField>("theme");
            theme.objectType = typeof(UnityEngine.UI.Image);

            var dialogueCounter = root.Q<ObjectField>("dialogueCounter");
            dialogueCounter.objectType = typeof(TextMeshProUGUI);

            var dialogueChoiceLayout = root.Q<ObjectField>("dialogueChoiceLayout");
            dialogueChoiceLayout.objectType = typeof(RectTransform);

            var dialogueChoiceButton = root.Q<ObjectField>("dialogueChoiceButton");
            dialogueChoiceButton.objectType = typeof(UnityEngine.UI.Button);

            var dialogueChoiceText = root.Q<ObjectField>("dialogueChoiceText");
            dialogueChoiceText.objectType = typeof(TextMeshProUGUI);

            var dialogueChoiceIcon = root.Q<ObjectField>("dialogueChoiceIcon");
            dialogueChoiceIcon.objectType = typeof(UnityEngine.UI.Image);

            dialogueCanvas.RegisterValueChangedCallback(_ => {
                if (dialogueCanvas.value != null) {
                    clearDialogueButton.Show();

                    var systemUI = ((Canvas)dialogueCanvas.value).GetComponent<DialogueManager>();
                    if (systemUI != null) {
                        loadDialogueButton.Show();
                    }
                    else {
                        loadDialogueButton.Hide();
                    }
                }
                else {
                    clearDialogueButton.Hide();
                }
            });

            if (dialogueCanvas.value != null) {
                clearDialogueButton.Show();
            }
            else {
                clearDialogueButton.Hide();
                loadDialogueButton.Hide();
            }

            #region MAIN

            pages = new List<VisualElement> {
                root.Q<VisualElement>("PAGE1"),
                root.Q<VisualElement>("PAGE2"),
                root.Q<VisualElement>("PAGE3"),
                root.Q<VisualElement>("PAGE4"),
            };

            var mainBtn = root.Q<Button>("main_btn");
            var dialogueBtn = root.Q<Button>("dialogue_btn");

            mainBtn.clickable = new Clickable(_ => {
                tabGroup = TAB.Main;
                SwitchTab();
            });

            dialogueBtn.clickable = new Clickable(_ => {
                tabGroup = TAB.Dialogue;
                SwitchTab();
            });

            void SwitchTab() {
                switch (tabGroup) {
                    case TAB.Main:
                        mainTab.Show();
                        dialogueTab.Hide();

                        mainBtn.RemoveFromClassList("tab_btn");
                        mainBtn.AddToClassList("tab_btn_selected");

                        dialogueBtn.RemoveFromClassList("tab_btn_selected");
                        dialogueBtn.AddToClassList("tab_btn");

                        mainTab.Focus();
                        break;
                    case TAB.Dialogue:
                        dialogueTab.Show();
                        mainTab.Hide();

                        dialogueBtn.RemoveFromClassList("tab_btn");
                        dialogueBtn.AddToClassList("tab_btn_selected");

                        mainBtn.RemoveFromClassList("tab_btn_selected");
                        mainBtn.AddToClassList("tab_btn");

                        dialogueTab.Focus();
                        break;
                }
            }

            RefreshPage();

            var prevBtn = root.Q<Button>("prevBtn");
            var nextBtn = root.Q<Button>("nextBtn");
            var setupBtn = root.Q<Button>("setupBtn");

            prevBtn.clickable = new Clickable(_ => {
                if (currentPage == 0) {
                    return;
                }

                currentPage--;

                RefreshPage();
            });

            nextBtn.clickable = new Clickable(_ => {
                if (currentPage == 3) {
                    return;
                }

                currentPage++;

                RefreshPage();
            });

            var imgui = new IMGUIContainer();
            root.Add(imgui);

            //Main Content
            //=====================================================

            var canvasField = root.Q<ObjectField>("canvasField");
            canvasField.objectType = typeof(Canvas);

            var actorPanel = root.Q<ObjectField>("actorPanel");
            actorPanel.objectType = typeof(RectTransform);

            var clanPanel = root.Q<ObjectField>("clanPanel");
            var activeSchemesPanel = root.Q<ObjectField>("activeSchemesPanel");

            var clearBtn = root.Q<Button>("clearBtn");
            var loadBtn = root.Q<Button>("loadBtn");

            clearBtn.clickable = new Clickable(_ => { if(canvasField.value != null) ClearAll(((Canvas)canvasField.value).transform); });

            void ClearAll(Transform transform) {
                var objs = transform.GetComponentsInChildren<IIEditor>(true);

                foreach (var obj in objs) {
                    DestroyImmediate(obj);
                }

                int count = objs.Count();

                if (count < 1) return;
                NDebug.Log($"{count} element removed.", NLogType.Log, true);
            }

            canvasField.RegisterValueChangedCallback(_ => {
                if (canvasField.value != null) {
                    clearBtn.Show();

                    var systemUI = ((Canvas)canvasField.value).GetComponent<IntrigueSystemUI>();
                    if (systemUI != null) {
                        loadBtn.Show();
                    }
                    else {
                        loadBtn.Hide();
                    }
                }
                else {
                    clearBtn.Hide();
                }
            });

            if (canvasField.value != null) {
                clearBtn.Show();
            }
            else {
                clearBtn.Hide();
                loadBtn.Hide();
            }

            var refreshRate = root.Q<FloatField>("refreshRate");

            //Actor Panel
            //=====================================================

            var actorPortrait = root.Q<ObjectField>("actorPortrait");
            actorPortrait.objectType = typeof(UnityEngine.UI.Image);

            var actorName = root.Q<ObjectField>("actorName");
            actorName.objectType = typeof(TextMeshProUGUI);

            var actorAge = root.Q<ObjectField>("actorAge");
            actorAge.objectType = typeof(TextMeshProUGUI);

            var actorRelationship = root.Q<ObjectField>("relationship");
            actorRelationship.objectType = typeof(TextMeshProUGUI);

            var actorRole = root.Q<ObjectField>("actorRole");
            actorRole.objectType = typeof(TextMeshProUGUI);

            var actorRoleIcon = root.Q<ObjectField>("actorRoleIcon");
            actorRoleIcon.objectType = typeof(UnityEngine.UI.Image);

            var cultureName = root.Q<ObjectField>("cultureName");
            cultureName.objectType = typeof(TextMeshProUGUI);
            
            var cultureIcon = root.Q<ObjectField>("actorCultureIcon");
            cultureIcon.objectType = typeof(UnityEngine.UI.Image);

            var clanButton = root.Q<ObjectField>("clanButton");
            clanButton.objectType = typeof(UnityEngine.UI.Button);

            var clanBanner = root.Q<ObjectField>("clanBanner");
            clanBanner.objectType = typeof(UnityEngine.UI.Image);

            var categoryField = root.Q<VisualElement>("categoryField");

            var actorScrollView = categoryField.parent;

            actorScrollView.Remove(categoryField);

            var addCategoryButton = root.Q<Button>("addCategoryBtn");

            addCategoryButton.clickable = new Clickable(_ => { CreateActorCategory(actorScrollView); });

            var schemeContentItem = root.Q<ObjectField>("schemeContentItem");

            var schemeBtn = root.Q<ObjectField>("schemeBtn");
            schemeBtn.objectType = typeof(UnityEngine.UI.Button);

            var schemeName = root.Q<ObjectField>("schemeName");
            schemeName.objectType = typeof(TextMeshProUGUI);

            var schemeIcon = root.Q<ObjectField>("schemeIcon");
            schemeIcon.objectType = typeof(UnityEngine.UI.Image);

            var schemeDesc = root.Q<ObjectField>("schemeDesc");
            schemeDesc.objectType = typeof(TextMeshProUGUI);

            var actorCloseBtn = root.Q<ObjectField>("actorCloseBtn");
            actorCloseBtn.objectType = typeof(UnityEngine.UI.Button);            
            
            var actorPreviousBtn = root.Q<ObjectField>("actorPreviousBtn");
            actorPreviousBtn.objectType = typeof(UnityEngine.UI.Button);

            //Clan Panel
            //=====================================================

            var clanName = root.Q<ObjectField>("clanName");
            clanName.objectType = typeof(TextMeshProUGUI);

            var clanDesc = root.Q<ObjectField>("clanDesc");
            clanDesc.objectType = typeof(TextMeshProUGUI);

            var clanIcon = root.Q<ObjectField>("clanIcon");
            clanIcon.objectType = typeof(UnityEngine.UI.Image);

            var memberCount = root.Q<ObjectField>("memberCount");
            memberCount.objectType = typeof(TextMeshProUGUI);

            var clanCulture = root.Q<ObjectField>("clanCulture");
            clanCulture.objectType = typeof(TextMeshProUGUI);
            
            var clanCultureIcon = root.Q<ObjectField>("clanCultureIcon");
            clanCultureIcon.objectType = typeof(UnityEngine.UI.Image);

            var policyItem = root.Q<ObjectField>("policyItem");
            policyItem.objectType = typeof(RectTransform);

            var policyName = root.Q<ObjectField>("policyName");
            policyName.objectType = typeof(TextMeshProUGUI);

            var policyIcon = root.Q<ObjectField>("policyIcon");
            policyIcon.objectType = typeof(UnityEngine.UI.Image);
            
            var policyItem2 = root.Q<ObjectField>("familyPolicyItem");
            policyItem2.objectType = typeof(RectTransform);

            var policyName2 = root.Q<ObjectField>("familyPolicyName");
            policyName2.objectType = typeof(TextMeshProUGUI);

            var policyIcon2 = root.Q<ObjectField>("familyPolicyIcon");
            policyIcon2.objectType = typeof(UnityEngine.UI.Image);

            var clanCategoryField = root.Q<VisualElement>("clanCategoryField");

            var clanMemberScrollView = clanCategoryField.parent;

            clanMemberScrollView.Remove(clanCategoryField);

            var addClanCategoryBtn = root.Q<Button>("addClanCategoryBtn");

            addClanCategoryBtn.clickable = new Clickable(_ => { CreateMemberCategory(clanMemberScrollView); });

            var clanCloseBtn = root.Q<ObjectField>("clanCloseBtn");
            clanCloseBtn.objectType = typeof(UnityEngine.UI.Button);            
            
            var clanPreviousBtn = root.Q<ObjectField>("clanPreviousBtn");
            clanPreviousBtn.objectType = typeof(UnityEngine.UI.Button);

            //Scheme Panel
            //=====================================================

            var _schemeContentItem = root.Q<ObjectField>("_schemeItem");
            _schemeContentItem.objectType = typeof(RectTransform);

            var _schemeName = root.Q<ObjectField>("_schemeName");
            _schemeName.objectType = typeof(TextMeshProUGUI);

            var _schemeDesc = root.Q<ObjectField>("_schemeDescription");
            _schemeDesc.objectType = typeof(TextMeshProUGUI);

            var _schemeIcon = root.Q<ObjectField>("_schemeIcon");
            _schemeIcon.objectType = typeof(UnityEngine.UI.Image);

            var _schemeObjective = root.Q<ObjectField>("_schemeObjective");
            _schemeObjective.objectType = typeof(TextMeshProUGUI);

            var _conspiratorBtn = root.Q<ObjectField>("_conspiratorBtn");
            _conspiratorBtn.objectType = typeof(UnityEngine.UI.Button);
            var _conspiratorPortrait = root.Q<ObjectField>("_conspiratorPortrait");
            _conspiratorPortrait.objectType = typeof(UnityEngine.UI.Image);

            var _targetBtn = root.Q<ObjectField>("_targetBtn");
            _targetBtn.objectType = typeof(UnityEngine.UI.Button);

            var _targetPortrait = root.Q<ObjectField>("_targetPortrait");
            _targetPortrait.objectType = typeof(UnityEngine.UI.Image);

            var schemesCloseBtn = root.Q<ObjectField>("schemesCloseBtn");
            schemesCloseBtn.objectType = typeof(UnityEngine.UI.Button);            
            
            var schemesPreviousBtn = root.Q<ObjectField>("schemesPreviousBtn");
            schemesPreviousBtn.objectType = typeof(UnityEngine.UI.Button);

            #endregion

            imgui.onGUIHandler = () => {
                if (!IM.Exists || !IM.DatabaseExists) {
                    root.Disable();
                    return;
                }

                root.Enable();

                if (currentPage == 0) {
                    prevBtn.Hide();
                }
                else {
                    prevBtn.Show();
                }

                if (currentPage == 3) {
                    nextBtn.Hide();
                    setupBtn.Show();
                }
                else {
                    nextBtn.Show();
                    setupBtn.Hide();
                }

                if (canvasField.value == null || actorPanel.value == null || clanPanel.value == null ||
                    activeSchemesPanel.value == null) {
                    nextBtn.Disable();
                    setupBtn.Disable();
                }
                else {
                    nextBtn.Enable();
                    setupBtn.Enable();
                }

                if (dialogueCanvas.value == null || dialoguePanel.value == null || dialogueTitle.value == null ||
                    dialogueContent.value == null || dialogueCounter.value == null ||
                    dialogueChoiceLayout.value == null || dialogueChoiceButton.value == null ||
                    dialogueChoiceIcon.value == null || dialogueChoiceText.value == null) {
                    setupDialogueButton.Disable();
                }
                else {
                    setupDialogueButton.Enable();
                }
            };

            loadDialogueButton.clickable = new Clickable(_ => {
                var systemUI = ((Canvas)dialogueCanvas.value).GetComponent<DialogueManager>();
                if (systemUI != null) {
                    dialoguePanel.value = systemUI.dialoguePanel;
                    if (systemUI.dialoguePanel != null) {
                        if (systemUI.dialoguePanel.choiceLayout != null) {
                            dialogueChoiceLayout.value = systemUI.dialoguePanel.choiceLayout;
                            dialogueChoiceButton.value = systemUI.dialoguePanel.choiceLayout.button;
                            dialogueChoiceText.value = systemUI.dialoguePanel.choiceLayout.text;
                            dialogueChoiceIcon.value = systemUI.dialoguePanel.choiceLayout.icon;
                            dialogueTitle.value = systemUI.dialoguePanel.choiceLayout;
                        }

                        theme.value = systemUI.dialoguePanel.background;
                        dialogueTitle.value = systemUI.dialoguePanel.titleLabel;
                        dialogueContent.value = systemUI.dialoguePanel.contentLabel;
                        dialogueCounter.value = systemUI.dialoguePanel.counterLabel;
                    }
                }
            });
            
            clearDialogueButton.clickable = new Clickable(_ => { if(dialogueCanvas.value != null) ClearAll(((Canvas)dialogueCanvas.value).transform); });

            setupDialogueButton.clickable = new Clickable(_ => {
                if (!EditorUtility.DisplayDialog("Are you sure?",
                        "The UI Editor will perform the installation shortly.\nAre you sure you want to proceed?",
                        "Yes",
                        "Nope")) return;

                ClearAll(((Canvas)dialogueCanvas.value).transform);

                var canvas = (Canvas)dialogueCanvas.value;
                var panelObj = (RectTransform)dialoguePanel.value;
                var choiceObj = (RectTransform)dialogueChoiceLayout.value;

                var dialogueManager = canvas.gameObject.AddComponent<DialogueManager>();
                var dialogue = panelObj.gameObject.AddComponent<DialoguePanel>();
                var choice = choiceObj.gameObject.AddComponent<Choice>();

                dialogueManager.dialoguePanel = dialogue;

                dialogue.titleLabel = (TextMeshProUGUI)dialogueTitle.value;
                dialogue.contentLabel = (TextMeshProUGUI)dialogueContent.value;
                dialogue.counterLabel = (TextMeshProUGUI)dialogueCounter.value;
                dialogue.choiceLayout = choice;
                if(theme.value != null)
                    dialogue.background = (UnityEngine.UI.Image)theme.value;

                choice.button = (UnityEngine.UI.Button)dialogueChoiceButton.value;
                choice.text = (TextMeshProUGUI)dialogueChoiceText.value;
                choice.icon = (UnityEngine.UI.Image)dialogueChoiceIcon.value;
                
                dialogue.contentLabel.gameObject.AddComponent<LinkHandler>();
                var typeWriter = dialogue.contentLabel.gameObject.AddComponent<TypewriterEffect>();

                dialogue.typewriter = typeWriter;

                NDebug.Log("The UI installation has been successfully completed.", NLogType.Log, true);
            });

            setupBtn.clickable = new Clickable(_ => {
                //if (canvasField.value == null) {
                //    EditorUtility.DisplayDialog("Required Fields", "Please fill in the required fields to proceed.", "Ok");
                //    //NDebug.Log("Fill in the required fields to proceed.", NLogType.Error, true);
                //    return;
                //}

                if (!EditorUtility.DisplayDialog("Are you sure?",
                        "The UI Editor will perform the installation shortly.\nAre you sure you want to proceed?",
                        "Yes",
                        "Nope")) return;

                ClearAll(((Canvas)canvasField.value).transform);

                var canvas = (Canvas)canvasField.value;
                var systemUI = canvas.gameObject.AddComponent<IntrigueSystemUI>();

                // Actor Panel
                var actorPnlTransform = (RectTransform)actorPanel.value;
                var actorPanelNavigator = actorPnlTransform.gameObject.AddComponent<ActorPanelNavigator>();

                systemUI.ActorPanel = actorPanelNavigator;
                systemUI.RefreshRate = refreshRate.value;

                //Actor Elements
                actorPanelNavigator.ActorPortrait = (UnityEngine.UI.Image)actorPortrait.value;
                actorPanelNavigator.ActorName = (TextMeshProUGUI)actorName.value;
                actorPanelNavigator.ActorAge = (TextMeshProUGUI)actorAge.value;
                actorPanelNavigator.Relationship = (TextMeshProUGUI)actorRelationship.value;
                actorPanelNavigator.RoleName = (TextMeshProUGUI)actorRole.value;
                actorPanelNavigator.RoleIcon = (UnityEngine.UI.Image)actorRoleIcon.value;
                actorPanelNavigator.CultureName = (TextMeshProUGUI)cultureName.value;
                actorPanelNavigator.CultureIcon = (UnityEngine.UI.Image)cultureIcon.value;
                actorPanelNavigator.ClanButton = (UnityEngine.UI.Button)clanButton.value;
                actorPanelNavigator.ClanBanner = (UnityEngine.UI.Image)clanBanner.value;
                actorPanelNavigator.CloseButton = (UnityEngine.UI.Button)actorCloseBtn.value;
                actorPanelNavigator.PreviousButton = (UnityEngine.UI.Button)actorPreviousBtn.value;

                var schemeContentItm = (RectTransform)schemeContentItem.value;

                if (schemeContentItm != null) {
                    var schemesRef = schemeContentItm.gameObject.AddComponent<SchemeRefs>();

                    schemesRef.SchemeName = (TextMeshProUGUI)schemeName.value;
                    schemesRef.SchemeDescription = (TextMeshProUGUI)schemeDesc.value;
                    schemesRef.SchemeIcon = (UnityEngine.UI.Image)schemeIcon.value;
                    schemesRef.SchemeTriggerButton = (UnityEngine.UI.Button)schemeBtn.value;

                    actorPanelNavigator.SchemeRef = schemesRef;
                }

                actorPanelNavigator.familyCategories = new List<FamilyCategory>();

                // Family Elements
                foreach (var familyElement in categoryElements.Where(c => c.Key.name == "Family Member")
                             .Select(c => c.Value)) {
                    var familyActorContentItem = (RectTransform)((ObjectField)familyElement[1]).value;
                    if (familyActorContentItem != null) {
                        var familyCategory = familyActorContentItem.gameObject.AddComponent<FamilyCategory>();
                        familyCategory.category = (FamilyCategory.Category)((DropdownField)familyElement[0]).index;

                        familyCategory.button = (UnityEngine.UI.Button)((ObjectField)familyElement[2]).value;
                        familyCategory.portrait = (UnityEngine.UI.Image)((ObjectField)familyElement[3]).value;
                        familyCategory.showDeadActors = ((Toggle)familyElement[4]).value;

                        actorPanelNavigator.familyCategories.Add(familyCategory);
                    }
                }

                // Clan Panel
                var clanPnlTransform = (RectTransform)clanPanel.value;
                var clanPanelNavigator = clanPnlTransform.gameObject.AddComponent<ClanPanelNavigator>();

                systemUI.ClanPanel = clanPanelNavigator;

                // Clan Elements
                clanPanelNavigator.ClanName = (TextMeshProUGUI)clanName.value;
                clanPanelNavigator.ClanDescription = (TextMeshProUGUI)clanDesc.value;
                clanPanelNavigator.ClanBanner = (UnityEngine.UI.Image)clanIcon.value;
                clanPanelNavigator.MemberCount = (TextMeshProUGUI)memberCount.value;
                clanPanelNavigator.ClanCulture = (TextMeshProUGUI)clanCulture.value;
                clanPanelNavigator.ClanCultureIcon = (UnityEngine.UI.Image)clanCultureIcon.value;
                clanPanelNavigator.CloseButton = (UnityEngine.UI.Button)clanCloseBtn.value;
                clanPanelNavigator.PreviousButton = (UnityEngine.UI.Button)clanPreviousBtn.value;

                // Clan Policy Refs
                var clanPolicyRef = (RectTransform)policyItem.value;
                var clanPolicyRefs = clanPolicyRef.gameObject.AddComponent<PolicyRefs>();

                clanPolicyRefs.PolicyName = (TextMeshProUGUI)policyName.value;
                clanPolicyRefs.PolicyIcon = (UnityEngine.UI.Image)policyIcon.value;

                clanPanelNavigator.PolicyRef = clanPolicyRefs;
                
                // Family Policy Refs
                var familyPolicyRef = (RectTransform)policyItem2.value;
                var familyPolicyRefs = familyPolicyRef.gameObject.AddComponent<PolicyRefs>();

                familyPolicyRefs.PolicyName = (TextMeshProUGUI)policyName2.value;
                familyPolicyRefs.PolicyIcon = (UnityEngine.UI.Image)policyIcon2.value;

                actorPanelNavigator.PolicyRef = familyPolicyRefs;
                
                //

                clanPanelNavigator.clanCategories = new List<ClanCategory>();

                // Clan Member Elements
                foreach (var clanElement in categoryElements.Where(c => c.Key.name == "Clan Member")
                             .Select(c => c.Value)) {
                    var clanActorContentItem = (RectTransform)((ObjectField)clanElement[1]).value;
                    if (clanActorContentItem != null) {
                        var clanCategory = clanActorContentItem.gameObject.AddComponent<ClanCategory>();
                        clanCategory.role = clanElement[0].name.Equals("NONE") ? string.Empty : clanElement[0].name;

                        clanCategory.button = (UnityEngine.UI.Button)((ObjectField)clanElement[2]).value;
                        clanCategory.portrait = (UnityEngine.UI.Image)((ObjectField)clanElement[3]).value;
                        clanCategory.showDeadActors = ((Toggle)clanElement[4]).value;

                        clanPanelNavigator.clanCategories.Add(clanCategory);
                    }
                }

                // Active Schemes Panel
                var schemesPnlTransform = (RectTransform)activeSchemesPanel.value;
                var schemesPanelNavigator = schemesPnlTransform.gameObject.AddComponent<SchemesPanelNavigator>();

                systemUI.SchemesPanel = schemesPanelNavigator;

                // Scheme Elements
                var _schemeContentItm = (RectTransform)_schemeContentItem.value;
                var _schemesRef = _schemeContentItm.gameObject.AddComponent<ActiveSchemeRefs>();

                schemesPanelNavigator.ActiveSchemeRefs = _schemesRef;
                schemesPanelNavigator.CloseButton = (UnityEngine.UI.Button)schemesCloseBtn.value;
                schemesPanelNavigator.PreviousButton = (UnityEngine.UI.Button)schemesPreviousBtn.value;

                _schemesRef.SchemeName = (TextMeshProUGUI)_schemeName.value;
                _schemesRef.SchemeDescription = (TextMeshProUGUI)_schemeDesc.value;
                _schemesRef.SchemeIcon = (UnityEngine.UI.Image)_schemeIcon.value;
                _schemesRef.SchemeObjective = (TextMeshProUGUI)_schemeObjective.value;
                _schemesRef.ConspiratorButton = (UnityEngine.UI.Button)_conspiratorBtn.value;
                _schemesRef.ConspiratorPortrait = (UnityEngine.UI.Image)_conspiratorPortrait.value;
                _schemesRef.TargetButton = (UnityEngine.UI.Button)_targetBtn.value;
                _schemesRef.TargetPortrait = (UnityEngine.UI.Image)_targetPortrait.value;

                NDebug.Log("The UI installation has been successfully completed.", NLogType.Log, true);
            });

            loadBtn.clickable = new Clickable(_ => {
                var systemUI = ((Canvas)canvasField.value).GetComponent<IntrigueSystemUI>();
                if (systemUI != null) {
                    ClearCategories(actorScrollView);
                    ClearCategories(clanMemberScrollView);

                    actorPanel.value = systemUI.ActorPanel.transform;
                    clanPanel.value = systemUI.ClanPanel.transform;
                    activeSchemesPanel.value = systemUI.SchemesPanel.transform;
                    refreshRate.value = systemUI.RefreshRate;

                    if (systemUI.ActorPanel != null) {
                        actorPortrait.value = systemUI.ActorPanel.ActorPortrait;
                        actorName.value = systemUI.ActorPanel.ActorName;
                        actorAge.value = systemUI.ActorPanel.ActorAge;
                        actorRelationship.value = systemUI.ActorPanel.Relationship;
                        actorRole.value = systemUI.ActorPanel.RoleName;
                        actorRoleIcon.value = systemUI.ActorPanel.RoleIcon;
                        cultureName.value = systemUI.ActorPanel.CultureName;
                        cultureIcon.value = systemUI.ActorPanel.CultureIcon;
                        clanButton.value = systemUI.ActorPanel.ClanButton;
                        clanBanner.value = systemUI.ActorPanel.ClanBanner;
                        actorCloseBtn.value = systemUI.ActorPanel.CloseButton;
                        actorPreviousBtn.value = systemUI.ActorPanel.PreviousButton;
                        
                        if (systemUI.ActorPanel.PolicyRef != null) {
                            policyItem2.value = systemUI.ActorPanel.PolicyRef.transform;
                            policyName2.value = systemUI.ActorPanel.PolicyRef.PolicyName;
                            policyIcon2.value = systemUI.ActorPanel.PolicyRef.PolicyIcon;
                        }

                        if (systemUI.ActorPanel.SchemeRef != null) {
                            schemeContentItem.value = systemUI.ActorPanel.SchemeRef.transform;
                            schemeName.value = systemUI.ActorPanel.SchemeRef.SchemeName;
                            schemeDesc.value = systemUI.ActorPanel.SchemeRef.SchemeDescription;
                            schemeIcon.value = systemUI.ActorPanel.SchemeRef.SchemeIcon;
                            schemeBtn.value = systemUI.ActorPanel.SchemeRef.SchemeTriggerButton;
                        }

                        foreach (var category in systemUI.ActorPanel.familyCategories) {
                            var familyElement = CreateActorCategory(actorScrollView);

                            familyElement.Item1.index = (int)category.category;
                            familyElement.Item2.value = category.transform;
                            familyElement.Item3.value = category.button;
                            familyElement.Item4.value = category.portrait;
                            familyElement.Item5.value = category.showDeadActors;
                        }
                    }

                    if (systemUI.ClanPanel != null) {
                        clanName.value = systemUI.ClanPanel.ClanName;
                        clanDesc.value = systemUI.ClanPanel.ClanDescription;
                        clanIcon.value = systemUI.ClanPanel.ClanBanner;
                        memberCount.value = systemUI.ClanPanel.MemberCount;
                        clanCulture.value = systemUI.ClanPanel.ClanCulture;
                        clanCultureIcon.value = systemUI.ClanPanel.ClanCultureIcon;

                        clanCloseBtn.value = systemUI.ClanPanel.CloseButton;
                        clanPreviousBtn.value = systemUI.ClanPanel.PreviousButton;

                        if (systemUI.ClanPanel.PolicyRef != null) {
                            policyItem.value = systemUI.ClanPanel.PolicyRef.transform;
                            policyName.value = systemUI.ClanPanel.PolicyRef.PolicyName;
                            policyIcon.value = systemUI.ClanPanel.PolicyRef.PolicyIcon;
                        }

                        foreach (var category in systemUI.ClanPanel.clanCategories) {
                            var role = IM.IEDatabase.roleDefinitions.FirstOrDefault(c => c.ID == category.role);
                            var clanElement = CreateMemberCategory(clanMemberScrollView);

                            var index = clanElement.Item1.choices.IndexOf(role?.RoleName);
                            clanElement.Item1.index = index == -1 ? 0 : index;

                            clanElement.Item2.value = category.transform;
                            clanElement.Item3.value = category.button;
                            clanElement.Item4.value = category.portrait;
                            clanElement.Item5.value = category.showDeadActors;
                        }
                    }

                    if (systemUI.SchemesPanel != null) {
                        _schemeContentItem.value = systemUI.SchemesPanel.ActiveSchemeRefs.transform;
                        _schemeName.value = systemUI.SchemesPanel.ActiveSchemeRefs.SchemeName;
                        _schemeDesc.value = systemUI.SchemesPanel.ActiveSchemeRefs.SchemeDescription;
                        _schemeIcon.value = systemUI.SchemesPanel.ActiveSchemeRefs.SchemeIcon;
                        _schemeObjective.value = systemUI.SchemesPanel.ActiveSchemeRefs.SchemeObjective;

                        _conspiratorBtn.value = systemUI.SchemesPanel.ActiveSchemeRefs.ConspiratorButton;
                        _conspiratorPortrait.value = systemUI.SchemesPanel.ActiveSchemeRefs.ConspiratorPortrait;

                        _targetBtn.value = systemUI.SchemesPanel.ActiveSchemeRefs.TargetButton;
                        _targetPortrait.value = systemUI.SchemesPanel.ActiveSchemeRefs.TargetPortrait;

                        schemesCloseBtn.value = systemUI.SchemesPanel.CloseButton;
                        schemesPreviousBtn.value = systemUI.SchemesPanel.PreviousButton;
                    }
                }
            });
        }

        private void ClearCategories(VisualElement parent) {
            foreach (var category in categoryElements.Keys) {
                parent.Remove(category);
            }

            categoryElements.Clear();
        }

        private (DropdownField, ObjectField, ObjectField, ObjectField, Toggle)
            CreateActorCategory(VisualElement parent) {
            var categoryField = new VisualElement() {
                name = "Family Member"
            };

            categoryField.AddManipulator(new ContextualMenuManipulator(m => {
                m.menu.AppendAction("Remove", _ => {
                    parent.Remove(categoryField);
                    categoryElements.Remove(categoryField);
                });
            }));

            var dropdown = new DropdownField {
                label = "Category",
                choices = new List<string>() {
                    "Parent", "Child", "Spouse", "Grandparent", "Grandchild", "Sibling", "Nephew", "Niece", "Uncle",
                    "Aunt",
                    "BrotherInLaw", "SisterInLaw"
                },
                style = {
                    fontSize = 14,
                },
                index = 0
            };
            categoryField.Add(dropdown);

            var actorContentItem = new ObjectField {
                label = "Family Actor Content Item",
                objectType = typeof(RectTransform),
                style = {
                    fontSize = 14,
                }
            };
            categoryField.Add(actorContentItem);

            var actorField = new ObjectField {
                label = "Family Actor Button",
                objectType = typeof(UnityEngine.UI.Button),
                style = {
                    fontSize = 14,
                }
            };
            categoryField.Add(actorField);

            var actorPortraitField = new ObjectField {
                label = "Family Actor Portrait",
                objectType = typeof(UnityEngine.UI.Image),
                style = {
                    fontSize = 14,
                }
            };
            categoryField.Add(actorPortraitField);

            var showDeadActors = new Toggle() {
                label = "Show Dead Actors",
                value = true,
                style = {
                    fontSize = 14,
                }
            };
            categoryField.Add(showDeadActors);

            actorContentItem.RegisterValueChangedCallback(_ => {
                if (actorContentItem.value == null) return;
                var gameObj = ((RectTransform)actorContentItem.value).gameObject;

                var img = gameObj.GetComponent<UnityEngine.UI.Image>();
                var btn = gameObj.GetComponent<UnityEngine.UI.Button>();

                if (btn != null)
                    actorField.value = btn;
                if (img != null)
                    actorPortraitField.value = img;
            });

            categoryField.style.marginBottom = 10;

            parent.Add(categoryField);

            categoryElements.Add(categoryField,
                new List<VisualElement>()
                    { dropdown, actorContentItem, actorField, actorPortraitField, showDeadActors });

            return (dropdown, actorContentItem, actorField, actorPortraitField, showDeadActors);
        }

        private (DropdownField, ObjectField, ObjectField, ObjectField, Toggle) CreateMemberCategory(
            VisualElement parent) {
            var categoryField = new VisualElement() {
                name = "Clan Member"
            };

            categoryField.AddManipulator(new ContextualMenuManipulator(m => {
                m.menu.AppendAction("Remove", _ => {
                    parent.Remove(categoryField);
                    categoryElements.Remove(categoryField);
                });
            }));

            var dropdown = new DropdownField {
                label = "Category",
                style = {
                    fontSize = 14,
                },
                choices = new List<string>() { "None" },
                index = 0
            };

            foreach (var clan in IM.IEDatabase.roleDefinitions) {
                dropdown.choices.Add(clan.RoleName);
            }

            dropdown.RegisterValueChangedCallback(_ => {
                var role = IM.IEDatabase.roleDefinitions.FirstOrDefault(r => r.RoleName == dropdown.value);
                dropdown.name = role == null ? "NONE" : role.ID;
            });

            categoryField.Add(dropdown);

            var actorContentItem = new ObjectField {
                label = "Clan Member Content Item",
                objectType = typeof(RectTransform),
                style = {
                    fontSize = 14,
                }
            };
            categoryField.Add(actorContentItem);

            var actorField = new ObjectField {
                label = "Clan Member Button",
                objectType = typeof(UnityEngine.UI.Button),
                style = {
                    fontSize = 14,
                }
            };
            categoryField.Add(actorField);

            var actorPortraitField = new ObjectField {
                label = "Clan Member Portrait",
                objectType = typeof(UnityEngine.UI.Image),
                style = {
                    fontSize = 14,
                }
            };
            categoryField.Add(actorPortraitField);

            var showDeadActors = new Toggle() {
                label = "Show Dead Actors",
                style = {
                    fontSize = 14,
                }
            };
            categoryField.Add(showDeadActors);

            actorContentItem.RegisterValueChangedCallback(_ => {
                if (actorContentItem.value == null) return;
                var gameObj = ((RectTransform)actorContentItem.value).gameObject;

                var img = gameObj.GetComponent<UnityEngine.UI.Image>();
                var btn = gameObj.GetComponent<UnityEngine.UI.Button>();

                if (btn != null)
                    actorField.value = btn;
                if (img != null)
                    actorPortraitField.value = img;
            });

            categoryField.style.marginBottom = 10;

            parent.Add(categoryField);

            categoryElements.Add(categoryField,
                new List<VisualElement>()
                    { dropdown, actorContentItem, actorField, actorPortraitField, showDeadActors });

            return (dropdown, actorContentItem, actorField, actorPortraitField, showDeadActors);
        }

        private void RefreshPage() {
            foreach (var page in pages) {
                page.Hide();
            }

            pages[currentPage].Show();
        }
    }
}