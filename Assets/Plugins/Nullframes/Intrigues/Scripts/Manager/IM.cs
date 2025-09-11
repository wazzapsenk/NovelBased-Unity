using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Nullframes.Intrigues.Graph;
using Nullframes.Intrigues.SaveSystem;
using Nullframes.Intrigues.UI;
using Nullframes.Intrigues.Utils;
using Nullframes.Intrigues.XML;
using Nullframes.Threading;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.Audio;

namespace Nullframes.Intrigues {
    [DefaultExecutionOrder(-400)]
    [ExecuteInEditMode]
    public abstract class IM : MonoBehaviour {
        #region PRIVATE

        private IReadOnlyDictionary< string, string > ITEXT => IEDatabase.localisationTexts[ CurrentLanguage ];
        private List< Clan > clans;
        private List< Family > families;
        private List< NVar > variables;
        private List< Culture > cultures;
        private List< Scheme > schemes;
        private List< Policy > policies;
        private List< Role > roles;
        private Actor player;

        #endregion

        #region SERIALIZEFIELD

        [SerializeField] private IEDatabase ieDatabase;
        [SerializeField] private DialogueManager dialogueManager;
        [SerializeField] private string currentLanguage;
        [SerializeField] private SerializableDictionary< string, Actor > _actors = new();

        #endregion

        #region EVENTS

        /// <summary>
        /// Triggered based on specific conditions, passing the scheme, a string, and a boolean value.
        /// </summary>
        public static event Action< Scheme, string, bool > onTrigger;

        public static event Action< KeyCode, KeyType, float > keyHandler;

        /// <summary>
        /// Triggered when the language setting is changed.
        /// </summary>
        public static event Action< string > onLanguageChanged;

        /// <summary>
        /// Triggered when a signal is received.
        /// </summary>
        public static event Action< Signal > onSignalReceive;

        /// <summary>
        /// Triggered when there's a change in the player's character.
        /// </summary>
        public static event Action< Actor, Actor > onPlayerIsChanged;

        /// <summary>
        /// Triggered upon the death of a player.
        /// </summary>
        public static Action< Actor > onActorDeath;

        /// <summary>
        /// Triggered when a runtime actor is created in the game world.
        /// </summary>
        public static Action< Actor, GameObject > onRuntimeActorCreated;

        #endregion

        #region METHOD_INFO

        private static Dictionary< string, MethodInfo > _schemerInvokeMethods;
        public static IReadOnlyDictionary< string, MethodInfo > SchemerInvokeMethods => _schemerInvokeMethods;

        private static Dictionary< string, MethodInfo > _rulerInvokeMethods;
        public static IReadOnlyDictionary< string, MethodInfo > RulerInvokeMethods => _rulerInvokeMethods;

        private static Dictionary< string, MethodInfo > _schemerGetActorMethods;
        public static IReadOnlyDictionary< string, MethodInfo > SchemerGetActorMethods => _schemerGetActorMethods;

        private static Dictionary< string, MethodInfo > _rulerGetActorMethods;
        public static IReadOnlyDictionary< string, MethodInfo > RulerGetActorMethods => _rulerGetActorMethods;

        private static Dictionary< string, MethodInfo > _schemerDualActorMethods;
        public static IReadOnlyDictionary< string, MethodInfo > SchemerDualActorMethods => _schemerDualActorMethods;

        private static Dictionary< string, MethodInfo > _rulerDualActorMethods;
        public static IReadOnlyDictionary< string, MethodInfo > RulerDualActorMethods => _rulerDualActorMethods;

        private static Dictionary< string, MethodInfo > _schemerGetClanMethods;
        public static IReadOnlyDictionary< string, MethodInfo > SchemerGetClanMethods => _schemerGetClanMethods;

        private static Dictionary< string, MethodInfo > _rulerGetClanMethods;
        public static IReadOnlyDictionary< string, MethodInfo > RulerGetClanMethods => _rulerGetClanMethods;

        private static Dictionary< string, MethodInfo > _schemerGetFamilyMethods;
        public static IReadOnlyDictionary< string, MethodInfo > SchemerGetFamilyMethods => _schemerGetFamilyMethods;

        private static Dictionary< string, MethodInfo > _rulerGetFamilyMethods;
        public static IReadOnlyDictionary< string, MethodInfo > RulerGetFamilyMethods => _rulerGetFamilyMethods;

        public static int LOADED_ATTRIBUTES =>
            _schemerInvokeMethods.Count + _rulerInvokeMethods.Count +
            _schemerGetActorMethods.Count + _rulerGetActorMethods.Count +
            _schemerDualActorMethods.Count + _rulerDualActorMethods.Count +
            _schemerGetClanMethods.Count + _rulerGetClanMethods.Count;

        public static bool IsSynced { get; private set; }

        private static Dictionary< string, MethodInfo > systemMethods;
        public static IReadOnlyDictionary< string, MethodInfo > SystemMethods => systemMethods;

        #endregion


        #region PROPERTIES

        /// <summary>
        /// Provides access to the transform where the IntrigueManager component is attached.
        /// </summary>
        public static Transform Transform => Instance.transform;

        /// <summary>
        /// Provides access to the GameObject where the IntrigueManager component is attached.
        /// </summary>
        public static GameObject GameObject => Instance.gameObject;

