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
        public static bool IsLoaded = false;

        public GetConfig()
        {
            EventHandlers[$"{GetCurrentResourceName()}:SendConfig"] += new Action<string, ExpandoObject>(LoadDefaultConfig);
            TriggerServerEvent($"{GetCurrentResourceName()}:getConfig");
            
        }

        private void LoadDefaultConfig(string dc, ExpandoObject dl)
        {
            Config = JObject.Parse(dc);

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