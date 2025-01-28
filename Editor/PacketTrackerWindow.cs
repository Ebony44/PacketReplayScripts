#if UNITY_EDITOR
using DG.Tweening.Plugins.Core.PathCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


public class PacketTrackerWindow : EditorWindow
{
    public UnityEngine.Object packetFile;
    public GameObject packetPrefab;
    public PacketLogInfoItem packetLogInfo;
    SerializedProperty m_packetPrefabProp;

    SerializedObject m_setting;
    SerializedProperty m_packetFileNumber;

    public EGAMELIST eGAMELIST = EGAMELIST.UNSELECTED;

    public string objectPath;
    public bool bFileAndSelectedMatched = false;
    private bool bGameSelected = false;
    private bool bSelectionChanged = false;

    public string previousSearchWords;

    private object objectFieldDelimiter;

    #region conversion temp variables
    public bool bIsUGUIVersion = false;
    #endregion


    [MenuItem("Window/PacketTracker")]
    public static void Open()
    {
        PacketTrackerWindow window = EditorWindow.CreateInstance<PacketTrackerWindow>();

        window.Show();
        
    }

    private void OnEnable()
    {
        //packetPrefab = new SerializedObject(PacketTracker.Instance);

        GetPacketPrefab();
        m_setting = new SerializedObject(packetLogInfo);
        m_packetPrefabProp = m_setting.FindProperty("packetLogPrefab");
        
        //m_packetPrefabProp = m_setting.FindProperty("packetLogPrefab");
        

    }
    private void GetPacketPrefab()
    {
        packetPrefab = AssetDatabase.FindAssets("PacketLogInfoItem")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Select(path => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path))
            .First() as GameObject;

        string[] guids1 = AssetDatabase.FindAssets("PacketLogInfoItem");

