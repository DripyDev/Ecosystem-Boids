using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

//Animal hereda de LivingEntity
public class Animal : LivingEntity {
    ///<summary>Campo de vision del animal.</summary>
    public int maxViewDistance = 10;

    [EnumFlags]
    public Species diet;

    public CreatureAction currentAction;
    public Genes genes;
    public int[] val;
    public Color maleColour;
    public Color femaleColour;
    [HideInInspector]
    public Environment env;

    // Settings:
    ///<summary>Tiempo minimo que tiene que pasar entre acciones
    ///NOTA: Puede que haya que cambiar esto para simular que algunos animales sean mas rapidos</summary>
    protected float timeBetweenActionChoices = 1;
    ///<summary>Velocidad de animacion del movimiento. Cuanto mas alto, menos tardan en animar el movimiento y mas rapido vuelven a tomar decisiones
    ///Cambiar esta variable si queremos que se muevan mas lentos o rapidos</summary>
    public float moveSpeed = 1.5f;
    public const float timeToDeathByHunger = 200;
    public const float timeToDeathByThirst = 200;
    public const float timeToDeathByAge = 2000;

    //Ratios en funcion de los genes
    public float ratioCrecimiento = 1f;
    public float ratioReproductiveUrge = 1f;
    public float ratioTiempoEmbarazo = 1f;

    float drinkDuration = 6;
    float eatDuration = 10;
    ///<summary>Tiempo de animacion para la reproduccion. NO IMPLEMENTADO</summary>
    float matingDuration = 20;
    //Rango critico a partir del cual empiezan a buscar comida o agua
    protected float criticalPercent = 0.7f;

    // Visual settings:
    float moveArcHeight = .2f;

    // State:
    [Header ("State")]
    public float hunger;
    public float thirst;
    public float edad;
    ///<summary>Ganas de reproducirse, ira en funcion de los genes</summary>
    public float reproductiveUrge;
    public bool embarazada = false;
    public Padres fatherVals;
    public Padres motherVals;
    ///<summary>Aumenta cuando embarazada y cuando llega a 1 se da a luz. NOTA: Ira en funcion de genes</summary>
    public float tiempoParaParto;

    public List<Animal> potentialMates;

    public List<Animal> machosRechazados;
    public List<Animal> hembrasNoImpresionadas;

    protected LivingEntity foodTarget;
    protected Coord waterTarget;

    // Move data:
    protected bool animatingMovement;
    Coord moveFromCoord;
    Coord moveTargetCoord;
    Vector3 moveStartPos;
    Vector3 moveTargetPos;
    float moveTime;
    float moveSpeedFactor;
    float moveArcHeightFactor;
    //Array de coordenadas que es el camino que va a coger el agente
    protected Coord[] path;
    int pathIndex;

    // Other
    ///<summary>Tiempo en segundos desde la ultima actionChooseTime</summary>
    protected float lastActionChooseTime;
    const float sqrtTwo = 1.4142f;
    const float oneOverSqrtTwo = 1 / sqrtTwo;

    ///<summary>Hacemos los cambios necesarios en funcion de los genes del animal</summary>
    void ComprobarGenes(){
        material.color = (genes.isMale) ? maleColour : femaleColour;
        //NO ESTA SUMANDO BIEN LAS VELOCIDADES
        //Prueba gen velocidad:
        //SI LA VELOCIDAD EN FUNCION DE SI TIENE PADRES O NO LA PONEMOS EN INIT, AQUI SOLO HAY QUE SUMARLE Y COMPROBAR QUE SEA UNA CRIA
        if(genes.values[0]){
            moveSpeed += 0.1f;
        }
        //Tal y como esta ahora el color con la edad, esto se sobreescribe
        //Prueba gen deseabilidad (solo para machos):
        if(genes.values[1] && genes.isMale){
            //print("Gen deseabilidad activado. I AM SEXY, BOY");
            material.color += new Color(material.color.r + 0.3f,0,0,0);
        }
        //Prueba gen tiempo embarazo:
        if(genes.values[2] && !genes.isMale){
            ratioTiempoEmbarazo = (fatherVals.genes != null && motherVals.genes != null)? 
                ((motherVals.rTiempoEmbarazo + fatherVals.rTiempoEmbarazo)/2 + 0.1f):(1.1f);
        }
        //Prueba gen rango vision:
        if(genes.values[3]){
            //print("Gen rango vision activado. I AM CATALEJO, BOY");
            maxViewDistance +=  2;
        }
        //Prueba gen curiosidad:
        /*if(genes.values[4] == 1){
            print("Gen velocidad activado. I AM SPEED, BOY");
        }*/
        //Prueba gen reproductive urge:
        if(genes.values[5]){
            ratioReproductiveUrge = (fatherVals.genes != null && motherVals.genes != null)? 
                ((motherVals.rReproductiveUrge + fatherVals.rReproductiveUrge)/2 + 0.1f) : 1.1f;
        }
        //Prueba gen crecer mas rapido:
        if(genes.values[6]){
            ratioCrecimiento = (fatherVals.genes != null && motherVals.genes != null)? 
                ((motherVals.rCrecimiento + fatherVals.rCrecimiento)/2 + 0.1f) : (ratioCrecimiento * 1.1f);
        }
        //Prueba gen sentimiento manada:
        /*if(genes.values[7] == 1){
            print("Gen velocidad activado. I AM SPEED, BOY");
        }*/
    }

