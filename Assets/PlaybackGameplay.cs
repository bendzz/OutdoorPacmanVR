using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Reflection;

//using System.Text.Json;
//using System.Text.Json.Serialization;


/// <summary>
/// For recording and playing back gameplay for promotional purposes
/// </summary>
public class PlaybackGameplay : MonoBehaviour
{
    [Tooltip("Transforms to record and playback")]
    public List<Transform> transforms;
    public List<Ghost> ghosts;

    


    // Start is called before the first frame update
    void Start()
    {
        saveFile();
    }

    // Update is called once per frame
    void Update()
    {
         
    }


    public void saveFile()
    {
        //{
        //    var obj = ghosts[0];
        //    //var obj = Transform;
        //    print("breaking down " + obj);
        //    foreach (FieldInfo field in obj.GetType().GetFields())
        //    {
        //        print(obj.GetType() + ": " + field + " = " + field.GetValue(obj));
        //    }
        //}
        {
            //var obj = ghosts[0];
            var obj = transform;
            print("Breaking down: " + obj);
            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                try
                {
                    //print("PROPERTY = " + property + " TYPE = " + property.PropertyType + " VALUE = " + property.GetValue(obj));
                    //print("PROPERTY = " + property + " TYPE = " + property.PropertyType + " VALUE = " + property.GetValue(obj));
                    print("PROPERTY = " + property + " VALUE = " + property.GetValue(obj));
                } catch (System.Exception e)
                {
                    //Debug.LogError("Exception thrown: " + e);
                }
            }
            foreach (FieldInfo field in obj.GetType().GetFields())
            {
                print("FIELD = " + field + " VALUE = " + field.GetValue(obj));
            }
        }

        /*
        // https://docs.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide uh oh. "Don't use binaryFormatter"
        BinaryFormatter bf = new BinaryFormatter();

        /// https://docs.unity3d.com/ScriptReference/Application-dataPath.html  might be able to also do it saving to the editor project files
        FileStream file = File.Create(Application.persistentDataPath + "/jiggleMesh_" + meshName + ".save");
        bf.Serialize(file, this);
        file.Close();

        print("WrappedMesh saved to filesystem: " + file.Name);
        */

        //https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to?pivots=dotnet-6-0

        string filePath = Application.persistentDataPath + "/jiggleMesh_" + "testname1" + ".save";
        //FileStream file = File.Create(filePath);

        JsonableListWrapper<Ghost> lis = new JsonableListWrapper<Ghost>(ghosts);
        //JsonableListWrapper<Transform> lis = new JsonableListWrapper<Transform>(transforms);

        // https://forum.unity.com/threads/jsonutilities-tojson-with-list-string-not-working-as-expected.722783/
        //string stringListAsJson = JsonUtility.ToJson(new JsonListWrapper<string>(stringList));
        //string stringListAsJson = JsonUtility.ToJson(lis);
        //string stringListAsJson = JsonUtility.ToJson(lis); 
        string stringListAsJson = JsonUtility.ToJson(ghosts[0]); 

        //string jsonString = JsonUtility.ToJson(stringListAsJson); 

        File.WriteAllText(filePath, stringListAsJson);

        print("Mesh file written to " + filePath);
    }


    [System.Serializable]
    public class JsonableListWrapper<T>
    {
        public List<T> list;
        public JsonableListWrapper(List<T> list) => this.list = list;
    }
}

