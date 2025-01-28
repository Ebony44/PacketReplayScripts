using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
// using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.Text;
using System.Linq;
using System.IO;
using Prime31;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;

// using Wooriline.Framework;
// using Wooriline.Paikaeng;
using System.Net;
using UnityEditor;
using System.IO.Compression;
// using System.Security.Policy;
// using UnityEditor.PackageManager.Requests;

// using UnityEditor.SceneManagement;
#if UNITY_EDITOR

#endif




public class PacketTracker : MonoSingleton<PacketTracker>
{
    public List<TrackedGamePacketInfo> loadedPacketInfoList = new List<TrackedGamePacketInfo>();

    public TrackedGamePacketInfo gameInfoPacket = new TrackedGamePacketInfo();

    public Queue<TrackedGamePacketInfo> mainInfoPackets = new Queue<TrackedGamePacketInfo>();

    public List<TimeContainer> loadedPacketTimeList = new List<TimeContainer>();

    // public string filePath;

    #region Variables that aren't categorized
    // public string userID = "";

    public EGAMELIST currentGameCode = EGAMELIST.UNSELECTED;
    
    #endregion

    public string loadedFilePath;

    [DisplayWithoutEdit] public int currentReplayStep = 0;
    [DisplayWithoutEdit] public int totalReplayStep = 0;

    [HideInInspector] public int markedFastFowardStep = 0;
    [HideInInspector] public bool bMiddleOfSkipping = false;

    [DisplayWithoutEdit] public float fastFowardSpeed = 4f;
    [HideInInspector] public bool bTestFastFowardStarted = false;
    public Coroutine testFastFowardCoroutine = null;



    #region Variables For Loading

    [HideInInspector]
    public bool bIssuedFromTracker = false; // obsolete
    [HideInInspector]
    public bool bIssuedFromTrackerWithFormatter = false;
    [HideInInspector]
    public bool bMiddleOfLoadingFromTracker = false;
    [HideInInspector] 
    public string LoadedFileNumber;
    #endregion

    #region Variables For Sending 
    [HideInInspector] public string savedLogsFolderPath = "";

    [DisplayWithoutEdit] public string urlForFTPServer = "ftp://61.38.80.92:6523/TestFolder/";
    string ftpUser = "wooriftp";
    string ftpPassword = "dnflfkdls";
    [HideInInspector] public bool bFolderExistsOnServer = false;

    #endregion

    #region Variables For Setting And Replaying
    public UISprite wallScreenForFastFoward;
    #endregion

    #region Variables For Converting
    // [SerializeField] 
    private bool bIsConverting = false;
    #endregion

    #region Variables For Packet name
    Dictionary<int, string> packetNamesDic = new Dictionary<int, string>(capacity: 1024 * 8);
    #endregion

    #region flag for ugui version
    [DisplayWithoutEdit] public bool bIsUGUI = false;
    #endregion

    // ---------

    #region Awake For Display Replaying packet name
    private void Awake()
    {
        var tempInfoEnum = (int[])Enum.GetValues(typeof(PK));
        var tempCommonEnum = (int[])Enum.GetValues(typeof(COMMON_PK));
        var gameEnumType = typeof(POKDENG_PK);
        
        switch (currentGameCode)
        {
            case EGAMELIST.UNSELECTED:
                break;
            case EGAMELIST.GAME01_POKDENG:
                gameEnumType = typeof(POKDENG_PK);
                break;
            case EGAMELIST.GAME02_GAOGEO:
                gameEnumType = typeof(GAOGAE_PK);
                break;
            case EGAMELIST.GAME03_HOLDEM:
                gameEnumType = typeof(HOLDEM_PK);
                break;
            case EGAMELIST.GAME04_BIGTWO:
                gameEnumType = typeof(BIGTWO_PK);
                break;
            case EGAMELIST.GAME05_THIRTEENPOKER:
                gameEnumType = typeof(BIXA_PK);
                break;
            case EGAMELIST.GAME06_PAIKAENG:
                gameEnumType = typeof(PAIKAENG_PK);
                break;
            case EGAMELIST.GAME07_DOMINO:
                gameEnumType = typeof(DOMINO_PK);
                break;
            case EGAMELIST.GAME09_ANIMALDICE:
                gameEnumType = typeof(ANIMALDICE_PK);
                break;
            case EGAMELIST.GAME10_MIXEDTEN:
                gameEnumType = typeof(MIXTEN_PK);
                break;
            case EGAMELIST.COUNT_MAX:
                break;
            default:
                break;
        }

        var tempGameEnum = (int[])Enum.GetValues(gameEnumType);
        var tempGameEnumValues = Enum.GetValues(gameEnumType);

        Debug.Log("tempGameEnum length : " + tempGameEnum.Length);
        Debug.Log("tempGameEnumValues length : " + tempGameEnumValues.Length);

        // PK

        // INFO_PK
        for (int i = 0; i < tempInfoEnum.Length; i++)
        {
            var temp = (PK)tempInfoEnum[i];
            var tempIndex = tempInfoEnum[i];
            if (packetNamesDic.ContainsKey(tempIndex))
            {
                Debug.LogError("INFO PK same key exsists! that key number is " + tempIndex);
                continue;
            }
            packetNamesDic.Add(tempIndex, ((PK)tempIndex).ToString());
        }
        // COMMON PK
        for (int i = 0; i < tempCommonEnum.Length; i++)
        {
            var temp = (COMMON_PK)tempCommonEnum[i];
            var tempIndex = tempCommonEnum[i];
            if (packetNamesDic.ContainsKey(tempIndex))
            {
                Debug.LogError("COMMON PK same key exsists! that key number is " + tempIndex);
                continue;
            }
            packetNamesDic.Add(tempIndex, ((COMMON_PK)tempIndex).ToString());
        }
        // GAME PK
        for (int i = 0; i < tempGameEnum.Length; i++)
        {
            var tempIndex = tempGameEnum[i];
            if (packetNamesDic.ContainsKey(tempIndex))
            {
                Debug.LogError("GAME PK same key exsists! that key number is " + tempIndex);
                continue;
            }
            var temp = tempGameEnumValues.GetValue(i);
            packetNamesDic.Add(tempIndex, temp.ToString());
        }
    }
    #endregion

