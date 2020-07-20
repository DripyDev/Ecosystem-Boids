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
        
        var valorX = tamañoRegion/2; var valorZ = tamañoRegion/2;
        for (int x = 0; x < numeroRegiones; x++){
            if(x!=0)
                valorX += tamañoRegion;
            //El eje z es la profundidad en unity
            for (int z = 0; z < numeroRegiones; z++){
                if(z!=0)
                    valorZ += tamañoRegion;
                else
                    valorZ = tamañoRegion/2;
                var cent = new Vector3(valorX, 0, valorZ);
                //Debug.Log("Centro de cubo en: X: " + valorX + " Y: " + 0 + " Z: " + valorZ);
                centros[x,z] = cent;
                mapaAnimales[x,z] = new List<LivingEntity>();
            }
        }
    }

    ///<summary>Devuelve las entidades dentro del rango de vision</summary>
    public List<LivingEntity> ObtenerEntidades(Vector3Int origen, float distanciaVision){
        List<LivingEntity> lista = new List<LivingEntity>();
        List<(int,int)> indicesRegionesVisibles = RegionesVisibles(origen, distanciaVision);
        for (int i = 0; i < indicesRegionesVisibles.Count; i++){
            var x = indicesRegionesVisibles[i].Item1; var y = indicesRegionesVisibles[i].Item2;
            var listaAniumalesAux = mapaAnimales[x,y];
            foreach (var ent in listaAniumalesAux){
                if(Vector3.Distance(origen, ent.coord) <= distanciaVision){
                    lista.Add(ent);
                }
            }
        }
        return lista;
    }

    ///<summary>Devuelve la entidad mas cercana a nosotros</summary>
    public LivingEntity EntidadMascercana(Vector3Int origen, float distanciaVision){
        var lista = ObtenerEntidades(origen, distanciaVision);
        //lista.Sort( (a,b) => Coord.Distance(origen, a.coord).CompareTo(Coord.Distance(origen, b.coord)) );
        lista.Sort( (a,b) => Vector3Int.Distance(origen, a.coord).CompareTo(Vector3Int.Distance(origen, b.coord)) );
        if(lista.Count > 0)
            return lista[0];
        else
            return null;
    }

    ///<summary>Devuelve las regiones del mapa a la vista desde origen</summary>
    public List<(int,int)> RegionesVisibles(Vector3Int origen, float distanciaVision){
        /*List<(int,int)> regions = new List<(int,int)>();
        //Region del mapa a la que pertenecemos
        int originRegionX = origen.x / tamañoRegion;
        int originRegionY = origen.z / tamañoRegion;
        float sqrViewDst = distanciaVision * distanciaVision;
        var aux = new Vector2(origen.x, origen.z);
        Vector2 viewCentre = aux + Vector2.one * .5f;

        //El numero maximo de regiones que podriamos ver
        int searchNum = Mathf.Max (1, Mathf.CeilToInt (distanciaVision / tamañoRegion));
        // Loop over all regions that might be within the view dst to check if they actually are
        for (int offsetY = -searchNum; offsetY <= searchNum; offsetY++) {
            for (int offsetX = -searchNum; offsetX <= searchNum; offsetX++) {
                int viewedRegionX = originRegionX + offsetX;
                int viewedRegionY = originRegionY + offsetY;
                //Comprovamos que sean regiones validas
                if (viewedRegionX >= 0 && viewedRegionX < numeroRegiones && viewedRegionY >= 0 && viewedRegionY < numeroRegiones) {
                    // Calculate distance from view coord to closest edge of region to test if region is in range
                    float ox = Mathf.Max (0, Mathf.Abs (viewCentre.x - centros[viewedRegionX, viewedRegionY].x) - tamañoRegion / 2f);
                    float oy = Mathf.Max (0, Mathf.Abs (viewCentre.y - centros[viewedRegionX, viewedRegionY].y) - tamañoRegion / 2f);
                    float sqrDstFromRegionEdge = ox * ox + oy * oy;
                    if (sqrDstFromRegionEdge <= sqrViewDst) {
                        regions.Add((viewedRegionX, viewedRegionY));
                    }
                }
            }
        }*/
        //return regions;
        List<(int,int)> regiones = new List<(int,int)>();
        int regionX = origen.x/tamañoRegion; int regionY = origen.z/tamañoRegion;
        int maximoRegionesVisibles = (int) Mathf.Max(1, distanciaVision/tamañoRegion);
        for (int x = -maximoRegionesVisibles; x < maximoRegionesVisibles; x++){
            for (int y = -maximoRegionesVisibles; y < maximoRegionesVisibles; y++){
                int regionVisibleX = regionX + x; int regionVisibleY = regionY + y;
                if( regionVisibleX >= 0 && regionVisibleX < numeroRegiones && regionVisibleY >= 0 && regionVisibleY < numeroRegiones){
                    if(Vector3.Distance(centros[regionVisibleX, regionVisibleY], origen) <= distanciaVision){
                        regiones.Add( (regionVisibleX, regionVisibleY) );
                    }
                }
            }
        }
        return regiones;
    }

    //Dada una entidad y la region del mapa a la que pertenece, lo añade en la lista de entidades del mapa
    public void Añadir(LivingEntity ent, Vector3Int coord){
        int regionX = coord.x/tamañoRegion; int regionY = coord.z/tamañoRegion;
        int indice = mapaAnimales[regionX, regionY].Count;
        //Guardamos el indice de la lista de seres de la region
        ent.mapIndex = indice;
        ent.mapCoord = new Vector2Int(regionX, regionY);
        mapaAnimales[regionX, regionY].Add(ent);
        numeroEntidades++;
    }

    //Dada una entidad y la region del mapa a la que pertenece. Lo elimina de la lista de entes
    public void Eliminar(LivingEntity ent, Vector3Int coord){
         int regionX = coord.x/tamañoRegion; int regionY = coord.z/tamañoRegion;
        //Para no usar .Remove que tiene que buscar en toa la lista, vamos a usar indices
        int indice = ent.mapIndex;
        int ultimoIndice = mapaAnimales[regionX, regionY].Count - 1;
        //Por si no hay ninguna entidad en la region
        ultimoIndice = ultimoIndice < 0 ? 0 : ultimoIndice;
        //Si no estamos en el ultimo indice, colcoamos el ultimo en nuestro indice
        if (indice != ultimoIndice) {
            //Error con index en las siguientes dos lineas
            mapaAnimales[regionX, regionY][indice] = mapaAnimales[regionX, regionY][ultimoIndice];
            mapaAnimales[regionX, regionY][indice].mapIndex = ent.mapIndex;
        }
        //Eliminamos el ultimo elemento para ahorrarnos usar .Remove
        mapaAnimales[regionX, regionY].RemoveAt (ultimoIndice);
        numeroEntidades--;
    }

    ///<summary>Movemos la entidad eliminando y añadiendola en la region del mapa correspondiente si hace falta</summary>
    public void Mover(LivingEntity ent, Vector3Int de, Vector3Int a){
        //Debug.Log("Nos movemos de: " + de + " a: " + a);
        int regOX = de.x/tamañoRegion; int regOY = de.z/tamañoRegion;
        int regDX = a.x/tamañoRegion; int regDY = a.z/tamañoRegion;
        //Si la region de y a son iguales, no hace falta que hagamos ninguna operacion porque no nos vamos a mover de la region
        if(regOX != regDX || regOY != regDY){
            Eliminar(ent, de);
            Añadir(ent, a);
        }
    }
}
