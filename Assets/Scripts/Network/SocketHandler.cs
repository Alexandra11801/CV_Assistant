using CVAssistant.Audio;
using NSpeex;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace CVAssistant.Network
{
    public class SocketHandler
    {
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

        //Функция передачи аудио отключена, так как выбранные инструменты не удовлетворяют требованиям производительности. Планируется дальнейшая отладка и/или переработка функции.

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
            //audioClient.Close();
            imageStream.Flush();
            selectionStream.Flush();
            //audioStream.Flush();
            //audioSender.StopSendingAudio();
            //audioReceiver.StopPlay();
        }

        public async Task TrySendAudio(float[] floats)
        {
            try
            {
                var task = Task.Run(() => SendAudio(floats, audioStream, cancellationTokenSource.Token));
                await task;
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

        protected async Task WriteInt(NetworkStream stream, int value, CancellationToken cancellationToken)
        {
            await stream.WriteAsync(BitConverter.GetBytes((uint)value), 0, 4, cancellationToken);
        }

        public async Task SendAudio(float[] floats, NetworkStream stream, CancellationToken cancellationToken)
        {
            await WriteInt(stream, floats.Length, cancellationToken);
            var encodedAudio = EncodeAudio(floats);
            await WriteInt(stream, encodedAudio.Length, cancellationToken);
            await stream.WriteAsync(encodedAudio, 0, encodedAudio.Length, cancellationToken);
        }

        private byte[] EncodeAudio(float[] floats)
        {
            var shorts = new short[floats.Length];
            for (var i = 0; i < shorts.Length; i++)
            {
                shorts[i] = (short)Mathf.FloorToInt(short.MaxValue * ((floats[i] + 1) / 2));
            }
            var encoder = new SpeexEncoder(BandMode.Narrow);
            byte[] encoded = new byte[shorts.Length];
            var dataLength = encoder.Encode(shorts, 0, shorts.Length, encoded, 0, encoded.Length);
            Array.Resize(ref encoded, dataLength);
            return encoded;
        }

        public async Task<float[]> ReceiveAudio(NetworkStream stream, CancellationToken cancellationToken)
        {
            var decodedLength = await ReadInt(stream, cancellationToken);
            var encodedLength = await ReadInt(stream, cancellationToken);
            var bytes = new byte[encodedLength];
            var bytesCount = 0;
            while (bytesCount < encodedLength)
            {
                bytesCount += await audioStream.ReadAsync(bytes, bytesCount, encodedLength - bytesCount, cancellationToken);
            }
            var floats = DecodeAudio(bytes, decodedLength);
            return floats;
        }

        private float[] DecodeAudio(byte[] encoded, int decodedLength)
        {
            var shorts = new short[decodedLength];
            var decoder = new SpeexDecoder(BandMode.Narrow);
            decoder.Decode(encoded, 0, encoded.Length, shorts, 0, false);
            var floats = new float[decodedLength];
            for(var i = 0; i < floats.Length; i++)
            {
                floats[i] = (shorts[i] / (float)short.MaxValue) * 2 - 1;
            }
            return floats;
        }
    }
}
