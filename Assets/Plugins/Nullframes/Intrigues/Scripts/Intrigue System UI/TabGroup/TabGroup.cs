using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nullframes.Intrigues.UI
{
    public class TabGroup : MonoBehaviour
    {
        public List<TabButton> tabButtons;
        public Sprite tabIdle;
        public Sprite tabHover;
        public Sprite tabActive;
        public TabButton selectedTab;
        public List<GameObject> objectsToSwap;

        public void Subscribe(TabButton button)
        {
            tabButtons ??= new List<TabButton>();
            tabButtons.Add(button);
        }

        public void OnTabEnter(TabButton button)
        {
            ResetTabs();
            if (selectedTab == null || button != selectedTab) button.background.sprite = tabHover;
        }

        public void OnTabExit(TabButton button)
        {
            ResetTabs();
        }

        public void OnTabSelected(TabButton button)
        {
            if (selectedTab != null && button != selectedTab) selectedTab.Deselect();

            if (selectedTab != button)
            {
                button.Select();
            }
            selectedTab = button;

            ResetTabs();
            button.background.sprite = tabActive;

            var index = button.transform.GetSiblingIndex();
            for (var i = 0; i < objectsToSwap.Count; i++)
            {
                if (i == index)
                {
                    objectsToSwap[i].SetActive(true);
                    continue;
                }

                objectsToSwap[i].SetActive(false);
            }
        }
        
        public void OnTabSelectedWithoutNotify(TabButton button)
        {
            selectedTab = button;

            ResetTabs();
            button.background.sprite = tabActive;

            var index = button.transform.GetSiblingIndex();
            for (var i = 0; i < objectsToSwap.Count; i++)
            {
                if (i == index)
                {
                    objectsToSwap[i].SetActive(true);
                    continue;
                }

                objectsToSwap[i].SetActive(false);
            }
        }

        private void ResetTabs()
        {
            foreach (var button in tabButtons)
            {
                if (selectedTab != null && button == selectedTab) continue;
                button.background.sprite = tabIdle;
            }
        }
    }
}