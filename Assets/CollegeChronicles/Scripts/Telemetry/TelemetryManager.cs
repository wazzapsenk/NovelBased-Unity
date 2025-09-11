using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using CollegeChronicles.Core;

namespace CollegeChronicles.Telemetry
{
    public class TelemetryManager : MonoBehaviour
    {
        [Header("Settings")]
        public bool enableTelemetry = true;
        public bool enableDebugLogging = true;
        
        [Header("Session Data")]
        [SerializeField] private string currentSessionId;
        
        private List<TelemetryEvent> _sessionEvents = new List<TelemetryEvent>();
        private string _telemetryFilePath;
        
        private void Awake()
        {
            _telemetryFilePath = Path.Combine(Application.persistentDataPath, "telemetry_data.json");
            
            // Register with ServiceLocator
            if (ServiceLocator.Instance != null)
            {
                ServiceLocator.Instance.RegisterService(this);
            }
        }
        
        public void LogSessionStart(string sessionId, string device, string version)
        {
            if (!enableTelemetry) return;
            
            currentSessionId = sessionId;
            
            var parameters = new Dictionary<string, object>
            {
                { "session_id", sessionId },
                { "device", device },
                { "version", version }
            };
            
            LogEvent("session_start", parameters);
            
            if (enableDebugLogging)
            {
                Debug.Log($"Session Started: {sessionId} on {device} (Version: {version})");
            }
        }
        
        public void LogChoiceMade(string sceneId, string choiceId, int alphaImpact, int betaImpact)
        {
            if (!enableTelemetry) return;
            
            var parameters = new Dictionary<string, object>
            {
                { "scene_id", sceneId },
                { "choice_id", choiceId },
                { "alpha_impact", alphaImpact },
                { "beta_impact", betaImpact },
                { "session_id", currentSessionId }
            };
            
            LogEvent("choice_made", parameters);
            
            if (enableDebugLogging)
            {
                Debug.Log($"Choice Made: {choiceId} in {sceneId} (Alpha: {alphaImpact}, Beta: {betaImpact})");
            }
        }
        
        public void LogSceneTransition(string fromScene, string toScene)
        {
            if (!enableTelemetry) return;
            
            var parameters = new Dictionary<string, object>
            {
                { "from_scene", fromScene },
                { "to_scene", toScene },
                { "session_id", currentSessionId }
            };
            
            LogEvent("scene_transition", parameters);
        }
        
        public void LogMinigameResult(string minigameId, bool success, float accuracy, bool wasSkipped)
        {
            if (!enableTelemetry) return;
            
            var parameters = new Dictionary<string, object>
            {
                { "minigame_id", minigameId },
                { "success", success },
                { "accuracy", accuracy },
                { "was_skipped", wasSkipped },
                { "session_id", currentSessionId }
            };
            
            LogEvent("minigame_result", parameters);
        }
        
        private void LogEvent(string eventType, Dictionary<string, object> parameters)
        {
            var telemetryEvent = new TelemetryEvent
            {
                eventType = eventType,
                timestamp = System.DateTime.UtcNow,
                parameters = parameters
            };
            
            _sessionEvents.Add(telemetryEvent);
            
            // Auto-save events periodically
            if (_sessionEvents.Count % 5 == 0)
            {
                SaveTelemetryData();
            }
        }
        
        public void SaveTelemetryData()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_sessionEvents, Formatting.Indented);
                File.WriteAllText(_telemetryFilePath, json);
                
                if (enableDebugLogging)
                {
                    Debug.Log($"Telemetry data saved to: {_telemetryFilePath}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save telemetry data: {e.Message}");
            }
        }
        
        public List<TelemetryEvent> GetSessionEvents()
        {
            return new List<TelemetryEvent>(_sessionEvents);
        }
        
        public void ClearSessionData()
        {
            _sessionEvents.Clear();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveTelemetryData();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SaveTelemetryData();
            }
        }
        
        private void OnDestroy()
        {
            SaveTelemetryData();
        }
    }
    
    [System.Serializable]
    public class TelemetryEvent
    {
        public string eventType;
        public System.DateTime timestamp;
        public Dictionary<string, object> parameters;
    }
}