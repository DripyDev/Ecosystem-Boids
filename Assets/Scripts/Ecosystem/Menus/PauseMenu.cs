using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public static bool gameIsPaused = false;
    public GameObject pauseUI;
    public GameObject fondo;
    public Text tiempo;
    public GameObject menuGraficas;
    public GameObject informacionAnimal;
    public GameObject barraTiempo;
    public GameObject grafica;

    void Update()
    {
        tiempo.text = Time.timeScale.ToString();
        if(Input.GetKeyDown(KeyCode.Escape)){
            if(gameIsPaused){
                Resume();
            }
            else{
                 Pause();
            }
        }
        if(Input.GetKeyDown(KeyCode.Tab)){
            MenuGraficas();
        }
    }

    public void MenuGraficas(){
        //Desactivamos el menu de graficas y reactivamos la barra de tiempo y informacion del animal
        if(menuGraficas.activeSelf) {
            menuGraficas.SetActive(false);
            barraTiempo.SetActive(true);
            informacionAnimal.SetActive(true);
            grafica.SetActive(true);
        }
        else {
            menuGraficas.SetActive(true);
            barraTiempo.SetActive(false);
            informacionAnimal.SetActive(false);
            grafica.SetActive(false);
        }
    }

    public void Resume(){
        pauseUI.SetActive(false);
        Time.timeScale = 1f;
        gameIsPaused = false;
        fondo.SetActive(false);
    }
    public void Pause(){
        fondo.SetActive(true);
        pauseUI.SetActive(true);
        Time.timeScale = 0f;
        gameIsPaused = true;
    }

    public void cambiarTiempo(float time){
        Time.timeScale = time;
    }
}
