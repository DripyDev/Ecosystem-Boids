using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class Pathfinder {
    //------------------------------------------ALGORITMO A*---------------------------------------------
    //La idea es solo usarlo cuando Bresenham no funciona porque no seria muy eficiente que siempre buscaran con este algoritmo
    ///<summary>Algoritmo de busqueda A*. Dado un origen (x,y), un destino (x1,y1) y el mapa de nodos sobre el que se va a operar. Devuelve el camino segun A* evitando obstaculos</summary>
    public static Vector3Int[] AStar(int x, int y, int x1, int y1, Mundo.Nodo[,] mapaNodos){
        if(!mapaNodos[x,y].caminable){
            Debug.Log("Partimos de no caminable, devolvemos null");
            return null;
        }

        List<Mundo.Nodo> openList = new List<Mundo.Nodo>();
        List<Mundo.Nodo> closedList = new List<Mundo.Nodo>();

        //Inicializamos el primer nodo
        mapaNodos[x,y].g = 0;
        mapaNodos[x,y].h = CalcularH(mapaNodos[x,y], mapaNodos[x1,y1]);
        mapaNodos[x,y].f = mapaNodos[x,y].g + mapaNodos[x,y].f;

        openList.Add(mapaNodos[x,y]);
        Mundo.Nodo nodoActual = mapaNodos[x,y];
        while(openList.Count > 0){
            nodoActual = NodoMenorF(openList);

            //Hemos llegado al objetivo
            if(nodoActual.centro == mapaNodos[x1,y1].centro){
                //return CalcularPath(mapaNodos[x1,y1], mapaNodos).ToArray();
                return CalcularPath(nodoActual, mapaNodos).ToArray();
            }

            openList.Remove(nodoActual);
            closedList.Add(nodoActual);
            List<Mundo.Nodo> vecinos = NodosVecinos(nodoActual, mapaNodos);
            //print("Numero vecinos: " + vecinos.Count);
            foreach (Mundo.Nodo v in vecinos){
                Mundo.Nodo aux = v;

                //Si el vecino esta en closed list, lo saltamos
                if(closedList.Contains(aux))
                    continue;

                //Si es una tile no caminable, la metemos en closed list
                if(!aux.caminable){
                    //if(openList.Contains(aux))
                    //    openList.Remove(aux);
                    closedList.Add(aux);
                    continue;
                }

                int tentativeG = nodoActual.g + CalcularH(nodoActual, aux);
                //print("TentativeG: " + tentativeG);
                //print("Valor g del vecino: " + aux.g);
                if(tentativeG < aux.g){
                    aux.vieneDe = Indice(nodoActual, mapaNodos);
                    aux.g = tentativeG;
                    aux.h = CalcularH(aux, mapaNodos[x1,y1]);
                    aux.f = aux.g+aux.h;
                    
                    var indiceAux = Indice(aux, mapaNodos);
                    mapaNodos[indiceAux.Item1, indiceAux.Item2] = aux;
                    if(!openList.Contains(aux)){
                        openList.Add(aux);
                    }
                }
            }
        }
        Debug.Log("No hemos encontrado el camino con A*");
        return null;
    }
    
    private static int CalcularH(Mundo.Nodo a, Mundo.Nodo b){
        //Hay que dividir entre las dimensiones del cubo que en nuestro caso es 1
        int xD = (int) Mathf.Abs(a.centro.x - b.centro.x);
        int zD = (int) Mathf.Abs(a.centro.z - b.centro.z);
        int resto = (int) Mathf.Abs(xD - zD);
        return 14*Math.Min(xD,zD) + 10*resto;
    }

    private static Mundo.Nodo NodoMenorF(List<Mundo.Nodo> lista){
        Mundo.Nodo menor = lista[0];
        foreach (var n in lista){
            if(n.f < menor.f)
                menor = n;
        }
        return menor;
    }

    private static List<Vector3Int> CalcularPath(Mundo.Nodo a, Mundo.Nodo[,] mapa){
        List<Vector3Int> res = new List<Vector3Int>();
        res.Add(a.centro);
        Mundo.Nodo actual = a;
        while(actual.vieneDe != (-1,-1)){
            res.Add(mapa[actual.vieneDe.Item1, actual.vieneDe.Item2].centro);
            actual = mapa[actual.vieneDe.Item1, actual.vieneDe.Item2];
            //var aux = mundo[actual.cameFrom];
            //aux.prefab.material.color = Color.green;
            //mundo[actual.cameFrom] = aux;
        }
        return DarVuelta(res);
    }

    ///<summary>Dada una lista, la devuelve dada la vuelta</summary>
    private static List<Vector3Int> DarVuelta(List<Vector3Int> lista){
        List<Vector3Int> aux = new List<Vector3Int>();
        for (int i = lista.Count-1; i >= 0; i--){
            aux.Add(lista[i]);
        }
        //Eliminamos el primer nodo que es nuestra posicion original
        if(aux.Count > 0)
            aux.RemoveAt(0);
        return aux;
    }

    //Devuelve el indice de a en el mundo
    private static (int,int) Indice(Mundo.Nodo a, Mundo.Nodo[,] mapa){
        for (int x = 0; x < mapa.GetLength(0); x++){
            for (int y = 0; y < mapa.GetLength(1); y++){
                if(a.centro == mapa[x,y].centro)
                    return (x,y);
            }   
        }
        //Error, no esta en la lista
        return (-1,-1);
    }

    ///<summary>Dado un nodo y el mapa del que forma parte. Devuelve todos sus nodos vecinos (8 como maximo)</summary>
    private static List<Mundo.Nodo> NodosVecinos(Mundo.Nodo a, Mundo.Nodo[,] mapa){
        List<Mundo.Nodo> vec = new List<Mundo.Nodo>();
        if(a.centro.x -1 >= 0){
            vec.Add(mapa[a.centro.x-1, a.centro.z]);//Izquierda
            if(a.centro.z-1 >= 0)
                vec.Add(mapa[a.centro.x-1, a.centro.z-1]);//Izquierda abajo
            if(a.centro.z+1 < Mundo.tamaño)
                vec.Add(mapa[a.centro.x-1, a.centro.z+1]);//Izquierda arriba
        }
        if(a.centro.x + 1 < Mundo.tamaño){
            vec.Add(mapa[a.centro.x+1, a.centro.z]);//Derecha
            if(a.centro.z-1 >= 0)
                vec.Add(mapa[a.centro.x+1, a.centro.z-1]);//Derecha abajo
            if(a.centro.z+1 < Mundo.tamaño)
                vec.Add(mapa[a.centro.x+1, a.centro.z+1]);//Derecha arriba
        }
        if(a.centro.z-1 >= 0)
            vec.Add(mapa[a.centro.x, a.centro.z-1]);//Abajo
        if(a.centro.z+1 < Mundo.tamaño)
            vec.Add(mapa[a.centro.x, a.centro.z+1]);//Arriba
        return vec;
    }

    //--------------------------------------------ALGORITMO BRESENHAM------------------------------------------
    //Una mierda, solo sirve si vamos de izquierda a derecha
    /*public static Vector3Int[] BresenhamError2(int x1, int y1, int x2, int y2) { 
        List<Vector3Int> pathAux = new List<Vector3Int>();
        int m_new = 2 * (y2 - y1); 
        int slope_error_new = m_new - (x2 - x1); 
        for (int x = x1, y = y1; x <= x2; x++){
            if(!Environment.walkable[x,y])
                return null;
            pathAux.Add(new Vector3Int(x,0,y));
            //Console.Write("(" + x + "," + y + ")\n"); 
  
            // Add slope to increment angle formed 
            slope_error_new += m_new; 
  
            // Slope error reached limit, time to 
            // increment y and update slope error. 
            if (slope_error_new >= 0) { 
                y++; 
                slope_error_new -= 2 * (x2 - x1); 
            } 
        }
        return pathAux.ToArray();
    }*/

    ///<summary>Devuelve el camino desde (x,y) hasta (x1,y1) siguiendo el algoritmo de Bresenham</summary>
    public static Vector3Int[] BresenhamError(int x, int y, int x1, int y1){
        List<Vector3Int> pathAux = new List<Vector3Int>();
        //Debug.Log("Vamos desde: " + x + " " + y + " a: " + x1 + " " + y1);
        
        int dx = Math.Abs(x1-x);
        var sx = x < x1? 1:-1;
        int dy = -Math.Abs(y1-y);
        //int dy = Math.Abs(y1-y);
        var sy = y < y1? 1:-1;
        //Caso en el que sea linea horizontal o vertical no hace falta usar el algoritmo
        if(dx == 0 || dy == 0)
            return BresenhamRecto(x, y, x1, y1);
        var err = dx+dy;
        while(true){
            //Nos hemos pasado y algo ha fallado, devolvemos null
            if(x > Mundo.caminable.GetLength(0)-1 || y > Mundo.caminable.GetLength(1)-1)
                return null;
            //Si la seccion no es caminable pero no es el objetivo intentamos bordear
            if(!Mundo.caminable[x,y] && (x != x1 && y != y1) ){
                //NOTA: DE MOMENTO DEVOLVEMOS NULL, deberiamos de intentar usar A* para encontrar el camino
                Debug.Log("El camino es unwalkable, devolvemos hasta donde podemos llegar");
                /*if(pathAux.Count > 0)
                    pathAux.RemoveAt(0);
                return pathAux.ToArray();*/
                return null;
            }
            //El caminos es caminable, seguimos
            else{
                pathAux.Add(new Vector3Int(x, 0, y));
            }
            //Somos vecinos o el mismo, salimos
            if(dx <=1 && Math.Abs(dy) <= 1){
                return null;
            }
            //Hemos llegado al final, salimos del bucle
            //if(x==x1 && y==y1)
            if( (x==x1-1 && y==y1-1) || (x==x1 && y==y1) )
                break;

            var e2 = 2*err;
            if(e2>=dy){
                err += dy;
                x+=sx;
            }
            if(e2 <= dx){
                err += dx;
                y+=sy;
            }
        }
        pathAux.RemoveAt(0);//Eliminamos el primer punto que es nuestra posicion original
        return pathAux.ToArray();
    }

    ///<summary>El camino desde (x,y) hasta (x1,y1) que en este caso es una linea recta asi que es un caso especial</summary>
    public static Vector3Int[] BresenhamRecto(int x, int y, int x1, int y1){
        List<Vector3Int> pathAux = new List<Vector3Int>();
        //Para saber si tenemos que avanzar o retroceder en los ejes
        var sx = x < x1? 1:-1;
        var sy = y < y1? 1:-1;

        while(true){
            //El camino no es caminable. Devolvemos hasta donde podemos llegar
            if(!Mundo.caminable[x,y] ){
                /*if(pathAux.Count > 0)
                    pathAux.RemoveAt(0);//Eliminamos el primer punto que es nuestra posicion original
                return pathAux.ToArray();*/
                return null;
            }
            pathAux.Add(new Vector3Int(x, 0, y));
            //Hemos llegado al final
            if((x==x1-1 && y==y1-1) || (x==x1 && y==y1))
                break;
            if(x != x1){
                x += sx;
            }
            if(y != y1){
                y += sy;
            }
        }
        pathAux.RemoveAt(0);//Eliminamos el primer punto que es nuestra posicion original
        return pathAux.ToArray();
    }

    //NOTA: SECCION VISIBLE USA BRESENHAM ASI QUE NUNCA VAMOS A USAR A* PORQUE LAS ENTIDADES QUE NO ESTAN A LA VISTA NOS LAS VAMOS A INTENTAR BUSCAR

    ///<summary>Devuelve si hay un camino sin interrumpir desde (x,y) hasta (x1,y1)</summary>
    public static bool SeccionVisible (int x, int y, int x1, int y1) {
        int dx = Math.Abs(x1-x);
        var sx = x < x1? 1:-1;
        int dy = -Math.Abs(y1-y);
        var sy = y < y1? 1:-1;
        //Caso en el que sea linea horizontal o vertical no hace falta usar el algoritmo
        if(dx == 0 || dy == 0){
            var rectaVisible = BresenhamRecto(x, y, x1, y1);
            return rectaVisible == null? false:true;
        }
        var err = dx+dy;
        while(true){
            //Si hay una parte no caminable, el tile no es visible
            if(!Mundo.caminable[x,y]){
                return false;
            }
            //Somos vecinos o el mismo, salimos
            if(dx <=1 && dy <= 1)
                return true;
            //Hemos llegado al final, salimos del bucle
            if(x==x1 && y==y1)
                break;
            var e2 = 2*err;
            if(e2>=dy){
                err += dy;
                x+=sx;
            }
            if(e2 <= dx){
                err += dx;
                y+=sy;
            }
        }
        return true;
    }
}
