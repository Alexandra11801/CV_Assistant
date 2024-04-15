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
            var host = Host.GetInstance(translationImage);
            host.Listen();
            address.text = host.Address.ToString();
            translationImage.gameObject.SetActive(true);
            cameraImageProcessor.enabled = true;
            core.IsHost = true;
            hostUI.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}