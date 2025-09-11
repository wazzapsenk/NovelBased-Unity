using System;
using Nullframes.Intrigues.Graph;
using UnityEngine;

namespace Nullframes.Intrigues {
    [Serializable]
    public class Scheme {
        /// <summary>
        /// Gets the unique ID for the scheme.
        /// </summary>
        [field: SerializeField]
        public string ID { get; private set; }

        /// <summary>
        /// Gets the name of the scheme, e.g., "Assassination."
        /// </summary>
        [field: SerializeField]
        public string SchemeName { get; private set; }

        /// <summary>
        /// Gets the icon representing the scheme, e.g., an icon of a stabbing from behind.
        /// </summary>
        [field: SerializeField]
        public Sprite Icon { get; private set; }

        [SerializeField] private string _description;

        /// <summary>
        /// Gets the description of the scheme, e.g., "Plan to eliminate the target. If it happens, {t*Name} will be eliminated."
        /// </summary>
        public string Description {
            get => Schemer != null ? _description.SchemeFormat(Schemer.Conspirator, Schemer.Target) : _description; private set => _description = value; }

        /// <summary>
        /// Gets or sets the current objective of the scheme.
        /// </summary>
        [field: SerializeField]
        public string CurrentObjective { get; private set; }

        /// <summary>
        /// Gets the Rule ID that the scheme follows.
        /// </summary>
        [field: SerializeField]
        public string RuleID { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the scheme should be hidden if it is not compatible. Generally used for UI purposes.
        /// </summary>
        [field: SerializeField]
        public bool HideIfNotCompatible { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a target is not required for starting the scheme.
        /// </summary>
        [field: SerializeField]
        public bool TargetNotRequired { get; private set; }        
        
        /// <summary>
        /// Gets a value indicating whether a target is not required for starting the scheme.
        /// </summary>
        [field: SerializeField]
        public bool HideOnUI { get; private set; }

        [field: NonSerialized] private Schemer schemer { get; set; }

        /// <summary>
        /// Gets or sets the Schemer object that operates the scheme in the background.
        /// </summary>
        public Schemer Schemer { get => schemer; private set => schemer = value; }
        
        /// <summary>
        /// Checks if Schemer is enabled. Returns True if enabled.
        /// </summary>
        public bool IsValid => Schemer != null;

        private bool Duplicated { get; set; }

        //Constructor
        public Scheme(string id, string schemeName, string description, string ruleID, Sprite icon,
            bool hideIfNotCompatible, bool targetNotRequired, bool hideOnUI) {
            ID = id;
            SchemeName = schemeName;
            Description = description;
            RuleID = ruleID;
            Icon = icon;
            HideIfNotCompatible = hideIfNotCompatible;
            TargetNotRequired = targetNotRequired;
            HideOnUI = hideOnUI;
        }

        //Copy
        internal Scheme Duplicate() {
            return new Scheme(ID, SchemeName, Description, RuleID, Icon, HideIfNotCompatible, TargetNotRequired, HideOnUI)
            {
                Duplicated = true
            };
        }

        private void Scheme_Init(Schemer _schemer) => Schemer = _schemer;
        private void Scheme_SetObjective(string objective) => CurrentObjective = objective;

        public void SetDescription(string description) => Description = description;

        public RuleResult IsCompatible(Actor conspirator, Actor target) {
            if (TargetNotRequired && target != null) {
                return new RuleResult(RuleState.Failed);
            }

            if (target != null || TargetNotRequired) return Ruler.StartGraph(RuleID, conspirator, target);
            return new RuleResult(RuleState.Failed);
        }
    }
}