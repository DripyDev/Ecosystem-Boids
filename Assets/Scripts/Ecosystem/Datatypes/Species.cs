//Solo hay 4 posibles especies, indefinido, conejo, planta y zorro
public enum Species {
    //"<<" es usado para sacar una mascara de bits para definir los valores
    Undefined = (1 << 0),
    Plant = (1 << 1),
    Rabbit = (1 << 2),
    Fox = (1 << 3)
}