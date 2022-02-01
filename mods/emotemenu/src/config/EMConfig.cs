using Newtonsoft.Json;

namespace emotemenu
{
    public class EMConfig
    {
        [JsonProperty]
        public float scale = 1f;
        [JsonProperty]
        public bool show_middle_circle = true;
        [JsonProperty]
        public string button_mouse_binding = "";
    }
}
