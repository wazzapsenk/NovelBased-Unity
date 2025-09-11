using System;
using Nullframes.Intrigues.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nullframes.Intrigues.UI {
    public class IntrigueSystemUI : IIEditor {
        #region References

        // ===================================== [ Main Content] =====================================

        public ActorPanelNavigator ActorPanel;
        public ClanPanelNavigator ClanPanel;
        public SchemesPanelNavigator SchemesPanel;

        public static IntrigueSystemUI instance;

        #endregion

        #region Settings

        private List< GameObject > activeWindows;
        public bool MultipleWindow;

        public float RefreshRate = 2f;

        private Action RefreshEvent;

        public bool Disabled { get; private set; }

        #endregion

        #region Keys

        public KeyCode actorMenuKey = KeyCode.P;
        public KeyCode clanMenuKey = KeyCode.C;
        public KeyCode schemesMenuKey = KeyCode.J;

        #endregion

        public Stack< Action > actions = new Stack< Action >();

        private void Awake() {
            if ( instance != null ) {
                Destroy(gameObject);
                return;
            }

            instance = this;

            activeWindows = new List< GameObject >();
        }

        private void Update() {
            if ( IM.Player == null ) {
                NDebug.Log("Player not found.", NLogType.Error);
                return;
            }

            // Check if the actor menu key is pressed
            if ( Input.GetKeyDown(actorMenuKey) ) {
                // Open the actor menu for the player
                OpenActorMenu(IM.Player);
            }

            // Check if the clan menu key is pressed
            if ( Input.GetKeyDown(clanMenuKey) ) {
                // Open the clan menu for the player's clan
                OpenClanMenu(IM.Player.Clan);
            }

            // Check if the schemes menu key is pressed
            if ( Input.GetKeyDown(schemesMenuKey) ) {
                // Open the schemes menu for the player
                OpenSchemeMenu(IM.Player);
            }
        }

        public void OpenActorMenu(Actor actor, bool ignoreDisableMode = false) {
            if ( Disabled && !ignoreDisableMode ) return;

            if ( ActorPanel == null || actor == null ) return;

            actions.Push(() => OpenActorMenu(actor, ignoreDisableMode));

            foreach ( var window in activeWindows ) {
                var actorPanel = window.GetComponent< ActorPanelNavigator >();
                if ( actorPanel != null ) {
                    if ( actorPanel.actor == actor ) return;
                }
            }

            var actorNavigator = ActorPanel.gameObject.Duplicate< ActorPanelNavigator >();

            // Destroy the active window if it exists.
            if ( !MultipleWindow ) {
                foreach ( var window in activeWindows ) {
                    var navigator = window.GetComponent< INavigator >();
                    if ( navigator != null ) {
                        navigator.Close();
                    }
                }

                if ( activeWindows.Any() ) {
                    actorNavigator.onPageChanged?.Invoke();
                }

                activeWindows.Clear();
            } else {
                actorNavigator.PreviousButton.gameObject.SetActive(false);
            }
            
            actorNavigator.onPageOpened?.Invoke();

            actorNavigator.actor = actor;

            var behaviours = actorNavigator.transform.GetComponentsInChildren< IUIActorBehaviour >();

            foreach ( var uiBehaviour in behaviours ) {
                uiBehaviour.OnMenuOpened(actor);
                uiBehaviour.Init(actor);
                if ( actor.Clan != null )
                    uiBehaviour.Init(actor.Clan);
            }

            activeWindows.Add(actorNavigator.gameObject);

            actorNavigator.transform.SetAsLastSibling();

            if ( actorNavigator.CloseButton != null ) {
                actorNavigator.CloseButton.onClick.AddListener(() => {
                    if ( Disabled ) return;
                    activeWindows.Remove(actorNavigator.gameObject);
                    actorNavigator.Close();
                    actions.Clear();
                });
            }

            if ( actorNavigator.PreviousButton != null ) {
                actorNavigator.PreviousButton.onClick.AddListener(() => {
                    if ( Disabled || MultipleWindow ) return;

                    activeWindows.Remove(actorNavigator.gameObject);
                    actorNavigator.Close();

                    NullUtils.TryPop(actions, out _);

                    if ( NullUtils.TryPop(actions, out var previousAction) )
                        previousAction?.Invoke();
                });
            }

            if ( actorNavigator.ClanButton != null ) {
                actorNavigator.ClanButton.onClick.AddListener(() => {
                    if ( actor.Clan != null ) {
                        OpenClanMenu(actor.Clan);
                    }
                });
            }

            List< GameObject > createdObjects = new List< GameObject >();

            UpdateInfo();

            void UpdateInfo() {
                if ( actor.State == Actor.IState.Passive && !actorNavigator.isDeadInvoked ) {
                    actorNavigator.isDead?.Invoke();
                    actorNavigator.isDeadInvoked = true;
                }

                if ( actorNavigator.ActorName != null )
                    actorNavigator.ActorName.text = actor.FullName;
                if ( actorNavigator.ActorAge != null )
                    actorNavigator.ActorAge.text = actor.Age.ToString();

                // Get the relation name of the actor.
                string relationName = GetRelationName(actor);

                // If the relation name is not null, set it as the text of the relationNameWithPlayer UI element in the ActorReferences, with a bullet point symbol at the beginning.
                if ( relationName != null ) {
                    if ( actorNavigator.Relationship != null )
                        actorNavigator.Relationship.text = relationName;
                }

                if ( actor.Portrait != null ) {
                    actorNavigator.ActorPortrait.sprite = actor.Portrait;
                }

                if ( actor.Role != null ) {
                    if ( actor.Role.Icon != null ) {
                        if ( actorNavigator.RoleIcon != null )
                            actorNavigator.RoleIcon.sprite = actor.Role.Icon;
                    }

                    if ( actorNavigator.RoleName != null )
                        actorNavigator.RoleName.text = actor.Title;
                }

                if ( actor.Culture != null ) {
                    if ( actorNavigator.CultureName != null )
                        actorNavigator.CultureName.text = actor.Culture.CultureName;
                    if ( actorNavigator.CultureIcon != null && actor.Culture.Icon != null )
                        actorNavigator.CultureIcon.sprite = actor.Culture.Icon;
                }

                if ( actor.Clan?.Icon != null ) {
                    if ( actorNavigator.ClanBanner != null )
                        actorNavigator.ClanBanner.sprite = actor.Clan.Icon;
                }

                int familyMemberCount = 0;
                if ( actor.Family != null ) {
                    if ( actorNavigator.PolicyRef != null ) {
                        if ( !actor.Family.Policies.Any() ) {
                            actorNavigator.isEmpty_Policies?.Invoke();
                        } else {
                            actorNavigator.isNotEmpty_Policies?.Invoke();
                        }

                        foreach ( var policy in actor.Family.Policies ) {
                            var duplicatedPolicy = actorNavigator.PolicyRef.gameObject.Duplicate< PolicyRefs >();
                            duplicatedPolicy.gameObject.SetActive(true);

                            createdObjects.Add(duplicatedPolicy.gameObject);

                            if ( duplicatedPolicy.PolicyName != null )
                                duplicatedPolicy.PolicyName.text = policy.PolicyName;
                            if ( duplicatedPolicy.PolicyIcon != null )
                                duplicatedPolicy.PolicyIcon.sprite = policy.Icon;

                            foreach ( var uiBehaviour in behaviours ) {
                                uiBehaviour.OnPolicyLoaded(policy, duplicatedPolicy.transform);
                            }
                        }

                        actorNavigator.PolicyRef.gameObject.SetActive(false);
                    }

                    foreach ( var category in actorNavigator.familyCategories ) {
                        if ( category == null ) continue;
                        int loadedCount = 0;
                        if ( category.category == FamilyCategory.Category.Parent ) {
                            var parents = actor.Parents(category.showDeadActors)
                                .OrderByDescending(s => s.Role?.Priority).ThenBy(s => s.Age).ToList();
                            if ( parents.Any() ) {
                                foreach ( var parent in parents ) {
                                    var familyCategory = category.gameObject.Duplicate< FamilyCategory >();
                                    if ( parent.State == Actor.IState.Passive )
                                        familyCategory.isDead?.Invoke();
                                    familyCategory.button.onClick.AddListener(() => { OpenActorMenu(parent); });
                                    if ( parent.Portrait != null && familyCategory.portrait != null )
                                        familyCategory.portrait.sprite = parent.Portrait;

                                    familyCategory.gameObject.SetActive(true);

                                    foreach ( var uiBehaviour in behaviours ) {
                                        uiBehaviour.OnFamilyMemberLoaded(parent, familyCategory.transform);
                                    }

                                    createdObjects.Add(familyCategory.gameObject);

                                    loadedCount++;
                                }
                            }
                        } else if ( category.category == FamilyCategory.Category.Child ) {
                            var children = actor.Children(category.showDeadActors)
                                .OrderByDescending(s => s.Role?.Priority).ThenBy(s => s.Age).ToList();
                            if ( children.Any() ) {
                                foreach ( var child in children ) {
                                    var familyCategory = category.gameObject.Duplicate< FamilyCategory >();
                                    if ( child.State == Actor.IState.Passive )
                                        familyCategory.isDead?.Invoke();
                                    familyCategory.button.onClick.AddListener(() => { OpenActorMenu(child); });
                                    if ( child.Portrait != null && familyCategory.portrait != null )
                                        familyCategory.portrait.sprite = child.Portrait;

                                    familyCategory.gameObject.SetActive(true);

                                    foreach ( var uiBehaviour in behaviours ) {
                                        uiBehaviour.OnFamilyMemberLoaded(child, familyCategory.transform);
                                    }

                                    createdObjects.Add(familyCategory.gameObject);

                                    loadedCount++;
                                }
                            }
                        } else if ( category.category == FamilyCategory.Category.Spouse ) {
                            var spouses = actor.Spouses(category.showDeadActors)
                                .OrderByDescending(s => s.Role?.Priority).ThenBy(s => s.Age).ToList();
                            if ( spouses.Any() ) {
                                foreach ( var spouse in spouses ) {
                                    var familyCategory = category.gameObject.Duplicate< FamilyCategory >();
                                    if ( spouse.State == Actor.IState.Passive )
                                        familyCategory.isDead?.Invoke();
                                    familyCategory.button.onClick.AddListener(() => { OpenActorMenu(spouse); });
                                    if ( spouse.Portrait != null && familyCategory.portrait != null )
                                        familyCategory.portrait.sprite = spouse.Portrait;

                                    familyCategory.gameObject.SetActive(true);

                                    foreach ( var uiBehaviour in behaviours ) {
                                        uiBehaviour.OnFamilyMemberLoaded(spouse, familyCategory.transform);
                                    }

                                    createdObjects.Add(familyCategory.gameObject);

                                    loadedCount++;
                                }
                            }
                        } else if ( category.category == FamilyCategory.Category.Grandparent ) {
                            var grandparents = actor.Grandparents(category.showDeadActors)
                                .OrderByDescending(s => s.Role?.Priority).ThenBy(s => s.Age).ToList();
                            if ( grandparents.Any() ) {
                                foreach ( var grandparent in grandparents ) {
                                    var familyCategory = category.gameObject.Duplicate< FamilyCategory >();
                                    if ( grandparent.State == Actor.IState.Passive )
                                        familyCategory.isDead?.Invoke();
                                    familyCategory.button.onClick.AddListener(() => { OpenActorMenu(grandparent); });
                                    if ( grandparent.Portrait != null && familyCategory.portrait != null )
                                        familyCategory.portrait.sprite = grandparent.Portrait;

                                    familyCategory.gameObject.SetActive(true);

                                    foreach ( var uiBehaviour in behaviours ) {
                                        uiBehaviour.OnFamilyMemberLoaded(grandparent, familyCategory.transform);
                                    }

                                    createdObjects.Add(familyCategory.gameObject);

                                    loadedCount++;
                                }
                            }
                        } else if ( category.category == FamilyCategory.Category.Grandchild ) {
                            var grandchildren = actor.Grandchildren(category.showDeadActors)
                                .OrderByDescending(s => s.Role?.Priority).ThenBy(s => s.Age).ToList();
                            if ( grandchildren.Any() ) {
                                foreach ( var grandchild in grandchildren ) {
                                    var familyCategory = category.gameObject.Duplicate< FamilyCategory >();
                                    if ( grandchild.State == Actor.IState.Passive )
                                        familyCategory.isDead?.Invoke();
                                    familyCategory.button.onClick.AddListener(() => { OpenActorMenu(grandchild); });
                                    if ( grandchild.Portrait != null && familyCategory.portrait != null )
                                        familyCategory.portrait.sprite = grandchild.Portrait;

                                    familyCategory.gameObject.SetActive(true);

                                    foreach ( var uiBehaviour in behaviours ) {
                                        uiBehaviour.OnFamilyMemberLoaded(grandchild, familyCategory.transform);
                                    }

                                    createdObjects.Add(familyCategory.gameObject);

                                    loadedCount++;
                                }
                            }
                        } else if ( category.category == FamilyCategory.Category.Sibling ) {
                            var siblings = actor.Siblings(category.showDeadActors)
                                .OrderByDescending(s => s.Role?.Priority).ThenBy(s => s.Age).ToList();
                            if ( siblings.Any() ) {
                                foreach ( var _sibling in siblings ) {
                                    var familyCategory = category.gameObject.Duplicate< FamilyCategory >();
                                    if ( _sibling.State == Actor.IState.Passive )
                                        familyCategory.isDead?.Invoke();
                                    familyCategory.button.onClick.AddListener(() => { OpenActorMenu(_sibling); });
                                    if ( _sibling.Portrait != null && familyCategory.portrait != null )
                                        familyCategory.portrait.sprite = _sibling.Portrait;

                                    familyCategory.gameObject.SetActive(true);

                                    foreach ( var uiBehaviour in behaviours ) {
                                        uiBehaviour.OnFamilyMemberLoaded(_sibling, familyCategory.transform);
                                    }

                                    createdObjects.Add(familyCategory.gameObject);

                                    loadedCount++;
                                }
                            }
                        } else if ( category.category == FamilyCategory.Category.Nephew ) {
                            var nephews = actor.Nephews(category.showDeadActors)
                                .OrderByDescending(s => s.Role?.Priority).ThenBy(s => s.Age).ToList();
                            if ( nephews.Any() ) {
                                foreach ( var nephew in nephews ) {
                                    var familyCategory = category.gameObject.Duplicate< FamilyCategory >();
                                    if ( nephew.State == Actor.IState.Passive )
                                        familyCategory.isDead?.Invoke();
                                    familyCategory.button.onClick.AddListener(() => { OpenActorMenu(nephew); });
                                    if ( nephew.Portrait != null && familyCategory.portrait != null )
                                        familyCategory.portrait.sprite = nephew.Portrait;

                                    familyCategory.gameObject.SetActive(true);

                                    foreach ( var uiBehaviour in behaviours ) {
                                        uiBehaviour.OnFamilyMemberLoaded(nephew, familyCategory.transform);
                                    }

                                    createdObjects.Add(familyCategory.gameObject);

                                    loadedCount++;
                                }
                            }
                        } else if ( category.category == FamilyCategory.Category.Niece ) {
                            var nieces = actor.Nieces(category.showDeadActors).OrderByDescending(s => s.Role?.Priority)
                                .ThenBy(s => s.Age).ToList();
                            if ( nieces.Any() ) {
                                foreach ( var niece in nieces ) {
                                    var familyCategory = category.gameObject.Duplicate< FamilyCategory >();
                                    if ( niece.State == Actor.IState.Passive )
                                        familyCategory.isDead?.Invoke();
                                    familyCategory.button.onClick.AddListener(() => { OpenActorMenu(niece); });
                                    if ( niece.Portrait != null && familyCategory.portrait != null )
                                        familyCategory.portrait.sprite = niece.Portrait;

                                    familyCategory.gameObject.SetActive(true);

                                    foreach ( var uiBehaviour in behaviours ) {
                                        uiBehaviour.OnFamilyMemberLoaded(niece, familyCategory.transform);
                                    }

                                    createdObjects.Add(familyCategory.gameObject);

                                    loadedCount++;
                                }
                            }
                        } else if ( category.category == FamilyCategory.Category.Uncle ) {
                            var uncles = actor.Uncles(category.showDeadActors).OrderByDescending(s => s.Role?.Priority)
                                .ThenBy(s => s.Age).ToList();
                            if ( uncles.Any() ) {
                                foreach ( var uncle in uncles ) {
                                    var familyCategory = category.gameObject.Duplicate< FamilyCategory >();
                                    if ( uncle.State == Actor.IState.Passive )
                                        familyCategory.isDead?.Invoke();
                                    familyCategory.button.onClick.AddListener(() => { OpenActorMenu(uncle); });
                                    if ( uncle.Portrait != null && familyCategory.portrait != null )
                                        familyCategory.portrait.sprite = uncle.Portrait;

                                    familyCategory.gameObject.SetActive(true);

                                    foreach ( var uiBehaviour in behaviours ) {
                                        uiBehaviour.OnFamilyMemberLoaded(uncle, familyCategory.transform);
                                    }

                                    createdObjects.Add(familyCategory.gameObject);

                                    loadedCount++;
                                }
                            }
                        } else if ( category.category == FamilyCategory.Category.Aunt ) {
                            var aunts = actor.Aunts(category.showDeadActors).OrderByDescending(s => s.Role?.Priority)
                                .ThenBy(s => s.Age).ToList();
                            if ( aunts.Any() ) {
                                foreach ( var aunt in aunts ) {
                                    var familyCategory = category.gameObject.Duplicate< FamilyCategory >();
                                    if ( aunt.State == Actor.IState.Passive )
                                        familyCategory.isDead?.Invoke();
                                    familyCategory.button.onClick.AddListener(() => { OpenActorMenu(aunt); });
                                    if ( aunt.Portrait != null && familyCategory.portrait != null )
                                        familyCategory.portrait.sprite = aunt.Portrait;

                                    familyCategory.gameObject.SetActive(true);

                                    foreach ( var uiBehaviour in behaviours ) {
                                        uiBehaviour.OnFamilyMemberLoaded(aunt, familyCategory.transform);
                                    }

                                    createdObjects.Add(familyCategory.gameObject);

                                    loadedCount++;
                                }
                            }
                        } else if ( category.category == FamilyCategory.Category.BrotherInLaw ) {
                            var brothersinlaw = actor.BrothersInLaw(category.showDeadActors)
                                .OrderByDescending(s => s.Role?.Priority).ThenBy(s => s.Age).ToList();
                            if ( brothersinlaw.Any() ) {
                                foreach ( var brotherinlaw in brothersinlaw ) {
                                    var familyCategory = category.gameObject.Duplicate< FamilyCategory >();
                                    if ( brotherinlaw.State == Actor.IState.Passive )
                                        familyCategory.isDead?.Invoke();
                                    familyCategory.button.onClick.AddListener(() => { OpenActorMenu(brotherinlaw); });
                                    if ( brotherinlaw.Portrait != null && familyCategory.portrait != null )
                                        familyCategory.portrait.sprite = brotherinlaw.Portrait;

                                    familyCategory.gameObject.SetActive(true);

                                    foreach ( var uiBehaviour in behaviours ) {
                                        uiBehaviour.OnFamilyMemberLoaded(brotherinlaw, familyCategory.transform);
                                    }

                                    createdObjects.Add(familyCategory.gameObject);

                                    loadedCount++;
                                }
                            }
                        } else if ( category.category == FamilyCategory.Category.SisterInLaw ) {
                            var sistersinlaw = actor.SistersInLaw(category.showDeadActors)
                                .OrderByDescending(s => s.Role?.Priority).ThenBy(s => s.Age).ToList();
                            if ( sistersinlaw.Any() ) {
                                foreach ( var sisterinlaw in sistersinlaw ) {
                                    var familyCategory = category.gameObject.Duplicate< FamilyCategory >();
                                    if ( sisterinlaw.State == Actor.IState.Passive )
                                        familyCategory.isDead?.Invoke();
                                    familyCategory.button.onClick.AddListener(() => { OpenActorMenu(sisterinlaw); });
                                    if ( sisterinlaw.Portrait != null && familyCategory.portrait != null )
                                        familyCategory.portrait.sprite = sisterinlaw.Portrait;

                                    familyCategory.gameObject.SetActive(true);

                                    foreach ( var uiBehaviour in behaviours ) {
                                        uiBehaviour.OnFamilyMemberLoaded(sisterinlaw, familyCategory.transform);
                                    }

                                    createdObjects.Add(familyCategory.gameObject);

                                    loadedCount++;
                                }
                            }
                        }

                        familyMemberCount += loadedCount;

                        if ( loadedCount < 1 ) {
                            //category.gameObject.SetActive(true);
                            category.isEmpty?.Invoke();
                        } else {
                            category.isNotEmpty?.Invoke();
                        }

                        category.gameObject.SetActive(false);
                    }
                }

                if ( familyMemberCount < 1 )
                    actorNavigator.isEmpty_FamilyMembers?.Invoke();
                else
                    actorNavigator.isNotEmpty_FamilyMembers?.Invoke();

                int availableSchemeCount = 0;

                // Check if the actor is not the player character.
                if ( actorNavigator.SchemeRef != null ) {
                    foreach ( var scheme in IM.Schemes ) {
                        if ( scheme.HideOnUI ) continue;
                        switch ( scheme.TargetNotRequired ) {
                            case true when !IM.IsPlayer(actor):
                            case false when IM.IsPlayer(actor):
                                continue;
                        }

                        Actor actorRef = actor;

                        if ( scheme.TargetNotRequired ) {
                            actorRef = IM.IsPlayer(actor) ? null : actor;
                        }

                        // If the current scheme is already active for the player, skip the rest of this iteration.
                        if ( IM.Player.SchemeIsActive(scheme.ID, actorRef) ) continue;

                        var ruleResult = scheme.IsCompatible(IM.Player, actorRef);

                        if ( ( !ruleResult && scheme.HideIfNotCompatible ) ||
                             ( !ruleResult && ( !ruleResult.ErrorList.Any() && !ruleResult.WarningList.Any() ) ) )
                            continue;

                        // Create a duplicate of the SchemeItem component associated with the schemeItem.
                        var schemeItem = actorNavigator.SchemeRef.gameObject.Duplicate< SchemeRefs >();

                        createdObjects.Add(schemeItem.gameObject);

                        // Set the name and description of the scheme in the duplicated SchemeItem.
                        if ( schemeItem.SchemeName != null )
                            schemeItem.SchemeName.text = scheme.SchemeName;
                        if ( schemeItem.SchemeDescription != null )
                            schemeItem.SchemeDescription.text = scheme.Description.SchemeFormat(IM.Player, actor);

                        // Check if the scheme's icon is not null
                        if ( scheme.Icon != null ) {
                            // If the icon is not null, set the sprite of the schemeItem's schemeIcon to the scheme's icon
                            if ( schemeItem.SchemeIcon != null )
                                schemeItem.SchemeIcon.sprite = scheme.Icon;
                        }

                        if ( schemeItem.SchemeTriggerButton != null ) {
                            var button = schemeItem.SchemeTriggerButton;

                            button.onClick.AddListener(() => {
                                // if (Disabled) return;

                                if ( !scheme.IsCompatible(IM.Player, actorRef) ) return;
                                if ( behaviours.Length < 1 ) {
                                    IM.Player.StartScheme(scheme, actorRef);
                                    Refresh();
                                    return;
                                }

                                foreach ( var behaviour in behaviours ) {
                                    behaviour.Init(IM.Player, actorRef, scheme);
                                    behaviour.OnSchemeButtonTriggered(button.transform);
                                }

                                Refresh();
                            });

                            // If it is not scheme compatible, send it to the bottom of the list.
                            if ( !ruleResult ) {
                                button.interactable = false;
                                schemeItem.transform.SetAsLastSibling();
                            } else {
                                // If the scheme is compatible, send it to the top of the list.
                                schemeItem.transform.SetSiblingIndex(1);
                            }
                        }

                        foreach ( var behaviour in behaviours ) {
                            behaviour.OnAvailableSchemeLoaded(ruleResult, scheme, schemeItem.transform);
                        }

                        schemeItem.gameObject.SetActive(true);

                        availableSchemeCount++;
                    }

                    actorNavigator.SchemeRef.gameObject.SetActive(false);
                }

                if ( availableSchemeCount < 1 )
                    actorNavigator.isEmpty_Schemes?.Invoke();
                else
                    actorNavigator.isNotEmpty_Schemes?.Invoke();

                foreach ( var behaviour in behaviours ) {
                    behaviour.OnPageUpdated();
                }
            }

            void UpDate() {
                foreach ( var obj in createdObjects ) {
                    Destroy(obj);
                }

                createdObjects.Clear();
                UpdateInfo();
            }

            if ( RefreshRate > 0 ) {
                var update = NullUtils.DelayedCall(new DelayedCallParams {
                    Delay = Mathf.Clamp(RefreshRate, 1f, float.MaxValue),
                    Call = UpDate,
                    UnscaledTime = true,
                    LoopCount = -1,
                });

                // var update = NullUtils.DelayedCall(Mathf.Clamp(RefreshRate, 1f, float.MaxValue), UpDate, -1, true);

                actorNavigator.onPageClosed.AddListener(() => {
                    NullUtils.StopCall(update);
                    actorNavigator.onPageClosed.RemoveAllListeners();

                    RefreshEvent -= UpDate;
                });
            }

            RefreshEvent += UpDate;

            actorNavigator.gameObject.SetActive(true);
        }

        public void OpenClanMenu(Clan clan, bool ignoreDisableMode = false) {
            if ( Disabled && !ignoreDisableMode ) return;

            if ( ClanPanel == null || clan == null ) return;

            actions.Push(() => OpenClanMenu(clan, ignoreDisableMode));

            foreach ( var window in activeWindows ) {
                var clanPanel = window.GetComponent< ClanPanelNavigator >();
                if ( clanPanel != null ) {
                    if ( clanPanel.clan == clan ) return;
                }
            }

            var clanNavigator = ClanPanel.gameObject.Duplicate< ClanPanelNavigator >();

            // Destroy the active window if it exists.
            if ( !MultipleWindow ) {
                foreach ( var window in activeWindows ) {
                    var navigator = window.GetComponent< INavigator >();
                    if ( navigator != null ) {
                        navigator.Close();
                    }
                }

                if ( activeWindows.Any() ) {
                    clanNavigator.onPageChanged?.Invoke();
                }

                activeWindows.Clear();
            } else {
                clanNavigator.PreviousButton.gameObject.SetActive(false);
            }
            
            clanNavigator.onPageOpened?.Invoke();

            clanNavigator.clan = clan;

            var behaviours = clanNavigator.transform.GetComponentsInChildren< IUIClanBehaviour >();

            foreach ( var uiBehaviour in behaviours ) {
                uiBehaviour.OnMenuOpened(clan);
                uiBehaviour.Init(clan);
            }

            activeWindows.Add(clanNavigator.gameObject);

            clanNavigator.transform.SetAsLastSibling();

            if ( clanNavigator.CloseButton != null ) {
                clanNavigator.CloseButton.onClick.AddListener(() => {
                    if ( Disabled ) return;
                    activeWindows.Remove(clanNavigator.gameObject);
                    clanNavigator.Close();
                    actions.Clear();
                });
            }

            if ( clanNavigator.PreviousButton != null ) {
                clanNavigator.PreviousButton.onClick.AddListener(() => {
                    if ( Disabled || MultipleWindow ) return;

                    activeWindows.Remove(clanNavigator.gameObject);
                    clanNavigator.Close();

                    NullUtils.TryPop(actions, out _);

                    if ( NullUtils.TryPop(actions, out var previousAction) )
                        previousAction?.Invoke();
                });
            }

            List< GameObject > createdObjects = new List< GameObject >();

            UpdateInfo();

            void UpdateInfo() {
                if ( clanNavigator.ClanBanner != null )
                    clanNavigator.ClanBanner.sprite = clan.Icon;

                if ( clanNavigator.ClanName != null )
                    clanNavigator.ClanName.text = clan.ClanName;

                if ( clanNavigator.ClanDescription != null )
                    clanNavigator.ClanDescription.text = clan.Description;

                if ( clanNavigator.MemberCount != null )
                    clanNavigator.MemberCount.text = clan.MemberCount.ToString();

                if ( clan.Culture != null ) {
                    if ( clanNavigator.ClanCulture != null )
                        clanNavigator.ClanCulture.text = clan.Culture.CultureName;
                    if ( clanNavigator.ClanCultureIcon != null && clan.Culture.Icon != null )
                        clanNavigator.ClanCultureIcon.sprite = clan.Culture.Icon;
                }

                if ( clanNavigator.PolicyRef != null ) {
                    if ( !clan.Policies.Any() ) {
                        clanNavigator.isEmpty_Policies?.Invoke();
                    } else {
                        clanNavigator.isNotEmpty_Policies?.Invoke();
                    }

                    foreach ( var policy in clan.Policies ) {
                        var duplicatedPolicy = clanNavigator.PolicyRef.gameObject.Duplicate< PolicyRefs >();
                        duplicatedPolicy.gameObject.SetActive(true);

                        createdObjects.Add(duplicatedPolicy.gameObject);

                        if ( duplicatedPolicy.PolicyName != null )
                            duplicatedPolicy.PolicyName.text = policy.PolicyName;
                        if ( duplicatedPolicy.PolicyIcon != null )
                            duplicatedPolicy.PolicyIcon.sprite = policy.Icon;

                        foreach ( var uiBehaviour in behaviours ) {
                            uiBehaviour.OnPolicyLoaded(policy, duplicatedPolicy.transform);
                        }
                    }

                    clanNavigator.PolicyRef.gameObject.SetActive(false);
                }

                if ( clan.MemberCount < 1 )
                    clanNavigator.isEmpty_ClanMembers?.Invoke();
                else
                    clanNavigator.isNotEmpty_ClanMembers?.Invoke();


                foreach ( var memberCategory in clanNavigator.clanCategories ) {
                    if ( memberCategory == null ) continue;
                    var roleExists = IM.Roles.Any(r => r.ID == memberCategory.role);

                    if ( roleExists ) {
                        if ( clan.Members.Count(
                                m => m.State == Actor.IState.Active && m.Role?.ID == memberCategory.role) < 1 ) {
                            memberCategory.isEmpty?.Invoke();
                            continue;
                        }

                        memberCategory.isNotEmpty?.Invoke();
                    } else {
                        if ( clan.Members.Count(m => m.State == Actor.IState.Active && m.Role == null) < 1 ||
                             !string.IsNullOrEmpty(memberCategory.role) ) {
                            memberCategory.isEmpty?.Invoke();
                            continue;
                        }

                        memberCategory.isNotEmpty?.Invoke();
                    }

                    var orderByPriority = clan.Members.Where(m =>
                            !roleExists && string.IsNullOrEmpty(memberCategory.role)
                                ? m.Role == null
                                : m.Role?.ID == memberCategory.role)
                        .OrderByDescending(m => m.Role == null ? m.Age : m.Role?.Priority).Select(m => m.Role)
                        .ToHashSet();

                    // Iterate through each role and create member groups.
                    foreach ( var role in orderByPriority ) {
                        foreach ( var member in clan.Members.Where(m => m.Role == role) ) {
                            if ( !memberCategory.showDeadActors ) {
                                if ( member.State == Actor.IState.Passive ) continue;
                            }

                            var memberGroup = memberCategory.gameObject.Duplicate< ClanCategory >();
                            memberGroup.gameObject.SetActive(true);

                            createdObjects.Add(memberGroup.gameObject);

                            if ( member.State == Actor.IState.Passive )
                                memberGroup.isDead?.Invoke();

                            if ( memberGroup.button != null )
                                memberGroup.button.onClick.AddListener(() => { OpenActorMenu(member); });
                            if ( memberGroup.portrait != null && member.Portrait != null )
                                memberGroup.portrait.sprite = member.Portrait;

                            foreach ( var uiBehaviour in behaviours ) {
                                uiBehaviour.OnClanMemberLoaded(member, memberGroup.transform);
                            }
                        }
                    }


                    memberCategory.gameObject.SetActive(false);
                }

                foreach ( var behaviour in behaviours ) {
                    behaviour.OnPageUpdated();
                }
            }

            void UpDate() {
                foreach ( var obj in createdObjects ) {
                    Destroy(obj);
                }

                createdObjects.Clear();
                UpdateInfo();
            }

            if ( RefreshRate > 0 ) {
                var update = NullUtils.DelayedCall(new DelayedCallParams {
                    Delay = Mathf.Clamp(RefreshRate, 1f, float.MaxValue),
                    Call = UpDate,
                    UnscaledTime = true,
                    LoopCount = -1,
                });

                // var update = NullUtils.DelayedCall(Mathf.Clamp(RefreshRate, 1f, float.MaxValue), UpDate, -1, true);

                clanNavigator.onPageClosed.AddListener(() => {
                    NullUtils.StopCall(update);
                    clanNavigator.onPageClosed.RemoveAllListeners();

                    RefreshEvent -= UpDate;
                });
            }

            RefreshEvent += UpDate;

            clanNavigator.gameObject.SetActive(true);
        }

        public void OpenSchemeMenu(Actor actor, bool ignoreDisableMode = false) {
            if ( Disabled && !ignoreDisableMode ) return;

            if ( SchemesPanel == null || actor == null ) return;

            actions.Push(() => OpenSchemeMenu(actor, ignoreDisableMode));

            foreach ( var window in activeWindows ) {
                var schemesPanel = window.GetComponent< SchemesPanelNavigator >();
                if ( schemesPanel != null ) {
                    if ( schemesPanel.actor == actor ) return;

                    if ( !MultipleWindow ) {
                        schemesPanel.onPageChanged?.Invoke();
                    } else {
                        schemesPanel.onPageOpened?.Invoke();
                    }
                }
            }

            var schemesNavigator = SchemesPanel.gameObject.Duplicate< SchemesPanelNavigator >();

            // Destroy the active window if it exists.
            if ( !MultipleWindow ) {
                foreach ( var window in activeWindows ) {
                    var navigator = window.GetComponent< INavigator >();
                    if ( navigator != null ) {
                        navigator.Close();
                    }
                }

                if ( activeWindows.Any() ) {
                    schemesNavigator.onPageChanged?.Invoke();
                }

                activeWindows.Clear();
            } else {
                schemesNavigator.PreviousButton.gameObject.SetActive(false);
            }
            
            schemesNavigator.onPageOpened?.Invoke();

            schemesNavigator.actor = actor;

            var behaviours = schemesNavigator.transform.GetComponentsInChildren< IUISchemeBehaviour >();

            foreach ( var uiBehaviour in behaviours ) {
                uiBehaviour.OnMenuOpened(actor);
                uiBehaviour.Init(actor);
            }

            activeWindows.Add(schemesNavigator.gameObject);

            // Set the sibling index of the clanReference game object to be one index higher than SchemesPanelNavigator.
            schemesNavigator.transform.SetAsLastSibling();

            if ( schemesNavigator.CloseButton != null ) {
                schemesNavigator.CloseButton.onClick.AddListener(() => {
                    if ( Disabled ) return;
                    activeWindows.Remove(schemesNavigator.gameObject);
                    schemesNavigator.Close();
                    actions.Clear();
                });
            }

            if ( schemesNavigator.PreviousButton != null ) {
                schemesNavigator.PreviousButton.onClick.AddListener(() => {
                    if ( Disabled || MultipleWindow ) return;

                    activeWindows.Remove(schemesNavigator.gameObject);
                    schemesNavigator.Close();

                    NullUtils.TryPop(actions, out _);

                    if ( NullUtils.TryPop(actions, out var previousAction) )
                        previousAction?.Invoke();
                });
            }

            List< GameObject > createdObjects = new List< GameObject >();

            UpdateInfo();

            void UpdateInfo() {
                if ( !actor.Schemes.Any() )
                    schemesNavigator.isEmpty?.Invoke();
                else
                    schemesNavigator.isNotEmpty?.Invoke();

                if ( schemesNavigator.ActiveSchemeRefs != null ) {
                    // Iterate over each scheme associated with the actor
                    foreach ( var scheme in actor.Schemes.OrderBy(s => s.TargetNotRequired) ) {
                        if ( scheme.HideOnUI ) continue;
                        var schemeRef = schemesNavigator.ActiveSchemeRefs.gameObject.Duplicate< ActiveSchemeRefs >();

                        if ( scheme.Schemer.Target == null ) {
                            schemeRef.targetIsNull?.Invoke();
                        }

                        schemeRef.gameObject.SetActive(true);

                        createdObjects.Add(schemeRef.gameObject);

                        if ( schemeRef.SchemeName != null )
                            schemeRef.SchemeName.text = scheme.SchemeName;
                        if ( schemeRef.SchemeDescription != null )
                            schemeRef.SchemeDescription.text = scheme.Description;
                        if ( schemeRef.SchemeIcon != null && scheme.Icon != null )
                            schemeRef.SchemeIcon.sprite = scheme.Icon;
                        if ( schemeRef.SchemeObjective != null )
                            schemeRef.SchemeObjective.text = scheme.CurrentObjective;
                        if ( scheme.Schemer.Conspirator != null && schemeRef.ConspiratorPortrait != null &&
                             scheme.Schemer.Conspirator.Portrait != null )
                            schemeRef.ConspiratorPortrait.sprite = scheme.Schemer.Conspirator.Portrait;
                        if ( scheme.Schemer.Target != null && schemeRef.TargetPortrait != null &&
                             scheme.Schemer.Target.Portrait != null )
                            schemeRef.TargetPortrait.sprite = scheme.Schemer.Target.Portrait;

                        if ( schemeRef.ConspiratorButton != null )
                            schemeRef.ConspiratorButton.onClick.AddListener(() => {
                                OpenActorMenu(scheme.Schemer.Conspirator);
                            });
                        if ( schemeRef.TargetButton != null )
                            schemeRef.TargetButton.onClick.AddListener(() => { OpenActorMenu(scheme.Schemer.Target); });

                        foreach ( var uiBehaviour in behaviours ) {
                            uiBehaviour.OnActiveSchemeLoaded(scheme, schemeRef.transform);
                        }
                    }

                    schemesNavigator.ActiveSchemeRefs.gameObject.SetActive(false);
                }

                foreach ( var behaviour in behaviours ) {
                    behaviour.OnPageUpdated();
                }
            }

            void UpDate() {
                foreach ( var obj in createdObjects ) {
                    Destroy(obj);
                }

                createdObjects.Clear();
                UpdateInfo();
            }

            if ( RefreshRate > 0 ) {
                var update = NullUtils.DelayedCall(new DelayedCallParams {
                    Delay = Mathf.Clamp(RefreshRate, 1f, float.MaxValue),
                    Call = UpDate,
                    UnscaledTime = true,
                    LoopCount = -1,
                });

                // var update = NullUtils.DelayedCall(Mathf.Clamp(RefreshRate, 1f, float.MaxValue), UpDate, -1, true);

                schemesNavigator.onPageClosed.AddListener(() => {
                    NullUtils.StopCall(update);
                    schemesNavigator.onPageClosed.RemoveAllListeners();

                    RefreshEvent -= UpDate;
                });
            }

            RefreshEvent += UpDate;

            schemesNavigator.gameObject.SetActive(true);
        }

        public void Refresh() => RefreshEvent?.Invoke();

        public void CloseWindows() {
            foreach ( var iNavigator in activeWindows.Select(window => window.GetComponent< INavigator >())
                         .Where(iNavigator => iNavigator != null) ) {
                iNavigator.Close();
            }

            activeWindows.Clear();
        }

        public void Hide() {
            foreach ( var iNavigator in activeWindows.Select(window => window.GetComponent< INavigator >())
                         .Where(iNavigator => iNavigator != null) ) {
                iNavigator.Hide();
            }
        }

        public void Show() {
            foreach ( var iNavigator in activeWindows.Select(window => window.GetComponent< INavigator >())
                         .Where(iNavigator => iNavigator != null) ) {
                iNavigator.Show();
            }
        }

        public void Enable() {
            Disabled = false;
        }

        public void Disable() {
            Disabled = true;
        }

        public static string GetRelationName(Actor target) {
            // Check if the target is the same as the player
            if ( IM.Player == target ) return "It's You";

            // Check if the target is a parent of the player
            if ( IM.Player.IsParent(target) ) {
                // Check the gender of the parent
                return target.Gender == Actor.IGender.Male ? "Your Father" : "Your Mother";
            }

            // Check if the target is a spouse of the player
            if ( IM.Player.IsSpouse(target) ) {
                // Check the gender of the spouse
                return target.Gender == Actor.IGender.Male ? "Your Husband" : "Your Wife";
            }

            // Check if the target is a child of the player
            if ( IM.Player.IsChild(target) ) {
                // Check the gender of the child
                return target.Gender == Actor.IGender.Male ? "Your Son" : "Your Daughter";
            }

            // Check if the target is a sibling of the player
            if ( IM.Player.IsSibling(target) ) {
                // Check the gender of the sibling
                return target.Gender == Actor.IGender.Male ? "Your Brother" : "Your Sister";
            }

            // Check if the target is a grandchild of the player
            if ( IM.Player.IsGrandchild(target) ) {
                // Check the gender of the grandchild
                return target.Gender == Actor.IGender.Male ? "Your Grandson" : "Your Granddaughter";
            }

            // Check if the target is a grandparent of the player
            if ( IM.Player.IsGrandparent(target) ) {
                // Check the gender of the grandparent
                return target.Gender == Actor.IGender.Male ? "Your Grandfather" : "Your Grandmother";
            }

            // Check if the target is a uncle of the player
            if ( IM.Player.IsUncle(target) ) {
                return "Your Uncle";
            }

            // Check if the target is a aunt of the player
            if ( IM.Player.IsAunt(target) ) {
                return "Your Aunt";
            }

            // Check if the target is a nephew of the player
            if ( IM.Player.IsNephew(target) ) {
                return "Your Nephew";
            }

            // Check if the target is a niece of the player
            if ( IM.Player.IsNiece(target) ) {
                return "Your Niece";
            }

            // Check if the target is a sister-in-law of the player
            if ( IM.Player.IsSisterInLaw(target) ) {
                return "Your Sister-In-Law";
            }

            // Check if the target is a brother-in-law of the player
            if ( IM.Player.IsBrotherInLaw(target) ) {
                return "Your Brother-In-Law";
            }

            // Check if the player has a family.
            if ( IM.Player.Family != null ) {
                // Check if the target belongs to the same family as the player.
                if ( IM.Player.Family == target.Family ) {
                    return "Same Family";
                }
            }

            // If none of the conditions above are met, return null
            return null;
        }
    }

    #region EDITOR

#if UNITY_EDITOR
    [CustomEditor(typeof( IntrigueSystemUI ))]
    public class IntriguesUIEditor : Editor {
        SerializedProperty actorKey;
        SerializedProperty clanKey;
        SerializedProperty schemesKey;
        SerializedProperty multipleWindow;

        private bool settingsFold = true;

        private void OnEnable() {
            actorKey = serializedObject.FindProperty("actorMenuKey");
            schemesKey = serializedObject.FindProperty("schemesMenuKey");
            clanKey = serializedObject.FindProperty("clanMenuKey");

            multipleWindow = serializedObject.FindProperty("MultipleWindow");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            settingsFold = EditorGUILayout.Foldout(settingsFold, "Settings");
            if ( settingsFold ) {
                EditorGUILayout.PropertyField(actorKey);
                EditorGUILayout.PropertyField(clanKey);
                EditorGUILayout.PropertyField(schemesKey);
                EditorGUILayout.PropertyField(multipleWindow);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

#endif

    #endregion
}