using UnityEngine;

namespace CollegeChronicles.Data
{
    [System.Serializable]
    public struct PersonalityImpact
    {
        [Header("Personality Score Changes")]
        public int alphaPointChange;
        public int betaPointChange;
        
        public PersonalityImpact(int alphaChange, int betaChange)
        {
            alphaPointChange = alphaChange;
            betaPointChange = betaChange;
        }
        
        public static PersonalityImpact Alpha(int points) => new PersonalityImpact(points, 0);
        public static PersonalityImpact Beta(int points) => new PersonalityImpact(0, points);
        public static PersonalityImpact Neutral => new PersonalityImpact(0, 0);
        public static PersonalityImpact Mixed(int alpha, int beta) => new PersonalityImpact(alpha, beta);
        
        public bool HasImpact => alphaPointChange != 0 || betaPointChange != 0;
        
        public override string ToString()
        {
            if (alphaPointChange > 0 && betaPointChange == 0)
                return $"+{alphaPointChange} Alpha";
            else if (betaPointChange > 0 && alphaPointChange == 0)
                return $"+{betaPointChange} Beta";
            else if (alphaPointChange != 0 && betaPointChange != 0)
                return $"+{alphaPointChange} Alpha, +{betaPointChange} Beta";
            else
                return "No Impact";
        }
    }
}