using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Nos permite crear el asset BoidSettings en el inspector de unity
[CreateAssetMenu]
///<summary>Valores base de todos los Depredadores. No van a necesitar las 3 reglas basicas</summary>
public class DepredadorSettings : ScriptableObject {
    public GameObject sueloMundo;
    public float minSpeed = 3f;
    public float maxSpeed = 9f;
    public float radioPercepcion = 5f;

    [Header ("Pesos reglas")]
    public float pesoAtaque = 2f;
    public float pesoEvitarChoque = 10f;
    public float ratioTiempovuelo = 1f/100f;
    public float ratioTiempoDescanso = 1f/10f;

    [Header("Colision")]
    ///<summary>Evitan todo obstaculo que este a esta distancia</summary>
    public float distanciaEvitarColision = 5f;
    public LayerMask mascaraObstaculos;
}
