using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;

namespace VORPMailboxServer
{
    public class VorpMailboxServer: BaseScript
    {
        public static dynamic CORE;
        private List<MailboxUser> _usersCache = new List<MailboxUser>();
        private Dictionary<string, long> _lastUserMessageSent = new Dictionary<string, long>();
        private Dictionary<string, long> _lastUserBroadcastSent = new Dictionary<string, long>();
        private long _lastUsersRefresh = 0;
        
        public VorpMailboxServer()
        {
            //Event for send new message
            EventHandlers["mailbox:message:send"] += new Action<Player, dynamic, string>(SendNewMessage);
            EventHandlers["mailbox:broadcast:send"] += new Action<Player, string>(BroadcastMessage);
            EventHandlers["mailbox:message:getMessages"] += new Action<Player>(GetMailboxMessages);
            EventHandlers["mailbox:message:updateMessages"] += new Action<Player, dynamic , dynamic>(UpdateMessages);
            EventHandlers["mailbox:message:getUsers"] += new Action<Player>(GetMailboxUsers);

            //GetCore Event
            TriggerEvent("getCore",new Action<dynamic>((dic) =>
            {
                CORE = dic;
            }));

            //Thread.Sleep(2000); // recommended for correct loading
            
            RefreshUsersCache();
        }

