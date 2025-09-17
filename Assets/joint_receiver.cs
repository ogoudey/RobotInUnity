using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class RobotSocketReceiver : MonoBehaviour
{
    public int port = 5005;   // Port to listen on
    private TcpListener listener;
    private Thread listenerThread;
    private bool running = false;

    private float[] latestValues;
    private readonly object lockObj = new object();

    public ArticulationBody[] joints;
    float[] currentAngles = new float[8];
    
    void Start()
    {
        // Set up body
        joints = GetComponentsInChildren<ArticulationBody>();
        running = true;
        listenerThread = new Thread(ListenForClients);
        listenerThread.IsBackground = true;
        listenerThread.Start();
    }

    void ListenForClients()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            while (running)
            {
                if (!listener.Pending())
                {
                    Thread.Sleep(10);
                    continue;
                }

                TcpClient client = listener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

                byte[] buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    float[] values = ParseFloatList(message);

                    // Reponse
                    string responseMessage = "[" + string.Join(", ", currentAngles) + "]\n";
                    Debug.Log("Sending " + responseMessage); 
                    // Send over the network
                    byte[] encodedMessage = Encoding.UTF8.GetBytes(responseMessage);
                    stream.Write(encodedMessage, 0, encodedMessage.Length);

                    lock (lockObj)
                    {
                        latestValues = values;
                    }
                }
                
                client.Close();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Socket listener exception: " + e);
        }
    }

    float[] ParseFloatList(string message)
    {
        try
        {
            string[] parts = message.Trim(new char[] { '[', ']', '\n', '\r', ' ' }).Split(',');
            float[] values = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (float.TryParse(parts[i], out float val))
                    values[i] = val;
            }
            return values;
        }
        catch
        {
            return new float[0];
        }
    }

    void Update()
    {
        float[] copy = null;
        lock (lockObj)
        {
            if (latestValues != null)
                copy = (float[])latestValues.Clone();
        }

        if (copy != null)
        {
            Debug.Log("Received: " + string.Join(", ", copy));
            // Turn each value into drive.
            if (copy[0] != 1)
                {
                    for (int v = 0; v < copy.Length; v++)
                    {
                        ArticulationDrive drive = joints[v].xDrive;
                        drive.driveType = ArticulationDriveType.Target;
                        drive.stiffness = 0.5F;
                        drive.forceLimit = 0.3F;
                        drive.target = copy[v];
                        joints[v].xDrive = drive;
                    }
                }
            else
            {
                Debug.Log("Read-only flag from client");
            }
            
            
            
            


        }
        // Now read joints


        for (int i = 0; i < joints.Length; i++)
        {
            try
            {
                currentAngles[i] = joints[i].jointPosition[0]; // safe for revolute
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to read joint {i}: {e.Message}");
                currentAngles[i] = 0f; // fallback for fixed/unexpected joints
            }
        }
    }

    void OnApplicationQuit()
    {
        running = false;
        if (listener != null) listener.Stop();
        if (listenerThread != null && listenerThread.IsAlive) listenerThread.Abort();
    }
}
