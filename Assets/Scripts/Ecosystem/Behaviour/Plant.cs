using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plant : LivingEntity {
    public float amountRemaining = 1;
    const float consumeSpeed = 8;

    void Update() {
        //La planta se restaura poco a poco
        if(amountRemaining<1 && transform.localScale.x < 1){
            amountRemaining += Time.deltaTime * 1/50;
            transform.localScale = Vector3.one * amountRemaining;
        }
    }

    public float Consume (float amount) {
        float amountConsumed = Mathf.Max (0, Mathf.Min (amountRemaining, amount));
        amountRemaining -= amount * consumeSpeed;

        transform.localScale = Vector3.one * amountRemaining;

        if (amountRemaining <= 0) {
            Die (CauseOfDeath.Eaten);
        }

        return amountConsumed;
    }

    public float AmountRemaining {
        get {
            return amountRemaining;
        }
    }
}