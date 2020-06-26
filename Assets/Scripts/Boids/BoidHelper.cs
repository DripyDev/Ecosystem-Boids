using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BoidHelper {

    //Numero de rayos casteados
    const int numViewDirections = 300;
    //Vector con los rayos casteados
    public static readonly Vector3[] directions;
    //Direcciones del Boid en funcion del GoldenRatio para optimizar las direcciones o algo asi
    static BoidHelper () {
        directions = new Vector3[BoidHelper.numViewDirections];
        //Factor del numero aureo 1.618033989
        float goldenRatio = (1 + Mathf.Sqrt (5)) / 2;
        //Aumentamos el angulo del rayo en funcion de pi y del numero aureo
        float angleIncrement = Mathf.PI * 2 * goldenRatio;
        //Calculamos el vector del rayo
        for (int i = 0; i < numViewDirections; i++) {
            float t = (float) i / numViewDirections;
            float inclination = Mathf.Acos (1 - 2 * t);
            float azimuth = angleIncrement * i;

            float x = Mathf.Sin (inclination) * Mathf.Cos (azimuth);
            float y = Mathf.Sin (inclination) * Mathf.Sin (azimuth);
            float z = Mathf.Cos (inclination);
            directions[i] = new Vector3 (x, y, z);
        }
    }

}