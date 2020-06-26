using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;//Control de errores

public class Boid : MonoBehaviour {
    private BoidSettings settings;
    ///<summary>Direccion a la que nos dirigimos. direccion = velocidad/speed</summary>
    public Vector3 direccion;
    ///<summary>Velocidad de movimiento. speed = velocidad/direccion</summary>
    public float speed;
    ///<summary>Es la direccion(vector3) por la velocidad(float)</summary>
    public Vector3 velocidad;
    public List<Boid> todosBoids;
    public List<Boid> manada;
    //TEMPORAL PARA PRUEBAS CON SHADER, BORRAR EN EL FUTURO
    public int numeroManada;
    public List<Boid> boidsRegion;

    public List<Depredador> depredadoresRegion;
    public Depredador depredador;

    ///<summary>Indice de la region en la que esta el Boid actualmente</summary>
    public int region;
    public List<int> regionesAdyacentes;
    ///<summary>Indice de la region anterior donde estaba el Boid. Lo usamos para quitar el Boid de la lista de la region si hemos cambiado de region</summary>
    public int regionAnterior;
    ///<summary>Tiempo de vuelo. Cuando llega a 1 el boid devera descansar en el suelo</summary>
    public float tVuelo;
    ///<summary>Indica si estamos en el suelo descansando o no</summary>
    public bool descansando = false;
    //Temporal para gizmos
    public Vector3 r1,r2,r3,r4, r5;
    //Vector auxiliar para no escribir todo el rato vectores nulos
    private Vector3 cero = new Vector3(0,0,0);
    //TEMPORAL PARA PRUEBAS CON GPU
    public Spawner spawner;
    
    //Inicializamos settings, posicion, direccion, speed, velocidad y region del Boid
    public void Inicializar(BoidSettings set, Vector3 pos){
        settings = set;
        this.transform.position = pos;
        //Direccion inicial: adelante
        direccion = transform.forward;
        //Velocidad de movimiento inicial
        speed = UnityEngine.Random.Range(settings.maxSpeed, settings.minSpeed);
        //Velocidad inicial
        velocidad = direccion*speed;
        
        region = RegionManager.EncontrarRegion(transform.position);
        regionesAdyacentes = RegionManager.RegionesAdyacentes(region);
        regionAnterior = region;
        tVuelo=0f;
    }

    float Distancia(Vector3 destino, Vector3 origen){
        Vector3 vector = destino - origen;
        return Mathf.Sqrt(vector.x*vector.x + vector.y*vector.y +vector.z*vector.z);
    }

    //NOTA: IGUAL TODAS LAS FUNCIONES DE PERCIBIR DEBERIAN DE ESTAR EN EL REGION MANAGER?¿?¿?¿?¿?¿?¿

    //PRUEBA PARA USAR LA GPU EN ESTA FUNCION
    ///<summary>Devuelve una lista con los boids en el rango settings.radioPercepcion de nosotros. Recorre todos los boids de la Region a la que pertenecemos y las adyacentes.</summary>
    List<Boid> PercibirBoidsRegion(){
        List<Boid> manadaAux = new List<Boid>();
        //Percibimos los boids de las regiones nuestra y adyacentes
        foreach (var indice in regionesAdyacentes){
            foreach (var b in RegionManager.mapaRegiones[indice].boids){
                if(b!=this){
                    float distancia = Distancia(b.transform.position, this.transform.position);
                    //La segunda condicion es para no contarnos a nosotros mismos
                    if(distancia <= settings.radioPercepcion && distancia > 0){
                        manadaAux.Add(b);
                    }
                    //TEMPORAL PARA COMPROBAR LOS BOIDS QUE DETECTA EN LAS REGIONES SUYA Y ADYACENTES
                    if(!boidsRegion.Contains(b))
                        boidsRegion.Add(b);
                }
            }
        }
        return manadaAux;
    }

    List<Depredador> PercibirDepredadoresRegion(){
        List<Depredador> depredadoresAux = new List<Depredador>();
        //Percibimos los boids de las regiones nuestra y adyacentes
        foreach (var indice in regionesAdyacentes){
            foreach (var d in RegionManager.mapaRegiones[indice].depredador){
                float distancia = Distancia(d.transform.position, this.transform.position);
                //La segunda condicion es para no contarnos a nosotros mismos
                if(distancia < settings.radioPercepcion && distancia > 0){
                    depredadoresAux.Add(d);
                }
            }
        }
        return depredadoresAux;
    }

