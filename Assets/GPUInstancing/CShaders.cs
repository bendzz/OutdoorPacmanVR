using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.InteropServices;


public class CShaders : MonoBehaviour
{

}



public class CSKernel
{
    public static int ThreadCount1D = 64; // Corresponds to the #define in jiggleStructDefs

    public ComputeShader computeShader;

    //public int threadStart = 1;    // Sets corresponding values in the CS before the kernel dispatches. (Note: does nothing if the kernel doesn't *use* those values!)
    // TODO: Make threadStart line up with the buffer starting offsets in Jiggle.cs. A global constant?
    public int threadStart = 2;    // Sets corresponding values in the CS before the kernel dispatches. (Note: does nothing if the kernel doesn't *use* those values!)
    public int threadCount;

    public string name;
    public int kernel;
    public List<CSBuffer> buffers;

    /// <summary>
    /// Runs computeShader.FindKernel(name);
    /// </summary>
    /// <param name="kernelName"></param>
    public CSKernel(string kernelName, ComputeShader _computeShader)
    {
        computeShader = _computeShader;
        name = kernelName;
        kernel = computeShader.FindKernel(name);
    }

    /// <summary>
    /// (Warning: Parameter REPLACES current list.) Links the list of buffers to the kernel. threadCount is set to the first buffers count size (but it can be changed later)
    /// </summary>
    public void setBuffers(List<CSBuffer> _buffers)
    {
        buffers = _buffers;
        setBuffers();
    }
    /// <summary>
    /// Links the list of buffers to the kernel. threadCount is set to the first buffers count size (but it can be changed later)
    /// </summary>
    public void setBuffers()
    {
        foreach (CSBuffer buffer in buffers)
        {
            //print("kernel " + kernel + " buffer.name " + buffer.name + " buffer.buffer " + buffer.buffer);
            computeShader.SetBuffer(kernel, buffer.name, buffer.buffer);
        }
        threadCount = buffers[0].buffer.count;
    }
    /// <summary>
    /// runs setBuffer() for the given buffer. Note: Does not add it to the list of buffers
    /// </summary>
    /// <param name="buffer"></param>
    public void setBuffer(CSBuffer buffer)
    {
        computeShader.SetBuffer(kernel, buffer.name, buffer.buffer);
    }

    /// <summary>
    /// Also sets threadStart and threadCount on the compute shader. Threadcount by default is the count of the first buffer given
    /// </summary>
    public void dispatch()
    {
        dispatch(threadStart, threadCount);
    }
    public void dispatch(int tempThreadStart, int tempThreadCount)
    {
        int workGroupCount = tempThreadCount / ThreadCount1D + 1;

        computeShader.SetInt("threadStart", tempThreadStart);
        computeShader.SetInt("threadCount", tempThreadCount);
        //try {
        computeShader.Dispatch(kernel, workGroupCount, 1, 1);
        //} catch (System.Exception e) { 
        //    Debug.LogError("Kernel " + name + " dispatch exception " +  e.ToString());
        //}
    }

    /// <summary>
    /// Use this if the kernel is running an unknown number of times on an append/consume buffer. It'll dispatchIndirect using the size of the append buffer in the first buffers list slot
    /// </summary>
    public void dispatchAppendBuffer()
    {
        CSBuffer appendBuffer = buffers[0];
        if (appendBuffer.argumentsBuffer == null)
        {
            // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/exceptions/creating-and-throwing-exceptions this says don't use System.Exception, but their example name doesn't work so whatever
            throw new System.Exception("The append buffer (at buffers[0]) .argumentsBuffer is null. Try initializing it as an appendBuffer first.");
        }

        ComputeBuffer.CopyCount(appendBuffer.buffer, appendBuffer.argumentsBuffer, 0);
        //Jiggle.instance.computeShader.DispatchIndirect(kernel, appendBuffer.argumentsBuffer, 0);
    }
}

public abstract class CSBuffer
{
    public ComputeBuffer buffer;
    public string name;
    public ComputeBufferType computeBufferType;

    /// <summary>
    /// Set to true to get warnings for possible errors, like say, resizing buffers detaching them from kernels
    /// </summary>
    public static bool verboseDebug = false;

    /// <summary>
    /// Only used for append/Consume type buffers. Null otherwise
    /// </summary>
    public ComputeBuffer argumentsBuffer;

    public CSBuffer(string _name)
    {
        name = _name;
    }

    ~CSBuffer()
    {
        buffer.Release();
    }
}

// So that the WIP list of data can always be nicely linked to the buffer in code
// https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/generic-classes
public class CSBuffer<T> : CSBuffer where T : struct
{
    public List<T> list;
    // TODO: Update one section of a buffer
    // TODO eventually: Update multiple (possibly) overlapping buffer sections. Complex

    public CSBuffer(string _name) : base(_name)
    {
        name = _name;
        list = new List<T>();
        //list.Add(new T());      // First position in buffer is kept empty, as a null

        computeBufferType = ComputeBufferType.Default;
    }

    //public ComputeBufferType computeBufferType;




