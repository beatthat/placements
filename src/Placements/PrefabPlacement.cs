using UnityEngine;
using BeatThat;

namespace BeatThat
{
	public abstract class PrefabPlacement : MonoBehaviour
	{

		abstract public bool objectExists { get; }

		abstract public GameObject managedGameObject { get; }

		abstract public void EnsureCreated();

		abstract public void Delete(bool deleteFoundInstance);
	}

	public class PrefabPlacement<T> : PrefabPlacement, ObjectPlacement<T> where T : Component
	{
		public T m_prefab;
		public bool m_createObjectOnStart = false;
		public bool m_createObjectOnEnable = false;
		public bool m_destroyObjectOnDisable = false;

		public bool m_disableWarnOnFindInstance;

		// Analysis disable ConvertToAutoProperty
		public T prefab
		// Analysis restore ConvertToAutoProperty
		{
			get {
				return m_prefab;
			}
			set {
				m_prefab = value;
			}
		}

		void Start()
		{
			if(m_createObjectOnStart) {
				EnsureCreated();
			}
		}

		void OnEnable()
		{
			if(m_createObjectOnEnable) {
				EnsureCreated();
			}
		}

		void OnDisable()
		{
			if(m_destroyObjectOnDisable) {
				Delete();
			}
		}
		
		override public bool objectExists
		{
			get {
				return m_object.value != null || FindObject() != null;
//				return m_gameObject != null || FindObject() != null;
			}
		}
		
		// non-virtual because ios does not support virtual generic methods
		public T managedObject
		{
			get {
				EnsureCreated();
				return m_object.value;
			}
		}

		override public GameObject managedGameObject
		{
			get {
				EnsureCreated();
				var o = m_object.value;
				return o != null? o.gameObject: null;
//				return m_gameObject;
			}
		}


		public void Recreate()
		{
			Delete();
			EnsureCreated();
		}

		override public void EnsureCreated()
		{
			if(m_object.value != null) {
				return;
			}
//			if(m_object != null && m_gameObject != null) {
//				return;
//			}


			var o = FindObject() as T;
			if(o != null) {
				m_foundNotInstantiated = true;

				if(!m_disableWarnOnFindInstance) {
					Debug.LogWarning("[" + Time.time + "] Found instance under placement prefab " 
						+ this.Path() + ". Check 'disableWarnOnFindInstance' to disable this message.");
				}

				if(!o.gameObject.activeSelf) {
					o.gameObject.SetActive(true);
				}
			}
			else {
				o = NewObject() as T;
				m_foundNotInstantiated = false;
			}
			
			ConfigureObject(o);

			m_object = new SafeRef<T>(o);
//			m_gameObject = o.gameObject;
		}

		public void Delete()
		{
			Delete(false);
		}

		override public void Delete(bool deleteFoundInstance)
		{
			var o = m_object.value;
			if(o == null) {
				m_object = new SafeRef<T>(null);
				return;
			}

			var go = o.gameObject;
			if(go == null) {
				m_object = new SafeRef<T>(null);
				return;
			}

			if(m_foundNotInstantiated && !deleteFoundInstance) {
				go.SetActive(false);
				return;
			}


			m_lastDestroyFrame = Time.frameCount;
#if UNITY_EDITOR
			if(Application.isPlaying) {
				Destroy(go);
			}
			else {
				DestroyImmediate(go);
			}
#else
			Destroy(go);
#endif

			m_object = new SafeRef<T>(null);
		}

		private object FindObject()
		{
			if(m_lastDestroyFrame == Time.frameCount) { // we can't use DestroyImmediate. If was destroyed this frame, don't find
				return null;
			}
			else if(this.transform.childCount > 0) {
				return this.transform.GetComponentInChildren(typeof(T), true);
			}
			else {
				return null;
			}
		}
		
		// returns object instead of generic T because ios does not support virtual generic methods
		private object NewObject()
		{
			T obj = null;
	#if UNITY_EDITOR
			obj = !Application.isPlaying ? UnityEditor.PrefabUtility.InstantiatePrefab (m_prefab) as T : (Instantiate (m_prefab) as T);
	#else
			obj = (Instantiate(m_prefab) as T);
	#endif

			obj.name = this.name;

			obj.transform.SetParent(this.transform, false);

			OrientToParent(obj.transform);
			
			return obj;
		}

		private void OrientToParent(Transform inst)
		{

			inst.localScale = Vector3.one;
			inst.localRotation = Quaternion.identity;
			inst.localPosition = Vector3.zero;

	#if !(UNITY_4_3 || UNITY_4_5)
			var rectTransform = inst as RectTransform;
			if(rectTransform != null) {
				RectTransform instRT = rectTransform;
				var prefabRT = this.prefab.transform as RectTransform;

				// RectTransform objects seem to lose their offsets in instantiation?
				// Maybe this is unity trying to scale for screen res/aspect ratio, 
				// but it's really broken at point of this edit
				instRT.anchorMin = prefabRT.anchorMin;
				instRT.anchorMax = prefabRT.anchorMax;
				instRT.offsetMin = prefabRT.offsetMin;
				instRT.offsetMax = prefabRT.offsetMax;
			}
	#endif

		}
		
		// param object instead of generic T because ios does not support virtual generic methods
		virtual protected void ConfigureObject(object o)
		{
			
		}

		private int m_lastDestroyFrame = -1;
		private bool m_foundNotInstantiated;
		private SafeRef<T> m_object;


	}
}
