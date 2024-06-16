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
using System.Runtime.InteropServices;//�������� ���� �����

using DefineInfo;
using static DefineData;

/// <summary>
/// Ŭ���̾�Ʈ Ŭ����
/// </summary>
public class ClientInfo
{
    public TcpClient clientSocket;//Ŭ���̾�Ʈ ����(��� ����)
    public string clientname;//Ŭ���̾�Ʈ �̸�
    public Vector3 position;//Ŭ���̾�Ʈ ��ġ
    public Quaternion quaternion;//Ŭ���̾�Ʈ ���ʹϾ�

    public NetworkStream stream; // Ŭ���̾�Ʈ ��� ����

    //���� ������ �������
    public byte[] buffer;
    //���� �����Ͱ� �߸� ��츦 ����Ͽ� �ӽù��ۿ� �����Ͽ� ����
    public byte[] tempBuffer;//�ӽù���
    public bool isTempByte;//�ӽù��� ����
    public int nTempByteSize;//�ӽù����� ũ��


    public ClientInfo(TcpClient clientSocket, string clientname = "Ŭ���̾�Ʈ", 
        Vector3 position = default, Quaternion quaternion = default)
    {
        this.clientSocket = clientSocket;
        this.clientname = clientname;
        this.position = position;
        this.quaternion = quaternion;
        this.stream = clientSocket.GetStream();

        //������ ������� �ʱ�ȭ
        this.buffer = new byte[1024];
        //�ӽù��� �ʱ�ȭ
        this.tempBuffer = new byte[1024];
        this.isTempByte = false;
        this.nTempByteSize = 0;
    }
}

public class NetworkManager_Server : MonoBehaviour
{
    //������
    private Thread tcpListenerThread;
    //������ ����
    private TcpListener tcpListener;
    //Ŭ���̾�Ʈ
    //private ClientInfo client;

    // Ŭ���̾�Ʈ ���
    private Dictionary<string,ClientInfo> ConnectedClients; // ����� Ŭ���̾�Ʈ ���
    private List<ClientInfo> disconnectedClients;  // ���� ������ Ŭ���̾�Ʈ ���

    //������, ��Ʈ
    public string ip;
    public int port;

    //���� ����
    private bool serverReady;
    //��� �޽��� �а� ���� ����
    //private NetworkStream stream;

    //�α�
    public Text ServerLog;//ui
    private List<string> logList;//data

    //���� �޽���
    public InputField Text_Input;

    //Ŭ���̾�Ʈ ��� UI
    public GameObject ButtonConnect;
    public GameObject ButtonDisConnect;
    public GameObject ClientFunctionUI;

    

    //���� ������ ó�� ����
    private Queue<stChangeInfoMsg> receive_changeInfo_MSG = new Queue<stChangeInfoMsg>();

    //������ �޽��� �������
    byte[] sendMessage;


    // Start is called before the first frame update
    void Start()
    {
        //�α� �ʱ�ȭ
        logList = new List<string>();

        //Ŭ���̾�Ʈ ��� �ʱ�ȭ
        ConnectedClients = new Dictionary<string, ClientInfo>();
        disconnectedClients = new List<ClientInfo>();

        //������ �޽��� �ʱ�ȭ
        sendMessage = new byte[1024];
    }

    // Update is called once per frame
    void Update()
    {
        //���� �����Ͱ� �ִ°��(�� ���� Ȯ��)
        if (receive_changeInfo_MSG.Count > 0)
        {
            //���ʴ�� �̾Ƴ���.
            stChangeInfoMsg CreateObjMsg = receive_changeInfo_MSG.Dequeue();

            if(ConnectedClients.ContainsKey(CreateObjMsg.sendClientName))
            {
                //�����͸� �ִ´�.
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

        //�α׸���Ʈ�� �׿��ٸ�
        if (logList.Count > 0)
        {
            //�α� ���
            WriteLog(logList[0]);
            logList.RemoveAt(0);
        }

        //���� ���¿� ���� Ŭ���̾�Ʈ ��ư Ȱ��ȭ/��Ȱ��ȭ
        ButtonConnect.SetActive(!serverReady);
        ButtonDisConnect.SetActive(!serverReady);
        ClientFunctionUI.SetActive(!serverReady);
    }

    /// <summary>
    /// ���� ���� ��ư
    /// </summary>
    public void ServerCreate()
    {
        //ip, port ����
        port = int.Parse(GameObject.Find("Text_Port").GetComponent<InputField>().text);

        // TCP���� ��� ������ ����
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequeset));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
    }

