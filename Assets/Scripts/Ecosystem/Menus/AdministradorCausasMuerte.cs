using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AdministradorCausasMuerte : MonoBehaviour
{
    //Arrays con el numero de causas de muerte. x[0] son las de zorro y x[1] las de conejos
    private static int[] eaten = new int[2];
    private static int[] hunger = new int[2];
    private static int[] thirst = new int[2];
    private static int[] age = new int[2];
    //Arrays con los textos de las causas de muerte. x[0] el zorro y x[1] conejos
    public static Text[] eatenText = new Text[2];
    public static Text[] hungerText = new Text[2];
    public static Text[] thirstText = new Text[2];
    public static Text[] ageText = new Text[2];

    public static void ActualizarCausas(Species especie, CauseOfDeath causa){
        //Actualizamos los contadores
        switch (causa) {
            case CauseOfDeath.Eaten:
                if(especie == Species.Fox)
                    eaten[0]+=1;
                    eatenText[0].text = eaten[0].ToString();
                if(especie == Species.Rabbit)
                    eaten[1]+=1;
                    eatenText[1].text = eaten[1].ToString();
            break;
            case CauseOfDeath.Hunger:
                if(especie == Species.Fox)
                    hunger[0]+=1;
                    hungerText[0].text = hunger[0].ToString();
                if(especie == Species.Rabbit)
                    hunger[1]+=1;
                    hungerText[1].text = hunger[1].ToString();
            break;
            case CauseOfDeath.Thirst:
                if(especie == Species.Fox)
                    thirst[0]+=1;
                    thirstText[0].text = thirst[0].ToString();
                if(especie == Species.Rabbit)
                    thirst[1]+=1;
                    thirstText[1].text = thirst[1].ToString();
            break;
            case CauseOfDeath.Age:
                if(especie == Species.Fox)
                    age[0]+=1;
                    ageText[0].text = age[0].ToString();
                if(especie == Species.Rabbit)
                    age[1]+=1;
                    ageText[1].text = age[1].ToString();
            break;
        }
    }

    // Start is called before the first frame update
    void Start() {
        print("se llama a start");
        eatenText[0] = transform.Find("ZorrosText").transform.Find("ZorroEatenInput").GetComponent<Text>(); eatenText[1] = transform.Find("ConejosText").transform.Find("ConejoEatenInput").GetComponent<Text>();
        hungerText[0] = transform.Find("ZorrosText").transform.Find("ZorroHungerInput").GetComponent<Text>(); hungerText[1] = transform.Find("ConejosText").transform.Find("ConejoHungerInput").GetComponent<Text>();
        thirstText[0] = transform.Find("ZorrosText").transform.Find("ZorroThirstInput").GetComponent<Text>(); thirstText[1] = transform.Find("ConejosText").transform.Find("ConejoThirstInput").GetComponent<Text>();
        ageText[0] = transform.Find("ZorrosText").transform.Find("ZorroAgeInput").GetComponent<Text>(); ageText[1] = transform.Find("ConejosText").transform.Find("ConejoAgeInput").GetComponent<Text>();
        if(eatenText[0] == null)
            print("No se han guardado las referencias");
        else
            print("Se ha guardado la referencia");
        //this.gameObject.SetActive(false);
    }
}
