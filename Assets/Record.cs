using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
//using System.Runtime.Serialization;

//using System.Text.Json;
//using System.Text.Json.Serialization;


// TODO rename this class to "Record" later (causes issues with existing recordings atm)
/// <summary>
/// For recording and playing back gameplay for promotional purposes
/// TODO rename to Recordings
/// </summary>
public class Record : MonoBehaviour
{
    public bool active = true;
    [Tooltip("WARNING temporary, must be set before game starts. If not recording then yes playing.")]
    public bool recordingMode = true;

    [Tooltip("Transforms to record and playback")]
    public List<Transform> transforms;
    public List<Ghost> ghosts;

    public Clip clip;

    [Tooltip("The recording number to append to the clip when loading it in.")]
    public int fileNumber;
     




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

        /// <summary>
        /// Simple; only handles transforms atm. TODO. Other properties have to be spawned with the AnimationProperty constructor. (They automatically add themselves to the clip, fyi)
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public AnimatedProperty addProperty(object obj, GameObject gameObject)
        {
            AnimatedProperty property = new AnimatedProperty(obj, gameObject, this);
            return property;
        }

        //public static string GetFilePath(string _name)
        //{
        //    return Application.persistentDataPath + "/Clip_" + _name + ".txt";
        //}

        public static string GetFilePathLoading(string _name, int fileNumber)
        {
            return Application.persistentDataPath + "/Clip_" + _name + "_" + fileNumber + ".txt";
        }
        /// <summary>
        /// Appends a unique fileNumber to prevent overwriting
        /// </summary>
        public static string GetFilePathSaving(string _name)
        {
            int fileNumber = 0;
            while (true) 
            {
                string fileName = Application.persistentDataPath + "/Clip_" + _name + "_" + fileNumber + ".txt";
                if (!File.Exists(fileName))
                {
                    return fileName;
                    //break;
                }
                fileNumber++;  
            }

            //return "";
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

        // TODO
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

        /// <summary>
        /// Saves clip to external file, according to the clip's name
        /// </summary>
        public void saveClip()
        {
            //string filePath = GetFilePath(name);
            string filePath = GetFilePathSaving(name);

            //File.WriteAllText(filePath, "");  // overwrite file

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
        /// <summary>
        /// Loads clip from external file using the clip name
        /// </summary>
        /// <param name="_name">clip name</param>
        /// <returns></returns>
        public static Clip loadClip(string _name, int fileNumber)
        {
            Clip newClip = new Clip(_name);
            newClip.name = _name;

            string filePath = GetFilePathLoading(_name, fileNumber);

            string[] fileLines = File.ReadAllLines(filePath);

            foreach (string line in fileLines)
            {
                //print("line " + line);
                // check if it's a property or frame
                int i0 = line.IndexOf("=");
                string type = line.Substring(0, i0 + 1);
                string trimmedLine = line.Substring(i0 + 1);

                if (type.Contains(propertyString))
                {
                    // set up property
                    AnimatedProperty property = AnimatedProperty.FromJson(trimmedLine);
                    property.preLoadedProperty();

                    // Make old recordings compatible
                    property.frameTypeString = property.frameTypeString.Replace("PlaybackGameplay", "Record");

                    property.frameType = System.Type.GetType(property.frameTypeString);
                    property.type = System.Type.GetType(property.typeString); // not working?


                    newClip.animatedProperties.Add(property);
                }
                else if (type.Contains(frameString))
                {

                    List<object> objects = (List<object>)SpeedyJson.readObject(line, frameString.Length, out int ignored);  // Faster if I don't cut out a substring; less garbage collection

                    AnimatedProperty property = newClip.animatedProperties[(int)objects[0]];

                    // Is this slow?
                    FrameData frame = (FrameData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(property.frameType);


                    frame.setProperty(property);
                    frame.time = (float)objects[1];

                    frame.ds = (string)objects[2];

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



        /// <summary>
        /// In game reference to the variable/method/object etc to be recorded or animated
        /// </summary>
        public object obj;
        /// <summary>
        /// In case the Json fails to convert the obj into string
        /// </summary>
        public string objString;


        /// <summary>
        /// The type of variable or Reflection reference being used
        /// </summary>
        public System.Type type;
        public string typeString;


        /// <summary>
        /// For loading then converting hundreds of frames
        /// </summary>
        public System.Type frameType;
        /// <summary>
        /// What FrameData type exactly loaded frames need to be casted into
        /// </summary>
        public string frameTypeString;


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
            //typeString = JsonConvert.ToString(type);    // throws error

            frames = new List<FrameData>();

            frameDataFactory(this, 0);  // Will log an error if the frame type isn't supported. (Note: causes a bit of garbage collection)
        }

        /// <summary>
        /// For setting up Reflection based keyframes, like modifying variables using FieldInfo or PropertyInfo
        /// </summary>
        /// <param name="_gameObject">The base gameobject</param>
        /// <param name="animatedObject">The actual script component that propertyOrField belongs to.</param>
        /// <param name="propertyOrField">The specific value of the script/gameobject to be animated (will be grabbed via reflection)</param>
        /// <param name="_clip">parent clip</param>
        /// 
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
                    if (propertyOrField.Equals(property.GetValue(animatedComponent)))
                    {
                        obj = property;
                        break;
                    }
                }
                catch (System.Exception e)
                { }
            }
            foreach (FieldInfo field in animatedComponent.GetType().GetFields())
            {
                if (propertyOrField.Equals(field.GetValue(animatedComponent)))
                {
                    obj = field;
                    break;
                }
            }
            if (obj == null)
                Debug.LogError("Reflection failed");

            finishConstructor();
        }

        /// <summary>
        /// Set up a property to call methods during animation.
        /// </summary>
        /// <param name="script">Any component with runtime methods to call</param> // TODO test build in components
        /// <param name="methodName"></param>
        /// <param name="parameters">List of parameters, in order, to match to the function overload</param>
        /// <param name="_clip">parent clip</param>
        public AnimatedProperty(Component script, string methodName, object[] parameters, Clip _clip)
        {
            obj = getMethod(script, methodName, parameters);

            if (obj == null)
                Debug.LogError("ERROR: Unable to find method " + methodName + " with " + parameters.Length + " parameters in script " + script);


            startConstructor(obj, script.gameObject, _clip);

            animatedComponent = script;
            animatedComponentString = script.ToString();    // ToString()?

            finishConstructor();
        }

        /// <summary>
        /// was kinda dumb to pull this out as a method. oops
        /// </summary>
        MethodInfo getMethod(Component script, string methodName, object[] parameters)
        {
            MethodInfo result = null;
            foreach (MethodInfo method in script.GetType().GetRuntimeMethods())
            {
                if (!methodName.Equals(method.Name))
                    continue;

                int pi = 0;
                bool match = true;
                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    if (parameter.ParameterType != parameters[pi].GetType())
                        match = false;

                    pi++;
                }
                if (match)
                {
                    result = method;
                    break;
                }
            }
            return result;
        }




        // TODO add helper functions that can re-find the game object based on a saved path string etc

        /// <summary>
        /// Also goes through and readies the loaded in frames
        /// </summary>
        /// <param name="_gameObject"></param>
        public void linkLoadedPropertyToObject(GameObject _gameObject)
        {
            gameObject = _gameObject;

            // find links
            foreach(Component component in gameObject.GetComponents(typeof(Component)))
            {
                if (component.ToString().Equals(animatedComponentString))
                {
                    animatedComponent = component;

                    //if (type == typeof(PropertyInfo))
                    if (typeString.Equals("System.Reflection.MonoProperty"))    // This is probably really slow but I can't get the type comparison to work >__>
                    {
                        foreach (PropertyInfo property in animatedComponent.GetType().GetProperties())
                        {
                            try
                            {
                                if (property.ToString().Equals(objString))
                                {
                                    obj = property;
                                    break;
                                }
                            }
                            catch (System.Exception e)
                            { } // TODO only catch 'item is depreciated' exceptions
                        }
                    }
                    //else if (type == typeof(FieldInfo))
                    else if (typeString.Equals("System.Reflection.MonoField"))
                    {
                        foreach (FieldInfo field in animatedComponent.GetType().GetFields())
                        {
                            if (field.ToString().Equals(objString))
                            {
                                obj = field;
                                break;
                            }
                        }
                    }
                    //else if (type == typeof(MethodInfo))
                    else if (typeString.Equals("System.Reflection.MonoMethod"))
                    {
                        foreach (MethodInfo method in component.GetType().GetRuntimeMethods())
                        {
                            if (method.ToString().Equals(objString))
                            {
                                obj = method;
                                break;
                            }

                        }
                    }


                    break;
                } else if (component.ToString().Equals(objString))
                {
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
            if (frame == null)
                return;

            frames.Add(frame);
            if (frameTypeString == null)
            {
                frameTypeString = frame.GetType().ToString();
            }
        }

        /// <summary>
        /// This adds a method call frame to the clip, letting you call arbitrary code.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="methodParameters"></param>
        public void addNewFrame(float time, object[] methodParameters)
        {
            FrameData frame = new MethodFrame(methodParameters, this, time);
            frames.Add(frame);
            
            if (frameTypeString == null)
            {
                frameTypeString = frame.GetType().ToString();
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
                frame = new TransformFrame(this, time);
            else if (obj is FieldInfo)
                frame = new GenericFrame(this, time);
            else if (obj is PropertyInfo)
                frame = new GenericFrame(this, time);
            else if (obj is MethodInfo) { }
            // Methods can't be live recorded (I think), their frames are added via code
            else
                Debug.LogError("unknown frameData type attempting to be created for: " + obj + " of type: " + obj.GetType());

            return frame;
        }
        ///// <summary>
        ///// Yes I hate that there are 2 factories >__> This one's needed for loading frames in before properties have been linked to their objects yet
        ///// </summary>
        //public static FrameData frameDataFactory(AnimatedProperty parent, float time, string frameTypeStr)
        //{
        //    FrameData frame = null;

        //    if (frameTypeStr.Equals("PlaybackGameplay+TransformFrame"))
        //        frame = new TransformFrame(parent, time);
        //    else if (frameTypeStr.Equals("PlaybackGameplay+GenericFrame"))
        //        frame = new GenericFrame(parent, time);
        //    //else if (frameTypeStr.Equals("PlaybackGameplay+MethodFrame"))
        //    //    frame = new MethodFrame(parent, time);    // TODO
        //    else
        //        Debug.LogError("unknown frameData type attempting to be created for: " + frameTypeStr);

        //    return frame;
        //}


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






    /// <summary>
    /// Base class for all animation frames holding arbitrary data
    /// </summary>
    public abstract class FrameData
    {
        public int ID;
        public float time;
        public string ds;
        protected AnimatedProperty property;    // protected so that the Json converters don't see it

        public FrameData(float _time, AnimatedProperty _property)
        {
            time = _time;
            ID = _property.ID;
            property = _property;
        }

        public abstract void playBack();
        //public virtual void playBack() { }
        
        public virtual string ToJson()
        {
            //return JsonUtility.ToJson(this);
            //List<object> frame = new List<object>();
            //frame.Add(ID);
            //frame.Add(time);
            //frame.Add()

            return SpeedyJson.toString(new List<object> { ID, time, ds });
        }

        /// <summary>
        /// A chance to clean up the loaded in frame data, like fixing the data variable in GenericFrame
        /// </summary>
        public virtual void loadedFromJson()
        {
        }

        public void setProperty(AnimatedProperty _property)
        {
            property = _property;
            ID = property.ID;
        }
    }
    /// <summary>
    /// Frame for transform data
    /// </summary>
    public class TransformFrame : FrameData
    {

        public Vector3 lPos;
        public Vector3 lScal;
        public Quaternion lRot;

        public TransformFrame(AnimatedProperty _property, float _time) : base(_time, _property)
        {
            if (!(property.obj is Transform))   // TODO can I make this a generic function that gets called by all FrameDatas?
                Debug.LogError("Given framedata input " + property.obj + " is not type Transform");

            Transform obj = (Transform)property.obj;

            lPos = obj.localPosition;
            lScal = obj.localScale;
            lRot = obj.localRotation;

            ds = SpeedyJson.toString(new List<object> { lPos, lScal, lRot.eulerAngles });
        }

        public override void playBack()
        {
            Transform obj = (Transform)property.obj;

            obj.localPosition = lPos;
            obj.localScale = lScal;
            obj.localRotation = lRot;
        }

        public override void loadedFromJson()
        {
            List<object> objs = (List<object>)SpeedyJson.readObject(ds);
            Transform obj = (Transform)property.obj;

            lPos = (Vector3)objs[0];
            lScal = (Vector3)objs[1];
            lRot = Quaternion.Euler((Vector3)objs[2]);

            //print("transform frame DS " + ds + " pos " + objs[0].ToString());
        }
    }
    /// <summary>
    /// Uses reflection to let you animate arbitrary variables of components and scripts
    /// </summary>
    public class GenericFrame : FrameData
    {
        public object data;
        /// <summary>
        /// "data string", for to and from Json
        /// </summary>
        //public string ds;


        public GenericFrame(AnimatedProperty _property, float _time) : base(_time, _property)
        {
            // TODO check if inputs are valid
            if (property.obj is FieldInfo)
            {
                data = ((FieldInfo)property.obj).GetValue(property.animatedComponent);
            }
            else if (property.obj is PropertyInfo)
            {
                data = ((PropertyInfo)property.obj).GetValue(property.animatedComponent);
            }
            //ds = JsonUtility.ToJson(data);  // idk why this works, while converting the whole object leaves out the data part, but it works.
            //ds = JsonConvert.SerializeObject(data, Formatting.None, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });   // otherwise it throws a "self referencing loop detected" exception and dies. Unity Json didn't do this >__>  // Too slow

            ds = SpeedyJson.toString(data);
        }

        public override void playBack() 
        {
            if (property.obj is FieldInfo)
            {
                ((FieldInfo)property.obj).SetValue(property.animatedComponent, data);
            }
            else if (property.obj is PropertyInfo)
            {
                ((PropertyInfo)property.obj).SetValue(property.animatedComponent, data);
            }
        }

        public override void loadedFromJson()
        {
            //if (property.obj is FieldInfo)
            //{
            //    if (data == null)
            //        //data = JsonUtility.FromJson(ds, ((FieldInfo)property.obj).GetValue(property.animatedComponent).GetType());
            //        data = JsonConvert.DeserializeObject(ds, ((FieldInfo)property.obj).GetValue(property.animatedComponent).GetType());
            //}
            //else if (property.obj is PropertyInfo)
            //{
            //    if (data == null)
            //        //data = JsonUtility.FromJson(ds, ((PropertyInfo)property.obj).GetValue(property.animatedComponent).GetType());
            //        data = JsonConvert.DeserializeObject(ds, ((PropertyInfo)property.obj).GetValue(property.animatedComponent).GetType());
            //}
            if (data == null)
            {
                data = SpeedyJson.readObject(ds);

                if (data is int)
                {
                    // Check if this int should be an enum >__> Stupid casting rules...
                    System.Type type;
                    if (property.obj is FieldInfo)
                        type = ((FieldInfo)property.obj).GetValue(property.animatedComponent).GetType();
                    else
                        type = ((PropertyInfo)property.obj).GetValue(property.animatedComponent).GetType();

                    //if (type == typeof(System.Enum))  // doesn't work
                    if (type != typeof(int))
                        data = ((IList)System.Enum.GetValues(type))[(int)data]; // The only way to cast ints to unknown enums I think. lmao. https://stackoverflow.com/questions/23596490/cannot-apply-indexing-with-to-an-expression-of-type-system-array-with-c-sha#comment88091793_23597770
                    //print("Data " + data);
                }
            }
        }
    }
    /// <summary>
    /// Lets you call methods in recorded animations! (Note: Method calls can't be recorded (I think), add these via code)
    /// </summary>
    [System.Serializable]
    public class MethodFrame : FrameData
    {
        /// <summary>
        /// "parameterLength"
        /// </summary>
        public object[] parameters;
        /// <summary>
        /// "paramtersString"
        /// </summary>
        //public string ps;

        public MethodFrame(object[] _parameters, AnimatedProperty _property, float _time) : base(_time, _property)
        {
            parameters = _parameters;
            //ps = JsonConvert.SerializeObject(parameters);   // Had to switch to a new json library here because unity json just *would not* export a list of objects
            ds = JsonConvert.SerializeObject(parameters);   // Had to switch to a new json library here because unity json just *would not* export a list of objects
        }

        public override void playBack()
        {
            ((MethodInfo)property.obj).Invoke(property.animatedComponent, parameters);
        }

        public override void loadedFromJson()
        {
            //parameters = JsonConvert.DeserializeObject<object[]>(ps);
            parameters = JsonConvert.DeserializeObject<object[]>(ds);

            // So this netwon json library likes to pull out ints as int64. That screws up functions expecting an int, ie int32
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].GetType() == typeof(System.Int64))
                    parameters[i] = System.Convert.ChangeType(parameters[i], typeof(int));
            }
        }
    }









    int frameCount = 0; // temp

    Clip testClip;

    // Start is called before the first frame update
    void Start()
    {

        string clipName = "PacmanRecording";

        //print("Date time:" + System.DateTime.Now);


        //string input = "L[S:\"test \\\" string\",i:42,F:1.11,B:1,V3:1|2.222|3,]";
        //print("input " + input);
        //object output = SpeedyJson.readObject(input);
        //print("output " + output);
        //foreach (object obj in (List<object>)output)
        //{
        //    if (obj != null)
        //    print("obj " + obj + " type " + obj.GetType());
        //}
        //print("Objects to string: " + SpeedyJson.toString(output));


        //ghosts[0].direction = System.Convert.ChangeType(1, enum);
        //ghosts[0].direction = (System.Enum)1;
        //ghosts[0].direction = (System.Enum)System.Convert.ChangeType(1, typeof(System.Enum));


        //setTestRecording();

        //setTestPlayback();

        if (recordingMode)
            clip = setUpRecording(clipName);
        else
            clip = setUpPlayback(clipName);

    }


    /// <summary>
    /// A simple converter for a short list of primitive and unity data types, for saving/loading animation frame data. (Since Unity json just can't do object[] arrays and some object types, and newtonSoft jason is slow as hell. (They're both slow really; My loader, using both Jsons, takes 4.3 seconds to load 70 seconds of recorded pacman gameplay))
    /// </summary>
    // I decided to do it this way instead of using a binary serializer because those seem to be inherently insecure if your input is uncontrolled; This is safer for users. 
    public class SpeedyJson
    {
        /// <summary>
        /// Separates objects; Required after every object!
        /// </summary>
        const string separator = ",";

        /// <summary>
        /// Separates sub properties and fields of a class, like individual values in a vector3 or Transform
        /// </summary>
        const string separatorProperty = "|";
        /// <summary>
        /// Separates groups of properties, like 3 floats making a vector 3 in a transform
        /// </summary>
        //const string separatorProperty2 = " ";

        // types to convert
        const string listStart = "L[";
        const string listEnd = "]";

        // TODO: An array reader (to save on garbage collection from List to Array conversions)

        const string stringStart = "S:\"";
        const string stringEnd = "\"";

        const string intStart = "i:";

        const string floatStart = "F:";

        const string boolStart = "B:";

        const string vector3Start = "V3:";

        //const string transformStart = "TF:";

        /// <summary>
        /// Returns a blank string and logs error to console if an unknown type is found
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string toString(object obj)
        {
            string output = null;     // TODO: Concatenating strings spawns a new string every time, causing garbage collecting. Is there a way around this?

            if (obj is List<object>)
            {
                output += listStart;

                foreach(object o in (List<object>)obj)
                {
                    output += toString(o);
                }
                output += listEnd;
            }
            else if (obj is string)
            {
                List<char> str = new List<char>();
                foreach (char c in (string)obj)
                {
                    if (c == '"')
                        str.Add('\\');
                    str.Add(c);
                }
                output += stringStart + new string(str.ToArray()) + stringEnd + separator;
            }
            else if (obj is int || obj is System.Enum)
            {
                output += intStart + (int)obj + separator;
            }
            else if (obj is float)
            {
                output += floatStart + (float)obj + separator;
            }
            else if (obj is bool)
            {
                int bo = 0;
                if ((bool)obj)
                    bo = 1;
                output += boolStart + (int)bo + separator;
            }
            else if (obj is Vector3)
            {
                Vector3 vec = (Vector3)obj;
                output += vector3Start + vec.x + separatorProperty + vec.y + separatorProperty + vec.z + separator;
            }
            else
            {
                Debug.LogError("unrecognized item " + obj + " of type " + obj.GetType());
            }

            return output;
        }


        // TODO: This is still slow. Look into getting rid of all string spawning and just using static char[] arrays.
        // -Will need to rewrite a bunch of functions, especially System.Convert.ChangeType(str, typeof(int));. How to do that with chars?
        // -chars to numbers https://stackoverflow.com/a/21587247 and concatenating them, just multiply by 10 and add https://stackoverflow.com/a/26853517
        /// <summary>
        /// Takes a pseudo json string, returns an object (or null plus console errors). See the const strings of this class for supported types
        /// </summary>
        /// <param name="Json"></param>
        /// <returns></returns>
        public static object readObject(string Json)
        {
            return readObject(Json, 0, out int unusedVar);
        }
        public static object readObject(string Json, int objectStart, out int objectEnd)
        {
            //print("Json string " + Json);
            object result = null;
            //objectEnd = objectStart + 1;
            objectEnd = Json.Length;

            if (objectStart >= Json.Length)
            { }
            else if (SS(Json, objectStart, listStart))  // List found
            {
                List<object> objects = new List<object>();
                int i = objectStart + listStart.Length;

                while (i < Json.Length)
                {
                    if (SS(Json, i, listEnd))
                    {
                        objectEnd = i + listEnd.Length;
                        break;
                    }
                    else if (SS(Json, i, separator))  // honestly the commas are just there for human readability; they aren't required
                    {
                        i++;
                        continue;
                    }

                    int objectEnd1;
                    object obj = readObject(Json, i, out objectEnd1);

                    objects.Add(obj);
                    i = objectEnd1;
                }

                result = objects;
            }
            else if (SS(Json, objectStart, stringStart))    // String found
            {
                int strStart = objectStart + stringStart.Length;
                int i = strStart;
                List<char> str = new List<char>();

                while (i < Json.Length)
                {
                    // Have to be careful of delimited quotes
                    if (Json[i] == '\\')
                    {
                        str.Add(Json[i + 1]);
                        i += 2;
                        continue;
                    }

                    if (SS(Json, i, stringEnd))
                    {
                        objectEnd = i + stringEnd.Length;
                        result = new string(str.ToArray()); // Kinda garbage collect-y, but I can't think of a better way
                        break;
                    }

                    str.Add(Json[i]);
                    i++;
                }
            }
            else if (SS(Json, objectStart, intStart))    // Int found
            {
                objectEnd = Json.IndexOf(separator, objectStart) + 1;
                string str = Json.Substring(objectStart + intStart.Length, objectEnd - objectStart - 1 - intStart.Length);
                
                result = System.Convert.ChangeType(str, typeof(int));
            }
            else if (SS(Json, objectStart, floatStart))    // Float found
            {
                objectEnd = Json.IndexOf(separator, objectStart) + 1;
                string str = Json.Substring(objectStart + floatStart.Length, objectEnd - objectStart - 1 - floatStart.Length);

                //print("str " + str);
                result = System.Convert.ChangeType(str, typeof(float));
            }
            else if (SS(Json, objectStart, boolStart))    // Bool found
            {
                objectEnd = Json.IndexOf(separator, objectStart) + 1;
                string str = Json.Substring(objectStart + boolStart.Length, objectEnd - objectStart - 1 - boolStart.Length);

                //print("str " + str);
                result = System.Convert.ToBoolean(System.Convert.ChangeType(str, typeof(int)));
            }            
            else if (SS(Json, objectStart, vector3Start))    // Vector3 found
            {
                objectEnd = Json.IndexOf(separator, objectStart) + 1;
                string str = Json.Substring(objectStart + vector3Start.Length, objectEnd - objectStart - 1 - vector3Start.Length);

                //print("str " + str);
                result = stringToVector3(str);
            }
            //else if (SS(Json, objectStart, transformStart))    // Transform found
            //{
            //    objectEnd = Json.IndexOf(separator, objectStart) + 1;
            //    int vec3End = Json.IndexOf(separatorProperty2, objectStart);
            //    string str = Json.Substring(objectStart + transformStart.Length, vec3End - objectStart - transformStart.Length);
            //    print("str " + str);
            //    Vector3 pos = stringToVector3(str);

            //    int vec3Start = vec3End;
            //    vec3End = Json.IndexOf(separatorProperty2, vec3Start + 1);
            //    str = Json.Substring(vec3Start + 1, vec3End - vec3Start);
            //    print("str " + str);
            //    Vector3 scale = stringToVector3(str);

            //    print(" " + vec3End + " " + (objectEnd - vec3End) + " end " + objectEnd);
            //    str = Json.Substring(vec3End + 1, objectEnd - vec3End - 2);
            //    print("str " + str);
            //    Vector3 rot = stringToVector3(str);


            //    print("TF " + new Transform(pos, scale, rot));  // Ah shit you can't make new transforms...
            //    //print("str " + str);
            //    result = stringToVector3(str);
            //}
            else           // object not recognized
            {
                objectEnd = Json.IndexOf(separator, objectStart) + 1;
                if (objectEnd <= 0)
                {
                    objectEnd = Json.Length;
                    return result;
                }

                Debug.LogError("Object at " + objectStart + " not recognized; string \"" + Json.Substring(objectStart, objectEnd - objectStart) + "\" in json string \"" + Json + "\".");
            }



            return result;
        }

        public static Vector3 stringToVector3(string Json)
        {
            int next0 = 0;
            int next1 = Json.IndexOf(separatorProperty);
            string str = Json.Substring(next0, next1);
            //print("str " + str);
            float one = (float)System.Convert.ChangeType(str, typeof(float));

            next0 = Json.IndexOf(separatorProperty, next1+1);
            //str = Json.Substring(next1 + 1, next0 - 2);
            //print("next0 " + next0 + " next1 " + next1);
            str = Json.Substring(next1 + 1, next0 - next1 - 1);
            //print("str " + str);
            float two = (float)System.Convert.ChangeType(str, typeof(float));

            //print("next0 " + next0 + " next1 " + next1);
            str = Json.Substring(next0 + 1, Json.Length - (next0 + 1));
            //print("str " + str);
            float three = (float)System.Convert.ChangeType(str, typeof(float));

            return new Vector3(one, two, three);
        }

        /// <summary>
        /// SubstringSearch
        /// </summary>
        /// <param name="sourceString"></param>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public static bool SS(string sourceString, int startingChar, string searchString)
        {
            int i = 0;
            foreach(char c in searchString)
            {
                if (c != sourceString[startingChar + i++])
                    return false;
            }

            return true;
        }
    }




    /// <summary>
    /// Like a unit test, for regression testing and testing new things 
    /// </summary>
    public void setTestRecording()
    {
        testClip = new Clip("Pacman Test Ghost");

        new AnimatedProperty(ghosts[0], ghosts[0].direction, ghosts[0].gameObject, testClip);
        new AnimatedProperty(ghosts[0].transform, ghosts[0].transform.position, ghosts[0].gameObject, testClip);
        new AnimatedProperty(transforms[0], transforms[0].gameObject, testClip); ;

        object[] parameters = { "test string to be called by function!", 42 };
        AnimatedProperty testFunctionProperty = new AnimatedProperty(ghosts[0], "testFunction", parameters, testClip);
        testFunctionProperty.addNewFrame(1, parameters);


        testClip.recordFrame(0);
        testClip.recordFrame(1);
        testClip.saveClip();
    }
    public void setTestPlayback()
    {
        testClip = Clip.loadClip("Pacman Test Ghost", fileNumber);

        testClip.animatedProperties[0].linkLoadedPropertyToObject(ghosts[0].gameObject);
        testClip.animatedProperties[1].linkLoadedPropertyToObject(ghosts[0].gameObject);
        testClip.animatedProperties[2].linkLoadedPropertyToObject(transforms[0].gameObject);
        testClip.animatedProperties[3].linkLoadedPropertyToObject(ghosts[0].gameObject);

        //testClip.debugPrintClip();
    }


    public Clip setUpRecording(string clipName)
    {
        Clip newClip = new Clip(clipName);
        foreach (Transform tf in transforms)
        {
            newClip.addProperty(tf, tf.gameObject);
        }

        foreach (Ghost ghost in ghosts)
        {
            // TODO these gameobject refs seem redundant..?
            newClip.addProperty(ghost.transform, ghost.gameObject);
            new AnimatedProperty(ghost, ghost.direction, ghost.gameObject, newClip);
            new AnimatedProperty(ghost, ghost.state, ghost.gameObject, newClip);
            new AnimatedProperty(ghost, ghost.target, ghost.gameObject, newClip);
            new AnimatedProperty(ghost, ghost.bodySpinAngle, ghost.gameObject, newClip);

        }

        Game game = Game.instance;
        new AnimatedProperty(game, game.frightened, game.gameObject, newClip);
        new AnimatedProperty(game, game.paused, game.gameObject, newClip);

        return newClip;
    }
    public Clip setUpPlayback(string clipName)
    {
        float startTime = Time.realtimeSinceStartup;
        print("Starting loading");
        Clip newClip = Clip.loadClip(clipName, fileNumber);
        print("Clip generation time " + (Time.realtimeSinceStartup - startTime).ToString("F6"));
        startTime = Time.realtimeSinceStartup;

        int i = 0;
        foreach (Transform tf in transforms)
        {
            newClip.animatedProperties[i++].linkLoadedPropertyToObject(tf.gameObject);
        }

        foreach (Ghost ghost in ghosts)
        {
            newClip.animatedProperties[i++].linkLoadedPropertyToObject(ghost.gameObject);
            newClip.animatedProperties[i++].linkLoadedPropertyToObject(ghost.gameObject);
            newClip.animatedProperties[i++].linkLoadedPropertyToObject(ghost.gameObject);
            newClip.animatedProperties[i++].linkLoadedPropertyToObject(ghost.gameObject);
            newClip.animatedProperties[i++].linkLoadedPropertyToObject(ghost.gameObject);
        }

        newClip.animatedProperties[i++].linkLoadedPropertyToObject(Game.instance.gameObject);
        newClip.animatedProperties[i++].linkLoadedPropertyToObject(Game.instance.gameObject);
        print("property conversion time " + (Time.realtimeSinceStartup - startTime).ToString("F6"));

        return newClip;
    }









    // Update is called once per frame
    void Update()
    {
        //foreach (AnimatedProperty property in testClip.animatedProperties)
        //{
        //    property.frames[0].playBack();
        //}


        //newClip.animatedProperties[0].frames[0].playBack();


        //frameCount++;
        //fieldInfo.SetValue(objBase, frameCount % 4);
        //propertyInfo.SetValue(objBase, new Vector3(frameCount % 4, frameCount % 4, frameCount % 4));

        if (OVRInput.Get(OVRInput.Button.Three) && OVRInput.Get(OVRInput.Button.Four))
        {
            print("Saving animation data");
            clip.saveClip();
        }

        if (active)
        {
            if (recordingMode)
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
                //if (frameCount >= 900)
                //if (frameCount >= 1600)
                //    frameCount = 0;
            }
        }
    }

    private void OnDestroy()
    {
        if (recordingMode)
        {
            print("Saving animation data");
            clip.saveClip();
        }
    }

    public void testReflection()
    {
        {
            //var obj = ghosts[0];
            var obj = ghosts[0];
            print("Breaking down: " + obj);
            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                try
                {
                    print("PROPERTY = " + property + " VALUE = " + property.GetValue(obj));
                    print("JSON " + JsonUtility.ToJson(property));
                }
                catch (System.Exception e)
                {
                    //Debug.LogError("Exception thrown: " + e);     // just spits out "depreciated property!" a bunch
                }
            }
            foreach (FieldInfo field in obj.GetType().GetFields())
            {
                print("FIELD = " + field + " VALUE = " + field.GetValue(obj));
                print("JSON " + JsonUtility.ToJson(field));
            }
        }
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
