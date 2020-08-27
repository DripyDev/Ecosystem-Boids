using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary>Encargado de dividir el mundo en subregiones (X.X.X) y administrar los Boids que se encuentran en cada region</summary>
public class RegionManager : MonoBehaviour {
    //Suponemos que el suelo que nos dan tiene dimensiones AxA. Vamos a dividir en cubos a partir de sus valores X y Z de Scale
    public GameObject sueloMundo;
    //Se elije la dimension del cubo y al cubo daria el numero de regiones. 512=7x7x7, 64=4x4x4, 216=6x6x6...
    ///<summary>Numero de regiones en las que queremos dividir el mapa.</summary>
    public static int numeroRegiones = 1000;
    ///<summary>Lista de Region donde se controla que boids hay en cada region. Se inicializa aqui y luego cada boid va avisando de donde esta para actualizar.</summary>
    public static List<Region> mapaRegiones = new List<Region>();

    //Los campos estaticos no aparecen en el inspector (excepto en modo debug)
    [Header ("Dimension regiones")]
    //Dimensiones de las regiones
    public static float xR, yR, zR;
    
    [Header ("Dimension suelo")]
    //Dimensiones de suelo
    public float X, Y, Z;
    
    void Awake(){
        //X = sueloMundo.transform.localScale.x;
        //De momento suponemos que la altura es igual a X. NOTA: CAMBIAR EN EL FUTURO
        //Y = X;
        //Z = sueloMundo.transform.localScale.z;
        //Dimensiones de las regiones
        xR = Mathf.Pow(((X*Y*Z)/numeroRegiones), (1f/3f) ); yR = xR; zR = xR;
        InicializarMapaRegiones();
    }

    //Quizas podriamos usar aqui la gpu? Asi las creamos mas rapidamente. No se podria porque los 3 fors son necesarios y no separables
    ///<summary>Divide el mapa en numeroRegiones regiones cubicas</summary>
    void InicializarMapaRegiones(){
        int raizCubo = (int) Mathf.Pow(numeroRegiones, (1f/3f));//64:4 / 216:6 / 1000:10
        Vector3 centroObjecto = sueloMundo.transform.position;
        //Centro de los cubos y sus dimensiones
        Vector3 pos, dimensiones;
        dimensiones = new Vector3(xR , yR, zR);
        //Centro de la primera region. Nota: la y es diferente porque es el centro del suelo que esta en y=0 del cubo que queremos subdividir
        //Centro cuando environment es NOT CENTERED
        //pos = new Vector3(centroObjecto.x - (-X/2) - xR/2, yR/2, centroObjecto.z - (-Z/2) - zR/2);
        //NOTA: REPASAR PORQUE COLOCA MAL LAS REGIONES DE LOS BOIDS, USAR - Mundo.tamaño/2f ?¿?¿
        //Centro cuando environment es CENTERED
        pos = new Vector3(xR/2, yR/2, zR/2);

        Vector3 posX, posY, posZ;
        posY = pos;
        Region regionAux;
        //Creamos los cubos de manera ordenada, asi luego podemos encontrar los adyacentes con operaciones de indices
        //Eje Y
        for (int a = 0; a < raizCubo; a++){
            posX = posY;
            posY.y += yR;
            //Eje X
            for (int b = 0; b < raizCubo; b++) {
                posZ = posX;
                posX.x += xR;
                //Eje Z
                for (int c = 0; c < raizCubo; c++) {
                    regionAux = new Region(posZ, dimensiones);
                    mapaRegiones.Add(regionAux);
                    posZ.z += zR;
                }
            }
        }
    }

    void OnDrawGizmosSelected() {
        float raizCubo = Mathf.Pow(numeroRegiones, (1f/3f));
        Color color = Color.red; color.a = 0.1f;
        //Dibujamos el primer cubo en azul para diferenciarlo
        Color colorA = Color.blue; colorA.a = 0.1f;
        Gizmos.color = colorA;
        foreach (var r in mapaRegiones) {
            Gizmos.DrawCube(r.posicion, r.dimensiones);
            Gizmos.color = color;
            color += new Color(1f/1000f,1f/1000f,1f/1000f);
            color.a = 0.1f;
            Gizmos.color = color;
        }
    }

