using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class Example : MonoBehaviour
{
    //public Environment environment;
    public Mundo environment;
    public SimplestPlot.PlotType PlotExample = SimplestPlot.PlotType.TimeSeries;
    //Activar para crear txt, sino no
    public bool crearTxt = true;
    //Numero de puntos de la grafica a calcular
    //private int DataPoints = 300;
    private SimplestPlot SimplestPlotScript;
    //Indice
    private float Counter = 0;
    private Color[] MyColors = new Color[3];

    private System.Random MyRandom;
    //Valores en x e y de la grafica
    /*private float[] X1Values;
    private float[] Y1 Values;
    private float[] Y2 Values;*/
    private List<float> ConejosX;
    private List<float> ConejosY;
    private List<float> ZorrosX;
    private List<float> ZorrosY;
    private List<float> PlantasX;
    private List<float> PlantasY;
    private List<float> Y2Values;


    private Vector2 Resolution;
    private string path;

    void CrearArchivo(){
        var fechaSinBarras = System.DateTime.Now.Year.ToString() +"-" + System.DateTime.Now.Month.ToString() +"-" 
        + System.DateTime.Now.Day.ToString() +"-" + System.DateTime.Now.Hour.ToString() + "-" + System.DateTime.Now.Minute.ToString() + "-"
        + System.DateTime.Now.Second.ToString();
        path = Application.dataPath + "/Logs/NSeres/seres_" + fechaSinBarras + ".txt";
        //Si no existe, lo creamos
        if(!File.Exists(path)){
            File.WriteAllText(path, "Numero de seres\n");
        }
        //Si existe, lo borramos y creamos uno nuevo
        else{
            File.Delete(path);
            File.WriteAllText(path, "Numero de seres\n");
        }
    }

    // Use this for initialization
    void Start()
    {
        if(crearTxt) {
            CrearArchivo();
            print("Archivo creado en: " + path);
        }
        SimplestPlotScript = GetComponent<SimplestPlot>();

        InvokeRepeating("UpdateGrafica", 0f, 0.95f);

        MyRandom = new System.Random();
        /*ConejosX = new float[DataPoints];
        ConejosY = new float[DataPoints];
        Y2Values = new float[DataPoints-2];*/
        ConejosX = new List<float>();
        ConejosY = new List<float>();
        ZorrosX = new List<float>();
        ZorrosY = new List<float>();
        PlantasX = new List<float>();
        PlantasY = new List<float>();

        Y2Values = new List<float>();
        //Valores iniciales para que no pete en la primera iteracion
        ConejosX.Add((float) environment.grafConejos.Count);
        ConejosY.Add((float) environment.grafConejos[0]);
        //print("grafConejos.count: " + environment.grafConejos.Count + " grafConejos[0]: " + environment.grafConejos[0]);
        ZorrosX.Add((float) environment.grafZorros.Count);
        ZorrosY.Add((float) environment.grafZorros[0]);
        //print("grafZorros.count: " + environment.grafZorros.Count + " grafZorros[0]: " + environment.grafZorros[0]);
        PlantasX.Add((float) environment.grafPlantas.Count);
        PlantasY.Add((float) environment.grafPlantas[0]);
        //print("grafPlantas.count: " + environment.grafPlantas.Count + " grafPlantas[0]: " + environment.grafPlantas[0]);
        
        //Vector de colores para las diferentes lineas
        MyColors[0] = Color.white;
        MyColors[1] = Color.red;
        MyColors[2] = Color.green;

        SimplestPlotScript.SetResolution(new Vector2(300, 300));
        SimplestPlotScript.BackGroundColor = new Color(0.1f, 0.1f, 0.1f, 0.4f);
        //SimplestPlotScript.TextColor = Color.yellow;
        //Configuracion de las 3 graficas que se van a dibujar
        for (int Cnt = 0; Cnt < 3; Cnt++)
        {
            SimplestPlotScript.SeriesPlotY.Add(new SimplestPlot.SeriesClass());
            SimplestPlotScript.DistributionPlot.Add(new SimplestPlot.DistributionClass());
            SimplestPlotScript.PhaseSpacePlot.Add(new SimplestPlot.PhaseSpaceClass());

            SimplestPlotScript.SeriesPlotY[Cnt].MyColor = MyColors[Cnt];
            SimplestPlotScript.DistributionPlot[Cnt].MyColor = MyColors[Cnt];
            SimplestPlotScript.PhaseSpacePlot[Cnt].MyColor = MyColors[Cnt];

            SimplestPlotScript.DistributionPlot[Cnt].NumberOfBins = (0 + 1) * 5;
        }

        Resolution = SimplestPlotScript.GetResolution();
        //PrepareArrays();
    }

    // Update is called once per frame
    void UpdateGrafica()
    {
        Counter++;
        PrepareArrays();
        SimplestPlotScript.MyPlotType = PlotExample;
        switch (PlotExample)
        {
            case SimplestPlot.PlotType.TimeSeries:
                SimplestPlotScript.SeriesPlotY[0].YValues = ConejosY.ToArray();
                SimplestPlotScript.SeriesPlotY[1].YValues = ZorrosY.ToArray();
                SimplestPlotScript.SeriesPlotY[2].YValues = PlantasY.ToArray();
                SimplestPlotScript.SeriesPlotX = ConejosX.ToArray();
                break;
            //Mirar los otros dos casos
            case SimplestPlot.PlotType.Distribution:
                SimplestPlotScript.DistributionPlot[0].Values = ConejosY.ToArray();
                SimplestPlotScript.DistributionPlot[1].Values = Y2Values.ToArray();
                break;
            case SimplestPlot.PlotType.PhaseSpace:
                SimplestPlotScript.PhaseSpacePlot[0].XValues = ConejosX.ToArray();
                SimplestPlotScript.PhaseSpacePlot[0].YValues = ConejosY.ToArray();
                SimplestPlotScript.PhaseSpacePlot[1].XValues = ZorrosX.ToArray();
                SimplestPlotScript.PhaseSpacePlot[1].YValues = Y2Values.ToArray();
                break;
            default:
                break;
        }
        //NOTA:REPASAR CADA CUANTO SE LLAMA, Example SE LLAMA MAS VECES (POCAS MAS)
        //QUE ENVIRONMENT ACTUALIZA LOS DATOS. SINCRONIZARLOS MEJOR
        //Debug.Log("Long lista datos X: " + ConejosY.Count.ToString());
        SimplestPlotScript.UpdatePlot();
    }
    private void PrepareArrays()
    {
        //for (int Cnt = 0; Cnt < DataPoints; Cnt++)
        //for (int Cnt = 0; Cnt < environment.grafConejos.Count; Cnt++)
        //{
            //Segundos pasados
            //ConejosX[Cnt] = (float) environment.grafConejos.Count;
            //ConejosY[Cnt] = (float) environment.grafConejos[Cnt];
            ConejosX.Add((float) environment.grafConejos.Count);
            ConejosY.Add((float) environment.grafConejos[environment.grafConejos.Count - 1]);
            ZorrosX.Add((float) environment.grafZorros.Count);
            ZorrosY.Add((float) environment.grafZorros[environment.grafZorros.Count - 1]);
            //Para evitar que la escala de la grafica de las plantas no deje diferenciar las otras
            if(environment.grafPlantas[environment.grafPlantas.Count - 1]>450)
                PlantasY.Add(450f);
            else
                PlantasY.Add((float) environment.grafPlantas[environment.grafPlantas.Count - 1]);    
            PlantasX.Add((float) environment.grafPlantas.Count);

            if(crearTxt){
                var c = (environment.grafConejos[environment.grafConejos.Count - 1]).ToString() + "c ";
                var z = (environment.grafZorros[environment.grafZorros.Count - 1]).ToString() + "z ";
                var p = (environment.grafPlantas[environment.grafPlantas.Count - 1]).ToString() + "p ";
                File.AppendAllText(path,  c+z+p+"\n" );
            }

            //ConejosX[Cnt] = (Counter + Cnt) * Mathf.PI / (Resolution.x);
            //ConejosY[Cnt] = Mathf.Cos(ConejosX[Cnt]) * 20;
            //if (Cnt < DataPoints - 2) Y2Values[Cnt] = Mathf.Sin(ConejosX[Cnt]) * 10 + 7;
        //}
    }
}
