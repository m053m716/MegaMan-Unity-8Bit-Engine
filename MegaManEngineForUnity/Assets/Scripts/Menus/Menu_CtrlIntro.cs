using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu_CtrlIntro : MonoBehaviour
{


    public Menu_Intro menu;

    private void Start()
    {
        menu.Start();
    }
    private void Update()
    {
        menu.Update();
        if (Input.GetButtonDown("Start")) // Skip the cutscene
        {
            GameManager.GoToStageSelect();
        }
    }
    private void OnGUI()
    {
        menu.DrawGUI();
    }

}
