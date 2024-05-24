using CVAssistant.CameraImageProcessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Rect = OpenCvSharp.Rect;

namespace CVAssistant.Network
{
    public class Host : SocketHandler
    {
        private TcpListener imageListener;
        private TcpListener selectionListener;
        private TcpListener audioListener;
        private CVCameraImageProcessor imageProcessor;
        private List<Rect> selectRects;

        public List<Rect> SelectRects => selectRects;

        public static Host GetInstance()
        {
            if (instance == null)
            {
                var host = new Host();
                host.Init();
                instance = host;
            }
            return (Host)instance;
        }

        public void SetImage(RawImage image)
        {
            imageProcessor = image.GetComponent<CVCameraImageProcessor>();
        }

        protected void Init()
        {
            var hostAddresses = Dns.GetHostAddresses(Dns.GetHostName());
            address = hostAddresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            imageListener = new TcpListener(address, imagePort);
            selectionListener = new TcpListener(address, selectionPort);
            audioListener = new TcpListener(address, audioPort);
            selectRects = new List<Rect>();
        }

        public async void StartListening()
        {
            imageListener.Start();
            selectionListener.Start();
            audioListener.Start();
            while (true)
            {
                var imageListenerTask = ListenForImageRequest();
                var selectionListenerTask = ListenForSelectionRequest();
                var audioListenerTask = ListenForAudioRequest();
                await Task.WhenAll(imageListenerTask, selectionListenerTask, audioListenerTask);
                cancellationTokenSource = new CancellationTokenSource();
                WriteImageLoop();
                ReceiveSelectionLoop();
                //audioReceiver.StartPlay();
                //ReceiveAudioLoop();
            }
        }

        public async Task ListenForImageRequest()
        {
            imageClient = await imageListener.AcceptTcpClientAsync();
            imageStream = imageClient.GetStream();
        }

        public async Task ListenForSelectionRequest()
        {
            selectionClient = await selectionListener.AcceptTcpClientAsync();
            selectionStream = selectionClient.GetStream();
        }

        public async Task ListenForAudioRequest()
        {
            audioClient = await audioListener.AcceptTcpClientAsync(); 
            audioStream = audioClient.GetStream();
        }

        public override void Disconnect(Exception e)
        {
            base.Disconnect(e);
            selectRects.Clear();
        }

        private async void WriteImageLoop()
        {
            while (imageClient.Connected)
            {
                try
                {
                    var texture = imageProcessor.ClearTexture;
                    var task = Task.Run(() => SendTexture(texture));
                    await task;
                }
                catch (Exception ex)
                {
                    Disconnect(ex);
                }
            }
        }

        private async void ReceiveSelectionLoop()
        {
            while (selectionClient.Connected)
            {
                try
                {
                    var task = Task.Run(() => ReceiveSelection(cancellationTokenSource.Token));
                    await task;
                }
                catch (Exception ex)
                {
                    Disconnect(ex);
                }
            }
        }

        private async Task SendTexture(Texture2D texture)
        {
            var bytes = ImageConversion.EncodeToJPG(texture, 25);
            await WriteInt(imageStream, texture.width);
            await WriteInt(imageStream, texture.height);
            var bytesCoded = Convert.ToBase64String(bytes);
            var bytesCodedArray = Encoding.UTF8.GetBytes(bytesCoded);
            await WriteInt(imageStream, bytesCodedArray.Length);
            await imageStream.WriteAsync(bytesCodedArray, 0, bytesCodedArray.Length);
        }

        private async Task ReceiveSelection(CancellationToken cancellationToken)
        {
            var rectCount = await ReadInt(selectionStream, cancellationToken);
            var newRects = new List<Rect>();
            for(int i = 0; i < rectCount; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                else
                {
                    var x = await ReadInt(selectionStream, cancellationToken);
                    var y = await ReadInt(selectionStream, cancellationToken);
                    var width = await ReadInt(selectionStream, cancellationToken);
                    var height = await ReadInt(selectionStream, cancellationToken);
                    var selectRect = new Rect(x, y, width, height);
                    newRects.Add(selectRect);
                }
            }
            selectRects = newRects;
        }
    }
}