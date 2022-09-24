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
    private Vector3 offset = new Vector3(0f,0f,5f);

    [SerializeField]
    private Vector3 teamOffset = Vector3.zero;

    private void Update()
    {
        LookAtBoard();
    }

    private void LookAtBoard()
    {
        Vector3 position = transform.position;
        position.x = Mathf.Lerp(position.x, teamOffset.x, 0.1f);
        position.y = Mathf.Lerp(position.y, height, 0.1f);
        position.z = Mathf.Lerp(position.z, teamOffset.z, 0.1f);
        transform.position = position;

        transform.LookAt(lookAt.position + Vector3.up * lookAtZ);
    }

    public void SetCamera(ChessGameMgr.EChessTeam team)
    {
        teamOffset = new Vector3(5f, 0f, 0f);

        if (team == ChessGameMgr.EChessTeam.Black)
        {
            teamOffset = offset;
        }
        else if(team == ChessGameMgr.EChessTeam.White)
        {
            teamOffset = -offset;
        }
    }
}
