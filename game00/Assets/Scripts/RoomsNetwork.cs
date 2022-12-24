using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RoomsNetwork : MonoBehaviour
{
    Network netWK;
    Socket clientSock;
    byte[] recvBuff = new byte[100];
    Text outputText;
    String recvStr = "";
    String strRooms = "roomList 0 ";
    // Start is called before the first frame update
    void Start()
    {
        netWK = GameObject.FindGameObjectWithTag("networkobject").GetComponent<Network>();
     //   netWK.iSceneNum = 1;
        outputText = GameObject.Find("Canvas/OutputText").GetComponent<Text>();
        clientSock = netWK.GetClientSocket();
        clientSock.BeginReceive(recvBuff, 0, 100, 0, recvCb, clientSock);
       // GameObject.DontDestroyOnLoad(gameObject);
    }
    int flag = 0;
    void recvCb(IAsyncResult iar)
    {
        Socket tempSock = (Socket)iar.AsyncState;
        int recvNum = tempSock.EndReceive(iar);
        String tempStr = System.Text.Encoding.Default.GetString(recvBuff);
        String[] values = tempStr.Split(' ');
        switch (values[0])
        {
            case "roomList":
                strRooms = recvStr = tempStr;
                break;
            case "beginGame":
                Debug.Log(values[0]);
                flag = 1;
             //   SceneManager.LoadScene(2);
                break;
        }
        clientSock.BeginReceive(recvBuff, 0, 100, 0, recvCb, clientSock);
    }
    GameObject[] textObjs;
    GameObject[] btnObjs;
    void RefreshRoomList(String[] args)
    {
        if (textObjs != null)
            for (int i = 0; i < textObjs.Length; i++) Destroy(textObjs[i]);
        if (btnObjs != null)
            for (int i = 0; i < btnObjs.Length; i++) Destroy(btnObjs[i]);
        int iRoomCount = Convert.ToInt32(args[1]);
        textObjs = new GameObject[iRoomCount];
        btnObjs = new GameObject[iRoomCount];
        for(int i=0;i<iRoomCount;i++)
        {
            textObjs[i] = GameObject.Instantiate(Resources.Load("Text", typeof(GameObject))) as GameObject;
            textObjs[i].transform.SetParent(GameObject.Find("Canvas/Image/GameObject").transform, false);
            textObjs[i].GetComponent<Text>().text = args[i + 2];
            btnObjs[i] = GameObject.Instantiate(Resources.Load("Button", typeof(GameObject))) as GameObject;
            btnObjs[i].transform.SetParent(GameObject.Find("Canvas/Image/GameObject").transform, false);
            btnObjs[i].GetComponent<EnterRoomBtnNum>().SetNum(i);
            GameObject tempObj = btnObjs[i];
            btnObjs[i].GetComponent<Button>().onClick.AddListener(
                delegate () { this.OnClick(tempObj, args); }
                );
        }

    }
      public void OnClick(GameObject sender,String[] args)
       {
            int i = sender.GetComponent<EnterRoomBtnNum>().GetNum();
            String sSendStr = "enterRoom " + args[i + 2];
            clientSock.Send(System.Text.Encoding.Default.GetBytes(sSendStr));
       }
    
    public void CreateRoomBtnClicked()
    {
        clientSock.Send(System.Text.Encoding.Default.GetBytes("createRoom "));
    }

    // Update is called once per frame
    int iFrequcy = 0;
    void Update()
    {
        iFrequcy++;
        if(iFrequcy%120==0)
        {
            outputText.text = recvStr;
            String[] values = strRooms.Split(' ');
            RefreshRoomList(values);
        }
        if (flag == 1)
            SceneManager.LoadScene(2);
    }
}
