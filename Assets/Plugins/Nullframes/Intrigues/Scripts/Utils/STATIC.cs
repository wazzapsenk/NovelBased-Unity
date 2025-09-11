using UnityEngine;

namespace Nullframes.Intrigues.Utils
{
    public static class STATIC
    {
        public static readonly string CURRENT_VERSION = "1.0.62 Hotfix";
        public static readonly string RELEASE_NOTES_URL = "https://bit.ly/intrigues1062hotfix";
        public static readonly string WEB_URL = "https://www.wlabsocks.com";
        public static readonly string REVIEW_URL = "https://u3d.as/2TCR";
        
        public static readonly string AI_URL = "https://www.wlabsocks.com/wiki/index.php/Advanced_AI_System";
        public static readonly string GPT_URL = "https://www.wlabsocks.com/wiki/index.php/ChatGPT_Integration";
        public static readonly string VOTE_URL = "https://www.wlabsocks.com/wiki/index.php/Vote_System";
        public static readonly string QUESTS_URL = "https://www.wlabsocks.com/wiki/index.php/Expansion:_QUESTS";
        public static readonly string MP_URL = "https://www.wlabsocks.com/wiki/index.php/Multiplayer_Integration";
        public static readonly string FORUM_URL = "https://www.wlabsocks.com/forum";

        public static readonly int MARRIAGE_AGE = 16;
        public static readonly int MAX_AGE = 120;
        
        public static readonly int MAX_FLOW_DEPTH = 100;

        public static readonly Color DefaultColor = NullUtils.HTMLColor("#C8C8C8");
        public static readonly Color Chance = NullUtils.HTMLColor("#8c5171");
        public static readonly Color ChanceModifier = NullUtils.HTMLColor("#6a518c");
        public static readonly Color GreenPort = NullUtils.HTMLColor("#67a365");
        public static readonly Color ClassPort = NullUtils.HTMLColor("#BC9B52");
        public static readonly Color RedPort = NullUtils.HTMLColor("#a3364c");
        public static readonly Color BluePort = NullUtils.HTMLColor("#595bd4");
        public static readonly Color YellowPort = NullUtils.HTMLColor("#ded831");
        public static readonly Color ChildPortMale = NullUtils.HTMLColor("#4644ab");
        public static readonly Color ChildPortFemale = NullUtils.HTMLColor("#a444ab");
        public static readonly Color ParentPortMale = NullUtils.HTMLColor("#4644ab");
        public static readonly Color ParentPortFemale = NullUtils.HTMLColor("#a444ab");
        public static readonly Color SpouseInPort = NullUtils.HTMLColor("#914e6d");
        public static readonly Color SpouseOutPort = NullUtils.HTMLColor("#914e6d");
        public static readonly Color FailedPort = NullUtils.HTMLColor("#8c5151");
        public static readonly Color SuccessPort = NullUtils.HTMLColor("#86ab7b");
        public static readonly Color SoftYellow = NullUtils.HTMLColor("#c9c47f");
        public static readonly Color SequencerPort = NullUtils.HTMLColor("#6e549e");
        public static readonly Color BreakSequencerPort = NullUtils.HTMLColor("#9e5454");
        public static readonly Color EndNode = NullUtils.HTMLColor("#ab9394");
        public static readonly Color FlowPort = NullUtils.HTMLColor("#bdbd35");
        public static Color RandomColor => Random.ColorHSV(0f, 1f, 0.3f, 0.6f, 0.5f, 0.8f, 1f, 1f);

        //==========================================================
        public static readonly string MANAGER_NOT_EXISTS =
            "Intrigues Manager does not exist.";

        public static readonly string DEBUG_LOADING_METHODS = "Loading all attributes..";
        public static readonly string DEBUG_LOADED_ATTRIBUTES = "Successfully loaded attributes.[{0}]";
        public static readonly string EXISTS_LANGUAGE_KEY = "This language code already exists.";
        public static readonly string CONTAINS_MANAGER = "This scene already contains the IM.";
        public static readonly string INVALID_LOCALISATION_KEY = "Invalid Localisation Key: {0}.";
        public static readonly string DEBUG_INVALID_LANGUAGE_KEY = "Invalid language key: {0}.";
        public static readonly string CHOICE_TEXT_MESH_NOT_FOUND = "ERROR";
        public static readonly string DEBUG_AI_SELECTED_DIALOGUE = "AI selected a random choice.";
        public static readonly string DEBUG_TITLE = "Intrigues -> {0}";
        public static readonly string DEBUG_CLAN_SAME_TITLE = "Clans cannot have the same name or id: {0}.";
        public static readonly string DEBUG_ROLE_SAME_TITLE = "Roles cannot have the same name or id: {0}.";
        public static readonly string DEBUG_CULTURE_SAME_TITLE = "Cultures cannot have the same name or id: {0}.";
        public static readonly string DEBUG_FAMILY_SAME_TITLE = "Families cannot have the same name or id: {0}.";
        public static readonly string DEBUG_POLICY_SAME_TITLE = "Policies cannot have the same name or id: {0}.";

        public static readonly string DEBUG_DIALOGUE_MANAGER_NOT_FOUND =
            "IM -> Dialogue Manager not found. Please assign it to the IM Inspector.";
        
        public static readonly string LOCALISATION = @"(?<=\{l\:)(.*?)(?=\})";
        public static readonly string GLOBAL_VARIABLE = @"(?<=\{g\:)(.*?)(?=\})";
        public static readonly string TABLE_VARIABLE = @"(?<=\{table\:)(.*?)(?=\})";
        public static readonly string TARGET_VARIABLE = @"(?<=\{t\:)(.*?)(?=\})";
        public static readonly string CONSPIRATOR_VARIABLE = @"(?<=\{c\:)(.*?)(?=\})";
    }
}