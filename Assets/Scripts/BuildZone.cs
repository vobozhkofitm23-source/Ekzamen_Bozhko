using UnityEngine;

namespace NightWatch
{
    public class BuildZone : MonoBehaviour
    {
        public bool HasTower;

        public void PutTowerHere(Tower tower)
        {
            HasTower = true;
            SetTileVisible(false);
            tower.transform.SetParent(transform, false);
            tower.transform.localPosition = new Vector3(0, 0.65f, 0);
            tower.transform.localRotation = Quaternion.identity;
            tower.transform.localScale = Vector3.one;
        }

        public void Free()
        {
            HasTower = false;
            SetTileVisible(true);
        }

        void SetTileVisible(bool on)
        {
            foreach (Transform child in transform)
                if (!child.GetComponent<Tower>()) child.gameObject.SetActive(on);
        }
    }
}
