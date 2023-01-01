using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Net;
using System.Net.Sockets;
using System;

public class RoomsNetwork : MonoBehaviour
{
    Network netWK;
    Socket clientSock;
    byte[] readBuff = new byte[100];
    Text outputText;
    String recvStr = "";
    String strRooms = "roomList 0 ";
    // Start is called， before the first frame update
    void Start()
    {
        outputText = GameObject.Find("Canvas/OutputText").GetComponent<Text>();
        netWK = GameObject.FindGameObjectWithTag("networkobject").GetComponent<Network>();
        netWK.iSceneNum = 1;
        clientSock = netWK.GetClientSock();
        clientSock.BeginReceive(readBuff, 0, 100, 0, recvCb, clientSock);
    }
    int flag = 0;
    private void Awake()
    {

    }
   
    void recvCb(IAsyncResult iar)
    {
        Socket tempSock = (Socket)iar.AsyncState;
        int recvNum = tempSock.EndReceive(iar);
        String tempStr = System.Text.Encoding.Default.GetString(readBuff);
        String[] values = tempStr.Split(' ');
        
        switch (values[0])
        {
            case "roomList":
                strRooms = recvStr = tempStr;
                //更新房间列表.
                //RefreshRoomList(values);   这是错的
                break;
            case "beginGame":
                netWK.whoAmI = values[1];
                flag = 1;
              //   SceneManager.LoadScene(2);
                break;
        }
          if(netWK.iSceneNum == 1)
             clientSock.BeginReceive(readBuff, 0, 100, 0, recvCb, clientSock);
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
        for (int i = 0; i < iRoomCount; i++)
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
    public void OnClick(GameObject sender, String[] args)
    {
        int i = sender.GetComponent<EnterRoomBtnNum>().GetNum();
        String sSendStr = "enterRoom " + args[i + 2];
        netWK.IP = args[i + 2];
        clientSock.Send(System.Text.Encoding.Default.GetBytes(sSendStr));
        sender.GetComponent<Button>().enabled = false;
    }
    // Update is called once per frame
    int iFrequcy = 0;
    void Update()
    {
        iFrequcy++;
        if (iFrequcy % 240 == 0)
        {
            outputText.text = recvStr;
            String[] values = strRooms.Split(' ');
            RefreshRoomList(values);
        }
        if(flag==1)
            SceneManager.LoadScene(2);

    }
    public void CreateRoomBtnClicked()
    {
        clientSock.Send(System.Text.Encoding.Default.GetBytes("createRoom "));
        GameObject.Find("Canvas/Button").GetComponent<Button>().enabled = false;
    }
}