    //NOTA: LAS TRES REGLAS PODRIAN COMPARTIR EL FOR DE RECORRER LA MANADA EN UNA UNICA FUNCION, ASI NO LA RECORREMOS CADA VEZ POR REGLA
    ///<summary>R1. Devuelve el vector entre nosotros y el centro de la manada</summary>
    Vector3 Cohesion(List<Boid> manada){
        Vector3 pC = new Vector3(0,0,0);
        if(manada.Count == 0)
            return pC;
        foreach (var b in manada) {
            pC += b.transform.position;
        }
        //NOTA: en la manada NO estamos nosotros nunca
        pC = (manada.Count) > 1? pC/manada.Count : pC;
        return (pC-this.transform.position)*settings.pesoCohesion;
    }

    ///<summary>R2. Devuelve el vector sumatorio entre nosotros y los boids demasiado cercanos a nosotros. Se develve en negativo para alejarnos</summary>
    Vector3 Separacion(List<Boid> manada){
        Vector3 c = new Vector3(0,0,0);
        if(manada.Count == 0)
            return c;
        foreach (var b in manada) {
            if(Distancia(b.transform.position, this.transform.position) < settings.dstSeparacion){
                c-= b.transform.position - this.transform.position;
            }
        }
        return c*settings.pesoSeparacion;
    }

    ///<summary>R3. Devuelve el vector entre nosotros la direccion general de la manada.
    ///NOTA: De momento iguala la velocidad, pero creo que deberia de igualar la direccion</summary>
    Vector3 Alineacion(List<Boid> manada){
        Vector3 pV = new Vector3(0,0,0);
        if(manada.Count == 0)
            return pV;
        foreach (var b in manada) {
            pV += b.direccion;
        }
        pV = (manada.Count) > 1? pV/manada.Count: pV;
        return (pV)*settings.pesoAlineacion;
    }
    
    ///<summary>R4. Devuelve el vector entre nosotros el depredador pero en negativo.
    ///NOTA: De momento iguala la velocidad, pero creo que deberia de igualar la direccion</summary>
    Vector3 HuirDepredador(Depredador dep){
        Vector3 pV = new Vector3(0,0,0);
        if(dep != null)
            return -(dep.transform.position - this.transform.position)*settings.pesoHuirDepredador;
        else
            return pV;
    }

    ///<summary>Dada la lista de depredadores percibidos, devuelve el que este dentro de nuestro radio de vision</summary>
    Depredador DepredadorCercano(List<Depredador> lD){
        foreach (var d in lD) {
            if(Distancia(d.transform.position, this.transform.position) <= settings.radioPercepcion)
                return d;
        }
        return null;
    }

    ///<summary>Regla tiempo vuelo. Cuando tVuelo supere un umbral, el boid se dirigira al suelo a descansar.</summary>
    Vector3 Reglasuelo(){
        Vector3 suelo = new Vector3(0,0,0);
        if(!descansando){
            //Nos movemos hacia el suelo
            if(tVuelo >= 0.8f){
                suelo = new Vector3(this.transform.position.x, settings.sueloMundo.transform.position.y, this.transform.position.z);
                //if(this.transform.position.y == settings.sueloMundo.transform.position.y){
                if( Distancia(this.transform.position, suelo) <= 0.5f){
                    descansando = true;
                    return suelo;
                }
                return (suelo - this.transform.position)*50;
            }
        }
        return suelo;
    }

