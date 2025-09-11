using Nullframes.Intrigues.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nullframes.Intrigues.Demo
{
    public class SaveSlot : MonoBehaviour
    {
        public TextMeshProUGUI date;

        [HideInInspector] public string saveName;

        public void Load()
        {
            IntrigueSaveSystem.Instance.Load(saveName);
            SceneManager.LoadScene(1);
        }
    }
}