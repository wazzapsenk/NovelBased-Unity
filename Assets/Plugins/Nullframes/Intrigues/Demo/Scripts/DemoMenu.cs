using System.Globalization;
using Nullframes.Intrigues.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nullframes.Intrigues.Demo
{
    public class DemoMenu : MonoBehaviour
    {
        public Material blurMat;
        public float blur;
        private bool accessShortcuts;

        public GameObject continueButton;
        public GameObject loadButton;

        public GameObject slotContent;
        public GameObject slot;
        private static readonly int Radius = Shader.PropertyToID("_radius");

        private void Start()
        {
            NullUtils.DelayedCall(new DelayedCallParams {
                Delay = 4f,
                Call = () => accessShortcuts = true,
            });

            if (!IntrigueSaveSystem.Instance.AnySaveFileExists)
            {
                continueButton.SetActive(false);
                loadButton.SetActive(false);
            }
        }

        private void Update()
        {
            blurMat.SetFloat(Radius, blur);

            if (!accessShortcuts)
            {
#if !UNITY_EDITOR
                return;
#endif
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Quit();
            }
        }

        public void NewGame()
        {
            IntrigueSaveSystem.Instance.NewGame();
            SceneManager.LoadScene(1);
        }

        public void Quit()
        {
            Application.Quit();
        }

        public void Continue()
        {
            IntrigueSaveSystem.Instance.LoadLatest();
            SceneManager.LoadScene(1);
        }

        public void LoadSlots()
        {
            foreach (Transform child in slotContent.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (var saveFileInfo in IntrigueSaveSystem.Instance.GET_SAVE_FILES)
            {
                var saveSlot = Instantiate(slot, slotContent.transform).GetComponent<SaveSlot>();
                saveSlot.saveName = saveFileInfo.Name;
                saveSlot.date.text =
                    $"{saveFileInfo.FileInfo.LastWriteTime.ToString("yyyy MMM dd", CultureInfo.InvariantCulture)}\n{saveFileInfo.FileInfo.LastWriteTime.ToString("HH:mm", CultureInfo.InvariantCulture)}";
            }
        }

        public void StorePage()
        {
            Application.OpenURL("https://u3d.as/2TCR");
        }
    }
}