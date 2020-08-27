using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Animal : LivingEntity {
    public Mundo.Nodo[,] mapaNodos;
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
    //public Environment env;
    public Mundo env;

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
    protected float criticalPercent = 0.6f;

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
    protected Vector3Int waterTarget;

    // Move data:
    protected bool animatingMovement;
    public Vector3Int moveFromCoord;
    Vector3Int moveTargetCoord;
    Vector3 moveStartPos;
    Vector3 moveTargetPos;
    float moveTime;
    float moveSpeedFactor;
    float moveArcHeightFactor;
    //Array de coordenadas que es el camino que va a coger el agente
    public Vector3Int[] path;
    public int pathIndex;

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
    public override void Init (Vector3Int coord) {
        //env = (Environment) GameObject.Find("Environment").GetComponent("Environment");
        env = (Mundo) GameObject.Find("Environment").GetComponent("Mundo");
        base.Init (coord);
        moveFromCoord = coord;
        pathIndex = 0;

        mapaNodos = Mundo.mapaNodos;

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
        if(cria){
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
                foreach (var population in env.poblacionInicial) {
                    if(population.prefab.species == species){
                        if(species == Species.Rabbit){
                            //Damos entre 4 y 13 hijos NOTA: Tendra que ir mas o menos en funcion del tiempo de embarazo
                            for (int i = 0; i < UnityEngine.Random.Range(4,13); i++){
                                var hijo = Mundo.SpawnLivingEntitySR(population.prefab, coord, fatherVals, motherVals);
                                //Cuanto mas tiempo de embarazo mas desarrollados salen los hijos. NOTA: Esto es kk, habra que cambiarlo
                                ((Animal)hijo).edad += ratioTiempoEmbarazo==1f? 0.05f:0;
                            }
                            break;
                        }
                        else{
                            for (int i = 0; i < UnityEngine.Random.Range(4,6); i++){
                                var hijo = Mundo.SpawnLivingEntitySR(population.prefab, coord, fatherVals, motherVals);
                                ((Animal)hijo).edad += ratioTiempoEmbarazo==1f? 0.05f:0;
                            }
                            break;
                        }
                    }
                }
            }
        }
    }

    //Ordena una lista de animales en funcion de quien esta mas cerca de la coordenada dada
    List<Animal> OrdenarListaAnimales(List<Animal> lista, Vector3Int origen){
        var aux = lista;
        aux.Sort((a,b) => (Vector3Int.Distance(a.coord,origen)).CompareTo(Vector3Int.Distance(b.coord,origen)));
        return aux;
    }

    private List<Animal> ListaLivingEntityAListaAnimal(List<LivingEntity> lista){
        var listaAnimales = new List<Animal>();
        foreach (var ser in lista){
            listaAnimales.Add((Animal) ser);
        }
        return listaAnimales;
    }

    ///<summary>Encontramos las posibles parejas (que no esten en las listas de excluidos) y creamos camino al mas cercano</summary>
    protected void FindMate(){
        potentialMates = ListaLivingEntityAListaAnimal(Mundo.SentirPosiblesParejas(this, coord, maxViewDistance));
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
            //if(EnvironmentUtility.TileIsVisibile(coord.x, coord.z, potentialMates[0].coord.x, potentialMates[0].coord.z))
            //if(Pathfinder.SeccionVisible(coord.x, coord.z, potentialMates[0].coord.x, potentialMates[0].coord.z))
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
        LivingEntity foodSource = Mundo.SentirComida(this, coord, maxViewDistance);
        if (foodSource) {
            CreatePath(foodSource.coord);
            if(path != null && path.Length > 0){
                currentAction = CreatureAction.GoingToFood;
                foodTarget = foodSource;
            }
            else{
                currentAction = CreatureAction.Exploring;
            }
        } 
        else {
            currentAction = CreatureAction.Exploring;
        }
    }

    protected void FindWater () {
        Vector3Int agua = Mundo.SentirAgua(coord, maxViewDistance);
        if(agua!=Mundo.invalid && Vector3.Distance(agua, coord) <= maxViewDistance){
            CreatePath(agua);
            if(path!=null && path.Length > 0){
                currentAction = CreatureAction.GoingToWater;
                waterTarget = agua;
            }
            //No somos capaces de llegar al agua (NOTA: SI USAMOS A* CASI NUNCA VA A PASAR ESTO)
            else{
                currentAction = CreatureAction.Exploring;
            }
        }
        else{
            currentAction = CreatureAction.Exploring;
        }
    }

    // When choosing from multiple food sources, the one with the lowest penalty will be selected
    protected virtual int FoodPreferencePenalty (LivingEntity self, LivingEntity food) {
        //Tambien tenemos en cuenta que el animal sea lento. NOTA: En el futuro deberia de tener en cuenta el valor nutricional, la deseabilidad y asi tambien
        //PROBLEMA: Por algun motivo no deja la llamada (Animal)food. Sera por algo de copias de instancias¿?¿?¿?¿?
        //int velocidad = (int) (((Animal)food).moveSpeed / ((Animal) self).moveSpeed );
        return (int)Vector3Int.Distance(self.coord, food.coord);
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

    //PERO VAMOS A VER, TONTOSHEISSE. (10,2) Y (11,2) ESTAN A DISTANCIA 1 Y SIN EMBARGO SON VECINOS. RAIZ DE 2 ES SI ES UN VECINO DIAGONAL
    protected bool CoordenadasVecinas(Vector3Int a, Vector3Int b){
        //NOTA: SI SON VECINOS LA DISTANCIA DE LAS POSICIONES NO ES 1, ES RAIZ DE 2, TONTO DEL CULO
        //Evitamos usar la raiz para que sea mas eficaz. La y es diferente ahora pero nos da igual asi que... xD
        return Mathf.Pow((a.x - b.x), 2f) + Mathf.Pow((a.z - b.z), 2f) < 2;
    }

    //Controla y ejecuta la accion que desea hacer el agente
    protected void Act () {
        switch (currentAction) {
            case CreatureAction.Exploring:
                StartMoveToCoord(Mundo.TileTendencia(coord, moveFromCoord, 0.4f));
                break;
            case CreatureAction.GoingToFood:
                if (CoordenadasVecinas(coord, foodTarget.coord)) {
                    LookAt (foodTarget.coord);
                    currentAction = CreatureAction.Eating;
                }else if(path!=null){
                    if(path.Length > pathIndex){
                        StartMoveToCoord(path[pathIndex]);
                        pathIndex++;
                    }
                }
                break;
            case CreatureAction.GoingToWater:
                if (CoordenadasVecinas(coord, waterTarget)) {
                    LookAt (waterTarget);
                    currentAction = CreatureAction.Drinking;
                }else if(path!=null){
                    if(path.Length > pathIndex){
                        StartMoveToCoord (path[pathIndex]);
                        pathIndex++;
                }
                }
                break;
            case CreatureAction.Fleeing:
                //Solo los conejos huyen, asi que nunca deberia de petar porque aqui no entraran los zorros
                LookAt(base.coord + (base.coord - ((Rabbit)this).depredadorMasCercano));
                //StartMoveToCoord(base.coord - (base.coord - depredadorMasCercano));
                //NOTA: Da error cuando se acerca al agua o al final del mapa, es posible que haya que mejorar el pathfinder
                if(path == null){
                    //Debug.Log("No hay escapatoria :(");
                }
                else{
                    //NOTA: ESTO ES KK ES UNA COMPROBACION QUE NO DEBERIA DE HACER FALTA
                    if(path.Length > pathIndex){
                        StartMoveToCoord(path[pathIndex]);
                        pathIndex++;
                    }
                }
                //print("Estoy huyendo");
            break;
            case CreatureAction.SearchingForMate:
                if (potentialMates.Count > 0) {
                    //Si estamos junto al potentialMate
                    if (CoordenadasVecinas(coord, potentialMates[0].coord)) {
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
                        if(path != null && pathIndex < path.Length){
                            LookAt(potentialMates[0].coord);
                            StartMoveToCoord(path[pathIndex]);
                            pathIndex++;
                        }
                    }
                }
                else {
                    StartMoveToCoord(Mundo.TileTendencia(coord, moveFromCoord, 0.4f));
                }
                break;
        }
    }

    //Dado un vector3Int. Si sus componentes estan fuera del mapa del mundo, devuelve con los componentes en los maximos del mapa
    private Vector3Int ReducirDimensionesOverflow(Vector3Int target){
        //print("Direccion original: " + target);
        var aux = target;
        if(target.x >= Mundo.centros.GetLength(0))
            aux.x = Mundo.centros.GetLength(0)-1;
        if(target.x < 0)
            aux.x = 0;
        if(target.z >= Mundo.centros.GetLength(1))
            aux.z = Mundo.centros.GetLength(1)-1;
        if(target.z < 0)
            aux.z = 0;
        //print("Direccion despues de procesar el overflow: " + aux);
        return aux;
    }

    //Camino mas cercano al target
    protected void CreatePath(Vector3Int target) {
        //Si target es fuera del mapa, devolvemos el limite del mapa
        target = ReducirDimensionesOverflow(target);

        //NOTA: Cuidado, cuando pathIndex vale 0 da error en el if en path[pathIndex - 1] porque esta pidiendo path[-1]
        //por eso esta el primer if pero seguro que hay una manera mas elegante de hacerlo
        // Create new path if current is not already going to target
        if(pathIndex > 0) {
            if (path == null || pathIndex >= path.Length || ( path[path.Length - 1] != target || path[pathIndex - 1] != moveTargetCoord )) {
            //if (path == null || pathIndex >= path.Length) {
                //NOTA: Falla al buscar path en coordenadas no validas (agua o fin del mapa)
                path = Pathfinder.BresenhamError(coord.x, coord.z, target.x, target.z);
                //path = ListaCoordAListaVector3(EnvironmentUtility.GetPath(coord.x, coord.z, target.x, target.z));

                if(path == null){
                    //Mundo.ResetMapaNodos(mapaNodos);//Reseteamos el mapa por si un A* anterior lo ha modificado
                    //print("Llamamos a A*, a ver que pasa xD");
                    //path = Pathfinder.AStar(coord.x, coord.z, target.x, target.z, mapaNodos);
                }
                pathIndex = 0;
            }
        }
        else{
            path = Pathfinder.BresenhamError(coord.x, coord.z, target.x, target.z);
            //path = ListaCoordAListaVector3(EnvironmentUtility.GetPath(coord.x, coord.z, target.x, target.z));

            if(path == null){
                //Mundo.ResetMapaNodos(mapaNodos);//Reseteamos el mapa por si un A* anterior lo ha modificado
                //print("Llamamos a A*, a ver que pasa xD");
                //path = Pathfinder.AStar(coord.x, coord.z, target.x, target.z, mapaNodos);
            }

            pathIndex = 0;
        }
    }

    private Vector3Int[] ListaCoordAListaVector3(Coord[] lista){
        if(lista != null){
            Vector3Int[] res = new Vector3Int[lista.Length];
            for (int i = 0; i < lista.Length; i++){
                res[i] = new Vector3Int(lista[i].x, 0, lista[i].y);
            }
            return res;
        }
        else{
            return null;
        }
    }

    /// <summary>Dadas las coordenadas objetivo, empezamos a movernos ahi</summary>
    protected void StartMoveToCoord (Vector3Int target) {
        moveFromCoord = coord;
        moveTargetCoord = target;
        moveStartPos = transform.position;
        moveTargetPos = Mundo.centros[moveTargetCoord.x, moveTargetCoord.z];
        animatingMovement = true;

        //bool diagonalMove = Coord.SqrDistance(moveFromCoord, moveTargetCoord)) > 1;
        bool diagonalMove = Mathf.Sqrt(Vector3Int.Distance(moveFromCoord, moveTargetCoord)) > 1;
        moveArcHeightFactor = (diagonalMove) ? sqrtTwo : 1;
        moveSpeedFactor = (diagonalMove) ? oneOverSqrtTwo : 1;

        LookAt (moveTargetCoord);
    }

    //Miramos en direccion al target
    protected void LookAt (Vector3Int target) {
        //Si no estamos ya en el target
        if (target != coord) {
            Vector3Int offset = target - coord;
            transform.eulerAngles = Vector3.up * Mathf.Atan2 (offset.x, offset.z) * Mathf.Rad2Deg;
        }
    }

    ///<summary>Administra las interacciones entre criaturas. Conejo-hierba y zorro-conejo</summary>
    protected void HandleInteractions () {
        if (currentAction == CreatureAction.Eating) {
            if (foodTarget && hunger > 0) {
                //NOTA: Cambiar porque ahora solo funciona porque esta hecho a mano
                //Nos comemos un conejo
                if( foodTarget.species ==  Species.Rabbit){
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
        }
        else if (currentAction == CreatureAction.Drinking) {
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
            Mundo.RegistrarMovimiento(this, coord, moveTargetCoord);
            //Actualizamos moveFromCoord que es la ultima tile en la que hemos estado
            moveFromCoord = coord;
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