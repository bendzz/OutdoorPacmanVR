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
        public const string propertyString = "Property=";
        public const string frameString = "F=";

        public string name;
        /// <summary>
        /// Note: The order of this has to stay constant during the game- No removing properties. 
        /// These indices are used for matching frameData to their property during saving/loading
        /// </summary>
        public List<AnimatedProperty> animatedProperties;

        public Clip(string _name)
        {
            name = _name;
            animatedProperties = new List<AnimatedProperty>();
        }

        public AnimatedProperty addProperty(object obj, GameObject gameObject)
        {
            AnimatedProperty property = new AnimatedProperty(obj, gameObject, this);
            return property;
        }

        public static string GetFilePath(string _name)
        {
            return Application.persistentDataPath + "/Clip_" + _name + ".txt";
        }

        /// <summary>
        /// Records a frame to all contained properties
        /// </summary>
        public void recordFrame(float time)
        {
            foreach(AnimatedProperty property in animatedProperties)
            {
                property.addNewFrame(time);
            }
        }

        public void saveClip()
        {
            string filePath = GetFilePath(name);

            File.WriteAllText(filePath, "");  // overwrite file

            using (StreamWriter sw = File.AppendText(filePath))
            {
                foreach (AnimatedProperty property in animatedProperties)
                {
                    sw.WriteLine(propertyString + property.ToJson());
                }
                foreach (AnimatedProperty property in animatedProperties)
                {
                    foreach (FrameData frame in property.frames)
                    {
                        sw.WriteLine(frameString + frame.ToJson());
                    }
                }

            }
        }
        public static Clip loadClip(string _name)
        {
            Clip newClip = new Clip(_name);
            newClip.name = _name;

            string filePath = GetFilePath(_name);

            foreach (string line in File.ReadLines(filePath))
            {
                //print("line " + line);
                int i0 = line.IndexOf("=");
                //print("i0 " + i0);
                string type = line.Substring(0, i0 + 1);
                string trimmedLine = line.Substring(i0 + 1);
                //print("trimmed line " + trimmedLine);
                if (type.Contains(propertyString))
                {
                    //newClip.addProperty()
                    //AnimatedProperty property = AnimatedProperty.FromJson(line);
                    AnimatedProperty property = AnimatedProperty.FromJson(trimmedLine);
                    property.preLoadedProperty();
                    newClip.animatedProperties.Add(property);
                    //print("property type " + property.frameType);
                }
                else if (type.Contains(frameString))
                {
                    // get frameType
                    string IDSearch = "\"ID\":";
                    int s0 = line.IndexOf(IDSearch) + IDSearch.Length;
                    //print("s0 " + s0);
                    int s1 = line.IndexOf(",", s0);
                    string IDs = line.Substring(s0, s1 - s0);
                    //print("IDs " + IDs);
                    int ID = int.Parse(IDs);
                    //print("ID " + ID);

                    //AnimatedProperty property = newClip.animatedProperties[frame.ID];
                    AnimatedProperty property = newClip.animatedProperties[ID];
                    string frameTypeS = property.frameType;

                    System.Type frameType = System.Type.GetType(frameTypeS);
                    //print("frameType " + frameType);

                    //FrameData frame = JsonUtility.FromJson<FrameData>(trimmedLine);
                    //FrameData frame = JsonUtility.FromJson<typeof( frameType)>(trimmedLine);
                    //FrameData frame = JsonUtility.FromJson<TransformFrame>(trimmedLine);    // TEMP
                    FrameData frame = (FrameData)JsonUtility.FromJson(trimmedLine, frameType);

                    frame.setProperty(property);

                    property.frames.Add(frame);
                }
                else
                    Debug.LogError("Couldn't determine type of file line " + line);
            }

            return newClip;
        }
        public void debugPrintClip()
        {
            foreach (AnimatedProperty property in animatedProperties)
            {
                print("PROPERTY " + property.ToJson());
                foreach (FrameData frame in property.frames)
                {
                    print("FRAME " + frame.ToJson());
                }
            }
        }
    }


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
        /// What FrameData type exactly loaded frames need to be casted into
        /// </summary>
        public string frameType;

        /// <summary>
        /// Some way for the data to be linked to an object when loaded back into unity. 
        /// Can be script specific or from the scene hierarchy or etc. Default is the object name
        /// </summary>
        public string gameObjectRef;
        public GameObject gameObject;   // TODO make this not record to Json; private, with getters/setters?
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
        public AnimatedProperty(object _obj, GameObject _gameObject, Clip _clip)
        {
            obj = _obj;
            clip = _clip;

            gameObject = _gameObject;
            gameObjectRef = gameObject.name;


            _clip.animatedProperties.Add(this);
            ID = _clip.animatedProperties.Count - 1;

            Type = obj.GetType();
            type = Type.ToString();

            frames = new List<FrameData>();

        }

        /// <summary>
        /// Only used for loading clips from a file. The game object references still need to be restored.
        /// </summary>
        //public AnimatedProperty(string _gameObjectRef, string _type, string _propertyReference,  Clip _clip)
        //{
        //    gameObjectRef = _gameObjectRef;
        //    type = _type;
        //    propertyReference = _propertyReference;
        //    clip = _clip;



        //    frames = new List<FrameData>();
        //}


        /// <summary>
        /// Factory to make the correct frameData type
        /// </summary>
        public void addNewFrame(float time)
        {
            FrameData frame = null;
            //frame = new (FrameType)(ID, this);  // TODO how? Automated type setup?

            if (obj is Transform)
            {
                frame = new TransformFrame(this, time);
            }
            else
                Debug.LogError("unknown frameData object attempting to be created for: " + obj);

            frames.Add(frame);
            if (frameType == null)
            {
                frameType = frame.GetType().ToString();
            }
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
        public static AnimatedProperty FromJson(string json)
        {
            return (AnimatedProperty)JsonUtility.FromJson(json, typeof(AnimatedProperty));
        }
        /// <summary>
        /// When a property is loaded from a file via Json, this sets up some basics. Doesn't restore gameobject refs by default though!
        /// </summary>
        public void preLoadedProperty()
        {
            frames = new List<FrameData>();
        }
    }

    // TODO: Write a far more compact string to data and data to string function that can be called by all of these 
    public abstract class FrameData
    {
        //public string type; // TODO pretty wasteful having this in every frame
        public int ID;
        public float time;
        protected AnimatedProperty property;
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

        public void setProperty(AnimatedProperty _property)
        {
            property = _property;
        }
    }
    //[System.Serializable]
    public class TransformFrame : FrameData
    {
        //public Transform obj;
        //public AnimatedProperty property;

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

    int frameCount = 0;

    // Start is called before the first frame update
    void Start()
    {

        //testReflection();

        //clip = new Clip("test pacman clip");
        //AnimatedProperty property1 = clip.addProperty(transform, gameObject);

        //property1.addNewFrame(Time.time);
        //property1.addNewFrame(666);

        ////print("JSON " + property.frames[0].ToJson());
        ////print("JSON " + property.frames[1].ToJson());

        //AnimatedProperty property2 = clip.addProperty(transforms[0], transforms[0].gameObject);
        //property2.addNewFrame(Time.time);
        ////print("JSON 2 " + property2.frames[0].ToJson());

        //clip.saveClip();


        //clip.loadClip("test pacman clip");
        //clip = Clip.loadClip("test pacman clip");

        //clip.debugPrintClip();


        //clip = new Clip("test pacman clip");
        //foreach(Transform tf in transforms)
        //{
        //    clip.addProperty(tf, tf.gameObject);
        //}


        clip = Clip.loadClip("test pacman clip");

        int index = 0;
        foreach (Transform tf in transforms)
        {
            //clip.addProperty(tf, tf.gameObject);
            clip.animatedProperties[index].obj = tf;
            //print("set property " + index + " of " + clip.animatedProperties[index].obj);
            index++;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //clip.recordFrame(Time.realtimeSinceStartup);

        foreach(AnimatedProperty property in clip.animatedProperties)
        {
            //print("property " + property.obj);
            property.frames[frameCount].playBack();
        }
        frameCount++;
        if (frameCount >= 450)
            frameCount = 0;
    }

    private void OnDestroy()
    {
        //print("Saving animation data");
        //clip.saveClip();
    }

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

