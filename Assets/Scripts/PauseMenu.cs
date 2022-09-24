using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu = null;

    void Update()
    {
        if (!pauseMenu)
            return;

        if (Input.GetButtonDown("Pause"))
            pauseMenu.SetActive(!pauseMenu.activeSelf);
    }
}
