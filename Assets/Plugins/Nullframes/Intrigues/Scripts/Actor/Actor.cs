using System;
using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Graph;
using Nullframes.Intrigues.SaveSystem;
using Nullframes.Intrigues.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace Nullframes.Intrigues {
    [DefaultExecutionOrder(-360)]
    public abstract class Actor : MonoBehaviour {
        #region PRIVATE_EVENTS

        private static event Action<Actor, string> onChildSpawned;
        private static event Action<Actor, string> onParentSpawned;
        private static event Action<Actor, string> onSpouseSpawned;
        private static event Action<Actor> _onDestroy;

        #endregion

        #region EVENTS

        /// <summary>
        /// Occurs when a scheme is started by the actor.
        /// </summary>
        public event Action<Scheme> onSchemeStarted;

        /// <summary>
        /// Occurs when a scheme is loaded from the Intrigue Save System.
        /// </summary>
        /// <remarks>
        /// This event is triggered whenever a scheme, which has been previously saved in the Intrigue Save System, is loaded.
        /// It provides a mechanism to respond to the loading of schemes, enabling actions to be taken when specific schemes are retrieved from the save system.
        /// </remarks>
        public event Action<Scheme> onSchemeLoaded;

        /// <summary>
        /// Occurs when a scheme ends.
        /// </summary>
        /// <remarks>
        /// This event is triggered when a scheme comes to a conclusion. The event handler receives both the scheme that has just ended and its result.
        /// This enables reacting to the end of a scheme, facilitating the assessment and response to its outcome, 
        /// which is crucial for managing the consequences and flow of schemes within the game.
        /// </remarks>
        public event Action<Scheme, SchemeResult> onSchemeEnded;

        /// <summary>
        /// Occurs when the player-character dies.
        /// </summary>
        public event Action onActorDeath;

        /// <summary>
        /// Occurs when an Actor is destroyed.
        /// </summary>
        /// <remarks>
        /// This event is triggered when an Actor instance is destroyed. It can be used to perform cleanup, 
        /// release resources, or trigger other game mechanics that need to respond to the destruction of an actor.
        /// </remarks>
        public event Action<Actor> onDestroy;

        /// <summary>
        /// Occurs when the actor changes culture.
        /// </summary>
        public event Action<Culture> onCultureChanged;

        /// <summary>
        /// Occurs when the actor is assigned a new role.
        /// </summary>
        public event Action<Role> onRoleChanged;

        /// <summary>
        /// Occurs when the actor joins a clan.
        /// </summary>
        public event Action<Clan> onJoinedClan;

        /// <summary>
        /// Occurs when the actor leaves a clan.
        /// </summary>
        public event Action<Clan> onLeftClan;

        /// <summary>
        /// Occurs when the actor joins a family.
        /// </summary>
        public event Action<Family> onJoinedFamily;

        /// <summary>
        /// Occurs when the actor leaves a family.
        /// </summary>
        public event Action<Family> onLeftFamily;

        /// <summary>
        /// Occurs when an inheritance is received by an actor.
        /// </summary>
        /// <remarks>
        /// This event is triggered when an actor inherits a role. It allows for actions to be taken in response to the inheritance event,
        /// such as updating the state of the actor, reflecting changes in the actor's assets or status, or triggering related game mechanics.
        /// </remarks>
        public event Action<Actor> onInherited;

        /// <summary>
        /// Occurs when the player-character gets married.
        /// </summary>
        public event Action<Actor, Actor, bool> onActorIsMarried;

        /// <summary>
        /// Occurs when a spouse is removed from the actor.
        /// </summary>
        public event Action<Actor, Actor> onActorDivorced;

        /// <summary>
        /// Occurs when the actor's age changes.
        /// </summary>
        public event Action<Actor, int> onAgeChanged;

        /// <summary>
        /// Occurs when a specific trigger is activated for the actor.
        /// </summary>
        public event Action<string, bool> OnTrigger;

        #endregion

        #region ENUMS

        /// <summary>
        /// Specifies the gender of an actor.
        /// </summary>
        public enum IGender {
            /// <summary>
            /// Indicates a male actor.
            /// </summary>
            Male = 0,

            /// <summary>
            /// Indicates a female actor.
            /// </summary>
            Female = 1
        }

        /// <summary>
        /// Specifies the state of an actor.
        /// Active represents an alive actor while Passive indicates a dead actor.
        /// </summary>
        public enum IState {
            /// <summary>
            /// Indicates an alive actor.
            /// </summary>
            Active = 0,

            /// <summary>
            /// Indicates a dead actor.
            /// </summary>
            Passive = 1
        }

        /// <summary>
        /// Specifies verification types for checking the status of schemes relative to an actor.
        /// </summary>
        [Flags]
        public enum VerifyType {
            /// <summary>
            /// Checks if the scheme is active against a specific target.
            /// </summary>
            ToTarget = 1 << 0,

            /// <summary>
            /// Checks if the scheme is active against any target.
            /// </summary>
            ToAnyone = 1 << 1,

            /// <summary>
            /// Checks if someone has activated the scheme against this actor.
            /// </summary>
            FromAnyone = 1 << 2,
            
            /// <summary>
            /// Checks if target has activated the scheme against this actor.
            /// </summary>
            FromTarget = 1 << 3,
        }

        #endregion

        #region FIELDS

        [SerializeField] private string id;
        [SerializeField] private bool isPlayer;
        [FormerlySerializedAs("actorSpecificVariables")] [SerializeField] private SerializableDictionary<Actor, List<NVar>> relationVariables = new();
        [SerializeReference] protected List<NVar> variables = new();
        private List<Scheme> _schemes;
        private List<Scheme> _schemesAgainstToActor;
        private List<Actor> _parents;
        private List<Actor> _children;
        private List<Actor> _spouses;

        private Family _origin;
        private Family _overrideOrigin;

        private Actor _heir {
            get {
                List<Actor> compatibleList = new List<Actor>();

                if (Role.HeirFilter?.Filter != null) {
                    foreach (var heir in IM.Actors.Where(a => a.State == IState.Active && this != a && a.Inheritor)
                                 .Where(a => a.Role == null || a.Role.Priority < Role.Priority)) {
                        foreach (var @delegate in Role.HeirFilter.Filter.GetInvocationList()) {
                            var run = (HeirFilter.HFilter<Actor, Actor, bool>)@delegate;

                            if (run.Invoke(this, heir)) {
                                compatibleList.Add(heir);
                                break;
                            }
                        }
                    }
                }

                List<Actor> filteredActors = new List<Actor>();

                if (Role.HeirFilter?.FilterAbsolute != null) {
                    foreach (var heir in compatibleList) {
                        foreach (var @delegate in Role.HeirFilter.FilterAbsolute.GetInvocationList()) {
                            var run = (HeirFilter.HFilter<Actor, Actor, bool>)@delegate;

                            if (!run.Invoke(this, heir)) {
                                filteredActors.Add(heir);
                                break;
                            }
                        }
                    }
                }

                foreach (var filtered in filteredActors) {
                    compatibleList.Remove(filtered);
                }

                if (Role.HeirFilter?.OrderBy != null)
                    compatibleList = Role.HeirFilter.OrderBy.GetInvocationList()
                        .Cast<HeirFilter.HOrderBy<Actor, Actor, object>>()
                        .Where(delg => delg != null).Aggregate(compatibleList,
                            (current, delg) => current.OrderBy(a => delg.Invoke(this, a)).ToList());

                if (Role.HeirFilter?.OrderByDesc != null)
                    compatibleList = Role.HeirFilter.OrderByDesc.GetInvocationList()
                        .Cast<HeirFilter.HOrderBy<Actor, Actor, object>>()
                        .Where(delg => delg != null).Aggregate(compatibleList,
                            (current, delg) => current.OrderByDescending(a => delg.Invoke(this, a)).ToList());

                if (Role.HeirFilter?.Filter != null)
                    compatibleList = Role.HeirFilter.Filter.GetInvocationList()
                        .Cast<HeirFilter.HFilter<Actor, Actor, bool>>()
                        .Where(delg => delg != null).Aggregate(compatibleList,
                            (current, delg) => current.OrderBy(a => delg.Invoke(this, a)).ToList());

                if (Role.HeirFilter?.FilterAbsolute != null)
                    compatibleList = Role.HeirFilter.FilterAbsolute.GetInvocationList()
                        .Cast<HeirFilter.HFilter<Actor, Actor, bool>>()
                        .Where(delg => delg != null).Aggregate(compatibleList,
                            (current, delg) => current.OrderBy(a => delg.Invoke(this, a)).ToList());

                if (Role.HeirFilter?.OrderFilter != null)
                    compatibleList = Role.HeirFilter.OrderFilter.GetInvocationList()
                        .Cast<HeirFilter.HFilter<Actor, Actor, bool>>()
                        .Where(delg => delg != null).Aggregate(compatibleList,
                            (current, delg) => current.OrderBy(a => delg.Invoke(this, a)).ToList());

                return compatibleList.FirstOrDefault();
            }
        }

        private Actor _overrideHeir;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// Gets the unique ID of the actor.
        /// </summary>
        public string ID {
            get => id;
            protected set => id = value;
        }

        /// <summary>
        /// Gets the name of the actor.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Retrieves the full name of the actor, typically in the format of "role name + name + of + family name".
        /// </summary>
        public string FullName {
            get {
                var fullName = Name;
                var title = Title;
                const string subfix = "of";
                if (Role != null && !string.IsNullOrEmpty(title)) fullName = fullName.Insert(0, title + " ");

                if (Family != null) fullName = fullName + $" {subfix} " + Family.FamilyName;
                return fullName;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the actor's heir has been overridden.
        /// </summary>
        public bool HeirOverridden => _overrideHeir != null;

        /// <summary>
        /// Provides a randomly generated gender.
        /// </summary>
        public static IGender RandomGender => (IGender)UnityEngine.Random.Range(0, 2);

        /// <summary>
        /// Gets the state of the actor, either Passive (Dead) or Active (Alive).
        /// </summary>
        public IState State { get; protected set; }

        /// <summary>
        /// Gets the age of the actor.
        /// </summary>
        public int Age { get; protected set; }

        /// <summary>
        /// Gets the culture of the actor.
        /// </summary>
        public Culture Culture { get; protected set; }

        /// <summary>
        /// Gets the role of the actor.
        /// </summary>
        public Role Role { get; private set; }

        /// <summary>
        /// Gets the clan to which the actor belongs.
        /// </summary>
        public Clan Clan { get; private set; }

        /// <summary>
        /// Gets the family to which the actor belongs.
        /// </summary>
        public Family Family => _overrideOrigin ?? _origin;

        /// <summary>
        /// Gets the origin family of the actor.
        /// </summary>
        public Family Origin => _origin;

        /// <summary>
        /// Gets the heir of the actor.
        /// </summary>
        public Actor Heir {
            get {
                var result = Clan == null
                    ? null
                    : Role == null
                        ? null
                        : _overrideHeir != null && _overrideHeir.State == IState.Active && _overrideHeir.Inheritor
                            ? _overrideHeir
                            : _heir;
                return result;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the actor is an inheritor. The default value is true.
        /// </summary>
        public bool Inheritor { get; private set; } = true;

        /// <summary>
        /// Gets the gender of the actor.
        /// </summary>
        public IGender Gender { get; protected set; }

        /// <summary>
        /// Gets the portrait of the actor.
        /// </summary>
        public Sprite Portrait { get; protected set; }

        /// <summary>
        /// Retrieves the schemes that the actor has activated.
        /// </summary>
        public IEnumerable<Scheme> Schemes => _schemes;

        /// <summary>
        /// Retrieves the schemes that have been activated against this actor.
        /// </summary>
        public IEnumerable<Scheme> SchemesAgainstToActor => _schemesAgainstToActor;

        /// <summary>
        /// Contains private variables associated with the actor.
        /// </summary>
        public IEnumerable<NVar> Variables => variables;
        
        public IReadOnlyDictionary<Actor, List<NVar>> RelationVariables => relationVariables;
        
        /// <summary>
        /// Checks whether the actor is designated as the player.
        /// </summary>
        public bool IsPlayer => isPlayer;

        #endregion

        #region SAVE SYSTEM

        [field: NonSerialized] protected SaveSystem.ActorData Data { get; set; }

        #endregion

        #region METHODS

        protected void Init() {
            _parents = new List<Actor>();
            _children = new List<Actor>();
            _spouses = new List<Actor>();

            _schemes = new List<Scheme>();
            _schemesAgainstToActor = new List<Scheme>();
        }

        protected void OnEnable() {
            Enabled();
        }

        protected void OnDisable() {
            Disabled();
        }

        private void OnDestroy() {
            Destroyed();
            _onDestroy?.Invoke(this);
            onDestroy?.Invoke(this);
        }

        protected void Start() {
            OnStart();
        }

        protected void Awake() {
            OnAwake();
        }

        protected virtual void OnStart() {
            SpawnFamilyMember();
            if (Data != null) {
                SaveSystem_LoadSchemes();
                SaveSystem_LoadRelationVariables();
            }
        }

        protected virtual void OnAwake() {
            Init();

            LoadData();

            if (Data != null) {
                isPlayer = Data.isPlayer;
                Inheritor = Data.inheritor;
            }

            IM.AddActor(this);

            RegisterEvents();

            UpdateVariables();
            UpdateActor();
            UpdateFamily();
            UpdateClan();
        }

        private void OnPlayerChanged(Actor oldPlayer, Actor newPlayer) {
            isPlayer = newPlayer == this;
        }

        protected virtual void Enabled() { }

        protected virtual void Disabled() { }

        protected virtual void Destroyed() {
            IM.DestroyActor(this);
            UnRegisterEvents();
            Clan?.Quit(this, false);
            Family?.Quit(this, false);
        }

        private void OnActorDestroy(Actor actor) {
            _parents.Remove(actor);
            _children.Remove(actor);
            _spouses.Remove(actor);
        }

        protected void RegisterEvents() {
            onChildSpawned += OnChildSpawned;
            onParentSpawned += OnParentSpawned;
            onSpouseSpawned += OnSpouseSpawned;
            _onDestroy += OnActorDestroy;

            onSchemeEnded += OnSchemeEnded;
            onSchemeStarted += OnSchemeStarted;
            onSchemeLoaded += OnSchemeLoaded;

            Schemer.onConspiratorChanged += OnConspiratorChanged;
            Schemer.onTargetChanged += OnTargetChanged;

            onActorDeath += OnActorDeath;

            IM.onPlayerIsChanged += OnPlayerChanged;

            IntrigueSaveSystem.onLoad += OnLoad;
        }

        private void UnRegisterEvents() {
            onChildSpawned -= OnChildSpawned;
            onParentSpawned -= OnParentSpawned;
            onSpouseSpawned -= OnSpouseSpawned;
            _onDestroy -= OnActorDestroy;

            onSchemeEnded -= OnSchemeEnded;
            onSchemeStarted -= OnSchemeStarted;
            onSchemeLoaded -= OnSchemeLoaded;

            Schemer.onConspiratorChanged -= OnConspiratorChanged;
            Schemer.onTargetChanged -= OnTargetChanged;

            onActorDeath -= OnActorDeath;

            IM.onPlayerIsChanged -= OnPlayerChanged;

            IntrigueSaveSystem.onLoad -= OnLoad;
        }

        private void OnLoad() {
            if (IntrigueSaveSystem.Instance.Data.actorData.All(a => a.id != ID)) {
                Destroy(gameObject);
            }

            if (IsPlayer) {
                IM.SetPlayer(this);
            }
        }

        protected void LoadData() {
            if (IntrigueSaveSystem.Instance != null && IntrigueSaveSystem.Instance.Data != null)
                Data = IntrigueSaveSystem.Instance.Data.actorData.Find(a => a.id == ID);
        }

        protected virtual void OnActorDeath() {
            if (Role is { Inheritance: true }) {
                if (Heir == null) return;

                var heirActor = Heir;

                heirActor.JoinClan(Clan);
                heirActor.SetRole(Role);
                heirActor.onInherited?.Invoke(this);
            }
        }

        private void OnSchemeStarted(Scheme scheme) {
            if (scheme.Schemer.Target == null) return;

            scheme.Schemer.Target._schemesAgainstToActor.Add(scheme);
        }

        private void OnSchemeLoaded(Scheme scheme) {
            if (scheme.Schemer.Target == null) return;

            scheme.Schemer.Target._schemesAgainstToActor.Add(scheme);
        }

        private void OnSchemeEnded(Scheme scheme, SchemeResult result) {
            if (scheme.Schemer.Target == null) return;

            scheme.Schemer.Target._schemesAgainstToActor.Remove(scheme);
        }

        private void OnConspiratorChanged(Scheme scheme, Actor prevConspirator) {
            if (prevConspirator == this) {
                _schemes.Remove(scheme);
                scheme.Schemer.Conspirator._schemes.Add(scheme);
            }
        }

        private void OnTargetChanged(Scheme scheme, Actor prevTarget) {
            if (prevTarget == this) {
                _schemesAgainstToActor.Remove(scheme);
                scheme.Schemer.Target._schemesAgainstToActor.Add(scheme);
            }
        }

        private void OnChildSpawned(Actor child, string parentID) {
            if (child == null) return;
            if (ID.Equals(parentID) && !_children.Contains(child)) _children.Add(child);
        }

        private void OnParentSpawned(Actor parent, string childID) {
            if (parent == null) return;
            if (ID.Equals(childID) && !_parents.Contains(parent)) _parents.Add(parent);
        }

        private void OnSpouseSpawned(Actor partner, string partnerID) {
            if (partner == null) return;
            if (ID.Equals(partnerID) && !_spouses.Contains(partner)) _spouses.Add(partner);
        }

        protected void SpawnFamilyMember() {
            if (Data != null) {
                SaveSystem_SpawnFamilyMember();
                return;
            }

            //Children
            foreach (var parent in from child in FamilyFlow.Childs
                     from node in child.Value
                     where ID.Equals(node.ActorID)
                     select child) onChildSpawned?.Invoke(this, parent.Key); // parent.Key = Parent ID

            if (FamilyFlow.Childs.ContainsKey(ID))
                foreach (var child in FamilyFlow.Childs[ID]
                             .Where(child => IM.ActorDictionary.ContainsKey(child.ActorID))) {
                    if (!IM.ActorDictionary.ContainsKey(child.ActorID)) continue;
                    var managerActor = IM.ActorDictionary[child.ActorID];
                    if (_children.Contains(managerActor)) continue;
                    _children.Add(managerActor);
                }

            //Parent
            foreach (var child in from parent in FamilyFlow.Parents
                     from node in parent.Value
                     where ID.Equals(node.ActorID)
                     select parent) onParentSpawned?.Invoke(this, child.Key); // child.Key = Child ID

            if (FamilyFlow.Parents.ContainsKey(ID))
                foreach (var parent in FamilyFlow.Parents[ID]
                             .Where(child => IM.ActorDictionary.ContainsKey(child.ActorID))) {
                    if (!IM.ActorDictionary.ContainsKey(parent.ActorID)) continue;
                    var managerActor = IM.ActorDictionary[parent.ActorID];
                    if (_parents.Contains(managerActor)) continue;
                    _parents.Add(managerActor);
                }

            //Partner
            foreach (var partner in from partner in FamilyFlow.Partners
                     from node in partner.Value
                     where ID.Equals(node.ActorID)
                     select partner) onSpouseSpawned?.Invoke(this, partner.Key); // partner.Key = Partner ID

            if (FamilyFlow.Partners.ContainsKey(ID))
                foreach (var spouse in FamilyFlow.Partners[ID]
                             .Where(child => IM.ActorDictionary.ContainsKey(child.ActorID))) {
                    if (!IM.ActorDictionary.ContainsKey(spouse.ActorID)) continue;
                    var managerActor = IM.ActorDictionary[spouse.ActorID];
                    if (_spouses.Contains(managerActor)) continue;
                    _spouses.Add(managerActor);
                }
        }

        protected void UpdateActor() {
            if (Data != null) {
                SaveSystem_LoadActor();
                return;
            }

            if (this is RuntimeActor) return;

            var dbActor = IM.IEDatabase.actorRegistry.FirstOrDefault(a => a.ID == ID);
            if (dbActor == null) {
                NDebug.Log("No data found for this actor object.");
                enabled = false;
                return;
            }

            Name = dbActor.Name;
            State = dbActor.CurrentState;
            Age = dbActor.Age;
            Culture = IM.Cultures.FirstOrDefault(c => c.ID == dbActor.CultureID);
            Gender = dbActor.Gender;
            Portrait = dbActor.Portrait;
            isPlayer = dbActor.IsPlayer;
        }

        protected void UpdateVariables() {
            //Clear removed variables
            var removedVariables = Variables.Where(privateVariable => IM.Variables.Any(v => v.id == privateVariable.id) == false).ToList();

            removedVariables.ForEach(v => variables.Remove(v));

            //Initialize variables
            foreach (var variable in IM.Variables) {
                if (!variables.Exists(v => v.id == variable.id)) {
                    variables.Add(variable.Duplicate());
                }
            }

            if (Data != null)
                SaveSystem_LoadVariables();
        }
        
        private void UpdateRelationVariables(Actor target) {
            if (!RelationVariables.ContainsKey(target)) {
                relationVariables.Add(target, new List<NVar>());
            }
            
            //Clear removed variables
            var removedVariables = Variables.Where(privateVariable => IM.Variables.Any(v => v.id == privateVariable.id) == false).ToList();

            removedVariables.ForEach(v => relationVariables[target].Remove(v));
            
            //Initialize variables
            foreach (var variable in IM.Variables) {
                if (!relationVariables[target].Exists(v => v.id == variable.id)) {
                    relationVariables[target].Add(variable.Duplicate());
                }
            }
        }

        protected void UpdateClan() {
            if (Data != null) {
                SaveSystem_LoadClan();
                return;
            }

            foreach (var _clan in ClanFlow.Clans) {
                var dbValue = _clan.Value.Find(c => c.ActorID == ID);
                if (dbValue != null) {
                    var clanObject = IM.Clans.FirstOrDefault(c => c.ID == _clan.Key.ID);
                    if (clanObject != null) {
                        var role = IM.Roles.FirstOrDefault(m => m.ID == dbValue.RoleID);
                        if (role != null) {
                            var currentSlot =
                                clanObject.Members.Count(r => r.Role == role && r.State == IState.Active);
                            if (role.Capacity > currentSlot) {
                                Role = role;
                            }
                        }

                        if (clanObject.Join(this, true)) Clan = clanObject;
                    }

                    break;
                }
            }
        }

        protected void UpdateFamily() {
            if (Data != null) {
                SaveSystem_LoadFamily();
                return;
            }

            foreach (var _family in FamilyFlow.Families) {
                var dbValue = _family.Value.Find(c => c.ActorID == ID);
                if (dbValue != null) {
                    var familyObject = IM.Families.FirstOrDefault(c => c.ID == _family.Key.ID);
                    if (familyObject != null)
                        if (familyObject.Join(this, true))
                            _origin = familyObject;

                    break;
                }
            }
        }

        #endregion

        #region FAMILY

        /// <summary>
        /// Retrieves the parents of the actor.
        /// </summary>
        /// <param name="inclusivePassive">Flag indicating whether dead parents should be included.</param>
        /// <returns>A collection of parents.</returns>
        public IEnumerable<Actor> Parents(bool inclusivePassive = true) {
            return inclusivePassive ? _parents : _parents.Where(p => p.State == IState.Active);
        }

        /// <summary>
        /// Retrieves the children of the actor.
        /// </summary>
        /// <param name="inclusivePassive">Flag indicating whether dead children should be included.</param>
        /// <returns>A collection of children.</returns>
        public IEnumerable<Actor> Children(bool inclusivePassive = true) {
            return inclusivePassive ? _children : _children.Where(c => c.State == IState.Active);
        }

        /// <summary>
        /// Retrieves the spouses of the actor.
        /// </summary>
        /// <param name="inclusivePassive">Flag indicating whether dead spouses should be included.</param>
        /// <returns>A collection of spouses.</returns>
        public IEnumerable<Actor> Spouses(bool inclusivePassive = true) {
            return inclusivePassive ? _spouses : _spouses.Where(s => s.State == IState.Active);
        }

        /// <summary>
        /// Retrieves the grandparents of the actor.
        /// </summary>
        /// <param name="inclusivePassive">Flag indicating whether dead grandparents should be included.</param>
        /// <returns>A collection of grandparents.</returns>
        public IEnumerable<Actor> Grandparents(bool inclusivePassive = true) {
            return inclusivePassive
                ? _parents.SelectMany(parent => parent._parents).ToList()
                : _parents.SelectMany(parent => parent._parents).Where(p => p.State == IState.Active).ToList();
        }

        /// <summary>
        /// Retrieves the grandchildren of the actor.
        /// </summary>
        /// <param name="inclusivePassive">Flag indicating whether dead grandchildren should be included.</param>
        /// <returns>A collection of grandchildren.</returns>
        public IEnumerable<Actor> Grandchildren(bool inclusivePassive = true) {
            return inclusivePassive
                ? _children.SelectMany(grandchild => grandchild._children).ToHashSet()
                : _children.SelectMany(grandchild => grandchild._children).Where(p => p.State == IState.Active)
                    .ToHashSet();
        }

        /// <summary>
        /// Retrieves the siblings of the actor.
        /// </summary>
        /// <param name="inclusivePassive">Flag indicating whether dead siblings should be included.</param>
        /// <returns>A collection of siblings.</returns>
        public IEnumerable<Actor> Siblings(bool inclusivePassive = true) {
            return inclusivePassive
                ? _parents.SelectMany(parent => parent._children).Where(e => e != this).ToHashSet()
                : _parents.SelectMany(parent => parent._children).Where(e => e != this)
                    .Where(p => p.State == IState.Active).ToHashSet();
        }

        /// <summary>
        /// Retrieves the nephews of the actor.
        /// </summary>
        /// <param name="inclusivePassive">Flag indicating whether dead nephews should be included.</param>
        /// <returns>A collection of nephews.</returns>
        public IEnumerable<Actor> Nephews(bool inclusivePassive = true) {
            return inclusivePassive
                ? Siblings().SelectMany(sibling => sibling._children.Where(s => s.Gender == IGender.Male)).ToHashSet()
                : Siblings().SelectMany(sibling => sibling._children.Where(s => s.Gender == IGender.Male))
                    .Where(p => p.State == IState.Active).ToHashSet();
        }

        /// <summary>
        /// Retrieves the nieces of the actor.
        /// </summary>
        /// <param name="inclusivePassive">Flag indicating whether dead nieces should be included.</param>
        /// <returns>A collection of nieces.</returns>
        public IEnumerable<Actor> Nieces(bool inclusivePassive = true) {
            return inclusivePassive
                ? Siblings().SelectMany(sibling => sibling._children.Where(s => s.Gender == IGender.Female)).ToHashSet()
                : Siblings().SelectMany(sibling => sibling._children.Where(s => s.Gender == IGender.Female))
                    .Where(p => p.State == IState.Active).ToHashSet();
        }

        /// <summary>
        /// Retrieves the uncles of the actor.
        /// </summary>
        /// <param name="inclusivePassive">Flag indicating whether dead uncles should be included.</param>
        /// <returns>A collection of uncles.</returns>
        public IEnumerable<Actor> Uncles(bool inclusivePassive = true) {
            return inclusivePassive
                ? _parents.SelectMany(parent => parent.Siblings().Where(s => s.Gender == IGender.Male)).ToHashSet()
                : _parents.SelectMany(parent => parent.Siblings().Where(s => s.Gender == IGender.Male))
                    .Where(p => p.State == IState.Active).ToHashSet();
        }

        /// <summary>
        /// Retrieves the aunts of the actor.
        /// </summary>
        /// <param name="inclusivePassive">Flag indicating whether dead aunts should be included.</param>
        /// <returns>A collection of aunts.</returns>
        public IEnumerable<Actor> Aunts(bool inclusivePassive = true) {
            return inclusivePassive
                ? _parents.SelectMany(parent => parent.Siblings().Where(s => s.Gender == IGender.Female)).ToHashSet()
                : _parents.SelectMany(parent => parent.Siblings().Where(s => s.Gender == IGender.Female))
                    .Where(p => p.State == IState.Active).ToHashSet();
        }

        /// <summary>
        /// Retrieves the brothers-in-law of the actor.
        /// </summary>
        /// <param name="inclusivePassive">Flag indicating whether dead brothers-in-law should be included.</param>
        /// <returns>A collection of brothers-in-law.</returns>
        public IEnumerable<Actor> BrothersInLaw(bool inclusivePassive = true) {
            return inclusivePassive
                ? Siblings().SelectMany(sibling => sibling._spouses.Where(s => s.Gender == IGender.Male)).ToHashSet()
                : Siblings().SelectMany(sibling => sibling._spouses.Where(s => s.Gender == IGender.Male))
                    .Where(p => p.State == IState.Active).ToHashSet();
        }

        /// <summary>
        /// Retrieves the sisters-in-law of the actor.
        /// </summary>
        /// <param name="inclusivePassive">Flag indicating whether dead sisters-in-law should be included.</param>
        /// <returns>A collection of sisters-in-law.</returns>
        public IEnumerable<Actor> SistersInLaw(bool inclusivePassive = true) {
            return inclusivePassive
                ? Siblings().SelectMany(sibling => sibling._spouses.Where(s => s.Gender == IGender.Female)).ToHashSet()
                : Siblings().SelectMany(sibling => sibling._spouses.Where(s => s.Gender == IGender.Female))
                    .Where(p => p.State == IState.Active).ToHashSet();
        }

        /// <summary>
        /// Determines whether the actor has a spouse.
        /// </summary>
        /// <returns>True if the actor has a spouse; otherwise, false.</returns>
        public bool HasSpouse => Spouses(false).Any();

        /// <summary>
        /// Determines whether the actor has any child.
        /// </summary>
        /// <returns>True if the actor has at least one child; otherwise, false.</returns>
        public bool HasChildren => Children(false).Any();

        /// <summary>
        /// Determines whether the actor has any parent.
        /// </summary>
        /// <returns>True if the actor has at least one parent; otherwise, false.</returns>
        public bool HasParent => Parents(false).Any();

        /// <summary>
        /// Determines whether the actor has any grandparent.
        /// </summary>
        /// <returns>True if the actor has at least one grandparent; otherwise, false.</returns>
        public bool HasGrandparent => Grandparents(false).Any();

        /// <summary>
        /// Determines whether the actor has any sibling.
        /// </summary>
        /// <returns>True if the actor has at least one sibling; otherwise, false.</returns>
        public bool HasSibling => Siblings(false).Any();

        /// <summary>
        /// Determines whether the actor has any uncle.
        /// </summary>
        /// <returns>True if the actor has at least one uncle; otherwise, false.</returns>
        public bool HasUncle => Uncles(false).Any();

        /// <summary>
        /// Determines whether the actor has any aunt.
        /// </summary>
        /// <returns>True if the actor has at least one aunt; otherwise, false.</returns>
        public bool HasAunt => Aunts(false).Any();

        /// <summary>
        /// Determines whether the actor has any nephew.
        /// </summary>
        /// <returns>True if the actor has at least one nephew; otherwise, false.</returns>
        public bool HasNephew => Nephews(false).Any();

        /// <summary>
        /// Determines whether the actor has any niece.
        /// </summary>
        /// <returns>True if the actor has at least one niece; otherwise, false.</returns>
        public bool HasNiece => Nieces(false).Any();

        /// <summary>
        /// Determines whether the actor has any grandchild.
        /// </summary>
        /// <returns>True if the actor has at least one grandchild; otherwise, false.</returns>
        public bool HasGrandchildren => Grandchildren(false).Any();

        /// <summary>
        /// Determines whether the actor has any brother-in-law.
        /// </summary>
        /// <returns><c>true</c> if the actor has any brother-in-law; otherwise, <c>false</c>.</returns>
        public bool HasBrotherInLaw => Siblings(false).SelectMany(s => s._spouses).Any(s => s.Gender == IGender.Male);

        /// <summary>
        /// Determines whether the actor has any sister-in-law.
        /// </summary>
        /// <returns><c>true</c> if the actor has any sister-in-law; otherwise, <c>false</c>.</returns>
        public bool HasSisterInLaw => Siblings(false).SelectMany(s => s._spouses).Any(s => s.Gender == IGender.Female);

        /// <summary>
        /// Determines whether the actor is a spouse of the specified actor.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <returns>True if the actor is a spouse of the specified actor; otherwise, false.</returns>
        public bool IsSpouse(Actor actor) => _spouses.Contains(actor);

        /// <summary>
        /// Determines whether the actor is a child of the specified actor.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <returns>True if the actor is a child of the specified actor; otherwise, false.</returns>
        public bool IsChild(Actor actor) => _children.Contains(actor);

        /// <summary>
        /// Determines whether the actor is a parent of the specified actor.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <returns>True if the actor is a parent of the specified actor; otherwise, false.</returns>
        public bool IsParent(Actor actor) => _parents.Contains(actor);

        /// <summary>
        /// Determines whether the actor is an uncle of the specified actor.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <returns>True if the actor is an uncle of the specified actor; otherwise, false.</returns>
        public bool IsUncle(Actor actor) => Uncles().Contains(actor);

        /// <summary>
        /// Determines whether the actor is an aunt of the specified actor.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <returns>True if the actor is an aunt of the specified actor; otherwise, false.</returns>
        public bool IsAunt(Actor actor) => Aunts().Contains(actor);

        /// <summary>
        /// Determines whether the actor is a nephew of the specified actor.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <returns>True if the actor is a nephew of the specified actor; otherwise, false.</returns>
        public bool IsNephew(Actor actor) => Nephews().Contains(actor);

        /// <summary>
        /// Determines whether the actor is a niece of the specified actor.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <returns>True if the actor is a niece of the specified actor; otherwise, false.</returns>
        public bool IsNiece(Actor actor) => Nieces().Contains(actor);

        /// <summary>
        /// Determines whether the actor is a sibling of the specified actor.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <returns>True if the actor is a sibling of the specified actor; otherwise, false.</returns>
        public bool IsSibling(Actor actor) => Siblings().Contains(actor);

        /// <summary>
        /// Determines whether the actor is a grandparent of the specified actor.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <returns>True if the actor is a grandparent of the specified actor; otherwise, false.</returns>
        public bool IsGrandparent(Actor actor) => Grandparents().Contains(actor);

        /// <summary>
        /// Determines whether the actor is a grandchild of the specified actor.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <returns>True if the actor is a grandchild of the specified actor; otherwise, false.</returns>
        public bool IsGrandchild(Actor actor) => Grandchildren().Contains(actor);

        /// <summary>
        /// Determines whether the actor is a brother-in-law of the specified actor.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <returns><c>true</c> if the actor is a brother-in-law of the specified actor; otherwise, <c>false</c>.</returns>
        public bool IsBrotherInLaw(Actor actor) =>
            Siblings().SelectMany(s => s._spouses).Any(s => s == actor && s.Gender == IGender.Male);

        /// <summary>
        /// Determines whether the actor is a sister-in-law of the specified actor.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <returns><c>true</c> if the actor is a sister-in-law of the specified actor; otherwise, <c>false</c>.</returns>
        public bool IsSisterInLaw(Actor actor) =>
            Siblings().SelectMany(s => s._spouses).Any(s => s == actor && s.Gender == IGender.Female);

        /// <summary>
        /// Determines whether the actor is a relative of the specified actor.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <returns>True if the actor is a relative of the specified actor; otherwise, false.</returns>
        public bool IsRelative(Actor actor)
            => IsChild(actor) || IsParent(actor) || IsSibling(actor) || IsGrandparent(actor) ||
               IsGrandchild(actor) || IsUncle(actor) || IsAunt(actor) || IsNephew(actor) || IsNiece(actor);


        /// <summary>
        /// Makes the actor join a specified family.
        /// </summary>
        /// <param name="family">The Family object to join.</param>
        /// <param name="setOrigin">If true, also sets the family as the actor's origin family.</param>
        public void JoinFamily(Family family, bool setOrigin = false) {
            if (family == null) return;

            if (family.Join(this, true)) {
                QuitFamily();

                if (setOrigin)
                    _origin = family;
                else
                    _overrideOrigin = family;

                onJoinedFamily?.Invoke(family);
                family.onMemberJoin?.Invoke(this);
            }
        }

        /// <summary>
        /// Makes the actor join a family based on the given family name or ID.
        /// </summary>
        /// <param name="familyNameOrId">Name or ID of the family to join.</param>
        /// <param name="setOrigin">If true, also sets the family as the actor's origin family.</param>
        public void JoinFamily(string familyNameOrId, bool setOrigin = false) => JoinFamily(
            IM.Families.FirstOrDefault(c => c.FamilyName == familyNameOrId || c.ID == familyNameOrId), setOrigin);

        /// <summary>
        /// Makes the actor quit their current family.
        /// </summary>
        /// <param name="forgetOrigin">If true and the actor is quitting their origin family, the origin family is forgotten.</param>
        public void QuitFamily(bool forgetOrigin = false) {
            if (Family == null) return;
            var oldFamily = Family;
            if (Family.Quit(this)) {
                onLeftFamily?.Invoke(Family);
                _overrideOrigin = null;

                if (forgetOrigin && oldFamily == _origin)
                    _origin = null;
            }
        }

        /// <summary>
        /// Adds a child to the actor.
        /// </summary>
        /// <param name="child">The Actor object representing the child to be added.</param>
        /// <param name="joinFamily">If true, the child will automatically join the family.</param>
        /// <param name="setOrigin">If true, the child's origin family will be set to the current actor's family.</param>
        public void AddChild(Actor child, bool joinFamily = true, bool setOrigin = true) {
            if (Age < child.Age || _children.Contains(child) || child._parents.Count > 1) return;

            _children.Add(child);
            child._parents.Add(this);

            if (joinFamily && Family != null) {
                child.JoinFamily(Family, setOrigin);
            }
        }

        /// <summary>
        /// Adds a child to the actor, specifying the other parent.
        /// </summary>
        /// <param name="spouse">The Actor object representing the other parent of the child.</param>
        /// <param name="child">The Actor object representing the child to be added.</param>
        /// <param name="joinFamily">If true, the child will automatically join the family.</param>
        /// <param name="setOrigin">If true, the child's origin family will be set to the current actor's family.</param>
        public void AddChild(Actor spouse, Actor child, bool joinFamily = true, bool setOrigin = true) {
            if (child._parents.Count > 0) return;

            _children.Add(child);
            spouse._children.Add(child);

            child._parents.Add(this);
            child._parents.Add(spouse);

            if (joinFamily && Family != null) {
                child.JoinFamily(Family, setOrigin);
            }
        }

        /// <summary>
        /// Adds a specified actor as a spouse to the current actor.
        /// </summary>
        /// <param name="spouse">The Actor object representing the spouse to be added.</param>
        /// <param name="joinSpouseFamily">If true, the current actor will join the family of the added spouse.</param>
        public void AddSpouse(Actor spouse, bool joinSpouseFamily = false) {
            // if (Family == null && spouse.Family == null) return;
            // if (_spouses.Count > 0 && _spouses[0]._spouses.Count > 1) return;
            if (_spouses.Contains(spouse)) return;
            _spouses.Add(spouse);
            spouse._spouses.Add(this);

            if (Family != null && spouse.Family != null) {
                if (joinSpouseFamily) {
                    if (spouse.Family != null)
                        JoinFamily(spouse.Family);
                    else
                        spouse.JoinFamily(Family);
                }
                else {
                    if (Family != null)
                        spouse.JoinFamily(Family);
                    else
                        JoinFamily(spouse.Family);
                }
            }

            // onSpouseAdded?.Invoke(spouse);
            // spouse.onSpouseAdded?.Invoke(this);

            onActorIsMarried?.Invoke(this, spouse, true);
            spouse.onActorIsMarried?.Invoke(spouse, this, false);
        }

        /// <summary>
        /// Adds one or more actors as spouses to the current actor.
        /// </summary>
        /// <param name="spouses">An array of Actor objects representing the spouses to be added.</param>
        public void AddSpouses(params Actor[] spouses) {
            foreach (var spouse in _spouses) AddSpouse(spouse);
        }

        /// <summary>
        /// If the specified actor is the spouse of the current actor, they will be divorced.
        /// </summary>
        /// <param name="spouse">The Actor object representing the spouse to be removed.</param>
        public void RemoveSpouse(Actor spouse) {
            if (!Spouses().Contains(spouse) || !spouse.Spouses().Contains(this)) return;
            _spouses.Remove(spouse);
            spouse._spouses.Remove(this);

            onActorDivorced?.Invoke(this, spouse);
            spouse.onActorDivorced?.Invoke(spouse, this);

            if (spouse._spouses.Count < 1) spouse._overrideOrigin = null;

            if (_spouses.Count < 1) _overrideOrigin = null;
        }

        /// <summary>
        /// The actor divorces all of their current spouses.
        /// </summary>
        public void RemoveSpouses() {
            var spouses = _spouses.ToList();
            foreach (var spouse in spouses) RemoveSpouse(spouse);
        }

        #endregion

        #region CLAN

        /// <summary>
        /// Makes the actor join a specified clan.
        /// </summary>
        /// <param name="clan">The Clan object to join.</param>
        public void JoinClan(Clan clan) {
            if (clan == null) return;

            if (clan.Join(this, true)) {
                QuitClan();

                Clan = clan;

                var oldRole = Role;
                Role = null;
                SetRoleWithoutNotification(oldRole);

                onJoinedClan?.Invoke(clan);
                clan.onMemberJoin?.Invoke(this);
            }
        }

        /// <summary>
        /// Makes the actor join a clan based on the given clan name or ID.
        /// </summary>
        /// <param name="clanNameOrId">Name or ID of the clan to join.</param>
        public void JoinClan(string clanNameOrId) =>
            JoinClan(IM.Clans.FirstOrDefault(c => c.ClanName == clanNameOrId || c.ID == clanNameOrId));

        /// <summary>
        /// Leaves the clan if the actor is currently a member.
        /// This method checks if the actor is part of a clan and attempts to leave it.
        /// If successful, triggers the onLeftClan event and updates the actor's clan status.
        /// </summary>
        public void QuitClan() {
            // If the actor is not part of a clan, no further action is needed.
            if (Clan == null) return;

            // Attempt to leave the clan. If successful, notify and update the clan status.
            if (Clan.Quit(this)) {
                // Trigger the onLeftClan event with the current clan before leaving.
                onLeftClan?.Invoke(Clan);

                // Set the clan to null as the actor is no longer a clan member.
                Clan = null;
            }
        }


        /// <summary>
        /// Determines whether the specified policy exists within the actor's clan or family.
        /// </summary>
        /// <param name="policyNameOrId">The name or ID of the policy to check.</param>
        /// <returns>true if the actor's clan or family has the specified policy; otherwise, false.</returns>
        public bool HasPolicy(string policyNameOrId) {
            return !string.IsNullOrEmpty(policyNameOrId) && 
                   ((Clan != null && Clan.HasPolicy(policyNameOrId)) || 
                    (Family != null && Family.HasPolicy(policyNameOrId)));
        }

        #endregion

        #region HEIR

        /// <summary>
        /// Designates a specified actor as the heir to the current actor.
        /// </summary>
        /// <param name="actor">The Actor object to set as the heir.</param>
        public void SetHeir(Actor actor) {
            if (actor == null) {
                _overrideHeir = null;
                return;
            }

            _overrideHeir = actor;
        }

        /// <summary>
        /// Gets the title of the actor based on their role and gender.
        /// </summary>
        /// <remarks>
        /// This property returns the appropriate title depending on the actor's role and gender. 
        /// For example, if the role is 'Leader', it returns 'King' for male actors and 'Queen' for female actors.
        /// </remarks>
        public string Title => Role?.Title(Gender);

        #endregion

        #region PORTRAIT

        /// <summary>
        /// Sets the actor's portrait.
        /// </summary>
        /// <param name="portrait">The Sprite to use as the actor's portrait.</param>
        public void SetPortrait(Sprite portrait) {
            Portrait = portrait;
        }

        #endregion

        #region AGE

        /// <summary>
        /// Sets the actor's age.
        /// </summary>
        /// <param name="age">The desired age to set for the actor.</param>
        public void SetAge(int age) {
            int ageBeforeChanged = Age;
            Age = Mathf.Clamp(age, 0, STATIC.MAX_AGE);
            onAgeChanged?.Invoke(this, ageBeforeChanged);
        }

        #endregion

        #region ROLE

        /// <summary>
        /// Assigns a role to the actor based on the given role name or ID.
        /// </summary>
        /// <param name="roleNameOrId">Name or ID of the desired role.</param>
        public void SetRole(string roleNameOrId) {
            var role = IM.Roles.FirstOrDefault(
                r => r.RoleName == roleNameOrId || r.ID == roleNameOrId);
            if (role == null) return;
            if (Clan != null) {
                var currentSlot = Clan.Members.Count(r => r.Role == role && r.State == IState.Active);
                if (role.Capacity <= currentSlot)
                    return;
            }

            var oldRole = Role;
            Role = role;
            onRoleChanged?.Invoke(oldRole);
            Clan?.onActorRoleChanged?.Invoke(this, oldRole);
        }

        /// <summary>
        /// Assigns a specified role to the actor.
        /// </summary>
        /// <param name="role">The Role object to assign to the actor.</param>
        public void SetRole(Role role) {
            if (role == null) return;
            if (Clan != null) {
                var currentSlot = Clan.Members.Count(r => r.Role == role && r.State == IState.Active);
                if (role.Capacity <= currentSlot)
                    return;
            }

            var oldRole = Role;
            Role = role;
            onRoleChanged?.Invoke(oldRole);
            Clan?.onActorRoleChanged?.Invoke(this, oldRole);
        }

        private void SetRoleWithoutNotification(Role role) {
            if (role == null) return;
            var currentSlot = Clan.Members.Count(r => r.Role == role && r.State == IState.Active);
            if (role.Capacity <= currentSlot)
                return;

            Role = role;
        }

        /// <summary>
        /// Removes any role from the actor, leaving them without a specific role.
        /// </summary>
        public void RoleDismiss() {
            if (Role == null) return;
            var oldRole = Role;
            Role = null;
            onRoleChanged?.Invoke(oldRole);
            Clan?.onActorRoleChanged?.Invoke(this, oldRole);
        }

        /// <summary>
        /// Retires the actor and transfers their inheritable role, such as a leadership position, to their heir.
        /// </summary>
        /// <param name="heir">The heir who will inherit the role. Set to null if no valid heir is found or the role is not inheritable.</param>
        /// <returns>True if the inheritance is successfully transferred; otherwise, false.</returns>
        /// <remarks>
        /// This method first checks if the actor's role is inheritable. If the role is not inheritable or no heir is present, 
        /// it returns false and sets the heir to null. If the conditions are met, the actor's role is relinquished, 
        /// and the heir inherits the role, including joining the same clan and receiving the same responsibilities.
        /// The method also invokes the onInherited event for the heir, signaling the inheritance's completion.
        /// This method is crucial for the game's succession mechanics, ensuring roles like leadership are smoothly transferred.
        /// </remarks>
        public bool RetireAndTransferInheritance(out Actor heir) {
            if (Role is not { Inheritance: true }) {
                heir = null;
                return false;
            }

            if (Heir == null) {
                heir = null;
                return false;
            }

            var role = Role;
            heir = Heir;

            RoleDismiss();

            heir.JoinClan(Clan);
            heir.SetRole(role);
            heir.onInherited?.Invoke(this);
            return true;
        }

        #endregion

        #region Scheme

        /// <summary>
        /// Verifies if a specific scheme is active based on the provided conditions.
        /// </summary>
        /// <param name="schemeNameOrId">Name or ID of the scheme to check.</param>
        /// <param name="type">The type of verification: 
        /// ToTarget checks if the scheme is active against a specific target,
        /// ToAnyone checks if the scheme is active against any target,
        /// FromAnyone checks if someone has activated the scheme against this actor.</param>
        /// <param name="target">Optional. The actor target to check against. Relevant only for ToTarget verification.</param>
        /// <returns>Returns true if the scheme is active based on the provided conditions.</returns>
        public bool SchemeIsActive(string schemeNameOrId, VerifyType type, Actor target = null) {
            bool result = false;

            if (type.HasFlag(VerifyType.ToTarget)) {
                result = _schemes.Exists(
                    s => (s.SchemeName == schemeNameOrId || s.ID == schemeNameOrId) && s.Schemer.Target == target);
            }

            if (type.HasFlag(VerifyType.FromAnyone)) {
                result = _schemesAgainstToActor.Exists(
                    s => s.SchemeName == schemeNameOrId || s.ID == schemeNameOrId);
            }

            if (type.HasFlag(VerifyType.ToAnyone)) {
                result = _schemes.Exists(
                    s => s.SchemeName == schemeNameOrId || s.ID == schemeNameOrId);
            }
            
            if (type.HasFlag(VerifyType.FromTarget)) {
                result = _schemesAgainstToActor.Exists(
                    s => s.SchemeName == schemeNameOrId || s.ID == schemeNameOrId && s.Schemer.Target == target);
            }

            return result;
        }

        /// <summary>
        /// Checks if a specific scheme is active.
        /// </summary>
        /// <param name="schemeNameOrId">Name or ID of the scheme to check.</param>
        /// <returns>Returns true if the specified scheme is active.</returns>
        public bool SchemeIsActive(string schemeNameOrId) =>
            _schemes.Exists(
                s => s.SchemeName == schemeNameOrId || s.ID == schemeNameOrId);

        /// <summary>
        /// Checks if a specific scheme is active against the specified target.
        /// </summary>
        /// <param name="schemeNameOrId">Name or ID of the scheme to check.</param>
        /// <param name="target">The actor target to check against.</param>
        /// <returns>Returns true if the scheme is active against the given target.</returns>
        public bool SchemeIsActive(string schemeNameOrId, Actor target) =>
            _schemes.Exists(
                s => (s.SchemeName == schemeNameOrId || s.ID == schemeNameOrId) && s.Schemer.Target == target);

        /// <summary>
        /// Initiates a scheme against the given target.
        /// </summary>
        /// <param name="schemeNameOrId">Name or ID of the scheme to start.</param>
        /// <param name="target">The actor target for the scheme.</param>
        /// <returns>Returns the initiated Scheme object.</returns>
        public Scheme StartScheme(string schemeNameOrId, Actor target = null) {
            if (target == this) {
                NDebug.Log("An actor cannot plot intrigue against themselves.");
                return null;
            }

            if (SchemeIsActive(schemeNameOrId, VerifyType.ToTarget, target)) {
                NDebug.Log("This scheme is already active.");
                return null;
            }

            var schemeFinded =
                IM.Schemes.FirstOrDefault(s =>
                    s.SchemeName == schemeNameOrId || s.ID == schemeNameOrId);

            if (schemeFinded == null) {
                NDebug.Log("Invalid scheme. Make sure you enter the correct scheme name or ID.");
                return null;
            }

            if (!Ruler.StartGraph(schemeFinded.RuleID, this, target)) {
                return null;
            }

            schemeFinded = schemeFinded.Duplicate();

            _schemes.Add(schemeFinded);

            Schemer.StartGraph(schemeFinded, this, target);

            onSchemeStarted?.Invoke(schemeFinded);
            return schemeFinded;
        }

        /// <summary>
        /// Initiates a scheme against the given target.
        /// </summary>
        /// <param name="scheme">Scheme to Start.</param>
        /// <param name="target">The actor target for the scheme.</param>
        /// <returns>Returns the initiated Scheme object.</returns>
        public Scheme StartScheme(Scheme scheme, Actor target = null) {
            if (scheme == null) {
                NDebug.Log("Invalid scheme.");
                return null;
            }

            if (target == this) {
                NDebug.Log("An actor cannot plot intrigue against themselves.");
                return null;
            }

            if (SchemeIsActive(scheme.ID, VerifyType.ToTarget, target)) {
                NDebug.Log("This scheme is already active.");
                return null;
            }

            if (!Ruler.StartGraph(scheme.RuleID, this, target)) {
                return null;
            }

            scheme = scheme.Duplicate();

            _schemes.Add(scheme);

            Schemer.StartGraph(scheme, this, target);

            onSchemeStarted?.Invoke(scheme);
            return scheme;
        }

        /// <summary>
        /// Retrieves an active scheme against a specific target, if available.
        /// </summary>
        /// <param name="schemeNameOrId">Name or ID of the desired scheme.</param>
        /// <param name="target">The actor target of the scheme.</param>
        /// <returns>If active, returns the corresponding Scheme object for the given target; otherwise, null.</returns>
        public Scheme GetScheme(string schemeNameOrId, Actor target = null) {
            var scheme = _schemes.Find(s =>
                (s.SchemeName == schemeNameOrId || s.ID == schemeNameOrId) && s.Schemer.Target == target);
            return scheme;
        }

        public void SendSchemeEndEvent(Scheme scheme, SchemeResult result) {
            if ( result == SchemeResult.Null ) {
                _schemes.Remove(scheme);
                scheme?.Schemer?.Target?._schemesAgainstToActor?.Remove(scheme);
                return;
            }
            if (_schemes.Remove(scheme))
                onSchemeEnded?.Invoke(scheme, result);
        }

        /// <summary>
        /// Sets the actor's state. 
        /// State.Passive represents a deceased character, 
        /// while State.Active represents a living character.
        /// </summary>
        /// <param name="state">The desired state to set for the actor.</param>
        /// <param name="noninheritor">Indicates whether the actor should leave an inheritance upon death. Default value is false.</param>
        public void SetState(IState state, bool noninheritor = false) {
            var oldState = State;
            State = state;

            if (oldState == State) return;

            if (state == IState.Passive) {
                IM.onActorDeath?.Invoke(this);

                if (noninheritor) {
                    onActorDeath -= OnActorDeath;
                }

                onActorDeath?.Invoke();
                if (noninheritor) {
                    onActorDeath += OnActorDeath;
                }
            }
        }

        /// <summary>
        /// Sets the inheritor status of Actor.
        /// </summary>
        /// <param name="inheritor">If set to <c>true</c>, the Actor can be considered as an inheritor. 
        /// If <c>false</c>, the Actor cannot be an inheritor and is ineligible to claim any inheritance.</param>
        public void SetInheritor(bool inheritor) => Inheritor = inheritor;

        /// <summary>
        /// Lists all the schemes that the actor can initiate against a specific target.
        /// </summary>
        /// <param name="target">The actor target to check against.</param>
        /// <returns>Returns a list of compatible schemes for the given target.</returns>
        public IEnumerable<Scheme> GetCompatibleSchemes(Actor target = null) =>
            IM.GetCompatibleSchemes(this, target);

        #endregion

        #region Variable
        
        public NVar GetRelationVariable(string variableNameOrId, Actor target) {
            if (target == null) return null;
            UpdateRelationVariables(target);
            return relationVariables[target].Find(v => v.name == variableNameOrId || v.id == variableNameOrId);
        }
        
        public T GetRelationVariable<T>(string variableNameOrId, Actor target) where T : NVar {
            if (target == null) return null;
            UpdateRelationVariables(target);
            return relationVariables[target].Find(v => v.name == variableNameOrId || v.id == variableNameOrId) as T;
        }
        
        public void SetRelationVariable(string variableNameOrId, object value, Actor target) {
            if (target == null) return;
            UpdateRelationVariables(target);
            var specificVariable = relationVariables[target].Find(v => v.name == variableNameOrId || v.id == variableNameOrId);
            if (specificVariable == null) return;
            specificVariable.value = value;
        }
        
        public T SetRelationVariable<T>(string variableNameOrId, object value, Actor target) where T : NVar {
            if (target == null) return null;
            UpdateRelationVariables(target);
            var specificVariable = relationVariables[target].Find(v => v.name == variableNameOrId || v.id == variableNameOrId);
            if (specificVariable == null) return null;
            specificVariable.value = value;
            return specificVariable as T;
        }
        
        /// <summary>
        /// Retrieves a private variable from the actor based on its name or ID.
        /// </summary>
        /// <param name="variableNameOrId">Name or ID of the desired variable.</param>
        /// <returns>Returns the corresponding variable from the actor.</returns>
        public NVar GetVariable(string variableNameOrId) => variables.Find(v => v.name == variableNameOrId || v.id == variableNameOrId);

        /// <summary>
        /// Retrieves a private variable from the actor based on its name or ID and casts it to the specified type.
        /// </summary>
        /// <param name="variableNameOrId">Name or ID of the desired variable.</param>
        /// <returns>Returns the corresponding variable of type T from the actor.</returns>
        public T GetVariable<T>(string variableNameOrId) where T : NVar => variables.Find(v => v.name == variableNameOrId || v.id == variableNameOrId) as T;

        /// <summary>
        /// Sets the value of a private variable for the actor.
        /// </summary>
        /// <param name="variableNameOrId">Name or ID of the variable to set.</param>
        /// <param name="value">New value for the actor's variable.</param>
        public void SetVariable(string variableNameOrId, object value) {
            var localVariable = variables.Find(v => v.name == variableNameOrId || v.id == variableNameOrId);
            if (localVariable == null) return;
            localVariable.value = value;
        }

        /// <summary>
        /// Sets the value of a private variable for the actor and returns the updated value cast to the specified type.
        /// </summary>
        /// <param name="variableNameOrId">Name or ID of the variable to set.</param>
        /// <param name="value">New value for the actor's variable.</param>
        /// <returns>Returns the updated variable of type T from the actor.</returns>
        public T SetVariable<T>(string variableNameOrId, object value) where T : NVar {
            var localVariable = variables.Find(v => v.name == variableNameOrId || v.id == variableNameOrId);
            if (localVariable == null) return null;
            localVariable.value = value;
            return localVariable as T;
        }

        #endregion

        #region Culture

        /// <summary>
        /// Changes the culture of the actor based on the given culture name or ID.
        /// </summary>
        /// <param name="cultureNameOrId">Name or ID of the culture to set for the actor.</param>
        public void SetCulture(string cultureNameOrId) {
            var _culture =
                IM.Cultures.FirstOrDefault(c =>
                    c.CultureName == cultureNameOrId || c.ID == cultureNameOrId);
            if (_culture == null) {
                NDebug.Log($"Invalid Culture name/id. / {cultureNameOrId}", NLogType.Error);
                return;
            }

            var oldCulture = Culture;
            Culture = _culture;
            onCultureChanged?.Invoke(oldCulture);
        }
        
        /// <summary>
        /// Changes the culture of the actor based on the given culture name or ID.
        /// </summary>
        /// <param name="culture">The Culture to set for the actor.</param>
        public void SetCulture(Culture culture) {
            if (culture == null) {
                NDebug.Log("Culture cannot be null.", NLogType.Error);
                return;
            }

            var oldCulture = Culture;
            Culture = culture;
            onCultureChanged?.Invoke(oldCulture);
        }

        #endregion

        #region TRIGGER

        /// <summary>
        /// Sends a specific trigger to the actor.
        /// </summary>
        /// <param name="triggerName">Name of the trigger.</param>
        /// <param name="value">Value to be passed with the trigger.</param>
        public void Trigger(string triggerName, bool value) => OnTrigger?.Invoke(triggerName, value);

        #endregion

        #region SAVE SYSTEM

        /// <summary>
        /// Loads actor attributes and data from the provided ActorData. 
        /// Especially used in the Intrigue Save System.
        /// </summary>
        /// <param name="data">The ActorData containing the actor's saved attributes and data.</param>
        public void LoadActor(SaveSystem.ActorData data) {
            if (data == null) return;
            Data = data;
            
            Init();

            UpdateVariables();
            SaveSystem_LoadActor();
            SaveSystem_LoadFamily();
            SaveSystem_LoadClan();
            SaveSystem_SpawnFamilyMember();

            SaveSystem_LoadSchemes();
            SaveSystem_LoadRelationVariables();
        }

        private void SaveSystem_LoadSchemes() {
            _schemes.ForEach(s => {
                if (s.Schemer == null) return;
                s.Schemer.Kill(true);
            });
            foreach (var schemeData in Data.schemeData) {
                var schemeFinded = IM.Schemes.FirstOrDefault(s => s.ID == schemeData.id);
                if (schemeFinded == null) continue;
                if (!string.IsNullOrEmpty(schemeData.targetId) &&
                    !IM.ActorDictionary.ContainsKey(schemeData.targetId)) continue;

                var m_target = string.IsNullOrEmpty(schemeData.targetId)
                    ? null
                    : IM.ActorDictionary[schemeData.targetId];

                schemeFinded = schemeFinded.Duplicate();
                _schemes.Add(schemeFinded);

                Schemer.LoadGraph(schemeFinded, this, m_target, schemeData);

                onSchemeLoaded?.Invoke(schemeFinded);
            }
        }

        private void SaveSystem_LoadVariables() {
            variables.RemoveAll(v => Data.variableData.All(d => d.id != v.id));

            //Load Variables
            foreach (var variableData in Data.variableData) {
                var variable = GetVariable(variableData.id);
                switch (variable) {
                    case NString:
                        variable.value = variableData.stringValue;
                        break;
                    case NInt:
                        variable.value = variableData.integerValue;
                        break;
                    case NBool:
                        variable.value = variableData.boolValue;
                        break;
                    case NFloat:
                        variable.value = variableData.floatValue;
                        break;
                    case NObject:
                        variable.value = variableData.objectValue;
                        break;
                    case NEnum:
                        variable.value = variableData.enumIndex;
                        break;
                }
            }
        }

        private void SaveSystem_LoadRelationVariables() {
            foreach (var specificData in Data.relationVariableData) {
                var target = IM.ActorDictionary[specificData.actorId];
                if(target == null) continue;
                UpdateRelationVariables(target);
                
                //Load Variables
                foreach (var variableData in specificData.varData) {
                    var variable = GetRelationVariable(variableData.id, target);
                    switch (variable) {
                        case NString:
                            variable.value = variableData.stringValue;
                            break;
                        case NInt:
                            variable.value = variableData.integerValue;
                            break;
                        case NBool:
                            variable.value = variableData.boolValue;
                            break;
                        case NFloat:
                            variable.value = variableData.floatValue;
                            break;
                        case NObject:
                            variable.value = variableData.objectValue;
                            break;
                        case NEnum:
                            variable.value = variableData.enumIndex;
                            break;
                    }
                }
            }
        }

        private void SaveSystem_LoadActor() {
            Name = Data.name;
            State = Data.state;
            Age = Data.age;
            Culture = IM.Cultures.FirstOrDefault(c => c.ID == Data.culture);
            Gender = Data.gender;
            Portrait = Data.portrait;
            isPlayer = Data.isPlayer;
            Inheritor = Data.inheritor;
        }

        private void SaveSystem_LoadFamily() {
            var origin = IM.Families.FirstOrDefault(c => c.ID == Data.familyOrigin);
            if (origin != null) {
                _origin = origin;
            }

            var family = IM.Families.FirstOrDefault(c => c.ID == Data.family);
            if (family != null)
                if (family.Join(this, true)) {
                    _overrideOrigin = family;
                }
        }

        private void SaveSystem_LoadClan() {
            var clanObject = IM.Clans.FirstOrDefault(c => c.ID == Data.clan);
            if (clanObject != null) {
                var role = IM.Roles.FirstOrDefault(m => m.ID == Data.role);
                if (role != null) {
                    var currentSlot =
                        clanObject.Members.Count(r => r.Role == role && r.State == IState.Active);
                    if (role.Capacity > currentSlot) Role = role;
                }

                if (clanObject.Join(this, true)) Clan = clanObject;
            }

            if (IM.ActorDictionary.ContainsKey(Data.overrideHeir)) {
                _overrideHeir = IM.ActorDictionary[Data.overrideHeir];
            }
        }

        private void SaveSystem_SpawnFamilyMember() {
            //Parent - Child
            foreach (var parent in Data.parents) {
                onChildSpawned?.Invoke(this, parent);
            }

            foreach (var child in Data.children) {
                if (!IM.ActorDictionary.ContainsKey(child)) continue;
                var managerActor = IM.ActorDictionary[child];
                if (_children.Contains(managerActor)) continue;
                _children.Add(managerActor);
            }

            //Child - Parent

            foreach (var child in Data.children) {
                onParentSpawned?.Invoke(this, child);
            }

            foreach (var parent in Data.parents) {
                if (!IM.ActorDictionary.ContainsKey(parent)) continue;
                var managerActor = IM.ActorDictionary[parent];
                if (_parents.Contains(managerActor)) continue;
                _parents.Add(managerActor);
            }

            //Spouse - Spouse

            foreach (var spouse in Data.spouses) {
                onSpouseSpawned?.Invoke(this, spouse);
            }

            foreach (var spouse in Data.spouses) {
                if (!IM.ActorDictionary.ContainsKey(spouse)) continue;
                var managerActor = IM.ActorDictionary[spouse];
                if (_spouses.Contains(managerActor)) continue;
                _spouses.Add(managerActor);
            }
        }

        #endregion
    }
}