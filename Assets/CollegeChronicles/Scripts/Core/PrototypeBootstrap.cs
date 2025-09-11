using UnityEngine;
using UnityEngine.SceneManagement;
using CollegeChronicles.Core;

namespace CollegeChronicles.Core
{
    /// <summary>
    /// Bootstrap script to ensure proper initialization order and handle prototype-specific setup
    /// </summary>
    public class PrototypeBootstrap : MonoBehaviour
    {
        [Header("Prototype Configuration")]
        public bool autoStartIntroduction = true;
        public bool enableDebugMode = true;
        public bool skipMainMenu = false;
        
        [Header("Scene References")]
        public string mainMenuScene = "MainMenu";
        public string introductionScene = "DormRoom";
        
        private void Awake()
        {
            // Ensure this runs only once
            var existingBootstrap = FindObjectOfType<PrototypeBootstrap>();
            if (existingBootstrap != null && existingBootstrap != this)
            {
                Destroy(gameObject);
                return;
            }
            
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("College Chronicles - Prototype Bootstrap Started");
            InitializePrototype();
        }
        
        private void InitializePrototype()
        {
            // Set up application-wide settings
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            
            // Configure DOTween if available
            try
            {
                DG.Tweening.DOTween.Init(true, true, DG.Tweening.LogBehaviour.ErrorsOnly);
                Debug.Log("DOTween initialized successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"DOTween initialization failed: {e.Message}");
            }
            
            // Handle prototype flow
            if (skipMainMenu && !string.IsNullOrEmpty(introductionScene))
            {
                LoadIntroductionScene();
            }
            else
            {
                LoadMainMenuScene();
            }
        }
        
        private void LoadMainMenuScene()
        {
            var currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != mainMenuScene)
            {
                Debug.Log($"Loading Main Menu Scene: {mainMenuScene}");
                SceneManager.LoadScene(mainMenuScene);
            }
        }
        
        private void LoadIntroductionScene()
        {
            Debug.Log($"Skipping to Introduction Scene: {introductionScene}");
            SceneManager.LoadScene(introductionScene);
        }
        
        private void Start()
        {
            // Check if we're in the introduction scene and should auto-start
            if (autoStartIntroduction && SceneManager.GetActiveScene().name == introductionScene)
            {
                var dialogueUI = FindObjectOfType<UI.DialogueUI_Controller>();
                if (dialogueUI != null)
                {
                    // Small delay to ensure all systems are initialized
                    Invoke(nameof(StartIntroductionDialogue), 1f);
                }
            }
        }
        
        private void StartIntroductionDialogue()
        {
            var dialogueUI = FindObjectOfType<UI.DialogueUI_Controller>();
            if (dialogueUI != null)
            {
                dialogueUI.StartDialogue("DormRoom");
                Debug.Log("Auto-started introduction dialogue");
            }
        }
        
        private void Update()
        {
            // Debug shortcuts
            if (!enableDebugMode) return;
            
            if (Input.GetKeyDown(KeyCode.F1))
            {
                DebugReloadCurrentScene();
            }
            
            if (Input.GetKeyDown(KeyCode.F2))
            {
                DebugGoToMainMenu();
            }
            
            if (Input.GetKeyDown(KeyCode.F3))
            {
                DebugGoToDormRoom();
            }
            
            if (Input.GetKeyDown(KeyCode.F4))
            {
                DebugGoToCampusQuad();
            }
            
            if (Input.GetKeyDown(KeyCode.F5))
            {
                DebugStartDialogue();
            }
        }
        
        private void DebugReloadCurrentScene()
        {
            var currentScene = SceneManager.GetActiveScene().name;
            Debug.Log($"[DEBUG] Reloading scene: {currentScene}");
            SceneManager.LoadScene(currentScene);
        }
        
        private void DebugGoToMainMenu()
        {
            Debug.Log("[DEBUG] Going to Main Menu");
            SceneManager.LoadScene(mainMenuScene);
        }
        
        private void DebugGoToDormRoom()
        {
            Debug.Log("[DEBUG] Going to Dorm Room");
            SceneManager.LoadScene("DormRoom");
        }
        
        private void DebugGoToCampusQuad()
        {
            Debug.Log("[DEBUG] Going to Campus Quad");
            SceneManager.LoadScene("CampusQuad");
        }
        
        private void DebugStartDialogue()
        {
            var dialogueUI = FindObjectOfType<UI.DialogueUI_Controller>();
            if (dialogueUI != null)
            {
                var currentScene = SceneManager.GetActiveScene().name;
                dialogueUI.StartDialogue(currentScene);
                Debug.Log($"[DEBUG] Started dialogue for scene: {currentScene}");
            }
        }
        
        private void OnGUI()
        {
            if (!enableDebugMode) return;
            
            // Debug overlay
            GUI.Box(new Rect(10, 10, 300, 150), "College Chronicles Debug");
            GUI.Label(new Rect(20, 30, 280, 20), "F1: Reload Scene");
            GUI.Label(new Rect(20, 50, 280, 20), "F2: Main Menu");
            GUI.Label(new Rect(20, 70, 280, 20), "F3: Dorm Room");
            GUI.Label(new Rect(20, 90, 280, 20), "F4: Campus Quad");
            GUI.Label(new Rect(20, 110, 280, 20), "F5: Start Dialogue");
            
            var currentScene = SceneManager.GetActiveScene().name;
            GUI.Label(new Rect(20, 130, 280, 20), $"Scene: {currentScene}");
        }
    }
}