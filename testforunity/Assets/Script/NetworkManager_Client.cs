using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using UnityEngine.UI;
using System.Runtime.InteropServices;//�������� ���� �������

using DefineInfo;
using static DefineData;

public class NetworkManager_Client : MonoBehaviour
{
    //������ 1
    private Thread tcpListenerThread;

    //����
    private TcpClient socketConnection;
    private NetworkStream stream;

    //����
    private bool clientReady;

    //ip, port
    public string ip;
    public int port;

    //�α�
    public Text ClientLog;
    private List<string> logList;
    //���� �޽���
    public GameObject Text_Input;

    //���� ��� UI
    public GameObject ButtonServerOpen;
    public GameObject ButtonServerClose;

    //���� ������ �������
    byte[] buffer;
    //���� �����Ͱ� �߸� ��츦 ����Ͽ� �ӽù��ۿ� �����Ͽ� ����
    byte[] tempBuffer;//�ӽù���
    bool isTempByte;//�ӽù��� ����
    int nTempByteSize;//�ӽù����� ũ��

    //������ �޽��� �������
    byte[] sendMessage = new byte[1024];
    bool bChangeMyName = false;
    
    // Ŭ���̾�Ʈ��(ID)
    string strRcvMyName = "";
    
    // Start is called before the first frame update
    void Start()
    {
        //�α� �ʱ�ȭ
        logList = new List<string>();

        //���� ������ ������� �ʱ�ȭ
        buffer = new byte[1024];
        //�ӽù��� �ʱ�ȭ
        tempBuffer = new byte[1024];
        isTempByte = false;
        nTempByteSize = 0;
    }

    // Update is called once per frame
    void Update()
    {
        //�α׸���Ʈ�� �׿��ٸ�
        if (logList.Count > 0)
        {
            //����
            WriteLog(logList[0]);
            logList.RemoveAt(0);
        }

        if(bChangeMyName)
        {
            bChangeMyName = false;
            GameObject.Find("Text_Name").GetComponent<InputField>().text = strRcvMyName;
        }

        //Ŭ���̾�Ʈ ���¿� ���� ���� ��ư Ȱ��ȭ/��Ȱ��ȭ
        ButtonServerOpen.SetActive(!clientReady);
        ButtonServerClose.SetActive(!clientReady);
    }