        /// <summary>
        /// Checks if the IEDatabase exists.
        /// </summary>
        public static bool DatabaseExists => instance.ieDatabase != null;

        /// <summary>
        /// Provides access to the dictionary of actors.
        /// </summary>
        public static IReadOnlyDictionary< string, Actor > ActorDictionary => Instance._actors;

        /// <summary>
        /// Provides access to the collection of actor values.
        /// </summary>
        public static IEnumerable< Actor > Actors => Instance._actors.Values;

        /// <summary>
        /// Gets the currently active language setting.
        /// </summary>
        public static string CurrentLanguage => Instance.currentLanguage;

        /// <summary>
        /// Accessor for the database.
        /// </summary>
        public static IEDatabase IEDatabase => Instance.ieDatabase;

        /// <summary>
        /// Accessor for the dialogue manager.
        /// </summary>
        public static DialogueManager DialogueManager => Instance.dialogueManager;

        /// <summary>
        /// Provides access to the collection of clans.
        /// </summary>
        public static IEnumerable< Clan > Clans => Instance.clans;

        /// <summary>
        /// Provides access to the collection of schemes.
        /// </summary>
        public static IEnumerable< Scheme > Schemes => Instance.schemes;

        /// <summary>
        /// Provides access to the collection of families.
        /// </summary>
        public static IEnumerable< Family > Families => Instance.families;

        /// <summary>
        /// Provides access to the collection of game variables.
        /// </summary>
        public static IEnumerable< NVar > Variables => Instance.variables;

        /// <summary>
        /// Provides access to the collection of cultures.
        /// </summary>
        public static IEnumerable< Culture > Cultures => Instance.cultures;

        /// <summary>
        /// Provides access to the collection of policies.
        /// </summary>
        public static IEnumerable< Policy > Policies => Instance.policies;

        /// <summary>
        /// Provides access to the collection of roles.
        /// </summary>
        public static IEnumerable< Role > Roles => Instance.roles;

        /// <summary>
        /// Gets the current player actor.
        /// </summary>
        public static Actor Player => Instance.player;

        #endregion

        #region INSTANCE

        private static IntrigueManager instance;

        public BatchMatchEngine MatchEngine { get; private set; }

        public static IntrigueManager Instance =>
            instance != null ? instance : throw new ArgumentException(STATIC.MANAGER_NOT_EXISTS);

        public static bool Exists => instance != null;

        #endregion

        #region RESOURCES

        public static Sprite Approval_Icon;
        public static Sprite Cancel_Icon;

        private Dictionary< string, AudioSource > playlist;

        #endregion

        #region METHODS

        private static readonly List< KeyCode > keyBuffer = new();

        private void Update() {
#if UNITY_EDITOR
            if ( PrefabStageUtility.GetPrefabStage(gameObject) != null ) return;

            instance ??= (IntrigueManager)this;
#endif

            if ( !Application.isPlaying ) return;
            OnUpdate();

            #region KEY_DOWN

            keyBuffer.Clear();
            NullUtils.GetCurrentKeysDown(keyBuffer);
            for ( int i = 0; i < keyBuffer.Count; i++ )
                keyHandler?.Invoke(keyBuffer[ i ], KeyType.Down, 0f);

            #endregion

            #region KEY_UP

            keyBuffer.Clear();
            NullUtils.GetCurrentKeysUp(keyBuffer);
            for ( int i = 0; i < keyBuffer.Count; i++ )
                keyHandler?.Invoke(keyBuffer[ i ], KeyType.Up, 0f);

            #endregion
        }

        private void Awake() {
#if UNITY_EDITOR
            if ( PrefabStageUtility.GetPrefabStage(gameObject) != null ) return;
#endif

            if ( instance == null ) {
                instance = (IntrigueManager)this;
                if ( Application.isPlaying ) DontDestroyOnLoad(this);
            } else {
                if ( Application.isPlaying ) {
                    Destroy(gameObject);
                } else {
                    DestroyImmediate(gameObject);
                    NDebug.Log(STATIC.CONTAINS_MANAGER, NLogType.Error);
                }

                return;
            }

            if ( !Application.isPlaying ) return;

            if ( !Exists ) {
                enabled = false;
                return;
            }

            Init();
            LoadSystemMethods();
            LoadAttributes();
            XMLUtils.Init(IEDatabase);
            LoadResources();
            LoadVariables();
            LoadCultures();
            LoadSchemes();
            LoadPolicies();
            LoadRoles();
            LoadFamilies();
            LoadClans();
            LoadDialogueManager();

            OnAwake();
        }

        private void Start() {
            if ( !Application.isPlaying ) return;
            OnStart();
        }

        private void LoadVariables() {
            foreach ( var variable in ieDatabase.variablePool ) {
                variables.Add(variable.Duplicate());
            }
        }

        private void LoadCultures() {
            foreach ( var culture in ieDatabase.culturalProfiles ) {
                cultures.Add(culture.Duplicate());
            }
        }

        private void LoadSchemes() {
            foreach ( var scheme in ieDatabase.schemeLibrary ) {
                schemes.Add(scheme.Duplicate());
            }
        }

        private void LoadPolicies() {
            foreach ( var policy in ieDatabase.policyCatalog ) {
                policies.Add(policy.Duplicate());
            }
        }

        private void LoadRoles() {
            foreach ( var role in ieDatabase.roleDefinitions ) {
                var duplicatedRole = role.Duplicate();
                roles.Add(duplicatedRole);

                duplicatedRole.HeirFilter = HeirFilter(duplicatedRole.FilterID);
            }
        }

