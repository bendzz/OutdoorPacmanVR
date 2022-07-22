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

    //[System.Serializable]
    //public abstract class Frame
    //{
    //    // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/where-generic-type-constraint TODO limit types
    //    public float time;
    //    //public Frame()
    //    //{ }
    //    //public abstract void playValue();
    //    //public abstract void recordValue();
    //}
    //[System.Serializable]
    //public class TransformFrame : Frame
    //{
    //    bool testTF = true;
    //    Transform transform;

    //    public TransformFrame(Transform tf)
    //    {
    //        transform = tf;
    //    }

    //    //public override void playValue()
    //    //{
    //    //    throw new System.NotImplementedException();
    //    //}
    //    //public override void recordValue()
    //    //{
    //    //    throw new System.NotImplementedException();
    //    //}
    //}
    //[System.Serializable]
    //public class GhostFrame : Frame
    //{
    //    public GhostFrame(Ghost ghos)
    //    {
    //        ghost = ghos;
    //    }
    //    float testGhost = 0;
    //    Ghost ghost;
    //}

    // TODO a comment or tooltip explaining this saving hierarchy
    // TODO a class or method to tie frame IDs to objects in unity!
    // "keyset"?

    //[System.Serializable]
    //public class Frame
    //{
    //    public int ID;
    //    public float time;
    //    public FrameData data;

    //    public string ToJson()
    //    {
    //        return JsonUtility.ToJson(this) + data.ToJson();
    //    }
    //    public void loadJSON()
    //    {

    //    }
    //}
    //[System.Serializable]
    // TODO: Write a far more compact string to data and data to string function that can be called by all of these 
    public abstract class FrameData
    {
        public float time;
        public int ID;
        public System.Type type;

        //public abstract void record();

        public FrameData(int _ID, float _time)
        {
            time = _time;
            ID = _ID;
            type = GetType();
            //type = ;
        }

        public abstract void playBack();
        
        public virtual string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public virtual void FromJson(string json)
        {
            JsonUtility.FromJsonOverwrite(json, this);
        }
    }
    //[System.Serializable]
    public class TransformFrame : FrameData
    {
        public Transform obj;

        public Vector3 lPos;
        public Vector3 lScal;
        public Quaternion lRot;

        public TransformFrame(Transform refTransform, int _ID, float _time) : base(_ID, _time)
        {
            obj = refTransform;

            lPos = obj.localPosition;
            lScal = obj.localScale;
            lRot = obj.localRotation;
        }

        //public override string ToJson()
        //{
        //    return JsonUtility.ToJson(this) + " alt func";
        //}

        public override void playBack()
        {
            obj.localPosition = lPos;
            obj.localScale = lScal;
            obj.localRotation = lRot;
        }
    }
    // TODO how to keep the same keying set for frame after frame being added?
    /*
    /// <summary>
    /// Can store object and script data from .GetType().GetProperties() and .GetFields;
    /// TODO: add systems for recording and playing back method calls?
    /// </summary>
    public class GenericFrame : FrameData
    {
        // TODO should these be private? Will the JSON still work?
        // TODO checks to make sure these lists are in sync
        public List<PropertyInfo> properties;
        public List<Object> propertyData;

        public List<FieldInfo> fields;
        public List<Object> fieldData;

        public GenericFrame()
        {
            properties = new List<PropertyInfo>();
            propertyData = new List<Object>();

            fields = new List<FieldInfo>();
            fieldData = new List<Object>();
        }

        public void addProperty(PropertyInfo property)
        {
            properties.Add(property);
            propertyData.Add(null);
        }
        public void addField(FieldInfo field)
        {
            fields.Add(field);
            fieldData.Add(null);
        }

        public override string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public override void FromJson(string json)
        {
            JsonUtility.FromJsonOverwrite(json, this);
        }

        // TODO setting and reading data view the fields/properties
        public override void playBack()
        {
            throw new System.NotImplementedException();
        }

        //public override void record()
        //{
        //    throw new System.NotImplementedException();
        //}
    }
    */


    //public class Keyframe1<T> : Keyframe
    //{
    //    public T value;
    //    public float time;
    //}
    //public class Keyframe<Transform> : Keyframe<T> where T : Transform
    //{
    //    public T value;
    //    public float time;
    //}
    //public List<Frame> keyingSet;

    public List<FrameData> frames;

    // Start is called before the first frame update
    void Start()
    {
        //saveFile();

        //keyingSet = new List<Frame>();
        //keyingSet.Add(new TransformFrame(transform));
        //keyingSet.Add(new GhostFrame(ghosts[0]));

        //JsonableListWrapper<Frame> jsonList = new JsonableListWrapper<Frame>(keyingSet);
        ////string json = JsonUtility.ToJson(jsonList);
        //string json = JsonUtility.ToJson(new GhostFrame(ghosts[0]));
        //print(json);

        //Frame frame = new Frame();
        //frame.time = Time.realtimeSinceStartup;
        //frame.data = new TransformFrame(transform);

        //string json = JsonUtility.ToJson(frame);
        //FrameData frame = new TransformFrame(transform, 123, Time.realtimeSinceStartup);
        //string json = frame.ToJson();
        //print("JSON " + json);

        //FrameData frame2 = new TransformFrame(transform, 222, 555);
        //frame2.FromJson(frame.ToJson());
        //string json2 = frame2.ToJson();
        //print("JSON 2 " + json2);


        frames = new List<FrameData>();
        frames.Add(new TransformFrame(transform, 123, Time.realtimeSinceStartup));
        frames.Add(new TransformFrame(transform, 5555, 6666));

        saveFrames();

        //loadFrames();
    }

    // Update is called once per frame
    void Update()
    {
         
    }

    public void loadFrames()
    {
        frames = new List<FrameData>();

        string filePath = Application.persistentDataPath + "/savedFrames" + "test1" + ".save";

        foreach (string line in File.ReadLines(filePath))
        {
            //System.Console.WriteLine(line);
            //counter++;
            // get type
            int i0 = line.IndexOf("\"type\"");
            int i1 = line.IndexOf(",", i0);
            //System.Type type = (System.Type)line.Substring(i0, i1 - i0);
            System.Type type = System.Type.GetType( line.Substring(i0, i1 - i0));

            frames.Add((FrameData)JsonUtility.FromJson(line, type));
            //frames.Add(System.Convert.ChangeType(JsonUtility.FromJson(line, type), type);
        }
    }

    public void saveFrames()
    {
        string filePath = Application.persistentDataPath + "/savedFrames" + "test1" + ".save";

        File.WriteAllText(filePath, "");  // overwrite file

        using (StreamWriter sw = File.AppendText(filePath))
        {
            //sw.WriteLine("This is the new text");
            foreach(FrameData frame in frames)
            {
                sw.WriteLine(frame.ToJson());
            }
        }
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

    /// <summary>
    /// Wrap a list so it gets exported in json
    /// https://forum.unity.com/threads/jsonutilities-tojson-with-list-string-not-working-as-expected.722783/
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [System.Serializable]
    public class JsonableListWrapper<T>
    {
        public List<T> list;
        public JsonableListWrapper(List<T> list) => this.list = list;
    }


}

