using CVAssistant.CameraImageProcessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rect = OpenCvSharp.Rect;

namespace CVAssistant.Network
{
    public class Host 
    {
        private static Host instance;
        private IPAddress address;
        private int port = 13000;
        private TcpListener listener;
        private TcpClient client;
        private NetworkStream stream;
        private CVCameraImageProcessor imageProcessor;
        private List<Rect> selectRects;

        public IPAddress Address => address;
        public List<Rect> SelectRects => selectRects;

        private Host(RawImage image)
        {
            imageProcessor = image.GetComponent<CVCameraImageProcessor>();
            var hostAddresses = Dns.GetHostAddresses(Dns.GetHostName());
            address = hostAddresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            listener = new TcpListener(address, port);
            selectRects = new List<Rect>();
        }

        public static Host GetInstance(RawImage image)
        {
            if (instance == null)
            {
                instance = new Host(image);
            }
            return instance;
        }

        public async void Listen()
        {
            listener.Start();
            while (true)
            {
                client = await listener.AcceptTcpClientAsync();
                stream = client.GetStream();
                WriteToStream();
            }
        }

        private async void WriteToStream()
        {
            while (client.Connected)
            {
                try
                {
                    var texture = imageProcessor.ClearTexture;
                    await SendTexture(texture);
                    await ReceiveSelection();
                }
                catch (Exception ex)
                {
                    selectRects.Clear();
                }
            }
        }

        private async Task SendTexture(Texture2D texture)
        {
            var bytes = ImageConversion.EncodeToJPG(texture, 10);
            await stream.WriteAsync(BitConverter.GetBytes((uint)texture.width), 0, 4);
            await stream.WriteAsync(BitConverter.GetBytes((uint)texture.height), 0, 4);
            var bytesCoded = Convert.ToBase64String(bytes);
            var bytesCodedArray = Encoding.UTF8.GetBytes(bytesCoded);
            await stream.WriteAsync(BitConverter.GetBytes(bytesCodedArray.Length), 0, 4);
            await stream.WriteAsync(bytesCodedArray, 0, bytesCodedArray.Length);
        }

        private async Task ReceiveSelection()
        {
            var rectCount = await ReadInt();
            var newRects = new List<Rect>();
            for(int i = 0; i < rectCount; i++)
            {
                var x = await ReadInt();
                var y = await ReadInt();
                var width = await ReadInt();
                var height = await ReadInt();
                var selectRect = new Rect(x, y, width, height);
                newRects.Add(selectRect);
            }
            selectRects = newRects;
        }

        private async Task<int> ReadInt()
        {
            var bytes = new byte[4];
            var bytesCount = 0;
            while (bytesCount < 4)
            {
                bytesCount += await stream.ReadAsync(bytes, bytesCount, 4 - bytesCount);
            }
            return BitConverter.ToInt32(bytes);
        }
    }
}