    /// <summary>
    /// ���� ������ ����
    /// </summary>
    private void ListenForIncommingRequeset()
    {
        try
        {
            // ���� ����
            tcpListener = new TcpListener(IPAddress.Any/*������ ���� ������ IP*/, port);
            tcpListener.Start();

            // ���� ���� ON
            serverReady = true;

            // �α� ���
            logList.Add("[�ý���] ���� ����(port:" + port + ")");

            // ������ ���ú� �׽� ���(Update)
            while (true)
            {
                // ������ ������ ���ٸ�
                if(!serverReady)
                    break;

                //���� �õ����� Ŭ���̾�Ʈ Ȯ��
                if(tcpListener != null && tcpListener.Pending())
                {
                    // ����� Ŭ���̾�Ʈ ��Ͽ� ����
                    string clientName = "Ŭ���̾�Ʈ" + ConnectedClients.Count.ToString();

                    ConnectedClients.Add(clientName, new ClientInfo(tcpListener.AcceptTcpClient(),clientName));
                    BroadCast(clientName + " ����!");

                    stHeader stHeaderTmp = new stHeader();

                    stHeaderTmp.MsgID = 3;
                    stHeaderTmp.sendClientName = clientName;
                    stHeaderTmp.PacketSize = (ushort)Marshal.SizeOf(stHeaderTmp);//�޽��� ũ��

                    byte[] SendData = GetHeaderToByte(stHeaderTmp);

                    ConnectedClients[clientName].stream.Write(SendData, 0, SendData.Length);
                    ConnectedClients[clientName].stream.Flush();
                }

                //���ӵ� Ŭ���̾�Ʈ ����� ��ȣ�ۿ� ó��
                foreach(KeyValuePair<string, ClientInfo> DicClient in ConnectedClients)
                {
                    ClientInfo client = DicClient.Value;

                    if (client != null)
                    {
                        //Ŭ���̾�Ʈ ���� �����
                        if (!IsConnected(client.clientSocket))
                        {
                            // �̰����� �ٷ� Ŭ���̾�Ʈ�� �����ϸ� �����尣�� ������ ���̷� ������ �߻������� ���������� Ŭ���̾�Ʈ ������� ����
                            // ���������� Ŭ���̾�Ʈ ��Ͽ� �߰�
                            disconnectedClients.Add(client);

                            continue;
                        }
                        //Ŭ���̾�Ʈ �޽��� ó��
                        else
                        {
                            //�޽����� ���Դٸ�
                            if (client.stream.DataAvailable)
                            {
                                //�޽��� ���� ���� �ʱ�ȭ
                                Array.Clear(client.buffer, 0, client.buffer.Length);

                                //�޽����� �д´�.
                                int messageLength = client.stream.Read(client.buffer, 0, client.buffer.Length);

                                //���� ó���ϴ� ����
                                //���� �о�� �޽����� ���� �޽����� ����� ���ؼ� ó���� ���� ����
                                byte[] pocessBuffer = new byte[messageLength + client.nTempByteSize];
                                
                                //���Ҵ� �޽����� �ִٸ�
                                if (client.isTempByte)
                                {
                                    //�� �κп� ���Ҵ� �޽��� ����
                                    Array.Copy(client.tempBuffer, 0, pocessBuffer, 0, client.nTempByteSize);
                                    //���� ���� �޽��� ����
                                    Array.Copy(client.buffer, 0, pocessBuffer, client.nTempByteSize, messageLength);
                                }
                                else
                                {
                                    //���Ҵ� �޽����� ������ ���� �о�� �޽����� ����
                                    Array.Copy(client.buffer, 0, pocessBuffer, 0, messageLength);
                                }

                                //ó���ؾ� �ϴ� �޽����� ���̰� 0�� �ƴ϶��
                                if (client.nTempByteSize + messageLength > 0)
                                {
                                    //���� �޽��� ó��
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
                
                //���� ������ Ŭ���̾�Ʈ ��� ó��
                for(int i = disconnectedClients.Count-1; i >= 0; i--)
                {
                    //�αױ��
                    logList.Add("[�ý���]Ŭ���̾�Ʈ ���� ����");
                    //���ӵ� Ŭ���̾�Ʈ ��Ͽ��� ����
                    ConnectedClients.Remove(disconnectedClients[i].clientname);
                    // ó���� ���������� Ŭ���̾�Ʈ ��Ͽ��� ����
                    disconnectedClients.Remove(disconnectedClients[i]);
                }
                
                //����� Ŭ���̾�Ʈ ���(connectedClients)�� �߰��� �Ǿ� foreach���� Ÿ�� ������
                //������ �ȵ��� client�� null�� �Ǵ� ������ �߻��Ͽ� �����̸� �ش�
                //������ ���ѷ����� ���� CPU ����� ����
                Thread.Sleep(10);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }

    /// <summary>
    /// Ŭ���̾�Ʈ ���� Ȯ��
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
    /// ���� �޽��� ó��
    /// </summary>
    /// <param name="client"></param>
    /// <param name="data"></param>
    private void OnIncomingData(ClientInfo client, byte[] data)
    {
        // �������� ũ�Ⱑ ����� ũ�⺸�ٵ� ������
        if (data.Length < Constants.HEADER_SIZE)
        {
            Array.Copy(data, 0, client.tempBuffer, client.nTempByteSize, data.Length);     // ���� ���� ���ۿ� ���� �޽��� ����
            client.isTempByte = true;
            client.nTempByteSize += data.Length;
            return;
        }

        //����κ� �߶󳻱�(�����ϱ�)
        byte[] headerDataByte = new byte[Constants.HEADER_SIZE];
        Array.Copy(data, 0, headerDataByte, 0, headerDataByte.Length); //��� ������ ��ŭ ������ ����
        //��� ������ ����üȭ(������)
        stHeader headerData = HeaderfromByte(headerDataByte);

        // ����� ������� ���� �޽����� ����� ������
        if (headerData.PacketSize > data.Length)
        {
            Array.Copy(data, 0, client.tempBuffer, client.nTempByteSize, data.Length);     // ���� ���� ���ۿ� ���� �޽��� ����
            client.isTempByte = true;
            client.nTempByteSize += data.Length;
            return;
        }

        //����� �޽���ũ�⸸ŭ�� �޽��� �����ϱ�
        byte[] msgData = new byte[headerData.PacketSize]; //��Ŷ �и��� ���� ���� ���� ����� ��Ŷ �����ŭ ���� ����
        Array.Copy(data, 0, msgData, 0, headerData.PacketSize); //������ ���ۿ� ��Ŷ ���� ����

        //����� �޽�����
        if (headerData.MsgID == 0)//�� ���� Ȯ��
        {
            if (ConnectedClients.ContainsKey(headerData.sendClientName))
            {
                ClientInfo clientInfo = ConnectedClients[headerData.sendClientName];

                stChangeInfoMsg stChangeInfoMsgData = new stChangeInfoMsg();

                float[] positionArray = { clientInfo.position.x, clientInfo.position.y, clientInfo.position.z };
                float[] QuaternionArray = { clientInfo.quaternion.x, clientInfo.quaternion.y, clientInfo.quaternion.z, clientInfo.quaternion.w };

                //�޽��� �ۼ�
                stChangeInfoMsgData.sendClientName = clientInfo.clientname;
                stChangeInfoMsgData.MsgID = 0;//�޽��� ID
                stChangeInfoMsgData.PacketSize = (ushort)Marshal.SizeOf(stChangeInfoMsgData);//�޽��� ũ��
                stChangeInfoMsgData.strClientName = clientInfo.clientname;
                stChangeInfoMsgData.position = positionArray;
                stChangeInfoMsgData.Quaternion = QuaternionArray;

                byte[] SendData = GetChangeInfoMsgToByte(stChangeInfoMsgData);

                clientInfo.stream.Write(SendData, 0, SendData.Length);
            }


        }
        else if(headerData.MsgID == 1)//�� ���� ����
        {
            stChangeInfoMsg stChangeInfoMsg1 = ChangeInfoMsgfromByte(msgData);
            receive_changeInfo_MSG.Enqueue(stChangeInfoMsg1);

            logList.Add(client.clientname + " : �� ���� ���� �޽��� ����" );
        }
        else if (headerData.MsgID == 2)//�޽���
        {
            stSendMsg SendMsgInfo = SendMsgfromByte(msgData);

            BroadCastByte(msgData);
            //�޽��� �α׿� ���
            logList.Add(client.clientname + " : " + SendMsgInfo.strSendMsg);
        }
        else//�ĺ����� ���� ID
        {

        }

        // ��� �޽����� ó���Ǽ� ���� �޽����� ���� ��� 
        if (data.Length == msgData.Length)
        {
            client.isTempByte = false;
            client.nTempByteSize = 0;
        }
        // �޽��� ó�� �� �޽����� �����ִ� ���
        else
        {
            //�ӽ� ���� û��
            Array.Clear(client.tempBuffer, 0, client.tempBuffer.Length);

            //������ ���ۿ� ��Ŷ ���� ����
            Array.Copy(data, msgData.Length, client.tempBuffer, 0, data.Length - (msgData.Length));// �ӽ� ���� ���ۿ� ���� �޽��� ����
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
    /// �α� ����
    /// </summary>
    /// <param name="message"></param>
    public void WriteLog(/*Time*/string message)
    {
        ServerLog.GetComponent<Text>().text += message + "\n";
    }

    public void SendMsg()
    {
        // ���� ���� ����ü �ʱ�ȭ
        stSendMsg stSendMsgInfo = new stSendMsg();

        string strSendMsg = Text_Input.text;

        //�޽��� �ۼ�
        stSendMsgInfo.sendClientName = "Server";
        stSendMsgInfo.MsgID = 2;//�޽��� ID
        stSendMsgInfo.PacketSize = (ushort)Marshal.SizeOf(stSendMsgInfo);//�޽��� ũ��
        stSendMsgInfo.strSendMsg = strSendMsg;

        //����ü ����Ʈȭ �� ����
        byte[] SendData = GetSendMsgToByte(stSendMsgInfo);

        bool bCheckSend = false;
        foreach (KeyValuePair<string, ClientInfo> client in ConnectedClients)
        {
            client.Value.stream.Write(SendData, 0, SendData.Length);
            client.Value.stream.Flush();
            bCheckSend = true;
        }
        //�α� ���
        if(bCheckSend)
            logList.Add("���� : " + strSendMsg);
    }


    /// <summary>
    /// �޽��� ����
    /// </summary>
    public void Send(ClientInfo client, string message = "")
    {
        //������ �����°� �ƴ϶��
        if (!serverReady)
            return;

        //������ �ƴѰ�� �Է��� �ؽ�Ʈ ����
        if(message == "")
        {
            // ���� ���� ����ü �ʱ�ȭ
            stSendMsg stSendMsgInfo = new stSendMsg();

            string strSendMsg = Text_Input.text;

            //�޽��� �ۼ�
            stSendMsgInfo.sendClientName = "Server";
            stSendMsgInfo.MsgID = 2;//�޽��� ID
            stSendMsgInfo.PacketSize = (ushort)Marshal.SizeOf(stSendMsgInfo);//�޽��� ũ��
            stSendMsgInfo.strSendMsg = strSendMsg;

            //����ü ����Ʈȭ �� ����
            byte[] SendData = GetSendMsgToByte(stSendMsgInfo);

            client.stream.Write(SendData, 0, SendData.Length);
            client.stream.Flush();

            //�α� ���
            logList.Add("���� : " + strSendMsg);

        }
        else
        {
            try
            {
                stSendMsg stSendMsgInfo = new stSendMsg();

                stSendMsgInfo.sendClientName = "Server";
                stSendMsgInfo.MsgID = 2;//�޽��� ID
                stSendMsgInfo.PacketSize = (ushort)Marshal.SizeOf(stSendMsgInfo);//�޽��� ũ��
                stSendMsgInfo.strSendMsg = message;

                //����ü ����Ʈȭ �� ����
                byte[] sendMessageByte = GetSendMsgToByte(stSendMsgInfo);

                //����
                client.stream.Write(sendMessageByte, 0, sendMessageByte.Length);
                client.stream.Flush();

                //�α� ���
                logList.Add("���� : " + message);
            }
            catch (Exception e)
            {
                Debug.Log("SendException " + e.ToString());
            }
        }

        
    }   

    /// <summary>
    /// ���� �ݱ�
    /// </summary>
    public void CloseSocket()
    {
        //������ ������ ���ٸ�
        if (!serverReady)
        {
            return;
        }
        else//�ʱ�ȭ
        {
            //Ŭ���̾�Ʈ���� ���� ���� ����
            BroadCast("���� ����!");
            
            //���� ���� �� �ʱ�ȭ
            if (tcpListener != null) { tcpListener.Stop(); tcpListener = null; }

            //���� �ʱ�ȭ
            serverReady = false;

            //������ �ʱ�ȭ
            tcpListenerThread.Abort();
            tcpListenerThread = null;

            //����� Ŭ���̾�Ʈ �ʱ�ȭ
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
    /// ���� �����
    /// </summary>
    private void OnApplicationQuit()
    {
        CloseSocket();
    }
}
