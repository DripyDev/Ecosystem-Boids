using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using static System.Math;

public class Rabbit : Animal {
    public static readonly string[] GeneNames = { "A", "B" };
    //Valor nutricional del conejo 
    //NOTA: EN EL FUTURO TIENE QUE IR EN FUNCION DEL ALGUN GEN
    public float valorNutricional;
    //Variables que antes estaban en Animal
    public Vector3Int depredadorMasCercano;
    private static Vector3Int cero = new Vector3Int(0,0,0);
    /*void Start() {
        var aux = new System.Random();
        valorNutricional = Mathf.Max( (aux.Next(80)/100f), 0.5f);
    }*/
    
    public void Update() {
        // Increase hunger, thirst and age over time
        //Si somos mas rapidos pasaremos mas hambre y sed
        hunger += Time.deltaTime * 1 / timeToDeathByHunger * (moveSpeed/1.5f);
        thirst += Time.deltaTime * 1 / timeToDeathByThirst * (moveSpeed/1.5f);
        edad += Time.deltaTime * 1 / timeToDeathByAge * ratioCrecimiento;
        reproductiveUrge += Time.deltaTime * 1 / 350 * ratioReproductiveUrge;
        Crecer();
        comprobarEmbarazo();

        //Path ya recorrido, lo reseteamos
        if(path != null){
            if(pathIndex >= path.Length)
                path=null;
        }

        //Si ya estamos en el final del path, lo reseteamos (es para evitar que al final del path vayamos, por ejemplo, a por agua y no podamos ir porque noe sta visible)
        if(path != null && path.Length > 0){
            if(coord == path[path.Length-1]){
                print("Estamos al final del path, lo reseteamos");
                path = null;
            }
        }
        
        //material.color += new Color(material.color.r,edad,edad,0);

        // Animate movement. After moving a single tile, the animal will be able to choose its next action
        //puede que haya que cambiar esto para simular la velocidad¿?¿?¿?
        if (animatingMovement) {
            //print("-------------INICIO MOVIMIENTO---------------------");
            AnimateMove();
        }
        else {
            // Handle interactions with external things, like food, water, mates
            HandleInteractions();
            float timeSinceLastActionChoice = Time.time - lastActionChooseTime;
            //Elegimos la siguiente accion si ha pasado timeBetweenActionChoices segundos (1 segundo) desde la ultima accion
            if (timeSinceLastActionChoice > timeBetweenActionChoices) {
                ChooseNextAction ();
            }
        }
        if (hunger >= 1) {
            Die (CauseOfDeath.Hunger);
        } else if (thirst >= 1) {
            Die (CauseOfDeath.Thirst);
        } else if (edad >= 1) {
            Die (CauseOfDeath.Age);
        }
    }

    protected override void ChooseHijo(){
        ChooseNextAction();
    }

    //NOTA: De momento es un sistema reactivo, seria interesante cambiarlo a BDI
    // Animals choose their next action after each movement step (1 tile),
    // or, when not moving (e.g interacting with food etc), at a fixed time interval
    public void ChooseNextAction () {
        lastActionChooseTime = Time.time;
        // Get info about surroundings

        // Decide next action:
        var aux = Mundo.SentirDepredador(this, maxViewDistance);
        depredadorMasCercano = aux==null? cero:(aux).coord;
        //NOTA: Cambiar el species!=Species.Fox porque en el futuro puede que haya más animales. Cambiarlo a buscar en el diccionario de depredadores
        if (depredadorMasCercano != cero && species != Species.Fox) {
            HuirDepredador(depredadorMasCercano);
        }
        //NOTA: Repasar cuando elige que accion
        else {
            depredadorMasCercano = cero;
            // Eat if (more hungry than thirsty) or (currently eating and not critically thirsty)
            bool currentlyEating = currentAction == CreatureAction.Eating && foodTarget && hunger > 0;
            bool wellFed = hunger < criticalPercent/1.5;
            bool wellThirst = thirst < criticalPercent/1.5;
            //Si estamos bien alimentados podemos buscar pareja
            if(wellFed && wellThirst && reproductiveUrge>0.3){
                FindMate();
            }
            //Si no estamos bien alimentados, vamos a buscar comida o agua
            else{
                if(hunger>thirst || currentlyEating){
                    FindFood();
                }
                else{
                    FindWater();
                }
            }        
        }
    Act();
    }

    

