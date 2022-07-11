using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUInstancing : MonoBehaviour
{
    public ComputeShader CS;

    public Transform[] botSkins;
    public Shader botShader; 

    public static class Bots
    {

        public struct bot
        {
            public Vector3 pos;
            //public Quaternion rot;
            public Vector3 scale;
            public Vector3 info;
        }

        public struct Submesh
        {
            public int verticesStart;
            public int verticesCount;

            public int trianglesStart;
            public int trianglesCount;
        }

        public static CSBuffer<bot> bots;
        //public static List<CSBuffer<bot>> botLists;
        /// <summary>
        /// How many of each submesh there are. (Make sure it lines up with the bots buffer)
        /// </summary>
        public static List<int> submeshInstances;

        // These 2 lists have to stay in sync!
        public static CSBuffer<Submesh> submeshes;
        //public static List<Material> submeshMaterials;
        // Add buffers as needed for more mesh info
        public static CSBuffer<Vector3> meshVertices;
        public static CSBuffer<int> meshTriangles;

        public static Shader botShader;
        public static Material botMaterial;

        public static ComputeShader CS;
        public static CSKernel botTick;

        /// <summary>
        /// Run this before using anything
        /// </summary>
        public static void setShaders(ComputeShader CS_, Shader botShader_)
        {
            CS = CS_;
            botShader = botShader_;
        }

        public static void setupBots(Transform[] botSkins)
        {
            // materials
            submeshes = new CSBuffer<Submesh>("botMeshes");
            meshVertices = new CSBuffer<Vector3>("meshVertices");
            meshTriangles = new CSBuffer<int>("meshTriangles");

            //submeshes.list = new List<Submesh>();
            //submeshMaterials = new List<Material>();

            foreach (Transform TF in botSkins)
            {
                Mesh mesh = null;

                if (TF.GetComponent<MeshFilter>() != null) mesh = TF.GetComponent<MeshFilter>().mesh;
                if (TF.GetComponent<SkinnedMeshRenderer>() != null) mesh = TF.GetComponent<SkinnedMeshRenderer>().sharedMesh;

                if (mesh != null)
                {
                    print("adding mesh " + mesh.name);
                    for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                    {
                        int[] triangles = mesh.GetTriangles(submesh);
                        List<Vector3> vertices = new List<Vector3>();
                        mesh.GetVertices(vertices);

                        Submesh sub = new Submesh();
                        sub.verticesStart = meshVertices.list.Count;
                        sub.trianglesStart = meshTriangles.list.Count;
                        sub.verticesCount = vertices.Count;
                        sub.trianglesCount = triangles.Length;
                        submeshes.list.Add(sub);

                        meshVertices.list.AddRange(vertices);
                        meshTriangles.list.AddRange(triangles);
                        //print("submesh " + submesh + " added");
                    }
                    if (mesh.subMeshCount > 1)
                        Debug.LogError("WARNING! I haven't set up support for multiple submeshes yet!");

                    //List<Material> materials = new List<Material>();

                    //if (TF.GetComponent<MeshRenderer>() != null) TF.GetComponent<MeshRenderer>().GetMaterials(materials);
                    //if (TF.GetComponent<SkinnedMeshRenderer>() != null) TF.GetComponent<SkinnedMeshRenderer>().GetMaterials(materials);

                    //submeshMaterials.AddRange(materials);
                    //// This error will only be called if A, I made a bad assumption about the materials system, or B code is changed to allow them to desync
                    //if (submeshMaterials.Count != submeshes.list.Count) Debug.LogError("ERROR, submesh count " +
                    //    "is different from submeshMaterials count, incorrect materials will be applied to submeshes!");

                }
                else
                {
                    Debug.LogWarning("WARNING, botskin " + TF + " didn't have a viable mesh to load");
                }
            }
            print(submeshes.list.Count + " submeshes found");

            meshVertices.fillBuffer();
            meshTriangles.fillBuffer();
            submeshes.fillBuffer();

            //botShader = Resources.Load<Shader>("bots");
            botMaterial = new Material(botShader);


            // bots
            //bots = new CSBuffer<bot>("bots");

            //for (int b = 0; b < 400; b++)
            //{
            //    bot bo = new bot();
            //    //bo.pos = Random.insideUnitSphere * 120;
            //    bo.pos = Random.insideUnitSphere * 3;
            //    bots.list.Add(bo);
            //}

            //bots.fillBuffer();

            //CS = Resources.Load<ComputeShader>("TerrainCompute");
            botTick = new CSKernel("botTick", CS);
            //botTick.setBuffers(new List<CSBuffer> { bots, Terrain.terrainCells });
        }

        public static void update()
        {
            //botTick.dispatch();
        }

        /// <summary>
        /// Give it a list of bot (ie mesh) positions and which submesh type they apply to
        /// </summary>
        /// <param name="bots"></param>
        /// <param name="submesh"></param>
        //public static void setABotList(List<bot> bots, int submesh)
        //{
        //    if (botLists == null)
        //        botLists = new List<CSBuffer<bot>>();

        //    if (botLists.Count <= submesh)
        //    {
        //        for (int i = 0; i < (submesh) - botLists.Count; i++)
        //        {
        //            botLists.Add(new CSBuffer<bot>("bots"));
        //        }
        //    }

        //    CSBuffer

        //}

        public static void render()
        {
            if (submeshInstances.Count == 0 && submeshInstances[0] == 0)
            {
                Debug.LogError("submeshInstances not set! no GPUInstancing meshes will be rendered!");
            }
            if (submeshInstances.Count > submeshes.list.Count)
                Debug.LogError("Warning! More different mesh types attempting to be drawn than you have meshes entered! Add more meshes to GPUInstancing script list!");

            int offset = 0;
            for (int submeshI = 0; submeshI < submeshInstances.Count; submeshI++)
            {

                int instanceCount = submeshInstances[submeshI];

                botMaterial.SetPass(0);
                botMaterial.SetBuffer("meshVertices", meshVertices.buffer);
                botMaterial.SetBuffer("meshTriangles", meshTriangles.buffer);
                botMaterial.SetBuffer("submeshes", submeshes.buffer);
                botMaterial.SetInt("submeshI", submeshI);   // why +1..?
                botMaterial.SetInt("botOffset", offset);

                botMaterial.SetBuffer("bots", bots.buffer);

                // HACKY FIX! If there's more than one mesh type, you have to swap out their polygon/instance counts in the Draw command,
                // or they draw too few/too many polys. I have no idea why. But. This makes it work. <__>
                int tricountIndice = submeshI + submeshInstances.Count-1;   // So that input indices of 0,1,2,3 becomes 3,0,1,2 etc
                if (tricountIndice >= submeshInstances.Count)
                    tricountIndice = tricountIndice % submeshInstances.Count;
                int triCount = submeshes.list[tricountIndice].trianglesCount / 3 * submeshInstances[tricountIndice];

                //print("submeshI " + submeshI + " tricountIndice " + tricountIndice + " triCount " + triCount);
                Graphics.DrawProceduralNow(MeshTopology.Points, triCount);   // The number of meshes drawn is determined in the shader, by dividing the given total triangle count by the submesh triangle count

                offset += submeshInstances[submeshI];
                //offset += instanceCount;
            }
        }
    }



    // Start is called before the first frame update
    void Start()
    {
        Bots.setShaders(CS, botShader);
        Bots.setupBots(botSkins);
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void FixedUpdate()
    {
        Bots.update();
    }

    private void OnRenderObject()   // TODO: This runs once per camera? Are my 'DrawProcedural' calls rendering multiple times..?
    {

        Bots.render();
    }
}