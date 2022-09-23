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
    private float zOffset = 5f;

    [SerializeField]
    private float teamZOffset= 0f;

    private void Update()
    {
        LookAtBoard();
    }

    private void LookAtBoard()
    {
        Vector3 position = transform.position;
        position.z = Mathf.Lerp(position.z, teamZOffset, 0.1f);
        position.y = Mathf.Lerp(position.y, height, 0.1f);
        transform.position = position;

        transform.LookAt(lookAt.position + Vector3.up * lookAtZ);
    }

    public void SetCamera(ChessGameMgr.EChessTeam team)
    {
        teamZOffset = -zOffset;

        if (team == ChessGameMgr.EChessTeam.Black)
        {
            teamZOffset = zOffset;
        }
    }
}
