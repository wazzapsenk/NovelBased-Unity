using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nullframes.Intrigues.Demo {
    public class DemoCharacter : MonoBehaviour {
        // Reference to the Actor component this script is associated with.
        private Actor actor;

        // Range for calculating base income; actors will earn a random amount within this range.
        public int baseIncomeMin = 15; // Minimum amount of base income an actor can earn.
        public int baseIncomeMax = 50; // Maximum amount of base income an actor can earn.

        [Space(10)] // Adds a spacer in the Unity inspector to visually separate the following fields.
        public NInt currentCoin; // The current amount of coins the actor possesses.
        public NInt currentPoison; // The current amount of poison units the actor has, potentially for gameplay mechanics.
        public NInt currentBomb; // The current amount of mist bombs the actor holds.
        public NInt currentSpy; // The current number of spies at the actor's disposal.
        public NInt currentPower; // Represents the actor's current scheme power or influence level.
        public NFloat currentChance; // A floating-point number representing the actor's chance of success in certain actions.

        // Timer IDs for scheduling various life event checks and updates. These could be used with a timer management system to execute actions at specific intervals.
        private string olderTimerId; // Timer for aging the actor over time.
        private string deathChanceId; // Timer for checking the actor's chance of death as they age.
        private string makeChildTimerId; // Timer for determining opportunities for the actor to have children.
        private string incomeTimerId; // Timer for regularly updating the actor's income.

        private string marriageTimerId; // Timer for checking marriage opportunities for the actor.
        private string revoltTimerId; // Timer for opportunities where the actor may start or be involved in a revolt.


        // Initialize references and subscribe to events at the start of the script.
        private void Start() {
            // Retrieves the Actor component attached to the same GameObject.
            actor = GetComponent<Actor>();
            // Retrieves various variables associated with the actor, presumably for gameplay mechanics.
            currentCoin = actor.GetVariable<NInt>("Coin");
            currentPoison = actor.GetVariable<NInt>("Poison");
            currentBomb = actor.GetVariable<NInt>("MistBomb");
            currentSpy = actor.GetVariable<NInt>("Spy");
            currentPower = actor.GetVariable<NInt>("SchemePower");
            currentChance = actor.GetVariable<NFloat>("Chance");

            // Subscribes to specific events triggered by the actor, allowing for reactive gameplay changes.
            actor.onActorIsMarried += OnActorMarried;
            actor.onInherited += OnInherited;
            actor.onRoleChanged += OnRoleChanged;

            // Sets a flag if the actor is older than 17, possibly affecting portrait display or other age-related features.
            if (actor.Age > 17) {
                actor.SetVariable("PortraitIsSet", true);
            }

            // Schedules various gameplay mechanics to occur at intervals if the actor is in an active state.
            if (actor.State == Actor.IState.Active) {
                // Age the actor over time.
                NullUtils.DelayedCall(new DelayedCallParams {
                    DelayName = olderTimerId,
                    Delay = Random.Range(60f, 80f),
                    Call = Older,
                    LoopCount = -1
                });
                // Check for the actor's death based on certain conditions.
                NullUtils.DelayedCall(new DelayedCallParams {
                    DelayName = deathChanceId,
                    Delay = Random.Range(100f, 200f),
                    Call = DeathChance,
                    LoopCount = -1
                });
                // Calculate and apply income periodically.
                NullUtils.DelayedCall(new DelayedCallParams {
                    DelayName = incomeTimerId,
                    Delay = 30f,
                    Call = Income,
                    LoopCount = -1
                });
                
                // Schedule events related to family dynamics and social status.
                NullUtils.DelayedCall(new DelayedCallParams {
                    DelayName = makeChildTimerId,
                    Delay = Random.Range(60f, 200f),
                    Call = MakeChildTimer,
                    LoopCount = -1
                });
                NullUtils.DelayedCall(new DelayedCallParams {
                    DelayName = marriageTimerId,
                    Delay = Random.Range(60f, 200f),
                    Call = MarriageTimer,
                    LoopCount = -1
                });
                NullUtils.DelayedCall(new DelayedCallParams {
                    DelayName = revoltTimerId,
                    Delay = Random.Range(60f, 200f),
                    Call = RevoltTimer,
                    LoopCount = -1
                });
            }

            // Subscribe to a global event related to actor death, allowing the actor to respond to this event.
            IM.onActorDeath += OnActorDeath;
        }

        // Update is called once per frame to dynamically adjust the actor's chance value based on their scheme power.
        private void Update() {
            // Dynamically updates the 'Chance' variable based on the 'SchemePower' variable times a multiplier.
            // This could influence gameplay mechanics like success rates of certain actions.
            currentChance.Value = currentPower.Value * 2.6f;
            
            if(actor.Parents().Count() == 1) 
                Debug.Log(actor.FullName);
        }


        // Called when the actor's role within the game changes.
        private void OnRoleChanged(Role oldRole) {
            Role newRole = actor.Role;

            // Handle the case where the actor no longer holds any role.
            if (newRole == null) {
                NotificationSystem.ShowNotification(
                    $"<color=#9BFF78>{oldRole.Title(actor.Gender)} <link=\"{actor.ID}\">{actor.FullName}</link></color> is no longer a <color=#9BFF78>{actor.Title}</color>.");
                return;
            }

            // Announce a promotion if the new role has a higher (or equal) priority than the old role.
            if (oldRole == null || newRole.Priority >= oldRole.Priority) {
                NotificationSystem.ShowNotification(
                    $"<color=#9BFF78><link=\"{actor.ID}\">{actor.FullName}</link></color> was promoted to the position of <color=#9BFF78>{actor.Title}</color>.");

                // Trigger a "Promoted" scheme for the player character.
                if (IM.IsPlayer(actor)) {
                    NullUtils.DelayedCall(new DelayedCallParams {
                        Delay = 4f,
                        Call = () => actor.StartScheme("Promoted"),
                    });
                }
            }
            // Announce a demotion if the new role has a lower priority than the old role.
            else {
                NotificationSystem.ShowNotification(
                    $"<color=#9BFF78><link=\"{actor.ID}\">{actor.FullName}</link></color> was demoted to the position of <color=#9BFF78>{actor.Title}</color>.");
            }
        }

        // Called when the actor inherits from another actor.
        private void OnInherited(Actor inheritor) {
            // Skip inheritance if both actors are from the same clan.
            if (actor.Clan == inheritor.Clan) return;

            // Transfer coins from the inheritor to the actor.
            NInt inheritorCoins = inheritor.GetVariable<NInt>("Coin");
            actor.SetVariable("Coin", inheritorCoins.Value);
            inheritorCoins.Value = 0;
    
            // Trigger a "Claimed Legacy" scheme for the player character.
            if (IM.IsPlayer(actor)) {
                actor.StartScheme("Claimed Legacy");
            }

            // Notify about the inheritance.
            NotificationSystem.ShowNotification(
                $"<color=#9BFF78><link=\"{actor.ID}\">{actor.FullName}</link></color>, as the heir, claimed <color=#FFFFAD><link=\"{inheritor.ID}\">{inheritor.FullName}'s</link></color> legacy.");
        }


        // Called when the actor gets married.
        private void OnActorMarried(Actor marriedActor, Actor spouse, bool primaryActor) {
            if (primaryActor) {
                // Notify about the marriage.
                NotificationSystem.ShowNotification(
                    $"<color=#9BFF78><link=\"{marriedActor.ID}\">{marriedActor.FullName}</link></color> married <color=#FFFFAD><link=\"{spouse.ID}\">{spouse.FullName}</link></color>.");
            }
        }

        // Cleans up by stopping all timers when the script is destroyed.
        private void OnDestroy() {
            StopTimers();
        }

        // Called when an actor dies.
        private void OnActorDeath(Actor deadCharacter) {
            // Stop timers and handle player-specific logic if the deceased is the player character.
            if (deadCharacter == actor) {
                StopTimers();

                // Assign a new player character either to the deceased's heir or a random family member.
                if (IM.IsPlayer(deadCharacter)) {
                    IM.SetPlayer(deadCharacter.Heir != null ? deadCharacter.Heir : deadCharacter.Family.Members.PickRandom());

                    // Trigger a "Farewell" scheme for the new player character.
                    IM.Player.StartScheme("Farewell", deadCharacter);
                }
            }
        }

        // Stops all ongoing timers related to the actor's life events.
        private void StopTimers() {
            NullUtils.StopCall(olderTimerId);
            NullUtils.StopCall(deathChanceId);
            NullUtils.StopCall(makeChildTimerId);
            NullUtils.StopCall(marriageTimerId);
            NullUtils.StopCall(incomeTimerId);
            NullUtils.StopCall(revoltTimerId);
        }

        #region INCOME

        public void Income() {
            // Checks if the actor benefits from an "Economy" policy, which influences income calculations.
            // This simulates a scenario where economic policies can have a direct impact on personal income.
            bool economyPolicy = actor.HasPolicy("Economy");

            // Determines the base income randomly within a specified range.
            // This introduces variability in income, reflecting real-world scenarios where earnings can fluctuate.
            int baseIncome = Random.Range(baseIncomeMin, baseIncomeMax);

            // Calculates an income bonus based on the presence of the economy policy.
            // Actors under the economy policy receive a flat bonus, enhancing the policy's perceived benefits.
            int incomeBonus = economyPolicy ? 30 : 0;

            // If the actor has a defined role, the role's priority adds to the income bonus.
            // This simulates higher earnings for actors in more prestigious or important roles.
            if (actor.Role != null) {
                incomeBonus += actor.Role.Priority;
            }

            // The final income calculation includes both base income and total bonuses.
            int income = baseIncome + incomeBonus;

            // Adds the calculated income to the actor's current coin balance.
            currentCoin.Value += income;

            // If the actor is the player, displays a notification with the monthly earnings.
            // This provides immediate feedback to the player about the benefits of their economic status and role.
            if (IM.Player == actor) {
                NotificationSystem.ShowNotification(
                    $"Your <color=#9BFF78>earnings</color> for this month <color=#9BFF78>[{income}]</color> Coin");
            }
        }

        #endregion

        #region OLDER

        private void DeathChance() {
            // Determines if the "Health" policy is active for the actor.
            // This policy affects the actor's maximum age and the probability of death.
            bool healthPolicy = actor.HasPolicy("Health");

            // Sets the maximum age threshold. If the health policy is active,
            // the actor has a higher max age due to presumably better health care or conditions.
            float ageThreshold = healthPolicy ? 80f : 65f;

            // Sets the probability of death after reaching max age.
            // The health policy significantly reduces the probability,
            // reflecting the policy's positive impact on longevity.
            float probability = healthPolicy ? 15f : 25f;

            // Checks if the actor's age exceeds the defined maximum age threshold.
            if (actor.Age > ageThreshold) {
                // Determines if the actor dies based on the calculated probability.
                // This is a stochastic process, introducing randomness into the actor's lifespan.
                if (NullUtils.CheckProbability(probability)) {
                    // Changes the actor's state to Passive, effectively simulating their death.
                    actor.SetState(Actor.IState.Passive);
                    // Displays a notification to inform about the actor's death,
                    // highlighting the emotional impact on the actor's close ones.
                    NotificationSystem.ShowNotification(
                        $"<color=#9BFF78><link=\"{actor.ID}\">{actor.FullName}</link></color> has <color=#FF6262>died</color>. His close ones are mourning his loss.");
                }
            }
        }


        public void Older() {
            // Check if the actor is in a Passive state; if so, exit the method early.
            if (actor.State == Actor.IState.Passive) return;

            // Increment the actor's age by one year.
            actor.SetAge(actor.Age + 1);

            // Assign the appropriate portrait based on the actor's age and gender.
            if (actor.Age >= 4 && actor.Age < 12) {
                // For ages 4-11, assign child portraits based on gender.
                Sprite portrait = IM.GetAsset("Child", actor.Gender.ToString()); 
                actor.SetPortrait(portrait);
            }
            else if (actor.Age >= 13 && actor.Age < 18) {
                // For ages 13-17, assign teen portraits based on gender.
                Sprite portrait = IM.GetAsset("Teen", actor.Gender.ToString()); 
                actor.SetPortrait(portrait);
            }
            else if (actor.Age >= 18) {
                // For ages 18 and older, assign adult portraits based on gender and culture.
                // This only happens once due to the "portraitIsSet" flag.
                NBool portraitIsSet = actor.GetVariable<NBool>("PortraitIsSet");
                if (!portraitIsSet.Value) {
                    // Select a random portrait from the AssetDb.
                    var portrait = IM.GetAssets(actor.Culture.CultureName, actor.Gender.ToString()).PickRandom();
                    
                    if (portrait != null) {
                        actor.SetPortrait(portrait);
                        
                        // Mark that a portrait has been set to prevent future changes.
                        portraitIsSet.Value = true;
                    }
                }
            }
        }

        #endregion

        #region MAKECHILD

        private void MakeChildTimer() {
            // Determines if the actor has adopted the "Growing Seeds" policy, affecting childbearing probability and limits.
            bool growingSeeds = actor.HasPolicy("Growing Seeds");

            // Sets the probability of having a child based on whether the "Growing Seeds" policy is in effect.
            float probability = growingSeeds ? 25f : 15f;

            // Sets the maximum number of children allowed based on the "Growing Seeds" policy.
            int maxChild = growingSeeds ? 6 : 3;

            // Checks if the conditions are met for the actor to potentially have a child, based on the calculated probability.
            if (NullUtils.CheckProbability(probability)) {
                // Ensures the actor does not exceed the maximum number of children allowed.
                if (actor.Children().Count() < maxChild) {
                    // Attempts to create a child if the conditions are met.
                    MakeChild();
                }
            }
        }


        public void MakeChild() {
            // Prevents child creation if the actor is in a Passive state.
            if (actor.State == Actor.IState.Passive) return;

            // Conditions for child creation: actor must have a spouse, be female, and be under 60 years old.
            if (actor.HasSpouse && actor.Gender == Actor.IGender.Female && actor.Age < 45) {
                // Selects a random spouse from the actor's spouses.
                var randomSpouse = actor.Spouses(false).PickRandom();

                // Determines the gender of the child randomly.
                Actor.IGender gender = Actor.RandomGender;

                // Generates a name for the child based on the actor's culture and the child's gender.
                string childName = actor.Culture.GenerateName(gender);

                // Creates a new actor instance for the child, specifying basic attributes.
                Sprite childPortrait = IM.GetAsset("Baby", actor.Gender.ToString());
                RuntimeActor bornChild = RuntimeActor.CreateActor(childName, Actor.IState.Active, 0,
                    actor.Culture, gender,
                    childPortrait);

                // Adds the newly born child to the actor's family, linking the child with its parents.
                actor.AddChild(randomSpouse, bornChild,
                    true); // The child automatically joins the family if true.

                // Displays a notification to inform players about the new child.
                NotificationSystem.ShowNotification(
                    $"<color=#9BFF78><link=\"{actor.ID}\">{actor.Name}</link></color> and <color=#FFFFAD><link=\"{randomSpouse.ID}\">{randomSpouse.Name}</link></color> have a new child named <color=#9BFF78><link=\"{bornChild.ID}\">{bornChild.Name}</link></color>.");

                if (IM.IsPlayer(randomSpouse) || IM.IsPlayer(actor)) {
                    IM.Player.StartScheme("Born Child", bornChild);
                }
            }
        }

        #endregion

        #region MARRIAGE

        /// <summary>
        /// Called periodically to allow AI-controlled (non-player) actors to attempt marriage.
        /// The method uses a probability check to decide if the actor will seek a spouse during this cycle.
        /// If so, it requests a match using the MatchEngine, which performs compatibility evaluation asynchronously
        /// and returns the result via a callback. This ensures non-blocking behavior without requiring async/await usage.
        /// </summary>
        private void MarriageTimer()
        {
            // Player-controlled characters are excluded from automated marriage logic
            if (IM.IsPlayer(actor)) return;

            // 20% chance to trigger matchmaking on this cycle
            if (NullUtils.CheckProbability(20f))
            {
                //////////////////////////////////////////////////// Actor randomMatch = await FindNewSpouse(); (for manual)
                // Note: The candidate filtering logic (MatchEngine.CandidateFilter) is not defined here.
                // See DemoManager.cs > Start() for the CandidateFilter configuration.
        
                // Uses the MatchEngine to search for a suitable match for this actor based on the "Love Rule".
                // The compatibility evaluation is asynchronous and non-blocking.
                IM.Instance.MatchEngine.FindMatch(actor, "Love Rule", match => {
                    // If a match is found, initiate the "Love" scheme with the matched actor.
                    if (match != null)
                    {
                        actor.StartScheme("Love", match);
                    }
                });
            }
        }

        [Obsolete("Check out 'IM.Instance.MatchEngine.FindMatch'")]
        public async Task<Actor> FindNewSpouse() {
            // This method is part of the DemoCharacter component on each player character and is executed regularly via a timer.
            // In a game environment with a substantial number of characters (e.g., 300), this method's execution by every character
            // could initiate up to 300x300 compatibility checks, if each character evaluates compatibility with every other character.
            // This large-scale operation underscores the necessity of asynchronous execution to prevent performance degradation,
            // ensuring that the game remains responsive by allowing these compatibility checks to occur in the background
            // without blocking the game's main execution thread.

            // Begin by filtering out potential spouses. This reduces the scope of compatibility checks to only those characters
            // who are not currently married, are in an active state within the game, and have a different gender from the character
            // executing this method. This pre-filtering is crucial for minimizing the computational load by narrowing down
            // the list of characters to those who meet basic criteria for spousal selection.
            var eligibleSpouseCandidates = IM.Actors.Where(p =>
                p.HasSpouse == false && p.State == Actor.IState.Active && p.Gender != actor.Gender);

            var compatibleSpouses = new List<Actor>();

            // Iterate over the filtered list of potential spouses. For each one, perform an asynchronous compatibility check
            // based on the game's "Love Rule". This async approach is vital, especially considering the method's regular execution
            // via a timer for potentially hundreds of characters, as it allows the game to handle a vast number of operations concurrently
            // without impacting the fluidity of gameplay or user experience.
            foreach (var spouseCandidate in eligibleSpouseCandidates) {
                var isCompatible = await IM.IsCompatibleAsync("Love Rule", actor, spouseCandidate);

                // If a potential spouse passes the compatibility check, they are added to the list of candidates.
                // This step accumulates a subset of all evaluated characters that could be considered suitable partners
                // according to the game's defined compatibility logic.
                if (isCompatible) {
                    compatibleSpouses.Add(spouseCandidate);
                }
            }

            // After evaluating the compatibility of potential spouses, select one at random from the list of candidates
            // if any compatible partners are found. This selection process introduces an element of unpredictability
            // and diversity in the outcomes of spousal selection, reflecting the dynamic and evolving nature of character
            // relationships within the game.
            if (compatibleSpouses.Count > 0) {
                return compatibleSpouses.PickRandom();
            }

            // If no compatible spouse is found after all potential candidates have been evaluated, return null.
            // This indicates that the current character did not find a suitable match at this time, which could influence
            // subsequent gameplay and character development decisions.
            return null;
        }

        #endregion

        #region REVOLT

        public void RevoltTimer() {
            // Prevents automated revolt logic for the player character to maintain player agency.
            // This is crucial to ensure that player-controlled characters are not automatically engaged in revolts, which could disrupt the player's gameplay experience.
            if (IM.IsPlayer(actor)) return;

            // Checks if the actor meets the prerequisites for starting a revolt:
            // - Must be part of a clan.
            // - Must have a role assigned.
            // - Must be at least 32 years old, implying a certain level of maturity and experience.
            // - Must hold the role of "General", suggesting a position of military power and influence, which is typically necessary to initiate a revolt.
            if (actor.Clan == null || actor.Role == null || actor.Age < 32 || actor.Role.RoleName != "General") return;

            // Introduces a 20% chance for the revolt to be initiated.
            // This probabilistic approach adds an element of unpredictability to the gameplay, making the game world feel more dynamic.
            if (NullUtils.CheckProbability(25f)) {
                // Retrieves the leader of the actor's clan to target for the revolt.
                // The assumption here is that revolts are directed against the clan's current leadership.
                Actor leader = actor.Clan.GetMember("Leader");
                if (leader == null) return; // Ensures there is a valid leader to revolt against.

                // Initiates a "Revolt" scheme against the clan leader.
                // This action represents the actor leveraging their position and influence to challenge the current leadership, potentially altering the clan's power structure.
                actor.StartScheme("Revolt", leader);
            }
        }

        #endregion
    }
}