using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//Como depredador, no tendremos las 3 reglas basicas, tendremos mas velocidad, mas radio de vision y cuando veamos a un boid iremos a por el
//y si llegamos a estar a cierta distancia consideramos que lo matamos
//Al no tener reglas basicas, es posible que haya que hacer que se mueva en direcciones un poco random para que encuentre boids sino solo
//se va a guiar por el choque contra objetos
public class Depredador : MonoBehaviour
{
    private DepredadorSettings settings;
    ///<summary>Direccion a la que nos dirigimos. direccion = velocidad/speed</summary>
    public Vector3 direccion;
    ///<summary>Velocidad de movimiento. speed = velocidad/direccion</summary>
    public float speed;
    ///<summary>Es la direccion(vector3) por la velocidad(float)</summary>
    public Vector3 velocidad;

    public List<Boid> boidsRegion;
    ///<summary>Presas que estan en nuestro campo de vision</summary>
    public List<Boid> posiblesPresas;
    ///<summary>Presa mas cercana y a la que vamos a atacar</summary>
    public Boid presaObjetivo;

    ///<summary>Indice de la region en la que esta el Depredador actualmente</summary>
    public int region;
    public List<int> regionesAdyacentes;
    ///<summary>Indice de la region anterior donde estaba el Boid. Lo usamos para quitar el Boid de la lista de la region si hemos cambiado de region</summary>
    public int regionAnterior;
    ///<summary>Tiempo de vuelo. Cuando llega a 1 el boid devera descansar en el suelo</summary>
    public float tVuelo;
    ///<summary>Indica si estamos en el suelo descansando o no</summary>
    public bool descansando = false;
    public Vector3 rA;
    
    //Inicializamos settings, posicion, direccion, speed, velocidad y region del Boid
    public void Inicializar(DepredadorSettings set, Vector3 pos){
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
    }

    float Distancia(Vector3 destino, Vector3 origen){
        Vector3 vector = destino - origen;
        return Mathf.Sqrt(vector.x*vector.x + vector.y*vector.y +vector.z*vector.z);
    }

