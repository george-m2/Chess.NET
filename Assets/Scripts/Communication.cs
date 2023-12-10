using System.Threading;
using AsyncIO;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using PGNDelegate;

namespace Communication
{
    public class HelloClient : MonoBehaviour
    {
        private HelloRequester _helloRequester;
        private PGNExporter _pgnExporter;

        private void Start()
        {
            _pgnExporter = FindObjectOfType<PGNExporter>();
            string pgnString = _pgnExporter.CommunciatePGN(); //thread-safe implementation
            _helloRequester = new HelloRequester(pgnString);
            _helloRequester.Start();
            Debug.Log("Sending PGN");
        }

        private void OnDestroy()
        {
            _helloRequester.Stop();
        }
    }

    public class HelloRequester : RunAbleThread
    {
        private readonly string _pgnString;

        public HelloRequester(string pgnString)
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