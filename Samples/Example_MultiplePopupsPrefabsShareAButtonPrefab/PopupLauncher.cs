using System.Linq;
using BeatThat.Placements;
using UnityEngine;

namespace BeatThat.Examples
{
    public class PopupLauncher : MonoBehaviour
    {
        public GameObject[] m_popupPrefabs;

        private GameObject m_popupInst;

        private int selectedIx { get; set;  }

		private void OnGUI()
        {
           
            if (m_popupInst == null)
            {
                this.selectedIx = GUI.SelectionGrid(
                    new Rect(10, 10, Screen.width - 100, 40), 
                    this.selectedIx,
                    (string[])m_popupPrefabs.Select(p =>  p.name).ToArray(), 
                    m_popupPrefabs.Length
                );

                if(GUI.Button(new Rect(10, 60, 120, 40), "Open Popup"))
                {
                    var prefab = m_popupPrefabs[this.selectedIx];
                    m_popupInst = Instantiate(prefab, this.transform);
                    PrefabPlacement.OrientToParent(m_popupInst.transform, prefab.transform);
                }


            }
            else {
                if (GUI.Button(new Rect(10, 10, 120, 40), "Close Popup"))
                {
                    Destroy(m_popupInst);
                }
            }

		}
	}

}
