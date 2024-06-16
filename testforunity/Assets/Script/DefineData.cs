using System.Runtime.InteropServices;
using System;
using UnityEngine;
using DefineInfo;

namespace DefineInfo
{
    /// <summary>
    /// 상수 클래스
    /// </summary>
    static public class Constants
    {
        public const int HEADER_SIZE = 36;//헤더 사이즈는 36(ID:ushort + Size:ushort + 이름 : 32)
        public const int MAX_NAME_BYTE = 32;//이름의 최대 바이트 수 : 한글10글자, 영어숫자32글자
        public const int MAX_SEND_MSG_BYTE = 100;//전송 메시지의 최대 바이트 수 : 한글33글자, 영어숫자10글자
    }
    }

public class DefineData
{
    /// <summary>
    /// 헤더 구조체 마샬링
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*들어오는순서대로(Queue)*/, Pack = 1/*데이터를 읽을 단위*/)]
    public struct stHeader
    {
        [MarshalAs(UnmanagedType.ByValTStr/*string*/, SizeConst = (int)(Constants.MAX_NAME_BYTE)/*클라이언트 이름의 최대 바이트*/)]
        public string sendClientName; // 클라이언트 이름
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 1/*ushort size*/)]
        public UInt16 MsgID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 1/*ushort size*/)]
        public UInt16 PacketSize; // 메시지 크기
    }
    /// <summary>
    /// 헤더 구조체 마샬링 함수(Byte->구조체)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stHeader HeaderfromByte(byte[] arr)
    {
        //구조체 초기화
        stHeader str = default(stHeader);
        int size = Marshal.SizeOf(str);//구조체 Size
        //Size만큼 메모리 할당(메모리 자리 빌리기)
        IntPtr ptr = Marshal.AllocHGlobal(size);
        //데이터를 복사하여 메모리에 넣기(데이터 입력)
        Marshal.Copy(arr, 0, ptr, size);
        //구조체에 넣기(입력 데이터 정리 해서 구조체에 넣기)
        str = (stHeader)Marshal.PtrToStructure(ptr, str.GetType());
        //할당한 메모리 해제
        Marshal.FreeHGlobal(ptr);
        //구조체 리턴S
        return str;
    }
    /// <summary>
    /// 헤더 구조체 마샬링 함수(구조체->Byte)
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
    /// 내 정보 변경 메시지 구조체 마샬링
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stChangeInfoMsg
    {
        [MarshalAs(UnmanagedType.ByValTStr/*string*/, SizeConst = (int)(Constants.MAX_NAME_BYTE)/*클라이언트 이름의 최대 바이트*/)]
        public string sendClientName; // 클라이언트 이름
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 1/*ushort size*/)]
        public UInt16 MsgID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 1/*ushort size*/)]
        public UInt16 PacketSize; // 나머지부분 메시지 크기
        [MarshalAs(UnmanagedType.ByValTStr/*string*/, SizeConst = (int)(Constants.MAX_NAME_BYTE)/*클라이언트 이름의 최대 바이트*/)]
        public string strClientName; // 클라이언트 이름
        [MarshalAs(UnmanagedType.ByValArray/*float array*/, SizeConst = 3)]
        public float[] position; // 클라이언트 위치 XYZ좌표
        [MarshalAs(UnmanagedType.ByValArray/*float array*/, SizeConst = 4)]
        public float[] Quaternion; // 클라이언트 쿼터니언 XYZW
    }
    /// <summary>
    /// 내 정보 변경 메시지 구조체 마샬링 함수(Byte->구조체)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stChangeInfoMsg ChangeInfoMsgfromByte(byte[] arr)
    {
        //구조체 초기화
        stChangeInfoMsg str = default(stChangeInfoMsg);
        int size = Marshal.SizeOf(str);//구조체 Size
        //Size만큼 메모리 할당(메모리 자리 빌리기)
        IntPtr ptr = Marshal.AllocHGlobal(size);
        //데이터를 복사하여 메모리에 넣기(데이터 입력)
        Marshal.Copy(arr, 0, ptr, size);
        //구조체에 넣기(입력 데이터 정리 해서 구조체에 넣기)
        str = (stChangeInfoMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //할당한 메모리 해제
        Marshal.FreeHGlobal(ptr);
        //구조체 리턴
        return str;
    }

    /// <summary>
    /// 내 정보 변경 메시지 구조체 마샬링 함수(구조체->Byte)
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
    /// 전송 메시지 구조체 마샬링
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stSendMsg
    {
        [MarshalAs(UnmanagedType.ByValTStr/*string*/, SizeConst = (int)(Constants.MAX_NAME_BYTE)/*클라이언트 이름의 최대 바이트*/)]
        public string sendClientName; // 클라이언트 이름
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 MsgID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // 메시지 크기

        [MarshalAs(UnmanagedType.ByValTStr/*string*/, SizeConst = (int)(Constants.MAX_SEND_MSG_BYTE)/*전송 메시지의 최대 바이트*/)]
        public string strSendMsg; // 전송 메시지

    }
    /// <summary>
    /// 내 정보 변경 메시지 구조체 마샬링 함수(Byte->구조체)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stSendMsg SendMsgfromByte(byte[] arr)
    {
        //구조체 초기화
        stSendMsg str = default(stSendMsg);
        int size = Marshal.SizeOf(str);//구조체 Size

        //Size만큼 메모리 할당(메모리 자리 빌리기)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //데이터를 복사하여 메모리에 넣기(데이터 입력)
        Marshal.Copy(arr, 0, ptr, size);

        //구조체에 넣기(입력 데이터 정리 해서 구조체에 넣기)
        str = (stSendMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //할당한 메모리 해제
        Marshal.FreeHGlobal(ptr);

        //구조체 리턴
        return str;
    }
    /// <summary>
    /// 내 정보 변경 메시지 구조체 마샬링 함수(구조체->Byte)
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
