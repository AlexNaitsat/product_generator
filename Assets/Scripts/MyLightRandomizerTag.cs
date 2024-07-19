using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;

[AddComponentMenu("Perception/RandomizerTags/MyLightRandomizerTag")]
[RequireComponent(typeof(Light))] //tag can be added only to object with Component "Ligh",(only to light source?)
public class MyLightRandomizerTag : RandomizerTag
{    
    public float minIntensity;
    public float maxIntensity;

    public void SetIntensity(float rawIntensity)
    {
        var light = gameObject.GetComponent<Light>();        
        var scaledIntensity = rawIntensity * (maxIntensity - minIntensity) + minIntensity;
        light.intensity = scaledIntensity;        
    }

}