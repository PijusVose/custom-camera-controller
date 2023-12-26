using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraController : Singleton<CameraController>
{
    public enum CameraState
    {
        FIRST_PERSON,
        THIRD_PERSON,
        TRANSITION
    }
    
    [Header("References")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform cameraTransform;

    [Header("Camera Config")]
    [SerializeField] private Vector3 cameraOffset;
    [SerializeField] private float sensitivityX = 1f;
    [SerializeField] private float sensitivityY = 1f;
    [SerializeField] private float followSpeed = 0.1f;
    [SerializeField] private float zoomSpeed = 1f;
    [SerializeField] private float minZoom = 2f;
    [SerializeField] private float maxZoom = 15f;
    [SerializeField] private float sphereRayRadius = 0.2f;
    [SerializeField] private LayerMask collisionLayers;

    public Camera PlayerCamera => playerCamera;

    private PlayerController playerController;

    private Camera playerCamera;
    private CameraState cameraState;
    private Vector3 followVelocity;
    private float rotVertical;
    private float rotHorizontal;
    private float goalZoom;
    private float maxCollisionZoom;
    private float zoomVelocity;
    private float lerpTime;
    private bool isColliding;

    private bool isMovementEnabled;

    private readonly float minCameraAngle = -85f;
    private readonly float maxCameraAngle = 85f;
    
    private const float SENSITIVITY_CONST = 100f;
    private const float FORCE_ROTATE_SPEED = 10f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        playerController = PlayerController.Instance;

        maxCollisionZoom = maxZoom;
        isMovementEnabled = true;

        ForceFirstPerson();
    }

    public void SetCameraMovementState(bool state, bool freeMouse = false)
    {
        isMovementEnabled = state;

        Cursor.lockState = freeMouse ? CursorLockMode.None : CursorLockMode.Locked;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && cameraState != CameraState.TRANSITION && isMovementEnabled)
        {
            SwitchViewType();
        }

        if (Input.GetKey(KeyCode.Mouse1)) 
        {
            Time.timeScale = 0.1f;
        }
        else 
        {
            Time.timeScale = 1f;
        }
        
        RotateCamera();
    }

    private void FixedUpdate()
    {
        if (playerController == null) return;
        
        if (cameraState != CameraState.FIRST_PERSON)
            CameraCollisions();
    }

    private void LateUpdate()
    {
        if (playerController == null) return;

        if (cameraState == CameraState.THIRD_PERSON)
        {
            CameraFollowCharacter();
        }
        else if (cameraState == CameraState.TRANSITION)
        {
            lerpTime += Time.deltaTime * 3f;
            
            ForcePivotPositionToHead();
        }
    }

    private void HandleZoom()
    {
        goalZoom = Mathf.Clamp(goalZoom - Input.mouseScrollDelta.y, minZoom, maxZoom);
        var targetZoom = Mathf.Clamp(goalZoom, minZoom, maxCollisionZoom);
        var smoothZoom = Mathf.SmoothDamp(cameraTransform.localPosition.z, -targetZoom, ref zoomVelocity, zoomSpeed);

        cameraTransform.localPosition = new Vector3(0f, 0f, smoothZoom);
    }

    private void CameraFollowCharacter()
    {
        var targetPosition = playerController.CharacterTransform.position + cameraOffset;
        var smoothFollow = Vector3.SmoothDamp(cameraPivot.position, targetPosition, ref followVelocity, followSpeed);

        cameraPivot.position = smoothFollow;

        HandleZoom();
    }

    private void ForcePivotPositionToHead()
    {
        var targetPosition = playerController.CharacterTransform.position + cameraOffset;
        var lerpToHead = Vector3.Lerp(cameraPivot.position, targetPosition, lerpTime);

        cameraPivot.position = lerpToHead;
    }

    private void SwitchViewType()
    {
        if (cameraState == CameraState.FIRST_PERSON)
        {
            SwitchToThirdPerson();
        }
        else
        {
            SwitchToFirstPerson();
        }
    }

    private void SwitchToFirstPerson()
    {
        cameraState = CameraState.TRANSITION;

        lerpTime = 0f;  
        
        cameraTransform.DOLocalMove(Vector3.zero, 0.4f).SetEase(Ease.InOutExpo).OnUpdate(() =>
        {
            var lookVector = cameraTransform.forward;
            lookVector.y = 0;

            var lookRotation = Quaternion.LookRotation(lookVector);
            playerController.CharacterTransform.rotation = Quaternion.Slerp(playerController.CharacterTransform.rotation, lookRotation, Time.deltaTime * FORCE_ROTATE_SPEED);
            
        }
        ).OnComplete(() =>
        {
            cameraState = CameraState.FIRST_PERSON;

            var lookVector = cameraTransform.forward;
            lookVector.y = 0;

            playerController.CharacterTransform.localRotation = Quaternion.LookRotation(lookVector);
            playerController.CharacterRenderer.enabled = false;
                
            cameraPivot.SetParent(playerController.CharacterTransform);
        });
    }

    private void ForceFirstPerson()
    {
        cameraState = CameraState.FIRST_PERSON;

        rotVertical = 0f;
        
        var lookVector = cameraTransform.forward;
        lookVector.y = 0;
                
        playerController.CharacterTransform.localRotation = Quaternion.LookRotation(lookVector);
        playerController.CharacterRenderer.enabled = false;
                
        cameraPivot.SetParent(playerController.CharacterTransform, false);
        cameraPivot.localPosition = cameraOffset;
        cameraPivot.localRotation = Quaternion.identity;
        
        cameraTransform.localPosition = Vector3.zero;
    }

    private void SwitchToThirdPerson()
    {
        cameraState = CameraState.TRANSITION;
            
        cameraPivot.SetParent(null);
        rotHorizontal = transform.eulerAngles.y;
        goalZoom = maxZoom;

        playerController.CharacterRenderer.enabled = true;

        cameraTransform.localPosition = new Vector3(0f, 0f, -0.1f);
        // cameraTransform.DOLocalMove(new Vector3(0f, 0f, -goalZoom), 0.4f)
        //     .SetEase(Ease.InOutExpo)
        //     .OnComplete(() =>
        //     {
        //         cameraState = CameraState.THIRD_PERSON;
        //     });
        
        cameraState = CameraState.THIRD_PERSON;
    }

    private void RotateCamera()
    {
        if (!isMovementEnabled) return;
        
        var horizontalInput = Input.GetAxis("Mouse X") * sensitivityX * SENSITIVITY_CONST * Time.deltaTime;
        var verticalInput = Input.GetAxis("Mouse Y") * sensitivityY * SENSITIVITY_CONST * Time.deltaTime;
        
        rotHorizontal += horizontalInput;
        rotVertical -= verticalInput;
        rotVertical = Mathf.Clamp(rotVertical, minCameraAngle, maxCameraAngle);

        if (cameraState == CameraState.FIRST_PERSON)
        {
            cameraPivot.localRotation = Quaternion.Euler(rotVertical, 0, 0);
            
            playerController.CharacterTransform.Rotate(Vector3.up * horizontalInput);
        }
        else
        {
            cameraPivot.localRotation = Quaternion.Euler(rotVertical, rotHorizontal, 0);
        }
    }

    public Vector3 GetCameraDirectionNormalized()
    {
        var dir = (cameraPivot.position - cameraTransform.position);
        dir.y = 0;

        return dir.normalized;
    }

    public bool IsInFirstPerson()
    {
        return cameraState == CameraState.FIRST_PERSON;
    }

    public Quaternion GetCameraRotation()
    {
        return cameraTransform.rotation;
    }
    
    private void CameraCollisions()
    {
        var dirVector = cameraTransform.position - cameraPivot.position;
        var ray = new Ray(cameraPivot.position, dirVector.normalized);

        isColliding = Physics.SphereCast(ray, sphereRayRadius, out var hitData, goalZoom, collisionLayers);
        
        if (isColliding)
        {
            maxCollisionZoom = hitData.distance < maxZoom ? hitData.distance : maxZoom;

            cameraTransform.localPosition = new Vector3(0f, 0f, -hitData.distance);
        }
        else
        {
            maxCollisionZoom = maxZoom;
        }
    }
}
