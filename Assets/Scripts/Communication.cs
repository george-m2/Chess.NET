using System.Threading;
using AsyncIO;
using ChessNET;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using PGNDelegate;

namespace Communication
{
    public class Client : MonoBehaviour
    {
        // Singleton pattern to easily access the instance
        public static Client Instance { get; private set; }

        private RequestSocket _requester;
        private PGNExporter _pgnExporter;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _pgnExporter = FindObjectOfType<PGNExporter>();
            _requester = new RequestSocket();
            _requester.Connect("tcp://localhost:5555");
        }

        public void SendPGNUpdate()
        {
            string pgnString = _pgnExporter.ConvertCurrentMoveToSAN();
            new Thread(() =>
            {
                _requester.SendFrame(pgnString);
                string message = _requester.ReceiveFrameString();
                Debug.Log("Received: " + message);
                // Handle the received best move
            }).Start();
        }

        private void OnDestroy()
        {
            _requester?.Dispose();
        }
    }

    public class Requester : RunAbleThread
    {
        private readonly string _pgnString;

        public Requester(string pgnString)
        {
            _pgnString = pgnString;
        }

        protected override void Run()
        {
            ForceDotNet.Force();
            using (RequestSocket client = new RequestSocket())
            {
                client.Connect("tcp://localhost:5555");

                for (int i = 0; i < 10 && Running; i++)
                {
                    Debug.Log("Sending PGN");
                    client.SendFrame(_pgnString);

                    string message = null;
                    bool gotMessage = false;
                    while (Running)
                    {
                        gotMessage = client.TryReceiveFrameString(out message);
                        if (gotMessage) break;
                    }

                    if (gotMessage) Debug.Log("Received " + message);
                }
            }

            NetMQConfig.Cleanup();
        }
    }

    public abstract class RunAbleThread
    {
        private readonly Thread _runnerThread;

        protected RunAbleThread()
        {
            _runnerThread = new Thread(Run);
        }

        protected bool Running { get; private set; }

        protected abstract void Run();

        public void Start()
        {
            Running = true;
            _runnerThread.Start();
        }

        public void Stop()
        {
            Running = false;
            _runnerThread.Join();
        }
    }
}