    #region Functions For Loading
    public void ConnectFromTracker()
    {
        SubGameSocket.m_iData = new int[2];
        SubGameSocket.m_bytebuffer = new ByteBuffer[1];
        SubGameSocket.m_bytebuffer[0] = new ByteBuffer();
        SubGameSocket.m_bfaileMsg = false;
        // SubGameSocket.m_TempBuffer = new ByteBuffer();
    }
    public bool HasNoBytePacket(ByteBuffer byteBuffer)
    {
        if(byteBuffer._nAddPos == 0 && byteBuffer._nPos == 0)
        {
            return true;
        }
        return false;
    }
    public void RemoveRoomInfoPacketFromList()
    {
        if (IsExistsAndEnableToIssue())
        {
            var item = loadedPacketInfoList.Find(x => (x.packetNumber % 1000) == 102);
            loadedPacketInfoList.Remove(item);
        }
        
    }


    
    public bool InitializeBeforeReplayStart()
    {
        Debug.Log("InitializeBeforeReplayStart");
        
        PacketTracker.Instance.LoadFromFileForPacketDataWithCompress();

        var myInfoPacket = loadedPacketInfoList.Find(x => (x.packetNumber == 12));
        var myInfoPackets = loadedPacketInfoList.FindAll(x => (x.packetNumber == 12));

        var gameChannelInfoPacket = loadedPacketInfoList.Find(x => (x.packetNumber == 131));
        // var intoRoomPacket = loadedPacketInfoList.Find(x => (x.packetNumber == 970102));

        var intoRoomPackets = loadedPacketInfoList.FindAll(x => (x.packetNumber == 970102));


        if (myInfoPacket != null)
        {
            Debug.Log("PacketTracker R_00_REF_MAINMYINFO01 received");

            var rec = new R_00_REF_MAINMYINFO01(myInfoPacket.byteBuffers);
            cGlobalInfos.SetMainMyInfo(rec);
            // loadedPacketInfoList.Remove(myInfoPacket);
            loadedPacketInfoList.RemoveAll(x => x.packetNumber == 12);
        }
        else if(myInfoPacket == null)
        {

        }
        if (gameChannelInfoPacket != null)
        {
            Debug.Log("PacketTracker R_00_RECON_GAMECHANNEL01 received");

            var rec = new R_00_RECON_GAMECHANNEL01(gameChannelInfoPacket.byteBuffers);
            cGlobalInfos.SetGameChannelInfo(rec);
            loadedPacketInfoList.Remove(gameChannelInfoPacket);

        }
        // if (intoRoomPacket != null)
        if (intoRoomPackets.Count != 0)
        {
            Debug.Log("PacketTracker R_97_INTOROOMOK received");

            // var rec = new R_97_INTOROOMOK(intoRoomPacket.byteBuffers);
            var rec = new R_97_INTOROOMOK(intoRoomPackets[0].byteBuffers);

            cGlobalInfos.SetIntoRoomInfo(rec);
            // loadedPacketInfoList.Remove(intoRoomPacket);
            loadedPacketInfoList.RemoveAll(x => (x.packetNumber == 970102));
        }
        if (myInfoPacket != null && gameChannelInfoPacket != null)
        {
            return true;
        }
        else
        {
            return false;
        }


    }

