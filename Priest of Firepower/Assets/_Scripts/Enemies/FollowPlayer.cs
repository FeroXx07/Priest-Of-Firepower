using UnityEngine;
using UnityEngine.AI;

namespace _Scripts.Enemies
{
    public class FollowPlayer : MonoBehaviour
    {
        [SerializeField] private Transform target;
        NavMeshAgent _agent;

        // Start is called before the first frame update
        void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
        }

        // Update is called once per frame
        void Update()
        {
            _agent.SetDestination(target.position);
        }
    }
}
