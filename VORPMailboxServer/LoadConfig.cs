using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VORPMailboxServer
{
    class LoadConfig : BaseScript
    {
        public static JObject Config = new JObject();
        public static string ConfigString;
        public static Dictionary<string, string> Langs = new Dictionary<string, string>();
        public static string resourcePath = $"{API.GetResourcePath(API.GetCurrentResourceName())}";
        
        public static bool IsConfigLoaded = false;
        
        public LoadConfig()
        {
            EventHandlers[$"{API.GetCurrentResourceName()}:getConfig"] += new Action<Player>(getConfig);
        
            LoadConfigAndLang();
        }
        
        private void LoadConfigAndLang()
        {
            if (File.Exists($"{resourcePath}/Config.json"))
            {
                ConfigString = File.ReadAllText($"{resourcePath}/Config.json", Encoding.UTF8);
                Config = JObject.Parse(ConfigString);
                if (File.Exists($"{resourcePath}/{Config["language"]}.json"))
                {
                    string langstring = File.ReadAllText($"{resourcePath}/{Config["language"]}.json", Encoding.UTF8);
                    Langs = JsonConvert.DeserializeObject<Dictionary<string, string>>(langstring);
                    Debug.WriteLine($"{API.GetCurrentResourceName()}: Language {Config["language"]}.json loaded!");
                    
                    // Add used key is tips near mailboxes
                    Langs["TextNearMailboxLocation"] = Langs["TextNearMailboxLocation"].Replace("$1", Config["keyToOpen"].ToString());
                    Langs["TextNearMailboxLocation"] = Langs["TextNearMailboxLocation"].Replace("$2", Config["keyToOpenBroadcast"].ToString());
                    
                    // replace insufficiant money tip
                    Langs["TipOnInsufficientMoneyForMessage"] = Langs["TipOnInsufficientMoneyForMessage"].Replace("$1", Config["MessageSendPrice"].ToString());
                    Langs["TipOnInsufficientMoneyForBroadcast"] = Langs["TipOnInsufficientMoneyForBroadcast"].Replace("$1", Config["MessageBroadcastPrice"].ToString());
                }
                else
                {  
                    Debug.WriteLine($"{API.GetCurrentResourceName()}: {Config["language"]}.json Not Found");
                }
            }
            else
            {
                Debug.WriteLine($"{API.GetCurrentResourceName()}: Config.json Not Found");
            }
            IsConfigLoaded = true;
        }
        
        private void getConfig([FromSource]Player source)
        {
            source.TriggerEvent($"{API.GetCurrentResourceName()}:SendConfig", ConfigString, Langs);
        }
    }
}