    public void RemovePacketsFromOtherGames(EGAMELIST gameCode)
    {
        var temp2 = "Game0" + ((int)gameCode).ToString();
        var otherGamePackets = loadedPacketInfoList.FindAll(x => (x.packetNumber / 10000) != (int)gameCode);
        List<TrackedGamePacketInfo> RemovedPackets = new List<TrackedGamePacketInfo>(); // = loadedPacketInfoList.FindAll(x => (x.packetNumber % 10) == 3);

        switch (gameCode)
        {
            case EGAMELIST.UNSELECTED:
                break;
            case EGAMELIST.GAME01_POKDENG:
                RemovedPackets = loadedPacketInfoList.FindAll(x => x.packetNumber == 10003);
                break;
            case EGAMELIST.GAME02_GAOGEO:
                RemovedPackets = loadedPacketInfoList.FindAll(x => x.packetNumber == 20003);
                break;
            case EGAMELIST.GAME03_HOLDEM:
                RemovedPackets = loadedPacketInfoList.FindAll(x => x.packetNumber == 30003);
                break;
            case EGAMELIST.GAME04_BIGTWO:
                RemovedPackets = loadedPacketInfoList.FindAll(x => x.packetNumber == 970003);
                break;
            //case EGAMELIST.GAME08_POKDENGPLUS:
            //    RemovedPackets = loadedPacketInfoList.FindAll(x => x.packetNumber == 970003);
            //    break;
            case EGAMELIST.GAME05_THIRTEENPOKER:
                RemovedPackets = loadedPacketInfoList.FindAll(x => x.packetNumber == 50003);
                break;
            case EGAMELIST.GAME06_PAIKAENG:
                RemovedPackets = loadedPacketInfoList.FindAll(x => x.packetNumber == 970003);
                break;
            case EGAMELIST.GAME07_DOMINO:
                RemovedPackets = loadedPacketInfoList.FindAll(x => x.packetNumber == 70003);
                break;
            case EGAMELIST.GAME10_MIXEDTEN:
                RemovedPackets = loadedPacketInfoList.FindAll(x => x.packetNumber == 970003);
                break;
            case EGAMELIST.COUNT_MAX:
                break;
        }


        var myInfoPacket = loadedPacketInfoList.Find(x => (x.packetNumber == 12));
        var gameChannelInfoPacket = loadedPacketInfoList.Find(x => (x.packetNumber == 131));

        foreach (var packet in otherGamePackets.FindAll(x => (x.packetNumber / 10000) == 97))
        {
            if (temp2.Equals("Game04") || temp2.Equals("Game05") || temp2.Equals("Game06") || temp2.Equals("Game08") || temp2.Equals("Game09") || temp2.Equals("Game10")
                || bIsUGUI == true
                )
            {
                // Debug.LogError("removing, packet number is " + packet.packetNumber);
                RemovedPackets = loadedPacketInfoList.FindAll(x => x.packetNumber == 970003);
                otherGamePackets.Remove(packet);

                
            }
            
            
            
        }
        otherGamePackets.RemoveAll(x => (x.packetNumber / 10000) == 97); // 990018
        otherGamePackets.RemoveAll(x => (x.packetNumber) == 990018); // 990018
        otherGamePackets.Remove(myInfoPacket);
        otherGamePackets.Remove(gameChannelInfoPacket);
        //if (temp2.Equals("Game09"))
        //{
        //    otherGamePackets.RemoveAll(x => x.packetNumber == 970102);
        //}
        otherGamePackets.RemoveAll(x => x.packetNumber == 970102);



        for (int i = 0; i < RemovedPackets.Count; ++i)
        {
            loadedPacketInfoList.Remove(RemovedPackets[i]);
        }
        for (int i = 0; i < otherGamePackets.Count; ++i)
        {
            loadedPacketInfoList.Remove(otherGamePackets[i]);
        }
        //if (EditorSceneManager.GetActiveScene().name.Equals(temp2))
        //{
        //}
    }

    //public void LoadFromFileForPacketDataWithFormatter()
    //{
    //    string path = Application.dataPath + "/CommonPrefabs/Scripts/PacketTracker/SavedPackets/PacketsTrackedList" + LoadedFileNumber + ".bin";
    //    // path = "D:/Workspace/Len89_2019_3_8/Assets" + "/CommonPrefabs/Scripts/PacketTracker/SavedPackets/PacketsTrackedList" + LoadedFileNumber + ".bin";

    //    // loadedFilePath = path;

    //    // loadedFilePath = loadedFilePath.Replace("Assets/", string.Empty);

    //    string tempPath = loadedFilePath.Replace("Assets/", string.Empty);



    //    path = Application.dataPath + "/" + tempPath;



    //    IFormatter formatter = new BinaryFormatter();
    //    // Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

    //    using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
    //    {
    //        while(stream.Position != stream.Length)
    //        {
    //            int assertindex2 = 0;
    //            using (var decompressor = new DeflateStream(stream, CompressionMode.Decompress, true))
    //            {
    //                int assertIndex = 0;

    //                loadedPacketInfoList.Add((TrackedGamePacketInfo)formatter.Deserialize(decompressor));

    //                //while ( true )
    //                //{
    //                //    assertIndex++;
    //                //    loadedPacketInfoList.Add((TrackedGamePacketInfo)formatter.Deserialize(decompressor));
    //                //    long length = decompressor.BaseStream.Length;
    //                //    long position = decompressor.BaseStream.Position;
    //                //    // long length2 = decompressor.Length;
    //                //    // long position2 = decompressor.Position;
    //                //    if (loadedPacketInfoList.Count != assertIndex )
    //                //    {
    //                //        break;
    //                //    }

    //                //    // LoadedPacketInfoList[assertIndex] = (TrackedGamePacketInfo)formatter.Deserialize(stream);


    //                //    if (assertIndex > (1024))  //1024)
    //                //    {
    //                //        return;
    //                //    }
    //                //}

    //                RemovePacketsFromOtherGames(currentGameCode);

