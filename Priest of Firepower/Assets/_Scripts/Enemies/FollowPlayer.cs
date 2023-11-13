using UnityEngine;
using UnityEngine.AI;

namespace _Scripts.Enemies
{
    public class FollowPlayer : MonoBehaviour
    {
        [SerializeField] 
        Transform target;


        NavMeshAgent agent;

        // Start is called before the first frame update
        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        // Update is called once per frame
        void Update()
        {
            agent.SetDestination(target.position);
        }
    }
}
