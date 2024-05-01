using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatData 
{
    public string MatName;
    public Color Albedo = Color.white;
    public float Metallic = 0.5f;
    public float Glossiness = 0.5f;
    public string TextureMapPaht = string.Empty;
    public string NormalMapPath = string.Empty;
    public string RoughnessMapPath = string.Empty;
    public float Alpha = 1;
    public Color Emission = Color.black;
}
