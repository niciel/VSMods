using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace emotemenu
{
    public class RadialMenuCFG
    {

        [JsonProperty]
        public float scale = 1f;

        [JsonProperty]
        public int button_hold_milis = 250;
    }
}