    //                gameInfoPacket = loadedPacketInfoList.Find(x => ((x.packetNumber % 1000) == 102));
    //                Queue<TrackedGamePacketInfo> tempList = new Queue<TrackedGamePacketInfo>();

    //                foreach (var packet in loadedPacketInfoList)
    //                {
    //                    if (HasNoBytePacket(packet.byteBuffers[0]))
    //                    {
    //                        tempList.Enqueue(packet);
    //                    }
    //                }
    //                int tempInt = tempList.Count;
    //                for (int i = 0; i < tempInt; ++i)
    //                {
    //                    loadedPacketInfoList.Remove(tempList.Dequeue());
    //                }
    //            }
    //            assertindex2++;
    //            if(assertindex2 > 128)
    //            {
    //                break;
    //            }

    //        }
    //    }

    //    //int assertIndex = 0;
    //    //while (stream.Length != stream.Position)
    //    //{
    //    //    loadedPacketInfoList.Add((TrackedGamePacketInfo)formatter.Deserialize(stream));


    //    //    assertIndex++;
    //    //    if (assertIndex > (1024 * 16))  //1024)
    //    //    {
    //    //        return;
    //    //    }
    //    //}

    //    //RemovePacketsFromOtherGames(currentGameCode);

    //    //gameInfoPacket = loadedPacketInfoList.Find(x => ((x.packetNumber % 1000) == 102));
    //    //Queue<TrackedGamePacketInfo> tempList = new Queue<TrackedGamePacketInfo>();

    //    //foreach (var packet in loadedPacketInfoList)
    //    //{
    //    //    if (HasNoBytePacket(packet.byteBuffers[0]))
    //    //    {
    //    //        tempList.Enqueue(packet);
    //    //    }
    //    //}
    //    //int tempInt = tempList.Count;
    //    //for (int i = 0; i < tempInt; ++i)
    //    //{
    //    //    loadedPacketInfoList.Remove(tempList.Dequeue());
    //    //}


    //    // stream.Close();

    //}

    public void LoadFromFileForPacketDataWithCompress()
    {
        string path = Application.dataPath + "/CommonPrefabs/Scripts/PacketTracker/SavedPackets/PacketsTrackedList" + LoadedFileNumber + ".bin";
        // path = "D:/Workspace/Len89_2019_3_8/Assets" + "/CommonPrefabs/Scripts/PacketTracker/SavedPackets/PacketsTrackedList" + LoadedFileNumber + ".bin";

        // loadedFilePath = path;

        // loadedFilePath = loadedFilePath.Replace("Assets/", string.Empty);

        string tempPath = loadedFilePath.Replace("Assets/", string.Empty);

        path = Application.dataPath + "/" + tempPath;

        IFormatter formatter = new BinaryFormatter();
        // Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);


        // Jones: From file, gets information and adds up to the list
        using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            string tempStreamPath = Application.dataPath + "/CommonPrefabs/Scripts/PacketTracker/Downloaded";
            if (!Directory.Exists(tempStreamPath))
            {
                Directory.CreateDirectory(tempStreamPath);
            }
            Stream tempStream = new FileStream(tempStreamPath + "tempLog.bin", FileMode.Create, FileAccess.Write, FileShare.Read);
            tempStream.Close();
            tempStream = new FileStream(tempStreamPath + "tempLog.bin", FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            using (var decompressor = new DeflateStream(stream, CompressionMode.Decompress))
            {
                decompressor.CopyTo(tempStream);
            }
            tempStream.Position = 0;
            while (tempStream.Position != tempStream.Length)
            {
                try
                {
                    var tempInfo = (TrackedGamePacketInfo)formatter.Deserialize(tempStream);
                    // tempTestVariableInfo = tempInfo;
                    loadedPacketInfoList.Add(tempInfo);
                }
                catch (Exception e)
                {

                    //throw;
                }

                // loadedPacketInfoList.Add((TrackedGamePacketInfo)formatter.Deserialize(tempStream)); // Jones: 일부 역직렬화 오류로 인해 남아있는 로그라도 살리기 위해 위의 코드로 대체

            }
            tempStream.Close();
            File.Delete(tempStreamPath + "tempLog.bin");

        }
        if (!bIsConverting)
        {
            RemovePacketsFromOtherGames(currentGameCode);
        }
        gameInfoPacket = loadedPacketInfoList.Find(x => ((x.packetNumber % 1000) == 102));
        Queue<TrackedGamePacketInfo> tempList = new Queue<TrackedGamePacketInfo>();
        foreach (var packet in loadedPacketInfoList)
        {
            if (HasNoBytePacket(packet.byteBuffers[0]))
            {
                tempList.Enqueue(packet);
            }
        }
        int tempInt = tempList.Count;
        for (int i = 0; i < tempInt; ++i)
        {
            loadedPacketInfoList.Remove(tempList.Dequeue());
        }


    }

    public void SetPacketInfoTimeList()
    {
        for(int i = 0; i< loadedPacketInfoList.Count - 1;++i)
        {
            loadedPacketTimeList.Add(TrackThePacketsTime(i + 1, true));
        }

        // 0 -> 1, 0
        // 1 -> 2, 1
    }

    #endregion

    #region Functions For Setting And Replaying

