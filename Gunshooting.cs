using System.Collections;
using UnityEngine;

public class GunShooting : MonoBehaviour
{
    [Header("Damage")]
    public int bodyDamage = 10;
    public int headDamage = 25;

    [Header("Fire")]
    public float range = 150f;
    public float fireRate = 0.15f;
    public bool automaticFire = true;

    [Header("ADS")]
    public Camera mainCam;
    public float normalFOV = 60f;
    public float aimFOV = 40f;
    public float adsSpeed = 10f;

    [Header("FF Aim Offset")]
    [Range(0.3f, 0.7f)] public float hipAimX = 0.56f;
    [Range(0.3f, 0.7f)] public float hipAimY = 0.48f;
    [Range(0.3f, 0.7f)] public float adsAimX = 0.52f;
    [Range(0.3f, 0.7f)] public float adsAimY = 0.50f;

    [Header("Spread")]
    public float hipSpread = 0.01f;
    public float aimSpread = 0f;

    [Header("References")]
    public Camera fpsCam;
    public Transform muzzlePoint;
    public GameObject muzzleFlashPrefab;
    public GameObject impactEffect;
    public LineRenderer bulletTracer;
    public AudioSource gunAudioSource;
    public AudioClip fireSound;
    public CameraRecoil recoil;
    public Animator animator;

    [Header("Animator")]
    public bool useShootTrigger = true;
    public string shootTriggerName = "Fire";
    public string aimBoolName = "IsAiming";

    [Header("Tracer")]
    public bool useTracer = true;
    public float tracerTime = 0.08f;
    public float tracerStartWidth = 0.12f;
    public float tracerEndWidth = 0.03f;

    [Header("Layer Mask")]
    public LayerMask hitMask = ~0;

    float nextTimeToFire;
    bool isAiming;

    void Start()
    {
        if (fpsCam == null && Camera.main != null)
            fpsCam = Camera.main;

        if (mainCam == null && Camera.main != null)
            mainCam = Camera.main;

        if (mainCam != null)
            mainCam.fieldOfView = normalFOV;

        if (bulletTracer != null)
        {
            bulletTracer.enabled = false;
            bulletTracer.positionCount = 2;
            bulletTracer.useWorldSpace = true;
            bulletTracer.startWidth = tracerStartWidth;
            bulletTracer.endWidth = tracerEndWidth;
        }
    }

    void Update()
    {
        HandleADS();
        HandleFire();
    }

    void HandleADS()
    {
        isAiming = Input.GetMouseButton(1);

        if (mainCam != null)
        {
            float targetFOV = isAiming ? aimFOV : normalFOV;
            mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, targetFOV, adsSpeed * Time.deltaTime);
        }

        if (animator != null && HasBoolParameter(aimBoolName))
            animator.SetBool(aimBoolName, isAiming);
    }

    void HandleFire()
    {
        bool fireInput = automaticFire ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");

        if (fireInput && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        if (fpsCam == null || muzzlePoint == null) return;

        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
            Destroy(flash, 0.3f);
        }

        if (gunAudioSource != null && fireSound != null)
            gunAudioSource.PlayOneShot(fireSound);

        if (recoil != null)
            recoil.RecoilFire();

        if (animator != null && useShootTrigger && HasTriggerParameter(shootTriggerName))
            animator.SetTrigger(shootTriggerName);

        float spread = isAiming ? aimSpread : hipSpread;
        float aimX = isAiming ? adsAimX : hipAimX;
        float aimY = isAiming ? adsAimY : hipAimY;

        Ray camRay = fpsCam.ViewportPointToRay(new Vector3(aimX, aimY, 0f));

        Vector3 shootDirection = camRay.direction;
        shootDirection += fpsCam.transform.right * Random.Range(-spread, spread);
        shootDirection += fpsCam.transform.up * Random.Range(-spread, spread);
        shootDirection.Normalize();

        Ray finalRay = new Ray(camRay.origin, shootDirection);

        Vector3 targetPoint;

        if (Physics.Raycast(finalRay, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            targetPoint = hit.point;

            Debug.Log("Hit: " + hit.collider.name);

            if (impactEffect != null)
            {
                GameObject impact = Instantiate(
                    impactEffect,
                    hit.point + hit.normal * 0.05f,
                    Quaternion.LookRotation(hit.normal)
                );

                Destroy(impact, 1f);
            }

            EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                if (hit.collider.CompareTag("Head"))
                {
                    enemy.TakeDamage(headDamage);
                    Debug.Log("Headshot");
                }
                else
                {
                    enemy.TakeDamage(bodyDamage);
                    Debug.Log("Bodyshot");
                }
            }
        }
        else
        {
            targetPoint = finalRay.origin + finalRay.direction * range;
        }

        if (useTracer && bulletTracer != null)
        {
            StopCoroutine(nameof(ShowTracer));
            StartCoroutine(ShowTracer(muzzlePoint.position, targetPoint));
        }
    }

    IEnumerator ShowTracer(Vector3 startPoint, Vector3 endPoint)
    {
        bulletTracer.enabled = true;
        bulletTracer.SetPosition(0, startPoint);
        bulletTracer.SetPosition(1, endPoint);

        yield return new WaitForSeconds(tracerTime);

        if (bulletTracer != null)
            bulletTracer.enabled = false;
    }

    bool HasBoolParameter(string paramName)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
            if (param.name == paramName && param.type == AnimatorControllerParameterType.Bool)
                return true;

        return false;
    }

    bool HasTriggerParameter(string paramName)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
            if (param.name == paramName && param.type == AnimatorControllerParameterType.Trigger)
                return true;

        return false;
    }
}
