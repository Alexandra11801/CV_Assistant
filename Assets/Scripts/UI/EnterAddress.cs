using CVAssistant.Audio;
using CVAssistant.Network;
using CVAssistant.ObjectsTracking;
using CVAssistant.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CVAssistant.UI
{
    public class EnterAddress : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private RawImage translationImage;
        [SerializeField] private ObjectsTracker tracker;
        [SerializeField] private GameObject startMenu;
        [SerializeField] private Core core;

        public async void JoinTranslation()
        {
            translationImage.gameObject.SetActive(true);
            var assistant = Assistant.GetInstance();
            assistant.SetImage(translationImage);
            //assistant.SetAudioReceiver((AudioReceiver)FindObjectOfType(typeof(AudioReceiver)));
            //assistant.SetAudioSender((AudioSender)FindObjectOfType(typeof(AudioSender)));
            assistant.StartMenu = startMenu;
            await assistant.ConnectToHost(inputField.text);
            tracker.enabled = true;
            core.IsHost = false;
            gameObject.SetActive(false);
        }
    }
}