    public IEnumerator IssuePackets(EGAMELIST gameCode) // TODO
    {
        yield return null;
        yield return new WaitForSeconds(0.2f);
        int i = 0;

        Debug.Log("Issue Packets");

        Debug.Log("packet tracker load from file activated, packet count: " + loadedPacketInfoList.Count);
        ConnectFromTracker();
        SetPacketInfoTimeList();
        totalReplayStep = loadedPacketInfoList.Count;
        // SkipToNextGame(4);
        while (i < loadedPacketInfoList.Count)
        {
            
            var tempString = packetNamesDic[loadedPacketInfoList[i].packetNumber];
            Debug.Log("step : " + i + " packet number:  " + loadedPacketInfoList[i].packetNumber + " time will be taken: " + TrackThePacketsTimeFloat(i)
                + " name of packet is " + tempString);
            SubGameSocket.m_iData[0] = loadedPacketInfoList[i].packetNumber;
            SubGameSocket.m_bytebuffer = loadedPacketInfoList[i].byteBuffers;
            var tempData = SubGameSocket.m_iData[0];
            var tempBuffer = SubGameSocket.m_bytebuffer;
            var packetNum = loadedPacketInfoList[i].packetNumber;
            
            PacketHandler tempPacketHandler;

            if (PacketManager.Instance.packetListDic.TryGetValue(packetNum, out tempPacketHandler))
                tempPacketHandler.Func();
            else
                throw new Exception("해당하는 PK 메서드가 없습니다.\npkNumber : " + packetNum.ToString());

            if (i + 1 >= loadedPacketInfoList.Count)
            {
                Debug.LogError("end of packet, Replay End");
                yield break;
            }

            var stepTimeContained = loadedPacketTimeList[i];
            currentReplayStep = i;
            DisplayWallScreen(currentReplayStep, markedFastFowardStep);
            while (stepTimeContained.t < 1f)
            {
                yield return null;
                stepTimeContained.t += Time.deltaTime / stepTimeContained.time;

            }
            
            i++;
            if (i > (4096 * 4))
            {
                yield break;
            }
        }
        Debug.LogError("Replay End");

    }

    private void DisplayWallScreen(int currentStep, int fastFowardStep)
    {
        if(currentStep < fastFowardStep)
        {
            wallScreenForFastFoward.gameObject.SetActive(true);
            bMiddleOfSkipping = true;

        }
        else
        {
            wallScreenForFastFoward.gameObject.SetActive(false);
            bMiddleOfSkipping = false;
        }
    }


    // [TestMethod]
    public List<int> FindDividePackets()
    {
        List<int> dividePacketIndexList = new List<int>();

        int dividePacketNumber = 0;

        switch (currentGameCode)
        {
            case EGAMELIST.UNSELECTED:
                break;
            case EGAMELIST.GAME01_POKDENG:
                dividePacketNumber = (int)POKDENG_PK.R_01_DIVIDE;
                break;
            case EGAMELIST.GAME02_GAOGEO:
                dividePacketNumber = (int)GAOGAE_PK.R_02_DIVIDE;
                break;
            case EGAMELIST.GAME03_HOLDEM:
                dividePacketNumber = (int)HOLDEM_PK.R_03_DIVIDE;
                break;
            case EGAMELIST.GAME04_BIGTWO:
                dividePacketNumber = (int)BIGTWO_PK.R_04_DIVIDE;
                break;
            case EGAMELIST.GAME05_THIRTEENPOKER:
                dividePacketNumber = (int)BIXA_PK.R_05_DIVIDE;
                break;
            case EGAMELIST.GAME06_PAIKAENG:
                dividePacketNumber = (int)PAIKAENG_PK.R_06_DIVIDE;
                break;
            case EGAMELIST.GAME07_DOMINO:
                dividePacketNumber = (int)DOMINO_PK.R_07_DIVIDE;
                break;
            case EGAMELIST.GAME10_MIXEDTEN:
                dividePacketNumber = (int)MIXTEN_PK.R_10_DIVIDE;
                break;
            case EGAMELIST.COUNT_MAX:
                break;
            default:
                break;
        }

        var dividePacketList = loadedPacketInfoList.FindAll(x => (x.packetNumber) == dividePacketNumber);
        Debug.Log(" played game count is " + dividePacketList.Count);
        foreach (var packet in dividePacketList)
        {
            dividePacketIndexList.Add(loadedPacketInfoList.IndexOf(packet));
        }

        var tempIndex = 0;
        foreach (var packetIndex in dividePacketIndexList)
        {
            Debug.Log("divide packet index " + tempIndex + " 's " + "indexnumber " + packetIndex);
            ++tempIndex;
        }
        return dividePacketIndexList;
    }

