using UnityEngine;

public class ForceStereo : MonoBehaviour
{
    void Start()
    {
        Camera.main.stereoTargetEye = StereoTargetEyeMask.Both; // Forces stereo rendering for both eyes
    }
}