        private void SendNewMessage([FromSource]Player player, dynamic receiver, string message)
        {
            int source = int.Parse(player.Handle);
            string steam = "steam:" + player.Identifiers["steam"];
            
            
            dynamic sourceCharacter = CORE.getUser(source).getUsedCharacter;
            
            // checking if user is allowed to send a message now
            int delay = int.Parse(LoadConfig.Config["DelayBetweenTwoMessage"].ToString()); // In seconds
            
            if (_lastUserMessageSent.ContainsKey(steam) && _lastUserMessageSent[steam] + 1000 * delay >= API.GetGameTimer())
            {
                long remainingTime = ((_lastUserMessageSent[steam] + (1000 * delay)) - API.GetGameTimer()) / 1000;
                string errorMessage = LoadConfig.Langs["TipOnTooRecentMessageSent"].Replace("$1", remainingTime.ToString());
                
                // TriggerServerEvent comme quoi tu dois attendre
                player.TriggerEvent("mailbox:displayCustomError", errorMessage);
                return;
            }
            
            // checking if user has enough money
            int price = int.Parse(LoadConfig.Config["MessageSendPrice"].ToString());
            
            if (sourceCharacter.money < price)
            {
                player.TriggerEvent("mailbox:displayCustomError", LoadConfig.Langs["TipOnInsufficientMoneyForMessage"]);
                return;
            }
            
            // Insert new message in DB
            Exports["ghmattimysql"].execute("INSERT INTO mailbox_mails SET sender_id = ? , sender_firstname = ?, sender_lastname = ?, receiver_id = ?, receiver_firstname = ?, receiver_lastname = ?, message = ?",
                new object[] {
                    steam,
                    sourceCharacter.firstname,
                    sourceCharacter.lastname,
                    receiver.steam,
                    receiver.firstname,
                    receiver.lastname,
                    message
                });

            TriggerEvent("vorp:removeMoney", source, 0, price);
            _lastUserMessageSent[steam] = API.GetGameTimer();
            player.TriggerEvent("mailbox:displayCustomMessage", LoadConfig.Langs["TipOnMessageSent"]);
            try
            {
                dynamic connectedUsers = CORE.getUsers(); // return a Dictionary of <SteamID, User>

                foreach (KeyValuePair<string, dynamic> user in connectedUsers)
                {
                    // if the steamID does not correspond to the receiver SteamID, skip.
                    if (user.Key != receiver.steam.ToString()) continue;

                    dynamic receiverCharacter = user.Value.getUsedCharacter;

                    // if connected receiver use the right character, send a tip to him
                    if (receiverCharacter.firstname == receiver.firstname &&
                        receiverCharacter.lastname == receiver.lastname)
                    {
                        Player p = GetPlayer(user.Value.source);
                        if (p != null)
                        {
                            GetPlayer(user.Value.source).TriggerEvent("mailbox:message:receive", $"{sourceCharacter.firstname} {sourceCharacter.lastname}");
                        }
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        
        private void BroadcastMessage([FromSource]Player player, string message)
        {
            int source = int.Parse(player.Handle);
            string steam = "steam:" + player.Identifiers["steam"];
            
            
            dynamic sourceCharacter = CORE.getUser(source).getUsedCharacter;
            
            // checking if user is allowed to send a message now
            int delay = int.Parse(LoadConfig.Config["DelayBetweenTwoBroadcast"].ToString()); // In seconds
            
            if (_lastUserBroadcastSent.ContainsKey(steam) && _lastUserBroadcastSent[steam] + 1000 * delay >= API.GetGameTimer())
            {
                long remainingTime = ((_lastUserBroadcastSent[steam] + (1000 * delay)) - API.GetGameTimer()) / 1000;
                string errorMessage = LoadConfig.Langs["TipOnTooRecentMessageSent"].Replace("$1", remainingTime.ToString());
                
                // TriggerServerEvent comme quoi tu dois attendre
                player.TriggerEvent("mailbox:displayCustomError", errorMessage);
                return;
            }
            
            // checking if user has enough money
            int price = int.Parse(LoadConfig.Config["MessageBroadcastPrice"].ToString());
            
            if (sourceCharacter.money < price)
            {
                player.TriggerEvent("mailbox:displayCustomError", LoadConfig.Langs["TipOnInsufficientMoneyForBroadcast"]);
                return;
            }
            
            
            TriggerEvent("vorp:removeMoney", source, 0, price);
            _lastUserBroadcastSent[steam] = API.GetGameTimer();
            player.TriggerEvent("mailbox:displayCustomMessage", LoadConfig.Langs["TipOnBroadcastSent"]);
            try
            {
                dynamic connectedUsers = CORE.getUsers(); // return a Dictionary of <SteamID, User>

                foreach (KeyValuePair<string, dynamic> user in connectedUsers)
                {
                    Player p = GetPlayer(user.Value.source);
                    if (p != null)
                    {
                        GetPlayer(user.Value.source).TriggerEvent("mailbox:broadcast:receive", $"{message}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        
        private Player GetPlayer(int handle)
        {
            Player p = null;

            try
            {
                PlayerList pl = new PlayerList();
                p = pl[handle];
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Info] Player Not Found: {ex.Message}");
                return p;
            }

            return p;
        }

        private void GetMailboxMessages([FromSource]Player player)
        {
            try
            {
                int source = int.Parse(player.Handle);
                string steam = "steam:" + player.Identifiers["steam"];
                dynamic sourceCharacter = CORE.getUser(source).getUsedCharacter;
                Exports["ghmattimysql"].execute("SELECT * FROM mailbox_mails WHERE receiver_id = ? AND receiver_firstname = ? AND receiver_lastname = ?", new object[]
                {
                    steam,
                    sourceCharacter.firstname,
                    sourceCharacter.lastname
                }, new Action<dynamic>(
                    (userLetters) =>
                    {
                        /*letters: Array<{
                         id,
                         sender_id,
                         sender_firstname,
                         sender_lastname,
                         receiver_id,
                         receiver_firstname,
                         receiver_lastname,
                         message,
                         opened,
                         received_at
                         }
                         >*/
                        List<string> messages = new List<string>();
                        foreach (dynamic mailboxMessage in userLetters)
                        {
                            MailboxMessage message = new MailboxMessage().FromJson(mailboxMessage);
                            messages.Add(JsonConvert.SerializeObject(message));
                        }

                        player.TriggerEvent("mailbox:message:setMessages", messages);
                    }));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        
        private void GetMailboxUsers([FromSource]Player player)
        {
            try
            {
                int resfreshRate = int.Parse(LoadConfig.Config["TimeBetweenUsersRefresh"].ToString()); // In seconds
                
                if (resfreshRate > 0 && _lastUsersRefresh + (1000 * resfreshRate) < API.GetGameTimer())
                {
                    RefreshUsersCache();
                }
                
                List<string> users = new List<string>();
                foreach (MailboxUser user in _usersCache)
                {
                    users.Add(JsonConvert.SerializeObject(user));
                }
                
                player.TriggerEvent("mailbox:message:setUsers", users);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void RefreshUsersCache()
        {
            Exports["ghmattimysql"].execute("SELECT identifier, firstname, lastname FROM characters", new object[]{}, new Action<dynamic>(
                (mailboxUsers) =>
                {
                    /*users: Array<{
                     identifier,
                     firstname,
                     lastname
                     }
                     >*/
                    _usersCache.Clear();
                    foreach (dynamic mailboxUser in mailboxUsers)
                    {
                        MailboxUser user = new MailboxUser().FromJson(mailboxUser);
                        _usersCache.Add(user);
                    }
                    
                    _usersCache.Sort(delegate(MailboxUser a, MailboxUser b)
                    {
                        string aName = $"{a.firstname} {a.lastname}";
                        string bName = $"{b.firstname} {b.lastname}";
                        
                        return aName.CompareTo(bName);
                    });
                    _lastUsersRefresh = API.GetGameTimer();
                }));
        }

        private void UpdateMessages([FromSource] Player player, dynamic toDelete, dynamic toMarkAsOpened)
        {
            try
            {
                if (toDelete.Count > 0)
                {
                    Exports["ghmattimysql"].execute("DELETE FROM mailbox_mails WHERE id IN (?)", new object[] {toDelete});
                }

                if (toMarkAsOpened.Count > 0)
                {
                    Exports["ghmattimysql"].execute("UPDATE mailbox_mails SET opened = true WHERE id IN (?);", new object[] {toMarkAsOpened});   
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
    }
    
    struct MailboxMessage
    {
        public string id;
        public string steam;
        public string firstname;
        public string lastname;
        public bool opened;
        public DateTime received_at;
        public string message;

        public MailboxMessage FromJson(dynamic json)
        {
            id = json.id.ToString();
            firstname = json.sender_firstname;
            lastname = json.sender_lastname;
            steam = json.sender_id;
            opened = json.opened;
            received_at =  (new DateTime(1970, 1, 1)).AddMilliseconds(json.received_at);
            message = json.message;

            return this;
        }
    }
    
    struct MailboxUser
    {
        public string steam;
        public string firstname;
        public string lastname;
        
        public MailboxUser FromJson(dynamic json)
        {
            steam = json.identifier;
            firstname = json.firstname;
            lastname = json.lastname;

            return this;
        }
    }
}
