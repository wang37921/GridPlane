using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GridOccupy : MonoBehaviour
{
    public HashSet<int> occupyCellIndexs = new HashSet<int>();
}
