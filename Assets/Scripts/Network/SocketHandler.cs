using CVAssistant.Audio;
using NSpeex;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace CVAssistant.Network
{
    public class SocketHandler
    {
        public struct AudioInfo
        {
            byte[] audioData;
            int dataLength;

            public byte[] AudioData => audioData;
            public int DataLength => dataLength;

            public AudioInfo(byte[] audioData, int dataLength)
            {
                this.audioData = audioData;
                this.dataLength = dataLength;
            }
        }

        protected static SocketHandler instance;
        protected IPAddress address;
        protected CancellationTokenSource cancellationTokenSource;

        protected int imagePort = 13000;
        protected TcpClient imageClient;
        protected NetworkStream imageStream;

        protected int selectionPort = 12000;
        protected TcpClient selectionClient;
        protected NetworkStream selectionStream;

        protected int audioPort = 11000;
        protected TcpClient audioClient;
        protected NetworkStream audioStream;
        protected AudioSender audioSender;
        protected AudioReceiver audioReceiver;

        public IPAddress Address => address;

        public void SetAudioSender(AudioSender value)
        {
            audioSender = value;
            audioSender.SetSocketHandler(this);
        }

        public void SetAudioReceiver(AudioReceiver value)
        {
            audioReceiver = value;
        }

        public virtual void Disconnect(Exception e) 
        {
            if(e != null)
            {
                Debug.LogException(e);
            }
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            imageClient.Close();
            selectionClient.Close();
            audioClient.Close();
            imageStream.Flush();
            selectionStream.Flush();
            audioStream.Flush();
            audioSender.StopSendingAudio();
            audioReceiver.StopPlay();
        }

        public async Task TrySendAudio(AudioClip clip, int startPosition, int endPosition)
        {
            try
            {
                if(endPosition > startPosition)
                {
                    var floats = new float[endPosition - startPosition];
                    clip.GetData(floats, startPosition);
                    var info = EncodeAudio(floats);
                    var floats2 = DecodeAudio(info.AudioData, info.DataLength, floats.Length);
                    audioReceiver.AddFloats(floats);
                    var task = Task.Run(() => SendAudio(floats, audioStream));
                    await task;
                }
            }
            catch (Exception ex)
            {
                Disconnect(ex);
            }
        }

        protected async void ReceiveAudioLoop()
        {
            while(audioClient.Connected)
            {
                try
                {
                    var task = Task.Run(() => ReceiveAudio(audioStream, cancellationTokenSource.Token));
                    var floats = await task;
                    audioReceiver.AddFloats(floats);
                }
                catch (Exception ex) 
                {
                    Disconnect(ex);
                } 
            }
        }

        protected async Task<int> ReadInt(NetworkStream stream, CancellationToken cancellationToken)
        {
            var bytes = new byte[4];
            var bytesCount = 0;
            while (bytesCount < 4)
            {
                bytesCount += await stream.ReadAsync(bytes, bytesCount, 4 - bytesCount, cancellationToken);
            }
            return BitConverter.ToInt32(bytes);
        }

        protected async Task WriteInt(NetworkStream stream, int value)
        {
            await stream.WriteAsync(BitConverter.GetBytes((uint)value), 0, 4);
        }

        public async Task SendAudio(float[] floats, NetworkStream stream)
        {
            await WriteInt(stream, floats.Length); 
            var encodedAudioInfo = EncodeAudio(floats);
            await WriteInt(stream, encodedAudioInfo.DataLength);
            await WriteInt(stream, encodedAudioInfo.AudioData.Length);
            await stream.WriteAsync(encodedAudioInfo.AudioData, 0, encodedAudioInfo.AudioData.Length);
        }

        private AudioInfo EncodeAudio(float[] floats)
        {
            var shorts = new short[floats.Length];
            for (var i = 0; i < shorts.Length; i++)
            {
                shorts[i] = (short)Mathf.FloorToInt(short.MaxValue * (floats[i] + 1) * 2);
            }
            var encoder = new SpeexEncoder(BandMode.Wide);
            byte[] bytes = new byte[shorts.Length];
            var dataLength = encoder.Encode(shorts, 0, shorts.Length, bytes, 0, bytes.Length);
            var info = new AudioInfo(bytes, dataLength);
            return info;
        }

        public async Task<float[]> ReceiveAudio(NetworkStream stream, CancellationToken cancellationToken)
        {
            var decodedLength = await ReadInt(stream, cancellationToken);
            var realDataLength = await ReadInt(stream, cancellationToken);
            var bytesLength = await ReadInt(stream, cancellationToken);
            var bytes = new byte[bytesLength];
            var bytesCount = 0;
            while (bytesCount < bytesLength)
            {
                bytesCount += await audioStream.ReadAsync(bytes, bytesCount, bytesLength - bytesCount, cancellationToken);
            }
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return null;
            }
            else
            {
                var floats = DecodeAudio(bytes, realDataLength, decodedLength);
                return floats;
            }
        }

        private float[] DecodeAudio(byte[] bytes, int dataLength, int decodedLength)
        {
            var shorts = new short[decodedLength];
            var decoder = new SpeexDecoder(BandMode.Wide);
            decoder.Decode(bytes, 0, dataLength, shorts, 0, false);
            var floats = new float[shorts.Length];
            for(var i = 0; i < floats.Length; i++)
            {
                floats[i] = shorts[i] / (2 * (float)short.MaxValue) - 1;
            }
            return floats;
        }
    }
}
