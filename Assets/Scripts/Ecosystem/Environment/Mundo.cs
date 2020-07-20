using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using TerrainGeneration;

public class Mundo : MonoBehaviour {
    //MAPA/MUNDO
    [Header ("MAPA / MUNDO")]
    TerrainGenerator.TerrainData datosTerreno;
    ///<summary>El tamaño de las regiones en las que subdividimos el mundo para el mapa</summary>
    int tamañoRegionesMapa = 10;
    ///<summary>Semilla que utilizamos para randomizar valores</summary>
    public int seed;
    ///<summary>Matriz con los centros de cada tile del mundo</summary>
    public static Vector3[,] centros;
    ///<summary>Matriz con las tiles caminables del mundo</summary>
    public static bool[,] caminable;
    ///<summary>Lista con todas las coordenadas caminables del mapa</summary>
    public static List<Vector3Int> coordenadasCaminables;
    ///<summary>El tamaño del mapa es tamaño x tamaño</summary>
    public static int tamaño;
    //static Vector3Int[, ][] walkableNeighboursMap; No hace falta aunque puede que sea mas eficiente asi
    //static List<Vector3Int> walkableCoords; No hace falta porque ya tenemos la matriz caminable
    ///<summary>Una matriz con las coordenadas del agua mas cercana de cada tile. Asi no hace falta buscarlas cada vez</summary>
    public static Vector3Int[,] aguaMasCercana;

    //ARBOLES
    [Header ("ARBOLES")]
    [Range (0,1)]
    ///<summary>Probabilidad de que spawnee un arbol</summary>
    public float probabilidadArboles;
    public MeshRenderer prefabArbol;

    //DICCIONARIOS
    [Header ("DICCIONARIOS")]
    ///<summary>Relacion Depredador - Lista de presas</summary>
    private static Dictionary<Species, List<Species>> depredadorPresas;
    ///<summary>Relacion Presa - Lista de depredadores</summary>
    private static Dictionary<Species, List<Species>> presasDepredador;
    ///<summary>Contiene el mapa correspondiente a cada especie</summary>
    private static  Dictionary<Species, Mapa> mapasEspecies;

    //POBLACION
    [Header ("POBLACION")]
    public Population[] poblacionInicial;
    private static System.Random rnd;

    //Variables para textos
    //NOTA: CAMBIAR ESTO EN EL FUTURO. IGUAL CON UNA CLASE Y GAMEOBJECT PROPIO QUE LO ADMINISTRE QUEDA UN POCO MAS ESCUETO
    //Listas con el numero de seres por tiempo (el tiempo es el indice que va a ser por fotogramas)
    [HideInInspector]
    ///<summary>Lista con con el numero de zorros en cada momento.null Su ultimo elemento tiene los zorros actuales</summary>
    public List<int> grafZorros = new List<int>();
    [HideInInspector]
    ///<summary>Lista con con el numero de conejos en cada momento.null Su ultimo elemento tiene los conejos actuales</summary>
    public List<int> grafConejos = new List<int>();
    [HideInInspector]
    public List<int> grafPlantas = new List<int>();
    private SimplestPlot SimplestPlotScript;

    //Contadores a mostrar en pantalla
    public Text contadorConejo;
    public Text contadorZorro;
    public Text contadorPlanta;

    //Carpetas donde guardar las entidades
    private GameObject foxHolder;
    private GameObject plantHolder;
    private GameObject rabbitHolder;
    //Sumatorios
    [HideInInspector]
    public float velocidadZorros = 0f;
    [HideInInspector]
    public float velocidadConejos = 0f;
    [HideInInspector]
    public int radioVisionConejos = 0;
    [HideInInspector]
    public int radioVisionZorros = 0;

    private GameObject menuCausasMuerte;
    [HideInInspector]
    public static Vector3Int invalid = new Vector3Int(-1, 0, -1);

