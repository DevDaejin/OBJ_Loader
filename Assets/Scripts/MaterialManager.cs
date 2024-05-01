using System.Collections.Generic;
using UnityEngine;

public class MaterialManager
{
    private static MaterialManager instance;
    public static MaterialManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new MaterialManager();
            }
            return instance;
        }
    }
    private Dictionary<string, Material> container = new Dictionary<string, Material>();
    private Shader shader;
    private TextureLoader texLoader = new TextureLoader();

    public Material DefaultMat { private set; get; }

    public Material GetMaterial(string name)
    {
        if (container.ContainsKey(name))
        {
            return container[name];
        }
        else 
        {
            Debug.LogWarning("Materil is not included");
            return DefaultMat;
        }
    }

    public void BuildMaterial(MatData[] matDatas)
    {
        if (!shader)
            shader = Shader.Find("Standard");

        if (!DefaultMat)
            DefaultMat = new Material(shader);

        for (int i = 0; i < matDatas.Length; i++)
        {
            if (container.ContainsKey(matDatas[i].MatName))
                continue;

            Material mat = new Material(shader);
            mat.name = matDatas[i].MatName;

            Color matColor = Color.white;
            matColor = matDatas[i].Albedo;
            
            if (matDatas[i].Alpha != 1)
            {
                mat.SetFloat(ConstValue.MaterialMode, 3);
                matColor.a = matDatas[i].Alpha;
            }

            Texture tex = null;
            if (!string.IsNullOrEmpty(matDatas[i].TextureMapPaht))
            {
                tex = texLoader.GetTexture(matDatas[i].TextureMapPaht);
            }

            Texture normal = null;
            if (!string.IsNullOrEmpty(matDatas[i].NormalMapPath))
            {
                normal = texLoader.GetTexture(matDatas[i].NormalMapPath);
            }

            Texture roughness = null;
            if (!string.IsNullOrEmpty(matDatas[i].RoughnessMapPath))
            {
                roughness = texLoader.GetTexture(matDatas[i].RoughnessMapPath);
            }

            if (matDatas[i].Emission != Color.black)
            {
                mat.EnableKeyword(ConstValue.EmissionMode);
            }

            mat.color = matColor;
            mat.SetFloat(ConstValue.Metallic, matDatas[i].Metallic);
            mat.SetFloat(ConstValue.Glossiness, matDatas[i].Glossiness);
            mat.SetTexture(ConstValue.MainTex, tex);
            mat.SetTexture(ConstValue.BumpMap, normal);
            mat.SetTexture(ConstValue.RoughnessMap, roughness);
            mat.SetColor(ConstValue.Emission, matDatas[i].Emission);

            container.Add(mat.name, mat);
        }
    }
}