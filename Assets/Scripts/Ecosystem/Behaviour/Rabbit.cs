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
    public Coord depredadorMasCercano;
    void Start() {
        var aux = new System.Random();
        valorNutricional = Mathf.Max((float) (aux.Next(60)/100), 0.3f);
    }
    
    public void Update() {
        // Increase hunger, thirst and age over time
        //Si somos mas rapidos pasaremos mas hambre y sed
        hunger += Time.deltaTime * 1 / timeToDeathByHunger * (moveSpeed/1.5f);
        thirst += Time.deltaTime * 1 / timeToDeathByThirst * (moveSpeed/1.5f);
        edad += Time.deltaTime * 1 / timeToDeathByAge * ratioCrecimiento;
        reproductiveUrge += Time.deltaTime * 1 / 350 * ratioReproductiveUrge;
        Crecer();
        comprobarEmbarazo();

        
        //material.color += new Color(material.color.r,edad,edad,0);

        // Animate movement. After moving a single tile, the animal will be able to choose its next action
        //puede que haya que cambiar esto para simular la velocidad¿?¿?¿?
        if (animatingMovement) {
            //print("-------------INICIO MOVIMIENTO---------------------");
            AnimateMove();
        } else {
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
        Coord coordDepredadorCercano = Environment.SenseDepredador(species, coord, maxViewDistance);
        //NOTA: Cambiar el species!=Species.Fox porque en el futuro puede que haya más animales. Cambiarlo a buscar en el diccionario de depredadores
        if (coordDepredadorCercano.x+coordDepredadorCercano.y != 0 && species != Species.Fox) {
            HuirDepredador(coordDepredadorCercano);
        }
        //NOTA: Repasar cuando elige que accion
        else {
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
            Act ();
        }
    }

    

    ///<summary>Huimos en direccion completamente opuesta al depredador mas cercano</summary>
    private void HuirDepredador(Coord depredador){
        depredadorMasCercano = depredador;
        currentAction = CreatureAction.Fleeing;
        CreatePath(base.coord + (base.coord - depredador));
    }

    public override void Init (Coord coord) {
        //print("Init de Rabbit");
        base.Init(coord);
    }
    public void OnMouseDown(){
        //print("Click en mi");
        GameObject.Find("InformacionAnimal").GetComponent<InformacionAnimal>().SetAnimal(this);
    }
}