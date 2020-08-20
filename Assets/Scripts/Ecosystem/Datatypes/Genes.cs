using UnityEngine;
using System;
using System.Collections;
using static System.Math;

public class Genes {
    const float mutationChance = .1f;
    static readonly System.Random prng = new System.Random ();

    public readonly bool isMale;
    ///<summary>Array con los valores de diferentes genes: 
    ///[velocidad, deseabilidad, tiempo embarazo, rango vision, curiosidad, reproductive urge, crecer mas rapido, sentimiento manada]</summary>
    public readonly BitArray values;

    public Genes (BitArray valuesB) {
        isMale = RandomValue () < 0.5f;
        this.values = valuesB;
    }

    public static Genes RandomGenes (int num) {
        BitArray valuesB = new BitArray(num);
        for (int i = 0; i < num; i++) {
            //mutationChance% de activar cada gen al principio
            valuesB[i] = RandomValue()<mutationChance? true:false;
        }
        return new Genes (valuesB);
    }

    public static Genes InheritedGenes (Genes mother, Genes father) {
        BitArray valuesB = new BitArray(mother.values.Length);
        
        for (int i = 0; i < father.values.Length; i++) {
            //Ambos tienen el gen activado, el hijo lo hereda pero puede que haya mutacion
            if(father.values[i] == mother.values[i] && father.values[i]){
                valuesB[i] = RandomValue()<mutationChance/10? false:true;
            }
            else{
                //Uno de los padres tiene el gen, 50% de heredar y 1% de mutacion
                if(father.values[i] || mother.values[i]){
                    valuesB[i] = RandomValue() > 0.5? true:false;
                    if(valuesB[i]){ valuesB[i] = RandomValue() > mutationChance/10? true:false;}
                    else{ valuesB[i] = RandomValue() > mutationChance/10? false:true;}
                }
                //Ninguno tiene el gen, solo mutacion% de poder tenerlo
                else{
                    valuesB[i] = RandomValue()<mutationChance? true:false;
                }
            }
        }
        Genes genes = new Genes (valuesB);
        return genes;
    }

    static float RandomValue () {
        //Double random x. 0.0 <= x < 1.0
        return (float) prng.NextDouble ();
    }

    static float RandomGaussian () {
        double u1 = 1 - prng.NextDouble ();
        double u2 = 1 - prng.NextDouble ();
        double randStdNormal = Sqrt (-2 * Log (u1)) * Sin (2 * PI * u2);
        return (float) randStdNormal;
    }
}