    ///<summary>Aumentamos los sumatorios para las graficas de velocidad, radio vision...</summary>
    void AumentarSumatorios(){
        if(species == Species.Fox){
            env.radioVisionZorros+=maxViewDistance;
            env.velocidadZorros+=moveSpeed;
        }
        else{
            env.radioVisionConejos+=maxViewDistance;
            env.velocidadConejos+=moveSpeed;
        }
    }
    ///<summary>Si somos una cria, reducimos los parametros necesarios en un 30%</summary>
    void ComprobarCria(){
        if(cria){
            moveSpeed *= 0.7f;
            //print("Somos CRIAS, vision antes de cambio: " + maxViewDistance);
            //Por algun motivo sin el "+0.5" 10*0.7 devuelve 6. NOTA: Mirar porque pasa esto
            maxViewDistance =(int) (maxViewDistance * 0.7f +0.5);
            //print("Somos CRIAS, nueva vision: " + maxViewDistance);
        }
    }

    private int[] BitArrayToIntArray(BitArray lista){
        var aux = new int[lista.Count];
        for (int i = 0; i < lista.Count; i++) {
            aux[i] = lista[i]? 1:0;
        }
        return aux;
    }

    ///<summary>Inicializamos el color y los genes</summary>
    public override void Init (Coord coord) {
        env = (Environment) GameObject.Find("Environment").GetComponent("Environment");
        base.Init (coord);
        moveFromCoord = coord;

        //Si somos la primera generacion, tenemos genes random, sino los heredamos
        if(fatherVals.genes == null && motherVals.genes == null) {
            //print("Nueovs genes");
            genes = Genes.RandomGenes (7); val = BitArrayToIntArray(genes.values);   
            //print("Genes.values[1]: " + genes.values[1].ToString());
        }
        else {
            genes = Genes.InheritedGenes(motherVals.genes, fatherVals.genes); val = BitArrayToIntArray(genes.values);
            //Hemos nacido por reproduccion sexual, somos crias, nos movemos mas lento.
            /*print("Nacemos por reproduccion sexual, somos crias, VELOCIDAD PADRE: " + fatherVals.speed);
            print("Nacemos por reproduccion sexual, somos crias, VELOCIDAD MADRE: " + motherVals.speed);
            print("Nacemos por reproduccion sexual, somos crias, VELOCIDAD OPTIMA: " + (fatherVals.speed + motherVals.speed)/2);*/
            moveSpeed = (fatherVals.speed + motherVals.speed)/2 ;
            /*print("Nacemos por reproduccion sexual, somos crias, VISION PADRE: " + fatherVals.viewDistance);
            print("Nacemos por reproduccion sexual, somos crias, VISION MADRE: " + motherVals.viewDistance);
            print("Nacemos por reproduccion sexual, somos crias, VISION OPTIMA: " + (int) ((fatherVals.viewDistance + motherVals.viewDistance)/2));*/
            maxViewDistance = (int) ((fatherVals.viewDistance + motherVals.viewDistance)/2);
        }

        ComprobarGenes();

        AumentarSumatorios();

        ComprobarCria();

        ChooseHijo();
    }