        public static HeirFilter HeirFilter(string filterNameOrId) {
            var filter = IEDatabase.nodeDataList.OfType< HeirFilterData >()
                .FirstOrDefault(n => n.FilterName == filterNameOrId || n.ID == filterNameOrId);
            if ( filter == null ) return null;

            var newFilter = new HeirFilter();

            var gender = (GenderFilter)filter.Gender;
            switch ( gender ) {
                case GenderFilter.Female:
                    newFilter.FilterAbsolute += (_, heir) => heir.Gender == Actor.IGender.Female;
                    break;
                case GenderFilter.Male:
                    newFilter.FilterAbsolute += (_, heir) => heir.Gender == Actor.IGender.Male;
                    break;
                case GenderFilter.FemaleMale:
                    newFilter.OrderFilter += (_, heir) =>
                        heir.Gender is Actor.IGender.Female;
                    newFilter.OrderFilter += (_, heir) =>
                        heir.Gender is Actor.IGender.Male;
                    break;
                case GenderFilter.MaleFemale:
                    newFilter.OrderFilter += (_, heir) =>
                        heir.Gender is Actor.IGender.Male;
                    newFilter.OrderFilter += (_, heir) =>
                        heir.Gender is Actor.IGender.Female;
                    break;
            }

            var sameClan = filter.Clan;

            if ( sameClan == 1 ) {
                newFilter.FilterAbsolute += (decedent, heir) => decedent.Clan == heir.Clan;
            }

            var age = (AgeFilter)filter.Age;
            switch ( age ) {
                case AgeFilter.Oldest:
                    newFilter.OrderByDesc += (_, heir) => heir.Age;
                    break;
                case AgeFilter.Youngest:
                    newFilter.OrderBy += (_, heir) => heir.Age;
                    break;
            }

            foreach ( var relative in filter.Relatives.Select(relativeId => (RelativeFilter)relativeId) ) {
                switch ( relative ) {
                    case RelativeFilter.Child:
                        newFilter.Filter += (decedent, heir) => decedent.IsChild(heir);
                        break;
                    case RelativeFilter.Parent:
                        newFilter.Filter += (decedent, heir) => decedent.IsParent(heir);
                        break;
                    case RelativeFilter.Sibling:
                        newFilter.Filter += (decedent, heir) => decedent.IsSibling(heir);
                        break;
                    case RelativeFilter.Spouse:
                        newFilter.Filter += (decedent, heir) => decedent.IsSpouse(heir);
                        break;
                    case RelativeFilter.Uncle:
                        newFilter.Filter += (decedent, heir) => decedent.IsUncle(heir);
                        break;
                    case RelativeFilter.Aunt:
                        newFilter.Filter += (decedent, heir) => decedent.IsAunt(heir);
                        break;
                    case RelativeFilter.Grandparent:
                        newFilter.Filter += (decedent, heir) => decedent.IsGrandparent(heir);
                        break;
                    case RelativeFilter.Grandchild:
                        newFilter.Filter += (decedent, heir) => decedent.IsGrandchild(heir);
                        break;
                    case RelativeFilter.Nephew:
                        newFilter.Filter += (decedent, heir) => decedent.IsNephew(heir);
                        break;
                    case RelativeFilter.Niece:
                        newFilter.Filter += (decedent, heir) => decedent.IsNiece(heir);
                        break;
                    case RelativeFilter.BrotherInLaw:
                        newFilter.Filter += (decedent, heir) => decedent.IsBrotherInLaw(heir);
                        break;
                    case RelativeFilter.SisterInLaw:
                        newFilter.Filter += (decedent, heir) => decedent.IsSisterInLaw(heir);
                        break;
                }
            }

            return newFilter;
        }

        private void LoadResources() {
            Approval_Icon = Resources.Load< Sprite >("Nullframes/approval");
            Cancel_Icon = Resources.Load< Sprite >("Nullframes/cancel");
        }

        private void LoadFamilies() {
            FamilyFlow.LoadActors();

            families = new List< Family >();

            var varList = variables.Select(varr => varr.Duplicate()).ToList();

            foreach ( var family in FamilyFlow.Families.Keys ) {
                // CONTINUE
                var policyList = Policies.Where(policy =>
                        policy.Type is PolicyType.Family or PolicyType.Generic && family.Policies.Contains(policy.ID))
                    .ToList();

                families.Add(new Family(family.ID, family.Title, family.Story, family.Emblem,
                    Cultures.FirstOrDefault(c => c.ID == family.CultureID), policyList, varList));
            }
        }

        private void LoadClans() {
            ClanFlow.LoadClans();

            clans = new List< Clan >();

            var varList = variables.Select(varr => varr.Duplicate()).ToList();

            foreach ( var clan in ClanFlow.Clans.Keys ) {
                var policyList = Policies.Where(policy =>
                    policy.Type is PolicyType.Clan or PolicyType.Generic && clan.Policies.Contains(policy.ID)).ToList();

                clans.Add(new Clan(clan.ID, clan.Title, clan.Story, clan.Emblem,
                    Cultures.FirstOrDefault(c => c.ID == clan.CultureID), policyList, varList));
            }
        }

