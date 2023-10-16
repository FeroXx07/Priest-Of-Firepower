using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyData : MonoBehaviour
{
    public int currentLife = 1;    
    public int totalLife = 10;

    public int damage = 5;

    public float attackCooldown = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        currentLife = totalLife;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