    ///<summary>Si somos hijo y ya somos adultos (edad>0.15) crecemos a tamaño normal.
    ///Si no somos aun adulto, el reproductive urge es 0.
    ///NOTA: Mejorar, que vayan creciendo poco a poco.</summary>
    protected void Crecer(){
        //EL material del animal se hace mas blanco con la edad. NOTA: El material del zorro no cambia
        //var colorR = material.color.r;
        //material.color = (genes.isMale) ? (maleColour + new Color(edad+colorR,edad,edad,0)) : (femaleColour + new Color(edad,edad,edad,0));
        //Gen deseabilidad activado, tenemos en cuenta que el material es mas rojo
//        if(genes.values[1])
//            material.color = (genes.isMale) ? (maleColour + new Color(edad+0.3f,edad,edad,0)) : (femaleColour + new Color(edad,edad,edad,0));
//        else
//            material.color = (genes.isMale) ? (maleColour + new Color(edad,edad,edad,0)) : (femaleColour + new Color(edad,edad,edad,0));

        //Si somos crias
        if(transform.localScale.x == 0.5f){
            //Consideramos al conejo como adulto y lo escalamos a 1
            if(edad >= 0.15f){
                transform.localScale *=2;
                cria = false;
                print("Ya soy adulto, yeyyyy");
                moveSpeed *= 1.428571429f;
                //print("MaxviewDistance antes: " + maxViewDistance + " maxViewdistance despues de crecer: " + (int) (maxViewDistance * 1.428571429f));
                maxViewDistance =(int) (maxViewDistance * 1.428571429f);
            }
            else{
                reproductiveUrge = 0f;
            }
        }
    }

    //Funcion auxiliar para llamar en cada frame
    ///<summary>Si estamos embarazados empezamos a sumar el tiempo de embarazo y cuando llega a 1 spawneamos entre x e y hijos.
    ///NOTA: El tiempo de embarazo deberia de afectar a la edad de los hijos, cuanto mas tiempo, mas desarrollados nacen.</summary>
    public void comprobarEmbarazo(){
        if (embarazada) {
            tiempoParaParto += Time.deltaTime * 1/50 * ratioTiempoEmbarazo;
            //Hora de dar a luz
            if(tiempoParaParto >= 1f){
                moveSpeed *= 1.428571429f;
                tiempoParaParto = 0f;
                embarazada = false;
                motherVals = CrearPadres(this);
                foreach (var population in env.initialPopulations) {
                    if(population.prefab.species == species){
                        if(species == Species.Rabbit){
                            //Damos entre 4 y 13 hijos NOTA: Tendra que ir mas o menos en funcion del tiempo de embarazo
                            for (int i = 0; i < UnityEngine.Random.Range(4,13); i++){
                                var hijo = Environment.SpawnLivingEntitySR(population.prefab, coord, fatherVals, motherVals);
                                //Cuanto mas tiempo de embarazo mas desarrollados salen los hijos. NOTA: Esto es kk, habra que cambiarlo
                                ((Animal)hijo).edad += ratioTiempoEmbarazo==1f? 0.05f:0;
                            }
                            break;
                        }
                        else{
                            for (int i = 0; i < UnityEngine.Random.Range(4,6); i++){
                                var hijo = Environment.SpawnLivingEntitySR(population.prefab, coord, fatherVals, motherVals);
                                ((Animal)hijo).edad += ratioTiempoEmbarazo==1f? 0.05f:0;
                            }
                            break;
                        }
                    }
                }
            }
        }
    }

