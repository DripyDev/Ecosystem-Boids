using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BDI {
    ///<summary>Informacion de la que dispone el agente</summary>
    public enum Belief{
        depredador,
        posiblesParejas,
        pareja,
        hambre,
        sed,
        comida,
        agua
        
    }
    ///<summary>Objetivos que desea alcanzar el agente</summary>
    public enum Desire{
        sobrevivir,
        comer,
        beber,
        reproducirse
    }
    ///<summary>Son los deseos que el agente ha decidido intentar alcanzar. Tiene los mismos valores que Desire</summary>
    public enum Intentions{
        sobrevivir,
        comer,
        beber,
        reproducirse
    }
}
