using System.Collections.Generic;
using UnityEngine;
public class AirportManager : MonoBehaviour
{
private List<GameObject> Children = new List<GameObject>();
private GOInteraction myGOI;
// Start is called before the first frame update
void Start()
{
foreach (Transform child in transform)
{
if (child.tag == "Information")
{
Children.Add(child.gameObject);
}
}
myGOI = GetComponent<GOInteraction>();
if (myGOI == null)
{
Debug.Log("No GOInteraction attached to this object.");
}
// Default children to invisible.
foreach (GameObject child in Children)
{
child.SetActive(false);
}
}
// Update is called once per frame
void Update()
{
if (myGOI.Interaction == true)
{
foreach (GameObject child in Children)
{
if (child.activeSelf)
{
child.SetActive(false);
}
else
{
child.SetActive(true);
}
}
myGOI.Interaction = false;
}
}
}