        public static void AddFamily(Family family) {
            if ( Instance.families.Contains(family) ) return;
            Instance.families.Add(family);
        }

        public static void AddClan(Clan clan) {
            if ( Instance.clans.Contains(clan) ) return;
            Instance.clans.Add(clan);
        }

        public static void AddRole(Role role) {
            if ( Instance.roles.Contains(role) ) return;
            Instance.roles.Add(role);
        }

        public static void AddCulture(Culture culture) {
            if ( Instance.cultures.Contains(culture) ) return;
            Instance.cultures.Add(culture);
        }

        public static void AddPolicy(Policy policy) {
            if ( Instance.policies.Contains(policy) ) return;
            Instance.policies.Add(policy);
        }

        /// <summary>
        /// Changes the current language based on the provided language key.
        /// </summary>
        /// <param name="languageKey">Key representing the language to switch to.</param>
        public static void ChangeLanguage(string languageKey) {
            if ( CurrentLanguage == languageKey )
                return;
            if ( IEDatabase.localisationTexts.ContainsKey(languageKey) ) {
                Instance.currentLanguage = languageKey;
                onLanguageChanged?.Invoke(languageKey);
                return;
            }

            NDebug.Log(string.Format(STATIC.DEBUG_INVALID_LANGUAGE_KEY, languageKey), NLogType.Error);
        }

        private static string getText(string key) {
            if ( Instance.ITEXT.ContainsKey(key) )
                return Variables != null
                    ? Variables.Aggregate(Instance.ITEXT[ key ],
                        (current, variable) => current.BracesIn(variable.name))
                    : Instance.ITEXT[ key ];
            NDebug.Log(string.Format(STATIC.INVALID_LOCALISATION_KEY, key), NLogType.Error);
            return $"{{null:{key}}}";
        }

        /// <summary>
        /// Retrieves the corresponding string for the given key in the current language.
        /// </summary>
        /// <param name="key">The key associated with the desired string.</param>
        /// <returns>Returns the corresponding string in the current language.</returns>
        public static string GetText(string key) {
#if UNITY_EDITOR
            if ( !Application.isPlaying ) return getText(key);
#endif
            var text = XMLUtils.GetKey(IEDatabase, key, CurrentLanguage);
            if ( !string.IsNullOrEmpty(text) )
                return Variables != null
                    ? Variables.Aggregate(text, (current, variable) => current.BracesIn(variable.name))
                    : text;
            return getText(key);
        }

        private void LoadDialogueManager() {
            _ = dialogueManager ?? throw new ArgumentException(STATIC.DEBUG_DIALOGUE_MANAGER_NOT_FOUND);
            DialogueManager.name = "Dialogue Canvas";
        }

        private void Init() {
            MatchEngine ??= new BatchMatchEngine();

            _actors = new SerializableDictionary< string, Actor >();
            clans = new List< Clan >();
            families = new List< Family >();
            variables = new List< NVar >();
            cultures = new List< Culture >();
            schemes = new List< Scheme >();
            policies = new List< Policy >();
            roles = new List< Role >();
            playlist = new Dictionary< string, AudioSource >();
            
            _schemerInvokeMethods = new Dictionary< string, MethodInfo >();
            _rulerInvokeMethods = new Dictionary< string, MethodInfo >();
            
            _schemerGetActorMethods = new Dictionary< string, MethodInfo >();
            _rulerGetActorMethods = new Dictionary< string, MethodInfo >();
            
            _schemerDualActorMethods = new Dictionary< string, MethodInfo >();
            _rulerDualActorMethods = new Dictionary< string, MethodInfo >();
            
            _schemerGetClanMethods = new Dictionary< string, MethodInfo >();
            _rulerGetClanMethods = new Dictionary< string, MethodInfo >();
            
            _schemerGetFamilyMethods = new Dictionary< string, MethodInfo >();
            _rulerGetFamilyMethods = new Dictionary< string, MethodInfo >();
            
            systemMethods = new Dictionary< string, MethodInfo >();
        }

        private static void LoadAttributes() {
            IsSynced = false;

            var thread = new System.Threading.Thread(LoadAssemblies) {
                Name = "Nullframes_Intrigues"
            };
            thread.Start();

            NDebug.Log(STATIC.DEBUG_LOADING_METHODS);

            void LoadAssemblies() {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach ( var assembly in assemblies ) {
                    var types = assembly
                        .GetTypes()
                        .Where(t => t.IsClass && t.IsSubclassOf(typeof( MonoBehaviour )))
                        .ToList();

                    foreach ( var method in types.SelectMany(t =>
                                 t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) ) {
                        if ( !method.IsDefined(typeof( IInvokeAttribute )) || method.ReturnType != typeof( IResult ) ) {
                            HandleAttributeMethod(method);
                            continue;
                        }

                        var parameters = method.GetParameters();

                        if ( parameters.Length == 1 && parameters[ 0 ].ParameterType == typeof( Scheme ) ) {
                            RegisterInvokeMethod(method, _schemerInvokeMethods, SchemerInvokeMethods);
                        } else if ( parameters.Length == 2 && parameters[ 0 ].ParameterType == typeof( Actor ) &&
                                    parameters[ 1 ].ParameterType == typeof( Actor ) ) {
                            RegisterInvokeMethod(method, _rulerInvokeMethods, RulerInvokeMethods);
                        }
                    }
                }

                IsSynced = true;
                NDebug.Log(string.Format(STATIC.DEBUG_LOADED_ATTRIBUTES, LOADED_ATTRIBUTES), true);
            }
        }