    ///<summary>Huimos en direccion completamente opuesta al depredador mas cercano</summary>
    private void HuirDepredador(Vector3Int depredador){
        depredadorMasCercano = depredador;
        currentAction = CreatureAction.Fleeing;
        //print("Huimos a: " + base.coord + (base.coord - depredador));
        CreatePath(base.coord + (base.coord - depredador));
    }

    public override void Init (Vector3Int coord) {
        //print("Init de Rabbit");
        base.Init(coord);
    }
    public void OnMouseDown(){
        GameObject.Find("InformacionAnimal").GetComponent<InformacionAnimal>().SetAnimal(this);
    }

    private Vector3 SumarCeroCinco(Vector3 v){
        return new Vector3(v.x + 0.5f, 0, v.z + 0.5f);
    }

    void OnDrawGizmosSelected () {
        /*if(waterTarget != null){
            Gizmos.DrawLine(transform.position, waterTarget);
        }
        var auxcolor = Color.yellow;
        auxcolor.a = 0.1f;
        Gizmos.color = auxcolor;
        Gizmos.DrawSphere(transform.position, maxViewDistance);*/
        if (Application.isPlaying) {
            //var surroundings = Environment.Sense(coord, maxViewDistance);
            var auxcolor = Color.yellow;
            auxcolor.a = 0.1f;
            Gizmos.color = auxcolor;
            Gizmos.DrawSphere(transform.position, maxViewDistance);
            Gizmos.color = Color.white;
            /*if (surroundings.nearestFoodSource != null) {
                Gizmos.DrawLine (transform.position, surroundings.nearestFoodSource.transform.position);
            }
            if (surroundings.nearestWaterTile != Mundo.invalid) {
                Gizmos.color = Color.blue; 
                Gizmos.DrawLine (transform.position, Mundo.centros[surroundings.nearestWaterTile.x, surroundings.nearestWaterTile.z]);
            }*/
            if(Mundo.aguaMasCercana[coord.x, coord.z] != Mundo.invalid){
                Gizmos.color = Color.black;
                Gizmos.DrawLine(SumarCeroCinco(coord), SumarCeroCinco(Mundo.aguaMasCercana[coord.x, coord.z]));
            }
            if(path != null){
                Gizmos.color = Color.green;
                for (int i = 0; i < path.Length; i++) {
                    Gizmos.DrawSphere (Mundo.centros[path[i].x, path[i].z], .2f);
                }
            }
            if(depredadorMasCercano != new Vector3(0,0,0)){
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(SumarCeroCinco(coord), SumarCeroCinco(coord - (depredadorMasCercano - coord)) );
            }
            if (currentAction == CreatureAction.GoingToFood && path != null) {
                //var path = EnvironmentUtility.GetPath (coord.x, coord.y, foodTarget.coord.x, foodTarget.coord.y);
                Gizmos.color = Color.black;
                for (int i = 0; i < path.Length; i++) {
                    Gizmos.DrawSphere (Mundo.centros[path[i].x, path[i].z], .2f);
                }
            }
            var mapa = Mundo.mapasEspecies[Species.Rabbit];
            var listaRegiones = mapa.RegionesVisibles(coord, maxViewDistance);
            for (int i = 0; i < listaRegiones.Count; i++){
                var colorcito = Color.black;
                colorcito.a = 0.3f;
                Gizmos.color = colorcito;
                Gizmos.DrawCube(mapa.centros[listaRegiones[i].Item1, listaRegiones[i].Item2], new Vector3(mapa.tamañoRegion, mapa.tamañoRegion, mapa.tamañoRegion));
            }
            var colorcito2 = Color.magenta;var colorcito3 = Color.yellow;
            colorcito2.a = 0.3f;colorcito3.a = 0.3f;
            Gizmos.color = colorcito2;
            
            var animales = mapa.ObtenerEntidades(coord, maxViewDistance);
            var aMAsCercano = mapa.EntidadMascercana(coord, maxViewDistance);
            foreach (var a in animales){
                Gizmos.DrawSphere(SumarCeroCinco(a.coord), 0.1f);
            }
            Gizmos.color = colorcito3;
            if(aMAsCercano != null)
                Gizmos.DrawSphere(aMAsCercano.coord, 0.1f);
        }
    }
}