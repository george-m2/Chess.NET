using System.Threading;
using AsyncIO;
using ChessNET;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using PGNDelegate;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Diagnostics; //potential ambiguity with UnityEngine.Debug.Log and System.Diagnostics.Debug.Log

namespace Communication
{
    public class Client : MonoBehaviour
    {
        private Process _cobraProcess = null;
        public void CreateEngineProcess()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "Assets/cobra";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;

            var _cobraProcess = new Process();
            _cobraProcess.StartInfo = startInfo;
            _cobraProcess.OutputDataReceived +=
                (sender, args) =>
                    UnityEngine.Debug .Log(args.Data); 
            if (_cobraProcess.Start())
            {
                UnityEngine.Debug.Log("Process started");
            }
            _cobraProcess.BeginOutputReadLine();
        }
        // Singleton pattern to easily access the instance
        public static Client Instance { get; private set; }

        private RequestSocket _requester;
        private PGNExporter _pgnExporter;
        private Chessboard _chessboard;

        private void Awake()
        {
            _chessboard = FindObjectOfType<Chessboard>();
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
            CreateEngineProcess();
            _pgnExporter = FindObjectOfType<PGNExporter>();
            _requester = new RequestSocket();
            _requester.Connect("tcp://localhost:5555");
        }
        
        public delegate void PGNReceivedHandler(string pgnString);

        public void ReceivePgnUpdate(PGNReceivedHandler callback)
        {
            string pgnString = _pgnExporter.ConvertCurrentMoveToSAN();
            new Thread(() =>
            {
                _requester.SendFrame(pgnString);
                string message = _requester.ReceiveFrameString();
                UnityEngine.Debug.Log("Received: " + message);

                // Use Unity's main thread to call the callback method and to find the Chessboard
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (_chessboard != null)
                    {
                        // Check if the current player is white before processing the move
                        if (_chessboard.isWhiteTurn) return;
                        callback(message);
                        _chessboard.ProcessReceivedMove(message);

                    }
                });
            }).Start();
        }

        
        private void OnDestroy()
        {
            if (_cobraProcess is { HasExited: false })
            {
                _cobraProcess.Kill();
                _cobraProcess.Dispose();
            }

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
                    UnityEngine.Debug.Log("Sending PGN");
                    client.SendFrame(_pgnString);

                    string message = null;
                    bool gotMessage = false;
                    while (Running)
                    {
                        gotMessage = client.TryReceiveFrameString(out message);
                        if (gotMessage) break;
                    }

                    if (gotMessage) UnityEngine.Debug.Log("Received " + message);
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