    //Actualizamos los contadores
    //NOTA: EN UN FUTURO TAMBIEN SE DEBERIAN DE PASAR POR AQUI LOS DATOS PARA LAS GRAFICAS
    void Update() {
        if(mapasEspecies[Species.Rabbit].numeroEntidades == 0 && mapasEspecies[Species.Fox].numeroEntidades == 0){
            //print("NO QUEDAN ANIMALES VIVOS");
        }

        UpdateGrafs();
        //Actualizamos los contadores de la pantalla
        //NOTA: Puede que no se deba de actualziar con cada fotograma, deberia de bastar con una vez cada segundo
        //pero no deberia de afectar en nanda a la eficiencia
        contadorConejo.text = mapasEspecies[Species.Rabbit].numeroEntidades.ToString();
        contadorZorro.text = mapasEspecies[Species.Fox].numeroEntidades.ToString();
        contadorPlanta.text = mapasEspecies[Species.Plant].numeroEntidades.ToString();
    }

    /// <summary>Actualiza las variables grafZorros y grafConejos. Se le va a llamar una vez cada segundo </summary>
    void UpdateGrafs(){
        int zorros = mapasEspecies[Species.Fox].numeroEntidades;
        int conejos = mapasEspecies[Species.Rabbit].numeroEntidades;
        int plantas = mapasEspecies[Species.Plant].numeroEntidades;
        grafZorros.Add(zorros);
        grafConejos.Add(conejos);
        grafPlantas.Add(plantas);
    }

    /// <summary>Inicializa las listas de animales que usamos para imprimir en pantalla con las poblaciones iniciales </summary>
    private void StartListaAnimales() {
        for (int i = 0; i < poblacionInicial.Length; i++)
        {
            switch (poblacionInicial[i].prefab.species)
            {
                case Species.Rabbit:
                    grafConejos.Add(poblacionInicial[i].count);
                break;
                case Species.Fox:
                    grafZorros.Add(poblacionInicial[i].count);
                break;
                case Species.Plant:
                    grafPlantas.Add(poblacionInicial[i].count);
                break;
            }
        }
    }

    /// <summary>Spawnea plantas de manera aleatoria en el mapa</summary>
    private void SpawnPlantasTiempo(){
        //seed auxiliar que cambia cada vez. Si usamos la variable seed, al ser siempre el mismo valor
        //genera siempre el mismo numero random
        var aux_seed = System.DateTime.Now.Millisecond;
        var spawnPrng = new System.Random (aux_seed);
        //Lista de coordenadas spawneables, haya donde se pueda andar
        if (coordenadasCaminables.Count == 0) {
            Debug.Log ("No hay sitio para spawnear la planta");
            return;
        }
        int indiceCoordenada = spawnPrng.Next (0, coordenadasCaminables.Count);
        Vector3Int coordenada = coordenadasCaminables[indiceCoordenada];

        //Instanciamos una planta
        foreach (var pop in poblacionInicial) {
            if(pop.prefab.species == Species.Plant){
                var entity = Instantiate (pop.prefab);
                entity.Init(coordenada);

                //Almacenamos la nueva entidad en su correspondiente lista en funcion de la especie
                mapasEspecies[entity.species].Añadir(entity, coordenada);
                entity.transform.parent = plantHolder.transform;
            }
        }
    }

    //Funcion principal que inicia el ecosistema
    public void Start () {
        //NOTA: CAMBIAR O QUITAR
        //Inicializamos las listas con el numero de animales
        StartListaAnimales();
        //Llamamos a updateGrafs despues de 0,5 segundos de delay una vez cada segundo
        //InvokeRepeating("UpdateGrafs", 0.5f, 1f);
        InvokeRepeating("SpawnPlantasTiempo", 0, 0.5f);
        
        foxHolder = GameObject.Find("FoxHolder");
        plantHolder = GameObject.Find("PlantHolder");
        rabbitHolder = GameObject.Find("RabbitHolder");

        rnd = new System.Random ();
        //Init ();
        //SpawnInitialPopulations ();
        menuCausasMuerte = GameObject.Find("CausasMuerte");
        //GameObject.Find("MenuGraficas").SetActive(false);
    }

