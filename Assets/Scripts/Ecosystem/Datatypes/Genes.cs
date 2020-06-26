using UnityEngine;
using static System.Math;

public class Genes {

    const float mutationChance = .1f;
    const float maxMutationAmount = .3f;
    static readonly System.Random prng = new System.Random ();

    public readonly bool isMale;
    ///<summary>Array con los valores de diferentes genes: 
    ///[velocidad, deseabilidad, tiempo embarazo, rango vision, curiosidad, reproductive urge, crecer mas rapido, sentimiento manada]</summary>
    public readonly int[] values;

    public Genes (int[] values) {
        isMale = RandomValue () < 0.5f;
        this.values = values;
    }

    public static Genes RandomGenes (int num) {
        int[] values = new int[num];
        for (int i = 0; i < num; i++) {
            //mutationChance% de activar cada gen al principio
            values[i] = RandomValue()<mutationChance? 1:0;
        }
        return new Genes (values);
    }

    public static Genes InheritedGenes (Genes mother, Genes father) {
        int[] values = new int[mother.values.Length];
        // TODO: implement inheritance
        for (int i = 0; i < father.values.Length; i++) {
            //Ambos tienen el gen activado, el hijo lo hereda pero puede que haya mutacion
            if(father.values[i] == mother.values[i] && father.values[i] == 1){
                values[i] = RandomValue()<mutationChance/10? 0:1;
            }
            else{
                //Uno de los padres tiene el gen, 50% de heredar y 1% de mutacion
                if(father.values[i] == 1 || mother.values[i] == 1){
                    values[i] = RandomValue() > 0.5? 1:0;
                    if(values[i] == 1){ values[i] = RandomValue() > mutationChance/10? 1:0;}
                    else{ values[i] = RandomValue() > mutationChance/10? 0:1;}
                }
                //Ninguno tiene el gen, solo mutacion% de poder tenerlo
                else{
                    values[i] = RandomValue()<mutationChance? 1:0;
                }
            }
        }
        Genes genes = new Genes (values);
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