    /// <summary>
    /// ���� ����
    /// </summary>
    public void ConnectToTcpServer()
    {
        //ip, port ����
        ip = GameObject.Find("Text_IP").GetComponent<InputField>().text;
        port = int.Parse(GameObject.Find("Text_Port").GetComponent<InputField>().text);

        // TCPŬ���̾�Ʈ ������ ����
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequeset));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
    }

    /// <summary>
    /// TCPŬ���̾�Ʈ ������
    /// </summary>
    private void ListenForIncommingRequeset()
    {
        try
        {
            //����
            socketConnection = new TcpClient(ip, port);
            stream = socketConnection.GetStream();
            clientReady = true;

            //�α� ���
            logList.Add("�ý��� : ���� ����(ip:" + ip + "/port:" + port + ")");

            //������ ���ú� �׽� ���
            while (true)
            {
                //���� ���� ����
                if (!IsConnected(socketConnection))
                {
                    //���� ����
                    DisConnect();
                    break;
                }

                //���� ��
                if (clientReady)
                {
                    //�޽����� ���Դٸ�
                    if (stream.DataAvailable)
                    {
                        //�޽��� ���� ���� �ʱ�ȭ
                        Array.Clear(buffer, 0, buffer.Length);

                        //�޽����� �д´�.
                        int messageLength = stream.Read(buffer, 0, buffer.Length);

                        //���� ó���ϴ� ����
                        byte[] pocessBuffer = new byte[messageLength + nTempByteSize];//���� �о�� �޽����� ���� �޽����� ����� ���ؼ� ó���� ���� ����
                        //���Ҵ� �޽����� �ִٸ�
                        if (isTempByte)
                        {
                            //�� �κп� ���Ҵ� �޽��� ����
                            Array.Copy(tempBuffer, 0, pocessBuffer, 0, nTempByteSize);
                            //���� ���� �޽��� ����
                            Array.Copy(buffer, 0, pocessBuffer, nTempByteSize, messageLength);
                        }
                        else
                        {
                            //���Ҵ� �޽����� ������ ���� �о�� �޽����� ����
                            Array.Copy(buffer, 0, pocessBuffer, 0, messageLength);
                        }

                        //ó���ؾ� �ϴ� �޽����� ���̰� 0�� �ƴ϶��
                        if (nTempByteSize + messageLength > 0)
                        {
                            //���� �޽��� ó��
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
                    //���� ������
                    break;
                }
            }
        }
        catch (SocketException socketException)
        {
            //�α� ���
            logList.Add("�ý��� : ���� ���� ����(ip:" + ip + "/port:" + port + ")");
            logList.Add(socketException.ToString());

            //Ŭ���̾�Ʈ ���� ����
            clientReady = false;
        }
    }

    /// <summary>
    /// ���� Ȯ��
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
    /// ���� �޽��� ó��
    /// </summary>
    /// <param name="data"></param>
    private void OnIncomingData(byte[] data)
    {

        // �������� ũ�Ⱑ ����� ũ�⺸�ٵ� ������
        if (data.Length < Constants.HEADER_SIZE)
        {
            Array.Copy(data, 0, tempBuffer, nTempByteSize, data.Length);     // ���� ���� ���ۿ� ���� �޽��� ����
            isTempByte = true;
            nTempByteSize += data.Length;
            return;
        }


        //����κ� �߶󳻱�(�����ϱ�)
        byte[] headerDataByte = new byte[Constants.HEADER_SIZE];// ��� ������� 6(ID:ushort + Size:int)
        Array.Copy(data, 0, headerDataByte, 0, headerDataByte.Length);// ��� ������ ��ŭ ������ ����
        //��� ������ ����üȭ(������)
        stHeader headerData = HeaderfromByte(headerDataByte);


        // ����� ������� ���� �޽����� ����� ������
        if (headerData.PacketSize > data.Length)
        {
            Array.Copy(data, 0, tempBuffer, nTempByteSize, data.Length);     // ���� ���� ���ۿ� ���� �޽��� ����
            isTempByte = true;
            nTempByteSize += data.Length;
            return;
        }

        //����� �޽���ũ�⸸ŭ�� �޽��� �����ϱ�
        byte[] msgData = new byte[headerData.PacketSize]; //��Ŷ �и��� ���� ���� ���� ����� ��Ŷ �����ŭ ���� ����
        Array.Copy(data, 0, msgData, 0, headerData.PacketSize); //������ ���ۿ� ��Ŷ ���� ����

        //����� �޽�����
        if (headerData.MsgID == 0)// �� ���� Ȯ�� �޽���
        {
            stChangeInfoMsg stChangeInfoMsgData = ChangeInfoMsgfromByte(msgData);

            string strTmp = "�� ����\n" +
            "Name : " + stChangeInfoMsgData.strClientName + "\n" +
            "Position_X : " + stChangeInfoMsgData.position[0].ToString() + "\n" +
            "Position_Y : " + stChangeInfoMsgData.position[1].ToString() + "\n" +
            "Position_Z : " + stChangeInfoMsgData.position[2].ToString() + "\n" +
            "Quaternion_X : " + stChangeInfoMsgData.Quaternion[0].ToString() + "\n" +
            "Quaternion_Y : " + stChangeInfoMsgData.Quaternion[1].ToString() + "\n" +
            "Quaternion_Z : " + stChangeInfoMsgData.Quaternion[2].ToString() + "\n" +
            "Quaternion_W : " + stChangeInfoMsgData.Quaternion[3].ToString();

            //�޽��� �α׿� ���
            logList.Add(headerData.sendClientName + " : \n" + strTmp);
        }
        else if (headerData.MsgID == 2)//�޽���
        {
            stSendMsg SendMsgInfo = SendMsgfromByte(msgData);
            //�޽��� �α׿� ���
            logList.Add(headerData.sendClientName + " : " + SendMsgInfo.strSendMsg);
        }
        else if (headerData.MsgID == 3)// Ŭ���̾�Ʈ�� ����
        {

            bChangeMyName = true;
            strRcvMyName = headerData.sendClientName;

            //�޽��� �α׿� ���
            logList.Add(headerData.sendClientName + " : " + "���� �̸��� -> " + headerData.sendClientName);
        }
        else//�ĺ����� ���� ID
        {

        }

        // ��� �޽����� ó���Ǽ� ���� �޽����� ���� ��� 
        if (data.Length == msgData.Length)
        {
            isTempByte = false;
            nTempByteSize = 0;
        }
        // �޽��� ó�� �� �޽����� �����ִ� ���
        else
        {
            //�ӽ� ���� û��
            Array.Clear(tempBuffer, 0, tempBuffer.Length);

            //������ ���ۿ� ��Ŷ ���� ����
            Array.Copy(data, msgData.Length, tempBuffer, 0, data.Length - (msgData.Length));// �ӽ� ���� ���ۿ� ���� �޽��� ����
            isTempByte = true;
            nTempByteSize += data.Length - (msgData.Length);
        }

    }

    /// <summary>
    /// �޽��� ����
    /// </summary>
    public void Send()
    {
        //������°� �ƴ� ���
        if(socketConnection == null)
        {
            return;
        }

        // ���� ���� ����ü �ʱ�ȭ
        stSendMsg stSendMsgInfo = new stSendMsg();
        string strSendMsg = Text_Input.GetComponent<InputField>().text;

        //�޽��� �ۼ�
        string name = GameObject.Find("Text_Name").GetComponent<InputField>().text;

        stSendMsgInfo.sendClientName = name;
        stSendMsgInfo.MsgID = 2;//�޽��� ID
        stSendMsgInfo.PacketSize = (ushort)Marshal.SizeOf(stSendMsgInfo);//�޽��� ũ��
        stSendMsgInfo.strSendMsg = strSendMsg;

        //����ü ����Ʈȭ �� ����
        SendMsg(GetSendMsgToByte(stSendMsgInfo));
    }

    /// <summary>
    /// �α� ����
    /// </summary>
    /// <param name="message"></param>
    public void WriteLog(/*Time*/string message)
    {
        ClientLog.GetComponent<Text>().text += message + "\n";
    }

    /// <summary>
    /// ���� ����
    /// </summary>
    public void DisConnect()
    {
        //�� �����
        if(socketConnection == null)
        {
            return;
        }

        //�α� ���
        logList.Add("[�ý���] Ŭ���̾�Ʈ ���� ����");

        //���� �ʱ�ȭ
        clientReady = false;

        //stream �ʱ�ȭ
        stream.Close();

        //���� �ʱ�ȭ
        socketConnection.Close();
        socketConnection = null;

        //������ �ʱ�ȭ
        tcpListenerThread.Abort();
        tcpListenerThread = null;
    }

    /// <summary>
    /// ���� �����
    /// </summary>
    private void OnApplicationQuit()
    {
        DisConnect();
    }

    /// <summary>
    /// �� ���� Ȯ��
    /// </summary>
    public void CheckMyInformation()
    {
        //�޽��� �ʱ�ȭ
        sendMessage = new byte[1024];

        // �� ���� Ȯ�� ����ü �ʱ�ȭ
        stHeader stCheckInfoMsgData = new stHeader();

        //�޽��� �ۼ�
        string name = GameObject.Find("Text_Name").GetComponent<InputField>().text;
        stCheckInfoMsgData.sendClientName = name;
        stCheckInfoMsgData.MsgID = 0;
        stCheckInfoMsgData.PacketSize = (ushort)Marshal.SizeOf(stCheckInfoMsgData);

        //����ü ����Ʈȭ �� ����
        SendMsg(GetHeaderToByte(stCheckInfoMsgData));
    }

    /// <summary>
    /// �� ���� ����
    /// </summary>
    public void ChangeMyInformation()
    {
        //�޽��� �ʱ�ȭ
        sendMessage = new byte[1024];

        // ���� ���� ����ü �ʱ�ȭ
        stChangeInfoMsg stChangeInfoMsgData = new stChangeInfoMsg();

        //������ �� ���� ��������
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


        //�޽��� �ۼ�
        stChangeInfoMsgData.sendClientName = strRcvMyName;
        stChangeInfoMsgData.MsgID = 1;//�޽��� ID
        stChangeInfoMsgData.PacketSize = (ushort)Marshal.SizeOf(stChangeInfoMsgData);//�޽��� ũ��
        stChangeInfoMsgData.strClientName = name;
        stChangeInfoMsgData.position = positionArray;
        stChangeInfoMsgData.Quaternion = QuaternionArray;

        //����ü ����Ʈȭ �� ����
        SendMsg(GetChangeInfoMsgToByte(stChangeInfoMsgData));

        strRcvMyName = name;
    }

    /// <summary>
    /// �Ű����� �޽��� ������
    /// </summary>
    private void SendMsg(byte[] message)
    {
        //������°� �ƴ� ���
        if (socketConnection == null)
        {
            return;
        }

        //����
        stream.Write(message,0, message.Length);
        stream.Flush();
    }
}
