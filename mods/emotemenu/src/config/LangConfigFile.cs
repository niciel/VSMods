using Newtonsoft.Json;

namespace emotemenu
{
    public class LangConfigFile
    {
        [JsonProperty]
        public string emote_properties_KeyBindDescription = "Opens emote menu";
        [JsonProperty]
        public string emote_menu_wave = "wave";
        [JsonProperty]
        public string emote_menu_cheer = "cheer";
        [JsonProperty]
        public string emote_menu_shrug = "shrug";
        [JsonProperty]
        public string emote_menu_cry = "cry";
        [JsonProperty]
        public string emote_menu_nod = "nod";
        [JsonProperty]
        public string emote_menu_facepalm = "facepalm";
        [JsonProperty]
        public string emote_menu_bow = "bow";
        [JsonProperty]
        public string emote_menu_laugh = "laugh";
        [JsonProperty]
        public string emote_menu_rage = "rage";
    }
}