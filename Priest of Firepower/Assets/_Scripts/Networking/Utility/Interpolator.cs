using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Networking.Utility
{
    public class Interpolator : MonoBehaviour
    {
        [SerializeField] private float timeElapsed = 0f;
        [SerializeField] private float timeToReachTarget = 0.05f;
        [SerializeField] private float movementThreshold = 0.05f;

        //sotre all transform updates
        private readonly List<TransformUpdate> futureTransformUpdates = new List<TransformUpdate>();
        private float squareMovementThreshold;

        //movement references
        private TransformUpdate to;
        private TransformUpdate from;
        private TransformUpdate previous;

        private void Start()
        {
            //precalculate the square movement threshold
            squareMovementThreshold = movementThreshold * movementThreshold;

            // initilaize the refrences with the current object state
            to = new TransformUpdate(NetworkManager.Instance.GetClient().ServerTick, false, transform.position, transform.rotation.z);
            from = new TransformUpdate(NetworkManager.Instance.GetClient().InterpolationTick, false, transform.position, transform.rotation.z);
            previous = new TransformUpdate(NetworkManager.Instance.GetClient().InterpolationTick, false, transform.position, transform.rotation.z);
        }

        private void Update()
        {

            if(futureTransformUpdates.Count>0)
            {
                Debug.Log($"New transform count {futureTransformUpdates.Count} ");
            }
      
            for (int i = 0;i < futureTransformUpdates.Count; i++)
            {
                if(NetworkManager.Instance.GetClient().ServerTick >= futureTransformUpdates[i].Tick)
                {
                    if (futureTransformUpdates[i].IsTeleport)
                    {
                        //if tp is needed then set all the references to the new position
                        to = futureTransformUpdates[i];
                        from = to;
                        previous = to;
                        transform.position = to.Position;
                        transform.rotation = Quaternion.Euler(new Vector3(0,0,to.Rotation));
                    }
                    else
                    {
                        //set the new transform update to interpolate with
                        previous = to;
                        to = futureTransformUpdates[i];
                        from = new TransformUpdate(NetworkManager.Instance.GetClient().InterpolationTick, false, transform.position, transform.rotation.z);
                    }
                    //remove used transform update, decrease i count to not exceed for loop 
                    futureTransformUpdates.RemoveAt(i);
                    i--;
                    timeElapsed = 0f;
                    timeToReachTarget = (to.Tick - from.Tick) * Time.fixedDeltaTime;

                    Debug.Log($"Time to reach target {timeToReachTarget}, target{to.Position}");
                }
            }
            timeElapsed += Time.deltaTime;
            InterpolatePosition(timeElapsed/timeToReachTarget);
        }

        private void  InterpolatePosition(float lerpAmount)
        {
            // should not move
            if((to.Position - previous.Position).sqrMagnitude < squareMovementThreshold)
            {
                // make sure that the position is to the destination position
                // even passing the threshold
                if(to.Position != from.Position)
                {
                    transform.position = Vector2.Lerp(from.Position,to.Position,lerpAmount);
                }
                return;
            }
            //Debug.Log($"interpolation from {from.Position} to {to.Position} amount {lerpAmount}");
            // extrapolate position, if a position is lost or no data it will continue moving on that direction
            // may give problems if the direction has changed

            if (lerpAmount > 1) return;
            transform.position = Vector2.LerpUnclamped(from.Position,to.Position,lerpAmount);
        }

        public void NewUpdateTransform(ushort tick, bool isTeleport, Vector2 position, float rotation)
        {
            //if the tick is outdated then skip this new transform
            if(tick <= NetworkManager.Instance.GetClient().ServerTick && !isTeleport)
            {
                return;
            }

            // update new transform list 
            // ordered with the oldets transform first
            for(int i = 0; i < futureTransformUpdates.Count; i++)
            {
                if(tick < futureTransformUpdates[i].Tick)
                {
                    futureTransformUpdates.Insert(i,new TransformUpdate(tick,isTeleport,position,rotation));

                    return;
                }
            }
   
            //if the list is empty or is the newest then add it to the end
            futureTransformUpdates.Add(new TransformUpdate(tick,isTeleport,position,rotation));
        }
    }
}
