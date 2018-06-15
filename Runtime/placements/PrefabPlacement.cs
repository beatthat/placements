using System;
using System.Collections.Generic;
using BeatThat.GetComponentsExt;
using BeatThat.GizmoSetting;
using BeatThat.ManagePrefabInstances;
using BeatThat.SafeRefs;
using BeatThat.TransformPathExt;
using UnityEngine;
using UnityEngine.Serialization;

namespace BeatThat.Placements
{
    /// <summary>
    /// Non-generic base class for PrefabPlacement, makes it possible to have a default inspector (that's also why the class is not abstract)
    /// </summary>
    public class PrefabPlacement : MonoBehaviour, ManagesPrefabInstances
	{
		public bool m_createObjectOnStart = false;
		public bool m_createObjectOnEnable = false;
		public bool m_destroyObjectOnDisable = false;


		[Tooltip(@"Control how you want this PrefabPlacement to treat instances of its prefab:
	
WarnOnFindInstance 
------------------
Delete instances on apply parent prefab. When the app is running, log a warning if an instance of the prefab is found. 

AllowInstanceInScene
-------------------- 
Delete instances on apply parent prefab, but ignore instances found on the scene at runtime.

AllowApplyInstanceToParentPrefab
-------------------------------- 
When parent prefab is applied bake any found instances into the parent prefab."
)]
		[FormerlySerializedAs("m_disableWarnOnFindInstance")]public PrefabInstancePolicy m_defaultInstancePolicy;


		[Tooltip(@"set TRUE to see GizmoSettings in inspector")]
		public bool m_unhideGizmoSettings;

		public PrefabInstancePolicy defaultInstancePolicy { get { return m_defaultInstancePolicy; } } 

		virtual public bool isPrefabSet { get { throw new NotImplementedException(); } } 

		virtual public bool objectExists { get { throw new NotImplementedException(); } } 

		virtual public GameObject managedGameObject { get { throw new NotImplementedException(); } } 
        
		virtual public UnityEngine.Object prefabObject { get { throw new NotImplementedException(); } }

		virtual public bool supportsMultiplePrefabTypes { get { return false; } }

		virtual public void GetPrefabInstances (ICollection<PrefabInstance> instances, bool ensureCreated = false)
		{
			throw new NotImplementedException ();
		}

		virtual public void GetPrefabTypes (ICollection<PrefabType> types)
		{
			throw new NotImplementedException ();
		}

		virtual public void EnsureCreated()
		{
			throw new NotImplementedException();
		}

		virtual public void Delete(bool deleteFoundInstance)
		{
			throw new NotImplementedException();
		}

		public static void OrientToParent(Transform inst, Transform asset)
		{
			inst.localScale = Vector3.one;
			inst.localRotation = Quaternion.identity;
			inst.localPosition = Vector3.zero;

			#if !(UNITY_4_3 || UNITY_4_5)
			var rectTransform = inst as RectTransform;
			if(rectTransform != null) {
				RectTransform instRT = rectTransform;
				var prefabRT = asset.transform as RectTransform;

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

		#if UNITY_EDITOR
		virtual protected void Awake()
		{
			UpdateGizmoSettingsVisibility ();
		}

		void OnValidate()
		{
			UpdateGizmoSettingsVisibility ();
		}

		public void UpdateGizmoSettingsVisibility()
		{
			var hf = (m_unhideGizmoSettings) ? HideFlags.None : HideFlags.HideInInspector;

			this.AddIfMissing<GizmoSettings> ().hideFlags = hf;
		}
		#endif

		public void OnApplyPrefab_BeforeAllSiblings (GameObject prefabInstance, GameObject prefab) {}

		public void OnApplyPrefab (GameObject prefabInstance, GameObject prefab)
		{
			#if UNITY_EDITOR
			this.ApplyManagedPrefabInstancesThenRemoveFromParentPrefab();
			#endif
		}

		public void OnApplyPrefab_AfterAllSiblings (GameObject prefabInstance, GameObject prefab) {}

	}

	public class PrefabPlacement<T> : PrefabPlacement, Placement<T> where T : Component
	{
		public T m_prefab;

		override public bool isPrefabSet { get { return m_prefab != null; } }

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

		public override UnityEngine.Object prefabObject { get { return this.prefab; } }

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
			}
		}

		override public void GetPrefabInstances (ICollection<PrefabInstance> instances, bool ensureCreated = false)
		{
			if(ensureCreated) {
				EnsureCreated(true);
			}

			var inst = m_object.isValid? m_object.value: FindObject() as T;
			if(inst == null) {
				return;
			}

			instances.Add(new PrefabInstance {
				prefab = this.prefab,
				prefabType = typeof(T),
				instance = inst.gameObject,
				instancePolicy = this.defaultInstancePolicy
			});
		}

		override public void GetPrefabTypes(ICollection<PrefabType> types)
		{
			types.Add (new PrefabType {
				prefab = this.prefab,
				prefabType = typeof(T),
				instancePolicy = this.defaultInstancePolicy
			});
		}

		public void Recreate()
		{
			Delete();
			EnsureCreated();
		}

		override public void EnsureCreated()
		{
			EnsureCreated (false);
		}

		private void EnsureCreated(bool supressWarnings)
		{
			if(m_object.value != null) {
				return;
			}


			var o = FindObject() as T;
			if(o != null) {
				m_foundNotInstantiated = true;

				if(m_defaultInstancePolicy == PrefabInstancePolicy.WarnOnFindInstance && !supressWarnings) {
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
			obj.name = m_prefab.name;
	#else
			obj = (Instantiate(m_prefab) as T);
	#endif

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





