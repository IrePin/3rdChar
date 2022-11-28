using static scr_Models;
using UnityEngine;

public class scr_CameraController : MonoBehaviour
{
    [Header("References")]
    public scr_PlayerController playerController;
    [HideInInspector]
    public Vector3 targetRotation;
    public GameObject yGimbal;
    private Vector3 yGibalRotation;

    [Header("Settings")]
    public CameraSettingsModel settings;

    #region - Update -

    private void Update()
    {
        CameraRotation();
        FollowPlayerCameraTarget();
    }

    #endregion

    #region - Position / Rotation -

    private void CameraRotation()
    {
        var viewInput = playerController.input_View;

        targetRotation.y += (settings.InvertedX ? -(viewInput.x * settings.SensitivityX) : (viewInput.x * settings.SensitivityX)) * Time.deltaTime;
        transform.rotation = Quaternion.Euler(targetRotation);

        yGibalRotation.x += (settings.InvertedY ? (viewInput.y * settings.SensitivityY) : -(viewInput.y * settings.SensitivityY)) * Time.deltaTime;
        yGibalRotation.x = Mathf.Clamp(yGibalRotation.x, settings.YClampMin, settings.YClampMax);

        yGimbal.transform.localRotation = Quaternion.Euler(yGibalRotation);

    }

    private void FollowPlayerCameraTarget()
    {
        transform.position = playerController.cameraTarget.position;
    }

    #endregion
}
