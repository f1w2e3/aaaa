using System.Runtime.InteropServices;
using System;
using UnityEngine;
using DefineInfo;

namespace DefineInfo
{
    /// <summary>
    /// ��� Ŭ����
    /// </summary>
    static public class Constants
    {
        public const int HEADER_SIZE = 36;//��� ������� 36(ID:ushort + Size:ushort + �̸� : 32)
        public const int MAX_NAME_BYTE = 32;//�̸��� �ִ� ����Ʈ �� : �ѱ�10����, �������32����
        public const int MAX_SEND_MSG_BYTE = 100;//���� �޽����� �ִ� ����Ʈ �� : �ѱ�33����, �������10����
    }
    }

public class DefineData
{
    /// <summary>
    /// ��� ����ü ������
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*�����¼������(Queue)*/, Pack = 1/*�����͸� ���� ����*/)]
    public struct stHeader
    {
        [MarshalAs(UnmanagedType.ByValTStr/*string*/, SizeConst = (int)(Constants.MAX_NAME_BYTE)/*Ŭ���̾�Ʈ �̸��� �ִ� ����Ʈ*/)]
        public string sendClientName; // Ŭ���̾�Ʈ �̸�
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 1/*ushort size*/)]
        public UInt16 MsgID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 1/*ushort size*/)]
        public UInt16 PacketSize; // �޽��� ũ��
    }
    /// <summary>
    /// ��� ����ü ������ �Լ�(Byte->����ü)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stHeader HeaderfromByte(byte[] arr)
    {
        //����ü �ʱ�ȭ
        stHeader str = default(stHeader);
        int size = Marshal.SizeOf(str);//����ü Size
        //Size��ŭ �޸� �Ҵ�(�޸� �ڸ� ������)
        IntPtr ptr = Marshal.AllocHGlobal(size);
        //�����͸� �����Ͽ� �޸𸮿� �ֱ�(������ �Է�)
        Marshal.Copy(arr, 0, ptr, size);
        //����ü�� �ֱ�(�Է� ������ ���� �ؼ� ����ü�� �ֱ�)
        str = (stHeader)Marshal.PtrToStructure(ptr, str.GetType());
        //�Ҵ��� �޸� ����
        Marshal.FreeHGlobal(ptr);
        //����ü ����S
        return str;
    }
    /// <summary>
    /// ��� ����ü ������ �Լ�(����ü->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetHeaderToByte(stHeader str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);
        return arr;
    }
    /// <summary>
    /// �� ���� ���� �޽��� ����ü ������
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stChangeInfoMsg
    {
        [MarshalAs(UnmanagedType.ByValTStr/*string*/, SizeConst = (int)(Constants.MAX_NAME_BYTE)/*Ŭ���̾�Ʈ �̸��� �ִ� ����Ʈ*/)]
        public string sendClientName; // Ŭ���̾�Ʈ �̸�
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 1/*ushort size*/)]
        public UInt16 MsgID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 1/*ushort size*/)]
        public UInt16 PacketSize; // �������κ� �޽��� ũ��
        [MarshalAs(UnmanagedType.ByValTStr/*string*/, SizeConst = (int)(Constants.MAX_NAME_BYTE)/*Ŭ���̾�Ʈ �̸��� �ִ� ����Ʈ*/)]
        public string strClientName; // Ŭ���̾�Ʈ �̸�
        [MarshalAs(UnmanagedType.ByValArray/*float array*/, SizeConst = 3)]
        public float[] position; // Ŭ���̾�Ʈ ��ġ XYZ��ǥ
        [MarshalAs(UnmanagedType.ByValArray/*float array*/, SizeConst = 4)]
        public float[] Quaternion; // Ŭ���̾�Ʈ ���ʹϾ� XYZW
    }
    /// <summary>
    /// �� ���� ���� �޽��� ����ü ������ �Լ�(Byte->����ü)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stChangeInfoMsg ChangeInfoMsgfromByte(byte[] arr)
    {
        //����ü �ʱ�ȭ
        stChangeInfoMsg str = default(stChangeInfoMsg);
        int size = Marshal.SizeOf(str);//����ü Size
        //Size��ŭ �޸� �Ҵ�(�޸� �ڸ� ������)
        IntPtr ptr = Marshal.AllocHGlobal(size);
        //�����͸� �����Ͽ� �޸𸮿� �ֱ�(������ �Է�)
        Marshal.Copy(arr, 0, ptr, size);
        //����ü�� �ֱ�(�Է� ������ ���� �ؼ� ����ü�� �ֱ�)
        str = (stChangeInfoMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //�Ҵ��� �޸� ����
        Marshal.FreeHGlobal(ptr);
        //����ü ����
        return str;
    }

    /// <summary>
    /// �� ���� ���� �޽��� ����ü ������ �Լ�(����ü->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetChangeInfoMsgToByte(stChangeInfoMsg str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);
        return arr;
    }
    /// <summary>
    /// ���� �޽��� ����ü ������
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stSendMsg
    {
        [MarshalAs(UnmanagedType.ByValTStr/*string*/, SizeConst = (int)(Constants.MAX_NAME_BYTE)/*Ŭ���̾�Ʈ �̸��� �ִ� ����Ʈ*/)]
        public string sendClientName; // Ŭ���̾�Ʈ �̸�
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 MsgID; // �޽��� ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // �޽��� ũ��

        [MarshalAs(UnmanagedType.ByValTStr/*string*/, SizeConst = (int)(Constants.MAX_SEND_MSG_BYTE)/*���� �޽����� �ִ� ����Ʈ*/)]
        public string strSendMsg; // ���� �޽���

    }
    /// <summary>
    /// �� ���� ���� �޽��� ����ü ������ �Լ�(Byte->����ü)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stSendMsg SendMsgfromByte(byte[] arr)
    {
        //����ü �ʱ�ȭ
        stSendMsg str = default(stSendMsg);
        int size = Marshal.SizeOf(str);//����ü Size

        //Size��ŭ �޸� �Ҵ�(�޸� �ڸ� ������)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //�����͸� �����Ͽ� �޸𸮿� �ֱ�(������ �Է�)
        Marshal.Copy(arr, 0, ptr, size);

        //����ü�� �ֱ�(�Է� ������ ���� �ؼ� ����ü�� �ֱ�)
        str = (stSendMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //�Ҵ��� �޸� ����
        Marshal.FreeHGlobal(ptr);

        //����ü ����
        return str;
    }
    /// <summary>
    /// �� ���� ���� �޽��� ����ü ������ �Լ�(����ü->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetSendMsgToByte(stSendMsg str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }


}
