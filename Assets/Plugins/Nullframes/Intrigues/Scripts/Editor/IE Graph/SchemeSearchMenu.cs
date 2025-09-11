using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class SchemeSearchMenu : ScriptableObject, ISearchWindowProvider
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
                new(new GUIContent("Create Story", spaceIcon))
                {
                    level = 1, userData = 0
                },
                new(new GUIContent("Dialogue", spaceIcon))
                {
                    level = 1, userData = 1
                },
                new(new GUIContent("Global Message", spaceIcon))
                {
                    level = 1, userData = 57
                },
                new(new GUIContent("Comment(LR)", spaceIcon))
                {
                    level = 1, userData = 12
                },
                new(new GUIContent("Comment(RL)", spaceIcon))
                {
                    level = 1, userData = 113
                },
                new(new GUIContent("Validator", spaceIcon))
                {
                    level = 1, userData = 9999
                },                
                new(new GUIContent("Return [Actor]", spaceIcon))
                {
                    level = 1, userData = 4777
                },                    
                new(new GUIContent("Return [Dual] Actor", spaceIcon))
                {
                    level = 1, userData = 4776
                },                
                new(new GUIContent("Return [Clan]", spaceIcon))
                {
                    level = 1, userData = 4778
                },
                new(new GUIContent("Return [Family]", spaceIcon))
                {
                    level = 1, userData = 4779
                },
                new(new GUIContent("Set Actor", spaceIcon))
                {
                    level = 1, userData = 8787
                },
                new SearchTreeGroupEntry(new GUIContent("Clan Nodes"), 1),
                new(new GUIContent("Set Role", spaceIcon))
                {
                    level = 2, userData = 43
                },
                new(new GUIContent("Check Role", spaceIcon))
                {
                    level = 2, userData = 22
                },
                new(new GUIContent("Set Clan", spaceIcon))
                {
                    level = 2, userData = 211
                },
                new(new GUIContent("Check Clan", spaceIcon))
                {
                    level = 2, userData = 21
                },
                new(new GUIContent("Add Policy", spaceIcon))
                {
                    level = 2, userData = 49
                },
                new(new GUIContent("Remove Policy", spaceIcon))
                {
                    level = 2, userData = 50
                },
                new(new GUIContent("Has Policy", spaceIcon))
                {
                    level = 2, userData = 51
                },
                new(new GUIContent("Inheritor", spaceIcon))
                {
                    level = 2, userData = 5464
                },
                new SearchTreeGroupEntry(new GUIContent("Actor Nodes"), 1), new(new GUIContent("Set State", spaceIcon))
                {
                    level = 2, userData = 42
                },
                new(new GUIContent("Check State", spaceIcon))
                {
                    level = 2, userData = 44
                },
                new(new GUIContent("Set Culture", spaceIcon))
                {
                    level = 2, userData = 52
                },
                new(new GUIContent("Marry", spaceIcon))
                {
                    level = 2, userData = 53
                },
                new(new GUIContent("Divorce", spaceIcon))
                {
                    level = 2, userData = 54
                },
                new SearchTreeGroupEntry(new GUIContent("Probability Nodes"), 1),
                new(new GUIContent("Chance", spaceIcon))
                {
                    level = 2, userData = 2
                },
                new(new GUIContent("Chance Modifier", spaceIcon))
                {
                    level = 2, userData = 122
                },
                new(new GUIContent("Random", spaceIcon))
                {
                    level = 2, userData = 3
                },
                new SearchTreeGroupEntry(new GUIContent("Trigger Nodes"), 1),
                new(new GUIContent("Wait Trigger", spaceIcon))
                {
                    level = 2, userData = 5
                },
                new(new GUIContent("Trigger", spaceIcon))
                {
                    level = 2, userData = 6
                },
                new(new GUIContent("Invoke", spaceIcon))
                {
                    level = 2, userData = 47
                },
                new(new GUIContent("Signal", spaceIcon))
                {
                    level = 2, userData = 59
                },
                new SearchTreeGroupEntry(new GUIContent("Action Nodes"), 1),
                new(new GUIContent("Success Scheme", spaceIcon))
                {
                    level = 2, userData = 24
                },
                new(new GUIContent("Fail Scheme", spaceIcon))
                {
                    level = 2, userData = 25
                },
                new(new GUIContent("Objective", spaceIcon))
                {
                    level = 2, userData = 55
                },
                new(new GUIContent("Play Sound", spaceIcon))
                {
                    level = 2, userData = 9
                },
                new(new GUIContent("Set Conspirator", spaceIcon))
                {
                    level = 2, userData = 9494
                },
                new(new GUIContent("Set Target", spaceIcon))
                {
                    level = 2, userData = 9595
                },
                new(new GUIContent("Log", spaceIcon))
                {
                    level = 2, userData = 13
                },
                new SearchTreeGroupEntry(new GUIContent("Control Flow Nodes"), 1),
                new(new GUIContent("Sequencer", spaceIcon))
                {
                    level = 2, userData = 11
                },
                new(new GUIContent("Break Sequencer", spaceIcon))
                {
                    level = 2, userData = 104
                },
                new(new GUIContent("Skip Sequencer", spaceIcon))
                {
                    level = 2, userData = 111
                },
                new(new GUIContent("Repeater", spaceIcon))
                {
                    level = 2, userData = 10
                },
                new(new GUIContent("Break Repeater", spaceIcon))
                {
                    level = 2, userData = 103
                },
                new(new GUIContent("New Flow", spaceIcon))
                {
                    level = 2, userData = 999
                },                
                new(new GUIContent("Background Worker", spaceIcon))
                {
                    level = 2, userData = 998
                },
                new(new GUIContent("Wait", spaceIcon))
                {
                    level = 2, userData = 4
                },
                new(new GUIContent("Wait(Range)", spaceIcon))
                {
                    level = 2, userData = 444
                },
                new(new GUIContent("Continue", spaceIcon))
                {
                    level = 2, userData = 555
                },
                new(new GUIContent("Break", spaceIcon))
                {
                    level = 2, userData = 888
                },
                new(new GUIContent("Go To", spaceIcon))
                {
                    level = 2, userData = 666
                },
                new(new GUIContent("Link", spaceIcon))
                {
                    level = 2, userData = 777
                },
                new(new GUIContent("Pause", spaceIcon))
                {
                    level = 2, userData = 881
                },
                new(new GUIContent("Resume", spaceIcon))
                {
                    level = 2, userData = 882
                },
                new SearchTreeGroupEntry(new GUIContent("Variable Nodes"), 1),
                new(new GUIContent("Wait Until(Variable)", spaceIcon))
                {
                    level = 2, userData = 8888
                },
                new(new GUIContent("Set Variable", spaceIcon))
                {
                    level = 2, userData = 7
                },
                new(new GUIContent("Get Variable", spaceIcon))
                {
                    level = 2, userData = 8
                },
                new(new GUIContent("Set [Clan] Variable", spaceIcon))
                {
                    level = 2, userData = 7879
                },
                new(new GUIContent("Get [Clan] Variable", spaceIcon))
                {
                    level = 2, userData = 8879
                },
                new(new GUIContent("Set [Family] Variable", spaceIcon))
                {
                    level = 2, userData = 7878
                },
                new(new GUIContent("Get [Family] Variable", spaceIcon))
                {
                    level = 2, userData = 8880
                },
                new(new GUIContent("Set Relation Variable", spaceIcon))
                {
                    level = 2, userData = 7777
                },                
                new(new GUIContent("Get Relation Variable", spaceIcon))
                {
                    level = 2, userData = 7778
                },
                new(new GUIContent("Set Variable | Table", spaceIcon))
                {
                    level = 2, userData = 15
                },
                new(new GUIContent("Get Variable | Table", spaceIcon))
                {
                    level = 2, userData = 14
                },
                // new(new GUIContent("Get Active Scheme Variable | Table", spaceIcon))
                // {
                //     level = 2, userData = 1444
                // },
                new SearchTreeGroupEntry(new GUIContent("Conditional Nodes"), 1),
                new(new GUIContent("Is Compatible", spaceIcon))
                {
                    level = 2, userData = 48
                },
                new(new GUIContent("Scheme State", spaceIcon))
                {
                    level = 2, userData = 987
                },                
                new(new GUIContent("Scheme Is Active", spaceIcon))
                {
                    level = 2, userData = 933
                },
                new(new GUIContent("Is AI", spaceIcon))
                {
                    level = 2, userData = 16
                },
                new(new GUIContent("Gender", spaceIcon))
                {
                    level = 2, userData = 18
                },
                new(new GUIContent("Check Actor", spaceIcon))
                {
                    level = 2, userData = 19
                },
                new(new GUIContent("Check Culture", spaceIcon))
                {
                    level = 2, userData = 20
                },
                new(new GUIContent("Check Family", spaceIcon))
                {
                    level = 2, userData = 23
                },
                new(new GUIContent("Age", spaceIcon))
                {
                    level = 2, userData = 17
                },
                new(new GUIContent("Children Count", spaceIcon))
                {
                    level = 2, userData = 26
                },
                new(new GUIContent("Spouse Count", spaceIcon))
                {
                    level = 2, userData = 27
                },
                new(new GUIContent("Sibling Count", spaceIcon))
                {
                    level = 2, userData = 45
                },
                new(new GUIContent("Parent Count", spaceIcon))
                {
                    level = 2, userData = 46
                },
                new(new GUIContent("Grandchildren Count", spaceIcon))
                {
                    level = 2, userData = 101
                },
                new(new GUIContent("Grandparent Count", spaceIcon))
                {
                    level = 2, userData = 102
                },
                new(new GUIContent("Same Family", spaceIcon))
                {
                    level = 2, userData = 28
                },
                new(new GUIContent("Same Clan", spaceIcon))
                {
                    level = 2, userData = 29
                },
                new(new GUIContent("Same Culture", spaceIcon))
                {
                    level = 2, userData = 30
                },
                new(new GUIContent("Same Gender", spaceIcon))
                {
                    level = 2, userData = 31
                },
                new(new GUIContent("Has Heir", spaceIcon))
                {
                    level = 2, userData = 58
                },
                new(new GUIContent("Is Relative", spaceIcon))
                {
                    level = 2, userData = 32
                },
                new(new GUIContent("Is Parent", spaceIcon))
                {
                    level = 2, userData = 34
                },
                new(new GUIContent("Is Grandparent", spaceIcon))
                {
                    level = 2, userData = 35
                },
                new(new GUIContent("Is Grandchildren", spaceIcon))
                {
                    level = 2, userData = 36
                },
                new(new GUIContent("Is Child", spaceIcon))
                {
                    level = 2, userData = 33
                },
                new(new GUIContent("Is Sibling", spaceIcon))
                {
                    level = 2, userData = 37
                },
                new(new GUIContent("Is Spouse", spaceIcon))
                {
                    level = 2, userData = 38
                },
                new SearchTreeGroupEntry(new GUIContent("Save System"), 1),
                new(new GUIContent("On Load", spaceIcon))
                {
                    level = 2, userData = 2153
                },
                new SearchTreeGroupEntry(new GUIContent("Others"), 1),
                new(new GUIContent("Choice Data", spaceIcon))
                {
                    level = 2, userData = 199
                },                
                new(new GUIContent("Sound Class", spaceIcon))
                {
                    level = 2, userData = 1122
                },
                new(new GUIContent("Voice Class", spaceIcon))
                {
                    level = 2, userData = 1123
                },
                new(new GUIContent("Key Handler", spaceIcon))
                {
                    level = 2, userData = 1124
                },
            };

            return searchTreeEntries;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            if (!ieGraphView.graphElements.ToList().Exists(e => e is SchemeGroup) &&
                SearchTreeEntry.userData is not 0)
            {
                NDebug.Log("You need a Scheme Group to create a Node.", NLogType.Error);
                return false;
            }

            var localPosition = ieGraphView.GetLocalMousePosition(context.screenMousePosition, true);
            switch (SearchTreeEntry.userData)
            {
                case 0:
                {
                    ieGraphView.CreateSchemeGroup(localPosition);
                    return true;
                }
                case 1:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<DialogueNode>(localPosition, true, true));
                    return true;
                }
                case 2:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ChanceNode>(localPosition, true, true));
                    return true;
                }
                case 122:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ChanceModifierNode>(localPosition, true, true));
                    return true;
                }
                case 3:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<RandomNode>(localPosition, true, true));
                    return true;
                }
                case 4:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<WaitNode>(localPosition, true, true));
                    return true;
                }
                case 444:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<WaitRandomNode>(localPosition, true, true));
                    return true;
                }
                case 555:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ContinueNode>(localPosition, true, true));
                    return true;
                }
                case 888:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<BreakNode>(localPosition, true, true));
                    return true;
                }
                case 881:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<PauseNode>(localPosition, true, true));
                    return true;
                }
                case 882:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ResumeNode>(localPosition, true, true));
                    return true;
                }
                case 666:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<JumpNode>(localPosition, true, true));
                    return true;
                }
                case 777:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<LinkedNode>(localPosition, true, true));
                    return true;
                }
                case 5:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<WaitTriggerNode>(localPosition, true, true));
                    return true;
                }
                case 6:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<TriggerNode>(localPosition, true, true));
                    return true;
                }
                case 7:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SetVariableNode>(localPosition, true, true));
                    return true;
                }
                case 7878:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SetFamilyVariableNode>(localPosition, true, true));
                    return true;
                }
                case 7879:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SetClanVariableNode>(localPosition, true, true));
                    return true;
                }
                case 8:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetVariableNode>(localPosition, true, true));
                    return true;
                }
                case 8879:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetClanVariableNode>(localPosition, true, true));
                    return true;
                }
                case 8880:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetFamilyVariableNode>(localPosition, true, true));
                    return true;
                }
                case 8787:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SetActorNode>(localPosition, true, true));
                    return true;
                }
                case 7777:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SetRelationVariableNode>(localPosition, true, true));
                    return true;
                }                
                case 7778:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetRelationVariableNode>(localPosition, true, true));
                    return true;
                }
                case 8888:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<WaitUntilNode>(localPosition, true, true));
                    return true;
                }
                case 9:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SoundNode>(localPosition, true, true));
                    return true;
                }
                case 10:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<RepeaterNode>(localPosition, true, true));
                    return true;
                }
                case 11:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SequencerNode>(localPosition, true, true));
                    return true;
                }
                case 111:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SkipSequencerNode>(localPosition, true, true));
                    return true;
                }
                case 12:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<CommentLeftToRightNode>(localPosition, true, true));
                    return true;
                }
                case 113:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<CommentRightToLeftNode>(localPosition, true, true));
                    return true;
                }
                case 13:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<LogNode>(localPosition, true, true));
                    return true;
                }
                case 14:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetTableVariableNode>(localPosition, true, true));
                    return true;
                }
                case 1444:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetSchemeTableVariableNode>(localPosition, true, true));
                    return true;
                }
                case 15:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SetTableVariableNode>(localPosition, true, true));
                    return true;
                }
                case 16:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<IsAINode>(localPosition, true, true));
                    return true;
                }
                case 17:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<AgeNode>(localPosition, true, true));
                    return true;
                }
                case 18:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GenderNode>(localPosition, true, true));
                    return true;
                }
                case 19:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetActorNode>(localPosition, true, true));
                    return true;
                }
                case 20:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetCultureNode>(localPosition, true, true));
                    return true;
                }
                case 21:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetClanNode>(localPosition, true, true));
                    return true;
                }                
                case 211:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SetClanNode>(localPosition, true, true));
                    return true;
                }
                case 22:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetRoleNode>(localPosition, true, true));
                    return true;
                }
                case 23:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetFamilyNode>(localPosition, true, true));
                    return true;
                }
                case 24:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SuccessSchemeNode>(localPosition, true, true));
                    return true;
                }
                case 25:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<FailSchemeNode>(localPosition, true, true));
                    return true;
                }
                case 26:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ChildCountNode>(localPosition, true, true));
                    return true;
                }
                case 27:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SpouseCountNode>(localPosition, true, true));
                    return true;
                }
                case 28:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SameFamilyNode>(localPosition, true, true));
                    return true;
                }
                case 29:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SameClanNode>(localPosition, true, true));
                    return true;
                }
                case 30:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SameCultureNode>(localPosition, true, true));
                    return true;
                }
                case 31:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SameGenderNode>(localPosition, true, true));
                    return true;
                }
                case 32:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<IsRelativeNode>(localPosition, true, true));
                    return true;
                }
                case 33:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<IsChildNode>(localPosition, true, true));
                    return true;
                }
                case 34:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<IsParentNode>(localPosition, true, true));
                    return true;
                }
                case 35:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<IsGrandParentNode>(localPosition, true, true));
                    return true;
                }
                case 36:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<IsGrandChildNode>(localPosition, true, true));
                    return true;
                }
                case 37:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<IsSiblingNode>(localPosition, true, true));
                    return true;
                }
                case 38:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<IsSpouseNode>(localPosition, true, true));
                    return true;
                }
                case 42:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SetStateNode>(localPosition, true, true));
                    return true;
                }
                case 43:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SetRoleNode>(localPosition, true, true));
                    return true;
                }
                case 44:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetStateNode>(localPosition, true, true));
                    return true;
                }
                case 45:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SiblingCountNode>(localPosition, true, true));
                    return true;
                }
                case 46:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ParentCountNode>(localPosition, true, true));
                    return true;
                }
                case 101:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GrandchildCountNode>(localPosition, true, true));
                    return true;
                }
                case 102:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GrandparentCountNode>(localPosition, true, true));
                    return true;
                }
                case 47:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<InvokeNode>(localPosition, true, true));
                    return true;
                }                
                case 4777:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ReturnActorNode>(localPosition, true, true));
                    return true;
                }                   
                case 4776:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<DualActorNode>(localPosition, true, true));
                    return true;
                }                
                case 4778:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ReturnClanNode>(localPosition, true, true));
                    return true;
                }
                case 4779:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ReturnFamilyNode>(localPosition, true, true));
                    return true;
                }
                case 48:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<RuleNode>(localPosition, true, true));
                    return true;
                }
                case 49:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<AddPolicyNode>(localPosition, true, true));
                    return true;
                }
                case 50:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<RemovePolicyNode>(localPosition, true, true));
                    return true;
                }
                case 51:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GetPolicyNode>(localPosition, true, true));
                    return true;
                }
                case 52:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SetCultureNode>(localPosition, true, true));
                    return true;
                }
                case 53:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<AddSpouseNode>(localPosition, true, true));
                    return true;
                }
                case 54:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<RemoveSpousesNode>(localPosition, true, true));
                    return true;
                }
                case 55:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ObjectiveNode>(localPosition, true, true));
                    return true;
                }
                case 57:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<GlobalMessageNode>(localPosition, true, true));
                    return true;
                }
                case 58:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<HasHeirNode>(localPosition, true, true));
                    return true;
                }
                case 59:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SignalNode>(localPosition, true, true));
                    return true;
                }

                case 103:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<BreakRepeaterNode>(localPosition, true, true));
                    return true;
                }
                case 104:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<BreakSequencerNode>(localPosition, true, true));
                    return true;
                }
                case 199:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ChoiceDataNode>(localPosition, true, true));
                    return true;
                }
                case 999:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<NewFlowNode>(localPosition, true, true));
                    return true;
                }                
                case 998:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<BackgroundWorkerNode>(localPosition, true, true));
                    return true;
                }
                case 987:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SchemeStateNode>(localPosition, true, true));
                    return true;
                }
                case 933:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SchemeIsActiveNode>(localPosition, true, true));
                    return true;
                }
                case 9494:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SetConspiratorNode>(localPosition, true, true));
                    return true;
                }
                case 9595:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SetTargetNode>(localPosition, true, true));
                    return true;
                }
                case 2153:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<OnLoadNode>(localPosition, true, true));
                    return true;
                }                
                case 5464:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SetInheritorNode>(localPosition, true, true));
                    return true;
                }
                case 9999:
                {
                    if (ieGraphView.graphElements.Any(n => n is ValidatorNode))
                    {
                        NDebug.Log("Multiple validators cannot be created.", NLogType.Error);
                        return false;
                    }

                    ieGraphView.AddElement(ieGraphView.CreateNode<ValidatorNode>(localPosition, true, true));
                    return true;
                }
                case 1122: {
                    ieGraphView.AddElement(ieGraphView.CreateNode<SoundClassNode>(localPosition, true, true));
                    return true;
                }
                case 1123: {
                    ieGraphView.AddElement(ieGraphView.CreateNode<VoiceClassNode>(localPosition, true, true));
                    return true;
                }
                case 1124: {
                    ieGraphView.AddElement(ieGraphView.CreateNode<KeyHandlerNode>(localPosition, true, true));
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