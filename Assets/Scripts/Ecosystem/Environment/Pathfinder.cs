using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class Pathfinder {
    
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

    ///<summary>Devuelve el camino desde (x,y) hasta (x1,y1) siguiendo el algoritmo de Brasenham</summary>
    public static Vector3Int[] BresenhamError(int x, int y, int x1, int y1){
        List<Vector3Int> pathAux = new List<Vector3Int>(); var xOriginal = x; var yOriginal = y;
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
            //Si la seccion no es caminable pero no somos vecinos del objetivo intentamos bordear
            if(!Mundo.caminable[x,y] && x != xOriginal && y != yOriginal){
                //NOTA: DE MOMENTO DEVOLVEMOS NULL, EN EL FUTURO INTENTAR BORDEAR O ALGO
                //Debug.Log("El camino es unwalkable");
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
            //El camino no es caminable. Path no valido
            if(!Mundo.caminable[x,y] && Math.Abs(x1-x) != 0 && Math.Abs(y1-y) != 0)
                return null;
            pathAux.Add(new Vector3Int(x, 0, y));
            //if(x==x1 && y==y1)
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
