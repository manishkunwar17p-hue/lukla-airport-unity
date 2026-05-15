using UnityEngine;
using UnityEngine.InputSystem;
public class MouseClicker : MonoBehaviour
{
[SerializeField]
private Camera m_Camera;
private bool mousePress = false;
// Start is called before the first frame update
void Start()
{
}
// Update is called once per frame
void Update()
{
Mouse mouse = Mouse.current;
if (mouse.leftButton.wasPressedThisFrame)
{
mousePress = true;
}
}
// Update is called once per frame
void FixedUpdate()
{
if (mousePress)
{
mousePress = false;
Mouse mouse = Mouse.current;
Vector3 mousePosition = mouse.position.ReadValue();
Ray ray = m_Camera.ScreenPointToRay(mousePosition);
if (Physics.Raycast(ray, out RaycastHit hit))
{
Debug.Log("Clicked on: " + hit.collider.gameObject.name);
GOInteraction aGOI = hit.collider.gameObject.GetComponent<GOInteraction>();
if (aGOI)
{
aGOI.Interaction = true;
}
}
}
}
}