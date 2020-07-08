using System.Collections;
using System.Collections.Generic;
using TerrainGeneration;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

public class Environment : MonoBehaviour {

    ///<summary>Tamaño de las regiones en las que se divide el mapa</summary>
    const int mapRegionSize = 10;
    //Seed para la generacion aleatoria
    public int seed;

    //Probabilidad ajustable de aparicion de arboles
    [Header ("Trees")]
    public MeshRenderer treePrefab;
    [Range (0, 1)]
    public float treeProbability;

    //[Header ("Loquesea")] es para poner un texto en el inspector de unity
    [Header ("Populations")]
    public Population[] initialPopulations;

    [Header ("Debug")]
    public bool showMapDebug;
    public Transform mapCoordTransform;
    public float mapViewDst;

    //Informacion ESTATICA cacheada que usamos muchas veces, por eso esta "cacheada"
    // Cached data:
    public static Vector3[, ] tileCentres;
    //Array de tuplas [(X,Y)] con las coordenadas no ocupadas
    public static bool[, ] walkable;
    //Variable estatica con el tamaño del mapa
    static int size;
    static Coord[, ][] walkableNeighboursMap;
    static List<Coord> walkableCoords;

    static Dictionary<Species, List<Species>> preyBySpecies;
    static Dictionary<Species, List<Species>> predatorsBySpecies;

    // array of visible tiles from any tile; value is Coord.invalid if no visible water tile
    static Coord[, ] closestVisibleWaterMap;

    static System.Random prng;
    TerrainGenerator.TerrainData terrainData;

    ///<summary></summary>
    static Dictionary<Species, Map> speciesMaps;

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
    private int indice =  0;

    //Carpetas donde guardar las entidades
    private GameObject foxHolder;
    private GameObject plantHolder;
    private GameObject rabbitHolder;
    [HideInInspector]
    public float velocidadZorros = 0f;
    [HideInInspector]
    public float velocidadConejos = 0f;
    [HideInInspector]
    public int radioVisionConejos = 0;
    [HideInInspector]
    public int radioVisionZorros = 0;

    private GameObject menuCausasMuerte;

    //Actualizamos los contadores
    //NOTA: EN UN FUTURO TAMBIEN SE DEBERIAN DE PASAR POR AQUI LOS DATOS PARA LAS GRAFICAS
    void Update()
    {
        if(speciesMaps[Species.Rabbit].numEntities == 0 && speciesMaps[Species.Fox].numEntities == 0){
            //print("NO QUEDAN ANIMALES VIVOS");
        }

        UpdateGrafs();
        //Actualizamos los contadores de la pantalla
        //NOTA: Puede que no se deba de actualziar con cada fotograma, deberia de bastar con una vez cada segundo
        //pero no deberia de afectar en nanda a la eficiencia
        contadorConejo.text = speciesMaps[Species.Rabbit].numEntities.ToString();
        contadorZorro.text = speciesMaps[Species.Fox].numEntities.ToString();
        contadorPlanta.text = speciesMaps[Species.Plant].numEntities.ToString();
    }

    /// <summary>Inicializa las listas de animales que usamos para imprimir en pantalla con las poblaciones iniciales </summary>
    private void StartListaAnimales(){
        for (int i = 0; i < initialPopulations.Length; i++)
        {
            switch (initialPopulations[i].prefab.species)
            {
                case Species.Rabbit:
                    grafConejos.Add(initialPopulations[i].count);
                break;
                case Species.Fox:
                    grafZorros.Add(initialPopulations[i].count);
                break;
                case Species.Plant:
                    grafPlantas.Add(initialPopulations[i].count);
                break;
            }
        }
    }

    /// <summary>Actualiza las variables grafZorros y grafConejos. Se le va a llamar una vez cada segundo </summary>
    void UpdateGrafs(){
        int zorros = speciesMaps[Species.Fox].numEntities;
        int conejos = speciesMaps[Species.Rabbit].numEntities;
        int plantas = speciesMaps[Species.Plant].numEntities;
        grafZorros.Add(zorros);
        grafConejos.Add(conejos);
        grafPlantas.Add(plantas);
    }

