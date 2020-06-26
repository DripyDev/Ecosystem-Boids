using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Encargado de actualizar el panel de informacion del animal clickado
/// </summary>
public class InformacionAnimal : MonoBehaviour
{
    //Animal cuya informacion exponemos
    public Animal animal;
    //Textos donde escribimos la informacion del animal
    Text hambreText;
    Text sedText;
    Text edadText;
    Text reproductiveUrgeText;
    Text embarazadaText;
    Text currentActionText;
    //Radio de vision del animal
    GameObject esfera;

    void Start()
    {      
        //Informacion que solo hace falta buscar una vez
        esfera = GameObject.Find("EsferaVision");
        hambreText = GameObject.Find("HambreText").GetComponent<Text>();
        sedText = GameObject.Find("SedText").GetComponent<Text>();
        edadText = GameObject.Find("EdadText").GetComponent<Text>();
        reproductiveUrgeText = GameObject.Find("ReproductiveUrgeText").GetComponent<Text>();
        embarazadaText = GameObject.Find("EmbarazadoText").GetComponent<Text>();
        currentActionText = GameObject.Find("CurrentActionText").GetComponent<Text>();
    }

    ///<summary>Dado un animal, exponemos su informacion y colocamos la esfera de vision sobre el.</summary>
    public void SetAnimal(Animal animalObjetivo){
        animal = animalObjetivo;
        esfera.GetComponent<MeshRenderer>().enabled = true;
        //De esta forma en lugar de una esfera es una circunferencia en xy (con 0 el shader lo renderiza mal)
        //maxViewDistance*2 porque es el radio, no el diametro
        esfera.transform.localScale = new Vector3(animalObjetivo.maxViewDistance*2, 1f, animalObjetivo.maxViewDistance*2);
        esfera.transform.position = animal.transform.position;
    }

    void Update()
    {
        if (animal != null){
            hambreText.text = animal.hunger.ToString();
            sedText.text = animal.thirst.ToString();
            edadText.text = animal.edad.ToString();
            reproductiveUrgeText.text = animal.reproductiveUrge.ToString();
            embarazadaText.text = animal.embarazada.ToString();
            currentActionText.text = animal.currentAction.ToString();
            esfera.transform.position = animal.transform.position;
        }
        else{
            hambreText.text = "";
            sedText.text = "";
            esfera.GetComponent<MeshRenderer>().enabled = false;
        }
    }
}
