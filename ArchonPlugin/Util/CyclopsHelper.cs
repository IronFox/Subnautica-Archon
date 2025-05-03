using System.Collections;
using UnityEngine;
using UWE;
using Object = UnityEngine.Object;

namespace Subnautica_Archon.Util
{
    public class CyclopsHelper
    {
        internal static TaskResult<GameObject> request = new TaskResult<GameObject>();

        private static Coroutine myRoutine = null;

        public static GameObject Cyclops
        {
            get
            {
                GameObject gameObject = request.Get();
                if (gameObject == null)
                {
                    //Logging.Verbose.LogError("Couldn't get Cyclops. This is probably normal, and we'll probably get it next frame.");
                    return null;
                }

                Object.DontDestroyOnLoad(gameObject);
                gameObject.SetActive(value: false);
                return gameObject;
            }
        }

        public static void Start()
        {
            if (myRoutine == null && !request.Get())
            {
                Logging.Verbose.LogError("Starting Cyclops loading coroutine...");
                myRoutine = UWE.CoroutineHost.StartCoroutine(Ensure());
            }
        }

        private static IEnumerator Ensure()
        {
            while (Cyclops == null && !request.Get())
            {
                Logging.Verbose.LogError("Starting Cyclops loading...");
                var steps = CraftData.InstantiateFromPrefabAsync(TechType.Cyclops, request, customOnly: true);
                yield return steps;
                Logging.Verbose.LogError($"Co-routine has completed. Result = {request.Get()}");
                CoroutineHost.StopCoroutine(myRoutine);
                myRoutine = null;
            }
        }
    }
}
