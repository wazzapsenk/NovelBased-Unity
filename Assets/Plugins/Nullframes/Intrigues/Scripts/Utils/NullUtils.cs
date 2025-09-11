using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nullframes.Intrigues.Graph;
using Nullframes.Intrigues.Utils;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nullframes.Intrigues {
    public class CoroutineData {
        public Coroutine Coroutine { get; set; }
        public float Delay { get; set; }
        public float RemainingTime { get; set; }
        public int LoopCount { get; set; }
        public Action Call { get; }
        public Func< bool > WaitUntil { get; }
        public Action< float > OnUpdate { get; set; }
        public bool UnscaledTime { get; }
        public bool Paused { get; set; }

        public CoroutineData(Coroutine coroutine, float delay, Func< bool > waitUntil, int loopCount, Action call,
            bool unscaledTime) {
            Coroutine = coroutine;
            Delay = delay;
            RemainingTime = delay;
            WaitUntil = waitUntil;
            LoopCount = loopCount;
            Call = call;
            UnscaledTime = unscaledTime;
            Paused = false;
        }
    }

    public class DelayedCallParams {
        public string DelayName;
        public float Delay = 0f;
        public Func< bool > WaitUntil;
        public Action Call;
        public Action< float > OnUpdate;
        public int LoopCount = 0;
        public bool UnscaledTime = false;
    }

    public static class NullUtils {
        public static float Round(this float number) {
            return Mathf.Round(number * 100f) * .01f;
        }
        
        public static bool TryPop<T>(Stack<T> stack, out T value)
        {
            if (stack.Count > 0)
            {
                value = stack.Pop();
                return true;
            }
            value = default;
            return false;
        }

        // For List<T> using UnityEngine.Random
        public static T PickRandom< T >(this List< T > list) {
            if ( list == null || list.Count == 0 )
                throw new InvalidOperationException("The list cannot be empty or null.");

            int index = UnityEngine.Random.Range(0, list.Count);
            return list[ index ];
        }

        // For IReadOnlyList<T> using UnityEngine.Random
        public static T PickRandom< T >(this IReadOnlyList< T > list) {
            if ( list == null || list.Count == 0 )
                throw new InvalidOperationException("The list cannot be empty or null.");

            int index = UnityEngine.Random.Range(0, list.Count);
            return list[ index ];
        }

        // For IEnumerable<T> using reservoir sampling
        public static T PickRandom< T >(this IEnumerable< T > sequence) {
            if ( sequence == null )
                throw new ArgumentNullException(nameof( sequence ), "The sequence cannot be null.");

            int count = 0;
            T current = default;

            foreach ( var element in sequence ) {
                count++;
                if ( UnityEngine.Random.Range(0, count) == 0 )
                    current = element;
            }

            if ( count == 0 )
                throw new InvalidOperationException("The sequence cannot be empty.");

            return current;
        }

        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax) {
            return ( value - fromMin ) / ( fromMax - fromMin ) * ( toMax - toMin ) + toMin;
        }

        private static readonly Dictionary< string, CoroutineData > coroutines = new();
        public static Dictionary< string, CoroutineData > Coroutines => coroutines;
        
        public static async Task WaitUntilAsync(Func<bool> condition, int checkIntervalMs = 10, int timeoutMs = -1)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (!condition())
            {
                await Task.Delay(checkIntervalMs);
                if (timeoutMs > 0 && stopwatch.ElapsedMilliseconds > timeoutMs)
                    break;
            }
        }
        public static string DelayedCall(DelayedCallParams config) {
            string delayName = config.DelayName ?? GenerateID();

            if ( coroutines.ContainsKey(delayName) )
                StopCall(delayName);

            var data = new CoroutineData(
                coroutine: null,
                delay: config.Delay,
                waitUntil: config.WaitUntil,
                loopCount: config.LoopCount,
                call: config.Call,
                unscaledTime: config.UnscaledTime
            );

            if ( config.OnUpdate != null )
                data.OnUpdate += config.OnUpdate;

            coroutines[ delayName ] = data;
            coroutines[ delayName ].Coroutine = CoroutineManager.StartRoutine(delayedCall(delayName));

            return delayName;
        }

        private static IEnumerator delayedCall(string delayName) {
            if ( !coroutines.TryGetValue(delayName, out var coroutineData) )
                yield break;

            // Optional: wait until a custom condition is met
            if ( coroutineData.WaitUntil != null )
                yield return new WaitUntil(coroutineData.WaitUntil);

            // Delay of -1 means wait forever
            if ( coroutineData.Delay.Equals(-1) )
                yield return new WaitUntil(() => false);

            // Handle time-based delay (scaled vs unscaled)
            while ( coroutineData.RemainingTime > 0 ) {
                float delta = coroutineData.UnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                coroutineData.RemainingTime -= delta;

                if ( coroutineData.RemainingTime < 0 )
                    coroutineData.RemainingTime = 0;

                coroutineData.OnUpdate?.Invoke(coroutineData.RemainingTime);
                yield return null;
            }

            // Reset remaining time for next loop
            coroutineData.RemainingTime = coroutineData.Delay;

            // Execute callback
            coroutineData.Call?.Invoke();

            // Decrease loop count only if greater than 0
            if ( coroutineData.LoopCount > 0 )
                coroutineData.LoopCount--;

            // Loop if infinite or still has remaining cycles
            if ( coroutineData.LoopCount is -1 or > 0 ) {
                coroutineData.Coroutine = CoroutineManager.StartRoutine(delayedCall(delayName));
            } else {
                StopCall(delayName);
            }
        }

        public static bool StopCall(string delayName) {
            if ( string.IsNullOrEmpty(delayName) || !coroutines.ContainsKey(delayName) ) return false;
            CoroutineManager.StopRoutine(coroutines[ delayName ].Coroutine);
            coroutines.Remove(delayName);
            return true;
        }

        public static bool PauseCall(string delayName) {
            if ( !coroutines.ContainsKey(delayName) || coroutines[ delayName ].Paused ) return false;
            coroutines[ delayName ].Paused = true;
            CoroutineManager.StopRoutine(coroutines[ delayName ].Coroutine);
            return true;
        }

        public static bool ResumeCall(string delayName) {
            if ( !coroutines.ContainsKey(delayName) || !coroutines[ delayName ].Paused ) return false;
            coroutines[ delayName ].Paused = false;
            coroutines[ delayName ].Coroutine = CoroutineManager.StartRoutine(delayedCall(delayName));
            return true;
        }

        public static CoroutineData CoroutineData(string delayName) {
            if ( !coroutines.ContainsKey(delayName) ) return null;
            return coroutines[ delayName ];
        }

        public static void SetDelay(string delayName, float newDelay) {
            if ( !coroutines.ContainsKey(delayName) ) return;

            coroutines[ delayName ].RemainingTime = newDelay;
            coroutines[ delayName ].Delay = newDelay;
        }

        public static void SetLoop(string delayName, int loopCount) {
            if ( coroutines.ContainsKey(delayName) ) coroutines[ delayName ].LoopCount = loopCount;
        }

        public static bool IsRunning(string callName) {
            return coroutines.ContainsKey(callName);
        }

        public static void SaveAsset(params Object[ ] assets) {
#if UNITY_EDITOR
            foreach ( var asset in assets ) {
                if ( asset == null ) return;
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }

        public static string BracesIn(this string text, string keyword) {
            var replaceText = (string)IM.Variables.FirstOrDefault(v => v.name == keyword);
            replaceText ??= $"{{{keyword}}}";
            return Regex.Replace(text, $"{{{keyword}}}", replaceText);
        }

        public static void AddItem< T, Y >(this SerializableDictionary< T, List< Y > > serializableDictionary, T key,
            Y value) {
            if ( serializableDictionary.ContainsKey(key) ) {
                serializableDictionary[ key ].Add(value);
                return;
            }

            serializableDictionary.Add(key, new List< Y >() { value });
        }

        public static string GenerateID() {
            return Guid.NewGuid().ToString();
        }

        public static bool ExistsClan(this IEnumerable< Clan > clans, string clanName, out Clan clan) {
            clan = clans.FirstOrDefault(d => d.ClanName == clanName);
            return clan != null;
        }

        public static void Trigger(this Scheme scheme, string triggerName, bool value = true) {
            IM.Trigger(scheme, triggerName, value);
        }

        public static string Shortener(this string actorName, int length = 8) {
            if ( actorName.Length > length ) {
                return actorName[ ..length ] + "..";
            }

            return actorName;
        }

        private static bool IsWhitespace(this char character) {
            switch ( character ) {
                case '\u0020':
                case '\u00A0':
                case '\u1680':
                case '\u2000':
                case '\u2001':
                case '\u2002':
                case '\u2003':
                case '\u2004':
                case '\u2005':
                case '\u2006':
                case '\u2007':
                case '\u2008':
                case '\u2009':
                case '\u200A':
                case '\u202F':
                case '\u205F':
                case '\u3000':
                case '\u2028':
                case '\u2029':
                case '\u0009':
                case '\u000A':
                case '\u000B':
                case '\u000C':
                case '\u000D':
                case '\u0085': {
                    return true;
                }

                default: {
                    return false;
                }
            }
        }

        public static string RemoveWhitespaces(this string text) {
            var textLength = text.Length;

            var textCharacters = text.ToCharArray();

            var currentWhitespacelessTextLength = 0;

            for ( var currentCharacterIndex = 0; currentCharacterIndex < textLength; ++currentCharacterIndex ) {
                var currentTextCharacter = textCharacters[ currentCharacterIndex ];

                if ( currentTextCharacter.IsWhitespace() ) continue;

                textCharacters[ currentWhitespacelessTextLength++ ] = currentTextCharacter;
            }

            return new string(textCharacters, 0, currentWhitespacelessTextLength);
        }

        public static string RemoveSpecialCharacters(this string text) {
            var textLength = text.Length;

            var textCharacters = text.ToCharArray();

            var currentWhitespacelessTextLength = 0;

            for ( var currentCharacterIndex = 0; currentCharacterIndex < textLength; ++currentCharacterIndex ) {
                var currentTextCharacter = textCharacters[ currentCharacterIndex ];

                if ( !char.IsLetterOrDigit(currentTextCharacter) && !currentTextCharacter.IsWhitespace() ) continue;

                textCharacters[ currentWhitespacelessTextLength++ ] = currentTextCharacter;
            }

            return new string(textCharacters, 0, currentWhitespacelessTextLength);
        }

        public static List< int > DivideNumber(this int number, int parts) {
            var remainder = number % parts;
            var partSize = number / parts;

            var numbers = new List< int >();

            if ( remainder == 0 ) {
                for ( var i = 0; i < parts; i++ ) numbers.Add(partSize);
                return numbers;
            }

            var largerParts = remainder;
            var smallerParts = parts - remainder;
            var largerPartSize = partSize + 1;
            var smallerPartSize = partSize;

            for ( var i = 0; i < largerParts; i++ ) numbers.Add(largerPartSize);

            for ( var i = 0; i < smallerParts; i++ ) numbers.Add(smallerPartSize);

            return numbers;
        }
        
        public static List< string > GetLanguageCodes() {
            var cultureCodes = new List< string >();
            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            foreach ( var culture in cultures )
                if ( !cultureCodes.Contains(culture.Name) )
                    cultureCodes.Add(culture.Name);

            return cultureCodes;
        }

        public static string LocaliseText(this string source) {
            if ( !IM.Exists || string.IsNullOrEmpty(source) ) return source;
            var text = source;
            foreach ( Match titleLocalisation in Regex.Matches(text, STATIC.LOCALISATION) )
                if ( titleLocalisation.Success )
                    text = Regex.Replace(text, "{l:" + titleLocalisation.Value + "}",
                        IM.GetText(titleLocalisation.Value));

            return text;
        }

        public static string SchemeFormat(this string source, Actor conspirator, Actor target) {
            target ??= conspirator;

            var replacements = new Dictionary< string, string > {
                { "{t*Name}", $"<link=\"{target.ID}\">{target.Name}</link>" },
                { "{c*Name}", $"<link=\"{conspirator.ID}\">{conspirator.Name}</link>" },
                { "{t*FullName}", $"<link=\"{target.ID}\">{target.FullName}</link>" },
                { "{c*FullName}", $"<link=\"{conspirator.ID}\">{conspirator.FullName}</link>" },
                { "{c*Gender}", conspirator.Gender.ToString() },
                { "{t*Gender}", target.Gender.ToString() },
                { "{c*Age}", conspirator.Age.ToString() },
                { "{t*Age}", target.Age.ToString() }
            };

            if ( conspirator.Culture != null ) replacements[ "{c*Culture}" ] = conspirator.Culture.CultureName;
            if ( target.Culture != null ) replacements[ "{t*Culture}" ] = target.Culture.CultureName;

            if ( conspirator.Family != null ) replacements[ "{c*Family}" ] = conspirator.Family.FamilyName;
            if ( target.Family != null ) replacements[ "{t*Family}" ] = target.Family.FamilyName;

            if ( conspirator.Role != null ) {
                replacements[ "{c*Role}" ] = conspirator.Role.RoleName;
                replacements[ "{c*Title}" ] = conspirator.Title;
            }

            if ( target.Role != null ) {
                replacements[ "{t*Role}" ] = target.Role.RoleName;
                replacements[ "{t*Title}" ] = target.Title;
            }

            if ( conspirator.Clan != null ) replacements[ "{c*Clan}" ] = conspirator.Clan.ClanName;
            if ( target.Clan != null ) replacements[ "{t*Clan}" ] = target.Clan.ClanName;

            // Replace fixed patterns (non-regex)
            foreach ( var kvp in replacements )
                source = source.Replace(kvp.Key, kvp.Value);

            // Dynamic {c:...} and {t:...} variables
            source = Regex.Replace(source, @"{c:(\w+)}", match => {
                var value = conspirator.GetVariable(match.Groups[ 1 ].Value);
                return value?.ToString() ?? string.Empty;
            });

            source = Regex.Replace(source, @"{t:(\w+)}", match => {
                var value = target.GetVariable(match.Groups[ 1 ].Value);
                return value?.ToString() ?? string.Empty;
            });

            return source;
        }


        public static float CalculatePercentage(this float number, float percentage) {
            return number * percentage / 100f;
        }


        public static Color HTMLColor(string htmlString) {
            return ColorUtility.TryParseHtmlString($"{htmlString}", out var clr) ? clr : new Color(1f, 1f, 1f, 1f);
        }

        public static Color RandomHTMLColor(params string[ ] htmlString) {
            return ColorUtility.TryParseHtmlString($"{htmlString[ UnityEngine.Random.Range(0, htmlString.Length) ]}",
                out var clr)
                ? clr
                : new Color(1f, 1f, 1f, 1f);
        }

        public static string ToUpperPreserveTags(string input) {
            if ( string.IsNullOrEmpty(input) ) return input;

            var result = new System.Text.StringBuilder();
            bool insideTag = false;

            foreach ( char c in input ) {
                if ( c == '<' ) insideTag = true;
                if ( !insideTag )
                    result.Append(char.ToUpper(c, CultureInfo.InvariantCulture));
                else
                    result.Append(c);
                if ( c == '>' ) insideTag = false;
            }

            return result.ToString();
        }

#if UNITY_EDITOR

        public static void PlayClip(AudioClip clip, int startSample = 0, bool loop = false) {
            if ( IsClipPlaying() ) return;
            if ( clip == null ) return;
            Assembly unityEditorAssembly = typeof( AudioImporter ).Assembly;

            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");

            MethodInfo method = audioUtilClass.GetMethod(
                "PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[ ] { typeof( AudioClip ), typeof( int ), typeof( bool ) },
                null
            );

            if ( method != null )
                method.Invoke(
                    null,
                    new object[ ] { clip, startSample, loop }
                );
        }

        public static bool IsClipPlaying() {
            Assembly unityEditorAssembly = typeof( AudioImporter ).Assembly;
            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod(
                "IsPreviewClipPlaying",
                BindingFlags.Static | BindingFlags.Public
            );

            bool playing = method != null && (bool)method.Invoke(null, null);

            return playing;
        }

        public static void StopAllClips() {
            Assembly unityEditorAssembly = typeof( AudioImporter ).Assembly;

            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod(
                "StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[ ] { },
                null
            );

            if ( method != null )
                method.Invoke(
                    null,
                    new object[ ] { }
                );
        }

        public static TextureImporterType GetTextureType(this Texture2D texture) {
            var path = AssetDatabase.GetAssetPath(texture);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if ( importer != null ) return importer.textureType;
            return TextureImporterType.Default;
        }

        public static void SerializedProperty(this Object @object, Action< SerializedObject > @event = null,
            bool ApplyModifiedProperties = true) {
            var serializedObject = new SerializedObject(@object);
            if ( @event != null ) {
                @event.Invoke(serializedObject);
                if ( ApplyModifiedProperties ) serializedObject.ApplyModifiedProperties();
            }
        }

        public static SerializedObject Serialize(this Object @object) {
            return new SerializedObject(@object);
        }

        public static T GetValue< T >(this SerializedProperty property) where T : class {
            object obj = property.serializedObject.targetObject;
            var path = property.propertyPath.Replace(".Array.data", "");
            var fieldStructure = path.Split('.');
            var rgx = new Regex(@"\[\d+\]");
            foreach ( var t in fieldStructure )
                if ( t.Contains("[") ) {
                    var index = Convert.ToInt32(new string(t.Where(char.IsDigit).ToArray()));
                    obj = GetFieldValueWithIndex(rgx.Replace(t, ""), obj, index);
                } else {
                    obj = GetFieldValue(t, obj);
                }

            return (T)obj;
        }

        public static bool SetValue< T >(this SerializedProperty property, T value) where T : class {
            object obj = property.serializedObject.targetObject;
            var path = property.propertyPath.Replace(".Array.data", "");
            var fieldStructure = path.Split('.');
            var rgx = new Regex(@"\[\d+\]");
            for ( var i = 0; i < fieldStructure.Length - 1; i++ )
                if ( fieldStructure[ i ].Contains("[") ) {
                    var index = Convert.ToInt32(new string(fieldStructure[ i ].Where(char.IsDigit).ToArray()));
                    obj = GetFieldValueWithIndex(rgx.Replace(fieldStructure[ i ], ""), obj, index);
                } else {
                    obj = GetFieldValue(fieldStructure[ i ], obj);
                }

            var fieldName = fieldStructure.Last();
            if ( fieldName.Contains("[") ) {
                var index = Convert.ToInt32(new string(fieldName.Where(char.IsDigit).ToArray()));
                return SetFieldValueWithIndex(rgx.Replace(fieldName, ""), obj, index, value);
            }

            return SetFieldValue(fieldName, obj, value);
        }

        private static object GetFieldValue(string fieldName, object obj,
            BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                    BindingFlags.NonPublic) {
            var field = obj.GetType().GetField(fieldName, bindings);
            if ( field != null ) return field.GetValue(obj);

            return default;
        }

        private static object GetFieldValueWithIndex(string fieldName, object obj, int index,
            BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                    BindingFlags.NonPublic) {
            var field = obj.GetType().GetField(fieldName, bindings);
            if ( field != null ) {
                var list = field.GetValue(obj);
                if ( list.GetType().IsArray ) return ( (object[ ])list )[ index ];

                if ( list is IEnumerable ) return ( (IList)list )[ index ];
            }

            return default;
        }

        private static FieldInfo FindFieldRecursively(Type type, string fieldName, BindingFlags bindings) {
            FieldInfo field = null;
            while ( type != null && field == null ) {
                field = type.GetField(fieldName, bindings);
                type = type.BaseType;
            }

            return field;
        }

        private static bool SetFieldValue(string fieldName, object obj, object value,
            BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                    BindingFlags.NonPublic) {
            var field = obj.GetType().GetField(fieldName, bindings);
            if ( field != null ) {
                field.SetValue(obj, value);
                return true;
            }

            return false;
        }

        private static bool SetFieldValueWithIndex(string fieldName, object obj, int index, object value,
            BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                    BindingFlags.NonPublic) {
            var field = obj.GetType().GetField(fieldName, bindings);
            Debug.Log(field?.Name);
            if ( field == null ) return false;
            var list = field.GetValue(obj);
            if ( list.GetType().IsArray ) {
                ( (object[ ])list )[ index ] = value;
                return true;
            }

            if ( list is not IEnumerable ) return false;
            ( (IList)list )[ index ] = value;
            return true;
        }
#endif

        public static bool CheckProbability(float percentage) {
            percentage = Math.Max(0f, Math.Min(100f, percentage));

            Random random = new Random();

            int randomValue = random.Next(10000);

            return randomValue < percentage * 100;
        }

        public static void Adds< T >(this List< T > list, params T[ ] item) {
            list.AddRange(item);
        }

        public static NVar GetVariable(this Actor actor, string variableNameOrId) =>
            actor.GetVariable(variableNameOrId);

        public static void SetVariable(this Actor actor, string variableNameOrId, object value) {
            actor.SetVariable(variableNameOrId, value);
        }

        public static void SetVariable(this Schemer schemer, string variableName, object value) {
            var variable = schemer.GetVariable(variableName);
            if ( variable == null ) return;
            variable.value = value;
        }

        public static T Duplicate< T >(this GameObject @object) {
            return Object.Instantiate(@object, @object.transform.parent).GetComponent< T >();
        }

        public static T Duplicate< T >(this GameObject @object, Transform parent) {
            return Object.Instantiate(@object, parent).GetComponent< T >();
        }

        public static readonly KeyCode[ ] _keyCodes =
            Enum.GetValues(typeof( KeyCode ))
                .Cast< KeyCode >()
                .ToArray();

        public static readonly string[ ] _keyNames =
            Enum.GetNames(typeof( KeyCode ))
                .ToArray();

        public static void GetCurrentKeysDown(List< KeyCode > buffer) {
            foreach ( var key in _keyCodes ) {
                if ( Input.GetKeyDown(key) )
                    buffer.Add(key);
            }
        }

        public static void GetCurrentKeysUp(List< KeyCode > buffer) {
            foreach ( var key in _keyCodes ) {
                if ( Input.GetKeyUp(key) )
                    buffer.Add(key);
            }
        }

        public static void GetCurrentKeys(List< KeyCode > buffer) {
            foreach ( var key in _keyCodes ) {
                if ( Input.GetKey(key) )
                    buffer.Add(key);
            }
        }
    }
}