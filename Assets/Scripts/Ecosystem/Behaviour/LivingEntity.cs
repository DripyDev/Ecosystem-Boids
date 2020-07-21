using UnityEngine;

public class LivingEntity : MonoBehaviour {

    //Color del material, supongo que se usara para la deseabilidad de los conejos
    //NO SE USA NUNCA
    public int colourMaterialIndex;
    //Especie a la que pertenece
    public Species species;
    public Material material;
    //Coordenadas de la entidad
    public Vector3Int coord;
    //Para no mostrar Coord en el inspector
    //[HideInInspector]
    ///<summary>Indice de la entidad en la lista de entidades de su region del mapa</summary>
    public int mapIndex;
    //[HideInInspector]
    ///<summary>Son las coordenadas de la region del mapa en la que esta el ente</summary>
    public Vector2Int mapCoord;

    //Booleano que indica si la entidad esta viva o muerta
    protected bool dead;
    public bool cria;


    ///<summary>Inicializacion de la entidad en el mundo. guardamos su posicion y cargamos su material</summary>
    public virtual void Init (Vector3Int coord) {
        this.coord = coord;
        transform.position = Mundo.centros[coord.x, coord.z];

        // Set material to the instance material
        var meshRenderer = transform.GetComponentInChildren<MeshRenderer> ();
        for (int i = 0; i < meshRenderer.sharedMaterials.Length; i++)
        {
            if (meshRenderer.sharedMaterials[i] == material) {
                material = meshRenderer.materials[i];
                break;
            }
        }
    }

    ///<summary>Funcion para matar a la entidad. Registramos su muerte en el mapa de environment
    ///NOTA: Habra que cambiarla para que registre tambien las causas de las muertes.</summary>
    public virtual void Die (CauseOfDeath cause) {
        if (!dead) {
            dead = true;
            //Restamos la velocidad del sumatorio de velocidades
            if( species == Species.Fox){
                //Si eramos niños o estabamos embarazados, restamos la velocidad que hubieramos tenido siendo adultos o sin estar embarazado
                //((Animal)this).env.velocidadZorros -= (((Animal)this).moveSpeed<1.5)? ((Animal)this).moveSpeed*1.428571429f : ((Animal)this).moveSpeed;
                bool embOcria = ((Animal)this).embarazada || this.cria;
                if(embOcria){
                    ((Animal)this).env.velocidadZorros -= ((Animal)this).moveSpeed*1.428571429f;
                    ((Animal)this).env.radioVisionZorros -= (int) (((Animal)this).maxViewDistance *1.428571429f);
                }
                else{
                    ((Animal)this).env.velocidadZorros -= ((Animal)this).moveSpeed;
                    ((Animal)this).env.radioVisionZorros -= ((Animal)this).maxViewDistance;
                }
            }
            if( species == Species.Rabbit){
                bool embOcria = ((Animal)this).embarazada || this.cria;
                if(embOcria){
                    ((Animal)this).env.velocidadConejos -= ((Animal)this).moveSpeed*1.428571429f;
                    ((Animal)this).env.radioVisionConejos -= (int) (((Animal)this).maxViewDistance *1.428571429f);
                }
                else{
                    ((Animal)this).env.velocidadConejos -= ((Animal)this).moveSpeed;
                    ((Animal)this).env.radioVisionConejos -= ((Animal)this).maxViewDistance;
                }
                
                //((Animal)this).env.velocidadConejos -= (((Animal)this).moveSpeed<1.5)? ((Animal)this).moveSpeed*1.428571429f : ((Animal)this).moveSpeed;
                //Comprobamos que no seamos crias o que estemos embarazados
                if( embOcria )
                    print("restamos en velocidadConejos: " + ((Animal)this).moveSpeed*1.428571429f);
                else
                    print("restamos en velocidadConejos: " + ((Animal)this).moveSpeed);
            }
            
            //Registramos la muerte
            Mundo.RegistrarMuerte(this, cause);
            Destroy (gameObject);
        }
    }
}