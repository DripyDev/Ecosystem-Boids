using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public GameObject menu;
    public BoidSettings settings;

    // Update is called once per frame
    void Update() {
        if(Input.GetKeyDown(KeyCode.Tab)){
            MenuGraficas();
        }
    }
    void MenuGraficas(){
        if(menu.activeSelf)
            menu.SetActive(false);
        else
            menu.SetActive(true);
    }

    public void CambiarTiempo(float tiempo){
        Time.timeScale = tiempo;
    }
    public void CambiarCohesion(float cohesion){
        settings.pesoCohesion = cohesion;
    }
    public void CambiarSeparacion(float separacion){
        settings.pesoSeparacion = separacion;
    }
    public void CambiarAlineacion(float alineacion){
        settings.pesoAlineacion = alineacion;
    }
    public void CambiarRadioVision(float percepcion){
        settings.radioPercepcion = percepcion;
    }
    public void CambiarDstSeparacion(float separacion){
        settings.dstSeparacion = separacion;
    }
}
