using System;
using System.Threading;
using ChessNET;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using PGNDelegate;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Diagnostics;
using GameUIManager;
using Debug = UnityEngine.Debug; //potential ambiguity with UnityEngine.Debug.Log and System.Diagnostics.Debug.Log

namespace Communication
{
    public class Client : MonoBehaviour
    {
        private Process _cobraProcess;
        string bestMoveMessage;

        public void CreateEngineProcess()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Application.streamingAssetsPath + "/cobra";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;

            _cobraProcess = new Process();
            _cobraProcess.StartInfo = startInfo;
            if (_cobraProcess.Start())
            {
                Debug.Log("Process started");
                Debug.Log("Process started with ID: " + _cobraProcess.Id);
            }

            _cobraProcess.BeginOutputReadLine();
        }

        // Singleton pattern to easily access the instance
        public static Client Instance { get; private set; }

        private RequestSocket _requester;
        private PGNExporter _pgnExporter;
        private Chessboard _chessboard;
        private UIManager _ui;

        private void Awake()
        {
            _chessboard = FindObjectOfType<Chessboard>();
            if (Instance == null)
            {
                Instance = this;
                CreateEngineProcess();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _pgnExporter = FindObjectOfType<PGNExporter>();
            _ui = FindObjectOfType<UIManager>();
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

        public void GracefulShutdown()
        {
            if (_requester != null)
                _requester.SendFrame("SHUTDOWN");
        }

        public delegate void BestMoveReceivedHandler(string bestMoveString);
        
        //TODO: refactor both SendGameOver and HandlePGN to use an event system
        public void SendGameOver(BestMoveReceivedHandler callbackMove)
        {
            if (_requester == null) return;
            _requester.SendFrame("GAME_END");
            Debug.Log("Sent game over signal");

            // Wait and receive the response
            new Thread(() =>
            {
                string bestMoveMessage = _requester.ReceiveFrameString();
                Debug.Log("Received: " + bestMoveMessage);

                // Use Unity's main thread to process the received message
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    callbackMove(bestMoveMessage);
                    _ui.HandleBestMoveNumber(bestMoveMessage);
                    Debug.Log("complete");
                });
            }).Start();
        }
        
        //killing the process twice isn't ideal, but cobra seems to be launching two processes on macOS
        //Unity in-engine process management also does not kill the process on Unity editor stop
        private void OnApplicationQuit()
        { 
            if(_cobraProcess != null)
                KillCobraProcess();
        }

        private void OnDestroy()
        {
            if (_cobraProcess == null) return;
            KillCobraProcess();
        }

        internal void KillCobraProcess()
        {
            if (_cobraProcess != null && !_cobraProcess.HasExited)
            {
                try
                {
                    _cobraProcess.Kill();
                    UnityEngine.Debug.Log("Process killed");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to kill process: {ex.Message}");
                }
                finally
                {
                    _cobraProcess.Dispose();
                    GracefulShutdown();
                    _requester?.Dispose();
                }
            }
            else
            {
                UnityEngine.Debug.Log("Process already exited or was not started.");
            }
        }

    }
}