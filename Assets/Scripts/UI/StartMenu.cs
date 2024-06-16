using CVAssistant.Audio;
using CVAssistant.CameraImageProcessing;
using CVAssistant.Network;
using CVAssistant.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CVAssistant.UI
{
    public class StartMenu : MonoBehaviour
    {
        [SerializeField] private GameObject hostUI;
        [SerializeField] private TextMeshProUGUI address;
        [SerializeField] private CVCameraImageProcessor cameraImageProcessor;
        [SerializeField] private RawImage translationImage;
        [SerializeField] private Button joinButton;
        [SerializeField] private Core core;

        private void Start()
        {
#if UNITY_EDITOR
#elif UNITY_ANDROID
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            joinButton.gameObject.SetActive(false);
#endif
        }

        private void Update()
        {
#if UNITY_EDITOR
#elif UNITY_ANDROID
            if (Input.GetKeyUp(KeyCode.JoystickButton0))
            {
                StartTranslation();
            }
#endif
        }

        public void StartTranslation()
        {
            translationImage.gameObject.SetActive(true);
            var host = Host.GetInstance();
            host.SetImage(translationImage);
            //host.SetAudioReceiver((AudioReceiver)FindObjectOfType(typeof(AudioReceiver)));
            //host.SetAudioSender((AudioSender)FindObjectOfType(typeof(AudioSender)));
            host.StartListening();
            address.text = host.Address.ToString();
            cameraImageProcessor.enabled = true;
            core.IsHost = true;
            hostUI.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}