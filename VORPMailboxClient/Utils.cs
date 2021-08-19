using CitizenFX.Core;
using CitizenFX.Core.Native;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VORPMailboxClient
{
    class Utils : BaseScript
    {
        static int subtitleTimeEnd;
        static string subtitleText = null;

        [Tick]
        private static Task OnTick()
        {

            if (subtitleText != null)
            {
                DrawText(subtitleText, 23, 0.5F, 0.85F, 0.50F, 0.40F, 255, 255, 255, 255);

                if (subtitleTimeEnd <= API.GetGameTimer())
                {
                    subtitleText = null;
                }
            }
            
            foreach(Vector3 pos in GetConfig.Locations)
            {
                // Display Yellow halo on coords
                Function.Call((Hash)0x2A32FAA57B937173, -1795314153, pos.X, pos.Y, pos.Z, 0, 0, 0, 0, 0, 0, 1.0, 1.0, 0.9, 255, 255, 0, 155, 0, 0, 2, 0, 0, 0, 0);
            }

            return null;
        }

        public static void DisplayText(string text, int duration = 1000)
        {
            subtitleTimeEnd = API.GetGameTimer() + duration;
            subtitleText = text;
        }
        
        public static void StopDisplay()
        {
            subtitleText = null;
        }

        public static void DrawText(string text, int fontId, float x, float y, float scaleX, float scaleY, int r, int g, int b, int a)
        {
            // Draw Text
            API.SetTextScale(scaleX, scaleY);
            API.SetTextColor(r, g, b, a);
            API.SetTextCentre(true);
            Function.Call((Hash)0xADA9255D, fontId); // Loads the font requested
            API.DisplayText(Function.Call<long>(Hash._CREATE_VAR_STRING, 10, "LITERAL_STRING", text), x, y);

            // Draw Backdrop
            float lineLength = (float) text.Length / 100 * 0.66F;
            DrawTexture("boot_flow", "selection_box_bg_1d", x, y, lineLength, 0.035F, 0F, 0, 0, 0, 200);
        }

        public static async void DrawTexture(string textureDict, string textureName, float x, float y, float width, float height, float rotation, int r, int g, int b, int a)
        {
            if (!API.HasStreamedTextureDictLoaded(textureDict))
            {
                API.RequestStreamedTextureDict(textureDict, false);
                while (!API.HasStreamedTextureDictLoaded(textureDict))
                {
                    Debug.WriteLine($"{textureDict}.{textureName} is waiting to load...");
                    await BaseScript.Delay(100);
                }
            }
            API.DrawSprite(textureDict, textureName, x, y + 0.015F, width, height, rotation, r, g, b, a, true);
        }

        public static async Task<bool> LoadModel(string model)
        {
            int hash = API.GetHashKey(model);
            if (!API.IsModelValid((uint)hash))
            {
                Debug.WriteLine($"Model {model} is not valid and could not be loaded.");
                return false;
            }
            if (!API.HasModelLoaded((uint)hash))
            {
                API.RequestModel((uint)hash, true);
                while (!API.HasModelLoaded((uint)hash))
                {
                    Debug.WriteLine($"Model {model} is waiting to load...");
                    await BaseScript.Delay(100);
                }
            }
            return true;
        }
    }
}