using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class GameNetWork : MonoBehaviour
{
    GameObject hero1;
    GameObject hero2;
    Network netObj;
    
    ulong add1PerFrame = 0;
    byte[] readBuff = new byte[1024];
    byte[] sendBuff = new byte[1024];
    
    Vector2 newPostion;
    Socket clientSock;
    // Start is called before the first frame update
    void Start()
    {
        netObj = GameObject.FindGameObjectWithTag("networkobject").GetComponent<Network>();
        netObj.iSceneNum = 2;
        clientSock = netObj.GetClientSock();
        clientSock.BeginReceive(readBuff,0, 1024, 0, recvCb, clientSock);
        
        setPosition();
    }
    void setPosition()
    {
        hero1 = GameObject.Find("hero");
        hero2 = GameObject.Find("hero2");
        Vector3 Scale = hero2.transform.localScale;
        Scale.x *= -1;
        hero2.transform.localScale = Scale;
        float x1, x2, y1, y2;
        y1 = y2 = hero1.transform.position.y;
        x1 = -17;
        x2 = 17;
        
        Vector2 position1 = new Vector2(x1, y1);
        Vector2 position2 = new Vector2(x2, y2);
        hero1.transform.position = position1;
        hero2.transform.position = position2;
    }
    void recvCb(IAsyncResult iar)//? ? ? 
    {
        Socket tempSocket=(Socket)iar.AsyncState;
        int num = clientSock.EndReceive(iar);
        string tempStr = System.Text.Encoding.Default.GetString(readBuff);
        string[] recvstr = tempStr.Split(' ');
        if (recvstr[0] == "Position")
     //   {
            BaseSocket.msgList.Add(tempStr);
     //   }

        if (recvstr[0] == "Flip")
            BaseSocket.msgList.Add(tempStr);
        if (recvstr[0] == "Fire")
            BaseSocket.msgList.Add(tempStr);
       // if (netObj.iSceneNum == 2)
      //  {
          //  Debug.Log("2");
            clientSock.BeginReceive(readBuff, 0, 100, 0, recvCb, clientSock);
      //  }

    }
    private void Update()
    {
        if (BaseSocket.msgList.Count > 0)
        {
            string tempStr = BaseSocket.msgList[0];
            BaseSocket.msgList.RemoveAt(0);
            String[] values = tempStr.Split(' ');

            switch (values[0]) {
                case "Position":
                    float x = float.Parse(values[1]);
                    float y = float.Parse(values[2]);
                    hero2.transform.position = new Vector2(-x, y);
                    float h = float.Parse(values[3]);
                    GameObject.Find("hero2").GetComponent<PlayerHealth2>().health = h;
                    GameObject.Find("hero2").GetComponent<PlayerHealth2>().UpdateHealthBar();
                    if (h <= 0) 
                    {
                     //   SendData("gameover " + netObj.IP);
                        GameObject.Find("hero2").GetComponent<Animator>().SetTrigger("Die");                                                
                    }
                    break;
                case "Flip":
                    GameObject.Find("hero2").GetComponent<PlayerControl2>().Flip();
                    break;
                case "Fire":
                    GameObject.Find("hero2/Gun").GetComponent<Gun2>().Fire();
                    break;
            }
        }
        add1PerFrame++;
        if (add1PerFrame % 5 == 0)
        {
            SendPosition();
        }
    }
    void SendPosition()
    {
        float x, y, h;
        x = hero1.transform.position.x;
        y = hero1.transform.position.y;
        h = hero1.GetComponent<PlayerHealth>().health;
        string strSend = "Position " + x.ToString() + " " + y.ToString() + " " + h.ToString()+" ";
        SendData(strSend);
    }
    void SendData(string sendStr)
    {
        sendBuff = System.Text.Encoding.Default.GetBytes(sendStr);
        clientSock.Send(sendBuff);
    }
   
}
