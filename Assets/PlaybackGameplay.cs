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
    public bool activelyRecordingOrPlaying = true;
    [Tooltip("WARNING temporary, must be set before game starts")]
    public bool recordingNotPlayback = true;

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


        // TODO: Need to set clip time as it records (via timedelta?) and grab the max time from loaded recordings

        /// <summary>
        /// The current playback time of the clip (loops when it reaches the end and a frame is played)
        /// </summary>
        public float clipTime;
        public float clipLength;


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

        //public void playbackAFrame(float timeDelta)
        //{
        //    clipTime += timeDelta;
        //    if (clipTime > clipLength)
        //        clipTime = clipTime % clipLength;

        //    foreach (AnimatedProperty property in animatedProperties)
        //    {
        //        //property.addNewFrame(time);
        //        asdasda
        //    }
        //}

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

                    //System.Type objType = System.Type.GetType(property.typeString);
                    //property.obj = System.Convert.ChangeType(property.objString, objType);


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
        /// <summary>
        /// In case the Json fails to convert the obj into string
        /// </summary>
        public string objString;

        //public System.Type FrameType;
        public System.Type type;
        public string typeString;
        /// <summary>
        /// What FrameData type exactly loaded frames need to be casted into
        /// </summary>
        public string frameType;


        /// <summary>
        /// Note, needs to point to the specific script or component etc to be animated (if using reflection)
        /// </summary>
        public GameObject gameObject;   // TODO make this not record to Json; private, with getters/setters?
        /// <summary>
        /// Some way for the data to be linked to an object when loaded back into unity. 
        /// Can be script specific or from the scene hierarchy or etc. Default is the object name
        /// </summary>
        public string gameObjectString;    // TODO not actually used when loading in clips atm


        /// <summary>
        /// (Used for Reflection based keyframes) The actual script component or transform or gameobject or whatever that the propertyOrField belongs to.
        /// </summary>
        public object animatedComponent;
        /// <summary>
        /// For saving and restoring Reflection gathered FieldInfo, PropertyInfo, methods etc, so those variables/methods can be recorded and animated
        /// </summary>
        public string animatedComponentString;

        public List<FrameData> frames;


        //public AnimatedProperty(object _property, System.Type _FrameType)
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_obj">The property to be animated</param>
        public AnimatedProperty(object _obj, GameObject _gameObject, Clip _clip)
        {
            startConstructor(_obj, _gameObject, _clip);

            finishConstructor();
        }

        void startConstructor(object _obj, GameObject _gameObject, Clip _clip)
        {
            obj = _obj;
            clip = _clip;

            gameObject = _gameObject;
            gameObjectString = gameObject.name;
        }
        void finishConstructor()
        {
            clip.animatedProperties.Add(this);
            ID = clip.animatedProperties.Count - 1;

            objString = obj.ToString();

            type = obj.GetType();
            typeString = type.ToString();

            frames = new List<FrameData>();

            frameDataFactory(this, 0);  // Will log an error if the frame type isn't supported. (Note: causes a bit of garbage collection)
        }

        /// <summary>
        /// For setting up Reflection based keyframes, like modifying variables using FieldInfo or PropertyInfo
        /// </summary>
        /// <param name="_gameObject">The base gameobject</param>
        /// <param name="animatedObject">The actual script component or transform or gameobject or whatever that the propertyOrField belongs to.</param>
        /// <param name="propertyOrField">The specific value of the script/gameobject to be animated (will be grabbed via reflection)</param>
        /// <param name="_clip">parent clip</param>
        /// 
        //public AnimatedProperty(object _animatedObject, object propertyOrField, GameObject _gameObject, Clip _clip) : this(propertyOrField, _gameObject, _clip)
        public AnimatedProperty(object _animatedObject, object propertyOrField, GameObject _gameObject, Clip _clip)
        {
            startConstructor(propertyOrField, _gameObject, _clip);

            animatedComponent = _animatedObject;
            animatedComponentString = animatedComponent.ToString();

            obj = null;
            foreach (PropertyInfo property in animatedComponent.GetType().GetProperties())
            {
                try
                {
                    //print("PROPERTY = " + property + " VALUE = " + property.GetValue(obj));
                    if (propertyOrField.Equals(property.GetValue(animatedComponent)))
                    {
                        obj = property;
                        break;
                    }
                }
                catch (System.Exception e)
                {
                    //Debug.LogError("Exception thrown: " + e); 
                }
            }
            foreach (FieldInfo field in animatedComponent.GetType().GetFields())
            {
                //print("FIELD = " + field + " VALUE = " + field.GetValue(obj));
                if (propertyOrField.Equals(field.GetValue(animatedComponent)))
                {
                    obj = field;
                    break;
                }
            }
            if (obj == null)
                Debug.LogError("Reflection failed");

            //if (obj is FieldInfo)
            //    print("FieldInfo");
            //else
            //    print("Property");
            //print("TEST " + propertyOrField + " property/field " + obj + " ref type " + obj.GetType());

            finishConstructor();
        }

        // TODO add helper functions that can re-find the game object based on a saved path string etc
        /// <summary>
        /// Also goes through and readies the loaded in frames
        /// </summary>
        /// <param name="_gameObject"></param>
        public void linkLoadedPropertyToObject(GameObject _gameObject)
        {
            gameObject = _gameObject;

            // set property links
            foreach(Component component in gameObject.GetComponents(typeof(Component)))
            {
                //print("component " + component);
                if (component.ToString().Equals(animatedComponentString))
                {
                    //print("matched animatedObject to " + component);
                    animatedComponent = component;

                    foreach (PropertyInfo property in animatedComponent.GetType().GetProperties())
                    {
                        try
                        {
                            if (property.ToString().Equals(objString))
                            {
                                //print("MATCHED PROPERTY! " + property);
                                obj = property;
                                break;
                            }
                        }
                        catch (System.Exception e)
                        { } //Debug.LogError("Exception thrown: " + e); 
                    }
                    foreach (FieldInfo field in animatedComponent.GetType().GetFields())
                    {
                        if (field.ToString().Equals(objString))
                        {
                            //print("MATCHED FIELD! " + field);
                            obj = field;
                            break;
                        }
                    }
                    break;
                } else if (component.ToString().Equals(objString))
                {
                    //print("matched obj to " + component);
                    obj = component;
                    break;
                }
            }
            if (obj == null)
                Debug.LogError("ERROR: Failed to match " + gameObject + " to property objString " + objString);

            // cleanup frames
            foreach(FrameData frame in frames)
            {
                frame.loadedFromJson();
            }
        }


        /// <summary>
        /// Factory to make the correct frameData type
        /// </summary>
        public void addNewFrame(float time)
        {
            FrameData frame = frameDataFactory(this, time);

            frames.Add(frame);
            if (frameType == null)
            {
                frameType = frame.GetType().ToString();
            }
        }

        /// <summary>
        /// Generate new framedatas of the correct polymorphic type to hold their data. (And test if the types are yet supported)
        /// </summary>
        /// <returns></returns>
        public FrameData frameDataFactory(AnimatedProperty parent, float time)
        {
            FrameData frame = null;

            //frame = new (FrameType)(ID, this);  // TODO how? Automated type setup?

            if (obj is Transform)
            {
                frame = new TransformFrame(this, time);
            }
            else if (obj is FieldInfo)
            {
                frame = new GenericFrame(this, time);
            } else if (obj is PropertyInfo)
            {
                frame = new GenericFrame(this, time);
            }
            else
                Debug.LogError("unknown frameData type attempting to be created for: " + obj + " of type: " + obj.GetType());

            return frame;
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

        //public virtual void FromJson(string json)
        //{
        //    JsonUtility.FromJsonOverwrite(json, this);
        //}
        /// <summary>
        /// A chance to clean up the loaded in frame data, like fixing the data variable in GenericFrame
        /// </summary>
        public virtual void loadedFromJson()
        {
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
            //property = _property; // TOD untested
            if (!(property.obj is Transform))   // TODO can I make this a generic function that gets called by all FrameDatas?
                Debug.LogError("Given framedata input " + property.obj + " is not type Transform");

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
    /// <summary>
    /// Used in reflection driven frames, like FieldInfo or PropertyInfo references
    /// </summary>
    public class GenericFrame : FrameData
    {
        public object data;
        /// <summary>
        /// "data string", for to and from Json
        /// </summary>
        public string ds;


        public GenericFrame(AnimatedProperty _property, float _time) : base(_time, _property)
        {
            if (property.obj is FieldInfo)
            {
                data = ((FieldInfo)property.obj).GetValue(property.animatedComponent);
            }
            else if (property.obj is PropertyInfo)
            {
                data = ((PropertyInfo)property.obj).GetValue(property.animatedComponent);
            }
            //ds = data.ToString();
            ds = JsonUtility.ToJson(data);
        }

        public override void playBack() 
        {
            if (property.obj is FieldInfo)
            {
                // TODO maybe there's a better place for this?
                //if (data == null)
                //    data = JsonUtility.FromJson(ds, ((FieldInfo)property.obj).GetValue(property.animatedComponent).GetType());

                ((FieldInfo)property.obj).SetValue(property.animatedComponent, data);
                //print("setting field: " + (FieldInfo)property.obj + " value " + data);
            }
            else if (property.obj is PropertyInfo)
            {
                // TODO maybe there's a better place for this?
                //if (data == null)
                //    data = JsonUtility.FromJson(ds, ((PropertyInfo)property.obj).GetValue(property.animatedComponent).GetType());

                ((PropertyInfo)property.obj).SetValue(property.animatedComponent, data);
                //print("setting property: " + (PropertyInfo)property.obj + " value " + data);
            }
        }

        public override void loadedFromJson()
        {
            //print("loadedFromJson");
            if (property.obj is FieldInfo)
            {
                // TODO maybe there's a better place for this?
                if (data == null)
                    data = JsonUtility.FromJson(ds, ((FieldInfo)property.obj).GetValue(property.animatedComponent).GetType());
                //print("data = " + data);

                //((FieldInfo)property.obj).SetValue(property.animatedComponent, data);
                ////print("setting field: " + (FieldInfo)property.obj + " value " + data);
            }
            else if (property.obj is PropertyInfo)
            {
                // TODO maybe there's a better place for this?
                if (data == null)
                    data = JsonUtility.FromJson(ds, ((PropertyInfo)property.obj).GetValue(property.animatedComponent).GetType());
                //print("data = " + data);

                //((PropertyInfo)property.obj).SetValue(property.animatedComponent, data);
                ////print("setting property: " + (PropertyInfo)property.obj + " value " + data);
            }
        }
    }









    int frameCount = 0; // temp

    FieldInfo fieldInfo = null;
    PropertyInfo propertyInfo = null;
    //dynamic objBase;    // Assets\PlaybackGameplay.cs(544,23): error CS0656: Missing compiler required member 'Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create'
    object objBase;   
    object objProperty;// = 


    Clip testClip;

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



        //objBase = ghosts[0];
        objBase = transform;
        //objProperty = ((typeof(objBase))objBase).direction;
        //objProperty = objBase.direction;
       // objProperty = ghosts[0].direction;
        objProperty = transform.position;

        //ref object obj = ref transform.position;
        //ref object obj = ref ghosts[0].direction;


        //foreach (PropertyInfo property in objBase.GetType().GetProperties())
        //{
        //    try
        //    {
        //        //print("PROPERTY = " + property + " VALUE = " + property.GetValue(obj));
        //        if (objProperty.Equals(property.GetValue(objBase)))
        //        {
        //            propertyInfo = property;
        //            break;
        //        }
        //    }
        //    catch (System.Exception e)
        //    {
        //        //Debug.LogError("Exception thrown: " + e); 
        //    }
        //}
        //foreach (FieldInfo field in objBase.GetType().GetFields())
        //{
        //    //print("FIELD = " + field + " VALUE = " + field.GetValue(obj));
        //    if (objProperty.Equals(field.GetValue(objBase)))
        //    {
        //        fieldInfo = field;
        //        break;
        //    }
        //}
        //print("TEST " + objProperty + " field " + fieldInfo + " property " + propertyInfo);


        string clipName = "Pacman Test";



        ////print("test type " + (ghosts[0] is GameObject));
        //testClip = new Clip("Pacman Test Ghost");
        ////testClip.animatedProperties.Add(new AnimatedProperty(ghosts[0], ghosts[0].direction, ghosts[0].gameObject, testClip));
        ////testClip.animatedProperties.Add(new AnimatedProperty(ghosts[0].transform, ghosts[0].transform.position, ghosts[0].gameObject, testClip));

        //new AnimatedProperty(ghosts[0], ghosts[0].direction, ghosts[0].gameObject, testClip);
        //new AnimatedProperty(ghosts[0].transform, ghosts[0].transform.position, ghosts[0].gameObject, testClip);
        //new AnimatedProperty(transforms[0], transforms[0].gameObject, testClip); ;


        //testClip.recordFrame(0);
        //testClip.recordFrame(1);
        //testClip.saveClip(); 


        testClip = Clip.loadClip("Pacman Test Ghost");

        testClip.animatedProperties[0].linkLoadedPropertyToObject(ghosts[0].gameObject);
        testClip.animatedProperties[1].linkLoadedPropertyToObject(ghosts[0].gameObject);
        testClip.animatedProperties[2].linkLoadedPropertyToObject(transforms[0].gameObject);


        //testClip.debugPrintClip();




        if (recordingNotPlayback)
            clip = setUpRecording(clipName);
        else
            clip = setUpPlayback(clipName);

    }

    public Clip setUpRecording(string clipName)
    {
        Clip newClip = new Clip(clipName);
        foreach (Transform tf in transforms)
        {
            newClip.addProperty(tf, tf.gameObject);
        }
        return newClip;
    }
    public Clip setUpPlayback(string clipName)
    {
        Clip newClip = Clip.loadClip(clipName);

        int index = 0;
        foreach (Transform tf in transforms)
        {
            newClip.animatedProperties[index].obj = tf;
            index++;
        }
        return newClip;
    }









    // Update is called once per frame
    void Update()
    {
        foreach (AnimatedProperty property in testClip.animatedProperties)
        {
            //print("property " + property.obj);
            property.frames[0].playBack();
        }

        //newClip.animatedProperties[0].frames[0].playBack();


        //frameCount++;
        //fieldInfo.SetValue(objBase, frameCount % 4);
        //propertyInfo.SetValue(objBase, new Vector3(frameCount % 4, frameCount % 4, frameCount % 4));

        if (activelyRecordingOrPlaying)
        {
            if (recordingNotPlayback)
                clip.recordFrame(Time.realtimeSinceStartup);
            else
            {
                // TODO a playback function
                foreach (AnimatedProperty property in clip.animatedProperties)
                {
                    //print("property " + property.obj);
                    property.frames[frameCount].playBack();
                }
                frameCount++;
                if (frameCount >= 450)
                    frameCount = 0;
            }
        }
    }

    private void OnDestroy()
    {
        if (recordingNotPlayback)
        {
            print("Saving animation data");
            clip.saveClip();
        }
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