    ///<summary>Registra el movimiento de la entidad en su correspondiente mapa</summary>
    public void RegistrarMovimiento(LivingEntity ent, Vector3Int de, Vector3Int a){
        mapasEspecies[ent.species].Mover(ent, de, a);
    }

    ///<summary>Registra la muerte de la entidad en su mapa y actualiza las causas de muerte</summary>
    public void RegistrarMuerte(LivingEntity ent, CauseOfDeath causa){
        mapasEspecies[ent.species].Eliminar(ent, ent.coord);
        AdministradorCausasMuerte.ActualizarCausas(ent.species, causa);
    }

    ///<summary>Devuelve si el agua mas cercana de desde esta dentro del rango de vision dado o no</summary>
    public static Vector3Int SentirAgua(Vector3Int desde, int distanciaV){
        if(aguaMasCercana[desde.x, desde.z] != invalid)
            if(Vector3Int.Distance(aguaMasCercana[desde.x, desde.z], desde) <= distanciaV)
                return aguaMasCercana[desde.x, desde.z];
        return invalid;
    }

    ///<summary>Develve la comida mas cercana de la entidad de entre las que puede ver</summary>
    public static LivingEntity SentirComida(LivingEntity dep, Vector3Int desde, int distanciaV){
        List<LivingEntity> lista = new List<LivingEntity>();
        var presas = depredadorPresas[dep.species];
        foreach (var p in presas) {
            foreach (var ent in mapasEspecies[p].ObtenerEntidades(desde, distanciaV)){
                if(Vector3Int.Distance(ent.coord, desde) <= distanciaV && Pathfinder.SeccionVisible(ent.coord.x, ent.coord.z, desde.x, desde.z))
                    lista.Add(ent);
            }
        }
        //lista.Sort((a,b) => (a.coord.x * a.coord.x + a.coord.z * a.coord.z).CompareTo (b.coord.x * b.coord.x + b.coord.z * b.coord.z));
        //NOTA: IGUAL NO HACE FALTA CALCULAR LA DISTANCIA PARA PODER ORDENARLO. IGUAL BASTA CON COMPARAR LAS RESTAS EN VALOR ABSOLUTO
        lista.Sort((a,b) => (Vector3.Distance(desde, a.coord)).CompareTo (Vector3.Distance(desde, b.coord)));
        return lista.Count>0? lista[0]:null;
    }

    ///<summary>Develve una lista con las posibles parejas dentro del rango de vision dado</summary>
    public static List<LivingEntity> SentirPosiblesParejas(LivingEntity yo, Vector3Int desde, int distanciaV){
        List<LivingEntity> lista = new List<LivingEntity>();
        List<LivingEntity> seresEspecie = (mapasEspecies[yo.species].ObtenerEntidades(yo.coord, distanciaV));
        Animal yoAnimal = (Animal) yo;
        foreach(var ser in seresEspecie){
            Animal animalSer = (Animal) ser;
            if( animalSer.genes.isMale != yoAnimal.genes.isMale && animalSer.currentAction == CreatureAction.SearchingForMate)
                if(Pathfinder.SeccionVisible(yoAnimal.coord.x, yoAnimal.coord.z, animalSer.coord.z, animalSer.coord.z))
                    lista.Add(ser);
        }
        return lista.Count>0? lista:null;
    }
    
