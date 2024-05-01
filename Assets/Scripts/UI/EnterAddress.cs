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
        [SerializeField] private Core core;

        public async void JoinTranslation()
        {
            var assistant = Assistant.GetInstance(translationImage);
            await assistant.ConnectToHost(inputField.text);
            translationImage.gameObject.SetActive(true);
            tracker.enabled = true;
            core.IsHost = false;
            gameObject.SetActive(false);
        }
    }
}