    [TestMethod]
    public void SkipToNextGames(int gameStepCount, int playSpeed)
    {
        Debug.Log("SkipToNextGames gameStepCount : " + gameStepCount + " playSpeed : " + playSpeed);

        if(playSpeed > 0)
        {
            fastFowardSpeed = playSpeed;
        }
        var dividePacketIndexList = FindDividePackets();
        if (dividePacketIndexList.Count == 0)
        {
            Debug.LogError("replay file has only 1 game");
            return;
        }
        if (gameStepCount >= dividePacketIndexList.Count)
        {
            Debug.LogError("total games of divide packet included are " + (dividePacketIndexList.Count - 1) + ", your request " + gameStepCount + " is out of index");
            return;
        }

        int stepCount = dividePacketIndexList[gameStepCount];

        Debug.Log("stepCount : " + stepCount + " loadedPacketTimeList.Count : " + loadedPacketTimeList.Count);

        for (int i = 0; i < stepCount; ++i)
        {
            loadedPacketTimeList[i].t = 0.9f;
        }
        markedFastFowardStep = stepCount;
        bMiddleOfSkipping = true;

        Debug.Log("step skipped " + stepCount + " times");
    }


    public void SkipToNextGamesWithTimeScale(int gameDivideStepCount)
    {
        if (bMiddleOfSkipping)
        {
            return;
        }

        Debug.Log("SkipToNextGamesWithTimeScale gameDivideStepCount : " + gameDivideStepCount);

        var dividePacketIndexList = FindDividePackets();
        if (dividePacketIndexList.Count == 0)
        {
            Debug.LogError("replay file has only 1 game");
            return;
        }
        if (gameDivideStepCount >= dividePacketIndexList.Count)
        {
            Debug.LogError("total games of divide packet included are " + (dividePacketIndexList.Count - 1) + ", your request " + gameDivideStepCount + " is out of index");
            return;
        }
        int stepCount = dividePacketIndexList[gameDivideStepCount];
        
        bMiddleOfSkipping = true;

        Debug.Log("step skipped " + stepCount + " times");
    }

    public void FastFowardWithTimeScale()
    {
        Time.timeScale = fastFowardSpeed;
    }
    public void ResetTimeScale()
    {
        Time.timeScale = 1f;
    }
    [TestMethod]
    private void StartFastFoward(float playSpeed)
    {
        if(playSpeed > 0)
        {
            fastFowardSpeed = playSpeed;
        }
        if (testFastFowardCoroutine != null || bTestFastFowardStarted)
        {
            StopFastFoward();
        }
        bTestFastFowardStarted = true;
        testFastFowardCoroutine = StartCoroutine(FastFowardRoutine());
    }
    [TestMethod]
    private void StopFastFoward()
    {
        bTestFastFowardStarted = false;
        StopCoroutine(testFastFowardCoroutine);
        ResetTimeScale();
    }
    private IEnumerator FastFowardRoutine()
    {
        while (bTestFastFowardStarted)
        {
            FastFowardWithTimeScale();
            yield return null;
        }
        ResetTimeScale();

    }


    public TimeContainer TrackThePacketsTime(int currentStep, bool bTimeContained)
    {
        DateTime startTime;
        DateTime storedTime;
        TimeSpan dueTime;
        if (currentStep == 0)
        {
            startTime = loadedPacketInfoList[0].packetTime;
            dueTime = startTime - startTime;
            var floatTime = Mathf.Abs((float)dueTime.TotalSeconds);
            TimeContainer t = new TimeContainer("ReplayStepTime", floatTime);
            return t;
        }
        else
        {

            startTime = loadedPacketInfoList[currentStep - 1].packetTime;
            storedTime = loadedPacketInfoList[currentStep].packetTime;
            // TimeSpan dueTime = startTime - trackedPacketInfoList[0].packetTimes[0];
            dueTime = startTime - storedTime;
            //UnityEngine.Debug.Log("due time is " + Mathf.Abs((float)dueTime.TotalSeconds) +
            //    "\n  start time is " + startTime + " stored time is " + storedTime);
            var floatTime = Mathf.Abs((float)dueTime.TotalSeconds);
            TimeContainer t = new TimeContainer("ReplayStepTime", floatTime);
            return t;
        }

    }

    public float TrackThePacketsTimeFloat(int currentStep)
    {
        var tempDueTime = TrackThePacketsTime(currentStep);
        var dueTime = Mathf.Abs((float)tempDueTime.TotalSeconds);
        return dueTime;
    }
    public TimeSpan TrackThePacketsTime(int currentStep)
    {
        // Math.Truncate( (DateTime.UtcNow.Subtract  ) )

        //DateTime startTime = System.DateTime.Now;

        DateTime startTime;
        DateTime storedTime;
        TimeSpan dueTime;

        if (currentStep == 0)
        {
            startTime = loadedPacketInfoList[0].packetTime;
            dueTime = startTime - startTime;
            return dueTime;
        }
        else
        {

            startTime = loadedPacketInfoList[currentStep - 1].packetTime;
            storedTime = loadedPacketInfoList[currentStep].packetTime;
            // TimeSpan dueTime = startTime - trackedPacketInfoList[0].packetTimes[0];
            dueTime = startTime - storedTime;
            UnityEngine.Debug.Log("due time is " + Mathf.Abs((float)dueTime.TotalSeconds) +
                "\n  start time is " + startTime + " stored time is " + storedTime);

            return dueTime;
        }

    }

    #endregion












