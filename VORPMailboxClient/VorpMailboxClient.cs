using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static CitizenFX.Core.Native.API;

namespace VORPMailboxClient
{
    public class VorpMailboxClient : BaseScript
    {
        private bool _mailboxOpened = false;
        private List<MailboxMessage> _messagesCache = new List<MailboxMessage>();
        private bool _canRefreshMessages = true;

        public VorpMailboxClient()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
            EventHandlers["mailbox:message:receive"] += new Action<string>(ReceiveMessage);
            
            EventHandlers["mailbox:message:setMessages"] += new Action<dynamic>(SetMessages);
            EventHandlers["mailbox:message:setUsers"] += new Action<dynamic>(SetUsers);
            
            RegisterNuiCallbackType("close");
            EventHandlers["__cfx_nui:close"] += new Action<ExpandoObject>(CloseUI);
            
            RegisterNuiCallbackType("send");
            EventHandlers["__cfx_nui:send"] += new Action<ExpandoObject>(SendMessage);
        }
        
        
        private void OnClientResourceStart(string resourceName)
        {
            // check if actual initializing resource is our resource. this avoid initializing a resource two times.
            if (GetCurrentResourceName() != resourceName) return;

            Tick += OnTick;
            
        }

        [Tick]
        private async Task OnTick()
        {
            await Delay(0);
            try
            {
                if (!_mailboxOpened && IsNearbyMailbox())
                {
                    Utils.DisplayText(GetConfig.Langs["TextNearMailboxLocation"]);
                
                    if (!_mailboxOpened && IsControlJustReleased(0, 0xB2F377E8)) // f key, see: https://forum.cfx.re/t/keybind-hashes/1666877
                    {
                        OpenUI();
                        await Delay(300);
                    }
                }
                else
                {
                    Utils.StopDisplay();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private bool IsNearbyMailbox()
        {
            List<Vector3> locations = GetConfig.Locations;

             foreach (var location in locations) 
             {
                if (IsPlayerNearCoords((int)location.X , (int)location.Y, (int)location.Z, 2f))
                {
                    return true;
                }
             }
             return false;
        }
        
        private bool IsPlayerNearCoords(int x, int y, int z, float nearDst = 1f)
        {
            Vector3 playerCoords = GetEntityCoords(PlayerPedId(), false, false);
            float distance = GetDistanceBetweenCoords(playerCoords.X, playerCoords.Y, playerCoords.Z, x, y, z, true);

            return distance < nearDst;
        }

        private void SendToServer(string steam, string receiverFirstname, string receiverLastname, string message)
        {
            dynamic receiver = new Dictionary<string, object>() {
                {"steam", steam},
                {"firstname", receiverFirstname},
                {"lastname", receiverLastname},
            };
            TriggerServerEvent("mailbox:message:send", new object[]{receiver, message});
        }
        
        private void ReceiveMessage(string author)
        {
            try
            {
                if (author.Length == 0) return;
                TriggerEvent("vorp:TipRight", $"{GetConfig.Langs["TipOnMessageReceived"]} {author}", 5000);
                _canRefreshMessages = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void SendMessage(ExpandoObject payload)
        {
            MailboxUser author = JsonConvert.DeserializeObject<MailboxUser>(((dynamic) payload).author);
            string message = ((dynamic) payload).message;

            SendToServer(author.steam, author.firstname, author.lastname, message);
        }

        private void SetMessages(dynamic data)
        {
            try
            {
                if (_canRefreshMessages == true)
                {
                    List<object> messageList = data as List<object>;
                    _messagesCache.Clear();
                    foreach (object message in messageList)
                    {
                        MailboxMessage parsedMessage = JsonConvert.DeserializeObject<MailboxMessage>(message.ToString());
                        _messagesCache.Add(parsedMessage);
                    }

                    SendNuiMessage(JsonConvert.SerializeObject(new
                    {
                        action = "set_messages",
                        messages = JsonConvert.SerializeObject(messageList)
                    }));
                    _canRefreshMessages = false;
                }
            }
            catch (Exception e)
            {
                SendChatMessage(e.Message);
                Debug.WriteLine(e.Message);
            }
        }
        
        private void SetUsers(dynamic data)
        {
            try
            {
                List<object> userList = data as List<object>;

                SendNuiMessage(JsonConvert.SerializeObject(new
                {
                    action = "set_users",
                    users = JsonConvert.SerializeObject(userList)
                }));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        
        private void SetUILanguage()
        {
            try
            {
                SendNuiMessage(JsonConvert.SerializeObject(new
                {
                    action = "set_language",
                    language = JsonConvert.SerializeObject(GetConfig.UILangs)
                }));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        
        private void OpenUI()
        {
            SetUILanguage();
            SetNuiFocus(true, true);
            SendNuiMessage(JsonConvert.SerializeObject(new
            {
                action = "open"
            }));
            _mailboxOpened = true;

            if (_canRefreshMessages)
            {
                TriggerServerEvent("mailbox:message:getMessages");
            }
            
            TriggerServerEvent("mailbox:message:getUsers");
        }

        private void CloseUI(ExpandoObject payload)
        {
            try
            {
                // First close UI. In case of fail, the user will not be stuck focused on the UI
                SetNuiFocus(false, false);
                SendNuiMessage(JsonConvert.SerializeObject(new
                {
                    action = "close"
                }));
                _mailboxOpened = false;
                
                // Then parse received messages from UI
                List<MailboxMessage> messages = JsonConvert.DeserializeObject<List<MailboxMessage>>(((dynamic) payload).messages);

                List<string> toDelete = new List<string>();
                List<string> toMarkAsOpened = new List<string>();

                // pass through all messages and check deleted one and ones that as been opened
                foreach (MailboxMessage message in _messagesCache)
                {
                    int idx = messages.FindIndex(m => m.id == message.id);
                    if (idx == -1) // if index is not found, then messages is deleted
                    {
                        toDelete.Add(message.id);
                    }
                    else if (message.opened == false && messages[idx].opened == true) // if cached message is not marked as opened but received message is, update
                    {
                        toMarkAsOpened.Add(message.id);
                    }
                }
            
                // Send data to server
                TriggerServerEvent("mailbox:message:updateMessages", toDelete, toMarkAsOpened);
                
                // Finally, Cache received messages from UI as most recent messages
                _messagesCache.Clear();
                foreach (MailboxMessage message in messages)
                {
                    _messagesCache.Add(message);
                }
            }
            catch (Exception e)
            {
                SendChatMessage(e.Message);
                Debug.WriteLine(e.Message);
            }
        }

        private void SendChatMessage(string message)
        {
            TriggerEvent("chat:addMessage", new
            {
                color = new[] {0, 255, 0},
                args = new[] {"[Mailbox]", message}
            });
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
            id = json.id;
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
