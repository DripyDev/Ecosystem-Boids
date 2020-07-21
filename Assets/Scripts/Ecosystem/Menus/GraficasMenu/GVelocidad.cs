using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class GVelocidad : MonoBehaviour
{
    public Mundo mundo;
    public SimplestPlot.PlotType PlotExample = SimplestPlot.PlotType.TimeSeries;
    //Numero de puntos de la grafica a calcular
    //private int DataPoints = 300;
    private SimplestPlot SimplestPlotScript;
    //Indice
    private float Counter = 0;
    private Color[] MyColors = new Color[1];

    private System.Random MyRandom;
    //Valores en x e y de la grafica
    /*private float[] X1Values;
    private float[] Y1 Values;
    private float[] Y2 Values;*/
    //Valor de X
    private List<float> tiempo;
    //Valor de Y
    private List<float> velocidadMedia;
    private List<float> Y2Values;
    public Species especieGrafica;


    private Vector2 Resolution;
    public bool crearTxt = true;
    private string path;
    void CrearArchivo(){
        var fechaSinBarras = System.DateTime.Now.Year.ToString() +"-" + System.DateTime.Now.Month.ToString() +"-" 
        + System.DateTime.Now.Day.ToString() +"-" + System.DateTime.Now.Hour.ToString() + "-" + System.DateTime.Now.Minute.ToString() + "-"
        + System.DateTime.Now.Second.ToString();
        path = Application.dataPath + "/Logs/Velocidad/velocidad" + especieGrafica + "_" + fechaSinBarras + ".txt";
        //Si no existe, lo creamos
        if(!File.Exists(path)){
            File.WriteAllText(path, "Velocidad media\n");
        }
        //Si existe, lo borramos y creamos uno nuevo
        else{
            File.Delete(path);
            File.WriteAllText(path, "Velocidad media\n");
        }
    }
    // Use this for initialization
    void Start() {
        if(crearTxt) {
            CrearArchivo();
            print("Archivo creado en: " + path);
        }
        SimplestPlotScript = GetComponent<SimplestPlot>(); 

        MyRandom = new System.Random();
        tiempo = new List<float>();
        velocidadMedia = new List<float>();

        Y2Values = new List<float>();

        //Valores iniciales para que no pete en la primera iteracion
        tiempo.Add(1f);
        velocidadMedia.Add(1.5f);
        
        //Color para la grafica
        MyColors[0] = especieGrafica==Species.Fox? Color.red:Color.white;

        SimplestPlotScript.SetResolution(new Vector2(300, 300));
        SimplestPlotScript.BackGroundColor = new Color(0.1f, 0.1f, 0.1f, 0.4f);

        SimplestPlotScript.SeriesPlotY.Add(new SimplestPlot.SeriesClass());
        SimplestPlotScript.SeriesPlotY[0].MyColor = MyColors[0];

        Resolution = SimplestPlotScript.GetResolution();
        //PrepareArrays();

        InvokeRepeating("UpdateGrafica", 0f, 10f);
    }

    void UpdateGrafica()
    {
        Counter++;
        PrepareArrays();
        SimplestPlotScript.MyPlotType = PlotExample;
        switch (PlotExample)
        {
            case SimplestPlot.PlotType.TimeSeries:
                SimplestPlotScript.SeriesPlotY[0].YValues = velocidadMedia.ToArray();
                SimplestPlotScript.SeriesPlotX = tiempo.ToArray();
                break;
            default:
                break;
        }
        SimplestPlotScript.UpdatePlot();
    }

    private void PrepareArrays() {
        //El tiempo avanza en 1 cada 10 segundos
        tiempo.Add(Time.fixedTime/10f);
        if(especieGrafica == Species.Rabbit){
            //Si ya no hay zorros o conejos, matamos el proceso
            if(mundo.grafConejos[mundo.grafConejos.Count - 1]<=0)
                this.enabled=false;
            float media = (mundo.velocidadConejos/mundo.grafConejos[mundo.grafConejos.Count - 1]);
            //print("Sumatorio velocidad conejos: " + mundo.velocidadConejos + " numero conejos: " + mundo.grafConejos[mundo.grafConejos.Count - 1]);
            //print("Velocidad media conejos: " + media );
            //Como es asincrono, a veces puede pasar que coja el sumatorio actualizado pero el numero sin actualizar.
            //la media NO puede ser menor a 1.5, si lo es, es problema de la sincronizacion y lo ignoramos
            if(media >=1.5)
                velocidadMedia.Add(media);
            else
                velocidadMedia.Add(1.5f);
            if(crearTxt){
                File.AppendAllText(path,  media+"\n" );
            }
        }
        if(especieGrafica == Species.Fox){
            if(mundo.grafZorros[mundo.grafZorros.Count - 1]<=0)
                this.enabled=false;
            float media = (mundo.velocidadZorros/mundo.grafZorros[mundo.grafZorros.Count - 1]);
            //print("Sumatorio velocidad zorros: " + mundo.velocidadZorros + " numero zorros: " + mundo.grafZorros[mundo.grafZorros.Count - 1]);
            //print("Velocidad media zorros: " + media );
            //Como es asincrono, a veces puede pasar que coja el sumatorio actualizado pero el numero sin actualizar.
            //la media NO puede ser menor a 1.5, si lo es, es problema de la sincronizacion y lo ignoramos
            if(media >=1.5)
                velocidadMedia.Add(media);
            else
                velocidadMedia.Add(1.5f);
            if(crearTxt){
                File.AppendAllText(path,  media+"\n" );
            }
        }
    }
}