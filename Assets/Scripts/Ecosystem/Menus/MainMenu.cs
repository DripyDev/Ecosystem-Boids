using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TerrainGeneration;

public class MainMenu : MonoBehaviour {
    public Mundo mundo;
    public TerrainGenerator terrainGen;
    //Funcion para cargar la escena del juego, llamarla al darle al boton de jugar
    public void PlayGame(){
        //environment = GameObject.Find("Environment").GetComponent<Environment>();
        //terrainGen.Generate();
        mundo.Start();

        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    } 

    //Salimos del juego, funcion a llamar cuando le dan a Salir
    public void QuitGame(){
        Application.Quit();
        print("QUIT");
    }
}