    /// <summary>
    /// Creates a whole new buffer from list. WARNING: If list is a different length, the current buffer will be deleted and replaced- And all the connected kernels will lose it!
    /// </summary>
    public void fillBuffer()
    {
        bool log = verboseDebug;    // You can set this to true manually in code to see buffer creation details
        if (buffer == null)
        {
            log = true;
            buffer = new ComputeBuffer(list.Count, Marshal.SizeOf(list[0]), computeBufferType);
        }
        else
        {
            if (buffer.count != list.Count)
            {
                buffer.Release();
                buffer = new ComputeBuffer(list.Count, Marshal.SizeOf(list[0]), computeBufferType);
                if (verboseDebug) Debug.LogWarning("Warning: Buffer " + name + " was resized, and thus deleted/remade. " +
                    "All kernel links have been broken, run CSKernel.SetBuffer(buffer) to fix them.");
            }
        }
        buffer.SetData(list);
        if (log) logDetails();
    }
    public void logDetails()
    {
        int stride = 16;
        int struckSize = Marshal.SizeOf(list[0]);
        int remainder = struckSize % stride;
        string strideWarning = "";
        if (remainder != 0) strideWarning = "Warning: Not divisible by the " + stride + " byte stride, add "
                + (stride - remainder) + " to match the stride.";
        //print("Buffer " + name + " created, contains " + buffer.count + " structs of size " + struckSize + " bytes (Totalling " + ((float)(buffer.count * struckSize) / 1000) + " KB). " + strideWarning);
        Debug.Log("Buffer " + name + " created, contains " + buffer.count + " structs of size " + struckSize + " bytes (Totalling " + ((float)(buffer.count * struckSize) / 1000) + " KB). " + strideWarning);

    }
    /// <summary>
    /// Just creates a blank list then buffer
    /// </summary>
    /// <param name="count"></param>
    public void fillEmptyBuffer(int count)  // untested
    {
        list = new List<T>(new T[count]);
        //print("list " + list + " list.count " + list.Count);
        fillBuffer();
    }

    /// <summary>
    /// Loads the data from the GPU buffer back into the list. WARNING: Super slow!
    /// </summary>
    public void GetData()
    {
        T[] array = new T[buffer.count];
        buffer.GetData(array);
        list = new List<T>(array);
    }

    /// <summary>
    /// Changes the buffer type to append/consume, sets up argumentsBuffer, sizes the append buffer
    /// </summary>
    public void initializeAppendBuffer(int maxSize)
    {
        computeBufferType = ComputeBufferType.Counter;
        fillEmptyBuffer(maxSize);

        /*
        argumentsBuffer = new CSBuffer<int>("patchPairsArguments");
        //// https://cheneyshen.com/directcompute-tutorial-for-unity-append-buffers/
        //// https://cheneyshen.com/directcompute-tutorial-for-unity-counter-buffers/
        //patchPairsArguments.computeBufferType = ComputeBufferType.Raw;
        argumentsBuffer.computeBufferType = ComputeBufferType.IndirectArguments;    // Why this instead of .Raw? Does it matter?
                                                                                    // Set Y and Z workgroup count to 1, so that it actually runs some workgroups. https://docs.unity3d.com/ScriptReference/ComputeShader.DispatchIndirect.html
        argumentsBuffer.list = new List<int>() { 1, 1, 1, 1 };
        argumentsBuffer.fillBuffer();
        */
        List<int> argumentsList = new List<int>() { 1, 1, 1, 1 };
        if (argumentsBuffer != null) argumentsBuffer.Release();
        // argumentsBuffer.fillBuffer()
        //argumentsBuffer = new ComputeBuffer(argumentsList.Count, Marshal.SizeOf(argumentsList[0]), ComputeBufferType.IndirectArguments);
        argumentsBuffer = new ComputeBuffer(argumentsList.Count, Marshal.SizeOf((int)1), ComputeBufferType.IndirectArguments);
        argumentsBuffer.SetData(argumentsList);
    }

    /// <summary>
    /// Shows up as "Append/Consume buffer (name) counts: # , # , # , #" in debug log. WARNING: Slow! Runs .GetData on the arguments buffer.
    /// </summary>
    public void debugPrintAppendBufferCount()
    {
        if (argumentsBuffer != null)
        {
            ComputeBuffer.CopyCount(buffer, argumentsBuffer, 0);

            // argumentsBuffer.GetData()
            int[] array = new int[argumentsBuffer.count];
            argumentsBuffer.GetData(array);
            List<int> argsList = new List<int>(array);

            Debug.Log("Append/Consume buffer " + name + " counts: " + string.Join(" , ", argsList));
        }
        else
        {
            Debug.LogWarning("Append/Consume buffer " + name + " had a null argumentsBuffer! debugPrint failed");
        }
    }

    /// <summary>
    /// Call this before a kernel attempts to fill this appendBuffer, to reset the count to 0. (Also sets the max buffer count in shader)
    /// </summary>
    public void resetAppendBuffer()
    {
        //Jiggle.instance.computeShader.SetInt("appendBufferLimit", buffer.count);
        buffer.SetCounterValue(0);
    }
}