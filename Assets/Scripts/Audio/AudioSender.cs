using CVAssistant.Network;
using System.Collections;
using UnityEngine;

namespace CVAssistant.Audio
{
    public class AudioSender : MonoBehaviour
    {
        private SocketHandler socketHandler;
        private int clipSize = 11200;

        public void SetSocketHandler(SocketHandler value)
        {
            socketHandler = value;
        }

        public void StartSendingAudio()
        {
            var clip = Microphone.Start(Microphone.devices[0], true, 16, 44100);
            StartCoroutine(nameof(SendAudio), clip);
        }

        public void StopSendingAudio()
        {
            StopCoroutine(nameof(SendAudio));
        }

        private IEnumerator SendAudio(AudioClip clip)
        {
            var lastPosition = 0;
            var previousMicPosition = Microphone.GetPosition(Microphone.devices[0]);
            var micLoop = 0;
            var sendLoop = 0;
            while (true)
            {
                yield return new WaitUntil(() => Microphone.GetPosition(Microphone.devices[0]) - lastPosition > clipSize 
                    || micLoop > sendLoop && Microphone.GetPosition(Microphone.devices[0]) + clip.samples - lastPosition > clipSize);
                if(previousMicPosition < Microphone.GetPosition(Microphone.devices[0]))
                {
                    micLoop++;
                }
                previousMicPosition = Microphone.GetPosition(Microphone.devices[0]);
                socketHandler.TrySendAudio(clip, lastPosition, lastPosition + clipSize);
                if(lastPosition + clipSize >= clip.samples)
                {
                    lastPosition = 0;
                    sendLoop++;
                }
                else
                {
                    lastPosition += clipSize;
                }
            }
        }
    }
}