    public static void EliminarDepredadorDeRegion(int indiceRegion, Depredador dep){
        mapaRegiones[indiceRegion].depredador.Remove(dep);
    }

    public static void AñadirDepredadorDeRegion(int indiceRegion, Depredador dep){
        mapaRegiones[indiceRegion].depredador.Add(dep);
    }

    ///<summary>Dado el indice de una region del mapa y el boid, lo elimina de la lista de boids de esa region.</summary>
    public static void EliminarBoidDeRegion(int indiceRegion, Boid boid){
        mapaRegiones[indiceRegion].boids.Remove(boid);
    }

    ///<summary>Dado el indice de una region del mapa y el boid, lo añade en la lista de boids de esa region.</summary>
    public static void AñadirBoidDeRegion(int indiceRegion, Boid boid){
        mapaRegiones[indiceRegion].boids.Add(boid);
    }

    ///<summary>Dada una posicion y el centro y dimensiones de un cubo, devuelve si la posicion esta dentro de un cubo con las dimensiones dadas</summary>
    public static bool DentroDeCubo(Vector3 pos, Vector3 centroCubo,  Vector3 dimensionesCubo){
        float xMin = centroCubo.x - dimensionesCubo.x/2; float xMax = centroCubo.x + dimensionesCubo.x/2;
        float yMin = centroCubo.y - dimensionesCubo.y/2; float yMax = centroCubo.y + dimensionesCubo.y/2;
        float zMin = centroCubo.z - dimensionesCubo.z/2; float zMax = centroCubo.z + dimensionesCubo.z/2;

        bool Xin = pos.x >= xMin && pos.x <= xMax; bool Yin = pos.y >= yMin && pos.y <= yMax; bool Zin = pos.z >= zMin && pos.z <= zMax;
        return Xin && Yin && Zin; 
    }

    ///<summary>Dada la posicion y una lista con las regiones adyacentes, devuelve la nueva region en la que estamos.
    ///No buscamos entre todas las regiones, asi es mas eficiente.</summary>
    public static int EncontrarRegionIndices(Vector3 pos, List<int> regionesAdyacentes){
        foreach (var r in regionesAdyacentes){
            if(DentroDeCubo(pos, mapaRegiones[r].posicion, mapaRegiones[r].dimensiones)){
                return r;
            }
        }
        //Si llega aqui es que no funciona o se ha escapado del mapa en cuyo caso nos da igual
        //ERROR
        return -1;
    }

    //Usada para encontrar la region inicial en la que estamos
    ///<summary>Dada una posicion, devuelve el indice de la region del mapa a la que pertenece</summary>
    public static int EncontrarRegion(Vector3 pos){
        int indice = 0;
        foreach (var r in mapaRegiones){
            if(DentroDeCubo(pos, r.posicion, r.dimensiones)){
                //return r;
                return indice;
            }
            indice+=1;
        }
        //Si llega aqui es que no funciona o se ha escapado del mapa en cuyo caso nos da igual
        //ERROR
        return -1;
    }

