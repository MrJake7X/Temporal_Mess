using MoreMountains.TopDownEngine;
using UnityEngine;

public class SwitchAimMode : MonoBehaviour
{

    public InputManager inputManager;

    void Start()
    {
        
    }

    void Update()
    {
        if(Input.GetJoystickNames().Length > 0)
        {
            inputManager.WeaponForcedMode = WeaponAim3D.AimControls.SecondaryMovement;
        }
        else if (Input.mousePresent)
        {
            inputManager.WeaponForcedMode = WeaponAim3D.AimControls.Mouse;
        }
    }
}