    ///<summary>Devuelve boids en las regiones a distancia settings.radioPercepcion. Es la lista de posibles presas del depredador</summary>
    List<Boid> PercibirBoidsRegionDepredador(){
        List<Boid> manadaAux = new List<Boid>();
        //Percibimos los boids de las regiones nuestra y adyacentes
        foreach (var indice in regionesAdyacentes){
            foreach (var b in RegionManager.mapaRegiones[indice].boids){
                    float distancia = Distancia(b.transform.position, this.transform.position);
                    if(distancia < settings.radioPercepcion){
                        manadaAux.Add(b);
                    }
                    //TEMPORAL PARA COMPROBAR LOS BOIDS QUE DETECTA EN LAS REGIONES SUYA Y ADYACENTES
                    if(!boidsRegion.Contains(b))
                        boidsRegion.Add(b);
            }
        }
        return manadaAux;
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
            try{
                if(tVuelo>=1f)
                    Destruir();
                if(descansando){
                    tVuelo-= settings.ratioTiempoDescanso * Time.deltaTime;
                    if (tVuelo<=0.3f)
                        descansando=false;
                }
                else{
                    //Si cambiamos de region nos eliminamos de la region anterior y nos añadimos a la nueva
                    if( !RegionManager.DentroDeCubo(this.transform.position, RegionManager.mapaRegiones[region].posicion, RegionManager.mapaRegiones[region].dimensiones) ){
                        //Hemos cambiado de region, nos eliminamos de la lista de boids de esa region
                        RegionManager.EliminarDepredadorDeRegion(regionAnterior, this);

                        //NOTA: Aqui se deberia de encontrar la region a la que pertenece pero con indice de la region inicial o algo asi.
                        //ahora mismo busca la region de entre las 64 existentes, no es muy eficiente
                        region = RegionManager.EncontrarRegionIndices(transform.position, regionesAdyacentes);
                        regionesAdyacentes = RegionManager.RegionesAdyacentes(region);

                        RegionManager.AñadirDepredadorDeRegion(region, this);
                        regionAnterior = region;

                        boidsRegion.Clear();
                    }
                    boidsRegion.Clear();

                    tVuelo += settings.ratioTiempovuelo * Time.deltaTime;
                    var rVuelo = new Vector3(0,0,0);
                    rVuelo = Reglasuelo();
                    //Percibimos las posibles presas de nuestra region y las adyacentes
                    posiblesPresas = PercibirBoidsRegionDepredador();

                    //Regla de ataque
                    presaObjetivo = PresaMasCercana(posiblesPresas);
                    rA = new Vector3(0,0,0);
                    if(presaObjetivo != null)
                        rA = ReglaAtaque(presaObjetivo);
                    
                    //Regla de colision con objetos
                    Vector3 col = new Vector3(0,0,0);
                    if(Colision()){
                        col =  ObstacleRays () * settings.pesoEvitarChoque;
                    }
                    //print("r1: " + r1 + " r2: " + r2 + " r3: " + r3 + " col: " + col);
                    var aceleracion = (rA + col + rVuelo);
                    velocidad += aceleracion;
                    
                    //Actualizamos las nuevas direccion y velocidad
                    speed = velocidad.magnitude;
                    Vector3 auxDir = velocidad/speed;//Nueva direccion porque hemos aumentado la velocidad
                    //Nueva velocidad
                    speed = Mathf.Clamp (speed, settings.minSpeed, settings.maxSpeed);
                    velocidad = auxDir*speed;
                    direccion = auxDir;

                    transform.forward = direccion;
                    transform.position += velocidad * Time.deltaTime;

                    if(presaObjetivo != null){
                        if(Distancia(presaObjetivo.transform.position, this.transform.position) < 0.2f)
                            presaObjetivo.Destruir();
                    }
                }
            }
            catch(ArgumentOutOfRangeException){
                print("He petado, nos inicializamos a la primera region del mapa");
                this.Inicializar(settings, RegionManager.mapaRegiones[0].posicion);
            }
        }
    }

    ///<summary>De las presas que vemos, atacamos a la mas cercana</summary>
    public Vector3 ReglaAtaque(Boid presa){
        return (presa.transform.position - this.transform.position)*settings.pesoAtaque;
    }

    //No se si esta devolviendo la mas cercana, creo que no
    ///<summary>De las presas que vemos, atacamos a la mas cercana</summary>
    public Boid PresaMasCercana(List<Boid> presas){
        if(presas.Count > 0){
            var presa = presas[0];
            var aux = Distancia(presas[0].transform.position, this.transform.position);
            //Encontramos la presa mas cercana
            foreach (var p in presas) {
                if(Distancia(p.transform.position, this.transform.position) < aux){
                    aux = Distancia(p.transform.position, this.transform.position);
                    presa = p;
                }
            }
            return presa;
        }
        else{
            return null;
        }
    }

    ///<summary>Comprobamos con un rayo si vamos a chocarnos o no con algun obstaculo</summary>
    public bool Colision(){
        RaycastHit hit;
        float speed = velocidad.magnitude;
        Vector3 dir = velocidad / speed;
        //origen, radio esfera que casteamos, direccion, informacion hit, maxima distancia casteo, mascara de obstaculos
        if (Physics.SphereCast (transform.position, 0.2f, direccion, out hit, settings.distanciaEvitarColision, settings.mascaraObstaculos))
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

    public void ActualizarDepredador(){
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
                    if( !RegionManager.DentroDeCubo(this.transform.position, RegionManager.mapaRegiones[region].posicion, RegionManager.mapaRegiones[region].dimensiones) ){
                        //Hemos cambiado de region, nos eliminamos de la lista de boids de esa region
                        RegionManager.EliminarDepredadorDeRegion(regionAnterior, this);

                        region = RegionManager.EncontrarRegionIndices(transform.position, regionesAdyacentes);
                        regionesAdyacentes = RegionManager.RegionesAdyacentes(region);

                        RegionManager.AñadirDepredadorDeRegion(region, this);
                        regionAnterior = region;

                        boidsRegion.Clear();
                    }
                    
                    //Regla de colision con objetos
                    Vector3 col = new Vector3(0,0,0);
                    if(Colision()){
                        col =  ObstacleRays () * settings.pesoEvitarChoque;
                    }

                    tVuelo += settings.ratioTiempovuelo * Time.deltaTime;
                    var rVuelo = new Vector3(0,0,0);
                    rVuelo = Reglasuelo();
                    rA =(rA) * settings.pesoAtaque;
                    var aceleracion = (rA + col + rVuelo);
                    velocidad += aceleracion;
                    
                    //Actualizamos las nuevas direccion y velocidad
                    speed = velocidad.magnitude;
                    Vector3 auxDir = velocidad/speed;//Nueva direccion porque hemos aumentado la velocidad
                    //Nueva velocidad
                    speed = Mathf.Clamp (speed, settings.minSpeed, settings.maxSpeed);
                    velocidad = auxDir*speed;
                    direccion = auxDir;

                    transform.forward = direccion;
                    transform.position += velocidad * Time.deltaTime;

                    if(presaObjetivo != null){
                        if(Distancia(presaObjetivo.transform.position, this.transform.position) < 0.2f)
                            presaObjetivo.Destruir();
                    }
                }
            }
        }
        //Si por algun casual nos salimos del mapa, la region va a ser -1 y va a petar. Reseteamos el boid a al primer cubo del mapa de regiones
        catch(ArgumentOutOfRangeException){
            print("He petado con gpu, ultima region en la que estaba: " + regionAnterior);
            this.Inicializar(settings, RegionManager.mapaRegiones[0].posicion);
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(rayo);

        var colorAux = Color.blue;colorAux.a = 0.1f;
        var colorAux2 = Color.yellow;colorAux2.a = 0.1f;

        //Radio percepcion depredador
        Gizmos.color = colorAux;
        Gizmos.DrawSphere(transform.position, settings.radioPercepcion);

        //Direccion del depredador
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.position, (direccion + transform.position)*1f);

        //Presa
        //En cpu
        if(presaObjetivo != null){
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, presaObjetivo.transform.position);
        }
        //En gpu
        if(rA != new Vector3(0,0,0)){
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, (rA/settings.pesoAtaque + this.transform.position));
        }
        
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
    public void Destruir(){
        RegionManager.EliminarDepredadorDeRegion(region, this);
        Destroy(this.gameObject);
    }
}