    //RegionesAdyacentes bueno
    ///<summary>Regiones adyacentes pero con las regiones creadas en un orden concreto. Entonces los adyacentes se pueden calcular con operaciones simples</summary>
    public static List<int> RegionesAdyacentes(int indiceRegion){
        List<int> indices = new List<int>();
        int regionesPorPlanta = (int) Mathf.Pow(numeroRegiones, (2f/3f) );//64:16 / 216:36
        int regionesPorFila = (int) Mathf.Pow(numeroRegiones, (1f/3f) );//64:4 / 216:6
        //print("Planta: " + regionesPorPlanta + " fila: " + regionesPorFila);

        int arriba = indiceRegion+regionesPorPlanta<numeroRegiones? indiceRegion+regionesPorPlanta : -1;
        int abajo = indiceRegion-regionesPorPlanta>=0? indiceRegion-regionesPorPlanta : -1;
        //int arriba = indiceRegion+16<numeroRegiones? indiceRegion+16 : -1;
        //int abajo = indiceRegion-16>=0? indiceRegion-16 : -1;

        //indice - planta
        //int izquierda = ( (indiceRegion - (((int)(indiceRegion/16))*16) ) < 4 )? -1 : indiceRegion-4;
        //int derecha = ( (indiceRegion - (((int)(indiceRegion/16))*16) ) > 11 )? -1 : indiceRegion+4;
        int izquierda = ( (indiceRegion - (((int)(indiceRegion/regionesPorPlanta))*regionesPorPlanta) ) < regionesPorFila )? -1 : indiceRegion-regionesPorFila;
        int derecha = ( (indiceRegion - (((int)(indiceRegion/regionesPorPlanta))*regionesPorPlanta) ) > (regionesPorPlanta-regionesPorFila-1) )? -1 : indiceRegion+regionesPorFila;

        //int delante = ((indiceRegion)%4==0)? -1 : indiceRegion-1;
        //int atras = ((indiceRegion-3)%4==0)? -1 : indiceRegion+1;
        int delante = ( ((indiceRegion)%regionesPorFila) ==0)? -1 : indiceRegion-1;
        int atras = ( ((indiceRegion- (regionesPorFila-1) )%regionesPorFila) ==0)? -1 : indiceRegion+1;

        //Diagonales
        //Piso superior
        /*int arribaDerecha = (arriba!=-1 && derecha != -1)? indiceRegion+16+4:-1;
        int arribaIzquierda = (arriba!=-1 && izquierda != -1)? indiceRegion+16-4:-1;
        int arribaDerechaAdelante = (arriba!=-1 && delante!=-1 && derecha != -1)? indiceRegion+16+4-1:-1;
        int arribaIzquierdaAdelante = (arriba!=-1 && delante!=-1 && izquierda != -1)? indiceRegion+16-4-1:-1;
        int arribaDerechaAtras = (arriba!=-1 && atras!=-1 && derecha != -1)? indiceRegion+16+4+1:-1;
        int arribaIzquierdaAtras = (arriba!=-1 && atras!=-1 && izquierda != -1)? indiceRegion+16-4+1:-1;
        int arribaAdelante = (arriba!=-1 && delante!=-1)? indiceRegion+16-1:-1;
        int arribaAtras = (arriba!=-1 && atras!=-1)? indiceRegion+16+1:-1;*/
        int arribaDerecha = (arriba!=-1 && derecha != -1)? indiceRegion+regionesPorPlanta+regionesPorFila:-1;
        int arribaIzquierda = (arriba!=-1 && izquierda != -1)? indiceRegion+regionesPorPlanta-regionesPorFila:-1;
        int arribaDerechaAdelante = (arriba!=-1 && delante!=-1 && derecha != -1)? indiceRegion+regionesPorPlanta+regionesPorFila-1:-1;
        int arribaIzquierdaAdelante = (arriba!=-1 && delante!=-1 && izquierda != -1)? indiceRegion+regionesPorPlanta-regionesPorFila-1:-1;
        int arribaDerechaAtras = (arriba!=-1 && atras!=-1 && derecha != -1)? indiceRegion+regionesPorPlanta+regionesPorFila+1:-1;
        int arribaIzquierdaAtras = (arriba!=-1 && atras!=-1 && izquierda != -1)? indiceRegion+regionesPorPlanta-regionesPorFila+1:-1;
        int arribaAdelante = (arriba!=-1 && delante!=-1)? indiceRegion+regionesPorPlanta-1:-1;
        int arribaAtras = (arriba!=-1 && atras!=-1)? indiceRegion+regionesPorPlanta+1:-1;

        //Piso inferior
        /*int abajoDerecha = (abajo!=-1 && derecha != -1)? indiceRegion-16+4:-1;
        int abajoIzquierda = (abajo!=-1 && izquierda != -1)? indiceRegion-16-4:-1;
        int abajoDerechaAdelante = (abajo!=-1 && delante!=-1 && derecha != -1)? indiceRegion-16+4-1:-1;
        int abajoIzquierdaAdelante = (abajo!=-1 && delante!=-1 && izquierda != -1)? indiceRegion-16-4-1:-1;
        int abajoDerechaAtras = (abajo!=-1 && atras!=-1 && derecha != -1)? indiceRegion-16+4+1:-1;
        int abajoIzquierdaAtras = (abajo!=-1 && atras!=-1 && izquierda != -1)? indiceRegion-16-4+1:-1;
        int abajoAdelante = (abajo!=-1 && delante!=-1)? indiceRegion-16-1:-1;
        int abajoAtras = (abajo!=-1 && atras!=-1)? indiceRegion-16+1:-1;*/
        int abajoDerecha = (abajo!=-1 && derecha != -1)? indiceRegion-regionesPorPlanta+regionesPorFila:-1;
        int abajoIzquierda = (abajo!=-1 && izquierda != -1)? indiceRegion-regionesPorPlanta-regionesPorFila:-1;
        int abajoDerechaAdelante = (abajo!=-1 && delante!=-1 && derecha != -1)? indiceRegion-regionesPorPlanta+regionesPorFila-1:-1;
        int abajoIzquierdaAdelante = (abajo!=-1 && delante!=-1 && izquierda != -1)? indiceRegion-regionesPorPlanta-regionesPorFila-1:-1;
        int abajoDerechaAtras = (abajo!=-1 && atras!=-1 && derecha != -1)? indiceRegion-regionesPorPlanta+regionesPorFila+1:-1;
        int abajoIzquierdaAtras = (abajo!=-1 && atras!=-1 && izquierda != -1)? indiceRegion-regionesPorPlanta-regionesPorFila+1:-1;
        int abajoAdelante = (abajo!=-1 && delante!=-1)? indiceRegion-regionesPorPlanta-1:-1;
        int abajoAtras = (abajo!=-1 && atras!=-1)? indiceRegion-regionesPorPlanta+1:-1;

        //Piso central
        /*int enfrenteDerecha = (delante!=-1 && derecha != -1)? indiceRegion-1+4:-1;
        int enfrenteIzquierda = (delante!=-1 && izquierda != -1)? indiceRegion-1-4:-1;
        int atrasDerecha = (atras!=-1 && derecha != -1)? indiceRegion+1+4:-1;
        int atrasIzquierda = (atras!=-1 && izquierda != -1)? indiceRegion+1-4:-1;*/
        int enfrenteDerecha = (delante!=-1 && derecha != -1)? indiceRegion-1+regionesPorFila:-1;
        int enfrenteIzquierda = (delante!=-1 && izquierda != -1)? indiceRegion-1-regionesPorFila:-1;
        int atrasDerecha = (atras!=-1 && derecha != -1)? indiceRegion+1+regionesPorFila:-1;
        int atrasIzquierda = (atras!=-1 && izquierda != -1)? indiceRegion+1-regionesPorFila:-1;

        //Regiones adyacentes sin incluir diagonales
        //int[] aux = {indiceRegion, arriba, abajo, izquierda, derecha, delante, atras};
        //Todas las regiones adyacentes, diagonales incluidas
        int[] aux = {indiceRegion, arriba, abajo, izquierda, derecha, delante, atras,
        arribaDerecha, arribaIzquierda, arribaDerechaAdelante, arribaIzquierdaAdelante, arribaDerechaAtras,arribaIzquierdaAtras, arribaAdelante, arribaAtras, 
        abajoDerecha, abajoIzquierda, abajoDerechaAdelante,abajoIzquierdaAdelante, abajoDerechaAtras, abajoIzquierdaAtras, abajoAdelante, abajoAtras,
        enfrenteDerecha, enfrenteIzquierda, atrasDerecha, atrasIzquierda};
        for (int i = 0; i < 27; i++){
            if(aux[i] >= 0)
                indices.Add(aux[i]);
        }
        return indices;
    }

    public struct Region{
        public Vector3 posicion;
        public Vector3 dimensiones;
        public List<Boid> boids;
        public List<Depredador> depredador;
        //Constructora
        public Region(Vector3 pos, Vector3 dim){
            this.posicion = pos;
            this.dimensiones = dim;
            this.boids = new List<Boid>();
            this.depredador = new List<Depredador>();
        }
    }

}
