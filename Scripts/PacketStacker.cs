//using Microsoft.SqlServer.Server;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using UnityEngine;
using System.Linq;



enum eCount
{
    dataCount = (int)eDataOrder.dataCount,


    FILECOUNT = 1024,
};
enum eDataOrder
{
    Number,
    DateTime,
    packetNumber,
    packet_nAddpos,
    packet_nPos,

    Ticks,
    Byte_Exsists,
    dataCount,

    packet_ArrayByte = 99,
}


public class PacketStacker : MonoSingleton<PacketStacker>
{
    public List<TrackedGamePacketInfo> savedPacketInfoList = new List<TrackedGamePacketInfo>();
    public TrackedGamePacketInfo gameInfoPacket = new TrackedGamePacketInfo();

    public Queue<TrackedGamePacketInfo> mainInfoPackets = new Queue<TrackedGamePacketInfo>();

    #region LocalChange For Specific Games
    // Queue<TrackedThirteenPokerLocalChangeInfo> thirteenPokerLocalChangeInfos = new Queue<TrackedThirteenPokerLocalChangeInfo>();
    #endregion

    #region Variables For Saving

    public string userID = "";
    [DisplayWithoutEdit] public EGAMELIST currentGameCode = EGAMELIST.UNSELECTED;
    public string logDateTime = "";

    public bool bRecordedWithFormatter = false;
    public string recordedFileNumber;

    public Coroutine recordCoroutine = null;

    public Coroutine recordLocalChangeCoroutine = null;

    public bool bRecordTick = false;

    public bool bLocalRecordTick = false;

    [HideInInspector]
    public int recordedIndex = 0;

    public string savedLogsFolderPath = "";

    Stream cachedStream;
    DeflateStream cachedDeflateStream;

    Stream cachedLocalStream;
    DeflateStream cachedLocalDeflateStream;

    public TrackedGamePacketInfo mainInfo = null;
    public TrackedGamePacketInfo gameChannelInfo = null;


    #endregion

    #region Functions For Saving



    public void SetUserID(string user)
    {
        userID = user;
    }
    public void SetGameCode(EGAMELIST currentCode)
    {
        currentGameCode = currentCode;
    }
    public void SetGameCode(string currentCode)
    {
        int tempInt = -1;
        if (int.TryParse(currentCode, out tempInt))
        {
            currentGameCode = (EGAMELIST)tempInt;
        }
    }

    public void SetDateTime(string dateTime)
    {
        logDateTime = dateTime;
    }

    public void SetFolderPath()
    {
        string folderPath = Application.dataPath + "/PacketTracker/";

#if UNITY_ANDROID
        folderPath = Application.persistentDataPath + "/PacketTracker/";
#endif

        if (savedLogsFolderPath.Equals(""))
        {
            savedLogsFolderPath = folderPath;
        }
    }

    public void AddMainInfoPacketToTrackerList(int packetNumber, ByteBuffer byteBuffer)
    {
        TrackedGamePacketInfo packetInfo = new TrackedGamePacketInfo();

        packetInfo.packetTime = System.DateTime.Now;

        ByteBuffer[] tempBufferArray = new ByteBuffer[1];
        tempBufferArray[0] = new ByteBuffer();
        tempBufferArray[0]._nAddPos = byteBuffer._nAddPos;
        tempBufferArray[0]._nPos = byteBuffer._nPos;

        tempBufferArray[0]._ArrayByte = byteBuffer.get();

        packetInfo.byteBuffers = new ByteBuffer[1];

        packetInfo.byteBuffers[0] = tempBufferArray[0];
        packetInfo.byteBuffers[0]._nPos = 0;

        packetInfo.packetNumber = packetNumber;

        Debug.Log(" saved user id in cGlobalInfos is " + cGlobalInfos.GetLoginID()
            + " saved user id in stacker is " + userID);
        if (cGlobalInfos.GetLoginID().Equals(userID) == false) // when logout and login with differenet user...
        {
            Debug.Log(" user id" + cGlobalInfos.GetLoginID()
                + " will save into stacker");

            // mainInfo = packetInfo;

            userID = cGlobalInfos.GetLoginID();
        }

        if (mainInfoPackets.Count == 0)
        {
            //if (cGlobalInfos.GetLoginID().Equals(userID) == false) // when logout and login with differenet user...
            //{
            //    Debug.Log(" user id" + cGlobalInfos.GetLoginID()
            //        + " will save into stacker");

            //    // mainInfo = packetInfo;

            //    userID = cGlobalInfos.GetLoginID();
            //}

            if (packetInfo.packetNumber != 12)
            {

                mainInfoPackets.Enqueue(mainInfo);
                
                if (mainInfo != null)
                {
                    // mainInfoPackets.Enqueue(mainInfo);
                }
            }
            else
            {
                mainInfo = packetInfo;
            }
        }
        //if(packetInfo.packetNumber != 131)
        //{

        //}
        //else
        //{

        //}

        // if(mainInfoPackets.Count)
        

        if(mainInfoPackets.Count != 0)
        {
            if (mainInfoPackets.Peek().packetNumber == 12
                && packetInfo.packetNumber == 12)
            {
                mainInfoPackets.Clear();
            }
        }
            mainInfoPackets.Enqueue(packetInfo);
        
    }

