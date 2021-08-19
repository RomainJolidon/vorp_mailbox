using System;
using System.Collections;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;

namespace VORPMailboxServer
{
    public class VorpMailboxServer: BaseScript
    {
        public static dynamic CORE;
        // TODO cache au lancement des users
        private List<MailboxUser> _usersCache = new List<MailboxUser>();
        private int _lastUsersRefresh = 0;
        
        public VorpMailboxServer()
        {
            //Event for send new message
            EventHandlers["mailbox:message:send"] += new Action<Player, dynamic, string>(SendNewMessage);
            EventHandlers["mailbox:message:getMessages"] += new Action<Player>(GetMailboxMessages);
            EventHandlers["mailbox:message:updateMessages"] += new Action<Player, dynamic , dynamic>(UpdateMessages);
            EventHandlers["mailbox:message:getUsers"] += new Action<Player>(GetMailboxUsers);
            
            //GetCore Event
            TriggerEvent("getCore",new Action<dynamic>((dic) =>
            {
                CORE = dic;
            }));
        }

        private void SendNewMessage([FromSource]Player player, dynamic receiver, string message)
        {
            int source = int.Parse(player.Handle);
            string steam = "steam" + player.Identifiers["steam"];
            
            
            dynamic sourceCharacter = CORE.getUser(source).getUsedCharacter;

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
                    player.TriggerEvent("mailbox:message:receive", $"{sourceCharacter.firstname} {sourceCharacter.lastname}");
                    return;
                }
            }
            
        }

        private void GetMailboxMessages([FromSource]Player player)
        {
            Debug.WriteLine("Getting mails");
            try
            {
                string steam = "steam:" + player.Identifiers["steam"];
                Exports["ghmattimysql"].execute("SELECT * FROM mailbox_mails WHERE receiver_id = ?", new object[] {steam}, new Action<dynamic>(
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
                         opened
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
            Debug.WriteLine("Getting users");
            try
            {
                int resfreshRate = int.Parse(LoadConfig.Config["TimeBetweenUsersRefresh"].ToString()); // In minutes
                
                if (resfreshRate != 0 && _lastUsersRefresh + 1000 * 60 * resfreshRate < API.GetGameTimer())
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
                }));
        }

        private void UpdateMessages([FromSource] Player player, dynamic toDelete, dynamic toMarkAsOpened)
        {
            try
            {
                if (toDelete.Count > 0)
                {
                    Exports["ghmattimysql"].execute("DELETE FROM mailbox_mails WHERE id = ?", new object[] {toDelete});
                }

                if (toMarkAsOpened.Count > 0)
                {
                    Exports["ghmattimysql"].execute("UPDATE mailbox_mails SET opened = true WHERE id = ?;", new object[] {toMarkAsOpened});   
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
        public string message;

        public MailboxMessage FromJson(dynamic json)
        {
            id = json.id.ToString();
            firstname = json.sender_firstname;
            lastname = json.sender_lastname;
            steam = json.sender_id;
            opened = json.opened;
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
