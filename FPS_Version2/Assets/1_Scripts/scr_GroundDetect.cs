using UnityEngine;

public class scr_GroundDetect : MonoBehaviour
{
    scr_PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponentInParent<scr_PlayerController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == this.gameObject)
        {
            return;
        }
        playerController.isGrounded = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject == this.gameObject)
        {
            return;
        }
        playerController.isGrounded = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == this.gameObject)
        {
            return;
        }
        playerController.isGrounded = false;
    }
}
