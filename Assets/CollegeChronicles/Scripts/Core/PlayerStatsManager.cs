using UnityEngine;
using CollegeChronicles.Data;

namespace CollegeChronicles.Core
{
    public class PlayerStatsManager : MonoBehaviour
    {
        [Header("Current Stats")]
        [SerializeField] private int _alphaScore = 0;
        [SerializeField] private int _betaScore = 0;
        
        [Header("Debug")]
        public bool debugMode = true;
        
        public System.Action<int, int> OnStatsChanged;
        public System.Action<PersonalityImpact> OnPersonalityImpacted;
        
        public int AlphaScore => _alphaScore;
        public int BetaScore => _betaScore;
        
        private void Start()
        {
            // Ensure this manager is registered with ServiceLocator
            if (ServiceLocator.Instance != null)
            {
                ServiceLocator.Instance.RegisterService(this);
            }
            
            // Notify initial stats
            OnStatsChanged?.Invoke(_alphaScore, _betaScore);
        }
        
        public void ApplyPersonalityImpact(PersonalityImpact impact)
        {
            if (!impact.HasImpact) return;
            
            var oldAlpha = _alphaScore;
            var oldBeta = _betaScore;
            
            _alphaScore = Mathf.Max(0, _alphaScore + impact.alphaPointChange);
            _betaScore = Mathf.Max(0, _betaScore + impact.betaPointChange);
            
            if (debugMode)
            {
                Debug.Log($"Personality Impact Applied: {impact} | New Stats: Alpha {_alphaScore}, Beta {_betaScore}");
            }
            
            // Notify listeners about the impact and new stats
            OnPersonalityImpacted?.Invoke(impact);
            OnStatsChanged?.Invoke(_alphaScore, _betaScore);
        }
        
        public void SetStats(int alpha, int beta)
        {
            _alphaScore = Mathf.Max(0, alpha);
            _betaScore = Mathf.Max(0, beta);
            
            OnStatsChanged?.Invoke(_alphaScore, _betaScore);
        }
        
        public void ResetStats()
        {
            SetStats(0, 0);
        }
        
        public string GetStatsString()
        {
            return $"Alpha: {_alphaScore} | Beta: {_betaScore}";
        }
        
        public bool MeetsRequirements(int requiredAlpha, int requiredBeta)
        {
            return _alphaScore >= requiredAlpha && _betaScore >= requiredBeta;
        }
        
        // For save/load system in future
        [System.Serializable]
        public class PlayerStatsData
        {
            public int alphaScore;
            public int betaScore;
            
            public PlayerStatsData(int alpha, int beta)
            {
                alphaScore = alpha;
                betaScore = beta;
            }
        }
        
        public PlayerStatsData GetSaveData()
        {
            return new PlayerStatsData(_alphaScore, _betaScore);
        }
        
        public void LoadFromSaveData(PlayerStatsData data)
        {
            SetStats(data.alphaScore, data.betaScore);
        }
    }
}