    /// <summary>Spawnea plantas de manera aleatoria en el mapa</summary>
    void SpawnPlantasTiempo(){
        //seed auxiliar que cambia cada vez. Si usamos la variable seed, al ser siempre el mismo valor
        //genera siempre el mismo numero random
        var aux_seed = System.DateTime.Now.Millisecond;
        var spawnPrng = new System.Random (aux_seed);
        //Lista de coordenadas spawneables, haya donde se pueda andar
        var spawnCoords = new List<Coord> (walkableCoords);
        if (spawnCoords.Count == 0) {
            Debug.Log ("No hay sitio para spawnear la planta");
            return;
        }
        int spawnCoordIndex = spawnPrng.Next (0, spawnCoords.Count);
        Coord coord = spawnCoords[spawnCoordIndex];

        //Instanciamos una planta
        foreach (var population in initialPopulations) {
            if(population.prefab.species == Species.Plant){
                var entity = Instantiate (population.prefab);
                entity.Init (coord);

                //Almacenamos la nueva entidad en su correspondiente lista en funcion de la especie
                speciesMaps[entity.species].Add (entity, coord);
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

        prng = new System.Random ();
        Init ();
        SpawnInitialPopulations ();
        menuCausasMuerte = GameObject.Find("CausasMuerte");
        //GameObject.Find("MenuGraficas").SetActive(false);
    }

    public int GetSpeciesNumber(Species specie){
        return speciesMaps[specie].numEntities;
    }

    //Mirar como funciona DrawGizmos
    void OnDrawGizmos () {
//    void OnDrawGizmosSelected() {
        //Comentado de origen
        //Dibuja el campo de vision de los animales?????
        /* 
        if (showMapDebug) {
            if (preyMap != null && mapCoordTransform != null) {
                Coord coord = new Coord ((int) mapCoordTransform.position.x, (int) mapCoordTransform.position.z);
                preyMap.DrawDebugGizmos (coord, mapViewDst);
            }
        }
        */
        //print("He entrado en OnDrawGizmosSelected");
        
    }

    ///<summary>Actualizamos speciesMaps</summary>
    public static void RegisterMove (LivingEntity entity, Coord from, Coord to) {
        speciesMaps[entity.species].Move (entity, from, to);
    }

    //NOTA: al ser una funcion estatica, no puede llamar a objetos no estaticos
    ///<summary>Eliminamos la entidad del mapa de entidades y actualizamos la lista de causas de muerte</summary>
    public static void RegisterDeath (LivingEntity entity, CauseOfDeath cause) {
        speciesMaps[entity.species].Remove (entity, entity.coord);
        AdministradorCausasMuerte.ActualizarCausas(entity.species, cause);
        //Si es el ultimo ser vivo, lo hacemos saber
        if (speciesMaps.Count >= 0)
        {

        }
        else{
            //print("NO QUEDAN ANIMALES VIVOS.");
        }

    }

    public static Coord SenseWater (Coord coord, int viewDistance) {
        var closestWaterCoord = closestVisibleWaterMap[coord.x, coord.y];
        if (closestWaterCoord != Coord.invalid) {
            float sqrDst = (tileCentres[coord.x, coord.y] - tileCentres[closestWaterCoord.x, closestWaterCoord.y]).sqrMagnitude;
            if (sqrDst <= viewDistance * viewDistance) {
                return closestWaterCoord;
            }
        }
        return Coord.invalid;
    }

    //Dada una coordenada, un animal y una lista de preferencias de comida devuelve la presa mas cercana (sea conejo o planta)
    public static LivingEntity SenseFood (Coord coord, Animal self, System.Func<LivingEntity, LivingEntity, int> foodPreference) {
        var foodSources = new List<LivingEntity> ();

        List<Species> prey = preyBySpecies[self.species];
        for (int i = 0; i < prey.Count; i++) {

            Map speciesMap = speciesMaps[prey[i]];

            foodSources.AddRange (speciesMap.GetEntities (coord, self.maxViewDistance));
        }

        // Sort food sources based on preference function. NOTA: Mirar como alterar la funcion foodPreference para que tenga en cuenta la velocidad... del animal
        foodSources.Sort ((a, b) => foodPreference ((LivingEntity)self, a).CompareTo (foodPreference ((LivingEntity)self, b)));

        // Return first visible food source
        for (int i = 0; i < foodSources.Count; i++) {
            Coord targetCoord = foodSources[i].coord;
            if (EnvironmentUtility.TileIsVisibile (coord.x, coord.y, targetCoord.x, targetCoord.y)) {
                return foodSources[i];
            }
        }
        return null;
    }

    ///<summary> Return list of animals of the same species, with the opposite gender, who are also searching for a mate</summary>
    public static List<Animal> SensePotentialMates (Coord coord, Animal self) {
        Map speciesMap = speciesMaps[self.species];
        List<LivingEntity> visibleEntities = speciesMap.GetEntities (coord, self.maxViewDistance);
        var potentialMates = new List<Animal> ();

        for (int i = 0; i < visibleEntities.Count; i++) {
            var visibleAnimal = (Animal) visibleEntities[i];
            if (visibleAnimal != self && visibleAnimal.genes.isMale != self.genes.isMale) {
                if (visibleAnimal.currentAction == CreatureAction.SearchingForMate) {
                    potentialMates.Add (visibleAnimal);
                }
            }
        }
        return potentialMates;
    }

    ///<summary>Dadas unas coordenadas y la especie de un animal, devuelve el depredador mas cercano</summary>
    public static Coord SenseDepredador(Species especie, Coord coord, int viewDistance){
        //NOTA: Creo que funciona, pero hay que repasarlo porsiaca
        //Lista de depredadores de nuestra especie
        var listaEspecieDepredadores = predatorsBySpecies[especie];
        //Lista que va a conetener los mapas de los depredadores de nuestra especie
        List<Map> mapasDepredadores = new List<Map>();
        for (int i = 0; i < listaEspecieDepredadores.Count; i++) {
            mapasDepredadores.Add(speciesMaps[ listaEspecieDepredadores[i] ]);
        }
        for (int i = 0; i < mapasDepredadores.Count; i++) {
            //Lista de zorros a la vista
            List<LivingEntity> depredadoresVisibles = mapasDepredadores[i].GetEntities(coord, viewDistance);
            if (depredadoresVisibles.Count > 0) {
                int indiceMasCercano = 0;
                //print("Numero de zorros visibles: " + zorrosVisibles.Count);
                for (int j = 0; j < depredadoresVisibles.Count; j++) {
                    //zorrosVisibles[indiceMasCercano] = zorrosVisibles[i];
                    if (Coord.Distance(depredadoresVisibles[indiceMasCercano].coord, coord) <= Coord.Distance(depredadoresVisibles[j].coord, coord) ) {
                        indiceMasCercano = j;
                    }
                }
                return depredadoresVisibles[indiceMasCercano].coord;
            }
            else {
                return new Coord(0,0);
            }
        }
        return new Coord(0,0);

        //esto funciona
        /*Map mapaZorros = speciesMaps[Species.Fox];
        //Lista de zorros a la vista
        List<LivingEntity> zorrosVisibles = mapaZorros.GetEntities(coord, Animal.maxViewDistance);
        if (zorrosVisibles.Count > 0) {
            int indiceMasCercano = 0;
            //print("Numero de zorros visibles: " + zorrosVisibles.Count);
            for (int i = 0; i < zorrosVisibles.Count; i++) {
                //zorrosVisibles[indiceMasCercano] = zorrosVisibles[i];
                if (Coord.Distance(zorrosVisibles[indiceMasCercano].coord, coord) <= Coord.Distance(zorrosVisibles[i].coord, coord) ) {
                    indiceMasCercano = i;
                }
            }
            return zorrosVisibles[indiceMasCercano].coord;
        }
        else {
            return new Coord(0,0);
        }*/
    }

    ///<summary> Dadas unas coordenadas, devuelve todo la planta y agua mas cercanos </summary>
    public static Surroundings Sense (Coord coord, int viewDistance) {
        var closestPlant = speciesMaps[Species.Plant].ClosestEntity (coord, viewDistance);
        var surroundings = new Surroundings ();
        surroundings.nearestFoodSource = closestPlant;
        surroundings.nearestWaterTile = closestVisibleWaterMap[coord.x, coord.y];

        return surroundings;
    }

    ///<summary>Dadas unas coordenadas, devuelve la siguiente tile donde se pueda caminar</summary>
    public static Coord GetNextTileRandom (Coord current) {
        var neighbours = walkableNeighboursMap[current.x, current.y];
        if (neighbours.Length == 0) {
            return current;
        }
        return neighbours[prng.Next (neighbours.Length)];
    }

    /// Get random neighbour tile, weighted towards those in similar direction as currently facing
    public static Coord GetNextTileWeighted (Coord current, Coord previous, double forwardProbability = 0.2, int weightingIterations = 3) {

        if (current == previous) {

            return GetNextTileRandom (current);
        }

        Coord forwardOffset = (current - previous);
        // Random chance of returning foward tile (if walkable)
        if (prng.NextDouble () < forwardProbability) {
            Coord forwardCoord = current + forwardOffset;
            //Comprobamos que sea una coordenada valida
            if (forwardCoord.x >= 0 && forwardCoord.x < size && forwardCoord.y >= 0 && forwardCoord.y < size) {
                if (walkable[forwardCoord.x, forwardCoord.y]) {
                    return forwardCoord;
                }
            }
        }

        // Get walkable neighbours
        var neighbours = walkableNeighboursMap[current.x, current.y];
        if (neighbours.Length == 0) {
            return current;
        }

        // From n random tiles, pick the one that is most aligned with the forward direction:
        Vector2 forwardDir = new Vector2 (forwardOffset.x, forwardOffset.y).normalized;
        float bestScore = float.MinValue;
        Coord bestNeighbour = current;

        for (int i = 0; i < weightingIterations; i++) {
            Coord neighbour = neighbours[prng.Next (neighbours.Length)];
            Vector2 offset = neighbour - current;
            float score = Vector2.Dot (offset.normalized, forwardDir);
            if (score > bestScore) {
                bestScore = score;
                bestNeighbour = neighbour;
            }
        }

        return bestNeighbour;
    }

    // Call terrain generator and cache useful info
    public void Init () {
        if(crearTxt) 
            CrearArchivo();
        //Contador de tiempo
        var sw = System.Diagnostics.Stopwatch.StartNew ();

        //objeto generador de terreno
        var terrainGenerator = FindObjectOfType<TerrainGenerator> ();
        terrainData = terrainGenerator.Generate ();

        tileCentres = terrainData.tileCentres;
        //Terreno por el que se puede caminar
        walkable = terrainData.walkable;
        size = terrainData.size;

        //Numero de ESPECIES
        int numSpecies = System.Enum.GetNames (typeof (Species)).Length;
        //Lista de animales a devorar por cada especie
        preyBySpecies = new Dictionary<Species, List<Species>> ();
        //Lista de depredadores de cada especie
        predatorsBySpecies = new Dictionary<Species, List<Species>> ();

        // Init species maps
        speciesMaps = new Dictionary<Species, Map> ();
        for (int i = 0; i < numSpecies; i++) {
            Species species = (Species) (1 << i);
            speciesMaps.Add (species, new Map (size, mapRegionSize));

            preyBySpecies.Add (species, new List<Species> ());
            predatorsBySpecies.Add (species, new List<Species> ());
        }

        // Store predator/prey relationships for all species
        for (int i = 0; i < initialPopulations.Length; i++) {

            if (initialPopulations[i].prefab is Animal) {
                Animal hunter = (Animal) initialPopulations[i].prefab;
                Species diet = hunter.diet;

                for (int huntedSpeciesIndex = 0; huntedSpeciesIndex < numSpecies; huntedSpeciesIndex++) {
                    int bit = ((int) diet >> huntedSpeciesIndex) & 1;
                    // this bit of diet mask set (i.e. the hunter eats this species)
                    if (bit == 1) {
                        int huntedSpecies = 1 << huntedSpeciesIndex;
                        preyBySpecies[hunter.species].Add ((Species) huntedSpecies);
                        predatorsBySpecies[(Species) huntedSpecies].Add (hunter.species);
                    }
                }
            }
        }

        LogPredatorPreyRelationships ();

        SpawnTrees ();

        walkableNeighboursMap = new Coord[size, size][];

        // Find and store all walkable neighbours for each walkable tile on the map
        for (int y = 0; y < terrainData.size; y++) {
            for (int x = 0; x < terrainData.size; x++) {
                if (walkable[x, y]) {
                    List<Coord> walkableNeighbours = new List<Coord> ();
                    for (int offsetY = -1; offsetY <= 1; offsetY++) {
                        for (int offsetX = -1; offsetX <= 1; offsetX++) {
                            if (offsetX != 0 || offsetY != 0) {
                                int neighbourX = x + offsetX;
                                int neighbourY = y + offsetY;
                                if (neighbourX >= 0 && neighbourX < size && neighbourY >= 0 && neighbourY < size) {
                                    if (walkable[neighbourX, neighbourY]) {
                                        walkableNeighbours.Add (new Coord (neighbourX, neighbourY));
                                    }
                                }
                            }
                        }
                    }
                    walkableNeighboursMap[x, y] = walkableNeighbours.ToArray ();
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

        // Find closest accessible water tile for each tile on the map:
        closestVisibleWaterMap = new Coord[size, size];
        for (int y = 0; y < terrainData.size; y++) {
            for (int x = 0; x < terrainData.size; x++) {
                bool foundWater = false;
                if (walkable[x, y]) {
                    for (int i = 0; i < viewOffsets.Count; i++) {
                        int targetX = x + viewOffsetsArr[i].x;
                        int targetY = y + viewOffsetsArr[i].y;
                        if (targetX >= 0 && targetX < size && targetY >= 0 && targetY < size) {
                            if (terrainData.shore[targetX, targetY]) {
                                if (EnvironmentUtility.TileIsVisibile (x, y, targetX, targetY)) {
                                    closestVisibleWaterMap[x, y] = new Coord (targetX, targetY);
                                    foundWater = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (!foundWater) {
                    closestVisibleWaterMap[x, y] = Coord.invalid;
                }
            }
        }
        Debug.Log ("Init time: " + sw.ElapsedMilliseconds);
    }

    void SpawnTrees () {
        // Settings:
        float maxRot = 4;
        float maxScaleDeviation = .2f;
        float colVariationFactor = 0.15f;
        float minCol = .8f;

        var spawnPrng = new System.Random (seed);
        var treeHolder = new GameObject ("Tree holder").transform;
        walkableCoords = new List<Coord> ();

        for (int y = 0; y < terrainData.size; y++) {
            for (int x = 0; x < terrainData.size; x++) {
                if (walkable[x, y]) {
                    if (prng.NextDouble () < treeProbability) {
                        // Randomize rot/scale
                        float rotX = Mathf.Lerp (-maxRot, maxRot, (float) spawnPrng.NextDouble ());
                        float rotZ = Mathf.Lerp (-maxRot, maxRot, (float) spawnPrng.NextDouble ());
                        float rotY = (float) spawnPrng.NextDouble () * 360f;
                        Quaternion rot = Quaternion.Euler (rotX, rotY, rotZ);
                        float scale = 1 + ((float) spawnPrng.NextDouble () * 2 - 1) * maxScaleDeviation;

                        // Randomize colour
                        float col = Mathf.Lerp (minCol, 1, (float) spawnPrng.NextDouble ());
                        float r = col + ((float) spawnPrng.NextDouble () * 2 - 1) * colVariationFactor;
                        float g = col + ((float) spawnPrng.NextDouble () * 2 - 1) * colVariationFactor;
                        float b = col + ((float) spawnPrng.NextDouble () * 2 - 1) * colVariationFactor;

                        // Spawn
                        MeshRenderer tree = Instantiate (treePrefab, tileCentres[x, y], rot);
                        tree.transform.parent = treeHolder;
                        tree.transform.localScale = Vector3.one * scale;
                        tree.material.color = new Color (r, g, b);

                        // Mark tile unwalkable
                        walkable[x, y] = false;
                    } else {
                        walkableCoords.Add (new Coord (x, y));
                    }
                }
            }
        }
    }

    //Funcion encargada de spawnear la poblacion inicial
    void SpawnInitialPopulations () {
        //Numero aleatorio en funcion de la seed
        var spawnPrng = new System.Random (seed);
        //Lista de coordenadas spawneables, haya donde se pueda andar
        var spawnCoords = new List<Coord> (walkableCoords);

        //Loop para spawnear los agentes
        //Loop para cada tipo de animal
        foreach (var pop in initialPopulations) {
            //Loop para el numero de animales a spawnear de ese tipo
            for (int i = 0; i < pop.count; i++) {
                //Si ya no queda hueco donde spawnear avisara
                if (spawnCoords.Count == 0) {
                    Debug.Log ("Ran out of empty tiles to spawn initial population");
                    break;
                }
                //Coordenada random
                int spawnCoordIndex = spawnPrng.Next (0, spawnCoords.Count);
                Coord coord = spawnCoords[spawnCoordIndex];
                spawnCoords.RemoveAt (spawnCoordIndex);

                SpawnLivingEntity(pop.prefab, coord);

                //Instanciamos el prefab pop
                //var entity = Instantiate (pop.prefab);
                //entity.Init (coord);

                //Almacenamos la nueva entidad en su correspondiente lista en funcion de la especie
                //speciesMaps[entity.species].Add (entity, coord);
            }
        }
    }

    //Spawn normal
    ///<summary>Dada una entidad y una coordenada, las spawnea</summary>
    public void SpawnLivingEntity(LivingEntity prefab, Coord pos){
        var ent = Instantiate(prefab);
        ent.Init(pos);
        speciesMaps[ent.species].Add(ent, pos);
        if(ent.species == Species.Fox){
            ent.transform.parent = foxHolder.transform;
        }
        if(ent.species == Species.Rabbit){
            ent.transform.parent = rabbitHolder.transform;
        }
        if(ent.species == Species.Plant){
            ent.transform.parent = plantHolder.transform;
        }
    }

    static void PrintGenes(Animal an){
        for (int i = 0; i < an.genes.values.Length; i++)
        {
            print("Gen " + i + " vale: " + an.genes.values[i]);
        }
    }

    //Spawn de reproduccion sexual. Es la funcion a la que se llama desde Animal
    ///<summary>Dada una entidad, una coordenada y padre y madre, spawnea un hijo a traves de la reproduccion sexual</summary>
    //public static LivingEntity SpawnLivingEntitySR(LivingEntity prefab, Coord pos, Animal fath, Animal moth){
    public static LivingEntity SpawnLivingEntitySR(LivingEntity prefab, Coord pos, Animal.Padres fath, Animal.Padres moth){
        var ent = Instantiate(prefab);
        ( (Animal) ent).fatherVals = fath;
        ( (Animal) ent).motherVals = moth;
        ent.cria = true;

        ent.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        ent.Init(pos);
        speciesMaps[ent.species].Add(ent, pos);

        var foxHolderAux = GameObject.Find("FoxHolder");
        var rabbitHolderAux = GameObject.Find("RabbitHolder");
        var plantHolderAux = GameObject.Find("PlantHolder");
        print("Spawneamos un: " + ent.species);
        if(ent.species == Species.Fox){
            ent.transform.parent = foxHolderAux.transform;
        }
        if(ent.species == Species.Rabbit){
            ent.transform.parent = rabbitHolderAux.transform;
        }
        if(ent.species == Species.Plant){
            ent.transform.parent = plantHolderAux.transform;
        }
        /*if(prefab.species == Species.Fox)
            velocidadZorros += ((Animal) ent).moveSpeed;
        else
            velocidadConejos += ((Animal) ent).moveSpeed;*/
        return ent;
    }

    //Cargamos las relaciones presa-depredador
    void LogPredatorPreyRelationships () {
        int numSpecies = System.Enum.GetNames (typeof (Species)).Length;
        for (int i = 0; i < numSpecies; i++) {
            string s = "(" + System.Enum.GetNames (typeof (Species)) [i] + ") ";
            int enumVal = 1 << i;
            var prey = preyBySpecies[(Species) enumVal];
            var predators = predatorsBySpecies[(Species) enumVal];

            s += "Prey: " + ((prey.Count == 0) ? "None" : "");
            for (int j = 0; j < prey.Count; j++) {
                s += prey[j];
                if (j != prey.Count - 1) {
                    s += ", ";
                }
            }

            s += " | Predators: " + ((predators.Count == 0) ? "None" : "");
            for (int j = 0; j < predators.Count; j++) {
                s += predators[j];
                if (j != predators.Count - 1) {
                    s += ", ";
                }
            }
            print (s);
        }
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

    //--------------SETTERS PARA LA CONFIGURACION-----------------------
    public void setSeed(string input){
        seed = int.Parse(input);
    }
    public void setTreePrefab(string input){
        //tre = int.Parse(input);
    }
    public void setTreeProbab(float input){
        treeProbability = input;
    }
    public void setSize(string input){
        size = int.Parse(input);
    }
    public void setShowMapDebug(bool input){
        showMapDebug = input;
    }
    public void setMapCoordTransform(string input){
        //tre = int.Parse(input);
    }
    public void setMapViewDistance(string input){
       mapViewDst = float.Parse(input);
    }

    public int getIndice(){
        return indice;
    }

    public List<LivingEntity> GetInitialPopulations(){
        var aux =  new List<LivingEntity>();
        foreach (var pop in initialPopulations)
        {
            aux.Add(pop.prefab);
        }
        return aux;
    }
}