using DragonGate;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.AddressableAssets;

public class PoolScopeDemoController : MonoBehaviour
{
    [SerializeField] private GameObject _particlePrefab;
    [SerializeField] private Transform _particlePosition;
    [Space]
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private float _fireInterval = 0.1f;
    [Space]
    [SerializeField] private Transform _ballPosition;
    
    private float _elapsedFire = 0;
    private float _previousAngle;
    private float _fireCooldown = 2f;
    private float _cooldownElapsed;
    private bool _isCooldown = false;
    
    private float _elapsedParticle = 0;

    private float _elapsedBall = float.MaxValue;
    private float _randomIntervalBall;

    private float _elapsedMakeClass = 0;
    private float _elapsedReturnClass = 0;
    private float _randomIntervalClass = 1;
    private List<EmptyClass> _classes = new();

    private class EmptyClass { }
    
    private const string DemoBulletResourcePath = "PoolScopeDemoBullet";
    private const string DemoBallResourcePath = "PoolScopeDemoBall";
    
    private PoolHandle<PoolScopeDemoBullet> _bulletPool;
    private GameObjectPoolHandle _particlePool;
    private PoolHandle<PoolScopeDemoBall> _ballPool;
    private ClassPoolHandle<EmptyClass> _classPool;

    private void Awake()
    {
        _bulletPool = PoolScope.CreatePool<PoolScopeDemoBullet>(PoolScopeLoader.FromResources(DemoBulletResourcePath));
        _particlePool = PoolScope.CreateObjectPool(PoolScopeLoader.FromPrefab(_particlePrefab));
        _ballPool = PoolScope.CreatePool<PoolScopeDemoBall>(PoolScopeLoader.FromResources(DemoBallResourcePath));
        _classPool = PoolScope.CreateClassPool<EmptyClass>();
        // StartCoroutine(LoadCustom());
    }

    // ex) Addressable Async Custom Load
    // private IEnumerator LoadCustom()
    // {
    //     var handle = Addressables.LoadAssetAsync<GameObject>("Bullet");
    //     yield return handle;
    //     var loader = PoolScopeLoader.FromFunc(() => UnityEngine.Object.Instantiate(handle.Result));
    //     _bulletPool = PoolScope.CreatePool<PoolScopeDemoBullet>(loader);
    // } 

    private void Update()
    {
        var moveVector = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
        {
            moveVector.z = 1;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            moveVector.z = -1;
        }

        if (Input.GetKey(KeyCode.A))
        {
            moveVector.x = -1;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            moveVector.x = 1;
        }
        _characterController.Move(moveVector * Time.deltaTime);

        Fire();
        Particle();
        Ball();
        MakeClass();
        ReturnClass();
    }

    private void Fire()
    {
        if (_bulletPool == null) return;
        if (_isCooldown)
        {
            _cooldownElapsed += Time.deltaTime;
            if (_cooldownElapsed < _fireCooldown)
            {
                return;
            }
            _isCooldown = false;
        }
        _elapsedFire += Time.deltaTime;
        if (_elapsedFire >= _fireInterval)
        {
            _elapsedFire -= _fireInterval;
            var bullet = _bulletPool.Get(lifetime: 1.2f);
            bullet.transform.position = _characterController.transform.position;
            bullet.transform.rotation = Quaternion.Euler(0, _previousAngle, 0);
            _previousAngle += 5;
            if (_previousAngle >= 360)
            {
                _previousAngle -= 360;
                _isCooldown = true;
                _cooldownElapsed = 0;
            }
        }
    }

    private void Particle()
    {
        _elapsedParticle += Time.deltaTime;
        if (_elapsedParticle >= 1f)
        {
            _elapsedParticle -= 1f;
            var particle = _particlePool.GetObject();
            particle.transform.position = _particlePosition.position;
        }
    }

    private void Ball()
    {
        _elapsedBall += Time.deltaTime;
        if (_elapsedBall >= _randomIntervalBall)
        {
            _elapsedBall = 0;
            _randomIntervalBall = Random.Range(1f, 2f);
            var ball = _ballPool.Get();
            ball.transform.position = _ballPosition.position + Random.insideUnitSphere;
        }
    }

    private void MakeClass()
    {
        _elapsedMakeClass += Time.deltaTime;
        if (_elapsedMakeClass >= 0.2f)
        {
            _elapsedMakeClass -= 0.2f;
            var newClass = _classPool.Get();
            _classes.Add(newClass);
        }
    }

    private void ReturnClass()
    {
        _elapsedReturnClass += Time.deltaTime;
        if (_elapsedReturnClass >= _randomIntervalClass)
        {
            _randomIntervalClass = Random.Range(.1f, .3f);
            _elapsedReturnClass = 0;
            
            if (_classes.Count != 0)
            {
                var emptyClass = _classes[0];
                _classes.RemoveAt(0);
                _classPool.Return(emptyClass);
            }
        }
    }
}