    public void AddPacketToTrackerList(int packetNumber, ByteBuffer byteBuffer)
    {
        TrackedGamePacketInfo packetInfo = new TrackedGamePacketInfo();

        packetInfo.packetTime = System.DateTime.Now;

        ByteBuffer[] tempBufferArray = new ByteBuffer[1];
        tempBufferArray[0] = new ByteBuffer();
        tempBufferArray[0]._nAddPos = byteBuffer._nAddPos;
        tempBufferArray[0]._nPos = byteBuffer._nPos;
        tempBufferArray[0]._ArrayByte = byteBuffer.get();

        packetInfo.byteBuffers = new ByteBuffer[1];

        packetInfo.byteBuffers[0] = tempBufferArray[0];
        packetInfo.byteBuffers[0]._nPos = 0;

        packetInfo.packetNumber = packetNumber;


        

        savedPacketInfoList.Add(packetInfo);

        
        bRecordTick = true;
        if(recordCoroutine == null)
        {
            recordCoroutine = StartCoroutine(SavePacketToFileWithCompression());
        }
        

    }
    //public void AddLocalChangeToList(int MyCardIndex, int ChangeCardIndex, Vector3 orgpos = new Vector3(), int orgDepth = 0, bool AutoBtn = false)
    //{
    //    var tempLocalChange = new TrackedThirteenPokerLocalChangeInfo(MyCardIndex, ChangeCardIndex, orgpos, orgDepth, AutoBtn);
    //    thirteenPokerLocalChangeInfos.Enqueue(tempLocalChange);

    //    bLocalRecordTick = true;
    //    if (recordLocalChangeCoroutine == null)
    //    {
    //        recordLocalChangeCoroutine = StartCoroutine(SaveLocalChangeToFile());
    //    }
        

    //}

