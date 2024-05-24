using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CVAssistant.Audio
{
    public class AudioReceiver : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        private int currentPosition;
        private int audioLoopCount;
        private int loadedDataLoopCount;

        public void StartPlay()
        {
            audioSource.clip = AudioClip.Create("", 44100 * 16, 1, 44100, false);
            currentPosition = 0;
            audioLoopCount = 0;
            loadedDataLoopCount = 0;
            audioSource.Play();
            StartCoroutine(nameof(WatchAudioPosition));
        }

        public void StopPlay()
        {
            audioSource.Stop();
            StopCoroutine(nameof(WatchAudioPosition));
        }

        public void AddFloats(float[] floats)
        {
            audioSource.clip.SetData(floats, currentPosition);
            if(currentPosition + floats.Length < audioSource.clip.samples - 1)
            {
                currentPosition += floats.Length;
            }
            else
            {
                currentPosition = 0;
                loadedDataLoopCount++;
            }
        }

        private IEnumerator WatchAudioPosition()
        {
            while (true)
            {
                if(audioSource.timeSamples >= currentPosition && audioLoopCount == loadedDataLoopCount)
                {
                    audioSource.Pause();
                    yield return new WaitUntil(() => audioSource.timeSamples < currentPosition || audioLoopCount < loadedDataLoopCount);
                    if (audioLoopCount < loadedDataLoopCount)
                    {
                        audioLoopCount = loadedDataLoopCount;
                    }
                    audioSource.UnPause();
                }
                yield return null;
            }
        }
    }
}
