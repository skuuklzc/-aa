using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Network : MonoBehaviour
{
    Socket clientSock;
    byte[] recvBuff = new byte[100];
    // Start is called before the first frame update
    void Start()
    {
        clientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        GameObject nobj = GameObject.Find("GameObject");
        DontDestroyOnLoad(this);
    }
    bool connected = false;
    public void ConnectBtnClicked()
    {
        clientSock.Connect("127.0.0.1", 10001);
     //   clientSock.BeginReceive(recvBuff, 0, 100, 0, recvCb, clientSock);
        connected = true;
        SceneManager.LoadScene(1);//ÇÐ»»³¡¾°1
    }
    void recvCb(IAsyncResult iar)
    {
        Socket tempSock = (Socket)iar.AsyncState;
      //  int recvNum = tempSock.EndReceive(iar);
        tempSock.BeginReceive(recvBuff, 0, 100, 0, recvCb, tempSock);
    }
    public Socket GetClientSocket()
    {
        return clientSock;
    }
    void SendHeartbeat()
    {
        clientSock.Send(System.Text.Encoding.Default.GetBytes("heartBeat "));
    }
    int i = 0;
    // Update is called once per frame
    void FixedUpdate()
    {
        i++;
        if (i % 50 == 0 && connected)
            SendHeartbeat();
    }
}