        private static void HandleAttributeMethod(MethodInfo method) {
            _ = method switch {
                _ when TryRegisterMethod<Actor, GetActorAttribute>(_schemerGetActorMethods, method, typeof(Scheme)) => true,
                _ when TryRegisterMethod<Actor, GetActorAttribute>(_rulerGetActorMethods, method, typeof(Actor), typeof(Actor)) => true,
                _ when TryRegisterMethod<(Actor, Actor), GetDualActorAttribute>(_schemerDualActorMethods, method, typeof(Scheme)) => true,
                _ when TryRegisterMethod<(Actor, Actor), GetDualActorAttribute>(_rulerDualActorMethods, method, typeof(Actor), typeof(Actor)) => true,
                _ when TryRegisterMethod<Clan, GetClanAttribute>(_schemerGetClanMethods, method, typeof(Scheme)) => true,
                _ when TryRegisterMethod<Clan, GetClanAttribute>(_rulerGetClanMethods, method, typeof(Actor), typeof(Actor)) => true,
                _ when TryRegisterMethod<Family, GetFamilyAttribute>(_schemerGetFamilyMethods, method, typeof(Scheme)) => true,
                _ => TryRegisterMethod<Family, GetFamilyAttribute>(_rulerGetFamilyMethods, method, typeof(Actor), typeof(Actor))
            };
        }

        private static bool TryRegisterMethod< TReturn, TAttribute >(Dictionary< string, MethodInfo > dict,
            MethodInfo method, params Type[ ] expectedParams)
            where TAttribute : Attribute {
            if ( !method.IsDefined(typeof( TAttribute )) && method.ReturnType != typeof( TReturn ) )
                return false;

            var parameters = method.GetParameters();
            if ( parameters.Length != expectedParams.Length ||
                 !parameters.Select((p, i) => p.ParameterType == expectedParams[ i ]).All(b => b) )
                return false;

            var attribute = method.GetCustomAttribute(typeof(TAttribute)) as INamedAttribute;
            var methodName = string.IsNullOrEmpty(attribute?.Name) ? method.Name : attribute.Name;

            if ( !dict.TryAdd(methodName, method) ) {
                Debug.LogError(
                    $"There is a method with this name(\"{methodName}\"). Please change the method name or customize the name using the attribute name parameter. [{typeof( TAttribute ).Name.Replace("Attribute", "")}(\"Name\")]\nMethod: {method.Name}");
                return false;
            }

            return true;
        }

        private static void RegisterInvokeMethod(MethodInfo method, Dictionary< string, MethodInfo > targetDict,
            IReadOnlyDictionary< string, MethodInfo > publicDict) {
            var attribute = method.GetCustomAttribute(typeof( IInvokeAttribute )) as IInvokeAttribute;
            var name = string.IsNullOrEmpty(attribute?.Name) ? method.Name : attribute.Name;

            if ( publicDict.ContainsKey(name) ) {
                Debug.LogError(
                    $"There is a method with this name(\"{name}\"). Please change the method name or customize the name using the attribute name parameter. [Scheme(\"Name\")]\nMethod: {method.Name}");
                return;
            }

            targetDict.Add(name, method);
        }

