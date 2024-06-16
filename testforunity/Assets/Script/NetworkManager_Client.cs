using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using UnityEngine.UI;
using System.Runtime.InteropServices;//마샬링을 위한 어셈블리

using DefineInfo;
using static DefineData;

public class NetworkManager_Client : MonoBehaviour
{
    //쓰레드
    private Thread tcpListenerThread;

    //소켓
    private TcpClient socketConnection;
    private NetworkStream stream;

    //상태
    private bool clientReady;

    //ip, port
    public string ip;
    public int port;

    //로그
    public Text ClientLog;
    private List<string> logList;
    //전송 메시지
    public GameObject Text_Input;

    //서버 기능 UI
    public GameObject ButtonServerOpen;
    public GameObject ButtonServerClose;

    //받은 데이터 저장공간
    byte[] buffer;
    //받은 데이터가 잘릴 경우를 대비하여 임시버퍼에 저장하여 관리
    byte[] tempBuffer;//임시버퍼
    bool isTempByte;//임시버퍼 유무
    int nTempByteSize;//임시버퍼의 크기

    //보내는 메시지 저장공간
    byte[] sendMessage = new byte[1024];
    bool bChangeMyName = false;
    
    // 클라이언트명(ID)
    string strRcvMyName = "";
    
    // Start is called before the first frame update
    void Start()
    {
        //로그 초기화
        logList = new List<string>();

        //받은 데이터 저장공간 초기화
        buffer = new byte[1024];
        //임시버퍼 초기화
        tempBuffer = new byte[1024];
        isTempByte = false;
        nTempByteSize = 0;
    }

    // Update is called once per frame
    void Update()
    {
        //로그리스트에 쌓였다면
        if (logList.Count > 0)
        {
            //배출
            WriteLog(logList[0]);
            logList.RemoveAt(0);
        }

        if(bChangeMyName)
        {
            bChangeMyName = false;
            GameObject.Find("Text_Name").GetComponent<InputField>().text = strRcvMyName;
        }

        //클라이언트 상태에 따라 서버 버튼 활성화/비활성화
        ButtonServerOpen.SetActive(!clientReady);
        ButtonServerClose.SetActive(!clientReady);
    }