    //A esta funcion se le llama una vez por frame
    /*public virtual void Update () {
        // Increase hunger, thirst and age over time
        //Si somos mas rapidos pasaremos mas hambre y sed
        hunger += Time.deltaTime * 1 / timeToDeathByHunger * (moveSpeed/1.5f);
        thirst += Time.deltaTime * 1 / timeToDeathByThirst * (moveSpeed/1.5f);
        edad += Time.deltaTime * 1 / timeToDeathByAge * ratioCrecimiento;
        reproductiveUrge += Time.deltaTime * 1 / 350 * ratioReproductiveUrge;
        Crecer();
        comprobarEmbarazo();

        
        //material.color += new Color(material.color.r,edad,edad,0);

        // Animate movement. After moving a single tile, the animal will be able to choose its next action
        //puede que haya que cambiar esto para simular la velocidad¿?¿?¿?
        if (animatingMovement) {
            //print("-------------INICIO MOVIMIENTO---------------------");
            AnimateMove ();
        } else {
            // Handle interactions with external things, like food, water, mates
            HandleInteractions ();
            float timeSinceLastActionChoice = Time.time - lastActionChooseTime;
            //Elegimos la siguiente accion si ha pasado timeBetweenActionChoices segundos (1 segundo) desde la ultima accion
            if (timeSinceLastActionChoice > timeBetweenActionChoices) {
                ChooseNextAction ();
            }
        }
        if (hunger >= 1) {
            Die (CauseOfDeath.Hunger);
        } else if (thirst >= 1) {
            Die (CauseOfDeath.Thirst);
        } else if (edad >= 1) {
            Die (CauseOfDeath.Age);
        }
    }*/

    //NOTA: De momento es un sistema reactivo, seria interesante cambiarlo a BDI
    // Animals choose their next action after each movement step (1 tile),
    // or, when not moving (e.g interacting with food etc), at a fixed time interval
    /*protected virtual void ChooseNextAction () {
        lastActionChooseTime = Time.time;
        // Get info about surroundings

        // Decide next action:
        Coord coordDepredadorCercano = Environment.SenseDepredador(species, coord, maxViewDistance);
        //NOTA: Cambiar el species!=Species.Fox porque en el futuro puede que haya más animales. Cambiarlo a buscar en el diccionario de depredadores
        if (coordDepredadorCercano.x+coordDepredadorCercano.y != 0 && species != Species.Fox) {
            HuirDepredador(coordDepredadorCercano);
        }
        //NOTA: Repasar cuando elige que accion
        else {
            // Eat if (more hungry than thirsty) or (currently eating and not critically thirsty)
            bool currentlyEating = currentAction == CreatureAction.Eating && foodTarget && hunger > 0;
            bool wellFed = hunger < criticalPercent/1.5;
            bool wellThirst = thirst < criticalPercent/1.5;
            //Si estamos bien alimentados podemos buscar pareja
            if(wellFed && wellThirst && reproductiveUrge>0.3){
                FindMate();
            }
            //Si no estamos bien alimentados, vamos a buscar comida o agua
            else{
                if(hunger>thirst || currentlyEating){
                    FindFood();
                }
                else{
                    FindWater();
                }
            }*/
            //if (hunger >= thirst || currentlyEating && thirst < criticalPercent && !wellFed) {
            //Si no estamos bien alimentado y tenemos mas hambre que sed, comemos
            /*if ( (hunger>thirst && !wellFed) || currentlyEating) {
                FindFood ();
            }
            else{
                // Si no estamos bien alimentados, bebemos
                if(!wellThirst || reproductiveUrge < 0.6f) {
                    FindWater ();
                }
                //Si estamos bien alimentados y reproductive urge es muy alto, buscamos reproducirnos
                else{
                    FindMate();
                }
            }*/
        /*}
        /*if (reproductiveUrge > 0.4f && !embarazada) {
                FindMate();
        }
        Act ();
    }*/

    //Al hacer click en el objeto, hacemos que InformacionAnimal apunte a este animal
    /*public void OnMouseDown(){
        print("Click en mi");
        GameObject.Find("InformacionAnimal").GetComponent<InformacionAnimal>().SetAnimal(this);
    }*/

    //Ordena una lista de animales en funcion de quien esta mas cerca de la coordenada dada
    List<Animal> OrdenarListaAnimales(List<Animal> lista, Coord origen){
        var aux = lista;
        aux.Sort((a,b) => (Coord.Distance(a.coord,origen)).CompareTo(Coord.Distance(b.coord,origen)));
        return aux;
    }

    ///<summary>Encontramos las posibles parejas (que no esten en las listas de excluidos) y creamos camino al mas cercano</summary>
    protected void FindMate(){
        potentialMates = Environment.SensePotentialMates(coord, this);
        //Eliminamos los machos rechazados y hembras no impresionadas para no preguntarles todo el rato
        foreach (var machoRechazado in machosRechazados) {
            potentialMates.Remove(machoRechazado);
        }
        foreach (var hembrasNoImpresionada in hembrasNoImpresionadas) {
            potentialMates.Remove(hembrasNoImpresionada);
        }
        currentAction = CreatureAction.SearchingForMate;
        potentialMates = OrdenarListaAnimales(potentialMates, coord);
        if(potentialMates.Count > 0) {
            CreatePath(potentialMates[0].coord);
        }
    }

