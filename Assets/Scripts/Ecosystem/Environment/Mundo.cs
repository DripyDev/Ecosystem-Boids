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
    //TerrainGenerator.TerrainData datosTerreno;
    GeneradorTerreno.DatosTerreno datosTerreno;

    ///<summary>El tamaño de las regiones en las que subdividimos el mundo para el mapa</summary>
    int tamañoRegionesMapa = 10;

    ///<summary>Semilla que utilizamos para randomizar valores</summary>
    public int seed;

    //NOTA: EN LUGAR DE CENTROS QUE SEAN NODOS. UNA ESTRUCTURA CON H,F,G,NODOANTERIOR Y CENTRO. NO ALTERARIA MUCHO LA ESTRUCTURA GENERAL
    ///<summary>Matriz con los centros de cada tile del mundo</summary>
    public static Vector3[,] centros;

    ///<summary>Matriz con los centros de cada tile del mundo, lo usamos para A*</summary>
    public static Nodo[,] mapaNodos;

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
    ///<summary>Relacion Presa - Lista de depredadores</summary>
    private static Dictionary<Species, List<Species>> depredadoresPresa;
    ///<summary>Relacion Depredador - Lista de presas</summary>
    private static Dictionary<Species, List<Species>> presasDepredador;
    ///<summary>Contiene el mapa correspondiente a cada especie</summary>
    public static  Dictionary<Species, Mapa> mapasEspecies;

    //POBLACION
    [Header ("POBLACION")]
    public Poblacion[] poblacionInicial;
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
        InvokeRepeating("SpawnPlantasTiempo", 0, 0.2f);
        
        foxHolder = GameObject.Find("FoxHolder");
        plantHolder = GameObject.Find("PlantHolder");
        rabbitHolder = GameObject.Find("RabbitHolder");

        rnd = new System.Random(seed);
        Init();
        SpawnPoblacionInicial();
        menuCausasMuerte = GameObject.Find("CausasMuerte");
        //GameObject.Find("MenuGraficas").SetActive(false);
    }

    ///<summary>Registra el movimiento de la entidad en su correspondiente mapa</summary>
    public static void RegistrarMovimiento(LivingEntity ent, Vector3Int de, Vector3Int a){
        mapasEspecies[ent.species].Mover(ent, de, a);
    }

    ///<summary>Registra la muerte de la entidad en su mapa y actualiza las causas de muerte</summary>
    public static void RegistrarMuerte(LivingEntity ent, CauseOfDeath causa){
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
        var presas = presasDepredador[dep.species];
        foreach (var p in presas) {
            foreach (var ent in mapasEspecies[p].ObtenerEntidades(desde, distanciaV)){
                if(Vector3Int.Distance(ent.coord, desde) <= distanciaV)// && Pathfinder.SeccionVisible(ent.coord.x, ent.coord.z, desde.x, desde.z))
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
                //if(Pathfinder.SeccionVisible(yoAnimal.coord.x, yoAnimal.coord.z, animalSer.coord.z, animalSer.coord.z))
                    lista.Add(ser);
        }
        return lista;
    }

    ///<summary>Detecta el depredador mas cercano dentro del rango de vision</summary>
    public static LivingEntity SentirDepredador(LivingEntity yo, int distanciaV){
        List<LivingEntity> listaDepredadores = new List<LivingEntity>();
        foreach (var especieDepredador in depredadoresPresa[yo.species]){
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
            for (int y = -1; y <= 1; y++) {
                //Saltamos nuestra propia tile
                if(x==0&&y==0)
                    continue;
                
                var X = origen.x + x;
                if(X < 0)
                    X=0;
                if(X >= tamaño)
                    X=tamaño-1;
                var Y = origen.z + y;
                if(Y < 0)
                    Y=0;
                if(Y >= tamaño)
                    Y=tamaño-1;
                if(caminable[X, Y]){
                    //tilesCaminable.Add(Vector3Int.RoundToInt(centros[X,Y]));
                    tilesCaminable.Add(new Vector3Int(X,0,Y));
                }
            }
        }
        //Si se da el caso en el que estamos en una isla o entre arboles, nos quedamos quietos
        return tilesCaminable.Count > 0? tilesCaminable[rnd.Next(0,tilesCaminable.Count)] : origen;
    }

    private static Vector3Int ComprobarOverflow(Vector3Int coord){
        var aux = coord;
        aux.x = aux.x < 0? 0:aux.x; aux.x = aux.x >= tamaño? tamaño-1:aux.x;
        aux.z = aux.z < 0? 0:aux.z; aux.z = aux.z >= tamaño? tamaño-1:aux.z;
        return aux;
    }

    ///<summary>Devolvemos el siguiente tile de manera aleatoria pero con tendencia hacia adelante</summary>
    public static Vector3Int TileTendencia(Vector3Int origen, Vector3Int anterior, float tendencia){
        Vector3Int direccion = (origen - anterior);
        //No hay direccion, devolvemos random
        if(direccion == Vector3Int.zero)
            return TileRandom(origen);
        
        Vector3Int recto = ComprobarOverflow(origen+direccion);
        //Probabilidad de ir recto
        if(rnd.NextDouble()<tendencia){
            if(caminable[recto.x, recto.z])
                return recto;
        }
        //Tiende hacia adelante
        else{
            bool diagonal = (direccion.x!=0 && direccion.z!=0)?true:false;
            List<Vector3Int> vecinosValidos = new List<Vector3Int>();
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
                return vecinosValidos.Count>0? vecinosValidos[rnd.Next(0,vecinosValidos.Count)]:TileRandom(origen);
            }
            else{//No nos hemos movido en diagonal
                //Nos hemos movido en z
                if(direccion.z!=0){
                    if(caminable[recto.x, recto.z])
                        vecinosValidos.Add(recto);
                    var aux = ComprobarOverflow(new Vector3Int(recto.x-1,0, recto.z));
                    if(caminable[aux.x, aux.z])
                        vecinosValidos.Add(aux);
                    aux = ComprobarOverflow(new Vector3Int(recto.x+1, 0, recto.z));
                    if(caminable[aux.x, aux.z])
                        vecinosValidos.Add(aux);
                    return vecinosValidos.Count>0? vecinosValidos[rnd.Next(0,vecinosValidos.Count)]:TileRandom(origen);
                }
                //Nos hemos movido en x
                if(direccion.x!=0){
                    if(caminable[recto.x, recto.z])
                        vecinosValidos.Add(recto);
                    var aux = ComprobarOverflow(new Vector3Int(recto.x,0, recto.z-1));
                    if(caminable[aux.x, aux.z])
                        vecinosValidos.Add(aux);
                    aux = ComprobarOverflow(new Vector3Int(recto.x, 0, recto.z+1));
                    if(caminable[aux.x, aux.z])
                        vecinosValidos.Add(aux);
                    return vecinosValidos.Count>0? vecinosValidos[rnd.Next(0,vecinosValidos.Count)]:TileRandom(origen);
                }

            }
        }
        return TileRandom(origen);
    }

    ///<summary>Funcion auxiliar en la que recorremos las tiles caminables para igualar mapaNodos.caminables a ellos. Asi hacemos que el agua NO sea caminable</summary>
    private void CaminablesAgua(){
        for (int x = 0; x < caminable.GetLength(0); x++) {
            for (int y = 0; y < caminable.GetLength(1); y++) {
                mapaNodos[x,y].caminable = caminable[x,y];
            }
        }
    }

    // Call terrain generator and cache useful info
    //NOTA: CAMBIAR
    public void Init () {
        if(crearTxt) 
            CrearArchivo();
        //Contador de tiempo
        var sw = System.Diagnostics.Stopwatch.StartNew ();

        //objeto generador de terreno
        /*var terrainGenerator = FindObjectOfType<TerrainGenerator> ();
        datosTerreno = terrainGenerator.Generate ();*/
        var terrainGenerator = FindObjectOfType<GeneradorTerreno> ();
        datosTerreno = terrainGenerator.GenerarMeshTerreno();

        centros = datosTerreno.centros;
        //Terreno por el que se puede caminar
        caminable = datosTerreno.caminables;
        tamaño = datosTerreno.centros.GetLength(0);

        mapaNodos = new Nodo[tamaño,tamaño];
        CaminablesAgua();//Tenemos en cuenta el agua para que no sea caminable
        print("Start 1 Long matriz mapaNodos: " + mapaNodos.GetLength(0) + "," + mapaNodos.GetLength(1));
        coordenadasCaminables = new List<Vector3Int>();

        //Numero de ESPECIES
        int numSpecies = 4;//System.Enum.GetNames (typeof (Species)).Length;
        //Lista de animales a devorar por cada especie
        presasDepredador = new Dictionary<Species, List<Species>> ();
        //preyBySpecies = new Dictionary<Species, List<Species>> ();
        //Lista de depredadores de cada especie
        depredadoresPresa = new Dictionary<Species, List<Species>> ();
        //predatorsBySpecies = new Dictionary<Species, List<Species>> ();

        // Init species maps
        mapasEspecies = new Dictionary<Species, Mapa> ();
        for (int i = 0; i < numSpecies; i++) {
            Species species = (Species) (1 << i);
            mapasEspecies.Add (species, new Mapa (tamaño, tamañoRegionesMapa));

            presasDepredador.Add (species, new List<Species> ());
            depredadoresPresa.Add (species, new List<Species> ());
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
                        depredadoresPresa[(Species) huntedSpecies].Add (hunter.species);
                    }
                }
            }
        }

        //LogPredatorPreyRelationships();

        SpawnArboles();

        //walkableNeighboursMap = new Vector3Int[size, size][];
        // Find and store all walkable neighbours for each walkable tile on the map
        /*for (int y = 0; y < datosTerreno.size; y++) {
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
        }*/

        // Generate offsets within max view distance, sorted by distance ascending
        // Used to speed up per-tile search for closest water tile
        List<Coord> viewOffsets = new List<Coord> ();
        //Como casi nunca van a llegar a ver mas de 20, usamos 20 para encontrar las aguas mas cercanas
        int viewRadius = 20;
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

        // Find closest accessible water tile for each tile on the map:
        aguaMasCercana = new Vector3Int[tamaño, tamaño];
        for (int y = 0; y < datosTerreno.centros.GetLength(0); y++) {
            for (int x = 0; x < datosTerreno.centros.GetLength(0); x++) {
                bool foundWater = false;
                if (caminable[x, y]) {
                    //ViewOffsets son el numero de tiles que podemos llegar a ver, desde -20 a 20 sin incluir 0
                    for (int i = 0; i < viewOffsets.Count; i++) {
                        int targetX = x + viewOffsetsArr[i].x;
                        int targetY = y + viewOffsetsArr[i].y;
                        if (targetX >= 0 && targetX < tamaño && targetY >= 0 && targetY < tamaño) {
                            if (datosTerreno.shore[targetX, targetY]) {
                                //if (EnvironmentUtility.TileIsVisibile (x, y, targetX, targetY)) {
                                //if (Pathfinder.SeccionVisible(x, y, targetX, targetY)) {
                                    aguaMasCercana[x, y] = new Vector3Int (targetX, 0, targetY);
                                    foundWater = true;
                                    break;
                                //}
                            }
                        }
                    }
                }
                if (!foundWater) {
                    aguaMasCercana[x, y] = invalid;
                }
            }
        }
        Debug.Log ("Init time: " + sw.ElapsedMilliseconds);
    }

    ///<summary>Spawneamos la poblacion inicial en coordenadas (caminables) aleatorias del mapa</summary>
    private void SpawnPoblacionInicial(){
        foreach (Poblacion pop in poblacionInicial){
            for (int i = 0; i < pop.count; i++) {
                if(pop.count <= 0)
                    break;
                Vector3Int coordenada = coordenadasCaminables[rnd.Next(0, coordenadasCaminables.Count)];
                SpawnLivingEntity(pop.prefab, coordenada);
            }
        }
    }

    //Spawn normal
    ///<summary>Dada una entidad y una coordenada, las spawnea. Da valores aleatorios a su hambre, sed y reproductiveUrge</summary>
    public void SpawnLivingEntity(LivingEntity prefab, Vector3Int pos){
        var ent = Instantiate(prefab);
        ent.Init(pos);
        mapasEspecies[ent.species].Añadir(ent, pos);
        if(ent.species == Species.Fox){
            ParametrosAleatorios(ent, 0.4f);
            ent.transform.parent = foxHolder.transform;
        }
        if(ent.species == Species.Rabbit){
            ParametrosAleatorios(ent, 0.4f);
            ent.transform.parent = rabbitHolder.transform;
        }
        if(ent.species == Species.Plant){
            ent.transform.parent = plantHolder.transform;
        }
    }

    //Spawn de reproduccion sexual. Es la funcion a la que se llama desde Animal
    ///<summary>Dada una entidad, una coordenada y padre y madre, spawnea un hijo a traves de la reproduccion sexual</summary>
    //public static LivingEntity SpawnLivingEntitySR(LivingEntity prefab, Vector3Int pos, Animal fath, Animal moth){
    public static LivingEntity SpawnLivingEntitySR(LivingEntity prefab, Vector3Int pos, Animal.Padres fath, Animal.Padres moth){
        var ent = Instantiate(prefab);
        ( (Animal) ent).fatherVals = fath;
        ( (Animal) ent).motherVals = moth;
        ent.cria = true;
        //Reseteamos los valores de la cria
        ((Animal) ent).edad = 0f;((Animal) ent).hunger = 0f;((Animal) ent).thirst = 0f;((Animal) ent).reproductiveUrge = 0f;

        ent.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        ent.Init(pos);
        mapasEspecies[ent.species].Añadir(ent, pos);

        var foxHolderAux = GameObject.Find("FoxHolder");
        var rabbitHolderAux = GameObject.Find("RabbitHolder");
        var plantHolderAux = GameObject.Find("PlantHolder");
        print("Spawneamos un: " + ent.species);

        //Colocamos la entidad en los holders
        if(ent.species == Species.Fox)
            ent.transform.parent = foxHolderAux.transform;
        if(ent.species == Species.Rabbit)
            ent.transform.parent = rabbitHolderAux.transform;
        if(ent.species == Species.Plant)
            ent.transform.parent = plantHolderAux.transform;
        
        return ent;
    }

    ///<summary>Spawnea arboles y calcula la variable coordenadasCaminables. Los arboles spawnean con diferentes rotaciones y escalas.
    ///NOTA: EN EL FUTURO TAMBIEN ALTERAR LIGERAMENTE EL COLOR O INCLUSO EN FUNCION DE LA TILE EN LA QUE SE ENCUENTRE</summary>
    private void SpawnArboles(){
        var holder = GameObject.Find("TreeHolder");
        for (int x = 0; x < tamaño; x++) {
            for (int y = 0; y < tamaño; y++) {
                //PRUEBA MAPA NODOS
                mapaNodos[x,y] = new Nodo(Vector3Int.FloorToInt(centros[x,y]), (-1,-1));
                if(caminable[x,y]){
                    if(rnd.NextDouble() < probabilidadArboles){
                        var rotX = (float) rnd.NextDouble(); var rotY = (float) rnd.NextDouble(); var rotZ = (float) rnd.NextDouble();
                        Quaternion rotacion = Quaternion.Euler(rotX, rotY, rotZ);
                        var scale = (float) (rnd.Next(5,10)/10f);
                        MeshRenderer arbol = Instantiate(prefabArbol, centros[x,y], rotacion);
                        //Prueba color aleatorio
                        //System.Random randomAux = new System.Random(x+y);
                        //arbol.material.color += new Color(randomAux.Next(0,10)/100f, randomAux.Next(0,10)/100f, 0);
                        //Color pruebaColor = arbol.material.color + new Color(randomAux.Next(0,10)/100f, randomAux.Next(0,10)/100f, 0);
                        arbol.transform.parent = holder.transform;
                        arbol.transform.localScale *= scale;
                        caminable[x,y] = false;
                        //PRUEBA MAPA NODOS
                        mapaNodos[x,y].caminable = false;
                    }
                    else{
                        //print("Añadimos centro a la lista de coordenadasCaminables: " + centros[x,y]);
                        //coordenadasCaminables.Add(Vector3Int.FloorToInt(centros[x,y] + new Vector3(centros.GetLength(0)/2f, 0f, centros.GetLength(1)/2f)) );
                        coordenadasCaminables.Add(Vector3Int.FloorToInt(centros[x,y]) );
                    }
                }
            }
            
        }
    }

    ///<summary>Da valores aleatorios al hambre, sed y reproductive urge del ente entre [0,maxRango)</summary>
    private void ParametrosAleatorios(LivingEntity ent, float maxRango){
        Animal an = (Animal) ent;
        an.hunger = AletorioRango(0.1f, maxRango);
        an.thirst = AletorioRango(0.1f, maxRango);
        an.reproductiveUrge = AletorioRango(0.1f, maxRango);
        an.edad = AletorioRango(0.3f, maxRango);
        //print("Hambre: " + an.hunger + " sed: " + an.thirst + " reproductive urge: " + an.reproductiveUrge + " edad: " + an.edad);
    }

    ///<summary>Devuelve un float en el rango [min,max)</summary>
    private float AletorioRango(float min, float max){
        int minAux = (int)(min*10);
        int maxAux = (int)(max*10);
        //print("min float: " + min + " min int: " + minAux +"max float: " + max + " max int: " + maxAux);
        int aux = rnd.Next(minAux, maxAux);
        //print("Aux: " + aux);
        //print("resultado: " + (aux / 10f));
        return (aux / 10f);
    }

    ///<summary>Dada una matriz de Nodo la recorre y resetea sus valores</summary>
    public static Nodo[,] ResetMapaNodos(Nodo[,] mapa){
        for (int x = 0; x < mapa.GetLength(0); x++){
            for (int y = 0; y < mapa.GetLength(0); y++){
                mapa[x,y].g = int.MaxValue;
                mapa[x,y].h = 0;
                mapa[x,y].f = mapa[x,y].g + mapa[x,y].h;
                mapa[x,y].vieneDe = (-1,-1);
            }
        }
        return mapa;
    }

    public bool crearTxt = false;
    private string path;
    private void CrearArchivo(){
        var fechaSinBarras = System.DateTime.Now.Year.ToString() +"-" + System.DateTime.Now.Month.ToString() +"-" 
        + System.DateTime.Now.Day.ToString() +"-" + System.DateTime.Now.Hour.ToString() + "-" + System.DateTime.Now.Minute.ToString() + "-"
        + System.DateTime.Now.Second.ToString();
        path = Application.dataPath + "/Logs/CausasMuerte/causasMuerte_" + fechaSinBarras + ".txt";
        //Si no existe, lo creamos
        if(!File.Exists(path)){
            File.WriteAllText(path, "Causas muerte\nConejos \t Zorros\n");
        }
        //Si existe, lo borramos y creamos uno nuevo
        else{
            File.Delete(path);
            File.WriteAllText(path, "Causas muerte\n Conejos \t Zorros\n");
        }
    }
    private void OnApplicationQuit() {
        if(crearTxt){
            var eC = "Eaten: " + AdministradorCausasMuerte.eatenText[1].text;var eZ = "Eaten: " + AdministradorCausasMuerte.eatenText[0].text;
            var hC = "Hunger: "+ AdministradorCausasMuerte.hungerText[1].text;var hZ = "Hunger: "+ AdministradorCausasMuerte.hungerText[0].text;
            var tC = "Thirst: "+ AdministradorCausasMuerte.thirstText[1].text;var tZ = "Thirst: "+ AdministradorCausasMuerte.thirstText[0].text;
            var aC = "Age: "+ AdministradorCausasMuerte.ageText[1].text;var aZ = "Age: "+ AdministradorCausasMuerte.ageText[0].text;
            File.AppendAllText(path,  eC + "\t" + eZ +"\n" + hC + "\t" + hZ +"\n" + tC + "\t" + tZ +"\n" + aC + "\t" + aZ +"\n");
        }
    }

    //Vamos a dibujar un mapa para los conejos
    void OnDrawGizmosSelected() {
        var aux = Color.blue;
        aux.a = 0.1f;
        Gizmos.color = aux;
        Mapa con = mapasEspecies[Species.Rabbit];
        for (int x = 0; x < con.centros.GetLength(0); x++){
            for (int y = 0; y < con.centros.GetLength(1); y++){
                Gizmos.DrawCube(con.centros[x,y], new Vector3(con.tamañoRegion, con.tamañoRegion, con.tamañoRegion));
            }
        }
        /*foreach (var m in mapasEspecies){
            Mapa aux = m.Value;
            for (int x = 0; x < aux.centros.GetLength(0); x++){
                for (int y = 0; y < aux.centros.GetLength(1); y++){
                    Gizmos.DrawCube(aux.centros[x,y], new Vector3(aux.tamañoRegion, aux.tamañoRegion, aux.tamañoRegion));
                }
            }
        }*/
    }

    //Estructura para guardar la poblacion de un tipo de entidad viva
    [System.Serializable]
    public struct Poblacion {
        public LivingEntity prefab;
        public int count;
    }
    
    ///<summary>Estructura de nodos para usar el algoritmo A*</summary>
    public struct Nodo{
        public Vector3Int centro;
        public bool caminable;
        public int g;
        public int h;
        public int f;
        public (int,int) vieneDe;
        public Nodo(Vector3Int cent, (int,int) vieneDeInvalido){
            this.centro = cent;
            this.caminable = true;
            this.g = int.MaxValue;
            this.h=0;
            this.f=this.g + this.h;
            this.vieneDe=vieneDeInvalido;
        }
    }
}
