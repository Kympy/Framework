using DragonGate;
using UnityEngine;

public class PoolScopeDemoBullet : MonoBehaviour, IPoolable
{
    private void Update()
    {
        transform.position += transform.forward * Time.deltaTime * 15f; 
    }

    public void OnGet()
    {
        
    }

    public void OnReturn()
    {
        
    }
}
