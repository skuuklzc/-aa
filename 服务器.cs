/* 

2023/01/01

 Before reading it, you should read the readme.md file , it can help you finish this test!
！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！
Designed by skuuklzc 
 */
using System;
using System.Collections.Generic;

using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Data.SqlClient;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        const byte REGIST_REQUEST = 1;
        const byte REGIST_FAIL = 2;
        const byte REGIST_SUCCESS = 3;
        const byte LOGIN_REQUEST = 4;
        const byte LOGIN_FAIL_USERDONTEXIST = 5;
        const byte LOGIN_FAIL_PASSWORDWRONG = 6;
        const byte LOGIN_FAIL_ALREADYLOGIN = 7;
        const byte LOGIN_SUCCESS = 8;

        public int bufferCount = 0;
        StateObject[] stateOs;
        int maxConn = 10;
        int index = 0, num = 0;
        List<RoomObject> roomList;
        List<RoomObject> freeRoomList;
        System.Timers.Timer timer = new System.Timers.Timer(1000);
        public long heartBeatTime = 4;

        Socket listSock;     //监听连接的Socket
        private static string connStr = "server=.;Trusted_Connection=SSPI;DataBase=db1";
        public Form1()
        {
            InitializeComponent();
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            roomList = new List<RoomObject>();
            roomList.Clear();
            freeRoomList = new List<RoomObject>();
        }
        public void HandleMainTimer(object sender,System.Timers.ElapsedEventArgs e)
        {
            long timeNow = Sys.GetTimeStamp();
            for(int i=0;i<index;i++)
            {
                if (stateOs[i] == null) continue;
                if (stateOs[i].lastTickTime < timeNow - heartBeatTime)
                {
                    DeleteRoom(roomList, stateOs[i]);
                    stateOs[i].sock.Shutdown(SocketShutdown.Both);
                    System.Threading.Thread.Sleep(30);
                    lock (stateOs[i].sock)
                        stateOs[i].sock.Close();
                    DeleteClient(stateOs, i);
                }
            }
            showRoomList(roomList);
            CopyFreeRoomList();
            SendRoomList(freeRoomList);
            timer.Start();
        }
        private void DeleteClient(StateObject[] clients,int i)
        {
            if (index == 0) return;
            lock(clients)
            {
                for(int j = i; j < index - 1; j++)
                {
                    clients[j] = clients[j + 1];
                }
                index--;
            }
        }
        private void DeleteRoom(List<RoomObject>rooms,StateObject client)
        {
            for(int i = rooms.Count - 1; i >= 0; i--)
            {
                if (client == rooms[i].client0)
                    rooms.Remove(rooms[i]);
            }
        }
        public void SendRoomList(List<RoomObject> rooms)
        {
            String str = "roomList ";
            str += (rooms.Count).ToString();
        //    str += ip;
          //  outputText.Text = "hhh" + str;
            foreach (RoomObject room in rooms)
            {
                if (!room.playing)
                {
                    str += " ";
                    str += ((IPEndPoint)(room.client0.sock.RemoteEndPoint)).ToString();//房主IP
                }
            }
            byte[] sendBuff = new byte[1024];
            sendBuff = System.Text.Encoding.ASCII.GetBytes(str);
            for (int i = 0; i < index; i++)
            {
                try
                {
                    stateOs[i].sock.Send(sendBuff);
                    outputText.Text = "发送:" + str + "To:" + stateOs[i].sock.RemoteEndPoint.ToString();
                }
                catch (System.Exception ex)
                {
                    outputText.Text = ex.ToString();
                }
            }
        }
        public void showRoomList(List<RoomObject> rooms)
        {
            String str = "";
            str = index.ToString() + "\r\n";
            for(int i=0;i<index;i++)
            {
                str += stateOs[i].sock.RemoteEndPoint.ToString();
                str += "\r\n";
            }
            outputText.Text = str;//1
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipa = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipe = new IPEndPoint(ipa, 10001);
            stateOs = new StateObject[maxConn];
            try
            {
                listSock.Bind(ipe);
                listSock.Listen(10);
                listSock.BeginAccept(new AsyncCallback(acceptCb), listSock);
                outputText.Text = "开始监听10001号端口";
                button1.Enabled = false;
            }
            catch (Exception exc)
            {
                outputText.Text = "10001号端口已被占用";

            }
            timer.Elapsed += new System.Timers.ElapsedEventHandler(HandleMainTimer);
            timer.AutoReset = false;
            timer.Enabled = true;
        }
        //    byte[] recvBuff = new byte[100];
        string ip;
        public void acceptCb(IAsyncResult iar)
        {
            Socket tempSock = (Socket)iar.AsyncState;
            stateOs[index] = new StateObject();
            stateOs[index].sock = tempSock.EndAccept(iar);
            outputText.Text += "有新的连接建立" + index.ToString() + stateOs[index].sock.RemoteEndPoint.ToString() + "\r\n";
            ip = stateOs[index].sock.RemoteEndPoint.ToString();
            try
            {
                stateOs[index].sock.BeginReceive(stateOs[index].buffer, 0, StateObject.BUFFER_SIZE, 0, new AsyncCallback(recvCb), stateOs[index]);
            }
            catch (Exception e){ }
            tempSock.BeginAccept(new AsyncCallback(acceptCb), listSock);
            index++;
            if (index > 10)
            {
                outputText.Text = "连接池已满";
                index = 0;
            }
        }
        byte[] sendBuff = new byte[100];

        void recvCb(IAsyncResult iar)
        {
            String recvStr = "";
            StateObject stateo1 = (StateObject)iar.AsyncState;
            Socket client = stateo1.sock;
            if (client.Connected == false) { outputText.Text = "连接失败"; return; }
            try
            {
               int recvNum = stateo1.sock.EndReceive(iar);
                recvStr = System.Text.Encoding.ASCII.GetString(stateo1.buffer, 0, recvNum);
                if (recvStr.IndexOf("\0") != -1)
                {
                    recvStr = recvStr.Substring(0, recvStr.IndexOf("\0"));
                }
                string[] args = recvStr.Split(' ');
                
                if ("registRequest" == args[0])
                {
                    Regist(args[1], args[2], stateo1);
                }
                if("loginRequest"==args[0])
                {
                    Login(args[1], args[2], stateo1);
                }
                if("createRoom"==args[0])
                {
                    NewRoom(stateo1);
                }
                if ("heartBeat" == args[0])
                {
                    outputText.Text = "收到一个心跳包" + num.ToString();
                    num++;
                    stateo1.lastTickTime = Sys.GetTimeStamp();
                }
                if ("enterRoom" == args[0])
                {
                    EnterRoom(args[1], stateo1);
                }
                if ("Flip" == args[0])
                {
                    int a = 1;
                }
                if ("Position" == args[0] || "Flip" == args[0] || "Fire" == args[0])
                    foreach (RoomObject room in roomList)
                    {
                        if (client == room.client0.sock)
                            room.client1.sock.Send(stateo1.buffer);
                        else if (client == room.client1.sock)
                            room.client0.sock.Send(stateo1.buffer);
                    }
               
                client.BeginReceive(stateo1.buffer, 0, StateObject.BUFFER_SIZE, 0, recvCb, stateo1);
            }
            catch (System.Exception ex)
            {
            }

        }
        String ipStr;//这是从Login函数里拿出来的
        public void NewRoom(StateObject obj0)
        {
            RoomObject room = new RoomObject();
            room.CreateRoom(obj0);
            roomList.Add(room);
        }
        public void EnterRoom(String strClient0,StateObject obj1)
        {
            foreach(RoomObject room in roomList)
            {
                if (room.client0.sock.RemoteEndPoint.ToString() == strClient0)
                {
                    room.EnterRoom(obj1);
                    room.client0.sock.Send(System.Text.Encoding.ASCII.GetBytes("beginGame 0 "));
                    room.client1.sock.Send(System.Text.Encoding.ASCII.GetBytes("beginGame 1 "));
                }
            }
        }
        void CopyFreeRoomList()
        {
            freeRoomList.Clear();
            foreach(RoomObject room in roomList)
            {
                if (!room.playing)
                {
                    RoomObject tempRoom = new RoomObject();
                    tempRoom = room;
                    freeRoomList.Add(tempRoom);
                }
            }
        }
        void Regist(string userName, string passwordMD5, StateObject stateo1)
        {
            //string userName = "aacc";
            //string passwordMD5 = "1234567890ABCDEF1234567890ABCDEF";
            SqlConnection conn = new SqlConnection(connStr);
            conn.Open();
            string sqlStr = "SELECT * FROM table5 WHERE userName='" + userName + "'";
            SqlCommand command = new SqlCommand(sqlStr, conn);
            SqlDataReader odrReader = command.ExecuteReader();
            command.Dispose();
            if (odrReader.HasRows)
                SendRegistFail(stateo1);
            else
            {
                odrReader.Close();
                WriteToDataBase(userName, passwordMD5, conn);
                SendRegistSuccess(stateo1);
            }
            conn.Close();
        }
        void SendRegistFail(StateObject stateo1)
        {
            sendBuff[0] = REGIST_FAIL;
            stateo1.sock.Send(sendBuff);
        }
        void SendRegistSuccess(StateObject stateo1)
        {
            sendBuff[0] = REGIST_SUCCESS;
            stateo1.sock.Send(sendBuff);
        }
        void WriteToDataBase(String userName, String passwordMD5, SqlConnection conn)
        {
            int online = 0;
            int score = 0;
            String sqlStr = "INSERT INTO table5(userName,passwordMD5,online,score)" +
                "VALUES('" + userName + "','" + passwordMD5 + "'," + online + "," + score + ")";
            SqlCommand command = new SqlCommand(sqlStr, conn);
            command.CommandText = sqlStr;
            command.ExecuteNonQuery();
            outputText.Text = "写入一条用户记录";
            command.Dispose();
            conn.Close();
        }

        //用户登录
        void Login(String userName, String passwordMD5, StateObject stateo1)
        {
            SqlConnection conn = new SqlConnection(connStr);
            conn.Open();
            if (!CheckUserNameExist(conn, userName))
            {
                SendLoginMessage(LOGIN_FAIL_USERDONTEXIST, stateo1);
                return;
            }
            if (!CheckPassword(conn, userName, passwordMD5))
            {
                SendLoginMessage(LOGIN_FAIL_PASSWORDWRONG, stateo1);
                return;
            }
            if (CheckUserOnline(conn, userName))
            {
                SendLoginMessage(LOGIN_FAIL_ALREADYLOGIN, stateo1);
                return;
            }//都检测完了，可以成功登录
            ipStr = stateo1.sock.RemoteEndPoint.ToString();//登录的ip
            UpdateToOnline(userName, conn, ipStr);
            SendLoginMessage(LOGIN_SUCCESS, stateo1);
        }
        //检查用户是否存在。存在返回true，否则返回false
        bool CheckUserNameExist(SqlConnection conn, string userName)
        {
            string sqlStr = "SELECT * FROM table5 WHERE userName='" + userName + "'";
            SqlCommand command = new SqlCommand(sqlStr, conn);
            SqlDataReader odrReader = command.ExecuteReader();
            command.Dispose();
            if (odrReader.HasRows)
            {
                //odrReader.Read(); 
                odrReader.Close();
                return true;
            }
            else
            {
                odrReader.Close();
                return false;
            }
        }
        //检查密码是否正确，正确返回true。错误返回false
        bool CheckPassword(SqlConnection conn, string userName, string passwordMD5)
        {
            string sqlStr = "SELECT * FROM table5 WHERE userName='" + userName + "'";
            SqlCommand command = new SqlCommand(sqlStr, conn);
            SqlDataReader odrReader = command.ExecuteReader();
            command.Dispose();
            odrReader.Read();
            String passwordInDatabase = odrReader.GetString(1);

            if (passwordInDatabase == passwordMD5)
            {
             //   outputText.Text = "密码相同";
                odrReader.Close();
                return true;
            }
            else
            {
             //   outputText.Text = "密码不同";
                odrReader.Close();
                return false;
            }
        }
        //检查用户是否在线，返回true表示在线，false表示不在线
        bool CheckUserOnline(SqlConnection conn, string userName)
        {
            string sqlStr = "SELECT * FROM table5 WHERE userName='" + userName + "'";
            SqlCommand command = new SqlCommand(sqlStr, conn);
            SqlDataReader odrReader = command.ExecuteReader();
            command.Dispose();
            odrReader.Read();
            int a = odrReader.GetInt32(3);
            if (a == 1)
            {
                odrReader.Close();
                return true;
            }
            else
            {
                odrReader.Close();
                return false;
            }
        }
        //发送登录成功/失败消息给客户端
        void SendLoginMessage(byte LOGIN_MESSAGE, StateObject stateo1)
        {
              sendBuff[0] = LOGIN_MESSAGE;//修改一下协议，我要将ip一起发送过去，这样我才能算分数！ 
          //  sendBuff = System.Text.Encoding.Default.GetBytes(LOGIN_MESSAGE.ToString() + " " + ipStr);
              stateo1.sock.Send(sendBuff);
            
        }
        //更换用户状态为在线状态，并记录IP地址，供掉线时反向查询用户名
        void UpdateToOnline(String userName, SqlConnection conn, String ipStr)
        {
            String sqlStr = "UPDATE table5 SET online=1, userIP='" + ipStr + "' WHERE userName='" + userName + "'";
            SqlCommand comm = new SqlCommand(sqlStr, conn);
            comm.ExecuteNonQuery();
            comm.Dispose();
        }

    }
    public class StateObject
    {
        public const int BUFFER_SIZE = 1024;
        public byte[] buffer = new byte[BUFFER_SIZE];
        public Socket sock = null;
        public bool bIsUsed = false;

        public long lastTickTime;
        public StateObject() { lastTickTime = Sys.GetTimeStamp(); }
        
    }
    public class RoomObject
    {
        public StateObject client0;
        public StateObject client1;
        public bool playing;
        public RoomObject()
        {
            client0 = new StateObject();
            client1 = new StateObject();
        }
        public void CreateRoom(StateObject c0) { client0 = c0; playing = false; }
        public void EnterRoom(StateObject c1) { client1 = c1; playing = true; }
    }
    public class Sys
    {
        public static long GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1907, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }

    }
}

