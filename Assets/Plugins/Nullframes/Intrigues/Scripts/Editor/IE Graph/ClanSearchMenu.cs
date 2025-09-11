using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class ClanSearchMenu : ScriptableObject, ISearchWindowProvider
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
                new(new GUIContent("New Clan", spaceIcon))
                {
                    level = 1, userData = 0
                },
                // new(new GUIContent("Add Member", spaceIcon))
                // {
                //     level = 1, userData = 1
                // },
                new(new GUIContent("New Policy", spaceIcon))
                {
                    level = 1, userData = 2
                },
                new(new GUIContent("New Role", spaceIcon))
                {
                    level = 1, userData = 3
                },                
                new(new GUIContent("Heir Filter", spaceIcon))
                {
                    level = 1, userData = 4
                },
            };

            return searchTreeEntries;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            var localPosition = ieGraphView.GetLocalMousePosition(context.screenMousePosition, true);
            switch (SearchTreeEntry.userData)
            {
                case 0:
                {
                    ieGraphView.CreateClanGroup(localPosition);
                    return true;
                }
                case 1:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<ClanMemberNode>(localPosition, true, true));
                    return true;
                }
                case 2:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<PolicyNode>(localPosition, true, true));
                    return true;
                }
                case 3:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<RoleNode>(localPosition, true, true));
                    return true;
                }                
                case 4:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<HeirFilterNode>(localPosition, true, true));
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