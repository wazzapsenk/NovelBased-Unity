using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using CollegeChronicles.Core;
using CollegeChronicles.Telemetry;

namespace CollegeChronicles.Navigation
{
    public class NavigationManager : MonoBehaviour
    {
        [Header("Scene Configuration")]
        public string dormRoomScene = "DormRoom";
        public string campusQuadScene = "CampusQuad";
        public string mainMenuScene = "MainMenu";
        
        [Header("Loading Settings")]
        public GameObject loadingScreen;
        public float minLoadingTime = 1f;
        
        public System.Action<string> OnSceneLoadStarted;
        public System.Action<string> OnSceneLoadCompleted;
        public System.Action OnFreeRoamStarted;
        public System.Action OnFreeRoamEnded;
        
        private string _currentScene;
        private string _previousScene;
        private bool _isFreeRoamActive = false;
        private TelemetryManager _telemetryManager;
        
        public string CurrentScene => _currentScene;
        public string PreviousScene => _previousScene;
        public bool IsFreeRoamActive => _isFreeRoamActive;
        public bool IsLoading { get; private set; } = false;
        
        private void Awake()
        {
            // Register with ServiceLocator
            if (ServiceLocator.Instance != null)
            {
                ServiceLocator.Instance.RegisterService(this);
            }
            
            _currentScene = SceneManager.GetActiveScene().name;
        }
        
        private void Start()
        {
            _telemetryManager = ServiceLocator.Instance?.GetService<TelemetryManager>();
            
            if (loadingScreen != null)
                loadingScreen.SetActive(false);
        }
        
        public void LoadScene(string sceneName, bool useFadeTransition = true)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"NavigationManager: Already loading a scene, ignoring request for '{sceneName}'");
                return;
            }
            
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("NavigationManager: Scene name cannot be null or empty");
                return;
            }
            
            StartCoroutine(LoadSceneAsync(sceneName, useFadeTransition));
        }
        
        private IEnumerator LoadSceneAsync(string sceneName, bool useFadeTransition)
        {
            IsLoading = true;
            _previousScene = _currentScene;
            
            Debug.Log($"NavigationManager: Loading scene '{sceneName}' from '{_previousScene}'");
            
            // Log telemetry
            _telemetryManager?.LogSceneTransition(_previousScene, sceneName);
            
            // Notify listeners
            OnSceneLoadStarted?.Invoke(sceneName);
            
            // Show loading screen
            if (loadingScreen != null && useFadeTransition)
            {
                loadingScreen.SetActive(true);
            }
            
            // Start loading the scene
            var asyncOperation = SceneManager.LoadSceneAsync(sceneName);
            asyncOperation.allowSceneActivation = false;
            
            float startTime = Time.time;
            
            // Wait for loading to complete
            while (!asyncOperation.isDone)
            {
                if (asyncOperation.progress >= 0.9f)
                {
                    // Ensure minimum loading time for smooth transition
                    float elapsedTime = Time.time - startTime;
                    if (elapsedTime >= minLoadingTime)
                    {
                        asyncOperation.allowSceneActivation = true;
                    }
                }
                
                yield return null;
            }
            
            // Update current scene
            _currentScene = sceneName;
            
            // Hide loading screen
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(false);
            }
            
            IsLoading = false;
            
            Debug.Log($"NavigationManager: Scene '{sceneName}' loaded successfully");
            
            // Notify listeners
            OnSceneLoadCompleted?.Invoke(sceneName);
        }
        
        public void GoToDormRoom()
        {
            LoadScene(dormRoomScene);
        }
        
        public void GoToCampusQuad()
        {
            LoadScene(campusQuadScene);
        }
        
        public void GoToMainMenu()
        {
            LoadScene(mainMenuScene);
        }
        
        public void StartFreeRoam()
        {
            if (_isFreeRoamActive)
            {
                Debug.LogWarning("NavigationManager: Free roam is already active");
                return;
            }
            
            _isFreeRoamActive = true;
            Debug.Log("NavigationManager: Free roam started");
            
            OnFreeRoamStarted?.Invoke();
        }
        
        public void EndFreeRoam()
        {
            if (!_isFreeRoamActive)
            {
                Debug.LogWarning("NavigationManager: Free roam is not active");
                return;
            }
            
            _isFreeRoamActive = false;
            Debug.Log("NavigationManager: Free roam ended");
            
            OnFreeRoamEnded?.Invoke();
        }
        
        public bool CanNavigateTo(string sceneName)
        {
            if (IsLoading) return false;
            if (string.IsNullOrEmpty(sceneName)) return false;
            if (sceneName == _currentScene) return false;
            
            // Add any additional navigation rules here
            return true;
        }
        
        public void RestartCurrentScene()
        {
            LoadScene(_currentScene);
        }
        
        public void GoBackToPreviousScene()
        {
            if (!string.IsNullOrEmpty(_previousScene))
            {
                LoadScene(_previousScene);
            }
            else
            {
                Debug.LogWarning("NavigationManager: No previous scene to return to");
            }
        }
        
        // Debug utilities
        [ContextMenu("Go To Dorm Room")]
        private void Debug_GoToDormRoom()
        {
            GoToDormRoom();
        }
        
        [ContextMenu("Go To Campus Quad")]
        private void Debug_GoToCampusQuad()
        {
            GoToCampusQuad();
        }
        
        [ContextMenu("Start Free Roam")]
        private void Debug_StartFreeRoam()
        {
            StartFreeRoam();
        }
        
        [ContextMenu("End Free Roam")]
        private void Debug_EndFreeRoam()
        {
            EndFreeRoam();
        }
    }
}