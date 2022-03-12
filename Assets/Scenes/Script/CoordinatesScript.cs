/* Enrico Casarotto - Piero Pettena - February/March 2022
 * This script is thought to be applied to an Microsoft Hololens 2 app development in Unity.
 * It allows a user to save the position and rotation of GameObjects in a scene to a json file, and load the data from a json file.
 * The file format is very specific and future implementations of this project should test the file format before trying to read it.
 * Each line of the json file is a single SaveObject element, thus the JSON Serialization requires the SaveObject script of this project,
 * where this class is defined. This script is thought to be attached to a GameObject whose purpose is that of managing the scene.
 * No testing has been done on this script and little attention has been paid to the efficiency of the code. Future implementations
 * should consider both these matters, as well as possible memory allocation problems.
 * This project was developed using:
 *      - Unity Version 2020.3.24f1 Personal
 *      - Microsoft Visual Studio Community 2019 Version 16.11.10
 *      - Build platform Universal Windows Platform
 *      - MRTK Version 2.7.2.0.
 * Project link on GitHub: 
 ***************************************************************************************************************************
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;

public class CoordinatesScript : MonoBehaviour
{
    /* dir and filePath contain json directory and file path (including file name), respectively.
     * json is the string file written by the unity json write methods.
     * jsonArray is the string array read by the unity json read methods.
     */
    static string dir;
    static string filePath;
    private string json;    
    private string[] jsonArray;

    /* totalObjects contains every GameObject in its initial position when Start() is executed.
     * initialData contains initial relevant information for every GameObject.
     * scene is the current active scene. 
     */
    private GameObject[] totalObjects;
    private SaveObject[] initialData;
    Scene scene;

    //Saves initial data of every GameObject and creates json directory
    public void Start()
    {
        /* Finds all GameObjects at the beginning of the scene that have a Rigidbody component attached
         * and saves them in an array of GameObjects
         */
        Rigidbody[] rigidbodyArrayObject = FindObjectsOfType<Rigidbody>(true);
        totalObjects = new GameObject[rigidbodyArrayObject.Length];

        for (int i = 0; i < rigidbodyArrayObject.Length; i++)
            totalObjects[i] = rigidbodyArrayObject[i].gameObject;

        //Convert initial GameObjects to SaveObjects  
        initialData = new SaveObject[totalObjects.Length]; 
        for(int i = 0; i < totalObjects.Length; i++)
            initialData[i] = GOToSO(totalObjects[i]);

        //Writing directory and file path as strings
        dir = Application.streamingAssetsPath + "/SavedData/";
        scene = SceneManager.GetActiveScene();
        filePath = Application.streamingAssetsPath + "/SavedData/DataObjectSet" + scene.buildIndex + ".json";

        //Check if the directory dir exists, otherwise create it
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    /* Saves to a json file in dir the position, rotation, scale and name of every active GameObject.
     * activeObjects is the array of active objects at the moment of assignment.
     * This function is thought to be called by an event (e.g. on click() event of a button).
     */
    public void SaveToJson()
    {
        File.WriteAllText(filePath, string.Empty);
        GameObject[] activeObjects = GameObject.FindGameObjectsWithTag("model");
        
        SaveObject so;
        foreach (GameObject go in activeObjects)
        {
            so = GOToSO(go);
            json = JsonUtility.ToJson(so);
            File.AppendAllText(filePath, json + "\n");
        }
    }
    
    /* Resets position, rotation and scale of active objects.
     * Data is stored in SaveObject format so it can use UpdateExistingObjects method.
     * activeObjects contains every active object at execution time. If there are 
     * no active objects the function doesn't do anything and prints a message to the console
     */
    public void Reset()
    {
        GameObject[] activeObjects = GameObject.FindGameObjectsWithTag("model");

        if(activeObjects.Length != 0)
        {
            GameObject[] resetObjects = new GameObject[initialData.Length];

            for (int i = 0; i < initialData.Length; i++)
                resetObjects[i] = SOToGO(initialData[i]);

            UpdateExistingObjects(resetObjects, activeObjects);
        }
        else
        {
            Debug.LogError("In Reset(): there are no active objects to reset.");
        }
    }

    /* Loads data only in SaveObject format (see SaveObject.cs) from json file stored in filePath
     * Data has to be in the same specific format as it is written in SaveToJson(), that is each
     * line must contain only one SaveObject. If file in filePath path doesn't exist or is empty,
     * it prints a message to the Unity console.
     */
    public void LoadFromJson()
    {
        if(File.Exists(filePath) && new FileInfo(filePath).Length != 0)
        {
            /* Separates every line from json file and saves them in jsonArray
             * Then converts from json to SaveObject each line and finally from
             * SaveObject to GameObject, all in one line.
             */
            jsonArray = File.ReadAllLines(filePath);
            GameObject[] newGameObj = new GameObject[jsonArray.Length];

            for (int i = 0; i < jsonArray.Length; i++)
                newGameObj[i] = SOToGO(JsonUtility.FromJson<SaveObject>(jsonArray[i]));

            UpdateExistingObjects(newGameObj, totalObjects);
        }
        else
        {
            if(File.Exists(filePath))
                Debug.LogError("Save file is empty.");
            else
                Debug.LogError("Save file does not exist.");
        }
    }

    /* Updates oldList GameObject array with data from newList.
     * targetIndex is the index of oldList where the current object being read in oldList is saved.
     * If an object in newList is not found in oldList nothing happens.
     */
    public void UpdateExistingObjects(GameObject[] newList, GameObject[] oldList)
    {        
        int targetIndex;
        
        for(int i = 0; i < newList.Length; i++)
        {
            targetIndex = FindExistingObject(oldList, newList[i].name);
            if(targetIndex != oldList.Length)
            {
                oldList[targetIndex].transform.localPosition = newList[i].transform.localPosition;
                oldList[targetIndex].transform.localRotation = newList[i].transform.localRotation;
                oldList[targetIndex].transform.localScale = newList[i].transform.localScale;
                
                Physics(oldList[targetIndex]);                
            }
            //Destroys old objects from the variables of this script that are not used anymore
            Destroy(newList[i]);
        }
    }

    //Returns index of list where an object called target is found. Returns list.Length if object is not found inside list.
    public int FindExistingObject(GameObject[] list, string target)
    {
        int i;
        for(i = 0; i < list.Length; i++)
        {
            if (list[i].name == target)
                return i;
        }
        return i;
    }

    //Activates GameObject and sets linear and angular velocity to zero (0;0;0)
    public void Physics(GameObject OBJ)
    {
        OBJ.SetActive(true);

        OBJ.GetComponent<Rigidbody>().velocity = Vector3.zero;
        OBJ.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }

    //Reads GameObject data and returns SaveObject.
    public SaveObject GOToSO(GameObject go)
    {
        SaveObject so = new SaveObject();
        so.name = go.name;
        so.position = Vector3ToArray(go.transform.localPosition);
        so.rotation = QuaternionToArray(go.transform.localRotation);
        so.scale = Vector3ToArray(go.transform.localScale);

        return so;
    }

    //Reads SaveObject and creates GameObject with this info. Then returns GameObject
    public GameObject SOToGO(SaveObject so)
    {
        GameObject go = new GameObject();
        go.name = so.name;
        go.transform.localPosition = ArrayToVector3(so.position);
        go.transform.localRotation = ArrayToQuaternion(so.rotation);
        go.transform.localScale = ArrayToVector3(so.scale);

        return go;
    }

    // Converts from Vector3 to float array and returns array. 
    public float[] Vector3ToArray(Vector3 vector)
    {
        float[] array = new float[3];
        array[0] = vector.x;
        array[1] = vector.y;
        array[2] = vector.z;

        return array;
    }

    // Converts from float array to Vector3 and returns Vector3. 
    public Vector3 ArrayToVector3(float[] array)
    {
        Vector3 vector = new Vector3();
        vector.x = array[0];
        vector.y = array[1];
        vector.z = array[2];

        return vector;
    }

    // Converts from Quaternion to float array and returns array.
    public float[] QuaternionToArray(Quaternion quat)
    {
        float[] array = new float[4];
        array[0] = quat.x;
        array[1] = quat.y;
        array[2] = quat.z;
        array[3] = quat.w;

        return array;
    }
    
    // Converts from float array to Quaternion and returns Quaternion.
    public Quaternion ArrayToQuaternion(float[] array)
    {
        Quaternion quat = new Quaternion();
        quat.x = array[0];
        quat.y = array[1];
        quat.z = array[2];
        quat.w = array[3];

        return quat;
    }
}