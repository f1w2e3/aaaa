using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine.UI;
using System.IO;
using System.Runtime.InteropServices;//마샬링을 위한 어셈블리

using DefineInfo;
using static DefineData;

/// <summary>
/// 클라이언트 클래스
/// </summary>
public class ClientInfo
{
    public TcpClient clientSocket;//클라이언트 소켓(통신 도구)
    public string clientname;//클라이언트 이름
    public Vector3 position;//클라이언트 위치
    public Quaternion quaternion;//클라이언트 쿼터니언

    public NetworkStream stream; // 클라이언트 통신 도구

    //받은 데이터 저장공간
    public byte[] buffer;
    //받은 데이터가 잘릴 경우를 대비하여 임시버퍼에 저장하여 관리
    public byte[] tempBuffer;//임시버퍼
    public bool isTempByte;//임시버퍼 유무
    public int nTempByteSize;//임시버퍼의 크기


    public ClientInfo(TcpClient clientSocket, string clientname = "클라이언트", 
        Vector3 position = default, Quaternion quaternion = default)
    {
        this.clientSocket = clientSocket;
        this.clientname = clientname;
        this.position = position;
        this.quaternion = quaternion;
        this.stream = clientSocket.GetStream();

        //데이터 저장공간 초기화
        this.buffer = new byte[1024];
        //임시버퍼 초기화
        this.tempBuffer = new byte[1024];
        this.isTempByte = false;
        this.nTempByteSize = 0;
    }
}

public class NetworkManager_Server : MonoBehaviour
{
    //쓰레드
    private Thread tcpListenerThread;
    //서버의 소켓
    private TcpListener tcpListener;
    //클라이언트
    //private ClientInfo client;

    // 클라이언트 목록
    private Dictionary<string,ClientInfo> ConnectedClients; // 연결된 클라이언트 목록
    private List<ClientInfo> disconnectedClients;  // 연결 해제된 클라이언트 목록

    //아이피, 포트
    public string ip;
    public int port;

    //서버 상태
    private bool serverReady;
    //통신 메시지 읽고 쓰기 도구
    //private NetworkStream stream;

    //로그
    public Text ServerLog;//ui
    private List<string> logList;//data

    //전송 메시지
    public InputField Text_Input;

    //클라이언트 기능 UI
    public GameObject ButtonConnect;
    public GameObject ButtonDisConnect;
    public GameObject ClientFunctionUI;

    

    //받은 데이터 처리 공간
    private Queue<stChangeInfoMsg> receive_changeInfo_MSG = new Queue<stChangeInfoMsg>();

    //보내는 메시지 저장공간
    byte[] sendMessage;


    // Start is called before the first frame update
    void Start()
    {
        //로그 초기화
        logList = new List<string>();

        //클라이언트 목록 초기화
        ConnectedClients = new Dictionary<string, ClientInfo>();
        disconnectedClients = new List<ClientInfo>();

        //보내는 메시지 초기화
        sendMessage = new byte[1024];
    }

