using System;
using System.Reflection; // to get internal property of "PerceptionCamera" using "GetField(*, BindingFalg*)"
using System.Linq;
using System.IO; //for file manipulations
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers.Utilities;
using UnityEngine.Perception.Randomization.Samplers;
using Unity.Simulation; //to acces "localPersistentDataPath" - a root path to the dataset

using System.Collections.Generic; //for Dictionaries 
//using UnityEditor; //for getting prefab name via "PrefabUtility.GetCorrespondingObjectFromSource"
using System.Text.RegularExpressions; //for Regex used to remove (Clone) from object name string 
//using static SetMaterialsForRenderModes.MaterialForRendering; // to use "enum MaterialForRendering" types  from "SetMaterialsForRenderModes.cs"

namespace UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Creates a 2D layer of of evenly spaced GameObjects from a given list of prefabs
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("My Randomizers/Foreground Single Object Randomizer")]
    public class ForegroundSingleObjectRandomizer : Randomizer
    {
        private SetMaterialsForRenderModes setMaterialsForRenderModes;

        /// <summary>
        /// Name of the dataset to be generated or appended. Located in the perception data generation folder (e.g. C:/Users/jack/UnityProjects/PerceptionDataGen)
        /// </summary>
        [Tooltip("Name of the dataset to be generated or appended. Located in the perception data generation folder ")]
        public string dataset = "renderings";


        /// <summary>
        /// The Z offset component applied to the generated layer of GameObjects
        /// </summary>
        [Tooltip("The Z offset applied to positions of all placed objects.")]
        public float depth;

        /// <summary>
        /// The minimum distance between all placed objects
        /// </summary>
        [Tooltip("The minimum distance between the centers of the placed objects.")]
        public float separationDistance = 2f;
        

        /// The size of the 2D area designated for object placement
        private int frame_index  = 0;//unique index of each rendering 
        private int prefab_index = 0;
        private int view_index   = 0; //viewpoint index of the current object
        
        //each prefab is augmented "scaleParams.sampleNumber" times by non-unform rescaling 
        private int scale_augmentation_index    = 0; //index of scale augmentaiton of the current prefab

     
        public const int firstFrameIndex = 2;


        /// <summary>
        /// The size of the 3D area designated for object placement
        /// </summary>
        [Tooltip("The width and height and the depth of the area in which objects will be placed. These should be positive numbers and sufficiently large in relation with the Separation Distance specified.")]
        public Vector3 placementArea;

        /// <summary>
        /// The range of random rotations to assign to target objects
        /// </summary>
        [Tooltip("The range of random rotations to assign to target objects.")]
         public Vector3Parameter rotation = new Vector3Parameter
        {
            x = new UniformSampler(0, 360),
            y = new UniformSampler(0, 360),
            z = new UniformSampler(0, 360)
        };


       
        [Serializable]
        public struct  NonUniformScaleParams //metada necessary for generating unique names for PNG  output files
        {
            public NonUniformScaleParams(ISampler range_, int sampleNumber_, float  minAspect_, bool keepOriginalSize_)
            {
               range       = range_;
               sampleNumber  = sampleNumber_;
               minAspect  = minAspect_;
               keepOriginalSize = keepOriginalSize_;

            }
            [SerializeReference]
            public ISampler range;
            [Min(0)]
            public int sampleNumber;
            
            [Min(1.0f)]
            public float  minAspect;

            public bool keepOriginalSize;

        }

        /// <summary>
        /// "The number of samples,  range and minimal aspect ratio of non-uniform scalings"
        /// </summary>
        [Tooltip("The number of samples,  range and minimal aspect ratio of non-uniform scalings")]
        public NonUniformScaleParams scaleParams = new NonUniformScaleParams {
            range =  new UniformSampler(1, 20),
            sampleNumber = 0,
            minAspect = 1.1f,
            keepOriginalSize = true
        };
        
        [Tooltip("The list of Prefabs to be placed by this Randomizer.")]
        [SerializeField]  // Ensure this field is serialized and visible in the Inspector

        /// <summary>
        /// The list of prefabs to sample and randomly place
        /// </summary>
        public GameObjectParameter prefabs;
        /*
        /// <summary>
        /// Castom material for non-default rendering modes 
        /// </summary>
        public Material local3D_to_RGB_Material;
        public Material global3D_to_RGB_Material;
        public Material insibeBboxMask_Material;
        */
        /// <summary>
        /// Replacement material to render prefabs instead of their original materials (if set to "None" use the native materials)
        /// </summary>
        [Tooltip("A material to replace original prefab materials in rendering (to disable set it to 'None').")]
        public Material replacementPrefabMaterial;
        /*
        public enum RenderingMode
        {
            Default,
            Local3D_to_RGB,
            Global3D_to_RGB,
            InsibeBboxMask
        }
        /// <summary>
        /// Rendering  mode in which orognal object materials are substetuted with previosuly set custom materials 
        /// </summary>
        [SerializeField]
        public RenderingMode renderingMode = RenderingMode.Default;
        */


        [System.NonSerialized]
        private GameObject current_instance;
        private string current_postfix = "";
        private string current_object_name = "";

        private int mainCameraIndex = 0;

        GameObject m_Container;
        GameObjectOneWayCache m_GameObjectOneWayCache;

        /// <summary>
        /// split data  into 'train' and 'test' folders if the given percent is greater than 0
        /// </summary>
        [Tooltip("Split data into 'train' and 'test' folders accoridng to the given test/train ratio")]
        [Min(0)]
        public float testTrainRatio =0.0f;

        public struct  PNGOutputFileSpec //metada necessary for generating unique names for PNG  output files
        {
            public PNGOutputFileSpec(int view_num_,string name_, string postfix_, int camera_num_) 
            {
                view_num   = view_num_;
                name       = name_;
                postfix    = postfix_;
                camera_num = camera_num_;
            }

            public string name {get;}
            public string postfix {get;}
            public int view_num  {get;}
            public int camera_num  {get;}
            //generate relative path to rendered png in ShapeNetPlain format 
            public override string ToString() => $"{name}/frame_{view_num:D3}_{postfix}_{camera_num:D2}";
        }

        [System.NonSerialized]
        private Dictionary<int,PNGOutputFileSpec> pngfileSpec;
        private List<string> prefabNames = new List<string>();

        /// <inheritdoc/>
        protected override void OnAwake()
        {   
            //copy all objects included in the 'Prefabs' list of the script 
            m_Container = new GameObject("Foreground Objects");
            m_Container.transform.parent = scenario.transform;
            m_GameObjectOneWayCache = new GameObjectOneWayCache(
                m_Container.transform, prefabs.categories.Select(element => element.Item1).ToArray());
            
            prefab_index=0;
            scale_augmentation_index =0;
            frame_index = firstFrameIndex;
            pngfileSpec = new Dictionary<int,PNGOutputFileSpec>(); 
            prefabNames.Clear();


            //check if front camera (1)  or back camera (1) are enanble
            var mainCamera =  GameObject.FindWithTag("MainCamera");
            var mainCameraView = mainCamera.GetComponent<ChangeCamera>();
            mainCameraIndex = (mainCameraView is null)? 0 : mainCameraView.cameraIndex * (mainCameraView.enabled? 1:0);

            current_postfix = MaterialToString(replacementPrefabMaterial);

            /*
            // TODOs:  move the following two switch statements to  OnAwake(), combine them in a single switch statement if posisble  
            current_postfix = renderingMode switch  // add postfix to the  names of generated  PNG files  according to the rendering mode  
            {
                RenderingMode.Default => "Color",
                RenderingMode.Local3D_to_RGB => "NOXRayTL",
                RenderingMode.Global3D_to_RGB => "Global3D",
                RenderingMode.InsibeBboxMask => "InsibeBboxMask",
                _ => ""
            };

            materialForRenderingMode = renderingMode switch //set  material to overide original materials
            {
                RenderingMode.Local3D_to_RGB => local3D_to_RGB_Material,
                RenderingMode.Global3D_to_RGB => global3D_to_RGB_Material,
                RenderingMode.InsibeBboxMask => insibeBboxMask_Material,
                _ => null
            };
            */
        }

        string MaterialToString(Material material)
        {   if (material) {
                // Regular expression to remove "_material", "material", "_Material", "Material"
                string pattern = @"(_material|material|_Material|Material)$";
                return Regex.Replace(material.name, pattern, "", RegexOptions.IgnoreCase);
            } else return "Default";
        }

        /// <summary>
        /// Generates a foreground layer of objects at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart() //this runs once per each object
        {   
            view_index =0;
            Debug.Log("ForegroundAllObjectPoseRandomizer iter start prefab_index=" + prefab_index.ToString());
            
            //pick the current object formt he list of prefab copies 
            current_instance = m_GameObjectOneWayCache.GetOrInstantiate(prefabs.GetCategory(prefab_index));
            current_object_name = Regex.Replace(current_instance.name, @"\(.*\)", ""); //remove "(copy)" sub-string from the name of the current object 
            
            if (scale_augmentation_index > 0) 
            { //check if it's a scale augmentation iteration 
                Vector3 scale = new Vector3 {
                    x = scaleParams.range.Sample(),
                    y = scaleParams.range.Sample(),
                    z = scaleParams.range.Sample()
                };
                
                var scaleMax = Math.Max(scale.z, Math.Max(scale.x, scale.y));
                var scaleMin = Math.Min(scale.z, Math.Min(scale.x, scale.y));
                while (scaleMax/scaleMin < scaleParams.minAspect) {
                    scale.x = scaleParams.range.Sample();
                    scaleMax = Math.Max(scale.z, Math.Max(scale.x, scale.y));
                    scaleMin = Math.Min(scale.z, Math.Min(scale.x, scale.y));
                }
                Debug.Log($"Scale Augmentation#{scale_augmentation_index}: ({scale.x},{scale.y},{scale.z})");
                current_object_name = current_object_name + $"_Scale{scale_augmentation_index}";
                
                if (scaleParams.keepOriginalSize)
                {   //rescale the prefab back to its orignal size (i.e., only the non-unform componet of 'scale' is applied) 
                    var orig_bbox = SetMaterialsForRenderModes.GetMaxBounds(current_instance);
                    current_instance.transform.localScale = scale;
                    var scaled_bbox = SetMaterialsForRenderModes.GetMaxBounds(current_instance);
                    current_instance.transform.localScale *= (orig_bbox.size.magnitude / scaled_bbox.size.magnitude);
                }
                

                
            }
            prefabNames.Add(current_object_name);                                      //to get the orignal prefab name   

            if (setMaterialsForRenderModes == null)
            {
                setMaterialsForRenderModes = Object.FindObjectOfType<SetMaterialsForRenderModes>();

                if (setMaterialsForRenderModes == null)
                {
                    Debug.LogError("SetMaterialsForRenderModes instance not found!");
                    //return; // Exit the method early if not found
                }
            }

            //if (renderingMode != RenderingMode.Default)
            if (replacementPrefabMaterial)
            {
                Debug.Log($"Substituting original material of {current_object_name} with material set for {current_postfix} rendering mode");
                var number_of_materials = current_instance.GetComponent<Renderer>().materials.Length;  // a single prefab may have  mutliple material,
                var replaced_materials = new Material[number_of_materials];                            // need to replace them all 
                for (int i = 0; i < number_of_materials; i++)
                    replaced_materials[i] = replacementPrefabMaterial;
                //make sure that its a copy of a prefeb, and that the original one is not corrupted!
                current_instance.GetComponent<Renderer>().materials = replaced_materials;

            }

            Debug.Log($"OnIterationStart (last line): current_postfix = {current_object_name}");
        }



        protected override void OnUpdate() //this runs once for each frame (viewpoint)  per each object
        {
            Debug.Log("OnUpdate" + frame_index.ToString());

            //string current_object_name = Regex.Replace(current_instance.name, @"\(.*\)", "");

            pngfileSpec[frame_index] = new PNGOutputFileSpec(view_index, current_object_name, current_postfix, mainCameraIndex);
            Debug.Log("  png file name:" + pngfileSpec[frame_index].ToString());

            var seed = SamplerState.NextRandomState();
            var placementSamples = PoissonDiskSampling.GenerateSamples( //generates array of 8 random floats, why 8 ?
                placementArea.x, placementArea.y, separationDistance, seed);
            
            var offset = new Vector3(placementArea.x, placementArea.y, 0f) * -0.5f; 


            var sample = placementSamples[0];
            var z_random_offset =(placementSamples[1].x/placementArea.x)*placementArea.z  - 0.5f*placementArea.z; //scale it to the [-placementArea.z, placementArea.z] range
            //Debug.Log("Z_sample= " + z_random_offset.ToString() + " X_sample= " + placementSamples[1].x.ToString());
            // NEW: add randomization to Z position  using  "placementSamples" to get a same sequence in each session 
            current_instance.transform.position = new Vector3(sample.x, sample.y, depth + z_random_offset) + offset; //and place it at random location
            placementSamples.Dispose();

            current_instance.transform.rotation = Quaternion.Euler(rotation.Sample());//apply a random rotation

            frame_index++;
            view_index++;
        }

        /// <summary>
        /// Deletes generated foreground objects after each scenario iteration is complete
        /// </summary>
        protected override void OnIterationEnd()
        {
            m_GameObjectOneWayCache.ResetAllObjects();

            //for each prefab it generates 'scaleParams.sampleNumber' augmentations 
            if (scale_augmentation_index >= scaleParams.sampleNumber) 
            {
                scale_augmentation_index = 0;
                prefab_index++; //switch to the next prefab objects 
            } 
            else scale_augmentation_index ++; //contineu with prefab scale-augmentations 
        }

        //Here I use the collected metadata to generate a dataset copy in the ShapeNet format 
        protected override void OnScenarioComplete()
        {
            //var root_path =Configuration.localPersistentDataPath;
            //Guid.

            var mainCamera = GameObject.FindWithTag("MainCamera");
            var perceptionCamera = mainCamera.GetComponent<UnityEngine.Perception.GroundTruth.PerceptionCamera>();

            // a tricky way I found to access internal property of PerceptionCamera that stores dataset output folder
            var data_folder = perceptionCamera.GetType().GetProperty("rgbDirectory", BindingFlags.Instance |
                                                                                      BindingFlags.NonPublic).GetValue(perceptionCamera);

            //path to the dataset that was generated in Perception Toolkit foramt
            string data_path = Manager.Instance.GetDirectoryFor(data_folder.ToString());

            //var data_path  = typeof(UnityEngine.Perception.GroundTruth.PerceptionCamera).GetField("rgbDirectory", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(perceptionCamera);
            Debug.Log("Data folder:" + data_path);
            if (!Directory.Exists(data_path))
                Debug.LogError("data path does not exist");

            //string rendering_data_path = $"{Configuration.localPersistentDataPath}/renderings";// new "data_path" copy in the ShapeNet format
            string rendering_data_path = $"{Configuration.localPersistentDataPath.TrimEnd('/')}/{dataset.TrimStart('/')}";  //TrimEnd ensures a single slash between folder names

            Directory.CreateDirectory(rendering_data_path);

            //splitting data into 'test' and 'train'
            int testSamplesNum  = (int) (prefabNames.Count * testTrainRatio);
            int trainSamplesNum = prefabNames.Count  - testSamplesNum;
            var random_order =  new UniformSampler(1, 0);
            //prefabNames.OrderBy(x => random_order.Sample()).Take(testSamplesNum);

            var isTestSample = // random permuatation of (1,..,1,0,...0) to get  'testSamplesNum' true and 'trainSamplesNum' false inclusion flags for test/train split
                (Enumerable.Repeat(true,testSamplesNum).Concat(Enumerable.Repeat(false,trainSamplesNum))).OrderBy(x => random_order.Sample());
            //converting test inclusion collection into a dictionary with name keys 
            Dictionary<string,bool> isTestName = isTestSample.Select((k, i) => new { k, v = prefabNames[i]}).ToDictionary(x => x.v, x => x.k);

            //creating folder for each rendered prefab with test/train split 
            foreach (var prefab_name in prefabNames) {
                string mode_folder =  (isTestName[prefab_name])? "test" : "train";
                var prefab_target_directory = $"{rendering_data_path}/{mode_folder}/{prefab_name}";
                Debug.Log("rendering:" + prefab_name + " in " + prefab_target_directory);
                if (! Directory.Exists(prefab_target_directory) )
                      Directory.CreateDirectory(prefab_target_directory);
            }

            Debug.Log("Last key value =" + pngfileSpec.Last().Key.ToString());
            //copy output images to the prefab folders accoridng to the ShapeNet naming format 
            foreach(var item in pngfileSpec)
            {
                string source_file_name = $"{data_path}/rgb_{item.Key}.png";
                //fix  perceprion toolkit bug of missing last png  
                if (!File.Exists(source_file_name) && item.Key == pngfileSpec.Last().Key) 
                    source_file_name = $"{data_path}/rgb_{item.Key-1}.png"; 

                if (File.Exists(source_file_name)) {
                    string mode_folder =  (isTestName[item.Value.name])? "test" : "train";
                    string target_file_name = $"{rendering_data_path}/{mode_folder}/{item.Value.ToString()}.png";
                    File.Copy(source_file_name, target_file_name, true);//allow override
                }
            } 
        }
        /*
        private void SetAdditionalShaderParameters()
        {


            //GetPrefabInstanceHandle(containterBbox); // 1st method to get Containerbbox object: 
            //var bbox = GetMaxBounds(containterBbox); //Foreground products are prefabs, thus I cannot add a scene object into a field of their component class! 
            //If I add the prefab of "ContanerBbox" to the component class field, then 
            // its coordinates will be differnt from these depicted in the scene


            // To get correct scene cooridnates I need to attach a tag to the Contianer object
            //(If "ContainerBbox" is hidden, then use the method from  https://answers.unity.com/questions/890636/find-an-inactive-game-object.html)
            GameObject containerBboxFromScene = GameObject.FindWithTag("ContainerBbox");
            var bbox = GetMaxBounds(containerBboxFromScene);
            // Debug.Log("Bbox of contianer is:");// Debug.Log(bbox);
            // Debug.Log("min point is:");// Debug.Log(bbox.min);
            // Debug.Log("max point is:");// Debug.Log(bbox.max);


            float leftPnt = bbox.min[0], bottomPnt = bbox.min[1], nearPnt = bbox.min[2];
            float rightPnt = bbox.max[0], topPnt = bbox.max[1], farPnt = bbox.max[2];

            var number_of_materials = GetComponent<Renderer>().materials.Length;
            for (int i = 0; i < number_of_materials; i++)
            {
                GetComponent<Renderer>().materials[i].SetFloat("_LeftPnt", leftPnt);
                GetComponent<Renderer>().materials[i].SetFloat("_RightPnt", rightPnt);
                GetComponent<Renderer>().materials[i].SetFloat("_TopPnt", topPnt);
                GetComponent<Renderer>().materials[i].SetFloat("_BottomPnt", bottomPnt);
                GetComponent<Renderer>().materials[i].SetFloat("_NearPnt", nearPnt);
                GetComponent<Renderer>().materials[i].SetFloat("_FarPnt", farPnt);
            }


            // GetComponent<Renderer>().material.SetFloat("_LeftPnt", leftPnt);
            // GetComponent<Renderer>().material.SetFloat("_RightPnt", rightPnt);
            // GetComponent<Renderer>().material.SetFloat("_TopPnt", topPnt);
            // GetComponent<Renderer>().material.SetFloat("_BottomPnt", bottomPnt);
            // GetComponent<Renderer>().material.SetFloat("_NearPnt", nearPnt);
            // GetComponent<Renderer>().material.SetFloat("_FarPnt", farPnt);

        }
        */
    }
}
 