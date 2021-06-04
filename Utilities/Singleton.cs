using UnityEngine;

namespace Voxul.Utilities
{
	/// <summary>
	/// Be aware this will not prevent a non singleton constructor
	///   such as `T myT = new T();`
	/// To prevent that, add `protected T () {}` to your singleton class.
	/// 
	/// As a note, this is made as MonoBehaviour because we need Coroutines.
	/// </summary>
	public class Singleton<T> : MonoBehaviour where T : Singleton<T>
	{

		public Singleton()
		{

			//_instance = (T)this;

		}

		private static T _instance;
		//private static readonly object _lock = new object();

		public virtual void Awake()
		{
			_instance = (T)this;
			var ins = Instance;
			//Debug.Log(ins);
		}

		public static T Instance
		{
			get
			{
				if (_instance == null)
				{
					var list = FindObjectsOfType(typeof(T));
					if (list.Length > 1)
					{
						Debug.LogError(string.Format("[Singleton] Something went really wrong, there is more than one singleton<{0}>", typeof(T)));
					}
					for (int i = 0; i < list.Length; i++)
					{
						var item = (T)list[i];
						if ((item.gameObject.hideFlags & HideFlags.HideInHierarchy) == 0)
						{
							if (_instance != null)
							{
								Debug.LogError("[Singleton] Something went really wrong " +
											   " - there should never be more than 1 singleton!" +
											   " Reopenning the scene might fix it.");
								return _instance;
							}
							_instance = item;
						}
					}
					if (list.Length == 0)
					{
						Debug.LogError(string.Format("FindObjectsOfType({0}) returned no instances!", typeof(T)));
					}

					if (_instance == null)
					{
						/*GameObject singleton = new GameObject();
						_instance = singleton.AddComponent<T>();
						singleton.name = "(singleton) " + typeof(T).ToString();
						DontDestroyOnLoad(singleton);
						Debug.Log("[Singleton] An instance of " + typeof(T) +
							" is needed in the scene, so '" + singleton +
							"' was created with DontDestroyOnLoad.");*/
						Debug.LogError(string.Format("Returning null for singleton <{0}>", typeof(T)));
					}
				}

				return _instance;
			}
		}

		private static bool isDestroyed = false;
		private static bool isApplicationQuitting = false;
		/// <summary>
		/// When Unity quits, it destroys objects in a random order.
		/// In principle, a Singleton is only destroyed when application quits.
		/// If any script calls Instance after it have been destroyed, 
		///   it will create a buggy ghost object that will stay on the Editor scene
		///   even after stopping playing the Application. Really bad!
		/// So, this was made to be sure we're not creating that buggy ghost object.
		/// </summary>
		public virtual void OnDestroy()
		{
			isDestroyed = true;
		}

		void OnApplicationQuit()
		{
			isApplicationQuitting = true;
		}

		public static bool HasInstance()
		{
			if (isApplicationQuitting)
			{
				return false;
			}

			if (isDestroyed)
			{
				return false;
			}
			return _instance != null;
		}

		public static bool IsQuitting()
		{
			return isApplicationQuitting;
		}
	}
}
