using System.Collections.Generic;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class FamilySearchMenu : ScriptableObject, ISearchWindowProvider
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
                new(new GUIContent("New Family", spaceIcon))
                {
                    level = 1, userData = 0
                },
                // new(new GUIContent("Add Member", spaceIcon))
                // {
                //     level = 1, userData = 1
                // },
            };

            return searchTreeEntries;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            if (!ieGraphView.graphElements.ToList().Exists(e => e is FamilyGroup) &&
                SearchTreeEntry.userData is not 0)
            {
                NDebug.Log("You need a Family Group to create a Node.", NLogType.Error);
                return false;
            }

            var localPosition = ieGraphView.GetLocalMousePosition(context.screenMousePosition, true);
            switch (SearchTreeEntry.userData)
            {
                case 0:
                {
                    ieGraphView.CreateFamilyGroup(localPosition);
                    return true;
                }
                case 1:
                {
                    ieGraphView.AddElement(ieGraphView.CreateNode<FamilyMemberNode>(localPosition, true, true));
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