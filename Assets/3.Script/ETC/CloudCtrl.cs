using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudCtrl : MonoBehaviour
{
    [SerializeField] float maxX,maxY;
    [SerializeField] Vector2 offset;
    [SerializeField] private float minSpeed = 2f;
    [SerializeField] private float maxSpeed = 10f;
    private float xSpeed;

    private void Start()
    {
        RandSpeed();
    }

    private void Respawn()
    {
        float randomY = Random.Range(-maxY, maxY);
        transform.position = new Vector3(offset.x + maxX, offset.y + randomY, transform.position.z);

        RandSpeed();
    }

    private void RandSpeed()
    {
        xSpeed = Random.Range(minSpeed, maxSpeed);
    }

    private void Update()
    {
        transform.Translate(Vector3.left * xSpeed * Time.deltaTime);

        if (transform.position.x < -maxX)
        {
            Respawn();
        }
    }

    private void OnBecameVisible()
    {
        enabled = true;
    }

    private void OnBecameInvisible()
    {
        enabled = false;
    }
}
