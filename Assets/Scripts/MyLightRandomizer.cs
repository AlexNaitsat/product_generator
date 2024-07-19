using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;

[Serializable] //make it visible in inspector menu
[AddRandomizerMenu("Perception/My Light Randomizer")]
public class MyLightRandomizer : Randomizer
{
    //FloatParameter is class wrapper for  float that has method/propetires for random initialization
    // and that has "Serealize" propery to make it visible in unity Inspector tab
    public FloatParameter lightIntensityParameter;
    public ColorRgbParameter lightColorParameter;

    protected override void OnIterationStart()
    {
        //get all objects taged for light randomizer
        var tags = tagManager.Query<MyLightRandomizerTag>();

        foreach (var tag in tags)
        {
            var light = tag.GetComponent<Light>();// light params of each light sources
            light.color = lightColorParameter.Sample();  
            //moved light intensity param  from "MyLightRandomizer" to "MyLightRandomizerTag" 
            //light.intensity = lightIntensityParameter.Sample(); 
        }
    }
}