    /// <summary>
    /// 서버 연결
    /// </summary>
    public void ConnectToTcpServer()
    {
        //ip, port 설정
        ip = GameObject.Find("Text_IP").GetComponent<InputField>().text;
        port = int.Parse(GameObject.Find("Text_Port").GetComponent<InputField>().text);

        // TCP클라이언트 스레드 시작
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequeset));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
    }

    /// <summary>
    /// TCP클라이언트 쓰레드
    /// </summary>
    private void ListenForIncommingRequeset()
    {
        try
        {
            //연결
            socketConnection = new TcpClient(ip, port);
            stream = socketConnection.GetStream();
            clientReady = true;

            //로그 기록
            logList.Add("시스템 : 서버 연결(ip:" + ip + "/port:" + port + ")");

            //데이터 리시브 항시 대기
            while (true)
            {
                //연결 끊김 감지
                if (!IsConnected(socketConnection))
                {
                    //연결 해제
                    DisConnect();
                    break;
                }

                //연결 중
                if (clientReady)
                {
                    //메시지가 들어왔다면
                    if (stream.DataAvailable)
                    {
                        //메시지 저장 공간 초기화
                        Array.Clear(buffer, 0, buffer.Length);

                        //메시지를 읽는다.
                        int messageLength = stream.Read(buffer, 0, buffer.Length);

                        //실제 처리하는 버퍼
                        byte[] pocessBuffer = new byte[messageLength + nTempByteSize];//지금 읽어온 메시지에 남은 메시지의 사이즈를 더해서 처리할 버퍼 생성
                        //남았던 메시지가 있다면
                        if (isTempByte)
                        {
                            //앞 부분에 남았던 메시지 복사
                            Array.Copy(tempBuffer, 0, pocessBuffer, 0, nTempByteSize);
                            //지금 읽은 메시지 복사
                            Array.Copy(buffer, 0, pocessBuffer, nTempByteSize, messageLength);
                        }
                        else
                        {
                            //남았던 메시지가 없으면 지금 읽어온 메시지를 저장
                            Array.Copy(buffer, 0, pocessBuffer, 0, messageLength);
                        }

                        //처리해야 하는 메시지의 길이가 0이 아니라면
                        if (nTempByteSize + messageLength > 0)
                        {
                            //받은 메시지 처리
                            OnIncomingData(pocessBuffer);
                        }
                    }
                    else if(nTempByteSize > 0)
                    {
                        byte[] pocessBuffer = new byte[nTempByteSize];
                        Array.Copy(tempBuffer, 0, pocessBuffer, 0, nTempByteSize);
                        OnIncomingData(pocessBuffer);
                    }
                }
                else//socketReady == false
                {
                    //연결 해제시
                    break;
                }
            }
        }
        catch (SocketException socketException)
        {
            //로그 기록
            logList.Add("시스템 : 서버 연결 실패(ip:" + ip + "/port:" + port + ")");
            logList.Add(socketException.ToString());

            //클라이언트 연결 실패
            clientReady = false;
        }
    }

    /// <summary>
    /// 접속 확인
    /// </summary>
    /// <param name="client"></param>
    /// <returns></returns>
    private bool IsConnected(TcpClient client)
    {
        try
        {
            if (client != null && client.Client != null && client.Client.Connected)
            {
                if (client.Client.Poll(0, SelectMode.SelectRead))
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
    /// <param name="data"></param>
    private void OnIncomingData(byte[] data)
    {

        // 데이터의 크기가 헤더의 크기보다도 작으면
        if (data.Length < Constants.HEADER_SIZE)
        {
            Array.Copy(data, 0, tempBuffer, nTempByteSize, data.Length);     // 임지 저장 버퍼에 지금 메시지 저장
            isTempByte = true;
            nTempByteSize += data.Length;
            return;
        }


        //헤더부분 잘라내기(복사하기)
        byte[] headerDataByte = new byte[Constants.HEADER_SIZE];// 헤더 사이즈는 6(ID:ushort + Size:int)
        Array.Copy(data, 0, headerDataByte, 0, headerDataByte.Length);// 헤더 사이즈 만큼 데이터 복사
        //헤더 데이터 구조체화(마샬링)
        stHeader headerData = HeaderfromByte(headerDataByte);


        // 헤더의 사이즈보다 남은 메시지의 사이즈가 작으면
        if (headerData.PacketSize > data.Length)
        {
            Array.Copy(data, 0, tempBuffer, nTempByteSize, data.Length);     // 임지 저장 버퍼에 지금 메시지 저장
            isTempByte = true;
            nTempByteSize += data.Length;
            return;
        }

        //헤더의 메시지크기만큼만 메시지 복사하기
        byte[] msgData = new byte[headerData.PacketSize]; //패킷 분리를 위한 현재 읽은 헤더의 패킷 사이즈만큼 버퍼 생성
        Array.Copy(data, 0, msgData, 0, headerData.PacketSize); //생성된 버퍼에 패킷 정보 복사

        //헤더의 메시지가
        if (headerData.MsgID == 0)// 내 정보 확인 메시지
        {
            stChangeInfoMsg stChangeInfoMsgData = ChangeInfoMsgfromByte(msgData);

            string strTmp = "내 정보\n" +
            "Name : " + stChangeInfoMsgData.strClientName + "\n" +
            "Position_X : " + stChangeInfoMsgData.position[0].ToString() + "\n" +
            "Position_Y : " + stChangeInfoMsgData.position[1].ToString() + "\n" +
            "Position_Z : " + stChangeInfoMsgData.position[2].ToString() + "\n" +
            "Quaternion_X : " + stChangeInfoMsgData.Quaternion[0].ToString() + "\n" +
            "Quaternion_Y : " + stChangeInfoMsgData.Quaternion[1].ToString() + "\n" +
            "Quaternion_Z : " + stChangeInfoMsgData.Quaternion[2].ToString() + "\n" +
            "Quaternion_W : " + stChangeInfoMsgData.Quaternion[3].ToString();

            //메시지 로그에 기록
            logList.Add(headerData.sendClientName + " : \n" + strTmp);
        }
        else if (headerData.MsgID == 2)//메시지
        {
            stSendMsg SendMsgInfo = SendMsgfromByte(msgData);
            //메시지 로그에 기록
            logList.Add(headerData.sendClientName + " : " + SendMsgInfo.strSendMsg);
        }
        else if (headerData.MsgID == 3)// 클라이언트명 정보
        {

            bChangeMyName = true;
            strRcvMyName = headerData.sendClientName;

            //메시지 로그에 기록
            logList.Add(headerData.sendClientName + " : " + "너의 이름은 -> " + headerData.sendClientName);
        }
        else//식별되지 않은 ID
        {

        }

        // 모든 메시지가 처리되서 남은 메시지가 없을 경우 
        if (data.Length == msgData.Length)
        {
            isTempByte = false;
            nTempByteSize = 0;
        }
        // 메시지 처리 후 메시지가 남아있는 경우
        else
        {
            //임시 버퍼 청소
            Array.Clear(tempBuffer, 0, tempBuffer.Length);

            //생성된 버퍼에 패킷 정보 복사
            Array.Copy(data, msgData.Length, tempBuffer, 0, data.Length - (msgData.Length));// 임시 저장 버퍼에 남은 메시지 저장
            isTempByte = true;
            nTempByteSize += data.Length - (msgData.Length);
        }

    }

    /// <summary>
    /// 메시지 전송
    /// </summary>
    public void Send()
    {
        //연결상태가 아닌 경우
        if(socketConnection == null)
        {
            return;
        }

        // 정보 변경 구조체 초기화
        stSendMsg stSendMsgInfo = new stSendMsg();
        string strSendMsg = Text_Input.GetComponent<InputField>().text;

        //메시지 작성
        string name = GameObject.Find("Text_Name").GetComponent<InputField>().text;

        stSendMsgInfo.sendClientName = name;
        stSendMsgInfo.MsgID = 2;//메시지 ID
        stSendMsgInfo.PacketSize = (ushort)Marshal.SizeOf(stSendMsgInfo);//메시지 크기
        stSendMsgInfo.strSendMsg = strSendMsg;

        //구조체 바이트화 및 전송
        SendMsg(GetSendMsgToByte(stSendMsgInfo));
    }

    /// <summary>
    /// 로그 전시
    /// </summary>
    /// <param name="message"></param>
    public void WriteLog(/*Time*/string message)
    {
        ClientLog.GetComponent<Text>().text += message + "\n";
    }

    /// <summary>
    /// 연결 해제
    /// </summary>
    public void DisConnect()
    {
        //미 연결시
        if(socketConnection == null)
        {
            return;
        }

        //로그 기록
        logList.Add("[시스템] 클라이언트 연결 해제");

        //상태 초기화
        clientReady = false;

        //stream 초기화
        stream.Close();

        //소켓 초기화
        socketConnection.Close();
        socketConnection = null;

        //쓰레드 초기화
        tcpListenerThread.Abort();
        tcpListenerThread = null;
    }

    /// <summary>
    /// 어플 종료시
    /// </summary>
    private void OnApplicationQuit()
    {
        DisConnect();
    }

    /// <summary>
    /// 내 정보 확인
    /// </summary>
    public void CheckMyInformation()
    {
        //메시지 초기화
        sendMessage = new byte[1024];

        // 내 정보 확인 구조체 초기화
        stHeader stCheckInfoMsgData = new stHeader();

        //메시지 작성
        string name = GameObject.Find("Text_Name").GetComponent<InputField>().text;
        stCheckInfoMsgData.sendClientName = name;
        stCheckInfoMsgData.MsgID = 0;
        stCheckInfoMsgData.PacketSize = (ushort)Marshal.SizeOf(stCheckInfoMsgData);

        //구조체 바이트화 및 전송
        SendMsg(GetHeaderToByte(stCheckInfoMsgData));
    }

    /// <summary>
    /// 내 정보 변경
    /// </summary>
    public void ChangeMyInformation()
    {
        //메시지 초기화
        sendMessage = new byte[1024];

        // 정보 변경 구조체 초기화
        stChangeInfoMsg stChangeInfoMsgData = new stChangeInfoMsg();

        //변경할 내 정보 가져오기
        string name = GameObject.Find("Text_Name").GetComponent<InputField>().text;
        float positionX = float.Parse(GameObject.Find("Text_Position_X").GetComponent<InputField>().text);
        float positionY = float.Parse(GameObject.Find("Text_Position_Y").GetComponent<InputField>().text);
        float positionZ = float.Parse(GameObject.Find("Text_Position_Z").GetComponent<InputField>().text);
        float[] positionArray = { positionX, positionY, positionZ };

        float QuaternionX = float.Parse(GameObject.Find("Text_Quaternion_X").GetComponent<InputField>().text);
        float QuaternionY = float.Parse(GameObject.Find("Text_Quaternion_Y").GetComponent<InputField>().text);
        float QuaternionZ = float.Parse(GameObject.Find("Text_Quaternion_Z").GetComponent<InputField>().text);
        float QuaternionW = float.Parse(GameObject.Find("Text_Quaternion_W").GetComponent<InputField>().text);
        float[] QuaternionArray = { QuaternionX, QuaternionY, QuaternionZ, QuaternionW };


        //메시지 작성
        stChangeInfoMsgData.sendClientName = strRcvMyName;
        stChangeInfoMsgData.MsgID = 1;//메시지 ID
        stChangeInfoMsgData.PacketSize = (ushort)Marshal.SizeOf(stChangeInfoMsgData);//메시지 크기
        stChangeInfoMsgData.strClientName = name;
        stChangeInfoMsgData.position = positionArray;
        stChangeInfoMsgData.Quaternion = QuaternionArray;

        //구조체 바이트화 및 전송
        SendMsg(GetChangeInfoMsgToByte(stChangeInfoMsgData));

        strRcvMyName = name;
    }

    /// <summary>
    /// 매개변수 메시지 보내기
    /// </summary>
    private void SendMsg(byte[] message)
    {
        //연결상태가 아닌 경우
        if (socketConnection == null)
        {
            return;
        }

        //전송
        stream.Write(message,0, message.Length);
        stream.Flush();
    }
}