    //Funcion para encontrar comida
    /// <summary>
    /// El animal encuentra comida en funcion de su dieta, cambia su currentAction
    /// a GointToFood y crea un path para dirigirse a comer
    /// </summary>
    protected void FindFood () {
        //Encontramos la entidad que sea fuente de comida mas cercana
        LivingEntity foodSource = Environment.SenseFood (coord, this, FoodPreferencePenalty);
        if (foodSource) {
            currentAction = CreatureAction.GoingToFood;
            foodTarget = foodSource;
            CreatePath (foodTarget.coord);

        } else {
            currentAction = CreatureAction.Exploring;
        }
    }

    protected void FindWater () {
        //Encontramos la tile de agua mas cercana
        Coord waterTile = Environment.SenseWater (coord, maxViewDistance);
        //Si la coordenada es valida, vamos ahi
        if (waterTile != Coord.invalid) {
            currentAction = CreatureAction.GoingToWater;
            waterTarget = waterTile;
            CreatePath (waterTarget);
        } 
        //Sino, volvemos a explorar
        else {
            currentAction = CreatureAction.Exploring;
        }
    }

    // When choosing from multiple food sources, the one with the lowest penalty will be selected
    protected virtual int FoodPreferencePenalty (LivingEntity self, LivingEntity food) {
        //Tambien tenemos en cuenta que el animal sea lento. NOTA: En el futuro deberia de tener en cuenta el valor nutricional, la deseabilidad y asi tambien
        //PROBLEMA: Por algun motivo no deja la llamada (Animal)food. Sera por algo de copias de instancias¿?¿?¿?¿?
        //int velocidad = (int) (((Animal)food).moveSpeed / ((Animal) self).moveSpeed );
        return Coord.SqrDistance (self.coord, food.coord);
    }

    ///<summary>Dado un animal, devuelve la estructura Padres con los valores inicializados</summary>
    private Padres CrearPadres(Animal animal){
        return new Padres(animal.moveSpeed, animal.maxViewDistance, animal.ratioTiempoEmbarazo, animal.ratioCrecimiento, animal.ratioReproductiveUrge, animal.genes);
    }

    ///<summary>Funcion para embarazar al animal actual y guardar el padre.true Usar solo en hembras</summary>
    public void Embarazar(Animal padre){
        //Si somos hembra, nos embarazamos
        embarazada = true;
        moveSpeed *= 0.7f;
        fatherVals = CrearPadres(padre);
        //Nos hemos reproducido, vaciamos potentialMates y reseteamos reproductiveUrge
        reproductiveUrge = 0f;
        potentialMates.Clear();
        //print("Me han embarazado :) y soy un: " + species);
    }

    //NOTA: Falta controlar cuando nos han aceptado y cuando no para que no pidan reproducirse todo el rato al mismo
    ///<summary>Nos solicitan reproducirnos. Si somos machos aceptamos, sino ira en funcion de aleatoriedad y deseabilidad del macho</summary>
    public bool SolicitarMating(Animal posiblePareja){
        //Si somos machos devolvemos true porque siempre aceptamos la reproduccion
        if(genes.isMale){
            return true;
        }
        //Somos hembras, aceptamos o rechazamos en funcion de la deseabilidad
        else{
            var rndm = UnityEngine.Random.Range(0f, 1f);
            //if((posiblePareja.material.color.r-maleColour.r) > 0f){print("Diferencia color rojo: " + (posiblePareja.material.color.r-maleColour.r));}
            //Si random entre 0 y 1 + diferencia de color en rojo es mayor a 0.3, aceptamos reproducirnos
            if(rndm+(posiblePareja.material.color.r-maleColour.r) >= 0.3f){
                //print("Aceptamos la solicitus de mating");
                return true;
            }
            else{
                //print("Rechazamos la solicitud de mating");
                machosRechazados.Add(posiblePareja);
                potentialMates.Remove(posiblePareja);
                return false;
            }
        }
    }