        if (packetPrefab != null)
        {
            Debug.Log(packetPrefab);
        }
        else
        {
            // Debug.Log("find nothing");
            foreach(var guid1 in guids1)
            {
                Debug.Log(AssetDatabase.GUIDToAssetPath(guid1));
                string temp = AssetDatabase.GUIDToAssetPath(guid1);
                if (temp.Contains(".asset"))
                {
                    packetLogInfo = AssetDatabase.LoadAssetAtPath<PacketLogInfoItem>(temp);
                    packetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(temp);
                    Debug.Log("load succeed");
                }
            }
        }
    }

    private void OnGUI()
    {

        // Debug.Log(Event.current.commandName);

        // GUILayout.Space(20f);

        // DisplayDownloadFromServerButton();

        GUILayout.Space(20f);

        DisplayPacketPrefab();

        GUILayout.Space(20f);

        DisplayDropDownGameList();

        GUILayout.Space(20f);

        

    }

    //private void DisplayDownloadFromServerButton()
    //{
    //    if(GUILayout.Button("Download replay files from FTP server"))
    //    {
    //        PacketTracker.Instance.DownloadLogFilesFromFTPServer();
    //    }
        
    //}

    private void DisplayPacketPrefab()
    {
        
        // packetPrefab = EditorGUILayout.ObjectField(packetPrefab, typeof(GameObject), true) as GameObject;
        // var packetLogPrefab = m_setting.FindProperty()

        m_packetPrefabProp.objectReferenceValue =
            EditorGUILayout.ObjectField(m_packetPrefabProp.objectReferenceValue, typeof(PacketLogInfoItem), false);

        // Debug.Log(m_packetPrefabProp.objectReferenceValue.name);

        m_setting.Update();
        m_setting.ApplyModifiedProperties();
    }

    private void DisplayDropDownGameList()
    {
        // Debug.Log(Event.current.commandName);
        GUILayout.Label("Game list", EditorStyles.boldLabel);

        eGAMELIST = (EGAMELIST)EditorGUILayout.EnumPopup(eGAMELIST);


        if (eGAMELIST == EGAMELIST.UNSELECTED || eGAMELIST == EGAMELIST.COUNT_MAX)
        {
            bGameSelected = false;
        }
        #region

        GUILayout.Space(20f);

        DisplayPickerAgain();
        #endregion
        if (eGAMELIST != EGAMELIST.UNSELECTED && eGAMELIST != EGAMELIST.COUNT_MAX)
        {
            GUILayout.Space(20f);

            DisplayTextFileSelection();
            
        }

        #region
        GUILayout.Space(20f);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("UGUI version?", GUILayout.Width(90));
        bool bTemp = EditorGUILayout.Toggle(bIsUGUIVersion, GUILayout.Width(50));
        bIsUGUIVersion = bTemp;
        EditorGUILayout.EndHorizontal();
        #endregion



    }

    private void DisplayObjectPickerForPacketFile()
    {
        if(eGAMELIST == EGAMELIST.UNSELECTED || eGAMELIST == EGAMELIST.COUNT_MAX)
        {
            return;
        }
        
        if (!bGameSelected)
        {
            Debug.Log("displaying picker from selection");
            int controlID = EditorGUIUtility.GetControlID(FocusType.Passive);
            EditorGUIUtility.ShowObjectPicker<UnityEngine.Object>(packetFile, false, "Game" + ((int)eGAMELIST).ToString("D2"), controlID);
            previousSearchWords = "Game" + ((int)eGAMELIST).ToString("D2");

            bGameSelected = true;
            
            
            
            
        }

        if (bSelectionChanged && bGameSelected)
        {
            Debug.Log("displaying picker from change");
            int controlID = EditorGUIUtility.GetControlID(FocusType.Passive);
            EditorGUIUtility.ShowObjectPicker<UnityEngine.Object>(packetFile, false, "Game" + ((int)eGAMELIST).ToString("D2"), controlID);
            previousSearchWords = "Game" + ((int)eGAMELIST).ToString("D2");

            bSelectionChanged = false;
            
        }


        if (Event.current.commandName.Equals("ObjectSelectorUpdated") && bGameSelected)
        {
            Debug.Log("file selection updated");
            packetFile = EditorGUIUtility.GetObjectPickerObject();
        }
        else if (Event.current.commandName.Equals("ObjectSelectorClosed") && bGameSelected)
        {
            Debug.Log("file selection closed");
            packetFile = EditorGUIUtility.GetObjectPickerObject();
        }

        


    }
    private void DisplayTextFileSelection()
    {
        GUILayout.Label("File For Replaying", EditorStyles.boldLabel);

        // packetFile = EditorGUILayout.ObjectField(packetFile,)

        packetFile = EditorGUILayout.ObjectField(packetFile, typeof(UnityEngine.Object), false);
        
        CheckSelectionChanged();
        DisplayObjectPickerForPacketFile();
        




        if (packetFile != null)
        {
            objectPath = AssetDatabase.GetAssetPath(packetFile);

            // Debug.Log(objectPath);

            
            if (!packetFile.name.Contains(eGAMELIST.ToString()))
            {
                EditorGUILayout.HelpBox("Your replay file and game are not matched", MessageType.Warning);
                bFileAndSelectedMatched = false;
            }
            else
            {
                
                bFileAndSelectedMatched = true;
            }

            //if (GUILayout.Button("instantiate replayer prefab"))
            //{
            //    Instantiate(m_packetPrefabProp.objectReferenceValue);
            //}

            DisplaySettingUpScene();

            

        }

    }

    private void CheckSelectionChanged()
    {
        if (packetFile != null)
        {
            if (!packetFile.name.Contains(eGAMELIST.ToString()))
            {
                
                bFileAndSelectedMatched = false;
            }
            else
            {

                bFileAndSelectedMatched = true;
            }
        }
        
        if (!bFileAndSelectedMatched && Event.current.commandName.Equals("PopupMenuChanged") && eGAMELIST != EGAMELIST.UNSELECTED)
        {
            bSelectionChanged = true;
        }
    }

    private void DisplaySettingUpScene()
    {

        if (bFileAndSelectedMatched)
        {
            bool bPrefabExsist = GameObject.Find(m_packetPrefabProp.objectReferenceValue.name) != null || 
                GameObject.Find(m_packetPrefabProp.objectReferenceValue.name + "(Clone)") != null;
            string scenePath = "";
            if (GUILayout.Button("Setting Scene Up"))
            {
                string[] scenePaths = AssetDatabase.FindAssets("Game" + ((int)eGAMELIST).ToString("D2"));
                foreach (var path in scenePaths)
                {
                    // Debug.Log(AssetDatabase.GUIDToAssetPath(path));
                    if (AssetDatabase.GUIDToAssetPath(path).Contains(".unity") 
                        && AssetDatabase.GUIDToAssetPath(path).Contains("Test") == false
                        )
                    {


                        // int foundS1 = AssetDatabase.GUIDToAssetPath(path).IndexOf("Game0");
                        // int foundS2 = AssetDatabase.GUIDToAssetPath(path).IndexOf("\\.");
                        // Debug.Log(AssetDatabase.GUIDToAssetPath(path).IndexOf("Game0"));
                        // Debug.Log(AssetDatabase.GUIDToAssetPath(path).Substring(foundS1,foundS2));

                        // if (AssetDatabase.GUIDToAssetPath(path).Equals("Game0"  ))

                        Debug.Log("bIsUGUIVersion is " + bIsUGUIVersion);
                        if(bIsUGUIVersion == true 
                            && AssetDatabase.GUIDToAssetPath(path).Contains("UGUI") == true)
                        {
                            scenePath = AssetDatabase.GUIDToAssetPath(path);
                            
                        }
                        else if(bIsUGUIVersion == false
                            && AssetDatabase.GUIDToAssetPath(path).Contains("UGUI") == false)
                        {
                            Debug.Log("it's not contain UGUI path is " + path);
                            scenePath = AssetDatabase.GUIDToAssetPath(path);
                        }
                        Debug.Log("it's path is " + AssetDatabase.GUIDToAssetPath(path));

                    }

                }

                if (packetFile.name == "")
                {

                }

                Debug.Log(scenePath);
                EditorSceneManager.OpenScene(scenePath);

                var temp2 = "Game" + ((int)eGAMELIST).ToString("D2");
                Debug.Log(temp2 + " and " + EditorSceneManager.GetActiveScene().name);
                if (// EditorSceneManager.GetActiveScene().name.Equals(temp2)
                    EditorSceneManager.GetActiveScene().name.Contains(temp2))
                {
                    
                    Debug.Log(m_packetPrefabProp.objectReferenceValue.name);
                    
                    if (bPrefabExsist) // Setting Exsists object
                    {
                        
                        EditorGUILayout.HelpBox("there is replayer exsists.", MessageType.Warning);
                        var foundObject = GameObject.Find(m_packetPrefabProp.objectReferenceValue.name + "(Clone)");
                        //Destroy(foundObject);
                        DestroyImmediate(foundObject);

                        var cloneObject = Instantiate(m_packetPrefabProp.objectReferenceValue) as GameObject;
                        if (bFileAndSelectedMatched)
                        {
                            cloneObject.GetComponent<PacketTracker>().loadedFilePath = objectPath;
                            cloneObject.GetComponent<PacketTracker>().currentGameCode = eGAMELIST;
                            cloneObject.GetComponent<PacketTracker>().bIssuedFromTrackerWithFormatter = true;

                            if (bIsUGUIVersion)
                            {
                                cloneObject.GetComponent<PacketTracker>().bIsUGUI = true;
                            }
                        }

                        // foundObject.GetComponent<PacketTracker>().
                    }
                    else // Instantiate Prefab and set it up
                    {
                        var cloneObject = Instantiate(m_packetPrefabProp.objectReferenceValue) as GameObject;
                        if (bFileAndSelectedMatched)
                        {
                            cloneObject.GetComponent<PacketTracker>().loadedFilePath = objectPath;
                            cloneObject.GetComponent<PacketTracker>().currentGameCode = eGAMELIST;
                            cloneObject.GetComponent<PacketTracker>().bIssuedFromTrackerWithFormatter = true;
                            if (bIsUGUIVersion)
                            {
                                cloneObject.GetComponent<PacketTracker>().bIsUGUI = true;
                            }
                        }
                        

                    }

                    // UGUI 용으로 PacketTrackerNeededScripts 프리팹에 STM을 추가하였는데 혹시 NGUI 게임에서 STM 이 중복된다면 여기서 추가한 STM을 코드 상에서 삭제해야 할것으로 보임

                    if (GUILayout.Button("Replay"))
                    {
                        Debug.Log("Replay " + packetFile.name);

                    }
                }

            }
        }
    }
    
    private void DisplayPickerAgain()
    {
        if (GUILayout.Button("Display current game picker"))
        {
            bGameSelected = false;
            DisplayObjectPickerForPacketFile();
        }
    }

}
#endif


