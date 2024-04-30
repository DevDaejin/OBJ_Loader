using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstValue
{
    //공통
    public const char BlankSplitChar = ' ';
    public const char FaceSplitChar = '/';

    //obj
    public const string ObjectToken = "o";
    public const string GroupToken = "g";
    public const string MatLibraryToken = "mtllib";
    public const string VerticesToken = "v";
    public const string UVsToken = "vt";
    public const string NormalsToken = "vn";
    public const string FaceToken = "f";

    //mtl
    /*        
    * Kd = Diffuse Color, Albedo
    * Ks = Specular Color, Metallic
    * Ns = Smoothness, Glossiness
    * Map_Kd = Diffuse Texture, Albedo 텍스처
    * Map_Bump = Bump Mapping, Normal Map 텍스처
    * d = 투명도
    * Ke = 에미션 컬러
    * Ni는 지원 안함.
    */
    public const string NewMaterialToken = "newmtl";
    public const string Kd = "Kd";
    public const string Ks = "Ks";
    public const string Ns = "Ns";
    public const string Map_Kd = "Map_Kd";
    public const string Map_Bump = "Map_Bump";
    public const string d = "d";
    public const string Ke = "Ke";
    public const string NormalMap = "_NORMALMAP";
    public const string BumpMap = "_BumpMap";
    public const string MainTex = "_MainTex";
    public const string Glossiness = "_Glossiness";
    public const string Metallic = "_Metallic";
    public const string UseMaterialToken = "usemtl";
}
