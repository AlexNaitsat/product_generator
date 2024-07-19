using System; //for Array, but here is now "Fill" funtion
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetMaterialsForRenderModes : MonoBehaviour
{
    public Material local3D_to_RGB_Material;
    public Material global3D_to_RGB_Material;
    public Material insibeBboxMask_Material;
    //public GameObject containterBbox;


    
    public enum MaterialForRendering
    {
        Default,
        Local3D_to_RGB,
        Global3D_to_RGB,
        InsibeBboxMask
    }
    /// <summary>
    /// Material with shaders used during rendering, ( non default materials have shaders to visualize 3D params)
    /// </summary>
    [SerializeField] 
    public MaterialForRendering materialForRendering = MaterialForRendering.Default;

    public Material GetMaterialInstanceForRendering() {
        //using a shortened switch statement 
        return materialForRendering switch {
            MaterialForRendering.Local3D_to_RGB  => local3D_to_RGB_Material,
            MaterialForRendering.Global3D_to_RGB => global3D_to_RGB_Material,
            MaterialForRendering.InsibeBboxMask  => insibeBboxMask_Material,
            _ =>  GetComponent<Renderer>().material
        };
    }
    // Start is called before the first frame update
    void Start()
    {

        if   (materialForRendering == MaterialForRendering.Default) 
             return; //do nothing 
        // Rendered objects could have multiple materials that need to be replaced by "materialForRendering"
        //In that case the problem is that  I cannot assign it  via "GetComponent<Renderer>().materials[i] = replaced_material_instance"
        // because they did not implement  per index "set" for "Renderer.materials"  attribute. 
        // The only way I found is to assign the entire array of materials
        var number_of_materials = GetComponent<Renderer>().materials.Length;
        var replaced_materials = new Material[number_of_materials];
        var replaced_material_instance = GetMaterialInstanceForRendering();
        for (int i = 0; i < number_of_materials; i++)
                        replaced_materials[i] = replaced_material_instance;
                        
        GetComponent<Renderer>().materials = replaced_materials;
        //Debug.Log($"Start: materialForRendering = {materialForRendering}");

        // //the old version  works only for a single material object
        // switch(materialForRendering) {
        //     case MaterialForRendering.Default:
        //     {
        //        break;     
        //     }
        //     case MaterialForRendering.Local3D_to_RGB:
        //     {
        //         GetComponent<Renderer>().material = local3D_to_RGB_Material;    
        //         break;     
        //     }
        //     case MaterialForRendering.Global3D_to_RGB:
        //     {
        //         GetComponent<Renderer>().material = global3D_to_RGB_Material;
        //         break;     
        //     }
        //     case MaterialForRendering.InsibeBboxMask:
        //     {
        //         GetComponent<Renderer>().material = insibeBboxMask_Material;
        //         break;     
        //     }


        //     default:
        //     break;
        // }
    }
    void Render(){
         if (materialForRendering == MaterialForRendering.InsibeBboxMask)
            SetInsideBboxShaderProperties();
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log($"Update: materialForRendering = {materialForRendering}");
        if (materialForRendering == MaterialForRendering.InsibeBboxMask)
            SetInsideBboxShaderProperties();
    }

    //TODO: it's used outside the class, thus it  should be moved to a separate utilty script
    //returns mesh bbox of given (arbitary) game object
    public  static Bounds GetMaxBounds(GameObject g)
    {
        var m = g.GetComponent<MeshFilter>().mesh;
        
        //var bbox = g.GetComponent<MeshFilter>().mesh.bounds;
        //var gg = g.transform.position;
        //returns a point (zero-sized-bbox) places at the "Position" field of ContianerBbox 
        //var b = new Bounds(g.transform.position, Vector3.zero);

        Vector3 bb_min = Vector3.one* 10e+10f , bb_max= -Vector3.one* 10e+10f;
        //Note: "transfrom" = "this.trnasform" = transformation of the "Set Materials For Rendering Modes" game object
        //=> use "g.transform" is transformation of contianerBbox object 
        Matrix4x4 localToWorld = g.transform.localToWorldMatrix;



        //computing bbox of mesh in word vertices coordinates
        for (int i = 0; i < m.vertices.Length; ++i)
        {
            Vector3 world_v  = g.transform.TransformPoint(m.vertices[i]);
            //     world_v1 = localToWorld.MultiplyPoint3x4(m.vertices[i]);//equivalent compuyation 
            for (int j = 0; j < 3; ++j) {
                bb_min[j] = Mathf.Min(bb_min[j], world_v[j]);
                bb_max[j] = Mathf.Max(bb_max[j], world_v[j]);
            }

            //Debug.Log(world_v);
            
        }
        Bounds  bbox  = new Bounds((bb_min+bb_max)/2.0f, bb_max-bb_min);
        
        // Debug.Log("===bbox for special material is ==="); // Debug.Log(bbox);
        
        return bbox;
    }

    //Two exmpales of how to get game object bbox
    //looks like it returns bboxes that are not in the word coord system 

    // private Bounds GetMaxBounds(GameObject g)
    // {
    //     var renderers = g.GetComponentsInChildren<Renderer>();
    //     if (renderers.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
    //     var b = renderers[0].bounds;
    //     foreach (Renderer r in renderers)
    //     {
    //         b.Encapsulate(r.bounds);
    //     }
    //     return b;
    // }


    // private Bounds GetMaxBounds(GameObject g)
    // {
    //     var b = new Bounds(g.transform.position, Vector3.zero);
    //     foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
    //     {
    //         b.Encapsulate(r.bounds);
    //     }
    //     return b;
    // }


    private void SetInsideBboxShaderProperties()
    {
        
        
        //GetPrefabInstanceHandle(containterBbox); // 1st method to get Containerbbox object: 
        //var bbox = GetMaxBounds(containterBbox); //Foreground products are prefabs, thus I cannot add a scene object into a field of their component class! 
                                                   //If I add the prefab of "ContanerBbox" to the component class field, then 
                                                   // its coordinates will be differnt from these depicted in the scene

        
        // To get correct scene cooridnates I need to attch a tag to the Contianer object
        //(If "ContainerBbox" is hidden, then use the method from  https://answers.unity.com/questions/890636/find-an-inactive-game-object.html)
        GameObject containerBboxFromScene = GameObject.FindWithTag("ContainerBbox");
        var bbox = GetMaxBounds(containerBboxFromScene);
        // Debug.Log("Bbox of contianer is:");// Debug.Log(bbox);
        // Debug.Log("min point is:");// Debug.Log(bbox.min);
        // Debug.Log("max point is:");// Debug.Log(bbox.max);


        float leftPnt = bbox.min[0], bottomPnt = bbox.min[1], nearPnt = bbox.min[2];
        float rightPnt = bbox.max[0], topPnt = bbox.max[1], farPnt = bbox.max[2];

        var number_of_materials = GetComponent<Renderer>().materials.Length;
        for (int i = 0; i < number_of_materials; i++) {
            GetComponent<Renderer>().materials[i].SetFloat("_LeftPnt", leftPnt);
            GetComponent<Renderer>().materials[i].SetFloat("_RightPnt", rightPnt);
            GetComponent<Renderer>().materials[i].SetFloat("_TopPnt", topPnt);
            GetComponent<Renderer>().materials[i].SetFloat("_BottomPnt", bottomPnt);
            GetComponent<Renderer>().materials[i].SetFloat("_NearPnt", nearPnt);
            GetComponent<Renderer>().materials[i].SetFloat("_FarPnt", farPnt);
        }
    }
}
