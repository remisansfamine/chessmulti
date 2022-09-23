using UnityEngine;

/*
 * Simple static camera
 */

public class PlayerCamera : MonoBehaviour
{
    [SerializeField]
    private Transform lookAt = null;
    [SerializeField]
    private float lookAtZ = 0.5f;
    [SerializeField]
    private float height = 32f;

    [SerializeField]
    private float zPos = 5f;

    private void Awake()
    {
        LookAtBoard();
    }

    private void Update()
    {
        LookAtBoard();
    }

    private void LookAtBoard()
    {
        Vector3 position = transform.position;
        position.y = height;
        transform.position = position;
        transform.LookAt(lookAt.position + Vector3.up * lookAtZ);
    }

    public void SetCamera(ChessGameMgr.EChessTeam team)
    {
        if(team == ChessGameMgr.EChessTeam.Black)
        {
            transform.position = Vector3.forward * zPos;
        }
        else
        {
            transform.position = Vector3.back * zPos;
        }

        LookAtBoard();
    }
}