    //Controla y ejecuta la accion que desea hacer el agente
    protected void Act () {
        switch (currentAction) {
            case CreatureAction.Exploring:
                StartMoveToCoord (Environment.GetNextTileWeighted (coord, moveFromCoord));
                break;
            case CreatureAction.GoingToFood:
                if (Coord.AreNeighbours (coord, foodTarget.coord)) {
                    LookAt (foodTarget.coord);
                    currentAction = CreatureAction.Eating;
                } else {
                    StartMoveToCoord (path[pathIndex]);
                    pathIndex++;
                }
                break;
            case CreatureAction.GoingToWater:
                if (Coord.AreNeighbours (coord, waterTarget)) {
                    LookAt (waterTarget);
                    currentAction = CreatureAction.Drinking;
                } else {
                    StartMoveToCoord (path[pathIndex]);
                    pathIndex++;
                }
                break;
            case CreatureAction.Fleeing:
                print("Estamos huyendo");
                //Solo los conejos huyen, asi que nunca deberia de petar porque aqui no entraran los zorros
                LookAt(base.coord + (base.coord - ((Rabbit)this).depredadorMasCercano));
                //StartMoveToCoord(base.coord - (base.coord - depredadorMasCercano));
                //NOTA: Da error cuando se acerca al agua o al final del mapa, es posible que haya que mejorar el pathfinder
                if(path == null){
                    //Debug.Log("No hay escapatoria :(");
                }
                else{
                StartMoveToCoord(path[pathIndex]);
                pathIndex++;
                }
                //print("Estoy huyendo");
            break;
            case CreatureAction.SearchingForMate:
                if (potentialMates.Count > 0) {
                    //Si estamos junto al potentialMate
                    if (Coord.AreNeighbours (coord, potentialMates[0].coord)) {
                        //Si somos macho, solicitamos la reproduccion que sera en funcion de la deseabilidad
                        //NOTA: Solo importa el SolicitarMating del macho, el de la hembra siempre se acepta porque los machos siempre aceptan
                        if(genes.isMale && potentialMates[0].SolicitarMating(this)) {
                            LookAt(potentialMates[0].coord);
                            //currentAction = CreatureAction.Mating;
                            //Si la hembra acepta la reproduccion, la embarazamos
                            potentialMates[0].Embarazar(this);
                            //Nos hemos reproducido, ya no tenemos reproductiveUrge y vaciamos la lista de potentialMates
                            reproductiveUrge = 0f;
                            potentialMates.Clear();
                        }
                        //Nos han rechazado. La añadimos en hembrasNoImpresionadas y eliminamos de la lista a la hembra 
                        else {
                            hembrasNoImpresionadas.Add(potentialMates[0]);
                            potentialMates.Remove(potentialMates[0]);
                        }
                    }
                    else {
                        LookAt(potentialMates[0].coord);
                        StartMoveToCoord(path[pathIndex]);
                        pathIndex++;
                    }
                }
                else {
                    StartMoveToCoord (Environment.GetNextTileWeighted (coord, moveFromCoord));
                }
                break;
        }
    }

    //Camino mas cercano al target
    protected void CreatePath (Coord target) {
        //NOTA: Cuidado, cuando pathIndex vale 0 da error en el if en path[pathIndex - 1] porque esta pidiendo path[-1]
        //por eso esta el primer if pero seguro que hay una manera mas elegante de hacerlo
        // Create new path if current is not already going to target
        if(pathIndex > 0) {
            if (path == null || pathIndex >= path.Length || ( path[path.Length - 1] != target || path[pathIndex - 1] != moveTargetCoord )) {
            //if (path == null || pathIndex >= path.Length) {
                //NOTA: Falla al buscar path en coordenadas no validas (agua o fin del mapa)
                //puede que haya que mejorar el PathFinder
                path = EnvironmentUtility.GetPath (coord.x, coord.y, target.x, target.y);
                pathIndex = 0;
                //if(currentAction == CreatureAction.Fleeing){
                //print("Path creado para huir");}
            }
        }
        else{
            path = EnvironmentUtility.GetPath(coord.x, coord.y, target.x, target.y);
            pathIndex = 0;
        }
    }