    ///<summary>Detecta el depredador mas cercano dentro del rango de vision</summary>
    public static LivingEntity SentirDepredador(LivingEntity yo, int distanciaV){
        List<LivingEntity> listaDepredadores = new List<LivingEntity>();
        foreach (var especieDepredador in presasDepredador[yo.species]){
            listaDepredadores.Add(mapasEspecies[especieDepredador].EntidadMascercana(yo.coord, distanciaV));
        }
        listaDepredadores.Sort( (a,b) => (Vector3.Distance(yo.coord,a.coord).CompareTo(Vector3.Distance(yo.coord,b.coord)) ));
        return listaDepredadores.Count>0? listaDepredadores[0]:null;
    }

    ///<summary>Devuelve el siguiente tile de manera aleatoria</summary>
    public static Vector3Int TileRandom(Vector3Int origen){
        //Guardamos las tiles caminables a nuestro alrededor
        List<Vector3Int> tilesCaminable = new List<Vector3Int>();
        for (int x = -1; x <= 1; x++){
            for (int y = -1; y < 1; y++) {
                var X = origen.x + x < 0? 0:origen.x+x; X = origen.x + x > tamaño? tamaño:origen.x+x;
                var Y = origen.y + y < 0? 0:origen.y+y; Y = origen.y + y > tamaño? tamaño:origen.y+y;
                if(caminable[X, Y])
                    tilesCaminable.Add(new Vector3Int(X,0,Y));
            }
        }
        int indiceRandom = rnd.Next(0,tilesCaminable.Count);
        return tilesCaminable[indiceRandom];
    }

    ///<summary>Devolvemos el siguiente tile de manera aleatoria pero con tendencia hacia adelante</summary>
    public static Vector3Int TileTendencia(Vector3Int origen, Vector3Int anterior, float tendencia){
        Vector3Int direccion = (origen - anterior);
        //No hay direccion, devolvemos random
        if(direccion == Vector3Int.zero)
            return TileRandom(origen);
        
        //Probabilidad de ir recto
        if(rnd.NextDouble()<tendencia){
            if(caminable[origen.x + direccion.x, origen.z + direccion.z])
                return (origen+direccion);
        }
        //Tiende hacia adelante
        else{
            bool diagonal = (direccion.x!=0 && direccion.z!=0)?true:false;
            var vecinosValidos = new List<Vector3Int>();
            var recto = origen+direccion;
            if(diagonal){
                if(caminable[recto.x, recto.z])
                    vecinosValidos.Add(recto);
                var aux = new Vector3Int(recto.x, 0, origen.z);
                if(caminable[aux.x, aux.z])
                    vecinosValidos.Add(aux);
                aux = new Vector3Int(origen.x, 0, recto.z);
                if(caminable[aux.x, aux.z])
                    vecinosValidos.Add(aux);
                //Devolvemos cualquiera de las tres tiles en la direccion general que seguiamos
                return vecinosValidos[rnd.Next(0,vecinosValidos.Count)];
            }
            else{//No nos hemos movido en diagonal
                if(direccion.x!=0){
                    if(caminable[recto.x, recto.z])
                        vecinosValidos.Add(recto);
                    var aux = new Vector3Int(recto.x-1,0, origen.z);
                    if(caminable[aux.x, aux.z])
                        vecinosValidos.Add(aux);
                    aux = new Vector3Int(recto.x+1, 0, origen.z);
                    if(caminable[aux.x, aux.z])
                        vecinosValidos.Add(aux);
                    return vecinosValidos[rnd.Next(0,vecinosValidos.Count)];
                }
                if(direccion.z!=0){
                    if(caminable[recto.x, recto.z])
                        vecinosValidos.Add(recto);
                    var aux = new Vector3Int(origen.x,0, recto.z-1);
                    if(caminable[aux.x, aux.z])
                        vecinosValidos.Add(aux);
                    aux = new Vector3Int(origen.x, 0, recto.z+1);
                    if(caminable[aux.x, aux.z])
                        vecinosValidos.Add(aux);
                    return vecinosValidos[rnd.Next(0,vecinosValidos.Count)];
                }

            }
        }
        return TileRandom(origen);
    }