    //public void SaveCurrentPacketToFile() // TODO: Finish this method for single saving purpose
    //{
    //    StreamWriter packetLog;
    //    // string dataPath = Application.dataPath + "/CommonPrefabs/Scripts/PacketTracker/PacketsTrackedList" + recordedFileNumber + ".txt";
    //    string dataPath = Application.persistentDataPath + "/CommonPrefabs/Scripts/PacketTracker/PacketsTrackedList" + recordedFileNumber + ".txt";

    //    if (!File.Exists(dataPath))
    //    {
    //        packetLog = new StreamWriter(dataPath);
    //    }
    //    else
    //    {
    //        packetLog = File.AppendText(dataPath);
    //    }
    //    if (recordedIndex == 0)
    //    {
    //        packetLog.WriteLine("No. , DateTime, packetNumber, packet_nAddpos, packet_nPos ,packet_ArrayByte, Ticks");
    //    }


    //    recordedIndex++;
    //    packetLog.Close();



    //    using (System.IO.StreamWriter file =
    //        new System.IO.StreamWriter(dataPath))
    //    {
    //        if (recordedIndex == 0)
    //        {

    //        }
    //    }

    //    string[] dataLines = new string[trackedPacketInfoList.Count];
    //    string[] byteLines = new string[trackedPacketInfoList.Count];




    //}


