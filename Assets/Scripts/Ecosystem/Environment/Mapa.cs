using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mapa {
    public Vector3[,] centros;
    public List<LivingEntity>[,] mapaAnimales ;
    public int tamañoRegion;
    public int numeroRegiones;
    public int numeroEntidades;

    //Contructor del mapa
    public Mapa(int tamañoMapa, int tamañoR){
        this.tamañoRegion = tamañoR;
        this.numeroRegiones = tamañoMapa/tamañoRegion;
        this.centros = new Vector3[numeroRegiones, numeroRegiones];
        this.mapaAnimales = new List<LivingEntity>[numeroRegiones,numeroRegiones];
        var aux = new Vector3(tamañoRegion/2, 0, tamañoRegion/2);
        for (int x = 0; x < numeroRegiones; x++){
            //El eje z es la profundidad en unity
            for (int z = 0; z < numeroRegiones; z++){
                var cent = aux;
                aux = new Vector3(aux.x * x, 0, aux.z*z);
                centros[x,z] = cent;
                mapaAnimales[x,z] = new List<LivingEntity>();
            }
        }
    }

    public List<LivingEntity> GetEntities(Coord origen, float distanciaVision){
        List<LivingEntity> lista = new List<LivingEntity>();
        List<(int,int)> indicesRegionesVisibles = RegionesVisibles(origen, distanciaVision);
        for (int i = 0; i < indicesRegionesVisibles.Count; i++){
            var x = indicesRegionesVisibles[i].Item1; var y = indicesRegionesVisibles[i].Item2;
            var listaAniumalesAux = mapaAnimales[x,y];
            foreach (var ent in listaAniumalesAux){
                if(Coord.Distance(ent.mapCoord, origen) <= distanciaVision){
                    lista.Add(ent);
                }
            }
        }
        return lista;
    }

    public LivingEntity EntidadMascercana(Coord origen, float distanciaVision){
        var lista = GetEntities(origen, distanciaVision);
        lista.Sort( (a,b) => Coord.Distance(origen, a.coord).CompareTo(Coord.Distance(origen, b.coord)) );
        return lista[0];
    }

    public List<(int,int)> RegionesVisibles(Coord origen, float distanciaVision){
        List<(int,int)> regiones = new List<(int,int)>();
        for (int i = 0; i < centros.Length; i++) {
            for (int j = 0; j < centros.Length; j++){
                //Si la distancia es menos, entra y guardamos su indice
            }
            
        }
        return regiones;
    }

    //Dada una entidad y sus coordenadas, las añade en el mapa
    //NOTA: REPASAR Y CAMBIAR
    public void Añadir(LivingEntity ent, Coord c){
        int regionX = c.x / tamañoRegion;
        int regionY = c.y / tamañoRegion;
        int index = mapaAnimales[regionX, regionY].Count;
        // store the entity's index in the list inside the entity itself for quick access
        ent.mapIndex = index;
        ent.mapCoord = c;
        mapaAnimales[regionX, regionY].Add(ent);
        numeroEntidades++;

    }


}