    //NOTA: Como es un update independiente, no sirve inicializar valores porque entra en el update antes de que se llame a Inicializar.
    //por eso settings tiene que ser referencia desde el principio
    void Update(){
        if(!Spawner.activarGPU){
            try {
                depredadoresRegion = PercibirDepredadoresRegion();
                depredador = DepredadorCercano(depredadoresRegion);
                //Si hay un depredador cerca, dejamos de descansar
                if(depredador!=null)
                    descansando = false;
                //Nos morimos de cansancio
                if(tVuelo>=1f)
                    Destruir();

                if(descansando){
                    tVuelo-= settings.ratioTiempoDescanso* Time.deltaTime;
                    if (tVuelo<=0.3f)
                        descansando=false;
                        //Reseteamos las direcciones etc porque sino cuando vuelve a moverse si guarda la anterior direccion puede que se salga del mapa
                        //direccion = Vector3.up;
                        //direccion=cero;
                        //velocidad = cero;
                        //speed = 0f;
                }
                else{
                    //Si cambiamos de region nos eliminamos de la region anterior y nos añadimos a la nueva
                    if( !RegionManager.DentroDeCubo(this.transform.position, RegionManager.mapaRegiones[region].posicion, RegionManager.mapaRegiones[region].dimensiones) ){
                        //Hemos cambiado de region, nos eliminamos de la lista de boids de esa region
                        RegionManager.EliminarBoidDeRegion(regionAnterior, this);

                        //Buscamos la region en la que estamos a partir de las adyacentes
                        region = RegionManager.EncontrarRegionIndices(transform.position, regionesAdyacentes);
                        regionesAdyacentes = RegionManager.RegionesAdyacentes(region);

                        RegionManager.AñadirBoidDeRegion(region, this);
                        regionAnterior = region;

                        boidsRegion.Clear();
                    }
                    boidsRegion.Clear();
                    tVuelo += settings.ratioTiempovuelo * Time.deltaTime;
                    //Percibimos los Boids de nuestra region y las adyacentes
                    manada = PercibirBoidsRegion();

                    
                    r4 = HuirDepredador(depredador);
                    r5 = Reglasuelo();

                    //Las 3 reglas basicas
                    //Vector3 r1,r2,r3;
                    r1 = Cohesion(manada);//Bien
                    r2 = Separacion(manada);//Bien
                    r3 = Alineacion(manada);//Bien
                    
                    //Regla de colision con objetos
                    Vector3 col = new Vector3(0,0,0);
                    if(Colision()){//NOTA: Lo unico que no colisiona es el centro del boid, por eso a veces parece que atraviesan obscaculos, aunque a veces los atraviesan de verdad
                        col =  ObstacleRays () * settings.pesoEvitarChoque;
                    }
                    //print("r1: " + r1 + " r2: " + r2 + " r3: " + r3 + " col: " + col);
                    var aceleracion = (r1+r2+r3+col+r4+r5);
                    velocidad += aceleracion;
                    
                    //Actualizamos las nuevas direccion y velocidad
                    speed = velocidad.magnitude;
                    Vector3 auxDir = speed > 0? velocidad/speed : velocidad;//Nueva direccion porque hemos aumentado la velocidad
                    //Nueva velocidad
                    speed = Mathf.Clamp (speed, settings.minSpeed, settings.maxSpeed);
                    velocidad = auxDir*speed;
                    direccion = auxDir;

                    //IMPORTANTE: ESTO ACTUALIZA LOS EJES DEL BOID, SI NO LO ACTUALIZAMOS, EL RAYCASTING NO VA A FUNCIONAR BIEN
                    transform.forward = direccion==cero? transform.forward : direccion;
                    transform.position += velocidad * Time.deltaTime;
                }
            }
            //Si por algun casual nos salimos del mapa, la region va a ser -1 y va a petar. Reseteamos el boid a al primer cubo del mapa de regiones
            catch(ArgumentOutOfRangeException){
                print("He petado, nos inicializamos a la primera region del mapa");
                print("Ultima region en la que estaba: " + regionAnterior);
                this.Inicializar(settings, RegionManager.mapaRegiones[0].posicion);
            }
        }
    }

    ///<summary>Comprobamos con un rayo si vamos a chocarnos o no con algun obstaculo</summary>
    public bool Colision(){
        RaycastHit hit;
        float speed = velocidad.magnitude;
        Vector3 dir = velocidad / speed;
        //origen, radio esfera que casteamos, direccion, informacion hit, maxima distancia casteo, mascara de obstaculos
        if (Physics.SphereCast (transform.position, 0.2f, direccion, out hit, settings.radioPercepcion, settings.mascaraObstaculos))
            return true;
        else
            return false;
    }

    public Ray rayo;//Rayo para dibujar en gizmos
    ///<summary>Decidimos a donde nos movemos en funcion de los rayos casteados</summary>
    Vector3 ObstacleRays () {
        //Vector con las direcciones de los rayos en funcion del numero aureo
        Vector3[] rayDirections = BoidHelper.directions;
        //Si un rayo NO golpea, nos movemos en esa direccion
        for (int i = 0; i < rayDirections.Length; i++) {
            Vector3 dir = this.transform.TransformDirection (rayDirections[i]);
            Ray ray = new Ray (transform.position, dir);
            //Devolvemos la direccion del primer rayo que no golpea un obstaculo
            if (!Physics.SphereCast (ray, 0.2f, settings.distanciaEvitarColision, settings.mascaraObstaculos)) {
                rayo = ray;
                return dir;
            }
        }
        //Si todos los rayos han golpeado, seguimos adelante (Si todos golpean, no tenemos escapatoria asi que da igual que hacer)
        return direccion;
    }