        private void LoadSystemMethods() {
            var schemeType = typeof( Scheme );

            foreach ( var methodName in new[ ] { "Scheme_Init", "Scheme_SetObjective" } ) {
                var method = schemeType.GetMethod(methodName,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if ( method != null ) {
                    systemMethods.Add(method.Name, method);
                }
            }
        }

        public static void AddActor(Actor actor) {
            if ( ActorDictionary.ContainsKey(actor.ID) ) return;

            Instance._actors.Add(actor.ID, actor);
            if ( Player == null && actor.IsPlayer ) {
                instance.player = actor;
            }
        }

        public static void DestroyActor(Actor actor) {
            if ( !Exists ) return;
            Instance._actors.Remove(actor.ID);
        }

        /// <summary>
        /// Sets the current player to the given actor.
        /// </summary>
        /// <param name="actor">Actor to set as the current player.</param>
        public static void SetPlayer(Actor actor) {
            var oldPlayer = Player;
            instance.player = actor;

            if ( oldPlayer != actor )
                onPlayerIsChanged?.Invoke(oldPlayer, actor);
        }

        /// <summary>
        /// Invokes the onTrigger event.
        /// </summary>
        /// <param name="scheme">Related scheme.</param>
        /// <param name="triggerName">Name of the trigger.</param>
        /// <param name="value">Value to pass to the trigger.</param>
        public static void Trigger(Scheme scheme, string triggerName, bool value = true) {
            onTrigger?.Invoke(scheme, triggerName, value);
        }

        /// <summary>
        /// Calls a method that is defined with a [Scheme] attribute.
        /// </summary>
        /// <param name="scheme">Related scheme.</param>
        /// <param name="invokeName">Name of the method to invoke.</param>
        public static IResult Invoke(Scheme scheme, string invokeName) {
            return scheme.Invoke(invokeName);
        }

        /// <summary>
        /// Checks if a given actor is the player.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <returns>Returns true if the provided actor is the player.</returns>
        public static bool IsPlayer(Actor actor) => Player == actor;

        protected abstract void OnAwake();
        protected abstract void OnStart();
        protected abstract void OnUpdate();

        #endregion

        #region VARIABLE_SYSTEM

        /// <summary>
        /// Retrieves the public variable associated with the given name or ID.
        /// </summary>
        /// <param name="variableNameOrId">Name or ID of the desired variable.</param>
        /// <returns>Returns the corresponding variable.</returns>
        public static NVar GetVariable(string variableNameOrId) {
            var variable = Instance.variables.Find(v => v.name == variableNameOrId || v.id == variableNameOrId);
            return variable ?? new NString($"{{null:{variableNameOrId}}}");
        }

        /// <summary>
        /// Retrieves the public variable associated with the given name or ID, cast to the specified type.
        /// </summary>
        /// <param name="variableNameOrId">Name or ID of the desired variable.</param>
        /// <returns>Returns the corresponding variable of type T.</returns>
        public static T GetVariable< T >(string variableNameOrId) where T : NVar {
            var variable = Instance.variables.Find(v => v.name == variableNameOrId || v.id == variableNameOrId) as T;
            return variable ?? new NString($"{{null:{variableNameOrId}}}") as T;
        }

        /// <summary>
        /// Sets the value of a public variable.
        /// </summary>
        /// <param name="varNameOrId">Name or ID of the variable to set.</param>
        /// <param name="value">New value for the variable.</param>
        public static void SetVariable(string varNameOrId, object value) {
            var variable = Variables.FirstOrDefault(v => v.name == varNameOrId || v.id == varNameOrId);
            if ( variable == null ) return;
            variable.value = value;
        }

        /// <summary>
        /// Sets the value of a public variable, returning the updated value cast to the specified type.
        /// </summary>
        /// <param name="varNameOrId">Name or ID of the variable to set.</param>
        /// <param name="value">New value for the variable.</param>
        /// <returns>Returns the updated variable of type T.</returns>
        public static T SetVariable< T >(string varNameOrId, object value) where T : NVar {
            var variable = Variables.FirstOrDefault(v => v.name == varNameOrId || v.id == varNameOrId);
            if ( variable == null ) return null;
            variable.value = value;
            return variable as T;
        }

        // /// <summary>
        // /// Resets all global variables to their initial states.
        // /// </summary>
        public static void Load(SaveData saveData) {
            DialogueManager.Close(true, true);

            Instance._actors = new SerializableDictionary< string, Actor >();
            Instance.clans = new List< Clan >();
            Instance.families = new List< Family >();
            Instance.variables = new List< NVar >();
            Instance.cultures = new List< Culture >();
            Instance.schemes = new List< Scheme >();
            Instance.policies = new List< Policy >();
            Instance.roles = new List< Role >();
            Instance.playlist = new Dictionary< string, AudioSource >();

            if ( saveData != null ) {
                foreach ( var variableData in saveData.gameData.variableData ) {
                    switch ( variableData.type ) {
                        case NType.String:
                            instance.variables.Add(NVar.Create(variableData.id, variableData.name,
                                variableData.stringValue, variableData.type));
                            break;
                        case NType.Integer:
                            instance.variables.Add(NVar.Create(variableData.id, variableData.name,
                                variableData.integerValue, variableData.type));
                            break;
                        case NType.Bool:
                            instance.variables.Add(NVar.Create(variableData.id, variableData.name,
                                variableData.boolValue, variableData.type));
                            break;
                        case NType.Float:
                            instance.variables.Add(NVar.Create(variableData.id, variableData.name,
                                variableData.floatValue, variableData.type));
                            break;
                        case NType.Object:
                            instance.variables.Add(NVar.Create(variableData.id, variableData.name,
                                variableData.objectValue, variableData.type));
                            break;
                        case NType.Enum:
                            instance.variables.Add(NVar.Create(variableData.id, variableData.name,
                                variableData.enumIndex, variableData.type));
                            break;
                    }
                }

                foreach ( var cultureData in saveData.gameData.cultureData ) {
                    instance.cultures.Add(new Culture(cultureData.id, cultureData.name, cultureData.description,
                        cultureData.icon, cultureData.femaleNameList, cultureData.maleNameList));
                }

                foreach ( var policyData in saveData.gameData.policyData ) {
                    instance.policies.Add(new Policy(policyData.id, policyData.name, policyData.description,
                        policyData.type, policyData.icon));
                }

                foreach ( var roleData in saveData.gameData.roleData ) {
                    var role = new Role(roleData.id, roleData.name, roleData.titleForMale, roleData.titleForFemale,
                        roleData.description,
                        roleData.heirFilterId, roleData.capacity, roleData.inheritance, roleData.icon,
                        roleData.priority);

                    instance.roles.Add(role);
                    role.HeirFilter = HeirFilter(roleData.heirFilterId);
                }

                foreach ( var familyData in saveData.gameData.familyData ) {
                    // CONTINUE
                    var policyList = familyData.policies
                        .Select(policyId => Policies.FirstOrDefault(p =>
                            p.Type is PolicyType.Family or PolicyType.Generic && p.ID == policyId))
                        .Where(policy => policy != null).ToList();

                    var vars = instance.variables.Select(varr => varr.Duplicate()).ToList();

                    //Load Variables
                    foreach ( var variableData in familyData.variableData ) {
                        var variable = vars.FirstOrDefault(v => v.id == variableData.id);
                        if ( variable == null ) continue;
                        switch ( variable ) {
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

                    instance.families.Add(new Family(familyData.id, familyData.name, familyData.description,
                        familyData.icon, Cultures.FirstOrDefault(c => c.ID == familyData.cultureId), policyList, vars));
                }

                foreach ( var clanData in saveData.gameData.clanData ) {
                    var policyList = clanData.policies
                        .Select(policyId => Policies.FirstOrDefault(p =>
                            p.Type is PolicyType.Clan or PolicyType.Generic && p.ID == policyId))
                        .Where(policy => policy != null).ToList();

                    var vars = instance.variables.Select(varr => varr.Duplicate()).ToList();

                    //Load Variables
                    foreach ( var variableData in clanData.variableData ) {
                        var variable = vars.FirstOrDefault(v => v.id == variableData.id);
                        if ( variable == null ) continue;
                        switch ( variable ) {
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

                    instance.clans.Add(new Clan(clanData.id, clanData.name, clanData.description, clanData.icon,
                        Cultures.FirstOrDefault(c => c.ID == clanData.cultureId), policyList, vars));
                }

                // Load Schemes
                Instance.LoadSchemes();

                return;
            }

            Instance.LoadVariables();
            Instance.LoadCultures();
            Instance.LoadSchemes();
            Instance.LoadPolicies();
            Instance.LoadRoles();
            Instance.LoadFamilies();
            Instance.LoadClans();
        }

        #endregion

        #region CLAN

        /// <summary>
        /// Retrieves the clan associated with the given name or ID.
        /// </summary>
        /// <param name="clanNameOrID">Name or ID of the desired clan.</param>
        /// <returns>Returns the corresponding Clan object.</returns>
        public static Clan GetClan(string clanNameOrID) {
            return Clans.FirstOrDefault(c => c.ClanName == clanNameOrID || c.ID == clanNameOrID);
        }

        /// <summary>
        /// Retrieves the role associated with the given name or ID.
        /// </summary>
        /// <param name="roleNameOrId">Name or ID of the desired role.</param>
        /// <returns>Returns the corresponding Role object.</returns>
        public static Role GetRole(string roleNameOrId) {
            return Roles.FirstOrDefault(c => c.RoleName == roleNameOrId || c.ID == roleNameOrId);
        }

        /// <summary>
        /// Retrieves the policy associated with the given name or ID.
        /// </summary>
        /// <param name="policyNameOrId">Name or ID of the desired policy.</param>
        /// <returns>Returns the corresponding Policy object.</returns>
        public static Policy GetPolicy(string policyNameOrId) {
            return Policies.FirstOrDefault(c => c.PolicyName == policyNameOrId || c.ID == policyNameOrId);
        }

        /// <summary>
        /// Retrieves the family associated with the given name or ID.
        /// </summary>
        /// <param name="familyNameOrId">Name or ID of the desired family.</param>
        /// <returns>Returns the corresponding Family object.</returns>
        public static Family GetFamily(string familyNameOrId) {
            return Families.FirstOrDefault(c => c.FamilyName == familyNameOrId || c.ID == familyNameOrId);
        }

        /// <summary>
        /// Retrieves the culture associated with the given name or ID.
        /// </summary>
        /// <param name="cultureNameOrId">Name or ID of the desired culture.</param>
        /// <returns>Returns the corresponding Culture object.</returns>
        public static Culture GetCulture(string cultureNameOrId) {
            return Cultures.FirstOrDefault(c => c.CultureName == cultureNameOrId || c.ID == cultureNameOrId);
        }

        #endregion

        #region SCHEME

        /// <summary>
        /// Lists the schemes that a conspirator can initiate against a target.
        /// </summary>
        /// <param name="conspirator">The actor initiating the scheme.</param>
        /// <param name="target">The target actor of the scheme.</param>
        /// <returns>Returns a list of compatible schemes.</returns>
        public static IEnumerable< Scheme > GetCompatibleSchemes(Actor conspirator, Actor target = null) =>
            ( from scheme in Schemes
                where !conspirator.SchemeIsActive(scheme.SchemeName, target)
                where Schemes.Any(e => e.ID == scheme.ID)
                where Ruler.StartGraph(scheme.RuleID, conspirator, target)
                select scheme ).ToList();

        /// <summary>
        /// Checks if a conspirator meets a specific rule against a target.
        /// </summary>
        /// <param name="ruleNameOrId">Name or ID of the rule to check against.</param>
        /// <param name="conspirator">The actor initiating the scheme.</param>
        /// <param name="target">The target actor of the scheme.</param>
        /// <returns>Returns true if the conspirator satisfies the rule against the target.</returns>
        public static RuleResult IsCompatible(string ruleNameOrId, Actor conspirator, Actor target = null) =>
            Ruler.StartGraph(ruleNameOrId, conspirator, target);

        public static Task< RuleResult >
            IsCompatibleAsync(string ruleNameOrId, Actor conspirator, Actor target = null) =>
            Ruler.StartGraphAsync(ruleNameOrId, conspirator, target);

        #endregion

        #region AUDIO_SYSTEM

        public static AudioSource SetupAudio(string audioid = null, AudioClip clip = null, float volume = 1f,
            float destroyTime = 0f, bool autoPlay = false, AudioMixerGroup audioMixerGroup = null) {
            if ( clip == null ) return null;

            if ( !string.IsNullOrEmpty(audioid) ) {
                if ( instance.playlist.ContainsKey(audioid) ) return null;
            }

            var audioObject = new GameObject("Sound");
            var audioSource = audioObject.AddComponent< AudioSource >();
            audioSource.outputAudioMixerGroup = audioMixerGroup;
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioObject.transform.SetParent(Instance.transform);

            if ( !string.IsNullOrEmpty(audioid) ) {
                instance.playlist.Add(audioid, audioSource);
            }

            if ( autoPlay ) {
                audioSource.Play();
            }

            if ( destroyTime == 0f ) {
                NullUtils.DelayedCall(new DelayedCallParams {
                    WaitUntil = () => audioObject == null || audioSource.isPlaying,
                    Call = () => {
                        NullUtils.DelayedCall(new DelayedCallParams {
                            WaitUntil =
                                () => audioObject == null || ( !audioSource.isPlaying && audioSource.time == 0 ),
                            Call = () => {
                                if ( audioSource != null )
                                    Destroy(audioObject.gameObject);

                                if ( !string.IsNullOrEmpty(audioid) )
                                    instance.playlist.Remove(audioid);
                            }
                        });
                    }
                });
                // NullUtils.DelayedCall( () => audioObject == null || audioSource.isPlaying,
                //     () => {
                //         NullUtils.DelayedCall(() => audioObject == null || (!audioSource.isPlaying && audioSource.time == 0), () => {
                //             if(audioSource != null)
                //                 Destroy(audioObject.gameObject);
                //             
                //             if (!string.IsNullOrEmpty(audioid))
                //                 instance.playlist.Remove(audioid);
                //         });
                //     });
            } else {
                NullUtils.DelayedCall(new DelayedCallParams {
                    Delay = destroyTime,
                    Call = () => {
                        if ( audioObject != null )
                            Destroy(audioObject.gameObject);

                        if ( !string.IsNullOrEmpty(audioid) )
                            instance.playlist.Remove(audioid);
                    }
                });
                // NullUtils.DelayedCall(destroyTime,
                //     () => {
                //         if(audioObject != null)
                //             Destroy(audioObject.gameObject);
                //         
                //         if (!string.IsNullOrEmpty(audioid))
                //             instance.playlist.Remove(audioid);
                //     });
            }

            return audioSource;
        }

        public static void RemoveAudio(string audioid, bool dontDestroyAudio = false) {
            if ( instance.playlist.ContainsKey(audioid) ) {
                if ( instance.playlist[ audioid ] != null ) {
                    if ( !dontDestroyAudio ) {
                        Destroy(instance.playlist[ audioid ].gameObject);
                    }
                }
            }

            instance.playlist.Remove(audioid);
        }

        private static Dictionary< AudioSource, Coroutine > fadeList = new();

        public static AudioSource SetupAudio(AudioClip clip = null, float volume = 1f, float destroyTime = 0f,
            bool autoPlay = false) {
            return SetupAudio(null, clip, volume, destroyTime, autoPlay);
        }

        public static void AudioFade(AudioSource audioSource, float duration, float startVolume, float targetVolume,
            Action onComplete = null) {
            if ( fadeList.ContainsKey(audioSource) ) {
                CoroutineManager.StopRoutine(fadeList[ audioSource ]);
                fadeList.Remove(audioSource);
            }

            fadeList.Add(audioSource,
                CoroutineManager.StartRoutine(StartFade(audioSource, duration, startVolume, targetVolume, onComplete)));
        }

        private static IEnumerator StartFade(AudioSource audioSource, float duration, float startVolume,
            float targetVolume, Action onComplete = null) {
            float currentTime = 0;
            audioSource.volume = startVolume;
            while ( currentTime < duration ) {
                if ( audioSource == null ) break;
                currentTime += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / duration);
                yield return null;
            }

            fadeList.Remove(audioSource);
            onComplete?.Invoke();
        }

        #endregion

        #region SIGNAL

        /// <summary>
        /// Sends a signal, which can be caught by listeners of the onSignalReceive event.
        /// </summary>
        /// <param name="signal">The signal to be sent.</param>
        public static void Signal(Signal signal) => onSignalReceive?.Invoke(signal);

        #endregion

        #region SpriteDb

        public static List< Sprite > GetAssets(string category, params string[ ] tags) {
            List< Sprite > result = new();

            if ( !IEDatabase.spriteDatabase.TryGetValue(category, out var assetDb) )
                return result;

            foreach ( var kvp in assetDb.Sprites ) {
                var obj = kvp.Key;
                var objTags = kvp.Value;

                bool allMatch = tags.All(_tag =>
                    objTags.Any(tag => tag.Equals(_tag, StringComparison.OrdinalIgnoreCase)));

                if ( !allMatch )
                    continue;

                result.Add(obj);
            }

            return result;
        }

        public static Sprite GetAsset(string category, params string[ ] tags) {
            if ( !IEDatabase.spriteDatabase.TryGetValue(category, out var assetDb) )
                return null;

            foreach ( var kvp in assetDb.Sprites ) {
                var obj = kvp.Key;
                var objTags = kvp.Value;

                bool allMatch = tags.All(_tag =>
                    objTags.Any(tag => tag.Equals(_tag, StringComparison.OrdinalIgnoreCase)));

                if ( allMatch )
                    return obj;
            }

            return null;
        }

        #endregion
    }
}