    // Update is called once per frame
    void Update()
    {
        //받은 데이터가 있는경우(내 정보 확인)
        if (receive_changeInfo_MSG.Count > 0)
        {
            //차례대로 뽑아낸다.
            stChangeInfoMsg CreateObjMsg = receive_changeInfo_MSG.Dequeue();

            if(ConnectedClients.ContainsKey(CreateObjMsg.sendClientName))
            {
                //데이터를 넣는다.
                ConnectedClients[CreateObjMsg.sendClientName].clientname = CreateObjMsg.strClientName;
                ConnectedClients[CreateObjMsg.sendClientName].position.x = CreateObjMsg.position[0];
                ConnectedClients[CreateObjMsg.sendClientName].position.y = CreateObjMsg.position[1];
                ConnectedClients[CreateObjMsg.sendClientName].position.z = CreateObjMsg.position[2];

                ConnectedClients[CreateObjMsg.sendClientName].quaternion.x = CreateObjMsg.Quaternion[0];
                ConnectedClients[CreateObjMsg.sendClientName].quaternion.y = CreateObjMsg.Quaternion[1];
                ConnectedClients[CreateObjMsg.sendClientName].quaternion.z = CreateObjMsg.Quaternion[2];
                ConnectedClients[CreateObjMsg.sendClientName].quaternion.w = CreateObjMsg.Quaternion[3];

                ClientInfo client = ConnectedClients[CreateObjMsg.sendClientName];
                ConnectedClients.Remove(CreateObjMsg.sendClientName);
                ConnectedClients.Add(CreateObjMsg.strClientName, client);
            }
        }

        //로그리스트에 쌓였다면
        if (logList.Count > 0)
        {
            //로그 출력
            WriteLog(logList[0]);
            logList.RemoveAt(0);
        }

        //서버 상태에 따라 클라이언트 버튼 활성화/비활성화
        ButtonConnect.SetActive(!serverReady);
        ButtonDisConnect.SetActive(!serverReady);
        ClientFunctionUI.SetActive(!serverReady);
    }

