using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class SocketServer : MonoBehaviour
{
    // Define an event to signal when new data is received
    public delegate void OnDataReceivedHandler(Dictionary<string, float> data);
    public static event OnDataReceivedHandler OnDataReceived;

    private TcpListener tcpListener;
    private Thread tcpListenerThread;
    private TcpClient connectedTcpClient;

    [System.Serializable]
    public class DataWrapper
    {
        public List<Item> items;
    }

    [System.Serializable]
    public class Item
    {
        public string name;
        public float value;
    }

    void Start()
    {
        // Start TcpListener background thread
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncomingRequests));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void ListenForIncomingRequests()
    {
        try
        {
            // Set up the TcpListener
            tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 12345);
            tcpListener.Start();
            Debug.Log("Server started, listening for incoming connections...");
            byte[] bytes = new byte[1024];
            while (true)
            {
                using (connectedTcpClient = tcpListener.AcceptTcpClient())
                {
                    // Get a stream object for reading
                    using (NetworkStream stream = connectedTcpClient.GetStream())
                    {
                        int length;
                        // Read incoming stream into byte array
                        while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            var incomingData = new byte[length];
                            System.Array.Copy(bytes, 0, incomingData, 0, length);
                            // Convert byte array to string message
                            string jsonString = Encoding.ASCII.GetString(incomingData);
                            


                            // Parse JSON string into a data wrapper
                            DataWrapper dataWrapper = JsonUtility.FromJson<DataWrapper>(jsonString);
                            Debug.Log(dataWrapper.items.Count);

                            // Convert the data wrapper into a dictionary
                            Dictionary<string, float> data = new Dictionary<string, float>();
                            foreach (var item in dataWrapper.items)
                            {
                                data[item.name] = item.value;
                                Debug.Log(item.name);
                            }

                            // Raise the OnDataReceived event
                            OnDataReceived?.Invoke(data);
                            Debug.Log(jsonString);
                        }
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException: " + socketException.ToString());
        }
    }
}