    // Call terrain generator and cache useful info
    public void Init () {
        if(crearTxt) 
            CrearArchivo();
        //Contador de tiempo
        var sw = System.Diagnostics.Stopwatch.StartNew ();

        //objeto generador de terreno
        var terrainGenerator = FindObjectOfType<TerrainGenerator> ();
        datosTerreno = terrainGenerator.Generate ();

        centros = datosTerreno.tileCentres;
        //Terreno por el que se puede caminar
        caminable = datosTerreno.walkable;
        tamaño = datosTerreno.size;

        //Numero de ESPECIES
        int numSpecies = System.Enum.GetNames (typeof (Species)).Length;
        //Lista de animales a devorar por cada especie
        presasDepredador = new Dictionary<Species, List<Species>> ();
        //preyBySpecies = new Dictionary<Species, List<Species>> ();
        //Lista de depredadores de cada especie
        depredadorPresas = new Dictionary<Species, List<Species>> ();
        //predatorsBySpecies = new Dictionary<Species, List<Species>> ();

        // Init species maps
        mapasEspecies = new Dictionary<Species, Mapa> ();
        for (int i = 0; i < numSpecies; i++) {
            Species species = (Species) (1 << i);
            mapasEspecies.Add (species, new Mapa (tamaño, tamañoRegionesMapa));

            presasDepredador.Add (species, new List<Species> ());
            depredadorPresas.Add (species, new List<Species> ());
        }

        // Store predator/prey relationships for all species
        for (int i = 0; i < poblacionInicial.Length; i++) {

            if (poblacionInicial[i].prefab is Animal) {
                Animal hunter = (Animal) poblacionInicial[i].prefab;
                Species diet = hunter.diet;

                for (int huntedSpeciesIndex = 0; huntedSpeciesIndex < numSpecies; huntedSpeciesIndex++) {
                    int bit = ((int) diet >> huntedSpeciesIndex) & 1;
                    // this bit of diet mask set (i.e. the hunter eats this species)
                    if (bit == 1) {
                        int huntedSpecies = 1 << huntedSpeciesIndex;
                        presasDepredador[hunter.species].Add ((Species) huntedSpecies);
                        depredadorPresas[(Species) huntedSpecies].Add (hunter.species);
                    }
                }
            }
        }

        //LogPredatorPreyRelationships ();

        //SpawnTrees ();

        //walkableNeighboursMap = new Vector3Int[size, size][];

        // Find and store all walkable neighbours for each walkable tile on the map
        for (int y = 0; y < datosTerreno.size; y++) {
            for (int x = 0; x < datosTerreno.size; x++) {
                if (caminable[x, y]) {
                    List<Vector3Int> walkableNeighbours = new List<Vector3Int> ();
                    for (int offsetY = -1; offsetY <= 1; offsetY++) {
                        for (int offsetX = -1; offsetX <= 1; offsetX++) {
                            if (offsetX != 0 || offsetY != 0) {
                                int neighbourX = x + offsetX;
                                int neighbourY = y + offsetY;
                                if (neighbourX >= 0 && neighbourX < tamaño && neighbourY >= 0 && neighbourY < tamaño) {
                                    if (caminable[neighbourX, neighbourY]) {
                                        //print("WalkableNeighbour añadido");
                                        walkableNeighbours.Add (new Vector3Int (neighbourX, 0, neighbourY));
                                    }
                                }
                            }
                        }
                    }
                    //walkableNeighboursMap[x, y] = walkableNeighbours.ToArray ();
                }
            }
        }

        // Generate offsets within max view distance, sorted by distance ascending
        // Used to speed up per-tile search for closest water tile
        List<Coord> viewOffsets = new List<Coord> ();
        //int viewRadius = Animal.maxViewDistance;
        int viewRadius = 10;
        int sqrViewRadius = viewRadius * viewRadius;
        for (int offsetY = -viewRadius; offsetY <= viewRadius; offsetY++) {
            for (int offsetX = -viewRadius; offsetX <= viewRadius; offsetX++) {
                int sqrOffsetDst = offsetX * offsetX + offsetY * offsetY;
                if ((offsetX != 0 || offsetY != 0) && sqrOffsetDst <= sqrViewRadius) {
                    viewOffsets.Add (new Coord (offsetX, offsetY));
                }
            }
        }
        viewOffsets.Sort ((a, b) => (a.x * a.x + a.y * a.y).CompareTo (b.x * b.x + b.y * b.y));
        Coord[] viewOffsetsArr = viewOffsets.ToArray ();

        int nWT = 0;
        // Find closest accessible water tile for each tile on the map:
        aguaMasCercana = new Vector3Int[tamaño, tamaño];
        for (int y = 0; y < datosTerreno.size; y++) {
            for (int x = 0; x < datosTerreno.size; x++) {
                bool foundWater = false;
                if (caminable[x, y]) {
                    for (int i = 0; i < viewOffsets.Count; i++) {
                        int targetX = x + viewOffsetsArr[i].x;
                        int targetY = y + viewOffsetsArr[i].y;
                        if (targetX >= 0 && targetX < tamaño && targetY >= 0 && targetY < tamaño) {
                            if (datosTerreno.shore[targetX, targetY]) {
                                if (EnvironmentUtility.TileIsVisibile (x, y, targetX, targetY)) {
                                    aguaMasCercana[x, y] = new Vector3Int (targetX, 0, targetY);
                                    foundWater = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (!foundWater) {
                    nWT++;
                    aguaMasCercana[x, y] = invalid;
                }
            }
        }
        Debug.Log ("Init time: " + sw.ElapsedMilliseconds);
    }

    public bool crearTxt = false;
    private string path;
    void CrearArchivo(){
        var fechaSinBarras = System.DateTime.Now.Year.ToString() +"-" + System.DateTime.Now.Month.ToString() +"-" 
        + System.DateTime.Now.Day.ToString() +"-" + System.DateTime.Now.Hour.ToString() + "-" + System.DateTime.Now.Minute.ToString() + "-"
        + System.DateTime.Now.Second.ToString();
        path = Application.dataPath + "/Logs/CausasMuerte/causasMuerte_" + fechaSinBarras + ".txt";
        //Si no existe, lo creamos
        if(!File.Exists(path)){
            File.WriteAllText(path, "Causas muerte\n Conejos \t Zorros");
        }
        //Si existe, lo borramos y creamos uno nuevo
        else{
            File.Delete(path);
            File.WriteAllText(path, "Causas muerte\n Conejos \t Zorros");
        }
    }
    void OnApplicationQuit() {
        if(crearTxt){
            var eC = "Eaten: " + AdministradorCausasMuerte.eatenText[1].text;var eZ = "Eaten: " + AdministradorCausasMuerte.eatenText[0].text;
            var hC = "Hunger: "+ AdministradorCausasMuerte.hungerText[1].text;var hZ = "Hunger: "+ AdministradorCausasMuerte.hungerText[0].text;
            var tC = "Thirst: "+ AdministradorCausasMuerte.thirstText[1].text;var tZ = "Thirst: "+ AdministradorCausasMuerte.thirstText[0].text;
            var aC = "Age: "+ AdministradorCausasMuerte.ageText[1].text;var aZ = "Age: "+ AdministradorCausasMuerte.ageText[0].text;
            File.AppendAllText(path,  eC + "\t" + eZ +"\n" + hC + "\t" + hZ +"\n" + tC + "\t" + tZ +"\n" + aC + "\t" + aZ +"\n");
        }
    }

    //Estructura para guardar la poblacion de un tipo de entidad viva
    [System.Serializable]
    public struct Population {
        public LivingEntity prefab;
        public int count;
    }
}