    /// <summary>
    /// 커스텀 에디터 - 리플레이 파일 및 씬 선택 - 씬 세팅 후 사용.
    /// </summary>
    [TestMethod]
    public void ConvertSavedLogToText()
    {
        Debug.Log("converting");
        // LoadFromFileForPacketDataWithFormatter();
        bIsConverting = true;
        PacketTracker.Instance.LoadFromFileForPacketDataWithCompress();


        // string path = Application.dataPath + "/CommonPrefabs/Scripts/PacketTracker/LoadedPackets/PacketsTrackedList" + LoadedFileNumber + ".txt";
        string path = Application.dataPath + "/Submodules/WooriUtil/PacketTracker/PacketsTrackedList" + LoadedFileNumber + ".txt";
        // string path = loadedFilePath;



        string[] dataLines = new string[loadedPacketInfoList.Count];
        string[] byteLines = new string[loadedPacketInfoList.Count];
        byte[] arrayByte = new byte[1];

        for (int i = 0; i < loadedPacketInfoList.Count; ++i)
        {
            string data = "";
            data += (i + 1) + ", ";
            data += loadedPacketInfoList[i].packetTime.ToString() + " and " + loadedPacketInfoList[i].packetTime.Millisecond + "ms, ";
            data += loadedPacketInfoList[i].packetNumber.ToString() + ", ";
            data += loadedPacketInfoList[i].byteBuffers[0]._nAddPos.ToString() + ", ";
            data += loadedPacketInfoList[i].byteBuffers[0]._nPos.ToString() + ", ";
            // data += trackedPacketInfoList[i].byteBuffers[0]._ArrayByte.ToString() + ", ";
            // data += Encoding.ASCII.GetString(trackedPacketInfoList[i].byteBuffers[0]._ArrayByte) + ", ";
            data += loadedPacketInfoList[i].packetTime.Ticks.ToString() + ", ";

            if (loadedPacketInfoList[i].byteBuffers[0] != null)
            {

                if (loadedPacketInfoList[i].byteBuffers[0]._ArrayByte != null)
                {

                    byteLines[i] = Encoding.ASCII.GetString(loadedPacketInfoList[i].byteBuffers[0]._ArrayByte) + "/// ";

                    arrayByte = loadedPacketInfoList[i].byteBuffers[0]._ArrayByte;

                    // byteLines[i] = Encoding.UTF8.GetString(trackedPacketInfoList[i].byteBuffers[0]._ArrayByte) + "/// ";
                }
            }
            else if (loadedPacketInfoList[i].byteBuffers[0] == null)
            {
                Debug.LogError("bytebuffer error");
                Debug.LogError("byte error");
                // data += "NULL";
            }
            dataLines[i] = data;
        }

        using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(path))
        {
            file.WriteLine("No. , DateTime, packetNumber, packet_nAddpos, packet_nPos ,packet_ArrayByte, Ticks");
            for (int i = 0; i < loadedPacketInfoList.Count; ++i)
            {
                
                file.WriteLine(dataLines[i]);
            }
            file.WriteLine("");
            file.WriteLine("End Of DateLines");
            file.WriteLine("");


        }
        loadedPacketInfoList.Clear();
        bIsConverting = false;
    }


    #region Jones: 실시간 저장 테스트

    
    #endregion


    #region obsolete codes
    
    
    #endregion


    #region Jones: 바이너리 내에서 바이트 찾기, 특정 스트링 찾기 등 기능성 메서드
    public static long FindPosition(Stream stream, byte[] byteSequence)
    {
        if (byteSequence.Length > stream.Length)
            return -1;

        byte[] buffer = new byte[byteSequence.Length];

        using (BufferedStream bufStream = new BufferedStream(stream, byteSequence.Length))
        {
            int i;
            while ((i = bufStream.Read(buffer, 0, byteSequence.Length)) == byteSequence.Length)
            {
                if (byteSequence.SequenceEqual(buffer))
                    return bufStream.Position - byteSequence.Length;
                else
                    bufStream.Position -= byteSequence.Length - PadLeftSequence(buffer, byteSequence);
            }
        }

        return -1;
    }

    private static int PadLeftSequence(byte[] bytes, byte[] seqBytes)
    {
        int i = 1;
        while (i < bytes.Length)
        {
            int n = bytes.Length - i;
            byte[] aux1 = new byte[n];
            byte[] aux2 = new byte[n];
            Array.Copy(bytes, i, aux1, 0, n);
            Array.Copy(seqBytes, aux2, n);
            if (aux1.SequenceEqual(aux2))
                return i;
            i++;
        }
        return i;
    }

    
    public void TestDisplayStringPosition()
    {
        // string path = Application.dataPath + "/CommonPrefabs/Scripts/PacketTracker/PacketsTrackedList" + LoadedFileNumber + ".txt";
        // Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        // var tempLength = stream.Length;
        // var tempPosition = stream.Position;
        // var temp = GetPosition(stream, "End Of DateLines");
    }

    public int GetPosition(Stream stream, string findStandardString)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        System.Text.StringBuilder returnTextBuilder = new System.Text.StringBuilder();
        string returnText = string.Empty;
        int size = System.Convert.ToInt32(findStandardString.Length / (double)2) - 1;
        byte[] buffer = new byte[size + 1];
        int currentRead = -1;
        int totalRead = 0;

        while (currentRead != 0)
        {
            string collected = null;
            string chars = null;
            int foundIndex = -1;

            currentRead = stream.Read(buffer, 0, buffer.Length);
            totalRead += currentRead;
            chars = System.Text.Encoding.Default.GetString(buffer, 0, currentRead);

            builder.Append(chars);
            returnTextBuilder.Append(chars);

            collected = builder.ToString();
            foundIndex = collected.IndexOf(findStandardString);

            if ((foundIndex >= 0))
            {
                returnText = returnTextBuilder.ToString();

                int indexOfSep = returnText.IndexOf(findStandardString);
                int cutLength = returnText.Length - indexOfSep;

                returnText = returnText.Remove(indexOfSep, cutLength);

                builder.Remove(0, foundIndex + findStandardString.Length);

                if ((cutLength > findStandardString.Length))
                    stream.Position = stream.Position - (cutLength - findStandardString.Length);

                //return returnText;
                return totalRead;
            }
            else if ((!collected.Contains(findStandardString.First())))
                builder.Length = 0;
        }

        //return string.Empty;
        return -1;
    }

    public static string ReadUntil(System.IO.FileStream Stream, string UntilText)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        System.Text.StringBuilder returnTextBuilder = new System.Text.StringBuilder();
        string returnText = string.Empty;
        int size = System.Convert.ToInt32(UntilText.Length / (double)2) - 1;
        byte[] buffer = new byte[size + 1];
        int currentRead = -1;

        while (currentRead != 0)
        {
            string collected = null;
            string chars = null;
            int foundIndex = -1;

            currentRead = Stream.Read(buffer, 0, buffer.Length);
            chars = System.Text.Encoding.Default.GetString(buffer, 0, currentRead);

            builder.Append(chars);
            returnTextBuilder.Append(chars);

            collected = builder.ToString();
            foundIndex = collected.IndexOf(UntilText);

            if ((foundIndex >= 0))
            {
                returnText = returnTextBuilder.ToString();

                int indexOfSep = returnText.IndexOf(UntilText);
                int cutLength = returnText.Length - indexOfSep;

                returnText = returnText.Remove(indexOfSep, cutLength);

                builder.Remove(0, foundIndex + UntilText.Length);

                if ((cutLength > UntilText.Length))
                    Stream.Position = Stream.Position - (cutLength - UntilText.Length);

                return returnText;
            }
            else if ((!collected.Contains(UntilText.First())))
                builder.Length = 0;
        }

        return string.Empty;
    }

    //[TestMethod]
    //public void TestDisplayUserID()
    //{
    //    PacketStacker.Instance.SetUserID(cGlobalInfos.GetLoginID());
    //    // Debug.Log(userID);
    //}
    #endregion

    public static bool IsExistsAndEnableToIssue()
    {
        var tempBool = PacketTracker.CheckExist();
        var tempBool2 = PacketTracker.IsExist;
        if (PacketTracker.CheckExist())
        {
            if(tempBool2 == false)
            {
                return false;
            }
            if (PacketTracker.Instance.bIssuedFromTrackerWithFormatter || PacketTracker.Instance.bMiddleOfLoadingFromTracker)
            {
                return true;
            }
        }
        

        return false;
    }
    public static bool IsExistsAndEnableToIssue(bool bOnlyFirstTimeInitiated)
    {
        var tempBool = PacketTracker.CheckExist();
        var tempBool2 = PacketTracker.IsExist;
        Debug.LogError("is exists? " + tempBool + " " + tempBool2);
        // var tempBool2 = (PacketTracker.Instance.bIssuedFromTrackerWithFormatter && !PacketTracker.Instance.bMiddleOfLoadingFromTracker);
        if (tempBool2)
        {
            if (PacketTracker.Instance.bIssuedFromTrackerWithFormatter && !PacketTracker.Instance.bMiddleOfLoadingFromTracker)
            {
                return true;
            }
        }


        return false;
    }



}