    /// <summary>Dadas las coordenadas objetivo, empezamos a movernos ahi</summary>
    protected void StartMoveToCoord (Coord target) {
        moveFromCoord = coord;
        moveTargetCoord = target;
        moveStartPos = transform.position;
        moveTargetPos = Environment.tileCentres[moveTargetCoord.x, moveTargetCoord.y];
        animatingMovement = true;

        bool diagonalMove = Coord.SqrDistance (moveFromCoord, moveTargetCoord) > 1;
        moveArcHeightFactor = (diagonalMove) ? sqrtTwo : 1;
        moveSpeedFactor = (diagonalMove) ? oneOverSqrtTwo : 1;

        LookAt (moveTargetCoord);
    }

    //Miramos en direccion al target
    protected void LookAt (Coord target) {
        //Si no estamos ya en el target
        if (target != coord) {
            Coord offset = target - coord;
            transform.eulerAngles = Vector3.up * Mathf.Atan2 (offset.x, offset.y) * Mathf.Rad2Deg;
        }
    }

    ///<summary>Administra las interacciones entre criaturas. Conejo-hierba y zorro-conejo</summary>
    protected void HandleInteractions () {
        if (currentAction == CreatureAction.Eating) {
            if (foodTarget && hunger > 0) {
                //NOTA: Cambiar porque ahora solo funciona porque esta hecho a mano
                //Nos comemos un conejo
                if((foodTarget.species).ToString() ==  "Rabbit"){
                    //print("Valor nutricional:" + ((Rabbit) ((Animal) foodTarget) ).valorNutricional);
                    //hunger -= ((Rabbit) ((Animal) foodTarget) ).valorNutricional;
                    hunger -= ((Rabbit) ((Animal) foodTarget) ).valorNutricional;
                    if(hunger < 0 ){
                        hunger = 0;
                    }
                    foodTarget.Die(CauseOfDeath.Eaten);
                }
                //Comemos hierba
                else{
                    //Minimo entre hunger y 0.1
                    float eatAmount = Mathf.Min (hunger, Time.deltaTime * 1 / eatDuration);
                    eatAmount = ((Plant) foodTarget).Consume (eatAmount);
                    hunger -= eatAmount;
                }
            }
        } else if (currentAction == CreatureAction.Drinking) {
            if (thirst > 0) {
                thirst -= Time.deltaTime * 1 / drinkDuration;
                thirst = Mathf.Clamp01 (thirst);
            }
        }
    }

    ///<summary>Animamos el movimiento de un salto y cuando terminamos elegimos la proxima accion.</summary>
    protected void AnimateMove () {
        //Si queremos que pueda elegir antes la accion, (moveTime +...) tendra que crecer mas rapido ya sea aumentando moveSpeed o moveSpeedFactor
        // Move in an arc from start to end tile
        moveTime = Mathf.Min (1, moveTime + Time.deltaTime * moveSpeed * moveSpeedFactor);
        //Altura del salto para moverse entre tiles
        float height = (1 - 4 * (moveTime - .5f) * (moveTime - .5f)) * moveArcHeight * moveArcHeightFactor;
        transform.position = Vector3.Lerp (moveStartPos, moveTargetPos, moveTime) + Vector3.up * height;

        // Finished moving
        if (moveTime >= 1) {
            Environment.RegisterMove (this, coord, moveTargetCoord);
            coord = moveTargetCoord;

            animatingMovement = false;
            moveTime = 0;

            ChooseHijo();
        }
    }
    //Funcion que en Fox y Rabbit va a ser sobreescrita para que llame a ChooseNextAction. Asi lo podemos llamar desde aqui
    protected virtual void ChooseHijo(){
        //print("Llamamos a pruebaChoose desde Animal");
    }

    ///<summary>Datos relevantes de los padres como su velocidad y genes para que puedan heredar los hijos</summary>
    public struct Padres {
        public float speed;
        public int viewDistance;
        public float rTiempoEmbarazo;
        public float rCrecimiento;
        public float rReproductiveUrge;
        public Genes genes;
        public Padres(float s, int vd, float re, float rc, float rr, Genes g){
            this.speed = s;
            this.viewDistance = vd;
            this.rTiempoEmbarazo = re;
            this.rCrecimiento = rc;
            this.rReproductiveUrge = rr;
            this.genes = g;
        }
    }

}