    /// <summary>
    /// 서버 생성 버튼
    /// </summary>
    public void ServerCreate()
    {
        //ip, port 설정
        port = int.Parse(GameObject.Find("Text_Port").GetComponent<InputField>().text);

        // TCP서버 배경 스레드 시작
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequeset));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
    }

    /// <summary>
    /// 서버 쓰레드 시작
    /// </summary>
    private void ListenForIncommingRequeset()
    {
        try
        {
            // 소켓 생성
            tcpListener = new TcpListener(IPAddress.Any/*서버에 접속 가능한 IP*/, port);
            tcpListener.Start();

            // 서버 상태 ON
            serverReady = true;

            // 로그 기록
            logList.Add("[시스템] 서버 생성(port:" + port + ")");

            // 데이터 리시브 항시 대기(Update)
            while (true)
            {
                // 서버를 연적이 없다면
                if(!serverReady)
                    break;

                //연결 시도중인 클라이언트 확인
                if(tcpListener != null && tcpListener.Pending())
                {
                    // 연결된 클라이언트 목록에 저장
                    string clientName = "클라이언트" + ConnectedClients.Count.ToString();

                    ConnectedClients.Add(clientName, new ClientInfo(tcpListener.AcceptTcpClient(),clientName));
                    BroadCast(clientName + " 접속!");

                    stHeader stHeaderTmp = new stHeader();

                    stHeaderTmp.MsgID = 3;
                    stHeaderTmp.sendClientName = clientName;
                    stHeaderTmp.PacketSize = (ushort)Marshal.SizeOf(stHeaderTmp);//메시지 크기

                    byte[] SendData = GetHeaderToByte(stHeaderTmp);

                    ConnectedClients[clientName].stream.Write(SendData, 0, SendData.Length);
                    ConnectedClients[clientName].stream.Flush();
                }

                //접속된 클라이언트 존재시 상호작용 처리
                foreach(KeyValuePair<string, ClientInfo> DicClient in ConnectedClients)
                {
                    ClientInfo client = DicClient.Value;

                    if (client != null)
                    {
                        //클라이언트 접속 종료시
                        if (!IsConnected(client.clientSocket))
                        {
                            // 이곳에서 바로 클라이언트를 삭제하면 쓰레드간의 딜레이 차이로 에러가 발생함으로 연결해제된 클라이언트 목록으로 관리
                            // 연결해제된 클라이언트 목록에 추가
                            disconnectedClients.Add(client);

                            continue;
                        }
                        //클라이언트 메시지 처리
                        else
                        {
                            //메시지가 들어왔다면
                            if (client.stream.DataAvailable)
                            {
                                //메시지 저장 공간 초기화
                                Array.Clear(client.buffer, 0, client.buffer.Length);

                                //메시지를 읽는다.
                                int messageLength = client.stream.Read(client.buffer, 0, client.buffer.Length);

                                //실제 처리하는 버퍼
                                //지금 읽어온 메시지에 남은 메시지의 사이즈를 더해서 처리할 버퍼 생성
                                byte[] pocessBuffer = new byte[messageLength + client.nTempByteSize];
                                
                                //남았던 메시지가 있다면
                                if (client.isTempByte)
                                {
                                    //앞 부분에 남았던 메시지 복사
                                    Array.Copy(client.tempBuffer, 0, pocessBuffer, 0, client.nTempByteSize);
                                    //지금 읽은 메시지 복사
                                    Array.Copy(client.buffer, 0, pocessBuffer, client.nTempByteSize, messageLength);
                                }
                                else
                                {
                                    //남았던 메시지가 없으면 지금 읽어온 메시지를 저장
                                    Array.Copy(client.buffer, 0, pocessBuffer, 0, messageLength);
                                }

                                //처리해야 하는 메시지의 길이가 0이 아니라면
                                if (client.nTempByteSize + messageLength > 0)
                                {
                                    //받은 메시지 처리
                                    OnIncomingData(client, pocessBuffer);
                                }
                            }
                            else if (client.nTempByteSize > 0)
                            {
                                byte[] pocessBuffer = new byte[client.nTempByteSize];
                                Array.Copy(client.tempBuffer, 0, pocessBuffer, 0, client.nTempByteSize);
                                OnIncomingData(client, pocessBuffer);
                            }
                        }
                    }
                }
                
                //접속 해제된 클라이언트 목록 처리
                for(int i = disconnectedClients.Count-1; i >= 0; i--)
                {
                    //로그기록
                    logList.Add("[시스템]클라이언트 접속 해제");
                    //접속된 클라이언트 목록에서 삭제
                    ConnectedClients.Remove(disconnectedClients[i].clientname);
                    // 처리후 접속해제된 클라이언트 목록에서 삭제
                    disconnectedClients.Remove(disconnectedClients[i]);
                }
                
                //연결된 클라이언트 목록(connectedClients)에 추가가 되어 foreach문을 타게 되지만
                //내용은 안들어가서 client가 null이 되는 현상이 발생하여 딜레이를 준다
                //스레드 무한루프에 의한 CPU 과사용 방지
                Thread.Sleep(10);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }

    /// <summary>
    /// 클라이언트 접속 확인
    /// </summary>
    /// <param name="client"></param>
    /// <returns></returns>
    private bool IsConnected(TcpClient client)
    {
        try
        {
            if(client != null && client.Client != null && client.Client.Connected)
            {
                if(client.Client.Poll(0, SelectMode.SelectRead))
                {
                    return !(client.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 받은 메시지 처리
    /// </summary>
    /// <param name="client"></param>
    /// <param name="data"></param>
    private void OnIncomingData(ClientInfo client, byte[] data)
    {
        // 데이터의 크기가 헤더의 크기보다도 작으면
        if (data.Length < Constants.HEADER_SIZE)
        {
            Array.Copy(data, 0, client.tempBuffer, client.nTempByteSize, data.Length);     // 임지 저장 버퍼에 지금 메시지 저장
            client.isTempByte = true;
            client.nTempByteSize += data.Length;
            return;
        }

        //헤더부분 잘라내기(복사하기)
        byte[] headerDataByte = new byte[Constants.HEADER_SIZE];
        Array.Copy(data, 0, headerDataByte, 0, headerDataByte.Length); //헤더 사이즈 만큼 데이터 복사
        //헤더 데이터 구조체화(마샬링)
        stHeader headerData = HeaderfromByte(headerDataByte);

        // 헤더의 사이즈보다 남은 메시지의 사이즈가 작으면
        if (headerData.PacketSize > data.Length)
        {
            Array.Copy(data, 0, client.tempBuffer, client.nTempByteSize, data.Length);     // 임지 저장 버퍼에 지금 메시지 저장
            client.isTempByte = true;
            client.nTempByteSize += data.Length;
            return;
        }

        //헤더의 메시지크기만큼만 메시지 복사하기
        byte[] msgData = new byte[headerData.PacketSize]; //패킷 분리를 위한 현재 읽은 헤더의 패킷 사이즈만큼 버퍼 생성
        Array.Copy(data, 0, msgData, 0, headerData.PacketSize); //생성된 버퍼에 패킷 정보 복사

        //헤더의 메시지가
        if (headerData.MsgID == 0)//내 정보 확인
        {
            if (ConnectedClients.ContainsKey(headerData.sendClientName))
            {
                ClientInfo clientInfo = ConnectedClients[headerData.sendClientName];

                stChangeInfoMsg stChangeInfoMsgData = new stChangeInfoMsg();

                float[] positionArray = { clientInfo.position.x, clientInfo.position.y, clientInfo.position.z };
                float[] QuaternionArray = { clientInfo.quaternion.x, clientInfo.quaternion.y, clientInfo.quaternion.z, clientInfo.quaternion.w };

                //메시지 작성
                stChangeInfoMsgData.sendClientName = clientInfo.clientname;
                stChangeInfoMsgData.MsgID = 0;//메시지 ID
                stChangeInfoMsgData.PacketSize = (ushort)Marshal.SizeOf(stChangeInfoMsgData);//메시지 크기
                stChangeInfoMsgData.strClientName = clientInfo.clientname;
                stChangeInfoMsgData.position = positionArray;
                stChangeInfoMsgData.Quaternion = QuaternionArray;

                byte[] SendData = GetChangeInfoMsgToByte(stChangeInfoMsgData);

                clientInfo.stream.Write(SendData, 0, SendData.Length);
            }


        }
        else if(headerData.MsgID == 1)//내 정보 변경
        {
            stChangeInfoMsg stChangeInfoMsg1 = ChangeInfoMsgfromByte(msgData);
            receive_changeInfo_MSG.Enqueue(stChangeInfoMsg1);

            logList.Add(client.clientname + " : 내 정보 변경 메시지 수신" );
        }
        else if (headerData.MsgID == 2)//메시지
        {
            stSendMsg SendMsgInfo = SendMsgfromByte(msgData);

            BroadCastByte(msgData);
            //메시지 로그에 기록
            logList.Add(client.clientname + " : " + SendMsgInfo.strSendMsg);
        }
        else//식별되지 않은 ID
        {

        }

        // 모든 메시지가 처리되서 남은 메시지가 없을 경우 
        if (data.Length == msgData.Length)
        {
            client.isTempByte = false;
            client.nTempByteSize = 0;
        }
        // 메시지 처리 후 메시지가 남아있는 경우
        else
        {
            //임시 버퍼 청소
            Array.Clear(client.tempBuffer, 0, client.tempBuffer.Length);

            //생성된 버퍼에 패킷 정보 복사
            Array.Copy(data, msgData.Length, client.tempBuffer, 0, data.Length - (msgData.Length));// 임시 저장 버퍼에 남은 메시지 저장
            client.isTempByte = true;
            client.nTempByteSize += data.Length - (msgData.Length);
        }
    }

    public void BroadCastByte(byte[] data)
    {
        foreach(KeyValuePair < string, ClientInfo> client in ConnectedClients)
        {
            client.Value.stream.Write(data,0, data.Length);
            client.Value.stream.Flush();
        }
    }


    /// <summary>
    /// 로그 전시
    /// </summary>
    /// <param name="message"></param>
    public void WriteLog(/*Time*/string message)
    {
        ServerLog.GetComponent<Text>().text += message + "\n";
    }

    public void SendMsg()
    {
        // 정보 변경 구조체 초기화
        stSendMsg stSendMsgInfo = new stSendMsg();

        string strSendMsg = Text_Input.text;

        //메시지 작성
        stSendMsgInfo.sendClientName = "Server";
        stSendMsgInfo.MsgID = 2;//메시지 ID
        stSendMsgInfo.PacketSize = (ushort)Marshal.SizeOf(stSendMsgInfo);//메시지 크기
        stSendMsgInfo.strSendMsg = strSendMsg;

        //구조체 바이트화 및 전송
        byte[] SendData = GetSendMsgToByte(stSendMsgInfo);

        bool bCheckSend = false;
        foreach (KeyValuePair<string, ClientInfo> client in ConnectedClients)
        {
            client.Value.stream.Write(SendData, 0, SendData.Length);
            client.Value.stream.Flush();
            bCheckSend = true;
        }
        //로그 기록
        if(bCheckSend)
            logList.Add("서버 : " + strSendMsg);
    }


    /// <summary>
    /// 메시지 전송
    /// </summary>
    public void Send(ClientInfo client, string message = "")
    {
        //서버가 연상태가 아니라면
        if (!serverReady)
            return;

        //공지가 아닌경우 입력한 텍스트 전송
        if(message == "")
        {
            // 정보 변경 구조체 초기화
            stSendMsg stSendMsgInfo = new stSendMsg();

            string strSendMsg = Text_Input.text;

            //메시지 작성
            stSendMsgInfo.sendClientName = "Server";
            stSendMsgInfo.MsgID = 2;//메시지 ID
            stSendMsgInfo.PacketSize = (ushort)Marshal.SizeOf(stSendMsgInfo);//메시지 크기
            stSendMsgInfo.strSendMsg = strSendMsg;

            //구조체 바이트화 및 전송
            byte[] SendData = GetSendMsgToByte(stSendMsgInfo);

            client.stream.Write(SendData, 0, SendData.Length);
            client.stream.Flush();

            //로그 기록
            logList.Add("서버 : " + strSendMsg);

        }
        else
        {
            try
            {
                stSendMsg stSendMsgInfo = new stSendMsg();

                stSendMsgInfo.sendClientName = "Server";
                stSendMsgInfo.MsgID = 2;//메시지 ID
                stSendMsgInfo.PacketSize = (ushort)Marshal.SizeOf(stSendMsgInfo);//메시지 크기
                stSendMsgInfo.strSendMsg = message;

                //구조체 바이트화 및 전송
                byte[] sendMessageByte = GetSendMsgToByte(stSendMsgInfo);

                //전송
                client.stream.Write(sendMessageByte, 0, sendMessageByte.Length);
                client.stream.Flush();

                //로그 기록
                logList.Add("서버 : " + message);
            }
            catch (Exception e)
            {
                Debug.Log("SendException " + e.ToString());
            }
        }

        
    }   

    /// <summary>
    /// 서버 닫기
    /// </summary>
    public void CloseSocket()
    {
        //서버를 연적이 없다면
        if (!serverReady)
        {
            return;
        }
        else//초기화
        {
            //클라이언트에게 서버 종료 선언
            BroadCast("서버 종료!");
            
            //소켓 종료 및 초기화
            if (tcpListener != null) { tcpListener.Stop(); tcpListener = null; }

            //상태 초기화
            serverReady = false;

            //쓰레드 초기화
            tcpListenerThread.Abort();
            tcpListenerThread = null;

            //연결된 클라이언트 초기화
            foreach (KeyValuePair<string, ClientInfo> client in ConnectedClients)
            {
                client.Value.stream = null;
                client.Value.clientSocket.Close();
            }
            ConnectedClients.Clear();
        }
    }

    public void BroadCast(string message)
    {
        foreach (KeyValuePair<string, ClientInfo> client in ConnectedClients)
        {
            Send(client.Value, message);
        }
    }

    /// <summary>
    /// 어플 종료시
    /// </summary>
    private void OnApplicationQuit()
    {
        CloseSocket();
    }
}
