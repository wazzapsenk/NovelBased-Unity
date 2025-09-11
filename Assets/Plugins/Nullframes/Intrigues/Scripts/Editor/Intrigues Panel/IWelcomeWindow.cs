using Nullframes.Intrigues.Graph;
using Nullframes.Intrigues.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues {
    public class IWelcomeWindow : EditorWindow {
        [MenuItem("Tools/Nullframes/Intrigues/Welcome Window", false, 0)]
        public static void OpenWindow() {
            var wnd = GetWindow<IWelcomeWindow>();
            wnd.titleContent = new GUIContent("Intrigues");

            wnd.maxSize = new Vector2(600f, 920f);
            wnd.minSize = new Vector2(600f, 920f);
        }
        
        [MenuItem("Tools/Nullframes/Intrigues/Video Tutorials")]
        public static void GoVideos() {
            Application.OpenURL("https://www.youtube.com/playlist?list=PLIwYhgYrkPtL5hAOSu-Mj-TUxEUF0--3J");
        }

        [MenuItem("Tools/Nullframes/Intrigues/Wiki")]
        public static void Wiki() {
            Application.OpenURL("https://wlabsocks.com/wiki");
        }
        
        [MenuItem("Tools/Nullframes/Intrigues/Forum")]
        public static void PHPBB() {
            Application.OpenURL("https://wlabsocks.com/forum");
        }

        [MenuItem("Tools/Nullframes/Intrigues/Support")]
        public static void Contact() {
            Application.OpenURL("https://wlabsocks.com/contact.html");
        }

        public void CreateGUI() {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Import UXML
            var visualTree =
                (VisualTreeAsset)EditorGUIUtility.Load("Nullframes/WelcomeWindow.uxml");
            visualTree.CloneTree(root);

            // Import USS
            var styleSheet = (StyleSheet)EditorGUIUtility.Load(EditorGUIUtility.isProSkin
                ? "Nullframes/WelcomeWindow_Dark.uss"
                : "Nullframes/WelcomeWindow_Light.uss");
            root.styleSheets.Add(styleSheet);

            var debugMode = root.Q<Toggle>("debug_mode");
            var startup = root.Q<Toggle>("startup");

            var debugKey = PlayerPrefs.HasKey("IntriguesDebugMode") &&
                           bool.Parse(PlayerPrefs.GetString("IntriguesDebugMode"));
            debugMode.value = debugKey;

            var startupKey = PlayerPrefs.HasKey("Intrigues_Startup_V4c") && bool.Parse(PlayerPrefs.GetString("Intrigues_Startup_V4c"));
            startup.value = startupKey;

            debugMode.RegisterCallback<ChangeEvent<bool>>(evt => {
                PlayerPrefs.SetString("IntriguesDebugMode", evt.newValue.ToString());
                PlayerPrefs.Save();
            });

            startup.RegisterCallback<ChangeEvent<bool>>(evt => {
                PlayerPrefs.SetString("Intrigues_Startup_V4c", evt.newValue.ToString());
                PlayerPrefs.Save();
            });

            var pdfButton = root.Q<VisualElement>("pdf");
            var phpbb = root.Q<VisualElement>("phpbb");
            var mailButton = root.Q<VisualElement>("mail");

            pdfButton.RegisterCallback<MouseDownEvent>(_ => Wiki());
            phpbb.RegisterCallback<MouseDownEvent>(_ => PHPBB());

            mailButton.RegisterCallback<MouseDownEvent>(_ => Contact());

            var version = root.Q<Label>("version");
            version.text = $"VERSION: {STATIC.CURRENT_VERSION}";            
            
            var releaseNotes = root.Q<Label>("releaseNotes");
            releaseNotes.RegisterCallback<MouseDownEvent>(_ => {
                Application.OpenURL(STATIC.RELEASE_NOTES_URL);
            });

            var multiplayer = root.Q<Toggle>("MP");
            multiplayer.SetEnabled(false);
            
            var lbl = multiplayer.GetChild<Label>();
            if (lbl != null) {
                lbl.style.color = NullUtils.HTMLColor(EditorGUIUtility.isProSkin ? "#79BC6B" : "#105B00");
            }
            
            var AI = root.Q<Toggle>("AI");
            AI.SetEnabled(false);
            
            lbl = AI.GetChild<Label>();
            if (lbl != null) {
                lbl.style.color = NullUtils.HTMLColor(EditorGUIUtility.isProSkin ? "#79BC6B" : "#105B00");
            }
            
            var GPT = root.Q<Toggle>("GPT");
            GPT.SetEnabled(false);
            
            lbl = GPT.GetChild<Label>();
            if (lbl != null) {
                lbl.style.color = NullUtils.HTMLColor(EditorGUIUtility.isProSkin ? "#79BC6B" : "#105B00");
            }
            
            var VOTE = root.Q<Toggle>("VOTE");
            VOTE.SetEnabled(false);
            
            lbl = VOTE.GetChild<Label>();
            if (lbl != null) {
                lbl.style.color = NullUtils.HTMLColor(EditorGUIUtility.isProSkin ? "#79BC6B" : "#105B00");
            }
            
            var QUEST = root.Q<Toggle>("QUEST");
            QUEST.SetEnabled(false);
            
            lbl = QUEST.GetChild<Label>();
            if (lbl != null) {
                lbl.style.color = NullUtils.HTMLColor(EditorGUIUtility.isProSkin ? "#79BC6B" : "#105B00");
            }

            var webPage = root.Q("webPage");
            webPage.RegisterCallback<MouseDownEvent>(_ => {
                Application.OpenURL(STATIC.WEB_URL);
            });            
            
            var reviewUs = root.Q("reviewUs");
            reviewUs.RegisterCallback<MouseDownEvent>(_ => {
                Application.OpenURL(STATIC.REVIEW_URL);
            });
            
            //Links
            
            var ai_url = root.Q<VisualElement>("ai_url");
            ai_url.RegisterCallback<MouseDownEvent>(_ => {
                Application.OpenURL(STATIC.AI_URL);
            });
            
            var gpt_url = root.Q<VisualElement>("ai_url");
            gpt_url.RegisterCallback<MouseDownEvent>(_ => {
                Application.OpenURL(STATIC.GPT_URL);
            });
            
            var vote_url = root.Q<VisualElement>("vote_url");
            vote_url.RegisterCallback<MouseDownEvent>(_ => {
                Application.OpenURL(STATIC.VOTE_URL);
            });     
            
            var quests_url = root.Q<VisualElement>("quests_url");
            quests_url.RegisterCallback<MouseDownEvent>(_ => {
                Application.OpenURL(STATIC.QUESTS_URL);
            });     
            
            var mp_url = root.Q<VisualElement>("mp_url");
            mp_url.RegisterCallback<MouseDownEvent>(_ => {
                Application.OpenURL(STATIC.MP_URL);
            });                
            
            var forum = root.Q<VisualElement>("forum");
            forum.RegisterCallback<MouseDownEvent>(_ => {
                Application.OpenURL(STATIC.FORUM_URL);
            });     
        }
    }

    [InitializeOnLoad]
    public class Startup {
        static Startup() {
            EditorApplication.update += RunOnce;
        }

        private static void RunOnce() {
            if (Application.isPlaying) return;
            var startupKey = PlayerPrefs.HasKey("Intrigues_Startup_V4c") && bool.Parse(PlayerPrefs.GetString("Intrigues_Startup_V4c"));
            if (!startupKey)
                IWelcomeWindow.OpenWindow();
            EditorApplication.update -= RunOnce;
        }
    }
}