using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Scenarios; //to access "FixedLengthScenario"
using UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers; //to access "ForegroundAllObjectPoseRandomizer"


public class ChangeCamera : MonoBehaviour
{

    /// <summary>
    /// Number of cameras  on the same plane rotated around a chosen axis by a constant angle. 
    /// </summary>
    //[SerializeField]
    public int NumberOfCameras = 2;
    public enum Axis { x, y, z }
    public Axis RotationAxis;
    /*
    TODOS:
    1. Add NumberOfCameras  public field
    2. below that field in the inspector window it shoud write "Alpha angle between cameras", where Alpha = 360/NumberOfCameras
    3. CameraView should be in the Range(0,NumberOfCameras-1) 
    */

    /// <summary>
    /// Choose a camera for rendering (camera 0 = main camera from the inspector window )
    /// </summary>
    //[Range(0,1)]
    //[SerializeField]
    public int cameraIndex;
    private Transform saved_transform;


    // Start is called before the first frame update
    void Start()
    {   
        saved_transform  = transform;
        
        float angle_between_cameras = 360f / NumberOfCameras;
        if (cameraIndex < 0 || cameraIndex >= NumberOfCameras)
        {
            Debug.LogError("cameraView=" + cameraIndex + ", it should be in [0, " + NumberOfCameras + " - 1] range, setting it to 0");
            cameraIndex = 0;
        }
        Vector3 axisVector;
        switch (RotationAxis)
        {
            case Axis.x:
                axisVector = Vector3.right;
                break;
            case Axis.y:
                axisVector = Vector3.up;
                break;
            case Axis.z:
                axisVector = Vector3.forward;
                break;
            default:
                axisVector = Vector3.up;
                break;
        }

        if (cameraIndex != 0)
        {
            var simulationScenarioObj = GameObject.Find("Simulation Scenario");
            var average_product_depth = simulationScenarioObj.GetComponent<FixedLengthScenario>().GetRandomizer<FgSingleObjectRandomizer>().depth;
            var obj_center = new Vector3(0.0f, 0.0f, average_product_depth);

            // Rotate the camera by Alpha * cameraView degrees
            transform.RotateAround(obj_center, axisVector, angle_between_cameras * cameraIndex);
        }


        //TODO: instead of "switch" use "if":  for  cameraView = 0 do nothing , for other values rotate it by Alpha*cameraView degree 
            /*
            switch (cameraView){
                //case CameraView.View0:
                case 1:
                {
                    //do nothing
                        break;
                }
                //case CameraView.View1:
                case 0:
                {
                    var simulationScenrioObj =  GameObject.Find("Simulation Scenario");
                    var average_product_depth =
                        simulationScenrioObj.GetComponent<FixedLengthScenario>().GetRandomizer<FgSingleObjectRandomizer>().depth;


                        var obj_center = new Vector3(0.0f,0.0f,average_product_depth);
                    transform.RotateAround(obj_center, Vector3.up, 180);
                    //Debug.Log("Camera Position after="); Debug.Log(transform.position);

                    break;
                }
                default:
                break;
            } 
            */
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnScenarioComplete()
    { //restore the original camera settings
        transform.position = saved_transform.position;
        transform.rotation = saved_transform.rotation;
    }

}
