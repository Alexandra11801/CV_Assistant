using CVAssistant.Network;
using UnityEngine;

namespace CVAssistant.Scripts.Core
{
    public class Core : MonoBehaviour
    {
        private bool isHost;

        public bool IsHost {  get { return isHost; } set { isHost = value; } }

        private void Update()
        {
#if UNITY_EDITOR
#elif UNITY_ANDROID
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                Application.Quit();
            }
#endif
        }

        private void OnApplicationQuit()
        {
            Assistant.GetInstance(null).Disconnect();
        }
    }
}
