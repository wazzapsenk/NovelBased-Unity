using System.Collections.Generic;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class RuleSearchMenu : ScriptableObject, ISearchWindowProvider
    {
        private IEGraphView ieGraphView;
        private Texture2D spaceIcon;

        public void Init(IEGraphView _ieGraphView)
        {
            ieGraphView = _ieGraphView;

            spaceIcon = new Texture2D(1, 1);
            spaceIcon.SetPixel(0, 0, Color.clear);
            spaceIcon.Apply();
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var searchTreeEntries = new List<SearchTreeEntry>()
            {
                new SearchTreeGroupEntry(new GUIContent("Elements")), 
                new(new GUIContent("New Rule", spaceIcon))
                {
                    level = 1, userData = 0
                },
                new(new GUIContent("Log", spaceIcon))
                {
                    level = 1, userData = 133
                },
                new(new GUIContent("Success", spaceIcon))
                {
                    level = 1, userData = 1
                },
                new(new GUIContent("Error", spaceIcon))
                {
                    level = 1, userData = 31
                },
                new(new GUIContent("Warning", spaceIcon))
                {
                    level = 1, userData = 32
                },                
                new(new GUIContent("Comment", spaceIcon))
                {
                    level = 1, userData = 33
                },
                new(new GUIContent("Invoke", spaceIcon))
                {
                    level = 1, userData = 47
                },
                new(new GUIContent("Return [Actor]", spaceIcon))
                {
                    level = 1, userData = 333
                },                
                new(new GUIContent("Return [Dual] Actor", spaceIcon))
                {
                    level = 1, userData = 334
                },
                new(new GUIContent("Return [Clan]", spaceIcon))
                {
                    level = 1, userData = 335
                },
                new(new GUIContent("Return [Family]", spaceIcon))
                {
                    level = 1, userData = 33515
                },
                new SearchTreeGroupEntry(new GUIContent("All"), 1),
                new(new GUIContent("Get Variable", spaceIcon))
                {
                    level = 2, userData = 2
                },        
                new(new GUIContent("Get [Clan] Variable", spaceIcon))
                {
                    level = 2, userData = 8879
                },
                new(new GUIContent("Get [Family] Variable", spaceIcon))
                {
                    level = 2, userData = 8880
                },
                new(new GUIContent("Get Relation Variable", spaceIcon))
                {
                    level = 2, userData = 336
                },  
                new(new GUIContent("Get Relation Variable", spaceIcon))
                {
                    level = 2, userData = 336
                },  
                new(new GUIContent("Scheme Is Active", spaceIcon))
                {
                    level = 2, userData = 99
                },
                // new(new GUIContent("Get Active Scheme Variable | Table", spaceIcon))
                // {
                //     level = 2, userData = 1444
                // },
                new(new GUIContent("Check Actor", spaceIcon))
                {
                    level = 2, userData = 3
                },
                new(new GUIContent("Check Clan", spaceIcon))
                {
                    level = 2, userData = 4
                },
                new(new GUIContent("Check Family", spaceIcon))
                {
                    level = 2, userData = 5
                },
                new(new GUIContent("Check Role", spaceIcon))
                {
                    level = 2, userData = 6
                },
                new(new GUIContent("Check Culture", spaceIcon))
                {
                    level = 2, userData = 7
                },
                new(new GUIContent("Has Policy", spaceIcon))
                {
                    level = 2, userData = 8
                },
                new(new GUIContent("Same Clan", spaceIcon))
                {
                    level = 2, userData = 9
                },
                new(new GUIContent("Same Family", spaceIcon))
                {
                    level = 2, userData = 10
                },
                new(new GUIContent("Same Culture", spaceIcon))
                {
                    level = 2, userData = 11
                },
                new(new GUIContent("Same Gender", spaceIcon))
                {
                    level = 2, userData = 12
                },
                new(new GUIContent("Age", spaceIcon))
                {
                    level = 2, userData = 13
                },
                new(new GUIContent("Gender", spaceIcon))
                {
                    level = 2, userData = 14
                },
                new(new GUIContent("Is AI", spaceIcon))
                {
                    level = 2, userData = 15
                },
                new(new GUIContent("Check State", spaceIcon))
                {
                    level = 2, userData = 16
                },
                new(new GUIContent("Is Relative", spaceIcon))
                {
                    level = 2, userData = 17
                },
                new(new GUIContent("Is Parent", spaceIcon))
                {
                    level = 2, userData = 18
                },
                new(new GUIContent("Is Grandparent", spaceIcon))
                {
                    level = 2, userData = 19
                },
                new(new GUIContent("Is Grandchildren", spaceIcon))
                {
                    level = 2, userData = 20
                },
                new(new GUIContent("Is Spouse", spaceIcon))
                {
                    level = 2, userData = 21
                },
                new(new GUIContent("Is Child", spaceIcon))
                {
                    level = 2, userData = 22
                },
                new(new GUIContent("Is Sibling", spaceIcon))
                {
                    level = 2, userData = 23
                },
                new(new GUIContent("Has Heir", spaceIcon))
                {
                    level = 2, userData = 24
                },
                new(new GUIContent("Parent Count", spaceIcon))
                {
                    level = 2, userData = 25
                },
                new(new GUIContent("Grandparent Count", spaceIcon))
                {
                    level = 2, userData = 26
                },
                new(new GUIContent("Grandchildren Count", spaceIcon))
                {
                    level = 2, userData = 27
                },
                new(new GUIContent("Children Count", spaceIcon))
                {
                    level = 2, userData = 28
                },
                new(new GUIContent("Spouse Count", spaceIcon))
                {
                    level = 2, userData = 29
                },
                new(new GUIContent("Sibling Count", spaceIcon))
                {
                    level = 2, userData = 30
                },
            };

            return searchTreeEntries;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            if (!ieGraphView.graphElements.ToList().Exists(e => e is RuleGroup) &&
                SearchTreeEntry.userData is not 0)
            {
                NDebug.Log("You need a Rule Group to create a Node.", NLogType.Error);
                return false;
            }

            var localPosition = ieGraphView.GetLocalMousePosition(context.screenMousePosition, true);
            switch (SearchTreeEntry.userData)
            {
                case 0:
                {
                    ieGraphView.CreateRuleGroup(localPosition);
                    return true;
                }
                case 1:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SuccessRuleNode>(localPosition, true, true));
                    return true;
                }
                case 2:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetVariableRuleNode>(localPosition, true, true));
                    return true;
                }
                case 8879:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetClanVariableRuleNode>(localPosition, true, true));
                    return true;
                }
                case 8880:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetFamilyVariableRuleNode>(localPosition, true, true));
                    return true;
                }
                case 3:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetActorRuleNode>(localPosition, true, true));
                    return true;
                }
                case 4:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetClanRuleNode>(localPosition, true, true));
                    return true;
                }
                case 5:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetFamilyRuleNode>(localPosition, true, true));
                    return true;
                }
                case 6:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetRoleRuleNode>(localPosition, true, true));
                    return true;
                }
                case 7:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetCultureRuleNode>(localPosition, true, true));
                    return true;
                }
                case 8:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetPolicyRuleNode>(localPosition, true, true));
                    return true;
                }
                case 9:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SameClanRuleNode>(localPosition, true, true));
                    return true;
                }
                case 10:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SameFamilyRuleNode>(localPosition, true, true));
                    return true;
                }
                case 11:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SameCultureRuleNode>(localPosition, true, true));
                    return true;
                }
                case 12:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SameGenderRuleNode>(localPosition, true, true));
                    return true;
                }
                case 13:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<AgeRuleNode>(localPosition, true, true));
                    return true;
                }
                case 14:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GenderRuleNode>(localPosition, true, true));
                    return true;
                }
                case 15:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<IsAIRuleNode>(localPosition, true, true));
                    return true;
                }
                case 16:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetStateRuleNode>(localPosition, true, true));
                    return true;
                }
                case 17:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<IsRelativeRuleNode>(localPosition, true, true));
                    return true;
                }
                case 18:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<IsParentRuleNode>(localPosition, true, true));
                    return true;
                }
                case 19:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<IsGrandParentRuleNode>(localPosition, true, true));
                    return true;
                }
                case 20:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<IsGrandChildRuleNode>(localPosition, true, true));
                    return true;
                }
                case 21:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<IsSpouseRuleNode>(localPosition, true, true));
                    return true;
                }
                case 22:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<IsChildRuleNode>(localPosition, true, true));
                    return true;
                }
                case 23:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<IsSiblingRuleNode>(localPosition, true, true));
                    return true;
                }
                case 24:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<HasHeirRuleNode>(localPosition, true, true));
                    return true;
                }
                case 25:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ParentCountRuleNode>(localPosition, true, true));
                    return true;
                }
                case 26:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GrandparentCountRuleNode>(localPosition, true, true));
                    return true;
                }
                case 27:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GrandchildCountRuleNode>(localPosition, true, true));
                    return true;
                }
                case 28:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ChildCountRuleNode>(localPosition, true, true));
                    return true;
                }
                case 29:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SpouseCountRuleNode>(localPosition, true, true));
                    return true;
                }
                case 30:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SiblingCountRuleNode>(localPosition, true, true));
                    return true;
                }                
                case 31:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ErrorRuleNode>(localPosition, true, true));
                    return true;
                }
                case 32:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<WarningRuleNode>(localPosition, true, true));
                    return true;
                }                
                case 33:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<CommentRuleNode>(localPosition, true, true));
                    return true;
                }
                case 99:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SchemeIsActiveRuleNode>(localPosition, true, true));
                    return true;
                }
                case 333: {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ReturnActorRuleNode>(localPosition, true, true));
                    return true;
                }
                case 334: {
                    ieGraphView.AddElement(ieGraphView.CreateNode<DualActorRuleNode>(localPosition, true, true));
                    return true;
                }
                case 335: {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ReturnClanRuleNode>(localPosition, true, true));
                    return true;
                }
                case 33515: {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ReturnFamilyRuleNode>(localPosition, true, true));
                    return true;
                }
                case 336: {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetRelationVariableRuleNode>(localPosition, true, true));
                    return true;
                }
                case 1444:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetSchemeTableVariableRuleNode>(localPosition, true, true));
                    return true;
                }
                case 47: {
                    ieGraphView.AddElement(ieGraphView.CreateNode<InvokeRuleNode>(localPosition, true, true));
                    return true;
                }
                case 133:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<LogRuleNode>(localPosition, true, true));
                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }
    }
}