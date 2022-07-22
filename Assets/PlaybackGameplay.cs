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
/// TODO rename to Recordings
/// </summary>
public class PlaybackGameplay : MonoBehaviour
{
    [Tooltip("Transforms to record and playback")]
    public List<Transform> transforms;
    public List<Ghost> ghosts;


    public Clip clip;

    /// <summary>
    /// Animation clips -> AnimatedObjects -> AnimatedPropertys -> FrameData // TODO
    /// </summary>
    public class Clip
    {
        public string name;
        //public List<AnimatedObject> objects;
        /// <summary>
        /// Note: The order of this has to stay constant during the game- No removing properties. 
        /// These indices are used for matching frameData to their property during saving/loading
        /// </summary>
        public List<AnimatedProperty> animatedProperties;
        //public Dictionary<int, AnimatedProperty> animatedProperties;

        public Clip(string _name)
        {
            name = _name;
            //objects = new List<AnimatedObject>();
            animatedProperties = new List<AnimatedProperty>();
            //animatedProperties = new Dictionary<int, AnimatedProperty>();
        }

        public AnimatedProperty addProperty(object obj)
        {
            AnimatedProperty property = new AnimatedProperty(obj, this);
            //animatedProperties.Add(property);
            //animatedProperties.Add(property);
            return property;
        }

        public void saveClip()
        {
        }
        public void loadClip()
        {
        }
    }

    //public class AnimatedObject
    //{
    //    /// <summary>
    //    /// The link to the object in question. Set manually or via scene hierarchy or other methods, stored at the start of the JSON file.
    //    /// </summary>
    //    public object obj;
    //    public List<AnimatedProperty> animatedProperties;

    //    public AnimatedObject(object _obj)
    //    {
    //        obj = _obj;
    //        animatedProperties = new List<AnimatedProperty>();
    //    }

    //}
    /// <summary>
    /// Links frameDatas to unity properties and serves as a factory for creating different FrameData types
    /// </summary>
    public class AnimatedProperty
    {
        /// <summary>
        /// Which clip this property belongs to
        /// </summary>
        public Clip clip;
        
        /// <summary>
        /// Corresponds to the ID in FrameData
        /// </summary>
        public int ID;


        //public string objString;  // JSON reference to object..? 
        /// <summary>
        /// In game reference to the variable/method/object etc to be recorded or animated
        /// </summary>
        public object obj;

        //public System.Type FrameType;
        public System.Type Type;
        public string type;


        /// <summary>
        /// For saving and restoring Reflection gathered FieldInfo, PropertyInfo, methods etc, so those variables/methods can be recorded and animated
        /// </summary>
        public string propertyReference;

        public List<FrameData> frames;

        //public AnimatedProperty(object _property, System.Type _FrameType)
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_obj">The property to be animated</param>
        public AnimatedProperty(object _obj, Clip _clip)
        {
            obj = _obj;
            clip = _clip;
            //FrameType = _FrameType; // TODO automate this?

            _clip.animatedProperties.Add(this);
            ID = _clip.animatedProperties.Count - 1;

            Type = obj.GetType();
            type = Type.ToString();

            frames = new List<FrameData>();

        }

        /// <summary>
        /// Factory to make the correct frameData type
        /// </summary>
        public void addNewFrame(float time)
        {
            FrameData frame = null;
            //frame = new (FrameType)(ID, this);  // TODO how?

            if (obj is Transform)
            {
                frame = new TransformFrame(this, time);
            }
            else
                Debug.LogError("unknown frameData object attempting to be created for: " + obj);

            frames.Add(frame);
        }
    }

    // TODO: Write a far more compact string to data and data to string function that can be called by all of these 
    public abstract class FrameData
    {
        //public string type; // TODO pretty wasteful having this in every frame
        public float time;
        public int ID;
        private AnimatedProperty property;
        //public System.Type type;

        //public abstract void record();

        //public FrameData(int _ID, float _time, AnimatedProperty _property)
        public FrameData(float _time, AnimatedProperty _property)
        {
            time = _time;
            ID = _property.ID;
            //type = GetType();
            //type = ;
            //type = GetType().ToString();
            property = _property;
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
        //public Transform obj;
        public AnimatedProperty property;

        public Vector3 lPos;
        public Vector3 lScal;
        public Quaternion lRot;

        //public TransformFrame(AnimatedProperty _property, int _ID, float _time) : base(_ID, _time, _property)
        public TransformFrame(AnimatedProperty _property, float _time) : base(_time, _property)
        {
            //obj = refTransform;
            property = _property;
            Transform obj = (Transform)property.obj;

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
            Transform obj = (Transform)property.obj;

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


    // Start is called before the first frame update
    void Start()
    {

        //testReflection();

        clip = new Clip("test pacman clip");
        AnimatedProperty property = clip.addProperty(transform);

        property.addNewFrame(Time.time);
        property.addNewFrame(666);

        print("JSON " + property.frames[0].ToJson());
        print("JSON " + property.frames[1].ToJson());

        AnimatedProperty property2 = clip.addProperty(transforms[0]);
        property2.addNewFrame(Time.time);
        print("JSON 2 " + property2.frames[0].ToJson());
    }

    // Update is called once per frame
    void Update()
    {
         
    }

    //public void loadFrames()
    //{
    //    frames = new List<FrameData>();

    //    string filePath = Application.persistentDataPath + "/savedFrames" + "test1" + ".save";

    //    foreach (string line in File.ReadLines(filePath))
    //    {
    //        //System.Console.WriteLine(line);
    //        //counter++;
    //        // get type
    //        int i0 = line.IndexOf("\"type\"");
    //        int i1 = line.IndexOf(",", i0);
    //        //System.Type type = (System.Type)line.Substring(i0, i1 - i0);
    //        System.Type type = System.Type.GetType( line.Substring(i0, i1 - i0));

    //        frames.Add((FrameData)JsonUtility.FromJson(line, type));
    //        //frames.Add(System.Convert.ChangeType(JsonUtility.FromJson(line, type), type);
    //    }
    //}

    //public void saveFrames()
    //{
    //    string filePath = Application.persistentDataPath + "/savedFrames" + "test1" + ".save";

    //    File.WriteAllText(filePath, "");  // overwrite file

    //    using (StreamWriter sw = File.AppendText(filePath))
    //    {
    //        //sw.WriteLine("This is the new text");
    //        foreach(FrameData frame in frames)
    //        {
    //            sw.WriteLine(frame.ToJson());
    //        }
    //    }
    //}

    public void testReflection()
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
            var obj = ghosts[0];
            print("Breaking down: " + obj);
            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                try
                {
                    //print("PROPERTY = " + property + " TYPE = " + property.PropertyType + " VALUE = " + property.GetValue(obj));
                    //print("PROPERTY = " + property + " TYPE = " + property.PropertyType + " VALUE = " + property.GetValue(obj));
                    print("PROPERTY = " + property + " VALUE = " + property.GetValue(obj));
                    print("JSON " + JsonUtility.ToJson(property));
                }
                catch (System.Exception e)
                {
                    //Debug.LogError("Exception thrown: " + e);
                }
            }
            foreach (FieldInfo field in obj.GetType().GetFields())
            {
                print("FIELD = " + field + " VALUE = " + field.GetValue(obj));
                print("JSON " + JsonUtility.ToJson(field));
            }
        }
    }

    public void saveFile()
    {

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

