using System;
using System.Diagnostics;
using System.Threading;
using ChessNET;
using GameUIManager;
using NetMQ;
using NetMQ.Sockets;
using PGNDelegate;
using PimDeWitte.UnityMainThreadDispatcher;
using UnityEngine;
using Debug = UnityEngine.Debug; //ambiguity with UnityEngine.Debug.Log and System.Diagnostics.Debug.Log

namespace Communication
{
    public class Client : MonoBehaviour
    {
        private Process _cobraProcess;
        string bestMoveMessage;

        public void CreateEngineProcess()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo //start the cobra process
            {
                FileName = Application.streamingAssetsPath + "/cobra", //path to the cobra executable (root of executable)
                UseShellExecute = false, //don't use the shell to execute the process
                //use std input/output to communicate with the process
                RedirectStandardOutput = true, 
                RedirectStandardInput = true 
            };

            _cobraProcess = new Process(); //instantiate cobra
            _cobraProcess.StartInfo = startInfo;
            if (_cobraProcess.Start()) 
            {
                Debug.Log("Process started");
                Debug.Log("Process started with ID: " + _cobraProcess.Id);
            }

            _cobraProcess.BeginOutputReadLine(); //begin reading the output
        }

        // singleton pattern to easily access the instance
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
                Instance = this; //set the instance to this object
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
            _requester.Connect("tcp://localhost:5555"); //cobra listener at localhost
        }

        //callback delegates for thread safety 
        public delegate void PGNReceivedHandler(string pgnString); 
        public delegate void ACPLReceivedHandler(int acpl); 

        public void ReceiveMoveData(PGNReceivedHandler callback, ACPLReceivedHandler acplCallback)
        {
            string pgnString = _pgnExporter.ConvertCurrentMoveToSAN();
            
            //**NETMQ REP SOCKET**//
            new Thread(() =>
            {
                _requester.SendFrame(pgnString); //send the PGN string to cobra 
                string message = _requester.ReceiveFrameString(); //receive the response
                Debug.Log("Received: " + message);
                // use Unity's main thread to call the callback method and to find the Chessboard
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (_chessboard != null)
                    {
                        // Check if the current player is white before processing the move
                        if (_chessboard.isWhiteTurn) return;
                        MoveACPLResponseData sanAcplResponseData = JsonUtility.FromJson<MoveACPLResponseData>(message); //deserialize the JSON response
                        var san = sanAcplResponseData.move;
                        var ACPL = sanAcplResponseData.acpl;
                        callback(san); //call the callback with the SAN string
                        acplCallback(ACPL); //call the callback with the ACPL value
                        Debug.Log("Received SAN: " + san);
                        _chessboard.ProcessReceivedMove(san);
                        _ui.HandleACPL(ACPL); //minor callback workaround to pass ACPL to UIManager
                    }
                });
            }).Start(); 
        }

        private void GracefulShutdown()
        {
            _requester?.SendFrame("SHUTDOWN"); //shutdown signal
        }

        public delegate void BestMoveReceivedHandler(string bestMoveString); 
        public delegate void BlunderReceivedHandler(string blunder);
        
        //TODO: refactor both SendGameOver and HandlePGN to use an event system
        public void SendGameOver(BestMoveReceivedHandler callbackBest, BlunderReceivedHandler callbackBlunder)
        {
            if (_requester == null) return;
            _requester.SendFrame("GAME_END");
            Debug.Log("Sent game over signal");

            new Thread(() =>
            {
                string jsonResponse = _requester.ReceiveFrameString();
                Debug.Log("Received: " + jsonResponse);

                // Deserialize the JSON response
                Debug.Log($"Final JSON String Before Deserialisation: {jsonResponse}");
                GameOverResponse response = JsonUtility.FromJson<GameOverResponse>(jsonResponse);

                // Use Unity's main thread to process the received message
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    // Now calling the callback with both values
                    callbackBest(response.bestMoveCount.ToString()); 
                    callbackBlunder(response.blunderCount.ToString());
                    _ui.HandleBestMoveNumber(response.bestMoveCount.ToString()); 
                    _ui.HandleBlunderNumber(response.blunderCount.ToString());
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
            if (_cobraProcess is { HasExited: false })
            {
                try
                {
                    _cobraProcess.Kill();
                    Debug.Log("Process killed");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to kill process: {ex.Message}");
                }
                finally
                {
                    _cobraProcess.Dispose(); //free the process from memory
                    GracefulShutdown(); 
                    _requester?.Dispose(); //dispose of the NetMQ socket
                }
            }
            else
            {
                Debug.Log("Process already exited or was not started.");
            }
        }

    }
}
[Serializable]
public class GameOverResponse
{
    public int bestMoveCount;
    public int blunderCount;
}
[Serializable]
public class MoveACPLResponseData
{
    public string move;
    public int acpl;
}
