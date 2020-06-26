using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TerrainGeneration;

public class OpcionesTG : MonoBehaviour
{
    public TerrainGenerator tg;
    public void setSize(int size){
        tg.setWorldSize(size);
    }
}