    private IEnumerator SavePacketToFileWithCompression()
    {
        
        string folderPath = Application.dataPath + "/PacketTracker/";

#if UNITY_ANDROID || UNITY_EDITOR
        folderPath = Application.persistentDataPath + "/PacketTracker/";
#endif
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        if (savedLogsFolderPath.Equals(""))
        {
            savedLogsFolderPath = folderPath;
        }

        // path = Application.dataPath + "/CommonPrefabs/Scripts/PacketTracker/PacketsTrackedList" + recordedFileNumber + ".bin";
        string path;
        if (userID.Equals(""))
        {
            //path = folderPath + currentGameCode + "_" + logDateTime + "_" + "Log" + "_" + recordedFileNumber + ".bin";
            path = folderPath + currentGameCode + "_" + logDateTime + "_" + "Log" + "_" + ".bin";
        }
        else
        {
            // path = folderPath + currentGameCode + "_" + logDateTime + "_" + "Log" + "_" + recordedFileNumber + "_" + userID + ".bin";
            path = folderPath + currentGameCode + "_" + logDateTime + "_" + "Log" + "_" + userID + ".bin";
        }


        IFormatter formatter = new BinaryFormatter();

        if (!File.Exists(path))
        {
            Stream tempCreateStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            tempCreateStream.Close();
            // var compressor = new GZipStream(createStream, CompressionMode.Compress);

            //using(Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile())

            

        }
        
        if(File.Exists(path))
        {
            cachedStream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);
            cachedDeflateStream = new DeflateStream(cachedStream, System.IO.Compression.CompressionLevel.Optimal);
            using (Stream openStream = cachedStream)
            using (var compressor = cachedDeflateStream)
            {
                while (bRecordTick == true || bRecordedWithFormatter == true)
                {
                    if (bRecordTick)
                    {

                        while (mainInfoPackets.Count != 0)
                        {
                            formatter.Serialize(compressor, mainInfoPackets.Dequeue());
                        }
                        while (savedPacketInfoList.Count != 0)
                        {
                            formatter.Serialize(compressor, savedPacketInfoList[0]);
                            savedPacketInfoList.RemoveAt(0);
                        }

                        bRecordTick = false;
                    }

                    yield return null;
                }
            }


            // cachedDeflateStream.Close();
            // cachedStream.Close();

            if (cachedDeflateStream.CanWrite)
            {
                cachedDeflateStream.Close();
            }
            if (cachedStream.CanWrite)
            {
                cachedStream.Close();
            }

        }
        //openStream.Close();



    }

    private IEnumerator SaveLocalChangeToFile()
    {
        yield return null;
        string folderPath = Application.dataPath + "/CommonPrefabs/Scripts/PacketTracker/";

#if UNITY_ANDROID || UNITY_EDITOR
        folderPath = Application.persistentDataPath + "/CommonPrefabs/Scripts/PacketTracker/";
#endif
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        if (savedLogsFolderPath.Equals(""))
        {
            savedLogsFolderPath = folderPath;
        }

        string path;
        if (userID.Equals(""))
        {
            //path = folderPath + currentGameCode + "_" + logDateTime + "_" + "Log" + "_" + recordedFileNumber + ".bin";
            path = folderPath + currentGameCode + "_" + logDateTime + "_" + "LocalLog" + "_" + ".bin";
        }
        else
        {
            // path = folderPath + currentGameCode + "_" + logDateTime + "_" + "Log" + "_" + recordedFileNumber + "_" + userID + ".bin";
            path = folderPath + currentGameCode + "_" + logDateTime + "_" + "LocalLog" + "_" + userID + ".bin";
        }

        IFormatter formatter = new BinaryFormatter();

        if (!File.Exists(path))
        {
            Stream tempCreateStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            tempCreateStream.Close();

        }
        if (File.Exists(path))
        {
            //cachedLocalStream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);
            //cachedLocalDeflateStream = new DeflateStream(cachedLocalStream, System.IO.Compression.CompressionLevel.Optimal);
            //using (Stream openStream = cachedLocalStream)
            //using (var compressor = cachedLocalDeflateStream)
            //{
            //    while (bLocalRecordTick == true || bRecordedWithFormatter == true)
            //    {
            //        if (bLocalRecordTick)
            //        {
            //            while(thirteenPokerLocalChangeInfos.Count != 0)
            //            {
            //                formatter.Serialize(compressor, thirteenPokerLocalChangeInfos.Dequeue());
            //            }
                        

            //            bLocalRecordTick = false;
            //        }

            //        yield return null;
            //    }
            //}


            // cachedLocalDeflateStream.Close();
            // cachedLocalStream.Close();
            if (cachedLocalDeflateStream.CanWrite)
            {
                cachedLocalDeflateStream.Close();
            }
            if (cachedLocalStream.CanWrite)
            {
                cachedLocalStream.Close();
            }

        }


    }
    private void SaveCurrentPacketToFileWithCompression()
    {
        string folderPath = Application.dataPath + "/CommonPrefabs/Scripts/PacketTracker/";

#if UNITY_ANDROID || UNITY_EDITOR
        folderPath = Application.persistentDataPath + "/CommonPrefabs/Scripts/PacketTracker/";
#endif
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        if (savedLogsFolderPath.Equals(""))
        {
            savedLogsFolderPath = folderPath;
        }

        // path = Application.dataPath + "/CommonPrefabs/Scripts/PacketTracker/PacketsTrackedList" + recordedFileNumber + ".bin";
        string path;
        if (userID.Equals(""))
        {
            //path = folderPath + currentGameCode + "_" + logDateTime + "_" + "Log" + "_" + recordedFileNumber + ".bin";
            path = folderPath + currentGameCode + "_" + logDateTime + "_" + "Log" + "_" + ".bin";
        }
        else
        {
            // path = folderPath + currentGameCode + "_" + logDateTime + "_" + "Log" + "_" + recordedFileNumber + "_" + userID + ".bin";
            path = folderPath + currentGameCode + "_" + logDateTime + "_" + "Log" + "_" + userID + ".bin";
        }

        
        IFormatter formatter = new BinaryFormatter();

        if (!File.Exists(path))
        {
            Stream tempCreateStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            tempCreateStream.Close();
            // var compressor = new GZipStream(createStream, CompressionMode.Compress);

            //using(Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile())

            int tempAssert = 0;

            while (mainInfoPackets.Count != 0)
            {
                using (Stream createStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    //using (var compressor = new GZipStream(createStream, CompressionMode.Compress))
                    using (var compressor = new DeflateStream(createStream, System.IO.Compression.CompressionLevel.Optimal))
                    {
                        Debug.Log("cleared " + mainInfoPackets.Peek().packetNumber);
                        formatter.Serialize(compressor, mainInfoPackets.Dequeue());

                    }
                }
                tempAssert++;
                if(tempAssert > 64)
                {
                    break;
                }
            }

            using (Stream openStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None))
            {
                using (var compressor = new DeflateStream(openStream, System.IO.Compression.CompressionLevel.Optimal))
                {
                    formatter.Serialize(compressor, savedPacketInfoList[0]); // 최적화 이슈로 인해 변경
                    savedPacketInfoList.Clear(); // 최적화 이슈로 인해 변경

                    // openStream.Close();
                    recordedIndex++;
                }
            }

            //createStream.Close();

        }
        else
        {
            // Stream openStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None);

            // using (var compressor = new GZipStream(openStream, CompressionMode.Compress))

            //if (mainInfoPackets.Count != 0)
            //{

            //    using (Stream openStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None))
            //    {
            //        using (var compressor = new DeflateStream(openStream, System.IO.Compression.CompressionLevel.Optimal))
            //        {
            //            formatter.Serialize(compressor, mainInfoPackets.Dequeue());

            //        }
            //    }
            //}

            using (Stream openStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None))
            {
                using (var compressor = new DeflateStream(openStream, System.IO.Compression.CompressionLevel.Optimal))
                {
                    formatter.Serialize(compressor, savedPacketInfoList[0]); // 최적화 이슈로 인해 변경
                    savedPacketInfoList.Clear(); // 최적화 이슈로 인해 변경

                    // openStream.Close();
                    recordedIndex++;
                }
            }
        }
        //openStream.Close();



    }

    

    public void ClearPacketLogList()
    {
        savedPacketInfoList.Clear();
        gameInfoPacket = null;
        mainInfoPackets.Clear();
        currentGameCode = EGAMELIST.UNSELECTED;
        recordedIndex = 0;
        recordedFileNumber = "1";
        logDateTime = "";

        


        if (recordCoroutine != null)
        {
            var temp = cachedDeflateStream.CanRead;
            var temp2 = cachedStream.CanRead;
            StopCoroutine(recordCoroutine);
            if (cachedDeflateStream.CanWrite)
            {
                cachedDeflateStream.Close();
            }
            if (cachedStream.CanWrite)
            {
                cachedStream.Close();
            }

            
            
        }
        if (recordLocalChangeCoroutine != null)
        {
            StopCoroutine(recordLocalChangeCoroutine);
            if (cachedLocalDeflateStream.CanWrite)
            {
                cachedLocalDeflateStream.Close();
            }
            if (cachedLocalStream.CanWrite)
            {
                cachedLocalStream.Close();
            }


            
        }
        recordCoroutine = null;
        bRecordTick = false;
        recordLocalChangeCoroutine = null;
        bLocalRecordTick = false;
    }
    private void OnDestroy()
    {
        if(bRecordedWithFormatter)
        {
            ClearPacketLogList();
        }
    }

    private void OnApplicationQuit()
    {
        if (bRecordedWithFormatter)
        {
            // ClearPacketLogList();
            // cachedDeflateStream.Close();
            // cachedStream.Close();
            // cachedLocalDeflateStream.Close();
            // cachedLocalStream.Close();
            // ClearPacketLogList();
        }
    }

    static void Quit()
    {
        Debug.Log("Player prevented from quitting.");
        if(PacketStacker.IsExist)
            PacketStacker.Instance.ClearPacketLogList();
        // PacketStacker.Instance.cachedDeflateStream.Close();
        // PacketStacker.Instance.cachedStream.Close();
        // if(PacketStacker.Instance.cachedLocalDeflateStream != null)
        // {
        //     PacketStacker.Instance.cachedLocalDeflateStream.Close();
        // }
        // if (PacketStacker.Instance.cachedLocalStream != null)
        // {
        //     PacketStacker.Instance.cachedLocalStream.Close();
        // }

        
    }

    [RuntimeInitializeOnLoadMethod]
    static void RunOnStart()
    {
        Application.quitting += Quit;
    }

    #endregion

}
