using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;

namespace VORPMailboxClient
{
    public class GetConfig : BaseScript
    {
        public static JObject Config = new JObject();
        public static Dictionary<string, string> Langs = new Dictionary<string, string>();
        public static Dictionary<string, string> UILangs = new Dictionary<string, string>();
        public static List<Vector3> Locations = new List<Vector3>();

        public static Dictionary<string, uint> Keys = new Dictionary<string, uint> {
            {"A", 0x7065027D},
            {"B", 0x4CC0E2FE},
            {"C", 0x9959A6F0},
            {"D", 0xB4E465B4},
            {"E", 0xCEFD9220},
            {"F", 0xB2F377E8},
            {"G", 0x760A9C6F},
            {"H", 0x24978A28},
            {"I", 0xC1989F95},
            {"J", 0xF3830D8E},
            // Missing K, don't know if anything is actually bound to it
            {"L", 0x80F28E95},
            {"M", 0xE31C6A41},
            {"N", 0x4BC9DABB},
            {"O", 0xF1301666},
            {"P", 0xD82E0BD2},
            {"Q", 0xDE794E3E},
            {"R", 0xE30CD707},
            {"S", 0xD27782E3},
            // Missing T
            {"U", 0xD8F73058},
            {"V", 0x7F8D09B8},
            {"W", 0x8FD015D8},
            {"X", 0x8CC9CD42},
            // Missing Y
            {"Z", 0x26E9DC00}
    };

    public static bool IsLoaded = false;

        public GetConfig()
        {
            EventHandlers[$"{GetCurrentResourceName()}:SendConfig"] += new Action<string, ExpandoObject>(LoadDefaultConfig);
            TriggerServerEvent($"{GetCurrentResourceName()}:getConfig");
            
        }

        private void LoadDefaultConfig(string dc, ExpandoObject dl)
        {
            Config = JObject.Parse(dc);

            // Lang and UI
            foreach (KeyValuePair<string, object> l in dl)
            {
                if (l.Key.StartsWith("UI"))
                {
                    UILangs[l.Key] = l.Value.ToString();
                }
                else
                {
                    Langs[l.Key] = l.Value.ToString();
                }
            }
            
            // Add used key is tips near mailboxes
            Langs["TextNearMailboxLocation"] = Langs["TextNearMailboxLocation"].Replace('$', Config["keyToOpen"].ToString()[0]);


            dynamic locations = GetConfig.Config["locations"];


            foreach (var location in locations)
            {
                JArray coords = location as JArray;

                if (coords == null || coords.Count != 3)
                {
                    continue;
                }
                
                Locations.Add(new Vector3(coords[0].Value<int>() , coords[1].Value<int>(), coords[2].Value<int>()));
            }
            
            IsLoaded = true;
        }
    }
}