    public void ActualizarBoid(){
        try{
            if(this!=null){
                if(tVuelo>=1f)
                    Destruir();
                if(descansando){
                    tVuelo-= settings.ratioTiempoDescanso * Time.deltaTime;
                    if (tVuelo<=0.3f)
                        descansando=false;
                }
                else{
                    //Actualizamos la region en la que estamos
                    if( !RegionManager.DentroDeCubo(this.transform.position, RegionManager.mapaRegiones[region].posicion, RegionManager.mapaRegiones[region].dimensiones) ){
                        //Hemos cambiado de region, nos eliminamos de la lista de boids de esa region
                        RegionManager.EliminarBoidDeRegion(regionAnterior, this);

                        //Buscamos la region en la que estamos a partir de las adyacentes
                        region = RegionManager.EncontrarRegionIndices(transform.position, regionesAdyacentes);
                        regionesAdyacentes = RegionManager.RegionesAdyacentes(region);

                        RegionManager.AñadirBoidDeRegion(region, this);
                        regionAnterior = region;

                        boidsRegion.Clear();
                    }
                    tVuelo += settings.ratioTiempovuelo * Time.deltaTime;
                
                    //Las 3 reglas basicas
                    if(numeroManada > 0){
                        r1 = (r1 - this.transform.position)*settings.pesoCohesion;//Cohesion
                        r2 = r2 * settings.pesoSeparacion;//Separacion
                        r3 = (r3 - this.direccion)*settings.pesoAlineacion;//Alineacion
                    }
                    r5 = Reglasuelo();

                    //Regla de colision con objetos
                    Vector3 col = new Vector3(0,0,0);
                    if(Colision()){//NOTA: Lo unico que no colisiona es el centro del boid, por eso a veces parece que atraviesan obscaculos, aunque a veces los atraviesan de verdad
                        col =  ObstacleRays () * settings.pesoEvitarChoque;
                    }
                    //print("r1: " + r1 + " r2: " + r2 + " r3: " + r3 + " col: " + col);
                    var aceleracion = (r1+r2+r3+col+r4+r5);
                    velocidad += aceleracion;
                    
                    //Actualizamos las nuevas direccion y velocidad
                    speed = velocidad.magnitude;
                    Vector3 auxDir = speed > 0? velocidad/speed : velocidad;//Nueva direccion porque hemos aumentado la velocidad
                    //Nueva velocidad
                    speed = Mathf.Clamp (speed, settings.minSpeed, settings.maxSpeed);
                    velocidad = auxDir*speed;
                    direccion = auxDir;

                    //IMPORTANTE: ESTO ACTUALIZA LOS EJES DEL BOID, SI NO LO ACTUALIZAMOS, EL RAYCASTING NO VA A FUNCIONAR BIEN
                    transform.forward = direccion==cero? transform.forward : direccion;
                    transform.position += velocidad * Time.deltaTime;
                }
            }
        }
        //Si por algun casual nos salimos del mapa, la region va a ser -1 y va a petar. Reseteamos el boid a al primer cubo del mapa de regiones
        catch(ArgumentOutOfRangeException){
            print("He petado con gpu, ultima region en la que estaba: " + regionAnterior);
            this.Inicializar(settings, RegionManager.mapaRegiones[0].posicion);
        }
    }

    public void Destruir(){
        RegionManager.EliminarBoidDeRegion(region, this);
        Destroy(this.gameObject);
    }

    void OnDestroy(){
        print("Me han comido, wey :(");
        spawner.todosBoids.Remove(this);
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(rayo);

        var colorAux = Color.blue;colorAux.a = 0.1f;
        var colorAux2 = Color.yellow;colorAux2.a = 0.1f;

        //Radio percepcion
        Gizmos.color = colorAux;
        Gizmos.DrawSphere(transform.position, settings.radioPercepcion);

        //Radios distancia separacion entre boids
        Gizmos.color = colorAux2;
        Gizmos.DrawSphere(transform.position, settings.dstSeparacion);

        //Direccion del boid
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.position, (direccion + transform.position)*1f);

        //R1: Cohesion, el centro de la manada
        Gizmos.color = Color.red;
        if(r1!=cero)
            Gizmos.DrawLine(transform.position, r1+transform.position);

        //R2: Separacion, vector para alejarnos resultante si hay un boid muy cerca de nosotros
        Gizmos.color = Color.white;
        if(r2!=cero)
            Gizmos.DrawLine(transform.position, r2+transform.position);

        //R3: Alineacion, direccion general de la manada
        Gizmos.color = Color.green;
        if(r3!=cero)
            Gizmos.DrawLine(transform.position, (r3+transform.position)*1f);
        
        //R4: Huir del depredador
        Gizmos.color = Color.yellow;
        if(r4!=cero)
            Gizmos.DrawLine(transform.position, (r4+transform.position));
        
        //R5: Descansar
        Gizmos.color = Color.blue;
        if(r5!=cero)
            Gizmos.DrawLine(transform.position, (r5+transform.position));
        
        Color auxRojo = Color.red;Color auxVerde = Color.green;
        auxRojo.a = 0.1f;auxVerde.a = 0.1f;
        Gizmos.color = auxVerde;
        //Region en la que estamos
        Gizmos.DrawCube(RegionManager.mapaRegiones[region].posicion, RegionManager.mapaRegiones[region].dimensiones);

        //Regiones adyacentes
        Gizmos.color = auxRojo;
        foreach (var i in regionesAdyacentes) {
            Gizmos.DrawCube(RegionManager.mapaRegiones[i].posicion, RegionManager.mapaRegiones[i].dimensiones);
        }
    }
}