using System;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class SocketServer : MonoBehaviour
{
    public delegate void OnDataReceivedHandler(List<Item> data);
    public static event OnDataReceivedHandler OnDataReceived;

    private TcpListener tcpListener;
    private Thread tcpListenerThread;
    private volatile bool isRunning = true;

    [System.Serializable]
    public class DataWrapper
    {
        public List<Item> items;
    }

    [System.Serializable]
    public class Item
    {
        public string name;
        public string stringValue; 
        public float numberValue; 
    }

    void Start()
    {
        tcpListenerThread = new Thread(ListenForIncomingRequests);
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
    }

    private void ListenForIncomingRequests()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 12345);
            tcpListener.Start();
            Debug.Log("Server started, listening for incoming connections...");
            byte[] bytes = new byte[1024];

            while (isRunning)
            {
                using (TcpClient connectedTcpClient = tcpListener.AcceptTcpClient())
                {
                    using (NetworkStream stream = connectedTcpClient.GetStream())
                    {
                        int length;
                        while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            var incomingData = new byte[length];
                            System.Array.Copy(bytes, 0, incomingData, 0, length);
                            string jsonString = Encoding.ASCII.GetString(incomingData);

                            DataWrapper dataWrapper = JsonUtility.FromJson<DataWrapper>(jsonString);
                            OnDataReceived?.Invoke(dataWrapper.items);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log("Exception in ListenForIncomingRequests: " + ex.ToString());
        }
        finally
        {
            if (tcpListener != null)
            {
                tcpListener.Stop();
            }
        }
    }

    void OnDestroy()
    {
        isRunning = false;
        if (tcpListener != null)
        {
            tcpListener.Stop();
        }
        if (tcpListenerThread != null)
        {
            tcpListenerThread.Join();
        }
    }
}
