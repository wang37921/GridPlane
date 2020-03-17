using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace i50Games.GridPlane
{
    [RequireComponent(typeof(Collider))]
    public class GridOccupy : MonoBehaviour
    {
        [SerializeField]
        public List<int> occupyCellIndexs = new List<int>();
    }

}
