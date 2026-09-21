using UnityEngine;


public class Test1246 : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       GameEvents.TriggerOnEnemyKilled();
        GameEvents.TriggerOnGoldObtained(100);
        GameEvents.TriggerOnStageCleared();
        GameEvents.TriggerOnPlayTime(10);
    }
    // Update is called once per frame
    void Update()
    {
      
    }
}
