using UnityEngine;
using CollegeChronicles.Dialogue;
using CollegeChronicles.Navigation;
using CollegeChronicles.Telemetry;

namespace CollegeChronicles.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Core Systems")]
        public DialogueManager dialogueManager;
        public PlayerStatsManager playerStatsManager;
        public NavigationManager navigationManager;
        public TelemetryManager telemetryManager;
        
        private void Awake()
        {
            InitializeServices();
        }
        
        private void Start()
        {
            StartGame();
        }
        
        private void InitializeServices()
        {
            if (ServiceLocator.Instance == null)
            {
                var serviceLocatorGO = new GameObject("ServiceLocator");
                serviceLocatorGO.AddComponent<ServiceLocator>();
            }
            
            // Register core services
            if (playerStatsManager != null)
                ServiceLocator.Instance.RegisterService(playerStatsManager);
                
            if (dialogueManager != null)
                ServiceLocator.Instance.RegisterService(dialogueManager);
                
            if (navigationManager != null)
                ServiceLocator.Instance.RegisterService(navigationManager);
                
            if (telemetryManager != null)
                ServiceLocator.Instance.RegisterService(telemetryManager);
        }
        
        private void StartGame()
        {
            // Initialize game session
            var sessionId = System.Guid.NewGuid().ToString();
            var device = SystemInfo.deviceType.ToString();
            var version = Application.version;
            
            if (telemetryManager != null)
            {
                telemetryManager.LogSessionStart(sessionId, device, version);
            }
            
            Debug.Log("College Chronicles - Game Started");
        }
    }
}